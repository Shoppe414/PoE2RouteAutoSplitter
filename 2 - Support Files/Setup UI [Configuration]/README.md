# PoE2 Route AutoSplitter Setup UI — v2.1.2

This is the Windows configuration tool for the Route AutoSplitter package.

## Normal users

Install the ready-to-run `PoE2AS-v2.1.2-Setup.exe` GitHub Release asset.

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

The **Premade setups** tab exposes all 41 bundled `.lss` presets from
Exploration, Boss Rush, mixed Exploration + Boss Rush, and Level Race modes.

Deployment first builds the complete setup in a temporary staging directory,
then asks before replacing a non-empty `LiveSplit Target`.

## Custom route

The **Custom route** tab lets the user search and combine supported areas and
bosses, reorder objectives, and choose ordered/unordered completion. Timer start
is controlled by the same required three-option start policy used by premade setups.

## Watcher buttons

**Start BossWatcher** launches BossWatcher for Boss Rush / mixed modes and
directs its event log to the active `LiveSplit Target`.

**Start GameTimeWatcher** is enabled when the optional manual-pause setting is
selected. It writes `poe2_manual_pause_state.txt` into the active target.

**Developer console diagnostics** requests verbose BossWatcher output. For
GameTimeWatcher it instead launches the external crash watchdog, which captures
stdout/stderr, process memory/handle/thread samples, the watcher internal log,
and matching Windows Application crash events under the GameTimeWatcher
`diagnostics\YYYYMMDD-HHMMSS` folder.

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
.\..\Build-Release.ps1 -Version 2.1.2
```

> **Manual-pause protocol note:** v0.4.3 GameTimeWatcher requires an ASL generated from the same package. Re-run the Setup UI after updating and re-browse LiveSplit's Scriptable Auto Splitter component to the newly generated `.asl`.
