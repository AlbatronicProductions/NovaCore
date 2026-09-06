using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using NovaCore.Graphics;

internal static class WindowLifecycleTests
{
    // Measured SDK 1.4.357 performance warning: planetary.vert's uint layer at 11
    // is used by planetary_production.frag, not the generic planetary.frag pair.
    // It is a legal unused output; do not accept other locations, IDs or severities.
    private const string UnusedLayerWarning = "[native] Vulkan validation [warning][WARNING-Shader-OutputNotConsumed]: vkCreateGraphicsPipelines(): pCreateInfos[0] (SPIR-V Interface) [VK_SHADER_STAGE_VERTEX_BIT] has an Output value declared at Location 11 Component 0, but there is no corresponding Input declared in [VK_SHADER_STAGE_FRAGMENT_BIT].";
    internal static string VerifyDeployment(string root)
    {
        var directory = Path.Combine(root, "samples", "NovaCore.Triangle", "bin", GraphicsTestHarness.Configuration, "net10.0");
        var sample = Path.Combine(directory, "NovaCore.Triangle.exe");
        Require(File.Exists(sample), "Build the matching Triangle configuration before window tests: " + sample);
        var native = Path.Combine(directory, "NovaCore.Native.dll");
        Require(GraphicsTestHarness.Hash(native) == GraphicsTestHarness.Hash(Path.Combine(root, "build", GraphicsTestHarness.NativeDirectory, "NovaCore.Native.dll")), "Sample native DLL is stale.");
        var project = XDocument.Load(Path.Combine(root, "samples", "NovaCore.Triangle", "NovaCore.Triangle.csproj"));
        var shaders = project.Descendants("RuntimeShader").Select(e => Path.GetFileName(e.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))).ToArray();
        foreach (var shader in shaders)
            Require(GraphicsTestHarness.Hash(Path.Combine(directory, "shaders", shader)) ==
                GraphicsTestHarness.Hash(Path.Combine(root, "build", GraphicsTestHarness.NativeDirectory, "shaders", shader)), "Stale deployed shader: " + shader);
        Require(!Directory.Exists(Path.Combine(root, "shaders")), "Repository-root shader shadowing must be removed before validation.");
        Console.WriteLine($"Window deployment: sample={sample}; managedSha256={GraphicsTestHarness.Hash(Path.ChangeExtension(sample,"dll"))}; nativeSha256={GraphicsTestHarness.Hash(native)}; verifiedShaders={shaders.Length}");
        return sample;
    }

    internal static void VerifyValidation(string log)
    {
        foreach (var line in log.Split('\n'))
        {
            if (line.Contains("VUID-", StringComparison.Ordinal) || line.Contains("Vulkan validation [error]", StringComparison.Ordinal))
                throw new InvalidOperationException("Vulkan validation failure: " + line);
            if (line.Contains("Vulkan validation [warning]", StringComparison.Ordinal) && line.TrimEnd() != UnusedLayerWarning)
                throw new InvalidOperationException("Unclassified Vulkan warning: " + line);
        }
    }

    internal static void ValidationPolicyTest()
    {
        VerifyValidation(UnusedLayerWarning);
        foreach (var rejected in new[] { UnusedLayerWarning.Replace("Location 11", "Location 12"),
            UnusedLayerWarning.Replace("[warning]", "[error]"), UnusedLayerWarning.Replace("WARNING-Shader-OutputNotConsumed", "NEW-WARNING"),
            "VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645", "VUID-new-regression", "Vulkan validation [error][unnamed]: failure" })
        {
            var threw = false; try { VerifyValidation(rejected); } catch (InvalidOperationException) { threw = true; }
            Require(threw, "Validation policy must reject: " + rejected);
        }
    }

    internal static void Run()
    {
        Require(TerrainAssetRepository.TryFindRoot(out var root), "Repository root.");
        var sample = VerifyDeployment(root);
        // Production has startup windowed/borderless selection, no exclusive-fullscreen
        // or runtime mode-switch API. Exercise both actual startup paths twice.
        var failures = new List<Exception>();
        foreach (var borderless in new[] { false, true, false, true })
        {
            try { RunWindow(sample, root, borderless); }
            catch (Exception error) { failures.Add(error); Console.Error.WriteLine(error); }
        }
        if (failures.Count != 0) throw new AggregateException("Production window lifecycle failures", failures);
    }

    internal static void RunInputBoundary()
    {
        Require(TerrainAssetRepository.TryFindRoot(out var root), "Repository root.");
        var sample = VerifyDeployment(root);
        RunWindow(sample, root, false, false);
        RunWindow(sample, root, false, true);
    }

    private static void RunWindow(string sample, string root, bool borderless, bool? isolateInput = null)
    {
        var lines = new ConcurrentQueue<string>(); using var changed = new AutoResetEvent(false);
        var start = new ProcessStartInfo(sample) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("--scene=sol"); start.ArgumentList.Add("--solar-epoch=j2000"); start.ArgumentList.Add("--log=startup,vulkan,validation,camera,input");
        start.Environment["NOVACORE_WINDOW_CLIENT_WIDTH"] = "640";
        start.Environment["NOVACORE_WINDOW_CLIENT_HEIGHT"] = "480";
        start.Environment["NOVACORE_WINDOW_BORDERLESS"] = borderless ? "1" : "0";
        var inputProbe = isolateInput == true ? Path.Combine(root, "build", "regional-live-tests", Guid.NewGuid().ToString("N")) : null;
        if (inputProbe != null) { Directory.CreateDirectory(inputProbe); start.Environment["NOVACORE_REGIONAL_PHYSICAL_PROBE"] = inputProbe; }
        using var process = new Process { StartInfo = start };
        process.OutputDataReceived += Receive; process.ErrorDataReceived += Receive;
        void Receive(object sender, DataReceivedEventArgs e) { if (e.Data != null) { lines.Enqueue(e.Data); changed.Set(); } }
        process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
        try
        {
            Wait(() => lines.Any(l => l.Contains("Swapchain: ")), "initial swapchain");
            process.Refresh(); var window = process.MainWindowHandle;
            Require(window != IntPtr.Zero && IsWindowVisible(window), "Production creates a visible Win32 window.");
            var loaded = process.Modules.Cast<ProcessModule>().Single(m => m.ModuleName.Equals("NovaCore.Native.dll", StringComparison.OrdinalIgnoreCase)).FileName;
            Require(string.Equals(loaded, Path.Combine(Path.GetDirectoryName(sample)!, "NovaCore.Native.dll"), StringComparison.OrdinalIgnoreCase), "Unexpected sample native module.");
            Console.WriteLine($"Window loaded module: {loaded}; sha256={GraphicsTestHarness.Hash(loaded)}; borderless={borderless}; visible=true");
            Require(GetClientRect(window, out var rect) && rect.Right == 640 && rect.Bottom == 480, "Requested client extent.");
            var style = GetWindowLongW(window, -16);
            Require(((style & 0x80000000u) != 0) == borderless, "Requested window style.");
            if (isolateInput != null) Require(PostMessageW(window, 0x20A, new IntPtr(120 << 16), IntPtr.Zero), "Deliver one wheel detent to this test's production window.");
            Require(SetWindowPos(window, IntPtr.Zero, 0, 0, 800, 600, 0x16), "Resize production window.");
            Wait(() => lines.Any(l => l.Contains("Swapchain recreated after resize")), "resized swapchain");
            Require(GetClientRect(window, out rect) && rect.Right > 0 && rect.Bottom > 0, "Resized client extent.");
            var resizedExtent = $"extent={rect.Right}x{rect.Bottom}";
            Wait(() => lines.Any(l => l.Contains(resizedExtent)), "swapchain matches client extent");
            var resizeCount = lines.Count(l => l.Contains("Swapchain recreated after resize"));
            ShowWindowAsync(window, 6);
            Wait(() => IsIconic(window), "minimized window");
            Require(GetClientRect(window, out rect) && rect.Right == 0 && rect.Bottom == 0, "Minimized zero client extent.");
            ShowWindowAsync(window, 9);
            Wait(() => !IsIconic(window) && lines.Count(l => l.Contains("Swapchain recreated after resize")) > resizeCount, "restored swapchain");
            Require(PostMessageW(window, 0x10, IntPtr.Zero, IntPtr.Zero), "Close through production WM_CLOSE.");
            Require(process.WaitForExit(60_000), "Production teardown did not finish.");
            process.WaitForExit();
            var log = string.Join('\n', lines);
            Require(process.ExitCode == 0 && !IsWindow(window), "Clean process/window teardown.");
            Require(log.Contains("Average frame time:") && !log.Contains("(0 frames)"), "Frames acquired, submitted and presented before shutdown.");
            Require(log.Contains(GraphicsTestHarness.Configuration == "Debug" ? "Enabled layer: VK_LAYER_KHRONOS_validation" : "Vulkan validation layer: disabled (Release)"), "Native validation configuration.");
            Console.WriteLine(log);
            if (isolateInput != null)
            {
                Require(log.Contains("WM_MOUSEWHEEL"), "Injected wheel reached the real window procedure.");
                Require(log.Contains("Solar orbit distance=") == !isolateInput.Value, "Diagnostic wheel isolation must preserve ordinary wheel input and suppress probe input.");
                Console.WriteLine($"Wheel boundary PASS: diagnosticIsolation={isolateInput}; nativeMessageDelivered=true; solarZoom={!isolateInput.Value}");
            }
            VerifyValidation(log);
            Console.WriteLine($"Window lifecycle PASS: borderless={borderless}; resize/minimize/restore/close; nativeHandlesReleased=true");
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(); }
            if (inputProbe != null && !Directory.EnumerateFileSystemEntries(inputProbe).Any()) Directory.Delete(inputProbe);
        }
        void Wait(Func<bool> condition, string responsibility)
        {
            var deadline = Stopwatch.StartNew();
            while (!condition())
            {
                if (process.HasExited || deadline.Elapsed > TimeSpan.FromSeconds(60))
                    throw new InvalidOperationException("Window checkpoint failed: " + responsibility + "\n" + string.Join('\n', lines));
                changed.WaitOne(20);
            }
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern uint GetWindowLongW(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern bool PostMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
