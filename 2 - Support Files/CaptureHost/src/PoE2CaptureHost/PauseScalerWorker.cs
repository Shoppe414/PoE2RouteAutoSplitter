using System.Buffers;
using PoE2SharedCapture;

namespace PoE2CaptureHost;

/// <summary>
/// Keeps pause scaling away from the WGC/D3D capture loop. Only the newest
/// pending pause frame is retained; stale intermediate frames are dropped.
/// </summary>
internal sealed class PauseScalerWorker : IDisposable
{
    private readonly object _gate = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _thread;
    private readonly SharedFrameWriter _writer;
    private readonly int _targetWidth;
    private readonly int _targetHeight;
    private PauseWorkItem? _pending;
    private bool _stopping;

    // Cached horizontal resampling map. Pause viewport dimensions normally stay constant for
    // the session, so avoid recomputing floor/clamp/weights for every pixel of every frame.
    private int _mapSrcWidth = -1;
    private int[]? _x0;
    private int[]? _x1;
    private double[]? _fx;

    public PauseScalerWorker(SharedFrameWriter writer, int targetWidth, int targetHeight)
    {
        _writer = writer;
        _targetWidth = targetWidth;
        _targetHeight = targetHeight;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "PoE2AS WGC pause scaler",
            Priority = ThreadPriority.BelowNormal
        };
        _thread.Start();
    }

    public void Submit(PauseWorkItem item)
    {
        PauseWorkItem? dropped = null;
        lock (_gate)
        {
            if (_stopping)
            {
                dropped = item;
            }
            else
            {
                dropped = _pending;
                _pending = item;
                _signal.Set();
            }
        }
        dropped?.Dispose();
    }

    private void Run()
    {
        while (true)
        {
            _signal.WaitOne();
            PauseWorkItem? item;
            lock (_gate)
            {
                if (_stopping && _pending is null) break;
                item = _pending;
                _pending = null;
            }
            if (item is null) continue;

            using (item)
            {
                var outputLength = checked(_targetWidth * _targetHeight * 3);
                var output = ArrayPool<byte>.Shared.Rent(outputLength);
                try
                {
                    ScaleBilinearBgr24(
                        item.Frame.Buffer, item.Frame.Width, item.Frame.Height, item.Frame.Stride,
                        output, _targetWidth, _targetHeight, _targetWidth * 3);

                    _writer.Write(
                        output,
                        outputLength,
                        item.CaptureUtcTicks,
                        item.SystemRelativeTicks,
                        item.ProcessId,
                        item.ClientWidth,
                        item.ClientHeight,
                        _targetWidth,
                        _targetHeight,
                        _targetWidth * 3,
                        item.Flags);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(output);
                }
            }
        }
    }

    private void ScaleBilinearBgr24(
        byte[] src, int srcWidth, int srcHeight, int srcStride,
        byte[] dst, int dstWidth, int dstHeight, int dstStride)
    {
        if (srcWidth == dstWidth && srcHeight == dstHeight)
        {
            for (var y = 0; y < dstHeight; y++)
                Buffer.BlockCopy(src, y * srcStride, dst, y * dstStride, dstWidth * 3);
            return;
        }

        EnsureHorizontalMap(srcWidth, dstWidth);
        var x0Map = _x0!;
        var x1Map = _x1!;
        var fxMap = _fx!;
        var scaleY = srcHeight / (double)dstHeight;

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy = (dy + 0.5) * scaleY - 0.5;
            var y0 = Math.Clamp((int)Math.Floor(sy), 0, srcHeight - 1);
            var y1 = Math.Min(srcHeight - 1, y0 + 1);
            var fy = Math.Clamp(sy - y0, 0.0, 1.0);
            var row0 = y0 * srcStride;
            var row1 = y1 * srcStride;
            var dstRow = dy * dstStride;

            for (var dx = 0; dx < dstWidth; dx++)
            {
                var p00 = row0 + x0Map[dx] * 3;
                var p10 = row0 + x1Map[dx] * 3;
                var p01 = row1 + x0Map[dx] * 3;
                var p11 = row1 + x1Map[dx] * 3;
                var dp = dstRow + dx * 3;
                var fx = fxMap[dx];

                // BGR24 is always exactly three channels. Unrolling avoids the innermost
                // channel loop and repeated index arithmetic in the hottest scaler path.
                var topB = src[p00] + (src[p10] - src[p00]) * fx;
                var botB = src[p01] + (src[p11] - src[p01]) * fx;
                var topG = src[p00 + 1] + (src[p10 + 1] - src[p00 + 1]) * fx;
                var botG = src[p01 + 1] + (src[p11 + 1] - src[p01 + 1]) * fx;
                var topR = src[p00 + 2] + (src[p10 + 2] - src[p00 + 2]) * fx;
                var botR = src[p01 + 2] + (src[p11 + 2] - src[p01 + 2]) * fx;

                dst[dp] = (byte)Math.Clamp((int)Math.Round(topB + (botB - topB) * fy), 0, 255);
                dst[dp + 1] = (byte)Math.Clamp((int)Math.Round(topG + (botG - topG) * fy), 0, 255);
                dst[dp + 2] = (byte)Math.Clamp((int)Math.Round(topR + (botR - topR) * fy), 0, 255);
            }
        }
    }

    private void EnsureHorizontalMap(int srcWidth, int dstWidth)
    {
        if (_mapSrcWidth == srcWidth && _x0 is { Length: > 0 } && _x0.Length == dstWidth) return;

        _mapSrcWidth = srcWidth;
        _x0 = new int[dstWidth];
        _x1 = new int[dstWidth];
        _fx = new double[dstWidth];
        var scaleX = srcWidth / (double)dstWidth;
        for (var dx = 0; dx < dstWidth; dx++)
        {
            var sx = (dx + 0.5) * scaleX - 0.5;
            var x0 = Math.Clamp((int)Math.Floor(sx), 0, srcWidth - 1);
            _x0[dx] = x0;
            _x1[dx] = Math.Min(srcWidth - 1, x0 + 1);
            _fx[dx] = Math.Clamp(sx - x0, 0.0, 1.0);
        }
    }

    public void Dispose()
    {
        PauseWorkItem? pending;
        lock (_gate)
        {
            _stopping = true;
            pending = _pending;
            _pending = null;
            _signal.Set();
        }
        pending?.Dispose();
        if (_thread.IsAlive) _thread.Join(2000);
        _signal.Dispose();
    }
}

internal sealed class PauseWorkItem : IDisposable
{
    public PooledBgrFrame Frame { get; }
    public long CaptureUtcTicks { get; }
    public long SystemRelativeTicks { get; }
    public int ProcessId { get; }
    public int ClientWidth { get; }
    public int ClientHeight { get; }
    public int Flags { get; }

    public PauseWorkItem(
        PooledBgrFrame frame,
        long captureUtcTicks,
        long systemRelativeTicks,
        int processId,
        int clientWidth,
        int clientHeight,
        int flags)
    {
        Frame = frame;
        CaptureUtcTicks = captureUtcTicks;
        SystemRelativeTicks = systemRelativeTicks;
        ProcessId = processId;
        ClientWidth = clientWidth;
        ClientHeight = clientHeight;
        Flags = flags;
    }

    public void Dispose() => Frame.Dispose();
}
