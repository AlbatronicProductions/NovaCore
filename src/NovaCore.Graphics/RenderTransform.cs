using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Blittable FP32 GPU vector. W padding is supplied at the ABI boundary.</summary>
public readonly record struct Float3(float X, float Y, float Z)
{
    public static Float3 FromDouble3(in Double3 value) => new((float)value.X, (float)value.Y, (float)value.Z);
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}

/// <summary>Right-handed Hamilton quaternion in XYZW order. Rotation is q * v * conjugate(q).</summary>
public readonly record struct FloatQuaternion(float X, float Y, float Z, float W)
{
    public static FloatQuaternion Identity => new(0, 0, 0, 1);
    public static FloatQuaternion FromDoubleQuaternion(in DoubleQuaternion value) => new((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z) && float.IsFinite(W);
}

/// <summary>FP32 transport transform. It is not authoritative simulation state.</summary>
public readonly record struct RenderTransform(FloatQuaternion Rotation, Float3 Scale)
{
    public static RenderTransform FromAuthoritative(in DoubleQuaternion rotation, in Double3 scale) =>
        new(FloatQuaternion.FromDoubleQuaternion(rotation), Float3.FromDouble3(scale));
    public bool IsFinite => Rotation.IsFinite && Scale.IsFinite;
}
