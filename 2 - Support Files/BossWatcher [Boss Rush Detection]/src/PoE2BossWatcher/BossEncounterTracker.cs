using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// v0.1.13 encounter-level state machine. PoE2 uses one fixed-width boss UI container:
/// single encounters use the whole width, while dual encounters split it horizontally into
/// left/right lanes. Layout changes are processed before ordinary single-boss disappearance
/// tracking so SINGLE -> DUAL cannot be mistaken for a boss death.
/// </summary>
public sealed class BossEncounterTracker
{
    private enum Mode
    {
        Scan,
        AcquireSingle,
        SingleBackoff,
        TrackSingle,
        AcquireDual,
        TrackDual,
        ResolveDual
    }

    private readonly AppConfig _config;
    private readonly EventWriter _events;
    private readonly DebugImageWriter _images;
    private readonly OcrService _ocr;
    private readonly BossNameMatcher _matcher;
    private readonly BossTracker _single;

    private Mode _mode = Mode.Scan;
    private DateTimeOffset _nextEncounterAcquireAllowed = DateTimeOffset.MinValue;

    // Generic single-acquisition burst state.
    private int _singleCandidateFrames;
    private int _singleCenteredCandidateFrames;
    private int _singleCandidateLossFrames;
    private DateTimeOffset _singleBurstEnd = DateTimeOffset.MinValue;
    private DateTimeOffset _nextSingleBurstAllowed = DateTimeOffset.MinValue;
    private DateTimeOffset _nextSingleOcr = DateTimeOffset.MinValue;
    private readonly OcrLaneHistory _singleOcrHistory;
    private int _singleFailedOcrCycles;
    private DateTimeOffset _singleLastOcrDiagnostic = DateTimeOffset.MinValue;

    // Dual topology / acquisition state.
    private int _dualPresentFrames;
    private int _dualAbsentFrames;
    private DateTimeOffset? _dualLossCandidateAt;
    private DateTimeOffset _dualBurstEnd = DateTimeOffset.MinValue;
    private DateTimeOffset _nextDualBurstAllowed = DateTimeOffset.MinValue;
    private DateTimeOffset _nextDualOcr = DateTimeOffset.MinValue;
    private BossDefinition? _existingSingleBeforeDual;
    private LaneCandidate? _leftCandidate;
    private LaneCandidate? _rightCandidate;
    // v0.1.13: a successful lane OCR result survives burst boundaries. Its UI template is
    // learned immediately so the pending identity can be retained while only the unresolved
    // lane continues receiving OCR attempts.
    private DualBossTrack? _leftPendingTrack;
    private DualBossTrack? _rightPendingTrack;
    private DateTimeOffset? _leftPendingMissingAt;
    private DateTimeOffset? _rightPendingMissingAt;
    private DateTimeOffset? _dualAcquireSignatureLostAt;
    private DualBossTrack? _leftTrack;
    private DualBossTrack? _rightTrack;
    private readonly OcrLaneHistory _leftOcrHistory;
    private readonly OcrLaneHistory _rightOcrHistory;
    private int _leftFailedOcrCycles;
    private int _rightFailedOcrCycles;
    private DateTimeOffset _leftLastOcrDiagnostic = DateTimeOffset.MinValue;
    private DateTimeOffset _rightLastOcrDiagnostic = DateTimeOffset.MinValue;

    // DUAL -> SINGLE/NONE resolution remains strictly bar-presence based:
    // health-fill percentages are diagnostics only and never identify a completed boss.
    private DateTimeOffset _resolveStarted = DateTimeOffset.MinValue;
    private DateTimeOffset _resolveFirstMissing = DateTimeOffset.MinValue;
    private DateTimeOffset _nextResolveOcr = DateTimeOffset.MinValue;
    private LaneCandidate? _resolveSurvivor;

    // Diagnostics.
    private int _burstAttempts;
    private long _ocrAttempts;
    private string _lastOcr = "-";
    private string _lastMatch = "-";
    private string _lastOcrSource = "-";
    private DateTimeOffset _lastOcrAt = DateTimeOffset.MinValue;

    public BossEncounterTracker(
        AppConfig config,
        EventWriter events,
        DebugImageWriter images,
        OcrService ocr,
        BossNameMatcher matcher)
    {
        _config = config;
        _events = events;
        _images = images;
        _ocr = ocr;
        _matcher = matcher;
        _single = new BossTracker(config, events, images);
        _singleOcrHistory = new OcrLaneHistory(config.OcrTemporalFrameCount);
        _leftOcrHistory = new OcrLaneHistory(config.OcrTemporalFrameCount);
        _rightOcrHistory = new OcrLaneHistory(config.OcrTemporalFrameCount);
    }

    public string StateLabel => _mode switch
    {
        Mode.Scan => "SCAN",
        Mode.AcquireSingle => "ACQUIRE",
        Mode.SingleBackoff => "BACKOFF",
        Mode.TrackSingle => _single.IsMissingWindowActive ? "VERIFY_GONE" : "TRACK",
        Mode.AcquireDual => DateTimeOffset.Now < _nextDualBurstAllowed ? "DUAL_BACKOFF" : "DUAL_ACQUIRE",
        Mode.TrackDual => "DUAL_TRACK",
        Mode.ResolveDual => "DUAL_RESOLVE",
        _ => "SCAN"
    };

    public bool IsDualMode => _mode is Mode.AcquireDual or Mode.TrackDual or Mode.ResolveDual;
    public bool IsTrackingAny => _single.TrackedBoss is not null || _leftTrack is not null || _rightTrack is not null;
    public long OcrAttempts => _ocrAttempts;
    public int BurstAttempts => _burstAttempts;
    public DateTimeOffset LastOcrAt => _lastOcrAt;
    public string LastOcr => _lastOcr;
    public string LastMatch => _lastMatch;
    public string LastOcrSource => _lastOcrSource;

    public string TrackedSummary
    {
        get
        {
            if (_mode == Mode.TrackSingle && _single.TrackedBoss is not null)
                return _single.TrackedBoss.Name;
            if (_leftTrack is not null || _rightTrack is not null)
                return $"L:{_leftTrack?.Boss.Name ?? "-"} R:{_rightTrack?.Boss.Name ?? "-"}";
            if (_mode == Mode.AcquireDual && (_leftCandidate is not null || _rightCandidate is not null))
                return $"pending L:{_leftCandidate?.Match.Boss.Name ?? "-"} R:{_rightCandidate?.Match.Boss.Name ?? "-"}";
            if (_existingSingleBeforeDual is not null)
                return $"+dual from {_existingSingleBeforeDual.Name}";
            return "-";
        }
    }

    public double SingleTemplateCoverage => _single.TemplateCoverage;
    public bool SingleUsingTemplate => _single.UsingNameTemplate;
    public double SingleRecentRunReference => _single.RecentRedRunReference;
    public double SingleRunDropRatio => _single.RedRunDropRatio;
    public bool SingleRunCollapse => _single.RedRunCollapseActive;
    public double LeftTemplateCoverage => _leftTrack?.Coverage ?? _leftPendingTrack?.Coverage ?? 0;
    public double RightTemplateCoverage => _rightTrack?.Coverage ?? _rightPendingTrack?.Coverage ?? 0;

