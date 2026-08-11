# Option 4 — Area Checklist / Flexible Act Rush

A generic unordered objective-list engine. It is the easiest way to create Act Rush, Explorer%, flexible Any%, or a custom area checklist without predetermining completion order.

## Config format

Copy one preset to `poe2_area_checklist.txt` beside `LiveSplit.exe`, or edit the supplied default.

```text
@start=G1_1
G1_town
G1_2
G1_4
...
```

`@start=manual` disables automatic start. Every other line is an area objective. Objective order in the file does **not** impose run order.

Load the `.lss` that accompanies the preset. On first completion of an unresolved timed objective, the current generic row is renamed to that area and split. Revisits are ignored. When `@start=G1_1` and Riverbank is included in the checklist, Riverbank is counted as satisfied by the timer-start event and therefore does not require its own zero-time LiveSplit row. All remaining objectives have one row each.

Included presets cover Campaign Any%, Campaign All Areas/Explorer, Act 1–4 Any% and All Areas, each Interlude, and all Interludes together.

This is a new v1.1.0 validation mode.
