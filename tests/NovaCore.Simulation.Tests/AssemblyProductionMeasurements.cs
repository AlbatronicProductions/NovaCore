using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Transactions;

// Measurement-only fixtures call production entry points. No numerical or resource
// implementation is duplicated here. Setup, serialization and reporting are cold.
internal static class AssemblyProductionMeasurements
{
    private const int Warm = 128, Samples = 1024;
    private const long IntervalTicks = 15625;
    private const string StockId = "novacore.stock.SRV01.FourHorn";
    private static readonly AssemblyCommand Concurrent = new(true,"+ROLL",.05,.05,IntervalTicks);
    private readonly record struct Population(string Name,CompiledAssemblyDesign Design,AssemblyCommand Command,
        PropellantClassification Expected,int StepsPerEpisode=128);
    private readonly record struct Collections(int Gen0,int Gen1,int Gen2)
    {
        internal static Collections Capture()=>new(GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2));
        internal static Collections Delta(Collections before)
        {var after=Capture();return new(after.Gen0-before.Gen0,after.Gen1-before.Gen1,after.Gen2-before.Gen2);}
    }
    private static void Check(bool value,string why)
    {if(!value)throw new InvalidOperationException("Assembly measurement: "+why);}
    private static CompiledAssemblyDesign Stock()=>AssemblyStockCatalog.LoadDefault().Resolve(StockId);
    private static CompiledAssemblyDesign Load(CompiledAssemblyDesign source,double fuel,double oxidizer)
        =>CompiledAssemblyDesign.Compile(AssemblyJson.Write(source.Data with {Design=source.Data.Design with {InitialFuelKg=fuel,InitialOxidizerKg=oxidizer}}));
    private static Population[] Populations(CompiledAssemblyDesign stock)=>[
        new("B-no-demand",stock,new(false,null,0,0,IntervalTicks),PropellantClassification.NoDemand),
        new("B-main-only",stock,new(true,null,0,0,IntervalTicks),PropellantClassification.FullPowered),
        new("B-RCS-pair-only",stock,new(false,"+ROLL",0,0,IntervalTicks),PropellantClassification.FullPowered),
        new("B-concurrent-main-RCS-gimbal",stock,Concurrent,PropellantClassification.FullPowered),
        new("B-exhausted-no-feed",Load(stock,0,0),Concurrent,PropellantClassification.NoFeed),
        new("B-endpoint-exhaustion",Load(stock,5d/4096,15d/8192),new(true,null,0,0,IntervalTicks),PropellantClassification.EndpointExhaustion,1),
        new("B-interior-exhaustion",Load(stock,43d/65536,129d/131072),Concurrent,PropellantClassification.InteriorExhaustion,1)
    ];
    private static AssemblyApplicationSession Create(CompiledAssemblyDesign design,AssemblyCommand command,int count,string identity)
    {
        var plan=new AssemblyCommand[count];Array.Fill(plan,command);
        var launch=new AssemblyLaunch(design,new SpacecraftDefinition(new(201),new(1),new(2),"assembly measurement"),identity,
            new(Double3.Zero,Double3.Zero,DoubleQuaternion.Identity,Double3.Zero),default,plan);
        return AssemblyApplicationSession.Create(launch,count);
    }
    private static AssemblyApplicationSession[] Episodes(Population p,int operations,string label)
    {
        Check(operations%p.StepsPerEpisode==0,"whole bounded measurement episodes");
        var episodes=new AssemblyApplicationSession[operations/p.StepsPerEpisode];
        for(var i=0;i<episodes.Length;i++)episodes[i]=Create(p.Design,p.Command,p.StepsPerEpisode,label+"_"+i);
        return episodes;
    }
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {
        Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"copied canonical observation");return o;
    }
    private static void Complete(AssemblyApplicationSession s)
    {
        var before=Observe(s);var ticks=s.Launch.Plan[before.State.Frontier].Request.Ticks;
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"host admission");
        var result=s.Engine.ServiceAssemblyFlightDebt(s.Authority);
        Check(result.PublishedCount==1&&result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"one integrated publication");
        var after=Observe(s);
        Check(after.HistoryCount==before.HistoryCount+1&&after.Clock.Debt.Ticks==0,"history and debt successor");
    }
    private static void AssertClassification(AssemblyApplicationSession s,PropellantClassification expected)
    {
        var o=Observe(s);
        Check(s.Engine.TryGetAssemblyHistory(s.Authority,o.HistoryCount-1,out var record)&&record.Classification==expected,"population classification");
    }
    private static void Fund(AssemblyApplicationSession s,int intervals=1)
    {Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(IntervalTicks*intervals)).Status==AssemblyFlightStatus.AcceptedCredit,"pre-fund outside split boundary");}
    private static AssemblyFlightProposal Prepare(AssemblyApplicationSession s)
    {Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var p)==AssemblyFlightStatus.Prepared,"production preparation");return p;}
    private static void Publish(AssemblyApplicationSession s,AssemblyFlightProposal p)
    {
        Check(s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.Published,"production publication");
        _=Observe(s);
    }

    internal static void Allocation()
    {
        var stock=Stock();var windows=0;
        foreach(var p in Populations(stock))
        {
            var warm=Episodes(p,Warm,"allocation_warm");var measured=Episodes(p,Warm,"allocation_measured");
            for(var i=0;i<Warm;i++)Complete(warm[i/p.StepsPerEpisode]);
            using(var gate=new OrdinaryAllocationMeasurement(p.Name))
            {
                for(var i=0;i<Warm;i++)Complete(measured[i/p.StepsPerEpisode]);
                OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),p.Name);
            }
            foreach(var episode in measured)AssertClassification(episode,p.Expected);
            windows++;GC.KeepAlive(warm);GC.KeepAlive(measured);
        }
        // C/D use fresh equivalent mutable fixtures. Their preparation and fixed
        // publication are measured independently under the unchanged helper.
        foreach(var p in Populations(stock).Where(p=>p.Name is "B-concurrent-main-RCS-gimbal" or "B-interior-exhaustion"))
        {
            var single=p with {StepsPerEpisode=1};
            var warm=Episodes(single,Warm,"split_warm");var measured=Episodes(single,Warm,"split_measured");
            foreach(var s in warm){Fund(s);Publish(s,Prepare(s));}
            foreach(var s in measured)Fund(s);
            var proposals=new AssemblyFlightProposal[Warm];
            using(var gate=new OrdinaryAllocationMeasurement("C-preparation-"+p.Name))
            {
                for(var i=0;i<Warm;i++)proposals[i]=Prepare(measured[i]);
                OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),"assembly preparation");
            }
            using(var gate=new OrdinaryAllocationMeasurement("D-publication-"+p.Name))
            {
                for(var i=0;i<Warm;i++)Publish(measured[i],proposals[i]);
                OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),"assembly publication and copied observation");
            }
            foreach(var s in measured)AssertClassification(s,p.Expected);
            windows+=2;
        }
        var idle=Create(stock,Concurrent,128,"idle_refusals");
        for(var i=0;i<Warm;i++)NoWorkAndRefusal(idle);
        using(var gate=new OrdinaryAllocationMeasurement("assembly-no-work-refusal"))
        {
            for(var i=0;i<Samples;i++)NoWorkAndRefusal(idle);
            OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),"assembly no work/refusal");
        }
        windows++;
        var backlogWarm=Create(stock,Concurrent,128,"backlog_warm");
        var backlog=Create(stock,Concurrent,128,"backlog_measured");Fund(backlogWarm,128);Fund(backlog,128);
        for(var i=0;i<32;i++)Four(backlogWarm);
        using(var gate=new OrdinaryAllocationMeasurement("assembly-four-interval-backlog"))
        {
            for(var i=0;i<32;i++)Four(backlog);
            OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),"assembly bounded debt service");
        }
        windows++;
        Check(Observe(backlog).State.Frontier==128&&Observe(backlog).Clock.Debt.Ticks==0,"backlog drained exactly");
        var creditFull=Create(stock,Concurrent,128,"credit_capacity");
        for(var i=0;i<SimulationTransactionEngine.AssemblyHostCreditCapacity;i++)
            Check(creditFull.Engine.AdmitAssemblyHostTime(creditFull.Authority,i+1,new(1)).Status==AssemblyFlightStatus.AcceptedCredit,"fill bounded credit trace");
        void RefuseCredit()=>Check(creditFull.Engine.AdmitAssemblyHostTime(creditFull.Authority,SimulationTransactionEngine.AssemblyHostCreditCapacity+1,new(1)).Status==AssemblyFlightStatus.HistoryCapacity,"full credit trace refusal");
        for(var i=0;i<Warm;i++)RefuseCredit();
        using(var gate=new OrdinaryAllocationMeasurement("assembly-credit-capacity-refusal"))
        {
            for(var i=0;i<Samples;i++)RefuseCredit();
            OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),"assembly credit capacity refusal");
        }
        windows++;
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("ASSEMBLY_ALLOCATION "+JsonSerializer.Serialize(new {windows,bytes=0,positiveControl="nonzero required",policy="checked existing helper; no tolerance, subtraction or retry"}));
    }
    private static void NoWorkAndRefusal(AssemblyApplicationSession s)
    {
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status==AssemblyFlightStatus.AwaitingDebt,"idle debt");
        Check(s.Engine.PublishAssemblyFlight(s.Authority,default).Status==AssemblyFlightStatus.InvalidProposal,"invalid publication lease");
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(-1)).Status==AssemblyFlightStatus.InvalidInput,"negative host refusal");
    }
    private static void Four(AssemblyApplicationSession s)
    {
        var result=s.Engine.ServiceAssemblyFlightDebt(s.Authority);
        Check(result.PublishedCount==4&&result.Status is AssemblyFlightStatus.BudgetExhausted or AssemblyFlightStatus.Completed,"four-interval service cap");
        _=Observe(s);
    }
    private sealed class Series(string boundary,string population,string fingerprint,int work,bool coldAllocation=false,int? rkStages=null)
    {
        internal readonly double[] Milliseconds=new double[Samples];
        internal readonly long[] CounterDeltas=new long[Samples];
        internal readonly Collections[] Gc=new Collections[Samples];
        internal void Report()
        {
            var sorted=(double[])Milliseconds.Clone();Array.Sort(sorted);
            var tails=Enumerable.Range(0,Samples).OrderByDescending(i=>Milliseconds[i]).Take(8)
                .Select(i=>new {index=i,milliseconds=Milliseconds[i],collections=Gc[i],allocationCounterDelta=CounterDeltas[i]}).ToArray();
            Console.WriteLine("ASSEMBLY_COST "+JsonSerializer.Serialize(new {
                boundary,population,fingerprint,pid=Environment.ProcessId,runtime=Environment.Version.ToString(),warm=Warm,samples=Samples,
                medianMs=(sorted[511]+sorted[512])/2,p95Ms=sorted[972],p99Ms=sorted[1013],maxMs=sorted[1023],
                percentilePolicy="middle-pair median; nearest-rank P95/P99",tails,
                gen0DuringSamples=Gc.Sum(x=>x.Gen0),gen1DuringSamples=Gc.Sum(x=>x.Gen1),gen2DuringSamples=Gc.Sum(x=>x.Gen2),
                allocationCounterDeltaDuringSamples=CounterDeltas.Sum(),allocationPolicy=coldAllocation?"cold save/restore allocations reported; no hot-path zero claim":"separate exact-zero checked windows are authoritative",
                physicalEvaluationIntervalsPerSample=work,rkDerivativeStagesPerSample=rkStages,ownerThreads=1,synchronization="existing owner-thread phase; no waiting lock or worker",
                nativeSolverCalls=0,threshold="none invented; whole-engine policy assessed with application companion",rawMilliseconds=Milliseconds,
                rawAllocationCounterDeltas=CounterDeltas,rawCollections=Gc
            }));
        }
    }
    private static void Timed(Action action,Series s,int index)
    {
        var gc=Collections.Capture();var bytes=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        action();var ticks=Stopwatch.GetTimestamp()-start;
        s.Milliseconds[index]=ticks*1000d/Stopwatch.Frequency;s.CounterDeltas[index]=GC.GetAllocatedBytesForCurrentThread()-bytes;s.Gc[index]=Collections.Delta(gc);
    }
    internal static void Cost()
    {
        // Ordinary runtime timing only: no no-GC region, forced GC or runtime
        // setting changes. Reporting occurs after each complete population.
        var stock=Stock();var baseline=new PoweredFreeFlightTests.AssemblyBaselineMeasurement();
        Action baselineOperation=baseline.Step;
        for(var i=0;i<Warm;i++)baselineOperation();
        var a=new Series("A","existing-powered-free-flight-mixed-burn-coast",
            "banked one-store dry-8 profile; exact 60 Hz; 128 warm then 1024 retained intervals; includes exhaustion; 128 RK4 steps per ordinary exact segment (512 stages, 1024 if two); different physics workload, no median subtraction",1);
        for(var i=0;i<Samples;i++)Timed(baselineOperation,a,i);
        a.Report();GC.KeepAlive(baseline);
        foreach(var p in Populations(stock))
        {
            var warm=Episodes(p,Warm,"cost_warm");var measured=Episodes(p,Samples,"cost_measured");
            // Delegate instances are created cold, not once per timed interval.
            var operations=measured.Select< AssemblyApplicationSession,Action >(s=>()=>Complete(s)).ToArray();
            for(var i=0;i<Warm;i++)Complete(warm[i/p.StepsPerEpisode]);
            var series=new Series("B",p.Name,$"design={p.Design.Digest}; ticks={p.Command.Ticks}; main={p.Command.MainOn}; pair={p.Command.Pair}; gimbal={p.Command.GimbalTargetY:R},{p.Command.GimbalTargetZ:R}; episode={p.StepsPerEpisode}; four RK stages per nonzero powered/coast segment; actual admission appended to fixed host-credit trace",1,rkStages:p.Expected==PropellantClassification.InteriorExhaustion?8:4);
            for(var i=0;i<Samples;i++)Timed(operations[i/p.StepsPerEpisode],series,i);
            foreach(var s in measured)AssertClassification(s,p.Expected);
            series.Report();GC.KeepAlive(warm);GC.KeepAlive(measured);
        }
        foreach(var p in Populations(stock).Where(p=>p.Name is "B-concurrent-main-RCS-gimbal" or "B-interior-exhaustion"))
            SplitCost(p);
        BacklogCost(stock);
        SaveResumeCost();
    }
    private static void SplitCost(Population p)
    {
        var one=p with {StepsPerEpisode=1};var all=Episodes(one,Warm+Samples,"split_cost");
        foreach(var s in all)Fund(s);
        var c=new Series("C",p.Name,"production prepare incl exact resources, current tensor, wrench and physical evaluation; credited source",1,rkStages:p.Expected==PropellantClassification.InteriorExhaustion?8:4);
        var d=new Series("D",p.Name,"production final checks, fixed canonical writes, history, clock/debt, acknowledgement and observation; one committed interval",0,rkStages:0);
        for(var i=0;i<all.Length;i++)
        {
            var s=all[i];var gc=Collections.Capture();var bytes=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
            var proposal=Prepare(s);var ticks=Stopwatch.GetTimestamp()-start;
            if(i>=Warm){var n=i-Warm;c.Milliseconds[n]=ticks*1000d/Stopwatch.Frequency;c.CounterDeltas[n]=GC.GetAllocatedBytesForCurrentThread()-bytes;c.Gc[n]=Collections.Delta(gc);}
            gc=Collections.Capture();bytes=GC.GetAllocatedBytesForCurrentThread();start=Stopwatch.GetTimestamp();
            Publish(s,proposal);ticks=Stopwatch.GetTimestamp()-start;
            if(i>=Warm){var n=i-Warm;d.Milliseconds[n]=ticks*1000d/Stopwatch.Frequency;d.CounterDeltas[n]=GC.GetAllocatedBytesForCurrentThread()-bytes;d.Gc[n]=Collections.Delta(gc);}
            AssertClassification(s,p.Expected);
        }
        c.Report();d.Report();GC.KeepAlive(all);
    }
    private static void BacklogCost(CompiledAssemblyDesign stock)
    {
        const int perEpisode=32;
        var all=new AssemblyApplicationSession[(Warm+Samples)/perEpisode];
        for(var i=0;i<all.Length;i++){all[i]=Create(stock,Concurrent,128,"backlog_cost_"+i);Fund(all[i],128);}
        var operations=all.Select<AssemblyApplicationSession,Action>(s=>()=>Four(s)).ToArray();
        for(var i=0;i<Warm;i++)operations[i/perEpisode]();
        var series=new Series("B-backlog","concurrent-main-RCS-four-interval-service","four qualified 15625-tick intervals; one copied observation; no render multiplication; prefunded debt",4,rkStages:16);
        for(var i=0;i<Samples;i++)Timed(operations[(Warm+i)/perEpisode],series,i);
        foreach(var s in all)Check(Observe(s).State.Frontier==128&&Observe(s).Clock.Debt.Ticks==0,"bounded backlog population complete");
        series.Report();GC.KeepAlive(all);
    }
    private static void SaveResumeCost()
    {
        var catalog=AssemblyStockCatalog.LoadDefault();var stock=catalog.Resolve(StockId);
        var source=Create(stock,Concurrent,128,"save_cost");for(var i=0;i<64;i++)Complete(source);
        var expected=Observe(source);var bytes=source.Save();
        var save=new Series("S-save","explicit-request-at-frontier64","runtime/2 capture canonical state, history digest, actual host-credit trace and serialize; cold request, 64 publications and 64 credits",0,true,0);
        var restore=new Series("S-restore","explicit-request-at-frontier64","runtime/2 validate pinned design, construct owner at saved rate, replay 64 actual host admissions and physical publications, verify current resource/state/frontier; no aggregate credit or clock override",64,true,256);
        var resume=new Series("S-resumed-interval","concurrent-main-RCS-frontier65","normal application operation after verified restore; 15625 ticks",1,rkStages:4);
        for(var i=-Warm;i<Samples;i++)
        {
            var gc=Collections.Capture();var allocated=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
            var saved=source.Save();var ticks=Stopwatch.GetTimestamp()-start;
            if(i>=0){save.Milliseconds[i]=ticks*1000d/Stopwatch.Frequency;save.CounterDeltas[i]=GC.GetAllocatedBytesForCurrentThread()-allocated;save.Gc[i]=Collections.Delta(gc);}
            Check(saved.AsSpan().SequenceEqual(bytes),"repeated canonical save bytes");
            gc=Collections.Capture();allocated=GC.GetAllocatedBytesForCurrentThread();start=Stopwatch.GetTimestamp();
            var restored=AssemblyApplicationSession.Restore(catalog,bytes);ticks=Stopwatch.GetTimestamp()-start;
            if(i>=0){restore.Milliseconds[i]=ticks*1000d/Stopwatch.Frequency;restore.CounterDeltas[i]=GC.GetAllocatedBytesForCurrentThread()-allocated;restore.Gc[i]=Collections.Delta(gc);}
            Check(Observe(restored)==expected,"restore exact source identity/resources/frontier");
            gc=Collections.Capture();allocated=GC.GetAllocatedBytesForCurrentThread();start=Stopwatch.GetTimestamp();
            Complete(restored);ticks=Stopwatch.GetTimestamp()-start;
            if(i>=0){resume.Milliseconds[i]=ticks*1000d/Stopwatch.Frequency;resume.CounterDeltas[i]=GC.GetAllocatedBytesForCurrentThread()-allocated;resume.Gc[i]=Collections.Delta(gc);}
            Check(Observe(restored).State.Frontier==65,"one resumed interval");GC.KeepAlive(restored);
        }
        save.Report();restore.Report();resume.Report();
        Console.WriteLine("ASSEMBLY_SAVE_STORAGE "+JsonSerializer.Serialize(new {frontier=64,hostCredits=64,schema="novacore.assembly-runtime/2",saveBytes=bytes.Length,refill=false,authority="canonical state plus pinned immutable launch data and replay of actual host admission and physical publication"}));
        GC.KeepAlive(source);GC.KeepAlive(catalog);
    }

    internal static void Storage()
    {
        // Cold traffic is an upper bound, not a live-heap measurement. Forced-GC
        // deltas are explicitly estimates, outside all ordinary timing windows.
        var warmed=AssemblyStockCatalog.LoadDefault();GC.KeepAlive(warmed);
        var heapBefore=GC.GetTotalMemory(true);var before=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        var catalog=AssemblyStockCatalog.LoadDefault();var design=catalog.Resolve(StockId);
        var catalogMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var catalogTraffic=GC.GetAllocatedBytesForCurrentThread()-before;
        var catalogHeap=GC.GetTotalMemory(true)-heapBefore;
        var designBytes=design.Save();
        before=GC.GetAllocatedBytesForCurrentThread();start=Stopwatch.GetTimestamp();
        var decoded=CompiledAssemblyDesign.Compile(designBytes);
        var compileMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var compileTraffic=GC.GetAllocatedBytesForCurrentThread()-before;
        // Use a warmed creation body before measuring a new live instance.
        var warm=Create(design,Concurrent,128,"storage_warm");Complete(warm);
        var runtimeHeapBefore=GC.GetTotalMemory(true);before=GC.GetAllocatedBytesForCurrentThread();start=Stopwatch.GetTimestamp();
        var session=Create(design,Concurrent,128,"storage_measured");
        var createMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var runtimeTraffic=GC.GetAllocatedBytesForCurrentThread()-before;
        var runtimeHeap=GC.GetTotalMemory(true)-runtimeHeapBefore;
        for(var i=0;i<128;i++)Complete(session);
        var endHeap=GC.GetTotalMemory(true)-runtimeHeapBefore;
        var record=Unsafe.SizeOf<AssemblyFlightRecord>();var history=24L+128L*record;
        var creditRecord=Unsafe.SizeOf<AssemblyHostCredit>();var creditCapacity=SimulationTransactionEngine.AssemblyHostCreditCapacity;
        var creditTrace=24L+(long)creditCapacity*creditRecord;
        var keys=session.Launch.PartKeys.Concat(session.Launch.CapabilityKeys).Concat(session.Launch.StoreKeys).ToArray();
        var keyUtf16Bytes=keys.Sum(k=>2L*k.Length);var keyUtf8Bytes=keys.Sum(k=>(long)Encoding.UTF8.GetByteCount(k));
        var saveBytes=session.Save();
        Check(runtimeTraffic>=history+creditTrace,"cold owner traffic includes fixed history and host-credit storage");
        Check(Observe(session).HistoryCount==128,"history capacity witnessed at end");
        Console.WriteLine("ASSEMBLY_STORAGE "+JsonSerializer.Serialize(new {
            definitions=design.Data.Definitions.Length,instances=design.Parts.Length,stores=2,consumers=design.Jets.Length+1,
            catalogAndCompiledStock=new {coldMs=catalogMs,coldManagedTraffic=catalogTraffic,retainedHeapDeltaEstimate=catalogHeap,includes="shared immutable definitions, compiled stock, parsing transients"},
            standaloneCompile=new {coldMs=compileMs,coldManagedTraffic=compileTraffic,serializedDesignBytes=designBytes.Length},
            runtime=new {coldMs=createMs,coldManagedTraffic=runtimeTraffic,retainedHeapDeltaEstimate=runtimeHeap,endRetainedHeapDeltaEstimate=endHeap,
                managedConservativeRetentionUpperBound=runtimeTraffic,includes="launch, identities, plan, common state store, clock, owner, proposal scratch, history and fixed host-credit trace"},
            inlineSizes=new {state=Unsafe.SizeOf<AssemblyRuntimeState>(),record,hostCredit=creditRecord,stores=Unsafe.SizeOf<AssemblyStores>(),mass=Unsafe.SizeOf<AssemblyMass>(),proposal=Unsafe.SizeOf<AssemblyFlightProposal>(),compiledCommand=Unsafe.SizeOf<CompiledAssemblyCommand>()},
            history=new {capacity=128,bytesIncluded=history,recordContainsReferences=true,referencedStrings="shared immutable launch/design; inline sizeof is not a whole object graph"},
            hostCredits=new {capacity=creditCapacity,count=Observe(session).HostSequence,bytesIncluded=creditTrace,sequenceAuthority="sequence derives from trace count; each record contains actual host ticks and frontier"},
            runtimeIdentity=new {launch=session.Launch.LaunchId,partKeys=session.Launch.PartKeys.Length,capabilityKeys=session.Launch.CapabilityKeys.Length,storeKeys=session.Launch.StoreKeys.Length,
                keyUtf16PayloadBytes=keyUtf16Bytes,keyUtf8PayloadBytes=keyUtf8Bytes,backingReferenceArrayBytesIncluded=3L*24+keys.Length*IntPtr.Size,
                note="namespace is recorded launch ID / design-local instance / capability or store; string object headers excluded from payload figures, included in whole runtime bound"},
            compiledPlan=new {commands=128,arrayBytesIncluded=24L+128L*Unsafe.SizeOf<CompiledAssemblyCommand>()},serializedRuntimeSaveBytes=saveBytes.Length,
            registry="application session/stock lifetimes; no measurement cache",nativeRetainedBytes=0,
            scope="cold allocation traffic is a conservative bound including transients; forced-GC heap deltas are estimates; history and host-credit trace are included, not additional totals",
            historicalSingleCraft8MiBComparison=runtimeTraffic<8*1024*1024,notANewSubsystemBudget=true
        }));
        GC.KeepAlive(catalog);GC.KeepAlive(decoded);GC.KeepAlive(session);GC.KeepAlive(warm);GC.KeepAlive(warmed);
    }
}

// Reuses the unchanged banked fixture and operation, without copying its owner or
// changing any existing regression. This comparison is intentionally labelled as
// a different one-store 60 Hz physics population, not an SRV net-cost subtraction.
internal static partial class PoweredFreeFlightTests
{
    internal sealed class AssemblyBaselineMeasurement
    {
        private readonly Flight _flight=new(mountY:.005,omegaZ:.1);
        internal void Step()=>CompleteOperation(_flight);
    }
}
