using NovaCore.Core;

namespace NovaCore.Graphics;

public readonly record struct SurfaceAnchorAcquisitionResult(
    SurfaceAnchorFocus Anchor,
    UniversePosition RootPositionAtAcquisition,
    double RayDistanceMetres,
    int SurfaceRefinementCount)
{
    public bool IsValid => Anchor.IsValid && RootPositionAtAcquisition.Value.IsFinite &&
        double.IsFinite(RayDistanceMetres) && RayDistanceMetres >= 0d && SurfaceRefinementCount >= 0;
}

public readonly record struct CameraExteriorConstraintResult(
    Double3 RootPosition,
    Double3 BodyLocalPosition,
    double SurfaceAltitudeMetres,
    double CorrectionMetres,
    int Iterations,
    int TerrainQueries)
{
    public bool IsValid => RootPosition.IsFinite && BodyLocalPosition.IsFinite &&
        double.IsFinite(SurfaceAltitudeMetres) && double.IsFinite(CorrectionMetres) &&
        CorrectionMetres >= 0d && Iterations >= 0 && TerrainQueries > 0;
}

/// <summary>
/// Exact body-local camera authority compared with the split-FP32 value
/// consumed by the production GPU shaders.  The physical surface decision is
/// made before encoding; this record proves that presentation transport does
/// not substitute a different camera.
/// </summary>
public readonly record struct CameraExteriorTransportIdentity(
    Double3 ValidatedBodyLocalPosition,
    Double3 GpuBodyLocalPosition,
    double ValidatedSurfaceRadiusMetres,
    double GpuSurfaceRadiusMetres,
    double ValidatedClearanceMetres,
    double GpuClearanceMetres)
{
    public double PositionErrorMetres => Math.Sqrt((GpuBodyLocalPosition - ValidatedBodyLocalPosition).LengthSquared);
    public bool IsFinite => ValidatedBodyLocalPosition.IsFinite && GpuBodyLocalPosition.IsFinite &&
        double.IsFinite(ValidatedSurfaceRadiusMetres) && double.IsFinite(GpuSurfaceRadiusMetres) &&
        double.IsFinite(ValidatedClearanceMetres) && double.IsFinite(GpuClearanceMetres) &&
        double.IsFinite(PositionErrorMetres);
}

/// <summary>Deterministic body-fixed ray/surface acquisition. It performs no streaming or filesystem work.</summary>
public static class SurfaceAnchorAcquisition
{
    public const int TerrainRefinementCount = 4;
    public const int CameraExteriorMaximumIterations = 3;

    public static bool TryAcquire(
        in PlanetRenderProxy body,
        in UniversePosition cameraRoot,
        in Double3 cameraForwardRoot,
        in PlanetaryTerrainDefinition terrain,
        out SurfaceAnchorAcquisitionResult result)
    {
        result = default;
        if (!body.IsValid || cameraRoot.Frame != body.Position.Frame || !cameraRoot.Value.IsFinite ||
            !cameraForwardRoot.IsFinite || cameraForwardRoot.LengthSquared <= 0d) return false;

        var rootToBody = body.BodyFixedToRoot.Conjugate().Normalized();
        var rayOrigin = rootToBody.Rotate(cameraRoot.Value - body.Position.Value);
        var rayDirection = rootToBody.Rotate(cameraForwardRoot).Normalized();
        var initialRadius = body.RadiusMetres + (terrain.IsValid ? terrain.MaximumHeightMetres : 0d);
        if (!TryRaySphere(rayOrigin, rayDirection, initialRadius, out var rayDistance)) return false;

        var refinements = terrain.IsValid ? TerrainRefinementCount : 0;
        var elevation = 0d;
        var direction = (rayOrigin + rayDirection * rayDistance).Normalized();
        for (var iteration = 0; iteration < refinements; iteration++)
        {
            elevation = terrain.SampleHeight(direction, 24);
            if (!TryRaySphere(rayOrigin, rayDirection, body.RadiusMetres + elevation, out rayDistance)) return false;
            direction = (rayOrigin + rayDirection * rayDistance).Normalized();
        }
        if (terrain.IsValid) elevation = terrain.SampleHeight(direction, 24);

        var anchor = SurfaceAnchorFocus.AtDirection(body.BodyId, direction, body.RadiusMetres, elevation);
        var root = new UniversePosition(
            body.Position.Value + body.BodyFixedToRoot.Rotate(anchor.BodyLocalPosition),
            body.Position.Frame);
        result = new(anchor, root, rayDistance, refinements);
        return result.IsValid;
    }

