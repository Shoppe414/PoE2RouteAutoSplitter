# Option 1 — Ordered Route Mode

Use this mode when the campaign route is known before the run.

This mode uses the tested v0.2.15 ordered-route engine unchanged.

## Files

- `PathOfExile2_RouteAutosplitter.asl` — Scriptable Auto Splitter script.
- `poe2_route.txt` — active route definition.
- `Path of Exile 2 - Ordered Campaign 100%.lss` — supplied 100% LiveSplit layout.
- `routes/` — alternate and practice route files.
- `tools/Generate-LiveSplitSplits.ps1` — generates a matching `.lss` after route edits.
- `tools/Test-ClientLog.ps1` — replays a Client.txt log against a route.
- `tools/Summarize-Validation.ps1` — summarizes area-validation output.
- `zones.csv` — known area-ID reference.

## Install

1. Close LiveSplit.
2. Copy `poe2_route.txt` beside `LiveSplit.exe`.
3. Open LiveSplit and load `Path of Exile 2 - Ordered Campaign 100%.lss`, or use your own matching splits file.
4. Add one `Control -> Scriptable Auto Splitter` component.
5. Point it to `PathOfExile2_RouteAutosplitter.asl`.
6. Make sure the Flexible ASL is not also loaded in another Scriptable Auto Splitter component.
7. Leave experimental load removal disabled unless specifically testing it.

## Route behavior

`poe2_route.txt` is authoritative.

Example:

```text
G1_town
G1_2
G1_3
G1_4
G1_5
```

If the player enters `G1_5` while the script is waiting for `G1_3`, Red Vale is ignored. The script continues waiting for Mud Burrow and later splits remain aligned.

The Riverbank is the auto-start trigger and does not consume a route row.

## Create a custom ordered route

Edit `poe2_route.txt` directly, or copy one of the files from `routes/` over it.

After changing the route, create a LiveSplit file with the same number/order of segments:

```powershell
.\tools\Generate-LiveSplitSplits.ps1 `
  -RouteFile '.\poe2_route.txt' `
  -ZonesCsv '.\zones.csv' `
  -OutputFile '.\My PoE2 Route.lss' `
  -CategoryName 'My Route'
```

Reload the ASL after changing `poe2_route.txt`.

## Skip / undo synchronization

The ordered engine follows LiveSplit's active split index. Standard LiveSplit manual **Split**, **Skip Segment**, and **Undo Split** actions can therefore be used to resynchronize or bypass progression-state areas during testing.

This assumes the LiveSplit segment list is one-to-one with the route file.

## Finish

Entering **The Ziggurat Refuge** is the run-completion condition. The final sequence uses the tested v0.2.15 finish implementation.

## Notes

- Deserted Post is not a route entry because it does not emit an independent area transition.
- `campaign-any-percent.txt` is a baseline route file, not a claim of an optimized category route.
- Visible segment names are secondary; internal area IDs in the route file control matching.


## v1.1.0 preset layouts

The stable ordered ASL is unchanged. `layouts/` now includes matching LiveSplit files for Campaign Any% and the existing Act 1–4 practice route files. Act practice still requires manual start when using the v0.2.15 campaign engine; use Option 5 if you want an arbitrary area auto-start trigger.
