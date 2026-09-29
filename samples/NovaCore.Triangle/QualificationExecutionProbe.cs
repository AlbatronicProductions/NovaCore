using System.Diagnostics;
using System.Runtime.InteropServices;

// Opt-in surface-qualification observation only. No production scheduling policy.
internal static class QualificationExecutionProbe
{
    internal readonly record struct Stamp(long Qpc,ulong Cycles,uint Processor,int Gen0,int Gen1,int Gen2,uint Thread);
    internal readonly record struct Sample(long BeginQpc,long EndQpc,ulong ThreadCycles,uint BeginProcessor,uint EndProcessor,int Gen0,int Gen1,int Gen2,uint Thread);
    internal static Stamp Read()
    {
        if(!QueryThreadCycleTime(GetCurrentThread(),out var cycles))throw new InvalidOperationException("Qualification thread-cycle query failed.");
        return new(Stopwatch.GetTimestamp(),cycles,GetCurrentProcessorNumber(),GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),GetCurrentThreadId());
    }
    internal static Sample Finish(Stamp before)
    {
        var after=Read();
        if(after.Thread!=before.Thread||after.Cycles<before.Cycles)throw new InvalidOperationException("Qualification thread identity changed.");
        return new(before.Qpc,after.Qpc,after.Cycles-before.Cycles,before.Processor,after.Processor,after.Gen0-before.Gen0,after.Gen1-before.Gen1,after.Gen2-before.Gen2,after.Thread);
    }
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentThread();
    [DllImport("kernel32.dll")] private static extern bool QueryThreadCycleTime(IntPtr thread,out ulong cycles);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessorNumber();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
