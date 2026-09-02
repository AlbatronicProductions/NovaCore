namespace NovaCore.Launcher;

public enum NovaCoreScenarioPreset
{
    SolarSystemOverview,
    EarthFarOrbital,
    Earth700Km,
    FloridaLaunchSite,
    SubdivisionDiagnostic,
    EarthFullscreenNative,
    M12DNaturalTerrainCandidate,
    M12DSphericalBillboardGpuProof
}

public enum NovaCoreScene
{
    Solar,
    Earth,
    SubdivisionDiagnostic,
    SphericalBillboardGpuProof
}

public enum NovaCoreStartingBody
{
    None,
    Earth
}

public enum NovaCoreWindowMode
{
    Windowed,
    BorderlessFullscreen
}

public enum NovaCoreResolutionPreset
{
    NativeDesktop,
    Resolution3440x1440,
    Resolution2560x1440,
    Resolution1920x1080,
    Resolution1280x720,
    Resolution960x540
}

public enum NovaCoreDiagnosticsMode
{
    Normal,
    PerformanceTelemetry,
    VulkanValidation,
    VulkanValidationAndPerformance
}

public enum NovaCorePhysicalSurface
{
    Generation3,
    M12DNaturalTerrainCandidate
}

public readonly record struct NovaCoreClientResolution(int Width, int Height)
{
    public override string ToString() => $"{Width}×{Height}";
}

public sealed record NovaCoreLaunchConfiguration(
    NovaCoreScenarioPreset Preset,
    NovaCoreScene Scene,
    NovaCoreStartingBody StartingBody,
    double? AltitudeMetres,
    string? SurfaceSite,
    NovaCoreWindowMode WindowMode,
    NovaCoreResolutionPreset ResolutionPreset,
    NovaCoreClientResolution ClientResolution,
    NovaCoreDiagnosticsMode Diagnostics,
    NovaCorePhysicalSurface PhysicalSurface = NovaCorePhysicalSurface.Generation3)
{
    public bool EnableVulkanValidation => Diagnostics is
        NovaCoreDiagnosticsMode.VulkanValidation or NovaCoreDiagnosticsMode.VulkanValidationAndPerformance;

    public bool EnablePerformanceTelemetry => Diagnostics is
        NovaCoreDiagnosticsMode.PerformanceTelemetry or NovaCoreDiagnosticsMode.VulkanValidationAndPerformance;
}

public sealed record NovaCoreScenarioDefinition(
    NovaCoreScenarioPreset Preset,
    string DisplayName,
    string Description,
    NovaCoreScene? Scene,
    NovaCoreStartingBody StartingBody,
    double? DefaultAltitudeMetres,
    string? SurfaceSite,
    NovaCoreWindowMode DefaultWindowMode,
    NovaCoreResolutionPreset DefaultResolution,
    NovaCoreDiagnosticsMode DefaultDiagnostics,
    NovaCorePhysicalSurface PhysicalSurface,
    bool IsSupported,
    string? UnsupportedReason)
{
    public override string ToString() => DisplayName;
}
