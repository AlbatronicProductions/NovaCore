using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal readonly record struct EngineeringBox(Double3 Dimensions, Double3 CentreAssembly,
    DoubleQuaternion Orientation, double MassKilograms);

/// <summary>Versioned SHA-256 value provenance. Contains no solver handles or managed references.</summary>
internal readonly record struct EngineeringArticleIdentity(uint Version, ulong A, ulong B, ulong C, ulong D);

internal readonly record struct EngineeringInertiaTensor(double XX, double YY, double ZZ, double XY, double XZ, double YZ);

/// <summary>
/// NovaCore.EngineeringBusPods v1. Three additive uniform rigid boxes, in metres/kg.
/// X spans the pods, Y is up, Z is fore/aft. Identity-oriented, symmetric pods permit
/// fixed principal axes; this is not a general spacecraft assembly or eigensolver.
/// </summary>
internal sealed class EngineeringContactArticle
{
    internal const int ChildCount = 3;
    private readonly EngineeringBox bus, left, right;
    internal double MassKilograms { get; }
    internal Double3 CentreOfMassAssembly { get; }
    internal EngineeringInertiaTensor Tensor { get; }
    internal PrincipalMomentsOfInertia PrincipalInertia => new(Tensor.XX, Tensor.YY, Tensor.ZZ);
    internal Double3 Dimensions { get; }
    internal double SmallestFeature { get; }
    internal double BoundingRadius { get; }
    internal EngineeringArticleIdentity Identity { get; }

    private EngineeringContactArticle(EngineeringBox bus, EngineeringBox left, EngineeringBox right,
        double mass, Double3 com, EngineeringInertiaTensor tensor, Double3 dimensions,
        double minimum, double radius, EngineeringArticleIdentity identity)
    {
        this.bus = bus; this.left = left; this.right = right; MassKilograms = mass;
        CentreOfMassAssembly = com; Tensor = tensor; Dimensions = dimensions;
        SmallestFeature = minimum; BoundingRadius = radius; Identity = identity;
    }

    internal static EngineeringContactArticle Create()
    {
        if (!TryCreate(new(new(1.5, 1, 2.5), Double3.Zero, DoubleQuaternion.Identity, 800),
            new(new(.5, 1, 1.5), new(-1, 0, .5), DoubleQuaternion.Identity, 100),
            new(new(.5, 1, 1.5), new(1, 0, .5), DoubleQuaternion.Identity, 100), out var article))
            throw new InvalidOperationException("Invalid built-in engineering article.");
        return article!;
    }

    internal EngineeringBox Child(int index) => index switch
    { 0 => bus, 1 => left, 2 => right, _ => throw new ArgumentOutOfRangeException(nameof(index)) };

    // Both collision construction and presentation apply this one assembly-to-COM subtraction.
    internal Double3 ChildCentreBody(int index) => Child(index).CentreAssembly - CentreOfMassAssembly;

