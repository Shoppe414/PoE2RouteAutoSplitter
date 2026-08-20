# PoE2 Route AutoSplitter Setup UI — v3.0.0 Release Candidate

This is the Windows configuration tool for the Route AutoSplitter package.

## Shared user settings

The action panel now includes **Settings**. It edits `1 - User Setup\PoE2AS-Settings.json`, the supported user-facing configuration shared by SetupUI, BossWatcher, and GameTimeWatcher. SetupUI window size/default diagnostic-console behavior, BossWatcher's boss-disappearance grace, and selected GameTimeWatcher diagnostic thresholds can be adjusted without rebuilding.

Generate / Deploy writes the effective values into `LiveSplit Target\poe2_run_settings.json`. Watchers started from SetupUI read that immutable per-setup snapshot, and `3 - verification files\poe2_setup_validation.sha256` includes it. Hand-editing the master settings after generation therefore does not silently alter an already-generated run. Malformed/out-of-range master settings are backed up and replaced with defaults.

Identity/single-boss and Maps disappearance confirmation both default to **5.5 seconds** in this development build. The grace delays only confirmation; BossWatcher retains `firstMissing` so accepted completion remains backdated to the original disappearance signal. The settings remain independently adjustable.

## SetupUI languages

SetupUI now supports a selectable startup language from **Settings**. Saving a new language applies it immediately to the open SetupUI and saves it as the default for future launches. SetupUI Language and PoE2 Game Language intentionally expose the same current PoE2-supported set: English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai.

The Windows installer asks for the application's default SetupUI language, with English preselected. Existing supported language settings are preserved/preselected during upgrades. SetupUI chrome, controls, help text, and policy descriptions use the selected UI locale. Area and boss display names use verified game-localized proper nouns when available, while canonical English runtime identities/IDs remain unchanged. BossWatcher OCR continues to use the independently selected PoE2 Game Language.

## Normal users

Install the ready-to-run `PoE2AS-v3.0.0-Setup.exe` GitHub Release asset.

The installed launcher is:

`1 - User Setup\PoE2RouteSetup.exe`

and it deploys LiveSplit files into:

`1 - User Setup\LiveSplit Target`

## LiveSplit layouts

The Setup UI does **not** generate, copy, or modify `.lsl` files.

After deployment:

1. open the generated `.lss` from `LiveSplit Target`;
2. keep your own LiveSplit layout;
3. add/edit a **Scriptable Auto Splitter** component; and
4. browse that component to the generated `.asl` in `LiveSplit Target`.

The exact deployed ASL path is also written to `SETUP_INFO.txt`.

The Setup UI also keeps a persistent **LiveSplit reminders** box visible. It
reminds users to attach the generated `.asl` after generation and to switch
LiveSplit to **Game Time** when load-screen and optional manual-pause time should
be excluded. Real Time continues counting those periods.

The previous post-generation success/instructions dialog was removed as
redundant. The target-directory deletion confirmation remains because deployment
replaces the contents of `LiveSplit Target`.

## Custom level progression

The **Custom route** tab can add generated level milestones alongside area and boss objectives. Enable **Add level progression**, then choose **Max level** (2-100) and **Split interval** (1-100). The preview updates immediately. The selected Max Level is always included as the final milestone, even when it is not divisible by the interval (for example, Max 73 / Interval 10 produces 10, 20, 30, 40, 50, 60, 70, 73).

Level milestones may be moved among area and boss objectives using the normal Move Up/Move Down controls. Before generation, Setup UI verifies that the generated level milestones are still present and that the level objectives appear in strictly ascending order. Individual generated level milestones are removed by changing the level settings or disabling **Add level progression**, rather than with the route Remove button.

In an **ordered** custom route, the ASL tracks the highest level observed even when that level is reached while an earlier objective is still active. When a previously reached level milestone later becomes the active LiveSplit row, it is automatically **skipped** using LiveSplit's Skip Split behavior rather than marked completed. This prevents an ordered challenge route from getting stuck on a level event that cannot occur again. In an unordered route, level milestones complete naturally whenever the corresponding level is reached.

LiveSplit's native Skip Split command cannot skip the final segment. For an ordered mixed challenge where a level milestone might be reached early, keep a later objective after that milestone.


## Optional trial boss objectives

The **Custom route** tab keeps Ascension Trial bosses opt-in. Enable **Add trial boss objectives** to reveal the trial checklist; checking a milestone inserts it directly into the route preview. Ordered routes can move those milestones anywhere in the sequence. Unordered routes treat the selected milestones as normal completion objectives. Disabling the option removes its generated trial-boss milestones from the route.

