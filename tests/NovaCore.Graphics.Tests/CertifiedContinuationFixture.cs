using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Time;
using static ContactGenerationFixture;
using Case = FloridaContactProductionTests.Case;
using Proof = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof;
using CoverageOwner = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof.PostImpactCoverageProvider;

internal sealed class CertifiedContinuationFixture
{
    internal static void Check(bool condition,string contract)
    { if(!condition) throw new InvalidOperationException(contract); }
    internal static readonly FloridaKinematicsRequest Kinematics = new(1e-5,1e-8,1e-5,1e-12,24);
    internal static readonly CertifiedResponseRequest Response = new(1e-6,1e-6,1e-6,3e-6);
    internal static readonly CertifiedPreImpactVelocityRequest Preimpact = new(1e-5,1e-4);
    internal static readonly PrivatePostImpactPoseRequest Pose = new(.5);
    internal static readonly PrivatePropagationRequest Propagation = new(.002,2e-6,2e-6,4e-6);
    internal readonly Case Source;
    internal CertifiedContinuationRequest Request;
    internal readonly PrivateCanonicalStateValues Staged;
    internal readonly CoverageStatus CoverageStatus;
    internal readonly CoverageFailure CoverageFailure;
    internal FloridaContactUse Use => Source.Use;

    internal static PlanetaryPhysicalSurfacePointQuery Acquire()
    {
        var root=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)==PhysicalSurfaceQueryStatus.Ready,"publication real terrain");
        return query!;
    }

    internal CertifiedContinuationFixture(PlanetaryPhysicalSurfacePointQuery query,bool force=false,int capacity=1,
        StateRevision revision=default,long debt=1_500_000,bool sourceGap=false,Case? existing=null,
        ContinuationQualificationInputs? qualification=null,PrivatePropagationRequest? propagation=null)
    {
        Source=existing??new Case(query,.5,-1,radialAcceleration:force?-.2:0,attitude:new(0,0,.6,.8),
            initialRevision:revision,publicationCapacity:capacity);
        if(existing is null && debt>0) Source.Clock.AdvanceByHostDuration(new(debt));
        var start=sourceGap?new SimulationInstant(1):Source.Start;
        FloridaContactProvider.TryCreate(Source.Engine,Source.Engine.State.Revision,start,Source.End,Source.Geometry,Source.System,
            Source.Graph,Source.Query,Source.Query.Authority,Source.Evals,Source.Roots,Source.Staging,Source.StagingRoots,
            out var provider,out var failure);
        Check(provider is not null,"publication provider admission: "+failure);
        var root=provider!.Evaluate(Source.Engine,Source.Geometry,Source.System,Source.Graph,Source.Query).Proof;
        Check(root.IsRoot,"publication first root");
        var keys=qualification??new(Kinematics,Response,Preimpact,Pose,0);
        var propagationKey=propagation??Propagation;
        var k=root.QualifyKinematics(keys.Kinematics,Use);
        Check(k.Status==FloridaKinematicsStatus.Qualified,"publication kinematics");
        var response=root.QualifyResponse(k.Witness,keys.Response,Use);
        Check(response.Status==CertifiedResponseStatus.Qualified,"publication response");
        var pre=root.QualifyPreImpactVelocity(k.Witness,keys.Preimpact,Use);
        Check(pre.Status==CertifiedPreImpactVelocityStatus.Qualified,"publication preimpact");
        var final=root.QualifyPostImpactVelocity(k.Witness,response.Proposal,keys.Response,pre.Tuple,keys.Preimpact,Use);
        Check(final.Status==CertifiedPostImpactVelocityStatus.Qualified,"publication final velocity");
        var initial=root.PreparePrivatePostImpactState(k.Witness,response.Proposal,keys.Response,pre.Tuple,keys.Preimpact,final.Realization,keys.Pose,Use);
        Check(initial.Status==PrivatePostImpactStateStatus.Ready,"publication private alpha state");
        var staged=root.PreparePrivatePropagation(initial.State,final.Realization,keys.Pose,Source.End,propagationKey,Use);
        Check(staged.Status==PrivatePropagationStatus.Ready,"publication staged endpoint");
        Check(staged.State.Read(root,initial.State,final.Realization,keys.Pose,Source.End,propagationKey,Use,out Staged)==PrivatePropagationStatus.Ready,
            "publication checked endpoint");
        Check(CoverageOwner.TryCreate(staged.State,root,initial.State,final.Realization,keys.Pose,Source.End,propagationKey,Use,out var coverage)==PrivatePropagationStatus.Ready,
            "publication coverage owner");
        CoverageStatus=coverage!.Evaluate(staged.State,Use,out var receipt,out var result);
        CoverageFailure=result.Failure;
        Check(CoverageStatus==(force?CoverageStatus.Unresolved:CoverageStatus.EventFreeThroughTarget),"unchanged Coast/Force status");
        Check(result.Failure==(force?CoverageFailure.DepartureUnproved:CoverageFailure.None),"unchanged Coast/Force reason");
        if(Source.Start.Ticks==0)Check(result.Visits==(force?25:63)&&result.MaximumDepth==(force?24:5),"unchanged Coast/Force proof work");
        Request=new(root,initial.State,final.Realization,keys.Pose,Source.End,propagationKey,staged.State,receipt,
            Source.Engine.State.Revision,Source.Timeline.Revision,Source.Engine.CaptureContinuationClock());
    }

    internal ContinuationPublicationResult Publish()=>Source.Engine.PublishCertifiedContinuation(Request,Use);
    internal void RefreshExpectations()=>Request=Request with { Clock=Source.Engine.CaptureContinuationClock(),
        StateRevision=Source.Engine.State.Revision,TimelineRevision=Source.Timeline.Revision };

    internal static readonly JsonSerializerOptions Json=Options();
    private static JsonSerializerOptions Options()
    {
        var options=new JsonSerializerOptions { IncludeFields=true };
        options.Converters.Add(new DoubleBitsConverter());
        options.Converters.Add(new SimulationRateConverter());
        return options;
    }
    private sealed class DoubleBitsConverter:JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
            BitConverter.Int64BitsToDouble(Convert.ToInt64(reader.GetString(),16));
        public override void Write(Utf8JsonWriter writer,double value,JsonSerializerOptions options)=>
            writer.WriteStringValue(BitConverter.DoubleToInt64Bits(value).ToString("X16"));
    }
    // Test record reader: readonly SimulationRate has an invariant-enforcing constructor,
    // not JSON-writable properties. Reconstruct through that constructor without changing production.
    private sealed class SimulationRateConverter:JsonConverter<SimulationRate>
    {
        public override SimulationRate Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {using var doc=JsonDocument.ParseValue(ref reader);return new(doc.RootElement.GetProperty("Numerator").GetInt64(),doc.RootElement.GetProperty("Denominator").GetInt64());}
        public override void Write(Utf8JsonWriter writer,SimulationRate value,JsonSerializerOptions options)
        {writer.WriteStartObject();writer.WriteNumber("Numerator",value.Numerator);writer.WriteNumber("Denominator",value.Denominator);writer.WriteEndObject();}
    }
    internal static string Bits<T>(T value)=>JsonSerializer.Serialize(value,Json);
    internal string Snapshot()
    {
        var engine=Source.Engine;var state=engine.State;
        var craft=Source.Geometry.Spacecraft;
        state.Spacecraft.TryGetTranslation(craft,out var linear,out var properties);
        state.Spacecraft.TryGetRigidBody(craft,out var angular);
        state.Spacecraft.TryGetAttitude(craft,out var attitude);
        var pending=new ScheduledSimulationEvent[Source.Timeline.PendingCount];Source.Timeline.CopyPending(pending);
        var intents=new List<SpacecraftContactImpulseIntent>();
        foreach(var item in pending)if(Source.Timeline.TryResolveContactImpulse(item,out var intent))intents.Add(intent);
        var histories=new SortedDictionary<string,object[]>();
        foreach(var field in typeof(SimulationTransactionEngine).GetFields(BindingFlags.Instance|BindingFlags.NonPublic))
        {
            if(field.GetValue(engine) is IList list && field.Name!="_continuationHistory")
            {var values=new object[list.Count];list.CopyTo(values,0);histories.Add(field.Name,values);}
        }
        var publications=new ProcessedCertifiedContinuation[engine.ProcessedContinuationCount];
        for(var i=0;i<publications.Length;i++)Check(engine.TryGetProcessedContinuation(i,out publications[i]),"history copy");
        var allCraft=new object[state.Spacecraft.Count];
        for(var i=0;i<allCraft.Length;i++)
        {var id=state.Spacecraft.GetDefinition(i).Id;state.Spacecraft.TryGetTranslation(id,out var l,out var p);state.Spacecraft.TryGetRigidBody(id,out var r);state.Spacecraft.TryGetAttitude(id,out var a);allCraft[i]=new{id,l,p,r,a};}
        return Bits(new{linear,angular,attitude,properties,allCraft,state.MarkerValue,state.Revision,
            clock=engine.CaptureContinuationClock(),timeline=TimelineSnapshot(Source.Timeline),intents,histories,publications});
    }

    internal static object EventSnapshot(ScheduledSimulationEvent e)=>new{e.Header,payload=new{
        e.Payload.Kind,e.Payload.ContactSlot,e.Payload.Subject,e.Payload.SpacecraftSubject,e.Payload.DeltaVelocity,e.Payload.ForceRoot}};
    internal static object TimelineSnapshot(SimulationTimeline timeline)
    {
        object Field(string name)=>typeof(SimulationTimeline).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(timeline)!;
        var pending=new ScheduledSimulationEvent[timeline.PendingCount];timeline.CopyPending(pending);
        var cancelled=(List<ScheduledSimulationEvent>)Field("_cancelled");
        var slots=(SimulationTimeline.ContactPayloadSlot[])Field("_contactPayloads");
        return new{timeline.Revision,timeline.PendingCount,timeline.CancelledCount,timeline.ContactPayloadCount,
            pending=pending.Select(EventSnapshot).ToArray(),cancelled=cancelled.Select(EventSnapshot).ToArray(),
            used=((HashSet<SimulationEventId>)Field("_usedIds")).Select(x=>x.Value).Order().ToArray(),next=Field("_nextSequenceValue"),
            freeHead=Field("_contactFreeHead"),slots=slots.Select(x=>new{x.Intent,x.Event,x.NextFree}).ToArray()};
    }

    internal static void SameEndpoint(in PrivateCanonicalStateValues staged,in ContinuationPublicationObservation actual)
    {
        Check(Bits(actual.Translation.PositionRoot)==Bits(staged.Endpoint.PositionRoot),"position exact bits");
        Check(Bits(actual.Translation.VelocityRoot)==Bits(staged.Endpoint.VelocityRoot),"velocity exact bits");
        Check(Bits(actual.Rotation.OrientationLocalToParent)==Bits(staged.Endpoint.OrientationBodyToRoot),"attitude exact bits");
        Check(Bits(actual.Rotation.AngularVelocityBody)==Bits(staged.Endpoint.AngularVelocityBody),"spin exact bits");
    }
}
