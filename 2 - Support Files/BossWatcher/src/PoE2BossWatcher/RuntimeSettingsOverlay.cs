using System.Text.Json;

namespace PoE2BossWatcher;

internal static class RuntimeSettingsOverlay
{
    public static string Apply(AppConfig config, string? settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            return $"shared-settings=not-supplied; gameLanguage={config.GameLanguage}; using BossWatcher config.json values";

        var fullPath = Path.GetFullPath(settingsPath);
        if (!File.Exists(fullPath))
            return $"shared-settings=missing path={fullPath}; gameLanguage={config.GameLanguage}; using BossWatcher config.json values";

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
                config.GameLanguage = GameLanguageCatalog.Normalize(languageElement.GetString());
            }

            var goneMs = config.DisappearConfirmMs;
            var mapGoneMs = config.MapDisappearConfirmMs;
            var mapExitAssistMs = config.MapExitAssistMinMissingMs;
            if (doc.RootElement.TryGetProperty("BossWatcher", out var boss) && boss.ValueKind == JsonValueKind.Object)
            {
                if (boss.TryGetProperty("GoneConfirmMs", out var goneElement))
                {
                    if (!goneElement.TryGetInt32(out goneMs) || goneMs is < 500 or > 30000)
                        return $"shared-settings=invalid GoneConfirmMs; gameLanguage={config.GameLanguage}; using BossWatcher config.json timing values";
                }
                if (boss.TryGetProperty("MapGoneConfirmMs", out var mapGoneElement))
                {
                    if (!mapGoneElement.TryGetInt32(out mapGoneMs) || mapGoneMs is < 100 or > 30000)
                        return $"shared-settings=invalid MapGoneConfirmMs; gameLanguage={config.GameLanguage}; using BossWatcher config.json timing values";
                }
                if (boss.TryGetProperty("MapExitAssistMinMissingMs", out var mapExitAssistElement))
                {
                    if (!mapExitAssistElement.TryGetInt32(out mapExitAssistMs) || mapExitAssistMs is < 100 or > 5000)
                        return $"shared-settings=invalid MapExitAssistMinMissingMs; gameLanguage={config.GameLanguage}; using BossWatcher config.json timing values";
                }
            }

            config.DisappearConfirmMs = goneMs;
            config.MapDisappearConfirmMs = mapGoneMs;
            config.MapExitAssistMinMissingMs = mapExitAssistMs;
            config.Validate();
            return $"shared-settings=applied path={fullPath}; gameLanguage={config.GameLanguage}; GoneConfirmMs={goneMs}; MapGoneConfirmMs={mapGoneMs}; MapExitAssistMinMissingMs={mapExitAssistMs}";
        }
        catch (Exception ex)
        {
            return $"shared-settings=invalid path={fullPath}; gameLanguage={config.GameLanguage}; using BossWatcher config.json values; error={ex.Message}";
        }
    }
}
