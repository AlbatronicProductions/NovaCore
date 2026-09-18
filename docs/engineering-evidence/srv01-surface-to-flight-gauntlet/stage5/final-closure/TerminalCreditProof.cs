using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyFloridaSiteTests
{
    internal static void TerminalCreditProof()
    {
        using var s=NovaCore.Simulation.Spacecraft.Assemblies.AssemblyApplicationSession.CreateSupported(Launch(Site()));
        var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
        var first=world.AssemblyIdentityForTest;
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(20_004_094)).Status==AssemblyFlightStatus.AcceptedCredit,"exact episode plus4094 credit");
        var published=0;AssemblyFlightResult service;
        do{service=s.Engine.ServiceAssemblyContactDebt(s.Authority);published+=service.PublishedCount;Check(service.PublishedCount<=4,"bounded service");}
        while(service.Status==AssemblyFlightStatus.BudgetExhausted);
        var v=Observe(s);var id=world.AssemblyIdentityForTest;
        Check(service.Status==AssemblyFlightStatus.Completed&&published==1200&&v.State.Frontier==1200&&v.State.Epoch.Ticks==20_000_000&&v.Clock.Time.Ticks==20_000_000,"all required simulation published");
        Check(v.Clock.Debt.Ticks==4094&&v.StateRevision.Value==1200&&v.TimelineRevision.Value==0&&v.HistoryCount==1200,"conserved excess debt and exact canonical identity");
        Check(v.Consumer==AssemblyPhysicalConsumer.SupportedContact&&!v.PrivateInvalidated&&!id.Pending&&!id.Invalidated&&id.Frontier==1200&&id.Generation==first.Generation&&id.Body==first.Body&&id.Shape==first.Shape,"held terminal native owner, no unpublished endpoint");
        for(var i=0;i<4;i++)
        {
            var next=s.Engine.ServiceAssemblyContactDebt(s.Authority);
            Check(next.Status==AssemblyFlightStatus.Completed&&next.PublishedCount==0&&Observe(s)==v&&world.AssemblyIdentityForTest==id,"repeated terminal service changes nothing");
        }
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,v.HostSequence+1,new(100)).Status==AssemblyFlightStatus.Completed&&Observe(s)==v,"terminal credit refuses without discard");
        Console.WriteLine("TERMINAL_CREDIT "+JsonSerializer.Serialize(new{classification="A. LEGITIMATE EXCESS HOST CREDIT AFTER COMPLETE FRONTIER",credit=20_004_094,publishedTicks=v.Clock.Time.Ticks,debt=v.Clock.Debt.Ticks,publications=published,history=v.HistoryCount,pending=id.Pending,invalidated=id.Invalidated,consumer=v.Consumer.ToString(),repeatedService="Completed/0/nonmutating"}));
    }
}
