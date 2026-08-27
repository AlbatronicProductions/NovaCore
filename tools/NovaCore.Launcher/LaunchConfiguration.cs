namespace NovaCore.Launcher;

public enum NovaCoreScenarioPreset
{
    SolarSystemOverview,
    EarthFarOrbital,
    Earth700Km,
    FloridaSurface,
    FloridaLaunchSite,
    PlanetaryDiagnostic,
    SubdivisionDiagnostic,
    AnchoredBillboardDiagnostic
}

public enum NovaCoreScene
{
    Solar,
    Earth,
    PlanetaryDiagnostic,
    SubdivisionDiagnostic,
    AnchoredBillboardDiagnostic
}

public enum NovaCoreStartingBody
{
    None,
    Earth
}

public sealed record NovaCoreLaunchConfiguration(
    NovaCoreScenarioPreset Preset,
    NovaCoreScene Scene,
    NovaCoreStartingBody StartingBody,
    double? AltitudeMetres,
    string? SurfaceSite,
    bool EnableVulkanValidation);

public sealed record NovaCoreScenarioDefinition(
    NovaCoreScenarioPreset Preset,
    string DisplayName,
    string Description,
    NovaCoreScene? Scene,
    NovaCoreStartingBody StartingBody,
    double? DefaultAltitudeMetres,
    string? SurfaceSite,
    bool IsSupported,
    string? UnsupportedReason)
{
    public override string ToString() => DisplayName;
}
