Path of Exile 2 BossRush v0.2.0
Mode: Campaign 100% - Predefined
Targets: 67

Files:
- Path of Exile 2 - BossRush - Campaign 100% - Predefined.lss
- PathOfExile2_BossRush_campaign100-predefined_v0.2.0.asl

Use LiveSplit Timing Method = Real Time.
Timer start: automatic when Path of Exile 2 enters G1_1 (The Riverbank). The ASL reads Client.txt for this start event; BossWatcher remains responsible only for boss events.
You can disable the ASL autoStart setting and start LiveSplit manually if desired.
Add a Scriptable Auto Splitter component and point it to the ASL in this folder.
BossWatcher writes poe2_boss_events.log beside LiveSplit; the bridge reads that shared event file.

This Predefined profile contains a fixed canonical boss clear list. Follow the listed order for meaningful per-row timing; use the Dynamic profile if boss order may vary.

Only boss IDs in this mode whitelist can split the timer. Support-only identities and excluded minibosses cannot create mode splits.
