# PoE2 Route AutoSplitter

A setup tool and LiveSplit autosplitter for **Path of Exile 2 campaign speedrunning**.

Current release: **v3.0.0 Release Candidate**.

PoE2 Route AutoSplitter provides premade and custom routes for:

* Exploration / area completion
* Boss Rush
* Combined Exploration + Boss Rush
* Campaign Any%
* Campaign 100%
* Required Campaign Bosses Only
* 0.5 Pinnacle bosses
* Temple of Chaos
* Sekhemas Trials
* Custom user-defined routes
* Maps (Important information here)

The included **PoE2RouteSetup** application handles most of the setup for you.

Allows for synchronous pausing of the game and LiveSplit timer when opening the pause menu.
Game Time option in LiveSplit will exclude loading times and pause timer (when option is active).

Screen shots found here: https://imgur.com/a/VgiRn6o

---
# Run Policies

I've tried my best to be as run agnostic as possible. Players have significant freedom when deciding
how to best manage their run rules and what triggers they want to use.

For the fresh starts on the Riverbank, I've intentionally made the short period between waking up 
and speaking to The Wounded Man un-timed. This is so players have a moment to fix settings, select
'skip tutorial' option, or adjust any other options before actually starting their run. After
interacting with the wounded man, run time will begin on his last voice line.

Zone-Transition-Starts activate as soon as your character enters the pre-defined zone. For dynamic runs,
this will mean the timer only starts and begins tracking when your character enters that specific zone, 
even if you are are starting in a different zone.

Because of the length of the game, I've developed the GameTimeWatcher which is a simple program that
will tell LiveSplit to pause its Game Time while the Pause Game menu and microtransaction menu are open.
This was intended to allow people to take breaks or address situations that may arise that 
require their full attention.

Other menus will not pause the timer since you have control of your character. The timer will run during in
game cut scenes since you will have access to your inventory during these moments which can be 
used for inventory management for run optimization. The timer currently only pauses during loading screens, 
the pause menu, and microtransaction shop.

---

# Download

The downloader can be found [Here](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)

OR

Go to the **Releases** section of this GitHub repository and download the latest:

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

For most users, the installer is the recommended method.

A portable ZIP may also be available for users who prefer not to use the installer.
This will require using powershell to run the 2 - Support Files\Build-tools.ps1 file
to generate the necessary executable files.

---

# Quick Start

## 1. Install PoE2 Route AutoSplitter

Run:

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

Follow the installation prompts.

After installation, open:

**PoE2 Route AutoSplitter**

This launches the route setup application.

---

## 2. Choose Your Route

The Setup application provides a list of premade routes.

Select the route you want to run.

Examples include:

* Campaign Any%
* Campaign 100%
* Required Bosses Only
* Exploration routes
* Boss Rush routes
* Combined Exploration + Boss Rush routes

You can also select **Custom Route** to build your own route.

---

## 3. Generate the LiveSplit Setup

After selecting your route, click the Generate button.

The application will create the required files inside:

`LiveSplit Target` directory

This folder contains the files LiveSplit needs for the selected route.

The contents of **LiveSplit Target** are replaced whenever you generate a new setup.

---

# LiveSplit Setup

Two things need to be configured in LiveSplit:

1. The split file (`.lss`)
2. The Scriptable Auto Splitter (`.asl`)

## Load the Split File

Inside the generated **LiveSplit Target** folder, locate the `.lss` file.

Open it with LiveSplit.

You can also load it manually from LiveSplit using:

**File → Open Splits → From File**

Select the generated `.lss` file.

---

## Add the Scriptable Auto Splitter

The autosplitter script must be added to your LiveSplit layout manually.

In LiveSplit:

1. Right-click LiveSplit.

2. Select **Edit Layout**.

3. Click the **+** button.

4. Select:

   **Control → Scriptable Auto Splitter**

5. Click "Layout Settings"

6. Select the new **Scriptable Auto Splitter** component.

7. Browse to the `.asl` file inside your **LiveSplit Target** folder.

8. Save your layout.

You only need to change this path when you move the generated files or switch to a setup
using a different ASL file.

> PoE2 Route AutoSplitter does **not** generate or replace your LiveSplit layout.

Your layout remains under your control.

---

# Boss Rush Setup

Routes that track bosses use the included **BossWatcher** program.

