using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// Ordinary endgame-map boss tracker.
///
/// Deterministic map bosses are armed only after database-backed OCR proves the expected
/// identity. Unknown/special maps fail closed rather than allowing structural UI alone to
/// qualify a map. After trusted acquisition, disappearance remains structural because the
/// health fill can reach zero before the boss UI itself vanishes.
///
/// A long in-map disappearance grace protects scripted/temporary boss absence. When the
/// ASL reports a real external map exit while that same trusted boss was already missing,
/// a shorter exit-assist gate can finalize the disappearance without making ordinary
/// in-map tracking more permissive.
/// </summary>
public sealed class GenericMapBossTracker
{
    private enum TrackMode
    {
        Scan,
        Single,
        Dual
    }

    private sealed record MapOcrDecision(
        bool Accepted,
        string BossId,
        string BossName,
        double Similarity,
        string Source,
        string OcrText,
        string? EventMechanic,
        string? EventBossName);

    private readonly AppConfig _config;
    private readonly EventWriter _events;
    private readonly DebugImageWriter _images;
    private readonly OcrService _ocr;
    private readonly MapBossDatabase _database;
    private readonly BossLocalizationDatabase _localizations;
    private readonly string _gameLanguage;
    private readonly BossNameMatcher? _eventMatcher;

    private TrackMode _mode = TrackMode.Scan;
    private int _singleCandidateFrames;
    private int _singleCenteredCandidateFrames;
    private int _dualCandidateFrames;
    private int _dualPromotionFrames;
    private DateTimeOffset _nextAcquireAllowed = DateTimeOffset.MinValue;
    private DateTimeOffset _nextMapOcrAllowed = DateTimeOffset.MinValue;
    private DateTimeOffset _lastDatabaseNoticeAt = DateTimeOffset.MinValue;
    private string _lastDatabaseNoticeKey = "";
    private double _singleNameBaseline;
    private double _singleMissingThreshold;
    private double _singleReturnedThreshold;
    private DateTimeOffset? _firstMissing;
    private DateTimeOffset? _verifyStarted;
    private BossContextState _armedContext = BossContextState.IdentityDefault;
    private string _armedBossId = "";
    private string _armedBossName = "";
    private string _armedDetector = "structural-fallback";

    public GenericMapBossTracker(
        AppConfig config,
        EventWriter events,
        DebugImageWriter images,
        OcrService ocr,
        MapBossDatabase database,
        BossLocalizationDatabase localizations,
        string gameLanguage)
    {
        _config = config;
        _events = events;
        _images = images;
        _ocr = ocr;
        _database = database;
        _localizations = localizations;
        _gameLanguage = GameLanguageCatalog.Normalize(gameLanguage);
        var eventDefs = database.GetEventDefinitions(localizations, _gameLanguage);
        _eventMatcher = eventDefs.Count > 0 ? new BossNameMatcher(eventDefs) : null;
    }

    public string StateLabel => _mode switch
    {
        TrackMode.Single => _verifyStarted.HasValue ? "MAP_VERIFY" : "MAP_TRACK",
        TrackMode.Dual => _verifyStarted.HasValue ? "MAP_DUAL_VERIFY" : "MAP_DUAL",
        _ => "MAP_SCAN"
    };

    public bool IsTracking => _mode != TrackMode.Scan;
    public bool IsDual => _mode == TrackMode.Dual;
    public BossContextState ArmedContext => _armedContext;
    public string TrackedBossName => _armedBossName;

    public void Observe(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context)
    {
        if (_mode == TrackMode.Scan)
        {
            ObserveScan(now, raw, metrics, context);
            return;
        }

        if (_mode == TrackMode.Single)
        {
            ObserveSingle(now, raw, metrics);
            return;
        }

        ObserveDual(now, raw, metrics);
    }

