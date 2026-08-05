using System.Runtime.InteropServices;
using NovaCore.Core;using NovaCore.Core.Camera;using NovaCore.Core.ReferenceFrames;using NovaCore.Graphics;using NovaCore.Interop;using NovaCore.Platform;using NovaCore.Simulation.Time;
if(!SampleOptions.TryParse(args,out var options,out var error)){Console.Error.WriteLine(error);return 2;}if(!LogOptions.TryParse(options.LogArguments,out var log,out var logError)){Console.Error.WriteLine(logError);return 2;}return Run(options,log);
static unsafe int Run(SampleOptions options,LogOptions log)
{
    var root=new ReferenceFrameId(1); var defaultStart=new UniversePosition(new Double3(4e12,-3e12,7e12),root);
    ResolvedRenderSnapshot? snapshot=null; FixtureSceneDiagnostics fixture=default; DynamicReferenceFrameFixtureScene? dynamic=null; CelestialAnalyticalScene? celestial=null; var cameraPosition=defaultStart.Value; var projection=new CameraProjection(Math.PI/3,16d/9,.01,1000); var movementSpeed=.1d;
    if(options.Scene=="fixture")
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
    var state=new HostState(new CameraState(new FramePosition(root,cameraPosition),DoubleQuaternion.Identity,projection,CameraMode.Free),frames,new ReferenceFrameResolver(frames),snapshot!,new RenderFrameSubmission(snapshot!.Count,snapshot.OrbitCurve?.Count??0),log,movementSpeed,dynamic,celestial);
    if(!TryBuild(state,out var buildError)){Console.Error.WriteLine(buildError);return 1;}
    var objects=new NativeRenderObject[snapshot.Count];var batches=new NativeDrawBatch[snapshot.Count];var orbit=new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var previousOrbit=new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var h=GCHandle.Alloc(state);
    CopyObjects(state.Submission,objects,batches,orbit,previousOrbit,out var batchCount);
    fixed(NativeRenderObject* po=objects)fixed(NativeDrawBatch* pb=batches)fixed(NativeOrbitLineVertex* pl=orbit)fixed(NativeOrbitLineVertex* pp=previousOrbit){NativeFrameSubmission n=new(){Camera=NativeCamera(state.Submission.Camera),Objects=po,ObjectCount=(uint)objects.Length,Batches=pb,BatchCount=(uint)batchCount,OrbitVertices=pl,OrbitVertexCount=(uint)state.Submission.OrbitVertexCount,PreviousOrbitVertices=pp,PreviousOrbitVertexCount=(uint)state.Submission.PreviousOrbitVertexCount};try{return NativeRuntime.RunRenderer(&n,Callback,GCHandle.ToIntPtr(h))==NativeResult.Success?0:1;}finally{h.Free();}}
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
    var celestialScene=s.Celestial;Span<CameraCommand> commands=stackalloc CameraCommand[4];var count=DebugCameraInput.Map(e->Input,commands,celestialScene is null,celestialScene is null);var previousSpeed=s.Controller.MovementSpeed;
    if(s.Dynamic is not null){var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!s.Dynamic.TryAdvanceByHostDuration(duration,out var publicationError)&&!s.DynamicFailureLogged){Console.Error.WriteLine(publicationError);s.DynamicFailureLogged=true;}else s.Snapshot=s.Dynamic.CurrentSnapshot;}
    if(celestialScene is not null){celestialScene.ApplyPresentationInput(s.Camera,e->Input,out var rateChanged,out var pauseChanged);var previousSasMode=celestialScene.SasMode;var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!celestialScene.TryAdvanceByHostDuration(duration,e->Input,out var publicationError)&&!s.CelestialFailureLogged){Console.Error.WriteLine(publicationError);s.CelestialFailureLogged=true;}else s.Snapshot=celestialScene.CurrentSnapshot;if(s.Log.IsEnabled(LogCategory.Camera)&&rateChanged)Console.WriteLine($"Celestial rate={celestialScene.Rate.Numerator}:{celestialScene.Rate.Denominator}");if(s.Log.IsEnabled(LogCategory.Camera)&&pauseChanged)Console.WriteLine($"Celestial paused={celestialScene.IsPaused}");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Celestial zoom distance={celestialScene.OrbitDistance:R}");if(s.Log.IsEnabled(LogCategory.Camera)&&previousSasMode!=celestialScene.SasMode)Console.WriteLine($"Celestial SAS mode={celestialScene.SasMode}");}
    if(e->Input.Reset!=0){s.Camera.Position=s.Default.Position;s.Camera.Orientation=s.Default.Orientation;s.Camera.Projection=s.Default.Projection;s.Camera.Mode=s.Default.Mode;s.Controller.Reset();celestialScene?.ResetPresentationCamera(s.Camera);}else if(celestialScene is null)s.Controller.Update(s.Camera,commands[..count],e->Input.DeltaSeconds);
    if(!TryBuild(s,out var buildError)){Console.Error.WriteLine(buildError);PostQuitMessage(1);return;} CopyObjectsToNative(s.Submission,e->Submission->Objects,e->Submission->Batches,e->Submission->OrbitVertices,e->Submission->PreviousOrbitVertices,out var batchCount);e->Submission->ObjectCount=(uint)s.Submission.ObjectCount;e->Submission->BatchCount=(uint)batchCount;e->Submission->OrbitVertexCount=(uint)s.Submission.OrbitVertexCount;e->Submission->PreviousOrbitVertexCount=(uint)s.Submission.PreviousOrbitVertexCount;e->Submission->Camera=NativeCamera(s.Submission.Camera);
    if(celestialScene is null&&s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Camera wheelDetents={e->Input.MouseWheelDetents} speed={previousSpeed:R}->{s.Controller.MovementSpeed:R}");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.Reset!=0)Console.WriteLine($"Camera reset speed={s.Controller.MovementSpeed:R}");
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
    Console.WriteLine("Scene: celestial");Console.WriteLine("Clock-driven analytical two-body marker fixture; no prescribed orbital phase.");Console.WriteLine($"Presentation: 1 display unit = {CelestialAnalyticalScene.MetresPerDisplayUnit:R} m; clock rate={CelestialAnalyticalScene.SampleRate}:1.");Console.WriteLine("Scheduled inertial impulse: +200 m/s tangential at 100000 simulated seconds.");Console.WriteLine("Controls: LMB/RMB drag orbit, wheel zoom, WASD/QE manual torque, 1-7 SAS select, 0 SAS off, ,/. rate, Space pause, R camera reset.");Console.WriteLine("Objects: central marker + satellite marker; triangle geometry is presentation-only.");
}
[DllImport("user32.dll")] static extern void PostQuitMessage(int exitCode);
static unsafe void CopyObjects(RenderFrameSubmission s,NativeRenderObject[] o,NativeDrawBatch[] b,NativeOrbitLineVertex[] l,NativeOrbitLineVertex[] p,out int count){fixed(NativeRenderObject* po=o)fixed(NativeDrawBatch* pb=b)fixed(NativeOrbitLineVertex* pl=l)fixed(NativeOrbitLineVertex* pp=p)CopyObjectsToNative(s,po,pb,pl,pp,out count);}
static unsafe void CopyObjectsToNative(RenderFrameSubmission s,NativeRenderObject* o,NativeDrawBatch* b,NativeOrbitLineVertex* l,NativeOrbitLineVertex* p,out int count){for(var i=0;i<s.ObjectCount;i++){var x=s.Objects[i];o[i]=new(){Position=NativePosition(x.Position),Transform=new(){RotationX=x.Transform.Rotation.X,RotationY=x.Transform.Rotation.Y,RotationZ=x.Transform.Rotation.Z,RotationW=x.Transform.Rotation.W,ScaleX=x.Transform.Scale.X,ScaleY=x.Transform.Scale.Y,ScaleZ=x.Transform.Scale.Z},Mesh=new(){Value=x.Mesh.Value}};}count=s.BatchCount;for(var i=0;i<count;i++){var x=s.Batches[i];b[i]=new(){Mesh=new(){Value=x.Mesh.Value},FirstObject=x.FirstObject,ObjectCount=x.ObjectCount};}for(var i=0;i<s.OrbitVertexCount;i++){var x=s.OrbitVertices[i];l[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}for(var i=0;i<s.PreviousOrbitVertexCount;i++){var x=s.PreviousOrbitVertices[i];p[i]=new(){X=x.X,Y=x.Y,Z=x.Z};}}
static NativeEncodedPosition NativePosition(EncodedPosition x,float highPadding=0,float lowPadding=0)=>new(){HighX=x.HighX,HighY=x.HighY,HighZ=x.HighZ,HighPadding=highPadding,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ,LowPadding=lowPadding};static NativeCameraData NativeCamera(GpuCameraData c)=>new(){Position=NativePosition(c.Position,c.PositionHighPadding,c.PositionLowPadding),ViewProjection=new(){C0R0=c.ViewProjection.C0R0,C0R1=c.ViewProjection.C0R1,C0R2=c.ViewProjection.C0R2,C0R3=c.ViewProjection.C0R3,C1R0=c.ViewProjection.C1R0,C1R1=c.ViewProjection.C1R1,C1R2=c.ViewProjection.C1R2,C1R3=c.ViewProjection.C1R3,C2R0=c.ViewProjection.C2R0,C2R1=c.ViewProjection.C2R1,C2R2=c.ViewProjection.C2R2,C2R3=c.ViewProjection.C2R3,C3R0=c.ViewProjection.C3R0,C3R1=c.ViewProjection.C3R1,C3R2=c.ViewProjection.C3R2,C3R3=c.ViewProjection.C3R3}};
file sealed class HostState(CameraState camera,ReferenceFrameSnapshot frames,ReferenceFrameResolver resolver,ResolvedRenderSnapshot snapshot,RenderFrameSubmission submission,LogOptions log,double movementSpeed,DynamicReferenceFrameFixtureScene? dynamic,CelestialAnalyticalScene? celestial){public CameraState Camera=camera;public CameraState Default=new(new FramePosition(camera.Position.Frame,camera.Position.Value),camera.Orientation,camera.Projection,camera.Mode);public ReferenceFrameSnapshot Frames=frames;public ReferenceFrameResolver Resolver=resolver;public ResolvedRenderSnapshot Snapshot=snapshot;public RenderFrameSubmission Submission=submission;public LogOptions Log=log;public FreeCameraController Controller=new(movementSpeed,.002,Math.PI*89/180);public DynamicReferenceFrameFixtureScene? Dynamic=dynamic;public CelestialAnalyticalScene? Celestial=celestial;public bool DynamicFailureLogged;public bool CelestialFailureLogged;}
file readonly record struct SampleOptions(int ObjectCount,string Scene,string[] LogArguments){public static bool TryParse(string[] a,out SampleOptions v,out string? e){var n=100;var scene="grid";var logs=new List<string>();foreach(var x in a){if(x.StartsWith("--objects=")&&(!int.TryParse(x[10..],out n)||n is not(1 or 100 or 1000 or 10000))){v=default;e="Usage: --objects=1|100|1000|10000";return false;}if(x.StartsWith("--scene="))scene=x[8..];else logs.Add(x);}if(scene is not("grid"or"frames"or"fixture"or"fixture-dynamic"or"celestial")){v=default;e="Usage: --scene=grid|frames|fixture|fixture-dynamic|celestial";return false;}v=new(n,scene,logs.ToArray());e=null;return true;}}
