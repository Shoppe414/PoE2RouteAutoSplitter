# Path of Exile 2 Route AutoSplitter for LiveSplit — v2.0.0

v2.0.0 changes distribution from a source-first package into an installer/release workflow. Normal users no longer need PowerShell, the .NET SDK, OCR setup, or compilation.

## Normal user installation

Download the Windows installer from the GitHub Release:

`PoE2RouteAutoSplitter-v2.0.0-Setup.exe`

Run it and launch **PoE2 Route AutoSplitter** from the Start Menu, optional desktop shortcut, or the installed `1 - User Setup\PoE2RouteSetup.exe`.

The default install location is the current user's Local AppData folder, so the Setup UI can safely update its dedicated `LiveSplit Target` without requiring administrator access.

The installer deploys a self-contained Windows runtime. It also installs the Microsoft Visual C++ x64 runtime used by BossWatcher's OCR/native dependencies.

## Installed runtime layout

The installed application preserves the established two-folder runtime structure:

```text
PoE2RouteAutoSplitter
├── 1 - User Setup
│   ├── PoE2RouteSetup.exe
│   └── LiveSplit Target\
└── 2 - Support Files
    ├── Setup UI [Configuration]\ui-manifest.json
    ├── BossWatcher [Boss Rush Detection]\publish\...
    ├── 01 - Ordered Route [Exploration]\...
    ├── ...
    └── 14 - Custom Route [Exploration + Boss Rush]\...
```

`LiveSplit Target` remains the fixed deployment directory. Each successful Setup UI deployment replaces that target's active generated setup after confirmation.

The installer intentionally preserves `LiveSplit Target` across application upgrades/uninstalls so user-generated setup files are not silently destroyed.

## LiveSplit setup

1. Launch `PoE2RouteSetup.exe`.
2. Select a premade configuration or build a custom route.
3. Press **Generate / Deploy Selected Setup**.
4. Open the generated `.lss` from `1 - User Setup\LiveSplit Target` in LiveSplit.
5. Keep your own LiveSplit layout.
6. Add/edit the layout's **Scriptable Auto Splitter** component and point it at the generated `.asl` in `LiveSplit Target`.
7. For Boss Rush or mixed routes, press **Start BossWatcher** in the Setup UI.

The Setup UI does not generate, copy, or modify `.lsl` files.

## Source repository vs. release assets

Compiled executables are intentionally not committed to Git. `.gitignore` excludes generated `publish` folders, OCR language data, installer output, release artifacts, and the large user executables.

The source repository contains the build definitions. GitHub Releases contain the ready-to-run files:

- `PoE2RouteAutoSplitter-v2.0.0-Setup.exe` — recommended Windows installer.
- `PoE2RouteAutoSplitter-v2.0.0.zip` — portable self-contained runtime.
- `SHA256SUMS.txt` — release asset checksums.

## Automated GitHub release

`.github\workflows\build-release.yml` runs for version tags such as `v2.0.0` and can also be started manually.

The workflow:

1. checks out the repository;
2. installs the .NET 10 SDK;
3. uses the Inno Setup 6 compiler provided by the Windows 2025 GitHub-hosted runner;
4. downloads Tesseract English OCR data;
5. builds the Setup UI and BossWatcher as self-contained Windows applications;
6. assembles the two-folder runtime;
7. creates the portable ZIP;
8. creates the Windows installer;
9. creates SHA-256 checksums;
10. uploads workflow artifacts; and
11. for a version tag, creates/updates the matching GitHub Release assets.

Normal users never run these build steps.

## Local developer release build

Requirements:

- Windows x64
- .NET 10 SDK
- Inno Setup 6
- Internet access for NuGet, Tesseract OCR data, and the Microsoft VC++ redistributable

From `2 - Support Files`:

```powershell
.\Build-Release.ps1 -Version 2.0.0
```

Generated release files are written to the repository-level `artifacts` directory. The installer is also copied to `1 - User Setup` for convenient local testing, but is ignored by Git.

For development-only builds without creating an installer:

```powershell
.\Build-User-Tools.ps1
```

## Setup UI

The Setup UI exposes all 41 bundled premade `.lss` profiles and the mixed custom-route builder. The custom builder can combine supported areas and bosses, reorder objectives, choose ordered/unordered completion, and select manual or area-based timer start.

Deployment is staged before the active target is touched. Once generation succeeds, replacing a non-empty target requires confirmation.

## BossWatcher v0.3.0 console

Normal BossWatcher output is event-focused: timestamped boss encounters, boss defeats, and fight duration in seconds. Verbose frame-by-frame detector output remains available through `--dev-console` or the Setup UI's **Developer console diagnostics** option.

## Mode directories

1. `01 - Ordered Route [Exploration]`
2. `02 - Flexible Route [Exploration]`
3. `03 - Level Race`
4. `04 - Area Checklist and Flexible Act Rush [Exploration]`
5. `05 - Ordered Segment and Act Practice [Exploration]`
6. `06 - Campaign 100 - Dynamic [Boss Rush]`
7. `07 - Campaign 100 - Predefined [Boss Rush]`
8. `08 - Campaign Any v0.5 - Dynamic [Boss Rush]`
9. `09 - Campaign Required Bosses Only v0.5 - Predefined [Boss Rush]`
10. `10 - Pinnacle v0.5 - Dynamic [Boss Rush]`
11. `11 - Pinnacle v0.5 - Predefined [Boss Rush]`
12. `12 - Campaign 100 - Dynamic [Exploration + Boss Rush]`
13. `13 - Campaign Any v0.5 - Dynamic [Exploration + Boss Rush]`
14. `14 - Custom Route [Exploration + Boss Rush]`

Campaign 100% retains 67 boss targets. The required-boss v0.5 baseline retains 40 bosses. The merged Campaign 100% route remains 165 objectives and merged Campaign Any% v0.5 remains 117 objectives.
