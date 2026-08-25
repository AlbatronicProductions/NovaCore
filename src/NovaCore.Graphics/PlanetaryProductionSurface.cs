using NovaCore.Core;
using System.Buffers.Binary;

namespace NovaCore.Graphics;

/// <summary>The sole authoritative Earth terrain renderer.</summary>
public enum PlanetarySurfaceRendererMode : uint
{
    ProductionCubeSphere = 2
}

public static class PlanetaryProductionSamplingPolicy
{
    public const float TargetTexelPixels = 1.5f;
}

/// <summary>
/// Explicit presentation-only eligibility for a production spherical surface.
/// A body is supported only when its stable identity, physical configuration,
/// terrain contract, and checked dataset identity all agree. Merely being a
/// spherical celestial body never implies production-terrain eligibility.
/// </summary>
public readonly record struct PlanetaryProductionSurfaceEligibility(
    ulong BodyId,
    double PhysicalRadiusMetres,
    uint TerrainSourceId,
    uint TerrainVersion,
    string DatasetIdentity)
{
    public bool IsValid => BodyId != 0 && double.IsFinite(PhysicalRadiusMetres) && PhysicalRadiusMetres > 0d &&
        TerrainSourceId != 0 && TerrainVersion != 0 && !string.IsNullOrWhiteSpace(DatasetIdentity);

    public bool Supports(ulong bodyId, double physicalRadiusMetres, in PlanetaryTerrainDefinition terrain) =>
        IsValid && bodyId == BodyId && BitConverter.DoubleToInt64Bits(physicalRadiusMetres) == BitConverter.DoubleToInt64Bits(PhysicalRadiusMetres) &&
        terrain.IsValid && terrain.SourceId == TerrainSourceId && terrain.Version == TerrainVersion;
}

/// <summary>
/// Stable production identity for one body-fixed curved surface patch. Camera,
/// frame, residency slot, GPU address, and representation mode are deliberately
/// absent: none of them changes the physical region represented by this key.
/// </summary>
public readonly record struct PlanetarySurfacePatchId(
    ulong BodyId,
    uint TerrainVersion,
    CubeSphereFace Face,
    int Level,
    int X,
    int Y) : IComparable<PlanetarySurfacePatchId>
{
    public bool IsValid
    {
        get
        {
            if (BodyId == 0 || TerrainVersion == 0 || Level is < 0 or > 24) return false;
            var size = 1 << Level;
            return X >= 0 && Y >= 0 && X < size && Y < size;
        }
    }

    public PlanetaryPatch Patch => IsValid ? new(Face, Level, X, Y) : throw new InvalidOperationException("Invalid production surface patch identity.");
    public PlanetarySurfacePatchId? Parent => Level == 0 ? null : this with { Level = Level - 1, X = X >> 1, Y = Y >> 1 };

    public PlanetarySurfacePatchId Child(int childIndex)
    {
        if (!IsValid || childIndex is < 0 or > 3 || Level == 24) throw new ArgumentOutOfRangeException(nameof(childIndex));
        return this with { Level = Level + 1, X = (X << 1) + (childIndex & 1), Y = (Y << 1) + (childIndex >> 1) };
    }

    public int CompareTo(PlanetarySurfacePatchId other)
    {
        var body = BodyId.CompareTo(other.BodyId); if (body != 0) return body;
        var version = TerrainVersion.CompareTo(other.TerrainVersion); if (version != 0) return version;
        var patch = Patch.CompareTo(other.Patch); return patch;
    }
}

/// <summary>
/// Deterministic relaxed (spherified) cube mapping. All face boundaries are
/// evaluated through the same symmetric cube-to-sphere equation.
/// </summary>
public static class RelaxedCubeSphereProjection
{
    public const uint AlgorithmVersion = 1;

    public static Double3 Project(CubeSphereFace face, double u, double v, double radius)
    {
        if (!double.IsFinite(u) || !double.IsFinite(v) || u is < 0d or > 1d || v is < 0d or > 1d ||
            !double.IsFinite(radius) || radius <= 0d) throw new ArgumentOutOfRangeException();
        return ProjectCube(CubePoint(face, u, v), radius);
    }

