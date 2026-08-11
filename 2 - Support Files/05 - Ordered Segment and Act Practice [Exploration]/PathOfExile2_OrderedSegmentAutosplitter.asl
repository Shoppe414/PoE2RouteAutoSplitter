/*
Path of Exile 2 Ordered Segment / Act Practice AutoSplitter for LiveSplit
v1.1.0 validation build

Config: <LiveSplit folder>\poe2_segment_route.txt
- @start=<area id> sets the auto-start trigger.
- Every following area ID is an ordered split target.
- Unexpected areas are ignored without advancing the route.
- The last route target ends naturally when the .lss has the same number of rows.
*/
state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}
startup
{
    refreshRate = 20;
    settings.Add("autoStart", true, "Auto-start on the configured @start area");
    settings.Add("debugLog", true, "Write Segment Practice diagnostic log");
    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.configPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_segment_route.txt");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_segment_practice_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_segment_practice_debug.log");
    vars.reader = null;
    vars.route = new System.Collections.Generic.List<string>();
    vars.routeIndex = 0;
    vars.startAreaId = "";
    vars.startTrigger = false;
    vars.pending = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.areaRegex = new System.Text.RegularExpressions.Regex("^[^ ]+ [^ ]+ (\\d+).*Generating level (\\d+) area \\\"([^\\\"]+)\\\"", System.Text.RegularExpressions.RegexOptions.Compiled);
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
}
init
{
    vars.route = new System.Collections.Generic.List<string>(); vars.routeIndex = 0; vars.startAreaId = ""; vars.startTrigger = false; vars.pending = false; vars.configValid = false; vars.lastAreaId = "";
    try
    {
        if (!System.IO.File.Exists(vars.configPath)) throw new System.Exception("Missing poe2_segment_route.txt beside LiveSplit.exe");
        foreach (string raw in System.IO.File.ReadAllLines(vars.configPath))
        {
            string line=raw; int hash=line.IndexOf('#'); if(hash>=0) line=line.Substring(0,hash); line=line.Trim(); if(line=="") continue;
            if (line.StartsWith("@start=", System.StringComparison.OrdinalIgnoreCase)) { vars.startAreaId=line.Substring(7).Trim(); continue; }
            if (!vars.areaNames.ContainsKey(line)) throw new System.Exception("Unknown route area ID: " + line);
            vars.route.Add(line);
        }
        if (vars.startAreaId=="" || !vars.areaNames.ContainsKey(vars.startAreaId)) throw new System.Exception("A valid @start=<area id> is required");
        if (vars.route.Count==0) throw new System.Exception("No route split targets configured");
        int runCount=0; foreach(LiveSplit.Model.ISegment s in timer.Run) runCount++;
        if(runCount!=vars.route.Count) throw new System.Exception("LiveSplit segment count ("+runCount+") must equal route target count ("+vars.route.Count+")");
        string gameDir=System.IO.Path.GetDirectoryName(modules.First().FileName); vars.clientLogPath=System.IO.Path.Combine(gameDir,"logs","Client.txt");
        var fs=new System.IO.FileStream(vars.clientLogPath,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.ReadWrite); fs.Seek(0,System.IO.SeekOrigin.End); vars.reader=new System.IO.StreamReader(fs); vars.configValid=true;
        string ready="READY | v1.1.0 | Mode=ORDERED_SEGMENT | Start="+vars.areaNames[vars.startAreaId]+" | Targets="+vars.route.Count+" | Client.txt="+vars.clientLogPath;
        System.IO.File.WriteAllText(vars.statusPath,ready+System.Environment.NewLine); if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" "+ready+System.Environment.NewLine); print("[PoE2 ASL] "+ready);
    }
    catch(System.Exception ex) { vars.configValid=false; try{if(vars.reader!=null)vars.reader.Close();}catch{} vars.reader=null; System.IO.File.WriteAllText(vars.statusPath,"ERROR | "+ex.Message+System.Environment.NewLine); print("[PoE2 ASL] ORDERED_SEGMENT ERROR: "+ex.Message); }
}
update
{
    vars.startTrigger=false; if(!vars.configValid||vars.reader==null) return false; if(vars.pending) return true;
    while(vars.reader.Peek()>=0)
    {
        string line=vars.reader.ReadLine(); var m=vars.areaRegex.Match(line); if(!m.Success) continue; string id=m.Groups[3].Value;
        if(System.String.Equals(id,vars.lastAreaId,System.StringComparison.OrdinalIgnoreCase)) continue; vars.lastAreaId=id;
        if(System.String.Equals(id,vars.startAreaId,System.StringComparison.OrdinalIgnoreCase)) { vars.startTrigger=true; if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" START_TRIGGER "+id+" "+vars.areaNames[id]+System.Environment.NewLine); break; }
        if(timer.CurrentPhase==LiveSplit.Model.TimerPhase.NotRunning) continue;
        if(vars.routeIndex>=vars.route.Count) continue;
        string expected=vars.route[vars.routeIndex];
        if(System.String.Equals(id,expected,System.StringComparison.OrdinalIgnoreCase)) { vars.pending=true; if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" ROUTE_MATCH "+id+" "+vars.areaNames[id]+" | routeIndex="+vars.routeIndex+System.Environment.NewLine); break; }
        else if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" IGNORE "+id+" "+(vars.areaNames.ContainsKey(id)?vars.areaNames[id]:"<unknown>")+" | expected="+expected+" "+vars.areaNames[expected]+System.Environment.NewLine);
    }
    return true;
}
start { if(settings["autoStart"]&&vars.startTrigger) return true; }
split { return vars.pending; }
onSplit
{
    if(vars.pending) { int old=vars.routeIndex; vars.routeIndex=timer.CurrentSplitIndex; vars.pending=false; if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" SPLIT_COMMITTED | routeIndex="+old+" -> "+vars.routeIndex+" | phase="+timer.CurrentPhase.ToString()+System.Environment.NewLine); }
}
onReset { vars.routeIndex=0; vars.startTrigger=false; vars.pending=false; vars.lastAreaId=""; if(settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,System.DateTime.Now.ToString("s")+" RESET"+System.Environment.NewLine); }
exit { try{if(vars.reader!=null)vars.reader.Close();}catch{} vars.reader=null; }
shutdown { try{if(vars.reader!=null)vars.reader.Close();}catch{} vars.reader=null; }
