using System.Globalization;
using NovaCore.Launcher;

var tests = new (string Name, Action Test)[]
{
    ("default selection", DefaultSelection),
    ("scenario catalog", ScenarioCatalogMappings),
    ("Earth fullscreen native preset", EarthFullscreenNativePreset),
    ("Earth fullscreen Solar camera path", EarthFullscreenSolarCameraPath),
    ("structured launch environment", StructuredLaunchEnvironment),
    ("Earth orbital arguments", EarthOrbitalArguments),
    ("invariant altitude formatting", InvariantAltitudeFormatting),
    ("subdivision diagnostic mapping", SubdivisionMapping),
    ("Florida launch mapping", FloridaLaunchMapping),
    ("invalid configuration rejection", InvalidConfiguration),
    ("diagnostics arguments", DiagnosticsArguments),
    ("existing production scenarios", ExistingProductionScenarios)
};

foreach (var (name, test) in tests)
{
    test();
    Console.WriteLine($"PASS {name}");
}

Console.WriteLine($"NovaCore launcher tests passed: {tests.Length}");

static void DefaultSelection()
{
    Equal(NovaCoreScenarioPreset.SolarSystemOverview, ScenarioCatalog.Default.Preset);
    var configuration = Create(ScenarioCatalog.Default.Preset);
    SequenceEqual(["--scene=sol"], LaunchCommandBuilder.BuildArguments(configuration));
}

static void ScenarioCatalogMappings()
{
    Equal(6, ScenarioCatalog.All.Count);
    Equal(6, ScenarioCatalog.All.Count(definition => definition.IsSupported));
    True(ScenarioCatalog.All.Select(definition => definition.Preset).Distinct().Count() == ScenarioCatalog.All.Count,
        "Scenario presets must be unique.");
}

