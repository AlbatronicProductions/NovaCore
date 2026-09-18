using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed record AssemblySaveData(string Schema,string DesignId,string DesignDigest,SpacecraftDefinition Spacecraft,string LaunchId,
    long OriginTicks,AssemblyMotion InitialMotion,AssemblyGimbal InitialGimbal,AssemblyCommand[] Plan,int HistoryCapacity,
    ulong InitialRevision,AssemblyRuntimeState Current,ulong StateRevision,string HistoryDigest,long DebtTicks,
    long RateNumerator,long RateDenominator,long RateRemainder,long HostSequence,AssemblyHostCredit[] Credits,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] AssemblyControlSave? Live=null);

internal sealed partial class SimulationTransactionEngine
{
    /// <summary>Cold save. Definitions remain catalog authority; no live lease or callback is serialized.</summary>
    internal byte[] SaveAssemblyFlight(AssemblyFlightAuthority authority)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)throw new InvalidOperationException("Save outside owner phase.");
        try
        {
            var p=_assemblyFlight;
            if(p is null||!ReferenceEquals(p.Authority,authority)||p.Control?.Retired==true||authority.Launch.Consumer!=AssemblyPhysicalConsumer.FreeFlight||authority.Launch.Design.Development is not null||authority.Launch.GravityRoot!=NovaCore.Core.Double3.Zero||p.Active||_clock.Timeline.Revision.Value!=0||_clock.Timeline.PendingCount!=0)
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
            AssemblyControlSave? live=null;
            var demandOnly=false;
            if(p.Control is {} c)
            {
                var admissions=c.Journal.AsSpan(0,c.Count).ToArray();
                demandOnly=admissions.Any(a=>a.Requested.PilotOnly||a.Requested.Pilot!=default);
                live=new(c.Authority.Identity.Generation,c.Journal.Length,admissions,AssemblyJson.Digest(admissions),c.Allocation is null?AssemblyControlExecution.DemandOnly:AssemblyControlExecution.PhysicalActuators);
            }
            // v4 explicitly records demand-only execution. A later allocator must
            // never reinterpret these journals as physically allocated requests.
            return AssemblyJson.Write(new AssemblySaveData(live is null?"novacore.assembly-runtime/2":live.Execution==AssemblyControlExecution.PhysicalActuators?"novacore.assembly-runtime/5":demandOnly?"novacore.assembly-runtime/4":"novacore.assembly-runtime/3",launch.Design.Data.Design.Id,launch.Design.Digest,
                launch.Spacecraft,launch.LaunchId,launch.Initial.Epoch.Ticks,launch.Initial.Motion,launch.Initial.Gimbal,commands,p.History.Length,
                p.InitialRevision.Value,state,view.Revision.Value,AssemblyJson.Digest(p.History.AsSpan(0,p.Count).ToArray()),_clock.PendingSimulationDebt.Ticks,
                _clock.Rate.Numerator,_clock.Rate.Denominator,_clock.RateRemainder,p.CanonicalHostSequence,p.Credits.AsSpan(0,p.CreditCount).ToArray(),live));
        }
        finally{_clock.PublicationPhase.Exit();}
    }

    internal static AssemblyApplicationSession RestoreAssemblyFlight(AssemblyStockCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var s=AssemblyJson.Read<AssemblySaveData>(bytes,1_048_576);
        if((s.Schema!="novacore.assembly-runtime/2"&&s.Schema!="novacore.assembly-runtime/3"&&s.Schema!="novacore.assembly-runtime/4"&&s.Schema!="novacore.assembly-runtime/5")||
            (s.Schema!="novacore.assembly-runtime/2")!=(s.Live is not null)||s.HistoryCapacity is <1 or >128||s.Plan is null||s.Plan.Length is <1 or >128||
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
        if(s.Live is {} live)
        {
            if(live.Generation!=1||live.Capacity is <1 or >AssemblyControlCapacity||live.Admissions is null||
                live.Admissions.Length>live.Capacity||AssemblyJson.Digest(live.Admissions)!=live.Digest||!Enum.IsDefined(live.Execution)||
                (s.Schema=="novacore.assembly-runtime/5")!=(live.Execution==AssemblyControlExecution.PhysicalActuators)||
                s.Schema=="novacore.assembly-runtime/3"&&live.Admissions.Any(a=>a.Requested.PilotOnly||a.Requested.Pilot!=default||a.Effective.Pilot!=default))
                throw new InvalidDataException("Invalid live command journal.");
            session.EnableLiveControl(live.Capacity,live.Execution);
            var creditIndex=0;var admissionIndex=0;var frontier=0;
            while(creditIndex<s.Credits.Length||admissionIndex<live.Admissions.Length||frontier<s.Current.Frontier)
            {
                while(admissionIndex<live.Admissions.Length && live.Admissions[admissionIndex].Frontier==frontier &&
                    live.Admissions[admissionIndex].HostSequence==creditIndex)
                {
                    var admission=live.Admissions[admissionIndex];
                    var result=engine.AdmitAssemblyControl(session.Control!,admission.Identity,admission.Sequence,admission.Requested);
                    if(result.Status!=AssemblyControlStatus.Admitted||result.Admission!=admission)
                        throw new InvalidDataException("Saved control admission refused.");
                    admissionIndex++;
                }
                if(creditIndex<s.Credits.Length&&s.Credits[creditIndex].Frontier==frontier)
                {
                    var credit=s.Credits[creditIndex];
                    if(credit.HostTicks<=0||engine.AdmitAssemblyHostTime(session.Authority,creditIndex+1,new(credit.HostTicks)).Status!=AssemblyFlightStatus.AcceptedCredit)
                        throw new InvalidDataException("Saved live host credit refused.");
                    creditIndex++;continue;
                }
                if(frontier<s.Current.Frontier)
                {
                    if(engine.PrepareAssemblyFlight(session.Authority,out var proposal)!=AssemblyFlightStatus.Prepared||
                        engine.PublishAssemblyFlight(session.Authority,proposal).Status!=AssemblyFlightStatus.Published)
                        throw new InvalidDataException("Saved live prefix replay refused.");
                    frontier++;continue;
                }
                if(creditIndex!=s.Credits.Length||admissionIndex!=live.Admissions.Length)
                    throw new InvalidDataException("Saved live event ordering refused.");
            }
            VerifyRestoredAssembly(s,engine);
            return session;
        }
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
        VerifyRestoredAssembly(s,engine);
        return session;
    }
    private static void VerifyRestoredAssembly(AssemblySaveData s,SimulationTransactionEngine engine)
    {
        var p=engine._assemblyFlight!;
        if(p.ExpectedState!=s.Current||AssemblyJson.Digest(p.History.AsSpan(0,p.Count).ToArray())!=s.HistoryDigest||
            p.ExpectedClock.Debt.Ticks!=s.DebtTicks||p.ExpectedClock.RateRemainder!=s.RateRemainder||p.CanonicalHostSequence!=s.HostSequence)
            throw new InvalidDataException("Runtime state/history does not reproduce.");
    }
}
