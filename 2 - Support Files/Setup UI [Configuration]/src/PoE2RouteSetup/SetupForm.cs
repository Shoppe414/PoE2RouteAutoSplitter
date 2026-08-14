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
    private readonly ListView _presetList = new();
    private readonly Label _presetDescription = new();
    private readonly TextBox _areaSearch = new();
    private readonly TextBox _bossSearch = new();
    private readonly ListBox _areaList = new();
    private readonly ListBox _bossList = new();
    private readonly ListBox _routeList = new();
    private readonly CheckBox _orderedCheck = new();
    private readonly RadioButton _manualStartRadio = new();
    private readonly RadioButton _riverbankStartRadio = new();
    private readonly RadioButton _zoneStartRadio = new();
    private readonly ComboBox _startZoneCombo = new();
    private readonly CheckBox _excludeManualPauseCheck = new();
    private readonly CheckBox _devConsoleCheck = new();
    private readonly Button _gameTimeWatcherButton = new();
    private readonly Label _status = new();

    public SetupForm()
    {
        var located = PackageData.LocatePackage();
        _packageRoot = located.PackageRoot;
        _userRoot = located.UserRoot;
        _manifest = SetupManifest.Load(located.ManifestPath);
        _areas = PackageData.LoadAreas(Resolve(_manifest.AreaCatalog));
        _bosses = PackageData.LoadBosses(Resolve(_manifest.BossCatalog), Resolve(_manifest.BossSupportOnlyList));

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
        _modeTabs.TabPages.Add(premade);
        _modeTabs.TabPages.Add(custom);
        return _modeTabs;
    }

    private Control BuildPremadePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _presetList.Dock = DockStyle.Fill;
        _presetList.View = View.Details;
        _presetList.FullRowSelect = true;
        _presetList.HideSelection = false;
        _presetList.MultiSelect = false;
        _presetList.Columns.Add("Mode", 300);
        _presetList.Columns.Add("Setup", 650);
        _presetList.SelectedIndexChanged += (_, _) => UpdatePresetDescription();
        panel.Controls.Add(_presetList, 0, 0);

        _presetDescription.AutoSize = true;
        _presetDescription.MaximumSize = new Size(1000, 0);
        _presetDescription.Padding = new Padding(2, 8, 2, 4);
        panel.Controls.Add(_presetDescription, 0, 1);
        return panel;
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
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 4) };
        tabs.TabPages.Add(BuildObjectiveTab("Areas", _areaSearch, _areaList, () => AddSelected(_areaList)));
        tabs.TabPages.Add(BuildObjectiveTab("Bosses", _bossSearch, _bossList, () => AddSelected(_bossList)));
        _areaSearch.TextChanged += (_, _) => RefreshAvailableList(_areaList, _areas, _areaSearch.Text);
        _bossSearch.TextChanged += (_, _) => RefreshAvailableList(_bossList, _bosses, _bossSearch.Text);
        return tabs;
    }

    private TabPage BuildObjectiveTab(string title, TextBox search, ListBox list, Action addAction)
    {
        var tab = new TabPage(title);
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        search.Dock = DockStyle.Top;
        search.PlaceholderText = $"Search {title.ToLowerInvariant()} by name or ID…";
        list.Dock = DockStyle.Fill;
        list.SelectionMode = SelectionMode.MultiExtended;
        list.DoubleClick += (_, _) => addAction();
        var add = new Button { Text = $"Add Selected {title}", Dock = DockStyle.Fill, Height = 34 };
        add.Click += (_, _) => addAction();
        panel.Controls.Add(search, 0, 0);
        panel.Controls.Add(list, 0, 1);
        panel.Controls.Add(add, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private Control BuildRoutePanel()
    {
        var group = new GroupBox { Text = "Custom route", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _orderedCheck.Text = "Ordered route (otherwise objectives may complete in any order)";
        _orderedCheck.AutoSize = true;
        panel.Controls.Add(_orderedCheck, 0, 0);

        var startNote = new Label
        {
            Text = "Timer start is selected below. Riverbank uses the fresh-character Wounded Man gate; Zone Entry starts when the selected non-Riverbank zone is entered; Manual requires you to start LiveSplit yourself.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            Padding = new Padding(0, 4, 0, 4)
        };
        panel.Controls.Add(startNote, 0, 1);

        _routeList.Dock = DockStyle.Fill;
        panel.Controls.Add(_routeList, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        buttons.Controls.Add(MakeButton("Move Up", () => MoveRoute(-1)));
        buttons.Controls.Add(MakeButton("Move Down", () => MoveRoute(1)));
        buttons.Controls.Add(MakeButton("Remove", RemoveRoute));
        buttons.Controls.Add(MakeButton("Clear", ClearRoute));
        panel.Controls.Add(buttons, 0, 3);
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

        var deploy = new Button { Text = "Generate / Deploy Selected Setup", AutoSize = true, Height = 38, Padding = new Padding(12, 2, 12, 2) };
        deploy.Click += (_, _) => DeploySelected();
        panel.Controls.Add(deploy, 0, 0);

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

    private void PopulatePresets()
    {
        foreach (var preset in _manifest.Presets)
        {
            var item = new ListViewItem(preset.Group);
            item.SubItems.Add(preset.DisplayName);
            item.Tag = preset;
            _presetList.Items.Add(item);
        }
        if (_presetList.Items.Count > 0) _presetList.Items[0].Selected = true;
    }

    private void PopulateCustomCatalogs()
    {
        RefreshAvailableList(_areaList, _areas, "");
        RefreshAvailableList(_bossList, _bosses, "");
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
        var preset = SelectedPreset();
        _presetDescription.Text = preset is null
            ? "Select a setup."
            : $"{preset.Description}  BossWatcher: {(preset.RequiresBossWatcher ? "required" : "not required")}.";
        UpdateStartZoneEnabled();
    }

    private void UpdateStartZoneEnabled()
    {
        _startZoneCombo.Enabled = _zoneStartRadio.Checked;
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
        StartMode.ZoneEntry => $"ZONE ENTRY AUTO START — LiveSplit auto-starts when {policy.AreaName} [{policy.AreaId}] is entered.",
        _ => throw new InvalidOperationException("Unknown start policy.")
    };

    private PresetDefinition? SelectedPreset() => _presetList.SelectedItems.Count == 0 ? null : _presetList.SelectedItems[0].Tag as PresetDefinition;

    private void AddSelected(ListBox list)
    {
        var additions = list.SelectedItems.Cast<RouteEntry>().ToList();
        if (additions.Count == 0) return;
        foreach (var entry in additions)
        {
            if (_customRoute.Any(x => x.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase) && x.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))) continue;
            _customRoute.Add(entry);
        }
        RefreshRouteList();
    }

    private void RefreshRouteList(int selectIndex = -1)
    {
        _routeList.BeginUpdate();
        _routeList.Items.Clear();
        for (var i = 0; i < _customRoute.Count; i++)
            _routeList.Items.Add($"{i + 1:D3}  {_customRoute[i].Type.ToUpperInvariant(),-4}  {_customRoute[i].Name}  [{_customRoute[i].Id}]");
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
        _customRoute.RemoveAt(i);
        RefreshRouteList(Math.Min(i, _customRoute.Count - 1));
    }

    private void ClearRoute()
    {
        _customRoute.Clear();
        RefreshRouteList();
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

    private void DeploySelected()
    {
        string? stage = null;
        try
        {
            PresetDefinition? preset = null;
            if (_modeTabs.SelectedIndex == 0)
                preset = SelectedPreset() ?? throw new InvalidOperationException("Select a premade setup first.");
            else if (_customRoute.Count == 0)
                throw new InvalidOperationException("Add at least one area or boss to the custom route.");

            // Timer Start is a required setup field. Radio buttons make the choices
            // mutually exclusive; this validation also protects programmatic/invalid UI state.
            var startPolicy = GetRequiredStartPolicy();
            var target = ValidateTargetPath();
            stage = Path.Combine(Path.GetTempPath(), "PoE2RouteSetup", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);

            if (preset is not null)
                DeployPreset(preset, stage, target, startPolicy);
            else
                DeployCustom(stage, target, startPolicy);

            if (!CommitStage(stage, target)) return;

            if (preset is not null)
                SetStatus($"Deployed: {preset.Group} / {preset.DisplayName}");
            else
                SetStatus($"Deployed custom route with {_customRoute.Count} objective(s).");

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
        var routePath = Path.Combine(stage, "poe2_mixed_route.txt");
        var route = new StringBuilder();
        route.AppendLine("# Generated by PoE2 Route AutoSplitter Setup UI");
        route.AppendLine($"@start={startPolicy.RouteDirectiveValue}");
        route.AppendLine($"@order={(_orderedCheck.Checked ? "ordered" : "unordered")}");
        route.AppendLine();
        foreach (var entry in _customRoute)
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
        LiveSplitFiles.WriteCustomSplits(stagedLss, _customRoute);

        var needsWatcher = _customRoute.Any(x => x.Type.Equals("boss", StringComparison.OrdinalIgnoreCase));
        if (needsWatcher) EnsureBossEventFile(stage);
        if (_excludeManualPauseCheck.Checked) EnsureManualPauseStateFile(stage);
        WriteSetupSummary(stage, "Custom Route", $"{_customRoute.Count} objectives; {(_orderedCheck.Checked ? "ordered" : "unordered")}",
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
        text.AppendLine();
        for (var i = 0; i < _customRoute.Count; i++)
            text.AppendLine($"{i + 1:D3}. {_customRoute[i].Type.ToUpperInvariant()} | {_customRoute[i].Name} | {_customRoute[i].Id}");
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
