using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// Bounded history of recent boss-ROI frames. Frames are retained at native capture resolution
/// so identity OCR can continue after a very short encounter has already disappeared.
/// </summary>
public sealed class BossRoiRingBuffer : IDisposable
{
    private readonly int _capacity;
    private readonly Queue<BufferedBossFrame> _frames = new();
    private DateTimeOffset _lastWhen = DateTimeOffset.MinValue;

    public BossRoiRingBuffer(int capacity)
    {
        _capacity = Math.Max(2, capacity);
    }

    public int Count => _frames.Count;

    public void Add(DateTimeOffset when, Bitmap raw, BossBarMetrics metrics)
    {
        if (when == _lastWhen) return;
        _lastWhen = when;

        var score =
            metrics.HealthRedRunFraction * 3.0 +
            metrics.CenterNameGoldFraction * 4.0 +
            metrics.NameGoldFraction * 2.0;

        _frames.Enqueue(new BufferedBossFrame(when, (Bitmap)raw.Clone(), score));
        while (_frames.Count > _capacity)
            _frames.Dequeue().Dispose();
    }

    public List<BufferedBossFrame> SnapshotBest(int maxFrames)
    {
        var count = Math.Max(1, Math.Min(maxFrames, _frames.Count));
        return _frames
            .OrderByDescending(frame => frame.QualityScore)
            .Take(count)
            .OrderBy(frame => frame.When)
            .Select(frame => frame.Clone())
            .ToList();
    }

    public void Clear()
    {
        while (_frames.Count > 0)
            _frames.Dequeue().Dispose();
        _lastWhen = DateTimeOffset.MinValue;
    }

    public void Dispose() => Clear();
}

public sealed class BufferedBossFrame : IDisposable
{
    public DateTimeOffset When { get; }
    public Bitmap Bitmap { get; }
    public double QualityScore { get; }

    public BufferedBossFrame(DateTimeOffset when, Bitmap bitmap, double qualityScore)
    {
        When = when;
        Bitmap = bitmap;
        QualityScore = qualityScore;
    }

    public BufferedBossFrame Clone()
        => new(When, (Bitmap)Bitmap.Clone(), QualityScore);

    public void Dispose() => Bitmap.Dispose();
}
