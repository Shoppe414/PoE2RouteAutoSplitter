using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PoE2BossWatcher;

public enum OcrPreprocessMode
{
    Gold,
    Broad
}

public sealed class ScreenCapture
{
    public CaptureResult? CaptureBossRoi(GameWindowInfo window, NormalizedRect roi, bool requireForeground)
    {
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle)
            return null;

        if (!NativeMethods.GetClientRect(window.Handle, out var client))
            return null;

        var origin = new NativeMethods.POINT { X = 0, Y = 0 };
        if (!NativeMethods.ClientToScreen(window.Handle, ref origin))
            return null;

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

    // Compatibility overload: the calibrated orange/gold mask remains the primary OCR source.
    public static Bitmap PreprocessBossNameForOcr(Bitmap source, AppConfig config)
        => PreprocessBossNameForOcr(source, config, BossLane.Single, OcrPreprocessMode.Gold);

    public static Bitmap PreprocessBossNameForOcr(Bitmap source, AppConfig config, BossLane lane)
        => PreprocessBossNameForOcr(source, config, lane, OcrPreprocessMode.Gold);

    public static Bitmap PreprocessBossNameForOcr(
        Bitmap source,
        AppConfig config,
        BossLane lane,
        OcrPreprocessMode mode)
    {
        using var native = CreateBossNameNativeMask(source, config, lane, mode);
        return ScaleOcrMask(native, config.OcrUpscale);
    }

    /// <summary>
    /// Builds a native-resolution black/white mask for one known boss-name lane.
    /// Gold is the original narrow color classifier. Broad intentionally accepts a wider range
    /// of bright/warm antialiased text pixels while relying on the tightly constrained lane ROI
    /// to keep unrelated scene pixels out of Tesseract.
    /// </summary>
    public static Bitmap CreateBossNameNativeMask(
        Bitmap source,
        AppConfig config,
        BossLane lane,
        OcrPreprocessMode mode)
    {
        var laneRoi = GetBossNameLaneRoi(config, lane);
        var cropRect = ToPixelRect(source.Width, source.Height, laneRoi);
        using var cropped = source.Clone(cropRect, PixelFormat.Format24bppRgb);

        var mask = new Bitmap(cropped.Width, cropped.Height, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, cropped.Width, cropped.Height);
        var srcData = cropped.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var dstData = mask.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            unsafe
            {
                for (var y = 0; y < srcData.Height; y++)
                {
                    var srcRow = (byte*)srcData.Scan0 + y * srcData.Stride;
                    var dstRow = (byte*)dstData.Scan0 + y * dstData.Stride;
                    for (var x = 0; x < srcData.Width; x++)
                    {
                        var sp = srcRow + x * 3;
                        var dp = dstRow + x * 3;
                        var b = (int)sp[0];
                        var g = (int)sp[1];
                        var r = (int)sp[2];

                        var isText = mode == OcrPreprocessMode.Gold
                            ? IsBossNamePixel(r, g, b, config)
                            : IsBroadBossNamePixel(r, g, b, config);
                        var v = isText ? (byte)255 : (byte)0;
                        dp[0] = v;
                        dp[1] = v;
                        dp[2] = v;
                    }
                }
            }
        }
        finally
        {
            cropped.UnlockBits(srcData);
            mask.UnlockBits(dstData);
        }

        // Dual-lane crops can include the long gold highlight/border running across a half-bar.
        // Remove only very long continuous horizontal white runs; text strokes are much shorter.
        if (lane != BossLane.Single)
            RemoveLongHorizontalArtifacts(mask, config.DualOcrHorizontalArtifactMinFraction);

