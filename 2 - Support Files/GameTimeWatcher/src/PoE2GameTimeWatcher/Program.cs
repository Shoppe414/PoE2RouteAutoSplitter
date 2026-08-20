using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace PoE2GameTimeWatcher;

internal static class Program
{
    private const string Version = "0.4.5-structure-first";

    [STAThread]
    private static int Main(string[] args)
    {
        DiagnosticLogger? diagnostics = null;
        string? startupStateFile = null;
        bool waitOnError = args.Any(a => a.Equals("--wait-on-error", StringComparison.OrdinalIgnoreCase));
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var configPath = ResolveConfigPath(baseDir, GetArg(args, "--config"));
            var settingsFile = GetArg(args, "--settings");
            var stateFile = GetArg(args, "--state-file");
            startupStateFile = stateFile;
            var testImage = GetArg(args, "--test-image");
            var diagnosticDir = GetArg(args, "--diagnostic-dir");
            var diagnosticImageDir = GetArg(args, "--diagnostic-image-dir");
            var devConsole = args.Any(a => a.Equals("--dev-console", StringComparison.OrdinalIgnoreCase));

            diagnostics = new DiagnosticLogger(diagnosticDir);
            diagnostics.InstallGlobalHandlers();
            var diagnosticImageDirectory = string.IsNullOrWhiteSpace(diagnosticImageDir) ? diagnostics.DirectoryPath : Path.GetFullPath(diagnosticImageDir);
            if (!string.IsNullOrWhiteSpace(diagnosticImageDirectory)) Directory.CreateDirectory(diagnosticImageDirectory);
            diagnostics.Log("STARTUP",
                $"version={Version} pid={Environment.ProcessId} baseDir={baseDir} " +
                $"os={Environment.OSVersion} process64={Environment.Is64BitProcess} args={string.Join(" ", args)}");

            var config = AppConfig.Load(configPath);
            var settingsStatus = RuntimeSettingsOverlay.Apply(config, settingsFile);
            diagnostics.Log("SETTINGS_OVERLAY", settingsStatus);
            var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? baseDir;
            diagnostics.Log("CONFIG_LOADED",
                $"path={Path.GetFullPath(configPath)} fps={config.CaptureFps} fastFps={config.FastCaptureFps} " +
                $"inputFastMs={config.InputFastModeMs} inputHintMs={config.InputHintWindowMs} heartbeatMs={config.HeartbeatMs} provisionalTimeoutMs={config.ProvisionalTimeoutMs} " +
                $"stackThreshold={config.PauseStackThreshold:F4} resumeThreshold={config.ResumeGameThreshold:F4} bannerThreshold={config.PauseBannerThreshold:F4} " +
                $"exitThreshold={config.ExitPathOfExileThreshold:F4} mtxThreshold={config.MtxShopThreshold:F4} " +
                $"gameLanguage={config.GameLanguage} detectorPolicy=structure-first/banner-second/text-low-weight " +
                $"foregroundRequired={config.RequireForegroundForNewDetection}");

            using var matcher = new TemplateMatcher(config, configDir);
            diagnostics.Log("TEMPLATES_LOADED",
                $"stack={config.PauseStackTemplate} resume={config.ResumeGameTemplate} banner={config.PauseBannerTemplate} exit={config.ExitPathOfExileTemplate} " +
                $"mtx={config.MtxShopTemplate} canonicalHeight={config.CanonicalHeight}");

            if (!string.IsNullOrWhiteSpace(testImage))
            {
                using var bitmap = new Bitmap(Path.GetFullPath(testImage));
                var result = matcher.Analyze(bitmap);
                Console.WriteLine(
                    $"state={result.State} pause={result.PauseMenuScore:F4} stack={result.PauseStackScore:F4} resume={result.ResumeGameScore:F4} " +
                    $"banner={result.PauseBannerScore:F4} exit={result.ExitPathOfExileScore:F4} mtx={result.MtxShopScore:F4} content={result.ContentBounds}");
                diagnostics.Log("TEST_IMAGE_RESULT",
                    $"state={result.State} pause={result.PauseMenuScore:F4} stack={result.PauseStackScore:F4} resume={result.ResumeGameScore:F4} " +
                    $"banner={result.PauseBannerScore:F4} exit={result.ExitPathOfExileScore:F4} mtx={result.MtxShopScore:F4} content={result.ContentBounds}");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(stateFile))
            {
                Console.Error.WriteLine("PoE2GameTimeWatcher requires --state-file <path> when running normally.");
                diagnostics.Log("ARGUMENT_ERROR", "missing --state-file");
                return 2;
            }

            var statePath = Path.GetFullPath(stateFile);
            var stateDirectory = Path.GetDirectoryName(statePath)!;
            var userSetupDirectory = Directory.GetParent(stateDirectory)?.FullName;
            var releaseRoot = userSetupDirectory is null ? null : Directory.GetParent(userSetupDirectory)?.FullName;
            var runtimeDiagnosticDirectory = !string.IsNullOrWhiteSpace(diagnosticDir)
                ? Path.GetFullPath(diagnosticDir)
                : releaseRoot is null
                    ? stateDirectory
                    : Path.Combine(releaseRoot, "4-README's_and_Diagnostics", "Diagnostics");
            var writer = new StateWriter(stateFile, runtimeDiagnosticDirectory);
            var finder = new GameWindowFinder(config.ProcessNames);
            var capture = new ScreenCapture();

            Console.WriteLine($"PoE2 GameTimeWatcher v{Version} - optional manual-pause helper");
            Console.WriteLine("Detects manual pause / MTX state with structure-first visual confirmation.");
            Console.WriteLine("Loading-screen Game Time is handled directly by the ASL from Client.txt.");
            Console.WriteLine($"State file: {Path.GetFullPath(stateFile)}");
            Console.WriteLine($"Runtime settings: {settingsStatus}");
            Console.WriteLine($"PoE2 game language: {config.GameLanguage} (pause structure is primary; paused-state banner is secondary; English text templates are low-weight only)");
            if (diagnostics.Enabled) Console.WriteLine($"Diagnostic directory: {diagnostics.DirectoryPath} | images: {diagnosticImageDirectory}");
            if (!devConsole) Console.WriteLine("Use --dev-console to show detector scores continuously.");

            ManualPauseVisualState stableState = ManualPauseVisualState.Running;
            ManualPauseVisualState candidateState = ManualPauseVisualState.Running;
            int candidateFrames = 0;
            double lastPauseScore = 0;
            double lastStackScore = 0;
            double lastResumeScore = 0;
            double lastBannerScore = 0;
            double lastExitScore = 0;
            double lastMtxScore = 0;
            var nextCapture = DateTime.UtcNow;
            var fastModeUntil = DateTime.MinValue;
            var lastMenuInputUtc = DateTime.MinValue;
            long lastSeenMenuInputSequence = 0;
            var nextDevStatus = DateTime.MinValue;
            var nextDiagnosticStatus = DateTime.MinValue;
            var nextCandidateScreenshot = DateTime.MinValue;
            DateTime menuProbeStart = DateTime.MinValue;
            int menuProbeIndex = -1;
            int[] menuProbeDelaysMs = [100, 250, 500, 1000];
            double lastAnalyzeMs = 0;
            Rectangle lastContentBounds = Rectangle.Empty;
            int? lastPid = null;
            GameWindowInfo? currentWindow = null;
            var nextWindowScan = DateTime.MinValue;

            // The state file has two layers:
            //   visual state: the last fully confirmed PoE2 surface
            //   wire state: RUNNING / PAUSED / PENDING_PAUSE / PENDING_RUN
            // PENDING states let LiveSplit react immediately to an ESC/Start edge while
            // the visual detector verifies the result. The ASL refunds/re-removes the
            // provisional interval if verification rejects it.
            var heartbeatSync = new object();
            bool heartbeatGamePresent = false;
            bool heartbeatForeground = false;
            ManualPauseVisualState heartbeatState = ManualPauseVisualState.Running;
            string heartbeatWireState = "RUNNING";
            string heartbeatReason = "watcher-start";
            long heartbeatOriginUtcTicks = DateTime.UtcNow.Ticks;
            long heartbeatStateSequence = 0;
            double heartbeatPauseScore = 0;
            double heartbeatMtxScore = 0;

            var heartbeatSignal = new AutoResetEvent(false);

            void PublishWireState(string wireState, string reason, long originUtcTicks)
            {
                bool changed = false;
                lock (heartbeatSync)
                {
                    if (!string.Equals(heartbeatWireState, wireState, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(heartbeatReason, reason, StringComparison.OrdinalIgnoreCase))
                    {
                        heartbeatWireState = wireState;
                        heartbeatReason = reason;
                        heartbeatOriginUtcTicks = originUtcTicks > 0 ? originUtcTicks : DateTime.UtcNow.Ticks;
                        heartbeatStateSequence++;
                        changed = true;
                    }
                }
                if (changed) heartbeatSignal.Set();
            }

            (string State, long OriginTicks, long Sequence) GetWireSnapshot()
            {
                lock (heartbeatSync)
                    return (heartbeatWireState, heartbeatOriginUtcTicks, heartbeatStateSequence);
            }

            // One writer thread owns the state file. It refreshes the heartbeat periodically
            // and also wakes immediately on provisional/confirmed transitions.
            var heartbeatThread = new Thread(() =>
            {
                while (true)
                {
                    heartbeatSignal.WaitOne(config.HeartbeatMs);
                    bool gamePresent;
                    string wireState;
                    string reason;
                    long originTicks;
                    long stateSequence;
                    double pauseScore;
                    double mtxScore;
                    lock (heartbeatSync)
                    {
                        gamePresent = heartbeatGamePresent;
                        wireState = heartbeatWireState;
                        reason = heartbeatReason;
                        originTicks = heartbeatOriginUtcTicks;
                        stateSequence = heartbeatStateSequence;
                        pauseScore = heartbeatPauseScore;
                        mtxScore = heartbeatMtxScore;
                    }

                    SafeStateWrite(writer, gamePresent ? wireState : "RUNNING",
                        gamePresent ? reason : "game-not-found", stateSequence, originTicks,
                        pauseScore, mtxScore, diagnostics, devConsole);
                }
            })
            {
                IsBackground = true,
                Name = "PoE2GameTimeWatcher-Heartbeat"
            };
            heartbeatThread.Start();

            // The input thread may publish a provisional state without waiting for image
            // analysis. It only does so while PoE2 is the foreground game. ESC/Start from
            // gameplay provisionally pauses; ESC/Start from the confirmed pause menu
            // provisionally resumes. ESC inside MTX remains paused because it normally
            // returns to the pause menu rather than gameplay.
            using var menuInputMonitor = new MenuInputMonitor(diagnostics, edge =>
            {
                bool gamePresent;
                bool foreground;
                ManualPauseVisualState visualState;
                lock (heartbeatSync)
                {
                    gamePresent = heartbeatGamePresent;
                    foreground = heartbeatForeground;
                    visualState = heartbeatState;
                }
                if (!gamePresent || !foreground) return;

                if (visualState == ManualPauseVisualState.Running)
                {
                    PublishWireState("PENDING_PAUSE", "input-pause-candidate", edge.Utc.Ticks);
                    diagnostics.Log("PROVISIONAL_STATE", $"state=PENDING_PAUSE source={edge.Source} origin={edge.Utc:O}");
                }
                else if (visualState == ManualPauseVisualState.PauseMenu)
                {
                    PublishWireState("PENDING_RUN", "input-resume-candidate", edge.Utc.Ticks);
                    diagnostics.Log("PROVISIONAL_STATE", $"state=PENDING_RUN source={edge.Source} origin={edge.Utc:O}");
                }
            });

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = false;
                SafeStateWrite(writer, "RUNNING", "watcher-exit", heartbeatStateSequence + 1, DateTime.UtcNow.Ticks,
                    lastPauseScore, lastMtxScore, diagnostics, devConsole);
                diagnostics.Log("CANCEL_KEY_PRESS");
            };

            while (true)
            {
                var now = DateTime.UtcNow;

                // ESC / controller Start is an acceleration hint, never pause authority.
                // Poll it on a dedicated 5 ms thread so a short key press cannot disappear
                // while this thread is busy analyzing a frame. v0.4.0 detected only one
                // MENU_INPUT edge in the supplied test because ~500 ms image analysis blocked
                // the polling loop. Visual confirmation remains mandatory.
                var menuInput = menuInputMonitor.Snapshot();
                if (menuInput.Sequence != lastSeenMenuInputSequence)
                {
                    lastSeenMenuInputSequence = menuInput.Sequence;
                    lastMenuInputUtc = menuInput.Utc;
                    fastModeUntil = now.AddMilliseconds(config.InputFastModeMs);
                    nextCapture = DateTime.MinValue;
                    if (diagnostics.Enabled)
                    {
                        menuProbeStart = now;
                        menuProbeIndex = 0;
                        diagnostics.Log("MENU_INPUT",
                            $"source={menuInput.Source} edgeAgeMs={(now - menuInput.Utc).TotalMilliseconds:F1} fastModeMs={config.InputFastModeMs}");
                    }
                }

                // Window/process discovery does not need to run at capture-frame frequency.
                // A 250 ms scan interval reduces Process wrapper/handle churn substantially.
                if (now >= nextWindowScan)
                {
                    nextWindowScan = now.AddMilliseconds(250);
                    currentWindow = finder.Find();
                }
                var window = currentWindow;
                bool recentMenuInput = lastMenuInputUtc != DateTime.MinValue &&
                    (now - lastMenuInputUtc).TotalMilliseconds <= config.InputHintWindowMs;
                bool fastCapture = stableState != ManualPauseVisualState.Running ||
                    candidateState != stableState || now < fastModeUntil;
                int activeCaptureFps = fastCapture ? config.FastCaptureFps : config.CaptureFps;
                if (window is null)
                {
                    if (lastPid.HasValue)
                    {
                        SafeStateLog(writer, "GAME_DISCONNECTED", diagnostics, devConsole);
                        diagnostics.Log("GAME_DISCONNECTED", $"pid={lastPid.Value}");
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} GAME_DISCONNECTED");
                    }
                    lastPid = null;
                    stableState = ManualPauseVisualState.Running;
                    candidateState = ManualPauseVisualState.Running;
                    candidateFrames = 0;
                    PublishWireState("RUNNING", "game-not-found", DateTime.UtcNow.Ticks);
                }
                else
                {
                    if (lastPid != window.ProcessId)
                    {
                        lastPid = window.ProcessId;
                        SafeStateLog(writer, $"GAME_CONNECTED process={window.ProcessName} pid={window.ProcessId}", diagnostics, devConsole);
                        diagnostics.Log("GAME_CONNECTED", $"process={window.ProcessName} pid={window.ProcessId} hwnd=0x{window.Handle.ToInt64():X}");
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} GAME_CONNECTED | {window.ProcessName} pid={window.ProcessId}");
                    }

                    bool foreground = NativeMethods.GetForegroundWindow() == window.Handle;
                    lock (heartbeatSync)
                    {
                        heartbeatGamePresent = true;
                        heartbeatForeground = foreground;
                        heartbeatState = stableState;
                    }

                    // A provisional input state is intentionally temporary. If the visual
                    // detector never confirms the requested transition, resolve it back to
                    // the last stable visual state. The ASL will refund/re-remove the
                    // provisional interval using the pending-state timestamps.
                    var wireBeforeCapture = GetWireSnapshot();
                    if (wireBeforeCapture.OriginTicks > 0 &&
                        (now.Ticks - wireBeforeCapture.OriginTicks) >= TimeSpan.FromMilliseconds(config.ProvisionalTimeoutMs).Ticks)
                    {
                        if (wireBeforeCapture.State == "PENDING_PAUSE" && stableState == ManualPauseVisualState.Running)
                        {
                            PublishWireState("RUNNING", "pause-candidate-rejected", wireBeforeCapture.OriginTicks);
                            diagnostics.Log("PROVISIONAL_REJECTED", $"state=PENDING_PAUSE ageMs={(now.Ticks - wireBeforeCapture.OriginTicks) / (double)TimeSpan.TicksPerMillisecond:F1}");
                        }
                        else if (wireBeforeCapture.State == "PENDING_RUN" && stableState != ManualPauseVisualState.Running)
                        {
                            PublishWireState("PAUSED", "resume-candidate-rejected", wireBeforeCapture.OriginTicks);
                            diagnostics.Log("PROVISIONAL_REJECTED", $"state=PENDING_RUN ageMs={(now.Ticks - wireBeforeCapture.OriginTicks) / (double)TimeSpan.TicksPerMillisecond:F1}");
                        }
                    }

                    if (now >= nextCapture && (!config.RequireForegroundForNewDetection || foreground))
                    {
                        nextCapture = now.AddMilliseconds(1000.0 / activeCaptureFps);
                        try
                        {
                            var frameStartUtc = DateTime.UtcNow;
                            using var cap = capture.CaptureRoi(window, new NormalizedRect(0, 0, 1, 1), false);
                            if (cap != null)
                            {
                                var analyzeWatch = Stopwatch.StartNew();
                                bool detailedScores = (devConsole && DateTime.UtcNow >= nextDevStatus) ||
                                    (diagnostics.Enabled && DateTime.UtcNow >= nextDiagnosticStatus);
                                var detection = matcher.Analyze(cap.Bitmap, stableState, recentMenuInput, detailedScores);
                                analyzeWatch.Stop();
                                lastAnalyzeMs = analyzeWatch.Elapsed.TotalMilliseconds;
                                lastPauseScore = detection.PauseMenuScore;
                                lastStackScore = detection.PauseStackScore;
                                lastResumeScore = detection.ResumeGameScore;
                                lastBannerScore = detection.PauseBannerScore;
                                lastExitScore = detection.ExitPathOfExileScore;
                                lastMtxScore = detection.MtxShopScore;
                                lastContentBounds = detection.ContentBounds;

                                var rawState = detection.State;

                                // If a visual is close to a threshold but does not classify,
                                // save an occasional center-column screenshot in diagnostic mode.
                                // This gives us the exact frame the watcher saw, not a separately
                                // captured screenshot from Windows/Steam.
                                var candidateNow = DateTime.UtcNow;
                                if (diagnostics.Enabled && diagnosticImageDirectory is not null &&
                                    candidateNow >= nextCandidateScreenshot &&
                                    stableState == ManualPauseVisualState.Running &&
                                    rawState == ManualPauseVisualState.Running &&
                                    !recentMenuInput &&
                                    (lastStackScore >= 0.50 || lastResumeScore >= 0.50 || lastBannerScore >= 0.30 ||
                                     lastExitScore >= 0.40 || lastMtxScore >= 0.60))
                                {
                                    nextCandidateScreenshot = candidateNow.AddSeconds(10);
                                    try
                                    {
                                        int cropWidth = Math.Min(cap.Bitmap.Width,
                                            Math.Max(1, (int)Math.Round(cap.Bitmap.Height * 1.8)));
                                        int cropX = Math.Max(0, (cap.Bitmap.Width - cropWidth) / 2);
                                        using var candidateBitmap = cap.Bitmap.Clone(
                                            new Rectangle(cropX, 0, cropWidth, cap.Bitmap.Height),
                                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                        var candidatePath = Path.Combine(diagnosticImageDirectory!,
                                            $"candidate-{DateTime.Now:yyyyMMdd-HHmmss-fff}-r{lastResumeScore:F3}-b{lastBannerScore:F3}-e{lastExitScore:F3}-m{lastMtxScore:F3}.png");
                                        candidateBitmap.Save(candidatePath, System.Drawing.Imaging.ImageFormat.Png);
                                        diagnostics.Log("CANDIDATE_SCREENSHOT",
                                            $"path={candidatePath} analyzeMs={lastAnalyzeMs:F1} client={cap.ClientWidth}x{cap.ClientHeight} " +
                                            $"content={detection.ContentBounds} stack={lastStackScore:F4} resume={lastResumeScore:F4} banner={lastBannerScore:F4} " +
                                            $"exit={lastExitScore:F4} mtx={lastMtxScore:F4}");
                                    }
                                    catch (Exception shotEx)
                                    {
                                        diagnostics.LogException("CANDIDATE_SCREENSHOT_ERROR", shotEx);
                                    }
                                }

                                // First visual evidence can also start a provisional transition.
                                // This covers mouse clicks such as Resume Game / Options / Challenges,
                                // where there is no ESC edge to timestamp. LiveSplit can react after
                                // this first frame while the normal multi-frame check verifies it.
                                if (rawState != stableState)
                                {
                                    var wireVisual = GetWireSnapshot();
                                    if (stableState == ManualPauseVisualState.Running &&
                                        rawState != ManualPauseVisualState.Running &&
                                        wireVisual.State == "RUNNING")
                                    {
                                        PublishWireState("PENDING_PAUSE", "visual-pause-candidate", frameStartUtc.Ticks);
                                        diagnostics.Log("PROVISIONAL_STATE", $"state=PENDING_PAUSE source=visual origin={frameStartUtc:O}");
                                    }
                                    else if (stableState != ManualPauseVisualState.Running &&
                                             rawState == ManualPauseVisualState.Running &&
                                             wireVisual.State == "PAUSED")
                                    {
                                        PublishWireState("PENDING_RUN", "visual-resume-candidate", frameStartUtc.Ticks);
                                        diagnostics.Log("PROVISIONAL_STATE", $"state=PENDING_RUN source=visual origin={frameStartUtc:O}");
                                    }
                                }

                                if (rawState == candidateState)
                                    candidateFrames++;
                                else
                                {
                                    candidateState = rawState;
                                    candidateFrames = 1;
                                }

                                int needed;
                                if (rawState == ManualPauseVisualState.Running)
                                {
                                    // ESC/controller Start while already paused is a strong unpause
                                    // intent, but a visual Running frame is still required.
                                    needed = stableState != ManualPauseVisualState.Running && recentMenuInput
                                        ? 1
                                        : config.ConfirmRunningFrames;
                                }
                                else
                                {
                                    // On an opening ESC/Start edge, one strong visual pause frame is
                                    // sufficient. Without an input hint retain normal multi-frame confirmation.
                                    needed = stableState == ManualPauseVisualState.Running && recentMenuInput
                                        ? 1
                                        : config.ConfirmPausedFrames;
                                }

                                if (candidateFrames >= needed && stableState != rawState)
                                {
                                    var old = stableState;
                                    stableState = rawState;
                                    fastModeUntil = DateTime.UtcNow.AddMilliseconds(config.InputFastModeMs);
                                    string reason = Reason(stableState);
                                    SafeStateLog(writer,
                                        $"STATE {old} -> {stableState} pauseScore={lastPauseScore:F4} stackScore={lastStackScore:F4} " +
                                        $"resumeScore={lastResumeScore:F4} bannerScore={lastBannerScore:F4} exitScore={lastExitScore:F4} mtxScore={lastMtxScore:F4}",
                                        diagnostics, devConsole);
                                    diagnostics.Log("STATE_CHANGE",
                                        $"old={old} new={stableState} reason={reason} pause={lastPauseScore:F4} stack={lastStackScore:F4} " +
                                        $"resume={lastResumeScore:F4} banner={lastBannerScore:F4} exit={lastExitScore:F4} mtx={lastMtxScore:F4} " +
                                        $"client={cap.ClientWidth}x{cap.ClientHeight} content={detection.ContentBounds} analyzeMs={lastAnalyzeMs:F1} " +
                                        $"inputAgeMs={(lastMenuInputUtc == DateTime.MinValue ? -1 : (DateTime.UtcNow - lastMenuInputUtc).TotalMilliseconds):F1} confirmFrames={needed}");
                                    // Resolve any provisional state before diagnostic PNG encoding.
                                    // In v0.4.1 full-resolution diagnostic screenshots could add
                                    // several hundred milliseconds before LiveSplit saw the state.
                                    var pendingAtResolution = GetWireSnapshot();
                                    long resolvedOrigin = (pendingAtResolution.State == "PENDING_PAUSE" || pendingAtResolution.State == "PENDING_RUN")
                                        ? pendingAtResolution.OriginTicks
                                        : frameStartUtc.Ticks;
                                    lock (heartbeatSync)
                                    {
                                        heartbeatGamePresent = true;
                                        heartbeatForeground = foreground;
                                        heartbeatState = stableState;
                                        heartbeatPauseScore = lastPauseScore;
                                        heartbeatMtxScore = lastMtxScore;
                                    }
                                    PublishWireState(stableState == ManualPauseVisualState.Running ? "RUNNING" : "PAUSED",
                                        reason, resolvedOrigin);

                                    Console.WriteLine(
                                        $"{DateTime.Now:HH:mm:ss.fff} {(stableState == ManualPauseVisualState.Running ? "RUNNING" : "PAUSED")} " +
                                        $"| reason={reason} | pause={lastPauseScore:F3} | stack={lastStackScore:F3} | resume={lastResumeScore:F3} " +
                                        $"| banner={lastBannerScore:F3} | exit={lastExitScore:F3} | mtx={lastMtxScore:F3}");

                                    if (diagnostics.Enabled && diagnosticImageDirectory is not null)
                                    {
                                        try
                                        {
                                            var safeOld = old.ToString();
                                            var safeNew = stableState.ToString();
                                            var shot = Path.Combine(diagnosticImageDirectory!,
                                                $"state-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{safeOld}-to-{safeNew}.png");
                                            cap.Bitmap.Save(shot, System.Drawing.Imaging.ImageFormat.Png);
                                            diagnostics.Log("STATE_SCREENSHOT", $"path={shot}");
                                        }
                                        catch (Exception shotEx) { diagnostics.LogException("STATE_SCREENSHOT_ERROR", shotEx); }
                                    }
                                }

                                // Diagnostic screenshots are intentionally last. They may take
                                // hundreds of milliseconds to PNG-encode at 5120x1440, but by this
                                // point provisional/confirmed wire state has already been published.
                                if (diagnostics.Enabled && diagnosticImageDirectory is not null &&
                                    menuProbeIndex >= 0 && menuProbeIndex < menuProbeDelaysMs.Length &&
                                    DateTime.UtcNow >= menuProbeStart.AddMilliseconds(menuProbeDelaysMs[menuProbeIndex]))
                                {
                                    try
                                    {
                                        var probePath = Path.Combine(diagnosticImageDirectory!,
                                            $"menu-probe-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{menuProbeDelaysMs[menuProbeIndex]}ms-" +
                                            $"s{lastStackScore:F3}-r{lastResumeScore:F3}-b{lastBannerScore:F3}-e{lastExitScore:F3}-m{lastMtxScore:F3}.png");
                                        cap.Bitmap.Save(probePath, System.Drawing.Imaging.ImageFormat.Png);
                                        diagnostics.Log("MENU_PROBE_SCREENSHOT",
                                            $"path={probePath} delayMs={menuProbeDelaysMs[menuProbeIndex]} client={cap.ClientWidth}x{cap.ClientHeight} " +
                                            $"content={detection.ContentBounds} stack={lastStackScore:F4} resume={lastResumeScore:F4} " +
                                            $"banner={lastBannerScore:F4} exit={lastExitScore:F4} mtx={lastMtxScore:F4}");
                                    }
                                    catch (Exception shotEx)
                                    {
                                        diagnostics.LogException("MENU_PROBE_SCREENSHOT_ERROR", shotEx);
                                    }
                                    menuProbeIndex++;
                                    if (menuProbeIndex >= menuProbeDelaysMs.Length) menuProbeIndex = -1;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            diagnostics.LogException("CAPTURE_ERROR", ex);
                            if (devConsole) Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} CAPTURE_ERROR | {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                    // If PoE2 loses foreground while paused, retain the last confirmed state.
                    // This is necessary for breaks where the runner pauses and alt-tabs away.
                }

                // Publish the latest stable result to the independent heartbeat
                // thread. It will continue refreshing the state file even while the
                // next screenshot is being analyzed.
                lock (heartbeatSync)
                {
                    heartbeatGamePresent = lastPid.HasValue;
                    heartbeatForeground = currentWindow is not null && NativeMethods.GetForegroundWindow() == currentWindow.Handle;
                    heartbeatState = stableState;
                    heartbeatPauseScore = lastPauseScore;
                    heartbeatMtxScore = lastMtxScore;
                }

                now = DateTime.UtcNow;
                if (devConsole && now >= nextDevStatus)
                {
                    nextDevStatus = now.AddSeconds(1);
                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff} STATUS | state={stableState} | pause={lastPauseScore:F4} | stack={lastStackScore:F4} " +
                        $"| resume={lastResumeScore:F4} | banner={lastBannerScore:F4} | exit={lastExitScore:F4} | mtx={lastMtxScore:F4} " +
                        $"| analyzeMs={lastAnalyzeMs:F1} | mode={(fastCapture ? "FAST" : "NORMAL")} | fps={activeCaptureFps} " +
                        $"| content={lastContentBounds} | pid={(lastPid?.ToString(CultureInfo.InvariantCulture) ?? "none")}");
                }

                if (diagnostics.Enabled && now >= nextDiagnosticStatus)
                {
                    nextDiagnosticStatus = now.AddSeconds(1);
                    diagnostics.Log("STATUS",
                        BuildRuntimeStatus(stableState, candidateState, candidateFrames,
                            lastPauseScore, lastStackScore, lastResumeScore, lastBannerScore, lastExitScore, lastMtxScore, lastAnalyzeMs, lastPid));
                }

                Thread.Sleep(10);
            }
        }
        catch (Exception ex)
        {
            diagnostics?.LogException("FATAL", ex);
            TryWriteStartupError(startupStateFile, ex);
            Console.Error.WriteLine(ex.ToString());
            if (waitOnError)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("GameTimeWatcher stopped with an error. Press Enter to close this window.");
                try { Console.ReadLine(); } catch { }
            }
            return 1;
        }
    }

    private static string ResolveConfigPath(string baseDir, string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var resolved = Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitPath));
            if (!File.Exists(resolved))
                throw new FileNotFoundException($"GameTimeWatcher config file was not found: {resolved}", resolved);
            return resolved;
        }

        var candidates = new List<string>
        {
            Path.Combine(baseDir, "config.json")
        };

        var trimmedBase = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var baseInfo = new DirectoryInfo(trimmedBase);
        if (baseInfo.Name.Equals("publish", StringComparison.OrdinalIgnoreCase) && baseInfo.Parent is not null)
            candidates.Add(Path.Combine(baseInfo.Parent.FullName, "config.json"));

        foreach (var candidate in candidates)
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);

        throw new FileNotFoundException(
            "GameTimeWatcher could not locate config.json. Checked: " + string.Join("; ", candidates.Select(Path.GetFullPath)));
    }

    private static void TryWriteStartupError(string? stateFile, Exception ex)
    {
        if (string.IsNullOrWhiteSpace(stateFile)) return;
        try
        {
            var stateDir = Path.GetDirectoryName(Path.GetFullPath(stateFile));
            if (string.IsNullOrWhiteSpace(stateDir)) return;

            // Normal SetupUI deployments place the state file under:
            // <release>\1 - User Setup\LiveSplit Target. Keep fatal startup logs with
            // the rest of the centralized diagnostics instead of polluting LiveSplit Target.
            var userSetupDir = Directory.GetParent(stateDir)?.FullName;
            var releaseRoot = userSetupDir is null ? null : Directory.GetParent(userSetupDir)?.FullName;
            var errorDir = releaseRoot is null
                ? stateDir
                : Path.Combine(releaseRoot, "4-README's_and_Diagnostics", "Diagnostics");
            Directory.CreateDirectory(errorDir);
            var path = Path.Combine(errorDir, "poe2_gametimewatcher_startup_error.log");
            File.AppendAllText(path,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                " FATAL STARTUP ERROR" + Environment.NewLine +
                ex + Environment.NewLine + Environment.NewLine,
                new UTF8Encoding(false));
        }
        catch { }
    }

    private static void SafeStateWrite(StateWriter writer, string state, string reason, long stateSequence, long originUtcTicks, double pauseScore, double mtxScore, DiagnosticLogger diagnostics, bool devConsole)
    {
        try
        {
            writer.Write(state, reason, stateSequence, originUtcTicks, pauseScore, mtxScore);
        }
        catch (Exception ex)
        {
            diagnostics.LogException("STATE_WRITE_ERROR", ex);
            if (devConsole) Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} STATE_WRITE_ERROR | {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void SafeStateLog(StateWriter writer, string message, DiagnosticLogger diagnostics, bool devConsole)
    {
        try
        {
            writer.Log(message);
        }
        catch (Exception ex)
        {
            diagnostics.LogException("STATE_LOG_ERROR", ex);
            if (devConsole) Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} STATE_LOG_ERROR | {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string BuildRuntimeStatus(
        ManualPauseVisualState stableState,
        ManualPauseVisualState candidateState,
        int candidateFrames,
        double pauseScore,
        double stackScore,
        double resumeScore,
        double bannerScore,
        double exitScore,
        double mtxScore,
        double analyzeMs,
        int? gamePid)
    {
        try
        {
            using var self = Process.GetCurrentProcess();
            return $"stable={stableState} candidate={candidateState} frames={candidateFrames} pause={pauseScore:F4} " +
                   $"stack={stackScore:F4} resume={resumeScore:F4} banner={bannerScore:F4} exit={exitScore:F4} mtx={mtxScore:F4} analyzeMs={analyzeMs:F1} " +
                   $"gamePid={(gamePid?.ToString(CultureInfo.InvariantCulture) ?? "none")} " +
                   $"workingSet={self.WorkingSet64} privateBytes={self.PrivateMemorySize64} handles={self.HandleCount} threads={self.Threads.Count} " +
                   $"managedBytes={GC.GetTotalMemory(false)} gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)} gen2={GC.CollectionCount(2)}";
        }
        catch (Exception ex)
        {
            return $"stable={stableState} candidate={candidateState} frames={candidateFrames} pause={pauseScore:F4} " +
                   $"stack={stackScore:F4} resume={resumeScore:F4} banner={bannerScore:F4} exit={exitScore:F4} mtx={mtxScore:F4} analyzeMs={analyzeMs:F1} " +
                   $"gamePid={(gamePid?.ToString(CultureInfo.InvariantCulture) ?? "none")} metricsError={ex.GetType().Name}:{ex.Message}";
        }
    }

    private static string Reason(ManualPauseVisualState state) => state switch
    {
        ManualPauseVisualState.PauseMenu => "pause-menu",
        ManualPauseVisualState.MtxShop => "mtx-shop",
        _ => "gameplay"
    };

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}
