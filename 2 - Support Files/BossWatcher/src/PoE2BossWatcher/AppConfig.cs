using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoE2BossWatcher;

public sealed class AppConfig
{
    public string[] ProcessNames { get; set; } =
    [
        "PathOfExileSteam",
        "PathOfExile",
        "PathOfExile_x64Steam",
        "PathOfExile_x64"
    ];

    // Region captured from the PoE2 client. Coordinates are normalized to the game client.
    public NormalizedRect BossRoi { get; set; } = new(0.25, 0.015, 0.50, 0.115);

    // Horizontal boss-UI geometry is centered and scaled from CLIENT HEIGHT, not client width.
    // 8/9 reproduces the historical 50%-wide capture on a 16:9 client:
    //   0.5 * (16/9) = 8/9 client-heights.
    // Keeping this height-relative prevents the capture/name lanes from stretching apart on
    // 21:9 and 32:9 displays while still adapting automatically to 1080p/1440p/2160p UI scale.
    // This is an internal calibration value, not a user resolution setting.
    public double BossCaptureWidthHeightRatio { get; set; } = 8.0 / 9.0;

    // Sub-region INSIDE the centered BossRoi capture used only for OCR/name-pixel analysis.
    public NormalizedRect BossNameRoi { get; set; } = new(0.25, 0.235, 0.50, 0.245);

    // Legacy broad-gold diagnostic region INSIDE BossRoi. This is intentionally NOT used as
    // a boss-presence trigger because terrain/effects can contain the same colors because terrain/effects can contain the same colors.
    public NormalizedRect BossFrameRoi { get; set; } = new(0.26, 0.02, 0.48, 0.56);

    // Sub-region INSIDE BossRoi covering the horizontal health-bar fill. v0.1.6 uses the
    // longest continuous red run in this region as a cheap structural pre-gate for OCR.
    public NormalizedRect BossHealthRoi { get; set; } = new(0.30, 0.15, 0.40, 0.30);

    public int CaptureFps { get; set; } = 20;
    public int OcrUpscale { get; set; } = 5;
    public int ConsoleUpdateMs { get; set; } = 200;

    // Acquisition is now burst-based. A strong structural boss-bar candidate must be present
    // for a few frames before Tesseract is allowed to run. Once armed, OCR runs rapidly for a
    // short burst so a real boss is identified quickly, then backs off if nothing matches.
    public double OcrTriggerRedRunFraction { get; set; } = 0.08;
    public double OcrMinNameGoldPixelFraction { get; set; } = 0.004;

    // v0.1.14 idle-scan optimization: a real full-width single boss has name-colored pixels
    // in the center anchor of BossNameRoi. Requiring a modest centered-name signal prevents
    // terrain/effects with a long red horizontal run from repeatedly waking Tesseract.
    public double SingleOcrMinCenterNameGoldFraction { get; set; } = 0.020;
    public int OcrCandidateConsecutiveFrames { get; set; } = 2;
    public int SingleOcrWeakCenterConfirmFrames { get; set; } = 8;

    // Once single acquisition starts, stop spending OCR if the boss-like structural gate
    // disappears for several consecutive capture frames. This rejects transient UI/effect
    // candidates without changing tracked-boss disappearance semantics.
    public int SingleOcrCandidateLossFrames { get; set; } = 3;
    public int OcrBurstIntervalMs { get; set; } = 100;
    public int OcrBurstDurationMs { get; set; } = 1600;
    public int OcrRetryCooldownMs { get; set; } = 900;

    // Acquisition OCR is organized into fresh-frame cycles instead of repeatedly feeding nearly
    // identical masks to Tesseract. The proven v0.1.13 gold -> broad -> temporal order is retained;
    // v0.1.14 optimizes when OCR wakes up rather than changing successful identity resolution.
    public int OcrAcquisitionCycleMs { get; set; } = 300;
    public int OcrTemporalFrameCount { get; set; } = 4;
    public int OcrTemporalFallbackAfterFailedCycles { get; set; } = 2;
    public int OcrFailureDiagnosticAfterCycles { get; set; } = 4;
    public int OcrFailureDiagnosticIntervalMs { get; set; } = 15000;

