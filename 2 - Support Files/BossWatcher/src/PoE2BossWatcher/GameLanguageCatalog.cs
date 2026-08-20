namespace PoE2BossWatcher;

public sealed record GameLanguageInfo(string Code, string DisplayName, string TesseractCode);

public static class GameLanguageCatalog
{
    public static readonly IReadOnlyList<GameLanguageInfo> All = new List<GameLanguageInfo>
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

    public static GameLanguageInfo Resolve(string? code)
    {
        var match = All.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return match ?? All[0];
    }

    public static string Normalize(string? code) => Resolve(code).Code;
}
