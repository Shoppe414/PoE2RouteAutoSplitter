## 0.3.7 - Height-relative boss UI geometry

- BossWatcher now derives the horizontal boss capture width from the PoE2 **client height** and keeps it centered on the game client. This preserves comparable boss-UI geometry across 16:9, ultrawide, and super-ultrawide clients instead of stretching the OCR region with total client width.
- Dual-boss OCR lanes are now independent wide left/right regions split at the capture midpoint rather than narrow children of the single-boss name ROI. This prevents long dual-boss names such as Hadi of the Flaming River / Rafiq of the Frozen Spring from being truncated before OCR.
- Development diagnostics now log detected client resolution/aspect ratio, boss-capture dimensions, and the left/right OCR rectangles whenever the game client size changes.
- Capture continues to use the PoE2 client rectangle only; window title bars, borders, taskbar, and unrelated desktop pixels are outside the captured ROI.

## 0.3.6-map-exit-assist (development)
- Preserved the conservative 5500 ms in-map disappearance confirmation for ordinary deterministic map bosses.
- Added a guarded external-exit assist: a boss already armed by `database-ocr` and continuously missing for at least 500 ms can emit trusted `MAP_GONE` when ASL context confirms a real external map exit.
- The assist cannot arm a boss, cannot use structural fallback, and is explicitly suppressed for recognized map-child transitions.
- `MAP_GONE` now records `confirmation=timer` or `confirmation=exit-assist`.
- Added `MapExitAssistMinMissingMs` (default 500 ms) to BossWatcher config and generated run-settings snapshots.
- Added the observed `MapSavanna` -> Savannah mapping so Caedron, the Hyena Lord can be resolved by the deterministic map database.
- Client.txt parsing changes live in the generated ASLs; BossWatcher consumes only the resulting canonical context and remains independent of localized area display names.

## 0.3.5-map-identity-safe (development)
- Map mode now fails closed when the current map is missing from the map-boss database or uses a special completion type. Structural fallback no longer emits MAP_SEEN/MAP_GONE for unknown/special maps.
- Added deterministic `MapPort` -> `Malgor, the Nautilord` mapping from current PoE2DB data.
- BossWatcher now creates `poe2_boss_watcher_debug.log` immediately when the event writer starts and prints its path in developer console mode.

# v0.3.4-localized-ocr — development

- Added a separate PoE2 game-language setting for boss-name OCR.
- BossWatcher selects the matching Tesseract model for English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, or Thai.
- Added `boss-localizations.json`, keyed by invariant boss ID. Runtime OCR resolves a verified localized name back to the same canonical boss ID.
- Added `--localization-db` and generated-run localization database snapshots with SHA-256 validation metadata.
- Non-English missing-name coverage is conservative: no guessed or silent English-name fallback; deterministic maps log `MAP_LOCALIZATION_UNAVAILABLE` and do not arm from an unrelated boss.
- `Setup-OCR.ps1` can install one language or all supported language models; build/release scripts verify the complete model set.
- Existing 5.5-second disappearance policy and structural tracking remain unchanged.

# Changelog

## 0.3.3 - 5.5-second global development grace + deterministic map-boss database

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

## 0.3.2 - Configurable GONE confirmation grace

- Raised the default identity/single-boss disappearance confirmation grace from 350 ms to 8000 ms.
- The original `firstMissing` timestamp remains authoritative, so confirmation delay does not inflate recorded completion time.
- Added `--settings <path>` support for the generated shared `poe2_run_settings.json` snapshot.
- Added separate user-facing `BossWatcher.MapGoneConfirmMs` for structural Maps mode (default 700 ms). It is intentionally independent because map qualification must occur before exit.
- Identity dual-lane resolver windows remain separately calibrated at 350/700 ms.
- Valid shared-setting ranges are GoneConfirmMs=500-30000 ms and MapGoneConfirmMs=100-30000 ms; invalid/missing overlays fall back to validated `config.json`.
- Pending missing diagnostics now include the active confirmation window.

## 0.3.1 - Map context / structural ordinary-map bosses

