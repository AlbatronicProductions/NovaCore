using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Coordinate identity for the independent nested scale-mesh topology family.
/// The four signed 64-bit integers represent an exact rational cube point;
/// physical identity remains H(normalize(X,Y,Z)).
/// </summary>
public enum PlanetaryNestedScaleMeshCoordinateEncoding : uint
{
    SignedCubeRationalInt64 = 1,
}

/// <summary>Immutable, whole-planet, nested rectilinear scale mesh.</summary>
public sealed class PlanetaryNestedScaleMeshTopology
{
    public const uint Magic = 0x314D534Eu; // NSM1
    public const uint FormatVersion = 1;
    public const uint GeneratorVersion = 1;
    private const int HeaderBytes = 320, VertexBytes = 40, RegionBytes = 64;

    public readonly record struct Vertex(long CubeX, long CubeY, long CubeZ, long Denominator,
        ushort DensityRegion, ushort RefinementStage)
    {
        public Double3 Direction => new Double3(CubeX / (double)Denominator,
            CubeY / (double)Denominator, CubeZ / (double)Denominator).Normalized();
    }

    public readonly record struct DensityRegion(int Identity, int ParentIdentity, int RefinementStage,
        int Ratio, int HalfExtentCells, int VertexCount, int TriangleCount,
        double MinimumRadiusRadians, double MaximumRadiusRadians);

    public readonly record struct GeometricContract(
        double ReferenceRadiusMetres,
        double PlanetOcclusionSupportRadiusMetres,
        double MaximumTesDisplacementMetres,
        double MinimumEdgeMetres,
        double AverageEdgeMetres,
        double MaximumEdgeMetres,
        double MinimumAngularSpanRadians,
        double AverageAngularSpanRadians,
        double MaximumAngularSpanRadians,
        double MaximumChordSagMetres,
        double PupilRadiusRadians,
        double MaximumAltitudeMetres,
        float EntryPixels,
        float ReturnPixels,
        float UrgentPixels,
        float TessellationTargetPixels,
        uint MaximumTesFactor)
    {
        public double OcclusionSupportInsetMetres => ReferenceRadiusMetres - PlanetOcclusionSupportRadiusMetres;
        public bool SupportsDisplacedTriangleOcclusion =>
            MaximumChordSagMetres + MaximumTesDisplacementMetres <= OcclusionSupportInsetMetres + 1e-7d;
    }

    internal PlanetaryNestedScaleMeshTopology(int scale, Vertex[] vertices, uint[] indices,
        int[] neighborOffsets, int[] neighbors, DensityRegion[] regions,
        GeometricContract geometry, ulong topologyHash)
    {
        Scale = scale;
        Vertices = Array.AsReadOnly(vertices.ToArray());
        Indices = Array.AsReadOnly(indices.ToArray());
        NeighborOffsets = Array.AsReadOnly(neighborOffsets.ToArray());
        Neighbors = Array.AsReadOnly(neighbors.ToArray());
        Regions = Array.AsReadOnly(regions.ToArray());
        Geometry = geometry;
        TopologyHash = topologyHash;
    }

    public int Scale { get; }
    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<uint> Indices { get; }
    public IReadOnlyList<int> NeighborOffsets { get; }
    public IReadOnlyList<int> Neighbors { get; }
    public IReadOnlyList<DensityRegion> Regions { get; }
    public GeometricContract Geometry { get; }
    public ulong TopologyHash { get; }
    public int TriangleCount => Indices.Count / 3;
    public ulong ImmutableGpuBytes => checked((ulong)Vertices.Count * 32ul +
        (ulong)Indices.Count * 4ul + (ulong)(NeighborOffsets.Count + Neighbors.Count) * 4ul);