    /// <summary>
    /// Projects gutter coordinates through the canonical neighboring cube
    /// face. This is the offline data-builder path that makes edge filtering
    /// sample one continuous spherical function rather than clamped pages.
    /// </summary>
    public static Double3 ProjectExtended(CubeSphereFace face, double u, double v, double radius)
    {
        if (!double.IsFinite(u) || !double.IsFinite(v) || !double.IsFinite(radius) || radius <= 0d) throw new ArgumentOutOfRangeException();
        var cube = CubePoint(face, u, v); var scale = Math.Max(Math.Abs(cube.X), Math.Max(Math.Abs(cube.Y), Math.Abs(cube.Z)));
        return ProjectCube(cube / scale, radius);
    }

    private static Double3 ProjectCube(in Double3 cube, double radius)
    {
        var x2 = cube.X * cube.X; var y2 = cube.Y * cube.Y; var z2 = cube.Z * cube.Z;
        var x = cube.X * Math.Sqrt(Math.Max(0d, 1d - .5d * (y2 + z2) + y2 * z2 / 3d));
        var y = cube.Y * Math.Sqrt(Math.Max(0d, 1d - .5d * (z2 + x2) + z2 * x2 / 3d));
        var z = cube.Z * Math.Sqrt(Math.Max(0d, 1d - .5d * (x2 + y2) + x2 * y2 / 3d));
        return new Double3(x * radius, y * radius, z * radius);
    }

    public static Double3 UnitDirection(CubeSphereFace face, double u, double v) => Project(face, u, v, 1d);

    /// <summary>
    /// Inverse of the accepted relaxed cube projection. The dominant cube face
    /// is canonical and Newton refinement recovers the face coordinate used by
    /// GPU production addressing without introducing longitude/latitude page identity.
    /// </summary>
    public static bool TryAddress(in Double3 unitDirection, out CubeSphereFace face, out double u, out double v)
    {
        face = default; u = v = 0d;
        if (!unitDirection.IsFinite || unitDirection.LengthSquared <= 0d) return false;
        var direction = unitDirection.Normalized(); var absolute = new Double3(Math.Abs(direction.X), Math.Abs(direction.Y), Math.Abs(direction.Z));
        face = absolute.X >= absolute.Y && absolute.X >= absolute.Z
            ? direction.X >= 0d ? CubeSphereFace.PositiveX : CubeSphereFace.NegativeX
            : absolute.Y >= absolute.Z
                ? direction.Y >= 0d ? CubeSphereFace.PositiveY : CubeSphereFace.NegativeY
                : direction.Z >= 0d ? CubeSphereFace.PositiveZ : CubeSphereFace.NegativeZ;
        var target = FaceCoordinates(face, direction); var a = Math.Clamp(target.U, -1d, 1d); var b = Math.Clamp(target.V, -1d, 1d);
        const double epsilon = 1e-6d;
        for (var iteration = 0; iteration < 8; iteration++)
        {
            var value = FaceCoordinates(face, ProjectCube(CubePoint(face, (a + 1d) * .5d, (b + 1d) * .5d), 1d).Normalized());
            var valueA = FaceCoordinates(face, ProjectCube(CubePoint(face, (a + epsilon + 1d) * .5d, (b + 1d) * .5d), 1d).Normalized());
            var valueB = FaceCoordinates(face, ProjectCube(CubePoint(face, (a + 1d) * .5d, (b + epsilon + 1d) * .5d), 1d).Normalized());
            var duA = (valueA.U - value.U) / epsilon; var dvA = (valueA.V - value.V) / epsilon;
            var duB = (valueB.U - value.U) / epsilon; var dvB = (valueB.V - value.V) / epsilon;
            var determinant = duA * dvB - dvA * duB;
            if (Math.Abs(determinant) < 1e-14d) break;
            var errorU = value.U - target.U; var errorV = value.V - target.V;
            a = Math.Clamp(a - (errorU * dvB - errorV * duB) / determinant, -1d, 1d);
            b = Math.Clamp(b - (duA * errorV - dvA * errorU) / determinant, -1d, 1d);
        }
        u = a * .5d + .5d; v = b * .5d + .5d;
        return double.IsFinite(u) && double.IsFinite(v);
    }

