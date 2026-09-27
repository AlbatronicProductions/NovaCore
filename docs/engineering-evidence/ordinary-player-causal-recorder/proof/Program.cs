using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

// CPU-only persistence-order proof. No NovaCore/Vulkan references or GPU calls.
// Reset is a model cut that discards every write not acknowledged durable.
// This proves an indistinguishable-prefix counterexample, not hardware behavior.
internal static class Program
{
    private const int Bytes = 64;
    private const ulong Magic = 0x31464F4F5250434E;
    private static int checks;
    private enum Kind : ulong { Session, Completed, FrameEnter, SubmitEnter, SubmitReturn, PresentEnter, PresentReturn, FrameReturn }
    private readonly record struct Event(ulong Sequence, Kind Kind, ulong Frame, ulong Submission, long Result = 0);

    private static byte[] Encode(Event e)
    {
        var b = new byte[Bytes];
        ulong[] words = [Magic, e.Sequence, (ulong)e.Kind, e.Frame, e.Submission, unchecked((ulong)e.Result)];
        for (int i = 0; i < words.Length; i++) BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(i * 8), words[i]);
        SHA256.HashData(b.AsSpan(0, 48)).AsSpan(0, 8).CopyTo(b.AsSpan(48));
        BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(56), e.Sequence);
        return b;
    }
    private static List<Event> Parse(ReadOnlySpan<byte> bytes)
    {
        var events = new List<Event>();
        for (int at = 0; at + Bytes <= bytes.Length; at += Bytes)
        {
            var b = bytes.Slice(at, Bytes);
            // Local array is parser-only; no hot-path performance claim is made.
            var bytesCopy = b.ToArray();
            ulong W(int index) => BinaryPrimitives.ReadUInt64LittleEndian(bytesCopy.AsSpan(index * 8));
            if (W(0) != Magic || W(1) != (ulong)events.Count + 1 || W(7) != W(1) ||
                !SHA256.HashData(b[..48]).AsSpan(0, 8).SequenceEqual(b.Slice(48, 8))) break;
            events.Add(new(W(1), (Kind)W(2), W(3), W(4), unchecked((long)W(5))));
        }
        return events;
    }
    private static byte[] Join(IEnumerable<Event> events) => events.SelectMany(Encode).ToArray();
    private static void Require(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void DurableWrite(string file, byte[] bytes)
    {
        using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        stream.Write(bytes); stream.Flush(flushToDisk: true);
    }
    private static void Main(string[] args)
    {
        if (args.Length != 1 || Directory.Exists(args[0])) throw new ArgumentException("Use one fresh proof-output directory.");
        var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
        Event[] prefix = [new(1, Kind.Session, 0, 0), new(2, Kind.Completed, 40, 40)];
        Event[] submitStall = [..prefix, new(3, Kind.FrameEnter, 41, 0), new(4, Kind.SubmitEnter, 41, 41)];
        Event[] presentStall = [..prefix, new(3, Kind.FrameEnter, 41, 0), new(4, Kind.SubmitEnter, 41, 41),
            new(5, Kind.SubmitReturn, 41, 41), new(6, Kind.PresentEnter, 41, 41)];
        // Current NovaCore submits/presents in Draw; fence completion is inspected
        // in the following Update. Do not move completion ahead of Present.
        Event[] allReturned = [..presentStall, new(7, Kind.PresentReturn, 41, 41), new(8, Kind.FrameReturn, 41, 41),
            new(9, Kind.FrameEnter, 42, 0), new(10, Kind.Completed, 42, 41)];
        // A background writer has persisted only prefix. Distinct suffixes are
        // already committed in volatile memory when whole-machine reset occurs.
        byte[] durable = Join(prefix);
        DurableWrite(Path.Combine(output, "submit-stall-after-reset.bin"), durable);
        DurableWrite(Path.Combine(output, "present-stall-after-reset.bin"), durable);
        DurableWrite(Path.Combine(output, "returned-after-reset.bin"), durable);
        var a = File.ReadAllBytes(Path.Combine(output, "submit-stall-after-reset.bin"));
        var b = File.ReadAllBytes(Path.Combine(output, "present-stall-after-reset.bin"));
        var c = File.ReadAllBytes(Path.Combine(output, "returned-after-reset.bin"));
        Require(a.SequenceEqual(b) && b.SequenceEqual(c), "Distinct terminal histories must have identical durable bytes.");
        Require(!Join(submitStall).SequenceEqual(Join(presentStall)), "Volatile histories differ.");
        Require(Parse(a).Count == 2 && Parse(a)[^1].Submission == 40, "Only prior completion is proven.");
        Require(Parse(Join(submitStall))[^1].Kind == Kind.SubmitEnter, "Surviving observer could identify submit marker.");
        Require(Parse(Join(presentStall))[^1].Kind == Kind.PresentEnter, "Surviving observer could identify present marker.");
        Require(Parse(Join(allReturned))[^1].Kind == Kind.Completed, "Third history reaches next-update completion after present returned.");

        // Positive example is achievable when worker flush actually completes.
        DurableWrite(Path.Combine(output, "present-stall-observer-survived.bin"), Join(presentStall));
        Require(Parse(File.ReadAllBytes(Path.Combine(output, "present-stall-observer-survived.bin")))[^1].Kind == Kind.PresentEnter,
            "Successful observer drain retains present boundary; this is not a hard-reset guarantee.");

        // Every partial final record recovers the prefix, never a fabricated end.
        var next = Encode(submitStall[2]);
        for (int cut = 0; cut < Bytes; cut++)
            Require(Parse(durable.Concat(next.Take(cut)).ToArray()).Count == 2, $"Torn final record cut {cut}.");
        Require(Parse(durable.Concat(next).ToArray()).Count == 3, "At exact complete-record boundary, third record parses.");
        for (int byteIndex = 0; byteIndex < Bytes; byteIndex++)
        {
            var bad = (byte[])next.Clone(); bad[byteIndex] ^= 1;
            Require(Parse(durable.Concat(bad).ToArray()).Count == 2, $"Corrupt record byte {byteIndex}.");
        }

        // Acknowledging enqueue/write is not acknowledging flush durability.
        ulong written = 6, durableAcknowledged = 2;
        Require(written > durableAcknowledged && Parse(b).Count == (int)durableAcknowledged, "Written versus durable distinction.");
        // Nominal cadence does not constrain a stalled scheduler/storage owner.
        const int cadenceMs = 10, writerStallMs = 10_000;
        Require(writerStallMs > cadenceMs && Parse(b).Count == 2, "No unconditional maximum durability lag follows from cadence.");
        // Even durable Enter is an entry marker, not a physical API-start proof.
        var beforeApi = Join(submitStall); var insideApi = Join(submitStall);
        Require(beforeApi.SequenceEqual(insideApi), "Reset just before API and stall inside API share entry-marker bytes.");
        var report = new {
            judgment = "COUNTEREXAMPLE_PROVEN", cpuOnly = true, checks,
            limitations = "Deterministic persistence model, not a machine-reset experiment or production recorder qualification. No performance claim.",
            durablePrefixSha256 = Convert.ToHexStringLower(SHA256.HashData(a)), durableRecords = 2,
            indistinguishableHistories = new[] {
                new { terminal = "submit 41 entered, no return", volatileRecords = submitStall.Length },
                new { terminal = "submission 41 returned; completion 40 proven; present 41 entered, no return", volatileRecords = presentStall.Length },
                new { terminal = "submission/present/frame 41 returned; next Update proves completion 41", volatileRecords = allReturned.Length }
            },
            parserCanProve = "Durable session and completion 40; everything after durable sequence 2 may be absent.",
            parserCannotProve = "Whether terminal work was submit, present, completed, or not admitted.",
            survivingObserverCase = "Successfully flushed present-entry marker retained; conditional on persistence owner survival and flush success.",
            partialRecordCuts = Bytes, corruptByteCases = Bytes,
            nominalCadenceMs = cadenceMs, simulatedWriterStallMs = writerStallMs,
            gpuExecution = false, applicationExecution = false
        };
        File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }
}