    public static byte[] Serialize(PlanetaryNestedScaleMeshTopology value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateStructure(value);
        var vertexOffset = HeaderBytes;
        var indexOffset = checked(vertexOffset + value.Vertices.Count * VertexBytes);
        var adjacencyOffset = checked(indexOffset + value.Indices.Count * 4);
        var regionOffset = checked(adjacencyOffset +
            (value.NeighborOffsets.Count + value.Neighbors.Count) * 4);
        var totalBytes = checked(regionOffset + value.Regions.Count * RegionBytes);
        var bytes = new byte[totalBytes];

        W32(0, Magic); W32(4, FormatVersion); W32(8, GeneratorVersion);
        W32(12, (uint)PlanetaryNestedScaleMeshCoordinateEncoding.SignedCubeRationalInt64);
        WI32(16, value.Scale); WI32(20, value.Vertices.Count); WI32(24, value.Indices.Count);
        WI32(28, value.NeighborOffsets.Count); WI32(32, value.Neighbors.Count);
        WI32(36, value.Regions.Count); WI32(40, vertexOffset); WI32(44, indexOffset);
        WI32(48, adjacencyOffset); WI32(52, regionOffset); WI32(56, totalBytes);
        WD(88, value.Geometry.ReferenceRadiusMetres);
        WD(96, value.Geometry.PlanetOcclusionSupportRadiusMetres);
        WD(104, value.Geometry.MaximumTesDisplacementMetres);
        WD(112, value.Geometry.MinimumEdgeMetres); WD(120, value.Geometry.AverageEdgeMetres);
        WD(128, value.Geometry.MaximumEdgeMetres); WD(136, value.Geometry.MinimumAngularSpanRadians);
        WD(144, value.Geometry.AverageAngularSpanRadians); WD(152, value.Geometry.MaximumAngularSpanRadians);
        WD(160, value.Geometry.MaximumChordSagMetres); WD(168, value.Geometry.PupilRadiusRadians);
        WD(176, value.Geometry.MaximumAltitudeMetres); WF(184, value.Geometry.EntryPixels);
        WF(188, value.Geometry.ReturnPixels); WF(192, value.Geometry.UrgentPixels);
        WF(196, value.Geometry.TessellationTargetPixels); W32(200, value.Geometry.MaximumTesFactor);

        for (var i = 0; i < value.Vertices.Count; i++)
        {
            var p = vertexOffset + i * VertexBytes; var vertex = value.Vertices[i];
            WI64(p, vertex.CubeX); WI64(p + 8, vertex.CubeY); WI64(p + 16, vertex.CubeZ);
            WI64(p + 24, vertex.Denominator); W16(p + 32, vertex.DensityRegion);
            W16(p + 34, vertex.RefinementStage);
        }
        for (var i = 0; i < value.Indices.Count; i++) W32(indexOffset + i * 4, value.Indices[i]);
        for (var i = 0; i < value.NeighborOffsets.Count; i++) WI32(adjacencyOffset + i * 4, value.NeighborOffsets[i]);
        var neighborStart = adjacencyOffset + value.NeighborOffsets.Count * 4;
        for (var i = 0; i < value.Neighbors.Count; i++) WI32(neighborStart + i * 4, value.Neighbors[i]);
        for (var i = 0; i < value.Regions.Count; i++)
        {
            var p = regionOffset + i * RegionBytes; var region = value.Regions[i];
            WI32(p, region.Identity); WI32(p + 4, region.ParentIdentity); WI32(p + 8, region.RefinementStage);
            WI32(p + 12, region.Ratio); WI32(p + 16, region.HalfExtentCells);
            WI32(p + 20, region.VertexCount); WI32(p + 24, region.TriangleCount);
            WD(p + 32, region.MinimumRadiusRadians); WD(p + 40, region.MaximumRadiusRadians);
        }
        W64(80, ComputeHash(bytes));
        return bytes;

        void W16(int p, ushort x) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(p), x);
        void W32(int p, uint x) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(p), x);
        void WI32(int p, int x) => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(p), x);
        void W64(int p, ulong x) => BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(p), x);
        void WI64(int p, long x) => BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(p), x);
        void WD(int p, double x) => WI64(p, BitConverter.DoubleToInt64Bits(x));
        void WF(int p, float x) => WI32(p, BitConverter.SingleToInt32Bits(x));
    }

    public static PlanetaryNestedScaleMeshTopology Load(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderBytes) throw new InvalidDataException("Nested scale mesh is truncated.");
        var bytes = input.ToArray();
        ushort U16(int p) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(p));
        uint U32(int p) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(p));
        int I32(int p) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(p));
        ulong U64(int p) => BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(p));
        long I64(int p) => BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(p));
        double D(int p) => BitConverter.Int64BitsToDouble(I64(p));
        float F(int p) => BitConverter.Int32BitsToSingle(I32(p));
        if (U32(0) != Magic || U32(4) != FormatVersion || U32(8) != GeneratorVersion ||
            U32(12) != (uint)PlanetaryNestedScaleMeshCoordinateEncoding.SignedCubeRationalInt64)
            throw new InvalidDataException("Nested scale-mesh header is incompatible.");
        var scale = I32(16); var vertexCount = I32(20); var indexCount = I32(24);
        var offsetCount = I32(28); var neighborCount = I32(32); var regionCount = I32(36);
        var vertexOffset = I32(40); var indexOffset = I32(44); var adjacencyOffset = I32(48);
        var regionOffset = I32(52); var totalBytes = I32(56); var expectedHash = U64(80);
        if (scale < 0 || vertexCount <= 0 || indexCount <= 0 || indexCount % 3 != 0 ||
            offsetCount != vertexCount + 1 || neighborCount < 0 || regionCount <= 0 ||
            vertexOffset != HeaderBytes || indexOffset != checked(vertexOffset + vertexCount * VertexBytes) ||
            adjacencyOffset != checked(indexOffset + indexCount * 4) ||
            regionOffset != checked(adjacencyOffset + (offsetCount + neighborCount) * 4) ||
            totalBytes != checked(regionOffset + regionCount * RegionBytes) || totalBytes != bytes.Length ||
            ComputeHash(bytes) != expectedHash)
            throw new InvalidDataException("Nested scale-mesh lengths or hash are invalid.");

        var vertices = new Vertex[vertexCount];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = vertexOffset + i * VertexBytes;
            vertices[i] = new(I64(p), I64(p + 8), I64(p + 16), I64(p + 24), U16(p + 32), U16(p + 34));
        }
        var indices = new uint[indexCount];
        for (var i = 0; i < indices.Length; i++) indices[i] = U32(indexOffset + i * 4);
        var offsets = new int[offsetCount];
        for (var i = 0; i < offsets.Length; i++) offsets[i] = I32(adjacencyOffset + i * 4);
        var neighbors = new int[neighborCount]; var neighborStart = adjacencyOffset + offsetCount * 4;
        for (var i = 0; i < neighbors.Length; i++) neighbors[i] = I32(neighborStart + i * 4);
        var regions = new DensityRegion[regionCount];
        for (var i = 0; i < regions.Length; i++)
        {
            var p = regionOffset + i * RegionBytes;
            regions[i] = new(I32(p), I32(p + 4), I32(p + 8), I32(p + 12), I32(p + 16),
                I32(p + 20), I32(p + 24), D(p + 32), D(p + 40));
        }
        var geometry = new GeometricContract(D(88), D(96), D(104), D(112), D(120), D(128),
            D(136), D(144), D(152), D(160), D(168), D(176), F(184), F(188), F(192), F(196), U32(200));
        var result = new PlanetaryNestedScaleMeshTopology(scale, vertices, indices, offsets, neighbors,
            regions, geometry, expectedHash);
        ValidateStructure(result);
        return result;
    }

    internal static ulong ComputeHash(byte[] source)
    {
        var copy = source.ToArray(); copy.AsSpan(80, 8).Clear();
        return BinaryPrimitives.ReadUInt64LittleEndian(SHA256.HashData(copy));
    }

    internal static void ValidateStructure(PlanetaryNestedScaleMeshTopology value)
    {
        if (value.Scale < 0 || value.Vertices.Count == 0 || value.Indices.Count == 0 ||
            value.Indices.Count % 3 != 0 || value.NeighborOffsets.Count != value.Vertices.Count + 1 ||
            value.NeighborOffsets[0] != 0 || value.NeighborOffsets[^1] != value.Neighbors.Count ||
            value.Regions.Count == 0 || !value.Geometry.SupportsDisplacedTriangleOcclusion)
            throw new InvalidDataException("Invalid nested scale-mesh structure or occlusion contract.");
        foreach (var vertex in value.Vertices)
        {
            if (vertex.Denominator <= 0 || Math.Max(Math.Abs(vertex.CubeX),
                    Math.Max(Math.Abs(vertex.CubeY), Math.Abs(vertex.CubeZ))) != vertex.Denominator ||
                !vertex.Direction.IsFinite)
                throw new InvalidDataException("Invalid rational cube vertex identity.");
        }
        foreach (var index in value.Indices)
            if (index >= value.Vertices.Count) throw new InvalidDataException("Invalid nested scale-mesh index.");
    }
}