    public void Observe(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        switch (_mode)
        {
            case Mode.TrackSingle:
                ObserveTrackedSingle(now, raw, metrics);
                return;
            case Mode.AcquireDual:
                ObserveDualAcquisition(now, raw, metrics);
                return;
            case Mode.TrackDual:
                ObserveTrackedDual(now, raw, metrics);
                return;
            case Mode.ResolveDual:
                ObserveDualResolution(now, raw, metrics);
                return;
            case Mode.SingleBackoff:
                if (TryPromoteIdleToDual(now, raw, metrics)) return;
                if (now >= _nextSingleBurstAllowed) _mode = Mode.Scan;
                return;
            case Mode.AcquireSingle:
                if (TryPromoteIdleToDual(now, raw, metrics)) return;
                ObserveSingleAcquisition(now, raw, metrics);
                return;
            default:
                if (TryPromoteIdleToDual(now, raw, metrics)) return;
                ObserveScan(now, raw, metrics);
                return;
        }
    }

    private void ObserveScan(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        if (now < _nextEncounterAcquireAllowed || !_single.CanAcquire(now)) return;

        var baseCandidate = IsSingleOcrBaseStructuralCandidate(metrics);
        if (!baseCandidate)
        {
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            return;
        }

        _singleCandidateFrames++;
        var centeredName = metrics.CenterNameGoldFraction >= _config.SingleOcrMinCenterNameGoldFraction;
        _singleCenteredCandidateFrames = centeredName ? _singleCenteredCandidateFrames + 1 : 0;
        var fastConfirmed = _singleCenteredCandidateFrames >= _config.OcrCandidateConsecutiveFrames;
        var persistentFallbackConfirmed = _singleCandidateFrames >= _config.SingleOcrWeakCenterConfirmFrames;
        if (!fastConfirmed && !persistentFallbackConfirmed) return;
        if (now < _nextSingleBurstAllowed) return;

        _mode = Mode.AcquireSingle;
        _singleBurstEnd = now.AddMilliseconds(_config.OcrBurstDurationMs);
        _nextSingleOcr = now;
        _singleCandidateLossFrames = 0;
        _singleFailedOcrCycles = 0;
        _singleOcrHistory.Clear();
        _burstAttempts = 0;
        _events.Debug($"OCR_BURST_START | layout=single | redRun={metrics.HealthRedRunFraction:F4}" +
            $" | nameGold={metrics.NameGoldFraction:F4} | centerName={metrics.CenterNameGoldFraction:F4}" +
            $" | idleGate={(fastConfirmed ? "centered-fast" : "weak-center-persistent")}" +
            $" | candidateFrames={_singleCandidateFrames}" +
            $" | centeredFrames={_singleCenteredCandidateFrames}");
        ObserveSingleAcquisition(now, raw, metrics);
    }

    private bool IsSingleOcrBaseStructuralCandidate(BossBarMetrics metrics)
        => metrics.HealthRedRunFraction >= _config.OcrTriggerRedRunFraction
        && metrics.NameGoldFraction >= _config.OcrMinNameGoldPixelFraction;

    private void ObserveSingleAcquisition(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        if (_mode != Mode.AcquireSingle) return;

        // v0.1.14: do not spend the remainder of a 1.6 s OCR burst on a transient scan candidate.
        // The acquisition gate is intentionally separate from tracked-boss disappearance logic.
        // A tracked boss may have 0% health; this gate only decides whether idle OCR should run.
        if (!IsSingleOcrBaseStructuralCandidate(metrics))
        {
            _singleCandidateLossFrames++;
            if (_singleCandidateLossFrames >= _config.SingleOcrCandidateLossFrames)
            {
                _events.Debug($"SINGLE_ACQUIRE_CANCELLED | reason=structural candidate lost" +
                    $" | lossFrames={_singleCandidateLossFrames}" +
                    $" | redRun={metrics.HealthRedRunFraction:F4}" +
                    $" | nameGold={metrics.NameGoldFraction:F4}" +
                    $" | centerName={metrics.CenterNameGoldFraction:F4}" +
                    $" | attempts={_burstAttempts}");
                _mode = Mode.SingleBackoff;
                _singleCandidateFrames = 0;
                _singleCenteredCandidateFrames = 0;
                _singleCandidateLossFrames = 0;
                _singleFailedOcrCycles = 0;
                _nextSingleBurstAllowed = now.AddMilliseconds(_config.OcrRetryCooldownMs);
                _singleOcrHistory.Clear();
            }
            return;
        }
        _singleCandidateLossFrames = 0;

        // Build OCR history from captured frames rather than OCR calls. The broad mask is allowed
        // to recover live-rendered antialiasing that can fall outside the calibrated gold range.
        _singleOcrHistory.AddFrame(raw, _config, BossLane.Single);

        if (now >= _singleBurstEnd)
        {
            _mode = Mode.SingleBackoff;
            _singleCandidateFrames = 0;
            _singleCenteredCandidateFrames = 0;
            _singleCandidateLossFrames = 0;
            _nextSingleBurstAllowed = now.AddMilliseconds(_config.OcrRetryCooldownMs);
            _events.Debug($"OCR_BURST_END | layout=single | matched=false | attempts={_burstAttempts}" +
                $" | failedCycles={_singleFailedOcrCycles}");
            // Failed-cycle count is per burst. A later, genuinely new candidate gets the calibrated
            // fresh-frame fallback sequence again instead of inheriting an old idle-scan count.
            _singleFailedOcrCycles = 0;
            _singleOcrHistory.Clear();
            return;
        }

        if (now < _nextSingleOcr)
            return;

        _nextSingleOcr = now.AddMilliseconds(_config.OcrAcquisitionCycleMs);
        var result = ReadAcquisitionCycle(BossLane.Single, now, _singleOcrHistory, _singleFailedOcrCycles);

        if (result.Match is null)
        {
            _singleFailedOcrCycles++;
            MaybeSaveOcrFailure(now, raw, BossLane.Single, _singleOcrHistory, _singleFailedOcrCycles);
            _single.Observe(now, raw, null, result.Read, null, metrics);
            return;
        }

        _singleFailedOcrCycles = 0;
        // Recompute diagnostics so SEEN contains the same full metrics as prior versions.
        var fullMetrics = BossBarMetrics.Analyze(raw, _config, includeDiagnostics: true);
        _single.Observe(now, raw, null, result.Read, result.Match, fullMetrics);
        if (_single.TrackedBoss is null) return;

        _events.Debug($"OCR_BURST_END | layout=single | matched=true | source={result.Source}" +
            $" | attempts={_burstAttempts}");
        _mode = Mode.TrackSingle;
        ResetSingleAcquisitionTransient();
    }

    private void ObserveTrackedSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        // Layout topology still has priority over disappearance for a REAL SINGLE -> DUAL change,
        // but v0.1.13 retains the stronger evidence requirement before suspending single-boss tracking. The death
        // animation in the v0.1.9 Iktab/Ekbab test briefly resembled a dual name layout while the
        // coherent red health structure had already collapsed to ~0.3%. A newly-added boss should
        // instead produce a persistent dual signature with a substantial half-width health run.
        var strongDualAddition =
            !_single.IsMissingWindowActive &&
            metrics.DualSignature &&
            metrics.HealthRedRunFraction >= _config.DualAddMinCombinedHealthRunFraction;

        if (strongDualAddition)
        {
            _dualPresentFrames++;
            if (_dualPresentFrames == 1)
            {
                _events.Debug($"DUAL_ADD_CANDIDATE | boss={_single.TrackedBoss?.Id ?? "-"}" +
                    $" | run={metrics.HealthRedRunFraction:F4}" +
                    $" | requiredRun={_config.DualAddMinCombinedHealthRunFraction:F4}" +
                    $" | confirmFrames={_config.DualAddConfirmFrames}");
            }

            if (_dualPresentFrames >= _config.DualAddConfirmFrames)
            {
                BeginDualAcquisition(now, _single.TrackedBoss);
                ObserveDualAcquisition(now, raw, metrics);
            }
            return;
        }

