using System.Diagnostics;using System.Runtime.InteropServices;
using NovaCore.Core;using NovaCore.Core.Camera;using NovaCore.Core.ReferenceFrames;using NovaCore.Graphics;using NovaCore.Interop;using NovaCore.Platform;using NovaCore.Simulation.Time;
if(!SampleOptions.TryParse(args,out var options,out var error)){Console.Error.WriteLine(error);return 2;}if(!LogOptions.TryParse(options.LogArguments,out var log,out var logError)){Console.Error.WriteLine(logError);return 2;}return Run(options,log);
static unsafe int Run(SampleOptions options,LogOptions log)
{
    var root=new ReferenceFrameId(1); var defaultStart=new UniversePosition(new Double3(4e12,-3e12,7e12),root);
    ResolvedRenderSnapshot? snapshot=null; FixtureSceneDiagnostics fixture=default; DynamicReferenceFrameFixtureScene? dynamic=null; CelestialAnalyticalScene? celestial=null; EarthPlanetaryScene? earth=null; var cameraPosition=defaultStart.Value; var projection=new CameraProjection(Math.PI/3,16d/9,.01,1000); var movementSpeed=.1d;
    if(options.Scene=="earth")
    {
        if(!EarthPlanetaryScene.TryCreate(root,out earth,out var earthError)||earth is null){Console.Error.WriteLine(earthError);return 1;}
        var source=new[]{new SampleRenderableState(new UniversePosition(Double3.Zero,root),DoubleQuaternion.Identity,Double3.Zero,MeshHandle.Triangle)};
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Earth renderer snapshot failed: {status}");return 1;}
        projection=earth.Projection;movementSpeed=earth.Earth.RadiusMetres*.01d;PrintEarthDiagnostics(earth);
    }
    else if(options.Scene=="planetary-diagnostic")
    {
        var source=new[]{new SampleRenderableState(new UniversePosition(Double3.Zero,root),DoubleQuaternion.Identity,Double3.Zero,MeshHandle.Triangle)};
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Planetary diagnostic snapshot failed: {status}");return 1;}
        cameraPosition=new Double3(0,0,4);movementSpeed=.5d;Console.WriteLine("Scene: planetary-diagnostic; six level-0 cube-sphere patches; radius=1; face debug colors.");
    }
    else if(options.Scene=="fixture")
    {
        if(!StaticReferenceFrameFixtureSceneFactory.TryCreate(out var fixtureScene,out var fixtureError)){Console.Error.WriteLine(fixtureError);return 1;}
        snapshot=fixtureScene.Snapshot; fixture=fixtureScene.Diagnostics; var fixtureCamera=StaticReferenceFrameFixtureSceneFactory.Camera; cameraPosition=fixtureCamera.Position; projection=fixtureCamera.Projection; movementSpeed=fixtureCamera.MovementSpeed;
        PrintFixtureDiagnostics(fixture,log);
    }
    else if(options.Scene=="fixture-dynamic")
    {
        if(!DynamicReferenceFrameFixtureScene.TryCreate(out dynamic,out var dynamicDiagnostics,out var dynamicError)||dynamic is null){Console.Error.WriteLine(dynamicError);return 1;}
        snapshot=dynamic.CurrentSnapshot; fixture=dynamicDiagnostics.InitialFixture; var fixtureCamera=DynamicReferenceFrameFixtureScene.Camera; cameraPosition=fixtureCamera.Position; projection=fixtureCamera.Projection; movementSpeed=fixtureCamera.MovementSpeed;
        PrintDynamicFixtureDiagnostics(dynamicDiagnostics);
    }
    else if(options.Scene=="celestial")
    {
        if(!CelestialAnalyticalScene.TryCreate(out celestial,out var celestialError)||celestial is null){Console.Error.WriteLine(celestialError);return 1;}
        snapshot=celestial.CurrentSnapshot; var celestialCamera=CelestialAnalyticalScene.Camera; cameraPosition=celestialCamera.Position; projection=celestialCamera.Projection; movementSpeed=celestialCamera.MovementSpeed;
        PrintCelestialDiagnostics();
    }
    else
    {
        var source=options.Scene=="frames"?ReferenceFrameDemoSceneFactory.Create(defaultStart):SampleSceneFactory.Create(options.ObjectCount,defaultStart);
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Scene snapshot failed: {status}");return 1;}
    }
    var frames=new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root,null,ReferenceFrameKind.Ecl,"ECL"),CelestialFrameFactory.RootEcl())]);
    var planetaryPatches=earth?.Patches??(options.Scene=="planetary-diagnostic"?CreateDiagnosticPatches():[]);
    var camera=new CameraState(new FramePosition(root,cameraPosition),DoubleQuaternion.Identity,projection,CameraMode.Free);
    if(earth is not null&&!earth.TryFocus(camera)){Console.Error.WriteLine("Earth camera focus failed.");return 1;}
    var state=new HostState(camera,frames,new ReferenceFrameResolver(frames),snapshot!,new RenderFrameSubmission(snapshot!.Count,snapshot.OrbitCurve?.Count??0),log,movementSpeed,dynamic,celestial,earth,planetaryPatches);
    UpdatePlanetaryPatches(state);
    if(!TryBuild(state,out var buildError)){Console.Error.WriteLine(buildError);return 1;}
    var objects=new NativeRenderObject[snapshot.Count];var batches=new NativeDrawBatch[snapshot.Count];var orbit=new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var previousOrbit=new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var bodyForward=new NativeOrbitLineVertex[2];var targetDirection=new NativeOrbitLineVertex[2];var h=GCHandle.Alloc(state);
    CopyObjects(state.Submission,objects,batches,orbit,previousOrbit,bodyForward,targetDirection,out var batchCount);
    fixed(NativeRenderObject* po=objects)fixed(NativeDrawBatch* pb=batches)fixed(NativeOrbitLineVertex* pl=orbit)fixed(NativeOrbitLineVertex* pp=previousOrbit)fixed(NativeOrbitLineVertex* pf=bodyForward)fixed(NativeOrbitLineVertex* pt=targetDirection)fixed(NativePlanetaryPatch* px=planetaryPatches){NativeFrameSubmission n=new(){Camera=NativeCamera(state.Submission.Camera),Objects=po,ObjectCount=(uint)objects.Length,Batches=pb,BatchCount=(uint)batchCount,OrbitVertices=pl,OrbitVertexCount=(uint)state.Submission.OrbitVertexCount,PreviousOrbitVertices=pp,PreviousOrbitVertexCount=(uint)state.Submission.PreviousOrbitVertexCount,BodyForwardVertices=pf,BodyForwardVertexCount=(uint)state.Submission.BodyForwardVertexCount,TargetDirectionVertices=pt,TargetDirectionVertexCount=(uint)state.Submission.TargetDirectionVertexCount,PlanetaryPatches=px,PlanetaryPatchCount=ActivePlanetaryPatchCount(state)};try{return NativeRuntime.RunRenderer(&n,Callback,GCHandle.ToIntPtr(h))==NativeResult.Success?0:1;}finally{h.Free();}}
}
static bool TryCreateSnapshot(SampleRenderableState[] source,out ResolvedRenderSnapshot? snapshot,out ResolvedRenderSnapshotStatus status)
{
    var objects=new ResolvedRenderObject[source.Length]; for(var i=0;i<source.Length;i++){var value=source[i];objects[i]=new(new RenderObjectId((uint)(i+1)),value.Position,value.Rotation,value.Scale,value.Mesh);} return ResolvedRenderSnapshot.TryCreate(objects,out snapshot,out status);
}
static bool TryBuild(HostState s,out string error)
{
    if(!CameraRenderSnapshotBuilder.TryBuild(s.Camera,s.Resolver,s.Frames.RootId,out var camera,out var rootPosition,out var cameraStatus)){error=$"Camera snapshot failed: {cameraStatus}";return false;}
    var status=ResolvedRenderSubmissionBuilder.TryBuild(s.Snapshot,camera,rootPosition,s.Submission); if(status!=ResolvedRenderSubmissionBuildStatus.Success){error=$"Resolved submission failed: {status}";return false;} error=string.Empty;return true;
}
static unsafe void Callback(NativeHostEvent* e,IntPtr data)
{
    var s=(HostState)GCHandle.FromIntPtr(data).Target!;if(e->Type==NativeHostEventType.Diagnostic){var c=(LogCategory)e->LogCategory;if(c is LogCategory.None or LogCategory.Validation||s.Log.IsEnabled(c))Console.WriteLine($"[native] {Marshal.PtrToStringUTF8((IntPtr)e->Utf8Message)}");return;}
    var celestialScene=s.Celestial;var earthScene=s.Earth;var orbitCamera=celestialScene is not null||earthScene is not null;Span<CameraCommand> commands=stackalloc CameraCommand[4];var count=DebugCameraInput.Map(e->Input,commands,!orbitCamera,!orbitCamera);var previousSpeed=s.Controller.MovementSpeed;
    if(s.Dynamic is not null){var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!s.Dynamic.TryAdvanceByHostDuration(duration,out var publicationError)&&!s.DynamicFailureLogged){Console.Error.WriteLine(publicationError);s.DynamicFailureLogged=true;}else s.Snapshot=s.Dynamic.CurrentSnapshot;}
    if(celestialScene is not null){celestialScene.ApplyPresentationInput(s.Camera,e->Input,out var rateChanged,out var pauseChanged);var previousSasMode=celestialScene.SasMode;var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!celestialScene.TryAdvanceByHostDuration(duration,e->Input,out var publicationError)&&!s.CelestialFailureLogged){Console.Error.WriteLine(publicationError);s.CelestialFailureLogged=true;}else s.Snapshot=celestialScene.CurrentSnapshot;if(s.Log.IsEnabled(LogCategory.Camera)&&rateChanged)Console.WriteLine($"Celestial rate={celestialScene.Rate.Numerator}:{celestialScene.Rate.Denominator}");if(s.Log.IsEnabled(LogCategory.Camera)&&pauseChanged)Console.WriteLine($"Celestial paused={celestialScene.IsPaused}");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Celestial zoom distance={celestialScene.OrbitDistance:R}");if(s.Log.IsEnabled(LogCategory.Camera)&&previousSasMode!=celestialScene.SasMode)Console.WriteLine($"Celestial SAS mode={celestialScene.SasMode}");LogSasIndicatorDiagnostics(s,celestialScene);}
    earthScene?.ApplyPresentationInput(s.Camera,e->Input);
    if(e->Input.Reset!=0){s.Camera.Position=s.Default.Position;s.Camera.Orientation=s.Default.Orientation;s.Camera.Projection=s.Default.Projection;s.Camera.Mode=s.Default.Mode;s.Controller.Reset();celestialScene?.ResetPresentationCamera(s.Camera);earthScene?.ResetPresentationCamera(s.Camera);}else if(!orbitCamera)s.Controller.Update(s.Camera,commands[..count],e->Input.DeltaSeconds);
    UpdatePlanetaryPatches(s);LogEarthLodDiagnostics(s,earthScene);
    if(!TryBuild(s,out var buildError)){Console.Error.WriteLine(buildError);PostQuitMessage(1);return;} CopyObjectsToNative(s.Submission,e->Submission->Objects,e->Submission->Batches,e->Submission->OrbitVertices,e->Submission->PreviousOrbitVertices,e->Submission->BodyForwardVertices,e->Submission->TargetDirectionVertices,out var batchCount);e->Submission->ObjectCount=(uint)s.Submission.ObjectCount;e->Submission->BatchCount=(uint)batchCount;e->Submission->OrbitVertexCount=(uint)s.Submission.OrbitVertexCount;e->Submission->PreviousOrbitVertexCount=(uint)s.Submission.PreviousOrbitVertexCount;e->Submission->BodyForwardVertexCount=(uint)s.Submission.BodyForwardVertexCount;e->Submission->TargetDirectionVertexCount=(uint)s.Submission.TargetDirectionVertexCount;e->Submission->PlanetaryPatchCount=ActivePlanetaryPatchCount(s);e->Submission->Camera=NativeCamera(s.Submission.Camera);
    if(!orbitCamera&&s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Camera wheelDetents={e->Input.MouseWheelDetents} speed={previousSpeed:R}->{s.Controller.MovementSpeed:R}");if(earthScene is not null&&s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Earth orbit distance={earthScene.OrbitDistance:R} m");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.Reset!=0)Console.WriteLine($"Camera reset speed={s.Controller.MovementSpeed:R}");
}
static void PrintFixtureDiagnostics(in FixtureSceneDiagnostics fixture,LogOptions log)
{
    Console.WriteLine("Scene: fixture");Console.WriteLine("Static hierarchy: ECL Star -> CCE Planet -> CCI Moon -> CCF TestVessel");Console.WriteLine($"Resolved roots: Star={fixture.Star.Value}; Planet={fixture.Planet.Value}; Moon={fixture.Moon.Value}; TestVessel={fixture.Vessel.Value}");Console.WriteLine("Objects: 4; batches: 1");Console.WriteLine($"Fixture render setup hash: 0x{fixture.SetupHash:X16}");Console.WriteLine("Static reference-frame render fixture; no propagation or orbital simulation.");
    if(log.IsEnabled(LogCategory.Precision))Console.WriteLine($"Fixture root frame: {fixture.RootFrame.Value}");
}
static void PrintDynamicFixtureDiagnostics(in DynamicFixtureDiagnostics diagnostics)
{
    Console.WriteLine("Scene: fixture-dynamic");Console.WriteLine("Prescribed deterministic transform motion; no gravity or orbital simulation.");Console.WriteLine($"Moon: radius={DynamicReferenceFrameFixtureScene.MoonRadius:R}, rate={DynamicReferenceFrameFixtureScene.MoonAngularRate:R} rad/s; Vessel: radius={DynamicReferenceFrameFixtureScene.VesselRadius:R}, rate={DynamicReferenceFrameFixtureScene.VesselAngularRate:R} rad/s");Console.WriteLine("Initial controlled time: 0 ticks");Console.WriteLine($"Initial resolved roots: Star={diagnostics.InitialFixture.Star.Value}; Planet={diagnostics.InitialFixture.Planet.Value}; Moon={diagnostics.InitialFixture.Moon.Value}; TestVessel={diagnostics.InitialFixture.Vessel.Value}");Console.WriteLine($"Dynamic scripted-sequence hash: 0x{diagnostics.ScriptedSequenceHash:X16}");Console.WriteLine("Dynamic publication allocations are measured by NovaCore.Graphics.Tests; warmed frame assembly remains allocation-free.");
}
static void PrintCelestialDiagnostics()
{
    Console.WriteLine("Scene: celestial");Console.WriteLine("Clock-driven analytical two-body marker fixture; no prescribed orbital phase.");Console.WriteLine($"Presentation: 1 display unit = {CelestialAnalyticalScene.MetresPerDisplayUnit:R} m; clock rate={CelestialAnalyticalScene.SampleRate}:1.");Console.WriteLine("Scheduled inertial impulse: +200 m/s tangential at 100000 simulated seconds.");Console.WriteLine("Controls: LMB/RMB drag orbit, wheel zoom, WASD/QE manual torque, 1-7 SAS select, 0 SAS off, ,/. rate, Space pause, R camera reset.");Console.WriteLine("SAS torque control is suspended above 10:1; use , to reduce the rate before automatic pointing.");Console.WriteLine("Objects: central marker + satellite marker; triangle geometry is presentation-only.");
}
static void PrintEarthDiagnostics(EarthPlanetaryScene scene)
{
    Console.WriteLine("Scene: earth; SolAnalytical Earth through PlanetaryPresentationSnapshot.");
    Console.WriteLine($"Earth: body={scene.Earth.BodyId}; radius={scene.Earth.RadiusMetres:R} m; root position=({scene.Earth.Position.Value.X:R}, {scene.Earth.Position.Value.Y:R}, {scene.Earth.Position.Value.Z:R}) m.");
    Console.WriteLine($"LOD: projected-span threshold={EarthPlanetaryScene.LodConfiguration.MaximumProjectedPatchSpan:R}; altitude threshold={EarthPlanetaryScene.LodConfiguration.NearFieldAltitudeRadii:R} radii; maximum level={EarthPlanetaryScene.MaximumLod}; initial six-root coverage.");
    Console.WriteLine("Controls: LMB/RMB drag orbit, wheel zoom, R Earth focus reset.");
}
static void LogSasIndicatorDiagnostics(HostState state,CelestialAnalyticalScene scene)
{
    if(!state.Log.IsEnabled(LogCategory.Camera))return;
    var target=state.Snapshot.TargetDirectionIndicator;var valid=target.HasValue;var now=Stopwatch.GetTimestamp();
    var transitioned=!state.HasSasIndicatorDiagnostic||valid!=state.LastSasTargetValid;var due=valid&&now>=state.NextSasIndicatorDiagnosticTimestamp;
    if(!transitioned&&!due)return;
    state.HasSasIndicatorDiagnostic=true;state.LastSasTargetValid=valid;state.NextSasIndicatorDiagnosticTimestamp=checked(now+Stopwatch.Frequency);
    if(!valid){Console.WriteLine($"Celestial SAS target invalid or hidden ({scene.SasMode}).");return;}
    var body=state.Snapshot.BodyForwardIndicator!.Value;var bodyDirection=(body.End.Value-body.Start.Value).Normalized();var targetDirection=(target!.Value.End.Value-target.Value.Start.Value).Normalized();var angle=Math.Acos(Math.Clamp(Double3.Dot(bodyDirection,targetDirection),-1d,1d));Console.WriteLine($"Celestial SAS target valid; angular error={angle:R} rad.");
}
[DllImport("user32.dll")] static extern void PostQuitMessage(int exitCode);
static unsafe void CopyObjects(RenderFrameSubmission s,NativeRenderObject[] o,NativeDrawBatch[] b,NativeOrbitLineVertex[] l,NativeOrbitLineVertex[] p,NativeOrbitLineVertex[] f,NativeOrbitLineVertex[] t,out int count){fixed(NativeRenderObject* po=o)fixed(NativeDrawBatch* pb=b)fixed(NativeOrbitLineVertex* pl=l)fixed(NativeOrbitLineVertex* pp=p)fixed(NativeOrbitLineVertex* pf=f)fixed(NativeOrbitLineVertex* pt=t)CopyObjectsToNative(s,po,pb,pl,pp,pf,pt,out count);}
static unsafe void CopyObjectsToNative(RenderFrameSubmission s,NativeRenderObject* o,NativeDrawBatch* b,NativeOrbitLineVertex* l,NativeOrbitLineVertex* p,NativeOrbitLineVertex* f,NativeOrbitLineVertex* t,out int count){for(var i=0;i<s.ObjectCount;i++){var x=s.Objects[i];o[i]=new(){Position=NativePosition(x.Position),Transform=new(){RotationX=x.Transform.Rotation.X,RotationY=x.Transform.Rotation.Y,RotationZ=x.Transform.Rotation.Z,RotationW=x.Transform.Rotation.W,ScaleX=x.Transform.Scale.X,ScaleY=x.Transform.Scale.Y,ScaleZ=x.Transform.Scale.Z},Mesh=new(){Value=x.Mesh.Value}};}count=s.BatchCount;for(var i=0;i<count;i++){var x=s.Batches[i];b[i]=new(){Mesh=new(){Value=x.Mesh.Value},FirstObject=x.FirstObject,ObjectCount=x.ObjectCount};}for(var i=0;i<s.OrbitVertexCount;i++){var x=s.OrbitVertices[i];l[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}for(var i=0;i<s.PreviousOrbitVertexCount;i++){var x=s.PreviousOrbitVertices[i];p[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}for(var i=0;i<s.BodyForwardVertexCount;i++){var x=s.BodyForwardVertices[i];f[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}for(var i=0;i<s.TargetDirectionVertexCount;i++){var x=s.TargetDirectionVertices[i];t[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}}
static NativeEncodedPosition NativePosition(EncodedPosition x,float highPadding=0,float lowPadding=0)=>new(){HighX=x.HighX,HighY=x.HighY,HighZ=x.HighZ,HighPadding=highPadding,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ,LowPadding=lowPadding};static NativeCameraData NativeCamera(GpuCameraData c)=>new(){Position=NativePosition(c.Position,c.PositionHighPadding,c.PositionLowPadding),ViewProjection=new(){C0R0=c.ViewProjection.C0R0,C0R1=c.ViewProjection.C0R1,C0R2=c.ViewProjection.C0R2,C0R3=c.ViewProjection.C0R3,C1R0=c.ViewProjection.C1R0,C1R1=c.ViewProjection.C1R1,C1R2=c.ViewProjection.C1R2,C1R3=c.ViewProjection.C1R3,C2R0=c.ViewProjection.C2R0,C2R1=c.ViewProjection.C2R1,C2R2=c.ViewProjection.C2R2,C2R3=c.ViewProjection.C2R3,C3R0=c.ViewProjection.C3R0,C3R1=c.ViewProjection.C3R1,C3R2=c.ViewProjection.C3R2,C3R3=c.ViewProjection.C3R3}};
static NativePlanetaryPatch[] CreateDiagnosticPatches(){var colors=new[]{(1f,.2f,.2f),(.2f,1f,.2f),(.2f,.4f,1f),(1f,1f,.2f),(1f,.2f,1f),(.2f,1f,1f)};var result=new NativePlanetaryPatch[6];for(uint face=0;face<6;face++){var color=colors[face];result[face]=new(){Face=face,Level=0,X=0,Y=0,Radius=1,ColorR=color.Item1,ColorG=color.Item2,ColorB=color.Item3,ColorA=1};}return result;}
static void UpdatePlanetaryPatches(HostState state){if(state.Earth is not null){state.Earth.UpdatePatches(state.Camera);return;}if(state.PlanetaryPatches.Length==0)return;var relative=Double3.Zero-state.Camera.Position.Value;foreach(ref var patch in state.PlanetaryPatches.AsSpan()){patch.CenterX=(float)relative.X;patch.CenterY=(float)relative.Y;patch.CenterZ=(float)relative.Z;}}
static uint ActivePlanetaryPatchCount(HostState state)=>(uint)(state.Earth?.ActivePatchCount??state.PlanetaryPatches.Length);
static void LogEarthLodDiagnostics(HostState state,EarthPlanetaryScene? earth){if(earth is null||(state.HasEarthLodDiagnostic&&state.LastEarthPatchCount==earth.ActivePatchCount&&state.LastEarthMinimumLod==earth.MinimumActiveLod&&state.LastEarthMaximumLod==earth.MaximumActiveLod&&state.LastEarthRepresentation==earth.Representation))return;state.HasEarthLodDiagnostic=true;state.LastEarthPatchCount=earth.ActivePatchCount;state.LastEarthMinimumLod=earth.MinimumActiveLod;state.LastEarthMaximumLod=earth.MaximumActiveLod;state.LastEarthRepresentation=earth.Representation;Console.WriteLine($"Earth LOD: patches={earth.ActivePatchCount}; min={earth.MinimumActiveLod}; max={earth.MaximumActiveLod}; refined={earth.RefinementCount}; balanced={earth.BalancedRefinementCount}; culled={earth.CulledPatchCount}; representation={earth.Representation}; altitude/radius={earth.AltitudeRadii:R}");}
file sealed class HostState(CameraState camera,ReferenceFrameSnapshot frames,ReferenceFrameResolver resolver,ResolvedRenderSnapshot snapshot,RenderFrameSubmission submission,LogOptions log,double movementSpeed,DynamicReferenceFrameFixtureScene? dynamic,CelestialAnalyticalScene? celestial,EarthPlanetaryScene? earth,NativePlanetaryPatch[] planetaryPatches){public CameraState Camera=camera;public CameraState Default=new(new FramePosition(camera.Position.Frame,camera.Position.Value),camera.Orientation,camera.Projection,camera.Mode);public ReferenceFrameSnapshot Frames=frames;public ReferenceFrameResolver Resolver=resolver;public ResolvedRenderSnapshot Snapshot=snapshot;public RenderFrameSubmission Submission=submission;public LogOptions Log=log;public FreeCameraController Controller=new(movementSpeed,.002,Math.PI*89/180);public DynamicReferenceFrameFixtureScene? Dynamic=dynamic;public CelestialAnalyticalScene? Celestial=celestial;public EarthPlanetaryScene? Earth=earth;public NativePlanetaryPatch[] PlanetaryPatches=planetaryPatches;public bool DynamicFailureLogged;public bool CelestialFailureLogged;public bool HasSasIndicatorDiagnostic;public bool LastSasTargetValid;public long NextSasIndicatorDiagnosticTimestamp;public bool HasEarthLodDiagnostic;public int LastEarthPatchCount;public int LastEarthMinimumLod;public int LastEarthMaximumLod;public PlanetaryRepresentation LastEarthRepresentation;}
file readonly record struct SampleOptions(int ObjectCount,string Scene,string[] LogArguments){public static bool TryParse(string[] a,out SampleOptions v,out string? e){var n=100;var scene="grid";var logs=new List<string>();foreach(var x in a){if(x.StartsWith("--objects=")&&(!int.TryParse(x[10..],out n)||n is not(1 or 100 or 1000 or 10000))){v=default;e="Usage: --objects=1|100|1000|10000";return false;}if(x.StartsWith("--scene="))scene=x[8..];else logs.Add(x);}if(scene is not("grid"or"frames"or"fixture"or"fixture-dynamic"or"celestial"or"planetary-diagnostic"or"earth")){v=default;e="Usage: --scene=grid|frames|fixture|fixture-dynamic|celestial|planetary-diagnostic|earth";return false;}v=new(n,scene,logs.ToArray());e=null;return true;}}
