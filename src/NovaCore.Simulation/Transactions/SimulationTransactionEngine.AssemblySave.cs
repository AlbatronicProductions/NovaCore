using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed record AssemblySaveData(string Schema,string DesignId,string DesignDigest,SpacecraftDefinition Spacecraft,string LaunchId,
    long OriginTicks,AssemblyMotion InitialMotion,AssemblyGimbal InitialGimbal,AssemblyCommand[] Plan,int HistoryCapacity,
    ulong InitialRevision,AssemblyRuntimeState Current,ulong StateRevision,string HistoryDigest,long DebtTicks,
    long RateNumerator,long RateDenominator,long RateRemainder,long HostSequence,AssemblyHostCredit[] Credits);

internal sealed partial class SimulationTransactionEngine
{
    /// <summary>Cold save. Definitions remain catalog authority; no live lease or callback is serialized.</summary>
    internal byte[] SaveAssemblyFlight(AssemblyFlightAuthority authority)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)throw new InvalidOperationException("Save outside owner phase.");
        try
        {
            var p=_assemblyFlight;
            if(p is null||!ReferenceEquals(p.Authority,authority)||authority.Launch.Consumer!=AssemblyPhysicalConsumer.FreeFlight||authority.Launch.Design.Development is not null||authority.Launch.GravityRoot!=NovaCore.Core.Double3.Zero||p.Active||_clock.Timeline.Revision.Value!=0||_clock.Timeline.PendingCount!=0)
                throw new InvalidDataException("Unsaveable assembly authority/proposal/timeline.");
            if(!p.Invalidated&&CheckAssemblySource(authority)!=AssemblyFlightStatus.Ready)throw new InvalidDataException("Stale save source.");
            var view=_state.CreateView();
            if(CaptureContinuationClock()!=p.ExpectedClock||view.Revision!=p.ExpectedRevision)throw new InvalidDataException("Canonical state changed outside assembly publication.");
            if(!view.Spacecraft.TryGetAssembly(authority.Launch.Spacecraft.Id,out var launch,out var state)||!ReferenceEquals(launch,authority.Launch)||
                _clock.CurrentTime!=state.Epoch||_clock.IsPaused||_clock.Rate!=p.ExpectedClock.Rate||state.Frontier!=p.Count||view.Revision.Value!=checked(p.InitialRevision.Value+(ulong)p.Count))
                throw new InvalidDataException("Unsaveable canonical state.");
            // Even a terminal private acknowledgement fault must match committed history.
            if(state!=(p.Count==0?launch!.Initial:p.History[p.Count-1].Successor))throw new InvalidDataException("History/current-state mismatch.");
            var commands=launch!.Plan.Select(c=>c.Request).ToArray();
            return AssemblyJson.Write(new AssemblySaveData("novacore.assembly-runtime/2",launch.Design.Data.Design.Id,launch.Design.Digest,
                launch.Spacecraft,launch.LaunchId,launch.Initial.Epoch.Ticks,launch.Initial.Motion,launch.Initial.Gimbal,commands,p.History.Length,
                p.InitialRevision.Value,state,view.Revision.Value,AssemblyJson.Digest(p.History.AsSpan(0,p.Count).ToArray()),_clock.PendingSimulationDebt.Ticks,
                _clock.Rate.Numerator,_clock.Rate.Denominator,_clock.RateRemainder,p.CanonicalHostSequence,p.Credits.AsSpan(0,p.CreditCount).ToArray()));
        }
        finally{_clock.PublicationPhase.Exit();}
    }

    internal static AssemblyApplicationSession RestoreAssemblyFlight(AssemblyStockCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var s=AssemblyJson.Read<AssemblySaveData>(bytes,1_048_576);
        if(s.Schema!="novacore.assembly-runtime/2"||s.HistoryCapacity is <1 or >128||s.Plan is null||s.Plan.Length is <1 or >128||
            s.Credits is null||s.Credits.Length>AssemblyHostCreditCapacity||s.HostSequence!=s.Credits.Length||
            s.Current.Frontier<0||s.Current.Frontier>s.Plan.Length||s.Current.Frontier>s.HistoryCapacity||s.DebtTicks<0||s.HostSequence<0||
            s.RateNumerator<=0||s.RateDenominator<=0||s.RateRemainder<0||s.RateRemainder>=s.RateDenominator||
            s.StateRevision!=checked(s.InitialRevision+(ulong)s.Current.Frontier)||(Int128)s.Current.Epoch.Ticks+s.DebtTicks>long.MaxValue)
            throw new InvalidDataException("Unsupported runtime snapshot.");
        var d=catalog.Resolve(s.DesignId);if(d.Digest!=s.DesignDigest)throw new InvalidDataException("Runtime design digest mismatch.");
        var rate=new SimulationRate(s.RateNumerator,s.RateDenominator);
        if(rate.Numerator!=s.RateNumerator||rate.Denominator!=s.RateDenominator)throw new InvalidDataException("Noncanonical rate.");
        var launch=new AssemblyLaunch(d,s.Spacecraft,s.LaunchId,s.InitialMotion,s.InitialGimbal,s.Plan,new(s.OriginTicks));
        // Replay through the actual canonical transaction owner, never a second evaluator/owner.
        var session=AssemblyApplicationSession.Create(launch,s.HistoryCapacity,rate,new(s.InitialRevision));
        var engine=session.Engine;
        var replayed=0;
        void PublishThrough(int frontier)
        {
            if(frontier<replayed||frontier>s.Current.Frontier)throw new InvalidDataException("Invalid saved credit frontier.");
            while(replayed<frontier)
            {
                if(engine.PrepareAssemblyFlight(session.Authority,out var proposal)!=AssemblyFlightStatus.Prepared||
                    engine.PublishAssemblyFlight(session.Authority,proposal).Status!=AssemblyFlightStatus.Published)
                    throw new InvalidDataException("Saved prefix replay refused.");
                replayed++;
            }
        }
        for(var i=0;i<s.Credits.Length;i++)
        {
            var credit=s.Credits[i];if(credit.HostTicks<=0)throw new InvalidDataException("Invalid saved host duration.");
            PublishThrough(credit.Frontier);
            if(engine.AdmitAssemblyHostTime(session.Authority,i+1,new(credit.HostTicks)).Status!=AssemblyFlightStatus.AcceptedCredit)
                throw new InvalidDataException("Saved host credit replay refused.");
        }
        PublishThrough(s.Current.Frontier);
        var p=engine._assemblyFlight!;
        if(p.ExpectedState!=s.Current||AssemblyJson.Digest(p.History.AsSpan(0,p.Count).ToArray())!=s.HistoryDigest||
            p.ExpectedClock.Debt.Ticks!=s.DebtTicks||p.ExpectedClock.RateRemainder!=s.RateRemainder||p.CanonicalHostSequence!=s.HostSequence)
            throw new InvalidDataException("Runtime state/history does not reproduce.");
        return session;
    }
}
