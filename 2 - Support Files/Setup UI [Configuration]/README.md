# PoE2 Route AutoSplitter Setup UI — v2.0.2

This is the Windows configuration tool for the Route AutoSplitter package.

## Normal users

Normal users should not build this project. Install the ready-to-run `PoE2RouteAutoSplitter-v2.0.2-Setup.exe` GitHub Release asset instead.

The installed launcher is:

`1 - User Setup\PoE2RouteSetup.exe`

and it deploys LiveSplit files into:

`1 - User Setup\LiveSplit Target`

## LiveSplit layouts

The Setup UI does **not** generate, copy, or modify `.lsl` files.

After deployment:

1. open the generated `.lss` from `LiveSplit Target`;
2. keep your own LiveSplit layout;
3. add/edit a **Scriptable Auto Splitter** component; and
4. browse that component to the generated `.asl` in `LiveSplit Target`.

The exact deployed ASL path is also written to `SETUP_INFO.txt`.

## Premade setups

The **Premade setups** tab exposes all 41 bundled `.lss` presets from Exploration, Boss Rush, mixed Exploration + Boss Rush, and Level Race modes.

Deployment first builds the complete setup in a temporary staging directory, then asks before replacing a non-empty `LiveSplit Target`.

## Custom route

The **Custom route** tab lets the user search and combine supported areas and bosses, reorder objectives, choose ordered/unordered completion, and choose manual or area-based timer start.

Custom deployment generates the custom `.lss`, mixed-objective `.asl`, route file, objective summary, optional boss event log, and `SETUP_INFO.txt`.

## BossWatcher button

**Start BossWatcher** launches the installed BossWatcher from `2 - Support Files\BossWatcher [Boss Rush Detection]` and directs its event log to the active `LiveSplit Target`. Check **Developer console diagnostics** only when verbose frame diagnostics are needed.

## Developer build

Requirements: Windows and .NET 10 SDK.

From this directory:

```powershell
.\Build.ps1
```

The Setup UI is published as a **self-contained single-file Windows executable** and copied to:

```text
..\..\1 - User Setup\PoE2RouteSetup.exe
```

A self-contained build is intentionally large and should not be committed to Git. The repository `.gitignore` excludes it.

To build both tools without an installer, run from `2 - Support Files`:

```powershell
.\Build-User-Tools.ps1
```

To create the actual distributable installer and portable ZIP, use:

```powershell
.\Build-Release.ps1 -Version 2.0.2
```
