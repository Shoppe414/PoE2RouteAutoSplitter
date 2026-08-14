# v2.1.3 development — Custom Add All + premade trial scheduling

- Added **Add All** buttons to the Custom Areas and Bosses subtabs. Add All operates on the currently visible Content/search list so users can quickly build a complete Act/Interlude/Pinnacle set and prune it in the route preview.
- Removed internal area/boss IDs from SetupUI display text, including catalog/start-zone labels, premade trial insertion dropdowns, and dedicated Trials policy descriptions. Runtime/config IDs remain internal.
- Premade Sekhemas now defaults to Floor 1 only. Each selected Floor 1–4 segment has its own ordered-route **Run after** selector.
- Premade Chaos now exposes 4-round, 7-round, and 10-round stages. Each selected stage has its own ordered-route **Run after** selector; optional Trialmaster follows the scheduled 10-round stage.
- Later trial stages default to the end of the selected route until the user chooses a planned return point. Floor 1 Sekhemas and 4-round Chaos retain the established campaign defaults.
- If multiple trial stages use the same predecessor, the generator preserves natural stage order (for example Sekhemas Floor 2 before Floor 3).
- Added mixed-ASL `areaocc|AREA~N` support so premade Area Completion can track multiple separate Trial of Chaos visits even though all stages reuse the same active game area.
- Trial-area successor handling now completes a separated Sekhemas floor on return to its lobby and a Chaos stage on return to the Temple staging area when the next configured objective is outside the trial.

# v2.1.3 development — Custom catalog cleanup + repeated/dynamic boss encounters

- Reworked the Custom route Areas/Bosses catalog UI around an Act / Interlude / Pinnacle filter. Internal area/boss IDs are no longer shown in the selectable lists or route preview.
- Added-object behavior is now exclusive: once an area or boss is added, it disappears from the available list until removed from the route. Trial content remains in its separate opt-in selector.
- Added ordered **Multi-boss / repeated encounters** support. When enabled, each boss row exposes an Occurrences value (minimum 1, default 1); adding a boss creates individually keyed `bossocc|BOSS~N` objectives.
- Added dynamic/unordered custom boss-pool policy. Selected bosses become eligible pool members and **Boss encounters required** controls how many qualifying BossWatcher defeats are needed. The default is derived from `campaign-any-v0.5.txt` (currently 40). Repeat kills of the same eligible boss count as separate encounters.
- Added shared mixed-ASL `bossslot|N`, `@bossPool=...`, and `@bossTarget=N` support. Dynamic boss-pool rows are renamed to the boss BossWatcher actually identifies.
- Updated the custom layout generator and `.lss` labels for repeated and dynamic custom boss objectives.

# v2.1.3 development — dedicated Trials runtime

- Removed the previous development-status labels from functional Trials UI/runtime choices. Active Challenges Only remains explicitly NON-FUNCTIONAL and in active development.
- Boss policy now defaults/recommends **Each boss kill**.
- Exit policy now defaults/recommends **Trial completion / exit only**.

- Enabled Generate / Deploy from the Trials tab for the first functional trial-run test implementation.
- Dedicated trial starts are fixed to first active chamber entry: normalized `Sanctum_1_*` for Sekhemas and `G3_10` for Chaos. Manual start was removed from the Trials rules.
- Full Trial is the only functional timing scope. Active Challenges Only remains visible but disabled as **NON-FUNCTIONAL — Active development**.
- Added **Boss policy** (default) and **Exit policy** finish rules. Boss policy requires BossWatcher; Exit policy can run without BossWatcher when no boss splits are requested.
- Exit detection uses the validated return transitions from the supplied logs: `G2_13` for Sekhemas and `G3_10_Airlock` for Chaos.
- Reworked trial split-frequency choices to **Each boss kill**, **Final boss only**, and **Trial completion / exit only**.
- Four-floor Sekhemas Each-boss mode creates five boss splits: Rattlecage, both Hadi/Rafiq kills in either order, Ashar, and Zarokh.
- Added restricted Hadi/Rafiq dynamic slots so their two individual Floor-2 splits can occur in either kill order.
- Added `bossnth|TARGET` support to the shared mixed ASL. Chaos Final-boss-only categories without Trialmaster silently count prior random Chaos boss deaths and split only on the selected Nth stage (4-round=1st, 7-round=2nd, 10-round=3rd).
- Trial generation writes `Trial-Run.lss`, `PoE2-Trial.asl`, `poe2_mixed_route.txt`, and `TRIAL_RULES.txt`.
- Existing loading-screen Game Time removal and optional GameTimeWatcher manual-pause removal are retained.

