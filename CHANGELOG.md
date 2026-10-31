# PoE2 Route AutoSplitter — Unified Changelog

## v3.2.0 — Structural geometry hardening / load suppression / OCR isolation (BossWatcher 0.4.3)

- Promoted the validated structural-geometry-hardening WGC build to **v3.2.0** after successful field testing. No detector-policy changes were introduced by the promotion itself; v3.2.0 is the release version for this validated code line.
- Updated SetupUI, CaptureHost, installer, release-build defaults, GitHub release workflow defaults, package documentation, localized README version labels, verification metadata, and source-package naming to 3.2.0.
- Added an ASL-owned `poe2_load_state.txt` protocol. Every generated/source ASL marks `LOADING` at authoritative Client.txt load start and `RUNNING` at load completion. BossWatcher cancels existing acquisition/tracking and suppresses new boss events while the state is `LOADING`, preventing loading-screen artwork from becoming an anonymous boss.
- Added full-ROI health-run geometry before OCR-lane analysis. A single-boss candidate must now exhibit a boss-scale coherent health structure across the complete capture, preventing ordinary centered enemy health bars from looking full-width merely because they fill a local OCR lane. Dual topology also requires independent boss-scale left/right full-ROI health structure before lane OCR is allowed.
- Added upper plausibility bounds for boss-name gold density. Extremely gold-heavy loading art/UI is rejected before anonymous arming.
- Retained the fast one-frame path for real boss bars, but it now requires both the prior inner health/name signal and the new boss-scale full-ROI structure.
- Applied the same boss-scale sanity gate to generic map structural acquisition.
- Hardened OCR preprocessing by removing long horizontal frame/border artifacts from single-boss masks as well as dual lanes, then blanking distant mask components outside the centered text cluster while preserving mask dimensions for temporal OCR. Tesseract remains in `SingleLine` mode and the 0.72 identity threshold is unchanged.
- Preserved anonymous timing, ring buffering, post-disappearance OCR, ordered/custom provisional resolution, Sekhemas deterministic fallback, Chaos no-repeat elimination, and `firstMissing` backdating semantics.
- Consolidated release/change history into this root `CHANGELOG.md`; it is the sole package changelog.
- Updated all localized user READMEs to document load suppression, boss-scale structural validation, fail-closed unresolved behavior, and the specialized ordered/Sekhemas/Chaos fallback policies.
- Removed legacy DPI-awareness declarations from the WinForms watcher manifests while retaining `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` in the projects, eliminating the .NET 10 `WFO0003` duplicate-DPI warning without changing effective DPI behavior.


### Anonymous route-resolution iteration (BossWatcher 0.4.2 / Mixed ASL 1.3.12)

- Unresolved structurally verified single-boss defeats now emit `ANON_GONE` instead of being discarded. The event preserves structural first-seen and `firstMissing` timing.
- Buffered post-disappearance OCR now logs per-frame/per-mask text, confidence, nearest boss candidate, similarity, and acceptance threshold without relaxing OCR matching.
- Generated mixed/custom routes add specialized resolution policies:
  - Sekhemas fixed-floor deterministic fallback for Floors 1, 3, and 4; Floor 2 uses two-boss occurrence/elimination handling.
  - Chaos anonymous occurrence support plus retroactive no-repeat identity elimination when the remaining identity is mathematically unique.
  - Ordered/custom ordered routes may advance an unidentified boss provisionally and confirm contiguous provisional predecessors when the next named boss matches route order.
  - Contradictions disable further provisional anonymous fallback and require manual Undo/reconciliation; automatic LiveSplit rollback is intentionally not attempted.
- Dynamic/unordered campaign routing does not infer boss identity from area alone.

This is the single authoritative change timeline for the package. It consolidates the former project, BossWatcher, GameTimeWatcher, ordered-engine, flexible-engine, and v3.0.0 RC release-note histories. Entries are newest first. Component versions are retained where they are useful for identifying the code that shipped with a project release.

## v3.1.0 — Controller profiles, display-scaling hardening, fixed-cost pause analysis, and reliable mixed-route area progression

### Release status

- Promoted the validated buildfix9 line from the v3.0.0 Release Candidate series to **v3.1.0**.
- Updated SetupUI, installer, release-build defaults, GitHub release workflow defaults, package documentation, verification metadata, and user-facing version labels to 3.1.0.
- Replaced the scattered changelog/release-note files with this unified timeline.

### Shared Windows Graphics Capture transport

- Replaced the independent BossWatcher and GameTimeWatcher GDI screen-acquisition paths with one hidden `PoE2CaptureHost` process using a persistent Windows Graphics Capture session for the PoE2 window.
- BossWatcher and GameTimeWatcher now consume separate shared-memory frame channels from that single capture source rather than each copying the screen independently.
- The WGC session uses a free-threaded Direct3D11 capture frame pool; pause-frame scaling is isolated on a below-normal-priority background worker so it cannot block boss-frame publication.
- Preserved Per-Monitor-V2/client-area geometry: WGC window frames are translated back to the PoE2 client rectangle before boss or pause ROIs are derived.
- Removed `CopyFromScreen`, desktop `GetDC`, and `StretchBlt` screen acquisition from the watcher capture paths. `System.Drawing` remains only for detector/OCR bitmap processing.
- Existing boss identity logic, pause classification, route logic, and `firstMissing` split-time backdating are intentionally unchanged in this transport-only refactor.

### BossWatcher anonymous structural arming / delayed identity OCR

- Decoupled single-boss encounter timing from boss-name OCR. A structurally valid boss UI now arms an anonymous encounter immediately; OCR resolves identity independently instead of deciding whether an encounter exists.
- Added a strong one-frame structural arm path for very short encounters (`redRun >= 0.75` and centered name signal `>= 0.06`). Weaker candidates keep the existing multi-frame confirmation gates.
- Added a bounded 24-frame native boss-ROI ring buffer and best-frame selection for delayed identity recovery. The buffer is frame-driven and independent of Tesseract cadence.
- Added asynchronous post-disappearance OCR on a dedicated Tesseract engine so capture/timing can continue while identity is recovered from retained frames. Buffered identity uses per-frame gold/broad OCR followed by temporal-mask fallback.
- Anonymous disappearance is measured with the learned boss-name spatial template, which does not require knowing the boss identity. Health-fill collapse remains diagnostic only and cannot itself complete an encounter.
- Live OCR remains the primary identity mechanism. If it resolves while the boss is still present, tracking transitions into the existing known-boss path without changing the structurally recorded encounter start.
- If identity resolves only after disappearance, `SEEN` is emitted using the original structural first-seen timestamp and `GONE` uses the original `firstMissing` timestamp; the 5.5 s verification window and delayed OCR therefore do not inflate the recorded fight/split time.
- Structural evidence alone cannot emit a named boss completion. An unresolved anonymous encounter is dropped after the configured identity-resolution timeout rather than guessing a boss ID.

### Timing accuracy and sampling/performance tuning

- Fixed boss split backdating being silently skipped when the watcher confirmation delay exceeded the ASL safety cap. The previous ASL guard accepted at most 5.0 seconds while the normal disappearance confirmation is 5.5 seconds; boss and mixed route scripts now allow a 45-second sanity window, preserving the original `firstMissing` timestamp under supported confirmation settings.
- Raised BossWatcher capture sampling from 20 FPS to 30 FPS to reduce boss-bar disappearance quantization from roughly 50 ms to roughly 33 ms before the verification window begins.
- Raised GameTimeWatcher normal pause sampling from 10 FPS to 15 FPS and fast/menu-transition sampling from 30 FPS to 40 FPS; the menu-input fast window is extended from 1.5 s to 2.0 s to improve rapid exit/re-entry handling.
- Kept pause scaling isolated on the existing below-normal-priority background worker and retained newest-frame-only queuing so increased pause sampling cannot build a stale-frame backlog.
- Optimized the pause bilinear scaler by caching horizontal source-coordinate/weight maps for stable viewport dimensions and unrolling the fixed BGR24 channel loop, reducing repeated per-pixel arithmetic and temporary work in the highest-frequency pause path.
- No additional GPU compute/offload stage was added: WGC/D3D11 already performs capture/cropping on the GPU, while adding a separate compute pipeline for the relatively small detector viewport would add synchronization/transfer complexity without a demonstrated net gain.

### Mixed Area + Boss routes

- Fixed premade **Area + Boss** generation so release packages contain the complete `BossWatcher/BossLists` directory rather than only `support-only.txt`.
- Release validation now requires the runtime boss lists used by Campaign 100%, Campaign Required Bosses, Pinnacle routes, and support-only OCR filtering before a package can be assembled.
- Fixed dynamic/ordered mixed routes failing to split on area transitions even though Game Time had already parsed the same `Client.txt` load completion.
- The Game Time load-completion parser now provides an in-memory area-transition queue to the mixed-route objective state machine. This removes the fragile requirement that two independent `Client.txt` readers both recognize the same line.
- Area transition queue state is cleared on reset so events from an abandoned attempt cannot leak into a restarted run.
- Retained normal `GeneratingLevel`, relaxed internal-area, and `You have entered...` parsing as alternate/early area sources.

### Controller / Mouse + Keyboard profiles

- Added an explicit **Input UI** selector beside Generate / Deploy: **Mouse + Keyboard** or **Controller**.
- The selected mode is persisted into `poe2_run_settings.json` and is now treated as authoritative at generation time; generation validates that the snapshot matches the visible SetupUI choice.
- GameTimeWatcher loads separate controller pause-menu templates/anchors when Controller mode is selected, while Mouse + Keyboard keeps the existing calibration.
- Input events remain acceleration hints rather than the source of truth: ESC is used for Mouse + Keyboard, Controller Start/Menu for Controller, and visual confirmation remains authoritative.
- Added menu-input debounce for controller/Steam Input polling chatter.
- BossWatcher Controller mode uses controller-specific OCR preprocessing calibrated from the controller boss-name presentation while preserving the Mouse + Keyboard OCR profile.

### Windows display scale and high-resolution capture

- BossWatcher and GameTimeWatcher declare **Per-Monitor-V2 DPI awareness** so Windows display scaling does not virtualize the PoE2 client geometry seen by the watcher process.
- Watcher diagnostics now report client dimensions and DPI/scale information so physical-resolution capture can be verified on scaled displays.
- GameTimeWatcher uses a fixed-cost centered 16:9 analysis viewport normalized to **1024x576**, preventing 4K/8K or ultrawide source resolutions from increasing template-matching work or changing detector geometry.
- This keeps pause detection workload consistent across common 1080p, 1440p, 4K, 8K, 21:9, and 32:9 configurations while still using the actual physical game window for capture geometry.
- Provisional pause/run timeout rejection is evaluated after the current visual-analysis opportunity so a slower frame cannot be rejected immediately before that same frame would have confirmed the state.
- BossWatcher retains height-relative, center-anchored boss capture geometry so boss UI scale is derived from client height rather than total ultrawide width.

### Pause and boss detection validation

- Controller-mode pause/unpause was field-tested through the dedicated controller profile and correctly propagated into LiveSplit Game Time.
- Controller-mode BossWatcher was field-tested successfully against **The Bloated Miller**.
- Existing first-completion boss semantics remain unchanged: once a normal boss objective is completed, later events for the same canonical boss ID are ignored unless the user undoes/re-arms that split.

### Build/release hardening carried into 3.1.0

