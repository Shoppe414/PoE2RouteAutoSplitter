Path of Exile 2 BossRush v0.2.0
Mode: Pinnacle v0.5 - Dynamic
Targets: 10

Files:
- Pinnacle-v0.5-Dyn.lss
- PoE2-Pinnacle-Dyn.asl

For load-removed timing, use LiveSplit Timing Method = Game Time. Real Time remains available for RTA comparison.
Add a Scriptable Auto Splitter component and point it to the ASL in this folder.
BossWatcher writes poe2_boss_events.log beside LiveSplit; the bridge reads that shared event file.

This Dynamic profile starts with generic Boss XX rows and renames each row when an accepted boss GONE event is processed.

Only boss IDs in this mode whitelist can split the timer. Support-only identities and excluded minibosses cannot create mode splits.

Game Time removes Client.txt-reported loading-screen durations. GameTimeWatcher is only needed if the optional manual-pause removal setting is enabled during setup.
