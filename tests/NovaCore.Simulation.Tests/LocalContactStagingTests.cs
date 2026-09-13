using System.Diagnostics;
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

internal static class LocalContactStagingTests
{
    private static readonly SpacecraftId Craft = new(901);
    private static readonly ReferenceFrameId Root = new(1), Body = new(902);
    private static readonly Double3 Dimensions = new(2, 1, 1);
    private const double Mass = 1000;
    // Project Control: 20 mm = 2% of this fixture's minimum 1 m dimension.
    // This is not a terrain, spacecraft-collider or gameplay tolerance.
    private const double QualificationPeakPenetration = .020;

    private sealed record Fixture(SimulationState State, SimulationClock Clock, SimulationTransactionEngine Engine,
        LocalContactConfiguration Configuration, SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular);

    private static void Check(bool condition, string contract)
    { if (!condition) throw new InvalidOperationException("Local contact staging: " + contract); }
    private static void Status(LocalContactStatus actual, LocalContactStatus expected, string contract)
    { if (actual != expected) throw new InvalidOperationException($"Local contact staging: {contract}: expected {expected}, actual {actual}"); }
    private static double Length(Double3 v) => Math.Sqrt(v.LengthSquared);
    private static LocalContactConfiguration Configuration(Double3? origin = null, Double3? velocity = null,
        DoubleQuaternion? rotation = null, long revision = 1)
    {
        Status(LocalContactConfiguration.TryCreate(revision, Root, origin ?? Double3.Zero, velocity ?? Double3.Zero,
            rotation ?? DoubleQuaternion.Identity, Dimensions, 64, out var configuration), LocalContactStatus.Success, "configuration");
        return configuration!;
    }
    private static Fixture Create(double height = 2, Double3? velocity = null, Double3? acceleration = null,
        DoubleQuaternion? orientation = null, Double3? omega = null, Double3? torque = null,
        LocalContactConfiguration? configuration = null, long start = 0, PrincipalMomentsOfInertia? inertia = null)
    {
        var c = configuration ?? Configuration(); var time = new SimulationInstant(start);
        var linear = new SpacecraftTranslationState(Craft, Root, time,
            c.OriginRoot + c.LocalToRoot.Rotate(new(0, height, 0)),
            c.OriginVelocityRoot + c.LocalToRoot.Rotate(velocity ?? Double3.Zero),
            c.LocalToRoot.Rotate((acceleration ?? new(0, -9.81, 0)) * Mass));
        var angular = new SpacecraftRigidBodyRotationState(Craft, time,
            c.LocalToRoot * (orientation ?? DoubleQuaternion.Identity), omega ?? Double3.Zero,
            inertia ?? c.BoxInertia(Mass), torque ?? Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        var graph = new ReferenceFrameGraphBuilder();
        graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "Qualification inertial root"));
        graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "Qualification body"));
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "Qualification-only box")],
            [angular], [new(Mass)], [linear], graph.Build(), out var store, out _), "canonical source fixture");
        var state = new SimulationState(spacecraft: store);
        var clock = new SimulationClock(time, new SimulationTimeline(4));
        return new(state, clock, new(clock, state, 4), c, linear, angular);
    }
    private static LocalContactSource Capture(Fixture f, long seconds = 30)
    {
        Status(LocalContactSource.Capture(f.Engine, Craft, f.Configuration,
            f.Clock.CurrentTime + SimulationDuration.FromWholeSeconds(seconds), out var source), LocalContactStatus.Success, "source capture");
        return source!;
    }
    private static LocalContactWorld World(Fixture f, LocalContactSource source, out LocalContactWorld.Receipt receipt)
    {
        Status(LocalContactWorld.TryCreate(f.Engine, source, f.Configuration, out var world, out receipt), LocalContactStatus.Success, "world bind");
        return world!;
    }
    private static LocalContactStatus Advance(Fixture f, LocalContactSource source, LocalContactWorld world,
        ref LocalContactWorld.Receipt receipt, long step, out LocalContactWorld.Export export)
    {
        export = default;
        if (!source.TryEndpoint(step, out var target)) return LocalContactStatus.InvalidInterval;
        var status = world.Step(f.Engine, f.Configuration, receipt, target, out var next);
        if (status != LocalContactStatus.Success) return status;
        receipt = next;
        return world.Read(f.Engine, f.Configuration, receipt, out export);
    }
    private static void Unchanged(Fixture f, StateRevision revision, TimelineRevision timeline, SimulationInstant time,
        int pending, int processed)
    {
        var view = f.Engine.State;
        Check(view.Revision == revision, "canonical StateRevision unchanged");
        Check(f.Clock.Timeline.Revision == timeline && f.Clock.CurrentTime == time, "canonical timeline/clock unchanged");
        Check(f.Clock.Timeline.PendingCount == pending && f.Engine.ProcessedCount == processed, "pending events/history unchanged");
        Check(view.Spacecraft.TryGetTranslation(Craft, out var linear, out var properties) && linear == f.Linear && properties.MassKilograms == Mass,
            "canonical FP64 linear segment/mass unchanged");
        Check(view.Spacecraft.TryGetRigidBody(Craft, out var angular) && angular == f.Angular, "canonical angular segment unchanged");
        Check(f.Engine.ProcessedSpacecraftForceCount == 0 && f.Engine.ProcessedRigidBodyTorqueCount == 0 &&
            f.Engine.ProcessedSpacecraftAttitudeCount == 0, "domain histories unchanged");
    }

    internal static void Run()
    {
        Admission(); ContactAndContinuation(); FreeFlightAndTransport(); AllocationAndStorage();
        Console.WriteLine("PASS Local contact staging: authority, exact time, persistent finite-body contact, paired export, negatives, allocation/storage");
    }

    private static void Admission()
    {
        var f = Create(); var source = Capture(f); using var world = World(f, source, out var receipt);
        source.TryEndpoint(1, out var target);
        Status(world.Read(f.Engine, f.Configuration, receipt, out var initial), LocalContactStatus.Success, "initial read");
        Check(initial.Motion == source.Motion, "initial receipt preserves original double source without float round trip");
        Status(world.Step(f.Engine, f.Configuration, receipt, source.Motion.Time, out _), LocalContactStatus.InvalidInterval, "nonpositive step");
        Status(world.Step(f.Engine, f.Configuration, receipt, new(target.Ticks + 1), out _), LocalContactStatus.InvalidInterval, "noncanonical step mapping");
        Status(world.Step(f.Engine, Configuration(), receipt, target, out _), LocalContactStatus.ConfigurationMismatch, "reused numeric configuration identity");
        Status(world.Step(f.Engine, Configuration(revision: 2), receipt, target, out _), LocalContactStatus.ConfigurationMismatch, "changed configuration");
        var foreign = Create();
        Status(world.Step(foreign.Engine, f.Configuration, receipt, target, out _), LocalContactStatus.ForeignEngine, "foreign identical engine");
        using var other = World(f, source, out var otherReceipt);
        Check(other.Generation != world.Generation, "restored source creates fresh generation");
        Status(world.Step(f.Engine, f.Configuration, otherReceipt, target, out _), LocalContactStatus.GenerationMismatch, "other-world receipt");
        Status(world.Step(f.Engine, f.Configuration, default, target, out _), LocalContactStatus.GenerationMismatch, "missing receipt");
        var wrongThread = LocalContactStatus.Success; var wrongCapture = LocalContactStatus.Success; var wrongDispose = LocalContactStatus.Success;
        var thread = new Thread(() =>
        {
            wrongThread = world.Step(f.Engine, f.Configuration, receipt, target, out _);
            wrongCapture = LocalContactSource.Capture(f.Engine, Craft, f.Configuration, new(1_000_000), out _);
            wrongDispose = world.TryDispose();
        });
        thread.Start(); thread.Join();
        Status(wrongThread, LocalContactStatus.WrongThread, "step ownership");
        Status(wrongCapture, LocalContactStatus.WrongThread, "capture ownership");
        Status(wrongDispose, LocalContactStatus.WrongThread, "disposal ownership");
        var old = receipt;
        Status(Advance(f, source, world, ref receipt, 1, out _), LocalContactStatus.Success, "valid after pre-step rejections");
        source.TryEndpoint(2, out var second);
        Status(world.Step(f.Engine, f.Configuration, old, second, out _), LocalContactStatus.FrontierMismatch, "stale acknowledged frontier");
        Status(world.Read(f.Engine, f.Configuration, old, out _), LocalContactStatus.FrontierMismatch, "stale export receipt");
        f.State.CommitMarkerValue(1);
        Status(world.Step(f.Engine, f.Configuration, receipt, second, out _), LocalContactStatus.ChangedAuthority, "changed StateRevision");

        var changed = Create(); var changedSource = Capture(changed); using var changedWorld = World(changed, changedSource, out var changedReceipt);
        changed.Clock.Timeline.Schedule(changed.Clock.CurrentTime, new(new(1), new(40_000_000), 0, SimulationEventKind.Marker));
        Status(changedWorld.Step(changed.Engine, changed.Configuration, changedReceipt, target, out _), LocalContactStatus.TimelineConflict, "changed TimelineRevision even beyond interval");
        var moved = Create(); var movedSource = Capture(moved); using var movedWorld = World(moved, movedSource, out var movedReceipt);
        moved.Clock.AdvanceTo(new(1));
        Status(movedWorld.Step(moved.Engine, moved.Configuration, movedReceipt, target, out _), LocalContactStatus.ChangedAuthority, "clock advanced without revision");
        var pending = Create();
        pending.Clock.Timeline.Schedule(pending.Clock.CurrentTime, new(new(1), target, 0, SimulationEventKind.Marker));
        Status(LocalContactSource.Capture(pending.Engine, Craft, pending.Configuration, target, out _), LocalContactStatus.PendingEvent, "event exactly at target");
        Check(pending.Clock.Timeline.PendingCount == 1 && pending.Engine.ProcessedCount == 0, "admission does not consume pending event");
        Status(LocalContactSource.Capture(pending.Engine, Craft, pending.Configuration, pending.Clock.CurrentTime, out _), LocalContactStatus.InvalidInterval, "zero source interval");
        Status(LocalContactSource.Capture(foreign.Engine, Craft, null, new(1_000_000), out _), LocalContactStatus.InvalidConfiguration, "missing physical surface configuration");
        var torque = Create(torque: Double3.UnitX);
        Status(LocalContactSource.Capture(torque.Engine, Craft, torque.Configuration, new(1_000_000), out _), LocalContactStatus.UnsupportedForceTorqueState, "nonzero source torque");
        var mismatch = Create(inertia: new(1, 1, 1));
        Status(LocalContactSource.Capture(mismatch.Engine, Craft, mismatch.Configuration, new(1_000_000), out _), LocalContactStatus.InvalidSource, "inertia differs from authored box");
        foreach (var mass in new[] {0d, -1d, double.NaN, double.PositiveInfinity})
            Check(!new SpacecraftPhysicalProperties(mass).IsValid, "invalid mass refused by source authority");
        Check(!new PrincipalMomentsOfInertia(0, 1, 1).IsStrictlyPositive && !new PrincipalMomentsOfInertia(double.NaN, 1, 1).IsFinite, "invalid inertia authority");
        foreach (var dims in new[] {Double3.Zero, new Double3(1e20, 1e20, 1e20), new Double3(1e-30, 1e-30, 1e-30)})
            Status(LocalContactConfiguration.TryCreate(1, Root, Double3.Zero, Double3.Zero, DoubleQuaternion.Identity, dims,
                dims.X == 0 ? 64 : dims.X * 64, out _), LocalContactStatus.InvalidConfiguration, "float geometry overflow/underflow");
        Status(LocalContactConfiguration.TryCreate(1, Root, new(1e30, 0, 0), Double3.Zero, DoubleQuaternion.Identity, Dimensions, 64, out _),
            LocalContactStatus.InvalidConfiguration, "canonical root cannot resolve contact tolerance");
        var outside = Create(height: 1024); var outsideSource = Capture(outside);
        Status(LocalContactWorld.TryCreate(outside.Engine, outsideSource, outside.Configuration, out _, out _), LocalContactStatus.PrecisionEnvelopeExceeded, "initial envelope");
        var excessive = Create(velocity: new(100, 0, 0)); var excessiveSource = Capture(excessive);
        Status(LocalContactWorld.TryCreate(excessive.Engine, excessiveSource, excessive.Configuration, out _, out _), LocalContactStatus.PrecisionEnvelopeExceeded, "swept-speed envelope");
        var late = Create(start: long.MaxValue - 50_000);
        Status(LocalContactSource.Capture(late.Engine, Craft, late.Configuration, new(long.MaxValue), out var lateSource), LocalContactStatus.Success, "late source");
        Check(lateSource!.TryEndpoint(3, out var last) && last.Ticks == long.MaxValue && !lateSource.TryEndpoint(4, out _) &&
            !lateSource.TryEndpoint(long.MaxValue, out _) && !lateSource.TryEndpoint(-1, out _), "checked exact endpoint overflow");
        var epoch = Create(start: -123); var epochSource = Capture(epoch);
        var previous = epochSource.Motion.Time;
        for (var n = 1; n <= 180; n++)
        {
            Check(epochSource.TryEndpoint(n, out var e) && e.Ticks == -123 + n * 1_000_000L / 60, "integral endpoint oracle");
            Check(e.Ticks - previous.Ticks is 16666 or 16667, "exact interval schedule"); previous = e;
        }

        // Real post-step refusal, no artificial solver failure hook: admitted source acceleration drives
        // a valid initially stationary body beyond the qualified speed envelope in its first step.
        var failed = Create(height: 5, acceleration: new(10000, 0, 0)); var failedSource = Capture(failed);
        using var failedWorld = World(failed, failedSource, out var failedReceipt);
        Status(Advance(failed, failedSource, failedWorld, ref failedReceipt, 1, out var refused), LocalContactStatus.PrecisionEnvelopeExceeded, "post-step export rejection");
        Check(refused == default, "no partial paired export on failure");
        Status(failedWorld.Step(failed.Engine, failed.Configuration, failedReceipt, target, out _), LocalContactStatus.Invalidated, "advanced unsafe world cannot retry");
        Status(failedWorld.Read(failed.Engine, failed.Configuration, failedReceipt, out _), LocalContactStatus.Invalidated, "invalidated world cannot export");
        Unchanged(failed, default, default, default, 0, 0);
        using var retired = World(foreign, Capture(foreign), out var retiredReceipt);
        Status(retired.TryDispose(), LocalContactStatus.Success, "owned disposal");
        Status(retired.Step(foreign.Engine, foreign.Configuration, retiredReceipt, target, out _), LocalContactStatus.Disposed, "disposed continuation");
        using var fresh = World(foreign, Capture(foreign), out _);
        Status(fresh.Step(foreign.Engine, foreign.Configuration, retiredReceipt, target, out _), LocalContactStatus.GenerationMismatch, "prior-generation receipt after rebind");
        Console.WriteLine("STAGING_ADMISSION PASS: exact schedule; authority/configuration/event/world/frontier/thread/disposal/torque/geometry refusals; post-step invalidation");
    }

    private static void ContactAndContinuation()
    {
        foreach (var tilted in new[] { false, true })
        {
            var q = tilted ? DoubleQuaternion.FromAxisAngle(Double3.UnitZ, .25) : DoubleQuaternion.Identity;
            var f = Create(orientation: q); var source = Capture(f);
            using var world = World(f, source, out var receipt);
            using var replay = World(f, source, out var replayReceipt);
            var maxPoints = 0; var maxDepth = 0f; var maxOmega = 0d; var restMin = double.MaxValue; var restMax = double.MinValue;
            var supportSteps = 0; var geometricPeak = 0d; LocalContactWorld.Export output = default;
            for (var step = 1; step <= 1200; step++)
            {
                Status(Advance(f, source, world, ref receipt, step, out output), LocalContactStatus.Success, "contact continuation");
                Status(Advance(f, source, replay, ref replayReceipt, step, out var repeated), LocalContactStatus.Success, "repeat continuation");
                Check(output.Motion == repeated.Motion, "same-build same-machine paired repeatability");
                maxPoints = Math.Max(maxPoints, output.ContactPoints); maxDepth = Math.Max(maxDepth, output.MaximumDepth);
                maxOmega = Math.Max(maxOmega, Length(output.Motion.AngularVelocityBody));
                // Check endpoint geometry independently of BEPU's pre-solve manifold depth.
                // All eight authored box corners are measured against the unchanged local y=0 plane.
                for (var x = -1; x <= 1; x += 2)
                    for (var y = -1; y <= 1; y += 2)
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var corner = output.Motion.PositionRoot + output.Motion.BodyToRoot.Rotate(
                                new(x * Dimensions.X * .5, y * Dimensions.Y * .5, z * Dimensions.Z * .5));
                            geometricPeak = Math.Max(geometricPeak, -corner.Y);
                        }
                if (step > 600)
                {
                    restMin = Math.Min(restMin, output.Motion.PositionRoot.Y); restMax = Math.Max(restMax, output.Motion.PositionRoot.Y);
                    if (output.ConstraintCount > 0 && output.ContactPoints >= 2) supportSteps++;
                }
            }
            Console.WriteLine($"STAGING_CONTACT tilted={tilted} steps=1200 seconds=20 max_points={maxPoints} max_depth_m={maxDepth:R} geometric_peak_m={geometricPeak:R} max_omega_rad_s={maxOmega:R} rest_y_m={output.Motion.PositionRoot.Y:R} rest_drift_m={restMax-restMin:R} speed_m_s={Length(output.Motion.VelocityRoot):R} support_steps={supportSteps}");
            Check(maxPoints >= 2 && supportSteps == 600, "finite-body multipoint persistent support rather than point bounce");
            Check(maxDepth <= QualificationPeakPenetration, "manifold peak penetration <=20mm qualification bar");
            Check(geometricPeak <= QualificationPeakPenetration, "geometric endpoint peak penetration <=20mm qualification bar");
            Check(Math.Abs(output.Motion.PositionRoot.Y - .5) <= 2 * f.Configuration.ContactTolerance, "resting height within two contact tolerances");
            Check(restMax - restMin <= f.Configuration.ContactTolerance, "rest drift within contact tolerance over 10 seconds");
            Check(Length(output.Motion.VelocityRoot) <= f.Configuration.ContactTolerance / (1d / 60), "rest velocity below one tolerance per step");
            if (tilted) Check(maxOmega > .1, "off-centre contact creates angular response");
            Unchanged(f, default, default, default, 0, 0);
        }
    }

    private static void FreeFlightAndTransport()
    {
        var frame = DoubleQuaternion.FromAxisAngle(new(1, 2, -1), .7);
        var c = Configuration(new(1e11, -1e11, 1e11), new(30000, -10000, 8000), frame);
        var f = Create(height: 10.123456789, velocity: new(.125, .25, -.5), acceleration: Double3.Zero,
            omega: new(.1, 0, 0), configuration: c);
        var source = Capture(f); using var world = World(f, source, out var receipt);
        Check(world.ImportPositionError <= c.ContactTolerance / 8 && world.ImportVelocityError <= c.ContactTolerance / 8, "local float conversion error");
        LocalContactWorld.Export output = default;
        for (var step = 1; step <= 60; step++) Status(Advance(f, source, world, ref receipt, step, out output), LocalContactStatus.Success, "free-flight transport");
        var positionError = Length(output.Motion.PositionRoot - (f.Linear.PositionRoot + f.Linear.VelocityRoot));
        var velocityError = Length(output.Motion.VelocityRoot - f.Linear.VelocityRoot);
        Check(positionError < c.ContactTolerance && velocityError < c.ContactTolerance / 8, "FP64 free-flight reconstruction including moving origin");
        Check(Length(output.Motion.AngularVelocityBody - f.Angular.AngularVelocityBody) < 1e-5, "angular body/root/local mapping");
        Check(output.ContactPoints == 0 && output.ConstraintCount == 0, "noncontact control");
        Unchanged(f, default, default, default, 0, 0);

        var forced = Create(height: 20, acceleration: new(0, -1, 0)); var forceSource = Capture(forced); using var forcedWorld = World(forced, forceSource, out var forceReceipt);
        for (var step = 1; step <= 60; step++) Status(Advance(forced, forceSource, forcedWorld, ref forceReceipt, step, out output), LocalContactStatus.Success, "source force control");
        Check(Math.Abs(output.Motion.VelocityRoot.Y + 1) < 2e-6, "force integrated once, no hidden gravity");
        // Semi-implicit integration has O(dt) position error; one half a 60 Hz step bounds it here.
        Check(Math.Abs(output.Motion.PositionRoot.Y - 19.5) <= .009, "bounded numerical force integration");
        Console.WriteLine($"STAGING_PRECISION bound_m={c.MaximumCoordinate:R} tolerance_m={c.ContactTolerance:R} margin_m={c.Margin:R} local_import_position_error_m={world.ImportPositionError:R} local_import_velocity_error_m_s={world.ImportVelocityError:R} root_reconstruction_error_m={positionError:R} root_velocity_error_m_s={velocityError:R} forced_velocity_y={output.Motion.VelocityRoot.Y:R}");
    }

    private static void AllocationAndStorage()
    {
        // Conservative owned-managed bound includes fixture/source/config construction and all warmup
        // allocation. Native pool bytes cover body/shape/static/solver storage without double counting.
        var before = GC.GetAllocatedBytesForCurrentThread();
        var f = Create(); var source = Capture(f); using var world = World(f, source, out var receipt);
        for (var step = 1; step <= 128; step++) Status(Advance(f, source, world, ref receipt, step, out _), LocalContactStatus.Success, "allocation warmup");
        var managedUpperBound = GC.GetAllocatedBytesForCurrentThread() - before;
        var poolBefore = world.PoolBytes; var total = (ulong)managedUpperBound + poolBefore;
        Check(total <= 8 * 1024 * 1024, "retained storage upper bound <=8MiB");
        var success = true;
        using var measurement = new OrdinaryAllocationMeasurement("private-bepu-full-step-export");
        for (var step = 129; step <= 1152; step++) success &= Advance(f, source, world, ref receipt, step, out _) == LocalContactStatus.Success;
        var bytes = measurement.Complete();
        OrdinaryAllocationMeasurement.RequireZero(bytes, "private-bepu-full-step-export");
        Check(success, "all measured private steps and exports complete");
        Check(world.PoolBytes == poolBefore, "retained native storage stable after warmup");
        Unchanged(f, default, default, default, 0, 0);
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"STAGING_STORAGE pool_bytes={poolBefore} managed_allocation_upper_bound_bytes={managedUpperBound} retained_upper_bound_bytes={total} bound_bytes=8388608 measured_steps=1024 allocation_bytes={bytes}");
    }

    internal static void Performance()
    {
        // Three fresh process invocations are orchestrated externally. No no-GC region or runtime changes here.
        var samples = new double[1024]; var raw = new long[1024]; var f = Create(); var source = Capture(f);
        var cold = Stopwatch.GetTimestamp(); using var world = World(f, source, out var receipt);
        var creationMs = Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        for (var step = 1; step <= 128; step++) Status(Advance(f, source, world, ref receipt, step, out _), LocalContactStatus.Success, "performance warmup");
        var completed = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp();
            var status = Advance(f, source, world, ref receipt, i + 129, out _);
            raw[i] = Stopwatch.GetTimestamp() - start;
            if (status != LocalContactStatus.Success) throw new InvalidOperationException("Timed private step/export failed: " + status);
            completed++;
        }
        for (var i = 0; i < samples.Length; i++) samples[i] = raw[i] * 1000d / Stopwatch.Frequency;
        var chronological = (double[])samples.Clone(); Array.Sort(samples);
        var median = (samples[511] + samples[512]) / 2; var p95 = samples[972]; var p99 = samples[1013]; var maximum = samples[^1];
        var dispose = Stopwatch.GetTimestamp(); world.Dispose(); var disposalMs = Stopwatch.GetElapsedTime(dispose).TotalMilliseconds;
        var passed = median <= .05 && p95 <= .10 && p99 <= .25 && maximum <= .50;
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "STAGING_PERFORMANCE", result = passed ? "PASS" : "FAIL", process = Environment.ProcessId,
            warmSteps = 128, completed, medianMs = median, p95Ms = p95, p99Ms = p99, maxMs = maximum, creationMs, disposalMs, samplesMs = chronological }));
        Check(passed, "full step/export absolute performance gates");
    }
}
