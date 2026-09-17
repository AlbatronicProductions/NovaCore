using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using System.Runtime.InteropServices;
using System.Numerics;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class StockAssemblyPresentationTests
{
    private static void Check(bool v,string name){if(!v)throw new InvalidOperationException("Assembly presentation: "+name);}
    internal static unsafe void Run()
    {
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
