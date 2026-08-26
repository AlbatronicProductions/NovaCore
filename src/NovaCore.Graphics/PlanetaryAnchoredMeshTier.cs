using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>
/// Canonical body-fixed anchor of a future spherical mesh tier. The existing
/// SurfaceAnchor remains geographic authority; face/UV are deterministic
/// relaxed-cube correspondence derived from that authority, never from camera
/// or narrowed presentation state.
/// </summary>
public readonly record struct PlanetaryAnchoredMeshAnchor(
    SurfaceAnchor SurfaceAnchor,
    CubeSphereFace Face,
    double U,
    double V)
{
    public const double MaximumRoundTripDirectionError = 2e-12d;

    public bool IsValid => SurfaceAnchor.IsValid && double.IsFinite(U) && double.IsFinite(V) &&
        U is >= 0d and <= 1d && V is >= 0d and <= 1d &&
        Math.Sqrt((RelaxedCubeSphereProjection.UnitDirection(Face, U, V) -
            SurfaceAnchor.NormalizedBodyFixedDirection).LengthSquared) <= MaximumRoundTripDirectionError;

    public Double3 BodyFixedDirection => SurfaceAnchor.NormalizedBodyFixedDirection;

    public static bool TryCreate(in SurfaceAnchor anchor, out PlanetaryAnchoredMeshAnchor meshAnchor)
    {
        meshAnchor = default;
        if (!anchor.IsValid || !RelaxedCubeSphereProjection.TryAddress(anchor.NormalizedBodyFixedDirection,
            out var face, out var u, out var v)) return false;
        var value = new PlanetaryAnchoredMeshAnchor(anchor, face, u, v);
        if (!value.IsValid) return false;
        meshAnchor = value;
        return true;
    }

    public ulong DeterministicHash
    {
        get
        {
            var hash = AnchoredMeshHash.OffsetBasis;
            hash = AnchoredMeshHash.Mix(hash, SurfaceAnchor.DeterministicHash);
            hash = AnchoredMeshHash.Mix(hash, (uint)Face);
            hash = AnchoredMeshHash.Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(U));
            return AnchoredMeshHash.Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(V));
        }
    }
}

/// <summary>
/// Reproducible geographic identity for a future near-surface tier. Camera,
/// focus mode, frame number, residency, GPU address, and backend are excluded.
/// The same authoritative SurfaceAnchor/tier always produces the same key.
/// </summary>
public readonly record struct PlanetaryAnchoredMeshTierId(
    PlanetaryAnchoredMeshAnchor Anchor,
    int Tier,
    PlanetarySurfacePatchId AnchorCell,
    uint TopologyVersion)
{
    public const uint CurrentTopologyVersion = 1;
    public const int MaximumTier = 24;

    public bool IsValid
    {
        get
        {
            if (!Anchor.IsValid || Tier is < 0 or > MaximumTier || TopologyVersion == 0 || !AnchorCell.IsValid ||
                AnchorCell.BodyId != Anchor.SurfaceAnchor.BodyId ||
                AnchorCell.TerrainVersion != Anchor.SurfaceAnchor.TerrainAuthorityVersion.Version ||
                AnchorCell.Face != Anchor.Face) return false;
            var scale = 1 << AnchorCell.Level;
            var minimumU = (double)AnchorCell.X / scale; var maximumU = (double)(AnchorCell.X + 1) / scale;
            var minimumV = (double)AnchorCell.Y / scale; var maximumV = (double)(AnchorCell.Y + 1) / scale;
            return Anchor.U >= minimumU - 2e-15d && Anchor.U <= maximumU + 2e-15d &&
                Anchor.V >= minimumV - 2e-15d && Anchor.V <= maximumV + 2e-15d;
        }
    }

    public static bool TryCreate(in SurfaceAnchor anchor, int tier, int anchorCellLevel,
        out PlanetaryAnchoredMeshTierId identity)
    {
        identity = default;
        if (tier is < 0 or > MaximumTier || anchorCellLevel is < 0 or > 24 ||
            !PlanetaryAnchoredMeshAnchor.TryCreate(anchor, out var meshAnchor)) return false;
        var scale = 1 << anchorCellLevel;
        var x = Math.Min(scale - 1, (int)Math.Floor(meshAnchor.U * scale));
        var y = Math.Min(scale - 1, (int)Math.Floor(meshAnchor.V * scale));
        var cell = new PlanetarySurfacePatchId(anchor.BodyId, anchor.TerrainAuthorityVersion.Version,
            meshAnchor.Face, anchorCellLevel, x, y);
        var value = new PlanetaryAnchoredMeshTierId(meshAnchor, tier, cell, CurrentTopologyVersion);
        if (!value.IsValid) return false;
        identity = value;
        return true;
    }

    public ulong DeterministicHash
    {
        get
        {
            var hash = AnchoredMeshHash.OffsetBasis;
            hash = AnchoredMeshHash.Mix(hash, Anchor.DeterministicHash);
            hash = AnchoredMeshHash.Mix(hash, (uint)Tier);
            hash = AnchoredMeshHash.Mix(hash, AnchorCell.BodyId);
            hash = AnchoredMeshHash.Mix(hash, AnchorCell.TerrainVersion);
            hash = AnchoredMeshHash.Mix(hash, (uint)AnchorCell.Face);
            hash = AnchoredMeshHash.Mix(hash, (uint)AnchorCell.Level);
            hash = AnchoredMeshHash.Mix(hash, (uint)AnchorCell.X);
            hash = AnchoredMeshHash.Mix(hash, (uint)AnchorCell.Y);
            return AnchoredMeshHash.Mix(hash, TopologyVersion);
        }
    }
}

