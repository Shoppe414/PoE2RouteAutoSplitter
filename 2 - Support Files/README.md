# Path of Exile 2 Route AutoSplitter for LiveSplit — v1.4.0

v1.4.0 simplifies the release into two root folders

## Release layout

The release root contains only:

- `1 - User Setup`
- `2 - Support Files`

### 1 - User Setup

This is the normal user-facing folder.

After building the Setup UI it contains:

- `PoE2RouteSetup.exe`
- `LiveSplit Target\`

`LiveSplit Target` is the fixed deployment directory used by the Setup UI. Each successful deployment replaces the contents of that directory with the selected/generated `.lss`, `.asl`, route/config files, and `SETUP_INFO.txt`.

### 2 - Support Files

This contains everything else: all autosplitter mode sources, BossWatcher, Setup UI source/build files, route catalogs, documentation, validation data, and developer/support files.

## Recommended setup

1. Open `2 - Support Files\Setup UI [Configuration]`.
2. Run `Build.ps1` once. This publishes a single-file launcher to `1 - User Setup\PoE2RouteSetup.exe` and ensures `1 - User Setup\LiveSplit Target` exists.
3. For Boss Rush or mixed modes, build BossWatcher as well. If OCR data is missing, run `2 - Support Files\BossWatcher [Boss Rush Detection]\Setup-OCR.ps1`, then `Build.ps1` in that folder.
4. Launch `1 - User Setup\PoE2RouteSetup.exe`.
5. Select a premade setup or build a custom route.
6. Press **Generate / Deploy Selected Setup**.
7. Open the generated `.lss` from `1 - User Setup\LiveSplit Target` in LiveSplit.
8. Use your own LiveSplit layout. Add/edit its **Scriptable Auto Splitter** component and point it to the generated `.asl` in `LiveSplit Target`.
9. For Boss Rush or mixed routes, press **Start BossWatcher** in the Setup UI.

The Setup UI does not generate, copy, or modify `.lsl` files.

## Setup UI

The Setup UI exposes all bundled premade `.lss` profiles plus the mixed custom-route builder. The custom builder can combine supported areas and bosses, reorder objectives, choose ordered/unordered completion, and select manual or area-based timer start.

Deployment is staged before the active target is touched. Once generation succeeds, a non-empty `LiveSplit Target` requires confirmation before its contents are replaced.

The target path is intentionally fixed to `1 - User Setup\LiveSplit Target`; there is no arbitrary target-folder selector in v1.4.0.

## BossWatcher v0.3.0 console

Normal BossWatcher output is event-focused: timestamped boss encounters, boss defeats, and fight duration in seconds. The verbose frame-by-frame detector output remains available through `--dev-console` or the Setup UI's **Developer console diagnostics** option.

## Mode directories

All mode directories are under `2 - Support Files`:

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
