using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Transactions;

internal static partial class PoweredFreeFlightTests
{
    private static void CompleteOperation(Flight f)
    {
        var frontier=f.Engine.ObservePoweredFreeFlight(f.Power).Observation.Actuator.Frontier;
        var ticks=(frontier+1)*1_000_000/60-frontier*1_000_000/60;
        var credit=f.Engine.AdmitPoweredHostTime(f.Power,++f.HostSequence,new(ticks));
        if(credit.Status!=PoweredFlightStatus.AcceptedCredit)throw new InvalidOperationException("Complete operation credit failed.");
        var service=f.Engine.ServicePoweredFlightDebt(f.Power);
        if(service.PublishedCount!=1||!service.CanonicalCommitted||service.Status is not(PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.Completed))
            throw new InvalidOperationException("Complete operation physical publication failed.");
    }
    internal static void Schedules()
    {
        foreach(var rotating in new[]{false,true})
        {
            var reference=new Flight(mountY:rotating?.005:0,omegaZ:rotating?.1:0);
            reference.Credit(20_000_000);
            while(reference.Engine.ObservePoweredFreeFlight(reference.Power).Observation.Actuator.Frontier<1200)
            {
                var result=reference.Engine.ServicePoweredFlightDebt(reference.Power);
                Check(result.Status is PoweredFlightStatus.BudgetExhausted or PoweredFlightStatus.Completed,"prefunded reference service");
            }
            foreach(var hz in new[]{30,60,150,240,-1})
            {
                var f=new Flight(mountY:rotating?.005:0,omegaZ:rotating?.1:0);
                long admitted=0;var frames=hz>0?20*hz:400;var compared=0;var shortIntervals=0;var longIntervals=0;var budgetHits=0;
                for(var frame=1;frame<=frames;frame++)
                {
                    // Same exact 20s total. The delayed schedule alternates 100ms and zero-credit drain frames.
                    var target=hz>0?(long)frame*20_000_000/frames:(long)((frame+1)/2)*100_000;
                    if(target>admitted){f.Credit(target-admitted);admitted=target;}
                    var result=f.Engine.ServicePoweredFlightDebt(f.Power);
                    Check(result.Status is PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.BudgetExhausted or PoweredFlightStatus.Completed,"partition service");
                    Check(result.PublishedCount<=4,"bounded display service");
                    if(result.Status==PoweredFlightStatus.BudgetExhausted)budgetHits++;
                    while(compared<result.Observation.HistoryCount)
                    {
                        Check(f.Engine.TryGetPoweredFlightRecord(compared,out var actual)&&reference.Engine.TryGetPoweredFlightRecord(compared,out var expected)&&
                            actual with{DebtBefore=default,DebtAfter=default}==expected with{DebtBefore=default,DebtAfter=default},"physical/resource/actual/provenance history partition identity");
                        Check(actual.DebtBefore.Ticks-actual.DebtAfter.Ticks==actual.Segmentation.Engine.End.Ticks-actual.Segmentation.Engine.Start.Ticks,
                            "partition-specific clock/debt record still conserves exact interval");
                        var delta=actual.Segmentation.Engine.End.Ticks-actual.Segmentation.Engine.Start.Ticks;
                        if(delta==16666)shortIntervals++;else if(delta==16667)longIntervals++;else Check(false,"tick lattice");
                        compared++;
                    }
                    var obs=f.Engine.ObservePoweredFreeFlight(f.Power).Observation;
                    Check(admitted-obs.Clock.Time.Ticks==obs.Clock.Debt.Ticks,"exact accounting conservation at every host frame");
                }
                var final=f.Engine.ObservePoweredFreeFlight(f.Power).Observation;
                Check(compared==1200&&shortIntervals==400&&longIntervals==800&&final.StateRevision.Value==1200&&final.Actuator.ActuatorRevision==1200&&
                    final.TimelineRevision.Value==0&&final.HistoryCount==1200&&final.Clock.Time.Ticks==20_000_000&&final.Clock.Debt.Ticks==0,
                    "complete host-partition lattice/revision/history");
                Check(final.Resource.RemainingUnits.IsZero&&final.Actuator.Activity==ActualEngineActivity.EnabledNoFeed,"long burn exact depletion");
                Console.WriteLine($"POWERED_SCHEDULE {(rotating?"rotating":"straight")} hz={hz} frames={frames} endpoints={compared} short={shortIntervals} long={longIntervals} debt={final.Clock.Debt.Ticks} budget_hits={budgetHits} PASS");
            }
        }
        var shutdown=new Flight();CompleteOperation(shutdown);var before=shutdown.Engine.ObservePoweredFreeFlight(shutdown.Power).Observation;
        shutdown.Send(SpacecraftCommandIntent.Shutdown());CompleteOperation(shutdown);var after=shutdown.Engine.ObservePoweredFreeFlight(shutdown.Power).Observation;
        Check(after.Actuator.Activity==ActualEngineActivity.Off&&after.Resource==before.Resource&&after.Endpoint.VelocityRoot==before.Endpoint.VelocityRoot,
            "shutdown stops force without changing existing velocity/resource");
    }
    internal static void Allocation()
    {
        var powered=new Flight();for(var i=0;i<128;i++)CompleteOperation(powered);
        using(var m=new OrdinaryAllocationMeasurement("powered-complete-full"))
        {for(var i=0;i<128;i++)CompleteOperation(powered);OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"powered complete");}
        var coast=new Flight(fuel:0);for(var i=0;i<128;i++)CompleteOperation(coast);
        using(var m=new OrdinaryAllocationMeasurement("powered-complete-unpowered"))
        {for(var i=0;i<128;i++)CompleteOperation(coast);OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"unpowered complete");}
        var exhaustion=new Flight[17];for(var i=0;i<exhaustion.Length;i++){exhaustion[i]=new(thrust:9000,exhaust:3000,intervals:1,capacity:1);exhaustion[i].Seal();}
        exhaustion[0].Apply();
        using(var m=new OrdinaryAllocationMeasurement("powered-interior-prepare-commit-ack"))
        {
            for(var i=1;i<exhaustion.Length;i++)
            {
                var f=exhaustion[i];var prepared=f.Engine.PreparePoweredFlight(f.Power,f.FuelLease,out var token);
                var result=f.Engine.PublishPoweredFlight(f.Power,token);
                if(prepared!=PoweredFlightStatus.Prepared||result.Status!=PoweredFlightStatus.Published)throw new InvalidOperationException("Interior allocation workload failed.");
            }
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"interior complete");
        }
        var single=new Flight();single.Seal();
        using(var m=new OrdinaryAllocationMeasurement("powered-physical-joint-preparation"))
        {var status=single.Engine.PreparePoweredFlight(single.Power,single.FuelLease,out single.PhysicalLease);var bytes=m.Complete();Check(status==PoweredFlightStatus.Prepared,"preparation allocation result");OrdinaryAllocationMeasurement.RequireZero(bytes,"physical preparation");}
        using(var m=new OrdinaryAllocationMeasurement("powered-fixed-publication-ack-observation"))
        {var result=single.Engine.PublishPoweredFlight(single.Power,single.PhysicalLease);var bytes=m.Complete();Check(result.Status==PoweredFlightStatus.Published,"publication allocation result");OrdinaryAllocationMeasurement.RequireZero(bytes,"joint publication");}
        var idle=new Flight();for(var i=0;i<128;i++){_=idle.Engine.ServicePoweredFlightDebt(idle.Power);_=idle.Engine.PublishPoweredFlight(idle.Power,default);}
        using(var m=new OrdinaryAllocationMeasurement("powered-no-work-refusal"))
        {
            for(var i=0;i<256;i++)
            {
                if(idle.Engine.ServicePoweredFlightDebt(idle.Power).Status!=PoweredFlightStatus.AwaitingDebt||idle.Engine.PublishPoweredFlight(idle.Power,default).Status!=PoweredFlightStatus.InvalidProposal)
                    throw new InvalidOperationException("No-work allocation workload failed.");
            }
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"no work/refusal");
        }
        var backlog=new Flight();for(var i=0;i<128;i++)CompleteOperation(backlog);
        backlog.Credit(1_000_000);
        using(var m=new OrdinaryAllocationMeasurement("powered-backlog-existing-debt"))
        {
            for(var i=0;i<15;i++)if(backlog.Engine.ServicePoweredFlightDebt(backlog.Power).PublishedCount!=4)throw new InvalidOperationException("Backlog workload failed.");
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"backlog");
        }
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("POWERED_ALLOCATION PASS exact_zero=7_windows positive_control=NONZERO");
    }
    internal static void Cost()
    {
        var warm=new Flight();CompleteOperation(warm);
        var before=GC.GetAllocatedBytesForCurrentThread();var coldStart=Stopwatch.GetTimestamp();var f=new Flight(mountY:.005,omegaZ:.1);
        var coldMs=Stopwatch.GetElapsedTime(coldStart).TotalMilliseconds;var coldBytes=GC.GetAllocatedBytesForCurrentThread()-before;
        var recordSize=Unsafe.SizeOf<PoweredFlightRecord>();var history=24L+1200L*recordSize;
        Check(coldBytes>=history,"cold retained upper bound includes coherent history");
        // The fixture has no unmanaged world. Every retained object was allocated during this cold construction;
        // cold allocation is a conservative upper bound, including transient setup objects, not process working set.
        Console.WriteLine($"POWERED_STORAGE record_bytes={recordSize} history_bytes={history} all_cold_managed_upper_bound={coldBytes} cold_ms={coldMs:R} <=8MiB={coldBytes<=8*1024*1024}");
        for(var i=0;i<128;i++)CompleteOperation(f);
        var samples=new double[1024];
        for(var i=0;i<samples.Length;i++){var start=Stopwatch.GetTimestamp();CompleteOperation(f);samples[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;}
        var worst=Array.IndexOf(samples,samples.Max());Array.Sort(samples);
        Console.WriteLine($"POWERED_COST runtime={Environment.Version} samples=1024 warm=128 median_ms={samples[512]:R} p95_ms={samples[972]:R} p99_ms={samples[1013]:R} max_ms={samples[1023]:R} worst_index={worst} frame_headroom_objective_ms=6.67");
        GC.KeepAlive(f);GC.KeepAlive(warm);
    }
}