    public static double SurfaceAltitude(
        in PlanetRenderProxy body,
        in Double3 cameraRoot,
        in PlanetaryTerrainDefinition terrain)
    {
        if (!body.IsValid || !cameraRoot.IsFinite) throw new ArgumentOutOfRangeException();
        var bodyLocal = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(cameraRoot - body.Position.Value);
        var radius = Math.Sqrt(bodyLocal.LengthSquared);
        if (!(radius > 0d) || !double.IsFinite(radius)) return double.NaN;
        var surfaceRadius = body.RadiusMetres + (terrain.IsValid ? terrain.SampleHeight(bodyLocal / radius, 24) : 0d);
        return radius - surfaceRadius;
    }

    public static double EnforceClearanceDistance(
        in PlanetRenderProxy body,
        in Double3 targetRoot,
        in Double3 cameraOffsetDirectionRoot,
        double requestedDistance,
        in PlanetaryTerrainDefinition terrain,
        double clearanceMetres)
    {
        if (!body.IsValid || !targetRoot.IsFinite || !cameraOffsetDirectionRoot.IsFinite ||
            cameraOffsetDirectionRoot.LengthSquared <= 0d || !double.IsFinite(requestedDistance) || requestedDistance < 0d ||
            !double.IsFinite(clearanceMetres) || clearanceMetres < 0d) throw new ArgumentOutOfRangeException();
        var direction = cameraOffsetDirectionRoot.Normalized();
        if (SurfaceAltitude(body, targetRoot + direction * requestedDistance, terrain) >= clearanceMetres) return requestedDistance;

        var low = requestedDistance;
        var high = Math.Max(requestedDistance + clearanceMetres + 1d, 1d);
        for (var expansion = 0; expansion < 64 && SurfaceAltitude(body, targetRoot + direction * high, terrain) < clearanceMetres; expansion++)
            high = Math.Max(high * 2d, high + body.RadiusMetres);
        if (!double.IsFinite(high)) throw new InvalidOperationException("A finite camera clearance distance could not be found.");
        for (var iteration = 0; iteration < 52; iteration++)
        {
            var middle = low + (high - low) * .5d;
            if (SurfaceAltitude(body, targetRoot + direction * middle, terrain) >= clearanceMetres) high = middle;
            else low = middle;
        }
        return high;
    }

    /// <summary>
    /// Applies the final camera-origin terrain invariant in body-fixed FP64.
    /// The proposed origin, rather than its visual-aim line or pivot, owns the
    /// correction direction. This prevents an inward-facing orbit line from
    /// finding the opposite-side surface exit while retaining the caller's
    /// independent aim/orientation authority.
    /// </summary>
    public static bool TryConstrainCameraOrigin(
        in PlanetRenderProxy body,
        in Double3 proposedCameraRoot,
        in PlanetaryTerrainDefinition terrain,
        double minimumClearanceMetres,
        double correctionTargetMetres,
        out CameraExteriorConstraintResult result)
    {
        result = default;
        if (!body.IsValid || !proposedCameraRoot.IsFinite ||
            !double.IsFinite(minimumClearanceMetres) || minimumClearanceMetres < 0d ||
            !double.IsFinite(correctionTargetMetres) || correctionTargetMetres < minimumClearanceMetres)
            return false;

        var rootToBody = body.BodyFixedToRoot.Conjugate().Normalized();
        var bodyToRoot = body.BodyFixedToRoot.Normalized();
        var bodyLocal = rootToBody.Rotate(proposedCameraRoot - body.Position.Value);
        var correction = 0d;
        var iterations = 0;
        var terrainQueries = 0;
        var altitude = double.NaN;

        for (var iteration = 0; iteration <= CameraExteriorMaximumIterations; iteration++)
        {
            var radius = Math.Sqrt(bodyLocal.LengthSquared);
            if (!(radius > 0d) || !double.IsFinite(radius)) return false;
            var direction = bodyLocal / radius;
            var terrainHeight = terrain.IsValid ? terrain.SampleHeight(direction, 24) : 0d;
            terrainQueries++;
            if (!double.IsFinite(terrainHeight)) return false;
            altitude = radius - (body.RadiusMetres + terrainHeight);
            if (altitude >= minimumClearanceMetres) break;
            if (iteration == CameraExteriorMaximumIterations) return false;

            var requiredRadius = body.RadiusMetres + terrainHeight + correctionTargetMetres;
            correction += Math.Max(0d, requiredRadius - radius);
            bodyLocal = direction * requiredRadius;
            iterations++;

            // Recreate the exact root-space transport consumed by presentation,
            // then recover body-local state before the next bounded verification.
            // The millimetre correction target absorbs the measured AU-scale
            // recomposition loss without making root space surface authority.
            var transportedRoot = body.Position.Value + bodyToRoot.Rotate(bodyLocal);
            bodyLocal = rootToBody.Rotate(transportedRoot - body.Position.Value);
        }

        var finalRoot = body.Position.Value + bodyToRoot.Rotate(bodyLocal);
        result = new(finalRoot, bodyLocal, altitude, correction, iterations, terrainQueries);
        return result.IsValid && altitude >= minimumClearanceMetres;
    }

