using System.Diagnostics;
using System.Text;

namespace PoE2RouteSetup;

public sealed class SetupForm : Form
{
    private readonly string _packageRoot;
    private readonly string _userRoot;
    private readonly SetupManifest _manifest;
    private readonly List<RouteEntry> _areas;
    private readonly List<RouteEntry> _bosses;
    private readonly List<RouteEntry> _customRoute = [];

    private readonly TextBox _targetText = new();
    private readonly TabControl _modeTabs = new();

    // Compact premade-route selector. Premade routes are generated from the
    // existing validated route/boss catalogs instead of exposing the old 41-row list.
    private readonly ComboBox _premadeModeCombo = new();
    private readonly ComboBox _premadeSetupCombo = new();
    private readonly RadioButton _premadeOrderedRadio = new();
    private readonly RadioButton _premadeDynamicRadio = new();
    private readonly Label _presetDescription = new();
    private readonly FlowLayoutPanel _premadeCombinationPanel = new();
    private readonly CheckedListBox _premadeCombinationList = new();

    private readonly CheckBox _premadeSekhemasCheck = new();
    private readonly FlowLayoutPanel _premadeSekhemasPanel = new();
    private readonly CheckBox _premadeSekhemasFloor1 = new();
    private readonly CheckBox _premadeSekhemasFloor2 = new();
    private readonly CheckBox _premadeSekhemasFloor3 = new();
    private readonly CheckBox _premadeSekhemasFloor4 = new();
    // Ordered premades schedule each selected trial segment independently. The first
    // combo retains the original field name for compatibility with earlier UI code.
    private readonly ComboBox _premadeSekhemasInsertCombo = new();
    private readonly ComboBox _premadeSekhemasFloor2InsertCombo = new();
    private readonly ComboBox _premadeSekhemasFloor3InsertCombo = new();
    private readonly ComboBox _premadeSekhemasFloor4InsertCombo = new();

    private readonly CheckBox _premadeChaosCheck = new();
    private readonly FlowLayoutPanel _premadeChaosPanel = new();
    private readonly CheckBox _premadeChaosBoss1 = new();
    private readonly CheckBox _premadeChaosBoss2 = new();
    private readonly CheckBox _premadeChaosBoss3 = new();
    private readonly CheckBox _premadeTrialmaster = new();
    private readonly ComboBox _premadeChaosInsertCombo = new();
    private readonly ComboBox _premadeChaosStage2InsertCombo = new();
    private readonly ComboBox _premadeChaosStage3InsertCombo = new();
    private bool _syncingPremadeTrialDepth;
    private bool _updatingPremadeUi;
    private readonly ComboBox _customCatalogGroupCombo = new();
    private readonly TextBox _areaSearch = new();
    private readonly TextBox _bossSearch = new();
    private readonly ListBox _areaList = new();
    private readonly DataGridView _bossGrid = new();
    private readonly CheckBox _multiBossCheck = new();
    private readonly FlowLayoutPanel _orderedBossOptionsPanel = new();
    private readonly FlowLayoutPanel _unorderedBossOptionsPanel = new();
    private readonly NumericUpDown _unorderedBossTargetNumeric = new();
    private readonly Label _unorderedBossTargetNote = new();
    private readonly ListBox _routeList = new();
    private readonly Label _routePolicySummary = new();
    private readonly CheckBox _orderedCheck = new();
    private readonly CheckBox _levelProgressionCheck = new();
    private readonly NumericUpDown _maxLevelNumeric = new();
    private readonly NumericUpDown _levelIntervalNumeric = new();
    private readonly FlowLayoutPanel _levelOptionsPanel = new();
    private readonly CheckBox _trialBossObjectivesCheck = new();
    private readonly CheckedListBox _trialBossChecklist = new();
    private readonly FlowLayoutPanel _trialBossOptionsPanel = new();
    private readonly List<RouteEntry> _trialBossRouteEntries;
    private bool _syncingTrialBossChecklist;

    // Dedicated trial-run configuration.
    private readonly RadioButton _trialSekhemasRadio = new();
    private readonly RadioButton _trialChaosRadio = new();
    private readonly ComboBox _sekhemasLengthCombo = new();
    private readonly ComboBox _chaosLengthCombo = new();
    private readonly CheckBox _trialmasterCheck = new();
    private readonly RadioButton _trialFullTimeRadio = new();
    private readonly RadioButton _trialActiveOnlyRadio = new();
    private readonly Label _trialFixedStartLabel = new();
    private readonly RadioButton _trialFinalBossRadio = new();
    private readonly RadioButton _trialExitRadio = new();
    private readonly RadioButton _trialFinalOnlyRadio = new();
    private readonly RadioButton _trialMajorBossRadio = new();
    private readonly RadioButton _trialEveryChallengeRadio = new();
    private readonly Label _trialDescription = new();
    private readonly Label _trialLengthDescription = new();
    private readonly Label _trialTimingDescription = new();
    private readonly Label _trialStartDescription = new();
    private readonly Label _trialFinishDescription = new();
    private readonly Label _trialSplitsDescription = new();
    private readonly Label _trialPreviewTrialValue = new();
    private readonly Label _trialPreviewLengthValue = new();
    private readonly Label _trialPreviewTimingValue = new();
    private readonly Label _trialPreviewStartValue = new();
    private readonly Label _trialPreviewFinishValue = new();
    private readonly Label _trialPreviewSplitsValue = new();

    private readonly RadioButton _manualStartRadio = new();
    private readonly RadioButton _riverbankStartRadio = new();
    private readonly RadioButton _zoneStartRadio = new();
    private readonly ComboBox _startZoneCombo = new();
    private readonly CheckBox _excludeManualPauseCheck = new();
    private readonly CheckBox _devConsoleCheck = new();
    private readonly Button _gameTimeWatcherButton = new();
    private readonly Button _deployButton = new();
    private readonly Label _status = new();

    public SetupForm()
    {
        var located = PackageData.LocatePackage();
        _packageRoot = located.PackageRoot;
        _userRoot = located.UserRoot;
        _manifest = SetupManifest.Load(located.ManifestPath);
        _areas = PackageData.LoadAreas(Resolve(_manifest.AreaCatalog));
        _bosses = PackageData.LoadBosses(Resolve(_manifest.BossCatalog), Resolve(_manifest.BossSupportOnlyList));
        _trialBossRouteEntries = BuildTrialBossRouteEntries();

        Text = $"PoE2 Route AutoSplitter Setup — v{_manifest.Version}";
        Width = 1120;
        Height = 860;
        MinimumSize = new Size(920, 720);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        PopulatePresets();
        PopulateCustomCatalogs();
        PopulateStartZones();
        _riverbankStartRadio.Checked = true;
        UpdateStartZoneEnabled();
        _targetText.Text = Path.Combine(_userRoot, "LiveSplit Target");
        _targetText.ReadOnly = true;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(BuildTargetPanel(), 0, 0);
        root.Controls.Add(BuildLiveSplitReminderPanel(), 0, 1);
        root.Controls.Add(BuildModeTabs(), 0, 2);
        root.Controls.Add(BuildActionPanel(), 0, 3);
        _status.AutoSize = true;
        _status.Padding = new Padding(4, 8, 4, 0);
        _status.Text = "Choose a premade setup or build a custom route, then deploy it to the target directory.";
        root.Controls.Add(_status, 0, 4);
    }

    private Control BuildTargetPanel()
    {
        var group = new GroupBox { Text = "LiveSplit Target", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _targetText.Dock = DockStyle.Fill;
        var open = new Button { Text = "Open Folder", AutoSize = true };
        open.Click += (_, _) => OpenTargetFolder();
        panel.Controls.Add(_targetText, 0, 0);
        panel.Controls.Add(open, 1, 0);
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildLiveSplitReminderPanel()
    {
        var box = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 8, 0, 4),
            MaximumSize = new Size(1060, 0),
            Text =
                "LiveSplit reminders:\r\n" +
                "• After Generate, open the generated .lss and attach the generated .asl to a Scriptable Auto Splitter component. LiveSplit does not attach the .asl automatically.\r\n" +
                "• To exclude loading screens and, when enabled, manual-pause time from the displayed run time, set LiveSplit to Game Time. Real Time will continue counting those periods."
        };
        return box;
    }

    private Control BuildModeTabs()
    {
        _modeTabs.Dock = DockStyle.Fill;
        _modeTabs.SelectedIndexChanged += (_, _) => UpdateStartZoneEnabled();
        var premade = new TabPage("Premade setups");
        premade.Controls.Add(BuildPremadePanel());
        var custom = new TabPage("Custom route");
        custom.Controls.Add(BuildCustomPanel());
        var trials = new TabPage("Trials");
        trials.Controls.Add(BuildTrialsPanel());
        _modeTabs.TabPages.Add(premade);
        _modeTabs.TabPages.Add(custom);
        _modeTabs.TabPages.Add(trials);
        return _modeTabs;
    }

