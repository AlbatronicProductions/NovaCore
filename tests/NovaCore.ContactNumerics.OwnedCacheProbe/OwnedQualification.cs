using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using NovaCore.Simulation.Spacecraft.Resources;

internal static class OwnedQualification
{
    private const double Bar=1e-4,OldH=(double)(1f/60),SourceMass=8+1d/128,MidMass=8+1d/256;
    private static readonly List<object> Results=new();
    private static string Current="entry";
    private static int Checks;
    private static long InstallationPiece;
    private static bool NativeWrites,NativeVerified;
    private static void Check(bool value,string text){Checks++;if(!value)throw new InvalidOperationException(text);}
    private static readonly Load Gravity=new(new(0,-9.81,0),default,default);
    private static readonly CacheIdentity Identity=new(1,0,1,0,1);
    private static CacheDuration Binary(double seconds)
    {Check(CacheDuration.FromSolver(seconds,out var value),"positive binary producing duration");return value;}
    private static CacheDuration Exact(double fuel,double flow)
    {
        Check(PropellantInteger.TryFromKilograms(fuel,out var n)&&PropellantInteger.TryDecodeFlow(flow,out _),"exact duration input");
        PropellantInteger.TryDecodeFlow(flow,out var d);
        Check(CacheDuration.FromExact(new(n,d),out var h),"exact duration retained");return h;
    }
    private static Features FeatureKey(Capture c)=>new(c.Features[0],c.Features[1],c.Features[2],c.Features[3]);
    private static NormalRow[] Rows(Capture c)=>Enumerable.Range(0,4).Select(i=>new NormalRow(c.Features[i],
        new(c.Contacts[i].Normal.X,c.Contacts[i].Normal.Y,c.Contacts[i].Normal.Z),
        new(c.Contacts[i].Offset.X,c.Contacts[i].Offset.Y,c.Contacts[i].Offset.Z))).ToArray();
    private static InverseBody Body(double mass)=>new(1/mass,new(.5,.5,.5),default);
    private static double[] Transport(CacheVector cache)=>Enumerable.Range(0,7).Select(i=>(double)(float)cache.At(i).Value).ToArray();
    private static CacheHistory Owner(Capture c,CacheVector cache,CacheDuration duration,Load load,PieceKernel.State endpoint=default)
        =>new(new(Identity,FeatureKey(c),new(c.Contacts[0].Normal.X,c.Contacts[0].Normal.Y,c.Contacts[0].Normal.Z),
            0,PieceKind.Preparation,duration,cache,load,endpoint));
    private static CacheStatus Prepare(CacheHistory history,Capture c,PieceKernel.State source,double mass,Load next,
        CacheDuration h,out CacheHistory.Prepared prepared,CacheIdentity? identity=null,long? piece=null,double[]? transport=null)
        =>history.Prepare(identity??history.Snapshot.Identity,FeatureKey(c),piece??history.Snapshot.Piece+1,
            next.Force==default&&next.Torque==default?PieceKind.Coast:PieceKind.Powered,h,
            transport??Transport(history.Snapshot.Impulses),Rows(c),c.Contacts.Select(x=>(double)x.Depth).ToArray(),
            source,Body(mass),next,c.Omega,out prepared);
    private static Capture AnalyticCapture()
    {
        var c=new Capture{Count=4,Omega=new SpringSettings(30,1).AngularFrequency,TwiceDamping=2,Friction=.5f,Recovery=2};
        var x=new[]{-1f,1f,1f,-1f};var z=new[]{-.5f,-.5f,.5f,.5f};
        for(var i=0;i<4;i++){c.Features[i]=i;c.Contacts[i]=new(){FeatureId=i,Normal=Vector3.UnitY,
            Offset=new(x[i],-.5f,z[i]),Depth=(float)(.75*(8*9.81/4)/((double)c.Omega*c.Omega))};}
        return c;
    }
    private static void Lifecycle()
    {
        var c=AnalyticCapture();var h0=Binary(OldH);var h1=Exact(1d/128,1);var h2=Binary(OldH/2);
        var cache=CacheVector.From(new[]{.25,.25,.25,.25,.01,-.01,.01});var owner=Owner(c,cache,h0,Gravity);var snapshot=owner.Snapshot;
        Current="lifecycle_prepare_discard";
        Check(Prepare(owner,c,default,8,Gravity,h1,out var a)==CacheStatus.Ready,"first preparation");
        Check(Prepare(owner,c,default,8,Gravity,h2,out var b)==CacheStatus.Ready&&owner.Snapshot==snapshot,"speculative preparation never installs");
        Check(Prepare(owner,c,default,8,Gravity,default,out _)==CacheStatus.InvalidDuration&&owner.Snapshot==snapshot,"invalid duration nonmutation");
        Check(Prepare(owner,c,default,8,Gravity,Binary(.5),out _)==CacheStatus.InvalidDuration&&owner.Snapshot==snapshot,"unchanged-load consumer duration limit");
        Check(Prepare(owner,c,default,8,default,h1,out _)==CacheStatus.UnsupportedSupport&&owner.Snapshot==snapshot,"unloaded compressed support refuses");
        Check(Prepare(owner,c,default,8,new(Gravity.Gravity,default,new(0,0,100)),h1,out _)==CacheStatus.UnsupportedSupport,"opening torque refuses");
        foreach(var key in new[]{Identity with{World=2},Identity with{Body=1},Identity with{BodyGeneration=2},
            Identity with{Constraint=1},Identity with{ManifoldGeneration=2}})
            Check(Prepare(owner,c,default,8,Gravity,h1,out _,key)==CacheStatus.IdentityMismatch&&owner.Snapshot==snapshot,"foreign cache lineage nonmutation");
        var altered=Transport(cache);altered[0]+=.001;
        Check(Prepare(owner,c,default,8,Gravity,h1,out _,transport:altered)==CacheStatus.CacheTransportChanged,"unexpected cache write");
        Check(Prepare(owner,c,new(new(.001,0,0),default),8,Gravity,h1,out _)==CacheStatus.IdentityMismatch,"external velocity differs from accepted endpoint");
        Check(Task.Run(()=>Prepare(owner,c,default,8,Gravity,h1,out _)).GetAwaiter().GetResult()==CacheStatus.WrongThread,"wrong owner");
        Check(owner.Snapshot==snapshot,"all refusal paths unchanged");
        Check(owner.BeginInstall(a,a.Scaled,default)==CacheStatus.Ready,"begin exact selected installation");
        Check(Prepare(owner,c,default,8,Gravity,h1,out _)==CacheStatus.Busy,"reentrant preparation refuses");
        var mismatch=Owner(c,cache,h0,Gravity);Prepare(mismatch,c,default,8,Gravity,h1,out var ma);Prepare(mismatch,c,default,8,Gravity,h2,out var mb);
        Check(mismatch.BeginInstall(ma,ma.Scaled,default)==CacheStatus.Ready&&mismatch.CompleteInstall(mb)==CacheStatus.InstallMismatch&&
            mismatch.Snapshot==snapshot&&mismatch.Invalidated&&mismatch.CompleteInstall(ma)==CacheStatus.InstallMismatch,
            "different prepared duration poisons install without provenance promotion");
        Check(owner.CompleteInstall(a)==CacheStatus.Ready,"accepted cache and producing duration promoted");
        Check(owner.Snapshot.Duration==h1&&owner.Snapshot.Piece==1&&owner.Snapshot.Impulses==a.Scaled,"atomic installed tuple");
        Check(owner.BeginInstall(a,a.Scaled,default)==CacheStatus.StalePiece,"duplicate accepted piece refuses");
        var installed=owner.Snapshot;
        Check(Prepare(owner,c,default,8,Gravity,h2,out _,piece:1)==CacheStatus.StalePiece&&owner.Snapshot==installed,"old submission and pending-refusal preserve NEW tuple");
        var failed=Owner(c,cache,h0,Gravity);Prepare(failed,c,default,8,Gravity,h1,out var f);
        Check(failed.BeginInstall(f,f.Scaled,default)==CacheStatus.Ready,"failure seam enters private mutation");failed.Invalidate();
        Check(failed.Snapshot==snapshot&&failed.Invalidated&&Prepare(failed,c,default,8,Gravity,h1,out _)==CacheStatus.Invalidated,"post-mutation failure poisons with no promotion");
        owner.Invalidate();Check(owner.Invalidated&&Prepare(owner,c,default,8,Gravity,h2,out _)==CacheStatus.Invalidated,"reset/disposal invalidates provenance");
        Results.Add(new{name=Current,checks=Checks,installedPiece=installed.Piece,installedDuration=installed.Duration.Numerical,canonicalMutations=0});
        Current="scaled_tiny_provenance_roundtrip";
        var tiny=Exact(double.Epsilon,2);var ordinary=Exact(1d/128,1);
        Check(!tiny.Exact.IsZero&&tiny.Numerical.Significand>0&&tiny.Numerical.Value==0,"positive exact event survives ordinary projection");
        var powers=CacheVector.From(new[]{1d/128,1d/128,1d/128,1d/128,1d/256,-1d/256,1d/512});
        Check(DurationInitialization.TryScale(powers,ordinary,tiny,out var small)&&small.N0.Mantissa>0&&small.N0.Value==0,"complete-product scaled impulse survives");
        Check(DurationInitialization.TryScale(small,tiny,ordinary,out var returned)&&returned==powers,"accepted scaled cache roundtrip all components");
        var minimum=CacheVector.From(new[]{double.Epsilon,double.Epsilon,double.Epsilon,double.Epsilon,0d,0d,0d});
        Check(DurationInitialization.TryScale(minimum,tiny,ordinary,out var finite)&&finite.N0.Value==1d/64,"overflowing standalone ratio has finite full product");
        Check(!DurationInitialization.TryScale(powers,default,tiny,out _),"missing duration no fabricated history");
        Results.Add(new{name=Current,tinyNumerical=tiny.Numerical,exactPositive=!tiny.Exact.IsZero,small,returned,
            finiteNormal=finite.N0.Value,retainedWorldQualification=false,cacheDurationBytes=Unsafe.SizeOf<CacheDuration>(),acceptedCacheBytes=Unsafe.SizeOf<AcceptedCache>()});
    }
    private static object Errors(PieceKernel.SweepResult solved,PieceKernel.OracleResult reference,out bool pass)
    {
        var impulse=Math.Abs(solved.Impulses.Take(4).Sum()-reference.Impulses.Take(4).Sum());
        var linear=(solved.Velocity.Linear-reference.Velocity.Linear).Length;var angular=(solved.Velocity.Angular-reference.Velocity.Angular).Length;
        pass=impulse<=Bar&&linear<=Bar&&angular<=Bar;return new{impulse,linear,angular,pass};
    }
    private static void Components()
    {
        foreach(var component in new[]{"normal","tangent","twist"})foreach(var h in new[]{OldH,OldH/2,OldH/4,2*OldH,1d/128})
        {
            Current="component_"+component;var c=AnalyticCapture();const double mass=8;var weight=mass*9.81;
            var p=component=="tangent"?weight/16:0;var q=component=="tangent"?weight/32:0;
            var spin=component=="twist"?weight*Math.Sqrt(1.25)/16:0;
            var rates=new[]{weight/4,weight/4,weight/4,weight/4,p,q,spin};
            var acceleration=new D3(-q/mass,-9.81,p/mass);var torque=new D3(-.5*p,-spin,-.5*q);
            var load=new Load(acceleration,default,torque);
            var history=Owner(c,CacheVector.From(rates.Select(x=>x*OldH).ToArray()),Binary(OldH),load);
            var duration=h==1d/128?Exact(h,1):Binary(h);
            var admission=Prepare(history,c,default,mass,load,duration,out var prepared);
            var guess=new double[7];
            if(h==2*OldH)
            {
                Check(admission==CacheStatus.InvalidDuration,"doubled duration remains outside consumer domain");
                Check(DurationInitialization.TryScale(history.Snapshot.Impulses,history.Snapshot.Duration,duration,out var algebraic),"doubled pure component arithmetic");
                algebraic.Project(guess);
            }
            else{Check(admission==CacheStatus.Ready,"component support admission");prepared.Project(guess);}
            var expected=rates.Select(x=>x*h).ToArray();Check(guess.Zip(expected,(a,b)=>Math.Abs(a-b)).Max()<=1e-15,"component force/torque-normalized initialization");
            var free=new PieceKernel.State(new(acceleration.X*h,acceleration.Y*h,acceleration.Z*h),new(.5*torque.X*h,.5*torque.Y*h,.5*torque.Z*h));
            var patch=new PieceKernel.Patch(c,mass);var reference=PieceKernel.Reference(patch,free,c.Omega,h);
            Check(reference.Admissible&&reference.Residual<=1e-12,"analytical component reference");
            Check(reference.Impulses.Zip(expected,(a,b)=>Math.Abs(a-b)).Max()<=1e-8,"independent explicit box reaction reference");
            var solved=PieceKernel.Solve(patch,free,guess,c.Omega,8,h);var error=Errors(solved,reference,out var pass);
            var raw=new double[7];history.Snapshot.Impulses.Project(raw);var rawSolve=PieceKernel.Solve(patch,free,raw,c.Omega,8,h);
            var oldC=c.Omega*OldH*(c.Omega*OldH+2);oldC/=1+oldC;
            var currentC=c.Omega*h*(c.Omega*h+2);currentC/=1+currentC;
            Check(history.Snapshot.Duration.Numerical.Value==OldH&&duration.Numerical.Value==h&&
                (h==OldH||currentC!=oldC),"old provenance separate from current solver coefficient");
            Results.Add(new{name=Current,h,ratio=h/OldH,guess,reference,error,rawError=Errors(rawSolve,reference,out _),
                normalClamps=solved.NormalClamps,tangentClamps=solved.TangentClamps,twistClamps=solved.TwistClamps,oldC,currentC});
            Check(pass,"component unchanged eight-sweep accuracy");
        }
        var omega=(double)new SpringSettings(30,1).AngularFrequency;
        double Unloaded(double h)=>omega*omega*h*.001/(.125*Math.Pow(1+omega*h,2));
        Results.Add(new{name="unloaded_soft_counterexample",physicalRatio=Unloaded(OldH/2)/Unloaded(OldH),durationRatio=.5,
            admission="refused by zero compressive drive; predictor is not universal physical conversion"});
    }
    private static void Main(string[] args)
    {
        string? failure=null;
        try
        {
            if(args[0]=="contracts"){Lifecycle();Components();}
            else if(args[0]=="interior")Interior();
            else throw new InvalidOperationException("Unknown bounded phase");
        }
        catch(Exception e){failure=Current+": "+e.Message;Environment.ExitCode=1;}
        var report=new{phase=args[0],status=failure is null?"PASS":"FAIL",firstFailure=failure,checks=Checks,results=Results,
            runtime=RuntimeInformation.FrameworkDescription,sweeps=8,bar=Bar,operatorQualified=false,canonicalMutations=0};
        File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);
        Console.WriteLine(JsonSerializer.Serialize(new{report.phase,report.status,report.firstFailure,report.checks,records=Results.Count}));
    }
    private static (CacheHistory.Prepared Prepared,PieceKernel.SweepResult Solved) Measure(CacheHistory owner,Capture capture,
        PieceKernel.State source,double mass,Load load,CacheDuration duration,double[] transport)
    {
        var before=owner.Snapshot;
        var admission=Prepare(owner,capture,source,mass,load,duration,out var prepared,transport:transport);
        Results.Add(new{name=Current+"_admission",admission=admission.ToString(),piece=before.Piece+1,
            previousKind=before.Kind.ToString(),previousDuration=before.Duration.Numerical,currentDuration=duration.Numerical,
            exactDurationPositive=duration.Origin==DurationOrigin.ExactEvent&&!duration.Exact.IsZero,identity=before.Identity,
            features=FeatureKey(capture),load,acceptedTupleUnchanged=owner.Snapshot==before});
        Check(admission==CacheStatus.Ready,"scoped retained-support admission: "+admission);
        var h=duration.Numerical.Value;Check(h>0,"ordinary piece consumer requires represented positive duration");
        var guess=new double[7];prepared.Project(guess);
        var a=load.Gravity+Body(mass).Mass*load.Force;var angular=Body(mass).Apply(load.Torque);
        var free=new PieceKernel.State(source.Linear+new PieceKernel.V(a.X*h,a.Y*h,a.Z*h),
            source.Angular+new PieceKernel.V(angular.X*h,angular.Y*h,angular.Z*h));
        var patch=new PieceKernel.Patch(capture,mass);var reference=PieceKernel.Reference(patch,free,capture.Omega,h);
        Check(reference.Admissible&&reference.Residual<=1e-12,"independent current-piece reference applicability");
        var solved=PieceKernel.Solve(patch,free,guess,capture.Omega,8,h);var error=Errors(solved,reference,out var pass);
        Results.Add(new{name=Current,mass,h,load,previousDuration=before.Duration.Numerical,currentDuration=duration.Numerical,
            source,guess,initialError=guess.Zip(reference.Impulses,(x,y)=>x-y).ToArray(),common=prepared.Common,
            solved,reference,error,acceptedTupleUnchanged=owner.Snapshot==before});
        Check(owner.Snapshot==before,"solving does not install or promote provenance");
        Check(pass,"unchanged eight-sweep physical accuracy gate");return (prepared,solved);
    }
    private static void Install(Simulation simulation,BodyHandle body,ConstraintHandle handle,Capture capture,CacheHistory owner,
        CacheHistory.Prepared prepared,PieceKernel.SweepResult solved)
    {
        var h=(float)prepared.Duration.Numerical.Value;
        var linear=new Vector3((float)solved.Velocity.Linear.X,(float)solved.Velocity.Linear.Y,(float)solved.Velocity.Linear.Z);
        var angular=new Vector3((float)solved.Velocity.Angular.X,(float)solved.Velocity.Angular.Y,(float)solved.Velocity.Angular.Z);
        var pose=simulation.Bodies[body].Pose;pose.Position+=linear*h;
        var dq=Quaternion.Multiply(new Quaternion(angular,0),pose.Orientation);
        pose.Orientation=Quaternion.Normalize(new(pose.Orientation.X+.5f*h*dq.X,pose.Orientation.Y+.5f*h*dq.Y,
            pose.Orientation.Z+.5f*h*dq.Z,pose.Orientation.W+.5f*h*dq.W));
        Check(float.IsFinite(linear.Length())&&float.IsFinite(angular.Length())&&float.IsFinite(pose.Position.Length())&&
            float.IsFinite(pose.Orientation.Length()),"all transport/endpoint arithmetic prepared before install");
        var wideCache=CacheVector.From(solved.Impulses);Check(wideCache.Valid,"accepted cache finite");
        for(var i=0;i<7;i++){capture.Impulses[i]=(float)solved.Impulses[i];Check(float.IsFinite(capture.Impulses[i]),"prepared cache transport");}
        var endpoint=new PieceKernel.State(PieceKernel.V.From(linear),PieceKernel.V.From(angular));
        InstallationPiece=prepared.Piece;NativeWrites=false;NativeVerified=false;
        Check(owner.BeginInstall(prepared,wideCache,endpoint)==CacheStatus.Ready,"owner enters selected fixed installation");
        try
        {
            var writer=new Extractor(capture,true);
            NativeWrites=true;
            if(!simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref writer))throw new InvalidOperationException("accepted cache writer failed");
            simulation.Bodies[body].Velocity.Linear=linear;simulation.Bodies[body].Velocity.Angular=angular;
            simulation.Bodies[body].Pose=pose;
            var reader=new Extractor(capture,false);
            if(!simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader)||
                !wideCache.MatchesTransport(capture.Impulses.Select(x=>(double)x).ToArray()))
                throw new InvalidOperationException("native installed projection did not verify");
            NativeVerified=true;
            if(owner.CompleteInstall(prepared)!=CacheStatus.Ready)throw new InvalidOperationException("private cache provenance acknowledgement failed");
        }
        catch{owner.Invalidate();throw;}
        var snapshot=owner.Snapshot;
        Results.Add(new{name=Current+"_installed",piece=snapshot.Piece,kind=snapshot.Kind.ToString(),duration=snapshot.Duration.Numerical,
            exactDurationRetained=snapshot.Duration.Exact==prepared.Duration.Exact,identity=snapshot.Identity,cache=snapshot.Impulses,
            endpoint=snapshot.Endpoint,position=pose.Position.ToString(),orientation=pose.Orientation.ToString(),invalidated=owner.Invalidated});
    }
    private static void Interior()
    {
        var powered=Exact(1d/128,1);
        PropellantInteger.TryFromKilograms(1d/128,out var fuel);PropellantInteger.TryDecodeFlow(1,out var flow);
        Check(PropellantInteger.TryMultiply(flow,16666,out var full)&&PropellantInteger.TrySubtract(full,fuel,out _),"exact admitted interval");
        PropellantInteger.TrySubtract(full,fuel,out var remainder);
        Check(CacheDuration.FromExact(new(remainder,flow),out var coast)&&powered.Numerical.Value+coast.Numerical.Value==16666d/1e6,"powered/coast exact partition");
        var pool=new BufferPool(16384);var c=new Capture();CacheHistory? owner=null;var acceptedPieces=0;var worldUnsafe=false;
        try
        {
            using var simulation=Simulation.Create(pool,new Callbacks(c),new Integrator(),new SolveDescription(8,1));
            var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=(float)(1/SourceMass),InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
                new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            for(var i=0;i<120;i++)simulation.Timestep((float)OldH);
            worldUnsafe=true;
            simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/MidMass);
            simulation.PredictBoundingBoxes((float)powered.Numerical.Value);simulation.CollisionDetection((float)powered.Numerical.Value);
            Check(c.Count==4&&simulation.Bodies[body].Constraints.Count==1,"genuine retained four-contact cache");
            var handle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;var reader=new Extractor(c,false);
            Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"genuine installed native cache");
            Check(c.Omega==new SpringSettings(30,1).AngularFrequency&&c.TwiceDamping==2&&c.Friction==.5f&&c.Recovery==2,"pinned material");
            var v=simulation.Bodies[body].Velocity;var source=new PieceKernel.State(PieceKernel.V.From(v.Linear),PieceKernel.V.From(v.Angular));
            var initial=c.Impulses.Select(x=>(double)x).ToArray();
            owner=Owner(c,CacheVector.From(initial),Binary(OldH),Gravity,source);
            Check(owner.Snapshot.Identity.Body==body.Value&&owner.Snapshot.Identity.Constraint==handle.Value,"actual world body/constraint identity");
            Results.Add(new{name="actual_seed",worlds=1,preparationSteps=120,oldDuration=owner.Snapshot.Duration.Numerical,
                oldSolverBits=owner.Snapshot.Duration.SolverBits,features=FeatureKey(c),source,initial,powered=powered.Numerical,coast=coast.Numerical});
            Current="original_owned_powered_piece";
            var first=Measure(owner,c,source,MidMass,new(Gravity.Gravity,new(0,32,0),default),powered,initial);
            foreach(var h in new[]{OldH,OldH/2,OldH/4,OldH/8,1d/128})foreach(var force in new[]{0d,16,32,64})
            {
                if(h==1d/128&&force==32)continue;
                Current="owned_composition";
                Measure(owner,c,source,MidMass,new(Gravity.Gravity,new(0,force,0),default),h==1d/128?powered:Binary(h),initial);
            }
            Current="original_owned_powered_piece";Install(simulation,body,handle,c,owner,first.Prepared,first.Solved);acceptedPieces++;worldUnsafe=false;
            var beforeCoast=owner.Snapshot;var poweredPose=simulation.Bodies[body].Pose;
            Current="owned_coast_continuation";worldUnsafe=true;
            simulation.Bodies[body].LocalInertia.InverseMass=1f/8;
            simulation.PredictBoundingBoxes((float)coast.Numerical.Value);simulation.CollisionDetection((float)coast.Numerical.Value);
            Check(c.Count==4&&simulation.Bodies[body].Constraints.Count==1&&simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle==handle,
                "same world/body/constraint continues after powered install");
            Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"coast actual retained cache");
            v=simulation.Bodies[body].Velocity;source=new(PieceKernel.V.From(v.Linear),PieceKernel.V.From(v.Angular));
            Check(source==beforeCoast.Endpoint&&simulation.Bodies[body].Pose.Position==poweredPose.Position&&
                simulation.Bodies[body].Pose.Orientation==poweredPose.Orientation,"actual installed physical endpoint continuity");
            var second=Measure(owner,c,source,8,Gravity,coast,c.Impulses.Select(x=>(double)x).ToArray());
            Check(second.Prepared.PreviousDuration==powered&&coast.Numerical.Value/powered.Numerical.Value==1.133248,"coast uses newly installed powered duration");
            Install(simulation,body,handle,c,owner,second.Prepared,second.Solved);acceptedPieces++;worldUnsafe=false;
            var pose=simulation.Bodies[body].Pose;double deepest=0;
            foreach(var x in new[]{-1f,1f})foreach(var y in new[]{-.5f,.5f})foreach(var z in new[]{-.5f,.5f})
                deepest=Math.Max(deepest,-(pose.Position+Vector3.Transform(new(x,y,z),pose.Orientation)).Y);
            Results.Add(new{name="retained_interior_continuity",acceptedPieces,totalTicks=16666,worlds=1,deepest,
                supportSpeed=Math.Abs(owner.Snapshot.Endpoint.Linear.Y),final=owner.Snapshot.Duration.Numerical,worldUnsafe});
            Check(deepest<=.020&&Math.Abs(owner.Snapshot.Endpoint.Linear.Y)<=.0005/(16667d/1e6),"independent geometry/support endpoint bounds");
        }
        catch
        {
            owner?.Invalidate();Results.Add(new{name="failure_linearization",acceptedPieces,worldUnsafe,invalidated=owner?.Invalidated,
                lastAcceptedPiece=owner?.Snapshot.Piece,lastAcceptedDuration=owner?.Snapshot.Duration.Numerical,
                acceptedCache=owner?.Snapshot.Impulses,acceptedEndpoint=owner?.Snapshot.Endpoint,
                lastInstallationPiece=InstallationPiece,lastInstallationWritesAttempted=NativeWrites,
                lastInstallationTransportVerified=NativeVerified,canonicalMutations=0});throw;
        }
        finally{pool.Clear();}
    }
}
