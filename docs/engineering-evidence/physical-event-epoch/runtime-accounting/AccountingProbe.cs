using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static class AccountingProbe
{
    internal static bool OmitRegion;
    [DllImport("profiler.dll")] private static extern int ConfigureCounter(nint entry);
    [DllImport("profiler.dll")] internal static extern void BeginWindow();
    [DllImport("profiler.dll")] internal static extern void StopCapture(int id, long delta);
    [DllImport("profiler.dll")] internal static extern void DumpCapture();
    [DllImport("profiler.dll")] private static extern void ResetRecords();
    [DllImport("profiler.dll")] private static extern void CheckpointRecord(int id,long counter,long total,int c0,int c1,int c2,int thread,int region);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Point(int id, long counter, int region) => CheckpointRecord(id,counter,GC.GetTotalAllocatedBytes(false),GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),Environment.CurrentManagedThreadId,region);
    internal static void Prepare()
    {
        NativeLibrary.SetDllImportResolver(typeof(AccountingProbe).Assembly,(name,assembly,path)=>name=="profiler.dll"?NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!):0);
        var method=typeof(GC).GetMethod(nameof(GC.GetAllocatedBytesForCurrentThread))!;
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        var p=method.MethodHandle.GetFunctionPointer();
        if(Marshal.ReadByte(p)==0xff&&Marshal.ReadByte(p,1)==0x25)p=Marshal.ReadIntPtr(p+6+Marshal.ReadInt32(p,2));
        if(ConfigureCounter(p)!=1)throw new InvalidOperationException("Unsupported native counter pattern; probe invalid.");
        BeginWindow(); Point(0,GC.GetAllocatedBytesForCurrentThread(),0); StopCapture(0,0); ResetRecords();
        Console.WriteLine("OBSERVER_READY exact owner-thread ObjectAllocated callbacks; BASIC_GC; counter decoder verified");
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static byte[] AllocateControl()=>new byte[128];
    internal static void PositiveControl()
    {
        GC.KeepAlive(AllocateControl());
        BeginWindow(); var before=GC.GetAllocatedBytesForCurrentThread(); Point(90,before,0);
        var value=AllocateControl();var after=GC.GetAllocatedBytesForCurrentThread();
        Point(91,after,0);StopCapture(90,after-before);DumpCapture();GC.KeepAlive(value);
        Console.WriteLine($"POSITIVE_CONTROL type=System.Byte[] length=128 delta={after-before}");
        if(after-before!=152)throw new InvalidOperationException("Positive control accounting mismatch");
    }
}
