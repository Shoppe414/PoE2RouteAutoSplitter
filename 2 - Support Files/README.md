# Path of Exile 2 Route AutoSplitter for LiveSplit — v2.2.1 Release Candidate

## Maps structural boss mode (v2.2.1 Release Candidate)

SetupUI includes a **Maps** tab for Dynamic/unordered endgame boss runs. The user chooses 1–100 ordinary map-boss completions and may optionally add selected Pinnacle bosses. On a qualifying ordinary-map entry, the mixed ASL renames the active row to `Map Level X - Boss #Y` and writes `mode=map` to `poe2_boss_context.txt`. BossWatcher then bypasses Tesseract and the boss-name catalog: it uses only structural boss-UI evidence (red health-bar run plus the gold boss-UI band) and emits `MAP_GONE` after the UI is verified absent.

After a committed ordinary-map split, BossWatcher is switched to `mode=off` for the remainder of that map so another boss UI cannot consume the next slot. LiveSplit **Undo Split** re-arms the current map objective when a disappearance represented a failed/abandoned attempt. Known non-map/Pinnacle contexts use `mode=identity`, preserving the normal OCR path for selected Pinnacle objectives.

Map classification remains diagnostic: a level-65+ unknown destination entered from The Ziggurat Refuge or a Hideout is logged as `unconfirmed-map-or-special`. Additional special/Pinnacle exclusions will be based on mapping diagnostics. Random map-objective completion and modifier-count extraction remain non-functional/diagnostic.

## Compact package paths

This release uses a short archive root (`PoE2AS-v2.2.1`) and compact support names such as `01-Ordered`, `04-Checklist`, `BossWatcher`, and `GameTimeWatcher`. Generated .NET `bin` and `obj` folders are intentionally excluded from the release archive.

The four runtime anchor names `1 - User Setup`, `2 - Support Files`, `Setup UI [Configuration]`, and `LiveSplit Target` are intentionally retained for compatibility with the already-built Setup UI executable.

## Normal user installation

Download and run:

`PoE2AS-v2.2.1-Setup.exe`

The installer uses the established two-folder runtime:

```text
PoE2AS-v2.2.1
├── 1 - User Setup
│   ├── PoE2RouteSetup.exe
│   └── LiveSplit Target\
└── 2 - Support Files
    ├── Setup UI [Configuration]\ui-manifest.json
    ├── BossWatcher\publish\...
    ├── GameTimeWatcher\publish\...
    ├── 01-Ordered\...
    ├── ...
    └── 14-Custom\...
```

`LiveSplit Target` remains the fixed deployment directory and is preserved
across installer upgrades.

## LiveSplit setup

1. Launch `PoE2RouteSetup.exe`.
2. Select a premade configuration or build a custom route.
3. Optionally enable **Pause LiveSplit Game Time while PoE2 is manually paused**.
4. Press **Generate / Deploy Selected Setup**.
5. Open the generated `.lss` from `1 - User Setup\LiveSplit Target`.
6. Keep your own LiveSplit layout.
7. Add/edit the layout's **Scriptable Auto Splitter** component and point it at
   the generated `.asl`.
8. For Boss Rush or mixed routes, press **Start BossWatcher**.
9. If manual-pause removal was enabled, press **Start GameTimeWatcher**.

The Setup UI does not generate, copy, or modify `.lsl` files.

## Game Time

Every deployed ASL has load removal enabled by default.

The ASL tails Path of Exile 2 `Client.txt` independently of its route/boss
reader. `Got Instance Details` begins the live Game Time pause for a zone
transition. When PoE2 later writes:

```text
[LOADING SCREEN] (Area Name) Duration = X seconds
```

the ASL uses that game-reported duration as the authoritative amount of load
time to remove and corrects any small log-observation difference.

Boss introductions, boss outros, NPC dialogue, and ordinary scripted story
sequences remain timed. Testing showed that PoE2 commonly allows useful
inventory management during those sequences, so they are treated as part of
the run rather than free time.

For load-removed timing, configure LiveSplit to display/compare against
**Game Time**.

### Optional manual-pause removal

GameTimeWatcher is **not required** for normal load removal.

It is only used when the runner enables the Setup UI option to pause LiveSplit
Game Time along with PoE2's real manual pause state. GameTimeWatcher v0.4.3 recognizes:

- the centered in-game pause menu; and
- the Microtransaction Shop when opened from that paused state.

For ESC/controller-Start transitions, v0.4.3 publishes a provisional timestamp immediately and lets the ASL hold/release Game Time on its next update while the screen detector verifies the result. Rejected candidates are compensated automatically, so the final Game Time remains screen-authoritative.

Options and Challenges/Achievements remain timed because PoE2 resumes the game
simulation while those interfaces are open.

