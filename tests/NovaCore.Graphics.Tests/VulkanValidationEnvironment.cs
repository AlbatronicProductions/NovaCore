using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json.Nodes;
using Microsoft.Win32;

// Test-process ownership only. Production launcher/runtime inherit the user's environment.
internal sealed class VulkanValidationEnvironment : IDisposable
{
    internal static bool IsCanonical => Environment.GetEnvironmentVariable("NOVACORE_CANONICAL_VULKAN") == "1";
    private readonly Dictionary<string, string?> previous = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? directory;
    internal VulkanValidationEnvironment(bool ambient)
    {
        Console.WriteLine(ambient ? "Vulkan profile: AMBIENT (failures remain failures)" : "Vulkan profile: CANONICAL (Khronos validation only)");
        if (ambient) { Set("NOVACORE_CANONICAL_VULKAN", null); DescribeAmbient(); return; }
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Graphics validation requires Windows.");
        using var identity = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Canonical Vulkan validation must run unelevated: the loader ignores discovery overrides in elevated processes.");
        var sdk = Environment.GetEnvironmentVariable("VULKAN_SDK") ?? Environment.GetEnvironmentVariable("VK_SDK_PATH")
            ?? throw new InvalidOperationException("Set VULKAN_SDK to the installed validation SDK.");
        var manifestPath = Path.GetFullPath(Path.Combine(sdk, "Bin", "VkLayer_khronos_validation.json"));
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
        var layer = manifest["layer"]!;
        if (layer["name"]!.GetValue<string>() != "VK_LAYER_KHRONOS_validation") throw new InvalidOperationException("Unexpected SDK layer.");
        var library = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath)!, layer["library_path"]!.GetValue<string>()));
        if (!File.Exists(library)) throw new FileNotFoundException("Validation layer binary missing.", library);
        layer["library_path"] = library;
        directory = Path.Combine(Path.GetTempPath(), "NovaCore-vulkan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "implicit"));
        Directory.CreateDirectory(Path.Combine(directory, "explicit"));
        File.WriteAllText(Path.Combine(directory, "explicit", "validation.json"), manifest.ToJsonString());
        // Remove inherited layer activation/settings, including validation-feature disabling.
        // Driver selection is not changed. The explicit manifest still points to the real SDK DLL.
        foreach (string key in Environment.GetEnvironmentVariables().Keys)
            if (new[] { "VK_LAYER", "VK_ADD_LAYER", "VK_IMPLICIT_LAYER", "VK_ADD_IMPLICIT_LAYER", "VK_LOADER_LAYERS", "VK_VALIDATION", "VK_INSTANCE_LAYERS" }
                .Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) Set(key, null);
        Set("VK_LAYER_PATH", Path.Combine(directory, "explicit"));
        Set("VK_IMPLICIT_LAYER_PATH", Path.Combine(directory, "implicit"));
        Set("VK_LAYER_SETTINGS_PATH", directory);
        Set("VK_INSTANCE_LAYERS", "VK_LAYER_KHRONOS_validation");
        Set("NOVACORE_CANONICAL_VULKAN", "1");
        Console.WriteLine($"Canonical layer: manifest={manifestPath}; binary={library}; sha256={GraphicsTestHarness.Hash(library)}; strict=true");
    }
    private void Set(string key, string? value)
    {
        previous.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }
    private static void DescribeAmbient()
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (string key in Environment.GetEnvironmentVariables().Keys)
            if (key.StartsWith("VK_") || key.StartsWith("DISABLE_VULKAN")) Console.WriteLine($"Ambient environment: {key}={Environment.GetEnvironmentVariable(key)}");
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (var kind in new[] { "ImplicitLayers", "ExplicitLayers" })
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var key = root.OpenSubKey(@"SOFTWARE\Khronos\Vulkan\" + kind);
            if (key == null) continue;
            foreach (var manifest in key.GetValueNames())
            {
                Console.WriteLine($"Ambient registration: hive={hive}; view={view}; kind={kind}; value={key.GetValue(manifest)}; manifest={manifest}; exists={File.Exists(manifest)}; loaded=not-inferred");
                if (!File.Exists(manifest)) continue;
                try
                {
                    var layer = JsonNode.Parse(File.ReadAllText(manifest))?["layer"];
                    var library = layer?["library_path"]?.GetValue<string>();
                    var binary = library == null ? null : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifest)!, library));
                    Console.WriteLine($"Ambient manifest: name={layer?["name"]}; binary={binary}; exists={File.Exists(binary)}; enable={layer?["enable_environment"]}; disable={layer?["disable_environment"]}");
                }
                catch (Exception error) { Console.WriteLine($"Ambient manifest unreadable: {error.Message}"); }
            }
        }
    }
    internal static void VerifyLoaded(Process process)
    {
        var modules = process.Modules.Cast<ProcessModule>().Where(m => m.ModuleName.Contains("vulkan", StringComparison.OrdinalIgnoreCase) ||
            m.ModuleName.StartsWith("VkLayer", StringComparison.OrdinalIgnoreCase) || m.ModuleName.Contains("graphics-hook", StringComparison.OrdinalIgnoreCase) ||
            m.ModuleName.Contains("overlay", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var module in modules) Console.WriteLine($"Vulkan module: {module.FileName}; sha256={GraphicsTestHarness.Hash(module.FileName)}");
        if (IsCanonical &&
            !modules.Any(m => m.ModuleName.Equals("VkLayer_khronos_validation.dll", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Requested validation layer is not loaded.");
    }
    public void Dispose()
    {
        foreach (var item in previous) Environment.SetEnvironmentVariable(item.Key, item.Value);
        if (directory == null) return;
        // Only the one generated manifest and three owned empty directories; never recurse.
        File.Delete(Path.Combine(directory, "explicit", "validation.json"));
        Directory.Delete(Path.Combine(directory, "explicit"));
        Directory.Delete(Path.Combine(directory, "implicit"));
        Directory.Delete(directory);
    }
}
