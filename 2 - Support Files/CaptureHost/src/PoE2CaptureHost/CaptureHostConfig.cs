using System.Text.Json;

namespace PoE2CaptureHost;

internal sealed class CaptureHostConfig
{
    public string[] ProcessNames { get; private init; } = [];
    public double BossCaptureWidthHeightRatio { get; private init; } = 8.0 / 9.0;
    public double BossRoiY { get; private init; } = 0.015;
    public double BossRoiHeight { get; private init; } = 0.115;
    public int CanonicalHeight { get; private init; } = 576;

    public int CanonicalWidth => Math.Max(1, (int)Math.Round(CanonicalHeight * (16.0 / 9.0)));

    public static CaptureHostConfig Load(string bossConfigPath, string gameTimeConfigPath)
    {
        var jsonOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };
        using var boss = JsonDocument.Parse(File.ReadAllText(bossConfigPath), jsonOptions);
        using var gameTime = JsonDocument.Parse(File.ReadAllText(gameTimeConfigPath), jsonOptions);

        var names = ReadStringArray(boss.RootElement, "ProcessNames");
        if (names.Length == 0)
            names = ReadStringArray(gameTime.RootElement, "ProcessNames");
        if (names.Length == 0)
            throw new InvalidOperationException("CaptureHost could not read any PoE2 process names from watcher configuration.");

        var ratio = ReadDouble(boss.RootElement, "BossCaptureWidthHeightRatio", 8.0 / 9.0);
        var roiY = 0.015;
        var roiHeight = 0.115;
        if (boss.RootElement.TryGetProperty("BossRoi", out var roi) && roi.ValueKind == JsonValueKind.Object)
        {
            roiY = ReadDouble(roi, "Y", roiY);
            roiHeight = ReadDouble(roi, "Height", roiHeight);
        }

        var canonicalHeight = ReadInt(gameTime.RootElement, "CanonicalHeight", 576);
        return new CaptureHostConfig
        {
            ProcessNames = names,
            BossCaptureWidthHeightRatio = Math.Clamp(ratio, 0.40, 2.00),
            BossRoiY = Math.Clamp(roiY, 0.0, 1.0),
            BossRoiHeight = Math.Clamp(roiHeight, 0.001, 1.0),
            CanonicalHeight = Math.Clamp(canonicalHeight, 240, 2160)
        };
    }

    public CaptureRect GetBossRect(int width, int height)
    {
        var roiWidth = Math.Clamp((int)Math.Round(height * BossCaptureWidthHeightRatio), 1, width);
        var roiHeight = Math.Clamp((int)Math.Round(height * BossRoiHeight), 1, height);
        var x = Math.Max(0, (width - roiWidth) / 2);
        var y = Math.Clamp((int)Math.Round(height * BossRoiY), 0, Math.Max(0, height - roiHeight));
        return new CaptureRect(x, y, roiWidth, roiHeight);
    }

    public CaptureRect GetPauseViewport(int width, int height)
    {
        var targetAspect = 16.0 / 9.0;
        var clientAspect = width / (double)height;
        if (clientAspect >= targetAspect)
        {
            var sourceWidth = Math.Clamp((int)Math.Round(height * targetAspect), 1, width);
            return new CaptureRect((width - sourceWidth) / 2, 0, sourceWidth, height);
        }

        var sourceHeight = Math.Clamp((int)Math.Round(width / targetAspect), 1, height);
        return new CaptureRect(0, (height - sourceHeight) / 2, width, sourceHeight);
    }

    private static string[] ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];
        return value.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static double ReadDouble(JsonElement root, string name, double fallback)
        => root.TryGetProperty(name, out var value) && value.TryGetDouble(out var parsed) ? parsed : fallback;

    private static int ReadInt(JsonElement root, string name, int fallback)
        => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;
}

internal readonly record struct CaptureRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
