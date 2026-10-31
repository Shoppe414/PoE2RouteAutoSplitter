using System.Diagnostics;
using PoE2SharedCapture;

namespace PoE2CaptureHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        var bossConfig = GetArg(args, "--boss-config");
        var gameTimeConfig = GetArg(args, "--gametime-config");
        var diagnosticDir = GetArg(args, "--diagnostic-dir");
        if (string.IsNullOrWhiteSpace(bossConfig) || string.IsNullOrWhiteSpace(gameTimeConfig))
            return 2;

        var log = new HostLog(diagnosticDir);
        try
        {
            using var singleInstance = new Mutex(initiallyOwned: true, SharedCaptureProtocol.HostMutexName, out var createdNew);
            if (!createdNew) return 0;

            var config = CaptureHostConfig.Load(Path.GetFullPath(bossConfig), Path.GetFullPath(gameTimeConfig));
            log.Write($"STARTUP | pid={Environment.ProcessId} | transport=WGC | oneSession=true | bossConfig={Path.GetFullPath(bossConfig)} | gameTimeConfig={Path.GetFullPath(gameTimeConfig)}");

            using var control = new SharedControlServer();
            using var bossWriter = new SharedFrameWriter(SharedCaptureProtocol.BossMapName, SharedCaptureProtocol.BossCapacity);
            using var pauseWriter = new SharedFrameWriter(SharedCaptureProtocol.PauseMapName, SharedCaptureProtocol.PauseCapacity);
            using var pauseScaler = new PauseScalerWorker(pauseWriter, config.CanonicalWidth, config.CanonicalHeight);

            var finder = new GameWindowFinder(config.ProcessNames);
            GameWindowInfo? window = null;
            WgcCaptureSession? capture = null;
            GpuRegionReader? bossReader = null;
            GpuRegionReader? pauseReader = null;

            var startedUtc = DateTime.UtcNow;
            var lastClientUtc = startedUtc;
            var nextWindowCheck = 0L;
            var nextBossSample = 0L;
            var nextPauseSample = 0L;
            CaptureRect? cachedClientFrame = null;
            var cachedFrameWidth = -1;
            var cachedFrameHeight = -1;

            while (true)
            {
                var utcNow = DateTime.UtcNow;
                var state = control.Read();
                var bossActive = state.BossActive(utcNow);
                var pauseActive = state.PauseActive(utcNow);
                if (bossActive || pauseActive) lastClientUtc = utcNow;

                if (!bossActive && !pauseActive)
                {
                    if ((utcNow - startedUtc).TotalSeconds > 30 && (utcNow - lastClientUtc).TotalSeconds > 20)
                    {
                        log.Write("SHUTDOWN | reason=no-active-clients");
                        break;
                    }
                    Thread.Sleep(25);
                    continue;
                }

                var qpc = Stopwatch.GetTimestamp();
                if (qpc >= nextWindowCheck)
                {
                    nextWindowCheck = qpc + Stopwatch.Frequency;
                    if (window is null || !GameWindowFinder.IsStillValid(window))
                    {
                        DisposeCapture(ref capture, ref bossReader, ref pauseReader);
                        window = finder.Find();
                        if (window is null)
                        {
                            Thread.Sleep(50);
                            continue;
                        }

                        try
                        {
                            capture = new WgcCaptureSession(window.Handle, log);
                            bossReader = new GpuRegionReader(capture.Device, capture.Context);
                            pauseReader = new GpuRegionReader(capture.Device, capture.Context);
                            nextBossSample = 0;
                            nextPauseSample = 0;
                            cachedClientFrame = null;
                            cachedFrameWidth = -1;
                            cachedFrameHeight = -1;
                            log.Write($"GAME_FOUND | process={window.ProcessName} | pid={window.ProcessId} | hwnd=0x{window.Handle.ToInt64():X}");
                        }
                        catch (Exception ex)
                        {
                            log.Write($"WGC_START_FAILED | {ex.GetType().Name}: {ex.Message}");
                            DisposeCapture(ref capture, ref bossReader, ref pauseReader);
                            window = null;
                            Thread.Sleep(250);
                            continue;
                        }
                    }
                }

                if (capture is null || window is null || bossReader is null || pauseReader is null)
                {
                    Thread.Sleep(10);
                    continue;
                }

                try
                {
                    if (!capture.TryGetNextFrame(out var frame) || frame is null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    using (frame)
                    {
                        var nowQpc = Stopwatch.GetTimestamp();
                        var foreground = NativeMethods.GetForegroundWindow() == window.Handle;
                        var flags = foreground ? SharedCaptureProtocol.FlagForeground : 0;
                        var captureUtcTicks = DateTime.UtcNow.Ticks;

                        // Preserve the historical detector coordinate system: both
                        // consumers see PoE2 CLIENT-area geometry even if WGC includes
                        // a window border/non-client region in windowed mode. Geometry is
                        // cached until WGC reports a different content size.
                        if (cachedClientFrame is null ||
                            cachedFrameWidth != frame.ContentWidth ||
                            cachedFrameHeight != frame.ContentHeight)
                        {
                            NativeMethods.TryGetClientFrameRect(
                                window.Handle, frame.ContentWidth, frame.ContentHeight, out var detectedClientFrame);
                            cachedClientFrame = detectedClientFrame;
                            cachedFrameWidth = frame.ContentWidth;
                            cachedFrameHeight = frame.ContentHeight;
                            log.Write($"CLIENT_GEOMETRY | frame={frame.ContentWidth}x{frame.ContentHeight} | clientFrame={detectedClientFrame.X},{detectedClientFrame.Y},{detectedClientFrame.Width},{detectedClientFrame.Height}");
                        }
                        var clientFrame = cachedClientFrame.Value;

                        if (bossActive && IsDue(nowQpc, nextBossSample))
                        {
                            var fps = Math.Clamp(state.BossDesiredFps, 1, 120);
                            nextBossSample = NextDue(nowQpc, fps);
                            var relative = config.GetBossRect(clientFrame.Width, clientFrame.Height);
                            var rect = new CaptureRect(
                                clientFrame.X + relative.X,
                                clientFrame.Y + relative.Y,
                                relative.Width,
                                relative.Height);
                            using var pixels = bossReader.ReadBgr24(frame.Texture, rect);
                            bossWriter.Write(
                                pixels.Buffer, pixels.Length,
                                captureUtcTicks, frame.SystemRelativeTicks,
                                window.ProcessId,
                                clientFrame.Width, clientFrame.Height,
                                pixels.Width, pixels.Height, pixels.Stride, flags);
                        }

                        if (pauseActive && IsDue(nowQpc, nextPauseSample))
                        {
                            var fps = Math.Clamp(state.PauseDesiredFps, 1, 120);
                            nextPauseSample = NextDue(nowQpc, fps);
                            var relative = config.GetPauseViewport(clientFrame.Width, clientFrame.Height);
                            var rect = new CaptureRect(
                                clientFrame.X + relative.X,
                                clientFrame.Y + relative.Y,
                                relative.Width,
                                relative.Height);
                            var pixels = pauseReader.ReadBgr24(frame.Texture, rect);
                            pauseScaler.Submit(new PauseWorkItem(
                                pixels, captureUtcTicks, frame.SystemRelativeTicks,
                                window.ProcessId, clientFrame.Width, clientFrame.Height, flags));
                        }

                        if (frame.ContentWidth != capture.Width || frame.ContentHeight != capture.Height)
                        {
                            // WGC recommends recreating the pool when content size changes.
                            capture.Recreate(frame.ContentWidth, frame.ContentHeight);
                            nextBossSample = 0;
                            nextPauseSample = 0;
                            cachedClientFrame = null;
                            cachedFrameWidth = -1;
                            cachedFrameHeight = -1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Write($"CAPTURE_LOOP_ERROR | {ex.GetType().Name}: {ex.Message}");
                    DisposeCapture(ref capture, ref bossReader, ref pauseReader);
                    window = null;
                    Thread.Sleep(100);
                }
            }

            DisposeCapture(ref capture, ref bossReader, ref pauseReader);
            return 0;
        }
        catch (Exception ex)
        {
            log.Write($"FATAL | {ex}");
            return 1;
        }
    }

    private static bool IsDue(long now, long due) => due == 0 || now >= due;

    private static long NextDue(long now, int fps)
        => now + Math.Max(1L, Stopwatch.Frequency / Math.Max(1, fps));

    private static void DisposeCapture(
        ref WgcCaptureSession? capture,
        ref GpuRegionReader? bossReader,
        ref GpuRegionReader? pauseReader)
    {
        pauseReader?.Dispose();
        bossReader?.Dispose();
        capture?.Dispose();
        pauseReader = null;
        bossReader = null;
        capture = null;
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i + 1 < args.Length; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}