    // PoE2 boss names are orange/gold rather than white. These thresholds isolate that text.
    public int OcrRedMin { get; set; } = 140;
    public int OcrGreenMin { get; set; } = 55;
    public int OcrBlueMax { get; set; } = 125;
    public int OcrRedMinusGreenMin { get; set; } = 30;
    public int OcrGreenMinusBlueMin { get; set; } = 5;

    // Broader lane-local fallback for live-rendered boss text. The ROI is already tightly
    // constrained to a boss-name lane, so this classifier can admit antialiased/paler pixels
    // that the original orange/gold mask rejects without reopening the full-scene noise problem.
    public int OcrBroadRedMin { get; set; } = 105;
    public int OcrBroadGreenMin { get; set; } = 70;
    public int OcrBroadBlueMax { get; set; } = 220;
    public int OcrBroadRedMinusGreenMin { get; set; } = -10;
    public int OcrBroadGreenMinusBlueMin { get; set; } = -50;
    public double OcrBroadLuminanceMin { get; set; } = 95;

    public double MinOcrSimilarity { get; set; } = 0.72;
    public int AcquireConsecutiveMatches { get; set; } = 1;

    // The spatial boss-name mask is learned at acquisition and measures how much of that exact
    // shape remains on subsequent frames. Unlike a raw gold-pixel fraction, terrain gold
    // elsewhere in the ROI does not count unless it appears at the learned boss-name pixels.
    public int TrackedTemplateMinReferencePixels { get; set; } = 80;
    public int TrackedTemplatePixelSearchRadius { get; set; } = 1;
    public double TrackedTemplateCenterWidthFraction { get; set; } = 0.80;
    public double TrackedTemplateMissingCoverage { get; set; } = 0.30;
    public double TrackedTemplateReturnedCoverage { get; set; } = 0.60;

    // Red horizontal-run history is retained for diagnostics only in v0.1.13. Health fill can
    // reach 0% during immunity/phase mechanics while the boss bar remains present, so none of
    // these values may start, confirm, or backdate a completion event.
    public int TrackedRedRunLookbackMs { get; set; } = 750;
    public int TrackedRedRunMinSamples { get; set; } = 3;
    public double TrackedRedRunReferenceMinFraction { get; set; } = 0.08;
    public double TrackedRedRunCollapseRelativeFraction { get; set; } = 0.40;
    public double TrackedRedRunCollapseAbsoluteFraction { get; set; } = 0.015;
    public double TrackedRedRunRecoveryRelativeFraction { get; set; } = 0.70;
    public int TrackedRedRunRebaseMs { get; set; } = 900;
    public double TrackedRedRunRebaseTemplateCoverage { get; set; } = 0.85;

    // Fallback only if a valid spatial name template cannot be built. This retains the adaptive
    // v0.1.6 name-fraction detector; broad frame gold is no longer a presence fallback.
    public double TrackedNameMissingAbsoluteFraction { get; set; } = 0.0025;
    public double TrackedNameMissingRelativeFraction { get; set; } = 0.20;
    public double TrackedNameReturnedAbsoluteFraction { get; set; } = 0.0050;
    public double TrackedNameReturnedRelativeFraction { get; set; } = 0.40;


    // v0.1.13 dual-boss topology detection. PoE2 keeps the overall boss UI width fixed and
    // splits it horizontally into left/right lanes. The detector samples name-colored pixels
    // around the center of each half-lane and requires at least one plausible red-health lane.
    // This runs BEFORE single-boss disappearance logic so SINGLE -> DUAL cannot be mistaken for
    // a sudden health-bar collapse.
    public double DualLayoutMinLaneNameGoldFraction { get; set; } = 0.006;
    public double DualLayoutMaxCenterNameGoldFraction { get; set; } = 0.020;
    public double DualLayoutMinLaneHealthRunFraction { get; set; } = 0.05;
    public int DualLayoutConfirmFrames { get; set; } = 2;

