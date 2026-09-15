// Captured-system causal diagnostic. No world, canonical owner, cache installation or production correction.
using System.Reflection;
using System.Text.Json;
using System.Numerics;
using V=PieceKernel.V;
using S=PieceKernel.State;
using P=PieceKernel.Patch;

internal static class CoastAccuracy
{
    const double ErrorBar=1e-4,ProofBar=1e-12;
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true,IncludeFields=true};
    static Capture Old=null!,New=null!;
    static S OldState,NewState;
    static CacheDuration OldH,NewH;
    static double[] Cache=null!;
    static readonly Load Previous=new(new(0,-9.81,0),new(0,32,0),default);
    static T Call<T>(string name,params object?[] values)=>(T)typeof(BasisProbe).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,values)!;
    static void Check(bool v,string message){if(!v)throw new InvalidOperationException(message);}
    static double[] Array(JsonElement v)=>v.EnumerateArray().Select(x=>x.GetDouble()).ToArray();
    static V Vec(JsonElement x)=>new(x.GetProperty("X").GetDouble(),x.GetProperty("Y").GetDouble(),x.GetProperty("Z").GetDouble());
    static S State(JsonElement x)=>new(Vec(x.GetProperty("Linear")),Vec(x.GetProperty("Angular")));
    static D3 D(V v)=>new(v.X,v.Y,v.Z);
    static double Max(double[] x)=>x.Max(Math.Abs);
    static double[] Sub(double[] a,double[] b)=>a.Zip(b,(x,y)=>x-y).ToArray();
    static bool Bits(double[] a,double[] b)=>a.Length==b.Length&&a.Zip(b,(x,y)=>BitConverter.DoubleToUInt64Bits(x)==BitConverter.DoubleToUInt64Bits(y)).All(x=>x);
    static Capture Geometry(bool geometry,bool depth)
    {
        var source=geometry?New:Old;var c=new Capture{Count=4,Omega=Old.Omega,TwiceDamping=2,Friction=.5f,Recovery=2};
        for(int i=0;i<4;i++){c.Features[i]=source.Features[i];c.Contacts[i]=source.Contacts[i];c.Contacts[i].Depth=(depth?New:Old).Contacts[i].Depth;}return c;
    }
    internal sealed record Config(Capture Capture,S Source,double Mass,CacheDuration Duration,Load Load);
    static Config Case(bool geometry=false,bool depth=false,bool velocity=false,bool mass=false,bool duration=false,double force=32)
        =>new(Geometry(geometry,depth),velocity?NewState:OldState,mass?8:8+1d/256,duration?NewH:OldH,new(Previous.Gravity,new(0,force,0),default));
    internal sealed record System(P Patch,S Free,double Omega,double H,double[] Guess,double[,] A,double[] Rhs,
        PieceKernel.OracleResult Reference,object Inputs);
    static (double[,] A,double[] Rhs) Equation(P p,S free,double omega,double h)
    {
        var a=(double[,])p.K.Clone();var b=new double[7];double alpha=1/(omega*h*(omega*h+2));
        for(int i=0;i<7;i++){b[i]=-p.J(i,free);if(i<4){a[i,i]+=alpha*p.K[i,i];b[i]+=Math.Min(p.Depth[i]/h,Math.Min(p.Depth[i]/(h+2/omega),2));}}
        return(a,b);
    }
    static System Prepare(Config c)
    {
        var p=new P(c.Capture,c.Mass);var old=new P(Old,c.Mass);
        var move=Call<BasisProbe.Transport>("Move",old,p,Cache,null,null);Check(move.Status=="Ready","unchanged basis domain: "+move.Status);
        Check(DurationInitialization.TryScale(CacheVector.From(move.Cache!),OldH,c.Duration,out var scaled),"unchanged duration preparation");
        var guess=new double[7];scaled.Project(guess);
        var rows=Enumerable.Range(0,4).Select(i=>new NormalRow(c.Capture.Features[i],D(p.Linear[i]),D(p.Offsets[i]))).ToArray();
        var body=new InverseBody(1/c.Mass,new(.5,.5,.5),default);
        Check(CommonNormal.Prepare(rows,guess.AsSpan(0,4),body,Previous,c.Load,c.Duration.Numerical,c.Capture.Omega,out var common)==PredictionStatus.Ready,"unchanged load preparation");
        for(int i=0;i<4;i++)guess[i]=common.Materialize(guess[i]);
        double h=c.Duration.Numerical.Value;var acceleration=c.Load.Gravity+body.Mass*c.Load.Force;var angular=body.Apply(c.Load.Torque);
        var free=new S(c.Source.Linear+h*new V(acceleration.X,acceleration.Y,acceleration.Z),c.Source.Angular+h*new V(angular.X,angular.Y,angular.Z));
        var e=Equation(p,free,c.Capture.Omega,h);var reference=PieceKernel.Reference(p,free,c.Capture.Omega,h);
        return new(p,free,c.Capture.Omega,h,guess,e.A,e.Rhs,reference,
            new{c.Source,c.Mass,inverseMass=body.Mass,inverseInertia=body.Diagonal,c.Load,h,omega=(double)c.Capture.Omega,
                alpha=1/(c.Capture.Omega*h*(c.Capture.Omega*h+2)),normal=p.Linear[0],tangent1=p.Linear[4],tangent2=p.Linear[5],
                offsets=p.Offsets,depths=p.Depth,features=c.Capture.Features,transported=move.Cache,scaled=Enumerable.Range(0,7).Select(i=>scaled.At(i).Value).ToArray(),
                loadShift=common.Shift.Value,guess,kinematics=Enumerable.Range(0,4).Select(i=>{
                    var point=c.Source.Linear+V.Cross(c.Source.Angular,p.Offsets[i]);
                    return new{normal=V.Dot(point,p.Linear[i]),tangent1=V.Dot(point,p.Linear[4]),tangent2=V.Dot(point,p.Linear[5]),point};}).ToArray()});
    }
    static double[] Residual(System s,double[] x)
    {var r=new double[7];for(int i=0;i<7;i++){r[i]=-s.Rhs[i];for(int j=0;j<7;j++)r[i]+=s.A[i,j]*x[j];}return r;}
    static double[][] Rows(double[,] m)=>Enumerable.Range(0,7).Select(i=>Enumerable.Range(0,7).Select(j=>m[i,j]).ToArray()).ToArray();
    static double[] Modes(double[] x)=>new[]{(x[0]+x[1]+x[2]+x[3])/4,(-x[0]+x[1]-x[2]+x[3])/4,
        (-x[0]+x[1]+x[2]-x[3])/4,(x[0]+x[1]-x[2]-x[3])/4,x[4],x[5],x[6]};
    static S Kick(P p,double[] x)=>p.Reconstruct(default,x);
    static S Wrench(P p,double[] x)
    {var l=default(V);var a=default(V);for(int i=0;i<7;i++){l+=x[i]*p.Linear[i];a+=x[i]*p.Angular[i];}return new(l,a);}
    internal sealed record Observation(int Sweeps,double[] Impulses,S Velocity,double[] SignedError,double NormalError,double LinearError,
        double AngularError,double Residual,double[] ResidualComponents,int NormalClamps,int TangentClamps,int TwistClamps,bool Pass);
    static Observation Observe(System s,double[] initial,int count)
    {
        var x=PieceKernel.Solve(s.Patch,s.Free,initial,s.Omega,count,s.H);var error=Sub(x.Impulses,s.Reference.Impulses);var r=Residual(s,x.Impulses);
        double n=Math.Abs(x.Impulses.Take(4).Sum()-s.Reference.Impulses.Take(4).Sum()),v=(x.Velocity.Linear-s.Reference.Velocity.Linear).Length,w=(x.Velocity.Angular-s.Reference.Velocity.Angular).Length;
        return new(count,x.Impulses,x.Velocity,error,n,v,w,Max(r),r,x.NormalClamps,x.TangentClamps,x.TwistClamps,n<=ErrorBar&&v<=ErrorBar&&w<=ErrorBar);
    }
    static double[] ErrorStep(System s,double[] input)
    {
        var x=(double[])input.Clone();for(int i=0;i<4;i++){double rhs=0;for(int j=0;j<7;j++)if(j!=i)rhs-=s.A[i,j]*x[j];x[i]=rhs/s.A[i,i];}
        double b4=0,b5=0;for(int j=0;j<7;j++)if(j is not 4 and not 5){b4-=s.A[4,j]*x[j];b5-=s.A[5,j]*x[j];}
        double det=s.A[4,4]*s.A[5,5]-s.A[4,5]*s.A[5,4];x[4]=(s.A[5,5]*b4-s.A[4,5]*b5)/det;x[5]=(s.A[4,4]*b5-s.A[5,4]*b4)/det;
        double b6=0;for(int j=0;j<6;j++)b6-=s.A[6,j]*x[j];x[6]=b6/s.A[6,6];return x;
    }
    static double[] Propagate(System s,double[] input,int count){var x=input;for(int i=0;i<count;i++)x=ErrorStep(s,x);return x;}
    static object Modal(System s,Observation measured)
    {
        var initial=Sub(s.Guess,s.Reference.Impulses);var coeff=Modes(initial);double[][] patterns=[ [1,1,1,1],[-1,1,-1,1],[-1,1,1,-1],[1,1,-1,-1] ];
        var components=new List<object>();var sum=new double[7];string[] names=["common","x-pressure","z-pressure","checker","tangent-block","twist"];
        for(int k=0;k<6;k++)
        {
            var e=new double[7];if(k<4){for(int j=0;j<4;j++)e[j]=coeff[k]*patterns[k][j];}else if(k==4){e[4]=initial[4];e[5]=initial[5];}else e[6]=initial[6];
            var end=Propagate(s,e,8);for(int j=0;j<7;j++)sum[j]+=end[j];components.Add(new{name=names[k],initial=e,afterEight=end,initialWrench=Wrench(s.Patch,e),
                finalWrench=Wrench(s.Patch,end),finalKick=Kick(s.Patch,end),signedNormalSum=end.Take(4).Sum(),finalModes=Modes(end)});
        }
        double difference=Max(Sub(sum,measured.SignedError));Check(difference<=ProofBar,"signed modal reconstruction");
        return new{initialError=initial,initialModes=coeff,finalModes=Modes(measured.SignedError),components,reconstructed=sum,
            maxSignedDifference=difference,reconstructedKick=Kick(s.Patch,sum),measuredKick=new S(measured.Velocity.Linear-s.Reference.Velocity.Linear,measured.Velocity.Angular-s.Reference.Velocity.Angular)};
    }
    static object Trace(System s,Observation[] curve)
    {
        var x=(double[])s.Guess.Clone();var trace=new List<object>();double maxDifference=0;bool unclamped=true;
        object Snap(int n,string stage){var r=Residual(s,x);return new{sweep=n,stage,impulses=(double[])x.Clone(),residual=r,
            normalMax=r.Take(4).Max(Math.Abs),tangentNorm=Math.Sqrt(r[4]*r[4]+r[5]*r[5]),twist=Math.Abs(r[6]),modes=Modes(Sub(x,s.Reference.Impulses))};}
        for(int sweep=1;sweep<=32;sweep++)
        {
            for(int i=0;i<4;i++){double b=s.Rhs[i];for(int j=0;j<7;j++)if(j!=i)b-=s.A[i,j]*x[j];x[i]=b/s.A[i,i];unclamped&=x[i]>0;}
            var before=Residual(s,x);var oldT=new[]{x[4],x[5]};if(sweep<=8)trace.Add(Snap(sweep,"after-normals-before-tangent"));
            double b4=s.Rhs[4],b5=s.Rhs[5];for(int j=0;j<7;j++)if(j is not 4 and not 5){b4-=s.A[4,j]*x[j];b5-=s.A[5,j]*x[j];}
            double det=s.A[4,4]*s.A[5,5]-s.A[4,5]*s.A[5,4];x[4]=(s.A[5,5]*b4-s.A[4,5]*b5)/det;x[5]=(s.A[4,4]*b5-s.A[5,4]*b4)/det;
            unclamped&=Math.Sqrt(x[4]*x[4]+x[5]*x[5])<.125*x.Take(4).Sum();var after=Residual(s,x);
            if(sweep<=8){trace.Add(Snap(sweep,"after-tangent"));trace.Add(new{sweep,stage="tangent-feedback-proof",observed=Sub(after,before).Take(4).ToArray(),
                predicted=Enumerable.Range(0,4).Select(i=>s.A[i,4]*(x[4]-oldT[0])+s.A[i,5]*(x[5]-oldT[1])).ToArray()});}
            double b6=s.Rhs[6];for(int j=0;j<6;j++)b6-=s.A[6,j]*x[j];x[6]=b6/s.A[6,6];double cap=0;for(int i=0;i<4;i++)cap+=.125*x[i]*s.Patch.Radii[i];unclamped&=Math.Abs(x[6])<cap;
            if(sweep<=8)trace.Add(Snap(sweep,"after-twist"));var captured=curve.SingleOrDefault(c=>c.Sweeps==sweep);if(captured is not null)maxDifference=Math.Max(maxDifference,Max(Sub(x,captured.Impulses)));
        }
        Check(unclamped&&maxDifference<=ProofBar,"unclamped trace matches actual candidate curve");
        var columns=Enumerable.Range(0,7).Select(j=>{var u=new double[7];u[j]=1;return ErrorStep(s,u);}).ToArray();
        return new{unclamped,maxDifference,trace,errorMapColumns=columns};
    }
    static object Describe(string name,System s,Observation? o)=>new{name,inputs=s.Inputs,a=Rows(s.A),rhs=s.Rhs,initialResidual=Residual(s,s.Guess),
        reference=s.Reference,result=o,referenceApplicable=s.Reference.Admissible&&s.Reference.Residual<=ProofBar};
    public static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));using var prior=JsonDocument.Parse(File.ReadAllText(args[1]));
        var records=doc.RootElement.GetProperty("records").EnumerateArray().ToArray();
        var oldInput=records.Single(x=>x.GetProperty("label").GetString()=="original_owned_powered_piece").GetProperty("inputs");
        var newInput=records.Single(x=>x.GetProperty("label").GetString()=="owned_coast_continuation").GetProperty("inputs");
        Old=Call<Capture>("ReadCapture",oldInput);New=Call<Capture>("ReadCapture",newInput);OldState=State(oldInput.GetProperty("source"));NewState=State(newInput.GetProperty("source"));
        OldH=Call<CacheDuration>("Duration",newInput.GetProperty("previousDuration"));NewH=Call<CacheDuration>("Duration",newInput.GetProperty("currentDuration"));Cache=Array(newInput.GetProperty("cacheValues"));
        var coast=Prepare(Case(true,true,true,true,true,0));var baseline=Observe(coast,coast.Guess,8);var recorded=prior.RootElement.GetProperty("C");
        Check(Bits(coast.Guess,Array(recorded.GetProperty("initial")))&&Bits(baseline.Impulses,Array(recorded.GetProperty("solved").GetProperty("Impulses")))&&
            baseline.Velocity==State(recorded.GetProperty("solved").GetProperty("Velocity")),"STOP: coast exact replay mismatch");
        Check(coast.Reference.Admissible&&coast.Reference.Residual<=ProofBar,"coast reference");
        // Original error uses abs(sum(solved normals)-sum(reference normals)), retain those exact operands too.
        double exactNormal=Math.Abs(baseline.Impulses.Take(4).Sum()-coast.Reference.Impulses.Take(4).Sum());
        Check(exactNormal==recorded.GetProperty("normalImpulseError").GetDouble()&&baseline.LinearError==recorded.GetProperty("linearVelocityError").GetDouble()&&
            baseline.AngularError==recorded.GetProperty("angularVelocityError").GetDouble(),"STOP: exact physical error replay mismatch");
        var curve=new[]{1,2,4,8,12,16,24,32}.Select(n=>n==8?baseline:Observe(coast,coast.Guess,n)).ToArray();
        var modal=Modal(coast,baseline);var trace=Trace(coast,curve);
        File.WriteAllText(Path.Combine(args[2],"convergence.json"),JsonSerializer.Serialize(new{exactReplay=true,exactNormal,coast=Describe("coast",coast,baseline),curve,modal,trace,
            minimumObservedPassing=curve.FirstOrDefault(x=>x.Pass)?.Sweeps,referenceInitialized=Observe(coast,coast.Reference.Impulses,8),cold=Observe(coast,new double[7],8)},Json)+Environment.NewLine);
        var cases=new (string Name,Config Config)[]{
            ("P-producing-equations",Case()),("P-load-only",Case(force:0)),("P-duration-only",Case(duration:true)),("P-mass-only",Case(mass:true)),
            ("P-geometry-only",Case(geometry:true)),("P-depth-only",Case(depth:true)),("P-linear-velocity-only",Case() with{Source=new(NewState.Linear,OldState.Angular)}),
            ("P-angular-velocity-only",Case() with{Source=new(OldState.Linear,NewState.Angular)}),("P-full-velocity-only",Case(velocity:true)),
            ("successor-held-producing-h-mass-load32",Case(true,true,true,false,false,32)),("successor-held-producing-h-mass-load0",Case(true,true,true,false,false,0)),
            ("C-old-velocity-old-depth",Case(true,false,false,true,true,0)),("C-new-velocity-old-depth",Case(true,false,true,true,true,0)),
            ("C-old-velocity-new-depth",Case(true,true,false,true,true,0)),("C-full",Case(true,true,true,true,true,0)),
            ("C-separating-body-Y-zero",Case(true,true,true,true,true,0) with{Source=new(new(NewState.Linear.X,0,NewState.Linear.Z),NewState.Angular)}),
            ("C-separating-body-Y-bound",Case(true,true,true,true,true,0) with{Source=new(new(NewState.Linear.X,.0005/(16667d/1e6),NewState.Linear.Z),NewState.Angular)})};
        var reports=new List<object>();var errors=new Dictionary<string,double[]>();
        foreach(var item in cases)
        {
            var s=Prepare(item.Config);Observation? o=null;if(s.Reference.Admissible&&s.Reference.Residual<=ProofBar){o=Observe(s,s.Guess,8);errors[item.Name]=o.SignedError;}
            if(item.Name=="P-producing-equations")Check(o?.Pass==true,"controlled passing anchor required");reports.Add(Describe(item.Name,s,o));
        }
        var b=errors["C-old-velocity-old-depth"];var v=Sub(errors["C-new-velocity-old-depth"],b);var d=Sub(errors["C-old-velocity-new-depth"],b);
        var interaction=Sub(Sub(Sub(errors["C-full"],b),v),d);
        var factorial=new{baseline=b,velocityEffect=v,depthEffect=d,interaction,
            baselineKick=Kick(coast.Patch,b),velocityKick=Kick(coast.Patch,v),depthKick=Kick(coast.Patch,d),interactionKick=Kick(coast.Patch,interaction),
            signedNormalSums=new[]{b.Take(4).Sum(),v.Take(4).Sum(),d.Take(4).Sum(),interaction.Take(4).Sum()}};
        File.WriteAllText(Path.Combine(args[2],"controlled-witnesses.json"),JsonSerializer.Serialize(new{cases=reports,factorial,physicsBar=ErrorBar,
            unchangedSeedMeaning="Accepted powered output cache, original producing geometry/duration/load; not oracle",canonicalMutations=0},Json)+Environment.NewLine);
        Console.WriteLine("Exact coast replay PASS; bounded curve, signed modes and17 controlled witnesses recorded. No installation or correction.");
    }
}
