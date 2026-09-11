using System.Reflection;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Transactions;
using static ContactGenerationFixture;
using Case = FloridaContactProductionTests.Case;
using Owner = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof.PostImpactCoverageProvider;

internal static class PostImpactCoverageProductionTests
{
    private static readonly FloridaKinematicsRequest Kinematics = new(1e-5,1e-8,1e-5,1e-12,24);
    private static readonly CertifiedResponseRequest Response = new(1e-6,1e-6,1e-6,3e-6);
    private static readonly CertifiedPreImpactVelocityRequest Preimpact = new(1e-5,1e-4);
    private static readonly PrivatePostImpactPoseRequest Pose = new(.5);
    private static readonly PrivatePropagationRequest Request = new(.002,2e-6,2e-6,4e-6);
    private static void Require(bool condition,string contract) => ContactGenerationFixture.Check(condition,"post-impact Florida coverage "+contract);
    private static SimulationClock Clock(Case c) => (SimulationClock)typeof(SimulationTransactionEngine)
        .GetField("_clock",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(c.Engine)!;

    internal static void Run()=>RunCore(false);
    internal static void Cost()=>RunCore(true);
    private static void RunCore(bool measure)
    {
        var repository=GraphicsTestHarness.RepositoryPath();
        Require(EarthElevationDataset.TryLoad(Path.Combine(repository,"assets","earth","runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(repository,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Require(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"actual Earth authority");
        var useful=0;
        // Same predeclared coast and force inputs as the accepted paired-propagation fixtures.
        foreach(var force in new[]{false,true})
        {
            var c=new Case(acquired!,.5,-1,radialAcceleration:force?-.2:0,attitude:new(0,0,.6,.8));
            Require(Clock(c).AdvanceByHostDuration(new(1_000)).Reason==SimulationHostAdvanceStopReason.Accepted,"pending debt setup");
            var before=Snapshot(c);
            Require(c.Geometry.Count==1,"complete singleton geometry");
            Require(c.Admit(out var preProvider,out var admission),"M14.9 admission: "+admission);
            var root=c.Evaluate(preProvider!).Proof;
            Require(root.IsRoot,"M14.9 approaching alpha");
            var k=root.QualifyKinematics(Kinematics,c.Use);
            Require(k.Status==FloridaKinematicsStatus.Qualified,"M14.10 kinematics");
            var response=root.QualifyResponse(k.Witness,Response,c.Use);
            Require(response.Status==CertifiedResponseStatus.Qualified,"M14.11 response proposal");
            var pre=root.QualifyPreImpactVelocity(k.Witness,Preimpact,c.Use);
            Require(pre.Status==CertifiedPreImpactVelocityStatus.Qualified,"M14.12 pre-impact tuple");
            var final=root.QualifyPostImpactVelocity(k.Witness,response.Proposal,Response,pre.Tuple,Preimpact,c.Use);
            Require(final.Status==CertifiedPostImpactVelocityStatus.Qualified,"M14.13 final velocities");
            var initial=root.PreparePrivatePostImpactState(k.Witness,response.Proposal,Response,pre.Tuple,Preimpact,final.Realization,Pose,c.Use);
            Require(initial.Status==PrivatePostImpactStateStatus.Ready,"M14.14 private initial state");
            Require(initial.State.Read(root,final.Realization,Pose,c.Use,out var source)==PrivatePostImpactStateStatus.Ready,"checked initial identity");
            Require(source.InitialAngularVelocityBody!=Double3.Zero,"nonzero asymmetric post-impact spin");
            var staged=root.PreparePrivatePropagation(initial.State,final.Realization,Pose,c.End,Request,c.Use);
            Require(staged.Status==PrivatePropagationStatus.Ready,"M14.15 paired continuation: "+staged.Failure);
            Require(staged.State.Read(root,initial.State,final.Realization,Pose,c.End,Request,c.Use,out var continuation)==PrivatePropagationStatus.Ready,
                "checked M14.15 continuation");
            Require(continuation.Initial==source && continuation.Target==c.End,"same alpha/initial state/target");
            Require(continuation.EventCoverage==PrivatePropagationCoverage.Unknown,"M14.15 remains endpoint-only");
            Require(Owner.TryCreate(staged.State,root,initial.State,final.Realization,Pose,c.End,Request,c.Use,out var owner)==PrivatePropagationStatus.Ready,
                "coverage admission from exact checked continuation");
            var status=owner!.Evaluate(staged.State,c.Use,out var receipt,out var proof);
            Require(status==proof.Status,"coverage outcome identity");
            Require(proof.Visits<=255 && proof.MaximumDepth<=24,"unchanged search limits");
            Require(status==(force?CoverageStatus.Unresolved:CoverageStatus.EventFreeThroughTarget),
                "exact production outcome: "+status);
            Require(proof.Failure==(force?CoverageFailure.DepartureUnproved:CoverageFailure.None),
                "exact production reason: "+proof.Failure);
            Require(proof.Visits==(force?25:63) && proof.MaximumDepth==(force?24:5) && proof.Evaluations==(force?25:63),
                "unchanged Coast/Force bounded work");
            Require(owner.Evaluate(staged.State,c.Use,out _,out var replay)==status && replay==proof,"complete deterministic replay");
            if(status == CoverageStatus.EventFreeThroughTarget)
            {
                useful++;
                Require(receipt.Read(staged.State,c.Use,out var values)==status,"checked coverage receipt");
                Require(values.Proof==proof && values.Terrain==c.Query.Authority && values.Target==c.End,"scope/proof/terrain/target identity");
                var expectedFeature=new ContactFeatureIdentity(c.Geometry.Spacecraft,c.Geometry.Identity,c.Geometry.GetFeature(0).Id);
                Require(values.Feature==expectedFeature,"complete admitted authored point identity");
                Require(receipt.Read(default,c.Use,out _)==CoverageStatus.Unsupported,"receipt rejects incompatible staged state");
                Require(default(Owner.CoverageEvidence).Read(staged.State,c.Use,out _)==CoverageStatus.Unsupported,
                    "default value cannot forge clearance");
            }
            else Require(status==CoverageStatus.Unresolved && receipt.Read(staged.State,c.Use,out _)==CoverageStatus.Unsupported,
                "refusal issues no coverage capability");

            // Fixed boundary diagnostics accompany the coverage outcome, not an adaptive search for
            // permissive tolerances. The initial leaf is the declared depth-24 departure boundary.
            var leafStatus=owner.Enclose(staged.State,c.Use,new(0,Math.ScaleB(1,-24)),out var leaf,out var gap,out var failure);
            Console.WriteLine(JsonSerializer.Serialize(new{kind="postimpact-Florida-coverage",force,status=status.ToString(),failure=proof.Failure.ToString(),
                proof.Visits,proof.MaximumDepth,proof.Evaluations,proof.ClearPrefix,rootAlpha=source.PoseRootEnclosure,
                source.InitialAngularVelocityBody,continuation.RemainderSeconds,geometry=c.Geometry.Identity,terrain=c.Query.Authority,
                target=c.End.Ticks,leafStatus=leafStatus.ToString(),leafFailure=failure.ToString(),leaf.Elapsed,gap,
                sourceAndContinuation="CHECKED_SAME",canonicalMutation="NONE"}));
            Require(owner.Evaluate(default,c.Use,out _,out _)==CoverageStatus.Unsupported,"default continuation refused");
            var foreign=new Case(acquired!,.5,-1,radialAcceleration:force?-.2:0,attitude:new(0,0,.6,.8));
            Require(owner.Evaluate(staged.State,foreign.Use,out _,out _)==CoverageStatus.Stale,"foreign authority refused");
            if(!force)Require(receipt.Read(staged.State,foreign.Use,out _)==CoverageStatus.Stale,"checked receipt rejects foreign authority");
            Require(!c.Admit(out _,out _,geometry:Geometry(2)),"multi-feature input cannot select a singleton subset");
            Require(staged.State.Read(root,initial.State,final.Realization,Pose,c.End,Request,c.Use,out var after)==PrivatePropagationStatus.Ready && after==continuation,
                "unchanged M14.15 paired state");
            Require(before==Snapshot(c),"complete canonical state, history counts, clock and timeline unchanged");
            if(measure && !force)
            {
                PostImpactCoverageCost.Measure("coverage owner admission",()=>Owner.TryCreate(staged.State,root,initial.State,final.Realization,Pose,c.End,Request,c.Use,out _)==PrivatePropagationStatus.Ready,false);
                PostImpactCoverageCost.Measure("reusable interval",()=>owner.Enclose(staged.State,c.Use,new(0,1d/32),out _,out _,out _)==PrivatePropagationStatus.Ready);
                PostImpactCoverageCost.Measure("real Coast clear certification",()=>owner.Evaluate(staged.State,c.Use,out _,out _)==CoverageStatus.EventFreeThroughTarget);
                PostImpactCoverageCost.Measure("checked coverage read",()=>receipt.Read(staged.State,c.Use,out _)==CoverageStatus.EventFreeThroughTarget);
                PostImpactCoverageCost.Measure("foreign authority refusal",()=>owner.Evaluate(staged.State,foreign.Use,out _,out _)==CoverageStatus.Stale);
                PostImpactCoverageCost.Measure("invalid numerical interval refusal",()=>owner.Enclose(staged.State,c.Use,new(double.NaN,double.NaN),out _,out _,out _)==PrivatePropagationStatus.Unresolved);
                OrdinaryAllocationMeasurement.PositiveControl();
                Require(before==Snapshot(c),"cost qualification preserves canonical state");
                Console.WriteLine(JsonSerializer.Serialize(new{kind="coverage-workspace",setupTrajectoryEvaluations=2,
                    searchTrajectoryEvaluations=proof.Evaluations,rotationEvaluations=proof.Evaluations,
                    earthBodyFixedEvaluations=proof.Evaluations,terrainDomainEvaluations=proof.Evaluations,
                    terrainPointQueriesDuringCoverage=0,
                    searchStackBytes=PostImpactCoverageSearch.SearchStackBytes,
                    receiptBytes=System.Runtime.CompilerServices.Unsafe.SizeOf<Owner.CoverageEvidence>(),
                    proofBytes=System.Runtime.CompilerServices.Unsafe.SizeOf<CoverageSearchResult>(),
                    valuesBytes=System.Runtime.CompilerServices.Unsafe.SizeOf<CoverageValues>()}));
            }
            if(measure && force)
                PostImpactCoverageCost.Measure("real Force departure refusal",()=>owner.Evaluate(staged.State,c.Use,out _,out var refused)==CoverageStatus.Unresolved && refused.Failure==CoverageFailure.DepartureUnproved);
        }
        Require(useful>0,"NO USEFUL SUPPORTED COVERAGE: both representative Florida cases unresolved; stop for Project Control");
        Console.WriteLine("POSTIMPACT_FLORIDA_COVERAGE checked-chain/singleton/terrain/nonzero-spin/nonmutation PASS");
    }

    private static string Snapshot(Case c)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);
        c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);var clock=Clock(c);
        return JsonSerializer.Serialize(new{linear,mass,angular,c.Engine.State.Revision,c.Engine.State.MarkerValue,
            timeline=c.Timeline.Revision,pending=c.Timeline.PendingCount,clock.CurrentTime,clock.PendingSimulationDebt,clock.Rate,
            clock.RateRemainder,clock.IsPaused,general=c.Engine.ProcessedCount,forces=c.Engine.ProcessedSpacecraftForceCount,
            torques=c.Engine.ProcessedRigidBodyTorqueCount,attitudes=c.Engine.ProcessedSpacecraftAttitudeCount,
            contacts=c.Engine.ProcessedContactImpulseCount,terrain=c.Query.Authority,geometry=c.Geometry.Identity});
    }
}
