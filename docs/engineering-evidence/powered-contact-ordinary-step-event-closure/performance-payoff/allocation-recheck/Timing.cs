using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

// Evidence only: one identical retained-world target interval per cold fixture.
internal sealed class TimedWorld : IDisposable
{
    internal readonly InputState Input=new();
    internal readonly Capture Capture=new();
    internal readonly BufferPool Pool=new(16384);
    internal readonly Simulation Simulation;
    internal readonly BodyHandle Body;
    internal int TargetSubsteps;
    internal Prepared LastPrepared;
    internal TimedWorld(Prepared p,float v0,float w0)
    {
        Simulation=Simulation.Create(Pool,new Callbacks(Capture),new Integrator(Input),new SolveDescription(8,1),timestepper:new DefaultTimestepper());
        var shape=Simulation.Shapes.Add(new Box(2,1,1));var slab=Simulation.Shapes.Add(new Box(16,2,16));
        var inertia=new BodyInertia{InverseMass=p.InverseMass,InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
        Body=Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
        Simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
        for(var j=0;j<120;j++)Simulation.Timestep(p.Dt);
        Simulation.Bodies[Body].Velocity.Linear.X=v0;Simulation.Bodies[Body].Velocity.Angular.Y=w0;
        Simulation.Solver.SubstepStarted+=CountSubstep;
    }
    private void CountSubstep(int index){if(index!=0)throw new InvalidOperationException("Unexpected substep index");TargetSubsteps++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Step(Prepared p)
    {
        LastPrepared=p;
        Input.Linear=new(p.Ax,p.Ay,0);Input.Angular=new(p.Wx,p.Wy,0);
        Simulation.Timestep(p.Dt);
    }
    internal void Check(JsonElement expected,bool endpoint)
    {
        var b=Simulation.Bodies[Body];
        if(b.Constraints.Count!=1)throw new InvalidOperationException("Retained constraint lost");
        var handle=b.Constraints[0].ConnectingConstraintHandle;var reader=new Reader(Capture);
        if(!Simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader))throw new InvalidOperationException("Native read failed");
        if(handle.Value!=expected.GetProperty("handle").GetInt32()||Body.Value!=expected.GetProperty("body").GetInt32()||Capture.Count!=4)throw new InvalidOperationException("Native identity differs");
        Span<float> values=[b.Pose.Position.X,b.Pose.Position.Y,b.Pose.Position.Z,b.Pose.Orientation.X,b.Pose.Orientation.Y,b.Pose.Orientation.Z,b.Pose.Orientation.W,
            b.Velocity.Linear.X,b.Velocity.Linear.Y,b.Velocity.Linear.Z,b.Velocity.Angular.X,b.Velocity.Angular.Y,b.Velocity.Angular.Z,
            Capture.Impulses[0],Capture.Impulses[1],Capture.Impulses[2],Capture.Impulses[3],Capture.Impulses[4],Capture.Impulses[5],Capture.Impulses[6]];
        var bits=expected.GetProperty("bits");
        if(bits.GetArrayLength()!=values.Length)throw new InvalidOperationException("Snapshot width differs");
        for(var i=0;i<values.Length;i++)if(BitConverter.SingleToInt32Bits(values[i])!=bits[i].GetInt32())throw new InvalidOperationException($"Native snapshot differs at {i}, endpoint={endpoint}");
        for(var i=0;i<4;i++)
        {
            var contact=expected.GetProperty("contacts")[i];var c=Capture.Contacts[i];
            if(Capture.Features[i]!=contact.GetProperty("feature").GetInt32())throw new InvalidOperationException("Feature differs");
            Same(c.Offset.X,contact.GetProperty("offset")[0]);Same(c.Offset.Y,contact.GetProperty("offset")[1]);Same(c.Offset.Z,contact.GetProperty("offset")[2]);
            Same(c.Normal.X,contact.GetProperty("normal")[0]);Same(c.Normal.Y,contact.GetProperty("normal")[1]);Same(c.Normal.Z,contact.GetProperty("normal")[2]);Same(c.Depth,contact.GetProperty("depth"));
        }
        Same(Capture.Friction,expected.GetProperty("friction"));Same(Capture.Omega,expected.GetProperty("omega"));Same(Capture.Damping,expected.GetProperty("damping"));Same(Capture.Recovery,expected.GetProperty("recovery"));
        if(Simulation.Solver.VelocityIterationCount!=8||TargetSubsteps!=(endpoint?1:0))throw new InvalidOperationException("Solver work changed");
        if(endpoint&&BitConverter.SingleToInt32Bits(LastPrepared.InverseMass)!=BitConverter.SingleToInt32Bits(b.LocalInertia.InverseMass))throw new InvalidOperationException("Prepared mass differs from native mass");
    }
    private static void Same(float x,JsonElement expected){if(BitConverter.SingleToInt32Bits(x)!=BitConverter.SingleToInt32Bits(expected.GetSingle()))throw new InvalidOperationException("Contact/material bits differ");}
    public void Dispose(){Simulation.Dispose();Pool.Clear();}
}

internal readonly record struct Sample(int Index,long Total,long Preparation,long Native,int Gen0,int Gen1,int Gen2);
internal static class Timing
{
    private const int Warm=128,Count=1024;
    private static double Us(long ticks)=>ticks*1e6/Stopwatch.Frequency;
    private static void Equal(Prepared a,Prepared b)
    {
        if(BitConverter.SingleToInt32Bits(a.Ax)!=BitConverter.SingleToInt32Bits(b.Ax)||BitConverter.SingleToInt32Bits(a.Ay)!=BitConverter.SingleToInt32Bits(b.Ay)||
           BitConverter.SingleToInt32Bits(a.Wx)!=BitConverter.SingleToInt32Bits(b.Wx)||BitConverter.SingleToInt32Bits(a.Wy)!=BitConverter.SingleToInt32Bits(b.Wy)||
           BitConverter.SingleToInt32Bits(a.InverseMass)!=BitConverter.SingleToInt32Bits(b.InverseMass)||BitConverter.SingleToInt32Bits(a.Dt)!=BitConverter.SingleToInt32Bits(b.Dt))throw new InvalidOperationException("FP32 changed");
    }
    private static Sample Measure(int index,TimedWorld world,bool candidate,in FixedInput input,Prepared ordinary)
    {
        var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
        var start=Stopwatch.GetTimestamp();
        var p=candidate?Correction.Map(input).Floats:ordinary;
        var mapped=Stopwatch.GetTimestamp();
        world.Step(p);
        var end=Stopwatch.GetTimestamp();
        var sample=new Sample(index,end-start,mapped-start,end-mapped,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2);
        Equal(p,ordinary);return sample;
    }
    private static object Stats(Sample[] samples)
    {
        var sorted=samples.Select(x=>x.Total).Order().ToArray();
        double Median(long[] a)=>(Us(a[a.Length/2-1])+Us(a[a.Length/2]))/2;
        return new{count=samples.Length,medianUs=Median(sorted),p95Us=Us(sorted[(int)Math.Ceiling(sorted.Length*.95)-1]),p99Us=Us(sorted[(int)Math.Ceiling(sorted.Length*.99)-1]),maxUs=Us(sorted[^1]),
            preparationMedianUs=Median(samples.Select(x=>x.Preparation).Order().ToArray()),nativeAndBindingMedianUs=Median(samples.Select(x=>x.Native).Order().ToArray()),
            collectionDeltas=new[]{samples.Sum(x=>x.Gen0),samples.Sum(x=>x.Gen1),samples.Sum(x=>x.Gen2)},
            topTen=samples.OrderByDescending(x=>x.Total).Take(10).Select(x=>new{x.Index,totalUs=Us(x.Total),preparationUs=Us(x.Preparation),nativeAndBindingUs=Us(x.Native),x.Gen0,x.Gen1,x.Gen2}).ToArray()};
    }
    private static int Main(string[] args)
    {
        using var inputDocument=JsonDocument.Parse(File.ReadAllText(args[0]));using var witness=JsonDocument.Parse(File.ReadAllText(args[1]));
        var row=inputDocument.RootElement.GetProperty("cases").EnumerateArray().Single(x=>x.GetProperty("input").GetProperty("name").GetString()=="off-com").GetProperty("input");
        var exact=new ExactInput(row);var input=new FixedInput(exact);
        var r=witness.RootElement;var p=r.GetProperty("prepared");
        var ordinary=new Prepared(p.GetProperty("linearAcceleration")[0].GetSingle(),p.GetProperty("linearAcceleration")[1].GetSingle(),p.GetProperty("angularAcceleration")[0].GetSingle(),p.GetProperty("angularAcceleration")[1].GetSingle(),p.GetProperty("inverseMass").GetSingle(),r.GetProperty("backendDt").GetSingle());
        Equal(Correction.Map(input).Floats,ordinary);
        var v0=(float)Rational.Parse(row.GetProperty("v0").GetString()!).Value;var w0=(float)Rational.Parse(row.GetProperty("w0").GetString()!).Value;
        var source=r.GetProperty("snapshots")[0];var endpoint=r.GetProperty("snapshots")[3];
        var a=new Sample[Count];var b=new Sample[Count];var c=new Sample[Count];
        long coldTicks=0,coldBytes=0;ulong maxPoolBytes=0;int sourceChecks=0,endpointChecks=0,observedSubsteps=0;
        var fullG0=GC.CollectionCount(0);var fullG1=GC.CollectionCount(1);var fullG2=GC.CollectionCount(2);
        for(var i=-Warm;i<Count;i++)
        {
            var coldStart=Stopwatch.GetTimestamp();var bytesStart=GC.GetAllocatedBytesForCurrentThread();
            using var wa=new TimedWorld(ordinary,v0,w0);using var wb=new TimedWorld(ordinary,v0,w0);
            coldTicks+=Stopwatch.GetTimestamp()-coldStart;coldBytes+=GC.GetAllocatedBytesForCurrentThread()-bytesStart;
            maxPoolBytes=Math.Max(maxPoolBytes,wa.Pool.GetTotalAllocatedByteCount()+wb.Pool.GetTotalAllocatedByteCount());
            wa.Check(source,false);wb.Check(source,false);sourceChecks+=2;
            Sample sa,sb;
            if((i&1)==0){sa=Measure(i,wa,false,input,ordinary);sb=Measure(i,wb,true,input,ordinary);}
            else{sb=Measure(i,wb,true,input,ordinary);sa=Measure(i,wa,false,input,ordinary);}
            wa.Check(endpoint,true);wb.Check(endpoint,true);endpointChecks+=2;observedSubsteps+=wa.TargetSubsteps+wb.TargetSubsteps;
            if(i>=0){a[i]=sa;b[i]=sb;}
        }
        for(var i=0;i<Warm;i++)Equal(Correction.Map(input).Floats,ordinary);
        for(var i=0;i<Count;i++)
        {
            var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
            var start=Stopwatch.GetTimestamp();var mapped=Correction.Map(input).Floats;var end=Stopwatch.GetTimestamp();
            c[i]=new(i,end-start,end-start,0,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2);Equal(mapped,ordinary);
        }
        long allocationA,allocationB,allocationC;
        using(var world=new TimedWorld(ordinary,v0,w0))
        {world.Check(source,false);using(var m=new OrdinaryAllocationMeasurement("timed-A-equivalent")){world.Step(ordinary);allocationA=m.Complete();}OrdinaryAllocationMeasurement.RequireZero(allocationA,"A");world.Check(endpoint,true);}
        using(var world=new TimedWorld(ordinary,v0,w0))
        {world.Check(source,false);using(var m=new OrdinaryAllocationMeasurement("timed-B-equivalent")){world.Step(Correction.Map(input).Floats);allocationB=m.Complete();}OrdinaryAllocationMeasurement.RequireZero(allocationB,"B");Equal(world.LastPrepared,ordinary);world.Check(endpoint,true);}
        Prepared last=default;
        using(var m=new OrdinaryAllocationMeasurement("timed-C-repeat128")){for(var i=0;i<128;i++)last=Correction.Map(input).Floats;allocationC=m.Complete();}
        OrdinaryAllocationMeasurement.RequireZero(allocationC,"C");Equal(last,ordinary);
        var result=new{result="PERFORMANCE_PROCESS_PASS",process=int.Parse(args[3]),runtime=Environment.Version.ToString(),architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),stopwatchFrequency=Stopwatch.Frequency,
            warmPerArm=Warm,measuredPerArm=Count,A=Stats(a),B=Stats(b),C=Stats(c),allocations=new{A=allocationA,B=allocationB,C128=allocationC},
            sourceChecks,endpointChecks,observedTimedAndWarmSubsteps=observedSubsteps,explicitTimedAndWarmTimestepCalls=2*(Warm+Count),configuredIterationsPerStep=8,configuredSubsteps=1,independentlyObservedIterations="UNAVAILABLE; no iteration scheduler override",
            ordinaryDtBits=BitConverter.SingleToInt32Bits(ordinary.Dt),nativeIdentity="all checked against retained off-com source/endpoint including contacts/cache",eventSizedSteps=0,manualCacheWrites=0,
            cold=new{worldCount=2*(Warm+Count),preparationStepsPerWorld=120,totalPreparationUs=Us(coldTicks),meanPreparationUsPerWorld=Us(coldTicks)/(2*(Warm+Count)),totalManagedCounterBytes=coldBytes,maxConcurrentNativePoolBytes=maxPoolBytes},
            campaignIncludingColdFixturesAndAllocationControlsCollectionDeltas=new[]{GC.CollectionCount(0)-fullG0,GC.CollectionCount(1)-fullG1,GC.CollectionCount(2)-fullG2},
            fixedInputValueBytes=Unsafe.SizeOf<FixedInput>(),projectedValueBytes=Unsafe.SizeOf<Projected>(),preparedValueBytes=Unsafe.SizeOf<Prepared>(),newMapperRetainedHeapBytes=0,
            limitations="Repeated one-interval retained-world fixtures, not live depletion/canonical publication or whole-engine performance. Inner timestamps included; no subtraction."};
        File.WriteAllText(args[2],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine(JsonSerializer.Serialize(new{result.result,result.process,result.A,result.B,result.C}));return 0;
    }
}