- Fixed Windows PowerShell StrictMode scalar/array `.Count` failures in proper-noun refresh and OCR setup paths.
- Fixed runtime hash-path separator handling that could fail while validating the assembled release.
- Corrected Inno Setup script compatibility issues found during local installer compilation.
- Preserved short temporary release staging paths to reduce Windows MAX_PATH risk.
- Preserved package/runtime SHA-256 verification, setup-validation manifests, and per-run audit artifacts.

### Component identities in this release line

- GameTimeWatcher: `0.5.0-wgc-shared-capture`.
- BossWatcher: `0.4.1-anonymous-structural-arming`.
- Shared CaptureHost: package `3.1.0`, WGC/D3D11 transport.
- Generated/custom mixed route after the area-queue fix: `v1.3.11-loadend-area-queue`.
- Legacy mixed dynamic route area-queue line: `v1.2.4-loadend-area-queue`.

## v3.0.0 Release Candidate — consolidated release summary

Version 3.0.0 promotes the accumulated route, localization, diagnostics, and BossWatcher geometry work from the 2.2.1 development line into a new major release candidate.

### Buildfix8 validation fixes

- SetupUI now treats the visible Input UI selector as authoritative at Generate / Deploy time and verifies the generated `poe2_run_settings.json` snapshot matches it, preventing Controller runs from silently launching watcher profiles as MouseKeyboard after the master-settings reload.
- Mixed area+boss routes now accept current Client.txt area transitions through a relaxed internal-area parser plus the already-tested load-completion parser used by Game Time. This fixes area-entry objectives such as Clearfell Encampment / Clearfell being visible to Game Time but ignored by route progression.
- Existing Controller-specific GameTimeWatcher templates and BossWatcher OCR preprocessing remain isolated from Mouse + Keyboard calibration; this buildfix makes sure the selected Controller profile actually reaches both watchers.

### Major release highlights

- SetupUI and PoE2 game-language selectors are limited to the nine languages currently supported by Path of Exile 2: English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai.
- SetupUI UI text plus authoritative boss/area proper nouns are localized where a verified game-data source is available; unresolved proper nouns remain English rather than being guessed.
- Campaign, Custom Route, Trial of the Sekhemas, Trial of Chaos, Vaal Ruins, and Maps setup policies have been consolidated into the current SetupUI.
- Premade and Custom Route start policies include Riverbank start, first-split-zone entry auto-start, and manual start.
- BossWatcher supports localized OCR and now uses height-relative, center-anchored boss-bar geometry so standard 16:9, ultrawide, and super-ultrawide clients do not require manual resolution configuration.
- Dual-boss OCR lanes are independent wide left/right regions rather than subdivisions of the single-boss name ROI.
- GameTimeWatcher remains the optional manual-pause helper while ASL Game Time removes detected loading-screen time.
- Release packaging now separates verification files from documentation and diagnostics.

### Package layout

- `1 - User Setup` — SetupUI launcher, user settings, and generated `LiveSplit Target`.
- `2 - Support Files` — route data, BossWatcher, GameTimeWatcher, build tools, and installer sources/runtime support.
- `3 - verification files` — package/runtime SHA-256 manifests, setup-validation manifests, per-run audit logs/summaries/checksum manifests, and verification helpers.
- `4-README's_and_Diagnostics` — README translations plus centralized diagnostics; diagnostic PNGs are stored in `Diagnostics\images`.

### Release-candidate note

This is a release candidate and should receive another field-validation pass across campaign, trials, maps, manual pause, localized SetupUI display, and common display aspect ratios before the final v3.0.0 release is tagged.

## Prior project release timeline

## v3.0.0 Release Candidate — multilingual release / maps, trials, diagnostics, and adaptive BossWatcher geometry

### v3.0.0 RC buildfix7 — explicit input UI profiles

- Added an **Input UI** selector beside Generate / Deploy: **Mouse + Keyboard** or **Controller**. The selection is persisted and snapshotted into `poe2_run_settings.json`.
- GameTimeWatcher now loads a controller-specific pause-menu template/anchor profile when Controller is selected and only treats the selected hardware's menu key (ESC or Controller Start) as the acceleration hint.
- Added a 300 ms controller/menu-edge debounce to reject XInput/Steam Input polling chatter without making input authoritative.
- GameTimeWatcher now analyzes a centered fixed 16:9 1024x576-equivalent viewport, so ultrawide clients no longer retain a 2048x576 analysis surface. This stabilizes visual geometry and workload across 1080p/1440p/4K/8K and 21:9/32:9 displays.
- BossWatcher Controller mode uses a bright-luminance OCR mask calibrated from the supplied controller `THE BLOATED MILLER` sample while leaving the tested KBM gold-mask calibration unchanged.
- Added release validation for both KBM and Controller GameTimeWatcher template sets.
- No ASL route/split semantics changed.


- **Critical premade packaging fix:** release builds now include the complete `BossWatcher/BossLists` directory and explicitly validate Campaign 100%, Campaign Any%, Pinnacle, and support-only lists before packaging. This fixes Area + Boss premades failing at generation because `campaign-100.txt` or another target list was missing from the installed runtime.
- **Critical DPI fix:** BossWatcher and GameTimeWatcher now declare Per-Monitor-V2 DPI awareness so Windows display scaling (125%/150%/200%/300%, etc.) does not virtualize PoE2 client geometry before watcher capture.
- **Critical GameTimeWatcher throughput/timing fix:** GameTimeWatcher captures directly into its 576-pixel-high analysis surface regardless of 1080p/1440p/4K/8K source resolution, and provisional timeout rejection now occurs after the current visual verification opportunity rather than immediately before it.
- Promotes the current development line to the 3.0.0 Release Candidate.
- Consolidates supported SetupUI and PoE2 game languages to the nine languages supported by Path of Exile 2.
- Adds authoritative localized boss and area display names with conservative English fallback.
- Adds campaign, Trial, Vaal Ruins, and Maps route-policy/UI improvements completed during the 2.2.1 development cycle.
- Moves Trial boss objectives into the Custom Route content selector to preserve usable space on smaller displays.
- Adds height-relative, center-anchored BossWatcher capture geometry so 16:9, ultrawide, and super-ultrawide clients use consistent boss-UI scaling.
- Adds dedicated package verification and documentation/diagnostics directories.
- Centralizes setup-validation manifests, per-run SHA-256 manifests, audit logs, and run summaries under `3 - verification files` so regenerating `LiveSplit Target` does not delete prior per-run audit files.
- Centralizes diagnostic logs under `4-README's_and_Diagnostics\Diagnostics` and diagnostic PNGs under its `images` subdirectory.

### v2.2.1 dev - authoritative localization / supported-language hotfix
- Localized the Custom Route **Dynamic/unordered boss mode** explanatory sentence in every supported SetupUI language, including its runtime default-count value.
- Expanded the authoritative proper-noun refresh for campaign and Atlas/map area names: when PoE2DB autocomplete lacks an area label, the refresh now reads the localized title from that same canonical PoE2DB area/map page. Missing or synthetic pages still fall back to canonical English rather than being machine-translated.
- SetupUI Language and PoE2 Game Language now share one catalog and expose only the nine current PoE2-supported languages: English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai.
- Removed the previously offered non-PoE2 SetupUI locales from embedded resources and the installer language page; legacy unsupported saved codes normalize safely to English.
- Added complete SetupUI locale packs for Portuguese (Brazil) and Thai so every selectable SetupUI language has a corresponding UI resource.
- Fixed authoritative proper-noun refresh corruption on Windows PowerShell 5.1 by downloading PoE2DB catalog responses as raw bytes and decoding them explicitly as strict UTF-8. This prevents mojibake such as `PrÃ©teur` and corrupted Japanese/Korean/Thai names.
- Expanded the proper-noun refresh to all eight supported non-English languages, including Portuguese (Brazil) and Thai.
- BossWatcher localization is now regenerated from the same canonical English -> localized-entry mapping used by SetupUI instead of remaining a separate sparse hand-maintained database. Missing/ambiguous names are still omitted rather than guessed.
- Corrected the Portuguese bootstrap localization for The Bloated Miller to `O Moleiro Inchado`.
- No ASL timing, route-order, Maps lifecycle, pause-detection, Trials, or Vaal runtime policy was intentionally changed by this localization hotfix.
- Windows/.NET/PowerShell execution was not available in the packaging environment; source/static validation is included and the refresh/build path still requires normal Windows field testing.

### v2.2.1 dev - Maps Game Time policy selection
- Simplified the Maps start/completion wording: **The timer will automatically start when first entering the map. A valid run is from first entry to first exit after the area boss kill.**
- Added a Maps **Game Time policy** selector.
- **PoE2 Map Completion** is the default and preserves the existing behavior: Game Time pauses after a completed-map exit and resumes on the next new map entry.
- **Continuous Game Time** counts all non-loading time after the run starts; only loading-screen exclusion and the configured Manual Pause policy may pause Game Time.
- Continuous mode does not apply between-map Game Time rollback after an unfinished map is replaced by a new seed, and it does not rewind current Game Time when fast-exit boss confirmation arrives after the exit. Split boundaries may still be attributed to the saved map-exit timestamp.
- The generated route now records `@mapGameTimePolicy=completion|continuous`, and `poe2_run_settings.json` records both `MapGameTimePolicy` and the fixed `MapCompletionPolicy`.
- Added the new Maps policy UI strings to all 14 translated SetupUI locale packs.
- No pre-run validity gate was added for runners who enter a map before the timed run; video/rules review remains responsible for validating the required start state.

### v2.2.1 dev - Locale-resistant Client.txt parsing + fast map-exit reconciliation
- Hardened generated ASL Client.txt parsing so critical load/area timing prefers stable structural fields: canonical quoted Area IDs, numeric seeds/levels, and message-site hash families. English phrases remain compatibility fallbacks rather than the sole trigger.
- Kept `You have entered ...` as a legacy display-name fallback only; canonical internal Area IDs remain authoritative whenever available.
- Historical user Client.txt samples show exact message-site hashes can change across PoE2 builds, so the parser now accepts the observed hash families (`2d8e*`, `2caa*`, `4cba*`) instead of one exact build-specific hash.
- Added a guarded map-exit assist: the normal in-map MAP_GONE confirmation remains 5500 ms, but a database-OCR-qualified boss that was already continuously missing for at least 500 ms before a real external map exit may confirm immediately on that exit.
- Map-child transitions cannot use the exit assist, and structural/untrusted boss detections cannot use it.
- When trusted MAP_GONE arrives after a fast provisional exit, Maps policy retroactively finalizes SUCCESS at the already-saved map-exit Game Time/Real Time.
- Added the observed `MapSavanna` internal Area ID to the Savannah / Caedron map entry.
- BossWatcher version: `0.3.6-map-exit-assist`.

### v2.2.1 dev - Map premature-exit safety hotfix
- Fixed a Maps false-completion path where an unknown map could arm BossWatcher via structural fallback during loading, then become boss-qualified before the real boss was encountered.
- Maps now require database-OCR boss identity for MAP_GONE qualification; unknown/special maps fail closed.
- Added current Port / Malgor map mapping and guaranteed BossWatcher debug-log creation.

## v2.2.1 development — Scriptable Auto Splitter load regression hotfix

