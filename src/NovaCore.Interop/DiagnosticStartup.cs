using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

namespace NovaCore.Interop;

// Separate fixed-size lifecycle channel: never makes the causal render ring multi-producer.
// Slots are write-once timestamps. Observer owns heartbeat/stop; UI and native own
// their respective milestones. No file I/O, polling thread, or per-frame allocation.
[SupportedOSPlatform("windows")]
public static unsafe class DiagnosticStartup
{
    public const long Magic = 0x315452415453434e, Version = 1, Bytes = 256;
    public const int UiReady = 6, Loading = 7, NativeBegin = 8, NativeReady = 9,
        Submission = 10, Completion = 11, Steady = 12, Shutdown = 13, Cleanup = 14, Exit = 15, Denied = 19;
    static readonly MemoryMappedFile? mapping;
    static readonly MemoryMappedViewAccessor? view;
    static readonly long* words;
    static DiagnosticStartup()
    {
        var name = Environment.GetEnvironmentVariable("NOVACORE_STARTUP_MAPPING");
        if (string.IsNullOrEmpty(name)) return;
        mapping = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWrite);
        view = mapping.CreateViewAccessor(0, Bytes);
        byte* pointer = null; view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer); words = (long*)pointer;
        if (words[0] != Magic || words[1] != Version || words[2] != Bytes || words[24] != Stopwatch.Frequency)
            throw new InvalidDataException("Diagnostic startup contract mismatch.");
        var owner = Interlocked.CompareExchange(ref words[3], Environment.ProcessId, 0);
        if (owner != 0 && owner != Environment.ProcessId) throw new InvalidDataException("Diagnostic startup owner mismatch.");
    }
    public static void Mark(int slot)
    {
        if (slot is < UiReady or > Denied) throw new ArgumentOutOfRangeException(nameof(slot));
        if (words != null) Interlocked.CompareExchange(ref words[slot], Stopwatch.GetTimestamp(), 0);
    }
    public static bool TryBeginLoading()
    {
        if (words == null) return true;
        var now = Stopwatch.GetTimestamp();
        if (Volatile.Read(ref words[5]) != 0 || Volatile.Read(ref words[Shutdown]) != 0 ||
            (now - Volatile.Read(ref words[4])) > Stopwatch.Frequency)
        { Mark(Denied); return false; }
        Mark(Loading); return true;
    }
}
