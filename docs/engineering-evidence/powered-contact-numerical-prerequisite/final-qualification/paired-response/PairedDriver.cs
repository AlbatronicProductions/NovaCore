using System.Text.Json;
using S=PieceKernel.State;
using V=PieceKernel.V;

// Cold observation only; no callback value feeds the selected arithmetic.
internal static class ProbeTrace
{
    internal static int Sweeps=12;
    internal static bool UseD=true;
    internal static readonly List<object> Stages=new();
    internal static object? Equation;
    internal static void Reset(int sweeps,bool d){Sweeps=sweeps;UseD=d;Stages.Clear();Equation=null;}
    internal static void Take(string stage,CacheVector cache)=>Stages.Add(new{stage,values=PairedDriver.Cache(cache)});
    internal static void Equations(CurrentPatch p,S source,S free,CacheDuration duration,InverseBody body,Load load,bool ordinary)
    {
        Equation=new{source,free,geometry=p.Geometry,body,load,ordinary,
            durationSignificand=duration.Numerical.Significand,durationExponent=duration.Numerical.Exponent,
            h=duration.Numerical.Value,K=Enumerable.Range(0,7).Select(i=>Enumerable.Range(0,7).Select(j=>p.K[i,j]).ToArray()).ToArray(),
            linear=p.Linear.ToArray(),angular=p.Angular.ToArray(),freeJ=Enumerable.Range(0,7).Select(i=>p.J(i,free)).ToArray()};
    }
}

