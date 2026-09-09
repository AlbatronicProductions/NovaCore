using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static ContactGenerationFixture;

internal static class IsolatedContactResponseTests
{
    // Predeclared arithmetic comparison budget: gamma2048 covers independent quaternion/matrix,
    // effective mass, momentum subtraction and frame transport. Scales include pre-impact velocities.
    // Post-impact bars separately include the admitted squared-normal-length error (unit-normal formula).
    private const double U = 1.1102230246251565e-16;
    private static double Bar(double scale) => 2048 * U / (1 - 2048 * U) * Math.Max(1, scale);
    private static double Norm(Double3 v) => Math.Sqrt(v.LengthSquared);
    private static void Require(bool test, string message) { if (!test) throw new InvalidOperationException("Isolated response: " + message); }
    private static double _maxNormal, _maxLinear, _maxAngular;

    // Independent sphere authority. Radius is authored at the represented fixture feature radius before
    // observation so the strict zero-gap case is deliberate, not obtained by retries/tolerance adjustment.
    private sealed class Sphere(double radius, Double3? normal = null, PhysicalSurfaceQueryStatus status = PhysicalSurfaceQueryStatus.Ready,
        double gap = 0) : IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority { get; } = new(6, new(2, 5), 4, 16, new string('A', 64), new string('B', 64), 1, 1, 1);
        internal int Calls;
        public PhysicalSurfacePointResult Query(ulong bodyId, in Double3 d)
        {
            Calls++;
            var height = (radius - 16) - gap;
            return new(status, Authority, d, d * (16 + height), height, normal ?? d, 0);
        }
    }
    private sealed record Fixture(SimulationState State, SimulationClock Clock, SimulationTransactionEngine Engine,
        SpacecraftContactGeometry Geometry, CelestialSystemDefinition System, ContactBodyMotion Body, Sphere Query,
        IsolatedContactObservation Receipt, SpacecraftContactObservation[] Scratch);

    private static Fixture Create(Double3 offset = default, double tilt = 0, double speed = -3,
        double gap = 0, bool moving = false, bool spinning = false, double origin = 0,
        Double3? normal = null, PhysicalSurfaceQueryStatus status = PhysicalSurfaceQueryStatus.Ready)
    {
        var system = StaticSystem(new(origin, 0, 0), moving ? new(30000, -40, 9) : Double3.Zero);
        var body = Body(system);
        var q = (body.BodyFixedToRoot.Rotation * DoubleQuaternion.FromAxisAngle(new(.3, .7, .2), tilt)).Normalized();
        var r = q.Rotate(offset);
        var target = body.BodyFixedToRoot.LocalToParent(new(0, 0, 16));
        var position = target - r;
        var pointRoot = position + r;
        var pointBody = body.BodyFixedToRoot.ParentToLocal(pointRoot);
        var radius = Norm(pointBody);
        var query = new Sphere(radius, normal, status, gap);
        var rootNormal = body.BodyFixedToRoot.LocalDirectionToParent(pointBody / radius);
        var omega = spinning ? new Double3(.125, -.25, .375) : Double3.Zero;
        var support = body.VelocityRoot + Double3.Cross(body.AngularVelocityRoot, body.BodyFixedToRoot.LocalDirectionToParent(pointBody));
        var velocity = support + rootNormal * speed - Double3.Cross(q.Rotate(omega), r);
        var state = State(position, velocity, q, omega);
        Require(SpacecraftContactGeometry.TryCreate(Craft, 101, 1, [new(1, offset, ContactFeatureRole.LandingTip)], out var geometry), "geometry");
        var clock = new SimulationClock(default, new SimulationTimeline(4, 4));
        var engine = new SimulationTransactionEngine(clock, state, 4, contactImpulseHistoryCapacity: 4);
        var scratch = new SpacecraftContactObservation[1];
        var generated = IsolatedContactObservation.TryObserve(engine.State, engine.State.Revision, default, geometry, body,
            system, query, query.Authority, scratch, out var receipt);
        Require(generated.Succeeded == (status == PhysicalSurfaceQueryStatus.Ready && (normal is null || normal.Value.IsFinite)), "generation readiness");
        return new(state, clock, engine, geometry!, system, body, query, receipt, scratch);
    }
    private static IsolatedContactResponseResult Evaluate(Fixture f) => f.Receipt.Evaluate(f.Engine.State, f.Engine.State.Revision,
        default, f.Geometry, f.System, f.Query, f.Query.Authority, 1);
    private static SpacecraftMotion Motion(Fixture f)
    {
        Require(SpacecraftMotionEvaluator.TryEvaluate(f.Engine.State, Craft, default, out var m) == SpacecraftTranslationStatus.Success, "coherent motion");
        return m;
    }

    internal static void Run()
    {
        Numerical(); Refusals(); IdentityAndTransaction(); Allocations();
        Console.WriteLine($"Isolated policy maxima: postNormalMps={_maxNormal:R}; linearMomentumError={_maxLinear:R}; angularMomentumError={_maxAngular:R}; bars=gamma2048*statedScales+admittedNormalInputError; poses/replay=exact");
        Console.WriteLine("PASS Isolated contact response: strict canonical-zero-gap admission, qualified receipt, conservative velocity sign, independent impulse oracle, atomic transaction, refusal, zero-allocation contracts");
    }
    private static void Numerical()
    {
        foreach (var origin in new[] { 0d, 150_000_000_000d })
        foreach (var offset in new[] { Double3.Zero, new Double3(2, 0, 0), new Double3(1, -2, .5) })
        foreach (var tilt in new[] { 0d, .7d })
        foreach (var moving in new[] { false, true })
        foreach (var spinning in new[] { false, true })
        foreach (var normal in new Double3?[] { null, new Double3(.3, 0, 1).Normalized(), new Double3(0, 0, Math.Sqrt(1 + .5e-12)) })
        {
            // Nonradial outward normal represents a qualified plane witness through this same sampled point.
            var f = Create(offset, tilt, moving: moving, spinning: spinning, origin: origin, normal: normal);
            var before = Motion(f); var revision = f.Engine.State.Revision; var calls = f.Query.Calls;
            var result = Evaluate(f);
            Require(result.Succeeded, $"eligible isolated point: {result.Status}; gap={f.Receipt.Observation.RadialSignedGapMetres:R}");
            Require(result == Evaluate(f) && Motion(f) == before && f.Engine.State.Revision == revision && f.Clock.Timeline.PendingCount == 0 && f.Query.Calls == calls, "pure deterministic evaluation; no terrain requery or zero event");
            var n = f.Receipt.Observation.PhysicalNormalRoot;
            // Independent compliance evaluation: transform n's moment using explicit rotation matrix,
            // apply body inverse inertia, rotate back, and project point velocity response onto n.
            var r = Rotate(before.BodyToRoot, offset);
            var torque = Double3.Cross(r, n);
            var bodyTorque = Rotate(before.BodyToRoot.Conjugate(), torque);
            var inverse = new Double3(bodyTorque.X / 2, bodyTorque.Y / 3, bodyTorque.Z / 4);
            var compliance = Double3.Dot(n, n / 8 + Double3.Cross(Rotate(before.BodyToRoot, inverse), r));
            var expectedImpulse = n * (-Double3.Dot(n, f.Receipt.Observation.RelativeVelocityRoot) / compliance);
            var normalInputError = Math.Abs(n.LengthSquared - 1);
            Require(Norm(result.Intent.ImpulseRoot - expectedImpulse) <= Bar(Norm(expectedImpulse)) + normalInputError * Norm(expectedImpulse), "independent effective mass/impulse oracle");
            Require(f.Clock.Timeline.ScheduleContactImpulse(default, new(1), 0, result.Intent).Succeeded && f.Engine.ExecuteCanonicalPendingEvent().Committed, "canonical atomic commit");
            var after = Motion(f);
            Require(after.PositionRoot == before.PositionRoot && after.BodyToRoot == before.BodyToRoot, "exact event pose");
            var pointVelocity = after.VelocityRoot + Double3.Cross(Rotate(after.BodyToRoot, after.AngularVelocityBody), r);
            var post = Math.Abs(Double3.Dot(n, pointVelocity - f.Receipt.Observation.TerrainVelocityRoot));
            var linear = Norm((after.VelocityRoot - before.VelocityRoot) * 8 - result.Intent.ImpulseRoot);
            var deltaOmega = after.AngularVelocityBody - before.AngularVelocityBody;
            var angular = Norm(Rotate(after.BodyToRoot, new(deltaOmega.X * 2, deltaOmega.Y * 3, deltaOmega.Z * 4)) - Double3.Cross(r, result.Intent.ImpulseRoot));
            var scale = Norm(before.VelocityRoot) + Norm(f.Receipt.Observation.TerrainVelocityRoot) + Norm(expectedImpulse) + Norm(r);
            var normalInputBar = normalInputError * Math.Abs(result.NormalSpeed) / (8 * result.EffectiveInverseMass);
            Require(post <= Bar(scale) + normalInputBar && linear <= Bar(8 * scale) && angular <= Bar(8 * scale), "normal velocity/momentum bars");
            if (offset == Double3.Zero) Require(after.AngularVelocityBody == before.AngularVelocityBody, "centered zero torque");
            Require(f.Engine.ProcessedContactImpulseCount == 1 && f.Engine.State.Revision.Value == revision.Value + 1, "paired history/revision");
            _maxNormal = Math.Max(_maxNormal, post); _maxLinear = Math.Max(_maxLinear, linear); _maxAngular = Math.Max(_maxAngular, angular);
        }
    }
    private static Double3 Rotate(DoubleQuaternion q, Double3 v) => new(
        (1 - 2 * (q.Y * q.Y + q.Z * q.Z)) * v.X + 2 * (q.X * q.Y - q.Z * q.W) * v.Y + 2 * (q.X * q.Z + q.Y * q.W) * v.Z,
        2 * (q.X * q.Y + q.Z * q.W) * v.X + (1 - 2 * (q.X * q.X + q.Z * q.Z)) * v.Y + 2 * (q.Y * q.Z - q.X * q.W) * v.Z,
        2 * (q.X * q.Z - q.Y * q.W) * v.X + 2 * (q.Y * q.Z + q.X * q.W) * v.Y + (1 - 2 * (q.X * q.X + q.Y * q.Y)) * v.Z);

    private static void Refusals()
    {
        foreach (var (f, expected) in new[] {
            (Create(speed: 3), IsolatedContactResponseStatus.Separating),
            (Create(speed: 0, moving: true), IsolatedContactResponseStatus.Indeterminate),
            (Create(speed: -1e-13, moving: true), IsolatedContactResponseStatus.Indeterminate),
            (Create(gap: 1), IsolatedContactResponseStatus.NotContacting),
            (Create(gap: -1), IsolatedContactResponseStatus.RecoveryRequired),
            (Create(gap: 1e-12), IsolatedContactResponseStatus.NotContacting),
            (Create(gap: -1e-12), IsolatedContactResponseStatus.RecoveryRequired),
            (Create(normal: new(0, 0, 2)), IsolatedContactResponseStatus.InvalidNormal),
            (Create(normal: new(0, 0, -1)), IsolatedContactResponseStatus.InvalidNormal),
            (Create(normal: new(double.NaN, 0, 1)), IsolatedContactResponseStatus.ObservationUnavailable),
            (Create(status: PhysicalSurfaceQueryStatus.NormalUnqualified), IsolatedContactResponseStatus.ObservationUnavailable) })
        {
            var before = Motion(f); var result = Evaluate(f);
            Require(result.Status == expected, $"refusal {expected}: actual={result.Status}");
            Require(result.Intent == default && Motion(f) == before && f.Clock.Timeline.PendingCount == 0, "refusal is pure with no intent/event");
        }
        Require(!new SpacecraftPhysicalProperties(0).IsValid && !new SpacecraftPhysicalProperties(double.NaN).IsValid, "invalid mass cannot enter authoritative motion");
        Require(SpacecraftRigidBodyRotationState.TryCreate(Craft, default, DoubleQuaternion.Identity, Double3.Zero,
            new(0, 3, 4), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1, out _) != SpacecraftRigidBodyRotationEvaluationStatus.Success, "invalid inertia admission");
    }
    private static void IdentityAndTransaction()
    {
        var f = Create(new(2, 0, 0)); var state = f.Engine.State;
        IsolatedContactResponseResult Eval(SpacecraftContactGeometry? geometry = null, CelestialSystemDefinition? system = null,
            IPhysicalSurfacePointQuery? query = null, PhysicalSurfaceAuthorityIdentity? authority = null, ulong id = 1,
            SimulationInstant time = default, StateRevision? revision = null) => f.Receipt.Evaluate(state, revision ?? state.Revision, time,
                geometry ?? f.Geometry, system ?? f.System, query ?? f.Query, authority ?? f.Query.Authority, id);
        Require(Eval(revision: new(1)).Status == IsolatedContactResponseStatus.StaleState, "stale revision");
        Require(Eval(time: new(1)).Status == IsolatedContactResponseStatus.TimeMismatch, "wrong exact time");
        Require(Eval(geometry: Geometry(1)).Status == IsolatedContactResponseStatus.IdentityMismatch, "wrong geometry/feature");
        Require(Eval(geometry: Geometry(2)).Status == IsolatedContactResponseStatus.UnsupportedGeometry, "competing features");
        Require(Eval(system: StaticSystem(Double3.Zero, Double3.Zero)).Status == IsolatedContactResponseStatus.IdentityMismatch, "different system");
        Require(Eval(authority: f.Query.Authority with { CompositionIdentity = 2 }).Status == IsolatedContactResponseStatus.AuthorityMismatch, "changed terrain authority");
        Require(Eval(authority: f.Query.Authority with { BodyId = 7 }).Status == IsolatedContactResponseStatus.AuthorityMismatch, "wrong support body");
        Require(Eval(query: new Sphere(16)).Status == IsolatedContactResponseStatus.AuthorityMismatch, "different authority instance even with identical bytes");
        Require(Eval(id: 0).Status == IsolatedContactResponseStatus.IdentityMismatch, "invalid provenance identity");
        Require(!IsolatedContactObservation.TryObserve(state, state.Revision, default, Geometry(2), f.Body, f.System,
            f.Query, f.Query.Authority, new SpacecraftContactObservation[2], out _).Succeeded, "cannot select subset of multi-feature definition");
        Require(!IsolatedContactObservation.TryObserve(state, state.Revision, default, f.Geometry, Body(f.System, root: 2), f.System,
            f.Query, f.Query.Authority, f.Scratch, out _).Succeeded, "root mismatch at receipt issuance");
        Require(!IsolatedContactObservation.TryObserve(state, state.Revision, new(1), f.Geometry, f.Body, f.System,
            f.Query, f.Query.Authority, f.Scratch, out _).Succeeded, "time mismatch at receipt issuance");
        // Mutating caller scratch cannot alter the opaque issued receipt.
        f.Scratch[0] = default;
        var intent = Eval().Intent;
        Require(intent.IsValid, "receipt independent of caller scratch");
        var before = Motion(f);
        Require(f.Clock.Timeline.ScheduleContactImpulse(default, new(1), 0, intent).Succeeded &&
            f.Clock.Timeline.ScheduleContactImpulse(default, new(2), 0, intent).Succeeded, "duplicate intents remain canonical events");
        Require(f.Engine.ExecuteCanonicalPendingEvent().Committed, "first intent commits");
        var after = Motion(f);
        Require(!f.Engine.ExecuteCanonicalPendingEvent().Committed && Motion(f) == after && f.Engine.ProcessedContactImpulseCount == 1, "stale duplicate cannot partially mutate");
        Require(Evaluate(f).Status == IsolatedContactResponseStatus.StaleState && before != after, "receipt invalidated by commit");
        Require(f.Clock.Timeline.ContactPayloadCount == 1 && f.Clock.Timeline.ValidateContactPayloadInvariants(), "rejected payload retained, no leak");
        Require(f.Clock.Timeline.Cancel(new(2)).Succeeded && f.Clock.Timeline.ContactPayloadCount == 0, "explicit cancellation retires rejected payload");
        var fresh = Create(new(2, 0, 0)); var replay = Evaluate(fresh);
        Require(replay.Intent == intent, "deterministic fresh-state replay intent");
    }

    private static void Allocations()
    {
        var f = Create();
        for (var i = 0; i < 256; i++) _ = Evaluate(f);
        Require(GC.TryStartNoGCRegion(1 << 20, disallowFullBlockingGC: true), "banked GC accounting boundary");
        long bytes; bool all = true;
        try
        {
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++) all &= Evaluate(f).Succeeded;
            bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        }
        finally { GC.EndNoGCRegion(); }
        Require(all && bytes == 0, $"warmed policy zero allocation: bytes={bytes}");
        Console.WriteLine($"Isolated policy allocations: 10000 evaluations; bytes={bytes}; Query calls={f.Query.Calls}");
    }

    internal static void Performance()
    {
        // Declared bounded plan: 4 warm batches + 21 samples x 128 independent states, per mode.
        // Setup outside measurement; transaction mode excludes policy; full path uses analytical CPU sphere,
        // already-current body kinematics and freshly generated receipt. Percentiles are batch averages.
        const int count = 128;
        for (var mode = 0; mode < 3; mode++)
        {
            var times = new double[21]; var bytes = new long[21];
            for (var batch = -4; batch < 21; batch++)
            {
                var fixtures = Enumerable.Range(0, count).Select(_ => Create(new(2, 0, 0))).ToArray();
                var intents = fixtures.Select(f => Evaluate(f).Intent).ToArray();
                var start = Stopwatch.GetTimestamp();
                Execute(mode, fixtures, intents);
                var elapsed = Stopwatch.GetTimestamp() - start;
                if (batch >= 0) times[batch] = elapsed * (1e9 / Stopwatch.Frequency) / count;
            }
            for (var batch = 0; batch < 21; batch++)
            {
                var fixtures = Enumerable.Range(0, count).Select(_ => Create(new(2, 0, 0))).ToArray();
                var intents = fixtures.Select(f => Evaluate(f).Intent).ToArray();
                Require(GC.TryStartNoGCRegion(1 << 20, disallowFullBlockingGC: true), "allocation accounting boundary");
                try { var start = GC.GetAllocatedBytesForCurrentThread(); Execute(mode, fixtures, intents); bytes[batch] = GC.GetAllocatedBytesForCurrentThread() - start; }
                finally { GC.EndNoGCRegion(); }
            }
            Array.Sort(times); Array.Sort(bytes);
            Require(bytes[^1] == 0, "measured workload zero allocations");
            Console.WriteLine($"Isolated response performance mode={mode} (0=policy,1=schedule+transaction,2=analytical observation+policy+transaction): median={times[10]:F1}ns P95={times[19]:F1}ns P99={times[20]:F1}ns; maxBytesPer128={bytes[^1]}; 21 batch-average samples");
        }
    }
    private static void Execute(int mode, Fixture[] fixtures, SpacecraftContactImpulseIntent[] intents)
    {
        for (var i = 0; i < fixtures.Length; i++)
        {
            var f = fixtures[i]; var intent = intents[i];
            if (mode == 0) { Require(Evaluate(f).Succeeded, "policy benchmark"); continue; }
            if (mode == 2)
            {
                Require(IsolatedContactObservation.TryObserve(f.Engine.State, f.Engine.State.Revision, default, f.Geometry,
                    f.Body, f.System, f.Query, f.Query.Authority, f.Scratch, out var receipt).Succeeded, "complete observation");
                var result = receipt.Evaluate(f.Engine.State, f.Engine.State.Revision, default, f.Geometry, f.System, f.Query, f.Query.Authority, 1);
                Require(result.Succeeded, "complete policy"); intent = result.Intent;
            }
            Require(f.Clock.Timeline.ScheduleContactImpulse(default, new(1), 0, intent).Succeeded && f.Engine.ExecuteCanonicalPendingEvent().Committed, "complete transaction benchmark");
        }
    }
}
