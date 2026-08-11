using System.Diagnostics;

namespace PoE2BossWatcher;

public sealed class GameWindowFinder
{
    private readonly string[] _processNames;

    public GameWindowFinder(IEnumerable<string> processNames)
    {
        _processNames = processNames.ToArray();
    }

    public GameWindowInfo? Find()
    {
        foreach (var processName in _processNames)
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { continue; }

            foreach (var process in processes.OrderBy(p => p.Id))
            {
                try
                {
                    if (process.HasExited || process.MainWindowHandle == IntPtr.Zero) continue;
                    if (NativeMethods.IsIconic(process.MainWindowHandle)) continue;
                    return new GameWindowInfo(process.Id, process.ProcessName, process.MainWindowHandle);
                }
                catch { }
            }
        }
        return null;
    }
}

public sealed record GameWindowInfo(int ProcessId, string ProcessName, IntPtr Handle);
