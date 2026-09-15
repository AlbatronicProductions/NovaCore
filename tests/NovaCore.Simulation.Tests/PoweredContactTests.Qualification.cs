using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class PoweredContactTests
{
    private static void Complete(Contact c)
    {
        var n = c.Engine.ObservePoweredContact(c.Power).Observation.Actuator.Frontier;
        var ticks = (n + 1) * 1_000_000 / 60 - n * 1_000_000 / 60;
        var credit = c.Engine.AdmitPoweredContactHostTime(c.Power, ++c.Sequence, new(ticks));
        if (credit.Status != PoweredFlightStatus.AcceptedCredit) throw new InvalidOperationException("Complete contact credit failed.");
        var result = c.Engine.ServicePoweredContactDebt(c.Power);
        if (result.PublishedCount != 1 || result.Status is not (PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.Completed))
            throw new InvalidOperationException("Complete contact operation failed: " + result.Status);
    }
    internal static void Sequences()
    {
        foreach (var fixture in new[] { PoweredContactFixture.CenteredEndpoint, PoweredContactFixture.OffCom })
        {
            using var reference = new Contact(fixture); reference.Credit(20_000_000);
            while (reference.Observe.HistoryCount < 1200)
            {
                var r = reference.Engine.ServicePoweredContactDebt(reference.Power);
                Check(r.Status is PoweredFlightStatus.BudgetExhausted or PoweredFlightStatus.Completed, "prefunded contact continuation " + r.Status);
            }
            foreach (var hz in new[] { 30, 60, 150, 240, -1 })
            {
                using var c = new Contact(fixture); long admitted = 0; var compared = 0; var shortIntervals = 0; var longIntervals = 0; var budget = 0; var supported = 0;
                var frames = hz > 0 ? 20 * hz : 400; var native = Native(c); var generation = c.World.Generation;
                for (var frame = 1; frame <= frames; frame++)
                {
                    var target = hz > 0 ? (long)frame * 20_000_000 / frames : (long)((frame + 1) / 2) * 100_000;
                    if (target > admitted) { c.Credit(target - admitted); admitted = target; }
                    var result = c.Engine.ServicePoweredContactDebt(c.Power);
                    Check(result.Status is PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.BudgetExhausted or PoweredFlightStatus.Completed, "host-partition contact continuation " + result.Status);
                    Check(result.PublishedCount <= 4, "bounded service"); if (result.Status == PoweredFlightStatus.BudgetExhausted) budget++;
                    while (compared < result.Observation.HistoryCount)
                    {
                        Check(c.Engine.TryGetPoweredFlightRecord(compared, out var actual) && reference.Engine.TryGetPoweredFlightRecord(compared, out var expected) &&
                            actual with { DebtBefore = default, DebtAfter = default } == expected with { DebtBefore = default, DebtAfter = default } &&
                            actual.Endpoint.SameBits(expected.Endpoint), "matching frontier exact physical/resource/actuator history");
                        var ticks = actual.Segmentation.Engine.End.Ticks - actual.Segmentation.Engine.Start.Ticks;
                        Check(actual.DebtBefore.Ticks - actual.DebtAfter.Ticks == ticks, "exact history debt conservation");
                        if (ticks == 16666) shortIntervals++; else if (ticks == 16667) longIntervals++; else Check(false, "tick lattice");
                        if (compared >= 600)
                        {
                            var e = actual.Endpoint; var lowest = double.PositiveInfinity;
                            for (var x = -1; x <= 1; x += 2) for (var y = -1; y <= 1; y += 2) for (var z = -1; z <= 1; z += 2)
                                lowest = Math.Min(lowest, e.PositionRoot.Y + e.BodyToRoot.Rotate(new(x, .5 * y, .5 * z)).Y);
                            Check(lowest >= -.001 && lowest <= .001 && Math.Abs(e.VelocityRoot.Y) <= .06, "independent supported dry endpoint"); supported++;
                        }
                        compared++;
                    }
                    var obs = c.Observe;
                    Check(admitted - obs.Clock.Time.Ticks == obs.Clock.Debt.Ticks && c.World.Generation == generation && Native(c) == native,
                        "accounting/world identity conservation");
                }
                var final = c.Observe;
                Check(compared == 1200 && shortIntervals == 400 && longIntervals == 800 && supported == 600 && final.StateRevision.Value == 1200 &&
                    final.Actuator.ActuatorRevision == 1200 && final.TimelineRevision.Value == 0 && final.Clock.Time.Ticks == 20_000_000 &&
                    final.Clock.Debt.Ticks == 0 && final.Resource.RemainingUnits.IsZero && final.Actuator.Activity == ActualEngineActivity.EnabledNoFeed, "long-sequence exact endpoint");
                Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Completed, "finite episode holds endpoint");
                Console.WriteLine($"POWERED_CONTACT_SEQUENCE {fixture} hz={hz} endpoints=1200 ticks=20000000 short=400 long=800 support=600 debt=0 budgetHits={budget} PASS");
            }
            var velocity = new Double3(32, -2, 5);
            using var moving = new Contact(fixture, frameVelocity: velocity);
            for (var i = 0; i < 1200; i++)
            {
                Complete(moving); var e = moving.Observe.Endpoint;
                Check(reference.Engine.TryGetPoweredFlightRecord(i, out var expected), "moving reference");
                var shift = velocity * (e.Epoch.Ticks / 1_000_000d);
                Check(e.PositionRoot == expected.Endpoint.PositionRoot + shift && e.VelocityRoot == expected.Endpoint.VelocityRoot + velocity &&
                    e.BodyToRoot == expected.Endpoint.BodyToRoot && e.AngularVelocityBody == expected.Endpoint.AngularVelocityBody, "original moving-frame epoch");
            }
            Check(NativeBits(moving).SequenceEqual(NativeBits(reference)), "moving-frame native trajectory identity");
            Console.WriteLine($"POWERED_CONTACT_MOVING {fixture} intervals=1200 original_frame_epoch=PASS nativeBits=PASS");
        }
    }

    internal static void Allocation()
    {
        using var warm = new Contact(); for (var i = 0; i < 128; i++) Complete(warm);
        using (var measurement = new OrdinaryAllocationMeasurement("powered-contact-dry-complete"))
        { for (var i = 0; i < 128; i++) Complete(warm); OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "contact dry complete"); }
        // Each genuine episode contains its event once. Prepare cold worlds before measuring;
        // do not re-fuel or reset a retained world to manufacture recurring powered work.
        var episodes = new Contact[17];
        try
        {
            for (var i = 0; i < episodes.Length; i++) episodes[i] = new(PoweredContactFixture.OffCom, capacity: 3);
            Complete(episodes[0]); Complete(episodes[0]);
            using (var measurement = new OrdinaryAllocationMeasurement("powered-contact-interior-exhaustion-to-dry"))
            { for (var i = 1; i < episodes.Length; i++) { Complete(episodes[i]); Complete(episodes[i]); } OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "contact exhaustion and continuation"); }
        }
        finally { foreach (var c in episodes) c?.Dispose(); }
        using var idle = new Contact(); for (var i = 0; i < 128; i++) _ = idle.Engine.ServicePoweredContactDebt(idle.Power);
        using (var measurement = new OrdinaryAllocationMeasurement("powered-contact-no-work"))
        { for (var i = 0; i < 256; i++) if (idle.Engine.ServicePoweredContactDebt(idle.Power).Status != PoweredFlightStatus.AwaitingDebt) throw new InvalidOperationException(); OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "contact no-work"); }
        warm.Credit(1_000_000);
        using (var measurement = new OrdinaryAllocationMeasurement("powered-contact-backlog"))
        { for (var i = 0; i < 15; i++) if (warm.Engine.ServicePoweredContactDebt(warm.Power).PublishedCount != 4) throw new InvalidOperationException(); OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "contact backlog"); }
        using var mapped = new Contact(PoweredContactFixture.OffCom); var p = mapped.Seal();
        var source = mapped.Observe.Endpoint; var force = source.BodyToRoot.Rotate(p.Engine.ProposedForceBodyNewtons); var moment = source.BodyToRoot.Rotate(p.Engine.ProposedMomentBodyNewtonMetres);
        for (var i = 0; i < 128; i++) OrdinaryContactInputProjection.TryMap(p.PoweredDuration, 16666, 1, force, moment, source.Properties.MassKilograms, 2, PoweredContactPreparation.GravityLocal, out _);
        using (var measurement = new OrdinaryAllocationMeasurement("powered-contact-input-projection"))
        { for (var i = 0; i < 256; i++) if (!OrdinaryContactInputProjection.TryMap(p.PoweredDuration, 16666, 1, force, moment, source.Properties.MassKilograms, 2, PoweredContactPreparation.GravityLocal, out _)) throw new InvalidOperationException(); OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), "contact mapper"); }
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("POWERED_CONTACT_ALLOCATION PASS five_exact_zero_windows positive_control=NONZERO");
    }

    private static void Distribution(string label, double[] samples, int[] beforeGc, int[] afterGc)
    {
        var worst = Array.IndexOf(samples, samples.Max()); Array.Sort(samples);
        var median = samples[512]; var p95 = samples[972]; var p99 = samples[1013]; var max = samples[1023];
        Console.WriteLine($"POWERED_CONTACT_COST {label} n=1024 warm=128 median_ms={median:R} p95_ms={p95:R} p99_ms={p99:R} max_ms={max:R} worstIndex={worst} gc={afterGc[0]-beforeGc[0]}/{afterGc[1]-beforeGc[1]}/{afterGc[2]-beforeGc[2]} ownerThreads=1 solver=8/1 synchronization=exclusive_owner_phase");
        Check(median <= .05 && p95 <= .10 && p99 <= .25 && max <= .50, "complete-path performance ceiling " + label);
    }
    private static int[] Collections() => [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    internal static void UnpoweredStillFuelledControl()
    {
        using var prime = new Contact(); for (var i = 0; i < 128; i++) Complete(prime);
        var coldBefore = GC.GetAllocatedBytesForCurrentThread(); var coldStart = Stopwatch.GetTimestamp();
        using var dry = new Contact(PoweredContactFixture.CenteredBaseline);
        var coldMs = Stopwatch.GetElapsedTime(coldStart).TotalMilliseconds; var coldManaged = GC.GetAllocatedBytesForCurrentThread() - coldBefore;
        var pool = dry.World.PoolBytes; var history = 24L + 1200L * Unsafe.SizeOf<PoweredFlightRecord>();
        Console.WriteLine($"POWERED_CONTACT_STORAGE managed_cold_upper_bound={coldManaged} native_pool={pool} combined_upper_bound={coldManaged+(long)pool} history_included={history} recordBytes={Unsafe.SizeOf<PoweredFlightRecord>()} cold_ms={coldMs:R}");
        Check(coldManaged >= history && coldManaged + (long)pool <= 8 * 1024 * 1024, "combined retained storage upper bound");
        for (var i = 0; i < 128; i++) Complete(dry);
        var samples = new double[1024]; var gc = Collections();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Complete(dry); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        Distribution("UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL", samples, gc, Collections());
        Check(dry.Observe.Resource.RemainingUnits == dry.Prepared.ResourceDefinition.InitialUnits &&
            dry.Observe.Actuator.Activity == ActualEngineActivity.Off, "control is OFF and still fuelled");
        Console.WriteLine("POWERED_CONTACT_CONTROL PASS fixture=CenteredBaseline engine=OFF fuel_kg=0.000013020833333333334");
    }

    internal static void Cost()
    {
        UnpoweredStillFuelledControl();
        var samples = new double[1024]; int[] gc;

        // Genuine events occur only at these frontiers. Report each cost explicitly rather than
        // pretending 1,024 coast operations are a population of 1,024 powered events.
        foreach (var fixture in new[] { PoweredContactFixture.CenteredEndpoint, PoweredContactFixture.OffCom, PoweredContactFixture.NearUnloading, PoweredContactFixture.TinyEvent })
        {
            using var c = new Contact(fixture); var eventCosts = new double[3]; var before = Collections();
            for (var i = 0; i < 3; i++)
            {
                var start = Stopwatch.GetTimestamp(); Complete(c); eventCosts[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                Check(c.Engine.TryGetPoweredFlightRecord(i, out var record), "event cost record");
                Console.WriteLine($"POWERED_CONTACT_EVENT_COST {fixture} frontier={i+1} classification={record.Segmentation.Classification} milliseconds={eventCosts[i]:R} physicalSteps=1");
                Check(eventCosts[i] <= .50, "real-frontier event maximum " + fixture);
            }
            var after = Collections(); Console.WriteLine($"POWERED_CONTACT_EVENT_GC {fixture} gc={after[0]-before[0]}/{after[1]-before[1]}/{after[2]-before[2]} samplePopulation=3_no_percentile_claim");
            if (fixture != PoweredContactFixture.OffCom) continue;
            for (var i = 0; i < 128; i++) Complete(c);
            gc = Collections();
            for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Complete(c); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
            Distribution("powered-episode-retained-dry-continuation", samples, gc, Collections());
        }
        using var mapped = new Contact(PoweredContactFixture.OffCom); var preview = mapped.Seal();
        var source = mapped.Observe.Endpoint; var force = source.BodyToRoot.Rotate(preview.Engine.ProposedForceBodyNewtons); var moment = source.BodyToRoot.Rotate(preview.Engine.ProposedMomentBodyNewtonMetres);
        for (var i = 0; i < 128; i++) OrdinaryContactInputProjection.TryMap(preview.PoweredDuration, 16666, 1, force, moment, source.Properties.MassKilograms, 2, PoweredContactPreparation.GravityLocal, out _);
        gc = Collections();
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp();
            var ok = OrdinaryContactInputProjection.TryMap(preview.PoweredDuration, 16666, 1, force, moment, source.Properties.MassKilograms, 2, PoweredContactPreparation.GravityLocal, out _);
            samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; Check(ok, "timed projection");
        }
        Distribution("pure-input-projection-not-complete-step", samples, gc, Collections());
        Console.WriteLine($"POWERED_CONTACT_COST_PROCESS PASS runtime={Environment.Version} thread={Environment.CurrentManagedThreadId} context_ms=6.67-6.94 no_FPS_extrapolation");
    }
}
