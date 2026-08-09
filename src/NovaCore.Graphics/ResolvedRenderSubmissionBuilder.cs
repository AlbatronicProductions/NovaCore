using NovaCore.Core;

namespace NovaCore.Graphics;

public enum ResolvedRenderSubmissionBuildStatus : byte
{
    Success = 0,
    CameraRootMismatch,
    InvalidCameraData,
    DestinationCapacityExceeded,
    InvalidSnapshotObject,
    InvalidOrbitCurve,
}

/// <summary>Deterministically copies a validated resolved snapshot into reusable GPU-facing frame storage.</summary>
public static class ResolvedRenderSubmissionBuilder
{
    public static ResolvedRenderSubmissionBuildStatus TryBuild(
        ResolvedRenderSnapshot snapshot,
        in GpuCameraData camera,
        in UniversePosition cameraRootPosition,
        RenderFrameSubmission destination)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(destination);

        if (cameraRootPosition.Frame != snapshot.RootFrame) return ResolvedRenderSubmissionBuildStatus.CameraRootMismatch;
        if (!cameraRootPosition.Value.IsFinite || !IsFinite(camera)) return ResolvedRenderSubmissionBuildStatus.InvalidCameraData;
        if (snapshot.Count > destination.Capacity) return ResolvedRenderSubmissionBuildStatus.DestinationCapacityExceeded;

        foreach (ref readonly var current in snapshot.Objects)
            if (!current.IsValid(out _)) return ResolvedRenderSubmissionBuildStatus.InvalidSnapshotObject;

        destination.Begin(camera, cameraRootPosition);
        foreach (ref readonly var current in snapshot.Objects)
            destination.Add(current.RootPosition, current.RootOrientation, current.Scale, current.Mesh);
        destination.Complete();
        if (snapshot.OrbitCurve is not null && !destination.TrySetOrbitVertices(snapshot.OrbitCurve, cameraRootPosition)) return ResolvedRenderSubmissionBuildStatus.InvalidOrbitCurve;
        if (snapshot.PreviousOrbitCurve is not null && !destination.TrySetOrbitVertices(snapshot.PreviousOrbitCurve, cameraRootPosition, true)) return ResolvedRenderSubmissionBuildStatus.InvalidOrbitCurve;
        if (snapshot.BodyForwardIndicator is { } bodyForward && !destination.TrySetDirectionIndicator(bodyForward, cameraRootPosition, false)) return ResolvedRenderSubmissionBuildStatus.InvalidOrbitCurve;
        if (snapshot.TargetDirectionIndicator is { } targetDirection && !destination.TrySetDirectionIndicator(targetDirection, cameraRootPosition, true)) return ResolvedRenderSubmissionBuildStatus.InvalidOrbitCurve;
        return ResolvedRenderSubmissionBuildStatus.Success;
    }

    private static bool IsFinite(in GpuCameraData camera) =>
        float.IsFinite(camera.Position.HighX) && float.IsFinite(camera.Position.HighY) && float.IsFinite(camera.Position.HighZ) &&
        float.IsFinite(camera.Position.LowX) && float.IsFinite(camera.Position.LowY) && float.IsFinite(camera.Position.LowZ) &&
        float.IsFinite(camera.PositionHighPadding) && float.IsFinite(camera.PositionLowPadding) &&
        float.IsFinite(camera.ViewProjection.C0R0) && float.IsFinite(camera.ViewProjection.C0R1) && float.IsFinite(camera.ViewProjection.C0R2) && float.IsFinite(camera.ViewProjection.C0R3) &&
        float.IsFinite(camera.ViewProjection.C1R0) && float.IsFinite(camera.ViewProjection.C1R1) && float.IsFinite(camera.ViewProjection.C1R2) && float.IsFinite(camera.ViewProjection.C1R3) &&
        float.IsFinite(camera.ViewProjection.C2R0) && float.IsFinite(camera.ViewProjection.C2R1) && float.IsFinite(camera.ViewProjection.C2R2) && float.IsFinite(camera.ViewProjection.C2R3) &&
        float.IsFinite(camera.ViewProjection.C3R0) && float.IsFinite(camera.ViewProjection.C3R1) && float.IsFinite(camera.ViewProjection.C3R2) && float.IsFinite(camera.ViewProjection.C3R3);
}
