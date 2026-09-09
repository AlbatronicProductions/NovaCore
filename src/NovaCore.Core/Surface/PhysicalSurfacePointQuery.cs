namespace NovaCore.Core.Surface;

/// <summary>Zero/default is never an authoritative result.</summary>
public enum PhysicalSurfaceQueryStatus : byte
{
    RequiredDataNotReady,
    Ready,
    InvalidInput,
    UnsupportedBody,
    AuthorityUnavailable,
    AuthorityMismatch,
    NormalUnqualified,
}

/// <summary>
/// Immutable physical authority, independent of render generations. Dataset digests identify
/// the published CPU bytes; composition and support identities identify the full physical H.
/// QueryPolicyVersion identifies the numerical normal qualification policy.
/// </summary>
public readonly record struct PhysicalSurfaceAuthorityIdentity(
    ulong BodyId, TerrainAuthorityVersion Terrain, uint PhysicalGeneration,
    double ReferenceRadiusMetres, string GlobalSha256, string RegionalSha256,
    ulong CompositionIdentity, ulong FacilitySupportIdentity, uint QueryPolicyVersion);

/// <summary>
/// Natural physical terrain, including canonical grading, never an authored pad collider.
/// Only Ready makes the position/height/normal authoritative. NormalSampleDistanceMetres
/// records the fine stencil scale for independent numerical verification, not contact extent.
/// </summary>
public readonly record struct PhysicalSurfacePointResult(
    PhysicalSurfaceQueryStatus Status, PhysicalSurfaceAuthorityIdentity Authority,
    Double3 BodyFixedDirection, Double3 BodyFixedPositionMetres, double HeightMetres,
    Double3 PhysicalNormal, double NormalSampleDistanceMetres)
{
    public bool IsReady => Status == PhysicalSurfaceQueryStatus.Ready;
}

/// <summary>
/// A ready immutable CPU authority acquired by composition and supplied to gameplay.
/// Queries perform no I/O, streaming, GPU work, or allocation. Failure never supplies a
/// substitute normal. Dataset replacement requires acquisition of a new authority.
/// </summary>
public interface IPhysicalSurfacePointQuery
{
    PhysicalSurfaceAuthorityIdentity Authority { get; }
    PhysicalSurfacePointResult Query(ulong bodyId, in Double3 bodyFixedUnitDirection);
}
