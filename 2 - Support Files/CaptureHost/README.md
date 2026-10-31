# PoE2 CaptureHost (WGC)

`PoE2CaptureHost.exe` owns the single Windows Graphics Capture session used by
BossWatcher and GameTimeWatcher. The watcher processes no longer perform GDI
screen acquisition.

The host targets the PoE2 HWND with Windows.Graphics.Capture, reads only the
requested GPU regions back to CPU memory, and publishes BGR24 frames through
named memory-mapped channels:

- `Local\PoE2AS.WGC.Boss.v1`
- `Local\PoE2AS.WGC.Pause.v1`
- control/heartbeat: `Local\PoE2AS.WGC.Control.v1`

BossWatcher and GameTimeWatcher publish their desired sampling rates into the
control map. This lets one WGC source serve different consumer rates without
creating multiple capture sessions.

The pause viewport is scaled to the configured canonical 16:9 size on a
below-normal-priority background worker. The WGC/D3D capture loop remains free
to deliver boss frames while pause scaling is in progress.

No PoE2 memory is read and no code is injected into the game process.
