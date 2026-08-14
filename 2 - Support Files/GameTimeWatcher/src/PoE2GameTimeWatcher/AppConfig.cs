using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoE2GameTimeWatcher;

public sealed class AppConfig
{
    public string[] ProcessNames { get; set; } = ["PathOfExileSteam", "PathOfExile", "PathOfExile_x64Steam", "PathOfExile_x64"];
    public int CaptureFps { get; set; } = 10;
    public int FastCaptureFps { get; set; } = 30;
    public int InputFastModeMs { get; set; } = 1500;
    public int InputHintWindowMs { get; set; } = 750;
    public int HeartbeatMs { get; set; } = 250;
    public int ProvisionalTimeoutMs { get; set; } = 1200;
    public int ConfirmPausedFrames { get; set; } = 2;
    public int ConfirmRunningFrames { get; set; } = 2;
    public bool RequireForegroundForNewDetection { get; set; } = true;

    // Separate thresholds let the strongest invariant (RESUME GAME) be more
    // sensitive without making the weaker fallbacks overly permissive.
    public double PauseStackThreshold { get; set; } = 0.62;
    public double ResumeGameThreshold { get; set; } = 0.58;
    public double PauseBannerThreshold { get; set; } = 0.40;
    public double ExitPathOfExileThreshold { get; set; } = 0.50;
    public double MtxShopThreshold { get; set; } = 0.70;
    public int CanonicalHeight { get; set; } = 576;
    public string PauseStackTemplate { get; set; } = "templates/pause-menu-stack.png";
    public string ResumeGameTemplate { get; set; } = "templates/pause-resume-game.png";
    public string PauseBannerTemplate { get; set; } = "templates/pause-menu-tight.png";
    public string ExitPathOfExileTemplate { get; set; } = "templates/pause-exit-path-of-exile.png";
    public string MtxShopTemplate { get; set; } = "templates/mtx-shop.png";

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
            throw new FileNotFoundException($"GameTimeWatcher config file was not found: {Path.GetFullPath(path)}", path);

        var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialize config.json");
        config.Validate();
        return config;
    }

    private void Validate()
    {
        if (ProcessNames.Length == 0) throw new InvalidOperationException("ProcessNames must not be empty.");
        if (CaptureFps is < 1 or > 30) throw new InvalidOperationException("CaptureFps must be 1-30.");
        if (FastCaptureFps is < 1 or > 60) throw new InvalidOperationException("FastCaptureFps must be 1-60.");
        if (FastCaptureFps < CaptureFps) throw new InvalidOperationException("FastCaptureFps must be >= CaptureFps.");
        if (InputFastModeMs is < 100 or > 5000) throw new InvalidOperationException("InputFastModeMs must be 100-5000.");
        if (InputHintWindowMs is < 100 or > 3000) throw new InvalidOperationException("InputHintWindowMs must be 100-3000.");
        if (HeartbeatMs is < 50 or > 2000) throw new InvalidOperationException("HeartbeatMs must be 50-2000.");
        if (ProvisionalTimeoutMs is < 200 or > 3000) throw new InvalidOperationException("ProvisionalTimeoutMs must be 200-3000.");
        if (ConfirmPausedFrames is < 1 or > 20 || ConfirmRunningFrames is < 1 or > 20)
            throw new InvalidOperationException("Confirmation frame counts must be 1-20.");
        if (PauseStackThreshold is < 0 or > 1 || ResumeGameThreshold is < 0 or > 1 || PauseBannerThreshold is < 0 or > 1 ||
            ExitPathOfExileThreshold is < 0 or > 1 || MtxShopThreshold is < 0 or > 1)
            throw new InvalidOperationException("Template thresholds must be 0-1.");
        if (CanonicalHeight is < 360 or > 1440)
            throw new InvalidOperationException("CanonicalHeight must be 360-1440.");
    }
}