# v2.1.3 development — compact premade generator + opt-in trials

- Replaced the large Premade preset list with **Mode**, **Setup**, and **Ordered / Dynamic** selectors.
- Premade Mode choices are Area Completion, Boss Completion, Area + Boss Completion, and Level Race. Setup choices cover Campaign 100% / Any%, Acts, Interludes, All Interludes, Act/Interlude combinations, and Pinnacle where applicable.
- Premade routes are assembled at Generate time from the existing validated area and boss catalogs and use the mixed route runtime format. Static preset files remain packaged for compatibility/reference.
- Removed hard-coded trial inclusion from premade semantics. Sekhemas and Chaos are now independent opt-ins.
- Sekhemas uses sequential Floor 1–4 selections; enabling it defaults to Floors 1–2, leaving Floors 3–4 as explicit later-progression choices.
- Chaos boss-based routes use sequential Chaos Boss 1/2/3 dynamic slots plus optional Trialmaster. Area-based Chaos routes add the active Trial of Chaos area once because the rounds share one logged area instance.
- Ordered area routes default Sekhemas insertion after The Halani Gates and Chaos after Chimeral Wetlands. Ordered boss routes default Sekhemas after Jamanra, the Risen King and Chaos after Xyclucian, the Chimera; all insertion points are user-selectable.
- Dynamic routes ignore trial insertion order and add the selected trial objectives to the unordered completion count.
- Added Act/Interlude combination generation. Any% combination rules use the Any% baseline for Acts and the available full route for selected Interludes.
- Area + Boss premades remain Dynamic / unordered until a validated interleaved ordered mixed baseline is defined.
- Added Sekhemas `Sanctum_1_*` through `Sanctum_4_*` and active Chaos `G3_10` area recognition to the shared mixed ASL used by generated premades.
- Cleared the SetupUI nullable-font warning by falling back to `Control.DefaultFont` if `SystemFonts.MessageBoxFont` is unavailable.

# v2.1.3 development — Chaos dynamic subset objectives

- Replaced fixed Custom Route Uxmal/Chetza/Bahlak milestones with generic **Chaos Boss 1 / Chaos Boss 2 / Chaos Boss 3** slots.
- Added Custom mixed-ASL `bossany|SLOT` objectives. Each Chaos slot accepts one BossWatcher defeat from the restricted Uxmal/Chetza/Bahlak pool and dynamically renames the LiveSplit row to the detected boss.
- Ordered routes therefore retain a fixed route position without predicting the random Chaos boss identity; unordered routes consume the next incomplete Chaos slot. Trialmaster remains deterministic.
- The same boss identity may satisfy a later Chaos slot if the game selects it again; completion is tracked by encounter slot, not permanently by identity.

# v2.1.3 development — optional trial boss catalog

- Expanded BossWatcher OCR catalog from 78 to 85 identities by adding the missing Sekhemas and Chaos trial bosses.
- Reclassified Zarokh and The Trialmaster as trial identities for Custom-route UI grouping while preserving their existing pinnacle target IDs/lists.
- Added **Add trial boss objectives** to Custom Routes; trial bosses remain hidden from the ordinary Bosses search until explicitly opted into through the dedicated checklist.
- Checked trial milestones are inserted into the route preview, can be moved normally in ordered routes, and are normal completion objectives in unordered routes.
- Added a composite `bossall|sekhemas_floor2` objective so Hadi and Rafiq complete one floor milestone after both BossWatcher `GONE` events, independent of their kill order.
- Chaos bosses remain individual opt-in identities because their order is randomized; no fixed Chaos boss order is assumed by Custom Routes.
- Existing premade campaign/pinnacle target lists are unchanged.

# v2.1.3 development — custom level progression

