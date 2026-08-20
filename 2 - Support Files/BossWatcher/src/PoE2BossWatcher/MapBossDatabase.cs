using System.Text;
using System.Text.Json;

namespace PoE2BossWatcher;

public sealed class MapBossDatabase
{
    public int SchemaVersion { get; set; } = 1;
    public string DatabaseVersion { get; set; } = "";
    public string Purpose { get; set; } = "";
    public MapBossDatabaseSource Source { get; set; } = new();
    public List<MapBossEntry> Maps { get; set; } = new();
    public List<MapEventBossEntry> EventBosses { get; set; } = new();

    private Dictionary<string, MapBossEntry> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public static MapBossDatabase Load(string path)
    {
        if (!File.Exists(path))
            return Empty($"database missing: {path}");

        try
        {
            var json = File.ReadAllText(path);
            var db = JsonSerializer.Deserialize<MapBossDatabase>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? new MapBossDatabase();

            db.Maps ??= new List<MapBossEntry>();
            db.EventBosses ??= new List<MapEventBossEntry>();
            foreach (var map in db.Maps)
            {
                map.AreaIds ??= new List<string>();
                map.Bosses ??= new List<MapBossIdentity>();
                foreach (var boss in map.Bosses)
                    boss.Aliases ??= new List<string>();
            }
            foreach (var boss in db.EventBosses)
                boss.Aliases ??= new List<string>();

            db.BuildIndex();
            return db;
        }
        catch (Exception ex)
        {
            return Empty($"database parse failed: {ex.Message}");
        }
    }

    private static MapBossDatabase Empty(string status)
    {
        var db = new MapBossDatabase
        {
            DatabaseVersion = "unavailable",
            Purpose = status
        };
        db.BuildIndex();
        return db;
    }

    private void BuildIndex()
    {
        _byKey = new Dictionary<string, MapBossEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Maps)
        {
            AddKey(entry.MapName, entry);
            foreach (var areaId in entry.AreaIds)
                AddKey(areaId, entry);
        }
    }

    private void AddKey(string? key, MapBossEntry entry)
    {
        var normalized = NormalizeAreaKey(key);
        if (normalized.Length == 0) return;
        _byKey[normalized] = entry;
    }

    public MapBossEntry? Resolve(string? areaId)
    {
        var key = NormalizeAreaKey(areaId);
        return key.Length > 0 && _byKey.TryGetValue(key, out var entry) ? entry : null;
    }

    public IReadOnlyList<BossDefinition> GetExpectedDefinitions(MapBossEntry entry)
        => entry.Bosses
            .Where(b => !string.IsNullOrWhiteSpace(b.Id) && !string.IsNullOrWhiteSpace(b.Name))
            .Select(b => new BossDefinition(b.Id.Trim(), b.Name.Trim(), b.Aliases))
            .ToList();

    public IReadOnlyList<BossDefinition> GetExpectedDefinitions(MapBossEntry entry, BossLocalizationDatabase localizations, string gameLanguage)
        => GetExpectedDefinitions(entry)
            .Select(b => localizations.Localize(b, gameLanguage))
            .Where(b => b is not null)
            .Select(b => b!)
            .ToList();

    public IReadOnlyList<BossDefinition> GetEventDefinitions()
        => EventBosses
            .Where(b => !string.IsNullOrWhiteSpace(b.Id) && !string.IsNullOrWhiteSpace(b.Name))
            .Select(b => new BossDefinition(b.Id.Trim(), b.Name.Trim(), b.Aliases))
            .ToList();

    public IReadOnlyList<BossDefinition> GetEventDefinitions(BossLocalizationDatabase localizations, string gameLanguage)
        => GetEventDefinitions()
            .Select(b => localizations.Localize(b, gameLanguage))
            .Where(b => b is not null)
            .Select(b => b!)
            .ToList();

    public MapEventBossEntry? FindEventBoss(string id)
        => EventBosses.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));

    public static string NormalizeAreaKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var text = value.Trim();
        if (text.StartsWith("Map", StringComparison.OrdinalIgnoreCase) && text.Length > 3)
            text = text[3..];

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }
}

public sealed class MapBossDatabaseSource
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Retrieved { get; set; } = "";
    public string Status { get; set; } = "";
}

public sealed class MapBossEntry
{
    public string MapName { get; set; } = "";
    public List<string> AreaIds { get; set; } = new();
    public string CompletionType { get; set; } = "boss";
    public string BossRule { get; set; } = "any";
    public List<MapBossIdentity> Bosses { get; set; } = new();
    public string SourceStatus { get; set; } = "";
    public string? Notes { get; set; }

    public bool HasDeterministicBosses
        => CompletionType.Equals("boss", StringComparison.OrdinalIgnoreCase) && Bosses.Count > 0;

    public bool RequiresAllBosses
        => BossRule.Equals("all", StringComparison.OrdinalIgnoreCase) && Bosses.Count > 1;
}

public class MapBossIdentity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
}

public sealed class MapEventBossEntry : MapBossIdentity
{
    public string Mechanic { get; set; } = "";
}
