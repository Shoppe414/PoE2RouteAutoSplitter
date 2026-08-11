/*
Path of Exile 2 BossRush bridge for LiveSplit
BossRush v0.2.0 | Campaign Required Bosses Only v0.5 - Predefined

Detector/bridge baseline preserved from v0.1.14:
- Snapshot-polls poe2_boss_events.log and processes only new lines.
- Mode whitelist: Campaign Required Bosses Only v0.5 - Predefined (40 targets).
- Dynamic boss-row naming default: false.
- Keeps split-time diagnostics from v0.1.4.
- Backdates Real Time to the watcher's firstMissing timestamp after the disappearance is confirmed.
- Reuses the exact same stored Real Time for queued GONE events with an identical firstMissing timestamp.
- Retained: unread GONE lines are preserved when one split is pending, allowing two queued dual-boss deaths to split on consecutive updates.
- This bridge expects LiveSplit Timing Method = Real Time; Game Time is not initialized yet.
- Campaign-only addition: Riverbank auto-start is detected directly from Client.txt; BossWatcher is still used only for boss events.

Reads <LiveSplit folder>\poe2_boss_events.log written by PoE2BossWatcher.
Every first-time GONE event whose boss ID is allowed by this mode can cause one LiveSplit split while the timer is running.
Campaign timer auto-starts when Client.txt reports entry into G1_1 (The Riverbank). Manual start remains available, and the auto-start setting can be disabled.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 30;
    settings.Add("autoStart", true, "Auto-start when entering The Riverbank");
    settings.Add("dynamicSegmentNames", false, "Rename the current split row to the detected boss");
    settings.Add("debugLog", true, "Write poe2_boss_bridge_debug.log");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.eventPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_events.log");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_bridge_debug.log");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_bridge_status.txt");
    vars.clientLogPath = "";
    vars.clientReader = null;
    vars.startTrigger = false;

    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.modeName = "Campaign Required Bosses Only v0.5 - Predefined";
    vars.modeExpectedCount = 40;
    vars.allowedBosses = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.allowedBosses.Add("the_bloated_miller");
    vars.allowedBosses.Add("the_rust_king");
    vars.allowedBosses.Add("draven");
    vars.allowedBosses.Add("asinia");
    vars.allowedBosses.Add("lachlann");
    vars.allowedBosses.Add("the_executioner");
    vars.allowedBosses.Add("count_geonor");
    vars.allowedBosses.Add("rathbreaker");
    vars.allowedBosses.Add("rudja_the_dread_engineer");
    vars.allowedBosses.Add("jamanra_risen_king");
    vars.allowedBosses.Add("azarian_forsaken_son");
    vars.allowedBosses.Add("iktab_the_deathlord");
    vars.allowedBosses.Add("ekbab_ancient_steed");
    vars.allowedBosses.Add("zalmarath_the_colossus");
    vars.allowedBosses.Add("tor_gul_the_defiler");
    vars.allowedBosses.Add("jamanra_abomination");
    vars.allowedBosses.Add("xyclucian_the_chimera");
    vars.allowedBosses.Add("zicoatl_warden_core");
    vars.allowedBosses.Add("queen_of_filth");
    vars.allowedBosses.Add("ketzuli_high_priest_sun");
    vars.allowedBosses.Add("viper_napuatzi");
    vars.allowedBosses.Add("doryani_royal_thaumaturge");
    vars.allowedBosses.Add("the_prisoner");
    vars.allowedBosses.Add("krutog_lord_of_kin");
    vars.allowedBosses.Add("scourge_of_the_sky");
    vars.allowedBosses.Add("torvian_hand_saviour");
    vars.allowedBosses.Add("benedictus_first_herald");
    vars.allowedBosses.Add("tavakai_the_chieftain");
    vars.allowedBosses.Add("isolde_white_shroud");
    vars.allowedBosses.Add("heldra_black_pyre");
    vars.allowedBosses.Add("siora_blade_mists");
    vars.allowedBosses.Add("thane_wulfric");
    vars.allowedBosses.Add("lady_elswyth");
    vars.allowedBosses.Add("elzarah_cobra_lord");
    vars.allowedBosses.Add("vornas_fell_flame");
    vars.allowedBosses.Add("azmadi_faridun_prince");
    vars.allowedBosses.Add("rakkar_frozen_talon");
    vars.allowedBosses.Add("stormgore_guardian");
    vars.allowedBosses.Add("zelina_blood_priestess");
    vars.allowedBosses.Add("zolin_blood_priest");
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.nameById = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.lastObservedIndex = -1;
    vars.lastUndoneBossId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();
    vars.processedLineCount = 0;
    vars.nextPollUtc = System.DateTime.MinValue;
    vars.ready = false;
}

init
{
    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.nameById = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.lastUndoneBossId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();
    vars.lastObservedIndex = timer.CurrentSplitIndex;
    vars.nextPollUtc = System.DateTime.MinValue;
    vars.ready = false;
    vars.startTrigger = false;

    try
    {
        foreach (LiveSplit.Model.ISegment segment in timer.Run) vars.baseSegmentNames.Add(segment.Name);

        if (!System.IO.File.Exists(vars.eventPath))
        {
            System.IO.File.WriteAllText(vars.eventPath, "# Created by LiveSplit Boss Watcher bridge" + System.Environment.NewLine);
        }

        // Ignore pre-existing events. Only events appended after the ASL attaches are actionable.
        string[] existing = System.IO.File.ReadAllLines(vars.eventPath);
        vars.processedLineCount = existing.Length;
        vars.ready = true;

        string ready = "READY | BossRushBridge v0.2.0 | Mode=" + vars.modeName
            + " | Targets=" + vars.modeExpectedCount
            + " | EventFile=" + vars.eventPath
            + " | ExistingLines=" + vars.processedLineCount
            + " | Segments=" + vars.baseSegmentNames.Count;
        System.IO.File.WriteAllText(vars.statusPath, ready + System.Environment.NewLine);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " " + ready + System.Environment.NewLine);
        print("[PoE2 Boss Bridge] " + ready);
    }
    catch (System.Exception ex)
    {
        vars.ready = false;
        System.IO.File.WriteAllText(vars.statusPath, "ERROR | " + ex.ToString() + System.Environment.NewLine);
        print("[PoE2 Boss Bridge] ERROR: " + ex.Message);
    }

    // Campaign Boss Rush uses the same game-log start signal as campaign exploration:
    // entry into G1_1 (The Riverbank). Seek to EOF so attaching the ASL never
    // replays an old Riverbank entry from a previous run.
    try
    {
        try { if (vars.clientReader != null) vars.clientReader.Dispose(); } catch { }

        string gameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.clientLogPath = System.IO.Path.Combine(gameDir, "logs", "Client.txt");
        var clientFs = new System.IO.FileStream(
            vars.clientLogPath,
            System.IO.FileMode.Open,
            System.IO.FileAccess.Read,
            System.IO.FileShare.ReadWrite
        );
        clientFs.Seek(0, System.IO.SeekOrigin.End);
        vars.clientReader = new System.IO.StreamReader(clientFs);

        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " AUTOSTART_READY | Client.txt=" + vars.clientLogPath
            + " | trigger=G1_1 The Riverbank" + System.Environment.NewLine);
    }
    catch (System.Exception startEx)
    {
        vars.clientReader = null;
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " AUTOSTART_UNAVAILABLE | " + startEx.Message
            + " | manual timer start remains available" + System.Environment.NewLine);
        print("[PoE2 Boss Bridge] Riverbank auto-start unavailable: " + startEx.Message);
    }
}

update
{
    vars.startTrigger = false;

    // Keep the Client.txt reader caught up even while the timer is already running.
    // The trigger is deliberately an AREA-ENTRY signal, not an area-departure signal.
    if (vars.clientReader != null)
    {
        try
        {
            string clientLine;
            while ((clientLine = vars.clientReader.ReadLine()) != null)
            {
                bool riverbankInternal = clientLine.IndexOf("area \"G1_1\"", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && clientLine.IndexOf("Generating level", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool riverbankEntered = clientLine.IndexOf("You have entered The Riverbank.", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (riverbankInternal || riverbankEntered)
                {
                    vars.startTrigger = true;
                    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " RIVERBANK_ENTRY | source="
                        + (riverbankInternal ? "GeneratingLevel" : "EnteredName")
                        + " | phase=" + timer.CurrentPhase.ToString() + System.Environment.NewLine);
                }
            }
        }
        catch (System.Exception startReadEx)
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " AUTOSTART_READ_ERROR | " + startReadEx.Message
                + System.Environment.NewLine);
        }
    }

    if (!vars.ready) return false;

    int idx = timer.CurrentSplitIndex;

    // One-step/multi-step Undo: re-arm autosplit bosses that correspond to removed split rows.
    if (idx < vars.lastObservedIndex)
    {
        int undoCount = vars.lastObservedIndex - idx;
        while (undoCount > 0 && vars.completedOrder.Count > 0)
        {
            string id = vars.completedOrder[vars.completedOrder.Count - 1];
            vars.completedOrder.RemoveAt(vars.completedOrder.Count - 1);
            vars.completed.Remove(id);
            vars.lastUndoneBossId = id;
            string n = vars.nameById.ContainsKey(id) ? vars.nameById[id] : id;
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " UNDO_REARM | boss=" + id + " " + n
                + " | liveSplitIndex=" + idx + System.Environment.NewLine);
            undoCount--;
        }
    }
    else if (idx > vars.lastObservedIndex && vars.pendingBossId == "" && vars.lastUndoneBossId != "")
    {
        vars.suppressed.Add(vars.lastUndoneBossId);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " MANUAL_SKIP_SUPPRESS | boss=" + vars.lastUndoneBossId
            + " | liveSplitIndex=" + idx + System.Environment.NewLine);
        vars.lastUndoneBossId = "";
    }
    vars.lastObservedIndex = idx;

    if (vars.pendingBossId != "") return true;

    // Poll at ~10 Hz. The watcher confirmation is short and split time is backdated to firstMissing
    // and avoids keeping a StreamReader parked at EOF on a growing file.
    var nowUtc = System.DateTime.UtcNow;
    if (nowUtc < vars.nextPollUtc) return true;
    vars.nextPollUtc = nowUtc.AddMilliseconds(100);

    try
    {
        string[] lines = System.IO.File.ReadAllLines(vars.eventPath);

        // If the event file was replaced/truncated, re-baseline rather than indexing past the start.
        if (lines.Length < vars.processedLineCount)
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " EVENT_FILE_RESET | oldLineCount=" + vars.processedLineCount
                + " | newLineCount=" + lines.Length + System.Environment.NewLine);
            vars.processedLineCount = lines.Length;
            return true;
        }

        int startLine = vars.processedLineCount;

        for (int j = startLine; j < lines.Length; j++)
        {
            string line = lines[j];
            // Advance only through the line actually inspected. If a GONE event requests a
            // split and we break, later appended/queued GONE lines remain unread for the next
            // update. This is required for simultaneous dual-boss deaths (2 -> 0).
            vars.processedLineCount = j + 1;

            if (line == null || line.Trim() == "" || line.StartsWith("#")) continue;

            string[] parts = line.Split('|');
            if (parts.Length < 4) continue;

            string type = parts[1].Trim();
            if (!System.String.Equals(type, "GONE", System.StringComparison.OrdinalIgnoreCase)) continue;

            string bossId = parts[2].Trim();
            string bossName = parts[3].Trim();
            if (bossId == "") continue;
            vars.nameById[bossId] = bossName;

            if (!vars.allowedBosses.Contains(bossId))
            {
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_NOT_IN_MODE | mode=" + vars.modeName
                    + " | boss=" + bossId + " " + bossName + System.Environment.NewLine);
                continue;
            }

            bool hasFirstMissing = false;
            System.DateTimeOffset firstMissing = System.DateTimeOffset.MinValue;
            for (int k = 4; k < parts.Length; k++)
            {
                string extra = parts[k].Trim();
                if (!extra.StartsWith("firstMissing=", System.StringComparison.OrdinalIgnoreCase)) continue;
                string value = extra.Substring("firstMissing=".Length);
                System.DateTimeOffset parsed;
                if (System.DateTimeOffset.TryParse(value, out parsed))
                {
                    firstMissing = parsed;
                    hasFirstMissing = true;
                    break;
                }
            }

            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " GONE_EVENT_READ | boss=" + bossId + " " + bossName
                + " | line=" + j + " | phase=" + timer.CurrentPhase.ToString()
                + " | liveSplitIndex=" + timer.CurrentSplitIndex + System.Environment.NewLine);

            if (vars.completed.Contains(bossId))
            {
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_ALREADY_COMPLETED | boss=" + bossId + " " + bossName + System.Environment.NewLine);
                continue;
            }
            if (vars.suppressed.Contains(bossId))
            {
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_SUPPRESSED | boss=" + bossId + " " + bossName + System.Environment.NewLine);
                continue;
            }
            if (timer.CurrentPhase != LiveSplit.Model.TimerPhase.Running && timer.CurrentPhase != LiveSplit.Model.TimerPhase.Paused)
            {
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " IGNORE_TIMER_NOT_RUNNING | boss=" + bossId + " " + bossName
                    + " | phase=" + timer.CurrentPhase.ToString() + System.Environment.NewLine);
                continue;
            }

            vars.pendingBossId = bossId;
            vars.pendingBossName = bossName;
            vars.pendingHasFirstMissing = hasFirstMissing;
            vars.pendingFirstMissing = firstMissing;
            vars.lastUndoneBossId = "";

            // Cosmetic only. Never let a row-renaming failure prevent the split request.
            if (settings["dynamicSegmentNames"])
            {
                try
                {
                    int i = 0;
                    foreach (LiveSplit.Model.ISegment segment in timer.Run)
                    {
                        if (i == timer.CurrentSplitIndex)
                        {
                            segment.Name = bossName;
                            timer.Run.HasChanged = true;
                            timer.CallRunManuallyModified();
                            break;
                        }
                        i++;
                    }
                }
                catch (System.Exception renameEx)
                {
                    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                        System.DateTime.Now.ToString("s") + " NAME_RENAME_FAILED | boss=" + bossId
                        + " | error=" + renameEx.Message + System.Environment.NewLine);
                }
            }

            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " BOSS_SPLIT_REQUESTED | boss=" + bossId + " " + bossName
                + " | liveSplitIndex=" + timer.CurrentSplitIndex + System.Environment.NewLine);
            break;
        }
    }
    catch (System.Exception ex)
    {
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " EVENT_POLL_ERROR | " + ex.ToString() + System.Environment.NewLine);
    }

    return true;
}

start
{
    if (settings["autoStart"] && vars.startTrigger)
    {
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " START G1_1 The Riverbank" + System.Environment.NewLine);
        return true;
    }
}

split
{
    return vars.pendingBossId != "";
}

onSplit
{
    if (vars.pendingBossId != "")
    {
        string id = vars.pendingBossId;
        string name = vars.pendingBossName;

        // A normal TimerModel.Split() stamps SplitTime BEFORE firing OnSplit.
        // Verify that stamp here. If a LiveSplit/ASL edge case advanced the index
        // without a usable RealTime stamp, repair only that just-completed row.
        int completedIndex = timer.CurrentSplitIndex - 1;
        bool segmentFound = false;
        bool repairedTime = false;
        bool backdatedTime = false;
        bool sameTimeReuse = false;
        double backdateMs = 0.0;
        string nativeReal = "<not found>";
        string nativeGame = "<not found>";
        string finalReal = "<not found>";
        string finalGame = "<not found>";

        try
        {
            int i = 0;
            foreach (LiveSplit.Model.ISegment completedSegment in timer.Run)
            {
                if (i == completedIndex)
                {
                    segmentFound = true;
                    nativeReal = completedSegment.SplitTime.RealTime.HasValue
                        ? completedSegment.SplitTime.RealTime.Value.ToString()
                        : "null";
                    nativeGame = completedSegment.SplitTime.GameTime.HasValue
                        ? completedSegment.SplitTime.GameTime.Value.ToString()
                        : "null";

                    if (!completedSegment.SplitTime.RealTime.HasValue
                        || completedSegment.SplitTime.RealTime.Value <= System.TimeSpan.Zero)
                    {
                        completedSegment.SplitTime = new LiveSplit.Model.Time(
                            timer.CurrentTime.RealTime,
                            timer.CurrentTime.GameTime
                        );
                        timer.Run.HasChanged = true;
                        repairedTime = true;
                    }

                    // The watcher waits briefly to confirm that the boss UI really stayed gone.
                    // Preserve that safety delay without charging it to the run: use the watcher
                    // firstMissing wall-clock timestamp to backdate the just-completed Real Time.
                    if (vars.pendingHasFirstMissing && completedSegment.SplitTime.RealTime.HasValue)
                    {
                        // Dual 2->0 can queue two GONE events with the exact same firstMissing.
                        // Reuse the first event's adjusted Real Time so both completed rows store
                        // exactly the same timestamp instead of differing by ASL processing latency.
                        if (vars.sameTimeCacheValid && vars.pendingFirstMissing == vars.sameTimeCacheFirstMissing)
                        {
                            completedSegment.SplitTime = new LiveSplit.Model.Time(
                                vars.sameTimeCacheReal,
                                completedSegment.SplitTime.GameTime
                            );
                            timer.Run.HasChanged = true;
                            backdatedTime = true;
                            sameTimeReuse = true;
                            backdateMs = 0.0;
                        }
                        else
                        {
                            System.TimeSpan wallDelay = System.DateTimeOffset.Now - vars.pendingFirstMissing;
                            if (wallDelay >= System.TimeSpan.Zero && wallDelay <= System.TimeSpan.FromSeconds(5))
                            {
                                System.TimeSpan adjustedReal = completedSegment.SplitTime.RealTime.Value - wallDelay;
                                if (adjustedReal < System.TimeSpan.Zero) adjustedReal = System.TimeSpan.Zero;
                                completedSegment.SplitTime = new LiveSplit.Model.Time(
                                    adjustedReal,
                                    completedSegment.SplitTime.GameTime
                                );
                                timer.Run.HasChanged = true;
                                backdatedTime = true;
                                backdateMs = wallDelay.TotalMilliseconds;
                                vars.sameTimeCacheValid = true;
                                vars.sameTimeCacheFirstMissing = vars.pendingFirstMissing;
                                vars.sameTimeCacheReal = adjustedReal;
                            }
                        }
                    }

                    finalReal = completedSegment.SplitTime.RealTime.HasValue
                        ? completedSegment.SplitTime.RealTime.Value.ToString()
                        : "null";
                    finalGame = completedSegment.SplitTime.GameTime.HasValue
                        ? completedSegment.SplitTime.GameTime.Value.ToString()
                        : "null";
                    break;
                }
                i++;
            }
        }
        catch (System.Exception stampEx)
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " SPLIT_TIME_VERIFY_ERROR | boss=" + id
                + " | completedIndex=" + completedIndex
                + " | error=" + stampEx.ToString() + System.Environment.NewLine);
        }

        if (repairedTime || backdatedTime)
        {
            try { timer.CallRunManuallyModified(); } catch { }
        }

        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " SPLIT_TIME_VERIFY | boss=" + id + " " + name
            + " | completedIndex=" + completedIndex
            + " | segmentFound=" + segmentFound
            + " | nativeReal=" + nativeReal
            + " | nativeGame=" + nativeGame
            + " | repaired=" + repairedTime
            + " | backdated=" + backdatedTime
            + " | sameTimeReuse=" + sameTimeReuse
            + " | backdateMs=" + backdateMs.ToString("F1")
            + " | finalReal=" + finalReal
            + " | finalGame=" + finalGame
            + " | currentTimerReal=" + (timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : "null")
            + " | currentTimerGame=" + (timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : "null")
            + System.Environment.NewLine);

        vars.completed.Add(id);
        vars.completedOrder.Add(id);
        vars.nameById[id] = name;
        vars.pendingBossId = "";
        vars.pendingBossName = "";
        vars.pendingHasFirstMissing = false;
        vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
        vars.lastObservedIndex = timer.CurrentSplitIndex;

        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Mode: " + vars.modeName + System.Environment.NewLine
            + "Targets: " + vars.modeExpectedCount + System.Environment.NewLine
            + "Last boss: " + id + " | " + name + System.Environment.NewLine
            + "Autosplit bosses: " + vars.completed.Count + System.Environment.NewLine
            + "Suppressed/skipped bosses: " + vars.suppressed.Count + System.Environment.NewLine
            + "LiveSplit index: " + timer.CurrentSplitIndex + System.Environment.NewLine
            + "Last split RealTime: " + finalReal + System.Environment.NewLine
            + "Last split GameTime: " + finalGame + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);

        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " BOSS_SPLIT_COMMITTED | boss=" + id + " " + name
            + " | liveSplitIndex=" + timer.CurrentSplitIndex
            + " | phase=" + timer.CurrentPhase.ToString() + System.Environment.NewLine);
    }
}

onReset
{
    vars.startTrigger = false;
    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.nameById = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.lastUndoneBossId = "";
    vars.lastObservedIndex = timer.CurrentSplitIndex;

    try
    {
        vars.processedLineCount = System.IO.File.Exists(vars.eventPath)
            ? System.IO.File.ReadAllLines(vars.eventPath).Length
            : 0;
    }
    catch { }

    if (settings["dynamicSegmentNames"])
    {
        try
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
        catch { }
    }

    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
        System.DateTime.Now.ToString("s") + " RESET | eventLineBaseline=" + vars.processedLineCount + System.Environment.NewLine);
}
