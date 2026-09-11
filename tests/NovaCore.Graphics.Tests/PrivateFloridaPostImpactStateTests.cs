using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static ContactGenerationFixture;
using Case = FloridaContactProductionTests.Case;

internal static class PrivateFloridaPostImpactStateTests
{
    private static void Check(bool value, string name) => ContactGenerationFixture.Check(value, "private root state " + name);
    private static readonly FloridaKinematicsRequest Kinematics = new(1e-5, 1e-8, 1e-5, 1e-12, 24);
    private static readonly CertifiedResponseRequest ResponseRequest = new(1e-6, 1e-6, 1e-6, 3e-6);
    private static readonly CertifiedPreImpactVelocityRequest PreimpactRequest = new(1e-5, 1e-4);
    // Maximum FULL source-position component width: at most 1e-5 s times <4e4 m/s,
    // with remaining room for acceleration and outward arithmetic. This is not a contact tolerance.
    private static readonly PrivatePostImpactPoseRequest PoseRequest = new(.5);
    private static readonly PrivatePostImpactPoseRequest Tight = new(1e-30);

    private readonly record struct Inputs(Case Case, FloridaContactProvider.Proof Root,
        FloridaContactProvider.Proof.Kinematics Witness, FloridaContactProvider.Proof.ResponseProposal Response,
        FloridaContactProvider.Proof.PreImpactVelocity Preimpact, FloridaContactProvider.Proof.PostImpactVelocity Realization)
    {
        internal PrivatePostImpactStateResult Prepare(PrivatePostImpactPoseRequest request) =>
            Root.PreparePrivatePostImpactState(Witness, Response, ResponseRequest, Preimpact, PreimpactRequest,
                Realization, request, Case.Use);
        internal PrivatePostImpactStateStatus Read(FloridaContactProvider.Proof.PrivatePostImpactState state,
            out PrivatePostImpactStateValues values) => state.Read(Root, Realization, PoseRequest, Case.Use, out values);
    }

