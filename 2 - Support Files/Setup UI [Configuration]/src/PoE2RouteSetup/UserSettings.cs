using System.Text.Json;

namespace PoE2RouteSetup;

public sealed class UserSettings
{
    public int SchemaVersion { get; set; } = 1;
    public SetupUiUserSettings SetupUI { get; set; } = new();
    public PoE2UserSettings PoE2 { get; set; } = new();
    public BossWatcherUserSettings BossWatcher { get; set; } = new();
    public GameTimeWatcherUserSettings GameTimeWatcher { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static UserSettings LoadOrCreate(string path, out string? warning)
    {
        warning = null;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        if (!File.Exists(path))
        {
            var defaults = new UserSettings();
            defaults.Save(path);
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidOperationException("The settings file did not contain a settings object.");
            settings.SetupUI ??= new SetupUiUserSettings();
            settings.PoE2 ??= new PoE2UserSettings();
            settings.BossWatcher ??= new BossWatcherUserSettings();
            settings.GameTimeWatcher ??= new GameTimeWatcherUserSettings();
            settings.Validate();
            return settings;
        }
        catch (Exception ex)
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backup = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, $"PoE2AS-Settings.invalid-{stamp}.json");
            try { File.Copy(path, backup, true); } catch { backup = "<backup failed>"; }

            var defaults = new UserSettings();
            defaults.Save(path);
            warning = $"PoE2AS-Settings.json was invalid and built-in defaults were restored. Backup: {backup}. Error: {ex.Message}";
            return defaults;
        }
    }

    public void Save(string path)
    {
        Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    public UserSettings Clone()
        => JsonSerializer.Deserialize<UserSettings>(JsonSerializer.Serialize(this, JsonOptions), JsonOptions) ?? new UserSettings();

    public void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidOperationException($"Unsupported settings SchemaVersion: {SchemaVersion}. Expected 1.");
        SetupUI.Validate();
        PoE2.Validate();
        BossWatcher.Validate();
        GameTimeWatcher.Validate();
    }
}

public sealed class SetupUiUserSettings
{
    public int WindowWidthPercent { get; set; } = 50;
    public int WindowHeightPercent { get; set; } = 100;
    public bool DeveloperConsoleDefault { get; set; } = false;
    public string DefaultLanguage { get; set; } = "en";

    public void Validate()
    {
        if (WindowWidthPercent is < 25 or > 100) throw new InvalidOperationException("SetupUI.WindowWidthPercent must be 25-100.");
        if (WindowHeightPercent is < 50 or > 100) throw new InvalidOperationException("SetupUI.WindowHeightPercent must be 50-100.");
        DefaultLanguage = Localization.Normalize(DefaultLanguage);
    }
}

public sealed class PoE2UserSettings
{
    // Language displayed by the Path of Exile 2 game client. This is intentionally
    // independent from SetupUI.DefaultLanguage so UI language and game language may differ.
    public string Language { get; set; } = "en";

    public void Validate()
    {
        Language = PoE2GameLanguages.Normalize(Language);
    }
}

public sealed class BossWatcherUserSettings
{
    // Identity/single-boss confirmation delay only. Completion timestamps remain backdated
    // to the first valid missing signal.
    public int GoneConfirmMs { get; set; } = 5500;

    // Conservative confirmation while the runner remains inside the map. A separately
    // guarded exit-assist path can reconcile a trusted boss disappearance after a fast exit.
    public int MapGoneConfirmMs { get; set; } = 5500;

    // Internal safety gate for fast boss-kill -> external-exit reconciliation. This is
    // snapshotted with the run settings even though it is not exposed as a normal UI knob.
    public int MapExitAssistMinMissingMs { get; set; } = 500;

    public void Validate()
    {
        if (GoneConfirmMs is < 500 or > 30000) throw new InvalidOperationException("BossWatcher.GoneConfirmMs must be 500-30000 milliseconds.");
        if (MapGoneConfirmMs is < 100 or > 30000) throw new InvalidOperationException("BossWatcher.MapGoneConfirmMs must be 100-30000 milliseconds.");
        if (MapExitAssistMinMissingMs is < 100 or > 5000) throw new InvalidOperationException("BossWatcher.MapExitAssistMinMissingMs must be 100-5000 milliseconds.");
    }
}

public sealed class GameTimeWatcherUserSettings
{
    public int ProvisionalTimeoutMs { get; set; } = 1200;
    public double PauseStackThreshold { get; set; } = 0.62;
    public double ResumeGameThreshold { get; set; } = 0.58;
    public double PauseBannerThreshold { get; set; } = 0.40;
    public double ExitPathOfExileThreshold { get; set; } = 0.50;
    public double MtxShopThreshold { get; set; } = 0.70;

    public void Validate()
    {
        if (ProvisionalTimeoutMs is < 200 or > 3000) throw new InvalidOperationException("GameTimeWatcher.ProvisionalTimeoutMs must be 200-3000.");
        if (PauseStackThreshold is < 0 or > 1 || ResumeGameThreshold is < 0 or > 1 || PauseBannerThreshold is < 0 or > 1 ||
            ExitPathOfExileThreshold is < 0 or > 1 || MtxShopThreshold is < 0 or > 1)
            throw new InvalidOperationException("GameTimeWatcher visual thresholds must be between 0 and 1.");
    }
}