- Fixed a LiveSplit ASL compile regression introduced by the generated run-audit / Maps policy delegates.
- LiveSplit normalizes the exact token `return;` in an ASL action to `return null;`. That normalization is correct for an ASL action body, but it also affects text inside nested `System.Action<...>` lambdas. A void lambda containing `return;` therefore became `return null;` and failed C# compilation before `startup` could run.
- Changed the three generated void-lambda early returns to `return /* ASL void lambda: keep return void */;`, preserving valid C# void-return semantics while avoiding LiveSplit's exact-token normalization.
- The run-audit layer, Maps lifecycle policy, multilingual BossWatcher/GameTimeWatcher work, localization, and all base ASL templates remain enabled/unchanged otherwise.
- No Windows/.NET compilation was performed for this development package.

### v2.2.1 development — multilingual watcher foundation

- Added a separate PoE2 **game language** setting, independent from SetupUI display language.
- BossWatcher v0.3.4 can select localized OCR models and verified localized boss-name dictionaries while preserving invariant runtime boss IDs.
- Added hashed `poe2_boss_localizations.json` run snapshots.
- GameTimeWatcher v0.4.5 now gives primary weight to pause-menu structure/layout and secondary weight to the paused-state banner; English Resume/Exit text is only low-weight corroboration.
- Build tooling can provision the supported Tesseract language models through BossWatcher `Setup-OCR.ps1`.
- Localization coverage is intentionally conservative in this first runtime-OCR iteration: missing non-English boss names are not guessed or silently treated as English.

## v2.2.1 development test — localized SetupUI / installer language selection

- Simplified the Maps **Map completion rule** explanation to the end-user result: defeat the expected map boss and exit; event/side-area bosses do not count.
- Simplified Maps **Tracked character** help to require an exact Path of Exile 2 character-name match when death tracking is enabled.
- Added SetupUI localization resources and a Settings startup-language selector for English, Spanish (Latin America), Spanish (Spain), French, Standard Mandarin Chinese (Simplified), Cantonese Chinese (Traditional), German, Russian, Japanese, Korean, Bengali, Marathi, Telugu, Tamil, and Italian.
- Saving the language applies it immediately to the open SetupUI and persists it as `SetupUI.DefaultLanguage`.
- Added an Inno Setup **Application Language** page. English is the fresh-install default; upgrades preselect an existing supported SetupUI language.
- Reworked GameTimeWatcher visual thresholds as explicit percentage inputs and clarified which saved screen image each threshold compares against. These are screen-template image matches, not text OCR. Runtime 0.00-1.00 values remain unchanged.
- Localization changes only SetupUI presentation; canonical route/area/boss data and BossWatcher OCR language/detection behavior are unchanged.
- No ASL, BossWatcher, GameTimeWatcher, Maps lifecycle, Trials runtime, or Vaal Ruins runtime policy was changed.

## v2.2.1 development test — Settings UI cleanup

- Removed the **Developer console diagnostics** checkbox from the main SetupUI window. Developer diagnostics can now be enabled only from **Settings**.
- The Settings dialog now explains that developer diagnostics is intended for troubleshooting/log collection: BossWatcher launches with its verbose developer console and GameTimeWatcher launches through its diagnostic wrapper.
- Developer-diagnostics changes apply to the next watcher launch in the current SetupUI session; the main window no longer carries a second potentially conflicting toggle.
- Reworked GameTimeWatcher tuning labels and descriptions so each visual threshold states which on-screen template it is matching.
- GameTimeWatcher visual thresholds are displayed as human-readable percentages (62%, 58%, 40%, 50%, 70% defaults) instead of 0.62-style floating-point values. Stored/runtime values remain unchanged internally.
- Added explanatory text that GameTimeWatcher uses image-template matching rather than OCR, that higher percentages are stricter, and that overly high/low settings can cause missed detections/false positives.
- No BossWatcher, GameTimeWatcher, ASL timing, Maps, Trials, or Vaal Ruins runtime policy was changed by this UI-only cleanup.

## v2.2.1 development test — Vaal Ruins UI / policy iteration

- Renamed the Vaal Ruins draft run-target section to **Completion Criteria** with **Dive Number**, **Royal Architect**, and **Atziri** choices.
- Replaced **Single Temple Dive** with a configurable **Temple Dive** count. Default is 1; SetupUI enforces the hard 1-10 dive range.
- Added Vaal Ruins death-condition tracking choices: **No deaths** (default), **First death**, and **Track all deaths**.
- Added an independent **First death** deathless-run condition. When enabled, Death Condition Tracking is forced and locked to **First death** so the planned rules cannot contradict each other.
- Added a tracked-character field to the Vaal Ruins UI scaffold for future exact Client.txt death matching when death tracking is active.
- Expanded the Vaal Ruins policy preview to show dive count, completion criteria, first-death condition, death tracking, and character requirement.
- Vaal Ruins remains UI/policy scaffolding only in this iteration; Generate / Deploy stays disabled and no Temple runtime state machine is emitted yet.
- Existing Maps child-area handling and the Vaal Ruins Maps exit-boundary policy are unchanged.

## v2.2.1 development test — Maps lifecycle policy v2

- Replaced the earlier Maps candidate/Undo-Split policy with exact Client.txt `Map<name>` + seed lifecycle tracking. Scene names are display-only and no longer define map exit.
- Map entry starts/resumes Game Time; successful boss qualification plus exit commits `SUCCESS` and pauses between-map setup time.
- Premature exits save a provisional exit time while Game Time continues. Same-seed re-entry continues the attempt; a different map seed confirms `FAILED`, backdates that map split to the saved exit, removes intervening setup time, and immediately times the replacement map.
- Added four Maps endpoints: fixed finalized-map count, until first death, manual finish via LiveSplit Start/Split, and selected Pinnacle boss defeat.
- Added three death policies: no tracking (default), end on first death, and tracked dynamic `Death [x]` rows while timing continues. Character Name is required only when death tracking is enabled.
- Character Name validation accepts Unicode letters plus `_` only. Runtime death credit uses exact configured-name comparison against Client.txt `<name> has been slain.` notifications so party-member deaths are ignored.
- Maps lifecycle, seed, success/failure, rollback, optional death, Pinnacle, and manual-finish events are written into the shared SHA-256 run-audit chain.
- BossWatcher `MAP_GONE` now qualifies the active map instead of immediately implying a LiveSplit split; user-facing map completion text and documentation were updated accordingly.
- No maximum portal/attempt count is hard-coded in this iteration.

## v2.2.1 development test — SHA-256 run validation / audit logging

- Added a shared timing-neutral run-audit layer to every SetupUI-generated ASL: Premade, Custom, Trials, Maps, and legacy/static preset deployments.
- Each LiveSplit start creates a unique Run ID and an append-only `poe2_run_<RunId>.log`. Every event is chained to the previous event with SHA-256 so removed, reordered, or edited event lines are detectable.
- Every committed LiveSplit split is recorded with its final committed segment name, Game Time, and Real Time. Dynamic Maps rows therefore immediately produce per-map timing statistics using the map row name that was active when the split committed.
- Run completion/reset/ASL exit writes `poe2_run_<RunId>_summary.txt` plus `poe2_run_<RunId>.sha256`, containing the final event-chain hash and SHA-256 checksums for the run log, summary, and generated setup-validation manifest.
- SetupUI now creates `poe2_setup_validation.sha256` for stable generated ASL/config/rules/support files and `Verify-RunValidation.ps1` to verify both the file checksums and the event hash chain. LiveSplit `.lss` files and mutable watcher/state logs are intentionally excluded from the setup baseline because they legitimately change during use.
- `poe2_run_current.txt` exposes the current Run ID and output paths so BossWatcher/GameTimeWatcher can be correlated with the same run in a later integration pass.
- The run summary explicitly describes this as integrity/audit evidence rather than tamper-proof anti-cheat proof.
- The audit core remains shared by all run types. Maps policy v2 now adds map seed/lifecycle, success/failure, rollback, optional tracked-death, Pinnacle, and manual-finish events to the same chain.

## v2.2.1 Release Candidate — Trial auto-start and SetupUI refinement

- Fixed dedicated Trial generation so Trial runs use the same independent `Client.txt` zone-entry start policy as Premade and Custom runs. Wildcard trial IDs such as `Sanctum_1_*` are now handled by the hardened generated start reader.
- Trial of the Sekhemas start remains targeted at the first active Floor 1 `Sanctum_1_*` area and does not start in the preceding `G2_13` Trial lobby.
- SetupUI now opens at half of the usable monitor width and the full usable monitor height, centered horizontally on the monitor containing the mouse cursor.
- Custom Route order selection uses explicit **Ordered** / **Dynamic / unordered** radio buttons and a concise selected-rule description.
- Removed redundant Timer Start text from the Custom Route panel.
- Corrected the Content-selector note so it points to the Trial content selector below it.
- Shortened the Trial boss objective description to only the relevant selection rules and boss-pool details.
- Updated release, installer, build, manifest, documentation, and SetupUI assembly metadata to v2.2.1. BossWatcher and GameTimeWatcher component versions remain unchanged.

## v2.2.0 Release Candidate — promoted working baseline

- Custom Route now uses explicit **Ordered** and **Dynamic / unordered** radio buttons instead of a single ordered-route checkbox. The selected route rule is summarized at the bottom of the Custom route panel, and the redundant Timer Start explanation above the objective list was removed.
- Promoted the validated v2.1.3 development line to the v2.2.0 release series after successful field testing of the configurable zone-entry start path.
- SetupUI retains three explicit timer-start policies: Manual Start, Riverbank Start using the Wounded Man final opening line, and First Split Zone Entry Auto Start.
- Fixed First Split Zone Entry Auto Start so the configured `Client.txt` area entry starts LiveSplit both on a normal zone transition and when loading directly into the selected starting zone while the autosplitter is active.
- Scriptable Auto Splitter settings now identify the generated start policy/area instead of leaving Riverbank Start displayed as the selected rule for non-Riverbank runs.
- Includes the v2.1.3 shared-ASL compile regression fixes, Maps structural boss runtime, compact Premade generator, expanded Custom route/multiboss support, and Trials runtime/catalog work documented below.
- Retains the v2.1.2/v2.1.1 Game Time and optional manual-pause hardening, including transient manual-pause state read grace and provisional pause/resume accounting.
- Autosplitter, BossWatcher, and GameTimeWatcher runtime logic remains unchanged from the working `v2.1.3-dev-zone-entry-hotfix` test package; the additional v2.2.0 RC change is limited to the Custom Route SetupUI presentation described above.

## v2.1.3 development hotfix — configurable zone-entry auto-start

- Fixed SetupUI **First Split Zone Entry Auto Start** for generated premade/custom runs and other `@start=`-driven ASLs.
- Non-Riverbank automatic starts now use stable `Client.txt` substrings for both `Generating level ... area "AREA_ID"` and `You have entered AREA NAME.` events instead of depending only on the stricter legacy area-line regex.
- The selected start zone is recognized when the event is produced by a normal zone transition or by loading into the selected area while the autosplitter is active.
- Ordered successor-entry routes arm the selected start-area objective before LiveSplit starts, preserving the existing first-segment completion rule.
- Start-area IDs ending in `*` are supported by prefix matching for generated trial-floor style area IDs.
- Runtime-supported ASLs now label the Scriptable Auto Splitter start checkbox from the generated `@start=` rule: Manual Start, Riverbank Start, or First Split Zone Entry Auto Start with the selected area name. This prevents a non-Riverbank setup such as **The Glade** from appearing as a checked Riverbank start.
- SetupUI source was also standardized so future rebuilt launchers use the independent generated `Client.txt` start reader for premade, custom, and preset routes rather than maintaining separate start-detection paths.

