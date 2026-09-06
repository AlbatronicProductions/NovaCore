using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class EarthRouteConvergenceTests
{
    internal static void Run()
    {
        FacilitySupportTests.LoadPhysicalData();
        var routes = new (string[] Arguments, string Scene, NativePresentationFocus Focus, double? Altitude, string Site)[]
        {
            (["--scene=sol"], "sol", NativePresentationFocus.None, null, "land"),
            (["--scene=earth", "--altitude=3000000", "--surface-site=land"], "earth", NativePresentationFocus.None, 3000000, "land"),
            (["--scene=earth", "--altitude=700000", "--surface-site=land"], "earth", NativePresentationFocus.None, 700000, "land"),
            (["--scene=sol", "--focus=earth", "--altitude=700000"], "sol", NativePresentationFocus.Earth, 700000, "land"),
            (["--scene=sol", "--focus=earth", "--surface-site=florida-launch"], "sol", NativePresentationFocus.Earth, null, "florida-launch")
        };
        foreach (var route in routes)
        {
            Require(SampleOptions.TryParse(route.Arguments, out var options, out var error), error ?? "parse");
            Require(options.Scene == route.Scene && options.InitialFocus == route.Focus &&
                options.AltitudeMetres == route.Altitude && options.SurfaceSite == route.Site, "scenario intent changed");
            Require(options.UseProductionEarth && options.PhysicalSurfaceGeneration == PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate,
                "ordinary route must select NCSM1 and generation 4, never the compatibility owner");
        }
        foreach (var args in new string[][]
        {
            ["--scene=earth", "--physical-surface=generation-3"],
            ["--scene=earth", "--planetary-mode=validate"],
            ["--scene=earth", "--planetary-mode=cpu"],
            ["--scene=earth", "--dynamic-traversal"],
            ["--scene=earth", "--m12c-fixed-pose", "--altitude=100", "--benchmark-frames=100"],
            ["--scene=earth", "--gpu-capacity=100"]
        })
        {
            Require(!SampleOptions.TryParse(args, out _, out _), "retired Earth renderer mode must be rejected");
        }
        var previous=PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            var root=new ReferenceFrameId(1);
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.Generation3);
            Require(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var oldScene,out _),"generation-3 Florida reference");
            var oldSite=oldScene!.FloridaLaunchSite;
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            Require(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error),error??"Solar create");
            var solar=value!;
            var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,solar.Projection,CameraMode.Free);
            solar.ResetPresentationCamera(camera);
            Require(!solar.ProductionSurfaceEligible && solar.Presentation.Count==10,"overview changed or Earth inherited by Sun");
            foreach(var body in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Mars,NativePresentationFocus.Saturn,NativePresentationFocus.Earth})
            {
                Require(solar.Focus(camera,body),"focus switch");solar.Update(camera);
                Require(solar.ProductionSurfaceEligible==(body==NativePresentationFocus.Earth),"Earth authority leaked to another body");
            }
            Require(solar.TryStartAtFloridaLaunchSite(camera),"Florida startup");
            var site=solar.FloridaLaunchSite;
            Require(site.Object.Id==oldSite.Object.Id && site.Object.Anchor.BodyId==oldSite.Object.Anchor.BodyId &&
                site.Object.Anchor.NormalizedBodyFixedDirection==oldSite.Object.Anchor.NormalizedBodyFixedDirection &&
                site.Object.Anchor.TerrainAuthorityVersion==oldSite.Object.Anchor.TerrainAuthorityVersion,
                "physical generation migration changed authored site identity");
            Console.WriteLine($"Florida generation migration: height={oldSite.AnchorTerrainHeightMetres:R}->{site.AnchorTerrainHeightMetres:R}; foundation={oldSite.FoundationOffsetMetres:R}->{site.FoundationOffsetMetres:R}; authoredIdentity=unchanged; placement=canonical-generation-4");
            Require(site.LatitudeDegrees==FloridaLaunchSite.Latitude && site.LongitudeDegrees==FloridaLaunchSite.Longitude,"Florida geography changed");
            Require(solar.TryEvaluateFloridaLaunchSite(out var pose),"pad evaluation");
            Require(solar.TryGetFloridaLaunchSitePresentation(camera,out _,out _),"pad not submitted at startup");
            var terrain=new PlanetaryPhysicalTerrainAuthority(6,PlanetaryTerrainDefinition.EarthProductionCubeV5);
            Require(terrain.TrySampleHeight(6,site.Object.Anchor.NormalizedBodyFixedDirection,out var height)&&height==site.AnchorTerrainHeightMetres,"pad uses a different physical authority");
            Require(solar.DetachSurfaceCamera(camera)&&solar.TryAttachSurfaceCamera(camera),"surface detach/attach");
            Require(solar.TryStartAtFloridaValidationAltitude(camera,700000),"Florida ascent");
            Require(solar.TryStartAtFloridaLaunchSite(camera),"Florida return");
            Require(solar.FloridaLaunchSite==site&&solar.TryEvaluateFloridaLaunchSite(out var returned)&&returned==pose,"Florida anchor or pad shifted across return");
            solar.EnforceFinalCameraInvariant(camera);
            Require(solar.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,"Florida clearance");
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous); }
    }
    private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
}
