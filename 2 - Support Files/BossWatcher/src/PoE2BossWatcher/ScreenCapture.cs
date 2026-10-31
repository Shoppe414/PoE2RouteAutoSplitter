using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PoE2SharedCapture;

namespace PoE2BossWatcher;

public enum OcrPreprocessMode
{
    Gold,
    Broad
}

public sealed class ScreenCapture : IDisposable
{
    private readonly SharedCaptureClient _client = new();
    private readonly SharedFrameReader _reader = new(
        SharedCaptureProtocol.BossMapName,
        SharedCaptureProtocol.BossCapacity);

    public void KeepAlive(int desiredFps)
        => _client.PulseBoss(Math.Clamp(desiredFps, 1, 120));

    /// <summary>
    /// Reads the boss UI band produced by the shared WGC CaptureHost. The
    /// capture host owns the only screen-capture session; this watcher performs
    /// no direct screen-acquisition calls.
    /// </summary>
    public CaptureResult? CaptureBossRoi(GameWindowInfo window, AppConfig config, bool requireForeground)
    {
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle)
            return null;

        _client.PulseBoss(Math.Clamp(config.CaptureFps, 1, 120));
        var timeoutMs = Math.Clamp((int)Math.Ceiling(2000.0 / Math.Max(1, config.CaptureFps)), 35, 500);
        if (!_reader.TryReadNext(window.ProcessId, timeoutMs, out var snapshot) || snapshot is null)
            return null;
        if (requireForeground && (snapshot.Flags & SharedCaptureProtocol.FlagForeground) == 0)
            return null;