        if (_dualPresentFrames > 0)
        {
            _events.Debug($"DUAL_ADD_CANDIDATE_CANCELLED | frames={_dualPresentFrames}" +
                $" | dualSignature={metrics.DualSignature}" +
                $" | run={metrics.HealthRedRunFraction:F4}" +
                $" | missingWindow={_single.IsMissingWindowActive}");
        }
        _dualPresentFrames = 0;

        // Weak/transient dual-like frames are allowed to continue through the normal single-boss
        // disappearance detector instead of hijacking the encounter into DUAL_ACQUIRE.
        _single.Observe(now, raw, null, null, null, metrics);
        if (_single.TrackedBoss is null)
        {
            _mode = Mode.Scan;
            _nextEncounterAcquireAllowed = now.AddMilliseconds(_config.ReacquireCooldownMs);
            ResetSingleAcquisitionTransient();
        }
    }

    private bool TryPromoteIdleToDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        if (now < _nextEncounterAcquireAllowed) return false;

        // v0.1.14: initial dual OCR is more conservative than topology measurement itself.
        // A strong coherent health run uses the fast confirmation window. If health fill is low
        // (including immunity/phase cases), a persistent dual-name topology can still acquire via
        // the longer fallback window. Health therefore changes latency, never eligibility/death.
        if (!metrics.DualSignature)
        {
            _dualPresentFrames = 0;
            return false;
        }

        _dualPresentFrames++;
        var strongHealth = metrics.HealthRedRunFraction >= _config.DualInitialMinCombinedHealthRunFraction;
        var requiredFrames = strongHealth
            ? _config.DualInitialConfirmFrames
            : _config.DualInitialLowHealthConfirmFrames;
        if (_dualPresentFrames < requiredFrames) return true;

        BeginDualAcquisition(now, existingSingle: null);
        ObserveDualAcquisition(now, raw, metrics);
        return true;
    }

    private void BeginDualAcquisition(DateTimeOffset now, BossDefinition? existingSingle)
    {
        _mode = Mode.AcquireDual;
        _existingSingleBeforeDual = existingSingle;
        _leftCandidate = null;
        _rightCandidate = null;
        _leftPendingTrack = null;
        _rightPendingTrack = null;
        _leftPendingMissingAt = null;
        _rightPendingMissingAt = null;
        _dualAcquireSignatureLostAt = null;
        _leftOcrHistory.Clear();
        _rightOcrHistory.Clear();
        _leftFailedOcrCycles = 0;
        _rightFailedOcrCycles = 0;
        _leftLastOcrDiagnostic = DateTimeOffset.MinValue;
        _rightLastOcrDiagnostic = DateTimeOffset.MinValue;
        _dualAbsentFrames = 0;
        _dualBurstEnd = now.AddMilliseconds(_config.OcrBurstDurationMs);
        _nextDualOcr = now;
        _nextDualBurstAllowed = DateTimeOffset.MinValue;
        _burstAttempts = 0;
        _events.Debug($"LAYOUT_CHANGE | to=dual | reason={(existingSingle is null ? "DUAL_INITIAL" : "BOSS_ADDED")}" +
            $" | existing={(existingSingle?.Id ?? "-")}");
        _events.Debug($"OCR_BURST_START | layout=dual | lanes=left,right | persistentLaneAcquisition=true" +
            $" | cycleMs={_config.OcrAcquisitionCycleMs}" +
            $" | sources=gold-single,broad-single,temporal-fallback");
    }

    private void ObserveDualAcquisition(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        // v0.1.13: collect a fresh native-resolution gold+broad mask from every captured frame for
        // each unresolved lane. Temporal fallback therefore represents different render samples,
        // not repeated OCR of one image. Resolved lanes stop collecting and consuming OCR entirely.
        if (_leftCandidate is null) _leftOcrHistory.AddFrame(raw, _config, BossLane.Left);
        if (_rightCandidate is null) _rightOcrHistory.AddFrame(raw, _config, BossLane.Right);

        // Keep measuring any lane that has already been positively identified. This lets a good
        // OCR result survive burst boundaries while still expiring it if that UI lane genuinely
        // disappears for the configured grace period.
        MeasurePendingDualTemplates(raw);
        ValidatePendingLanePresence(now, metrics, BossLane.Left);
        ValidatePendingLanePresence(now, metrics, BossLane.Right);

        if (!metrics.DualSignature)
        {
            _dualAbsentFrames++;
            if (_dualAbsentFrames < _config.DualLayoutConfirmFrames) return;

            if (HasPendingDualCandidate())
            {
                if (_dualAcquireSignatureLostAt is null)
                {
                    _dualAcquireSignatureLostAt = now;
                    _events.Debug($"PENDING_LANE_RETAINED | reason=dual signature temporarily absent" +
                        $" | left={_leftCandidate?.Match.Boss.Id ?? "-"}" +
                        $" | right={_rightCandidate?.Match.Boss.Id ?? "-"}" +
                        $" | graceMs={_config.DualAcquirePartialGraceMs}");
                }

                if ((now - _dualAcquireSignatureLostAt.Value).TotalMilliseconds < _config.DualAcquirePartialGraceMs)
                    return;

                _events.Debug($"LAYOUT_CHANGE_CANCELLED | target=dual | reason=dual signature absent beyond partial-acquire grace" +
                    $" | left={_leftCandidate?.Match.Boss.Id ?? "-"}" +
                    $" | right={_rightCandidate?.Match.Boss.Id ?? "-"}" +
                    $" | absentMs={(now - _dualAcquireSignatureLostAt.Value).TotalMilliseconds:F1}");
            }
            else
            {
                _events.Debug("LAYOUT_CHANGE_CANCELLED | target=dual | reason=dual signature disappeared before reconciliation");
            }

            var existing = _existingSingleBeforeDual;
            var hadExisting = existing is not null && _single.TrackedBoss is not null;
            ClearDualAcquisitionTransient(clearExisting: true);
            if (hadExisting && existing is not null)
            {
                // Relearn the full-width single template after a cancelled layout transition.
                _single.ArmKnownBoss(now, raw, null, existing, metrics, emitSeen: false);
                _mode = Mode.TrackSingle;
            }
            else
            {
                _mode = Mode.Scan;
            }
            return;
        }

        if (_dualAcquireSignatureLostAt is not null)
        {
            _events.Debug($"PENDING_LANE_RETAINED | reason=dual signature returned" +
                $" | left={_leftCandidate?.Match.Boss.Id ?? "-"}" +
                $" | right={_rightCandidate?.Match.Boss.Id ?? "-"}" +
                $" | lossMs={(now - _dualAcquireSignatureLostAt.Value).TotalMilliseconds:F1}");
        }
        _dualAcquireSignatureLostAt = null;
        _dualAbsentFrames = 0;

        // If both lane identities were collected across previous OCR cycles/bursts, reconcile now
        // without spending another OCR call.
        if (TryResolveDuplicatePendingCandidates() && _leftCandidate is not null && _rightCandidate is not null)
        {
            FinalizeDualAcquisition(now, raw, metrics);
            return;
        }

        if (now < _nextDualBurstAllowed) return;
        if (now >= _dualBurstEnd)
        {
            _events.Debug($"OCR_BURST_END | layout=dual | matched={(_leftCandidate is not null && _rightCandidate is not null).ToString().ToLowerInvariant()}" +
                $" | attempts={_burstAttempts}" +
                $" | left={_leftCandidate?.Match.Boss.Id ?? "-"}" +
                $" | right={_rightCandidate?.Match.Boss.Id ?? "-"}");
            _events.Debug($"DUAL_ACQUIRE_PROGRESS | left={_leftCandidate?.Match.Boss.Id ?? "unresolved"}" +
                $" | right={_rightCandidate?.Match.Boss.Id ?? "unresolved"}" +
                $" | failedCycles=L{_leftFailedOcrCycles}/R{_rightFailedOcrCycles}" +
                $" | next=retry-after-backoff");
            _nextDualBurstAllowed = now.AddMilliseconds(_config.OcrRetryCooldownMs);
            _dualBurstEnd = _nextDualBurstAllowed.AddMilliseconds(_config.OcrBurstDurationMs);
            _nextDualOcr = _nextDualBurstAllowed;
            _burstAttempts = 0;
            return;
        }

        if (now < _nextDualOcr) return;
        _nextDualOcr = now.AddMilliseconds(_config.OcrAcquisitionCycleMs);

        // v0.1.13: only unresolved lanes consume OCR. A positively identified lane remains armed
        // using its learned template while all subsequent OCR budget is directed to the other side.
        if (_leftCandidate is null)
        {
            var leftResult = ReadAcquisitionCycle(BossLane.Left, now, _leftOcrHistory, _leftFailedOcrCycles);
            if (leftResult.Match is not null)
            {
                _leftFailedOcrCycles = 0;
                SetPendingLaneCandidate(now, raw, BossLane.Left,
                    new LaneCandidate(leftResult.Read, leftResult.Match, BossLane.Left, leftResult.Source));
            }
            else
            {
                _leftFailedOcrCycles++;
                MaybeSaveOcrFailure(now, raw, BossLane.Left, _leftOcrHistory, _leftFailedOcrCycles);
            }
        }

        if (_rightCandidate is null)
        {
            var rightResult = ReadAcquisitionCycle(BossLane.Right, now, _rightOcrHistory, _rightFailedOcrCycles);
            if (rightResult.Match is not null)
            {
                _rightFailedOcrCycles = 0;
                SetPendingLaneCandidate(now, raw, BossLane.Right,
                    new LaneCandidate(rightResult.Read, rightResult.Match, BossLane.Right, rightResult.Source));
            }
            else
            {
                _rightFailedOcrCycles++;
                MaybeSaveOcrFailure(now, raw, BossLane.Right, _rightOcrHistory, _rightFailedOcrCycles);
            }
        }

        if (!TryResolveDuplicatePendingCandidates()) return;
        if (_leftCandidate is null || _rightCandidate is null) return;

        FinalizeDualAcquisition(now, raw, metrics);
    }

    private void SetPendingLaneCandidate(DateTimeOffset now, Bitmap raw, BossLane lane, LaneCandidate incoming)
    {
        LaneCandidate? current = lane == BossLane.Left ? _leftCandidate : _rightCandidate;
        if (current is not null && current.Match.Similarity > incoming.Match.Similarity)
            return;

        var pendingTrack = DualBossTrack.Create(incoming.Match.Boss, lane, raw, _config);
        if (lane == BossLane.Left)
        {
            _leftCandidate = incoming;
            _leftPendingTrack = pendingTrack;
            _leftPendingMissingAt = null;
            _leftFailedOcrCycles = 0;
            _leftOcrHistory.Clear();
        }
        else
        {
            _rightCandidate = incoming;
            _rightPendingTrack = pendingTrack;
            _rightPendingMissingAt = null;
            _rightFailedOcrCycles = 0;
            _rightOcrHistory.Clear();
        }

        _events.Debug($"PENDING_LANE_MATCH | lane={lane.ToString().ToLowerInvariant()}" +
            $" | boss={incoming.Match.Boss.Id}" +
            $" | score={incoming.Match.Similarity:F3}" +
            $" | source={incoming.Source}" +
            $" | templatePixels={pendingTrack.ReferencePixels}" +
            $" | left={_leftCandidate?.Match.Boss.Id ?? "unresolved"}" +
            $" | right={_rightCandidate?.Match.Boss.Id ?? "unresolved"}");
    }

    private bool TryResolveDuplicatePendingCandidates()
    {
        if (_leftCandidate is null || _rightCandidate is null) return true;
        if (!string.Equals(_leftCandidate.Match.Boss.Id, _rightCandidate.Match.Boss.Id, StringComparison.OrdinalIgnoreCase))
            return true;

        _events.Debug($"DUAL_RECONCILE_WAIT | duplicateBoss={_leftCandidate.Match.Boss.Id}" +
            $" | leftScore={_leftCandidate.Match.Similarity:F3} | rightScore={_rightCandidate.Match.Similarity:F3}");

        // Keep the stronger side and reopen only the weaker side for OCR.
        if (_leftCandidate.Match.Similarity >= _rightCandidate.Match.Similarity)
            ClearPendingLane(BossLane.Right, "duplicate weaker lane");
        else
            ClearPendingLane(BossLane.Left, "duplicate weaker lane");
        return false;
    }

    private void MeasurePendingDualTemplates(Bitmap raw)
    {
        if (_leftPendingTrack is not null) _leftPendingTrack.Measure(raw, _config);
        if (_rightPendingTrack is not null) _rightPendingTrack.Measure(raw, _config);
    }

    private void ValidatePendingLanePresence(DateTimeOffset now, BossBarMetrics metrics, BossLane lane)
    {
        var candidate = lane == BossLane.Left ? _leftCandidate : _rightCandidate;
        var track = lane == BossLane.Left ? _leftPendingTrack : _rightPendingTrack;
        if (candidate is null || track is null) return;

        var laneName = lane == BossLane.Left ? metrics.LeftLaneNameGoldFraction : metrics.RightLaneNameGoldFraction;
        var clearlyPresent = track.Coverage >= _config.TrackedTemplateReturnedCoverage ||
            laneName >= _config.DualLayoutMinLaneNameGoldFraction;
        if (clearlyPresent)
        {
            if (lane == BossLane.Left) _leftPendingMissingAt = null;
            else _rightPendingMissingAt = null;
            return;
        }

        var clearlyMissing = track.Coverage <= _config.TrackedTemplateMissingCoverage &&
            laneName <= _config.DualLaneGoneMaxNameGoldFraction;
        if (!clearlyMissing) return;

        DateTimeOffset? missingAt = lane == BossLane.Left ? _leftPendingMissingAt : _rightPendingMissingAt;
        if (missingAt is null)
        {
            if (lane == BossLane.Left) _leftPendingMissingAt = now;
            else _rightPendingMissingAt = now;
            _events.Debug($"PENDING_LANE_RETAINED | lane={lane.ToString().ToLowerInvariant()}" +
                $" | boss={candidate.Match.Boss.Id} | reason=template temporarily missing" +
                $" | coverage={track.Coverage:F3} | laneName={laneName:F4}" +
                $" | graceMs={_config.DualAcquirePartialGraceMs}");
            return;
        }

        if ((now - missingAt.Value).TotalMilliseconds < _config.DualAcquirePartialGraceMs) return;
        ClearPendingLane(lane, $"template absent for {(now - missingAt.Value).TotalMilliseconds:F1}ms");
    }

    private bool HasPendingDualCandidate() => _leftCandidate is not null || _rightCandidate is not null;

    private void ClearPendingLane(BossLane lane, string reason)
    {
        var bossId = lane == BossLane.Left ? _leftCandidate?.Match.Boss.Id : _rightCandidate?.Match.Boss.Id;
        if (lane == BossLane.Left)
        {
            _leftCandidate = null;
            _leftPendingTrack = null;
            _leftPendingMissingAt = null;
            _leftFailedOcrCycles = 0;
            _leftOcrHistory.Clear();
        }
        else
        {
            _rightCandidate = null;
            _rightPendingTrack = null;
            _rightPendingMissingAt = null;
            _rightFailedOcrCycles = 0;
            _rightOcrHistory.Clear();
        }

        if (bossId is not null)
            _events.Debug($"PENDING_LANE_LOST | lane={lane.ToString().ToLowerInvariant()} | boss={bossId} | reason={reason}");
    }

    private void FinalizeDualAcquisition(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        if (_leftCandidate is null || _rightCandidate is null) return;

        var leftBoss = _leftCandidate.Match.Boss;
        var rightBoss = _rightCandidate.Match.Boss;
        var existingId = _existingSingleBeforeDual?.Id;

        // Relearn both final tracking templates from the current reconciled frame. Pending
        // templates are acquisition confidence only; refreshing here avoids carrying an older
        // acquisition-frame mask into long-term tracking.
        _leftTrack = DualBossTrack.Create(leftBoss, BossLane.Left, raw, _config);
        _rightTrack = DualBossTrack.Create(rightBoss, BossLane.Right, raw, _config);
        var eventMetrics = BossBarMetrics.Analyze(raw, _config, includeDiagnostics: true);

        if (existingId is null || !string.Equals(existingId, leftBoss.Id, StringComparison.OrdinalIgnoreCase))
            _events.BossSeen(leftBoss, now, _leftCandidate.Read, eventMetrics, _leftCandidate.Match.Similarity, BossLane.Left);
        if (existingId is null || !string.Equals(existingId, rightBoss.Id, StringComparison.OrdinalIgnoreCase))
            _events.BossSeen(rightBoss, now, _rightCandidate.Read, eventMetrics, _rightCandidate.Match.Similarity, BossLane.Right);

        _events.Debug($"DUAL_RECONCILED | left={leftBoss.Id} | right={rightBoss.Id}" +
            $" | leftSource={_leftCandidate.Source} | rightSource={_rightCandidate.Source}" +
            $" | existingBefore={existingId ?? "-"}" +
            $" | leftTemplatePixels={_leftTrack.ReferencePixels}" +
            $" | rightTemplatePixels={_rightTrack.ReferencePixels}");

        if (_existingSingleBeforeDual is not null)
            _single.ResetTracking("layout SINGLE->DUAL reconciled");

        if (_config.SaveStateChangeImages)
            _images.Save($"DUAL_{leftBoss.Id}_{rightBoss.Id}", raw, null);

        _mode = Mode.TrackDual;
        ClearDualAcquisitionTransient(clearExisting: true);
        _dualPresentFrames = 0;
        _dualAbsentFrames = 0;
        _dualLossCandidateAt = null;
    }

    private void ObserveTrackedDual(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        MeasureDualTemplates(raw);

        // v0.1.13 can continue tracking one lane if PoE2 removes one dual bar without immediately
        // recentering the survivor. Completion still depends only on the remaining UI template.
        var activeLaneCount = (_leftTrack is not null ? 1 : 0) + (_rightTrack is not null ? 1 : 0);
        if (activeLaneCount == 1)
        {
            var remaining = _leftTrack ?? _rightTrack!;

            if (IsSingleTopologyPresent(metrics))
            {
                var survivorBoss = remaining.Boss;
                _events.Debug($"DUAL_SURVIVOR_RECENTERED | boss={survivorBoss.Id}" +
                    $" | oldLane={remaining.Lane.ToString().ToLowerInvariant()}");
                ClearDualTracks();
                _single.ArmKnownBoss(now, raw, null, survivorBoss, metrics, emitSeen: false);
                _mode = Mode.TrackSingle;
                ResetSingleAcquisitionTransient();
                return;
            }

            if (remaining.Coverage >= _config.TrackedTemplateReturnedCoverage)
            {
                _dualAbsentFrames = 0;
                _dualLossCandidateAt = null;
                return;
            }

            if (remaining.Coverage > _config.TrackedTemplateMissingCoverage)
                return;

            if (_dualAbsentFrames == 0) _dualLossCandidateAt = now;
            _dualAbsentFrames++;
            if (_dualAbsentFrames < _config.DualLayoutConfirmFrames) return;

            _mode = Mode.ResolveDual;
            _resolveFirstMissing = _dualLossCandidateAt ?? now;
            _resolveStarted = now;
            _nextResolveOcr = now;
            _resolveSurvivor = null;
            _burstAttempts = 0;
            _events.Debug($"LAYOUT_CHANGE | from=single-dual-lane | to=none-or-recenter | reason=BAR_UI_MISSING_CANDIDATE" +
                $" | boss={remaining.Boss.Id} | lane={remaining.Lane.ToString().ToLowerInvariant()}" +
                $" | firstMissing={_resolveFirstMissing:O}" +
                $" | coverage={remaining.Coverage:F3}");
            return;
        }

        // Once a dual encounter is established, topology persistence uses the two name anchors
        // without requiring either red-health lane to remain above the acquisition threshold.
        // This prevents two near-dead bosses from looking like DUAL -> SINGLE merely because
        // both health fills have become very short.
        if (metrics.DualNameSignature)
        {
            _dualAbsentFrames = 0;
            _dualLossCandidateAt = null;
            return;
        }

        if (_dualAbsentFrames == 0)
            _dualLossCandidateAt = now;
        _dualAbsentFrames++;

        if (_dualAbsentFrames < _config.DualLayoutConfirmFrames) return;

        _mode = Mode.ResolveDual;
        _resolveFirstMissing = _dualLossCandidateAt ?? now;
        _resolveStarted = now;
        _nextResolveOcr = now;
        _resolveSurvivor = null;
        _burstAttempts = 0;
        var leftCoverage = _leftTrack?.Coverage ?? 0;
        var rightCoverage = _rightTrack?.Coverage ?? 0;
        _events.Debug($"LAYOUT_CHANGE | from=dual | to=single-or-none | reason=BOSS_REMOVED_CANDIDATE" +
            $" | firstMissing={_resolveFirstMissing:O}" +
            $" | leftCoverage={leftCoverage:F3} | rightCoverage={rightCoverage:F3}" +
            $" | laneRunDiagnostic=L{metrics.LeftHealthRedRunFraction:F3}/R{metrics.RightHealthRedRunFraction:F3}");
    }

    private void ObserveDualResolution(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics)
    {
        MeasureDualTemplates(raw);

        var activeLaneCount = (_leftTrack is not null ? 1 : 0) + (_rightTrack is not null ? 1 : 0);
        if (activeLaneCount == 1)
        {
            var remaining = _leftTrack ?? _rightTrack!;

            if (IsSingleTopologyPresent(metrics))
            {
                var survivorBoss = remaining.Boss;
                _events.Debug($"DUAL_SURVIVOR_RECENTERED | boss={survivorBoss.Id}" +
                    $" | duringResolve=true | elapsedMs={(now - _resolveStarted).TotalMilliseconds:F1}");
                ClearDualTracks();
                _single.ArmKnownBoss(now, raw, null, survivorBoss, metrics, emitSeen: false);
                _mode = Mode.TrackSingle;
                ResetSingleAcquisitionTransient();
                return;
            }

            if (remaining.Coverage >= _config.TrackedTemplateReturnedCoverage)
            {
                _events.Debug($"DUAL_RESOLVE_CANCELLED | reason=remaining lane bar returned" +
                    $" | boss={remaining.Boss.Id} | coverage={remaining.Coverage:F3}");
                _mode = Mode.TrackDual;
                _dualAbsentFrames = 0;
                _dualLossCandidateAt = null;
                return;
            }

            var remainingLaneName = remaining.Lane == BossLane.Left
                ? metrics.LeftLaneNameGoldFraction
                : metrics.RightLaneNameGoldFraction;
            var noBossNameUi = metrics.NameGoldFraction <= _config.DualBothGoneMaxNameGoldFraction;
            if (remaining.Coverage <= _config.TrackedTemplateMissingCoverage &&
                remainingLaneName <= _config.DualLaneGoneMaxNameGoldFraction &&
                noBossNameUi &&
                (now - _resolveFirstMissing).TotalMilliseconds >= _config.DualRemovalConfirmMs)
            {
                if (_config.SaveStateChangeImages)
                    _images.Save($"GONE_{remaining.Boss.Id}_FROM_DUAL_LANE", raw, null);
                _events.Debug($"DUAL_REMAINING_GONE | boss={remaining.Boss.Id}" +
                    $" | evidence=bar-ui-absent | firstMissing={_resolveFirstMissing:O}");
                _events.BossGone(remaining.Boss, _resolveFirstMissing, _resolveStarted, now, _config.DualRemovalConfirmMs);
                ClearDualTracks();
                _single.ResetTracking("remaining dual lane gone");
                _mode = Mode.Scan;
                _nextEncounterAcquireAllowed = now.AddMilliseconds(_config.ReacquireCooldownMs);
                _nextSingleBurstAllowed = _nextEncounterAcquireAllowed;
            }
            return;
        }

        // A restored dual name topology means the layout transition was transient. Both bars are
        // still present, regardless of either health-fill percentage.
        if (metrics.DualNameSignature)
        {
            _events.Debug($"DUAL_RESOLVE_CANCELLED | reason=dual layout returned" +
                $" | elapsedMs={(now - _resolveStarted).TotalMilliseconds:F1}");
            _mode = Mode.TrackDual;
            _dualAbsentFrames = 0;
            _dualLossCandidateAt = null;
            _resolveSurvivor = null;
            return;
        }

        var elapsedMs = (now - _resolveStarted).TotalMilliseconds;
        var leftMissing = _leftTrack is null ||
            (_leftTrack.Coverage <= _config.TrackedTemplateMissingCoverage &&
             metrics.LeftLaneNameGoldFraction <= _config.DualLaneGoneMaxNameGoldFraction);
        var rightMissing = _rightTrack is null ||
            (_rightTrack.Coverage <= _config.TrackedTemplateMissingCoverage &&
             metrics.RightLaneNameGoldFraction <= _config.DualLaneGoneMaxNameGoldFraction);
        var leftPresent = _leftTrack is not null &&
            _leftTrack.Coverage >= _config.TrackedTemplateReturnedCoverage &&
            metrics.LeftLaneNameGoldFraction >= _config.DualLayoutMinLaneNameGoldFraction;
        var rightPresent = _rightTrack is not null &&
            _rightTrack.Coverage >= _config.TrackedTemplateReturnedCoverage &&
            metrics.RightLaneNameGoldFraction >= _config.DualLayoutMinLaneNameGoldFraction;

        // Presence-only lane resolution. If one learned lane UI has disappeared while the other
        // learned lane UI is still clearly present, the missing lane is the completed boss. No
        // health-fill threshold participates in this decision.
        if (elapsedMs >= _config.DualRemovalConfirmMs)
        {
            if (leftMissing && rightPresent && _leftTrack is not null && _rightTrack is not null)
            {
                FinalizeDualLanePresenceRemoval(now, raw, metrics, _rightTrack, _leftTrack);
                return;
            }

            if (rightMissing && leftPresent && _leftTrack is not null && _rightTrack is not null)
            {
                FinalizeDualLanePresenceRemoval(now, raw, metrics, _leftTrack, _rightTrack);
                return;
            }
        }

        // If the survivor has recentered into the normal single-boss UI, both old lane templates
        // may disappear at once. OCR the CURRENT centered bar and use its positive identity to infer
        // which of the previously active bosses is missing. Health fill remains diagnostic only.
        if (IsSingleTopologyPresent(metrics) && now >= _nextResolveOcr)
        {
            _nextResolveOcr = now.AddMilliseconds(_config.OcrAcquisitionCycleMs);
            var result = ReadCurrentFrameWithFallback(raw, BossLane.Single, now);
            if (result.Match is not null && IsActiveDualBoss(result.Match.Boss.Id))
            {
                KeepBest(ref _resolveSurvivor,
                    new LaneCandidate(result.Read, result.Match, BossLane.Single, result.Source));
                _events.Debug($"DUAL_SURVIVOR_MATCH | boss={result.Match.Boss.Id}" +
                    $" | source={result.Source}" +
                    $" | score={result.Match.Similarity:F3} | elapsedMs={elapsedMs:F1}");
            }
        }

        if (_resolveSurvivor is not null && elapsedMs >= _config.DualRemovalConfirmMs)
        {
            FinalizeDualToSingle(now, raw, metrics, _resolveSurvivor);
            return;
        }

        // DUAL -> NONE: both old lane templates are gone and there is no meaningful boss-name UI
        // anywhere in the boss-name region. Red health fill is deliberately ignored: a bar can be
        // at 0% while the boss remains active, and colored combat effects can also resemble health.
        var cleanNoBossNameUi = metrics.NameGoldFraction <= _config.DualBothGoneMaxNameGoldFraction;
        if (leftMissing && rightMissing && cleanNoBossNameUi &&
            (now - _resolveFirstMissing).TotalMilliseconds >= _config.DualBothGoneConfirmMs)
        {
            FinalizeDualBothGone(now, raw);
        }
    }

    private bool IsSingleTopologyPresent(BossBarMetrics metrics)
    {
        return !metrics.DualNameSignature &&
            metrics.CenterNameGoldFraction >= _config.DualRemovalMinSingleCenterNameGoldFraction &&
            metrics.NameGoldFraction >= _config.OcrMinNameGoldPixelFraction;
    }

    private void FinalizeDualLanePresenceRemoval(
        DateTimeOffset now,
        Bitmap raw,
        BossBarMetrics metrics,
        DualBossTrack survivor,
        DualBossTrack missing)
    {
        if (_config.SaveStateChangeImages)
            _images.Save($"DUAL_GONE_{missing.Boss.Id}_SURVIVOR_{survivor.Boss.Id}_PRESENCE", raw, null);

        _events.Debug($"DUAL_REMOVAL_PRESENCE_RESOLVED | survivor={survivor.Boss.Id} | gone={missing.Boss.Id}" +
            $" | firstMissing={_resolveFirstMissing:O}" +
            $" | survivorCoverage={survivor.Coverage:F3}" +
            $" | missingCoverage={missing.Coverage:F3}" +
            $" | laneRunDiagnostic=L{metrics.LeftHealthRedRunFraction:F3}/R{metrics.RightHealthRedRunFraction:F3}" +
            $" | resolveMs={(now - _resolveStarted).TotalMilliseconds:F1}");
        _events.BossGone(missing.Boss, _resolveFirstMissing, _resolveStarted, now, _config.DualRemovalConfirmMs);

        // Most PoE2 dual encounters recenter the surviving bar. If that centered UI is already
        // present, relearn it directly from the known survivor identity. Otherwise keep the old
        // resolver alive long enough for the centered topology to appear or for OCR to identify it.
        if (IsSingleTopologyPresent(metrics))
        {
            var survivorBoss = survivor.Boss;
            ClearDualTracks();
            _single.ArmKnownBoss(now, raw, null, survivorBoss, metrics, emitSeen: false);
            _mode = Mode.TrackSingle;
            ResetSingleAcquisitionTransient();
            return;
        }

        // A disappearing lane with the survivor still visibly occupying its old half is unusual but
        // valid. Remove only the completed lane and continue watching the survivor's existing lane
        // template until the UI recenters or that bar itself disappears.
        if (missing.Lane == BossLane.Left) _leftTrack = null;
        else _rightTrack = null;
        _mode = Mode.TrackDual;
        _dualAbsentFrames = 0;
        _dualLossCandidateAt = null;
        _resolveSurvivor = null;
        _resolveStarted = DateTimeOffset.MinValue;
        _resolveFirstMissing = DateTimeOffset.MinValue;
        _nextResolveOcr = DateTimeOffset.MinValue;
    }

    private void FinalizeDualToSingle(DateTimeOffset now, Bitmap raw, BossBarMetrics metrics, LaneCandidate survivorCandidate)
    {
        if (_leftTrack is null || _rightTrack is null) return;

        var survivor = survivorCandidate.Match.Boss;
        DualBossTrack missing;
        if (string.Equals(_leftTrack.Boss.Id, survivor.Id, StringComparison.OrdinalIgnoreCase))
            missing = _rightTrack;
        else if (string.Equals(_rightTrack.Boss.Id, survivor.Id, StringComparison.OrdinalIgnoreCase))
            missing = _leftTrack;
        else
            return;

        if (_config.SaveStateChangeImages)
            _images.Save($"DUAL_GONE_{missing.Boss.Id}_SURVIVOR_{survivor.Id}", raw, null);

        _events.Debug($"DUAL_REMOVAL_RESOLVED | survivor={survivor.Id} | gone={missing.Boss.Id}" +
            $" | evidence=survivor-ui-identity" +
            $" | firstMissing={_resolveFirstMissing:O}" +
            $" | resolveMs={(now - _resolveStarted).TotalMilliseconds:F1}");
        _events.BossGone(missing.Boss, _resolveFirstMissing, _resolveStarted, now, _config.DualRemovalConfirmMs);

        ClearDualTracks();
        _single.ArmKnownBoss(now, raw, null, survivor, metrics, survivorCandidate.Read,
            survivorCandidate.Match.Similarity, emitSeen: false);
        _mode = Mode.TrackSingle;
        ResetSingleAcquisitionTransient();
    }

    private void FinalizeDualBothGone(DateTimeOffset now, Bitmap raw)
    {
        if (_leftTrack is null || _rightTrack is null) return;

        var left = _leftTrack.Boss;
        var right = _rightTrack.Boss;
        if (_config.SaveStateChangeImages)
            _images.Save($"DUAL_BOTH_GONE_{left.Id}_{right.Id}", raw, null);

        _events.Debug($"DUAL_BOTH_GONE | left={left.Id} | right={right.Id}" +
            $" | evidence=both-bar-ui-absent" +
            $" | firstMissing={_resolveFirstMissing:O}");
        _events.BossGone(left, _resolveFirstMissing, _resolveStarted, now, _config.DualBothGoneConfirmMs);
        _events.BossGone(right, _resolveFirstMissing, _resolveStarted, now, _config.DualBothGoneConfirmMs);

        ClearDualTracks();
        _single.ResetTracking("dual both gone");
        _mode = Mode.Scan;
        _nextEncounterAcquireAllowed = now.AddMilliseconds(_config.ReacquireCooldownMs);
        _nextSingleBurstAllowed = _nextEncounterAcquireAllowed;
    }

    private OcrResult ReadAcquisitionCycle(
        BossLane lane,
        DateTimeOffset now,
        OcrLaneHistory history,
        int failedCyclesBeforeThisAttempt)
    {
        OcrResult last = new(new OcrRead("", 0), null, "none");

        // First fresh-frame cycle: preserve the original calibrated gold mask as the primary path.
        // Once it has already failed for this lane, do not spend another Tesseract call on the same
        // narrow classifier every cycle; the broader live-rendering path becomes the main retry.
        if (failedCyclesBeforeThisAttempt == 0)
        {
            using var gold = history.CreateLatestScaled(_config, OcrPreprocessMode.Gold);
            if (gold is not null)
            {
                _burstAttempts++;
                last = ReadAndMatch(gold, lane, now, "gold-single");
                if (last.Match is not null) return last;
            }
        }

        // Path B: broader lane-local mask. Because the lane geometry is already known, this can
        // admit paler/antialiased live-rendered glyph pixels without exposing Tesseract to the
        // full boss ROI or scene background.
        using (var broad = history.CreateLatestScaled(_config, OcrPreprocessMode.Broad))
        {
            if (broad is not null)
            {
                _burstAttempts++;
                last = ReadAndMatch(broad, lane, now, "broad-single");
                if (last.Match is not null) return last;
            }
        }

        // Path C: tertiary temporal fallback after repeated fresh-frame failures. Only ONE temporal
        // variant is tried per cycle, alternating broad/gold composites, so repeated failure does
        // not explode into four Tesseract calls per lane per captured sample.
        if (failedCyclesBeforeThisAttempt + 1 >= _config.OcrTemporalFallbackAfterFailedCycles &&
            history.Count >= _config.OcrTemporalFrameCount)
        {
            var temporalMode = failedCyclesBeforeThisAttempt % 2 == 0
                ? OcrPreprocessMode.Broad
                : OcrPreprocessMode.Gold;
            var source = temporalMode == OcrPreprocessMode.Broad ? "broad-temporal" : "gold-temporal";
            using var temporal = history.CreateTemporalScaled(
                _config, temporalMode, _config.OcrTemporalFrameCount);
            if (temporal is not null)
            {
                _burstAttempts++;
                last = ReadAndMatch(temporal, lane, now, source);
                if (last.Match is not null) return last;
            }
        }

        return last;
    }

    private OcrResult ReadCurrentFrameWithFallback(Bitmap raw, BossLane lane, DateTimeOffset now)
    {
        using (var gold = ScreenCapture.PreprocessBossNameForOcr(raw, _config, lane, OcrPreprocessMode.Gold))
        {
            _burstAttempts++;
            var result = ReadAndMatch(gold, lane, now, "gold-single");
            if (result.Match is not null) return result;
        }

        using (var broad = ScreenCapture.PreprocessBossNameForOcr(raw, _config, lane, OcrPreprocessMode.Broad))
        {
            _burstAttempts++;
            return ReadAndMatch(broad, lane, now, "broad-single");
        }
    }

    private void MaybeSaveOcrFailure(
        DateTimeOffset now,
        Bitmap raw,
        BossLane lane,
        OcrLaneHistory history,
        int failedCycles)
    {
        if (!_config.SaveStateChangeImages || failedCycles < _config.OcrFailureDiagnosticAfterCycles)
            return;

        var lastSave = lane switch
        {
            BossLane.Left => _leftLastOcrDiagnostic,
            BossLane.Right => _rightLastOcrDiagnostic,
            _ => _singleLastOcrDiagnostic
        };
        if (lastSave != DateTimeOffset.MinValue &&
            (now - lastSave).TotalMilliseconds < _config.OcrFailureDiagnosticIntervalMs)
            return;

        using var gold = history.CreateLatestScaled(_config, OcrPreprocessMode.Gold);
        using var broad = history.CreateLatestScaled(_config, OcrPreprocessMode.Broad);
        using var temporalGold = history.CreateTemporalScaled(
            _config, OcrPreprocessMode.Gold, _config.OcrTemporalFrameCount);
        using var temporalBroad = history.CreateTemporalScaled(
            _config, OcrPreprocessMode.Broad, _config.OcrTemporalFrameCount);

        _images.SaveOcrDiagnostic(
            $"OCRFAIL_{lane.ToString().ToUpperInvariant()}_{failedCycles}",
            raw, gold, broad, temporalGold, temporalBroad);

        var goldPixels = history.LatestPixelCount(OcrPreprocessMode.Gold);
        var broadPixels = history.LatestPixelCount(OcrPreprocessMode.Broad);
        var temporalGoldPixels = history.Count >= _config.OcrTemporalFrameCount
            ? history.TemporalPixelCount(OcrPreprocessMode.Gold) : 0;
        var temporalBroadPixels = history.Count >= _config.OcrTemporalFrameCount
            ? history.TemporalPixelCount(OcrPreprocessMode.Broad) : 0;
        _events.Debug($"OCR_DIAGNOSTIC_SAVED | lane={lane.ToString().ToLowerInvariant()}" +
            $" | failedCycles={failedCycles} | frames={history.Count}" +
            $" | goldPixels={goldPixels} | broadPixels={broadPixels}" +
            $" | temporalGoldPixels={temporalGoldPixels} | temporalBroadPixels={temporalBroadPixels}");

        if (lane == BossLane.Left) _leftLastOcrDiagnostic = now;
        else if (lane == BossLane.Right) _rightLastOcrDiagnostic = now;
        else _singleLastOcrDiagnostic = now;
    }

    private OcrResult ReadAndMatch(Bitmap processed, BossLane lane, DateTimeOffset now, string source)
    {
        var read = _ocr.ReadSingleLine(processed);
        var match = _matcher.Match(read.Text, _config.MinOcrSimilarity);
        _ocrAttempts++;
        _lastOcrAt = now;
        _lastOcrSource = source;
        _lastOcr = BossNameMatcher.Normalize(read.Text);
        _lastMatch = match is null ? "-" : $"{match.Boss.Name} {match.Similarity:P0}";

        if (match is not null)
            _events.Debug($"OCR_MATCH | lane={lane.ToString().ToLowerInvariant()} | source={source} | boss={match.Boss.Id}" +
                $" | score={match.Similarity:F3} | conf={read.Confidence:F3} | text={_lastOcr}");
        else if (_config.LogRejectedOcr)
            _events.Debug($"OCR_REJECT | lane={lane.ToString().ToLowerInvariant()} | source={source}" +
                $" | conf={read.Confidence:F3} | text={_lastOcr}");

        return new OcrResult(read, match, source);
    }

    private void MeasureDualTemplates(Bitmap raw)
    {
        if (_leftTrack is not null) _leftTrack.Measure(raw, _config);
        if (_rightTrack is not null) _rightTrack.Measure(raw, _config);
    }

    private bool IsActiveDualBoss(string id)
        => (_leftTrack is not null && string.Equals(_leftTrack.Boss.Id, id, StringComparison.OrdinalIgnoreCase))
        || (_rightTrack is not null && string.Equals(_rightTrack.Boss.Id, id, StringComparison.OrdinalIgnoreCase));

    private static void KeepBest(ref LaneCandidate? current, LaneCandidate? incoming)
    {
        if (incoming is null) return;
        if (current is null || incoming.Match.Similarity > current.Match.Similarity)
            current = incoming;
    }

    public void SuspendCapture()
    {
        _single.SuspendCapture();
        // Do not advance layout-change confirmation while PoE2 is not capturable, and never mix
        // pre-alt-tab OCR masks into a later temporal composite.
        _dualPresentFrames = 0;
        _dualAbsentFrames = 0;
        _dualLossCandidateAt = null;
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _singleCandidateLossFrames = 0;
        _singleOcrHistory.Clear();
        _leftOcrHistory.Clear();
        _rightOcrHistory.Clear();
    }

    public void ResetTracking(string reason)
    {
        _events.Debug($"ENCOUNTER_RESET | reason={reason}");
        _single.ResetTracking(reason);
        ClearDualTracks();
        ClearDualAcquisitionTransient(clearExisting: true);
        ResetSingleAcquisitionTransient();
        _mode = Mode.Scan;
        _nextEncounterAcquireAllowed = DateTimeOffset.MinValue;
        _nextSingleBurstAllowed = DateTimeOffset.MinValue;
    }

    private void ResetSingleAcquisitionTransient()
    {
        _singleCandidateFrames = 0;
        _singleCenteredCandidateFrames = 0;
        _singleCandidateLossFrames = 0;
        _singleBurstEnd = DateTimeOffset.MinValue;
        _nextSingleOcr = DateTimeOffset.MinValue;
        _singleFailedOcrCycles = 0;
        _singleLastOcrDiagnostic = DateTimeOffset.MinValue;
        _singleOcrHistory.Clear();
        _burstAttempts = 0;
    }

    private void ClearDualAcquisitionTransient(bool clearExisting = false)
    {
        _leftCandidate = null;
        _rightCandidate = null;
        _leftPendingTrack = null;
        _rightPendingTrack = null;
        _leftPendingMissingAt = null;
        _rightPendingMissingAt = null;
        _dualAcquireSignatureLostAt = null;
        _leftOcrHistory.Clear();
        _rightOcrHistory.Clear();
        _leftFailedOcrCycles = 0;
        _rightFailedOcrCycles = 0;
        _leftLastOcrDiagnostic = DateTimeOffset.MinValue;
        _rightLastOcrDiagnostic = DateTimeOffset.MinValue;
        _dualPresentFrames = 0;
        _dualAbsentFrames = 0;
        _dualBurstEnd = DateTimeOffset.MinValue;
        _nextDualOcr = DateTimeOffset.MinValue;
        _nextDualBurstAllowed = DateTimeOffset.MinValue;
        _burstAttempts = 0;
        if (clearExisting) _existingSingleBeforeDual = null;
    }

    private void ClearDualTracks()
    {
        _leftTrack = null;
        _rightTrack = null;
        _dualLossCandidateAt = null;
        _dualAbsentFrames = 0;
        _resolveSurvivor = null;
        _resolveStarted = DateTimeOffset.MinValue;
        _resolveFirstMissing = DateTimeOffset.MinValue;
        _nextResolveOcr = DateTimeOffset.MinValue;
        _existingSingleBeforeDual = null;
    }

    private sealed class DualBossTrack
    {
        public BossDefinition Boss { get; }
        public BossLane Lane { get; }
        public BossNameTemplate? Template { get; }
        public int ReferencePixels { get; }
        public double Coverage { get; private set; }

        private DualBossTrack(BossDefinition boss, BossLane lane, BossNameTemplate? template, int referencePixels)
        {
            Boss = boss;
            Lane = lane;
            Template = template;
            ReferencePixels = referencePixels;
            Coverage = template is null ? 0 : 1.0;
        }

        public static DualBossTrack Create(BossDefinition boss, BossLane lane, Bitmap raw, AppConfig config)
        {
            var captured = BossNameTemplate.Capture(raw, config, lane);
            var valid = captured.ReferencePixelCount >= config.TrackedTemplateMinReferencePixels ? captured : null;
            return new DualBossTrack(boss, lane, valid, captured.ReferencePixelCount);
        }

        public void Measure(Bitmap raw, AppConfig config)
        {
            Coverage = Template is null ? 0 : Template.MeasureCoverage(raw, config);
        }

    }

    private sealed record LaneCandidate(OcrRead Read, BossMatch Match, BossLane Lane, string Source);
    private sealed record OcrResult(OcrRead Read, BossMatch? Match, string Source);
}
