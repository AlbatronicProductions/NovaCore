using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using static ContactGenerationFixture;

internal static class ContactGenerationTests
{
    private static void Check(bool value, string message) => ContactGenerationFixture.Check(value, message);
    internal static void Run()
    {
        Identity(); GeometryAndKinematics(); Failures(); RevisionAdmission(); BodyAdapter(); Allocations();
        Console.WriteLine("PASS Contact generation: immutable identity, analytical point geometry, rotating frames, readiness, replay, no mutation, allocations");
    }
    private static void Identity()
    {
        SpacecraftContactFeature[] source = [new(2, new(-0d, 2, 1), ContactFeatureRole.LandingTip), new(1, new(1, 0, 1), ContactFeatureRole.SupportHardpoint)];
        Check(SpacecraftContactGeometry.TryCreate(Craft, 1, 1, source, out var a), "feature admission");
        Check(SpacecraftContactGeometry.TryCreate(Craft, 1, 1, [source[1], source[0] with { OffsetFromComMetres = new(0, 2, 1) }], out var b), "reordered admission");
        Check(a!.Identity == b!.Identity && a.GetFeature(0).Id == 1, "canonical order and zero identity");
        source[1] = source[1] with { OffsetFromComMetres = new(99, 0, 1) };
        Check(a.GetFeature(0).OffsetFromComMetres.X == 1, "caller storage mutation cannot affect geometry");
        Check(SpacecraftContactGeometry.TryCreate(Craft, 1, 1, source, out var changed) && changed!.Identity != a.Identity, "content distinguishes reused version");
        Check(!SpacecraftContactGeometry.TryCreate(Craft, 1, 1, [source[0], source[0]], out _), "duplicate IDs");
        Check(!SpacecraftContactGeometry.TryCreate(Craft, 1, 1, [source[0] with { OffsetFromComMetres = new(double.NaN, 0, 0) }], out _), "nonfinite offset");
        Check(!SpacecraftContactGeometry.TryCreate(default, 1, 1, source, out _) && !SpacecraftContactGeometry.TryCreate(Craft, 0, 1, source, out _) &&
            !SpacecraftContactGeometry.TryCreate(Craft, 1, 0, source, out _) && !SpacecraftContactGeometry.TryCreate(Craft, 1, 1, [], out _), "invalid identity/empty geometry");
    }
    private static void GeometryAndKinematics()
    {
        // Predeclared bars: eight root-coordinate ulps plus 128 FP64 rounding units of local scale.
        // Velocity adds omega*positionBar and 128 rounding units of transport speed; normal uses 128u.
        const double u = 1.1102230246251565e-16;
        double maxFeature = 0, maxWitness = 0, maxVelocity = 0, maxNormal = 0, maxGap = 0, maxZeroGap = 0;
        foreach (var origin in new[] { 0d, 150_000_000_000d })
        foreach (var (slope, plane) in new[] { (0d, false), (0d, true), (.5d, true) })
        foreach (var gap in new[] { -2d, 0d, 2d })
        foreach (var tilt in new[] { 0d, .7d })
        {
            var system = StaticSystem(new(origin, 0, 0), new(30000, -40, 9)); var body = Body(system);
            var query = new AnalyticalSurface(slope, plane); var geometry = Geometry(4);
            var q = body.BodyFixedToRoot.Rotation;
            var centerBody = new Double3(0, 0, 15 + gap);
            var centerRoot = body.BodyFixedToRoot.LocalToParent(centerBody);
            var craftOmegaBody = new Double3(.125, -.25, .375);
            var centerVelocity = new Double3(30000.5, -39, 7);
            var localQ = DoubleQuaternion.FromAxisAngle(new(.3, .7, .2), tilt);
            var state = State(centerRoot, centerVelocity, (q * localQ).Normalized(), craftOmegaBody);
            var view = state.CreateView(); SpacecraftMotionEvaluator.TryEvaluate(view, Craft, default, out var before);
            var output = new SpacecraftContactObservation[4]; var again = new SpacecraftContactObservation[4];
            Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, output).Succeeded, "analytical generation");
            Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, again).Succeeded && output.Zip(again).All(p => SameBits(p.First, p.Second)), "bit-identical replay");
            SpacecraftMotionEvaluator.TryEvaluate(state.CreateView(), Craft, default, out var after);
            Check(before == after && state.CreateView().Revision == view.Revision, "no physical/revision mutation");
            var positionBar = 8 * (Math.BitIncrement(Math.Max(1, origin)) - Math.Max(1, origin)) + 128 * u * 32;
            var velocityBar = 128 * u * 40000 + Math.Sqrt(body.AngularVelocityRoot.LengthSquared) * positionBar;
            for (var i = 0; i < 4; i++)
            {
                var f = geometry.GetFeature(i); var local = centerBody + localQ.Rotate(f.OffsetFromComMetres);
                var rho = Math.Sqrt(local.LengthSquared); var d = local / rho;
                var surfaceRadius = !plane ? 16 : 16 / (d.Z - slope * d.X);
                var sBody = d * surfaceRadius;
                var normalBody = !plane ? d : new Double3(-slope, 0, 1) / Math.Sqrt(1 + slope * slope);
                var expectedFeature = body.BodyFixedToRoot.Translation + q.Rotate(local);
                var expectedWitness = body.BodyFixedToRoot.Translation + q.Rotate(sBody);
                // Independently distribute the rotation over the craft cross product.
                var featureVelocity = centerVelocity + q.Rotate(localQ.Rotate(Double3.Cross(craftOmegaBody, f.OffsetFromComMetres)));
                var terrainVelocity = body.VelocityRoot + Double3.Cross(body.AngularVelocityRoot, q.Rotate(sBody));
                var o = output[i];
                var pe = Norm(o.FeaturePositionRoot - expectedFeature); var se = Norm(o.TerrainPositionRoot - expectedWitness);
                var ve = Norm(o.RelativeVelocityRoot - (featureVelocity - terrainVelocity)); var ne = Norm(o.PhysicalNormalRoot - q.Rotate(normalBody));
                var ge = Math.Abs(o.RadialSignedGapMetres - (rho - surfaceRadius));
                maxFeature = Math.Max(maxFeature, pe); maxWitness = Math.Max(maxWitness, se); maxVelocity = Math.Max(maxVelocity, ve); maxNormal = Math.Max(maxNormal, ne); maxGap = Math.Max(maxGap, ge);
                // Plane intersection and normal transport amplify direction error by a bounded slope factor in this fixture.
                Check(pe <= positionBar && se <= 4 * positionBar && ge <= 4 * positionBar && ve <= velocityBar && ne <= 128 * u + positionBar / 8, "scale-derived analytical bars");
                Check(o.Feature.FeatureId == (ulong)i + 1 && o.Time == default && o.SpacecraftRevision == view.Revision, "identity/time/revision");
            }
        }
        var sys = StaticSystem(Double3.Zero, new(10, 20, 30)); var bdy = Body(sys); var sphere = new AnalyticalSurface();
        Check(SpacecraftContactGeometry.TryCreate(Craft, 2, 1, [new(1, new(0, 0, 1), ContactFeatureRole.LandingTip)], out var tip), "axial tip");
        foreach (var height in new[] { -1d, 0d, 1d })
        {
            var center = new Double3(0, 0, 15 + height); var pose = bdy.BodyFixedToRoot;
            var v = bdy.VelocityRoot + Double3.Cross(bdy.AngularVelocityRoot, pose.Rotation.Rotate(center));
            var state = State(pose.LocalToParent(center), v, pose.Rotation, pose.Rotation.Conjugate().Rotate(bdy.AngularVelocityRoot));
            var output = new SpacecraftContactObservation[1];
            Check(SpacecraftContactGenerator.Generate(state.CreateView(), state.CreateView().Revision, default, tip, bdy, sys, sphere, sphere.Authority, output).Succeeded, "co-moving point");
            Check(Math.Abs(output[0].RadialSignedGapMetres - height) <= 128 * u * 16, "zero/above/inside radial gap");
            if (height == 0) maxZeroGap = Math.Max(maxZeroGap, Math.Abs(output[0].RadialSignedGapMetres));
            if (height == 0) Check(Norm(output[0].RelativeVelocityRoot) <= 128 * u * 40, "co-moving coincident witnesses");
        }
        Console.WriteLine($"Contact oracle maxima: featureM={maxFeature:R} witnessM={maxWitness:R} relativeVelocityMps={maxVelocity:R} normalVector={maxNormal:R} radialGapM={maxGap:R}; zeroGapM={maxZeroGap:R}; replayDifference=0 bits");
    }
    private static double Norm(Double3 v) => Math.Sqrt(v.LengthSquared);
    private static bool SameBits(SpacecraftContactObservation a, SpacecraftContactObservation b)
    {
        static bool Scalar(double x, double y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y);
        static bool Vector(Double3 x, Double3 y) => Scalar(x.X, y.X) && Scalar(x.Y, y.Y) && Scalar(x.Z, y.Z);
        return a == b && Scalar(a.RadialSignedGapMetres, b.RadialSignedGapMetres) &&
            Vector(a.FeaturePositionRoot, b.FeaturePositionRoot) && Vector(a.TerrainPositionRoot, b.TerrainPositionRoot) &&
            Vector(a.TerrainDirectionBodyFixed, b.TerrainDirectionBodyFixed) && Vector(a.PhysicalNormalRoot, b.PhysicalNormalRoot) &&
            Vector(a.FeatureVelocityRoot, b.FeatureVelocityRoot) && Vector(a.TerrainVelocityRoot, b.TerrainVelocityRoot) && Vector(a.RelativeVelocityRoot, b.RelativeVelocityRoot);
    }
    private static void RevisionAdmission()
    {
        var system = StaticSystem(Double3.Zero, Double3.Zero); var body = Body(system); var query = new AnalyticalSurface();
        var state = State(body.BodyFixedToRoot.LocalToParent(new(0, 0, 20)), Double3.Zero, body.BodyFixedToRoot.Rotation, Double3.Zero);
        var clock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
        var engine = new SimulationTransactionEngine(clock, state, 8); var stale = engine.State;
        var proposal = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(engine.State, new(Craft, new(0, 0, .125), SimulationInstant.Zero));
        Check(proposal.Succeeded && engine.ValidateAndCommit(proposal.Transaction!.Value).Committed, "real canonical torque commit");
        var output = new SpacecraftContactObservation[4]; var geometry = Geometry(4);
        Check(SpacecraftContactGenerator.Generate(stale, engine.State.Revision, default, geometry, body, system, query, query.Authority, output).Status == ContactGenerationStatus.StaleState && query.Calls == 0, "live-backed old view cannot label current motion with stale revision");
        Check(SpacecraftContactGenerator.Generate(engine.State, engine.State.Revision, default, geometry, body, system, query, query.Authority, output).Succeeded && output.All(o => o.SpacecraftRevision == engine.State.Revision), "fresh post-transaction observations");
    }
    private static void Failures()
    {
        var system = StaticSystem(Double3.Zero, Double3.Zero); var body = Body(system); var geometry = Geometry(4);
        var state = State(body.BodyFixedToRoot.LocalToParent(new(0, 0, 20)), Double3.Zero, body.BodyFixedToRoot.Rotation, Double3.Zero);
        var view = state.CreateView(); var query = new AnalyticalSurface(); var output = new SpacecraftContactObservation[4];
        ContactGenerationResult Generate() => SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, output);
        Check(Generate().Succeeded, "prefill");
        foreach (var failure in Enum.GetValues<PhysicalSurfaceQueryStatus>().Where(s => s != PhysicalSurfaceQueryStatus.Ready))
        {
            query.Calls = 0; query.FailAt = 3; query.Failure = failure;
            var result = Generate();
            Check(result.Status == ContactGenerationStatus.SurfaceUnavailable && result.Written == 0 && result.FailedFeatureId == 3 && result.SurfaceStatus == failure && output.All(o => !o.IsReady), "late failure clears complete attempted set");
        }
        query.FailAt = 0; query.Calls = 0;
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, output.AsSpan(0, 3)).Status == ContactGenerationStatus.InsufficientCapacity && query.Calls == 0, "capacity before query");
        query.WrongAuthority = true; Check(Generate().Status == ContactGenerationStatus.AuthorityMismatch, "returned stale authority"); query.WrongAuthority = false;
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority with { CompositionIdentity = 77 }, output).Status == ContactGenerationStatus.AuthorityMismatch, "provider authority");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority with { BodyId = 7 }, output).Status == ContactGenerationStatus.BodyMismatch, "wrong support body");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, new(1), geometry, body, system, query, query.Authority, output).Status == ContactGenerationStatus.TimeMismatch, "wrong time");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, Body(system, root: 99), system, query, query.Authority, output).Status == ContactGenerationStatus.RootMismatch, "wrong root");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, StaticSystem(Double3.Zero, Double3.Zero), query, query.Authority, output).Status == ContactGenerationStatus.InvalidFrame, "replaced celestial definition");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, null, query.Authority, output).SurfaceStatus == PhysicalSurfaceQueryStatus.RequiredDataNotReady, "unacquired authority");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, null, body, system, query, query.Authority, output).Status == ContactGenerationStatus.InvalidGeometry, "invalid geometry");
        Check(SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, default, system, query, query.Authority, output).Status == ContactGenerationStatus.InvalidFrame, "default body sample");
    }
    private static void BodyAdapter()
    {
        var system = StaticSystem(new(100, 200, 300), new(1, 2, 3), ancestor: true); var body = Body(system);
        Check(body.BodyFixedToRoot.Translation == new Double3(110, 220, 330) && body.VelocityRoot == new Double3(5, 7, 9), "ancestor position and velocity counted once");
        var eval = new ReferenceFrameEvaluation[system.Count]; var roots = new FrameTransform[system.Count];
        Check(ContactBodyMotion.TryEvaluate(system, Graph(), new(999), default, eval, roots, new ReferenceFrameEvaluation[system.Count], new FrameTransform[system.Count], out var invalid) == ContactGenerationStatus.UnsupportedBody && !invalid.IsReady, "unsupported body");
        Check(ContactBodyMotion.TryEvaluate(system, Graph(), new(6), default, eval, roots, eval, roots, out invalid) == ContactGenerationStatus.InvalidFrame && !invalid.IsReady, "overlapping scratch");
        Check(ContactBodyMotion.TryEvaluate(system, Graph(), new(6), default, [], [], [], [], out invalid) == ContactGenerationStatus.BodyEvaluationFailed && !invalid.IsReady, "insufficient body workspace");
        Check(Body(system, new(1)).Time != body.Time, "fresh exact time");
    }
    private static void Allocations()
    {
        var system = StaticSystem(Double3.Zero, Double3.Zero); var body = Body(system); var geometry = Geometry(4); var query = new AnalyticalSurface();
        var state = State(body.BodyFixedToRoot.LocalToParent(new(0, 0, 20)), Double3.Zero, body.BodyFixedToRoot.Rotation, Double3.Zero); var view = state.CreateView();
        var output = new SpacecraftContactObservation[4];
        for (var i = 0; i < 20000; i++) SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, output);
        Check(GC.TryStartNoGCRegion(1L << 20, disallowFullBlockingGC: true), "allocation-context measurement boundary");
        long bytes; var successes = 0;
        try
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++) if (SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, body, system, query, query.Authority, output).Succeeded) successes++;
            bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        }
        finally { GC.EndNoGCRegion(); }
        Check(successes == 10000, "all allocation-window observations complete"); Check(bytes == 0, $"warmed allocations={bytes}");
        Console.WriteLine($"Contact warmed allocations={bytes}; completeFourFeatureBatches={successes}");
    }
}