    // v0.1.14 idle dual optimization. Initial NONE -> DUAL candidates must persist slightly
    // longer and show a substantial coherent health-bar run before OCR starts. This is only an
    // acquisition/topology gate; health fill is never boss-completion evidence.
    public int DualInitialConfirmFrames { get; set; } = 4;
    public int DualInitialLowHealthConfirmFrames { get; set; } = 8;
    public double DualInitialMinCombinedHealthRunFraction { get; set; } = 0.20;

    public double DualOcrHorizontalArtifactMinFraction { get; set; } = 0.12;

    // v0.1.13 retains SINGLE -> DUAL promotion hardening. A real added boss produces a persistent dual
    // topology with a substantial half-width health run; death-animation/effect noise can briefly
    // resemble two name anchors but usually has almost no coherent health fill. Initial DUAL
    // acquisition still uses DualLayoutConfirmFrames so encounters that begin dual remain fast.
    public int DualAddConfirmFrames { get; set; } = 4;
    public double DualAddMinCombinedHealthRunFraction { get; set; } = 0.12;

    // v0.1.13 retains persistent dual acquisition and adds multi-source OCR. Once either lane is positively OCR-matched, that
    // identity and its learned template survive OCR burst boundaries. Brief dual-signature or
    // lane-template losses do not discard the partial acquisition until this grace expires.
    public int DualAcquirePartialGraceMs { get; set; } = 750;

    // v0.1.13 dual completion remains strictly boss-bar UI presence based. A lane completes only when
    // its learned UI/name template disappears and stays gone, or when a recentered survivor is
    // positively identified. Health-fill percentages are never used as death evidence.
    public int DualRemovalConfirmMs { get; set; } = 350;
    public double DualLaneGoneMaxNameGoldFraction { get; set; } = 0.004;
    public double DualRemovalMinSingleCenterNameGoldFraction { get; set; } = 0.004;
    public int DualBothGoneConfirmMs { get; set; } = 700;
    public double DualBothGoneMaxNameGoldFraction { get; set; } = 0.0020;

    // Deprecated diagnostics retained so older config files still deserialize cleanly.
    public double FrameMissingGoldPixelFraction { get; set; } = 0.008;
    public double FrameReturnedGoldPixelFraction { get; set; } = 0.015;

    // The watcher still confirms disappearance to reject one-frame UI flicker. The bridge can
    // backdate the recorded LiveSplit Real Time to firstMissing, so this confirmation does not
    // need to become timing error.
    public int DisappearConfirmMs { get; set; } = 5500;
    // Ordinary-map disappearance keeps the conservative long confirmation while the player
    // remains in the map. If the expected boss has already been continuously missing before
    // a real map-exit context arrives, the shorter exit-assist window can safely finalize the
    // pending disappearance without making all in-map disappearances permissive.
    public int MapDisappearConfirmMs { get; set; } = 5500;
    public int MapExitAssistMinMissingMs { get; set; } = 500;
    public int ReacquireCooldownMs { get; set; } = 900;

    // Retained as diagnostics.
    public double MinRedPixelFraction { get; set; } = 0.004;
    public double MinLightPixelFraction { get; set; } = 0.010;

    public bool RequireGameForeground { get; set; } = true;
    public bool SaveStateChangeImages { get; set; } = true;
    public int SaveDebugFrameEverySeconds { get; set; } = 0;
    public bool LogRejectedOcr { get; set; } = false;
    public string BossListFile { get; set; } = "bosses.txt";
    public string MapBossDatabaseFile { get; set; } = "map-bosses.json";
    public string BossLocalizationDatabaseFile { get; set; } = "boss-localizations.json";
    public string GameLanguage { get; set; } = "en";
    public string TessdataParent { get; set; } = ".";
    public string EventFile { get; set; } = "";
    public string DebugDirectory { get; set; } = "debug";