public static class PlanetaryNestedScaleMeshTopologyLibrary
{
    public const string ManifestFileName = "nested-scale-mesh-manifest.json";

    public static IReadOnlyList<PlanetaryNestedScaleMeshTopology> Load(string directory)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, ManifestFileName)));
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "NovaCoreNestedScaleMeshTopology" ||
                root.GetProperty("formatVersion").GetUInt32() != PlanetaryNestedScaleMeshTopology.FormatVersion ||
                root.GetProperty("generatorVersion").GetUInt32() != PlanetaryNestedScaleMeshTopology.GeneratorVersion ||
                root.GetProperty("coordinateEncoding").GetString() != nameof(PlanetaryNestedScaleMeshCoordinateEncoding.SignedCubeRationalInt64))
                throw new InvalidDataException("Nested scale-mesh manifest is incompatible.");
            var entries = root.GetProperty("scales");
            if (entries.GetArrayLength() != root.GetProperty("scaleCount").GetInt32())
                throw new InvalidDataException("Nested scale-mesh manifest count is invalid.");
            var result = new List<PlanetaryNestedScaleMeshTopology>(entries.GetArrayLength());
            var expectedScale = 0;
            foreach (var entry in entries.EnumerateArray())
            {
                var scale = entry.GetProperty("scale").GetInt32();
                var file = entry.GetProperty("file").GetString() ?? string.Empty;
                if (scale != expectedScale++ || Path.GetFileName(file) != file)
                    throw new InvalidDataException("Nested scale-mesh manifest ordering is invalid.");
                var bytes = File.ReadAllBytes(Path.Combine(directory, file));
                var topology = PlanetaryNestedScaleMeshTopology.Load(bytes);
                if (bytes.Length != entry.GetProperty("bytes").GetInt32() || topology.Scale != scale ||
                    topology.TopologyHash != ParseHash(entry.GetProperty("topologyHash").GetString()))
                    throw new InvalidDataException("Nested scale-mesh artifact identity differs from its manifest.");
                result.Add(topology);
            }
            return result.AsReadOnly();
        }
        catch (InvalidDataException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or
                                      KeyNotFoundException or FormatException or OverflowException)
        {
            throw new InvalidDataException("Nested scale-mesh library could not be loaded.", error);
        }
    }

    private static ulong ParseHash(string? value)
    {
        if (value is null || !value.StartsWith("0x", StringComparison.Ordinal) || value.Length != 18)
            throw new InvalidDataException("Nested scale-mesh manifest hash is invalid.");
        return Convert.ToUInt64(value[2..], 16);
    }
}
