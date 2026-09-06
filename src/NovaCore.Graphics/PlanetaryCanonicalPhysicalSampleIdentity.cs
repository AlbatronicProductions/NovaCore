using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Canonical prepared-sample identity.  A renderer topology may demand this
/// sample, but patch, vertex, pupil, camera, and frame identity never enter the
/// physical cache key.
/// </summary>
public readonly record struct PlanetaryCanonicalPhysicalSampleIdentity(
    ulong BodyId, uint PhysicalGeneration, uint TerrainDataGeneration,
    long DirectionXBits, long DirectionYBits, long DirectionZBits)
{
    public ulong FacilitySupportIdentity { get; init; }
    public static PlanetaryCanonicalPhysicalSampleIdentity Create(in Double3 direction,
        uint physicalGeneration, uint terrainDataGeneration)
    {
        var value = direction.Normalized();
        return new(PlanetaryPhysicalSurface.EarthBodyId, physicalGeneration, terrainDataGeneration,
            BitConverter.DoubleToInt64Bits(value.X), BitConverter.DoubleToInt64Bits(value.Y),
            BitConverter.DoubleToInt64Bits(value.Z))
        { FacilitySupportIdentity=physicalGeneration==4u?NovaCore.Core.Surface.FloridaFacilitySupport.DefinitionIdentity:0ul };
    }
}
