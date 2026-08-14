using System.Diagnostics;

namespace PoE2BossWatcher;

public static class PathResolver
{
    public static string ResolveEventFile(AppConfig config, string baseDir)
    {
        if (!string.IsNullOrWhiteSpace(config.EventFile))
            return Path.GetFullPath(Path.IsPathRooted(config.EventFile) ? config.EventFile : Path.Combine(baseDir, config.EventFile));

        try
        {
            foreach (var p in Process.GetProcessesByName("LiveSplit"))
            {
                var exe = p.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exe))
                    return Path.Combine(Path.GetDirectoryName(exe)!, "poe2_boss_events.log");
            }
        }
        catch { }

        return Path.Combine(baseDir, "poe2_boss_events.log");
    }
}
