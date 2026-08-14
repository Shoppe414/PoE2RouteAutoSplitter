/*
Path of Exile 2 BossRush bridge for LiveSplit
BossRush v0.2.0 | Pinnacle v0.5 - Dynamic

Detector/bridge baseline preserved from v0.1.14:
- Snapshot-polls poe2_boss_events.log and processes only new lines.
- Mode whitelist: Pinnacle v0.5 - Dynamic (10 targets).
- Dynamic boss-row naming default: true.
- Keeps split-time diagnostics from v0.1.4.
- Backdates Real Time and Game Time to the watcher's firstMissing timestamp after the disappearance is confirmed.
- Reuses the exact same stored Real/Game Time for queued GONE events with an identical firstMissing timestamp.
- Retained: unread GONE lines are preserved when one split is pending, allowing two queued dual-boss deaths to split on consecutive updates.
- Game Time load removal is available from Client.txt; BossWatcher still controls boss events only.

Reads <LiveSplit folder>\poe2_boss_events.log written by PoE2BossWatcher.
Every first-time GONE event whose boss ID is allowed by this mode can cause one LiveSplit split while the timer is running.
Manual timer start is intentional in BossRush v0.2.0.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 30;
    settings.Add("dynamicSegmentNames", true, "Rename the current split row to the detected boss");
    settings.Add("debugLog", true, "Write poe2_boss_bridge_debug.log");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.eventPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_events.log");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_bridge_debug.log");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_bridge_status.txt");

    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
    vars.modeName = "Pinnacle v0.5 - Dynamic";
    vars.modeExpectedCount = 10;
    vars.allowedBosses = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.allowedBosses.Add("atziri_red_queen");
    vars.allowedBosses.Add("the_aberration");
    vars.allowedBosses.Add("arbiter_of_ash");
    vars.allowedBosses.Add("arbiter_of_divinity");
    vars.allowedBosses.Add("the_bodach");
    vars.allowedBosses.Add("raven_trickster");
    vars.allowedBosses.Add("the_trialmaster");
    vars.allowedBosses.Add("vessel_of_kulemak");
    vars.allowedBosses.Add("xesht_we_that_are_one");
    vars.allowedBosses.Add("zarokh_temporal");
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

    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.nameById = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.lastUndoneBossId = "";
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();
    vars.lastObservedIndex = timer.CurrentSplitIndex;
    vars.nextPollUtc = System.DateTime.MinValue;
    vars.ready = false;

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
                    // firstMissing wall-clock timestamp to backdate the just-completed Real Time and Game Time.
                    if (vars.pendingHasFirstMissing && completedSegment.SplitTime.RealTime.HasValue)
                    {
                        // Dual 2->0 can queue two GONE events with the exact same firstMissing.
                        // Reuse the first event's adjusted Real/Game Time so both completed rows store
                        // exactly the same timestamp instead of differing by ASL processing latency.
                        if (vars.sameTimeCacheValid && vars.pendingFirstMissing == vars.sameTimeCacheFirstMissing)
                        {
                            System.TimeSpan? reusedGame = completedSegment.SplitTime.GameTime;
                            if (vars.sameTimeCacheHasGame)
                                reusedGame = vars.sameTimeCacheGame;
                            completedSegment.SplitTime = new LiveSplit.Model.Time(
                                vars.sameTimeCacheReal,
                                reusedGame
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

                                System.TimeSpan? adjustedGame = completedSegment.SplitTime.GameTime;
                                if (adjustedGame.HasValue)
                                {
                                    System.TimeSpan gameAtFirstMissing = adjustedGame.Value - wallDelay;
                                    if (gameAtFirstMissing < System.TimeSpan.Zero) gameAtFirstMissing = System.TimeSpan.Zero;
                                    adjustedGame = gameAtFirstMissing;
                                }

                                completedSegment.SplitTime = new LiveSplit.Model.Time(
                                    adjustedReal,
                                    adjustedGame
                                );
                                timer.Run.HasChanged = true;
                                backdatedTime = true;
                                backdateMs = wallDelay.TotalMilliseconds;
                                vars.sameTimeCacheValid = true;
                                vars.sameTimeCacheFirstMissing = vars.pendingFirstMissing;
                                vars.sameTimeCacheReal = adjustedReal;
                                vars.sameTimeCacheHasGame = adjustedGame.HasValue;
                                vars.sameTimeCacheGame = adjustedGame.HasValue ? adjustedGame.Value : System.TimeSpan.Zero;
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

    // Reset attempt-local correction state, but preserve an active Riverbank load
    // that began before the timer start event.
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

    vars.pendingBossId = "";
    vars.pendingBossName = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
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
