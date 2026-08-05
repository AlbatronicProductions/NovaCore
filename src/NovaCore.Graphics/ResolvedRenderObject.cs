using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Derived root-resolved renderer input. This is never authoritative simulation state.</summary>
public readonly record struct ResolvedRenderObject(
    RenderObjectId Id,
    UniversePosition RootPosition,
    DoubleQuaternion RootOrientation,
    Double3 Scale,
    MeshHandle Mesh)
{
    internal bool IsValid(out ResolvedRenderSnapshotStatus status)
    {
        if (!Id.IsValid) { status = ResolvedRenderSnapshotStatus.InvalidObjectId; return false; }
        if (!RootPosition.Value.IsFinite) { status = ResolvedRenderSnapshotStatus.NonFinitePosition; return false; }
        if (!RootOrientation.IsFinite || Math.Abs(RootOrientation.LengthSquared - 1d) > 1e-10d) { status = ResolvedRenderSnapshotStatus.InvalidOrientation; return false; }
        if (!Scale.IsFinite) { status = ResolvedRenderSnapshotStatus.NonFiniteScale; return false; }
        if (!Mesh.IsValid) { status = ResolvedRenderSnapshotStatus.InvalidMeshHandle; return false; }
        status = ResolvedRenderSnapshotStatus.Success;
        return true;
    }
}