internal static class PairedDriver
{
    internal static Scaled[] Cache(CacheVector c)=>Enumerable.Range(0,7).Select(c.At).ToArray();
    private static Scaled[] Inc(RetainedOperator.Proposal p)=>[p.LinearIncrement.X,p.LinearIncrement.Y,p.LinearIncrement.Z,p.AngularIncrement.X,p.AngularIncrement.Y,p.AngularIncrement.Z];
    private static double[] End(S s)=>[s.Linear.X,s.Linear.Y,s.Linear.Z,s.Angular.X,s.Angular.Y,s.Angular.Z];
    private static S Project(S s)=>new(new((float)s.Linear.X,(float)s.Linear.Y,(float)s.Linear.Z),new((float)s.Angular.X,(float)s.Angular.Y,(float)s.Angular.Z));
    private static double[] Native(CacheVector c)=>Cache(c).Select(x=>(double)(float)x.Value).ToArray();
    private static T Read<T>(JsonElement e)=>JsonSerializer.Deserialize<T>(e.GetRawText(),Qualification.Json)!;
    private static void Require(bool b,string m)=>Qualification.Require(b,m);
    private static bool Same<T>(T a,T b)=>JsonSerializer.Serialize(a,Qualification.Json)==JsonSerializer.Serialize(b,Qualification.Json);
    private sealed record Result(string Name,OperatorStatus Status,RetainedOperator.Proposal Proposal,object? Equations,object[] Stages,object Summary);
    private static Result Run(BoundaryDriver.Input input,string name,int sweeps=12,bool d=true)
    {
        ProbeTrace.Reset(sweeps,d);
        var owner=new RetainedOperator(input.Seed);var s=input.Seed;
        var status=owner.Prepare(s.Identity,s.Generation,s.Piece+1,PieceKind.Coast,input.Current,input.Coast,
            Qualification.Body(8),Qualification.Gravity,s.Endpoint,input.Native,out var p,out var proof);
        Require(owner.Snapshot==s,"speculative owner nonmutation "+name);
        var equations=ProbeTrace.Equation;var stages=ProbeTrace.Stages.ToArray();
        var actual=Cache(p.Target.Cache).Select(x=>x.Value).ToArray();
        var expected=input.Reference.GetProperty("endpoint").EnumerateArray().Select(x=>double.Parse(x.GetString()!,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var ri=input.Reference.GetProperty("impulses").EnumerateArray().Select(x=>double.Parse(x.GetString()!,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        double h=input.Coast.Numerical.Value,alpha=1/(input.Current.Omega*h*(input.Current.Omega*h+2)),residual=0;
        if(status==OperatorStatus.Ready)for(int i=0;i<7;i++)
        {
            double e=owner.Current.J(i,p.Target.Endpoint);
            if(i<4)e+=alpha*owner.Current.K[i,i]*actual[i]-Math.Min(input.Current.Depth(i)/h,Math.Min(input.Current.Depth(i)/(h+2/input.Current.Omega),2));
            residual=Math.Max(residual,Math.Abs(e));
        }
        object summary=new{name,status,sweeps,d,cache=Cache(p.Target.Cache),increments=Inc(p),endpoint=p.Target.Endpoint,
            nativeBits=End(Project(p.Target.Endpoint)).Select(BitConverter.DoubleToUInt64Bits).ToArray(),proof,
            normalError=Math.Abs(actual.Take(4).Sum()-ri.Take(4).Sum()),
            linearError=(p.Target.Endpoint.Linear-new V(expected[0],expected[1],expected[2])).Length,
            angularError=(p.Target.Endpoint.Angular-new V(expected[3],expected[4],expected[5])).Length,residual,
            acceptedUnchanged=owner.Snapshot==s};
        return new(name,status,p,equations,stages,summary);
    }
    private static void MatchFrozen(Result r,string branch,string path)
    {
        using var f=JsonDocument.Parse(File.ReadAllText(Path.Combine(path,branch+"-coast.json")));
        Require(r.Status==OperatorStatus.Ready,"reproduction Ready "+branch);
        Require(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(r.Proposal.Target,Qualification.Json),f.RootElement.GetProperty("target")),"instrumented target differs "+branch);
        Require(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(new[]{r.Proposal.LinearIncrement,r.Proposal.AngularIncrement},Qualification.Json),f.RootElement.GetProperty("increments")),"instrumented increment differs "+branch);
        Require(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(r.Proposal.Proof,Qualification.Json),f.RootElement.GetProperty("proof")),"instrumented proof differs "+branch);
    }
    private static void NullControl(BoundaryDriver.Input b,string old)
    {
        using var f=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"candidate-results/baseline-tiny.json")));
        var root=f.RootElement;var src=root.GetProperty("source");
        var seed=new OwnedState(Read<CacheIdentity>(src.GetProperty("Identity")),src.GetProperty("Generation").GetInt64(),src.GetProperty("Piece").GetInt64(),PieceKind.Preparation,
            Read<ContactGeometry>(src.GetProperty("ProducingGeometry")),Qualification.Binary((double)(1f/60)),Read<CacheVector>(src.GetProperty("Cache")),Qualification.Gravity,Qualification.State(src.GetProperty("Endpoint")));
        var geometry=Read<ContactGeometry>(root.GetProperty("geometry"));
        var pairs=new List<object>();var outputs=new List<Result>();
        foreach(var kind in new[]{PieceKind.Coast,PieceKind.Powered})
        {
            ProbeTrace.Reset(12,true);var owner=new RetainedOperator(seed);
            var status=owner.Prepare(seed.Identity,seed.Generation,seed.Piece+1,kind,geometry,Qualification.Exact(double.Epsilon,2),Qualification.Body(8),Qualification.Gravity,
                seed.Endpoint,Native(seed.Cache),out var p,out _);
            Require(status==OperatorStatus.Ready,"zero-input tiny path Ready");
            Require(owner.BeginInstall(p,Project(p.Target.Endpoint))==OperatorStatus.Ready&&owner.CompleteInstall(p)==OperatorStatus.Ready,"diagnostic owner install");
            var next=b with{Seed=owner.Snapshot,Native=Native(owner.Snapshot.Cache)};
            var result=Run(next,"zero-input-"+kind);Require(result.Status==OperatorStatus.Ready,"null coast Ready");outputs.Add(result);
            pairs.Add(new{kind,tinyCache=Cache(p.Target.Cache),tinyIncrements=Inc(p),tinyEndpoint=p.Target.Endpoint,accepted=owner.Snapshot,coast=result.Summary});
        }
        Require(Same(Cache(outputs[0].Proposal.Target.Cache),Cache(outputs[1].Proposal.Target.Cache))&&Same(Inc(outputs[0].Proposal),Inc(outputs[1].Proposal))&&
            Same(outputs[0].Proposal.Target.Endpoint,outputs[1].Proposal.Target.Endpoint),"zero-contribution powered-kind path alters physical results");
        Qualification.Save("null-event-control.json",new{status="PASS",zeroEngineForce=true,sameTinyDuration=true,samePhysicalInputs=true,
            difference="PieceKind.Coast versus PieceKind.Powered only; diagnostic prepare/begin/complete/coast lifecycle",pairs,nativeWorldInstallations=0});
    }
    private static void Downstream(BoundaryDriver.Input b,BoundaryDriver.Input p,Result rb,Result rp)
    {
        var results=new List<object>();
        foreach(var pair in new[]{(Input:b,Output:rb),(Input:p,Output:rp)})
        {
            var owner=new RetainedOperator(pair.Input.Seed); // Reconstitute the exact accepted diagnostic source.
            ProbeTrace.Reset(12,true);var s=owner.Snapshot;
            var status=owner.Prepare(s.Identity,s.Generation,s.Piece+1,PieceKind.Coast,pair.Input.Current,pair.Input.Coast,Qualification.Body(8),Qualification.Gravity,
                s.Endpoint,pair.Input.Native,out var first,out _);
            Require(status==OperatorStatus.Ready&&Same(first.Target,pair.Output.Proposal.Target),"downstream source matches frozen D12 output");
            Require(owner.BeginInstall(first,Project(first.Target.Endpoint))==OperatorStatus.Ready&&owner.CompleteInstall(first)==OperatorStatus.Ready,"diagnostic tuple promotion");
            var before=owner.Snapshot;ProbeTrace.Reset(12,true);
            var duration=Qualification.Exact(1,60);
            status=owner.Prepare(before.Identity,before.Generation,before.Piece+1,PieceKind.Coast,b.Current,duration,Qualification.Body(8),Qualification.Gravity,
                before.Endpoint,Native(before.Cache),out var next,out var proof);
            results.Add(new{branch=pair.Input.Branch,status,before,cache=Cache(next.Target.Cache),increments=Inc(next),endpoint=next.Target.Endpoint,
                nativeBits=End(Project(next.Target.Endpoint)).Select(BitConverter.DoubleToUInt64Bits).ToArray(),proof,acceptedUnchanged=owner.Snapshot==before,
                equations=ProbeTrace.Equation});
        }
        Qualification.Save("downstream-sensitivity.json",new{kind="one identical later constrained equation piece; diagnostic owner only",duration="1/60 second exact",
            geometry=b.Current,body=Qualification.Body(8),load=Qualification.Gravity,results,
            physicalWorldInstallations=0,canonicalMutations=0,fullTrajectoryQualified=false});
    }
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);
        Require(!Directory.Exists(Qualification.Output),"fresh output required");Directory.CreateDirectory(Qualification.Output);
        try
        {
            var old=Path.Combine(Qualification.Repo,"docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/tiny-active-friction");
            using var reference=JsonDocument.Parse(File.ReadAllText(Path.Combine(old,"coast-reference.json")));
            var b=BoundaryDriver.InputFrom("baseline",old,reference.RootElement);var p=BoundaryDriver.InputFrom("powered",old,reference.RootElement);
            var rb=Run(b,"baseline-12");var rp=Run(p,"powered-12");
            var frozen=Path.Combine(Path.GetDirectoryName(Qualification.Output)!,"reproduction");
            MatchFrozen(rb,"baseline",frozen);MatchFrozen(rp,"powered",frozen);
            Qualification.Save("stage-delta-inputs.json",new{status="D12 matches uninstrumented reproduction exactly",baseline=new{rb.Equations,rb.Stages,rb.Summary},powered=new{rp.Equations,rp.Stages,rp.Summary}});
            var bb=Run(b,"baseline-repeat");var pp=Run(p,"powered-repeat");
            Require(Same(rb.Proposal.Target,bb.Proposal.Target)&&Same(Inc(rb.Proposal),Inc(bb.Proposal))&&Same(rb.Stages,bb.Stages),"baseline identical input nondeterminism");
            Require(Same(rp.Proposal.Target,pp.Proposal.Target)&&Same(Inc(rp.Proposal),Inc(pp.Proposal))&&Same(rp.Stages,pp.Stages),"powered identical input nondeterminism");
            Qualification.Save("identical-control.json",new{status="PASS",independentOwners=true,baselineBitIdentical=true,poweredBitIdentical=true,compared="target, increments, every stage"});
            NullControl(b,old);
            var runs=new List<object>{rb.Summary,rp.Summary};
            foreach(int count in new[]{16,24,32})foreach(var input in new[]{b,p})
            {var r=Run(input,input.Branch+"-"+count,count);runs.Add(r.Summary);Require(r.Status==OperatorStatus.Ready,"higher-sweep admission "+r.Name);}
            foreach(var input in new[]{b,p}){var r=Run(input,input.Branch+"-noD",12,false);runs.Add(r.Summary);Require(r.Status==OperatorStatus.Ready,"noD diagnostic admission");}
            Qualification.Save("sweep-results.json",new{candidate="D12 unchanged",runs});
            Downstream(b,p,rb,rp);
            Qualification.Save("completion.json",new{status="BOUNDED DIAGNOSTICS COMPLETED",solverAdmissions=BoundaryPolicy.SolverAdmissions,
                buildsOrRuntimeTracesInsideMeasurement=false,canonicalMutations=0,nativeWorldInstallations=0});
        }
        catch(Exception e){Qualification.Save("stop.json",new{error=e.Message,type=e.GetType().Name});Environment.ExitCode=1;Console.WriteLine("STOP "+e.Message);}
    }
}
