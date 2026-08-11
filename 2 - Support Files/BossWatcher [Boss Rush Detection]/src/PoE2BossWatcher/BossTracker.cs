using System.Drawing;

namespace PoE2BossWatcher;

public sealed class BossTracker
{
    private readonly AppConfig _config;
    private readonly EventWriter _events;
    private readonly DebugImageWriter _images;
    private readonly TemporalRunCollapse _runCollapse;

    private BossDefinition? _candidate;
    private int _candidateCount;
    private BossDefinition? _tracked;

    // v0.1.8 separates the timestamp used for timing/backdating from the timestamp used to
    // enforce the confirmation window. A temporal red-run collapse can predate strong template
    // loss, so firstMissing may intentionally be earlier than verifyStarted.
    private DateTimeOffset? _firstMissing;
    private DateTimeOffset? _verifyStarted;
    private DateTimeOffset _nextAcquireAllowed = DateTimeOffset.MinValue;

    private BossNameTemplate? _nameTemplate;
    private double _templateCoverage;
    private double _trackedRedRunAtAcquisition;

    // Fallback retained for the rare case where an OCR match occurs with too few usable
    // name-mask pixels to build a reliable spatial template.
    private double _trackedNameBaseline;
    private double _nameMissingThreshold;
    private double _nameReturnedThreshold;

    public BossTracker(AppConfig config, EventWriter events, DebugImageWriter images)
    {
        _config = config;
        _events = events;
        _images = images;
        _runCollapse = new TemporalRunCollapse(config);
    }

    public BossDefinition? TrackedBoss => _tracked;
    public bool IsMissingWindowActive => _verifyStarted.HasValue;
    public bool CanAcquire(DateTimeOffset now) => _tracked is null && now >= _nextAcquireAllowed;
    public double NameMissingThreshold => _nameMissingThreshold;
    public double NameReturnedThreshold => _nameReturnedThreshold;
    public bool UsingNameTemplate => _nameTemplate is not null;
    public double TemplateCoverage => _templateCoverage;
    public double TrackedRedRunAtAcquisition => _trackedRedRunAtAcquisition;
    public bool RedRunCollapseActive => _runCollapse.IsActive;
    public DateTimeOffset? RedRunCollapseStarted => _runCollapse.CollapseStarted;
    public double RecentRedRunReference => _runCollapse.RecentReference;
    public double RedRunDropRatio => _runCollapse.DropRatio;
    public int RedRunHistoryCount => _runCollapse.HistoryCount;
    public DateTimeOffset? FirstMissing => _firstMissing;
    public DateTimeOffset? VerifyStarted => _verifyStarted;

    public void Observe(DateTimeOffset now, Bitmap raw, Bitmap? processed, OcrRead? ocr, BossMatch? match, BossBarMetrics metrics)
    {
        if (_tracked is null)
        {
            if (!CanAcquire(now)) return;

            if (match is null)
            {
                if (ocr is not null)
                {
                    _candidate = null;
                    _candidateCount = 0;
                }
                return;
            }

            if (_candidate is not null && string.Equals(_candidate.Id, match.Boss.Id, StringComparison.OrdinalIgnoreCase))
                _candidateCount++;
            else
            {
                _candidate = match.Boss;
                _candidateCount = 1;
            }

            if (_candidateCount < _config.AcquireConsecutiveMatches) return;

            ArmKnownBoss(now, raw, processed, match.Boss, metrics, ocr, match.Similarity, emitSeen: true);
            return;
        }

        if (_nameTemplate is not null)
        {
            _templateCoverage = _nameTemplate.MeasureCoverage(raw, _config);
            _runCollapse.Update(now, metrics.HealthRedRunFraction, _templateCoverage);

            // v0.1.13: completion remains strictly UI-presence based. Health fill can reach 0%
            // during immunity/phase mechanics while the boss remains active, so temporal red-run
            // collapse is diagnostics only and can NEVER start or backdate disappearance.
            var missing = _templateCoverage <= _config.TrackedTemplateMissingCoverage;

            if (!_verifyStarted.HasValue)
            {
                if (missing)
                {
                    _verifyStarted = now;
                    _firstMissing = now;

                    _events.Debug($"MISSING_STARTED | boss={_tracked.Id} | presence=template" +
                        $" | coverage={_templateCoverage:F3}" +
                        $" | runDiagnostic={metrics.HealthRedRunFraction:F4}" +
                        $" | recentRunDiagnostic={_runCollapse.RecentReference:F4}" +
                        $" | dropRatioDiagnostic={_runCollapse.DropRatio:F3}" +
                        $" | firstMissing={_firstMissing.Value:O}" +
                        $" | verifyStarted={_verifyStarted.Value:O}");
                }
                return;
            }

            if (_templateCoverage >= _config.TrackedTemplateReturnedCoverage)
            {
                _events.BossReturned(_tracked, now);
                _events.Debug($"MISSING_CANCELLED | boss={_tracked.Id} | presence=template" +
                    $" | coverage={_templateCoverage:F3}" +
                    $" | returnedThreshold={_config.TrackedTemplateReturnedCoverage:F3}");
                _firstMissing = null;
                _verifyStarted = null;
                return;
            }
        }
        else
        {
            _runCollapse.Clear();
            var value = metrics.NameGoldFraction;

            if (!_verifyStarted.HasValue)
            {
                if (value <= _nameMissingThreshold)
                {
                    _firstMissing = now;
                    _verifyStarted = now;
                    _events.Debug($"MISSING_STARTED | boss={_tracked.Id} | presence=name-fallback" +
                        $" | value={value:F4} | threshold={_nameMissingThreshold:F4}" +
                        $" | firstMissing={_firstMissing.Value:O} | verifyStarted={_verifyStarted.Value:O}");
                }
                return;
            }

            if (value >= _nameReturnedThreshold)
            {
                _events.BossReturned(_tracked, now);
                _events.Debug($"MISSING_CANCELLED | boss={_tracked.Id} | presence=name-fallback" +
                    $" | value={value:F4} | returnedThreshold={_nameReturnedThreshold:F4}");
                _firstMissing = null;
                _verifyStarted = null;
                return;
            }
        }

        if ((now - _verifyStarted!.Value).TotalMilliseconds < _config.DisappearConfirmMs) return;

        var goneBoss = _tracked;
        var firstMissing = _firstMissing ?? _verifyStarted.Value;
        var verifyStarted = _verifyStarted.Value;
        if (_config.SaveStateChangeImages) _images.Save("GONE_" + goneBoss.Id, raw, processed);
        _events.BossGone(goneBoss, firstMissing, verifyStarted, now, _config.DisappearConfirmMs);

        ClearTrackedState();
        _nextAcquireAllowed = now.AddMilliseconds(_config.ReacquireCooldownMs);
    }

