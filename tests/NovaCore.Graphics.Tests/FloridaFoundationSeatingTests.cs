using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Time;

internal static class FloridaFoundationSeatingTests
{
    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        Require(EarthElevationDataset.TryLoad(Path.Combine(root, "assets", "earth", "runtime"), out var error), error);
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out error), error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(localPath, out error), error);
        var previous = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            Require(SampleOptions.TryParse(["--scene=sol", "--focus=earth", "--surface-site=florida-launch"], out var options, out error), error ?? "Florida route");
            Require(options.UseProductionEarth && options.PhysicalSurfaceGeneration == PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate,
                "ordinary Florida must use generation 4 and NCSM1");
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(options.PhysicalSurfaceGeneration);
            var frame = new ReferenceFrameId(1);
            Require(SolarSystemScene.TryCreateAt(frame, SimulationInstant.Zero, out var value, out error), error ?? "Solar scene");
            var scene = value!; var site = scene.FloridaLaunchSite;
            var camera = new CameraState(new FramePosition(frame, Double3.Zero), DoubleQuaternion.Identity, scene.Projection, CameraMode.Free);
            Require(scene.TryStartAtFloridaLaunchSite(camera) && scene.ProductionSurfaceEligible, "Florida production startup");
            Require(scene.TryEvaluateFloridaLaunchSite(out var pose), "Florida physical pose");
            var direction = site.Object.Anchor.NormalizedBodyFixedDirection;
            Require(direction == BodyFixedGeography.DirectionFromLatitudeLongitude(28.6084d * Math.PI / 180d, -80.6042d * Math.PI / 180d).Normalized() &&
                site.Object.Id == FloridaLaunchSite.ObjectId && site.Object.Anchor.BodyId == 6 &&
                site.Object.LocalEnuPositionOffsetMetres == Double3.Zero && site.Object.LocalEnuOrientation == DoubleQuaternion.Identity,
                "authored geography, pivot and heading unchanged");
            Require(scene.CurrentSurfaceCameraState.Anchor == site.Object.Anchor, "camera retains the slab anchor");
            var radius = scene.FocusedBody.RadiusMetres;
            var terrain = new PlanetaryPhysicalTerrainAuthority(6, PlanetaryTerrainDefinition.EarthProductionCubeV5);
            Require(terrain.TrySampleHeight(6, direction, out var height) && height == site.AnchorTerrainHeightMetres, "same canonical generation-4 height");
            Require(EarthLocalTerrainElevationDataset.SampleResidual(direction) != 0, "installed Florida residual participates before seating");
            Require(site.FoundationScale == new Double3(64, 48, site.FoundationDepthMetres), "foundation dimensions preserve the slab footprint");

            // Independently intersect the physical surface with vertical lines by bisection, rather than
            // repeating the placement fixed-point algorithm. One micrometre bounds numerical agreement;
            // the physical embed is separately required to be 25 cm.
            var minimum = PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(terrain.Terrain,direction)-(site.LocalPhysicalSurfaceRadiusMetres-radius);
            foreach (var (east, north) in Perimeter(FloridaLaunchSite.FoundationSurveySpacingMetres))
            {
                var lower = -100d; var upper = 100d;
                for (var iteration = 0; iteration < 40; iteration++)
                {
                    var middle = (lower + upper) * .5d;
                    if (NaturalGap(east, north, middle) > 0) upper = middle; else lower = middle;
                }
                minimum = Math.Min(minimum, (lower + upper) * .5d);
            }
            Require(Math.Abs(site.MinimumTerrainLocalUpMetres - minimum) < 1e-6d, "canonical contact survey agrees within one micrometre");
            Require(Math.Abs(-site.FoundationDepthMetres + FloridaLaunchSite.FoundationEmbedMetres - minimum) < 1e-6d,
                "actual unit-mesh bottom plus structural embed equals lowest canonical contact");

            // Four times denser than the placement survey: all foundation edges penetrate ground,
            // while the unchanged deck top stays above it. This catches a missing or inverted footing.
            var maximumBottomGap = double.NegativeInfinity;
            var minimumDeckClearance = double.PositiveInfinity;
            foreach (var (east, north) in Perimeter(.0625d))
            {
                maximumBottomGap = Math.Max(maximumBottomGap, Gap(east, north, -site.FoundationDepthMetres));
                minimumDeckClearance = Math.Min(minimumDeckClearance, Gap(east, north, FloridaLaunchSite.PlatformThicknessMetres));
            }
            Require(Math.Abs(maximumBottomGap) < .001d, $"authored support meets the unchanged foundation within 1 mm: actual={maximumBottomGap:R}; depth={site.FoundationDepthMetres:R}; minimum={minimum:R}");
            Require(minimumDeckClearance > 0, "the deck is not buried");
            Require(Math.Abs(Gap(0, 0, -site.FoundationOffsetMetres)) < 1e-6d, "center physical contact is H, independently of model origin");

            var levels = PlanetaryProductionSphericalBillboardTopologyLibrary.Load(Path.Combine(root, "assets", "planetary-production-topology"));
            foreach (var level in new[] { 8, 12, 17 })
            {
                var topology = levels[level];
                var pupil = PlanetaryProductionBillboardPupil.Resolve(default, direction, topology);
                var moved = (direction + pupil.Tangent.East * (topology.Snap.PupilCellRadians * (topology.Snap.CandidateShiftMultiple + 1))).Normalized();
                var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, moved, topology);
                Require(snapped.Generation > pupil.Generation, "exercise a real pupil rebase");
                Require(FloridaLaunchSite.TryCreate(6, radius, terrain.Terrain, out var rebuilt) && rebuilt == site &&
                    scene.TryEvaluateFloridaLaunchSite(out var unchanged) && unchanged.BodyFixedPosition == pose.BodyFixedPosition,
                    "LOD/pupil state cannot affect physical site or footing dimensions");
            }
            Require(scene.DetachSurfaceCamera(camera) && scene.TryAttachSurfaceCamera(camera) &&
                scene.TryStartAtFloridaValidationAltitude(camera, 700000) && scene.TryStartAtFloridaLaunchSite(camera) &&
                scene.FloridaLaunchSite == site, "camera/ascent/return preserve seating");
            foreach (var seconds in new[] { 1L, 86400L, 31536000L })
            {
                Require(SolarSystemScene.TryCreateAt(frame, SimulationInstant.FromWholeSeconds(seconds), out var advanced, out error) &&
                    advanced!.FloridaLaunchSite == site && advanced.TryEvaluateFloridaLaunchSite(out var advancedPose) &&
                    advancedPose.BodyFixedPosition == pose.BodyFixedPosition && advancedPose.BodyFixedOrientation == pose.BodyFixedOrientation,
                    "Earth rotation changes only the derived root pose");
            }
            // Native model contract matters: scaling a centered box would leave half the old gap.
            var native = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.cpp"));
            var authored = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "AuthoredFacilityGeometry.h"));
            Require(authored.Contains("FoundationUnit{{-.5f,-.5f,-1},{.5f,.5f,0}") &&
                authored.Contains("{{-32,-24,0},{32,24,1.5f}") &&
                native.Contains("nc::facility::FoundationUnit;box(") &&
                native.Contains("nc::facility::LaunchPadBoxes)box("),
                "shared authored native footing ends at the unchanged slab origin and extends one unit down");
            Require(MeshHandle.FloridaLaunchPad.Value == 3 && MeshHandle.FloridaLaunchFoundation.Value == 4, "distinct persistent slab and footing meshes");
            Console.WriteLine($"Florida seating proof: H={height:R}; root={height + site.FoundationOffsetMetres:R}; minimumContactLocalZ={minimum:R}; depth={site.FoundationDepthMetres:R}; bottomCenterHeight={height + site.FoundationOffsetMetres - site.FoundationDepthMetres:R}; denseMaximumBottomGap={maximumBottomGap:R}; denseMinimumDeckClearance={minimumDeckClearance:R}; regional={EarthLocalTerrainElevationDataset.SampleResidual(direction):R}; generation=4; owner=NCSM1; geography/time/LOD/pupil/return=stable");

            double Gap(double east, double north, double up)
            {
                var point = pose.BodyFixedPosition + pose.Enu.East * east + pose.Enu.North * north + pose.Enu.Up * up;
                Require(terrain.TrySampleHeight(6, point.Normalized(), out var terrainHeight), "footprint canonical query");
                return Math.Sqrt(point.LengthSquared) - radius - terrainHeight;
            }
            double NaturalGap(double east,double north,double up)
            {
                var point=pose.BodyFixedPosition+pose.Enu.East*east+pose.Enu.North*north+pose.Enu.Up*up;
                return Math.Sqrt(point.LengthSquared)-radius-PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(terrain.Terrain,point.Normalized());
            }
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous); }
    }

    private static IEnumerable<(double East, double North)> Perimeter(double spacing)
    {
        for (var east = -32d; east <= 32d; east += spacing) { yield return (east, -24); yield return (east, 24); }
        for (var north = -24d + spacing; north < 24d; north += spacing) { yield return (-32, north); yield return (32, north); }
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
