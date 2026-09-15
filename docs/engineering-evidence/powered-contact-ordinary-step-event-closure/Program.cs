using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using BepuUtilities.Memory;

// Evidence-only input preparation. No production resource/actuator/canonical state.
internal readonly struct Rational
{
    internal readonly BigInteger N, D;
    internal Rational(BigInteger n, BigInteger d)
    {
        if(d<=0)throw new ArgumentOutOfRangeException(nameof(d));
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);N=n/gcd;D=d/gcd;
    }
    internal static Rational Parse(string text)
    {var p=text.Split('/');return new(BigInteger.Parse(p[0],CultureInfo.InvariantCulture),p.Length==1?BigInteger.One:BigInteger.Parse(p[1],CultureInfo.InvariantCulture));}
    public static Rational operator *(Rational a,Rational b)=>new(a.N*b.N,a.D*b.D);
    public static Rational operator /(Rational a,Rational b)=>b.N>0?new(a.N*b.D,a.D*b.N):throw new ArgumentOutOfRangeException(nameof(b));
    public static Rational operator +(Rational a,Rational b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
    public static Rational operator -(Rational a,Rational b)=>new(a.N*b.D-b.N*a.D,a.D*b.D);
    internal double Value
    {
        get
        {
            if(N.IsZero)return 0;
            var n=BigInteger.Abs(N);var sn=Math.Max(0,(int)n.GetBitLength()-55);var sd=Math.Max(0,(int)D.GetBitLength()-55);
            return Math.CopySign(Math.ScaleB((double)(n>>sn)/(double)(D>>sd),sn-sd),N.Sign);
        }
    }
    public override string ToString()=>D.IsOne?N.ToString():$"{N}/{D}";
}

internal sealed class InputState { internal Vector3 Linear=new(0,-9.81f,0), Angular; }
internal sealed class Capture
{
    internal int Count;
    internal readonly int[] Features=new int[4];
    internal readonly Contact[] Contacts=new Contact[4];
    internal readonly float[] Impulses=new float[7];
    internal float Friction,Omega,Damping,Recovery;
}
internal struct Callbacks(Capture capture):INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation){}
    public bool AllowContactGeneration(int worker,CollidableReference a,CollidableReference b,ref float margin)=>a.Mobility==CollidableMobility.Dynamic||b.Mobility==CollidableMobility.Dynamic;
    public bool AllowContactGeneration(int worker,CollidablePair pair,int a,int b)=>true;
    public bool ConfigureContactManifold<T>(int worker,CollidablePair pair,ref T manifold,out PairMaterialProperties material)
        where T:unmanaged,IContactManifold<T>
    {
        material=new(.5f,2,new SpringSettings(30,1));
        if(!manifold.Convex||manifold.Count>4)throw new InvalidOperationException("unexpected manifold");
        capture.Count=manifold.Count;
        for(var i=0;i<manifold.Count;i++){capture.Features[i]=manifold[i].FeatureId;capture.Contacts[i]=manifold[i];}
        return true;
    }
    public bool ConfigureContactManifold(int worker,CollidablePair pair,int a,int b,ref ConvexContactManifold manifold)=>true;
    public void Dispose(){}
}
internal struct Integrator(InputState input):IPoseIntegratorCallbacks
{
    public AngularIntegrationMode AngularIntegrationMode=>AngularIntegrationMode.ConserveMomentumWithGyroscopicTorque;
    public bool AllowSubstepsForUnconstrainedBodies=>false;
    public bool IntegrateVelocityForKinematics=>false;
    public void Initialize(Simulation simulation){}
    public void PrepareForIntegration(float dt){}
    public void IntegrateVelocity(Vector<int> indices,Vector3Wide position,QuaternionWide orientation,BodyInertiaWide inertia,
        Vector<int> mask,int worker,Vector<float> dt,ref BodyVelocityWide velocity)
    {
        velocity.Linear.X+=new Vector<float>(input.Linear.X)*dt;
        velocity.Linear.Y+=new Vector<float>(input.Linear.Y)*dt;
        velocity.Linear.Z+=new Vector<float>(input.Linear.Z)*dt;
        velocity.Angular.X+=new Vector<float>(input.Angular.X)*dt;
        velocity.Angular.Y+=new Vector<float>(input.Angular.Y)*dt;
        velocity.Angular.Z+=new Vector<float>(input.Angular.Z)*dt;
    }
}
// Deliberately READ ONLY. No exchange/writer, basis transport, scaling or correction.
internal struct Reader(Capture c):ISolverContactPrestepAndImpulsesExtractor
{
    public void ConvexOneBody<TP,TI>(ref TP prestep,ref TI impulses)
        where TP:struct,IConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI>
    {
        if(TP.ContactCount!=4)throw new InvalidOperationException("reference requires four native contact rows");
        ref var n=ref TP.GetNormal(ref prestep);ref var m=ref TP.GetMaterialProperties(ref prestep);
        c.Friction=m.FrictionCoefficient[0];c.Omega=m.SpringSettings.AngularFrequency[0];c.Damping=m.SpringSettings.TwiceDampingRatio[0];c.Recovery=m.MaximumRecoveryVelocity[0];
        for(var j=0;j<4;j++)
        {
            ref var p=ref TP.GetContact(ref prestep,j);
            c.Contacts[j].Normal=new(n.X[0],n.Y[0],n.Z[0]);c.Contacts[j].Offset=new(p.OffsetA.X[0],p.OffsetA.Y[0],p.OffsetA.Z[0]);c.Contacts[j].Depth=p.Depth[0];
            c.Impulses[j]=TI.GetPenetrationImpulseForContact(ref impulses,j)[0];
        }
        ref var t=ref TI.GetTangentFriction(ref impulses);c.Impulses[4]=t.X[0];c.Impulses[5]=t.Y[0];c.Impulses[6]=TI.GetTwistFriction(ref impulses)[0];
    }
    public void ConvexTwoBody<TP,TI>(ref TP p,ref TI i) where TP:struct,ITwoBodyConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("two body");
    public void NonconvexOneBody<TP,TI>(ref TP p,ref TI i) where TP:struct,INonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("nonconvex");
    public void NonconvexTwoBody<TP,TI>(ref TP p,ref TI i) where TP:struct,ITwoBodyNonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("nonconvex two body");
}

