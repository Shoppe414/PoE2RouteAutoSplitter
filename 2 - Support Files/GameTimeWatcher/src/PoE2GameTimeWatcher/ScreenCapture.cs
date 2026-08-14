using System.Drawing;
using System.Drawing.Imaging;

namespace PoE2GameTimeWatcher;

public sealed class ScreenCapture
{
    public CaptureResult? CaptureRoi(GameWindowInfo window, NormalizedRect roi, bool requireForeground)
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

        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return new CaptureResult(bitmap, clientWidth, clientHeight, new Rectangle(x, y, width, height));
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
