using System.Globalization;
using System.Text;

namespace PoE2GameTimeWatcher;

public sealed class StateWriter
{
    private readonly string _path;
    private readonly string _logPath;

    public StateWriter(string path, string? logDirectory = null)
    {
        _path = Path.GetFullPath(path);
        var stateDirectory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(stateDirectory);

        var resolvedLogDirectory = string.IsNullOrWhiteSpace(logDirectory)
            ? stateDirectory
            : Path.GetFullPath(logDirectory);
        Directory.CreateDirectory(resolvedLogDirectory);
        _logPath = Path.Combine(resolvedLogDirectory, "poe2_gametimewatcher.log");
    }

    public void Write(string state, string reason, long stateSequence, long originUtcTicks, double pauseScore, double mtxScore)
    {
        var now = DateTime.UtcNow;
        var text = new StringBuilder()
            .AppendLine("version=2")
            .AppendLine("state=" + state)
            .AppendLine("reason=" + reason)
            .AppendLine("heartbeatUtcTicks=" + now.Ticks.ToString(CultureInfo.InvariantCulture))
            .AppendLine("stateSequence=" + stateSequence.ToString(CultureInfo.InvariantCulture))
            .AppendLine("originUtcTicks=" + originUtcTicks.ToString(CultureInfo.InvariantCulture))
            .AppendLine("pauseMenuScore=" + pauseScore.ToString("F4", CultureInfo.InvariantCulture))
            .AppendLine("mtxShopScore=" + mtxScore.ToString("F4", CultureInfo.InvariantCulture))
            .ToString();

        var temp = _path + "." + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) +
                   "." + Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture) + ".tmp";
        try
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    File.WriteAllText(temp, text, new UTF8Encoding(false));
                    File.Move(temp, _path, true);
                    return;
                }
                catch (IOException) when (attempt < 7)
                {
                    Thread.Sleep(10);
                }
                catch (UnauthorizedAccessException) when (attempt < 7)
                {
                    Thread.Sleep(10);
                }
            }
        }
        finally
        {
            try
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
            catch { }
        }
    }

    public void Log(string message)
    {
        File.AppendAllText(_logPath,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + message + Environment.NewLine,
            new UTF8Encoding(false));
    }
}