/// <summary>
/// Dormant split-FP32 transport for an authoritative body-fixed anchor. It is
/// not geographic authority and has no native ABI or live renderer binding in
/// 11B-7A.
/// </summary>
public readonly record struct PlanetaryAnchoredMeshSplitAnchor(
    EncodedPosition BodyFixedPosition,
    EncodedPosition BodyFixedDirection)
{
    public static PlanetaryAnchoredMeshSplitAnchor Encode(in PlanetaryAnchoredMeshAnchor anchor,
        double physicalSurfaceRadiusMetres)
    {
        if (!anchor.IsValid || !double.IsFinite(physicalSurfaceRadiusMetres) || physicalSurfaceRadiusMetres <= 0d)
            throw new ArgumentOutOfRangeException(nameof(physicalSurfaceRadiusMetres));
        return new(EncodedPosition.Encode(anchor.BodyFixedDirection * physicalSurfaceRadiusMetres),
            EncodedPosition.Encode(anchor.BodyFixedDirection));
    }
}

/// <summary>
/// Exact face-independent rational point on the canonical cube surface. GCD
/// reduction gives parent/child and cross-face vertices one bit-identical key.
/// </summary>
public readonly record struct PlanetaryAnchoredMeshVertexId(
    long X,
    long Y,
    long Z,
    long Denominator) : IComparable<PlanetaryAnchoredMeshVertexId>
{
    public bool IsValid => Denominator > 0 && Math.Max(Math.Abs(X), Math.Max(Math.Abs(Y), Math.Abs(Z))) == Denominator;
    public Double3 CanonicalCubePoint => IsValid
        ? new((double)X / Denominator, (double)Y / Denominator, (double)Z / Denominator)
        : throw new InvalidOperationException("Invalid canonical cube-surface vertex identity.");
    public Double3 BodyFixedDirection => RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(CanonicalCubePoint);

    public bool TryCanonicalAddress(out CubeSphereFace face, out double u, out double v) =>
        RelaxedCubeSphereProjection.TryCubeSurfaceAddress(CanonicalCubePoint, out face, out u, out v);

    public static PlanetaryAnchoredMeshVertexId FromPatchGrid(in PlanetarySurfacePatchId patch,
        int gridX, int gridY, int gridResolution)
    {
        if (!patch.IsValid || gridResolution <= 0 || gridX is < 0 || gridX > gridResolution ||
            gridY is < 0 || gridY > gridResolution) throw new ArgumentOutOfRangeException();
        var cells = 1L << patch.Level;
        var denominator = cells * gridResolution;
        var u = (long)patch.X * gridResolution + gridX;
        var v = (long)patch.Y * gridResolution + gridY;
        var a = 2L * u - denominator; var b = 2L * v - denominator;
        var cube = patch.Face switch
        {
            CubeSphereFace.PositiveX => (denominator, b, -a),
            CubeSphereFace.NegativeX => (-denominator, b, a),
            CubeSphereFace.PositiveY => (a, denominator, -b),
            CubeSphereFace.NegativeY => (a, -denominator, b),
            CubeSphereFace.PositiveZ => (a, b, denominator),
            CubeSphereFace.NegativeZ => (-a, b, -denominator),
            _ => throw new ArgumentOutOfRangeException()
        };
        return Create(cube.Item1, cube.Item2, cube.Item3, denominator);
    }

    public static PlanetaryAnchoredMeshVertexId Create(long x, long y, long z, long denominator)
    {
        if (denominator <= 0 || Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z))) != denominator)
            throw new ArgumentOutOfRangeException(nameof(denominator));
        var divisor = GreatestCommonDivisor(GreatestCommonDivisor(GreatestCommonDivisor(Math.Abs(x), Math.Abs(y)), Math.Abs(z)), denominator);
        return new(x / divisor, y / divisor, z / divisor, denominator / divisor);
    }

    public int CompareTo(PlanetaryAnchoredMeshVertexId other)
    {
        var x = X.CompareTo(other.X); if (x != 0) return x;
        var y = Y.CompareTo(other.Y); if (y != 0) return y;
        var z = Z.CompareTo(other.Z); if (z != 0) return z;
        return Denominator.CompareTo(other.Denominator);
    }

    private static long GreatestCommonDivisor(long a, long b)
    {
        while (b != 0) { var remainder = a % b; a = b; b = remainder; }
        return a == 0 ? 1 : a;
    }
}

