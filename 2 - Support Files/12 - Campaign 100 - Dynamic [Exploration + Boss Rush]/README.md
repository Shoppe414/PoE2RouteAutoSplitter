# Mixed - Campaign 100% - Exploration + Boss Rush

This v1.2.0 merged mode combines **Exploration** area completions and **Boss Rush** boss completions in one unordered objective pool.

- Exploration objectives: **98**
- Boss objectives: **67**
- Total LiveSplit rows: **165**
- Start area: `G1_1` (The Riverbank)

Copy `poe2_mixed_route.txt` beside `LiveSplit.exe`, load the supplied `.lss`, and attach **only** `PathOfExile2_MixedObjectiveAutosplitter.asl`. Boss objectives also require the watcher in `BossWatcher [Boss Rush Detection]` to be running.

Rows start as generic `Objective NNN` slots and are renamed to the area or boss actually completed. Objective order is not enforced.

Boss split timestamps retain BossRush's `firstMissing` Real Time backdating; area splits use the native LiveSplit timestamp.
