using System.Text.Json;
using System.Globalization;
using System.Runtime.InteropServices;
using NovaCore.Simulation.Spacecraft.Resources;
using S=PieceKernel.State;
using V=PieceKernel.V;

internal static class BoundaryDriver
{
    private static void Require(bool b,string m)=>Qualification.Require(b,m);
    private static T Read<T>(JsonElement x)=>JsonSerializer.Deserialize<T>(x.GetRawText(),Qualification.Json)!;
    private static Scaled[] Scales(CacheVector x)=>Enumerable.Range(0,7).Select(x.At).ToArray();
    private static Load Load(JsonElement x)=>new(Qualification.Vec(x.GetProperty("Gravity")),Qualification.Vec(x.GetProperty("Force")),Qualification.Vec(x.GetProperty("Torque")));
    private static double[] Reference(JsonElement x,string name)=>x.GetProperty(name).EnumerateArray().Select(v=>double.Parse(v.GetString()!,CultureInfo.InvariantCulture)).ToArray();
    private static void CheckDuration(JsonElement saved,CacheDuration d)
    {
        Require(saved.GetProperty("exactPositive").GetBoolean()&&saved.GetProperty("valid").GetBoolean(),"saved exact positive duration");
        for(int i=0;i<PropellantInteger.LimbCount;i++)
        {
            Require(d.Exact.Numerator.Limb(i).ToString("X16")==saved.GetProperty("numeratorLimbs")[i].GetString(),"exact duration numerator");
            Require(d.Exact.Denominator.Limb(i).ToString("X16")==saved.GetProperty("denominatorLimbs")[i].GetString(),"exact duration denominator");
        }
    }
    private static CacheDuration Coast(CacheDuration tiny)
    {
        Require(PropellantInteger.TryMultiply(tiny.Exact.Denominator,16666,out var full),"outer exact ticks");
        Require(PropellantInteger.TrySubtract(full,tiny.Exact.Numerator,out var remainder),"exact remainder");
        Require(CacheDuration.FromExact(new(remainder,tiny.Exact.Denominator),out var result),"coast duration");return result;
    }
    internal sealed record Input(string Branch,OwnedState Seed,ContactGeometry Current,CacheDuration Coast,double[] Native,JsonElement Reference);
    private static Input InputFrom(string branch,string old,JsonElement coastRef)
    {
        using var receipt=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"candidate-results",branch+"-installed.json")));
        using var coast=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"candidate-results",branch+"-coast-input.json")));
        using var comparison=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"tiny-comparison.json")));
        var a=receipt.RootElement.GetProperty("after");var ci=coast.RootElement;
        Require(receipt.RootElement.GetProperty("status").GetString()=="PASS"&&comparison.RootElement.GetProperty("status").GetString()=="PASS", "accepted solve and installation evidence");
        Require(JsonElement.DeepEquals(a,ci.GetProperty("accepted")),"same branch accepted/coast tuple");
        var h=Qualification.Exact(double.Epsilon,2);var next=Coast(h);
        CheckDuration(receipt.RootElement.GetProperty("duration"),h);CheckDuration(ci.GetProperty("duration"),next);
        var cache=Read<CacheVector>(a.GetProperty("Cache"));var g=Read<ContactGeometry>(a.GetProperty("ProducingGeometry"));
        var source=Qualification.State(a.GetProperty("Endpoint"));var identity=Read<CacheIdentity>(a.GetProperty("Identity"));
        var seed=new OwnedState(identity,a.GetProperty("Generation").GetInt64(),a.GetProperty("Piece").GetInt64(),
            Enum.Parse<PieceKind>(a.GetProperty("Kind").GetString()!),g,h,cache,Load(a.GetProperty("Load")),source);
        var current=Read<ContactGeometry>(ci.GetProperty("geometry"));var native=Qualification.Values(ci.GetProperty("nativeCache"));
        Require(seed.Generation==2&&seed.Piece==1&&seed.Identity.Valid&&g.Valid&&current.Valid&&cache.Valid&&
            cache.MatchesTransport(native)&&source==Qualification.State(ci.GetProperty("source")),"saved lineage/basis/cache/source identity");
        var classification=BoundaryPolicy.Classify(g,Scales(cache));
        Qualification.Save(branch+"-historical.json",new{status=classification.ClosedFeasible?"VALID HISTORICAL CACHE":"INFEASIBLE",seed,current,
            classification,sourceReceipt=branch+"-installed.json",referenceComparison="prior frozen tiny comparison PASS",
            exactDurationChecked=true,geometryLineageChecked=true,acceptedInstallChecked=true,canonicalCapabilities=0});
        Require(classification.ClosedFeasible,"historical cache is outside the consistent closed feasible set");
        return new(branch,seed,current,next,native,coastRef.GetProperty(branch).GetProperty("installedSourceReference").Clone());
    }
    private static RetainedOperator.Proposal Run(Input input)
    {
        BoundaryPolicy.Branch=input.Branch;BoundaryPolicy.AuthenticatedHistoricalReceipt=true;
        var owner=new RetainedOperator(input.Seed);var before=owner.Snapshot;
        var status=owner.Prepare(before.Identity,before.Generation,before.Piece+1,PieceKind.Coast,input.Current,input.Coast,
            Qualification.Body(8),Qualification.Gravity,before.Endpoint,input.Native,out var p,out var proof);
        Qualification.Save(input.Branch+"-preparation.json",new{status,proof,acceptedUnchanged=owner.Snapshot==before,
            solverAdmissions=BoundaryPolicy.SolverAdmissions,canonicalMutations=0});
        Require(status==OperatorStatus.Ready,"first material diagnostic result "+input.Branch+": "+status);
        var expected=Reference(input.Reference,"endpoint");var refImpulse=Reference(input.Reference,"impulses");
        var c=p.Target.Endpoint;var lv=new V(expected[0],expected[1],expected[2]);var av=new V(expected[3],expected[4],expected[5]);
        var actual=Qualification.Project(p.Target.Cache);
        double normal=Math.Abs(actual.Take(4).Sum()-refImpulse.Take(4).Sum());
        double tangent=Math.Sqrt(Math.Pow(actual[4]-refImpulse[4],2)+Math.Pow(actual[5]-refImpulse[5],2));
        double twist=Math.Abs(actual[6]-refImpulse[6]);double linear=(c.Linear-lv).Length,angular=(c.Angular-av).Length;
        var patch=owner.Current;double h=input.Coast.Numerical.Value,alpha=1/(input.Current.Omega*h*(input.Current.Omega*h+2));double residual=0;
        for(int i=0;i<7;i++)
        {
            double e=patch.J(i,c);
            if(i<4)e+=alpha*patch.K[i,i]*actual[i]-Math.Min(input.Current.Depth(i)/h,Math.Min(input.Current.Depth(i)/(h+2/input.Current.Omega),2));
            residual=Math.Max(residual,Math.Abs(e));
        }
        bool pass=normal<=1e-4&&linear<=1e-4&&angular<=1e-4;
        Qualification.Save(input.Branch+"-coast.json",new{status=pass?"PASS":"FAIL",target=p.Target,increments=new[]{p.LinearIncrement,p.AngularIncrement},
            normalError=normal,tangentError=tangent,twistError=twist,linearError=linear,angularError=angular,residual,
            finalClassification=BoundaryPolicy.Classify(input.Current,Scales(p.Target.Cache)),proof,acceptedUnchanged=owner.Snapshot==before});
        Require(pass,"coast physical errors");
        return p;
    }
    private static void Pair(Input b,Input p,RetainedOperator.Proposal bc,RetainedOperator.Proposal pc)
    {
        var bs=bc.Target.Endpoint;var ps=pc.Target.Endpoint;
        double[] End(S s)=>[s.Linear.X,s.Linear.Y,s.Linear.Z,s.Angular.X,s.Angular.Y,s.Angular.Z];
        Scaled[] Inc(RetainedOperator.Proposal q)=>[q.LinearIncrement.X,q.LinearIncrement.Y,q.LinearIncrement.Z,q.AngularIncrement.X,q.AngularIncrement.Y,q.AngularIncrement.Z];
        var be=End(bs);var pe=End(ps);var br=Reference(b.Reference,"endpoint");var pr=Reference(p.Reference,"endpoint");
        var endpoint=Enumerable.Range(0,6).Select(i=>(double)(float)pe[i]-(double)(float)be[i]==(double)(float)pr[i]-(double)(float)br[i]).ToArray();
        var rows=new List<object>();bool all=endpoint.All(x=>x);
        foreach(string field in new[]{"impulses","increment"})
        {
            var brf=Reference(b.Reference,field);var prf=Reference(p.Reference,field);
            var bcf=field=="impulses"?Scales(bc.Target.Cache):Inc(bc);var pcf=field=="impulses"?Scales(pc.Target.Cache):Inc(pc);
            for(int i=0;i<brf.Length;i++)
            {
                var target=ScaleMath.Subtract(Scaled.From(prf[i]),Scaled.From(brf[i]));
                var actual=ScaleMath.Subtract(pcf[i],bcf[i]);var error=ScaleMath.Abs(ScaleMath.Subtract(actual,target));
                double referenceSize=Math.Max(Math.Abs(brf[i]),Math.Abs(prf[i]));
                var ulp=referenceSize==0?default:new Scaled(1,Math.ILogB(referenceSize)-52);
                bool pass=ScaleMath.Compare(error,ulp)<=0;all&=pass;
                rows.Add(new{field,component=i,target,actual,error,referenceSizedUlp=ulp,pass});
            }
        }
        Qualification.Save("paired-response.json",new{status=all?"PASS":"FAIL",endpointNativeDifferences=endpoint,rows});
        Require(all,"previously frozen coast represented branch-response bars");
    }
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);
        Require(!Directory.Exists(Qualification.Output),"fresh diagnostic output");Directory.CreateDirectory(Qualification.Output);
        Qualification.Save("attempt.json",new{runtime=RuntimeInformation.FrameworkDescription,process=Environment.ProcessId,
            thread=Environment.CurrentManagedThreadId,kind="saved accepted tuple diagnostic; no native/canonical world",sweeps=12});
        try
        {
            var old=Path.Combine(Qualification.Repo,"docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/tiny-active-friction");
            using var refs=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"coast-reference.json")));
            var baseline=InputFrom("baseline",old,refs.RootElement);var powered=InputFrom("powered",old,refs.RootElement);
            var b=Run(baseline);
            if(args.Length>2&&args[2]=="baseline"){Console.WriteLine("BASELINE_TWELVE_PASS");return;}
            var p=Run(powered);Pair(baseline,powered,b,p);
            Console.WriteLine("BOTH_COAST_BRANCHES_PASSED: stop before counter-witness phase review.");
        }
        catch(Exception e)
        {
            Qualification.Save("stop.json",new{status="STOP",cause=e.Message,type=e.GetType().Name,solverAdmissions=BoundaryPolicy.SolverAdmissions,
                canonicalMutations=0,permanentSourceChanges=0});Console.WriteLine("STOP "+e.Message);Environment.ExitCode=1;
        }
    }
}
