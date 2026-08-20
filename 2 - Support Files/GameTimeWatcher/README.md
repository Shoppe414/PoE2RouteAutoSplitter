# PoE2 GameTimeWatcher v0.4.5 — structure-first multilingual pause detection

## v0.4.5 structure-first multilingual pause detection

GameTimeWatcher now reads the generated `PoE2.Language` setting, but pause recognition is intentionally designed to depend as little as possible on text.

Evidence priority is:

1. **Pause-menu structure/layout — 68% weight.** The primary detector compares the stable centered four-button geometry, button side frames, and row separators while excluding most of the button centers where translated text appears. A strong structure match remains the primary pause invariant.
2. **Paused-state banner — 24% weight.** The second detector looks for the dark horizontal banner with centered bright title text used by the `GAME PAUSED` state. It evaluates the banner/title shape rather than requiring the exact English letters, so translated equivalents can still corroborate the pause layout.
3. **English Resume / Exit text templates — 4% each.** These are low-weight corroborators only and are searched only when the selected PoE2 game language is English. They cannot prove pause by themselves.

The MTX Shop remains a separate visual state. The existing percentage thresholds remain user-adjustable in SetupUI. Because the new masked structure score differs from the older full-template score, the existing default structure threshold is a field-calibration value in this development build; diagnostics should be collected if a language/display configuration misses or falsely recognizes pause.


## v0.4.4 shared user settings

GameTimeWatcher accepts `--settings <poe2_run_settings.json>`. SetupUI passes the generated snapshot automatically. The shared user-facing section can override the provisional input timeout plus the five validated pause/MTX template thresholds without editing the watcher's advanced `config.json`. Missing or invalid shared settings fail back to the validated component config.

The master values are edited through SetupUI or `1 - User Setup\PoE2AS-Settings.json`. Generate / Deploy copies the effective values into `LiveSplit Target\poe2_run_settings.json`, and the setup SHA-256 manifest validates that snapshot.


## v0.4.3 pause-accounting integration

v0.4.3 keeps the v0.4.2 provisional timestamp protocol and pairs it with the ASL-side accounting update in the full package. On ESC/controller-Start, GameTimeWatcher publishes `PENDING_PAUSE` or `PENDING_RUN` immediately with the original input timestamp. The screen remains authoritative.

**Important:** the provisional behavior only affects LiveSplit when the deployed ASL also supports manual-pause protocol v2.1. Rebuilding GameTimeWatcher alone is not enough. Re-run PoE2RouteSetup from the matching package and point LiveSplit's Scriptable Auto Splitter component at that newly generated `.asl`.

The matching ASL now:
- provisionally pauses on `PENDING_PAUSE`;
- provisionally resumes on `PENDING_RUN`;
- rewinds an accepted pause to the original input timestamp while the timer is still frozen;
- refunds a rejected pause candidate when ESC only closed another in-game window;
- removes falsely counted time if a provisional resume is rejected.


## v0.4.2 provisional timestamp timing

GameTimeWatcher remains an optional helper for excluding true PoE2 manual-pause time from LiveSplit Game Time. Loading-screen removal is still handled directly by the ASL from `Client.txt`.

v0.4.2 separates **timer response** from **visual verification**. A foreground ESC/controller-Start edge is timestamped on the dedicated 5 ms input thread and immediately published as `PENDING_PAUSE` or `PENDING_RUN`. The ASL can react on its next update instead of waiting roughly 0.5-0.75 seconds for the full visual check. The screen detector remains authoritative: a confirmed transition resolves the pending state, while a rejected transition is compensated by the ASL so final Game Time is unchanged.

State protocol v2 adds `stateSequence` and `originUtcTicks` alongside `RUNNING`, `PAUSED`, `PENDING_PAUSE`, and `PENDING_RUN`. Missing or stale watcher data still fails open. Diagnostic PNG encoding occurs only after provisional/confirmed state publication so developer diagnostics do not delay the timer handoff.

## v0.3.9 pillarbox/overlay center fix

The 2048x576 in-program menu probes showed that the pause menu itself was captured correctly, but LiveSplit was visible in the right black pillarbox. The old content-bound detector treated that overlay as part of the game image, making the canonical image too wide and shifting the assumed center away from the actual PoE2 pause menu. v0.3.9 identifies separate horizontal content bands and uses the band containing the client center (or the widest band as fallback), so side-bar overlays are ignored.

## v0.3.8 pause-menu capture/structure changes

The v0.3.7 live test confirmed that MTX detection and heartbeat timing were stable, but the short pause-menu text templates produced paradoxical scores: unrelated gameplay could score higher than the actual open pause menu. v0.3.8 therefore stops treating a single text match as authoritative.

