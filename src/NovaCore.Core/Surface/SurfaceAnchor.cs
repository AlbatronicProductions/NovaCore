using NovaCore.Core.ReferenceFrames;

namespace NovaCore.Core.Surface;

/// <summary>
/// Exact identity of the physical terrain semantics against which a surface-relative offset is
/// defined. A mismatch is an error; 11B-6A provides no implicit terrain migration.
/// </summary>
public readonly record struct TerrainAuthorityVersion(uint SourceId, uint Version)
{
    public bool IsValid => SourceId != 0 && Version != 0;
}

public enum SurfaceAnchorCreationStatus : byte
{
    Success = 0,
    InvalidBodyId,
    InvalidTerrainAuthorityVersion,
    NonFiniteDirection,
    DegenerateDirection,
    NonUnitDirection,
    NonFiniteTerrainRelativeOffset,
}

/// <summary>
/// Canonical body-fixed surface identity. Direction and offset use exact FP64 value equality;
/// latitude/longitude, terrain height, surface normal, ENU, root position, render LOD, and GPU
/// residency are derived state and are deliberately absent.
/// </summary>
public readonly struct SurfaceAnchor : IEquatable<SurfaceAnchor>
{
    /// <summary>Maximum accepted absolute error in squared unit-direction length.</summary>
    public const double DirectionUnitLengthSquaredTolerance = 1e-12d;

    private SurfaceAnchor(
        ulong bodyId,
        in TerrainAuthorityVersion terrainAuthorityVersion,
        in Double3 normalizedBodyFixedDirection,
        double terrainRelativeOffsetMetres)
    {
        BodyId = bodyId;
        TerrainAuthorityVersion = terrainAuthorityVersion;
        NormalizedBodyFixedDirection = normalizedBodyFixedDirection;
        TerrainRelativeOffsetMetres = terrainRelativeOffsetMetres;
    }

    public ulong BodyId { get; }
    public TerrainAuthorityVersion TerrainAuthorityVersion { get; }
    public Double3 NormalizedBodyFixedDirection { get; }
    public double TerrainRelativeOffsetMetres { get; }

    public bool IsValid => Validate(
        BodyId,
        TerrainAuthorityVersion,
        NormalizedBodyFixedDirection,
        TerrainRelativeOffsetMetres) == SurfaceAnchorCreationStatus.Success;

    public static SurfaceAnchorCreationStatus TryCreate(
        ulong bodyId,
        in TerrainAuthorityVersion terrainAuthorityVersion,
        in Double3 normalizedBodyFixedDirection,
        double terrainRelativeOffsetMetres,
        out SurfaceAnchor anchor)
    {
        anchor = default;
        var status = Validate(bodyId, terrainAuthorityVersion, normalizedBodyFixedDirection, terrainRelativeOffsetMetres);
        if (status != SurfaceAnchorCreationStatus.Success) return status;
        anchor = new(bodyId, terrainAuthorityVersion, normalizedBodyFixedDirection, terrainRelativeOffsetMetres);
        return SurfaceAnchorCreationStatus.Success;
    }

    public ulong DeterministicHash
    {
        get
        {
            ulong hash = 14695981039346656037UL;
            hash = Mix(hash, BodyId);
            hash = Mix(hash, TerrainAuthorityVersion.SourceId);
            hash = Mix(hash, TerrainAuthorityVersion.Version);
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.X));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.Y));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.Z));
            return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(TerrainRelativeOffsetMetres));
        }
    }

    public bool Equals(SurfaceAnchor other) =>
        BodyId == other.BodyId &&
        TerrainAuthorityVersion == other.TerrainAuthorityVersion &&
        BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.X) == BitConverter.DoubleToInt64Bits(other.NormalizedBodyFixedDirection.X) &&
        BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.Y) == BitConverter.DoubleToInt64Bits(other.NormalizedBodyFixedDirection.Y) &&
        BitConverter.DoubleToInt64Bits(NormalizedBodyFixedDirection.Z) == BitConverter.DoubleToInt64Bits(other.NormalizedBodyFixedDirection.Z) &&
        BitConverter.DoubleToInt64Bits(TerrainRelativeOffsetMetres) == BitConverter.DoubleToInt64Bits(other.TerrainRelativeOffsetMetres);

    public override bool Equals(object? obj) => obj is SurfaceAnchor other && Equals(other);
    public override int GetHashCode()
    {
        var hash = DeterministicHash;
        return unchecked((int)(hash ^ (hash >> 32)));
    }

    public static bool operator ==(SurfaceAnchor left, SurfaceAnchor right) => left.Equals(right);
    public static bool operator !=(SurfaceAnchor left, SurfaceAnchor right) => !left.Equals(right);

    private static SurfaceAnchorCreationStatus Validate(
        ulong bodyId,
        in TerrainAuthorityVersion terrainAuthorityVersion,
        in Double3 direction,
        double offset)
    {
        if (bodyId == 0) return SurfaceAnchorCreationStatus.InvalidBodyId;
        if (!terrainAuthorityVersion.IsValid) return SurfaceAnchorCreationStatus.InvalidTerrainAuthorityVersion;
        if (!direction.IsFinite) return SurfaceAnchorCreationStatus.NonFiniteDirection;
        if (direction.LengthSquared <= 0d) return SurfaceAnchorCreationStatus.DegenerateDirection;
        if (Math.Abs(direction.LengthSquared - 1d) > DirectionUnitLengthSquaredTolerance) return SurfaceAnchorCreationStatus.NonUnitDirection;
        return double.IsFinite(offset)
            ? SurfaceAnchorCreationStatus.Success
            : SurfaceAnchorCreationStatus.NonFiniteTerrainRelativeOffset;
    }

    private static ulong Mix(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++)
        {
            hash ^= (byte)value;
            hash *= 1099511628211UL;
            value >>= 8;
        }
        return hash;
    }
}