        return mask;
    }

    public static Bitmap ScaleOcrMask(Bitmap nativeMask, int upscale)
    {
        if (upscale <= 1) return (Bitmap)nativeMask.Clone();

        var scaled = new Bitmap(nativeMask.Width * upscale, nativeMask.Height * upscale, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(scaled);
        g.Clear(Color.Black);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(nativeMask,
            new Rectangle(0, 0, scaled.Width, scaled.Height),
            0, 0, nativeMask.Width, nativeMask.Height,
            GraphicsUnit.Pixel);
        return scaled;
    }

    /// <summary>
    /// OR-combines several native black/white masks. Boss-name UI coordinates are fixed, so this
    /// reconstructs antialiased glyph pixels that may blink in/out across consecutive live frames.
    /// </summary>
    public static Bitmap CombineNativeMasks(IReadOnlyList<Bitmap> masks)
    {
        if (masks.Count == 0) throw new ArgumentException("At least one mask is required.", nameof(masks));
        var width = masks[0].Width;
        var height = masks[0].Height;
        if (masks.Any(m => m.Width != width || m.Height != height))
            throw new ArgumentException("All temporal masks must have identical dimensions.", nameof(masks));

        var combined = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, width, height);
        var dst = combined.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        var locked = new List<(Bitmap Bitmap, BitmapData Data)>(masks.Count);
        try
        {
            foreach (var mask in masks)
                locked.Add((mask, mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb)));

            unsafe
            {
                for (var y = 0; y < height; y++)
                {
                    var dstRow = (byte*)dst.Scan0 + y * dst.Stride;
                    for (var x = 0; x < width; x++)
                    {
                        var white = false;
                        foreach (var item in locked)
                        {
                            var row = (byte*)item.Data.Scan0 + y * item.Data.Stride;
                            var p = row + x * 3;
                            if (p[0] > 0 || p[1] > 0 || p[2] > 0)
                            {
                                white = true;
                                break;
                            }
                        }

                        var dp = dstRow + x * 3;
                        var value = white ? (byte)255 : (byte)0;
                        dp[0] = value;
                        dp[1] = value;
                        dp[2] = value;
                    }
                }
            }
        }
        finally
        {
            foreach (var item in locked)
                item.Bitmap.UnlockBits(item.Data);
            combined.UnlockBits(dst);
        }

        return combined;
    }

    public static int CountMaskPixels(Bitmap mask)
    {
        var rect = new Rectangle(0, 0, mask.Width, mask.Height);
        var data = mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var count = 0;
        try
        {
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    for (var x = 0; x < data.Width; x++)
                    {
                        var p = row + x * 3;
                        if (p[0] > 0 || p[1] > 0 || p[2] > 0) count++;
                    }
                }
            }
        }
        finally { mask.UnlockBits(data); }
        return count;
    }

    private static void RemoveLongHorizontalArtifacts(Bitmap mask, double minFraction)
    {
        var rect = new Rectangle(0, 0, mask.Width, mask.Height);
        var minRun = Math.Max(8, (int)Math.Round(mask.Width * minFraction));
        var data = mask.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    var runStart = -1;
                    for (var x = 0; x <= data.Width; x++)
                    {
                        var white = false;
                        if (x < data.Width)
                        {
                            var p = row + x * 3;
                            white = p[0] > 0 || p[1] > 0 || p[2] > 0;
                        }

                        if (white && runStart < 0)
                        {
                            runStart = x;
                        }
                        else if (!white && runStart >= 0)
                        {
                            if (x - runStart >= minRun)
                            {
                                for (var clearX = runStart; clearX < x; clearX++)
                                {
                                    var p = row + clearX * 3;
                                    p[0] = p[1] = p[2] = 0;
                                }
                            }
                            runStart = -1;
                        }
                    }
                }
            }
        }
        finally { mask.UnlockBits(data); }
    }

    public static NormalizedRect GetBossNameLaneRoi(AppConfig config, BossLane lane)
    {
        return lane switch
        {
            // The calibrated dual capture shows the left name within ~12-48% and the right
            // name within ~53-86% of the full BossNameRoi. These 40%-wide lane crops exclude
            // most outer frame/terrain noise while retaining the full names.
            BossLane.Left => Compose(config.BossNameRoi, new NormalizedRect(0.10, 0.0, 0.40, 1.0)),
            BossLane.Right => Compose(config.BossNameRoi, new NormalizedRect(0.50, 0.0, 0.40, 1.0)),
            _ => config.BossNameRoi
        };
    }

    public static NormalizedRect Compose(NormalizedRect parent, NormalizedRect child)
        => new(
            parent.X + parent.Width * child.X,
            parent.Y + parent.Height * child.Y,
            parent.Width * child.Width,
            parent.Height * child.Height);

    public static bool IsBossNamePixel(int r, int g, int b, AppConfig config)
        => r >= config.OcrRedMin
        && g >= config.OcrGreenMin
        && b <= config.OcrBlueMax
        && r - g >= config.OcrRedMinusGreenMin
        && g - b >= config.OcrGreenMinusBlueMin;

    public static bool IsBroadBossNamePixel(int r, int g, int b, AppConfig config)
    {
        // Broader live-rendering fallback. The narrow lane ROI supplies the spatial selectivity,
        // while this classifier admits pale/antialiased warm text pixels rejected by the gold mask.
        var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
        return r >= config.OcrBroadRedMin
            && g >= config.OcrBroadGreenMin
            && b <= config.OcrBroadBlueMax
            && r - g >= config.OcrBroadRedMinusGreenMin
            && g - b >= config.OcrBroadGreenMinusBlueMin
            && luminance >= config.OcrBroadLuminanceMin;
    }

    public static Rectangle ToPixelRect(int width, int height, NormalizedRect roi)
    {
        var x = Math.Clamp((int)Math.Round(width * roi.X), 0, Math.Max(0, width - 1));
        var y = Math.Clamp((int)Math.Round(height * roi.Y), 0, Math.Max(0, height - 1));
        var w = Math.Max(1, (int)Math.Round(width * roi.Width));
        var h = Math.Max(1, (int)Math.Round(height * roi.Height));
        if (x + w > width) w = width - x;
        if (y + h > height) h = height - y;
        return new Rectangle(x, y, Math.Max(1, w), Math.Max(1, h));
    }
}

public sealed record CaptureResult(Bitmap Bitmap, int ClientWidth, int ClientHeight, Rectangle ScreenRectangle) : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}