The ordinary Bosses search list excludes trial bosses so they are not included accidentally. The OCR catalog still contains the individual trial identities for BossWatcher. Sekhemas Floor 2 is exposed to the route as one composite **Hadi + Rafiq** milestone and completes only after both bosses are defeated, regardless of kill order.

Chaos uses three generic route milestones — **Chaos Boss 1**, **Chaos Boss 2**, and **Chaos Boss 3** — backed by the restricted Uxmal/Chetza/Bahlak BossWatcher pool. Each milestone accepts whichever pool boss actually appears and the LiveSplit row is renamed to that detected identity. Ordered routes can place the generic slots wherever required; unordered routes consume the next still-open Chaos slot for each qualifying boss defeat. **The Trialmaster** remains a deterministic separate objective.

## Dedicated Trials runs

The **Trials** tab generates a functional `.asl` / `.lss` setup for Trial of the Sekhemas and Trial of Chaos. The implementation intentionally uses a small standard policy surface.

**Active Challenges Only** remains visible but disabled as **NON-FUNCTIONAL — Active development**; it cannot be generated yet. Boss policy defaults to **Each boss kill**. Exit policy defaults to **Trial completion / exit only**. Other available split-frequency choices remain selectable as alternative rules.

Current policies:

- **Start:** fixed automatic first-chamber entry. Sekhemas starts on the first normalized `Sanctum_1_*` entry. Chaos starts on `G3_10`. There is no manual-start choice for dedicated Trials runs.
- **Timing:** **Full Trial** only. Combat, movement, transition paths, room/modifier choices, interactables, and decision time all count. Client.txt loading-screen removal still applies to LiveSplit Game Time, and optional manual-pause removal can still be enabled through GameTimeWatcher.
- **Boss policy (default):** the run ends on the final required boss death for the selected length. BossWatcher is required.
- **Exit policy:** the run ends when Sekhemas returns to `G2_13`, or when Chaos returns to `G3_10_Airlock`. This can run without BossWatcher when **Trial completion / exit only** is selected. If boss-based intermediate splits are selected, BossWatcher is still required for those splits.

Split-frequency choices:

- **Each boss kill (default/recommended for Boss policy):** creates every required boss split for the selected category. A four-floor Sekhemas run therefore has five boss kills because Hadi and Rafiq are separate kills on Floor 2. The two Floor-2 rows accept either kill order and rename to the identity BossWatcher detects. Chaos uses one dynamic Uxmal/Chetza/Bahlak slot per selected boss stage, plus Trialmaster when included.
- **Final boss only (alternative):** one boss split under Boss policy; under Exit policy, the final-boss split is followed by an exit split.
- **Trial completion / exit only (default/recommended for Exit policy):** available with Exit policy; creates one exit/completion split and requires no BossWatcher.

For Chaos **Final boss only** without Trialmaster, the shared ASL uses an Nth-dynamic-boss counter so a 7-round or 10-round category does not incorrectly finish on the first random Chaos boss. Earlier random boss deaths are counted silently until the selected final stage is reached.

Each rule group includes a short description of its current selection. The right-side **Selected trial rules** table remains a compact two-column summary.

## Maps

The **Maps** tab now uses the development **Maps lifecycle policy v2**. Ordinary map identity comes from the authoritative Client.txt generated area ID + seed (`Map<name>|seed`). Scene names are supplemental display text only. **The timer will automatically start when first entering the map. A valid run is from first entry to first exit after the area boss kill.**

Game Time policy:

- **PoE2 Map Completion (default):** after the first exit following the area-boss kill, Game Time pauses between completed maps and resumes on the next new map entry. This mirrors PoE2's map-completion boundary.
- **Continuous Game Time:** all non-loading time after the run starts counts. Only loading-screen exclusion and the configured Manual Pause policy may pause Game Time. Re-entering a completed map or doing side objectives after map completion therefore continues to consume Game Time.

Run endpoints:

- **Fixed number of maps:** 1–100 finalized map instances.
- **Until first death:** requires Character Name and forces End on first death.
- **Manual finish:** Maps still auto-starts on map entry; use LiveSplit Start/Split when the self-selected endpoint is reached.
- **Specific Pinnacle boss defeat:** select one Pinnacle identity; BossWatcher `SEEN` releases setup pause and `GONE` creates the final Pinnacle split.

Death policy:

- **No death tracking (default):** does not parse/store death messages and requires no Character Name.
- **End on first death:** exact tracked-character death creates the final `Death [1]` split.
- **Track deaths:** exact tracked-character deaths insert `Death [x]` LiveSplit rows while Game Time continues.

