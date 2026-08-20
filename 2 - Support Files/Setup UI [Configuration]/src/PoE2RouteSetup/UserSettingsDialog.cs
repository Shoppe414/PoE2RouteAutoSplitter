namespace PoE2RouteSetup;

public sealed class UserSettingsDialog : Form
{
    private readonly NumericUpDown _windowWidth = PercentNumeric(25, 100, 50);
    private readonly NumericUpDown _windowHeight = PercentNumeric(50, 100, 100);
    private readonly ComboBox _defaultLanguage = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly ComboBox _gameLanguage = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly CheckBox _devConsoleDefault = new() { AutoSize = true, Text = "Enable developer console diagnostics" };
    private readonly NumericUpDown _goneSeconds = DecimalNumeric(0.5m, 30m, 5.5m, 1, 0.5m);
    private readonly NumericUpDown _mapGoneSeconds = DecimalNumeric(0.1m, 30m, 5.5m, 1, 0.1m);
    private readonly NumericUpDown _provisionalMs = IntegerNumeric(200, 3000, 1200, 100);
    private readonly NumericUpDown _pauseStack = ThresholdPercentNumeric(62m);
    private readonly NumericUpDown _resumeGame = ThresholdPercentNumeric(58m);
    private readonly NumericUpDown _pauseBanner = ThresholdPercentNumeric(40m);
    private readonly NumericUpDown _exitPath = ThresholdPercentNumeric(50m);
    private readonly NumericUpDown _mtxShop = ThresholdPercentNumeric(70m);
    private int _mapExitAssistMinMissingMs = 500;

    public UserSettings Settings { get; private set; }

    public UserSettingsDialog(UserSettings settings)
    {
        Settings = settings.Clone();
        Text = "PoE2 AutoSplitter Settings";
        StartPosition = FormStartPosition.CenterParent;
        var startupWorkArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        MinimumSize = new Size(Math.Min(700, startupWorkArea.Width), Math.Min(560, startupWorkArea.Height));
        MaximumSize = startupWorkArea.Size;
        Size = new Size(Math.Min(800, startupWorkArea.Width), Math.Min(780, startupWorkArea.Height));
        FormBorderStyle = FormBorderStyle.Sizable;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var intro = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(730, 0),
            Text = "These are user-facing tuning values. Generate / Deploy snapshots the effective values into poe2_run_settings.json so the run audit records the settings actually used. Advanced detector internals remain in each watcher's support config.json."
        };
        root.Controls.Add(intro);
        root.Controls.Add(BuildSetupGroup());
        root.Controls.Add(BuildBossGroup());
        root.Controls.Add(BuildGameTimeGroup());
        root.Controls.Add(BuildButtons());

        foreach (var language in Localization.Languages) _defaultLanguage.Items.Add(language);
        _defaultLanguage.DisplayMember = nameof(UiLanguage.DisplayName);
        foreach (var language in PoE2GameLanguages.All) _gameLanguage.Items.Add(language);
        _gameLanguage.DisplayMember = nameof(PoE2GameLanguage.DisplayName);
        LoadValues(Settings);
        Localization.Apply(this);

