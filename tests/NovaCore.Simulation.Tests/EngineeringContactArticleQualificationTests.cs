using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

// Qualification uses the authored article Fixture and the complete production owner path.
// Diagnostics/assertion formatting and cold preparation remain outside measured windows.
internal static partial class EngineeringContactArticleTests
{
    private static bool CompleteOperation(Fixture fixture)
    {
        var step = fixture.Receipt.Step + 1;
        var ticks = step % 3 == 1 ? 16666 : 16667;
        var credit = fixture.Credit(ticks);
        var service = fixture.Service();
        return credit.Status == ContactHostCreditStatus.Accepted && service.Published == 1 &&
            service.Status == ContactServiceStatus.AwaitingDebt &&
            service.Observation.StateRevision == fixture.Engine.State.Revision;
    }

internal static void Storage()
    {
        // Measured cold+warm allocation is a conservative managed-retention upper bound.
        // It includes the article, graph/store, owner/histories, publication binding and collector.
        var before = GC.GetAllocatedBytesForCurrentThread();
        using var fixture = new Fixture(tilted: true);
        for (var i = 0; i < 128; i++) Check(CompleteOperation(fixture), "article storage warmup");
        var managed = GC.GetAllocatedBytesForCurrentThread() - before;
        var native = checked((long)fixture.World.PoolBytes);
        var combined = checked(managed + native);
        var history = System.Runtime.CompilerServices.Unsafe.SizeOf<ProcessedPersistentContact>() *
            (long)fixture.Engine.PersistentContactHistoryCapacity;
        var selectorPayload = 3 + 12 * (long)System.Runtime.CompilerServices.Unsafe.SizeOf<CompoundContactSelector.Candidate>() +
            12 * (long)System.Runtime.CompilerServices.Unsafe.SizeOf<CompoundContactSelector.Scratch>() + 8 * sizeof(int);
        Console.WriteLine($"ARTICLE_STORAGE managed_cold_warm_upper_bound={managed} native_pool={native} combined_upper_bound={combined} history_payload_included={history} selector_array_payload_included={selectorPayload} limit=8388608");
        Check(combined <= 8 * 1024 * 1024, "article combined retained-storage ceiling");
        for (var i = 128; i < 1200; i++)
        {
            var step = fixture.Receipt.Step + 1;
            Check(fixture.Credit(step % 3 == 1 ? 16666 : 16667).CanonicalCommitted, "storage continuation credit");
            var result = fixture.Service();
            Check(result.Published == 1, "storage continuation publication");
        }
        Check(fixture.World.PoolBytes == (ulong)native, "native storage bounded through completion");
        Check(fixture.Engine.ProcessedPersistentContactCount == 1200, "preallocated history completed");
        Check(fixture.Service().Status == ContactServiceStatus.Completed && fixture.Receipt.Step == 1200,
            "completed episode does not grow history or frontier");
        Check(fixture.World.TryDispose() == LocalContactStatus.Success && fixture.World.PoolBytes == 0,
            "native compound/world storage disposed");
        Cheap();
        Console.WriteLine("ARTICLE_STORAGE_LIFETIME bounded_to_completion=PASS compound_disposal=PASS construction_failure_cleanup=PASS");
    }

    internal static void Physical(string kind)
    {
        _ = kind switch
        {
            "centered" => ContactSequence(false, false),
            "tilted" => ContactSequence(true, false),
            "mirrored" => ContactSequence(true, false, tiltAngle: -.25),
            "moving" => ContactSequence(true, true),
            _ => throw new ArgumentException("Unknown article physical case", nameof(kind))
        };
    }

private static bool ExactPhysical(in ProcessedPersistentContact a, in ProcessedPersistentContact b)
    {
        static bool Bits(double x, double y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y);
        static bool Vector(NovaCore.Core.Double3 x, NovaCore.Core.Double3 y) => Bits(x.X,y.X) && Bits(x.Y,y.Y) && Bits(x.Z,y.Z);
        static bool Quaternion(NovaCore.Core.DoubleQuaternion x, NovaCore.Core.DoubleQuaternion y) =>
            Bits(x.X,y.X) && Bits(x.Y,y.Y) && Bits(x.Z,y.Z) && Bits(x.W,y.W);
        return a.AfterTranslation == b.AfterTranslation && a.AfterRotation == b.AfterRotation && a.Properties == b.Properties &&
            Vector(a.AfterTranslation.PositionRoot,b.AfterTranslation.PositionRoot) &&
            Vector(a.AfterTranslation.VelocityRoot,b.AfterTranslation.VelocityRoot) &&
            Quaternion(a.AfterRotation.OrientationLocalToParent,b.AfterRotation.OrientationLocalToParent) &&
            Vector(a.AfterRotation.AngularVelocityBody,b.AfterRotation.AngularVelocityBody);
    }

