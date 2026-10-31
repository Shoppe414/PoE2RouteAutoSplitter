using System.IO.MemoryMappedFiles;

namespace PoE2SharedCapture;

public static class SharedCaptureProtocol
{
    public const int ProtocolVersion = 1;
    public const int FrameMagic = 0x31434757; // WGC1
    public const int ControlMagic = 0x31435457; // WTC1
    public const int FrameHeaderSize = 128;
    public const int ControlCapacity = 4096;
    public const int BossCapacity = 64 * 1024 * 1024;
    public const int PauseCapacity = 4 * 1024 * 1024;

    public const string BossMapName = @"Local\PoE2AS.WGC.Boss.v1";
    public const string PauseMapName = @"Local\PoE2AS.WGC.Pause.v1";
    public const string ControlMapName = @"Local\PoE2AS.WGC.Control.v1";
    public const string HostMutexName = @"Local\PoE2AS.WGC.Host.v1";

    public const int FlagForeground = 1;

    // Frame header offsets.
    internal const long FMagic = 0;
    internal const long FVersion = 4;
    internal const long FSequence = 8;
    internal const long FCaptureUtcTicks = 16;
    internal const long FSystemRelativeTicks = 24;
    internal const long FProcessId = 32;
    internal const long FClientWidth = 36;
    internal const long FClientHeight = 40;
    internal const long FPixelWidth = 44;
    internal const long FPixelHeight = 48;
    internal const long FStride = 52;
    internal const long FDataLength = 56;
    internal const long FFlags = 60;

    // Control header offsets. Boss and pause clients write disjoint fields so
    // no inter-process mutex is needed on the hot path.
    internal const long CMagic = 0;
    internal const long CVersion = 4;
    internal const long CBossHeartbeatUtcTicks = 8;
    internal const long CBossDesiredFps = 16;
    internal const long CBossClientPid = 20;
    internal const long CPauseHeartbeatUtcTicks = 24;
    internal const long CPauseDesiredFps = 32;
    internal const long CPauseClientPid = 36;
}

public sealed record SharedFrameSnapshot(
    long Sequence,
    long CaptureUtcTicks,
    long SystemRelativeTicks,
    int ProcessId,
    int ClientWidth,
    int ClientHeight,
    int PixelWidth,
    int PixelHeight,
    int Stride,
    int Flags,
    byte[] Pixels);

public sealed record SharedControlState(
    long BossHeartbeatUtcTicks,
    int BossDesiredFps,
    int BossClientPid,
    long PauseHeartbeatUtcTicks,
    int PauseDesiredFps,
    int PauseClientPid)
{
    public bool BossActive(DateTime utcNow, int timeoutMs = 2500)
        => IsRecent(BossHeartbeatUtcTicks, utcNow, timeoutMs) && BossDesiredFps > 0;

    public bool PauseActive(DateTime utcNow, int timeoutMs = 2500)
        => IsRecent(PauseHeartbeatUtcTicks, utcNow, timeoutMs) && PauseDesiredFps > 0;

    private static bool IsRecent(long ticks, DateTime now, int timeoutMs)
    {
        if (ticks <= 0) return false;
        var ageTicks = now.Ticks - ticks;
        return ageTicks >= 0 && ageTicks <= TimeSpan.FromMilliseconds(timeoutMs).Ticks;
    }
}

public sealed class SharedFrameWriter : IDisposable
{
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;
    private readonly int _capacity;
    private long _sequence;

    public SharedFrameWriter(string mapName, int capacity)
    {
        if (capacity <= SharedCaptureProtocol.FrameHeaderSize)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _file = MemoryMappedFile.CreateOrOpen(mapName, capacity, MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);
        _view.Write(SharedCaptureProtocol.FMagic, SharedCaptureProtocol.FrameMagic);
        _view.Write(SharedCaptureProtocol.FVersion, SharedCaptureProtocol.ProtocolVersion);
        _view.Write(SharedCaptureProtocol.FSequence, 0L);
    }

