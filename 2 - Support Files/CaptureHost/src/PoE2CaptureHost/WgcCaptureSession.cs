using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace PoE2CaptureHost;

/// <summary>
/// Persistent Windows.Graphics.Capture session for one HWND. WinRT capture is
/// driven through the documented raw COM interfaces so CaptureHost does not
/// require a UI thread, picker, DispatcherQueue, or injection into the game.
/// </summary>
internal sealed class WgcCaptureSession : IDisposable
{
    private const int RoInitMultithreaded = 1;
    private const int RpcEChangedMode = unchecked((int)0x80010106);
    private const int PixelFormatBgra8 = 87; // DirectXPixelFormat.B8G8R8A8UIntNormalized

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoInitialize(int initType);

    [DllImport("combase.dll")]
    private static extern void RoUninitialize();

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoGetActivationFactory(IntPtr classId, [In] ref Guid iid, out IntPtr factory);

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", PreserveSig = false)]
    private static extern void CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [StructLayout(LayoutKind.Sequential)]
    private struct SizeInt32
    {
        public int Width;
        public int Height;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtGetSize(IntPtr thisPtr, out SizeInt32 size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtCreateForWindow(IntPtr thisPtr, IntPtr hwnd, ref Guid iid, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtCreateFreeThreaded(
        IntPtr thisPtr, IntPtr device, int pixelFormat, int numberOfBuffers, SizeInt32 size, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtRecreate(
        IntPtr thisPtr, IntPtr device, int pixelFormat, int numberOfBuffers, SizeInt32 size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtTryGetNextFrame(IntPtr thisPtr, out IntPtr frame);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtCreateCaptureSession(IntPtr thisPtr, IntPtr item, out IntPtr session);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtStartCapture(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtPutBool(IntPtr thisPtr, byte value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtGetSurface(IntPtr thisPtr, out IntPtr surface);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtGetTimeSpan(IntPtr thisPtr, out long ticks);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtGetContentSize(IntPtr thisPtr, out SizeInt32 size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtGetInterface(IntPtr thisPtr, ref Guid iid, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtClose(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtQueryInterface(IntPtr thisPtr, ref Guid iid, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint VtRelease(IntPtr thisPtr);

    private static readonly Guid IidGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IidGraphicsCaptureItemInterop = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    private static readonly Guid IidFramePoolStatics2 = new("589B103F-6BBC-5DF5-A991-02E28B3B66D5");
    private static readonly Guid IidSession2 = new("2C39AE40-7D2E-5044-804E-8B6799D4CF9E");
    private static readonly Guid IidSession3 = new("F2CDD966-22AE-5EA1-9596-3A289344C3BE");
    private static readonly Guid IidDxgiInterfaceAccess = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static readonly Guid IidTexture2D = typeof(ID3D11Texture2D).GUID;
    private static readonly Guid IidClosable = new("30D5A829-7FA4-4026-83BB-D75BAE4EA99E");

    private readonly HostLog _log;
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IntPtr _winRtDevice;
    private readonly IntPtr _item;
    private readonly IntPtr _pool;
    private readonly IntPtr _session;
    private readonly VtTryGetNextFrame _tryGetNextFrame;
    private readonly VtRecreate _recreate;
    private SizeInt32 _poolSize;
    private bool _roInitialized;
    private bool _disposed;

    public ID3D11Device Device => _device;
    public ID3D11DeviceContext Context => _context;
    public int Width => _poolSize.Width;
    public int Height => _poolSize.Height;

    public WgcCaptureSession(IntPtr hwnd, HostLog log)
    {
        _log = log;
        var roHr = RoInitialize(RoInitMultithreaded);
        if (roHr < 0 && roHr != RpcEChangedMode)
            Marshal.ThrowExceptionForHR(roHr);
        _roInitialized = roHr >= 0;

        D3D11.D3D11CreateDevice(
            IntPtr.Zero,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null!,
            out var device,
            out var context).CheckError();
        _device = device;
        _context = context;

        using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
        CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var winRtDevice);
        _winRtDevice = winRtDevice;

        _item = CreateCaptureItem(hwnd);
        var getSize = GetMethod<VtGetSize>(_item, 7);
        Marshal.ThrowExceptionForHR(getSize(_item, out _poolSize));
        if (_poolSize.Width <= 0 || _poolSize.Height <= 0)
            throw new InvalidOperationException("WGC returned a zero-sized capture item.");

        _pool = CreateFramePool(_winRtDevice, _poolSize);
        _tryGetNextFrame = GetMethod<VtTryGetNextFrame>(_pool, 7);
        _recreate = GetMethod<VtRecreate>(_pool, 6);

        var createSession = GetMethod<VtCreateCaptureSession>(_pool, 10);
        Marshal.ThrowExceptionForHR(createSession(_pool, _item, out _session));

        TrySetSessionBool(_session, IidSession2, slot: 7, value: false, "cursor capture");
        TrySetSessionBool(_session, IidSession3, slot: 7, value: false, "capture border");

        var startCapture = GetMethod<VtStartCapture>(_session, 6);
        Marshal.ThrowExceptionForHR(startCapture(_session));
        _log.Write($"WGC_STARTED | hwnd=0x{hwnd.ToInt64():X} | size={_poolSize.Width}x{_poolSize.Height}");
    }

    public bool TryGetNextFrame(out WgcFrame? frame)
    {
        frame = null;
        if (_disposed) return false;

        var hr = _tryGetNextFrame(_pool, out var framePtr);
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
        if (framePtr == IntPtr.Zero) return false;

        try
        {
            var getSize = GetMethod<VtGetContentSize>(framePtr, 8);
            Marshal.ThrowExceptionForHR(getSize(framePtr, out var contentSize));

            long systemRelativeTicks = 0;
            var getTime = GetMethod<VtGetTimeSpan>(framePtr, 7);
            var timeHr = getTime(framePtr, out systemRelativeTicks);
            if (timeHr < 0) systemRelativeTicks = 0;

            var getSurface = GetMethod<VtGetSurface>(framePtr, 6);
            Marshal.ThrowExceptionForHR(getSurface(framePtr, out var surfacePtr));
            try
            {
                var access = ComQueryInterface(surfacePtr, IidDxgiInterfaceAccess);
                try
                {
                    var getInterface = GetMethod<VtGetInterface>(access, 3);
                    var textureIid = IidTexture2D;
                    Marshal.ThrowExceptionForHR(getInterface(access, ref textureIid, out var texturePtr));
                    var texture = new ID3D11Texture2D(texturePtr);
                    frame = new WgcFrame(
                        framePtr,
                        surfacePtr,
                        texture,
                        Math.Max(1, contentSize.Width),
                        Math.Max(1, contentSize.Height),
                        systemRelativeTicks);
                    framePtr = IntPtr.Zero;
                    surfacePtr = IntPtr.Zero;
                    return true;
                }
                finally
                {
                    ComRelease(access);
                }
            }
            finally
            {
                ComRelease(surfacePtr);
            }
        }
        finally
        {
            ComCloseAndRelease(framePtr);
        }
    }

    public void Recreate(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (width == _poolSize.Width && height == _poolSize.Height) return;

        var next = new SizeInt32 { Width = width, Height = height };
        Marshal.ThrowExceptionForHR(_recreate(_pool, _winRtDevice, PixelFormatBgra8, 3, next));
        _poolSize = next;
        _log.Write($"WGC_RECREATED | size={width}x{height}");
    }

    private IntPtr CreateCaptureItem(IntPtr hwnd)
    {
        const string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
        Marshal.ThrowExceptionForHR(WindowsCreateString(className, className.Length, out var hstring));
        try
        {
            var factoryIid = IidGraphicsCaptureItemInterop;
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(hstring, ref factoryIid, out var factory));
            try
            {
                var createForWindow = GetMethod<VtCreateForWindow>(factory, 3);
                var itemIid = IidGraphicsCaptureItem;
                Marshal.ThrowExceptionForHR(createForWindow(factory, hwnd, ref itemIid, out var item));
                return item;
            }
            finally { ComRelease(factory); }
        }
        finally { WindowsDeleteString(hstring); }
    }

    private static IntPtr CreateFramePool(IntPtr device, SizeInt32 size)
    {
        const string className = "Windows.Graphics.Capture.Direct3D11CaptureFramePool";
        Marshal.ThrowExceptionForHR(WindowsCreateString(className, className.Length, out var hstring));
        try
        {
            var iid = IidFramePoolStatics2;
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(hstring, ref iid, out var factory));
            try
            {
                var create = GetMethod<VtCreateFreeThreaded>(factory, 6);
                Marshal.ThrowExceptionForHR(create(factory, device, PixelFormatBgra8, 3, size, out var pool));
                return pool;
            }
            finally { ComRelease(factory); }
        }
        finally { WindowsDeleteString(hstring); }
    }

    private void TrySetSessionBool(IntPtr session, Guid interfaceId, int slot, bool value, string label)
    {
        try
        {
            var iface = ComQueryInterface(session, interfaceId);
            try
            {
                var setter = GetMethod<VtPutBool>(iface, slot);
                var hr = setter(iface, value ? (byte)1 : (byte)0);
                if (hr >= 0) _log.Write($"WGC_SESSION_OPTION | {label}={value}");
            }
            finally { ComRelease(iface); }
        }
        catch (Exception ex)
        {
            _log.Write($"WGC_SESSION_OPTION_UNAVAILABLE | {label} | {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static T GetMethod<T>(IntPtr comPtr, int slot) where T : Delegate
    {
        var vtable = Marshal.ReadIntPtr(comPtr);
        var address = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static IntPtr ComQueryInterface(IntPtr ptr, Guid iid)
    {
        var query = GetMethod<VtQueryInterface>(ptr, 0);
        var local = iid;
        Marshal.ThrowExceptionForHR(query(ptr, ref local, out var result));
        return result;
    }

    private static void ComRelease(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return;
        try { GetMethod<VtRelease>(ptr, 2)(ptr); }
        catch { }
    }

    private static void ComCloseAndRelease(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return;
        try
        {
            var closable = ComQueryInterface(ptr, IidClosable);
            try { GetMethod<VtClose>(closable, 6)(closable); }
            finally { ComRelease(closable); }
        }
        catch { }
        ComRelease(ptr);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ComCloseAndRelease(_session);
        ComCloseAndRelease(_pool);
        ComRelease(_item);
        ComCloseAndRelease(_winRtDevice);
        _context.Dispose();
        _device.Dispose();
        if (_roInitialized) RoUninitialize();
    }

    internal sealed class WgcFrame : IDisposable
    {
        private IntPtr _frame;
        private IntPtr _surface;
        public ID3D11Texture2D Texture { get; }
        public int ContentWidth { get; }
        public int ContentHeight { get; }
        public long SystemRelativeTicks { get; }

        public WgcFrame(
            IntPtr frame,
            IntPtr surface,
            ID3D11Texture2D texture,
            int contentWidth,
            int contentHeight,
            long systemRelativeTicks)
        {
            _frame = frame;
            _surface = surface;
            Texture = texture;
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
            SystemRelativeTicks = systemRelativeTicks;
        }

        public void Dispose()
        {
            Texture.Dispose();
            ComRelease(_surface);
            ComCloseAndRelease(_frame);
            _surface = IntPtr.Zero;
            _frame = IntPtr.Zero;
        }
    }
}
