using NovaCore.Core;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static ContactGenerationFixture;

internal static class ContinuationOwnershipTests
{
    private static void Check(bool condition,string message)=>ContactGenerationFixture.Check(condition,message);
    internal static void Run()
    {
        var state=State(new(1,2,3),new(4,5,6),DoubleQuaternion.Identity,Double3.Zero);
        var timeline=new SimulationTimeline(4);var clock=new SimulationClock(default,timeline);
        var engine=new SimulationTransactionEngine(clock,state,4);
        var borrowed=state.CreateView();
        Check(borrowed.Spacecraft.TryGetTranslation(Craft,out _,out _),"current borrow");
        Throws(()=>_=new SimulationTransactionEngine(clock,state,4),"duplicate engine rejected");
        Throws(()=>_=new SimulationTransactionEngine(new(default,new(4)),state,4),"duplicate clock rejected");
        var phase=timeline.PublicationPhase;
        Check(phase.TryEnter(engine),"exclusive phase entered");
        try
        {
            Throws(()=>clock.Pause(),"clock interleave");
            Throws(()=>timeline.Schedule(default,new(new(1),default,0,SimulationEventKind.Marker)),"timeline interleave");
            Throws(()=>state.CommitMarkerValue(3),"state interleave");
            Throws(()=>engine.ExecuteCanonicalPendingEvent(),"transaction interleave");
            Check(!engine.TryCaptureContinuationObservation(Craft,out _),"nested observation rejected");
            Check(engine.PublishCertifiedContinuation(default,default).Status==ContinuationPublicationStatus.ReentrantPublication,"nested publication rejected");
        }
        finally { phase.Exit(); }
        Check(timeline.Schedule(default,new(new(1),default,0,SimulationEventKind.Marker)).Succeeded,"ordinary event after phase");
        Check(engine.ExecuteCanonicalPendingEvent().Committed,"ordinary commit after phase");
        Check(!borrowed.Spacecraft.TryGetTranslation(Craft,out _,out _),"old borrowed view expires after revision");
        Check(engine.TryCaptureContinuationObservation(Craft,out var copied),"copied observation");
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                Check(!engine.TryCaptureContinuationObservation(Craft,out _),"foreign-thread observation rejected");
                Check(engine.PublishCertifiedContinuation(default,default).Status==ContinuationPublicationStatus.WrongOwnerThread,"foreign-thread publication rejected");
                Throws(()=>clock.Pause(),"foreign-thread clock mutation rejected");
                Check(copied.Translation.PositionRoot==new Double3(1,2,3),"copied values cross threads safely");
            }
            catch(Exception ex) { failure=ex; }
        });thread.Start();thread.Join();if(failure is not null)throw failure;
        Console.WriteLine("PASS continuation ownership: owner thread, exclusive phase, borrowed lifetime, copied observation, single engine");
    }
    internal static void Throws(Action operation,string contract)
    {
        try { operation(); }catch(InvalidOperationException){return;}
        throw new InvalidOperationException(contract+": expected refusal");
    }
}
