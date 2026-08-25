using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// One immutable production spherical-billboard geometry tier. The buffer
/// contains only canonical pupil-space parameters; body-fixed orientation,
/// displacement and camera-relative reconstruction remain shader inputs.
/// </summary>
public sealed class PlanetaryProductionEyeballTier
{
    public readonly record struct Vertex(float Radial, float AzimuthTurns);

    internal PlanetaryProductionEyeballTier(int index, int radialRings, int azimuthSegments, double radialWarpExponent)
    {
        if (index < 0 || radialRings < 8 || azimuthSegments < 16 || (azimuthSegments & (azimuthSegments - 1)) != 0 ||
            !double.IsFinite(radialWarpExponent) || radialWarpExponent <= 1d) throw new ArgumentOutOfRangeException();
        Index = index;
        RadialRings = radialRings;
        AzimuthSegments = azimuthSegments;
        RadialWarpExponent = radialWarpExponent;
        Vertices = BuildVertices();
        Indices = BuildIndices();
        DeterministicHash = Hash();
    }

    public int Index { get; }
    public int RadialRings { get; }
    public int AzimuthSegments { get; }
    public double RadialWarpExponent { get; }
    public Vertex[] Vertices { get; }
    public uint[] Indices { get; }
    public int VertexCount => Vertices.Length;
    public int IndexCount => Indices.Length;
    public ulong DeterministicHash { get; }

    private Vertex[] BuildVertices()
    {
        var result = new Vertex[1 + RadialRings * AzimuthSegments];
        result[0] = new(0f, 0f);
        for (var ring = 1; ring <= RadialRings; ring++)
            for (var segment = 0; segment < AzimuthSegments; segment++)
                result[1 + (ring - 1) * AzimuthSegments + segment] =
                    new((float)((double)ring / RadialRings), (float)((double)segment / AzimuthSegments));
        return result;
    }

    private uint[] BuildIndices()
    {
        var result = new uint[AzimuthSegments * 3 + (RadialRings - 1) * AzimuthSegments * 6];
        var output = 0;
        for (var segment = 0; segment < AzimuthSegments; segment++)
        {
            var next = (segment + 1) % AzimuthSegments;
            result[output++] = 0; result[output++] = (uint)(1 + segment); result[output++] = (uint)(1 + next);
        }
        for (var ring = 1; ring < RadialRings; ring++)
            for (var segment = 0; segment < AzimuthSegments; segment++)
            {
                var next = (segment + 1) % AzimuthSegments;
                var inner = 1 + (ring - 1) * AzimuthSegments;
                var outer = 1 + ring * AzimuthSegments;
                result[output++] = (uint)(inner + segment); result[output++] = (uint)(outer + segment); result[output++] = (uint)(inner + next);
                result[output++] = (uint)(inner + next); result[output++] = (uint)(outer + segment); result[output++] = (uint)(outer + next);
            }
        return result;
    }

    private ulong Hash()
    {
        var hash = 14695981039346656037ul;
        Mix((uint)Index); Mix((uint)RadialRings); Mix((uint)AzimuthSegments);
        Mix64((ulong)BitConverter.DoubleToInt64Bits(RadialWarpExponent));
        foreach (var vertex in Vertices) { Mix((uint)BitConverter.SingleToInt32Bits(vertex.Radial)); Mix((uint)BitConverter.SingleToInt32Bits(vertex.AzimuthTurns)); }
        foreach (var index in Indices) Mix(index);
        return hash;
        void Mix(uint value) => hash = (hash ^ value) * 1099511628211ul;
        void Mix64(ulong value) { Mix((uint)value); Mix((uint)(value >> 32)); }
    }
}

/// <summary>Finite renderer-lifetime tier set; no runtime subdivision exists.</summary>
public static class PlanetaryProductionEyeballTopology
{
    public const double MaximumAngleRadians = 1.45d;
    public const double RadialWarpExponent = 1.85d;
    public const int MaximumTier = 3;