## v2.1.3 development — LiveSplit ASL load/settings regression hotfix

- Compared the current Maps structural-boss package against the last known-good `chaos-dynamic-subset` package. The 13 mode-specific/static ASLs are unchanged; the shared `14-Custom/PoE2-Mixed.asl` is the runtime source used by the newer generated Premade, Custom, Trials, and Maps deployments.
- Fixed C# local-declaration-space collisions in the shared mixed ASL that can prevent LiveSplit from compiling the script before `startup` runs, leaving Scriptable Auto Splitter custom settings unpopulated.
- Renamed the map-event `hasFirstMissing` / `firstMissing` parsing locals so they no longer collide with the normal identity-boss event locals in the containing event-processing scope.
- Renamed the successor-area queue/direct-entry `key` locals and repeated-area helper locals so nested and enclosing declaration spaces are unique.
- ASL identity updated to `v1.3.7 LiveSplit compile-regression hotfix` / `v1.3.7-asl-compile-hotfix`.
- No route policy, Maps structural detection, Pinnacle OCR behavior, Trials policy, BossWatcher, or GameTimeWatcher behavior was changed by this hotfix.

## v2.1.3 development — Maps structural boss runtime

- Promoted the Maps tab from placeholder `mapdiag` objectives to functional `mapboss|1..N` objectives for 1–100 Dynamic/unordered ordinary-map boss completions.
- Added ASL→BossWatcher context handoff through `poe2_boss_context.txt`: `map` for ordinary map candidates, `identity` for normal OCR contexts, and `off` after an ordinary-map split.
- Added BossWatcher v0.3.1 map-context runtime. Ordinary map mode bypasses Tesseract, name matching, and the boss catalog; structural boss-UI appearance arms the encounter and verified disappearance emits `MAP_GONE`.
- Generic map tracking is identity-free: it uses health-bar red-run structure plus the gold boss-UI/name band only. No boss-name OCR or glyph/template matching is required. Dual boss UI is treated as one ordinary map objective and completes only when the complete boss UI disappears.
- Maps ASL renames the active row at entry to `Map Level X - Boss #Y`; the ordinary boss identity never changes the row.
- Added `MAP_SEEN`, `MAP_GONE`, `MAP_BOSS_SPLIT_SIGNAL`, `BOSS_CONTEXT_WRITE`, and map-context diagnostics. `MAP_GONE` retains first-missing backdating.
- Added failed-attempt policy: a disappearance may split even if the runner died/left; LiveSplit Undo Split removes the completed map objective and re-arms structural detection for the retry.
- After a successful/accepted map split, context switches to `off` until the next area transition, preventing extra boss bars in the same map from consuming another map slot or entering the OCR path.
- Optional Pinnacle objectives remain identity/OCR based. Unknown special/Pinnacle endgame area IDs may still be classified as `unconfirmed-map-or-special` until diagnostics establish explicit exclusions.
- Map auto-start occurs on entry to the first qualifying candidate map.
- Random map-objective completion remains disabled/non-functional. Unique modifier count remains diagnostic only.

### v2.1.2 release-candidate administrative cleanup — SetupUI reminders
- Removed the redundant post-generation success/instructions popup.
- Preserved the target-directory deletion confirmation before replacing a non-empty `LiveSplit Target`.
- Added a persistent SetupUI reminder to attach the generated `.asl` to LiveSplit's Scriptable Auto Splitter component.
- Added a persistent reminder that LiveSplit must be set to **Game Time** for loading screens and optional manual-pause time to be excluded from the displayed run time; Real Time continues counting those periods.
- Normalized final release metadata/default build version from stale `2.1.0` / `2.1.1-dev` values to `2.1.2` so `Build-Release.ps1 -Version 2.1.2` passes its manifest-version check.

### v2.1.2 release-candidate hotfix - compact Windows paths
- Shortened the portable archive root to `PoE2AS-v2.1.2-RC`.
- Condensed all 14 mode folders, BossWatcher, GameTimeWatcher, installer, ASL, LSS, and helper-script filenames.
- Updated `ui-manifest.json`, build scripts, workflow artifact names, installer references, documentation, and generator output filenames to the compact paths.
- Removed generated .NET `bin` / `obj` trees and stale diagnostic/generated LiveSplit output from the release archive.
- Preserved the four SetupUI binary-compatible runtime anchors: `1 - User Setup`, `2 - Support Files`, `Setup UI [Configuration]`, and `LiveSplit Target`.
- `Build-Release.ps1` now discovers compact mode folders with the `NN-Name` pattern and produces `PoE2AS-vX.Y.Z` release artifacts.


### v2.1.2 release-candidate hotfix - manual-pause state read-race grace
- Hardened the optional GameTimeWatcher state-file reader in all 14 source ASLs. The release `LiveSplit Target` is intentionally empty until SetupUI generates the selected setup.
- A missing, locked, or malformed `poe2_manual_pause_state.txt` read now retains the last successfully parsed fresh watcher state for up to 500 ms instead of immediately treating one failed 25 ms poll as `RUNNING`.
- The grace never extends a stale heartbeat: the cached heartbeat must still be inside the existing 2-second freshness window.
- A successfully read stale heartbeat still fails open to `RUNNING` immediately.
- Added `GT_MANUAL_READ_GRACE`, `GT_MANUAL_READ_RECOVERED`, and `GT_MANUAL_READ_GRACE_EXPIRED` diagnostics.
- `GT_READY` now reports `readGraceMs=500` so deployed patched ASLs are easy to identify.
- GameTimeWatcher v0.4.3 templates, thresholds, state writer, provisional timing protocol, load timing, BossWatcher, and SetupUI start-policy behavior are unchanged.

### v2.1.1 dev - GameTimeWatcher v0.4.3 / manual-pause accounting
- The GameTimeWatcher `PENDING_PAUSE` / `PENDING_RUN` protocol is now fully accounted for by all 14 ASLs.
- Accepted ESC pauses can rewind Game Time to the original input timestamp while still paused.
- Rejected ESC pause candidates refund the provisional hold when ESC only closes another in-game interface.
- Rejected provisional resumes re-remove falsely counted time.
- Manual-pause state polling reduced from 50 ms to 25 ms.
- Added `manualPauseProtocol=v2.1-provisional-accounting` to Game Time debug startup output so stale ASLs are obvious.
- Setup UI instructions now explicitly require redeploying/repointing the ASL when the manual-pause protocol changes.

### v2.1.1 development - GameTimeWatcher v0.4.2 provisional timestamp timing

- Optional manual-pause timing now uses protocol-v2 provisional `PENDING_PAUSE` / `PENDING_RUN` states.
- ESC/controller Start writes its timestamp immediately; all 14 ASLs react provisionally on their next update and compensate the interval if visual verification later rejects the candidate.
- Mouse-driven Resume/Options/Challenges publishes a provisional Running state on the first visual gameplay frame.
- GameTimeWatcher publishes state before diagnostic PNG encoding so developer diagnostics cannot delay the timer handoff.
- No LiveSplit Pause hotkey is rebound or injected.
- Wounded Man campaign start, Client.txt load timing, exploration/boss logic, and BossWatcher are unchanged.

### v2.1.1 development hotfix — GameTimeWatcher v0.3.7 timing stability

- Confirmed the Riverbank Wounded-Man start gate and Client.txt loading-screen removal remain unchanged.
- Diagnosed the v0.3.6 visual loop running at roughly one analyzed frame every ~2 seconds instead of the configured 10 FPS.
- The slow detector caused the ASL's 2-second heartbeat freshness check to repeatedly expire while the watcher was still alive, producing small bursts of Game Time during a pause.
- The same throughput problem stretched 2-frame pause / 3-frame running confirmation into multi-second MTX entry/exit delays.
- GameTimeWatcher now precomputes scaled pause templates once at startup and searches only narrow centered vertical bands.
- Pause-menu priority remains `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE`, with separate thresholds 0.58 / 0.40 / 0.50.
- The state heartbeat now runs on an independent background thread, so image-analysis latency cannot make the heartbeat stale.
- Added `analyzeMs` to diagnostics and rate-limited near-threshold center-column screenshots for exact capture debugging.
- MTX template matching itself is unchanged because the live shop score remained strong (~0.97).

### v2.1.1 development hotfix — GameTimeWatcher v0.3.5 pause-menu button detector

- Confirmed Riverbank start gate and Client.txt load-removed Game Time remain unchanged.
- Replaced GameTimeWatcher's `GAME PAUSED` banner detector with centered `RESUME GAME` detection plus `EXIT PATH OF EXILE` fallback.
- Pause-menu matching now searches only the canonicalized central column, making normal display-width/aspect-ratio differences irrelevant to the horizontal search.
- Added separate dev-console/diagnostic scores for Resume, Exit, aggregate pause, and MTX detection.
- MTX Shop detection remains unchanged.

### v2.1.1 development hotfix — GameTimeWatcher v0.3.4 literal-path launcher

- Fixed the external GameTimeWatcher diagnostic launcher failing before the watcher started when the support folder name contained literal square brackets.
- The launcher now starts the watcher with `.NET System.Diagnostics.ProcessStartInfo` instead of PowerShell `Start-Process`.
- Startup exceptions remain visible in the diagnostic console and `--wait-on-error` is enabled.

### v2.1.1 development hotfix — GameTimeWatcher v0.3.3 startup diagnostics

- Preserved the Wounded Man start gate introduced in v0.2.17-startgate.
- GameTimeWatcher now keeps its console open on fatal startup errors when launched from the Setup UI and the developer diagnostic launcher uses PowerShell `-NoExit`.
- Fatal startup exceptions are also persisted beside the LiveSplit target state file as `poe2_gametimewatcher_startup_error.log`.
- GameTimeWatcher build output now explicitly copies and validates `config.json` and both visual template files in `publish`, avoiding silent runtime data omissions.
- Config/template resolution now reports the exact missing path if startup data cannot be found.
- No BossWatcher, route, boss catalog, or split semantics were changed.

### v2.1.1 development hotfix — start-gate verification + GameTimeWatcher v0.3.2

- Ordered campaign ASL now identifies itself as `v0.2.17-startgate` in the debug READY line. A log that still says `v0.2.16` or starts directly on `AREA G1_1` is running a stale ASL path.
- Setup UI now explicitly warns that LiveSplit keeps the previous Scriptable Auto Splitter file path when a new dev ZIP is extracted to a different folder.
- GameTimeWatcher v0.3.2 replaces the false-positive-prone dark-pixel matcher with normalized-correlation matching and a tight `GAME PAUSED` banner template.
- GameTimeWatcher frame matching now uses contiguous grayscale buffers rather than repeated `GetPixel` calls, reducing detector stalls that could make its heartbeat appear stale.

### v2.1.0 development patch — Riverbank start policy and GameTimeWatcher diagnostics

#### Campaign start policy
- Campaign autosplitters whose start is `G1_1` / The Riverbank now **arm** on Riverbank entry instead of starting LiveSplit immediately.
- The actual timer start is the newly appended Client.txt line `Wounded Man: Reach... Clearfell... Find the Miller...`.
- The new-character wake-up/setup period, movement to the first NPC, and the first Wounded Man interaction are intentionally untimed so runners have a brief deterministic setup window before the attempt.
- Non-Riverbank configurable/custom start areas retain their existing area-entry start behavior.
- Pinnacle Boss Rush modes 10-11 remain manual-start.
- Riverbank remains an exploration objective and existing successor-entry completion semantics are unchanged.

