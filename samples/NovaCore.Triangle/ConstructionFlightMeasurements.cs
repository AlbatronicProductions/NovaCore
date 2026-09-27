using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Bounded observational capture; filling it never stops simulation.</summary>
internal sealed class ConstructionFlightMeasurements
{
    private const int Warmup=64,Samples=256,Windows=3;
    private readonly Sample[][] values=[new Sample[Samples*Windows],new Sample[Samples*Windows],new Sample[Samples*Windows]];
    private readonly int[] seen=new int[3],counts=new int[3];
    internal bool PhaseComplete(int phase)=>counts[phase]==values[phase].Length;
    private readonly record struct Sample(double Frame,double Cpu,long Allocated,int G0,int G1,int G2,bool Rcs);
    internal void Record(ConstructionFlightScene scene,double frame,double cpu,long allocated,int g0,int g1,int g2)
    {
        if(scene.Failed)return;
        var phase=scene.State.Physical!.Consumer==AssemblyPhysicalConsumer.SupportedContact?0:scene.Actuation.Main?1:2;
        if(seen[phase]++<Warmup||counts[phase]==values[phase].Length)return;
        values[phase][counts[phase]++]=new(frame,cpu,allocated,g0,g1,g2,!scene.Actuation.Jets.IsEmpty);
    }
    internal void Report()
    {
        var rows=new List<object>();var names=new[]{"supported","powered","coast"};
        static object Distribution(double[] samples){var sorted=samples.Order().ToArray();double P(double p)=>sorted[Math.Clamp((int)Math.Ceiling(p*sorted.Length)-1,0,sorted.Length-1)];var threshold=P(.95);var consecutive=0;var maximum=0;foreach(var s in samples){consecutive=s>=threshold?consecutive+1:0;maximum=Math.Max(maximum,consecutive);}return new {median=P(.5),p95=threshold,p99=P(.99),max=P(1),maximumConsecutiveP95=maximum};}
        for(var phase=0;phase<3;phase++)for(var window=0;window<Windows;window++)
        {
            var n=Math.Min(Samples,counts[phase]-window*Samples);if(n<=0)continue;var samples=values[phase].Skip(window*Samples).Take(n).ToArray();
            rows.Add(new{phase=names[phase],window,samples=n,complete=n==Samples,rcsFrames=samples.Count(s=>s.Rcs),frameMs=Distribution(samples.Select(s=>s.Frame).ToArray()),callbackMs=Distribution(samples.Select(s=>s.Cpu).ToArray()),allocatedBytes=Distribution(samples.Select(s=>(double)s.Allocated).ToArray()),collections=new[]{samples.Sum(s=>s.G0),samples.Sum(s=>s.G1),samples.Sum(s=>s.G2)}});
        }
        Console.WriteLine("MODULAR_INTEGRATED_MEASURE "+JsonSerializer.Serialize(new{warmupPerPhase=Warmup,sampling="first three 256-frame windows within each phase; normal GC; phase re-entry retains that phase's sequence",boundary="callback input, bounded physical service, celestial publication, camera and render preparation; frame interval also includes native presentation/scheduling",windows=rows.Count}));
        foreach(var row in rows)Console.WriteLine("MODULAR_INTEGRATED_WINDOW "+JsonSerializer.Serialize(row));
    }
}
