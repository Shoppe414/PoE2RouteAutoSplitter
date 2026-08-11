# PoE2 Route AutoSplitter Setup UI — v1.4.0

This is the configuration tool for the Route AutoSplitter package.

## Fixed user folder

The release uses two top-level folders. The Setup UI source lives here under `2 - Support Files`, but `Build.ps1` publishes the user launcher to:

`1 - User Setup\PoE2RouteSetup.exe`

The build also ensures this dedicated deployment directory exists:

`1 - User Setup\LiveSplit Target`

The UI always deploys to that target. The target path is read-only in the UI; arbitrary deployment folders are no longer selected.

## LiveSplit layouts

The Setup UI does **not** generate, copy, or modify `.lsl` files. No starter layout is included.

After deployment:

1. open the generated `.lss` from `LiveSplit Target`;
2. keep your own LiveSplit layout;
3. add/edit a **Scriptable Auto Splitter** component;
4. browse that component to the generated `.asl` in `LiveSplit Target`.

The exact deployed ASL path is also written to `SETUP_INFO.txt`.

## Premade setups

The **Premade setups** tab exposes all 41 bundled `.lss` presets from Exploration, Boss Rush, mixed Exploration + Boss Rush, and Level Race modes.

Deployment first builds the complete setup in a temporary staging directory, then asks before replacing a non-empty `LiveSplit Target`.

## Custom route

The **Custom route** tab lets the user search and combine supported areas and bosses, reorder objectives, choose ordered/unordered completion, and choose manual or area-based timer start.

Custom deployment generates the custom `.lss`, mixed-objective `.asl`, route file, objective summary, optional boss event log, and `SETUP_INFO.txt`.

## BossWatcher button

**Start BossWatcher** launches the built BossWatcher from `2 - Support Files\BossWatcher [Boss Rush Detection]` and directs its event log to the active `LiveSplit Target`. Check **Developer console diagnostics** only when verbose frame diagnostics are needed.

## Build

Requirements: Windows and .NET 10 SDK.

From this directory:

```powershell
.\Build.ps1
```

The Setup UI is published as a framework-dependent **single-file executable** and copied to:

```text
..\..\1 - User Setup\PoE2RouteSetup.exe
```

To build both Setup UI and BossWatcher, run from `2 - Support Files`:

```powershell
.\Build-User-Tools.ps1
```