#### GameTimeWatcher v0.3.1 crash diagnostics / resource hardening
- Added an external `Run-Diagnostic.ps1` / `Run-Diagnostic.cmd` watchdog that launches GameTimeWatcher, captures stdout/stderr, samples working/private/virtual memory, handle count, thread count, CPU time and responsiveness, records the watcher exit code, and queries Windows Application crash events.
- Added a best-effort internal `watcher-internal.log` with startup/config/template/state transitions, managed exceptions, per-second resource/GC status, and state-file/log I/O errors.
- State-file/log write failures are now diagnostic events rather than reasons for GameTimeWatcher to terminate.
- Fixed a plausible long-run resource leak by disposing every `Process` wrapper returned by repeated `Process.GetProcessesByName` scans.
- Throttled game-window/process discovery to 250 ms instead of enumerating processes on every ~10 ms main-loop iteration.
- Setup UI **Developer console diagnostics** now launches the external GameTimeWatcher watchdog when the optional manual-pause helper is started.
- Installed/release runtimes now include the external diagnostic launcher beside the optional GameTimeWatcher runtime.

### v2.1.0

#### Load-removed Game Time
- Added Game Time support to all 14 autosplitter engines.
- Each ASL tails Path of Exile 2 `Client.txt` with an independent reader so Game Time processing does not consume or interfere with route/boss progression events.
- `Got Instance Details` begins the live loading pause.
- PoE2's `[LOADING SCREEN] (...) Duration = X seconds` value is treated as the authoritative completed loading-screen duration.
- After each load, the ASL corrects the small difference between observed live-pause time and the duration reported by PoE2.
- The opening Riverbank load occurs before the new Wounded Man start gate and is discarded from the attempt when LiveSplit starts; Game Time cannot become negative from pre-start load bookkeeping.
- Boss intros/outros, dialogue, and scripted story events remain timed; they are not treated as loads or free inventory-management time.
- Added an enabled-by-default ASL setting: `Game Time: remove Client.txt-reported loading screens`.

#### Optional manual-pause removal
- Re-scoped GameTimeWatcher to v0.3.1 as an optional manual-pause helper with crash diagnostics/resource hardening.
- GameTimeWatcher is no longer involved in load, boss-bar, voice-line, cutscene, movement, keyboard, or controller gameplay-response detection.
- The helper now recognizes only the actual `GAME PAUSED` menu and Microtransaction Shop using external screen capture/template matching.
- Options and Challenges/Achievements intentionally remain timed because PoE2 unpauses while those interfaces are open.
- Added a heartbeat-based `poe2_manual_pause_state.txt`; stale/missing state fails open so Game Time cannot remain stuck paused if the helper exits.
- Setup UI now exposes `Pause LiveSplit Game Time while PoE2 is manually paused` and a `Start GameTimeWatcher` button.
- Manual-pause removal is disabled by default and is patched into the deployed ASL only when selected during setup.

#### Packaging
- Release builds now compile and stage the self-contained optional GameTimeWatcher runtime alongside Setup UI and BossWatcher.
- Updated installer/release workflow defaults to v2.1.0.
- Preserved the two-root-folder runtime, user-owned LiveSplit layout policy, zero `.lsl` generation, short-path release staging, and existing BossWatcher behavior.

### v2.0.2

#### Campaign Boss Rush Riverbank auto-start
- Campaign Boss Rush-only modes now auto-start LiveSplit when `Client.txt` reports entry into `G1_1` / The Riverbank.
- Applied to Campaign 100% Dynamic, Campaign 100% Predefined, Campaign Any% Dynamic, and Campaign Required Bosses Only Predefined.
- The start detector uses the same Riverbank area-entry signal used by campaign exploration; it does **not** use area departure as a completion/start signal.
- BossWatcher remains responsible only for boss encounter/defeat events and is not used to start the timer.
- The `autoStart` ASL setting defaults to enabled and can be disabled for manual start.
- If `Client.txt` cannot be opened, Boss Rush remains usable with manual timer start and the problem is written to the bridge debug log.
- Pinnacle Boss Rush Dynamic and Predefined intentionally remain manual-start.
- No boss whitelist, boss split, OCR, firstMissing backdating, exploration route, or combined-mode behavior changed.

### v2.0.1

#### Release build path-length hotfix
- Fixed `Build-Release.ps1` failing during cleanup when deeply nested staged runtime paths exceeded the classic Windows 260-character `MAX_PATH` boundary.
- Expanded portable-runtime staging now uses a short system temporary path instead of nesting the full runtime beneath the repository checkout path.
- Added long-path-safe fallback cleanup for stale `artifacts` directories left by an interrupted/failed build.
- The final installer, portable ZIP, and SHA-256 files are still written to the repository `artifacts` directory as before.

#### Riverbank exploration correction
- Added The Riverbank (`G1_1`) to Campaign 100% and Campaign Any% exploration accounting.
- Ordered campaign routes now use **successor-entry completion** when the route begins with Riverbank: entering Riverbank starts the timer, and only entering the next configured route area completes the active segment.
- Returning to town, visiting a hideout, or revisiting an earlier area does not complete the current ordered segment unless that destination is the exact configured successor.
- Campaign 100% ordered splits now contain 99 area rows; Campaign Any% ordered contains 78.
- Flexible/checklist exploration modes count Riverbank as implicitly completed by the auto-start event without adding a zero-time LiveSplit split.
- Combined Campaign 100% and Any% modes use successor-entry completion for their exploration objectives while boss objectives remain dynamic. Their totals are now 166 and 118 objectives respectively.
- Custom mixed routes retain entry-based area objectives unless `@areaCompletion=successor` is explicitly present.

### v2.0.0

#### Installer and release distribution
- Replaced the source-first normal-user setup with a Windows installer workflow.
- Normal users now install `PoE2RouteAutoSplitter-v2.0.0-Setup.exe` and do not need PowerShell, the .NET SDK, OCR setup scripts, or local compilation.
- Added an Inno Setup 6 installer definition under `Installer`.
- The installer deploys to the current user's Local AppData by default and preserves the established `1 - User Setup` / `2 - Support Files` runtime structure.
- `LiveSplit Target` is intentionally preserved across installer upgrades/uninstalls.
- Setup UI and BossWatcher release builds are now self-contained Windows applications.
- The installer includes the Microsoft Visual C++ x64 redistributable used by BossWatcher's OCR/native dependencies.
- Added a portable self-contained ZIP as an alternative to the installer.

#### Git/GitHub release pipeline
- Added repository `.gitignore` rules for compiled executables, `bin`/`obj`/`publish`, downloaded OCR data, release staging, and installer output.
- Added `Build-Release.ps1` to build both applications, assemble a runtime-only package, create the portable ZIP, compile the installer, and generate SHA-256 release checksums.
- Fixed Inno Setup discovery so local release builds recognize both machine-wide installs and WinGet/current-user installs under `%LOCALAPPDATA%\Programs\Inno Setup 6`, with registry-based fallback for custom install locations.
- Local release builds copy the installer into `1 - User Setup` for developer convenience while keeping it ignored by Git.
- Added `.github/workflows/build-release.yml`. Version tags such as `v2.0.0` build the Windows release and publish the installer, portable ZIP, and checksum file as GitHub Release assets.
- Manual workflow dispatch can build the same artifacts without creating a tagged release.

#### Runtime behavior
- No `.lsl` generation was reintroduced. Users continue to maintain their own LiveSplit layout and manually point its Scriptable Auto Splitter component at the deployed `.asl`.
- No boss lists, route definitions, exploration logic, BossWatcher detection logic, or autosplitter split semantics were changed for v2.0.0.

### v1.4.0

#### Two-folder user/support package layout
- Restructured the release root to contain only `1 - User Setup` and `2 - Support Files`.
- `1 - User Setup` is the normal user-facing folder and contains `PoE2RouteSetup.exe` after build plus the dedicated `LiveSplit Target` directory.
- Moved all mode directories, BossWatcher files, Setup UI source/build files, documentation, and other support content under `2 - Support Files`.
- Setup UI `Build.ps1` now publishes as a single-file executable and copies `PoE2RouteSetup.exe` into `1 - User Setup`.
- The Setup UI now uses the fixed `1 - User Setup\LiveSplit Target` directory; the arbitrary target-folder Browse control was removed.
- Removed the optional starter `.lsl` layout and its UI button entirely. No `.lsl` files are included, generated, copied, or modified.
- Updated package discovery so the launcher in Subfolder 1 resolves catalogs, mode sources, and BossWatcher from Subfolder 2.
- No boss lists, route definitions, BossWatcher detection logic, or autosplitter engine behavior changed.

### v1.3.3

#### LiveSplit layout safety fix
- Removed all automatic `.lsl` generation from the Setup UI after generated layouts were found capable of causing LiveSplit startup/loading failures.
- Premade and custom deployment now output only `.lss`, `.asl`, runtime route/config files, and setup information.
- `SETUP_INFO.txt` now records the exact deployed ASL path and tells users to manually point a Scriptable Auto Splitter component at it.
- Added a **Starter Layout Folder** button to the Setup UI.
- Added `Optional LiveSplit Layout [User Editable]\Path of Exile 2 - Starter Layout.lsl`, based on a manually created LiveSplit layout.
- Removed the starter layout's machine-specific Scriptable Auto Splitter component/ASL path and reset its screen position to `(50, 50)` for portability.
- Existing user layouts are never modified by the Setup UI.
- No boss, area, route, detector, or BossWatcher behavior changed.

### v1.3.2

#### PowerShell bracket-path hotfix
- Fixed `BossWatcher\Build.ps1` falsely reporting that `tessdata\eng.traineddata` was missing when the file existed.
- `Test-Path` now uses `-LiteralPath`, preventing `[Boss Rush Detection]` from being interpreted as a wildcard character class.
- BossWatcher build-time `Copy-Item` sources now use `-LiteralPath` for the same reason.
- Applied the equivalent literal-path check to `01-Ordered\tools\Gen-Ordered.ps1` for absolute output directories containing brackets.
- No route, boss catalog, detector, LiveSplit, Setup UI, or console behavior changes.

### v1.3.1

#### Setup UI startup hotfix
- Fixed a startup exception in the Custom Route tab caused by applying `SplitContainer` minimum-panel sizes and splitter position while the control still had its small default construction size.
- The custom split pane now receives a valid working size before `Panel1MinSize`, `Panel2MinSize`, and `SplitterDistance` are applied.
- Added user-visible fatal-error handling for startup and WinForms UI-thread exceptions instead of allowing a `WinExe` startup failure to appear as a silent exit.
- Added `PoE2RouteSetup-crash.log` reporting beside the executable when writable, with the system temporary directory as a fallback.
- No route definitions, boss lists, exploration logic, BossWatcher detection behavior, or LiveSplit generation semantics changed in this hotfix.

### v1.3.0

#### Setup UI
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

#### BossWatcher v0.3.0 console
- Normal user mode no longer prints the per-frame detector status line.
- User output now reports timestamped encounter and defeat events.
- Defeat output includes fight duration in seconds from the recorded `SEEN` encounter timestamp to the boss disappearance `firstMissing` timestamp.
- Added `--dev-console` / `--dev` to retain the previous verbose diagnostic display.
- Added `--event-file <path>` so launchers can explicitly choose the BossWatcher event log location.
- `RETURNED` events and diagnostic details remain in log files but are not printed in normal user mode.

