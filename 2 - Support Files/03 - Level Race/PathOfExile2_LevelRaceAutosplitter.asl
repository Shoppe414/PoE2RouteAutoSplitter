/*
Path of Exile 2 Level Race AutoSplitter for LiveSplit
v1.1.0 validation build

- Tails Path of Exile 2 logs/Client.txt.
- Auto-starts on The Riverbank (optional).
- Splits when Client.txt reports a configured milestone level.
- Milestones are read from <LiveSplit folder>\poe2_level_race.txt.
- The last configured milestone is the target level.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 20;
    settings.Add("autoStart", true, "Auto-start when entering The Riverbank");
    settings.Add("debugLog", true, "Write Level Race diagnostic log");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.configPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_level_race.txt");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_level_race_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_level_race_debug.log");

    vars.reader = null;
    vars.milestones = new System.Collections.Generic.List<int>();
    vars.milestoneSet = new System.Collections.Generic.HashSet<int>();
    vars.completedLevels = new System.Collections.Generic.HashSet<int>();
    vars.pendingLevel = -1;
    vars.targetLevel = -1;
    vars.startTrigger = false;
    vars.configValid = false;

    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Generating level (\\d+) area \\\"([^\\\"]+)\\\"",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    vars.levelRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).* is now level (\\d+)$",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
}

init
{
    vars.milestones = new System.Collections.Generic.List<int>();
    vars.milestoneSet = new System.Collections.Generic.HashSet<int>();
    vars.completedLevels = new System.Collections.Generic.HashSet<int>();
    vars.pendingLevel = -1;
    vars.targetLevel = -1;
    vars.startTrigger = false;
    vars.configValid = false;

    try
    {
        if (!System.IO.File.Exists(vars.configPath))
            throw new System.Exception("Missing poe2_level_race.txt beside LiveSplit.exe");

        foreach (string raw in System.IO.File.ReadAllLines(vars.configPath))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            line = line.Trim();
            if (line == "") continue;

            int level;
            if (!System.Int32.TryParse(line, out level) || level < 2 || level > 100)
                throw new System.Exception("Invalid milestone level: " + line);

            if (vars.milestoneSet.Add(level))
                vars.milestones.Add(level);
        }

        vars.milestones.Sort();
        if (vars.milestones.Count == 0)
            throw new System.Exception("No milestone levels configured");
        vars.targetLevel = vars.milestones[vars.milestones.Count - 1];

        int runCount = 0;
        foreach (LiveSplit.Model.ISegment segment in timer.Run) runCount++;
        if (runCount != vars.milestones.Count)
            throw new System.Exception("LiveSplit segment count (" + runCount + ") must equal milestone count (" + vars.milestones.Count + ")");

        string gameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.clientLogPath = System.IO.Path.Combine(gameDir, "logs", "Client.txt");
        var fs = new System.IO.FileStream(vars.clientLogPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        fs.Seek(0, System.IO.SeekOrigin.End);
        vars.reader = new System.IO.StreamReader(fs);
        vars.configValid = true;

        string ready = "READY | v1.1.0 | Mode=LEVEL_RACE | Target=" + vars.targetLevel
            + " | Milestones=" + vars.milestones.Count + " | Client.txt=" + vars.clientLogPath;
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
        print("[PoE2 ASL] LEVEL_RACE ERROR: " + ex.Message);
    }
}

update
{
    vars.startTrigger = false;
    if (!vars.configValid || vars.reader == null) return false;
    if (vars.pendingLevel > 0) return true;

    while (vars.reader.Peek() >= 0)
    {
        string line = vars.reader.ReadLine();
        var am = vars.areaRegex.Match(line);
        if (am.Success && am.Groups[3].Value == "G1_1")
        {
            vars.startTrigger = true;
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " START_TRIGGER G1_1 The Riverbank" + System.Environment.NewLine);
            break;
        }

        var lm = vars.levelRegex.Match(line);
        if (lm.Success)
        {
            int level = System.Int32.Parse(lm.Groups[2].Value);
            if (vars.milestoneSet.Contains(level) && !vars.completedLevels.Contains(level))
            {
                vars.pendingLevel = level;
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                    System.DateTime.Now.ToString("s") + " LEVEL_MILESTONE " + level
                    + " | liveSplitIndex=" + timer.CurrentSplitIndex + System.Environment.NewLine);
                break;
            }
        }
    }
    return true;
}

start
{
    if (settings["autoStart"] && vars.startTrigger) return true;
}

split
{
    return vars.pendingLevel > 0;
}

onSplit
{
    if (vars.pendingLevel > 0)
    {
        int level = vars.pendingLevel;
        vars.completedLevels.Add(level);
        vars.pendingLevel = -1;
        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Last milestone: Level " + level + System.Environment.NewLine
            + "Target: Level " + vars.targetLevel + System.Environment.NewLine
            + "Completed milestones: " + vars.completedLevels.Count + " / " + vars.milestones.Count + System.Environment.NewLine
            + "Timer phase: " + timer.CurrentPhase.ToString() + System.Environment.NewLine;
        System.IO.File.WriteAllText(vars.statusPath, status);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " LEVEL_SPLIT_COMMITTED " + level
            + " | liveSplitIndex=" + timer.CurrentSplitIndex
            + " | phase=" + timer.CurrentPhase.ToString()
            + (level == vars.targetLevel ? " | TARGET_REACHED" : "")
            + System.Environment.NewLine);
    }
}

onReset
{
    vars.completedLevels = new System.Collections.Generic.HashSet<int>();
    vars.pendingLevel = -1;
    vars.startTrigger = false;
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
