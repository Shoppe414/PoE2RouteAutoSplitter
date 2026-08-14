namespace PoE2GameTimeWatcher;

/// <summary>
/// Polls ESC / controller Start independently of image analysis so short key presses
/// cannot be missed while a screenshot is being canonicalized or template-matched.
/// The input is only an acceleration hint; visual confirmation remains authoritative.
/// </summary>
public sealed class MenuInputMonitor : IDisposable
{
    private readonly DiagnosticLogger _diagnostics;
    private readonly Thread _thread;
    private readonly Action<MenuInputSnapshot>? _onEdge;
    private volatile bool _stop;
    private long _sequence;
    private long _lastInputUtcTicks;
    private readonly object _sourceGate = new();
    private string _lastSource = "none";

    public MenuInputMonitor(DiagnosticLogger diagnostics, Action<MenuInputSnapshot>? onEdge = null)
    {
        _diagnostics = diagnostics;
        _onEdge = onEdge;
        _thread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "PoE2GameTimeWatcher-MenuInput"
        };
        _thread.Start();
    }

    public MenuInputSnapshot Snapshot()
    {
        var sequence = Interlocked.Read(ref _sequence);
        var ticks = Interlocked.Read(ref _lastInputUtcTicks);
        string source;
        lock (_sourceGate) source = _lastSource;
        return new MenuInputSnapshot(sequence,
            ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue,
            source);
    }

    private void PollLoop()
    {
        bool lastEscDown = false;
        bool lastControllerStartDown = false;

        while (!_stop)
        {
            try
            {
                bool escDown = (NativeMethods.GetAsyncKeyState(0x1B) & 0x8000) != 0;
                bool controllerStartDown = IsControllerStartDown();
                bool escEdge = escDown && !lastEscDown;
                bool controllerEdge = controllerStartDown && !lastControllerStartDown;

                if (escEdge || controllerEdge)
                {
                    var utc = DateTime.UtcNow;
                    var source = escEdge ? "ESC" : "ControllerStart";
                    lock (_sourceGate) _lastSource = source;
                    Interlocked.Exchange(ref _lastInputUtcTicks, utc.Ticks);
                    var sequence = Interlocked.Increment(ref _sequence);
                    _diagnostics.Log("MENU_INPUT_EDGE", $"source={source}");
                    try { _onEdge?.Invoke(new MenuInputSnapshot(sequence, utc, source)); }
                    catch (Exception callbackEx) { _diagnostics.LogException("MENU_INPUT_CALLBACK_ERROR", callbackEx); }
                }

                lastEscDown = escDown;
                lastControllerStartDown = controllerStartDown;
            }
            catch (Exception ex)
            {
                _diagnostics.LogException("MENU_INPUT_MONITOR_ERROR", ex);
            }

            Thread.Sleep(5);
        }
    }

    private static bool IsControllerStartDown()
    {
        const ushort XINPUT_GAMEPAD_START = 0x0010;
        try
        {
            for (uint i = 0; i < 4; i++)
            {
                if (NativeMethods.XInputGetState(i, out var state) == 0 &&
                    (state.Gamepad.wButtons & XINPUT_GAMEPAD_START) != 0)
                    return true;
            }
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        return false;
    }

    public void Dispose()
    {
        _stop = true;
        try { _thread.Join(250); } catch { }
    }
}

public readonly record struct MenuInputSnapshot(long Sequence, DateTime Utc, string Source);