#### Build / packaging
- Added `Build-Tools.ps1` to build both the Setup UI and BossWatcher.
- Updated BossWatcher `Run-Source.ps1` with `-DevConsole` and `-EventFile` arguments.

### v1.2.0

#### Boss-list corrections
- Added Beira of the Rotten Pack to the supported boss catalog and Campaign 100% list.
- Removed Beira from the unstable miniboss exclusion list.
- Removed The Rotten Druid and Diamora, Song of Death from the required-boss v0.5 list.
- Renamed Campaign Any v0.5 - Predefined to Campaign Required Bosses Only v0.5 - Predefined.
- Campaign 100% now has 67 boss targets; required-boss v0.5 now has 40.

#### Package integration
- Merged v1.1.0 Exploration modes and BossRush v0.2.0 into one package.
- Added explicit `[Exploration]`, `[Boss Rush]`, and `[Exploration + Boss Rush]` directory labels.
- Added Campaign 100% mixed mode (165 objectives).
- Added Campaign Any% v0.5 Dynamic mixed mode (117 objectives).
- Added Custom Route mixed mode with ordered/unordered `area|...` and `boss|...` objectives plus a layout generator.
- Preserved BossRush Real Time backdating for boss objectives in mixed modes.

### v1.1.0
- Exploration release baseline imported unchanged except for directory naming.

## BossWatcher component timeline

#### 0.3.8 - Per-monitor DPI-aware capture geometry

- Declares Per-Monitor-V2 DPI awareness so Windows display scaling no longer virtualizes PoE2 client dimensions before BossWatcher computes its height-relative capture rectangle.
- A native 3840x2160 PoE2 client should therefore remain 3840x2160 to BossWatcher even when Windows desktop scaling is 300%.
- Existing height-relative boss capture, OCR masks, thresholds, and encounter logic are unchanged.

#### 0.3.7 - Height-relative boss UI geometry

- BossWatcher now derives the horizontal boss capture width from the PoE2 **client height** and keeps it centered on the game client. This preserves comparable boss-UI geometry across 16:9, ultrawide, and super-ultrawide clients instead of stretching the OCR region with total client width.
- Dual-boss OCR lanes are now independent wide left/right regions split at the capture midpoint rather than narrow children of the single-boss name ROI. This prevents long dual-boss names such as Hadi of the Flaming River / Rafiq of the Frozen Spring from being truncated before OCR.
- Development diagnostics now log detected client resolution/aspect ratio, boss-capture dimensions, and the left/right OCR rectangles whenever the game client size changes.
- Capture continues to use the PoE2 client rectangle only; window title bars, borders, taskbar, and unrelated desktop pixels are outside the captured ROI.

#### 0.3.6-map-exit-assist (development)
- Preserved the conservative 5500 ms in-map disappearance confirmation for ordinary deterministic map bosses.
- Added a guarded external-exit assist: a boss already armed by `database-ocr` and continuously missing for at least 500 ms can emit trusted `MAP_GONE` when ASL context confirms a real external map exit.
- The assist cannot arm a boss, cannot use structural fallback, and is explicitly suppressed for recognized map-child transitions.
- `MAP_GONE` now records `confirmation=timer` or `confirmation=exit-assist`.
- Added `MapExitAssistMinMissingMs` (default 500 ms) to BossWatcher config and generated run-settings snapshots.
- Added the observed `MapSavanna` -> Savannah mapping so Caedron, the Hyena Lord can be resolved by the deterministic map database.
- Client.txt parsing changes live in the generated ASLs; BossWatcher consumes only the resulting canonical context and remains independent of localized area display names.

#### 0.3.5-map-identity-safe (development)
- Map mode now fails closed when the current map is missing from the map-boss database or uses a special completion type. Structural fallback no longer emits MAP_SEEN/MAP_GONE for unknown/special maps.
- Added deterministic `MapPort` -> `Malgor, the Nautilord` mapping from current PoE2DB data.
- BossWatcher now creates `poe2_boss_watcher_debug.log` immediately when the event writer starts and prints its path in developer console mode.

### v0.3.4-localized-ocr — development

- Added a separate PoE2 game-language setting for boss-name OCR.
- BossWatcher selects the matching Tesseract model for English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, or Thai.
- Added `boss-localizations.json`, keyed by invariant boss ID. Runtime OCR resolves a verified localized name back to the same canonical boss ID.
- Added `--localization-db` and generated-run localization database snapshots with SHA-256 validation metadata.
- Non-English missing-name coverage is conservative: no guessed or silent English-name fallback; deterministic maps log `MAP_LOCALIZATION_UNAVAILABLE` and do not arm from an unrelated boss.
- `Setup-OCR.ps1` can install one language or all supported language models; build/release scripts verify the complete model set.
- Existing 5.5-second disappearance policy and structural tracking remain unchanged.

### Changelog

#### 0.3.3 - 5.5-second global development grace + deterministic map-boss database

- Set both user-facing disappearance defaults to **5500 ms**:
  - `BossWatcher.GoneConfirmMs=5500`
  - `BossWatcher.MapGoneConfirmMs=5500`
- Preserved first-missing backdating: the longer confirmation window delays acceptance only and does not add 5.5 seconds to recorded completion time.
- Added `map-bosses.json`, a versioned map -> expected completion-boss database seeded from the current Path of Exile 2 Wiki map list.
- Deterministic Maps mode now uses a narrow OCR identity gate before arming a structural map-boss encounter.
- Non-matching Unique/event bosses no longer qualify a deterministic map. Known event identities are logged as `MAP_EVENT_BOSS_IGNORED`; other non-matches are logged as `MAP_UNEXPECTED_BOSS_IGNORED`.
- Added initial Delirium event-boss exclusions for Omniphobia, Fear Manifest and Kosis, the Revelation.
- Unknown and special/random-completion map entries retain structural fallback during development and are explicitly logged.
- `MAP_SEEN` / `MAP_GONE` now include the matched map-boss identity and detector source when database OCR is used.
- SetupUI records the map-boss database version and SHA-256 inside each generated `poe2_run_settings.json` audit snapshot.
- `Build.ps1` now copies `map-bosses.json` into the BossWatcher publish directory.
- Identity dual-lane resolver windows remain separately calibrated pending a dedicated per-lane long-grace refactor.

#### 0.3.2 - Configurable GONE confirmation grace

- Raised the default identity/single-boss disappearance confirmation grace from 350 ms to 8000 ms.
- The original `firstMissing` timestamp remains authoritative, so confirmation delay does not inflate recorded completion time.
- Added `--settings <path>` support for the generated shared `poe2_run_settings.json` snapshot.
- Added separate user-facing `BossWatcher.MapGoneConfirmMs` for structural Maps mode (default 700 ms). It is intentionally independent because map qualification must occur before exit.
- Identity dual-lane resolver windows remain separately calibrated at 350/700 ms.
- Valid shared-setting ranges are GoneConfirmMs=500-30000 ms and MapGoneConfirmMs=100-30000 ms; invalid/missing overlays fall back to validated `config.json`.
- Pending missing diagnostics now include the active confirmation window.

#### 0.3.1 - Map context / structural ordinary-map bosses

- Added `--context-file <path>` and ASL-controlled `identity`, `map`, and `off` detection modes.
- `map` mode bypasses OCR, name matching, and the boss catalog. It detects a structurally valid boss UI and emits `MAP_SEEN` / `MAP_GONE` on verified appearance/disappearance.
- Structural map tracking uses boss health-bar red-run evidence plus the gold boss-UI band; it does not read or template-match boss-name glyphs.
- Dual map-boss UI is one map objective and completes only when the entire dual/recentered boss UI is gone.
- `identity` mode preserves the existing campaign/trial/Pinnacle OCR detector unchanged.
- `off` mode disables boss tracking after an accepted map completion until the ASL changes context.
- Context-file transient/missing reads preserve the last good context rather than briefly flipping detector modes.

#### 0.3.1 data/catalog update - optional trial bosses

- Expanded the external `bosses.txt` OCR identity catalog from 78 to 85 without changing the BossWatcher detector executable.
- Added Rattlecage, Hadi, Rafiq, Ashar, Uxmal, Chetza, and Bahlak.
- Re-grouped the already-supported Zarokh and The Trialmaster identities under their trial sections while retaining their IDs and existing pinnacle target references.
- Existing Campaign 100%, Required Bosses Only, and Pinnacle target lists are unchanged; trial identities are opt-in through Custom Routes or applicable campaign/Act premades. Pinnacle retains Zarokh and Trialmaster as category targets.

#### 0.3.1 - Build path hotfix

- Fixed `Build.ps1` OCR-data validation when the package is located under the bundled `BossWatcher` directory.
- `Test-Path` and build-time source copies now use `-LiteralPath`, preventing PowerShell from interpreting square brackets as wildcard syntax.
- No detector, OCR, timing, event, or console behavior changes from 0.3.0.

#### 0.3.0 - User console and launcher integration

##### User console
- Removed the per-frame detector status output from normal launches.
- Normal output now shows timestamped watcher start/stop, boss encounter, and boss defeat events.
- Boss defeat lines include fight duration in seconds.
- Fight duration is measured from the recorded `SEEN` encounter timestamp to `firstMissing`, excluding the later disappearance-confirmation delay.
- `RETURNED` events and raw detector/OCR data remain log-only in normal mode.

##### Developer console
- Added `--dev-console` and alias `--dev` to restore verbose frame-by-frame diagnostics.
- Developer mode retains [S] diagnostic capture and [R] tracking reset controls.

##### Event path override
- Added `--event-file <path>` so a launcher can explicitly choose the event log location.
- Updated `Run-Source.ps1` with `-DevConsole` and `-EventFile` parameters.

##### Detector / catalog
- No intentional detector-algorithm change from the v0.2.0 package baseline.
- Retains 67 Campaign 100% targets, 40 required-boss v0.5 targets, 10 Pinnacle targets, and The Plagueling as support-only OCR identity.

#### 0.2.0 - BossRush content expansion

##### Boss catalog

- Expanded `bosses.txt` from the v0.1.14 validation set to **78 OCR identities**.
- Added the complete supported Acts 1-4 + three-interlude campaign boss catalog for the v0.5 baseline.
- Added the current **10-boss v0.5 pinnacle roster**.
- Split ambiguous Jamanra identity into `jamanra_risen_king` and `jamanra_abomination`.
- Preserved existing live-tested IDs such as `scourge_of_the_sky`, `iktab_the_deathlord`, and `ekbab_ancient_steed`.
- Added phase/alternate aliases only where useful; avoided broad short aliases to limit fuzzy-OCR ambiguity as the catalog grows.

##### Modes

Added six LiveSplit profile pairs:

1. Campaign 100% - Predefined (67 targets)
2. Campaign 100% - Dynamic (67 targets)
3. Campaign Required Bosses Only v0.5 - Predefined (40 targets)
4. Campaign Any% v0.5 - Dynamic (40 targets)
5. Pinnacle v0.5 - Predefined (10 targets)
6. Pinnacle v0.5 - Dynamic (10 targets)

- Each ASL embeds a mode-specific boss whitelist.
- Out-of-mode `GONE` events are ignored and logged as `IGNORE_NOT_IN_MODE`.
- Predefined profiles ship with named rows and dynamic row naming OFF by default.
- Dynamic profiles ship with generic `Boss XX` rows and dynamic row naming ON by default.

