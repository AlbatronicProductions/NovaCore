using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public enum PlanetaryProductionTopologyCoordinateEncoding : uint
{
    SignedCubeLatticeInt32 = 1,
}

public enum PlanetaryProductionTesResponsibility : uint
{
    None = 0,
    ExceptionalEdges = 1,
    BoundedLocal = 2,
    NearCamera = 3,
}

/// <summary>Immutable production spherical-billboard topology. Coordinates are exact
/// signed cube-lattice numerators; physical identity remains H(normalize(cube)).</summary>
public sealed class PlanetaryProductionSphericalBillboardTopology
{
    public const uint Magic = 0x3254424Eu; // NBT2
    public const uint FormatVersion = 2;
    public const uint GeneratorVersion = 1;
    private const int HeaderBytes = 208, VertexBytes = 16, RegionBytes = 32;
    private readonly NativeProductionBillboardLatticeVertex[] _nativeLattice;
    private readonly uint[] _nativeIndices;

    public readonly record struct Vertex(int CubeX, int CubeY, int CubeZ, byte DensityRegion, byte RefinementDepth)
    {
        public Double3 Direction(int latticeScale) => new Double3(CubeX / (double)latticeScale,
            CubeY / (double)latticeScale, CubeZ / (double)latticeScale).Normalized();
    }

    public readonly record struct DensityRegion(int Identity, int RefinementDepth, int VertexCount,
        int TriangleCount, double RepresentativeAngularSpacingRadians, double MaximumAngularEdgeRadians);

    public readonly record struct SnapMetadata(ulong LatticeIdentity, double PupilCellRadians,
        int CandidateShiftMultiple, int OverlapFootprintCells, int EnteringStripCells);

    public readonly record struct ErrorMetadata(double MaximumAltitudeMetres, double PupilSpacingRadians,
        double TransitionSpacingRadians, double OuterSpacingRadians, double DisplacementEnvelopeMetres,
        float EntryPixels, float ReturnPixels, float UrgentPixels, float TesTargetMinimumPixels,
        float TesTargetMaximumPixels, double MinimumRepresentablePhysicalWavelengthMetres,
        float MaximumExpectedBaseErrorPixels, uint MaximumTesFactor, PlanetaryProductionTesResponsibility TesResponsibility);

    internal PlanetaryProductionSphericalBillboardTopology(int level, int latticeScale, Vertex[] vertices,
        uint[] indices, int[] neighborOffsets, int[] neighbors, DensityRegion[] regions, int[] parentVertexMap,
        SnapMetadata snap, ErrorMetadata error, ulong topologyHash, ulong parentMappingHash)
    {
        var immutableVertices=vertices.ToArray();
        _nativeIndices=indices.ToArray();
        _nativeLattice=immutableVertices.Select(vertex=>new NativeProductionBillboardLatticeVertex
        {
            CubeX=vertex.CubeX,CubeY=vertex.CubeY,CubeZ=vertex.CubeZ,
            Metadata=(uint)vertex.DensityRegion|((uint)vertex.RefinementDepth<<8)
        }).ToArray();
        Level = level; LatticeScale = latticeScale; Vertices = Array.AsReadOnly(immutableVertices);
        Indices = Array.AsReadOnly(_nativeIndices); NeighborOffsets = Array.AsReadOnly(neighborOffsets.ToArray());
        Neighbors = Array.AsReadOnly(neighbors.ToArray()); Regions = Array.AsReadOnly(regions.ToArray());
        ParentVertexMap = Array.AsReadOnly(parentVertexMap.ToArray()); Snap = snap; Error = error;
        TopologyHash = topologyHash; ParentMappingHash = parentMappingHash;
    }

