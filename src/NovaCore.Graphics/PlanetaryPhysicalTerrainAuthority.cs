using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>
/// Narrow adapter from the accepted physical terrain-v5/NCCUBE2 CPU oracle to Core surface
/// authority. Rendering residency, pupil, and topology are not consulted.
/// </summary>
public readonly record struct PlanetaryPhysicalTerrainAuthority(
    ulong BodyId,
    PlanetaryTerrainDefinition Terrain) : IPhysicalTerrainAuthority
{
    public TerrainAuthorityVersion AuthorityVersion => new(Terrain.SourceId, Terrain.Version);
    public bool IsValid => BodyId != 0 && Terrain.IsValid;
    public bool SupportsBody(ulong bodyId) => IsValid && bodyId == BodyId;

    public bool TrySampleHeight(ulong bodyId, in Double3 normalizedBodyFixedDirection, out double heightMetres)
    {
        heightMetres = default;
        if (!SupportsBody(bodyId) || !normalizedBodyFixedDirection.IsFinite ||
            Math.Abs(normalizedBodyFixedDirection.LengthSquared - 1d) > SurfaceAnchor.DirectionUnitLengthSquaredTolerance)
            return false;
        try
        {
            heightMetres = Terrain.SampleHeight(normalizedBodyFixedDirection, 24);
            return double.IsFinite(heightMetres);
        }
        catch (ArgumentOutOfRangeException)
        {
            heightMetres = default;
            return false;
        }
    }
}
