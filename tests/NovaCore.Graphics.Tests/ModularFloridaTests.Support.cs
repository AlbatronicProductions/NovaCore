using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SupportProbe()
    {
        var t=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(var count in new[]{2,3}){
            var craft=CraftCompiler.Compile(catalog,ScalableLaunchSupportFixture.Document(catalog,Enumerable.Repeat("nc.tank.long-2",count).ToArray()).Data,Assets);
            var p=new CompiledCraftContact(craft);var site=AssemblyFloridaSite.CreateCraftSlab(t.Query,default,new(3),t.Slab,p);
            var g=site.LinearAcceleration(site.At(default),AssemblyContactProfile.Upright.Rotate(craft.InitialMass.Com),default);
            foreach(var rotating in new[]{false,true})try{
                var proof=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftSupportPreparation.Prepare(p,craft.InitialMass,g,rotating?site:null,supportDimensions:t.Slab.Dimensions);
                Console.WriteLine($"SUPPORT_SITE_PROBE n={count} rotating={rotating} gravity={g} slab={t.Slab.Dimensions} peak={proof.MaximumObservedLoad:R}");
            }catch(InvalidDataException e){Console.WriteLine($"SUPPORT_SITE_PROBE n={count} rotating={rotating} {e.Message}");}
        }
    }
    internal static void ScalableSupport()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));var results=new List<object>();
        foreach(var tanks in new[]{new[]{"short"},new[]{"long"},new[]{"short","short"},new[]{"long","long"},new[]{"short","short","long"}}){
            var draft=ScalableLaunchSupportFixture.Document(catalog,tanks.Select(t=>"nc.tank."+t+"-2").ToArray(),fill:false);
            using var editor=new ConstructionEditorSession(catalog);editor.Load(0,draft.Save());var empty=editor.Save(editor.Revision);editor.FillForLaunch(editor.Revision);var saved=editor.Save(editor.Revision);
            Need(!saved.SequenceEqual(empty),"ordinary explicit Fill changes finite inventory");editor.Undo(editor.Revision);Need(editor.Save(editor.Revision).SequenceEqual(empty),"Fill undo");editor.Redo(editor.Revision);Need(editor.Save(editor.Revision).SequenceEqual(saved),"Fill redo");
            var craft=CraftCompiler.Compile(catalog,editor.Current!.Design.Data,Assets);var started=Stopwatch.GetTimestamp();var allocated=GC.GetAllocatedBytesForCurrentThread();
            using(var immediate=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab)){
                var original=Observe(immediate);var exactPose=original.Physical!.Motion;
                Need(exactPose.PositionO==Double3.Zero&&exactPose.VelocityO==Double3.Zero,"canonical spawn retains exact original material pose");
                Need(immediate.Engine.AdmitAssemblyControl(immediate.Control!,immediate.Control!.Identity,1,new(true)).Status==AssemblyControlStatus.Admitted,"immediate ignition without preliminary idle");
                Need(immediate.Engine.AdmitConstructionHostTime(immediate.Authority,1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"immediate host credit");
                Need(immediate.Engine.ServiceConstructionDebt(immediate.Authority,out var first)==ConstructionServiceStatus.Published&&first==1,"first mass-changing midpoint/contact import publishes");
                Need(Observe(immediate).ReferenceMass!.Value.Mass<original.ReferenceMass!.Value.Mass,"first midpoint and endpoint use consumed finite propellant");
            }
            started=Stopwatch.GetTimestamp();allocated=GC.GetAllocatedBytesForCurrentThread();
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            var admissionMs=Stopwatch.GetElapsedTime(started).TotalMilliseconds;var admissionBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            var host=0L;var control=0L;var supported=new List<double>();var releaseMs=0d;var steadyBytes=new List<long>();var released=false;var departureMass=0d;var departureTime=0L;
            var native=s.Engine.ConstructionContactWorldForTest(s.Authority)!;var poolBytes=native.PoolBytes;
            void Advance(bool measure){
                var before=Observe(s);Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625))==ConstructionServiceStatus.AcceptedCredit,"support host credit");
                var b=GC.GetAllocatedBytesForCurrentThread();var t=Stopwatch.GetTimestamp();var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);var ms=Stopwatch.GetElapsedTime(t).TotalMilliseconds;var bytes=GC.GetAllocatedBytesForCurrentThread()-b;
                Need(status==ConstructionServiceStatus.Published&&count==1,$"support lifecycle: {status} {s.Engine.ConstructionSupportFailure(s.Authority)} host={host} mass={before.ReferenceMass!.Value.Mass:R} motion={before.Physical!.Motion}");var after=Observe(s);
                if(measure&&!released){supported.Add(ms);steadyBytes.Add(bytes);}
                if(!released&&after.Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight){
                    Console.WriteLine("SUPPORT_RELEASE "+JsonSerializer.Serialize(new{tanks,host,mass=after.ReferenceMass!.Value.Mass,motion=after.Physical!.Motion}));
                    released=true;releaseMs=ms;departureMass=after.ReferenceMass!.Value.Mass;departureTime=after.Epoch.Ticks;
                    Need(s.Engine.ConstructionContactWorldForTest(s.Authority) is null,"all child constraints retire with the support world");
                    var dt=1d/64;var bound=Norm(before.Physical!.Motion.VelocityO)*dt+40*dt*dt+4*s.Binding.Physical!.Contact.ContactTolerance;
                    Need(Norm(after.Physical.Motion.PositionO-before.Physical.Motion.PositionO)<bound,"support release has no position jump");
                }
            }
            for(var i=0;i<320;i++)Advance(i>=64);
            Need(!released&&Observe(s).Physical!.Consumer==AssemblyPhysicalConsumer.SupportedContact,"five seconds stable physical support");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,++control,new(true)).Status==AssemblyControlStatus.Admitted,"player ignition authority");
            Advance(true);
            if(craft.InitialMass.Mass*9.7>30720)Need(!released,"heavy ignition remains supported until real mass/thrust unload");
            while(!released&&host<30000)Advance(true);
            Need(released,$"finite fuel depletion reaches physical liftoff: final mass={Observe(s).ReferenceMass!.Value.Mass:R}");
            // Heavy craft lifts off near T/W=1; gain safe altitude before cutoff/attitude.
            for(var i=0;i<640;i++)Advance(false);
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,++control,new(false,new(1,1,1))).Status==AssemblyControlStatus.Admitted,"cutoff and combined physical RCS input");
            var omega=Observe(s).Physical!.Motion.AngularVelocityBody;
            for(var i=0;i<16;i++)Advance(false);
            Need(Norm(Observe(s).Physical!.Motion.AngularVelocityBody-omega)>1e-4,"larger spacecraft has physical attitude response");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,++control,new(false)).Status==AssemblyControlStatus.Admitted,"physical input release");Advance(false);
            Need(editor.Save(editor.Revision).SequenceEqual(saved),"flight/return preserves construction document");
            var identity=s.Binding.Physical!.Support.Digest;var peak=s.Binding.Physical.Support.MaximumObservedLoad;var proof=s.Binding.Physical.Support;
            s.Dispose();Need(s.Engine.ConstructionContactWorldForTest(s.Authority) is null,"return retains no native support world");
            editor.Load(editor.Revision,saved,true);var rebuilt=CraftCompiler.Compile(catalog,editor.Current!.Design.Data,Assets);
            using(var reload=ConstructionApplicationSession.CreateSupported(rebuilt,terrain.Query,terrain.Slab))Need(reload.Binding.Physical!.Support.Digest==identity,"save/reload deterministically selects identical site support");
            supported.Sort();steadyBytes.Sort();
            var row=new{tanks,mass=craft.InitialMass.Mass,jets=craft.Allocation.JetActuators.Length,peak,identity,proof.MaximumDisplacement,departureMass,departureTime,admissionMs,admissionBytes,retainedPoolBytes=poolBytes,supported=new{median=supported[supported.Count/2],p95=supported[(int)(supported.Count*.95)],p99=supported[(int)(supported.Count*.99)],worst=supported[^1],allocatedMedian=steadyBytes[steadyBytes.Count/2]},releaseMs};
            results.Add(row);Console.WriteLine("SUPPORT_FLIGHT "+JsonSerializer.Serialize(row));
        }
        var bad=ScalableLaunchSupportFixture.Document(catalog,Enumerable.Repeat("nc.tank.long-2",4).ToArray());var badBytes=bad.Save();var invalid=CraftCompiler.Compile(catalog,bad.Data,Assets);
        try{using var impossible=ConstructionApplicationSession.CreateSupported(invalid,terrain.Query,terrain.Slab);throw new Exception("Overloaded craft admitted");}
        catch(InvalidDataException e){Need(e.Message.Contains("load capacity",StringComparison.Ordinal),"actual total-capacity refusal reason");Need(bad.Save().SequenceEqual(badBytes),"refusal preserves source");}
        // Corrupt only the disposable private native continuation to force a real
        // solved overload; all canonical state must survive the refused interval.
        var baseline=CraftCompiler.Compile(catalog,ScalableLaunchSupportFixture.Document(catalog,["nc.tank.short-2"]).Data,Assets);
        using(var s=ConstructionApplicationSession.CreateSupported(baseline,terrain.Query,terrain.Slab)){
            var world=s.Engine.ConstructionContactWorldForTest(s.Authority)!;var simulation=Field<BepuPhysics.Simulation>(world,"simulation");var body=simulation.Bodies[Field<BepuPhysics.BodyHandle>(world,"body")];body.Velocity.Linear=new Vector3(0,-10,0);
            Need(s.Engine.AdmitConstructionHostTime(s.Authority,1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"overload witness credit");var clock=s.Engine.CaptureContinuationClock();var state=Observe(s);var revision=s.Engine.State.Revision;
            Need(s.Engine.ServiceConstructionDebt(s.Authority,out var published)==ConstructionServiceStatus.Invalidated&&published==0,"actual per-foot overload refuses publication");
            Need(s.Engine.ConstructionSupportFailure(s.Authority)?.Contains("Authored support load exceeded",StringComparison.Ordinal)==true,"actual support reason reaches player diagnostic owner");
            Need(s.Engine.State.Spacecraft.TryGetConstruction(s.Binding.Spacecraft.Id,out _,out var after)&&ReferenceEquals(state,after)&&clock==s.Engine.CaptureContinuationClock()&&revision==s.Engine.State.Revision,"overload preserves source state resources time and revision");
        }
        Console.WriteLine($"SCALABLE_SUPPORT_FLIGHT_PASS checks={checks}");
    }
}





