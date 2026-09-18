using System.Reflection;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class AssemblyContactAdmissionTests
{
    private static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(value)!;
    private static void SetField(object value,string name,object field)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.SetValue(value,field);
    private static void RejectCold(Action action,string label)
    {
        try{action();}catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException){return;}
        throw new InvalidOperationException("Cold contact input accepted: "+label);
    }
    internal static void AdditionalFailures(bool powered=false)
    {
        AssemblyApplicationSession Session()=>powered?PoweredSession():AssemblyContactAdmissionTests.Session();
        foreach(var name in new[]{"physical","stores","resource-revision","mass","com","inertia","command","gimbal","actuator","state-revision","timeline"})
        {
            using var s=Session();Credit(s,16666);var e=s.Engine;var a=s.Authority;
            Check(e.PrepareAssemblyContact(a,out var receipt)==AssemblyFlightStatus.Prepared,"mutation pending source");
            var state=(SimulationState)Field(e,"_state");var before=Observe(s);var changed=before.State;
            changed=name switch
            {
                "physical"=>changed with {Motion=changed.Motion with {PositionO=Double3.UnitX}},
                "stores"=>changed with {Stores=default},
                "resource-revision"=>changed with {ResourceRevision=1},
                "mass"=>changed with {Mass=changed.Mass with {Mass=706}},
                "com"=>changed with {Mass=changed.Mass with {Com=Double3.UnitX}},
                "inertia"=>changed with {Mass=changed.Mass with {Inertia=Matrix3.Identity}},
                "command"=>changed with {AppliedCommand=new(true,null,0,0,16666)},
                "gimbal"=>changed with {Gimbal=new(.01,0,.01,0)},
                "actuator"=>changed with {ActuatorRevision=1},_=>changed
            };
            if(name=="state-revision")state.CommitMarkerValue(0);
            else if(name=="timeline")s.Clock.Timeline.Schedule(s.Clock.CurrentTime,new(new(1),new(16666),0,NovaCore.Simulation.Timeline.SimulationEventKind.Marker));
            else state.InstallAssembly(0,changed,before.StateRevision); // Deliberate unauthorized external write, test only.
            var snapshot=Observe(s);var world=e.AssemblyContactWorldForTest(a)!;var wi=world.AssemblyIdentityForTest;var pending=s.Clock.Timeline.PendingCount;
            Check(e.PublishAssemblyContact(a,receipt).Status==AssemblyFlightStatus.StaleSource,"post-prepare stale "+name);
            Check(Observe(s)==snapshot&&world.AssemblyIdentityForTest==wi&&s.Clock.Timeline.PendingCount==pending,"post-prepare refusal nonmutation "+name);
        }
        using(var s=Session())using(var other=Session())
        {
            Credit(s,16666);Credit(other,16666);
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out var original)==AssemblyFlightStatus.Prepared&&other.Engine.PrepareAssemblyContact(other.Authority,out _) == AssemblyFlightStatus.Prepared,"foreign sources");
            Check(other.Engine.PrepareAssemblyContact(other.Authority,out _)==AssemblyFlightStatus.OutstandingProposal,"foreign source pending");
            var foreignStorage=Field(other.Engine,"_assemblyFlight");var storage=Field(s.Engine,"_assemblyFlight");
            var snapshot=Observe(s);var foreignSnapshot=Observe(other);var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var wi=w.AssemblyIdentityForTest;
            var current=Field(storage,"ContactReceipt");var foreign=Field(foreignStorage,"ContactReceipt");
            foreach(var kind in new[]{"default","foreign","generation","frontier"})
            {
                object replacement=kind=="foreign"?foreign:kind=="default"?default(LocalContactWorld.Receipt):(LocalContactWorld.Receipt)current;
                if(kind=="generation")SetField(replacement,"<Generation>k__BackingField",w.Generation+1);
                if(kind=="frontier")SetField(replacement,"Step",0L);
                SetField(storage,"ContactReceipt",replacement);
                Check(s.Engine.PublishAssemblyContact(s.Authority,original).Status==AssemblyFlightStatus.InvalidProposal,"native receipt "+kind);
                Check(Observe(s)==snapshot&&Observe(other)==foreignSnapshot&&w.AssemblyIdentityForTest==wi,"foreign/native refusal nonmutation");
            }
            SetField(storage,"ContactReceipt",current);
            var foreignProposal=new AssemblyFlightProposal((long)Field(foreignStorage,"Generation"),Field(foreignStorage,"Seal"));
            Check(s.Engine.PublishAssemblyContact(s.Authority,foreignProposal).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==snapshot,"genuine foreign proposal");
            Check(s.Engine.PublishAssemblyContact(s.Authority,original).Status==AssemblyFlightStatus.Published,"original sound proposal remains usable");
            Credit(s,16667);Check(s.Engine.PrepareAssemblyContact(s.Authority,out var next)==AssemblyFlightStatus.Prepared,"second proposal");snapshot=Observe(s);wi=w.AssemblyIdentityForTest;
            Check(s.Engine.PublishAssemblyContact(s.Authority,original).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==snapshot&&w.AssemblyIdentityForTest==wi,"old proposal while new pending");
            Check(s.Engine.PublishAssemblyContact(s.Authority,next).Status==AssemblyFlightStatus.Published,"new proposal valid");
        }
        foreach(var kind in new[]{"disposed","domain","precision"})
        {
            using var s=Session();Credit(s,16666);var e=s.Engine;var a=s.Authority;var w=e.AssemblyContactWorldForTest(a)!;var before=Observe(s);
            if(kind=="disposed")
            {
                Check(e.PrepareAssemblyContact(a,out var receipt)==AssemblyFlightStatus.Prepared,"disposed pending");w.Dispose();var wi=w.AssemblyIdentityForTest;
                Check(e.PublishAssemblyContact(a,receipt).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==before&&w.AssemblyIdentityForTest==wi,"disposed world cannot publish");
            }
            else
            {
                var sim=NativeField<BepuPhysics.Simulation>(w,"simulation");var body=sim.Bodies[NativeField<BepuPhysics.BodyHandle>(w,"body")];
                body.Pose.Position.X=kind=="domain"?20:100000; // Deliberate unsafe private state: exercise real step/export failure.
                var result=e.PrepareAssemblyContact(a,out _);
                Check(result is AssemblyFlightStatus.Invalidated or AssemblyFlightStatus.OutsideContactDomain,"real post-native failure");
                var after=Observe(s);Check(after.State==before.State&&after.Clock==before.Clock&&after.HistoryCount==0&&after.StateRevision==before.StateRevision&&after.PrivateInvalidated,"unsafe step preserves canonical authority");
                var wi=w.AssemblyIdentityForTest;Check(e.PrepareAssemblyContact(a,out _)==AssemblyFlightStatus.Invalidated&&w.AssemblyIdentityForTest==wi&&Observe(s)==after,"unsafe continuation cannot repeat");
            }
        }
        using(var s=Session())
        {
            Credit(s,long.MaxValue);var before=Observe(s);var wi=s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest;
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,2,new(1)).Status==AssemblyFlightStatus.Overflow&&Observe(s)==before&&s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest==wi,"checked debt overflow nonmutation");
        }
        var catalog=AssemblyStockCatalog.LoadDefault();var design=catalog.Resolve("novacore.stock.SRV01.FourHorn");var profile=AssemblyContactProfile.Create(design);
        var launch=AssemblyLaunch.CreateSupported(profile,new(new(201),new(1),new(2),"SRV-01 supported"),"failure-tests");
        using(var s=AssemblyApplicationSession.CreateSupported(launch,initialRevision:new(ulong.MaxValue)))
        {
            Credit(s,16666);var before=Observe(s);var wi=s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest;
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.Overflow&&Observe(s)==before&&s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest==wi,"revision overflow before step");
        }
        RejectCold(()=>AssemblyContactProfile.Create(catalog.Resolve("novacore.stock.SRV01.G0B")),"foreign pinned design");
        RejectCold(()=>AssemblyApplicationSession.CreateSupported(launch,0),"zero capacity");
        RejectCold(()=>AssemblyLaunch.CreateSupported(profile,launch.Spacecraft,"bad-frame",new(double.NaN,0,0)),"nonfinite moving frame");
        RejectCold(()=>AssemblyLaunch.CreateSupported(profile,launch.Spacecraft,"bad-epoch",origin:new(long.MaxValue)),"source end overflow");
        RejectCold(()=>AssemblyApplicationSession.Create(launch),"contact launch through free-flight begin");
        var freeLaunch=new AssemblyLaunch(design,launch.Spacecraft,"free-control",new(default,default,DoubleQuaternion.Identity,default),default,[new(false,null,0,0,15625)]);
        RejectCold(()=>AssemblyApplicationSession.CreateSupported(freeLaunch),"free launch through contact begin");
        using(var s=AssemblyApplicationSession.Create(freeLaunch))
        {
            var before=Observe(s);
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority&&s.Engine.PublishAssemblyContact(s.Authority,default).Status==AssemblyFlightStatus.InvalidAuthority&&s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.InvalidAuthority&&Observe(s)==before,"contact consumer refuses free-flight authority");
            var dto=AssemblyJson.Read<AssemblySaveData>(s.Save(),1_048_576);
            RejectCold(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Schema="novacore.assembly-contact/1",Plan=launch.Plan.Select(x=>x.Request).ToArray(),Current=launch.Initial,HistoryCapacity=1200})),"contact snapshot through free-flight restore");
        }
        Console.WriteLine("ASSEMBLY_CONTACT_ADDITIONAL_FAILURES PASS mutable authority / native receipts / terminal exports / arithmetic / cold consumer boundaries");
    }
}