    private void ObserveScan(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context)
    {
        if (now < _nextAcquireAllowed) return;

        var mapEntry = _database.Resolve(context.AreaId);
        var deterministic = mapEntry is not null && mapEntry.HasDeterministicBosses;
        IReadOnlyList<BossDefinition> expectedDefinitions = Array.Empty<BossDefinition>();
        if (deterministic)
        {
            expectedDefinitions = _database.GetExpectedDefinitions(mapEntry!, _localizations, _gameLanguage);
            if (expectedDefinitions.Count != mapEntry!.Bosses.Count)
            {
                var missing = string.Join(",", mapEntry.Bosses.Select(b => b.Id).Where(id => expectedDefinitions.All(x => !string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))));
                MaybeLogDatabaseNotice(now, $"LOCALIZATION:{context.AreaId}:{_gameLanguage}:{missing}",
                    $"MAP_LOCALIZATION_UNAVAILABLE | area={context.AreaId} | map={mapEntry.MapName} | gameLanguage={_gameLanguage}" +
                    $" | missingBossIds={missing} | behavior=do-not-arm");
                _singleCandidateFrames = 0;
                _singleCenteredCandidateFrames = 0;
                _dualCandidateFrames = 0;
                return;
            }
        }

        if (mapEntry is null)
        {
            MaybeLogDatabaseNotice(now, $"MISS:{context.AreaId}",
                $"MAP_DATABASE_MISS | area={context.AreaId} | behavior=do-not-arm | reason=no-authoritative-map-boss");
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            _dualCandidateFrames = 0;
            return;
        }

        if (!deterministic)
        {
            MaybeLogDatabaseNotice(now, $"SPECIAL:{context.AreaId}:{mapEntry.CompletionType}",
                $"MAP_DATABASE_SPECIAL | area={context.AreaId} | map={mapEntry.MapName}" +
                $" | completionType={mapEntry.CompletionType} | behavior=do-not-arm | reason=dedicated-policy-required");
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            _dualCandidateFrames = 0;
            return;
        }

        if (metrics.DualSignature)
        {
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            _dualCandidateFrames++;
            if (_dualCandidateFrames < _config.DualInitialConfirmFrames) return;

            if (now < _nextMapOcrAllowed) return;
            var decision = TryIdentifyDual(raw, mapEntry!, expectedDefinitions);
            _nextMapOcrAllowed = now.AddMilliseconds(_config.OcrAcquisitionCycleMs);
            if (decision.Accepted)
                ArmDual(now, raw, metrics, context, "initial-dual-db-ocr", decision);
            else
                LogRejectedMapBoss(now, context, mapEntry!, decision, "dual");
            return;
        }

        _dualCandidateFrames = 0;
        if (!IsSingleStructuralCandidate(metrics))
        {
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            return;
        }

        _singleCandidateFrames++;
        var centeredUi = metrics.CenterNameGoldFraction >= _config.SingleOcrMinCenterNameGoldFraction;
        _singleCenteredCandidateFrames = centeredUi ? _singleCenteredCandidateFrames + 1 : 0;
        var fastConfirmed = _singleCenteredCandidateFrames >= _config.OcrCandidateConsecutiveFrames;
        var persistentConfirmed = _singleCandidateFrames >= _config.SingleOcrWeakCenterConfirmFrames;
        if (!fastConfirmed && !persistentConfirmed) return;

