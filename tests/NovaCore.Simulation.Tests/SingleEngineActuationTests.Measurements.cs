using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Transactions;

internal static partial class SingleEngineActuationTests
{
    // Input production and test clock advancement are outside the proposal-only timing interval.
    private static void ReadyOperation(Fixture f, int i, bool edges = true)
    {
        if (edges)
        {
            f.Send(SpacecraftCommandIntent.Ignite());
            f.Send(SpacecraftCommandIntent.Shutdown());
            f.Send(SpacecraftCommandIntent.Ignite());
        }
        f.Send(SpacecraftCommandIntent.ThrottlePosition((i & 1) == 0 ? .25 : .75));
        f.Close();
    }
    private static bool PrepareAndCopy(Fixture f, out EngineActuationProposal token)
    {
        var result = f.Engine.PrepareSingleEngineActuation(f.Authority, f.Target, out token);
        return result == EnginePreparationStatus.Prepared &&
            f.Engine.PreviewSingleEngineActuation(f.Authority, token, out var preview) == EnginePreparationStatus.Preview &&
            preview.Start == f.Clock.CurrentTime && preview.End == f.Target &&
            preview.SourceCommandSequence == f.Requested().LastConsumedSequence &&
            preview.Meaning == EnginePreviewMeaning.ProposedIfApplied;
    }
    private static void NextOperation(Fixture f, EngineActuationProposal token)
    {
        Check(f.Engine.DiscardSingleEngineProposal(f.Authority, token) == EnginePreparationStatus.Retired, "measurement explicit retirement");
        f.Advance();
    }
    internal static void Allocation()
    {
        var complete = new Fixture();
        var noEdge = new Fixture();
        for (var i = 0; i < 128; i++)
        {
            ReadyOperation(complete, i); Check(PrepareAndCopy(complete, out var token), "complete warmup"); NextOperation(complete, token);
            ReadyOperation(noEdge, i, false); Check(PrepareAndCopy(noEdge, out token), "empty-range warmup"); NextOperation(noEdge, token);
        }
        bool ok = true;
        using (var measurement = new OrdinaryAllocationMeasurement("engine-complete"))
        {
            for (var i = 0; i < 1024; i++)
            {
                ReadyOperation(complete, i); ok &= PrepareAndCopy(complete, out var token); NextOperation(complete, token);
            }
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-complete");
        }
        Check(ok, "1024 complete command capture/consumption/preparation/preview operations");
        using (var measurement = new OrdinaryAllocationMeasurement("engine-no-edge-preparation"))
        {
            for (var i = 0; i < 1024; i++)
            {
                ReadyOperation(noEdge, i, false); ok &= PrepareAndCopy(noEdge, out var token); NextOperation(noEdge, token);
            }
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-no-edge-preparation");
        }
        Check(ok, "1024 no-engine-edge operations");
        // Isolate capture at the actual command commit, after cold binding and prior warmed calls.
        for (var i = 0; i < 7; i++) Check(complete.Admit(SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.Accepted, "capture inputs");
        using (var measurement = new OrdinaryAllocationMeasurement("engine-transition-capture"))
        {
            for (var i = 0; i < 7; i++) ok &= complete.Engine.CommitNextSpacecraftCommand(complete.Commands).Status == SpacecraftCommandStatus.Committed;
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-transition-capture");
        }
        complete.Close();
        EngineActuationProposal current;
        using (var measurement = new OrdinaryAllocationMeasurement("engine-preparation"))
        {
            ok &= complete.Engine.PrepareSingleEngineActuation(complete.Authority, complete.Target, out current) == EnginePreparationStatus.Prepared;
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-preparation");
        }
        // Warm the exact expected-refusal methods; do not replace them with exception paths.
        complete.Engine.PrepareSingleEngineActuation(complete.Authority, complete.Target, out _);
        complete.Engine.PreviewSingleEngineActuation(complete.Authority, default, out _);
        using (var measurement = new OrdinaryAllocationMeasurement("engine-preview"))
        {
            for (var i = 0; i < 1024; i++)
                ok &= complete.Engine.PreviewSingleEngineActuation(complete.Authority, current, out var preview) == EnginePreparationStatus.Preview && preview.EngineTransitionCount == 7;
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-preview");
        }
        using (var measurement = new OrdinaryAllocationMeasurement("engine-duplicate-refusal"))
        {
            for (var i = 0; i < 1024; i++)
            {
                ok &= complete.Engine.PrepareSingleEngineActuation(complete.Authority, complete.Target, out _) == EnginePreparationStatus.OutstandingProposal;
                ok &= complete.Engine.PreviewSingleEngineActuation(complete.Authority, default, out _) == EnginePreparationStatus.InvalidProposal;
            }
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "engine-duplicate-refusal");
        }
        Check(ok, "capture/preparation/preview/refusal independent outcomes");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("ENGINE_ALLOCATION PASS complete=0 no_edge=0 capture=0 preparation=0 preview=0 duplicate_refusal=0");
    }
    internal static void Cost()
    {
        var f = new Fixture();
        for (var i = 0; i < 256; i++)
        { ReadyOperation(f, i); Check(PrepareAndCopy(f, out var token), "cost warmup"); NextOperation(f, token); }
        var samples = new double[4096]; var ok = true;
        for (var i = 0; i < samples.Length; i++)
        {
            ReadyOperation(f, i);
            var before = Stopwatch.GetTimestamp();
            ok &= PrepareAndCopy(f, out var token);
            samples[i] = Stopwatch.GetElapsedTime(before).TotalMilliseconds;
            NextOperation(f, token);
        }
        Check(ok, "4096 complete transition consumption/preparation/copied preview results");
        Array.Sort(samples);
        Console.WriteLine($"ENGINE_PERFORMANCE warm=256 samples={samples.Length} median_ms={samples[samples.Length/2]:R} p95_ms={samples[(int)Math.Ceiling(samples.Length*.95)-1]:R} p99_ms={samples[(int)Math.Ceiling(samples.Length*.99)-1]:R} max_ms={samples[^1]:R} timing_runtime=ordinary");
        // All allocation in this isolated cold region is retained by the issued binding: one definition,
        // authority, storage object, capability seal and seven-element value array. No temporary fixture is counted.
        var cold = new Fixture(bind:false);
        EnginePreparationAuthority? authority;
        long retained;
        using (var measurement = new OrdinaryAllocationMeasurement("engine-cold-retained-storage"))
        {
            var definition = Definition();
            ok = cold.Engine.BindSingleEnginePreparation(cold.Commands, definition, out authority) == EnginePreparationStatus.Ready;
            retained = measurement.Complete();
        }
        Check(ok, "retained storage binding"); GC.KeepAlive(authority); GC.KeepAlive(cold);
        Console.WriteLine($"ENGINE_STORAGE retained_graph_bytes={retained} objects=5 edge_capacity={SimulationTransactionEngine.EngineTransitionCapacity} edge_payload_bytes={Unsafe.SizeOf<CapturedEngineTransition>()*SimulationTransactionEngine.EngineTransitionCapacity} definition_value_bytes={Unsafe.SizeOf<IdealEngineDefinitionValues>()} preview_value_bytes={Unsafe.SizeOf<EngineActuationPreview>()} engine_reference_bytes={IntPtr.Size} growing_history_bytes=0");
    }
}