Character Name accepts Unicode letters plus `_` only. Runtime death matching extracts `<name>` from the exact Client.txt `'<name> has been slain.'` notification and compares it directly to the configured character, so party-member deaths are ignored.

Ordinary map completion is two-stage: BossWatcher `MAP_GONE` **qualifies** the active Map+seed, but LiveSplit does not split until Client.txt confirms the **first exit after the area-boss kill**. A premature exit saves the exit boundary and continues timing provisionally. Same-seed re-entry continues the attempt with no map completion. Entering a different map seed confirms the prior map `FAILED`. Under the default PoE2 Map Completion policy, intervening setup time is removed; under Continuous Game Time, it remains counted.

After successful exit, setup Game Time pauses until the next map entry. Re-entering a previously finalized Map+seed is ignored and remains setup-paused. No maximum-attempt count is hard-coded in this development iteration.

`poe2_boss_context.txt` remains the handoff between the ASL and BossWatcher: `mode=map` uses the identity-free structural ordinary-map tracker, `mode=identity` preserves the OCR/catalog path (including Pinnacle endpoints), and `mode=off` is used after an ordinary map is qualified while waiting for exit.

See the package-root `MAPS-POLICY-TEST-NOTES.md` for the focused test matrix and run-audit events.

## Start policy

The Setup UI requires exactly one mutually exclusive timer-start policy.
**Riverbank Start** is selected by default.

- **Manual Start:** generated ASL never auto-starts LiveSplit.
- **Riverbank Start (default):** use a fresh character. Entering The Riverbank
  arms the start gate and timing begins on the Wounded Man's final opening line.
- **First Split Zone Entry Auto Start:** enables a dropdown containing the game
  area catalog except The Riverbank. Timing begins on a fresh Client.txt entry
  into the selected zone from another zone (for example, Kingsmarch / `G4_town`).

Generation is blocked if a valid start policy is not selected. The zone dropdown
is only enabled for the third option. Runtime files that support `@start=` receive
the selected area ID directly; other modes receive an independent generated
Client.txt start reader, including Boss Rush, Level Race, and Pinnacle setups.

When Riverbank Start is selected, the Act 1 Practice - Ordered preset is deployed
with The Riverbank prepended as its first segment. Other start policies preserve
the preset's original Clearfell Encampment-first route.

## Game Time

Load-removed Game Time is built into every deployed ASL. The ASL tails Path of
Exile 2 `Client.txt`, pauses Game Time during an active zone transition, and
uses the game's own `[LOADING SCREEN] (...) Duration = X seconds` value to
correct the final amount removed.

GameTimeWatcher is **not required** for ordinary load removal.

The Setup UI has an optional setting:

`Pause LiveSplit Game Time while PoE2 is manually paused`

When enabled, the deployed ASL also accepts a fresh pause-state heartbeat from
GameTimeWatcher. Start GameTimeWatcher from the Setup UI before or during the
run. The helper recognizes the actual `GAME PAUSED` menu and the
Microtransaction Shop. Options and Challenges/Achievements remain timed.

If GameTimeWatcher is closed or its state file becomes stale, the ASL fails
open and Game Time continues rather than remaining stuck paused.

## Premade setups

The compact Premade selector uses a fixed full-width layout container so the Mode / Setup / Route order controls remain readable inside the scrollable tab when Windows recalculates preferred control sizes.

The **Premade setups** tab uses a compact generated-route selector instead of the old 41-row preset list. The first dropdown selects **Area Completion**, **Boss Completion**, **Area + Boss Completion**, or **Level Race**. The second dropdown selects the setup scope (Campaign 100% / Any%, individual Acts, Interludes, All Interludes, an Act/Interlude Combination, or Pinnacle where applicable). A binary **Ordered / Dynamic** choice controls completion order; Area + Boss premades currently use Dynamic / unordered because a validated interleaved ordered mixed baseline has not yet been defined.

Combination setups expose a small Act/Interlude checklist. Trial content is not hard-coded into premade routes. Pinnacle is the exception: its established pinnacle target list still includes Zarokh and Trialmaster as category-defining pinnacle encounters. **Include Trial of the Sekhemas** and **Include Trial of Chaos** are independent opt-ins.

Sekhemas exposes sequential Floor 1–4 checkboxes and defaults to **Floor 1 only** when enabled. Every selected floor has its own **Run after** selector on ordered routes. Floor 1 defaults after **The Halani Gates** for area routes or **Jamanra, the Risen King** for boss routes; later floors default to the end of the selected route until the user chooses where they expect to return. If Floors 2 and 3 are planned back-to-back, they may use the same predecessor and the generator preserves Floor 2 → Floor 3 order.

