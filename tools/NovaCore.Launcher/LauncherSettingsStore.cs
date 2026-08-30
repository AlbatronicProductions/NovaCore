using System.Text.Json;

namespace NovaCore.Launcher;

public sealed record LauncherSettings(
    NovaCoreScenarioPreset Preset,
    double? AltitudeMetres,
    NovaCoreWindowMode WindowMode,
    NovaCoreResolutionPreset Resolution,
    NovaCoreDiagnosticsMode Diagnostics);

public static class LauncherSettingsStore
{
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NovaCore", "Launcher", "settings.json");

    public static LauncherSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return Default();
            }

            var json = File.ReadAllText(SettingsPath);
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(LauncherSettings.WindowMode), out _) ||
                !document.RootElement.TryGetProperty(nameof(LauncherSettings.Resolution), out _) ||
                !document.RootElement.TryGetProperty(nameof(LauncherSettings.Diagnostics), out _))
            {
                var legacy = JsonSerializer.Deserialize<LegacyLauncherSettings>(json);
                if (legacy is null || !Enum.IsDefined(legacy.Preset)) return Default();
                var definition = ScenarioCatalog.Get(legacy.Preset);
                return new(legacy.Preset, legacy.AltitudeMetres, definition.DefaultWindowMode,
                    definition.DefaultResolution, legacy.EnableVulkanValidation
                        ? NovaCoreDiagnosticsMode.VulkanValidation
                        : definition.DefaultDiagnostics);
            }

            var settings = JsonSerializer.Deserialize<LauncherSettings>(json);
            return settings is not null && Enum.IsDefined(settings.Preset) &&
                   Enum.IsDefined(settings.WindowMode) && Enum.IsDefined(settings.Resolution) &&
                   Enum.IsDefined(settings.Diagnostics)
                ? settings : Default();
        }
        catch (IOException)
        {
            return Default();
        }
        catch (UnauthorizedAccessException)
        {
            return Default();
        }
        catch (JsonException)
        {
            return Default();
        }
    }

    public static bool TrySave(LauncherSettings settings, out string? error)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, SettingsPath, true);
            error = null;
            return true;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static LauncherSettings Default() =>
        new(ScenarioCatalog.Default.Preset, ScenarioCatalog.Default.DefaultAltitudeMetres,
            ScenarioCatalog.Default.DefaultWindowMode, ScenarioCatalog.Default.DefaultResolution,
            ScenarioCatalog.Default.DefaultDiagnostics);

    private sealed record LegacyLauncherSettings(
        NovaCoreScenarioPreset Preset,
        double? AltitudeMetres,
        bool EnableVulkanValidation);
}
