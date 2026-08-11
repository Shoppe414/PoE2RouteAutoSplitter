/*
Path of Exile 2 Area Checklist AutoSplitter for LiveSplit
v1.1.0 validation build

A file-defined unordered objective pool.
- @start=<area id> defines the auto-start area. @start=manual disables auto-start.
- Every other non-comment line is an objective area ID.
- Objectives may be completed in any order.
- First visit to an unresolved objective renames the current LiveSplit slot and splits.
- Revisits are ignored.
- With a matching .lss (one row per objective), the final unresolved objective naturally ends the timer.

Config: <LiveSplit folder>\poe2_area_checklist.txt
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 20;
    settings.Add("autoStart", true, "Auto-start on the @start area from poe2_area_checklist.txt");
    settings.Add("dynamicSegmentNames", true, "Rename generic split slots to completed objective names");
    settings.Add("debugLog", true, "Write Area Checklist diagnostic log");
    settings.Add("enteredNameFallback", true, "Fallback to Client.txt 'You have entered ...' messages");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.configPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_area_checklist.txt");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_area_checklist_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_area_checklist_debug.log");

    vars.reader = null;
    vars.objectiveIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.startAreaId = "";
    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.startTrigger = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();

    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Generating level (\\d+) area \\\"([^\\\"]+)\\\"",
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
    vars.nameToId = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    foreach (var p in vars.areaNames) if (!vars.nameToId.ContainsKey(p.Value)) vars.nameToId[p.Value] = p.Key;
}

init
{
    vars.objectiveIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.startAreaId = "";
    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.startTrigger = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();

    try
    {
        if (!System.IO.File.Exists(vars.configPath))
            throw new System.Exception("Missing poe2_area_checklist.txt beside LiveSplit.exe");

        foreach (string raw in System.IO.File.ReadAllLines(vars.configPath))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            line = line.Trim();
            if (line == "") continue;

            if (line.StartsWith("@start=", System.StringComparison.OrdinalIgnoreCase))
            {
                string value = line.Substring(7).Trim();
                vars.startAreaId = System.String.Equals(value, "manual", System.StringComparison.OrdinalIgnoreCase) ? "" : value;
                continue;
            }

            if (!vars.areaNames.ContainsKey(line))
                throw new System.Exception("Unknown objective area ID: " + line);
            vars.objectiveIds.Add(line);
        }

        if (vars.startAreaId != "" && !vars.areaNames.ContainsKey(vars.startAreaId))
            throw new System.Exception("Unknown @start area ID: " + vars.startAreaId);
        if (vars.objectiveIds.Count == 0)
            throw new System.Exception("No objective areas configured");

        int runCount = 0;
        foreach (LiveSplit.Model.ISegment segment in timer.Run)
        {
            vars.baseSegmentNames.Add(segment.Name);
            runCount++;
        }
        int implicitStartObjectives = (vars.startAreaId != "" && vars.objectiveIds.Contains(vars.startAreaId)) ? 1 : 0;
        int expectedRunCount = vars.objectiveIds.Count - implicitStartObjectives;
        if (runCount != expectedRunCount)
            throw new System.Exception("LiveSplit segment count (" + runCount + ") must equal timed objective count (" + expectedRunCount + "). The auto-start area is counted as completed at timer start and does not require a zero-time split.");

        string gameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.clientLogPath = System.IO.Path.Combine(gameDir, "logs", "Client.txt");
        var fs = new System.IO.FileStream(vars.clientLogPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        fs.Seek(0, System.IO.SeekOrigin.End);
        vars.reader = new System.IO.StreamReader(fs);
        vars.configValid = true;

        string startName = vars.startAreaId == "" ? "manual" : vars.areaNames[vars.startAreaId];
        string ready = "READY | v1.1.0 | Mode=AREA_CHECKLIST_UNORDERED | Objectives=" + vars.objectiveIds.Count
            + " | Start=" + startName + " | Client.txt=" + vars.clientLogPath;
        System.IO.File.WriteAllText(vars.statusPath, ready + System.Environment.NewLine);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " " + ready + System.Environment.NewLine);
        print("[PoE2 ASL] " + ready);
    }
    catch (System.Exception ex)
    {
        vars.configValid = false;
        try { if (vars.reader != null) vars.reader.Close(); } catch {}
        vars.reader = null;
        System.IO.File.WriteAllText(vars.statusPath, "ERROR | " + ex.Message + System.Environment.NewLine);
        print("[PoE2 ASL] AREA_CHECKLIST ERROR: " + ex.Message);
    }
}

update
{
    vars.startTrigger = false;
    if (!vars.configValid || vars.reader == null) return false;
    if (vars.pendingAreaId != "") return true;

    while (vars.reader.Peek() >= 0)
    {
        string line = vars.reader.ReadLine();
        string areaId = "";
        string source = "";

        var am = vars.areaRegex.Match(line);
        if (am.Success)
        {
            areaId = am.Groups[3].Value;
            source = "GeneratingLevel";
        }
        else if (settings["enteredNameFallback"])
        {
            var em = vars.enteredNameRegex.Match(line);
            if (em.Success)
            {
                string displayName = em.Groups[2].Value.Trim();
                if (vars.nameToId.ContainsKey(displayName))
                {
                    areaId = vars.nameToId[displayName];
                    source = "EnteredName";
                }
            }
        }

        if (areaId == "" || !vars.areaNames.ContainsKey(areaId)) continue;
        if (System.String.Equals(areaId, vars.lastAreaId, System.StringComparison.OrdinalIgnoreCase)) continue;
        vars.lastAreaId = areaId;
        string areaName = vars.areaNames[areaId];

        if (vars.startAreaId != ""
            && System.String.Equals(areaId, vars.startAreaId, System.StringComparison.OrdinalIgnoreCase)
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
        {
            // The configured start area is a real exploration objective. Because the timer
            // begins on this same entry event, count it as satisfied without creating a
            // separate zero-time LiveSplit split.
            if (vars.objectiveIds.Contains(areaId) && !vars.completedIds.Contains(areaId))
                vars.completedIds.Add(areaId);

            vars.startTrigger = true;
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " START_TRIGGER " + areaId + " " + areaName
                + " | implicitObjective=" + (vars.objectiveIds.Contains(areaId) ? "true" : "false")
                + " | source=" + source + System.Environment.NewLine);
            break;
        }

        if (!vars.objectiveIds.Contains(areaId))
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " IGNORE_NOT_OBJECTIVE " + areaId + " " + areaName + System.Environment.NewLine);
            continue;
        }

        if (vars.completedIds.Contains(areaId))
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " IGNORE_REVISIT " + areaId + " " + areaName + System.Environment.NewLine);
            continue;
        }

        if (timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " IGNORE_BEFORE_START " + areaId + " " + areaName + System.Environment.NewLine);
            continue;
        }

        vars.pendingAreaId = areaId;
        vars.pendingAreaName = areaName;

        if (settings["dynamicSegmentNames"])
        {
            int i = 0;
            foreach (LiveSplit.Model.ISegment segment in timer.Run)
            {
                if (i == timer.CurrentSplitIndex)
                {
                    segment.Name = areaName;
                    timer.Run.HasChanged = true;
                    timer.CallRunManuallyModified();
                    break;
                }
                i++;
            }
        }

        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " OBJECTIVE_MATCH " + areaId + " " + areaName
            + " | completed=" + vars.completedIds.Count + "/" + vars.objectiveIds.Count
            + " | liveSplitIndex=" + timer.CurrentSplitIndex + System.Environment.NewLine);
        break;
    }
    return true;
}

start
{
    if (settings["autoStart"] && vars.startTrigger) return true;
}

split
{
    return vars.pendingAreaId != "";
}

onSplit
{
    if (vars.pendingAreaId != "")
    {
        string id = vars.pendingAreaId;
        string name = vars.pendingAreaName;
        vars.completedIds.Add(id);
        vars.pendingAreaId = "";
        vars.pendingAreaName = "";

        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Mode: unordered area checklist" + System.Environment.NewLine
            + "Last objective: " + id + " | " + name + System.Environment.NewLine
            + "Completed: " + vars.completedIds.Count + " / " + vars.objectiveIds.Count + System.Environment.NewLine
            + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " OBJECTIVE_SPLIT_COMMITTED " + id + " " + name
            + " | completed=" + vars.completedIds.Count + "/" + vars.objectiveIds.Count
            + " | liveSplitIndex=" + timer.CurrentSplitIndex
            + " | phase=" + timer.CurrentPhase.ToString()
            + (vars.completedIds.Count == vars.objectiveIds.Count ? " | CHECKLIST_COMPLETE" : "")
            + System.Environment.NewLine);
    }
}

onReset
{
    vars.completedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.startTrigger = false;
    vars.lastAreaId = "";

    if (settings["dynamicSegmentNames"])
    {
        int i = 0;
        foreach (LiveSplit.Model.ISegment segment in timer.Run)
        {
            if (i < vars.baseSegmentNames.Count) segment.Name = vars.baseSegmentNames[i];
            i++;
        }
        timer.Run.HasChanged = true;
        timer.CallRunManuallyModified();
    }
    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " RESET" + System.Environment.NewLine);
}

exit
{
    try { if (vars.reader != null) vars.reader.Close(); } catch {}
    vars.reader = null;
}

shutdown
{
    try { if (vars.reader != null) vars.reader.Close(); } catch {}
    vars.reader = null;
}