- Ordered custom routes now automatically **Skip Split** for a level milestone that was already reached before that milestone becomes the active objective; the missed level is suppressed, not completed.
- Added a **Trials** SetupUI tab for policy/layout refinement. Trial type, length, timing scope, start rule, finish rule, and split frequency are selectable and reflected in a live policy preview.
- Trials Generate/runtime support remains intentionally disabled until Sekhemas and Chaos logs validate reliable event boundaries.
- Trial policies identify recommended/default choices and alternative rules directly in the selection UI.
- Added **Add level progression** to the Custom route SetupUI with Max Level and Split Interval controls.
- Level milestones are generated using the existing Level Race policy: interval milestones plus the selected Max Level as the final milestone.
- Generated level milestones appear in the custom objective preview and can be moved among area and boss objectives.
- Generation validates that all configured level milestones remain present and appear in strictly ascending order.
- Custom mixed ASL now accepts `level|N` objectives and splits on `Client.txt` `is now level N` records.
- Individual generated level milestones are controlled by the level-progression settings rather than removed one-at-a-time.
- Existing area, boss, start-policy, loading-screen Game Time, and GameTimeWatcher behavior is unchanged.

# Changelog

## v2.1.2 release-candidate administrative cleanup — SetupUI reminders
- Removed the redundant post-generation success/instructions popup.
- Preserved the target-directory deletion confirmation before replacing a non-empty `LiveSplit Target`.
- Added a persistent SetupUI reminder to attach the generated `.asl` to LiveSplit's Scriptable Auto Splitter component.
- Added a persistent reminder that LiveSplit must be set to **Game Time** for loading screens and optional manual-pause time to be excluded from the displayed run time; Real Time continues counting those periods.
- Normalized final release metadata/default build version from stale `2.1.0` / `2.1.1-dev` values to `2.1.2` so `Build-Release.ps1 -Version 2.1.2` passes its manifest-version check.

## v2.1.2 release-candidate hotfix - compact Windows paths
- Shortened the portable archive root to `PoE2AS-v2.1.2-RC`.
- Condensed all 14 mode folders, BossWatcher, GameTimeWatcher, installer, ASL, LSS, and helper-script filenames.
- Updated `ui-manifest.json`, build scripts, workflow artifact names, installer references, documentation, and generator output filenames to the compact paths.
- Removed generated .NET `bin` / `obj` trees and stale diagnostic/generated LiveSplit output from the release archive.
- Preserved the four SetupUI binary-compatible runtime anchors: `1 - User Setup`, `2 - Support Files`, `Setup UI [Configuration]`, and `LiveSplit Target`.
- `Build-Release.ps1` now discovers compact mode folders with the `NN-Name` pattern and produces `PoE2AS-vX.Y.Z` release artifacts.


## v2.1.2 release-candidate hotfix - manual-pause state read-race grace
- Hardened the optional GameTimeWatcher state-file reader in all 14 source ASLs. The release `LiveSplit Target` is intentionally empty until SetupUI generates the selected setup.
- A missing, locked, or malformed `poe2_manual_pause_state.txt` read now retains the last successfully parsed fresh watcher state for up to 500 ms instead of immediately treating one failed 25 ms poll as `RUNNING`.
- The grace never extends a stale heartbeat: the cached heartbeat must still be inside the existing 2-second freshness window.
- A successfully read stale heartbeat still fails open to `RUNNING` immediately.
- Added `GT_MANUAL_READ_GRACE`, `GT_MANUAL_READ_RECOVERED`, and `GT_MANUAL_READ_GRACE_EXPIRED` diagnostics.
- `GT_READY` now reports `readGraceMs=500` so deployed patched ASLs are easy to identify.
- GameTimeWatcher v0.4.3 templates, thresholds, state writer, provisional timing protocol, load timing, BossWatcher, and SetupUI start-policy behavior are unchanged.

## v2.1.1 dev - GameTimeWatcher v0.4.3 / manual-pause accounting
- The GameTimeWatcher `PENDING_PAUSE` / `PENDING_RUN` protocol is now fully accounted for by all 14 ASLs.
- Accepted ESC pauses can rewind Game Time to the original input timestamp while still paused.
- Rejected ESC pause candidates refund the provisional hold when ESC only closes another in-game interface.
- Rejected provisional resumes re-remove falsely counted time.
- Manual-pause state polling reduced from 50 ms to 25 ms.
- Added `manualPauseProtocol=v2.1-provisional-accounting` to Game Time debug startup output so stale ASLs are obvious.
- Setup UI instructions now explicitly require redeploying/repointing the ASL when the manual-pause protocol changes.