BossWatcher reads boss names from the game and sends boss events to the autosplitter.

If your selected route requires BossWatcher, use the:

**Start BossWatcher**

button inside PoE2 Route Setup.

A console window will appear.

During normal use, BossWatcher only displays useful boss events such as:

* Boss encountered
* Boss defeated
* Fight duration

Example:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

You do not need to interact with the BossWatcher console while running.

Keep it open during the speedrun.

---

# Exploration Routes

Exploration routes detect when your character enters specific Path of Exile 2 areas.

BossWatcher is **not required** for exploration-only routes.

The autosplitter reads Path of Exile 2's area-transition information automatically.

---

# Combined Exploration + Boss Rush

Combined routes track both:

* Area completion
* Boss defeats

For these routes:

1. Load the generated `.lss`.
2. Point Scriptable Auto Splitter to the generated `.asl`.
3. Start BossWatcher from PoE2 Route Setup.
4. Start your run.

Both area and boss objectives will then be handled by the same route.

---

# Custom Routes

Select **Custom Route** in PoE2 Route Setup to create your own route.

You can include:

* Areas
* Bosses
* Both areas and bosses
* Levels

Add the objectives you want and arrange them in the desired order.

When finished, generate the setup.

The application will create the custom:

* `.lss`
* `.asl`
* Route configuration

inside **LiveSplit Target**.

Load these files using the same LiveSplit instructions above.

---

# Trials

Intended for Trial of the Sekhemas and Temple of Chaos.

Start condition is when you first enter the trial itself. The foyer where you perform setup is not tracked.

There are 2 end conditions:

1. You select how deep into the trial you want to go, and when you kill the boss at the defined depth, 
the trial ends successfully. Failing to complete the trial is considered a failed run and a 
manual restart is needed.

2. Exiting the trial marks it complete. This option is available for those who what to treat exiting the 
trial arena as the end condition. This means that collecting loot, caches, merchant shop, 
and acendency selection will be part of the run.

---

# Vaal Ruins

The foyer is considered a boundry zone for transition reasons. 
This means entering the console room from a map will treat it as exiting the map, 
and not a sub-area of that map.

Vaal ruins are still under development

---

# Maps

Setup of a map is not time while in a hideout or other type of map hub. Upon entry into the map, 
the timer starts automatically, and will split on the first exit after the area boss is defeated. 
If exiting the map before the area boss is defeated, the timer will continue to run. 
This means you can rush to kill the boss, exit the map, re-enter the same map and extra map 
content with a paused timer. (alternative policy below)

Maps runs have several end point definitions:

* Fixed number of map runs
* Until first death (Deathless Run)
* Manual finish
* Defeating a specific Pinnacle Boss

You can also activate death tracking with 3 opitons:
* no death tracking
* First Death Only
* Track Deaths

When selecting either first death or tracked deaths, you will need to input your character's name 
exactly as it appears in game. This is because it reads the client logs to identify your character's death.

There are 2 pausing policies:

* Using the defeat of a boss as the defined map completion event and the split ends upon first exit after
boss defeat. Similar to PoE2's map completion policy.
* Alternative policy: The timer will only pause in loading screens, during a manual pause, or in the
microtransaction menu (if enabled). All other times, including map setup, inventory management, and loot parsing.

**VERY IMPORTANT: Exiting a map, regardless of map state, a snapshot of the time upon exit will be saved.** 
**Even though the GAME TIMER WILL REMAIN RUNNING, setup time after a failed map run attempt WILL NOT be tracked.**
 Upon entry to a new map instance, LiveSplit will be back dated to the time you first exited the original map,
 split, and the timer will start on the new map. Should you choose to manually end the split early, 
 it should also use that backdated time when you first left the failed map.

---

# Switching Routes

To switch to another route:

1. Open PoE2 Route Setup.
2. Select the new route.
3. Generate the setup again.
4. Open the new `.lss` in LiveSplit.
5. Verify that Scriptable Auto Splitter points to the `.asl` inside **LiveSplit Target**.
6. Start BossWatcher if the new route requires boss detection.

The previous contents of **LiveSplit Target** will be replaced.

---

# Starting a Run

Once setup is complete:

1. Open Path of Exile 2.
2. Open LiveSplit.
3. Load your route's `.lss`.
4. Make sure the Scriptable Auto Splitter component is using the correct `.asl`.
5. Start BossWatcher if your route uses bosses.
6. Begin the run.