        // Size the dialog to its localized content once WinForms has completed layout.
        // Grow until every option and the bottom buttons fit, but never exceed 100%
        // of the current monitor work area. AutoScroll remains as a fallback only
        // when a very small display or unusually tall localization cannot fit.
        Shown += (_, _) => FitToLocalizedContent(root);
    }

    private void FitToLocalizedContent(TableLayoutPanel root)
    {
        PerformLayout();
        root.PerformLayout();

        var workArea = Screen.FromControl(this).WorkingArea;
        MaximumSize = workArea.Size;
        MinimumSize = new Size(Math.Min(700, workArea.Width), Math.Min(560, workArea.Height));

        var nonClientWidth = Math.Max(0, Width - ClientSize.Width);
        var nonClientHeight = Math.Max(0, Height - ClientSize.Height);
        var targetClientWidth = Math.Min(Math.Max(ClientSize.Width, 760), Math.Max(1, workArea.Width - nonClientWidth));
        var preferred = root.GetPreferredSize(new Size(targetClientWidth, 0));

        var desiredWidth = Math.Min(workArea.Width, Math.Max(MinimumSize.Width, targetClientWidth + nonClientWidth));
        var desiredHeight = Math.Min(workArea.Height, Math.Max(MinimumSize.Height, preferred.Height + nonClientHeight + 8));
        Size = new Size(desiredWidth, desiredHeight);

        // Re-run preferred-size measurement at the final width because translated
        // descriptions may wrap differently after the dialog grows.
        PerformLayout();
        root.PerformLayout();
        preferred = root.GetPreferredSize(new Size(Math.Max(1, ClientSize.Width), 0));
        desiredHeight = Math.Min(workArea.Height, Math.Max(MinimumSize.Height, preferred.Height + nonClientHeight + 8));
        Height = desiredHeight;
        root.AutoScroll = preferred.Height > ClientSize.Height;

        var ownerScreen = Screen.FromControl(this).WorkingArea;
        Left = Math.Max(ownerScreen.Left, Math.Min(Left, ownerScreen.Right - Width));
        Top = Math.Max(ownerScreen.Top, Math.Min(Top, ownerScreen.Bottom - Height));
    }

    private Control BuildSetupGroup()
    {
        var group = NewGroup("SetupUI");
        var panel = NewGrid();
        AddRow(panel, "Default language", _defaultLanguage, "Language used when SetupUI opens. Saving a new selection applies it immediately and keeps it as the default for future launches.");
        AddRow(panel, "PoE2 game language", _gameLanguage, "Language used by the Path of Exile 2 game client. BossWatcher uses the matching OCR model and authoritative localized boss names. GameTimeWatcher records this language for structure-first pause detection and future language-specific visual profiles. This can be different from the SetupUI language.");
        AddRow(panel, "Initial window width", _windowWidth, "% of current monitor work area");
        AddRow(panel, "Initial window height", _windowHeight, "% of current monitor work area");
        AddSpanning(panel, _devConsoleDefault);
        AddSpanning(panel, new Label
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = "Developer console diagnostics is intended for troubleshooting and test-log collection. When enabled, BossWatcher starts with its verbose developer console and GameTimeWatcher starts through its diagnostic wrapper. Leave this disabled for normal runs."
        });
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildBossGroup()
    {
        var group = NewGroup("BossWatcher");
        var panel = NewGrid();
        AddRow(panel, "Identity/single-boss disappearance", _goneSeconds, "seconds (0.5-30.0)");
        AddRow(panel, "Map-boss disappearance", _mapGoneSeconds, "seconds (0.1-30.0)");
        AddSpanning(panel, new Label
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = "Both disappearance delays default to 5.5 seconds in this development build. These are confirmation delays only: accepted GONE/MAP_GONE events retain the original first-missing timestamp for backdated timing. The two values remain independently adjustable for field testing. Identity dual-boss lane removal keeps its separately calibrated short resolver windows to preserve per-boss ordering/backdating."
        });
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildGameTimeGroup()
    {
        var group = NewGroup("GameTimeWatcher");
        var panel = NewGrid();
        AddRow(panel, "Provisional input timeout", _provisionalMs,
            "ms — how long an ESC/Start input may remain pending while GameTimeWatcher waits for visual confirmation before rejecting the provisional pause/resume.");
        AddRow(panel, "Pause-menu structure/layout match", PercentInput(_pauseStack),
            "Primary pause signal. Compares the language-neutral geometry of the centered four-button pause-menu stack while ignoring most button text. Higher values require a closer layout match.");
        AddRow(panel, "Pause banner match", PercentInput(_pauseBanner),
            "Second strongest pause signal. Looks for the centered GAME PAUSED-style banner region: a dark horizontal banner with centered bright title text. The exact words are not required to match English.");
        AddRow(panel, "'Resume Game' text match", PercentInput(_resumeGame),
            "Low-weight English text-template corroboration. It may help on an English client, but it cannot confirm pause by itself and is not required for other game languages.");
        AddRow(panel, "'Exit Path of Exile' text match", PercentInput(_exitPath),
            "Low-weight English text-template corroboration near the bottom of the pause menu. It cannot confirm pause by itself and is not required for other game languages.");
        AddRow(panel, "MTX shop match", PercentInput(_mtxShop),
            "Compares the screen with the saved Microtransaction Shop image. A match identifies the shop overlay so it is not mistaken for the pause menu.");
        AddSpanning(panel, new Label
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = "Pause detection is structure-first: pause-menu layout has the most weight and the paused-state banner has the second most weight. English Resume/Exit text templates are only low-weight supporting evidence and never confirm pause on their own. Higher percentages are stricter; lower percentages are more permissive."
        });
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildButtons()
    {
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Top, Padding = new Padding(0, 8, 0, 0) };
        var save = new Button { Text = "Save", AutoSize = true, DialogResult = DialogResult.OK, Padding = new Padding(12, 2, 12, 2) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel, Padding = new Padding(12, 2, 12, 2) };
        var defaults = new Button { Text = "Restore Defaults", AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
        defaults.Click += (_, _) => LoadValues(new UserSettings());
        save.Click += (_, _) => StoreValues();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(defaults);
        AcceptButton = save;
        CancelButton = cancel;
        return buttons;
    }

    private void LoadValues(UserSettings settings)
    {
        var languageIndex = Localization.Languages.ToList().FindIndex(x => string.Equals(x.Code, settings.SetupUI.DefaultLanguage, StringComparison.OrdinalIgnoreCase));
        _defaultLanguage.SelectedIndex = languageIndex >= 0 ? languageIndex : 0;
        var gameLanguageIndex = PoE2GameLanguages.All.ToList().FindIndex(x => string.Equals(x.Code, settings.PoE2.Language, StringComparison.OrdinalIgnoreCase));
        _gameLanguage.SelectedIndex = gameLanguageIndex >= 0 ? gameLanguageIndex : 0;
        _windowWidth.Value = settings.SetupUI.WindowWidthPercent;
        _windowHeight.Value = settings.SetupUI.WindowHeightPercent;
        _devConsoleDefault.Checked = settings.SetupUI.DeveloperConsoleDefault;
        _goneSeconds.Value = settings.BossWatcher.GoneConfirmMs / 1000m;
        _mapGoneSeconds.Value = settings.BossWatcher.MapGoneConfirmMs / 1000m;
        _mapExitAssistMinMissingMs = settings.BossWatcher.MapExitAssistMinMissingMs;
        _provisionalMs.Value = settings.GameTimeWatcher.ProvisionalTimeoutMs;
        _pauseStack.Value = (decimal)(settings.GameTimeWatcher.PauseStackThreshold * 100.0);
        _resumeGame.Value = (decimal)(settings.GameTimeWatcher.ResumeGameThreshold * 100.0);
        _pauseBanner.Value = (decimal)(settings.GameTimeWatcher.PauseBannerThreshold * 100.0);
        _exitPath.Value = (decimal)(settings.GameTimeWatcher.ExitPathOfExileThreshold * 100.0);
        _mtxShop.Value = (decimal)(settings.GameTimeWatcher.MtxShopThreshold * 100.0);
    }

    private void StoreValues()
    {
        Settings = new UserSettings
        {
            SchemaVersion = 1,
            SetupUI = new SetupUiUserSettings
            {
                WindowWidthPercent = (int)_windowWidth.Value,
                WindowHeightPercent = (int)_windowHeight.Value,
                DeveloperConsoleDefault = _devConsoleDefault.Checked,
                DefaultLanguage = (_defaultLanguage.SelectedItem as UiLanguage)?.Code ?? "en"
            },
            PoE2 = new PoE2UserSettings
            {
                Language = (_gameLanguage.SelectedItem as PoE2GameLanguage)?.Code ?? "en"
            },
            BossWatcher = new BossWatcherUserSettings
            {
                GoneConfirmMs = (int)Math.Round(_goneSeconds.Value * 1000m),
                MapGoneConfirmMs = (int)Math.Round(_mapGoneSeconds.Value * 1000m),
                MapExitAssistMinMissingMs = _mapExitAssistMinMissingMs
            },
            GameTimeWatcher = new GameTimeWatcherUserSettings
            {
                ProvisionalTimeoutMs = (int)_provisionalMs.Value,
                PauseStackThreshold = (double)(_pauseStack.Value / 100m),
                ResumeGameThreshold = (double)(_resumeGame.Value / 100m),
                PauseBannerThreshold = (double)(_pauseBanner.Value / 100m),
                ExitPathOfExileThreshold = (double)(_exitPath.Value / 100m),
                MtxShopThreshold = (double)(_mtxShop.Value / 100m)
            }
        };
        Settings.Validate();
    }

    private static GroupBox NewGroup(string text) => new() { Text = text, AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10) };

    private static TableLayoutPanel NewGrid()
    {
        var grid = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control, string description)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 10, 3) }, 0, row);
        grid.Controls.Add(control, 1, row);
        grid.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(8, 7, 3, 5)
        }, 2, row);
    }

    private static void AddSpanning(TableLayoutPanel grid, Control control)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Margin = new Padding(3, 6, 3, 6);
        grid.Controls.Add(control, 0, row);
        grid.SetColumnSpan(control, 3);
    }


    private static Control PercentInput(NumericUpDown numeric)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        panel.Controls.Add(numeric);
        panel.Controls.Add(new Label { Text = "%", AutoSize = true, Margin = new Padding(2, 7, 0, 0) });
        return panel;
    }

    private static NumericUpDown PercentNumeric(int min, int max, int value)
        => IntegerNumeric(min, max, value, 5);

    private static NumericUpDown IntegerNumeric(int min, int max, int value, int increment)
        => new() { Minimum = min, Maximum = max, Value = value, Increment = increment, Width = 100, ThousandsSeparator = false };

    private static NumericUpDown DecimalNumeric(decimal min, decimal max, decimal value, int decimals, decimal increment)
        => new() { Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, Increment = increment, Width = 100 };

    private static NumericUpDown ThresholdPercentNumeric(decimal value)
        => new() { Minimum = 0m, Maximum = 100m, Value = value, DecimalPlaces = 0, Increment = 1m, Width = 82 };
}
