namespace PoE2BossWatcher;

public sealed record BossDefinition(string Id, string Name, IReadOnlyList<string> Aliases)
{
    public IEnumerable<string> AllNames()
    {
        yield return Name;
        foreach (var alias in Aliases) yield return alias;
    }
}

public static class BossDefinitionLoader
{
    public static IReadOnlyList<BossDefinition> Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Boss list not found", path);
        var result = new List<BossDefinition>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;

            var parts = line.Split('|');
            if (parts.Length < 2) throw new FormatException($"Invalid boss line: {raw}");
            var id = parts[0].Trim();
            var name = parts[1].Trim();
            var aliases = parts.Length >= 3
                ? parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : Array.Empty<string>();

            if (!ids.Add(id)) throw new FormatException($"Duplicate boss id: {id}");
            result.Add(new BossDefinition(id, name, aliases));
        }

        if (result.Count == 0) throw new InvalidOperationException("bosses.txt contains no bosses.");
        return result;
    }
}
