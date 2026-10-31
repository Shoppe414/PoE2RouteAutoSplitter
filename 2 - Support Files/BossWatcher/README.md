# Path of Exile 2 BossWatcher v0.4.3 / Structural geometry hardening

## v0.4.3 structural geometry / load suppression

- Suppresses acquisition/tracking while generated ASLs report `LOADING` through `poe2_load_state.txt`.
- Requires full-ROI boss-scale single geometry before OCR and independent full-ROI left/right structure before dual OCR.
- Rejects implausibly gold-dense name regions before anonymous arming.
- Removes long horizontal frame artifacts from all OCR masks and isolates the centered single-name text cluster before `SingleLine` OCR.
- Keeps v0.4.2 anonymous timing, buffered OCR diagnostics, `ANON_GONE`, and route-specific resolution semantics unchanged.

## v0.4.2 unresolved encounter handoff / buffered OCR diagnostics

- Structurally verified single-boss encounters that remain unidentified now emit `ANON_GONE` instead of being discarded.
- `ANON_GONE` preserves `encounterStart`, `firstMissing`, verification timing, and fight duration; route scripts decide whether a specialized resolver may consume it.
- Buffered OCR logs every selected frame/mask attempt, Tesseract confidence, nearest known boss, similarity, threshold, and accepted/rejected status. Production OCR thresholds are unchanged.
- General campaign routes remain fail-closed unless an ordered route explicitly uses provisional sequence confirmation. Trial-specific deterministic rules live in the mixed-route ASL, not BossWatcher.

## v0.4.1 anonymous structural arming / delayed identity OCR

Single-boss encounter timing is no longer gated on Tesseract finishing before the boss dies. A strong single-boss UI can arm anonymously from one captured frame; weaker candidates retain the existing multi-frame structural confirmation. The watcher immediately records the structural first-seen timestamp and learns the spatial boss-name template even though the boss ID is still unknown.

Live OCR remains the primary identity path. While identity is unresolved, BossWatcher keeps a bounded native-resolution boss-ROI ring buffer. If the UI disappears first, the learned template starts the normal disappearance verification window immediately, while a dedicated background Tesseract engine OCRs the best retained frames (gold, broad, then temporal composite fallback). A named `GONE` event is emitted only after identity is resolved; structural evidence alone cannot create a boss split.

This specifically removes the old race where `AcquireSingle` cancelled when the boss UI vanished before OCR had matched a name. `SEEN` is timestamped to the original structural first-seen frame and `GONE` continues to use the original `firstMissing` frame, so delayed identity resolution does not inflate fight or split time.


## v0.3.9 Controller boss-name OCR profile

SetupUI now snapshots `PoE2.InputUiMode`. Mouse + Keyboard retains the existing orange/gold OCR calibration unchanged. Controller uses a separate bright-luminance acquisition mask calibrated from the supplied controller boss-name rendering; this preserves the thinner/brighter controller glyph strokes that were fragmented by the KBM gold mask. The boss ID catalog, completion/disappearance rules, and LiveSplit event semantics are unchanged.


BossWatcher is the visual boss-completion tracker used by Boss Rush, mixed, Trials, and Maps setups. Localized OCR uses the selected PoE2 **game language** while downstream rules continue to use invariant boss IDs. Deterministic ordinary maps require database-backed OCR identity so unrelated/event boss UI cannot qualify the map. v0.3.6 also adds a guarded external-exit assist for runners who portal out immediately after a verified map-boss kill.

## v0.3.4 localized boss-name OCR

SetupUI now stores a separate **PoE2 game language**. It is independent from the SetupUI display language: a user may run the application UI in one language while the game client uses another. The generated `poe2_run_settings.json` records the selected game language and SetupUI snapshots `boss-localizations.json` into `LiveSplit Target\poe2_boss_localizations.json`. The localization snapshot and its SHA-256 are included in setup validation.

This development build defines OCR profiles for English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai. `Setup-OCR.ps1` downloads the matching Tesseract data models. Release/build scripts require all supported models before publishing BossWatcher.