static void EarthFullscreenNativePreset()
{
    var definition = ScenarioCatalog.Get(NovaCoreScenarioPreset.EarthFullscreenNative);
    Equal("Earth — Fullscreen Native", definition.DisplayName);
    var configuration = Create(NovaCoreScenarioPreset.EarthFullscreenNative);
    Equal(NovaCoreWindowMode.BorderlessFullscreen, configuration.WindowMode);
    Equal(NovaCoreResolutionPreset.NativeDesktop, configuration.ResolutionPreset);
    Equal(new NovaCoreClientResolution(3440, 1440), configuration.ClientResolution);
    Equal(NovaCoreDiagnosticsMode.VulkanValidationAndPerformance, configuration.Diagnostics);
    SequenceEqual(
        ["--scene=sol", "--focus=earth", "--altitude=700000", "--surface-site=land", "--log=validation", "--log=vulkan"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void EarthFullscreenSolarCameraPath()
{
    var overview = Create(NovaCoreScenarioPreset.SolarSystemOverview);
    var fullscreen = Create(NovaCoreScenarioPreset.EarthFullscreenNative);
    Equal(NovaCoreScene.Solar, overview.Scene);
    Equal(overview.Scene, fullscreen.Scene);
    Equal(NovaCoreStartingBody.Earth, fullscreen.StartingBody);
    var arguments=LaunchCommandBuilder.BuildArguments(fullscreen);
    True(arguments.Contains("--focus=earth",StringComparer.Ordinal),"Earth fullscreen does not pre-focus Earth.");
    False(arguments.Contains("--scene=earth",StringComparer.Ordinal),"Earth fullscreen still selects the legacy Earth-only camera scene.");
}

static void StructuredLaunchEnvironment()
{
    var configuration = Create(NovaCoreScenarioPreset.EarthFullscreenNative);
    var environment = LaunchCommandBuilder.BuildEnvironment(configuration);
    Equal("3440", environment["NOVACORE_WINDOW_CLIENT_WIDTH"]);
    Equal("1440", environment["NOVACORE_WINDOW_CLIENT_HEIGHT"]);
    Equal("1", environment["NOVACORE_WINDOW_BORDERLESS"]);
    Equal("VK_LAYER_KHRONOS_validation", environment["VK_INSTANCE_LAYERS"]);

    var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var plan = NovaCoreProcessLauncher.CreatePlan(repositoryRoot, configuration);
    Equal("3440", plan.EnvironmentVariables["NOVACORE_WINDOW_CLIENT_WIDTH"]);
    Equal("1", plan.EnvironmentVariables["NOVACORE_WINDOW_BORDERLESS"]);
    True(plan.Arguments.Contains("--scene=sol", StringComparer.Ordinal), "Solar camera scene was not encoded structurally.");
    True(plan.Arguments.Contains("--focus=earth", StringComparer.Ordinal), "Earth startup focus was not encoded structurally.");
}

static void EarthOrbitalArguments()
{
    var configuration = Create(NovaCoreScenarioPreset.EarthFarOrbital, 3_000_000.0);
    SequenceEqual(
        ["--scene=earth", "--altitude=3000000", "--surface-site=land"],
        LaunchCommandBuilder.BuildArguments(configuration));

    var at700Km = Create(NovaCoreScenarioPreset.Earth700Km, 700_000.0);
    SequenceEqual(
        ["--scene=earth", "--altitude=700000", "--surface-site=land"],
        LaunchCommandBuilder.BuildArguments(at700Km));
}

static void InvariantAltitudeFormatting()
{
    var previous = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        var configuration = Create(NovaCoreScenarioPreset.Earth700Km, 700_000.25);
        SequenceEqual(
            ["--scene=earth", "--altitude=700000.25", "--surface-site=land"],
            LaunchCommandBuilder.BuildArguments(configuration));
    }
    finally
    {
        CultureInfo.CurrentCulture = previous;
    }
}

static void SubdivisionMapping()
{
    var configuration = Create(NovaCoreScenarioPreset.SubdivisionDiagnostic);
    SequenceEqual(
        ["--scene=planetary-subdivision-diagnostic"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void FloridaLaunchMapping()
{
    var configuration = Create(NovaCoreScenarioPreset.FloridaLaunchSite);
    SequenceEqual(
        ["--scene=sol", "--focus=earth", "--surface-site=florida-launch"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void InvalidConfiguration()
{
    False(ScenarioCatalog.TryCreateConfiguration(
        NovaCoreScenarioPreset.Earth700Km,
        9.0,
        NovaCoreWindowMode.Windowed,
        NovaCoreResolutionPreset.Resolution960x540,
        NovaCoreDiagnosticsMode.Normal,
        3440,
        1440,
        out _,
        out _), "Below-clearance altitude was accepted.");

    var invalid = new NovaCoreLaunchConfiguration(
        NovaCoreScenarioPreset.SolarSystemOverview,
        NovaCoreScene.Earth,
        NovaCoreStartingBody.None,
        null,
        null,
        NovaCoreWindowMode.Windowed,
        NovaCoreResolutionPreset.Resolution960x540,
        new(960, 540),
        NovaCoreDiagnosticsMode.Normal);
    Throws<ArgumentException>(() => LaunchCommandBuilder.BuildArguments(invalid));
}

static void DiagnosticsArguments()
{
    var normal = Create(NovaCoreScenarioPreset.SolarSystemOverview, diagnostics: NovaCoreDiagnosticsMode.Normal);
    SequenceEqual(["--scene=sol"], LaunchCommandBuilder.BuildArguments(normal));
    var normalEnvironment = LaunchCommandBuilder.BuildEnvironment(normal);
    Equal("0", normalEnvironment["NOVACORE_WINDOW_BORDERLESS"]);
    False(normalEnvironment.ContainsKey("VK_INSTANCE_LAYERS"),
        "Normal diagnostics unexpectedly enabled Vulkan validation.");
    SequenceEqual(["--scene=sol", "--log=vulkan"], LaunchCommandBuilder.BuildArguments(
        Create(NovaCoreScenarioPreset.SolarSystemOverview, diagnostics: NovaCoreDiagnosticsMode.PerformanceTelemetry)));
    SequenceEqual(["--scene=sol", "--log=validation"], LaunchCommandBuilder.BuildArguments(
        Create(NovaCoreScenarioPreset.SolarSystemOverview, diagnostics: NovaCoreDiagnosticsMode.VulkanValidation)));
    SequenceEqual(["--scene=sol", "--log=validation", "--log=vulkan"], LaunchCommandBuilder.BuildArguments(
        Create(NovaCoreScenarioPreset.SolarSystemOverview, diagnostics: NovaCoreDiagnosticsMode.VulkanValidationAndPerformance)));
}

static void ExistingProductionScenarios()
{
    foreach (var definition in ScenarioCatalog.All.Where(definition =>
                 definition.Preset != NovaCoreScenarioPreset.EarthFullscreenNative))
    {
        var configuration = Create(definition.Preset);
        True(LaunchCommandBuilder.BuildArguments(configuration).Count > 0,
            $"Existing scenario {definition.DisplayName} produced no arguments.");
    }
}

static NovaCoreLaunchConfiguration Create(
    NovaCoreScenarioPreset preset,
    double? altitude = null,
    NovaCoreWindowMode? windowMode = null,
    NovaCoreResolutionPreset? resolution = null,
    NovaCoreDiagnosticsMode? diagnostics = null,
    int desktopWidth = 3440,
    int desktopHeight = 1440)
{
    var definition = ScenarioCatalog.Get(preset);
    True(ScenarioCatalog.TryCreateConfiguration(preset, altitude,
            windowMode ?? definition.DefaultWindowMode,
            resolution ?? definition.DefaultResolution,
            diagnostics ?? definition.DefaultDiagnostics,
            desktopWidth,
            desktopHeight,
            out var configuration, out var error),
        error ?? $"Could not create {preset}.");
    return configuration!;
}

static void SequenceEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
{
    if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
    {
        throw new InvalidOperationException($"Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
    }
}

static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}; actual {actual}.");
    }
}

static void True(bool condition, string message = "Expected true.")
{
    if (!condition) throw new InvalidOperationException(message);
}

static void False(bool condition, string message = "Expected false.") => True(!condition, message);

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}
