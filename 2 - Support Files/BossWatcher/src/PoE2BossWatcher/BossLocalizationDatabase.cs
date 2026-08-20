using System.Text.Json;

namespace PoE2BossWatcher;

/// <summary>
/// Authoritative localized display/OCR names keyed by the invariant BossWatcher boss ID.
/// English remains sourced from bosses.txt / map-bosses.json so the localization catalog
/// only needs to store verified non-English overrides.
/// </summary>
public sealed class BossLocalizationDatabase
{
    public int SchemaVersion { get; set; } = 1;
    public string DatabaseVersion { get; set; } = "";
    public string Purpose { get; set; } = "";
    public List<BossLocalizationEntry> Bosses { get; set; } = new();

    private Dictionary<string, BossLocalizationEntry> _byId = new(StringComparer.OrdinalIgnoreCase);

    public static BossLocalizationDatabase Load(string path)
    {
        if (!File.Exists(path))
            return Empty($"localization database missing: {path}");
        try
        {
            var db = JsonSerializer.Deserialize<BossLocalizationDatabase>(File.ReadAllText(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? new BossLocalizationDatabase();
            db.Bosses ??= new List<BossLocalizationEntry>();
            foreach (var entry in db.Bosses)
            {
                entry.Names ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in entry.Names.Keys.ToList())
                    entry.Names[key] ??= new List<string>();
            }
            db.BuildIndex();
            return db;
        }
        catch (Exception ex)
        {
            return Empty($"localization database parse failed: {ex.Message}");
        }
    }

    private static BossLocalizationDatabase Empty(string status)
    {
        var db = new BossLocalizationDatabase { DatabaseVersion = "unavailable", Purpose = status };
        db.BuildIndex();
        return db;
    }

    private void BuildIndex()
    {
        _byId = new Dictionary<string, BossLocalizationEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Bosses)
        {
            if (string.IsNullOrWhiteSpace(entry.Id)) continue;
            _byId[entry.Id.Trim()] = entry;
        }
    }

    public BossDefinition? Localize(BossDefinition source, string gameLanguage)
    {
        var lang = GameLanguageCatalog.Normalize(gameLanguage);
        if (lang == "en") return source;
        if (!_byId.TryGetValue(source.Id, out var entry)) return null;
        if (!TryNames(entry, lang, out var names)) return null;
        return new BossDefinition(source.Id, names[0], names.Skip(1).ToList());
    }

    public BossDefinition? Localize(string id, string englishName, IReadOnlyList<string> englishAliases, string gameLanguage)
        => Localize(new BossDefinition(id, englishName, englishAliases), gameLanguage);

    public IReadOnlyList<BossDefinition> LocalizeAll(IEnumerable<BossDefinition> source, string gameLanguage)
        => source.Select(x => Localize(x, gameLanguage)).Where(x => x is not null).Select(x => x!).ToList();

    public int CountCoverage(IEnumerable<BossDefinition> source, string gameLanguage)
        => LocalizeAll(source, gameLanguage).Count;

    private static bool TryNames(BossLocalizationEntry entry, string lang, out List<string> names)
    {
        foreach (var pair in entry.Names)
        {
            if (!string.Equals(pair.Key, lang, StringComparison.OrdinalIgnoreCase)) continue;
            names = pair.Value.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToList();
            return names.Count > 0;
        }
        names = new List<string>();
        return false;
    }
}

public sealed class BossLocalizationEntry
{
    public string Id { get; set; } = "";
    public Dictionary<string, List<string>> Names { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
