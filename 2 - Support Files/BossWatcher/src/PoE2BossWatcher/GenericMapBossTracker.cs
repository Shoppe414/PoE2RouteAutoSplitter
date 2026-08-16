using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// Identity-free boss-bar tracker used only while the ASL classifies the current
/// instance as an ordinary endgame map. It deliberately bypasses Tesseract/name
/// matching: a structurally valid PoE2 boss UI arms the encounter, and verified
/// disappearance of the UI emits MAP_GONE.
///
/// Single-boss encounters use only structural UI metrics (health-bar red run plus
/// the gold boss-UI/name band); no OCR, boss-name catalog, or glyph/template matching
/// is performed. Dual encounters are treated as one map-boss objective and complete
/// only when the entire dual/recentered boss UI is gone.
/// </summary>
public sealed class GenericMapBossTracker
{
    private enum TrackMode
    {
        Scan,
        Single,
        Dual
    }

    private readonly AppConfig _config;
    private readonly EventWriter _events;
    private readonly DebugImageWriter _images;

    private TrackMode _mode = TrackMode.Scan;
    private int _singleCandidateFrames;
    private int _singleCenteredCandidateFrames;
    private int _dualCandidateFrames;
    private int _dualPromotionFrames;
    private DateTimeOffset _nextAcquireAllowed = DateTimeOffset.MinValue;
    private double _singleNameBaseline;
    private double _singleMissingThreshold;
    private double _singleReturnedThreshold;
    private DateTimeOffset? _firstMissing;
    private DateTimeOffset? _verifyStarted;
    private BossContextState _armedContext = BossContextState.IdentityDefault;

    public GenericMapBossTracker(AppConfig config, EventWriter events, DebugImageWriter images)
    {
        _config = config;
        _events = events;
        _images = images;
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

        if (metrics.DualSignature)
        {
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            _dualCandidateFrames++;
            if (_dualCandidateFrames >= _config.DualInitialConfirmFrames)
                ArmDual(now, raw, metrics, context, "initial-dual");
            return;
        }

        _dualCandidateFrames = 0;
        if (!IsSingleStructuralCandidate(metrics))
        {
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            return;
        }

        // Reuse the proven structural persistence gate without invoking OCR. A centered
        // gold UI band confirms quickly; unusual layouts can still arm after sustained
        // red-bar + gold-band evidence.
        _singleCandidateFrames++;
        var centeredUi = metrics.CenterNameGoldFraction >= _config.SingleOcrMinCenterNameGoldFraction;
        _singleCenteredCandidateFrames = centeredUi ? _singleCenteredCandidateFrames + 1 : 0;
        var fastConfirmed = _singleCenteredCandidateFrames >= _config.OcrCandidateConsecutiveFrames;
        var persistentConfirmed = _singleCandidateFrames >= _config.SingleOcrWeakCenterConfirmFrames;
        if (!fastConfirmed && !persistentConfirmed) return;
        ArmSingle(now, raw, metrics, context);
    }

    private bool IsSingleStructuralCandidate(BossBarMetrics metrics)
        => metrics.HealthRedRunFraction >= _config.OcrTriggerRedRunFraction
        && metrics.NameGoldFraction >= _config.OcrMinNameGoldPixelFraction;

