using System.Text.Json;

namespace PoE2GameTimeWatcher;

internal static class RuntimeSettingsOverlay
{
    public static string Apply(AppConfig config, string? settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            return $"shared-settings=not-supplied; gameLanguage={config.GameLanguage}; using GameTimeWatcher config.json values";

        var fullPath = Path.GetFullPath(settingsPath);
        if (!File.Exists(fullPath))
            return $"shared-settings=missing path={fullPath}; gameLanguage={config.GameLanguage}; using GameTimeWatcher config.json values";

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(fullPath), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (doc.RootElement.TryGetProperty("PoE2", out var poe2) && poe2.ValueKind == JsonValueKind.Object &&
                poe2.TryGetProperty("Language", out var languageElement) && languageElement.ValueKind == JsonValueKind.String)
            {
                config.GameLanguage = languageElement.GetString() ?? config.GameLanguage;
            }

            if (doc.RootElement.TryGetProperty("GameTimeWatcher", out var section) && section.ValueKind == JsonValueKind.Object)
            {
                var provisional = ReadInt(section, "ProvisionalTimeoutMs", config.ProvisionalTimeoutMs);
                var stack = ReadDouble(section, "PauseStackThreshold", config.PauseStackThreshold);
                var resume = ReadDouble(section, "ResumeGameThreshold", config.ResumeGameThreshold);
                var banner = ReadDouble(section, "PauseBannerThreshold", config.PauseBannerThreshold);
                var exit = ReadDouble(section, "ExitPathOfExileThreshold", config.ExitPathOfExileThreshold);
                var mtx = ReadDouble(section, "MtxShopThreshold", config.MtxShopThreshold);

                if (provisional is < 200 or > 3000)
                    return $"shared-settings=invalid ProvisionalTimeoutMs={provisional}; gameLanguage={config.GameLanguage}; using GameTimeWatcher config.json values";
                if (!ThresholdValid(stack) || !ThresholdValid(resume) || !ThresholdValid(banner) || !ThresholdValid(exit) || !ThresholdValid(mtx))
                    return $"shared-settings=invalid visual threshold outside 0..1; gameLanguage={config.GameLanguage}; using GameTimeWatcher config.json values";

                config.ProvisionalTimeoutMs = provisional;
                config.PauseStackThreshold = stack;
                config.ResumeGameThreshold = resume;
                config.PauseBannerThreshold = banner;
                config.ExitPathOfExileThreshold = exit;
                config.MtxShopThreshold = mtx;
            }

            config.Validate();
            return $"shared-settings=applied path={fullPath}; gameLanguage={config.GameLanguage}; provisionalTimeoutMs={config.ProvisionalTimeoutMs}; " +
                   $"structure={config.PauseStackThreshold:F3}; banner={config.PauseBannerThreshold:F3}; resumeText={config.ResumeGameThreshold:F3}; exitText={config.ExitPathOfExileThreshold:F3}; mtx={config.MtxShopThreshold:F3}";
        }
        catch (Exception ex)
        {
            return $"shared-settings=invalid path={fullPath}; gameLanguage={config.GameLanguage}; using GameTimeWatcher config.json values; error={ex.Message}";
        }
    }

    private static int ReadInt(JsonElement section, string name, int fallback)
        => section.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

    private static double ReadDouble(JsonElement section, string name, double fallback)
        => section.TryGetProperty(name, out var value) && value.TryGetDouble(out var parsed) ? parsed : fallback;

    private static bool ThresholdValid(double value) => value >= 0 && value <= 1;
}
