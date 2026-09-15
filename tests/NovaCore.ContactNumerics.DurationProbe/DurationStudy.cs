using System.Numerics;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using NovaCore.Simulation.Spacecraft.Actuation;

// Bounded initialization counterfactuals only. All accepted numerical sources are linked unchanged.
internal static class DurationStudy
{
    private const double SourceMass=8+1d/128,MidMass=8+1d/256,ActualH=1d/128,Bar=1e-4;
    private static readonly double OldH=(double)(1f/60);
    private static readonly List<object> Records=new();
    private static string Current="start";
    private static void Require(bool pass,string why){if(!pass)throw new InvalidOperationException(why);}
    private sealed record ContactData(int Feature,PieceKernel.V Offset,PieceKernel.V Normal,float Depth);
    private sealed record Frozen(double PreviousH,double HistoricalLocalH,double Omega,double Mass,
        ContactData[] Contacts,int[] Features,double[] Cache,PieceKernel.State Source,int Body,int Constraint);
    private sealed record Error(double Normal,double Linear,double Angular,double[] SignedImpulse,double[] SignedVelocity,bool Pass);
    private sealed record Measurement(string Name,double H,double Ratio,double Mass,double Load,string GuessKind,
        double[] Guess,double[] InitialError,PieceKernel.SweepResult Solved,PieceKernel.OracleResult Reference,Error Error);
    private static double Length(double[] v,int first)=>Math.Sqrt(v.Skip(first).Take(3).Sum(x=>x*x));
    private static Error Errors(PieceKernel.SweepResult s,PieceKernel.OracleResult r)
    {
        var e=s.Impulses.Zip(r.Impulses,(a,b)=>a-b).ToArray();var v=s.Velocity.Linear-r.Velocity.Linear;var w=s.Velocity.Angular-r.Velocity.Angular;
        var ve=new[]{v.X,v.Y,v.Z,w.X,w.Y,w.Z};var n=Math.Abs(e.Take(4).Sum());
        return new(n,v.Length,w.Length,e,ve,n<=Bar&&v.Length<=Bar&&w.Length<=Bar);
    }
    private static Capture CaptureOf(Frozen f)
    {
        var c=new Capture{Count=4,Omega=(float)f.Omega};
        for(var i=0;i<4;i++){var d=f.Contacts[i];c.Contacts[i]=new(){FeatureId=d.Feature,Depth=d.Depth,
            Offset=new((float)d.Offset.X,(float)d.Offset.Y,(float)d.Offset.Z),Normal=new((float)d.Normal.X,(float)d.Normal.Y,(float)d.Normal.Z)};}
        return c;
    }
    private static double[] LoadCorrect(Frozen f,double mass,double thrust,double h,double[] guess)
    {
        if(thrust==0)return (double[])guess.Clone();
        var rows=f.Contacts.Select((r,i)=>new NormalRow(i,new(r.Normal.X,r.Normal.Y,r.Normal.Z),new(r.Offset.X,r.Offset.Y,r.Offset.Z))).ToArray();
        var exponent=Math.ILogB(h);var duration=new PoweredBinaryScale(Math.ScaleB(h,-exponent),exponent);
        var body=new InverseBody(1/mass,new(.5,.5,.5),default);var previous=new Load(new(0,-9.81,0),default,default);
        var current=new Load(previous.Gravity,new(0,thrust,0),default);
        Require(CommonNormal.Prepare(rows,guess.AsSpan(0,4),body,previous,current,duration,f.Omega,out var prediction)==PredictionStatus.Ready,
            "accepted common-load domain");
        var result=(double[])guess.Clone();for(var i=0;i<4;i++)result[i]=prediction.Materialize(result[i]);return result;
    }
    private static double[] RatioGuess(double[] old,double ratio)
    {
        Require(double.IsFinite(ratio)&&ratio>0,"finite positive duration ratio");
        var result=new double[old.Length];for(var i=0;i<old.Length;i++)result[i]=old[i]*ratio;
        Require(result.All(double.IsFinite),"bounded diagnostic transformation");return result;
    }
    private static Measurement Measure(Frozen f,double h,double mass,double force,string kind)
    {
        var patch=new PieceKernel.Patch(CaptureOf(f),mass);
        var free=new PieceKernel.State(f.Source.Linear+new PieceKernel.V(0,(force/mass-9.81)*h,0),f.Source.Angular);
        var oracle=PieceKernel.Reference(patch,free,f.Omega,h);
        Require(oracle.Admissible&&oracle.Residual<=1e-12,"independent reference domain/residual");
        var ratio=h/f.PreviousH;
        double[] guess=kind switch{
            "raw"=>LoadCorrect(f,mass,force,h,f.Cache),
            "zero"=>new double[7],
            "ratio"=>LoadCorrect(f,mass,force,h,RatioGuess(f.Cache,ratio)),
            "reverse_order"=>RatioGuess(LoadCorrect(f,mass,force,h,f.Cache),ratio),
            "ideal"=>(double[])oracle.Impulses.Clone(),
            _=>throw new InvalidOperationException("Unknown diagnostic arm")};
        var solved=PieceKernel.Solve(patch,free,guess,f.Omega,8,h);
        var measured=new Measurement(Current,h,ratio,mass,force,kind,guess,guess.Zip(oracle.Impulses,(a,b)=>a-b).ToArray(),
            solved,oracle,Errors(solved,oracle));
        Records.Add(measured);return measured;
    }
    private static Frozen Capture(string oldResults)
    {
        var pool=new BufferPool(16384);var c=new Capture();Frozen frozen;
        using(var simulation=Simulation.Create(pool,new Callbacks(c),new Integrator(),new SolveDescription(8,1)))
        {
            var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=(float)(1/SourceMass),InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
                new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            for(var i=0;i<120;i++)simulation.Timestep(1f/60);
            simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/MidMass);
            simulation.PredictBoundingBoxes((float)ActualH);simulation.CollisionDetection((float)ActualH);
            Require(c.Count==4&&simulation.Bodies[body].Constraints.Count==1,"same actual retained four-row constraint");
            var handle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;var reader=new Extractor(c,false);
            Require(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"actual seven-slot cache");
            Require(c.Omega==new SpringSettings(30,1).AngularFrequency&&c.TwiceDamping==2&&c.Friction==.5f&&c.Recovery==2,"retained material identity");
            var v=simulation.Bodies[body].Velocity;
            frozen=new(OldH,1d/60,c.Omega,SourceMass,
                c.Contacts.Select(d=>new ContactData(d.FeatureId,PieceKernel.V.From(d.Offset),PieceKernel.V.From(d.Normal),d.Depth)).ToArray(),
                (int[])c.Features.Clone(),c.Impulses.Select(x=>(double)x).ToArray(),
                new(PieceKernel.V.From(v.Linear),PieceKernel.V.From(v.Angular)),body.Value,handle.Value);
            Current="original_interior_failure_reproduction";
            var observed=Measure(frozen,ActualH,MidMass,32,"raw");
            using var prior=JsonDocument.Parse(File.ReadAllText(oldResults));
            var expected=prior.RootElement.GetProperty("interior").GetProperty("results")[2];
            var oldImpulse=expected.GetProperty("corrected").GetProperty("Impulses").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            Require(observed.Solved.Impulses.SequenceEqual(oldImpulse)&&!observed.Error.Pass,"original seven impulse bits/failure reproduced");
            var e=expected.GetProperty("error");
            Require(Math.Abs(observed.Error.Normal-e.GetProperty("Impulse").GetDouble())<=1e-15&&
                observed.Error.Linear==e.GetProperty("Linear").GetDouble()&&observed.Error.Angular==e.GetProperty("Angular").GetDouble(),
                "original three errors reproduced");
            Records.Add(new{name="capture_identity",oldH=OldH,oldBits=BitConverter.DoubleToUInt64Bits(OldH),
                historicalLocalH=1d/60,historicalBits=BitConverter.DoubleToUInt64Bits(1d/60),actualRatio=ActualH/OldH,
                sourceMass=SourceMass,stageMass=MidMass,worldsCreated=1,preparationSteps=120,endpointInstalls=0,cacheInstalls=0,coastSteps=0});
        }
        pool.Clear();return frozen;
    }
    private static void Analytic(Frozen f)
    {
        var c=new Capture{Count=4};var x=new[]{-1f,1f,1f,-1f};var z=new[]{-.5f,-.5f,.5f,.5f};
        for(var i=0;i<4;i++)c.Contacts[i]=new(){Normal=Vector3.UnitY,Offset=new(x[i],-.5f,z[i]),Depth=.001f,FeatureId=i};
        const double mass=8;var weight=mass*9.81;var patch=new PieceKernel.Patch(c,mass);
        var rates=new[]{weight/4,weight/4,weight/4,weight/4,weight/16,weight/32,weight*Math.Sqrt(1.25)/16};
        for(var i=0;i<4;i++)c.Contacts[i].Depth=(float)(patch.K[i,i]*rates[i]/(f.Omega*f.Omega));
        patch=new PieceKernel.Patch(c,mass);var response=patch.Reconstruct(default,rates);
        var old=rates.Select(v=>v*f.PreviousH).ToArray();
        foreach(var h in new[]{f.PreviousH,f.PreviousH/2,f.PreviousH/4,f.PreviousH/8,2*f.PreviousH,ActualH})
        {
            Current="analytic_steady_all_components";
            var free=new PieceKernel.State((-h)*response.Linear,(-h)*response.Angular);
            var expected=rates.Select(v=>h*v).ToArray();var oracle=PieceKernel.Reference(patch,free,f.Omega,h);
            var referenceError=oracle.Impulses.Zip(expected,(a,b)=>Math.Abs(a-b)).Max();
            Require(oracle.Admissible&&oracle.Residual<=1e-12&&referenceError<=1e-8&&
                oracle.Velocity.Linear.Length<=1e-8&&oracle.Velocity.Angular.Length<=1e-8,"independent steady impulse/force reference");
            var scaled=RatioGuess(old,h/f.PreviousH);var normalsOnly=(double[])old.Clone();
            for(var i=0;i<4;i++)normalsOnly[i]*=h/f.PreviousH;
            var complete=PieceKernel.Solve(patch,free,scaled,f.Omega,8,h);
            var partial=PieceKernel.Solve(patch,free,normalsOnly,f.Omega,8,h);
            var completeError=Errors(complete,oracle);Require(completeError.Pass,"analytical full-ratio solve");
            var forceError=scaled.Select((v,i)=>Math.Abs(v/h-old[i]/f.PreviousH)).Max();
            Require(forceError<=1e-12,"normal/tangent/twist force-normalized information");
            Records.Add(new{name=Current,h,ratio=h/f.PreviousH,rates,old,expected,scaled,normalsOnly,
                referenceError,forceError,completeError,normalsOnlyError=Errors(partial,oracle),
                fullInitialError=scaled.Zip(expected,(a,b)=>Math.Abs(a-b)).Max(),
                normalsOnlyInitialError=normalsOnly.Zip(expected,(a,b)=>Math.Abs(a-b)).Max(),
                linearAcceleration=((-1d)*response.Linear),angularAcceleration=((-1d)*response.Angular)});
        }
        // Independent one-row unloading counterexample: a ratio is not a universal soft-contact identity.
        const double d=.001,k=.125;var h0=f.PreviousH;var h1=h0/2;
        double Unloaded(double h)=>f.Omega*f.Omega*h*d/(k*Math.Pow(1+f.Omega*h,2));
        var actualRatio=Unloaded(h1)/Unloaded(h0);var durationRatio=h1/h0;
        Require(Math.Abs(actualRatio-durationRatio)>.1,"transient counterexample distinguishes duration ratio");
        Records.Add(new{name="analytic_unloaded_transient_counterexample",h0,h1,lambda0=Unloaded(h0),lambda1=Unloaded(h1),actualRatio,durationRatio});
    }
    private static double[] Signed(Measurement m)=>m.Error.SignedImpulse.Concat(m.Error.SignedVelocity).ToArray();
    private static void Study(Frozen f)
    {
        Analytic(f);
        var durationResults=new List<Measurement>();
        foreach(var h in new[]{f.PreviousH,f.PreviousH/2,f.PreviousH/4,2*f.PreviousH,ActualH})
        {
            foreach(var arm in new[]{"raw","zero","ratio","ideal"})
            {
                Current="duration_only";durationResults.Add(Measure(f,h,SourceMass,0,arm));
            }
        }
        Require(durationResults.Single(x=>x.H==f.PreviousH&&x.GuessKind=="raw").Error.Pass,"unchanged-duration control");
        Require(durationResults.Any(x=>x.H!=f.PreviousH&&x.GuessKind=="raw"&&!x.Error.Pass),"duration alone must reproduce degradation");
        Require(durationResults.Where(x=>x.GuessKind is "ratio" or "ideal").All(x=>x.Error.Pass),"ratio/ideal duration-only controls");
        var factorial=new Measurement[8];
        for(var mask=0;mask<8;mask++)
        {
            var h=(mask&1)==0?f.PreviousH:ActualH;var mass=(mask&2)==0?SourceMass:MidMass;var force=(mask&4)==0?0d:32;
            Current="factor_DML_"+mask;factorial[mask]=Measure(f,h,mass,force,"raw");
        }
        var contributions=new List<object>();
        for(var mask=0;mask<8;mask++)
        {
            var value=new double[13];
            for(var sub=0;sub<8;sub++)if((sub&mask)==sub)
            {
                var sign=((System.Numerics.BitOperations.PopCount((uint)mask)-System.Numerics.BitOperations.PopCount((uint)sub))&1)==0?1:-1;
                var input=Signed(factorial[sub]);for(var i=0;i<13;i++)value[i]+=sign*input[i];
            }
            contributions.Add(new{mask,meaning="bits:1=duration,2=mass,4=load; signed Mobius factorial term",signed=value,normalSum=value.Take(4).Sum(),linearNorm=Length(value,7),angularNorm=Length(value,10)});
        }
        Records.Add(new{name="signed_factorial_decomposition",contributions});
        Current="original_interior_ratio_then_load";var corrected=Measure(f,ActualH,MidMass,32,"ratio");
        Require(corrected.Error.Pass,"duration treatment did not fix original interior case; STOP");
        Current="original_interior_load_then_ratio_negative_control";var reverse=Measure(f,ActualH,MidMass,32,"reverse_order");
        var rho=ActualH/f.PreviousH;var raw=LoadCorrect(f,MidMass,32,ActualH,f.Cache);
        var shift=raw[0]-f.Cache[0];var difference=reverse.Guess[0]-corrected.Guess[0];
        Require(Math.Abs(difference-(rho-1)*shift)<=1e-15,"derived ordering difference");
        Records.Add(new{name="ordering_identity",rho,currentLoadShift=shift,observedReverseDifference=difference,expectedReverseDifference=(rho-1)*shift});
        foreach(var h in new[]{f.PreviousH,f.PreviousH/2,f.PreviousH/4,f.PreviousH/8,ActualH})foreach(var force in new[]{0d,16,32,64})
        {
            if(h==ActualH&&force==32)continue; // strongest original corrected tuple already measured.
            Current="duration_load_mass_composition";var result=Measure(f,h,MidMass,force,"ratio");
            Require(result.Error.Pass,"duration/load composition failure; STOP");
        }
        Records.Add(new{name="scope_and_cost",candidateTransformations=1,softnessCandidateEvaluated=false,
            correctionAdopted=false,retainedWorldQualification=false,allocationMeasured=false,performanceMeasured=false,
            staticAdditionalWork="one prepared ratio and seven scalar impulse products; existing common-load projection unchanged",
            doubledDurationDomain="zero-load algebraic kernel/analytic witness only; no expansion of CommonNormal's16667-tick limit"});
    }
    private static void Main(string[] args)
    {
        string? failure=null;Frozen? f=null;
        try
        {
            if(args[0]=="capture")f=Capture(args[2]);
            else if(args[0]=="study")
            {
                using var input=JsonDocument.Parse(File.ReadAllText(args[2]));
                Require(input.RootElement.GetProperty("status").GetString()=="PASS","valid capture prerequisite");
                f=input.RootElement.GetProperty("frozen").Deserialize<Frozen>()!;Study(f);
            }
            else throw new InvalidOperationException("Unknown bounded phase");
        }
        catch(Exception e){failure=Current+": "+e.Message;Environment.ExitCode=1;}
        var report=new{phase=args[0],status=failure is null?"PASS":"FAIL",firstFailure=failure,frozen=f,records=Records,
            runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,solverSweeps=8,physicalBar=Bar,
            canonicalMutations=0,correctionAdopted=false,operatorQualified=false};
        File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);
        Console.WriteLine(JsonSerializer.Serialize(new{report.phase,report.status,report.firstFailure,records=Records.Count}));
    }
}
