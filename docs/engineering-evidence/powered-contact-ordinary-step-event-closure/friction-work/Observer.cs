// Evidence-only recorder. Original native row arithmetic is generated separately.
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;

internal static class WorkObserver
{
    private struct Sample
    {
        internal int Pass, Row, Edge;
        internal BodyVelocityWide Velocity;
        internal BodyInertiaWide Inertia;
        internal Contact4AccumulatedImpulses Impulses;
    }
    private static readonly Sample[] Samples = new Sample[108];
    private static int count, pass = -1;
    internal static void Begin(bool warm)
    {
        pass++;
        if (warm != (pass == 0) || pass > 8) throw new InvalidOperationException("Unexpected diagnostic pass");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Record(int row, int edge, in BodyVelocityWide velocity,
        in BodyInertiaWide inertia, in Contact4AccumulatedImpulses impulses)
    {
        if (count >= Samples.Length) throw new InvalidOperationException("Observer capacity");
        Samples[count++] = new Sample { Pass = pass, Row = row, Edge = edge,
            Velocity = velocity, Inertia = inertia, Impulses = impulses };
    }
    private static int B(float x) => BitConverter.SingleToInt32Bits(x);
    internal static void Write(string path)
    {
        if (count != 108 || pass != 8) throw new InvalidOperationException("Missing observed row");
        var rows = Samples.Select(s => new[] { s.Pass, s.Row, s.Edge,
            B(s.Velocity.Linear.X[0]), B(s.Velocity.Linear.Y[0]), B(s.Velocity.Linear.Z[0]),
            B(s.Velocity.Angular.X[0]), B(s.Velocity.Angular.Y[0]), B(s.Velocity.Angular.Z[0]),
            B(s.Inertia.InverseMass[0]), B(s.Inertia.InverseInertiaTensor.XX[0]),
            B(s.Inertia.InverseInertiaTensor.YX[0]), B(s.Inertia.InverseInertiaTensor.YY[0]),
            B(s.Inertia.InverseInertiaTensor.ZX[0]), B(s.Inertia.InverseInertiaTensor.ZY[0]),
            B(s.Inertia.InverseInertiaTensor.ZZ[0]),
            B(s.Impulses.Penetration0[0]), B(s.Impulses.Penetration1[0]),
            B(s.Impulses.Penetration2[0]), B(s.Impulses.Penetration3[0]),
            B(s.Impulses.Tangent.X[0]), B(s.Impulses.Tangent.Y[0]), B(s.Impulses.Twist[0]) });
        File.WriteAllText(path, JsonSerializer.Serialize(new { schema = "pass,row,edge,v6,inverseMass,Iinverse6,impulses7", rows }));
    }
}
