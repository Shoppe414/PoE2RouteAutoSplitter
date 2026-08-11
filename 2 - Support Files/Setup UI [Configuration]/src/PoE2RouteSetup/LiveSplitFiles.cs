using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PoE2RouteSetup;

public static class LiveSplitFiles
{
    private static readonly Regex LiveSplitPathRegex = new(
        "System\\.IO\\.Path\\.Combine\\(vars\\.liveSplitDir,\\s*\"([^\"]+)\"\\)",
        RegexOptions.Compiled);

    public static string RewriteRuntimePaths(string aslText, string targetDir)
    {
        return LiveSplitPathRegex.Replace(aslText, match =>
        {
            var path = Path.Combine(targetDir, match.Groups[1].Value);
            return QuoteCSharp(path);
        });
    }

    public static void WriteCustomSplits(string outputPath, IReadOnlyList<RouteEntry> objectives)
    {
        var segments = new XElement("Segments");
        for (var i = 0; i < objectives.Count; i++)
        {
            var objective = objectives[i];
            var typeLabel = objective.Type.Equals("boss", StringComparison.OrdinalIgnoreCase) ? "Boss" : "Area";
            segments.Add(new XElement("Segment",
                new XElement("Name", $"{objective.Name} [{typeLabel}]"),
                new XElement("Icon"),
                new XElement("SplitTimes", new XElement("SplitTime", new XAttribute("name", "Personal Best"))),
                new XElement("BestSegmentTime"),
                new XElement("SegmentHistory")));
        }

        var run = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Run", new XAttribute("version", "1.7.0"),
                new XElement("GameIcon"),
                new XElement("GameName", "Path of Exile 2"),
                new XElement("CategoryName", "Custom Route - Exploration + Boss Rush"),
                new XElement("Metadata",
                    new XElement("Run", new XAttribute("id", "")),
                    new XElement("Platform", new XAttribute("usesEmulator", "False"), "PC"),
                    new XElement("Region"),
                    new XElement("Variables")),
                new XElement("Offset", "00:00:00"),
                new XElement("AttemptCount", 0),
                segments));
        run.Save(outputPath);
    }

    private static string QuoteCSharp(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