        if (now < _nextMapOcrAllowed) return;
        var singleDecision = TryIdentifySingle(raw, mapEntry!, expectedDefinitions);
        _nextMapOcrAllowed = now.AddMilliseconds(_config.OcrAcquisitionCycleMs);
        if (singleDecision.Accepted)
            ArmSingle(now, raw, metrics, context, singleDecision);
        else
            LogRejectedMapBoss(now, context, mapEntry!, singleDecision, "single");
    }

    private bool IsSingleStructuralCandidate(BossBarMetrics metrics)
        => metrics.HealthRedRunFraction >= _config.OcrTriggerRedRunFraction
        && metrics.NameGoldFraction >= _config.OcrMinNameGoldPixelFraction;

    private MapOcrDecision TryIdentifySingle(Bitmap raw, MapBossEntry entry, IReadOnlyList<BossDefinition> expectedDefinitions)
    {
        var expected = new BossNameMatcher(expectedDefinitions);
        MapOcrDecision? bestRejected = null;

        foreach (var mode in new[] { OcrPreprocessMode.Gold, OcrPreprocessMode.Broad })
        {
            using var processed = ScreenCapture.PreprocessBossNameForOcr(raw, _config, BossLane.Single, mode);
            var read = _ocr.ReadSingleLine(processed);
            var source = mode == OcrPreprocessMode.Gold ? "map-gold-single" : "map-broad-single";
            var match = expected.Match(read.Text, _config.MinOcrSimilarity);
            if (match is not null)
            {
                _events.Debug($"MAP_OCR_MATCH | map={entry.MapName} | lane=single | source={source}" +
                    $" | boss={match.Boss.Id} | score={match.Similarity:F3} | conf={read.Confidence:F3}" +
                    $" | text={BossNameMatcher.Normalize(read.Text)}");
                return new MapOcrDecision(true, match.Boss.Id, match.Boss.Name, match.Similarity,
                    source, BossNameMatcher.Normalize(read.Text), null, null);
            }

            var eventDecision = MatchEvent(read, source);
            if (eventDecision is not null)
                bestRejected = eventDecision;
            else
                bestRejected ??= new MapOcrDecision(false, "", "", 0, source,
                    BossNameMatcher.Normalize(read.Text), null, null);
        }

        return bestRejected ?? new MapOcrDecision(false, "", "", 0, "map-ocr", "", null, null);
    }

    private MapOcrDecision TryIdentifyDual(Bitmap raw, MapBossEntry entry, IReadOnlyList<BossDefinition> expectedDefinitions)
    {
        var expectedDefs = expectedDefinitions;
        var expected = new BossNameMatcher(expectedDefs);
        var matched = new Dictionary<string, BossMatch>(StringComparer.OrdinalIgnoreCase);
        MapOcrDecision? eventRejected = null;
        var texts = new List<string>();

        foreach (var lane in new[] { BossLane.Left, BossLane.Right })
        {
            BossMatch? laneBest = null;
            string laneSource = "";
            string laneText = "";

            foreach (var mode in new[] { OcrPreprocessMode.Gold, OcrPreprocessMode.Broad })
            {
                using var processed = ScreenCapture.PreprocessBossNameForOcr(raw, _config, lane, mode);
                var read = _ocr.ReadSingleLine(processed);
                var source = $"map-{(mode == OcrPreprocessMode.Gold ? "gold" : "broad")}-{lane.ToString().ToLowerInvariant()}";
                var normalized = BossNameMatcher.Normalize(read.Text);
                if (normalized.Length > 0) texts.Add($"{lane}:{normalized}");

                var match = expected.Match(read.Text, _config.MinOcrSimilarity);
                if (match is not null && (laneBest is null || match.Similarity > laneBest.Similarity))
                {
                    laneBest = match;
                    laneSource = source;
                    laneText = normalized;
                }

                eventRejected ??= MatchEvent(read, source);
            }

            if (laneBest is not null)
            {
                matched[laneBest.Boss.Id] = laneBest;
                _events.Debug($"MAP_OCR_MATCH | map={entry.MapName} | lane={lane.ToString().ToLowerInvariant()}" +
                    $" | source={laneSource} | boss={laneBest.Boss.Id} | score={laneBest.Similarity:F3}" +
                    $" | text={laneText}");
            }
        }

        var accepted = entry.RequiresAllBosses
            ? expectedDefs.All(b => matched.ContainsKey(b.Id))
            : matched.Count > 0;

        if (accepted)
        {
            var names = entry.RequiresAllBosses
                ? string.Join(" + ", expectedDefs.Select(b => b.Name))
                : matched.Values.OrderByDescending(m => m.Similarity).First().Boss.Name;
            var ids = entry.RequiresAllBosses
                ? string.Join("+", expectedDefs.Select(b => b.Id))
                : matched.Values.OrderByDescending(m => m.Similarity).First().Boss.Id;
            var score = matched.Count > 0 ? matched.Values.Min(m => m.Similarity) : 0;
            return new MapOcrDecision(true, ids, names, score, "map-dual-db-ocr",
                string.Join(" / ", texts), null, null);
        }

        return eventRejected ?? new MapOcrDecision(false, "", "", 0, "map-dual-db-ocr",
            string.Join(" / ", texts), null, null);
    }

    private MapOcrDecision? MatchEvent(OcrRead read, string source)
    {
        if (_eventMatcher is null) return null;
        var match = _eventMatcher.Match(read.Text, _config.MinOcrSimilarity);
        if (match is null) return null;
        var eventEntry = _database.FindEventBoss(match.Boss.Id);
        return new MapOcrDecision(false, match.Boss.Id, match.Boss.Name, match.Similarity,
            source, BossNameMatcher.Normalize(read.Text), eventEntry?.Mechanic, match.Boss.Name);
    }

    private void LogRejectedMapBoss(DateTimeOffset now, BossContextState context, MapBossEntry entry, MapOcrDecision decision, string layout)
    {
        var expected = string.Join(",", entry.Bosses.Select(b => b.Id));
        var noticeKey = decision.EventBossName is not null
            ? $"EVENT:{context.AreaId}:{decision.BossId}"
            : $"MISS:{context.AreaId}:{decision.OcrText}";
        if (_lastDatabaseNoticeKey == noticeKey &&
            (now - _lastDatabaseNoticeAt).TotalSeconds < 3)
            return;

        _lastDatabaseNoticeKey = noticeKey;
        _lastDatabaseNoticeAt = now;

        if (decision.EventBossName is not null)
        {
            _events.Debug($"MAP_EVENT_BOSS_IGNORED | area={context.AreaId} | map={entry.MapName}" +
                $" | mechanic={decision.EventMechanic ?? "unknown"} | boss={decision.EventBossName}" +
                $" | expected={expected} | layout={layout} | score={decision.Similarity:F3}");
        }
        else
        {
            _events.Debug($"MAP_UNEXPECTED_BOSS_IGNORED | area={context.AreaId} | map={entry.MapName}" +
                $" | expected={expected} | layout={layout} | source={decision.Source} | ocr={decision.OcrText}");
        }
    }

    private void MaybeLogDatabaseNotice(DateTimeOffset now, string key, string message)
    {
        if (_lastDatabaseNoticeKey == key &&
            (now - _lastDatabaseNoticeAt).TotalSeconds < 30)
            return;
        _lastDatabaseNoticeKey = key;
        _lastDatabaseNoticeAt = now;
        _events.Debug(message);
    }

    private void ArmSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context, MapOcrDecision? identity)
    {
        _mode = TrackMode.Single;
        _armedContext = context;
        _firstMissing = null;
        _verifyStarted = null;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _dualCandidateFrames = 0;
        _dualPromotionFrames = 0;

        _armedBossId = identity?.BossId ?? "";
        _armedBossName = identity?.BossName ?? "";
        _armedDetector = identity is null ? "structural-fallback" : "database-ocr";

        _singleNameBaseline = metrics.NameGoldFraction;
        _singleMissingThreshold = Math.Max(
            _config.TrackedNameMissingAbsoluteFraction,
            _singleNameBaseline * _config.TrackedNameMissingRelativeFraction);
        _singleReturnedThreshold = Math.Max(
            _config.TrackedNameReturnedAbsoluteFraction,
            _singleNameBaseline * _config.TrackedNameReturnedRelativeFraction);

        _events.MapBossSeen(now, _armedContext, metrics, "single", _armedBossId, _armedBossName, _armedDetector);
        _events.Debug($"MAP_TRACK_ARMED | layout=single | detector={_armedDetector} | area={_armedContext.AreaId}" +
            $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
            $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}" +
            $" | run={metrics.HealthRedRunFraction:F4} | nameGold={metrics.NameGoldFraction:F4}");
        if (_config.SaveStateChangeImages) _images.Save("MAP_SEEN", raw, null);
    }

    private void ArmDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context, string reason, MapOcrDecision? identity)
    {
        _mode = TrackMode.Dual;
        _armedContext = context;
        _firstMissing = null;
        _verifyStarted = null;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _dualCandidateFrames = 0;
        _dualPromotionFrames = 0;

        _armedBossId = identity?.BossId ?? "";
        _armedBossName = identity?.BossName ?? "";
        _armedDetector = identity is null ? "structural-fallback" : "database-ocr";

        _events.MapBossSeen(now, _armedContext, metrics, "dual", _armedBossId, _armedBossName, _armedDetector);
        _events.Debug($"MAP_TRACK_ARMED | layout=dual | reason={reason} | detector={_armedDetector}" +
            $" | area={_armedContext.AreaId} | level={_armedContext.AreaLevel}" +
            $" | bossNumber={_armedContext.MapBossNumber} | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}" +
            $" | leftRun={metrics.LeftHealthRedRunFraction:F4} | rightRun={metrics.RightHealthRedRunFraction:F4}" +
            $" | nameGold={metrics.NameGoldFraction:F4}");
        if (_config.SaveStateChangeImages) _images.Save("MAP_SEEN_DUAL", raw, null);
    }

    private void ObserveSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        if (!_verifyStarted.HasValue && metrics.DualSignature &&
            metrics.HealthRedRunFraction >= _config.DualAddMinCombinedHealthRunFraction)
        {
            _dualPromotionFrames++;
            if (_dualPromotionFrames >= _config.DualAddConfirmFrames)
            {
                // Identity was already proven when the single boss armed. Promotion only changes
                // topology; it does not allow a new unrelated boss to replace the expected identity.
                var identity = _armedBossName.Length > 0
                    ? new MapOcrDecision(true, _armedBossId, _armedBossName, 1, _armedDetector, "", null, null)
                    : null;
                ArmDual(now, raw, metrics, _armedContext, "single-to-dual", identity);
                return;
            }
        }
        else
        {
            _dualPromotionFrames = 0;
        }

        var value = metrics.NameGoldFraction;
        var missing = value <= _singleMissingThreshold;
        var returned = value >= _singleReturnedThreshold;

        if (!_verifyStarted.HasValue)
        {
            if (!missing) return;
            _firstMissing = now;
            _verifyStarted = now;
            _events.Debug($"MAP_MISSING_STARTED | layout=single | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}" +
                $" | presence=structural-gold-band | nameGold={metrics.NameGoldFraction:F4}" +
                $" | confirmMs={_config.MapDisappearConfirmMs}");
            return;
        }

        if (returned)
        {
            _events.Debug($"MAP_MISSING_CANCELLED | layout=single | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}");
            _firstMissing = null;
            _verifyStarted = null;
            return;
        }

        if ((now - _verifyStarted.Value).TotalMilliseconds < _config.MapDisappearConfirmMs) return;
        CommitGone(now, raw, _config.MapDisappearConfirmMs, "single", "timer");
    }

    private void ObserveDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        var missing = metrics.NameGoldFraction <= _config.DualBothGoneMaxNameGoldFraction;
        var returned = metrics.NameGoldFraction >= _config.TrackedNameReturnedAbsoluteFraction;

        if (!_verifyStarted.HasValue)
        {
            if (!missing) return;
            _firstMissing = now;
            _verifyStarted = now;
            _events.Debug($"MAP_MISSING_STARTED | layout=dual | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}" +
                $" | nameGold={metrics.NameGoldFraction:F4} | confirmMs={_config.MapDisappearConfirmMs}");
            return;
        }

        if (returned)
        {
            _events.Debug($"MAP_MISSING_CANCELLED | layout=dual | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}" +
                $" | nameGold={metrics.NameGoldFraction:F4} | confirmMs={_config.MapDisappearConfirmMs}");
            _firstMissing = null;
            _verifyStarted = null;
            return;
        }

        if ((now - _verifyStarted.Value).TotalMilliseconds < _config.MapDisappearConfirmMs) return;
        CommitGone(now, raw, _config.MapDisappearConfirmMs, "dual", "timer");
    }

    private void CommitGone(DateTimeOffset now, Bitmap? raw, int confirmMs, string layout, string confirmation)
    {
        var firstMissing = _firstMissing ?? _verifyStarted ?? now;
        var verifyStarted = _verifyStarted ?? firstMissing;
        if (_config.SaveStateChangeImages && raw is not null) _images.Save("MAP_GONE", raw, null);
        _events.MapBossGone(firstMissing, verifyStarted, now, confirmMs, _armedContext, layout,
            _armedBossId, _armedBossName, _armedDetector, confirmation);
        ResetInternal(now.AddMilliseconds(_config.ReacquireCooldownMs));
    }

    /// <summary>
    /// Reconciles an ASL-owned context transition with an already-armed map boss.
    /// A recognized map child never confirms the parent boss. A true external exit may
    /// shorten only an already-running disappearance verification for a trusted database
    /// OCR identity; it cannot create a disappearance from an otherwise-present boss.
    /// </summary>
    public void HandleContextChange(DateTimeOffset now, BossContextState nextContext)
    {
        if (_mode == TrackMode.Scan) return;

        var childTransition = string.Equals(
            nextContext.Classification,
            "map-child-area",
            StringComparison.OrdinalIgnoreCase);

        var sameArmedMap = nextContext.Mode == BossDetectionMode.Map
            && string.Equals(nextContext.AreaId, _armedContext.AreaId, StringComparison.OrdinalIgnoreCase)
            && nextContext.MapBossNumber == _armedContext.MapBossNumber;

        if (sameArmedMap)
        {
            _armedContext = nextContext;
            _events.Debug($"MAP_CONTEXT_REFRESH_RETAINED | area={_armedContext.AreaId}" +
                $" | bossNumber={_armedContext.MapBossNumber} | classification={nextContext.Classification}");
            return;
        }

        var realExternalExit = string.Equals(
            nextContext.Classification,
            "premature-exit-unresolved",
            StringComparison.OrdinalIgnoreCase);

        if (!childTransition && realExternalExit && _verifyStarted.HasValue &&
            string.Equals(_armedDetector, "database-ocr", StringComparison.OrdinalIgnoreCase))
        {
            var firstMissing = _firstMissing ?? _verifyStarted.Value;
            var missingMs = Math.Max(0, (now - firstMissing).TotalMilliseconds);
            if (missingMs >= _config.MapExitAssistMinMissingMs)
            {
                _events.Debug($"MAP_EXIT_ASSIST_CONFIRMED | area={_armedContext.AreaId}" +
                    $" | boss={(_armedBossName.Length > 0 ? _armedBossName : _armedBossId)}" +
                    $" | missingMs={missingMs:F1} | thresholdMs={_config.MapExitAssistMinMissingMs}" +
                    $" | nextArea={nextContext.AreaId} | nextClass={nextContext.Classification}");
                CommitGone(now, null, _config.MapExitAssistMinMissingMs,
                    _mode == TrackMode.Dual ? "dual" : "single", "exit-assist");
                return;
            }

            _events.Debug($"MAP_EXIT_ASSIST_REJECTED | area={_armedContext.AreaId}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : _armedBossId)}" +
                $" | missingMs={missingMs:F1} | thresholdMs={_config.MapExitAssistMinMissingMs}" +
                $" | reason=missing-too-recent | nextArea={nextContext.AreaId}");
        }
        else if (childTransition && _verifyStarted.HasValue)
        {
            _events.Debug($"MAP_EXIT_ASSIST_SUPPRESSED | area={_armedContext.AreaId}" +
                $" | reason=map-child-area | child={nextContext.AreaId}");
        }

        ResetTracking(childTransition ? "map child context changed" : "boss context changed");
    }

    public void SuspendCapture()
    {
        // Missing confirmation intentionally does not advance while the game is not capturable.
    }

    public void ResetTracking(string reason)
    {
        if (_mode != TrackMode.Scan)
            _events.Debug($"MAP_TRACK_RESET | reason={reason} | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | boss={(_armedBossName.Length > 0 ? _armedBossName : "<unknown>")}");
        ResetInternal(DateTimeOffset.MinValue);
    }

    private void ResetInternal(DateTimeOffset nextAcquireAllowed)
    {
        _mode = TrackMode.Scan;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _dualCandidateFrames = 0;
        _dualPromotionFrames = 0;
        _nextAcquireAllowed = nextAcquireAllowed;
        _nextMapOcrAllowed = DateTimeOffset.MinValue;
        _singleNameBaseline = 0;
        _singleMissingThreshold = 0;
        _singleReturnedThreshold = 0;
        _firstMissing = null;
        _verifyStarted = null;
        _armedContext = BossContextState.IdentityDefault;
        _armedBossId = "";
        _armedBossName = "";
        _armedDetector = "structural-fallback";
    }
}
