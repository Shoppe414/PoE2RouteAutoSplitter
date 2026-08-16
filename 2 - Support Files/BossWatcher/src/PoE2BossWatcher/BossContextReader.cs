namespace PoE2BossWatcher;

public enum BossDetectionMode
{
    Identity,
    Map,
    Off
}

public sealed record BossContextState(
    BossDetectionMode Mode,
    string AreaId,
    int AreaLevel,
    int MapBossNumber,
    string Classification)
{
    public static BossContextState IdentityDefault { get; } = new(BossDetectionMode.Identity, "", 0, 0, "default");

    public string Summary => Mode switch
    {
        BossDetectionMode.Map => $"MAP area={AreaId} level={AreaLevel} boss#{MapBossNumber}",
        BossDetectionMode.Off => "OFF",
        _ => AreaId.Length > 0 ? $"IDENTITY area={AreaId} level={AreaLevel}" : "IDENTITY"
    };
}

/// <summary>
/// Reads the ASL-owned boss-detection context file. Missing files intentionally
/// default to identity/OCR mode so every existing non-Maps setup remains backwards
/// compatible. Transient read/write races reuse the last successfully parsed state.
/// </summary>
public sealed class BossContextReader
{
    private readonly string _path;
    private BossContextState _lastGood = BossContextState.IdentityDefault;

    public BossContextReader(string path) => _path = System.IO.Path.GetFullPath(path);

    public string Path => _path;

    public BossContextState Read()
    {
        if (!File.Exists(_path))
            return _lastGood;

        try
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in File.ReadAllLines(_path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                var sep = line.IndexOf('=');
                if (sep <= 0) continue;
                fields[line[..sep].Trim()] = line[(sep + 1)..].Trim();
            }

            // A truncate/write race can expose an empty or partial file for a few ms.
            // Do not turn that transient into an identity-mode flip while a map boss is active.
            if (!fields.TryGetValue("mode", out var parsedMode) || string.IsNullOrWhiteSpace(parsedMode))
                return _lastGood;

            var modeText = parsedMode;
            var mode = modeText.Equals("map", StringComparison.OrdinalIgnoreCase)
                ? BossDetectionMode.Map
                : modeText.Equals("off", StringComparison.OrdinalIgnoreCase)
                    ? BossDetectionMode.Off
                    : BossDetectionMode.Identity;

            var areaId = fields.TryGetValue("areaId", out var parsedArea) ? parsedArea : "";
            var classification = fields.TryGetValue("classification", out var parsedClass) ? parsedClass : "";
            var areaLevel = fields.TryGetValue("areaLevel", out var parsedLevel) && int.TryParse(parsedLevel, out var level) ? level : 0;
            var mapBossNumber = fields.TryGetValue("mapBossNumber", out var parsedNumber) && int.TryParse(parsedNumber, out var number) ? number : 0;

            // Map mode is actionable, so require the complete map payload. This prevents
            // a partial truncate/write read from arming an unnumbered or wrong map encounter.
            if (mode == BossDetectionMode.Map &&
                (string.IsNullOrWhiteSpace(areaId) || areaLevel <= 0 || mapBossNumber <= 0))
                return _lastGood;

            _lastGood = new BossContextState(mode, areaId, areaLevel, mapBossNumber, classification);
            return _lastGood;
        }
        catch (IOException)
        {
            return _lastGood;
        }
        catch (UnauthorizedAccessException)
        {
            return _lastGood;
        }
    }
}
