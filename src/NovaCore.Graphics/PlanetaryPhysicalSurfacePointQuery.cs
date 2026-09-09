using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>
/// CPU adapter beside the existing canonical H oracle. Composition acquires it only after
/// verified global and complete regional CPU data publication, then injects the Core interface.
/// No renderer or simulation startup is required. The current oracle datasets publish once and
/// never mutate/unload; consequently an acquired authority cannot become partially resident.
/// </summary>
public sealed class PlanetaryPhysicalSurfacePointQuery : IPhysicalSurfacePointQuery
{
    // Existing physical normal parity ceiling. This is numerical qualification, not proof of
    // differentiability everywhere: unresolved creases and unstable stencils explicitly fail.
    public const double NormalAngularToleranceRadians = 5e-5;
    private const double MachineEpsilon = 2.2204460492503131e-16;
    private const int MaximumRefinements = 20;
    private const double Radius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
    private const PlanetaryPhysicalSurfaceGeneration Generation = PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate;
    private static readonly PlanetaryTerrainDefinition Terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;

    private PlanetaryPhysicalSurfacePointQuery(PhysicalSurfaceAuthorityIdentity authority) => Authority = authority;
    public PhysicalSurfaceAuthorityIdentity Authority { get; }

    /// <summary>
    /// repositoryRoot is the trusted application's content root. Authority comes from its fixed
    /// production asset manifest, not a caller-provided digest or a query-triggered load. This
    /// acquisition may read that small manifest; point queries never touch the filesystem.
    /// </summary>
    public static PhysicalSurfaceQueryStatus TryAcquire(ulong bodyId, string repositoryRoot,
        out PlanetaryPhysicalSurfacePointQuery? query)
    {
        query = null;
        if (bodyId == 0 || string.IsNullOrWhiteSpace(repositoryRoot)) return PhysicalSurfaceQueryStatus.InvalidInput;
        try { _ = Path.GetFullPath(repositoryRoot); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        { return PhysicalSurfaceQueryStatus.InvalidInput; }
        if (bodyId != PlanetaryPhysicalSurface.EarthBodyId) return PhysicalSurfaceQueryStatus.UnsupportedBody;
        if (!EarthElevationDataset.IsLoaded ||
            !EarthLocalTerrainElevationDataset.TryGetPublishedIdentity(out var header, out var sha256, out var bytes))
            return PhysicalSurfaceQueryStatus.RequiredDataNotReady;
        if (!TerrainAssetManifestFile.TryLoad(TerrainAssetRepository.ManifestPath(repositoryRoot,
                TerrainAssetCache.ProductionEarthLocalAssetId), out var manifest, out _))
            return PhysicalSurfaceQueryStatus.AuthorityUnavailable;
        if (manifest.AssetId != TerrainAssetCache.ProductionEarthLocalAssetId || manifest.BodyId != bodyId ||
            manifest.TerrainVersion != Terrain.Version || manifest.FormatVersion != PlanetaryLocalTerrainPackContract.Version ||
            header.BodyId != bodyId || header.TerrainVersion != Terrain.Version || header.Version != manifest.FormatVersion ||
            header.RecordCount != manifest.Hierarchy.RecordCount ||
            header.MinimumSectorLevel != manifest.Hierarchy.MinimumPayloadLevel ||
            header.MaximumSectorLevel != manifest.Hierarchy.MaximumPayloadLevel ||
            bytes != manifest.ByteSize || sha256 != manifest.Sha256)
            return PhysicalSurfaceQueryStatus.AuthorityMismatch;
        query = new(new(bodyId, new(Terrain.SourceId, Terrain.Version), (uint)Generation, Radius,
            EarthElevationDataset.Sha256, sha256,
            PlanetaryNaturalTerrainFamilies.ComputeManifestHash(PlanetaryPhysicalSurface.NaturalTerrainCandidateSeed),
            FloridaFacilitySupport.DefinitionIdentity, 1));
        return PhysicalSurfaceQueryStatus.Ready;
    }

    public PhysicalSurfacePointResult Query(ulong bodyId, in Double3 bodyFixedUnitDirection)
    {
        if (bodyId == 0 || !bodyFixedUnitDirection.IsFinite ||
            Math.Abs(bodyFixedUnitDirection.LengthSquared - 1d) > SurfaceAnchor.DirectionUnitLengthSquaredTolerance)
            return Failure(PhysicalSurfaceQueryStatus.InvalidInput);
        if (bodyId != Authority.BodyId) return Failure(PhysicalSurfaceQueryStatus.UnsupportedBody);
        var direction = bodyFixedUnitDirection.Normalized();
        var height = Height(direction);
        if (!double.IsFinite(height) || Radius + height <= 0d) return Failure(PhysicalSurfaceQueryStatus.AuthorityUnavailable);
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var east = frame.East; var north = frame.North;
        var crossEast = (east + north).Normalized(); var crossNorth = (north - east).Normalized();
        // R*cbrt(epsilon) balances central-difference truncation against FP64 directional
        // cancellation. Bounded refinement then qualifies the actual local H; no fixed metre
        // spacing or camera/LOD scale decides the normal.
        var h = Radius * Math.Cbrt(MachineEpsilon);
        var previous = Evaluate(direction, east, north, h, height);
        for (var level = 1; level <= MaximumRefinements; level++)
        {
            h *= .5;
            var current = Evaluate(direction, east, north, h, height);
            var limit = NormalAngularToleranceRadians / 8d;
            // H and these stencils are pure over the acquired immutable authority. Once a
            // required check fails, later stencils cannot qualify this refinement level.
            // Advance the same cardinal history, then defer their full-H work until needed.
            if (!(Angle(previous.Central, current.Central) < limit) || !(current.SideError < limit))
            {
                previous = current;
                continue;
            }
            var cross = Evaluate(direction, crossEast, crossNorth, h, height);
            if (!(cross.SideError < limit) || !(Angle(current.Central, cross.Central) < limit))
            {
                previous = current;
                continue;
            }
            var fine = Evaluate(direction, crossEast, crossNorth, h / 3d, height);
            if (fine.SideError < limit && Angle(current.Central, fine.Central) < limit &&
                fine.Central.IsFinite && Double3.Dot(fine.Central, direction) > 0d)
                return new(PhysicalSurfaceQueryStatus.Ready, Authority, direction, direction * (Radius + height),
                    height, fine.Central, h / 3d);
            previous = current;
        }
        return Failure(PhysicalSurfaceQueryStatus.NormalUnqualified);
    }

    private PhysicalSurfacePointResult Failure(PhysicalSurfaceQueryStatus status) => new(status, Authority, default, default, default, default, default);
    private static double Height(Double3 direction) => PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(Terrain, direction, Generation);
    private static Double3 Offset(Double3 direction, Double3 axis, double distance) => (direction + axis * (distance / Radius)).Normalized();
    private static double Angle(Double3 a, Double3 b) => Math.Atan2(Math.Sqrt(Double3.Cross(a, b).LengthSquared), Double3.Dot(a, b));
    private static Double3 Normal(Double3 direction, Double3 east, Double3 north, double x, double y, double height) =>
        (direction - (east * x + north * y) * (Radius / (Radius + height))).Normalized();

    private readonly record struct Stencil(Double3 Central, Double3 Left, Double3 Right)
    {
        public double SideError => Math.Max(Angle(Central, Left), Angle(Central, Right));
    }

    private static Stencil Evaluate(Double3 direction, Double3 east, Double3 north, double h, double center)
    {
        var ep = Height(Offset(direction, east, h)); var em = Height(Offset(direction, east, -h));
        var np = Height(Offset(direction, north, h)); var nm = Height(Offset(direction, north, -h));
        var ep2 = Height(Offset(direction, east, 2d * h)); var em2 = Height(Offset(direction, east, -2d * h));
        var np2 = Height(Offset(direction, north, 2d * h)); var nm2 = Height(Offset(direction, north, -2d * h));
        return new(Normal(direction, east, north, (ep - em) / (2d * h), (np - nm) / (2d * h), center),
            Normal(direction, east, north, (3d * center - 4d * em + em2) / (2d * h), (3d * center - 4d * nm + nm2) / (2d * h), center),
            Normal(direction, east, north, (-3d * center + 4d * ep - ep2) / (2d * h), (-3d * center + 4d * np - np2) / (2d * h), center));
    }
}