##### Deliberate exclusions

- Beira of the Rotten Pack is now treated as a proper optional boss and included in Campaign 100%.
- Campaign Required Bosses Only removes The Rotten Druid and Diamora, Song of Death from the former Any% predefined target set.
- The named rare miniboss roster is excluded for the same UI-consistency reason.
- The Plagueling remains OCR-supported for Scourge of the Skies topology reconciliation but is support-only and never included in a bundled split whitelist.

##### Detector / timing behavior

- No intentional visual-detector feature changes from v0.1.14.
- Preserved boss completion rule: actual boss UI disappearance is authoritative; health fill is never death/completion evidence.
- Preserved firstMissing Real-Time backdating, simultaneous-event same-time reuse, queued dual-event handling, Undo re-arm, and manual-skip suppression.

##### Documentation

- Added `BossLists/` with explicit mode rosters, scope notes, excluded encounters, support-only identities, and research sources.
- Documented Campaign Any% as a project-defined v0.5 core-progression baseline rather than an official leaderboard ruleset.

## GameTimeWatcher component timeline

### v0.4.6-fixed-analysis-dpi — development

- Declares Per-Monitor-V2 DPI awareness so Windows display scaling does not virtualize PoE2 client coordinates (for example, 3840x2160 at 300% appearing as 1280x720).
- Captures directly into the canonical 576-pixel-high analysis surface using a scaled screen blit. 1080p, 1440p, 4K, and 8K therefore feed the matcher comparable pixel counts instead of making analysis cost grow with native resolution.
- Keeps native client dimensions in diagnostics while logging the separate analysis-frame dimensions.
- Moves provisional timeout rejection until after the current capture/analysis opportunity. This prevents `PENDING_PAUSE -> RUNNING` from being published immediately before the same visual frame confirms `PauseMenu`.
- Pause/MTX thresholds and the ASL provisional-accounting protocol are unchanged.

### v0.4.5-structure-first — development

- Added the shared PoE2 game-language setting to runtime diagnostics.
- Reworked pause detection to prioritize language-neutral pause-menu geometry.
- Added a second-weight paused-banner detector that looks for the dark banner plus centered bright title shape instead of exact English `GAME PAUSED` text.
- English `Resume Game` and `Exit Path of Exile` templates are now low-weight corroborators only and are skipped on non-English game clients.
- Pause evidence weighting is structure 68%, banner 24%, Resume text 4%, Exit text 4%.
- MTX Shop detection and the existing manual-pause state protocol remain unchanged.

### Changelog

#### v0.4.4
- Added `--settings <path>` support for SetupUI's generated shared run-settings snapshot.
- User-facing overrides cover ProvisionalTimeoutMs and the pause-stack, Resume Game, pause-banner, Exit Path of Exile, and MTX template thresholds.
- Invalid or missing shared settings fall back to the existing validated component `config.json`.
- External diagnostic launcher now records/hashes/copies the run-settings snapshot.
- Visual detection logic and tested default threshold values are otherwise unchanged from v0.4.3.


#### v0.4.3
- Retains the v2 provisional state protocol introduced in v0.4.2.
- Full-package ASLs now apply accepted/rejected provisional timing corrections immediately, including while the pause is active.
- Adds explicit documentation that GameTimeWatcher and the deployed ASL must come from the same protocol-aware package.
- Diagnostic/version identifiers bumped so stale watcher/ASL combinations are easier to identify.

#### v0.4.2

- Added protocol-v2 provisional states: `PENDING_PAUSE` and `PENDING_RUN`. ESC/controller Start now publishes the input-edge timestamp immediately instead of waiting for visual classification.
- LiveSplit can provisionally hold/release Game Time on that state, while the normal screen detector remains the final authority.
- Added `originUtcTicks` and `stateSequence` to the state file so ASLs can compensate the small edge-to-poll interval and undo rejected provisional transitions exactly.
- Added a 1200 ms provisional timeout. If an input edge does not visually produce the expected state, GameTimeWatcher resolves back to the last confirmed visual state and the ASL refunds/re-removes the provisional interval.
- Mouse-driven Resume/Options/Challenges transitions publish `PENDING_RUN` on the first visual Running frame, then complete the existing confirmation check.
- Confirmed state is now published before any full-resolution diagnostic PNG encoding. In v0.4.1 the diagnostic screenshot path itself could add several hundred milliseconds before the state-file update.
- Retains the v0.4.1 dedicated 5 ms input thread, centered-content fix, anchored template search, MTX handling, and fail-open heartbeat policy.
- No LiveSplit Pause hotkey is injected or rebound; Game Time remains ASL-controlled.

#### v0.4.1

- Reduced pause-template search from broad screen scans to small anchor windows around the stable centered menu positions observed in the real GameTimeWatcher captures.
- Added a dedicated 5 ms ESC/controller-Start input monitor so short input edges are not missed while image analysis is running.
- Input remains an acceleration hint only; screen state is still authoritative.
- Retains the v0.4.0 immediate state-writer wakeup, heartbeat isolation, content-bound fix, MTX behavior, and fail-open policy.
- Intended to remove the ~2 second perceived pause/unpause delay seen at 5120x1440, where v0.4.0 still spent roughly 0.47-0.57 s analyzing each frame and often required two frames.

#### v0.4.0

- Added input-assisted low-latency mode: ESC / controller Start forces an immediate high-rate visual check but is never pause authority by itself.
- While PoE2 is visually paused, GameTimeWatcher remains in high-rate capture mode so mouse-click Resume is detected without needing an ESC hotkey.
- A recent ESC/Start edge reduces visual confirmation to one strong frame for entry/exit; normal transitions still retain multi-frame confirmation.
- Replaced periodic-sleep heartbeat publication with a signaled single-writer heartbeat thread so confirmed state changes are written immediately instead of waiting up to one heartbeat interval.
- Reduced ConfirmRunningFrames from 3 to 2 for click-based Resume latency.
- Added state-aware matcher short-circuiting: strong pause-stack or MTX matches avoid redundant template searches.
- Added cached pillarbox/content bounds, a direct 24bpp grayscale path, typical-scale-first matching with strong-match early exit, and one-pass NCC math.
- Dev status now reports NORMAL/FAST detector mode and active capture FPS.
- No LiveSplit Pause hotkey is injected or rebound; ASL Game Time remains authoritative.

#### v0.3.9

- Fixed pillarbox/overlay content-bound detection exposed by the 2048x576 live menu probes.
- GameTimeWatcher now selects the centered/widest contiguous game-content band instead of joining the PoE2 render surface to an always-on-top LiveSplit window in a black side bar.
- This restores the correct visual center for Resume Game / GAME PAUSED / Exit Path of Exile / menu-stack matching.
- MTX detection, heartbeat behavior, ASL timing, and BossWatcher are unchanged.

#### v0.3.8

- Replaced single-text pause authority with a centered multi-button stack signature (Resume / Challenges / Options / Microtransaction Shop).
- Kept Resume Game, GAME PAUSED, and Exit Path of Exile as corroborating fallbacks/diagnostics.
- Added ESC/controller-Start menu-probe screenshots at approximately 100/250/500/1000 ms from GameTimeWatcher's own capture path.
- Hardened state-file writes against transient access races.
- MTX detection and the independent heartbeat thread remained unchanged.

#### v0.3.7

- Fixed the major v0.3.6 detector-throughput regression that reduced a nominal 10 FPS loop to roughly one analyzed frame every ~2 seconds in the live test.
- Precompute all pause-template scale variants once at startup instead of rebuilding them per frame.
- Restrict pause matching to narrow centered vertical bands.
- Use detection priority `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE` with thresholds 0.58 / 0.40 / 0.50.
- Move the state-file heartbeat to an independent background thread so slow image analysis cannot make the ASL heartbeat expire mid-pause.
- Keep 2-frame pause and 3-frame running confirmation; with restored detector throughput these become sub-second confirmation instead of multi-second delays.
- Add `analyzeMs` to developer diagnostics.
- Save a rate-limited center-column candidate screenshot when a near-threshold pause/MTX frame still classifies as gameplay.
- MTX template matching itself is unchanged; live v0.3.6 scores were strong (~0.97), and its inconsistent timing came from analysis/confirmation latency.


#### v0.3.6

- Pause-menu detection priority is now `RESUME GAME` -> `GAME PAUSED` -> `EXIT PATH OF EXILE`.
- Added the `GAME PAUSED` banner back as a secondary confirmation signature instead of using it as the sole detector.
- Pause-menu signatures are searched only around the center of the rendered game content.
- Added multi-scale template matching to tolerate PoE2 UI scaling differences across 16:9, ultrawide, and super-ultrawide resolutions.
- Broadened vertical search windows while keeping the horizontal search constrained to the center column.
- Developer diagnostics now report independent `resume`, `banner`, and `exit` scores.

### GameTimeWatcher changelog

#### 0.3.5 - Centered pause-menu button detector

- Replaced `GAME PAUSED` banner matching with centered menu-button matching.
- Primary pause signature is `RESUME GAME`; `EXIT PATH OF EXILE` is a fallback.
- The search is restricted to a narrow central column after canonicalizing the game content to 576 pixels high, so normal display-width/aspect-ratio changes do not require a full-screen template search.
- The Exit fallback uses a wider vertical search window to tolerate the one-time `Skip Tutorials` menu row.
- Dev-console/internal diagnostics now report `resume`, `exit`, aggregate `pause`, and `mtx` scores separately.
- MTX Shop detection is unchanged.
- Added and explicitly staged `pause-resume-game.png` and `pause-exit-path-of-exile.png`.

#### 0.3.4 - Literal-path diagnostic launcher fix

- Replaced PowerShell `Start-Process` in the external diagnostic launcher with `System.Diagnostics.ProcessStartInfo`.
- This avoids PowerShell 5.1 path binding on the literal folder name `GameTimeWatcher`, whose square brackets can be interpreted as wildcard syntax.
- Diagnostic stdout/stderr now inherit the persistent PowerShell console so startup errors remain visible instead of disappearing with the child process.
- Diagnostic launches pass `--wait-on-error`, while persistent startup failures continue to be written beside the LiveSplit state file.
- Process sampling, exit-code capture, final-state capture, internal watcher diagnostics, and Windows Application event-log collection remain enabled.

#### 0.3.3 - Startup hardening and persistent diagnostics

- GameTimeWatcher now fails with an explicit path when `config.json` or either visual template is missing instead of silently creating a fallback config and then exiting during template load.
- Template paths are resolved relative to the selected config file, with a fallback from `publish\config.json` to the support-folder `config.json`.
- `Build.ps1` explicitly copies and validates `config.json`, `pause-menu-tight.png`, and `mtx-shop.png` in the published runtime so SDK content-copy behavior cannot leave the executable without its detector data.
- Any fatal startup exception now also writes `poe2_gametimewatcher_startup_error.log` beside the LiveSplit state file.
- Normal Setup-UI launches use `--wait-on-error`; a startup failure keeps the watcher console open until Enter is pressed.
- Developer diagnostic launches use PowerShell `-NoExit`, and `Run-Diagnostic.cmd` now remains open and forwards any supplied arguments.
- The default detector thresholds/templates now match the v0.3.2 runtime config even if defaults are used.

#### 0.3.2 - Selective pause-menu matching

