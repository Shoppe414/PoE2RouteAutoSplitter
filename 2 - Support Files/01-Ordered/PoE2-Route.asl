/*
Path of Exile 2 Route AutoSplitter for LiveSplit
v0.2.17 Riverbank-start-gate build

Core behavior:
- Reads Path of Exile 2 logs/Client.txt directly (no custom DLL).
- Auto-starts after the Wounded Man's final opening Riverbank line (optional).
- Splits ONLY when the detected area matches the NEXT route entry in poe2_route.txt.
- Unexpected/revisited areas never advance the route.
- Validates route IDs at startup and refuses to run a malformed route.
- Writes status/debug logs plus a unique-area validation CSV for fast-travel testing.
- Keeps route progress synchronized with LiveSplit manual Split / Skip Segment / Undo Split changes.
- Falls back to Client.txt "You have entered ..." messages when an area does not emit a usable internal-ID line.
- Treats entry into The Ziggurat Refuge as the explicit run finish.
- Holds The Cuachic Vault open until Ziggurat Refuge is detected.
- Uses a dedicated two-stage finish state machine: native ASL split #1 stamps Cuachic Vault; final Ziggurat completion is queued outside onSplit to avoid re-entrant LiveSplit split events.
- Captures the exact Ziggurat-entry time and reapplies it to both final split rows, with an explicit state-finalization fallback if LiveSplit refuses the final native split.

Route file:
  <LiveSplit folder>\poe2_route.txt

When G1_1 is the first route entry, route segments use successor-entry completion: entering G1_1 arms the campaign start, the timer starts when Client.txt logs the Wounded Man's final opening line ("Reach... Clearfell... Find the Miller..."), and entering the next configured route area completes The Riverbank. Detours/revisits do not complete a segment.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 20;

    settings.Add("autoStart", true, "Auto-start after the Wounded Man opening dialogue in The Riverbank");
    settings.Add("routeSplits", true, "Split only on the next expected route area");
    settings.Add("debugLog", true, "Write PoE2 autosplitter diagnostic log");
    settings.Add("validationLog", true, "Write unique-area validation CSV (recommended for v0.2 testing)");
    settings.Add("syncLiveSplitIndex", true, "Follow LiveSplit manual Split / Skip Segment / Undo Split changes");
    settings.Add("pauseAtFinish", true, "Finish fallback: pause after the Ziggurat split if LiveSplit is still running");
    settings.Add("enteredNameFallback", true, "Fallback: detect known areas from Client.txt 'You have entered ...' messages");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.routePath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_route.txt");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_autosplitter_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_autosplitter_debug.log");
    vars.validationPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_area_validation.csv");
    vars.unknownPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_unknown_areas.log");

    vars.reader = null;
    vars.route = new System.Collections.Generic.List<string>();
    vars.routeSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.routeIndex = 0;
    vars.lastLiveSplitIndex = -1;
    vars.routeValid = false;
    vars.successorCompletion = false;
    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "WAITING";
    vars.finishArmed = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;
    vars.seenAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.unknownAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    // Groups: 1=timestamp, 2=generated area level, 3=internal area ID.
    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Generating level (\\d+) area \"([^\"]+)\"",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    vars.loadStartRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Got Instance Details",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    // Fallback for known areas that may appear by display name in Client.txt.
    vars.enteredNameRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*: You have entered (.+)\\.$",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );

    vars.areaNames = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    // Act 1
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
    // Act 2
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
    // Act 3
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
    // Act 4
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
    // Reference-only subarea. Deserted Post is part of Plunder's Point and does not
    // create an independent area transition in Client.txt, so it must not be a route split.
    vars.areaNames["ExpeditionSubArea_Kalguur_Act4"] = "Deserted Post";
    // Interlude 1
    vars.areaNames["P1_Town"] = "The Refuge";
    vars.areaNames["P1_1"] = "Scorched Farmlands";
    vars.areaNames["P1_2"] = "Stones of Serle";
    vars.areaNames["P1_3"] = "The Blackwood";
    vars.areaNames["P1_4"] = "Holten";
    vars.areaNames["P1_5"] = "Wolvenhold";
    vars.areaNames["P1_6"] = "Holten Estate";
    // Interlude 2
    vars.areaNames["P2_Town"] = "The Khari Bazaar";
    vars.areaNames["P2_1"] = "The Khari Crossing";
    vars.areaNames["P2_2"] = "Pools of Khatal";
    vars.areaNames["P2_3"] = "Sel Khari Sanctuary";
    vars.areaNames["P2_5"] = "The Galai Gates";
    vars.areaNames["P2_6"] = "Qimah";
    vars.areaNames["P2_7"] = "Qimah Reservoir";
    // Interlude 3
    vars.areaNames["P3_Town"] = "The Glade";
    vars.areaNames["P3_1"] = "Ashen Forest";
    vars.areaNames["P3_2"] = "Kriar Village";
    vars.areaNames["P3_3"] = "Glacial Tarn";
    vars.areaNames["P3_4"] = "Howling Caves";
    vars.areaNames["P3_5"] = "Kriar Peaks";
    vars.areaNames["P3_6"] = "Etched Ravine";
    vars.areaNames["P3_7"] = "The Cuachic Vault";
    // Endgame
    vars.areaNames["G_Endgame_Town"] = "The Ziggurat Refuge";
    // Known Act 4 progression-state aliases (recognized for diagnostics; not route/checklist entries)
    vars.areaNames["G4_8a"] = "Arastas (pre-progression state)";
    vars.areaNames["G4_11_1a"] = "Ngakanu (pre-progression state)";

    vars.nonSplittableAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.nonSplittableAreaIds.Add("ExpeditionSubArea_Kalguur_Act4");

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
    vars.gtLoadStartRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Got Instance Details",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    vars.gtLoadEndRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*\\[LOADING SCREEN\\] \\((.*?)\\) Duration = ([0-9]+(?:\\.[0-9]+)?) seconds",
        System.Text.RegularExpressions.RegexOptions.Compiled
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

    vars.route = new System.Collections.Generic.List<string>();
    vars.routeSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.routeIndex = 0;
    vars.lastLiveSplitIndex = -1;
    vars.routeValid = false;
    vars.successorCompletion = false;
    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "INITIALIZING";
    vars.finishArmed = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;
    vars.seenAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.unknownAreaIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    try
    {
        if (!System.IO.File.Exists(vars.routePath))
        {
            System.IO.File.WriteAllText(vars.statusPath,
                "ERROR: Route file not found: " + vars.routePath + System.Environment.NewLine);
            print("[PoE2 ASL] Route file not found: " + vars.routePath);
            return;
        }

        var errors = new System.Collections.Generic.List<string>();
        int lineNumber = 0;

        foreach (string rawLine in System.IO.File.ReadAllLines(vars.routePath))
        {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            int comment = line.IndexOf('#');
            if (comment >= 0)
                line = line.Substring(0, comment).Trim();

            if (line.Length == 0)
                continue;

            if (!vars.areaNames.ContainsKey(line))
            {
                errors.Add("Line " + lineNumber + ": unknown area ID " + line);
                continue;
            }

            if (vars.nonSplittableAreaIds.Contains(line))
            {
                errors.Add("Line " + lineNumber + ": " + line + " is a non-splittable subarea and cannot be used as a route entry");
                continue;
            }

            vars.route.Add(line);
            vars.routeSet.Add(line);
        }

        if (errors.Count > 0)
        {
            string errorText = "ERROR: poe2_route.txt contains invalid entries:" + System.Environment.NewLine
                + string.Join(System.Environment.NewLine, errors.ToArray()) + System.Environment.NewLine;
            System.IO.File.WriteAllText(vars.statusPath, errorText);
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " ROUTE_ERROR " + errorText.Replace(System.Environment.NewLine, " | ") + System.Environment.NewLine);
            print("[PoE2 ASL] Route validation failed. See poe2_autosplitter_status.txt");
            return;
        }

        if (vars.route.Count == 0)
        {
            System.IO.File.WriteAllText(vars.statusPath, "ERROR: Route contains zero entries." + System.Environment.NewLine);
            return;
        }

        // Campaign routes that explicitly include Riverbank use successor-entry
        // completion. The active segment is route[routeIndex], and only entry into
        // route[routeIndex + 1] completes it. This prevents town/hideout detours or
        // revisits from being mistaken for area completion. Legacy practice routes
        // that do not start with G1_1 retain entry-based split behavior.
        vars.successorCompletion = vars.route.Count > 1
            && System.String.Equals(vars.route[0], "G1_1", System.StringComparison.OrdinalIgnoreCase);

        vars.routeValid = true;
    }
    catch (System.Exception ex)
    {
        System.IO.File.WriteAllText(vars.statusPath, "ERROR loading route: " + ex.Message + System.Environment.NewLine);
        print("[PoE2 ASL] ERROR loading route: " + ex.Message);
        return;
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
                "Timestamp,AreaId,AreaName,GeneratedLevel,Known,InCurrentRoute,ExpectedAtDetection,DetectionSource" + System.Environment.NewLine);

        // Start each script attachment with a clean unknown-area capture file so
        // validation results from an older pass cannot be mistaken for this one.
        System.IO.File.WriteAllText(vars.unknownPath, "");

        string ready = "READY | v0.2.19-pauseaccounting | Process=" + game.ProcessName
            + " | Client.txt=" + vars.clientLogPath
            + " | Route entries=" + vars.route.Count
            + " | Completion=" + (vars.successorCompletion ? "SUCCESSOR_ENTRY" : "ENTRY");
        System.IO.File.WriteAllText(vars.statusPath, ready + System.Environment.NewLine);
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " " + ready + System.Environment.NewLine);
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

    if (!vars.routeValid || vars.reader == null)
        return false;

    // LiveSplit itself is the source of truth for which segment is active.
    // This keeps the route state aligned when the runner manually uses
    // Split, Skip Segment, or Undo Split. The generated validation .lss has
    // one LiveSplit segment per route entry, so CurrentSplitIndex maps 1:1
    // to routeIndex.
    if (settings["syncLiveSplitIndex"] && !vars.finishComplete && vars.finishStage < 2 && timer.CurrentPhase.ToString() != "NotRunning")
    {
        int liveIndex = timer.CurrentSplitIndex;
        if (liveIndex >= 0 && liveIndex <= vars.route.Count && liveIndex != vars.routeIndex)
        {
            int oldIndex = vars.routeIndex;
            vars.routeIndex = liveIndex;
            vars.lastLiveSplitIndex = liveIndex;

            string action = liveIndex > oldIndex ? "LIVESPLIT_ADVANCE" : "LIVESPLIT_UNDO";
            vars.lastAction = action;

            string nextId = vars.routeIndex < vars.route.Count
                ? (vars.successorCompletion && vars.routeIndex + 1 < vars.route.Count ? vars.route[vars.routeIndex + 1] : vars.route[vars.routeIndex])
                : "<route complete>";
            string nextName = vars.areaNames.ContainsKey(nextId) ? vars.areaNames[nextId] : nextId;

            string syncStatus = "Status: RUNNING" + System.Environment.NewLine
                + "Last detected: " + (vars.currentAreaId == "" ? "<none>" : vars.currentAreaId) + System.Environment.NewLine
                + "Next expected: " + nextId + " | " + nextName + System.Environment.NewLine
                + "Progress: " + vars.routeIndex + " / " + vars.route.Count + System.Environment.NewLine
                + "LiveSplit split index: " + liveIndex + System.Environment.NewLine
                + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine
                + "Last action: " + action + " (route " + oldIndex + " -> " + liveIndex + ")" + System.Environment.NewLine;
            System.IO.File.WriteAllText(vars.statusPath, syncStatus);

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " " + action
                    + " | routeIndex=" + oldIndex + " -> " + liveIndex
                    + " | nextExpected=" + nextId + " " + nextName + System.Environment.NewLine);
        }
        else
        {
            vars.lastLiveSplitIndex = liveIndex;
        }
    }

    // Final Ziggurat completion is an explicit edge case.
    // This version intentionally uses only the segment-walking pattern already
    // proven to load in v0.2.13. It avoids keeping typed ISegment references in
    // local variables, which was the major new construct introduced in v0.2.14.
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
            // Walk the actual LiveSplit run and match by visible segment name.
            // Do not dynamically index timer.Run and do not retain ISegment refs.
            foreach (LiveSplit.Model.ISegment finalSegment in timer.Run)
            {
                if (finalSegment.Name == "The Cuachic Vault")
                {
                    cuachicSegmentIndex = runCount;
                    finalSegment.SplitTime = exact;
                    cuachicStamped = true;
                }
                else if (finalSegment.Name == "The Ziggurat Refuge")
                {
                    zigguratSegmentIndex = runCount;
                    finalSegment.SplitTime = exact;
                    zigguratStamped = true;
                }
                runCount++;
            }

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_RESOLVE"
                    + " | runCount=" + runCount
                    + " | routeCount=" + vars.route.Count
                    + " | liveSplitIndexBefore=" + timer.CurrentSplitIndex
                    + " | cuachicSegmentIndex=" + cuachicSegmentIndex
                    + " | zigguratSegmentIndex=" + zigguratSegmentIndex
                    + " | cuachicStamped=" + (cuachicStamped ? "true" : "false")
                    + " | zigguratStamped=" + (zigguratStamped ? "true" : "false")
                    + System.Environment.NewLine);

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
                // Advance beyond the actual Ziggurat row. If it is the final row,
                // reproduce the final state of a normal last-segment split.
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

                vars.routeIndex = vars.route.Count;
                vars.lastLiveSplitIndex = timer.CurrentSplitIndex;
                vars.finishArmed = false;
                vars.finishComplete = true;
                vars.finishStage = 4;
                vars.lastAction = endedNormally
                    ? "FINISHED / ZIGGURAT STAMPED / TIMER ENDED"
                    : "FINISHED / ZIGGURAT STAMPED / TIMER PAUSED (EXTRA LSS ROWS)";

                string finishStatus = "Status: FINISHED" + System.Environment.NewLine
                    + "Completion condition: ENTERED The Ziggurat Refuge" + System.Environment.NewLine
                    + "Penultimate stamp: P3_7 | The Cuachic Vault" + System.Environment.NewLine
                    + "Final stamp: G_Endgame_Town | The Ziggurat Refuge" + System.Environment.NewLine
                    + "LiveSplit run segments: " + runCount + System.Environment.NewLine
                    + "Route entries: " + vars.route.Count + System.Environment.NewLine
                    + "Ziggurat LiveSplit index: " + zigguratSegmentIndex + System.Environment.NewLine
                    + "LiveSplit split index after commit: " + timer.CurrentSplitIndex + System.Environment.NewLine
                    + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine
                    + "Last action: explicit named-segment final commit" + System.Environment.NewLine;
                System.IO.File.WriteAllText(vars.statusPath, finishStatus);

                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_COMMITTED"
                        + " | runCount=" + runCount
                        + " | routeCount=" + vars.route.Count
                        + " | cuachicSegmentIndex=" + cuachicSegmentIndex
                        + " | zigguratSegmentIndex=" + zigguratSegmentIndex
                        + " | liveSplitIndexAfter=" + timer.CurrentSplitIndex
                        + " | phase=" + timer.CurrentPhase.ToString()
                        + " | cuachicTimestampApplied=" + (cuachicStamped ? "true" : "false")
                        + " | zigguratTimestampApplied=" + (zigguratStamped ? "true" : "false")
                        + " | exactEntryTimeApplied=true"
                        + " | endedNormally=" + (endedNormally ? "true" : "false")
                        + " | uiRefreshNotified=true"
                        + " | RUN_COMPLETE"
                        + System.Environment.NewLine);
            }
        }
        catch (System.Exception ex)
        {
            vars.finishStage = -1;
            vars.lastAction = "FINAL ERROR / NAMED ZIGGURAT COMMIT FAILED";

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_ZIGGURAT_COMMIT_FAILED"
                    + " | " + ex.GetType().FullName + ": " + ex.Message
                    + " | runCount=" + runCount
                    + " | zigguratSegmentIndex=" + zigguratSegmentIndex
                    + " | currentLiveSplitIndex=" + timer.CurrentSplitIndex
                    + " | phase=" + timer.CurrentPhase.ToString()
                    + System.Environment.NewLine);
        }
    }

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

            vars.isLoading = false;
            areaId = vars.areaIdsByName[enteredName];
            detectionSource = "EnteredName";
        }
        else
        {
            continue;
        }

        if (System.String.Equals(areaId, vars.lastAreaId, System.StringComparison.OrdinalIgnoreCase))
            continue;

        vars.lastAreaId = areaId;
        vars.currentAreaId = areaId;
        vars.currentAreaLevel = generatedLevel;
        vars.areaChanged = true;
        vars.lastAction = "DETECTED";

        if (System.String.Equals(areaId, "G1_1", System.StringComparison.OrdinalIgnoreCase)
            && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
        {
            vars.riverbankStartArmed = true;
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " START_ARMED G1_1 The Riverbank | waiting=Wounded Man final line" + System.Environment.NewLine);
        }

        bool known = vars.areaNames.ContainsKey(areaId);
        string detectedName = known ? vars.areaNames[areaId] : "UNKNOWN AREA";
        string activeSegmentId = vars.routeIndex < vars.route.Count ? vars.route[vars.routeIndex] : "<route complete>";
        string expectedId = vars.routeIndex < vars.route.Count
            ? (vars.successorCompletion && vars.routeIndex + 1 < vars.route.Count ? vars.route[vars.routeIndex + 1] : vars.route[vars.routeIndex])
            : "<route complete>";
        string expectedName = vars.areaNames.ContainsKey(expectedId) ? vars.areaNames[expectedId] : expectedId;
        bool inRoute = vars.routeSet.Contains(areaId);

        if (!vars.seenAreaIds.Contains(areaId))
        {
            vars.seenAreaIds.Add(areaId);
            if (settings["validationLog"])
            {
                string safeName = detectedName.Replace("\"", "\"\"");
                string safeExpected = expectedId.Replace("\"", "\"\"");
                System.IO.File.AppendAllText(vars.validationPath,
                    System.DateTime.Now.ToString("s") + ",\"" + areaId + "\",\"" + safeName + "\"," + (generatedLevel >= 0 ? generatedLevel.ToString() : "")
                    + "," + (known ? "true" : "false") + "," + (inRoute ? "true" : "false") + ",\"" + safeExpected + "\",\"" + detectionSource + "\""
                    + System.Environment.NewLine);
            }
        }

        if (!known && !vars.unknownAreaIds.Contains(areaId))
        {
            vars.unknownAreaIds.Add(areaId);
            System.IO.File.AppendAllText(vars.unknownPath,
                System.DateTime.Now.ToString("s") + " UNKNOWN_AREA id=" + areaId
                + " generatedLevel=" + (generatedLevel >= 0 ? generatedLevel.ToString() : "n/a") + " | " + line + System.Environment.NewLine);
        }

        string levelText = generatedLevel >= 0 ? generatedLevel.ToString() : "n/a";
        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Last detected: " + areaId + " | " + detectedName + " | level " + levelText + " | via " + detectionSource + System.Environment.NewLine
            + "Next expected: " + expectedId + " | " + expectedName + System.Environment.NewLine
            + "Progress: " + vars.routeIndex + " / " + vars.route.Count + System.Environment.NewLine
            + "LiveSplit split index: " + timer.CurrentSplitIndex + System.Environment.NewLine
            + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine
            + "Last action: " + vars.lastAction + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " AREA " + areaId + " " + detectedName
                + " | level=" + levelText
                + " | source=" + detectionSource
                + " | expected=" + expectedId + " " + expectedName + System.Environment.NewLine);

        // Dedicated final-sequence state machine.
        // In successor-entry campaign mode, entering Ziggurat is the exact trigger
        // for the active Cuachic segment. The same event also satisfies the terminal
        // Ziggurat row, so the existing two-stage final commit stamps both rows at
        // the same captured entry time. No arbitrary area departure can arm this.
        bool routeEndsAtZigguratUpdate = vars.route.Count >= 2
            && System.String.Equals(vars.route[vars.route.Count - 1], "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase);

        if (vars.successorCompletion
            && routeEndsAtZigguratUpdate
            && vars.routeIndex == vars.route.Count - 2
            && System.String.Equals(areaId, "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase))
        {
            var exactFinishTime = timer.CurrentTime;
            vars.finishRealTime = exactFinishTime.RealTime;
            vars.finishGameTime = exactFinishTime.GameTime;
            vars.finishArmed = true;
            vars.finishStage = 2;
            vars.lastAction = "FINAL SEQUENCE / CUACHIC + ZIGGURAT QUEUED";

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINISH_TRIGGER"
                    + " | entered=G_Endgame_Town The Ziggurat Refuge"
                    + " | activeSegment=P3_7 The Cuachic Vault"
                    + " | completion=SUCCESSOR_ENTRY"
                    + " | liveSplitIndex=" + timer.CurrentSplitIndex
                    + System.Environment.NewLine);
        }
        else if (!vars.successorCompletion)
        {
            // Legacy entry-based behavior retained for older/custom route files.
            if (routeEndsAtZigguratUpdate
                && vars.routeIndex == vars.route.Count - 2
                && System.String.Equals(areaId, "P3_7", System.StringComparison.OrdinalIgnoreCase))
            {
                vars.finishArmed = true;
                vars.finishStage = 1;
                vars.lastAction = "FINISH ARMED / WAITING FOR ZIGGURAT";
            }

            if (routeEndsAtZigguratUpdate
                && vars.finishArmed
                && !vars.finishComplete
                && vars.finishStage == 1
                && System.String.Equals(areaId, "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase))
            {
                var exactFinishTime = timer.CurrentTime;
                vars.finishRealTime = exactFinishTime.RealTime;
                vars.finishGameTime = exactFinishTime.GameTime;
                vars.finishStage = 2;
                vars.lastAction = "FINAL SEQUENCE / SPLIT 1 QUEUED";
            }
        }
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
    if (!settings["routeSplits"] || !vars.routeValid)
        return false;

    // Final sequence uses two explicit native ASL split requests on consecutive
    // update cycles. These requests intentionally ignore areaChanged because both
    // splits are caused by the single transition into The Ziggurat Refuge.
    if (vars.finishStage == 2)
    {
        vars.finishStage = 20; // waiting for onSplit confirmation of split #1
        vars.lastAction = "FINAL SPLIT 1 REQUESTED / CUACHIC";
        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " FINAL_SPLIT_1_REQUEST"
                + " | currentLiveSplitIndex=" + timer.CurrentSplitIndex
                + " | stamps=P3_7 The Cuachic Vault"
                + System.Environment.NewLine);
        return true;
    }

    if (!vars.areaChanged)
        return false;

    if (vars.routeIndex >= vars.route.Count)
        return false;

    string segmentId = vars.route[vars.routeIndex];
    string expectedId = vars.successorCompletion
        ? (vars.routeIndex + 1 < vars.route.Count ? vars.route[vars.routeIndex + 1] : segmentId)
        : segmentId;
    bool routeEndsAtZiggurat = vars.route.Count >= 2
        && System.String.Equals(vars.route[vars.route.Count - 1], "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase);
    bool atPenultimateRouteEntry = routeEndsAtZiggurat && vars.routeIndex == vars.route.Count - 2;

    // Edge-case finish semantics: entering Cuachic arms the ending but does not
    // stamp it. Its timestamp is intentionally taken when the player exits Cuachic
    // by entering Ziggurat. The Ziggurat event then drives the two forced splits above.
    if (!vars.successorCompletion
        && atPenultimateRouteEntry
        && System.String.Equals(vars.currentAreaId, "P3_7", System.StringComparison.OrdinalIgnoreCase))
    {
        vars.finishArmed = true;
        vars.finishStage = 1;
        vars.lastAction = "FINISH ARMED / WAITING FOR ZIGGURAT";
        return false;
    }

    // If Ziggurat is seen outside the queued state, suppress ordinary route logic;
    // update{} is responsible for moving finishStage 1 -> 2.
    if (routeEndsAtZiggurat
        && System.String.Equals(vars.currentAreaId, "G_Endgame_Town", System.StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (System.String.Equals(vars.currentAreaId, expectedId, System.StringComparison.OrdinalIgnoreCase))
    {
        string name = vars.areaNames.ContainsKey(segmentId) ? vars.areaNames[segmentId] : segmentId;
        string triggerName = vars.areaNames.ContainsKey(expectedId) ? vars.areaNames[expectedId] : expectedId;
        int completedIndex = vars.routeIndex;

        vars.routeIndex++;
        vars.lastAction = "SPLIT";

        string nextId = vars.routeIndex < vars.route.Count ? vars.route[vars.routeIndex] : "<route complete>";
        string nextName = vars.areaNames.ContainsKey(nextId) ? vars.areaNames[nextId] : nextId;
        string levelText = vars.currentAreaLevel >= 0 ? vars.currentAreaLevel.ToString() : "n/a";
        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Last detected: " + vars.currentAreaId + " | " + name + " | level " + levelText + System.Environment.NewLine
            + "Next expected: " + nextId + " | " + nextName + System.Environment.NewLine
            + "Progress: " + vars.routeIndex + " / " + vars.route.Count + System.Environment.NewLine
            + "LiveSplit split index: " + timer.CurrentSplitIndex + System.Environment.NewLine
            + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine
            + "Last action: SPLIT" + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);

        if (settings["debugLog"])
            System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " SPLIT " + segmentId + " " + name
                + " | trigger=" + expectedId + " " + triggerName
                + " | routeIndex=" + completedIndex + System.Environment.NewLine);

        return true;
    }

    vars.lastAction = "IGNORE";
    if (settings["debugLog"])
    {
        string detectedName = vars.areaNames.ContainsKey(vars.currentAreaId) ? vars.areaNames[vars.currentAreaId] : "UNKNOWN AREA";
        string expectedName = vars.areaNames.ContainsKey(expectedId) ? vars.areaNames[expectedId] : expectedId;
        System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " IGNORE " + vars.currentAreaId + " " + detectedName
            + " | stillExpected=" + expectedId + " " + expectedName + System.Environment.NewLine);
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
    // Confirmation for forced final split #1. LiveSplit has already stamped the
    // current Cuachic segment and incremented CurrentSplitIndex before this event.
    if (vars.finishStage == 20)
    {
        int expectedIndex = vars.route.Count - 1;
        if (timer.CurrentSplitIndex == expectedIndex)
        {
            vars.routeIndex = expectedIndex;
            vars.lastLiveSplitIndex = timer.CurrentSplitIndex;
            vars.lastAction = "FINAL SPLIT 1 COMMITTED / ZIGGURAT ACTIVE";

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_SPLIT_1_COMMITTED"
                    + " | stamped=P3_7 The Cuachic Vault"
                    + " | liveSplitIndex=" + timer.CurrentSplitIndex
                    + " | next=G_Endgame_Town The Ziggurat Refuge"
                    + System.Environment.NewLine);

            // Queue the final Ziggurat commit for update{}. Do NOT invoke another
            // TimerModel.Split() from inside onSplit: LiveSplit testing showed that
            // re-entrant split call never returns on this edge case.
            vars.finishStage = 21;
            vars.finalForceNotBefore = System.DateTime.Now.AddMilliseconds(100);
            vars.lastAction = "FINAL SPLIT 1 COMMITTED / ZIGGURAT FINALIZATION QUEUED";

            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_SPLIT_2_QUEUED"
                    + " | liveSplitIndex=" + timer.CurrentSplitIndex
                    + " | notBefore=" + vars.finalForceNotBefore.ToString("o")
                    + " | context=update"
                    + System.Environment.NewLine);
        }
        else
        {
            vars.finishStage = -1;
            vars.lastAction = "FINAL ERROR / SPLIT 1 INDEX MISMATCH";
            if (settings["debugLog"])
                System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " FINAL_SPLIT_1_FAILED"
                    + " | actualIndex=" + timer.CurrentSplitIndex
                    + " | expectedIndex=" + expectedIndex
                    + " | phase=" + timer.CurrentPhase.ToString()
                    + System.Environment.NewLine);
        }
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

    vars.routeIndex = timer.CurrentSplitIndex >= 0 ? timer.CurrentSplitIndex : 0;
    vars.lastLiveSplitIndex = timer.CurrentSplitIndex;
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

    vars.routeIndex = 0;
    vars.lastLiveSplitIndex = -1;
    vars.currentAreaId = "";
    vars.currentAreaLevel = 0;
    vars.lastAreaId = "";
    vars.areaChanged = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.isLoading = false;
    vars.lastAction = "RESET";
    vars.finishArmed = false;
    vars.finishComplete = false;
    vars.finishStage = 0;
    vars.finishRealTime = null;
    vars.finishGameTime = null;
    vars.finalForceNotBefore = System.DateTime.MinValue;

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
