namespace NovaCore.Launcher;

public static class ScenarioCatalog
{
    public const double MinimumTerrainSafeAltitudeMetres = 10.0;

    public static IReadOnlyList<NovaCoreScenarioDefinition> All { get; } =
    [
        new(NovaCoreScenarioPreset.SolarSystemOverview, "Solar System Overview",
            "Interactive Solar map using the normal production scene.", NovaCoreScene.Solar,
            NovaCoreStartingBody.None, null, null, true, null),
        new(NovaCoreScenarioPreset.EarthFarOrbital, "Earth Far / Orbital View",
            "Earth production renderer from a 3,000 km orbital starting altitude.", NovaCoreScene.Earth,
            NovaCoreStartingBody.Earth, 3_000_000.0, "land", true, null),
        new(NovaCoreScenarioPreset.Earth700Km, "Earth 700 km",
            "Earth production renderer from 700 km altitude.", NovaCoreScene.Earth,
            NovaCoreStartingBody.Earth, 700_000.0, "land", true, null),
        new(NovaCoreScenarioPreset.FloridaSurface, "Florida Surface (unavailable)",
            "No existing CLI option selects a standalone Florida surface start.", null,
            NovaCoreStartingBody.Earth, null, null, false,
            "The current runtime exposes Florida only as the Solar launch-site configuration."),
        new(NovaCoreScenarioPreset.FloridaLaunchSite, "Florida Launch Site",
            "Solar scene focused on the existing anchored Florida launch site.", NovaCoreScene.Solar,
            NovaCoreStartingBody.Earth, null, "florida-launch", true, null),
        new(NovaCoreScenarioPreset.PlanetaryDiagnostic, "Planetary Diagnostic",
            "Six-root cube-sphere diagnostic scene.", NovaCoreScene.PlanetaryDiagnostic,
            NovaCoreStartingBody.None, null, null, true, null),
        new(NovaCoreScenarioPreset.SubdivisionDiagnostic, "Subdivision Diagnostic",
            "11B-7D screen-space subdivision diagnostic scene.", NovaCoreScene.SubdivisionDiagnostic,
            NovaCoreStartingBody.None, null, null, true, null),
        new(NovaCoreScenarioPreset.AnchoredBillboardDiagnostic, "Anchored Billboard Tiers",
            "11B-7E dormant Florida-anchored T0/T1/T2 transactional ownership diagnostic.",
            NovaCoreScene.AnchoredBillboardDiagnostic, NovaCoreStartingBody.None,
            null, null, true, null)
    ];

    public static NovaCoreScenarioDefinition Default => Get(NovaCoreScenarioPreset.SolarSystemOverview);

    public static NovaCoreScenarioDefinition Get(NovaCoreScenarioPreset preset) =>
        All.First(definition => definition.Preset == preset);

    public static bool TryCreateConfiguration(
        NovaCoreScenarioPreset preset,
        double? altitudeMetres,
        bool enableVulkanValidation,
        out NovaCoreLaunchConfiguration? configuration,
        out string? error)
    {
        var definition = Get(preset);
        if (!definition.IsSupported || definition.Scene is null)
        {
            configuration = null;
            error = definition.UnsupportedReason ?? "This preset is not supported by the current runtime CLI.";
            return false;
        }

        var resolvedAltitude = definition.DefaultAltitudeMetres.HasValue
            ? altitudeMetres ?? definition.DefaultAltitudeMetres
            : null;
        if (resolvedAltitude is { } altitude &&
            (!double.IsFinite(altitude) || altitude < MinimumTerrainSafeAltitudeMetres))
        {
            configuration = null;
            error = $"Altitude must be finite and at least {MinimumTerrainSafeAltitudeMetres:0.###} metres.";
            return false;
        }

        configuration = new NovaCoreLaunchConfiguration(
            preset,
            definition.Scene.Value,
            definition.StartingBody,
            resolvedAltitude,
            definition.SurfaceSite,
            enableVulkanValidation);
        error = null;
        return true;
    }
}