    public int Level { get; }
    public int LatticeScale { get; }
    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<uint> Indices { get; }
    public IReadOnlyList<int> NeighborOffsets { get; }
    public IReadOnlyList<int> Neighbors { get; }
    public IReadOnlyList<DensityRegion> Regions { get; }
    public IReadOnlyList<int> ParentVertexMap { get; }
    public SnapMetadata Snap { get; }
    public ErrorMetadata Error { get; }
    public ulong TopologyHash { get; }
    public ulong ParentMappingHash { get; }
    public int TriangleCount => Indices.Count / 3;
    internal NativeProductionBillboardLatticeVertex[] NativeLattice => _nativeLattice;
    internal uint[] NativeIndices => _nativeIndices;
    public ulong ImmutableGpuBytes => checked((ulong)Vertices.Count * 16ul + (ulong)Indices.Count * 4ul +
        (ulong)(NeighborOffsets.Count + Neighbors.Count) * 4ul);

    public static byte[] Serialize(PlanetaryProductionSphericalBillboardTopology value)
    {
        ValidateStructure(value);
        var vertexOffset = HeaderBytes;
        var indexOffset = checked(vertexOffset + value.Vertices.Count * VertexBytes);
        var adjacencyOffset = checked(indexOffset + value.Indices.Count * 4);
        var regionOffset = checked(adjacencyOffset + (value.NeighborOffsets.Count + value.Neighbors.Count) * 4);
        var parentOffset = checked(regionOffset + value.Regions.Count * RegionBytes);
        var totalBytes = checked(parentOffset + value.ParentVertexMap.Count * 4);
        var bytes = new byte[totalBytes]; var b = bytes;
        Write32(0, Magic); Write32(4, FormatVersion); Write32(8, GeneratorVersion);
        Write32(12, (uint)PlanetaryProductionTopologyCoordinateEncoding.SignedCubeLatticeInt32);
        WriteI32(16, value.Level); WriteI32(20, value.LatticeScale); WriteI32(24, value.Vertices.Count);
        WriteI32(28, value.Indices.Count); WriteI32(32, value.NeighborOffsets.Count); WriteI32(36, value.Neighbors.Count);
        WriteI32(40, value.Regions.Count); WriteI32(44, value.ParentVertexMap.Count); WriteI32(48, vertexOffset);
        WriteI32(52, indexOffset); WriteI32(56, adjacencyOffset); WriteI32(60, regionOffset); WriteI32(64, parentOffset);
        WriteI32(68, totalBytes); Write64(88, value.ParentMappingHash); Write64(96, value.Snap.LatticeIdentity);
        WriteDouble(104, value.Error.MaximumAltitudeMetres); WriteDouble(112, value.Error.PupilSpacingRadians);
        WriteDouble(120, value.Error.TransitionSpacingRadians); WriteDouble(128, value.Error.OuterSpacingRadians);
        WriteDouble(136, value.Error.DisplacementEnvelopeMetres); WriteFloat(144, value.Error.EntryPixels);
        WriteFloat(148, value.Error.ReturnPixels); WriteFloat(152, value.Error.UrgentPixels);
        WriteFloat(156, value.Error.TesTargetMinimumPixels); WriteFloat(160, value.Error.TesTargetMaximumPixels);
        Write32(164, value.Error.MaximumTesFactor); Write32(168, (uint)value.Error.TesResponsibility);
        WriteI32(172, value.Snap.CandidateShiftMultiple); WriteI32(176, value.Snap.OverlapFootprintCells);
        WriteI32(180, value.Snap.EnteringStripCells); WriteDouble(184, value.Snap.PupilCellRadians);
        WriteDouble(192, value.Error.MinimumRepresentablePhysicalWavelengthMetres); WriteFloat(200, value.Error.MaximumExpectedBaseErrorPixels);
        for (var i = 0; i < value.Vertices.Count; i++)
        {
            var p = vertexOffset + i * VertexBytes; var v = value.Vertices[i];
            BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(p), v.CubeX); BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(p + 4), v.CubeY);
            BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(p + 8), v.CubeZ); b[p + 12] = v.DensityRegion; b[p + 13] = v.RefinementDepth;
        }
        for (var i = 0; i < value.Indices.Count; i++) BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(indexOffset + i * 4), value.Indices[i]);
        for (var i = 0; i < value.NeighborOffsets.Count; i++) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(adjacencyOffset + i * 4), value.NeighborOffsets[i]);
        var np = adjacencyOffset + value.NeighborOffsets.Count * 4;
        for (var i = 0; i < value.Neighbors.Count; i++) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(np + i * 4), value.Neighbors[i]);
        for (var i = 0; i < value.Regions.Count; i++)
        {
            var p = regionOffset + i * RegionBytes; var r = value.Regions[i]; WriteI32(p, r.Identity); WriteI32(p + 4, r.RefinementDepth);
            WriteI32(p + 8, r.VertexCount); WriteI32(p + 12, r.TriangleCount); WriteDouble(p + 16, r.RepresentativeAngularSpacingRadians);
            WriteDouble(p + 24, r.MaximumAngularEdgeRadians);
        }
        for (var i = 0; i < value.ParentVertexMap.Count; i++) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(parentOffset + i * 4), value.ParentVertexMap[i]);
        Write64(80, ComputeHash(bytes)); return bytes;

        void Write32(int p, uint x) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p), x);
        void WriteI32(int p, int x) => BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(p), x);
        void Write64(int p, ulong x) => BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(p), x);
        void WriteDouble(int p, double x) => BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(p), BitConverter.DoubleToInt64Bits(x));
        void WriteFloat(int p, float x) => BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(p), BitConverter.SingleToInt32Bits(x));
    }

    public static PlanetaryProductionSphericalBillboardTopology Load(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderBytes) throw new InvalidDataException("Production topology is truncated.");
        var bytes = input.ToArray(); var b = bytes; uint U32(int p) => BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(p));
        int I32(int p) => BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(p)); ulong U64(int p) => BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(p));
        double D(int p) => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(p)));
        float F(int p) => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(p)));
        if (U32(0) != Magic || U32(4) != FormatVersion || U32(8) != GeneratorVersion ||
            U32(12) != (uint)PlanetaryProductionTopologyCoordinateEncoding.SignedCubeLatticeInt32)
            throw new InvalidDataException("Production topology header is incompatible.");
        var level = I32(16); var scale = I32(20); var vc = I32(24); var ic = I32(28); var oc = I32(32); var nc = I32(36);
        var rc = I32(40); var pc = I32(44); var vo = I32(48); var io = I32(52); var ao = I32(56); var ro = I32(60); var po = I32(64);
        var total = I32(68); var expected = U64(80);
        if (scale <= 0 || vc <= 0 || ic <= 0 || ic % 3 != 0 || oc != vc + 1 || nc < 0 || rc <= 0 || pc < 0 ||
            vo != HeaderBytes || io != checked(vo + vc * VertexBytes) || ao != checked(io + ic * 4) ||
            ro != checked(ao + (oc + nc) * 4) || po != checked(ro + rc * RegionBytes) || total != checked(po + pc * 4) || total != input.Length ||
            ComputeHash(bytes) != expected) throw new InvalidDataException("Production topology lengths or hash are invalid.");
        var vertices = new Vertex[vc]; for (var i = 0; i < vc; i++) { var p = vo + i * VertexBytes; vertices[i] = new(I32(p), I32(p + 4), I32(p + 8), b[p + 12], b[p + 13]); }
        var indices = new uint[ic]; for (var i = 0; i < ic; i++) indices[i] = U32(io + i * 4);
        var offsets = new int[oc]; for (var i = 0; i < oc; i++) offsets[i] = I32(ao + i * 4);
        var neighbors = new int[nc]; var nstart = ao + oc * 4; for (var i = 0; i < nc; i++) neighbors[i] = I32(nstart + i * 4);
        var regions = new DensityRegion[rc]; for (var i = 0; i < rc; i++) { var p = ro + i * RegionBytes; regions[i] = new(I32(p), I32(p + 4), I32(p + 8), I32(p + 12), D(p + 16), D(p + 24)); }
        var parent = new int[pc]; for (var i = 0; i < pc; i++) parent[i] = I32(po + i * 4);
        var snap = new SnapMetadata(U64(96), D(184), I32(172), I32(176), I32(180));
        var error = new ErrorMetadata(D(104), D(112), D(120), D(128), D(136), F(144), F(148), F(152), F(156), F(160), D(192), F(200), U32(164), (PlanetaryProductionTesResponsibility)U32(168));
        var result = new PlanetaryProductionSphericalBillboardTopology(level, scale, vertices, indices, offsets, neighbors, regions, parent, snap, error, expected, U64(88));
        ValidateStructure(result); return result;
    }

    internal static ulong ComputeHash(byte[] source)
    {
        var copy = source.ToArray(); copy.AsSpan(80, 8).Clear();
        return BinaryPrimitives.ReadUInt64LittleEndian(SHA256.HashData(copy));
    }

    private static void ValidateStructure(PlanetaryProductionSphericalBillboardTopology value)
    {
        if (value.LatticeScale <= 0 || value.Vertices.Count == 0 || value.Indices.Count == 0 || value.Indices.Count % 3 != 0 ||
            value.NeighborOffsets.Count != value.Vertices.Count + 1 || value.NeighborOffsets[0] != 0 || value.NeighborOffsets[^1] != value.Neighbors.Count)
            throw new InvalidDataException("Invalid production topology structure.");
        foreach (var index in value.Indices) if (index >= value.Vertices.Count) throw new InvalidDataException("Invalid production topology index.");
    }
}