    internal static void Schedules()
    {
        foreach (var tilted in new[] { false, true })
        {
            var selected = new int[1200 * 5];
            var reference = new ProcessedPersistentContact[1200];
            var motion = ContactSequence(tilted, false, selectionHistory: selected, publicationHistory: reference);
            using (var privateOnly = new Fixture(tilted, publishing: false))
            {
                for (var step = 1; step <= 1200; step++)
                {
                    privateOnly.Source.TryEndpoint(step, out var target);
                    Check(privateOnly.World.Step(privateOnly.Engine, privateOnly.Configuration, privateOnly.Receipt, target, out var next) ==
                        LocalContactStatus.Success, "private-only article step");
                    privateOnly.Receipt = next;
                    var read = privateOnly.World.Read(privateOnly.Engine, privateOnly.Configuration, next, out var export);
                    Check(read == LocalContactStatus.Success, "private-only article export readable");
                    var expected = motion[step-1];
                    // Match the banked private/public trajectory contract: physical values are equal;
                    // private source revision and precommit published revision are different evidence.
                    static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
                    static bool Vector(NovaCore.Core.Double3 a, NovaCore.Core.Double3 b) => Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z);
                    static bool Quaternion(NovaCore.Core.DoubleQuaternion a, NovaCore.Core.DoubleQuaternion b) =>
                        Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z) && Bits(a.W,b.W);
                    Check((export.Motion with { Revision=expected.Revision }) == expected &&
                        Vector(export.Motion.PositionRoot,expected.PositionRoot) && Vector(export.Motion.VelocityRoot,expected.VelocityRoot) &&
                        Quaternion(export.Motion.BodyToRoot,expected.BodyToRoot) && Vector(export.Motion.AngularVelocityBody,expected.AngularVelocityBody),
                        "private-only vs published exact physical endpoint bits and identity");
                    Check(export.Motion.Revision == privateOnly.Source.Motion.Revision && export.Motion.Revision.Value == 0 &&
                        expected.Revision == reference[step-1].BeforeRevision && expected.Revision.Value == (ulong)step-1 &&
                        reference[step-1].AfterRevision.Value == (ulong)step, "private/staged/committed revision contracts independent");
                }
                Check(privateOnly.Engine.State.Revision.Value == 0 && privateOnly.Engine.ProcessedPersistentContactCount == 0 &&
                    privateOnly.Clock.CurrentTime == privateOnly.Source.Motion.Time, "private-only canonical nonmutation");
            }
            foreach (var hz in new[] { 30, 60, 150, 240, 0 })
            {
                using var fixture = new Fixture(tilted);
                var accepted = 0L; var frames = 0; var prior = 0; var support = 0; var budgetStops = 0; var selectionChecks = 0;
                var generation = fixture.World.Generation; var peak = 0d; var hold = default(PersistentContactObservation);
                var selection = new CompoundContactSelector.Candidate[4]; var predicted = new double[4];
                while (fixture.Engine.ProcessedPersistentContactCount < 1200)
                {
                    if (accepted < 20_000_000)
                    {
                        frames++;
                        var cumulative = hz == 0 ? Math.Min(20_000_000, frames * 500_000L) :
                            Math.Min(20_000_000, (long)((Int128)frames * 1_000_000 / hz));
                        Check(fixture.Credit(cumulative - accepted).Status == ContactHostCreditStatus.Accepted, "article host partition credit");
                        accepted = cumulative;
                    }
                    var result = fixture.Service();
                    Check(result.Status is ContactServiceStatus.AwaitingDebt or ContactServiceStatus.BudgetExhausted or ContactServiceStatus.Completed,
                        "article host partition service");
                    Check(result.Published <= 4, "article per-call budget");
                    if (result.Status == ContactServiceStatus.BudgetExhausted) budgetStops++;
                    var count = fixture.Engine.ProcessedPersistentContactCount;
                    Check(count - prior == result.Published, "every reported publication recorded");
                    Check(fixture.Clock.CurrentTime.Ticks - fixture.Source.Motion.Time.Ticks + fixture.Clock.PendingSimulationDebt.Ticks == accepted,
                        "article admitted debt conservation");
                    for (var i = prior; i < count; i++)
                    {
                        Check(fixture.Engine.TryGetProcessedPersistentContact(i, out var record), "article partition history");
                        var expected = reference[i];
                        Check(ExactPhysical(record, expected), "article host partition complete physical bits");
                        Check(record.Episode == expected.Episode && record.Frontier == i+1 &&
                            record.AfterRevision.Value == (ulong)i+1 && record.BeforeRevision.Value == (ulong)i &&
                            record.TimelineRevision == expected.TimelineRevision &&
                            record.AfterClock.Time == expected.AfterClock.Time &&
                            record.AfterClock.Debt.Ticks == record.BeforeClock.Debt.Ticks - (i % 3 == 0 ? 16666 : 16667),
                            "article partition exact epoch/revision/history/debt");
                        var geometry = Geometry(record.AfterTranslation.PositionRoot, record.AfterRotation.OrientationLocalToParent);
                        peak = Math.Max(peak, -geometry.MinY);
                        Check(peak <= .020, "article host partition penetration");
                        if (i >= 600)
                        {
                            Check(Math.Abs(geometry.MinY) <= 2 * fixture.Configuration.ContactTolerance, "article partition supported height");
                            support++;
                        }
                    }
                    if (count > prior)
                    {
                        hold = result.Observation;
                        var n = fixture.World.CopyCompoundSelectionForTest(selection, predicted);
                        Check(n == selected[(count-1)*5], "article host partition selector count");
                        for (var j = 0; j < n; j++)
                            Check(selection[j].Contact.FeatureId == selected[(count-1)*5+j+1], "article host partition ordered selector identity");
                        selectionChecks++;
                    }
                    prior = count;
                }
                Check(support == 600 && accepted == 20_000_000 && fixture.Clock.PendingSimulationDebt.Ticks == 0 &&
                    fixture.World.Generation == generation && fixture.Receipt.Step == 1200, "article partition final conservation/continuity");
                if (hz == 0) Check(budgetStops > 0, "delayed input exercises bounded backlog");
                var finalClock = fixture.Engine.CaptureContinuationClock();
                for (var i = 0; i < 128; i++)
                {
                    var done = fixture.Service();
                    Check(done.Status == ContactServiceStatus.Completed && done.Published == 0 && fixture.Receipt.Step == 1200 &&
                        fixture.Engine.ProcessedPersistentContactCount == 1200 && fixture.Engine.CaptureContinuationClock() == finalClock,
                        "completed canonical hold never advances");
                }
                Check(hold.Translation == reference[^1].AfterTranslation && hold.Rotation == reference[^1].AfterRotation,
                    "last copied observation remains final endpoint");
                Console.WriteLine($"ARTICLE_SCHEDULE tilted={tilted} hz={hz} endpoints=1200/1200 support={support}/600 selector_service_boundaries={selectionChecks} selector_differences=0 budget_stops={budgetStops} accepted={accepted} debt=0 history=1200 final_hold=PASS");
            }
            Console.WriteLine($"ARTICLE_PRIVATE_EQUIVALENCE tilted={tilted} endpoints=1200/1200 canonical_nonmutation=PASS");
        }
    }

