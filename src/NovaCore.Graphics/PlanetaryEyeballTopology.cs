using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Deterministic camera-centric spherical-cap topology used by the V2 near-field renderer.
/// Topology is immutable; only its body-fixed pupil and angular extent change per frame.
/// </summary>
public sealed class PlanetaryEyeballTopology
{
    public const int RadialRingCount = 128;
    public const int AzimuthSegmentCount = 256;
    public const double RadialWarpExponent = 2d;
    public const double HorizonMarginRadians = .002d;
    public const int VertexCount = 1 + RadialRingCount * AzimuthSegmentCount;
    public const int IndexCount = AzimuthSegmentCount * 3 + (RadialRingCount - 1) * AzimuthSegmentCount * 6;

    private readonly uint[] _indices;

    private PlanetaryEyeballTopology()
    {
        _indices = BuildIndices();
        DeterministicHash = Hash(_indices);
    }

    public static PlanetaryEyeballTopology Shared { get; } = new();

    public ReadOnlySpan<uint> Indices => _indices;
    public ulong DeterministicHash { get; }

    public static double WarpedRadius(int ring)
    {
        if (ring is < 1 or > RadialRingCount) throw new ArgumentOutOfRangeException(nameof(ring));
        var normalized = (double)ring / RadialRingCount;
        return normalized * normalized;
    }

    public static Double3 DirectionAt(in Double3 pupil, int ring, int segment, double maximumAngleRadians)
    {
        if (!pupil.IsFinite || pupil.LengthSquared <= 0d) throw new ArgumentOutOfRangeException(nameof(pupil));
        if (segment is < 0 or >= AzimuthSegmentCount) throw new ArgumentOutOfRangeException(nameof(segment));
        if (!double.IsFinite(maximumAngleRadians) || maximumAngleRadians <= 0d || maximumAngleRadians >= Math.PI) throw new ArgumentOutOfRangeException(nameof(maximumAngleRadians));
        var frame = PlanetarySurfaceFrame.AtDirection(pupil);
        var azimuth = 2d * Math.PI * segment / AzimuthSegmentCount;
        var angle = maximumAngleRadians * WarpedRadius(ring);
        var tangent = frame.East * Math.Cos(azimuth) + frame.North * Math.Sin(azimuth);
        return (frame.Up * Math.Cos(angle) + tangent * Math.Sin(angle)).Normalized();
    }

    private static uint[] BuildIndices()
    {
        var indices = new uint[IndexCount];
        var output = 0;
        for (var segment = 0; segment < AzimuthSegmentCount; segment++)
        {
            var next = (segment + 1) % AzimuthSegmentCount;
            indices[output++] = 0;
            indices[output++] = (uint)(1 + segment);
            indices[output++] = (uint)(1 + next);
        }

        for (var ring = 1; ring < RadialRingCount; ring++)
        {
            var inner = 1 + (ring - 1) * AzimuthSegmentCount;
            var outer = 1 + ring * AzimuthSegmentCount;
            for (var segment = 0; segment < AzimuthSegmentCount; segment++)
            {
                var next = (segment + 1) % AzimuthSegmentCount;
                indices[output++] = (uint)(inner + segment);
                indices[output++] = (uint)(outer + segment);
                indices[output++] = (uint)(inner + next);
                indices[output++] = (uint)(inner + next);
                indices[output++] = (uint)(outer + segment);
                indices[output++] = (uint)(outer + next);
            }
        }

        if (output != indices.Length) throw new InvalidOperationException("Eyeball topology index generation did not fill its fixed allocation.");
        return indices;
    }

    private static ulong Hash(ReadOnlySpan<uint> indices)
    {
        var hash = 14695981039346656037ul;
        static void Mix(ref ulong value, uint input) => value = (value ^ input) * 1099511628211ul;
        Mix(ref hash, RadialRingCount);
        Mix(ref hash, AzimuthSegmentCount);
        Mix(ref hash, VertexCount);
        Mix(ref hash, IndexCount);
        var warpBits = (ulong)BitConverter.DoubleToInt64Bits(RadialWarpExponent);
        var marginBits = (ulong)BitConverter.DoubleToInt64Bits(HorizonMarginRadians);
        Mix(ref hash, (uint)warpBits); Mix(ref hash, (uint)(warpBits >> 32));
        Mix(ref hash, (uint)marginBits); Mix(ref hash, (uint)(marginBits >> 32));
        foreach (var index in indices) Mix(ref hash, index);
        return hash;
    }
}

public static class PlanetaryEyeballHandoff
{
    public const double RegionalOnlyAltitudeMetres = 2_000_000d;
    public const double EyeballOnlyAltitudeMetres = 1_000_000d;
    public const int RegionalMaximumLod = 12;

    public static float EyeballWeight(double altitudeMetres)
    {
        if (!double.IsFinite(altitudeMetres)) throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        var value = Math.Clamp((RegionalOnlyAltitudeMetres - altitudeMetres) /
            (RegionalOnlyAltitudeMetres - EyeballOnlyAltitudeMetres), 0d, 1d);
        return (float)(value * value * (3d - 2d * value));
    }
}
