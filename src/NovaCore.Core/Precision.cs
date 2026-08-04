namespace NovaCore.Core;

public readonly record struct Double2(double X, double Y);
public readonly record struct Double3(double X, double Y, double Z)
{
    public static Double3 operator -(Double3 left, Double3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static Double3 operator -(Double3 value) => new(-value.X, -value.Y, -value.Z);
    public static Double3 operator +(Double3 left, Double3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    public static Double3 operator *(Double3 value, double scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);
    public static Double3 operator /(Double3 value, double scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar);
    public static Double3 Zero => new(0d, 0d, 0d);
    public static Double3 UnitX => new(1d, 0d, 0d);
    public static Double3 UnitY => new(0d, 1d, 0d);
    public static Double3 UnitZ => new(0d, 0d, 1d);
    public static double Dot(in Double3 left, in Double3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    public static Double3 Cross(in Double3 left, in Double3 right) => new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);
    public double LengthSquared => Dot(this, this);
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
    public Double3 Normalized() { var length = Math.Sqrt(LengthSquared); if (!double.IsFinite(length) || length <= 0d) throw new ArgumentOutOfRangeException(nameof(Double3)); return this / length; }
}
public readonly record struct DoubleQuaternion(double X, double Y, double Z, double W)
{
    public static DoubleQuaternion Identity => new(0d, 0d, 0d, 1d);
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z) && double.IsFinite(W);
    public double LengthSquared => X * X + Y * Y + Z * Z + W * W;
    public DoubleQuaternion Normalized() { var length = Math.Sqrt(LengthSquared); if (!double.IsFinite(length) || length <= 0d) throw new ArgumentOutOfRangeException(nameof(DoubleQuaternion)); return new(X / length, Y / length, Z / length, W / length); }
    public DoubleQuaternion Conjugate() => new(-X, -Y, -Z, W);
    public static DoubleQuaternion operator *(DoubleQuaternion a, DoubleQuaternion b) => new(a.W*b.X+a.X*b.W+a.Y*b.Z-a.Z*b.Y, a.W*b.Y-a.X*b.Z+a.Y*b.W+a.Z*b.X, a.W*b.Z+a.X*b.Y-a.Y*b.X+a.Z*b.W, a.W*b.W-a.X*b.X-a.Y*b.Y-a.Z*b.Z);
    public Double3 Rotate(in Double3 value) { var q = Normalized(); var u = new Double3(q.X,q.Y,q.Z); return value + Double3.Cross(u, Double3.Cross(u,value) + value * q.W) * 2d; }
    public static DoubleQuaternion FromAxisAngle(in Double3 axis, double radians) { var n=axis.Normalized();var half=radians*.5d;var s=Math.Sin(half);return new(n.X*s,n.Y*s,n.Z*s,Math.Cos(half)); }
}
public readonly record struct ReferenceFrameId(long Value);
public readonly record struct UniversePosition(Double3 Value, ReferenceFrameId Frame);
public readonly record struct RelativePosition(Double3 Value);
public readonly record struct RenderOrigin(UniversePosition CameraPosition);

public static class ReferenceFrame
{
    public static RelativePosition Resolve(in UniversePosition position, in RenderOrigin origin)
    {
        if (position.Frame != origin.CameraPosition.Frame)
            throw new InvalidOperationException("Reference-frame conversion is not implemented in Milestone 1.");
        return new RelativePosition(position.Value - origin.CameraPosition.Value);
    }
}