    internal static bool TryCreate(EngineeringBox bus, EngineeringBox left, EngineeringBox right,
        out EngineeringContactArticle? article)
    {
        article = null;
        if (!Valid(bus) || !Valid(left) || !Valid(right) || bus.CentreAssembly != Double3.Zero ||
            left.Dimensions != right.Dimensions || left.MassKilograms != right.MassKilograms ||
            left.CentreAssembly.X >= 0 || right.CentreAssembly.X != -left.CentreAssembly.X ||
            left.CentreAssembly.Y != 0 || right.CentreAssembly.Y != 0 ||
            left.CentreAssembly.Z != right.CentreAssembly.Z ||
            left.CentreAssembly.X != -(bus.Dimensions.X + left.Dimensions.X) * .5)
            return false;
        var mass = bus.MassKilograms + left.MassKilograms + right.MassKilograms;
        var com = (bus.CentreAssembly * bus.MassKilograms + left.CentreAssembly * left.MassKilograms +
            right.CentreAssembly * right.MassKilograms) / mass;
        if (!double.IsFinite(mass) || !com.IsFinite || !PositiveFloat(1 / mass)) return false;
        double xx = 0, yy = 0, zz = 0, xy = 0, xz = 0, yz = 0, radiusSquared = 0, minimum = double.MaxValue;
        var low = new Double3(double.MaxValue, double.MaxValue, double.MaxValue);
        var high = new Double3(-double.MaxValue, -double.MaxValue, -double.MaxValue);
        Span<EngineeringBox> children = [bus, left, right];
        foreach (var child in children)
        {
            var d = child.Dimensions; var p = child.CentreAssembly - com; var m = child.MassKilograms;
            // Full uniform-box tensor plus the full parallel-axis tensor, before checking diagonal axes.
            xx += m * ((d.Y * d.Y + d.Z * d.Z) / 12 + p.Y * p.Y + p.Z * p.Z);
            yy += m * ((d.X * d.X + d.Z * d.Z) / 12 + p.X * p.X + p.Z * p.Z);
            zz += m * ((d.X * d.X + d.Y * d.Y) / 12 + p.X * p.X + p.Y * p.Y);
            xy -= m * p.X * p.Y; xz -= m * p.X * p.Z; yz -= m * p.Y * p.Z;
            minimum = Math.Min(minimum, Math.Min(d.X, Math.Min(d.Y, d.Z)));
            for (var corner = 0; corner < 8; corner++)
            {
                var v = child.CentreAssembly + new Double3((corner & 1) == 0 ? -d.X / 2 : d.X / 2,
                    (corner & 2) == 0 ? -d.Y / 2 : d.Y / 2, (corner & 4) == 0 ? -d.Z / 2 : d.Z / 2);
                radiusSquared = Math.Max(radiusSquared, (v - com).LengthSquared);
                low = new(Math.Min(low.X, v.X), Math.Min(low.Y, v.Y), Math.Min(low.Z, v.Z));
                high = new(Math.Max(high.X, v.X), Math.Max(high.Y, v.Y), Math.Max(high.Z, v.Z));
            }
        }
        // The supported symmetry must actually produce diagonal principal axes. Never discard cross terms.
        if (xy != 0 || xz != 0 || yz != 0 || !PositiveFloat(1 / xx) || !PositiveFloat(1 / yy) ||
            !PositiveFloat(1 / zz) || !double.IsFinite(radiusSquared) || !float.IsNormal((float)radiusSquared) ||
            !float.IsNormal((float)(minimum / 1000))) return false;
        var tolerance = minimum / 1000;
        foreach (var child in children)
            if (!FloatTransportFits(child.Dimensions, tolerance) || !FloatTransportFits(child.CentreAssembly - com, tolerance))
                return false;
        // Fixed little-endian encoding: version, then bus/left/right dimensions, assembly centre,
        // orientation and mass. Signed zero is canonicalized. The digest is computed only during preparation.
        Span<byte> encoded = stackalloc byte[4 + ChildCount * 88];
        BinaryPrimitives.WriteUInt32LittleEndian(encoded, 1);
        var offset = 4;
        foreach (var child in children)
        {
            Span<double> values = [child.Dimensions.X, child.Dimensions.Y, child.Dimensions.Z,
                child.CentreAssembly.X, child.CentreAssembly.Y, child.CentreAssembly.Z,
                child.Orientation.X, child.Orientation.Y, child.Orientation.Z, child.Orientation.W, child.MassKilograms];
            foreach (var value in values)
            { BinaryPrimitives.WriteDoubleLittleEndian(encoded[offset..], value == 0 ? 0 : value); offset += 8; }
        }
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(encoded, digest);
        var identity = new EngineeringArticleIdentity(1, BinaryPrimitives.ReadUInt64LittleEndian(digest),
            BinaryPrimitives.ReadUInt64LittleEndian(digest[8..]), BinaryPrimitives.ReadUInt64LittleEndian(digest[16..]),
            BinaryPrimitives.ReadUInt64LittleEndian(digest[24..]));
        article = new(bus, left, right, mass, com, new(xx, yy, zz, xy, xz, yz), high - low,
            minimum, Math.Sqrt(radiusSquared), identity);
        return true;
    }

    private static bool Valid(EngineeringBox b) => b.Dimensions.IsFinite && b.CentreAssembly.IsFinite &&
        b.Orientation == DoubleQuaternion.Identity && b.Dimensions.X > 0 && b.Dimensions.Y > 0 && b.Dimensions.Z > 0 &&
        double.IsNormal(b.MassKilograms) && b.MassKilograms > 0;
    private static bool PositiveFloat(double value) => value > 0 && float.IsNormal((float)value);
    private static bool FloatTransportFits(Double3 v, double tolerance) =>
        Math.Abs((double)(float)v.X - v.X) <= tolerance / 8 &&
        Math.Abs((double)(float)v.Y - v.Y) <= tolerance / 8 && Math.Abs((double)(float)v.Z - v.Z) <= tolerance / 8;
}