    [JsonIgnore]
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = new AppConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, JsonOptions));
            return defaults;
        }

        var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialize config.json");
        config.Validate();
        return config;
    }

    public void Validate()
    {
        GameLanguage = GameLanguageCatalog.Normalize(GameLanguage);
        if (ProcessNames.Length == 0) throw new InvalidOperationException("ProcessNames must contain at least one process name.");
        if (CaptureFps < 1 || CaptureFps > 60) throw new InvalidOperationException("CaptureFps must be 1-60.");
        if (OcrUpscale < 1 || OcrUpscale > 8) throw new InvalidOperationException("OcrUpscale must be 1-8.");
        if (ConsoleUpdateMs < 50 || ConsoleUpdateMs > 5000) throw new InvalidOperationException("ConsoleUpdateMs must be 50-5000.");
        if (MinOcrSimilarity is < 0 or > 1) throw new InvalidOperationException("MinOcrSimilarity must be 0-1.");
        if (OcrTriggerRedRunFraction is < 0 or > 1) throw new InvalidOperationException("OcrTriggerRedRunFraction must be 0-1.");
        if (OcrMinNameGoldPixelFraction is < 0 or > 1) throw new InvalidOperationException("OcrMinNameGoldPixelFraction must be 0-1.");
        if (SingleOcrMinCenterNameGoldFraction is < 0 or > 1)
            throw new InvalidOperationException("SingleOcrMinCenterNameGoldFraction must be 0-1.");
        if (OcrCandidateConsecutiveFrames < 1 || OcrCandidateConsecutiveFrames > 20)
            throw new InvalidOperationException("OcrCandidateConsecutiveFrames must be 1-20.");
        if (SingleOcrWeakCenterConfirmFrames < OcrCandidateConsecutiveFrames || SingleOcrWeakCenterConfirmFrames > 30)
            throw new InvalidOperationException("SingleOcrWeakCenterConfirmFrames must be >= OcrCandidateConsecutiveFrames and <= 30.");
        if (SingleOcrCandidateLossFrames < 1 || SingleOcrCandidateLossFrames > 20)
            throw new InvalidOperationException("SingleOcrCandidateLossFrames must be 1-20.");
        if (OcrBurstIntervalMs < 50 || OcrBurstIntervalMs > 2000) throw new InvalidOperationException("OcrBurstIntervalMs must be 50-2000.");
        if (OcrBurstDurationMs < 100 || OcrBurstDurationMs > 10000) throw new InvalidOperationException("OcrBurstDurationMs must be 100-10000.");
        if (OcrRetryCooldownMs < 0 || OcrRetryCooldownMs > 10000) throw new InvalidOperationException("OcrRetryCooldownMs must be 0-10000.");
        if (OcrAcquisitionCycleMs < 100 || OcrAcquisitionCycleMs > 2000)
            throw new InvalidOperationException("OcrAcquisitionCycleMs must be 100-2000.");
        if (OcrTemporalFrameCount < 2 || OcrTemporalFrameCount > 12)
            throw new InvalidOperationException("OcrTemporalFrameCount must be 2-12.");
        if (OcrTemporalFallbackAfterFailedCycles < 1 || OcrTemporalFallbackAfterFailedCycles > 20)
            throw new InvalidOperationException("OcrTemporalFallbackAfterFailedCycles must be 1-20.");
        if (OcrFailureDiagnosticAfterCycles < 1 || OcrFailureDiagnosticAfterCycles > 100)
            throw new InvalidOperationException("OcrFailureDiagnosticAfterCycles must be 1-100.");
        if (OcrFailureDiagnosticIntervalMs < 1000 || OcrFailureDiagnosticIntervalMs > 60000)
            throw new InvalidOperationException("OcrFailureDiagnosticIntervalMs must be 1000-60000.");
        if (TrackedTemplateMinReferencePixels < 1 || TrackedTemplateMinReferencePixels > 100000)
            throw new InvalidOperationException("TrackedTemplateMinReferencePixels must be 1-100000.");
        if (TrackedTemplatePixelSearchRadius < 0 || TrackedTemplatePixelSearchRadius > 4)
            throw new InvalidOperationException("TrackedTemplatePixelSearchRadius must be 0-4.");
        if (TrackedTemplateCenterWidthFraction is <= 0 or > 1)
            throw new InvalidOperationException("TrackedTemplateCenterWidthFraction must be > 0 and <= 1.");
        if (TrackedTemplateMissingCoverage is < 0 or > 1 || TrackedTemplateReturnedCoverage is < 0 or > 1)
            throw new InvalidOperationException("Tracked-template coverage thresholds must be 0-1.");
        if (TrackedTemplateReturnedCoverage <= TrackedTemplateMissingCoverage)
            throw new InvalidOperationException("TrackedTemplateReturnedCoverage must be greater than TrackedTemplateMissingCoverage.");
        if (TrackedRedRunLookbackMs < 100 || TrackedRedRunLookbackMs > 5000)
            throw new InvalidOperationException("TrackedRedRunLookbackMs must be 100-5000.");
        if (TrackedRedRunMinSamples < 2 || TrackedRedRunMinSamples > 100)
            throw new InvalidOperationException("TrackedRedRunMinSamples must be 2-100.");
        if (TrackedRedRunReferenceMinFraction is < 0 or > 1 ||
            TrackedRedRunCollapseRelativeFraction is < 0 or > 1 ||
            TrackedRedRunCollapseAbsoluteFraction is < 0 or > 1 ||
            TrackedRedRunRecoveryRelativeFraction is < 0 or > 1 ||
            TrackedRedRunRebaseTemplateCoverage is < 0 or > 1)
            throw new InvalidOperationException("Tracked temporal red-run thresholds must be 0-1.");
        if (TrackedRedRunRecoveryRelativeFraction <= TrackedRedRunCollapseRelativeFraction)
            throw new InvalidOperationException("TrackedRedRunRecoveryRelativeFraction must be greater than TrackedRedRunCollapseRelativeFraction.");
        if (TrackedRedRunRebaseMs < 100 || TrackedRedRunRebaseMs > 10000)
            throw new InvalidOperationException("TrackedRedRunRebaseMs must be 100-10000.");
        if (TrackedNameMissingAbsoluteFraction is < 0 or > 1 || TrackedNameMissingRelativeFraction is < 0 or > 1)
            throw new InvalidOperationException("Tracked-name missing thresholds must be 0-1.");
        if (TrackedNameReturnedAbsoluteFraction is < 0 or > 1 || TrackedNameReturnedRelativeFraction is < 0 or > 1)
            throw new InvalidOperationException("Tracked-name returned thresholds must be 0-1.");

        if (DualLayoutMinLaneNameGoldFraction is < 0 or > 1 ||
            DualLayoutMaxCenterNameGoldFraction is < 0 or > 1 ||
            DualLayoutMinLaneHealthRunFraction is < 0 or > 1)
            throw new InvalidOperationException("Dual-layout thresholds must be 0-1.");
        if (DualLayoutConfirmFrames < 1 || DualLayoutConfirmFrames > 20)
            throw new InvalidOperationException("DualLayoutConfirmFrames must be 1-20.");
        if (DualInitialConfirmFrames < DualLayoutConfirmFrames || DualInitialConfirmFrames > 30)
            throw new InvalidOperationException("DualInitialConfirmFrames must be >= DualLayoutConfirmFrames and <= 30.");
        if (DualInitialLowHealthConfirmFrames < DualInitialConfirmFrames || DualInitialLowHealthConfirmFrames > 60)
            throw new InvalidOperationException("DualInitialLowHealthConfirmFrames must be >= DualInitialConfirmFrames and <= 60.");
        if (DualInitialMinCombinedHealthRunFraction is < 0 or > 1)
            throw new InvalidOperationException("DualInitialMinCombinedHealthRunFraction must be 0-1.");
        if (DualOcrHorizontalArtifactMinFraction is <= 0 or > 1)
            throw new InvalidOperationException("DualOcrHorizontalArtifactMinFraction must be > 0 and <= 1.");
        if (DualAddConfirmFrames < DualLayoutConfirmFrames || DualAddConfirmFrames > 30)
            throw new InvalidOperationException("DualAddConfirmFrames must be >= DualLayoutConfirmFrames and <= 30.");
        if (DualAddMinCombinedHealthRunFraction is < 0 or > 1)
            throw new InvalidOperationException("DualAddMinCombinedHealthRunFraction must be 0-1.");
        if (DualAcquirePartialGraceMs < 100 || DualAcquirePartialGraceMs > 10000)
            throw new InvalidOperationException("DualAcquirePartialGraceMs must be 100-10000.");
        if (DualRemovalConfirmMs < 100 || DualRemovalConfirmMs > 5000)
            throw new InvalidOperationException("DualRemovalConfirmMs must be 100-5000.");
        if (DualLaneGoneMaxNameGoldFraction is < 0 or > 1 || DualRemovalMinSingleCenterNameGoldFraction is < 0 or > 1)
            throw new InvalidOperationException("Dual lane-removal UI thresholds must be 0-1.");
        if (DualBothGoneConfirmMs < DualRemovalConfirmMs || DualBothGoneConfirmMs > 10000)
            throw new InvalidOperationException("DualBothGoneConfirmMs must be >= DualRemovalConfirmMs and <= 10000.");
        if (DualBothGoneMaxNameGoldFraction is < 0 or > 1)
            throw new InvalidOperationException("DualBothGoneMaxNameGoldFraction must be 0-1.");

        if (FrameMissingGoldPixelFraction is < 0 or > 1) throw new InvalidOperationException("FrameMissingGoldPixelFraction must be 0-1.");
        if (FrameReturnedGoldPixelFraction is < 0 or > 1) throw new InvalidOperationException("FrameReturnedGoldPixelFraction must be 0-1.");
        if (FrameReturnedGoldPixelFraction <= FrameMissingGoldPixelFraction)
            throw new InvalidOperationException("FrameReturnedGoldPixelFraction must be greater than FrameMissingGoldPixelFraction.");
        if (DisappearConfirmMs < 100 || DisappearConfirmMs > 30000) throw new InvalidOperationException("DisappearConfirmMs must be 100-30000.");
        if (MapDisappearConfirmMs < 100 || MapDisappearConfirmMs > 30000) throw new InvalidOperationException("MapDisappearConfirmMs must be 100-30000.");
        if (MapExitAssistMinMissingMs < 100 || MapExitAssistMinMissingMs > 5000) throw new InvalidOperationException("MapExitAssistMinMissingMs must be 100-5000.");
        if (ReacquireCooldownMs < 0) throw new InvalidOperationException("ReacquireCooldownMs must be >= 0.");
        if (AcquireConsecutiveMatches < 1 || AcquireConsecutiveMatches > 5)
            throw new InvalidOperationException("AcquireConsecutiveMatches must be 1-5.");
        if (OcrRedMin is < 0 or > 255 || OcrGreenMin is < 0 or > 255 || OcrBlueMax is < 0 or > 255)
            throw new InvalidOperationException("OCR RGB thresholds must be 0-255.");
        if (OcrBroadRedMin is < 0 or > 255 || OcrBroadGreenMin is < 0 or > 255 || OcrBroadBlueMax is < 0 or > 255)
            throw new InvalidOperationException("Broad OCR RGB thresholds must be 0-255.");
        if (OcrBroadRedMinusGreenMin is < -255 or > 255 || OcrBroadGreenMinusBlueMin is < -255 or > 255)
            throw new InvalidOperationException("Broad OCR channel-difference thresholds must be -255 to 255.");
        if (OcrBroadLuminanceMin is < 0 or > 255)
            throw new InvalidOperationException("OcrBroadLuminanceMin must be 0-255.");
        if (BossCaptureWidthHeightRatio is < 0.40 or > 2.00)
            throw new InvalidOperationException("BossCaptureWidthHeightRatio must be between 0.40 and 2.00.");
        BossRoi.Validate("BossRoi");
        BossNameRoi.Validate("BossNameRoi");
        BossFrameRoi.Validate("BossFrameRoi");
        BossHealthRoi.Validate("BossHealthRoi");
    }
}

public sealed record NormalizedRect(double X, double Y, double Width, double Height)
{
    public void Validate(string name = "ROI")
    {
        if (X < 0 || Y < 0 || Width <= 0 || Height <= 0 || X + Width > 1.0 || Y + Height > 1.0)
            throw new InvalidOperationException($"{name} must be normalized: X/Y >= 0, Width/Height > 0, and X+Width/Y+Height <= 1.");
    }
}
