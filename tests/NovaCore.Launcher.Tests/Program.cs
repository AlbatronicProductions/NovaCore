using System.Globalization;
using NovaCore.Launcher;

var tests = new (string Name, Action Test)[]
{
    ("default selection", DefaultSelection),
    ("scenario catalog", ScenarioCatalogMappings),
    ("Earth orbital arguments", EarthOrbitalArguments),
    ("invariant altitude formatting", InvariantAltitudeFormatting),
    ("subdivision diagnostic mapping", SubdivisionMapping),
    ("anchored billboard diagnostic mapping", AnchoredBillboardMapping),
    ("Florida vertical slice mapping", FloridaVerticalSliceMapping),
    ("Florida launch mapping", FloridaLaunchMapping),
    ("unsupported Florida surface", UnsupportedFloridaSurface),
    ("invalid configuration rejection", InvalidConfiguration),
    ("validation argument", ValidationArgument)
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
    Equal(9, ScenarioCatalog.All.Count);
    Equal(8, ScenarioCatalog.All.Count(definition => definition.IsSupported));
    True(ScenarioCatalog.All.Select(definition => definition.Preset).Distinct().Count() == ScenarioCatalog.All.Count,
        "Scenario presets must be unique.");
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

static void AnchoredBillboardMapping()
{
    var configuration = Create(NovaCoreScenarioPreset.AnchoredBillboardDiagnostic);
    SequenceEqual(
        ["--scene=planetary-anchored-billboard-diagnostic"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void FloridaVerticalSliceMapping()
{
    var configuration = Create(NovaCoreScenarioPreset.FloridaVerticalSlice);
    SequenceEqual(["--scene=florida-vertical-slice"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void FloridaLaunchMapping()
{
    var configuration = Create(NovaCoreScenarioPreset.FloridaLaunchSite);
    SequenceEqual(
        ["--scene=sol", "--surface-site=florida-launch"],
        LaunchCommandBuilder.BuildArguments(configuration));
}

static void UnsupportedFloridaSurface()
{
    False(ScenarioCatalog.TryCreateConfiguration(
        NovaCoreScenarioPreset.FloridaSurface,
        null,
        false,
        out var configuration,
        out var error), "Florida Surface must not invent an unsupported CLI mapping.");
    True(configuration is null, "Unsupported preset unexpectedly created a configuration.");
    True(!string.IsNullOrWhiteSpace(error), "Unsupported preset must provide an explanation.");
}

static void InvalidConfiguration()
{
    False(ScenarioCatalog.TryCreateConfiguration(
        NovaCoreScenarioPreset.Earth700Km,
        9.0,
        false,
        out _,
        out _), "Below-clearance altitude was accepted.");

    var invalid = new NovaCoreLaunchConfiguration(
        NovaCoreScenarioPreset.SolarSystemOverview,
        NovaCoreScene.Earth,
        NovaCoreStartingBody.None,
        null,
        null,
        false);
    Throws<ArgumentException>(() => LaunchCommandBuilder.BuildArguments(invalid));
}

static void ValidationArgument()
{
    var configuration = Create(NovaCoreScenarioPreset.SolarSystemOverview, validation: true);
    SequenceEqual(["--scene=sol", "--log=validation"], LaunchCommandBuilder.BuildArguments(configuration));
}

static NovaCoreLaunchConfiguration Create(
    NovaCoreScenarioPreset preset,
    double? altitude = null,
    bool validation = false)
{
    True(ScenarioCatalog.TryCreateConfiguration(preset, altitude, validation, out var configuration, out var error),
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
