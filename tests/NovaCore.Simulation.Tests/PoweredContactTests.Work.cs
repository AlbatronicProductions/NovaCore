using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

// Same bounded observation edges as the accepted Method A. Never used in timing/allocation.
internal static class PoweredWorkRecorder
{
    private struct Sample
    {
        internal int Pass, Row, Edge;
        internal BodyVelocityWide Velocity;
        internal BodyInertiaWide Inertia;
        internal Contact4AccumulatedImpulses Impulses;
    }
    private static readonly Sample[] Samples = new Sample[108];
    private static int count, pass;
    internal static void Reset() { count = 0; pass = -1; }
    internal static void Begin(bool warm)
    {
        pass++;
        if (warm != (pass == 0) || pass > 8) throw new InvalidOperationException("Unexpected observed pass.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Record(int row, int edge, in BodyVelocityWide velocity, in BodyInertiaWide inertia,
        in Contact4AccumulatedImpulses impulses)
    {
        if (count >= Samples.Length) throw new InvalidOperationException("Observed capacity exceeded.");
        Samples[count++] = new() { Pass = pass, Row = row, Edge = edge, Velocity = velocity, Inertia = inertia, Impulses = impulses };
    }
    private static int B(float v) => BitConverter.SingleToInt32Bits(v);
    internal static int[][] CopyRows()
    {
        if (count != 108 || pass != 8) throw new InvalidOperationException("Missing observed row.");
        return Samples.Select(s => new[] { s.Pass, s.Row, s.Edge,
            B(s.Velocity.Linear.X[0]), B(s.Velocity.Linear.Y[0]), B(s.Velocity.Linear.Z[0]),
            B(s.Velocity.Angular.X[0]), B(s.Velocity.Angular.Y[0]), B(s.Velocity.Angular.Z[0]),
            B(s.Inertia.InverseMass[0]), B(s.Inertia.InverseInertiaTensor.XX[0]), B(s.Inertia.InverseInertiaTensor.YX[0]),
            B(s.Inertia.InverseInertiaTensor.YY[0]), B(s.Inertia.InverseInertiaTensor.ZX[0]), B(s.Inertia.InverseInertiaTensor.ZY[0]), B(s.Inertia.InverseInertiaTensor.ZZ[0]),
            B(s.Impulses.Penetration0[0]), B(s.Impulses.Penetration1[0]), B(s.Impulses.Penetration2[0]), B(s.Impulses.Penetration3[0]),
            B(s.Impulses.Tangent.X[0]), B(s.Impulses.Tangent.Y[0]), B(s.Impulses.Twist[0]) }).ToArray();
    }
}

internal static partial class PoweredContactTests
{
    private static Q FromBits(int bits) => Q.Exact(BitConverter.Int32BitsToSingle(bits));
    private static Q Energy(int[] r)
    {
        var a = FromBits(r[10]); var b = FromBits(r[11]); var d = FromBits(r[12]);
        var c = FromBits(r[13]); var e = FromBits(r[14]); var f = FromBits(r[15]); var inverseMass = FromBits(r[9]);
        var determinant = a * (d * f - e * e) - b * (b * f - c * e) + c * (b * e - c * d);
        Check(inverseMass.Positive && a.Positive && (a * d - b * b).Positive && determinant.Positive, "positive native energy metric");
        var x = FromBits(r[6]); var y = FromBits(r[7]); var z = FromBits(r[8]);
        var angular = ((d * f - e * e) * x * x + (a * f - c * c) * y * y + (a * d - b * b) * z * z +
            2 * ((c * e - b * f) * x * y + (b * e - c * d) * x * z + (b * c - a * e) * y * z)) / determinant;
        Q linear = 0; for (var i = 3; i < 6; i++) linear += FromBits(r[i]) * FromBits(r[i]);
        return linear / (2 * inverseMass) + angular / 2;
    }
    private static int[] NativeBits(Contact c)
    {
        var n = Native(c); var b = n.Simulation.Bodies[n.Body]; var rows = ReadRows(c);
        float[] head = [b.Pose.Position.X, b.Pose.Position.Y, b.Pose.Position.Z,
            b.Pose.Orientation.X, b.Pose.Orientation.Y, b.Pose.Orientation.Z, b.Pose.Orientation.W,
            b.Velocity.Linear.X, b.Velocity.Linear.Y, b.Velocity.Linear.Z, b.Velocity.Angular.X, b.Velocity.Angular.Y, b.Velocity.Angular.Z];
        return head.Concat(rows.Impulses).Concat(rows.Offsets.SelectMany(v => new[] { v.X, v.Y, v.Z }))
            .Concat(new[] { rows.Normal.X, rows.Normal.Y, rows.Normal.Z }).Select(BitConverter.SingleToInt32Bits).ToArray();
    }
    internal static void Work()
    {
        foreach (var fixture in new[] { PoweredContactFixture.OffCom, PoweredContactFixture.YawBaseline })
        {
            using var ordinary = new Contact(fixture);
            ordinary.Seal(); var standard = ordinary.Apply().Observation; var normalBits = NativeBits(ordinary);
            using var observed = new Contact(fixture);
            var preview = observed.Seal(); var reference = Chronological(observed, preview);
            var native = Native(observed); var original = native.Simulation.Solver.TypeProcessors[3];
            var processor = new PoweredObservedProcessor(); processor.Initialize(3);
            PoweredWorkRecorder.Reset(); native.Simulation.Solver.TypeProcessors[3] = processor;
            try
            {
                Check(observed.Engine.PreparePoweredContact(observed.Power, observed.FuelLease, out observed.PhysicalLease) ==
                    PoweredFlightStatus.Prepared, "observed genuine production step");
            }
            finally { native.Simulation.Solver.TypeProcessors[3] = original; }
            var committed = observed.Engine.PublishPoweredContact(observed.Power, observed.PhysicalLease);
            Check(committed.Status == PoweredFlightStatus.Published && committed.Observation == standard &&
                NativeBits(observed).SequenceEqual(normalBits), "observed/unobserved committed and native bits identical");
            var rows = PoweredWorkRecorder.CopyRows(); Q[] sums = new Q[6]; Q[] impulses = new Q[7];
            for (var index = 0; index < 54; index++)
            {
                var pass = index / 6; var at = index % 6; var row = pass == 0 ? at : at < 4 ? at + 1 : at == 4 ? 0 : 5;
                var a = rows[2 * index]; var b = rows[2 * index + 1];
                Check(a[0] == pass && a[1] == row && a[2] == 0 && b[0] == pass && b[1] == row && b[2] == 1, "native row application order");
                Check(a.Skip(9).Take(7).SequenceEqual(rows[0].Skip(9).Take(7)) && b.Skip(9).Take(7).SequenceEqual(a.Skip(9).Take(7)), "metric constant within contact stage");
                if (index > 0) Check(rows[2 * index - 1].Skip(3).SequenceEqual(a.Skip(3)), "no unobserved change between row edges");
                for (var j = 0; j < 7; j++)
                {
                    var channel = row == 0 ? j is 4 or 5 : row == 5 ? j == 6 : j == row - 1;
                    if (!channel || pass == 0) Check(a[16 + j] == b[16 + j], "unrelated channel/warm cache unchanged");
                    if (channel) impulses[j] += pass == 0 ? FromBits(b[16 + j]) : FromBits(b[16 + j]) - FromBits(a[16 + j]);
                }
                sums[row] += Energy(b) - Energy(a);
            }
            Q total = 0; foreach (var value in sums) total += value;
            Check((total - Energy(rows[^1]) + Energy(rows[0])).IsZero, "exact energy telescope");
            for (var j = 0; j < 7; j++) Check((impulses[j] - FromBits(rows[^1][16 + j])).IsZero, "exact impulse telescope");
            Check(rows[^1].Skip(3).Take(6).SequenceEqual(normalBits.Skip(7).Take(6)), "observed final native velocity");
            var tangentError = Math.Abs(sums[0].Value - reference.TangentWork) + reference.Tail;
            var twistError = Math.Abs(sums[5].Value - reference.TwistWork);
            Console.WriteLine($"POWERED_CONTACT_WORK {fixture} applications=54 edges=108 exactTelescopes=PASS nativeBits=PASS tangent={sums[0].Value:R} twist={sums[5].Value:R} tangentError={tangentError:R} twistError={twistError:R}");
            Check(tangentError < .07848 && twistError < .07848, "separate frozen discrete work bars");
        }
    }
}
