using System.Globalization;

namespace NovaCore.Launcher;

public static class LaunchCommandBuilder
{
    public static IReadOnlyList<string> BuildArguments(NovaCoreLaunchConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var definition = ScenarioCatalog.Get(configuration.Preset);
        if (!definition.IsSupported || definition.Scene != configuration.Scene ||
            definition.StartingBody != configuration.StartingBody ||
            definition.PhysicalSurface != configuration.PhysicalSurface ||
            !string.Equals(definition.SurfaceSite, configuration.SurfaceSite, StringComparison.Ordinal))
        {
            throw new ArgumentException("Launch configuration does not match the authoritative scenario catalog.", nameof(configuration));
        }

        var supportsEarthAltitude = configuration.Scene == NovaCoreScene.Earth ||
                                    configuration is { Scene: NovaCoreScene.Solar, StartingBody: NovaCoreStartingBody.Earth };
        if (configuration.AltitudeMetres is { } altitude &&
            (!double.IsFinite(altitude) || altitude < ScenarioCatalog.MinimumTerrainSafeAltitudeMetres ||
             !supportsEarthAltitude))
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Altitude is invalid for this scene.");
        }

        var arguments = new List<string>(4)
        {
            $"--scene={SceneArgument(configuration.Scene)}"
        };
        if (configuration is { Scene: NovaCoreScene.Solar, StartingBody: NovaCoreStartingBody.Earth })
        {
            arguments.Add("--focus=earth");
        }
        if (configuration.AltitudeMetres is { } altitudeMetres)
        {
            arguments.Add($"--altitude={altitudeMetres.ToString("0.################", CultureInfo.InvariantCulture)}");
        }

        if (configuration.SurfaceSite is { Length: > 0 } site)
        {
            arguments.Add($"--surface-site={site}");
        }

        if (configuration.EnableVulkanValidation)
        {
            arguments.Add("--log=validation");
        }

        if (configuration.EnablePerformanceTelemetry)
        {
            arguments.Add("--log=vulkan");
        }

        if (configuration.PhysicalSurface == NovaCorePhysicalSurface.M12DNaturalTerrainCandidate)
        {
            arguments.Add("--physical-surface=m12d-natural-candidate");
        }

        return arguments;
    }

    public static IReadOnlyDictionary<string, string> BuildEnvironment(
        NovaCoreLaunchConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.ClientResolution.Width is < 320 or > 8192 ||
            configuration.ClientResolution.Height is < 320 or > 8192)
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Resolved client dimensions are outside the native window domain.");
        }

        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NOVACORE_WINDOW_CLIENT_WIDTH"] = configuration.ClientResolution.Width.ToString(CultureInfo.InvariantCulture),
            ["NOVACORE_WINDOW_CLIENT_HEIGHT"] = configuration.ClientResolution.Height.ToString(CultureInfo.InvariantCulture),
            ["NOVACORE_WINDOW_BORDERLESS"] = configuration.WindowMode == NovaCoreWindowMode.BorderlessFullscreen ? "1" : "0"
        };
        if (configuration.EnableVulkanValidation)
        {
            environment["VK_INSTANCE_LAYERS"] = "VK_LAYER_KHRONOS_validation";
        }

        return environment;
    }

    private static string SceneArgument(NovaCoreScene scene) => scene switch
    {
        NovaCoreScene.Solar => "sol",
        NovaCoreScene.Earth => "earth",
        NovaCoreScene.SubdivisionDiagnostic => "planetary-subdivision-diagnostic",
        _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, null)
    };
}
