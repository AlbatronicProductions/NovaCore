using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using static ContactGenerationFixture;
using static ContactKinematicsOracle;
using R=CertifiedResponseOracle.R;
using B=CertifiedResponseOracle.B;
using Case=FloridaContactProductionTests.Case;

internal static class CertifiedFloridaResponseTests
{
    private static void Check(bool value,string name)=>ContactGenerationFixture.Check(value,"response "+name);
    private static readonly FloridaKinematicsRequest Kinematics=new(1e-5,1e-8,1e-5,1e-12,24);
    // Full output widths, chosen to qualify the existing useful fixture; not response slop.
    private static readonly CertifiedResponseRequest Request=new(1e-6,1e-6,1e-6,3e-6);
    private static readonly CertifiedResponseRequest Tight=new(1e-30,1e-30,1e-30,1e-30);

    internal static void Run()
    {
        var repository=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(repository,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(repository,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"acquire terrain");var query=acquired!;
        var c=Create(query);var(root,witness)=Source(c);var before=Snapshot(c);var rootBefore=(root.RootEnclosure,root.Function,root.Derivative,root.RefinementCount);
        var qualified=root.QualifyResponse(witness,Request,c.Use);
        Check(qualified.Status==CertifiedResponseStatus.Qualified,"useful response qualifies: "+qualified.Failure);
        var proposal=qualified.Proposal;
        Check(proposal.Read(root,witness,Request,c.Use,out var values)==CertifiedResponseStatus.Qualified,"checked read");
        Check(witness.Read(root,c.Use,out var source)==FloridaKinematicsStatus.Qualified,"checked source");
        IndependentRootResponse(c,source,values);
        Check(values.AngularImpulseBody.X.Upper<0&&values.AngularImpulseBody.Y.Upper<0&&values.AngularImpulseBody.Z.Upper<0,"off-center angular response retained");
        Print(values);

        var tight=root.QualifyResponse(witness,Tight,c.Use);
        Check(tight.Status==CertifiedResponseStatus.Unresolved&&tight.Failure==CertifiedResponseFailure.RequestedWidth&&
            tight.Proposal.Read(root,witness,Tight,c.Use,out var failed)==CertifiedResponseStatus.Unsupported&&failed==default,"width refusal has no usable receipt");
        Check(root.QualifyResponse(witness,default,c.Use).Status==CertifiedResponseStatus.Unsupported,"invalid request");
        var refined=root.Refine(1e-5,24,c.Use).Proof;
        Check(witness.Read(refined,c.Use,out _)==FloridaKinematicsStatus.Qualified,"upstream immutable lineage remains accepted");
        Check(proposal.Read(refined,witness,Request,c.Use,out _)==CertifiedResponseStatus.Unsupported,"refined tuple requires reissuance");
        Check(refined.QualifyResponse(witness,Request,c.Use).Status==CertifiedResponseStatus.Qualified,"explicit refined-tuple issuance");
        var coarseResult=root.QualifyKinematics(new(1,.01,.5,1e-12,24),c.Use);
        Check(coarseResult.Status==FloridaKinematicsStatus.Qualified,"substituted coarse source is valid");
        var coarse=coarseResult.Witness;
        Check(proposal.Read(root,coarse,Request,c.Use,out _)==CertifiedResponseStatus.Unsupported,"changed qualified values refused");
        Check(proposal.Read(root,witness,Request with{ScalarImpulse=2e-6},c.Use,out _)==CertifiedResponseStatus.Unsupported,"changed response request refused");
        Check(proposal.Read(default,witness,Request,c.Use,out _)==CertifiedResponseStatus.Unsupported&&
            proposal.Read(root,default,Request,c.Use,out _)==CertifiedResponseStatus.Unsupported,"default substituted inputs");
        Check(root.QualifyResponse(default,Request,c.Use).Status==CertifiedResponseStatus.Unsupported,"default source cannot issue");

        var replay=Create(query);var(rr,rw)=Source(replay);var rp=rr.QualifyResponse(rw,Request,replay.Use);
        Check(rp.Proposal.Read(rr,rw,Request,replay.Use,out var repeated)==CertifiedResponseStatus.Qualified&&Bits(values).SequenceEqual(Bits(repeated)),"independent replay numerical bits");
        Check(proposal.Read(rr,rw,Request,replay.Use,out _)==CertifiedResponseStatus.Unsupported&&
            root.QualifyResponse(rw,Request,c.Use).Status==CertifiedResponseStatus.Unsupported,"independent owner cannot borrow capability");
        uint seed=91;
        for(var i=0;i<24;i++)
        {
            seed=unchecked(seed*1664525+1013904223);var request=(seed&1)==0?Request:Tight;
            var result=root.QualifyResponse(witness,request,c.Use);
            Check(result.Status==((seed&1)==0?CertifiedResponseStatus.Qualified:CertifiedResponseStatus.Unresolved),"seeded status");
            Check(root.QualifyResponse(witness,Request,c.Use).Proposal.Read(root,witness,Request,c.Use,out var next)==CertifiedResponseStatus.Qualified&&
                Bits(values).SequenceEqual(Bits(next)),"reordered requests preserve exact bits");
        }
        Check(before==Snapshot(c)&&rootBefore==(root.RootEnclosure,root.Function,root.Derivative,root.RefinementCount),"full authority/root nonmutation");
        Refusals(query);
        Measure("request setup",()=>new CertifiedResponseRequest(1e-6,1e-6,1e-6,3e-6).IsValid);
        Measure("qualified proposal",()=>root.QualifyResponse(witness,Request,c.Use).Status==CertifiedResponseStatus.Qualified);
        Measure("checked consumption",()=>proposal.Read(root,witness,Request,c.Use,out _)==CertifiedResponseStatus.Qualified);
        Measure("width refusal",()=>root.QualifyResponse(witness,Tight,c.Use).Status==CertifiedResponseStatus.Unresolved);
        Measure("stale refusal",()=>root.QualifyResponse(witness,Request,replay.Use).Status==CertifiedResponseStatus.Stale);
        Measure("unsupported refusal",()=>default(FloridaContactProvider.Proof).QualifyResponse(witness,Request,c.Use).Status==CertifiedResponseStatus.Unsupported);
        Check(before==Snapshot(c),"measurement does not mutate authority");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine(JsonSerializer.Serialize(new{kind="response-workspace",proposalBytes=Unsafe.SizeOf<FloridaContactProvider.Proof.ResponseProposal>(),
            valuesBytes=Unsafe.SizeOf<CertifiedResponseValues>(),requestBytes=Unsafe.SizeOf<CertifiedResponseRequest>(),additionalHeapWorkspaceBytes=0,
            sourceReadsPerQualification=1,sourceReadsPerConsumption=1,refinementCalls=0,responseEvaluationsPerQualification=1,
            policyVersion=FloridaContactProvider.Proof.ResponseProposal.PolicyVersion,numericalVersion=FloridaContactProvider.Proof.ResponseProposal.NumericalVersion}));
        Console.WriteLine("CERTIFIED_FLORIDA_RESPONSE containment/width/refined-tuple/replay/stale/nonmutation/allocation PASS");
    }
    private static Case Create(PlanetaryPhysicalSurfacePointQuery q)=>new(q,.5,-1,radialAcceleration:-.2,attitude:new(0,0,.6,.8));
    private static (FloridaContactProvider.Proof,FloridaContactProvider.Proof.Kinematics) Source(Case c)
    {
        Check(c.Admit(out var p,out _),"source admission");var root=c.Evaluate(p!).Proof;var result=root.QualifyKinematics(Kinematics,c.Use);
        Check(root.IsRoot&&result.Status==FloridaKinematicsStatus.Qualified,"root kinematics");return(root,result.Witness);
    }
    private static string Snapshot(Case c)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);
        return JsonSerializer.Serialize(new{linear,mass,angular,c.Engine.State.Revision,c.Engine.State.MarkerValue,
            timeline=c.Timeline.Revision,pending=c.Timeline.PendingCount,time=c.Engine.ContactProofCurrentTime,
            general=c.Engine.ProcessedCount,forces=c.Engine.ProcessedSpacecraftForceCount,torques=c.Engine.ProcessedRigidBodyTorqueCount,
            attitudes=c.Engine.ProcessedSpacecraftAttitudeCount,contacts=c.Engine.ProcessedContactImpulseCount,terrain=c.Query.Authority,geometry=c.Geometry.Identity});
    }
    private static long[] Bits(CertifiedResponseValues v)=>new[]{v.EffectiveInverseMass,v.ScalarImpulse,v.LinearImpulseRoot.X,v.LinearImpulseRoot.Y,v.LinearImpulseRoot.Z,
        v.AngularImpulseBody.X,v.AngularImpulseBody.Y,v.AngularImpulseBody.Z}.SelectMany(b=>new[]{BitConverter.DoubleToInt64Bits(b.Lower),BitConverter.DoubleToInt64Bits(b.Upper)}).ToArray();
    private static void Print(CertifiedResponseValues v)=>Console.WriteLine(JsonSerializer.Serialize(new{kind="response-values",k=new[]{v.EffectiveInverseMass.Lower,v.EffectiveInverseMass.Upper},
        j=new[]{v.ScalarImpulse.Lower,v.ScalarImpulse.Upper},linear=new[]{v.LinearImpulseRoot.X,v.LinearImpulseRoot.Y,v.LinearImpulseRoot.Z},angular=new[]{v.AngularImpulseBody.X,v.AngularImpulseBody.Y,v.AngularImpulseBody.Z}}));

    private static void IndependentRootResponse(Case c,FloridaContactKinematics input,CertifiedResponseValues result)
    {
        c.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var mass);c.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);
        var system=c.System;system.TryGetNode(SolarSystemBodyIds.Earth,out var node);system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex,out var earth);
        system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun,out var sun);CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        // Same independently justified seed-cell ODE/decimal budget as the banked kinematics oracle.
        // |Earth''''| <=1.36e-13; <=20,000 decimal operations at physical scale <=2e11.
        // 1e-10 m/m/s conservatively covers arithmetic and Taylor remainder in this guarded fixture.
        Check(V.From(earth.StateAtEpoch.Position).Norm>1.1e11m&&V.From(earth.StateAtEpoch.Position).Norm<2e11m&&
            V.From(earth.StateAtEpoch.Velocity).Norm<4e4m&&sun.GravitationalParameter<2e20&&V.From(linear.PositionRoot).Norm<2e11m,"independent numerical scale");
        var rates=Math.Abs(D(model.RaT)/(D(model.SecondsPerDay)*D(model.DaysPerCentury)))+
            Math.Abs(D(model.DecT)/(D(model.SecondsPerDay)*D(model.DaysPerCentury)))+Math.Abs(D(model.Wd)/D(model.SecondsPerDay));
        Check(rates*Pi/180<1e-4m&&V.From(linear.ConstantForceRoot).Norm/D(mass.MassKilograms)<1&&
            (V.From(linear.VelocityRoot)-V.From(earth.StateAtEpoch.Velocity)).Norm+10<1000&&
            (V.From(linear.PositionRoot)-V.From(earth.StateAtEpoch.Position)).Norm+10000<1e7m,"independent derivative bound domain");
        const decimal error=1e-10m;decimal lo=0,hi=1;
        for(var i=0;i<26;i++){var mid=(lo+hi)/2;var p=EarthPoint(linear,angular,mass.MassKilograms,input.AuthoredLeverBodyMetres,mid);if(p.Gap>error)lo=mid;else if(p.Gap< -error)hi=mid;else break;}
        Check(EarthPoint(linear,angular,mass.MassKilograms,input.AuthoredLeverBodyMetres,lo).Gap>error&&
            EarthPoint(linear,angular,mass.MassKilograms,input.AuthoredLeverBodyMetres,hi).Gap< -error,"independent robust root bracket");
        var center=EarthPoint(linear,angular,mass.MassKilograms,input.AuthoredLeverBodyMetres,(lo+hi)/2);var h=(hi-lo)/2;
        var(_,omega)=Orientation(model,(lo+hi)/2,FloridaFacilitySupport.Region.Up);var np=V.Cross(omega,center.Normal);
        B Range(decimal value,decimal radius)=>new(R.From(value-radius),R.From(value+radius));
        var tail=1e-8m*h*h+1e-22m;
        B[] n=[Range(center.Normal.X,Math.Abs(np.X)*h+tail),Range(center.Normal.Y,Math.Abs(np.Y)*h+tail),Range(center.Normal.Z,Math.Abs(np.Z)*h+tail)];
        // |u'| <= A+2 Omega V+2 Omega² P <=10.4. Entire alpha bracket, not one sampled state.
        var u=Range(center.Speed,10.4m*h+error);
        var oracle=CertifiedResponseOracle.Enclose(n,u,input.AuthoredLeverBodyMetres,input.FixedAttitude,input.MassKilograms,input.PrincipalInertia);
        Check(CertifiedResponseOracle.Contains(result.EffectiveInverseMass,oracle.K)&&CertifiedResponseOracle.Contains(result.ScalarImpulse,oracle.J)&&
            CertifiedResponseOracle.Contains(result.LinearImpulseRoot,oracle.Linear)&&CertifiedResponseOracle.Contains(result.AngularImpulseBody,oracle.Angular),"independent entire-root response containment");
        Console.WriteLine(JsonSerializer.Serialize(new{kind="response-independent-root",rootLower=lo,rootUpper=hi,speed=center.Speed,speedError=10.4m*h+error,
            normalRemainder=tail,oracle="independent ODE/decimal root bracket then exact-rational interval rotation matrix and response",containment="PASS"}));
    }

    private sealed class CopiedTerrain(IPhysicalSurfacePointQuery source):IPhysicalSurfacePointQuery
    {public PhysicalSurfaceAuthorityIdentity Authority=>source.Authority;public PhysicalSurfacePointResult Query(ulong b,in Double3 d)=>source.Query(b,d);}
    private static void Refusals(PlanetaryPhysicalSurfacePointQuery query)
    {
        var c=Create(query);var(root,witness)=Source(c);var proposal=root.QualifyResponse(witness,Request,c.Use).Proposal;var other=Create(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,new(1,-2,4),ContactFeatureRole.LandingTip)],out var geometry),"changed geometry");
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(2,new(1,-2,3),ContactFeatureRole.LandingTip)],out var feature),"changed feature");
        foreach(var use in new[]{c.Use with{Engine=other.Engine},c.Use with{Geometry=geometry!},c.Use with{Geometry=feature!},c.Use with{Graph=other.Graph},
            c.Use with{System=SolAnalyticalDefinition.CreateForTest()},c.Use with{Terrain=new CopiedTerrain(query)}})
            Check(proposal.Read(root,witness,Request,use,out var value)==CertifiedResponseStatus.Stale&&value==default&&
                root.QualifyResponse(witness,Request,use).Status==CertifiedResponseStatus.Stale,"changed authority both paths");
        foreach(var change in new[]{"timeline","clock","state","force","torque","mass","inertia"})
        {
            var a=Create(query);var(ar,aw)=Source(a);var ap=ar.QualifyResponse(aw,Request,a.Use).Proposal;
            var oldState=a.Engine.State.Revision;var oldTimeline=a.Timeline.Revision;Action restore=()=>{};
            try
            {
                if(change=="timeline"){a.Timeline.Schedule(a.Start,new(new(71),new(1000001),0,SimulationEventKind.NoOpMarker));a.Timeline.Cancel(new(71));Check(a.Engine.State.Revision==oldState,"timeline-only control");}
                else if(change=="clock"){a.Engine.AdvanceAndExecuteOneCanonicalGroup(new(a.Start.Ticks+1));Check(a.Engine.State.Revision==oldState&&a.Timeline.Revision==oldTimeline,"clock-only control");}
                else if(change=="torque")
                {var replacement=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(a.Engine.State,new SpacecraftTorqueCommand(Craft,new(0,1,0),a.Start));Check(replacement.Succeeded&&a.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed,"torque commit");}
                else if(change is "mass" or "inertia")
                {
                    // Fault injection only into a disposable test fixture: no production replacement API added.
                    var store=typeof(NovaCore.Simulation.Transactions.SimulationState).GetField("_spacecraft",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(a.State)!;
                    if(change=="mass")
                    {var data=(SpacecraftPhysicalProperties[])store.GetType().GetField("_properties",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(store)!;var old=data[0];data[0]=new(9);restore=()=>data[0]=old;}
                    else
                    {var data=(SpacecraftRigidBodyRotationState[])store.GetType().GetField("_rigidBodies",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(store)!;var old=data[0];data[0]=old with{PrincipalInertia=new(3,4,5)};restore=()=>data[0]=old;}
                    Check(a.Engine.State.Revision==oldState&&a.Timeline.Revision==oldTimeline,"direct frozen-property guard control");
                }
                else
                {var request=new SimulationEventRequest(new(91),a.Start,0,SimulationEventKind.Marker);if(change=="force")Check(SimulationEventRequest.TryCreateSpacecraftForce(new(91),0,new(Craft,a.Start,new(2,0,0)),out request),"force intent");Check(a.Timeline.Schedule(a.Start,request).Succeeded&&a.Engine.ExecuteCanonicalPendingEvent().Committed,"source mutation");}
                Check(ap.Read(ar,aw,Request,a.Use,out var value)==CertifiedResponseStatus.Stale&&value==default&&
                    ar.QualifyResponse(aw,Request,a.Use).Status==CertifiedResponseStatus.Stale,change+" invalidates issuance/read");
            }
            finally{restore();}
            if(change is "mass" or "inertia")Check(ap.Read(ar,aw,Request,a.Use,out _)==CertifiedResponseStatus.Qualified,"fault injection restored");
        }
    }
    private static void Measure(string name,Func<bool> work)
    {
        for(var i=0;i<32;i++)Check(work(),name+" warmup");var ticks=new long[101];
        for(var i=0;i<101;i++){var start=Stopwatch.GetTimestamp();var ok=work();ticks[i]=Stopwatch.GetTimestamp()-start;Check(ok,name+" timing");}
        Array.Sort(ticks);var completed=0;using var measurement=new OrdinaryAllocationMeasurement("certified response "+name);
        for(var i=0;i<8;i++)if(work())completed++;var bytes=measurement.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,name);Check(completed==8,name+" completion");
        double Ns(int i)=>ticks[i]*1e9/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="response-cost",name,bytes,calls=8,medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99),maximumNs=Ns(100)}));
    }
}