/// <summary>
/// Allocation-free physical terrain seam. Implementations must query authoritative body-fixed
/// physical terrain, never current render topology or GPU residency.
/// </summary>
public interface IPhysicalTerrainAuthority
{
    TerrainAuthorityVersion AuthorityVersion { get; }
    bool SupportsBody(ulong bodyId);
    bool TrySampleHeight(ulong bodyId, in Double3 normalizedBodyFixedDirection, out double heightMetres);
}

/// <summary>One body's physical radius and evaluated CCF frame identity.</summary>
public readonly record struct SurfaceBodyReference(
    ulong BodyId,
    double ReferenceRadiusMetres,
    ReferenceFrameId BodyFixedFrame)
{
    public bool IsValid => BodyId != 0 && double.IsFinite(ReferenceRadiusMetres) && ReferenceRadiusMetres > 0d && BodyFixedFrame.Value != 0;
}

/// <summary>
/// Deterministic right-handed body-fixed east/north/radial-up frame. Up is geographic radial Up,
/// not the terrain geometric normal. The exact poles use a fixed +Z fallback axis.
/// </summary>
public readonly record struct SurfaceEnuFrame(Double3 East, Double3 North, Double3 Up)
{
    public bool IsValid => East.IsFinite && North.IsFinite && Up.IsFinite &&
        Math.Abs(East.LengthSquared - 1d) <= 1e-12d &&
        Math.Abs(North.LengthSquared - 1d) <= 1e-12d &&
        Math.Abs(Up.LengthSquared - 1d) <= 1e-12d &&
        Math.Abs(Double3.Dot(East, North)) <= 1e-12d &&
        Math.Abs(Double3.Dot(East, Up)) <= 1e-12d &&
        Math.Abs(Double3.Dot(North, Up)) <= 1e-12d &&
        Double3.Dot(Double3.Cross(East, North), Up) >= 1d - 1e-12d;

    public static bool TryCreate(in SurfaceAnchor anchor, out SurfaceEnuFrame frame)
    {
        frame = default;
        if (!anchor.IsValid) return false;
        var up = anchor.NormalizedBodyFixedDirection;
        var eastCandidate = Double3.Cross(Double3.UnitY, up);
        var east = eastCandidate.LengthSquared > 1e-24d
            ? eastCandidate.Normalized()
            : Double3.Cross(Double3.UnitZ, up).Normalized();
        var north = Double3.Cross(up, east).Normalized();
        frame = new(east, north, up);
        return frame.IsValid;
    }
}

public enum SurfaceAnchorEvaluationStatus : byte
{
    Success = 0,
    InvalidAnchor,
    InvalidBodyReference,
    BodyMismatch,
    UnsupportedTerrainAuthority,
    TerrainVersionMismatch,
    TerrainQueryFailed,
    NonFiniteTerrainHeight,
    NonPositiveRadialDistance,
    InvalidRootFrame,
    MissingBodyTransform,
    NonFiniteInput,
    DegenerateBodyFixedPosition,
    NonFiniteResult,
    AnchorCreationFailed,
}