    public void Write(
        byte[] pixels,
        int dataLength,
        long captureUtcTicks,
        long systemRelativeTicks,
        int processId,
        int clientWidth,
        int clientHeight,
        int pixelWidth,
        int pixelHeight,
        int stride,
        int flags)
    {
        if (dataLength <= 0 || dataLength > pixels.Length) return;
        if (dataLength > _capacity - SharedCaptureProtocol.FrameHeaderSize)
            throw new InvalidOperationException($"Shared capture frame ({dataLength} bytes) exceeds map capacity {_capacity}.");

        // Odd sequence means write in progress. Even sequence means stable.
        var odd = Interlocked.Add(ref _sequence, 2) - 1;
        _view.Write(SharedCaptureProtocol.FSequence, odd);
        Thread.MemoryBarrier();

        _view.Write(SharedCaptureProtocol.FMagic, SharedCaptureProtocol.FrameMagic);
        _view.Write(SharedCaptureProtocol.FVersion, SharedCaptureProtocol.ProtocolVersion);
        _view.Write(SharedCaptureProtocol.FCaptureUtcTicks, captureUtcTicks);
        _view.Write(SharedCaptureProtocol.FSystemRelativeTicks, systemRelativeTicks);
        _view.Write(SharedCaptureProtocol.FProcessId, processId);
        _view.Write(SharedCaptureProtocol.FClientWidth, clientWidth);
        _view.Write(SharedCaptureProtocol.FClientHeight, clientHeight);
        _view.Write(SharedCaptureProtocol.FPixelWidth, pixelWidth);
        _view.Write(SharedCaptureProtocol.FPixelHeight, pixelHeight);
        _view.Write(SharedCaptureProtocol.FStride, stride);
        _view.Write(SharedCaptureProtocol.FDataLength, dataLength);
        _view.Write(SharedCaptureProtocol.FFlags, flags);

        _view.WriteArray(SharedCaptureProtocol.FrameHeaderSize, pixels, 0, dataLength);

        Thread.MemoryBarrier();
        var even = odd + 1;
        _view.Write(SharedCaptureProtocol.FSequence, even);
        Volatile.Write(ref _sequence, even);
    }

    public void Dispose()
    {
        _view.Dispose();
        _file.Dispose();
    }
}

public sealed class SharedFrameReader : IDisposable
{
    private readonly string _mapName;
    private readonly int _capacity;
    private MemoryMappedFile? _file;
    private MemoryMappedViewAccessor? _view;
    private long _lastSequence;
    private byte[] _buffer = [];

    public SharedFrameReader(string mapName, int capacity)
    {
        _mapName = mapName;
        _capacity = capacity;
    }

    public bool TryReadNext(int expectedProcessId, int timeoutMs, out SharedFrameSnapshot? snapshot)
    {
        var deadline = Environment.TickCount64 + Math.Max(0, timeoutMs);
        do
        {
            if (TryReadLatest(expectedProcessId, requireNewSequence: true, out snapshot))
                return true;

            if (Environment.TickCount64 >= deadline) break;
            Thread.Sleep(1);
        }
        while (true);

        snapshot = null;
        return false;
    }

    public bool TryReadLatest(int expectedProcessId, bool requireNewSequence, out SharedFrameSnapshot? snapshot)
    {
        snapshot = null;
        if (!EnsureOpen()) return false;
        var view = _view!;

        try
        {
            if (view.ReadInt32(SharedCaptureProtocol.FMagic) != SharedCaptureProtocol.FrameMagic ||
                view.ReadInt32(SharedCaptureProtocol.FVersion) != SharedCaptureProtocol.ProtocolVersion)
                return false;

            var seq1 = view.ReadInt64(SharedCaptureProtocol.FSequence);
            if (seq1 <= 0 || (seq1 & 1) != 0) return false;
            if (requireNewSequence && seq1 == _lastSequence) return false;

            Thread.MemoryBarrier();
            var captureUtcTicks = view.ReadInt64(SharedCaptureProtocol.FCaptureUtcTicks);
            var systemRelativeTicks = view.ReadInt64(SharedCaptureProtocol.FSystemRelativeTicks);
            var processId = view.ReadInt32(SharedCaptureProtocol.FProcessId);
            var clientWidth = view.ReadInt32(SharedCaptureProtocol.FClientWidth);
            var clientHeight = view.ReadInt32(SharedCaptureProtocol.FClientHeight);
            var pixelWidth = view.ReadInt32(SharedCaptureProtocol.FPixelWidth);
            var pixelHeight = view.ReadInt32(SharedCaptureProtocol.FPixelHeight);
            var stride = view.ReadInt32(SharedCaptureProtocol.FStride);
            var dataLength = view.ReadInt32(SharedCaptureProtocol.FDataLength);
            var flags = view.ReadInt32(SharedCaptureProtocol.FFlags);

            if (expectedProcessId > 0 && processId != expectedProcessId) return false;
            if (pixelWidth <= 0 || pixelHeight <= 0 || stride < pixelWidth * 3 || dataLength <= 0)
                return false;
            if (dataLength > _capacity - SharedCaptureProtocol.FrameHeaderSize)
                return false;

            if (_buffer.Length != dataLength) _buffer = new byte[dataLength];
            view.ReadArray(SharedCaptureProtocol.FrameHeaderSize, _buffer, 0, dataLength);
            Thread.MemoryBarrier();
            var seq2 = view.ReadInt64(SharedCaptureProtocol.FSequence);
            if (seq1 != seq2 || (seq2 & 1) != 0) return false;

            _lastSequence = seq2;
            snapshot = new SharedFrameSnapshot(
                seq2, captureUtcTicks, systemRelativeTicks, processId,
                clientWidth, clientHeight, pixelWidth, pixelHeight, stride, flags, _buffer);
            return true;
        }
        catch (ObjectDisposedException)
        {
            Reset();
            return false;
        }
        catch (IOException)
        {
            Reset();
            return false;
        }
    }