    private void ArmSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context)
    {
        _mode = TrackMode.Single;
        _armedContext = context;
        _firstMissing = null;
        _verifyStarted = null;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _dualCandidateFrames = 0;
        _dualPromotionFrames = 0;

        _singleNameBaseline = metrics.NameGoldFraction;
        _singleMissingThreshold = Math.Max(
            _config.TrackedNameMissingAbsoluteFraction,
            _singleNameBaseline * _config.TrackedNameMissingRelativeFraction);
        _singleReturnedThreshold = Math.Max(
            _config.TrackedNameReturnedAbsoluteFraction,
            _singleNameBaseline * _config.TrackedNameReturnedRelativeFraction);

        _events.MapBossSeen(now, _armedContext, metrics, "single");
        _events.Debug($"MAP_TRACK_ARMED | layout=single | detector=structural-only | area={_armedContext.AreaId} | level={_armedContext.AreaLevel}" +
            $" | bossNumber={_armedContext.MapBossNumber} | run={metrics.HealthRedRunFraction:F4}" +
            $" | nameGold={metrics.NameGoldFraction:F4}");
        if (_config.SaveStateChangeImages) _images.Save("MAP_SEEN", raw, null);
    }

    private void ArmDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, BossContextState context, string reason)
    {
        _mode = TrackMode.Dual;
        _armedContext = context;
        _firstMissing = null;
        _verifyStarted = null;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _dualCandidateFrames = 0;
        _dualPromotionFrames = 0;

        _events.MapBossSeen(now, _armedContext, metrics, "dual");
        _events.Debug($"MAP_TRACK_ARMED | layout=dual | reason={reason} | area={_armedContext.AreaId}" +
            $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
            $" | leftRun={metrics.LeftHealthRedRunFraction:F4} | rightRun={metrics.RightHealthRedRunFraction:F4}" +
            $" | nameGold={metrics.NameGoldFraction:F4}");
        if (_config.SaveStateChangeImages) _images.Save("MAP_SEEN_DUAL", raw, null);
    }

    private void ObserveSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        // A map encounter can add a second boss after acquisition. Promote to dual only
        // after the same hardened persistence used by the identity tracker so death-effect
        // noise cannot turn a disappearing single bar into a false dual encounter.
        if (!_verifyStarted.HasValue && metrics.DualSignature &&
            metrics.HealthRedRunFraction >= _config.DualAddMinCombinedHealthRunFraction)
        {
            _dualPromotionFrames++;
            if (_dualPromotionFrames >= _config.DualAddConfirmFrames)
            {
                ArmDual(now, raw, metrics, _armedContext, "single-to-dual");
                return;
            }
        }
        else
        {
            _dualPromotionFrames = 0;
        }

        // Structural-only presence tracking. Health fill can reach zero before the UI
        // disappears, so disappearance is keyed to loss of the gold boss-UI/name band.
        // The threshold is learned from this encounter's baseline but never reads or
        // matches the boss name itself.
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
                $" | presence=structural-gold-band | nameGold={metrics.NameGoldFraction:F4}");
            return;
        }

        if (returned)
        {
            _events.Debug($"MAP_MISSING_CANCELLED | layout=single | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}");
            _firstMissing = null;
            _verifyStarted = null;
            return;
        }

        if ((now - _verifyStarted.Value).TotalMilliseconds < _config.DisappearConfirmMs) return;
        CommitGone(now, raw, _config.DisappearConfirmMs, "single");
    }

    private void ObserveDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        // Do not complete when only one lane disappears. Once a dual encounter has
        // been armed, wait until the complete boss-name UI band is absent. This also
        // tolerates the surviving boss recentering into the normal single layout.
        var missing = metrics.NameGoldFraction <= _config.DualBothGoneMaxNameGoldFraction;
        var returned = metrics.NameGoldFraction >= _config.TrackedNameReturnedAbsoluteFraction;

        if (!_verifyStarted.HasValue)
        {
            if (!missing) return;
            _firstMissing = now;
            _verifyStarted = now;
            _events.Debug($"MAP_MISSING_STARTED | layout=dual | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | nameGold={metrics.NameGoldFraction:F4}");
            return;
        }

        if (returned)
        {
            _events.Debug($"MAP_MISSING_CANCELLED | layout=dual | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}" +
                $" | nameGold={metrics.NameGoldFraction:F4}");
            _firstMissing = null;
            _verifyStarted = null;
            return;
        }

        if ((now - _verifyStarted.Value).TotalMilliseconds < _config.DualBothGoneConfirmMs) return;
        CommitGone(now, raw, _config.DualBothGoneConfirmMs, "dual");
    }

    private void CommitGone(DateTimeOffset now, Bitmap raw, int confirmMs, string layout)
    {
        var firstMissing = _firstMissing ?? _verifyStarted ?? now;
        var verifyStarted = _verifyStarted ?? firstMissing;
        if (_config.SaveStateChangeImages) _images.Save("MAP_GONE", raw, null);
        _events.MapBossGone(firstMissing, verifyStarted, now, confirmMs, _armedContext, layout);
        ResetInternal(now.AddMilliseconds(_config.ReacquireCooldownMs));
    }

    public void SuspendCapture()
    {
        // Missing confirmation intentionally does not advance while the game is not capturable.
    }

    public void ResetTracking(string reason)
    {
        if (_mode != TrackMode.Scan)
            _events.Debug($"MAP_TRACK_RESET | reason={reason} | area={_armedContext.AreaId}" +
                $" | level={_armedContext.AreaLevel} | bossNumber={_armedContext.MapBossNumber}");
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
        _singleNameBaseline = 0;
        _singleMissingThreshold = 0;
        _singleReturnedThreshold = 0;
        _firstMissing = null;
        _verifyStarted = null;
        _armedContext = BossContextState.IdentityDefault;
    }
}
