using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

internal static class Qualification
{
    private const double H=1d/60,Bar=1e-4;
    private static readonly List<object> Results=new();
    private static string Current="entry";
    private static void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);}
    private static PoweredBinaryScale Duration(double h)
    {var e=Math.ILogB(h);return new(Math.ScaleB(h,-e),e);}
    private static readonly Load Gravity=new(new(0,-9.81,0),default,default);
    private static InverseBody Body(double mass)=>new(1/mass,new(.5,.5,.5),default);
    private static double Oracle(NormalRow[] rows,InverseBody body,Load old,Load next,double h,double omega)
    {
        // Independent expanded all-pairs Galerkin assembly, not aggregate-J implementation.
        double sum=0,diagonal=0,q=0;
        var dv=next.Gravity-old.Gravity+body.Mass*(next.Force-old.Force);
        var dw=body.Apply(next.Torque-old.Torque);
        foreach(var a in rows)
        {
            var am=D3.Cross(a.Lever,a.Normal);
            q+=D3.Dot(a.Normal,dv)+D3.Dot(am,dw);
            foreach(var b in rows)
            {
                var k=body.Mass*D3.Dot(a.Normal,b.Normal)+D3.Dot(am,body.Apply(D3.Cross(b.Lever,b.Normal)));
                sum+=k;if(a.Identity==b.Identity)diagonal+=k;
            }
        }
        return -h*q/(sum+diagonal/(omega*h*(omega*h+2)));
    }
    private static void Synthetic()
    {
        var up=new D3(0,1,0);var omega=(double)new BepuPhysics.Constraints.SpringSettings(30,1).AngularFrequency;
        NormalRow Row(int id,double x,double z)=>new(id,up,new(x,-.5,z));
        var sets=new[]{new[]{Row(1,0,0)},new[]{Row(1,-1,0),Row(2,2,0)},
            new[]{Row(1,-1,-.5),Row(2,.75,-.5),Row(3,.25,1)},
            new[]{Row(1,-1,-.5),Row(2,1,-.5),Row(3,1,.5),Row(4,-1,.5)},
            new[]{Row(1,-1,-.5),Row(2,1.25,-.4),Row(3,.7,.6),Row(4,-.8,.8)},
            new[]{Row(1,-1,-.5),new NormalRow(2,new(.6,.8,0),new(1,-.5,.5)),Row(3,.25,.7)}};
        var loads=new[]{new Load(Gravity.Gravity,new(0,32,0),default),
            new Load(Gravity.Gravity,new(3,16,4),default),new Load(Gravity.Gravity,new(32,0,0),default),
            new Load(Gravity.Gravity,default,new(0,0,8)),Gravity};
        foreach(var rows in sets)foreach(var next in loads)
        {
            Current=$"synthetic_{Results.Count}_N{rows.Length}";
            var original=Enumerable.Range(0,rows.Length).Select(i=>2+i*.125).ToArray();
            var before=(double[])original.Clone();
            var b=Body(8+1d/256);
            var status=CommonNormal.Prepare(rows,original,b,Gravity,next,Duration(H),omega,out var p);
            var expected=Oracle(rows,b,Gravity,next,H,omega);
            var error=Math.Abs(p.Shift.Value-expected);
            Check(status==PredictionStatus.Ready&&error<=1e-12,"independent projected normal oracle");
            Check(original.SequenceEqual(before),"input differential cache immutable");
            for(var i=1;i<rows.Length;i++)
                Check(Math.Abs((p.Materialize(original[i])-p.Materialize(original[0]))-(original[i]-original[0]))<=1e-14,
                    "materialized normal differences");
            var reversed=rows.Reverse().ToArray();
            Check(CommonNormal.Prepare(reversed,original.Reverse().ToArray(),b,Gravity,next,Duration(H),omega,out var rp)==PredictionStatus.Ready&&
                Math.Abs(rp.Shift.Value-p.Shift.Value)<=1e-12,"row permutation covariance");
            Check(CommonNormal.Prepare(rows,original,b,next,next,Duration(H),omega,out var identity)==PredictionStatus.Ready&&
                identity.Shift.Mantissa==0,"repeated unchanged nonzero load is identity");
            if(next==Gravity)Check(p.Shift.Mantissa==0,"zero load change");
            Results.Add(new {name=Current,rows=rows.Length,next,status,shift=p.Shift,error,projection=p.ProjectedAcceleration});
        }
        Current="accepted_symmetric_specialization";
        var symmetric=sets[3];var cache=new[]{1d,1,1,1};var body=Body(8+1d/256);
        CommonNormal.Prepare(symmetric,cache,body,Gravity,loads[0],Duration(H),omega,out var accepted);
        Check(Math.Abs(accepted.Shift.Value-(-.12199946117364349))<=1e-12,"accepted symmetric adjustment");
        Results.Add(new{name=Current,shift=accepted.Shift.Value});
        Current="mass_only_identity";
        Check(CommonNormal.Prepare(symmetric,cache,Body(8),Gravity,Gravity,Duration(H),omega,out var mass)==PredictionStatus.Ready&&
            mass.Shift.Mantissa==0,"same-current-mass load comparison");
        Results.Add(new{name=Current,shift=mass.Shift.Value});
        Current="tiny_scaled_preconditioner_only";
        Check(PropellantInteger.TryFromKilograms(double.Epsilon,out var fuel)&&PropellantInteger.TryDecodeFlow(2,out _),"exact tiny inputs");
        PropellantInteger.TryDecodeFlow(2,out var flow);var exact=new PropellantDuration(fuel,flow);
        Check(PoweredFlightNumerics.TrySeconds(exact,out var tiny)&&!exact.IsZero&&tiny.Value==0,"exact duration not collapsed");
        Check(CommonNormal.Prepare(symmetric,cache,body,Gravity,loads[0],tiny,omega,out var tp)==PredictionStatus.Ready&&
            tp.Shift.Mantissa<0,"scaled nonzero common response retained");
        // h->0: beta/h^2=-2*omega*sum(J delta_a)/sum(Kii). Terms in omega*h are below binary64 resolution.
        var normalizedExpected=-2*omega*4*32*body.Mass/(4*(body.Mass+5d/8));
        var normalizedActual=Math.ScaleB(tp.Shift.Mantissa,tp.Shift.Exponent-2*tiny.Exponent);
        Check(Math.Abs((normalizedActual-normalizedExpected)/normalizedExpected)<=2e-15,"independent tiny coefficient");
        Results.Add(new{name=Current,tiny,shift=tp.Shift,normalizedActual,normalizedExpected,worldQualification=false});
        Current="fail_closed_inputs";
        var negative=CommonNormal.Prepare(symmetric,new double[4],body,Gravity,loads[0],Duration(H),omega,out _);
        var count=CommonNormal.Prepare(Array.Empty<NormalRow>(),Array.Empty<double>(),body,Gravity,loads[0],Duration(H),omega,out _);
        var invalid=CommonNormal.Prepare(symmetric,cache,new(-1,body.Diagonal,default),Gravity,loads[0],Duration(H),omega,out _);
        var zero=CommonNormal.Prepare(symmetric,cache,body,Gravity,loads[0],default,omega,out _);
        Check(negative==PredictionStatus.NegativeGuess&&count==PredictionStatus.InvalidInput&&invalid==PredictionStatus.InvalidInput&&
            zero==PredictionStatus.UnsupportedDuration,"bounded refusals");
        var splitNegative=CommonNormal.Prepare(new[]{new NormalRow(1,up,default)},new[]{double.Epsilon},
            new(1,new(1,1,1),default),Gravity,new(Gravity.Gravity,new(0,80*double.Epsilon,0),default),Duration(H),omega,out _);
        Check(splitNegative==PredictionStatus.NegativeGuess,"split-sign refusal before projection");
        var lattice=CommonNormal.Prepare(symmetric,cache,body,Gravity,Gravity,Duration(16667d/1e6),omega,out _);
        Check(lattice==PredictionStatus.Ready,"ordinary lattice maximum admitted");
        Results.Add(new{name=Current,negative,count,invalid,zero,splitNegative,lattice});
    }
    private static Vector3 Parse(string s)
    {var values=s.Trim('<','>').Split(',').Select(v=>float.Parse(v,CultureInfo.InvariantCulture)).ToArray();return new(values[0],values[1],values[2]);}
    private static PieceKernel.V ReadV(JsonElement j)=>new(j.GetProperty("X").GetDouble(),j.GetProperty("Y").GetDouble(),j.GetProperty("Z").GetDouble());
    private static NormalRow[] Rows(Capture c)=>Enumerable.Range(0,4).Select(i=>new NormalRow(i,
        new(c.Contacts[i].Normal.X,c.Contacts[i].Normal.Y,c.Contacts[i].Normal.Z),
        new(c.Contacts[i].Offset.X,c.Contacts[i].Offset.Y,c.Contacts[i].Offset.Z))).ToArray();
    private static double[] Predict(Capture c,double[] original,double mass,Load previous,Load current,double h,double omega,out CommonPrediction p)
    {
        var status=CommonNormal.Prepare(Rows(c),original.AsSpan(0,4),Body(mass),previous,current,Duration(h),omega,out p);
        Check(status==PredictionStatus.Ready,"preconditioner refuses: "+status);
        var guess=(double[])original.Clone();for(var i=0;i<4;i++)guess[i]=p.Materialize(guess[i]);
        Check(guess.AsSpan(4).SequenceEqual(original.AsSpan(4)),"tangent and twist unchanged");return guess;
    }
    private sealed record Error(double Impulse,double Linear,double Angular,bool Pass);
    private static Error Errors(PieceKernel.SweepResult s,PieceKernel.OracleResult r)
    {
        var impulse=Math.Abs(s.Impulses.Take(4).Sum()-r.Impulses.Take(4).Sum());
        var linear=(s.Velocity.Linear-r.Velocity.Linear).Length;var angular=(s.Velocity.Angular-r.Velocity.Angular).Length;
        return new(impulse,linear,angular,impulse<=Bar&&linear<=Bar&&angular<=Bar);
    }
    private static object LocalCase(Capture capture,PieceKernel.State source,double[] initial,double mass,double force,double h,double omega,
        Load previous,out PieceKernel.SweepResult corrected)
    {
        var patch=new PieceKernel.Patch(capture,mass);
        var next=new Load(Gravity.Gravity,new(0,force,0),default);
        var free=new PieceKernel.State(source.Linear+new PieceKernel.V(0,(force/mass-9.81)*h,0),source.Angular);
        var reference=PieceKernel.Reference(patch,free,omega,h);
        Check(reference.Admissible&&reference.Residual<=1e-12,"independent local reference applicability");
        var guess=Predict(capture,initial,mass,previous,next,h,omega,out var prediction);
        corrected=PieceKernel.Solve(patch,free,guess,omega,8,h);
        var original=PieceKernel.Solve(patch,free,initial,omega,8,h);
        var cold=PieceKernel.Solve(patch,free,new double[7],omega,8,h);
        var actual=Errors(corrected,reference);
        var initialError=guess.Zip(reference.Impulses,(a,b)=>a-b).ToArray();
        var result=new{name=Current,mass,force,h,initial,guess,prediction,initialError,corrected,reference,
            error=actual,originalError=Errors(original,reference),coldError=Errors(cold,reference)};
        Results.Add(result);
        Check(actual.Pass,"eight-sweep physical accuracy gate");
        return result;
    }
    private static void Local(string input)
    {
        Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input)))=="C4F6EFA2C2F7F60F0DCDBFFAFF847AFD145FE275951F751AE3F9C2D94E526E22",
            "historical captured-input bytes");
        using var doc=JsonDocument.Parse(File.ReadAllText(input));var root=doc.RootElement;var f=root.GetProperty("results")[2];
        var c=new Capture();var i=0;foreach(var item in root.GetProperty("contacts").EnumerateArray())
            c.Contacts[i++]=new(){Offset=Parse(item.GetProperty("offset").GetString()!),Normal=Parse(item.GetProperty("normal").GetString()!),
                Depth=item.GetProperty("Depth").GetSingle(),FeatureId=item.GetProperty("FeatureId").GetInt32()};
        var source=new PieceKernel.State(ReadV(f.GetProperty("source").GetProperty("Linear")),ReadV(f.GetProperty("source").GetProperty("Angular")));
        var initial=f.GetProperty("initial").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        var mass=f.GetProperty("stageMass").GetDouble();var omega=root.GetProperty("omega").GetDouble();
        Current="unchanged_equation_port_identity";
        var patch=new PieceKernel.Patch(c,mass);var free=new PieceKernel.State(source.Linear+new PieceKernel.V(0,(32/mass-9.81)*H,0),source.Angular);
        var original=PieceKernel.Solve(patch,free,initial,omega,8,H);
        var recorded=f.GetProperty("last").GetProperty("Impulses").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        Check(original.Impulses.SequenceEqual(recorded),"all seven original impulse bits preserved by explicit-h port");
        Results.Add(new{name=Current,allSevenImpulseBitsIdentical=true});
        foreach(var force in new[]{32d,0,8,16,64})
        {Current="centered_load_"+force;LocalCase(c,source,initial,mass,force,H,omega,Gravity,out _);}
    }
    private static void Interior()
    {
        const double sourceMass=8+1d/128;
        Check(PropellantInteger.TryFromKilograms(1d/128,out var fuel),"exact fuel");
        Check(PropellantInteger.TryDecodeFlow(1,out var flow),"exact one kg/s flow");
        Check(PropellantInteger.TryMultiply(flow,16666,out var full)&&PropellantInteger.TrySubtract(full,fuel,out _),"interior exact interval");
        PropellantInteger.TrySubtract(full,fuel,out var remainder);
        var poweredDuration=new PropellantDuration(fuel,flow);var coastDuration=new PropellantDuration(remainder,flow);
        Check(PoweredFlightNumerics.TrySeconds(poweredDuration,out var poweredSeconds)&&
            PoweredFlightNumerics.TrySeconds(coastDuration,out _),"banked exact piece scales");
        PoweredFlightNumerics.TrySeconds(coastDuration,out var coastSeconds);
        Check(PropellantInteger.TryFromKilograms(8,out var dry)&&
            PoweredFlightNumerics.TryStageMass(dry,default,fuel,1,2,out _),"banked midpoint");
        PoweredFlightNumerics.TryStageMass(dry,default,fuel,1,2,out var midpoint);
        Check(poweredSeconds.Value==1d/128&&poweredSeconds.Value+coastSeconds.Value==16666d/1e6,
            "exact ordered piece projection");
        Results.Add(new{name="interior_exact_input",fuelKilograms=1d/128,flowKilogramsPerSecond=1,admittedTicks=16666,
            poweredSeconds,coastSeconds,midpoint,successorMass=8});
        var pool=new BufferPool(16384);var capture=new Capture();
        using(var simulation=Simulation.Create(pool,new Callbacks(capture),new Integrator(),new SolveDescription(8,1)))
        {
            var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=(float)(1/sourceMass),InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
                new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            for(var i=0;i<120;i++)simulation.Timestep(1f/60);
            var previous=Gravity;var originalHandle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;
            var startPose=simulation.Bodies[body].Pose;
            for(var piece=0;piece<2;piece++)
            {
                Current=piece==0?"interior_powered_7812_5_ticks":"interior_coast_8853_5_ticks";
                var h=piece==0?poweredSeconds.Value:coastSeconds.Value;
                var mass=piece==0?midpoint:8;var force=piece==0?32d:0;
                simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/mass);
                simulation.PredictBoundingBoxes((float)h);simulation.CollisionDetection((float)h);
                Check(capture.Count==4&&simulation.Bodies[body].Constraints.Count==1,"same four-contact support");
                var handle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;var reader=new Extractor(capture,false);
                Check(handle==originalHandle&&simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"retained constraint cache");
                var velocity=simulation.Bodies[body].Velocity;var source=new PieceKernel.State(PieceKernel.V.From(velocity.Linear),PieceKernel.V.From(velocity.Angular));
                var initial=capture.Impulses.Select(x=>(double)x).ToArray();
                Results.Add(new{name=Current+"_admission",body=body.Value,constraint=handle.Value,features=(int[])capture.Features.Clone(),
                    pose=simulation.Bodies[body].Pose.Position.ToString(),source,initial,worldsCreated=1,canonicalMutations=0});
                LocalCase(capture,source,initial,mass,force,h,capture.Omega,previous,out var solved);
                // Only a locally admitted endpoint may become retained private continuation.
                ref var v=ref simulation.Bodies[body].Velocity;
                v.Linear=new((float)solved.Velocity.Linear.X,(float)solved.Velocity.Linear.Y,(float)solved.Velocity.Linear.Z);
                v.Angular=new((float)solved.Velocity.Angular.X,(float)solved.Velocity.Angular.Y,(float)solved.Velocity.Angular.Z);
                ref var pose=ref simulation.Bodies[body].Pose;
                pose.Position+=v.Linear*(float)h;
                var w=new Quaternion(v.Angular,0);var dq=Quaternion.Multiply(w,pose.Orientation);
                pose.Orientation=Quaternion.Normalize(new(pose.Orientation.X+.5f*(float)h*dq.X,pose.Orientation.Y+.5f*(float)h*dq.Y,
                    pose.Orientation.Z+.5f*(float)h*dq.Z,pose.Orientation.W+.5f*(float)h*dq.W));
                for(var i=0;i<7;i++)capture.Impulses[i]=(float)solved.Impulses[i];
                var writer=new Extractor(capture,true);
                Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref writer),"same private cache install");
                previous=new(Gravity.Gravity,new(0,force,0),default);
            }
            Results.Add(new{name="interior_continuity",startPose=startPose.Position.ToString(),endPose=simulation.Bodies[body].Pose.Position.ToString(),
                body=body.Value,constraint=originalHandle.Value,worldsCreated=1,pieces=2,totalTicks=16666,canonicalMutations=0});
        }
        pool.Clear();
    }
    private static void Main(string[] args)
    {
        string? failure=null;
        try
        {
            switch(args[0])
            {case "synthetic":Synthetic();break;case "local":Local(args[2]);break;case "interior":Interior();break;
                default:throw new InvalidOperationException("Unknown bounded phase");}
        }
        catch(Exception e){failure=Current+": "+e.Message;Environment.ExitCode=1;}
        var report=new{phase=args[0],status=failure is null?"PASS":"FAIL",firstFailure=failure,results=Results,
            runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,iterations=8,impulseBar=Bar,velocityBar=Bar,
            operatorQualified=false,canonicalMutations=0};
        File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);
        Console.WriteLine(JsonSerializer.Serialize(new{report.phase,report.status,report.firstFailure,cases=Results.Count}));
    }
}