    private bool EnsureOpen()
    {
        if (_view is not null) return true;
        try
        {
            _file = MemoryMappedFile.OpenExisting(_mapName, MemoryMappedFileRights.Read);
            _view = _file.CreateViewAccessor(0, _capacity, MemoryMappedFileAccess.Read);
            return true;
        }
        catch (FileNotFoundException)
        {
            Reset();
            return false;
        }
        catch (IOException)
        {
            Reset();
            return false;
        }
    }

    private void Reset()
    {
        _view?.Dispose();
        _file?.Dispose();
        _view = null;
        _file = null;
        _lastSequence = 0;
    }

    public void Dispose() => Reset();
}

public sealed class SharedCaptureClient : IDisposable
{
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;

    public SharedCaptureClient()
    {
        _file = MemoryMappedFile.CreateOrOpen(
            SharedCaptureProtocol.ControlMapName,
            SharedCaptureProtocol.ControlCapacity,
            MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, SharedCaptureProtocol.ControlCapacity, MemoryMappedFileAccess.ReadWrite);
        _view.Write(SharedCaptureProtocol.CMagic, SharedCaptureProtocol.ControlMagic);
        _view.Write(SharedCaptureProtocol.CVersion, SharedCaptureProtocol.ProtocolVersion);
    }

    public void PulseBoss(int desiredFps)
    {
        _view.Write(SharedCaptureProtocol.CBossDesiredFps, Math.Clamp(desiredFps, 1, 120));
        _view.Write(SharedCaptureProtocol.CBossClientPid, Environment.ProcessId);
        Thread.MemoryBarrier();
        _view.Write(SharedCaptureProtocol.CBossHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
    }

    public void PulsePause(int desiredFps)
    {
        _view.Write(SharedCaptureProtocol.CPauseDesiredFps, Math.Clamp(desiredFps, 1, 120));
        _view.Write(SharedCaptureProtocol.CPauseClientPid, Environment.ProcessId);
        Thread.MemoryBarrier();
        _view.Write(SharedCaptureProtocol.CPauseHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
    }

    public void Dispose()
    {
        _view.Dispose();
        _file.Dispose();
    }
}

public sealed class SharedControlServer : IDisposable
{
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;

    public SharedControlServer()
    {
        _file = MemoryMappedFile.CreateOrOpen(
            SharedCaptureProtocol.ControlMapName,
            SharedCaptureProtocol.ControlCapacity,
            MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, SharedCaptureProtocol.ControlCapacity, MemoryMappedFileAccess.ReadWrite);
        _view.Write(SharedCaptureProtocol.CMagic, SharedCaptureProtocol.ControlMagic);
        _view.Write(SharedCaptureProtocol.CVersion, SharedCaptureProtocol.ProtocolVersion);
    }

    public SharedControlState Read()
    {
        if (_view.ReadInt32(SharedCaptureProtocol.CMagic) != SharedCaptureProtocol.ControlMagic ||
            _view.ReadInt32(SharedCaptureProtocol.CVersion) != SharedCaptureProtocol.ProtocolVersion)
            return new SharedControlState(0, 0, 0, 0, 0, 0);

        return new SharedControlState(
            _view.ReadInt64(SharedCaptureProtocol.CBossHeartbeatUtcTicks),
            _view.ReadInt32(SharedCaptureProtocol.CBossDesiredFps),
            _view.ReadInt32(SharedCaptureProtocol.CBossClientPid),
            _view.ReadInt64(SharedCaptureProtocol.CPauseHeartbeatUtcTicks),
            _view.ReadInt32(SharedCaptureProtocol.CPauseDesiredFps),
            _view.ReadInt32(SharedCaptureProtocol.CPauseClientPid));
    }

    public void Dispose()
    {
        _view.Dispose();
        _file.Dispose();
    }
}
