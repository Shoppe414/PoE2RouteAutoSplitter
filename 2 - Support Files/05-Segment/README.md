# Option 5 — Ordered Segment / Act Practice

A lightweight ordered route engine with a configurable **start area**, intended for Act Rush and practice segments that do not begin in The Riverbank.

Copy a preset to `poe2_segment_route.txt` beside `LiveSplit.exe` and load its matching `.lss`.

```text
@start=G2_1
G2_town
G2_3a
...
G2_12
```

Entering the `@start` area normally starts the timer. Special case: `@start=G1_1` arms the Riverbank start gate and timing begins on the Wounded Man final opening Client.txt line. Thereafter only the next route target can split; out-of-order areas are ignored. The final route target naturally ends the run when the `.lss` row count matches the route.

Included presets cover Act 1–4 Any% and All Areas plus each Interlude.

This is a new v1.1.0 validation mode.
