using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>
/// Specialized numerical calculation, NOT a receipt issuer. Its physical premises (unit normal,
/// exact-normalized attitude, zero pre-spin, linked exact law and same alpha) come only from checked inputs.
/// Version 1: banked outward primitive operations, unfused half-sum seed, 64 outward corners.
/// No heap workspace, root evaluations/refinements, state mutation or impulse application.
/// </summary>
internal static class CertifiedPostImpactVelocityMath
{
    internal const int MaximumCandidates = 65;

    // Runtime identity cannot change during this process. Environment.Version creates a Version
    // object on every access, so qualify this immutable predicate once at the arithmetic owner's
    // initialization boundary. Thread arithmetic capabilities remain checked on every call below.
    private static readonly bool RuntimeVersionSupported = CheckRuntimeVersion();

    private static bool CheckRuntimeVersion()
    {
        var version = Environment.Version;
        return version.Major == 10 && version.Minor == 0 && version.Build == 12;
    }

    // Explicit call/return rounding boundaries for the center and runtime witnesses. There is no
    // multiply-add expression for the JIT to contract. FloridaBound likewise observes each rounded
    // primitive via adjacent-bit expansion before its result participates in the next operation.
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Add(double x, double y) => x + y;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Divide(double x, double y) => x / y;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Multiply(double x, double y) => x * y;

