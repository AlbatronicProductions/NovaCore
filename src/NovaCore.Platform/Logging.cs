namespace NovaCore.Platform;

[Flags]
public enum LogCategory : uint
{
    None = 0,
    Startup = 1 << 0,
    Vulkan = 1 << 1,
    Precision = 1 << 2,
    Input = 1 << 3,
    Renderer = 1 << 4,
    Validation = 1 << 5,
    Camera = 1 << 6,
    All = Startup | Vulkan | Precision | Input | Renderer | Validation | Camera,
}

public readonly record struct LogOptions(LogCategory Enabled)
{
    public bool IsEnabled(LogCategory category) => (Enabled & category) != 0;

    public static bool TryParse(string[] arguments, out LogOptions options, out string? error)
    {
        var enabled = LogCategory.Startup;
        foreach (var argument in arguments)
        {
            if (argument == "--verbose-input") { enabled |= LogCategory.Input; continue; }
            if (!argument.StartsWith("--log=", StringComparison.Ordinal)) continue;
            foreach (var value in argument[6..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (value.Equals("all", StringComparison.OrdinalIgnoreCase)) { enabled |= LogCategory.All; continue; }
                if (!Enum.TryParse<LogCategory>(value, true, out var category) || category is LogCategory.None or LogCategory.All)
                {
                    options = default; error = $"Unknown log category '{value}'. Use startup, vulkan, precision, input, renderer, validation, camera, or all."; return false;
                }
                enabled |= category;
            }
        }
        options = new LogOptions(enabled); error = null; return true;
    }
}
