using NovaCore.Core;
using NovaCore.Core.Camera;

namespace NovaCore.Graphics;

/// <summary>
/// Backend-neutral screen-space demand authority. Both incident owners
/// project the same canonically ordered physical endpoints, then quantize the
/// same fixed-point length. No face-local chart or camera value enters identity.
/// </summary>
public static class PlanetaryScreenSpaceSubdivision
{
    public const uint FixedPointScale = 65536;
    public const double DefaultHysteresisFraction = 0.125d;

    /// <summary>
    /// Production tessellation-stage policy. The factor is a power of two so
    /// incident edges with the same projected endpoints refine identically.
    /// It is independent of CPU patch level and bounded by device policy.
    /// </summary>
    public static uint QuantizedGpuFactor(double projectedLengthPixels,
        double targetLengthPixels = PlanetaryDynamicAnchoredSurface.GpuTargetEdgePixels,
        uint maximumFactor = PlanetaryDynamicAnchoredSurface.GpuMaximumTessellationFactor)
    {
        if (double.IsNaN(projectedLengthPixels) || projectedLengthPixels < 0d ||
            !double.IsFinite(targetLengthPixels) || targetLengthPixels <= 0d ||
            maximumFactor == 0 || (maximumFactor & (maximumFactor - 1u)) != 0u)
            throw new ArgumentOutOfRangeException();
        if (double.IsPositiveInfinity(projectedLengthPixels)) return maximumFactor;
        var required = Math.Max(1d, projectedLengthPixels / targetLengthPixels);
        var factor = 1u;
        while (factor < maximumFactor && factor < required) factor <<= 1;
        return Math.Min(factor, maximumFactor);
    }

    public static PlanetaryAnchoredMeshSubdivisionDemand Project(
        in PlanetaryAnchoredMeshEdgeId edge,
        in Double3 firstBodyFixedPosition,
        in Double3 secondBodyFixedPosition,
        in Double3 bodyRootPosition,
        in Double3 cameraRootPosition,
        in DoubleQuaternion cameraOrientation,
        in CameraProjection projection,
        double viewportHeightPixels,
        double targetLengthPixels,
        uint maximumFactor)
    {
        if (!edge.IsValid || !firstBodyFixedPosition.IsFinite || !secondBodyFixedPosition.IsFinite ||
            !bodyRootPosition.IsFinite || !cameraRootPosition.IsFinite ||
            !double.IsFinite(viewportHeightPixels) || viewportHeightPixels <= 0d)
            throw new ArgumentOutOfRangeException();
        projection.Validate();
        var inverse = cameraOrientation.Conjugate().Normalized();
        var first = inverse.Rotate(bodyRootPosition + firstBodyFixedPosition - cameraRootPosition);
        var second = inverse.Rotate(bodyRootPosition + secondBodyFixedPosition - cameraRootPosition);
        var projectedLength = ProjectedLength(first, second, projection, viewportHeightPixels);
        return PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(edge, projectedLength,
            targetLengthPixels, maximumFactor);
    }

    public static uint Stabilize(uint desiredFactor, uint previousFactor,
        double projectedLengthPixels, double targetLengthPixels, uint maximumFactor,
        double hysteresisFraction = DefaultHysteresisFraction)
    {
        if (desiredFactor is 0 || maximumFactor is 0 || desiredFactor > maximumFactor ||
            !double.IsFinite(projectedLengthPixels) || projectedLengthPixels < 0d ||
            !double.IsFinite(targetLengthPixels) || targetLengthPixels <= 0d ||
            !double.IsFinite(hysteresisFraction) || hysteresisFraction is < 0d or >= 0.5d)
            throw new ArgumentOutOfRangeException();
        if (previousFactor is 0 || previousFactor > maximumFactor) return desiredFactor;
        if (desiredFactor > previousFactor)
        {
            var threshold = previousFactor * targetLengthPixels * (1d + hysteresisFraction);
            return projectedLengthPixels > threshold ? desiredFactor : previousFactor;
        }
        if (desiredFactor < previousFactor)
        {
            var threshold = Math.Max(0d, previousFactor - 1d) * targetLengthPixels *
                (1d - hysteresisFraction);
            return projectedLengthPixels < threshold ? desiredFactor : previousFactor;
        }
        return previousFactor;
    }

    private static double ProjectedLength(in Double3 firstView, in Double3 secondView,
        in CameraProjection projection, double viewportHeightPixels)
    {
        // Camera forward is -Z. Clip endpoints to a tiny positive forward depth
        // so an edge crossing the near eye plane remains bounded and symmetric.
        const double minimumDepth = 1e-9d;
        var firstDepth = Math.Max(minimumDepth, -firstView.Z);
        var secondDepth = Math.Max(minimumDepth, -secondView.Z);
        var focalPixels = viewportHeightPixels /
            (2d * Math.Tan(projection.VerticalFieldOfViewRadians * 0.5d));
        var firstX = firstView.X / firstDepth * focalPixels;
        var firstY = firstView.Y / firstDepth * focalPixels;
        var secondX = secondView.X / secondDepth * focalPixels;
        var secondY = secondView.Y / secondDepth * focalPixels;
        var dx = firstX - secondX; var dy = firstY - secondY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        return double.IsFinite(length) ? Math.Min(length, uint.MaxValue / (double)FixedPointScale) :
            uint.MaxValue / (double)FixedPointScale;
    }
}