    internal static bool ArithmeticSupported()
    {
        // This first arithmetic version is qualified on the banked Desktop runtime, not a promise
        // about arbitrary future JITs/architectures. Do not change any thread/global arithmetic mode.
        return OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64 &&
            RuntimeVersionSupported &&
            VerifyArithmetic(1, Math.ScaleB(1, -1022), double.Epsilon, Math.ScaleB(1, -53),
                1 + Math.ScaleB(1, -27), 1 - Math.ScaleB(1, -27));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VerifyArithmetic(double one, double normal, double epsilon, double halfUlp, double a, double b) =>
        BitConverter.DoubleToUInt64Bits(Add(one, halfUlp)) == 0x3ff0000000000000UL &&
        BitConverter.DoubleToUInt64Bits(Add(Math.BitIncrement(one), halfUlp)) == 0x3ff0000000000002UL &&
        // Integer bit comparisons cannot themselves flush the expected subnormal to zero under DAZ.
        BitConverter.DoubleToUInt64Bits(Divide(normal, 2)) == 0x0008000000000000UL &&
        BitConverter.DoubleToUInt64Bits(Add(epsilon, epsilon)) == 2UL &&
        BitConverter.DoubleToUInt64Bits(Add(Multiply(a, b), -one)) == 0;

    private readonly record struct Pair(Double3 Linear, Double3 Angular);
    private readonly record struct Prepared(FloridaContactKinematics Input, CertifiedResponseValues Law,
        CertifiedPreImpactVelocityValues Before, FloridaVector NormalLever, FloridaVector LinearTarget,
        FloridaVector AngularTarget, FloridaVector RelativeTarget, FloridaBound Dissipation,
        CertifiedPostImpactVelocityBudgets Budgets);

    internal static CertifiedPostImpactVelocityFailure Select(in FloridaContactKinematics input,
        in CertifiedResponseValues response, in CertifiedPreImpactVelocityValues preimpact,
        out CertifiedPostImpactVelocityValues values)
    {
        values = default;
        if (!ArithmeticSupported()) return CertifiedPostImpactVelocityFailure.UnsupportedArithmetic;
        var failure = Prepare(input, response, preimpact, out var p, out var down, out var up, out var center);
        if (failure != CertifiedPostImpactVelocityFailure.None) return failure;
        var evaluated = 0; var admitted = 0; var found = false;
        for (var id = 0; id < MaximumCandidates; id++)
        {
            var candidate = id == 0 ? center : Corner(down, up, id - 1);
            // Each down_i < up_i, so corners are unique. Only the center can duplicate a corner.
            if (id != 0 && SameBits(candidate, center)) continue;
            evaluated++;
            if (!Certify(p, candidate, id, out var next)) continue;
            admitted++;
            if (!found || Better(next, values)) { values = next; found = true; }
        }
        if (!found) return CertifiedPostImpactVelocityFailure.NoAdmissibleCandidate;
        values = values with { CandidatesEvaluated = evaluated, AdmissibleCandidates = admitted };
        return CertifiedPostImpactVelocityFailure.None;
    }

    private static CertifiedPostImpactVelocityFailure Prepare(in FloridaContactKinematics input,
        in CertifiedResponseValues law, in CertifiedPreImpactVelocityValues before, out Prepared p,
        out Pair down, out Pair up, out Pair center)
    {
        p = default; down = up = center = default;
        var m = input.MassKilograms; var inertia = input.PrincipalInertia;
        if (!double.IsFinite(m) || m <= 0 || !inertia.IsFinite || !inertia.IsStrictlyPositive ||
            !input.FixedAttitude.IsFinite || !input.AuthoredLeverBodyMetres.IsFinite ||
            !input.NormalRoot.IsFinite || !input.NormalVelocityMetresPerSecond.IsFinite ||
            input.NormalVelocityMetresPerSecond.Upper >= 0 || !before.ComVelocityRoot.IsFinite ||
            !before.RelativeVelocityRoot.IsFinite || !law.EffectiveInverseMass.IsFinite || law.EffectiveInverseMass.Lower <= 0 ||
            !law.ScalarImpulse.IsFinite || law.ScalarImpulse.Lower <= 0 || !law.LinearImpulseRoot.IsFinite || !law.AngularImpulseBody.IsFinite)
            return CertifiedPostImpactVelocityFailure.InvalidNumericalInput;

        var q = input.FixedAttitude;
        var a = FloridaVector.Cross(FloridaVector.From(input.AuthoredLeverBodyMetres), InverseRotate(q, input.NormalRoot));
        var delta = law.LinearImpulseRoot / m;
        var tv = before.ComVelocityRoot + delta;
        var tw = new FloridaVector(law.AngularImpulseBody.X / inertia.X, law.AngularImpulseBody.Y / inertia.Y,
            law.AngularImpulseBody.Z / inertia.Z);
        var relative = before.RelativeVelocityRoot + delta;
        var dissipation = input.NormalVelocityMetresPerSecond.Square() / (2 * law.EffectiveInverseMass);
        if (!a.IsFinite || !tv.IsFinite || !tw.IsFinite || !relative.IsFinite || !dissipation.IsFinite)
            return CertifiedPostImpactVelocityFailure.NonFinite;
        down = new(Endpoints(tv, false), Endpoints(tw, false));
        up = new(Endpoints(tv, true), Endpoints(tw, true));
        center = new(Center(tv), Center(tw));
        if (!Finite(down) || !Finite(up) || !Finite(center)) return CertifiedPostImpactVelocityFailure.NonFinite;

        var bv = Errors(tv, down.Linear, up.Linear, center.Linear);
        var bw = Errors(tw, down.Angular, up.Angular, center.Angular);
        var v2 = FloridaVector.From(bv).NormSquared;
        var w2 = WeightedSquare(FloridaVector.From(bw), input);
        var r2 = m * v2 + w2;
        var brho = DotMagnitudes(input.NormalRoot, bv) + DotMagnitudes(a, bw);
        var bt = m * v2.Sqrt();
        var bc = ScaleInertia(FloridaVector.From(bw), input).Norm +
            m * FloridaVector.From(input.AuthoredLeverBodyMetres).Norm * v2.Sqrt();
        var be = m * DotMagnitudes(relative, bv) + DotMagnitudes(law.AngularImpulseBody, bw) + r2 / 2;
        if (!bv.IsFinite || !bw.IsFinite || !r2.IsFinite || !brho.IsFinite || !bt.IsFinite || !bc.IsFinite || !be.IsFinite)
            return CertifiedPostImpactVelocityFailure.NonFinite;
        var budgets = new CertifiedPostImpactVelocityBudgets(bv, bw, brho.Upper, r2.Upper, bt.Upper, bc.Upper,
            be.Upper, -input.NormalVelocityMetresPerSecond.Upper, dissipation.Lower);
        // Downward threshold for 2*dLower keeps the strict real inequality sound even at underflow.
        var responseSquaredLower = ((FloridaBound)dissipation.Lower * 2).Lower;
        if (dissipation.Lower <= 0 || brho.Upper >= budgets.IncomingSpeedLower || be.Upper >= dissipation.Lower ||
            r2.Upper >= responseSquaredLower) return CertifiedPostImpactVelocityFailure.UnresolvedResponse;
        p = new(input, law, before, a, tv, tw, relative, dissipation, budgets);
        return CertifiedPostImpactVelocityFailure.None;
    }

    private static bool Certify(in Prepared p, in Pair pair, int id, out CertifiedPostImpactVelocityValues value)
    {
        value = default;
        var ev = FloridaVector.From(pair.Linear) - p.LinearTarget;
        var ew = FloridaVector.From(pair.Angular) - p.AngularTarget;
        var b = p.Budgets; var m = p.Input.MassKilograms;
        if (!ev.IsFinite || !ew.IsFinite || !Within(ev, b.LinearComponent) || !Within(ew, b.AngularComponent)) return false;
        var dp = ev * m; var dl = ScaleInertia(ew, p.Input);
        var peff = (FloridaVector.From(pair.Linear) - p.Before.ComVelocityRoot) * m;
        var leff = ScaleInertia(FloridaVector.From(pair.Angular), p.Input);
        var rho = FloridaVector.Dot(p.Input.NormalRoot, ev) + FloridaVector.Dot(p.NormalLever, ew);
        var tangent = dp - p.Input.NormalRoot * FloridaVector.Dot(p.Input.NormalRoot, dp);
        var coupling = dl - FloridaVector.Cross(FloridaVector.From(p.Input.AuthoredLeverBodyMetres),
            InverseRotate(p.Input.FixedAttitude, dp));
        var weighted = m * ev.NormSquared + WeightedSquare(ew, p.Input);
        var defect = m * FloridaVector.Dot(p.RelativeTarget, ev) + FloridaVector.Dot(p.Law.AngularImpulseBody, ew) + weighted / 2;
        if (!dp.IsFinite || !dl.IsFinite || !peff.IsFinite || !leff.IsFinite || !tangent.IsFinite || !coupling.IsFinite)
            return false;
        // These are intersections of two INDEPENDENT mathematical enclosures: direct interval algebra
        // and the a priori component-error triangle/contraction bounds. Never clip to a desired sign.
        if (!Intersect(rho, new(-b.NormalResidual, b.NormalResidual), out rho) ||
            !Intersect(tangent.Norm, new(0, b.Tangential), out var tangentNorm) ||
            !Intersect(coupling.Norm, new(0, b.Coupling), out var couplingNorm) ||
            !Intersect(weighted, new(0, b.MassWeightedError), out weighted) ||
            !Intersect(defect, new(-b.EnergyDefect, b.EnergyDefect), out defect)) return false;
        var energy = -p.Dissipation + defect;
        if (!energy.IsFinite || rho.Lower < 0 || energy.Upper > 0) return false;
        value = new(pair.Linear, pair.Angular, p.LinearTarget, p.AngularTarget, ev, ew, peff, leff, dp, dl,
            rho, energy, defect, tangentNorm.Upper, couplingNorm.Upper, weighted.Upper, b, id, 0, 0);
        return true;
    }

    private static FloridaVector InverseRotate(DoubleQuaternion attitude, FloridaVector value)
    {
        // Same EXACT normalization meaning as the banked response law. No rounded quaternion chosen.
        var q = -FloridaVector.From(new(attitude.X, attitude.Y, attitude.Z));
        FloridaBound w = attitude.W;
        var normSquared = q.NormSquared + w.Square();
        if (!normSquared.IsFinite || normSquared.Lower <= 0) return new(new(double.NaN, double.NaN), default, default);
        var twice = FloridaVector.Cross(q, value) * 2;
        return value + (twice * w + FloridaVector.Cross(q, twice)) / normSquared;
    }

    private static FloridaVector ScaleInertia(FloridaVector v, in FloridaContactKinematics input) =>
        new(v.X * input.PrincipalInertia.X, v.Y * input.PrincipalInertia.Y, v.Z * input.PrincipalInertia.Z);
    private static FloridaBound WeightedSquare(FloridaVector v, in FloridaContactKinematics input) =>
        v.X.Square() * input.PrincipalInertia.X + v.Y.Square() * input.PrincipalInertia.Y + v.Z.Square() * input.PrincipalInertia.Z;
    private static FloridaBound DotMagnitudes(FloridaVector v, Double3 b) =>
        (FloridaBound)v.X.Magnitude * b.X + (FloridaBound)v.Y.Magnitude * b.Y + (FloridaBound)v.Z.Magnitude * b.Z;
    private static bool Intersect(FloridaBound x, FloridaBound y, out FloridaBound result)
    {
        result = default;
        if (!x.IsFinite || !y.IsFinite) return false;
        result = new(Math.Max(x.Lower, y.Lower), Math.Min(x.Upper, y.Upper));
        return result.IsFinite;
    }
    private static bool Within(FloridaVector v, Double3 bound) =>
        v.X.Magnitude <= bound.X && v.Y.Magnitude <= bound.Y && v.Z.Magnitude <= bound.Z;
    private static double Zero(double x) => x == 0 ? 0d : x;
    private static double Endpoint(FloridaBound x, bool upper) => Zero(upper ? Math.BitIncrement(x.Upper) : Math.BitDecrement(x.Lower));
    private static Double3 Endpoints(FloridaVector v, bool upper) => new(Endpoint(v.X, upper), Endpoint(v.Y, upper), Endpoint(v.Z, upper));
    private static double Center(FloridaBound x) => Zero(Add(Divide(x.Lower, 2), Divide(x.Upper, 2)));
    private static Double3 Center(FloridaVector v) => new(Center(v.X), Center(v.Y), Center(v.Z));
    private static double Error(FloridaBound target, double down, double up, double center) =>
        Math.Max(Math.Max((FloridaBound.Point(down) - target).Magnitude, (FloridaBound.Point(up) - target).Magnitude),
            (FloridaBound.Point(center) - target).Magnitude);
    private static Double3 Errors(FloridaVector target, Double3 down, Double3 up, Double3 center) =>
        new(Error(target.X, down.X, up.X, center.X), Error(target.Y, down.Y, up.Y, center.Y), Error(target.Z, down.Z, up.Z, center.Z));
    private static bool Finite(Pair p) => p.Linear.IsFinite && p.Angular.IsFinite;
    private static Pair Corner(Pair down, Pair up, int mask) => new(
        new((mask & 1) == 0 ? down.Linear.X : up.Linear.X, (mask & 2) == 0 ? down.Linear.Y : up.Linear.Y, (mask & 4) == 0 ? down.Linear.Z : up.Linear.Z),
        new((mask & 8) == 0 ? down.Angular.X : up.Angular.X, (mask & 16) == 0 ? down.Angular.Y : up.Angular.Y, (mask & 32) == 0 ? down.Angular.Z : up.Angular.Z));
    private static bool SameBits(Pair a, Pair b) => CompareBits(a.Linear, b.Linear) == 0 && CompareBits(a.Angular, b.Angular) == 0;
    // Deliberate total order of canonical finite IEEE bit patterns; not a floating numerical ordering.
    private static int CompareBits(Double3 a, Double3 b)
    {
        var c = BitConverter.DoubleToUInt64Bits(a.X).CompareTo(BitConverter.DoubleToUInt64Bits(b.X));
        if (c != 0) return c;
        c = BitConverter.DoubleToUInt64Bits(a.Y).CompareTo(BitConverter.DoubleToUInt64Bits(b.Y));
        return c != 0 ? c : BitConverter.DoubleToUInt64Bits(a.Z).CompareTo(BitConverter.DoubleToUInt64Bits(b.Z));
    }
    private static bool Better(in CertifiedPostImpactVelocityValues a, in CertifiedPostImpactVelocityValues b)
    {
        if (a.NormalResidual.Upper != b.NormalResidual.Upper) return a.NormalResidual.Upper < b.NormalResidual.Upper;
        if (a.MassWeightedErrorBound != b.MassWeightedErrorBound) return a.MassWeightedErrorBound < b.MassWeightedErrorBound;
        var c = CompareBits(a.LinearVelocityRoot, b.LinearVelocityRoot);
        return c < 0 || (c == 0 && CompareBits(a.AngularVelocityBody, b.AngularVelocityBody) < 0);
    }
}
