# PoE2 Route AutoSplitter v2.2.1 — Release Candidate

This release candidate promotes the current validated SetupUI/autosplitter package to v2.2.1 and incorporates the UI and Trial-start fixes completed after the v2.2.0 candidate.

## v2.2.1 highlights

- Fixed dedicated Trial auto-start generation. Trial runs now use the same independent `Client.txt` zone-entry start reader used by Premade and Custom routes, including wildcard area IDs such as `Sanctum_1_*` for Trial of the Sekhemas Floor 1.
- Trial of the Sekhemas auto-start remains gated to the first active `Sanctum_1_*` instance rather than the preceding `G2_13` Trial lobby.
- SetupUI now opens at half of the usable width and the full usable height of the monitor containing the mouse cursor, centered horizontally, instead of launching in the smaller fixed window.
- Custom Route order selection uses explicit **Ordered** and **Dynamic / unordered** radio buttons with a short description of the selected rule.
- Removed the redundant Timer Start explanation above the Custom Route objective preview.
- Corrected the Custom Route Content note so it states that the Trial content selector is **below** the Content selector.
- Shortened the Trial boss objective description to focus on the selectable milestones, Sekhemas Floor 2's Hadi/Rafiq requirement, the Chaos Boss 1/2/3 pool, and Trialmaster.
- Retains the configurable Manual / Riverbank / First Split Zone Entry start policies and the non-Riverbank zone-entry start fixes from the v2.2.0 line.
- Retains Maps structural boss detection, expanded Premade/Custom generation, dedicated Trials runtime, and Game Time/manual-pause handling.

## Runtime scope

The v2.2.1 change is concentrated in SetupUI generation/presentation and release metadata. BossWatcher and GameTimeWatcher component versions are unchanged. The shared ASL/runtime behavior remains the validated v2.2.0 baseline except that dedicated Trial generation now injects the hardened independent zone-start policy into the generated Trial ASL.
