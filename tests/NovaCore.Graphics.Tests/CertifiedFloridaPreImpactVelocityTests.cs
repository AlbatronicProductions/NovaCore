using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using static ContactGenerationFixture;
using static ContactKinematicsOracle;
using R = CertifiedResponseOracle.R;
using B = CertifiedResponseOracle.B;
using Case = FloridaContactProductionTests.Case;

internal static class CertifiedFloridaPreImpactVelocityTests
{
    private static void Check(bool value, string name) => ContactGenerationFixture.Check(value, "pre-impact " + name);
    private static readonly FloridaKinematicsRequest Kinematics = new(1e-5, 1e-8, 1e-5, 1e-12, 24);
    // Maximum FULL component widths, not a tolerance for contact or a selected velocity.
    private static readonly CertifiedPreImpactVelocityRequest Request = new(1e-5, 1e-4);
    private static readonly CertifiedPreImpactVelocityRequest Tight = new(1e-30, 1e-30);
    private static readonly CertifiedResponseRequest ResponseRequest = new(1e-6, 1e-6, 1e-6, 3e-6);

    internal static void Run()
    {
        var repository = GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository, "assets", "earth", "runtime"), out var error), error);
        Check(TerrainAssetCache.TryResolveRequired(repository, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var path, out error), error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path, out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, repository, out var acquired) == PhysicalSurfaceQueryStatus.Ready, "acquire terrain");
        var query = acquired!;
        var c = Create(query);
        var (root, witness) = Source(c);
        var before = Snapshot(c);
        var rootBefore = (root.RootEnclosure, root.Function, root.Derivative, root.RefinementCount);
        var qualified = root.QualifyPreImpactVelocity(witness, Request, c.Use);
        Check(qualified.Status == CertifiedPreImpactVelocityStatus.Qualified, "useful tuple qualifies: " + qualified.Failure);
        var tuple = qualified.Tuple;
        Check(tuple.Read(root, witness, Request, c.Use, out var values) == CertifiedPreImpactVelocityStatus.Qualified, "checked tuple read");
        Check(witness.Read(root, c.Use, out var input) == FloridaKinematicsStatus.Qualified, "checked source read");
        Check(values.AngularVelocityBody == Double3.Zero, "admitted angular velocity remains exactly zero");
        Check(input.FixedAttitude != DoubleQuaternion.Identity && input.AuthoredLeverBodyMetres != Double3.Zero, "fixed nonidentity off-center feature");
        Check(FloridaKinematicsRequest.Fits(values.ComVelocityRoot, Request.ComVelocityComponentMetresPerSecond), "all COM component widths");
        Check(FloridaKinematicsRequest.Fits(values.RelativeVelocityRoot, Request.RelativeVelocityComponentMetresPerSecond), "all relative component widths");

        // Both derived capabilities consume the actual same root and M14.10 witness. Neither is an executable command.
        var response = root.QualifyResponse(witness, ResponseRequest, c.Use);
        Check(response.Status == CertifiedResponseStatus.Qualified, "matching banked response qualification");
        Check(response.Proposal.Read(root, witness, ResponseRequest, c.Use, out var responseBefore) == CertifiedResponseStatus.Qualified, "matching response read");
        Check(responseBefore.AngularImpulseBody.X.Upper < 0 && responseBefore.AngularImpulseBody.Y.Upper < 0 &&
            responseBefore.AngularImpulseBody.Z.Upper < 0, "banked off-center angular impulse retained");
        IndependentWholeBracket(c, input, values);
        Print("constant force", values);

        // A second actual-terrain source retains constant COM velocity without substituting a stationary planet.
        var zeroForce = Create(query, false);
        var (zeroRoot, zeroWitness) = Source(zeroForce);
        var zeroResult = zeroRoot.QualifyPreImpactVelocity(zeroWitness, Request, zeroForce.Use);
        Check(zeroResult.Status == CertifiedPreImpactVelocityStatus.Qualified &&
            zeroResult.Tuple.Read(zeroRoot, zeroWitness, Request, zeroForce.Use, out _) == CertifiedPreImpactVelocityStatus.Qualified, "constant velocity real-terrain qualification");
        Check(zeroResult.Tuple.Read(zeroRoot, zeroWitness, Request, zeroForce.Use, out var zeroValues) == CertifiedPreImpactVelocityStatus.Qualified, "constant velocity checked tuple");
        Check(zeroWitness.Read(zeroRoot, zeroForce.Use, out var zeroInput) == FloridaKinematicsStatus.Qualified, "constant velocity checked source");
        IndependentWholeBracket(zeroForce, zeroInput, zeroValues);
        Print("constant velocity", zeroValues);

        var tight = root.QualifyPreImpactVelocity(witness, Tight, c.Use);
        Check(tight.Status == CertifiedPreImpactVelocityStatus.Unresolved && tight.Failure == CertifiedPreImpactVelocityFailure.RequestedWidth, "honest fixed-source width refusal");
        Check(tight.Tuple.Read(root, witness, Tight, c.Use, out var failed) == CertifiedPreImpactVelocityStatus.Unsupported && failed == default, "width refusal has no usable tuple");
        foreach (var invalid in new[] { default(CertifiedPreImpactVelocityRequest), new(double.NaN, 1), new(1, double.PositiveInfinity), new(-1, 1) })
        {
            var result = root.QualifyPreImpactVelocity(witness, invalid, c.Use);
            Check(result.Status == CertifiedPreImpactVelocityStatus.Unsupported && result.Failure == CertifiedPreImpactVelocityFailure.InvalidRequest, "invalid width request");
        }
        Check(root.QualifyPreImpactVelocity(default, Request, c.Use).Status == CertifiedPreImpactVelocityStatus.Unsupported, "default witness cannot issue");
        Check(default(FloridaContactProvider.Proof.PreImpactVelocity).Read(root, witness, Request, c.Use, out var missing) == CertifiedPreImpactVelocityStatus.Unsupported && missing == default, "default tuple cannot be consumed");
        Check(tuple.Read(default, witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported &&
            tuple.Read(root, default, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported, "default substituted inputs");
        Check(tuple.Read(root, witness, Request with { ComVelocityComponentMetresPerSecond = 2e-5 }, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported &&
            tuple.Read(root, witness, Request with { RelativeVelocityComponentMetresPerSecond = 2e-4 }, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported, "each changed request component rejected");

        var refined = root.Refine(1e-5, 24, c.Use).Proof;
        Check(witness.Read(refined, c.Use, out _) == FloridaKinematicsStatus.Qualified, "upstream immutable lineage remains valid");
        Check(tuple.Read(refined, witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported, "different supplied root requires tuple reissuance");
        Check(refined.QualifyPreImpactVelocity(witness, Request, c.Use).Status == CertifiedPreImpactVelocityStatus.Qualified, "explicit refined-root tuple issuance");
        Check(tuple.Read(root, witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Qualified, "refinement does not globally revoke original tuple");
        var coarse = root.QualifyKinematics(new(1, .01, .5, 1e-12, 24), c.Use);
        Check(coarse.Status == FloridaKinematicsStatus.Qualified, "substituted coarse qualification is valid");
        Check(tuple.Read(root, coarse.Witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported, "changed qualified source rejected");

        var replay = Create(query);
        var (replayRoot, replayWitness) = Source(replay);
        var replayResult = replayRoot.QualifyPreImpactVelocity(replayWitness, Request, replay.Use);
        Check(replayResult.Tuple.Read(replayRoot, replayWitness, Request, replay.Use, out var repeated) == CertifiedPreImpactVelocityStatus.Qualified &&
            Bits(values).SequenceEqual(Bits(repeated)), "independent replay numerical bits");
        Check(tuple.Read(replayRoot, replayWitness, Request, replay.Use, out _) == CertifiedPreImpactVelocityStatus.Unsupported &&
            root.QualifyPreImpactVelocity(replayWitness, Request, c.Use).Status == CertifiedPreImpactVelocityStatus.Unsupported, "equal numerical replay cannot transfer capabilities");
        uint seed = 131;
        for (var i = 0; i < 24; i++)
        {
            seed = unchecked(seed * 1664525 + 1013904223);
            var request = (seed & 1) == 0 ? Request : Tight;
            var result = root.QualifyPreImpactVelocity(witness, request, c.Use);
            Check(result.Status == ((seed & 1) == 0 ? CertifiedPreImpactVelocityStatus.Qualified : CertifiedPreImpactVelocityStatus.Unresolved), "seeded request status");
            Check(root.QualifyPreImpactVelocity(witness, Request, c.Use).Tuple.Read(root, witness, Request, c.Use, out var next) == CertifiedPreImpactVelocityStatus.Qualified &&
                Bits(values).SequenceEqual(Bits(next)), "request reordering preserves exact bits");
        }
        Check(response.Proposal.Read(root, witness, ResponseRequest, c.Use, out var responseAfter) == CertifiedResponseStatus.Qualified && responseBefore == responseAfter,
            "exact response and angular-impulse evidence unchanged");
        Check(before == Snapshot(c) && rootBefore == (root.RootEnclosure, root.Function, root.Derivative, root.RefinementCount), "source/root/authority nonmutation");
        Refusals(query);

        Measure("request setup", () => new CertifiedPreImpactVelocityRequest(1e-5, 1e-4).IsValid);
        Measure("qualification", () => root.QualifyPreImpactVelocity(witness, Request, c.Use).Status == CertifiedPreImpactVelocityStatus.Qualified);
        Measure("checked consumption", () => tuple.Read(root, witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Qualified);
        Measure("paired response consumption", () => tuple.Read(root, witness, Request, c.Use, out _) == CertifiedPreImpactVelocityStatus.Qualified &&
            response.Proposal.Read(root, witness, ResponseRequest, c.Use, out _) == CertifiedResponseStatus.Qualified);
        Measure("width refusal", () => root.QualifyPreImpactVelocity(witness, Tight, c.Use).Status == CertifiedPreImpactVelocityStatus.Unresolved);
        Measure("stale refusal", () => root.QualifyPreImpactVelocity(witness, Request, replay.Use).Status == CertifiedPreImpactVelocityStatus.Stale);
        Measure("unsupported refusal", () => default(FloridaContactProvider.Proof).QualifyPreImpactVelocity(witness, Request, c.Use).Status == CertifiedPreImpactVelocityStatus.Unsupported);
        Check(before == Snapshot(c), "measurements do not mutate authority");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "preimpact-workspace", tupleBytes = Unsafe.SizeOf<FloridaContactProvider.Proof.PreImpactVelocity>(),
            valuesBytes = Unsafe.SizeOf<CertifiedPreImpactVelocityValues>(), requestBytes = Unsafe.SizeOf<CertifiedPreImpactVelocityRequest>(),
            additionalHeapWorkspaceBytes = 0, sourceReadsPerQualification = 1, sourceReadsPerConsumption = 1,
            motionEvaluationsPerQualification = 1, rootRefinementsPerQualification = 0,
            policyVersion = FloridaContactProvider.Proof.PreImpactVelocity.PolicyVersion, numericalVersion = FloridaContactProvider.Proof.PreImpactVelocity.NumericalVersion }));
        Console.WriteLine("CERTIFIED_FLORIDA_PREIMPACT_VELOCITY whole-bracket/material/coherence/refusal/replay/stale/nonmutation/allocation PASS");
    }

    private static Case Create(PlanetaryPhysicalSurfacePointQuery query, bool force = true) =>
        new(query, .5, -1, radialAcceleration: force ? -.2 : 0, attitude: new(0, 0, .6, .8));

    private static (FloridaContactProvider.Proof, FloridaContactProvider.Proof.Kinematics) Source(Case c)
    {
        Check(c.Admit(out var provider, out _), "source admission");
        var root = c.Evaluate(provider!).Proof;
        var result = root.QualifyKinematics(Kinematics, c.Use);
        Check(root.IsRoot && result.Status == FloridaKinematicsStatus.Qualified, "source root kinematics");
        return (root, result.Witness);
    }

    private static string Snapshot(Case c)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft, out var linear, out var mass);
        c.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var angular);
        return JsonSerializer.Serialize(new { linear, mass, angular, c.Engine.State.Revision, c.Engine.State.MarkerValue,
            timeline = c.Timeline.Revision, pending = c.Timeline.PendingCount, time = c.Engine.ContactProofCurrentTime,
            general = c.Engine.ProcessedCount, forces = c.Engine.ProcessedSpacecraftForceCount, torques = c.Engine.ProcessedRigidBodyTorqueCount,
            attitudes = c.Engine.ProcessedSpacecraftAttitudeCount, contacts = c.Engine.ProcessedContactImpulseCount,
            terrain = c.Query.Authority, geometry = c.Geometry.Identity });
    }

    private static long[] Bits(CertifiedPreImpactVelocityValues values) => new[] { values.ComVelocityRoot.X, values.ComVelocityRoot.Y, values.ComVelocityRoot.Z,
        values.RelativeVelocityRoot.X, values.RelativeVelocityRoot.Y, values.RelativeVelocityRoot.Z }
        .SelectMany(b => new[] { BitConverter.DoubleToInt64Bits(b.Lower), BitConverter.DoubleToInt64Bits(b.Upper) }).ToArray();

    private static void Print(string fixture, CertifiedPreImpactVelocityValues values)
    {
        var com = new[] { values.ComVelocityRoot.X, values.ComVelocityRoot.Y, values.ComVelocityRoot.Z };
        var relative = new[] { values.RelativeVelocityRoot.X, values.RelativeVelocityRoot.Y, values.RelativeVelocityRoot.Z };
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "preimpact-values", fixture, com, relative,
            comFullWidths = com.Select(FloridaKinematicsRequest.Width).ToArray(), relativeFullWidths = relative.Select(FloridaKinematicsRequest.Width).ToArray(),
            angular = values.AngularVelocityBody, comMaximumWidth = Request.ComVelocityComponentMetresPerSecond,
            relativeMaximumWidth = Request.RelativeVelocityComponentMetresPerSecond }));
    }

    private static void IndependentWholeBracket(Case c, FloridaContactKinematics input, CertifiedPreImpactVelocityValues result)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft, out var linear, out var mass);
        c.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var angular);
        c.System.TryGetNode(SolarSystemBodyIds.Earth, out var node);
        c.System.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var earth);
        c.System.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var sun);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        Check(linear.Epoch.Ticks == 0 && angular.AngularVelocityBody == Double3.Zero, "independent seed-cell/zero-spin scope");
        Check(V.From(earth.StateAtEpoch.Position).Norm > 1.1e11m && V.From(earth.StateAtEpoch.Position).Norm < 2e11m &&
            V.From(earth.StateAtEpoch.Velocity).Norm < 4e4m && sun.GravitationalParameter < 2e20 && V.From(linear.PositionRoot).Norm < 2e11m,
            "independent ODE arithmetic scales");
        var rates = Math.Abs(D(model.RaT) / (D(model.SecondsPerDay) * D(model.DaysPerCentury))) +
            Math.Abs(D(model.DecT) / (D(model.SecondsPerDay) * D(model.DaysPerCentury))) + Math.Abs(D(model.Wd) / D(model.SecondsPerDay));
        Check(rates * Pi / 180 < 1e-4m && V.From(linear.ConstantForceRoot).Norm / D(mass.MassKilograms) < 1 &&
            (V.From(linear.VelocityRoot) - V.From(earth.StateAtEpoch.Velocity)).Norm + 10 < 1000 &&
            (V.From(linear.PositionRoot) - V.From(earth.StateAtEpoch.Position)).Norm + V.From(input.AuthoredLeverBodyMetres).Norm + 10000 < 1e7m,
            "independent whole-cell derivative domain");
        // Banked near-seed proof: |Earth''''|<=1.36e-13. Cubic position error <=5.67e-15 m;
        // quadratic velocity error <=2.27e-14 m/s. 1e-10 covers these plus guarded decimal arithmetic.
        const decimal error = 1e-10m;
        decimal lo = 0, hi = 1;
        for (var i = 0; i < 26; i++)
        {
            var mid = (lo + hi) / 2;
            var point = EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, mid);
            if (point.Gap > error) lo = mid;
            else if (point.Gap < -error) hi = mid;
            else break;
        }
        Check(EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, lo).Gap > error &&
            EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, hi).Gap < -error, "independent robust root bracket");
        Check(D(input.RootEnclosure.Lower) <= lo && D(input.RootEnclosure.Upper) >= hi, "qualified source contains entire independent root bracket");
        var centerTime = (lo + hi) / 2;
        var h = (hi - lo) / 2;
        var (com, relative, material) = IndependentVelocity(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, centerTime);
        // Exact-rational affine COM velocity encloses the complete bracket, without a decimal error padding
        // that would falsely exceed the exact constant-velocity production enclosure.
        B[] comBounds = new B[3];
        var initial = new[] { linear.VelocityRoot.X, linear.VelocityRoot.Y, linear.VelocityRoot.Z };
        var force = new[] { linear.ConstantForceRoot.X, linear.ConstantForceRoot.Y, linear.ConstantForceRoot.Z };
        for (var i = 0; i < 3; i++)
            comBounds[i] = B.Point(R.From(initial[i])) + B.Point(R.From(force[i]) / R.From(mass.MassKilograms)) * new B(R.From(lo), R.From(hi));
        Check(CertifiedResponseOracle.Contains(result.ComVelocityRoot, comBounds), "independent exact-rational entire-bracket COM containment");
        // W'=aCraft-aEarth-Omega' x r-Omega x (vCraft-vEarth). Source guards give
        // |aCraft-aEarth|<10, |Omega|<1e-4, |Omega'|<1e-8, |r|<1e7, |vRel|<1000.
        // Thus every component varies by <=10.2*h. This encloses all alpha, not one center sample.
        var relativeError = 10.2m * h + error;
        Check(Contains(result.RelativeVelocityRoot, relative, relativeError), "independent entire-bracket material-relative vector containment");
        var pointAtCenter = EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, centerTime);
        Check(Math.Abs(V.Dot(pointAtCenter.Normal, relative) - pointAtCenter.Speed) <= error,
            "independent full vector/scalar material relation");
        Check(Contains(input.NormalVelocityMetresPerSecond, pointAtCenter.Speed, 10.4m * h + error), "banked normal speed independently encloses same alpha");
        var normalBox = new[] { B.From(input.NormalRoot.X), B.From(input.NormalRoot.Y), B.From(input.NormalRoot.Z) };
        var relativeBox = new[] { B.From(result.RelativeVelocityRoot.X), B.From(result.RelativeVelocityRoot.Y), B.From(result.RelativeVelocityRoot.Z) };
        var dotBox = normalBox[0] * relativeBox[0] + normalBox[1] * relativeBox[1] + normalBox[2] * relativeBox[2];
        Check(dotBox.L <= R.From(pointAtCenter.Speed - 10.4m * h - error) && dotBox.H >= R.From(pointAtCenter.Speed + 10.4m * h + error),
            "vector/scalar enclosure coherence supplements independent containment");
        // Derive material velocity independently. Subtracting production boxes alone is not the oracle.
        // |V_material'|<=|aEarth|+|Omega'|P+|Omega|V <= .02+.1+.1=.22 m/s².
        var materialError = .22m * h + error;
        var comBox = new[] { B.From(result.ComVelocityRoot.X), B.From(result.ComVelocityRoot.Y), B.From(result.ComVelocityRoot.Z) };
        var materialCenter = new[] { material.X, material.Y, material.Z };
        for (var i = 0; i < 3; i++)
        {
            var bound = comBox[i] - relativeBox[i];
            Check(bound.L <= R.From(materialCenter[i] - materialError) && bound.H >= R.From(materialCenter[i] + materialError),
                "independent moving material velocity component " + i);
        }
        Check((com - relative).Norm > 10000 && (com - relative - material).Norm < error, "Earth material motion is present rather than a stationary-plane substitute");
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "preimpact-independent-root", rootLower = lo, rootUpper = hi,
            centerCom = com, centerRelative = relative, centerMaterial = material, relativeError, materialError,
            scalar = pointAtCenter.Speed, scalarError = 10.4m * h + error,
            oracle = "exact-rational affine COM; independent Earth ODE and orientation with whole-bracket derivative bounds", containment = "PASS" }));
    }

    private static (V Com, V Relative, V Material) IndependentVelocity(in SpacecraftTranslationState linear,
        in SpacecraftRigidBodyRotationState angular, double mass, Double3 feature, decimal time)
    {
        var system = SolAnalyticalDefinition.Instance;
        system.TryGetNode(SolarSystemBodyIds.Earth, out var node);
        system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var earth);
        system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var sun);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var orientation);
        var p0 = V.From(earth.StateAtEpoch.Position);
        var v0 = V.From(earth.StateAtEpoch.Velocity);
        var r0 = p0.Norm;
        var factor = -D(sun.GravitationalParameter) / r0 / r0 / r0;
        var earthAcceleration = p0 * factor;
        var earthJerk = (v0 - p0 * (3 * V.Dot(p0, v0) / r0 / r0)) * factor;
        var acceleration = V.From(linear.ConstantForceRoot) / D(mass);
        var earthVelocity = v0 + earthAcceleration * time + earthJerk * (time * time / 2);
        // Difference stored root positions BEFORE evaluating the small relative displacement.
        var featureRelative = V.From(linear.PositionRoot) - p0 + Lever(angular.OrientationLocalToParent, feature) +
            (V.From(linear.VelocityRoot) - v0) * time + (acceleration - earthAcceleration) * (time * time / 2) - earthJerk * (time * time * time / 6);
        var (_, omega) = Orientation(orientation, time, FloridaFacilitySupport.Region.Up);
        var com = V.From(linear.VelocityRoot) + acceleration * time;
        var material = earthVelocity + V.Cross(omega, featureRelative);
        return (com, com - material, material);
    }

    private sealed class CopiedTerrain(IPhysicalSurfacePointQuery source) : IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority => source.Authority;
        public PhysicalSurfacePointResult Query(ulong body, in Double3 direction) => source.Query(body, direction);
    }

    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        var c = Create(query);
        var (root, witness) = Source(c);
        var tuple = root.QualifyPreImpactVelocity(witness, Request, c.Use).Tuple;
        var other = Create(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(1, new(1, -2, 4), ContactFeatureRole.LandingTip)], out var geometry), "changed geometry fixture");
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(2, new(1, -2, 3), ContactFeatureRole.LandingTip)], out var feature), "changed feature fixture");
        foreach (var use in new[] { c.Use with { Engine = other.Engine }, c.Use with { Geometry = geometry! }, c.Use with { Geometry = feature! },
            c.Use with { Graph = other.Graph }, c.Use with { System = SolAnalyticalDefinition.CreateForTest() }, c.Use with { Terrain = new CopiedTerrain(query) } })
            Check(tuple.Read(root, witness, Request, use, out var value) == CertifiedPreImpactVelocityStatus.Stale && value == default &&
                root.QualifyPreImpactVelocity(witness, Request, use).Status == CertifiedPreImpactVelocityStatus.Stale, "changed authority invalidates issuance and consumption");
        foreach (var change in new[] { "timeline", "clock", "state", "force", "torque", "mass", "inertia" })
        {
            var a = Create(query);
            var (ar, aw) = Source(a);
            var at = ar.QualifyPreImpactVelocity(aw, Request, a.Use).Tuple;
            var oldState = a.Engine.State.Revision;
            var oldTimeline = a.Timeline.Revision;
            Action restore = () => { };
            try
            {
                if (change == "timeline")
                {
                    Check(a.Timeline.Schedule(a.Start, new(new(71), new(1000001), 0, SimulationEventKind.NoOpMarker)).Succeeded, "timeline-only schedule");
                    a.Timeline.Cancel(new(71));
                    Check(a.Engine.State.Revision == oldState, "timeline-only source control");
                }
                else if (change == "clock")
                {
                    a.Engine.AdvanceAndExecuteOneCanonicalGroup(new(a.Start.Ticks + 1));
                    Check(a.Engine.State.Revision == oldState && a.Timeline.Revision == oldTimeline, "clock-only source control");
                }
                else if (change == "torque")
                {
                    var replacement = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(a.Engine.State,
                        new SpacecraftTorqueCommand(Craft, new(0, 1, 0), a.Start));
                    Check(replacement.Succeeded && a.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed, "direct torque commit");
                    Check(a.Timeline.Revision == oldTimeline, "direct torque does not need timeline change");
                }
                else if (change is "mass" or "inertia")
                {
                    // Confined fault injection, restored even on assertion failure. No mutation API is added.
                    var store = typeof(NovaCore.Simulation.Transactions.SimulationState).GetField("_spacecraft", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(a.State)!;
                    if (change == "mass")
                    {
                        var data = (SpacecraftPhysicalProperties[])store.GetType().GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
                        var old = data[0]; data[0] = new(9); restore = () => data[0] = old;
                    }
                    else
                    {
                        var data = (SpacecraftRigidBodyRotationState[])store.GetType().GetField("_rigidBodies", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
                        var old = data[0]; data[0] = old with { PrincipalInertia = new(3, 4, 5) }; restore = () => data[0] = old;
                    }
                    Check(a.Engine.State.Revision == oldState && a.Timeline.Revision == oldTimeline, "direct frozen-property guard control");
                }
                else
                {
                    var request = new SimulationEventRequest(new(91), a.Start, 0, SimulationEventKind.Marker);
                    if (change == "force") Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91), 0, new(Craft, a.Start, new(2, 0, 0)), out request), "force intent");
                    Check(a.Timeline.Schedule(a.Start, request).Succeeded && a.Engine.ExecuteCanonicalPendingEvent().Committed, "source transaction");
                }
                Check(at.Read(ar, aw, Request, a.Use, out var value) == CertifiedPreImpactVelocityStatus.Stale && value == default &&
                    ar.QualifyPreImpactVelocity(aw, Request, a.Use).Status == CertifiedPreImpactVelocityStatus.Stale, change + " invalidates issuance/read");
            }
            finally { restore(); }
            if (change is "mass" or "inertia") Check(at.Read(ar, aw, Request, a.Use, out _) == CertifiedPreImpactVelocityStatus.Qualified, "fault injection restored");
        }
    }

    private static void Measure(string name, Func<bool> work)
    {
        for (var i = 0; i < 32; i++) Check(work(), name + " warmup");
        var ticks = new long[101];
        for (var i = 0; i < ticks.Length; i++)
        {
            var start = Stopwatch.GetTimestamp();
            var completed = work();
            ticks[i] = Stopwatch.GetTimestamp() - start;
            Check(completed, name + " timing completion");
        }
        Array.Sort(ticks);
        var completedCalls = 0;
        using var measurement = new OrdinaryAllocationMeasurement("certified pre-impact " + name);
        for (var i = 0; i < 8; i++) if (work()) completedCalls++;
        var bytes = measurement.Complete();
        OrdinaryAllocationMeasurement.RequireZero(bytes, name);
        Check(completedCalls == 8, name + " measured completion");
        double Nanoseconds(int index) => ticks[index] * 1e9 / Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "preimpact-cost", name, warmups = 32, timingSamples = 101, measuredCalls = 8,
            bytes, entry = "PASS", exit = "PASS", medianNs = Nanoseconds(50), p95Ns = Nanoseconds(95), p99Ns = Nanoseconds(99),
            maximumNs = Nanoseconds(100), timerResolutionNs = 1e9 / Stopwatch.Frequency }));
    }
}
