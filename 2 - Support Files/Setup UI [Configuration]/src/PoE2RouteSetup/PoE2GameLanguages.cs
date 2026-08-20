namespace PoE2RouteSetup;

public sealed record PoE2GameLanguage(string Code, string DisplayName, string TesseractCode);

public static class PoE2GameLanguages
{
    // These are the PoE2 game-client languages with supported GGG localization.
    // SetupUI derives its own language selector from this exact catalog so the
    // two language settings cannot drift apart.
    public static readonly IReadOnlyList<PoE2GameLanguage> All = new List<PoE2GameLanguage>
    {
        new("en", "English", "eng"),
        new("fr", "Français", "fra"),
        new("de", "Deutsch", "deu"),
        new("es-ES", "Español (España)", "spa"),
        new("ja", "日本語", "jpn"),
        new("ko", "한국어", "kor"),
        new("pt-BR", "Português (Brasil)", "por"),
        new("ru", "Русский", "rus"),
        new("th", "ไทย", "tha")
    };

    public static bool IsSupported(string? code)
        => !string.IsNullOrWhiteSpace(code) && All.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? code)
        => IsSupported(code)
            ? All.First(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)).Code
            : "en";
}
