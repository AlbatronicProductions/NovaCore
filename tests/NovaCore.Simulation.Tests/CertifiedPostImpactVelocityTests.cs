using System.Numerics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using static CertifiedResponseOracle;

internal static class CertifiedPostImpactVelocityTests
{
    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException("Certified post-impact velocity: " + name); }

    private readonly record struct Fixture(V Normal, R Speed, V Before, V Relative, Double3 Lever,
        DoubleQuaternion Attitude, double Mass, PrincipalMomentsOfInertia Inertia);
    private readonly record struct Exact(V IdealV, V IdealW, V Ev, V Ew, V P, V L, V DP, V DL,
        V Tangent, V Coupling, R Rho, R R2, R Energy, R IdealEnergy, R EnergyDefect);

    internal static void Run()
    {
        Check(CertifiedPostImpactVelocityMath.MaximumCandidates == 65, "fixed recipe limit");
        Check(CertifiedPostImpactVelocityMath.ArithmeticSupported(), "required binary64 arithmetic environment");
        ArithmeticControls();
        RunFixture(new(new(1, 0, 0), -3, new(14, -5, 7), new(-3, 2, -1), Double3.Zero,
            DoubleQuaternion.Identity, 2, new(2, 3, 4)), "central moving surface");
        var third = new Fixture(new(0, 1, 0), -1, new(0, -1, 0), new(0, -1, 0), Double3.UnitX,
            DoubleQuaternion.Identity, 1, new(1, 1, .5));
        var thirdValue = RunFixture(third, "one third final state");
        var thirdLaw = Solve(third.Normal, third.Speed, third.Lever, third.Attitude, third.Mass, third.Inertia);
        Check(thirdLaw.K == 3 && thirdLaw.J == new R(1, 3), "independent one-third fixture law");
        var naiveJ = 1d / 3; var naiveV = -1d + naiveJ; var naiveW = naiveJ / .5;
        Check(R.From(naiveV) + R.From(naiveW) == new R(-1, BigInteger.One << 53), "naive actual final values remain approaching");
        Check(thirdValue.NormalResidual.Lower >= 0 && Evaluate(third, thirdValue.LinearVelocityRoot, thirdValue.AngularVelocityBody).Rho >= 0,
            "selected final values replace the rejected negative-residual state");
        Check(thirdValue.LinearVelocityRoot.X >= 0 && thirdValue.LinearVelocityRoot.Z >= 0 &&
            thirdValue.AngularVelocityBody.X >= 0 && thirdValue.AngularVelocityBody.Y >= 0,
            "symmetric inactive-component ties use canonical unsigned-bit order");

        var n = new V(new(3, 5), new(4, 5), 0);
        var u = (R)(-2); var tangent = new V(new(-4, 5), new(3, 5), 1);
        RunFixture(new(n, u, new(32, -8, 3), n * u + tangent, new(1, -2, 3),
            new(0, 0, .6, .8), 8, new(2, 3, 4)), "rational unit normal and stored-bit attitude");
        RunFixture(new(new(1, 0, 0), new(-3, 2), new(32, -8, 3), new(new(-3, 2), 2, -1), new(1, -2, 3),
            new(0, 0, .6, .8), 2, new(2, 3, 5)), "asymmetric all-component angular response");
        RunFixture(new(new(1, 0, 0), -1, new(3, 0, 0), new(-1, 0, 0), Double3.Zero,
            DoubleQuaternion.Identity, 1, new(1, 1, 1)), "surface performs positive inertial work");
        RunFixture(new(new(1, 0, 0), -1, new(32768, new(1, 1024), -128), new(-1, 2, -3), new(1, 2, 3),
            new(.5, .5, .5, .5), 2, new(2, 3, 5)), "unequal component scales");
        Refusals(); Exhaustion();
        Console.WriteLine("CERTIFIED_POSTIMPACT_ANALYTICAL exact-rational/final-state/65-candidates/energy/momentum/rounding/refusal PASS");
    }

    private static CertifiedPostImpactVelocityValues RunFixture(Fixture f, string name)
    {
        Check(V.Dot(f.Normal, f.Normal) == 1 && V.Dot(f.Normal, f.Relative) == f.Speed, name + " independent physical inputs");
        Inputs(f, out var input, out var response, out var preimpact);
        var failure = CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out var actual);
        Check(failure == CertifiedPostImpactVelocityFailure.None, name + " selection: " + failure);
        Verify(f, input, response, preimpact, actual, name);
        for (var i = 0; i < 3; i++)
        {
            Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out var again) == CertifiedPostImpactVelocityFailure.None && actual == again,
                name + " deterministic complete result replay");
        }
        Console.WriteLine($"POSTIMPACT_ANALYTICAL {name}: candidate={actual.CandidateIndex}; evaluated={actual.CandidatesEvaluated}; admitted={actual.AdmissibleCandidates}; rho=[{actual.NormalResidual.Lower:R},{actual.NormalResidual.Upper:R}]");
        return actual;
    }

    private static void Inputs(Fixture f, out FloridaContactKinematics input, out CertifiedResponseValues response,
        out CertifiedPreImpactVelocityValues preimpact)
    {
        var law = Solve(f.Normal, f.Speed, f.Lever, f.Attitude, f.Mass, f.Inertia);
        input = new(default, new(0, 1), Enclose(f.Normal), Enclose(f.Speed), f.Lever, default,
            f.Attitude, f.Mass, f.Inertia, default, 0);
        response = new(Enclose(law.K), Enclose(law.J), Enclose(law.Linear), Enclose(law.Angular));
        preimpact = new(Enclose(f.Before), Enclose(f.Relative));
        // Constructible analytical data exercises only the pure arithmetic kernel, never issues a root receipt.
    }

    private static Exact Evaluate(Fixture f, Double3 velocity, Double3 angular)
    {
        var law = Solve(f.Normal, f.Speed, f.Lever, f.Attitude, f.Mass, f.Inertia);
        var m = R.From(f.Mass); var inertia = Inertia(f.Inertia);
        var vhat = V.From(velocity); var what = V.From(angular);
        var idealV = f.Before + law.Linear * (1 / m);
        var idealW = Divide(law.Angular, inertia);
        var ev = vhat - idealV; var ew = what - idealW;
        // Actual represented changes are calculated directly, before comparing with the exact command.
        var p = (vhat - f.Before) * m; var l = Multiply(inertia, what);
        var dp = p - law.Linear; var dl = l - law.Angular;
        var tangent = dp - f.Normal * V.Dot(f.Normal, dp);
        var coupling = dl - V.Cross(V.From(f.Lever), CertifiedResponseOracle.InverseRotate(f.Attitude, dp));
        var rho = V.Dot(f.Normal, f.Relative) + V.Dot(f.Normal, p) / m + V.Dot(law.A, what);
        var r2 = m * V.Dot(ev, ev) + V.Dot(ew, Multiply(inertia, ew));
        var energy = V.Dot(f.Relative, p) + V.Dot(p, p) / (2 * m) + V.Dot(l, Divide(l, inertia)) / 2;
        var idealEnergy = -f.Speed * f.Speed / (2 * law.K);
        var defect = energy - idealEnergy;
        var stable = m * V.Dot(f.Relative + law.Linear * (1 / m), ev) + V.Dot(law.Angular, ew) + r2 / 2;
        Check(defect == stable, "independent direct effective-work and stable-defect identities");
        Check(rho == V.Dot(f.Normal, ev) + V.Dot(law.A, ew), "independent actual-state and exact-law residual identities");
        return new(idealV, idealW, ev, ew, p, l, dp, dl, tangent, coupling, rho, r2, energy, idealEnergy, defect);
    }

    private static void Verify(Fixture f, FloridaContactKinematics input, CertifiedResponseValues response,
        CertifiedPreImpactVelocityValues preimpact, CertifiedPostImpactVelocityValues actual, string name)
    {
        var x = Evaluate(f, actual.LinearVelocityRoot, actual.AngularVelocityBody);
        Check(actual.LinearVelocityRoot.IsFinite && actual.AngularVelocityBody.IsFinite, name + " finite selected vectors");
        Check(Contains(actual.IdealLinearVelocityRoot, x.IdealV) && Contains(actual.IdealAngularVelocityBody, x.IdealW), name + " exact ideal final targets");
        Check(Contains(actual.LinearError, x.Ev) && Contains(actual.AngularError, x.Ew), name + " final component errors");
        Check(Contains(actual.EffectiveLinearMomentumRoot, x.P) && Contains(actual.EffectiveAngularMomentumBody, x.L), name + " actual effective momentum");
        Check(Contains(actual.LinearMomentumDefectRoot, x.DP) && Contains(actual.AngularMomentumDefectBody, x.DL), name + " effective momentum defects");
        Check(Contains(actual.NormalResidual, x.Rho) && x.Rho >= 0 && actual.NormalResidual.Lower >= 0, name + " actual final normal nonapproach");
        Check(Contains(actual.WorkAdjustedEnergy, x.Energy) && actual.WorkAdjustedEnergy.Upper <= 0 && x.Energy <= 0, name + " effective-momentum surface work");
        Check(Contains(actual.EnergyDefect, x.EnergyDefect), name + " defect from exact inelastic energy");
        BoundsSquared(actual.TangentialBound, V.Dot(x.Tangent, x.Tangent), name + " tangential contamination");
        BoundsSquared(actual.CouplingBound, V.Dot(x.Coupling, x.Coupling), name + " paired angular/linear coupling");
        Check(double.IsFinite(actual.MassWeightedErrorBound) && R.From(actual.MassWeightedErrorBound) >= x.R2, name + " mass-metric state error");
        var beforeEnergy = R.From(f.Mass) * V.Dot(f.Before, f.Before) / 2;
        var afterEnergy = R.From(f.Mass) * V.Dot(V.From(actual.LinearVelocityRoot), V.From(actual.LinearVelocityRoot)) / 2 +
            V.Dot(V.From(actual.AngularVelocityBody), Multiply(Inertia(f.Inertia), V.From(actual.AngularVelocityBody))) / 2;
        Check(afterEnergy - beforeEnergy - V.Dot(f.Before - f.Relative, x.P) == x.Energy, name + " spacecraft energy minus actual material work");
        if (name == "surface performs positive inertial work") Check(afterEnergy > beforeEnergy, "moving surface may increase inertial spacecraft energy");
        if (name == "asymmetric all-component angular response")
            Check(actual.AngularVelocityBody.X != 0 && actual.AngularVelocityBody.Y != 0 && actual.AngularVelocityBody.Z != 0, "all physical angular components retained");
        VerifyRecipeAndBudgets(input, response, preimpact, actual, name);
    }

    private static void VerifyRecipeAndBudgets(FloridaContactKinematics input, CertifiedResponseValues response,
        CertifiedPreImpactVelocityValues preimpact, CertifiedPostImpactVelocityValues result, string name)
    {
        var v = Components(preimpact.ComVelocityRoot); var j = Components(response.LinearImpulseRoot);
        var l = Components(response.AngularImpulseBody); var inertia = new[] { input.PrincipalInertia.X, input.PrincipalInertia.Y, input.PrincipalInertia.Z };
        var target = new FloridaBound[6];
        for (var i = 0; i < 3; i++) { target[i] = GraphAdd(v[i], GraphDivide(j[i], input.MassKilograms)); target[i + 3] = GraphDivide(l[i], inertia[i]); }
        Check(Components(result.IdealLinearVelocityRoot).Concat(Components(result.IdealAngularVelocityBody)).SequenceEqual(target), name + " declared outward target operation graph");
        var down = target.Select(t => Canonical(Math.BitDecrement(t.Lower))).ToArray();
        var up = target.Select(t => Canonical(Math.BitIncrement(t.Upper))).ToArray();
        var center = target.Select(t => Canonical(Add(Divide(t.Lower, 2), Divide(t.Upper, 2)))).ToArray();
        var chosen = Values(result.LinearVelocityRoot).Concat(Values(result.AngularVelocityBody)).ToArray();
        var distinct = new HashSet<string>(); var membership = false; var indexMatches = false;
        for (var index = 0; index < 65; index++)
        {
            var candidate = index == 0 ? center : Enumerable.Range(0, 6).Select(i => ((index - 1) & (1 << i)) == 0 ? down[i] : up[i]).ToArray();
            distinct.Add(BitKey(candidate)); membership |= BitKey(candidate) == BitKey(chosen);
            if (index == result.CandidateIndex) indexMatches = BitKey(candidate) == BitKey(chosen);
        }
        Check(membership && indexMatches && result.CandidateIndex is >= 0 and < 65, name + " selected identity/bits belong to fixed finite set");
        Check(result.CandidatesEvaluated == distinct.Count && result.CandidatesEvaluated <= 65 &&
            result.AdmissibleCandidates > 0 && result.AdmissibleCandidates <= result.CandidatesEvaluated, name + " exact deduplicated candidate-count bound");
        var bv = Values(result.Budgets.LinearComponent).Select(R.From).ToArray();
        var bw = Values(result.Budgets.AngularComponent).Select(R.From).ToArray();
        for (var i = 0; i < 6; i++)
        {
            var b = i < 3 ? bv[i] : bw[i - 3];
            foreach (var option in new[] { down[i], up[i], center[i] })
                Check(b >= Abs(R.From(option) - R.From(target[i].Lower)) && b >= Abs(R.From(option) - R.From(target[i].Upper)), name + " a priori complete component budget");
        }
        var n = Components(input.NormalRoot).Select(B.From).ToArray();
        var a = Cross(Values(input.AuthoredLeverBodyMetres).Select(x => B.Point(R.From(x))).ToArray(), InverseRotate(input.FixedAttitude, n));
        var m = R.From(input.MassKilograms);
        R brho = 0, r2 = 0, energy = 0, linearSquares = 0, angularMomentumSquares = 0;
        for (var i = 0; i < 3; i++)
        {
            brho += Magnitude(n[i]) * bv[i] + Magnitude(a[i]) * bw[i];
            linearSquares += bv[i] * bv[i];
            angularMomentumSquares += R.From(inertia[i]) * R.From(inertia[i]) * bw[i] * bw[i];
            r2 += m * bv[i] * bv[i] + R.From(inertia[i]) * bw[i] * bw[i];
            energy += m * Magnitude(B.From(Components(preimpact.RelativeVelocityRoot)[i]) + B.From(j[i]) / B.Point(m)) * bv[i] + Magnitude(B.From(l[i])) * bw[i];
        }
        energy += r2 / 2;
        Check(R.From(result.Budgets.NormalResidual) >= brho && R.From(result.Budgets.MassWeightedError) >= r2 &&
            R.From(result.Budgets.EnergyDefect) >= energy, name + " independent a priori scalar budgets");
        BoundsSquared(result.Budgets.Tangential, m * m * linearSquares, name + " prior tangential budget");
        var coupledLinear = m * m * V.Dot(V.From(input.AuthoredLeverBodyMetres), V.From(input.AuthoredLeverBodyMetres)) * linearSquares;
        var c2 = R.From(result.Budgets.Coupling) * R.From(result.Budgets.Coupling);
        var surplus = c2 - angularMomentumSquares - coupledLinear;
        Check(surplus >= 0 && surplus * surplus >= 4 * angularMomentumSquares * coupledLinear, name + " independent sum-of-norms coupling budget");
        var dissipation = B.From(input.NormalVelocityMetresPerSecond).Square() / (B.Point(2) * B.From(response.EffectiveInverseMass));
        Check(result.Budgets.IncomingSpeedLower > 0 && result.Budgets.DissipationLower > 0 &&
            R.From(result.Budgets.IncomingSpeedLower) <= -R.From(input.NormalVelocityMetresPerSecond.Upper) &&
            R.From(result.Budgets.DissipationLower) <= dissipation.L, name + " conservative physical resolution scales");
        Check(result.Budgets.NormalResidual < result.Budgets.IncomingSpeedLower && result.Budgets.EnergyDefect < result.Budgets.DissipationLower &&
            R.From(result.Budgets.MassWeightedError) < 2 * R.From(result.Budgets.DissipationLower), name + " readiness remains independent of selection");
        Check(result.NormalResidual.Upper <= result.Budgets.NormalResidual && result.TangentialBound <= result.Budgets.Tangential &&
            result.CouplingBound <= result.Budgets.Coupling && result.MassWeightedErrorBound <= result.Budgets.MassWeightedError &&
            Math.Max(Math.Abs(result.EnergyDefect.Lower), Math.Abs(result.EnergyDefect.Upper)) <= result.Budgets.EnergyDefect, name + " actual certificate respects all hard ceilings");
    }

    private static void Refusals()
    {
        var f = new Fixture(new(1, 0, 0), -1, new(new R(BigInteger.One << 54, 1), 0, 0), new(-1, 0, 0), Double3.Zero,
            DoubleQuaternion.Identity, 1, new(1, 1, 1));
        Inputs(f, out var input, out var response, out var preimpact);
        Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out _) == CertifiedPostImpactVelocityFailure.UnresolvedResponse,
            "lost-unit response cannot pass merely by choosing a large separating corner");
        MeasureRefusal("response resolution", input, response, preimpact, CertifiedPostImpactVelocityFailure.UnresolvedResponse);
        Inputs(f with { Before = new(3, 0, 0) }, out input, out response, out preimpact);
        var broad = preimpact with { ComVelocityRoot = new(new(-100, 100), new(-100, 100), new(-100, 100)) };
        Check(CertifiedPostImpactVelocityMath.Select(input, response, broad, out _) == CertifiedPostImpactVelocityFailure.UnresolvedResponse, "broad source refused before selection");
        Check(CertifiedPostImpactVelocityMath.Select(default, response, preimpact, out _) != CertifiedPostImpactVelocityFailure.None, "invalid raw numerical input refused");
        Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact with { ComVelocityRoot = new(new(double.NaN, 1), 0, 0) }, out _) !=
            CertifiedPostImpactVelocityFailure.None, "nonfinite input refused");
        Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact with { ComVelocityRoot = new(double.MaxValue, 0, 0) }, out _) !=
            CertifiedPostImpactVelocityFailure.None, "required outward endpoint overflow refused");
    }

    private static void Exhaustion()
    {
        // A valid linked unit-normal family, n(t)=(2t,1-t^2,0)/(1+t^2), |t|<=1/100.
        // Every member has u=-1,k=j=1,V=0,W=-n,J=n,L=0. The broad independent boxes lose
        // the normal/target correlation. A fixed recipe must refuse rather than grow another neighborhood.
        var edge = new R(200, 10001); var y = new R(9999, 10001);
        var nx = new FloridaBound(Enclose(-edge).Lower, Enclose(edge).Upper);
        var ny = new FloridaBound(Enclose(y).Lower, 1); var normal = new FloridaVector(nx, ny, 0);
        var input = new FloridaContactKinematics(default, new(0, 1), normal, -1, Double3.Zero, default,
            DoubleQuaternion.Identity, 1, new(1, 1, 1), default, 0);
        var response = new CertifiedResponseValues(1, 1, normal, default);
        var preimpact = new CertifiedPreImpactVelocityValues(default, new(new(-nx.Upper, -nx.Lower), new(-ny.Upper, -ny.Lower), 0));
        Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out _) == CertifiedPostImpactVelocityFailure.NoAdmissibleCandidate,
            "finite candidate exhaustion remains explicit");
        MeasureRefusal("fixed-set exhaustion", input, response, preimpact, CertifiedPostImpactVelocityFailure.NoAdmissibleCandidate);
        OrdinaryAllocationMeasurement.PositiveControl();
    }

    private static void MeasureRefusal(string name, FloridaContactKinematics input, CertifiedResponseValues response,
        CertifiedPreImpactVelocityValues preimpact, CertifiedPostImpactVelocityFailure expected)
    {
        // Prebuilt analytical source; fixed normal-runtime timing and separate checked allocation windows.
        for (var i = 0; i < 32; i++) Check(CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out _) == expected, name + " warmup");
        var ticks = new long[101];
        for (var i = 0; i < ticks.Length; i++)
        {
            var start = Stopwatch.GetTimestamp();
            var failure = CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out _);
            ticks[i] = Stopwatch.GetTimestamp() - start;
            Check(failure == expected, name + " timed outcome");
        }
        Array.Sort(ticks); var completed = 0;
        using var measurement = new OrdinaryAllocationMeasurement("post-impact " + name);
        for (var i = 0; i < 8; i++) if (CertifiedPostImpactVelocityMath.Select(input, response, preimpact, out _) == expected) completed++;
        var bytes = measurement.Complete();
        OrdinaryAllocationMeasurement.RequireZero(bytes, name);
        Check(completed == 8, name + " measured outcomes");
        double Nanoseconds(int index) => ticks[index] * 1e9 / Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "postimpact-refusal-cost", name, warmups = 32, timingSamples = 101,
            measuredCalls = 8, bytes, entry = "PASS", exit = "PASS", medianNs = Nanoseconds(50), p95Ns = Nanoseconds(95),
            p99Ns = Nanoseconds(99), maximumNs = Nanoseconds(100), timerResolutionNs = 1e9 / Stopwatch.Frequency }));
    }

    private static void ArithmeticControls()
    {
        var a = 1 + Math.ScaleB(1, -27); var b = 1 - Math.ScaleB(1, -27);
        Check(Add(Multiply(a, b), -1) == 0 && R.From(Math.FusedMultiplyAdd(a, b, -1)) == new R(-1, BigInteger.One << 54),
            "unfused operation graph differs from deliberately fused adversary");
        Check(Divide(double.Epsilon, 2) == 0 && Divide(3 * double.Epsilon, 2) == 2 * double.Epsilon,
            "gradual-underflow ties-to-even");
        Check(Multiply(double.Epsilon, 1) == double.Epsilon && Math.BitIncrement(0d) == double.Epsilon &&
            Math.BitDecrement(0d) == -double.Epsilon, "subnormal outward endpoints are represented");
        Check(BitConverter.DoubleToInt64Bits(Canonical(-0d)) == 0, "canonical exact zero");
    }

    private static FloridaBound Enclose(R x)
    {
        if (x < 0) { var positive = Enclose(-x); return new(-positive.Upper, -positive.Lower); }
        // Fixed at most 63 comparisons; finds adjacent finite binary64 values using exact rationals.
        ulong lo = 0, hi = 0x7fefffffffffffffUL;
        while (lo < hi)
        {
            var mid = lo + (hi - lo + 1) / 2;
            if (R.From(BitConverter.UInt64BitsToDouble(mid)) <= x) lo = mid; else hi = mid - 1;
        }
        var lower = BitConverter.UInt64BitsToDouble(lo);
        return R.From(lower) == x ? new(lower, lower) : new(lower, Math.BitIncrement(lower));
    }
    private static FloridaVector Enclose(V v) => new(Enclose(v.X), Enclose(v.Y), Enclose(v.Z));
    private static V Inertia(PrincipalMomentsOfInertia i) => new(R.From(i.X), R.From(i.Y), R.From(i.Z));
    private static V Multiply(V a, V b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    private static V Divide(V a, V b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    private static R Abs(R x) => x < 0 ? -x : x;
    private static R Magnitude(B x) => Abs(x.L) > Abs(x.H) ? Abs(x.L) : Abs(x.H);
    private static FloridaBound[] Components(FloridaVector v) => [v.X, v.Y, v.Z];
    private static double[] Values(Double3 v) => [v.X, v.Y, v.Z];
    private static void BoundsSquared(double bound, R square, string name) => Check(double.IsFinite(bound) && bound >= 0 && R.From(bound) * R.From(bound) >= square, name);
    private static double Canonical(double value) => value == 0 ? 0 : value;
    private static string BitKey(double[] values) => string.Join(",", values.Select(v => BitConverter.DoubleToUInt64Bits(v).ToString("X16")));

    [MethodImpl(MethodImplOptions.NoInlining)] private static double Add(double a, double b) => a + b;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Multiply(double a, double b) => a * b;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Divide(double a, double b) => a / b;
    private static FloridaBound GraphAdd(FloridaBound a, FloridaBound b) => new(Math.BitDecrement(Add(a.Lower, b.Lower)), Math.BitIncrement(Add(a.Upper, b.Upper)));
    private static FloridaBound GraphDivide(FloridaBound a, double divisor)
    {
        var lo = Math.BitDecrement(Divide(1, divisor)); var hi = Math.BitIncrement(Divide(1, divisor));
        var p = new[] { Multiply(a.Lower, lo), Multiply(a.Lower, hi), Multiply(a.Upper, lo), Multiply(a.Upper, hi) };
        return new(Math.BitDecrement(p.Min()), Math.BitIncrement(p.Max()));
    }
    private static B[] Cross(B[] a, B[] b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
    private static B[] InverseRotate(DoubleQuaternion q, B[] n)
    {
        var x = R.From(q.X); var y = R.From(q.Y); var z = R.From(q.Z); var w = R.From(q.W); var d = x * x + y * y + z * z + w * w;
        R[,] matrix = { { (w*w+x*x-y*y-z*z)/d, 2*(x*y+w*z)/d, 2*(x*z-w*y)/d },
            { 2*(x*y-w*z)/d, (w*w-x*x+y*y-z*z)/d, 2*(y*z+w*x)/d },
            { 2*(x*z+w*y)/d, 2*(y*z-w*x)/d, (w*w-x*x-y*y+z*z)/d } };
        B[] result = [0, 0, 0];
        for (var i = 0; i < 3; i++) for (var j = 0; j < 3; j++) result[i] += B.Point(matrix[i, j]) * n[j];
        return result;
    }
}