/// <summary>
/// Exact surface-position and kinematic transformations composed from caller-owned terrain and
/// immutable reference-frame authority. The hot path performs no file I/O, GPU work, or allocation.
/// </summary>
public static class SurfaceAnchorEvaluator
{
    public static SurfaceAnchorEvaluationStatus TryEvaluateBodyFixed<TTerrain>(
        in SurfaceAnchor anchor,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        out Double3 bodyFixedPosition,
        out double physicalTerrainHeightMetres)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        bodyFixedPosition = default;
        physicalTerrainHeightMetres = default;
        var status = ValidateAuthority(anchor, body, terrain);
        if (status != SurfaceAnchorEvaluationStatus.Success) return status;
        if (!terrain.TrySampleHeight(anchor.BodyId, anchor.NormalizedBodyFixedDirection, out physicalTerrainHeightMetres))
            return SurfaceAnchorEvaluationStatus.TerrainQueryFailed;
        if (!double.IsFinite(physicalTerrainHeightMetres)) return SurfaceAnchorEvaluationStatus.NonFiniteTerrainHeight;
        var radius = body.ReferenceRadiusMetres + physicalTerrainHeightMetres + anchor.TerrainRelativeOffsetMetres;
        if (!double.IsFinite(radius)) return SurfaceAnchorEvaluationStatus.NonFiniteResult;
        if (radius <= 0d) return SurfaceAnchorEvaluationStatus.NonPositiveRadialDistance;
        bodyFixedPosition = anchor.NormalizedBodyFixedDirection * radius;
        return bodyFixedPosition.IsFinite
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.NonFiniteResult;
    }

    public static SurfaceAnchorEvaluationStatus TryEvaluateRoot<TTerrain>(
        in SurfaceAnchor anchor,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        ReferenceFrameResolver frames,
        out UniversePosition rootPosition,
        out double physicalTerrainHeightMetres)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        rootPosition = default;
        physicalTerrainHeightMetres = default;
        if (frames is null) return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        var status = TryEvaluateBodyFixed(anchor, body, terrain, out var bodyFixedPosition, out physicalTerrainHeightMetres);
        if (status != SurfaceAnchorEvaluationStatus.Success) return status;
        if (!frames.TryResolvePosition(new(body.BodyFixedFrame, bodyFixedPosition), out rootPosition))
            return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        return rootPosition.Value.IsFinite
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.NonFiniteResult;
    }

    public static SurfaceAnchorEvaluationStatus TryCreateFromRoot<TTerrain>(
        ulong bodyId,
        in TerrainAuthorityVersion terrainAuthorityVersion,
        in UniversePosition rootPosition,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        ReferenceFrameResolver frames,
        out SurfaceAnchor anchor,
        out Double3 bodyFixedPosition,
        out double physicalTerrainHeightMetres)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        anchor = default;
        bodyFixedPosition = default;
        physicalTerrainHeightMetres = default;
        if (frames is null) return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        if (!rootPosition.Value.IsFinite) return SurfaceAnchorEvaluationStatus.NonFiniteInput;
        if (rootPosition.Frame != frames.RootFrame) return SurfaceAnchorEvaluationStatus.InvalidRootFrame;
        if (!body.IsValid) return SurfaceAnchorEvaluationStatus.InvalidBodyReference;
        if (bodyId == 0 || bodyId != body.BodyId) return SurfaceAnchorEvaluationStatus.BodyMismatch;
        if (!terrain.SupportsBody(bodyId)) return SurfaceAnchorEvaluationStatus.UnsupportedTerrainAuthority;
        if (!terrainAuthorityVersion.IsValid || terrain.AuthorityVersion != terrainAuthorityVersion)
            return SurfaceAnchorEvaluationStatus.TerrainVersionMismatch;
        if (!frames.TryConvertPosition(new(rootPosition.Frame, rootPosition.Value), body.BodyFixedFrame, out var converted))
            return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        bodyFixedPosition = converted.Value;
        if (!bodyFixedPosition.IsFinite) return SurfaceAnchorEvaluationStatus.NonFiniteResult;
        var radialDistance = Math.Sqrt(bodyFixedPosition.LengthSquared);
        if (!double.IsFinite(radialDistance)) return SurfaceAnchorEvaluationStatus.NonFiniteResult;
        if (radialDistance <= 0d) return SurfaceAnchorEvaluationStatus.DegenerateBodyFixedPosition;
        var direction = bodyFixedPosition / radialDistance;
        if (!terrain.TrySampleHeight(bodyId, direction, out physicalTerrainHeightMetres))
            return SurfaceAnchorEvaluationStatus.TerrainQueryFailed;
        if (!double.IsFinite(physicalTerrainHeightMetres)) return SurfaceAnchorEvaluationStatus.NonFiniteTerrainHeight;
        var offset = radialDistance - body.ReferenceRadiusMetres - physicalTerrainHeightMetres;
        if (!double.IsFinite(offset)) return SurfaceAnchorEvaluationStatus.NonFiniteResult;
        return SurfaceAnchor.TryCreate(bodyId, terrainAuthorityVersion, direction, offset, out anchor) == SurfaceAnchorCreationStatus.Success
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.AnchorCreationFailed;
    }

    public static SurfaceAnchorEvaluationStatus TryEvaluateRootState<TTerrain>(
        in SurfaceAnchor anchor,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        ReferenceFrameResolver frames,
        in Double3 surfaceRelativeVelocityBodyFixed,
        out UniversePosition rootPosition,
        out FrameVelocity rootVelocity,
        out double physicalTerrainHeightMetres)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        rootPosition = default;
        rootVelocity = default;
        physicalTerrainHeightMetres = default;
        if (!surfaceRelativeVelocityBodyFixed.IsFinite) return SurfaceAnchorEvaluationStatus.NonFiniteInput;
        var status = TryEvaluateBodyFixed(anchor, body, terrain, out var bodyFixedPosition, out physicalTerrainHeightMetres);
        if (status != SurfaceAnchorEvaluationStatus.Success) return status;
        if (frames is null || !frames.TryResolvePosition(new(body.BodyFixedFrame, bodyFixedPosition), out rootPosition) ||
            !frames.TryConvertVelocity(
                new(body.BodyFixedFrame, bodyFixedPosition),
                new(body.BodyFixedFrame, surfaceRelativeVelocityBodyFixed),
                frames.RootFrame,
                out rootVelocity))
            return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        return rootPosition.Value.IsFinite && rootVelocity.Value.IsFinite
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.NonFiniteResult;
    }

    public static SurfaceAnchorEvaluationStatus TryCreateFromRootState<TTerrain>(
        ulong bodyId,
        in TerrainAuthorityVersion terrainAuthorityVersion,
        in UniversePosition rootPosition,
        in FrameVelocity rootVelocity,
        in SurfaceBodyReference body,
        in TTerrain terrain,
        ReferenceFrameResolver frames,
        out SurfaceAnchor anchor,
        out Double3 surfaceRelativeVelocityBodyFixed,
        out double physicalTerrainHeightMetres)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        surfaceRelativeVelocityBodyFixed = default;
        if (!rootVelocity.Value.IsFinite) { anchor = default; physicalTerrainHeightMetres = default; return SurfaceAnchorEvaluationStatus.NonFiniteInput; }
        if (frames is null || rootVelocity.Frame != frames.RootFrame)
        {
            anchor = default;
            physicalTerrainHeightMetres = default;
            return frames is null ? SurfaceAnchorEvaluationStatus.MissingBodyTransform : SurfaceAnchorEvaluationStatus.InvalidRootFrame;
        }
        var status = TryCreateFromRoot(bodyId, terrainAuthorityVersion, rootPosition, body, terrain, frames, out anchor, out var bodyFixedPosition, out physicalTerrainHeightMetres);
        if (status != SurfaceAnchorEvaluationStatus.Success) return status;
        if (!frames.TryConvertVelocity(
                new(rootPosition.Frame, rootPosition.Value),
                rootVelocity,
                body.BodyFixedFrame,
                out var relative))
            return SurfaceAnchorEvaluationStatus.MissingBodyTransform;
        surfaceRelativeVelocityBodyFixed = relative.Value;
        return surfaceRelativeVelocityBodyFixed.IsFinite
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.NonFiniteResult;
    }

    private static SurfaceAnchorEvaluationStatus ValidateAuthority<TTerrain>(
        in SurfaceAnchor anchor,
        in SurfaceBodyReference body,
        in TTerrain terrain)
        where TTerrain : struct, IPhysicalTerrainAuthority
    {
        if (!anchor.IsValid) return SurfaceAnchorEvaluationStatus.InvalidAnchor;
        if (!body.IsValid) return SurfaceAnchorEvaluationStatus.InvalidBodyReference;
        if (anchor.BodyId != body.BodyId) return SurfaceAnchorEvaluationStatus.BodyMismatch;
        if (!terrain.SupportsBody(anchor.BodyId)) return SurfaceAnchorEvaluationStatus.UnsupportedTerrainAuthority;
        return terrain.AuthorityVersion == anchor.TerrainAuthorityVersion
            ? SurfaceAnchorEvaluationStatus.Success
            : SurfaceAnchorEvaluationStatus.TerrainVersionMismatch;
    }
}
