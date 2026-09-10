using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Time;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static ContactGenerationFixture;
using static ContactKinematicsOracle;
using Case=FloridaContactProductionTests.Case;

internal static class QualifiedFloridaKinematicsTests
{
    private static void Check(bool value,string message)=>ContactGenerationFixture.Check(value,message);
    private static readonly FloridaKinematicsRequest Coarse=new(1,.01,.5,1e-12,24);
    private static readonly FloridaKinematicsRequest Fine=new(1e-5,1e-8,1e-5,1e-12,24);
    internal static void Run()
    {
        var repository=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(repository,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"root witness acquired terrain");
        var query=acquired!;
        var c=new Case(query,.5,-1,radialAcceleration:-.2,attitude:new(0,0,.6,.8));
        Check(c.Admit(out var provider,out _),"kinematics admission");var proof=c.Evaluate(provider!).Proof;
        Check(proof.IsRoot,"current production root");
        var revision=c.Engine.State.Revision;var timeline=c.Timeline.Revision;var initialEnclosure=proof.RootEnclosure;
        var coarse=proof.QualifyKinematics(Coarse,c.Use);var fine=proof.QualifyKinematics(Fine,c.Use);
        var coarseValues=Read(coarse,proof,c);var fineValues=Read(fine,proof,c);
        Check(fineValues.RootEnclosure.Lower>=coarseValues.RootEnclosure.Lower&&fineValues.RootEnclosure.Upper<=coarseValues.RootEnclosure.Upper&&
            FloridaKinematicsRequest.Width(fineValues.RootEnclosure)<FloridaKinematicsRequest.Width(coarseValues.RootEnclosure),"root enclosure improves");
        Check(FloridaKinematicsRequest.Width(fineValues.NormalVelocityMetresPerSecond)<FloridaKinematicsRequest.Width(coarseValues.NormalVelocityMetresPerSecond),"speed qualification improves");
        Check(FloridaKinematicsRequest.Fits(fineValues.NormalRoot,Fine.NormalComponent)&&
            FloridaKinematicsRequest.Width(fineValues.NormalVelocityMetresPerSecond)<=Fine.NormalVelocityMetresPerSecond&&
            FloridaKinematicsRequest.Fits(fineValues.LeverRootMetres,Fine.LeverComponentMetres),"all explicit component widths");
        Check(fineValues.MassKilograms==8&&fineValues.PrincipalInertia==new NovaCore.Simulation.Spacecraft.Rotation.PrincipalMomentsOfInertia(2,3,4)&&
            fineValues.AuthoredLeverBodyMetres==new Double3(1,-2,3)&&fineValues.FixedAttitude!=DoubleQuaternion.Identity,"immutable physical snapshots");
        Check(fineValues.OriginalRoot.Equals(proof)&&fineValues.TerrainAuthority==query.Authority&&fineValues.ProviderVersion==1&&fineValues.RelationVersion==1,"root/provenance retained");
        var refined=proof.Refine(1e-5,24,c.Use).Proof;
        Check(fine.Witness.Read(refined,c.Use,out _)==FloridaKinematicsStatus.Qualified,"same-owner refined lineage accepted");
        IndependentEarth(c,fineValues);
        Print("coarse",coarseValues);Print("fine",fineValues);
        var budget=proof.QualifyKinematics(Fine with{RefinementBudget=0},c.Use);
        Check(budget.Status==FloridaKinematicsStatus.Unresolved&&budget.Failure==FloridaKinematicsFailure.RefinementBudget&&
            budget.Witness.Read(proof,c.Use,out _)==FloridaKinematicsStatus.Unsupported,"budget refusal carries no witness");
        Check(proof.QualifyKinematics(Fine with{LeverComponentMetres=1e-30},c.Use).Status==FloridaKinematicsStatus.Unresolved,"fixed lever arithmetic floor");
        Check(proof.QualifyKinematics(default,c.Use).Status==FloridaKinematicsStatus.Unsupported,"invalid request");
        Check(proof.QualifyKinematics(Fine,default).Status==FloridaKinematicsStatus.Stale,"missing current context");
        var late=new Case(query,.5,-1,start:3599);Check(late.Admit(out var lateProvider,out _),"late admission");var lateRoot=late.Evaluate(lateProvider!).Proof;
        Check(lateRoot.IsRoot,"late coarse existence retained");var floor=lateRoot.QualifyKinematics(Fine,late.Use);
        Check(floor.Status==FloridaKinematicsStatus.Unresolved&&floor.Failure==FloridaKinematicsFailure.NumericalResolution,"coverage-floor explicit refusal");
        Console.WriteLine($"KINEMATICS_FLOOR status={floor.Status} failure={floor.Failure} coverageRemainder={lateProvider!.CoveragePositionRemainder:R}");
        foreach(var noRoot in new[]{new Case(query,10,0),new Case(query,0,0)})
        {Check(noRoot.Admit(out var p,out _),"clear/nonroot admission");Check(noRoot.Evaluate(p!).Proof.QualifyKinematics(Fine,noRoot.Use).Status==FloridaKinematicsStatus.Unsupported,"clear/unresolved proof rejected");}
        Refusals(query);
        var replay=new Case(query,.5,-1,radialAcceleration:-.2,attitude:new(0,0,.6,.8));Check(replay.Admit(out var replayProvider,out _),"replay admission");
        var replayProof=replay.Evaluate(replayProvider!).Proof;var replayValues=Read(replayProof.QualifyKinematics(Fine,replay.Use),replayProof,replay);
        Check(fineValues with{OriginalRoot=default}==replayValues with{OriginalRoot=default},"deterministic independent replay fields");
        Check(fine.Witness.Read(replayProof,replay.Use,out _)==FloridaKinematicsStatus.Unsupported,"equal-time replay owner cannot borrow witness");
        uint seed=77;
        for(var i=0;i<16;i++)
        {
            seed=unchecked(seed*1664525+1013904223);
            _=proof.QualifyKinematics((seed&1)==0?Coarse:Fine with{RefinementBudget=0},c.Use);
            Check(Read(proof.QualifyKinematics(Fine,c.Use),proof,c)==fineValues,"reordered repeated requests deterministic");
        }
        Check(c.Engine.State.Revision==revision&&c.Timeline.Revision==timeline&&c.Timeline.PendingCount==0&&proof.RootEnclosure==initialEnclosure,"no state/root mutation");
        Measure("setup/admission/root",()=>c.Admit(out var p,out _)&&c.Evaluate(p!).Proof.IsRoot,false);
        Measure("qualified evaluation",()=>refined.QualifyKinematics(Fine with{RefinementBudget=0},c.Use).Status==FloridaKinematicsStatus.Qualified);
        Measure("qualified refinement",()=>proof.QualifyKinematics(Fine,c.Use).Status==FloridaKinematicsStatus.Qualified);
        Measure("checked witness consumption",()=>fine.Witness.Read(proof,c.Use,out _)==FloridaKinematicsStatus.Qualified);
        Measure("uncertainty-floor refusal",()=>lateRoot.QualifyKinematics(Fine,late.Use).Status==FloridaKinematicsStatus.Unresolved);
        Measure("stale refusal",()=>proof.QualifyKinematics(Fine,replay.Use).Status==FloridaKinematicsStatus.Stale);
        Measure("unsupported refusal",()=>default(FloridaContactProvider.Proof).QualifyKinematics(Fine,c.Use).Status==FloridaKinematicsStatus.Unsupported);
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine(JsonSerializer.Serialize(new{kind="kinematics-workspace",witnessBytes=Unsafe.SizeOf<FloridaContactProvider.Proof.Kinematics>(),
            snapshotBytes=Unsafe.SizeOf<FloridaContactKinematics>(),requestBytes=Unsafe.SizeOf<FloridaKinematicsRequest>(),additionalHeapWorkspaceBytes=0,
            maximumValueEvaluations=25,maximumSignEvaluations=24,coarseWork=coarseValues.AdditionalRefinements,fineWork=fineValues.AdditionalRefinements}));
        Console.WriteLine("QUALIFIED_FLORIDA_KINEMATICS independent/root/coherence/force/offset/stale/replay/floor/allocation PASS");
    }
    private static FloridaContactKinematics Read(FloridaKinematicsResult result,FloridaContactProvider.Proof proof,Case c)
    {
        Check(result.Status==FloridaKinematicsStatus.Qualified,$"witness qualification {result.Status}/{result.Failure}");
        Check(result.Witness.Read(proof,c.Use,out var values)==FloridaKinematicsStatus.Qualified,"current checked consumption");return values;
    }
    private static void Print(string name,FloridaContactKinematics v)=>Console.WriteLine(JsonSerializer.Serialize(new{kind="kinematics-values",name,
        root=new[]{v.RootEnclosure.Lower,v.RootEnclosure.Upper},normal=new[]{v.NormalRoot.X.Lower,v.NormalRoot.X.Upper,v.NormalRoot.Y.Lower,v.NormalRoot.Y.Upper,v.NormalRoot.Z.Lower,v.NormalRoot.Z.Upper},
        speed=new[]{v.NormalVelocityMetresPerSecond.Lower,v.NormalVelocityMetresPerSecond.Upper},
        lever=new[]{v.LeverRootMetres.X.Lower,v.LeverRootMetres.X.Upper,v.LeverRootMetres.Y.Lower,v.LeverRootMetres.Y.Upper,v.LeverRootMetres.Z.Lower,v.LeverRootMetres.Z.Upper},v.AdditionalRefinements}));

