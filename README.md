# PoE2 Route AutoSplitter

A setup tool and LiveSplit autosplitter for **Path of Exile 2 campaign speedrunning**.

PoE2 Route AutoSplitter provides premade and custom routes for:

* Exploration / area completion
* Boss Rush
* Combined Exploration + Boss Rush
* Campaign Any%
* Campaign 100%
* Required Campaign Bosses Only
* 0.5 Pinnacle bosses
* Custom user-defined routes

The included **PoE2RouteSetup** application handles most of the setup for you.

Allows for synchronous pausing of the game and LiveSplit timer when opening the pause menu.
Game Time option in LiveSplit will exclude loading times and pause timer (when option is active).

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
This was intended to allow people to take breaks or address situations that may arise that require their full attention.
Other menus will not pause the timer since you have control of your character.

The timer will run during in game cut scenes since you will have access to your inventory during these moments
which can be used for inventory management for run optimization. The timer currently only pauses during loading screens,
the pause menu, and microtransaction shop.

---

# Download

The downloader can be found [Here](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)

OR

Go to the **Releases** section of this GitHub repository and download the latest:

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

For most users, the installer is the recommended method.

A portable ZIP may also be available for users who prefer not to use the installer.
This will require using powershell to run the \Setup-UI[Configuration]\Build.ps1 file
to generate the RouteSetup.exe

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

5. Select the new **Scriptable Auto Splitter** component.

6. Browse to the `.asl` file inside your **LiveSplit Target** folder.

7. Save your layout.

You only need to change this path when you move the generated files or switch to a setup using a different ASL file.

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

Add the objectives you want and arrange them in the desired order.

When finished, generate the setup.

The application will create the custom:

* `.lss`
* `.asl`
* Route configuration

inside **LiveSplit Target**.

Load these files using the same LiveSplit instructions above.

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

# Current Major Version

**PoE2 Route AutoSplitter 2.x**

Version 2 introduced the graphical route setup application and installer-based distribution, allowing normal users to configure the autosplitter without compiling the project or running PowerShell commands.
