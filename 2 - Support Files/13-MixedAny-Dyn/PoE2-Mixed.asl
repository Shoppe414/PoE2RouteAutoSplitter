/*
Path of Exile 2 Mixed Objective AutoSplitter for LiveSplit
v1.2.1 integration build

Combines Exploration area-completion events from Client.txt with Boss Rush GONE events
from poe2_boss_events.log. One mixed objective file controls both sources.

Config beside LiveSplit.exe: poe2_mixed_route.txt
  @start=G1_1        or @start=manual
  @order=unordered   or @order=ordered
  @areaCompletion=entry or @areaCompletion=successor
  area|G1_town
  boss|the_bloated_miller

The LiveSplit layout must contain exactly one segment per objective line.
*/

state("PathOfExileSteam") {}
state("PathOfExile") {}
state("PathOfExile_x64Steam") {}
state("PathOfExile_x64") {}

startup
{
    refreshRate = 30;
    settings.Add("autoStart", true, "Auto-start on @start (Riverbank waits for the Wounded Man final opening line)");
    settings.Add("dynamicSegmentNames", true, "Rename generic split slots to completed area/boss names");
    settings.Add("debugLog", true, "Write mixed objective diagnostic log");
    settings.Add("enteredNameFallback", true, "Fallback to Client.txt 'You have entered ...' messages");

    var liveSplitExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    vars.liveSplitDir = System.IO.Path.GetDirectoryName(liveSplitExe);
    vars.configPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_mixed_route.txt");
    vars.eventPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_boss_events.log");
    vars.statusPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_mixed_route_status.txt");
    vars.debugPath = System.IO.Path.Combine(vars.liveSplitDir, "poe2_mixed_route_debug.log");

    vars.reader = null;
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
    vars.nameToAreaId = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    foreach (var p in vars.areaNames) if (!vars.nameToAreaId.ContainsKey(p.Value)) vars.nameToAreaId[p.Value] = p.Key;

    vars.bossNames = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.bossNames["the_bloated_miller"] = "The Bloated Miller";
    vars.bossNames["beira_of_the_rotten_pack"] = "Beira of the Rotten Pack";
    vars.bossNames["the_devourer"] = "The Devourer";
    vars.bossNames["the_brambleghast"] = "The Brambleghast";
    vars.bossNames["the_rust_king"] = "The Rust King";
    vars.bossNames["the_rotten_druid"] = "The Rotten Druid";
    vars.bossNames["asinia"] = "Asinia, the Praetor's Consort";
    vars.bossNames["lachlann"] = "Lachlann of Endless Lament";
    vars.bossNames["draven"] = "Draven, the Eternal Praetor";
    vars.bossNames["crowbell"] = "The Crowbell";
    vars.bossNames["king_in_the_mists"] = "The King in the Mists";
    vars.bossNames["the_executioner"] = "The Executioner";
    vars.bossNames["candlemass"] = "Candlemass, the Living Rite";
    vars.bossNames["count_geonor"] = "Count Geonor";
    vars.bossNames["rathbreaker"] = "Rathbreaker";
    vars.bossNames["rudja_the_dread_engineer"] = "Rudja, the Dread Engineer";
    vars.bossNames["balbala"] = "Balbala, the Traitor";
    vars.bossNames["jamanra_risen_king"] = "Jamanra, the Risen King";
    vars.bossNames["kabala_constrictor_queen"] = "Kabala, Constrictor Queen";
    vars.bossNames["azarian_forsaken_son"] = "Azarian, the Forsaken Son";
    vars.bossNames["iktab_the_deathlord"] = "Iktab, the Deathlord";
    vars.bossNames["ekbab_ancient_steed"] = "Ekbab, Ancient Steed";
    vars.bossNames["zalmarath_the_colossus"] = "Zalmarath, the Colossus";
    vars.bossNames["tor_gul_the_defiler"] = "Tor Gul, the Defiler";
    vars.bossNames["jamanra_abomination"] = "Jamanra, the Abomination";
    vars.bossNames["rootdredge"] = "Rootdredge";
    vars.bossNames["mighty_silverfist"] = "Mighty Silverfist";
    vars.bossNames["xyclucian_the_chimera"] = "Xyclucian, the Chimera";
    vars.bossNames["blackjaw_the_remnant"] = "Blackjaw, the Remnant";
    vars.bossNames["zicoatl_warden_core"] = "Zicoatl, Warden of the Core";
    vars.bossNames["ignagduk"] = "Ignagduk, the Bog Witch";
    vars.bossNames["mektul_forgemaster"] = "Mektul, the Forgemaster";
    vars.bossNames["queen_of_filth"] = "The Queen of Filth";
    vars.bossNames["ketzuli_high_priest_sun"] = "Ketzuli, High Priest of the Sun";
    vars.bossNames["viper_napuatzi"] = "Viper Napuatzi";
    vars.bossNames["doryani_royal_thaumaturge"] = "Doryani, Royal Thaumaturge";
    vars.bossNames["captain_hartlin"] = "Captain Hartlin";
    vars.bossNames["omniphobia_fear_manifest"] = "Omniphobia, Fear Manifest";
    vars.bossNames["great_white_one"] = "Great White One";
    vars.bossNames["diamora_song_of_death"] = "Diamora, Song of Death";
    vars.bossNames["the_prisoner"] = "The Prisoner";
    vars.bossNames["the_blind_beast"] = "The Blind Beast";
    vars.bossNames["krutog_lord_of_kin"] = "Krutog, Lord of Kin";
    vars.bossNames["scourge_of_the_sky"] = "Scourge of the Skies";
    vars.bossNames["yama_the_white"] = "Yama The White";
    vars.bossNames["torvian_hand_saviour"] = "Torvian, Hand of the Saviour";
    vars.bossNames["benedictus_first_herald"] = "Benedictus, First Herald of Utopia";
    vars.bossNames["tavakai_the_chieftain"] = "Tavakai, the Chieftain";
    vars.bossNames["isolde_white_shroud"] = "Isolde of the White Shroud";
    vars.bossNames["heldra_black_pyre"] = "Heldra of the Black Pyre";
    vars.bossNames["siora_blade_mists"] = "Siora, Blade of the Mists";
    vars.bossNames["sigbert_sullied_oath"] = "Sigbert of the Sullied Oath";
    vars.bossNames["godwin_shattered_creed"] = "Godwin of the Shattered Creed";
    vars.bossNames["oswin_dread_warden"] = "Oswin, the Dread Warden";
    vars.bossNames["thane_wulfric"] = "Thane Wulfric";
    vars.bossNames["lady_elswyth"] = "Lady Elswyth";
    vars.bossNames["akthi_final_sting"] = "Akthi, the Final Sting";
    vars.bossNames["anundr_sandworm"] = "Anundr, the Sandworm";
    vars.bossNames["elzarah_cobra_lord"] = "Elzarah, the Cobra Lord";
    vars.bossNames["vornas_fell_flame"] = "Vornas, the Fell Flame";
    vars.bossNames["azmadi_faridun_prince"] = "Azmadi, the Faridun Prince";
    vars.bossNames["lythara_wayward_spear"] = "Lythara, the Wayward Spear";
    vars.bossNames["abominable_yeti"] = "The Abominable Yeti";
    vars.bossNames["rakkar_frozen_talon"] = "Rakkar, the Frozen Talon";
    vars.bossNames["stormgore_guardian"] = "Stormgore, the Guardian";
    vars.bossNames["zelina_blood_priestess"] = "Zelina, Blood Priestess";
    vars.bossNames["zolin_blood_priest"] = "Zolin, Blood Priest";
    vars.bossNames["atziri_red_queen"] = "Atziri, the Red Queen";
    vars.bossNames["the_aberration"] = "The Aberration";
    vars.bossNames["arbiter_of_ash"] = "The Arbiter of Ash";
    vars.bossNames["arbiter_of_divinity"] = "The Arbiter of Divinity";
    vars.bossNames["the_bodach"] = "The Bodach";
    vars.bossNames["raven_trickster"] = "The Raven Trickster";
    vars.bossNames["the_trialmaster"] = "The Trialmaster";
    vars.bossNames["vessel_of_kulemak"] = "Vessel of Kulemak";
    vars.bossNames["xesht_we_that_are_one"] = "Xesht, We That Are One";
    vars.bossNames["zarokh_temporal"] = "Zarokh, the Temporal";
    vars.bossNames["rattlecage_earthbreaker"] = "Rattlecage, the Earthbreaker";
    vars.bossNames["hadi_flaming_river"] = "Hadi of the Flaming River";
    vars.bossNames["rafiq_frozen_spring"] = "Rafiq of the Frozen Spring";
    vars.bossNames["ashar_sand_mother"] = "Ashar, the Sand Mother";
    vars.bossNames["uxmal_beastlord"] = "Uxmal, the Beastlord";
    vars.bossNames["chetza_feathered_plague"] = "Chetza, the Feathered Plague";
    vars.bossNames["bahlak_sky_seer"] = "Bahlak, the Sky Seer";

    vars.areaRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*Generating level (\\d+) area \"([^\"]+)\"",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );
    vars.enteredNameRegex = new System.Text.RegularExpressions.Regex(
        "^[^ ]+ [^ ]+ (\\d+).*: You have entered (.+)\.$",
        System.Text.RegularExpressions.RegexOptions.Compiled
    );

    vars.objectiveOrder = new System.Collections.Generic.List<string>();
    vars.objectiveSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaSequence = new System.Collections.Generic.List<string>();
    vars.areaSuccessor = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.armedAreaObjectives = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaCompletionQueue = new System.Collections.Generic.List<string>();
    vars.areaSuccessorCompletion = false;
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();
    vars.startAreaId = "";
    vars.ordered = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.pendingKey = "";
    vars.pendingName = "";
    vars.pendingType = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
    vars.processedLineCount = 0;
    vars.nextBossPollUtc = System.DateTime.MinValue;
    vars.lastObservedIndex = -1;
    vars.lastUndoneKey = "";


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

    vars.objectiveOrder = new System.Collections.Generic.List<string>();
    vars.objectiveSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaSequence = new System.Collections.Generic.List<string>();
    vars.areaSuccessor = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    vars.armedAreaObjectives = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaCompletionQueue = new System.Collections.Generic.List<string>();
    vars.areaSuccessorCompletion = false;
    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.baseSegmentNames = new System.Collections.Generic.List<string>();
    vars.startAreaId = "";
    vars.ordered = false;
    vars.configValid = false;
    vars.lastAreaId = "";
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.pendingKey = "";
    vars.pendingName = "";
    vars.pendingType = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
    vars.lastUndoneKey = "";
    vars.nextBossPollUtc = System.DateTime.MinValue;
    vars.lastObservedIndex = timer.CurrentSplitIndex;

    try
    {
        if (!System.IO.File.Exists(vars.configPath))
            throw new System.Exception("Missing poe2_mixed_route.txt beside LiveSplit.exe");

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
            if (line.StartsWith("@order=", System.StringComparison.OrdinalIgnoreCase))
            {
                string value = line.Substring(7).Trim();
                if (System.String.Equals(value, "ordered", System.StringComparison.OrdinalIgnoreCase)) vars.ordered = true;
                else if (System.String.Equals(value, "unordered", System.StringComparison.OrdinalIgnoreCase)) vars.ordered = false;
                else throw new System.Exception("@order must be ordered or unordered");
                continue;
            }
            if (line.StartsWith("@areaCompletion=", System.StringComparison.OrdinalIgnoreCase))
            {
                string value = line.Substring("@areaCompletion=".Length).Trim();
                if (System.String.Equals(value, "successor", System.StringComparison.OrdinalIgnoreCase)) vars.areaSuccessorCompletion = true;
                else if (System.String.Equals(value, "entry", System.StringComparison.OrdinalIgnoreCase)) vars.areaSuccessorCompletion = false;
                else throw new System.Exception("@areaCompletion must be successor or entry");
                continue;
            }

            string[] parts = line.Split(new char[] { '|' }, 2);
            if (parts.Length != 2) throw new System.Exception("Objective must use area|ID or boss|ID: " + line);
            string type = parts[0].Trim().ToLowerInvariant();
            string id = parts[1].Trim();
            string key = type + ":" + id;
            if (type == "area")
            {
                if (!vars.areaNames.ContainsKey(id)) throw new System.Exception("Unknown area ID: " + id);
                vars.areaSequence.Add(key);
            }
            else if (type == "boss")
            {
                if (!vars.bossNames.ContainsKey(id)) throw new System.Exception("Unknown/unsupported boss ID: " + id);
            }
            else throw new System.Exception("Unknown objective type: " + type);
            if (!vars.objectiveSet.Add(key)) throw new System.Exception("Duplicate objective: " + line);
            vars.objectiveOrder.Add(key);
        }

        if (vars.areaSuccessorCompletion)
        {
            if (vars.areaSequence.Count == 0) throw new System.Exception("Successor area completion requires at least one area objective");
            for (int i = 0; i < vars.areaSequence.Count; i++)
            {
                string areaKey = vars.areaSequence[i];
                string triggerKey = i + 1 < vars.areaSequence.Count ? vars.areaSequence[i + 1] : areaKey;
                vars.areaSuccessor[areaKey] = triggerKey.Substring("area:".Length);
            }
        }

        if (vars.startAreaId != "" && !vars.areaNames.ContainsKey(vars.startAreaId))
            throw new System.Exception("Unknown @start area ID: " + vars.startAreaId);
        if (vars.objectiveOrder.Count == 0) throw new System.Exception("No mixed objectives configured");

        int runCount = 0;
        foreach (LiveSplit.Model.ISegment segment in timer.Run)
        {
            vars.baseSegmentNames.Add(segment.Name);
            runCount++;
        }
        if (runCount != vars.objectiveOrder.Count)
            throw new System.Exception("LiveSplit segment count (" + runCount + ") must equal mixed objective count (" + vars.objectiveOrder.Count + ")");

        string gameDir = System.IO.Path.GetDirectoryName(modules.First().FileName);
        vars.clientLogPath = System.IO.Path.Combine(gameDir, "logs", "Client.txt");
        var fs = new System.IO.FileStream(vars.clientLogPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        fs.Seek(0, System.IO.SeekOrigin.End);
        vars.reader = new System.IO.StreamReader(fs);

        if (!System.IO.File.Exists(vars.eventPath))
            System.IO.File.WriteAllText(vars.eventPath, "# Created by LiveSplit mixed objective bridge" + System.Environment.NewLine);
        vars.processedLineCount = System.IO.File.ReadAllLines(vars.eventPath).Length;
        vars.configValid = true;

        string startName = vars.startAreaId == "" ? "manual" : vars.areaNames[vars.startAreaId];
        string ready = "READY | v1.2.1 | Mode=MIXED_" + (vars.ordered ? "ORDERED" : "UNORDERED")
            + " | Objectives=" + vars.objectiveOrder.Count + " | Start=" + startName
            + " | AreaCompletion=" + (vars.areaSuccessorCompletion ? "SUCCESSOR_ENTRY" : "ENTRY")
            + " | Client.txt=" + vars.clientLogPath + " | BossEvents=" + vars.eventPath;
        System.IO.File.WriteAllText(vars.statusPath, ready + System.Environment.NewLine);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " " + ready + System.Environment.NewLine);
        print("[PoE2 Mixed ASL] " + ready);
    }
    catch (System.Exception ex)
    {
        vars.configValid = false;
        try { if (vars.reader != null) vars.reader.Close(); } catch {}
        vars.reader = null;
        System.IO.File.WriteAllText(vars.statusPath, "ERROR | " + ex.Message + System.Environment.NewLine);
        print("[PoE2 Mixed ASL] ERROR: " + ex.Message);
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

    int idx = timer.CurrentSplitIndex;
    if (idx < vars.lastObservedIndex)
    {
        int undoCount = vars.lastObservedIndex - idx;
        while (undoCount > 0 && vars.completedOrder.Count > 0)
        {
            string key = vars.completedOrder[vars.completedOrder.Count - 1];
            vars.completedOrder.RemoveAt(vars.completedOrder.Count - 1);
            vars.completed.Remove(key);
            if (vars.areaSuccessorCompletion && key.StartsWith("area:", System.StringComparison.OrdinalIgnoreCase))
                vars.armedAreaObjectives.Add(key);
            vars.lastUndoneKey = key;
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
                System.DateTime.Now.ToString("s") + " UNDO_REARM | objective=" + key + " | liveSplitIndex=" + idx + System.Environment.NewLine);
            undoCount--;
        }
    }
    else if (idx > vars.lastObservedIndex && vars.pendingKey == "" && vars.lastUndoneKey != "")
    {
        vars.suppressed.Add(vars.lastUndoneKey);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " MANUAL_SKIP_SUPPRESS | objective=" + vars.lastUndoneKey + " | liveSplitIndex=" + idx + System.Environment.NewLine);
        vars.lastUndoneKey = "";
    }
    vars.lastObservedIndex = idx;

    if (vars.pendingKey != "") return true;

    // Successor-entry area completion can queue more than one objective for the
    // same transition (notably Cuachic Vault + terminal Ziggurat). Drain queued
    // objectives before polling new BossWatcher or Client.txt events.
    if (vars.areaCompletionQueue.Count > 0
        && (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))
    {
        string queuedKey = vars.areaCompletionQueue[0];
        vars.areaCompletionQueue.RemoveAt(0);
        if (!vars.completed.Contains(queuedKey) && !vars.suppressed.Contains(queuedKey))
        {
            string queuedId = queuedKey.Substring("area:".Length);
            vars.pendingKey = queuedKey;
            vars.pendingName = vars.areaNames.ContainsKey(queuedId) ? vars.areaNames[queuedId] : queuedId;
            vars.pendingType = "area";
            vars.pendingHasFirstMissing = false;
            vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
            vars.lastUndoneKey = "";
            return true;
        }
    }

    // Poll BossWatcher first. Only advance through the line actually inspected so queued dual-boss
    // GONE events remain available for consecutive LiveSplit updates.
    var nowUtc = System.DateTime.UtcNow;
    if (nowUtc >= vars.nextBossPollUtc)
    {
        vars.nextBossPollUtc = nowUtc.AddMilliseconds(100);
        try
        {
            string[] lines = System.IO.File.ReadAllLines(vars.eventPath);
            if (lines.Length < vars.processedLineCount)
            {
                vars.processedLineCount = lines.Length;
            }
            else
            {
                for (int j = vars.processedLineCount; j < lines.Length; j++)
                {
                    string line = lines[j];
                    vars.processedLineCount = j + 1;
                    if (line == null || line.Trim() == "" || line.StartsWith("#")) continue;
                    string[] parts = line.Split('|');
                    if (parts.Length < 4 || !System.String.Equals(parts[1].Trim(), "GONE", System.StringComparison.OrdinalIgnoreCase)) continue;
                    string bossId = parts[2].Trim();
                    string bossName = parts[3].Trim();
                    string key = "boss:" + bossId;
                    if (!vars.objectiveSet.Contains(key))
                    {
                        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " IGNORE_BOSS_NOT_OBJECTIVE | " + key + " " + bossName + System.Environment.NewLine);
                        continue;
                    }
                    if (vars.completed.Contains(key) || vars.suppressed.Contains(key)) continue;
                    if (vars.ordered && (timer.CurrentSplitIndex < 0 || timer.CurrentSplitIndex >= vars.objectiveOrder.Count || !System.String.Equals(vars.objectiveOrder[timer.CurrentSplitIndex], key, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " IGNORE_OUT_OF_ORDER | got=" + key + " | expected=" + (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < vars.objectiveOrder.Count ? vars.objectiveOrder[timer.CurrentSplitIndex] : "<none>") + System.Environment.NewLine);
                        continue;
                    }
                    if (timer.CurrentPhase != LiveSplit.Model.TimerPhase.Running && timer.CurrentPhase != LiveSplit.Model.TimerPhase.Paused) continue;

                    bool hasFirstMissing = false;
                    System.DateTimeOffset firstMissing = System.DateTimeOffset.MinValue;
                    for (int k = 4; k < parts.Length; k++)
                    {
                        string extra = parts[k].Trim();
                        if (!extra.StartsWith("firstMissing=", System.StringComparison.OrdinalIgnoreCase)) continue;
                        System.DateTimeOffset parsed;
                        if (System.DateTimeOffset.TryParse(extra.Substring("firstMissing=".Length), out parsed))
                        { firstMissing = parsed; hasFirstMissing = true; break; }
                    }
                    vars.pendingKey = key;
                    vars.pendingName = bossName == "" && vars.bossNames.ContainsKey(bossId) ? vars.bossNames[bossId] : bossName;
                    vars.pendingType = "boss";
                    vars.pendingHasFirstMissing = hasFirstMissing;
                    vars.pendingFirstMissing = firstMissing;
                    vars.lastUndoneKey = "";
                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " BOSS_EVENT_POLL_ERROR | " + ex.Message + System.Environment.NewLine);
        }
    }

    if (vars.pendingKey == "")
    {
        while (vars.reader.Peek() >= 0)
        {
            string line = vars.reader.ReadLine();

            if (vars.riverbankStartArmed
                && !vars.startTrigger
                && System.String.Equals(vars.startAreaId, "G1_1", System.StringComparison.OrdinalIgnoreCase)
                && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning
                && line.IndexOf("Wounded Man: Reach... Clearfell... Find the Miller...", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                vars.startTrigger = true;
                vars.riverbankStartArmed = false;
                if (settings["debugLog"])
                    System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " START_TRIGGER WOUNDED_MAN_FINAL_LINE | G1_1 The Riverbank" + System.Environment.NewLine);
                break;
            }

            string areaId = "";
            string source = "";
            var am = vars.areaRegex.Match(line);
            if (am.Success) { areaId = am.Groups[3].Value; source = "GeneratingLevel"; }
            else if (settings["enteredNameFallback"])
            {
                var em = vars.enteredNameRegex.Match(line);
                if (em.Success)
                {
                    string displayName = em.Groups[2].Value.Trim();
                    if (vars.nameToAreaId.ContainsKey(displayName)) { areaId = vars.nameToAreaId[displayName]; source = "EnteredName"; }
                }
            }
            if (areaId == "" || !vars.areaNames.ContainsKey(areaId)) continue;
            if (System.String.Equals(areaId, vars.lastAreaId, System.StringComparison.OrdinalIgnoreCase)) continue;
            vars.lastAreaId = areaId;
            string areaName = vars.areaNames[areaId];
            string enteredKey = "area:" + areaId;

            if (vars.areaSuccessorCompletion && vars.objectiveSet.Contains(enteredKey) && !vars.completed.Contains(enteredKey))
                vars.armedAreaObjectives.Add(enteredKey);

            if (vars.startAreaId != "" && System.String.Equals(areaId, vars.startAreaId, System.StringComparison.OrdinalIgnoreCase) && timer.CurrentPhase == LiveSplit.Model.TimerPhase.NotRunning)
            {
                if (System.String.Equals(areaId, "G1_1", System.StringComparison.OrdinalIgnoreCase))
                {
                    vars.riverbankStartArmed = true;
                    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " START_ARMED G1_1 The Riverbank | source=" + source + " | waiting=Wounded Man final line" + System.Environment.NewLine);
                    continue;
                }

                vars.startTrigger = true;
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " START_TRIGGER | area=" + areaId + " " + areaName + " | armed=" + (vars.areaSuccessorCompletion ? "true" : "n/a") + " | source=" + source + System.Environment.NewLine);
                break;
            }

            if (vars.areaSuccessorCompletion)
            {
                // Complete only an armed area whose configured successor is the area
                // just entered. Returning to town, inventory detours, and revisits do
                // not complete anything unless they are the explicit successor.
                foreach (string candidateKey in vars.areaSequence)
                {
                    if (!vars.armedAreaObjectives.Contains(candidateKey) || vars.completed.Contains(candidateKey) || vars.suppressed.Contains(candidateKey)) continue;
                    if (!vars.areaSuccessor.ContainsKey(candidateKey) || !System.String.Equals(vars.areaSuccessor[candidateKey], areaId, System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (!vars.areaCompletionQueue.Contains(candidateKey)) vars.areaCompletionQueue.Add(candidateKey);
                }

                if (vars.areaCompletionQueue.Count > 0
                    && (timer.CurrentPhase == LiveSplit.Model.TimerPhase.Running || timer.CurrentPhase == LiveSplit.Model.TimerPhase.Paused))
                {
                    string key = vars.areaCompletionQueue[0];
                    vars.areaCompletionQueue.RemoveAt(0);
                    string completedId = key.Substring("area:".Length);
                    vars.pendingKey = key;
                    vars.pendingName = vars.areaNames.ContainsKey(completedId) ? vars.areaNames[completedId] : completedId;
                    vars.pendingType = "area";
                    vars.pendingHasFirstMissing = false;
                    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
                    vars.lastUndoneKey = "";
                    break;
                }
                continue;
            }

            string key = enteredKey;
            if (!vars.objectiveSet.Contains(key) || vars.completed.Contains(key) || vars.suppressed.Contains(key)) continue;
            if (vars.ordered && (timer.CurrentSplitIndex < 0 || timer.CurrentSplitIndex >= vars.objectiveOrder.Count || !System.String.Equals(vars.objectiveOrder[timer.CurrentSplitIndex], key, System.StringComparison.OrdinalIgnoreCase)))
            {
                if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " IGNORE_OUT_OF_ORDER | got=" + key + " | expected=" + (timer.CurrentSplitIndex >= 0 && timer.CurrentSplitIndex < vars.objectiveOrder.Count ? vars.objectiveOrder[timer.CurrentSplitIndex] : "<none>") + System.Environment.NewLine);
                continue;
            }
            if (timer.CurrentPhase != LiveSplit.Model.TimerPhase.Running && timer.CurrentPhase != LiveSplit.Model.TimerPhase.Paused) continue;
            vars.pendingKey = key;
            vars.pendingName = areaName;
            vars.pendingType = "area";
            vars.pendingHasFirstMissing = false;
            vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
            vars.lastUndoneKey = "";
            break;
        }
    }

    if (vars.pendingKey != "" && settings["dynamicSegmentNames"])
    {
        try
        {
            int i = 0;
            foreach (LiveSplit.Model.ISegment segment in timer.Run)
            {
                if (i == timer.CurrentSplitIndex)
                { segment.Name = vars.pendingName; timer.Run.HasChanged = true; timer.CallRunManuallyModified(); break; }
                i++;
            }
        } catch {}
    }
    return true;
}

