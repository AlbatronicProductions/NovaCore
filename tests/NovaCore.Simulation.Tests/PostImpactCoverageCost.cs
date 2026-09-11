using System.Diagnostics;
using System.Text.Json;

// Bounded candidate qualification: ordinary-runtime timing and separately isolated allocation.
internal static class PostImpactCoverageCost
{
    internal static void Measure(string name,Func<bool> work,bool zero=true)
    {
        for(var i=0;i<32;i++)Require(work(),name+" warmup");
        var samples=new long[101];
        for(var i=0;i<samples.Length;i++)
        {
            var start=Stopwatch.GetTimestamp();var success=work();samples[i]=Stopwatch.GetTimestamp()-start;
            Require(success,name+" timed result");
        }
        Array.Sort(samples);var completed=0;
        using var measurement=new OrdinaryAllocationMeasurement("coverage "+name);
        for(var i=0;i<8;i++)if(work())completed++;
        var bytes=measurement.Complete();
        if(zero)OrdinaryAllocationMeasurement.RequireZero(bytes,"coverage "+name);
        Require(completed==8,name+" completion count");
        double Us(int i)=>samples[i]*1e6/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="coverage-cost",name,warmup=32,samples=101,completed,bytes,
            zeroRequired=zero,entry="PASS",exit="PASS",medianUs=Us(50),p95Us=Us(95),p99Us=Us(99),maximumUs=Us(100)}));
    }
    private static void Require(bool value,string name)
    {if(!value)throw new InvalidOperationException("Coverage cost: "+name);}
}
