using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class AssemblyConstructionTests
{
    private static ConstructionRuntimeState Observe(ConstructionApplicationSession session)
    {Check(session.Engine.ObserveConstructionServices(session.Authority,out var state)==ConstructionServiceStatus.Ready&&state is not null,"canonical construction observation");return state!;}
    private static ConstructionServiceCommand ServiceStep(bool demand=true,long ticks=1_000_000)=>new(ticks,[demand],[]);
    private static CompiledConstructionDesign RuntimeFixture()=>LocalPower(
        [new("battery",ElectricalRole.Battery,10,0,null),new("generator",ElectricalRole.EngineGenerator,0,2,"engine"),new("load",ElectricalRole.Load,0,1,null)],
        [new("battery",1,true),new("generator",0,true),new("load",0,true)],true).Fuel.Design;
    private static (ConstructionRuntimeBinding Binding,SpacecraftStateStore Store,SimulationState State,SimulationClock Clock,SimulationTransactionEngine Engine,ConstructionServiceAuthority Authority) RawConstruction(CompiledConstructionDesign design)
    {
        var binding=new ConstructionRuntimeBinding(design,default);var frames=new ReferenceFrameGraphBuilder();
        frames.Add(new ReferenceFrameNode(binding.Spacecraft.CarrierFrame,null,ReferenceFrameKind.Ecl,"test"));
        frames.Add(new ReferenceFrameNode(binding.Spacecraft.BodyFrame,binding.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"test"));
        var store=SpacecraftStateStore.CreateConstruction(binding,frames.Build());var state=new SimulationState(spacecraft:store);
        var clock=new SimulationClock(default,new SimulationTimeline(4));var engine=new SimulationTransactionEngine(clock,state);
        Check(engine.BeginConstructionServices(binding,8,out var authority)==ConstructionServiceStatus.Ready,"raw canonical construction composition");
        return(binding,store,state,clock,engine,authority!);
    }
    internal static void ConstructionRuntime()
    {
        checks=0;var design=RuntimeFixture();using var session=ConstructionApplicationSession.Create(design,capacity:8);
        using var other=ConstructionApplicationSession.Create(CompiledConstructionDesign.Load(design.Catalog,design.Save()),capacity:8);
        Check(session.Binding.Identity!=other.Binding.Identity&&session.Binding.Parts[0]!=other.Binding.Parts[0]&&!ReferenceEquals(session.Authority,other.Authority),"fresh runtime identities and capabilities for same saved design");
        Check(session.Save().SequenceEqual(other.Save()),"stock/player canonical runtime content independent of fresh observational identity");
        var initial=Observe(session);var before=session.Save();var view=session.Engine.State.Spacecraft;
        Check(initial.ReferenceMass is {} mass&&mass.Mass==3&&session.Binding.FullLoadedReference.Mass==3,"constituent full-reference mass binding");
        Check(session.Engine.AdvanceConstructionServices(other.Authority,0,ServiceStep())==ConstructionServiceStatus.InvalidAuthority,"foreign runtime authority");
        Check(session.Engine.AdvanceConstructionServices(new(session.Binding),0,ServiceStep())==ConstructionServiceStatus.InvalidAuthority,"constructible same-ID authority is not issued capability");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep(),true)==ConstructionServiceStatus.PreparationRefused,"power refusal after valid fuel preparation");
        Check(before.SequenceEqual(session.Save())&&ReferenceEquals(initial,Observe(session))&&session.Clock.CurrentTime.Ticks==0&&session.Clock.PendingSimulationDebt.Ticks==0,"joint refusal preserves fuel power clock revision history");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,new(1,[],[]))==ConstructionServiceStatus.InvalidInput,"wrong demand dimensions");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep(ticks:-1))==ConstructionServiceStatus.InvalidInput,"negative service time");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,new(1,[false],[new(-1,true)]))==ConstructionServiceStatus.InvalidInput,"invalid load command");
        Check(Task.Run(()=>session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep())).GetAwaiter().GetResult()==ConstructionServiceStatus.WrongOwnerThread,"runtime wrong thread");
        Check(session.Clock.PublicationPhase.TryEnter(new object()),"test owns exclusion phase");
        try{Check(session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep())==ConstructionServiceStatus.Reentrant,"nested publisher refused");}
        finally{session.Clock.PublicationPhase.Exit();}
        Check(before.SequenceEqual(session.Save()),"all preflight refusals immutable");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep())==ConstructionServiceStatus.Advanced,"existing transaction owner publishes joint successor");
        var next=Observe(session);Quantity(next.Fuel,"f",1,3);Quantity(next.Fuel,"o",0);Energy(next.Power,"battery",2,3);
        Check(next.ReferenceMass is null&&next.Sequence==1&&next.Epoch.Ticks==1_000_000&&session.Engine.State.Revision.Value==1&&session.Clock.PendingSimulationDebt.Ticks==0,"partial distribution unqualified; exact time state and sequence joint");
        var total=ConstructionRuntimeBinding.TotalResourceMassInQ(next.Fuel);
        Check(total.Numerator*3==ConstructionFuelNetwork.Decode(1,true)*total.Denominator,"exact total resource mass remains known after partial depletion");
        Check(!view.TryGetConstruction(session.Binding.Spacecraft.Id,out _,out _)&&initial.Fuel.Save().SequenceEqual(session.Binding.Initial.Fuel.Save()),"expired borrowed view and retained immutable snapshot");
        Check(other.Save().SequenceEqual(before),"independent runtime inventory");
        var saved=session.Save();using var restored=ConstructionApplicationSession.Restore(design.Catalog,saved);
        Check(restored.Binding.Identity!=session.Binding.Identity&&restored.Save().SequenceEqual(saved),"restore replays through owner and issues fresh identity");
        Check(restored.Engine.AdvanceConstructionServices(session.Authority,1,ServiceStep(false))==ConstructionServiceStatus.InvalidAuthority,"pre-restore authority does not transfer");
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,ServiceStep(false))==ConstructionServiceStatus.InvalidInput&&session.Save().SequenceEqual(saved),"duplicate sequence refused without replaying consumption");
        var off=ServiceStep(false);Check(session.Engine.AdvanceConstructionServices(session.Authority,1,off)==ConstructionServiceStatus.Advanced&&
            restored.Engine.AdvanceConstructionServices(restored.Authority,1,off)==ConstructionServiceStatus.Advanced&&session.Save().SequenceEqual(restored.Save()),"restored continuation exact fuel power cursor clock history");
        var record=AssemblyJson.Read<ConstructionRuntimeSave>(saved,SimulationTransactionEngine.ConstructionSaveMaximumBytes);
        Reject(()=>ConstructionApplicationSession.Restore(design.Catalog,AssemblyJson.Write(record with {FinalTicks=0})),"altered replay epoch");
        Reject(()=>ConstructionApplicationSession.Restore(design.Catalog,AssemblyJson.Write(record with {Fuel=[1,2,3]})),"altered replay resource endpoint");
        Reject(()=>ConstructionApplicationSession.Restore(design.Catalog,AssemblyJson.Write(record with {Commands=[ServiceStep(false)]})),"altered replay command history");
        session.Dispose();Check(session.Engine.AdvanceConstructionServices(session.Authority,2,off)==ConstructionServiceStatus.Retired,"dispose retires service authority without optional controls");
        Reject(()=>session.Save(),"retired session cannot save authoritative state");
        var partialDesign=LocalFuel([("partial","F",.5,true)],[]).Design;
        using(var partial=ConstructionApplicationSession.Create(partialDesign))Check(Observe(partial).ReferenceMass is null&&partial.Binding.FullLoadedReference.Mass==2,"initial partial fill has no invented spatial law");

        using(var capped=ConstructionApplicationSession.Create(design,capacity:1))
        {
            Check(capped.Engine.AdvanceConstructionServices(capped.Authority,0,ServiceStep(false,1))==ConstructionServiceStatus.Advanced,"bounded first history record");
            var bytes=capped.Save();Check(capped.Engine.AdvanceConstructionServices(capped.Authority,1,ServiceStep(false,1))==ConstructionServiceStatus.HistoryCapacity&&bytes.SequenceEqual(capped.Save()),"history refusal atomic");
        }
        using(var overflow=ConstructionApplicationSession.Create(design,new(long.MaxValue-1)))
        {var bytes=overflow.Save();Check(overflow.Engine.AdvanceConstructionServices(overflow.Authority,0,ServiceStep(false,2))==ConstructionServiceStatus.Overflow&&bytes.SequenceEqual(overflow.Save()),"time overflow before any credit or service write");}
        using(var overflow=ConstructionApplicationSession.Create(design,revision:new(ulong.MaxValue)))
        {var bytes=overflow.Save();Check(overflow.Engine.AdvanceConstructionServices(overflow.Authority,0,off)==ConstructionServiceStatus.Overflow&&bytes.SequenceEqual(overflow.Save()),"revision overflow atomic");}
        var noData=PowerPair(true,crossData:false);
        using(var disconnected=ConstructionApplicationSession.Create(noData.Fuel.Design))
        {
            var bytes=disconnected.Save();var load=Module(disconnected.Binding.Power,"load");
            Check(disconnected.Engine.AdvanceConstructionServices(disconnected.Authority,0,new(1,[],[new(load,false)]))==ConstructionServiceStatus.NoDataPath&&bytes.SequenceEqual(disconnected.Save()),"command data path independent of powered bus");
            Check(disconnected.Engine.AdvanceConstructionServices(disconnected.Authority,0,new(1_000_000,[],[]))==ConstructionServiceStatus.Advanced,"configured autonomous load does not invent control data");
            Energy(Observe(disconnected).Power,"battery",8);
        }
        ConstructionGuardProof(design);
        ConstructionConvergenceProof();
        Console.WriteLine($"Construction Stage 6 PASS: {checks} checks; shared design admission and once-only runtime registration; no physical-flight admission");
    }
    private static void ConstructionConvergenceProof()
    {
        var baseDesign=PowerPair(true,false,true).Fuel.Design;
        string Id(char prefix,int i)=>prefix+new string('+',123)+i.ToString("D4",System.Globalization.CultureInfo.InvariantCulture);
        var links=Enumerable.Range(0,4096).Select(i=>new ConstructionServiceLink(Id('s',i),"source","a","load","b",ConstructionService.Data,true)).ToImmutableArray();
        var actions=Enumerable.Range(0,4096).Select(i=>new ConstructionAction(Id('a',i),i,0,"source",null,"wire")).ToImmutableArray();
        using var editor=new ConstructionEditorSession(baseDesign.Catalog);
        editor.Load(0,baseDesign.Save());var bytes=editor.Save(editor.Revision);var revision=editor.Revision;var current=editor.Current;var preview=editor.Preview;
        var oversized=baseDesign.Data with {ServiceLinks=links,Actions=actions};
        Check(AssemblyJson.Write(oversized).Length==6_938_644,"independent serialized size of historical admitted witness");
        Reject(()=>CompiledConstructionDesign.Compile(baseDesign.Catalog,oversized),"shared canonical byte admission");
        Reject(()=>editor.SetMetadata(revision,[],actions,links),"oversized metadata refuses before installation");
        Check(editor.Revision==revision&&ReferenceEquals(current,editor.Current)&&ReferenceEquals(preview,editor.Preview)&&editor.Save(revision).SequenceEqual(bytes),"oversized proposal preserves current preview revision and saved bytes");
        foreach(var length in new[]{CompiledConstructionDesign.MaximumDocumentBytes-1,CompiledConstructionDesign.MaximumDocumentBytes})
        {var padded=new byte[length];Array.Fill(padded,(byte)' ');bytes.CopyTo(padded,0);Check(CompiledConstructionDesign.Load(baseDesign.Catalog,padded).Save().SequenceEqual(bytes),"exact ingress byte boundary roundtrip");}
        var tooLong=new byte[CompiledConstructionDesign.MaximumDocumentBytes+1];Array.Fill(tooLong,(byte)' ');bytes.CopyTo(tooLong,0);
        Reject(()=>CompiledConstructionDesign.Load(baseDesign.Catalog,tooLong),"one beyond byte boundary");
        using var runtime=ConstructionApplicationSession.Create(baseDesign);var runtimeBytes=runtime.Save();
        var saved=AssemblyJson.Read<ConstructionRuntimeSave>(runtimeBytes,SimulationTransactionEngine.ConstructionSaveMaximumBytes);
        Reject(()=>ConstructionApplicationSession.Restore(baseDesign.Catalog,AssemblyJson.Write(saved with {Design=oversized})),"runtime restore cannot bypass canonical design byte admission");
        editor.Rotate(editor.Revision,Matrix3.Mate);Check(runtime.Save().SequenceEqual(runtimeBytes),"draft mutation does not modify instantiated runtime");

        ReferenceFrameGraph Frames(ConstructionRuntimeBinding b,bool valid=true){var f=new ReferenceFrameGraphBuilder();f.Add(new ReferenceFrameNode(b.Spacecraft.CarrierFrame,null,ReferenceFrameKind.Ecl,"test"));if(valid)f.Add(new ReferenceFrameNode(b.Spacecraft.BodyFrame,b.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"test"));return f.Build();}
        Reject(()=>SpacecraftStateStore.CreateConstruction(runtime.Binding,Frames(runtime.Binding)),"duplicate binding registration refused");
        runtime.Dispose();Reject(()=>SpacecraftStateStore.CreateConstruction(runtime.Binding,Frames(runtime.Binding)),"retirement never releases observational lifetime identity");
        var binding=new ConstructionRuntimeBinding(baseDesign,default);
        Reject(()=>SpacecraftStateStore.CreateConstruction(binding,Frames(binding,false)),"bad frames before registration claim");
        var graph=Frames(binding);var successes=0;var refusals=0;
        Parallel.For(0,16,_=>{try{SpacecraftStateStore.CreateConstruction(binding,graph);Interlocked.Increment(ref successes);}catch(InvalidDataException){Interlocked.Increment(ref refusals);}});
        Check(successes==1&&refusals==15,"failed preparation does not consume claim; concurrent registration has exactly one winner");
    }
    private static void ConstructionGuardProof(CompiledConstructionDesign design)
    {
        var raw=RawConstruction(design);var id=raw.Binding.Spacecraft.Id;var view=raw.Engine.State.Spacecraft;
        Check(!view.TryGetAttitude(id,out _)&&!view.TryGetRigidBody(id,out _)&&!view.TryGetTranslation(id,out _,out _)&&!view.TryGetAppliedEndpoint(id,out _),"generic construction denies all legacy motion observations");
        raw.State.PrepareAppliedEndpointStorage();
        Check(!raw.State.TryPrepareAppliedSlot(id,default,out _)&&!raw.State.TryPrepareContinuationSlot(default,default,out _),"generic construction denies applied/contact/continuation slots");
        Check(raw.Engine.PrepareSpacecraftCommands(id,1,out _)==SpacecraftCommandStatus.SubjectUnavailable,"legacy commands cannot bind generic static construction");
        Check(raw.Engine.BeginConstructionServices(raw.Binding,8,out _)==ConstructionServiceStatus.InvalidInput,"second generic binding refused");
        foreach(Action<(ConstructionRuntimeBinding Binding,SpacecraftStateStore Store,SimulationState State,SimulationClock Clock,SimulationTransactionEngine Engine,ConstructionServiceAuthority Authority)> change in new Action<(ConstructionRuntimeBinding Binding,SpacecraftStateStore Store,SimulationState State,SimulationClock Clock,SimulationTransactionEngine Engine,ConstructionServiceAuthority Authority)>[]{
            r=>r.Clock.Pause(),r=>r.Clock.AdvanceTo(new(1)),r=>r.Clock.AdvanceByHostDuration(new(1)),r=>r.Clock.TrySetRate(SimulationRate.Half),
            r=>{r.Clock.TrySetRate(SimulationRate.Half);r.Clock.AdvanceByHostDuration(new(1));},r=>r.State.CommitMarkerValue(1),
            r=>r.Clock.Timeline.Schedule(default,new(new(1),new(1),0,SimulationEventKind.Marker))})
        {
            var r=RawConstruction(design);change(r);var clock=r.Engine.CaptureContinuationClock();var revision=r.Engine.State.Revision;
            Check(r.Engine.AdvanceConstructionServices(r.Authority,0,ServiceStep())==ConstructionServiceStatus.StaleSource,"state/timeline/time/debt/rate/remainder/pause conflict refused");
            Check(r.Engine.CaptureContinuationClock()==clock&&r.Engine.State.Revision==revision&&r.Engine.State.Spacecraft.TryGetConstruction(r.Binding.Spacecraft.Id,out _,out var state)&&ReferenceEquals(state,r.Binding.Initial),"conflict causes no service mutation");
            Check(r.Engine.RetireConstructionServices(r.Authority)==ConstructionServiceStatus.Retired,"stale authority can still retire safely");
        }
        var legacyDefinition=new SpacecraftDefinition(new(999999),new(1),new(2),"legacy probe");
        Check(SpacecraftAttitudeState.TryCreate(legacyDefinition.Id,default,DoubleQuaternion.Identity,Double3.Zero,SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1,out var attitude)==SpacecraftAttitudeEvaluationStatus.Success,"legacy attitude fixture");
        Check(SpacecraftStateStore.TryCreate([legacyDefinition],[attitude],out var legacy,out _),"legacy store fixture");
        var legacyEngine=new SimulationTransactionEngine(new(default,new SimulationTimeline(4)),new SimulationState(spacecraft:legacy));
        Check(legacyEngine.BeginConstructionServices(raw.Binding,8,out _)==ConstructionServiceStatus.InvalidInput,"generic services cannot bind legacy slot");
        raw.Engine.RetireConstructionServices(raw.Authority);
    }
}
