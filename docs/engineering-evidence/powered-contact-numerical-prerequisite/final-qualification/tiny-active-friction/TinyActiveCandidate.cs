using System.Text.Json;
using System.Runtime.InteropServices;
using System.Reflection;
using NovaCore.Simulation.Spacecraft.Resources;
using S=PieceKernel.State;

// Cold evidence driver only. All retained operator/world files are source-linked unchanged.
// Deliberate interactive pauses preserve both real worlds while external independent
// arithmetic checks gate installation. No reference result enters the candidate solver.
internal static class TinyActiveCandidate
{
    private static bool SameJson<T>(T value,JsonElement expected)=>
        JsonElement.DeepEquals(JsonSerializer.SerializeToElement(value,Qualification.Json),expected);
    private static Scaled[] Increment(in RetainedOperator.Proposal p)=>
        [p.LinearIncrement.X,p.LinearIncrement.Y,p.LinearIncrement.Z,p.AngularIncrement.X,p.AngularIncrement.Y,p.AngularIncrement.Z];
    private static string[] Limbs(PropellantInteger value)=>Enumerable.Range(0,PropellantInteger.LimbCount).Select(i=>value.Limb(i).ToString("X16")).ToArray();
    private static object Duration(CacheDuration d)=>new{origin=d.Origin,numerical=d.Numerical,solverBits=d.SolverBits,
        exactPositive=d.Origin==DurationOrigin.ExactEvent&&!d.Exact.IsZero,valid=d.Valid,
        numeratorLimbs=Limbs(d.Exact.Numerator),denominatorLimbs=Limbs(d.Exact.Denominator)};
    private static CacheDuration Coast(CacheDuration tiny)
    {
        Require(PropellantInteger.TryMultiply(tiny.Exact.Denominator,16666,out var total),"exact outer ticks");
        Require(PropellantInteger.TrySubtract(total,tiny.Exact.Numerator,out var remainder),"exact coast remainder");
        Require(CacheDuration.FromExact(new(remainder,tiny.Exact.Denominator),out var coast),"positive exact coast");return coast;
    }
    private static void Require(bool p,string m)=>Qualification.Require(p,m);
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);
        Require(!Directory.Exists(Qualification.Output),"fresh result directory");Directory.CreateDirectory(Qualification.Output);
        Qualification.Save("attempt.json",new{runtime=RuntimeInformation.FrameworkDescription,process=Environment.ProcessId,
            thread=Environment.CurrentManagedThreadId,mode="matched exact tiny active-friction candidate",sweeps=8});
        using var frozen=JsonDocument.Parse(File.ReadAllText(Path.Combine(Qualification.Repo,
            "docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/results/tiny-event.json")));
        using var baseline=new RetainedWorld(8);using var powered=new RetainedWorld(8);
        var worlds=new[]{baseline,powered};var labels=new[]{"baseline","powered"};
        var proposals=new RetainedOperator.Proposal[2];var duration=Qualification.Exact(double.Epsilon,2);
        try
        {
            Require(duration.Valid&&!duration.Exact.IsZero&&duration.Numerical.Significand==1&&duration.Numerical.Exponent==-1075,
                "original exact tiny event authority");
            for(int i=0;i<2;i++)
            {
                var world=worlds[i];var seed=world.Operator.Snapshot;
                Require(SameJson(world.Velocity,frozen.RootElement.GetProperty("source")),"frozen source velocity bits");
                Require(SameJson(seed.ProducingGeometry,frozen.RootElement.GetProperty("accepted").GetProperty("ProducingGeometry")),"frozen producing geometry");
                Require(SameJson(seed.Cache,frozen.RootElement.GetProperty("accepted").GetProperty("Cache")),"frozen producing cache");
                var load=i==0?Qualification.Gravity:new Load(Qualification.Gravity.Gravity,new(0,16,0),default);
                var status=world.Prepare(8,load,duration,out proposals[i],out var proof);
                Qualification.Save(labels[i]+"-tiny.json",new{status,source=seed,sourcePose=world.Pose,
                    geometry=world.CurrentGeometry,duration=Duration(duration),load,proof,target=proposals[i].Target,
                    increments=Increment(proposals[i]),acceptedUnchanged=world.Operator.Snapshot==seed,world.Unsafe,
                    canonicalCapabilities=0,canonicalMutations=0});
                Require(status==OperatorStatus.Ready,"tiny candidate admission "+labels[i]+": "+status);
                Require(SameJson(world.CurrentGeometry,frozen.RootElement.GetProperty("geometry")),"same reference geometry");
                Require(world.Operator.Snapshot==seed,"tiny speculative accepted nonmutation");
            }
            Console.WriteLine("TINY_PROPOSALS_READY: independent comparison required; enter INSTALL or STOP.");
            if(Console.ReadLine()!="INSTALL") {Qualification.Save("stop.json",new{status="STOP BEFORE INSTALLATION",acceptedPieces=0,coastCalls=0});return;}
            for(int i=0;i<2;i++)
            {
                var world=worlds[i];var before=world.Operator.Snapshot;world.Install(proposals[i]);var after=world.Operator.Snapshot;
                Require(after.Generation==before.Generation+1&&after.Piece==before.Piece+1&&after.ProducingDuration==duration&&
                    after.ProducingGeometry==proposals[i].Target.ProducingGeometry&&after.Cache==proposals[i].Target.Cache&&after.Identity==before.Identity,
                    "normal exact tiny accepted installation");
                Qualification.Save(labels[i]+"-installed.json",new{status="PASS",after,duration=Duration(after.ProducingDuration),
                    pose=world.Pose,world.AcceptedPieces,world.Unsafe,canonicalMutations=0});
            }
            var coast=Coast(duration);var native=new double[2][];var seeds=new OwnedState[2];
            for(int i=0;i<2;i++)
            {
                var world=worlds[i];seeds[i]=world.Operator.Snapshot;
                Require(world.Velocity==seeds[i].Endpoint&&!world.Unsafe&&!world.Operator.Invalidated,"accepted coast source");
                // Split the existing cold geometry query from the unchanged numerical operator
                // so an independent reference can be frozen before its first coast call.
                var geometry=world.ObserveTinyGeometry(coast,8);
                var capture=(Capture)typeof(RetainedWorld).GetField("capture",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(world)!;
                native[i]=capture.Impulses.Select(x=>(double)x).ToArray();
                Require(seeds[i].Cache.MatchesTransport(native[i]),"actual post-refresh native cache matches owned source");
                Qualification.Save(labels[i]+"-coast-input.json",new{source=world.Velocity,geometry,accepted=seeds[i],
                    sourcePose=world.Pose,nativeCache=native[i],duration=Duration(coast),load=Qualification.Gravity,
                    acceptedUnchanged=world.Operator.Snapshot==seeds[i],world.Unsafe,candidateCoastCalls=0});
            }
            Console.WriteLine("COAST_INPUTS_READY: independent coast reference required; enter COAST or STOP.");
            if(Console.ReadLine()!="COAST")return;
            for(int i=0;i<2;i++)
            {
                var world=worlds[i];var seed=seeds[i];
                var status=world.Operator.Prepare(seed.Identity,seed.Generation,seed.Piece+1,PieceKind.Coast,
                    world.CurrentGeometry,coast,Qualification.Body(8),Qualification.Gravity,world.Velocity,native[i],out proposals[i],out var proof);
                Qualification.Save(labels[i]+"-coast.json",new{status,proof,target=proposals[i].Target,increments=Increment(proposals[i]),
                    acceptedUnchanged=world.Operator.Snapshot==seed,world.Unsafe,canonicalMutations=0});
                Require(status==OperatorStatus.Ready,"coast candidate admission "+labels[i]+": "+status);
            }
            Console.WriteLine("COAST_PROPOSALS_READY: independent comparison required; enter INSTALL or STOP.");
            if(Console.ReadLine()!="INSTALL")return;
            for(int i=0;i<2;i++)
            {
                worlds[i].Install(proposals[i]);Qualification.Save(labels[i]+"-coast-installed.json",new{status="PASS",
                    accepted=worlds[i].Operator.Snapshot,duration=Duration(worlds[i].Operator.Snapshot.ProducingDuration),canonicalMutations=0});
            }
        }
        catch(Exception e)
        {
            Qualification.Save("failure.json",new{status="STOP",error=e.Message,type=e.GetType().Name,
                baselineAccepted=baseline.AcceptedPieces,poweredAccepted=powered.AcceptedPieces});
            Environment.ExitCode=1;Console.WriteLine("STOP: "+e.Message);
        }
    }
}
