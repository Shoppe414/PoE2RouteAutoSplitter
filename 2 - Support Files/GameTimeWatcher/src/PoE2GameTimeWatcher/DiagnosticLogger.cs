using System.Globalization;
using System.Text;

namespace PoE2GameTimeWatcher;

/// <summary>
/// Best-effort internal diagnostics. Failures in diagnostic logging are intentionally
/// swallowed so the diagnostic layer can never become the reason the watcher exits.
/// </summary>
public sealed class DiagnosticLogger
{
    private readonly object _gate = new();
    private readonly string? _logPath;

    public string? DirectoryPath { get; }
    public bool Enabled => _logPath is not null;

    public DiagnosticLogger(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;

        try
        {
            DirectoryPath = Path.GetFullPath(directory);
            Directory.CreateDirectory(DirectoryPath);
            _logPath = Path.Combine(DirectoryPath, "watcher-internal.log");
        }
        catch
        {
            DirectoryPath = null;
            _logPath = null;
        }
    }

    public void InstallGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LogException("UNHANDLED_EXCEPTION", ex);
            else
                Log("UNHANDLED_EXCEPTION", e.ExceptionObject?.ToString() ?? "<null>");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException("UNOBSERVED_TASK_EXCEPTION", e.Exception);
            e.SetObserved();
        };
    }

    public void Log(string kind, string message = "")
    {
        if (_logPath is null) return;
        try
        {
            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                + " " + kind
                + (string.IsNullOrWhiteSpace(message) ? "" : " | " + message)
                + Environment.NewLine;
            lock (_gate)
                File.AppendAllText(_logPath, line, new UTF8Encoding(false));
        }
        catch { }
    }

    public void LogException(string kind, Exception ex)
    {
        Log(kind, ex.GetType().FullName + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
    }
}