Chaos exposes sequential **4-round**, **7-round**, and **10-round** stages, plus optional Trialmaster for boss routes. The 4-round stage defaults after **Chimeral Wetlands** for area routes or **Xyclucian, the Chimera** for boss routes; later stages default to the end until scheduled by the user. Boss routes use restricted Uxmal/Chetza/Bahlak dynamic slots. Area routes use unique logical area-occurrence objectives so the separate 4/7/10-round visits can all be tracked even though the game reuses the same active Trial of Chaos area. Dynamic routes ignore insertion position and simply add the selected trial stages to the unordered completion pool/count.

SetupUI displays user-facing area/boss names only. Runtime IDs remain in generated diagnostic/config files but are not shown in selection lists, insertion dropdowns, or start-policy text.

Premade generation writes `Premade-Route.lss`, `PoE2-Premade.asl`, `poe2_mixed_route.txt`, and `PREMADE_OBJECTIVES.txt` into the target. It reuses the validated mixed runtime so area, boss, composite Hadi/Rafiq, restricted Chaos dynamic slots, level events, start policy, loading-screen Game Time, and optional manual-pause accounting use one route format. Deployment first builds the complete setup in a temporary staging directory, then asks before replacing a non-empty `LiveSplit Target`.

## Custom route

The **Custom route** tab uses a two-choice **Route order** radio-button selector: **Ordered** or **Dynamic / unordered**. The selected rule is summarized at the bottom of the Custom route panel. Ordered routes advance through objectives in the sequence shown; Dynamic / unordered routes allow eligible objectives to complete in any order.

The tab keeps separate **Areas** and **Bosses** subtabs and filters both catalogs through a shared **Content** dropdown: Act 1-4, Interlude 1-3, or Pinnacle. Only user-facing names are shown; internal IDs remain implementation details. Trial content stays in its separate opt-in selector. After an area or boss is added, it is removed from the available list until that route entry is removed. Both subtabs also provide **Add All**, which adds every currently visible item in the selected Content group/search so a complete list can be built quickly and pruned in the route preview.

For **ordered** routes, enable **Multi-boss / repeated encounters** to expose an **Occurrences** input beside every boss (minimum 1, default 1). Adding a boss with Occurrences 5 creates five separately keyed objectives such as `Count Geonor — Kill 1` through `Kill 5`; BossWatcher may therefore satisfy the same boss identity on five distinct encounters.

For **dynamic / unordered** routes, selected boss names define the eligible boss pool instead of fixed one-time identities. **Boss encounters required** determines the number of qualifying boss defeats needed. Its default is read from the current Campaign Any% required-boss baseline (`campaign-any-v0.5.txt`, currently 40). Repeated defeats of the same eligible boss count as separate encounters, so a one-boss pool with target 5 is a valid five-kill challenge.

Timer start is controlled by the same required three-option start policy used by premade setups.

## Watcher buttons

**Start BossWatcher** launches BossWatcher for Boss Rush, mixed, Trials, and Maps modes and directs its event log to the active `LiveSplit Target`. Maps deployments also pass the active `poe2_boss_context.txt` so ordinary map structural detection and optional Pinnacle OCR detection can coexist in one run.

**Start GameTimeWatcher** is enabled when the optional manual-pause setting is
selected. It writes `poe2_manual_pause_state.txt` into the active target.

**Developer console diagnostics** requests verbose BossWatcher output. For
GameTimeWatcher it instead launches the external crash watchdog, which captures
stdout/stderr, process memory/handle/thread samples, the watcher internal log,
and matching Windows Application crash events under the GameTimeWatcher
`4-README's_and_Diagnostics\Diagnostics` folder, with PNG captures in its `images` subfolder.

## Developer build

Requirements: Windows and .NET 10 SDK.

From this directory:

```powershell
.\Build.ps1
```

To build all user tools without creating an installer:

```powershell
.\..\Build-Tools.ps1
```

To create the distributable installer and portable ZIP:

```powershell
.\..\Build-Release.ps1 -Version 3.0.0
```

> **Manual-pause protocol note:** v0.4.3 GameTimeWatcher requires an ASL generated from the same package. Re-run the Setup UI after updating and re-browse LiveSplit's Scriptable Auto Splitter component to the newly generated `.asl`.


## PoE2 game language

Settings includes a PoE2 **game language** selector independent from the SetupUI display language. Generate / Deploy writes the selected code into `poe2_run_settings.json`, copies the current boss-localization database into `poe2_boss_localizations.json`, and includes its version/SHA-256 in the generated settings snapshot. BossWatcher is launched with that exact localization snapshot. GameTimeWatcher reads the same game-language value but uses structure-first pause detection.