## v2.1.1 development - GameTimeWatcher v0.4.2 provisional timestamp timing

- Optional manual-pause timing now uses protocol-v2 provisional `PENDING_PAUSE` / `PENDING_RUN` states.
- ESC/controller Start writes its timestamp immediately; all 14 ASLs react provisionally on their next update and compensate the interval if visual verification later rejects the candidate.
- Mouse-driven Resume/Options/Challenges publishes a provisional Running state on the first visual gameplay frame.
- GameTimeWatcher publishes state before diagnostic PNG encoding so developer diagnostics cannot delay the timer handoff.
- No LiveSplit Pause hotkey is rebound or injected.
- Wounded Man campaign start, Client.txt load timing, exploration/boss logic, and BossWatcher are unchanged.

## v2.1.1 development hotfix — GameTimeWatcher v0.3.7 timing stability

- Confirmed the Riverbank Wounded-Man start gate and Client.txt loading-screen removal remain unchanged.
- Diagnosed the v0.3.6 visual loop running at roughly one analyzed frame every ~2 seconds instead of the configured 10 FPS.
- The slow detector caused the ASL's 2-second heartbeat freshness check to repeatedly expire while the watcher was still alive, producing small bursts of Game Time during a pause.
- The same throughput problem stretched 2-frame pause / 3-frame running confirmation into multi-second MTX entry/exit delays.
- GameTimeWatcher now precomputes scaled pause templates once at startup and searches only narrow centered vertical bands.
- Pause-menu priority remains `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE`, with separate thresholds 0.58 / 0.40 / 0.50.
- The state heartbeat now runs on an independent background thread, so image-analysis latency cannot make the heartbeat stale.
- Added `analyzeMs` to diagnostics and rate-limited near-threshold center-column screenshots for exact capture debugging.
- MTX template matching itself is unchanged because the live shop score remained strong (~0.97).

## v2.1.1 development hotfix — GameTimeWatcher v0.3.5 pause-menu button detector

- Confirmed Riverbank start gate and Client.txt load-removed Game Time remain unchanged.
- Replaced GameTimeWatcher's `GAME PAUSED` banner detector with centered `RESUME GAME` detection plus `EXIT PATH OF EXILE` fallback.
- Pause-menu matching now searches only the canonicalized central column, making normal display-width/aspect-ratio differences irrelevant to the horizontal search.
- Added separate dev-console/diagnostic scores for Resume, Exit, aggregate pause, and MTX detection.
- MTX Shop detection remains unchanged.

## v2.1.1 development hotfix — GameTimeWatcher v0.3.4 literal-path launcher

- Fixed the external GameTimeWatcher diagnostic launcher failing before the watcher started when the support folder name contained literal square brackets.
- The launcher now starts the watcher with `.NET System.Diagnostics.ProcessStartInfo` instead of PowerShell `Start-Process`.
- Startup exceptions remain visible in the diagnostic console and `--wait-on-error` is enabled.

## v2.1.1 development hotfix — GameTimeWatcher v0.3.3 startup diagnostics

- Preserved the Wounded Man start gate introduced in v0.2.17-startgate.
- GameTimeWatcher now keeps its console open on fatal startup errors when launched from the Setup UI and the developer diagnostic launcher uses PowerShell `-NoExit`.
- Fatal startup exceptions are also persisted beside the LiveSplit target state file as `poe2_gametimewatcher_startup_error.log`.
- GameTimeWatcher build output now explicitly copies and validates `config.json` and both visual template files in `publish`, avoiding silent runtime data omissions.
- Config/template resolution now reports the exact missing path if startup data cannot be found.
- No BossWatcher, route, boss catalog, or split semantics were changed.

## v2.1.1 development hotfix — start-gate verification + GameTimeWatcher v0.3.2

- Ordered campaign ASL now identifies itself as `v0.2.17-startgate` in the debug READY line. A log that still says `v0.2.16` or starts directly on `AREA G1_1` is running a stale ASL path.
- Setup UI now explicitly warns that LiveSplit keeps the previous Scriptable Auto Splitter file path when a new dev ZIP is extracted to a different folder.
- GameTimeWatcher v0.3.2 replaces the false-positive-prone dark-pixel matcher with normalized-correlation matching and a tight `GAME PAUSED` banner template.
- GameTimeWatcher frame matching now uses contiguous grayscale buffers rather than repeated `GetPixel` calls, reducing detector stalls that could make its heartbeat appear stale.

