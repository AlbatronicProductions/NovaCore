using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static ContactGenerationFixture;
using Case = FloridaContactProductionTests.Case;
using R = CertifiedResponseOracle.R;

internal static class PrivateFloridaPropagationTests
{
    private static void Check(bool value,string name)=>ContactGenerationFixture.Check(value,"paired Florida propagation "+name);
    private static readonly FloridaKinematicsRequest Kinematics=new(1e-5,1e-8,1e-5,1e-12,24);
    private static readonly CertifiedResponseRequest Response=new(1e-6,1e-6,1e-6,3e-6);
    private static readonly CertifiedPreImpactVelocityRequest Preimpact=new(1e-5,1e-4);
    private static readonly PrivatePostImpactPoseRequest Pose=new(.5);
    // Predeclared absolute OUTPUT errors, not contact/clearance tolerances. 2 mm covers ~64 ULPs
    // at 1 AU; a <.2 m/s² across a <10 us root bracket requires <2 us-equivalent velocity error.
    // Spin/attitude requests allow <10 us root uncertainty at the retained <.4 rad/s spin scale.
    private static readonly PrivatePropagationRequest Request=new(.002,2e-6,2e-6,4e-6);
    private readonly record struct Inputs(Case Case,FloridaContactProvider.Proof Root,
        FloridaContactProvider.Proof.PostImpactVelocity Realization,FloridaContactProvider.Proof.PrivatePostImpactState Initial)
    {
        internal PrivatePropagationResult Prepare()=>Root.PreparePrivatePropagation(Initial,Realization,Pose,Case.End,Request,Case.Use);
        internal PrivatePropagationStatus Read(in FloridaContactProvider.Proof.PrivateCanonicalState state,out PrivateCanonicalStateValues result)=>
            state.Read(Root,Initial,Realization,Pose,Case.End,Request,Case.Use,out result);
    }
    internal static void Run()
    {
        var repository=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(repository,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"real Earth authority");
        var query=acquired!;
        foreach(var force in new[]{false,true})
        {
            var s=Source(Create(query,force));var before=Snapshot(s.Case);var p=s.Prepare();
            Check(p.Status==PrivatePropagationStatus.Ready,"preparation "+force+": "+p.Failure);
            Check(s.Read(p.State,out var value)==PrivatePropagationStatus.Ready,"checked complete read");
            Validate(s,value);var replay=ReplayKey(value);
            Console.WriteLine("PRIVATE_PROPAGATION_REPLAY "+(force?"force ":"coast ")+replay);
            for(var i=0;i<8;i++)Check(s.Read(p.State,out var again)==PrivatePropagationStatus.Ready&&ReplayKey(again)==replay,"repeat complete bits");
            var independent=Source(Create(query,force));var pp=independent.Prepare();
            Check(pp.Status==PrivatePropagationStatus.Ready&&independent.Read(pp.State,out var other)==PrivatePropagationStatus.Ready&&ReplayKey(other)==replay,"independent full chain replay");
            Mismatches(s,p.State,independent);
            Check(before==Snapshot(s.Case),"success and refusal preserve complete canonical authority");
        }
        Refusals(query);
        var measured=Source(Create(query));var initialStatus=measured.Initial.Read(measured.Root,measured.Realization,Pose,measured.Case.Use,out var initial);
        Check(initialStatus==PrivatePostImpactStateStatus.Ready,"measured initial state");
        var prepared=measured.Prepare();Check(measured.Read(prepared.State,out var ready)==PrivatePropagationStatus.Ready,"measurement ready");
        var baseline=Snapshot(measured.Case);
        Measure("source preparation",()=>TrySource(measured.Case,out _),false);
        Measure("translation evaluation",()=>PrivatePropagationMath.Translation(initial.FrozenSourceTranslation,initial.Properties.MassKilograms,
            initial.InitialLinearVelocityRoot,initial.PoseRootEnclosure,initial.SourceEnd,out _)==PrivatePropagationFailure.None);
        Measure("rotational calculation",()=>PrivatePropagationMath.Rotation(initial.FrozenSourceRotation.OrientationLocalToParent,initial.InitialAngularVelocityBody,
            initial.FrozenSourceRotation.PrincipalInertia,ready.RemainderSeconds,out _)==PrivatePropagationFailure.None);
        Measure("paired staged construction",()=>measured.Prepare().Status==PrivatePropagationStatus.Ready);
        Measure("checked consumption",()=>measured.Read(prepared.State,out _)==PrivatePropagationStatus.Ready);
        var tight=Request with {PositionMetres=1e-30};
        Measure("numerical refusal",()=>measured.Root.PreparePrivatePropagation(measured.Initial,measured.Realization,Pose,measured.Case.End,tight,measured.Case.Use).Failure==PrivatePropagationFailure.EndpointResolution);
        var foreign=Source(Create(query));
        Measure("stale refusal",()=>measured.Root.PreparePrivatePropagation(measured.Initial,measured.Realization,Pose,measured.Case.End,Request,foreign.Case.Use).Status==PrivatePropagationStatus.Stale);
        OrdinaryAllocationMeasurement.PositiveControl();
        Check(baseline==Snapshot(measured.Case),"measurement nonmutation");
        Console.WriteLine(JsonSerializer.Serialize(new{kind="private-propagation-workspace",degree=PrivatePropagationMath.Degree,
            remainderOrder=PrivatePropagationMath.RemainderOrder,coefficientPairs=66,majorantPairs=78,substeps=0,retries=0,
            stackWorkspaceBytes=PrivatePropagationMath.WorkspaceBytes,receiptBytes=Unsafe.SizeOf<FloridaContactProvider.Proof.PrivateCanonicalState>(),
            valuesBytes=Unsafe.SizeOf<PrivateCanonicalStateValues>(),endpointBytes=Unsafe.SizeOf<PrivatePropagationEndpoint>(),
            eventCoverage="UNKNOWN",newProviderObjects=0,rootSelections=0,canonicalWrites=0}));
        Console.WriteLine("PRIVATE_FLORIDA_PROPAGATION coast/force/paired/replay/foreign/stale/coverage/nonmutation/allocation PASS");
    }
    private static SimulationClock Clock(Case c)=>(SimulationClock)typeof(SimulationTransactionEngine).GetField("_clock",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(c.Engine)!;
    private static Case Create(PlanetaryPhysicalSurfacePointQuery query,bool force=true)
    {
        var c=new Case(query,.5,-1,radialAcceleration:force?-.2:0,attitude:new(0,0,.6,.8));
        Check(Clock(c).AdvanceByHostDuration(new(1_000)).Reason==SimulationHostAdvanceStopReason.Accepted&&Clock(c).PendingSimulationDebt.Ticks==1_000,"1000 ticks debt before admission");
        return c;
    }
    private static bool TrySource(Case c,out Inputs source)
    {
        source=default;if(!c.Admit(out var provider,out _))return false;
        var root=c.Evaluate(provider!).Proof;var k=root.QualifyKinematics(Kinematics,c.Use);
        if(!root.IsRoot||k.Status!=FloridaKinematicsStatus.Qualified)return false;
        var r=root.QualifyResponse(k.Witness,Response,c.Use);var p=root.QualifyPreImpactVelocity(k.Witness,Preimpact,c.Use);
        if(r.Status!=CertifiedResponseStatus.Qualified||p.Status!=CertifiedPreImpactVelocityStatus.Qualified)return false;
        var v=root.QualifyPostImpactVelocity(k.Witness,r.Proposal,Response,p.Tuple,Preimpact,c.Use);
        if(v.Status!=CertifiedPostImpactVelocityStatus.Qualified)return false;
        var initial=root.PreparePrivatePostImpactState(k.Witness,r.Proposal,Response,p.Tuple,Preimpact,v.Realization,Pose,c.Use);
        if(initial.Status!=PrivatePostImpactStateStatus.Ready)return false;
        source=new(c,root,v.Realization,initial.State);return true;
    }
    private static Inputs Source(Case c){Check(TrySource(c,out var s),"real M14.9 through M14.14 checked chain");return s;}
    private static void Validate(Inputs s,PrivateCanonicalStateValues v)
    {
        Check(s.Initial.Read(s.Root,s.Realization,Pose,s.Case.Use,out var initial)==PrivatePostImpactStateStatus.Ready,"M14.14 read");
        Check(v.Initial==initial&&v.Target==initial.SourceEnd&&v.Target==s.Case.End,"same root/source and exact target");
        Check(VectorBits(v.Initial.InitialLinearVelocityRoot).SequenceEqual(VectorBits(initial.InitialLinearVelocityRoot))&&
            VectorBits(v.Initial.InitialAngularVelocityBody).SequenceEqual(VectorBits(initial.InitialAngularVelocityBody)),"exact initial derivative bits");
        Check(v.EventCoverage==PrivatePropagationCoverage.Unknown,"EVENT COVERAGE UNKNOWN");
        Check(v.RemainderSeconds.IsFinite&&v.RemainderSeconds.Lower>=0&&v.RemainderSeconds.Upper<=1,"bounded linked remainder");
        Check(v.Endpoint.AngularVelocityBody!=initial.InitialAngularVelocityBody,"asymmetric post-spin evolves");
        PrivateCanonicalPropagationTests.AssertTranslation(initial.FrozenSourceTranslation,initial.Properties.MassKilograms,
            initial.InitialLinearVelocityRoot,initial.PoseRootEnclosure,v.Target,v.Endpoint.Translation);
        var e=v.Endpoint;
        Error(e.Translation.Position,e.PositionRoot,e.PositionError,Request.PositionMetres);
        Error(e.Translation.Velocity,e.VelocityRoot,e.VelocityError,Request.VelocityMetresPerSecond);
        Error(e.Rotation.Spin,e.AngularVelocityBody,e.AngularVelocityError,Request.AngularVelocityRadiansPerSecond);
        Check(e.AttitudeChordalError>0&&e.AttitudeChordalError<=Request.AttitudeChordal,"final normalized-bit attitude qualification");
        Check(e.Rotation.ConvolutionTerms==66&&v.ArithmeticVersion==1,"fixed calculation version");
    }
    private static void Error(FloridaVector interval,Double3 bits,Double3 error,double limit)
    {
        Component(interval.X,bits.X,error.X);Component(interval.Y,bits.Y,error.Y);Component(interval.Z,bits.Z,error.Z);
        void Component(FloridaBound b,double x,double e)
        {
            var l=R.From(b.Lower)-R.From(x);var h=R.From(b.Upper)-R.From(x);var bound=R.From(e);
            Check(bound>=(l<0?-l:l)&&bound>=(h<0?-h:h)&&bound<=R.From(limit),"independent exact represented-error bound");
        }
    }
    private static void Mismatches(Inputs s,FloridaContactProvider.Proof.PrivateCanonicalState state,Inputs foreign)
    {
        Check(s.Read(default,out var empty)==PrivatePropagationStatus.Unsupported&&empty==default,"default staged receipt");
        Check(state.Read(s.Root,default,s.Realization,Pose,s.Case.End,Request,s.Case.Use,out empty)==PrivatePropagationStatus.Unsupported&&empty==default,"default initial receipt");
        Check(state.Read(s.Root,foreign.Initial,s.Realization,Pose,s.Case.End,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"numerically identical foreign initial");
        Check(state.Read(foreign.Root,s.Initial,s.Realization,Pose,s.Case.End,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"foreign root");
        Check(state.Read(s.Root,s.Initial,foreign.Realization,Pose,s.Case.End,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"foreign realization read key");
        Check(state.Read(s.Root,s.Initial,s.Realization,new(1),s.Case.End,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"changed pose key");
        Check(state.Read(s.Root,s.Initial,s.Realization,Pose,s.Case.End,Request with{PositionMetres=.003},s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"changed endpoint request");
        foreach(var target in new[]{s.Case.Start,new SimulationInstant(s.Case.End.Ticks-1),new SimulationInstant(s.Case.End.Ticks+1)})
        {
            var result=s.Root.PreparePrivatePropagation(s.Initial,s.Realization,Pose,target,Request,s.Case.Use);
            Check(result.Status==PrivatePropagationStatus.Unsupported&&result.Failure==PrivatePropagationFailure.UnsupportedTarget,"target is exact SourceEnd only");
            Check(state.Read(s.Root,s.Initial,s.Realization,Pose,target,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"read target mismatch");
        }
        Check(s.Root.PreparePrivatePropagation(default,s.Realization,Pose,s.Case.End,Request,s.Case.Use).Status==PrivatePropagationStatus.Unsupported,"default preparation source");
        Check(s.Root.PreparePrivatePropagation(foreign.Initial,s.Realization,Pose,s.Case.End,Request,s.Case.Use).Status==PrivatePropagationStatus.Unsupported,"foreign preparation source");
        foreach(var request in new[]{default(PrivatePropagationRequest),Request with{PositionMetres=double.NaN}})
            Check(s.Root.PreparePrivatePropagation(s.Initial,s.Realization,Pose,s.Case.End,request,s.Case.Use).Failure==PrivatePropagationFailure.InvalidRequest,"invalid endpoint request");
        var failed=s.Root.PreparePrivatePropagation(s.Initial,s.Realization,Pose,s.Case.End,Request with{PositionMetres=1e-30},s.Case.Use);
        Check(failed.Failure==PrivatePropagationFailure.EndpointResolution&&s.Read(failed.State,out _) == PrivatePropagationStatus.Unsupported,"no partial paired result");
        var refined=s.Root.Refine(1e-5,24,s.Case.Use).Proof;
        Check(state.Read(refined,s.Initial,s.Realization,Pose,s.Case.End,Request,s.Case.Use,out _)==PrivatePropagationStatus.Unsupported,"different issuing proof does not borrow stage");
        Check(s.Read(state,out _)==PrivatePropagationStatus.Ready,"refinement does not consume older receipt");
        Check(typeof(FloridaContactProvider.Proof.PrivateCanonicalState).GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).All(c=>c.IsPrivate),"private issuer only");
    }
    private sealed class CopiedTerrain(IPhysicalSurfacePointQuery source):IPhysicalSurfacePointQuery
    {public PhysicalSurfaceAuthorityIdentity Authority=>source.Authority;public PhysicalSurfacePointResult Query(ulong body,in Double3 direction)=>source.Query(body,direction);}
    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        var s=Source(Create(query));var stage=s.Prepare().State;var other=Create(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,new(1,-2,4),ContactFeatureRole.LandingTip)],out var geometry),"geometry control");
        foreach(var use in new[]{s.Case.Use with{Engine=other.Engine},s.Case.Use with{Geometry=geometry!},s.Case.Use with{Graph=other.Graph},
            s.Case.Use with{System=SolAnalyticalDefinition.CreateForTest()},s.Case.Use with{Terrain=new CopiedTerrain(query)}})
        {var before=Snapshot(s.Case);Stale(s,stage,use);Check(before==Snapshot(s.Case),"authority refusal nonmutation");}
        foreach(var change in new[]{"timeline","clock","state","force","torque"})
        {
            var x=Source(Create(query));var old=x.Prepare().State;
            if(change=="timeline")
            {Check(x.Case.Timeline.Schedule(x.Case.Start,new(new(71),new(1_000_001),0,SimulationEventKind.NoOpMarker)).Succeeded,"timeline schedule");x.Case.Timeline.Cancel(new(71));}
            else if(change=="clock")x.Case.Engine.AdvanceAndExecuteOneCanonicalGroup(new(x.Case.Start.Ticks+1));
            else if(change=="torque")
            {var r=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(x.Case.Engine.State,new SpacecraftTorqueCommand(Craft,new(0,1,0),x.Case.Start));Check(r.Succeeded&&x.Case.Engine.ValidateAndCommit(r.Transaction!.Value).Committed,"torque transaction");}
            else
            {var r=new SimulationEventRequest(new(91),x.Case.Start,0,SimulationEventKind.Marker);
                if(change=="force")Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91),0,new(Craft,x.Case.Start,new(2,0,0)),out r),"force intent");
                Check(x.Case.Timeline.Schedule(x.Case.Start,r).Succeeded&&x.Case.Engine.ExecuteCanonicalPendingEvent().Committed,"source transaction");}
            var before=Snapshot(x.Case);Stale(x,old,x.Case.Use);Check(before==Snapshot(x.Case),change+" refusal nonmutation");
        }
    }
    private static void Stale(Inputs s,FloridaContactProvider.Proof.PrivateCanonicalState state,FloridaContactUse use)
    {
        Check(state.Read(s.Root,s.Initial,s.Realization,Pose,s.Case.End,Request,use,out var empty)==PrivatePropagationStatus.Stale&&empty==default,"stale checked read");
        Check(s.Root.PreparePrivatePropagation(s.Initial,s.Realization,Pose,s.Case.End,Request,use).Status==PrivatePropagationStatus.Stale,"stale construction");
    }
    private static string Snapshot(Case c)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);var clock=Clock(c);
        return JsonSerializer.Serialize(new{linear,mass,angular,c.Engine.State.Revision,c.Engine.State.MarkerValue,timeline=c.Timeline.Revision,pending=c.Timeline.PendingCount,
            clock.CurrentTime,clock.PendingSimulationDebt,clock.Rate,clock.RateRemainder,clock.IsPaused,general=c.Engine.ProcessedCount,forces=c.Engine.ProcessedSpacecraftForceCount,
            torques=c.Engine.ProcessedRigidBodyTorqueCount,attitudes=c.Engine.ProcessedSpacecraftAttitudeCount,contacts=c.Engine.ProcessedContactImpulseCount,terrain=c.Query.Authority,geometry=c.Geometry.Identity});
    }
    private static long[] VectorBits(Double3 v)=>[BitConverter.DoubleToInt64Bits(v.X),BitConverter.DoubleToInt64Bits(v.Y),BitConverter.DoubleToInt64Bits(v.Z)];
    private static string ReplayKey(PrivateCanonicalStateValues v)=>JsonSerializer.Serialize(new
    {root=new{v.Initial.Root.RootEnclosure,v.Initial.Root.Function,v.Initial.Root.Derivative,v.Initial.Root.RefinementCount},v.Initial,v.Target,v.RemainderSeconds,v.Endpoint,v.Request,v.EventCoverage,v.ArithmeticVersion,
        positionBits=VectorBits(v.Endpoint.PositionRoot),velocityBits=VectorBits(v.Endpoint.VelocityRoot),angularBits=VectorBits(v.Endpoint.AngularVelocityBody),
        quaternionBits=new[]{BitConverter.DoubleToInt64Bits(v.Endpoint.OrientationBodyToRoot.X),BitConverter.DoubleToInt64Bits(v.Endpoint.OrientationBodyToRoot.Y),
            BitConverter.DoubleToInt64Bits(v.Endpoint.OrientationBodyToRoot.Z),BitConverter.DoubleToInt64Bits(v.Endpoint.OrientationBodyToRoot.W)}});
    private static void Measure(string name,Func<bool> work,bool zero=true)
    {
        for(var i=0;i<32;i++)Check(work(),name+" warmup");var times=new long[101];
        for(var i=0;i<times.Length;i++){var start=Stopwatch.GetTimestamp();var ok=work();times[i]=Stopwatch.GetTimestamp()-start;Check(ok,name+" timed outcome");}
        Array.Sort(times);var completed=0;
        using var measurement=new OrdinaryAllocationMeasurement("private propagation "+name);
        for(var i=0;i<8;i++)if(work())completed++;
        var bytes=measurement.Complete();if(zero)OrdinaryAllocationMeasurement.RequireZero(bytes,name);Check(completed==8,name+" exact completion");
        double Us(int i)=>times[i]*1e6/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="private-propagation-cost",name,warmup=32,samples=101,completed,bytes,zeroRequired=zero,
            entry="PASS",exit="PASS",medianUs=Us(50),p95Us=Us(95),p99Us=Us(99),maximumUs=Us(100)}));
    }
}
