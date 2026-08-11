# v0.2.15

- Loadability hotfix: rewrote the v0.2.14 final-segment resolver using only the segment-walk pattern already proven in v0.2.13.
- Removed retained typed ISegment local references from the finalization path.
- Restored UTF-8 BOM packaging to match the known-working v0.2.13 ASL file.
- Final behavior remains: stamp Cuachic Vault and Ziggurat Refuge at the exact Ziggurat-entry time, then end/pause the timer.

# Changelog

## 0.2.14

- Reworked the Ziggurat Refuge finish as an explicit named-segment commit.
- The script now locates `The Cuachic Vault` and `The Ziggurat Refuge` by LiveSplit segment name instead of assuming their numeric indices match the route file.
- Entering Ziggurat stamps both final cumulative split times to the exact Ziggurat-entry time.
- If Ziggurat is the actual last LiveSplit row, the timer is placed in `Ended`; if the loaded `.lss` has extra rows after it, the timer is paused at the exact finish time.
- Added `runCount`, Cuachic index, and Ziggurat index diagnostics to isolate `.lss`/route mismatches.
- Removed the second forced `TimerModel.Split()` path.

## 0.2.13
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

# v0.2.9

- Fixed the final Ziggurat row failing to receive a timestamp.
- Identified LiveSplit's 300 ms DoubleTapPrevention as the reason the second forced split could be ignored.
- Added a 400 ms guard between the Cuachic and Ziggurat native split requests.
- Captures the exact time of entering The Ziggurat Refuge and reapplies it to both final split rows after the delayed final split, so the route finish time is not inflated by the guard delay.
- Final expected state with the supplied 98-row validation splits: Ziggurat stamped, timer phase Ended.

# v0.2.8

- Reworked the Ziggurat Refuge finish into an explicit two-stage native ASL split sequence.
- Entering The Cuachic Vault arms the finish without stamping the segment.
- Entering The Ziggurat Refuge queues split #1, which timestamps The Cuachic Vault.
- `onSplit` confirms LiveSplit advanced to the Ziggurat segment, then queues split #2 on the next ASL cycle.
- Split #2 timestamps The Ziggurat Refuge and normally places LiveSplit in `Ended` state.
- Pause is now only a fallback when a custom splits file has additional segments after the route.
- Manual LiveSplit index synchronization is suspended while the final two-split transaction is in flight.

# Changelog

## 0.2.7

- Reworked the final-route transaction based on live validation.
- Entering The Ziggurat Refuge is now the explicit completion condition.
- The finish path now executes in strict order: commit The Cuachic Vault split, verify LiveSplit advanced to the Ziggurat row, then pause.
- Removed the prior `onSplit`-based finish pause, which could leave The Cuachic Vault active without committing its timestamp.
- Added `FINISH_COMMIT` / `FINISH_COMMIT_FAILED` diagnostics with before/after LiveSplit indices.

## v0.2.6
- Corrected final-route semantics at The Ziggurat Refuge.
- The penultimate route segment is now held open while the player is inside it.
- Entering The Ziggurat Refuge performs one final split to close the penultimate segment, advances LiveSplit onto the Ziggurat Refuge row, then immediately pauses the timer.
- The Ziggurat Refuge row is intentionally left active/highlighted rather than being split to an Ended state.
- Added finish-state logging: `FINISH_ARMED`, `FINISH_TRANSITION`, and `FINISH_COMPLETE`.
- Manual advancement onto the Ziggurat row is handled with a pause-only fallback.

## v0.2.5
- Fixed v0.2.4 failing to load in Scriptable Auto Splitter.
- Cause: ASL exposes `timer` as `LiveSplitState`; `LiveSplitState` has no `Pause()` method.
- Finish freeze now creates a `LiveSplit.Model.TimerModel` bound to the current `timer` state and calls its normal `Pause()` method.
- Corrected the READY diagnostic version string to v0.2.5.

## 0.2.4

- Added an explicit Ziggurat Refuge finish-freeze fallback.
- The autosplitter still performs the normal final split first.
- If LiveSplit remains `Running` afterward, it calls `timer.Pause()` immediately to freeze the displayed final time.
- If LiveSplit already transitioned to `Ended`, no pause call is needed.
- Added `pauseAtFinish` setting, enabled by default.
- Added `FINISH_FREEZE` and `FINISH_COMPLETE` diagnostics.

## 0.2.3

- Removed Deserted Post from the 100% route and Act 4 practice route.
- Deserted Post is now documented as a non-splittable subarea of Plunder's Point.
- Route validation rejects `ExpeditionSubArea_Kalguur_Act4` if manually added as a split.
- Updated the supplied Campaign 100% validation file from 99 to 98 segments.
- Removed the temporary Deserted Post raw-log diagnostic.
- Ziggurat Refuge remains the final route entry and completing that final split ends the run.

## 0.2.2

- Added explicit final-run handling/logging for The Ziggurat Refuge.
- Added a temporary Deserted Post fallback/diagnostic while investigating its missing area transition.

## 0.2.1

- Synchronize internal route progress with `timer.CurrentSplitIndex`.
- LiveSplit **Skip Segment** now skips the corresponding route entry.
- LiveSplit manual **Split** advances the route entry as well.
- LiveSplit **Undo Split** moves the expected route entry backward.
- Added `syncLiveSplitIndex` setting, enabled by default.
- Added LiveSplit split index to the status file.
- Added `LIVESPLIT_ADVANCE` / `LIVESPLIT_UNDO` diagnostic messages.

## v0.2.0 — campaign validation

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

## v0.1.0 — proof of concept

- First ASL build.
- Client.txt auto-discovery.
- Riverbank auto-start.
- Ordered-route splitting.
- Unexpected-area ignore behavior.
- Basic status/debug logging.

- Added a ready-made 99-segment LiveSplit `.lss` for the Campaign 100% validation route.
- Added `Generate-LiveSplitSplits.ps1` for regenerating `.lss` files after route edits.
- Clear the unknown-area capture on each new ASL attachment/reload for clean validation sessions.