internal static void ColdDiagnostics()
    {
        using var fixture = new Fixture(tilted: true);
        var histogram = new int[13]; var selectedHistogram = new int[5];
        var candidates = new CompoundContactSelector.Candidate[12]; var predicted = new double[12]; var native = new int[4];
        var firstStep = 0d; var firstManifold = 0d; var firstSelection = 0d; var manifoldStep = 0; var selectionStep = 0;
        for(var step=1;step<=1200;step++)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            Check(fixture.Credit(step%3==1?16666:16667).CanonicalCommitted,"cold diagnostic credit");
            var result=fixture.Service();
            var elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            Check(result.Published==1,"cold diagnostic complete operation");
            if(step==1)firstStep=elapsed;
            Check(fixture.World.Read(fixture.Engine,fixture.Configuration,fixture.Receipt,out var export)==LocalContactStatus.Success,"cold diagnostic export");
            var rawCount=fixture.World.CopyCompoundRawForTest(candidates,predicted,native);
            var selectedCount=fixture.World.CopyCompoundSelectionForTest(candidates,predicted);
            histogram[rawCount]++;selectedHistogram[selectedCount]++;
            if(manifoldStep==0 && export.ArticleContactChildMask!=0){manifoldStep=step;firstManifold=elapsed;}
            if(selectionStep==0 && selectedCount!=0){selectionStep=step;firstSelection=elapsed;}
        }
        Console.WriteLine("ARTICLE_COLD "+System.Text.Json.JsonSerializer.Serialize(new {
            article_preparation_ms=fixture.ArticlePreparationMs,world_preparation_ms=fixture.WorldPreparationMs,
            first_operation_ms=firstStep,first_manifold_step=manifoldStep,first_manifold_operation_ms=firstManifold,
            first_specialized_step=selectionStep,first_specialized_operation_ms=firstSelection,
            raw_count_histogram=histogram,selected_count_histogram=selectedHistogram,steps=1200,
            note="First manifold may be speculative. Timings cover the whole operation; no isolated callback attribution."
        }));
    }

    internal static void Performance()
    {
        var samples = new double[1024]; var ordered = new double[1024];
        var cold = System.Diagnostics.Stopwatch.GetTimestamp();
        using var fixture = new Fixture(tilted: true);
        var coldMs = System.Diagnostics.Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        for (var i = 0; i < 128; i++) Check(CompleteOperation(fixture), "article performance warmup");
        for (var i = 0; i < 1024; i++)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var ok = CompleteOperation(fixture);
            samples[i] = ordered[i] = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            Check(ok, "article measured complete operation");
        }
        Array.Sort(samples);
        var median = (samples[511] + samples[512]) / 2;
        var p95 = samples[972]; var p99 = samples[1013]; var maximum = samples[^1];
        Console.WriteLine("ARTICLE_PERFORMANCE " + System.Text.Json.JsonSerializer.Serialize(new {
            median_ms=median, p95_ms=p95, p99_ms=p99, max_ms=maximum, cold_preparation_ms=coldMs,
            warm=128, measured=1024, first_measured_frontier=129, last_measured_frontier=1152,
            tails=ordered.Select((ms,index)=>new {frontier=index+129,ms}).OrderByDescending(x=>x.ms).Take(8).ToArray()
        }));
        Check(median <= .05 && p95 <= .10 && p99 <= .25 && maximum <= .50, "article complete operation performance ceiling");
    }

    internal static void Allocation()
    {
        OrdinaryAllocationMeasurement.PositiveControl();
        foreach (var tilted in new[] { false, true })
        {
            using var fixture = new Fixture(tilted);
            for (var i = 0; i < 128; i++) Check(CompleteOperation(fixture), "article complete allocation warmup");
            var pool = fixture.World.PoolBytes;
            var success = true;
            var gate = tilted ? "article-tilted-complete" : "article-centered-complete";
            using (var measurement = new OrdinaryAllocationMeasurement(gate))
            {
                for (var i = 0; i < 1024; i++) success &= CompleteOperation(fixture);
                OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), gate);
            }
            Check(success, "article measured complete operations");
            Check(fixture.Engine.ProcessedPersistentContactCount == 1152, "article measured publication count");
            Check(fixture.Clock.PendingSimulationDebt.Ticks == 0, "article measured debt");
            Check(fixture.World.PoolBytes == pool, "article warmed native storage stable");

            for (var i = 0; i < 128; i++)
                Check(fixture.Credit(0).Status == ContactHostCreditStatus.NoWork &&
                    fixture.Service().Status == ContactServiceStatus.AwaitingDebt, "article no-work warmup");
            gate = tilted ? "article-tilted-no-work" : "article-centered-no-work";
            using (var measurement = new OrdinaryAllocationMeasurement(gate))
            {
                for (var i = 0; i < 1024; i++)
                    success &= fixture.Credit(0).Status == ContactHostCreditStatus.NoWork &&
                        fixture.Service().Status == ContactServiceStatus.AwaitingDebt;
                OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), gate);
            }
            Check(success && fixture.Engine.ProcessedPersistentContactCount == 1152, "article no-work nonadvancement");

            using var backlog = new Fixture(tilted, debt: 20_000_000);
            for (var i = 0; i < 128; i++) Check(backlog.Service().Published == 4, "article backlog warmup");
            pool = backlog.World.PoolBytes;
            gate = tilted ? "article-tilted-backlog" : "article-centered-backlog";
            using (var measurement = new OrdinaryAllocationMeasurement(gate))
            {
                for (var i = 0; i < 128; i++) success &= backlog.Service().Published == 4;
                OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(), gate);
            }
            Check(success && backlog.Engine.ProcessedPersistentContactCount == 1024, "article backlog count");
            Check(backlog.World.PoolBytes == pool, "article backlog native storage stable");
            Check(backlog.Clock.PendingSimulationDebt.Ticks == 20_000_000 - 1024L * 1_000_000 / 60,
                "article backlog retained exact debt");
            Console.WriteLine($"ARTICLE_ALLOCATION tilted={tilted} complete=1024 no_work=1024 backlog_calls=128 backlog_intervals=512 PASS");
        }
    }
}
