using System.Numerics;
using System.Reflection;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class PoweredContactTests
{
    // Test-only observation of private state; production exposes no native body capability.
    private static (Simulation Simulation, BodyHandle Body) Native(Contact c) =>
        ((Simulation)typeof(LocalContactWorld).GetField("simulation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(c.World)!,
         (BodyHandle)typeof(LocalContactWorld).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(c.World)!);
    private static Double3 Copy(Vector3 v) => new(v.X, v.Y, v.Z);
    private static double Length(Double3 v) => Math.Sqrt(v.LengthSquared);
    private static BigInteger Integer(in PropellantInteger value)
    {
        BigInteger n = 0;
        for (var i = PropellantInteger.LimbCount - 1; i >= 0; --i) n = (n << 64) + value.Limb(i);
        return n;
    }
    private readonly struct Q
    {
        private readonly BigInteger n, d;
        private BigInteger D => d.IsZero ? BigInteger.One : d;
        internal bool IsZero => n.IsZero;
        internal bool Positive => n.Sign > 0;
        internal Q(BigInteger n, BigInteger d)
        {
            if (d.IsZero) throw new DivideByZeroException();
            if (d.Sign < 0) { n = -n; d = -d; }
            var gcd = BigInteger.GreatestCommonDivisor(n, d); this.n = n / gcd; this.d = d / gcd;
        }
        internal static Q Exact(double value)
        {
            Check(double.IsFinite(value), "finite oracle input");
            var bits = BitConverter.DoubleToUInt64Bits(value); var e = (int)((bits >> 52) & 2047);
            BigInteger n = bits & 0xfffffffffffffUL;
            if (e != 0) n += BigInteger.One << 52;
            if ((bits >> 63) != 0) n = -n;
            var shift = e == 0 ? -1074 : e - 1075;
            return shift >= 0 ? new(n << shift, 1) : new(n, BigInteger.One << -shift);
        }
        internal double Value
        {
            get
            {
                if (n.IsZero) return 0;
                var a = Math.Max(0, (int)BigInteger.Abs(n).GetBitLength() - 62); var b = Math.Max(0, (int)D.GetBitLength() - 62);
                return n.Sign * Math.ScaleB((double)(BigInteger.Abs(n) >> a) / (double)(D >> b), a - b);
            }
        }
        internal static Q Abs(Q x) => new(BigInteger.Abs(x.n), x.D);
        internal static Q Pow(Q x, int n) { Q y = 1; for (var i = 0; i < n; i++) y *= x; return y; }
        public static implicit operator Q(int x) => new(x, 1);
        public static Q operator +(Q a, Q b) => new(a.n * b.D + b.n * a.D, a.D * b.D);
        public static Q operator -(Q a, Q b) => new(a.n * b.D - b.n * a.D, a.D * b.D);
        public static Q operator -(Q a) => new(-a.n, a.D);
        public static Q operator *(Q a, Q b) => new(a.n * b.n, a.D * b.D);
        public static Q operator /(Q a, Q b) => new(a.n * b.D, a.D * b.n);
    }
    private readonly record struct Reference(double V, double W, double X, double Theta,
        double Normal, double Tangent, double Twist, double Mx, double Mz, double TangentWork, double TwistWork, double Tail);

    // Independent chronological rigid-support oracle. Polynomial integration of 1/(m0-q*t),
    // then dry/wet coast, follows the accepted reference responsibility, not the native mapper.
    // Actual represented wrench, exact-ledger duration, canonical H, and wet successor are inputs.
    // Exact BigInteger fractions are confined to the independent test oracle. Tiny positive h
    // remains positive throughout integration. The certified series tail is added to errors.
    private static Reference Chronological(Contact c, in PropellantSegmentationPreview p)
    {
        var H = new Q(p.Engine.End.Ticks - p.Engine.Start.Ticks, 1_000_000);
        var h = new Q(Integer(p.PoweredDuration.Numerator), Integer(p.PoweredDuration.Denominator) * 1_000_000); var hc = H - h;
        var q = Q.Exact(p.Engine.RequiredMassFlowKilogramsPerSecond);
        var unit = new Q(1, 1_000_000 * (BigInteger.One << 1074));
        var m = 8 + new Q(Integer(p.Resource.RemainingUnits), 1) * unit;
        var mf = 8 + new Q(Integer(p.SuccessorUnits), 1) * unit;
        var orientation = c.Observe.Endpoint.BodyToRoot;
        var force = orientation.Rotate(p.Engine.ProposedForceBodyNewtons);
        var moment = orientation.Rotate(p.Engine.ProposedMomentBodyNewtonMetres);
        var native = Native(c); var body = native.Simulation.Bodies[native.Body];
        Check(Math.Abs(force.Z) < 1e-12, "scalar reference has no material out-of-plane force");
        var v0 = Q.Exact(body.Velocity.Linear.X); var w0 = Q.Exact(body.Velocity.Angular.Y);
        var centered = c.Prepared.Fixture is PoweredContactFixture.CenteredBaseline or PoweredContactFixture.LowThrust or PoweredContactFixture.CenteredEndpoint;
        var yaw = c.Prepared.Fixture is PoweredContactFixture.OffCom or PoweredContactFixture.YawBaseline or PoweredContactFixture.TinyEvent or PoweredContactFixture.ZeroEvent;
        Q k = centered ? 0 : new Q(1, 8); Q twist = yaw ? Q.Exact(Math.Sqrt(1.25)) / 8 : 0;
        Q g = new(981, 100); Q I = 2;
        var delta = q * h / m;
        Check(h.Value >= 0 && h.Value <= H.Value && delta.Value is >= 0 and < 2e-5, "reference domain");
        var v = new Q[19]; v[0] = v0; v[1] = -k * g * h;
        var f = Q.Exact(force.X) + k * Q.Exact(force.Y); var term = f * h / m;
        for (var n = 0; n <= 16; n++) { v[n + 1] += term / (n + 1); term *= delta; }
        var n0 = g * m - Q.Exact(force.Y); var n1 = -g * q * h;
        var w = new[] { w0, (Q.Exact(moment.Y) - twist * n0) * h / I, -twist * n1 * h / (2 * I) };
        static Q Integral(Q[] a) { Q s = 0; for (var i = 0; i < a.Length; i++) s += a[i] / (i + 1); return s; }
        static Q Weighted(Q[] a, Q n0, Q n1)
        { Q s = 0; for (var i = 0; i < a.Length; i++) s += a[i] * (n0 / (i + 1) + n1 / (i + 2)); return s; }
        Q vh = 0, wh = 0; foreach (var x in v) vh += x; foreach (var x in w) wh += x;
        var xcoast = vh * hc - k * g * hc * hc / 2;
        var thetaCoast = wh * hc - twist * g * mf * hc * hc / (2 * I);
        var normal = h * (n0 + n1 / 2) + g * mf * hc;
        var tail = Q.Abs(f) * h / m * Q.Pow(delta, 17) / (18 * (1 - delta));
        return new((vh - k * g * hc).Value, (wh - twist * g * mf * hc / I).Value,
            (h * Integral(v) + xcoast).Value, (h * Integral(w) + thetaCoast).Value, normal.Value, (-k * normal).Value, (-twist * normal).Value,
            (-Q.Exact(moment.X) * h).Value, (new Q(1, 2) * k * normal - Q.Exact(moment.Z) * h).Value,
            (-k * (h * Weighted(v, n0, n1) + g * mf * xcoast)).Value,
            (-twist * (h * Weighted(w, n0, n1) + g * mf * thetaCoast)).Value, tail.Value);
    }

    private sealed class ContactRows
    {
        internal readonly Vector3[] Offsets = new Vector3[4];
        internal readonly float[] Impulses = new float[7];
        internal Vector3 Normal;
    }
    private struct Rows(ContactRows rows) : ISolverContactPrestepAndImpulsesExtractor
    {
        public void ConvexOneBody<TP, TI>(ref TP p, ref TI i)
            where TP : struct, IConvexContactPrestep<TP> where TI : struct, IConvexContactAccumulatedImpulses<TI>
        {
            Check(TP.ContactCount == 4, "four native support rows");
            ref var n = ref TP.GetNormal(ref p); rows.Normal = new(n.X[0], n.Y[0], n.Z[0]);
            for (var j = 0; j < 4; j++)
            {
                ref var contact = ref TP.GetContact(ref p, j);
                rows.Offsets[j] = new(contact.OffsetA.X[0], contact.OffsetA.Y[0], contact.OffsetA.Z[0]);
                rows.Impulses[j] = TI.GetPenetrationImpulseForContact(ref i, j)[0];
            }
            ref var t = ref TI.GetTangentFriction(ref i);
            rows.Impulses[4] = t.X[0]; rows.Impulses[5] = t.Y[0]; rows.Impulses[6] = TI.GetTwistFriction(ref i)[0];
        }
        public void ConvexTwoBody<TP, TI>(ref TP p, ref TI i) where TP : struct, ITwoBodyConvexContactPrestep<TP> where TI : struct, IConvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected contact kind.");
        public void NonconvexOneBody<TP, TI>(ref TP p, ref TI i) where TP : struct, INonconvexContactPrestep<TP> where TI : struct, INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected contact kind.");
        public void NonconvexTwoBody<TP, TI>(ref TP p, ref TI i) where TP : struct, ITwoBodyNonconvexContactPrestep<TP> where TI : struct, INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected contact kind.");
    }
    private static ContactRows ReadRows(Contact c)
    {
        var n = Native(c); var b = n.Simulation.Bodies[n.Body];
        Check(b.Constraints.Count == 1, "one constraint");
        var rows = new ContactRows(); var reader = new Rows(rows);
        Check(n.Simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(b.Constraints[0].ConnectingConstraintHandle, ref reader), "native row observation");
        return rows;
    }
    private static double Angle(DoubleQuaternion q) => 2 * Math.Atan2(Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z), Math.Abs(q.W));

    internal static void Physics()
    {
        foreach (var fixture in Enum.GetValues<PoweredContactFixture>())
        {
            using var c = new Contact(fixture);
            var n = Native(c); var before = n.Simulation.Bodies[n.Body].Pose;
            var handle = n.Simulation.Bodies[n.Body].Constraints[0].ConnectingConstraintHandle;
            var sourcePenetration = c.World.PoweredPenetration();
            var p = c.Seal(); var reference = Chronological(c, p); var initial = c.Observe;
            var committed = c.Apply().Observation;
            var b = n.Simulation.Bodies[n.Body]; var rows = ReadRows(c);
            Helpers.BuildOrthonormalBasis(rows.Normal, out var t1, out var t2);
            var tangent = rows.Impulses[4] * t1 + rows.Impulses[5] * t2;
            double jn = 0; var moment = Vector3.Zero;
            for (var j = 0; j < 4; j++) { jn += rows.Impulses[j]; moment += rows.Impulses[j] * Vector3.Cross(rows.Offsets[j], rows.Normal); }
            var q = b.Pose.Orientation;
            var expected = new DoubleQuaternion(0, Math.Sin(reference.Theta / 2), 0, Math.Cos(reference.Theta / 2));
            var errors = new (string Name, double Value, double Bar)[] {
                ("velocity", Length(Copy(b.Velocity.Linear) - new Double3(reference.V, 0, 0)) + reference.Tail, .06),
                ("angularRate", Length(Copy(b.Velocity.Angular) - new Double3(0, reference.W, 0)), .0489897948557),
                ("position", Length(Copy(b.Pose.Position - before.Position) - new Double3(reference.X, 0, 0)) + reference.Tail, .001),
                ("orientation", Angle(expected.Conjugate() * new DoubleQuaternion(q.X, q.Y, q.Z, q.W)), .000816496581),
                ("normalImpulse", Math.Abs(jn - reference.Normal), .48),
                ("tangentImpulse", Length(Copy(tangent) - new Double3(reference.Tangent, 0, 0)), .48),
                ("twistImpulse", Math.Abs(rows.Impulses[6] - reference.Twist), .0979795897),
                ("supportMoment", Length(Copy(moment) - new Double3(reference.Mx, 0, reference.Mz)), .0979795897),
                ("sourcePenetration", sourcePenetration, .001), ("penetration", c.World.PoweredPenetration(), .001),
                ("sourceOrientation", Angle(new(before.Orientation.X, before.Orientation.Y, before.Orientation.Z, before.Orientation.W)), .000816496581) };
            Console.WriteLine("POWERED_CONTACT_PHYSICS " + fixture + " " + string.Join(" ", errors.Select(e => $"{e.Name}={e.Value:R}")));
            foreach (var e in errors) Check(double.IsFinite(e.Value) && e.Value <= e.Bar, $"{fixture} {e.Name}={e.Value:R} bar={e.Bar:R}");
            Check(handle == b.Constraints[0].ConnectingConstraintHandle && jn > 0 &&
                n.Simulation.Solver.SubstepCount == 1 && n.Simulation.Solver.VelocityIterationCount == 8, "retained native solve ownership");
            Check(committed.Resource.RemainingUnits == p.SuccessorUnits && committed.StateRevision.Value == initial.StateRevision.Value + 1 &&
                committed.HistoryCount == 1 && committed.TimelineRevision == initial.TimelineRevision &&
                committed.Endpoint.Properties.MassKilograms == p.ProposedSuccessorMass.TotalMassKilograms, "physical joint successor");
        }
    }
}
