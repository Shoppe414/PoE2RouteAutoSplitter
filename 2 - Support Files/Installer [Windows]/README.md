# Windows Installer — v2.0.1

`PoE2RouteAutoSplitter.iss` is the Inno Setup 6 definition used by `..\Build-Release.ps1` and the GitHub Actions release workflow.

Do not manually place compiled release binaries in Git. The installer is a generated release artifact.

## What the installer contains

- self-contained `PoE2RouteSetup.exe`;
- the complete runtime route/mode data needed by the Setup UI;
- self-contained BossWatcher runtime;
- Tesseract English OCR data;
- Microsoft Visual C++ 2015-2022 x64 redistributable; and
- the fixed `LiveSplit Target` directory.

## Installation behavior

Default installation location:

`%LOCALAPPDATA%\PoE2RouteAutoSplitter`

The installer creates a Start Menu shortcut and offers an optional desktop shortcut.

Upgrades replace the Setup UI and support/runtime files but deliberately preserve:

`1 - User Setup\LiveSplit Target`

This protects the user's currently generated LiveSplit setup.

## Build

From `2 - Support Files`:

```powershell
.\Build-Release.ps1 -Version 2.0.1
```

The script locates `ISCC.exe`, assembles a runtime staging package, and compiles the installer. The GitHub Actions workflow uses the Inno Setup compiler already provided on the Windows 2025 hosted runner.