start
{
    if (settings["autoStart"] && vars.startTrigger) return true;
}

split
{
    return vars.pendingKey != "";
}

onSplit
{
    if (vars.pendingKey != "")
    {
        string key = vars.pendingKey;
        string name = vars.pendingName;
        string type = vars.pendingType;
        int completedIndex = timer.CurrentSplitIndex - 1;
        string finalReal = "<not found>";

        // BossWatcher confirms disappearance after a short delay. Backdate boss objective Real Time
        // and Game Time to firstMissing, preserving the tested BossRush timing behavior. Area
        // objectives keep the native LiveSplit timestamp.
        if (type == "boss")
        {
            try
            {
                int i = 0;
                foreach (LiveSplit.Model.ISegment completedSegment in timer.Run)
                {
                    if (i == completedIndex)
                    {
                        if (!completedSegment.SplitTime.RealTime.HasValue || completedSegment.SplitTime.RealTime.Value <= System.TimeSpan.Zero)
                        {
                            completedSegment.SplitTime = new LiveSplit.Model.Time(timer.CurrentTime.RealTime, timer.CurrentTime.GameTime);
                            timer.Run.HasChanged = true;
                        }
                        if (vars.pendingHasFirstMissing && completedSegment.SplitTime.RealTime.HasValue)
                        {
                            if (vars.sameTimeCacheValid && vars.pendingFirstMissing == vars.sameTimeCacheFirstMissing)
                            {
                                System.TimeSpan? reusedGame = completedSegment.SplitTime.GameTime;
                                if (vars.sameTimeCacheHasGame)
                                    reusedGame = vars.sameTimeCacheGame;
                                completedSegment.SplitTime = new LiveSplit.Model.Time(vars.sameTimeCacheReal, reusedGame);
                                timer.Run.HasChanged = true;
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

                                    completedSegment.SplitTime = new LiveSplit.Model.Time(adjustedReal, adjustedGame);
                                    timer.Run.HasChanged = true;
                                    vars.sameTimeCacheValid = true;
                                    vars.sameTimeCacheFirstMissing = vars.pendingFirstMissing;
                                    vars.sameTimeCacheReal = adjustedReal;
                                    vars.sameTimeCacheHasGame = adjustedGame.HasValue;
                                    vars.sameTimeCacheGame = adjustedGame.HasValue ? adjustedGame.Value : System.TimeSpan.Zero;
                                }
                            }
                        }
                        finalReal = completedSegment.SplitTime.RealTime.HasValue ? completedSegment.SplitTime.RealTime.Value.ToString() : "null";
                        break;
                    }
                    i++;
                }
                timer.CallRunManuallyModified();
            } catch {}
        }

        vars.completed.Add(key);
        vars.completedOrder.Add(key);
        vars.pendingKey = "";
        vars.pendingName = "";
        vars.pendingType = "";
        vars.pendingHasFirstMissing = false;
        vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
        vars.lastObservedIndex = timer.CurrentSplitIndex;

        string status = "Status: RUNNING" + System.Environment.NewLine
            + "Mode: mixed " + (vars.ordered ? "ordered" : "unordered") + System.Environment.NewLine
            + "Last objective: " + key + " | " + name + System.Environment.NewLine
            + "Completed: " + vars.completed.Count + " / " + vars.objectiveOrder.Count + System.Environment.NewLine
            + "Suppressed: " + vars.suppressed.Count + System.Environment.NewLine
            + "LiveSplit index: " + timer.CurrentSplitIndex + System.Environment.NewLine
            + (type == "boss" ? "Boss split RealTime: " + finalReal + System.Environment.NewLine : "");
        System.IO.File.WriteAllText(vars.statusPath, status);
        if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath,
            System.DateTime.Now.ToString("s") + " OBJECTIVE_SPLIT_COMMITTED | " + key + " " + name
            + " | completed=" + vars.completed.Count + "/" + vars.objectiveOrder.Count
            + " | liveSplitIndex=" + timer.CurrentSplitIndex + System.Environment.NewLine);
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

    vars.completed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.completedOrder = new System.Collections.Generic.List<string>();
    vars.armedAreaObjectives = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.areaCompletionQueue = new System.Collections.Generic.List<string>();
    vars.suppressed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    vars.pendingKey = "";
    vars.pendingName = "";
    vars.pendingType = "";
    vars.pendingHasFirstMissing = false;
    vars.pendingFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheValid = false;
    vars.sameTimeCacheFirstMissing = System.DateTimeOffset.MinValue;
    vars.sameTimeCacheReal = System.TimeSpan.Zero;
    vars.sameTimeCacheGame = System.TimeSpan.Zero;
    vars.sameTimeCacheHasGame = false;
    vars.startTrigger = false;
    vars.riverbankStartArmed = false;
    vars.lastAreaId = "";
    vars.lastUndoneKey = "";
    vars.lastObservedIndex = timer.CurrentSplitIndex;
    try { vars.processedLineCount = System.IO.File.Exists(vars.eventPath) ? System.IO.File.ReadAllLines(vars.eventPath).Length : 0; } catch {}

    if (settings["dynamicSegmentNames"])
    {
        try
        {
            int i = 0;
            foreach (LiveSplit.Model.ISegment segment in timer.Run)
            { if (i < vars.baseSegmentNames.Count) segment.Name = vars.baseSegmentNames[i]; i++; }
            timer.Run.HasChanged = true;
            timer.CallRunManuallyModified();
        } catch {}
    }
    if (settings["debugLog"]) System.IO.File.AppendAllText(vars.debugPath, System.DateTime.Now.ToString("s") + " RESET | eventLineBaseline=" + vars.processedLineCount + System.Environment.NewLine);
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
