# Mixed - Campaign 100% - Exploration + Boss Rush

This v1.2.1 merged mode combines **Exploration** area completions and **Boss Rush** boss completions in one unordered objective pool.

- Exploration objectives: **99**
- Boss objectives: **67**
- Total LiveSplit rows: **166**
- Start gate: enter `G1_1` (The Riverbank), then start on the Wounded Man final opening Client.txt line

Copy `poe2_mixed_route.txt` beside `LiveSplit.exe`, load the supplied `.lss`, and attach **only** `PoE2-Mixed.asl`. Boss objectives also require the watcher in `BossWatcher` to be running.

Rows start as generic `Objective NNN` slots and are renamed to the area or boss actually completed. Objective order is not enforced.

Boss split timestamps retain BossRush's `firstMissing` backdating for both Real Time and Game Time; area splits use the native LiveSplit timestamp.

## Exploration completion semantics

Campaign exploration objectives use successor-entry completion. An area becomes active when it is entered and is completed only when its configured next progression area is entered. Detours to previously visited areas, towns, hideouts, or other unexpected destinations do not complete the active exploration objective.

## Game Time

Game Time removes Path of Exile 2 `Client.txt` loading-screen durations automatically. GameTimeWatcher is only required if the optional manual-pause removal setting is enabled during setup.
