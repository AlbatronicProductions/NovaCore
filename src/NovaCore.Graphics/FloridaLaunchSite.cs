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
    public const double MaximumRenderDistanceMetres = 150_000d;
    public static AnchoredSurfaceObjectId ObjectId => new(0x4E435F464C5F5044UL); // NC_FL_PD
    public static SurfaceGeometryId GeometryId => new(0x4C504144u, 1u); // LPAD

    public bool IsValid => Object.IsValid && double.IsFinite(AnchorTerrainHeightMetres) &&
        double.IsFinite(FoundationOffsetMetres) && FoundationOffsetMetres >= FoundationMarginMetres &&
        double.IsFinite(LocalPhysicalSurfaceRadiusMetres) && LocalPhysicalSurfaceRadiusMetres > 0d;

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
        if (!terrain.TrySampleHeight(earthBodyId, direction, out var centerHeight)) return false;

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
            if (!terrain.TrySampleHeight(earthBodyId, sampleDirection, out var height)) return false;
            maximumHeight = Math.Max(maximumHeight, height);
        }
        var foundationOffset = maximumHeight - centerHeight + FoundationMarginMetres;
        if (SurfaceAnchor.TryCreate(earthBodyId, terrain.AuthorityVersion, direction, foundationOffset,
            out var anchor) != SurfaceAnchorCreationStatus.Success) return false;
        var value = new AnchoredSurfaceObject(ObjectId, anchor, Double3.Zero, DoubleQuaternion.Identity, GeometryId);
        site = new(value, Latitude, Longitude, centerHeight, foundationOffset,
            earthRadiusMetres + centerHeight + foundationOffset);
        return site.IsValid;
    }
}
