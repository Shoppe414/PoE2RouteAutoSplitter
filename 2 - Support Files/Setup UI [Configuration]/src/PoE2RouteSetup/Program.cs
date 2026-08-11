namespace PoE2RouteSetup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                ReportFatalError(e.Exception);
                Application.Exit();
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                    ReportFatalError(exception);
            };

            Application.Run(new SetupForm());
        }
        catch (Exception exception)
        {
            ReportFatalError(exception);
        }
    }

    private static void ReportFatalError(Exception exception)
    {
        var report = $"""
            PoE2 Route AutoSplitter Setup encountered an unexpected error.

            Time: {DateTimeOffset.Now:O}

            {exception}
            """;

        var logPath = TryWriteCrashLog(report);

        try
        {
            var logMessage = logPath is null
                ? "A crash log could not be written."
                : $"Crash details were written to:\n{logPath}";

            MessageBox.Show(
                $"PoE2 Route AutoSplitter Setup could not continue.\n\n{exception.Message}\n\n{logMessage}",
                "PoE2 Route AutoSplitter Setup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Avoid masking the original failure if Windows cannot display a message box.
        }
    }

    private static string? TryWriteCrashLog(string report)
    {
        var candidates = new List<string>();
        try
        {
            var located = PackageData.LocatePackage();
            candidates.Add(Path.Combine(Path.GetDirectoryName(located.ManifestPath)!, "PoE2RouteSetup-crash.log"));
        }
        catch
        {
            // Package discovery may itself be the startup failure; temp remains available below.
        }
        candidates.Add(Path.Combine(Path.GetTempPath(), "PoE2RouteSetup-crash.log"));

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                File.WriteAllText(path, report);
                return path;
            }
            catch
            {
                // Try the next writable location.
            }
        }

        return null;
    }
}
