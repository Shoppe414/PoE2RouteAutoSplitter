using System.Diagnostics;
using System.Drawing;

namespace PoE2BossWatcher;

internal static class Program
{
    private const string Version = "0.3.7-height-relative-boss-geometry";

    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        var devConsole = args.Any(a => string.Equals(a, "--dev-console", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--dev", StringComparison.OrdinalIgnoreCase));
        var eventFileOverride = GetArgValue(args, "--event-file");
        var contextFileOverride = GetArgValue(args, "--context-file");
        var settingsFile = GetArgValue(args, "--settings");
        var mapDatabaseOverride = GetArgValue(args, "--map-db");
        var localizationDatabaseOverride = GetArgValue(args, "--localization-db");
        var diagnosticDirectoryOverride = GetArgValue(args, "--diagnostic-dir");
        Console.Title = devConsole ? "PoE2 BossWatcher 0.3.7 - DEV" : "PoE2 BossWatcher 0.3.7";
        var baseDir = AppContext.BaseDirectory;
        var configPath = Path.Combine(baseDir, "config.json");

        try
        {
            var config = AppConfig.Load(configPath);
            var settingsStatus = RuntimeSettingsOverlay.Apply(config, settingsFile);
            var bossListPath = Path.GetFullPath(Path.IsPathRooted(config.BossListFile)
                ? config.BossListFile
                : Path.Combine(baseDir, config.BossListFile));
            var bosses = BossDefinitionLoader.Load(bossListPath);

            var localizationDatabasePath = string.IsNullOrWhiteSpace(localizationDatabaseOverride)
                ? Path.GetFullPath(Path.IsPathRooted(config.BossLocalizationDatabaseFile)
                    ? config.BossLocalizationDatabaseFile
                    : Path.Combine(baseDir, config.BossLocalizationDatabaseFile))
                : Path.GetFullPath(localizationDatabaseOverride);
            var localizations = BossLocalizationDatabase.Load(localizationDatabasePath);
            var localizedBosses = localizations.LocalizeAll(bosses, config.GameLanguage);
            var matcher = new BossNameMatcher(localizedBosses);

            var mapDatabasePath = string.IsNullOrWhiteSpace(mapDatabaseOverride)
                ? Path.GetFullPath(Path.IsPathRooted(config.MapBossDatabaseFile)
                    ? config.MapBossDatabaseFile
                    : Path.Combine(baseDir, config.MapBossDatabaseFile))
                : Path.GetFullPath(mapDatabaseOverride);
            var mapDatabase = MapBossDatabase.Load(mapDatabasePath);

            var tessParent = Path.GetFullPath(Path.IsPathRooted(config.TessdataParent)
                ? config.TessdataParent
                : Path.Combine(baseDir, config.TessdataParent));
            using var ocr = new OcrService(tessParent, config.GameLanguage);

            var eventPath = string.IsNullOrWhiteSpace(eventFileOverride)
                ? PathResolver.ResolveEventFile(config, baseDir)
                : Path.GetFullPath(eventFileOverride);
            var diagnosticDirectory = string.IsNullOrWhiteSpace(diagnosticDirectoryOverride)
                ? null
                : Path.GetFullPath(diagnosticDirectoryOverride);
            if (diagnosticDirectory is not null) Directory.CreateDirectory(diagnosticDirectory);
            var events = new EventWriter(eventPath, devConsole, diagnosticDirectory);
            var contextPath = string.IsNullOrWhiteSpace(contextFileOverride)
                ? Path.Combine(Path.GetDirectoryName(eventPath)!, "poe2_boss_context.txt")
                : Path.GetFullPath(contextFileOverride);
            var contextReader = new BossContextReader(contextPath);
            var currentContext = contextReader.Read();
            var debugDir = diagnosticDirectory is not null
                ? Path.Combine(diagnosticDirectory, "images")
                : Path.GetFullPath(Path.IsPathRooted(config.DebugDirectory)
                    ? config.DebugDirectory
                    : Path.Combine(baseDir, config.DebugDirectory));
            var imageWriter = new DebugImageWriter(debugDir);
            var tracker = new BossEncounterTracker(config, events, imageWriter, ocr, matcher);
            var mapTracker = new GenericMapBossTracker(config, events, imageWriter, ocr, mapDatabase, localizations, config.GameLanguage);
            var finder = new GameWindowFinder(config.ProcessNames);
            var capture = new ScreenCapture();

            if (devConsole)
            {
                Console.WriteLine($"PoE2 BossWatcher {Version}");
                Console.WriteLine("Console mode: DEVELOPMENT (verbose frame diagnostics)");
                Console.WriteLine($"Boss definitions: {bosses.Count} canonical | {localizedBosses.Count} OCR-eligible for {ocr.Language.DisplayName}");
                Console.WriteLine($"PoE2 game language: {ocr.Language.DisplayName} ({ocr.Language.Code}) | Tesseract={ocr.Language.TesseractCode}");
                Console.WriteLine($"Boss localization database: {localizations.DatabaseVersion} | entries={localizations.Bosses.Count} | path={localizationDatabasePath}");
                Console.WriteLine($"Map boss database: {mapDatabase.DatabaseVersion} | entries={mapDatabase.Maps.Count} | path={mapDatabasePath}");
                Console.WriteLine($"Event file: {eventPath}");
                Console.WriteLine($"Debug log: {events.DebugPath}");
                Console.WriteLine($"Context file: {contextPath}");
                Console.WriteLine($"Runtime settings: {settingsStatus}");
                Console.WriteLine($"Detection context: {currentContext.Summary}");
                Console.WriteLine("Map context: authoritative database OCR identity is required. Unknown or special maps do not arm until a dedicated completion policy is available.");
                Console.WriteLine("Identity context: OCR uses the selected PoE2 game language and only verified localized names. Missing localization coverage is not guessed.");
                Console.WriteLine($"Boss capture: centered X; width={config.BossCaptureWidthHeightRatio:F4} x client-height; Y={config.BossRoi.Y:P1}, H={config.BossRoi.Height:P1}");
                Console.WriteLine("Window capture: PoE2 CLIENT AREA only (title bar, borders, taskbar, and the rest of the desktop are excluded).");
                Console.WriteLine($"Single OCR gate: redRun>={config.OcrTriggerRedRunFraction:P0}, name>={config.OcrMinNameGoldPixelFraction:P1}, frames={config.OcrCandidateConsecutiveFrames}");
                Console.WriteLine($"Dual topology: lane anchors>={config.DualLayoutMinLaneNameGoldFraction:P1}, center gap<={config.DualLayoutMaxCenterNameGoldFraction:P1}, lane health>={config.DualLayoutMinLaneHealthRunFraction:P0}, initial frames={config.DualLayoutConfirmFrames}");
                Console.WriteLine($"Dual add hardening: run>={config.DualAddMinCombinedHealthRunFraction:P0}, frames={config.DualAddConfirmFrames}");
                Console.WriteLine($"Dual removal: UI-presence only, confirm={config.DualRemovalConfirmMs}ms; survivor OCR for recentered bars; 2->0 confirm={config.DualBothGoneConfirmMs}ms; health fill is diagnostic only");
                Console.WriteLine($"OCR acquisition: fresh-frame cycle={config.OcrAcquisitionCycleMs}ms; gold -> broad -> temporal({config.OcrTemporalFrameCount} frames after {config.OcrTemporalFallbackAfterFailedCycles} failed cycles)");
                Console.WriteLine($"OCR burst window={config.OcrBurstDurationMs}ms, retry cooldown={config.OcrRetryCooldownMs}ms; failed-input diagnostics after {config.OcrFailureDiagnosticAfterCycles} cycles");
                Console.WriteLine($"Single gone confirmation: learned boss-name UI template only; health/run metrics diagnostic, confirm={config.DisappearConfirmMs}ms");
                Console.WriteLine($"Map gone confirmation: in-map={config.MapDisappearConfirmMs}ms; trusted external-exit assist after >={config.MapExitAssistMinMissingMs}ms already missing; first-missing timestamp is preserved for backdating");
                Console.WriteLine("Topology is evaluated before disappearance logic, but SINGLE->DUAL requires a strong persistent health structure.");
                Console.WriteLine("Dual names are OCRed independently from LEFT/RIGHT half-lanes; resolved lanes stop OCR immediately.");
                Console.WriteLine("Live OCR uses narrow gold first, broader lane-local text second, then short temporal composites only after repeated failures.");
                Console.WriteLine("Broad gold/frame metric remains diagnostic only.");
                Console.WriteLine("Keys: [S] save current ROI/mask, [R] reset tracking, [Q] quit");
            }
            else
            {
                Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] BossWatcher started. Press [Q] to quit.");
            }
            if (devConsole) Console.WriteLine();
            events.Debug($"SETTINGS | {settingsStatus}");
            events.Debug($"OCR_LANGUAGE | gameLanguage={ocr.Language.Code} | display={ocr.Language.DisplayName} | tesseract={ocr.Language.TesseractCode}" +
                $" | canonicalBosses={bosses.Count} | localizedCoverage={localizedBosses.Count} | localizationDb={localizations.DatabaseVersion}");
            events.Debug($"READY | v={Version} | bosses={bosses.Count} | ocrBosses={localizedBosses.Count} | gameLanguage={ocr.Language.Code} | tesseract={ocr.Language.TesseractCode} | mapDbVersion={mapDatabase.DatabaseVersion} | mapDbEntries={mapDatabase.Maps.Count} | eventFile={eventPath} | contextFile={contextPath} | context={currentContext.Summary} | goneConfirmMs={config.DisappearConfirmMs} | mapGoneConfirmMs={config.MapDisappearConfirmMs} | mapExitAssistMinMissingMs={config.MapExitAssistMinMissingMs}");

            GameWindowInfo? game = null;
            Bitmap? lastRaw = null;
            var nextFind = DateTimeOffset.MinValue;
            var nextConsole = DateTimeOffset.MinValue;
            var lastPeriodicSave = DateTimeOffset.MinValue;
            var frameDelay = TimeSpan.FromMilliseconds(1000.0 / config.CaptureFps);

            long capturedFrames = 0;
            DateTimeOffset lastSampleAt = DateTimeOffset.MinValue;
            DateTimeOffset hzWindowStart = DateTimeOffset.Now;
            long hzWindowFrames = 0;
            double measuredCaptureHz = 0;
            double sampleDtMs = 0;
            var lastGeometryClientWidth = -1;
            var lastGeometryClientHeight = -1;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            while (!cts.IsCancellationRequested)
            {
                var loopStart = Stopwatch.GetTimestamp();
                var now = DateTimeOffset.Now;

                var nextContext = contextReader.Read();
                if (nextContext != currentContext)
                {
                    events.Debug($"BOSS_CONTEXT_CHANGE | from={currentContext.Summary} | to={nextContext.Summary}");
                    tracker.ResetTracking("boss context changed");
                    mapTracker.HandleContextChange(now, nextContext);
                    currentContext = nextContext;
                }

                HandleKeys(cts, tracker, mapTracker, imageWriter, lastRaw, config, devConsole);
                if (cts.IsCancellationRequested) break;

                if (game is null || now >= nextFind)
                {
                    var found = finder.Find();
                    if (found is null)
                    {
                        if (game is not null) events.Debug("GAME_LOST");
                        game = null;
                        nextFind = now.AddSeconds(1);
                    }
                    else if (game is null || found.ProcessId != game.ProcessId)
                    {
                        game = found;
                        tracker.ResetTracking("game process changed");
                        mapTracker.ResetTracking("game process changed");
                        events.Debug($"GAME_FOUND | process={game.ProcessName} | pid={game.ProcessId}");
                    }
                }

                if (game is null)
                {
                    if (devConsole && now >= nextConsole)
                    {
                        Console.Write($"\r[{now:HH:mm:ss.fff}] State=WAIT_GAME                                                                                                                         ");
                        nextConsole = DateTimeOffset.Now.AddMilliseconds(config.ConsoleUpdateMs);
                    }
                    await DelayRemaining(loopStart, frameDelay, cts.Token);
                    continue;
                }

                using var result = capture.CaptureBossRoi(game, config, config.RequireGameForeground);
                if (result is null)
                {
                    tracker.SuspendCapture();
                    mapTracker.SuspendCapture();
                    if (devConsole && now >= nextConsole)
                    {
                        Console.Write($"\r[{now:HH:mm:ss.fff}] State=SUSPENDED (PoE2 not foreground)                                                                                                 ");
                        nextConsole = DateTimeOffset.Now.AddMilliseconds(config.ConsoleUpdateMs);
                    }
                    await DelayRemaining(loopStart, frameDelay, cts.Token);
                    continue;
                }

                if (devConsole &&
                    (result.ClientWidth != lastGeometryClientWidth || result.ClientHeight != lastGeometryClientHeight))
                {
                    lastGeometryClientWidth = result.ClientWidth;
                    lastGeometryClientHeight = result.ClientHeight;
                    var gcd = GreatestCommonDivisor(result.ClientWidth, result.ClientHeight);
                    var leftLane = ScreenCapture.ToPixelRect(result.Bitmap.Width, result.Bitmap.Height, ScreenCapture.GetBossNameLaneRoi(config, BossLane.Left));
                    var rightLane = ScreenCapture.ToPixelRect(result.Bitmap.Width, result.Bitmap.Height, ScreenCapture.GetBossNameLaneRoi(config, BossLane.Right));
                    Console.WriteLine();
                    Console.WriteLine($"DISPLAY_GEOMETRY | client={result.ClientWidth}x{result.ClientHeight} | aspect={result.ClientWidth / gcd}:{result.ClientHeight / gcd} | bossCapture={result.Bitmap.Width}x{result.Bitmap.Height} centered | dualLeft=x{leftLane.Left}-{leftLane.Right} | dualRight=x{rightLane.Left}-{rightLane.Right}");
                    events.Debug($"DISPLAY_GEOMETRY | client={result.ClientWidth}x{result.ClientHeight} | aspect={result.ClientWidth / gcd}:{result.ClientHeight / gcd} | bossCapture={result.Bitmap.Width}x{result.Bitmap.Height} | dualLeft={leftLane.X},{leftLane.Y},{leftLane.Width},{leftLane.Height} | dualRight={rightLane.X},{rightLane.Y},{rightLane.Width},{rightLane.Height}");
                }

                // Timestamp the pixels immediately after capture.
                now = DateTimeOffset.Now;
                capturedFrames++;
                hzWindowFrames++;
                if (lastSampleAt != DateTimeOffset.MinValue)
                    sampleDtMs = (now - lastSampleAt).TotalMilliseconds;
                lastSampleAt = now;
                var hzElapsed = (now - hzWindowStart).TotalSeconds;
                if (hzElapsed >= 1.0)
                {
                    measuredCaptureHz = hzWindowFrames / hzElapsed;
                    hzWindowFrames = 0;
                    hzWindowStart = now;
                }

                var consoleDue = devConsole && now >= nextConsole;
                if (devConsole && (lastRaw is null || consoleDue))
                {
                    lastRaw?.Dispose();
                    lastRaw = (Bitmap)result.Bitmap.Clone();
                }

                // Name, health-run, lane anchors and dual topology are always evaluated.
                // Expensive broad diagnostics are only collected at console cadence.
                var metrics = BossBarMetrics.Analyze(result.Bitmap, config, includeDiagnostics: consoleDue);
                if (currentContext.Mode == BossDetectionMode.Map)
                    mapTracker.Observe(now, result.Bitmap, metrics, currentContext);
                else if (currentContext.Mode == BossDetectionMode.Identity)
                    tracker.Observe(now, result.Bitmap, metrics);

                if (config.SaveDebugFrameEverySeconds > 0 && now >= lastPeriodicSave.AddSeconds(config.SaveDebugFrameEverySeconds))
                {
                    imageWriter.Save("PERIODIC", result.Bitmap, null);
                    lastPeriodicSave = now;
                }

                if (consoleDue)
                {
                    var mapMode = currentContext.Mode == BossDetectionMode.Map;
                    var recentOcr = !mapMode && (now - tracker.LastOcrAt).TotalSeconds <= 3 ? Trim(tracker.LastOcr, 28) : "-";
                    var recentMatch = !mapMode && (now - tracker.LastOcrAt).TotalSeconds <= 3 ? Trim(tracker.LastMatch, 20) : "-";
                    var recentSource = !mapMode && (now - tracker.LastOcrAt).TotalSeconds <= 3 ? Trim(tracker.LastOcrSource, 14) : "-";
                    var tracked = mapMode
                        ? (mapTracker.IsTracking ? (mapTracker.TrackedBossName.Length > 0 ? mapTracker.TrackedBossName : $"Map Boss #{currentContext.MapBossNumber}") : "-")
                        : Trim(tracker.TrackedSummary, 34);
                    var template = mapMode
                        ? (mapTracker.IsDual ? "dual-ui" : mapTracker.IsTracking ? "structural" : "-")
                        : tracker.IsDualMode
                            ? $"L{tracker.LeftTemplateCoverage:P0}/R{tracker.RightTemplateCoverage:P0}"
                            : tracker.SingleUsingTemplate ? $"{tracker.SingleTemplateCoverage:P0}" : "-";
                    var recentRun = mapMode || tracker.IsDualMode || !tracker.IsTrackingAny ? "-" : $"{tracker.SingleRecentRunReference:P1}";
                    var drop = mapMode || tracker.IsDualMode || !tracker.IsTrackingAny ? "-" : $"{tracker.SingleRunDropRatio:F2}";
                    var collapse = !mapMode && !tracker.IsDualMode && tracker.SingleRunCollapse ? "Y" : "-";
                    var dual = metrics.DualNameSignature ? "Y" : "-";
                    var state = currentContext.Mode == BossDetectionMode.Off ? "OFF" : mapMode ? mapTracker.StateLabel : tracker.StateLabel;
                    var contextLabel = currentContext.Mode == BossDetectionMode.Map ? "MAP" : currentContext.Mode == BossDetectionMode.Off ? "OFF" : "OCR";

                    Console.Write(
                        $"\r[{now:HH:mm:ss.fff}] Ctx={contextLabel,-3} State={state,-15}" +
                        $" track={Trim(tracked, 34),-34}" +
                        $" dual={dual}" +
                        $" run={metrics.HealthRedRunFraction,6:P1}" +
                        $" laneRun=L{metrics.LeftHealthRedRunFraction,5:P0}/R{metrics.RightHealthRedRunFraction,5:P0}" +
                        $" laneName=L{metrics.LeftLaneNameGoldFraction,5:P1}/C{metrics.CenterNameGoldFraction,5:P1}/R{metrics.RightLaneNameGoldFraction,5:P1}" +
                        $" ref={recentRun,6} drop={drop,5} tmpl={template,11} collapse={collapse}" +
                        $" name={metrics.NameGoldFraction,6:P1} gold={metrics.FrameGoldFraction,6:P1}" +
                        $" hz={measuredCaptureHz,4:F1} dt={sampleDtMs,5:F0}ms" +
                        $" OCR={recentOcr,-28} match={recentMatch,-20}" +
                        $" src={recentSource,-14}" +
                        $" tries={tracker.OcrAttempts,4} burst={tracker.BurstAttempts,2}   ");
                    nextConsole = DateTimeOffset.Now.AddMilliseconds(config.ConsoleUpdateMs);
                }

                await DelayRemaining(loopStart, frameDelay, cts.Token);
            }

            lastRaw?.Dispose();
            events.Debug($"SHUTDOWN | frames={capturedFrames} | ocrAttempts={tracker.OcrAttempts}");
            if (devConsole) Console.WriteLine("\nStopped.");
            else Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] BossWatcher stopped.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FATAL: " + ex);
            try
            {
                var fatalDirectory = string.IsNullOrWhiteSpace(diagnosticDirectoryOverride)
                    ? baseDir
                    : Path.GetFullPath(diagnosticDirectoryOverride);
                Directory.CreateDirectory(fatalDirectory);
                File.WriteAllText(Path.Combine(fatalDirectory, "poe2_boss_watcher_fatal.log"), ex.ToString());
            }
            catch { }
            Console.Error.WriteLine();
            Console.Error.WriteLine("Press any key to close...");
            try { Console.ReadKey(true); } catch { }
            return 1;
        }
    }

    private static void HandleKeys(CancellationTokenSource cts, BossEncounterTracker tracker, GenericMapBossTracker mapTracker, DebugImageWriter writer, Bitmap? raw, AppConfig config, bool devConsole)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Q) cts.Cancel();
            else if (devConsole && key == ConsoleKey.R)
            {
                tracker.ResetTracking("manual R key");
                mapTracker.ResetTracking("manual R key");
            }
            else if (devConsole && key == ConsoleKey.S && raw is not null)
            {
                using var gold = ScreenCapture.PreprocessBossNameForOcr(raw, config, BossLane.Single, OcrPreprocessMode.Gold);
                using var broad = ScreenCapture.PreprocessBossNameForOcr(raw, config, BossLane.Single, OcrPreprocessMode.Broad);
                writer.SaveOcrDiagnostic("MANUAL", raw, gold, broad, null, null);
                Console.WriteLine("\nSaved current ROI + gold/broad OCR masks to debug folder.");
            }
        }
    }

    private static async Task DelayRemaining(long startTimestamp, TimeSpan target, CancellationToken token)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var remaining = target - elapsed;
        if (remaining > TimeSpan.Zero) await Task.Delay(remaining, token);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            var t = a % b;
            a = b;
            b = t;
        }
        return Math.Max(1, a);
    }

    private static string? GetArgValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "…";
}
