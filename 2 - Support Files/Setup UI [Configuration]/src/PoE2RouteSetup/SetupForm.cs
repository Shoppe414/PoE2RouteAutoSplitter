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
    private readonly ComboBox _startAreaCombo = new();
    private readonly CheckBox _devConsoleCheck = new();
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
        Height = 800;
        MinimumSize = new Size(920, 680);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        PopulatePresets();
        PopulateCustomCatalogs();
        _targetText.Text = Path.Combine(_userRoot, "LiveSplit Target");
        _targetText.ReadOnly = true;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(BuildTargetPanel(), 0, 0);
        root.Controls.Add(BuildModeTabs(), 0, 1);
        root.Controls.Add(BuildActionPanel(), 0, 2);
        _status.AutoSize = true;
        _status.Padding = new Padding(4, 8, 4, 0);
        _status.Text = "Choose a premade setup or build a custom route, then deploy it to the target directory.";
        root.Controls.Add(_status, 0, 3);
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

    private Control BuildModeTabs()
    {
        _modeTabs.Dock = DockStyle.Fill;
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

        var startPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        startPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        startPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        startPanel.Controls.Add(new Label { Text = "Timer start:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
        _startAreaCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _startAreaCombo.Dock = DockStyle.Fill;
        startPanel.Controls.Add(_startAreaCombo, 1, 0);
        panel.Controls.Add(startPanel, 0, 1);

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
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, Padding = new Padding(0, 8, 0, 0) };
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

        var watcher = new Button { Text = "Start BossWatcher", AutoSize = true, Height = 38, Padding = new Padding(12, 2, 12, 2) };
        watcher.Click += (_, _) => StartBossWatcher();
        panel.Controls.Add(watcher, 3, 0);
        return panel;
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
        _startAreaCombo.Items.Add(new StartAreaOption());
        foreach (var area in _areas)
            _startAreaCombo.Items.Add(new StartAreaOption { Id = area.Id, Name = area.Name });
        _startAreaCombo.SelectedIndex = 0;
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
    }

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
            else
            {
                var start = _startAreaCombo.SelectedItem as StartAreaOption;
                if (start is not null && !string.IsNullOrEmpty(start.Id) &&
                    _customRoute.Any(x => x.Type.Equals("area", StringComparison.OrdinalIgnoreCase) && x.Id.Equals(start.Id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("The timer start area cannot also be a split objective. Choose Manual start, choose a different start area, or remove that area from the route.");
            }

            var target = ValidateTargetPath();
            stage = Path.Combine(Path.GetTempPath(), "PoE2RouteSetup", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);

            if (preset is not null)
                DeployPreset(preset, stage, target);
            else
                DeployCustom(stage, target);

            if (!CommitStage(stage, target)) return;

            if (preset is not null)
                SetStatus($"Deployed: {preset.Group} / {preset.DisplayName}");
            else
                SetStatus($"Deployed custom route with {_customRoute.Count} objective(s).");

            MessageBox.Show(this,
                "Setup generated successfully.\n\n" +
                "1. Open the generated .lss splits file in LiveSplit.\n" +
                "2. Keep your own LiveSplit layout.\n" +
                "3. Add/edit the Scriptable Auto Splitter component and browse to the generated .asl file in the LiveSplit Target directory.\n\n" +
                "No .lsl file is generated by this tool.",
                "PoE2 AutoSplitter Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void DeployPreset(PresetDefinition preset, string stage, string target)
    {
        var sourceLss = Resolve(preset.LssSource);
        var sourceAsl = Resolve(preset.AslSource);
        if (!File.Exists(sourceLss)) throw new FileNotFoundException("Preset splits file was not found.", sourceLss);
        if (!File.Exists(sourceAsl)) throw new FileNotFoundException("Preset autosplitter file was not found.", sourceAsl);

        var stagedLss = Path.Combine(stage, Path.GetFileName(sourceLss));
        var stagedAsl = Path.Combine(stage, Path.GetFileName(sourceAsl));
        var targetAsl = Path.Combine(target, Path.GetFileName(sourceAsl));
        File.Copy(sourceLss, stagedLss, true);

        foreach (var runtime in preset.RuntimeFiles)
        {
            var runtimeSource = Resolve(runtime.Source);
            if (!File.Exists(runtimeSource)) throw new FileNotFoundException("Preset runtime file was not found.", runtimeSource);
            File.Copy(runtimeSource, Path.Combine(stage, runtime.Target), true);
        }

        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(File.ReadAllText(sourceAsl), target);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        if (preset.RequiresBossWatcher) EnsureBossEventFile(stage);

        WriteSetupSummary(stage, preset.Group, preset.DisplayName, stagedLss, stagedAsl, targetAsl, preset.RequiresBossWatcher);
    }

    private void DeployCustom(string stage, string target)
    {
        var start = _startAreaCombo.SelectedItem as StartAreaOption ?? new StartAreaOption();
        var routePath = Path.Combine(stage, "poe2_mixed_route.txt");
        var route = new StringBuilder();
        route.AppendLine("# Generated by PoE2 Route AutoSplitter Setup UI");
        route.AppendLine($"@start={(string.IsNullOrEmpty(start.Id) ? "manual" : start.Id)}");
        route.AppendLine($"@order={(_orderedCheck.Checked ? "ordered" : "unordered")}");
        route.AppendLine();
        foreach (var entry in _customRoute)
            route.AppendLine($"{entry.RouteText,-42} # {entry.Name}");
        File.WriteAllText(routePath, route.ToString(), new UTF8Encoding(false));

        var sourceAsl = Resolve(_manifest.CustomAslSource);
        var stagedAsl = Path.Combine(stage, "PathOfExile2_CustomRouteAutosplitter.asl");
        var targetAsl = Path.Combine(target, "PathOfExile2_CustomRouteAutosplitter.asl");
        var patchedAsl = LiveSplitFiles.RewriteRuntimePaths(File.ReadAllText(sourceAsl), target);
        File.WriteAllText(stagedAsl, patchedAsl, new UTF8Encoding(false));

        var stagedLss = Path.Combine(stage, "Path of Exile 2 - Custom Route.lss");
        LiveSplitFiles.WriteCustomSplits(stagedLss, _customRoute);

        var needsWatcher = _customRoute.Any(x => x.Type.Equals("boss", StringComparison.OrdinalIgnoreCase));
        if (needsWatcher) EnsureBossEventFile(stage);
        WriteSetupSummary(stage, "Custom Route", $"{_customRoute.Count} objectives; {(_orderedCheck.Checked ? "ordered" : "unordered")}", stagedLss, stagedAsl, targetAsl, needsWatcher);
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
                throw new FileNotFoundException("PoE2BossWatcher.exe was not found. In 2 - Support Files\\BossWatcher [Boss Rush Detection], run Setup-OCR.ps1 and then Build.ps1.");

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

    private static void EnsureBossEventFile(string target)
    {
        var path = Path.Combine(target, "poe2_boss_events.log");
        if (!File.Exists(path)) File.WriteAllText(path, "");
    }

    private void WriteSetupSummary(string outputDirectory, string group, string setup, string lss, string asl, string deployedAslPath, bool bossWatcher)
    {
        var text = $"PoE2 Route AutoSplitter v{_manifest.Version}\r\n" +
                   $"Mode: {group}\r\nSetup: {setup}\r\n" +
                   $"Splits (.lss): {Path.GetFileName(lss)}\r\n" +
                   $"AutoSplitter (.asl): {Path.GetFileName(asl)}\r\n" +
                   $"AutoSplitter full path: {Path.GetFullPath(deployedAslPath)}\r\n" +
                   "Layout (.lsl): Not generated by design\r\n" +
                   $"BossWatcher required: {(bossWatcher ? "Yes" : "No")}\r\n\r\n" +
                   "LiveSplit setup:\r\n" +
                   "1. Open the generated .lss splits file.\r\n" +
                   "2. Keep your own LiveSplit layout.\r\n" +
                   "3. Add/edit the Scriptable Auto Splitter component in that layout and browse to the AutoSplitter full path above.\r\n" +
                   "The Setup UI intentionally does not generate .lsl files because LiveSplit layout files are user-specific.\r\n" +
                   (bossWatcher ? "4. Start BossWatcher from the Setup UI before/during the run.\r\n" : "");
        File.WriteAllText(Path.Combine(outputDirectory, "SETUP_INFO.txt"), text, new UTF8Encoding(false));
    }

    private string Resolve(string relative) => Path.GetFullPath(Path.Combine(_packageRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    private void SetStatus(string text) => _status.Text = text;
    private void ShowError(string text) => MessageBox.Show(this, text, "PoE2 AutoSplitter Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
