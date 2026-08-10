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

/// <summary>Deterministic body-fixed ray/surface acquisition. It performs no streaming or filesystem work.</summary>
public static class SurfaceAnchorAcquisition
{
    public const int TerrainRefinementCount = 4;

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
    // Ten metres covers the current elevation sampling footprint and the 5 cm
    // near plane without allowing the camera to graze through interpolated terrain.
    public const double MinimumTerrainClearanceMetres = 10d;

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
