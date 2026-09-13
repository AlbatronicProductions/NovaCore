using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class PersistentContactPublicationTests
{
    private static readonly SpacecraftId Craft = new(901);
    private static readonly ReferenceFrameId Root = new(1), Body = new(902);
    private static readonly Double3 Dimensions = new(2, 1, 1);
    private sealed record Fixture(SimulationState State, SpacecraftStateStore Store, SimulationClock Clock,
        SimulationTransactionEngine Engine, LocalContactConfiguration Configuration, LocalContactSource Source);
    private readonly record struct Snapshot(SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular,
        SpacecraftPhysicalProperties Properties, StateRevision Revision, ContinuationClockState Clock,
        TimelineRevision Timeline, int Pending, int Events, int Publications, int Certified, long Marker,
        int Force, int Torque, int Attitude, bool HasPending, ScheduledSimulationEvent PendingEvent,
        ProcessedPersistentContact LastPublication);

    private static void Check(bool value, string contract)
    { if (!value) throw new InvalidOperationException("Persistent contact: " + contract); }
    private static void Status(LocalContactStatus value, string contract) => Check(value == LocalContactStatus.Success, contract + ": " + value);
    private static void PublicationStatus(PersistentContactPublicationResult result, PersistentContactPublicationStatus expected, string contract)
    { if (result.Status != expected) throw new InvalidOperationException($"Persistent contact: {contract}: expected {expected}, actual {result.Status}"); }
    private static double Length(Double3 value) => Math.Sqrt(value.LengthSquared);

    private static Fixture Create(bool tilted = false, bool moving = false, int capacity = 1200,
        long debt = 30_000_000, StateRevision revision = default, bool unsafeExport = false, long? startTicks = null)
    {
        var origin = moving ? new Double3(1e9, -2e9, 3e9) : Double3.Zero;
        var velocity = moving ? new Double3(11, -7, 3) : Double3.Zero;
        var frame = moving ? DoubleQuaternion.FromAxisAngle(new(1, 2, -1), .7) : DoubleQuaternion.Identity;
        var start = new SimulationInstant(startTicks ?? (moving ? 1234567 : 0));
        Status(LocalContactConfiguration.TryCreate(1, Root, origin, velocity, frame, Dimensions, 64, out var c), "configuration");
        var linear = new SpacecraftTranslationState(Craft, Root, start, origin + frame.Rotate(new(0, 2, 0)), velocity,
            frame.Rotate(new Double3(0, unsafeExport ? -1e12 : -9.81, 0) * 1000));
        var angular = new SpacecraftRigidBodyRotationState(Craft, start,
            frame * (tilted ? DoubleQuaternion.FromAxisAngle(Double3.UnitZ, .25) : DoubleQuaternion.Identity),
            Double3.Zero, c!.BoxInertia(1000), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        var graph = new ReferenceFrameGraphBuilder();
        graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "Qualification inertial root"));
        graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "Qualification-only body"));
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "Qualification-only box")],
            [angular], [new(1000)], [linear], graph.Build(), out var store, out _), "canonical fixture");
        var state = new SimulationState(spacecraft: store, initialRevision: revision);
        var clock = new SimulationClock(start, new SimulationTimeline(4));
        if (debt >= 0) clock.AdvanceByHostDuration(new(debt));
        else Set(clock, "_pendingSimulationDebt", new SimulationDuration(debt)); // invalid-state arithmetic refusal witness
        var engine = new SimulationTransactionEngine(clock, state, 4, persistentContactHistoryCapacity: capacity);
        Status(LocalContactSource.Capture(engine, Craft, c, start + SimulationDuration.FromWholeSeconds(30), out var source), "source capture");
        return new(state, store!, clock, engine, c, source!);
    }

    private static LocalContactWorld World(Fixture f, bool publishing, out LocalContactWorld.Receipt receipt)
    {
        Status(LocalContactWorld.TryCreate(f.Engine, f.Source, f.Configuration, out var world, out receipt), "world creation");
        if (publishing) Status(f.Engine.BeginPersistentContact(world!, f.Configuration, receipt), "publication binding");
        return world!;
    }

    // This exact complete operation is shared by physical, allocation and timing qualification.
    private static bool Operation(Fixture f, LocalContactWorld world, ref LocalContactWorld.Receipt receipt,
        int step, out LocalContactWorld.Export endpoint, out PersistentContactPublicationResult result)
    {
        endpoint = default; result = default;
        if (!f.Source.TryEndpoint(step, out var target) ||
            world.Step(f.Engine, f.Configuration, receipt, target, out var next) != LocalContactStatus.Success) return false;
        receipt = next;
        if (world.Read(f.Engine, f.Configuration, receipt, out endpoint) != LocalContactStatus.Success) return false;
        result = f.Engine.PublishPersistentContact(world, f.Configuration, new(receipt, f.Engine.CaptureContinuationClock()));
        return result.Status == PersistentContactPublicationStatus.Published;
    }
    private static void Stage(Fixture f, LocalContactWorld world, ref LocalContactWorld.Receipt receipt, int step = 1)
    {
        Check(f.Source.TryEndpoint(step, out var target), "target");
        Status(world.Step(f.Engine, f.Configuration, receipt, target, out var next), "stage"); receipt = next;
    }
    private static PersistentContactPublicationResult Publish(Fixture f, LocalContactWorld w, LocalContactWorld.Receipt r) =>
        f.Engine.PublishPersistentContact(w, f.Configuration, new(r, f.Engine.CaptureContinuationClock()));
    private static Snapshot Capture(Fixture f)
    {
        var view = f.Engine.State;
        Check(view.Spacecraft.TryGetTranslation(Craft, out var linear, out var properties) &&
            view.Spacecraft.TryGetRigidBody(Craft, out _), "snapshot exists");
        view.Spacecraft.TryGetRigidBody(Craft, out var angular);
        var hasPending = f.Clock.Timeline.TryPeekPending(out var pending);
        f.Engine.TryGetProcessedPersistentContact(f.Engine.ProcessedPersistentContactCount-1,out var last);
        return new(linear, angular, properties, view.Revision, f.Engine.CaptureContinuationClock(), f.Clock.Timeline.Revision,
            f.Clock.Timeline.PendingCount, f.Engine.ProcessedCount, f.Engine.ProcessedPersistentContactCount,
            f.Engine.ProcessedContinuationCount, view.MarkerValue, f.Engine.ProcessedSpacecraftForceCount,
            f.Engine.ProcessedRigidBodyTorqueCount, f.Engine.ProcessedSpacecraftAttitudeCount,hasPending,pending,last);
    }
    private static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a, Double3 b) => Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z);
    private static bool Bits(DoubleQuaternion a, DoubleQuaternion b) => Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z) && Bits(a.W,b.W);

    internal static void Run()
    {
        LongSequence(false, false); LongSequence(true, false); LongSequence(true, true);
        DeterministicHistory(); ClockBoundaries(); Refusals(); AcknowledgementFailure(); AllocationAndStorage();
        Check(!RuntimeHelpers.IsReferenceOrContainsReferences<ProcessedPersistentContact>(), "history has no live references");
        Console.WriteLine("PASS Persistent contact publication: exact paired authority, retained trajectory, 28 refusal/terminal responsibilities, allocation/storage");
    }

    private static void LongSequence(bool tilted, bool moving)
    {
        var f = Create(tilted, moving); var control = Create(tilted, moving);
        using var world = World(f, true, out var receipt);
        using var privateWorld = World(control, false, out var privateReceipt);
        var original = Capture(f); var privateOriginal = Capture(control); var generation = world.Generation;
        var support = 0; var geometricPeak = 0d; var restMin = double.MaxValue; var restMax = double.MinValue;
        var maxOmega = 0d; var maxDepth = 0d; var shorts = 0; var longs = 0;
        for (var step = 1; step <= 1200; step++)
        {
            var before = Capture(f);
            Check(Operation(f, world, ref receipt, step, out var endpoint, out var result), "complete interval");
            Stage(control, privateWorld, ref privateReceipt, step);
            Status(privateWorld.Read(control.Engine, control.Configuration, privateReceipt, out var expected), "private control read");
            Check(endpoint.Motion with { Revision = expected.Motion.Revision } == expected.Motion &&
                Bits(endpoint.Motion.PositionRoot, expected.Motion.PositionRoot) &&
                Bits(endpoint.Motion.VelocityRoot, expected.Motion.VelocityRoot) &&
                Bits(endpoint.Motion.BodyToRoot, expected.Motion.BodyToRoot) &&
                Bits(endpoint.Motion.AngularVelocityBody, expected.Motion.AngularVelocityBody),
                "self-publication does not perturb any private physical bits");
            Check(world.Generation == generation && receipt.Generation == generation, "same world generation");
            var target = f.Source.Motion.Time.Ticks + (long)((Int128)step * 1_000_000 / 60);
            var delta = target - before.Clock.Time.Ticks;
            Check(delta == (step % 3 == 1 ? 16666 : 16667), "original ordered tick lattice");
            if (delta == 16666) shorts++; else longs++;
            var after = Capture(f); var motion = endpoint.Motion;
            Check(after.Linear.Epoch.Ticks == target && after.Angular.Epoch.Ticks == target && after.Clock.Time.Ticks == target, "exact paired target epochs");
            Check(Bits(after.Linear.PositionRoot,motion.PositionRoot) && Bits(after.Linear.VelocityRoot,motion.VelocityRoot) &&
                Bits(after.Angular.OrientationLocalToParent,motion.BodyToRoot) && Bits(after.Angular.AngularVelocityBody,motion.AngularVelocityBody), "raw staged bits installed exactly");
            Check(after.Revision.Value == (ulong)step && after.Timeline == original.Timeline, "one state revision, unchanged timeline revision");
            Check(after.Clock.Debt.Ticks == original.Clock.Debt.Ticks - (target - f.Source.Motion.Time.Ticks) &&
                after.Clock.Debt.Ticks == before.Clock.Debt.Ticks - delta, "exact integral debt subtraction");
            Check(after.Clock.Rate == original.Clock.Rate && after.Clock.RateRemainder == original.Clock.RateRemainder &&
                after.Clock.Paused == original.Clock.Paused, "unrelated clock fields preserved");
            Check(after.Linear.ConstantForceRoot == original.Linear.ConstantForceRoot && after.Properties == original.Properties &&
                after.Angular.PrincipalInertia == original.Angular.PrincipalInertia && after.Angular.Model == original.Angular.Model &&
                after.Angular.ConstantBodyTorque == original.Angular.ConstantBodyTorque, "physical configuration preserved");
            Check(after.Pending == 0 && after.Events == 0 && after.Certified == 0 && after.Publications == step, "dedicated history only");
            Check(f.Engine.TryGetProcessedPersistentContact(step - 1, out var record) && record.Frontier == step &&
                record.Index == step - 1 && record.BeforeClock == before.Clock && record.AfterClock == after.Clock &&
                record.BeforeRevision == before.Revision && record.AfterRevision == after.Revision &&
                record.BeforeTranslation == before.Linear && record.BeforeRotation == before.Angular &&
                record.AfterTranslation == after.Linear && record.AfterRotation == after.Angular && record.Version == 1 &&
                record.Episode.Start == f.Source.Motion.Time && record.Episode.Origin == f.Configuration.OriginRoot &&
                record.Episode.OriginVelocity == f.Configuration.OriginVelocityRoot && record.Episode.Sequence == 1,
                "deterministic history and original frame epoch");
            Check(result.Observation.HistoryIndex == step - 1 && result.Observation.Frontier == step &&
                result.Observation.Translation == after.Linear, "copied committed observation");
            // Independent reconstruction in the original moving plane; no BEPU depth flag used here.
            var c = f.Configuration;
            var origin = c.OriginRoot + c.OriginVelocityRoot * ((target - f.Source.Motion.Time.Ticks) / 1_000_000d);
            var center = c.LocalToRoot.Conjugate().Rotate(motion.PositionRoot - origin);
            var minY = double.MaxValue;
            for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var corner = center + c.LocalToRoot.Conjugate().Rotate(motion.BodyToRoot.Rotate(new(x, y*.5, z*.5)));
                        minY = Math.Min(minY, corner.Y);
                    }
            geometricPeak = Math.Max(geometricPeak, -minY); maxDepth = Math.Max(maxDepth, endpoint.MaximumDepth);
            maxOmega = Math.Max(maxOmega, Length(motion.AngularVelocityBody));
            if (step > 600)
            {
                restMin = Math.Min(restMin, center.Y); restMax = Math.Max(restMax, center.Y);
                if (Math.Abs(minY) <= .002 && Math.Abs(center.Y - .5) <= .002 &&
                    Length(motion.VelocityRoot - c.OriginVelocityRoot) <= .06 && endpoint.ConstraintCount > 0 && endpoint.ContactPoints >= 2) support++;
            }
        }
        Check(shorts == 400 && longs == 800, "1200 exact ordered intervals");
        Check(support == 600 && geometricPeak <= .020 && maxDepth <= .020 && restMax-restMin <= .001,
            "600 independent supported intervals and accepted penetration/drift bounds");
        if (tilted) Check(maxOmega > .1, "angular impact exercised");
        Check(Capture(control) == privateOriginal, "private-only canonical nonmutation");
        Console.WriteLine($"PERSISTENT_SEQUENCE tilted={tilted} moving={moving} steps=1200 support={support}/600 short={shorts} long={longs} geometric_peak_m={geometricPeak:R} depth_m={maxDepth:R} rest_drift_m={restMax-restMin:R} trajectory=EXACT");
    }

    // Reflection is confined to test setup to exercise otherwise unreachable corrupted/stale inputs.
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static T[] Array<T>(object target, string field) =>
        (T[])target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static void MutationRefusal(string name, Action<Fixture> mutation, PersistentContactPublicationStatus expected)
    {
        var f = Create(); using var world = World(f, true, out var receipt); Stage(f, world, ref receipt);
        mutation(f); var before = Capture(f);
        PublicationStatus(Publish(f, world, receipt), expected, name);
        Check(Capture(f) == before, name + " canonical nonmutation");
        Console.WriteLine("PERSISTENT_REFUSAL " + name + " nonmutation=PASS");
    }

    private static void DeterministicHistory()
    {
        var a=Create(tilted:true); var b=Create(tilted:true);
        using var wa=World(a,true,out var ra); using var wb=World(b,true,out var rb);
        Check(wa.Generation!=wb.Generation,"different global generations in replay witness");
        for(var step=1;step<=80;step++)
        {
            Check(Operation(a,wa,ref ra,step,out _,out _) && Operation(b,wb,ref rb,step,out _,out _),"history replay operation");
            Check(a.Engine.TryGetProcessedPersistentContact(step-1,out var ar) &&
                b.Engine.TryGetProcessedPersistentContact(step-1,out var br) && ar==br,"canonical history independent of process generation order");
        }
        Console.WriteLine("PERSISTENT_HISTORY deterministic_replay=80/80 different_world_generations=PASS");
    }

    private static void ClockBoundaries()
    {
        foreach(var start in new[]{long.MinValue,long.MaxValue-30_000_000})
        {
            var f=Create(startTicks:start,debt:long.MaxValue);
            f.Clock.TrySetRate(new(2,3)); f.Clock.AdvanceByHostDuration(new(1)); f.Clock.Pause();
            using var w=World(f,true,out var r); var initial=f.Engine.CaptureContinuationClock();
            Check(initial.RateRemainder==2 && initial.Paused,"nontrivial clock fixture");
            for(var step=1;step<=3;step++) Check(Operation(f,w,ref r,step,out _,out _),"extreme clock complete operation");
            var actual=f.Engine.CaptureContinuationClock();
            Check(actual.Time.Ticks==start+50_000 && actual.Debt.Ticks==long.MaxValue-50_000 &&
                actual.Rate==initial.Rate && actual.RateRemainder==2 && actual.Paused,
                "checked extreme ticks/debt, preserved rate/remainder/pause");
        }
        Console.WriteLine("PERSISTENT_CLOCK extreme_ticks=PASS max_debt=PASS rate_remainder_pause=UNCHANGED");
    }

    private static void Refusals()
    {
        MutationRefusal("stale StateRevision / numerically identical external mutation", f => f.State.CommitMarkerValue(0), PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("stale TimelineRevision", f => f.Clock.Timeline.Schedule(f.Clock.CurrentTime,
            new(new(1), new(40_000_000), 0, SimulationEventKind.Marker)), PersistentContactPublicationStatus.TimelineConflict);
        MutationRefusal("external translation", f => Array<SpacecraftTranslationState>(f.Store,"_translations")[0] =
            Array<SpacecraftTranslationState>(f.Store,"_translations")[0] with { PositionRoot = new(1,2,3) }, PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("mass", f => Array<SpacecraftPhysicalProperties>(f.Store,"_properties")[0] = new(1001), PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("inertia", f => Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] =
            Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with { PrincipalInertia = new(1,2,3) }, PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("force", f => Array<SpacecraftTranslationState>(f.Store,"_translations")[0] =
            Array<SpacecraftTranslationState>(f.Store,"_translations")[0] with { ConstantForceRoot = Double3.Zero }, PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("torque", f => Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] =
            Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with { ConstantBodyTorque = Double3.UnitX }, PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("rotation model", f => Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] =
            Array<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with { Model = (RigidBodyRotationModel)2 }, PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("spacecraft definition", f => Array<SpacecraftDefinition>(f.Store,"_definitions")[0] =
            new(Craft,Root,Body,"Foreign definition"), PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("external clock", f => f.Clock.AdvanceTo(new(1)), PersistentContactPublicationStatus.StateConflict);
        MutationRefusal("external debt", f => f.Clock.AdvanceByHostDuration(new(1)), PersistentContactPublicationStatus.StateConflict);
        foreach (var tick in new[] { 1L, 16666L })
            MutationRefusal("event at/before target=" + tick, f => f.Clock.Timeline.Schedule(f.Clock.CurrentTime,
                new(new(1),new(tick),0,SimulationEventKind.Marker)), PersistentContactPublicationStatus.PendingEvent);

        var f = Create(); using var world = World(f, true, out var initial); var receipt = initial; Stage(f, world, ref receipt);
        var snapshot = Capture(f); f.Source.TryEndpoint(2, out var second);
        Check(world.Step(f.Engine,f.Configuration,receipt,second,out _) == LocalContactStatus.PublicationPending, "one pending endpoint");
        void Refuse(LocalContactWorld.Receipt r, PersistentContactPublicationStatus expected, string name)
        { PublicationStatus(Publish(f,world,r),expected,name); Check(Capture(f)==snapshot,name+" nonmutation"); }
        Refuse(default,PersistentContactPublicationStatus.InvalidReceipt,"default receipt");
        Refuse(new(world,1),PersistentContactPublicationStatus.InvalidReceipt,"fabricated current receipt");
        Refuse(initial,PersistentContactPublicationStatus.FrontierConflict,"older/wrong frontier");
        using var other = World(Create(),true,out var foreignReceipt);
        Refuse(foreignReceipt,PersistentContactPublicationStatus.InvalidReceipt,"foreign world/body/generation");
        object forged = receipt; Set(forged,"<Generation>k__BackingField",receipt.Generation+1);
        Refuse((LocalContactWorld.Receipt)forged,PersistentContactPublicationStatus.InvalidReceipt,"wrong captured generation");
        object wrongFrontier = receipt; Set(wrongFrontier,"Step",2L);
        Refuse((LocalContactWorld.Receipt)wrongFrontier,PersistentContactPublicationStatus.FrontierConflict,"wrong frontier");
        var foreign = Create(); var foreignBefore = Capture(foreign);
        PublicationStatus(foreign.Engine.PublishPersistentContact(world,f.Configuration,new(receipt,foreign.Engine.CaptureContinuationClock())),
            PersistentContactPublicationStatus.ForeignEngine,"foreign engine");
        Check(Capture(foreign)==foreignBefore && Capture(f)==snapshot,"both foreign/owner engines unchanged");
        PublicationStatus(f.Engine.PublishPersistentContact(world,foreign.Configuration,new(receipt,f.Engine.CaptureContinuationClock())),
            PersistentContactPublicationStatus.ConfigurationMismatch,"changed configuration reference");
        PublicationStatus(f.Engine.PublishPersistentContact(world,f.Configuration,new(receipt,default)),
            PersistentContactPublicationStatus.ClockConflict,"incorrect request clock");
        Check(Capture(f)==snapshot,"configuration and request refusals unchanged");
        var threadResult = default(PersistentContactPublicationResult); var clock = f.Engine.CaptureContinuationClock();
        var thread = new Thread(() => threadResult = f.Engine.PublishPersistentContact(world,f.Configuration,new(receipt,clock)));
        thread.Start(); thread.Join(); PublicationStatus(threadResult,PersistentContactPublicationStatus.WrongOwnerThread,"wrong owner");
        Check(f.Clock.PublicationPhase.TryEnter(f.Engine),"test enters existing phase");
        try { PublicationStatus(Publish(f,world,receipt),PersistentContactPublicationStatus.ReentrantPublication,"reentrant"); }
        finally { f.Clock.PublicationPhase.Exit(); }
        Check(Capture(f)==snapshot,"ownership refusal nonmutation");
        PublicationStatus(Publish(f,world,receipt),PersistentContactPublicationStatus.Published,"sound pending receipt retries after request refusal");
        snapshot=Capture(f);
        Refuse(receipt,PersistentContactPublicationStatus.FrontierConflict,"duplicate/already consumed receipt");
        Check(Operation(f,world,ref receipt,2,out _,out _),"next interval after ack");
        snapshot=Capture(f); Refuse(initial,PersistentContactPublicationStatus.FrontierConflict,"old receipt after later commit");
        Stage(f,world,ref receipt,3); f.Source.TryEndpoint(3,out var eventTarget);
        f.Clock.Timeline.Schedule(f.Clock.CurrentTime,new(new(17),eventTarget,7,SimulationEventKind.Marker));
        snapshot=Capture(f); Refuse(receipt,PersistentContactPublicationStatus.PendingEvent,"pending event and existing history contents unchanged");

        foreach (var scenario in new[] { "capacity", "debt", "negative debt", "revision overflow" })
        {
            var q=Create(capacity:scenario=="capacity"?0:1200,debt:scenario=="debt"?0:scenario=="negative debt"?-1:30_000_000,
                revision:scenario=="revision overflow"?new(ulong.MaxValue):default);
            using var w=World(q,true,out var r); Stage(q,w,ref r); var before=Capture(q);
            var expected=scenario switch { "capacity"=>PersistentContactPublicationStatus.HistoryCapacityFailure,
                "debt"=>PersistentContactPublicationStatus.InsufficientDebt,"negative debt"=>PersistentContactPublicationStatus.ArithmeticOverflow,
                _=>PersistentContactPublicationStatus.StateRevisionOverflow };
            PublicationStatus(Publish(q,w,r),expected,scenario); Check(Capture(q)==before,scenario+" nonmutation");
            Status(w.Read(q.Engine,q.Configuration,r,out _),"sound staged endpoint retained after refusal");
        }
        foreach (var invalid in new[] { false,true })
        {
            var q=Create(unsafeExport:invalid); using var w=World(q,true,out var r); var before=Capture(q);
            if (invalid)
            {
                q.Source.TryEndpoint(1,out var target);
                Check(w.Step(q.Engine,q.Configuration,r,target,out _) == LocalContactStatus.PrecisionEnvelopeExceeded,"post-step export fails closed");
                Check(w.Step(q.Engine,q.Configuration,r,target,out _) == LocalContactStatus.Invalidated,"unsafe continuation invalidated");
            }
            else { Stage(q,w,ref r); w.Dispose(); }
            PublicationStatus(Publish(q,w,r),PersistentContactPublicationStatus.WorldUnavailable,invalid?"invalidated world":"disposed world");
            Check(Capture(q)==before,"disposed/invalidated canonical nonmutation");
        }
        Console.WriteLine("PERSISTENT_REFUSAL_MATRIX PASS (ordinary refusals preserve canonical state/clock/debt/revisions/events/history)");
    }

    private static void AcknowledgementFailure()
    {
        var f=Create(); using var w=World(f,true,out var r); Stage(f,w,ref r); var before=Capture(f);
        Status(w.Read(f.Engine,f.Configuration,r,out var staged),"terminal staged endpoint");
        var result=f.Engine.PublishPersistentContactWithFailedAcknowledgementForTest(w,f.Configuration,new(r,f.Engine.CaptureContinuationClock()));
        PublicationStatus(result,PersistentContactPublicationStatus.CanonicalCommittedPrivateInvalidated,"terminal result is not refusal");
        var after=Capture(f);
        Check(result.CanonicalCommitted && after.Publications==1 && after.Revision.Value==before.Revision.Value+1 &&
            after.Clock.Time==staged.Motion.Time && after.Clock.Debt.Ticks==before.Clock.Debt.Ticks-16666 &&
            Bits(after.Linear.PositionRoot,staged.Motion.PositionRoot) && Bits(after.Angular.OrientationLocalToParent,staged.Motion.BodyToRoot) &&
            Bits(after.Linear.VelocityRoot,staged.Motion.VelocityRoot) && Bits(after.Angular.AngularVelocityBody,staged.Motion.AngularVelocityBody) &&
            after.Linear.Epoch==staged.Motion.Time && after.Angular.Epoch==staged.Motion.Time && after.Timeline==before.Timeline &&
            after.LastPublication.BeforeTranslation==before.Linear && after.LastPublication.BeforeRotation==before.Angular &&
            after.LastPublication.AfterTranslation==after.Linear && after.LastPublication.AfterRotation==after.Angular &&
            after.LastPublication.BeforeClock==before.Clock && after.LastPublication.AfterClock==after.Clock &&
            after.LastPublication.BeforeRevision==before.Revision && after.LastPublication.AfterRevision==after.Revision,
            "canonical commit/history/debt survive failed ack");
        PublicationStatus(Publish(f,w,r),PersistentContactPublicationStatus.WorldUnavailable,"terminal retry refused");
        f.Source.TryEndpoint(2,out var target);
        Check(w.Step(f.Engine,f.Configuration,r,target,out _) == LocalContactStatus.Invalidated && Capture(f)==after,
            "terminal private continuation invalid, no rollback/reconstruction");
        Console.WriteLine("PERSISTENT_ACK_FAILURE canonical=COMMITTED private=INVALIDATED retry=REFUSED");
    }

    private static void AllocationAndStorage()
    {
        var before=GC.GetAllocatedBytesForCurrentThread();
        var f=Create(); using var w=World(f,true,out var r);
        for(var step=1;step<=128;step++) Check(Operation(f,w,ref r,step,out _,out _),"allocation warmup");
        var managedUpperBound=GC.GetAllocatedBytesForCurrentThread()-before;
        var pool=w.PoolBytes; var historyPayload=Unsafe.SizeOf<ProcessedPersistentContact>()*(long)f.Engine.PersistentContactHistoryCapacity;
        Check(managedUpperBound+(long)pool<=8*1024*1024,"combined retained storage <=8 MiB");
        var success=true;
        using(var measurement=new OrdinaryAllocationMeasurement("persistent-contact-complete-path"))
        {
            for(var step=129;step<=1152;step++) success &= Operation(f,w,ref r,step,out _,out _);
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"persistent-contact complete path");
        }
        Check(success && w.PoolBytes==pool && f.Engine.ProcessedPersistentContactCount==1152,"all warmed operations complete without storage growth");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"PERSISTENT_STORAGE pool={pool} managed_upper_bound={managedUpperBound} history_payload_included={historyPayload} combined_upper_bound={managedUpperBound+(long)pool} limit=8388608");
    }

    internal static void Performance()
    {
        var samples=new double[1024]; var chronological=new double[1024];
        var cold=Stopwatch.GetTimestamp(); var f=Create(); var historyMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        cold=Stopwatch.GetTimestamp(); var w=World(f,false,out var r); var worldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        cold=Stopwatch.GetTimestamp(); Status(f.Engine.BeginPersistentContact(w,f.Configuration,r),"performance bind");
        var bindMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        using(w)
        {
            for(var step=1;step<=128;step++) Check(Operation(f,w,ref r,step,out _,out _),"performance warmup");
            for(var i=0;i<1024;i++)
            {
                var start=Stopwatch.GetTimestamp();
                var success=Operation(f,w,ref r,i+129,out _,out _);
                var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                Check(success,"measured complete operation"); samples[i]=elapsed; chronological[i]=elapsed;
            }
        }
        System.Array.Sort(samples);
        var median=(samples[511]+samples[512])/2; var p95=samples[972]; var p99=samples[1013]; var max=samples[^1];
        var worst=System.Array.IndexOf(chronological,max);
        var tails=chronological.Select((ms,index)=>new {index,ms}).OrderByDescending(x=>x.ms).Take(8).ToArray();
        Console.WriteLine("PERSISTENT_PERFORMANCE "+JsonSerializer.Serialize(new {median_ms=median,p95_ms=p95,p99_ms=p99,max_ms=max,
            worst_measured_index=worst,over_005ms=chronological.Count(x=>x>.05),over_010ms=chronological.Count(x=>x>.10),
            fixture_history_cold_ms=historyMs,world_cold_ms=worldMs,binding_cold_ms=bindMs,warm=128,measured=1024,tails}));
        Check(median<=.05 && p95<=.10 && p99<=.25 && max<=.50,"complete path performance ceiling");
    }
}