/// <summary>Canonical ordered physical edge, independent of incident face and local traversal direction.</summary>
public readonly record struct PlanetaryAnchoredMeshEdgeId(
    PlanetaryAnchoredMeshVertexId First,
    PlanetaryAnchoredMeshVertexId Second)
{
    public bool IsValid => First.IsValid && Second.IsValid && First != Second && First.CompareTo(Second) < 0;

    public static PlanetaryAnchoredMeshEdgeId Create(in PlanetaryAnchoredMeshVertexId a,
        in PlanetaryAnchoredMeshVertexId b) => a.CompareTo(b) <= 0 ? new(a, b) : new(b, a);
}

public readonly record struct PlanetaryAnchoredMeshReferenceVertex(
    PlanetaryAnchoredMeshVertexId Identity,
    CubeSphereFace SourceFace,
    double SourceU,
    double SourceV,
    Double3 BodyFixedDirection,
    Double3 AnchorRelativeUnitChord);

/// <summary>
/// Small bounded CPU reference mesh used only to prove the 11B-7A contract.
/// Its density is deliberately not a production backend decision.
/// </summary>
public sealed class PlanetaryAnchoredMeshReferenceTopology
{
    public const int ProofQuadsPerSide = 4;
    private readonly PlanetaryAnchoredMeshReferenceVertex[] _vertices;
    private readonly uint[] _indices;

    private PlanetaryAnchoredMeshReferenceTopology(in PlanetaryAnchoredMeshTierId identity,
        PlanetaryAnchoredMeshReferenceVertex[] vertices, uint[] indices, ulong deterministicHash)
    {
        Identity = identity; _vertices = vertices; _indices = indices; DeterministicHash = deterministicHash;
    }

    public PlanetaryAnchoredMeshTierId Identity { get; }
    public ReadOnlySpan<PlanetaryAnchoredMeshReferenceVertex> Vertices => _vertices;
    public ReadOnlySpan<uint> Indices => _indices;
    public ulong DeterministicHash { get; }

    public static PlanetaryAnchoredMeshReferenceTopology Create(in PlanetaryAnchoredMeshTierId identity,
        int gridResolution = ProofQuadsPerSide)
    {
        if (!identity.IsValid || gridResolution is < 1 or > 64) throw new ArgumentOutOfRangeException();
        var row = gridResolution + 1;
        var vertices = new PlanetaryAnchoredMeshReferenceVertex[row * row];
        for (var y = 0; y <= gridResolution; y++)
            for (var x = 0; x <= gridResolution; x++)
            {
                var id = PlanetaryAnchoredMeshVertexId.FromPatchGrid(identity.AnchorCell, x, y, gridResolution);
                var coordinate = identity.AnchorCell.Patch.GridCoordinate(x, y, gridResolution);
                var direction = id.BodyFixedDirection;
                vertices[y * row + x] = new(id, identity.AnchorCell.Face, coordinate.U, coordinate.V,
                    direction, direction - identity.Anchor.BodyFixedDirection);
            }
        var indices = new uint[gridResolution * gridResolution * 6]; var index = 0;
        for (var y = 0; y < gridResolution; y++)
            for (var x = 0; x < gridResolution; x++)
            {
                var lowerLeft = (uint)(y * row + x); var lowerRight = lowerLeft + 1;
                var upperLeft = lowerLeft + (uint)row; var upperRight = upperLeft + 1;
                indices[index++] = lowerLeft; indices[index++] = upperRight; indices[index++] = lowerRight;
                indices[index++] = lowerLeft; indices[index++] = upperLeft; indices[index++] = upperRight;
            }
        return new(identity, vertices, indices, Hash(identity, vertices, indices));
    }