    private static void IndependentEarth(Case c,FloridaContactKinematics values)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);
        // Independent seed-cell decimal cubic oracle. Source guards imply r>=1e11, |v|<=5e4, mu<=2e20.
        // |Earth''''| <= 4mu²/r^5+24mu*v²/r^4 <=1.36e-13 m/s^4;
        // position remainder <=5.67e-15m, velocity <=2.27e-14m/s at |t|<=1.
        // 20,000 decimal operations * 5e-28 relative rounding * 1e12 physical position scale
        // is 1e-11m; allow a factor10 conditioning envelope for projections/conversions (1e-10).
        // Model guard checks below bind this conservative test-oracle budget, not production acceptance.
        var system=c.System;system.TryGetNode(SolarSystemBodyIds.Earth,out var node);system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex,out var earth);
        system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun,out var sun);
        Check(V.From(earth.StateAtEpoch.Position).Norm>1.1e11m&&V.From(earth.StateAtEpoch.Position).Norm<2e11m&&V.From(earth.StateAtEpoch.Velocity).Norm<4e4m&&
            sun.GravitationalParameter<2e20&&V.From(linear.PositionRoot).Norm<2e11m,"independent oracle numerical budget domain");
        const decimal error=1e-10m;decimal lo=0,hi=1;
        for(var i=0;i<26;i++)
        {
            var mid=(lo+hi)/2;var sample=EarthPoint(linear,angular,mass.MassKilograms,values.AuthoredLeverBodyMetres,mid);
            if(sample.Gap>error)lo=mid;else if(sample.Gap< -error)hi=mid;else break;
        }
        Check(EarthPoint(linear,angular,mass.MassKilograms,values.AuthoredLeverBodyMetres,lo).Gap>error&&
            EarthPoint(linear,angular,mass.MassKilograms,values.AuthoredLeverBodyMetres,hi).Gap< -error,"independent robust sign bracket");
        Check(D(values.RootEnclosure.Lower)<=lo&&D(values.RootEnclosure.Upper)>=hi,"same real root independently isolated inside witness");
        var center=EarthPoint(linear,angular,mass.MassKilograms,values.AuthoredLeverBodyMetres,(lo+hi)/2);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        var rates=(Math.Abs(D(model.RaT))+Math.Abs(D(model.DecT)))/(D(model.SecondsPerDay)*D(model.DaysPerCentury))+
            Math.Abs(D(model.Wd))/D(model.SecondsPerDay);
        Check(rates*Pi/180<1e-4m&&V.From(linear.ConstantForceRoot).Norm/D(mass.MassKilograms)<1&&
            (V.From(linear.PositionRoot)-V.From(earth.StateAtEpoch.Position)).Norm+10000<1e7m&&
            (V.From(linear.VelocityRoot)-V.From(earth.StateAtEpoch.Velocity)).Norm+10<1000,"independent root-neighborhood derivative guards");
        var orientation=Orientation(model,(lo+hi)/2,FloridaFacilitySupport.Region.Up);var nPrime=V.Cross(orientation.Omega,center.Normal);
        var h=(hi-lo)/2;
        // Entire independent alpha bracket, not sampled agreement: |n''|<=2 Omega²,
        // |u_n'|<= A+2 Omega V+2 Omega² P <=10.4 with the guarded A=10,V=1000,P=1e7.
        var normalRemainder=1e-8m*h*h+1e-22m;
        Check(Contains(values.NormalRoot.X,center.Normal.X,Math.Abs(nPrime.X)*h+normalRemainder)&&
            Contains(values.NormalRoot.Y,center.Normal.Y,Math.Abs(nPrime.Y)*h+normalRemainder)&&
            Contains(values.NormalRoot.Z,center.Normal.Z,Math.Abs(nPrime.Z)*h+normalRemainder),"independent entire-root normal containment");
        Check(Contains(values.LeverRootMetres,center.Lever),"independent fixed lever");
        Check(Contains(values.NormalVelocityMetresPerSecond,center.Speed,10.4m*h+error),"independent entire-root material velocity containment");
        Console.WriteLine(JsonSerializer.Serialize(new{kind="kinematics-independent-oracle",rootLower=lo,rootUpper=hi,normalSpeed=center.Speed,gapErrorMetres=error,speedErrorMetresPerSecond=error,
            speedRootBracketErrorMetresPerSecond=10.4m*h+error,snapBound=1.36e-13,source="exact stored seed bits; decimal ODE derivatives, rotation matrix and material velocity"}));
    }

    private sealed class CopiedTerrain(IPhysicalSurfacePointQuery source):IPhysicalSurfacePointQuery
    {public PhysicalSurfaceAuthorityIdentity Authority=>source.Authority;public PhysicalSurfacePointResult Query(ulong b,in Double3 d)=>source.Query(b,d);}
    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        var c=new Case(query,.5,-1);Check(c.Admit(out var p,out _),"refusal setup");var root=c.Evaluate(p!).Proof;var witness=root.QualifyKinematics(Fine,c.Use).Witness;
        var other=new Case(query,.75,-1);Check(other.Admit(out var op,out _),"other root");
        Check(witness.Read(other.Evaluate(op!).Proof,other.Use,out _)==FloridaKinematicsStatus.Unsupported,"mixed root refused");
        Check(c.Admit(out var sameOwnerInputs,out _),"second equal relation owner");
        Check(witness.Read(c.Evaluate(sameOwnerInputs!).Proof,c.Use,out _)==FloridaKinematicsStatus.Unsupported,"same relation independent owner refused");
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,new Double3(1,-2,4),ContactFeatureRole.LandingTip)],out var changedGeometry),"geometry fixture");
        foreach(var changed in new[]{c.Use with{Engine=other.Engine},c.Use with{Geometry=changedGeometry!},c.Use with{Graph=other.Graph},
            c.Use with{System=SolAnalyticalDefinition.CreateForTest()},c.Use with{Terrain=new CopiedTerrain(query)}})
        {Check(witness.Read(root,changed,out _)==FloridaKinematicsStatus.Stale&&root.QualifyKinematics(Fine,changed).Status==FloridaKinematicsStatus.Stale,"changed authority rejected on issuance and consumption");}
        c.Timeline.Schedule(c.Start,new(new(71),new(1000001),0,SimulationEventKind.NoOpMarker));c.Timeline.Cancel(new(71));
        Check(witness.Read(root,c.Use,out _)==FloridaKinematicsStatus.Stale,"timeline-only cancellation invalidates");
        foreach(var change in new[]{"force","torque","state"})
        {
            var a=new Case(query,.5,-1);Check(a.Admit(out var ap,out _),"mutation test setup");var ar=a.Evaluate(ap!).Proof;var aw=ar.QualifyKinematics(Fine,a.Use).Witness;
            var oldTimeline=a.Timeline.Revision;
            if(change=="torque")
            {
                var replacement=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(a.Engine.State,new SpacecraftTorqueCommand(Craft,new Double3(0,1,0),a.Start));
                Check(replacement.Succeeded&&a.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed,"independent torque commit");
                Check(a.Timeline.Revision==oldTimeline,"direct torque state-only invalidation");
            }
            else
            {
                var request=new SimulationEventRequest(new(91),a.Start,0,SimulationEventKind.Marker);
                if(change=="force")Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91),0,new(Craft,a.Start,new Double3(2,0,0)),out request),"force intent");
                Check(a.Timeline.Schedule(a.Start,request).Succeeded&&a.Engine.ExecuteCanonicalPendingEvent().Committed,"state mutation");
            }
            Check(ar.QualifyKinematics(Fine,a.Use).Status==FloridaKinematicsStatus.Stale&&aw.Read(ar,a.Use,out _)==FloridaKinematicsStatus.Stale,"stale state/force/torque issuance and consumption");
        }
    }
    private static void Measure(string name,Func<bool> work,bool zero=true)
    {
        // Fixed bounded plan: 32 warmups, 101 ordinary-runtime samples, 8 separate allocation calls.
        for(var i=0;i<32;i++)Check(work(),name+" warmup");
        var ticks=new long[101];
        for(var i=0;i<101;i++){var start=Stopwatch.GetTimestamp();var ok=work();ticks[i]=Stopwatch.GetTimestamp()-start;Check(ok,name+" timing");}
        Array.Sort(ticks);var completed=0;
        using var measurement=new OrdinaryAllocationMeasurement("root kinematics "+name);
        for(var i=0;i<8;i++)if(work())completed++;
        var bytes=measurement.Complete();if(zero)OrdinaryAllocationMeasurement.RequireZero(bytes,name);Check(completed==8,name+" completed");
        double Ns(int i)=>ticks[i]*1e9/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="kinematics-cost",name,bytes,calls=8,medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99),maximumNs=Ns(100)}));
    }
}
