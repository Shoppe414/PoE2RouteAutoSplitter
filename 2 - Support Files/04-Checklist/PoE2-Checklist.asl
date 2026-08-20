/*
Path of Exile 2 Area Checklist AutoSplitter for LiveSplit
v1.1.1 zone-start hotfix

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
    vars.riverbankStartArmed = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();

    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+)(?=.*(?:\\s2caa[0-9A-Fa-f]{4}\\s|Generating level))(?=.*\\[DEBUG\\s+[^\\]]+\\]\\s+.*?(\\d{1,3})\\D+\"([A-Za-z][A-Za-z0-9_]*)\")",
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
    // Present only the SetupUI-selected start rule in LiveSplit settings.
    string setupStartPolicyLabel = "Timer start: Manual Start";
    try
    {
        string setupStartPolicyId = "";
        if (System.IO.File.Exists(vars.configPath))
        {
            foreach (string setupStartRaw in System.IO.File.ReadLines(vars.configPath))
            {
                string setupStartText = setupStartRaw.Trim();
                if (!setupStartText.StartsWith("@start=", System.StringComparison.OrdinalIgnoreCase)) continue;
                setupStartPolicyId = setupStartText.Substring(7).Trim();
                break;
            }
        }
        if (System.String.Equals(setupStartPolicyId, "G1_1", System.StringComparison.OrdinalIgnoreCase))
            setupStartPolicyLabel = "Timer start: Riverbank Start — Wounded Man final opening line";
        else if (setupStartPolicyId != "" && !System.String.Equals(setupStartPolicyId, "manual", System.StringComparison.OrdinalIgnoreCase))
        {
            string setupStartDisplayName = vars.areaNames.ContainsKey(setupStartPolicyId) ? vars.areaNames[setupStartPolicyId] : setupStartPolicyId;
            setupStartPolicyLabel = "Timer start: First Split Zone Entry Auto Start — " + setupStartDisplayName;
        }
    }
    catch {}
    settings.Add("autoStart", true, setupStartPolicyLabel);
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

    vars.objectiveIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.startAreaId = "";
    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
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
        string ready = "READY | v1.1.1-zone-start | Mode=AREA_CHECKLIST_UNORDERED | Objectives=" + vars.objectiveIds.Count
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

    vars.startTrigger = false;
    if (!vars.configValid || vars.reader == null) return false;
    if (vars.pendingAreaId != "") return true;

    while (vars.reader.Peek() >= 0)
    {
        string line = vars.reader.ReadLine();


        // SetupUI zone-entry fast path. Use stable Client.txt substrings so the
        // configured start is not dependent on the stricter area regex prefix.
        if (!vars.startTrigger
            && vars.startAreaId != ""
            && !System.String.Equals(vars.startAreaId, "G1_1", System.StringComparison.OrdinalIgnoreCase)
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
        {
            bool setupStartWildcard = vars.startAreaId.EndsWith("*", System.StringComparison.Ordinal);
            string setupStartIdPrefix = setupStartWildcard ? vars.startAreaId.Substring(0, vars.startAreaId.Length - 1) : vars.startAreaId;
            string setupStartAreaNeedle = "area \"" + setupStartIdPrefix + (setupStartWildcard ? "" : "\"");
            bool setupStartInternal = (System.Text.RegularExpressions.Regex.IsMatch(line, @"\s2caa[0-9A-Fa-f]{4}\s") || line.IndexOf("Generating level", System.StringComparison.OrdinalIgnoreCase) >= 0)
                && line.IndexOf(setupStartAreaNeedle, System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool setupStartNamed = vars.areaNames.ContainsKey(vars.startAreaId)
                && line.IndexOf("You have entered " + vars.areaNames[vars.startAreaId] + ".", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (setupStartInternal || setupStartNamed)
            {
                if (vars.objectiveIds.Contains(vars.startAreaId) && !vars.completedIds.Contains(vars.startAreaId))
                    vars.completedIds.Add(vars.startAreaId);
                vars.startTrigger = true;
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_TRIGGER_FAST | area=" + vars.startAreaId + " " + vars.areaNames[vars.startAreaId]
                    + " | source=" + (setupStartInternal ? "GeneratingLevelSubstring" : "EnteredNameSubstring") + System.Environment.NewLine);
                break;
            }
        }

        if (vars.riverbankStartArmed
            && !vars.startTrigger
            && System.String.Equals(vars.startAreaId, "G1_1", System.StringComparison.OrdinalIgnoreCase)
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning
            && line.IndexOf("Wounded Man: Reach... Clearfell... Find the Miller...", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            vars.startTrigger = true;
            vars.riverbankStartArmed = false;
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_TRIGGER WOUNDED_MAN_FINAL_LINE | G1_1 The Riverbank" + System.Environment.NewLine);
            break;
        }

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
            // The configured start area is a real exploration objective. Count it as
            // satisfied without creating a zero-time split. Riverbank is special: its
            // area entry only arms the start; the Wounded Man's final line starts timing.
            if (vars.objectiveIds.Contains(areaId) && !vars.completedIds.Contains(areaId))
                vars.completedIds.Add(areaId);

            if (System.String.Equals(areaId, "G1_1", System.StringComparison.OrdinalIgnoreCase))
            {
                vars.riverbankStartArmed = true;
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_ARMED G1_1 The Riverbank"
                    + " | implicitObjective=" + (vars.objectiveIds.Contains(areaId) ? "true" : "false")
                    + " | source=" + source + " | waiting=Wounded Man final line" + System.Environment.NewLine);
                continue;
            }

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

onStart
{

    // Reset attempt-local correction state. When @start=G1_1, the opening load is
    // intentionally outside the run because start waits for the Wounded Man final line.
    vars.gtPendingCorrection = System.TimeSpan.Zero;
    vars.gtCorrectionPending = false;
    vars.gtManualPendingCorrection = System.TimeSpan.Zero;
    vars.gtManualCorrectionPending = false;
    vars.gtLoadObservedExclusiveSeconds = 0.0;
    vars.gtLoadManualOverlapSeconds = 0.0;
    vars.gtLoadSampleUtc = System.DateTime.MinValue;
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

    vars.completedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.pendingAreaId = "";
    vars.pendingAreaName = "";
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
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