internal static class Program
{
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
    private static float[] V(Vector3 v)=>[v.X,v.Y,v.Z];
    private static double Norm(double x,double y,double z)=>Math.Sqrt(x*x+y*y+z*z);
    private static double Penetration(RigidPose pose)
    {
        var lowest=double.PositiveInfinity;
        for(var x=-1;x<=1;x+=2)for(var y=-1;y<=1;y+=2)for(var z=-1;z<=1;z+=2)
            lowest=Math.Min(lowest,(pose.Position+Vector3.Transform(new Vector3(x,.5f*y,.5f*z),pose.Orientation)).Y);
        return -lowest;
    }
    private static int Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("inputs.json case-name output.json target-iterations");
        var root=JsonDocument.Parse(File.ReadAllText(args[0]));
        var row=root.RootElement.GetProperty("cases").EnumerateArray().Single(x=>x.GetProperty("input").GetProperty("name").GetString()==args[1]);
        var input=row.GetProperty("input");var reference=row.GetProperty("reference");
        Rational R(string key)=>Rational.Parse(input.GetProperty(key).GetString()!);
        double Ref(string key)=>double.Parse(reference.GetProperty(key).GetString()!,CultureInfo.InvariantCulture);
        var h=R("H");var hp=R("h");var hc=h-hp;var duty=hp/h;var mass=R("m0");var ii=Rational.Parse("2");
        var fx=R("fx")*duty;var fy=R("fy")*duty;var torqueX=Rational.Parse("-1")*R("e")*fy;var torqueY=R("e")*fx;
        var ax=fx/mass;var ay=fy/mass-Rational.Parse("981/100");var wx=torqueX/ii;var wy=torqueY/ii;
        var baseline=input.GetProperty("baseline").GetBoolean();
        if(!baseline&&(R("q")*hp).ToString()!=R("fuel").ToString())throw new InvalidOperationException("inexact event debit");
        if((hp+hc).ToString()!=h.ToString())throw new InvalidOperationException("duration partition");
        var targetIterations=int.Parse(args[3],CultureInfo.InvariantCulture);
        var state=new InputState();var capture=new Capture();var pool=new BufferPool(16384);
        var stepper=new DefaultTimestepper();var snapshots=new List<object>();var stages=new List<string>();var substeps=0;
        using(var simulation=Simulation.Create(pool,new Callbacks(capture),new Integrator(state),new SolveDescription(8,1),timestepper:stepper))
        {
            var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=(float)(Rational.Parse("1")/mass).Value,InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            var floor=simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            var backendDt=(float)h.Value;
            for(var j=0;j<120;j++)simulation.Timestep(backendDt);
            // One explicit cold-fixture initial-rate assignment, never a pose/cache reset.
            simulation.Bodies[body].Velocity.Linear.X=(float)R("v0").Value;
            simulation.Bodies[body].Velocity.Angular.Y=(float)R("w0").Value;
            var sourcePose=simulation.Bodies[body].Pose;var sourceVelocity=simulation.Bodies[body].Velocity;
            int Read(string stage)
            {
                var b=simulation.Bodies[body];
                if(b.Constraints.Count!=1)throw new InvalidOperationException("expected one retained constraint");
                var handle=b.Constraints[0].ConnectingConstraintHandle;var reader=new Reader(capture);
                if(!simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader))throw new InvalidOperationException("cannot observe native constraint");
                var contacts=Enumerable.Range(0,4).Select(j=>new {feature=capture.Features[j],offset=V(capture.Contacts[j].Offset),normal=V(capture.Contacts[j].Normal),depth=capture.Contacts[j].Depth}).ToArray();
                var values=V(b.Pose.Position).Concat(new[]{b.Pose.Orientation.X,b.Pose.Orientation.Y,b.Pose.Orientation.Z,b.Pose.Orientation.W}).Concat(V(b.Velocity.Linear)).Concat(V(b.Velocity.Angular)).Concat(capture.Impulses).ToArray();
                snapshots.Add(new{stage,handle=handle.Value,body=body.Value,slab=floor.Value,count=capture.Count,position=V(b.Pose.Position),orientation=new[]{b.Pose.Orientation.X,b.Pose.Orientation.Y,b.Pose.Orientation.Z,b.Pose.Orientation.W},velocity=V(b.Velocity.Linear),angular=V(b.Velocity.Angular),contacts,impulses=capture.Impulses.ToArray(),bits=values.Select(BitConverter.SingleToInt32Bits).ToArray(),penetration=Penetration(b.Pose),friction=capture.Friction,omega=capture.Omega,damping=capture.Damping,recovery=capture.Recovery});
                stages.Add(stage);return handle.Value;
            }
            var originalHandle=Read("source_after120ordinary_steps");
            var sourceCache=capture.Impulses.ToArray();var sourceFeatures=capture.Features.ToArray();
            stepper.CollisionsDetected+=(_,_)=>Read("collision_refresh_before_solve");
            simulation.Solver.SubstepStarted+=j=>{substeps++;stages.Add($"substep_start_{j}");};
            simulation.Solver.SubstepEnded+=j=>Read($"substep_end_{j}");
            state.Linear=new((float)ax.Value,(float)ay.Value,0);state.Angular=new((float)wx.Value,(float)wy.Value,0);
            simulation.Solver.VelocityIterationCount=targetIterations;
            simulation.Timestep(backendDt);
            var finalHandle=Read("ordinary_endpoint");
            var end=simulation.Bodies[body];var p=end.Pose.Position;var v=end.Velocity.Linear;var w=end.Velocity.Angular;
            var n=capture.Contacts[0].Normal;Helpers.BuildOrthonormalBasis(n,out var t1,out var t2);
            var tangent=capture.Impulses[4]*t1+capture.Impulses[5]*t2;
            var jn=capture.Impulses.Take(4).Sum(x=>(double)x);
            var center=Vector3.Zero;var count=0;
            for(var j=0;j<4;j++)if(capture.Contacts[j].Depth>=0){center+=capture.Contacts[j].Offset;count++;}
            if(count==0){for(var j=0;j<4;j++)center+=capture.Contacts[j].Offset;count=4;}center/=count;
            var moment=Vector3.Zero;double twistCap=0;
            for(var j=0;j<4;j++){moment+=capture.Impulses[j]*Vector3.Cross(capture.Contacts[j].Offset,n);twistCap+=.125*capture.Impulses[j]*Vector3.Distance(capture.Contacts[j].Offset,center);}
            var sourceSlip=sourceVelocity.Linear+Vector3.Cross(sourceVelocity.Angular,center);
            var endSlip=v+Vector3.Cross(w,center);
            var workMid=Vector3.Dot(tangent,(sourceSlip+endSlip)*.5f);
            var workEnd=Vector3.Dot(tangent,endSlip);
            var twistMid=capture.Impulses[6]*Vector3.Dot(n,(sourceVelocity.Angular+w)*.5f);
            var expectedQ=Quaternion.CreateFromAxisAngle(Vector3.UnitY,(float)Ref("theta"));
            var q=end.Pose.Orientation;var dq=Quaternion.Multiply(Quaternion.Conjugate(expectedQ),q);
            var angleError=2*Math.Atan2(Norm(dq.X,dq.Y,dq.Z),Math.Abs(dq.W));
            var measured=new Dictionary<string,double>{["linear_velocity_error"]=Norm(v.X-Ref("vx"),v.Y-Ref("vy"),v.Z),["angular_velocity_error"]=Norm(w.X-Ref("wx"),w.Y-Ref("wy"),w.Z),
                ["displacement_error"]=Norm(p.X-sourcePose.Position.X-Ref("x"),p.Y-sourcePose.Position.Y,p.Z-sourcePose.Position.Z),["orientation_error"]=angleError,
                ["support_impulse_error"]=Math.Abs(jn-Ref("jn")),["tangent_impulse_error"]=Norm(tangent.X-Ref("jt"),tangent.Y,tangent.Z),
                ["twist_impulse_error"]=Math.Abs(capture.Impulses[6]-Ref("jtw")),["support_moment_error"]=Norm(moment.X-Ref("support_mx"),moment.Y,moment.Z-Ref("support_mz")),
                ["penetration"]=Math.Max(0,Penetration(end.Pose)),["source_penetration"]=Math.Max(0,Penetration(sourcePose))};
            var angularBar=.001/(Math.Sqrt(1.5)*h.Value);
            var bars=new Dictionary<string,double>{["linear_velocity_error"]=.06,["angular_velocity_error"]=angularBar,["displacement_error"]=.001,["orientation_error"]=.001/Math.Sqrt(1.5),["support_impulse_error"]=.48,["tangent_impulse_error"]=.48,["twist_impulse_error"]=2*angularBar,["support_moment_error"]=2*angularBar,["penetration"]=.001,["source_penetration"]=.001};
            var failures=measured.Where(x=>!double.IsFinite(x.Value)||x.Value>bars[x.Key]).Select(x=>x.Key).ToList();
            if(substeps!=1||capture.Count!=4||originalHandle!=finalHandle||jn<=0||sourceFeatures.Except(capture.Features).Any())failures.Add("retained_support_identity");
            var result=new {name=args[1],iterations=targetIterations,preparationIterations=8,preparationSteps=120,worlds=1,backendSteps=1,backendDt,backendDtBits=BitConverter.SingleToInt32Bits(backendDt),substeps,
                result=failures.Count==0?"OBSERVED_PHYSICAL_GATES_PASS_WORK_UNQUALIFIED":"FAIL",failures,measured,bars,
                ledger=new{H=h.ToString(),hp=hp.ToString(),hc=hc.ToString(),positive=hp.N>0,fuel=input.GetProperty("fuel").GetString(),successorFuel=baseline?input.GetProperty("fuel").GetString():"0",actualState=baseline?"UNCHANGED_OFF":"OFF_AT_TARGET",canonicalPublications=0},
                prepared=new{duty=duty.ToString(),sourceMass=mass.ToString(),forceX=fx.ToString(),forceY=fy.ToString(),torqueX=torqueX.ToString(),torqueY=torqueY.ToString(),linearAcceleration=V(state.Linear),angularAcceleration=V(state.Angular),inverseMass=inertia.InverseMass},
                observation=new{normalImpulse=jn,tangentImpulse=V(tangent),twistImpulse=capture.Impulses[6],normalMoment=V(moment),minimumNormalImpulse=capture.Impulses.Take(4).Min(),residualAverageNormalLoad=jn/(double)backendDt,separationTrend=p.Y-sourcePose.Position.Y,
                    tangentCap=.125*jn,twistCap,sourceCache,endpointCache=capture.Impulses.ToArray(),internalClampEvents="UNAVAILABLE",warmStartEdits=0},
                work=new{qualification="UNAVAILABLE: proxies are not native chronological work",tangentMidpointProxy=workMid,tangentEndpointProxy=workEnd,twistMidpointProxy=twistMid,referenceTangent=Ref("work_t"),referenceTwist=Ref("work_twist"),bar=.07848},
                stages,snapshots,poolBytes=pool.GetTotalAllocatedByteCount(),reference,input,
                binaryIdentity=new{bepuPath=typeof(Simulation).Assembly.Location,bepuSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Simulation).Assembly.Location))),runtime=Environment.Version.ToString(),architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()}};
            File.WriteAllText(args[2],JsonSerializer.Serialize(result,Options)+Environment.NewLine);
            Console.WriteLine(JsonSerializer.Serialize(new {name=args[1],result=result.result,failures,measured,bars,work=result.work}));
            Environment.ExitCode=failures.Count==0?0:1;
        }
        pool.Clear();return Environment.ExitCode;
    }
}