Localized OCR is deliberately **ID-first**. `bosses.txt` and `map-bosses.json` keep the invariant BossWatcher boss IDs. `boss-localizations.json` maps those IDs to verified localized on-screen names. OCR matches the selected-language spelling and then emits the same invariant boss ID used by LiveSplit, Maps policy, and the run audit.

The initial v0.3.4 development localization catalog is intentionally incomplete while authoritative proper names are being populated. For a non-English client, a boss with no verified localized name is **not** silently matched against English and is never guessed. A deterministic map whose expected boss lacks localization logs `MAP_LOCALIZATION_UNAVAILABLE` and does not arm from an unrelated boss UI. This conservative behavior is intended for field testing while the catalog expands.

Command-line overrides used by SetupUI include `--settings`, `--map-db`, and `--localization-db`.

## v0.3.3 shared 5.5-second grace + map-boss database

BossWatcher now accepts `--settings <poe2_run_settings.json>`. SetupUI passes the generated run snapshot automatically. The user-facing `BossWatcher.GoneConfirmMs` setting defaults to **5500 ms** for ordinary identity/single-boss disappearance.

The longer window is confirmation only. The tracker keeps the original `firstMissing` timestamp; if the boss remains absent for the configured grace, `GONE` is emitted later but the event/split remains backdated to that first valid missing signal. If presence returns before the grace expires, the pending missing window is cancelled and the same encounter remains armed.

Maps mode keeps `BossWatcher.MapGoneConfirmMs` at **5500 ms** while the player remains inside the map. A fast post-kill external exit has a separate safety path: if the expected map boss was already armed by database OCR and continuously missing for at least **500 ms before the real external-exit context**, BossWatcher may confirm `MAP_GONE` on that exit. This does not shorten ordinary in-map disappearance handling. Identity dual-boss lane removal retains its separately calibrated short resolver windows.

The master settings are edited through SetupUI or `1 - User Setup\PoE2AS-Settings.json`. Generate / Deploy snapshots the effective values into `LiveSplit Target\poe2_run_settings.json`, which is included in setup SHA-256 validation. Valid ranges: `GoneConfirmMs` **500-30000 ms**; `MapGoneConfirmMs` **100-30000 ms**; internal `MapExitAssistMinMissingMs` **100-5000 ms** (default **500 ms**).


## Map context / deterministic map-boss identity

BossWatcher accepts `--context-file <path>`. The ASL-owned context file selects one of three detector modes:

- `mode=map` — ordinary map boss. BossWatcher resolves the active `Map<name>` area against `map-bosses.json`. For a deterministic database entry, a structural boss UI must also OCR-match one of that map's expected completion bosses before the encounter is armed. A non-matching boss is ignored rather than being allowed to emit `MAP_GONE`.
- `mode=identity` — existing OCR/catalog path for campaign bosses, trial identities, and selected Pinnacle objectives.
- `mode=off` — no boss tracking, used after an ordinary map boss has qualified the active map and while the policy waits for the player to leave that map.

`map-bosses.json` is deliberately separate from the campaign `bosses.txt` catalog. SetupUI snapshots it as `LiveSplit Target\poe2_map_bosses.json` and BossWatcher uses that generated copy for the run. This keeps the map OCR candidate set extremely narrow: normally one expected boss, or a small `any`/`all` set for maps that list multiple bosses. The database also has an `EventBosses` section. The initial development seed includes Delirium boss identities so a recognized event boss can be logged as `MAP_EVENT_BOSS_IGNORED`.

If the active area is not resolved by the deterministic map database, or is marked as a special/random-completion map, BossWatcher **fails closed** and writes `MAP_DATABASE_MISS` or `MAP_DATABASE_SPECIAL` to the debug log. Structural-only fallback cannot emit trusted map qualification.

Map events remain `MAP_SEEN` and `MAP_GONE`. Trusted deterministic-map events include `bossId`, `bossName`, `detector=database-ocr`, and `MAP_GONE` also records `confirmation=timer` or `confirmation=exit-assist`. Dual boss UI is treated as one ordinary map objective and completes only when the full tracked boss UI disappears.

