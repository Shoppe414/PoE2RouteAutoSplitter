# Changelog

## v2.0.0

### Installer and release distribution
- Replaced the source-first normal-user setup with a Windows installer workflow.
- Normal users now install `PoE2RouteAutoSplitter-v2.0.0-Setup.exe` and do not need PowerShell, the .NET SDK, OCR setup scripts, or local compilation.
- Added an Inno Setup 6 installer definition under `Installer [Windows]`.
- The installer deploys to the current user's Local AppData by default and preserves the established `1 - User Setup` / `2 - Support Files` runtime structure.
- `LiveSplit Target` is intentionally preserved across installer upgrades/uninstalls.
- Setup UI and BossWatcher release builds are now self-contained Windows applications.
- The installer includes the Microsoft Visual C++ x64 redistributable used by BossWatcher's OCR/native dependencies.
- Added a portable self-contained ZIP as an alternative to the installer.

### Git/GitHub release pipeline
- Added repository `.gitignore` rules for compiled executables, `bin`/`obj`/`publish`, downloaded OCR data, release staging, and installer output.
- Added `Build-Release.ps1` to build both applications, assemble a runtime-only package, create the portable ZIP, compile the installer, and generate SHA-256 release checksums.
- Local release builds copy the installer into `1 - User Setup` for developer convenience while keeping it ignored by Git.
- Added `.github/workflows/build-release.yml`. Version tags such as `v2.0.0` build the Windows release and publish the installer, portable ZIP, and checksum file as GitHub Release assets.
- Manual workflow dispatch can build the same artifacts without creating a tagged release.

### Runtime behavior
- No `.lsl` generation was reintroduced. Users continue to maintain their own LiveSplit layout and manually point its Scriptable Auto Splitter component at the deployed `.asl`.
- No boss lists, route definitions, exploration logic, BossWatcher detection logic, or autosplitter split semantics were changed for v2.0.0.

## v1.4.0

### Two-folder user/support package layout
- Restructured the release root to contain only `1 - User Setup` and `2 - Support Files`.
- `1 - User Setup` is the normal user-facing folder and contains `PoE2RouteSetup.exe` after build plus the dedicated `LiveSplit Target` directory.
- Moved all mode directories, BossWatcher files, Setup UI source/build files, documentation, and other support content under `2 - Support Files`.
- Setup UI `Build.ps1` now publishes as a single-file executable and copies `PoE2RouteSetup.exe` into `1 - User Setup`.
- The Setup UI now uses the fixed `1 - User Setup\LiveSplit Target` directory; the arbitrary target-folder Browse control was removed.
- Removed the optional starter `.lsl` layout and its UI button entirely. No `.lsl` files are included, generated, copied, or modified.
- Updated package discovery so the launcher in Subfolder 1 resolves catalogs, mode sources, and BossWatcher from Subfolder 2.
- No boss lists, route definitions, BossWatcher detection logic, or autosplitter engine behavior changed.

## v1.3.3

### LiveSplit layout safety fix
- Removed all automatic `.lsl` generation from the Setup UI after generated layouts were found capable of causing LiveSplit startup/loading failures.
- Premade and custom deployment now output only `.lss`, `.asl`, runtime route/config files, and setup information.
- `SETUP_INFO.txt` now records the exact deployed ASL path and tells users to manually point a Scriptable Auto Splitter component at it.
- Added a **Starter Layout Folder** button to the Setup UI.
- Added `Optional LiveSplit Layout [User Editable]\Path of Exile 2 - Starter Layout.lsl`, based on a manually created LiveSplit layout.
- Removed the starter layout's machine-specific Scriptable Auto Splitter component/ASL path and reset its screen position to `(50, 50)` for portability.
- Existing user layouts are never modified by the Setup UI.
- No boss, area, route, detector, or BossWatcher behavior changed.

## v1.3.2

### PowerShell bracket-path hotfix
- Fixed `BossWatcher [Boss Rush Detection]\Build.ps1` falsely reporting that `tessdata\eng.traineddata` was missing when the file existed.
- `Test-Path` now uses `-LiteralPath`, preventing `[Boss Rush Detection]` from being interpreted as a wildcard character class.
- BossWatcher build-time `Copy-Item` sources now use `-LiteralPath` for the same reason.
- Applied the equivalent literal-path check to `01 - Ordered Route [Exploration]\tools\Generate-LiveSplitSplits.ps1` for absolute output directories containing brackets.
- No route, boss catalog, detector, LiveSplit, Setup UI, or console behavior changes.

