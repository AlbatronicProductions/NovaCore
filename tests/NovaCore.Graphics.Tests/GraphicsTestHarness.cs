using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static class GraphicsTestHarness
{
    internal static string RepositoryPath(params string[] parts)
    {
        if (!TerrainAssetRepository.TryFindRoot(out var root)) throw new InvalidOperationException("Repository root unavailable.");
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
#if DEBUG
    internal const string Configuration = "Debug";
    internal const string NativeDirectory = "native-ninja";
#else
    internal const string Configuration = "Release";
    internal const string NativeDirectory = "native-ninja-release";
#endif
    private static readonly HashSet<string> GpuTests =
    [
        "M12D-P2S3 spherical billboard GPU runtime proof",
        "M12D-P2S4 canonical natural terrain billboard binding",
        "M12D-P2S5C production spherical billboard runtime",
        "GPU physical-height preparation", "Displaced mesh and physical normals",
        "M12D-P2A canonical hashed cell field proof",
        "M12D-P2B multiscale natural terrain family proof",
        "M12D-P2C1 prepared natural terrain"
    ];
    internal static string Category(string name) => name is "Live NCSM1 regional physical residency" or "Production window lifecycle" or "Regional diagnostic wheel isolation" or "Generic grid and frames startup"
        ? "window" : GpuTests.Contains(name) ? "gpu" : "headless";

    internal static int Run(string[] args, (string Name, Action Test)[] tests)
    {
        if (!args.Any(a => a.StartsWith("--case=")) && !args.Contains("--list") && !args.Contains("--category=headless"))
        {
            try
            {
                using var environment = new VulkanValidationEnvironment(args.Contains("--ambient"));
                return RunSelected(args, tests);
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        return RunSelected(args, tests);
    }
    private static int RunSelected(string[] args, (string Name, Action Test)[] tests)
    {
        if (args.Contains("--native-gpu"))
        {
            if (args.Any(a => a is not ("--native-gpu" or "--ambient"))) { Console.Error.WriteLine("Use --native-gpu [--ambient]."); return 2; }
            return RunNativeGpu();
        }
        var exact = args.FirstOrDefault(a => a.StartsWith("--case="))?[7..];
        var filter = args.FirstOrDefault(a => a.StartsWith("--test="))?[7..];
        var category = args.FirstOrDefault(a => a.StartsWith("--category="))?[11..];
        if (args.Any(a => a != "--list" && a != "--ambient" && !a.StartsWith("--case=") && !a.StartsWith("--test=") && !a.StartsWith("--category=")) ||
            category is not (null or "headless" or "gpu" or "window"))
        { Console.Error.WriteLine("Use --list, --test=<substring>, --category=headless|gpu|window, or --native-gpu; --ambient preserves user layer discovery."); return 2; }
        var selected = tests.Where(t => (exact == null || t.Name == exact) &&
            (filter == null || t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) &&
            (category == null || Category(t.Name) == category)).ToArray();
        if (selected.Length == 0) { Console.Error.WriteLine("No matching Graphics tests."); return 2; }
        if (args.Contains("--list"))
        { foreach (var t in selected) Console.WriteLine($"{Category(t.Name)} | {t.Name}"); return 0; }
        if (exact != null)
        {
            try { VerifyNativeIdentity(); if (Category(exact) != "headless") VerifyValidationLayer(); selected.Single().Test(); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        // Dataset publication is intentionally process-wide and has no unload API.
        // Each regression owns a fresh process, including its native singleton lifetime.
        var failures = 0;
        foreach (var t in selected)
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!)
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(typeof(GraphicsTestHarness).Assembly.Location);
            start.ArgumentList.Add("--case=" + t.Name);
            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            child.WaitForExit();
            Console.Write(stdout.GetAwaiter().GetResult()); Console.Error.Write(stderr.GetAwaiter().GetResult());
            if (child.ExitCode != 0) failures++;
            Console.WriteLine($"{(child.ExitCode == 0 ? "PASS" : "FAIL")} [{Category(t.Name)}] {t.Name}");
        }
        Console.WriteLine($"Graphics {Configuration}: selected={selected.Length}; pass={selected.Length-failures}; fail={failures}; skip=0; excluded={tests.Length-selected.Length}");
        return failures == 0 ? 0 : 1;
    }

    internal static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)); }
    private static int RunNativeGpu()
    {
        VerifyValidationLayer();
        var failed = 0;
        foreach (var (name, shader) in new[] { ("NovaCoreFacilityVisibilityTests", "facility_visibility_test.comp.spv"), ("NovaCoreSurfaceMaterialCoordinatesTests", "surface_material_coordinates_test.comp.spv"), ("NovaCoreStellarProjectionTests", "stellar_glow.vert.spv") })
        {
            var start = new ProcessStartInfo(RepositoryPath("build", NativeDirectory, name + ".exe")) { UseShellExecute=false, RedirectStandardOutput=true, RedirectStandardError=true };
            start.ArgumentList.Add(RepositoryPath("build", NativeDirectory, "shaders", shader));
            using var child = Process.Start(start)!;
            var stdout=child.StandardOutput.ReadToEndAsync(); var stderr=child.StandardError.ReadToEndAsync(); child.WaitForExit();
            Console.Write(stdout.GetAwaiter().GetResult()); Console.Error.Write(stderr.GetAwaiter().GetResult());
            if(child.ExitCode!=0) failed++;
            Console.WriteLine($"{(child.ExitCode==0?"PASS":"FAIL")} [native-gpu] {name}");
        }
        Console.WriteLine($"Native GPU {Configuration}: pass={3-failed}; fail={failed}; skip=0"); return failed==0?0:1;
    }
    private static void VerifyValidationLayer()
    {
        var disable = Environment.GetEnvironmentVariable("VK_LOADER_LAYERS_DISABLE");
        if (!string.IsNullOrEmpty(disable) && disable != "VK_LAYER_OBS_HOOK")
            throw new InvalidOperationException("Validation-disabling overrides are not permitted; use the canonical profile or the narrowly scoped OBS ambient comparison.");
        uint count = 0;
        if (vkEnumerateInstanceLayerProperties(ref count, IntPtr.Zero) != 0) throw new InvalidOperationException("Vulkan layer enumeration failed.");
        const int propertySize = 520; // VkLayerProperties: name[256], two uint32, description[256].
        var buffer = Marshal.AllocHGlobal(checked((int)count * propertySize));
        try
        {
            if (vkEnumerateInstanceLayerProperties(ref count, buffer) != 0) throw new InvalidOperationException("Vulkan layer enumeration failed.");
            var names = Enumerable.Range(0, (int)count).Select(i => Marshal.PtrToStringAnsi(buffer + i * propertySize)).ToArray();
            Console.WriteLine("Loader-visible layers: " + string.Join(", ", names));
            if (!names.Contains("VK_LAYER_KHRONOS_validation"))
                throw new InvalidOperationException("GPU validation requires VK_LAYER_KHRONOS_validation; install the Vulkan SDK. A layer-free run is not a validation PASS.");
            if (VulkanValidationEnvironment.IsCanonical && names.Any(n => n != "VK_LAYER_KHRONOS_validation"))
                throw new InvalidOperationException("Canonical discovery was contaminated by another layer.");
            Console.WriteLine("GPU prerequisite: VK_LAYER_KHRONOS_validation available; query/proof contexts enable it in both configurations.");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    [DllImport("vulkan-1.dll")] private static extern int vkEnumerateInstanceLayerProperties(ref uint count, IntPtr properties);
    private static void VerifyNativeIdentity()
    {
        if (!TerrainAssetRepository.TryFindRoot(out var root)) throw new InvalidOperationException("Repository root unavailable.");
        var deployed = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "NovaCore.Native.dll"));
        var source = Path.Combine(root, "build", NativeDirectory, "NovaCore.Native.dll");
        if (Hash(deployed) != Hash(source)) throw new InvalidOperationException("Native deployment differs from the selected build: " + source);
        var handle = NativeLibrary.Load(deployed);
        NativeLibrary.SetDllImportResolver(typeof(NativeRuntime).Assembly,
            (name, _, _) => name == "NovaCore.Native" ? handle : IntPtr.Zero);
        if (NativeRuntime.GetAbiLayout(out _) != NativeResult.Success) throw new InvalidOperationException("Native ABI query failed.");
        var loaded = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Single(m => m.ModuleName.Equals("NovaCore.Native.dll", StringComparison.OrdinalIgnoreCase)).FileName;
        if (!string.Equals(loaded, deployed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected loaded native path: " + loaded);
        Console.WriteLine($"Native identity: configuration={Configuration}; loaded={loaded}; sha256={Hash(loaded)}; source={source}");
    }
}
