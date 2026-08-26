using NovaCore.Core.ReferenceFrames;

namespace NovaCore.Core.Surface;

/// <summary>Stable identity of one body-fixed static surface object.</summary>
public readonly record struct AnchoredSurfaceObjectId(ulong Value)
{
    public bool IsValid => Value != 0;
}

/// <summary>Stable identity of renderer-owned geometry used to present an anchored object.</summary>
public readonly record struct SurfaceGeometryId(uint Value, uint Version)
{
    public bool IsValid => Value != 0 && Version != 0;
}

/// <summary>
/// Immutable physical identity of a static surface object. Local coordinates use metres in
/// canonical East, North, Up order. Solar-root state and render resources are derived state.
/// </summary>
public readonly record struct AnchoredSurfaceObject(
    AnchoredSurfaceObjectId Id,
    SurfaceAnchor Anchor,
    Double3 LocalEnuPositionOffsetMetres,
    DoubleQuaternion LocalEnuOrientation,
    SurfaceGeometryId Geometry)
{
    public bool IsValid => Id.IsValid && Anchor.IsValid && LocalEnuPositionOffsetMetres.IsFinite &&
        LocalEnuOrientation.IsFinite && Math.Abs(LocalEnuOrientation.LengthSquared - 1d) <= 1e-10d && Geometry.IsValid;

    public ulong DeterministicHash
    {
        get
        {
            ulong hash = 14695981039346656037UL;
            hash = Mix(hash, Id.Value);
            hash = Mix(hash, Anchor.DeterministicHash);
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuPositionOffsetMetres.X));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuPositionOffsetMetres.Y));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuPositionOffsetMetres.Z));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuOrientation.X));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuOrientation.Y));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuOrientation.Z));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(LocalEnuOrientation.W));
            hash = Mix(hash, Geometry.Value);
            return Mix(hash, Geometry.Version);
        }
    }

    private static ulong Mix(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; }
        return hash;
    }
}

/// <summary>Derived body-fixed and root pose of an anchored object at one evaluated instant.</summary>
public readonly record struct AnchoredSurfaceObjectPose(
    Double3 BodyFixedPosition,
    DoubleQuaternion BodyFixedOrientation,
    UniversePosition RootPosition,
    DoubleQuaternion RootOrientation,
    double PhysicalTerrainHeightMetres,
    SurfaceEnuFrame Enu)
{
    public bool IsValid => BodyFixedPosition.IsFinite && BodyFixedOrientation.IsFinite && RootPosition.Value.IsFinite &&
        RootOrientation.IsFinite && double.IsFinite(PhysicalTerrainHeightMetres) && Enu.IsValid;
}

/// <summary>Allocation-free transformation of immutable anchored-object authority.</summary>
public static class AnchoredSurfaceObjectEvaluator
{
    public static SurfaceAnchorEvaluationStatus TryEvaluate<TTerrain>(
        in AnchoredSurfaceObject value,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        in FrameTransform bodyFixedToRoot,
        ReferenceFrameId rootFrame,
        out AnchoredSurfaceObjectPose pose)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        pose = default;
        if (!value.IsValid || !bodyFixedToRoot.IsFinite || rootFrame.Value == 0)
            return SurfaceAnchorEvaluationStatus.NonFiniteInput;
        var status = SurfaceAnchorEvaluator.TryEvaluateBodyFixed(value.Anchor, body, terrain,
            out var anchorBodyFixed, out var terrainHeight);
        if (status != SurfaceAnchorEvaluationStatus.Success) return status;
        if (!SurfaceEnuFrame.TryCreate(value.Anchor, out var enu)) return SurfaceAnchorEvaluationStatus.InvalidAnchor;
        var local = value.LocalEnuPositionOffsetMetres;
        var bodyFixedPosition = anchorBodyFixed + enu.East * local.X + enu.North * local.Y + enu.Up * local.Z;
        var enuOrientation = QuaternionFromBasis(enu.East, enu.North, enu.Up);
        var bodyFixedOrientation = (enuOrientation * value.LocalEnuOrientation).Normalized();
        var rootPosition = new UniversePosition(bodyFixedToRoot.LocalToParent(bodyFixedPosition), rootFrame);
        var rootOrientation = (bodyFixedToRoot.Rotation * bodyFixedOrientation).Normalized();
        pose = new(bodyFixedPosition, bodyFixedOrientation, rootPosition, rootOrientation, terrainHeight, enu);
        return pose.IsValid ? SurfaceAnchorEvaluationStatus.Success : SurfaceAnchorEvaluationStatus.NonFiniteResult;
    }

    private static DoubleQuaternion QuaternionFromBasis(in Double3 x, in Double3 y, in Double3 z)
    {
        var trace = x.X + y.Y + z.Z;
        double qx, qy, qz, qw;
        if (trace > 0d) { var s = Math.Sqrt(trace + 1d) * 2d; qw = .25d * s; qx = (y.Z - z.Y) / s; qy = (z.X - x.Z) / s; qz = (x.Y - y.X) / s; }
        else if (x.X > y.Y && x.X > z.Z) { var s = Math.Sqrt(1d + x.X - y.Y - z.Z) * 2d; qw = (y.Z - z.Y) / s; qx = .25d * s; qy = (y.X + x.Y) / s; qz = (z.X + x.Z) / s; }
        else if (y.Y > z.Z) { var s = Math.Sqrt(1d + y.Y - x.X - z.Z) * 2d; qw = (z.X - x.Z) / s; qx = (y.X + x.Y) / s; qy = .25d * s; qz = (z.Y + y.Z) / s; }
        else { var s = Math.Sqrt(1d + z.Z - x.X - y.Y) * 2d; qw = (x.Y - y.X) / s; qx = (z.X + x.Z) / s; qy = (z.Y + y.Z) / s; qz = .25d * s; }
        return new DoubleQuaternion(qx, qy, qz, qw).Normalized();
    }
}
