using System.Drawing;
using System.Drawing.Imaging;

namespace PoE2GameTimeWatcher;

public sealed class ScreenCapture
{
    public CaptureResult? CaptureRoi(GameWindowInfo window, NormalizedRect roi, bool requireForeground, int targetHeight = 0)
    {
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle) return null;
        if (!NativeMethods.GetClientRect(window.Handle, out var client)) return null;
        var origin = new NativeMethods.POINT { X = 0, Y = 0 };
        if (!NativeMethods.ClientToScreen(window.Handle, ref origin)) return null;

        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        if (clientWidth <= 0 || clientHeight <= 0) return null;

        var x = origin.X + (int)Math.Round(clientWidth * roi.X);
        var y = origin.Y + (int)Math.Round(clientHeight * roi.Y);
        var width = Math.Max(1, (int)Math.Round(clientWidth * roi.Width));
        var height = Math.Max(1, (int)Math.Round(clientHeight * roi.Height));

        // GameTimeWatcher analyzes a canonical-height image. Capture directly into that
        // analysis size so 4K/8K clients do not pay a full-resolution resize cost on every
        // frame. ClientWidth/ClientHeight and ScreenRectangle remain the native physical
        // geometry for diagnostics and state reporting.
        var outputHeight = targetHeight > 0 ? Math.Min(height, targetHeight) : height;
        var outputWidth = outputHeight == height
            ? width
            : Math.Max(1, (int)Math.Round(width * (outputHeight / (double)height)));

        var bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format24bppRgb);
        if (outputWidth == width && outputHeight == height)
        {
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }
        else
        {
            CopyScaledFromScreen(bitmap, x, y, width, height);
        }

        return new CaptureResult(bitmap, clientWidth, clientHeight, new Rectangle(x, y, width, height));
    }



    public CaptureResult? CaptureAnalysisViewport(GameWindowInfo window, bool requireForeground, int targetWidth, int targetHeight)
    {
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle) return null;
        if (!NativeMethods.GetClientRect(window.Handle, out var client)) return null;
        var origin = new NativeMethods.POINT { X = 0, Y = 0 };
        if (!NativeMethods.ClientToScreen(window.Handle, ref origin)) return null;

        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        if (clientWidth <= 0 || clientHeight <= 0) return null;
        if (targetWidth <= 0 || targetHeight <= 0) throw new ArgumentOutOfRangeException(nameof(targetWidth));

        // The manual-pause UI is centered. Analyze a centered 16:9 viewport at a fixed
        // 1024x576-style workload instead of preserving ultrawide width. This keeps 1080p,
        // 1440p, 4K, 8K, 21:9 and 32:9 clients on the same detector geometry and prevents
        // side overlays (LiveSplit/OBS) from entering the pause-menu search surface.
        var targetAspect = targetWidth / (double)targetHeight;
        var clientAspect = clientWidth / (double)clientHeight;
        int sourceWidth;
        int sourceHeight;
        int offsetX;
        int offsetY;
        if (clientAspect >= targetAspect)
        {
            sourceHeight = clientHeight;
            sourceWidth = Math.Clamp((int)Math.Round(sourceHeight * targetAspect), 1, clientWidth);
            offsetX = (clientWidth - sourceWidth) / 2;
            offsetY = 0;
        }
        else
        {
            sourceWidth = clientWidth;
            sourceHeight = Math.Clamp((int)Math.Round(sourceWidth / targetAspect), 1, clientHeight);
            offsetX = 0;
            offsetY = (clientHeight - sourceHeight) / 2;
        }

        var sourceX = origin.X + offsetX;
        var sourceY = origin.Y + offsetY;
        var bitmap = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
        CopyScaledFromScreen(bitmap, sourceX, sourceY, sourceWidth, sourceHeight);
        return new CaptureResult(bitmap, clientWidth, clientHeight, new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight));
    }

    private static void CopyScaledFromScreen(Bitmap target, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
    {
        using var graphics = Graphics.FromImage(target);
        var destinationDc = graphics.GetHdc();
        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            graphics.ReleaseHdc(destinationDc);
            throw new InvalidOperationException("Could not acquire the desktop device context for scaled capture.");
        }

        try
        {
            NativeMethods.SetStretchBltMode(destinationDc, NativeMethods.HALFTONE);
            if (!NativeMethods.StretchBlt(
                    destinationDc, 0, 0, target.Width, target.Height,
                    screenDc, sourceX, sourceY, sourceWidth, sourceHeight, NativeMethods.SRCCOPY))
            {
                throw new System.ComponentModel.Win32Exception(
                    System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                    "Scaled screen capture failed.");
            }
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            graphics.ReleaseHdc(destinationDc);
        }
    }

    public static Rectangle ToPixelRect(int width, int height, NormalizedRect roi)
    {
        var x = Math.Clamp((int)Math.Round(width * roi.X), 0, Math.Max(0, width - 1));
        var y = Math.Clamp((int)Math.Round(height * roi.Y), 0, Math.Max(0, height - 1));
        var w = Math.Clamp((int)Math.Round(width * roi.Width), 1, width - x);
        var h = Math.Clamp((int)Math.Round(height * roi.Height), 1, height - y);
        return new Rectangle(x, y, w, h);
    }
}
