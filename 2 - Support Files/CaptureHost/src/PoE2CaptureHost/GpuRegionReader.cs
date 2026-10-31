using System.Buffers;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace PoE2CaptureHost;

/// <summary>
/// Copies only the requested rectangle from a WGC GPU texture into a reusable
/// CPU-readable staging texture. Output is BGR24 so the existing detector
/// bitmap path keeps its historical pixel format.
/// </summary>
internal sealed class GpuRegionReader : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private ID3D11Texture2D? _staging;
    private int _width;
    private int _height;
    private Format _format;

    public GpuRegionReader(ID3D11Device device, ID3D11DeviceContext context)
    {
        _device = device;
        _context = context;
    }

    public PooledBgrFrame ReadBgr24(ID3D11Texture2D source, CaptureRect rect)
    {
        var desc = source.Description;
        var maxWidth = (int)desc.Width;
        var maxHeight = (int)desc.Height;
        var x = Math.Clamp(rect.X, 0, Math.Max(0, maxWidth - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, maxHeight - 1));
        var width = Math.Clamp(rect.Width, 1, maxWidth - x);
        var height = Math.Clamp(rect.Height, 1, maxHeight - y);

        EnsureStaging(width, height, desc.Format);
        var sourceBox = new Box(x, y, 0, x + width, y + height, 1);
        _context.CopySubresourceRegion(_staging!, 0, 0, 0, 0, source, 0, sourceBox);

        var required = checked(width * height * 3);
        var output = ArrayPool<byte>.Shared.Rent(required);
        var mapped = _context.Map(_staging!, 0, MapMode.Read);
        try
        {
            unsafe
            {
                var srcBase = (byte*)mapped.DataPointer;
                fixed (byte* dstBase = output)
                {
                    for (var row = 0; row < height; row++)
                    {
                        var src = srcBase + row * mapped.RowPitch;
                        var dst = dstBase + row * width * 3;
                        for (var col = 0; col < width; col++)
                        {
                            dst[col * 3 + 0] = src[col * 4 + 0];
                            dst[col * 3 + 1] = src[col * 4 + 1];
                            dst[col * 3 + 2] = src[col * 4 + 2];
                        }
                    }
                }
            }

            return new PooledBgrFrame(output, required, width, height, width * 3);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(output);
            throw;
        }
        finally
        {
            _context.Unmap(_staging!, 0);
        }
    }

    private void EnsureStaging(int width, int height, Format format)
    {
        if (_staging is not null && width == _width && height == _height && format == _format)
            return;

        _staging?.Dispose();
        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };
        _staging = _device.CreateTexture2D(desc);
        _width = width;
        _height = height;
        _format = format;
    }

    public void Dispose()
    {
        _staging?.Dispose();
        _staging = null;
    }
}

internal sealed class PooledBgrFrame : IDisposable
{
    private byte[]? _buffer;
    public byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(PooledBgrFrame));
    public int Length { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }

    public PooledBgrFrame(byte[] buffer, int length, int width, int height, int stride)
    {
        _buffer = buffer;
        Length = length;
        Width = width;
        Height = height;
        Stride = stride;
    }

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null) ArrayPool<byte>.Shared.Return(buffer);
    }
}
