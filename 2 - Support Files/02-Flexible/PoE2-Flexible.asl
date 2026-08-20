/*
Path of Exile 2 Flexible Area AutoSplitter for LiveSplit
v0.3.0

Milestone baseline: v0.2.15.

v0.3 architecture:
- No predefined route order for enabled area splits.
- Scriptable Auto Splitter settings are organized into:
    Act 1
    Act 2
    Act 3
    Act 4
    Interludes
- Each enabled area is a first-visit completion item.
- Any enabled, not-yet-completed area may split regardless of which other
  enabled area was completed before it.
- The current LiveSplit slot can be renamed to the area actually completed,
  so the run records the completion order used by the player.
- The Ziggurat Refuge remains an explicit finish condition.
- If The Cuachic Vault is the final unresolved area (or LiveSplit is already
  on the penultimate slot), it is held open and stamped on the transition
  into The Ziggurat Refuge, preserving the working v0.2.15 finish behavior.

Default 100% layout:
- 97 timed unordered area slots
- 1 final The Ziggurat Refuge slot
- The Riverbank is also counted as an Act 1 objective, but its auto-start entry is satisfied implicitly and does not create a zero-time LiveSplit split.

Important:
The supplied 100% Flexible .lss has 98 timed rows. Riverbank is counted separately by the auto-start event, so the default enabled objective total is 99 areas.
If areas are disabled, the script still runs, but unused LiveSplit rows may
remain before the fixed final Ziggurat row. A category-aware layout generator
is a later v0.3 goal.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 20;

    settings.Add("autoStart", true, "Auto-start after the Wounded Man opening dialogue in The Riverbank");
    settings.Add("dynamicSegmentNames", true, "Rename each LiveSplit slot to the area actually completed");
    settings.Add("debugLog", true, "Write PoE2 autosplitter diagnostic log");
    settings.Add("validationLog", true, "Write unique-area validation CSV");
    settings.Add("enteredNameFallback", true, "Fallback: detect known areas from Client.txt 'You have entered ...' messages");
    settings.Add("finishAtZiggurat", true, "Finish the run when entering The Ziggurat Refuge");

    settings.Add("areaSplits", true, "Area split groups");
    settings.SetToolTip("areaSplits", "All enabled areas are first-visit splits. v0.3.0 does not enforce a route order.");
    settings.Add("act1", true, "Act 1", "areaSplits");
    settings.SetToolTip("act1", "Enabled areas in Act 1 are independent first-visit splits; no order is enforced.");
    settings.Add("area_G1_1", true, "The Riverbank", "act1");
    settings.Add("area_G1_town", true, "Clearfell Encampment", "act1");
    settings.Add("area_G1_2", true, "Clearfell", "act1");
    settings.Add("area_G1_3", true, "Mud Burrow", "act1");
    settings.Add("area_G1_4", true, "The Grelwood", "act1");
    settings.Add("area_G1_5", true, "The Red Vale", "act1");
    settings.Add("area_G1_6", true, "The Grim Tangle", "act1");
    settings.Add("area_G1_7", true, "Cemetery of the Eternals", "act1");
    settings.Add("area_G1_9", true, "Tomb of the Consort", "act1");
    settings.Add("area_G1_8", true, "Mausoleum of the Praetor", "act1");
    settings.Add("area_G1_11", true, "Hunting Grounds", "act1");
    settings.Add("area_G1_12", true, "Freythorn", "act1");
    settings.Add("area_G1_13_1", true, "Ogham Farmlands", "act1");
    settings.Add("area_G1_13_2", true, "Ogham Village", "act1");
    settings.Add("area_G1_14", true, "The Manor Ramparts", "act1");
    settings.Add("area_G1_15", true, "Ogham Manor", "act1");
    settings.Add("area_ExpeditionSubArea_Kalguur_Act1", true, "Lost Catacombs", "act1");
    settings.Add("act2", true, "Act 2", "areaSplits");
    settings.SetToolTip("act2", "Enabled areas in Act 2 are independent first-visit splits; no order is enforced.");
    settings.Add("area_G2_1", true, "Vastiri Outskirts", "act2");
    settings.Add("area_G2_town", true, "The Ardura Caravan", "act2");
    settings.Add("area_G2_3a", true, "The Halani Gates (blocked)", "act2");
    settings.Add("area_G2_10_1", true, "Mawdun Quarry", "act2");
    settings.Add("area_G2_10_2", true, "Mawdun Mine", "act2");
    settings.Add("area_G2_2", true, "Traitor's Passage", "act2");
    settings.Add("area_G2_3", true, "The Halani Gates", "act2");
    settings.Add("area_G2_4_1", true, "Keth", "act2");
    settings.Add("area_G2_4_2", true, "The Lost City", "act2");
    settings.Add("area_G2_4_3", true, "Buried Shrines", "act2");
    settings.Add("area_G2_5_1", true, "Mastodon Badlands", "act2");
    settings.Add("area_Abyss_Intro", true, "Lightless Passage", "act2");
    settings.Add("area_Abyss_Hub", true, "The Well of Souls", "act2");
    settings.Add("area_G2_5_2", true, "The Bone Pits", "act2");
    settings.Add("area_G2_6", true, "Valley of the Titans", "act2");
    settings.Add("area_G2_7", true, "The Titan Grotto", "act2");
    settings.Add("area_ExpeditionSubArea_Kalguur_Act2", true, "Skull of the Titan", "act2");
    settings.Add("area_G2_8", true, "Deshar", "act2");
    settings.Add("area_G2_9_1", true, "Path of Mourning", "act2");
    settings.Add("area_G2_9_2", true, "The Spires of Deshar", "act2");
    settings.Add("area_G2_13", true, "Trial of the Sekhemas", "act2");
    settings.Add("area_G2_12", true, "Dreadnought", "act2");
    settings.Add("act3", true, "Act 3", "areaSplits");
    settings.SetToolTip("act3", "Enabled areas in Act 3 are independent first-visit splits; no order is enforced.");
    settings.Add("area_G3_1", true, "Sandswept Marsh", "act3");
    settings.Add("area_G3_town", true, "Ziggurat Encampment", "act3");
    settings.Add("area_G3_3", true, "Jungle Ruins", "act3");
    settings.Add("area_G3_4", true, "The Venom Crypts", "act3");
    settings.Add("area_G3_2_1", true, "Infested Barrens", "act3");
    settings.Add("area_ExpeditionSubArea_Kalguur_Act3", true, "Mystic Refuge", "act3");
    settings.Add("area_G3_5", true, "Chimeral Wetlands", "act3");
    settings.Add("area_G3_6_1", true, "Jiquani's Machinarium", "act3");
    settings.Add("area_G3_6_2", true, "Jiquani's Sanctum", "act3");
    settings.Add("area_G3_2_2", true, "The Matlan Waterways", "act3");
    settings.Add("area_G3_7", true, "The Azak Bog", "act3");
    settings.Add("area_G3_8", true, "The Drowned City", "act3");
    settings.Add("area_G3_9", true, "The Molten Vault", "act3");
    settings.Add("area_G3_11", true, "Apex of Filth", "act3");
    settings.Add("area_G3_12", true, "Temple of Kopec", "act3");
    settings.Add("area_G3_10_Airlock", true, "The Temple of Chaos", "act3");
    settings.Add("area_G3_14", true, "Utzaal", "act3");
    settings.Add("area_G3_16", true, "Aggorat", "act3");
    settings.Add("area_G3_17", true, "The Black Chambers", "act3");
    settings.Add("act4", true, "Act 4", "areaSplits");
    settings.SetToolTip("act4", "Enabled areas in Act 4 are independent first-visit splits; no order is enforced.");
    settings.Add("area_G4_town", true, "Kingsmarch", "act4");
    settings.Add("area_G4_1_1", true, "Isle of Kin", "act4");
    settings.Add("area_G4_1_2", true, "Volcanic Warrens", "act4");
    settings.Add("area_G4_4_1", true, "Eye of Hinekora", "act4");
    settings.Add("area_G4_4_2", true, "Halls of the Dead", "act4");
    settings.Add("area_G4_4_3", true, "Trial of the Ancestors", "act4");
    settings.Add("area_G4_2_1", true, "Kedge Bay", "act4");
    settings.Add("area_G4_2_2", true, "Journey's End", "act4");
    settings.Add("area_G4_5_1", true, "Abandoned Prison", "act4");
    settings.Add("area_G4_5_2", true, "Solitary Confinement", "act4");
    settings.Add("area_G4_3_1", true, "Whakapanu Island", "act4");
    settings.Add("area_G4_3_2", true, "Singing Caverns", "act4");
    settings.Add("area_G4_7", true, "Shrike Island", "act4");
    settings.Add("area_G4_8b", true, "Arastas", "act4");
    settings.Add("area_G4_10", true, "The Excavation", "act4");
    settings.Add("area_G4_11_1b", true, "Ngakanu", "act4");
    settings.Add("area_G4_11_2", true, "Heart of the Tribe", "act4");
    settings.Add("area_G4_13", true, "Plunder's Point", "act4");
    settings.Add("interludes", true, "Interludes", "areaSplits");
    settings.SetToolTip("interludes", "Enabled areas in Interludes are independent first-visit splits; no order is enforced.");
    settings.Add("area_P1_Town", true, "The Refuge", "interludes");
    settings.Add("area_P1_1", true, "Scorched Farmlands", "interludes");
    settings.Add("area_P1_2", true, "Stones of Serle", "interludes");
    settings.Add("area_P1_3", true, "The Blackwood", "interludes");
    settings.Add("area_P1_4", true, "Holten", "interludes");
    settings.Add("area_P1_5", true, "Wolvenhold", "interludes");
    settings.Add("area_P1_6", true, "Holten Estate", "interludes");
    settings.Add("area_P2_Town", true, "The Khari Bazaar", "interludes");
    settings.Add("area_P2_1", true, "The Khari Crossing", "interludes");
    settings.Add("area_P2_2", true, "Pools of Khatal", "interludes");
    settings.Add("area_P2_3", true, "Sel Khari Sanctuary", "interludes");
    settings.Add("area_P2_5", true, "The Galai Gates", "interludes");
    settings.Add("area_P2_6", true, "Qimah", "interludes");
    settings.Add("area_P2_7", true, "Qimah Reservoir", "interludes");
    settings.Add("area_P3_Town", true, "The Glade", "interludes");
    settings.Add("area_P3_1", true, "Ashen Forest", "interludes");
    settings.Add("area_P3_2", true, "Kriar Village", "interludes");
    settings.Add("area_P3_3", true, "Glacial Tarn", "interludes");
    settings.Add("area_P3_4", true, "Howling Caves", "interludes");
    settings.Add("area_P3_5", true, "Kriar Peaks", "interludes");
    settings.Add("area_P3_6", true, "Etched Ravine", "interludes");
    settings.Add("area_P3_7", true, "The Cuachic Vault", "interludes");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_autosplitter_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_autosplitter_debug.log");
    vars.validationPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_area_validation.csv");
    vars.unknownPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_unknown_areas.log");

    vars.reader = null;
    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "WAITING";

    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.pendingSubgroup = "";
    vars.pendingSplitIndex = -1;

    vars.enabledAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.splitHistory = new System.Collections.Generic.List<string>();

    vars.subgroupTotals = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    vars.subgroupCompleted = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    vars.subgroupDisplay = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.subgroupDisplay["act1"] = "Act 1";
    vars.subgroupDisplay["act2"] = "Act 2";
    vars.subgroupDisplay["act3"] = "Act 3";
    vars.subgroupDisplay["act4"] = "Act 4";
    vars.subgroupDisplay["interludes"] = "Interludes";

    vars.finishArmed = false;
    vars.finishCuachicHeld = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;

    vars.runSegmentCount = 0;
    vars.zigguratSegmentIndex = -1;
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();

    vars.seenAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.unknownAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    // Canonical area identity comes from the quoted internal ID. Prefer the observed
    // generation-message token family 2caa* so localized surrounding text does not matter.
    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+)(?=.*(?:\\s2caa[0-9A-Fa-f]{4}\\s|Generating level))(?=.*\\[DEBUG\\s+[^\\]]+\\]\\s+.*?(\\d{1,3})\\D+\"([A-Za-z][A-Za-z0-9_]*)\")",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    vars.loadStartRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+)(?=.*(?:\\s2d8e[0-9A-Fa-f]{4}\\s|Got Instance Details))",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    vars.enteredNameRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*: You have entered (.+)\\.$",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );

    vars.areaNames = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaNames["G1_1"] = "The Riverbank";
    vars.areaNames["G1_town"] = "Clearfell Encampment";
    vars.areaNames["G1_2"] = "Clearfell";
    vars.areaNames["G1_3"] = "Mud Burrow";
    vars.areaNames["G1_4"] = "The Grelwood";
    vars.areaNames["G1_5"] = "The Red Vale";
    vars.areaNames["G1_6"] = "The Grim Tangle";
    vars.areaNames["G1_7"] = "Cemetery of the Eternals";
    vars.areaNames["G1_9"] = "Tomb of the Consort";
    vars.areaNames["G1_8"] = "Mausoleum of the Praetor";
    vars.areaNames["G1_11"] = "Hunting Grounds";
    vars.areaNames["G1_12"] = "Freythorn";
    vars.areaNames["G1_13_1"] = "Ogham Farmlands";
    vars.areaNames["G1_13_2"] = "Ogham Village";
    vars.areaNames["G1_14"] = "The Manor Ramparts";
    vars.areaNames["G1_15"] = "Ogham Manor";
    vars.areaNames["ExpeditionSubArea_Kalguur_Act1"] = "Lost Catacombs";
    vars.areaNames["G2_1"] = "Vastiri Outskirts";
    vars.areaNames["G2_town"] = "The Ardura Caravan";
    vars.areaNames["G2_3a"] = "The Halani Gates (blocked)";
    vars.areaNames["G2_10_1"] = "Mawdun Quarry";
    vars.areaNames["G2_10_2"] = "Mawdun Mine";
    vars.areaNames["G2_2"] = "Traitor's Passage";
    vars.areaNames["G2_3"] = "The Halani Gates";
    vars.areaNames["G2_4_1"] = "Keth";
    vars.areaNames["G2_4_2"] = "The Lost City";
    vars.areaNames["G2_4_3"] = "Buried Shrines";
    vars.areaNames["G2_5_1"] = "Mastodon Badlands";
    vars.areaNames["Abyss_Intro"] = "Lightless Passage";
    vars.areaNames["Abyss_Hub"] = "The Well of Souls";
    vars.areaNames["G2_5_2"] = "The Bone Pits";
    vars.areaNames["G2_6"] = "Valley of the Titans";
    vars.areaNames["G2_7"] = "The Titan Grotto";
    vars.areaNames["ExpeditionSubArea_Kalguur_Act2"] = "Skull of the Titan";
    vars.areaNames["G2_8"] = "Deshar";
    vars.areaNames["G2_9_1"] = "Path of Mourning";
    vars.areaNames["G2_9_2"] = "The Spires of Deshar";
    vars.areaNames["G2_13"] = "Trial of the Sekhemas";
    vars.areaNames["G2_12"] = "Dreadnought";
    vars.areaNames["G3_1"] = "Sandswept Marsh";
    vars.areaNames["G3_town"] = "Ziggurat Encampment";
    vars.areaNames["G3_3"] = "Jungle Ruins";
    vars.areaNames["G3_4"] = "The Venom Crypts";
    vars.areaNames["G3_2_1"] = "Infested Barrens";
    vars.areaNames["ExpeditionSubArea_Kalguur_Act3"] = "Mystic Refuge";
    vars.areaNames["G3_5"] = "Chimeral Wetlands";
    vars.areaNames["G3_6_1"] = "Jiquani's Machinarium";
    vars.areaNames["G3_6_2"] = "Jiquani's Sanctum";
    vars.areaNames["G3_2_2"] = "The Matlan Waterways";
    vars.areaNames["G3_7"] = "The Azak Bog";
    vars.areaNames["G3_8"] = "The Drowned City";
    vars.areaNames["G3_9"] = "The Molten Vault";
    vars.areaNames["G3_11"] = "Apex of Filth";
    vars.areaNames["G3_12"] = "Temple of Kopec";
    vars.areaNames["G3_10_Airlock"] = "The Temple of Chaos";
    vars.areaNames["G3_14"] = "Utzaal";
    vars.areaNames["G3_16"] = "Aggorat";
    vars.areaNames["G3_17"] = "The Black Chambers";
    vars.areaNames["G4_town"] = "Kingsmarch";
    vars.areaNames["G4_1_1"] = "Isle of Kin";
    vars.areaNames["G4_1_2"] = "Volcanic Warrens";
    vars.areaNames["G4_4_1"] = "Eye of Hinekora";
    vars.areaNames["G4_4_2"] = "Halls of the Dead";
    vars.areaNames["G4_4_3"] = "Trial of the Ancestors";
    vars.areaNames["G4_2_1"] = "Kedge Bay";
    vars.areaNames["G4_2_2"] = "Journey's End";
    vars.areaNames["G4_5_1"] = "Abandoned Prison";
    vars.areaNames["G4_5_2"] = "Solitary Confinement";
    vars.areaNames["G4_3_1"] = "Whakapanu Island";
    vars.areaNames["G4_3_2"] = "Singing Caverns";
    vars.areaNames["G4_7"] = "Shrike Island";
    vars.areaNames["G4_8b"] = "Arastas";
    vars.areaNames["G4_10"] = "The Excavation";
    vars.areaNames["G4_11_1b"] = "Ngakanu";
    vars.areaNames["G4_11_2"] = "Heart of the Tribe";
    vars.areaNames["G4_13"] = "Plunder's Point";
    vars.areaNames["ExpeditionSubArea_Kalguur_Act4"] = "Deserted Post";
    vars.areaNames["P1_Town"] = "The Refuge";
    vars.areaNames["P1_1"] = "Scorched Farmlands";
    vars.areaNames["P1_2"] = "Stones of Serle";
    vars.areaNames["P1_3"] = "The Blackwood";
    vars.areaNames["P1_4"] = "Holten";
    vars.areaNames["P1_5"] = "Wolvenhold";
    vars.areaNames["P1_6"] = "Holten Estate";
    vars.areaNames["P2_Town"] = "The Khari Bazaar";
    vars.areaNames["P2_1"] = "The Khari Crossing";
    vars.areaNames["P2_2"] = "Pools of Khatal";
    vars.areaNames["P2_3"] = "Sel Khari Sanctuary";
    vars.areaNames["P2_5"] = "The Galai Gates";
    vars.areaNames["P2_6"] = "Qimah";
    vars.areaNames["P2_7"] = "Qimah Reservoir";
    vars.areaNames["P3_Town"] = "The Glade";
    vars.areaNames["P3_1"] = "Ashen Forest";
    vars.areaNames["P3_2"] = "Kriar Village";
    vars.areaNames["P3_3"] = "Glacial Tarn";
    vars.areaNames["P3_4"] = "Howling Caves";
    vars.areaNames["P3_5"] = "Kriar Peaks";
    vars.areaNames["P3_6"] = "Etched Ravine";
    vars.areaNames["P3_7"] = "The Cuachic Vault";
    vars.areaNames["G_Endgame_Town"] = "The Ziggurat Refuge";
    vars.areaNames["G4_8a"] = "Arastas (pre-progression state)";
    vars.areaNames["G4_11_1a"] = "Ngakanu (pre-progression state)";

    vars.areaSubgroup = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaSubgroup["G1_1"] = "act1";
    vars.areaSubgroup["G1_town"] = "act1";
    vars.areaSubgroup["G1_2"] = "act1";
    vars.areaSubgroup["G1_3"] = "act1";
    vars.areaSubgroup["G1_4"] = "act1";
    vars.areaSubgroup["G1_5"] = "act1";
    vars.areaSubgroup["G1_6"] = "act1";
    vars.areaSubgroup["G1_7"] = "act1";
    vars.areaSubgroup["G1_9"] = "act1";
    vars.areaSubgroup["G1_8"] = "act1";
    vars.areaSubgroup["G1_11"] = "act1";
    vars.areaSubgroup["G1_12"] = "act1";
    vars.areaSubgroup["G1_13_1"] = "act1";
    vars.areaSubgroup["G1_13_2"] = "act1";
    vars.areaSubgroup["G1_14"] = "act1";
    vars.areaSubgroup["G1_15"] = "act1";
    vars.areaSubgroup["ExpeditionSubArea_Kalguur_Act1"] = "act1";
    vars.areaSubgroup["G2_1"] = "act2";
    vars.areaSubgroup["G2_town"] = "act2";
    vars.areaSubgroup["G2_3a"] = "act2";
    vars.areaSubgroup["G2_10_1"] = "act2";
    vars.areaSubgroup["G2_10_2"] = "act2";
    vars.areaSubgroup["G2_2"] = "act2";
    vars.areaSubgroup["G2_3"] = "act2";
    vars.areaSubgroup["G2_4_1"] = "act2";
    vars.areaSubgroup["G2_4_2"] = "act2";
    vars.areaSubgroup["G2_4_3"] = "act2";
    vars.areaSubgroup["G2_5_1"] = "act2";
    vars.areaSubgroup["Abyss_Intro"] = "act2";
    vars.areaSubgroup["Abyss_Hub"] = "act2";
    vars.areaSubgroup["G2_5_2"] = "act2";
    vars.areaSubgroup["G2_6"] = "act2";
    vars.areaSubgroup["G2_7"] = "act2";
    vars.areaSubgroup["ExpeditionSubArea_Kalguur_Act2"] = "act2";
    vars.areaSubgroup["G2_8"] = "act2";
    vars.areaSubgroup["G2_9_1"] = "act2";
    vars.areaSubgroup["G2_9_2"] = "act2";
    vars.areaSubgroup["G2_13"] = "act2";
    vars.areaSubgroup["G2_12"] = "act2";
    vars.areaSubgroup["G3_1"] = "act3";
    vars.areaSubgroup["G3_town"] = "act3";
    vars.areaSubgroup["G3_3"] = "act3";
    vars.areaSubgroup["G3_4"] = "act3";
    vars.areaSubgroup["G3_2_1"] = "act3";
    vars.areaSubgroup["ExpeditionSubArea_Kalguur_Act3"] = "act3";
    vars.areaSubgroup["G3_5"] = "act3";
    vars.areaSubgroup["G3_6_1"] = "act3";
    vars.areaSubgroup["G3_6_2"] = "act3";
    vars.areaSubgroup["G3_2_2"] = "act3";
    vars.areaSubgroup["G3_7"] = "act3";
    vars.areaSubgroup["G3_8"] = "act3";
    vars.areaSubgroup["G3_9"] = "act3";
    vars.areaSubgroup["G3_11"] = "act3";
    vars.areaSubgroup["G3_12"] = "act3";
    vars.areaSubgroup["G3_10_Airlock"] = "act3";
    vars.areaSubgroup["G3_14"] = "act3";
    vars.areaSubgroup["G3_16"] = "act3";
    vars.areaSubgroup["G3_17"] = "act3";
    vars.areaSubgroup["G4_town"] = "act4";
    vars.areaSubgroup["G4_1_1"] = "act4";
    vars.areaSubgroup["G4_1_2"] = "act4";
    vars.areaSubgroup["G4_4_1"] = "act4";
    vars.areaSubgroup["G4_4_2"] = "act4";
    vars.areaSubgroup["G4_4_3"] = "act4";
    vars.areaSubgroup["G4_2_1"] = "act4";
    vars.areaSubgroup["G4_2_2"] = "act4";
    vars.areaSubgroup["G4_5_1"] = "act4";
    vars.areaSubgroup["G4_5_2"] = "act4";
    vars.areaSubgroup["G4_3_1"] = "act4";
    vars.areaSubgroup["G4_3_2"] = "act4";
    vars.areaSubgroup["G4_7"] = "act4";
    vars.areaSubgroup["G4_8b"] = "act4";
    vars.areaSubgroup["G4_10"] = "act4";
    vars.areaSubgroup["G4_11_1b"] = "act4";
    vars.areaSubgroup["G4_11_2"] = "act4";
    vars.areaSubgroup["G4_13"] = "act4";
    vars.areaSubgroup["P1_Town"] = "interludes";
    vars.areaSubgroup["P1_1"] = "interludes";
    vars.areaSubgroup["P1_2"] = "interludes";
    vars.areaSubgroup["P1_3"] = "interludes";
    vars.areaSubgroup["P1_4"] = "interludes";
    vars.areaSubgroup["P1_5"] = "interludes";
    vars.areaSubgroup["P1_6"] = "interludes";
    vars.areaSubgroup["P2_Town"] = "interludes";
    vars.areaSubgroup["P2_1"] = "interludes";
    vars.areaSubgroup["P2_2"] = "interludes";
    vars.areaSubgroup["P2_3"] = "interludes";
    vars.areaSubgroup["P2_5"] = "interludes";
    vars.areaSubgroup["P2_6"] = "interludes";
    vars.areaSubgroup["P2_7"] = "interludes";
    vars.areaSubgroup["P3_Town"] = "interludes";
    vars.areaSubgroup["P3_1"] = "interludes";
    vars.areaSubgroup["P3_2"] = "interludes";
    vars.areaSubgroup["P3_3"] = "interludes";
    vars.areaSubgroup["P3_4"] = "interludes";
    vars.areaSubgroup["P3_5"] = "interludes";
    vars.areaSubgroup["P3_6"] = "interludes";
    vars.areaSubgroup["P3_7"] = "interludes";

    vars.areaIdsByName = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    foreach (var pair in vars.areaNames)
    {
        if (!vars.areaIdsByName.ContainsKey(pair.Value))
            vars.areaIdsByName[pair.Value] = pair.Key;
    }


    // Game Time support. Client.txt is authoritative for completed loading-screen duration.
    // GameTimeWatcher is optional and only supplies the current manual-pause state.
    settings.Add("gameTimeLoads", true, "Game Time: remove Client.txt-reported loading screens");
    settings.Add("manualPauseRemoval", false, "Game Time: pause with PoE2 pause menu / MTX shop (requires GameTimeWatcher)"); // SETUP_MANUAL_PAUSE_DEFAULT
    vars.gtManualPauseStatePath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_manual_pause_state.txt");
    vars.gtDebugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_gametime_debug.log");
    vars.gtReader = null;
    vars.gtClientPath = "";
    vars.gtLoadActive = false;
    vars.gtLoadObservedExclusiveSeconds = 0.0;
    vars.gtLoadManualOverlapSeconds = 0.0;
    vars.gtLoadSampleUtc = System.DateTime.MinValue;
    vars.gtPendingCorrection = System.TimeSpan.Zero;
    vars.gtCorrectionPending = false;
    vars.gtManualPendingCorrection = System.TimeSpan.Zero;
    vars.gtManualCorrectionPending = false;
    vars.gtManualPauseActive = false;
    vars.gtManualPauseFresh = false;
    vars.gtManualWireState = "RUNNING";
    vars.gtManualPendingKind = "";
    vars.gtManualPendingObservedUtc = System.DateTime.MinValue;
    vars.gtManualPendingOriginUtc = System.DateTime.MinValue;
    vars.gtManualStateSequence = 0L;
    // Last successfully parsed, heartbeat-fresh watcher state.  A transient
    // state-file read/parse race may reuse this snapshot for at most 500 ms,
    // but never beyond the normal 2-second heartbeat freshness limit.
    vars.gtManualLastGoodReadUtc = System.DateTime.MinValue;
    vars.gtManualLastGoodWireState = "RUNNING";
    vars.gtManualLastGoodHeartbeatTicks = 0L;
    vars.gtManualLastGoodOriginTicks = 0L;
    vars.gtManualLastGoodStateSequence = 0L;
    vars.gtManualReadGraceActive = false;
    vars.gtNextPausePollUtc = System.DateTime.MinValue;
    // Prefer the Client.txt load-start message-site family (2d8e*). The English text
    // remains a compatibility fallback for older/current logs, but load detection no longer
    // depends on the human-readable sentence being English.
    vars.gtLoadStartRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+)(?=.*(?:\\s2d8e[0-9A-Fa-f]{4}\\s|Got Instance Details))",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    // The 4cba* message-site family identifies loading-screen completion. Capture the
    // parenthesized display name only for diagnostics and the trailing numeric duration for
    // timing; translated labels such as "LOADING SCREEN", "Duration", or "seconds" are not required.
    vars.gtLoadEndRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+)(?=.*(?:\\s4cba[0-9A-Fa-f]{4}\\s|\\[LOADING SCREEN\\])).*?\\((.*?)\\).*?([0-9]+(?:\\.[0-9]+)?)\\D*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
}

init
{

    // Independent Client.txt tail for Game Time. It never consumes the route/boss reader.
    try { if (vars.gtReader != null) vars.gtReader.Close(); } catch {}
    vars.gtReader = null;
    vars.gtClientPath = "";
    vars.gtLoadActive = false;
    vars.gtLoadObservedExclusiveSeconds = 0.0;
    vars.gtLoadManualOverlapSeconds = 0.0;
    vars.gtLoadSampleUtc = System.DateTime.MinValue;
    vars.gtPendingCorrection = System.TimeSpan.Zero;
    vars.gtCorrectionPending = false;
    vars.gtManualPendingCorrection = System.TimeSpan.Zero;
    vars.gtManualCorrectionPending = false;
    vars.gtManualPauseActive = false;
    vars.gtManualPauseFresh = false;
    vars.gtManualWireState = "RUNNING";
    vars.gtManualPendingKind = "";
    vars.gtManualPendingObservedUtc = System.DateTime.MinValue;
    vars.gtManualPendingOriginUtc = System.DateTime.MinValue;
    vars.gtManualStateSequence = 0L;
    // Last successfully parsed, heartbeat-fresh watcher state.  A transient
    // state-file read/parse race may reuse this snapshot for at most 500 ms,
    // but never beyond the normal 2-second heartbeat freshness limit.
    vars.gtManualLastGoodReadUtc = System.DateTime.MinValue;
    vars.gtManualLastGoodWireState = "RUNNING";
    vars.gtManualLastGoodHeartbeatTicks = 0L;
    vars.gtManualLastGoodOriginTicks = 0L;
    vars.gtManualLastGoodStateSequence = 0L;
    vars.gtManualReadGraceActive = false;
    vars.gtNextPausePollUtc = System.DateTime.MinValue;
    try
    {
        string gtGameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.gtClientPath = System.IO.Path.Combine(gtGameDir, "logs", "Client.txt");
        var gtFs = new System.IO.FileStream(vars.gtClientPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        gtFs.Seek(0, System.IO.SeekOrigin.End);
        vars.gtReader = new System.IO.StreamReader(gtFs);
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.gtDebugPath,
                System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " GT_READY | Client.txt=" + vars.gtClientPath + " | manualPauseProtocol=v2.1-provisional-accounting | pollMs=25 | readGraceMs=500" + System.Environment.NewLine);
    }
    catch (System.Exception gtEx)
    {
        vars.gtReader = null;
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.gtDebugPath,
                System.DateTime.Now.ToString("s") + " GT_CLIENT_ERROR | " + gtEx.Message + System.Environment.NewLine);
    }

    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "INITIALIZING";

    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.pendingSubgroup = "";
    vars.pendingSplitIndex = -1;

    vars.enabledAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.splitHistory = new System.Collections.Generic.List<string>();

    vars.subgroupTotals = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    vars.subgroupCompleted = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    vars.subgroupTotals["act1"] = 0;
    vars.subgroupTotals["act2"] = 0;
    vars.subgroupTotals["act3"] = 0;
    vars.subgroupTotals["act4"] = 0;
    vars.subgroupTotals["interludes"] = 0;
    vars.subgroupCompleted["act1"] = 0;
    vars.subgroupCompleted["act2"] = 0;
    vars.subgroupCompleted["act3"] = 0;
    vars.subgroupCompleted["act4"] = 0;
    vars.subgroupCompleted["interludes"] = 0;

    foreach (var pair in vars.areaSubgroup)
    {
        string settingId = "area_" + pair.Key;
        if (settings[settingId])
        {
            vars.enabledAreaIds.Add(pair.Key);
            vars.subgroupTotals[pair.Value] = vars.subgroupTotals[pair.Value] + 1;
        }
    }

    vars.finishArmed = false;
    vars.finishCuachicHeld = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;

    vars.seenAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.unknownAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    vars.runSegmentCount = 0;
    vars.zigguratSegmentIndex = -1;
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();

    foreach (LiveSplit.Model.ISegment initSegment in timer.Run)
    {
        vars.baseSegmentNames.Add(initSegment.Name);
        if (initSegment.Name == "The Ziggurat Refuge")
            vars.zigguratSegmentIndex = vars.runSegmentCount;
        vars.runSegmentCount++;
    }

    try
    {
        string gameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.clientLogPath = System.IO.Path.Combine(gameDir, "logs", "Client.txt");

        var fs = new System.IO.FileStream(
            vars.clientLogPath,
            System.IO.FileMode.Open,
            System.IO.FileAccess.Read,
            System.IO.FileShare.ReadWrite
        );
        fs.Seek(0, System.IO.SeekOrigin.End);
        vars.reader = new System.IO.StreamReader(fs);

        if (settings["validationLog"])
            System.IO.File.WriteAllText(vars.validationPath,
                "Timestamp,AreaId,AreaName,GeneratedLevel,Known,Enabled,Subgroup,CompletedAtDetection,DetectionSource" + System.Environment.NewLine);

        System.IO.File.WriteAllText(vars.unknownPath, "");

        string ready = "READY | v0.3.0"
            + " | Mode=UNORDERED_FIRST_VISIT"
            + " | Process=" + game.ProcessName
            + " | Client.txt=" + vars.clientLogPath
            + " | Enabled areas=" + vars.enabledAreaIds.Count
            + " | LiveSplit segments=" + vars.runSegmentCount
            + " | Ziggurat index=" + vars.zigguratSegmentIndex;

        System.IO.File.WriteAllText(vars.statusPath, ready + System.Environment.NewLine);
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " " + ready + System.Environment.NewLine);
        print("[PoE2 ASL] " + ready);
    }
    catch (System.Exception ex)
    {
        vars.reader = null;
        System.IO.File.WriteAllText(vars.statusPath, "ERROR opening Client.txt: " + ex.Message + System.Environment.NewLine);
        print("[PoE2 ASL] ERROR opening Client.txt: " + ex.Message);
    }
}

update
{

    // ---- Game Time maintenance (independent of route / boss progression) ----
    var gtNowUtc = System.DateTime.UtcNow;

    // Optional manual-pause protocol v2. GameTimeWatcher can publish provisional
    // PENDING_PAUSE / PENDING_RUN states immediately on ESC/Start. LiveSplit reacts
    // immediately, then this block refunds or re-removes the provisional interval if
    // visual verification rejects it. Missing/stale watcher state still fails open.
    if (settings["manualPauseRemoval"] && gtNowUtc >= vars.gtNextPausePollUtc)
    {
        vars.gtNextPausePollUtc = gtNowUtc.AddMilliseconds(25);
        bool gtFresh = false;
        bool gtReadValid = false;
        bool gtUsingReadGrace = false;
        string gtWireState = "RUNNING";
        long gtHeartbeatTicks = 0;
        long gtOriginTicks = 0;
        long gtStateSequence = 0;
        try
        {
            if (System.IO.File.Exists(vars.gtManualPauseStatePath))
            {
                bool gtSawState = false;
                bool gtSawHeartbeat = false;
                foreach (string gtRaw in System.IO.File.ReadAllLines(vars.gtManualPauseStatePath))
                {
                    string gtLine = gtRaw.Trim();
                    if (gtLine.StartsWith("state=", System.StringComparison.OrdinalIgnoreCase))
                    {
                        gtWireState = gtLine.Substring(6).Trim().ToUpperInvariant();
                        gtSawState = true;
                    }
                    else if (gtLine.StartsWith("heartbeatUtcTicks=", System.StringComparison.OrdinalIgnoreCase))
                    {
                        long gtParsedHeartbeat = 0;
                        if (System.Int64.TryParse(gtLine.Substring(18).Trim(), out gtParsedHeartbeat))
                        {
                            gtHeartbeatTicks = gtParsedHeartbeat;
                            gtSawHeartbeat = true;
                        }
                    }
                    else if (gtLine.StartsWith("stateSequence=", System.StringComparison.OrdinalIgnoreCase))
                        System.Int64.TryParse(gtLine.Substring(14).Trim(), out gtStateSequence);
                    else if (gtLine.StartsWith("originUtcTicks=", System.StringComparison.OrdinalIgnoreCase))
                        System.Int64.TryParse(gtLine.Substring(15).Trim(), out gtOriginTicks);
                }

                bool gtStateRecognized = gtWireState == "RUNNING" || gtWireState == "PAUSED" ||
                    gtWireState == "PENDING_PAUSE" || gtWireState == "PENDING_RUN";
                gtReadValid = gtSawState && gtStateRecognized && gtSawHeartbeat && gtHeartbeatTicks > 0;

                if (gtReadValid)
                {
                    long gtAgeTicks = System.Math.Abs(gtNowUtc.Ticks - gtHeartbeatTicks);
                    gtFresh = gtAgeTicks <= System.TimeSpan.FromSeconds(2.0).Ticks;
                    if (gtFresh)
                    {
                        vars.gtManualLastGoodReadUtc = gtNowUtc;
                        vars.gtManualLastGoodWireState = gtWireState;
                        vars.gtManualLastGoodHeartbeatTicks = gtHeartbeatTicks;
                        vars.gtManualLastGoodOriginTicks = gtOriginTicks;
                        vars.gtManualLastGoodStateSequence = gtStateSequence;
                    }
                    else
                    {
                        // A successfully read but stale heartbeat is authoritative: fail
                        // open immediately and do not let read-race grace extend it.
                        vars.gtManualLastGoodReadUtc = System.DateTime.MinValue;
                    }
                }
            }
        }
        catch { gtReadValid = false; }

        if (!gtReadValid)
        {
            long gtCachedHeartbeatTicks = (long)vars.gtManualLastGoodHeartbeatTicks;
            bool gtCachedHeartbeatFresh = gtCachedHeartbeatTicks > 0 &&
                System.Math.Abs(gtNowUtc.Ticks - gtCachedHeartbeatTicks) <= System.TimeSpan.FromSeconds(2.0).Ticks;
            bool gtWithinReadGrace = vars.gtManualLastGoodReadUtc != System.DateTime.MinValue &&
                (gtNowUtc - vars.gtManualLastGoodReadUtc).TotalMilliseconds <= 500.0;

            if (gtWithinReadGrace && gtCachedHeartbeatFresh)
            {
                gtWireState = (string)vars.gtManualLastGoodWireState;
                gtHeartbeatTicks = gtCachedHeartbeatTicks;
                gtOriginTicks = (long)vars.gtManualLastGoodOriginTicks;
                gtStateSequence = (long)vars.gtManualLastGoodStateSequence;
                gtFresh = true;
                gtUsingReadGrace = true;
            }
        }

        if (!gtFresh)
            gtWireState = "RUNNING";

        if (gtUsingReadGrace)
        {
            if (!(bool)vars.gtManualReadGraceActive && settings["debugLog"])
                System.IO.File.AppendAllText(vars.gtDebugPath,
                    System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " GT_MANUAL_READ_GRACE"
                    + " | state=" + gtWireState
                    + " | maxMs=500"
                    + System.Environment.NewLine);
            vars.gtManualReadGraceActive = true;
        }
        else if ((bool)vars.gtManualReadGraceActive)
        {
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.gtDebugPath,
                    System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                    + (gtReadValid && gtFresh ? " GT_MANUAL_READ_RECOVERED" : " GT_MANUAL_READ_GRACE_EXPIRED")
                    + " | state=" + gtWireState
                    + System.Environment.NewLine);
            vars.gtManualReadGraceActive = false;
        }

        string gtPreviousWireState = (string)vars.gtManualWireState;
        if (!System.String.Equals(gtWireState, gtPreviousWireState, System.StringComparison.OrdinalIgnoreCase))
        {
            // Resolve the interval that LiveSplit provisionally held/released.
            if ((gtPreviousWireState == "PENDING_PAUSE" || gtPreviousWireState == "PENDING_RUN") &&
                vars.gtManualPendingObservedUtc != System.DateTime.MinValue &&
                (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))
            {
                double gtManualCorrectionSeconds = 0.0;
                bool gtAccepted = false;

                if (gtPreviousWireState == "PENDING_PAUSE")
                {
                    gtAccepted = gtWireState == "PAUSED";
                    if (gtAccepted)
                    {
                        // The timer did not provisionally stop until the ASL first saw
                        // PENDING_PAUSE. Remove the small edge->poll interval as well.
                        if (vars.gtManualPendingOriginUtc != System.DateTime.MinValue)
                            gtManualCorrectionSeconds = -System.Math.Max(0.0,
                                (vars.gtManualPendingObservedUtc - vars.gtManualPendingOriginUtc).TotalSeconds);
                    }
                    else
                    {
                        // False ESC/Start candidate: refund everything LiveSplit held.
                        gtManualCorrectionSeconds = System.Math.Max(0.0,
                            (gtNowUtc - vars.gtManualPendingObservedUtc).TotalSeconds);
                    }
                }
                else // PENDING_RUN
                {
                    gtAccepted = gtWireState == "RUNNING";
                    if (gtAccepted)
                    {
                        // The game resumed at the input/visual edge, before the ASL first
                        // saw PENDING_RUN. Add that small over-paused interval back.
                        if (vars.gtManualPendingOriginUtc != System.DateTime.MinValue)
                            gtManualCorrectionSeconds = System.Math.Max(0.0,
                                (vars.gtManualPendingObservedUtc - vars.gtManualPendingOriginUtc).TotalSeconds);
                    }
                    else
                    {
                        // False resume candidate: remove the time LiveSplit provisionally ran.
                        gtManualCorrectionSeconds = -System.Math.Max(0.0,
                            (gtNowUtc - vars.gtManualPendingObservedUtc).TotalSeconds);
                    }
                }

                if (System.Math.Abs(gtManualCorrectionSeconds) >= 0.0001)
                {
                    vars.gtManualPendingCorrection = vars.gtManualPendingCorrection.Add(
                        System.TimeSpan.FromSeconds(gtManualCorrectionSeconds));
                    vars.gtManualCorrectionPending = true;
                    if (settings["debugLog"])
                        System.IO.File.AppendAllText(vars.gtDebugPath,
                            System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " GT_MANUAL_PROVISIONAL_RESOLVE"
                            + " | from=" + gtPreviousWireState
                            + " | to=" + gtWireState
                            + " | accepted=" + (gtAccepted ? "true" : "false")
                            + " | correction=" + gtManualCorrectionSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + System.Environment.NewLine);
                }
            }

            if (gtWireState == "PENDING_PAUSE" || gtWireState == "PENDING_RUN")
            {
                vars.gtManualPendingKind = gtWireState;
                vars.gtManualPendingObservedUtc = gtNowUtc;
                try
                {
                    vars.gtManualPendingOriginUtc = gtOriginTicks > 0
                        ? new System.DateTime(gtOriginTicks, System.DateTimeKind.Utc)
                        : gtNowUtc;
                }
                catch { vars.gtManualPendingOriginUtc = gtNowUtc; }
            }
            else
            {
                vars.gtManualPendingKind = "";
                vars.gtManualPendingObservedUtc = System.DateTime.MinValue;
                vars.gtManualPendingOriginUtc = System.DateTime.MinValue;
            }

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.gtDebugPath,
                    System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " GT_MANUAL_STATE"
                    + " | state=" + gtWireState
                    + " | previous=" + gtPreviousWireState
                    + " | heartbeatFresh=" + (gtFresh ? "true" : "false")
                    + " | sequence=" + gtStateSequence.ToString()
                    + System.Environment.NewLine);

            vars.gtManualWireState = gtWireState;
            vars.gtManualStateSequence = gtStateSequence;
        }

        vars.gtManualPauseFresh = gtFresh;
        vars.gtManualPauseActive = gtFresh && (gtWireState == "PAUSED" || gtWireState == "PENDING_PAUSE");
    }
    else if (!settings["manualPauseRemoval"])
    {
        vars.gtManualPauseFresh = false;
        vars.gtManualPauseActive = false;
        vars.gtManualWireState = "RUNNING";
        vars.gtManualPendingKind = "";
        vars.gtManualPendingObservedUtc = System.DateTime.MinValue;
        vars.gtManualPendingOriginUtc = System.DateTime.MinValue;
        vars.gtManualLastGoodReadUtc = System.DateTime.MinValue;
        vars.gtManualLastGoodWireState = "RUNNING";
        vars.gtManualLastGoodHeartbeatTicks = 0L;
        vars.gtManualLastGoodOriginTicks = 0L;
        vars.gtManualLastGoodStateSequence = 0L;
        vars.gtManualReadGraceActive = false;
    }

    if (vars.gtReader != null)
    {
        int gtProcessed = 0;
        string gtLine = null;
        while (gtProcessed < 500 && (gtLine = vars.gtReader.ReadLine()) != null)
        {
            gtProcessed++;

            if (vars.gtLoadStartRegex.IsMatch(gtLine))
            {
                if (!vars.gtLoadActive)
                {
                    vars.gtLoadActive = true;
                    vars.gtLoadObservedExclusiveSeconds = 0.0;
                    vars.gtLoadManualOverlapSeconds = 0.0;
                    vars.gtLoadSampleUtc = System.DateTime.MinValue;
                    if (settings["debugLog"])
                        System.IO.File.AppendAllText(vars.gtDebugPath,
                            System.DateTime.Now.ToString("s") + " GT_LOAD_START" + System.Environment.NewLine);
                }
            }

            var gtEnd = vars.gtLoadEndRegex.Match(gtLine);
            if (gtEnd.Success)
            {
                double gtReportedSeconds = 0.0;
                bool gtParsed = System.Double.TryParse(
                    gtEnd.Groups[3].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out gtReportedSeconds);

                // Capture the final sample between the preceding isLoading tick and this log line.
                if (vars.gtLoadActive && vars.gtLoadSampleUtc != System.DateTime.MinValue &&
                    (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))
                {
                    double gtTailSeconds = (gtNowUtc - vars.gtLoadSampleUtc).TotalSeconds;
                    if (gtTailSeconds > 0 && gtTailSeconds < 2.0)
                    {
                        if (settings["manualPauseRemoval"] && vars.gtManualPauseActive)
                            vars.gtLoadManualOverlapSeconds += gtTailSeconds;
                        else
                            vars.gtLoadObservedExclusiveSeconds += gtTailSeconds;
                    }
                }

                if (gtParsed && gtReportedSeconds >= 0.0 && settings["gameTimeLoads"] &&
                    (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))
                {
                    // The first Riverbank load can begin before LiveSplit starts. Never remove more
                    // load time than the attempt has accumulated so Game Time cannot become negative.
                    double gtDesiredSeconds = gtReportedSeconds;
                    if (timer.CurrentTime.RealTime.HasValue)
                        gtDesiredSeconds = System.Math.Min(gtDesiredSeconds, System.Math.Max(0.0, timer.CurrentTime.RealTime.Value.TotalSeconds));

                    // Manual pause and loading may overlap (for example Respawn at Checkpoint).
                    // The overlap is already removed by the manual-pause isLoading state, so only
                    // the non-overlapping part of the reported load belongs to this correction.
                    double gtDesiredExclusiveSeconds = System.Math.Max(0.0, gtDesiredSeconds - vars.gtLoadManualOverlapSeconds);
                    double gtCorrectionSeconds = vars.gtLoadObservedExclusiveSeconds - gtDesiredExclusiveSeconds;
                    vars.gtPendingCorrection = vars.gtPendingCorrection.Add(System.TimeSpan.FromSeconds(gtCorrectionSeconds));
                    vars.gtCorrectionPending = true;

                    if (settings["debugLog"])
                        System.IO.File.AppendAllText(vars.gtDebugPath,
                            System.DateTime.Now.ToString("s") + " GT_LOAD_END"
                            + " | area=" + gtEnd.Groups[2].Value
                            + " | reported=" + gtReportedSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + " | desiredOverlap=" + gtDesiredSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + " | observedExclusive=" + vars.gtLoadObservedExclusiveSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + " | manualOverlap=" + vars.gtLoadManualOverlapSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + " | correction=" + gtCorrectionSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                            + System.Environment.NewLine);
                }

                vars.gtLoadActive = false;
                vars.gtLoadObservedExclusiveSeconds = 0.0;
                vars.gtLoadManualOverlapSeconds = 0.0;
                vars.gtLoadSampleUtc = System.DateTime.MinValue;
            }
        }
    }
    // ---- end Game Time maintenance ----

    vars.areaChanged = false;
    vars.startTrigger = false;

    if (vars.reader == null)
        return false;

    // v0.2.15-proven explicit finish commit, generalized for unordered mode.
    // If Cuachic was held because it was the last unresolved area, stamp both
    // Cuachic and Ziggurat at the exact Ziggurat-entry time. Otherwise only
    // stamp Ziggurat; Cuachic keeps the time from when it was completed earlier.
    if (vars.finishStage == 21 && !vars.finishComplete && System.DateTime.Now >= vars.finalForceNotBefore)
    {
        vars.finishStage = 40;
        vars.lastAction = "FINAL ZIGGURAT EXPLICIT COMMIT";

        int runCount = 0;
        int cuachicSegmentIndex = -1;
        int zigguratSegmentIndex = -1;
        bool cuachicStamped = false;
        bool zigguratStamped = false;

        var exact = new LiveSplit.Model.Time(
            (System.TimeSpan?)vars.finishRealTime,
            (System.TimeSpan?)vars.finishGameTime
        );

        try
        {
            foreach (LiveSplit.Model.ISegment finalSegment in timer.Run)
            {
                if (finalSegment.Name == "The Cuachic Vault")
                {
                    cuachicSegmentIndex = runCount;
                    if (vars.finishCuachicHeld)
                    {
                        finalSegment.SplitTime = exact;
                        cuachicStamped = true;
                    }
                }
                else if (finalSegment.Name == "The Ziggurat Refuge")
                {
                    zigguratSegmentIndex = runCount;
                    finalSegment.SplitTime = exact;
                    zigguratStamped = true;
                }
                runCount++;
            }

            if (!zigguratStamped)
            {
                vars.finishStage = -1;
                vars.lastAction = "FINAL ERROR / ZIGGURAT SEGMENT NOT FOUND";
                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_NOT_FOUND"
                        + " | runCount=" + runCount
                        + " | currentLiveSplitIndex=" + timer.CurrentSplitIndex
                        + System.Environment.NewLine);
            }
            else
            {
                timer.CurrentSplitIndex = zigguratSegmentIndex + 1;
                timer.Run.HasChanged = true;

                bool endedNormally = zigguratSegmentIndex == runCount - 1;
                if (endedNormally)
                {
                    timer.CurrentPhase = LiveSplit.Model.TimerPhase.Ended;
                    timer.AttemptEnded = LiveSplit.Model.TimeStamp.CurrentDateTime;
                }
                else
                {
                    if (vars.finishRealTime != null)
                        timer.TimePausedAt = (System.TimeSpan)vars.finishRealTime;
                    timer.CurrentPhase = LiveSplit.Model.TimerPhase.Paused;
                }

                timer.CallRunManuallyModified();

                vars.finishArmed = false;
                vars.finishComplete = true;
                vars.finishStage = 4;
                vars.lastAction = endedNormally
                    ? "FINISHED / ZIGGURAT STAMPED / TIMER ENDED"
                    : "FINISHED / ZIGGURAT STAMPED / TIMER PAUSED";

                string finishStatus = "Status: FINISHED" + System.Environment.NewLine
                    + "Mode: unordered first-visit area pools" + System.Environment.NewLine
                    + "Completion condition: ENTERED The Ziggurat Refuge" + System.Environment.NewLine
                    + "Completed enabled areas: " + vars.completedAreaIds.Count + " / " + vars.enabledAreaIds.Count + System.Environment.NewLine
                    + "Act 1: " + vars.subgroupCompleted["act1"] + " / " + vars.subgroupTotals["act1"] + System.Environment.NewLine
                    + "Act 2: " + vars.subgroupCompleted["act2"] + " / " + vars.subgroupTotals["act2"] + System.Environment.NewLine
                    + "Act 3: " + vars.subgroupCompleted["act3"] + " / " + vars.subgroupTotals["act3"] + System.Environment.NewLine
                    + "Act 4: " + vars.subgroupCompleted["act4"] + " / " + vars.subgroupTotals["act4"] + System.Environment.NewLine
                    + "Interludes: " + vars.subgroupCompleted["interludes"] + " / " + vars.subgroupTotals["interludes"] + System.Environment.NewLine
                    + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine;
                System.IO.File.WriteAllText(vars.statusPath, finishStatus);

                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_COMMITTED"
                        + " | runCount=" + runCount
                        + " | cuachicSegmentIndex=" + cuachicSegmentIndex
                        + " | zigguratSegmentIndex=" + zigguratSegmentIndex
                        + " | liveSplitIndexAfter=" + timer.CurrentSplitIndex
                        + " | phase=" + timer.CurrentPhase.ToString()
                        + " | cuachicHeld=" + (vars.finishCuachicHeld ? "true" : "false")
                        + " | cuachicTimestampApplied=" + (cuachicStamped ? "true" : "false")
                        + " | zigguratTimestampApplied=true"
                        + " | exactEntryTimeApplied=true"
                        + " | RUN_COMPLETE"
                        + System.Environment.NewLine);
            }
        }
        catch (System.Exception ex)
        {
            vars.finishStage = -1;
            vars.lastAction = "FINAL ERROR / ZIGGURAT COMMIT FAILED";
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_COMMIT_FAILED"
                    + " | " + ex.GetType().FullName + ": " + ex.Message
                    + System.Environment.NewLine);
        }

        return true;
    }

    if (vars.finishComplete)
        return true;

    // Do not consume more Client.txt lines while waiting for LiveSplit to commit
    // a queued normal split or Cuachic finish split.
    if (vars.pendingAreaId != "" || vars.finishStage == 2 || vars.finishStage == 20)
        return true;

    int processed = 0;
    string line = null;

    while (processed < 250 && (line = vars.reader.ReadLine()) != null)
    {
        processed++;

        if (vars.loadStartRegex.IsMatch(line))
            vars.isLoading = true;

        if (vars.riverbankStartArmed
            && !vars.startTrigger
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning
            && line.IndexOf("Wounded Man: Reach... Clearfell... Find the Miller...", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            vars.startTrigger = true;
            vars.riverbankStartArmed = false;
            vars.lastAction = "START TRIGGER / WOUNDED MAN FINAL LINE";
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_TRIGGER WOUNDED_MAN_FINAL_LINE | G1_1 The Riverbank" + System.Environment.NewLine);
            break;
        }

        string areaId = null;
        int generatedLevel = -1;
        string detectionSource = null;

        var match = vars.areaRegex.Match(line);
        if (match.Success)
        {
            vars.isLoading = false;
            int parsedLevel = 0;
            int.TryParse(match.Groups[2].Value, out parsedLevel);
            generatedLevel = parsedLevel;
            areaId = match.Groups[3].Value;
            detectionSource = "GeneratingLevel";
        }
        else if (settings["enteredNameFallback"])
        {
            var enteredMatch = vars.enteredNameRegex.Match(line);
            if (!enteredMatch.Success)
                continue;

            string enteredName = enteredMatch.Groups[2].Value.Trim();
            if (!vars.areaIdsByName.ContainsKey(enteredName))
                continue;

            areaId = vars.areaIdsByName[enteredName];
            generatedLevel = -1;
            detectionSource = "EnteredName";
            vars.isLoading = false;
        }
        else
        {
            continue;
        }

        if (areaId == null || areaId.Length == 0)
            continue;

        bool known = vars.areaNames.ContainsKey(areaId);
        string areaName = known ? vars.areaNames[areaId] : "UNKNOWN AREA";
        bool enabled = vars.enabledAreaIds.Contains(areaId);
        bool alreadyCompleted = vars.completedAreaIds.Contains(areaId);
        string subgroupId = vars.areaSubgroup.ContainsKey(areaId) ? vars.areaSubgroup[areaId] : "";
        string subgroupName = subgroupId != "" && vars.subgroupDisplay.ContainsKey(subgroupId)
            ? vars.subgroupDisplay[subgroupId]
            : "";

        if (!known && vars.unknownAreaIds.Add(areaId))
        {
            System.IO.File.AppendAllText(vars.unknownPath,
                System.DateTime.Now.ToString("s") + " | " + areaId + " | " + line + System.Environment.NewLine);
        }

        if (settings["validationLog"] && vars.seenAreaIds.Add(areaId))
        {
            string csvName = areaName.Replace("\"", "\"\"");
            System.IO.File.AppendAllText(vars.validationPath,
                System.DateTime.Now.ToString("s") + ","
                + "\"" + areaId + "\","
                + "\"" + csvName + "\","
                + generatedLevel + ","
                + (known ? "true" : "false") + ","
                + (enabled ? "true" : "false") + ","
                + "\"" + subgroupName + "\","
                + (alreadyCompleted ? "true" : "false") + ","
                + detectionSource
                + System.Environment.NewLine);
        }

        // Ignore the duplicate log representation of the same area transition.
        if (System.String.Equals(areaId, vars.lastAreaId, System.StringComparison.OrdinalIgnoreCase))
            continue;

        vars.lastAreaId = areaId;
        vars.currentAreaId = areaId;
        vars.currentAreaLevel = generatedLevel;
        vars.areaChanged = true;

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " AREA " + areaId + " " + areaName
                + " | level=" + generatedLevel
                + " | subgroup=" + (subgroupName == "" ? "<none>" : subgroupName)
                + " | enabled=" + (enabled ? "true" : "false")
                + " | completed=" + (alreadyCompleted ? "true" : "false")
                + " | source=" + detectionSource
                + System.Environment.NewLine);

        if (System.String.Equals(areaId, "G1_1", System.StringComparison.OrdinalIgnoreCase)
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
        {
            // Riverbank is a real campaign area. Count its first entry as completed,
            // but defer the timer start until the Wounded Man's final opening line so
            // the wake-up/setup and first NPC interaction remain outside the run.
            if (enabled && !vars.completedAreaIds.Contains(areaId))
            {
                vars.completedAreaIds.Add(areaId);
                if (subgroupId != "")
                    vars.subgroupCompleted[subgroupId] = vars.subgroupCompleted[subgroupId] + 1;
            }
            vars.riverbankStartArmed = true;
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_ARMED G1_1 The Riverbank | waiting=Wounded Man final line" + System.Environment.NewLine);
            continue;
        }

        if (settings["finishAtZiggurat"]
            && System.String.Equals(areaId, "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase))
        {
            var exactFinishTime = timer.CurrentTime;
            vars.finishRealTime = exactFinishTime.RealTime;
            vars.finishGameTime = exactFinishTime.GameTime;

            if (vars.finishCuachicHeld)
            {
                vars.finishStage = 2;
                vars.lastAction = "FINAL SEQUENCE / CUACHIC SPLIT QUEUED";
                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " FINISH_TRIGGER"
                        + " | entered=G_Endgame_Town The Ziggurat Refuge"
                        + " | mode=HELD_CUACHIC_THEN_ZIGGURAT"
                        + " | liveSplitIndex=" + timer.CurrentSplitIndex
                        + System.Environment.NewLine);
            }
            else
            {
                vars.finishStage = 21;
                vars.finalForceNotBefore = System.DateTime.Now;
                vars.lastAction = "FINAL SEQUENCE / ZIGGURAT EXPLICIT COMMIT QUEUED";
                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " FINISH_TRIGGER"
                        + " | entered=G_Endgame_Town The Ziggurat Refuge"
                        + " | mode=ZIGGURAT_ONLY"
                        + " | liveSplitIndex=" + timer.CurrentSplitIndex
                        + System.Environment.NewLine);
            }
            break;
        }

        if (!enabled)
        {
            vars.lastAction = "IGNORE / AREA DISABLED";
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_DISABLED " + areaId + " " + areaName + System.Environment.NewLine);
            continue;
        }

        if (alreadyCompleted)
        {
            vars.lastAction = "IGNORE / ALREADY COMPLETED";
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_REVISIT " + areaId + " " + areaName + System.Environment.NewLine);
            continue;
        }

        // Cuachic is only held open when it is the last unresolved enabled area,
        // or when the loaded LiveSplit file is already positioned on the
        // penultimate slot (useful for end-sequence practice).
        bool cuachic = System.String.Equals(areaId, "P3_7", System.StringComparison.OrdinalIgnoreCase);
        bool lastUnresolvedEnabledArea = vars.completedAreaIds.Count + 1 >= vars.enabledAreaIds.Count;
        bool liveSplitPenultimate = vars.zigguratSegmentIndex >= 1
            && timer.CurrentSplitIndex == vars.zigguratSegmentIndex - 1;

        if (cuachic && (lastUnresolvedEnabledArea || liveSplitPenultimate))
        {
            vars.completedAreaIds.Add(areaId);
            if (subgroupId != "")
                vars.subgroupCompleted[subgroupId] = vars.subgroupCompleted[subgroupId] + 1;
            vars.splitHistory.Add(areaId);

            vars.finishArmed = true;
            vars.finishCuachicHeld = true;
            vars.finishStage = 1;
            vars.lastAction = "FINISH ARMED / CUACHIC HELD";

            if (settings["dynamicSegmentNames"])
            {
                int renameIndex = 0;
                foreach (LiveSplit.Model.ISegment renameSegment in timer.Run)
                {
                    if (renameIndex == timer.CurrentSplitIndex)
                    {
                        renameSegment.Name = areaName;
                        timer.Run.HasChanged = true;
                        timer.CallRunManuallyModified();
                        break;
                    }
                    renameIndex++;
                }
            }

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINISH_ARMED"
                    + " | area=P3_7 The Cuachic Vault"
                    + " | liveSplitIndex=" + timer.CurrentSplitIndex
                    + " | completedAreas=" + vars.completedAreaIds.Count + "/" + vars.enabledAreaIds.Count
                    + " | waitingFor=G_Endgame_Town The Ziggurat Refuge"
                    + System.Environment.NewLine);
            break;
        }

        // Normal unordered area: queue one native LiveSplit split. The area is
        // committed to completedAreaIds only from onSplit, after LiveSplit confirms it.
        vars.pendingAreaId = areaId;
        vars.pendingAreaName = areaName;
        vars.pendingSubgroup = subgroupId;
        vars.pendingSplitIndex = timer.CurrentSplitIndex;
        vars.lastAction = "UNORDERED AREA SPLIT QUEUED";

        if (settings["dynamicSegmentNames"])
        {
            int renameIndex = 0;
            foreach (LiveSplit.Model.ISegment renameSegment in timer.Run)
            {
                if (renameIndex == timer.CurrentSplitIndex)
                {
                    renameSegment.Name = areaName;
                    timer.Run.HasChanged = true;
                    timer.CallRunManuallyModified();
                    break;
                }
                renameIndex++;
            }
        }

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " UNORDERED_MATCH " + areaId + " " + areaName
                + " | subgroup=" + subgroupName
                + " | liveSplitIndex=" + timer.CurrentSplitIndex
                + " | completedAreas=" + vars.completedAreaIds.Count + "/" + vars.enabledAreaIds.Count
                + System.Environment.NewLine);

        break;
    }

    return true;
}

start
{
    if (settings["autoStart"] && vars.startTrigger)
    {
        vars.lastAction = "START";
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " START G1_1 The Riverbank | trigger=Wounded Man final line" + System.Environment.NewLine);
        return true;
    }
}

split
{
    if (vars.finishStage == 2)
    {
        vars.finishStage = 20;
        vars.lastAction = "FINAL SPLIT 1 REQUESTED / CUACHIC";
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " FINAL_SPLIT_1_REQUEST"
                + " | currentLiveSplitIndex=" + timer.CurrentSplitIndex
                + " | stamps=P3_7 The Cuachic Vault"
                + System.Environment.NewLine);
        return true;
    }

    if (vars.pendingAreaId != "")
    {
        vars.lastAction = "SPLIT REQUESTED / " + vars.pendingAreaName;
        return true;
    }

    return false;
}

isLoading
{
    bool gtPauseLoad = settings["gameTimeLoads"] && vars.gtLoadActive;
    bool gtPauseManual = settings["manualPauseRemoval"] && vars.gtManualPauseActive;

    if (gtPauseLoad)
    {
        var gtSampleNow = System.DateTime.UtcNow;
        if (vars.gtLoadSampleUtc != System.DateTime.MinValue)
        {
            double gtDeltaSeconds = (gtSampleNow - vars.gtLoadSampleUtc).TotalSeconds;
            if (gtDeltaSeconds > 0 && gtDeltaSeconds < 2.0)
            {
                if (gtPauseManual)
                    vars.gtLoadManualOverlapSeconds += gtDeltaSeconds;
                else
                    vars.gtLoadObservedExclusiveSeconds += gtDeltaSeconds;
            }
        }
        vars.gtLoadSampleUtc = gtSampleNow;
    }
    else
    {
        vars.gtLoadSampleUtc = System.DateTime.MinValue;
    }

    return gtPauseLoad || gtPauseManual;
}


onSplit
{
    if (vars.finishStage == 20)
    {
        vars.lastAction = "FINAL SPLIT 1 COMMITTED / ZIGGURAT ACTIVE";

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " FINAL_SPLIT_1_COMMITTED"
                + " | stamped=P3_7 The Cuachic Vault"
                + " | liveSplitIndex=" + timer.CurrentSplitIndex
                + " | next=G_Endgame_Town The Ziggurat Refuge"
                + System.Environment.NewLine);

        vars.finishStage = 21;
        vars.finalForceNotBefore = System.DateTime.Now.AddMilliseconds(100);

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_COMMIT_QUEUED"
                + " | liveSplitIndex=" + timer.CurrentSplitIndex
                + " | notBefore=" + vars.finalForceNotBefore.ToString("o")
                + System.Environment.NewLine);
    }
    else if (vars.pendingAreaId != "")
    {
        string committedId = vars.pendingAreaId;
        string committedName = vars.pendingAreaName;
        string committedSubgroup = vars.pendingSubgroup;
        int oldIndex = vars.pendingSplitIndex;

        if (!vars.completedAreaIds.Contains(committedId))
        {
            vars.completedAreaIds.Add(committedId);
            if (committedSubgroup != "")
                vars.subgroupCompleted[committedSubgroup] = vars.subgroupCompleted[committedSubgroup] + 1;
            vars.splitHistory.Add(committedId);
        }

        vars.pendingAreaId = "";
        vars.pendingAreaName = "";
        vars.pendingSubgroup = "";
        vars.pendingSplitIndex = -1;
        vars.lastAction = "SPLIT COMMITTED / " + committedName;

        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Mode: unordered first-visit area pools" + System.Environment.NewLine
            + "Last split: " + committedId + " | " + committedName + System.Environment.NewLine
            + "Completed enabled areas: " + vars.completedAreaIds.Count + " / " + vars.enabledAreaIds.Count + System.Environment.NewLine
            + "Act 1: " + vars.subgroupCompleted["act1"] + " / " + vars.subgroupTotals["act1"] + System.Environment.NewLine
            + "Act 2: " + vars.subgroupCompleted["act2"] + " / " + vars.subgroupTotals["act2"] + System.Environment.NewLine
            + "Act 3: " + vars.subgroupCompleted["act3"] + " / " + vars.subgroupTotals["act3"] + System.Environment.NewLine
            + "Act 4: " + vars.subgroupCompleted["act4"] + " / " + vars.subgroupTotals["act4"] + System.Environment.NewLine
            + "Interludes: " + vars.subgroupCompleted["interludes"] + " / " + vars.subgroupTotals["interludes"] + System.Environment.NewLine
            + "LiveSplit split index: " + timer.CurrentSplitIndex + System.Environment.NewLine
            + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " SPLIT_COMMITTED " + committedId + " " + committedName
                + " | subgroup=" + (committedSubgroup == "" ? "<none>" : committedSubgroup)
                + " | liveSplitIndex=" + oldIndex + " -> " + timer.CurrentSplitIndex
                + " | completedAreas=" + vars.completedAreaIds.Count + "/" + vars.enabledAreaIds.Count
                + System.Environment.NewLine);
    }
}

onStart
{
    // Reset attempt-local correction state. The Riverbank opening load occurs before
    // the official Wounded Man dialogue start and is intentionally outside the run.
    vars.gtPendingCorrection = System.TimeSpan.Zero;
    vars.gtCorrectionPending = false;
    vars.gtManualPendingCorrection = System.TimeSpan.Zero;
    vars.gtManualCorrectionPending = false;
    vars.gtLoadObservedExclusiveSeconds = 0.0;
    vars.gtLoadManualOverlapSeconds = 0.0;
    vars.gtLoadSampleUtc = System.DateTime.MinValue;

    vars.lastAction = "RUN STARTED";
}

gameTime
{
    // Manual-pause corrections are timestamp/accounting corrections, not load corrections.
    // Apply them immediately even while PoE2 is currently paused. This lets a visually
    // confirmed ESC pause rewind the frozen Game Time to the original key timestamp.
    // Rejected ESC candidates receive the opposite correction, refunding the provisional
    // hold as soon as the watcher returns to RUNNING.
    if (vars.gtManualCorrectionPending)
    {
        System.TimeSpan? gtManualCurrent = timer.CurrentTime.GameTime;
        if (gtManualCurrent.HasValue)
        {
            System.TimeSpan gtManualCorrected = gtManualCurrent.Value.Add(vars.gtManualPendingCorrection);
            if (gtManualCorrected < System.TimeSpan.Zero) gtManualCorrected = System.TimeSpan.Zero;

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.gtDebugPath,
                    System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                    + " GT_MANUAL_CORRECTION_APPLIED"
                    + " | delta=" + vars.gtManualPendingCorrection.TotalSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                    + " | before=" + gtManualCurrent.Value.ToString()
                    + " | after=" + gtManualCorrected.ToString()
                    + System.Environment.NewLine);

            vars.gtManualPendingCorrection = System.TimeSpan.Zero;
            vars.gtManualCorrectionPending = false;
            return gtManualCorrected;
        }
    }

    // Apply completed-load correction only while Game Time is actively running.
    // During a load/manual pause, isLoading owns the visible pause and correction waits.
    if (vars.gtCorrectionPending && !vars.gtLoadActive && !vars.gtManualPauseActive)
    {
        System.TimeSpan? gtCurrent = timer.CurrentTime.GameTime;
        if (gtCurrent.HasValue)
        {
            System.TimeSpan gtCorrected = gtCurrent.Value.Add(vars.gtPendingCorrection);
            if (gtCorrected < System.TimeSpan.Zero) gtCorrected = System.TimeSpan.Zero;

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.gtDebugPath,
                    System.DateTime.Now.ToString("s") + " GT_CORRECTION_APPLIED"
                    + " | delta=" + vars.gtPendingCorrection.TotalSeconds.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                    + " | before=" + gtCurrent.Value.ToString()
                    + " | after=" + gtCorrected.ToString()
                    + System.Environment.NewLine);

            vars.gtPendingCorrection = System.TimeSpan.Zero;
            vars.gtCorrectionPending = false;
            return gtCorrected;
        }
    }
}

onReset
{
    vars.gtLoadActive = false;
    vars.gtLoadObservedExclusiveSeconds = 0.0;
    vars.gtLoadManualOverlapSeconds = 0.0;
    vars.gtLoadSampleUtc = System.DateTime.MinValue;
    vars.gtPendingCorrection = System.TimeSpan.Zero;
    vars.gtCorrectionPending = false;
    vars.gtManualPendingCorrection = System.TimeSpan.Zero;
    vars.gtManualCorrectionPending = false;

    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "RESET";

    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.pendingSubgroup = "";
    vars.pendingSplitIndex = -1;

    vars.completedAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.splitHistory = new System.Collections.Generic.List<string>();
    vars.subgroupCompleted["act1"] = 0;
    vars.subgroupCompleted["act2"] = 0;
    vars.subgroupCompleted["act3"] = 0;
    vars.subgroupCompleted["act4"] = 0;
    vars.subgroupCompleted["interludes"] = 0;

    vars.finishArmed = false;
    vars.finishCuachicHeld = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;

    // Restore the segment names that were present when the script attached.
    // This prevents a prior unordered run's dynamic names from carrying into
    // the next attempt during the same LiveSplit session.
    if (settings["dynamicSegmentNames"])
    {
        int restoreIndex = 0;
        foreach (LiveSplit.Model.ISegment restoreSegment in timer.Run)
        {
            if (restoreIndex < vars.baseSegmentNames.Count)
                restoreSegment.Name = vars.baseSegmentNames[restoreIndex];
            restoreIndex++;
        }
        timer.Run.HasChanged = true;
        timer.CallRunManuallyModified();
    }

    if (settings["debugLog"])
        System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " RESET" + System.Environment.NewLine);
}

exit
{
    try { if (vars.gtReader != null) vars.gtReader.Close(); } catch {}
    vars.gtReader = null;

    try { if (vars.reader != null) vars.reader.Close(); } catch {}
    vars.reader = null;
}

shutdown
{
    try { if (vars.gtReader != null) vars.gtReader.Close(); } catch {}
    vars.gtReader = null;

    try { if (vars.reader != null) vars.reader.Close(); } catch {}
    vars.reader = null;
}
