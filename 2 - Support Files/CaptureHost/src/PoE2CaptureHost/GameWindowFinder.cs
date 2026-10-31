using System.Diagnostics;

namespace PoE2CaptureHost;

internal sealed record GameWindowInfo(int ProcessId, string ProcessName, IntPtr Handle);

internal sealed class GameWindowFinder
{
    private readonly string[] _processNames;

    public GameWindowFinder(IEnumerable<string> processNames)
        => _processNames = processNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public GameWindowInfo? Find()
    {
        foreach (var processName in _processNames)
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { continue; }

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        if (process.HasExited) continue;
                        var hwnd = process.MainWindowHandle;
                        if (hwnd == IntPtr.Zero) continue;
                        return new GameWindowInfo(process.Id, process.ProcessName, hwnd);
                    }
                    catch { }
                }
            }
        }
        return null;
    }

    public static bool IsStillValid(GameWindowInfo window)
    {
        try
        {
            using var process = Process.GetProcessById(window.ProcessId);
            return !process.HasExited && process.MainWindowHandle == window.Handle && window.Handle != IntPtr.Zero;
        }
        catch { return false; }
    }
}
