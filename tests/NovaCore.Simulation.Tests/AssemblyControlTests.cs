using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

internal static class AssemblyControlTests
{
    private static int checks;
    private const long Step = 15625;
    private static readonly AssemblyStockCatalog Catalog = AssemblyStockCatalog.LoadDefault();
    private static readonly CompiledAssemblyDesign Design = Catalog.Resolve("novacore.stock.SRV01.FourHorn");
    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("LIVE CONTROL: " + message); checks++; }
    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (Exception e) when (e is InvalidDataException or InvalidOperationException or OverflowException)
        { checks++; return; }
        throw new InvalidOperationException("LIVE CONTROL accepted " + message);
    }
    private static AssemblyLaunch Launch(int count = 16, AssemblyCommand[]? commands = null) =>
        new(Design, new(new(201), new(1), new(2), "Controlled SRV-01"), "control-proof",
            new(default, new(.1, 0, 0), DoubleQuaternion.Identity, default), default,
            commands ?? Enumerable.Repeat(new AssemblyCommand(false, null, 0, 0, Step), count).ToArray());
    private static AssemblyApplicationSession Live(int count = 16, int capacity = 256)
    {
        var session = AssemblyApplicationSession.Create(Launch(count));
        session.EnableLiveControl(capacity);
        return session;
    }
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {
        Check(s.Engine.ObserveAssemblyFlight(s.Authority, out var observation) == AssemblyFlightStatus.Ready, "physical observation");
        return observation;
    }
    private static AssemblyControlObservation Control(AssemblyApplicationSession s)
    {
        Check(s.Engine.ObserveAssemblyControl(s.Control!, out var observation) == AssemblyControlStatus.Ready, "control observation");
        return observation;
    }
    private static AssemblyControlResult Admit(AssemblyApplicationSession s, bool on)
    {
        var sequence = Control(s).AdmissionCount + 1L;
        return s.Engine.AdmitAssemblyControl(s.Control!, s.Control!.Identity, sequence, new(on));
    }
    private static void Credit(AssemblyApplicationSession s, long ticks)
    {
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority, Observe(s).HostSequence + 1, new(ticks)).Status == AssemblyFlightStatus.AcceptedCredit, "credit");
    }
    private static void Publish(AssemblyApplicationSession s)
    {
        Check(s.Engine.PrepareAssemblyFlight(s.Authority, out var proposal) == AssemblyFlightStatus.Prepared, "prepare");
        Check(s.Engine.PublishAssemblyFlight(s.Authority, proposal).Status == AssemblyFlightStatus.Published, "publish");
    }
    private static void Next(AssemblyApplicationSession s) { Credit(s, Step); Publish(s); }
    private static void SamePhysical(AssemblyApplicationSession a, AssemblyApplicationSession b)
    {
        var x = Observe(a); var y = Observe(b);
        Check(x.State == y.State && x.StateRevision == y.StateRevision && x.TimelineRevision == y.TimelineRevision && x.HistoryCount == y.HistoryCount, "complete canonical successor equality");
        Check(x.Clock.Time == y.Clock.Time && x.Clock.Debt == y.Clock.Debt && x.Clock.RateRemainder == y.Clock.RateRemainder, "clock equality");
        for (var i = 0; i < x.HistoryCount; i++)
        {
            Check(a.Engine.TryGetAssemblyHistory(a.Authority, i, out var ar) && b.Engine.TryGetAssemblyHistory(b.Authority, i, out var br) && ar == br, "complete immutable physical history equality");
        }
    }
    private static void Admission()
    {
        using var s = Live();
        var before = Observe(s);
        Check(Control(s).Requested == default && Control(s).AdmissionCount == 0, "fresh OFF/no pending input");
        var on = Admit(s, true);
        Check(on.Status == AssemblyControlStatus.Admitted && on.Admission.Frontier == 0 && on.Admission.HostSequence == 0 && on.Admission.Sequence == 1 && on.Admission.Effective.MainOn, "next unprepared interval selected by owner");
        Check(Observe(s) == before, "admission cannot write physics/resources/clock");
        Check(s.Launch.Plan.All(c => !c.Request.MainOn), "recorded Plan remains immutable OFF");
        var off = Admit(s, false);
        Check(off.Status == AssemblyControlStatus.Admitted && !off.Admission.Effective.MainOn, "ON then OFF arbitrates OFF");
        Check(Admit(s, true).Admission.Effective.MainOn == false, "later ON at same interval cannot reverse OFF");
        Check(s.Engine.TryGetAssemblyControlAdmission(s.Control!, 0, out var original) && original == on.Admission, "earlier admission not rewritten");
        var snapshot = Control(s);
        var retry = s.Engine.AdmitAssemblyControl(s.Control!, s.Control!.Identity, 1, new(true));
        Check(retry.Status == AssemblyControlStatus.Duplicate && retry.Admission == on.Admission && Control(s) == snapshot, "historical duplicate receipt cannot resurrect ON");
        Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity, 1, new(false)).Status == AssemblyControlStatus.InvalidSequence, "changed duplicate rejected");
        Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity, 5, new(true)).Status == AssemblyControlStatus.InvalidSequence, "future sequence rejected");
        Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity with { Vessel = new(999) }, 4, new(true)).Status == AssemblyControlStatus.InvalidIdentity, "foreign vessel");
        Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity with { Generation = 2 }, 4, new(true)).Status == AssemblyControlStatus.InvalidIdentity, "stale generation");
        Check(Control(s) == snapshot && Observe(s) == before, "all refusal paths preserve control and physical state");
        Next(s);
        Check(!Observe(s).State.AppliedCommand.MainOn, "OFF physically realized");
        Check(Admit(s, true).Admission.Effective.MainOn, "OFF arbitration expires at next interval");
        Credit(s, Step);
        Check(s.Engine.PrepareAssemblyFlight(s.Authority, out var proposal) == AssemblyFlightStatus.Prepared, "sealed live proposal");
        var prepared = Control(s);
        Check(Admit(s, false).Status == AssemblyControlStatus.OutstandingProposal && Control(s) == prepared, "outstanding command cannot be rewritten");
        Reject(() => s.Save(), "save outstanding proposal");
        Check(s.Engine.PublishAssemblyFlight(s.Authority, proposal).Status == AssemblyFlightStatus.Published && Observe(s).State.AppliedCommand.MainOn, "sealed ON survives refused OFF");
        Check(Admit(s, false).Status == AssemblyControlStatus.Admitted, "refused sequence may retry at next boundary");
        using var foreign = Live();
        Check(foreign.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity, 1, new(true)).Status == AssemblyControlStatus.InvalidAuthority, "foreign capability with equal numeric identity");
        Check(Task.Run(() => s.Engine.AdmitAssemblyControl(s.Control!, s.Control.Identity, 99, new(true)).Status).Result == AssemblyControlStatus.WrongOwnerThread, "wrong thread admission refused");
        using var reverse = Live();
        Admit(reverse, false); Check(!Admit(reverse, true).Admission.Effective.MainOn, "OFF then ON also OFF");
        using var full = Live(capacity: 2);
        Admit(full, true); Admit(full, false);
        var fullControl = Control(full); var fullPhysical = Observe(full);
        Check(Admit(full, true).Status == AssemblyControlStatus.Capacity && Control(full) == fullControl && Observe(full) == fullPhysical, "exact capacity refusal is atomic");
        using var terminal = Live(count: 1);
        Admit(terminal, true); Next(terminal);
        var terminalState = Observe(terminal);
        Check(Admit(terminal, false).Status == AssemblyControlStatus.Terminal && Observe(terminal) == terminalState, "terminal never renews horizon");
        using var stale = Live();
        stale.Clock.Pause();
        Check(stale.Engine.AdmitAssemblyControl(stale.Control!, stale.Control!.Identity, 1, new(true)).Status == AssemblyControlStatus.StaleSource, "clock mutation cannot bypass source guards");
    }
    private static void Retirement()
    {
        var s = Live(); Admit(s, true); Credit(s, Step);
        Check(s.Engine.PrepareAssemblyFlight(s.Authority, out var proposal) == AssemblyFlightStatus.Prepared, "prepare before retirement");
        s.Dispose(); s.Dispose();
        Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control!.Identity, 2, new(false)).Status == AssemblyControlStatus.Retired, "disposed admission");
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority, 2, new(Step)).Status == AssemblyFlightStatus.Invalidated, "disposed credit");
        Check(s.Engine.PrepareAssemblyFlight(s.Authority, out _) == AssemblyFlightStatus.Invalidated, "disposed preparation");
        Check(s.Engine.PublishAssemblyFlight(s.Authority, proposal).Status == AssemblyFlightStatus.Invalidated, "disposed publication");
        Reject(() => s.Save(), "retired save");
        var neverEnabled = AssemblyApplicationSession.Create(Launch());
        neverEnabled.Dispose();
        Reject(() => neverEnabled.EnableLiveControl(), "enable after disposal");
        Check(neverEnabled.Engine.BeginAssemblyControl(neverEnabled.Authority, 256, out _) == AssemblyControlStatus.StaleSource, "direct enable after disposal");
        Check(neverEnabled.Engine.AdmitAssemblyHostTime(neverEnabled.Authority, 1, new(Step)).Status == AssemblyFlightStatus.Invalidated, "uncontrolled disposed physical owner");
        var direct = AssemblyApplicationSession.Create(Launch());
        Check(direct.Engine.BeginAssemblyControl(direct.Authority, 256, out var capability) == AssemblyControlStatus.Ready && direct.Control is null, "engine grant independent of application convenience property");
        direct.Dispose();
        Check(direct.Engine.AdmitAssemblyControl(capability!, capability!.Identity, 1, new(true)).Status == AssemblyControlStatus.Retired, "direct grant revoked by physical-owner disposal");
    }
    private static AssemblyApplicationSession Trace(int fragment)
    {
        var s = Live(count: 32);
        for (var i = 0; i < 32; i++)
        {
            if (i == 2 || i == 8 || i == 19 || i == 27)
                Check(Admit(s, i == 2 || i == 19).Status == AssemblyControlStatus.Admitted, "trace admission");
            var remaining = Step;
            while (remaining > 0) { var credit = Math.Min(remaining, fragment); Credit(s, credit); remaining -= credit; }
            Publish(s);
        }
        return s;
    }
    private static void Equivalence()
    {
        using var live = Trace((int)Step);
        var plan = Enumerable.Range(0, 32).Select(i => new AssemblyCommand(i >= 2 && i < 8 || i >= 19 && i < 27, null, 0, 0, Step)).ToArray();
        using var recorded = AssemblyApplicationSession.Create(Launch(commands: plan));
        for (var i = 0; i < plan.Length; i++) Next(recorded);
        SamePhysical(live, recorded);
        foreach (var fragment in new[] { 50000, 16667, 6944, 4166 })
        {
            using var other = Trace(fragment); SamePhysical(live, other);
            using var restore = AssemblyApplicationSession.Restore(Catalog, other.Save());
            Check(other.Save().AsSpan().SequenceEqual(restore.Save()), "fragment-specific credit and admission bytes reproduce");
        }
        var v2 = recorded.Save();
        Check(AssemblyJson.Read<AssemblySaveData>(v2, 1_048_576).Schema == "novacore.assembly-runtime/2" && !System.Text.Encoding.UTF8.GetString(v2).Contains("\"live\""), "recorded v2 schema unchanged");
        using var oldRestore = AssemblyApplicationSession.Restore(Catalog, v2);
        Check(v2.AsSpan().SequenceEqual(oldRestore.Save()), "v2 roundtrip bytes");
    }
    private static void SaveReplay()
    {
        using var s = Live();
        Admit(s, true);
        var initialSave = s.Save();
        using var initialRestore = AssemblyApplicationSession.Restore(Catalog, initialSave);
        Check(initialSave.AsSpan().SequenceEqual(initialRestore.Save()) && Control(initialRestore).Requested.MainOn, "admitted but unpublished save");
        Check(initialRestore.Engine.AdmitAssemblyControl(s.Control!, s.Control!.Identity, 2, new(false)).Status == AssemblyControlStatus.InvalidAuthority, "restore mints fresh capability");
        Credit(s, Step);
        Check(s.Engine.PrepareAssemblyFlight(s.Authority, out var proposal) == AssemblyFlightStatus.Prepared && s.Engine.AbortAssemblyFlight(s.Authority, proposal) == AssemblyFlightStatus.Ready, "abort private proposal");
        Admit(s, false); Admit(s, true); Publish(s);
        Credit(s, 1); Admit(s, true); Credit(s, Step - 1); Publish(s);
        Admit(s, false);
        var saved = s.Save();
        using var restored = AssemblyApplicationSession.Restore(Catalog, saved);
        Check(saved.AsSpan().SequenceEqual(restored.Save()), "admission/credit/abort ordering roundtrip");
        for (var i = 2; i < 16; i++) { Next(s); Next(restored); }
        SamePhysical(s, restored);
        Check(s.Save().AsSpan().SequenceEqual(restored.Save()), "exact continuation bytes");
        var data = AssemblyJson.Read<AssemblySaveData>(saved, 1_048_576);
        void Bad(Func<AssemblyControlAdmission, AssemblyControlAdmission> mutate)
        {
            var entries = (AssemblyControlAdmission[])data.Live!.Admissions.Clone();
            entries[0] = mutate(entries[0]);
            var live = data.Live with { Admissions = entries, Digest = AssemblyJson.Digest(entries) };
            Reject(() => AssemblyApplicationSession.Restore(Catalog, AssemblyJson.Write(data with { Live = live })), "tampered admission");
        }
        Bad(a => a with { Sequence = 0 }); Bad(a => a with { Frontier = -1 });
        Bad(a => a with { HostSequence = -1 }); Bad(a => a with { Identity = a.Identity with { Generation = 2 } });
        Bad(a => a with { Effective = new(false) });
        Reject(() => AssemblyApplicationSession.Restore(Catalog, AssemblyJson.Write(data with { Live = data.Live! with { Digest = "changed" } })), "journal digest");
        Reject(() => AssemblyApplicationSession.Restore(Catalog, AssemblyJson.Write(data with { Schema = "novacore.assembly-runtime/2" })), "schema/journal mismatch");
        Reject(() => AssemblyApplicationSession.Restore(Catalog, AssemblyJson.Write(data with { Live = data.Live! with { Capacity = 257 } })), "unbounded capacity");
        using var unsupported = AssemblyApplicationSession.Create(Launch(commands: [new(true, null, 0, 0, Step)]));
        Reject(() => unsupported.EnableLiveControl(), "recorded nonneutral plan live binding");
    }
    internal static void Run()
    {
        checks = 0; Admission(); Retirement(); Equivalence(); SaveReplay();
        Console.WriteLine($"Live command authority PASS: {checks} checks");
    }
    internal static void Measure()
    {
        const int iterations = 32, intervals = 64;
        var sessions = Enumerable.Range(0, iterations).Select(_ => Live(intervals)).ToArray();
        using (var warm = Live(intervals)) for (var i = 0; i < intervals; i++) { Admit(warm, i % 2 == 0); Next(warm); }
        var samples = new double[iterations * intervals];
        var plan = Enumerable.Range(0, intervals).Select(i => new AssemblyCommand(i % 2 == 0, null, 0, 0, Step)).ToArray();
        var recordedSessions = Enumerable.Range(0, iterations).Select(_ => AssemblyApplicationSession.Create(Launch(commands: plan))).ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var gc0 = GC.CollectionCount(0); var gc1 = GC.CollectionCount(1); var gc2 = GC.CollectionCount(2);
        var index = 0;
        foreach (var s in sessions) for (var i = 0; i < intervals; i++)
        {
            var start = Stopwatch.GetTimestamp();
            Check(s.Engine.AdmitAssemblyControl(s.Control!, s.Control!.Identity, i + 1, new(i % 2 == 0)).Status == AssemblyControlStatus.Admitted, "measured admission");
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority, i + 1, new(Step)).Status == AssemblyFlightStatus.AcceptedCredit, "measured credit");
            Publish(s);
            samples[index++] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var collections = new[] { GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2 };
        Array.Sort(samples);
        Check(allocated == 0, "warmed admission + compile + physical preparation/publication exact zero allocation");
        var baseline = new double[samples.Length]; var bi = 0;
        foreach (var s in recordedSessions) for (var i = 0; i < intervals; i++)
        {
            var start = Stopwatch.GetTimestamp();
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority, i + 1, new(Step)).Status == AssemblyFlightStatus.AcceptedCredit, "baseline credit");
            Publish(s); baseline[bi++] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        Array.Sort(baseline);
        using var storage = AssemblyApplicationSession.Create(Launch());
        var cold = GC.GetAllocatedBytesForCurrentThread(); storage.EnableLiveControl();
        var coldBytes = GC.GetAllocatedBytesForCurrentThread() - cold;
        for (var i = 0; i < 256; i++) Admit(storage, i % 2 == 0);
        for (var i = 0; i < SimulationTransactionEngine.AssemblyHostCreditCapacity; i++) Credit(storage, 1);
        var saveBytes = storage.Save().Length;
        var refusalBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 2048; i++)
        {
            Check(storage.Engine.AdmitAssemblyControl(storage.Control!, storage.Control!.Identity, 257, new(true)).Status == AssemblyControlStatus.Capacity, "allocation capacity refusal");
            Check(storage.Engine.AdmitAssemblyControl(storage.Control!, storage.Control.Identity, 1, new(true)).Status == AssemblyControlStatus.Duplicate, "allocation duplicate receipt");
            Check(storage.Engine.PrepareAssemblyFlight(storage.Authority, out _) == AssemblyFlightStatus.AwaitingDebt, "allocation no-work");
        }
        var refusalBytes = GC.GetAllocatedBytesForCurrentThread() - refusalBefore;
        Check(refusalBytes == 0, "warmed refusal/duplicate/no-work zero allocation");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { stage = 1, samples = index, medianMs = samples[index / 2], p95Ms = samples[(int)(index * .95)], p99Ms = samples[(int)(index * .99)], maxMs = samples[^1], allocatedBytes = allocated, gc = collections, recordedBaseline = new { medianMs = baseline[bi / 2], p95Ms = baseline[(int)(bi * .95)], p99Ms = baseline[(int)(bi * .99)], maxMs = baseline[^1] }, refusalAllocatedBytes = refusalBytes, journalCapacity = 256, recordBytes = Unsafe.SizeOf<AssemblyControlAdmission>(), journalPayloadBytes = 256 * Unsafe.SizeOf<AssemblyControlAdmission>(), coldControlBytes = coldBytes, capacitySaveBytes = saveBytes }));
        foreach (var s in sessions) s.Dispose();
        foreach (var s in recordedSessions) s.Dispose();
    }
}