- Added `--context-file <path>` and ASL-controlled `identity`, `map`, and `off` detection modes.
- `map` mode bypasses OCR, name matching, and the boss catalog. It detects a structurally valid boss UI and emits `MAP_SEEN` / `MAP_GONE` on verified appearance/disappearance.
- Structural map tracking uses boss health-bar red-run evidence plus the gold boss-UI band; it does not read or template-match boss-name glyphs.
- Dual map-boss UI is one map objective and completes only when the entire dual/recentered boss UI is gone.
- `identity` mode preserves the existing campaign/trial/Pinnacle OCR detector unchanged.
- `off` mode disables boss tracking after an accepted map completion until the ASL changes context.
- Context-file transient/missing reads preserve the last good context rather than briefly flipping detector modes.

## 0.3.1 data/catalog update - optional trial bosses

- Expanded the external `bosses.txt` OCR identity catalog from 78 to 85 without changing the BossWatcher detector executable.
- Added Rattlecage, Hadi, Rafiq, Ashar, Uxmal, Chetza, and Bahlak.
- Re-grouped the already-supported Zarokh and The Trialmaster identities under their trial sections while retaining their IDs and existing pinnacle target references.
- Existing Campaign 100%, Required Bosses Only, and Pinnacle target lists are unchanged; trial identities are opt-in through Custom Routes or applicable campaign/Act premades. Pinnacle retains Zarokh and Trialmaster as category targets.

## 0.3.1 - Build path hotfix

- Fixed `Build.ps1` OCR-data validation when the package is located under the bundled `BossWatcher` directory.
- `Test-Path` and build-time source copies now use `-LiteralPath`, preventing PowerShell from interpreting square brackets as wildcard syntax.
- No detector, OCR, timing, event, or console behavior changes from 0.3.0.

## 0.3.0 - User console and launcher integration

### User console
- Removed the per-frame detector status output from normal launches.
- Normal output now shows timestamped watcher start/stop, boss encounter, and boss defeat events.
- Boss defeat lines include fight duration in seconds.
- Fight duration is measured from the recorded `SEEN` encounter timestamp to `firstMissing`, excluding the later disappearance-confirmation delay.
- `RETURNED` events and raw detector/OCR data remain log-only in normal mode.

### Developer console
- Added `--dev-console` and alias `--dev` to restore verbose frame-by-frame diagnostics.
- Developer mode retains [S] diagnostic capture and [R] tracking reset controls.

### Event path override
- Added `--event-file <path>` so a launcher can explicitly choose the event log location.
- Updated `Run-Source.ps1` with `-DevConsole` and `-EventFile` parameters.

### Detector / catalog
- No intentional detector-algorithm change from the v0.2.0 package baseline.
- Retains 67 Campaign 100% targets, 40 required-boss v0.5 targets, 10 Pinnacle targets, and The Plagueling as support-only OCR identity.

## 0.2.0 - BossRush content expansion

### Boss catalog

- Expanded `bosses.txt` from the v0.1.14 validation set to **78 OCR identities**.
- Added the complete supported Acts 1-4 + three-interlude campaign boss catalog for the v0.5 baseline.
- Added the current **10-boss v0.5 pinnacle roster**.
- Split ambiguous Jamanra identity into `jamanra_risen_king` and `jamanra_abomination`.
- Preserved existing live-tested IDs such as `scourge_of_the_sky`, `iktab_the_deathlord`, and `ekbab_ancient_steed`.
- Added phase/alternate aliases only where useful; avoided broad short aliases to limit fuzzy-OCR ambiguity as the catalog grows.

### Modes

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

### Deliberate exclusions

- Beira of the Rotten Pack is now treated as a proper optional boss and included in Campaign 100%.
- Campaign Required Bosses Only removes The Rotten Druid and Diamora, Song of Death from the former Any% predefined target set.
- The named rare miniboss roster is excluded for the same UI-consistency reason.
- The Plagueling remains OCR-supported for Scourge of the Skies topology reconciliation but is support-only and never included in a bundled split whitelist.

### Detector / timing behavior

- No intentional visual-detector feature changes from v0.1.14.
- Preserved boss completion rule: actual boss UI disappearance is authoritative; health fill is never death/completion evidence.
- Preserved firstMissing Real-Time backdating, simultaneous-event same-time reuse, queued dual-event handling, Undo re-arm, and manual-skip suppression.

### Documentation

- Added `BossLists/` with explicit mode rosters, scope notes, excluded encounters, support-only identities, and research sources.
- Documented Campaign Any% as a project-defined v0.5 core-progression baseline rather than an official leaderboard ruleset.