    private Control BuildPremadePanel()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var root = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8),
            Width = 1020
        };
        scroll.Controls.Add(root);

        // Keep the compact selector at a stable width/height inside the vertically
        // auto-sized FlowLayoutPanel. An AutoSize GroupBox containing a docked
        // TableLayoutPanel can report an almost-zero preferred width, which caused
        // the selector to collapse into a narrow column on Windows.
        var selector = new GroupBox
        {
            Text = "Premade route",
            AutoSize = false,
            Width = 990,
            Height = 142,
            Padding = new Padding(10)
        };
        var grid = new TableLayoutPanel
        {
            AutoSize = false,
            ColumnCount = 2,
            RowCount = 3,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

        grid.Controls.Add(new Label { Text = "Mode:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        _premadeModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _premadeModeCombo.Width = 360;
        _premadeModeCombo.Items.AddRange(new object[]
        {
            "Area Completion",
            "Boss Completion",
            "Area + Boss Completion",
            "Level Race"
        });
        _premadeModeCombo.SelectedIndexChanged += (_, _) => UpdatePremadeSelectorUi(true);
        grid.Controls.Add(_premadeModeCombo, 1, 0);

        grid.Controls.Add(new Label { Text = "Setup:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
        _premadeSetupCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _premadeSetupCombo.Width = 500;
        _premadeSetupCombo.SelectedIndexChanged += (_, _) => UpdatePremadeSelectorUi(false);
        grid.Controls.Add(_premadeSetupCombo, 1, 1);

        grid.Controls.Add(new Label { Text = "Route order:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, 2);
        var orderPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _premadeOrderedRadio.Text = "Ordered";
        _premadeOrderedRadio.AutoSize = true;
        _premadeOrderedRadio.Checked = true;
        _premadeOrderedRadio.CheckedChanged += (_, _) => { if (_premadeOrderedRadio.Checked) UpdatePremadeSelectorUi(true); };
        _premadeDynamicRadio.Text = "Dynamic / unordered";
        _premadeDynamicRadio.AutoSize = true;
        _premadeDynamicRadio.CheckedChanged += (_, _) => { if (_premadeDynamicRadio.Checked) UpdatePremadeSelectorUi(true); };
        orderPanel.Controls.Add(_premadeOrderedRadio);
        orderPanel.Controls.Add(_premadeDynamicRadio);
        grid.Controls.Add(orderPanel, 1, 2);
        selector.Controls.Add(grid);
        root.Controls.Add(selector);

        _premadeCombinationPanel.AutoSize = true;
        _premadeCombinationPanel.FlowDirection = FlowDirection.TopDown;
        _premadeCombinationPanel.WrapContents = false;
        _premadeCombinationPanel.Visible = false;
        var combinationGroup = new GroupBox { Text = "Act / Interlude combination", AutoSize = true, Width = 990, Padding = new Padding(10) };
        _premadeCombinationList.CheckOnClick = true;
        _premadeCombinationList.Width = 470;
        _premadeCombinationList.Height = 112;
        _premadeCombinationList.IntegralHeight = false;
        _premadeCombinationList.Items.AddRange(new object[] { "Act 1", "Act 2", "Act 3", "Act 4", "Interlude 1", "Interlude 2", "Interlude 3" });
        _premadeCombinationList.ItemCheck += (_, _) => BeginInvoke(new Action(() => UpdatePremadeSelectorUi(false)));
        _premadeCombinationPanel.Controls.Add(_premadeCombinationList);
        _premadeCombinationPanel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(930, 0),
            Text = "Combination setups concatenate the selected Acts / Interludes in campaign order. The 100% or Any% rule in the Setup selection is applied to each selected Act; Interludes use their available full route."
        });
        combinationGroup.Controls.Add(_premadeCombinationPanel);
        root.Controls.Add(combinationGroup);

        root.Controls.Add(BuildPremadeTrialsPanel());

        _presetDescription.AutoSize = true;
        _presetDescription.MaximumSize = new Size(960, 0);
        _presetDescription.Padding = new Padding(4, 8, 4, 8);
        root.Controls.Add(_presetDescription);

        _premadeModeCombo.SelectedIndex = 0;
        return scroll;
    }

    private Control BuildPremadeTrialsPanel()
    {
        var group = new GroupBox { Text = "Optional trial content", AutoSize = true, Width = 990, Padding = new Padding(10) };
        var root = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 950 };

        _premadeSekhemasCheck.Text = "Include Trial of the Sekhemas";
        _premadeSekhemasCheck.AutoSize = true;
        _premadeSekhemasCheck.CheckedChanged += (_, _) =>
        {
            _premadeSekhemasPanel.Visible = _premadeSekhemasCheck.Checked && _premadeSekhemasCheck.Enabled;
            if (_premadeSekhemasCheck.Checked && !_syncingPremadeTrialDepth)
            {
                // Normal campaign progression grants the first Sekhemas attempt in Act 2.
                // Later floors are explicit opt-ins because players often defer them.
                _syncingPremadeTrialDepth = true;
                _premadeSekhemasFloor1.Checked = true;
                _premadeSekhemasFloor2.Checked = false;
                _premadeSekhemasFloor3.Checked = false;
                _premadeSekhemasFloor4.Checked = false;
                _syncingPremadeTrialDepth = false;
            }
            UpdatePremadeSelectorUi(false);
        };
        root.Controls.Add(_premadeSekhemasCheck);

        _premadeSekhemasPanel.AutoSize = true;
        _premadeSekhemasPanel.FlowDirection = FlowDirection.TopDown;
        _premadeSekhemasPanel.WrapContents = false;
        _premadeSekhemasPanel.Padding = new Padding(24, 0, 0, 8);
        _premadeSekhemasPanel.Visible = false;

        ConfigurePremadeDepthCheck(_premadeSekhemasFloor1, "Floor 1", 1, true);
        ConfigurePremadeDepthCheck(_premadeSekhemasFloor2, "Floor 2", 2, true);
        ConfigurePremadeDepthCheck(_premadeSekhemasFloor3, "Floor 3", 3, true);
        ConfigurePremadeDepthCheck(_premadeSekhemasFloor4, "Floor 4", 4, true);
        _premadeSekhemasPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeSekhemasFloor1, _premadeSekhemasInsertCombo));
        _premadeSekhemasPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeSekhemasFloor2, _premadeSekhemasFloor2InsertCombo));
        _premadeSekhemasPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeSekhemasFloor3, _premadeSekhemasFloor3InsertCombo));
        _premadeSekhemasPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeSekhemasFloor4, _premadeSekhemasFloor4InsertCombo));
        _premadeSekhemasPanel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(880, 0),
            Text =
                "Choose where you expect to begin each selected floor. Floors are sequential, so selecting a later floor also includes the earlier floors. " +
                "If Floors 2 and 3 will be run back-to-back, choose the same preceding objective for both; SetupUI keeps Floor 2 before Floor 3. " +
                "Typical progression is Floor 1 during Act 2, Floors 2–3 later (often during the Interludes), and Floor 4 after the campaign, but these are planning hints rather than enforced rules."
        });
        root.Controls.Add(_premadeSekhemasPanel);

        _premadeChaosCheck.Text = "Include Trial of Chaos";
        _premadeChaosCheck.AutoSize = true;
        _premadeChaosCheck.CheckedChanged += (_, _) =>
        {
            _premadeChaosPanel.Visible = _premadeChaosCheck.Checked && _premadeChaosCheck.Enabled;
            if (_premadeChaosCheck.Checked && !_syncingPremadeTrialDepth)
            {
                _syncingPremadeTrialDepth = true;
                _premadeChaosBoss1.Checked = true;
                _premadeChaosBoss2.Checked = false;
                _premadeChaosBoss3.Checked = false;
                _premadeTrialmaster.Checked = false;
                _syncingPremadeTrialDepth = false;
            }
            UpdatePremadeSelectorUi(false);
        };
        root.Controls.Add(_premadeChaosCheck);

        _premadeChaosPanel.AutoSize = true;
        _premadeChaosPanel.FlowDirection = FlowDirection.TopDown;
        _premadeChaosPanel.WrapContents = false;
        _premadeChaosPanel.Padding = new Padding(24, 0, 0, 8);
        _premadeChaosPanel.Visible = false;

        ConfigurePremadeDepthCheck(_premadeChaosBoss1, "4-round trial", 1, false);
        ConfigurePremadeDepthCheck(_premadeChaosBoss2, "7-round trial", 2, false);
        ConfigurePremadeDepthCheck(_premadeChaosBoss3, "10-round trial", 3, false);
        _premadeTrialmaster.Text = "Include Trialmaster after the 10-round trial";
        _premadeTrialmaster.AutoSize = true;
        _premadeTrialmaster.CheckedChanged += (_, _) => PremadeTrialDepthChanged(false, 4);

        _premadeChaosPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeChaosBoss1, _premadeChaosInsertCombo));
        _premadeChaosPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeChaosBoss2, _premadeChaosStage2InsertCombo));
        _premadeChaosPanel.Controls.Add(BuildPremadeStageScheduleRow(_premadeChaosBoss3, _premadeChaosStage3InsertCombo));
        _premadeChaosPanel.Controls.Add(_premadeTrialmaster);
        _premadeChaosPanel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(880, 0),
            Text =
                "Choose where you expect to return for each selected Chaos stage. The 4-round trial is commonly done during Act 3, the 7-round trial is often deferred until the Interludes, and the 10-round trial / Trialmaster is commonly post-campaign. " +
                "Boss routes use a dynamic Uxmal / Chetza / Bahlak encounter slot for each selected stage. Area routes track each separate visit to the active Trial of Chaos even though the game reuses the same trial area."
        });
        root.Controls.Add(_premadeChaosPanel);

        group.Controls.Add(root);
        return group;
    }

    private static Control BuildPremadeStageScheduleRow(CheckBox stageCheck, ComboBox combo)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 1, 0, 1)
        };
        stageCheck.AutoSize = true;
        stageCheck.Width = 145;
        row.Controls.Add(stageCheck);

        var schedule = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(8, 0, 0, 0)
        };
        schedule.Controls.Add(new Label { Text = "Run after:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) });
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Width = 430;
        schedule.Controls.Add(combo);
        row.Controls.Add(schedule);
        return row;
    }

    private void ConfigurePremadeDepthCheck(CheckBox box, string text, int depth, bool sekhemas)
    {
        box.Text = text;
        box.AutoSize = true;
        box.CheckedChanged += (_, _) => PremadeTrialDepthChanged(sekhemas, depth);
    }

    private void PremadeTrialDepthChanged(bool sekhemas, int depth)
    {
        if (_syncingPremadeTrialDepth) return;
        _syncingPremadeTrialDepth = true;
        try
        {
            var boxes = sekhemas
                ? new[] { _premadeSekhemasFloor1, _premadeSekhemasFloor2, _premadeSekhemasFloor3, _premadeSekhemasFloor4 }
                : new[] { _premadeChaosBoss1, _premadeChaosBoss2, _premadeChaosBoss3, _premadeTrialmaster };
            var changed = boxes[Math.Clamp(depth - 1, 0, boxes.Length - 1)];
            if (changed.Checked)
            {
                for (var i = 0; i < depth; i++) boxes[i].Checked = true;
            }
            else
            {
                for (var i = depth - 1; i < boxes.Length; i++) boxes[i].Checked = false;
            }
        }
        finally { _syncingPremadeTrialDepth = false; }
        UpdatePremadeSelectorUi(false);
    }

    private Control BuildCustomPanel()
    {
        // SplitContainer validates panel minimum sizes and SplitterDistance against its
        // current Size. During form construction a newly-created control still has its
        // small framework default size, so assigning the 350/330 minimums immediately
        // can throw before the main window is ever shown. Give it a valid design-time
        // working size first, then apply the constraints.
        var outer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Size = new Size(1000, 560)
        };
        outer.Panel1MinSize = 350;
        outer.Panel2MinSize = 330;
        outer.SplitterDistance = 515;
        outer.Panel1.Controls.Add(BuildAvailableObjectivesPanel());
        outer.Panel2.Controls.Add(BuildRoutePanel());
        return outer;
    }

    private Control BuildAvailableObjectivesPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(BuildCustomCatalogSelectorPanel(), 0, 0);
        panel.Controls.Add(BuildLevelProgressionPanel(), 0, 1);
        panel.Controls.Add(BuildTrialBossObjectivesPanel(), 0, 2);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 4) };
        tabs.TabPages.Add(BuildAreaObjectiveTab());
        tabs.TabPages.Add(BuildBossObjectiveTab());
        panel.Controls.Add(tabs, 0, 3);
        return panel;
    }

    private Control BuildCustomCatalogSelectorPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 6, 8, 4)
        };
        var selectorRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        selectorRow.Controls.Add(new Label { Text = "Content:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        _customCatalogGroupCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _customCatalogGroupCombo.Width = 190;
        _customCatalogGroupCombo.Items.AddRange(new object[]
        {
            "Act 1", "Act 2", "Act 3", "Act 4",
            "Interlude 1", "Interlude 2", "Interlude 3",
            "Pinnacle"
        });
        _customCatalogGroupCombo.SelectedIndexChanged += (_, _) => RefreshCustomCatalogs();
        selectorRow.Controls.Add(_customCatalogGroupCombo);
        panel.Controls.Add(selectorRow);
        panel.Controls.Add(new Label
        {
            Text = "Only areas/bosses from the selected Act, Interlude, or Pinnacle group are shown. Trial content remains in its separate selector above.",
            AutoSize = true,
            MaximumSize = new Size(450, 0),
            Padding = new Padding(0, 3, 0, 0)
        });
        return panel;
    }

    private TabPage BuildAreaObjectiveTab()
    {
        var tab = new TabPage("Areas");
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _areaSearch.Dock = DockStyle.Top;
        _areaSearch.PlaceholderText = "Search areas by name…";
        _areaSearch.TextChanged += (_, _) => RefreshCustomAreaList();
        panel.Controls.Add(_areaSearch, 0, 0);

        _areaList.Dock = DockStyle.Fill;
        _areaList.SelectionMode = SelectionMode.MultiExtended;
        _areaList.DisplayMember = nameof(RouteEntry.Name);
        _areaList.DoubleClick += (_, _) => AddSelectedAreas();
        panel.Controls.Add(_areaList, 0, 1);

        var areaButtons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1 };
        areaButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        areaButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        var add = new Button { Text = "Add Selected Area(s)", Dock = DockStyle.Fill, Height = 34 };
        add.Click += (_, _) => AddSelectedAreas();
        var addAll = new Button { Text = "Add All", Dock = DockStyle.Fill, Height = 34 };
        addAll.Click += (_, _) => AddAllAreas();
        areaButtons.Controls.Add(add, 0, 0);
        areaButtons.Controls.Add(addAll, 1, 0);
        panel.Controls.Add(areaButtons, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage BuildBossObjectiveTab()
    {
        var tab = new TabPage("Bosses");
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var modeOptions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        _orderedBossOptionsPanel.AutoSize = true;
        _orderedBossOptionsPanel.FlowDirection = FlowDirection.TopDown;
        _orderedBossOptionsPanel.WrapContents = false;
        _multiBossCheck.Text = "Multi-boss / repeated encounters";
        _multiBossCheck.AutoSize = true;
        _multiBossCheck.CheckedChanged += (_, _) => UpdateCustomBossModeUi();
        _orderedBossOptionsPanel.Controls.Add(_multiBossCheck);
        _orderedBossOptionsPanel.Controls.Add(new Label
        {
            Text = "When enabled, set a positive occurrence count beside each selected boss. Each generated occurrence has its own internal objective key.",
            AutoSize = true,
            MaximumSize = new Size(450, 0),
            Padding = new Padding(0, 2, 0, 0)
        });
        modeOptions.Controls.Add(_orderedBossOptionsPanel);

        _unorderedBossOptionsPanel.AutoSize = true;
        _unorderedBossOptionsPanel.FlowDirection = FlowDirection.TopDown;
        _unorderedBossOptionsPanel.WrapContents = false;
        var unorderedTargetRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        unorderedTargetRow.Controls.Add(new Label { Text = "Boss encounters required:", AutoSize = true, Padding = new Padding(0, 5, 3, 0) });
        _unorderedBossTargetNumeric.Minimum = 1;
        _unorderedBossTargetNumeric.Maximum = 999;
        _unorderedBossTargetNumeric.Width = 72;
        _unorderedBossTargetNumeric.ValueChanged += (_, _) => RefreshRouteList();
        unorderedTargetRow.Controls.Add(_unorderedBossTargetNumeric);
        _unorderedBossOptionsPanel.Controls.Add(unorderedTargetRow);
        _unorderedBossTargetNote.AutoSize = true;
        _unorderedBossTargetNote.MaximumSize = new Size(450, 0);
        _unorderedBossTargetNote.Padding = new Padding(0, 2, 0, 0);
        _unorderedBossOptionsPanel.Controls.Add(_unorderedBossTargetNote);
        modeOptions.Controls.Add(_unorderedBossOptionsPanel);
        panel.Controls.Add(modeOptions, 0, 0);

        _bossSearch.Dock = DockStyle.Top;
        _bossSearch.PlaceholderText = "Search bosses by name…";
        _bossSearch.TextChanged += (_, _) => RefreshCustomBossGrid();
        panel.Controls.Add(_bossSearch, 0, 1);

        _bossGrid.Dock = DockStyle.Fill;
        _bossGrid.AllowUserToAddRows = false;
        _bossGrid.AllowUserToDeleteRows = false;
        _bossGrid.AllowUserToResizeRows = false;
        _bossGrid.AutoGenerateColumns = false;
        _bossGrid.MultiSelect = true;
        _bossGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _bossGrid.RowHeadersVisible = false;
        _bossGrid.BackgroundColor = SystemColors.Window;
        _bossGrid.BorderStyle = BorderStyle.Fixed3D;
        _bossGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _bossGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "BossName",
            HeaderText = "Boss",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 180
        });
        _bossGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Occurrences",
            HeaderText = "Occurrences",
            Width = 88,
            ValueType = typeof(int)
        });
        _bossGrid.CellValidating += (_, e) =>
        {
            var occurrenceColumn = _bossGrid.Columns["Occurrences"];
            if (occurrenceColumn is null || e.ColumnIndex != occurrenceColumn.Index || !_multiBossCheck.Checked || !_orderedCheck.Checked) return;
            if (!int.TryParse(e.FormattedValue?.ToString(), out var count) || count < 1)
            {
                e.Cancel = true;
                _bossGrid.Rows[e.RowIndex].ErrorText = "Occurrences must be a positive integer.";
            }
            else _bossGrid.Rows[e.RowIndex].ErrorText = "";
        };
        _bossGrid.CellEndEdit += (_, e) => _bossGrid.Rows[e.RowIndex].ErrorText = "";
        _bossGrid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AddSelectedBosses(); };
        panel.Controls.Add(_bossGrid, 0, 2);

        var bossButtons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1 };
        bossButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        bossButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        var add = new Button { Text = "Add Selected Boss(es)", Dock = DockStyle.Fill, Height = 34 };
        add.Click += (_, _) => AddSelectedBosses();
        var addAll = new Button { Text = "Add All", Dock = DockStyle.Fill, Height = 34 };
        addAll.Click += (_, _) => AddAllBosses();
        bossButtons.Controls.Add(add, 0, 0);
        bossButtons.Controls.Add(addAll, 1, 0);
        panel.Controls.Add(bossButtons, 0, 3);

        tab.Controls.Add(panel);
        return tab;
    }

    private Control BuildLevelProgressionPanel()
    {
        var group = new GroupBox
        {
            Text = "Level progression",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8, 5, 8, 7),
            Margin = new Padding(0, 0, 0, 6)
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        _levelProgressionCheck.Text = "Add level progression";
        _levelProgressionCheck.AutoSize = true;
        _levelProgressionCheck.CheckedChanged += (_, _) =>
        {
            _levelOptionsPanel.Visible = _levelProgressionCheck.Checked;
            SyncLevelProgressionObjectives();
        };
        body.Controls.Add(_levelProgressionCheck);

        _levelOptionsPanel.AutoSize = true;
        _levelOptionsPanel.FlowDirection = FlowDirection.LeftToRight;
        _levelOptionsPanel.WrapContents = false;
        _levelOptionsPanel.Visible = false;

        _maxLevelNumeric.Minimum = 2;
        _maxLevelNumeric.Maximum = 100;
        _maxLevelNumeric.Value = 100;
        _maxLevelNumeric.Width = 65;
        _maxLevelNumeric.ValueChanged += (_, _) => { if (_levelProgressionCheck.Checked) SyncLevelProgressionObjectives(); };

        _levelIntervalNumeric.Minimum = 1;
        _levelIntervalNumeric.Maximum = 100;
        _levelIntervalNumeric.Value = 10;
        _levelIntervalNumeric.Width = 65;
        _levelIntervalNumeric.ValueChanged += (_, _) => { if (_levelProgressionCheck.Checked) SyncLevelProgressionObjectives(); };

        _levelOptionsPanel.Controls.Add(new Label { Text = "Max level:", AutoSize = true, Padding = new Padding(0, 5, 2, 0) });
        _levelOptionsPanel.Controls.Add(_maxLevelNumeric);
        _levelOptionsPanel.Controls.Add(new Label { Text = "Split interval:", AutoSize = true, Padding = new Padding(12, 5, 2, 0) });
        _levelOptionsPanel.Controls.Add(_levelIntervalNumeric);
        body.Controls.Add(_levelOptionsPanel);

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(470, 0),
            Text = "Generated level milestones appear in the route preview and can be moved among area/boss objectives. The Max Level is always included as the final milestone. On ordered routes, a level already reached before it becomes active is automatically skipped (not completed). LiveSplit cannot skip its final row, so keep a later objective after any level milestone that could be reached early."
        };
        body.Controls.Add(note);

        group.Controls.Add(body);
        return group;
    }


    private Control BuildTrialBossObjectivesPanel()
    {
        var group = new GroupBox
        {
            Text = "Trial boss objectives",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8, 5, 8, 7),
            Margin = new Padding(0, 0, 0, 6)
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        _trialBossObjectivesCheck.Text = "Add trial boss objectives";
        _trialBossObjectivesCheck.AutoSize = true;
        _trialBossObjectivesCheck.CheckedChanged += (_, _) =>
        {
            _trialBossOptionsPanel.Visible = _trialBossObjectivesCheck.Checked;
            if (!_trialBossObjectivesCheck.Checked) ClearTrialBossObjectives();
        };
        body.Controls.Add(_trialBossObjectivesCheck);

        _trialBossOptionsPanel.AutoSize = true;
        _trialBossOptionsPanel.FlowDirection = FlowDirection.TopDown;
        _trialBossOptionsPanel.WrapContents = false;
        _trialBossOptionsPanel.Visible = false;

        _trialBossChecklist.CheckOnClick = true;
        _trialBossChecklist.DisplayMember = nameof(RouteEntry.Name);
        _trialBossChecklist.Width = 470;
        _trialBossChecklist.Height = 132;
        _trialBossChecklist.IntegralHeight = false;
        foreach (var entry in _trialBossRouteEntries) _trialBossChecklist.Items.Add(entry);
        _trialBossChecklist.ItemCheck += (_, e) =>
        {
            if (_syncingTrialBossChecklist) return;
            if (e.Index < 0 || e.Index >= _trialBossChecklist.Items.Count) return;
            if (_trialBossChecklist.Items[e.Index] is RouteEntry entry)
                SetTrialBossRouteEntry(entry, e.NewValue == CheckState.Checked);
        };
        _trialBossOptionsPanel.Controls.Add(_trialBossChecklist);

        _trialBossOptionsPanel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(470, 0),
            Text =
                "Trial bosses are optional and are kept out of the normal boss list. Checking a milestone adds it directly to the route preview, where ordered routes can move it to the desired position; unordered routes treat it as another completion objective. " +
                "Sekhemas Floor 2 is one composite objective and completes only after both Hadi and Rafiq are defeated, in either kill order. " +
                "Chaos Boss 1/2/3 are dynamic encounter slots backed by the restricted Uxmal/Chetza/Bahlak pool. Each slot accepts whichever of those bosses BossWatcher actually identifies and renames that LiveSplit split to the detected boss; Trialmaster remains a fixed final-boss objective."
        });
        body.Controls.Add(_trialBossOptionsPanel);

        group.Controls.Add(body);
        return group;
    }


    private Control BuildTrialsPanel()
    {
        var outer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Size = new Size(1000, 560),
            SplitterDistance = 600,
            Panel1MinSize = 500,
            Panel2MinSize = 280
        };

        var settingsHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
        var settings = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        var banner = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
            Text =
                "Trial run policies.\r\n" +
                "Full Trial timing, automatic first-chamber start, Boss finish, and Exit finish are functional. Active Challenges Only remains non-functional and is still in active development."
        };
        settings.Controls.Add(banner);

        _trialSekhemasRadio.Text = "Trial of the Sekhemas";
        _trialSekhemasRadio.AutoSize = true;
        _trialSekhemasRadio.Checked = true;
        _trialSekhemasRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        _trialChaosRadio.Text = "Trial of Chaos";
        _trialChaosRadio.AutoSize = true;
        _trialChaosRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        ConfigureTrialDescriptionLabel(_trialDescription);
        settings.Controls.Add(BuildTrialGroup("Trial", _trialSekhemasRadio, _trialChaosRadio, _trialDescription));

        _sekhemasLengthCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sekhemasLengthCombo.Width = 245;
        _sekhemasLengthCombo.Items.AddRange(new object[]
        {
            "1 floor",
            "2 floors",
            "3 floors",
            "4 floors"
        });
        _sekhemasLengthCombo.SelectedIndex = 0;
        _sekhemasLengthCombo.SelectedIndexChanged += (_, _) => UpdateTrialsUi();

        _chaosLengthCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _chaosLengthCombo.Width = 245;
        _chaosLengthCombo.Items.AddRange(new object[]
        {
            "4 rounds",
            "7 rounds",
            "10 rounds"
        });
        _chaosLengthCombo.SelectedIndex = 0;
        _chaosLengthCombo.SelectedIndexChanged += (_, _) => UpdateTrialsUi();

        _trialmasterCheck.Text = "Include Trialmaster";
        _trialmasterCheck.AutoSize = true;
        _trialmasterCheck.CheckedChanged += (_, _) => UpdateTrialsUi();

        var lengthPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        lengthPanel.Controls.Add(new Label { Text = "Sekhemas length:", AutoSize = true });
        lengthPanel.Controls.Add(_sekhemasLengthCombo);
        lengthPanel.Controls.Add(new Label { Text = "Chaos length:", AutoSize = true, Margin = new Padding(3, 8, 3, 0) });
        lengthPanel.Controls.Add(_chaosLengthCombo);
        lengthPanel.Controls.Add(_trialmasterCheck);
        ConfigureTrialDescriptionLabel(_trialLengthDescription);
        settings.Controls.Add(BuildTrialGroup("Trial length / category", lengthPanel, _trialLengthDescription));

        _trialFullTimeRadio.Text = "Full Trial — Recommended";
        _trialFullTimeRadio.AutoSize = true;
        _trialFullTimeRadio.Checked = true;
        _trialFullTimeRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        _trialActiveOnlyRadio.Text = "Active Challenges Only — NON-FUNCTIONAL — Active development";
        _trialActiveOnlyRadio.AutoSize = true;
        _trialActiveOnlyRadio.Enabled = false;
        ConfigureTrialDescriptionLabel(_trialTimingDescription);
        settings.Controls.Add(BuildTrialGroup(
            "Timing scope",
            _trialFullTimeRadio,
            _trialActiveOnlyRadio,
            _trialTimingDescription));

        _trialFixedStartLabel.Text = "Automatic: first active trial chamber — Required";
        _trialFixedStartLabel.AutoSize = true;
        ConfigureTrialDescriptionLabel(_trialStartDescription);
        settings.Controls.Add(BuildTrialGroup("Start rule", _trialFixedStartLabel, _trialStartDescription));

        _trialFinalBossRadio.Text = "Boss policy — Recommended";
        _trialFinalBossRadio.AutoSize = true;
        _trialFinalBossRadio.Checked = true;
        _trialFinalBossRadio.CheckedChanged += (_, _) =>
        {
            if (_trialFinalBossRadio.Checked)
                _trialMajorBossRadio.Checked = true;
            UpdateTrialsUi();
        };
        _trialExitRadio.Text = "Exit policy — Alternative Rules";
        _trialExitRadio.AutoSize = true;
        _trialExitRadio.CheckedChanged += (_, _) =>
        {
            if (_trialExitRadio.Checked)
                _trialEveryChallengeRadio.Checked = true;
            UpdateTrialsUi();
        };
        ConfigureTrialDescriptionLabel(_trialFinishDescription);
        settings.Controls.Add(BuildTrialGroup("Finish rule", _trialFinalBossRadio, _trialExitRadio, _trialFinishDescription));

        _trialFinalOnlyRadio.Text = "Final boss only — Alternative Rules";
        _trialFinalOnlyRadio.AutoSize = true;
        _trialFinalOnlyRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        _trialMajorBossRadio.Text = "Each boss kill — Recommended for Boss policy";
        _trialMajorBossRadio.AutoSize = true;
        _trialMajorBossRadio.Checked = true;
        _trialMajorBossRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        _trialEveryChallengeRadio.Text = "Trial completion / exit only — Recommended for Exit policy";
        _trialEveryChallengeRadio.AutoSize = true;
        _trialEveryChallengeRadio.CheckedChanged += (_, _) => UpdateTrialsUi();
        ConfigureTrialDescriptionLabel(_trialSplitsDescription);
        settings.Controls.Add(BuildTrialGroup(
            "Split frequency",
            _trialFinalOnlyRadio,
            _trialMajorBossRadio,
            _trialEveryChallengeRadio,
            _trialSplitsDescription));

        settingsHost.Controls.Add(settings);
        outer.Panel1.Controls.Add(settingsHost);

        var previewGroup = new GroupBox { Text = "Selected trial rules", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var previewRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(2)
        };
        previewRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        previewRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        AddTrialPreviewRow(previewRoot, 0, "Trial", _trialPreviewTrialValue);
        AddTrialPreviewRow(previewRoot, 1, "Length", _trialPreviewLengthValue);
        AddTrialPreviewRow(previewRoot, 2, "Timing", _trialPreviewTimingValue);
        AddTrialPreviewRow(previewRoot, 3, "Start", _trialPreviewStartValue);
        AddTrialPreviewRow(previewRoot, 4, "Finish", _trialPreviewFinishValue);
        AddTrialPreviewRow(previewRoot, 5, "Splits", _trialPreviewSplitsValue);

        previewGroup.Controls.Add(previewRoot);
        outer.Panel2.Controls.Add(previewGroup);

        UpdateTrialsUi();
        return outer;
    }

    private static GroupBox BuildTrialGroup(string title, params Control[] controls)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            Width = 565,
            Padding = new Padding(8, 6, 8, 8),
            Margin = new Padding(0, 0, 0, 7)
        };
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        foreach (var control in controls)
            body.Controls.Add(control);
        group.Controls.Add(body);
        return group;
    }

    private static void ConfigureTrialDescriptionLabel(Label label)
    {
        label.AutoSize = true;
        label.MaximumSize = new Size(525, 0);
        label.Padding = new Padding(18, 4, 0, 0);
        label.ForeColor = SystemColors.GrayText;
    }

    private static void AddTrialPreviewRow(TableLayoutPanel table, int row, string title, Label valueLabel)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont ?? Control.DefaultFont, FontStyle.Bold),
            Padding = new Padding(2, 5, 6, 5)
        };
        valueLabel.AutoSize = true;
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Padding = new Padding(2, 5, 2, 5);
        table.Controls.Add(titleLabel, 0, row);
        table.Controls.Add(valueLabel, 1, row);
    }

    private void UpdateTrialsUi()
    {
        var sekhemas = _trialSekhemasRadio.Checked;
        _sekhemasLengthCombo.Enabled = sekhemas;
        _chaosLengthCombo.Enabled = !sekhemas;

        var chaosTenRounds = !sekhemas && _chaosLengthCombo.SelectedIndex == 2;
        _trialmasterCheck.Enabled = chaosTenRounds;
        if (!chaosTenRounds)
            _trialmasterCheck.Checked = false;

        // Active Challenges Only is intentionally visible as a roadmap item but cannot
        // be selected until challenge/transition boundaries are proven reliable.
        _trialActiveOnlyRadio.Enabled = false;
        if (!_trialFullTimeRadio.Checked)
            _trialFullTimeRadio.Checked = true;

        // Exit-only splitting is meaningful only when the run itself finishes on exit.
        _trialEveryChallengeRadio.Enabled = _trialExitRadio.Checked;
        if (_trialFinalBossRadio.Checked && _trialEveryChallengeRadio.Checked)
            _trialFinalOnlyRadio.Checked = true;

        string trial = sekhemas ? "Trial of the Sekhemas" : "Trial of Chaos";
        string length = sekhemas
            ? $"{_sekhemasLengthCombo.SelectedIndex + 1} {(_sekhemasLengthCombo.SelectedIndex == 0 ? "floor" : "floors")}" 
            : _chaosLengthCombo.SelectedIndex switch
            {
                1 => "7 rounds",
                2 => "10 rounds",
                _ => "4 rounds"
            };
        if (!sekhemas && _trialmasterCheck.Checked)
            length += " + Trialmaster";

        string timing = "Full Trial";
        string start = "First active trial chamber";
        string finish = _trialFinalBossRadio.Checked ? "Boss policy" : "Exit policy";
        string splits = _trialFinalOnlyRadio.Checked
            ? "Final boss only"
            : _trialMajorBossRadio.Checked
                ? "Each boss kill"
                : "Trial completion / exit only";

        _trialDescription.Text = sekhemas
            ? "Runs the selected number of Sekhemas floors. Floor bosses are deterministic; Floor 2 contains two bosses, Hadi and Rafiq."
            : "Runs the selected Chaos round category. Chaos boss stages use the restricted Uxmal / Chetza / Bahlak dynamic pool; Trialmaster is a separate optional 10-round endpoint.";

        if (sekhemas)
        {
            var bossKills = GetSekhemasBossKillCount(_sekhemasLengthCombo.SelectedIndex + 1);
            _trialLengthDescription.Text =
                $"The run includes {length}. Selecting Each boss kill creates {bossKills} boss split{(bossKills == 1 ? "" : "s")} for this length; Floor 2 contributes two separate kills.";
        }
        else
        {
            var chaosBosses = GetChaosBossStageCount() + (_trialmasterCheck.Checked ? 1 : 0);
            _trialLengthDescription.Text =
                $"The run includes {length}. Selecting Each boss kill creates {chaosBosses} boss split{(chaosBosses == 1 ? "" : "s")} for this category. The random stage identity is filled in from BossWatcher at runtime.";
        }

        _trialTimingDescription.Text =
            "Full Trial counts all player-controlled trial time: combat, movement, paths, room/modifier choices, interactables, and decision time. Normal Client.txt loading-screen removal still applies when LiveSplit is displaying Game Time. Active Challenges Only is non-functional and remains in active development.";

        _trialStartDescription.Text = sekhemas
            ? "Start is automatic and cannot be changed: the timer starts on entry to the first active Sekhemas floor. Pre-trial lobby/setup time is outside the run."
            : "Start is automatic and cannot be changed: the timer starts on entry to the active Trial of Chaos arena. Temple preparation time is outside the run.";

        _trialFinishDescription.Text = _trialFinalBossRadio.Checked
            ? "Boss policy ends the run when the final required boss for the selected trial length is defeated. This is the recommended logical endpoint and requires BossWatcher to be running."
            : sekhemas
                ? "Exit policy ends the run when the player leaves the active Sekhemas trial and returns to the trial lobby. BossWatcher is not required when Trial completion / exit only is selected; boss-based intermediate splits still require it."
                : "Exit policy ends the run when the player leaves the active Trial of Chaos and returns to the Temple staging area. BossWatcher is not required when Trial completion / exit only is selected; boss-based intermediate splits still require it.";

        _trialSplitsDescription.Text = _trialFinalOnlyRadio.Checked
            ? (_trialExitRadio.Checked
                ? "Creates a split on the final required boss and a final split on trial exit. BossWatcher is required for the boss split."
                : "Creates only the final required boss split; that split also ends the run. BossWatcher is required.")
            : _trialMajorBossRadio.Checked
                ? (_trialExitRadio.Checked
                    ? "Creates a split for every required boss kill, then a final split on trial exit. BossWatcher is required for the boss splits."
                    : "Creates a split for every required boss kill; the final boss split ends the run. BossWatcher is required.")
                : "Creates no boss splits. The single split is the detected trial completion/exit boundary, so BossWatcher is not required.";

        _trialPreviewTrialValue.Text = trial;
        _trialPreviewLengthValue.Text = length;
        _trialPreviewTimingValue.Text = timing;
        _trialPreviewStartValue.Text = start;
        _trialPreviewFinishValue.Text = finish;
        _trialPreviewSplitsValue.Text = splits;
    }

    private static int GetSekhemasBossKillCount(int floors)
    {
        if (floors <= 0) return 0;
        // Floor 1: Rattlecage; Floor 2: Hadi + Rafiq; Floor 3: Ashar; Floor 4: Zarokh.
        return floors switch { 1 => 1, 2 => 3, 3 => 4, _ => 5 };
    }

    private int GetChaosBossStageCount() => _chaosLengthCombo.SelectedIndex switch
    {
        1 => 2,
        2 => 3,
        _ => 1
    };

    private Control BuildRoutePanel()
    {
        var group = new GroupBox { Text = "Custom route", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _orderedCheck.Text = "Ordered route (otherwise objectives may complete in any order)";
        _orderedCheck.AutoSize = true;
        _orderedCheck.CheckedChanged += (_, _) =>
        {
            UpdateCustomBossModeUi();
            RefreshCustomCatalogs();
            RefreshRouteList();
        };
        panel.Controls.Add(_orderedCheck, 0, 0);

        _routePolicySummary.AutoSize = true;
        _routePolicySummary.MaximumSize = new Size(430, 0);
        _routePolicySummary.Padding = new Padding(0, 3, 0, 3);
        panel.Controls.Add(_routePolicySummary, 0, 1);

        var startNote = new Label
        {
            Text = "Timer start is selected below. Riverbank uses the fresh-character Wounded Man gate; Zone Entry starts when the selected non-Riverbank zone is entered; Manual requires you to start LiveSplit yourself.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            Padding = new Padding(0, 4, 0, 4)
        };
        panel.Controls.Add(startNote, 0, 2);

        _routeList.Dock = DockStyle.Fill;
        panel.Controls.Add(_routeList, 0, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        buttons.Controls.Add(MakeButton("Move Up", () => MoveRoute(-1)));
        buttons.Controls.Add(MakeButton("Move Down", () => MoveRoute(1)));
        buttons.Controls.Add(MakeButton("Remove", RemoveRoute));
        buttons.Controls.Add(MakeButton("Clear", ClearRoute));
        panel.Controls.Add(buttons, 0, 4);
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildActionPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 3, ColumnCount = 4, Padding = new Padding(0, 8, 0, 0) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _deployButton.Text = "Generate / Deploy Selected Setup";
        _deployButton.AutoSize = true;
        _deployButton.Height = 38;
        _deployButton.Padding = new Padding(12, 2, 12, 2);
        _deployButton.Click += (_, _) => DeploySelected();
        panel.Controls.Add(_deployButton, 0, 0);

        _devConsoleCheck.Text = "Developer console diagnostics";
        _devConsoleCheck.AutoSize = true;
        _devConsoleCheck.Anchor = AnchorStyles.Right;
        panel.Controls.Add(_devConsoleCheck, 2, 0);

        var bossWatcher = new Button { Text = "Start BossWatcher", AutoSize = true, Height = 38, Padding = new Padding(12, 2, 12, 2) };
        bossWatcher.Click += (_, _) => StartBossWatcher();
        panel.Controls.Add(bossWatcher, 3, 0);

        var startPolicy = BuildStartPolicyPanel();
        panel.Controls.Add(startPolicy, 0, 1);
        panel.SetColumnSpan(startPolicy, 4);

        _excludeManualPauseCheck.Text = "Pause LiveSplit Game Time while PoE2 is manually paused (optional; requires GameTimeWatcher)";
        _excludeManualPauseCheck.AutoSize = true;
        _excludeManualPauseCheck.Anchor = AnchorStyles.Left;
        _excludeManualPauseCheck.CheckedChanged += (_, _) => _gameTimeWatcherButton.Enabled = _excludeManualPauseCheck.Checked;
        panel.Controls.Add(_excludeManualPauseCheck, 0, 2);
        panel.SetColumnSpan(_excludeManualPauseCheck, 3);

        _gameTimeWatcherButton.Text = "Start GameTimeWatcher";
        _gameTimeWatcherButton.AutoSize = true;
        _gameTimeWatcherButton.Height = 34;
        _gameTimeWatcherButton.Padding = new Padding(10, 1, 10, 1);
        _gameTimeWatcherButton.Enabled = false;
        _gameTimeWatcherButton.Click += (_, _) => StartGameTimeWatcher();
        panel.Controls.Add(_gameTimeWatcherButton, 3, 2);

        return panel;
    }

    private Control BuildStartPolicyPanel()
    {
        var group = new GroupBox
        {
            Text = "Timer Start (required — select exactly one)",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10, 6, 10, 8)
        };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, RowCount = 3, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _manualStartRadio.Text = "1. Manual Start — start LiveSplit yourself";
        _manualStartRadio.AutoSize = true;
        panel.Controls.Add(_manualStartRadio, 0, 0);
        panel.SetColumnSpan(_manualStartRadio, 2);

        _riverbankStartRadio.Text = "2. Riverbank Start — fresh character; auto-start after the Wounded Man's final opening line (default)";
        _riverbankStartRadio.AutoSize = true;
        panel.Controls.Add(_riverbankStartRadio, 0, 1);
        panel.SetColumnSpan(_riverbankStartRadio, 2);

        _zoneStartRadio.Text = "3. First Split Zone Entry Auto Start — start when this zone is entered:";
        _zoneStartRadio.AutoSize = true;
        _zoneStartRadio.CheckedChanged += (_, _) => UpdateStartZoneEnabled();
        panel.Controls.Add(_zoneStartRadio, 0, 2);

        _startZoneCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _startZoneCombo.Width = 430;
        _startZoneCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(_startZoneCombo, 1, 2);

        group.Controls.Add(panel);
        return group;
    }

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }

    private static bool IsTrialBossIdentity(RouteEntry entry) =>
        entry.Group.StartsWith("Trial of", StringComparison.OrdinalIgnoreCase);

    private List<RouteEntry> StandardBossCatalog() =>
        _bosses.Where(x => !IsTrialBossIdentity(x)).ToList();

    private RouteEntry RequiredBoss(string id) =>
        _bosses.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Required boss catalog entry is missing: {id}");

    private List<RouteEntry> BuildTrialBossRouteEntries()
    {
        var rattlecage = RequiredBoss("rattlecage_earthbreaker");
        var ashar = RequiredBoss("ashar_sand_mother");
        var zarokh = RequiredBoss("zarokh_temporal");
        var trialmaster = RequiredBoss("the_trialmaster");

        return new List<RouteEntry>
        {
            new RouteEntry { Type = "boss", Id = rattlecage.Id, Name = rattlecage.Name, Group = "Trial of the Sekhemas — Floor 1" },
            new RouteEntry
            {
                Type = "bossall",
                Id = "sekhemas_floor2",
                Name = "Hadi of the Flaming River + Rafiq of the Frozen Spring",
                Group = "Trial of the Sekhemas — Floor 2"
            },
            new RouteEntry { Type = "boss", Id = ashar.Id, Name = ashar.Name, Group = "Trial of the Sekhemas — Floor 3" },
            new RouteEntry { Type = "boss", Id = zarokh.Id, Name = zarokh.Name, Group = "Trial of the Sekhemas — Floor 4" },
            new RouteEntry { Type = "bossany", Id = "chaos_boss_1", Name = "Chaos Boss 1", Group = "Trial of Chaos — Dynamic boss pool" },
            new RouteEntry { Type = "bossany", Id = "chaos_boss_2", Name = "Chaos Boss 2", Group = "Trial of Chaos — Dynamic boss pool" },
            new RouteEntry { Type = "bossany", Id = "chaos_boss_3", Name = "Chaos Boss 3", Group = "Trial of Chaos — Dynamic boss pool" },
            new RouteEntry { Type = "boss", Id = trialmaster.Id, Name = trialmaster.Name, Group = "Trial of Chaos — Final boss" }
        };
    }

    private static bool SameRouteEntry(RouteEntry a, RouteEntry b) =>
        a.Type.Equals(b.Type, StringComparison.OrdinalIgnoreCase)
        && a.Id.Equals(b.Id, StringComparison.OrdinalIgnoreCase);

    private bool IsTrialBossRouteEntry(RouteEntry entry) =>
        _trialBossRouteEntries.Any(x => SameRouteEntry(x, entry));

    private void SetTrialBossRouteEntry(RouteEntry entry, bool enabled)
    {
        var existing = _customRoute.FindIndex(x => SameRouteEntry(x, entry));
        if (enabled)
        {
            if (existing < 0) _customRoute.Add(entry);
        }
        else if (existing >= 0)
        {
            _customRoute.RemoveAt(existing);
        }
        RefreshRouteList();
    }

    private void ClearTrialBossObjectives()
    {
        _syncingTrialBossChecklist = true;
        try
        {
            for (var i = 0; i < _trialBossChecklist.Items.Count; i++)
                _trialBossChecklist.SetItemChecked(i, false);
        }
        finally { _syncingTrialBossChecklist = false; }

        _customRoute.RemoveAll(IsTrialBossRouteEntry);
        RefreshRouteList();
    }

    private void ValidateTrialBossObjectives()
    {
        var routeEntries = _customRoute.Where(IsTrialBossRouteEntry).ToList();
        var checkedEntries = new List<RouteEntry>();
        for (var i = 0; i < _trialBossChecklist.Items.Count; i++)
        {
            if (_trialBossChecklist.GetItemChecked(i) && _trialBossChecklist.Items[i] is RouteEntry entry)
                checkedEntries.Add(entry);
        }

        if (!_trialBossObjectivesCheck.Checked)
        {
            if (routeEntries.Count > 0)
                throw new InvalidOperationException("Trial boss objectives are present even though Add trial boss objectives is disabled.");
            return;
        }

        if (routeEntries.Count != checkedEntries.Count
            || checkedEntries.Any(expected => !routeEntries.Any(actual => SameRouteEntry(actual, expected))))
            throw new InvalidOperationException("Trial boss objectives no longer match the checked trial boss selections. Toggle the trial-boss option off and reselect the desired milestones.");
    }

    private void PopulatePresets()
    {
        UpdatePremadeSelectorUi(true);
    }

    private string PremadeMode => _premadeModeCombo.SelectedItem?.ToString() ?? "Area Completion";
    private string PremadeSetup => _premadeSetupCombo.SelectedItem?.ToString() ?? "";
    private bool PremadeOrdered => _premadeOrderedRadio.Checked;
    private bool PremadeHasBosses => PremadeMode is "Boss Completion" or "Area + Boss Completion";
    private bool PremadeHasAreas => PremadeMode is "Area Completion" or "Area + Boss Completion";
    private bool PremadeIsLevelRace => PremadeMode == "Level Race";
    private bool PremadeIsCombination => PremadeSetup.StartsWith("Combination", StringComparison.OrdinalIgnoreCase);
    private bool PremadeIsAnyPercent => PremadeSetup.Contains("Any%", StringComparison.OrdinalIgnoreCase);

    private void UpdatePremadeSelectorUi(bool rebuildSetups)
    {
        if (_updatingPremadeUi) return;
        _updatingPremadeUi = true;
        try
        {
            var mode = PremadeMode;
            var mixed = mode == "Area + Boss Completion";
            var level = mode == "Level Race";

            _premadeOrderedRadio.Enabled = !mixed;
            _premadeDynamicRadio.Enabled = !level;
            if (mixed && !_premadeDynamicRadio.Checked) _premadeDynamicRadio.Checked = true;
            if (level && !_premadeOrderedRadio.Checked) _premadeOrderedRadio.Checked = true;

            if (rebuildSetups || _premadeSetupCombo.Items.Count == 0)
            {
                var previous = PremadeSetup;
                _premadeSetupCombo.BeginUpdate();
                _premadeSetupCombo.Items.Clear();
                foreach (var setup in GetPremadeSetupLabels(mode)) _premadeSetupCombo.Items.Add(setup);
                _premadeSetupCombo.EndUpdate();
                var match = previous.Length == 0 ? -1 : _premadeSetupCombo.Items.IndexOf(previous);
                _premadeSetupCombo.SelectedIndex = match >= 0 ? match : (_premadeSetupCombo.Items.Count > 0 ? 0 : -1);
            }

            _premadeCombinationPanel.Visible = PremadeIsCombination;
            if (_premadeCombinationPanel.Parent is not null) _premadeCombinationPanel.Parent.Visible = PremadeIsCombination;
            if (PremadeIsCombination && _premadeCombinationList.CheckedItems.Count == 0 && _premadeCombinationList.Items.Count > 0)
                _premadeCombinationList.SetItemChecked(0, true);

            var groups = SelectedPremadeGroups();
            var campaign = PremadeSetup.StartsWith("Campaign", StringComparison.OrdinalIgnoreCase);
            var pinnacle = PremadeSetup.Equals("Pinnacle", StringComparison.OrdinalIgnoreCase);
            // Trial opt-ins are for campaign / Act practice-style premades. Pinnacle keeps
            // its established target list (including Zarokh and Trialmaster) because those
            // encounters are category-defining pinnacle targets rather than optional
            // campaign trial progression.
            var sekEligibleGroups = new[] { "Act 2", "Act 3", "Act 4", "Interlude 1", "Interlude 2", "Interlude 3" };
            var chaosEligibleGroups = new[] { "Act 3", "Act 4", "Interlude 1", "Interlude 2", "Interlude 3" };
            var sekAvailable = !level && !pinnacle
                && (campaign || groups.Any(g => sekEligibleGroups.Contains(g, StringComparer.OrdinalIgnoreCase)));
            var chaosAvailable = !level && !pinnacle
                && (campaign || groups.Any(g => chaosEligibleGroups.Contains(g, StringComparer.OrdinalIgnoreCase)));

            _premadeSekhemasCheck.Enabled = sekAvailable;
            _premadeChaosCheck.Enabled = chaosAvailable;
            if (!sekAvailable) _premadeSekhemasCheck.Checked = false;
            if (!chaosAvailable) _premadeChaosCheck.Checked = false;
            _premadeSekhemasPanel.Visible = sekAvailable && _premadeSekhemasCheck.Checked;
            _premadeChaosPanel.Visible = chaosAvailable && _premadeChaosCheck.Checked;

            // Trial stage selection applies to both area and boss premades. Trialmaster
            // itself is a boss objective, so hide that one checkbox for pure area runs.
            _premadeChaosBoss1.Visible = true;
            _premadeChaosBoss2.Visible = true;
            _premadeChaosBoss3.Visible = true;
            _premadeTrialmaster.Visible = PremadeHasBosses;
            if (!PremadeHasBosses && _premadeTrialmaster.Checked)
                _premadeTrialmaster.Checked = false;

            PopulatePremadeInsertionCombos();
            UpdatePresetDescription();
            UpdateStartZoneEnabled();
        }
        finally { _updatingPremadeUi = false; }
    }

    private static IReadOnlyList<string> GetPremadeSetupLabels(string mode)
    {
        if (mode == "Level Race") return new[] { "Level 100 — Every 10 Levels" };

        var common = new List<string>
        {
            "Campaign 100%",
            "Campaign Any%",
            "Act 1 — 100%",
            "Act 1 — Any%",
            "Act 2 — 100%",
            "Act 2 — Any%",
            "Act 3 — 100%",
            "Act 3 — Any%",
            "Act 4 — 100%",
            "Act 4 — Any%",
            "Interlude 1",
            "Interlude 2",
            "Interlude 3",
            "All Interludes",
            "Combination — 100%",
            "Combination — Any%"
        };
        if (mode == "Boss Completion") common.Add("Pinnacle");
        return common;
    }

    private List<string> SelectedPremadeGroups()
    {
        var setup = PremadeSetup;
        if (setup.StartsWith("Act ", StringComparison.OrdinalIgnoreCase))
            return new List<string> { setup.Split('—')[0].Trim() };
        if (setup.StartsWith("Interlude ", StringComparison.OrdinalIgnoreCase) && !setup.Equals("All Interludes", StringComparison.OrdinalIgnoreCase))
            return new List<string> { setup.Trim() };
        if (setup.Equals("All Interludes", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "Interlude 1", "Interlude 2", "Interlude 3" };
        if (PremadeIsCombination)
            return _premadeCombinationList.CheckedItems.Cast<string>().ToList();
        if (setup.StartsWith("Campaign", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "Act 1", "Act 2", "Act 3", "Act 4", "Interlude 1", "Interlude 2", "Interlude 3" };
        return new List<string>();
    }

    private static readonly string[] TrialAreaIds =
    {
        "G2_13", "Sanctum_1_*", "Sanctum_2_*", "Sanctum_3_*", "Sanctum_4_*", "G3_10_Airlock", "G3_10"
    };

    private static readonly HashSet<string> TrialBossIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "rattlecage_earthbreaker", "hadi_flaming_river", "rafiq_frozen_spring", "ashar_sand_mother", "zarokh_temporal",
        "uxmal_beastlord", "chetza_feathered_plague", "bahlak_sky_seer", "the_trialmaster"
    };

    private List<RouteEntry> BuildPremadeBaseObjectives()
    {
        if (PremadeIsLevelRace)
        {
            var levels = new List<RouteEntry>();
            for (var level = 10; level <= 100; level += 10)
                levels.Add(new RouteEntry { Type = "level", Id = level.ToString(), Name = $"Level {level}", Group = "Level Race" });
            return levels;
        }

        var result = new List<RouteEntry>();
        if (PremadeHasAreas) result.AddRange(BuildPremadeAreaObjectives());
        if (PremadeHasBosses) result.AddRange(BuildPremadeBossObjectives());
        return result;
    }

    private List<RouteEntry> BuildPremadeAreaObjectives()
    {
        var setup = PremadeSetup;
        var entries = new List<RouteEntry>();
        if (setup.StartsWith("Campaign", StringComparison.OrdinalIgnoreCase))
        {
            var rel = PremadeIsAnyPercent ? "01-Ordered/routes/campaign-any-percent.txt" : "01-Ordered/routes/campaign-100-percent.txt";
            entries.AddRange(LoadAreaRouteEntries(rel));
        }
        else
        {
            foreach (var group in SelectedPremadeGroups())
            {
                var rel = AreaRoutePathForGroup(group, PremadeIsAnyPercent);
                if (rel is not null) entries.AddRange(LoadAreaRouteEntries(rel));
            }
        }

        // Trial entry/lobby areas are optional premade content. Strip any legacy
        // occurrences from the source route and add them back only through the controls.
        return entries
            .Where(x => !TrialAreaIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private string? AreaRoutePathForGroup(string group, bool anyPercent)
    {
        return group switch
        {
            "Act 1" => anyPercent ? "05-Segment/presets/act1-any-percent.txt" : "05-Segment/presets/act1-all-areas.txt",
            "Act 2" => anyPercent ? "05-Segment/presets/act2-any-percent.txt" : "05-Segment/presets/act2-all-areas.txt",
            "Act 3" => anyPercent ? "05-Segment/presets/act3-any-percent.txt" : "05-Segment/presets/act3-all-areas.txt",
            "Act 4" => anyPercent ? "05-Segment/presets/act4-any-percent.txt" : "05-Segment/presets/act4-all-areas.txt",
            "Interlude 1" => "05-Segment/presets/interlude-1.txt",
            "Interlude 2" => "05-Segment/presets/interlude-2.txt",
            "Interlude 3" => "05-Segment/presets/interlude-3.txt",
            _ => null
        };
    }

    private List<RouteEntry> LoadAreaRouteEntries(string relativePath)
    {
        var path = Resolve(relativePath);
        if (!File.Exists(path)) throw new FileNotFoundException("Premade area route source was not found.", path);
        var byId = _areas.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<RouteEntry>();
        string? sourceStartId = null;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("@start=", StringComparison.OrdinalIgnoreCase))
            {
                var startValue = line[7..].Trim();
                if (!startValue.Equals("manual", StringComparison.OrdinalIgnoreCase)) sourceStartId = startValue;
                continue;
            }
            if (line.StartsWith("@", StringComparison.Ordinal)) continue;
            if (line.StartsWith("area|", StringComparison.OrdinalIgnoreCase)) line = line[5..].Trim();
            if (byId.TryGetValue(line, out var entry)) result.Add(entry);
        }
        if (sourceStartId is not null && byId.TryGetValue(sourceStartId, out var startEntry)
            && !result.Any(x => x.Id.Equals(sourceStartId, StringComparison.OrdinalIgnoreCase)))
            result.Insert(0, startEntry);
        return result;
    }

    private List<RouteEntry> BuildPremadeBossObjectives()
    {
        var ids = LoadBossIdList(PremadeSetup.Equals("Pinnacle", StringComparison.OrdinalIgnoreCase)
            ? "BossWatcher/BossLists/pinnacle-v0.5.txt"
            : PremadeIsAnyPercent
                ? "BossWatcher/BossLists/campaign-any-v0.5.txt"
                : "BossWatcher/BossLists/campaign-100.txt");

        var groups = SelectedPremadeGroups();
        var campaign = PremadeSetup.StartsWith("Campaign", StringComparison.OrdinalIgnoreCase);
        var pinnacle = PremadeSetup.Equals("Pinnacle", StringComparison.OrdinalIgnoreCase);
        var byId = _bosses.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<RouteEntry>();
        foreach (var id in ids)
        {
            // Campaign/Act premades keep trial bosses opt-in. Pinnacle preserves its
            // established list, where Zarokh and Trialmaster are normal pinnacle targets.
            if (!pinnacle && TrialBossIds.Contains(id)) continue;
            if (!byId.TryGetValue(id, out var entry)) continue;
            if (campaign || pinnacle || groups.Contains(entry.Group, StringComparer.OrdinalIgnoreCase))
                result.Add(entry);
        }
        return result;
    }

    private List<string> LoadBossIdList(string relativePath)
    {
        var path = Resolve(relativePath);
        if (!File.Exists(path)) throw new FileNotFoundException("Premade boss list was not found.", path);
        var ids = new List<string>();
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            var id = parts.Length > 1 && int.TryParse(parts[0], out _) ? parts[1].Trim() : parts[0].Trim();
            if (id.Length > 0) ids.Add(id);
        }
        return ids;
    }

    private List<RouteEntry> BuildPremadeObjectivesWithTrials()
    {
        var objectives = BuildPremadeBaseObjectives();
        if (PremadeIsLevelRace) return objectives;

        var scheduled = new List<PremadeScheduledBlock>();
        var sequence = 0;

        if (_premadeSekhemasCheck.Checked)
        {
            if (_premadeSekhemasFloor1.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeSekhemasInsertCombo,
                    Entries = BuildSekhemasPremadeFloor(1)
                });
            if (_premadeSekhemasFloor2.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeSekhemasFloor2InsertCombo,
                    Entries = BuildSekhemasPremadeFloor(2)
                });
            if (_premadeSekhemasFloor3.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeSekhemasFloor3InsertCombo,
                    Entries = BuildSekhemasPremadeFloor(3)
                });
            if (_premadeSekhemasFloor4.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeSekhemasFloor4InsertCombo,
                    Entries = BuildSekhemasPremadeFloor(4)
                });
        }

        if (_premadeChaosCheck.Checked)
        {
            if (_premadeChaosBoss1.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeChaosInsertCombo,
                    Entries = BuildChaosPremadeStage(1)
                });
            if (_premadeChaosBoss2.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeChaosStage2InsertCombo,
                    Entries = BuildChaosPremadeStage(2)
                });
            if (_premadeChaosBoss3.Checked)
                scheduled.Add(new PremadeScheduledBlock
                {
                    Sequence = sequence++,
                    PredecessorCombo = _premadeChaosStage3InsertCombo,
                    Entries = BuildChaosPremadeStage(3)
                });
        }

        ApplyPremadeScheduledBlocks(objectives, scheduled);
        return objectives;
    }

    private List<RouteEntry> BuildSekhemasPremadeFloor(int floor)
    {
        var additions = new List<RouteEntry>();
        if (PremadeHasAreas)
        {
            // The lobby is a distinct campaign area and is included once with Floor 1.
            if (floor == 1)
                additions.Add(new RouteEntry { Type = "area", Id = "G2_13", Name = "Trial of the Sekhemas", Group = "Trial of the Sekhemas" });
            additions.Add(new RouteEntry
            {
                Type = "area",
                Id = $"Sanctum_{floor}_*",
                Name = $"Trial of the Sekhemas — Floor {floor}",
                Group = "Trial of the Sekhemas"
            });
        }

        if (PremadeHasBosses)
        {
            if (floor == 1)
            {
                var rattle = RequiredBoss("rattlecage_earthbreaker");
                additions.Add(new RouteEntry { Type = "boss", Id = rattle.Id, Name = rattle.Name, Group = "Trial of the Sekhemas" });
            }
            else if (floor == 2)
            {
                additions.Add(new RouteEntry
                {
                    Type = "bossall",
                    Id = "sekhemas_floor2",
                    Name = "Hadi of the Flaming River + Rafiq of the Frozen Spring",
                    Group = "Trial of the Sekhemas"
                });
            }
            else if (floor == 3)
            {
                var ashar = RequiredBoss("ashar_sand_mother");
                additions.Add(new RouteEntry { Type = "boss", Id = ashar.Id, Name = ashar.Name, Group = "Trial of the Sekhemas" });
            }
            else if (floor == 4)
            {
                var zarokh = RequiredBoss("zarokh_temporal");
                additions.Add(new RouteEntry { Type = "boss", Id = zarokh.Id, Name = zarokh.Name, Group = "Trial of the Sekhemas" });
            }
        }
        return additions;
    }

    private List<RouteEntry> BuildChaosPremadeStage(int stage)
    {
        var additions = new List<RouteEntry>();
        var rounds = stage switch { 2 => 7, 3 => 10, _ => 4 };

        if (PremadeHasAreas)
        {
            // The Temple/Airlock is included once with the first visit. The active
            // trial area is reused by the game, so later visits need unique occurrence
            // keys even though they listen for the same underlying area entry.
            if (stage == 1)
                additions.Add(new RouteEntry { Type = "area", Id = "G3_10_Airlock", Name = "The Temple of Chaos", Group = "Trial of Chaos" });
            additions.Add(new RouteEntry
            {
                Type = "areaocc",
                Id = $"G3_10~{stage}",
                Name = $"Trial of Chaos — {rounds} Rounds",
                Group = "Trial of Chaos"
            });
        }

        if (PremadeHasBosses)
        {
            additions.Add(new RouteEntry
            {
                Type = "bossany",
                Id = $"chaos_boss_{stage}",
                Name = $"Chaos Boss — {rounds}-Round Trial",
                Group = "Trial of Chaos"
            });
            if (stage == 3 && _premadeTrialmaster.Checked)
            {
                var trialmaster = RequiredBoss("the_trialmaster");
                additions.Add(new RouteEntry { Type = "boss", Id = trialmaster.Id, Name = trialmaster.Name, Group = "Trial of Chaos" });
            }
        }
        return additions;
    }

    private sealed class PremadeScheduledBlock
    {
        public int Sequence { get; init; }
        public ComboBox PredecessorCombo { get; init; } = null!;
        public List<RouteEntry> Entries { get; init; } = [];
    }

    private void ApplyPremadeScheduledBlocks(List<RouteEntry> objectives, IReadOnlyList<PremadeScheduledBlock> blocks)
    {
        var populated = blocks.Where(x => x.Entries.Count > 0).OrderBy(x => x.Sequence).ToList();
        if (populated.Count == 0) return;

        if (!PremadeOrdered)
        {
            foreach (var block in populated)
                objectives.AddRange(block.Entries);
            return;
        }

        // Resolve every insertion point against the untouched base route. This is
        // important when two trial segments intentionally use the same predecessor
        // (for example Sekhemas Floors 2 and 3 back-to-back). Rebuilding from the base
        // route preserves the selected trial sequence instead of reversing insertions.
        var startBlocks = new List<PremadeScheduledBlock>();
        var endBlocks = new List<PremadeScheduledBlock>();
        var afterIndex = new Dictionary<int, List<PremadeScheduledBlock>>();

        foreach (var block in populated)
        {
            if (block.PredecessorCombo.SelectedItem is not PremadeInsertItem item)
            {
                endBlocks.Add(block);
                continue;
            }
            if (item.AtStart)
            {
                startBlocks.Add(block);
                continue;
            }
            if (item.AtEnd)
            {
                endBlocks.Add(block);
                continue;
            }

            var index = objectives.FindIndex(x =>
                x.Type.Equals(item.Type, StringComparison.OrdinalIgnoreCase)
                && x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                endBlocks.Add(block);
                continue;
            }
            if (!afterIndex.TryGetValue(index, out var list))
            {
                list = new List<PremadeScheduledBlock>();
                afterIndex[index] = list;
            }
            list.Add(block);
        }

        var rebuilt = new List<RouteEntry>();
        foreach (var block in startBlocks.OrderBy(x => x.Sequence))
            rebuilt.AddRange(block.Entries);

        for (var i = 0; i < objectives.Count; i++)
        {
            rebuilt.Add(objectives[i]);
            if (afterIndex.TryGetValue(i, out var list))
                foreach (var block in list.OrderBy(x => x.Sequence))
                    rebuilt.AddRange(block.Entries);
        }

        foreach (var block in endBlocks.OrderBy(x => x.Sequence))
            rebuilt.AddRange(block.Entries);

        objectives.Clear();
        objectives.AddRange(rebuilt);
    }

    private sealed class PremadeInsertItem
    {
        public string Type { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public bool AtStart { get; init; }
        public bool AtEnd { get; init; }
        public override string ToString() => AtStart ? "<Start of route>" : AtEnd ? "<End of route>" : Name;
    }

    private void PopulatePremadeInsertionCombos()
    {
        List<RouteEntry> baseObjectives;
        try { baseObjectives = BuildPremadeBaseObjectives(); }
        catch { baseObjectives = new List<RouteEntry>(); }

        var predecessorType = PremadeHasBosses && !PremadeHasAreas ? "boss" : "area";
        var candidates = baseObjectives.Where(x => x.Type.Equals(predecessorType, StringComparison.OrdinalIgnoreCase)).ToList();

        PopulateInsertionCombo(_premadeSekhemasInsertCombo, candidates,
            predecessorType == "area" ? "G2_3" : "jamanra_risen_king");
        PopulateInsertionCombo(_premadeSekhemasFloor2InsertCombo, candidates, "");
        PopulateInsertionCombo(_premadeSekhemasFloor3InsertCombo, candidates, "");
        PopulateInsertionCombo(_premadeSekhemasFloor4InsertCombo, candidates, "");

        PopulateInsertionCombo(_premadeChaosInsertCombo, candidates,
            predecessorType == "area" ? "G3_5" : "xyclucian_the_chimera");
        PopulateInsertionCombo(_premadeChaosStage2InsertCombo, candidates, "");
        PopulateInsertionCombo(_premadeChaosStage3InsertCombo, candidates, "");

        var ordered = PremadeOrdered && !PremadeIsLevelRace;
        SetPremadeScheduleVisibility(_premadeSekhemasInsertCombo, ordered && _premadeSekhemasFloor1.Checked);
        SetPremadeScheduleVisibility(_premadeSekhemasFloor2InsertCombo, ordered && _premadeSekhemasFloor2.Checked);
        SetPremadeScheduleVisibility(_premadeSekhemasFloor3InsertCombo, ordered && _premadeSekhemasFloor3.Checked);
        SetPremadeScheduleVisibility(_premadeSekhemasFloor4InsertCombo, ordered && _premadeSekhemasFloor4.Checked);
        SetPremadeScheduleVisibility(_premadeChaosInsertCombo, ordered && _premadeChaosBoss1.Checked);
        SetPremadeScheduleVisibility(_premadeChaosStage2InsertCombo, ordered && _premadeChaosBoss2.Checked);
        SetPremadeScheduleVisibility(_premadeChaosStage3InsertCombo, ordered && _premadeChaosBoss3.Checked);
    }

    private static void SetPremadeScheduleVisibility(ComboBox combo, bool visible)
    {
        // The combo lives inside the small "Run after" panel; keep the stage checkbox
        // visible while hiding only the placement controls for dynamic routes/unselected stages.
        if (combo.Parent is not null)
            combo.Parent.Visible = visible;
    }

    private static void PopulateInsertionCombo(ComboBox combo, IReadOnlyList<RouteEntry> candidates, string defaultId)
    {
        var prior = combo.SelectedItem as PremadeInsertItem;
        var previousId = prior is { AtStart: false, AtEnd: false } ? prior.Id : "";
        var previousAtStart = prior?.AtStart == true;
        var previousAtEnd = prior?.AtEnd == true;

        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.Add(new PremadeInsertItem { AtStart = true, Name = "Start of route" });
        foreach (var entry in candidates)
            combo.Items.Add(new PremadeInsertItem { Type = entry.Type, Id = entry.Id, Name = entry.Name });
        combo.Items.Add(new PremadeInsertItem { AtEnd = true, Name = "End of route" });
        combo.EndUpdate();

        if (previousAtStart)
        {
            combo.SelectedIndex = 0;
            return;
        }
        if (previousAtEnd)
        {
            combo.SelectedIndex = combo.Items.Count - 1;
            return;
        }

        foreach (var desired in new[] { previousId, defaultId }.Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is PremadeInsertItem item
                    && !item.AtStart && !item.AtEnd
                    && item.Id.Equals(desired, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        combo.SelectedIndex = combo.Items.Count - 1;
    }

    private void PopulateCustomCatalogs()
    {
        var defaultBossCount = GetCampaignMinimumBossCount();
        _unorderedBossTargetNumeric.Value = Math.Clamp(defaultBossCount, (int)_unorderedBossTargetNumeric.Minimum, (int)_unorderedBossTargetNumeric.Maximum);
        _unorderedBossTargetNote.Text = $"Dynamic/unordered boss mode: selected bosses form the eligible pool and repeated kills count as separate encounters. Default {defaultBossCount}, matching the current minimum campaign-required boss baseline.";
        if (_customCatalogGroupCombo.Items.Count > 0 && _customCatalogGroupCombo.SelectedIndex < 0)
            _customCatalogGroupCombo.SelectedIndex = 0;
        UpdateCustomBossModeUi();
        RefreshCustomCatalogs();
    }

    private int GetCampaignMinimumBossCount()
    {
        try
        {
            var path = Resolve("BossWatcher/BossLists/campaign-any-v0.5.txt");
            if (File.Exists(path))
                return File.ReadLines(path).Count(line =>
                {
                    var trimmed = line.Trim();
                    return trimmed.Length > 0 && !trimmed.StartsWith('#');
                });
        }
        catch { }
        return 40;
    }

    private string SelectedCustomCatalogGroup => _customCatalogGroupCombo.SelectedItem?.ToString() ?? "Act 1";

    private bool AreaMatchesCustomGroup(RouteEntry entry)
    {
        var selected = SelectedCustomCatalogGroup;
        if (selected.Equals("Pinnacle", StringComparison.OrdinalIgnoreCase))
            return entry.Group.Equals("Endgame", StringComparison.OrdinalIgnoreCase);
        return entry.Group.Equals(selected, StringComparison.OrdinalIgnoreCase);
    }

    private bool BossMatchesCustomGroup(RouteEntry entry)
    {
        var selected = SelectedCustomCatalogGroup;
        if (selected.Equals("Pinnacle", StringComparison.OrdinalIgnoreCase))
            return entry.Group.StartsWith("Pinnacle", StringComparison.OrdinalIgnoreCase);
        return entry.Group.Equals(selected, StringComparison.OrdinalIgnoreCase);
    }

    private static string BaseBossId(RouteEntry entry)
    {
        if (entry.Type.Equals("bossocc", StringComparison.OrdinalIgnoreCase))
        {
            var hash = entry.Id.LastIndexOf('~');
            return hash > 0 ? entry.Id.Substring(0, hash) : entry.Id;
        }
        return entry.Id;
    }

    private bool IsAreaAlreadySelected(RouteEntry entry) =>
        _customRoute.Any(x => x.Type.Equals("area", StringComparison.OrdinalIgnoreCase)
            && x.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));

    private bool IsStandardBossAlreadySelected(RouteEntry entry) =>
        _customRoute.Any(x =>
            (x.Type.Equals("boss", StringComparison.OrdinalIgnoreCase)
             || x.Type.Equals("bossocc", StringComparison.OrdinalIgnoreCase)
             || x.Type.Equals("bosspoolmember", StringComparison.OrdinalIgnoreCase))
            && BaseBossId(x).Equals(entry.Id, StringComparison.OrdinalIgnoreCase));

    private void RefreshCustomCatalogs()
    {
        RefreshCustomAreaList();
        RefreshCustomBossGrid();
    }

    private void RefreshCustomAreaList()
    {
        var term = _areaSearch.Text.Trim();
        var selectedId = (_areaList.SelectedItem as RouteEntry)?.Id;
        _areaList.BeginUpdate();
        _areaList.Items.Clear();
        foreach (var entry in _areas
                     .Where(AreaMatchesCustomGroup)
                     .Where(x => !IsAreaAlreadySelected(x))
                     .Where(x => term.Length == 0 || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Name))
            _areaList.Items.Add(entry);
        _areaList.EndUpdate();
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            for (var i = 0; i < _areaList.Items.Count; i++)
                if (_areaList.Items[i] is RouteEntry entry && entry.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
                { _areaList.SelectedIndex = i; break; }
        }
    }

    private void RefreshCustomBossGrid()
    {
        var term = _bossSearch.Text.Trim();
        // Filtering/rebuilding the catalog is a navigation action. Cancel any in-progress
        // occurrence-cell edit rather than forcing validation while changing Act/Interlude.
        _bossGrid.CancelEdit();
        _bossGrid.Rows.Clear();
        foreach (var entry in StandardBossCatalog()
                     .Where(BossMatchesCustomGroup)
                     .Where(x => !IsStandardBossAlreadySelected(x))
                     .Where(x => term.Length == 0 || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Name))
        {
            var rowIndex = _bossGrid.Rows.Add(entry.Name, 1);
            _bossGrid.Rows[rowIndex].Tag = entry;
        }
        if (_bossGrid.Rows.Count > 0) _bossGrid.ClearSelection();
    }

    private void UpdateCustomBossModeUi()
    {
        var ordered = _orderedCheck.Checked;
        _orderedBossOptionsPanel.Visible = ordered;
        _unorderedBossOptionsPanel.Visible = !ordered;
        var occurrenceColumn = _bossGrid.Columns["Occurrences"];
        if (occurrenceColumn is not null)
            occurrenceColumn.Visible = ordered && _multiBossCheck.Checked;

        _routePolicySummary.Text = ordered
            ? "Ordered boss objectives are exact encounters. Multi-boss mode can create repeated copies of the same boss, each with a unique internal occurrence key."
            : $"Dynamic/unordered boss policy: BOSS POOL rows define eligible identities. The generated run requires {_unorderedBossTargetNumeric.Value} boss encounter(s); repeat kills of the same eligible boss count separately and each completed row is renamed to the detected boss.";
    }

    private void PopulateStartZones()
    {
        _startZoneCombo.BeginUpdate();
        _startZoneCombo.Items.Clear();
        foreach (var area in _areas
                     .Where(x => !x.Id.Equals("G1_1", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Group)
                     .ThenBy(x => x.Name))
            _startZoneCombo.Items.Add(area);
        _startZoneCombo.EndUpdate();
        if (_startZoneCombo.Items.Count > 0) _startZoneCombo.SelectedIndex = 0;
    }

    private static void RefreshAvailableList(ListBox list, List<RouteEntry> source, string filter)
    {
        var term = filter.Trim();
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var group in source
                     .Where(x => term.Length == 0 || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Group).ThenBy(x => x.Name)
                     .GroupBy(x => x.Group))
        {
            foreach (var entry in group) list.Items.Add(entry);
        }
        list.EndUpdate();
    }

    private void UpdatePresetDescription()
    {
        if (_premadeSetupCombo.SelectedIndex < 0)
        {
            _presetDescription.Text = "Select a setup.";
            return;
        }

        var order = PremadeOrdered ? "Ordered" : "Dynamic / unordered";
        var trialParts = new List<string>();
        if (_premadeSekhemasCheck.Checked)
        {
            var depth = new[] { _premadeSekhemasFloor1, _premadeSekhemasFloor2, _premadeSekhemasFloor3, _premadeSekhemasFloor4 }.Count(x => x.Checked);
            trialParts.Add($"Sekhemas: {depth} floor(s)");
        }
        if (_premadeChaosCheck.Checked)
        {
            var stages = new List<string>();
            if (_premadeChaosBoss1.Checked) stages.Add("4 rounds");
            if (_premadeChaosBoss2.Checked) stages.Add("7 rounds");
            if (_premadeChaosBoss3.Checked) stages.Add("10 rounds");
            var stageText = stages.Count == 0 ? "no stage selected" : string.Join(", ", stages);
            trialParts.Add($"Chaos: {stageText}{(_premadeTrialmaster.Checked ? " + Trialmaster" : "")}");
        }

        var objectiveCount = 0;
        try { objectiveCount = BuildPremadeObjectivesWithTrials().Count; } catch { }
        var watcher = PremadeHasBosses ? "BossWatcher required" : "BossWatcher not required";
        var trialText = trialParts.Count == 0 ? "Trials: none (opt-in)" : "Trials: " + string.Join("; ", trialParts);
        var mixedNote = PremadeMode == "Area + Boss Completion"
            ? " Area + Boss premades currently use dynamic/unordered completion; an ordered mixed campaign baseline has not been defined."
            : "";
        _presetDescription.Text = $"{PremadeMode} — {PremadeSetup} — {order}. {objectiveCount} generated objective(s). {watcher}. {trialText}.{mixedNote}";
    }

    private void UpdateStartZoneEnabled()
    {
        var trialsSelected = _modeTabs.SelectedIndex == 2;

        // Dedicated Trials owns a fixed first-chamber start policy. The standard
        // Manual / Riverbank / arbitrary-zone controls therefore do not apply.
        _manualStartRadio.Enabled = !trialsSelected;
        _riverbankStartRadio.Enabled = !trialsSelected;
        _zoneStartRadio.Enabled = !trialsSelected;
        _startZoneCombo.Enabled = !trialsSelected && _zoneStartRadio.Checked;
        _deployButton.Enabled = true;

        if (trialsSelected)
            UpdateTrialsUi();
    }

    private StartPolicy GetRequiredStartPolicy()
    {
        var selectedCount = (_manualStartRadio.Checked ? 1 : 0)
            + (_riverbankStartRadio.Checked ? 1 : 0)
            + (_zoneStartRadio.Checked ? 1 : 0);
        if (selectedCount != 1)
            throw new InvalidOperationException("Select exactly one Timer Start option before generating the setup.");

        if (_manualStartRadio.Checked)
            return new StartPolicy { Mode = StartMode.Manual };

        if (_riverbankStartRadio.Checked)
            return new StartPolicy { Mode = StartMode.Riverbank, AreaId = "G1_1", AreaName = "The Riverbank" };

        if (_startZoneCombo.SelectedItem is not RouteEntry zone)
            throw new InvalidOperationException("Select a start zone for First Split Zone Entry Auto Start.");
        if (zone.Id.Equals("G1_1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The Riverbank is reserved for the Riverbank Start option. Choose a different zone.");

        return new StartPolicy { Mode = StartMode.ZoneEntry, AreaId = zone.Id, AreaName = zone.Name };
    }

    private static string DescribeStartPolicy(StartPolicy policy) => policy.Mode switch
    {
        StartMode.Manual => "MANUAL START — start LiveSplit yourself.",
        StartMode.Riverbank => "RIVERBANK START — fresh character; LiveSplit auto-starts on the Wounded Man's final opening line.",
        StartMode.ZoneEntry => $"ZONE ENTRY AUTO START — LiveSplit auto-starts when {policy.AreaName} is entered.",
        _ => throw new InvalidOperationException("Unknown start policy.")
    };

    private List<int> GetConfiguredLevelMilestones()
    {
        var maxLevel = (int)_maxLevelNumeric.Value;
        var interval = (int)_levelIntervalNumeric.Value;
        var levels = new List<int>();
        for (var level = interval; level < maxLevel; level += interval)
        {
            if (level >= 2) levels.Add(level);
        }
        if (!levels.Contains(maxLevel)) levels.Add(maxLevel);
        return levels;
    }

    private void SyncLevelProgressionObjectives()
    {
        var firstLevelIndex = _customRoute.FindIndex(x => x.Type.Equals("level", StringComparison.OrdinalIgnoreCase));
        if (firstLevelIndex < 0) firstLevelIndex = _customRoute.Count;
        _customRoute.RemoveAll(x => x.Type.Equals("level", StringComparison.OrdinalIgnoreCase));

        if (_levelProgressionCheck.Checked)
        {
            var insertionIndex = Math.Min(firstLevelIndex, _customRoute.Count);
            foreach (var level in GetConfiguredLevelMilestones())
            {
                _customRoute.Insert(insertionIndex++, new RouteEntry
                {
                    Type = "level",
                    Id = level.ToString(),
                    Name = $"Level {level}",
                    Group = "Level progression"
                });
            }
        }

        RefreshRouteList();
    }

    private void ValidateLevelProgressionObjectives()
    {
        var levelEntries = _customRoute.Where(x => x.Type.Equals("level", StringComparison.OrdinalIgnoreCase)).ToList();
        if (!_levelProgressionCheck.Checked)
        {
            if (levelEntries.Count > 0)
                throw new InvalidOperationException("Level objectives are present even though Add level progression is disabled. Re-enable it or remove the generated level objectives.");
            return;
        }

        var expected = GetConfiguredLevelMilestones();
        var configured = new List<int>();
        foreach (var entry in levelEntries)
        {
            if (!int.TryParse(entry.Id, out var level) || level < 2 || level > 100)
                throw new InvalidOperationException($"Invalid level objective: {entry.Id}.");
            configured.Add(level);
        }

        if (configured.Count != expected.Count || !configured.OrderBy(x => x).SequenceEqual(expected))
            throw new InvalidOperationException("The level objectives no longer match the selected Max Level / Split Interval. Toggle Add level progression off and on to regenerate them.");

        var previous = 1;
        foreach (var level in configured)
        {
            if (level <= previous)
                throw new InvalidOperationException($"Level objectives must remain in ascending order. Level {level} appears after Level {previous}. Move the level milestones so they progress sequentially before generating the setup.");
            previous = level;
        }
    }

    private void AddSelectedAreas()
    {
        AddAreaEntries(_areaList.SelectedItems.Cast<RouteEntry>());
    }

    private void AddAllAreas()
    {
        // Add every currently visible area in the selected Content group/search.
        // This is intentionally scoped to the displayed catalog so a player can
        // quickly build a full Act/Interlude list and prune it in the route preview.
        AddAreaEntries(_areaList.Items.Cast<RouteEntry>());
    }

    private void AddAreaEntries(IEnumerable<RouteEntry> entries)
    {
        var additions = entries.ToList();
        if (additions.Count == 0) return;
        foreach (var entry in additions)
        {
            if (IsAreaAlreadySelected(entry)) continue;
            _customRoute.Add(entry);
        }
        RefreshRouteList();
        RefreshCustomCatalogs();
    }

    private void AddSelectedBosses()
    {
        AddBossRows(_bossGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(x => x.Index));
    }

    private void AddAllBosses()
    {
        // Respect the currently visible boss catalog and each row's occurrence value.
        // In unordered mode this adds every visible boss to the eligible encounter pool.
        AddBossRows(_bossGrid.Rows.Cast<DataGridViewRow>().OrderBy(x => x.Index));
    }

    private void AddBossRows(IEnumerable<DataGridViewRow> rows)
    {
        try
        {
            _bossGrid.EndEdit();
            var selectedRows = rows.ToList();
            if (selectedRows.Count == 0) return;

            // Validate every selected count before mutating the route so a bad row cannot
            // partially add the other selected bosses.
            var selections = new List<(RouteEntry Entry, int Count)>();
            foreach (var row in selectedRows)
            {
                if (row.Tag is not RouteEntry entry) continue;
                if (IsStandardBossAlreadySelected(entry)) continue;

                var count = 1;
                if (_orderedCheck.Checked && _multiBossCheck.Checked)
                {
                    var raw = row.Cells["Occurrences"].Value?.ToString();
                    if (!int.TryParse(raw, out count) || count < 1)
                        throw new InvalidOperationException($"Occurrences for {entry.Name} must be a positive integer.");
                }
                selections.Add((entry, count));
            }

            foreach (var selection in selections)
            {
                var entry = selection.Entry;
                if (!_orderedCheck.Checked)
                {
                    _customRoute.Add(new RouteEntry
                    {
                        Type = "bosspoolmember",
                        Id = entry.Id,
                        Name = entry.Name,
                        Group = entry.Group
                    });
                    continue;
                }

                if (!_multiBossCheck.Checked)
                {
                    _customRoute.Add(entry);
                    continue;
                }

                for (var occurrence = 1; occurrence <= selection.Count; occurrence++)
                {
                    _customRoute.Add(new RouteEntry
                    {
                        Type = "bossocc",
                        Id = $"{entry.Id}~{occurrence}",
                        Name = $"{entry.Name} — Kill {occurrence}",
                        Group = entry.Group
                    });
                }
            }

            RefreshRouteList();
            RefreshCustomCatalogs();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RefreshRouteList(int selectIndex = -1)
    {
        UpdateCustomBossModeUi();
        _routeList.BeginUpdate();
        _routeList.Items.Clear();
        for (var i = 0; i < _customRoute.Count; i++)
        {
            var entry = _customRoute[i];
            var typeLabel = entry.Type.ToLowerInvariant() switch
            {
                "area" or "areaocc" => "AREA",
                "level" => "LEVEL",
                "boss" or "bossocc" => "BOSS",
                "bossall" => "BOSS PAIR",
                "bossany" or "bossnth" => "DYNAMIC BOSS",
                "bosspoolmember" => "BOSS POOL",
                _ => entry.Type.ToUpperInvariant()
            };
            _routeList.Items.Add($"{i + 1:D3}  {typeLabel,-12}  {entry.Name}");
        }
        _routeList.EndUpdate();
        if (_routeList.Items.Count > 0 && selectIndex >= 0)
            _routeList.SelectedIndex = Math.Clamp(selectIndex, 0, _routeList.Items.Count - 1);
    }

    private void MoveRoute(int delta)
    {
        var i = _routeList.SelectedIndex;
        if (i < 0) return;
        var target = i + delta;
        if (target < 0 || target >= _customRoute.Count) return;
        (_customRoute[i], _customRoute[target]) = (_customRoute[target], _customRoute[i]);
        RefreshRouteList(target);
    }

    private void RemoveRoute()
    {
        var i = _routeList.SelectedIndex;
        if (i < 0) return;
        if (IsTrialBossRouteEntry(_customRoute[i]) && _trialBossObjectivesCheck.Checked)
        {
            for (var j = 0; j < _trialBossChecklist.Items.Count; j++)
            {
                if (_trialBossChecklist.Items[j] is RouteEntry trialEntry && SameRouteEntry(trialEntry, _customRoute[i]))
                {
                    _trialBossChecklist.SetItemChecked(j, false);
                    return;
                }
            }
        }
        if (_customRoute[i].Type.Equals("level", StringComparison.OrdinalIgnoreCase) && _levelProgressionCheck.Checked)
        {
            MessageBox.Show(this,
                "Level milestones are generated from Max Level and Split Interval. Change those settings or uncheck Add level progression to remove them.",
                "Level Progression", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _customRoute.RemoveAt(i);
        RefreshRouteList(Math.Min(i, _customRoute.Count - 1));
        RefreshCustomCatalogs();
    }

    private void ClearRoute()
    {
        _levelProgressionCheck.Checked = false;
        _trialBossObjectivesCheck.Checked = false;
        _customRoute.Clear();
        RefreshRouteList();
        RefreshCustomCatalogs();
    }

    private void OpenTargetFolder()
    {
        try
        {
            var target = Path.GetFullPath(_targetText.Text.Trim());
            Directory.CreateDirectory(target);
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private List<RouteEntry> GetUnorderedBossPoolEntries() =>
        _customRoute.Where(x => x.Type.Equals("bosspoolmember", StringComparison.OrdinalIgnoreCase)).ToList();

    private bool IsStandardDirectBossObjective(RouteEntry entry)
    {
        if (!(entry.Type.Equals("boss", StringComparison.OrdinalIgnoreCase)
              || entry.Type.Equals("bossocc", StringComparison.OrdinalIgnoreCase))) return false;
        if (IsTrialBossRouteEntry(entry)) return false;
        var baseId = BaseBossId(entry);
        return StandardBossCatalog().Any(x => x.Id.Equals(baseId, StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateCustomBossPolicy()
    {
        var pool = GetUnorderedBossPoolEntries();
        if (_orderedCheck.Checked)
        {
            if (pool.Count > 0)
                throw new InvalidOperationException("This route was switched to Ordered after adding an unordered boss pool. Remove those BOSS POOL entries and re-add the bosses in Ordered mode.");
            return;
        }

        if (_customRoute.Any(IsStandardDirectBossObjective))
            throw new InvalidOperationException("This route was switched to Dynamic / unordered after adding ordered boss objectives. Remove those boss rows and re-add the bosses so they become members of the unordered boss pool.");

        if (pool.Count > 0 && _unorderedBossTargetNumeric.Value < 1)
            throw new InvalidOperationException("Boss encounters required must be a positive integer.");
    }

    private List<RouteEntry> BuildCustomRuntimeObjectives()
    {
        var objectives = _customRoute
            .Where(x => !x.Type.Equals("bosspoolmember", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!_orderedCheck.Checked)
        {
            var pool = GetUnorderedBossPoolEntries();
            if (pool.Count > 0)
            {
                var target = (int)_unorderedBossTargetNumeric.Value;
                for (var i = 1; i <= target; i++)
                {
                    objectives.Add(new RouteEntry
                    {
                        Type = "bossslot",
                        Id = i.ToString(),
                        Name = $"Boss Encounter {i}",
                        Group = "Dynamic boss pool"
                    });
                }
            }
        }

        return objectives;
    }

    private void DeploySelected()
    {
        string? stage = null;
        try
        {
            var premade = _modeTabs.SelectedIndex == 0;
            var custom = _modeTabs.SelectedIndex == 1;
            var trials = _modeTabs.SelectedIndex == 2;
            if (premade)
            {
                if (_premadeSetupCombo.SelectedIndex < 0)
                    throw new InvalidOperationException("Select a premade setup first.");
                ValidatePremadeSelection();
            }
            else if (custom)
            {
                if (_customRoute.Count == 0)
                    throw new InvalidOperationException("Add at least one area, boss, or level objective to the custom route.");
                ValidateLevelProgressionObjectives();
                ValidateTrialBossObjectives();
                ValidateCustomBossPolicy();
            }
            else if (trials)
            {
                ValidateTrialSelection();
            }
            else
            {
                throw new InvalidOperationException("Select a setup tab before generating.");
            }

            var startPolicy = trials ? GetTrialStartPolicy() : GetRequiredStartPolicy();
            var target = ValidateTargetPath();
            stage = Path.Combine(Path.GetTempPath(), "PoE2RouteSetup", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);

            if (premade)
                DeployPremadeGenerated(stage, target, startPolicy);
            else if (custom)
                DeployCustom(stage, target, startPolicy);
            else
                DeployTrial(stage, target, startPolicy);

            if (!CommitStage(stage, target)) return;

            if (premade)
                SetStatus($"Deployed premade: {PremadeMode} / {PremadeSetup} / {(PremadeOrdered ? "Ordered" : "Dynamic")}");
            else if (custom)
                SetStatus($"Deployed custom route with {BuildCustomRuntimeObjectives().Count} timed objective(s).");
            else
                SetStatus($"Deployed trial run: {(_trialSekhemasRadio.Checked ? "Sekhemas" : "Chaos")} / {(_trialFinalBossRadio.Checked ? "Boss finish" : "Exit finish")}.");

            // The persistent LiveSplit reminder panel contains the required post-generation
            // instructions. Do not show a second success dialog after the target-clear
            // confirmation; it was redundant and interrupted the setup flow.
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
        }
        finally
        {
            if (stage is not null && Directory.Exists(stage))
            {
                try { Directory.Delete(stage, true); } catch { }
            }
        }
    }

    private void ValidateTrialSelection()
    {
        if (!_trialFullTimeRadio.Checked)
            throw new InvalidOperationException("Active Challenges Only is non-functional and cannot be generated yet. Select Full Trial.");
        if (_trialFinalBossRadio.Checked && _trialEveryChallengeRadio.Checked)
            throw new InvalidOperationException("Trial completion / exit only requires the Exit policy. Choose a boss split option or select Exit policy.");
        if (!_trialFinalOnlyRadio.Checked && !_trialMajorBossRadio.Checked && !_trialEveryChallengeRadio.Checked)
            throw new InvalidOperationException("Select a trial split frequency.");
    }

    private StartPolicy GetTrialStartPolicy()
    {
        return _trialSekhemasRadio.Checked
            ? new StartPolicy { Mode = StartMode.ZoneEntry, AreaId = "Sanctum_1_*", AreaName = "Trial of the Sekhemas - Floor 1" }
            : new StartPolicy { Mode = StartMode.ZoneEntry, AreaId = "G3_10", AreaName = "The Trial of Chaos - Active Trial" };
    }

    private List<RouteEntry> BuildTrialRunObjectives()
    {
        var objectives = new List<RouteEntry>();
        var eachBoss = _trialMajorBossRadio.Checked;
        var finalBossOnly = _trialFinalOnlyRadio.Checked;
        var finishOnExit = _trialExitRadio.Checked;

        if (_trialSekhemasRadio.Checked)
        {
            var floors = _sekhemasLengthCombo.SelectedIndex + 1;
            if (eachBoss)
            {
                if (floors >= 1) objectives.Add(new RouteEntry { Type = "boss", Id = "rattlecage_earthbreaker", Name = "Rattlecage, the Earthbreaker", Group = "Sekhemas" });
                if (floors >= 2)
                {
                    objectives.Add(new RouteEntry { Type = "bossany", Id = "sekhemas_floor2_boss_1", Name = "Sekhemas Floor 2 Boss 1", Group = "Sekhemas" });
                    objectives.Add(new RouteEntry { Type = "bossany", Id = "sekhemas_floor2_boss_2", Name = "Sekhemas Floor 2 Boss 2", Group = "Sekhemas" });
                }
                if (floors >= 3) objectives.Add(new RouteEntry { Type = "boss", Id = "ashar_sand_mother", Name = "Ashar, the Sand Mother", Group = "Sekhemas" });
                if (floors >= 4) objectives.Add(new RouteEntry { Type = "boss", Id = "zarokh_temporal", Name = "Zarokh, the Temporal", Group = "Sekhemas" });
            }
            else if (finalBossOnly)
            {
                objectives.Add(floors switch
                {
                    1 => new RouteEntry { Type = "boss", Id = "rattlecage_earthbreaker", Name = "Rattlecage, the Earthbreaker", Group = "Sekhemas" },
                    2 => new RouteEntry { Type = "bossall", Id = "sekhemas_floor2", Name = "Hadi + Rafiq", Group = "Sekhemas" },
                    3 => new RouteEntry { Type = "boss", Id = "ashar_sand_mother", Name = "Ashar, the Sand Mother", Group = "Sekhemas" },
                    _ => new RouteEntry { Type = "boss", Id = "zarokh_temporal", Name = "Zarokh, the Temporal", Group = "Sekhemas" }
                });
            }

            if (finishOnExit)
                objectives.Add(new RouteEntry { Type = "area", Id = "G2_13", Name = "Exit Trial of the Sekhemas", Group = "Trial Exit" });
        }
        else
        {
            var stages = GetChaosBossStageCount();
            if (eachBoss)
            {
                for (var i = 1; i <= stages; i++)
                    objectives.Add(new RouteEntry { Type = "bossany", Id = $"chaos_boss_{i}", Name = $"Chaos Boss {i}", Group = "Trial of Chaos" });
                if (_trialmasterCheck.Checked)
                    objectives.Add(new RouteEntry { Type = "boss", Id = "the_trialmaster", Name = "The Trialmaster", Group = "Trial of Chaos" });
            }
            else if (finalBossOnly)
            {
                objectives.Add(_trialmasterCheck.Checked
                    ? new RouteEntry { Type = "boss", Id = "the_trialmaster", Name = "The Trialmaster", Group = "Trial of Chaos" }
                    : new RouteEntry { Type = "bossnth", Id = $"chaos_final_{stages}", Name = $"Chaos Boss {stages}", Group = "Trial of Chaos" });
            }

            if (finishOnExit)
                objectives.Add(new RouteEntry { Type = "area", Id = "G3_10_Airlock", Name = "Exit Trial of Chaos", Group = "Trial Exit" });
        }

        if (objectives.Count == 0)
            throw new InvalidOperationException("The selected trial rules produced no detectable completion objective.");
        return objectives;
    }

    private void DeployTrial(string stage, string target, StartPolicy startPolicy)
    {
        var objectives = BuildTrialRunObjectives();
        var routePath = Path.Combine(stage, "poe2_mixed_route.txt");
        var route = new StringBuilder();
        route.AppendLine("# Generated dedicated trial route");
        route.AppendLine($"@start={startPolicy.RouteDirectiveValue}");
        route.AppendLine("@order=ordered");
        route.AppendLine("@areaCompletion=entry");
        route.AppendLine();
        foreach (var entry in objectives)
            route.AppendLine($"{entry.RouteText,-42} # {entry.Name}");
        File.WriteAllText(routePath, route.ToString(), new UTF8Encoding(false));

        var sourceAsl = Resolve(_manifest.CustomAslSource);
        if (!File.Exists(sourceAsl)) throw new FileNotFoundException("Trial autosplitter source was not found.", sourceAsl);
        var stagedAsl = Path.Combine(stage, "PoE2-Trial.asl");
        var targetAsl = Path.Combine(target, "PoE2-Trial.asl");
        var sourceAslText = File.ReadAllText(sourceAsl);
        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(sourceAslText, target);
        patchedAsl = LiveSplitFiles.ApplyAutoStartOption(patchedAsl, true);
        patchedAsl = LiveSplitFiles.ApplyGameTimeOptions(patchedAsl, _excludeManualPauseCheck.Checked);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        var trialName = _trialSekhemasRadio.Checked ? "Trial of the Sekhemas" : "Trial of Chaos";
        var lengthName = _trialSekhemasRadio.Checked
            ? $"{_sekhemasLengthCombo.SelectedIndex + 1} Floor{(_sekhemasLengthCombo.SelectedIndex == 0 ? "" : "s")}" 
            : (_chaosLengthCombo.SelectedIndex switch { 1 => "7 Rounds", 2 => "10 Rounds", _ => "4 Rounds" }) + (_trialmasterCheck.Checked ? " + Trialmaster" : "");
        var finishName = _trialFinalBossRadio.Checked ? "Boss Finish" : "Exit Finish";
        var stagedLss = Path.Combine(stage, "Trial-Run.lss");
        LiveSplitFiles.WritePremadeSplits(stagedLss, objectives, $"{trialName} - {lengthName} - {finishName}");

        var needsWatcher = objectives.Any(x => x.Type.StartsWith("boss", StringComparison.OrdinalIgnoreCase));
        if (needsWatcher) EnsureBossEventFile(stage);
        if (_excludeManualPauseCheck.Checked) EnsureManualPauseStateFile(stage);

        WriteSetupSummary(stage, "Dedicated Trial Run", $"{trialName}; {lengthName}; {finishName}; {objectives.Count} split(s)",
            stagedLss, stagedAsl, targetAsl, needsWatcher, _excludeManualPauseCheck.Checked, startPolicy);
        WriteTrialRulesSummary(stage, objectives, needsWatcher);
    }

    private void WriteTrialRulesSummary(string stage, IReadOnlyList<RouteEntry> objectives, bool needsWatcher)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PoE2 Route AutoSplitter - Dedicated Trial Rules");
        sb.AppendLine($"Trial: {(_trialSekhemasRadio.Checked ? "Trial of the Sekhemas" : "Trial of Chaos")}");
        sb.AppendLine($"Length: {(_trialSekhemasRadio.Checked ? (_sekhemasLengthCombo.SelectedIndex + 1) + " floor(s)" : (_chaosLengthCombo.SelectedIndex switch { 1 => "7 rounds", 2 => "10 rounds", _ => "4 rounds" }) + (_trialmasterCheck.Checked ? " + Trialmaster" : ""))}");
        sb.AppendLine("Timing: Full Trial; all player-controlled time counts. Active Challenges Only is non-functional.");
        sb.AppendLine($"Start: automatic on {(_trialSekhemasRadio.Checked ? "first Sanctum_1_* entry" : "G3_10 entry")}");
        sb.AppendLine($"Finish: {(_trialFinalBossRadio.Checked ? "Boss policy" : "Exit policy")}");
        sb.AppendLine($"Splits: {(_trialMajorBossRadio.Checked ? "Each boss kill" : _trialFinalOnlyRadio.Checked ? "Final boss only" : "Trial completion / exit only")}");
        sb.AppendLine($"BossWatcher: {(needsWatcher ? "required" : "not required")}");
        sb.AppendLine();
        sb.AppendLine("Generated split objectives:");
        for (var i = 0; i < objectives.Count; i++)
            sb.AppendLine($"{i + 1:D2}. {objectives[i].Name} [{objectives[i].RouteText}]");
        File.WriteAllText(Path.Combine(stage, "TRIAL_RULES.txt"), sb.ToString(), new UTF8Encoding(false));
    }

    private void ValidatePremadeSelection()
    {
        if (PremadeMode == "Area + Boss Completion" && PremadeOrdered)
            throw new InvalidOperationException("Ordered Area + Boss Completion is not yet available because the premade mixed route does not have a validated interleaved area/boss sequence. Choose Dynamic / unordered.");
        if (PremadeIsCombination && _premadeCombinationList.CheckedItems.Count == 0)
            throw new InvalidOperationException("Select at least one Act or Interlude for the Combination setup.");
        if (_premadeSekhemasCheck.Checked && !_premadeSekhemasFloor1.Checked)
            throw new InvalidOperationException("Sekhemas is enabled but no floor is selected.");
        if (_premadeChaosCheck.Checked && !_premadeChaosBoss1.Checked)
            throw new InvalidOperationException("Trial of Chaos is enabled but no trial stage is selected.");
    }

    private void DeployPremadeGenerated(string stage, string target, StartPolicy startPolicy)
    {
        var objectives = BuildPremadeObjectivesWithTrials();
        if (objectives.Count == 0)
            throw new InvalidOperationException("The selected premade configuration produced no objectives.");

        // Dynamic area runs treat an automatic start-zone objective as satisfied by
        // the start itself. Ordered successor-entry runs keep it because that row is
        // completed on the configured successor entry instead of at time zero.
        if (!PremadeOrdered && startPolicy.IsAutomatic && startPolicy.AreaId is not null)
            objectives.RemoveAll(x => x.Type.Equals("area", StringComparison.OrdinalIgnoreCase)
                && x.Id.Equals(startPolicy.AreaId, StringComparison.OrdinalIgnoreCase));

        var routePath = Path.Combine(stage, "poe2_mixed_route.txt");
        var route = new StringBuilder();
        route.AppendLine("# Generated premade route — PoE2 Route AutoSplitter Setup UI");
        route.AppendLine($"@start={startPolicy.RouteDirectiveValue}");
        route.AppendLine($"@order={(PremadeOrdered ? "ordered" : "unordered")}");
        route.AppendLine($"@areaCompletion={(PremadeOrdered && PremadeHasAreas ? "successor" : "entry")}");
        route.AppendLine();
        foreach (var entry in objectives)
            route.AppendLine($"{entry.RouteText,-42} # {entry.Name}");
        File.WriteAllText(routePath, route.ToString(), new UTF8Encoding(false));

        var sourceAsl = Resolve(_manifest.CustomAslSource);
        if (!File.Exists(sourceAsl)) throw new FileNotFoundException("Premade generator ASL source was not found.", sourceAsl);
        var stagedAsl = Path.Combine(stage, "PoE2-Premade.asl");
        var targetAsl = Path.Combine(target, "PoE2-Premade.asl");
        var sourceAslText = File.ReadAllText(sourceAsl);
        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(sourceAslText, target);
        patchedAsl = LiveSplitFiles.ApplyAutoStartOption(patchedAsl, startPolicy.IsAutomatic);
        patchedAsl = LiveSplitFiles.ApplyGameTimeOptions(patchedAsl, _excludeManualPauseCheck.Checked);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        var stagedLss = Path.Combine(stage, "Premade-Route.lss");
        LiveSplitFiles.WritePremadeSplits(stagedLss, objectives, $"{PremadeMode} - {PremadeSetup} - {(PremadeOrdered ? "Ordered" : "Dynamic")}");

        var needsWatcher = objectives.Any(x => x.Type.StartsWith("boss", StringComparison.OrdinalIgnoreCase));
        if (needsWatcher) EnsureBossEventFile(stage);
        if (_excludeManualPauseCheck.Checked) EnsureManualPauseStateFile(stage);

        var setupName = $"{PremadeSetup}; {(PremadeOrdered ? "ordered" : "dynamic")}; {objectives.Count} objectives";
        WriteSetupSummary(stage, "Premade - " + PremadeMode, setupName, stagedLss, stagedAsl, targetAsl,
            needsWatcher, _excludeManualPauseCheck.Checked, startPolicy);
        WritePremadeObjectiveSummary(stage, objectives);
    }

    private void WritePremadeObjectiveSummary(string stage, IReadOnlyList<RouteEntry> objectives)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PoE2 Route AutoSplitter - Generated Premade Objectives");
        sb.AppendLine($"Mode: {PremadeMode}");
        sb.AppendLine($"Setup: {PremadeSetup}");
        sb.AppendLine($"Order: {(PremadeOrdered ? "Ordered" : "Dynamic / unordered")}");
        sb.AppendLine($"Sekhemas: {(_premadeSekhemasCheck.Checked ? "included" : "not included")}");
        sb.AppendLine($"Chaos: {(_premadeChaosCheck.Checked ? "included" : "not included")}");
        sb.AppendLine();
        for (var i = 0; i < objectives.Count; i++)
            sb.AppendLine($"{i + 1:D3}. {objectives[i].Type,-7} {objectives[i].Name} [{objectives[i].Id}]");
        File.WriteAllText(Path.Combine(stage, "PREMADE_OBJECTIVES.txt"), sb.ToString(), new UTF8Encoding(false));
    }

    private void DeployPreset(PresetDefinition preset, string stage, string target, StartPolicy startPolicy)
    {
        var sourceLss = Resolve(preset.LssSource);
        var sourceAsl = Resolve(preset.AslSource);
        if (!File.Exists(sourceLss)) throw new FileNotFoundException("Preset splits file was not found.", sourceLss);
        if (!File.Exists(sourceAsl)) throw new FileNotFoundException("Preset autosplitter file was not found.", sourceAsl);

        var sourceAslText = File.ReadAllText(sourceAsl);
        var runtimeStartSupported = LiveSplitFiles.SupportsRuntimeStartPolicy(sourceAslText);
        var stagedLss = Path.Combine(stage, Path.GetFileName(sourceLss));
        var stagedAsl = Path.Combine(stage, Path.GetFileName(sourceAsl));
        var targetAsl = Path.Combine(target, Path.GetFileName(sourceAsl));
        string? areaChecklistRuntimeText = null;

        foreach (var runtime in preset.RuntimeFiles)
        {
            var runtimeSource = Resolve(runtime.Source);
            if (!File.Exists(runtimeSource)) throw new FileNotFoundException("Preset runtime file was not found.", runtimeSource);

            var runtimeTarget = Path.Combine(stage, runtime.Target);
            var runtimeText = File.ReadAllText(runtimeSource);
            if (startPolicy.Mode == StartMode.Riverbank && preset.PrependRiverbankObjective)
                runtimeText = LiveSplitFiles.PrependRiverbankRouteEntry(runtimeText);
            runtimeText = LiveSplitFiles.ApplyRouteStartPolicy(runtimeText, startPolicy);
            File.WriteAllText(runtimeTarget, runtimeText, new UTF8Encoding(false));

            if (Path.GetFileName(sourceAsl).Contains("AreaChecklistAutosplitter", StringComparison.OrdinalIgnoreCase))
                areaChecklistRuntimeText = runtimeText;
        }

        LiveSplitFiles.WritePresetSplits(
            sourceLss,
            stagedLss,
            startPolicy.Mode == StartMode.Riverbank && preset.PrependRiverbankObjective);
        if (areaChecklistRuntimeText is not null)
            LiveSplitFiles.AdjustAreaChecklistSplits(stagedLss, areaChecklistRuntimeText);

        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(sourceAslText, target);
        patchedAsl = runtimeStartSupported
            ? LiveSplitFiles.ApplyAutoStartOption(patchedAsl, startPolicy.IsAutomatic)
            : LiveSplitFiles.ApplyGeneratedZoneStartPolicy(patchedAsl, startPolicy);
        patchedAsl = LiveSplitFiles.ApplyGameTimeOptions(patchedAsl, _excludeManualPauseCheck.Checked);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        if (preset.RequiresBossWatcher) EnsureBossEventFile(stage);
        if (_excludeManualPauseCheck.Checked) EnsureManualPauseStateFile(stage);

        WriteSetupSummary(stage, preset.Group, preset.DisplayName, stagedLss, stagedAsl, targetAsl,
            preset.RequiresBossWatcher, _excludeManualPauseCheck.Checked, startPolicy);
    }

    private void DeployCustom(string stage, string target, StartPolicy startPolicy)
    {
        var runtimeObjectives = BuildCustomRuntimeObjectives();
        if (runtimeObjectives.Count == 0)
            throw new InvalidOperationException("The custom route does not contain any timed objectives.");

        var bossPool = GetUnorderedBossPoolEntries();
        var routePath = Path.Combine(stage, "poe2_mixed_route.txt");
        var route = new StringBuilder();
        route.AppendLine("# Generated by PoE2 Route AutoSplitter Setup UI");
        route.AppendLine($"@start={startPolicy.RouteDirectiveValue}");
        route.AppendLine($"@order={(_orderedCheck.Checked ? "ordered" : "unordered")}");
        if (!_orderedCheck.Checked && bossPool.Count > 0)
        {
            route.AppendLine($"@bossPool={string.Join(";", bossPool.Select(x => x.Id))}");
            route.AppendLine($"@bossTarget={(int)_unorderedBossTargetNumeric.Value}");
        }
        route.AppendLine();
        foreach (var entry in runtimeObjectives)
            route.AppendLine($"{entry.RouteText,-42} # {entry.Name}");
        File.WriteAllText(routePath, route.ToString(), new UTF8Encoding(false));

        var sourceAsl = Resolve(_manifest.CustomAslSource);
        var stagedAsl = Path.Combine(stage, "PoE2-Custom.asl");
        var targetAsl = Path.Combine(target, "PoE2-Custom.asl");
        var sourceAslText = File.ReadAllText(sourceAsl);
        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(sourceAslText, target);
        patchedAsl = LiveSplitFiles.ApplyAutoStartOption(patchedAsl, startPolicy.IsAutomatic);
        patchedAsl = LiveSplitFiles.ApplyGameTimeOptions(patchedAsl, _excludeManualPauseCheck.Checked);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        var stagedLss = Path.Combine(stage, "Custom-Route.lss");
        LiveSplitFiles.WriteCustomSplits(stagedLss, runtimeObjectives);

        var needsWatcher = runtimeObjectives.Any(x => x.Type.StartsWith("boss", StringComparison.OrdinalIgnoreCase));
        if (needsWatcher) EnsureBossEventFile(stage);
        if (_excludeManualPauseCheck.Checked) EnsureManualPauseStateFile(stage);
        var poolSuffix = !_orderedCheck.Checked && bossPool.Count > 0
            ? $"; boss pool={bossPool.Count} identities / {(int)_unorderedBossTargetNumeric.Value} encounters"
            : "";
        WriteSetupSummary(stage, "Custom Route", $"{runtimeObjectives.Count} timed objectives; {(_orderedCheck.Checked ? "ordered" : "unordered")}{poolSuffix}",
            stagedLss, stagedAsl, targetAsl, needsWatcher, _excludeManualPauseCheck.Checked, startPolicy);
        WriteCustomObjectiveSummary(stage);
    }

    private string ValidateTargetPath()
    {
        if (string.IsNullOrWhiteSpace(_targetText.Text)) throw new InvalidOperationException("Choose a target directory.");
        var target = Path.GetFullPath(_targetText.Text.Trim());
        var expectedTarget = Path.GetFullPath(Path.Combine(_userRoot, "LiveSplit Target"));
        if (!string.Equals(NormalizeDirectory(target), NormalizeDirectory(expectedTarget), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The Setup UI only deploys to its dedicated LiveSplit Target directory.");
        var root = Path.GetPathRoot(target);
        if (string.Equals(target.TrimEnd(Path.DirectorySeparatorChar), root?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A drive root cannot be used as the target directory.");

        var package = Path.GetFullPath(_packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedTarget = NormalizeDirectory(target);
        if (string.Equals(normalizedTarget, package, StringComparison.OrdinalIgnoreCase) || package.StartsWith(normalizedTarget + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The target cannot be the release package directory or one of its parent directories.");

        var protectedFolders = new (string Path, string Label)[]
        {
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "user profile"),
            (Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Desktop"),
            (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Documents"),
            (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Roaming AppData"),
            (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Local AppData"),
            (Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ProgramData"),
            (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Program Files"),
            (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Program Files (x86)"),
            (Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Windows"),
            (Environment.GetFolderPath(Environment.SpecialFolder.System), "Windows System"),
            (Path.GetTempPath(), "system temporary directory")
        };

        foreach (var protectedFolder in protectedFolders)
        {
            if (string.IsNullOrWhiteSpace(protectedFolder.Path)) continue;
            var protectedPath = NormalizeDirectory(Path.GetFullPath(protectedFolder.Path));
            if (string.Equals(normalizedTarget, protectedPath, StringComparison.OrdinalIgnoreCase) ||
                protectedPath.StartsWith(normalizedTarget + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The {protectedFolder.Label} itself (or one of its parent directories) cannot be used as a disposable target. Choose a dedicated subfolder instead.");
        }

        return target;
    }

    private static string NormalizeDirectory(string path)
        => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private bool CommitStage(string stage, string target)
    {
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
        {
            var result = MessageBox.Show(this,
                $"All files and folders currently inside this target will be deleted:\n\n{target}\n\nContinue?",
                "Clear Target Directory", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return false;
        }

        if (Directory.Exists(target)) Directory.Delete(target, true);
        Directory.CreateDirectory(target);
        CopyDirectory(stage, target);
        _targetText.Text = target;
        return true;
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);
        }
    }

    private void WriteCustomObjectiveSummary(string stage)
    {
        var text = new StringBuilder();
        text.AppendLine("Custom objectives");
        text.AppendLine($"Order: {(_orderedCheck.Checked ? "ordered" : "unordered")}");
        var bossPool = GetUnorderedBossPoolEntries();
        if (!_orderedCheck.Checked && bossPool.Count > 0)
        {
            text.AppendLine($"Boss encounters required: {(int)_unorderedBossTargetNumeric.Value}");
            text.AppendLine("Eligible boss pool:");
            foreach (var member in bossPool.OrderBy(x => x.Name))
                text.AppendLine($"  - {member.Name} | {member.Id}");
        }
        text.AppendLine();
        var runtimeObjectives = BuildCustomRuntimeObjectives();
        for (var i = 0; i < runtimeObjectives.Count; i++)
            text.AppendLine($"{i + 1:D3}. {runtimeObjectives[i].Type.ToUpperInvariant()} | {runtimeObjectives[i].Name} | {runtimeObjectives[i].Id}");
        File.WriteAllText(Path.Combine(stage, "CUSTOM_OBJECTIVES.txt"), text.ToString(), new UTF8Encoding(false));
    }

    private void StartBossWatcher()
    {
        try
        {
            var target = Path.GetFullPath(_targetText.Text.Trim());
            if (!Directory.Exists(target) || !File.Exists(Path.Combine(target, "SETUP_INFO.txt")))
                throw new InvalidOperationException("Deploy a setup first so the target directory contains an active generated setup.");

            if (Process.GetProcessesByName("PoE2BossWatcher").Any(p => !p.HasExited))
            {
                MessageBox.Show(this, "BossWatcher is already running. Close the existing BossWatcher before starting another instance.",
                    "PoE2 AutoSplitter Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var watcherRoot = Resolve(_manifest.BossWatcherDirectory);
            var candidates = new[]
            {
                Path.Combine(watcherRoot, "publish", "PoE2BossWatcher.exe"),
                Path.Combine(watcherRoot, "PoE2BossWatcher.exe")
            };
            var exe = candidates.FirstOrDefault(File.Exists);
            if (exe is null)
                throw new FileNotFoundException("PoE2BossWatcher.exe was not found. In 2 - Support Files\\BossWatcher, run Setup-OCR.ps1 and then Build.ps1.");

            var eventPath = Path.Combine(target, "poe2_boss_events.log");
            EnsureBossEventFile(target);
            var args = $"--event-file {QuoteArgument(eventPath)}" + (_devConsoleCheck.Checked ? " --dev-console" : "");
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = true
            });
            SetStatus($"BossWatcher started in {(_devConsoleCheck.Checked ? "developer" : "user")} console mode.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void StartGameTimeWatcher()
    {
        try
        {
            if (!_excludeManualPauseCheck.Checked)
                throw new InvalidOperationException("Enable the manual-pause Game Time option and deploy the setup before starting GameTimeWatcher.");

            var target = Path.GetFullPath(_targetText.Text.Trim());
            if (!Directory.Exists(target) || !File.Exists(Path.Combine(target, "SETUP_INFO.txt")))
                throw new InvalidOperationException("Deploy a setup first so the target directory contains an active generated setup.");

            var existingWatchers = Process.GetProcessesByName("PoE2GameTimeWatcher");
            try
            {
                if (existingWatchers.Any(p => !p.HasExited))
                {
                    MessageBox.Show(this, "GameTimeWatcher is already running. Close the existing GameTimeWatcher before starting another instance.",
                        "PoE2 AutoSplitter Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            finally
            {
                foreach (var process in existingWatchers) process.Dispose();
            }

            var watcherRoot = Resolve(_manifest.GameTimeWatcherDirectory);
            var candidates = new[]
            {
                Path.Combine(watcherRoot, "publish", "PoE2GameTimeWatcher.exe"),
                Path.Combine(watcherRoot, "PoE2GameTimeWatcher.exe")
            };
            var exe = candidates.FirstOrDefault(File.Exists);
            if (exe is null)
                throw new FileNotFoundException("PoE2GameTimeWatcher.exe was not found. In 2 - Support Files\\GameTimeWatcher, run Build.ps1.");

            var statePath = Path.Combine(target, "poe2_manual_pause_state.txt");
            EnsureManualPauseStateFile(target);

            if (_devConsoleCheck.Checked)
            {
                var diagnosticScript = Path.Combine(watcherRoot, "Run-Diagnostic.ps1");
                if (!File.Exists(diagnosticScript))
                    throw new FileNotFoundException("Run-Diagnostic.ps1 was not found in the GameTimeWatcher support folder.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File {QuoteArgument(diagnosticScript)} -StateFile {QuoteArgument(statePath)}",
                    WorkingDirectory = watcherRoot,
                    UseShellExecute = true
                });
                SetStatus("GameTimeWatcher external crash diagnostic started. Results will be saved under the GameTimeWatcher diagnostics folder.");
                return;
            }

            var args = $"--state-file {QuoteArgument(statePath)} --wait-on-error";
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = true
            });
            SetStatus("GameTimeWatcher started in user console mode.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private static void EnsureBossEventFile(string target)
    {
        var path = Path.Combine(target, "poe2_boss_events.log");
        if (!File.Exists(path)) File.WriteAllText(path, "");
    }

    private static void EnsureManualPauseStateFile(string target)
    {
        var path = Path.Combine(target, "poe2_manual_pause_state.txt");
        if (!File.Exists(path))
        {
            var initial = "version=1\r\n" +
                          "state=RUNNING\r\n" +
                          "reason=watcher-not-started\r\n" +
                          "heartbeatUtcTicks=0\r\n" +
                          "pauseMenuScore=0.0000\r\n" +
                          "mtxShopScore=0.0000\r\n";
            File.WriteAllText(path, initial, new UTF8Encoding(false));
        }
    }

    private void WriteSetupSummary(string outputDirectory, string group, string setup, string lss, string asl, string deployedAslPath, bool bossWatcher, bool manualPauseRemoval, StartPolicy startPolicy)
    {
        var step = 4;
        var text = new StringBuilder()
            .AppendLine($"PoE2 Route AutoSplitter v{_manifest.Version}")
            .AppendLine($"Mode: {group}")
            .AppendLine($"Setup: {setup}")
            .AppendLine($"Splits (.lss): {Path.GetFileName(lss)}")
            .AppendLine($"AutoSplitter (.asl): {Path.GetFileName(asl)}")
            .AppendLine($"AutoSplitter full path: {Path.GetFullPath(deployedAslPath)}")
            .AppendLine("Layout (.lsl): Not generated by design")
            .AppendLine("Game Time load removal: Enabled by default (Client.txt authoritative loading-screen durations)")
            .AppendLine("Start policy: " + DescribeStartPolicy(startPolicy))
            .AppendLine($"Manual pause exclusion: {(manualPauseRemoval ? "Enabled" : "Disabled")}")
            .AppendLine($"BossWatcher required: {(bossWatcher ? "Yes" : "No")}")
            .AppendLine($"GameTimeWatcher required: {(manualPauseRemoval ? "Yes" : "No")}")
            .AppendLine()
            .AppendLine("LiveSplit setup:")
            .AppendLine("1. Open the generated .lss splits file.")
            .AppendLine("2. Keep your own LiveSplit layout.")
            .AppendLine("3. IMPORTANT: Add/edit the Scriptable Auto Splitter component in that layout and browse to the AutoSplitter full path above.")
            .AppendLine("   LiveSplit does not automatically follow a new dev package folder; an old component path will continue running the old ASL.");

        if (bossWatcher)
            text.AppendLine($"{step++}. Start BossWatcher from the Setup UI before/during the run.");
        if (manualPauseRemoval)
            text.AppendLine($"{step++}. Start GameTimeWatcher from the Setup UI if manual pauses should stop Game Time.");

        text.AppendLine();
        text.AppendLine("For load-removed timing, configure LiveSplit to display/compare against Game Time.");
        text.AppendLine("The Setup UI intentionally does not generate .lsl files because LiveSplit layout files are user-specific.");

        File.WriteAllText(Path.Combine(outputDirectory, "SETUP_INFO.txt"), text.ToString(), new UTF8Encoding(false));
    }

    private string Resolve(string relative) => Path.GetFullPath(Path.Combine(_packageRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    private void SetStatus(string text) => _status.Text = text;
    private void ShowError(string text) => MessageBox.Show(this, text, "PoE2 AutoSplitter Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
