using System.Runtime.InteropServices;

namespace PoE2CaptureHost;

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    /// <summary>
    /// Translates the HWND client area into WGC frame coordinates. WGC normally
    /// returns client-sized content for borderless PoE2, but this keeps windowed
    /// mode and non-client borders from shifting the historical detector ROIs.
    /// </summary>
    internal static bool TryGetClientFrameRect(
        IntPtr hwnd,
        int frameWidth,
        int frameHeight,
        out CaptureRect clientFrameRect)
    {
        clientFrameRect = new CaptureRect(0, 0, Math.Max(1, frameWidth), Math.Max(1, frameHeight));
        if (frameWidth <= 0 || frameHeight <= 0) return false;
        if (!GetClientRect(hwnd, out var client)) return false;

        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        if (clientWidth <= 0 || clientHeight <= 0) return false;

        // Borderless/windowed-fullscreen WGC commonly matches the client size.
        if (Math.Abs(frameWidth - clientWidth) <= 2 && Math.Abs(frameHeight - clientHeight) <= 2)
        {
            clientFrameRect = new CaptureRect(0, 0, frameWidth, frameHeight);
            return true;
        }

        if (!GetWindowRect(hwnd, out var window)) return false;
        var windowWidth = window.Right - window.Left;
        var windowHeight = window.Bottom - window.Top;
        if (windowWidth <= 0 || windowHeight <= 0) return false;

        var origin = new POINT { X = 0, Y = 0 };
        if (!ClientToScreen(hwnd, ref origin)) return false;

        // WGC content can be scaled relative to GetWindowRect on mixed-DPI
        // desktops. Scale the client offset/size into the actual texture space.
        var scaleX = frameWidth / (double)windowWidth;
        var scaleY = frameHeight / (double)windowHeight;
        var x = (int)Math.Round((origin.X - window.Left) * scaleX);
        var y = (int)Math.Round((origin.Y - window.Top) * scaleY);
        var width = (int)Math.Round(clientWidth * scaleX);
        var height = (int)Math.Round(clientHeight * scaleY);

        x = Math.Clamp(x, 0, Math.Max(0, frameWidth - 1));
        y = Math.Clamp(y, 0, Math.Max(0, frameHeight - 1));
        width = Math.Clamp(width, 1, frameWidth - x);
        height = Math.Clamp(height, 1, frameHeight - y);
        clientFrameRect = new CaptureRect(x, y, width, height);
        return true;
    }
}
