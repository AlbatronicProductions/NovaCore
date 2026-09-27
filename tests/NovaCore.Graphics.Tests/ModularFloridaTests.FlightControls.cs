using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void LongFlight()
    {
        checks=0;
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        FlightControls(catalog,Terrain(),true);
        Console.WriteLine($"MODULAR_GATE11_LONG_FLIGHT_PASS checks={checks}");
    }
    private static void FlightControls(AssemblyDefinitionCatalog catalog,(IPhysicalSurfacePointQuery Query,FloridaSlabSupport Slab) terrain,bool longer=false)
    {
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
        NativeInputState Input(NativePilotKeys keys=NativePilotKeys.None,NativeEngineActions action=NativeEngineActions.None)=>new(){ControlInputActive=1,PilotKeys=keys,EngineActions=action};
        using(var a=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab))
        using(var b=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab))
        {
            var ai=new PlayerFlightControlInput(a);var bi=new PlayerFlightControlInput(b);long ah=0,bh=0;
            void Credit(ConstructionApplicationSession s,ref long host,long ticks)
            {
                var previous=Observe(s);Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(ticks))==ConstructionServiceStatus.AcceptedCredit,"partition credit");
                var clock=s.Engine.CaptureContinuationClock();
                Need(s.Engine.AdmitConstructionHostTime(s.Authority,host,new(ticks))==ConstructionServiceStatus.Duplicate&&s.Engine.CaptureContinuationClock()==clock,"host receipt retry preserves debt");
                while(s.Clock.PendingSimulationDebt.Ticks>=15625)Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"partition service");
            }
            void Segment(int count)
            {
                for(var i=0;i<count;i++)Credit(a,ref ah,15625);
                var remaining=count*15625L;var index=0;var pattern=new[]{1000,2345,50000,17001,11111};
                while(remaining>0){var ticks=Math.Min(remaining,pattern[index++%pattern.Length]);Credit(b,ref bh,ticks);remaining-=ticks;}
                var x=Observe(a);var y=Observe(b);
                Need(x.Physical==y.Physical&&x.Sequence==y.Sequence&&x.Epoch==y.Epoch&&x.ReferenceMass==y.ReferenceMass&&x.Fuel.Save().SequenceEqual(y.Fuel.Save())&&x.Power.Save().SequenceEqual(y.Power.Save()),"bit-identical physics and exact ledgers across host partitions");
            }
            void Apply(NativeInputState input){var x=ai.Apply(input);var y=bi.Apply(input);Need(x.Status==y.Status,"same native input admission");}
            Segment(16);Apply(Input(action:NativeEngineActions.On));Segment(128);
            Apply(Input(NativePilotKeys.W|NativePilotKeys.D|NativePilotKeys.Q));Segment(16);
            Apply(Input());Segment(16);Apply(Input(action:NativeEngineActions.Off));Segment(64);
            var snapshot=Observe(a);var clock=a.Engine.CaptureContinuationClock();var adm=ai.Observation.AdmissionCount;
            Need(a.Engine.AdmitAssemblyControl(a.Control!,b.Control!.Identity,adm+1,new(true)).Status==AssemblyControlStatus.InvalidIdentity&&ReferenceEquals(snapshot,Observe(a))&&clock==a.Engine.CaptureContinuationClock(),"foreign control identity cannot alter source");
            Need(a.Engine.AdmitConstructionHostTime(a.Authority,ah+2,new(15625))==ConstructionServiceStatus.InvalidSequence,"host sequence gap refused");
            Need(a.Engine.AdmitConstructionHostTime(a.Authority,++ah,new(15625))==ConstructionServiceStatus.AcceptedCredit,"refusal credit");
            var refusalClock=a.Engine.CaptureContinuationClock();var revision=a.Engine.State.Revision;
            Need(a.Engine.ServiceConstructionDebt(a.Authority,out var n,true)==ConstructionServiceStatus.PreparationRefused&&n==0&&ReferenceEquals(snapshot,Observe(a))&&revision==a.Engine.State.Revision&&refusalClock==a.Engine.CaptureContinuationClock(),"prepared service refusal retains all canonical state and debt");
            Need(a.Engine.ServiceConstructionDebt(a.Authority,out n)==ConstructionServiceStatus.Published&&n==1,"retry after pure preparation refusal");
            var immutable=Observe(a);
            for(var i=0;i<600;i++)Need(ai.Apply(Input(i%2==0?NativePilotKeys.Q:NativePilotKeys.E)).Status==AssemblyControlStatus.Admitted,"continuous control journal rollover");
            Need(ReferenceEquals(immutable,Observe(a))&&ai.Observation.AdmissionCount>512,"control rollover changes no physical state");
            var last=ai.Observation.AdmissionCount;var request=new AssemblyControlRequest(false,new(0,0,-1),true);
            Need(a.Engine.AdmitAssemblyControl(a.Control!,a.Control!.Identity,last,request).Status==AssemblyControlStatus.Duplicate,"retained control receipt retry");
            Need(a.Engine.AdmitAssemblyControl(a.Control!,a.Control.Identity,1,new(true)).Status==AssemblyControlStatus.InvalidSequence,"expired control receipt never re-admits");
            Need(ai.Apply(Input(NativePilotKeys.W|NativePilotKeys.S|NativePilotKeys.A|NativePilotKeys.D|NativePilotKeys.Q|NativePilotKeys.E)).Status==AssemblyControlStatus.Admitted&&ai.Observation.Requested.Pilot==default,"opposed inputs cancel");
            ai.Apply(Input(NativePilotKeys.W));ai.Apply(Input(NativePilotKeys.W) with{ControlInputActive=0});Need(ai.Observation.Requested.Pilot==default,"focus loss releases attitude");
            ai.Apply(Input(action:NativeEngineActions.On));ai.Apply(Input(action:NativeEngineActions.Off));ai.Apply(Input(action:NativeEngineActions.On));
            Need(!ai.Observation.Requested.MainOn,"OFF wins same frontier");
            Need(a.Engine.ObserveConstructionActuation(a.Authority,out var actual)==ConstructionServiceStatus.Ready&&!actual.Main,"cutoff observed at admission boundary");
        }
        foreach(var main in new[]{true,false})foreach(var sign in new[]{-1,1})
        {
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);var input=new PlayerFlightControlInput(s);long host=0;
            void Advance(int count){for(var i=0;i<count;i++){Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625))==ConstructionServiceStatus.AcceptedCredit,"control credit");Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"control physics");}}
            input.Apply(Input(action:NativeEngineActions.On));Advance(640);
            if(!main){input.Apply(Input(action:NativeEngineActions.Off));Advance(1);}
            foreach(var axis in new[]{0,1,2})
            {
                var key=axis==0?(sign>0?NativePilotKeys.Q:NativePilotKeys.E):axis==1?(sign>0?NativePilotKeys.W:NativePilotKeys.S):(sign>0?NativePilotKeys.A:NativePilotKeys.D);
                var before=Observe(s);input.Apply(Input(key));
                Need(s.Engine.ObserveConstructionActuation(s.Authority,out var realized)==ConstructionServiceStatus.Ready&&realized.Main==main&&(main&&axis!=0?realized.Jets.IsEmpty:!realized.Jets.IsEmpty),"physical gimbal/32-jet actuator selection");
                var admissions=input.Observation.AdmissionCount;input.Apply(Input(key));Need(input.Observation.AdmissionCount==admissions,"held input does not repeat admission");
                Advance(8);var after=Observe(s);var change=after.Physical!.Motion.AngularVelocityBody-before.Physical!.Motion.AngularVelocityBody;
                var component=axis==0?change.X:axis==1?change.Y:change.Z;Need(sign*component>1e-5,"physical signed axis acceleration");
                input.Apply(Input());Advance(8);var released=Observe(s);
                Need(Norm(released.Physical!.Motion.AngularVelocityBody)>1e-5,"release preserves angular momentum");
                Need(s.Engine.ObserveConstructionActuation(s.Authority,out realized)==ConstructionServiceStatus.Ready&&realized.Jets.IsEmpty,"release stops physical RCS");
            }
        }
    }
}
