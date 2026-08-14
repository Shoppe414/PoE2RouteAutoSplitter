using System.Diagnostics;

namespace PoE2GameTimeWatcher;

public sealed class GameWindowFinder
{
    private readonly string[] _processNames;
    public GameWindowFinder(IEnumerable<string> processNames) => _processNames = processNames.ToArray();

    public GameWindowInfo? Find()
    {
        foreach (var processName in _processNames)
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { continue; }

            GameWindowInfo? found = null;
            try
            {
                foreach (var process in processes.OrderBy(p => p.Id))
                {
                    try
                    {
                        if (process.HasExited) continue;
                        var handle = process.MainWindowHandle;
                        if (handle == IntPtr.Zero) continue;
                        if (NativeMethods.IsIconic(handle)) continue;
                        found = new GameWindowInfo(process.Id, process.ProcessName, handle);
                        break;
                    }
                    catch { }
                }
            }
            finally
            {
                // Process.GetProcessesByName returns disposable Process wrappers. The
                // watcher scans repeatedly, so retaining these wrappers can leak native
                // process handles over a long run. Dispose every wrapper before returning.
                foreach (var process in processes)
                {
                    try { process.Dispose(); }
                    catch { }
                }
            }

            if (found is not null) return found;
        }
        return null;
    }

    public static string? TryResolveClientLog(GameWindowInfo window, string overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath)) return Path.GetFullPath(Environment.ExpandEnvironmentVariables(overridePath));
        try
        {
            using var process = Process.GetProcessById(window.ProcessId);
            var exe = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exe)) return null;
            return Path.Combine(Path.GetDirectoryName(exe)!, "logs", "Client.txt");
        }
        catch { return null; }
    }
}
