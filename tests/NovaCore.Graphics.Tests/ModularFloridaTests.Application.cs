using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void ApplicationRoute()
    {
        checks=0;_=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        void Refused(Action action,string label){try{action();throw new Exception("Accepted "+label);}catch(InvalidDataException){Need(true,label);}}
        foreach(var longer in new[]{false,true})foreach(var clock in new[]{0,90,180,270})
        {
            using var editor=new ConstructionEditorSession(catalog);editor.Load(editor.Revision,Craft(catalog,longer).Save());
            if(clock!=0){editor.PreviewClock(editor.Revision,"engine",clock);editor.AcceptPreview(editor.Revision);}
            editor.ConfigureSelection(editor.Revision,"tank",.75,.5,true,true);
            var source=editor.Current!.Design;var bytes=source.Save();var before=editor.Current;var revision=editor.Revision;var dirty=editor.Dirty;
            var compiled=CraftCompiler.Compile(catalog,source.Data,Assets);var hash=CraftLaunchAdmission.SourceHash(bytes);
            var options=new CraftFlightOptions(Assets,Path.Combine(Assets,"catalog.json"),hash,source.Digest,compiled.Digest,catalog.Digest);
            using var pipe=new MemoryStream(bytes);var craft=options.Load(pipe);
            Need(craft.Digest==compiled.Digest&&craft.Design.Save().SequenceEqual(bytes),"exact immutable process handoff");
            Refused(()=>CraftLaunchAdmission.Prepare(catalog,bytes,Assets,new string('0',64),source.Digest,compiled.Digest,catalog.Digest),"source replacement refused");
            Refused(()=>CraftLaunchAdmission.Prepare(catalog,bytes,Assets,hash,source.Digest,compiled.Digest,new string('0',64)),"catalog replacement refused");
            Refused(()=>CraftLaunchAdmission.Prepare(catalog,bytes,Assets,hash,new string('0',64),compiled.Digest,catalog.Digest),"document replacement refused");
            Refused(()=>CraftLaunchAdmission.Prepare(catalog,bytes,Assets,hash,source.Digest,new string('0',64),catalog.Digest),"compiled replacement refused");
            Need(SolarSystemScene.TryCreateAt(new(1),new(999000),out var solar,out _),"application solar");
            using var scene=new ConstructionFlightScene(craft,Assets,solar!);
            var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);
            scene.FloridaView.PrepareCamera(camera);
            var focus=scene.PrepareFocusObservation();Need(solar!.BindActiveVessel(focus)&&solar.RefocusActiveVessel(camera),"live generic focus");
            Need(scene.State.Fuel.Save().SequenceEqual(craft.Fuel.Initial().Save())&&scene.State.Power.Save().SequenceEqual(craft.Power.Initial().Save()),"partial resources preserved");
            Need(!scene.Actuation.Main&&scene.PlayerInput.Identity.Vessel.Value==focus.CanonicalId&&(ulong)scene.PlayerInput.Identity.Generation==focus.Generation,"one cold control/camera identity");
            var initial=scene.State;scene.Advance(new(1000000));
            Need(scene.State.Epoch.Ticks-initial.Epoch.Ticks==62500&&scene.Session.Clock.PendingSimulationDebt.Ticks==937500,"bounded physical debt retained");
            void Present()
            {
                Need(solar.TryPresentPhysicalEpoch(scene.State.Epoch,camera,out _),"solar consumes accepted physical epoch");
                Need(solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation())&&solar.PresentationTicks==scene.State.Epoch.Ticks,"follow uses same physical endpoint");
            }
            Present();Need(solar.CurrentTime.Ticks==initial.Epoch.Ticks+62500,"presentation does not consume outstanding host debt");
            while(scene.Session.Clock.PendingSimulationDebt.Ticks>=15625){scene.Advance(default);Present();}
            var filtered=ConstructionFlightScene.CameraInput(new(){MoveUp=1,MoveLeft=1,MoveForward=1,RateIncrease=1,RateDecrease=1,PauseToggle=1,CameraActions=NativeCameraActions.FocusActiveVessel});
            solar.ApplyPresentationInput(camera,filtered,out var rateChanged,out var paused,deferVesselPose:true);
            Need(!rateChanged&&!paused&&filtered.MoveUp==0&&filtered.MoveLeft==0&&filtered.MoveForward==0&&filtered.CameraActions==NativeCameraActions.FocusActiveVessel,"no flight warp or camera/control alias");
            scene.ApplyPlayerInput(new(){ControlInputActive=1,EngineActions=NativeEngineActions.On});
            for(var i=0;i<384;i++){scene.Advance(new(15625));Present();}
            Need(!scene.Failed&&scene.State.Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight&&scene.State.Physical.Motion.PositionO.Y>1&&scene.Actuation.Main,"same application ascent");
            var physical=scene.State;var identity=scene.PlayerInput.Identity;
            foreach(var body in new[]{NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Earth})
            {
                Need(solar.Focus(camera,body),"celestial focus during burn");scene.Advance(new(15625));
                // Exact application order: camera intent observes the prior
                // display, then accepted physical time and focus refresh land.
                solar.ApplyPresentationInput(camera,ConstructionFlightScene.CameraInput(new(){CameraActions=NativeCameraActions.FocusActiveVessel}),out _,out _,deferVesselPose:true);
                Need(solar.TryPresentPhysicalEpoch(scene.State.Epoch,camera,out _),"F callback epoch publication");
                solar.Focus(camera,NativePresentationFocus.None);
                Need(solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation()),"F callback refreshed endpoint");solar.EnforceFinalCameraInvariant(camera);
                Need(solar.CurrentFocusTarget.Kind==FocusTargetKind.SceneObject&&Math.Abs(Norm(camera.Position.Value-scene.PrepareFocusObservation().MaterialOrigin.Value)-solar.OrbitDistance)<.001&&scene.PlayerInput.Identity==identity,"delivered F spatially refocuses and retains command owner");
            }
            scene.ApplyPlayerInput(new(){ControlInputActive=1,PilotKeys=NativePilotKeys.W|NativePilotKeys.A|NativePilotKeys.Q});
            for(var i=0;i<8;i++)scene.Advance(new(15625));Present();
            Need(!scene.Actuation.Jets.IsEmpty&&scene.State.Physical!.Gimbal!=default,"combined demand realizes generic jets/gimbal at every legal clock");
            PoweredPresentation(scene);
            scene.ApplyPlayerInput(new(){ControlInputActive=0,PilotKeys=NativePilotKeys.W});Need(scene.PlayerInput.Observation.Requested.Pilot==default,"focus loss releases input");
            scene.ApplyPlayerInput(new(){ControlInputActive=1,EngineActions=NativeEngineActions.Off});var cutoff=scene.State;var fuel=cutoff.Fuel.Save();
            for(var i=0;i<192;i++){scene.Advance(new(15625));Present();}
            Need(!scene.Failed&&!scene.Actuation.Main&&scene.State.Fuel.Save().SequenceEqual(fuel)&&scene.State.Physical!.Motion.PositionO!=cutoff.Physical!.Motion.PositionO,"cutoff keeps continuous coast");
            var submission=new RenderFrameSubmission(scene.RenderCapacity);var root=scene.PrepareFocusObservation().MaterialOrigin;
            for(var i=0;i<20;i++)scene.BuildSubmission(default,root,submission);
            var allocated=GC.GetAllocatedBytesForCurrentThread();for(var i=0;i<1000;i++)scene.BuildSubmission(default,root,submission);
            Need(GC.GetAllocatedBytesForCurrentThread()==allocated,"warmed compiled presentation zero allocation");
            Need(submission.ObjectCount==scene.InitialSnapshot.Count,"all compiled meshes and one slab remain after cutoff");
            Need(ReferenceEquals(editor.Current,before)&&editor.Revision==revision&&editor.Dirty==dirty&&editor.Current.Design.Save().SequenceEqual(bytes),"runtime leaves editor source/history baseline untouched");
        }
        // Simulate accepted intervals followed by a private native publication
        // failure before the adapter copies its next frame.
        Need(SolarSystemScene.TryCreateAt(new(1),default,out var failureSolar,out _),"failure solar");
        using(var scene=new ConstructionFlightScene(CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets),Assets,failureSolar!))
        {
            var engine=scene.Session.Engine;var initial=scene.State;
            Need(engine.AdmitConstructionHostTime(scene.Session.Authority,1,new(78125))==ConstructionServiceStatus.AcceptedCredit,"failure credit");
            Need(engine.ServiceConstructionDebt(scene.Session.Authority,out var published)==ConstructionServiceStatus.Published&&published==4,"prior canonical publications");
            Need(engine.ServiceConstructionDebt(scene.Session.Authority,out published,refusePublicationForTest:true)==ConstructionServiceStatus.Invalidated&&published==0,"subsequent private invalidation");
            scene.Fail("adversarial publication refusal");
            Need(scene.State.Epoch.Ticks==initial.Epoch.Ticks+62500&&scene.State.Sequence==initial.Sequence+4&&!scene.Actuation.Main,"failed adapter exposes last actual publication");
            var last=scene.State;scene.Advance(new(15625));scene.ApplyPlayerInput(new(){ControlInputActive=1,EngineActions=NativeEngineActions.On});
            Need(ReferenceEquals(last,scene.State)&&!scene.Actuation.Main,"failed adapter cannot restart");
        }
        Need(CraftFlightOptions.TryParse([],out var absent,out _)&&absent is null,"ordinary options preserved");
        var failureCamera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);
        var beforeTime=failureSolar!.CurrentTime;var beforePresentation=failureSolar.Presentation;
        Need(!failureSolar.TryPresentPhysicalEpoch(new(long.MaxValue),failureCamera,out _)&&failureSolar.CurrentTime==beforeTime&&ReferenceEquals(failureSolar.Presentation,beforePresentation),"unavailable presentation epoch cannot advance clock/publication");
        Need(!CraftFlightOptions.TryParse(["--craft-stdin"],out _,out _)&&!CraftFlightOptions.TryParse(["--craft-unknown=x"],out _,out _),"incomplete/unknown launch refused");
        Console.WriteLine($"MODULAR_GATE12_APPLICATION_PASS checks={checks}");
    }
    private static unsafe void PoweredPresentation(ConstructionFlightScene scene)
    {
        var main=scene.Session.Binding.Physical!.Control.Main;var g=main.Gimbal!;var state=scene.State.Physical!;
        var cy=Math.Cos(state.Gimbal.ActualY);var sy=Math.Sin(state.Gimbal.ActualY);var cz=Math.Cos(state.Gimbal.ActualZ);var sz=Math.Sin(state.Gimbal.ActualZ);
        var rotation=new Matrix3(cz*cy,-sz,cz*sy,sz*cy,cz,sz*sy,-sy,0,cy);
        var pivot=main.PartOrigin+main.PartRotation.Apply(g.Pivot);
        var meshes=Field<(int Part,PartVisualMesh Mesh)[]>(scene,"meshes");
        for(var i=0;i<meshes.Length;i++)if(meshes[i].Mesh.Gimballed)
        {
            var part=scene.PresentedPart(i);var expected=scene.FloridaView.Position(state.Motion.PositionO+state.Motion.BodyToWorld.Rotate(pivot));
            Need(Norm(part.RootPosition.Value-expected.Value)<1e-6,"powered module rotates about authored pivot");
            foreach(var axis in new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ})
                Need(Norm(part.RootOrientation.Rotate(axis)-scene.FloridaView.Rotation.Rotate(state.Motion.BodyToWorld.Rotate(main.PartRotation.Apply(rotation.Apply(axis)))))<1e-12,"independent clocked gimbal basis");
        }
        var root=scene.PrepareFocusObservation().MaterialOrigin;var submission=new RenderFrameSubmission(scene.RenderCapacity);
        scene.BuildSubmission(default,root,submission);var plume=submission.Objects[meshes.Length+1];
        var expectedNozzle=scene.FloridaView.Position(state.Motion.PositionO+state.Motion.BodyToWorld.Rotate(pivot+main.PartRotation.Apply(rotation.Apply(g.NozzleOffset))));
        Need(Norm(plume.Position.Reconstruct()-(expectedNozzle.Value-root.Value))<1e-5,"independent rotated main exhaust origin");
        var q=plume.Transform.Rotation;var actualAxis=new DoubleQuaternion(q.X,q.Y,q.Z,q.W).Rotate(Double3.UnitX);
        var outward=scene.FloridaView.Rotation.Rotate(state.Motion.BodyToWorld.Rotate(main.PartRotation.Apply(rotation.Apply(-Double3.UnitX))));
        Need(Norm(actualAxis-outward)<2e-7,"exhaust opposes realized thrust at each engine clock");
        var native=new NativeRenderObject[scene.RenderCapacity];for(var i=0;i<submission.ObjectCount;i++)native[i].Mesh=new(){Value=submission.Objects[i].Mesh.Value};
        fixed(NativeRenderObject* objects=native)
        {
            for(var i=0;i<20;i++){scene.BuildSubmission(default,root,submission);scene.WriteExhaustParameters(objects,submission.ObjectCount);}
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++){scene.BuildSubmission(default,root,submission);scene.WriteExhaustParameters(objects,submission.ObjectCount);}
            Need(GC.GetAllocatedBytesForCurrentThread()==allocated,"warmed powered mesh and native exhaust submission zero allocation");
            Need(BitConverter.UInt32BitsToSingle(native[meshes.Length+1].Padding1)==3072f&&native[meshes.Length+1].Padding2==0,"main exhaust uses authored effective speed");
            var cursor=meshes.Length+2;
            for(var i=0;i<scene.Craft.Allocation.JetActuators.Length;i++)if(scene.Actuation.Jets.Contains(i))
            {
                var actuator=scene.Craft.Actuators[scene.Craft.Allocation.JetActuators[i]];var consumer=scene.Craft.Design.Parts[actuator.Part].Definition.Construction!.Consumers.Single(c=>c.Id==actuator.Key.Consumer);
                Need(native[cursor].Padding2==(uint)i+1&&BitConverter.UInt32BitsToSingle(native[cursor].Padding1)==(float)(consumer.ThrustN/consumer.TotalFlowKgS),"independent 32-bit jet mask stamps matching actuator speed");cursor++;
            }
            Need(cursor==submission.ObjectCount,"actual exhaust count only");
        }
    }
}
