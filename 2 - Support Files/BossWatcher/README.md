# Path of Exile 2 BossWatcher v0.3.1 / BossRush + Maps

BossWatcher is the visual boss-completion tracker used by Boss Rush, mixed, Trials, and Maps setups. Identity-based encounters retain the established OCR/catalog detector. v0.3.1 adds an ASL-controlled structural mode for ordinary map bosses so map completion does not depend on recognizing a boss name.

## Map context (v0.3.1)

BossWatcher now accepts `--context-file <path>`. The ASL-owned context file selects one of three detector modes without changing the existing identity/OCR logic:

- `mode=map` — ordinary map boss. Tesseract/name matching is bypassed. A structurally valid PoE2 boss UI (health-bar red run plus the gold boss-UI band) arms the encounter; verified UI disappearance emits `MAP_GONE`. No boss name is required or written to LiveSplit.
- `mode=identity` — existing OCR/catalog path for campaign bosses, trial identities, and selected Pinnacle objectives.
- `mode=off` — no boss tracking, used after an ordinary map split until the next area transition.

Map events are `MAP_SEEN` and `MAP_GONE` and include the ASL-provided area ID, generated area level, and map-boss number. The map tracker does not use boss-name glyph/template matching. Dual boss UI is treated as one ordinary map objective and completes only when the full boss UI disappears.

If a map boss bar disappears because the runner died or left rather than completing the encounter, the Maps policy intentionally relies on LiveSplit **Undo Split**. The ASL then restores/re-arms the current map objective.

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

The fight timer begins when BossWatcher emits the boss `SEEN` encounter and ends at the boss UI's `firstMissing` timestamp. The later disappearance-confirmation delay therefore does not inflate the displayed fight duration.

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

v0.3.0 accepts:

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

v0.3.0 does not intentionally change the visual detection algorithm. It retains:

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
- Tesseract data installed through `Setup-OCR.ps1`.

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
- `CHANGELOG.md` — release changes;
- `VALIDATION.txt` — validation report;
- `CHECKSUMS.sha256` — package file hashes.


## Build-path note (v0.3.1)

`Build.ps1` uses literal PowerShell filesystem paths so the bundled directory name `BossWatcher` is safe. Square brackets are otherwise wildcard syntax for cmdlets such as `Test-Path` and `Copy-Item`.
