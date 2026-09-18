using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Launcher;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Translation;

internal static partial class AssemblyFloridaSiteTests
{
    internal static void SolarConsumer()
    {
        var preset=ScenarioCatalog.Get(NovaCoreScenarioPreset.FloridaLaunchSite);
        Check(ScenarioCatalog.TryCreateConfiguration(preset.Preset,null,preset.DefaultWindowMode,preset.DefaultResolution,
            preset.DefaultDiagnostics,1920,1080,out var config,out _),"FL launcher configuration");
        Check(config!.Scene==NovaCoreScene.Solar,"launcher preserves Solar world");
        Check(SampleOptions.TryParse(LaunchCommandBuilder.BuildArguments(config).ToArray(),out var gui,out _)&&gui.UsesFloridaSlab&&gui.Scene=="sol","GUI canonical Florida construction selection");
        Check(SampleOptions.TryParse(["--scene=srv01-florida-support"],out var manual,out _)&&manual.UsesFloridaSlab,"manual same canonical construction");
        _=Site(true);
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var solar,out _),"existing Solar owner");
        using var gameplay=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true,solarWorld:solar);
        using var qualification=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true);
        Check(ReferenceEquals(gameplay.FloridaView!.Solar,solar)&&gameplay.FloridaView.SolarNavigation&&!qualification.FloridaView!.SolarNavigation,"distinct consumers, retained Solar owner");
        Check(gameplay.FloridaView.Site.Digest==qualification.FloridaView!.Site.Digest&&gameplay.Observation.State==qualification.Observation.State,"same site/slab/spacecraft initial authority");
        var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1000),CameraMode.Free);
        gameplay.FloridaView.PrepareCamera(camera);
        var render=new RenderFrameSubmission(gameplay.RenderCapacity);
        gameplay.Start();qualification.Start();gameplay.Advance(new(16666));qualification.Advance(new(16666));
        Check(gameplay.Observation.State==qualification.Observation.State,"same canonical first interval");
        var authority=gameplay.Observation;
        Check(solar!.Presentation.Count>=10,"Solar bodies retained");
        foreach(var focus in new[]{NativePresentationFocus.Mars,NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Earth})
        {
            Check(solar.Focus(camera,focus),"existing planet focus");
            var before=camera.Position.Value;
            solar.ApplyPresentationInput(camera,new NativeInputState{DeltaSeconds=.016f,MouseWheelDetents=1},out _,out _);
            Check(camera.Position.Value!=before,"existing continuous zoom");
            Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _),"existing celestial progression");
            solar.Update(camera);gameplay.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
            Check(render.ObjectCount==38&&render.Objects[37].Mesh==MeshHandle.FloridaSupportSlab&&gameplay.Observation==authority,"Solar navigation cannot mutate spacecraft; only slab emitted");
        }
        solar.ResetPresentationCamera(camera);solar.Update(camera);
        Check(solar.OrbitVertices.Length>0&&solar.DistantBodies.Length>0,"Solar overview/orbits remain present");
        Check(gameplay.Observation==authority,"overview is presentation only");
        Console.WriteLine("FLORIDA_SOLAR_CONSUMER PASS launcher=sol sharedCanonicalScenario=true siteSlabSpacecraft=EQUAL manualHarnessSeparate=true planetFocus=PASS zoom=PASS overview=PASS celestialProgression=PASS physicalNonmutation=PASS");
        Check(SolarSystemScene.TryCreate(new(1),out var currentSolar,out _),"actual current UTC Solar epoch");
        using var current=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true,solarWorld:currentSolar);
        var session=Field<AssemblyApplicationSession>(current,"session");var start=current.Observation.State.Epoch;
        Check(start.Ticks!=0&&start==currentSolar!.CurrentTime&&current.FloridaView!.Site.Start==start,"GUI uses original current epoch");
        current.Start();var maxError=0d;var peak=0d;var finalSupported=0;
        for(var n=1;n<=1200;n++)
        {
            current.Advance(new(n*1_000_000L/60-(n-1)*1_000_000L/60));var v=current.Observation;
            Check(!current.Failed&&v.State.Frontier==n&&v.State.Epoch.Ticks==start.Ticks+n*1_000_000L/60,"current-epoch exact original schedule");
            Check(CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,v.State.Epoch,out var earth),"current banked Earth orientation");
            Check(SpacecraftMotionEvaluator.TryEvaluateAssembly(session.Engine.State,session.Launch.Spacecraft.Id,v.State.Epoch,out var actual)==SpacecraftTranslationStatus.Success,"current-epoch committed inertial observation");
            var region=NovaCore.Core.Surface.FloridaFacilitySupport.Region;var local=v.State.Motion.PositionO;
            var bf=region.Up*(6371008.8+23.470461536198854+1.7+local.Y)+region.East*local.X-region.North*local.Z;
            // The contact contract anchors the existing model at an exact whole second,
            // then advances local angles. Full-epoch Evaluate has a different large-phase
            // rounding boundary; it is not a nanometre oracle for this admitted contract.
            var whole=v.State.Epoch.Ticks/1_000_000;var fraction=(v.State.Epoch.Ticks%1_000_000)/1e6;
            var days=whole/86400d;var centuries=days/36525d;var rad=Math.PI/180;
            var ra=(90-.641*centuries)*rad-.641*rad/(86400*36525d)*fraction;
            var tilt=(90-(90-.557*centuries))*rad+.557*rad/(86400*36525d)*fraction;
            var spin=Math.IEEERemainder((190.147+360.9856235*days)*rad,Math.Tau)+360.9856235*rad/86400*fraction;
            var expected=(Rz(ra)*Rx(tilt)*Rz(spin)*Rx(Math.PI/2)).Apply(bf);
            maxError=Math.Max(maxError,Norm(actual.MaterialOriginMotion.PositionO-expected));
            Check(maxError<1e-8,$"current-epoch independent split-angle matrix oracle error={maxError:R}");
            var minimum=double.MaxValue;
            foreach(var child in session.Launch.ContactProfile!.Children)for(var i=0;i<8;i++)
            {
                var p=child.AtOrigin.Position+child.AtOrigin.Rotation.Apply(new(((i&1)==0?-.5:.5)*child.Dimensions.X,((i&2)==0?-.5:.5)*child.Dimensions.Y,((i&4)==0?-.5:.5)*child.Dimensions.Z));
                minimum=Math.Min(minimum,(v.State.Motion.PositionO+Q(v.State.Motion.BodyToWorld).Apply(p)).Y+1.7);
            }
            peak=Math.Max(peak,-minimum);Check(peak<=.020,"current-epoch original penetration bar");
            if(n>600){Check(Math.Abs(minimum)<=.000122&&Norm(v.State.Motion.VelocityO)<=.00365993,"current-epoch settled support bars");finalSupported++;}
        }
        Check(current.Completed&&finalSupported==600&&current.Observation.HistoryCount==1200&&current.Observation.StateRevision.Value==1200&&current.Observation.Clock.Debt.Ticks==0,"current-epoch complete canonical accounting");
        Console.WriteLine($"FLORIDA_SOLAR_CURRENT_EPOCH PASS start={start.Ticks} intervals=1200 support={finalSupported}/600 peak={peak:R} maxPositionError={maxError:R}");
    }
}
