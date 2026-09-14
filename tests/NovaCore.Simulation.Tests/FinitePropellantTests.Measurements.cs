using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class FinitePropellantTests
{
    private static bool PrepareCopy(Fixture f, PropellantClassification expected, out PropellantProposal token)
    {
        return f.Engine.PrepareFinitePropellant(f.Resource,f.EngineToken,f.Target,out token)==PropellantPreparationStatus.Prepared &&
            f.Engine.PreviewFinitePropellant(f.Resource,token,out var preview)==PropellantPreparationStatus.Preview &&
            preview.Classification==expected && preview.Meaning==PropellantPreviewMeaning.ProposedIfApplied &&
            preview.TryGetSegment(0,out _) &&
            f.Engine.ObserveFinitePropellant(f.Resource,out var source,out var progress)==PropellantPreparationStatus.Ready &&
            source.RemainingUnits==preview.Resource.RemainingUnits && source.ResourceRevision==0 && progress.HasProposal;
    }
    private static bool Complete(Fixture f, PropellantClassification expected)
    {
        var ok=PrepareCopy(f,expected,out var token);
        return f.Engine.RetireFinitePropellant(f.Resource,token)==PropellantPreparationStatus.Retired && ok;
    }
    internal static void Allocation()
    {
        foreach(var (fuel,q,expected,name) in new[]{
            (1d,2d,PropellantClassification.FullPowered,"propellant-full"),
            (260.40625,15625d,PropellantClassification.EndpointExhaustion,"propellant-endpoint"),
            (1d/128,2d,PropellantClassification.InteriorExhaustion,"propellant-interior"),
            (double.Epsilon,2d,PropellantClassification.InteriorExhaustion,"propellant-tiny-interior"),
            (0d,2d,PropellantClassification.NoFeed,"propellant-empty")})
        {
            var f=new Fixture(fuel,EngineDefinition(q));f.Seal();
            for(var i=0;i<128;i++)Check(Complete(f,expected),"resource warmup");
            bool ok=true;
            using(var measurement=new OrdinaryAllocationMeasurement(name))
            {
                for(var i=0;i<1024;i++)ok&=Complete(f,expected);
                OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),name);
            }
            Check(ok,"complete exact proposal/read/retire outcomes");
        }
        var noDemand=new Fixture();noDemand.Seal(throttle:0);
        for(var i=0;i<128;i++)Check(Complete(noDemand,PropellantClassification.NoDemand),"no-demand warmup");
        bool success=true;
        using(var measurement=new OrdinaryAllocationMeasurement("propellant-no-demand"))
        {
            for(var i=0;i<1024;i++)success&=Complete(noDemand,PropellantClassification.NoDemand);
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"propellant-no-demand");
        }
        var preview=new Fixture();preview.Seal();preview.Prepare(out var active);
        var incompatible=new Fixture(engine:EngineDefinition(thrust:double.Epsilon,exhaust:2));incompatible.Seal();
        for(var i=0;i<128;i++)
        {
            preview.Engine.PreviewFinitePropellant(preview.Resource,active,out _);
            preview.Engine.PrepareFinitePropellant(preview.Resource,preview.EngineToken,preview.Target,out _);
            incompatible.Engine.PrepareFinitePropellant(incompatible.Resource,incompatible.EngineToken,incompatible.Target,out _);
            preview.Engine.PreviewFinitePropellant(preview.Resource,default,out _);
        }
        using(var measurement=new OrdinaryAllocationMeasurement("propellant-copied-observation"))
        {
            for(var i=0;i<1024;i++)success&=preview.Engine.PreviewFinitePropellant(preview.Resource,active,out var v)==PropellantPreparationStatus.Preview &&
                v.TryGetInteriorExhaustion(out _) && v.PoweredDuration.IsWithin(16666);
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"propellant-copied-observation");
        }
        using(var measurement=new OrdinaryAllocationMeasurement("propellant-refusals"))
        {
            for(var i=0;i<1024;i++)
            {
                success&=preview.Engine.PrepareFinitePropellant(preview.Resource,preview.EngineToken,preview.Target,out _)==PropellantPreparationStatus.OutstandingProposal;
                success&=preview.Engine.PreviewFinitePropellant(preview.Resource,default,out _)==PropellantPreparationStatus.InvalidProposal;
                success&=incompatible.Engine.PrepareFinitePropellant(incompatible.Resource,incompatible.EngineToken,incompatible.Target,out _)==PropellantPreparationStatus.IncompatibleFlow;
            }
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"propellant-refusals");
        }
        Check(success,"no-demand/observation/refusal outcomes");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("PROPELLANT_ALLOCATION PASS gates=8 expected_bytes=0 operations_per_gate=1024");
    }
    internal static void Cost()
    {
        var f=new Fixture();f.Seal();
        for(var i=0;i<256;i++)Check(Complete(f,PropellantClassification.InteriorExhaustion),"cost warmup");
        var samples=new double[4096];bool ok=true;
        for(var i=0;i<samples.Length;i++)
        {
            var before=Stopwatch.GetTimestamp();
            ok&=PrepareCopy(f,PropellantClassification.InteriorExhaustion,out var token);
            samples[i]=Stopwatch.GetElapsedTime(before).TotalMilliseconds;
            ok&=f.Engine.RetireFinitePropellant(f.Resource,token)==PropellantPreparationStatus.Retired;
        }
        Check(ok,"cost outcomes");Array.Sort(samples);
        Console.WriteLine($"PROPELLANT_PERFORMANCE warm=256 samples=4096 median_ms={samples[2048]:R} p95_ms={samples[3891]:R} p99_ms={samples[4055]:R} max_ms={samples[^1]:R} retirement=outside timing_runtime=ordinary threshold=REPORT_ONLY");
        // Only newly retained objects are constructed here: definition, authority, storage and seal.
        var cold=new Fixture(bind:false);PropellantResourceAuthority? resource;long bytes;
        using(var measurement=new OrdinaryAllocationMeasurement("propellant-cold-retained"))
        {
            var definition=Definition();
            ok=cold.Engine.BindFinitePropellant(cold.Parent,definition,out resource)==PropellantPreparationStatus.Ready;
            bytes=measurement.Complete();
        }
        Check(ok,"cold binding");GC.KeepAlive(resource);GC.KeepAlive(cold);
        Console.WriteLine($"PROPELLANT_STORAGE retained_graph_bytes={bytes} objects=4 owner_reference_bytes={IntPtr.Size} bounded_total_bytes={bytes+IntPtr.Size} integer_bytes={Unsafe.SizeOf<PropellantInteger>()} definition_values_bytes={Unsafe.SizeOf<PropellantDefinitionValues>()} source_observation_bytes={Unsafe.SizeOf<PropellantSourceObservation>()} ratio_bytes={Unsafe.SizeOf<PropellantDuration>()} witness_value_bytes={Unsafe.SizeOf<PropellantExhaustionWitness>()} proposal_value_bytes={Unsafe.SizeOf<PropellantSegmentationPreview>()} token_bytes={Unsafe.SizeOf<PropellantProposal>()} progress_bytes={Unsafe.SizeOf<PropellantPreparationProgress>()} growing_history_bytes=0");
    }
}