        var bitmap = CreateBitmap(snapshot);
        return new CaptureResult(
            bitmap,
            snapshot.ClientWidth,
            snapshot.ClientHeight,
            new Rectangle(0, 0, snapshot.PixelWidth, snapshot.PixelHeight));
    }

    // Compatibility overload retained for existing calibration helpers. The
    // shared host owns the canonical boss capture geometry, so live consumers
    // receive the same WGC boss channel regardless of this legacy ROI argument.
    public CaptureResult? CaptureBossRoi(GameWindowInfo window, NormalizedRect roi, bool requireForeground)
    {
        if (requireForeground && NativeMethods.GetForegroundWindow() != window.Handle)
            return null;

        _client.PulseBoss(20);
        if (!_reader.TryReadNext(window.ProcessId, 150, out var snapshot) || snapshot is null)
            return null;
        if (requireForeground && (snapshot.Flags & SharedCaptureProtocol.FlagForeground) == 0)
            return null;

        var bitmap = CreateBitmap(snapshot);
        return new CaptureResult(
            bitmap,
            snapshot.ClientWidth,
            snapshot.ClientHeight,
            new Rectangle(0, 0, snapshot.PixelWidth, snapshot.PixelHeight));
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

    public void Dispose()
    {
        _reader.Dispose();
        _client.Dispose();
    }

    public static Rectangle GetBossCaptureClientRect(int clientWidth, int clientHeight, AppConfig config)
    {
        var width = Math.Clamp(
            (int)Math.Round(clientHeight * config.BossCaptureWidthHeightRatio),
            1, clientWidth);
        var height = Math.Clamp(
            (int)Math.Round(clientHeight * config.BossRoi.Height),
            1, clientHeight);
        var x = Math.Max(0, (clientWidth - width) / 2);
        var y = Math.Clamp(
            (int)Math.Round(clientHeight * config.BossRoi.Y),
            0, Math.Max(0, clientHeight - height));
        return new Rectangle(x, y, width, height);
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

                        var isController = config.InputUiMode.Equals("Controller", StringComparison.OrdinalIgnoreCase);
                        var isText = isController
                            ? IsControllerBossNamePixel(r, g, b, config, mode)
                            : mode == OcrPreprocessMode.Gold
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

        // Boss frames can contribute long horizontal highlights/underlines. They are not glyphs and
        // materially hurt Tesseract segmentation. Remove them for both single and dual lanes.
        RemoveLongHorizontalArtifacts(mask, lane == BossLane.Single
            ? config.SingleOcrHorizontalArtifactMinFraction
            : config.DualOcrHorizontalArtifactMinFraction);

        // Single-boss names are center-anchored. Keep the mask dimensions stable for temporal OCR,
        // but blank distant unrelated components so terrain/UI fragments cannot dominate SingleLine OCR.
        if (lane == BossLane.Single)
            SuppressOutsideCentralTextCluster(mask);

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


    private static void SuppressOutsideCentralTextCluster(Bitmap mask)
    {
        var rect = new Rectangle(0, 0, mask.Width, mask.Height);
        var data = mask.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var colCounts = new int[mask.Width];
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    for (var x = 0; x < data.Width; x++)
                    {
                        var p = row + x * 3;
                        if (p[0] == 0 && p[1] == 0 && p[2] == 0) continue;
                        colCounts[x]++;
                    }
                }
            }

            var center = mask.Width / 2;
            var minColumnPixels = Math.Max(2, mask.Height / 16);
            var maxGap = Math.Max(8, mask.Width / 28);
            var active = new List<int>();
            for (var x = 0; x < colCounts.Length; x++)
                if (colCounts[x] >= minColumnPixels) active.Add(x);
            if (active.Count == 0) return;

            var groups = new List<(int Left, int Right, int Pixels)>();
            var left = active[0];
            var right = active[0];
            for (var i = 1; i < active.Count; i++)
            {
                if (active[i] - right <= maxGap)
                {
                    right = active[i];
                }
                else
                {
                    groups.Add((left, right, 0));
                    left = right = active[i];
                }
            }
            groups.Add((left, right, 0));

            (int Left, int Right, int Pixels)? best = null;
            double bestScore = double.MinValue;
            foreach (var g in groups)
            {
                var pixels = 0;
                for (var x = g.Left; x <= g.Right; x++) pixels += colCounts[x];
                var mid = (g.Left + g.Right) * 0.5;
                var centerPenalty = Math.Abs(mid - center) / Math.Max(1.0, mask.Width);
                var score = pixels - centerPenalty * pixels * 1.5;
                var intersectsCenterBand = g.Right >= mask.Width * 0.28 && g.Left <= mask.Width * 0.72;
                if (!intersectsCenterBand) score *= 0.25;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = (g.Left, g.Right, pixels);
                }
            }
            if (!best.HasValue) return;

            var padX = Math.Max(6, mask.Width / 40);
            var keepLeft = Math.Max(0, best.Value.Left - padX);
            var keepRight = Math.Min(mask.Width - 1, best.Value.Right + padX);

            var top = mask.Height;
            var bottom = -1;
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    for (var x = keepLeft; x <= keepRight; x++)
                    {
                        var p = row + x * 3;
                        if (p[0] > 0 || p[1] > 0 || p[2] > 0)
                        {
                            top = Math.Min(top, y);
                            bottom = Math.Max(bottom, y);
                        }
                    }
                }
            }
            if (bottom < top) return;
            var padY = Math.Max(1, mask.Height / 16);
            top = Math.Max(0, top - padY);
            bottom = Math.Min(mask.Height - 1, bottom + padY);

            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    for (var x = 0; x < data.Width; x++)
                    {
                        if (x >= keepLeft && x <= keepRight && y >= top && y <= bottom) continue;
                        var p = row + x * 3;
                        p[0] = p[1] = p[2] = 0;
                    }
                }
            }
        }
        finally { mask.UnlockBits(data); }
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
            // Dual names are NOT children of the narrow single-boss BossNameRoi. Each boss
            // receives a wide independent lane anchored to the capture midpoint. This preserves
            // the complete left/right names at 16:9 while the height-relative outer capture
            // keeps the same physical UI geometry on 21:9 and 32:9 clients.
            //
            // Left:  10% -> 50% of the centered boss capture
            // Right: 50% -> 90% of the centered boss capture
            BossLane.Left => new NormalizedRect(0.10, config.BossNameRoi.Y, 0.40, config.BossNameRoi.Height),
            BossLane.Right => new NormalizedRect(0.50, config.BossNameRoi.Y, 0.40, config.BossNameRoi.Height),
            _ => config.BossNameRoi
        };
    }

    public static NormalizedRect Compose(NormalizedRect parent, NormalizedRect child)
        => new(
            parent.X + parent.Width * child.X,
            parent.Y + parent.Height * child.Y,
            parent.Width * child.Width,
            parent.Height * child.Height);

    private static bool IsControllerBossNamePixel(int r, int g, int b, AppConfig config, OcrPreprocessMode mode)
    {
        var luminance = (int)Math.Round(0.299 * r + 0.587 * g + 0.114 * b);
        var threshold = mode == OcrPreprocessMode.Gold
            ? config.ControllerOcrLuminanceMin
            : config.ControllerOcrBroadLuminanceMin;
        return luminance >= threshold;
    }

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
