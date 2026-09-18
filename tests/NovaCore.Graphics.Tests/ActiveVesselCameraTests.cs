using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class ActiveVesselCameraTests
{
    private static void Check(bool pass,string message)
    { if(!pass)throw new InvalidOperationException("Active vessel camera: "+message); }
    private static double Distance(Double3 a,Double3 b)=>Math.Sqrt((a-b).LengthSquared);
    private static CameraState Camera()=>new(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);

    internal static void WarpFrames()
    {
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        foreach(var factor in new[]{1L,120L,14400L,86400L})
        {
            Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var value,out _),"warp Solar");
            var solar=value!;var camera=Camera();
            using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true,solarWorld:solar);
            scene.FloridaView!.PrepareCamera(camera);
            var first=scene.PrepareFocusObservation();var canonical=scene.Observation;
            Check(first.ReferenceFrame.ParentBodyId==6&&first.ReferenceFrame.LocalToParent==scene.FloridaView.Site.LocalToBodyFixed,"publisher owns accepted site frame");
            Check(solar.BindActiveVessel(first)&&solar.RefocusActiveVessel(camera),"warp bind");
            while(solar.Rate.Numerator!=factor||solar.Rate.Denominator!=1)
                solar.ApplyPresentationInput(camera,new(){RateIncrease=1},out _,out _,deferVesselPose:true);
            DoubleQuaternion Basis()=>solar.FocusedBody.BodyFixedToRoot*first.ReferenceFrame.LocalToParent;
            Double3 LocalOffset()=>Basis().Conjugate().Rotate(camera.Position.Value-solar.CurrentFocusRoot);
            Double3 Horizon()=>camera.Orientation.Conjugate().Rotate(Basis().Rotate(Double3.UnitY));
            var beforeOrbit=LocalOffset();
            solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=30,MouseDeltaY=-12,MouseWheelDetents=1},out _,out _,deferVesselPose:true);
            Check(LocalOffset()==beforeOrbit,"deferred input records demand without old-epoch placement");
            Check(solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation()),"realize orbit/zoom");
            Check(Distance(LocalOffset().Normalized(),beforeOrbit.Normalized())>.02&&Math.Abs(solar.OrbitDistance-19.2)<1e-12,"orbit and zoom change local view");
            var offset=LocalOffset();var horizon=Horizon();var initialDistance=Math.Sqrt(offset.LengthSquared);
            var correctionCount=solar.CameraClearanceCorrectionCount;
            var render=new RenderFrameSubmission(scene.RenderCapacity);var maximumOffset=0d;var maximumHorizon=0d;var maximumDistance=0d;
            for(var frame=0;frame<600;frame++)
            {
                var poseRevision=solar.ActiveVesselPoseRevision;var priorEye=camera.Position;var priorQ=camera.Orientation;
                // Same production order; F, reserved HOME bit and pause are covered.
                solar.ApplyPresentationInput(camera,new(){DeltaSeconds=1f/60,CameraActions=frame%100==0?NativeCameraActions.FocusActiveVessel|(NativeCameraActions)2:0,PauseToggle=frame is 200 or 220?1u:0u},out _,out _,deferVesselPose:true);
                Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _),"warp advances");
                Check(camera.Position==priorEye&&camera.Orientation==priorQ&&solar.ActiveVesselPoseRevision==poseRevision,"input and simulated progression do not reconstruct stale view");
                var copied=scene.PrepareFocusObservation();
                Check(copied.DisplayTicks==solar.CurrentTime.Ticks&&copied.ObservationTicks==canonical.State.Epoch.Ticks,"held physical state embedded at current display epoch");
                Check(solar.RefreshActiveVessel(camera,copied),"fresh frame and target");
                var refreshedEye=camera.Position;var refreshedQ=camera.Orientation;
                SolarSystemScene.ValidateFinalCameraClearance(6,solar.EnforceFinalCameraInvariant(camera));solar.Update(camera);
                scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
                Check(solar.ActiveVesselPoseRevision==poseRevision+1&&camera.Position==refreshedEye&&camera.Orientation==refreshedQ,"exactly one final vessel reconstruction per display, including F and pause");
                Check(solar.CurrentFocusTarget==FocusTarget.SceneObject(201)&&solar.EnvironmentalBodyId==6&&scene.PresentedPart(6).RootPosition.Value==solar.CurrentFocusRoot,"focus/render share copied current O");
                maximumOffset=Math.Max(maximumOffset,Distance(offset,LocalOffset()));
                maximumHorizon=Math.Max(maximumHorizon,Distance(horizon,Horizon()));
                maximumDistance=Math.Max(maximumDistance,Math.Abs(Math.Sqrt(LocalOffset().LengthSquared)-initialDistance));
                Check(Double3.Dot((solar.CurrentFocusRoot-camera.Position.Value).Normalized(),camera.Orientation.Rotate(new(0,0,-1)))>1-1e-12,"warp aim");
            }
            Check(maximumOffset<.0001&&maximumHorizon<5e-6&&maximumDistance<.0001,"body-fixed offset, azimuth/elevation, horizon and distance stable to FP64 presentation precision");
            Check(solar.CameraClearanceCorrectionCount==correctionCount,"terrain clamp does not mask frame invariants");
            var retained=LocalOffset();var retainedHorizon=Horizon();
            foreach(var body in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Mars})
            {
                Check(solar.Focus(camera,body),"warp celestial navigation");
                Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _)&&solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation()),"update while celestial-focused");
                Check(solar.RefocusActiveVessel(camera)&&Distance(LocalOffset(),retained)<.0001&&Distance(Horizon(),retainedHorizon)<5e-6,"F restores transported local view after elapsed celestial time");
            }
            Check(scene.Observation==canonical,"warp and camera never mutate canonical held endpoint");
            void Display()
            {
                solar.ApplyPresentationInput(camera,new(){DeltaSeconds=1f/60},out _,out _,deferVesselPose:true);
                solar.TryAdvanceByHostDuration(new(16667),camera,out _);
                solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation());
                solar.EnforceFinalCameraInvariant(camera);solar.Update(camera);
                scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
            }
            // Existing Solar publication allocates immutable snapshots. Its full
            // display allocation remains reported below. The exact-zero camera
            // contract covers input + copied focus + reconstruction + submission
            // against a warmed coherent published epoch, as in the existing gate.
            void CameraDisplay()
            {
                solar.ApplyPresentationInput(camera,new(){DeltaSeconds=1f/60},out _,out _,deferVesselPose:true);
                solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation());
                solar.EnforceFinalCameraInvariant(camera);solar.Update(camera);
                scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
            }
            for(var i=0;i<512;i++)CameraDisplay();
            using(var measurement=new OrdinaryAllocationMeasurement("warp-camera-display"))
            {
                for(var i=0;i<512;i++)CameraDisplay();
                OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"warp-camera-display");
            }
            Measure("camera-at-warp-"+factor,CameraDisplay);
            Measure("supported-warp-"+factor,Display);
            Console.WriteLine("ACTIVE_VESSEL_WARP "+JsonSerializer.Serialize(new{factor,frames=600,maximumOffset,maximumHorizon,maximumDistance,terrainCorrections=solar.CameraClearanceCorrectionCount-correctionCount,canonicalUnchanged=scene.Observation==canonical}));
        }
        FrameTransitions();
    }

    private static void FrameTransitions()
    {
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var value,out _),"frame transition Solar");
        var solar=value!;var camera=Camera();
        solar.Focus(camera,NativePresentationFocus.Mars);
        var parent=solar.FocusedBody.BodyId;
        var localBasis=DoubleQuaternion.FromAxisAngle(new Double3(1,2,3),.73);
        // Generic non-Florida/non-Earth parent and distinct environment. Far
        // exterior translation removes terrain clipping as a possible oracle.
        var root=new Double3(3e8,-2e8,4e8);
        var observation=new SceneObjectFocusObservation(909,44,6,new(root,new(1)),1,0,solar.CurrentTime.Ticks,SceneObjectFocusStatus.Held)
        {ReferenceFrame=new(parent,localBasis)};
        Check(solar.BindActiveVessel(observation)&&solar.RefocusActiveVessel(camera),"non-Earth parent distinct from Earth environment");
        solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=17,MouseDeltaY=-9},out _,out _);
        DoubleQuaternion ParentRotation()
        {
            for(var i=0;i<solar.Presentation.Count;i++)if(solar.Presentation.Bodies[i].BodyId==parent)return solar.Presentation.Bodies[i].BodyFixedToRoot;
            throw new Exception("missing test parent");
        }
        var localQ=(ParentRotation()*localBasis).Conjugate()*camera.Orientation;
        while(solar.Rate.Numerator!=86400)solar.ApplyPresentationInput(camera,new(){RateIncrease=1},out _,out _,deferVesselPose:true);
        Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _),"new transition epoch");
        var expectedQ=(ParentRotation()*localBasis)*localQ;
        var expectedEye=root+expectedQ.Rotate(Double3.UnitZ)*solar.OrbitDistance;
        observation=observation with{DisplayTicks=solar.CurrentTime.Ticks,ReferenceFrame=SceneObjectFocusReferenceFrame.Root};
        Check(solar.RefreshActiveVessel(camera,observation),"body to root rebase");
        Check(Distance(camera.Position.Value,expectedEye)<.0001&&Distance(camera.Orientation.Rotate(Double3.UnitY),expectedQ.Rotate(Double3.UnitY))<1e-5,$"same-new-epoch rebase preserves eye and roll eye={Distance(camera.Position.Value,expectedEye):R} up={Distance(camera.Orientation.Rotate(Double3.UnitY),expectedQ.Rotate(Double3.UnitY)):R}");
        var inertialQ=camera.Orientation;var inertialEye=camera.Position;
        solar.Focus(camera,NativePresentationFocus.Moon);
        Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _),"advance off-vessel");
        observation=observation with{DisplayTicks=solar.CurrentTime.Ticks,ReferenceFrame=new(parent,DoubleQuaternion.FromAxisAngle(Double3.UnitZ,2.3))};
        Check(solar.RefreshActiveVessel(camera,observation)&&solar.RefocusActiveVessel(camera),"retained view changes frame off-vessel");
        Check(Distance(camera.Position.Value,inertialEye.Value)<.0001&&Distance(camera.Orientation.Rotate(Double3.UnitY),inertialQ.Rotate(Double3.UnitY))<1e-5,"off-vessel rebase retains full pose");
        // A large frame rotation followed by orbit must retain a finite aimed view.
        solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=13,MouseDeltaY=7,MouseWheelDetents=1},out _,out _);
        Check(camera.Orientation.IsFinite&&Double3.Dot((root-camera.Position.Value).Normalized(),camera.Orientation.Rotate(new(0,0,-1)))>1-1e-12,"orbit/zoom after arbitrary rebase");
        foreach(var invalid in new[]{observation with{DisplayTicks=observation.DisplayTicks-1},observation with{ReferenceFrame=new(99999,DoubleQuaternion.Identity)},observation with{ReferenceFrame=new(parent,default)},observation with{ReferenceFrame=new(parent,new(double.NaN,0,0,1))},observation with{ReferenceFrame=new(parent,new(0,0,0,2))},observation with{EnvironmentalBodyId=99999},observation with{MaterialOrigin=new(root,new(999))}})
        {
            Check(!solar.RefreshActiveVessel(camera,invalid)&&solar.CurrentFocusTarget.Kind!=FocusTargetKind.SceneObject,"invalid frame/epoch/root fails closed");
            Check(solar.BindActiveVessel(observation)&&solar.RefocusActiveVessel(camera),"explicit recovery binding");
        }
        Console.WriteLine("ACTIVE_VESSEL_FRAME_TRANSITIONS PASS nonEarthParent=true separateEnvironment=true sameNewEpoch=true fullRoll=true retainedOffVessel=true invalidRefusal=true");
    }

    internal static void StaticFlorida()
    {
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var value,out _),"Solar creation");
        var solar=value!;
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true,solarWorld:solar);
        var camera=Camera();scene.FloridaView!.PrepareCamera(camera);
        var before=scene.Observation;var saved=JsonSerializer.SerializeToUtf8Bytes(before);
        var target=scene.PrepareFocusObservation();
        Check(target.CanonicalId==201&&target.Generation!=0&&target.Status==SceneObjectFocusStatus.Prepared,"canonical identity and publisher lifetime");
        Check(solar.BindActiveVessel(target)&&solar.RefocusActiveVessel(camera),"bind and focus");
        Check(!solar.BindActiveVessel(target with{CanonicalId=202})&&solar.ActiveVesselId==201,"live binding cannot silently replace viewed canonical identity");
        Check(solar.CurrentFocusTarget==FocusTarget.SceneObject(201)&&solar.ActiveVesselId==201&&
            solar.EnvironmentalBodyId==6&&solar.ProductionEarthFocused,"view target and Earth environment are distinct");
        Check(solar.CurrentFocusRoot==target.MaterialOrigin.Value,"copied material origin is focus");
        var original=camera.Position.Value;
        solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=30,MouseDeltaY=-15},out _,out _);
        Check(camera.Position.Value!=original&&solar.CurrentFocusRoot==target.MaterialOrigin.Value,"orbit moves camera around craft");
        var beforeZoom=solar.OrbitDistance;
        solar.ApplyPresentationInput(camera,new(){MouseWheelDetents=2},out _,out _);
        Check(Math.Abs(solar.OrbitDistance-beforeZoom/1.5625)<1e-12,"shared multiplicative target zoom");
        var orbit=(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians);
        foreach(var body in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Mars})
        {
            Check(solar.Focus(camera,body),"celestial focus");
            Check(solar.CurrentFocusTarget.Kind!=FocusTargetKind.SceneObject&&solar.ActiveVesselId==201,"view switch preserves active vessel");
            solar.ApplyPresentationInput(camera,new(){MouseWheelDetents=1},out _,out _);
            solar.ApplyPresentationInput(camera,new(){CameraActions=NativeCameraActions.FocusActiveVessel},out _,out _);
            Check(solar.CurrentFocusTarget.SceneObjectId==201&&solar.EnvironmentalBodyId==6&&
                orbit==(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians),"refocus restores bounded view memory and Earth");
        }
        // FREE is deferred. Unknown/reserved action bits cannot detach the view.
        // Exercise the complete display ordering, including the final terrain
        // invariant that the failed HOME acceptance test previously omitted.
        var focusedEye=camera.Position.Value;
        solar.ApplyPresentationInput(camera,new(){CameraActions=(NativeCameraActions)2},out _,out _);
        Check(camera.Position.Value==focusedEye&&solar.CurrentFocusTarget==FocusTarget.SceneObject(201),"reserved action preserves focused pose");
        var maximumSiteDistance=0d;
        for(var i=0;i<600;i++)
        {
            solar.ApplyPresentationInput(camera,new(){CameraActions=(NativeCameraActions)2,MoveUp=1,MoveRight=1},out _,out _);
            Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _),"display clock advances");
            Check(solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation()),"refresh at new display epoch");
            SolarSystemScene.ValidateFinalCameraClearance(6,solar.EnforceFinalCameraInvariant(camera));
            solar.Update(camera);
            maximumSiteDistance=Math.Max(maximumSiteDistance,Distance(camera.Position.Value,solar.CurrentFocusRoot));
            Check(solar.CurrentFocusTarget==FocusTarget.SceneObject(201)&&solar.ActiveVesselId==201&&solar.EnvironmentalBodyId==6,"view, active vessel and Earth context survive display callback");
        }
        Check(maximumSiteDistance<orbit.OrbitDistance+.001&&Distance(camera.Position.Value,focusedEye)>300_000,"vessel view carries Earth motion without local escape");
        Check(solar.Focus(camera,NativePresentationFocus.Moon),"leave vessel before combined action");
        solar.ApplyPresentationInput(camera,new(){CameraActions=NativeCameraActions.FocusActiveVessel|(NativeCameraActions)2},out _,out _);
        Check(solar.CurrentFocusTarget==FocusTarget.SceneObject(201)&&solar.EnvironmentalBodyId==6&&
            orbit==(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians),"reserved bits cannot suppress F refocus");
        // Try lower-hemisphere directions; final terrain exclusion must retain aim.
        for(var i=0;i<24;i++)
        {
            solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=120,MouseDeltaY=40},out _,out _);
            var toward=(solar.CurrentFocusRoot-camera.Position.Value).Normalized();
            Check(Double3.Dot(toward,camera.Orientation.Rotate(new(0,0,-1)))>1-1e-10,"terrain-constrained orbit still aims at O");
            SolarSystemScene.ValidateFinalCameraClearance(6,solar.EnforceFinalCameraInvariant(camera));
        }
        var submission=new RenderFrameSubmission(scene.RenderCapacity);
        scene.BuildSubmission(default,new(camera.Position.Value,new(1)),submission);
        Check(scene.PresentedPart(6).RootPosition.Value==solar.CurrentFocusRoot,"camera and material-O mesh share display mapping");
        Check(scene.Observation==before&&JsonSerializer.SerializeToUtf8Bytes(scene.Observation).AsSpan().SequenceEqual(saved),"all view actions preserve full canonical observation bytes");
        Check(solar.ActiveVesselObservation.DisplayTicks==solar.CurrentTime.Ticks&&
            solar.ActiveVesselObservation.ObservationTicks==before.State.Epoch.Ticks,"physical and display epochs explicitly differ");
        var live=scene.PrepareFocusObservation();
        Check(!solar.RefreshActiveVessel(camera,live with{Generation=live.Generation+1})&&
            solar.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&solar.EnvironmentalBodyId==6,"stale lifetime falls back to environmental body");
        Check(!solar.RefreshActiveVessel(camera,live)&&!solar.RefocusActiveVessel(camera),"retired binding cannot resurrect");
        Check(solar.BindActiveVessel(live)&&solar.RefocusActiveVessel(camera),"explicit rebind at lifetime boundary");
        scene.Dispose();
        Check(!solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation())&&camera.Position.Value.IsFinite,"disposed scene retires safely");
        Console.WriteLine($"ACTIVE_VESSEL_STATIC PASS identity=canonical lifetime=explicit orbit=PASS zoom=PASS free=DEFERRED home=unbound celestial=Earth,Moon,Sun,Mars refocus=F environment=Earth materialOrigin=O nonmutation=canonical-observation-bytes retirement=PASS displayFrames=600 maximumSiteDistance={maximumSiteDistance:R}");
    }

    internal static void MovingAndPrecision()
    {
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var value,out _),"moving Solar");
        var solar=value!;var camera=Camera();
        solar.Focus(camera,NativePresentationFocus.Earth);
        using var scene=new StockAssemblyDevelopmentScene();
        // Explicit test-only rigid translation of an existing qualified freeflight
        // publication. No new scenario or physical trajectory is introduced.
        var displayOrigin=solar.FocusedBody.Position.Value+new Double3(0,solar.FocusedBody.RadiusMetres+100_000,0);
        SceneObjectFocusObservation Snapshot()=>scene.PrepareFocusObservation() with
        {EnvironmentalBodyId=6,MaterialOrigin=new(displayOrigin+scene.Observation.State.Motion.PositionO,new(1)),DisplayTicks=solar.CurrentTime.Ticks};
        Check(solar.BindActiveVessel(Snapshot())&&solar.RefocusActiveVessel(camera),"moving canonical binding");
        solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=15,MouseDeltaY=10,MouseWheelDetents=2},out _,out _);
        var view=(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians);
        var previousTarget=solar.CurrentFocusRoot;var previousCamera=camera.Position.Value;
        var firstCom=scene.Observation.State.Mass.Com;var comChanged=false;var maxFollowError=0d;
        scene.Start();
        for(var i=0;i<128;i++)
        {
            scene.Advance(new(15625));var canonical=scene.Observation;
            Check(solar.RefreshActiveVessel(camera,Snapshot()),"moving published endpoint");
            maxFollowError=Math.Max(maxFollowError,Distance(camera.Position.Value-previousCamera,solar.CurrentFocusRoot-previousTarget));
            Check(view==(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians),"translation preserves user orbit demand");
            Check(scene.Observation==canonical,"camera leaves actual moving publication unchanged");
            comChanged|=canonical.State.Mass.Com!=firstCom;
            previousTarget=solar.CurrentFocusRoot;previousCamera=camera.Position.Value;
        }
        Check(scene.Completed&&comChanged&&maxFollowError<.0001,"real episode follows material O while COM changes");
        var saved=scene.Save();var held=scene.Observation;
        foreach(var distance in new[]{100d,1_000d,100_000d})
        {
            displayOrigin+=new Double3(distance,0,0);
            var beforeTarget=solar.CurrentFocusRoot;var beforeCamera=camera.Position.Value;
            Check(solar.RefreshActiveVessel(camera,Snapshot()),"bounded translated copy");
            Check(Distance(camera.Position.Value-beforeCamera,solar.CurrentFocusRoot-beforeTarget)<.0001,"100m/1km/100km camera translation");
            Check(view==(solar.OrbitDistance,solar.OrbitYawRadians,solar.OrbitPitchRadians),"translated orbit retained");
        }
        solar.ApplyPresentationInput(camera,new(){PauseToggle=1},out _,out _);
        var frozenTime=solar.CurrentTime;displayOrigin+=new Double3(100,0,0);
        Check(solar.TryAdvanceByHostDuration(new(16667),camera,out _)&&solar.CurrentTime==frozenTime&&
            solar.RefreshActiveVessel(camera,Snapshot())&&solar.CurrentFocusRoot==Snapshot().MaterialOrigin.Value,"paused Solar still follows copied updates");
        var maximumError=0d;
        foreach(var root in new[]{new Double3(1e11,-2e11,3e11),new Double3(4e12,-3e12,7e12)})
        {
            displayOrigin=root;
            Check(solar.RefreshActiveVessel(camera,Snapshot()),"large FP64 coordinates");
            var target=solar.CurrentFocusRoot;
            Check(CameraRelativeRenderPosition.TryCreate(new UniversePosition(target,new(1)),new UniversePosition(camera.Position.Value,new(1)),out var relative),"same-root relative transport");
            var reconstructed=relative.Encode().Reconstruct();
            maximumError=Math.Max(maximumError,Distance(reconstructed,target-camera.Position.Value));
            var viewPoint=camera.Orientation.Conjugate().Rotate(relative.Value);
            Check(Math.Abs(viewPoint.X)<.002&&Math.Abs(viewPoint.Y)<.002&&viewPoint.Z<0,"large-coordinate centered focus without float collapse");
            Check(camera.Projection.NearClip<solar.OrbitDistance*.02&&solar.EnvironmentalBodyId==6,"close craft clip independent of environmental altitude");
            solar.ApplyPresentationInput(camera,new(){MouseWheelDetents=1},out _,out _);
            solar.Focus(camera,NativePresentationFocus.Moon);Check(solar.RefocusActiveVessel(camera),"large-coordinate celestial/refocus");
        }
        Check(maximumError<1e-6&&scene.Observation==held&&scene.Save().AsSpan().SequenceEqual(saved),"precision and saved physical nonmutation");
        var latest=Snapshot();
        Check(!solar.RefreshActiveVessel(camera,latest with{DisplayTicks=latest.DisplayTicks+1})&&camera.Position.Value.IsFinite,"mismatched epoch falls back");
        Check(solar.BindActiveVessel(latest)&&solar.RefocusActiveVessel(camera),"explicit rebind after failed publication");
        Check(!solar.RefreshActiveVessel(camera,latest with{MaterialOrigin=new(latest.MaterialOrigin.Value,new(999))}),"cross-root refusal");
        Console.WriteLine("ACTIVE_VESSEL_MOVING "+JsonSerializer.Serialize(new{pass=true,physicalIntervals=128,comChanged,maxFollowError,translations=new[]{100,1000,100000},maximumRelativeEncodingError=maximumError,solarPaused=true,held=true,saveUnchanged=true,largestRoot=7e12}));
    }

    internal static void AllocationAndCosts()
    {
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var value,out _),"allocation Solar");
        var solar=value!;var camera=Camera();
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true,solarWorld:solar);
        scene.FloridaView!.PrepareCamera(camera);
        Check(solar.BindActiveVessel(scene.PrepareFocusObservation())&&solar.RefocusActiveVessel(camera),"allocation bind");
        var before=scene.Observation;
        var render=new RenderFrameSubmission(scene.RenderCapacity);
        var sequence=0;
        void Follow()
        {
            var sign=(sequence++&1)==0?1:-1;
            solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=sign*.1f,MouseDeltaY=sign*.05f,MouseWheelDetents=sign},out _,out _);
            solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation());
            solar.EnforceFinalCameraInvariant(camera);solar.Update(camera);
            scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
        }
        void Refocus()
        { solar.ApplyPresentationInput(camera,new(){CameraActions=NativeCameraActions.FocusActiveVessel},out _,out _); }
        void Switch()
        { solar.Focus(camera,NativePresentationFocus.Moon);Check(solar.RefocusActiveVessel(camera),"warm celestial refocus"); }
        static void Zero(string name,Action action)
        {
            for(var i=0;i<512;i++)action();
            using var measurement=new OrdinaryAllocationMeasurement(name);
            for(var i=0;i<2048;i++)action();
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),name);
        }
        Zero("vessel-follow-orbit-zoom-display",Follow);
        Zero("vessel-F-refocus",Refocus);
        OrdinaryAllocationMeasurement.PositiveControl();
        Measure("florida-vessel-follow-orbit-zoom",Follow);
        Measure("F-refocus",Refocus);
        Measure("celestial-vessel-switch",Switch);
        solar.Focus(camera,NativePresentationFocus.Moon);
        Measure("solar-celestial-display",()=>{solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=(sequence++&1)==0?.1f:-.1f},out _,out _);solar.Update(camera);});
        Check(scene.Observation==before,"all allocation/performance populations are observational");
        // Fixed value fields and one active binding: repeated actions have no target registry/history.
        var generation=solar.ActiveVesselGeneration;
        var retainedBefore=GC.GetTotalMemory(true);
        for(var i=0;i<10_000;i++)Refocus();
        var retainedAfter=GC.GetTotalMemory(true);
        Check(solar.ActiveVesselGeneration==generation&&solar.ActiveVesselId==201,"switching never registers a target");
        Console.WriteLine("ACTIVE_VESSEL_STORAGE "+JsonSerializer.Serialize(new{targetSlots=1,historySlots=0,observationBytes=Unsafe.SizeOf<SceneObjectFocusObservation>(),retainedBefore,retainedAfter,refocusOperations=10000,bindingUnchanged=true}));
    }

    private static void Measure(string route,Action action)
    {
        const int warm=512,count=2048;
        var samples=new double[count];for(var i=0;i<warm;i++)action();
        var gc0=GC.CollectionCount(0);var gc1=GC.CollectionCount(1);var gc2=GC.CollectionCount(2);
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<count;i++){var start=Stopwatch.GetTimestamp();action();samples[i]=(Stopwatch.GetTimestamp()-start)*1000d/Stopwatch.Frequency;}
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var collections=new[]{GC.CollectionCount(0)-gc0,GC.CollectionCount(1)-gc1,GC.CollectionCount(2)-gc2};
        var tails=samples.Select((ms,index)=>new{ms,index}).OrderByDescending(x=>x.ms).Take(8).ToArray();
        Array.Sort(samples);double Percent(double q)=>samples[(int)Math.Ceiling(q*count)-1];
        Console.WriteLine("ACTIVE_VESSEL_COST "+JsonSerializer.Serialize(new{route,warm,count,median=Percent(.5),p95=Percent(.95),p99=Percent(.99),maximum=Percent(1),collections,allocated,tails,scope="managed only; supported-warp includes Solar publication; camera-at-warp uses a coherent held publication; no GPU or physical ticks"}));
    }
}
