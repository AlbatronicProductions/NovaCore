using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class SpacecraftCommandTests
{
    internal static void Check(bool condition, string name)
    { if (!condition) throw new InvalidOperationException("Spacecraft command: " + name); }

    private sealed class Fixture
    {
        internal static readonly SpacecraftId Craft = new(101);
        internal static readonly ReferenceFrameId Root = new(1);
        internal readonly SimulationClock Clock;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftCommandAuthority Authority;
        internal Fixture(long origin = 0, long intervals = 1200, bool prepare = true)
        {
            var body = new ReferenceFrameId(2); var time = new SimulationInstant(origin);
            var graph = new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "root"));
            graph.Add(new ReferenceFrameNode(body, Root, ReferenceFrameKind.Ccf, "body"));
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, body, "command subject")],
                [new(Craft, time, DoubleQuaternion.Identity, new(0, 0, .1), new(1, 1, 1), default, RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(10)], [new(Craft, Root, time, default, default, default)], graph.Build(), out var store, out _), "fixture state");
            Clock = new(time, new SimulationTimeline(4)); Engine = new(Clock, new SimulationState(spacecraft: store), 4);
            if (prepare)
            {
                Check(Engine.PrepareSpacecraftCommands(Craft, intervals, out var authority) == SpacecraftCommandStatus.Accepted, "prepare");
                Authority = authority!;
            }
            else Authority = null!; // Storage measurement prepares through the actual engine after fixture setup.
        }
        internal SpacecraftCommandAdmission Admit(SpacecraftCommandIntent intent) =>
            Engine.AdmitSpacecraftCommand(Authority, Authority.Lease, Observe().LastAcceptedSequence + 1, intent);
        internal SpacecraftCommandCommit Commit() => Engine.CommitNextSpacecraftCommand(Authority);
        internal SpacecraftCommandObservation Observe()
        { Check(Engine.ObserveSpacecraftCommands(Authority, out var copy) == SpacecraftCommandStatus.Accepted, "observe"); return copy; }
        internal void Advance(long ticks)
        {
            var delta = ticks - Clock.CurrentTime.Ticks;
            Clock.AdvanceTo(new(ticks)); Clock.ConsumePendingSimulationDebt(new(delta));
        }
    }

    internal static void Cheap()
    {
        var f = new Fixture(); var originalClock = f.Engine.CaptureContinuationClock(); var physical = f.Engine.State.Revision;
        f.Engine.State.Spacecraft.TryGetTranslation(Fixture.Craft, out var linear, out _);
        f.Engine.State.Spacecraft.TryGetRigidBody(Fixture.Craft, out var angular);
        var axes = SpacecraftCommandIntent.Axes(new(.25, -.5, 1), new(-1, .5, .25));
        Check(f.Admit(axes) is { Status: SpacecraftCommandStatus.Accepted, EffectiveEpoch.Ticks: 0 }, "zero debt admission");
        var first = f.Commit();
        Check(first.Status == SpacecraftCommandStatus.Committed && first.Observation.Revision.Value == 1, "separate command revision");
        Check(f.Engine.CaptureContinuationClock() == originalClock && f.Engine.State.Revision == physical, "zero debt clock/debt/physical/timeline nonmutation");
        f.Engine.State.Spacecraft.TryGetTranslation(Fixture.Craft, out var linearAfter, out _);
        f.Engine.State.Spacecraft.TryGetRigidBody(Fixture.Craft, out var angularAfter);
        Check(linear == linearAfter && angular == angularAfter, "paired physical bits unchanged");
        var copy = f.Observe(); var demand = copy.ConsumeWithoutActuation();
        Check(demand.Requested.RotationIntent == axes.Rotation && demand.Requested.TranslationIntent == axes.Translation, "requested-only consumer");
        Check(f.Commit().Status == SpacecraftCommandStatus.NoCommand && f.Observe() == copy, "no sample preserves held state");
        Check(f.Engine.AdmitSpacecraftCommand(f.Authority, 1, 1, axes).Status == SpacecraftCommandStatus.DuplicateOrStaleSequence, "exact identity duplicate");
        Check(f.Admit(axes).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.NoChange, "repeated held state no-op");
        Check(f.Observe().Revision.Value == 1, "no-op does not revise");
        Check(f.Admit(SpacecraftCommandIntent.Axes(default, default)).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.Committed, "explicit release");
        Check(copy.State.RotationIntent == axes.Rotation && f.Observe().State.RotationIntent == default, "copied observation isolation");
        Check(f.Admit(SpacecraftCommandIntent.Axes(default, default)).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.NoChange, "repeated release");
        f.Admit(SpacecraftCommandIntent.Axes(new(-0d, 0, 0), default));
        Check(f.Commit().Status == SpacecraftCommandStatus.NoChange && BitConverter.DoubleToInt64Bits(f.Observe().State.RotationIntent.X) == 0, "no-op preserves signed-zero bits");

        foreach (var intent in new[] { SpacecraftCommandIntent.ThrottlePosition(.7), SpacecraftCommandIntent.Rcs(true),
            SpacecraftCommandIntent.ControlMode(RequestedControlMode.RateAssist),
            SpacecraftCommandIntent.DesiredTarget(new(CommandTargetKind.AngularRate, Fixture.Root, default, new(0, .2, 0))) })
        {
            Check(f.Admit(intent).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.Committed, "latched/typed transition");
            var revision = f.Observe().Revision;
            Check(f.Admit(intent).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.NoChange && f.Observe().Revision == revision, "latched/target repeat no-op");
        }
        Check(f.Observe().State.RequestedThrottlePosition == .7 && f.Observe().State.RequestedRcsEnabled, "throttle/RCS persist");
        var authoredTarget = new SpacecraftCommandTarget(CommandTargetKind.Attitude, Fixture.Root, new(1, 2, 3, 4), default);
        Check(f.Admit(SpacecraftCommandIntent.DesiredTarget(authoredTarget)).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.Committed, "nontrivial attitude normalized at admission");
        Check(f.Admit(SpacecraftCommandIntent.DesiredTarget(authoredTarget)).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.NoChange, "repeated original attitude no-op");
        Check(f.Admit(SpacecraftCommandIntent.DesiredTarget(f.Observe().State.Target)).Status == SpacecraftCommandStatus.Accepted && f.Commit().Status == SpacecraftCommandStatus.NoChange, "copied admitted attitude no-op without renormalization drift");
        var edgeRevision = f.Observe().Revision.Value;
        var ignite = f.Admit(SpacecraftCommandIntent.Ignite()); var shutdown = f.Admit(SpacecraftCommandIntent.Shutdown());
        Check(ignite.EffectiveEpoch == shutdown.EffectiveEpoch && ignite.Sequence + 1 == shutdown.Sequence, "same epoch authority order");
        var edge1 = f.Commit(); var edge2 = f.Commit();
        Check(edge1.Sequence == ignite.Sequence && edge1.Observation.State.LastEngineRequest == SpacecraftCommandKind.IgniteRequest, "ignite once");
        Check(edge2.Sequence == shutdown.Sequence && edge2.Observation.State.LastEngineRequest == SpacecraftCommandKind.ShutdownRequest, "shutdown follows ignite");
        Check(edge2.Observation.Revision.Value == edgeRevision + 2 && f.Commit().Status == SpacecraftCommandStatus.NoCommand, "two requests two transitions no repeat");
        Check(f.Engine.AdmitSpacecraftCommand(f.Authority, 1, ignite.Sequence, SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.DuplicateOrStaleSequence, "old edge cannot reignite");

        var beforeInvalid = f.Observe(); var accepted = f.Observe().LastAcceptedSequence;
        foreach (var invalid in new[] { default(SpacecraftCommandIntent), SpacecraftCommandIntent.ThrottlePosition(double.NaN),
            SpacecraftCommandIntent.ThrottlePosition(-.1), SpacecraftCommandIntent.ThrottlePosition(1.1),
            SpacecraftCommandIntent.Axes(new(double.PositiveInfinity, 0, 0), default), SpacecraftCommandIntent.Axes(default, new(2, 0, 0)),
            SpacecraftCommandIntent.ControlMode((RequestedControlMode)99),
            SpacecraftCommandIntent.DesiredTarget(new(CommandTargetKind.Attitude, new(99), DoubleQuaternion.Identity, default)),
            SpacecraftCommandIntent.DesiredTarget(new(CommandTargetKind.Attitude, Fixture.Root, default, default)),
            SpacecraftCommandIntent.DesiredTarget(new(CommandTargetKind.AngularRate, Fixture.Root, default, new(double.NaN, 0, 0))) })
            Check(f.Admit(invalid).Status == SpacecraftCommandStatus.InvalidInput, "invalid/nonfinite/frame refusal");
        Check(f.Observe() == beforeInvalid && f.Observe().LastAcceptedSequence == accepted, "invalid admission nonmutation");
        Check(f.Engine.AdmitSpacecraftCommand(f.Authority, 2, accepted + 1, axes).Status == SpacecraftCommandStatus.InvalidAuthority, "foreign lease");
        Check(f.Engine.AdmitSpacecraftCommand(f.Authority, 1, accepted + 2, axes).Status == SpacecraftCommandStatus.SequenceGap, "sequence gap");
        var other = new Fixture();
        Check(other.Engine.CommitNextSpacecraftCommand(f.Authority).Status == SpacecraftCommandStatus.InvalidAuthority, "foreign engine/capability");
        Check(f.Engine.PrepareSpacecraftCommands(Fixture.Craft, 1200, out _) == SpacecraftCommandStatus.AlreadyPrepared, "no resetting duplicate history");
        Check(Task.Run(() => f.Engine.CommitNextSpacecraftCommand(f.Authority)).GetAwaiter().GetResult().Status == SpacecraftCommandStatus.WrongOwnerThread, "wrong thread");
        Check(f.Clock.PublicationPhase.TryEnter(f), "test holds existing owner phase");
        try { Check(f.Admit(axes).Status == SpacecraftCommandStatus.ReentrantOperation && f.Commit().Status == SpacecraftCommandStatus.ReentrantOperation, "reentrant refusal"); }
        finally { f.Clock.PublicationPhase.Exit(); }
        f.Clock.TrySetRate(SimulationRate.Two);
        Check(f.Admit(axes).Status == SpacecraftCommandStatus.UnsupportedClock && f.Commit().Status == SpacecraftCommandStatus.UnsupportedClock, "unsupported warp");
        f.Clock.TrySetRate(SimulationRate.One); f.Clock.Pause();
        Check(f.Admit(axes).Status == SpacecraftCommandStatus.UnsupportedClock && f.Commit().Status == SpacecraftCommandStatus.UnsupportedClock, "unsupported pause");
        f.Clock.Resume();

        var overflow = new Fixture(); overflow.Engine.SaturateCommandRevisionForTest(overflow.Authority);
        Check(overflow.Admit(axes).Status == SpacecraftCommandStatus.Accepted, "overflow test admission");
        var overflowBefore = overflow.Observe();
        Check(overflow.Commit().Status == SpacecraftCommandStatus.RevisionOverflow && overflow.Observe() == overflowBefore, "revision overflow atomic refusal");
        var refusal = new Fixture(); refusal.Admit(SpacecraftCommandIntent.DesiredTarget(SpacecraftCommandTarget.Hold(Fixture.Root)));
        var refusalBefore = refusal.Observe();
        Check(refusal.Engine.RefusePreparedSpacecraftCommandForTest(refusal.Authority).Status == SpacecraftCommandStatus.PreparationRefused && refusal.Observe() == refusalBefore, "prepared capture refusal has no partial target/sequence/consumer");
        Check(refusal.Commit().Status == SpacecraftCommandStatus.Committed, "same pending command can commit after refusal");

        var capacity = new Fixture();
        for (var i = 0; i < SpacecraftCommandAuthority.OrdinaryCapacity; i++) Check(capacity.Admit(axes).Status == SpacecraftCommandStatus.Accepted, "bounded burst");
        var full = capacity.Observe();
        Check(capacity.Admit(axes).Status == SpacecraftCommandStatus.Capacity && capacity.Observe() == full, "saturation preserves accepted inputs");
        var revoke = capacity.Engine.RevokeSpacecraftControl(capacity.Authority, 1, 8);
        Check(revoke.Status == SpacecraftCommandStatus.Accepted && capacity.Observe().PendingCount == 8, "reserved administrative capacity");
        Check(capacity.Admit(axes).Status == SpacecraftCommandStatus.IngressRevoked, "immediate ingress revocation");
        for (var i = 0; i < 7; i++) Check(capacity.Commit().Sequence == (ulong)i + 1, "historical lease commands before revocation retained");
        Check(capacity.Commit().Sequence == 8 && capacity.Observe().State.RotationIntent == default, "ordered neutralization after earlier equal-epoch commands");
        Check(capacity.Observe().Revision.Value == 2, "one held transition and one neutralization");

        Backlog(); Boundaries(); TargetAdmissionPartitions();
        Check(!RuntimeHelpers.IsReferenceOrContainsReferences<SpacecraftCommandState>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<SpacecraftCommandObservation>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<RequestedControlDemand>(), "no camera/render/BEPU/device/live references in values");
        Check(typeof(SpacecraftCommandAuthority).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .All(field => field.IsInitOnly && field.FieldType.IsValueType), "issued capability exposes no mutable storage or internal array");
        Console.WriteLine("COMMAND_CHEAP PASS zero-debt/lifetime/edges/noops/frames/revocation/capacity/refusal/revision/nonactuation");
    }

    private static void Backlog()
    {
        var f = new Fixture(); f.Admit(SpacecraftCommandIntent.ThrottlePosition(.2)); f.Commit();
        f.Clock.AdvanceByHostDuration(new(50_000));
        var future = f.Admit(SpacecraftCommandIntent.ThrottlePosition(.8));
        Check(future.EffectiveEpoch.Ticks == 50_000, "funded horizon exact boundary");
        f.Clock.AdvanceByHostDuration(new(50_000));
        Check(f.Observe().NextPendingEpoch == future.EffectiveEpoch, "later funding cannot retime accepted input");
        foreach (var tick in new long[] { 16_666, 33_333 })
        {
            f.Engine.CloseSpacecraftCommandBoundary(f.Authority, out _); f.Advance(tick);
            Check(f.Commit().Status == SpacecraftCommandStatus.Pending && f.Observe().State.RequestedThrottlePosition == .2, "earlier funded work keeps old state");
        }
        f.Engine.CloseSpacecraftCommandBoundary(f.Authority, out _); f.Advance(50_000);
        Check(f.Commit().Status == SpacecraftCommandStatus.Committed && f.Observe().State.RequestedThrottlePosition == .8, "prospective effective epoch");
        var held = f.Admit(SpacecraftCommandIntent.Axes(new(1, 0, 0), default));
        var revoked = f.Engine.RevokeSpacecraftControl(f.Authority, 1, held.Sequence + 1);
        Check(revoked.EffectiveEpoch.Ticks == 100_000 && f.Observe().State.RequestedThrottlePosition == .8, "focus loss does not rewrite funded state");
        f.Advance(100_000); Check(f.Commit().Sequence == held.Sequence, "earlier accepted held applies before neutralization");
        Check(f.Commit().Sequence == revoked.Sequence && f.Observe().State.RotationIntent == default && f.Observe().State.RequestedThrottlePosition == .8, "prospective release preserves latched throttle");

        var capture = new Fixture(); capture.Clock.AdvanceByHostDuration(new(50_000));
        Check(capture.Admit(SpacecraftCommandIntent.DesiredTarget(SpacecraftCommandTarget.Hold(Fixture.Root))).Status == SpacecraftCommandStatus.Accepted, "future hold admission");
        capture.Advance(50_000);
        Check(SpacecraftMotionEvaluator.TryEvaluate(capture.Engine.State, Fixture.Craft, capture.Clock.CurrentTime, out var canonical) == SpacecraftTranslationStatus.Success, "canonical target oracle");
        var result = capture.Commit();
        Check(result.Observation.State.Target.Attitude == canonical.BodyToRoot && canonical.BodyToRoot != DoubleQuaternion.Identity, "capture uses effective canonical epoch not admission pose");
        Console.WriteLine("COMMAND_BACKLOG PASS H=50000 E=50000 later-H=100000 old-state-through=33333 new-state-at=50000 hold=captured-at-E");
    }

    private static void TargetAdmissionPartitions()
    {
        var a = new Fixture(); var b = new Fixture();
        var raw = new SpacecraftCommandTarget(CommandTargetKind.Attitude, Fixture.Root, new(1, 2, 3, 4), default);
        Check(SpacecraftAttitudeEvaluator.TryCanonicalize(raw.Attitude, out var canonical) == SpacecraftAttitudeEvaluationStatus.Success, "target oracle");
        var copied = raw with { Attitude = canonical };
        a.Admit(SpacecraftCommandIntent.DesiredTarget(raw)); var firstA = a.Commit();
        a.Clock.AdvanceByHostDuration(new(16_666)); var acceptedA = a.Admit(SpacecraftCommandIntent.DesiredTarget(copied));
        b.Admit(SpacecraftCommandIntent.DesiredTarget(raw));
        b.Clock.AdvanceByHostDuration(new(16_666)); var acceptedB = b.Admit(SpacecraftCommandIntent.DesiredTarget(copied));
        var firstB = b.Commit();
        Check(acceptedA == acceptedB && firstA.Observation.State == firstB.Observation.State, "same immutable accepted values/epochs despite interleaved admission");
        a.Advance(16_666); b.Advance(16_666); var secondA = a.Commit(); var secondB = b.Commit();
        Check(secondA.Status == SpacecraftCommandStatus.NoChange && secondB.Status == secondA.Status &&
            secondA.Observation.State == secondB.Observation.State && secondA.Observation.Revision == secondB.Observation.Revision,
            "normalization cannot make command history depend on admission/consumption partition");
    }

    private static void Boundaries()
    {
        var f = new Fixture();
        Check(f.Commit().Status == SpacecraftCommandStatus.NoCommand && f.Observe().ClosedThroughBoundaryIndex == -1, "polling no work keeps boundary open");
        Check(f.Engine.CloseSpacecraftCommandBoundary(f.Authority, out _) == SpacecraftCommandStatus.BoundaryReady, "explicit owner freeze");
        var closed = f.Observe();
        Check(f.Engine.CloseSpacecraftCommandBoundary(f.Authority, out var secondDemand) == SpacecraftCommandStatus.BoundaryClosed && secondDemand == default && f.Observe() == closed, "duplicate close distinctly refused without bogus ready demand");
        var a = f.Admit(SpacecraftCommandIntent.Ignite());
        Check(a.EffectiveEpoch.Ticks == 16_666 && f.Commit().Status == SpacecraftCommandStatus.BoundaryClosed, "closed zero-debt boundary selects next");
        f.Clock.AdvanceByHostDuration(new(33_333)); f.Advance(33_333);
        var previous = f.Observe();
        Check(f.Commit().Status == SpacecraftCommandStatus.MissedBoundary && f.Observe() == previous, "skipped E fails closed not retrospective");
        var nonlattice = new Fixture(); nonlattice.Clock.AdvanceTo(new(1));
        Check(nonlattice.Commit().Status == SpacecraftCommandStatus.NotAtBoundary, "no invented off-lattice consumption");
        var horizon = new Fixture(); horizon.Clock.AdvanceByHostDuration(new(16_667));
        Check(horizon.Admit(SpacecraftCommandIntent.Ignite()).EffectiveEpoch.Ticks == 33_333, "integer ceil after fractional 60Hz endpoint");
        var exhausted = new Fixture(intervals: 1); exhausted.Clock.AdvanceByHostDuration(new(16_667));
        Check(exhausted.Admit(SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.SourceExhausted, "finite source end");
        var arithmetic = new Fixture(origin: 100); arithmetic.Clock.AdvanceByHostDuration(new(long.MaxValue));
        Check(arithmetic.Admit(SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.ArithmeticOverflow, "funded horizon overflow");
        var regression = new Fixture(); regression.Clock.AdvanceByHostDuration(new(50_000)); regression.Admit(SpacecraftCommandIntent.Ignite());
        regression.Clock.ConsumePendingSimulationDebt(new(1));
        Check(regression.Admit(SpacecraftCommandIntent.Shutdown()).Status == SpacecraftCommandStatus.FundedHorizonRegressed, "unrelated debt removal cannot reorder accepted horizon");
        foreach (var tick in new long[] { 0, 16_666 })
        {
            var events = new Fixture(); events.Clock.AdvanceByHostDuration(new(16_666));
            events.Clock.Timeline.Schedule(events.Clock.CurrentTime, new(new(1), new(tick), 0, SimulationEventKind.Marker));
            Check(events.Admit(SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.PendingEvent, "event at/before E admission blocker");
        }
        var blocked = new Fixture(); blocked.Admit(SpacecraftCommandIntent.Ignite());
        blocked.Clock.Timeline.Schedule(blocked.Clock.CurrentTime, new(new(1), new(0), 0, SimulationEventKind.Marker));
        var pending = blocked.Observe();
        Check(blocked.Commit().Status == SpacecraftCommandStatus.PendingEvent && blocked.Observe() == pending && blocked.Clock.Timeline.PendingCount == 1, "new event precommit preserves command/event");
    }

    internal static void Schedules()
    {
        var reference = Replay(30);
        foreach (var fps in new[] { 60, 150, 240, 0 })
        {
            var actual = Replay(fps);
            Check(reference.AsSpan().SequenceEqual(actual), "identical accepted stream yields identical committed command history");
        }
        Console.WriteLine("COMMAND_SCHEDULES PASS 30/60/150/240/delayed history=8/8 funding=2000000 boundary=120");
    }

    private static SpacecraftCommandCommit[] Replay(int fps)
    {
        var f = new Fixture(); var history = new SpacecraftCommandCommit[8]; var count = 0;
        f.Admit(SpacecraftCommandIntent.Axes(new(.2, 0, 0), default)); history[count++] = f.Commit();
        // One identical accepted authority manifest, captured against the same canonical funded horizons.
        // Host partitions BELOW vary subsequent credit/service, not device observation or assigned epochs.
        var ticks = new long[] { 50_000, 100_000, 100_000, 150_000, 200_000, 250_000, 250_000 };
        var intents = new[] { SpacecraftCommandIntent.ThrottlePosition(.6), SpacecraftCommandIntent.Ignite(), SpacecraftCommandIntent.Shutdown(),
            SpacecraftCommandIntent.DesiredTarget(SpacecraftCommandTarget.Hold(Fixture.Root)), SpacecraftCommandIntent.Rcs(true),
            SpacecraftCommandIntent.ControlMode(RequestedControlMode.AttitudeAssist), SpacecraftCommandIntent.Axes(default, default) };
        long funded = 0;
        for (var i = 0; i < ticks.Length; i++)
        {
            f.Clock.AdvanceByHostDuration(new(ticks[i] - funded)); funded = ticks[i];
            Check(f.Admit(intents[i]).EffectiveEpoch.Ticks == ticks[i], "identical epoch/sequence manifest");
        }
        var step = 0; var frames = 0;
        while (step < 120)
        {
            frames++;
            var total = Math.Min(2_000_000L, 250_000 + (fps == 0 ? frames * 350_000L : (long)((Int128)frames * 1_000_000 / fps)));
            f.Clock.AdvanceByHostDuration(new(total - funded)); funded = total;
            for (var budget = 0; budget < 4 && step < 120; budget++)
            {
                Drain();
                var target = (long)((Int128)(step + 1) * 1_000_000 / 60);
                if (f.Clock.PendingSimulationDebt.Ticks < target - f.Clock.CurrentTime.Ticks) break;
                Check(f.Engine.CloseSpacecraftCommandBoundary(f.Authority, out var demand) == SpacecraftCommandStatus.BoundaryReady, "close evaluation boundary");
                Check(demand.Requested == f.Observe().State, "copied non-actuating demand at interval");
                f.Advance(target); step++;
            }
            Check(f.Clock.CurrentTime.Ticks + f.Clock.PendingSimulationDebt.Ticks == funded, "funding conservation independent of commands");
        }
        Drain(); Check(count == history.Length && funded == 2_000_000 && f.Clock.PendingSimulationDebt.Ticks == 0, "full manifest/drained backlog");
        Check(f.Engine.State.Revision.Value == 0 && f.Clock.Timeline.Revision.Value == 0, "command replay never publishes physics or timeline");
        return history;

        void Drain()
        {
            while (true)
            {
                var result = f.Commit();
                if (result.Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange) { history[count++] = result; continue; }
                Check(result.Status is SpacecraftCommandStatus.NoCommand or SpacecraftCommandStatus.Pending, "bounded exact consumption"); break;
            }
        }
    }

    private static bool Operation(Fixture f, int i)
    {
        if (f.Admit(SpacecraftCommandIntent.ThrottlePosition((i & 1) == 0 ? .25 : .75)).Status != SpacecraftCommandStatus.Accepted) return false;
        var result = f.Commit();
        if (result.Status != SpacecraftCommandStatus.Committed) return false;
        return f.Engine.ObserveSpacecraftCommands(f.Authority, out var observation) == SpacecraftCommandStatus.Accepted &&
            observation.ConsumeWithoutActuation().Requested.RequestedThrottlePosition == ((i & 1) == 0 ? .25 : .75);
    }

    internal static void Allocation()
    {
        var f = new Fixture();
        for (var i = 0; i < 128; i++) Check(Operation(f, i), "allocation warmup");
        bool complete = true;
        using (var m = new OrdinaryAllocationMeasurement("command-complete"))
        {
            for (var i = 0; i < 1024; i++) complete &= Operation(f, i);
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-complete");
        }
        Check(complete, "complete workload count/results");
        for (var i = 0; i < 7; i++) f.Admit(SpacecraftCommandIntent.Ignite());
        for (var i = 0; i < 7; i++) f.Commit();
        bool admitted = true;
        using (var m = new OrdinaryAllocationMeasurement("command-admission"))
        {
            for (var i = 0; i < 7; i++) admitted &= f.Admit(SpacecraftCommandIntent.Ignite()).Status == SpacecraftCommandStatus.Accepted;
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-admission");
        }
        Check(admitted, "seven admissions");
        bool committed = true;
        using (var m = new OrdinaryAllocationMeasurement("command-commit"))
        {
            for (var i = 0; i < 7; i++) committed &= f.Commit().Status == SpacecraftCommandStatus.Committed;
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-commit");
        }
        Check(committed, "seven commits");
        f.Commit(); f.Engine.ObserveSpacecraftCommands(f.Authority, out _);
        using (var m = new OrdinaryAllocationMeasurement("command-no-work-observation-consumption"))
        {
            for (var i = 0; i < 1024; i++)
            {
                complete &= f.Commit().Status == SpacecraftCommandStatus.NoCommand;
                complete &= f.Engine.ObserveSpacecraftCommands(f.Authority, out var copy) == SpacecraftCommandStatus.Accepted;
                complete &= copy.ConsumeWithoutActuation().Revision == copy.Revision;
            }
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-no-work-observation-consumption");
        }
        var backlog = new Fixture(); backlog.Clock.AdvanceByHostDuration(new(50_000)); backlog.Admit(SpacecraftCommandIntent.Ignite()); backlog.Commit();
        using (var m = new OrdinaryAllocationMeasurement("command-pending-poll"))
        {
            for (var i = 0; i < 1024; i++) complete &= backlog.Commit().Status == SpacecraftCommandStatus.Pending;
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-pending-poll");
        }
        Check(complete, "no-work/backlog predicates");
        var warmBacklog = PreparedBacklog(); Check(ConsumeBacklog(warmBacklog), "future consumption warmup");
        var measuredBacklog = PreparedBacklog();
        using (var m = new OrdinaryAllocationMeasurement("command-funded-backlog-consumption"))
        {
            complete = ConsumeBacklog(measuredBacklog);
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(), "command-funded-backlog-consumption");
        }
        Check(complete && measuredBacklog.Clock.CurrentTime.Ticks == 50_000 && measuredBacklog.Clock.PendingSimulationDebt.Ticks == 50_000,
            "seven future commits at E preserve later debt");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("COMMAND_ALLOCATION PASS complete=0 admission=0 commit=0 observation-consumer=0 no-work=0 backlog=0");

        static Fixture PreparedBacklog()
        {
            var result = new Fixture(); result.Clock.AdvanceByHostDuration(new(50_000));
            for (var i = 0; i < 7; i++) Check(result.Admit(SpacecraftCommandIntent.Ignite()).EffectiveEpoch.Ticks == 50_000, "queued future allocation fixture");
            result.Clock.AdvanceByHostDuration(new(50_000)); return result;
        }
        static bool ConsumeBacklog(Fixture result)
        {
            var ok = result.Engine.CloseSpacecraftCommandBoundary(result.Authority, out _) == SpacecraftCommandStatus.BoundaryReady;
            result.Advance(50_000);
            for (var i = 0; i < 7; i++) ok &= result.Commit().Status == SpacecraftCommandStatus.Committed;
            ok &= result.Engine.ObserveSpacecraftCommands(result.Authority, out var copy) == SpacecraftCommandStatus.Accepted;
            return ok && copy.ConsumeWithoutActuation().Requested.EngineRequestSequence == 7;
        }
    }

    internal static void Cost()
    {
        var f = new Fixture();
        for (var i = 0; i < 256; i++) Check(Operation(f, i), "cost warmup");
        var samples = new double[4096]; var complete = true;
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); complete &= Operation(f, i);
            samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        Check(complete, "cost complete operations"); Array.Sort(samples);
        Console.WriteLine($"COMMAND_PERFORMANCE samples={samples.Length} median_ms={samples[samples.Length / 2]:R} p95_ms={samples[(int)Math.Ceiling(samples.Length * .95) - 1]:R} p99_ms={samples[(int)Math.Ceiling(samples.Length * .99) - 1]:R} max_ms={samples[^1]:R}");
        var cold = new Fixture(prepare: false);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var prepared = cold.Engine.PrepareSpacecraftCommands(Fixture.Craft, 1200, out var owner);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(prepared == SpacecraftCommandStatus.Accepted, "storage measures complete preparation");
        GC.KeepAlive(owner);
        var payload = Unsafe.SizeOf<PendingSpacecraftCommand>() * 8;
        Console.WriteLine($"COMMAND_STORAGE capability_owner_array_bytes={bytes} queue_payload_bytes={payload} state_value_bytes={Unsafe.SizeOf<SpacecraftCommandState>()} observation_value_bytes={Unsafe.SizeOf<SpacecraftCommandObservation>()} demand_value_bytes={Unsafe.SizeOf<RequestedControlDemand>()} engine_reference_bytes={IntPtr.Size} production_history_bytes=0");
    }
}
