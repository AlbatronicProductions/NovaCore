using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class SpacecraftTranslationTests
{
    private static readonly SpacecraftId Craft = new(71);
    private static readonly ReferenceFrameId Root = new(1);
    private static readonly ReferenceFrameId Body = new(72);
    private static readonly SpacecraftPhysicalProperties Mass = new(8);
    private static readonly SpacecraftTranslationState Initial = new(Craft, Root, SimulationInstant.Zero,
        new(1024, -32, 8), new(2, -1, .5), new(4, 2, -1));

    internal static void Run()
    {
        Contracts(); Numerical(); Transactions(); Cadence(); Frames(); Allocations();
        Console.WriteLine("PASS Spacecraft translation: contracts, numerical oracle, transactions, cadence, frames, allocations");
    }

    private static void Check(bool ok, string message) { if (!ok) throw new Exception("Translation: " + message); }
    private static SpacecraftTranslationEvaluation Evaluate(SpacecraftTranslationState state, SimulationInstant time) =>
        SpacecraftTranslationEvaluator.TryEvaluate(state, Mass, time);

    private static ReferenceFrameGraph Graph()
    {
        var builder = new ReferenceFrameGraphBuilder();
        builder.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "Inertial root"));
        builder.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "Craft COM"));
        return builder.Build();
    }

    private static (SimulationTransactionEngine Engine, SimulationClock Clock, SpacecraftStateStore Store) Fixture(int capacity = 32)
    {
        var rotations = new[] { new SpacecraftRigidBodyRotationState(Craft, SimulationInstant.Zero, DoubleQuaternion.Identity,
            new(0, 0, .125), new(2, 2, 2), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1) };
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "Independent craft")], rotations,
            [Mass], [Initial], Graph(), out var store, out _), "create one coherent spacecraft");
        var clock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
        return (new SimulationTransactionEngine(clock, new SimulationState(spacecraft: store), capacity), clock, store!);
    }

    private static void Schedule(SimulationClock clock, ulong id, double seconds, Double3 force, int priority = 0)
    {
        Check(SimulationEventRequest.TryCreateSpacecraftForce(new(id), priority,
            new(Craft, SimulationInstant.FromSecondsRounded(seconds), force), out var request), "force intent");
        Check(clock.Timeline.Schedule(clock.CurrentTime, request).Succeeded, "schedule force");
    }

    private static SpacecraftTranslationState State(SimulationTransactionEngine engine)
    {
        Check(engine.State.Spacecraft.TryGetTranslation(Craft, out var state, out var properties) && properties == Mass, "identity and fixed mass");
        return state;
    }

    private static void Contracts()
    {
        var atEpoch = Evaluate(Initial, Initial.Epoch);
        Check(atEpoch.Succeeded && atEpoch.PositionRoot == Initial.PositionRoot && atEpoch.VelocityRoot == Initial.VelocityRoot, "epoch exact");
        foreach (var mass in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            Check(!SpacecraftTranslationEvaluator.TryEvaluate(Initial, new(mass), Initial.Epoch).Succeeded, "invalid mass");
        foreach (var invalid in new[] { Initial with { Spacecraft = default }, Initial with { RootFrame = default },
            Initial with { PositionRoot = new(double.NaN, 0, 0) }, Initial with { VelocityRoot = new(double.PositiveInfinity, 0, 0) },
            Initial with { ConstantForceRoot = new(double.NaN, 0, 0) } })
            Check(!Evaluate(invalid, Initial.Epoch).Succeeded, "invalid state");
        Check(Evaluate(Initial, new(-1)).Status == SpacecraftTranslationStatus.TimeBeforeEpoch, "no historical extrapolation");
        Check(Evaluate(Initial with { Epoch = new(long.MinValue) }, new(long.MaxValue)).Status == SpacecraftTranslationStatus.DurationOverflow, "checked exact duration");
        Check(!Evaluate(Initial with { VelocityRoot = new(double.MaxValue, 0, 0) }, SimulationInstant.FromWholeSeconds(4)).Succeeded, "nonfinite propagation");
        Check(!SpacecraftTranslationEvaluator.TryEvaluate(Initial, new(double.Epsilon), Initial.Epoch).Succeeded, "overflowing acceleration");
        var late = Initial with { Epoch = new(10_000_000_000_000_000) };
        Check(Evaluate(late, new(late.Epoch.Ticks + 1)).PositionRoot == Evaluate(Initial, new(1)).PositionRoot, "one microsecond at late epoch");
        var rotation = new SpacecraftRigidBodyRotationState(Craft, Initial.Epoch, DoubleQuaternion.Identity, Double3.Zero, new(2, 2, 2), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        Check(!SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "craft")], [rotation], [Mass],
            [Initial with { RootFrame = Body }], Graph(), out _, out _), "no rotating/body carrier authority");
        Check(!SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "craft")], [rotation with { Epoch = new(1) }], [Mass],
            [Initial], Graph(), out _, out _), "initial common epoch");
        foreach (var rootKind in new[] { ReferenceFrameKind.Ccf, ReferenceFrameKind.Cci })
        {
            var invalidGraph = new ReferenceFrameGraphBuilder();
            invalidGraph.Add(new ReferenceFrameNode(Root, null, rootKind, "not inertial root"));
            invalidGraph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "craft"));
            Check(!SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "craft")], [rotation], [Mass], [Initial], invalidGraph.Build(), out _, out _), "non-ECL root rejected");
        }
        var multi = new ReferenceFrameGraphBuilder();
        multi.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "root"));
        multi.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "craft"));
        multi.Add(new ReferenceFrameNode(new(3), null, ReferenceFrameKind.Ecl, "second root"));
        Check(!SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "craft")], [rotation], [Mass], [Initial], multi.Build(), out _, out _), "multiple roots rejected");
    }

    private static void Numerical()
    {
        double maxPosition = 0, maxVelocity = 0;
        // Decimal arithmetic is an independent oracle for these exactly decimal-representable inputs.
        // u=2^-53; gamma16 and gamma8 conservatively bound operations plus oracle conversion rounding.
        const double u = 1.1102230246251565e-16;
        var gamma16 = 16 * u / (1 - 16 * u); var gamma8 = 8 * u / (1 - 8 * u);
        foreach (var origin in new[] { 1024d, 150_000_000_000d })
        foreach (var ticks in new long[] { 0, 1, 125_000, 1_000_001, 60_123_456, 3_600_000_000, 86_400_000_000 })
        {
            var state = Initial with { PositionRoot = new(origin, -32, 8) };
            var result = Evaluate(state, new(ticks)); Check(result.Succeeded, "numerical evaluation");
            decimal t = ticks / 1_000_000m;
            var expectedP = (double)((decimal)origin + 2m * t + .25m * t * t);
            var expectedV = (double)(2m + .5m * t);
            var pe = Math.Abs(result.PositionRoot.X - expectedP); var ve = Math.Abs(result.VelocityRoot.X - expectedV);
            maxPosition = Math.Max(maxPosition, pe); maxVelocity = Math.Max(maxVelocity, ve);
            var td = (double)t;
            Check(pe <= gamma16 * (Math.Abs(origin) + 2 * td + .25 * td * td), "scale-aware position bar");
            Check(ve <= gamma8 * (2 + .5 * td), "scale-aware velocity bar");
            Check(result == Evaluate(state, new(ticks)), "bit-identical replay");
        }
        var ballistic = Evaluate(Initial with { ConstantForceRoot = Double3.Zero }, SimulationInstant.FromWholeSeconds(32));
        Check(ballistic.PositionRoot == Initial.PositionRoot + Initial.VelocityRoot * 32 && ballistic.VelocityRoot == Initial.VelocityRoot, "zero force");
        var dyadic = Evaluate(Initial, SimulationInstant.FromWholeSeconds(8));
        Check(dyadic.PositionRoot == new Double3(1056, -32, 8) && dyadic.VelocityRoot == new Double3(6, 1, -.5), "known force analytical dyadic");
        Console.WriteLine($"Translation oracle: maxPositionError={maxPosition:R} m; maxVelocityError={maxVelocity:R} m/s; boundedInterval=86400 s; repeatedEvaluationDifference=0 bits");
    }

    private static void Transactions()
    {
        var (engine, clock, _) = Fixture();
        Schedule(clock, 1, 2, new(8, 0, 0));
        clock.AdvanceTo(SimulationInstant.FromWholeSeconds(2));
        var outer = engine.EvaluateNext(); var candidate = outer.SpacecraftForceReplacement!.Value;
        var before = State(engine); var revision = engine.State.Revision; var timelineRevision = clock.Timeline.Revision;
        Check(!engine.ValidateAndCommit(outer with { SpacecraftForceReplacement = null, ChangesAuthoritativeState = false }).Committed,
            "stripped force proposal cannot consume event as marker");
        Check(!engine.ValidateAndCommit(outer with { EvaluationTime = new(1) }).Committed, "inconsistent wrapper time rejected");
        var forged = candidate with { Replacement = candidate.Replacement with { PositionRoot = Double3.Zero } };
        Check(engine.ValidateAndCommit(forged) == SpacecraftTranslationStatus.InvalidReplacement, "forged position rejection");
        Check(engine.ValidateAndCommit(candidate with { Replacement = candidate.Replacement with { VelocityRoot = Double3.Zero } }) == SpacecraftTranslationStatus.InvalidReplacement, "forged velocity rejection");
        Check(engine.ValidateAndCommit(candidate with { Replacement = candidate.Replacement with { RootFrame = Body } }) == SpacecraftTranslationStatus.InvalidReplacement, "forged root rejection");
        Check(engine.ValidateAndCommit(candidate with { Replacement = candidate.Replacement with { Spacecraft = new(99) } }) == SpacecraftTranslationStatus.InvalidReplacement, "forged craft rejection");
        Check(State(engine) == before && engine.State.Revision == revision && clock.Timeline.Revision == timelineRevision && engine.ProcessedCount == 0, "failed proposals are atomic");
        Check(engine.ValidateAndCommit(outer).Committed, "canonical force commit");
        var after = State(engine); var oldAtBoundary = Evaluate(before, clock.CurrentTime);
        Check(after.PositionRoot == oldAtBoundary.PositionRoot && after.VelocityRoot == oldAtBoundary.VelocityRoot && after.Epoch == clock.CurrentTime, "force boundary momentum continuity");
        Check(engine.ProcessedSpacecraftForceCount == 1 && engine.State.Revision.Value == revision.Value + 1, "single commit publication");
        Check(!engine.ValidateAndCommit(outer).Committed, "consumed transaction cannot replay");
        Check(Evaluate(after, new(clock.CurrentTime.Ticks - 1)).Status == SpacecraftTranslationStatus.TimeBeforeEpoch, "old history not extrapolated");
        Check(Evaluate(after, new(clock.CurrentTime.Ticks + 1)).Succeeded, "next exact tick");
        Schedule(clock, 2, 3, after.ConstantForceRoot); clock.AdvanceTo(SimulationInstant.FromWholeSeconds(3));
        Check(engine.ExecuteCanonicalPendingEvent().Committed && State(engine) == after && engine.State.Revision.Value == revision.Value + 1, "same force does not rebase");

        var limited = Fixture(0); Schedule(limited.Clock, 1, 0, Double3.Zero);
        var limitedState = State(limited.Engine); var limitedTimeline = limited.Clock.Timeline.Revision;
        Check(!limited.Engine.ExecuteCanonicalPendingEvent().Committed && State(limited.Engine) == limitedState && limited.Engine.ProcessedCount == 0 &&
            limited.Clock.Timeline.Revision == limitedTimeline, "capacity failure preserves state and pending event");
        var early = Fixture(); Schedule(early.Clock, 1, 2, Double3.Zero);
        early.Clock.Timeline.TryPeekPending(out var future);
        Check(SpacecraftForceTransactionEvaluator.TryCreate(early.Engine.State, future, future.Header.Time, early.Clock.Timeline.Revision, out var futureProposal) == SpacecraftTranslationStatus.Success,
            "pure future proposal");
        Check(early.Engine.ValidateAndCommit(futureProposal) == SpacecraftTranslationStatus.TimeMismatch && State(early.Engine) == Initial && early.Engine.ProcessedCount == 0,
            "future proposal cannot advance clock");

        var stale = Fixture(); Schedule(stale.Clock, 1, 0, Double3.Zero);
        var staleProposal = stale.Engine.EvaluateNext();
        var torque = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(stale.Engine.State, new(Craft, new(0, 0, .125), SimulationInstant.Zero));
        Check(torque.Succeeded && stale.Engine.ValidateAndCommit(torque.Transaction!.Value).Committed, "unrelated torque commit");
        Check(!stale.Engine.ValidateAndCommit(staleProposal).Committed && State(stale.Engine) == Initial, "global revision invalidates stale force");
        Check(stale.Engine.ExecuteCanonicalPendingEvent().Committed, "fresh proposal after torque");
        Check(SpacecraftMotionEvaluator.TryEvaluate(stale.Engine.State, Craft, SimulationInstant.FromWholeSeconds(1), out var motion) == SpacecraftTranslationStatus.Success &&
            motion.Time == SimulationInstant.FromWholeSeconds(1) && motion.Spacecraft == Craft && motion.Revision == stale.Engine.State.Revision, "coherent angular/linear publication");
        Check(SpacecraftMotionEvaluator.TryEvaluate(stale.Engine.State, Craft, new(-1), out var failed) != SpacecraftTranslationStatus.Success && failed == default, "no partial motion result");

        var mixedEpoch = Fixture(); mixedEpoch.Clock.AdvanceTo(SimulationInstant.FromWholeSeconds(4));
        var laterTorque = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(mixedEpoch.Engine.State,
            new(Craft, new(0, 0, .25), mixedEpoch.Clock.CurrentTime));
        Check(laterTorque.Succeeded && mixedEpoch.Engine.ValidateAndCommit(laterTorque.Transaction!.Value).Committed, "later torque segment");
        Check(SpacecraftMotionEvaluator.TryEvaluate(mixedEpoch.Engine.State, Craft, SimulationInstant.FromWholeSeconds(2), out var history) == SpacecraftTranslationStatus.TimeBeforeEpoch &&
            history == default, "latest torque cannot reconstruct prior history");
        Check(SpacecraftReferenceFrameEvaluator.TryEvaluate(mixedEpoch.Engine.State.Spacecraft, Graph(), SimulationInstant.FromWholeSeconds(2), new ReferenceFrameEvaluation[2]) ==
            SpacecraftReferenceFrameEvaluationStatus.AttitudeEvaluationFailed, "frame extraction respects both segment epochs");
        Check(SpacecraftMotionEvaluator.TryEvaluate(mixedEpoch.Engine.State, Craft, mixedEpoch.Clock.CurrentTime, out var coherent) == SpacecraftTranslationStatus.Success &&
            coherent.Time == mixedEpoch.Clock.CurrentTime, "different stored epochs combine at one current instant");

        var group = Fixture();
        Schedule(group.Clock, 1, 2, new(8, 0, 0), 0); Schedule(group.Clock, 3, 2, Double3.Zero, 2);
        Check(SimulationEventRequest.TryCreateRigidBodyTorque(new(2), SimulationInstant.FromWholeSeconds(2), 1, Craft, out var torqueEvent) &&
            group.Clock.Timeline.Schedule(group.Clock.CurrentTime, torqueEvent).Succeeded, "scheduled angular boundary");
        group.Clock.AdvanceTo(SimulationInstant.FromWholeSeconds(2));
        var timelineStale = group.Engine.EvaluateNext(); Schedule(group.Clock, 4, 3, Double3.Zero);
        Check(!group.Engine.ValidateAndCommit(timelineStale).Committed && group.Engine.ProcessedCount == 0 && State(group.Engine) == Initial, "stale timeline preserves state");
        Check(group.Engine.ExecuteCanonicalGroup().IsComplete && group.Engine.ProcessedCount == 3 && group.Engine.State.Revision.Value == 3, "same-instant serial force torque force");
        for (var i = 0; i < 3; i++) Check(group.Engine.TryGetProcessed(i, out var processed) && processed.Event.Id.Value == (ulong)i + 1, "canonical priority order");
        var groupState = State(group.Engine);
        Check(groupState.ConstantForceRoot == Double3.Zero && groupState.PositionRoot == new Double3(1029, -33.5, 8.75) &&
            groupState.VelocityRoot == new Double3(3, -.5, .25), "same-time events do not advance twice");
    }

    private static void Cadence()
    {
        static (SpacecraftTranslationState State, SpacecraftMotion Motion) Replay(int observations, SimulationRate rate)
        {
            var fixture = Fixture(); fixture.Clock.TrySetRate(rate);
            Schedule(fixture.Clock, 1, 2, new(8, 0, 0)); Schedule(fixture.Clock, 2, 4, Double3.Zero);
            for (var i = 0; i <= observations; i++)
            {
                var target = SimulationInstant.FromSecondsRounded(8d * i / observations);
                while (fixture.Clock.CurrentTime < target)
                {
                    fixture.Clock.AdvanceTo(target);
                    if (fixture.Clock.Timeline.TryPeekPending(out var pending) && pending.Header.Time == fixture.Clock.CurrentTime)
                        Check(fixture.Engine.ExecuteCanonicalGroup().IsComplete, "canonical boundary");
                }
                Check(SpacecraftMotionEvaluator.TryEvaluate(fixture.Engine.State, Craft, target, out _) == SpacecraftTranslationStatus.Success, "derived observation");
            }
            Check(SpacecraftMotionEvaluator.TryEvaluate(fixture.Engine.State, Craft, SimulationInstant.FromWholeSeconds(8), out var end) == SpacecraftTranslationStatus.Success, "final motion");
            return (State(fixture.Engine), end);
        }
        var baseline = Replay(1, SimulationRate.One);
        foreach (var cadence in new[] { 30, 60, 144, 1000 })
            Check(Replay(cadence, new SimulationRate(604800, 1)) == baseline, "render observation/warp-rate independence");
        Check(baseline.Motion.PositionRoot.X == 1057 && baseline.Motion.VelocityRoot.X == 5, "piecewise force oracle");
        foreach (var rate in new[] { 1, 2, 8 })
        {
            var host = Fixture(); host.Clock.TrySetRate(new(rate, 1));
            Schedule(host.Clock, 1, 2, new(8, 0, 0)); Schedule(host.Clock, 2, 4, Double3.Zero);
            long totalHostTicks = 8_000_000 / rate;
            for (var part = 1; part <= 137; part++)
            {
                var ticks = totalHostTicks * part / 137 - totalHostTicks * (part - 1) / 137;
                var converted = host.Clock.AdvanceByHostDuration(new(ticks));
                Check(host.Clock.TryGetPendingSimulationDebtTarget(out var target), "host conversion supplies exact target");
                while (host.Clock.CurrentTime < target)
                {
                    host.Clock.AdvanceTo(target);
                    if (host.Clock.Timeline.TryPeekPending(out var pending) && pending.Header.Time == host.Clock.CurrentTime)
                        Check(host.Engine.ExecuteCanonicalGroup().IsComplete, "warped canonical event");
                }
                host.Clock.ConsumePendingSimulationDebt(converted.DerivedSimulationDuration);
            }
            Check(State(host.Engine) == baseline.State && host.Clock.CurrentTime == SimulationInstant.FromWholeSeconds(8), "host partition and warp replay bits");
        }
    }

    private static void Frames()
    {
        var fixture = Fixture(); var time = SimulationInstant.FromWholeSeconds(8);
        Check(SpacecraftMotionEvaluator.TryEvaluate(fixture.Engine.State, Craft, time, out var motion) == SpacecraftTranslationStatus.Success, "frame motion");
        var values = new ReferenceFrameEvaluation[2];
        Check(SpacecraftReferenceFrameEvaluator.TryEvaluate(fixture.Engine.State.Spacecraft, Graph(), time, values) == SpacecraftReferenceFrameEvaluationStatus.Success, "existing frame extraction");
        Check(values[1].Value.LocalToParent.Translation == motion.PositionRoot && values[1].Value.OriginVelocityInParent == motion.VelocityRoot &&
            values[1].Value.LocalToParent.Rotation == motion.BodyToRoot, "root derived transport contains translation once");
        var bodyPose = new FrameTransform(new(150_000_000_000, 500, -800), new DoubleQuaternion(0, 0, Math.Sqrt(.5), Math.Sqrt(.5)));
        var centerVelocity = new Double3(-30000, 200, 10); var omega = new Double3(0, 0, .00007292115);
        var point = new Double3(6_371_000, 40, 20); var relative = new Double3(5, -2, .25);
        var rootPosition = ReferenceFrameMath.ResolvePositionToRoot(bodyPose, point);
        var rootVelocity = ReferenceFrameMath.ResolveVelocityToRoot(bodyPose, centerVelocity, omega, point, relative);
        var backPosition = ReferenceFrameMath.ConvertRootPositionToLocal(bodyPose, rootPosition);
        var backVelocity = ReferenceFrameMath.ConvertRootVelocityToLocal(bodyPose, centerVelocity, omega, rootPosition, rootVelocity);
        var pe = Math.Sqrt((backPosition - point).LengthSquared); var ve = Math.Sqrt((backVelocity - relative).LengthSquared);
        // Root subtraction at 1.5e11 m costs ~3e-5 m per ulp. Allow eight ulps plus rotational arithmetic.
        var positionBar = 8 * (Math.BitIncrement(150_000_000_000d) - 150_000_000_000d);
        var velocityBar = Math.Sqrt(omega.LengthSquared) * positionBar + 32 * (Math.BitIncrement(30000d) - 30000d);
        Check(pe <= positionBar && ve <= velocityBar, "moving rotating body transport round trip");
        var surfaceVelocity = ReferenceFrameMath.ResolveVelocityToRoot(bodyPose, centerVelocity, omega, point, Double3.Zero);
        Check(Math.Sqrt((rootVelocity - surfaceVelocity - bodyPose.Rotation.Rotate(relative)).LengthSquared) <= velocityBar, "surface relative velocity preserves body momentum");
        Console.WriteLine($"Translation frame roundtrip: position={pe:R} m; velocity={ve:R} m/s; bars={positionBar:R} m/{velocityBar:R} m/s");
    }

    private static void Allocations()
    {
        var fixture = Fixture(); var view = fixture.Engine.State; var time = SimulationInstant.FromWholeSeconds(8);
        for (var i = 0; i < 20_000; i++) SpacecraftMotionEvaluator.TryEvaluate(view, Craft, time, out _);
        using var ordinary25 = new OrdinaryAllocationMeasurement("spacecraft-translation"); double checksum = 0;
        for (var i = 0; i < 100_000; i++)
        {
            var evaluated = Evaluate(Initial, new(i)); checksum += evaluated.PositionRoot.X;
            SpacecraftMotionEvaluator.TryEvaluate(view, Craft, time, out var motion); checksum += motion.PositionRoot.X;
        }
        var bytes = ordinary25.Complete();
        OrdinaryAllocationMeasurement.RequireZero(bytes, "spacecraft-translation"); Check(checksum > 0, "spacecraft-translation: original non-allocation predicate (checksum > 0)");
        Console.WriteLine($"Translation warmed allocation: {bytes} bytes / 100000 linear + coherent evaluations");
    }

    internal static void Performance()
    {
        // Batch-average latency percentiles, not a claim about individual-call tail latency.
        foreach (var count in new[] { 1, 256 })
        {
            var states = Enumerable.Range(0, count).Select(i => Initial with { Spacecraft = new((ulong)i + 1), PositionRoot = new(1024 + i, -32, 8) }).ToArray();
            const int samples = 101; const int repeats = 1000; var values = new double[samples]; double checksum = 0;
            // Allow tiered JIT/PGO to settle before timed samples, rather than assuming a small call count is warm.
            var warmStart = Stopwatch.GetTimestamp();
            for (var warm = 0; Stopwatch.GetElapsedTime(warmStart).TotalMilliseconds < 500; warm++)
                checksum += Evaluate(states[warm % count], new(warm)).PositionRoot.X;
            for (var sample = 0; sample < samples; sample++)
            {
                var start = Stopwatch.GetTimestamp();
                for (var repeat = 0; repeat < repeats; repeat++)
                    for (var i = 0; i < states.Length; i++) checksum += Evaluate(states[i], new(1000000 + repeat)).PositionRoot.X;
                values[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / repeats;
            }
            // The identical immutable state array and 500 ms warmup policy are reused independently.
            // Warmup contributes a variable number of calls to each checksum, so require the original
            // positive checksum in each pass; do not compare whole checksum bits across timed warmups.
            double allocationChecksum = 0;
            var allocationWarmStart = Stopwatch.GetTimestamp();
            for (var warm = 0; Stopwatch.GetElapsedTime(allocationWarmStart).TotalMilliseconds < 500; warm++)
                allocationChecksum += Evaluate(states[warm % count], new(warm)).PositionRoot.X;
            using var measurement = new OrdinaryAllocationMeasurement("timed-translation-performance");
            for (var sample = 0; sample < samples; sample++)
                for (var repeat = 0; repeat < repeats; repeat++)
                    for (var i = 0; i < states.Length; i++) allocationChecksum += Evaluate(states[i], new(1000000 + repeat)).PositionRoot.X;
            var allocated = measurement.Complete();
            OrdinaryAllocationMeasurement.RequireZero(allocated, "timed-translation-performance");
            Check(allocationChecksum > 0, "benchmark allocation-pass checksum");
            Check(checksum > 0, "benchmark timing-pass checksum");
            Array.Sort(values);
            Console.WriteLine($"Translation PERF craftCount={count} samples={samples} callsPerSample={repeats * count} medianBatchNs={values[50]:F2} p95BatchNs={values[95]:F2} p99BatchNs={values[99]:F2} medianPerCraftNs={values[50] / count:F2} allocatedBytes={allocated} checksum={checksum:R}");
            Console.WriteLine($"TIMED_RESULT gate=translation-performance craftCount={count} allocation={allocated} checksum={allocationChecksum:R} timingChecksum={checksum:R} correctness=PASS threshold=REPORT_ONLY");
        }
    }
}
