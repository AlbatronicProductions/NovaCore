using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

internal static class PlayerIntegratedControlTests
{
    private static int checks;
    private static readonly AssemblyStockCatalog Catalog=AssemblyStockCatalog.LoadDefault();
    private static readonly CompiledAssemblyDesign Design=Catalog.Resolve("novacore.stock.SRV01.FourHorn");
    // Actual run-1 admitted frontiers, not wall-time or injected managed keyboard proof.
    private static readonly int[] Frontiers=[0,9,31,35,41,63,72,94];
    private static NativeInputState Input(int i)=>new(){ControlInputActive=1,
        PilotKeys=(NativePilotKeys)(i>=9&&i<31?21:i>=41&&i<63?42:i>=72&&i<94?8:0),
        EngineActions=i==0?NativeEngineActions.On:i==35?NativeEngineActions.Off:0};
    private static void Check(bool pass,string why){if(!pass)throw new InvalidOperationException("PLAYER INTEGRATED: "+why);checks++;}
    private static AssemblyApplicationSession Session(CompiledAssemblyDesign? d=null)
    {
        var plan=Enumerable.Repeat(new AssemblyCommand(false,null,0,0,15625),128).ToArray();
        var s=AssemblyApplicationSession.Create(new(d??Design,new(new(201),new(1),new(2),"SRV"),"integrated-captured-frontiers",new(default,default,DoubleQuaternion.Identity,default),default,plan));
        s.EnableLiveControl(execution:AssemblyControlExecution.PhysicalActuators);return s;
    }
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"observe");return o;}
    private static void Credit(AssemblyApplicationSession s,ref long host,long ticks)
    {Check(s.Engine.AdmitAssemblyHostTime(s.Authority,++host,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"host credit");}
    private static void Publish(AssemblyApplicationSession s)
    {Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var p)==AssemblyFlightStatus.Prepared&&s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.Published,"canonical interval");}
    internal static void Run()
    {
        using(var a=Session())using(var b=Session())
        {
            var input=new PlayerFlightControlInput(a);long ah=0,bh=0;var before=Observe(a).State;
            for(var i=0;i<128;i++)
            {
                var result=input.Apply(Input(i));
                if(Array.IndexOf(Frontiers,i)>=0)
                {
                    Check(result.Status==AssemblyControlStatus.Admitted&&result.Admission.Frontier==i,"captured frontier admission");
                    var x=result.Admission;
                    Check(b.Engine.AdmitAssemblyControl(b.Control!,x.Identity,x.Sequence,x.Requested).Admission.Effective==x.Effective,"same one-owner effective request");
                }
                Credit(a,ref ah,15625);Credit(b,ref bh,625);Credit(b,ref bh,15000);Publish(a);Publish(b);
                Check(a.Engine.TryGetAssemblyHistory(a.Authority,i,out var ar)&&b.Engine.TryGetAssemblyHistory(b.Authority,i,out var br)&&ar==br,"bit-exact history independent of credit fragmentation");
                var after=Observe(a).State;var request=input.Observation.Requested;
                var extent=(request.MainOn?5d/128:0)+BitOperations.PopCount((uint)after.Actual.Jets)*3d/2048;
                Check(AssemblyResources.Subtract(before.Stores.Fuel,after.Stores.Fuel)==AssemblyResources.Mass(extent*2/64)&&AssemblyResources.Subtract(before.Stores.Oxidizer,after.Stores.Oxidizer)==AssemblyResources.Mass(extent*3/64),"single exact combined feed debit");
                if(i==35)
                {
                    // Material origin O need not have constant velocity while the body spins.
                    var v0=before.Motion.VelocityO+before.Motion.BodyToWorld.Rotate(Double3.Cross(before.Motion.AngularVelocityBody,before.Mass.Com));
                    var v1=after.Motion.VelocityO+after.Motion.BodyToWorld.Rotate(Double3.Cross(after.Motion.AngularVelocityBody,after.Mass.Com));
                    Check(!after.Actual.MainOn&&ar.Wrench==default&&(v1-v0).LengthSquared<1e-20&&v1.LengthSquared>0,"X preserves COM translation momentum");
                }
                if(i==41)Check(!after.Actual.MainOn&&after.Actual.Jets!=0&&after.Stores!=before.Stores,"post-cutoff physical RCS");
                if(i==63||i==94)
                {
                    var l0=before.Motion.BodyToWorld.Rotate(before.Mass.Inertia.Apply(before.Motion.AngularVelocityBody));
                    var l1=after.Motion.BodyToWorld.Rotate(after.Mass.Inertia.Apply(after.Motion.AngularVelocityBody));
                    Check((l1-l0).LengthSquared<1e-18&&after.Actual.Jets==0,"release keeps angular momentum");
                }
                before=after;
            }
            Check(input.Observation.AdmissionCount==8&&Observe(a).State==Observe(b).State,"one eight-entry journal and canonical endpoint");
            using var restored=AssemblyApplicationSession.Restore(Catalog,a.Save());
            Check(Observe(restored)==Observe(a),"exact captured-pattern save continuation");
            var terminal=Observe(a);Check(input.Apply(Input(0)).Status==AssemblyControlStatus.Terminal&&Observe(a)==terminal,"terminal ignition cannot renew");
        }
        // Exhaust during simultaneous main+roll with pitch/yaw gimbal demand (frontiers9..30).
        foreach(var fuel in new[]{0d,1d/8192,1d/64})
        {
            var d=CompiledAssemblyDesign.Compile(AssemblyJson.Write(Design.Data with {Design=Design.Data.Design with {InitialFuelKg=fuel,InitialOxidizerKg=fuel*1.5}}));
            using var s=Session(d);var input=new PlayerFlightControlInput(s);long host=0;var exhaustedAt=-1;var sawOverlap=false;
            for(var i=0;i<128;i++)
            {
                var before=Observe(s).State;input.Apply(Input(i));Credit(s,ref host,15625);Publish(s);var after=Observe(s).State;
                if(i>=9&&i<31&&before.Stores!=default)sawOverlap=true;
                if(after.Stores==default&&exhaustedAt<0)exhaustedAt=i;
                if(before.Stores==default)Check(after.Stores==default&&!after.Actual.MainOn&&after.Actual.Jets==0&&after.ResourceRevision==before.ResourceRevision,"no requested actuator bypasses depleted feed");
            }
            Check(Observe(s).State.Stores==default&&exhaustedAt>=0,"both exact stores exhausted");
            if(fuel==1d/64)Check(sawOverlap&&exhaustedAt>=9&&exhaustedAt<31,"depletion inside combined main/gimbal/RCS phase");
        }
        Measure();Console.WriteLine($"PLAYER_INTEGRATED PASS checks={checks} captured_frontiers=0,9,31,35,41,63,72,94");
    }
    private static void Measure()
    {
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        var allocation=new AssemblyPilotAllocation(Design);
        var samples=Enumerable.Range(0,6).Select(_=>new double[640]).ToArray();
        long normalBytes=0,isolatedBytes=0;var gc=new int[3];
        void Episode(int repetition,bool isolate,bool timing)
        {
            Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var solar,out _),"fresh Solar/session fixture");
            using var scene=new StockAssemblyDevelopmentScene(solarWorld:solar,playerControls:true);
            var render=new RenderFrameSubmission(scene.RenderCapacity);
            var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);
            Check(solar!.BindActiveVessel(scene.PrepareFocusObservation())&&solar.RefocusActiveVessel(camera),"bind follow camera outside measurement");
            // Cold construction, asset intake and focus-selection edge excluded. Native
            // integrated capture and camera gates cover orbit/zoom/celestial/F separately.
            if(isolate)Check(GC.TryStartNoGCRegion(32*1024*1024),"combined allocation isolation entry");
            var c0=GC.CollectionCount(0);var c1=GC.CollectionCount(1);var c2=GC.CollectionCount(2);var bytes=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<128;i++)
            {
                var raw=Input(i);var begin=Stopwatch.GetTimestamp();
                var k=raw.PilotKeys;var request=AssemblyPilotDemand.FromOpposingRequests((k&NativePilotKeys.W)!=0,(k&NativePilotKeys.S)!=0,(k&NativePilotKeys.A)!=0,(k&NativePilotKeys.D)!=0,(k&NativePilotKeys.Q)!=0,(k&NativePilotKeys.E)!=0);
                var requestEnd=Stopwatch.GetTimestamp();
                scene.ApplyPlayerInput(raw);var inputEnd=Stopwatch.GetTimestamp();
                var row=allocation.Resolve(scene.PlayerInput!.Observation.Requested,15625);var allocationEnd=Stopwatch.GetTimestamp();
                scene.Advance(new(15625));var serviceEnd=Stopwatch.GetTimestamp();
                solar.RefreshActiveVessel(camera,scene.PrepareFocusObservation());scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);var end=Stopwatch.GetTimestamp();
                if(timing)
                {
                    var at=repetition*128+i;double Ms(long a,long b)=>(b-a)*1000d/Stopwatch.Frequency;
                    samples[0][at]=Ms(begin,requestEnd);samples[1][at]=Ms(requestEnd,inputEnd);samples[2][at]=Ms(inputEnd,allocationEnd);samples[3][at]=Ms(allocationEnd,serviceEnd);samples[4][at]=Ms(serviceEnd,end);samples[5][at]=Ms(begin,end);
                }
                if(request!=scene.PlayerInput.Observation.Requested.Pilot||row.Request.Ticks!=15625)throw new InvalidOperationException("measured request/allocation mismatch");
            }
            var delta=GC.GetAllocatedBytesForCurrentThread()-bytes;
            if(isolate){GC.EndNoGCRegion();isolatedBytes+=delta;}
            if(timing){normalBytes+=delta;gc[0]+=GC.CollectionCount(0)-c0;gc[1]+=GC.CollectionCount(1)-c1;gc[2]+=GC.CollectionCount(2)-c2;}
            Check(scene.Completed&&!scene.Failed&&scene.PlayerInput!.Observation.AdmissionCount==8,"measured same finite workload");
        }
        Episode(0,false,false);for(var r=0;r<5;r++)Episode(r,false,true);for(var r=0;r<5;r++)Episode(r,true,false);
        Check(isolatedBytes==0,"warmed combined control/service/follow/presentation exact zero allocation");
        var bytes=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[128]);var positive=GC.GetAllocatedBytesForCurrentThread()-bytes;Check(positive>0,"allocation positive control");
        var names=new[]{"request-sign-resolution","input-plus-canonical-admission","allocation-table-lookup","host-credit-physical-service-observation","vessel-follow-and-render-submission","combined-including-diagnostic-request-and-lookup"};
        for(var p=0;p<6;p++)
        {
            var tails=Enumerable.Range(0,5).Select(r=>samples[p].Skip(r*128).Take(128).Max()).ToArray();Array.Sort(samples[p]);var x=samples[p];
            Console.WriteLine(JsonSerializer.Serialize(new{measurement="PLAYER_INTEGRATED_COST",phase=names[p],samples=x.Length,medianMs=x[320],p95Ms=x[608],p99Ms=x[633],maxMs=x[^1],repetitionMaxMs=tails,normalBytes,isolatedBytes,collections=gc,positiveBytes=positive}));
        }
    }
}