For Maps policy v2, `MAP_GONE` is a **qualification event**. Normally the timer completes the map when Client.txt confirms the subsequent real map exit. If a very fast exit is observed before the 5500 ms in-map grace expires, the ASL first saves that exit provisionally; a trusted exit-assisted `MAP_GONE` can then retroactively finalize SUCCESS at that saved exit timestamp. Re-entering the same area ID + seed still continues an unresolved attempt, while entering a different map seed confirms it as failed.

## User console

A normal launch is intentionally quiet. It does not print the detector's frame-by-frame state.

The console reports:

- timestamped watcher start/stop;
- boss encounter and name;
- boss defeat and name;
- fight duration in seconds.

Example:

```text
[17:42:10.215] BossWatcher started. Press [Q] to quit.
[17:42:18.104] Encountered: Count Geonor
[17:43:09.771] Defeated: Count Geonor | Fight time: 51.667 s
```

The fight timer begins at the first structurally armed boss-UI frame and ends at the boss UI's `firstMissing` timestamp. If OCR identity resolves later, the emitted `SEEN` event is back-timestamped to that original structural first-seen frame. The later disappearance-confirmation and post-disappearance identity work therefore do not inflate the displayed fight duration.

`RETURNED` events, OCR data, template coverage, health-run diagnostics, and other detector internals remain available in the event/debug logs rather than being printed to ordinary users.

Press **Q** to stop BossWatcher.

## Developer console

The previous verbose detector console is retained for calibration, troubleshooting, and development.

```powershell
.\publish\PoE2BossWatcher.exe --dev-console
```

or from source:

```powershell
.\Run-Source.ps1 -DevConsole
```

Developer mode restores the frame status line, OCR/mask metrics, [S] diagnostic capture, and [R] tracking reset controls.

## Explicit event-file output

BossWatcher accepts:

```text
--event-file <path>
```

This overrides automatic event-log path resolution. The v1.3.2 Setup UI uses it so BossWatcher writes `poe2_boss_events.log` directly into the currently deployed LiveSplit target directory.

Maps deployments also pass `--context-file <LiveSplit Target\poe2_boss_context.txt>` so the same BossWatcher process can switch between structural ordinary-map detection and OCR identity detection for optional Pinnacles.

Source-run example:

```powershell
.\Run-Source.ps1 -EventFile 'C:\PoE2 LiveSplit Target\poe2_boss_events.log'
```

Both options may be combined:

```powershell
.\Run-Source.ps1 -DevConsole -EventFile 'C:\PoE2 LiveSplit Target\poe2_boss_events.log'
```

## Boss catalog

The current catalog contains **85 OCR identities** in `bosses.txt`. The bundled campaign and pinnacle target lists remain unchanged; seven newly cataloged trial identities expand visual recognition without silently adding optional trial bosses to existing routes.

The catalog now includes the full Trial of the Sekhemas boss roster and the Trial of Chaos boss pool. Zarokh and The Trialmaster were already OCR-supported through the pinnacle catalog; the remaining trial identities are promoted in the v2.2.1 Release Candidate.


### Optional trial bosses

Trial bosses are OCR-supported but are **opt-in** for custom route construction. The Setup UI keeps them out of the normal Bosses list and exposes a separate **Add trial boss objectives** checklist. Checked milestones are inserted into the custom route preview and can be moved normally.

Sekhemas Floor 2 is represented as one composite route objective: **Hadi of the Flaming River + Rafiq of the Frozen Spring**. BossWatcher still recognizes both identities separately, while the custom ASL completes the single floor milestone only after both `GONE` events are observed in either kill order.

The three ordinary Chaos bosses are random. They remain individually selectable rather than being bulk-required by the Setup UI; custom rules should select only the identities their category actually requires. The dedicated Trials runtime will define random-round semantics separately after further validation.

### Campaign 100%

Campaign 100% includes every supported proper boss encounter in the current package baseline, including optional boss **Beira of the Rotten Pack**. The 67 targets are documented in `BossLists/campaign-100.txt`.

### Campaign Required Bosses Only v0.5

This is a project-defined required-boss baseline rather than an official leaderboard ruleset. It contains **40 targets** and excludes optional bosses including **The Rotten Druid** and **Diamora, Song of Death**.

