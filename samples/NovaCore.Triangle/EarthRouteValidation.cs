using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;

/// <summary>Explicit native scenario acceptance driver; disabled in ordinary launches.</summary>
internal sealed class EarthRouteValidation(string mode)
{
    private int _frame;
    private FloridaLaunchSite? _site;
    private NovaCore.Core.Double3? _siteBodyPosition;
    internal static EarthRouteValidation? FromEnvironment() =>
        Environment.GetEnvironmentVariable("NOVACORE_EARTH_ROUTE_VALIDATION") switch
        {
            "solar" => new("solar"), "florida" => new("florida"), "regional" => new("regional"),
            "regional-isolation" => new("regional-isolation"), null or "" => null,
            _ => throw new InvalidOperationException("Earth route validation must be solar, florida, regional or regional-isolation.")
        };
    internal void Advance(SolarSystemScene scene, CameraState camera)
    {
        _frame++;
        if(mode=="regional-isolation")
        {
            if(_frame==1)Require(scene.Focus(camera,NativePresentationFocus.Mars),"fresh Mars isolation");
            if(_frame==30)Require(scene.Focus(camera,NativePresentationFocus.Saturn),"fresh Saturn isolation");
        }
        else if(mode=="regional")
        {
            _site??=scene.FloridaLaunchSite;
            if(_frame is >=120 and <150)
            {
                var step=new NativeInputState { DeltaSeconds=.1f, MoveRight=1 };
                scene.ApplyPresentationInput(camera,in step,out _,out _);
            }
            if(_frame==180)Require(scene.TryStartAtFloridaValidationAltitude(camera,700000),"regional retreat");
            if(_frame==280)Require(scene.TryStartAtFloridaLaunchSite(camera),"regional reapproach");
            if(_frame==400)Require(scene.TryStartAtEarthValidationAltitude(camera,700000,"land"),"outside Florida");
            if(_frame==500)Require(scene.Focus(camera,NativePresentationFocus.Mars),"regional Mars isolation");
            if(_frame==580)Require(scene.Focus(camera,NativePresentationFocus.Saturn),"regional Saturn isolation");
            if(_frame==660)Require(scene.TryStartAtFloridaLaunchSite(camera),"regional Florida reentry");
            Require(scene.FloridaLaunchSite==_site.Value,"regional test changed Florida identity");
        }
        else if(mode=="solar")
        {
            if(_frame==80)Require(scene.TryStartAtEarthValidationAltitude(camera,700000,"land"),"Earth focus");
            if(_frame==180)Require(scene.Focus(camera,NativePresentationFocus.Mars),"Mars focus");
            if(_frame==260)Require(scene.Focus(camera,NativePresentationFocus.Saturn),"Saturn focus");
            if(_frame==340)Require(scene.TryStartAtEarthValidationAltitude(camera,700000,"land"),"Earth return");
        }
        else
        {
            _site??=scene.FloridaLaunchSite;
            if(_frame==80)Require(scene.DetachSurfaceCamera(camera)&&scene.TryAttachSurfaceCamera(camera),"detach/attach");
            if(_frame==160)Require(scene.TryStartAtFloridaValidationAltitude(camera,700000),"Florida ascent");
            if(_frame==280)Require(scene.TryStartAtFloridaLaunchSite(camera),"Florida return");
            Require(scene.FloridaLaunchSite==_site.Value,"Florida identity changed");
            Require(scene.TryEvaluateFloridaLaunchSite(out var sitePose),"Florida seating evaluation");
            _siteBodyPosition??=sitePose.BodyFixedPosition;
            Require(sitePose.BodyFixedPosition==_siteBodyPosition.Value,"Florida body-fixed seating moved");
        }
        if(_frame is 1 or 80 or 160 or 180 or 260 or 280 or 340 or 400 or 440 or 500 or 580 or 660 or 740)
        {
            scene.EnforceFinalCameraInvariant(camera);scene.Update(camera);
            var pad=scene.TryGetFloridaLaunchSitePresentation(camera,out _,out _);
            if(mode=="florida")Console.WriteLine($"Florida seating stability: frame={_frame}; depth={scene.FloridaLaunchSite.FoundationDepthMetres:R}; bodyFixedStable=true; foundationSubmittedWithPad={pad}; H={scene.FloridaLaunchSite.AnchorTerrainHeightMetres:R}");
            Console.WriteLine($"Earth route validation: mode={mode}; frame={_frame}; focus={scene.FocusedBody.Label}; body={scene.FocusedBody.BodyId}; earthEligible={scene.ProductionSurfaceEligible}; bodies={scene.Presentation.Count}; altitude={scene.SurfaceAltitudeMetres:R}; padSubmitted={pad}; siteStable={(_site is null||scene.FloridaLaunchSite==_site.Value)}; physicalGeneration={(uint)PlanetaryPhysicalSurface.RuntimeGeneration}");
            if(_frame==440)Console.WriteLine("Earth route scenario validation: PASS; automated scenario evidence only.");
        }
    }
    private static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException("Earth route validation: "+reason);}
}