    internal static void Run()
    {
        var repository = GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository, "assets", "earth", "runtime"), out var error), error);
        Check(TerrainAssetCache.TryResolveRequired(repository, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var path, out error), error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path, out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, repository, out var acquired) == PhysicalSurfaceQueryStatus.Ready, "real Earth authority acquired");
        var query = acquired!;
        var force = Source(Create(query));
        var before = Snapshot(force.Case);
        var prepared = force.Prepare(PoseRequest);
        Check(prepared.Status == PrivatePostImpactStateStatus.Ready, "constant-force preparation: " + prepared.Failure);
        Check(force.Read(prepared.State, out var values) == PrivatePostImpactStateStatus.Ready, "constant-force checked read");
        ValidateValues(force, values);
        Print("constant force", values);
        Mismatches(force, prepared.State, values, query);
        Check(before == Snapshot(force.Case), "constant-force full authoritative nonmutation");

        var coast = Source(Create(query, false));
        var coastBefore = Snapshot(coast.Case);
        var coasting = coast.Prepare(PoseRequest);
        Check(coasting.Status == PrivatePostImpactStateStatus.Ready, "constant-velocity preparation: " + coasting.Failure);
        Check(coast.Read(coasting.State, out var coastValues) == PrivatePostImpactStateStatus.Ready, "constant-velocity checked read");
        ValidateValues(coast, coastValues);
        Print("constant velocity", coastValues);
        var coastReplay = Source(Create(query, false));
        var coastRebuilt = coastReplay.Prepare(PoseRequest);
        Check(coastRebuilt.Status == PrivatePostImpactStateStatus.Ready &&
            coastReplay.Read(coastRebuilt.State, out var coastAgain) == PrivatePostImpactStateStatus.Ready &&
            ReplayKey(coastValues) == ReplayKey(coastAgain), "constant-velocity independent complete replay");
        Check(coastBefore == Snapshot(coast.Case), "constant-velocity full authoritative nonmutation");
        Refusals(query);

        var other = Source(Create(query));
        Measure("source reconstruction", () => TrySource(force.Case, out _), false);
        Measure("private state construction", () => force.Prepare(PoseRequest).Status == PrivatePostImpactStateStatus.Ready);
        Measure("checked read", () => force.Read(prepared.State, out _) == PrivatePostImpactStateStatus.Ready);
        Measure("invalid pose request refusal", () => force.Prepare(default).Status == PrivatePostImpactStateStatus.Unsupported);
        Measure("numerical pose refusal", () => force.Prepare(Tight).Status == PrivatePostImpactStateStatus.Unresolved);
        Measure("stale source refusal", () => force.Root.PreparePrivatePostImpactState(force.Witness, force.Response, ResponseRequest,
            force.Preimpact, PreimpactRequest, force.Realization, PoseRequest, other.Case.Use).Status == PrivatePostImpactStateStatus.Stale);
        OrdinaryAllocationMeasurement.PositiveControl();
        Check(before == Snapshot(force.Case), "observations leave state/clock/debt/timeline/history unchanged");
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "private-state-workspace", stateBytes = Unsafe.SizeOf<FloridaContactProvider.Proof.PrivatePostImpactState>(),
            valuesBytes = Unsafe.SizeOf<PrivatePostImpactStateValues>(), resultBytes = Unsafe.SizeOf<PrivatePostImpactStateResult>(), requestBytes = Unsafe.SizeOf<PrivatePostImpactPoseRequest>(),
            newProviderObjectsPerConstruction = 0, rootRefinements = 0, responseSelections = 0, canonicalWrites = 0,
            retainedOwner = "existing provider through copied checked receipts; no new global cache or authoritative store" }));
        Console.WriteLine("PRIVATE_FLORIDA_POSTIMPACT_STATE pose/frozen-source/final-bits/refinement/replay/stale/nonmutation/allocation PASS");
    }

    private static SimulationClock Clock(Case c) => (SimulationClock)typeof(SimulationTransactionEngine)
        .GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(c.Engine)!;
    private static Case Create(PlanetaryPhysicalSurfacePointQuery query, bool force = true)
    {
        var c = new Case(query, .5, -1, radialAcceleration: force ? -.2 : 0, attitude: new(0, 0, .6, .8));
        var accepted = Clock(c).AdvanceByHostDuration(new(1_000));
        Check(accepted.Reason == SimulationHostAdvanceStopReason.Accepted && Clock(c).PendingSimulationDebt.Ticks == 1_000 &&
            Clock(c).CurrentTime == c.Start, "nonzero debt fixture without clock advance");
        return c;
    }
    private static bool TrySource(Case c, out Inputs source)
    {
        source = default;
        if (!c.Admit(out var provider, out _)) return false;
        var root = c.Evaluate(provider!).Proof;
        var kinematics = root.QualifyKinematics(Kinematics, c.Use);
        if (!root.IsRoot || kinematics.Status != FloridaKinematicsStatus.Qualified) return false;
        var response = root.QualifyResponse(kinematics.Witness, ResponseRequest, c.Use);
        var preimpact = root.QualifyPreImpactVelocity(kinematics.Witness, PreimpactRequest, c.Use);
        if (response.Status != CertifiedResponseStatus.Qualified || preimpact.Status != CertifiedPreImpactVelocityStatus.Qualified) return false;
        var final = root.QualifyPostImpactVelocity(kinematics.Witness, response.Proposal, ResponseRequest, preimpact.Tuple, PreimpactRequest, c.Use);
        if (final.Status != CertifiedPostImpactVelocityStatus.Qualified) return false;
        source = new(c, root, kinematics.Witness, response.Proposal, preimpact.Tuple, final.Realization);
        return true;
    }
    private static Inputs Source(Case c) { Check(TrySource(c, out var source), "complete M14.9–M14.13 source chain"); return source; }

    private static void ValidateValues(Inputs s, PrivatePostImpactStateValues v)
    {
        Check(s.Case.Engine.State.Spacecraft.TryGetTranslation(Craft, out var linear, out var mass), "authoritative translation exists");
        Check(s.Case.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var angular), "authoritative rotation exists");
        Check(s.Witness.Read(s.Root, s.Case.Use, out var kinematics) == FloridaKinematicsStatus.Qualified, "checked source enclosure");
        Check(s.Realization.Read(s.Root, s.Witness, s.Response, ResponseRequest, s.Preimpact, PreimpactRequest, s.Case.Use, out var final) == CertifiedPostImpactVelocityStatus.Qualified,
            "checked realized velocities");
        Check(v.Root.Equals(s.Root) && v.Root.IsRoot && v.PoseRootEnclosure == kinematics.RootEnclosure, "exact issuing root and qualified pose enclosure");
        Check(v.FrozenSourceTranslation == linear && v.FrozenSourceRotation == angular && v.Properties == mass, "complete frozen source values");
        Check(v.SourceRevision == s.Case.Engine.State.Revision && v.SourceTimelineRevision == s.Case.Timeline.Revision &&
            v.SourceStart == s.Case.Start && v.SourceEnd == s.Case.End && v.TerrainAuthority == s.Case.Query.Authority, "captured revisions/search/source authority");
        Check(QuaternionBits(v.FrozenSourceRotation.OrientationLocalToParent).SequenceEqual(QuaternionBits(kinematics.FixedAttitude)), "stored exact-normalization attitude bits unchanged");
        var storedQ = v.FrozenSourceRotation.OrientationLocalToParent;
        var exactLeverRoot = CertifiedResponseOracle.InverseRotate(new(-storedQ.X, -storedQ.Y, -storedQ.Z, storedQ.W),
            CertifiedResponseOracle.V.From(kinematics.AuthoredLeverBodyMetres));
        Check(CertifiedResponseOracle.Contains(kinematics.LeverRootMetres, exactLeverRoot),
            "stored attitude denotes independently reconstructed exact normalized rotation");
        Check(VectorBits(v.InitialLinearVelocityRoot).SequenceEqual(VectorBits(final.LinearVelocityRoot)) &&
            VectorBits(v.InitialAngularVelocityBody).SequenceEqual(VectorBits(final.AngularVelocityBody)), "M14.13 final velocity bits installed directly");
        Check(v.FrozenSourceRotation.AngularVelocityBody == Double3.Zero && v.FrozenSourceRotation.ConstantBodyTorque == Double3.Zero &&
            v.InitialAngularVelocityBody.X != 0 && v.InitialAngularVelocityBody.Y != 0 && v.InitialAngularVelocityBody.Z != 0,
            "zero pre-spin provenance is distinct from complete nonzero post-spin");
        Check(v.FrozenSourceRotation.PrincipalInertia == new PrincipalMomentsOfInertia(2, 3, 4), "asymmetric inertia retained");
        Check(v.PositionRoot.IsFinite && FloridaKinematicsRequest.Fits(v.PositionRoot, PoseRequest.PositionComponentMetres), "finite maximum-full-width pose qualification");
        // Exact-rational polynomial range (endpoints and any interior stationary point), independent
        // of production interval arithmetic. No single time sample is declared to be alpha.
        PrivatePostImpactStateTests.AssertContains(linear, mass, v.PoseRootEnclosure, v.PositionRoot);
        Check(Math.Abs(linear.VelocityRoot.X) < 4e4 && Math.Abs(linear.VelocityRoot.Y) < 4e4 && Math.Abs(linear.VelocityRoot.Z) < 4e4,
            "predeclared Florida pose-width scale");
    }

    private static void Mismatches(Inputs s, FloridaContactProvider.Proof.PrivatePostImpactState state,
        PrivatePostImpactStateValues values, PlanetaryPhysicalSurfacePointQuery query)
    {
        Check(s.Read(default, out var empty) == PrivatePostImpactStateStatus.Unsupported && empty == default, "default private state has no ready payload");
        var constructors = typeof(FloridaContactProvider.Proof.PrivatePostImpactState).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Check(constructors.Length > 0 && constructors.All(c => c.IsPrivate), "only private construction can issue state");
        Check(state.Read(default, s.Realization, PoseRequest, s.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported &&
            state.Read(s.Root, default, PoseRequest, s.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported, "default expected inputs refused");
        Check(state.Read(s.Root, s.Realization, new(1), s.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported, "changed pose request cannot borrow readiness");
        var tight = s.Prepare(Tight);
        Check(tight.Status == PrivatePostImpactStateStatus.Unresolved && tight.State.Read(s.Root, s.Realization, Tight, s.Case.Use, out var missing) == PrivatePostImpactStateStatus.Unsupported &&
            missing == default, "tight numerical refusal exposes no partial state");
        foreach (var invalid in new[] { default(PrivatePostImpactPoseRequest), new(-1), new(double.NaN), new(double.PositiveInfinity) })
        {
            var failure = s.Prepare(invalid);
            Check(failure.Status == PrivatePostImpactStateStatus.Unsupported && failure.State.Read(s.Root, s.Realization, invalid, s.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported,
                "invalid pose request refused");
        }
        Check(s.Root.PreparePrivatePostImpactState(default, s.Response, ResponseRequest, s.Preimpact, PreimpactRequest, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported &&
            s.Root.PreparePrivatePostImpactState(s.Witness, default, ResponseRequest, s.Preimpact, PreimpactRequest, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported &&
            s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, ResponseRequest, default, PreimpactRequest, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported &&
            s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, ResponseRequest, s.Preimpact, PreimpactRequest, default, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported,
            "each default banked input prevents construction");
        foreach (var request in new[] { ResponseRequest with { InverseMassPerKilogram = 2e-6 }, ResponseRequest with { ScalarImpulse = 2e-6 },
            ResponseRequest with { LinearImpulseComponent = 2e-6 }, ResponseRequest with { AngularImpulseComponent = 6e-6 } })
            Check(s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, request, s.Preimpact, PreimpactRequest, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported,
                "each original response request component retained");
        foreach (var request in new[] { PreimpactRequest with { ComVelocityComponentMetresPerSecond = 2e-5 }, PreimpactRequest with { RelativeVelocityComponentMetresPerSecond = 2e-4 } })
            Check(s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, ResponseRequest, s.Preimpact, request, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported,
                "each original pre-impact request component retained");

        var refined = s.Root.Refine(1e-5, 24, s.Case.Use).Proof;
        Check(s.Witness.Read(refined, s.Case.Use, out _) == FloridaKinematicsStatus.Qualified, "banked same-owner lineage remains valid");
        Check(state.Read(refined, s.Realization, PoseRequest, s.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported, "changed issuing root requires complete reissuance");
        var rr = refined.QualifyResponse(s.Witness, ResponseRequest, s.Case.Use);
        var rp = refined.QualifyPreImpactVelocity(s.Witness, PreimpactRequest, s.Case.Use);
        var rv = refined.QualifyPostImpactVelocity(s.Witness, rr.Proposal, ResponseRequest, rp.Tuple, PreimpactRequest, s.Case.Use);
        Check(rv.Status == CertifiedPostImpactVelocityStatus.Qualified, "refined-root velocity reissuance");
        var reissued = refined.PreparePrivatePostImpactState(s.Witness, rr.Proposal, ResponseRequest, rp.Tuple, PreimpactRequest, rv.Realization, PoseRequest, s.Case.Use);
        Check(reissued.Status == PrivatePostImpactStateStatus.Ready, "explicit private-state reissuance");
        Check(reissued.State.Read(refined, rv.Realization, PoseRequest, s.Case.Use, out var changedRootValues) == PrivatePostImpactStateStatus.Ready &&
            PoseAndVelocityBits(values).SequenceEqual(PoseAndVelocityBits(changedRootValues)), "same qualified pose/result under reissued lineage");
        Check(s.Read(state, out var original) == PrivatePostImpactStateStatus.Ready && ReplayKey(values) == ReplayKey(original), "refinement does not globally revoke original state");
        var coarse = s.Root.QualifyKinematics(new(1, .01, .5, 1e-12, 24), s.Case.Use);
        Check(coarse.Status == FloridaKinematicsStatus.Qualified && s.Root.PreparePrivatePostImpactState(coarse.Witness, s.Response, ResponseRequest,
            s.Preimpact, PreimpactRequest, s.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported, "changed qualified witness cannot borrow realization");

        var replay = Source(Create(query));
        var rebuilt = replay.Prepare(PoseRequest);
        Check(rebuilt.Status == PrivatePostImpactStateStatus.Ready, "independent replay prepares");
        Check(replay.Read(rebuilt.State, out var replayValues) == PrivatePostImpactStateStatus.Ready && ReplayKey(values) == ReplayKey(replayValues), "complete meaningful replay data and bits");
        Check(state.Read(replay.Root, replay.Realization, PoseRequest, replay.Case.Use, out _) == PrivatePostImpactStateStatus.Unsupported &&
            s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, ResponseRequest, s.Preimpact, PreimpactRequest, replay.Realization, PoseRequest, s.Case.Use).Status == PrivatePostImpactStateStatus.Unsupported,
            "numerically identical foreign capabilities do not transfer");
        for (var i = 0; i < 8; i++) Check(s.Read(state, out var again) == PrivatePostImpactStateStatus.Ready && ReplayKey(values) == ReplayKey(again), "deterministic repeated checked read");
    }

    private sealed class CopiedTerrain(IPhysicalSurfacePointQuery source) : IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority => source.Authority;
        public PhysicalSurfacePointResult Query(ulong body, in Double3 direction) => source.Query(body, direction);
    }
    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        var s = Source(Create(query)); var state = s.Prepare(PoseRequest).State; var other = Create(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(1, new(1, -2, 4), ContactFeatureRole.LandingTip)], out var geometry), "changed geometry fixture");
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(2, new(1, -2, 3), ContactFeatureRole.LandingTip)], out var feature), "changed feature fixture");
        foreach (var use in new[] { s.Case.Use with { Engine = other.Engine }, s.Case.Use with { Geometry = geometry! }, s.Case.Use with { Geometry = feature! },
            s.Case.Use with { Graph = other.Graph }, s.Case.Use with { System = SolAnalyticalDefinition.CreateForTest() }, s.Case.Use with { Terrain = new CopiedTerrain(query) } })
        {
            var before = Snapshot(s.Case);
            Check(state.Read(s.Root, s.Realization, PoseRequest, use, out var value) == PrivatePostImpactStateStatus.Stale && value == default &&
                s.Root.PreparePrivatePostImpactState(s.Witness, s.Response, ResponseRequest, s.Preimpact, PreimpactRequest, s.Realization, PoseRequest, use).Status == PrivatePostImpactStateStatus.Stale,
                "changed source/terrain/frame/geometry authority refused");
            Check(before == Snapshot(s.Case), "source-use refusal preserves complete authority");
        }
        foreach (var change in new[] { "timeline", "clock", "state", "force", "torque", "mass", "inertia" })
        {
            var a = Source(Create(query)); var oldState = a.Prepare(PoseRequest).State;
            var oldRevision = a.Case.Engine.State.Revision; var oldTimeline = a.Case.Timeline.Revision;
            Action restore = () => { };
            try
            {
                if (change == "timeline")
                {
                    Check(a.Case.Timeline.Schedule(a.Case.Start, new(new(71), new(1000001), 0, SimulationEventKind.NoOpMarker)).Succeeded, "timeline-only schedule");
                    a.Case.Timeline.Cancel(new(71)); Check(a.Case.Engine.State.Revision == oldRevision, "timeline-only control");
                }
                else if (change == "clock")
                {
                    a.Case.Engine.AdvanceAndExecuteOneCanonicalGroup(new(a.Case.Start.Ticks + 1));
                    Check(a.Case.Engine.State.Revision == oldRevision && a.Case.Timeline.Revision == oldTimeline, "clock-only control");
                }
                else if (change == "torque")
                {
                    var replacement = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(a.Case.Engine.State, new SpacecraftTorqueCommand(Craft, new(0, 1, 0), a.Case.Start));
                    Check(replacement.Succeeded && a.Case.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed, "torque source change");
                }
                else if (change is "mass" or "inertia")
                {
                    // Test-local fault injection verifies frozen-property matching even without a revision change.
                    var store = typeof(SimulationState).GetField("_spacecraft", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(a.Case.State)!;
                    if (change == "mass")
                    {
                        var data = (SpacecraftPhysicalProperties[])store.GetType().GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
                        var old = data[0]; data[0] = new(0); restore = () => data[0] = old;
                    }
                    else
                    {
                        var data = (SpacecraftRigidBodyRotationState[])store.GetType().GetField("_rigidBodies", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
                        var old = data[0]; data[0] = old with { PrincipalInertia = new(-1, 3, 4) }; restore = () => data[0] = old;
                    }
                    Check(a.Case.Engine.State.Revision == oldRevision && a.Case.Timeline.Revision == oldTimeline, "invalid frozen-property control");
                }
                else
                {
                    var request = new SimulationEventRequest(new(91), a.Case.Start, 0, SimulationEventKind.Marker);
                    if (change == "force") Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91), 0, new(Craft, a.Case.Start, new(2, 0, 0)), out request), "force intent");
                    Check(a.Case.Timeline.Schedule(a.Case.Start, request).Succeeded && a.Case.Engine.ExecuteCanonicalPendingEvent().Committed, "state source transaction");
                }
                var before = Snapshot(a.Case);
                Check(a.Read(oldState, out var value) == PrivatePostImpactStateStatus.Stale && value == default && a.Prepare(PoseRequest).Status == PrivatePostImpactStateStatus.Stale,
                    change + " invalidates state and construction");
                Check(before == Snapshot(a.Case), change + " refusal does not modify deliberate source state");
            }
            finally { restore(); }
            if (change is "mass" or "inertia") Check(a.Read(oldState, out _) == PrivatePostImpactStateStatus.Ready, "fault injection fully restored");
        }
    }

    private static string Snapshot(Case c)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft, out var linear, out var mass);
        c.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var angular);
        var clock = Clock(c);
        return JsonSerializer.Serialize(new { linear, mass, angular, c.Engine.State.Revision, c.Engine.State.MarkerValue,
            timeline = c.Timeline.Revision, pending = c.Timeline.PendingCount, clock.CurrentTime, clock.PendingSimulationDebt, clock.Rate, clock.RateRemainder, clock.IsPaused,
            general = c.Engine.ProcessedCount, forces = c.Engine.ProcessedSpacecraftForceCount, torques = c.Engine.ProcessedRigidBodyTorqueCount,
            attitudes = c.Engine.ProcessedSpacecraftAttitudeCount, contacts = c.Engine.ProcessedContactImpulseCount,
            terrain = c.Query.Authority, geometry = c.Geometry.Identity });
    }
    private static long[] VectorBits(Double3 v) => [BitConverter.DoubleToInt64Bits(v.X), BitConverter.DoubleToInt64Bits(v.Y), BitConverter.DoubleToInt64Bits(v.Z)];
    private static long[] QuaternionBits(DoubleQuaternion q) => [BitConverter.DoubleToInt64Bits(q.X), BitConverter.DoubleToInt64Bits(q.Y), BitConverter.DoubleToInt64Bits(q.Z), BitConverter.DoubleToInt64Bits(q.W)];
    private static long[] BoundsBits(FloridaBound b) => [BitConverter.DoubleToInt64Bits(b.Lower), BitConverter.DoubleToInt64Bits(b.Upper)];
    private static long[] PoseAndVelocityBits(PrivatePostImpactStateValues v) => BoundsBits(v.PoseRootEnclosure)
        .Concat(BoundsBits(v.PositionRoot.X)).Concat(BoundsBits(v.PositionRoot.Y)).Concat(BoundsBits(v.PositionRoot.Z))
        .Concat(QuaternionBits(v.FrozenSourceRotation.OrientationLocalToParent)).Concat(VectorBits(v.InitialLinearVelocityRoot)).Concat(VectorBits(v.InitialAngularVelocityBody)).ToArray();
    private static string ReplayKey(PrivatePostImpactStateValues v) => JsonSerializer.Serialize(new { root = new { v.Root.RootEnclosure, v.Root.Function, v.Root.Derivative, v.Root.RefinementCount },
        v.FrozenSourceTranslation, v.FrozenSourceRotation, v.Properties, v.SourceRevision, v.SourceTimelineRevision, v.SourceStart, v.SourceEnd, v.TerrainAuthority,
        poseAndVelocityBits = PoseAndVelocityBits(v) });
    private static void Print(string fixture, PrivatePostImpactStateValues v) => Console.WriteLine(JsonSerializer.Serialize(new { kind = "private-state-values", fixture,
        root = new { v.Root.RootEnclosure, v.Root.Function, v.Root.Derivative, v.Root.RefinementCount }, v.PoseRootEnclosure, v.PositionRoot,
        positionFullWidths = new[] { FloridaKinematicsRequest.Width(v.PositionRoot.X), FloridaKinematicsRequest.Width(v.PositionRoot.Y), FloridaKinematicsRequest.Width(v.PositionRoot.Z) },
        positionMaximumFullWidth = PoseRequest.PositionComponentMetres, v.FrozenSourceTranslation, v.FrozenSourceRotation, v.Properties,
        v.SourceRevision, v.SourceTimelineRevision, v.SourceStart, v.SourceEnd, v.TerrainAuthority,
        finalLinearBits = VectorBits(v.InitialLinearVelocityRoot), finalAngularBits = VectorBits(v.InitialAngularVelocityBody),
        completePoseVelocityBits = PoseAndVelocityBits(v), poseOracle = "exact-rational polynomial range including interior stationary points", poseContainment = "PASS" }));

    private static void Measure(string name, Func<bool> work, bool requireZero = true)
    {
        for (var i = 0; i < 32; i++) Check(work(), name + " warmup");
        var ticks = new long[101];
        for (var i = 0; i < ticks.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); var complete = work(); ticks[i] = Stopwatch.GetTimestamp() - start;
            Check(complete, name + " timed outcome");
        }
        Array.Sort(ticks); var completed = 0;
        using var measurement = new OrdinaryAllocationMeasurement("private post-impact " + name);
        for (var i = 0; i < 8; i++) if (work()) completed++;
        var bytes = measurement.Complete();
        if (requireZero) OrdinaryAllocationMeasurement.RequireZero(bytes, name);
        Check(completed == 8, name + " measured completion");
        double Nanoseconds(int index) => ticks[index] * 1e9 / Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "private-state-cost", name, warmups = 32, timingSamples = 101, measuredCalls = 8,
            bytes, zeroAllocationContract = requireZero, entry = "PASS", exit = "PASS", medianNs = Nanoseconds(50), p95Ns = Nanoseconds(95),
            p99Ns = Nanoseconds(99), maximumNs = Nanoseconds(100), timerResolutionNs = 1e9 / Stopwatch.Frequency }));
    }
}
