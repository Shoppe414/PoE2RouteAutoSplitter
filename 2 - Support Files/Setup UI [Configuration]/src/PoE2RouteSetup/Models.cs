using System.Text.Json;

namespace PoE2RouteSetup;

public sealed class SetupManifest
{
    public string Version { get; set; } = "";
    public string AreaCatalog { get; set; } = "";
    public string BossCatalog { get; set; } = "";
    public string BossSupportOnlyList { get; set; } = "";
    public string CustomAslSource { get; set; } = "";
    public string BossWatcherDirectory { get; set; } = "";
    public List<PresetDefinition> Presets { get; set; } = [];

    public static SetupManifest Load(string path)
    {
        var manifest = JsonSerializer.Deserialize<SetupManifest>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return manifest ?? throw new InvalidOperationException("Could not parse setup UI manifest.");
    }
}

public sealed class PresetDefinition
{
    public string DisplayName { get; set; } = "";
    public string Group { get; set; } = "";
    public string LssSource { get; set; } = "";
    public string AslSource { get; set; } = "";
    public List<RuntimeFileDefinition> RuntimeFiles { get; set; } = [];
    public bool RequiresBossWatcher { get; set; }
    public string Description { get; set; } = "";

    public override string ToString() => DisplayName;
}

public sealed class RuntimeFileDefinition
{
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}

public sealed class RouteEntry
{
    public string Type { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Group { get; init; } = "";
    public string DisplayText => $"{Group} — {Name}  [{Id}]";
    public string RouteText => $"{Type}|{Id}";
    public override string ToString() => DisplayText;
}

public sealed class StartAreaOption
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string DisplayText => string.IsNullOrEmpty(Id) ? "Manual start" : $"{Name}  [{Id}]";
    public override string ToString() => DisplayText;
}
