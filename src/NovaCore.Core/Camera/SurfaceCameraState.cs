using NovaCore.Core.Surface;

namespace NovaCore.Core.Camera;

/// <summary>Explicit authority for the player camera reference state.</summary>
public enum CameraReferenceAuthority : byte
{
    Inertial = 0,
    SurfaceRelative = 1,
}

/// <summary>
/// Retained, body-fixed camera authority. The canonical surface identity and all local camera
/// state are FP64 and independent of render topology, GPU residency, and simulation warp.
/// </summary>
public readonly record struct SurfaceCameraState(
    SurfaceAnchor Anchor,
    Double3 EyeOffsetEnuMetres,
    Double3 PivotOffsetEnuMetres,
    DoubleQuaternion BodyFixedOrientation)
{
    public bool IsValid => Anchor.IsValid && EyeOffsetEnuMetres.IsFinite && PivotOffsetEnuMetres.IsFinite &&
        BodyFixedOrientation.IsFinite && Math.Abs(BodyFixedOrientation.LengthSquared - 1d) <= 1e-10d;

    public static bool TryCreate(
        in SurfaceAnchor anchor,
        in Double3 eyeOffsetEnuMetres,
        in Double3 pivotOffsetEnuMetres,
        in DoubleQuaternion bodyFixedOrientation,
        out SurfaceCameraState state)
    {
        state = default;
        if (!anchor.IsValid || !eyeOffsetEnuMetres.IsFinite || !pivotOffsetEnuMetres.IsFinite ||
            !bodyFixedOrientation.IsFinite || bodyFixedOrientation.LengthSquared <= 0d) return false;
        state = new(anchor, eyeOffsetEnuMetres, pivotOffsetEnuMetres, bodyFixedOrientation.Normalized());
        return state.IsValid;
    }
}
