# v0.4.5-structure-first — development

- Added the shared PoE2 game-language setting to runtime diagnostics.
- Reworked pause detection to prioritize language-neutral pause-menu geometry.
- Added a second-weight paused-banner detector that looks for the dark banner plus centered bright title shape instead of exact English `GAME PAUSED` text.
- English `Resume Game` and `Exit Path of Exile` templates are now low-weight corroborators only and are skipped on non-English game clients.
- Pause evidence weighting is structure 68%, banner 24%, Resume text 4%, Exit text 4%.
- MTX Shop detection and the existing manual-pause state protocol remain unchanged.

# Changelog

## v0.4.4
- Added `--settings <path>` support for SetupUI's generated shared run-settings snapshot.
- User-facing overrides cover ProvisionalTimeoutMs and the pause-stack, Resume Game, pause-banner, Exit Path of Exile, and MTX template thresholds.
- Invalid or missing shared settings fall back to the existing validated component `config.json`.
- External diagnostic launcher now records/hashes/copies the run-settings snapshot.
- Visual detection logic and tested default threshold values are otherwise unchanged from v0.4.3.


## v0.4.3
- Retains the v2 provisional state protocol introduced in v0.4.2.
- Full-package ASLs now apply accepted/rejected provisional timing corrections immediately, including while the pause is active.
- Adds explicit documentation that GameTimeWatcher and the deployed ASL must come from the same protocol-aware package.
- Diagnostic/version identifiers bumped so stale watcher/ASL combinations are easier to identify.

## v0.4.2

- Added protocol-v2 provisional states: `PENDING_PAUSE` and `PENDING_RUN`. ESC/controller Start now publishes the input-edge timestamp immediately instead of waiting for visual classification.
- LiveSplit can provisionally hold/release Game Time on that state, while the normal screen detector remains the final authority.
- Added `originUtcTicks` and `stateSequence` to the state file so ASLs can compensate the small edge-to-poll interval and undo rejected provisional transitions exactly.
- Added a 1200 ms provisional timeout. If an input edge does not visually produce the expected state, GameTimeWatcher resolves back to the last confirmed visual state and the ASL refunds/re-removes the provisional interval.
- Mouse-driven Resume/Options/Challenges transitions publish `PENDING_RUN` on the first visual Running frame, then complete the existing confirmation check.
- Confirmed state is now published before any full-resolution diagnostic PNG encoding. In v0.4.1 the diagnostic screenshot path itself could add several hundred milliseconds before the state-file update.
- Retains the v0.4.1 dedicated 5 ms input thread, centered-content fix, anchored template search, MTX handling, and fail-open heartbeat policy.
- No LiveSplit Pause hotkey is injected or rebound; Game Time remains ASL-controlled.

## v0.4.1

- Reduced pause-template search from broad screen scans to small anchor windows around the stable centered menu positions observed in the real GameTimeWatcher captures.
- Added a dedicated 5 ms ESC/controller-Start input monitor so short input edges are not missed while image analysis is running.
- Input remains an acceleration hint only; screen state is still authoritative.
- Retains the v0.4.0 immediate state-writer wakeup, heartbeat isolation, content-bound fix, MTX behavior, and fail-open policy.
- Intended to remove the ~2 second perceived pause/unpause delay seen at 5120x1440, where v0.4.0 still spent roughly 0.47-0.57 s analyzing each frame and often required two frames.

## v0.4.0

- Added input-assisted low-latency mode: ESC / controller Start forces an immediate high-rate visual check but is never pause authority by itself.
- While PoE2 is visually paused, GameTimeWatcher remains in high-rate capture mode so mouse-click Resume is detected without needing an ESC hotkey.
- A recent ESC/Start edge reduces visual confirmation to one strong frame for entry/exit; normal transitions still retain multi-frame confirmation.
- Replaced periodic-sleep heartbeat publication with a signaled single-writer heartbeat thread so confirmed state changes are written immediately instead of waiting up to one heartbeat interval.
- Reduced ConfirmRunningFrames from 3 to 2 for click-based Resume latency.
- Added state-aware matcher short-circuiting: strong pause-stack or MTX matches avoid redundant template searches.
- Added cached pillarbox/content bounds, a direct 24bpp grayscale path, typical-scale-first matching with strong-match early exit, and one-pass NCC math.
- Dev status now reports NORMAL/FAST detector mode and active capture FPS.
- No LiveSplit Pause hotkey is injected or rebound; ASL Game Time remains authoritative.

## v0.3.9

- Fixed pillarbox/overlay content-bound detection exposed by the 2048x576 live menu probes.
- GameTimeWatcher now selects the centered/widest contiguous game-content band instead of joining the PoE2 render surface to an always-on-top LiveSplit window in a black side bar.
- This restores the correct visual center for Resume Game / GAME PAUSED / Exit Path of Exile / menu-stack matching.
- MTX detection, heartbeat behavior, ASL timing, and BossWatcher are unchanged.

## v0.3.8

- Replaced single-text pause authority with a centered multi-button stack signature (Resume / Challenges / Options / Microtransaction Shop).
- Kept Resume Game, GAME PAUSED, and Exit Path of Exile as corroborating fallbacks/diagnostics.
- Added ESC/controller-Start menu-probe screenshots at approximately 100/250/500/1000 ms from GameTimeWatcher's own capture path.
- Hardened state-file writes against transient access races.
- MTX detection and the independent heartbeat thread remained unchanged.

## v0.3.7

