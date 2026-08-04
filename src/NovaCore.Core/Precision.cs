namespace NovaCore.Core;

public readonly record struct Double2(double X, double Y);
public readonly record struct Double3(double X, double Y, double Z)
{
    public static Double3 operator -(Double3 left, Double3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static Double3 operator +(Double3 left, Double3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    public static Double3 operator *(Double3 value, double scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);
}
public readonly record struct DoubleQuaternion(double X, double Y, double Z, double W);
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