    private static readonly PlanetaryProductionEyeballTier[] Values =
    [
        new(0, 32, 64, RadialWarpExponent),
        new(1, 64, 128, RadialWarpExponent),
        new(2, 128, 256, RadialWarpExponent),
        new(3, 256, 512, RadialWarpExponent)
    ];

    public static IReadOnlyList<PlanetaryProductionEyeballTier> Tiers => Values;
    public static PlanetaryProductionEyeballTier Tier(int index) => (uint)index < Values.Length ? Values[index] : throw new ArgumentOutOfRangeException(nameof(index));

    public static Double3 DirectionAt(in Double3 pupil, int tierIndex, int ring, int segment, double maximumAngleRadians)
    {
        var tier = Tier(tierIndex);
        if (!pupil.IsFinite || pupil.LengthSquared <= 0d || ring < 0 || ring > tier.RadialRings ||
            segment < 0 || segment >= tier.AzimuthSegments || !double.IsFinite(maximumAngleRadians) || maximumAngleRadians <= 0d || maximumAngleRadians >= Math.PI)
            throw new ArgumentOutOfRangeException();
        var frame = PlanetarySurfaceFrame.AtDirection(pupil);
        var radial = Math.Pow((double)ring / tier.RadialRings, tier.RadialWarpExponent);
        var azimuth = 2d * Math.PI * segment / tier.AzimuthSegments;
        var angle = maximumAngleRadians * radial;
        var tangent = frame.East * Math.Cos(azimuth) + frame.North * Math.Sin(azimuth);
        return (frame.Up * Math.Cos(angle) + tangent * Math.Sin(angle)).Normalized();
    }

    /// <summary>Finds the body-fixed center of the visible footprint without fabricating a hit.</summary>
    public static bool TryViewPupil(in Double3 cameraBody, in Double3 viewForwardBody, double surfaceRadius, out Double3 pupil)
    {
        pupil = default;
        if (!cameraBody.IsFinite || !viewForwardBody.IsFinite || viewForwardBody.LengthSquared <= 0d || !double.IsFinite(surfaceRadius) || surfaceRadius <= 0d) return false;
        var direction = viewForwardBody.Normalized();
        var b = Double3.Dot(cameraBody, direction);
        var c = cameraBody.LengthSquared - surfaceRadius * surfaceRadius;
        var discriminant = b * b - c;
        if (!double.IsFinite(discriminant) || discriminant < 0d) return false;
        var root = Math.Sqrt(discriminant);
        var near = -b - root;
        var far = -b + root;
        var distance = near >= 0d ? near : far >= 0d ? far : double.NaN;
        if (!double.IsFinite(distance)) return false;
        pupil = (cameraBody + direction * distance).Normalized();
        return pupil.IsFinite;
    }

    public static bool CoversVisibleSurface(in Double3 cameraBody, in Double3 tangentAnchor, double bodyRadius, double maximumTerrainHeightMetres)
    {
        if (!cameraBody.IsFinite || cameraBody.LengthSquared <= 0d || !tangentAnchor.IsFinite || tangentAnchor.LengthSquared <= 0d ||
            !double.IsFinite(bodyRadius) || bodyRadius <= 0d || !double.IsFinite(maximumTerrainHeightMetres) || maximumTerrainHeightMetres < 0d)
            throw new ArgumentOutOfRangeException();
        var cameraDistance = Math.Sqrt(cameraBody.LengthSquared);
        if (cameraDistance <= bodyRadius + maximumTerrainHeightMetres) return false;
        var separation = Math.Acos(Math.Clamp(Double3.Dot(cameraBody / cameraDistance, tangentAnchor.Normalized()), -1d, 1d));
        var horizon = Math.Acos(Math.Clamp(bodyRadius / cameraDistance, 0d, 1d));
        var terrainMargin = Math.Asin(Math.Clamp(maximumTerrainHeightMetres / bodyRadius, 0d, .5d));
        return separation + horizon + terrainMargin + .002d <= MaximumAngleRadians;
    }
}

