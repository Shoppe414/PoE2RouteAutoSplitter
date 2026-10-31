using System.Drawing;
using System.Drawing.Imaging;
using PoE2SharedCapture;

namespace PoE2GameTimeWatcher;

/// <summary>
/// WGC transport adapter. Screen acquisition is owned by the single
/// PoE2CaptureHost process; GameTimeWatcher only consumes its canonical pause
/// channel. No GDI screen capture occurs in this process.
/// </summary>
public sealed class ScreenCapture : IDisposable
{
    private readonly SharedCaptureClient _client = new();
    private readonly SharedFrameReader _reader = new(
        SharedCaptureProtocol.PauseMapName,
        SharedCaptureProtocol.PauseCapacity);

    public void KeepAlive(int desiredFps)
        => _client.PulsePause(Math.Clamp(desiredFps, 1, 120));

    public CaptureResult? CaptureRoi(
        GameWindowInfo window,
        NormalizedRect roi,
        bool requireForeground,
        int targetHeight = 0,
        int desiredFps = 10)
    {
        // Retained for API compatibility. The live watcher uses
        // CaptureAnalysisViewport; this returns the canonical WGC pause frame.
        var target = targetHeight > 0 ? targetHeight : 576;
        var targetWidth = Math.Max(1, (int)Math.Round(target * (16.0 / 9.0)));
        return CaptureAnalysisViewport(window, requireForeground, targetWidth, target, desiredFps);
    }

    public CaptureResult? CaptureAnalysisViewport(
        GameWindowInfo window,
        bool requireForeground,
        int targetWidth,
        int targetHeight,
        int desiredFps = 10)
    {
        if (targetWidth <= 0 || targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetWidth));
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle)
            return null;

        _client.PulsePause(Math.Clamp(desiredFps, 1, 120));
        var timeoutMs = Math.Clamp((int)Math.Ceiling(2000.0 / Math.Max(1, desiredFps)), 35, 500);
        if (!_reader.TryReadNext(window.ProcessId, timeoutMs, out var snapshot) || snapshot is null)
            return null;
        if (requireForeground && (snapshot.Flags & SharedCaptureProtocol.FlagForeground) == 0)
            return null;

        // CaptureHost publishes this channel at the configured canonical size.
        // Refuse mismatched frames instead of silently rescaling in the watcher,
        // which would reintroduce duplicate image-processing work.
        if (snapshot.PixelWidth != targetWidth || snapshot.PixelHeight != targetHeight)
            return null;

        var bitmap = CreateBitmap(snapshot);
        return new CaptureResult(
            bitmap,
            snapshot.ClientWidth,
            snapshot.ClientHeight,
            new Rectangle(0, 0, snapshot.ClientWidth, snapshot.ClientHeight));
    }

    private static Bitmap CreateBitmap(SharedFrameSnapshot frame)
    {
        var bitmap = new Bitmap(frame.PixelWidth, frame.PixelHeight, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var rowBytes = frame.PixelWidth * 3;
            for (var y = 0; y < frame.PixelHeight; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    frame.Pixels,
                    y * frame.Stride,
                    data.Scan0 + y * data.Stride,
                    rowBytes);
            }
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }

    public static Rectangle ToPixelRect(int width, int height, NormalizedRect roi)
    {
        var x = Math.Clamp((int)Math.Round(width * roi.X), 0, Math.Max(0, width - 1));
        var y = Math.Clamp((int)Math.Round(height * roi.Y), 0, Math.Max(0, height - 1));
        var w = Math.Clamp((int)Math.Round(width * roi.Width), 1, width - x);
        var h = Math.Clamp((int)Math.Round(height * roi.Height), 1, height - y);
        return new Rectangle(x, y, w, h);
    }

    public void Dispose()
    {
        _reader.Dispose();
        _client.Dispose();
    }
}
