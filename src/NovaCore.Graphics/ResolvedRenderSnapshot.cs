using NovaCore.Core;

namespace NovaCore.Graphics;

public enum ResolvedRenderSnapshotStatus : byte
{
    Success = 0,
    Empty,
    InvalidObjectId,
    DuplicateObjectId,
    NonFinitePosition,
    InvalidOrientation,
    NonFiniteScale,
    InvalidMeshHandle,
    MixedRootFrame,
}

/// <summary>
/// Immutable, derived renderer input containing only already root-resolved object values.
/// It is not a SimulationSnapshot and contains no frame topology or authoritative simulation state.
/// </summary>
public sealed class ResolvedRenderSnapshot
{
    private readonly ResolvedRenderObject[] _objects;

    private ResolvedRenderSnapshot(ReferenceFrameId rootFrame, ResolvedRenderObject[] objects, ResolvedOrbitCurve? orbitCurve, ResolvedOrbitCurve? previousOrbitCurve, ResolvedDirectionIndicator? bodyForwardIndicator, ResolvedDirectionIndicator? targetDirectionIndicator)
    {
        RootFrame = rootFrame;
        _objects = objects;
        OrbitCurve = orbitCurve;
        PreviousOrbitCurve = previousOrbitCurve;
        BodyForwardIndicator = bodyForwardIndicator;
        TargetDirectionIndicator = targetDirectionIndicator;
    }

    public ReferenceFrameId RootFrame { get; }
    public int Count => _objects.Length;
    public ReadOnlySpan<ResolvedRenderObject> Objects => _objects;
    public ResolvedOrbitCurve? OrbitCurve { get; }
    /// <summary>Optional single pre-transition curve; derived presentation only.</summary>
    public ResolvedOrbitCurve? PreviousOrbitCurve { get; }
    /// <summary>Optional derived body-forward overlay; never authoritative simulation state.</summary>
    public ResolvedDirectionIndicator? BodyForwardIndicator { get; }
    /// <summary>Optional derived target-direction overlay; never authoritative guidance state.</summary>
    public ResolvedDirectionIndicator? TargetDirectionIndicator { get; }

    /// <summary>Copies validated caller input once, preserving its explicit declaration order.</summary>
    public static bool TryCreate(ReadOnlySpan<ResolvedRenderObject> objects, out ResolvedRenderSnapshot? snapshot, out ResolvedRenderSnapshotStatus status) => TryCreate(objects, null, null, null, null, out snapshot, out status);
    public static bool TryCreate(ReadOnlySpan<ResolvedRenderObject> objects, ResolvedOrbitCurve? orbitCurve, out ResolvedRenderSnapshot? snapshot, out ResolvedRenderSnapshotStatus status) => TryCreate(objects, orbitCurve, null, null, null, out snapshot, out status);
    public static bool TryCreate(ReadOnlySpan<ResolvedRenderObject> objects, ResolvedOrbitCurve? orbitCurve, ResolvedOrbitCurve? previousOrbitCurve, out ResolvedRenderSnapshot? snapshot, out ResolvedRenderSnapshotStatus status) => TryCreate(objects, orbitCurve, previousOrbitCurve, null, null, out snapshot, out status);
    public static bool TryCreate(ReadOnlySpan<ResolvedRenderObject> objects, ResolvedOrbitCurve? orbitCurve, ResolvedOrbitCurve? previousOrbitCurve, ResolvedDirectionIndicator? bodyForwardIndicator, ResolvedDirectionIndicator? targetDirectionIndicator, out ResolvedRenderSnapshot? snapshot, out ResolvedRenderSnapshotStatus status)
    {
        snapshot = null;
        if (objects.Length == 0) { status = ResolvedRenderSnapshotStatus.Empty; return false; }

        var rootFrame = objects[0].RootPosition.Frame;
        for (var index = 0; index < objects.Length; index++)
        {
            ref readonly var current = ref objects[index];
            if (!current.IsValid(out status)) return false;
            if (current.RootPosition.Frame != rootFrame) { status = ResolvedRenderSnapshotStatus.MixedRootFrame; return false; }
            for (var prior = 0; prior < index; prior++)
                if (objects[prior].Id == current.Id) { status = ResolvedRenderSnapshotStatus.DuplicateObjectId; return false; }
        }

        var copy = objects.ToArray();
        if ((orbitCurve is not null && orbitCurve.RootFrame != rootFrame) || (previousOrbitCurve is not null && previousOrbitCurve.RootFrame != rootFrame) ||
            (bodyForwardIndicator is { } bodyForward && !bodyForward.IsValid(rootFrame)) || (targetDirectionIndicator is { } targetDirection && !targetDirection.IsValid(rootFrame))) { status = ResolvedRenderSnapshotStatus.MixedRootFrame; return false; }
        snapshot = new ResolvedRenderSnapshot(rootFrame, copy, orbitCurve, previousOrbitCurve, bodyForwardIndicator, targetDirectionIndicator);
        status = ResolvedRenderSnapshotStatus.Success;
        return true;
    }
}