## v1.3.1

### Setup UI startup hotfix
- Fixed a startup exception in the Custom Route tab caused by applying `SplitContainer` minimum-panel sizes and splitter position while the control still had its small default construction size.
- The custom split pane now receives a valid working size before `Panel1MinSize`, `Panel2MinSize`, and `SplitterDistance` are applied.
- Added user-visible fatal-error handling for startup and WinForms UI-thread exceptions instead of allowing a `WinExe` startup failure to appear as a silent exit.
- Added `PoE2RouteSetup-crash.log` reporting beside the executable when writable, with the system temporary directory as a fallback.
- No route definitions, boss lists, exploration logic, BossWatcher detection behavior, or LiveSplit generation semantics changed in this hotfix.

## v1.3.0

### Setup UI
- Added a Windows Forms setup/configuration application under `Setup UI [Configuration]`.
- Added selection of all 41 bundled premade `.lss` profiles.
- Added automatic generation of a matching `.lsl` layout for the selected profile. **This behavior was removed in v1.3.3 after compatibility problems were found.**
- Added a custom route builder with searchable area and boss catalogs, mixed area/boss objectives, ordered/unordered completion, route reordering, and manual/area-based timer start.
- Custom `.lss` generation uses the selected area/boss names and also writes `CUSTOM_OBJECTIVES.txt`.
- Added validation preventing an auto-start area from also being configured as a split objective, avoiding an uncompletable first-area objective.
- Deployed ASLs use target-directory runtime paths, allowing LiveSplit setup files to live in one dedicated target rather than requiring config files beside `LiveSplit.exe`.
- Deployment is staged before the target is touched; a non-empty target is cleared only after successful generation and explicit confirmation.
- Added target safety checks that reject drive/package roots and destructive system/user roots such as the profile, Desktop/Documents themselves, AppData, ProgramData, Windows/System, Program Files, and the system temp root.
- Added a UI button to launch BossWatcher against the selected target event log; it requires a generated setup marker and prevents accidental duplicate watcher instances.
- Added a UI checkbox for the retained verbose developer console.

### BossWatcher v0.3.0 console
- Normal user mode no longer prints the per-frame detector status line.
- User output now reports timestamped encounter and defeat events.
- Defeat output includes fight duration in seconds from the recorded `SEEN` encounter timestamp to the boss disappearance `firstMissing` timestamp.
- Added `--dev-console` / `--dev` to retain the previous verbose diagnostic display.
- Added `--event-file <path>` so launchers can explicitly choose the BossWatcher event log location.
- `RETURNED` events and diagnostic details remain in log files but are not printed in normal user mode.

### Build / packaging
- Added `Build-User-Tools.ps1` to build both the Setup UI and BossWatcher.
- Updated BossWatcher `Run-Source.ps1` with `-DevConsole` and `-EventFile` arguments.

## v1.2.0

### Boss-list corrections
- Added Beira of the Rotten Pack to the supported boss catalog and Campaign 100% list.
- Removed Beira from the unstable miniboss exclusion list.
- Removed The Rotten Druid and Diamora, Song of Death from the required-boss v0.5 list.
- Renamed Campaign Any v0.5 - Predefined to Campaign Required Bosses Only v0.5 - Predefined.
- Campaign 100% now has 67 boss targets; required-boss v0.5 now has 40.

### Package integration
- Merged v1.1.0 Exploration modes and BossRush v0.2.0 into one package.
- Added explicit `[Exploration]`, `[Boss Rush]`, and `[Exploration + Boss Rush]` directory labels.
- Added Campaign 100% mixed mode (165 objectives).
- Added Campaign Any% v0.5 Dynamic mixed mode (117 objectives).
- Added Custom Route mixed mode with ordered/unordered `area|...` and `boss|...` objectives plus a layout generator.
- Preserved BossRush Real Time backdating for boss objectives in mixed modes.

## v1.1.0
- Exploration release baseline imported unchanged except for directory naming.
