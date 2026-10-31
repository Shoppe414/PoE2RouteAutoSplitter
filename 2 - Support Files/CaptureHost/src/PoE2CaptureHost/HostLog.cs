namespace PoE2CaptureHost;

internal sealed class HostLog
{
    private readonly string? _path;
    private readonly object _gate = new();

    public HostLog(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "poe2_capturehost_debug.log");
    }

    public void Write(string message)
    {
        if (_path is null) return;
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
        lock (_gate)
        {
            try { File.AppendAllText(_path, line); }
            catch { }
        }
    }
}
