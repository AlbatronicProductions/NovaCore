using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NovaCore.Graphics;

public enum PlanetaryProductionTopologyFamily : uint
{
    RadialNctop2 = 0,
    NestedScaleMeshNcsm1 = 1,
}

public readonly record struct PlanetaryProductionCullContract(
    PlanetaryProductionTopologyFamily Family,
    double PlanetOcclusionSupportRadiusMetres,
    double MaximumTesDisplacementMetres)
{
    public static PlanetaryProductionCullContract Radial =>
        new(PlanetaryProductionTopologyFamily.RadialNctop2, 0d, 0d);
}

/// <summary>
/// Mechanical bridge from the independent NCSM1 artifact to the existing
/// frame-local pupil/preparation ABI. It does not serialize as NCTOP2 and does
/// not make the old topology format an authority for the candidate.
/// </summary>
public static class PlanetaryNestedScaleMeshRuntimeAdapter
{
    public static (IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> Levels,
        IReadOnlyDictionary<ulong, PlanetaryProductionCullContract> CullContracts) Adapt(
        IReadOnlyList<PlanetaryNestedScaleMeshTopology> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Count != PlanetaryNestedScaleMeshTopologyGenerator.ScaleCount)
            throw new ArgumentException("The complete NCSM1 scale library is required.", nameof(source));

        var levels = new List<PlanetaryProductionSphericalBillboardTopology>(source.Count);
        var contracts = new Dictionary<ulong, PlanetaryProductionCullContract>(source.Count);
        for (var scale = 0; scale < source.Count; scale++)
        {
            var current = source[scale];
            if (current.Scale != scale || current.Vertices.Select(value => value.Denominator).Distinct().Count() != 1)
                throw new InvalidDataException("NCSM1 runtime adaptation requires ordered common-denominator scales.");
            var denominator = checked((int)current.Vertices[0].Denominator);
            var vertices = current.Vertices.Select(value => new PlanetaryProductionSphericalBillboardTopology.Vertex(
                checked((int)value.CubeX), checked((int)value.CubeY), checked((int)value.CubeZ),
                checked((byte)value.DensityRegion), checked((byte)value.RefinementStage))).ToArray();
            var regions = current.Regions.Select(region =>
                new PlanetaryProductionSphericalBillboardTopology.DensityRegion(region.Identity,
                    region.RefinementStage, region.VertexCount, region.TriangleCount,
                    RepresentativeSpacing(current, region.Identity), current.Geometry.MaximumAngularSpanRadians)).ToArray();
            var parent = scale == 0 ? [] : ParentMap(source[scale - 1], current);
            var parentHash = HashParentMap(parent);
            var pupilSpacing = current.Geometry.MinimumEdgeMetres / current.Geometry.ReferenceRadiusMetres;
            var snap = new PlanetaryProductionSphericalBillboardTopology.SnapMetadata(current.TopologyHash,
                pupilSpacing, 8, Math.Max(8, current.Regions[^1].HalfExtentCells),
                Math.Max(1, current.Regions[^1].HalfExtentCells * 2));
            var error = new PlanetaryProductionSphericalBillboardTopology.ErrorMetadata(
                current.Geometry.MaximumAltitudeMetres, pupilSpacing,
                current.Geometry.AverageAngularSpanRadians, current.Geometry.MaximumAngularSpanRadians,
                current.Geometry.MaximumTesDisplacementMetres, current.Geometry.EntryPixels,
                current.Geometry.ReturnPixels, current.Geometry.UrgentPixels, 1f,
                current.Geometry.TessellationTargetPixels,
                Math.Max(1d, current.Geometry.MinimumEdgeMetres * 2d), 64f,
                current.Geometry.MaximumTesFactor, PlanetaryProductionTesResponsibility.NearCamera);
            var adapted = new PlanetaryProductionSphericalBillboardTopology(scale, denominator, vertices,
                current.Indices.ToArray(), current.NeighborOffsets.ToArray(), current.Neighbors.ToArray(),
                regions, parent, snap, error, current.TopologyHash, parentHash);
            levels.Add(adapted);
            contracts.Add(adapted.TopologyHash, new(PlanetaryProductionTopologyFamily.NestedScaleMeshNcsm1,
                current.Geometry.PlanetOcclusionSupportRadiusMetres,
                current.Geometry.MaximumTesDisplacementMetres));
        }
        return (levels.AsReadOnly(), contracts);
    }

    private static double RepresentativeSpacing(PlanetaryNestedScaleMeshTopology topology, int region) =>
        topology.Geometry.MinimumAngularSpanRadians * Math.Pow(2d,
            Math.Max(0, topology.Regions.Count - 1 - region));

    private static int[] ParentMap(PlanetaryNestedScaleMeshTopology parent,
        PlanetaryNestedScaleMeshTopology child)
    {
        var childDenominator = child.Vertices[0].Denominator;
        var lookup = child.Vertices.Select((value, index) => (value, index)).ToDictionary(
            pair => (pair.value.CubeX, pair.value.CubeY, pair.value.CubeZ), pair => pair.index);
        var ratio = checked(childDenominator / parent.Vertices[0].Denominator);
        var result = new int[parent.Vertices.Count];
        for (var index = 0; index < parent.Vertices.Count; index++)
        {
            var value = parent.Vertices[index];
            if (!lookup.TryGetValue((checked(value.CubeX * ratio), checked(value.CubeY * ratio),
                    checked(value.CubeZ * ratio)), out result[index]))
                throw new InvalidDataException($"NCSM1 scale {child.Scale} does not retain parent vertex {index}.");
        }
        return result;
    }

    private static ulong HashParentMap(int[] map)
    {
        if (map.Length == 0) return 0;
        var bytes = new byte[map.Length * sizeof(int)];
        for (var index = 0; index < map.Length; index++)
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * sizeof(int)), map[index]);
        return BinaryPrimitives.ReadUInt64LittleEndian(SHA256.HashData(bytes));
    }
}
