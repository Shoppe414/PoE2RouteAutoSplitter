# Option 2 — Flexible Route Mode

Use this mode when the player's route order should be determined during the run rather than written beforehand.

This mode uses the tested v0.3.0 unordered first-visit engine unchanged.

## Files

- `PathOfExile2_FlexibleAreaAutosplitter.asl` — Scriptable Auto Splitter script.
- `Path of Exile 2 - Flexible Campaign.lss` — flexible LiveSplit layout.
- `poe2_area_groups.txt` — readable list of area groups.
- `tools/Generate-FlexibleSplits.ps1` — flexible layout-generation helper.
- `tools/Test-ClientLog.ps1` — Client.txt replay/testing helper.
- `tools/Summarize-Validation.ps1` — area-validation summary helper.
- `zones.csv` — known area-ID reference.

## Install

1. Open LiveSplit and load `Path of Exile 2 - Flexible Campaign.lss`.
2. Add one `Control -> Scriptable Auto Splitter` component.
3. Point it to `PathOfExile2_FlexibleAreaAutosplitter.asl`.
4. Make sure the Ordered ASL is not also loaded in another Scriptable Auto Splitter component.
5. Open the Scriptable Auto Splitter settings and enable/disable individual areas as desired.
6. Leave experimental load removal disabled unless specifically testing it.

`poe2_route.txt` is not used by this mode. An old copy may remain beside LiveSplit without affecting Flexible mode.

## Area groups

The settings are grouped into:

- Act 1 — 16 areas
- Act 2 — 22 areas
- Act 3 — 19 areas
- Act 4 — 18 areas
- Interludes — 22 areas

Each enabled area is independent. There is no expected next area.

## Flexible split behavior

When the player enters an enabled area for the first time:

```text
Area detected
    ↓
Enabled?
    ↓ yes
Already completed this run?
    ↓ no
Rename current generic LiveSplit row
    ↓
Split
    ↓
Mark area completed
```

For example, all of these are valid without reconfiguring the script:

```text
The Red Vale
→ Mud Burrow
→ The Grelwood
```

or:

```text
Interlude 1 area
→ Interlude 3 area
→ Interlude 2 area
→ another Interlude 1 area
```

Returning to an area already completed during the current attempt is ignored.

## Any% use

This mode is designed for runs where the exact route is discovered or chosen during play.

The supplied `.lss` contains enough generic slots for the full enabled-area pool plus the final Ziggurat row. Each completed area renames the next available generic slot to its actual name.

If an any% run finishes without using every possible flexible slot, unused rows may remain blank. This is expected in the current 1.0 release and does not prevent the Ziggurat finish condition from completing the run.

## PB/comparison behavior

Flexible mode stores completion times in the order areas were completed. Therefore the same LiveSplit row can represent different areas on different attempts if the route changes.

This is appropriate for route-flexibility and overall-run timing, but individual row PB comparisons should not be interpreted as strict area-to-area comparisons when two attempts used different area orders.

## Finish

**The Ziggurat Refuge** is the explicit run-completion condition.

The Cuachic Vault receives special handling when it is the final unresolved area / penultimate LiveSplit slot so the tested campaign-end behavior remains correct.

## Notes

- The Riverbank remains the normal auto-start trigger and is counted as an Act 1 exploration objective at timer start without creating a separate zero-time split.
- Deserted Post is not an independent area split.
- No `poe2_route.txt` file is required.


## Explorer / All Areas preset

With all area checkboxes left enabled, this stable flexible mode is also the Campaign All Areas / Explorer configuration. Option 4 adds file-defined checklists and shorter per-category layouts for Act Rush and custom objective pools.
