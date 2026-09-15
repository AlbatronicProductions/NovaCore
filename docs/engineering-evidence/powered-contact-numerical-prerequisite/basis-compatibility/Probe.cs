// Diagnostic only. Source-linked accepted equations; no world, canonical capability or installation.
using System.Numerics;
using System.Text.Json;
using BepuPhysics;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;
using V=PieceKernel.V;
using S=PieceKernel.State;
using P=PieceKernel.Patch;

internal static class BasisProbe
{
    const double Bar=1e-12,PhysicsBar=1e-4;
    readonly record struct Key(long World,int Body,long BodyGeneration,int Constraint,long ManifoldGeneration,int A,int B,int C,int D);
    static readonly Key FixtureKey=new(1,0,1,0,1,-4,-21,-5,-20);
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true,IncludeFields=true};
    static V Vec(JsonElement x)=>new(x.GetProperty("X").GetDouble(),x.GetProperty("Y").GetDouble(),x.GetProperty("Z").GetDouble());
    static V LowerVec(JsonElement x)=>new(x.GetProperty("x").GetDouble(),x.GetProperty("y").GetDouble(),x.GetProperty("z").GetDouble());
    static Vector3 F(V x)=>new((float)x.X,(float)x.Y,(float)x.Z);
    static D3 D(V x)=>new(x.X,x.Y,x.Z);
    static void Require(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    static double Component(V x,int i)=>i==0?x.X:i==1?x.Y:x.Z;
    static Capture ReadCapture(JsonElement x)
    {
        var c=new Capture{Count=4,Omega=x.GetProperty("currentOmega").GetSingle(),TwiceDamping=2,Friction=.5f,Recovery=2};
        var i=0;foreach(var item in x.GetProperty("rows").EnumerateArray())
        {var r=item.GetProperty("row");c.Features[i]=r.GetProperty("Identity").GetInt32();c.Contacts[i]=new(){FeatureId=c.Features[i],
            Normal=F(Vec(r.GetProperty("Normal"))),Offset=F(Vec(r.GetProperty("Lever"))),Depth=item.GetProperty("depth").GetSingle()};i++;}
        return c;
    }
    static V Center(P p){var c=default(V);for(int i=0;i<4;i++)c+=p.Offsets[i];return .25*c;}
    static S Wrench(P p,double[] x)
    {var a=default(V);var b=default(V);for(int i=0;i<7;i++){a+=x[i]*p.Linear[i];b+=x[i]*p.Angular[i];}return new(a,b);}
    static object Basis(P p)=>new{normal=p.Linear[0],tangent1=p.Linear[4],tangent2=p.Linear[5],center=Center(p),offsets=p.Offsets,
        handedness=V.Dot(p.Linear[0],V.Cross(p.Linear[4],p.Linear[5])),normalTangent1Dot=V.Dot(p.Linear[0],p.Linear[4])};
    static double Det(double[,] a)=>a[0,0]*(a[1,1]*a[2,2]-a[1,2]*a[2,1])-a[0,1]*(a[1,0]*a[2,2]-a[1,2]*a[2,0])+a[0,2]*(a[1,0]*a[2,1]-a[1,1]*a[2,0]);
    static double[] Null(P p,double length)
    {
        var b=new double[3,4];for(int i=0;i<4;i++){b[0,i]=1;b[1,i]=V.Dot(p.Angular[i],p.Linear[4])/length;b[2,i]=V.Dot(p.Angular[i],p.Linear[5])/length;}
        var z=new double[4];for(int skip=0;skip<4;skip++){var m=new double[3,3];for(int r=0;r<3;r++){int j=0;for(int c=0;c<4;c++)if(c!=skip)m[r,j++]=b[r,c];}z[skip]=(skip%2==0?1:-1)*Det(m);}
        var max=z.Max(Math.Abs);if(max==0||!double.IsFinite(max))return z;for(int i=0;i<4;i++)z[i]/=max;return z;
    }
    static (double[] X,double Condition) Dense(double[,] a,double[] b)
    {
        int n=b.Length;var m=new double[n,2*n+1];double norm=0;
        for(int i=0;i<n;i++){double sum=0;for(int j=0;j<n;j++){m[i,j]=a[i,j];sum+=Math.Abs(a[i,j]);}norm=Math.Max(norm,sum);m[i,n+i]=1;m[i,2*n]=b[i];}
        for(int col=0;col<n;col++)
        {
            int pivot=col;for(int r=col+1;r<n;r++)if(Math.Abs(m[r,col])>Math.Abs(m[pivot,col]))pivot=r;
            if(m[pivot,col]==0||!double.IsFinite(m[pivot,col]))return(new double[n],double.PositiveInfinity);
            if(pivot!=col)for(int j=0;j<=2*n;j++)(m[col,j],m[pivot,j])=(m[pivot,j],m[col,j]);
            double d=m[col,col];for(int j=0;j<=2*n;j++)m[col,j]/=d;
            for(int r=0;r<n;r++)if(r!=col){double f=m[r,col];for(int j=0;j<=2*n;j++)m[r,j]-=f*m[col,j];}
        }
        double inv=0;var x=new double[n];for(int i=0;i<n;i++){double sum=0;for(int j=0;j<n;j++)sum+=Math.Abs(m[i,n+j]);inv=Math.Max(inv,sum);x[i]=m[i,2*n];}
        return(x,norm*inv);
    }
    static bool Frame(P p)
    {
        var axes=new[]{p.Linear[0],p.Linear[4],p.Linear[5]};double bound=8*Math.ScaleB(1d,-23);
        for(int i=0;i<3;i++){if(!double.IsFinite(axes[i].Length)||Math.Abs(V.Dot(axes[i],axes[i])-1)>bound)return false;
            for(int j=0;j<i;j++)if(Math.Abs(V.Dot(axes[i],axes[j]))>bound)return false;}
        return V.Dot(axes[0],V.Cross(axes[1],axes[2]))<0;
    }
    static string Feasible(P p,double[] x)
    {
        if(x.Any(v=>!double.IsFinite(v)))return "Nonfinite";
        if(x.Take(4).Any(v=>v<=0))return "NonpositiveNormal";
        double cap=.125*x.Take(4).Sum(),twist=0;for(int i=0;i<4;i++)twist+=.125*x[i]*p.Radii[i];
        if(Math.Sqrt(x[4]*x[4]+x[5]*x[5])>=cap)return "TangentCap";
        if(Math.Abs(x[6])>=twist)return "TwistCap";return "Ready";
    }
    internal sealed record Transport(string Status,double[]? Cache,double? LinearError,double? AngularError,double? VelocityError,
        double? Condition,double? RoundoffScreen,object? Before,object? After);
    static Transport Move(P old,P next,double[] cache,Key? oldKey=null,Key? newKey=null)
    {
        Transport Refuse(string s)=>new(s,null,null,null,null,null,null,null,null);
        var sourceKey=oldKey??FixtureKey;var targetKey=newKey??FixtureKey;
        if(sourceKey!=targetKey||sourceKey.World<=0||sourceKey.Body<0||sourceKey.BodyGeneration<=0||sourceKey.Constraint<0||sourceKey.ManifoldGeneration<=0||
            new[]{sourceKey.A,sourceKey.B,sourceKey.C,sourceKey.D}.Distinct().Count()!=4)return Refuse("IdentityMismatch");
        if(!Frame(old)||!Frame(next))return Refuse("InvalidBasis");
        if(V.Dot(old.Linear[0],next.Linear[0])<=0)return Refuse("NormalHemisphere");
        if(old.Depth.Concat(next.Depth).Any(x=>!double.IsFinite(x)||x<=0||x>.020))return Refuse("SupportDepth");
        var initialFeasibility=Feasible(old,cache);if(initialFeasibility!="Ready")return Refuse("Old"+initialFeasibility);
        var wrench=Wrench(old,cache);double length=Math.Max(old.Radii.Max(),next.Radii.Max());
        if(!double.IsFinite(length)||length<=0)return Refuse("DegenerateGeometry");
        var z=Null(next,length);var a=new double[7,7];var b=new double[7];
        for(int r=0;r<3;r++){b[r]=Component(wrench.Linear,r);b[r+3]=Component(wrench.Angular,r)/length;
            for(int j=0;j<7;j++){double scale=j==6?length:1;a[r,j]=scale*Component(next.Linear[j],r);a[r+3,j]=scale*Component(next.Angular[j],r)/length;}}
        for(int i=0;i<4;i++){a[6,i]=z[i];b[6]+=z[i]*cache[i];}
        var solved=Dense(a,b);if(!double.IsFinite(solved.Condition))return Refuse("RankDeficient");
        double eps=Math.ScaleB(1d,-52),gamma=512*eps/(1-512*eps);
        double screen=gamma*solved.Condition*Math.Max(1,solved.X.Max(Math.Abs))*Math.Max(1,length);
        var x=solved.X;x[6]*=length;var after=Wrench(next,x);
        double pe=(after.Linear-wrench.Linear).Length,le=(after.Angular-wrench.Angular).Length;
        double ve=Math.Max(next.InverseMass*pe,.5*le);
        string status=screen>Bar?"ConditionErrorBudget":pe>Bar||le>Bar?"InvariantError":Feasible(next,x);
        return new(status,x,pe,le,ve,solved.Condition,screen,wrench,after);
    }
    static void Rebuild(P p)
    {for(int i=0;i<7;i++)for(int j=0;j<7;j++)p.K[i,j]=p.InverseMass*V.Dot(p.Linear[i],p.Linear[j])+.5*V.Dot(p.Angular[i],p.Angular[j]);}
    static P Synthetic(Vector3 n,double tangentAngle=0,V shift=default,bool reflect=false)
    {
        var c=new Capture{Count=4};float[] xs=[-1,1,-1,1],zs=[-.5f,.5f,.5f,-.5f];
        for(int i=0;i<4;i++)c.Contacts[i]=new(){Normal=n,Offset=new(xs[i],-.5f,zs[i]),Depth=.0004f};
        var p=new P(c,8);for(int i=0;i<4;i++){p.Offsets[i]+=shift;p.Angular[i]=V.Cross(p.Offsets[i],p.Linear[i]);}
        var t0=p.Linear[4];var t1=p.Linear[5];p.Linear[4]=Math.Cos(tangentAngle)*t0+Math.Sin(tangentAngle)*t1;
        p.Linear[5]=(-Math.Sin(tangentAngle))*t0+Math.Cos(tangentAngle)*t1;if(reflect)p.Linear[5]=-1*p.Linear[5];
        var center=Center(p);p.Angular[4]=V.Cross(center,p.Linear[4]);p.Angular[5]=V.Cross(center,p.Linear[5]);Rebuild(p);return p;
    }
    static double Residual(P p,S free,double[] x,double omega,double h)
    {
        double s=1/(omega*omega*h*h+2*omega*h),max=0;
        for(int i=0;i<7;i++){double v=p.J(i,free);for(int j=0;j<7;j++)v+=p.K[i,j]*x[j];
            if(i<4)v+=s*p.K[i,i]*x[i]-Math.Min(p.Depth[i]/h,Math.Min(p.Depth[i]/(h+2/omega),2));max=Math.Max(max,Math.Abs(v));}
        return max;
    }
    static object Comparison(P p,S free,double[] guess,double omega,double h,PieceKernel.OracleResult reference)
    {
        var x=PieceKernel.Solve(p,free,guess,omega,8,h);
        double normal=Math.Abs(x.Impulses.Take(4).Sum()-reference.Impulses.Take(4).Sum()),linear=(x.Velocity.Linear-reference.Velocity.Linear).Length,
            angular=(x.Velocity.Angular-reference.Velocity.Angular).Length;
        return new{initial=guess,solved=x,normalImpulseError=normal,linearVelocityError=linear,angularVelocityError=angular,
            componentErrors=x.Impulses.Zip(reference.Impulses,(a,b)=>a-b).ToArray(),residual=Residual(p,free,x.Impulses,omega,h),
            physicalBar=PhysicsBar,pass=normal<=PhysicsBar&&linear<=PhysicsBar&&angular<=PhysicsBar};
    }
    static CacheDuration Duration(JsonElement x)
    {
        var bytes=Convert.FromHexString(x.GetProperty("exactValueBytesHex").GetString()!);
        Require(bytes.Length==System.Runtime.CompilerServices.Unsafe.SizeOf<PropellantDuration>(),"Exact captured duration size");
        var exact=System.Runtime.InteropServices.MemoryMarshal.Read<PropellantDuration>(bytes);
        Require(CacheDuration.FromExact(exact,out var h),"Exact captured duration valid");return h;
    }
    static double[] Initialize(P p,double[] cache,CacheDuration oldH,CacheDuration h,double omega,Load previous,Load current)
    {
        Require(DurationInitialization.TryScale(CacheVector.From(cache),oldH,h,out var result),"duration transform");
        var x=new double[7];result.Project(x);var rows=Enumerable.Range(0,4).Select(i=>new NormalRow(i,D(p.Linear[i]),D(p.Offsets[i]))).ToArray();
        Require(CommonNormal.Prepare(rows,x.AsSpan(0,4),new(.125,new(.5,.5,.5),default),previous,current,h.Numerical,omega,out var common)==PredictionStatus.Ready,"current-piece common correction");
        for(int i=0;i<4;i++)x[i]=common.Materialize(x[i]);return x;
    }
    static Load ReadLoad(JsonElement x)=>new(D(Vec(x.GetProperty("Gravity"))),D(Vec(x.GetProperty("Force"))),D(Vec(x.GetProperty("Torque"))));
    public static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));var records=doc.RootElement.GetProperty("records").EnumerateArray().ToArray();
        var oldInput=records.Single(x=>x.GetProperty("label").GetString()=="original_owned_powered_piece").GetProperty("inputs");
        var coast=records.Single(x=>x.GetProperty("label").GetString()=="owned_coast_continuation").GetProperty("inputs");
        var oldCapture=ReadCapture(oldInput);var newCapture=ReadCapture(coast);var old=new P(oldCapture,8);var next=new P(newCapture,8);
        var cache=coast.GetProperty("cacheValues").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        var ci=coast.GetProperty("identity");
        var key=new Key(ci.GetProperty("World").GetInt64(),ci.GetProperty("Body").GetInt32(),ci.GetProperty("BodyGeneration").GetInt64(),
            ci.GetProperty("Constraint").GetInt32(),ci.GetProperty("ManifoldGeneration").GetInt64(),newCapture.Features[0],newCapture.Features[1],newCapture.Features[2],newCapture.Features[3]);
        Require(key==FixtureKey&&coast.GetProperty("previousPiece").GetInt64()==1&&coast.GetProperty("nextPiece").GetInt64()==2&&
            !coast.GetProperty("invalidated").GetBoolean()&&coast.GetProperty("previousKind").GetString()=="Powered"&&
            coast.GetProperty("nextKind").GetString()=="Coast"&&Vec(coast.GetProperty("expectedNormal"))==old.Linear[0],"captured ownership/basis lineage");
        Require(oldInput.GetProperty("identity").GetRawText()==ci.GetRawText()&&oldCapture.Features.SequenceEqual(newCapture.Features)&&
            records.Single(x=>x.GetProperty("label").GetString()=="original_owned_powered_piece").GetProperty("status").GetString()=="Ready",
            "captured producer identity and feature lineage");
        var installed=records.Single(x=>x.GetProperty("label").GetString()=="installed-powered-endpoint-before-coast-refresh");
        Require(LowerVec(installed.GetProperty("linearVelocity"))==Vec(coast.GetProperty("source").GetProperty("Linear"))&&
            LowerVec(installed.GetProperty("angularVelocity"))==Vec(coast.GetProperty("source").GetProperty("Angular"))&&
            installed.GetProperty("piece").GetInt64()==1&&!installed.GetProperty("invalidated").GetBoolean(),"accepted installed endpoint lineage");
        var native=coast.GetProperty("nativeCache").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        Require(CacheVector.From(cache).MatchesTransport(native),"accepted private/native cache projection");
        var move=Move(old,next,cache);Require(move.Status=="Ready","original compatibility: "+move.Status);
        var previous=ReadLoad(coast.GetProperty("previousLoad"));var current=ReadLoad(coast.GetProperty("currentLoad"));
        var h0=Duration(coast.GetProperty("previousDuration"));var h=Duration(coast.GetProperty("currentDuration"));
        Require(h0.Numerical.Value==1d/128&&h.Numerical.Value==17707d/2000000,"exact producing/current duration lineage");
        var source=new S(Vec(coast.GetProperty("source").GetProperty("Linear")),Vec(coast.GetProperty("source").GetProperty("Angular")));
        var sourceA=current.Gravity+.125*current.Force;var free=new S(source.Linear+h.Numerical.Value*new V(sourceA.X,sourceA.Y,sourceA.Z),source.Angular);
        var reference=PieceKernel.Reference(next,free,newCapture.Omega,h.Numerical.Value);
        Require(reference.Admissible&&reference.Residual<=Bar,"original independent reference");
        var transported=Initialize(next,move.Cache!,h0,h,newCapture.Omega,previous,current);
        var unchanged=Initialize(next,cache,h0,h,newCapture.Omega,previous,current);
        var scaledOld=new double[7];DurationInitialization.TryScale(CacheVector.From(cache),h0,h,out var so);so.Project(scaledOld);
        var moveScaled=Move(old,next,scaledOld);Require(moveScaled.Status=="Ready","scaled compatibility");
        var scaledMoved=new double[7];DurationInitialization.TryScale(CacheVector.From(move.Cache!),h0,h,out var sm);sm.Project(scaledMoved);
        var remaining=new List<object>();
        for(int i=0;i<4;i++)
        {
            var n=D(next.Linear[i]);var r=D(next.Offsets[i]);var moment=D3.Cross(r,n);var a0=previous.Gravity+.125*previous.Force;var a1=current.Gravity+.125*current.Force;
            bool pass=newCapture.Features[i]==oldCapture.Features[i]&&next.Depth[i]>0&&next.Depth[i]<=.020&&
                D3.Dot(n,a0)<0&&D3.Dot(n,a1)<0&&Math.Abs(V.Dot(next.Linear[i],source.Linear)+V.Dot(next.Angular[i],source.Angular))<=.0005/(16667d/1e6);
            remaining.Add(new{row=i,pass});Require(pass,"remaining current support predicate");
        }
        var original=new{oldBasis=Basis(old),newBasis=Basis(next),oldCache=cache,transport=move,
            actualPointChange=next.Offsets.Zip(old.Offsets,(a,b)=>a-b).ToArray(),oldDuration=h0.Numerical,currentDuration=h.Numerical,
            durationCommutationMaxError=scaledMoved.Zip(moveScaled.Cache!,(a,b)=>Math.Abs(a-b)).Max(),
            immutableInputs=new{mass=8,inverseInertia=.5,omega=newCapture.Omega,h=h.Numerical.Value,source,free,previous,current},remainingSupportPredicates=remaining,
            diagnosticAdmission="Ready: evidence-only compatibility + existing current support/duration/load predicates; no prepared owner capability",
            reference,A=new{status="UnsupportedSupport",predicate="row.Normal != normal",solve=false},
            B=Comparison(next,free,unchanged,newCapture.Omega,h.Numerical.Value,reference),
            C=Comparison(next,free,transported,newCapture.Omega,h.Numerical.Value,reference),
            D=Comparison(next,free,new double[7],newCapture.Omega,h.Numerical.Value,reference),
            E=Comparison(next,free,reference.Impulses,newCapture.Omega,h.Numerical.Value,reference),
            unchangedComponentWrenchError=new{linear=(Wrench(next,cache).Linear-Wrench(old,cache).Linear).Length,angular=(Wrench(next,cache).Angular-Wrench(old,cache).Angular).Length}};
        File.WriteAllText(Path.Combine(args[1],"original-coast-witness.json"),JsonSerializer.Serialize(original,Json)+Environment.NewLine);
        var synthetic=new List<object>();double[] seed=[.25,.25,.25,.25,.01,-.005,.02];var basePatch=Synthetic(Vector3.UnitY);
        Vector3 Normal(double angle)=>new(0,(float)Math.Cos(angle),(float)Math.Sin(angle));
        var cases=new (string Name,P Old,P New,bool Identity,string Expected)[]{
            ("identical",basePatch,Synthetic(Vector3.UnitY),true,"Ready"),
            ("tiny-normal",basePatch,Synthetic(Normal(1e-8)),true,"Ready"),
            ("moderate-normal",basePatch,Synthetic(Normal(.08)),true,"Ready"),
            ("tangent-only",basePatch,Synthetic(Vector3.UnitY,.7),true,"Ready"),
            ("normal-and-tangent",basePatch,Synthetic(Normal(.08),.7),true,"Ready"),
            ("reflection",basePatch,Synthetic(Vector3.UnitY,reflect:true),true,"InvalidBasis"),
            ("normal-flip",basePatch,Synthetic(-Vector3.UnitY),true,"NormalHemisphere"),
            ("identity-change",basePatch,Synthetic(Vector3.UnitY),false,"IdentityMismatch"),
            ("actual-helper-seam",Synthetic(new(.6f,.8f,1e-8f)),Synthetic(new(.6f,.8f,-1e-8f)),true,"Ready"),
            ("friction-incompatible",basePatch,Synthetic(Normal(.3)),true,"TangentCap"),
            ("lever-only",basePatch,Synthetic(Vector3.UnitY,shift:new(0,-.0001,0)),true,"Ready"),
            ("lever-incompatible",basePatch,Synthetic(Vector3.UnitY,shift:new(3,0,0)),true,"NonpositiveNormal")};
        foreach(var item in cases)
        {
            var result=Move(item.Old,item.New,seed,FixtureKey,item.Identity?FixtureKey:FixtureKey with{A=99});object? solve=null;object? oracle=null;
            if(result.Status=="Ready")
            {
                double sh=17707d/2000000,omega=newCapture.Omega;var sf=new S((-9.81*sh)*item.New.Linear[0],default);
                var sr=PieceKernel.Reference(item.New,sf,omega,sh);oracle=sr;
                if(sr.Admissible&&sr.Residual<=Bar)solve=Comparison(item.New,sf,result.Cache!,omega,sh,sr);
            }
            synthetic.Add(new{item.Name,item.Expected,decisionMatches=result.Status==item.Expected,oldBasis=Basis(item.Old),newBasis=Basis(item.New),transport=result,
                componentChange=result.Cache?.Zip(seed,(a,b)=>a-b).ToArray(),reference=oracle,eightSweep=solve});
        }
        // Nonzero twist: isolate its linear map from the difference of two strictly feasible caches.
        var noTwist=(double[])seed.Clone();noTwist[6]=0;var rotated=Synthetic(Normal(.08));
        var full=Move(basePatch,rotated,seed);var zero=Move(basePatch,rotated,noTwist);Require(full.Status=="Ready"&&zero.Status=="Ready","twist pair");
        var difference=full.Cache!.Zip(zero.Cache!,(a,b)=>a-b).ToArray();var twistWrench=Wrench(rotated,difference);
        var expectedTwist=.02*basePatch.Linear[0];var projected=(V.Dot(expectedTwist,rotated.Linear[0])/V.Dot(rotated.Linear[0],rotated.Linear[0]))*rotated.Linear[0];
        var twist=new{oldTwist=.02,transportedDifference=difference,reconstructed=twistWrench,expected=expectedTwist,
            angularError=(twistWrench.Angular-expectedTwist).Length,linearError=twistWrench.Linear.Length,projectionOnlyAngularLoss=(projected-expectedTwist).Length};
        File.WriteAllText(Path.Combine(args[1],"synthetic-results.json"),JsonSerializer.Serialize(new{seed,cases=synthetic,nonzeroTwist=twist,sweeps=8,transportBar=Bar,physicalBar=PhysicsBar},Json)+Environment.NewLine);
        Console.WriteLine("Bounded captured-state probe complete: original A/B/C/D/E, 12 synthetic cases, nonzero twist pair. No world step/install.");
    }
}
