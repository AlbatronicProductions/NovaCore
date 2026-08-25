using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>One evaluated surface-relative camera pose in Solar-root and body-fixed FP64.</summary>
public readonly record struct SurfaceCameraPose(
    UniversePosition RootEye,
    UniversePosition RootPivot,
    DoubleQuaternion RootOrientation,
    Double3 BodyFixedEye,
    Double3 BodyFixedPivot,
    double PhysicalTerrainHeightMetres)
{
    public bool IsValid => RootEye.Value.IsFinite && RootPivot.Value.IsFinite && RootEye.Frame == RootPivot.Frame &&
        RootOrientation.IsFinite && Math.Abs(RootOrientation.LengthSquared - 1d) <= 1e-10d &&
        BodyFixedEye.IsFinite && BodyFixedPivot.IsFinite && double.IsFinite(PhysicalTerrainHeightMetres);
}

public readonly record struct SurfaceCameraTransitionMetrics(
    double PositionErrorMetres,
    double PivotErrorMetres,
    double OrientationErrorRadians)
{
    public bool IsFinite => double.IsFinite(PositionErrorMetres) && double.IsFinite(PivotErrorMetres) &&
        double.IsFinite(OrientationErrorRadians);
}

/// <summary>
/// Allocation-free surface-camera evaluation and an explicit, allocation-tolerant attach seam.
/// The retained camera is never integrated with body rotation: it is re-evaluated from canonical
/// body-fixed state and the current immutable body transform.
/// </summary>
public static class SurfaceCameraAuthority
{
    private const int MaximumExteriorIterations = 3;

    public static bool TryAttach<TTerrain>(
        in PlanetRenderProxy body,
        in UniversePosition actualRootEye,
        in UniversePosition actualRootPivot,
        in DoubleQuaternion actualRootOrientation,
        in TTerrain terrain,
        out SurfaceCameraState state,
        out SurfaceCameraPose equivalentPose,
        out SurfaceCameraTransitionMetrics metrics)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        state = default;
        equivalentPose = default;
        metrics = default;
        if (!body.IsValid || actualRootEye.Frame != body.Position.Frame || actualRootPivot.Frame != body.Position.Frame ||
            !actualRootEye.Value.IsFinite || !actualRootPivot.Value.IsFinite || !actualRootOrientation.IsFinite ||
            actualRootOrientation.LengthSquared <= 0d || !terrain.SupportsBody(body.BodyId)) return false;

        var bodyFrame = BodyFrame(body.BodyId);
        var bodyReference = new SurfaceBodyReference(body.BodyId, body.RadiusMetres, bodyFrame);
        var frames = BuildResolver(body, bodyFrame);
        var status = SurfaceAnchorEvaluator.TryCreateFromRoot(
            body.BodyId, terrain.AuthorityVersion, actualRootEye, bodyReference, terrain, frames,
            out var eyeAnchor, out var bodyFixedEye, out _);
        if (status != SurfaceAnchorEvaluationStatus.Success ||
            SurfaceAnchor.TryCreate(body.BodyId, terrain.AuthorityVersion,
                eyeAnchor.NormalizedBodyFixedDirection, 0d, out var anchor) != SurfaceAnchorCreationStatus.Success ||
            !SurfaceEnuFrame.TryCreate(anchor, out var enu) ||
            !frames.TryConvertPosition(new(actualRootPivot.Frame, actualRootPivot.Value), bodyFrame, out var pivotBody))
            return false;

        status = SurfaceAnchorEvaluator.TryEvaluateBodyFixed(anchor, bodyReference, terrain,
            out var anchorBody, out _);
        if (status != SurfaceAnchorEvaluationStatus.Success) return false;
        var eyeOffset = ToEnu(bodyFixedEye - anchorBody, enu);
        var pivotOffset = ToEnu(pivotBody.Value - anchorBody, enu);
        var bodyOrientation = (body.BodyFixedToRoot.Conjugate().Normalized() * actualRootOrientation.Normalized()).Normalized();
        if (!SurfaceCameraState.TryCreate(anchor, eyeOffset, pivotOffset, bodyOrientation, out state) ||
            !TryEvaluate(body, state, terrain, out equivalentPose))
            return false;

