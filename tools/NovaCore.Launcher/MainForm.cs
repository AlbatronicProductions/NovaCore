using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;

namespace NovaCore.Launcher;

public sealed class MainForm : Form
{
    private readonly string _repositoryRoot;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ComboBox _scenario = new();
    private readonly Label _description = new();
    private readonly Label _bodyValue = new();
    private readonly Label _locationValue = new();
    private readonly NumericUpDown _altitude = new();
    private readonly CheckBox _validation = new();
    private readonly Label _productionAsset = new();
    private readonly Label _localAsset = new();
    private readonly TextBox _commandPreview = new();
    private readonly Label _status = new();
    private readonly Button _launch = new();
    private bool _loading;
    private NovaCoreScenarioPreset? _selectedPreset;

    public MainForm(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        Text = "NovaCore — Startup Configuration";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 720);
        ClientSize = new Size(760, 760);
        BackColor = Color.FromArgb(15, 20, 28);
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = new Font("Segoe UI", 10.0f, FontStyle.Regular, GraphicsUnit.Point);

        BuildInterface();
        AcceptButton = _launch;
        LoadSettings();
        Shown += async (_, _) => await RefreshAssetStatusAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            ColumnCount = 1,
            RowCount = 9,
            AutoScroll = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var title = new Label
        {
            AutoSize = true,
            Text = "NOVACORE",
            ForeColor = Color.FromArgb(94, 200, 255),
            Font = new Font("Segoe UI Semibold", 25.0f, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 0)
        };
        root.Controls.Add(title);
        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "STARTUP CONFIGURATION",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 10.0f, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(2, 0, 0, 18)
        });

        var scenarioGroup = Group("SCENARIO");
        var scenarioLayout = TwoColumnLayout();
        scenarioGroup.Controls.Add(scenarioLayout);
        scenarioLayout.Controls.Add(FieldLabel("Preset"), 0, 0);
        _scenario.DropDownStyle = ComboBoxStyle.DropDownList;
        _scenario.Dock = DockStyle.Fill;
        _scenario.DataSource = ScenarioCatalog.All.ToList();
        _scenario.SelectedIndexChanged += (_, _) => SelectionChanged();
        scenarioLayout.Controls.Add(_scenario, 1, 0);
        _description.AutoSize = true;
        _description.MaximumSize = new Size(470, 0);
        _description.ForeColor = Color.FromArgb(148, 163, 184);
        _description.Margin = new Padding(0, 8, 0, 0);
        scenarioLayout.Controls.Add(_description, 1, 1);
        root.Controls.Add(scenarioGroup);

        var situationGroup = Group("STARTING SITUATION");
        var situationLayout = TwoColumnLayout();
        situationGroup.Controls.Add(situationLayout);
        situationLayout.Controls.Add(FieldLabel("Body"), 0, 0);
        situationLayout.Controls.Add(_bodyValue, 1, 0);
        situationLayout.Controls.Add(FieldLabel("Location"), 0, 1);
        situationLayout.Controls.Add(_locationValue, 1, 1);
        situationLayout.Controls.Add(FieldLabel("Altitude (m)"), 0, 2);
        _altitude.Dock = DockStyle.Fill;
        _altitude.DecimalPlaces = 0;
        _altitude.Minimum = (decimal)ScenarioCatalog.MinimumTerrainSafeAltitudeMetres;
        _altitude.Maximum = 10_000_000_000m;
        _altitude.ThousandsSeparator = true;
        _altitude.ValueChanged += (_, _) => SelectionChanged();
        situationLayout.Controls.Add(_altitude, 1, 2);
        root.Controls.Add(situationGroup);

        var rendererGroup = Group("RENDERER / DEVELOPMENT");
        _validation.AutoSize = true;
        _validation.Text = "Enable Vulkan validation logging";
        _validation.Margin = new Padding(12);
        _validation.CheckedChanged += (_, _) => SelectionChanged();
        rendererGroup.Controls.Add(_validation);
        root.Controls.Add(rendererGroup);

        var assetsGroup = Group("ASSETS");
        var assetsLayout = TwoColumnLayout();
        assetsGroup.Controls.Add(assetsLayout);
        assetsLayout.Controls.Add(FieldLabel("earth-surface-v5"), 0, 0);
        assetsLayout.Controls.Add(_productionAsset, 1, 0);
        assetsLayout.Controls.Add(FieldLabel("earth-local-v2"), 0, 1);
        assetsLayout.Controls.Add(_localAsset, 1, 1);
        var refresh = SecondaryButton("Refresh status");
        refresh.Click += async (_, _) => await RefreshAssetStatusAsync();
        assetsLayout.Controls.Add(refresh, 1, 2);
        root.Controls.Add(assetsGroup);

        var commandGroup = Group("COMMAND PREVIEW");
        _commandPreview.ReadOnly = true;
        _commandPreview.Multiline = true;
        _commandPreview.ScrollBars = ScrollBars.Horizontal;
        _commandPreview.WordWrap = false;
        _commandPreview.Dock = DockStyle.Fill;
        _commandPreview.Height = 68;
        _commandPreview.BackColor = Color.FromArgb(8, 12, 18);
        _commandPreview.ForeColor = Color.FromArgb(196, 214, 229);
        _commandPreview.BorderStyle = BorderStyle.FixedSingle;
        commandGroup.Controls.Add(_commandPreview);
        root.Controls.Add(commandGroup);

        _status.AutoSize = true;
        _status.ForeColor = Color.FromArgb(148, 163, 184);
        _status.Margin = new Padding(2, 8, 0, 8);
        root.Controls.Add(_status);

        _launch.Text = "START NOVACORE";
        _launch.Dock = DockStyle.Top;
        _launch.Height = 52;
        _launch.FlatStyle = FlatStyle.Flat;
        _launch.FlatAppearance.BorderSize = 0;
        _launch.BackColor = Color.FromArgb(18, 130, 184);
        _launch.ForeColor = Color.White;
        _launch.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold, GraphicsUnit.Point);
        _launch.Click += (_, _) => LaunchSelectedScenario();
        root.Controls.Add(_launch);
    }

    private void LoadSettings()
    {
        _loading = true;
        var settings = LauncherSettingsStore.LoadOrDefault();
        var definition = ScenarioCatalog.Get(settings.Preset);
        _scenario.SelectedItem = definition;
        _validation.Checked = settings.EnableVulkanValidation;
        if (definition.DefaultAltitudeMetres.HasValue)
        {
            var altitude = settings.AltitudeMetres ?? definition.DefaultAltitudeMetres.Value;
            var boundedAltitude = double.IsFinite(altitude)
                ? Math.Clamp(altitude, decimal.ToDouble(_altitude.Minimum), decimal.ToDouble(_altitude.Maximum))
                : definition.DefaultAltitudeMetres.Value;
            _altitude.Value = (decimal)boundedAltitude;
        }

        _selectedPreset = definition.Preset;
        _loading = false;
        SelectionChanged();
    }

    private void SelectionChanged()
    {
        if (_loading || _scenario.SelectedItem is not NovaCoreScenarioDefinition definition)
        {
            return;
        }

        var presetChanged = _selectedPreset != definition.Preset;
        _selectedPreset = definition.Preset;
        _description.Text = definition.IsSupported
            ? definition.Description
            : $"{definition.Description}  {definition.UnsupportedReason}";
        _description.ForeColor = definition.IsSupported
            ? Color.FromArgb(148, 163, 184)
            : Color.FromArgb(251, 191, 36);
        _bodyValue.Text = definition.StartingBody == NovaCoreStartingBody.None ? "—" : definition.StartingBody.ToString();
        _locationValue.Text = definition.SurfaceSite ?? "—";
        _altitude.Enabled = definition.IsSupported && definition.DefaultAltitudeMetres.HasValue;
        if (definition.DefaultAltitudeMetres.HasValue && presetChanged)
        {
            _altitude.Value = Math.Clamp((decimal)definition.DefaultAltitudeMetres.Value, _altitude.Minimum, _altitude.Maximum);
        }

        if (TryCurrentConfiguration(out var configuration, out var error))
        {
            var plan = NovaCoreProcessLauncher.CreatePlan(_repositoryRoot, configuration!);
            _commandPreview.Text = plan.DisplayCommand;
            _launch.Enabled = true;
            _status.Text = "Ready.";
        }
        else
        {
            _commandPreview.Text = error;
            _launch.Enabled = false;
            _status.Text = error;
        }
    }

    private bool TryCurrentConfiguration(
        out NovaCoreLaunchConfiguration? configuration,
        out string? error)
    {
        if (_scenario.SelectedItem is not NovaCoreScenarioDefinition definition)
        {
            configuration = null;
            error = "Select a scenario preset.";
            return false;
        }

        double? altitude = definition.DefaultAltitudeMetres.HasValue ? decimal.ToDouble(_altitude.Value) : null;
        return ScenarioCatalog.TryCreateConfiguration(
            definition.Preset,
            altitude,
            _validation.Checked,
            out configuration,
            out error);
    }

    private void LaunchSelectedScenario()
    {
        try
        {
            if (!TryCurrentConfiguration(out var configuration, out var error))
            {
                _status.Text = error;
                return;
            }

            var plan = NovaCoreProcessLauncher.CreatePlan(_repositoryRoot, configuration!);
            var process = NovaCoreProcessLauncher.Launch(plan);
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => ReportProcessExit(process);
            LauncherSettingsStore.TrySave(
                new(configuration!.Preset, configuration.AltitudeMetres, configuration.EnableVulkanValidation),
                out var settingsError);
            _status.Text = settingsError is null
                ? $"NovaCore started (process {process.Id.ToString(CultureInfo.InvariantCulture)})."
                : $"NovaCore started; launcher settings were not saved: {settingsError}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or ArgumentException)
        {
            _status.Text = "Launch failed.";
            MessageBox.Show(exception.Message, "NovaCore launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReportProcessExit(Process process)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            process.Dispose();
            return;
        }

        try
        {
            BeginInvoke(() =>
            {
                _status.Text = $"NovaCore process {process.Id.ToString(CultureInfo.InvariantCulture)} exited.";
                process.Dispose();
            });
        }
        catch (InvalidOperationException)
        {
            process.Dispose();
        }
    }

    private async Task RefreshAssetStatusAsync()
    {
        SetAssetStatus(_productionAsset, "Checking…", Color.FromArgb(148, 163, 184));
        SetAssetStatus(_localAsset, "Checking…", Color.FromArgb(148, 163, 184));
        try
        {
            var productionTask = AssetStatusService.QueryAsync(_repositoryRoot, "earth-surface-v5", _lifetime.Token);
            var localTask = AssetStatusService.QueryAsync(_repositoryRoot, "earth-local-v2", _lifetime.Token);
            var statuses = await Task.WhenAll(productionTask, localTask);
            ApplyAssetStatus(_productionAsset, statuses[0], optional: false);
            ApplyAssetStatus(_localAsset, statuses[1], optional: true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
    }

    private static void ApplyAssetStatus(Label label, LauncherAssetStatus status, bool optional)
    {
        var suffix = optional && status.State == LauncherAssetState.Missing ? " (optional)" : string.Empty;
        var text = status.State switch
        {
            LauncherAssetState.Ready => "Ready",
            LauncherAssetState.Missing => "Missing" + suffix,
            _ => "Status unavailable"
        };
        var color = status.State switch
        {
            LauncherAssetState.Ready => Color.FromArgb(74, 222, 128),
            LauncherAssetState.Missing when optional => Color.FromArgb(251, 191, 36),
            LauncherAssetState.Missing => Color.FromArgb(248, 113, 113),
            _ => Color.FromArgb(148, 163, 184)
        };
        label.Text = text;
        label.ForeColor = color;
        label.AccessibleDescription = status.Detail;
    }

    private static void SetAssetStatus(Label label, string text, Color color)
    {
        label.Text = text;
        label.ForeColor = color;
    }

    private static GroupBox Group(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(12, 10, 12, 12),
        Margin = new Padding(0, 0, 0, 12),
        ForeColor = Color.FromArgb(203, 213, 225)
    };

    private static TableLayoutPanel TwoColumnLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static Label FieldLabel(string text) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = Color.FromArgb(148, 163, 184),
        Margin = new Padding(0, 7, 12, 7)
    };

    private static Button SecondaryButton(string text) => new()
    {
        AutoSize = true,
        Text = text,
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.FromArgb(203, 213, 225),
        BackColor = Color.FromArgb(30, 41, 59),
        Margin = new Padding(0, 8, 0, 0)
    };
}
