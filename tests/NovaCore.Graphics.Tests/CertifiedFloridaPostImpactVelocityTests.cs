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

internal static class CertifiedFloridaPostImpactVelocityTests
{
    private static void Check(bool value, string name) => ContactGenerationFixture.Check(value, "post-impact " + name);
    private static readonly FloridaKinematicsRequest Kinematics = new(1e-5, 1e-8, 1e-5, 1e-12, 24);
    private static readonly CertifiedResponseRequest ResponseRequest = new(1e-6, 1e-6, 1e-6, 3e-6);
    private static readonly CertifiedPreImpactVelocityRequest VelocityRequest = new(1e-5, 1e-4);

    private readonly record struct Inputs(Case Case, FloridaContactProvider.Proof Root,
        FloridaContactProvider.Proof.Kinematics Witness, FloridaContactProvider.Proof.ResponseProposal Response,
        FloridaContactProvider.Proof.PreImpactVelocity Preimpact)
    {
        internal CertifiedPostImpactVelocityResult Qualify() => Root.QualifyPostImpactVelocity(Witness, Response,
            ResponseRequest, Preimpact, VelocityRequest, Case.Use);
        internal CertifiedPostImpactVelocityStatus Read(FloridaContactProvider.Proof.PostImpactVelocity receipt,
            out CertifiedPostImpactVelocityValues values) => receipt.Read(Root, Witness, Response, ResponseRequest,
                Preimpact, VelocityRequest, Case.Use, out values);
    }