    public static Double3 PatchPoint(in PlanetarySurfacePatchId id, int gridX, int gridY, int gridResolution = PlanetaryPatchTopology.QuadsPerSide)
    {
        if (!id.IsValid) throw new ArgumentOutOfRangeException(nameof(id));
        var coordinate = id.Patch.GridCoordinate(gridX, gridY, gridResolution);
        return UnitDirection(id.Face, coordinate.U, coordinate.V);
    }

    private static Double3 CubePoint(CubeSphereFace face, double u, double v)
    {
        var a = 2d * u - 1d; var b = 2d * v - 1d;
        return face switch
        {
            CubeSphereFace.PositiveX => new(1d, b, -a),
            CubeSphereFace.NegativeX => new(-1d, b, a),
            CubeSphereFace.PositiveY => new(a, 1d, -b),
            CubeSphereFace.NegativeY => new(a, -1d, b),
            CubeSphereFace.PositiveZ => new(a, b, 1d),
            CubeSphereFace.NegativeZ => new(-a, b, -1d),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }

    private static (double U, double V) FaceCoordinates(CubeSphereFace face, in Double3 direction) => face switch
    {
        CubeSphereFace.PositiveX => (-direction.Z / direction.X, direction.Y / direction.X),
        CubeSphereFace.NegativeX => (direction.Z / -direction.X, direction.Y / -direction.X),
        CubeSphereFace.PositiveY => (direction.X / direction.Y, -direction.Z / direction.Y),
        CubeSphereFace.NegativeY => (direction.X / -direction.Y, direction.Z / -direction.Y),
        CubeSphereFace.PositiveZ => (direction.X / direction.Z, direction.Y / direction.Z),
        CubeSphereFace.NegativeZ => (-direction.X / -direction.Z, direction.Y / -direction.Z),
        _ => throw new ArgumentOutOfRangeException(nameof(face))
    };
}

/// <summary>Versioned deterministic geometry contract shared by every production patch.</summary>
public sealed class PlanetaryProductionPatchTopology
{
    private PlanetaryProductionPatchTopology()
    {
        DeterministicHash = Hash();
    }

    public static PlanetaryProductionPatchTopology Shared { get; } = new();
    public int GridResolution => PlanetaryPatchTopology.QuadsPerSide;
    public ReadOnlySpan<PlanetaryPatchTopology.Vertex> Vertices => PlanetaryPatchTopology.Shared.Vertices;
    public ReadOnlySpan<uint> Indices => PlanetaryPatchTopology.Shared.Indices;
    public ulong DeterministicHash { get; }

