using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

// CPU qualification only. This never creates a Vulkan device. Production camera,
// topology, pupil, height field, selector and publication coordinator are used.
// Screen classification is a conservative CPU model, NOT GPU parity evidence.
internal static class EarthBlackoutOfflineTests
{
    record Pose(string Name, Double3 Camera, Double3 Forward, Double3 Right, Double3 Up, double Altitude, double Fov, double Aspect=3440d/1440d);
    record Measurement(string Pose, int Height, int Level, int Vertices, int Triangles,
        int HorizonRejected, int ScreenRejected, int RetainedUpperBound, int NearCamera,
        int NonFiniteFactors, int NonFiniteOccluded, int NonFiniteScreenOutside, int NonFinitePotentiallyRetained, int SaturatedEdges, double MaximumFactor, long TessellationWorkEnvelope,
        int[] FactorHistogram, double Milliseconds);
    static void Require(bool value,string text){if(!value)throw new InvalidOperationException(text);}
    public static void Replay()
    {
        var result=new List<object>();
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        var repository=GraphicsTestHarness.RepositoryPath();
        Require(TerrainAssetCache.TryResolveRequired(repository,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var local,out var error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(local,out error),error);
        int fps=int.TryParse(Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_REPLAY_FPS"),out var requestedFps)?requestedFps:60;
        Require(fps is >=30 and <=240,"Bounded replay frame rate");
        var replayOutput=Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_REPLAY_REPORT")??Path.Combine(repository,"build","earth-blackout-closure","offline-replay.json");
        for(int stage=1;stage<=6;stage++)
        {
            var frame=new ReferenceFrameId(1);SolarSystemScene? candidate;
            bool created=long.TryParse(Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_REPLAY_EPOCH"),out var epoch)
                ?SolarSystemScene.TryCreateAt(frame,new SimulationInstant(epoch),out candidate,out error)
                :Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_CURRENT")=="1"
                ?SolarSystemScene.TryCreate(frame,out candidate,out error)
                :SolarSystemScene.TryCreateAt(frame,SimulationInstant.Zero,out candidate,out error);
            Require(created,error);
            var scene=candidate!;var camera=new CameraState(new FramePosition(frame,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
            var replay=new BlackoutCameraQualification(stage);double minimum=double.MaxValue,maximumDot=-1;int horizonFrames=0;
            for(int tick=0;tick<150*fps;tick++)
            {
                var input=replay.PrepareInput(new NativeInputState{DeltaSeconds=1f/fps,ViewportWidthPixels=3440,ViewportHeightPixels=1322},scene,camera);
                scene.ApplyPresentationInput(camera,input,out _,out _);
                Require(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded((double)input.DeltaSeconds),camera,out error),error);
                scene.Focus(camera,input.PresentationFocus);
                scene.EnforceFinalCameraInvariant(camera); // Same final camera boundary as the Player host.
                if(scene.FocusedBody.BodyId!=6)continue;
                Require(camera.Position.Value.IsFinite&&scene.SurfaceAltitudeMetres>=9.999,"Replay preserves finite camera and minimum clearance");
                minimum=Math.Min(minimum,scene.SurfaceAltitudeMetres);
                var radial=(camera.Position.Value-scene.FocusedBody.Position.Value).Normalized();double dot=Double3.Dot(radial,camera.Orientation.Rotate(-Double3.UnitZ));
                maximumDot=Math.Max(maximumDot,dot);if(dot>-.2&&dot<.2&&scene.SurfaceAltitudeMetres<1000)horizonFrames++;
            }
            result.Add(new{stage,fps,epoch,targetObserved=replay.TargetObserved,minimumAltitude=minimum,maximumRadialDot=maximumDot,horizonFrames});
            File.WriteAllText(replayOutput,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine(JsonSerializer.Serialize(result[^1]));
            Require(replay.TargetObserved&& (stage<4||horizonFrames>100),"Replay must reach the specified camera workload stage "+stage);
        }
        Console.WriteLine(JsonSerializer.Serialize(result));
    }
    public static void Run()
    {
        var root=GraphicsTestHarness.RepositoryPath();
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var local,out var error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(local,out error),error);
        var frame=new ReferenceFrameId(1);
        SolarSystemScene? candidate;
        bool created=Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_CURRENT")=="1"
            ?SolarSystemScene.TryCreate(frame,out candidate,out error)
            :SolarSystemScene.TryCreateAt(frame,SimulationInstant.Zero,out candidate,out error);
        Require(created&&candidate is not null,error);
        var scene=candidate!;
        var camera=new CameraState(new FramePosition(frame,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        Require(scene.Focus(camera,NativePresentationFocus.Earth),"Earth focus");
        scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
        var poses=new List<Pose>();
        void Capture(string name)
        {
            var inverse=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized();
            var gpu=scene.GpuConstants(camera);
            var bodyCamera=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
            poses.Add(new(name,bodyCamera,inverse.Rotate(camera.Orientation.Rotate(-Double3.UnitZ)),
                inverse.Rotate(camera.Orientation.Rotate(Double3.UnitX)),inverse.Rotate(camera.Orientation.Rotate(Double3.UnitY)),
                Math.Max(10d,gpu.SurfaceAltitudeMetres),camera.Projection.VerticalFieldOfViewRadians));
        }
        Capture("far-Earth");
        foreach(var altitude in new[]{1e6,1e5,1e4,1e3,100d,10.01})
        {
            for(int n=0;n<200&&scene.SurfaceAltitudeMetres>altitude;n++)
                scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
            Capture("approach-"+altitude);
        }
        for(int n=0;n<24;n++)
        {
            scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=131,MouseDeltaY=n<12?45:-45},out _,out _);
            if(n%3==0)Capture("actual-orbit-"+n);
        }
        // Additional adversarial view directions at the actual near-ground position.
        // These are explicit synthetic horizon samples, not fabricated player input evidence.
        var basis=poses[7];var outward=basis.Camera.Normalized();
        var tangent=Double3.Cross(outward,Math.Abs(outward.Y)<.9?Double3.UnitY:Double3.UnitX).Normalized();
        foreach(var degrees in new[]{-15d,-1d,0d,1d,15d})
        {
            var forward=(tangent*Math.Cos(degrees*Math.PI/180)+outward*Math.Sin(degrees*Math.PI/180)).Normalized();
            var right=Double3.Cross(forward,outward).Normalized();var up=Double3.Cross(right,forward).Normalized();
            poses.Add(basis with{Name="synthetic-horizon-"+degrees,Forward=forward,Right=right,Up=up});
        }
        if(Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_CAMERA_REPORT") is {Length:>0} cameraReport)
        {
            using var recorded=JsonDocument.Parse(File.ReadAllText(cameraReport));
            var sample=recorded.RootElement.GetProperty("lastSnapshots").EnumerateArray().Last(p=>p.GetProperty("gpuCompleted").GetBoolean());
            Double3 Vector(string name){var a=sample.GetProperty(name).EnumerateArray().Select(n=>n.GetDouble()).ToArray();return new(a[0],a[1],a[2]);}
            var eye=Vector("camera");var forward=Vector("forward").Normalized();
            var right=Double3.Cross(forward,eye.Normalized()).Normalized();var up=Double3.Cross(right,forward).Normalized();
            // Roll does not change the enclosing view cone or projected edge length.
            poses.Add(new("recorded-live-horizon",eye,forward,right,up,sample.GetProperty("altitude").GetDouble(),Math.PI/3,
                sample.GetProperty("width").GetDouble()/sample.GetProperty("height").GetDouble()));
        }
        if(Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_POSE_INPUT") is {Length:>0} poseInput)
        {
            using var saved=JsonDocument.Parse(File.ReadAllText(poseInput));
            poses=saved.RootElement.GetProperty("poses").EnumerateArray().Select(p=>{
                Double3 V(string name){var v=p.GetProperty(name);return new(v.GetProperty("X").GetDouble(),v.GetProperty("Y").GetDouble(),v.GetProperty("Z").GetDouble());}
                return new Pose(p.GetProperty("Name").GetString()!,V("Camera"),V("Forward"),V("Right"),V("Up"),
                    p.GetProperty("Altitude").GetDouble(),p.GetProperty("Fov").GetDouble(),p.GetProperty("Aspect").GetDouble());
            }).ToList();
        }
        var (levels,contracts)=PlanetaryNestedScaleMeshRuntimeAdapter.Adapt(PlanetaryNestedScaleMeshTopologyLibrary.Load(Path.Combine(root,"assets","planetary-nested-scale-mesh")));
        if(Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_POSE") is {Length:>0} onlyPose)
            poses=poses.Where(p=>p.Name==onlyPose).ToList();
        Require(poses.Count>0,"Offline workload must contain a pose");
        var results=new List<Measurement>();var selections=new List<object>();
        // Include native ultrawide height and 4K height. Exact native dimensions
        // must still be recorded by the diagnostic renderer before a live gate.
        foreach(int height in new[]{1440,2160})
        {
            var selector=new PlanetaryProductionSphericalBillboardSelector(levels);ulong tick=0;
            foreach(var pose in poses)
            {
                var view=new PlanetaryProductionBillboardView(pose.Altitude,pose.Camera.Normalized(),(int)Math.Ceiling(height*pose.Aspect),height,pose.Fov,tick++);
                var selection=selector.Evaluate(view,false);
                // Exercise actual adjacent publication transitions before the settled measurement.
                for(int transition=0;transition<36;transition++)
                {
                    int previous=selector.CurrentLevel;
                    selection=selector.Evaluate(view with{CompletedFrame=tick++},false);
                    Require(Math.Abs(previous-selection.Level)<=1,"adjacent selection bound");
                    if(selector.InFlightLevel>=0)selector.CommitPublication(selection.Level);
                }
                selections.Add(new{pose.Name,height,selection.Level,selection.BaseErrorPixels,selection.RequiredTesFactor});
                results.Add(Measure(pose,height,levels[selection.Level],contracts[levels[selection.Level].TopologyHash]));
                Console.WriteLine(JsonSerializer.Serialize(results[^1]));
            }
        }
        var scheduling=Scheduling(root,levels,contracts,poses);
        var topology=levels.Select(t=>new{t.Level,vertices=t.Vertices.Count,triangles=t.TriangleCount,
            latticeBytes=(long)t.Vertices.Count*16,indexBytes=(long)t.Indices.Count*4,
            physicalBytes=(long)t.Vertices.Count*64,visibilityBytes=(long)t.TriangleCount*4,
            compactedIndexBytes=(long)t.Indices.Count*4,regions=t.Regions.Select(r=>new{r.Identity,r.VertexCount,r.TriangleCount}).ToArray()}).ToArray();
        var invalid=results.Sum(r=>r.NonFiniteFactors);
        var output=Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_REPORT")??Path.Combine(root,"build","earth-blackout-closure","offline-workload.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output,JsonSerializer.Serialize(new{liveGatePassed=false,
            cullingDisabledForBound=Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_UNCULLED")=="1",
            scope="CPU actual camera/selector/pupil/height/coordinator; conservative screen upper bound; no shader execution or GPU timing qualification",
            poses,selections,topology,scheduling,results,invalid,
            unresolved=new[]{"FP32 shader parity, physical GPU preparation lifetime and measured GPU cost require further qualification","Screen model deliberately omits triangle narrow phase and near/far plane rejection; retained population is an upper bound"}},new JsonSerializerOptions{WriteIndented=true}));
        Require(invalid==0,"Non-finite TES edge factors in retained CPU terrain; live route remains blocked. See "+output);
    }
    static Measurement Measure(Pose pose,int height,PlanetaryProductionSphericalBillboardTopology topology,PlanetaryProductionCullContract contract)
    {
        var watch=Stopwatch.StartNew();var pupil=PlanetaryProductionBillboardPupil.Resolve(default,pose.Camera.Normalized(),topology);
        var points=new Double3[topology.Vertices.Count];
        for(int i=0;i<points.Length;i++)
        {
            var direction=pupil.ResolveCanonicalDirection(topology.Vertices[i],topology);
            double h=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,direction);
            points[i]=direction*(PlanetaryProductionSphericalBillboardTopologyGenerator.EarthRadiusMetres+h);
            Require(points[i].IsFinite,"finite prepared CPU vertex");
        }
        int horizon=0,screen=0,retained=0,near=0,invalid=0,invalidOccluded=0,invalidScreen=0,invalidRetained=0,saturated=0;double max=1;long work=0;int[] histogram=new int[7];
        double tanY=Math.Tan(pose.Fov*.5),tanX=tanY*pose.Aspect;
        double halfCone=Math.Atan(Math.Sqrt(tanX*tanX+tanY*tanY));
        bool ignoreCulling=Environment.GetEnvironmentVariable("NOVACORE_OFFLINE_UNCULLED")=="1";
        for(int i=0;i<topology.Indices.Count;i+=3)
        {
            var a=points[topology.Indices[i]];var b=points[topology.Indices[i+1]];var c=points[topology.Indices[i+2]];
            var center=(a+b+c)/3;double baseRadius=Math.Sqrt(Math.Max((a-center).LengthSquared,Math.Max((b-center).LengthSquared,(c-center).LengthSquared)));
            double radius=baseRadius+contract.MaximumTesDisplacementMetres+.02;
            bool wouldOcclude=pose.Camera.LengthSquared>contract.PlanetOcclusionSupportRadiusMetres*contract.PlanetOcclusionSupportRadiusMetres&&
                PlanetaryProductionSphericalBillboardCulling.IsOccludedByPlanet(pose.Camera,new(center,radius),contract.PlanetOcclusionSupportRadiusMetres);
            if(!ignoreCulling&&wouldOcclude){horizon++;continue;}
            var relative=center-pose.Camera;double distance=Math.Sqrt(relative.LengthSquared);
            // Keep the whole production broad cone with full TES support and
            // extra angular margin; omit every rectangular/narrow rejection.
            double cone=halfCone+(distance>radius?Math.Asin(Math.Clamp(radius/distance,0,1)):Math.PI)+.001;
            bool wouldScreen=distance>radius&&cone<Math.PI&&Double3.Dot(relative/distance,pose.Forward)<Math.Cos(cone);
            if(!ignoreCulling&&wouldScreen){screen++;continue;}
            retained++;if(Math.Sqrt(relative.LengthSquared)-baseRadius<=50)near++;
            Double3 View(Double3 point){var r=point-pose.Camera;return new(Double3.Dot(r,pose.Right),Double3.Dot(r,pose.Up),-Double3.Dot(r,pose.Forward));}
            var va=View(a);var vb=View(b);var vc=View(c);
            double f0=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(va,vb,height,pose.Fov,50),
                f1=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(vb,vc,height,pose.Fov,50),
                f2=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(vc,va,height,pose.Fov,50);
            if(!double.IsFinite(f0)||!double.IsFinite(f1)||!double.IsFinite(f2)){
                invalid++;if(wouldOcclude)invalidOccluded++;else if(wouldScreen)invalidScreen++;else invalidRetained++;
                if(invalid==1)Console.WriteLine(JsonSerializer.Serialize(new{diagnostic="first-nonfinite-edge",pose.Name,height,wouldOcclude,wouldScreen,
                    first=new[]{va.X,va.Y,va.Z},second=new[]{vb.X,vb.Y,vb.Z},third=new[]{vc.X,vc.Y,vc.Z}}));
                continue;
            }
            saturated+=(f0>=64?1:0)+(f1>=64?1:0)+(f2>=64?1:0);
            var factor=Math.Max(f0,Math.Max(f1,f2));max=Math.Max(max,factor);
            int bucket=factor<=1?0:factor<=2?1:factor<=4?2:factor<=8?3:factor<=16?4:factor<=32?5:6;histogram[bucket]++;
            // Deliberately loose combinatorial envelope, not a measured invocation count.
            long n=(long)Math.Ceiling(factor)+2;work+=6*n*n;
        }
        return new(pose.Name,height,topology.Level,points.Length,topology.TriangleCount,horizon,screen,retained,near,invalid,invalidOccluded,invalidScreen,invalidRetained,saturated,max,work,histogram,watch.Elapsed.TotalMilliseconds);
    }
    static object Scheduling(string root,IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,IReadOnlyDictionary<ulong,PlanetaryProductionCullContract> contracts,List<Pose> poses)
    {
        var runtime=new PlanetaryProductionSphericalBillboardMovingRuntime(root,levels,contracts);
        uint ready=0;ulong tick=0,submits=0;var payloads=new Dictionary<int,(object Lattice,object Indices)>();
        long allocatedStart=GC.GetTotalAllocatedBytes(true);ulong maximumResident=0;
        foreach(var pose in poses.Concat(poses.AsEnumerable().Reverse()))
        {
            var view=new PlanetaryProductionBillboardView(pose.Altitude,pose.Camera.Normalized(),3440,1440,pose.Fov,tick++);
            for(int n=0;n<50;n++)
            {
                var telemetry=runtime.Update(view with{CompletedFrame=tick++},ready);maximumResident=Math.Max(maximumResident,telemetry.PeakResidentGpuBytes);
                if(runtime.TrySubmitPrepared(out var generation))
                {
                    Require(generation.NativeGpuPhysicalPreparation&&generation.Physical.Vertices.Length==0,"CPU coordinator must not build replacement GPU physical arrays");
                    if(payloads.TryGetValue(generation.Topology.Level,out var previous))Require(ReferenceEquals(previous.Lattice,generation.Lattice)&&ReferenceEquals(previous.Indices,generation.Indices),"topology arrays reused by identity");
                    else payloads.Add(generation.Topology.Level,(generation.Lattice,generation.Indices));
                    // Delayed synthetic acknowledgement: verifies coordinator only, never GPU completion.
                    for(int delay=0;delay<3;delay++){runtime.Update(view with{CompletedFrame=tick++},ready);Require(!runtime.TrySubmitPrepared(out _),"one submitted replacement until acknowledgement");}
                    ready=checked((uint)generation.PublicationGeneration);submits++;
                }
                else if(runtime.ReplacementInFlight)Thread.Sleep(1);
            }
        }
        return new{scope="actual coordinator with synthetic completion acknowledgements",submits,distinctTopologyPayloads=payloads.Count,maximumResidentEstimate=maximumResident,
            managedAllocatedBytes=GC.GetTotalAllocatedBytes(true)-allocatedStart,frames=tick};
    }
}
