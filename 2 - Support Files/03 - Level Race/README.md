# Option 3 — Level Race

Times a run to a configured character level. The supplied preset is Level 100 with splits every 10 levels.

## Setup

1. Copy `poe2_level_race.txt` beside `LiveSplit.exe`.
2. Load `PathOfExile2_LevelRaceAutosplitter.asl` in one Scriptable Auto Splitter component.
3. Load the matching `.lss`.
4. Enter The Riverbank to auto-start, or disable auto-start and start manually.

`Client.txt` level-up lines matching `... is now level N` trigger configured milestones. The number of LiveSplit segments must equal the number of configured milestone levels.

To create a different target, edit the milestone file and create a matching `.lss`, or run `tools/Generate-Level-Race.ps1`.

This is a new v1.1.0 validation mode; the existing v1.0 campaign modes are unchanged.