/// <summary>Loads the authored v2 library and never constructs topology or adjacency.</summary>
public static class PlanetaryProductionSphericalBillboardTopologyLibrary
{
    public static IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> Load(string directory)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "production-manifest.json")));
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "NovaCoreProductionSphericalBillboardTopology" ||
                root.GetProperty("formatVersion").GetUInt32() != PlanetaryProductionSphericalBillboardTopology.FormatVersion ||
                root.GetProperty("generatorVersion").GetUInt32() != PlanetaryProductionSphericalBillboardTopology.GeneratorVersion ||
                root.GetProperty("coordinateEncoding").GetString() != nameof(PlanetaryProductionTopologyCoordinateEncoding.SignedCubeLatticeInt32))
                throw new InvalidDataException("Production topology manifest is incompatible.");
            var entries = root.GetProperty("levels"); var declaredCount = root.GetProperty("levelCount").GetInt32();
            if (declaredCount <= 0 || entries.GetArrayLength() != declaredCount) throw new InvalidDataException("Production topology manifest level count is invalid.");
            var result = new List<PlanetaryProductionSphericalBillboardTopology>(declaredCount); var expectedLevel = 0;
            foreach (var entry in entries.EnumerateArray())
            {
                var level = entry.GetProperty("level").GetInt32(); var file = entry.GetProperty("file").GetString() ?? "";
                if (level != expectedLevel++ || Path.GetFileName(file) != file) throw new InvalidDataException("Production topology manifest ordering or file name is invalid.");
                var bytes = File.ReadAllBytes(Path.Combine(directory, file));
                if (bytes.Length != entry.GetProperty("bytes").GetInt32()) throw new InvalidDataException("Production topology artifact length differs from its manifest.");
                var topology = PlanetaryProductionSphericalBillboardTopology.Load(bytes);
                if (topology.Level != level || topology.TopologyHash != ParseHash(entry.GetProperty("topologyHash").GetString()) ||
                    topology.ParentMappingHash != ParseHash(entry.GetProperty("parentMappingHash").GetString()))
                    throw new InvalidDataException("Production topology artifact identity differs from its manifest.");
                result.Add(topology);
            }
            return result.AsReadOnly();
        }
        catch (InvalidDataException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new InvalidDataException("Production topology library could not be loaded.", error);
        }
    }

    private static ulong ParseHash(string? value)
    {
        if (value is null || !value.StartsWith("0x", StringComparison.Ordinal) || value.Length != 18) throw new InvalidDataException("Production topology manifest hash is invalid.");
        return Convert.ToUInt64(value[2..], 16);
    }
}