    internal static void Run()
    {
        var repository = GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository, "assets", "earth", "runtime"), out var error), error);
        Check(TerrainAssetCache.TryResolveRequired(repository, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var path, out error), error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path, out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, repository, out var acquired) == PhysicalSurfaceQueryStatus.Ready, "acquire production terrain");
        var query = acquired!;
        var source = Source(Create(query));
        var before = Snapshot(source.Case);
        var rootBefore = RootBits(source.Root);
        var result = source.Qualify();
        Check(result.Status == CertifiedPostImpactVelocityStatus.Qualified, "constant-force realization: " + result.Failure);
        var receipt = result.Realization;
        Check(source.Read(receipt, out var values) == CertifiedPostImpactVelocityStatus.Qualified, "checked realization read");
        Check(source.Witness.Read(source.Root, source.Case.Use, out var input) == FloridaKinematicsStatus.Qualified, "checked kinematics");
        Check(source.Response.Read(source.Root, source.Witness, ResponseRequest, source.Case.Use, out var responseBefore) == CertifiedResponseStatus.Qualified,
            "checked banked response");
        Check(source.Preimpact.Read(source.Root, source.Witness, VelocityRequest, source.Case.Use, out var preimpactBefore) == CertifiedPreImpactVelocityStatus.Qualified,
            "checked banked pre-impact tuple");
        VerifyAdmission(values);
        Check(values.AngularVelocityBody.X != 0 && values.AngularVelocityBody.Y != 0 && values.AngularVelocityBody.Z != 0,
            "all off-center angular components retained");
        IndependentWholeRoot(source.Case, input, values);
        Print("constant force", values);

        var zero = Source(Create(query, false));
        var zeroBefore = Snapshot(zero.Case);
        var zeroResult = zero.Qualify();
        Check(zeroResult.Status == CertifiedPostImpactVelocityStatus.Qualified, "constant-velocity realization: " + zeroResult.Failure);
        Check(zero.Read(zeroResult.Realization, out var zeroValues) == CertifiedPostImpactVelocityStatus.Qualified, "constant-velocity checked read");
        Check(zero.Witness.Read(zero.Root, zero.Case.Use, out var zeroInput) == FloridaKinematicsStatus.Qualified, "constant-velocity checked source");
        VerifyAdmission(zeroValues);
        IndependentWholeRoot(zero.Case, zeroInput, zeroValues);
        Print("constant velocity", zeroValues);
        Check(zeroBefore == Snapshot(zero.Case), "constant-velocity source unchanged");

        Mismatches(source, receipt, values, query);
        Check(source.Response.Read(source.Root, source.Witness, ResponseRequest, source.Case.Use, out var responseAfter) == CertifiedResponseStatus.Qualified && responseBefore == responseAfter,
            "M14.11 exact law unchanged");
        Check(source.Preimpact.Read(source.Root, source.Witness, VelocityRequest, source.Case.Use, out var preimpactAfter) == CertifiedPreImpactVelocityStatus.Qualified && preimpactBefore == preimpactAfter,
            "M14.12 pre-impact evidence unchanged");
        Check(before == Snapshot(source.Case) && rootBefore == RootBits(source.Root), "authority/root/clock/events/history nonmutation");
        Refusals(query);

        var other = Source(Create(query));
        Measure("source preparation", () => TrySource(source.Case, out _), false);
        Measure("qualification at fixed candidate count", () => source.Qualify().Status == CertifiedPostImpactVelocityStatus.Qualified);
        Measure("checked consumption", () => source.Read(receipt, out _) == CertifiedPostImpactVelocityStatus.Qualified);
        Measure("stale refusal", () => source.Root.QualifyPostImpactVelocity(source.Witness, source.Response, ResponseRequest,
            source.Preimpact, VelocityRequest, other.Case.Use).Status == CertifiedPostImpactVelocityStatus.Stale);
        Measure("unsupported refusal", () => default(FloridaContactProvider.Proof).QualifyPostImpactVelocity(source.Witness,
            source.Response, ResponseRequest, source.Preimpact, VelocityRequest, source.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported);
        OrdinaryAllocationMeasurement.PositiveControl();
        Check(before == Snapshot(source.Case), "performance observation leaves authority unchanged");
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "postimpact-workspace", receiptBytes = Unsafe.SizeOf<FloridaContactProvider.Proof.PostImpactVelocity>(),
            valuesBytes = Unsafe.SizeOf<CertifiedPostImpactVelocityValues>(), budgetBytes = Unsafe.SizeOf<CertifiedPostImpactVelocityBudgets>(),
            maximumCandidates = 65, values.CandidatesEvaluated, values.AdmissibleCandidates,
            policyVersion = FloridaContactProvider.Proof.PostImpactVelocity.PolicyVersion,
            numericalVersion = FloridaContactProvider.Proof.PostImpactVelocity.NumericalVersion,
            recipeVersion = FloridaContactProvider.Proof.PostImpactVelocity.RecipeVersion }));
        Console.WriteLine("CERTIFIED_FLORIDA_POSTIMPACT_VELOCITY whole-root/effective-momentum/work-energy/mismatch/stale/replay/nonmutation/allocation PASS");
    }

    private static Case Create(PlanetaryPhysicalSurfacePointQuery query, bool force = true) =>
        new(query, .5, -1, radialAcceleration: force ? -.2 : 0, attitude: new(0, 0, .6, .8));

    private static bool TrySource(Case c, out Inputs source)
    {
        source = default;
        if (!c.Admit(out var provider, out _)) return false;
        var root = c.Evaluate(provider!).Proof;
        var kinematics = root.QualifyKinematics(Kinematics, c.Use);
        if (!root.IsRoot || kinematics.Status != FloridaKinematicsStatus.Qualified) return false;
        var response = root.QualifyResponse(kinematics.Witness, ResponseRequest, c.Use);
        var preimpact = root.QualifyPreImpactVelocity(kinematics.Witness, VelocityRequest, c.Use);
        if (response.Status != CertifiedResponseStatus.Qualified || preimpact.Status != CertifiedPreImpactVelocityStatus.Qualified) return false;
        source = new(c, root, kinematics.Witness, response.Proposal, preimpact.Tuple);
        return true;
    }
    private static Inputs Source(Case c) { Check(TrySource(c, out var source), "production-owned M14.9–M14.12 source"); return source; }

    private static void VerifyAdmission(CertifiedPostImpactVelocityValues v)
    {
        Check(v.LinearVelocityRoot.IsFinite && v.AngularVelocityBody.IsFinite, "finite final paired values");
        Check(v.CandidatesEvaluated == 65 && v.CandidateIndex is >= 0 and < 65 && v.AdmissibleCandidates is > 0 and <= 65, "fixed complete finite recipe");
        Check(v.NormalResidual.IsFinite && v.NormalResidual.Lower >= 0 && v.NormalResidual.Upper <= v.Budgets.NormalResidual, "independent nonapproach/residual admission");
        Check(v.WorkAdjustedEnergy.IsFinite && v.WorkAdjustedEnergy.Upper <= 0, "prescribed-surface work passivity");
        Check(v.TangentialBound <= v.Budgets.Tangential && v.CouplingBound <= v.Budgets.Coupling, "tangential/coupling limits");
        Check(v.MassWeightedErrorBound <= v.Budgets.MassWeightedError && Magnitude(v.EnergyDefect) <= v.Budgets.EnergyDefect, "state/energy limits");
        Check(v.Budgets.NormalResidual < v.Budgets.IncomingSpeedLower && v.Budgets.EnergyDefect < v.Budgets.DissipationLower &&
            v.Budgets.MassWeightedError < 2 * v.Budgets.DissipationLower, "predeclared strict resolution guards");
        var linear = new[] { v.LinearError.X, v.LinearError.Y, v.LinearError.Z };
        var angular = new[] { v.AngularError.X, v.AngularError.Y, v.AngularError.Z };
        var bv = new[] { v.Budgets.LinearComponent.X, v.Budgets.LinearComponent.Y, v.Budgets.LinearComponent.Z };
        var bw = new[] { v.Budgets.AngularComponent.X, v.Budgets.AngularComponent.Y, v.Budgets.AngularComponent.Z };
        for (var i = 0; i < 3; i++) Check(Magnitude(linear[i]) <= bv[i] && Magnitude(angular[i]) <= bw[i], "component budget " + i);
    }
    private static double Magnitude(FloridaBound b) => Math.Max(Math.Abs(b.Lower), Math.Abs(b.Upper));

    private static void Mismatches(Inputs s, FloridaContactProvider.Proof.PostImpactVelocity receipt,
        CertifiedPostImpactVelocityValues values, PlanetaryPhysicalSurfacePointQuery query)
    {
        Check(s.Read(default, out var missing) == CertifiedPostImpactVelocityStatus.Unsupported && missing == default, "default receipt");
        Check(typeof(FloridaContactProvider.Proof.PostImpactVelocity).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .All(constructor => constructor.IsPrivate), "only private receipt construction");
        Check(receipt.Read(default, s.Witness, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported,
            "default root substitution");
        Check(receipt.Read(s.Root, default, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported,
            "default witness substitution");
        Check(receipt.Read(s.Root, s.Witness, default, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported &&
            receipt.Read(s.Root, s.Witness, s.Response, ResponseRequest, default, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported,
            "default banked receipt substitutions");
        Check(s.Root.QualifyPostImpactVelocity(default, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported &&
            s.Root.QualifyPostImpactVelocity(s.Witness, default, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported &&
            s.Root.QualifyPostImpactVelocity(s.Witness, s.Response, ResponseRequest, default, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported,
            "default evidence cannot issue a realization");
        foreach (var changed in new[] { ResponseRequest with { InverseMassPerKilogram = 2e-6 }, ResponseRequest with { ScalarImpulse = 2e-6 },
            ResponseRequest with { LinearImpulseComponent = 2e-6 }, ResponseRequest with { AngularImpulseComponent = 6e-6 } })
            Check(receipt.Read(s.Root, s.Witness, s.Response, changed, s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported &&
                s.Root.QualifyPostImpactVelocity(s.Witness, s.Response, changed, s.Preimpact, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported,
                "each response request component bound");
        foreach (var changed in new[] { VelocityRequest with { ComVelocityComponentMetresPerSecond = 2e-5 }, VelocityRequest with { RelativeVelocityComponentMetresPerSecond = 2e-4 } })
            Check(receipt.Read(s.Root, s.Witness, s.Response, ResponseRequest, s.Preimpact, changed, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported &&
                s.Root.QualifyPostImpactVelocity(s.Witness, s.Response, ResponseRequest, s.Preimpact, changed, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported,
                "each pre-impact request component bound");
        var refined = s.Root.Refine(1e-5, 24, s.Case.Use).Proof;
        Check(s.Witness.Read(refined, s.Case.Use, out _) == FloridaKinematicsStatus.Qualified, "banked immutable lineage retained");
        Check(receipt.Read(refined, s.Witness, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported,
            "changed supplied root requires reissuance");
        var refinedResponse = refined.QualifyResponse(s.Witness, ResponseRequest, s.Case.Use);
        var refinedVelocity = refined.QualifyPreImpactVelocity(s.Witness, VelocityRequest, s.Case.Use);
        var reissued = refined.QualifyPostImpactVelocity(s.Witness, refinedResponse.Proposal, ResponseRequest, refinedVelocity.Tuple, VelocityRequest, s.Case.Use);
        Check(reissued.Status == CertifiedPostImpactVelocityStatus.Qualified && reissued.Realization.Read(refined, s.Witness, refinedResponse.Proposal,
            ResponseRequest, refinedVelocity.Tuple, VelocityRequest, s.Case.Use, out var reissuedValues) == CertifiedPostImpactVelocityStatus.Qualified &&
            Bits(values).SequenceEqual(Bits(reissuedValues)), "explicit same-kinematics refined-root reissuance");
        var coarse = s.Root.QualifyKinematics(new(1, .01, .5, 1e-12, 24), s.Case.Use);
        Check(coarse.Status == FloridaKinematicsStatus.Qualified && receipt.Read(s.Root, coarse.Witness, s.Response, ResponseRequest,
            s.Preimpact, VelocityRequest, s.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported, "different qualified source rejected");
        var replay = Source(Create(query));
        var repeated = replay.Qualify();
        Check(repeated.Status == CertifiedPostImpactVelocityStatus.Qualified && replay.Read(repeated.Realization, out var repeatedValues) == CertifiedPostImpactVelocityStatus.Qualified &&
            Bits(values).SequenceEqual(Bits(repeatedValues)), "independent replay final/certificate bits");
        Check(receipt.Read(replay.Root, replay.Witness, replay.Response, ResponseRequest, replay.Preimpact, VelocityRequest, replay.Case.Use, out _) == CertifiedPostImpactVelocityStatus.Unsupported &&
            s.Root.QualifyPostImpactVelocity(s.Witness, replay.Response, ResponseRequest, s.Preimpact, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported &&
            s.Root.QualifyPostImpactVelocity(s.Witness, s.Response, ResponseRequest, replay.Preimpact, VelocityRequest, s.Case.Use).Status == CertifiedPostImpactVelocityStatus.Unsupported,
            "equal numerical replay cannot transfer either capability");
        for (var i = 0; i < 8; i++)
            Check(s.Read(receipt, out var next) == CertifiedPostImpactVelocityStatus.Qualified && Bits(values).SequenceEqual(Bits(next)), "repeated checked reads retain exact bits");
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
    private static string RootBits(FloridaContactProvider.Proof root) => JsonSerializer.Serialize(new { root.RootEnclosure, root.Function, root.Derivative, root.RefinementCount });
    private static long[] Bits(CertifiedPostImpactVelocityValues v)
    {
        var bits = new List<long>();
        void Number(double x) => bits.Add(BitConverter.DoubleToInt64Bits(x));
        void Vector(Double3 x) { Number(x.X); Number(x.Y); Number(x.Z); }
        void Interval(FloridaBound x) { Number(x.Lower); Number(x.Upper); }
        void Bounds(FloridaVector x) { Interval(x.X); Interval(x.Y); Interval(x.Z); }
        Vector(v.LinearVelocityRoot); Vector(v.AngularVelocityBody);
        Bounds(v.IdealLinearVelocityRoot); Bounds(v.IdealAngularVelocityBody); Bounds(v.LinearError); Bounds(v.AngularError);
        Bounds(v.EffectiveLinearMomentumRoot); Bounds(v.EffectiveAngularMomentumBody); Bounds(v.LinearMomentumDefectRoot); Bounds(v.AngularMomentumDefectBody);
        Interval(v.NormalResidual); Interval(v.WorkAdjustedEnergy); Interval(v.EnergyDefect);
        Number(v.TangentialBound); Number(v.CouplingBound); Number(v.MassWeightedErrorBound);
        Vector(v.Budgets.LinearComponent); Vector(v.Budgets.AngularComponent); Number(v.Budgets.NormalResidual);
        Number(v.Budgets.MassWeightedError); Number(v.Budgets.Tangential); Number(v.Budgets.Coupling); Number(v.Budgets.EnergyDefect);
        Number(v.Budgets.IncomingSpeedLower); Number(v.Budgets.DissipationLower);
        bits.Add(v.CandidateIndex); bits.Add(v.CandidatesEvaluated); bits.Add(v.AdmissibleCandidates);
        return bits.ToArray();
    }
    private static void Print(string fixture, CertifiedPostImpactVelocityValues v) => Console.WriteLine(JsonSerializer.Serialize(new { kind = "postimpact-values", fixture,
        finalBits = new[] { v.LinearVelocityRoot.X, v.LinearVelocityRoot.Y, v.LinearVelocityRoot.Z, v.AngularVelocityBody.X, v.AngularVelocityBody.Y, v.AngularVelocityBody.Z }
            .Select(BitConverter.DoubleToInt64Bits).ToArray(), values = v }));

    // Independent arithmetic helpers: BigInteger rationals and a rotation MATRIX, not production interval operations.
    private static B[] Point(Double3 v) => [B.Point(R.From(v.X)), B.Point(R.From(v.Y)), B.Point(R.From(v.Z))];
    private static B[] Add(B[] a, B[] b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
    private static B[] Subtract(B[] a, B[] b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
    private static B[] Scale(B[] a, B b) => [a[0] * b, a[1] * b, a[2] * b];
    private static B Dot(B[] a, B[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    private static B NormSquared(B[] a) => a[0].Square() + a[1].Square() + a[2].Square();
    private static B[] Cross(B[] a, B[] b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
    private static B[] Rotate(DoubleQuaternion q, B[] v, bool transpose)
    {
        var x = R.From(q.X); var y = R.From(q.Y); var z = R.From(q.Z); var w = R.From(q.W); var d = x*x + y*y + z*z + w*w;
        R[,] matrix = { { (w*w+x*x-y*y-z*z)/d, 2*(x*y-w*z)/d, 2*(x*z+w*y)/d },
            { 2*(x*y+w*z)/d, (w*w-x*x+y*y-z*z)/d, 2*(y*z-w*x)/d },
            { 2*(x*z-w*y)/d, 2*(y*z+w*x)/d, (w*w-x*x-y*y+z*z)/d } };
        B[] answer = [0, 0, 0];
        for (var i = 0; i < 3; i++) for (var j = 0; j < 3; j++) answer[i] += B.Point(transpose ? matrix[j,i] : matrix[i,j]) * v[j];
        return answer;
    }
    private static void Contains(FloridaBound value, B oracle, string name) => Check(CertifiedResponseOracle.Contains(value, oracle), "independent entire-root " + name);
    private static void Contains(FloridaVector value, B[] oracle, string name) => Check(CertifiedResponseOracle.Contains(value, oracle), "independent entire-root " + name);
    private static void Bound(double value, B squared, string name) => Check(double.IsFinite(value) && value >= 0 && R.From(value) * R.From(value) >= squared.H, "independent entire-root " + name);

    private static void IndependentWholeRoot(Case c, FloridaContactKinematics input, CertifiedPostImpactVelocityValues result)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft, out var linear, out var mass);
        c.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var angular);
        c.System.TryGetNode(SolarSystemBodyIds.Earth, out var node);
        c.System.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var earth);
        c.System.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var sun);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        Check(linear.Epoch.Ticks == 0 && angular.AngularVelocityBody == Double3.Zero, "independent seed-cell/zero-spin domain");
        Check(V.From(earth.StateAtEpoch.Position).Norm > 1.1e11m && V.From(earth.StateAtEpoch.Position).Norm < 2e11m &&
            V.From(earth.StateAtEpoch.Velocity).Norm < 4e4m && sun.GravitationalParameter < 2e20 && V.From(linear.PositionRoot).Norm < 2e11m,
            "independent Earth ODE arithmetic scales");
        var rates = Math.Abs(D(model.RaT) / (D(model.SecondsPerDay) * D(model.DaysPerCentury))) +
            Math.Abs(D(model.DecT) / (D(model.SecondsPerDay) * D(model.DaysPerCentury))) + Math.Abs(D(model.Wd) / D(model.SecondsPerDay));
        Check(rates * Pi / 180 < 1e-4m && V.From(linear.ConstantForceRoot).Norm / D(mass.MassKilograms) < 1 &&
            (V.From(linear.VelocityRoot) - V.From(earth.StateAtEpoch.Velocity)).Norm + 10 < 1000 &&
            (V.From(linear.PositionRoot) - V.From(earth.StateAtEpoch.Position)).Norm + V.From(input.AuthoredLeverBodyMetres).Norm + 10000 < 1e7m,
            "independent whole-cell derivative domain");
        // Banked near-seed Earth snap <=1.36e-13. The existing independent decimal/ODE
        // oracle's 1e-10 envelope covers cubic position / quadratic velocity remainder and arithmetic.
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
            EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, hi).Gap < -error, "independent certified-sign root bracket");
        Check(D(input.RootEnclosure.Lower) <= lo && D(input.RootEnclosure.Upper) >= hi, "source encloses independent root bracket");
        var t = (lo + hi) / 2; var h = (hi - lo) / 2;
        var center = EarthPoint(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, t);
        var (com, relative, material) = IndependentVelocity(linear, angular, mass.MassKilograms, input.AuthoredLeverBodyMetres, t);
        var (_, omega) = Orientation(model, t, FloridaFacilitySupport.Region.Up);
        var normalDerivative = V.Cross(omega, center.Normal);
        B Range(decimal value, decimal radius) => new(R.From(value - radius), R.From(value + radius));
        var normalTail = 1e-8m * h * h + 1e-22m;
        B[] n = [Range(center.Normal.X, Math.Abs(normalDerivative.X) * h + normalTail), Range(center.Normal.Y, Math.Abs(normalDerivative.Y) * h + normalTail),
            Range(center.Normal.Z, Math.Abs(normalDerivative.Z) * h + normalTail)];
        // |u'|<10.4, |W'|<10.2 over this entire guarded source cell; no midpoint is the physical root.
        var u = Range(center.Speed, 10.4m * h + error);
        B[] relativeBox = [Range(relative.X, 10.2m * h + error), Range(relative.Y, 10.2m * h + error), Range(relative.Z, 10.2m * h + error)];
        B[] comBox = new B[3];
        var velocities = new[] { linear.VelocityRoot.X, linear.VelocityRoot.Y, linear.VelocityRoot.Z };
        var forces = new[] { linear.ConstantForceRoot.X, linear.ConstantForceRoot.Y, linear.ConstantForceRoot.Z };
        for (var i = 0; i < 3; i++) comBox[i] = B.Point(R.From(velocities[i])) + B.Point(R.From(forces[i]) / R.From(mass.MassKilograms)) * new B(R.From(lo), R.From(hi));
        var physical = CertifiedResponseOracle.Enclose(n, u, input.AuthoredLeverBodyMetres, input.FixedAttitude, input.MassKilograms, input.PrincipalInertia);
        var m = B.Point(R.From(input.MassKilograms));
        B[] inertia = [B.Point(R.From(input.PrincipalInertia.X)), B.Point(R.From(input.PrincipalInertia.Y)), B.Point(R.From(input.PrincipalInertia.Z))];
        var finalV = Point(result.LinearVelocityRoot); var finalW = Point(result.AngularVelocityBody);
        var idealV = Add(comBox, Scale(physical.Linear, B.Point(1 / R.From(input.MassKilograms))));
        B[] idealW = [physical.Angular[0] / inertia[0], physical.Angular[1] / inertia[1], physical.Angular[2] / inertia[2]];
        var ev = Subtract(finalV, idealV); var ew = Subtract(finalW, idealW);
        var effectiveP = Scale(Subtract(finalV, comBox), m);
        B[] effectiveL = [inertia[0] * finalW[0], inertia[1] * finalW[1], inertia[2] * finalW[2]];
        var dP = Subtract(effectiveP, physical.Linear); var dL = Subtract(effectiveL, physical.Angular);
        var lever = Point(input.AuthoredLeverBodyMetres);
        // Direct represented-state residual; independent from production's exact-law error cancellation.
        var postRelative = Add(Add(relativeBox, Subtract(finalV, comBox)), Rotate(input.FixedAttitude, Cross(finalW, lever), false));
        var rho = Dot(n, postRelative);
        // The source normal is exactly unit, J=j*n and L=r cross Q^T J at the same alpha.
        // Cancel these identities BEFORE interval evaluation. The vectors below are exactly
        // project(dP) and dL-r cross Q^T dP, while using actual represented momenta directly
        // avoids introducing a second, independent boxed J into the physical oracle.
        var tangent = Subtract(effectiveP, Scale(n, Dot(n, effectiveP)));
        var coupling = Subtract(effectiveL, Cross(lever, Rotate(input.FixedAttitude, effectiveP, true)));
        var energy = Dot(relativeBox, effectiveP) + NormSquared(effectiveP) / (2 * m) +
            (effectiveL[0].Square() / inertia[0] + effectiveL[1].Square() / inertia[1] + effectiveL[2].Square() / inertia[2]) / 2;
        var exactEnergy = -u.Square() / (2 * physical.K);
        var defect = energy - exactEnergy;
        var weighted = m * NormSquared(ev) + inertia[0] * ew[0].Square() + inertia[1] * ew[1].Square() + inertia[2] * ew[2].Square();
        Contains(result.IdealLinearVelocityRoot, idealV, "ideal linear velocity"); Contains(result.IdealAngularVelocityBody, idealW, "ideal angular velocity");
        Contains(result.LinearError, ev, "linear target error"); Contains(result.AngularError, ew, "angular target error");
        Contains(result.EffectiveLinearMomentumRoot, effectiveP, "actual linear momentum"); Contains(result.EffectiveAngularMomentumBody, effectiveL, "actual angular momentum");
        Contains(result.LinearMomentumDefectRoot, dP, "actual linear momentum defect"); Contains(result.AngularMomentumDefectBody, dL, "actual angular momentum defect");
        Contains(result.NormalResidual, rho, "direct final-state normal residual"); Contains(result.WorkAdjustedEnergy, energy, "effective-momentum work energy");
        Contains(result.EnergyDefect, defect, "effective-momentum energy defect");
        Bound(result.TangentialBound, NormSquared(tangent), "tangential contamination"); Bound(result.CouplingBound, NormSquared(coupling), "angular coupling");
        Check(R.From(result.MassWeightedErrorBound) >= weighted.H, "independent mass-weighted state error");
        Check(rho.L >= 0 && energy.H <= 0, "independent entire-root nonapproach and work passivity");
        Check((com - relative).Norm > 10000 && (com - relative - material).Norm < error, "moving material surface retained");
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "postimpact-independent-root", rootLower = lo, rootUpper = hi,
            centerCom = com, centerRelative = relative, centerMaterial = material, relativeError = 10.2m * h + error,
            oracle = "independent guarded Earth ODE/decimal root bracket; exact-rational whole-root final-state momentum and work", containment = "PASS" }));
    }

    private static (V Com, V Relative, V Material) IndependentVelocity(in SpacecraftTranslationState linear,
        in SpacecraftRigidBodyRotationState angular, double mass, Double3 feature, decimal time)
    {
        var system = SolAnalyticalDefinition.Instance;
        system.TryGetNode(SolarSystemBodyIds.Earth, out var node); system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var earth);
        system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var sun); CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var orientation);
        var p0 = V.From(earth.StateAtEpoch.Position); var v0 = V.From(earth.StateAtEpoch.Velocity); var r0 = p0.Norm;
        var factor = -D(sun.GravitationalParameter) / r0 / r0 / r0;
        var earthAcceleration = p0 * factor; var earthJerk = (v0 - p0 * (3 * V.Dot(p0, v0) / r0 / r0)) * factor;
        var acceleration = V.From(linear.ConstantForceRoot) / D(mass);
        var earthVelocity = v0 + earthAcceleration * time + earthJerk * (time * time / 2);
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
        var s = Source(Create(query)); var receipt = s.Qualify().Realization; var other = Create(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(1, new(1, -2, 4), ContactFeatureRole.LandingTip)], out var geometry), "changed geometry fixture");
        Check(SpacecraftContactGeometry.TryCreate(Craft, 501, 1, [new(2, new(1, -2, 3), ContactFeatureRole.LandingTip)], out var feature), "changed feature fixture");
        foreach (var use in new[] { s.Case.Use with { Engine = other.Engine }, s.Case.Use with { Geometry = geometry! }, s.Case.Use with { Geometry = feature! },
            s.Case.Use with { Graph = other.Graph }, s.Case.Use with { System = SolAnalyticalDefinition.CreateForTest() }, s.Case.Use with { Terrain = new CopiedTerrain(query) } })
            Check(receipt.Read(s.Root, s.Witness, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, use, out var value) == CertifiedPostImpactVelocityStatus.Stale && value == default &&
                s.Root.QualifyPostImpactVelocity(s.Witness, s.Response, ResponseRequest, s.Preimpact, VelocityRequest, use).Status == CertifiedPostImpactVelocityStatus.Stale,
                "changed source authority invalidates qualification/read");
        foreach (var change in new[] { "timeline", "clock", "state", "force", "torque", "mass", "inertia" })
        {
            var a = Source(Create(query)); var ar = a.Qualify().Realization;
            var oldState = a.Case.Engine.State.Revision; var oldTimeline = a.Case.Timeline.Revision;
            Action restore = () => { };
            try
            {
                if (change == "timeline")
                {
                    Check(a.Case.Timeline.Schedule(a.Case.Start, new(new(71), new(1000001), 0, SimulationEventKind.NoOpMarker)).Succeeded, "timeline-only schedule");
                    a.Case.Timeline.Cancel(new(71)); Check(a.Case.Engine.State.Revision == oldState, "timeline-only control");
                }
                else if (change == "clock")
                {
                    a.Case.Engine.AdvanceAndExecuteOneCanonicalGroup(new(a.Case.Start.Ticks + 1));
                    Check(a.Case.Engine.State.Revision == oldState && a.Case.Timeline.Revision == oldTimeline, "clock-only control");
                }
                else if (change == "torque")
                {
                    var replacement = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(a.Case.Engine.State, new SpacecraftTorqueCommand(Craft, new(0, 1, 0), a.Case.Start));
                    Check(replacement.Succeeded && a.Case.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed, "torque source change");
                }
                else if (change is "mass" or "inertia")
                {
                    // Fixture-local fault injection only, with guaranteed restoration. No production API added.
                    var store = typeof(NovaCore.Simulation.Transactions.SimulationState).GetField("_spacecraft", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(a.Case.State)!;
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
                    Check(a.Case.Engine.State.Revision == oldState && a.Case.Timeline.Revision == oldTimeline, "direct frozen-property control");
                }
                else
                {
                    var request = new SimulationEventRequest(new(91), a.Case.Start, 0, SimulationEventKind.Marker);
                    if (change == "force") Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91), 0, new(Craft, a.Case.Start, new(2, 0, 0)), out request), "force intent");
                    Check(a.Case.Timeline.Schedule(a.Case.Start, request).Succeeded && a.Case.Engine.ExecuteCanonicalPendingEvent().Committed, "source state transaction");
                }
                Check(a.Read(ar, out var value) == CertifiedPostImpactVelocityStatus.Stale && value == default && a.Qualify().Status == CertifiedPostImpactVelocityStatus.Stale,
                    change + " invalidates qualification/read");
            }
            finally { restore(); }
            if (change is "mass" or "inertia") Check(a.Read(ar, out _) == CertifiedPostImpactVelocityStatus.Qualified, "fault injection restored");
        }
    }

    private static void Measure(string name, Func<bool> work, bool requireZero = true)
    {
        for (var i = 0; i < 32; i++) Check(work(), name + " warmup");
        var ticks = new long[101];
        for (var i = 0; i < ticks.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); var completed = work(); ticks[i] = Stopwatch.GetTimestamp() - start;
            Check(completed, name + " timing completion");
        }
        Array.Sort(ticks); var completedCalls = 0;
        using var measurement = new OrdinaryAllocationMeasurement("certified post-impact " + name);
        for (var i = 0; i < 8; i++) if (work()) completedCalls++;
        var bytes = measurement.Complete();
        if (requireZero) OrdinaryAllocationMeasurement.RequireZero(bytes, name);
        Check(completedCalls == 8, name + " measured completion");
        double Nanoseconds(int index) => ticks[index] * 1e9 / Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "postimpact-cost", name, warmups = 32, timingSamples = 101, measuredCalls = 8,
            bytes, zeroAllocationContract = requireZero, entry = "PASS", exit = "PASS", medianNs = Nanoseconds(50), p95Ns = Nanoseconds(95),
            p99Ns = Nanoseconds(99), maximumNs = Nanoseconds(100), timerResolutionNs = 1e9 / Stopwatch.Frequency }));
    }
}