The helper writes a heartbeat state file to `LiveSplit Target`. The ASL honors
a pause only while that heartbeat is fresh; a missing/stale helper fails open.

### GameTimeWatcher crash diagnostics

Enable **Developer console diagnostics** before pressing **Start
GameTimeWatcher** to launch the external watchdog instead of the helper directly.
It records watcher stdout/stderr, exit code, working/private/virtual memory,
handle/thread counts, CPU time/responsiveness, the internal watcher diagnostic
log, and matching Windows Application crash events under:

`GameTimeWatcher\diagnostics\YYYYMMDD-HHMMSS`

v0.3.1 also disposes the `Process` wrappers created by repeated PoE2 window
scans and throttles process discovery to 250 ms. The older loop enumerated
processes every ~10 ms without disposing those wrappers, making native handle
accumulation a plausible cause of the reported early exit. The watchdog remains
in place to confirm resource stability or capture a different failure.

## Setup UI

The Setup UI provides a compact premade generator plus the mixed custom-route builder. Premade generation is selected with **Mode**, **Setup**, and **Ordered / Dynamic** controls rather than a long preset table. Area and boss modes support Campaign 100% / Any%, Act, Interlude, All-Interlude, and Act/Interlude Combination scopes where applicable; Boss Completion also exposes Pinnacle. Optional Sekhemas/Chaos content can be scheduled per floor/stage with user-facing **Run after** placement controls on ordered routes. Level Race retains the 1–100 / every-10-level premade.

Sekhemas and Chaos are independent opt-ins on the Premade tab. The user chooses the trial depth/encounter count, and ordered routes choose the base area or boss immediately preceding the trial block. Dynamic routes add the selected trial objectives to the completion pool without imposing a trial order. The underlying static preset files remain in Support Files for reference/backward compatibility, but new premade deployments are generated as `Premade-Route.lss` + `PoE2-Premade.asl`.

Custom routes can combine supported areas, bosses, levels, and opt-in trial boss milestones. The catalog is filtered by Act / Interlude / Pinnacle and hides internal IDs; Areas and Bosses both provide **Add All** for the currently visible filtered list. Ordered routes support repeated copies of the same boss through unique occurrence objectives; dynamic/unordered routes use a user-selected eligible boss pool plus a configurable encounter target (defaulting to the current 40-boss campaign-required baseline). Deployment is staged before the active target is touched. Replacing a non-empty target requires confirmation.

## BossWatcher

BossWatcher handles identity-based Boss Rush / trial / Pinnacle events and the Maps structural boss-bar mode. Ordinary Maps context bypasses OCR and boss-name matching; selected Pinnacle objectives keep the identity/OCR path. Normal console output reports meaningful encounter/completion events, while `--dev-console` retains verbose diagnostics.

## Source repository vs. release assets

Compiled executables are intentionally not committed to Git. GitHub Releases
contain:

- `PoE2AS-v2.2.1-Setup.exe`
- `PoE2AS-v2.2.1.zip`
- `SHA256SUMS.txt`

## Automated GitHub release

`.github\workflows\build-release.yml` runs for lowercase version tags such
as `v2.2.1` and can also be started manually. It builds the Setup UI,
BossWatcher, and optional GameTimeWatcher, assembles the two-folder runtime,
creates the portable ZIP and installer, and generates SHA-256 checksums.

## Local developer release build

Requirements:

- Windows x64
- .NET 10 SDK
- Inno Setup 6
- Internet access for NuGet, Tesseract OCR data, and the Microsoft VC++
  redistributable

From `2 - Support Files`:

```powershell
.\Build-Release.ps1 -Version 2.2.1
```

For development-only user-tool builds:

```powershell
.\Build-Tools.ps1
```

## Mode directories

1. `01-Ordered`
2. `02-Flexible`
3. `03-LevelRace`
4. `04-Checklist`
5. `05-Segment`
6. `06-Boss100-Dyn`
7. `07-Boss100-Pre`
8. `08-BossAny-Dyn`
9. `09-BossReq-Pre`
10. `10-Pinnacle-Dyn`
11. `11-Pinnacle-Pre`
12. `12-Mixed100-Dyn`
13. `13-MixedAny-Dyn`
14. `14-Custom`

Campaign modes that start in The Riverbank arm their start when `G1_1` is
entered, but LiveSplit does not begin timing until Client.txt records the Wounded
Man's final opening line: `Reach... Clearfell... Find the Miller...`. The initial
wake-up/setup period and first NPC interaction are intentionally untimed.
Pinnacle modes 10-11 remain manual-start.

**v0.4.3 manual-pause integration:** after updating, regenerate the setup and reselect the generated `.asl` in LiveSplit. The provisional timestamp protocol requires the matching ASL and GameTimeWatcher build.