## v2.1.0 development patch — Riverbank start policy and GameTimeWatcher diagnostics

### Campaign start policy
- Campaign autosplitters whose start is `G1_1` / The Riverbank now **arm** on Riverbank entry instead of starting LiveSplit immediately.
- The actual timer start is the newly appended Client.txt line `Wounded Man: Reach... Clearfell... Find the Miller...`.
- The new-character wake-up/setup period, movement to the first NPC, and the first Wounded Man interaction are intentionally untimed so runners have a brief deterministic setup window before the attempt.
- Non-Riverbank configurable/custom start areas retain their existing area-entry start behavior.
- Pinnacle Boss Rush modes 10-11 remain manual-start.
- Riverbank remains an exploration objective and existing successor-entry completion semantics are unchanged.

### GameTimeWatcher v0.3.1 crash diagnostics / resource hardening
- Added an external `Run-Diagnostic.ps1` / `Run-Diagnostic.cmd` watchdog that launches GameTimeWatcher, captures stdout/stderr, samples working/private/virtual memory, handle count, thread count, CPU time and responsiveness, records the watcher exit code, and queries Windows Application crash events.
- Added a best-effort internal `watcher-internal.log` with startup/config/template/state transitions, managed exceptions, per-second resource/GC status, and state-file/log I/O errors.
- State-file/log write failures are now diagnostic events rather than reasons for GameTimeWatcher to terminate.
- Fixed a plausible long-run resource leak by disposing every `Process` wrapper returned by repeated `Process.GetProcessesByName` scans.
- Throttled game-window/process discovery to 250 ms instead of enumerating processes on every ~10 ms main-loop iteration.
- Setup UI **Developer console diagnostics** now launches the external GameTimeWatcher watchdog when the optional manual-pause helper is started.
- Installed/release runtimes now include the external diagnostic launcher beside the optional GameTimeWatcher runtime.

## v2.1.0

### Load-removed Game Time
- Added Game Time support to all 14 autosplitter engines.
- Each ASL tails Path of Exile 2 `Client.txt` with an independent reader so Game Time processing does not consume or interfere with route/boss progression events.
- `Got Instance Details` begins the live loading pause.
- PoE2's `[LOADING SCREEN] (...) Duration = X seconds` value is treated as the authoritative completed loading-screen duration.
- After each load, the ASL corrects the small difference between observed live-pause time and the duration reported by PoE2.
- The opening Riverbank load occurs before the new Wounded Man start gate and is discarded from the attempt when LiveSplit starts; Game Time cannot become negative from pre-start load bookkeeping.
- Boss intros/outros, dialogue, and scripted story events remain timed; they are not treated as loads or free inventory-management time.
- Added an enabled-by-default ASL setting: `Game Time: remove Client.txt-reported loading screens`.

### Optional manual-pause removal
- Re-scoped GameTimeWatcher to v0.3.1 as an optional manual-pause helper with crash diagnostics/resource hardening.
- GameTimeWatcher is no longer involved in load, boss-bar, voice-line, cutscene, movement, keyboard, or controller gameplay-response detection.
- The helper now recognizes only the actual `GAME PAUSED` menu and Microtransaction Shop using external screen capture/template matching.
- Options and Challenges/Achievements intentionally remain timed because PoE2 unpauses while those interfaces are open.
- Added a heartbeat-based `poe2_manual_pause_state.txt`; stale/missing state fails open so Game Time cannot remain stuck paused if the helper exits.
- Setup UI now exposes `Pause LiveSplit Game Time while PoE2 is manually paused` and a `Start GameTimeWatcher` button.
- Manual-pause removal is disabled by default and is patched into the deployed ASL only when selected during setup.

### Packaging
- Release builds now compile and stage the self-contained optional GameTimeWatcher runtime alongside Setup UI and BossWatcher.
- Updated installer/release workflow defaults to v2.1.0.
- Preserved the two-root-folder runtime, user-owned LiveSplit layout policy, zero `.lsl` generation, short-path release staging, and existing BossWatcher behavior.

## v2.0.2

