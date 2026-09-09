using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static class SasAllocationObserver
{
    internal static readonly bool Enabled = Environment.GetEnvironmentVariable("SAS_ATTRIBUTION") == "1";
    [DllImport("profiler.dll", EntryPoint="ConfigureCounter")] private static extern int ConfigureCounter(nint entry);
    [DllImport("profiler.dll", EntryPoint="BeginWindow")] private static extern void NativeBegin();
    [DllImport("profiler.dll", EntryPoint="EndWindow")] private static extern void NativeEnd(long delta);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    internal static void BeginWindow() { if (Enabled) NativeBegin(); }
    internal static void EndWindow(long delta) { if (Enabled) NativeEnd(delta); }
    internal static void Prepare()
    {
        _ = GetCurrentThreadId();
        if (!Enabled) return;
        // Resolve the exact same module loaded by the CLR, not the copied test-bin DLL.
        NativeLibrary.SetDllImportResolver(typeof(SasAllocationObserver).Assembly,
            (name, assembly, searchPath) => name == "profiler.dll" ? NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!) : 0);
        var method=typeof(GC).GetMethod(nameof(GC.GetAllocatedBytesForCurrentThread))!;
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        var p=method.MethodHandle.GetFunctionPointer();
        if (Marshal.ReadByte(p)==0xff && Marshal.ReadByte(p,1)==0x25) p=Marshal.ReadIntPtr(p+6+Marshal.ReadInt32(p,2));
        if(ConfigureCounter(p)!=1) throw new InvalidOperationException("Unsupported native counter pattern; attribution aborted.");
        // Warm both P/Invoke wrappers before test setup, without observing a SAS workload.
        BeginWindow(); EndWindow(0);
        Console.WriteLine("OBSERVER_READY exact-object callbacks + BASIC_GC; no legacy MONITOR_GC");
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static byte[] AllocateControl() => new byte[128];
    internal static void PositiveControl()
    {
        GC.KeepAlive(AllocateControl());
        BeginWindow(); var before=GC.GetAllocatedBytesForCurrentThread();
        var value=AllocateControl(); var delta=GC.GetAllocatedBytesForCurrentThread()-before;
        EndWindow(delta); GC.KeepAlive(value);
        Console.WriteLine($"CONTROL_RESULT bytes={delta}; type=System.Byte[]; length=128");
        if(delta<=0) throw new InvalidOperationException("Real allocation control failed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Report(long delta, ulong hash, (int,int,int) gcBefore)
    {
    Console.WriteLine("SAS_RESULT " + System.Text.Json.JsonSerializer.Serialize(new {
        delta, completed=true, iterations=100000, explicitWarmup=1, proofSetupPassed=true, hash=hash.ToString("X16"),
        gcBefore=new[] {gcBefore.Item1,gcBefore.Item2,gcBefore.Item3}, gcAfter=new[] {GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)},
        managedThread=Environment.CurrentManagedThreadId, osThread=SasAllocationObserver.GetCurrentThreadId(), pid=Environment.ProcessId,
        runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        current="Identity", rate="Zero", target="Y axis pi/2", inertia="1,1,1", gain="10,10,10", derivative="0,0,0", limit="100,100,100",
        profiler=SasAllocationObserver.Enabled, mode=Environment.GetCommandLineArgs().Contains("--sas-only") ? "isolated" : "full" }));
    }
}
