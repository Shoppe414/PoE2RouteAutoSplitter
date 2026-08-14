using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// Small native-resolution OCR mask history for one fixed boss-name lane. Histories are built
/// from captured frames, not OCR calls, so a temporal composite represents genuinely different
/// render samples while keeping Tesseract invocation count low.
/// </summary>
public sealed class OcrLaneHistory : IDisposable
{
    private readonly int _maxFrames;
    private readonly Queue<Bitmap> _gold = new();
    private readonly Queue<Bitmap> _broad = new();

    public OcrLaneHistory(int maxFrames)
    {
        _maxFrames = Math.Max(1, maxFrames);
    }

    public int Count => Math.Min(_gold.Count, _broad.Count);

    public void AddFrame(Bitmap raw, AppConfig config, BossLane lane)
    {
        _gold.Enqueue(ScreenCapture.CreateBossNameNativeMask(raw, config, lane, OcrPreprocessMode.Gold));
        _broad.Enqueue(ScreenCapture.CreateBossNameNativeMask(raw, config, lane, OcrPreprocessMode.Broad));
        Trim(_gold);
        Trim(_broad);
    }

    public Bitmap? CreateLatestScaled(AppConfig config, OcrPreprocessMode mode)
    {
        var queue = mode == OcrPreprocessMode.Gold ? _gold : _broad;
        if (queue.Count == 0) return null;
        return ScreenCapture.ScaleOcrMask(queue.Last(), config.OcrUpscale);
    }

    public Bitmap? CreateTemporalScaled(AppConfig config, OcrPreprocessMode mode, int minFrames)
    {
        var queue = mode == OcrPreprocessMode.Gold ? _gold : _broad;
        if (queue.Count < Math.Max(2, minFrames)) return null;
        using var combined = ScreenCapture.CombineNativeMasks(queue.ToArray());
        return ScreenCapture.ScaleOcrMask(combined, config.OcrUpscale);
    }

    public int LatestPixelCount(OcrPreprocessMode mode)
    {
        var queue = mode == OcrPreprocessMode.Gold ? _gold : _broad;
        return queue.Count == 0 ? 0 : ScreenCapture.CountMaskPixels(queue.Last());
    }

    public int TemporalPixelCount(OcrPreprocessMode mode)
    {
        var queue = mode == OcrPreprocessMode.Gold ? _gold : _broad;
        if (queue.Count == 0) return 0;
        using var combined = ScreenCapture.CombineNativeMasks(queue.ToArray());
        return ScreenCapture.CountMaskPixels(combined);
    }

    public void Clear()
    {
        DisposeQueue(_gold);
        DisposeQueue(_broad);
    }

    public void Dispose() => Clear();

    private void Trim(Queue<Bitmap> queue)
    {
        while (queue.Count > _maxFrames)
            queue.Dequeue().Dispose();
    }

    private static void DisposeQueue(Queue<Bitmap> queue)
    {
        while (queue.Count > 0)
            queue.Dequeue().Dispose();
    }
}
