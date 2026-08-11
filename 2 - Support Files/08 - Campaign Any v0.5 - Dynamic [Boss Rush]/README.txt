Path of Exile 2 BossRush v0.2.0
Mode: Campaign Any% v0.5 - Dynamic
Targets: 40

Files:
- Path of Exile 2 - BossRush - Campaign Any% v0.5 - Dynamic.lss
- PathOfExile2_BossRush_campaign-any-v0.5-dynamic_v0.2.0.asl

Use LiveSplit Timing Method = Real Time.
Timer start: automatic when Path of Exile 2 enters G1_1 (The Riverbank). The ASL reads Client.txt for this start event; BossWatcher remains responsible only for boss events.
You can disable the ASL autoStart setting and start LiveSplit manually if desired.
Add a Scriptable Auto Splitter component and point it to the ASL in this folder.
BossWatcher writes poe2_boss_events.log beside LiveSplit; the bridge reads that shared event file.

This Dynamic profile starts with generic Boss XX rows and renames each row when an accepted boss GONE event is processed.

Only boss IDs in this mode whitelist can split the timer. Support-only identities and excluded minibosses cannot create mode splits.