- Fixed the major v0.3.6 detector-throughput regression that reduced a nominal 10 FPS loop to roughly one analyzed frame every ~2 seconds in the live test.
- Precompute all pause-template scale variants once at startup instead of rebuilding them per frame.
- Restrict pause matching to narrow centered vertical bands.
- Use detection priority `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE` with thresholds 0.58 / 0.40 / 0.50.
- Move the state-file heartbeat to an independent background thread so slow image analysis cannot make the ASL heartbeat expire mid-pause.
- Keep 2-frame pause and 3-frame running confirmation; with restored detector throughput these become sub-second confirmation instead of multi-second delays.
- Add `analyzeMs` to developer diagnostics.
- Save a rate-limited center-column candidate screenshot when a near-threshold pause/MTX frame still classifies as gameplay.
- MTX template matching itself is unchanged; live v0.3.6 scores were strong (~0.97), and its inconsistent timing came from analysis/confirmation latency.


## v0.3.6

- Pause-menu detection priority is now `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE`.
- Added the `GAME PAUSED` banner back as a secondary confirmation signature instead of using it as the sole detector.
- Pause-menu signatures are searched only around the center of the rendered game content.
- Added multi-scale template matching to tolerate PoE2 UI scaling differences across 16:9, ultrawide, and super-ultrawide resolutions.
- Broadened vertical search windows while keeping the horizontal search constrained to the center column.
- Developer diagnostics now report independent `resume`, `banner`, and `exit` scores.

# GameTimeWatcher changelog

## 0.3.5 - Centered pause-menu button detector

- Replaced `GAME PAUSED` banner matching with centered menu-button matching.
- Primary pause signature is `RESUME GAME`; `EXIT PATH OF EXILE` is a fallback.
- The search is restricted to a narrow central column after canonicalizing the game content to 576 pixels high, so normal display-width/aspect-ratio changes do not require a full-screen template search.
- The Exit fallback uses a wider vertical search window to tolerate the one-time `Skip Tutorials` menu row.
- Dev-console/internal diagnostics now report `resume`, `exit`, aggregate `pause`, and `mtx` scores separately.
- MTX Shop detection is unchanged.
- Added and explicitly staged `pause-resume-game.png` and `pause-exit-path-of-exile.png`.

## 0.3.4 - Literal-path diagnostic launcher fix

- Replaced PowerShell `Start-Process` in the external diagnostic launcher with `System.Diagnostics.ProcessStartInfo`.
- This avoids PowerShell 5.1 path binding on the literal folder name `GameTimeWatcher`, whose square brackets can be interpreted as wildcard syntax.
- Diagnostic stdout/stderr now inherit the persistent PowerShell console so startup errors remain visible instead of disappearing with the child process.
- Diagnostic launches pass `--wait-on-error`, while persistent startup failures continue to be written beside the LiveSplit state file.
- Process sampling, exit-code capture, final-state capture, internal watcher diagnostics, and Windows Application event-log collection remain enabled.

## 0.3.3 - Startup hardening and persistent diagnostics

- GameTimeWatcher now fails with an explicit path when `config.json` or either visual template is missing instead of silently creating a fallback config and then exiting during template load.
- Template paths are resolved relative to the selected config file, with a fallback from `publish\config.json` to the support-folder `config.json`.
- `Build.ps1` explicitly copies and validates `config.json`, `pause-menu-tight.png`, and `mtx-shop.png` in the published runtime so SDK content-copy behavior cannot leave the executable without its detector data.
- Any fatal startup exception now also writes `poe2_gametimewatcher_startup_error.log` beside the LiveSplit state file.
- Normal Setup-UI launches use `--wait-on-error`; a startup failure keeps the watcher console open until Enter is pressed.
- Developer diagnostic launches use PowerShell `-NoExit`, and `Run-Diagnostic.cmd` now remains open and forwards any supplied arguments.
- The default detector thresholds/templates now match the v0.3.2 runtime config even if defaults are used.

## 0.3.2 - Selective pause-menu matching

- Replaced broad dark-screen similarity with normalized grayscale correlation against a tight `GAME PAUSED` banner.
- Added the more selective MTX Shop correlation detector.
- Reduced image-processing overhead and added state-change screenshots in diagnostic mode.
- This revision addressed the false `PauseMenu` state seen in ordinary dark gameplay.

## 0.3.1 - External crash diagnostics and resource hardening

- Dispose all Process wrappers returned by repeated game-window scans and throttle process discovery to 250 ms to prevent native handle accumulation.
- Added `Run-Diagnostic.ps1` / `Run-Diagnostic.cmd`, an external watchdog that captures stdout/stderr, exit code, process memory/handle/thread/CPU samples, final state, and matching Windows crash events.
- Added optional internal `--diagnostic-dir` logging with per-second detector/runtime/GC status and managed exception logging.
- State-file and watcher-log write failures are now caught and logged instead of terminating the watcher.
- Loading, boss, story, movement, and input detection remain outside GameTimeWatcher; its runtime responsibility is still only manual pause-menu / MTX detection.

## 0.3.0
- Re-scoped GameTimeWatcher to an optional manual-pause helper only.
- Removed loading, boss-bar, voice-line, cutscene, movement, keyboard, and controller gameplay-response tracking.
- Loading-screen removal is now handled directly by each ASL from `Client.txt`.
- Added automatic `GAME PAUSED` menu detection.
- Added automatic Microtransaction Shop detection.
- Added a fresh-heartbeat state file consumed by the ASL.
- Paused state is latched while PoE2 is not foreground so a runner can pause and alt-tab during a break.
