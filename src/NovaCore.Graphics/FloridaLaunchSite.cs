using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>Immutable authored and terrain-seated definition of NovaCore's Florida launch-site proof.</summary>
public readonly record struct FloridaLaunchSite(
    AnchoredSurfaceObject Object,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double AnchorTerrainHeightMetres,
    double FoundationOffsetMetres,
    double LocalPhysicalSurfaceRadiusMetres)
{
    public const double Latitude = 28.6084d;
    public const double Longitude = -80.6042d;
    public const double PlatformEastWidthMetres = 64d;
    public const double PlatformNorthLengthMetres = 48d;
    public const double PlatformThicknessMetres = 1.5d;
    public const double MountHeightMetres = 7d;
    public const double FoundationMarginMetres = .25d;
    /// <summary>Authored footing extends 25 cm below the original natural survey; support grading meets that unchanged bottom.</summary>
    public const double FoundationEmbedMetres = .25d;
    public const double FoundationSurveySpacingMetres = .25d;
    public const double MaximumRenderDistanceMetres = 150_000d;
    public static AnchoredSurfaceObjectId ObjectId => new(0x4E435F464C5F5044UL); // NC_FL_PD
    public static SurfaceGeometryId GeometryId => new(0x4C504144u, 1u); // LPAD

    /// <summary>Lowest original natural-ground survey intersection in the pad's local ENU frame (including the center).</summary>
    public double MinimumTerrainLocalUpMetres { get; init; }
    /// <summary>Depth of the separate foundation below the unchanged slab origin. Its unit mesh spans Z=-1..0.</summary>
    public double FoundationDepthMetres { get; init; }
    public Double3 FoundationScale => new(PlatformEastWidthMetres, PlatformNorthLengthMetres, FoundationDepthMetres);

    public bool IsValid => Object.IsValid && double.IsFinite(AnchorTerrainHeightMetres) &&
        double.IsFinite(FoundationOffsetMetres) && FoundationOffsetMetres >= FoundationMarginMetres &&
        double.IsFinite(LocalPhysicalSurfaceRadiusMetres) && LocalPhysicalSurfaceRadiusMetres > 0d &&
        double.IsFinite(MinimumTerrainLocalUpMetres) && double.IsFinite(FoundationDepthMetres) &&
        FoundationDepthMetres >= FoundationEmbedMetres;

    public static bool TryCreate(
        ulong earthBodyId,
        double earthRadiusMetres,
        in PlanetaryTerrainDefinition definition,
        out FloridaLaunchSite site)
    {
        site = default;
        if (earthBodyId == 0 || !double.IsFinite(earthRadiusMetres) || earthRadiusMetres <= 0d || !definition.IsValid) return false;
        var latitude = Latitude * Math.PI / 180d;
        var longitude = Longitude * Math.PI / 180d;
        var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(latitude, longitude);
        if (SurfaceAnchor.TryCreate(earthBodyId, new(definition.SourceId, definition.Version), direction, 0d,
            out var provisional) != SurfaceAnchorCreationStatus.Success || !SurfaceEnuFrame.TryCreate(provisional, out var enu)) return false;
        var terrain = new PlanetaryPhysicalTerrainAuthority(earthBodyId, definition);
        var surveyDefinition=definition;
        if (!NaturalHeight(direction, out var centerHeight)) return false;

        var maximumHeight = centerHeight;
        Span<Double2> offsets = stackalloc Double2[8]
        {
            new(-PlatformEastWidthMetres*.5d,-PlatformNorthLengthMetres*.5d),
            new( PlatformEastWidthMetres*.5d,-PlatformNorthLengthMetres*.5d),
            new(-PlatformEastWidthMetres*.5d, PlatformNorthLengthMetres*.5d),
            new( PlatformEastWidthMetres*.5d, PlatformNorthLengthMetres*.5d),
            new(-PlatformEastWidthMetres*.5d,0d), new(PlatformEastWidthMetres*.5d,0d),
            new(0d,-PlatformNorthLengthMetres*.5d), new(0d,PlatformNorthLengthMetres*.5d),
        };
        foreach (var offset in offsets)
        {
            var sampleDirection = (direction * earthRadiusMetres + enu.East * offset.X + enu.North * offset.Y).Normalized();
            if (!NaturalHeight(sampleDirection, out var height)) return false;
            maximumHeight = Math.Max(maximumHeight, height);
        }
        var foundationOffset = maximumHeight - centerHeight + FoundationMarginMetres;
        if (SurfaceAnchor.TryCreate(earthBodyId, terrain.AuthorityVersion, direction, foundationOffset,
            out var anchor) != SurfaceAnchorCreationStatus.Success) return false;
        var value = new AnchoredSurfaceObject(ObjectId, anchor, Double3.Zero, DoubleQuaternion.Identity, GeometryId);
        // Preserve the authored slab/camera anchor. Survey physical ground once, before any render publication,
        // to size a solid footing that fills the old gap. No LOD, pupil, material, or GPU topology enters this query.
        var rootRadius = earthRadiusMetres + centerHeight + foundationOffset;
        var minimumLocalUp = -foundationOffset;
        var eastHalf = PlatformEastWidthMetres * .5d;
        var northHalf = PlatformNorthLengthMetres * .5d;
        for (var east = -eastHalf; east <= eastHalf; east += FoundationSurveySpacingMetres)
            if (!Survey(east, -northHalf) || !Survey(east, northHalf)) return false;
        for (var north = -northHalf + FoundationSurveySpacingMetres; north < northHalf; north += FoundationSurveySpacingMetres)
            if (!Survey(-eastHalf, north) || !Survey(eastHalf, north)) return false;
        if(PlanetaryPhysicalSurface.RuntimeGeneration==PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate)
        {
            var contact=rootRadius+minimumLocalUp-FoundationEmbedMetres-earthRadiusMetres;
            if(Math.Abs(contact-FloridaFacilitySupport.ContactPlaneAltitudeMetres)>1e-6d)
                throw new InvalidOperationException("Florida support definition no longer matches its natural survey; author a new physical support revision.");
            // Preserve the exact existing slab root and footing dimensions. The
            // terrain-relative anchor now expresses the same root above support H.
            centerHeight=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(definition,direction);
            foundationOffset=rootRadius-earthRadiusMetres-centerHeight;
            if(SurfaceAnchor.TryCreate(earthBodyId,terrain.AuthorityVersion,direction,foundationOffset,out anchor)!=SurfaceAnchorCreationStatus.Success)return false;
            value=new AnchoredSurfaceObject(ObjectId,anchor,Double3.Zero,DoubleQuaternion.Identity,GeometryId);
        }
        site = new(value, Latitude, Longitude, centerHeight, foundationOffset,
            rootRadius)
        {
            MinimumTerrainLocalUpMetres = minimumLocalUp,
            FoundationDepthMetres = -minimumLocalUp + FoundationEmbedMetres
        };
        return site.IsValid;

        bool NaturalHeight(Double3 sampleDirection,out double height)
        {
            if(PlanetaryPhysicalSurface.RuntimeGeneration!=PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate)
                return terrain.TrySampleHeight(earthBodyId,sampleDirection,out height);
            height=PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(surveyDefinition,sampleDirection);
            return double.IsFinite(height);
        }

        bool Survey(double east, double north)
        {
            var lateral = enu.East * east + enu.North * north;
            var radial = rootRadius;
            // Intersect the vertical ENU line with H(direction), accounting for spherical curvature.
            // At this 64 x 48 m footprint the fixed point converges well inside the 0.1 micrometre tolerance.
            for (var iteration = 0; iteration < 8; iteration++)
            {
                var sampleDirection = (direction * radial + lateral).Normalized();
                if (!NaturalHeight(sampleDirection, out var height)) return false;
                var surfaceRadius = earthRadiusMetres + height;
                var next = Math.Sqrt(surfaceRadius * surfaceRadius - lateral.LengthSquared);
                if (!double.IsFinite(next)) return false;
                if (Math.Abs(next - radial) <= 1e-7d)
                {
                    minimumLocalUp = Math.Min(minimumLocalUp, next - rootRadius);
                    return true;
                }
                radial = next;
            }
            return false;
        }
    }
}