- Adds a centered **pause-menu stack** template containing the invariant Resume / Challenges / Options / Microtransaction Shop button geometry.
- Uses the full button stack as the primary pause-menu signal; individual `RESUME GAME`, `GAME PAUSED`, and `EXIT PATH OF EXILE` scores remain as corroborating fallbacks and diagnostics.
- Requires corroboration for text-only pause classification so a high scene correlation cannot pause the timer by itself.
- When ESC or controller Start is pressed in diagnostic mode, saves the exact GameTimeWatcher capture at approximately 100/250/500/1000 ms. These `menu-probe-*.png` files show the actual capture/cropping path rather than a separate desktop screenshot.
- Hardens state-file writes with writer-specific temporary files and retries for transient IO/access races.
- MTX detection and the independent heartbeat thread are unchanged.


## v0.3.7 timing-stability changes

The v0.3.6 live test exposed a detector-throughput problem rather than a LiveSplit timing problem: the nominal 10 FPS visual loop was taking about two seconds per analyzed frame. That made the ASL's two-second heartbeat freshness check repeatedly expire while the watcher was still working, so Game Time could advance in small bursts during a pause. It also turned the existing 2-frame/3-frame confirmation into multi-second entry/exit delays.

v0.3.7 therefore:

- precomputes pause-template scale variants once at startup;
- searches only narrow centered vertical bands for `RESUME GAME`, then `GAME PAUSED`, then `EXIT PATH OF EXILE`;
- lowers the primary Resume threshold to 0.58 based on the live v0.3.6 scores while keeping stricter fallback thresholds;
- refreshes the state-file heartbeat from a dedicated background thread, independent of screenshot analysis;
- logs `analyzeMs` so actual detector throughput is visible;
- saves a rate-limited center-column candidate screenshot in diagnostic mode whenever a pause/MTX signature is near threshold but does not classify.

MTX matching itself is unchanged; its live match score was strong (~0.97), and the previous inconsistency was caused by slow frame analysis plus confirmation latency.

GameTimeWatcher is **not required for normal load-removed Game Time**. The LiveSplit ASL reads Path of Exile 2 `Client.txt` directly and uses the game's own `[LOADING SCREEN] ... Duration = X seconds` records.

GameTimeWatcher is only used when the runner selects **Pause LiveSplit Game Time while PoE2 is manually paused** in the Setup UI.

## What it detects

- The normal ESC / controller-Start pause menu.
  - Primary signature: centered **RESUME GAME** button.
  - Secondary signature: centered **GAME PAUSED** banner.
  - Tertiary signature: centered **EXIT PATH OF EXILE** button.
- The full-screen Microtransaction Shop when opened from the pause menu.

Options and Challenges/Achievements are intentionally treated as running time because the game simulation resumes when those interfaces are opened.

## Pause-menu detector

The watcher canonicalizes the rendered game content to a 576-pixel reference height, then searches only narrow **centered vertical bands** where the pause-menu invariants appear. It does not scan the full width of an ultrawide frame.

Detection priority is:

1. `RESUME GAME` — primary trigger, threshold 0.58.
2. `GAME PAUSED` — secondary fallback, threshold 0.40.
3. `EXIT PATH OF EXILE` — tertiary fallback, threshold 0.50.

The Exit search allows extra vertical space for the one-time **Skip Tutorials** row. The MTX Shop remains a separate visual state because the normal pause-menu invariants disappear after entering the shop.

Use `--dev-console` to show `pause`, `resume`, `banner`, `exit`, `mtx`, and `analyzeMs`. In diagnostic mode, a near-threshold frame that still classifies as gameplay is periodically saved as a center-column PNG so the exact image seen by GameTimeWatcher can be inspected.

## How it communicates with LiveSplit

The Setup UI starts GameTimeWatcher with a small state-file path inside `1 - User Setup\LiveSplit Target`. GameTimeWatcher refreshes that file several times per second. The ASL only honors a `PAUSED` state while the heartbeat is fresh; if GameTimeWatcher is closed or fails, the timer fails open and continues rather than remaining stuck paused.

## Crash diagnostics

Run `Run-Diagnostic.cmd` (or `Run-Diagnostic.ps1`) for development testing. The external launcher records process/resource diagnostics and keeps startup/runtime errors visible.

Diagnostic runs are saved under:

`4-README's_and_Diagnostics\Diagnostics` (logs) and `4-README's_and_Diagnostics\Diagnostics\images` (PNG captures)

The normal GameTimeWatcher runtime/debug log is also centralized at:

`4-README's_and_Diagnostics\Diagnostics\poe2_gametimewatcher.log`

A fatal startup exception is written into the same centralized diagnostics directory as:

`4-README's_and_Diagnostics\Diagnostics\poe2_gametimewatcher_startup_error.log`

You can also run the watcher from an already-open PowerShell window:

```powershell
.\publish\PoE2GameTimeWatcher.exe --state-file "<full path to LiveSplit Target\poe2_manual_pause_state.txt>" --dev-console --wait-on-error
```

## Build/runtime files

`Build.ps1` explicitly copies and validates:

- `config.json`
- `templates\pause-resume-game.png`
- `templates\pause-exit-path-of-exile.png`
- `templates\mtx-shop.png`

The individual Resume/Banner/Exit templates remain in the runtime as corroborating diagnostics/fallbacks, but v0.3.9 uses the multi-button stack as the primary pause-menu signal.
