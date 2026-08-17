# Windows Installer — v2.2.1 Release Candidate

`PoE2AS.iss` is the Inno Setup 6 definition used by
`..\Build-Release.ps1` and the GitHub Actions release workflow.

Do not manually place compiled release binaries in Git. The installer is a
generated release artifact.

## What the installer contains

- self-contained `PoE2RouteSetup.exe`;
- complete runtime route/mode data needed by the Setup UI;
- self-contained BossWatcher runtime and OCR data;
- self-contained optional GameTimeWatcher runtime;
- Microsoft Visual C++ 2015-2022 x64 redistributable; and
- the fixed `LiveSplit Target` directory.

## Game Time

Load removal is performed by the deployed ASL from Path of Exile 2
`Client.txt`. The optional GameTimeWatcher executable is installed only so
runners can elect to pause LiveSplit Game Time while PoE2 is manually paused.

## Installation behavior

Default installation location:

`%LOCALAPPDATA%\PoE2RouteAutoSplitter`

Upgrades replace the Setup UI and support/runtime files but deliberately
preserve:

`1 - User Setup\LiveSplit Target`

## Build

From `2 - Support Files`:

```powershell
.\Build-Release.ps1 -Version 2.2.1
```

The script locates `ISCC.exe`, assembles a short-path runtime staging package,
and compiles the installer.
