using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;
using System.Diagnostics;

internal static partial class AssemblyContactAdmissionTests
{
    private static AssemblyApplicationSession DepartureSession(Double3 frame=default,double speed=.25,int capacity=4)
    {
        var profile=AssemblyContactProfile.Create(AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn"));
        var launch=AssemblyLaunch.CreateDepartureQualification(profile,new(new(301),new(1),new(2),"SRV-01 separation qualification"),
            "separation-01",frame,speed);
        return AssemblyApplicationSession.CreateSupported(launch,capacity);
    }

    internal static void DepartureCheap()
    {
        using var s=DepartureSession();
        var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
        var initial=world.AssemblyIdentityForTest;
        Credit(s,16666);
        var before=Observe(s);
        var status=s.Engine.PrepareAssemblyContact(s.Authority,out var receipt);
        Console.WriteLine($"DEPARTURE_PREPARE status={status}");
        Check(status==AssemblyFlightStatus.Prepared,"genuine contact-owned separation endpoint prepared");
        Check(Observe(s)==before,"handoff preparation canonical nonmutation");
        var nativeBefore=NativeMaterialPoints(world);
        Check(s.Engine.PublishAssemblyContact(s.Authority,receipt,refuseForTest:true).Status==AssemblyFlightStatus.PreparationRefused&&Observe(s)==before,"handoff ordinary refusal retains successor");
        Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.Published,"handoff publication");
        var separated=Observe(s);
        Check(separated.Consumer==AssemblyPhysicalConsumer.FreeFlight&&!separated.PrivateInvalidated,"same owner selects free flight");
        Check(world.AssemblyIdentityForTest is {Frontier:1,Invalidated:true}&&world.AssemblyIdentityForTest.Body==initial.Body&&world.Generation==initial.Generation,"retired permission with retained body/world");
        Check(s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority,"old consumer revoked");
        Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.InvalidAuthority,"old receipt cannot republish");
        Check(world.CheckAssemblyReady(s.Engine)==LocalContactStatus.Invalidated,"native receipts remain revoked");
        Credit(s,15625);before=Observe(s);
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var flight)==AssemblyFlightStatus.Prepared,"first free-flight successor prepared");
        Check(Observe(s)==before,"flight preparation nonmutation");
        Check(s.Engine.PublishAssemblyFlight(s.Authority,flight).Status==AssemblyFlightStatus.Published,"first flight atomic successor");
        var after=Observe(s);
        Check(after.State.Epoch.Ticks==32291&&after.StateRevision.Value==2&&after.HistoryCount==2&&after.Clock.Debt.Ticks==0,"exact cumulative time/revision/debt/history");
        Check(nativeBefore.SequenceEqual(NativeMaterialPoints(world))&&world.AssemblyIdentityForTest.Frontier==1,"no native evolution or reconstruction after handoff");
        Check(after.State.Mass==AssemblyLaunch.ObserveMass(s.Launch.Design,after.State.Stores),"same exact stores derive successor mass");
        // Independent axial changing-mass COM acceleration integral; RK4 is not its oracle.
        // No angular motion in this upright fixture, so material-O and COM velocity agree.
        var h=15625/1_000_000d;
        var m0=separated.State.Mass.Mass;var m1=after.State.Mass.Mass;
        var expectedV=separated.State.Motion.VelocityO.Y+600/.1953125*Math.Log(m0/m1)-9.81*h;
        var velocityError=Math.Abs(after.State.Motion.VelocityO.Y-expectedV);
        Check(velocityError<1e-9,"independent thrust/mass/gravity first successor");
        Console.WriteLine($"DEPARTURE_FIRST_FLIGHT tick={after.State.Epoch.Ticks} heightO={after.State.Motion.PositionO.Y:R} vy={after.State.Motion.VelocityO.Y:R} oracleError={velocityError:R} nativeFrontier={world.AssemblyIdentityForTest.Frontier}");
        var iterations=0;
        while(++iterations<4)
        {
            before=Observe(s);Credit(s,s.Launch.Plan[before.State.Frontier].Request.Ticks);before=Observe(s);
            status=s.Engine.PrepareAssemblyFlight(s.Authority,out flight);
            if(status==AssemblyFlightStatus.ClearanceExpired)
            {
                Check(Observe(s)==before,"clearance expiry preserves accepted debt/last state/native authority");
                Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.ClearanceExpired&&
                    s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.ClearanceExpired&&
                    s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority&&Observe(s)==before,
                    "expired clearance retry holds state without native reacquisition");
                Console.WriteLine($"DEPARTURE_CLEARANCE_HOLD frontier={before.State.Frontier} tick={before.State.Epoch.Ticks} debt={before.Clock.Debt.Ticks}");
                return;
            }
            Check(status==AssemblyFlightStatus.Prepared&&s.Engine.PublishAssemblyFlight(s.Authority,flight).Status==AssemblyFlightStatus.Published,"bounded free-flight continuation");
        }
        Check(false,"clearance must expire before recontact in sub-weight fixture");
    }

    private static AssemblyFlightObservation Handoff(AssemblyApplicationSession s)
    {
        Credit(s,16666);
        Check(s.Engine.PrepareAssemblyContact(s.Authority,out var p)==AssemblyFlightStatus.Prepared,"handoff prepare");
        Check(s.Engine.PublishAssemblyContact(s.Authority,p).Status==AssemblyFlightStatus.Published,"handoff commit");
        return Observe(s);
    }

    internal static void DepartureFailures()
    {
        using(var s=DepartureSession())using(var foreign=DepartureSession())
        {
            var before=Observe(s);var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var wi=w.AssemblyIdentityForTest;
            Check(s.Engine.ServiceAssemblyDepartureDebt(foreign.Authority).Status==AssemblyFlightStatus.InvalidAuthority,"foreign continuation");
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority,"flight before transfer");
            Check(Task.Run(()=>s.Engine.ServiceAssemblyDepartureDebt(s.Authority)).Result.Status==AssemblyFlightStatus.WrongOwnerThread,"wrong continuation thread");
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"reentrant fixture");
            try{Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.Reentrant,"reentrant continuation");}
            finally{s.Clock.PublicationPhase.Exit();}
            Check(Observe(s)==before&&w.AssemblyIdentityForTest==wi,"consumer refusals do not mutate");
            Handoff(s);before=Observe(s);wi=w.AssemblyIdentityForTest;
            Check(s.Engine.PublishAssemblyFlight(s.Authority,default).Status==AssemblyFlightStatus.InvalidProposal,"default flight receipt");
            Check(s.Engine.PublishAssemblyFlight(s.Authority,new(2)).Status==AssemblyFlightStatus.InvalidProposal,"fabricated flight receipt");
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence,new(1)).Status==AssemblyFlightStatus.InvalidSequence,"duplicate credit after transfer");
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.AwaitingDebt,"unfunded flight");
            Check(Observe(s)==before&&w.AssemblyIdentityForTest==wi,"posthandoff refusals nonmutation");
            RejectCold(()=>s.Save(),"contact-origin transfer not old free-flight replay");
            Credit(s,15625);before=Observe(s);
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var p,true)==AssemblyFlightStatus.PreparationRefused&&Observe(s)==before,"preparation retry refusal");
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out p)==AssemblyFlightStatus.Prepared,"flight prepare");
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(1)).Status==AssemblyFlightStatus.OutstandingProposal,"pending flight credit blocked");
            Check(s.Engine.AbortAssemblyFlight(s.Authority,p)==AssemblyFlightStatus.Ready&&Observe(s)==before,"abort pure flight does not poison canonical owner");
            Check(s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.InvalidProposal,"aborted receipt stale");
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var next)==AssemblyFlightStatus.Prepared,"flight reprepares without native work");
            Check(s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==before,"old generation nonmutation");
            Check(s.Engine.PublishAssemblyFlight(s.Authority,next).Status==AssemblyFlightStatus.Published,"new receipt valid");
            before=Observe(s);
            Check(s.Engine.PublishAssemblyFlight(s.Authority,next).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==before,"duplicate no debit/history");
        }
        foreach(var kind in new[]{"physical","stores","mass","com","inertia","command","gimbal","resource-revision","actuator","state-revision","timeline","clock","rate","debt","pause","consumer"})
        {
            using var s=DepartureSession();Handoff(s);Credit(s,15625);
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var receipt)==AssemblyFlightStatus.Prepared,"stale flight pending");
            var state=(SimulationState)Field(s.Engine,"_state");var old=Observe(s);var changed=old.State;
            changed=kind switch
            {
                "physical"=>changed with {Motion=changed.Motion with {PositionO=Double3.UnitX}},
                "stores"=>changed with {Stores=default},"mass"=>changed with {Mass=changed.Mass with {Mass=706}},
                "com"=>changed with {Mass=changed.Mass with {Com=Double3.UnitX}},
                "inertia"=>changed with {Mass=changed.Mass with {Inertia=Matrix3.Identity}},
                "command"=>changed with {AppliedCommand=default},"gimbal"=>changed with {Gimbal=new(.01,0,.01,0)},
                "resource-revision"=>changed with {ResourceRevision=99},"actuator"=>changed with {ActuatorRevision=99},_=>changed
            };
            if(kind=="state-revision")state.CommitMarkerValue(0);
            else if(kind=="timeline")s.Clock.Timeline.Schedule(s.Clock.CurrentTime,new(new(1),new(32291),0,NovaCore.Simulation.Timeline.SimulationEventKind.Marker));
            else if(kind=="consumer")SetField(Field(s.Engine,"_assemblyFlight"),"Consumer",AssemblyPhysicalConsumer.SupportedContact);
            else if(kind is "clock" or "rate" or "debt" or "pause")
            {
                // Existing clock mutation API is deliberately external to the assembly owner.
                if(kind=="debt")s.Clock.AdvanceByHostDuration(new(1));
                else if(kind=="clock")SetField(s.Clock,"_currentTime",new NovaCore.Simulation.Time.SimulationInstant(old.State.Epoch.Ticks+1));
                else if(kind=="rate")s.Clock.TrySetRate(new(2,1));
                else s.Clock.Pause();
            }
            else state.InstallAssembly(0,changed,old.StateRevision);
            var before=Observe(s);var wi=s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest;
            var refused=s.Engine.PublishAssemblyFlight(s.Authority,receipt).Status;
            Check(refused is AssemblyFlightStatus.StaleSource or AssemblyFlightStatus.InvalidAuthority,"stale after handoff "+kind);
            Check(Observe(s)==before&&s.Engine.AssemblyContactWorldForTest(s.Authority)!.AssemblyIdentityForTest==wi,"stale nonmutation "+kind);
        }
        foreach(var afterTransfer in new[]{false,true})
        {
            using var s=DepartureSession();if(afterTransfer)Handoff(s);
            var ticks=afterTransfer?15625:16666;Credit(s,ticks);var before=Observe(s);
            var prepared=afterTransfer?s.Engine.PrepareAssemblyFlight(s.Authority,out var p):s.Engine.PrepareAssemblyContact(s.Authority,out p);
            Check(prepared==AssemblyFlightStatus.Prepared,"terminal pending");
            var result=afterTransfer?s.Engine.PublishAssemblyFlight(s.Authority,p,true):s.Engine.PublishAssemblyContact(s.Authority,p,true);
            var after=Observe(s);
            Check(result.Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated&&result.PublishedCount==1,"distinct committed terminal");
            Check(after.State.Epoch.Ticks==before.State.Epoch.Ticks+ticks&&after.StateRevision.Value==before.StateRevision.Value+1&&after.HistoryCount==before.HistoryCount+1&&after.Clock.Debt.Ticks==before.Clock.Debt.Ticks-ticks&&after.PrivateInvalidated,"terminal preserves actual commit");
            Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.Invalidated&&Observe(s)==after,"terminal cannot retry");
        }
        using(var s=DepartureSession(capacity:1))
        {Handoff(s);Credit(s,15625);var before=Observe(s);Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.HistoryCapacity&&Observe(s)==before,"handoff keeps finite history capacity");}
        using(var s=DepartureSession())
        {Handoff(s);var before=Observe(s);var r=s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(15625),true);var after=Observe(s);
            Check(r.Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated&&after.State==before.State&&after.Clock.Debt.Ticks==15625&&after.HistoryCount==before.HistoryCount,"credit terminal linearization after native retirement");}
        Console.WriteLine("DEPARTURE_FAILURES PASS authority/consumer/receipt/refusal/retry/terminal/capacity/save boundaries");
    }

    internal static void DepartureSchedules()
    {
        AssemblyFlightRecord[] reference=[];
        foreach(var parts in new[]{1,3,7,15,24,60})
        {
            using var s=DepartureSession();long elapsed=0;
            for(var frame=1;frame<=parts;frame++)
            {
                var next=(long)frame*63541/parts;Credit(s,next-elapsed);elapsed=next;
                var result=s.Engine.ServiceAssemblyDepartureDebt(s.Authority);
                Check(result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.ClearanceExpired,"partition status");
                Check(result.PublishedCount<=4&&Observe(s).Clock.Debt.Ticks==elapsed-Observe(s).State.Epoch.Ticks,"partition conservation");
            }
            var final=Observe(s);Check(final.State.Frontier==3&&final.State.Epoch.Ticks==47916&&final.Clock.Debt.Ticks==15625&&final.StateRevision.Value==3&&final.HistoryCount==3&&final.TimelineRevision.Value==0,"partition exact final state");
            var records=new AssemblyFlightRecord[3];
            for(var i=0;i<3;i++)Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out records[i]),"departure provenance");
            if(reference.Length==0)reference=records;
            else for(var i=0;i<3;i++)Check(records[i]==reference[i]&&MotionBits(records[i].Successor.Motion,reference[i].Successor.Motion),"same frontier bits across host partitions");
            var before=Observe(s);Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.ClearanceExpired&&Observe(s)==before,"clearance hold does not recredit/resolve");
        }
        using(var stationary=DepartureSession(speed:.2))using(var moving=DepartureSession(new(.05,0,0),.2))
        {
            Handoff(stationary);Handoff(moving);Credit(stationary,15625);Credit(moving,15625);
            Check(stationary.Engine.ServiceAssemblyDepartureDebt(stationary.Authority).PublishedCount==1&&moving.Engine.ServiceAssemblyDepartureDebt(moving.Authority).PublishedCount==1,"moving first flight");
            var a=Observe(stationary);var b=Observe(moving);var dt=b.State.Epoch.Ticks/1_000_000d;
            Check((b.State.Motion.PositionO-a.State.Motion.PositionO-new Double3(.05*dt,0,0)).LengthSquared<1e-26&&
                (b.State.Motion.VelocityO-a.State.Motion.VelocityO-new Double3(.05,0,0)).LengthSquared<1e-28&&Bits(a.State.Motion.BodyToWorld,b.State.Motion.BodyToWorld),"original moving frame is not initial relative velocity");
        }
        Console.WriteLine("DEPARTURE_SCHEDULES PASS 6 equal-time host partitions / same frontier bits / moving frame");
    }

    private static bool DepartureOperation(AssemblyApplicationSession s,long ticks)
    {
        s.Engine.ObserveAssemblyFlight(s.Authority,out var before);
        var credit=s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(ticks));
        var result=s.Engine.ServiceAssemblyDepartureDebt(s.Authority);
        var observed=s.Engine.ObserveAssemblyFlight(s.Authority,out var after);
        return credit.Status==AssemblyFlightStatus.AcceptedCredit&&result.PublishedCount==1&&result.Status==AssemblyFlightStatus.AwaitingDebt&&observed==AssemblyFlightStatus.Ready&&after.HistoryCount==before.HistoryCount+1;
    }

    internal static void DepartureAllocation()
    {
        for(var n=0;n<16;n++){using var warm=DepartureSession();Check(DepartureOperation(warm,16666)&&DepartureOperation(warm,15625),"departure warmup");}
        using var s=DepartureSession();
        using(var measurement=new OrdinaryAllocationMeasurement("departure-contact-handoff"))
        {var rawBefore=GC.GetAllocatedBytesForCurrentThread();var ok=DepartureOperation(s,16666);var rawAfter=GC.GetAllocatedBytesForCurrentThread();var bytes=measurement.Complete();Console.WriteLine($"DEPARTURE_COUNTER handoff before={rawBefore} after={rawAfter}");OrdinaryAllocationMeasurement.RequireZero(bytes,"departure-contact-handoff");Check(ok,"measured handoff");}
        using(var measurement=new OrdinaryAllocationMeasurement("departure-first-free-flight"))
        {var rawBefore=GC.GetAllocatedBytesForCurrentThread();var ok=DepartureOperation(s,15625);var rawAfter=GC.GetAllocatedBytesForCurrentThread();var bytes=measurement.Complete();Console.WriteLine($"DEPARTURE_COUNTER first-flight before={rawBefore} after={rawAfter}");OrdinaryAllocationMeasurement.RequireZero(bytes,"departure-first-free-flight");Check(ok,"measured first flight");}
        OrdinaryAllocationMeasurement.PositiveControl();
    }
}
