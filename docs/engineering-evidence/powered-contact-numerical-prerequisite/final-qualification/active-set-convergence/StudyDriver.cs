using System.Globalization;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Resources;
using S=PieceKernel.State;
using V=PieceKernel.V;

internal static partial class StudyDriver
{
    internal sealed record Input(OwnedState Seed,ContactGeometry Geometry,CacheDuration Duration,InverseBody Body,Load Load,double[] Native,double[] Reference,double[] Endpoint);
    internal sealed record Measure(string Arm,int Sweeps,bool D,double NormalError,double TangentError,double TwistError,double LinearError,double AngularError,double Residual,bool Pass,int NormalClamps,int TangentClamps,int TwistClamps,double[] Impulses,S Endpoint,RetainedOperator.Diagnostics Preparation);
    internal static T Read<T>(JsonElement x)=>JsonSerializer.Deserialize<T>(x.GetRawText(),Qualification.Json)!;
    internal static void Require(bool p,string m)=>Qualification.Require(p,m);
    internal static double[] Values(CacheVector x)=>Enumerable.Range(0,7).Select(i=>x.At(i).Value).ToArray();
    internal static CacheVector Cache(double[] x)=>CacheVector.FromScales(x.Select(Scaled.From).ToArray());
    internal static double[] Numbers(JsonElement x)=>x.EnumerateArray().Select(v=>double.Parse(v.GetString()!,CultureInfo.InvariantCulture)).ToArray();
    internal static Input Actual(string repo)
    {
        string dir=Path.Combine(repo,"docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification");
        using var history=JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"friction-boundary-continuation/results/baseline-historical.json")));
        using var refs=JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"tiny-active-friction/coast-reference.json")));
        using var native=JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"tiny-active-friction/candidate-results/baseline-coast-input.json")));
        var j=history.RootElement.GetProperty("seed");var oldH=Qualification.Exact(double.Epsilon,2);
        Require(PropellantInteger.TryMultiply(oldH.Exact.Denominator,16666,out var total)&&PropellantInteger.TrySubtract(total,oldH.Exact.Numerator,out _),"duration arithmetic");
        PropellantInteger.TrySubtract(total,oldH.Exact.Numerator,out var remain);
        Require(CacheDuration.FromExact(new(remain,oldH.Exact.Denominator),out var h),"duration provenance");
        var seed=new OwnedState(Read<CacheIdentity>(j.GetProperty("Identity")),j.GetProperty("Generation").GetInt64(),j.GetProperty("Piece").GetInt64(),
            Enum.Parse<PieceKind>(j.GetProperty("Kind").GetString()!),Read<ContactGeometry>(j.GetProperty("ProducingGeometry")),oldH,
            Read<CacheVector>(j.GetProperty("Cache")),Read<Load>(j.GetProperty("Load")),Qualification.State(j.GetProperty("Endpoint")));
        var reference=refs.RootElement.GetProperty("baseline").GetProperty("installedSourceReference");
        var nativeValues=Qualification.Values(native.RootElement.GetProperty("nativeCache"));
        Require(seed.Cache.MatchesTransport(nativeValues),"saved native signed-bit identity");
        return new(seed,Read<ContactGeometry>(history.RootElement.GetProperty("current")),h,Qualification.Body(8),Qualification.Gravity,nativeValues,Numbers(reference.GetProperty("impulses")),Numbers(reference.GetProperty("endpoint")));
    }
    internal static RetainedOperator.Proposal Prepare(RetainedOperator owner,Input input)
    {
        var s=owner.Snapshot;var status=owner.Prepare(s.Identity,s.Generation,s.Piece+1,PieceKind.Coast,input.Geometry,input.Duration,input.Body,input.Load,s.Endpoint,input.Native,out var p,out _);
        Require(status==OperatorStatus.Ready,"Unexpected preparation refusal: "+status);Require(owner.Snapshot==input.Seed,"accepted owner mutated");return p;
    }
    internal static Measure Run(Input input,string arm,int sweeps,bool d=true,bool trace=false)
    {
        StudyControl.Sweeps=sweeps;StudyControl.UseD=d;StudyControl.Trace=trace;
        var owner=new RetainedOperator(input.Seed);var p=Prepare(owner,input);var values=Values(p.Target.Cache);var r=input.Reference;var c=p.Target.Endpoint;
        double n=Math.Abs(values.Take(4).Sum()-r.Take(4).Sum()),t=Math.Sqrt(Math.Pow(values[4]-r[4],2)+Math.Pow(values[5]-r[5],2)),w=Math.Abs(values[6]-r[6]);
        double le=(c.Linear-new V(input.Endpoint[0],input.Endpoint[1],input.Endpoint[2])).Length,ae=(c.Angular-new V(input.Endpoint[3],input.Endpoint[4],input.Endpoint[5])).Length;
        var patch=owner.Current;var g=input.Geometry;double h=input.Duration.Numerical.Value,alpha=1/(g.Omega*h*(g.Omega*h+2)),res=0;
        for(int i=0;i<7;i++){double e=patch.J(i,c);if(i<4)e+=alpha*patch.K[i,i]*values[i]-Math.Min(g.Depth(i)/h,Math.Min(g.Depth(i)/(h+2/g.Omega),2));res=Math.Max(res,Math.Abs(e));}
        return new(arm,sweeps,d,n,t,w,le,ae,res,n<=1e-4&&le<=1e-4&&ae<=1e-4,p.Proof.NormalClamps,p.Proof.TangentClamps,p.Proof.TwistClamps,values,c,p.Proof);
    }
    private static void ExactReproduction(Input input,string repo)
    {
        using var old=JsonDocument.Parse(File.ReadAllText(Path.Combine(repo,"docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/friction-boundary-continuation/results/baseline-coast.json")));
        var j=old.RootElement;var a=Run(input,"generated-eight",8);var b=Run(input,"generated-eight-traced",8,true,true);
        Require(a.Impulses.SequenceEqual(Values(Read<CacheVector>(j.GetProperty("target").GetProperty("Cache"))))&&a.Endpoint==Qualification.State(j.GetProperty("target").GetProperty("Endpoint")),"generated endpoint/cache not exact original");
        Require(a.NormalError==j.GetProperty("normalError").GetDouble()&&a.LinearError==j.GetProperty("linearError").GetDouble()&&a.AngularError==j.GetProperty("angularError").GetDouble()&&a.TangentError==j.GetProperty("tangentError").GetDouble()&&a.TwistError==j.GetProperty("twistError").GetDouble()&&a.Residual==j.GetProperty("residual").GetDouble(),"comparison drift");
        Require(a.Impulses.SequenceEqual(b.Impulses)&&a.Endpoint==b.Endpoint&&a.Preparation==b.Preparation,"trace changes arithmetic");
        Qualification.Save("harness-equivalence.json",new{status="PASS",a,b,source="Frozen baseline; trace off/on exact output"});
        Qualification.Save("component-trace.json",StudyControl.Rows.ToArray());StudyControl.Trace=false;
    }
    private static double[] PredictTangent(Input input,CacheVector prepared)
    {
        var patch=new CurrentPatch();patch.Set(input.Geometry,input.Body);double h=input.Duration.Numerical.Value;
        var free=new S(input.Seed.Endpoint.Linear+h*new V(input.Load.Gravity.X+input.Body.Mass*input.Load.Force.X,input.Load.Gravity.Y+input.Body.Mass*input.Load.Force.Y,input.Load.Gravity.Z+input.Body.Mass*input.Load.Force.Z),
            input.Seed.Endpoint.Angular+h*new V(input.Body.Apply(input.Load.Torque).X,input.Body.Apply(input.Load.Torque).Y,input.Body.Apply(input.Load.Torque).Z));
        double v0=patch.J(4,free),v1=patch.J(5,free);for(int j=0;j<7;j++)if(j is not 4 and not 5){v0+=patch.K[4,j]*prepared.At(j).Value;v1+=patch.K[5,j]*prepared.At(j).Value;}
        double det=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];return [(-patch.K[5,5]*v0+patch.K[4,5]*v1)/det,(patch.K[5,4]*v0-patch.K[4,4]*v1)/det];
    }
    private static void Curves(Input input)
    {
        List<Measure> rows=new();List<int> tested=new();int first=0;
        foreach(int count in new[]{1,2,4,8,9,10,11,12,16,24,32}){
            var m=Run(input,"actual",count);rows.Add(m);tested.Add(count);
            Console.WriteLine($"D+{count}: pass={m.Pass} N={m.NormalError:R} V={m.LinearError:R} W={m.AngularError:R}");
            if(count>=9&&m.Pass){first=count;break;}
        }
        Require(first>0,"No bounded passing count through32; stop payoff");
        var noD=new List<Measure>();foreach(int count in tested)noD.Add(Run(input,"no-D",count,false));
        Qualification.Save("convergence.json",new{firstTestedPass=first,rows,noD});
        var traced=Run(input,"first-pass-trace",first,true,true);Qualification.Save("first-pass-trace.json",StudyControl.Rows.ToArray());StudyControl.Trace=false;
        var controls=new List<Measure>();
        foreach(var scale in new[]{.5,0d}){
            var x=Enumerable.Range(0,7).Select(i=>input.Seed.Cache.At(i).Times(i<4?1:scale)).ToArray();var seed=input.Seed with{Cache=CacheVector.FromScales(x)};
            controls.Add(Run(input with{Seed=seed,Native=Enumerable.Range(0,7).Select(i=>(double)(float)seed.Cache.At(i).Value).ToArray()},scale==0?"diagnostic-zero-friction-history":"diagnostic-half-friction-history",8));
        }
        StudyControl.AllowCold=true;controls.Add(Run(input with{Seed=input.Seed with{Cache=default},Native=new double[7]},"diagnostic-cold-history",8));StudyControl.AllowCold=false;
        var afterD=rows.Single(x=>x.Sweeps==8).Preparation.AfterD;
        foreach(string arm in new[]{"zero-tangent","current-block-prediction","oracle-reference-tangent"}){
            StudyControl.TangentOverride=arm=="zero-tangent"?[0,0]:arm=="current-block-prediction"?PredictTangent(input,afterD):[input.Reference[4],input.Reference[5]];
            controls.Add(Run(input,arm,8));
        }
        StudyControl.TangentOverride=null;
        Qualification.Save("initialization-controls.json",new{warning="Diagnostic cache counterfactuals, not accepted historical physical sequences; oracle explicitly labeled",controls});
        // Export fixed equations for independent error-propagation reconstruction.
        var patch=new CurrentPatch();patch.Set(input.Geometry,input.Body);var owner=new RetainedOperator(input.Seed);StudyControl.Sweeps=8;StudyControl.UseD=true;var p=Prepare(owner,input);
        var h=input.Duration.Numerical.Value;var g=input.Geometry;var a=h*g.Omega;var k=a*(a+2);var c=k/(1+k);
        Qualification.Save("fixed-equations.json",new{geometry=g,h,omega=g.Omega,c,alpha=1/k,K=Enumerable.Range(0,7).Select(i=>Enumerable.Range(0,7).Select(j=>patch.K[i,j]).ToArray()).ToArray(),
            reference=input.Reference,referenceEndpoint=input.Endpoint,source=input.Seed.Endpoint,load=input.Load,body=input.Body,
            initial=Values(p.Proof.AfterD),beforeD=Values(p.Proof.BeforeD),delta=p.Proof.DDelta.Value,
            linearRows=patch.Linear,angularRows=patch.Angular,
            bias=Enumerable.Range(0,4).Select(i=>Math.Min(g.Depth(i)/h,Math.Min(g.Depth(i)/(h+2/g.Omega),2))).ToArray()});
    }
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);Require(!Directory.Exists(Qualification.Output),"fresh output only");Directory.CreateDirectory(Qualification.Output);
        try{var input=Actual(Qualification.Repo);ExactReproduction(input,Qualification.Repo);Curves(input);}
        catch(Exception e){Qualification.Save("stop.json",new{status="STOP",cause=e.ToString()});Environment.ExitCode=1;Console.WriteLine(e);}
    }
}
