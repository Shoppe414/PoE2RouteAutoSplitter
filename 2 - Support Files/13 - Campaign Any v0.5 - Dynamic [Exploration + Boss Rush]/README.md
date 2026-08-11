# Mixed - Campaign Any% v0.5 - Exploration + Boss Rush

This v1.2.1 merged mode combines **Exploration** area completions and **Boss Rush** boss completions in one unordered objective pool.

- Exploration objectives: **78**
- Boss objectives: **40**
- Total LiveSplit rows: **118**
- Start area: `G1_1` (The Riverbank)

Copy `poe2_mixed_route.txt` beside `LiveSplit.exe`, load the supplied `.lss`, and attach **only** `PathOfExile2_MixedObjectiveAutosplitter.asl`. Boss objectives also require the watcher in `BossWatcher [Boss Rush Detection]` to be running.

Rows start as generic `Objective NNN` slots and are renamed to the area or boss actually completed. Objective order is not enforced.

Boss split timestamps retain BossRush's `firstMissing` Real Time backdating; area splits use the native LiveSplit timestamp.

## Exploration completion semantics

Campaign exploration objectives use successor-entry completion. An area becomes active when it is entered and is completed only when its configured next progression area is entered. Detours to previously visited areas, towns, hideouts, or other unexpected destinations do not complete the active exploration objective.
