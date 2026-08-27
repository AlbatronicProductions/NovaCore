using System.Text.Json;

namespace NovaCore.Launcher;

public sealed record LauncherSettings(
    NovaCoreScenarioPreset Preset,
    double? AltitudeMetres,
    bool EnableVulkanValidation);

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

            var settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsPath));
            return settings is not null && Enum.IsDefined(settings.Preset)
                ? settings
                : Default();
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
        new(ScenarioCatalog.Default.Preset, ScenarioCatalog.Default.DefaultAltitudeMetres, false);
}