    public PlanetaryAnchoredMeshVertexId EdgeVertex(PlanetaryPatchEdge edge, int sample)
    {
        var resolution = (int)Math.Sqrt(_vertices.Length) - 1;
        if (sample is < 0 || sample > resolution) throw new ArgumentOutOfRangeException(nameof(sample));
        var row = resolution + 1;
        return edge switch
        {
            PlanetaryPatchEdge.NegativeU => _vertices[sample * row].Identity,
            PlanetaryPatchEdge.PositiveU => _vertices[sample * row + resolution].Identity,
            PlanetaryPatchEdge.NegativeV => _vertices[sample].Identity,
            PlanetaryPatchEdge.PositiveV => _vertices[resolution * row + sample].Identity,
            _ => throw new ArgumentOutOfRangeException(nameof(edge))
        };
    }

    public PlanetaryAnchoredMeshEdgeId EdgeIdentity(PlanetaryPatchEdge edge)
    {
        var resolution = (int)Math.Sqrt(_vertices.Length) - 1;
        return PlanetaryAnchoredMeshEdgeId.Create(EdgeVertex(edge, 0), EdgeVertex(edge, resolution));
    }

    private static ulong Hash(in PlanetaryAnchoredMeshTierId identity,
        ReadOnlySpan<PlanetaryAnchoredMeshReferenceVertex> vertices, ReadOnlySpan<uint> indices)
    {
        var hash = AnchoredMeshHash.Mix(AnchoredMeshHash.OffsetBasis, identity.DeterministicHash);
        foreach (ref readonly var vertex in vertices)
        {
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Identity.X);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Identity.Y);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Identity.Z);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Identity.Denominator);
        }
        foreach (var value in indices) hash = AnchoredMeshHash.Mix(hash, value);
        return hash;
    }
}

/// <summary>
/// Backend-neutral future subdivision input. Camera projection may determine
/// the quantized demand, but the canonical edge key and resolved shared factor
/// are independent of tessellation/compute/mesh-shader implementation.
/// </summary>
public readonly record struct PlanetaryAnchoredMeshSubdivisionDemand(
    PlanetaryAnchoredMeshEdgeId Edge,
    uint ProjectedLengthQ16,
    uint TargetLengthQ16,
    uint MaximumFactor)
{
    public bool IsValid => Edge.IsValid && TargetLengthQ16 > 0 && MaximumFactor > 0;
    public uint BoundedFactor => !IsValid ? 0u : (uint)Math.Min(MaximumFactor,
        Math.Max(1UL, ((ulong)ProjectedLengthQ16 + TargetLengthQ16 - 1UL) / TargetLengthQ16));

    public static PlanetaryAnchoredMeshSubdivisionDemand FromPixels(in PlanetaryAnchoredMeshEdgeId edge,
        double projectedLengthPixels, double targetLengthPixels, uint maximumFactor)
    {
        if (!edge.IsValid || !double.IsFinite(projectedLengthPixels) || projectedLengthPixels < 0d ||
            !double.IsFinite(targetLengthPixels) || targetLengthPixels <= 0d || maximumFactor == 0)
            throw new ArgumentOutOfRangeException();
        return new(edge, Quantize(projectedLengthPixels), Math.Max(1u, Quantize(targetLengthPixels)), maximumFactor);
    }

    private static uint Quantize(double value) => (uint)Math.Min(uint.MaxValue, Math.Round(value * 65536d, MidpointRounding.ToEven));
}

internal static class AnchoredMeshHash
{
    public const ulong OffsetBasis = 14695981039346656037UL;

    public static ulong Mix(ulong hash, uint value) => Mix(hash, (ulong)value);
    public static ulong Mix(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++)
        {
            hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8;
        }
        return hash;
    }
}