    public static bool TryMeasureCameraTransport(
        in PlanetRenderProxy body,
        in Double3 cameraRoot,
        in PlanetaryTerrainDefinition terrain,
        out CameraExteriorTransportIdentity identity)
    {
        identity = default;
        if (!body.IsValid || !cameraRoot.IsFinite) return false;
        var bodyLocal = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(cameraRoot - body.Position.Value);
        var radius = Math.Sqrt(bodyLocal.LengthSquared);
        if (!(radius > 0d) || !double.IsFinite(radius)) return false;
        var direction = bodyLocal / radius;
        var surfaceRadius = body.RadiusMetres + (terrain.IsValid ? terrain.SampleHeight(direction, 24) : 0d);
        var encoded = EncodedPosition.Encode(bodyLocal);
        var gpuBody = new Double3(
            (double)encoded.HighX + encoded.LowX,
            (double)encoded.HighY + encoded.LowY,
            (double)encoded.HighZ + encoded.LowZ);
        var gpuRadius = Math.Sqrt(gpuBody.LengthSquared);
        if (!(gpuRadius > 0d) || !double.IsFinite(gpuRadius)) return false;
        var gpuDirection = gpuBody / gpuRadius;
        var gpuSurfaceRadius = body.RadiusMetres + (terrain.IsValid ? terrain.SampleHeight(gpuDirection, 24) : 0d);
        identity = new(bodyLocal, gpuBody, surfaceRadius, gpuSurfaceRadius,
            radius - surfaceRadius, gpuRadius - gpuSurfaceRadius);
        return identity.IsFinite;
    }

    /// <summary>Places a close-surface camera at an explicit physical altitude in its proposed body-fixed direction.</summary>
    public static bool TryPlaceCameraOriginAtSurfaceAltitude(
        in PlanetRenderProxy body,
        in Double3 proposedCameraRoot,
        in PlanetaryTerrainDefinition terrain,
        double surfaceAltitudeMetres,
        out Double3 cameraRoot,
        out int terrainQueries)
    {
        cameraRoot = default;
        terrainQueries = 0;
        if (!body.IsValid || !proposedCameraRoot.IsFinite || !double.IsFinite(surfaceAltitudeMetres) || surfaceAltitudeMetres < 0d)
            return false;
        var rootToBody = body.BodyFixedToRoot.Conjugate().Normalized();
        var bodyLocal = rootToBody.Rotate(proposedCameraRoot - body.Position.Value);
        var radius = Math.Sqrt(bodyLocal.LengthSquared);
        if (!(radius > 0d) || !double.IsFinite(radius)) return false;
        var direction = bodyLocal / radius;
        var terrainHeight = terrain.IsValid ? terrain.SampleHeight(direction, 24) : 0d;
        terrainQueries = 1;
        if (!double.IsFinite(terrainHeight)) return false;
        bodyLocal = direction * (body.RadiusMetres + terrainHeight + surfaceAltitudeMetres);
        cameraRoot = body.Position.Value + body.BodyFixedToRoot.Normalized().Rotate(bodyLocal);
        return cameraRoot.IsFinite;
    }

    private static bool TryRaySphere(in Double3 origin, in Double3 direction, double radius, out double distance)
    {
        distance = double.NaN;
        if (!origin.IsFinite || !direction.IsFinite || !double.IsFinite(radius) || radius <= 0d) return false;
        var b = Double3.Dot(origin, direction);
        var c = origin.LengthSquared - radius * radius;
        var discriminant = b * b - c;
        if (!double.IsFinite(discriminant) || discriminant < 0d) return false;
        var root = Math.Sqrt(discriminant);
        var near = -b - root;
        var far = -b + root;
        distance = near >= 0d ? near : far >= 0d ? far : double.NaN;
        return double.IsFinite(distance);
    }
}