public readonly record struct PlanetaryProductionPupilCell(
    CubeSphereFace Face, int Resolution, int X, int Y, Double3 BodyFixedDirection)
{
    public bool IsValid => Resolution > 0 && X >= 0 && Y >= 0 && X < Resolution && Y < Resolution &&
        BodyFixedDirection.IsFinite && Math.Abs(BodyFixedDirection.LengthSquared - 1d) <= 1e-10d;
}

/// <summary>
/// Stable discrete body-fixed pupil identity. A Schmitt boundary retains the
/// old cell until the desired pupil is materially outside it; current camera
/// reconstruction is never snapped.
/// </summary>
public sealed class PlanetaryProductionPupilOrientation
{
    // Pupil identity is a body-fixed surface property, not a mesh-tier
    // property.  A single fine angular grid prevents T0<->T3 replacement from
    // moving the physical tangent origin merely because tessellation changed.
    public const int Resolution = 2_097_152;
    private PlanetaryProductionPupilCell? _cell;
    private long _changes;

    public PlanetaryProductionPupilCell Current => _cell ?? throw new InvalidOperationException("No production pupil orientation has been acquired.");
    public bool HasCurrent => _cell.HasValue;
    public long Changes => _changes;

    public PlanetaryProductionPupilCell Update(in Double3 desiredBodyFixedDirection, int tier)
    {
        if (!desiredBodyFixedDirection.IsFinite || desiredBodyFixedDirection.LengthSquared <= 0d || tier is < 0 or > PlanetaryProductionEyeballTopology.MaximumTier)
            throw new ArgumentOutOfRangeException();
        var desired = desiredBodyFixedDirection.Normalized();
        var resolution = Resolution;
        if (_cell is { } current)
        {
            var angularError = Math.Acos(Math.Clamp(Double3.Dot(current.BodyFixedDirection, desired), -1d, 1d));
            var retainAngle = 1.15d * Math.PI / resolution;
            if (angularError <= retainAngle) return current;
        }
        var replacement = Quantize(desired, resolution);
        if (_cell != replacement) _changes++;
        _cell = replacement;
        return replacement;
    }

    public void Reset() => _cell = null;

    private static PlanetaryProductionPupilCell Quantize(in Double3 direction, int resolution)
    {
        // Quantization and reconstruction must be an inverse pair.  Using the
        // ordinary cube-map inverse here and the relaxed cube projection below
        // displaces an acquired pupil by a macroscopically visible angle.
        if (!RelaxedCubeSphereProjection.TryAddress(direction, out var face, out var faceU, out var faceV))
            throw new ArgumentOutOfRangeException(nameof(direction));
        var x = Math.Clamp((int)Math.Floor(faceU * resolution), 0, resolution - 1);
        var y = Math.Clamp((int)Math.Floor(faceV * resolution), 0, resolution - 1);
        var center = RelaxedCubeSphereProjection.UnitDirection(face, ((double)x + .5d) / resolution, ((double)y + .5d) / resolution);
        return new(face, resolution, x, y, center);
    }
}

/// <summary>Projected-error tier and ownership policy with deterministic hysteresis.</summary>
public sealed class PlanetaryProductionEyeballSelection
{
    private static readonly double[] TierUpperDemandPixels = [12d, 32d, 96d];
    private int _tier;
    private long _tierChanges;

    public int Tier => _tier;
    public long TierChanges => _tierChanges;