The autosplitter will handle the configured route objectives automatically.

---

# Updating

When a newer version is released:

1. Download the newest installer from **GitHub Releases**.
2. Run the installer.
3. Open PoE2 Route Setup.
4. Generate your route again.

Your personal LiveSplit layout does not need to be replaced.

---

# Troubleshooting

## Bosses are not splitting

Check that:

* BossWatcher is running.
* You started BossWatcher from PoE2 Route Setup.
* Your selected route actually contains boss objectives.
* LiveSplit's Scriptable Auto Splitter points to the generated `.asl`.

---

## Areas are not splitting

Check that:

* Path of Exile 2 is running.
* LiveSplit's Scriptable Auto Splitter points to the correct `.asl`.
* You generated the correct exploration route.
* The correct `.lss` is loaded.

---

## LiveSplit opens the wrong splits

Open the `.lss` directly from:

`LiveSplit Target`

or use:

**File → Open Splits → From File**

---

## I changed routes and things stopped working

Generate the new route again and verify both:

* The correct `.lss` is loaded.
* Scriptable Auto Splitter points to the current `.asl` inside **LiveSplit Target**.

---

## BossWatcher shows an error

Close BossWatcher and start it again using the **Start BossWatcher** button in PoE2 Route Setup.

If the problem continues, include the displayed error when reporting the issue.

---
## BossWatcher prematurely split or split on death

BossWatcher only records when the boss health bar leaves the screen. This can happen for any number of
reasons. It is up to the user to determine if the reason for the split is accurate or not. The assumption
is the boss died and the split happens. If the split happens without the boss being completed, undoing the split
reverts it to the prior state and you can reattempt the boss from the current time. Split undo hotkey can be found
in LiveSplit settings.

---

# Files Generated for LiveSplit

Depending on the selected route, **LiveSplit Target** may contain:

### `.lss`

The LiveSplit split list.

### `.asl`

The autosplitter script used by LiveSplit's Scriptable Auto Splitter component.

### Route/configuration files

Tell the autosplitter which areas and/or bosses belong to the selected route.

### Boss event files

Used by BossWatcher and boss-enabled autosplitters.

Do not manually edit these files unless you know what you are changing.

For normal use, generate them through **PoE2 Route Setup**.

---

# Important

PoE2 Route AutoSplitter does **not** control or replace your personal LiveSplit layout.

You are responsible for your own:

* Timer appearance
* Split colors
* Fonts
* Window size
* Comparison settings
* Other LiveSplit components

PoE2 Route AutoSplitter only provides the route splits and autosplitter configuration.

---

# Reporting Problems

When reporting an issue, please include:

* PoE2 Route AutoSplitter version
* Route/mode being used
* Whether BossWatcher was running
* What you expected to happen
* What actually happened
* Any error message shown by PoE2 Route Setup, BossWatcher, or LiveSplit

This makes problems significantly easier to reproduce and fix.

---

# Package Verification and Diagnostics

SHA-256 manifests used to verify release/runtime files are stored in:

`3 - verification files`

Run setup-validation manifests, per-run SHA-256 manifests, audit logs, and readable run summaries are also stored
there. They are kept outside `LiveSplit Target` so generating a new route does not delete previous 
per-run audit files.

Verification files can be zipped and included when submitting an official speedrun attempt. 
These files should not be considered authoratative in confirming a valid speed run. 
A video recording along side these files provide a written record of events that happened 
during a speedrun and the settings used for that specific speedrun. 

Verification files should be used in conjunction of video files, not a replacement for video confirmation.

Diagnostic logs from SetupUI, BossWatcher, and GameTimeWatcher are centralized under:

`4-README's_and_Diagnostics\Diagnostics`

Diagnostic PNG captures are stored under:

`4-README's_and_Diagnostics\Diagnostics\images`

---

# Current Major Version

**PoE2 Route AutoSplitter 3.x**

Version 3 adds multilingual SetupUI/game-language support, authoritative localized boss and area display names,
expanded Campaign/Trials/Vaal Ruins/Maps route policies, centralized diagnostics and verification files, and
adaptive height-relative BossWatcher capture geometry for standard, ultrawide, and super-ultrawide game clients.