public static class SurfaceFocusHandoffPolicy
{
    public const double AcquisitionAltitudeMetres = 2_000_000d;
    public const double FullSurfaceAltitudeMetres = 1_000_000d;
    public const double ReleaseAltitudeMetres = 3_000_000d;
    // Ten metres is the player-facing physical floor. Camera positions are
    // reconstructed in root space at astronomical coordinate magnitudes, so a
    // separate millimetre guard prevents a correction placed exactly on the
    // floor from losing a few root-space FP64 ulps during recomposition.
    public const double MinimumTerrainClearanceMetres = 10d;
    public const double TerrainClearanceNumericalGuardBandMetres = .001d;
    public const double TerrainClearanceInvariantToleranceMetres = .0001d;
    public const double TerrainClearanceCorrectionTargetMetres =
        MinimumTerrainClearanceMetres + TerrainClearanceNumericalGuardBandMetres;

    public static bool SatisfiesMinimumTerrainClearance(double altitudeMetres) =>
        double.IsFinite(altitudeMetres) &&
        altitudeMetres >= MinimumTerrainClearanceMetres - TerrainClearanceInvariantToleranceMetres;

    public static bool ShouldAcquire(double altitudeMetres) =>
        double.IsFinite(altitudeMetres) && altitudeMetres <= AcquisitionAltitudeMetres;

    public static bool ShouldRelease(double altitudeMetres) =>
        double.IsFinite(altitudeMetres) && altitudeMetres >= ReleaseAltitudeMetres;

    public static double SurfaceBlend(double altitudeMetres)
    {
        if (!double.IsFinite(altitudeMetres)) throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        var amount = Math.Clamp(
            (AcquisitionAltitudeMetres - altitudeMetres) /
            (AcquisitionAltitudeMetres - FullSurfaceAltitudeMetres), 0d, 1d);
        return amount * amount * (3d - 2d * amount);
    }

    public static Double3 BlendedRoot(in Double3 bodyCenterRoot, in Double3 anchorRoot, double surfaceBlend)
    {
        if (!bodyCenterRoot.IsFinite || !anchorRoot.IsFinite || !double.IsFinite(surfaceBlend))
            throw new ArgumentOutOfRangeException();
        return bodyCenterRoot + (anchorRoot - bodyCenterRoot) * Math.Clamp(surfaceBlend, 0d, 1d);
    }

    public static double SurfaceBlendForCameraOffset(
        in PlanetRenderProxy body,
        in Double3 anchorRoot,
        in Double3 inertialCameraOffset,
        in PlanetaryTerrainDefinition terrain)
    {
        if (!body.IsValid || !anchorRoot.IsFinite || !inertialCameraOffset.IsFinite)
            throw new ArgumentOutOfRangeException();

        var evaluatedBody = body;
        var evaluatedAnchorRoot = anchorRoot;
        var evaluatedCameraOffset = inertialCameraOffset;
        var evaluatedTerrain = terrain;
        var low = 0d;
        var high = 1d;
        var lowResidual = BlendResidual(low);
        if (lowResidual <= 0d) return 0d;
        var highResidual = BlendResidual(high);
        if (highResidual >= 0d) return 1d;
        for (var iteration = 0; iteration < 40; iteration++)
        {
            var middle = low + (high - low) * .5d;
            if (BlendResidual(middle) > 0d) low = middle; else high = middle;
        }
        return Math.Clamp(low + (high - low) * .5d, 0d, 1d);

        double BlendResidual(double weight)
        {
            var focusRoot = BlendedRoot(evaluatedBody.Position.Value, evaluatedAnchorRoot, weight);
            var altitude = SurfaceAnchorAcquisition.SurfaceAltitude(evaluatedBody, focusRoot + evaluatedCameraOffset, evaluatedTerrain);
            return SurfaceBlend(altitude) - weight;
        }
    }
}

/// <summary>Presentation-only body-attached scene object used to prove the future vessel focus seam.</summary>
public readonly record struct BodyFixedSceneObject(ulong ObjectId, ulong ParentBodyId, Double3 BodyLocalPosition)
{
    public bool IsValid => ObjectId != 0 && ParentBodyId != 0 && BodyLocalPosition.IsFinite;

    public bool TryEvaluate(in PlanetRenderProxy parentBody, out UniversePosition rootPosition)
    {
        rootPosition = default;
        if (!IsValid || !parentBody.IsValid || parentBody.BodyId != ParentBodyId) return false;
        rootPosition = new UniversePosition(
            parentBody.Position.Value + parentBody.BodyFixedToRoot.Rotate(BodyLocalPosition),
            parentBody.Position.Frame);
        return rootPosition.Value.IsFinite;
    }
}
