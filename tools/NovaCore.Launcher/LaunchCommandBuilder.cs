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
            !string.Equals(definition.SurfaceSite, configuration.SurfaceSite, StringComparison.Ordinal))
        {
            throw new ArgumentException("Launch configuration does not match the authoritative scenario catalog.", nameof(configuration));
        }

        if (configuration.AltitudeMetres is { } altitude &&
            (!double.IsFinite(altitude) || altitude < ScenarioCatalog.MinimumTerrainSafeAltitudeMetres ||
             configuration.Scene != NovaCoreScene.Earth))
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Altitude is invalid for this scene.");
        }

        var arguments = new List<string>(4)
        {
            $"--scene={SceneArgument(configuration.Scene)}"
        };
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

        return arguments;
    }

    private static string SceneArgument(NovaCoreScene scene) => scene switch
    {
        NovaCoreScene.Solar => "sol",
        NovaCoreScene.Earth => "earth",
        NovaCoreScene.PlanetaryDiagnostic => "planetary-diagnostic",
        NovaCoreScene.SubdivisionDiagnostic => "planetary-subdivision-diagnostic",
        NovaCoreScene.AnchoredBillboardDiagnostic => "planetary-anchored-billboard-diagnostic",
        _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, null)
    };
}