        metrics = new(
            Math.Sqrt((equivalentPose.RootEye.Value - actualRootEye.Value).LengthSquared),
            Math.Sqrt((equivalentPose.RootPivot.Value - actualRootPivot.Value).LengthSquared),
            QuaternionAngularError(equivalentPose.RootOrientation, actualRootOrientation));
        return metrics.IsFinite;
    }

    public static bool TryEvaluate<TTerrain>(
        in PlanetRenderProxy body,
        in SurfaceCameraState state,
        in TTerrain terrain,
        out SurfaceCameraPose pose)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        pose = default;
        if (!body.IsValid || !state.IsValid || body.BodyId != state.Anchor.BodyId ||
            terrain.AuthorityVersion != state.Anchor.TerrainAuthorityVersion || !terrain.SupportsBody(body.BodyId) ||
            !SurfaceEnuFrame.TryCreate(state.Anchor, out var enu)) return false;
        var bodyReference = new SurfaceBodyReference(body.BodyId, body.RadiusMetres, BodyFrame(body.BodyId));
        var status = SurfaceAnchorEvaluator.TryEvaluateBodyFixed(state.Anchor, bodyReference, terrain,
            out var anchorBody, out var height);
        if (status != SurfaceAnchorEvaluationStatus.Success) return false;
        var bodyEye = anchorBody + FromEnu(state.EyeOffsetEnuMetres, enu);
        var bodyPivot = anchorBody + FromEnu(state.PivotOffsetEnuMetres, enu);
        var rootEye = body.Position.Value + body.BodyFixedToRoot.Rotate(bodyEye);
        var rootPivot = body.Position.Value + body.BodyFixedToRoot.Rotate(bodyPivot);
        var rootOrientation = (body.BodyFixedToRoot * state.BodyFixedOrientation).Normalized();
        pose = new(new(rootEye, body.Position.Frame), new(rootPivot, body.Position.Frame), rootOrientation,
            bodyEye, bodyPivot, height);
        return pose.IsValid;
    }

    public static bool TryRetainEvaluatedEye<TTerrain>(
        in PlanetRenderProxy body,
        in SurfaceCameraState state,
        in Double3 correctedRootEye,
        in TTerrain terrain,
        out SurfaceCameraState correctedState)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        correctedState = default;
        if (!TryEvaluate(body, state, terrain, out var pose) || !correctedRootEye.IsFinite ||
            !SurfaceEnuFrame.TryCreate(state.Anchor, out var enu)) return false;
        var correctedBodyEye = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(correctedRootEye - body.Position.Value);
        var anchorBody = pose.BodyFixedEye - FromEnu(state.EyeOffsetEnuMetres, enu);
        return SurfaceCameraState.TryCreate(state.Anchor, ToEnu(correctedBodyEye - anchorBody, enu),
            state.PivotOffsetEnuMetres, state.BodyFixedOrientation, out correctedState);
    }

    /// <summary>
    /// Enforces the physical camera floor entirely in body-fixed FP64.  The retained state is
    /// corrected before root-space publication, so terrain safety never depends on recovering a
    /// metre-scale surface offset from an astronomical root coordinate.
    /// </summary>
    public static bool TryConstrainBodyFixedEye<TTerrain>(
        in PlanetRenderProxy body,
        in SurfaceCameraState state,
        in TTerrain terrain,
        double minimumClearanceMetres,
        double correctionTargetMetres,
        out SurfaceCameraState constrainedState,
        out SurfaceCameraPose constrainedPose,
        out CameraExteriorConstraintResult result)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        constrainedState = default;
        constrainedPose = default;
        result = default;
        if (!TryEvaluate(body, state, terrain, out var pose) ||
            !SurfaceEnuFrame.TryCreate(state.Anchor, out var enu) ||
            !double.IsFinite(minimumClearanceMetres) || minimumClearanceMetres < 0d ||
            !double.IsFinite(correctionTargetMetres) || correctionTargetMetres < minimumClearanceMetres)
            return false;

        var bodyEye = pose.BodyFixedEye;
        var correction = 0d;
        var iterations = 0;
        var queries = 0;
        var altitude = double.NaN;
        for (var iteration = 0; iteration <= MaximumExteriorIterations; iteration++)
        {
            var radius = Math.Sqrt(bodyEye.LengthSquared);
            if (!(radius > 0d) || !double.IsFinite(radius)) return false;
            var direction = (bodyEye / radius).Normalized();
            if (!terrain.TrySampleHeight(body.BodyId, direction, out var terrainHeight) || !double.IsFinite(terrainHeight))
                return false;
            queries++;
            altitude = radius - (body.RadiusMetres + Math.Max(0d, terrainHeight));
            if (altitude >= minimumClearanceMetres) break;
            if (iteration == MaximumExteriorIterations) return false;
            var requiredRadius = body.RadiusMetres + Math.Max(0d, terrainHeight) + correctionTargetMetres;
            correction += Math.Max(0d, requiredRadius - radius);
            bodyEye = direction * requiredRadius;
            iterations++;
        }

        var anchorBody = pose.BodyFixedEye - FromEnu(state.EyeOffsetEnuMetres, enu);
        if (!SurfaceCameraState.TryCreate(state.Anchor, ToEnu(bodyEye - anchorBody, enu),
                state.PivotOffsetEnuMetres, state.BodyFixedOrientation, out constrainedState) ||
            !TryEvaluate(body, constrainedState, terrain, out constrainedPose))
            return false;
        result = new(constrainedPose.RootEye.Value, bodyEye, altitude, correction, iterations, queries);
        return result.IsValid && altitude >= minimumClearanceMetres;
    }

    public static Double3 ToEnu(in Double3 bodyFixedVector, in SurfaceEnuFrame enu) =>
        new(Double3.Dot(bodyFixedVector, enu.East), Double3.Dot(bodyFixedVector, enu.North), Double3.Dot(bodyFixedVector, enu.Up));

    public static Double3 FromEnu(in Double3 enuVector, in SurfaceEnuFrame enu) =>
        enu.East * enuVector.X + enu.North * enuVector.Y + enu.Up * enuVector.Z;

    public static double QuaternionAngularError(in DoubleQuaternion left, in DoubleQuaternion right)
    {
        var a = left.Normalized();
        var b = right.Normalized();
        var dot = Math.Abs(a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W);
        return 2d * Math.Acos(Math.Clamp(dot, -1d, 1d));
    }

    private static ReferenceFrameId BodyFrame(ulong bodyId) => new(-checked((long)bodyId));

    private static ReferenceFrameResolver BuildResolver(in PlanetRenderProxy body, ReferenceFrameId bodyFrame)
    {
        var frames = new ReferenceFrameSnapshot([
            (new ReferenceFrameDefinition(body.Position.Frame, null, ReferenceFrameKind.Ecl, "Surface camera root"), CelestialFrameFactory.RootEcl()),
            (new ReferenceFrameDefinition(bodyFrame, body.Position.Frame, ReferenceFrameKind.Ccf, "Surface camera body-fixed"),
                new EvaluatedReferenceFrame(new FrameTransform(body.Position.Value, body.BodyFixedToRoot), Double3.Zero, Double3.Zero, false))]);
        return new(frames);
    }
}