    public static double ProjectedTerrainErrorPixels(double surfaceAltitudeMetres, double viewportHeightPixels, double verticalTanHalfFov, double maximumTerrainHeightMetres)
    {
        if (!double.IsFinite(surfaceAltitudeMetres) || surfaceAltitudeMetres <= 0d || !double.IsFinite(viewportHeightPixels) || viewportHeightPixels <= 0d ||
            !double.IsFinite(verticalTanHalfFov) || verticalTanHalfFov <= 0d || !double.IsFinite(maximumTerrainHeightMetres) || maximumTerrainHeightMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        return viewportHeightPixels * .5d / verticalTanHalfFov * maximumTerrainHeightMetres / surfaceAltitudeMetres;
    }

    public static float OwnershipWeight(double projectedTerrainErrorPixels)
    {
        if (!double.IsFinite(projectedTerrainErrorPixels) || projectedTerrainErrorPixels < 0d) throw new ArgumentOutOfRangeException(nameof(projectedTerrainErrorPixels));
        var t = Math.Clamp((projectedTerrainErrorPixels - 3d) / 5d, 0d, 1d);
        return (float)(t * t * (3d - 2d * t));
    }

    public int UpdateTier(double projectedTerrainErrorPixels)
    {
        if (!double.IsFinite(projectedTerrainErrorPixels) || projectedTerrainErrorPixels < 0d) throw new ArgumentOutOfRangeException(nameof(projectedTerrainErrorPixels));
        var candidate = _tier;
        while (candidate < PlanetaryProductionEyeballTopology.MaximumTier && projectedTerrainErrorPixels > TierUpperDemandPixels[candidate] * 1.12d) candidate++;
        while (candidate > 0 && projectedTerrainErrorPixels < TierUpperDemandPixels[candidate - 1] * .82d) candidate--;
        if (candidate != _tier) { _tier = candidate; _tierChanges++; }
        return _tier;
    }

    public void Reset() { _tier = 0; _tierChanges = 0; }
}

public readonly record struct PlanetaryProductionEyeballSlot(PlanetarySurfacePatchId Patch, int Slot, uint Generation);

/// <summary>Bounded deterministic LRU used by close-range patch dependencies.</summary>
public sealed class PlanetaryProductionEyeballResidency
{
    private sealed class Entry { internal PlanetarySurfacePatchId Patch; internal long LastUse; internal uint Generation; }
    private readonly Entry[] _entries;
    private long _serial, _hits, _misses, _evictions;

    public PlanetaryProductionEyeballResidency(int capacity = 256)
    {
        if (capacity is < 16 or > 512) throw new ArgumentOutOfRangeException(nameof(capacity));
        _entries = Enumerable.Range(0, capacity).Select(_ => new Entry()).ToArray();
    }

    public int Capacity => _entries.Length;
    public int ActiveSlots => _entries.Count(entry => entry.Patch.IsValid);
    public long Hits => _hits;
    public long Misses => _misses;
    public long Evictions => _evictions;

    public PlanetaryProductionEyeballSlot Touch(in PlanetarySurfacePatchId patch)
    {
        if (!patch.IsValid) throw new ArgumentOutOfRangeException(nameof(patch));
        _serial++;
        for (var index = 0; index < _entries.Length; index++)
            if (_entries[index].Patch == patch)
            {
                _entries[index].LastUse = _serial; _hits++;
                return new(patch, index, _entries[index].Generation);
            }
        _misses++;
        var selected = -1;
        for (var index = 0; index < _entries.Length; index++) if (!_entries[index].Patch.IsValid) { selected = index; break; }
        if (selected < 0)
        {
            selected = 0;
            for (var index = 1; index < _entries.Length; index++)
                if (_entries[index].LastUse < _entries[selected].LastUse) selected = index;
            _evictions++;
        }
        var entry = _entries[selected]; entry.Patch = patch; entry.LastUse = _serial; entry.Generation++;
        return new(patch, selected, entry.Generation);
    }

    public bool Owns(in PlanetaryProductionEyeballSlot slot) => (uint)slot.Slot < _entries.Length && _entries[slot.Slot].Patch == slot.Patch && _entries[slot.Slot].Generation == slot.Generation;
}