    private static ulong Hash()
    {
        var hash = 14695981039346656037ul;
        Mix(RelaxedCubeSphereProjection.AlgorithmVersion);
        Mix(PlanetaryPatchTopology.QuadsPerSide);
        var source = PlanetaryPatchTopology.Shared.DeterministicHash;
        Mix((uint)source); Mix((uint)(source >> 32));
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            for (var y = 0; y <= PlanetaryPatchTopology.QuadsPerSide; y++)
                for (var x = 0; x <= PlanetaryPatchTopology.QuadsPerSide; x++)
                {
                    var direction = RelaxedCubeSphereProjection.UnitDirection(face,
                        (double)x / PlanetaryPatchTopology.QuadsPerSide,
                        (double)y / PlanetaryPatchTopology.QuadsPerSide);
                    MixDouble(direction.X); MixDouble(direction.Y); MixDouble(direction.Z);
                }
        return hash;

        void Mix(uint value) => hash = (hash ^ value) * 1099511628211ul;
        void MixDouble(double value)
        {
            var bits = (ulong)BitConverter.DoubleToInt64Bits(value);
            Mix((uint)bits); Mix((uint)(bits >> 32));
        }
    }
}

[Flags]
public enum PlanetarySurfacePatchPayload : byte
{
    None = 0,
    Geometry = 1,
    Elevation = 2,
    Material = 4,
    Classification = 8,
    ProductionRequired = Geometry | Elevation | Material | Classification
}

public enum PlanetarySurfacePatchResidency : byte
{
    Missing,
    Preparing,
    Resident,
    Authoritative
}

/// <summary>
/// Patch-aligned offline data address. One ordinal identifies one curved
/// region; channel payloads are dependencies of that region, not independent
/// visible pages.
/// </summary>
public readonly record struct PlanetaryCubeSurfaceTileAddress(
    PlanetarySurfacePatchId Patch,
    PlanetarySurfacePatchPayload Channel)
{
    public bool IsValid => Patch.IsValid && Channel is PlanetarySurfacePatchPayload.Elevation or PlanetarySurfacePatchPayload.Material or PlanetarySurfacePatchPayload.Classification;
    public ulong PatchOrdinal => PlanetaryCubeSurfacePackContract.PatchOrdinal(Patch.Face, Patch.Level, Patch.X, Patch.Y);
}

/// <summary>On-disk production pack contract generated offline from lawful source data.</summary>
public static class PlanetaryCubeSurfacePackContract
{
    public const ulong Magic = 0x003145425543434Eul; // "NCCUBE1\0", little endian
    public const uint Version = 1;
    public const int InteriorTexels = 256;
    public const int SeamGutterTexels = 4;
    public const int StoredExtent = InteriorTexels + 2 * SeamGutterTexels;
    public const int MaximumLevel = 8;
    public const int HeaderBytes = 256;
    public const int RecordHeaderBytes = 96;

    public static ulong PatchOrdinal(CubeSphereFace face, int level, int x, int y)
    {
        if (level is < 0 or > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
        var size = 1 << level;
        if (x < 0 || y < 0 || x >= size || y >= size) throw new ArgumentOutOfRangeException();
        ulong preceding = 0;
        for (var previous = 0; previous < level; previous++) preceding += 6ul << (2 * previous);
        return preceding + (ulong)face * (ulong)(size * size) + Morton(x, y);
    }

    public static ulong PatchCountThroughLevel(int maximumLevel)
    {
        if (maximumLevel is < 0 or > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(maximumLevel));
        ulong count = 0; for (var level = 0; level <= maximumLevel; level++) count += 6ul << (2 * level);
        return count;
    }

    private static ulong Morton(int x, int y)
    {
        ulong value = 0;
        for (var bit = 0; bit < 24; bit++)
        {
            value |= ((ulong)(x >> bit) & 1ul) << (2 * bit);
            value |= ((ulong)(y >> bit) & 1ul) << (2 * bit + 1);
        }
        return value;
    }

    public static bool TryReadHeader(ReadOnlySpan<byte> bytes, out PlanetaryCubeSurfacePackHeader header)
    {
        header = default;
        if (bytes.Length < 256 || BinaryPrimitives.ReadUInt64LittleEndian(bytes) != Magic) return false;
        header = new(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[36..]));
        return header.IsValid;
    }

