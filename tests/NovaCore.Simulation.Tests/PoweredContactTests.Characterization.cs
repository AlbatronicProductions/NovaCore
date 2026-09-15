using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class PoweredContactTests
{
    internal static void StorageCharacterization()
    {
        using(var warm=new Contact()) Complete(warm);
        var heapBefore=GC.GetTotalMemory(true);var allocatedBefore=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        using var c=new Contact();
        var coldMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var coldTraffic=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;
        var heapAfter=GC.GetTotalMemory(true);var nativeInitial=c.World.PoolBytes;var peak=nativeInitial;
        for(var i=0;i<1200;i++){Complete(c);peak=Math.Max(peak,c.World.PoolBytes);}
        var heapEnd=GC.GetTotalMemory(true);
        var history=24L+1200L*System.Runtime.CompilerServices.Unsafe.SizeOf<PoweredFlightRecord>();
        Check(coldTraffic+(long)peak<=8*1024*1024,"conservative whole managed cold traffic plus native high water <=8MiB");
        Console.WriteLine("POWERED_CONTACT_STORAGE_FINAL "+JsonSerializer.Serialize(new {
            coldMs,managedColdAllocationTraffic=coldTraffic,managedRetainedHeapDeltaEstimate=heapAfter-heapBefore,
            managedEndHeapDeltaEstimate=heapEnd-heapBefore,nativeRetainedInitial=nativeInitial,nativeRetainedFinal=c.World.PoolBytes,nativePeak=peak,
            historyBytesIncluded=history,mapperValueBytes=System.Runtime.CompilerServices.Unsafe.SizeOf<OrdinaryContactProjectionSchedule>(),mapperCopies=2,
            managedConservativeRetainedUpperBound=coldTraffic,combinedConservativeUpperBound=coldTraffic+(long)peak,
            transactionOwner="included in managed bound; history separately identified; no double counting",intervals=1200,
            method="forced full-GC live-heap deltas are estimates; cold allocation traffic is a distinct conservative managed retention upper bound"
        }));
        GC.KeepAlive(c);
    }
    // Final characterization is separate from the preserved historical first-ticket cost routes.
    internal static void Characterization()
    {
        static void Report(string population, double[] samples, int[] collectionBefore,
            int[] gcSamples, int solverSteps, string lifetime)
        {
            var order = Enumerable.Range(0, samples.Length).OrderByDescending(i => samples[i]).ToArray();
            var sorted = (double[])samples.Clone(); Array.Sort(sorted);
            var after = Collections();
            Console.WriteLine("POWERED_CONTACT_CHARACTERIZATION " + JsonSerializer.Serialize(new
            {
                population, warm = 128, samples = samples.Length, medianMs = sorted[512],
                p95Ms = sorted[972], p99Ms = sorted[1013], maxMs = sorted[1023],
                tails = order.Take(8).Select(i => new { index = i, ms = samples[i], collections = gcSamples[i] }),
                collectionsIncludingCold = after.Zip(collectionBefore, (a,b) => a-b).ToArray(),
                collectionsDuringTimedOperations = gcSamples.Sum(), solverStepsPerSample = solverSteps,
                iterations = solverSteps == 0 ? 0 : 8, substeps = solverSteps == 0 ? 0 : 1,
                ownerThreads = 1, synchronization = solverSteps == 0 ? "none" : "exclusive owner phase; no waiting lock",
                lifetime, allocation = "separate checked exact-zero gate", historicalTargetsOnly = new[] { .05, .10, .25, .50 }
            }));
        }
        static int Gc() => GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        foreach (var powered in new[] { false, true })
        {
            using var c = new Contact(powered ? PoweredContactFixture.CenteredEndpoint : PoweredContactFixture.CenteredBaseline);
            for (var i=0;i<128;i++) Complete(c);
            Check(powered ? c.Observe.Resource.RemainingUnits.IsZero : c.Observe.Resource.RemainingUnits == c.Prepared.ResourceDefinition.InitialUnits,
                "characterization retained population identity");
            var times = new double[1024]; var gc = new int[1024]; var before = Collections();
            for (var i=0;i<times.Length;i++)
            {
                var g = Gc(); var start = Stopwatch.GetTimestamp(); Complete(c);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; gc[i] = Gc()-g;
            }
            Report(powered ? "GENUINE EXHAUSTED-DRY RETAINED CONTACT" : "UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL", times, before, gc, 1, "one retained world");
        }
        foreach (var eventPair in new[] { false, true })
        {
            var times = new double[1024]; var gc = new int[1024]; var before = Collections();
            for (var i=-128;i<times.Length;i++)
            {
                using var c = new Contact(eventPair ? PoweredContactFixture.OffCom : PoweredContactFixture.CenteredEndpoint, capacity:3);
                var g = Gc(); var start = Stopwatch.GetTimestamp(); Complete(c); if (eventPair) Complete(c);
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds; var collections = Gc()-g;
                Check(c.Engine.TryGetPoweredFlightRecord(0, out var record) && record.Segmentation.Classification ==
                    (eventPair ? PropellantClassification.InteriorExhaustion : PropellantClassification.FullPowered), "genuine timed powered/event sample");
                if (eventPair) Check(c.Observe.Resource.RemainingUnits.IsZero && c.Observe.Actuator.Activity == ActualEngineActivity.EnabledNoFeed,
                    "genuine timed exhausted dry successor");
                if (i>=0) { times[i]=elapsed; gc[i]=collections; }
            }
            Report(eventPair ? "GENUINE EXHAUSTION -> DRY CONTINUATION" : "PRODUCTION POWERED-CONTACT INTERVAL", times, before, gc, eventPair?2:1,
                "independent prepared episodes; cold construction and disposal excluded");
        }
        using var mapped = new Contact(PoweredContactFixture.OffCom); var preview = mapped.Seal();
        var source = mapped.Observe.Endpoint;
        var force = source.BodyToRoot.Rotate(preview.Engine.ProposedForceBodyNewtons);
        var moment = source.BodyToRoot.Rotate(preview.Engine.ProposedMomentBodyNewtonMetres);
        var projection = mapped.Prepared.Projection;
        var mapperTimes = new double[1024]; var mapperGc = new int[1024]; var mapperBefore = Collections();
        for(var i=-128;i<mapperTimes.Length;i++)
        {
            var g=Gc(); var start=Stopwatch.GetTimestamp();
            var ok=projection.TryMap(preview.PoweredDuration,16666,force,moment,source.Properties.MassKilograms,2,PoweredContactPreparation.GravityLocal,out _);
            var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var collections=Gc()-g;
            Check(ok,"prepared event-to-ordinary mapper");
            if(i>=0){mapperTimes[i]=elapsed;mapperGc[i]=collections;}
        }
        Report("EVENT-TO-ORDINARY INPUT PREPARATION",mapperTimes,mapperBefore,mapperGc,0,"accepted prepared production mapper; exact event preview");
    }
}
