using System.Text;

namespace PoE2BossWatcher;

public sealed class EventWriter
{
    private readonly object _gate = new();
    private readonly bool _devConsole;
    private readonly Dictionary<string, DateTimeOffset> _encounterStarts = new(StringComparer.OrdinalIgnoreCase);

    public string EventPath { get; }
    public string DebugPath { get; }

    public EventWriter(string eventPath, bool devConsole = false)
    {
        _devConsole = devConsole;
        EventPath = eventPath;
        Directory.CreateDirectory(Path.GetDirectoryName(eventPath)!);
        DebugPath = Path.Combine(Path.GetDirectoryName(eventPath)!, "poe2_boss_watcher_debug.log");
        AppendShared(EventPath, $"# PoE2BossWatcher session {DateTimeOffset.Now:O}{Environment.NewLine}");
    }

    public void BossSeen(BossDefinition boss, DateTimeOffset when, OcrRead ocr, BossBarMetrics metrics, double match, BossLane lane = BossLane.Single)
    {
        lock (_gate)
            _encounterStarts[boss.Id] = when;

        WriteEvent(when, "SEEN", boss,
            $"lane={lane.ToString().ToLowerInvariant()}|match={match:F3}|ocrConf={ocr.Confidence:F3}|red={metrics.RedFraction:F4}|light={metrics.LightFraction:F4}|sceneGold={metrics.FrameGoldFraction:F4}|nameGold={metrics.NameGoldFraction:F4}|redRun={metrics.HealthRedRunFraction:F4}|ocr={Clean(ocr.Text)}",
            userMessage: $"Encountered: {boss.Name}");
    }

    public void BossGone(BossDefinition boss, DateTimeOffset firstMissing, DateTimeOffset verifyStarted, DateTimeOffset confirmed, int confirmMs)
    {
        var preVerifyBackdateMs = Math.Max(0, (verifyStarted - firstMissing).TotalMilliseconds);
        DateTimeOffset? encounterStart;
        lock (_gate)
        {
            _encounterStarts.TryGetValue(boss.Id, out var start);
            encounterStart = start == default ? null : start;
            _encounterStarts.Remove(boss.Id);
        }

        var fightSeconds = encounterStart.HasValue
            ? Math.Max(0, (firstMissing - encounterStart.Value).TotalSeconds)
            : (double?)null;

        var timingExtra = encounterStart.HasValue
            ? $"|encounterStart={encounterStart.Value:O}|fightSeconds={fightSeconds!.Value:F3}"
            : "|encounterStart=unknown|fightSeconds=unknown";

        WriteEvent(confirmed, "GONE", boss,
            $"firstMissing={firstMissing:O}|verifyStarted={verifyStarted:O}|confirmMs={confirmMs}|preVerifyBackdateMs={preVerifyBackdateMs:F1}{timingExtra}",
            displayWhen: firstMissing,
            userMessage: fightSeconds.HasValue
                ? $"Defeated: {boss.Name} | Fight time: {fightSeconds.Value:F3} s"
                : $"Defeated: {boss.Name} | Fight time: unavailable");
    }

    public void BossReturned(BossDefinition boss, DateTimeOffset when)
        => WriteEvent(when, "RETURNED", boss, "missingWindowCancelled=true", printToUserConsole: false);

    public void Debug(string message)
    {
        lock (_gate)
            AppendShared(DebugPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
    }

    private void WriteEvent(
        DateTimeOffset when,
        string type,
        BossDefinition boss,
        string extra,
        bool printToUserConsole = true,
        DateTimeOffset? displayWhen = null,
        string? userMessage = null)
    {
        var line = $"{when:O}|{type}|{boss.Id}|{Clean(boss.Name)}|{extra}";
        lock (_gate)
        {
            AppendShared(EventPath, line + Environment.NewLine);
            AppendShared(DebugPath, line + Environment.NewLine);
        }

        if (_devConsole)
        {
            Console.WriteLine(line);
        }
        else if (printToUserConsole)
        {
            var stamp = displayWhen ?? when;
            Console.WriteLine($"[{stamp:HH:mm:ss.fff}] {userMessage ?? $"{type}: {boss.Name}"}");
        }
    }

    private static void AppendShared(string path, string text)
    {
        // Allow LiveSplit's ASL reader and the PowerShell test writer to have the file open.
        // Small retry window handles rare writer/writer collisions without losing an event.
        var bytes = Encoding.UTF8.GetBytes(text);
        Exception? last = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                return;
            }
            catch (IOException ex)
            {
                last = ex;
                Thread.Sleep(15 * (attempt + 1));
            }
        }
        throw new IOException($"Could not append to '{path}' after retries.", last);
    }

    private static string Clean(string value) => value.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
}
