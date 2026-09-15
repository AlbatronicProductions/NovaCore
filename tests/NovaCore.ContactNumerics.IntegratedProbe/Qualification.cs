using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using NovaCore.Simulation.Spacecraft.Resources;
using S=PieceKernel.State;
using V=PieceKernel.V;

internal static partial class Qualification
{
    internal static readonly JsonSerializerOptions Json=new(){WriteIndented=true,IncludeFields=true,Converters={new JsonStringEnumConverter()}};
    internal static readonly Load Gravity=new(new(0,-9.81,0),default,default);
    internal static readonly Load Powered=new(Gravity.Gravity,new(0,32,0),default);
    internal static readonly CacheIdentity Identity=new(1,0,1,0,1);
    internal const double MidMass=8+1d/256;
    internal static string Repo="",Output="",Gate="initialization";
    internal static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    internal static void Save(string file,object value)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(value,Json);
        using(var stream=new FileStream(Path.Combine(Output,file),FileMode.CreateNew,FileAccess.Write,FileShare.Read,4096,FileOptions.WriteThrough))
        {stream.Write(bytes);stream.WriteByte(10);stream.Flush(true);}
        Console.WriteLine("PERSISTED "+file);
    }
    internal static CacheDuration Binary(double h){Require(CacheDuration.FromSolver(h,out var d),"binary producing duration");return d;}
    internal static CacheDuration Exact(double fuel,double flow)
    {
        Require(PropellantInteger.TryFromKilograms(fuel,out var n)&&PropellantInteger.TryDecodeFlow(flow,out _),"exact event inputs");
        PropellantInteger.TryDecodeFlow(flow,out var denominator);Require(CacheDuration.FromExact(new(n,denominator),out var h),"exact positive duration");return h;
    }
    internal static InverseBody Body(double mass)=>new(1/mass,new(.5,.5,.5),default);
    internal static CacheDuration ExactCoast()
    {
        PropellantInteger.TryFromKilograms(1d/128,out var fuel);PropellantInteger.TryDecodeFlow(1,out var flow);
        Require(PropellantInteger.TryMultiply(flow,16666,out var full)&&PropellantInteger.TrySubtract(full,fuel,out _),"exact outer interval");
        PropellantInteger.TrySubtract(full,fuel,out var remainder);Require(CacheDuration.FromExact(new(remainder,flow),out var coast),"exact coast remainder");return coast;
    }
    internal static double[] Project(CacheVector cache,bool native=false)=>Enumerable.Range(0,7).Select(i=>native?(double)(float)cache.At(i).Value:cache.At(i).Value).ToArray();
    internal static D3 Vec(JsonElement x)=>new(x.GetProperty("X").GetDouble(),x.GetProperty("Y").GetDouble(),x.GetProperty("Z").GetDouble());
    internal static S State(JsonElement x)
    {var v=Vec(x.GetProperty("Linear"));var w=Vec(x.GetProperty("Angular"));return new(new(v.X,v.Y,v.Z),new(w.X,w.Y,w.Z));}
    internal static double[] Values(JsonElement x)=>x.EnumerateArray().Select(x=>x.GetDouble()).ToArray();
    internal static ContactGeometry Geometry(JsonElement input)
    {
        var c=new Capture{Count=4,Omega=input.GetProperty("currentOmega").GetSingle(),TwiceDamping=2,Friction=.5f,Recovery=2};
        int i=0;foreach(var element in input.GetProperty("rows").EnumerateArray())
        {
            var row=element.GetProperty("row");var n=Vec(row.GetProperty("Normal"));var r=Vec(row.GetProperty("Lever"));
            c.Features[i]=row.GetProperty("Identity").GetInt32();c.Contacts[i]=new(){Normal=new((float)n.X,(float)n.Y,(float)n.Z),
                Offset=new((float)r.X,(float)r.Y,(float)r.Z),Depth=element.GetProperty("depth").GetSingle(),FeatureId=c.Features[i]};i++;
        }
        return ContactGeometry.From(c);
    }
    internal static ContactGeometry FrozenGeometry(JsonElement f)
    {
        var c=new Capture{Count=4,Omega=f.GetProperty("Omega").GetSingle(),TwiceDamping=2,Friction=.5f,Recovery=2};
        int i=0;foreach(var r in f.GetProperty("Contacts").EnumerateArray())
        {
            var n=Vec(r.GetProperty("Normal"));var v=Vec(r.GetProperty("Offset"));c.Features[i]=r.GetProperty("Feature").GetInt32();
            c.Contacts[i]=new(){Normal=new((float)n.X,(float)n.Y,(float)n.Z),Offset=new((float)v.X,(float)v.Y,(float)v.Z),Depth=r.GetProperty("Depth").GetSingle(),FeatureId=c.Features[i]};i++;
        }
        return ContactGeometry.From(c);
    }
    internal static RetainedOperator Owner(ContactGeometry g,CacheDuration h,CacheVector x,Load load,S source)=>
        new(new(Identity,1,0,PieceKind.Preparation,g,h,x,load,source));
    internal static OperatorStatus Prepare(RetainedOperator owner,ContactGeometry g,CacheDuration h,double mass,Load load,S source,
        out RetainedOperator.Proposal proposal,out RetainedOperator.Diagnostics proof)=>owner.Prepare(owner.Snapshot.Identity,owner.Snapshot.Generation,
            owner.Snapshot.Piece+1,load.Force==default&&load.Torque==default?PieceKind.Coast:PieceKind.Powered,g,h,Body(mass),load,source,
            Project(owner.Snapshot.Cache,true),out proposal,out proof);
    internal sealed record Accuracy(double Normal,double Linear,double Angular,double Residual,bool ReferenceAdmissible,bool Pass);
    internal static (Accuracy Error,PieceKernel.OracleResult Reference) Compare(ContactGeometry g,double mass,Load load,S source,CacheDuration h,
        RetainedOperator.Proposal proposed)
    {
        var c=new Capture{Count=4,Omega=(float)g.Omega,TwiceDamping=2,Friction=.5f,Recovery=2};
        for(int i=0;i<4;i++)
        {var n=g.Normal;var r=g.Lever(i);c.Contacts[i]=new(){Normal=new((float)n.X,(float)n.Y,(float)n.Z),Offset=new((float)r.X,(float)r.Y,(float)r.Z),Depth=(float)g.Depth(i)};}
        var patch=new PieceKernel.Patch(c,mass);var acceleration=load.Gravity+(1/mass)*load.Force;var angular=Body(mass).Apply(load.Torque);
        var free=new S(source.Linear+h.Numerical.Value*new V(acceleration.X,acceleration.Y,acceleration.Z),
            source.Angular+h.Numerical.Value*new V(angular.X,angular.Y,angular.Z));
        var reference=PieceKernel.Reference(patch,free,g.Omega,h.Numerical.Value);
        var candidate=proposed.Target;var impulse=Project(candidate.Cache);
        double nerr=Math.Abs(impulse.Take(4).Sum()-reference.Impulses.Take(4).Sum());
        double v=(candidate.Endpoint.Linear-reference.Velocity.Linear).Length,w=(candidate.Endpoint.Angular-reference.Velocity.Angular).Length;
        return(new(nerr,v,w,reference.Residual,reference.Admissible,nerr<=1e-4&&v<=1e-4&&w<=1e-4&&reference.Admissible&&reference.Residual<=1e-12),reference);
    }
    private static void RegressionCase(string name,ContactGeometry old,ContactGeometry current,CacheVector cache,CacheDuration oldH,
        CacheDuration nextH,S source,double mass,Load previous,Load next)
    {
        Gate=name;var owner=Owner(old,oldH,cache,previous,source);var before=owner.Snapshot;
        var status=Prepare(owner,current,nextH,mass,next,source,out var proposed,out var proof);
        if(status!=OperatorStatus.Ready)
        {Save(name+".json",new{status,proof,old,current,oldH,nextH,source,cache});Require(false,"regression admission "+status);}
        var comparison=Compare(current,mass,next,source,nextH,proposed);
        Save(name+".json",new{status=comparison.Error.Pass?"PASS":"FAIL",old,current,oldH,nextH,source,mass,previous,next,proof,
            target=proposed.Target,comparison.Error,comparison.Reference,acceptedUnchanged=owner.Snapshot==before,sweeps=8});
        Require(comparison.Error.Pass&&owner.Snapshot==before,"regression physical bars or speculative nonmutation");
    }
    private static void Regressions()
    {
        string evidence=Path.Combine(Repo,"docs/engineering-evidence/powered-contact-numerical-prerequisite");
        using var duration=JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence,"duration-history/results.json")));
        var f=duration.RootElement.GetProperty("frozen");var g=FrozenGeometry(f);var source=State(f.GetProperty("Source"));
        var cache=CacheVector.From(Values(f.GetProperty("Cache")));
        // Historical load witness used nominal1/60 at both boundaries; duration regression uses actual producing binary.
        RegressionCase("01-original32N",g,g,cache,Binary(1d/60),Binary(1d/60),source,MidMass,Gravity,Powered);
        RegressionCase("02-actual128",g,g,cache,Binary(f.GetProperty("PreviousH").GetDouble()),Exact(1d/128,1),source,MidMass,Gravity,Powered);
        using var capture=JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence,"coast-admission/rejected-input.json")));
        var records=capture.RootElement.GetProperty("records").EnumerateArray().ToArray();
        var old=records.Single(x=>x.GetProperty("label").GetString()=="original_owned_powered_piece").GetProperty("inputs");
        var coast=records.Single(x=>x.GetProperty("label").GetString()=="owned_coast_continuation").GetProperty("inputs");
        var oldg=Geometry(old);var newg=Geometry(coast);var poweredCache=CacheVector.From(Values(coast.GetProperty("cacheValues")));
        Span<Scaled> oldx=stackalloc Scaled[7],newx=stackalloc Scaled[7];for(int i=0;i<7;i++)oldx[i]=poweredCache.At(i);
        Gate="03-basis";bool moved=GeneralizedTransport.TryMove(oldg,newg,oldx,newx,out var transport);
        Save("03-basis.json",new{status=moved?"PASS":"FAIL",transport,source=poweredCache,result=CacheVector.FromScales(newx),bar=1e-12});
        Require(moved&&transport.LinearError<=1e-12&&transport.AngularError<=1e-12,"basis generalized impulse preservation");
        RegressionCase("04-exact-coast",oldg,newg,poweredCache,Exact(1d/128,1),ExactCoast(),State(coast.GetProperty("source")),8,Powered,Gravity);
        using var synthetic=JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence,"basis-compatibility/synthetic-results.json")));
        foreach(string name in new[]{"moderate-normal","normal-and-tangent"})
        {
            Gate=name;var item=synthetic.RootElement.GetProperty("cases").EnumerateArray().Single(x=>x.GetProperty("Name").GetString()==name);
            ContactGeometry ReadBasis(JsonElement b)=>new(new(0,1,2,3),Vec(b.GetProperty("normal")),Vec(b.GetProperty("tangent1")),Vec(b.GetProperty("tangent2")),
                Vec(b.GetProperty("offsets")[0]),Vec(b.GetProperty("offsets")[1]),Vec(b.GetProperty("offsets")[2]),Vec(b.GetProperty("offsets")[3]),(double).0004f,(double).0004f,(double).0004f,(double).0004f,g.Omega,.5,2,2);
            var og=ReadBasis(item.GetProperty("oldBasis"));var ng=ReadBasis(item.GetProperty("newBasis"));
            var seed=CacheVector.From(Values(synthetic.RootElement.GetProperty("seed")));var h=Binary(17707d/2000000);
            var load=new Load((-9.81)*ng.Normal,default,default);var owner=Owner(og,h,seed,load,default);var before=owner.Snapshot;
            var status=Prepare(owner,ng,h,8,load,default,out _,out var proof);
            Save("05-"+name+".json",new{status=status.ToString(),expected="DRefusal",proof,acceptedUnchanged=before==owner.Snapshot,solverCalls=0,referenceCalls=0});
            Require(status==OperatorStatus.DRefusal&&before==owner.Snapshot,"known friction-infeasible refusal");
        }
        Gate="lifecycle";var life=Owner(g,Binary(1d/60),cache,Gravity,source);var initial=life.Snapshot;
        Require(Prepare(life,g,Exact(1d/128,1),MidMass,Powered,source,out var p1,out _)==OperatorStatus.Ready,"first speculative piece");
        Require(Prepare(life,g,Binary(1d/60),MidMass,Powered,source,out var p2,out _)==OperatorStatus.Ready&&life.Snapshot==initial,"second speculative piece");
        var endpoint=p1.Target.Endpoint;var projection=new S(new((float)endpoint.Linear.X,(float)endpoint.Linear.Y,(float)endpoint.Linear.Z),new((float)endpoint.Angular.X,(float)endpoint.Angular.Y,(float)endpoint.Angular.Z));
        Require(life.BeginInstall(p1,projection)==OperatorStatus.Ready&&life.CompleteInstall(p2)==OperatorStatus.InstallFailure&&life.Invalidated&&life.Snapshot==initial,"foreign prepared completion poisons without promotion");
        Save("06-lifecycle.json",new{status="PASS",speculativeStateUnchanged=true,wrongPreparedCompletion="InstallFailure",invalidated=true,acceptedGeneration=life.Snapshot.Generation});
        Save("regression-results.json",new{status="PASS",cases=7,sweeps=8,physicalBar=1e-4,transportBar=1e-12,productionMutations=0});
    }
    private static void Main(string[] args)
    {
        Repo=Path.GetFullPath(args[1]);Output=Path.GetFullPath(args[2]);Directory.CreateDirectory(Output);
        Save(args[0]+"-attempt.json",new{mode=args[0],runtime=RuntimeInformation.FrameworkDescription,process=Environment.ProcessId,thread=Environment.CurrentManagedThreadId});
        try
        {
            if(args[0]=="regressions")Regressions();
            else if(args[0]=="sequence")Sequence();
            else if(args[0]=="tiny")Tiny();
            else throw new ArgumentException("Unregistered qualification phase");
        }
        catch(Exception e)
        {Save(args[0]+"-stop.json",new{status="STOP",firstGate=Gate,error=e.Message,type=e.GetType().Name});Environment.ExitCode=1;Console.WriteLine("STOP "+Gate+": "+e.Message);}
    }
}
