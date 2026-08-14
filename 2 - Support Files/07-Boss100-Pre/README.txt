Path of Exile 2 BossRush v0.2.0
Mode: Campaign 100% - Predefined
Targets: 67

Files:
- Boss100-Pre.lss
- PoE2-Boss100-Pre.asl

For load-removed timing, use LiveSplit Timing Method = Game Time. Real Time remains available for RTA comparison.
Timer start: automatic on the Wounded Man's final opening line in G1_1 (The Riverbank): "Reach... Clearfell... Find the Miller...". The wake-up/setup period and first NPC interaction are intentionally untimed. The ASL reads Client.txt for this start event; BossWatcher remains responsible only for boss events.
You can disable the ASL autoStart setting and start LiveSplit manually if desired.
Add a Scriptable Auto Splitter component and point it to the ASL in this folder.
BossWatcher writes poe2_boss_events.log beside LiveSplit; the bridge reads that shared event file.

This Predefined profile contains a fixed canonical boss clear list. Follow the listed order for meaningful per-row timing; use the Dynamic profile if boss order may vary.

Only boss IDs in this mode whitelist can split the timer. Support-only identities and excluded minibosses cannot create mode splits.

Game Time removes Client.txt-reported loading-screen durations. GameTimeWatcher is only needed if the optional manual-pause removal setting is enabled during setup.
