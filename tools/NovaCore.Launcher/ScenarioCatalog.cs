namespace NovaCore.Launcher;

public static class ScenarioCatalog
{
    public const double MinimumTerrainSafeAltitudeMetres = 10.0;

    public static IReadOnlyList<NovaCoreScenarioDefinition> All { get; } =
    [
        new(NovaCoreScenarioPreset.SolarSystemOverview, "Solar System Overview",
            "Interactive Solar map using the normal production scene.", NovaCoreScene.Solar,
            NovaCoreStartingBody.None, null, null, NovaCoreWindowMode.Windowed,
            NovaCoreResolutionPreset.Resolution960x540, NovaCoreDiagnosticsMode.Normal, NovaCorePhysicalSurface.Generation3, true, null),
        new(NovaCoreScenarioPreset.EarthFarOrbital, "Earth Far / Orbital View",
            "Earth production renderer from a 3,000 km orbital starting altitude.", NovaCoreScene.Earth,
            NovaCoreStartingBody.Earth, 3_000_000.0, "land", NovaCoreWindowMode.Windowed,
            NovaCoreResolutionPreset.Resolution960x540, NovaCoreDiagnosticsMode.Normal, NovaCorePhysicalSurface.Generation3, true, null),
        new(NovaCoreScenarioPreset.Earth700Km, "Earth 700 km",
            "Earth production renderer from 700 km altitude.", NovaCoreScene.Earth,
            NovaCoreStartingBody.Earth, 700_000.0, "land", NovaCoreWindowMode.Windowed,
            NovaCoreResolutionPreset.Resolution960x540, NovaCoreDiagnosticsMode.Normal, NovaCorePhysicalSurface.Generation3, true, null),
        new(NovaCoreScenarioPreset.EarthFullscreenNative, "Earth — Fullscreen Native",
            "Solar production scene pre-focused on Earth at native desktop resolution with acceptance telemetry.", NovaCoreScene.Solar,
            NovaCoreStartingBody.Earth, 700_000.0, "land", NovaCoreWindowMode.BorderlessFullscreen,
            NovaCoreResolutionPreset.NativeDesktop, NovaCoreDiagnosticsMode.VulkanValidationAndPerformance, NovaCorePhysicalSurface.Generation3, true, null),
        new(NovaCoreScenarioPreset.M12DNaturalTerrainCandidate, "M12D Natural Terrain Candidate",
            "Opt-in Earth renderer candidate using the prepared M12D natural physical surface.", NovaCoreScene.Solar,
            NovaCoreStartingBody.Earth, 700_000.0, "land", NovaCoreWindowMode.BorderlessFullscreen,
            NovaCoreResolutionPreset.NativeDesktop, NovaCoreDiagnosticsMode.VulkanValidationAndPerformance,
            NovaCorePhysicalSurface.M12DNaturalTerrainCandidate, true, null),
        new(NovaCoreScenarioPreset.FloridaLaunchSite, "Florida Launch Site",
            "Solar scene focused on the existing anchored Florida launch site.", NovaCoreScene.Solar,
            NovaCoreStartingBody.Earth, null, "florida-launch", NovaCoreWindowMode.Windowed,
            NovaCoreResolutionPreset.Resolution960x540, NovaCoreDiagnosticsMode.Normal, NovaCorePhysicalSurface.Generation3, true, null),
        new(NovaCoreScenarioPreset.SubdivisionDiagnostic, "Subdivision Diagnostic",
            "Screen-space terrain subdivision diagnostic.", NovaCoreScene.SubdivisionDiagnostic,
            NovaCoreStartingBody.None, null, null, NovaCoreWindowMode.Windowed,
            NovaCoreResolutionPreset.Resolution960x540, NovaCoreDiagnosticsMode.Normal, NovaCorePhysicalSurface.Generation3, true, null)
    ];

    public static NovaCoreScenarioDefinition Default => Get(NovaCoreScenarioPreset.SolarSystemOverview);

    public static NovaCoreScenarioDefinition Get(NovaCoreScenarioPreset preset) =>
        All.First(definition => definition.Preset == preset);

    public static bool TryCreateConfiguration(
        NovaCoreScenarioPreset preset,
        double? altitudeMetres,
        NovaCoreWindowMode windowMode,
        NovaCoreResolutionPreset resolutionPreset,
        NovaCoreDiagnosticsMode diagnostics,
        int desktopWidth,
        int desktopHeight,
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

        if (!Enum.IsDefined(windowMode) || !Enum.IsDefined(resolutionPreset) || !Enum.IsDefined(diagnostics))
        {
            configuration = null;
            error = "Window, resolution, or diagnostics selection is invalid.";
            return false;
        }

        if (!TryResolveResolution(resolutionPreset, desktopWidth, desktopHeight, out var resolution))
        {
            configuration = null;
            error = "The selected display resolution is outside the native client-size domain.";
            return false;
        }

        configuration = new NovaCoreLaunchConfiguration(
            preset,
            definition.Scene.Value,
            definition.StartingBody,
            resolvedAltitude,
            definition.SurfaceSite,
            windowMode,
            resolutionPreset,
            resolution,
            diagnostics,
            definition.PhysicalSurface);
        error = null;
        return true;
    }

    public static bool TryResolveResolution(
        NovaCoreResolutionPreset preset,
        int desktopWidth,
        int desktopHeight,
        out NovaCoreClientResolution resolution)
    {
        resolution = preset switch
        {
            NovaCoreResolutionPreset.NativeDesktop => new(desktopWidth, desktopHeight),
            NovaCoreResolutionPreset.Resolution3440x1440 => new(3440, 1440),
            NovaCoreResolutionPreset.Resolution2560x1440 => new(2560, 1440),
            NovaCoreResolutionPreset.Resolution1920x1080 => new(1920, 1080),
            NovaCoreResolutionPreset.Resolution1280x720 => new(1280, 720),
            NovaCoreResolutionPreset.Resolution960x540 => new(960, 540),
            _ => default
        };
        return resolution.Width is >= 320 and <= 8192 && resolution.Height is >= 320 and <= 8192;
    }
}
