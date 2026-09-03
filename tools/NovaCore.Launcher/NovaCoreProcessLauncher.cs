using System.Diagnostics;

namespace NovaCore.Launcher;

public sealed record NovaCoreLaunchPlan(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    string BuildConfiguration)
{
    public string DisplayCommand => string.Join(' ',
        new[] { Quote(FileName) }.Concat(Arguments.Select(Quote)));

    private static string Quote(string value) =>
        value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
}

public static class NovaCoreProcessLauncher
{
    private static readonly string SampleProjectRelativePath = Path.Combine(
        "samples", "NovaCore.Triangle", "NovaCore.Triangle.csproj");

    public static string DefaultBuildConfiguration
    {
        get
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }

    public static NovaCoreLaunchPlan CreatePlan(
        string repositoryRoot,
        NovaCoreLaunchConfiguration configuration,
        string? buildConfiguration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var root = Path.GetFullPath(repositoryRoot);
        var sampleProject = Path.Combine(root, SampleProjectRelativePath);
        if (!File.Exists(sampleProject))
        {
            throw new FileNotFoundException("The NovaCore sample project could not be found.", sampleProject);
        }

        var sceneArguments = LaunchCommandBuilder.BuildArguments(configuration);
        var environment = LaunchCommandBuilder.BuildEnvironment(configuration);
        var resolvedBuildConfiguration = buildConfiguration ?? DefaultBuildConfiguration;
        if (resolvedBuildConfiguration is not ("Debug" or "Release"))
            throw new ArgumentOutOfRangeException(nameof(buildConfiguration));
        var builtExecutable = Path.Combine(root, "samples", "NovaCore.Triangle", "bin",
            resolvedBuildConfiguration, "net10.0", "NovaCore.Triangle.exe");
        if (File.Exists(builtExecutable))
        {
            return new NovaCoreLaunchPlan(builtExecutable, sceneArguments, root, environment,
                resolvedBuildConfiguration);
        }

        var arguments = new List<string>(8 + sceneArguments.Count)
        {
            "run",
            "--project",
            sampleProject,
            "-c",
            resolvedBuildConfiguration,
            "--"
        };
        arguments.AddRange(sceneArguments);
        return new NovaCoreLaunchPlan("dotnet", arguments, root, environment,
            resolvedBuildConfiguration);
    }

    public static Process Launch(NovaCoreLaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var startInfo = new ProcessStartInfo
        {
            FileName = plan.FileName,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        foreach (var argument in plan.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Diagnostics are authoritative launcher state. Do not let a Vulkan
        // validation layer inherited from the launcher process turn Normal or
        // Performance-only launches into validation runs.
        startInfo.Environment.Remove("VK_INSTANCE_LAYERS");
        foreach (var (name, value) in plan.EnvironmentVariables)
        {
            startInfo.Environment[name] = value;
        }

        return Process.Start(startInfo) ??
            throw new InvalidOperationException("Windows did not start the NovaCore process.");
    }
}
