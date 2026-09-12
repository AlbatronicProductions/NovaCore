using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using System.Diagnostics;
using System.Text.Json;
using static ContactGenerationFixture;

internal static class FloridaContactProductionTests
{
    private static void Check(bool value,string message)=>ContactGenerationFixture.Check(value,message);
    internal static void Run()
    {
        var root=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"acquired grading authority");
        var query=acquired!;var system=SolAnalyticalDefinition.Instance;var graph=Graph();
        var evaluations=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[system.Count];var staging=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[system.Count];
        var roots=new NovaCore.Core.ReferenceFrames.FrameTransform[system.Count];var stagingRoots=new NovaCore.Core.ReferenceFrames.FrameTransform[system.Count];
        foreach(var start in new[]{-3600L,0L,3599L})
        foreach(var crossing in new[]{false,true})
        {
            var time=SimulationInstant.FromWholeSeconds(start);
            Check(EarthPhysicalEventEvaluator.TryEvaluate(system,graph,PhysicalEventEpoch.FromCanonical(time),evaluations,roots,staging,stagingRoots,out var earth)==EarthPhysicalEventStatus.Ready,"current Earth");
            var site=FloridaFacilitySupport.Region;var center=query.Query(6,site.Up);Check(center.IsReady,"current graded terrain");
            var offset=new Double3(1,-2,3);var q=DoubleQuaternion.Identity;
            var feature=earth.BodyFixedToRoot.LocalToParent(center.BodyFixedPositionMetres+site.Up*(crossing?.5:10));
            var surfaceV=earth.VelocityRoot+Double3.Cross(earth.AngularVelocityRoot,earth.BodyFixedToRoot.LocalDirectionToParent(center.BodyFixedPositionMetres));
            var velocity=surfaceV-earth.BodyFixedToRoot.LocalDirectionToParent(site.Up)*(crossing?1:0);
            var state=State(feature-q.Rotate(offset),velocity,q,Double3.Zero,time);var view=state.CreateView();
            Check(view.Spacecraft.TryGetTranslation(Craft,out var linear,out var physical)&&view.Spacecraft.TryGetRigidBody(Craft,out _),"segments");
            view.Spacecraft.TryGetRigidBody(Craft,out var angular);
            Check(FloridaContactMotion.TryCreate(system,linear,angular,physical,offset,out var motion),"source-seed enclosure");
            var domain=motion.ProveDomain(new(start,start+1),site,out var gap,out var slope);
            motion.ProveDomain(new(start,start),site,out var initial,out _);motion.ProveDomain(new(start+1,start+1),site,out var final,out _);
            Console.WriteLine($"FLORIDA_PROOF start={start} crossing={crossing} domain={domain} gap={gap} slope={slope} initial={initial} final={final} coverageRemainder={motion.PositionRemainderAtCoverage:R}");
            Check(domain,"whole interval grading containment");
            Check(crossing?initial.Lower>0&&final.Upper<0&&slope.Upper<0:gap.Lower>0,"nonvacuous current-Earth proof");
        }
        var clear=new Case(query,10,0);var crossingCase=new Case(query,.5,-1);var late=new Case(query,.5,-1,start:3599);
        foreach(var c in new[]{clear,crossingCase,late})
        {
            Check(c.Admit(out var provider,out var failure),$"provider admission {failure}");
            var result=c.Evaluate(provider!);var expected=ReferenceEquals(c,clear)?FloridaContactStatus.CertifiedClear:FloridaContactStatus.CertifiedUniqueApproachingRoot;
            Check(result.Status==expected,"actual acquired provider result");
            Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-certificate",start=c.Start.Ticks,status=result.Status.ToString(),result.Proof.RootEnclosure,
                result.Proof.Function,result.Proof.Derivative,result.Proof.RefinementCount,provider!.StartNumericalError,provider.EndNumericalError,
                initialObserved=provider.StartObservation.RadialSignedGapMetres,endObserved=provider.EndObservation.RadialSignedGapMetres}));
            if(result.Proof.IsRoot)
            {
                // Requested witness width, never a physical contact tolerance.
                var refined=result.Proof.Refine(1e-5,FloridaContactProvider.MaximumRefinements,c.Use);
                Check(refined.Status==(ReferenceEquals(c,late)?FloridaContactStatus.Unresolved:FloridaContactStatus.CertifiedUniqueApproachingRoot),"refinement reports resolution explicitly");
                Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-refinement",start=c.Start.Ticks,status=refined.Status.ToString(),failure=refined.Failure.ToString(),refined.Proof.RootEnclosure,refined.Proof.RefinementCount}));
                if(refined.Proof.IsRoot)EnclosureObservations(c,refined.Proof);
            }
        }
        Refusals(query);Ordering(query);Measurements(clear,crossingCase,new Case(query,0,0));
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("FLORIDA_PRODUCTION clear/crossing/domain/refusals/stale/replay/ordering/allocation PASS");
    }

    internal sealed class Case
    {
        internal readonly PlanetaryPhysicalSurfacePointQuery Query;
        internal readonly CelestialSystemDefinition System=SolAnalyticalDefinition.Instance;
        internal readonly NovaCore.Core.ReferenceFrames.ReferenceFrameGraph Graph=ContactGenerationFixture.Graph();
        internal readonly NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[] Evals,Staging;
        internal readonly NovaCore.Core.ReferenceFrames.FrameTransform[] Roots,StagingRoots;
        internal readonly SimulationState State;
        internal readonly SimulationTimeline Timeline;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SimulationClock Clock;
        internal readonly SpacecraftContactGeometry Geometry;
        internal readonly SimulationInstant Start,End;
        internal FloridaContactUse Use=>new(Engine,Geometry,System,Graph,Query);
        internal Case(PlanetaryPhysicalSurfacePointQuery query,double gap,double speed,long start=0,double east=0,double eastSpeed=0,
            double eastAcceleration=0,double radialAcceleration=0,Double3? omega=null,Double3? torque=null,DoubleQuaternion? attitude=null,
            StateRevision initialRevision=default,int publicationCapacity=0,int contactCapacity=0)
        {
            Timeline=new(8,contactCapacity);
            Query=query;Start=SimulationInstant.FromWholeSeconds(start);End=SimulationInstant.FromWholeSeconds(start+1);
            Evals=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[System.Count];Staging=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[System.Count];
            Roots=new NovaCore.Core.ReferenceFrames.FrameTransform[System.Count];StagingRoots=new NovaCore.Core.ReferenceFrames.FrameTransform[System.Count];
            Check(EarthPhysicalEventEvaluator.TryEvaluate(System,Graph,PhysicalEventEpoch.FromCanonical(Start),Evals,Roots,Staging,StagingRoots,out var earth)==EarthPhysicalEventStatus.Ready,"case Earth");
            var site=FloridaFacilitySupport.Region;var direction=(site.Up+site.East*(east/site.RadiusMetres)).Normalized();
            var sample=query.Query(6,direction);Check(sample.IsReady,"case terrain");
            var offset=new Double3(1,-2,3);var q=attitude??DoubleQuaternion.Identity;
            var position=earth.BodyFixedToRoot.LocalToParent(sample.BodyFixedPositionMetres+direction*gap)-q.Rotate(offset);
            var velocity=earth.VelocityRoot+Double3.Cross(earth.AngularVelocityRoot,earth.BodyFixedToRoot.LocalDirectionToParent(sample.BodyFixedPositionMetres))+
                earth.BodyFixedToRoot.LocalDirectionToParent(direction*speed+site.East*eastSpeed);
            var force=earth.BodyFixedToRoot.LocalDirectionToParent(direction*radialAcceleration+site.East*eastAcceleration)*8;
            var rotation=new SpacecraftRigidBodyRotationState(Craft,Start,q,omega??Double3.Zero,new(2,3,4),torque??Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1);
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft,new(1),new(72),"Single authored landing point")],[rotation],[new(8)],
                [new(Craft,new(1),Start,position,velocity,force)],Graph,out var store,out _),"case state");
            State=new(spacecraft:store,initialRevision:initialRevision);Clock=new SimulationClock(Start,Timeline);
            Engine=new(Clock,State,8,continuationHistoryCapacity:publicationCapacity);
            Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,offset,ContactFeatureRole.LandingTip)],out var geometry),"case geometry");Geometry=geometry!;
        }
        internal bool Admit(out FloridaContactProvider? provider,out FloridaContactFailure failure,SpacecraftContactGeometry? geometry=null,
            IPhysicalSurfacePointQuery? query=null,SimulationInstant? end=null,StateRevision? revision=null,CelestialSystemDefinition? system=null,
            PhysicalSurfaceAuthorityIdentity? authority=null)
        {
            FloridaContactProvider.TryCreate(Engine,revision??Engine.State.Revision,Start,end??End,geometry??Geometry,system??System,Graph,query??Query,authority??Query.Authority,
                Evals,Roots,Staging,StagingRoots,out provider,out failure);return provider is not null;
        }
        internal FloridaContactResult Evaluate(FloridaContactProvider provider)=>provider.Evaluate(Engine,Geometry,System,Graph,Query);

        // Test-only source reconstruction. No selected event time or recorded endpoint is installed.
        internal Case(PlanetaryPhysicalSurfacePointQuery query,SimulationState state,SimulationClock clock,
            SimulationTransactionEngine engine,NovaCore.Core.ReferenceFrames.ReferenceFrameGraph graph,
            SpacecraftContactGeometry geometry,SimulationInstant start,SimulationInstant end)
        {
            Query=query;State=state;Clock=clock;Engine=engine;Timeline=clock.Timeline;Graph=graph;Geometry=geometry;Start=start;End=end;
            Evals=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[System.Count];Staging=new NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation[System.Count];
            Roots=new NovaCore.Core.ReferenceFrames.FrameTransform[System.Count];StagingRoots=new NovaCore.Core.ReferenceFrames.FrameTransform[System.Count];
        }
    }
    private sealed class ForgedQuery(IPhysicalSurfacePointQuery real):IPhysicalSurfacePointQuery
    { public PhysicalSurfaceAuthorityIdentity Authority=>real.Authority;public PhysicalSurfacePointResult Query(ulong body,in Double3 d)=>real.Query(body,d); }

    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        foreach(var c in new[]{new Case(query,.5,-1,omega:new(0,1e-20,0)),new Case(query,.5,-1,torque:new(0,1e-20,0))})
            Check(!c.Admit(out _,out var fail)&&fail==FloridaContactFailure.MotionDomain,"exact angular refusal");
        var normal=new Case(query,.5,-1);
        Check(!normal.Admit(out _,out var forged,query:new ForgedQuery(query))&&forged==FloridaContactFailure.UnsupportedAuthority,"copied provenance is not capability");
        Check(!normal.Admit(out _,out var stale,revision:new StateRevision(99))&&stale==FloridaContactFailure.ChangedAuthority,"stale supplied revision");
        Check(!normal.Admit(out _,out var cell,end:SimulationInstant.FromWholeSeconds(2))&&cell==FloridaContactFailure.TimeDomain,"single-cell boundary");
        Check(!normal.Admit(out _,out _,system:SolAnalyticalDefinition.CreateForTest()),"changed model/mapping definition refused");
        Check(!normal.Admit(out _,out _,authority:query.Authority with {BodyId=7})&&
            !normal.Admit(out _,out _,authority:query.Authority with {FacilitySupportIdentity=999})&&
            !normal.Admit(out _,out _,authority:query.Authority with {PhysicalGeneration=999}),"body/facility/generation provenance refused");
        foreach(var eventTick in new[]{0L,500000L,1000000L})
        {
            var c=new Case(query,.5,-1);c.Timeline.Schedule(c.Start,new(new(1),new(eventTick),0,SimulationEventKind.NoOpMarker));
            Check(!c.Admit(out _,out var reason)&&reason==FloridaContactFailure.PendingTransaction,"pending event through endpoint");
        }
        var future=new Case(query,.5,-1);future.Timeline.Schedule(future.Start,new(new(1),new(1000001),0,SimulationEventKind.NoOpMarker));
        Check(future.Admit(out var f,out _),"event strictly after search");
        future.Timeline.Cancel(new(1));Check(future.Evaluate(f!).Status==FloridaContactStatus.Stale,"cancel changes timeline applicability");
        var direct=new Case(query,.5,-1);Check(direct.Admit(out var old,out _),"capture before transaction");
        direct.Timeline.Schedule(direct.Start,new(new(8),direct.Start,0,SimulationEventKind.Marker));
        Check(direct.Engine.ExecuteCanonicalPendingEvent().Committed,"canonical marker changes state");
        Check(direct.Evaluate(old!).Status==FloridaContactStatus.Stale,"old proof after canonical transaction");
        var force=new Case(query,.5,-1,radialAcceleration:-.2);Check(force.Admit(out var forceProvider,out _)&&
            force.Evaluate(forceProvider!).Status==FloridaContactStatus.CertifiedUniqueApproachingRoot,"nonzero constant-root-force crossing");
        Check(SimulationEventRequest.TryCreateSpacecraftForce(new(9),0,new(Craft,force.Start,new Double3(2,0,0)),out var forceRequest),"changed force intent");
        Check(force.Timeline.Schedule(force.Start,forceRequest).Succeeded&&force.Engine.ExecuteCanonicalPendingEvent().Committed,"force replacement commit");
        Check(force.Evaluate(forceProvider!).Status==FloridaContactStatus.Stale,"changed force segment invalidates proof");
        var torque=new Case(query,.5,-1);Check(torque.Admit(out var torqueProvider,out _),"capture before direct torque");
        var oldRoot=torque.Evaluate(torqueProvider!).Proof;var timelineBefore=torque.Timeline.Revision;var stateBefore=torque.Engine.State.Revision;
        var replacement=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(torque.Engine.State,new SpacecraftTorqueCommand(Craft,new Double3(0,1,0),torque.Start));
        Check(replacement.Succeeded&&torque.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed,"direct torque commit");
        Check(torque.Timeline.Revision==timelineBefore&&torque.Engine.State.Revision!=stateBefore,"independent state revision invalidation");
        Check(torque.Evaluate(torqueProvider!).Status==FloridaContactStatus.Stale&&
            FloridaContactProvider.Proof.Evaluate(torqueProvider!,torque.Use).Status==FloridaContactStatus.Stale,"all certificate issuance checks stale authority");
        Check(oldRoot.CompareRoot(oldRoot,torque.Use,torque.Use).Applicability==FloridaContactStatus.Stale&&
            oldRoot.CompareRational(PhysicalEventEpoch.FromCanonical(torque.End),torque.Engine,torque.Geometry,torque.System,torque.Graph,query).Applicability==FloridaContactStatus.Stale&&
            oldRoot.Refine(1e-5,24,torque.Use).Status==FloridaContactStatus.Stale,"all root operations revalidate state");
        Check(normal.Admit(out var normalProvider,out _),"fresh authority tests");
        var other=new Case(query,.5,-1);
        Check(normalProvider!.Evaluate(other.Engine,normal.Geometry,normal.System,normal.Graph,query).Status==FloridaContactStatus.Stale,"wrong engine rejected");
        Check(normalProvider.Evaluate(normal.Engine,normal.Geometry,normal.System,other.Graph,query).Status==FloridaContactStatus.Stale,"different root graph rejected");
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,new Double3(1,-2,4),ContactFeatureRole.LandingTip)],out var differentGeometry),"changed geometry fixture");
        Check(normalProvider.Evaluate(normal.Engine,differentGeometry!,normal.System,normal.Graph,query).Status==FloridaContactStatus.Stale,"same IDs changed geometry contents rejected");
        Check(normalProvider.Evaluate(normal.Engine,normal.Geometry,normal.System,normal.Graph,new ForgedQuery(query)).Status==FloridaContactStatus.Stale,"terrain capability changed at use");
        var negative=new Case(query,.5,-1,start:-1);Check(negative.Admit(out var negativeProvider,out _)&&negative.Evaluate(negativeProvider!).Status==FloridaContactStatus.CertifiedUniqueApproachingRoot,"negative whole-second cell");
        var rotated=new Case(query,.5,-1,attitude:new DoubleQuaternion(0,0,.6,.8));
        Check(rotated.Admit(out var rotatedProvider,out _)&&rotated.Evaluate(rotatedProvider!).Status==FloridaContactStatus.CertifiedUniqueApproachingRoot,"nonidentity frozen attitude and off-COM point");
        var outside=new Case(query,.5,-1,start:3600);Check(!outside.Admit(out _,out var outsideFailure)&&outsideFailure==FloridaContactFailure.TimeDomain,"qualified coverage is explicit");
        foreach(var c in new[]{new Case(query,0,0),new Case(query,-.5,-1),new Case(query,.5,-4,radialAcceleration:8),new Case(query,1,-4,radialAcceleration:8)})
        {Check(c.Admit(out var p,out _),"initial/tangent/multiple admitted observation");Check(c.Evaluate(p!).Status==FloridaContactStatus.Unresolved,"no false unique/clear");}
        var near=new Case(query,.5,-1,east:63);Check(near.Admit(out var nearP,out _)&&near.Evaluate(nearP!).Status==FloridaContactStatus.CertifiedUniqueApproachingRoot,"near inner edge");
        var escape=new Case(query,10,0,east:60,eastSpeed:40,eastAcceleration:-80);
        Check(escape.Admit(out var escapeP,out _),"escape endpoints source branch");
        Check(escape.Evaluate(escapeP!).Failure==FloridaContactFailure.GradingDomain,"interior escape not endpoint-certified");
        var blend=new Case(query,.5,-1,east:80);Check(!blend.Admit(out _,out var blendFailure)&&blendFailure==FloridaContactFailure.GradingDomain,"blend refusal");
        var far=new Case(query,.5,-1,east:400);Check(!far.Admit(out _,out _),"facility guard refusal");
        Check(!normal.Admit(out _,out _,geometry:ContactGenerationFixture.Geometry(2)),"complete geometry not subset");
        Check(default(FloridaContactProvider.Proof).CompareRoot(default,normal.Use,normal.Use).Applicability==FloridaContactStatus.Unsupported,"default forged proof");
        Check(typeof(FloridaContactProvider.Proof).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).All(x=>x.IsPrivate),"descriptor/proof construction is private; no caller-supplied version or flags");
    }
    private static void EnclosureObservations(Case c,FloridaContactProvider.Proof proof)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);
        Check(FloridaContactMotion.TryCreate(c.System,linear,angular,mass,c.Geometry.GetFeature(0).OffsetFromComMetres,out var motion),"witness source model");
        foreach(var seconds in new[]{proof.RootEnclosure.Lower,proof.RootEnclosure.Upper})
        {
            // These test enclosures are binary bisections of [0,1], at most 24 steps;
            // their microsecond fraction denominator divides 2^32. No root is converted.
            var ticks=seconds*SimulationInstant.TicksPerSecond;var floor=(long)Math.Floor(ticks);const ulong denominator=1UL<<32;
            var numerator=(ulong)((ticks-floor)*denominator);
            Check(PhysicalEventEpoch.TryCreate(floor,numerator,denominator,out var epoch)==PhysicalEventEpochStatus.Success,"exact rational witness endpoint");
            Check(SpacecraftPhysicalEventObservationEvaluator.Evaluate(c.Engine.State,c.Engine.State.Revision,epoch,c.Geometry,1,c.System,c.Graph,c.Query,c.Query.Authority,
                c.Evals,c.Roots,c.Staging,c.StagingRoots,out var observed).Succeeded,"M14.8 enclosure endpoint observation");
            Check(motion.Evaluate(FloridaBound.Point(seconds),out var q,out _),"independent endpoint enclosure");
            var region=FloridaFacilitySupport.Region;var t=FloridaVector.Dot(FloridaVector.From(region.Up),q);
            var gap=(t-((FloridaBound)region.RadiusMetres+region.PlaneAltitudeMetres))*q.Norm/t;
            var error=(FloridaBound)observed.RadialSignedGapMetres-gap;
            Check(gap.IsFinite&&error.IsFinite,"sample-specific numerical discrepancy is bounded");
            Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-root-witness",seconds,epoch.FloorTicks,epoch.Numerator,epoch.Denominator,
                mathematicalRadialGap=gap,observedGap=observed.RadialSignedGapMetres,observedMinusMathematical=error}));
        }
    }
    private static void Ordering(PlanetaryPhysicalSurfacePointQuery query)
    {
        var a=new Case(query,.25,-1);var b=new Case(query,.75,-1);
        Check(a.Admit(out var pa,out _)&&b.Admit(out _,out _),"ordering admission");b.Admit(out var pb,out _);
        var ra=a.Evaluate(pa!).Proof;var rb=b.Evaluate(pb!).Proof;
        var refined=ra.Refine(1e-5,24,a.Use);Check(refined.Status==FloridaContactStatus.CertifiedUniqueApproachingRoot,"bounded refinement completes");
        Check(ra.CompareRoot(refined.Proof,a.Use,a.Use).Order==FloridaRootOrder.Equal,"refinement never changes root identity");
        Check(ra.Refine(1e-5,0,a.Use).Failure==FloridaContactFailure.RefinementBudget&&ra.Refine(1e-5,0,a.Use).Status==FloridaContactStatus.Unresolved,"zero budget cannot publish approximate success");
        Check(ra.Refine(1e-20,24,a.Use).Status==FloridaContactStatus.Unresolved,"precision exhaustion is explicit");
        Check(ra.CompareRoot(rb,a.Use,b.Use).Order==FloridaRootOrder.Less&&rb.CompareRoot(ra,b.Use,a.Use).Order==FloridaRootOrder.Greater,"strict separated root order");
        Check(ra.CompareRational(PhysicalEventEpoch.FromCanonical(new(500000)),a.Engine,a.Geometry,a.System,a.Graph,query).Order==FloridaRootOrder.Less,"root before rational");
        Check(ra.CompareRational(PhysicalEventEpoch.FromCanonical(new(100000)),a.Engine,a.Geometry,a.System,a.Graph,query).Order==FloridaRootOrder.Greater,"root after rational");
        Check(a.Admit(out var replay,out _),"replay admission");var repeated=a.Evaluate(replay!).Proof;
        Check(ra.RootEnclosure==repeated.RootEnclosure&&ra.CompareRoot(repeated,a.Use,a.Use).Order==FloridaRootOrder.Equal,"independent replay same verified relation/root");
        // A sub-micrometre position delta disappears at the inertial-root magnitude.
        // Distinct stored velocities instead produce nearby, genuinely different relations.
        var close=new Case(query,.25,-1.000001);Check(close.Admit(out var pc,out _),"close admission");var rc=close.Evaluate(pc!).Proof;
        a.Engine.State.Spacecraft.TryGetTranslation(Craft,out var aState,out _);close.Engine.State.Spacecraft.TryGetTranslation(Craft,out var closeState,out _);
        Check(aState.VelocityRoot!=closeState.VelocityRoot,"nearby-root fixture retains distinct physical input bits");
        Check(ra.CompareRoot(rc,a.Use,close.Use).Order==FloridaRootOrder.Unresolved,"overlap is not equality/priority");
        var revision=a.Engine.State.Revision;var pending=a.Timeline.PendingCount;
        uint orderSeed=0x72483;
        for(var i=0;i<16;i++)
        {
            orderSeed=unchecked(orderSeed*1664525+1013904223);
            if((orderSeed&8)==0)_=rb.CompareRoot(ra,b.Use,a.Use);else _=ra.Refine(1e-5,24,a.Use);
            Check(a.Evaluate(pa!).Proof.RootEnclosure==ra.RootEnclosure&&b.Evaluate(pb!).Proof.RootEnclosure==rb.RootEnclosure,"seeded request-order determinism");
        }
        Check(a.Engine.State.Revision==revision&&a.Timeline.PendingCount==pending,"proof never mutates");
    }
    private static void Measurements(Case clear,Case crossing,Case unresolved)
    {
        foreach(var c in new[]{clear,crossing,unresolved})
        {
            Check(c.Admit(out var provider,out _),"measurement admission");var p=provider!;var expected=c.Evaluate(p).Status;
            for(var i=0;i<32;i++)_=c.Evaluate(p);
            var ticks=new long[101];
            for(var i=0;i<ticks.Length;i++){var before=Stopwatch.GetTimestamp();_=c.Evaluate(p);ticks[i]=Stopwatch.GetTimestamp()-before;}
            Array.Sort(ticks);var complete=0;
            using var measurement=new OrdinaryAllocationMeasurement("Florida "+expected);
            for(var i=0;i<32;i++)if(c.Evaluate(p).Status==expected)complete++;
            var bytes=measurement.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"Florida provider");Check(complete==32,"measured completions");
            double Ns(int index)=>ticks[index]*1e9/Stopwatch.Frequency;
            Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-cost",status=expected.ToString(),bytes,medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99),maximumNs=Ns(100),
                refinements=c.Evaluate(p).Proof.RefinementCount,maximumRefinements=FloridaContactProvider.MaximumRefinements,
                observationCallsPerAdmission=2,observationCallsPerEvaluation=0,proofSize=System.Runtime.CompilerServices.Unsafe.SizeOf<FloridaContactProvider.Proof>()}));
        }
        Check(crossing.Admit(out var crossingProvider,out _),"operations measurement setup");var root=crossing.Evaluate(crossingProvider!).Proof;
        var second=new Case(clear.Query,.75,-1);Check(second.Admit(out var secondProvider,out _),"comparison measurement setup");var otherRoot=second.Evaluate(secondProvider!).Proof;
        Measure("bounded refinement",()=>root.Refine(1e-5,24,crossing.Use).Status==FloridaContactStatus.CertifiedUniqueApproachingRoot);
        Measure("root comparison",()=>root.CompareRoot(otherRoot,crossing.Use,second.Use).Order==FloridaRootOrder.Less);
        Measure("rational comparison",()=>root.CompareRational(PhysicalEventEpoch.FromCanonical(new(250000)),crossing.Engine,crossing.Geometry,crossing.System,crossing.Graph,crossing.Query).Order==FloridaRootOrder.Greater);
        Measure("stale refusal",()=>crossingProvider!.Evaluate(second.Engine,crossing.Geometry,crossing.System,crossing.Graph,crossing.Query).Status==FloridaContactStatus.Stale);
        var unsupported=new Case(clear.Query,.5,-1,omega:new(0,1e-20,0));
        Measure("unsupported admission",()=>!unsupported.Admit(out _,out var failure)&&failure==FloridaContactFailure.MotionDomain);
        // Caller-owned scratch is allocated before provider admission. Timings remain outside no-GC.
        Measure("admission",()=>crossing.Admit(out _,out _),requireZero:false);
        var payload=2*crossing.Evals.Length*System.Runtime.CompilerServices.Unsafe.SizeOf<NovaCore.Core.ReferenceFrames.ReferenceFrameEvaluation>()+
            2*crossing.Roots.Length*System.Runtime.CompilerServices.Unsafe.SizeOf<NovaCore.Core.ReferenceFrames.FrameTransform>();
        Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-workspace",callerArrays=4,callerArrayPayloadBytes=payload,
            proofSizeBytes=System.Runtime.CompilerServices.Unsafe.SizeOf<FloridaContactProvider.Proof>(),maximumRefinementSteps=24,
            maximumRootComparisonNarrowingCalls=48,domainCallsPerCertification=3,noPerCallHeapWorkspace=true}));
    }
    private static void Measure(string name,Func<bool> work,bool requireZero=true)
    {
        for(var i=0;i<32;i++)Check(work(),name+" warmup");
        var ticks=new long[101];
        for(var i=0;i<ticks.Length;i++){var before=Stopwatch.GetTimestamp();var succeeded=work();ticks[i]=Stopwatch.GetTimestamp()-before;Check(succeeded,name+" timing");}
        Array.Sort(ticks);var completed=0;
        using var measurement=new OrdinaryAllocationMeasurement(name);
        for(var i=0;i<8;i++)if(work())completed++;
        var bytes=measurement.Complete();if(requireZero)OrdinaryAllocationMeasurement.RequireZero(bytes,name);Check(completed==8,name+" completions");
        double Ns(int index)=>ticks[index]*1e9/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="Florida-operation-cost",name,bytes,calls=8,bytesPerCall=bytes/8,
            medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99),maximumNs=Ns(100),timingSamples=101}));
    }
}
