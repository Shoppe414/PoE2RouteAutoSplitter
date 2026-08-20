using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PoE2RouteSetup;

public static class LiveSplitFiles
{
    private static readonly Regex LiveSplitPathRegex = new(
        "System\\.IO\\.Path\\.Combine\\(vars\\.liveSplitDir,\\s*\"([^\"]+)\"\\)",
        RegexOptions.Compiled);

    private static readonly Regex ManualPauseDefaultRegex = new(
        "settings\\.Add\\(\"manualPauseRemoval\",\\s*(?:true|false),",
        RegexOptions.Compiled);

    private static readonly Regex AutoStartDefaultRegex = new(
        "settings\\.Add\\(\"autoStart\",\\s*(?:true|false),",
        RegexOptions.Compiled);

    private static readonly Regex AutoStartSettingRegex = new(
        "settings\\.Add\\(\"autoStart\",\\s*(?:true|false),[^\r\n;]*\\);",
        RegexOptions.Compiled);

    private static readonly Regex RouteStartDirectiveRegex = new(
        "(?im)^(\\s*@start=)[^\\r\\n#]+",
        RegexOptions.Compiled);

    public static string RewriteRuntimePaths(string aslText, string targetDir)
    {
        var targetPath = Path.GetFullPath(targetDir);
        var userSetupDir = Directory.GetParent(targetPath)?.FullName;
        var releaseRoot = userSetupDir is null ? null : Directory.GetParent(userSetupDir)?.FullName;
        var diagnosticsDir = releaseRoot is null
            ? targetPath
            : Path.Combine(releaseRoot, "4-README's_and_Diagnostics", "Diagnostics");

        return LiveSplitPathRegex.Replace(aslText, match =>
        {
            var fileName = match.Groups[1].Value;
            var isDiagnostic = fileName.EndsWith("_debug.log", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("poe2_unknown_areas.log", StringComparison.OrdinalIgnoreCase);
            var path = Path.Combine(isDiagnostic ? diagnosticsDir : targetPath, fileName);
            return QuoteCSharp(path);
        });
    }

    public static bool SupportsRuntimeStartPolicy(string aslText) =>
        aslText.Contains("vars.startAreaId", StringComparison.Ordinal)
        && aslText.Contains("@start", StringComparison.OrdinalIgnoreCase);

    public static string ApplyAutoStartOption(string aslText, bool autoStart)
    {
        if (!AutoStartDefaultRegex.IsMatch(aslText))
        {
            if (autoStart)
                throw new InvalidOperationException("The selected autosplitter does not expose the expected auto-start setting.");
            return aslText;
        }

        var replacement = "settings.Add(\"autoStart\", " + (autoStart ? "true" : "false") + ",";
        return AutoStartDefaultRegex.Replace(aslText, replacement, 1);
    }

    public static string ApplyRouteStartPolicy(string runtimeText, StartPolicy startPolicy)
    {
        if (!RouteStartDirectiveRegex.IsMatch(runtimeText))
            return runtimeText;

        return RouteStartDirectiveRegex.Replace(
            runtimeText,
            match => match.Groups[1].Value + startPolicy.RouteDirectiveValue,
            1);
    }

    /// <summary>
    /// Adds a setup-generated area-entry start reader to autosplitters that do not already
    /// support a configurable @start area. The reader is independent from every route,
    /// BossWatcher, and Game Time Client.txt reader so start detection cannot consume events
    /// needed by the rest of the autosplitter.
    /// </summary>
    public static string ApplyGeneratedZoneStartPolicy(string aslText, StartPolicy startPolicy)
    {
        // Every SetupUI-generated run uses one authoritative Client.txt start reader.
        // Keep the source ASL's autoStart checkbox as the user-facing control, but relabel
        // it to the rule actually selected in SetupUI so a non-Riverbank setup can never
        // appear as though Riverbank Start is still the active default.
        var startSettingAvailable = AutoStartSettingRegex.IsMatch(aslText);
        if (startSettingAvailable)
        {
            var startSettingLabel = startPolicy.Mode switch
            {
                StartMode.Manual => "Timer start: Manual Start (selected in SetupUI)",
                StartMode.Riverbank => "Timer start: Riverbank Start — Wounded Man final opening line",
                StartMode.ZoneEntry => "Timer start: First Split Zone Entry Auto Start — " + (startPolicy.AreaName ?? startPolicy.AreaId ?? "selected zone"),
                _ => "Timer start"
            };
            var startSettingDefault = startPolicy.IsAutomatic ? "true" : "false";
            aslText = AutoStartSettingRegex.Replace(
                aslText,
                "settings.Add(\"autoStart\", " + startSettingDefault + ", " + QuoteCSharp(startSettingLabel) + ");",
                1);
        }

        var enabled = startPolicy.IsAutomatic ? "true" : "false";
        var areaId = startPolicy.AreaId ?? "";
        var areaName = startPolicy.AreaName ?? "";
        var wildcardAreaId = areaId.EndsWith("*", StringComparison.Ordinal);
        var areaNeedle = wildcardAreaId
            ? "area \"" + areaId[..^1]
            : "area \"" + areaId + "\"";

        var startupCode = string.Join(Environment.NewLine, new[]
        {
            "    // SETUP_START_POLICY_BEGIN - generated by PoE2RouteSetup",
            $"    vars.setupStartEnabled = {enabled};",
            $"    vars.setupStartUsesSetting = {(startSettingAvailable ? "true" : "false")};",
            $"    vars.setupStartAreaId = {QuoteCSharp(areaId)};",
            $"    vars.setupStartAreaName = {QuoteCSharp(areaName)};",
            $"    vars.setupStartAreaNeedle = {QuoteCSharp(areaNeedle)};",
            "    vars.setupStartReader = null;",
            "    vars.setupStartTrigger = false;",
            "    vars.setupStartRiverbankArmed = false;",
            "    // SETUP_START_POLICY_END"
        });
        aslText = InsertAfterActionOpen(aslText, "startup", startupCode, required: true);

        var initCode = string.Join(Environment.NewLine, new[]
        {
            "    // SETUP_START_READER_INIT_BEGIN",
            "    try { if (vars.setupStartReader != null) vars.setupStartReader.Close(); } catch {}",
            "    vars.setupStartReader = null;",
            "    vars.setupStartTrigger = false;",
            "    vars.setupStartRiverbankArmed = false;",
            "    if (vars.setupStartEnabled)",
            "    {",
            "        try",
            "        {",
            "            string setupStartGameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);",
            "            string setupStartClientPath = System.IO.Path.Combine(setupStartGameDir, \"logs\", \"Client.txt\");",
            "            var setupStartFs = new System.IO.FileStream(setupStartClientPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);",
            "            setupStartFs.Seek(0, System.IO.SeekOrigin.End);",
            "            vars.setupStartReader = new System.IO.StreamReader(setupStartFs);",
            "        }",
            "        catch { vars.setupStartReader = null; }",
            "    }",
            "    // SETUP_START_READER_INIT_END"
        });
        aslText = InsertAfterActionOpen(aslText, "init", initCode, required: true);

        var riverbankInternalNeedle = QuoteCSharp("area \"G1_1\"");
        var riverbankEnteredNeedle = QuoteCSharp("You have entered The Riverbank.");
        var woundedNeedle = QuoteCSharp("Wounded Man: Reach... Clearfell... Find the Miller...");
        var startSettingGuard = startSettingAvailable ? "settings[\"autoStart\"]" : "true";

        var updateLines = new List<string>
        {
            "    // SETUP_START_READER_UPDATE_BEGIN",
            "    vars.setupStartTrigger = false;",
            $"    bool setupStartRuleActive = vars.setupStartEnabled && {startSettingGuard};",
            "    if (setupStartRuleActive && vars.setupStartReader != null)",
            "    {",
            "        int setupStartProcessed = 0;",
            "        string setupStartLine = null;",
            "        while (setupStartProcessed < 500 && (setupStartLine = vars.setupStartReader.ReadLine()) != null)",
            "        {",
            "            setupStartProcessed++;",
            "",
            "            // Always drain the independent reader while a run is active so a reset",
            "            // cannot replay an old zone entry and immediately restart the timer.",
            "            if (timer.CurrentPhase != LiveSplit.Model.TimerPhase.NotRunning) continue;",
            "",
            "            if (System.String.Equals(vars.setupStartAreaId, \"G1_1\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            $"                bool setupRiverbankInternalEntry = (System.Text.RegularExpressions.Regex.IsMatch(setupStartLine, @\"\\s2caa[0-9A-Fa-f]{4}\\s\") || setupStartLine.IndexOf(\"Generating level\", System.StringComparison.OrdinalIgnoreCase) >= 0)",
            $"                    && setupStartLine.IndexOf({riverbankInternalNeedle}, System.StringComparison.OrdinalIgnoreCase) >= 0;",
            $"                bool setupRiverbankEntry = setupRiverbankInternalEntry",
            $"                    || setupStartLine.IndexOf({riverbankEnteredNeedle}, System.StringComparison.OrdinalIgnoreCase) >= 0;",
            "                if (setupRiverbankEntry)",
            "                {",
            "                    vars.setupStartRiverbankArmed = true;",
            "                    continue;",
            "                }",
            "",
            $"                if (vars.setupStartRiverbankArmed && setupStartLine.IndexOf({woundedNeedle}, System.StringComparison.OrdinalIgnoreCase) >= 0)",
            "                {",
            "                    vars.setupStartTrigger = true;",
            "                    vars.setupStartRiverbankArmed = false;",
            "                    break;",
            "                }",
            "            }",
            "            else if (vars.setupStartAreaId != \"\")",
            "            {",
            "                bool setupInternalEntry = (System.Text.RegularExpressions.Regex.IsMatch(setupStartLine, @\"\\s2caa[0-9A-Fa-f]{4}\\s\") || setupStartLine.IndexOf(\"Generating level\", System.StringComparison.OrdinalIgnoreCase) >= 0)",
            "                    && setupStartLine.IndexOf(vars.setupStartAreaNeedle, System.StringComparison.OrdinalIgnoreCase) >= 0;",
            "                bool setupNamedEntry = setupStartLine.IndexOf(\"You have entered \" + vars.setupStartAreaName + \".\", System.StringComparison.OrdinalIgnoreCase) >= 0;",
            "                if (setupInternalEntry || setupNamedEntry)",
            "                {",
            "                    vars.setupStartTrigger = true;",
            "                    break;",
            "                }",
            "            }",
            "        }",
            "    }"
        };

        // Ordered/Flexible exploration and the mixed objective engine also own a Client.txt
        // reader. While an automatic setup start is waiting, keep that original reader at EOF
        // but do not let it mutate split state. The independent setup reader therefore owns the
        // exact transition that starts timing, including generated premade/custom runs.
        if (aslText.Contains("vars.reader", StringComparison.Ordinal))
        {
            updateLines.AddRange(new[]
            {
                "    if (setupStartRuleActive && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)",
                "    {",
                "        if (vars.reader != null)",
                "        {",
                "            try { while (vars.reader.ReadLine() != null) {} } catch {}",
                "        }",
                "        return true;",
                "    }"
            });
        }

        updateLines.Add("    // SETUP_START_READER_UPDATE_END");
        var updateCode = string.Join(Environment.NewLine, updateLines);
        const string updateAnchor = "    if (!vars.configValid || vars.reader == null) return false;";
        if (!aslText.Contains(updateAnchor, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected post-Game-Time update anchor for Maps policy v2.");
        aslText = aslText.Replace(updateAnchor, updateAnchor + Environment.NewLine + Environment.NewLine + updateCode, StringComparison.Ordinal);

        var startCode = $"    if (vars.setupStartEnabled && {startSettingGuard} && vars.setupStartTrigger) return true; // SETUP_START_POLICY";
        if (HasAction(aslText, "start"))
        {
            aslText = InsertAfterActionOpen(aslText, "start", startCode, required: true);
        }
        else
        {
            var newline = DetectNewline(aslText);
            var startBlock = "start" + newline + "{" + newline + startCode + newline + "}" + newline + newline;
            aslText = InsertBeforeAction(aslText, "split", startBlock);
        }

        // Flexible exploration has one fewer timed area row because its start area is
        // implicitly satisfied. Preserve that invariant for Riverbank or any selected
        // zone that is part of the enabled unordered area pool.
        if (aslText.Contains("vars.completedAreaIds", StringComparison.Ordinal)
            && aslText.Contains("vars.subgroupCompleted", StringComparison.Ordinal)
            && HasAction(aslText, "onStart"))
        {
            var flexibleOnStartCode = string.Join(Environment.NewLine, new[]
            {
                "    // SETUP_START_FLEXIBLE_IMPLICIT_OBJECTIVE_BEGIN",
                "    if (vars.setupStartEnabled",
                "        && vars.setupStartAreaId != \"\"",
                "        && vars.enabledAreaIds.Contains(vars.setupStartAreaId)",
                "        && !vars.completedAreaIds.Contains(vars.setupStartAreaId))",
                "    {",
                "        vars.completedAreaIds.Add(vars.setupStartAreaId);",
                "        string setupStartSubgroup = vars.areaSubgroup.ContainsKey(vars.setupStartAreaId) ? vars.areaSubgroup[vars.setupStartAreaId] : \"\";",
                "        if (setupStartSubgroup != \"\")",
                "            vars.subgroupCompleted[setupStartSubgroup] = vars.subgroupCompleted[setupStartSubgroup] + 1;",
                "    }",
                "    // SETUP_START_FLEXIBLE_IMPLICIT_OBJECTIVE_END"
            });
            aslText = InsertAfterActionOpen(aslText, "onStart", flexibleOnStartCode, required: true);
        }

        if (HasAction(aslText, "onReset"))
        {
            const string resetCode = "    vars.setupStartTrigger = false; vars.setupStartRiverbankArmed = false; // SETUP_START_POLICY_RESET";
            aslText = InsertAfterActionOpen(aslText, "onReset", resetCode, required: true);
        }

        return aslText;
    }

    /// <summary>
    /// Injects a timing-neutral run-audit layer into every SetupUI-generated ASL.
    /// The audit log is append-only and each event is SHA-256 chained to the previous
    /// event. A readable summary and a final checksum manifest are written under
    /// "3 - verification files" when the run finishes, resets, or the ASL shuts down.
    /// </summary>
    public static string ApplyRunAuditPolicy(string aslText, string targetDir, string modeName, string packageVersion)
    {
        if (aslText.Contains("RUN_AUDIT_POLICY_BEGIN", StringComparison.Ordinal))
            return aslText;

        if (!HasAction(aslText, "startup") || !HasAction(aslText, "onStart")
            || !HasAction(aslText, "onSplit") || !HasAction(aslText, "onReset"))
            throw new InvalidOperationException("The selected ASL does not expose the actions required for run validation logging.");

        var targetPath = Path.GetFullPath(targetDir);
        var userSetupDir = Directory.GetParent(targetPath)?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate 1 - User Setup from LiveSplit Target.");
        var packageRoot = Directory.GetParent(userSetupDir)?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the release package root from LiveSplit Target.");
        var outputDir = Path.Combine(packageRoot, "3 - verification files");
        var setupManifest = Path.Combine(outputDir, "poe2_setup_validation.sha256");
        var currentSession = Path.Combine(outputDir, "poe2_run_current.txt");

        var startupCode = string.Join(Environment.NewLine, new[]
        {
            "    // RUN_AUDIT_POLICY_BEGIN - generated by PoE2RouteSetup",
            "    settings.Add(\"runAudit\", true, \"Write SHA-256 chained run log + validation manifest\");",
            $"    vars.auditPackageVersion = {QuoteCSharp(packageVersion)};",
            $"    vars.auditModeName = {QuoteCSharp(modeName)};",
            $"    vars.auditOutputDir = {QuoteCSharp(outputDir)};",
            $"    vars.auditSetupManifestPath = {QuoteCSharp(setupManifest)};",
            $"    vars.auditCurrentSessionPath = {QuoteCSharp(currentSession)};",
            "    vars.auditActive = false;",
            "    vars.auditFinalized = false;",
            "    vars.auditRunId = \"\";",
            "    vars.auditLogPath = \"\";",
            "    vars.auditSummaryPath = \"\";",
            "    vars.auditChecksumPath = \"\";",
            "    vars.auditRunSetupManifestPath = \"\";",
            "    vars.auditSequence = 0;",
            "    vars.auditPreviousHash = new string('0', 64);",
            "    vars.auditStartedUtc = System.DateTimeOffset.MinValue;",
            "    vars.auditSetupHashAtStart = \"<missing>\";",
            "    vars.auditSetupValidationAtStart = \"NOT_CHECKED\";",
            "    vars.auditSummaryLines = new System.Collections.Generic.List<string>();",
            "",
            "    vars.auditEscape = (System.Func<string, string>)(value =>",
            "    {",
            "        if (value == null) return \"\";",
            "        return value.Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"|\", \"\\\\|\").Replace(\"\\r\", \"\\\\r\").Replace(\"\\n\", \"\\\\n\");",
            "    });",
            "",
            "    vars.auditSha256Text = (System.Func<string, string>)(value =>",
            "    {",
            "        using (var sha = System.Security.Cryptography.SHA256.Create())",
            "        {",
            "            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value ?? \"\");",
            "            byte[] hash = sha.ComputeHash(bytes);",
            "            return System.BitConverter.ToString(hash).Replace(\"-\", \"\").ToLowerInvariant();",
            "        }",
            "    });",
            "",
            "    vars.auditSha256File = (System.Func<string, string>)(path =>",
            "    {",
            "        try",
            "        {",
            "            using (var sha = System.Security.Cryptography.SHA256.Create())",
            "            using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))",
            "            {",
            "                byte[] hash = sha.ComputeHash(stream);",
            "                return System.BitConverter.ToString(hash).Replace(\"-\", \"\").ToLowerInvariant();",
            "            }",
            "        }",
            "        catch { return \"<missing>\"; }",
            "    });",
            "",
            "    vars.auditValidateSetup = (System.Func<string>)(() =>",
            "    {",
            "        try",
            "        {",
            "            if (!System.IO.File.Exists(vars.auditSetupManifestPath)) return \"MISSING_MANIFEST\";",
            "            string baseDir = System.IO.Path.GetDirectoryName(vars.auditSetupManifestPath);",
            "            string packageRoot = System.IO.Directory.GetParent(baseDir).FullName;",
            "            foreach (string raw in System.IO.File.ReadAllLines(vars.auditSetupManifestPath))",
            "            {",
            "                string line = raw == null ? \"\" : raw.Trim();",
            "                if (line == \"\" || line.StartsWith(\"#\")) continue;",
            "                if (line.Length < 66) return \"INVALID_MANIFEST_LINE\";",
            "                string expected = line.Substring(0, 64).ToLowerInvariant();",
            "                string relative = line.Substring(64).TrimStart(' ', '*', '\\t');",
            "                if (relative == \"\") return \"INVALID_MANIFEST_PATH\";",
            "                string candidate = System.IO.Path.Combine(packageRoot, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));",
            "                string actual = ((System.Func<string, string>)vars.auditSha256File)(candidate);",
            "                if (!System.String.Equals(expected, actual, System.StringComparison.OrdinalIgnoreCase))",
            "                    return \"MISMATCH:\" + relative;",
            "            }",
            "            return \"OK\";",
            "        }",
            "        catch (System.Exception ex) { return \"ERROR:\" + ex.GetType().Name; }",
            "    });",
            "",
            "    vars.auditAppend = (System.Action<string, string, string, string>)((eventType, gameTimeText, realTimeText, details) =>",
            "    {",
            "        if (!vars.auditActive || vars.auditFinalized) return /* ASL void lambda: keep return void */;",
            "        try",
            "        {",
            "            vars.auditSequence++;",
            "            string canonical = \"seq=\" + vars.auditSequence.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            "                + \"|utc=\" + System.DateTimeOffset.UtcNow.ToString(\"o\")",
            "                + \"|event=\" + ((System.Func<string, string>)vars.auditEscape)(eventType)",
            "                + \"|runId=\" + ((System.Func<string, string>)vars.auditEscape)(vars.auditRunId)",
            "                + \"|gameTime=\" + ((System.Func<string, string>)vars.auditEscape)(gameTimeText)",
            "                + \"|realTime=\" + ((System.Func<string, string>)vars.auditEscape)(realTimeText)",
            "                + \"|details=\" + ((System.Func<string, string>)vars.auditEscape)(details);",
            "            string eventHash = ((System.Func<string, string>)vars.auditSha256Text)(vars.auditPreviousHash + \"\\n\" + canonical);",
            "            string fullLine = \"prev=\" + vars.auditPreviousHash + \"|hash=\" + eventHash + \"|\" + canonical;",
            "            System.IO.File.AppendAllText(vars.auditLogPath, fullLine + System.Environment.NewLine, new System.Text.UTF8Encoding(false));",
            "            vars.auditPreviousHash = eventHash;",
            "        }",
            "        catch {}",
            "    });",
            "",
            "    vars.auditFinalize = (System.Action<string, string, string>)((result, gameTimeText, realTimeText) =>",
            "    {",
            "        if (!vars.auditActive || vars.auditFinalized) return /* ASL void lambda: keep return void */;",
            "        try",
            "        {",
            "            string setupValidationNow = ((System.Func<string>)vars.auditValidateSetup)();",
            "            string setupHashNow = ((System.Func<string, string>)vars.auditSha256File)(vars.auditSetupManifestPath);",
            "            ((System.Action<string, string, string, string>)vars.auditAppend)(\"RUN_END\", gameTimeText, realTimeText,",
            "                \"result=\" + result + \";setupValidation=\" + setupValidationNow + \";setupManifestHash=\" + setupHashNow);",
            "",
            "            var summary = new System.Text.StringBuilder();",
            "            summary.AppendLine(\"PoE2 Route AutoSplitter - Run Validation Summary\");",
            "            summary.AppendLine(\"Run ID: \" + vars.auditRunId);",
            "            summary.AppendLine(\"Package version: \" + vars.auditPackageVersion);",
            "            summary.AppendLine(\"Mode: \" + vars.auditModeName);",
            "            summary.AppendLine(\"Result: \" + result);",
            "            summary.AppendLine(\"Started UTC: \" + vars.auditStartedUtc.ToString(\"o\"));",
            "            summary.AppendLine(\"Finished UTC: \" + System.DateTimeOffset.UtcNow.ToString(\"o\"));",
            "            summary.AppendLine(\"Final Game Time: \" + gameTimeText);",
            "            summary.AppendLine(\"Final Real Time: \" + realTimeText);",
            "            summary.AppendLine(\"Setup validation at start: \" + vars.auditSetupValidationAtStart);",
            "            summary.AppendLine(\"Setup validation at finish: \" + setupValidationNow);",
            "            summary.AppendLine(\"Setup manifest SHA256 at start: \" + vars.auditSetupHashAtStart);",
            "            summary.AppendLine(\"Setup manifest SHA256 at finish: \" + setupHashNow);",
            "            summary.AppendLine(\"Final event-chain hash: \" + vars.auditPreviousHash);",
            "            summary.AppendLine();",
            "            summary.AppendLine(\"Committed splits:\");",
            "            if (vars.auditSummaryLines.Count == 0) summary.AppendLine(\"  (none)\");",
            "            foreach (string line in vars.auditSummaryLines) summary.AppendLine(\"  \" + line);",
            "            summary.AppendLine();",
            "            summary.AppendLine(\"Validation note: SHA-256 and the event hash chain provide integrity/audit evidence, not tamper-proof anti-cheat proof.\");",
            "            System.IO.File.WriteAllText(vars.auditSummaryPath, summary.ToString(), new System.Text.UTF8Encoding(false));",
            "",
            "            string logHash = ((System.Func<string, string>)vars.auditSha256File)(vars.auditLogPath);",
            "            string summaryHash = ((System.Func<string, string>)vars.auditSha256File)(vars.auditSummaryPath);",
            "            var manifest = new System.Text.StringBuilder();",
            "            manifest.AppendLine(\"# PoE2 Route AutoSplitter run checksum manifest\");",
            "            manifest.AppendLine(\"# RunId=\" + vars.auditRunId);",
            "            manifest.AppendLine(\"# Result=\" + result);",
            "            manifest.AppendLine(\"# FinalEventHash=\" + vars.auditPreviousHash);",
            "            manifest.AppendLine(logHash + \"  \" + System.IO.Path.GetFileName(vars.auditLogPath));",
            "            manifest.AppendLine(summaryHash + \"  \" + System.IO.Path.GetFileName(vars.auditSummaryPath));",
            "            if (setupHashNow != \"<missing>\")",
            "                manifest.AppendLine(setupHashNow + \"  \" + System.IO.Path.GetFileName(vars.auditSetupManifestPath));",
            "            System.IO.File.WriteAllText(vars.auditChecksumPath, manifest.ToString(), new System.Text.UTF8Encoding(false));",
            "",
            "            vars.auditFinalized = true;",
            "            string session = \"runId=\" + vars.auditRunId + System.Environment.NewLine",
            "                + \"state=\" + result + System.Environment.NewLine",
            "                + \"mode=\" + vars.auditModeName + System.Environment.NewLine",
            "                + \"log=\" + vars.auditLogPath + System.Environment.NewLine",
            "                + \"summary=\" + vars.auditSummaryPath + System.Environment.NewLine",
            "                + \"checksums=\" + vars.auditChecksumPath + System.Environment.NewLine",
            "                + \"finalEventHash=\" + vars.auditPreviousHash + System.Environment.NewLine;",
            "            System.IO.File.WriteAllText(vars.auditCurrentSessionPath, session, new System.Text.UTF8Encoding(false));",
            "        }",
            "        catch { vars.auditFinalized = true; }",
            "    });",
            "    // RUN_AUDIT_POLICY_END"
        });
        aslText = InsertAfterActionOpen(aslText, "startup", startupCode, required: true);

        var onStartCode = string.Join(Environment.NewLine, new[]
        {
            "    // RUN_AUDIT_ON_START_BEGIN",
            "    if (settings[\"runAudit\"])",
            "    {",
            "        try",
            "        {",
            "            System.IO.Directory.CreateDirectory(vars.auditOutputDir);",
            "            vars.auditRunId = System.DateTimeOffset.UtcNow.ToString(\"yyyyMMdd-HHmmss\") + \"-\" + System.Guid.NewGuid().ToString(\"N\").Substring(0, 8).ToUpperInvariant();",
            "            string auditStem = \"poe2_run_\" + vars.auditRunId;",
            "            vars.auditLogPath = System.IO.Path.Combine(vars.auditOutputDir, auditStem + \".log\");",
            "            vars.auditSummaryPath = System.IO.Path.Combine(vars.auditOutputDir, auditStem + \"_summary.txt\");",
            "            vars.auditChecksumPath = System.IO.Path.Combine(vars.auditOutputDir, auditStem + \".sha256\");",
            "            vars.auditRunSetupManifestPath = System.IO.Path.Combine(vars.auditOutputDir, auditStem + \"_setup.sha256\");",
            "            if (System.IO.File.Exists(vars.auditSetupManifestPath))",
            "                System.IO.File.Copy(vars.auditSetupManifestPath, vars.auditRunSetupManifestPath, true);",
            "            if (System.IO.File.Exists(vars.auditRunSetupManifestPath))",
            "                vars.auditSetupManifestPath = vars.auditRunSetupManifestPath;",
            "            vars.auditSequence = 0;",
            "            vars.auditPreviousHash = new string('0', 64);",
            "            vars.auditStartedUtc = System.DateTimeOffset.UtcNow;",
            "            vars.auditSummaryLines = new System.Collections.Generic.List<string>();",
            "            vars.auditFinalized = false;",
            "            vars.auditActive = true;",
            "            vars.auditSetupHashAtStart = ((System.Func<string, string>)vars.auditSha256File)(vars.auditSetupManifestPath);",
            "            vars.auditSetupValidationAtStart = ((System.Func<string>)vars.auditValidateSetup)();",
            "            System.IO.File.WriteAllText(vars.auditLogPath, \"\", new System.Text.UTF8Encoding(false));",
            "            string auditGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : \"null\";",
            "            string auditReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : \"null\";",
            "            ((System.Action<string, string, string, string>)vars.auditAppend)(\"RUN_START\", auditGame, auditReal,",
            "                \"version=\" + vars.auditPackageVersion + \";mode=\" + vars.auditModeName",
            "                + \";setupValidation=\" + vars.auditSetupValidationAtStart",
            "                + \";setupManifestHash=\" + vars.auditSetupHashAtStart);",
            "            string session = \"runId=\" + vars.auditRunId + System.Environment.NewLine",
            "                + \"state=RUNNING\" + System.Environment.NewLine",
            "                + \"mode=\" + vars.auditModeName + System.Environment.NewLine",
            "                + \"log=\" + vars.auditLogPath + System.Environment.NewLine",
            "                + \"summary=\" + vars.auditSummaryPath + System.Environment.NewLine",
            "                + \"checksums=\" + vars.auditChecksumPath + System.Environment.NewLine;",
            "            System.IO.File.WriteAllText(vars.auditCurrentSessionPath, session, new System.Text.UTF8Encoding(false));",
            "        }",
            "        catch { vars.auditActive = false; vars.auditFinalized = false; }",
            "    }",
            "    // RUN_AUDIT_ON_START_END"
        });
        aslText = InsertAfterActionOpen(aslText, "onStart", onStartCode, required: true);

        var onSplitCode = string.Join(Environment.NewLine, new[]
        {
            "    // RUN_AUDIT_ON_SPLIT_BEGIN",
            "    if (vars.auditActive && !vars.auditFinalized)",
            "    {",
            "        try",
            "        {",
            "            int auditCompletedIndex = timer.CurrentSplitIndex - 1;",
            "            string auditSegmentName = \"<unknown>\";",
            "            string auditGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : \"null\";",
            "            string auditReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : \"null\";",
            "            if (auditCompletedIndex >= 0)",
            "            {",
            "                int auditIndex = 0;",
            "                foreach (LiveSplit.Model.ISegment auditSegment in timer.Run)",
            "                {",
            "                    if (auditIndex == auditCompletedIndex)",
            "                    {",
            "                        auditSegmentName = auditSegment.Name ?? \"<unnamed>\";",
            "                        if (auditSegment.SplitTime.GameTime.HasValue) auditGame = auditSegment.SplitTime.GameTime.Value.ToString();",
            "                        if (auditSegment.SplitTime.RealTime.HasValue) auditReal = auditSegment.SplitTime.RealTime.Value.ToString();",
            "                        break;",
            "                    }",
            "                    auditIndex++;",
            "                }",
            "            }",
            "            string auditDetails = \"index=\" + (auditCompletedIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)",
            "                + \";segment=\" + auditSegmentName;",
            "            ((System.Action<string, string, string, string>)vars.auditAppend)(\"SPLIT\", auditGame, auditReal, auditDetails);",
            "            vars.auditSummaryLines.Add((auditCompletedIndex + 1).ToString(\"D3\") + \". \" + auditSegmentName + \" | Game Time \" + auditGame + \" | Real Time \" + auditReal);",
            "",
            "            if (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Ended)",
            "                ((System.Action<string, string, string>)vars.auditFinalize)(\"FINISHED\", auditGame, auditReal);",
            "        }",
            "        catch {}",
            "    }",
            "    // RUN_AUDIT_ON_SPLIT_END"
        });
        aslText = InsertBeforeActionClose(aslText, "onSplit", onSplitCode, required: true);

        var onResetCode = string.Join(Environment.NewLine, new[]
        {
            "    // RUN_AUDIT_ON_RESET_BEGIN",
            "    if (vars.auditActive && !vars.auditFinalized)",
            "    {",
            "        string auditGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : \"null\";",
            "        string auditReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : \"null\";",
            "        ((System.Action<string, string, string>)vars.auditFinalize)(\"RESET\", auditGame, auditReal);",
            "    }",
            "    vars.auditActive = false;",
            "    // RUN_AUDIT_ON_RESET_END"
        });
        aslText = InsertAfterActionOpen(aslText, "onReset", onResetCode, required: true);

        foreach (var terminalAction in new[] { "exit", "shutdown" })
        {
            if (!HasAction(aslText, terminalAction)) continue;
            var terminalCode = string.Join(Environment.NewLine, new[]
            {
                $"    // RUN_AUDIT_{terminalAction.ToUpperInvariant()}_BEGIN",
                "    if (vars.auditActive && !vars.auditFinalized)",
                "    {",
                "        string auditGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : \"null\";",
                "        string auditReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : \"null\";",
                $"        ((System.Action<string, string, string>)vars.auditFinalize)({QuoteCSharp(terminalAction.ToUpperInvariant())}, auditGame, auditReal);",
                "    }",
                $"    // RUN_AUDIT_{terminalAction.ToUpperInvariant()}_END"
            });
            aslText = InsertBeforeActionClose(aslText, terminalAction, terminalCode, required: false);
        }

        return aslText;
    }

    /// <summary>
    /// Applies the dedicated Maps lifecycle policy used by SetupUI-generated Maps runs.
    /// The base mixed ASL remains unchanged for every other run type. Maps receives an
    /// independent Client.txt reader, independent BossWatcher event cursor, dynamic
    /// LiveSplit rows, map+seed identity, recognized map-linked child instances,
    /// provisional-exit rollback, Vaal Ruins exit-boundary handling, and optional
    /// character-specific death tracking.
    /// </summary>
    public static string ApplyMapsPolicyV2(string aslText)
    {
        const string mapBossParserNeedle = "            else if (type == \"mapboss\")";
        if (!aslText.Contains(mapBossParserNeedle, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected objective parser for Maps policy v2.");

        const string objectiveSplitNeedle = "            string[] parts = line.Split(new char[] { '|' }, 2);";
        if (!aslText.Contains(objectiveSplitNeedle, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected route directive parser for Maps policy v2.");

        // A maprun row is only a dynamic LiveSplit placeholder. It intentionally does not
        // enable the legacy mapboss objective path; the generated Maps policy owns map
        // classification, BossWatcher context, qualification, and split timing itself.
        var mapRunParser = string.Join(Environment.NewLine, new[]
        {
            "            else if (type == \"maprun\")",
            "            {",
            "                int mapRunSlot;",
            "                if (!System.Int32.TryParse(id, out mapRunSlot) || mapRunSlot != 1)",
            "                    throw new System.Exception(\"maprun must use the single dynamic placeholder maprun|1\");",
            "                if (!vars.mapPolicyV2Enabled)",
            "                    throw new System.Exception(\"maprun requires @mapPolicy=v2\");",
            "            }"
        });
        aslText = aslText.Replace(mapBossParserNeedle, mapRunParser + Environment.NewLine + mapBossParserNeedle, StringComparison.Ordinal);

        var directiveParser = string.Join(Environment.NewLine, new[]
        {
            "            if (line.StartsWith(\"@mapPolicy=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                string value = line.Substring(\"@mapPolicy=\".Length).Trim();",
            "                if (!System.String.Equals(value, \"v2\", System.StringComparison.OrdinalIgnoreCase))",
            "                    throw new System.Exception(\"@mapPolicy must be v2\");",
            "                vars.mapPolicyV2Enabled = true;",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapEndpoint=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyEndpoint = line.Substring(\"@mapEndpoint=\".Length).Trim().ToLowerInvariant();",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapTarget=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                int mapTarget;",
            "                if (!System.Int32.TryParse(line.Substring(\"@mapTarget=\".Length).Trim(), out mapTarget) || mapTarget < 0 || mapTarget > 100)",
            "                    throw new System.Exception(\"@mapTarget must be 0..100\");",
            "                vars.mapPolicyTarget = mapTarget;",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapDeathPolicy=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyDeathPolicy = line.Substring(\"@mapDeathPolicy=\".Length).Trim().ToLowerInvariant();",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapGameTimePolicy=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyGameTimePolicy = line.Substring(\"@mapGameTimePolicy=\".Length).Trim().ToLowerInvariant();",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapCharacter=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyCharacter = line.Substring(\"@mapCharacter=\".Length).Trim();",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapPinnacleTarget=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyPinnacleTarget = line.Substring(\"@mapPinnacleTarget=\".Length).Trim();",
            "                continue;",
            "            }",
            "            if (line.StartsWith(\"@mapPinnacleName=\", System.StringComparison.OrdinalIgnoreCase))",
            "            {",
            "                vars.mapPolicyPinnacleName = line.Substring(\"@mapPinnacleName=\".Length).Trim();",
            "                continue;",
            "            }",
            ""
        });
        aslText = aslText.Replace(objectiveSplitNeedle, directiveParser + objectiveSplitNeedle, StringComparison.Ordinal);

        var startupCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_STARTUP_BEGIN - generated by PoE2RouteSetup",
            "    vars.mapPolicyV2Enabled = false;",
            "    vars.mapPolicyEndpoint = \"fixed\";",
            "    vars.mapPolicyTarget = 0;",
            "    vars.mapPolicyDeathPolicy = \"none\";",
            "    vars.mapPolicyGameTimePolicy = \"completion\";",
            "    vars.mapPolicyCharacter = \"\";",
            "    vars.mapPolicyPinnacleTarget = \"\";",
            "    vars.mapPolicyPinnacleName = \"\";",
            "    vars.mapPolicyReader = null;",
            "    vars.mapPolicyClientPath = \"\";",
            "    vars.mapPolicyMapRegex = new System.Text.RegularExpressions.Regex(\"^[^ ]+ [^ ]+ \\\\d+(?=.*(?:\\\\s2caa[0-9A-Fa-f]{4}\\\\s|Generating level))(?=.*\\\\[DEBUG\\\\s+[^\\\\]]+\\\\]\\\\s+.*?(\\\\d{1,3})\\\\D+\\\"([A-Za-z][A-Za-z0-9_]*)\\\"\\\\D+(\\\\d{1,10})\\\\s*$)\", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);",
            "    vars.mapPolicySceneRegex = new System.Text.RegularExpressions.Regex(\"\\\\[SCENE\\\\] Set Source \\\\[([^\\\\]]+)\\\\]\", System.Text.RegularExpressions.RegexOptions.Compiled);",
            "    vars.mapPolicyProcessedBossLines = 0;",
            "    vars.mapPolicyBaseSegmentCount = 0;",
            "    vars.mapPolicyCurrentActive = false;",
            "    vars.mapPolicyInsideMap = false;",
            "    vars.mapPolicyInChildArea = false;",
            "    vars.mapPolicyChildAreaId = \"\";",
            "    vars.mapPolicyChildSeed = \"\";",
            "    vars.mapPolicyCurrentAreaId = \"\";",
            "    vars.mapPolicyCurrentSeed = \"\";",
            "    vars.mapPolicyCurrentAreaLevel = 0;",
            "    vars.mapPolicyCurrentScene = \"\";",
            "    vars.mapPolicyAwaitingSceneName = false;",
            "    vars.mapPolicyAttemptNumber = 0;",
            "    vars.mapPolicyFinalizedCount = 0;",
            "    vars.mapPolicySuccessCount = 0;",
            "    vars.mapPolicyFailureCount = 0;",
            "    vars.mapPolicyCurrentDeathCount = 0;",
            "    vars.mapPolicyRunDeathCount = 0;",
            "    vars.mapPolicyBossQualified = false;",
            "    vars.mapPolicyProvisionalExit = false;",
            "    vars.mapPolicyExitHasGame = false;",
            "    vars.mapPolicyExitGame = System.TimeSpan.Zero;",
            "    vars.mapPolicyExitHasReal = false;",
            "    vars.mapPolicyExitReal = System.TimeSpan.Zero;",
            "    vars.mapPolicyExitAreaId = \"\";",
            "    vars.mapPolicyExitClass = \"\";",
            "    vars.mapPolicyLastFinalizedAreaId = \"\";",
            "    vars.mapPolicyLastFinalizedSeed = \"\";",
            "    vars.mapPolicySetupPauseActive = false;",
            "    vars.mapPolicySplitTrigger = false;",
            "    vars.mapPolicySplitKind = \"\";",
            "    vars.mapPolicySplitOverrideHasGame = false;",
            "    vars.mapPolicySplitOverrideGame = System.TimeSpan.Zero;",
            "    vars.mapPolicySplitOverrideHasReal = false;",
            "    vars.mapPolicySplitOverrideReal = System.TimeSpan.Zero;",
            "    vars.mapPolicyGameCorrectionPending = false;",
            "    vars.mapPolicyGameCorrectionTarget = System.TimeSpan.Zero;",
            "    vars.mapPolicyPinnacleSeen = false;",
            "    vars.mapPolicyWriteContext = (System.Action<string, string, int, int, string>)((mode, areaId, areaLevel, bossNumber, classification) =>",
            "    {",
            "        try",
            "        {",
            "            string mapContext = \"version=1\" + System.Environment.NewLine",
            "                + \"mode=\" + mode + System.Environment.NewLine",
            "                + \"areaId=\" + areaId + System.Environment.NewLine",
            "                + \"areaLevel=\" + areaLevel + System.Environment.NewLine",
            "                + \"mapBossNumber=\" + bossNumber + System.Environment.NewLine",
            "                + \"classification=\" + classification + System.Environment.NewLine;",
            "            System.IO.File.WriteAllText(vars.bossContextPath, mapContext);",
            "            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "                System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_CONTEXT | mode=\" + mode + \" | area=\" + areaId",
            "                + \" | level=\" + areaLevel + \" | bossNumber=\" + bossNumber + \" | classification=\" + classification + System.Environment.NewLine);",
            "        } catch {}",
            "    });",
            "    vars.mapPolicyAudit = (System.Action<string, string>)((eventType, details) =>",
            "    {",
            "        try",
            "        {",
            "            if (!vars.auditActive || vars.auditFinalized) return /* ASL void lambda: keep return void */;",
            "            string mapAuditGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value.ToString() : \"null\";",
            "            string mapAuditReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value.ToString() : \"null\";",
            "            ((System.Action<string, string, string, string>)vars.auditAppend)(eventType, mapAuditGame, mapAuditReal, details);",
            "        } catch {}",
            "    });",
            "    // MAP_POLICY_V2_STARTUP_END"
        });
        aslText = InsertAfterActionOpen(aslText, "startup", startupCode, required: true);

        // Initialize the dedicated readers after the base mixed ASL has parsed the generated
        // route. That keeps this policy dormant for every non-Maps run.
        var initCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_INIT_BEGIN",
            "    if (vars.mapPolicyV2Enabled && vars.configValid)",
            "    {",
            "        bool mapEndpointValid = vars.mapPolicyEndpoint == \"fixed\" || vars.mapPolicyEndpoint == \"death\" || vars.mapPolicyEndpoint == \"manual\" || vars.mapPolicyEndpoint == \"pinnacle\";",
            "        bool mapDeathPolicyValid = vars.mapPolicyDeathPolicy == \"none\" || vars.mapPolicyDeathPolicy == \"end\" || vars.mapPolicyDeathPolicy == \"track\";",
            "        bool mapGameTimePolicyValid = vars.mapPolicyGameTimePolicy == \"completion\" || vars.mapPolicyGameTimePolicy == \"continuous\";",
            "        if (!mapEndpointValid) throw new System.Exception(\"@mapEndpoint must be fixed, death, manual, or pinnacle\");",
            "        if (!mapDeathPolicyValid) throw new System.Exception(\"@mapDeathPolicy must be none, end, or track\");",
            "        if (!mapGameTimePolicyValid) throw new System.Exception(\"@mapGameTimePolicy must be completion or continuous\");",
            "        if (vars.mapPolicyEndpoint == \"fixed\" && vars.mapPolicyTarget < 1) throw new System.Exception(\"Fixed Maps endpoint requires @mapTarget >= 1\");",
            "        if (vars.mapPolicyEndpoint == \"death\" && vars.mapPolicyDeathPolicy != \"end\") throw new System.Exception(\"Until-first-death endpoint requires @mapDeathPolicy=end\");",
            "        if ((vars.mapPolicyDeathPolicy == \"end\" || vars.mapPolicyDeathPolicy == \"track\") && vars.mapPolicyCharacter == \"\") throw new System.Exception(\"Maps death tracking requires @mapCharacter\");",
            "        if (vars.mapPolicyEndpoint == \"pinnacle\" && vars.mapPolicyPinnacleTarget == \"\") throw new System.Exception(\"Pinnacle endpoint requires @mapPinnacleTarget\");",
            "        try",
            "        {",
            "            string mapPolicyGameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);",
            "            vars.mapPolicyClientPath = System.IO.Path.Combine(mapPolicyGameDir, \"logs\", \"Client.txt\");",
            "            var mapPolicyFs = new System.IO.FileStream(vars.mapPolicyClientPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);",
            "            mapPolicyFs.Seek(0, System.IO.SeekOrigin.End);",
            "            vars.mapPolicyReader = new System.IO.StreamReader(mapPolicyFs);",
            "        }",
            "        catch (System.Exception mapReaderEx)",
            "        {",
            "            vars.mapPolicyReader = null;",
            "            throw new System.Exception(\"Maps policy could not open Client.txt: \" + mapReaderEx.Message);",
            "        }",
            "        try { vars.mapPolicyProcessedBossLines = System.IO.File.Exists(vars.eventPath) ? System.IO.File.ReadAllLines(vars.eventPath).Length : 0; } catch { vars.mapPolicyProcessedBossLines = 0; }",
            "        vars.mapPolicyBaseSegmentCount = timer.Run.Count;",
            "        ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", \"\", 0, 0, \"maps-policy-v2-ready\");",
            "        if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "            System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_READY | endpoint=\" + vars.mapPolicyEndpoint",
            "            + \" | target=\" + vars.mapPolicyTarget + \" | deathPolicy=\" + vars.mapPolicyDeathPolicy",
            "            + \" | gameTimePolicy=\" + vars.mapPolicyGameTimePolicy",
            "            + \" | characterRequired=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"false\" : \"true\")",
            "            + \" | pinnacle=\" + vars.mapPolicyPinnacleTarget + System.Environment.NewLine);",
            "    }",
            "    // MAP_POLICY_V2_INIT_END"
        });
        aslText = InsertBeforeActionClose(aslText, "init", initCode, required: true);

        var updateCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_UPDATE_BEGIN",
            "    if (vars.mapPolicyV2Enabled && vars.configValid)",
            "    {",
            "        // Client.txt lifecycle observer. Unlike the legacy candidate heuristic, a",
            "        // generated area is an ordinary map only when its internal ID begins Map.",
            "        if (vars.mapPolicyReader != null && !vars.mapPolicySplitTrigger)",
            "        {",
            "            int mapPolicyLines = 0;",
            "            string mapPolicyLine = null;",
            "            while (mapPolicyLines < 800 && (mapPolicyLine = vars.mapPolicyReader.ReadLine()) != null)",
            "            {",
            "                mapPolicyLines++;",
            "",
            "                // Death parsing is completely disabled under the default none policy.",
            "                if (vars.mapPolicyDeathPolicy != \"none\"",
            "                    && ((vars.mapPolicyCurrentActive && vars.mapPolicyInsideMap) || vars.mapPolicyPinnacleSeen)",
            "                    && (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))",
            "                {",
            "                    int mapDeathMarker = mapPolicyLine.LastIndexOf(\"] : \", System.StringComparison.Ordinal);",
            "                    if (mapDeathMarker >= 0)",
            "                    {",
            "                        string mapDeathMessage = mapPolicyLine.Substring(mapDeathMarker + 4).Trim();",
            "                        string mapDeathSuffix = \" has been slain.\";",
            "                        if (mapDeathMessage.EndsWith(mapDeathSuffix, System.StringComparison.Ordinal))",
            "                        {",
            "                            string mapDeathName = mapDeathMessage.Substring(0, mapDeathMessage.Length - mapDeathSuffix.Length);",
            "                            if (System.String.Equals(mapDeathName, vars.mapPolicyCharacter, System.StringComparison.Ordinal))",
            "                            {",
            "                                vars.mapPolicyRunDeathCount++;",
            "                                bool deathInsideOrdinaryMap = vars.mapPolicyCurrentActive && vars.mapPolicyInsideMap;",
            "                                if (deathInsideOrdinaryMap) vars.mapPolicyCurrentDeathCount++;",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"PLAYER_DEATH\",",
            "                                    \"character=\" + vars.mapPolicyCharacter + \";death=\" + vars.mapPolicyRunDeathCount",
            "                                    + \";scope=\" + (deathInsideOrdinaryMap ? \"map\" : \"pinnacle\")",
            "                                    + \";mapDeath=\" + (deathInsideOrdinaryMap ? vars.mapPolicyCurrentDeathCount.ToString() : \"0\")",
            "                                    + \";map=\" + (deathInsideOrdinaryMap ? vars.mapPolicyCurrentAreaId : \"\")",
            "                                    + \";seed=\" + (deathInsideOrdinaryMap ? vars.mapPolicyCurrentSeed : \"\"));",
            "",
            "                                if (vars.mapPolicyDeathPolicy == \"end\")",
            "                                {",
            "                                    string deathSegmentName = \"Death [\" + vars.mapPolicyRunDeathCount + \"]\";",
            "                                    try",
            "                                    {",
            "                                        if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                        {",
            "                                            timer.Run[timer.CurrentSplitIndex].Name = deathSegmentName;",
            "                                            timer.Run.HasChanged = true;",
            "                                            timer.CallRunManuallyModified();",
            "                                        }",
            "                                    } catch {}",
            "                                    vars.mapPolicySplitKind = \"DEATH_END\";",
            "                                    vars.mapPolicySplitTrigger = true;",
            "                                    vars.mapPolicySetupPauseActive = false;",
            "                                    ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", vars.mapPolicyCurrentAreaId, vars.mapPolicyCurrentAreaLevel, 0, \"death-end\");",
            "                                    break;",
            "                                }",
            "                                else if (vars.mapPolicyDeathPolicy == \"track\")",
            "                                {",
            "                                    string deathSegmentName = \"Death [\" + vars.mapPolicyRunDeathCount + \"]\";",
            "                                    try",
            "                                    {",
            "                                        int deathInsertIndex = timer.CurrentSplitIndex;",
            "                                        if (deathInsertIndex < 0) deathInsertIndex = 0;",
            "                                        if (deathInsertIndex > timer.Run.Count) deathInsertIndex = timer.Run.Count;",
            "                                        timer.Run.Insert(deathInsertIndex, new LiveSplit.Model.Segment(deathSegmentName));",
            "                                        timer.Run.HasChanged = true;",
            "                                        timer.CallRunManuallyModified();",
            "                                    } catch {}",
            "                                    vars.mapPolicySplitKind = \"DEATH_TRACK\";",
            "                                    vars.mapPolicySplitTrigger = true;",
            "                                    break;",
            "                                }",
            "                            }",
            "                        }",
            "                    }",
            "                }",
            "                if (vars.mapPolicySplitTrigger) break;",
            "",
            "                var mapPolicyAreaMatch = vars.mapPolicyMapRegex.Match(mapPolicyLine);",
            "                if (mapPolicyAreaMatch.Success)",
            "                {",
            "                    int mapAreaLevel = 0;",
            "                    System.Int32.TryParse(mapPolicyAreaMatch.Groups[1].Value, out mapAreaLevel);",
            "                    string mapAreaId = mapPolicyAreaMatch.Groups[2].Value;",
            "                    string mapSeed = mapPolicyAreaMatch.Groups[3].Value;",
            "                    bool isMapArea = mapAreaId.StartsWith(\"Map\", System.StringComparison.OrdinalIgnoreCase);",
            "                    bool isMapChildArea = vars.mapPolicyCurrentActive && (",
            "                        mapAreaId.StartsWith(\"Abyss_Depths\", System.StringComparison.OrdinalIgnoreCase)",
            "                        || System.String.Equals(mapAreaId, \"Abyss_Boss1\", System.StringComparison.OrdinalIgnoreCase)",
            "                        || System.String.Equals(mapAreaId, \"Abyss_Boss2\", System.StringComparison.OrdinalIgnoreCase)",
            "                        || System.String.Equals(mapAreaId, \"Delirium_HungerBoss\", System.StringComparison.OrdinalIgnoreCase)",
            "                        || mapAreaId.StartsWith(\"ExpeditionSubArea\", System.StringComparison.OrdinalIgnoreCase));",
            "                    bool isVaalRuinsBoundary = System.String.Equals(mapAreaId, \"IncursionHub\", System.StringComparison.OrdinalIgnoreCase)",
            "                        || System.String.Equals(mapAreaId, \"IncursionHubEndgame\", System.StringComparison.OrdinalIgnoreCase);",
            "",
            "                    if (isMapArea)",
            "                    {",
            "                        bool sameCurrent = vars.mapPolicyCurrentActive",
            "                            && System.String.Equals(mapAreaId, vars.mapPolicyCurrentAreaId, System.StringComparison.OrdinalIgnoreCase)",
            "                            && System.String.Equals(mapSeed, vars.mapPolicyCurrentSeed, System.StringComparison.Ordinal);",
            "                        bool sameFinalized = !vars.mapPolicyCurrentActive",
            "                            && System.String.Equals(mapAreaId, vars.mapPolicyLastFinalizedAreaId, System.StringComparison.OrdinalIgnoreCase)",
            "                            && System.String.Equals(mapSeed, vars.mapPolicyLastFinalizedSeed, System.StringComparison.Ordinal);",
            "",
            "                        if (sameCurrent)",
            "                        {",
            "                            bool returningFromChild = vars.mapPolicyInChildArea;",
            "                            string returnedChildArea = vars.mapPolicyChildAreaId;",
            "                            string returnedChildSeed = vars.mapPolicyChildSeed;",
            "                            vars.mapPolicyInsideMap = true;",
            "                            vars.mapPolicyInChildArea = false;",
            "                            vars.mapPolicyChildAreaId = \"\";",
            "                            vars.mapPolicyChildSeed = \"\";",
            "                            if (returningFromChild)",
            "                            {",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_CHILD_RETURN\",",
            "                                    \"parent=\" + mapAreaId + \";parentSeed=\" + mapSeed + \";child=\" + returnedChildArea + \";childSeed=\" + returnedChildSeed);",
            "                            }",
            "                            if (vars.mapPolicyProvisionalExit)",
            "                            {",
            "                                vars.mapPolicyProvisionalExit = false;",
            "                                vars.mapPolicyExitHasGame = false;",
            "                                vars.mapPolicyExitHasReal = false;",
            "                                vars.mapPolicyExitAreaId = \"\";",
            "                                vars.mapPolicyExitClass = \"\";",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_REENTRY\",",
            "                                    \"map=\" + mapAreaId + \";seed=\" + mapSeed + \";attempt=\" + vars.mapPolicyAttemptNumber + \";outsideTimeCounted=true\");",
            "                            }",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(vars.mapPolicyBossQualified ? \"off\" : \"map\", mapAreaId, mapAreaLevel, vars.mapPolicyBossQualified ? 0 : vars.mapPolicyAttemptNumber, vars.mapPolicyBossQualified ? \"qualified-reentry\" : \"same-seed-reentry\");",
            "                            continue;",
            "                        }",
            "",
            "                        if (sameFinalized)",
            "                        {",
            "                            vars.mapPolicySetupPauseActive = true;",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", mapAreaId, mapAreaLevel, 0, \"completed-map-reentry-ignored\");",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_COMPLETED_REENTRY_IGNORED\", \"map=\" + mapAreaId + \";seed=\" + mapSeed);",
            "                            continue;",
            "                        }",
            "",
            "                        if (vars.mapPolicyCurrentActive && vars.mapPolicyInChildArea && vars.mapPolicyBossQualified && !vars.mapPolicyProvisionalExit)",
            "                        {",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_CHILD_DIRECT_NEW_MAP_UNRESOLVED\",",
            "                                \"parent=\" + vars.mapPolicyCurrentAreaId + \";parentSeed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";child=\" + vars.mapPolicyChildAreaId + \";newMap=\" + mapAreaId + \";newSeed=\" + mapSeed);",
            "                            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "                                System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_CHILD_DIRECT_NEW_MAP_UNRESOLVED | qualified=true | parent=\" + vars.mapPolicyCurrentAreaId",
            "                                + \" | child=\" + vars.mapPolicyChildAreaId + \" | newMap=\" + mapAreaId + System.Environment.NewLine);",
            "                            continue;",
            "                        }",
            "",
            "                        // A direct transition from a recognized map child into a different",
            "                        // Map+seed also proves the parent attempt was abandoned. Use the",
            "                        // new-map entry time as the failure boundary; there is no separate",
            "                        // setup interval to roll back in this direct-transition case.",
            "                        if (vars.mapPolicyCurrentActive && vars.mapPolicyInChildArea && !vars.mapPolicyBossQualified && !vars.mapPolicyProvisionalExit)",
            "                        {",
            "                            vars.mapPolicyProvisionalExit = true;",
            "                            vars.mapPolicyExitHasGame = timer.CurrentTime.GameTime.HasValue;",
            "                            vars.mapPolicyExitGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value : System.TimeSpan.Zero;",
            "                            vars.mapPolicyExitHasReal = timer.CurrentTime.RealTime.HasValue;",
            "                            vars.mapPolicyExitReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value : System.TimeSpan.Zero;",
            "                            vars.mapPolicyExitAreaId = mapAreaId;",
            "                            vars.mapPolicyExitClass = \"NEW_MAP\";",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_CHILD_EXIT_TO_NEW_MAP\",",
            "                                \"parent=\" + vars.mapPolicyCurrentAreaId + \";parentSeed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";child=\" + vars.mapPolicyChildAreaId + \";childSeed=\" + vars.mapPolicyChildSeed",
            "                                + \";newMap=\" + mapAreaId + \";newSeed=\" + mapSeed);",
            "                            vars.mapPolicyInChildArea = false;",
            "                            vars.mapPolicyChildAreaId = \"\";",
            "                            vars.mapPolicyChildSeed = \"\";",
            "                        }",
            "",
            "                        // A different seed while an unfinished map has a provisional",
            "                        // exit proves that prior map is abandoned/failed.",
            "                        if (vars.mapPolicyCurrentActive && vars.mapPolicyProvisionalExit)",
            "                        {",
            "                            int finalizedNumber = vars.mapPolicyFinalizedCount + 1;",
            "                            string oldDisplay = vars.mapPolicyCurrentScene != \"\" ? vars.mapPolicyCurrentScene : vars.mapPolicyCurrentAreaId;",
            "                            string failedName = \"Map [\" + vars.mapPolicyAttemptNumber + \"] — \" + oldDisplay + \" (Lv \" + vars.mapPolicyCurrentAreaLevel + \") — FAILED\";",
            "                            try",
            "                            {",
            "                                if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                    timer.Run[timer.CurrentSplitIndex].Name = failedName;",
            "                            } catch {}",
            "",
            "                            vars.mapPolicyFinalizedCount = finalizedNumber;",
            "                            vars.mapPolicyFailureCount++;",
            "                            vars.mapPolicyLastFinalizedAreaId = vars.mapPolicyCurrentAreaId;",
            "                            vars.mapPolicyLastFinalizedSeed = vars.mapPolicyCurrentSeed;",
            "                            vars.mapPolicySplitOverrideHasGame = vars.mapPolicyExitHasGame;",
            "                            vars.mapPolicySplitOverrideGame = vars.mapPolicyExitGame;",
            "                            vars.mapPolicySplitOverrideHasReal = vars.mapPolicyExitHasReal;",
            "                            vars.mapPolicySplitOverrideReal = vars.mapPolicyExitReal;",
            "                            if (vars.mapPolicyGameTimePolicy == \"completion\" && vars.mapPolicyExitHasGame)",
            "                            {",
            "                                vars.mapPolicyGameCorrectionTarget = vars.mapPolicyExitGame;",
            "                                vars.mapPolicyGameCorrectionPending = true;",
            "                            }",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_FAILURE_CONFIRMED\",",
            "                                \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";reason=SEED_REPLACED;newMap=\" + mapAreaId + \";newSeed=\" + mapSeed",
            "                                + \";deaths=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"<not-tracked>\" : vars.mapPolicyCurrentDeathCount.ToString()));",
            "                            if (vars.mapPolicyGameTimePolicy == \"completion\" && vars.mapPolicyExitHasGame)",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_TIME_ROLLBACK\", \"targetGameTime=\" + vars.mapPolicyExitGame.ToString() + \";setupExcluded=true\");",
            "                            else if (vars.mapPolicyGameTimePolicy == \"continuous\" && vars.mapPolicyExitHasGame)",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_TIME_CONTINUOUS\", \"savedExitGameTime=\" + vars.mapPolicyExitGame.ToString() + \";rollback=false\");",
            "",
            "                            bool fixedFinishedByFailure = vars.mapPolicyEndpoint == \"fixed\" && vars.mapPolicyFinalizedCount >= vars.mapPolicyTarget;",
            "                            if (!fixedFinishedByFailure)",
            "                            {",
            "                                int nextAttempt = vars.mapPolicyAttemptNumber + 1;",
            "                                string newDisplay = mapAreaId.StartsWith(\"Map\", System.StringComparison.OrdinalIgnoreCase) && mapAreaId.Length > 3 ? mapAreaId.Substring(3) : mapAreaId;",
            "                                string nextName = \"Map [\" + nextAttempt + \"] — \" + newDisplay + \" (Lv \" + mapAreaLevel + \")\";",
            "                                try",
            "                                {",
            "                                    timer.Run.Add(new LiveSplit.Model.Segment(nextName));",
            "                                    timer.Run.HasChanged = true;",
            "                                    timer.CallRunManuallyModified();",
            "                                } catch {}",
            "                                vars.mapPolicyAttemptNumber = nextAttempt;",
            "                                vars.mapPolicyCurrentAreaId = mapAreaId;",
            "                                vars.mapPolicyCurrentSeed = mapSeed;",
            "                                vars.mapPolicyCurrentAreaLevel = mapAreaLevel;",
            "                                vars.mapPolicyCurrentScene = newDisplay;",
            "                                vars.mapPolicyAwaitingSceneName = true;",
            "                                vars.mapPolicyCurrentDeathCount = 0;",
            "                                vars.mapPolicyBossQualified = false;",
            "                                vars.mapPolicyProvisionalExit = false;",
            "                                vars.mapPolicyExitHasGame = false;",
            "                                vars.mapPolicyExitHasReal = false;",
            "                                vars.mapPolicyExitAreaId = \"\";",
            "                                vars.mapPolicyExitClass = \"\";",
            "                                vars.mapPolicyCurrentActive = true;",
            "                                vars.mapPolicyInsideMap = true;",
            "                                vars.mapPolicyInChildArea = false;",
            "                                vars.mapPolicyChildAreaId = \"\";",
            "                                vars.mapPolicyChildSeed = \"\";",
            "                                vars.mapPolicySetupPauseActive = false;",
            "                                ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"map\", mapAreaId, mapAreaLevel, nextAttempt, \"new-seed-after-failure\");",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_ENTER\", \"attempt=\" + nextAttempt + \";map=\" + mapAreaId + \";seed=\" + mapSeed + \";level=\" + mapAreaLevel + \";afterFailure=true\");",
            "                            }",
            "                            else",
            "                            {",
            "                                vars.mapPolicyCurrentActive = false;",
            "                                vars.mapPolicyInsideMap = false;",
            "                                vars.mapPolicySetupPauseActive = false;",
            "                                ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", mapAreaId, mapAreaLevel, 0, \"fixed-run-complete-after-failure\");",
            "                            }",
            "                            vars.mapPolicySplitKind = \"MAP_FAILED\";",
            "                            vars.mapPolicySplitTrigger = true;",
            "                            break;",
            "                        }",
            "",
            "                        // New map after a successful map/setup pause, or the first map.",
            "                        if (!vars.mapPolicyCurrentActive)",
            "                        {",
            "                            vars.mapPolicyAttemptNumber++;",
            "                            vars.mapPolicyCurrentAreaId = mapAreaId;",
            "                            vars.mapPolicyCurrentSeed = mapSeed;",
            "                            vars.mapPolicyCurrentAreaLevel = mapAreaLevel;",
            "                            vars.mapPolicyCurrentScene = mapAreaId.Length > 3 ? mapAreaId.Substring(3) : mapAreaId;",
            "                            vars.mapPolicyAwaitingSceneName = true;",
            "                            vars.mapPolicyCurrentDeathCount = 0;",
            "                            vars.mapPolicyBossQualified = false;",
            "                            vars.mapPolicyProvisionalExit = false;",
            "                            vars.mapPolicyExitHasGame = false;",
            "                            vars.mapPolicyExitHasReal = false;",
            "                            vars.mapPolicyExitAreaId = \"\";",
            "                            vars.mapPolicyExitClass = \"\";",
            "                            vars.mapPolicyCurrentActive = true;",
            "                            vars.mapPolicyInsideMap = true;",
            "                            vars.mapPolicyInChildArea = false;",
            "                            vars.mapPolicyChildAreaId = \"\";",
            "                            vars.mapPolicyChildSeed = \"\";",
            "                            vars.mapPolicySetupPauseActive = false;",
            "                            string enteringName = \"Map [\" + vars.mapPolicyAttemptNumber + \"] — \" + vars.mapPolicyCurrentScene + \" (Lv \" + mapAreaLevel + \")\";",
            "                            try",
            "                            {",
            "                                if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                {",
            "                                    timer.Run[timer.CurrentSplitIndex].Name = enteringName;",
            "                                    timer.Run.HasChanged = true;",
            "                                    timer.CallRunManuallyModified();",
            "                                }",
            "                            } catch {}",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"map\", mapAreaId, mapAreaLevel, vars.mapPolicyAttemptNumber, \"active-map\");",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_ENTER\", \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + mapAreaId + \";seed=\" + mapSeed + \";level=\" + mapAreaLevel);",
            "                            if (timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning && !vars.startTrigger)",
            "                            {",
            "                                vars.startTrigger = true;",
            "                                if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_START | map=\" + mapAreaId + \" | seed=\" + mapSeed + System.Environment.NewLine);",
            "                            }",
            "                            continue;",
            "                        }",
            "                    }",
            "                    else",
            "                    {",
            "                        // Recognized map-linked child instances remain part of the",
            "                        // parent map attempt. Keep Game Time running, but disable",
            "                        // parent-map BossWatcher qualification while physically inside",
            "                        // the child so its bosses/events cannot complete the parent map.",
            "                        if (isMapChildArea)",
            "                        {",
            "                            bool childTransition = vars.mapPolicyInChildArea;",
            "                            string previousChild = vars.mapPolicyChildAreaId;",
            "                            vars.mapPolicyInChildArea = true;",
            "                            vars.mapPolicyChildAreaId = mapAreaId;",
            "                            vars.mapPolicyChildSeed = mapSeed;",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", mapAreaId, mapAreaLevel, 0, \"map-child-area\");",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(childTransition ? \"MAP_CHILD_TRANSITION\" : \"MAP_CHILD_ENTER\",",
            "                                \"parent=\" + vars.mapPolicyCurrentAreaId + \";parentSeed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";child=\" + mapAreaId + \";childSeed=\" + mapSeed",
            "                                + (childTransition ? \";previousChild=\" + previousChild : \"\"));",
            "                            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "                                System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_CHILD_ENTER | parent=\" + vars.mapPolicyCurrentAreaId",
            "                                + \" | seed=\" + vars.mapPolicyCurrentSeed + \" | child=\" + mapAreaId + \" | childSeed=\" + mapSeed + System.Environment.NewLine);",
            "                            continue;",
            "                        }",
            "",
            "                        // Every other generated non-Map area is a real map-exit boundary.",
            "                        // Vaal Ruins is intentionally included here: the optional Ancient",
            "                        // Beacon portal leaves the map and enters Temple setup/staging.",
            "                        if (vars.mapPolicyCurrentActive && vars.mapPolicyInsideMap)",
            "                        {",
            "                            bool exitingFromChild = vars.mapPolicyInChildArea;",
            "                            string exitingChildArea = vars.mapPolicyChildAreaId;",
            "                            string exitingChildSeed = vars.mapPolicyChildSeed;",
            "                            vars.mapPolicyInsideMap = false;",
            "                            vars.mapPolicyInChildArea = false;",
            "                            vars.mapPolicyChildAreaId = \"\";",
            "                            vars.mapPolicyChildSeed = \"\";",
            "                            if (exitingFromChild)",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_CHILD_EXIT_EXTERNAL\",",
            "                                    \"parent=\" + vars.mapPolicyCurrentAreaId + \";parentSeed=\" + vars.mapPolicyCurrentSeed",
            "                                    + \";child=\" + exitingChildArea + \";childSeed=\" + exitingChildSeed + \";exitArea=\" + mapAreaId);",
            "                            if (isVaalRuinsBoundary)",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_VAAL_RUINS_EXIT_BOUNDARY\",",
            "                                    \"map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed + \";exitArea=\" + mapAreaId);",
            "                            if (vars.mapPolicyBossQualified)",
            "                            {",
            "                                string mapDisplay = vars.mapPolicyCurrentScene != \"\" ? vars.mapPolicyCurrentScene : vars.mapPolicyCurrentAreaId;",
            "                                string successName = \"Map [\" + vars.mapPolicyAttemptNumber + \"] — \" + mapDisplay + \" (Lv \" + vars.mapPolicyCurrentAreaLevel + \") — SUCCESS\";",
            "                                try",
            "                                {",
            "                                    if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                        timer.Run[timer.CurrentSplitIndex].Name = successName;",
            "                                } catch {}",
            "                                vars.mapPolicyFinalizedCount++;",
            "                                vars.mapPolicySuccessCount++;",
            "                                vars.mapPolicyLastFinalizedAreaId = vars.mapPolicyCurrentAreaId;",
            "                                vars.mapPolicyLastFinalizedSeed = vars.mapPolicyCurrentSeed;",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_SUCCESS\",",
            "                                    \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                    + \";level=\" + vars.mapPolicyCurrentAreaLevel",
            "                                    + \";deaths=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"<not-tracked>\" : vars.mapPolicyCurrentDeathCount.ToString())",
            "                                    + \";exitArea=\" + mapAreaId + \";exitClass=\" + (isVaalRuinsBoundary ? \"VAAL_RUINS\" : \"EXTERNAL\"));",
            "                                bool fixedFinishedBySuccess = vars.mapPolicyEndpoint == \"fixed\" && vars.mapPolicyFinalizedCount >= vars.mapPolicyTarget;",
            "                                if (!fixedFinishedBySuccess)",
            "                                {",
            "                                    try",
            "                                    {",
            "                                        timer.Run.Add(new LiveSplit.Model.Segment(\"Map [\" + (vars.mapPolicyAttemptNumber + 1) + \"] — Waiting for map entry\"));",
            "                                        timer.Run.HasChanged = true;",
            "                                        timer.CallRunManuallyModified();",
            "                                    } catch {}",
            "                                    vars.mapPolicySetupPauseActive = true;",
            "                                }",
            "                                else vars.mapPolicySetupPauseActive = false;",
            "                                vars.mapPolicyCurrentActive = false;",
            "                                vars.mapPolicyInChildArea = false;",
            "                                vars.mapPolicyChildAreaId = \"\";",
            "                                vars.mapPolicyChildSeed = \"\";",
            "                                vars.mapPolicyBossQualified = false;",
            "                                vars.mapPolicyProvisionalExit = false;",
            "                                vars.mapPolicyExitHasGame = false;",
            "                                vars.mapPolicyExitHasReal = false;",
            "                                vars.mapPolicyExitAreaId = \"\";",
            "                                vars.mapPolicyExitClass = \"\";",
            "                                ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", mapAreaId, mapAreaLevel, 0, fixedFinishedBySuccess ? \"fixed-run-complete\" : \"between-maps-setup\");",
            "                                vars.mapPolicySplitKind = \"MAP_SUCCESS\";",
            "                                vars.mapPolicySplitTrigger = true;",
            "                                break;",
            "                            }",
            "                            else",
            "                            {",
            "                                vars.mapPolicyProvisionalExit = true;",
            "                                vars.mapPolicyExitHasGame = timer.CurrentTime.GameTime.HasValue;",
            "                                vars.mapPolicyExitGame = timer.CurrentTime.GameTime.HasValue ? timer.CurrentTime.GameTime.Value : System.TimeSpan.Zero;",
            "                                vars.mapPolicyExitHasReal = timer.CurrentTime.RealTime.HasValue;",
            "                                vars.mapPolicyExitReal = timer.CurrentTime.RealTime.HasValue ? timer.CurrentTime.RealTime.Value : System.TimeSpan.Zero;",
            "                                vars.mapPolicyExitAreaId = mapAreaId;",
            "                                vars.mapPolicyExitClass = isVaalRuinsBoundary ? \"VAAL_RUINS\" : \"EXTERNAL\";",
            "                                ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", mapAreaId, mapAreaLevel, 0, \"premature-exit-unresolved\");",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_PREMATURE_EXIT\",",
            "                                    \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                    + \";exitArea=\" + mapAreaId + \";exitClass=\" + (isVaalRuinsBoundary ? \"VAAL_RUINS\" : \"EXTERNAL\")",
            "                                    + \";savedGameTime=\" + (vars.mapPolicyExitHasGame ? vars.mapPolicyExitGame.ToString() : \"null\") + \";timerContinues=true\");",
            "                            }",
            "                        }",
            "                        else",
            "                        {",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", mapAreaId, mapAreaLevel, 0, \"non-map-area\");",
            "                        }",
            "                    }",
            "                    continue;",
            "                }",
            "",
            "                if (vars.mapPolicyCurrentActive && vars.mapPolicyInsideMap && !vars.mapPolicyInChildArea && vars.mapPolicyAwaitingSceneName)",
            "                {",
            "                    var mapSceneMatch = vars.mapPolicySceneRegex.Match(mapPolicyLine);",
            "                    if (mapSceneMatch.Success)",
            "                    {",
            "                        string mapScene = mapSceneMatch.Groups[1].Value.Trim();",
            "                        if (mapScene != \"\" && !System.String.Equals(mapScene, \"(null)\", System.StringComparison.OrdinalIgnoreCase)",
            "                            && !System.String.Equals(mapScene, \"(unknown)\", System.StringComparison.OrdinalIgnoreCase))",
            "                        {",
            "                            vars.mapPolicyCurrentScene = mapScene;",
            "                            vars.mapPolicyAwaitingSceneName = false;",
            "                            string sceneSegmentName = \"Map [\" + vars.mapPolicyAttemptNumber + \"] — \" + mapScene + \" (Lv \" + vars.mapPolicyCurrentAreaLevel + \")\";",
            "                            try",
            "                            {",
            "                                if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                {",
            "                                    timer.Run[timer.CurrentSplitIndex].Name = sceneSegmentName;",
            "                                    timer.Run.HasChanged = true;",
            "                                    timer.CallRunManuallyModified();",
            "                                }",
            "                            } catch {}",
            "                        }",
            "                    }",
            "                }",
            "            }",
            "        }",
            "",
            "        // Independent BossWatcher cursor. MAP_GONE qualifies but never splits an",
            "        // ordinary map. Selected Pinnacle identity SEEN/GONE owns the Pinnacle endpoint.",
            "        try",
            "        {",
            "            if (System.IO.File.Exists(vars.eventPath))",
            "            {",
            "                string[] mapBossLines = System.IO.File.ReadAllLines(vars.eventPath);",
            "                if (mapBossLines.Length < vars.mapPolicyProcessedBossLines) vars.mapPolicyProcessedBossLines = 0;",
            "                for (int mapBossLineIndex = vars.mapPolicyProcessedBossLines; mapBossLineIndex < mapBossLines.Length; mapBossLineIndex++)",
            "                {",
            "                    string bossLine = mapBossLines[mapBossLineIndex];",
            "                    vars.mapPolicyProcessedBossLines = mapBossLineIndex + 1;",
            "                    if (bossLine == null || bossLine.Trim() == \"\" || bossLine.StartsWith(\"#\")) continue;",
            "                    string[] bossParts = bossLine.Split('|');",
            "                    if (bossParts.Length < 4) continue;",
            "                    string bossEventType = bossParts[1].Trim();",
            "                    string bossId = bossParts[2].Trim();",
            "                    string bossName = bossParts[3].Trim();",
            "",
            "                    if (bossEventType == \"MAP_GONE\" && vars.mapPolicyCurrentActive && (vars.mapPolicyInsideMap || vars.mapPolicyProvisionalExit) && !vars.mapPolicyInChildArea && !vars.mapPolicyBossQualified)",
            "                    {",
            "                        string eventArea = \"\";",
            "                        string eventMapBossName = bossName;",
            "                        string eventMapBossId = \"\";",
            "                        string eventDetector = \"\";",
            "                        string eventConfirmation = \"timer\";",
            "                        for (int mapExtra = 4; mapExtra < bossParts.Length; mapExtra++)",
            "                        {",
            "                            if (bossParts[mapExtra].StartsWith(\"area=\", System.StringComparison.OrdinalIgnoreCase)) eventArea = bossParts[mapExtra].Substring(5);",
            "                            else if (bossParts[mapExtra].StartsWith(\"bossName=\", System.StringComparison.OrdinalIgnoreCase)) eventMapBossName = bossParts[mapExtra].Substring(9);",
            "                            else if (bossParts[mapExtra].StartsWith(\"bossId=\", System.StringComparison.OrdinalIgnoreCase)) eventMapBossId = bossParts[mapExtra].Substring(7);",
            "                            else if (bossParts[mapExtra].StartsWith(\"detector=\", System.StringComparison.OrdinalIgnoreCase)) eventDetector = bossParts[mapExtra].Substring(9);",
            "                            else if (bossParts[mapExtra].StartsWith(\"confirmation=\", System.StringComparison.OrdinalIgnoreCase)) eventConfirmation = bossParts[mapExtra].Substring(13);",
            "                        }",
            "                        bool trustedMapBossIdentity = System.String.Equals(eventDetector, \"database-ocr\", System.StringComparison.OrdinalIgnoreCase) && eventMapBossId.Trim() != \"\";",
            "                        if (!trustedMapBossIdentity)",
            "                        {",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_BOSS_QUALIFICATION_REJECTED\",",
            "                                \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";detector=\" + eventDetector + \";bossId=\" + eventMapBossId + \";reason=UNTRUSTED_IDENTITY\");",
            "                            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_BOSS_QUALIFICATION_REJECTED | map=\" + vars.mapPolicyCurrentAreaId + \" | detector=\" + eventDetector + \" | bossId=\" + eventMapBossId + System.Environment.NewLine);",
            "                            continue;",
            "                        }",
            "                        if (eventArea == \"\" || System.String.Equals(eventArea, vars.mapPolicyCurrentAreaId, System.StringComparison.OrdinalIgnoreCase))",
            "                        {",
            "                            vars.mapPolicyBossQualified = true;",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_BOSS_QUALIFIED\",",
            "                                \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                + \";boss=\" + eventMapBossName + \";confirmation=\" + eventConfirmation",
            "                                + \";deaths=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"<not-tracked>\" : vars.mapPolicyCurrentDeathCount.ToString()));",
            "",
            "                            // A trusted boss disappearance may arrive just after a very fast",
            "                            // boss-kill -> portal transition. The external exit was already",
            "                            // saved as provisional, so finalize SUCCESS at that saved boundary",
            "                            // rather than requiring the player to remain in the map for the full",
            "                            // conservative in-map disappearance grace.",
            "                            if (vars.mapPolicyProvisionalExit && !vars.mapPolicyInsideMap)",
            "                            {",
            "                                string lateMapDisplay = vars.mapPolicyCurrentScene != \"\" ? vars.mapPolicyCurrentScene : vars.mapPolicyCurrentAreaId;",
            "                                string lateSuccessName = \"Map [\" + vars.mapPolicyAttemptNumber + \"] — \" + lateMapDisplay + \" (Lv \" + vars.mapPolicyCurrentAreaLevel + \") — SUCCESS\";",
            "                                try",
            "                                {",
            "                                    if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                    {",
            "                                        timer.Run[timer.CurrentSplitIndex].Name = lateSuccessName;",
            "                                        timer.Run.HasChanged = true;",
            "                                        timer.CallRunManuallyModified();",
            "                                    }",
            "                                } catch {}",
            "                                vars.mapPolicyFinalizedCount++;",
            "                                vars.mapPolicySuccessCount++;",
            "                                vars.mapPolicyLastFinalizedAreaId = vars.mapPolicyCurrentAreaId;",
            "                                vars.mapPolicyLastFinalizedSeed = vars.mapPolicyCurrentSeed;",
            "                                vars.mapPolicySplitOverrideHasGame = vars.mapPolicyExitHasGame;",
            "                                vars.mapPolicySplitOverrideGame = vars.mapPolicyExitGame;",
            "                                vars.mapPolicySplitOverrideHasReal = vars.mapPolicyExitHasReal;",
            "                                vars.mapPolicySplitOverrideReal = vars.mapPolicyExitReal;",
            "                                if (vars.mapPolicyGameTimePolicy == \"completion\" && vars.mapPolicyExitHasGame)",
            "                                {",
            "                                    vars.mapPolicyGameCorrectionTarget = vars.mapPolicyExitGame;",
            "                                    vars.mapPolicyGameCorrectionPending = true;",
            "                                }",
            "                                ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_SUCCESS\",",
            "                                    \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed",
            "                                    + \";level=\" + vars.mapPolicyCurrentAreaLevel",
            "                                    + \";deaths=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"<not-tracked>\" : vars.mapPolicyCurrentDeathCount.ToString())",
            "                                    + \";exitArea=\" + vars.mapPolicyExitAreaId + \";exitClass=\" + vars.mapPolicyExitClass",
            "                                    + \";completion=DEFERRED_BOSS_CONFIRMATION;confirmation=\" + eventConfirmation",
            "                                    + \";savedGameTime=\" + (vars.mapPolicyExitHasGame ? vars.mapPolicyExitGame.ToString() : \"null\"));",
            "                                bool fixedFinishedByLateSuccess = vars.mapPolicyEndpoint == \"fixed\" && vars.mapPolicyFinalizedCount >= vars.mapPolicyTarget;",
            "                                if (!fixedFinishedByLateSuccess)",
            "                                {",
            "                                    try",
            "                                    {",
            "                                        timer.Run.Add(new LiveSplit.Model.Segment(\"Map [\" + (vars.mapPolicyAttemptNumber + 1) + \"] — Waiting for map entry\"));",
            "                                        timer.Run.HasChanged = true;",
            "                                        timer.CallRunManuallyModified();",
            "                                    } catch {}",
            "                                    vars.mapPolicySetupPauseActive = true;",
            "                                }",
            "                                else vars.mapPolicySetupPauseActive = false;",
            "                                string lateExitArea = vars.mapPolicyExitAreaId;",
            "                                vars.mapPolicyCurrentActive = false;",
            "                                vars.mapPolicyInsideMap = false;",
            "                                vars.mapPolicyInChildArea = false;",
            "                                vars.mapPolicyChildAreaId = \"\";",
            "                                vars.mapPolicyChildSeed = \"\";",
            "                                vars.mapPolicyBossQualified = false;",
            "                                vars.mapPolicyProvisionalExit = false;",
            "                                ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", lateExitArea, 0, 0, fixedFinishedByLateSuccess ? \"fixed-run-complete\" : \"between-maps-setup\");",
            "                                vars.mapPolicySplitKind = \"MAP_SUCCESS_EXIT_ASSIST\";",
            "                                vars.mapPolicySplitTrigger = true;",
            "                                if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_LATE_BOSS_SUCCESS | map=\" + vars.mapPolicyCurrentAreaId + \" | seed=\" + vars.mapPolicyCurrentSeed + \" | boss=\" + eventMapBossName + \" | confirmation=\" + eventConfirmation + System.Environment.NewLine);",
            "                                break;",
            "                            }",
            "",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", vars.mapPolicyCurrentAreaId, vars.mapPolicyCurrentAreaLevel, 0, \"boss-qualified-waiting-for-exit\");",
            "                            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_BOSS_QUALIFIED | map=\" + vars.mapPolicyCurrentAreaId + \" | seed=\" + vars.mapPolicyCurrentSeed + \" | boss=\" + eventMapBossName + \" | confirmation=\" + eventConfirmation + System.Environment.NewLine);",
            "                        }",
            "                        continue;",
            "                    }",
            "",
            "                    if (vars.mapPolicyEndpoint == \"pinnacle\" && System.String.Equals(bossId, vars.mapPolicyPinnacleTarget, System.StringComparison.OrdinalIgnoreCase))",
            "                    {",
            "                        if (bossEventType == \"SEEN\")",
            "                        {",
            "                            vars.mapPolicyPinnacleSeen = true;",
            "                            vars.mapPolicySetupPauseActive = false;",
            "                            string pinnacleSeenName = \"Pinnacle — \" + (vars.mapPolicyPinnacleName != \"\" ? vars.mapPolicyPinnacleName : bossName);",
            "                            try",
            "                            {",
            "                                if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                {",
            "                                    timer.Run[timer.CurrentSplitIndex].Name = pinnacleSeenName;",
            "                                    timer.Run.HasChanged = true;",
            "                                    timer.CallRunManuallyModified();",
            "                                }",
            "                            } catch {}",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"PINNACLE_SEEN\", \"boss=\" + bossId + \";name=\" + bossName);",
            "                            if (timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning && !vars.startTrigger) vars.startTrigger = true;",
            "                        }",
            "                        else if (bossEventType == \"GONE\" && !vars.mapPolicySplitTrigger)",
            "                        {",
            "                            string pinnacleSegmentName = \"Pinnacle — \" + (vars.mapPolicyPinnacleName != \"\" ? vars.mapPolicyPinnacleName : bossName);",
            "                            try",
            "                            {",
            "                                if (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < timer.Run.Count)",
            "                                {",
            "                                    timer.Run[timer.CurrentSplitIndex].Name = pinnacleSegmentName;",
            "                                    timer.Run.HasChanged = true;",
            "                                    timer.CallRunManuallyModified();",
            "                                }",
            "                            } catch {}",
            "                            vars.mapPolicySetupPauseActive = false;",
            "                            vars.mapPolicySplitKind = \"PINNACLE\";",
            "                            vars.mapPolicySplitTrigger = true;",
            "                            ((System.Action<string, string>)vars.mapPolicyAudit)(\"PINNACLE_COMPLETE\", \"boss=\" + bossId + \";name=\" + bossName);",
            "                            ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"off\", \"\", 0, 0, \"pinnacle-complete\");",
            "                            break;",
            "                        }",
            "                    }",
            "                }",
            "            }",
            "        } catch (System.Exception mapBossEx)",
            "        {",
            "            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_BOSS_EVENT_ERROR | \" + mapBossEx.Message + System.Environment.NewLine);",
            "        }",
            "    }",
            "    // MAP_POLICY_V2_UPDATE_END"
        });
        // The base mixed update action clears vars.startTrigger immediately before this
        // guard. Insert the Maps observer after that guard so map/Pinnacle start triggers
        // survive through the remainder of the update tick.
        const string mapUpdateAnchor = "    if (!vars.configValid || vars.reader == null) return false;";
        if (!aslText.Contains(mapUpdateAnchor, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected update guard for Maps policy v2.");
        aslText = aslText.Replace(mapUpdateAnchor, mapUpdateAnchor + Environment.NewLine + Environment.NewLine + updateCode, StringComparison.Ordinal);

        // Generated Maps splits are driven by the lifecycle policy rather than vars.pendingKey.
        const string splitReturn = "    return vars.pendingKey != \"\";";
        if (!aslText.Contains(splitReturn, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected split action for Maps policy v2.");
        aslText = aslText.Replace(splitReturn, "    return (vars.mapPolicyV2Enabled && vars.mapPolicySplitTrigger) || vars.pendingKey != \"\";", StringComparison.Ordinal);

        const string loadingReturn = "    return gtPauseLoad || gtPauseManual;";
        if (!aslText.Contains(loadingReturn, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected mixed ASL does not expose the expected isLoading return for Maps policy v2.");
        aslText = aslText.Replace(loadingReturn,
            "    bool mapPolicySetupPause = vars.mapPolicyV2Enabled && vars.mapPolicyGameTimePolicy == \"completion\" && vars.mapPolicySetupPauseActive;" + Environment.NewLine + Environment.NewLine
            + "    return gtPauseLoad || gtPauseManual || mapPolicySetupPause;", StringComparison.Ordinal);

        var gameTimeCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_GAMETIME_BEGIN",
            "    if (vars.mapPolicyV2Enabled && vars.mapPolicyGameCorrectionPending)",
            "    {",
            "        System.TimeSpan mapCorrected = vars.mapPolicyGameCorrectionTarget;",
            "        if (mapCorrected < System.TimeSpan.Zero) mapCorrected = System.TimeSpan.Zero;",
            "        vars.mapPolicyGameCorrectionPending = false;",
            "        if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "            System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_GAME_TIME_CORRECTION_APPLIED | target=\" + mapCorrected.ToString() + System.Environment.NewLine);",
            "        return mapCorrected;",
            "    }",
            "    // MAP_POLICY_V2_GAMETIME_END"
        });
        aslText = InsertAfterActionOpen(aslText, "gameTime", gameTimeCode, required: true);

        var onStartCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_ON_START_BEGIN",
            "    if (vars.mapPolicyV2Enabled)",
            "    {",
            "        vars.mapPolicyBaseSegmentCount = vars.baseSegmentNames.Count > 0 ? vars.baseSegmentNames.Count : timer.Run.Count;",
            "        if (vars.mapPolicyCurrentActive && vars.mapPolicyAttemptNumber > 0)",
            "            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_ENTER\", \"attempt=\" + vars.mapPolicyAttemptNumber + \";map=\" + vars.mapPolicyCurrentAreaId + \";seed=\" + vars.mapPolicyCurrentSeed + \";level=\" + vars.mapPolicyCurrentAreaLevel + \";runStart=true\");",
            "        else if (vars.mapPolicyPinnacleSeen)",
            "            ((System.Action<string, string>)vars.mapPolicyAudit)(\"PINNACLE_SEEN\", \"boss=\" + vars.mapPolicyPinnacleTarget + \";runStart=true\");",
            "        ((System.Action<string, string>)vars.mapPolicyAudit)(\"MAP_RUN_POLICY\",",
            "            \"endpoint=\" + vars.mapPolicyEndpoint + \";target=\" + vars.mapPolicyTarget + \";deathPolicy=\" + vars.mapPolicyDeathPolicy",
            "            + \";gameTimePolicy=\" + vars.mapPolicyGameTimePolicy",
            "            + \";character=\" + (vars.mapPolicyDeathPolicy == \"none\" ? \"<not-read>\" : vars.mapPolicyCharacter)",
            "            + \";pinnacle=\" + vars.mapPolicyPinnacleTarget);",
            "    }",
            "    // MAP_POLICY_V2_ON_START_END"
        });
        aslText = InsertAfterActionOpen(aslText, "onStart", onStartCode, required: true);

        var onSplitCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_ON_SPLIT_BEGIN",
            "    if (vars.mapPolicyV2Enabled)",
            "    {",
            "        bool mapAutomaticSplit = vars.mapPolicySplitTrigger;",
            "        string mapAutomaticKind = vars.mapPolicySplitKind;",
            "        if (mapAutomaticSplit)",
            "        {",
            "            int mapCompletedIndex = timer.CurrentSplitIndex - 1;",
            "            try",
            "            {",
            "                if (mapCompletedIndex >= 0 && mapCompletedIndex < timer.Run.Count",
            "                    && (vars.mapPolicySplitOverrideHasGame || vars.mapPolicySplitOverrideHasReal))",
            "                {",
            "                    var mapCompletedSegment = timer.Run[mapCompletedIndex];",
            "                    System.TimeSpan? mapReal = vars.mapPolicySplitOverrideHasReal ? (System.TimeSpan?)vars.mapPolicySplitOverrideReal : mapCompletedSegment.SplitTime.RealTime;",
            "                    System.TimeSpan? mapGame = vars.mapPolicySplitOverrideHasGame ? (System.TimeSpan?)vars.mapPolicySplitOverrideGame : mapCompletedSegment.SplitTime.GameTime;",
            "                    mapCompletedSegment.SplitTime = new LiveSplit.Model.Time(mapReal, mapGame);",
            "                    timer.Run.HasChanged = true;",
            "                    timer.CallRunManuallyModified();",
            "                }",
            "            } catch {}",
            "            vars.mapPolicySplitTrigger = false;",
            "            vars.mapPolicySplitKind = \"\";",
            "            vars.mapPolicySplitOverrideHasGame = false;",
            "            vars.mapPolicySplitOverrideHasReal = false;",
            "            if (settings[\"debugLog\"]) System.IO.File.AppendAllText(vars.debugPath,",
            "                System.DateTime.Now.ToString(\"s\") + \" MAP_POLICY_SPLIT_COMMITTED | kind=\" + mapAutomaticKind + \" | index=\" + mapCompletedIndex + System.Environment.NewLine);",
            "        }",
            "        else if (vars.mapPolicyEndpoint == \"manual\" && timer.CurrentPhase == LiveSplit.Model.TimerPhase.Ended)",
            "        {",
            "            int manualCompletedIndex = timer.CurrentSplitIndex - 1;",
            "            try",
            "            {",
            "                if (manualCompletedIndex >= 0 && manualCompletedIndex < timer.Run.Count)",
            "                {",
            "                    timer.Run[manualCompletedIndex].Name = \"Manual Finish\";",
            "                    timer.Run.HasChanged = true;",
            "                    timer.CallRunManuallyModified();",
            "                }",
            "            } catch {}",
            "            ((System.Action<string, string>)vars.mapPolicyAudit)(\"MANUAL_FINISH\", \"index=\" + manualCompletedIndex);",
            "        }",
            "    }",
            "    // MAP_POLICY_V2_ON_SPLIT_END"
        });
        aslText = InsertBeforeActionClose(aslText, "onSplit", onSplitCode, required: true);

        var resetCode = string.Join(Environment.NewLine, new[]
        {
            "    // MAP_POLICY_V2_RESET_BEGIN",
            "    if (vars.mapPolicyV2Enabled)",
            "    {",
            "        vars.mapPolicyCurrentActive = false;",
            "        vars.mapPolicyInsideMap = false;",
            "        vars.mapPolicyInChildArea = false;",
            "        vars.mapPolicyChildAreaId = \"\";",
            "        vars.mapPolicyChildSeed = \"\";",
            "        vars.mapPolicyCurrentAreaId = \"\";",
            "        vars.mapPolicyCurrentSeed = \"\";",
            "        vars.mapPolicyCurrentAreaLevel = 0;",
            "        vars.mapPolicyCurrentScene = \"\";",
            "        vars.mapPolicyAwaitingSceneName = false;",
            "        vars.mapPolicyAttemptNumber = 0;",
            "        vars.mapPolicyFinalizedCount = 0;",
            "        vars.mapPolicySuccessCount = 0;",
            "        vars.mapPolicyFailureCount = 0;",
            "        vars.mapPolicyCurrentDeathCount = 0;",
            "        vars.mapPolicyRunDeathCount = 0;",
            "        vars.mapPolicyBossQualified = false;",
            "        vars.mapPolicyProvisionalExit = false;",
            "        vars.mapPolicyExitHasGame = false;",
            "        vars.mapPolicyExitHasReal = false;",
            "        vars.mapPolicyExitAreaId = \"\";",
            "        vars.mapPolicyExitClass = \"\";",
            "        vars.mapPolicyLastFinalizedAreaId = \"\";",
            "        vars.mapPolicyLastFinalizedSeed = \"\";",
            "        vars.mapPolicySetupPauseActive = false;",
            "        vars.mapPolicySplitTrigger = false;",
            "        vars.mapPolicySplitKind = \"\";",
            "        vars.mapPolicySplitOverrideHasGame = false;",
            "        vars.mapPolicySplitOverrideHasReal = false;",
            "        vars.mapPolicyGameCorrectionPending = false;",
            "        vars.mapPolicyPinnacleSeen = false;",
            "        try",
            "        {",
            "            int mapBaseCount = vars.mapPolicyBaseSegmentCount > 0 ? vars.mapPolicyBaseSegmentCount : 1;",
            "            while (timer.Run.Count > mapBaseCount) timer.Run.RemoveAt(timer.Run.Count - 1);",
            "            if (timer.Run.Count > 0) timer.Run[0].Name = \"Map [1] — Waiting for map entry\";",
            "            timer.Run.HasChanged = true;",
            "            timer.CallRunManuallyModified();",
            "        } catch {}",
            "        try { vars.mapPolicyProcessedBossLines = System.IO.File.Exists(vars.eventPath) ? System.IO.File.ReadAllLines(vars.eventPath).Length : 0; } catch {}",
            "        ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", \"\", 0, 0, \"maps-policy-v2-reset\");",
            "    }",
            "    // MAP_POLICY_V2_RESET_END"
        });
        aslText = InsertBeforeActionClose(aslText, "onReset", resetCode, required: true);

        foreach (var terminalAction in new[] { "exit", "shutdown" })
        {
            if (!HasAction(aslText, terminalAction)) continue;
            var terminalCode = string.Join(Environment.NewLine, new[]
            {
                $"    // MAP_POLICY_V2_{terminalAction.ToUpperInvariant()}_BEGIN",
                "    if (vars.mapPolicyV2Enabled)",
                "    {",
                "        try { if (vars.mapPolicyReader != null) vars.mapPolicyReader.Close(); } catch {}",
                "        vars.mapPolicyReader = null;",
                "        ((System.Action<string, string, int, int, string>)vars.mapPolicyWriteContext)(\"identity\", \"\", 0, 0, \"maps-policy-v2-" + terminalAction + "\");",
                "    }",
                $"    // MAP_POLICY_V2_{terminalAction.ToUpperInvariant()}_END"
            });
            aslText = InsertBeforeActionClose(aslText, terminalAction, terminalCode, required: false);
        }

        return aslText;
    }

    public static string ApplyGameTimeOptions(string aslText, bool excludeManualPauses)
    {
        if (!ManualPauseDefaultRegex.IsMatch(aslText))
            throw new InvalidOperationException("The selected ASL does not expose the expected manual-pause Game Time setting.");

        var replacement = "settings.Add(\"manualPauseRemoval\", " + (excludeManualPauses ? "true" : "false") + ",";
        return ManualPauseDefaultRegex.Replace(aslText, replacement, 1);
    }

    public static string PrependRiverbankRouteEntry(string runtimeText)
    {
        var newline = runtimeText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = Regex.Split(runtimeText, "\r\n|\n").ToList();

        foreach (var raw in lines)
        {
            var line = raw;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;
            if (line.Equals("G1_1", StringComparison.OrdinalIgnoreCase))
                return runtimeText;
            break;
        }

        var insertAt = 0;
        while (insertAt < lines.Count)
        {
            var trimmed = lines[insertAt].Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
                break;
            insertAt++;
        }

        lines.Insert(insertAt, "G1_1                                   # The Riverbank");
        return string.Join(newline, lines);
    }

    public static void WritePresetSplits(string sourcePath, string outputPath, bool prependRiverbank)
    {
        if (!prependRiverbank)
        {
            File.Copy(sourcePath, outputPath, true);
            return;
        }

        var run = XDocument.Load(sourcePath, LoadOptions.PreserveWhitespace);
        var segments = run.Root?.Element("Segments")
            ?? throw new InvalidOperationException("The selected .lss file does not contain a Segments element.");

        var firstName = segments.Elements("Segment").FirstOrDefault()?.Element("Name")?.Value ?? "";
        if (!firstName.Equals("The Riverbank", StringComparison.OrdinalIgnoreCase))
            segments.AddFirst(CreateSegment("The Riverbank"));

        run.Save(outputPath);
    }

    public static void AdjustAreaChecklistSplits(string outputPath, string runtimeText)
    {
        string startId = "";
        var objectives = new List<string>();

        foreach (var raw in Regex.Split(runtimeText, "\r\n|\n"))
        {
            var line = raw;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("@start=", StringComparison.OrdinalIgnoreCase))
            {
                var value = line[7..].Trim();
                startId = value.Equals("manual", StringComparison.OrdinalIgnoreCase) ? "" : value;
                continue;
            }
            if (line.StartsWith("@", StringComparison.Ordinal)) continue;
            objectives.Add(line);
        }

        var implicitStart = startId.Length > 0 && objectives.Contains(startId, StringComparer.OrdinalIgnoreCase) ? 1 : 0;
        var expectedCount = objectives.Count - implicitStart;
        if (expectedCount < 1)
            throw new InvalidOperationException("The Area Checklist preset must contain at least one timed split after applying the start policy.");

        var run = XDocument.Load(outputPath, LoadOptions.PreserveWhitespace);
        var segments = run.Root?.Element("Segments")
            ?? throw new InvalidOperationException("The selected .lss file does not contain a Segments element.");
        var list = segments.Elements("Segment").ToList();

        while (list.Count > expectedCount)
        {
            list[^1].Remove();
            list.RemoveAt(list.Count - 1);
        }
        while (list.Count < expectedCount)
        {
            var segment = CreateSegment($"Objective {list.Count + 1:D3}");
            segments.Add(segment);
            list.Add(segment);
        }

        // Area Checklist layouts use generic objective rows that are renamed dynamically.
        // Keep numbering contiguous after a row is added or removed for a changed start area.
        for (var i = 0; i < list.Count; i++)
        {
            var name = list[i].Element("Name");
            if (name is not null && name.Value.StartsWith("Objective ", StringComparison.OrdinalIgnoreCase))
                name.Value = $"Objective {i + 1:D3}";
        }

        run.Save(outputPath);
    }

    public static void WritePremadeSplits(string outputPath, IReadOnlyList<RouteEntry> objectives, string categoryName)
    {
        var segments = new XElement("Segments");
        foreach (var objective in objectives)
            segments.Add(CreateSegment(objective.Name));

        var run = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Run", new XAttribute("version", "1.7.0"),
                new XElement("GameIcon"),
                new XElement("GameName", "Path of Exile 2"),
                new XElement("CategoryName", categoryName),
                new XElement("Metadata",
                    new XElement("Run", new XAttribute("id", "")),
                    new XElement("Platform", new XAttribute("usesEmulator", "False"), "PC"),
                    new XElement("Region"),
                    new XElement("Variables")),
                new XElement("Offset", "00:00:00"),
                new XElement("AttemptCount", 0),
                segments));
        run.Save(outputPath);
    }

    public static void WriteCustomSplits(string outputPath, IReadOnlyList<RouteEntry> objectives)
    {
        var segments = new XElement("Segments");
        for (var i = 0; i < objectives.Count; i++)
        {
            var objective = objectives[i];
            var typeLabel = objective.Type.ToLowerInvariant() switch
            {
                "boss" => "Boss",
                "bossocc" => "Repeated Boss",
                "bossall" => "Boss Pair",
                "bossany" => "Dynamic Boss",
                "bossnth" => "Nth Dynamic Boss",
                "bossslot" => "Dynamic Boss",
                "mapboss" => "Map Boss",
                "level" => "Level",
                "areaocc" => "Repeated Area",
                _ => "Area"
            };
            segments.Add(CreateSegment($"{objective.Name} [{typeLabel}]"));
        }

        var run = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Run", new XAttribute("version", "1.7.0"),
                new XElement("GameIcon"),
                new XElement("GameName", "Path of Exile 2"),
                new XElement("CategoryName", "Custom Route - Areas + Bosses + Levels"),
                new XElement("Metadata",
                    new XElement("Run", new XAttribute("id", "")),
                    new XElement("Platform", new XAttribute("usesEmulator", "False"), "PC"),
                    new XElement("Region"),
                    new XElement("Variables")),
                new XElement("Offset", "00:00:00"),
                new XElement("AttemptCount", 0),
                segments));
        run.Save(outputPath);
    }

    private static XElement CreateSegment(string name) => new(
        "Segment",
        new XElement("Name", name),
        new XElement("Icon"),
        new XElement("SplitTimes", new XElement("SplitTime", new XAttribute("name", "Personal Best"))),
        new XElement("BestSegmentTime"),
        new XElement("SegmentHistory"));

    private static bool HasAction(string text, string action) =>
        Regex.IsMatch(text, $@"(?m)^\s*{Regex.Escape(action)}\s*(?:\r?\n\s*)?\{{");

    private static string InsertAfterActionOpen(string text, string action, string code, bool required)
    {
        var regex = new Regex($@"(?m)^\s*{Regex.Escape(action)}\s*(?:\r?\n\s*)?\{{", RegexOptions.Compiled);
        var match = regex.Match(text);
        if (!match.Success)
        {
            if (required) throw new InvalidOperationException($"Could not locate the {action} action in the selected ASL.");
            return text;
        }

        var newline = DetectNewline(text);
        return text.Insert(match.Index + match.Length, newline + code);
    }

    private static string InsertBeforeAction(string text, string action, string block)
    {
        var regex = new Regex($@"(?m)^\s*{Regex.Escape(action)}\s*(?:\r?\n\s*)?\{{", RegexOptions.Compiled);
        var match = regex.Match(text);
        if (!match.Success)
            throw new InvalidOperationException($"Could not locate the {action} action in the selected ASL.");
        return text.Insert(match.Index, block);
    }

    private static string InsertBeforeActionClose(string text, string action, string code, bool required)
    {
        var regex = new Regex($@"(?m)^\s*{Regex.Escape(action)}\s*(?:\r?\n\s*)?\{{", RegexOptions.Compiled);
        var match = regex.Match(text);
        if (!match.Success)
        {
            if (required) throw new InvalidOperationException($"Could not locate the {action} action in the selected ASL.");
            return text;
        }

        var openBrace = text.IndexOf('{', match.Index);
        var closeBrace = FindMatchingBrace(text, openBrace);
        if (closeBrace < 0)
        {
            if (required) throw new InvalidOperationException($"Could not locate the closing brace for the {action} action in the selected ASL.");
            return text;
        }

        var newline = DetectNewline(text);
        return text.Insert(closeBrace, newline + code + newline);
    }

    private static int FindMatchingBrace(string text, int openBrace)
    {
        if (openBrace < 0 || openBrace >= text.Length || text[openBrace] != '{') return -1;
        var depth = 0;
        var inString = false;
        var inChar = false;
        var inLineComment = false;
        var inBlockComment = false;
        var verbatimString = false;

        for (var i = openBrace; i < text.Length; i++)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n') inLineComment = false;
                continue;
            }
            if (inBlockComment)
            {
                if (c == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }
            if (inString)
            {
                if (verbatimString)
                {
                    if (c == '"' && next == '"') { i++; continue; }
                    if (c == '"') { inString = false; verbatimString = false; }
                }
                else
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '"') inString = false;
                }
                continue;
            }
            if (inChar)
            {
                if (c == '\\') { i++; continue; }
                if (c == '\'') inChar = false;
                continue;
            }

            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (c == '@' && next == '"') { inString = true; verbatimString = true; i++; continue; }
            if (c == '"') { inString = true; verbatimString = false; continue; }
            if (c == '\'') { inChar = true; continue; }

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static string DetectNewline(string text) => text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    private static string QuoteCSharp(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
