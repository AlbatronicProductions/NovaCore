using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Bounded dormant 11B-7C whole-body mesh. Canonical cube-surface vertex keys
/// deduplicate all face edges and corners before adjacency is built, so physical
/// normal neighborhoods are face-independent. This is validation topology,
/// not live terrain ownership or a production density decision.
/// </summary>
public sealed class PlanetaryDormantDisplacedMesh
{
    public const int ProofQuadsPerFaceSide = 4;
    public const uint TopologyVersion = 2;
    public const int MaximumProofSubdivisionFactor = 4;
    private readonly PlanetaryAnchoredMeshVertexId[] _vertices;
    private readonly uint[] _indices;
    private readonly uint[] _adjacencyWords;
    private readonly uint[] _faceGridIndices;

    private PlanetaryDormantDisplacedMesh(PlanetaryAnchoredMeshVertexId[] vertices, uint[] indices,
        uint[] adjacencyWords, uint[] faceGridIndices, int quadsPerFaceSide, ulong deterministicHash)
    {
        _vertices = vertices; _indices = indices; _adjacencyWords = adjacencyWords;
        _faceGridIndices = faceGridIndices; QuadsPerFaceSide = quadsPerFaceSide;
        DeterministicHash = deterministicHash;
    }

    public ReadOnlySpan<PlanetaryAnchoredMeshVertexId> Vertices => _vertices;
    public ReadOnlySpan<uint> Indices => _indices;
    public ReadOnlySpan<uint> AdjacencyWords => _adjacencyWords;
    public int AdjacencyCount => _adjacencyWords.Length - _vertices.Length * 2;
    public int QuadsPerFaceSide { get; }
    public int SubdivisionFactor => QuadsPerFaceSide / ProofQuadsPerFaceSide;
    public ulong DeterministicHash { get; }

    public uint FaceVertex(CubeSphereFace face, int x, int y)
    {
        var row = QuadsPerFaceSide + 1;
        if ((uint)face >= 6 || x is < 0 || x > QuadsPerFaceSide || y is < 0 || y > QuadsPerFaceSide)
            throw new ArgumentOutOfRangeException();
        return _faceGridIndices[(int)face * row * row + y * row + x];
    }

    public static PlanetaryDormantDisplacedMesh Create(int subdivisionFactor = 1)
    {
        if (subdivisionFactor is < 1 or > MaximumProofSubdivisionFactor ||
            (subdivisionFactor & (subdivisionFactor - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(subdivisionFactor));
        var resolution = checked(ProofQuadsPerFaceSide * subdivisionFactor); var row = resolution + 1;
        var vertices = new List<PlanetaryAnchoredMeshVertexId>(6 * resolution * resolution + 2);
        var lookup = new Dictionary<PlanetaryAnchoredMeshVertexId, uint>();
        var faceGrid = new uint[6 * row * row];
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        {
            var patch = new PlanetarySurfacePatchId(6, 5, face, 0, 0, 0);
            for (var y = 0; y <= resolution; y++) for (var x = 0; x <= resolution; x++)
            {
                var id = PlanetaryAnchoredMeshVertexId.FromPatchGrid(patch, x, y, resolution);
                if (!lookup.TryGetValue(id, out var index)) { index = (uint)vertices.Count; lookup.Add(id, index); vertices.Add(id); }
                faceGrid[(int)face * row * row + y * row + x] = index;
            }
        }
        var indices = new uint[6 * resolution * resolution * 6]; var cursor = 0;
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            for (var y = 0; y < resolution; y++) for (var x = 0; x < resolution; x++)
            {
                var lowerLeft = faceGrid[(int)face * row * row + y * row + x];
                var lowerRight = faceGrid[(int)face * row * row + y * row + x + 1];
                var upperLeft = faceGrid[(int)face * row * row + (y + 1) * row + x];
                var upperRight = faceGrid[(int)face * row * row + (y + 1) * row + x + 1];
                AddOutward(lowerLeft, upperRight, lowerRight);
                AddOutward(lowerLeft, upperLeft, upperRight);
            }
        var incidence = new List<uint>[vertices.Count]; for (var index = 0; index < incidence.Length; index++) incidence[index] = [];
        for (uint triangle = 0; triangle < indices.Length / 3; triangle++)
            for (var corner = 0; corner < 3; corner++) incidence[indices[triangle * 3 + corner]].Add(triangle);
        var adjacencyCount = incidence.Sum(value => value.Count);
        var adjacency = new uint[vertices.Count * 2 + adjacencyCount]; var start = 0;
        for (var vertex = 0; vertex < vertices.Count; vertex++)
        {
            adjacency[vertex * 2] = (uint)start; adjacency[vertex * 2 + 1] = (uint)incidence[vertex].Count;
            foreach (var triangle in incidence[vertex]) adjacency[vertices.Count * 2 + start++] = triangle;
        }
        return new(vertices.ToArray(), indices, adjacency, faceGrid, resolution,
            Hash(vertices, indices, adjacency));

        void AddOutward(uint a, uint b, uint c)
        {
            var p0 = vertices[(int)a].BodyFixedDirection; var p1 = vertices[(int)b].BodyFixedDirection;
            var p2 = vertices[(int)c].BodyFixedDirection;
            if (Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) < 0d)
                (b, c) = (c, b);
            indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = c;
        }
    }

    private static ulong Hash(IEnumerable<PlanetaryAnchoredMeshVertexId> vertices,
        ReadOnlySpan<uint> indices, ReadOnlySpan<uint> adjacency)
    {
        var hash = AnchoredMeshHash.Mix(AnchoredMeshHash.OffsetBasis, TopologyVersion);
        foreach (var vertex in vertices)
        {
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.X); hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Y);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Z); hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Denominator);
        }
        foreach (var value in indices) hash = AnchoredMeshHash.Mix(hash, value);
        foreach (var value in adjacency) hash = AnchoredMeshHash.Mix(hash, value);
        return hash;
    }
}
