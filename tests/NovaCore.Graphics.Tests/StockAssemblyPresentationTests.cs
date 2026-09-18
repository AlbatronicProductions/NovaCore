using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using System.Runtime.InteropServices;
using System.Numerics;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class StockAssemblyPresentationTests
{
    internal static void SupportedStorage(bool powered=false)
    {
        // Cold full-heap measurement, separated from timing/allocation gates. Warm shared
        // type metadata first; include the whole live scene, pins, submission and samples.
        WarmSupportedStorage();
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var before=GC.GetTotalMemory(true);
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,poweredSupport:powered);
        var render=new RenderFrameSubmission(scene.RenderCapacity);var native=new NativeRenderObject[scene.RenderCapacity];
        scene.Start();for(var i=0;i<1200;i++){scene.Advance(new(i%3==0?16666:16667));scene.BuildSubmission(default,new(default,new(1)),render);}
        Check(scene.Completed&&!scene.Failed&&scene.Observation.State.Frontier==1200,"storage workload completed all 1200 intervals");
        var session=(AssemblyApplicationSession)typeof(StockAssemblyDevelopmentScene).GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(scene)!;
        var pool=(long)session.Engine.AssemblyContactWorldForTest(session.Authority)!.PoolBytes;
        var managed=GC.GetTotalMemory(true)-before;var gpuMeshPayload=scene.Visuals.BufferBytes;
        GC.KeepAlive(scene);GC.KeepAlive(render);GC.KeepAlive(native);
        Console.WriteLine($"ASSEMBLY_SUPPORTED_STORAGE retainedManagedSceneDelta={managed} nativePool={pool} ownedCpuRetained={managed+pool} gpuMeshPayload={gpuMeshPayload} ownedCpuPlusGpuPayload={managed+pool+gpuMeshPayload} limit=8388608 sharedRendererDeviceSwapchainExcluded=true");
        Console.WriteLine("ASSEMBLY_SUPPORTED_GPU_BUFFERS "+string.Join(',',scene.Visuals.Assets.SelectMany(a=>a.Meshes).SelectMany(m=>new long[]{m.VertexCount*48L,m.TriangleCount*12L}).Concat(new long[]{8*48,36*4})));
        Check(managed>=640000+gpuMeshPayload&&managed+pool+gpuMeshPayload<=8L*1024*1024,"combined owned scene, native world and visual mesh payload <=8MiB");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void WarmSupportedStorage()
    {using var warm=new StockAssemblyDevelopmentScene(supportedContact:true);warm.Start();warm.Advance(new(16666));}
    internal static unsafe void Supported()
    {
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true);var initial=scene.Observation;
        Check(scene.InitialSnapshot.Count==38&&initial.State.Mass.Mass==705&&initial.Clock.Debt.Ticks==0,"same seven parts plus declared slab; zero initial debt");
        Check(SampleOptions.TryParse(["--scene=srv01-supported-contact"],out var options,out _)&&options.Scene=="srv01-supported-contact","explicit supported CLI");
        var render=new RenderFrameSubmission(scene.RenderCapacity);var native=new NativeRenderObject[scene.RenderCapacity];
        void Draw()
        {
            scene.BuildSubmission(default,new(new(10,3,8),new(1)),render);
            for(var i=0;i<render.ObjectCount;i++)native[i].Mesh=new(){Value=render.Objects[i].Mesh.Value};
            fixed(NativeRenderObject* p=native)scene.WriteExhaustParameters(p,render.ObjectCount);
        }
        Draw();Check(render.ObjectCount==38&&render.Objects[37].Mesh==MeshHandle.ContactQualificationSupport,"physical slab represented, no exhaust");
        Check((render.Objects[37].Position.Reconstruct()-(new Double3(0,-2.7,0)-new Double3(10,3,8))).LengthSquared<1e-24,"camera-relative slab centre matches 16x2x16 collider and top -1.7");
        scene.Advance(new(20_000_000));Check(scene.Observation==initial,"READY ignores elapsed before start");scene.Start();
        scene.Advance(new(16665));Check(scene.Observation.State.Frontier==0,"supported exact debt boundary");
        scene.Advance(new(1));Check(scene.Observation.State.Frontier==1,"first16666 publication");
        var first=scene.Observation;Draw();scene.BuildSubmission(default,new(new(-7,6,-9),new(1)),render);
        Check(scene.Observation==first&&scene.PresentedPart(6).RootPosition.Value==first.State.Motion.PositionO,"camera-independent material-O presentation");
        scene.Advance(new(20_000_000-16666));Check(scene.Observation.State.Frontier==5,"delayed frame is bounded to four intervals");
        while(!scene.Completed&&!scene.Failed)scene.Advance(default);
        var final=scene.Observation;Check(scene.Completed&&!scene.Failed&&final.State.Frontier==1200&&final.State.Stores==initial.State.Stores&&final.State.Mass==initial.State.Mass,"supported full episode exact resources");
        Draw();scene.Advance(new(999));Check(scene.Observation==final&&render.ObjectCount==38&&!scene.ActiveExhaust,"held endpoint, no departure or exhaust");
        using var measured=new StockAssemblyDevelopmentScene(supportedContact:true);measured.Start();
        for(var i=0;i<128;i++){measured.Advance(new(i%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
        using(var allocation=new OrdinaryAllocationMeasurement("supported-assembly-application-presentation"))
        {
            for(var i=128;i<1152;i++){measured.Advance(new(i%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
            OrdinaryAllocationMeasurement.RequireZero(allocation.Complete(),"supported-assembly-application-presentation");
        }
        Check(!measured.Failed&&measured.Observation.State.Frontier==1152,"measured supported presentation advances correctly");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"ASSEMBLY_SUPPORTED_PRESENTATION PASS copied-O/38-draws/no-exhaust/camera/backlog/held-endpoint visualMeshBytes={scene.Visuals.BufferBytes} displaySampleArraysBytes=640000 slabUsesExistingMesh=true");
    }
    private static void Check(bool v,string name){if(!v)throw new InvalidOperationException("Assembly presentation: "+name);}
    internal static unsafe void PoweredSupport()
    {
        Check(SampleOptions.TryParse(["--scene=srv01-powered-support"],out var options,out _)&&options.Scene=="srv01-powered-support","explicit powered support route");
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,poweredSupport:true);
        var original=scene.Observation;var render=new RenderFrameSubmission(scene.RenderCapacity);var native=new NativeRenderObject[scene.RenderCapacity];
        void Draw()
        {
            scene.BuildSubmission(default,new(new(10,3,8),new(1)),render);
            for(var i=0;i<render.ObjectCount;i++)native[i].Mesh=new(){Value=render.Objects[i].Mesh.Value};
            fixed(NativeRenderObject* p=native)scene.WriteExhaustParameters(p,render.ObjectCount);
        }
        scene.Advance(new(20_000_000));Draw();Check(scene.Observation==original&&render.ObjectCount==38,"READY has no prefunded credit/exhaust");
        scene.Start();scene.Advance(new(16666));Draw();var first=scene.Observation;
        Check(first.State.Actual.MainOn&&first.State.Actual.Jets==0&&first.State.Stores.Fuel!=original.State.Stores.Fuel&&render.ObjectCount==39,"main-only supported realized plume and finite resource debit");
        scene.BuildSubmission(default,new(new(-7,6,-9),new(1)),render);
        Check(scene.Observation==first&&scene.PresentedPart(6).RootPosition.Value==first.State.Motion.PositionO,"powered camera-independent material-O presentation");
        scene.Advance(new(20_000_000-16666));Check(scene.Observation.State.Frontier==5,"powered backlog limited to4");
        while(!scene.Completed&&!scene.Failed)scene.Advance(default);
        Draw();var final=scene.Observation;scene.Advance(new(999));
        Check(scene.Completed&&!scene.Failed&&final.State.Frontier==1200&&final.State.Mass.Mass==701.09375&&
            final.State.Stores==new AssemblyStores(AssemblyResources.Mass(28.4375),AssemblyResources.Mass(42.65625))&&scene.Observation==final&&render.ObjectCount==38&&!scene.ActiveExhaust,
            "bounded powered episode completes with exact fuel/mass and frozen endpoint, plume off");
        using var measured=new StockAssemblyDevelopmentScene(supportedContact:true,poweredSupport:true);measured.Start();
        for(var i=0;i<128;i++){measured.Advance(new(i%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
        using(var allocation=new OrdinaryAllocationMeasurement("powered-supported-assembly-presentation"))
        {
            for(var i=128;i<1152;i++){measured.Advance(new(i%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
            OrdinaryAllocationMeasurement.RequireZero(allocation.Complete(),"powered-supported-assembly-presentation");
        }
        Check(!measured.Failed&&measured.Observation.State.Frontier==1152,"powered measured visual workload");
        OrdinaryAllocationMeasurement.PositiveControl();Console.WriteLine("ASSEMBLY_POWERED_SUPPORT_PRESENTATION PASS ready/canonical-pose/main-only/resources/camera/backlog/completion");
    }
    internal static unsafe void Run()
    {
        PoweredSupport();
        Supported();
        Check(Marshal.SizeOf<NativeVisualVertex>()==48&&Marshal.OffsetOf<NativeVisualVertex>(nameof(NativeVisualVertex.R))==12&&Marshal.OffsetOf<NativeVisualVertex>(nameof(NativeVisualVertex.Nx))==24&&Marshal.OffsetOf<NativeVisualVertex>(nameof(NativeVisualVertex.Metallic))==36,"native vertex ABI");
        Check(Marshal.SizeOf<NativeVisualMesh>()==32&&Marshal.OffsetOf<NativeVisualMesh>(nameof(NativeVisualMesh.Indices))==8&&Marshal.OffsetOf<NativeVisualMesh>(nameof(NativeVisualMesh.VertexCount))==16&&Marshal.OffsetOf<NativeVisualMesh>(nameof(NativeVisualMesh.IndexCount))==20&&Marshal.OffsetOf<NativeVisualMesh>(nameof(NativeVisualMesh.PresentationKind))==24,"native mesh ABI");
        using var callbackStart=new StockAssemblyDevelopmentScene();var beforeCallback=callbackStart.Observation;
        callbackStart.AdvanceLive(true);Check(callbackStart.Observation==beforeCallback,"first ready callback activates without admitting cold setup time");
        using var scene=new StockAssemblyDevelopmentScene();var initial=scene.Observation;
        Check(scene.InitialSnapshot.Count==37&&scene.Visuals.Assets.Length==4&&scene.Visuals.UniqueMeshCount==23&&initial.Clock.Debt.Ticks==0,"seven shared reusable parts, no startup credit");
        var capsule=scene.Visuals.Resolve("NC_SRV_Capsule_A/1");var capsulePath=Path.Combine(AppContext.BaseDirectory,"assets","visual","SRV01","NC_SRV_Capsule_A.glb");
        void RejectAsset(string id,string sha)
        {try{PartVisualLoader.Load(capsulePath,id,sha);}catch(InvalidDataException){return;}throw new InvalidOperationException("Invalid visual accepted");}
        RejectAsset(capsule.Identity,new string('0',64));RejectAsset("NC_SRV_Tank_A/1",capsule.Sha256);
        foreach(var mesh in scene.Visuals.Resolve("NC_SRV_Rcs_A/1").Meshes)
            Check(Enumerable.Range(0,37).Count(i=>scene.PresentedPart(i).Mesh==mesh.Handle)==4,"four block instances reuse each shared GPU mesh");
        scene.Advance(new(1_000_000));Check(scene.Observation==initial,"startup cannot spend");scene.Start();
        scene.Advance(new(15624));Check(scene.Observation.State.Frontier==0,"no hidden 60Hz resampling");
        scene.Advance(new(1));Check(scene.Observation.State.Frontier==1,"exact 64Hz interval");
        var published=scene.Observation;var render=new RenderFrameSubmission(scene.RenderCapacity);
        var nativeObjects=new NativeRenderObject[scene.RenderCapacity];
        void Transport(StockAssemblyDevelopmentScene current)
        {
            for(var i=0;i<render.ObjectCount;i++)nativeObjects[i].Mesh=new(){Value=render.Objects[i].Mesh.Value};
            fixed(NativeRenderObject* objects=nativeObjects)current.WriteExhaustParameters(objects,render.ObjectCount);
        }
        scene.BuildSubmission(default,new(new(0,0,8),new(1)),render);
        Check(render.ObjectCount==38&&scene.PresentedPart(6).RootPosition.Value==published.State.Motion.PositionO,"37 mesh instances plus one realized main exhaust; material origin from canonical copy");
        scene.BuildSubmission(default,new(new(50,20,-8),new(1)),render);Check(scene.Observation==published,"camera cannot mutate physics");
        scene.Advance(new(2_000_000-15625));Check(scene.Observation.State.Frontier==5,"at most four intervals per callback");
        while(!scene.Completed)scene.Advance(default);
        var end=scene.Observation;Check(end.State.Frontier==128&&end.Clock.Time.Ticks==2_000_000&&end.Clock.Debt.Ticks==0,"whole recorded plan completed");
        var savedEnd=scene.Save();scene.Advance(new(123));scene.BuildSubmission(default,new(default,new(1)),render);
        Check(scene.Observation==end&&scene.Save().AsSpan().SequenceEqual(savedEnd)&&render.ObjectCount==37&&!scene.ActiveExhaust,"final endpoint/save held, historical actuator flags cannot keep exhaust active");
        Check(SampleOptions.TryParse(["--scene=stock-assembly"],out var options,out _)&&
            options.StockDesign=="novacore.stock.SRV01.FourHorn"&&!options.UseProductionEarth,"ordinary stock-selection CLI");
        // Warm the exact adapter + publication-copy + presentation path separately.
        var design=AssemblyStockCatalog.LoadDefault().Resolve(Srv01StockId);
        using var warm=new StockAssemblyDevelopmentScene();warm.Start();for(var i=0;i<128;i++)
        {
            warm.Advance(new(15625));warm.BuildSubmission(default,new(default,new(1)),render);Transport(warm);
            var o=warm.Observation;var actual=o.State.Actual;var motion=o.State.Motion;
            Check(render.ObjectCount==37+(warm.ActiveExhaust?BitOperations.PopCount(actual.Jets)+(actual.MainOn?1:0):0),"only live realized thrust producers have plumes");
            var plume=37;
            void CheckPlume(Double3 point,Double3 direction,uint nozzle)
            {
                var native=nativeObjects[plume];
                Check(BitConverter.UInt32BitsToSingle(native.Padding0)==(float)(o.Clock.Time.Ticks/1_000_000d),"plume animation follows committed time");
                Check(BitConverter.UInt32BitsToSingle(native.Padding1)==3072f&&native.Padding2==nozzle,"qualified exhaust speed and stable nozzle identity");
                var item=render.Objects[plume++];var q=item.Transform.Rotation;
                var renderedDirection=new DoubleQuaternion(q.X,q.Y,q.Z,q.W).Rotate(Double3.UnitX);
                Check(item.Mesh==warm.Visuals.ExhaustMesh&&(item.Position.Reconstruct()-(motion.PositionO+motion.BodyToWorld.Rotate(point))).LengthSquared<1e-20&&
                    (renderedDirection-motion.BodyToWorld.Rotate(direction)).LengthSquared<1e-12,"plume tuple position and actual direction");
            }
            var g=design.Main.Definition.Gimbal!;
            var rotation=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,o.State.Gimbal.ActualZ)*DoubleQuaternion.FromAxisAngle(Double3.UnitY,o.State.Gimbal.ActualY);
            if(warm.ActiveExhaust&&actual.MainOn)CheckPlume(design.Main.Instance.Pose.Point(g.Pivot+rotation.Rotate(g.NozzleOffset)),design.Main.Instance.Pose.Rotation.Apply(rotation.Rotate(-Double3.UnitX)),0);
            for(var jet=0;jet<16;jet++)if(warm.ActiveExhaust&&(actual.Jets&(1<<jet))!=0)
            {var j=design.Jets[jet];CheckPlume(j.Instance.Pose.Point(j.Propulsion.Point),j.Instance.Pose.Rotation.Apply(-j.Propulsion.Axis),(uint)jet+1);}
            if(i==24)
            {
                Check(o.State.Gimbal.ActualY!=0,"nonzero published gimbal sample");
                var main=warm.Visuals.Resolve("NC_SRV_Main_A/1");
                foreach(var mesh in main.Meshes)
                {
                    var presented=Enumerable.Range(0,37).Select(warm.PresentedPart).Single(p=>p.Mesh==mesh.Handle);
                    var expected=motion.BodyToWorld*(mesh.Gimballed?rotation:DoubleQuaternion.Identity);
                    Check((presented.RootOrientation.Rotate(Double3.UnitX)-expected.Rotate(Double3.UnitX)).LengthSquared<1e-24,"only gimbal-owned meshes rotate around published pivot");
                    var local=mesh.Gimballed?design.Main.Instance.Pose.Point(g.Pivot):design.Main.Instance.Pose.Position;
                    Check(presented.RootPosition.Value==motion.PositionO+motion.BodyToWorld.Rotate(local),"gimbal and fixed engine mesh pivot positions");
                }
            }
        }
        using var measured=new StockAssemblyDevelopmentScene();measured.Start();
        using(var allocation=new OrdinaryAllocationMeasurement("assembly-application-service-presentation"))
        {
            for(var i=0;i<128;i++){measured.Advance(new(15625));measured.BuildSubmission(default,new(default,new(1)),render);Transport(measured);}
            OrdinaryAllocationMeasurement.RequireZero(allocation.Complete(),"assembly-application-service-presentation");
        }
        Check(measured.Completed,"measured complete episode");OrdinaryAllocationMeasurement.PositiveControl();
        using(var allocation=new OrdinaryAllocationMeasurement("assembly-terminal-presentation"))
        {
            for(var i=0;i<256;i++){measured.BuildSubmission(default,new(default,new(1)),render);Transport(measured);}
            OrdinaryAllocationMeasurement.RequireZero(allocation.Complete(),"assembly-terminal-presentation");
        }
        using var failure=new StockAssemblyDevelopmentScene();failure.Start();var beforeFailure=failure.Observation;failure.Advance(new(-1));
        Check(failure.Failed&&failure.Observation==beforeFailure,"expected refusal is terminal without crossing native callback");
        failure.Advance(new(15625));failure.BuildSubmission(default,new(default,new(1)),render);
        Check(failure.Observation==beforeFailure,"refused endpoint stays renderable and cannot retry");
        using var activeFailure=new StockAssemblyDevelopmentScene();activeFailure.Start();activeFailure.Advance(new(15625));
        var firing=activeFailure.Observation;activeFailure.BuildSubmission(default,new(default,new(1)),render);
        Check(render.ObjectCount==38&&firing.State.Actual.MainOn,"failure witness begins with a firing main");
        activeFailure.Advance(new(-1));activeFailure.BuildSubmission(default,new(default,new(1)),render);
        Check(activeFailure.Failed&&activeFailure.Observation==firing&&render.ObjectCount==37,"failure ends active FX without altering committed firing history");
        for(var repeat=0;repeat<3;repeat++)
        {
            var visuals=new ReusablePartVisuals(Path.Combine(AppContext.BaseDirectory,"assets","visual","SRV01"));
            Check(visuals.Assets.SelectMany(a=>a.Meshes).Sum(m=>m.TriangleCount)>0&&visuals.BufferBytes==scene.Visuals.BufferBytes,"repeat intake uses same qualified buffers");
            visuals.Dispose();visuals.Dispose();
            try{visuals.Resolve("NC_SRV_Rcs_A/1");throw new InvalidOperationException("disposed visual accepted");}catch(ObjectDisposedException){}
        }
        Console.WriteLine("ASSEMBLY_PRESENTATION PASS ready/start/64Hz/material-origin/copied-pose/camera/backlog/plan/final-hold/CLI/zero-allocation");
    }
    private const string Srv01StockId="novacore.stock.SRV01.FourHorn";
}
