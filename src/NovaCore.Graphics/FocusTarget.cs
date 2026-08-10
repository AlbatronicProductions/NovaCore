using NovaCore.Core;

namespace NovaCore.Graphics;

public enum FocusTargetKind : byte
{
    BodyCenter = 0,
    SurfaceAnchor = 1,
    SceneObject = 2,
}

/// <summary>Body-fixed, right-handed east/north/up tangent basis.</summary>
public readonly record struct LocalSurfaceTangentBasis(Double3 East, Double3 North, Double3 Up)
{
    public bool IsValid => East.IsFinite && North.IsFinite && Up.IsFinite &&
        Math.Abs(East.LengthSquared - 1d) <= 1e-10d &&
        Math.Abs(North.LengthSquared - 1d) <= 1e-10d &&
        Math.Abs(Up.LengthSquared - 1d) <= 1e-10d &&
        Math.Abs(Double3.Dot(East, North)) <= 1e-10d &&
        Math.Abs(Double3.Dot(East, Up)) <= 1e-10d &&
        Math.Abs(Double3.Dot(North, Up)) <= 1e-10d &&
        Double3.Dot(Double3.Cross(East, North), Up) >= 1d - 1e-10d;

    public Double3 ToLocal(in Double3 bodyLocalPoint, in Double3 bodyLocalAnchor)
    {
        if (!IsValid || !bodyLocalPoint.IsFinite || !bodyLocalAnchor.IsFinite) throw new ArgumentOutOfRangeException();
        var delta = bodyLocalPoint - bodyLocalAnchor;
        return new(Double3.Dot(delta, East), Double3.Dot(delta, North), Double3.Dot(delta, Up));
    }

    public Double3 ToBodyFixed(in Double3 localEnu, in Double3 bodyLocalAnchor)
    {
        if (!IsValid || !localEnu.IsFinite || !bodyLocalAnchor.IsFinite) throw new ArgumentOutOfRangeException();
        return bodyLocalAnchor + East * localEnu.X + North * localEnu.Y + Up * localEnu.Z;
    }
}

/// <summary>
/// Structural body-fixed surface anchor for the next camera phase. It carries no switching,
/// acquisition, camera-orientation, terrain-LOD, or render-space authority.
/// </summary>
public readonly record struct SurfaceAnchorFocus(
    ulong BodyId,
    Double3 BodyFixedDirection,
    double AuthoritativeElevationMetres,
    Double3 BodyLocalPosition,
    LocalSurfaceTangentBasis LocalTangentBasis)
{
    public double LatitudeRadians => Math.Asin(Math.Clamp(BodyFixedDirection.Y, -1d, 1d));
    public double LongitudeRadians => Math.Atan2(BodyFixedDirection.Z, BodyFixedDirection.X);

    public bool IsValid => BodyId != 0 && BodyFixedDirection.IsFinite &&
        Math.Abs(BodyFixedDirection.LengthSquared - 1d) <= 1e-10d &&
        double.IsFinite(AuthoritativeElevationMetres) && BodyLocalPosition.IsFinite &&
        BodyLocalPosition.LengthSquared > 0d &&
        Math.Sqrt((BodyLocalPosition.Normalized() - BodyFixedDirection).LengthSquared) <= 1e-10d &&
        LocalTangentBasis.IsValid && Double3.Dot(LocalTangentBasis.Up, BodyFixedDirection) >= 1d - 1e-10d;

    public static SurfaceAnchorFocus AtDirection(
        ulong bodyId,
        in Double3 bodyFixedDirection,
        double referenceRadiusMetres,
        double authoritativeElevationMetres)
    {
        if (bodyId == 0 || !bodyFixedDirection.IsFinite || bodyFixedDirection.LengthSquared <= 0d ||
            !double.IsFinite(referenceRadiusMetres) || referenceRadiusMetres <= 0d ||
            !double.IsFinite(authoritativeElevationMetres) || referenceRadiusMetres + authoritativeElevationMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        var direction = bodyFixedDirection.Normalized();
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        return new(bodyId, direction, authoritativeElevationMetres,
            direction * (referenceRadiusMetres + authoritativeElevationMetres),
            new(frame.East, frame.North, frame.Up));
    }
}

/// <summary>
/// Camera-follow identity only. Current root position is evaluated from current upstream authority;
/// camera orientation is deliberately absent and remains independently root-inertial by default.
/// </summary>
public readonly record struct FocusTarget
{
    private FocusTarget(FocusTargetKind kind, ulong bodyId, ulong sceneObjectId, in SurfaceAnchorFocus surfaceAnchor)
    {
        Kind = kind;
        BodyId = bodyId;
        SceneObjectId = sceneObjectId;
        SurfaceAnchor = surfaceAnchor;
    }

    public FocusTargetKind Kind { get; }
    public ulong BodyId { get; }
    public ulong SceneObjectId { get; }
    public SurfaceAnchorFocus SurfaceAnchor { get; }

    public bool IsValid => Kind switch
    {
        FocusTargetKind.BodyCenter => BodyId != 0 && SceneObjectId == 0,
        FocusTargetKind.SurfaceAnchor => BodyId != 0 && SceneObjectId == 0 &&
            SurfaceAnchor.BodyId == BodyId && SurfaceAnchor.IsValid,
        FocusTargetKind.SceneObject => BodyId == 0 && SceneObjectId != 0,
        _ => false,
    };

    public static FocusTarget BodyCenter(ulong bodyId) => bodyId == 0
        ? throw new ArgumentOutOfRangeException(nameof(bodyId))
        : new(FocusTargetKind.BodyCenter, bodyId, 0, default);

    public static FocusTarget AtSurface(in SurfaceAnchorFocus anchor) => !anchor.IsValid
        ? throw new ArgumentException("Surface anchor must be valid.", nameof(anchor))
        : new(FocusTargetKind.SurfaceAnchor, anchor.BodyId, 0, anchor);

    public static FocusTarget SceneObject(ulong sceneObjectId) => sceneObjectId == 0
        ? throw new ArgumentOutOfRangeException(nameof(sceneObjectId))
        : new(FocusTargetKind.SceneObject, 0, sceneObjectId, default);

    /// <summary>Evaluates body-center or body-fixed anchor position from the current immutable body proxy.</summary>
    public bool TryEvaluate(in PlanetRenderProxy currentBody, out UniversePosition rootPosition)
    {
        rootPosition = default;
        if (!IsValid || Kind == FocusTargetKind.SceneObject || currentBody.BodyId != BodyId || !currentBody.IsValid) return false;
        rootPosition = Kind == FocusTargetKind.BodyCenter
            ? currentBody.Position
            : new UniversePosition(
                currentBody.Position.Value + currentBody.BodyFixedToRoot.Rotate(SurfaceAnchor.BodyLocalPosition),
                currentBody.Position.Frame);
        return rootPosition.Value.IsFinite;
    }

    /// <summary>Future vessel/scene-object seam; the caller supplies the current authoritative root position.</summary>
    public bool TryEvaluateSceneObject(
        ulong currentSceneObjectId,
        in UniversePosition currentRootPosition,
        out UniversePosition rootPosition)
    {
        rootPosition = default;
        if (!IsValid || Kind != FocusTargetKind.SceneObject || currentSceneObjectId != SceneObjectId ||
            !currentRootPosition.Value.IsFinite) return false;
        rootPosition = currentRootPosition;
        return true;
    }
}
