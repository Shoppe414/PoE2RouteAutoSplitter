using System.Text;

namespace PoE2RouteSetup;

public static class PackageData
{
    public static (string PackageRoot, string UserRoot, string ManifestPath) LocatePackage()
    {
        var starts = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var start in starts)
        {
            var dir = new DirectoryInfo(Path.GetFullPath(start));
            for (var i = 0; dir is not null && i < 10; i++, dir = dir.Parent)
            {
                // Normal packaged layout:
                // <release>\1 - User Setup\PoE2RouteSetup.exe
                // <release>\2 - Support Files\Setup UI [Configuration]\ui-manifest.json
                var supportRoot = Path.Combine(dir.FullName, "2 - Support Files");
                var packagedManifest = Path.Combine(supportRoot, "Setup UI [Configuration]", "ui-manifest.json");
                if (File.Exists(packagedManifest))
                    return (supportRoot, Path.Combine(dir.FullName, "1 - User Setup"), packagedManifest);

                // Source/debug launch from inside 2 - Support Files\Setup UI [Configuration].
                var localManifest = Path.Combine(dir.FullName, "ui-manifest.json");
                if (File.Exists(localManifest) && string.Equals(dir.Name, "Setup UI [Configuration]", StringComparison.OrdinalIgnoreCase))
                {
                    var support = dir.Parent?.FullName ?? throw new DirectoryNotFoundException("Could not locate the support-files directory.");
                    var release = dir.Parent?.Parent?.FullName ?? throw new DirectoryNotFoundException("Could not locate the release root.");
                    return (support, Path.Combine(release, "1 - User Setup"), localManifest);
                }
            }
        }
        throw new FileNotFoundException("Could not locate 2 - Support Files\\Setup UI [Configuration]\\ui-manifest.json. Keep PoE2RouteSetup.exe inside 1 - User Setup beside 2 - Support Files.");
    }

    public static List<RouteEntry> LoadAreas(string path)
    {
        var rows = new List<RouteEntry>();
        foreach (var raw in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var cols = ParseCsvLine(raw);
            if (cols.Count < 3) continue;
            rows.Add(new RouteEntry { Type = "area", Group = NormalizeAreaGroup(cols[0]), Id = cols[1], Name = cols[2] });
        }
        return rows;
    }

    public static List<RouteEntry> LoadBosses(string bossCatalogPath, string supportOnlyPath)
    {
        var supportOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(supportOnlyPath))
        {
            foreach (var raw in File.ReadLines(supportOnlyPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var parts = line.Split('|');
                if (parts.Length > 0) supportOnly.Add(parts[0].Trim());
            }
        }

        var rows = new List<RouteEntry>();
        var group = "Bosses";
        foreach (var raw in File.ReadLines(bossCatalogPath))
        {
            var line = raw.Trim();
            if (line.StartsWith("# Act ", StringComparison.OrdinalIgnoreCase)) group = line.TrimStart('#', ' ');
            else if (line.StartsWith("# Interlude", StringComparison.OrdinalIgnoreCase)) group = line.TrimStart('#', ' ');
            else if (line.StartsWith("# Pinnacle", StringComparison.OrdinalIgnoreCase)) group = line.TrimStart('#', ' ');
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length < 2) continue;
            var id = parts[0].Trim();
            if (supportOnly.Contains(id)) continue;
            rows.Add(new RouteEntry { Type = "boss", Group = group, Id = id, Name = parts[1].Trim() });
        }
        return rows;
    }

    private static string NormalizeAreaGroup(string raw) => raw switch
    {
        "1" => "Act 1",
        "2" => "Act 2",
        "3" => "Act 3",
        "4" => "Act 4",
        _ => raw
    };

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else current.Append(c);
        }
        fields.Add(current.ToString());
        return fields;
    }
}
