// Bounded payoff closure; all numerical adoption/installation remains outside this diagnostic.
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using P=PieceKernel.Patch;
using S=PieceKernel.State;
using V=PieceKernel.V;

internal static class PayoffClosure
{
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true,IncludeFields=true};
    static string Output="";
    static double Sink;
    internal sealed record Fixture(string Name,P Patch,S Free,double H,double Omega,double[] Initial,double[,] A,double[] Rhs);
    internal sealed record Accuracy(double Normal,double Linear,double Angular,double Residual,double[] ResidualComponents,bool Pass);
    static void Check(bool b,string message){if(!b)throw new InvalidOperationException(message);}
    static V Vec(JsonElement x)=>new(x.GetProperty("X").GetDouble(),x.GetProperty("Y").GetDouble(),x.GetProperty("Z").GetDouble());
    static S State(JsonElement x)=>new(Vec(x.GetProperty("Linear")),Vec(x.GetProperty("Angular")));
    static double[] Values(JsonElement x)=>x.EnumerateArray().Select(x=>x.GetDouble()).ToArray();
    static bool Bits(ReadOnlySpan<double> a,ReadOnlySpan<double> b)
    {
        if(a.Length!=b.Length)return false;
        for(int i=0;i<a.Length;i++)if(BitConverter.DoubleToUInt64Bits(a[i])!=BitConverter.DoubleToUInt64Bits(b[i]))return false;
        return true;
    }
    static double[] Six(S s)=>[s.Linear.X,s.Linear.Y,s.Linear.Z,s.Angular.X,s.Angular.Y,s.Angular.Z];
    static double[][] Rows(double[,] a)=>Enumerable.Range(0,7).Select(i=>Enumerable.Range(0,7).Select(j=>a[i,j]).ToArray()).ToArray();
    static (double[,] A,double[] Rhs) Equations(P p,S free,double h,double omega)
    {
        var a=(double[,])p.K.Clone();var rhs=new double[7];double alpha=1/(omega*h*(omega*h+2));
        for(int i=0;i<7;i++)
        {
            rhs[i]=-p.J(i,free);
            if(i<4){a[i,i]+=alpha*p.K[i,i];rhs[i]+=Math.Min(p.Depth[i]/h,Math.Min(p.Depth[i]/(h+2/omega),2));}
        }
        return(a,rhs);
    }
    static Fixture Make(string name,P p,S free,double h,double omega,double[] initial)
    {
        Check(p.InverseMass>0&&double.IsFinite(p.InverseMass)&&p.Linear.Length==7&&p.Depth.Length==4,"restricted four-contact system");
        var e=Equations(p,free,h,omega);
        for(int i=0;i<7;i++)for(int j=0;j<7;j++)Check(double.IsFinite(e.A[i,j]),"finite current matrix");
        return new(name,p,free,h,omega,initial,e.A,e.Rhs);
    }
    static Fixture Coast(JsonElement coast)
    {
        var input=coast.GetProperty("inputs");
        var cap=new Capture{Count=4,Omega=(float)input.GetProperty("omega").GetDouble(),TwiceDamping=2,Friction=.5f,Recovery=2};
        var normal=Vec(input.GetProperty("normal"));
        for(int i=0;i<4;i++)
        {
            var r=Vec(input.GetProperty("offsets")[i]);
            cap.Features[i]=input.GetProperty("features")[i].GetInt32();
            cap.Contacts[i]=new(){Normal=new((float)normal.X,(float)normal.Y,(float)normal.Z),
                Offset=new((float)r.X,(float)r.Y,(float)r.Z),Depth=(float)input.GetProperty("depths")[i].GetDouble()};
        }
        var p=new P(cap,input.GetProperty("Mass").GetDouble());double h=input.GetProperty("h").GetDouble();
        var source=State(input.GetProperty("Source"));var free=new S(source.Linear+h*new V(0,-9.81,0),source.Angular);
        var f=Make("original-coast",p,free,h,input.GetProperty("omega").GetDouble(),Values(input.GetProperty("guess")));
        for(int i=0;i<7;i++)Check(Bits(Rows(f.A)[i],Values(coast.GetProperty("a")[i])),"exact current coast A");
        Check(Bits(f.Rhs,Values(coast.GetProperty("rhs"))),"exact current coast rhs");
        return f;
    }
    static Fixture Synthetic(JsonElement item,double omega)
    {
        var name=item.GetProperty("Name").GetString()!;Vector3 n=Vector3.UnitY;double angle=0;V shift=default;
        if(name is "tiny-normal" or "moderate-normal" or "normal-and-tangent")
        {double rotation=name=="tiny-normal"?1e-8:.08;n=new(0,(float)Math.Cos(rotation),(float)Math.Sin(rotation));}
        if(name is "tangent-only" or "normal-and-tangent")angle=.7;
        if(name=="lever-only")shift=new(0,-.0001,0);
        var p=(P)typeof(BasisProbe).GetMethod("Synthetic",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{n,angle,shift,false})!;
        var basis=item.GetProperty("newBasis");
        Check(Bits(Six(new(p.Linear[0],p.Linear[4])),Six(new(Vec(basis.GetProperty("normal")),Vec(basis.GetProperty("tangent1"))))),"same synthetic basis");
        Check(p.Linear[5]==Vec(basis.GetProperty("tangent2")),"same synthetic tangent2");
        for(int i=0;i<4;i++)Check(p.Offsets[i]==Vec(basis.GetProperty("offsets")[i]),"same synthetic offsets");
        double h=17707d/2000000;
        return Make(name,p,new((-9.81*h)*p.Linear[0],default),h,omega,Values(item.GetProperty("transport").GetProperty("Cache")));
    }
    static object Snapshot(Fixture f)=>new{f.Name,f.H,f.Omega,free=f.Free,inverseMass=f.Patch.InverseMass,inverseInertia=new[]{.5,.5,.5},
        normalAndTangentRows=f.Patch.Linear,angularRows=f.Patch.Angular,offsets=f.Patch.Offsets,depths=f.Patch.Depth,radii=f.Patch.Radii,
        friction=.5,tangentCapCoefficient=.125,twistCapCoefficient=.125,twiceDamping=2,recovery=2,A=Rows(f.A),rhs=f.Rhs,initial=f.Initial};
    static string EquationHash(Fixture f)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(Snapshot(f),Json)));
    static Accuracy Score(Fixture f,PieceKernel.SweepResult solved,PieceKernel.OracleResult reference)
    {
        var r=new double[7];double max=0;
        for(int i=0;i<7;i++){r[i]=-f.Rhs[i];for(int j=0;j<7;j++)r[i]+=f.A[i,j]*solved.Impulses[j];max=Math.Max(max,Math.Abs(r[i]));}
        double n=Math.Abs(solved.Impulses.Take(4).Sum()-reference.Impulses.Take(4).Sum());
        double v=(solved.Velocity.Linear-reference.Velocity.Linear).Length,w=(solved.Velocity.Angular-reference.Velocity.Angular).Length;
        return new(n,v,w,max,r,n<=1e-4&&v<=1e-4&&w<=1e-4);
    }
    static void Durable(string name,object report)
    {
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(report,Json);
        using(var file=new FileStream(Path.Combine(Output,name),FileMode.CreateNew,FileAccess.Write,FileShare.Read,4096,FileOptions.WriteThrough))
        {file.Write(bytes);file.WriteByte(10);file.Flush(flushToDisk:true);}
        Console.WriteLine("PERSISTED "+name);
    }
    static void Case(string file,Func<(object Report,bool Pass)> action)
    {
        try
        {
            var r=action();Durable(file,r.Report);
            Check(r.Pass,"case failed; full result already durably retained: "+file);
        }
        catch(Exception e)
        {
            if(!File.Exists(Path.Combine(Output,file)))Durable(file,new{status="STOP",exception=e.GetType().FullName,e.Message});
            throw;
        }
    }
    static void Equivalent(HotKernel.Result hot,double[] work,PieceKernel.SweepResult actual)
    {
        Check(Bits(work,actual.Impulses)&&Bits(Six(hot.Velocity),Six(actual.Velocity))&&
            hot.NormalClamps==actual.NormalClamps&&hot.TangentClamps==actual.TangentClamps&&hot.TwistClamps==actual.TwistClamps,
            "storage adapter bit identity (seven impulses, six velocities, clamps)");
    }
    static (object Report,bool Pass) MeasureD(Fixture f,JsonElement oldSolved)
    {
        string before=EquationHash(f);
        // Original accepted D is invoked BEFORE reference construction and with no reference arguments.
        var initialized=CoastPayoff.RemoveCommonResidual(f.Patch,f.Free,f.Omega,f.H,f.Initial,out var delta,out var numerator,out var denominator);
        var scratch=(double[])f.Initial.Clone();var prepared=HotKernel.Initialize(f.Patch,f.Free,f.Omega,f.H,scratch);
        Check(prepared.Status==HotKernel.Admission.Ready&&Bits(scratch,initialized)&&prepared.Delta==delta&&
            prepared.Numerator==numerator&&prepared.Denominator==denominator,"original D and allocation-safe D bit identity");
        var solved=PieceKernel.Solve(f.Patch,f.Free,initialized,f.Omega,8,f.H);
        var hot=HotKernel.Solve(f.Patch,f.Free,scratch,f.Omega,8,f.H);Equivalent(hot,scratch,solved);
        var reference=PieceKernel.Reference(f.Patch,f.Free,f.Omega,f.H);Check(reference.Admissible&&reference.Residual<=1e-12,"reference domain");
        var baseline=PieceKernel.Solve(f.Patch,f.Free,f.Initial,f.Omega,8,f.H);
        Check(Bits(baseline.Impulses,Values(oldSolved.GetProperty("Impulses")))&&Bits(Six(baseline.Velocity),Six(State(oldSolved.GetProperty("Velocity")))),"unchanged baseline exact replay");
        f.Initial.CopyTo(scratch,0);Equivalent(HotKernel.Solve(f.Patch,f.Free,scratch,f.Omega,8,f.H),scratch,baseline);
        string after=EquationHash(f);Check(before==after,"current system/input nonmutation");
        var accuracy=Score(f,solved,reference);
        return(new{name=f.Name,status=accuracy.Pass?"PASS":"FAIL",kind="ACTUAL SOURCE-LINKED CANDIDATE SOLVER MEASUREMENT",
            snapshot=Snapshot(f),equationAndSeedHash=before,unchangedAfter=after,delta,numerator,denominator,initialized,
            result=solved,accuracy,reference,baseline,baselineAccuracy=Score(f,baseline,reference),prepared,
            adapterBitIdentical=true,referenceUsedByD=false,sweeps=8,physicalBars=new[]{1e-4,1e-4,1e-4}},accuracy.Pass);
    }
    static (object Report,bool Pass) MeasureSweeps(Fixture f,int sweeps)
    {
        string hash=EquationHash(f);var solved=PieceKernel.Solve(f.Patch,f.Free,f.Initial,f.Omega,sweeps,f.H);
        var scratch=(double[])f.Initial.Clone();Equivalent(HotKernel.Solve(f.Patch,f.Free,scratch,f.Omega,sweeps,f.H),scratch,solved);
        var reference=PieceKernel.Reference(f.Patch,f.Free,f.Omega,f.H);Check(reference.Admissible&&reference.Residual<=1e-12,"same reference");
        var accuracy=Score(f,solved,reference);Check(hash==EquationHash(f),"ordinary input unchanged");
        // A physical miss here is an expected comparison result, not a harness error.
        return(new{status=accuracy.Pass?"PASS":"FAIL",kind="ACTUAL SOURCE-LINKED CANDIDATE SOLVER MEASUREMENT",
            sweeps,equationAndSeedHash=hash,result=solved,accuracy,reference,adapterBitIdentical=true,D=false},true);
    }
    static (object Report,bool Pass) Refusal(Fixture f)
    {
        string before=EquationHash(f);var scratch=(double[])f.Initial.Clone();
        var admission=HotKernel.Initialize(f.Patch,f.Free,f.Omega,f.H,scratch);
        bool pass=admission.Status==HotKernel.Admission.CurrentFrictionCap&&before==EquationHash(f);
        return(new{name=f.Name,status=pass?"EXPECTED REFUSAL":"FAIL",admission,snapshot=Snapshot(f),proposalOnly=scratch,
            inputUnchanged=before==EquationHash(f),solverCalls=0,referenceCalls=0,clamping=false},pass);
    }
    static void Correctness(JsonElement convergence,JsonElement synthetic)
    {
        var coast=convergence.GetProperty("coast");var f=Coast(coast);
        Case("01-coast-D.json",()=>MeasureD(f,coast.GetProperty("result")));
        for(int sweeps=9;sweeps<=11;sweeps++){int n=sweeps;Case($"0{n-7}-ordinary-{n}.json",()=>MeasureSweeps(f,n));}
        string[] accepted=["identical","tiny-normal","tangent-only","lever-only"];int index=5;
        var items=synthetic.GetProperty("cases").EnumerateArray().ToArray();
        foreach(string name in accepted)
        {
            var item=items.Single(x=>x.GetProperty("Name").GetString()==name);
            Check(item.GetProperty("transport").GetProperty("Status").GetString()=="Ready","prior synthetic admitted");
            var sf=Synthetic(item,f.Omega);Case($"{index++:D2}-{name}-D.json",()=>MeasureD(sf,item.GetProperty("eightSweep").GetProperty("solved")));
        }
        foreach(string name in new[]{"moderate-normal","normal-and-tangent"})
        {var sf=Synthetic(items.Single(x=>x.GetProperty("Name").GetString()==name),f.Omega);Case($"{index++:D2}-{name}-refusal.json",()=>Refusal(sf));}
        int minimum=12;
        for(int n=9;n<=11;n++)
        {
            using var file=JsonDocument.Parse(File.ReadAllText(Path.Combine(Output,$"0{n-7}-ordinary-{n}.json")));
            if(file.RootElement.GetProperty("accuracy").GetProperty("Pass").GetBoolean())minimum=Math.Min(minimum,n);
        }
        Check(convergence.GetProperty("curve").EnumerateArray().Single(x=>x.GetProperty("Sweeps").GetInt32()==12).GetProperty("Pass").GetBoolean(),"prior twelve pass");
        Durable("correctness-summary.json",new{status="PASS",coastMeasured=true,syntheticsMeasured=4,expectedRefusals=2,
            smallestTestedPassing=minimum,testedHere=new[]{9,10,11},priorTwelvePass=true,allPerCaseRecordsDurable=true,noAdoption=true});
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static HotKernel.Result Evaluate(int arm,Fixture f,double[] work,int passing)
    {
        f.Initial.CopyTo(work,0);
        if(arm==1)
        {
            var p=HotKernel.Initialize(f.Patch,f.Free,f.Omega,f.H,work);
            if(p.Status!=HotKernel.Admission.Ready)throw new InvalidOperationException("Unexpected timed D refusal");
        }
        return HotKernel.Solve(f.Patch,f.Free,work,f.Omega,arm==2?passing:8,f.H);
    }
    static double Percentile(double[] sorted,double p)=>sorted[(int)Math.Ceiling(p*sorted.Length)-1];
    static object Stats(long[] ticks)
    {
        double[] us=ticks.Select(t=>t*1e6/Stopwatch.Frequency).Order().ToArray();
        return new{samples=us.Length,medianUs=(us[us.Length/2-1]+us[us.Length/2])*.5,p95Us=Percentile(us,.95),p99Us=Percentile(us,.99),maxUs=us[^1],
            quartileBlockMedianUs=Enumerable.Range(0,4).Select(i=>{var b=ticks.Skip(i*ticks.Length/4).Take(ticks.Length/4).Select(t=>t*1e6/Stopwatch.Frequency).Order().ToArray();return(b[b.Length/2-1]+b[b.Length/2])*.5;}).ToArray()};
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] AllocateControl()=>new byte[128];
    static void Cost(JsonElement convergence)
    {
        using var summary=JsonDocument.Parse(File.ReadAllText(Path.Combine(Output,"correctness-summary.json")));
        Check(summary.RootElement.GetProperty("status").GetString()=="PASS","correctness before cost");
        int minimum=summary.RootElement.GetProperty("smallestTestedPassing").GetInt32();
        var f=Coast(convergence.GetProperty("coast"));var work=new double[7];var checks=new List<object>();
        for(int arm=0;arm<3;arm++)
        {
            double[] initial=f.Initial;
            if(arm==1)initial=CoastPayoff.RemoveCommonResidual(f.Patch,f.Free,f.Omega,f.H,initial,out _,out _,out _);
            var actual=PieceKernel.Solve(f.Patch,f.Free,initial,f.Omega,arm==2?minimum:8,f.H);
            var hot=Evaluate(arm,f,work,minimum);Equivalent(hot,work,actual);
            var reference=PieceKernel.Reference(f.Patch,f.Free,f.Omega,f.H);var accuracy=Score(f,actual,reference);
            Check(arm==0||accuracy.Pass,"Release payoff correctness");
            checks.Add(new{arm,sweeps=arm==2?minimum:8,bitIdentical=true,accuracy,result=actual});
        }
        Durable("release-equivalence.json",new{checks,sourceLinkedCandidate=true,adapter="caller-owned storage; identical solver arithmetic"});
        const int Warm=4096,Samples=4096;var timings=new[]{new long[Samples],new long[Samples],new long[Samples]};var overhead=new long[1024];
        for(int i=0;i<overhead.Length;i++){long t=Stopwatch.GetTimestamp();overhead[i]=Stopwatch.GetTimestamp()-t;}
        for(int i=0;i<Warm;i++)for(int arm=0;arm<3;arm++){var result=Evaluate(arm,f,work,minimum);Sink+=result.Velocity.Linear.Y;}
        double checksum=0;
        for(int i=0;i<Samples;i++)for(int offset=0;offset<3;offset++)
        {
            int arm=(i+offset)%3;long t=Stopwatch.GetTimestamp();var result=Evaluate(arm,f,work,minimum);
            timings[arm][i]=Stopwatch.GetTimestamp()-t;checksum+=result.Velocity.Linear.Y+result.Velocity.Angular.X;
        }
        Sink=checksum;
        Durable("timing.json",new{configuration="Release",processId=Environment.ProcessId,runtime=Environment.Version.ToString(),
            timestampFrequency=Stopwatch.Frequency,warmCallsPerArm=Warm,samplesPerArm=Samples,order="ABC/BCA/CAB rotating each round",
            units="microseconds per individual local operation",baseline8=Stats(timings[0]),D8=Stats(timings[1]),
            ordinaryPassing=Stats(timings[2]),passingSweeps=minimum,timestampPairOverhead=Stats(overhead),checksum,
            includes="seed copy; D checks/current-equation preparation if D; solve; velocity reconstruction",
            excludes="cold fixture/current input capture, reference, reporting; no world or frame",
            allocationSafeStorageAdapter=true,subtraction=false,noTieringOrPgoChanges=true});
        // All reporting and measurement entry/exit are outside the counted local work.
        long bytes;double allocationChecksum=0;
        using(var measurement=new OrdinaryAllocationMeasurement("coast-payoff-D8"))
        {
            for(int i=0;i<Samples;i++){var result=Evaluate(1,f,work,minimum);allocationChecksum+=result.Velocity.Linear.Y;}
            bytes=measurement.Complete();
        }
        Sink=allocationChecksum;long positiveBytes;GC.KeepAlive(AllocateControl());
        using(var measurement=new OrdinaryAllocationMeasurement("coast-payoff-positive"))
        {
            var value=AllocateControl();positiveBytes=measurement.Complete();GC.KeepAlive(value);
        }
        Durable("allocation.json",new{calls=Samples,bytes,pass=bytes==0,entry="PASS",exit="PASS",reservationBytes=1<<20,
            boundary="unchanged OrdinaryAllocationMeasurement",positiveControl=new{type="System.Byte[]",length=128,bytes=positiveBytes,pass=positiveBytes>0},
            scope="D preparation plus allocation-safe diagnostic local eight-sweep operator only",allocationChecksum});
        Check(bytes==0&&positiveBytes>0,"allocation or positive control failure; persisted");
    }
    public static int Main(string[] args)
    {
        Output=Path.GetFullPath(args[1]);Directory.CreateDirectory(Output);
        string mode=args[0];
        Durable(mode+"-attempt.json",new{mode,processId=Environment.ProcessId,runtime=Environment.Version.ToString(),status="ATTEMPT STARTED; no overwrite/retry"});
        try
        {
            using var convergence=JsonDocument.Parse(File.ReadAllText(args[2]));using var synthetic=JsonDocument.Parse(File.ReadAllText(args[3]));
            if(mode=="correctness")Correctness(convergence.RootElement,synthetic.RootElement);
            else if(mode=="cost")Cost(convergence.RootElement);
            else throw new ArgumentException("Unknown mode");
            Console.WriteLine("BOUNDED PAYOFF "+mode+" PASS; UNBANKED");return 0;
        }
        catch(Exception e){Durable(mode+"-stop.json",new{status="STOP",type=e.GetType().FullName,e.Message});Console.Error.WriteLine(e);return 1;}
    }
}