    public void ArmKnownBoss(
        DateTimeOffset now,
        Bitmap raw,
        Bitmap? processed,
        BossDefinition boss,
        BossBarMetrics metrics,
        OcrRead? ocr = null,
        double similarity = 1.0,
        bool emitSeen = true)
    {
        _tracked = boss;
        _firstMissing = null;
        _verifyStarted = null;
        _candidate = null;
        _candidateCount = 0;
        _nextAcquireAllowed = DateTimeOffset.MinValue;

        _trackedRedRunAtAcquisition = metrics.HealthRedRunFraction;
        _runCollapse.Reset(now, metrics.HealthRedRunFraction);

        var capturedTemplate = BossNameTemplate.Capture(raw, _config);
        if (capturedTemplate.ReferencePixelCount >= _config.TrackedTemplateMinReferencePixels)
        {
            _nameTemplate = capturedTemplate;
            _templateCoverage = 1.0;
        }
        else
        {
            _nameTemplate = null;
            _templateCoverage = 0;
        }

        _trackedNameBaseline = metrics.NameGoldFraction;
        _nameMissingThreshold = Math.Max(
            _config.TrackedNameMissingAbsoluteFraction,
            _trackedNameBaseline * _config.TrackedNameMissingRelativeFraction);
        _nameReturnedThreshold = Math.Max(
            _config.TrackedNameReturnedAbsoluteFraction,
            _trackedNameBaseline * _config.TrackedNameReturnedRelativeFraction);

        if (emitSeen)
            _events.BossSeen(_tracked, now, ocr ?? new OcrRead("", 0), metrics, similarity, BossLane.Single);

        _events.Debug($"TRACK_ARMED | boss={_tracked.Id}" +
            $" | emitSeen={emitSeen}" +
            $" | presence={(_nameTemplate is not null ? "template" : "name-fallback")}" +
            $" | templatePixels={capturedTemplate.ReferencePixelCount}" +
            $" | runAtAcquisition={_trackedRedRunAtAcquisition:F4}" +
            $" | temporalRunLookbackMs={_config.TrackedRedRunLookbackMs}" +
            $" | temporalRunDropRatioDiagnostic={_config.TrackedRedRunCollapseRelativeFraction:F3}" +
            $" | templateMissing={_config.TrackedTemplateMissingCoverage:F3}" +
            $" | nameFallbackMissing={_nameMissingThreshold:F4}");
        if (_config.SaveStateChangeImages) _images.Save((emitSeen ? "SEEN_" : "REARM_") + _tracked.Id, raw, processed);
    }

    public void SuspendCapture()
    {
        // Do not advance disappearance while the game is not capturable.
    }

    public void ResetTracking(string reason)
    {
        if (_tracked is not null) _events.Debug($"TRACK_RESET | boss={_tracked.Id} | reason={reason}");
        ClearTrackedState();
        _nextAcquireAllowed = DateTimeOffset.MinValue;
    }

    private void ClearTrackedState()
    {
        _tracked = null;
        _firstMissing = null;
        _verifyStarted = null;
        _candidate = null;
        _candidateCount = 0;
        _nameTemplate = null;
        _templateCoverage = 0;
        _trackedRedRunAtAcquisition = 0;
        _runCollapse.Clear();
        _trackedNameBaseline = 0;
        _nameMissingThreshold = 0;
        _nameReturnedThreshold = 0;
    }
}