- Replaced broad dark-screen similarity with normalized grayscale correlation against a tight `GAME PAUSED` banner.
- Added the more selective MTX Shop correlation detector.
- Reduced image-processing overhead and added state-change screenshots in diagnostic mode.
- This revision addressed the false `PauseMenu` state seen in ordinary dark gameplay.

#### 0.3.1 - External crash diagnostics and resource hardening

- Dispose all Process wrappers returned by repeated game-window scans and throttle process discovery to 250 ms to prevent native handle accumulation.
- Added `Run-Diagnostic.ps1` / `Run-Diagnostic.cmd`, an external watchdog that captures stdout/stderr, exit code, process memory/handle/thread/CPU samples, final state, and matching Windows crash events.
- Added optional internal `--diagnostic-dir` logging with per-second detector/runtime/GC status and managed exception logging.
- State-file and watcher-log write failures are now caught and logged instead of terminating the watcher.
- Loading, boss, story, movement, and input detection remain outside GameTimeWatcher; its runtime responsibility is still only manual pause-menu / MTX detection.

#### 0.3.0
- Re-scoped GameTimeWatcher to an optional manual-pause helper only.
- Removed loading, boss-bar, voice-line, cutscene, movement, keyboard, and controller gameplay-response tracking.
- Loading-screen removal is now handled directly by each ASL from `Client.txt`.
- Added automatic `GAME PAUSED` menu detection.
- Added automatic Microtransaction Shop detection.
- Added a fresh-heartbeat state file consumed by the ASL.
- Paused state is latched while PoE2 is not foreground so a runner can pause and alt-tab during a break.

## Ordered exploration engine timeline

### v0.2.15

- Loadability hotfix: rewrote the v0.2.14 final-segment resolver using only the segment-walk pattern already proven in v0.2.13.
- Removed retained typed ISegment local references from the finalization path.
- Restored UTF-8 BOM packaging to match the known-working v0.2.13 ASL file.
- Final behavior remains: stamp Cuachic Vault and Ziggurat Refuge at the exact Ziggurat-entry time, then end/pause the timer.

### Changelog

#### 0.2.14

- Reworked the Ziggurat Refuge finish as an explicit named-segment commit.
- The script now locates `The Cuachic Vault` and `The Ziggurat Refuge` by LiveSplit segment name instead of assuming their numeric indices match the route file.
- Entering Ziggurat stamps both final cumulative split times to the exact Ziggurat-entry time.
- If Ziggurat is the actual last LiveSplit row, the timer is placed in `Ended`; if the loaded `.lss` has extra rows after it, the timer is paused at the exact finish time.
- Added `runCount`, Cuachic index, and Ziggurat index diagnostics to isolate `.lss`/route mismatches.
- Removed the second forced `TimerModel.Split()` path.

#### 0.2.13
- Fixed the final Ziggurat Refuge row not displaying its completed split time even though the timer reached the finish state.
- The native final LiveSplit split now completes first; final timestamp adjustment happens only after that split event has returned.
- Replaced final-row indexed access with a typed `foreach` walk over `IRun`, avoiding ASL dynamic-binder indexing problems.
- Reapplies the exact Ziggurat-entry timestamp to both The Cuachic Vault and The Ziggurat Refuge and calls `CallRunManuallyModified()` so the LiveSplit UI refreshes the final row.

﻿# v0.2.11

- Fixed the final Ziggurat completion path hanging immediately after `FINAL_SPLIT_2_FORCE`.
- Root cause from live validation: the forced `TimerModel.Split()` was being called from inside the `onSplit` callback for the Cuachic split, creating a re-entrant LiveSplit split event.
- Final native Ziggurat split is now queued and executed from `update{}` after the Cuachic `onSplit` callback has returned.
- Added exception logging around the native final split (`FINAL_SPLIT_2_FORCE_EXCEPTION`).
- Added a narrow explicit final-state fallback: if LiveSplit still refuses the final native split, the script stamps both Cuachic and Ziggurat at the captured Ziggurat-entry time, sets the split index to route completion, sets the timer phase to `Ended`, and notifies LiveSplit that the run was manually modified.
- Added `FINAL_STATE_EXPLICIT_COMMIT` diagnostics so the fallback is visible rather than silent.

### v0.2.9

- Fixed the final Ziggurat row failing to receive a timestamp.
- Identified LiveSplit's 300 ms DoubleTapPrevention as the reason the second forced split could be ignored.
- Added a 400 ms guard between the Cuachic and Ziggurat native split requests.
- Captures the exact time of entering The Ziggurat Refuge and reapplies it to both final split rows after the delayed final split, so the route finish time is not inflated by the guard delay.
- Final expected state with the supplied 98-row validation splits: Ziggurat stamped, timer phase Ended.

### v0.2.8

- Reworked the Ziggurat Refuge finish into an explicit two-stage native ASL split sequence.
- Entering The Cuachic Vault arms the finish without stamping the segment.
- Entering The Ziggurat Refuge queues split #1, which timestamps The Cuachic Vault.
- `onSplit` confirms LiveSplit advanced to the Ziggurat segment, then queues split #2 on the next ASL cycle.
- Split #2 timestamps The Ziggurat Refuge and normally places LiveSplit in `Ended` state.
- Pause is now only a fallback when a custom splits file has additional segments after the route.
- Manual LiveSplit index synchronization is suspended while the final two-split transaction is in flight.

### Changelog

#### 0.2.7

- Reworked the final-route transaction based on live validation.
- Entering The Ziggurat Refuge is now the explicit completion condition.
- The finish path now executes in strict order: commit The Cuachic Vault split, verify LiveSplit advanced to the Ziggurat row, then pause.
- Removed the prior `onSplit`-based finish pause, which could leave The Cuachic Vault active without committing its timestamp.
- Added `FINISH_COMMIT` / `FINISH_COMMIT_FAILED` diagnostics with before/after LiveSplit indices.

#### v0.2.6
- Corrected final-route semantics at The Ziggurat Refuge.
- The penultimate route segment is now held open while the player is inside it.
- Entering The Ziggurat Refuge performs one final split to close the penultimate segment, advances LiveSplit onto the Ziggurat Refuge row, then immediately pauses the timer.
- The Ziggurat Refuge row is intentionally left active/highlighted rather than being split to an Ended state.
- Added finish-state logging: `FINISH_ARMED`, `FINISH_TRANSITION`, and `FINISH_COMPLETE`.
- Manual advancement onto the Ziggurat row is handled with a pause-only fallback.

#### v0.2.5
- Fixed v0.2.4 failing to load in Scriptable Auto Splitter.
- Cause: ASL exposes `timer` as `LiveSplitState`; `LiveSplitState` has no `Pause()` method.
- Finish freeze now creates a `LiveSplit.Model.TimerModel` bound to the current `timer` state and calls its normal `Pause()` method.
- Corrected the READY diagnostic version string to v0.2.5.

#### 0.2.4

- Added an explicit Ziggurat Refuge finish-freeze fallback.
- The autosplitter still performs the normal final split first.
- If LiveSplit remains `Running` afterward, it calls `timer.Pause()` immediately to freeze the displayed final time.
- If LiveSplit already transitioned to `Ended`, no pause call is needed.
- Added `pauseAtFinish` setting, enabled by default.
- Added `FINISH_FREEZE` and `FINISH_COMPLETE` diagnostics.

#### 0.2.3

- Removed Deserted Post from the 100% route and Act 4 practice route.
- Deserted Post is now documented as a non-splittable subarea of Plunder's Point.
- Route validation rejects `ExpeditionSubArea_Kalguur_Act4` if manually added as a split.
- Updated the supplied Campaign 100% validation file from 99 to 98 segments.
- Removed the temporary Deserted Post raw-log diagnostic.
- Ziggurat Refuge remains the final route entry and completing that final split ends the run.

#### 0.2.2

- Added explicit final-run handling/logging for The Ziggurat Refuge.
- Added a temporary Deserted Post fallback/diagnostic while investigating its missing area transition.

#### 0.2.1

- Synchronize internal route progress with `timer.CurrentSplitIndex`.
- LiveSplit **Skip Segment** now skips the corresponding route entry.
- LiveSplit manual **Split** advances the route entry as well.
- LiveSplit **Undo Split** moves the expected route entry backward.
- Added `syncLiveSplitIndex` setting, enabled by default.
- Added LiveSplit split index to the status file.
- Added `LIVESPLIT_ADVANCE` / `LIVESPLIT_UNDO` diagnostic messages.

#### v0.2.0 — campaign validation

- Expanded from the five-area Act 1 test route to the complete 100% campaign reference route.
- Corrected Freythorn ordering to immediately follow Hunting Grounds.
- Corrected The Azak Bog ordering to immediately follow The Matlan Waterways.
- Added Lost Catacombs (`ExpeditionSubArea_Kalguur_Act1`).
- Added Skull of the Titan (`ExpeditionSubArea_Kalguur_Act2`).
- Added Mystic Refuge (`ExpeditionSubArea_Kalguur_Act3`), counted once on first entry by the supplied route.
- Added Deserted Post (`ExpeditionSubArea_Kalguur_Act4`).
- Reordered the late Act 4 reference chain to Arastas → Excavation → Ngakanu → Heart of the Tribe → Plunder's Point → Deserted Post.
- Added strict route validation with line-number errors.
- Added generated area-level capture.
- Added unique-area validation CSV and unknown-area raw logging.
- Improved status output after splits.
- Added 100%, any% baseline, and Act-specific practice route files.
- Added `Summarize-Validation.ps1`.

#### v0.1.0 — proof of concept

- First ASL build.
- Client.txt auto-discovery.
- Riverbank auto-start.
- Ordered-route splitting.
- Unexpected-area ignore behavior.
- Basic status/debug logging.

- Added a ready-made 99-segment LiveSplit `.lss` for the Campaign 100% validation route.
- Added `Gen-Ordered.ps1` for regenerating `.lss` files after route edits.
- Clear the unknown-area capture on each new ASL attachment/reload for clean validation sessions.

## Flexible exploration engine timeline

### Changelog

#### v0.3.0

Milestone transition from ordered routes to flexible first-visit area pools.

##### Added
- Hierarchical Scriptable Auto Splitter settings:
  - Act 1
  - Act 2
  - Act 3
  - Act 4
  - Interludes
- Individual checkbox for every one of the 97 default campaign area splits.
- Unordered first-visit completion tracking.
- Dynamic LiveSplit slot naming based on actual completion order.
- Independent subgroup completion counters in the status file.
- `poe2_area_groups.txt` reference file.
- Flexible 97-slot + Ziggurat `.lss`.
- Unordered Client.txt replay test tool.

##### Changed
- `poe2_route.txt` is no longer used by the v0.3 script.
- No area is treated as "next expected."
- Revisits are ignored based on a completed-area set rather than route position.
- Act 4 and all three Interludes may be progressed in arbitrary order without desynchronizing the script.
- Ziggurat finish logic is generalized for both:
  - Cuachic-as-final-area runs;
  - Cuachic-completed-earlier runs.

##### Preserved
- Client.txt area parsing.
- Riverbank auto-start.
- validation CSV and unknown-area capture.
- explicit Ziggurat finish behavior derived from stable v0.2.15.
- Deserted Post remains reference-only/non-splittable.

##### Known limitations
- Default `.lss` assumes all 97 area settings are enabled.
- Manual Skip Segment is not supported by the unordered completion model.
- PB comparison rows are slot-based when route order changes.