### Campaign Boss Rush Riverbank auto-start
- Campaign Boss Rush-only modes now auto-start LiveSplit when `Client.txt` reports entry into `G1_1` / The Riverbank.
- Applied to Campaign 100% Dynamic, Campaign 100% Predefined, Campaign Any% Dynamic, and Campaign Required Bosses Only Predefined.
- The start detector uses the same Riverbank area-entry signal used by campaign exploration; it does **not** use area departure as a completion/start signal.
- BossWatcher remains responsible only for boss encounter/defeat events and is not used to start the timer.
- The `autoStart` ASL setting defaults to enabled and can be disabled for manual start.
- If `Client.txt` cannot be opened, Boss Rush remains usable with manual timer start and the problem is written to the bridge debug log.
- Pinnacle Boss Rush Dynamic and Predefined intentionally remain manual-start.
- No boss whitelist, boss split, OCR, firstMissing backdating, exploration route, or combined-mode behavior changed.

## v2.0.1

### Release build path-length hotfix
- Fixed `Build-Release.ps1` failing during cleanup when deeply nested staged runtime paths exceeded the classic Windows 260-character `MAX_PATH` boundary.
- Expanded portable-runtime staging now uses a short system temporary path instead of nesting the full runtime beneath the repository checkout path.
- Added long-path-safe fallback cleanup for stale `artifacts` directories left by an interrupted/failed build.
- The final installer, portable ZIP, and SHA-256 files are still written to the repository `artifacts` directory as before.

### Riverbank exploration correction
- Added The Riverbank (`G1_1`) to Campaign 100% and Campaign Any% exploration accounting.
- Ordered campaign routes now use **successor-entry completion** when the route begins with Riverbank: entering Riverbank starts the timer, and only entering the next configured route area completes the active segment.
- Returning to town, visiting a hideout, or revisiting an earlier area does not complete the current ordered segment unless that destination is the exact configured successor.
- Campaign 100% ordered splits now contain 99 area rows; Campaign Any% ordered contains 78.
- Flexible/checklist exploration modes count Riverbank as implicitly completed by the auto-start event without adding a zero-time LiveSplit split.
- Combined Campaign 100% and Any% modes use successor-entry completion for their exploration objectives while boss objectives remain dynamic. Their totals are now 166 and 118 objectives respectively.
- Custom mixed routes retain entry-based area objectives unless `@areaCompletion=successor` is explicitly present.

## v2.0.0

### Installer and release distribution
- Replaced the source-first normal-user setup with a Windows installer workflow.
- Normal users now install `PoE2RouteAutoSplitter-v2.0.0-Setup.exe` and do not need PowerShell, the .NET SDK, OCR setup scripts, or local compilation.
- Added an Inno Setup 6 installer definition under `Installer`.
- The installer deploys to the current user's Local AppData by default and preserves the established `1 - User Setup` / `2 - Support Files` runtime structure.
- `LiveSplit Target` is intentionally preserved across installer upgrades/uninstalls.
- Setup UI and BossWatcher release builds are now self-contained Windows applications.
- The installer includes the Microsoft Visual C++ x64 redistributable used by BossWatcher's OCR/native dependencies.
- Added a portable self-contained ZIP as an alternative to the installer.

### Git/GitHub release pipeline
- Added repository `.gitignore` rules for compiled executables, `bin`/`obj`/`publish`, downloaded OCR data, release staging, and installer output.
- Added `Build-Release.ps1` to build both applications, assemble a runtime-only package, create the portable ZIP, compile the installer, and generate SHA-256 release checksums.
- Fixed Inno Setup discovery so local release builds recognize both machine-wide installs and WinGet/current-user installs under `%LOCALAPPDATA%\Programs\Inno Setup 6`, with registry-based fallback for custom install locations.
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
- Fixed `BossWatcher\Build.ps1` falsely reporting that `tessdata\eng.traineddata` was missing when the file existed.
- `Test-Path` now uses `-LiteralPath`, preventing `[Boss Rush Detection]` from being interpreted as a wildcard character class.
- BossWatcher build-time `Copy-Item` sources now use `-LiteralPath` for the same reason.
- Applied the equivalent literal-path check to `01-Ordered\tools\Gen-Ordered.ps1` for absolute output directories containing brackets.
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
- Added `Build-Tools.ps1` to build both the Setup UI and BossWatcher.
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
