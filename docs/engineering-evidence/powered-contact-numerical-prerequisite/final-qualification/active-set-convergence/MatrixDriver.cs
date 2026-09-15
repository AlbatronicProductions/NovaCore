using System.Text.Json;
using S=PieceKernel.State;
using V=PieceKernel.V;
internal static class MatrixDriver
{
    private const double H=1d/64,P=1d/64,Omega=128;
    private static readonly InverseBody Body=Qualification.Body(8);
    private static double[] End(S s)=>[s.Linear.X,s.Linear.Y,s.Linear.Z,s.Angular.X,s.Angular.Y,s.Angular.Z];
    private static S Sub(S a,S b)=>new(a.Linear-b.Linear,a.Angular-b.Angular);
    private static S Response(ContactGeometry g,double[] x)
    {D3 v=default,w=default;for(int i=0;i<7;i++){v+=x[i]*g.Linear(i);w+=x[i]*g.Angular(i);}return new(new(.125*v.X,.125*v.Y,.125*v.Z),new(.5*w.X,.5*w.Y,.5*w.Z));}
    private static Load ToLoad(S kick)=>new(new(kick.Linear.X/H,kick.Linear.Y/H,kick.Linear.Z/H),default,new(2*kick.Angular.X/H,2*kick.Angular.Y/H,2*kick.Angular.Z/H));
    private static ContactGeometry Geometry()
    {
        double depth=.75*P/(Omega*Omega*H),b=.5-depth;
        return new(new(1,2,3,4),new(0,1,0),new(0,0,-1),new(1,0,0),new(-1,-b,-.5),new(1,-b,.5),new(-1,-b,.5),new(1,-b,-.5),depth,depth,depth,depth,Omega,.5,2,2);
    }
    private sealed record Definition(double[] Cache,S Endpoint,S Free,Load Load,object ReferenceProof);
    private static Definition Define(ContactGeometry g,bool boundary,bool historical)
    {
        double cap=P/2,wcap=cap*g.Radius(0),fraction=boundary?1:historical?.5:0;
        double[] x=[P,P,P,P,cap*fraction,0,wcap*fraction];
        var patch=new CurrentPatch();patch.Set(g,Body);
        // Sliding saturated rows use a strictly out-of-cap unconstrained proposal. Interior
        // rows have zero slip. This is a manufactured solution, never the actual coast oracle.
        var target=boundary?new S(new(0,0,patch.K[4,4]*x[4]),new(0,-patch.K[6,6]*x[6],0)):default;
        var free=Sub(target,Response(g,x));double alpha=1/(Omega*H*(Omega*H+2));
        var errors=new double[7];
        for(int i=0;i<4;i++)errors[i]=patch.J(i,target)+alpha*patch.K[i,i]*x[i]-Math.Min(g.Depth(i)/H,Math.Min(g.Depth(i)/(H+2/Omega),2));
        double v0=patch.J(4,free),v1=patch.J(5,free);for(int j=0;j<7;j++)if(j is not 4 and not 5){v0+=patch.K[4,j]*x[j];v1+=patch.K[5,j]*x[j];}
        double det=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];
        double tx=(-patch.K[5,5]*v0+patch.K[4,5]*v1)/det,ty=(patch.K[5,4]*v0-patch.K[4,4]*v1)/det;
        double scale=Math.Min(1,cap/Math.Max((double)1e-16f,Math.Sqrt(tx*tx+ty*ty)));errors[4]=tx*scale-x[4];errors[5]=ty*scale-x[5];
        double spin=patch.J(6,free);for(int j=0;j<6;j++)spin+=patch.K[6,j]*x[j];double raw=-spin/patch.K[6,6];errors[6]=Math.Clamp(raw,-wcap,wcap)-x[6];
        StudyDriver.Require(errors.Max(Math.Abs)<=1e-12,"Manufactured reference equations failed");
        StudyDriver.Require(StudyControl.CurrentFeasible(g,x.Select(Scaled.From).ToArray()),"Manufactured reference outside exact represented caps");
        return new(x,target,free,ToLoad(free),new{errors,tangentProposal=new[]{tx,ty},twistProposal=raw,cap,wcap,active=boundary?"SATURATED":"INTERIOR",historical});
    }
    private static string[] Classify(ContactGeometry g,CacheVector cache)
    {
        Scaled cap=default,tw=default;for(int i=0;i<4;i++){cap=ScaleMath.Add(cap,cache.At(i).Times(.125));tw=ScaleMath.Add(tw,cache.At(i).Times(.125*g.Radius(i)));}
        string C(int n)=>n<0?"INTERIOR":n==0?"SATURATED":"INFEASIBLE";
        return [C(ScaleMath.Compare(ScaleMath.Length(cache.T0,cache.T1),cap)),C(ScaleMath.Compare(ScaleMath.Abs(cache.Twist),tw))];
    }
    private static double[] MapResidual(ContactGeometry g,StudyDriver.Measure m)
    {
        var patch=new CurrentPatch();patch.Set(g,Body);var x=m.Impulses;var e=new double[7];double alpha=1/(Omega*H*(Omega*H+2));
        for(int i=0;i<4;i++)e[i]=patch.J(i,m.Endpoint)+alpha*patch.K[i,i]*x[i]-Math.Min(g.Depth(i)/H,Math.Min(g.Depth(i)/(H+2/Omega),2));
        double v0=patch.J(4,m.Endpoint),v1=patch.J(5,m.Endpoint),det=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];
        double a=x[4]+(-patch.K[5,5]*v0+patch.K[4,5]*v1)/det,b=x[5]+(patch.K[5,4]*v0-patch.K[4,4]*v1)/det;
        double cap=.125*x.Take(4).Sum(),twcap=Enumerable.Range(0,4).Sum(i=>.125*x[i]*g.Radius(i));
        double scale=Math.Min(1,cap/Math.Max((double)1e-16f,Math.Sqrt(a*a+b*b)));
        e[4]=a*scale-x[4];e[5]=b*scale-x[5];e[6]=Math.Clamp(x[6]-patch.J(6,m.Endpoint)/patch.K[6,6],-twcap,twcap)-x[6];return e;
    }
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);StudyDriver.Require(!Directory.Exists(Qualification.Output),"fresh matrix output");Directory.CreateDirectory(Qualification.Output);
        try {
            var g=Geometry();var h=Qualification.Exact(H,1);StudyDriver.Require(g.Valid,"matrix geometry");
            var definitions=new[]{Define(g,false,true),Define(g,true,true)};var targets=new[]{Define(g,false,false),Define(g,true,false)};
            Qualification.Save("references.json",new{scope="Source-law manufactured box/planar patch; independent loads; not actual tiny episode or native-world qualification",g,h=H,p=P,omega=Omega,definitions,targets});
            var histories=new OwnedState[2];
            for(int j=0;j<2;j++){
                var def=definitions[j];var cache=StudyDriver.Cache(def.Cache);
                var seed=new OwnedState(new(101+j,0,1,0,1),1,0,PieceKind.Coast,g,h,cache,def.Load,default);
                var input=new StudyDriver.Input(seed,g,h,Body,def.Load,def.Cache.Select(v=>(double)(float)v).ToArray(),def.Cache,End(def.Endpoint));
                StudyControl.Sweeps=8;StudyControl.UseD=true;StudyControl.Trace=false;StudyControl.AllowCold=false;StudyControl.TangentOverride=null;
                var measure=StudyDriver.Run(input,"prior-manufactured-"+j,8);StudyDriver.Require(measure.Pass,"Prior manufactured solve accuracy");
                var owner=new RetainedOperator(seed);var proposal=StudyDriver.Prepare(owner,input);var e=proposal.Target.Endpoint;
                var native=new S(new((double)(float)e.Linear.X,(double)(float)e.Linear.Y,(double)(float)e.Linear.Z),new((double)(float)e.Angular.X,(double)(float)e.Angular.Y,(double)(float)e.Angular.Z));
                StudyDriver.Require(owner.BeginInstall(proposal,native)==OperatorStatus.Ready&&owner.CompleteInstall(proposal)==OperatorStatus.Ready,"Numerical owner installation");
                histories[j]=owner.Snapshot;var classification=Classify(g,histories[j].Cache);
                Qualification.Save("history-"+j+".json",new{measure,accepted=histories[j],classification,scope="Verified manufactured prior solve and numerical-owner installation; no native world"});
                StudyDriver.Require(classification.All(x=>x==(j==0?"INTERIOR":"SATURATED")),"Actual installed historical active set differs");
            }
            var matrix=new List<object>();
            for(int current=0;current<2;current++)for(int historical=0;historical<2;historical++){
                var target=targets[current];var seed=histories[historical];var load=ToLoad(Sub(target.Free,seed.Endpoint));
                var input=new StudyDriver.Input(seed,g,h,Body,load,Enumerable.Range(0,7).Select(i=>(double)(float)seed.Cache.At(i).Value).ToArray(),target.Cache,End(target.Endpoint));
                var appliedFree=new S(seed.Endpoint.Linear+H*new V(load.Gravity.X,load.Gravity.Y,load.Gravity.Z),seed.Endpoint.Angular+H*new V(.5*load.Torque.X,.5*load.Torque.Y,.5*load.Torque.Z));
                double mismatch=End(Sub(appliedFree,target.Free)).Max(Math.Abs);StudyDriver.Require(mismatch<=1e-15,"Current equation matching failed");
                List<StudyDriver.Measure> curve=new();foreach(int count in new[]{1,2,4,8,9,10,11,12,16,24,32}){
                    var m=StudyDriver.Run(input,$"{historical}-to-{current}",count);curve.Add(m);
                    if(count>=8&&m.Pass)break;
                }
                matrix.Add(new{historical=historical==0?"INTERIOR":"SATURATED",current=current==0?"INTERIOR":"SATURATED",source=seed.Endpoint,load,targetFree=target.Free,appliedFree,currentFreeMismatch=mismatch,curve,
                    sourceMapResiduals=curve.Select(m=>new{m.Sweeps,residual=MapResidual(g,m)}).ToArray(),
                    residualNote="Measure.Residual is raw friction slip/stationarity, not active-friction map residual; use sourceMapResiduals for saturated cases"});
            }
            Qualification.Save("active-set-matrix.json",matrix);Console.WriteLine("MATRIX_COMPLETE");
        }catch(Exception e){Qualification.Save("stop.json",new{status="STOP",cause=e.ToString()});Environment.ExitCode=1;Console.WriteLine(e);}
    }
}
