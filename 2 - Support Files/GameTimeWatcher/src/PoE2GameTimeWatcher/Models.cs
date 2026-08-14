using System.Drawing;

namespace PoE2GameTimeWatcher;

public sealed record NormalizedRect(double X, double Y, double Width, double Height);
public sealed record GameWindowInfo(int ProcessId, string ProcessName, IntPtr Handle);

public sealed record CaptureResult(Bitmap Bitmap, int ClientWidth, int ClientHeight, Rectangle ScreenRectangle) : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}

public enum ManualPauseVisualState
{
    Running,
    PauseMenu,
    MtxShop
}

public sealed record DetectionResult(
    ManualPauseVisualState State,
    double PauseMenuScore,
    double PauseStackScore,
    double ResumeGameScore,
    double PauseBannerScore,
    double ExitPathOfExileScore,
    double MtxShopScore,
    Rectangle ContentBounds);
