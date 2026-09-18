using System.Reflection;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;

internal static class Probe
{
    struct Impulses : IForEach<float> { public List<float> Values; public void LoopBody(float value)=>Values.Add(value); }
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(o)!;
    static readonly JsonSerializerOptions Json=new(){IncludeFields=true};
    static void Print(string tag,object value)=>Console.WriteLine(tag+" "+JsonSerializer.Serialize(value,Json));
    static Double3 D(System.Numerics.Vector3 v)=>new(v.X,v.Y,v.Z);
    static void Require(bool b,string message){if(!b)throw new Exception(message);}
    static void Main(string[] args)
    {
        var mode=args.Single();var root="E:/NovaCore";
        Require(EarthElevationDataset.TryLoad(Path.Combine(root,"assets/earth/runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Require(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)==PhysicalSurfaceQueryStatus.Ready,"terrain");
        var site=AssemblyFloridaSite.Create(query!,SimulationInstant.Zero,new(3),48);
        var stock=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn");
        var development=AssemblyDevelopmentPropulsion.LoadDefault().Apply(stock);
        var spacecraft=new SpacecraftDefinition(new(305),new(1),new(2),"Stage5 diagnostic");
        var launch=mode=="stock-local"?AssemblyLaunch.CreateSupported(AssemblyContactProfile.Create(stock),spacecraft,"closure"):
            AssemblyLaunch.CreateFloridaSupported(development,spacecraft,"closure",site);
        using var s=AssemblyApplicationSession.CreateSupported(launch);
        var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
        var sim=Field<BepuPhysics.Simulation>(world,"simulation");var body=Field<BodyHandle>(world,"body");
        var config=Field<LocalContactConfiguration>(world,"configuration");var metrics=Field<LocalContactMetrics>(world,"metrics");
        var input=Field<LocalContactStepInput?>(world,"assemblyInput");
        var profile=launch.ContactProfile!;var mass=launch.Initial.Mass;
        if(mode=="flat-local") input!.Site=null; // Isolated native diagnostic only; no canonical service/publication in this mode.
        var initial=Native(); var f0=site.At(new(0));var f20=site.At(new(20_000_000));
        Print("INPUT",new{mode,site.Authority,site.Digest,site.OriginBodyFixed,site.LocalToBodyFixed,site.Mu,
            f0,f20,config.OriginRoot,config.LocalToRoot,config.OriginVelocityRoot,config.ContactTolerance,config.PlaneHalfExtent,
            mass,profileDigest=profile.Digest,profile.SmallestFeature,profile.BoundingRadius,children=profile.Children,initial,
            nativePose=sim.Bodies[body].Pose,nativeVelocity=sim.Bodies[body].Velocity,nativeInertia=sim.Bodies[body].LocalInertia,
            identity=world.AssemblyIdentityForTest,geometry="same production convex slab, top at local y=0",material="mu=.5 recovery=2 spring=30 damping=1",solver="8 iterations / 1 substep"});
        Terms(0,initial);Terms(20_000_000,initial);
        var tolerance=profile.SmallestFeature/1000;double peak=0,settledHeight=0,speed=0,angular=0,drift=0;
        int compliant=0,firstCompliant=0,regressions=0;bool wasCompliant=false;Double3 refP=default;
        var raw=new CompoundContactSelector.Candidate[32];var pred=new double[32];var selected=new int[4];
        for(var n=1;n<=1200;n++)
        {
            var source=(long)(n-1)*1_000_000/60;var target=(long)n*1_000_000/60;var dt=(float)((target-source)/1_000_000d);
            AssemblyMotion motion;
            if(mode=="canonical")
            {
                Require(s.Engine.AdmitAssemblyHostTime(s.Authority,n,new(target-source)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");
                var result=s.Engine.ServiceAssemblyContactDebt(s.Authority);
                Require(result.PublishedCount==1,"publication "+result.Status);
                s.Engine.ObserveAssemblyFlight(s.Authority,out var o);motion=o.State.Motion;
                Require(o.State.Stores==launch.Initial.Stores&&o.State.Mass==mass&&o.StateRevision.Value==(ulong)n&&o.HistoryCount==n&&o.Clock.Debt.Ticks==0&&o.TimelineRevision.Value==0,"accounting");
            }
            else
            {
                if(input is not null)
                {
                    input.SiteFrame=site.At(new(source));input.SiteInertia=mass.Inertia;
                    if(mode=="zero-rotation")input.SiteFrame=input.SiteFrame with {Omega=default,Alpha=default};
                    var b=sim.Bodies[body];var a=mode=="flat-local"?new Double3(0,-9.81,0):site.LinearAcceleration(input.SiteFrame,D(b.Pose.Position)+config.OriginRoot,D(b.Velocity.Linear));
                    input.Linear=new((float)a.X,(float)a.Y,(float)a.Z);input.Angular=default;
                    metrics.Coverage!.SetPreparedAcceleration(input.Linear);
                }
                metrics.Contacts=0;metrics.MaximumDepth=0;metrics.Coverage!.Begin(dt);sim.Timestep(dt);
                Require(!metrics.Coverage.Failed,"coverage");motion=Native();
            }
            var h=Height(motion);peak=Math.Max(peak,-h);var v=Math.Sqrt(motion.VelocityO.LengthSquared);var w=Math.Sqrt(motion.AngularVelocityBody.LengthSquared);
            if(n==600)refP=motion.PositionO;
            var delta=motion.PositionO-refP;var d=Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z);
            var pass=Math.Abs(h)<=2*tolerance&&d<=tolerance&&v<=tolerance/.016667&&w*profile.BoundingRadius<=tolerance/.016667;
            if(n>600){settledHeight=Math.Max(settledHeight,Math.Abs(h));speed=Math.Max(speed,v);angular=Math.Max(angular,w);drift=Math.Max(drift,d);if(pass){compliant++;if(firstCompliant==0)firstCompliant=n;}if(wasCompliant&&!pass)regressions++;wasCompliant=pass;}
            if(n is 1 or 2 or 4 or 35 or 600 or 601 or 1200)
            {
                var ch=sim.Bodies[body].Constraints[0].ConnectingConstraintHandle;
                sim.Solver.GetDescription(ch,out Contact4OneBody description);
                var impulses=new Impulses{Values=new()};sim.Solver.EnumerateAccumulatedImpulses(ch,ref impulses);
                Print("CONSTRAINT",new{mode,n,handle=ch.Value,description,impulses=impulses.Values});
                var count=world.CopyCompoundRawForTest(raw,pred,selected);
                Print("WITNESS",new{mode,n,target,motion,minimumHeight=h,speed=v,angular=w,drift=d,pass,
                    bodyPose=sim.Bodies[body].Pose,bodyVelocity=sim.Bodies[body].Velocity,body=body.Value,
                    constraints=sim.Bodies[body].Constraints.Count,raw=raw.Take(count).ToArray(),predictions=pred.Take(count).ToArray(),selected=selected.ToArray(),metrics.Contacts,metrics.MaximumDepth});
            }
            if(n==1200)Print("RESULT",new{mode,peak,settledHeight,speed,angular,drift,compliant,firstCompliant,regressions,final=motion,height=h,
                nativePose=sim.Bodies[body].Pose,nativeVelocity=sim.Bodies[body].Velocity,worldIdentity=world.AssemblyIdentityForTest,
                qualification="DIAGNOSTIC ONLY; count uses all inherited support predicates; raw modes do not publish"});
        }
        AssemblyMotion Native()
        {
            var b=sim.Bodies[body];var nq=b.Pose.Orientation;
            var q=(new DoubleQuaternion(nq.X,nq.Y,nq.Z,nq.W)*AssemblyContactProfile.Upright).Normalized();
            var p=D(b.Pose.Position)+config.OriginRoot;var v=D(b.Velocity.Linear);var w=q.Conjugate().Rotate(D(b.Velocity.Angular));
            return AssemblyContactProfile.ToOrigin(p,v,q,w,mass.Com);
        }
        double Height(AssemblyMotion m)
        {
            var q=m.BodyToWorld;var ry0=2*(q.X*q.Y+q.Z*q.W);var ry1=1-2*(q.X*q.X+q.Z*q.Z);var ry2=2*(q.Y*q.Z-q.X*q.W);
            double min=double.MaxValue;
            foreach(var child in profile.Children)for(var i=0;i<8;i++)
            {
                var x=((i&1)==0?-.5:.5)*child.Dimensions.X;var y=((i&2)==0?-.5:.5)*child.Dimensions.Y;var z=((i&4)==0?-.5:.5)*child.Dimensions.Z;
                var a=child.AtOrigin.Rotation;var o=child.AtOrigin.Position;
                var ax=o.X+a.A*x+a.B*y+a.C*z;var ay=o.Y+a.D*x+a.E*y+a.F*z;var az=o.Z+a.G*x+a.H*y+a.I*z;
                min=Math.Min(min,m.PositionO.Y+ry0*ax+ry1*ay+ry2*az-AssemblyContactProfile.SupportPlaneAtOrigin);
            }
            return min;
        }
        void Terms(long ticks,AssemblyMotion m)
        {
            var f=site.At(new(ticks));var com=AssemblyContactProfile.ToCom(m,mass.Com);
            var r=site.LocalToBodyFixed.Rotate(com.Position)+site.OriginBodyFixed;var radius=Math.Sqrt(r.LengthSquared);
            var gravity=site.LocalToBodyFixed.Conjugate().Rotate(r*(-site.Mu/(radius*radius*radius)));
            var origin=site.LocalToBodyFixed.Conjugate().Rotate(site.OriginBodyFixed);
            var originA=Double3.Cross(f.Alpha,origin)+Double3.Cross(f.Omega,Double3.Cross(f.Omega,origin));
            var centrifugal=-Double3.Cross(f.Omega,Double3.Cross(f.Omega,com.Position));
            var euler=-Double3.Cross(f.Alpha,com.Position);var coriolis=Double3.Cross(f.Omega,com.Velocity)*-2;
            var actual=site.LinearAcceleration(f,com.Position,com.Velocity);
            Print("TERMS",new{ticks,gravity,originA,centrifugal,euler,coriolis,sum=gravity-originA+centrifugal+euler+coriolis,actual,
                angular=AssemblyFloridaSite.AngularCorrection(f,m.BodyToWorld,m.BodyToWorld.Rotate(m.AngularVelocityBody),mass.Inertia),earth=site.ToEarth(m,new(ticks))});
        }
    }
}
