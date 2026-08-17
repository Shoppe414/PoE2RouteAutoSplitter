# Changelog

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