The targets are documented in `BossLists/campaign-any-v0.5.txt`.

### Endgame Pinnacle v0.5

The Pinnacle profile contains 10 supported pinnacle-boss targets documented in `BossLists/pinnacle-v0.5.txt`.

## Mini-boss policy

Fixed named rare minibosses are not full BossWatcher split targets because the current tracker depends on the stable top-center boss UI contract. See `BossLists/excluded-unstable-minibosses.txt`.

## The Plagueling

`The Plagueling` remains OCR-supported only for Scourge of the Skies dual-layout/topology reconciliation. It is not a split target.

## Completion and timing rule

Health percentage is never used as proof that a boss is complete. BossWatcher emits `GONE` only after the learned boss UI/name template disappears and remains absent through the configured confirmation window.

The event retains both the confirmation timestamp and `firstMissing`. Boss Rush and mixed ASLs use `firstMissing` to preserve the established Real Time backdating behavior.

## Detector pipeline retained

The established structural detection pipeline is retained:

- top-center boss UI capture;
- single/dual topology detection;
- broad + gold OCR preprocessing;
- persistent per-lane dual acquisition;
- learned name-template presence tracking;
- `SINGLE -> DUAL -> SINGLE -> NONE` reconciliation;
- low/zero-health-safe completion logic;
- idle OCR pre-gates and early cancellation.

## Build

Requirements:

- Windows;
- .NET 10 SDK;
- Tesseract language data installed through `Setup-OCR.ps1` (all supported game-language models are downloaded by default).

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Setup-OCR.ps1
.\Build.ps1
```

The Git source tree does not commit the compiled binary. `Build-Release.ps1` and the GitHub Actions release workflow build BossWatcher as a self-contained Windows runtime and include it in the installer/portable release.

## Files

- `bosses.txt` — all OCR-supported boss identities;
- `BossLists/` — target lists, exclusions, and support-only identities;
- `config.json` — visual/OCR configuration;
- `src/PoE2BossWatcher/` — watcher source;
- `Calibration/` — retained calibration captures;
- `Run-Source.ps1` — source launch with optional `-DevConsole` and `-EventFile`;
- `../../CHANGELOG.md` — unified project and component change timeline;
- `VALIDATION.txt` — validation report;
- `..\..\3 - verification files\BossWatcher-CHECKSUMS.sha256` — BossWatcher source/package file hashes.


## Build-path note (v0.3.1)

`Build.ps1` uses literal PowerShell filesystem paths so the bundled directory name `BossWatcher` is safe. Square brackets are otherwise wildcard syntax for cmdlets such as `Test-Path` and `Copy-Item`.

Map safety note: unknown or special map IDs fail closed; structural-only MAP_GONE events are not considered trustworthy map completion evidence.

## Display geometry

BossWatcher does not require the user to configure a monitor or game resolution. It reads the live Path of Exile 2 **client rectangle** from Windows. The boss UI capture is centered horizontally and its width is derived from client height, which keeps the expected UI scale stable across 16:9, ultrawide, and super-ultrawide aspect ratios.

Only the boss region inside the game client is captured. The Windows title bar, window borders, taskbar, and the rest of the desktop are not part of the OCR image. In development-console mode, BossWatcher logs the detected client dimensions, simplified aspect ratio, boss-capture dimensions, and dual-boss lane rectangles whenever the client size changes.

## v3.2.0 WGC structural hardening

BossWatcher 0.4.3 adds two pre-OCR safety layers. First, generated ASLs write `poe2_load_state.txt`; while it is `LOADING`, BossWatcher cancels/suppresses boss acquisition so loading-screen artwork cannot become an anonymous encounter. Second, single-boss acquisition requires a boss-scale coherent health run across the full captured ROI before any OCR lane is considered. This prevents ordinary enemy bars such as `Drowned` from appearing full-width only because they fill a local crop.

Single-boss OCR now removes long horizontal frame/border artifacts and blanks distant mask components outside the centered text cluster before `SingleLine` Tesseract OCR. The identity similarity threshold remains 0.72. Anonymous timing, ROI buffering, post-disappearance OCR, and route-specific fallback resolution remain unchanged.