    public static bool TryReadRecordHeader(ReadOnlySpan<byte> bytes, out PlanetaryCubeSurfaceRecordHeader header)
    {
        header = default;
        if (bytes.Length < RecordHeaderBytes) return false;
        var bodyId = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        var terrainVersion = BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]);
        var face = (CubeSphereFace)bytes[12];
        var level = bytes[13];
        var patch = new PlanetarySurfacePatchId(
            bodyId,
            terrainVersion,
            face,
            level,
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..])),
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..])));
        header = new(
            patch,
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[36..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[40..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[44..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[48..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[56..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[72..]));
        return header.IsValid;
    }
}

public readonly record struct PlanetaryCubeSurfacePackHeader(
    uint Version,
    uint HeaderBytes,
    uint InteriorTexels,
    uint GutterTexels,
    uint StoredExtent,
    uint MaximumLevel,
    uint RecordCount,
    uint TerrainVersion)
{
    public bool IsValid => Version == PlanetaryCubeSurfacePackContract.Version && HeaderBytes == PlanetaryCubeSurfacePackContract.HeaderBytes &&
        InteriorTexels > 0 && GutterTexels > 0 && StoredExtent == InteriorTexels + 2 * GutterTexels &&
        MaximumLevel <= PlanetaryCubeSurfacePackContract.MaximumLevel &&
        RecordCount == PlanetaryCubeSurfacePackContract.PatchCountThroughLevel((int)MaximumLevel) && TerrainVersion != 0;

    public bool IsProductionLayout => IsValid && InteriorTexels == PlanetaryCubeSurfacePackContract.InteriorTexels &&
        GutterTexels == PlanetaryCubeSurfacePackContract.SeamGutterTexels && StoredExtent == PlanetaryCubeSurfacePackContract.StoredExtent;
}

/// <summary>
/// Fixed record metadata. The four channel lengths and one digest cover the
/// complete patch payload, so no texture or elevation channel can independently
/// claim visible ownership.
/// </summary>
public readonly record struct PlanetaryCubeSurfaceRecordHeader(
    PlanetarySurfacePatchId Patch,
    ulong PatchOrdinal,
    uint AlbedoBytes,
    uint ElevationBytes,
    uint LandMaskBytes,
    uint CloudBytes,
    ulong Digest0,
    ulong Digest1,
    ulong Digest2,
    ulong Digest3)
{
    public bool IsValid => Patch.IsValid && Patch.Level <= PlanetaryCubeSurfacePackContract.MaximumLevel &&
        PatchOrdinal == PlanetaryCubeSurfacePackContract.PatchOrdinal(Patch.Face, Patch.Level, Patch.X, Patch.Y) &&
        AlbedoBytes > 0 && ElevationBytes > 0 && LandMaskBytes > 0 && CloudBytes > 0 &&
        (Digest0 | Digest1 | Digest2 | Digest3) != 0;
}

/// <summary>One coherent base/refinement contribution in a patch ownership snapshot.</summary>
public readonly record struct PlanetarySurfacePatchOwnership(
    PlanetarySurfacePatchId Patch,
    bool OpaqueBase,
    float RefinementWeight);

public readonly record struct PlanetarySurfacePatchCacheStatistics(
    int Capacity,
    int ResidentCount,
    int AuthoritativeCount,
    int ActiveTransactions,
    long Hits,
    long Misses,
    long Evictions,
    long CompletedPromotions);

/// <summary>
/// Bounded persistent cache with transactional quartet promotion. A child
/// quartet cannot become visible until every child owns complete geometry,
/// elevation, and material payloads. During morph, the parent remains the
/// opaque base and all four children share one transaction weight.
/// </summary>
public sealed class PlanetarySurfacePatchCache
{
    private sealed class Entry
    {
        internal PlanetarySurfacePatchId Id;
        internal PlanetarySurfacePatchPayload Payload;
        internal PlanetarySurfacePatchResidency Residency;
        internal long LastUse;
    }

    private struct Promotion
    {
        internal bool Active;
        internal PlanetarySurfacePatchId Parent;
        internal float Weight;
    }

    private readonly Entry[] _entries;
    private readonly Promotion[] _promotions;
    private long _serial, _hits, _misses, _evictions, _completed;

    public PlanetarySurfacePatchCache(int capacity, int maximumConcurrentPromotions)
    {
        if (capacity < 5 || maximumConcurrentPromotions <= 0 || maximumConcurrentPromotions > capacity / 5)
            throw new ArgumentOutOfRangeException();
        _entries = Enumerable.Range(0, capacity).Select(_ => new Entry()).ToArray();
        _promotions = new Promotion[maximumConcurrentPromotions];
    }

    public void RegisterPayload(in PlanetarySurfacePatchId id, PlanetarySurfacePatchPayload payload)
    {
        if (!id.IsValid || payload == PlanetarySurfacePatchPayload.None ||
            (payload & ~PlanetarySurfacePatchPayload.ProductionRequired) != 0) throw new ArgumentOutOfRangeException();
        var entry = GetOrCreate(id);
        entry.Payload |= payload;
        if (entry.Residency != PlanetarySurfacePatchResidency.Authoritative)
            entry.Residency = entry.Payload == PlanetarySurfacePatchPayload.ProductionRequired
                ? PlanetarySurfacePatchResidency.Resident
                : PlanetarySurfacePatchResidency.Preparing;
    }

    public void SetInitialAuthoritative(in PlanetarySurfacePatchId id)
    {
        var entry = Find(id) ?? throw new InvalidOperationException("Initial patch is not resident.");
        if (entry.Payload != PlanetarySurfacePatchPayload.ProductionRequired) throw new InvalidOperationException("Initial patch payload is incomplete.");
        if (id.Parent is not null) throw new InvalidOperationException("Initial authority must begin at a root patch.");
        entry.Residency = PlanetarySurfacePatchResidency.Authoritative;
    }

    public bool TryBeginPromotion(in PlanetarySurfacePatchId parent)
    {
        var parentEntry = Find(parent);
        if (parentEntry?.Residency != PlanetarySurfacePatchResidency.Authoritative || IsInTransaction(parent)) return false;
        if (!PreservesNeighborBalance(parent)) return false;
        for (var child = 0; child < 4; child++)
        {
            var childId = parent.Child(child); var entry = Find(childId);
            if (entry is null || entry.Residency != PlanetarySurfacePatchResidency.Resident ||
                entry.Payload != PlanetarySurfacePatchPayload.ProductionRequired || IsInTransaction(childId)) return false;
        }
        for (var index = 0; index < _promotions.Length; index++)
            if (!_promotions[index].Active)
            {
                _promotions[index] = new Promotion { Active = true, Parent = parent, Weight = 0f };
                return true;
            }
        return false;
    }

    private bool PreservesNeighborBalance(in PlanetarySurfacePatchId parent)
    {
        foreach (var edge in BalanceEdges)
        {
            var neighbor = CubeSphereAdjacency.NeighborAtSameLevel(parent.Patch, edge);
            for (;;)
            {
                var neighborId = new PlanetarySurfacePatchId(parent.BodyId, parent.TerrainVersion, neighbor.Face, neighbor.Level, neighbor.X, neighbor.Y);
                var entry = Find(neighborId);
                if (entry?.Residency == PlanetarySurfacePatchResidency.Authoritative && !IsPromotionParent(neighborId))
                {
                    if (neighborId.Level < parent.Level) return false;
                    break;
                }
                if (neighbor.Level == 0) break;
                neighbor = neighbor.Parent!.Value;
            }
        }
        return true;
    }

    public void AdvancePromotions(float step)
    {
        if (!float.IsFinite(step) || step < 0f || step > 1f) throw new ArgumentOutOfRangeException(nameof(step));
        for (var index = 0; index < _promotions.Length; index++)
        {
            if (!_promotions[index].Active) continue;
            _promotions[index].Weight = Math.Min(1f, _promotions[index].Weight + step);
            if (_promotions[index].Weight < 1f) continue;
            var parent = Find(_promotions[index].Parent) ?? throw new InvalidOperationException("Promotion parent was evicted.");
            parent.Residency = PlanetarySurfacePatchResidency.Resident;
            for (var child = 0; child < 4; child++)
                (Find(_promotions[index].Parent.Child(child)) ?? throw new InvalidOperationException("Promotion child was evicted.")).Residency = PlanetarySurfacePatchResidency.Authoritative;
            _promotions[index] = default; _completed++;
        }
    }

    public int SnapshotOwnership(Span<PlanetarySurfacePatchOwnership> destination)
    {
        var count = 0;
        for (var index = 0; index < _entries.Length; index++)
        {
            var entry = _entries[index];
            if (entry.Residency != PlanetarySurfacePatchResidency.Authoritative || IsPromotionChild(entry.Id) || IsPromotionParent(entry.Id)) continue;
            AddOwnership(destination, ref count, new(entry.Id, true, 0f));
        }
        for (var index = 0; index < _promotions.Length; index++)
        {
            if (!_promotions[index].Active) continue;
            AddOwnership(destination, ref count, new(_promotions[index].Parent, true, 0f));
            for (var child = 0; child < 4; child++) AddOwnership(destination, ref count, new(_promotions[index].Parent.Child(child), false, _promotions[index].Weight));
        }
        return count;
    }

    public PlanetarySurfacePatchResidency ResidencyOf(in PlanetarySurfacePatchId id) => Find(id)?.Residency ?? PlanetarySurfacePatchResidency.Missing;

    public PlanetarySurfacePatchCacheStatistics Statistics => new(
        _entries.Length,
        _entries.Count(entry => entry.Residency is PlanetarySurfacePatchResidency.Resident or PlanetarySurfacePatchResidency.Authoritative),
        _entries.Count(entry => entry.Residency == PlanetarySurfacePatchResidency.Authoritative),
        _promotions.Count(value => value.Active),
        _hits, _misses, _evictions, _completed);

    private Entry GetOrCreate(in PlanetarySurfacePatchId id)
    {
        var existing = Find(id);
        if (existing is not null) { existing.LastUse = ++_serial; _hits++; return existing; }
        _misses++;
        Entry? selected = null; var oldest = long.MaxValue;
        for (var index = 0; index < _entries.Length; index++)
        {
            var candidate = _entries[index];
            if (candidate.Residency == PlanetarySurfacePatchResidency.Missing) { selected = candidate; break; }
            if (candidate.Residency == PlanetarySurfacePatchResidency.Resident && !IsInTransaction(candidate.Id) && candidate.LastUse < oldest)
            { selected = candidate; oldest = candidate.LastUse; }
        }
        if (selected is null) throw new InvalidOperationException("Production patch cache has no safe eviction candidate.");
        if (selected.Residency != PlanetarySurfacePatchResidency.Missing) _evictions++;
        selected.Id = id; selected.Payload = PlanetarySurfacePatchPayload.None;
        selected.Residency = PlanetarySurfacePatchResidency.Preparing; selected.LastUse = ++_serial;
        return selected;
    }

    private Entry? Find(in PlanetarySurfacePatchId id)
    {
        for (var index = 0; index < _entries.Length; index++)
            if (_entries[index].Residency != PlanetarySurfacePatchResidency.Missing && _entries[index].Id == id) return _entries[index];
        return null;
    }

    private bool IsInTransaction(in PlanetarySurfacePatchId id)
    {
        for (var index = 0; index < _promotions.Length; index++)
            if (_promotions[index].Active && (_promotions[index].Parent == id || IsChildOf(id, _promotions[index].Parent))) return true;
        return false;
    }

    private bool IsPromotionChild(in PlanetarySurfacePatchId id)
    {
        for (var index = 0; index < _promotions.Length; index++)
            if (_promotions[index].Active && IsChildOf(id, _promotions[index].Parent)) return true;
        return false;
    }

    private bool IsPromotionParent(in PlanetarySurfacePatchId id)
    {
        for (var index = 0; index < _promotions.Length; index++)
            if (_promotions[index].Active && _promotions[index].Parent == id) return true;
        return false;
    }

    private static bool IsChildOf(in PlanetarySurfacePatchId candidate, in PlanetarySurfacePatchId parent) =>
        candidate.Level == parent.Level + 1 && candidate.Parent == parent;

    private static readonly PlanetaryPatchEdge[] BalanceEdges =
        [PlanetaryPatchEdge.NegativeU, PlanetaryPatchEdge.PositiveU, PlanetaryPatchEdge.NegativeV, PlanetaryPatchEdge.PositiveV];

    private static void AddOwnership(Span<PlanetarySurfacePatchOwnership> destination, ref int count, in PlanetarySurfacePatchOwnership ownership)
    {
        if (count >= destination.Length) throw new ArgumentException("Ownership destination is too small.", nameof(destination));
        destination[count++] = ownership;
    }
}

public readonly record struct PlanetarySurfacePatchDemand(
    PlanetarySurfacePatchId Patch,
    PlanetarySurfacePatchPayload Payload,
    int Priority);

/// <summary>
/// Bounded no-allocation residency prediction from the complete visible patch
/// footprint. Motion only changes neighbor priority; it never changes patch
/// identity or creates an anchor-centered rectangular page neighborhood.
/// </summary>
public static class PlanetarySurfaceResidencyPlanner
{
    private static readonly PlanetaryPatchEdge[] Edges =
        [PlanetaryPatchEdge.NegativeU, PlanetaryPatchEdge.PositiveU, PlanetaryPatchEdge.NegativeV, PlanetaryPatchEdge.PositiveV];

    public static int Build(
        ReadOnlySpan<PlanetarySurfacePatchId> visiblePatches,
        in Double3 cameraMotionBodyFixed,
        Span<PlanetarySurfacePatchDemand> destination)
    {
        if (!cameraMotionBodyFixed.IsFinite) throw new ArgumentOutOfRangeException(nameof(cameraMotionBodyFixed));
        var count = 0;
        for (var visibleIndex = 0; visibleIndex < visiblePatches.Length; visibleIndex++)
        {
            var patch = visiblePatches[visibleIndex];
            if (!patch.IsValid) throw new ArgumentOutOfRangeException(nameof(visiblePatches));
            AddDemand(destination, ref count, patch, PlanetarySurfacePatchPayload.ProductionRequired, 0);
            var parent = patch.Parent; var ancestorPriority = 1;
            while (parent is { } ancestor) { AddDemand(destination, ref count, ancestor, PlanetarySurfacePatchPayload.ProductionRequired, ancestorPriority++); parent = ancestor.Parent; }
            var center = PatchCenter(patch);
            for (var edgeIndex = 0; edgeIndex < Edges.Length; edgeIndex++)
            {
                var neighborPatch = CubeSphereAdjacency.NeighborAtSameLevel(patch.Patch, Edges[edgeIndex]);
                var neighbor = new PlanetarySurfacePatchId(patch.BodyId, patch.TerrainVersion, neighborPatch.Face, neighborPatch.Level, neighborPatch.X, neighborPatch.Y);
                var towardMotion = Double3.Dot(PatchCenter(neighbor) - center, cameraMotionBodyFixed);
                AddDemand(destination, ref count, neighbor, PlanetarySurfacePatchPayload.ProductionRequired, towardMotion > 0d ? 2 : 3);
            }
        }
        return count;
    }

    private static Double3 PatchCenter(in PlanetarySurfacePatchId id)
    {
        var bounds = id.Patch.Bounds;
        return RelaxedCubeSphereProjection.UnitDirection(id.Face, (bounds.MinX + bounds.MaxX) * .5d, (bounds.MinY + bounds.MaxY) * .5d);
    }

    private static void AddDemand(Span<PlanetarySurfacePatchDemand> destination, ref int count, in PlanetarySurfacePatchId patch, PlanetarySurfacePatchPayload payload, int priority)
    {
        for (var index = 0; index < count; index++)
            if (destination[index].Patch == patch)
            {
                destination[index] = destination[index] with
                {
                    Payload = destination[index].Payload | payload,
                    Priority = Math.Min(destination[index].Priority, priority)
                };
                return;
            }
        if (count >= destination.Length) throw new ArgumentException("Residency demand destination is too small.", nameof(destination));
        destination[count++] = new(patch, payload, priority);
    }
}
