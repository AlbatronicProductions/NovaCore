using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum FloridaContactStatus : byte { Unresolved, CertifiedClear, CertifiedUniqueApproachingRoot, Unsupported, Stale }
internal enum FloridaContactFailure : byte
{
    None, InvalidGeometry, UnsupportedAuthority, MotionDomain, TimeDomain, PendingTransaction, Observation,
    GradingDomain, InitialContactOrPenetration, NumericalResolution, NotStrictlyApproaching, InvalidProof, ChangedAuthority, RefinementBudget,
}
internal enum FloridaRootOrder : byte { Unresolved, Less, Equal, Greater }
internal readonly record struct FloridaContactResult(FloridaContactStatus Status,FloridaContactFailure Failure,
    FloridaContactProvider.Proof Proof=default);
internal readonly record struct FloridaComparison(FloridaContactStatus Applicability,FloridaRootOrder Order);
internal readonly record struct FloridaContactUse(SimulationTransactionEngine Engine,SpacecraftContactGeometry Geometry,
    CelestialSystemDefinition System,ReferenceFrameGraph Graph,IPhysicalSurfacePointQuery Terrain);

/// <summary>
/// One frozen current-Earth relation. Admission owns bounded setup; evaluation/refinement allocate nothing.
/// Single-writer read-phase only. Never schedules, executes or grants response permission.
/// </summary>
internal sealed partial class FloridaContactProvider
{
    internal const uint ProviderVersion=1,RelationVersion=1;
    internal const int MaximumRefinements=24;
    private readonly SpacecraftTranslationState linear;
    private readonly SpacecraftRigidBodyRotationState angular;
    private readonly SpacecraftPhysicalProperties properties;
    private readonly SpacecraftContactGeometry geometry;
    private readonly StateRevision stateRevision;
    private readonly TimelineRevision timelineRevision;
    private readonly PhysicalSurfaceAuthorityIdentity authority;
    private readonly IPhysicalGradingProofSource terrain;
    private readonly ReferenceFrameGraph graph;
    private readonly FloridaContactMotion motion;
    private readonly FacilitySupportRegion region;
    private readonly SimulationInstant start,end;
    private readonly object engineIdentity;
    internal SpacecraftPhysicalEventContactObservation StartObservation { get; }
    internal SpacecraftPhysicalEventContactObservation EndObservation { get; }
    internal FloridaBound StartNumericalError { get; }
    internal FloridaBound EndNumericalError { get; }
    internal double CoveragePositionRemainder=>motion.PositionRemainderAtCoverage;
    internal PhysicalSurfaceAuthorityIdentity TerrainAuthority=>authority;

    private FloridaContactProvider(SimulationTransactionEngine engine,SpacecraftTranslationState linear,SpacecraftRigidBodyRotationState angular,
        SpacecraftPhysicalProperties properties,SpacecraftContactGeometry geometry,StateRevision revision,TimelineRevision timeline,
        IPhysicalGradingProofSource terrain,ReferenceFrameGraph graph,FloridaContactMotion motion,SimulationInstant start,SimulationInstant end,
        SpacecraftPhysicalEventContactObservation first,SpacecraftPhysicalEventContactObservation last,FloridaBound firstError,FloridaBound lastError)
    {
        engineIdentity=engine;this.linear=linear;this.angular=angular;this.properties=properties;this.geometry=geometry;
        stateRevision=revision;timelineRevision=timeline;this.terrain=terrain;authority=terrain.Authority;this.graph=graph;
        this.motion=motion;region=terrain.GradingRegion;this.start=start;this.end=end;
        StartObservation=first;EndObservation=last;StartNumericalError=firstError;EndNumericalError=lastError;
    }

    internal static FloridaContactStatus TryCreate(SimulationTransactionEngine engine,StateRevision expectedRevision,
        SimulationInstant start,SimulationInstant end,SpacecraftContactGeometry? geometry,CelestialSystemDefinition system,
        ReferenceFrameGraph graph,IPhysicalSurfacePointQuery? query,in PhysicalSurfaceAuthorityIdentity expectedAuthority,
        Span<ReferenceFrameEvaluation> evaluations,Span<FrameTransform> roots,Span<ReferenceFrameEvaluation> staging,Span<FrameTransform> stagingRoots,
        out FloridaContactProvider? provider,out FloridaContactFailure failure)
    {
        provider=null;failure=FloridaContactFailure.None;
        var view=engine.State;var timeline=engine.ContactProofTimelineRevision;
        if(view.Revision!=expectedRevision){failure=FloridaContactFailure.ChangedAuthority;return FloridaContactStatus.Stale;}
        if(geometry is null||geometry.Count!=1){failure=FloridaContactFailure.InvalidGeometry;return FloridaContactStatus.Unsupported;}
        if(query is not IPhysicalGradingProofSource terrain||query.Authority!=expectedAuthority||
            terrain.GradingRegion!=FloridaFacilitySupport.Region||expectedAuthority.FacilitySupportIdentity!=FloridaFacilitySupport.DefinitionIdentity||
            expectedAuthority.ReferenceRadiusMetres!=terrain.GradingRegion.RadiusMetres)
        {failure=FloridaContactFailure.UnsupportedAuthority;return FloridaContactStatus.Unsupported;}
        var cell=start.Ticks/SimulationInstant.TicksPerSecond;if(start.Ticks%SimulationInstant.TicksPerSecond<0)cell--;
        if(start>=end||start<engine.ContactProofCurrentTime||start.Ticks< -FloridaContactMotion.CoverageSeconds*SimulationInstant.TicksPerSecond||
            end.Ticks>FloridaContactMotion.CoverageSeconds*SimulationInstant.TicksPerSecond||end.Ticks>(cell+1)*SimulationInstant.TicksPerSecond)
        {failure=FloridaContactFailure.TimeDomain;return FloridaContactStatus.Unsupported;}
        if(engine.HasContactProofBoundaryThrough(end)){failure=FloridaContactFailure.PendingTransaction;return FloridaContactStatus.Unsupported;}
        if(!view.Spacecraft.TryGetTranslation(geometry.Spacecraft,out var linear,out var properties)||
            !view.Spacecraft.TryGetRigidBody(geometry.Spacecraft,out var angular)||start<linear.Epoch||start<angular.Epoch||
            angular.AngularVelocityBody!=Double3.Zero||angular.ConstantBodyTorque!=Double3.Zero||
            !FloridaContactMotion.TryCreate(system,linear,angular,properties,geometry.GetFeature(0).OffsetFromComMetres,out var motion))
        {failure=FloridaContactFailure.MotionDomain;return FloridaContactStatus.Unsupported;}
        var a=PhysicalEventEpoch.FromCanonical(start);var b=PhysicalEventEpoch.FromCanonical(end);
        if(!SpacecraftPhysicalEventObservationEvaluator.Evaluate(view,expectedRevision,a,geometry,geometry.GetFeature(0).Id,system,graph,query,
            expectedAuthority,evaluations,roots,staging,stagingRoots,out var first).Succeeded||
            !SpacecraftPhysicalEventObservationEvaluator.Evaluate(view,expectedRevision,b,geometry,geometry.GetFeature(0).Id,system,graph,query,
            expectedAuthority,evaluations,roots,staging,stagingRoots,out var last).Succeeded)
        {failure=FloridaContactFailure.Observation;return FloridaContactStatus.Unsupported;}
        if(!terrain.IsNumericalFullWeight(first.TerrainDirectionBodyFixed)||!terrain.IsNumericalFullWeight(last.TerrainDirectionBodyFixed)||
            !NumericalError(motion,terrain.GradingRegion,Seconds(start),first.RadialSignedGapMetres,out var firstError)||
            !NumericalError(motion,terrain.GradingRegion,Seconds(end),last.RadialSignedGapMetres,out var lastError))
        {failure=FloridaContactFailure.GradingDomain;return FloridaContactStatus.Unresolved;}
        if(engine.State.Revision!=expectedRevision||engine.ContactProofTimelineRevision!=timeline)
        {failure=FloridaContactFailure.ChangedAuthority;return FloridaContactStatus.Stale;}
        provider=new(engine,linear,angular,properties,geometry,expectedRevision,timeline,terrain,graph,motion,start,end,first,last,firstError,lastError);
        return FloridaContactStatus.Unresolved; // admission is not a certificate
    }

    // An a-posteriori numerical error enclosure, obtained from an INDEPENDENT real-model enclosure.
    // It measures the combined observed evaluation error, including inherited Kepler/FP64 terrain effects;
    // it never supplies a sign, derivative, tolerance or premise to the physical proof.
    private static bool NumericalError(in FloridaContactMotion motion,in FacilitySupportRegion region,FloridaBound t,double observed,out FloridaBound error)
    {
        error=default;if(!motion.Evaluate(t,out var q,out _))return false;
        var p=FloridaVector.Dot(FloridaVector.From(region.Up),q);
        if(p.Lower<=0)return false;
        var physicalGap=(p-((FloridaBound)region.RadiusMetres+region.PlaneAltitudeMetres))*q.Norm/p;
        error=(FloridaBound)observed-physicalGap;return error.IsFinite;
    }
    private static FloridaBound Seconds(SimulationInstant t)=>t.Ticks%SimulationInstant.TicksPerSecond==0
        ?FloridaBound.Integer(t.Ticks/SimulationInstant.TicksPerSecond):FloridaBound.Integer(t.Ticks)/SimulationInstant.TicksPerSecond;
    private FloridaBound Interval=>new(Seconds(start).Lower,Seconds(end).Upper);

    // Pure theorem admission, not a certificate constructor. Also exercised by independent polynomial controls.
    internal static FloridaContactStatus Classify(FloridaBound whole,FloridaBound first,FloridaBound last,FloridaBound slope,
        out FloridaContactFailure failure)
    {
        failure=FloridaContactFailure.None;
        if(!whole.IsFinite||!first.IsFinite||!last.IsFinite||!slope.IsFinite)
        {failure=FloridaContactFailure.NumericalResolution;return FloridaContactStatus.Unresolved;}
        if(whole.Lower>0)return FloridaContactStatus.CertifiedClear;
        if(first.Lower<=0){failure=first.Upper<=0?FloridaContactFailure.InitialContactOrPenetration:FloridaContactFailure.NumericalResolution;return FloridaContactStatus.Unresolved;}
        if(slope.Upper>=0){failure=FloridaContactFailure.NotStrictlyApproaching;return FloridaContactStatus.Unresolved;}
        if(last.Lower>0)return FloridaContactStatus.CertifiedClear;
        if(last.Upper>=0){failure=FloridaContactFailure.NumericalResolution;return FloridaContactStatus.Unresolved;}
        return FloridaContactStatus.CertifiedUniqueApproachingRoot;
    }

    internal bool IsApplicable(SimulationTransactionEngine engine,SpacecraftContactGeometry currentGeometry,
        CelestialSystemDefinition system,ReferenceFrameGraph currentGraph,IPhysicalSurfacePointQuery currentTerrain)
    {
        if(!ReferenceEquals(engineIdentity,engine)||!ReferenceEquals(system,SolAnalyticalDefinition.Instance)||!ReferenceEquals(graph,currentGraph)||
            !ReferenceEquals(terrain,currentTerrain)||currentTerrain.Authority!=authority||currentGeometry.Identity!=geometry.Identity||
            currentGeometry.Spacecraft!=geometry.Spacecraft||currentGeometry.Count!=1||currentGeometry.GetFeature(0)!=geometry.GetFeature(0)||
            engine.State.Revision!=stateRevision||engine.ContactProofTimelineRevision!=timelineRevision||engine.ContactProofCurrentTime>start)return false;
        var state=engine.State;
        return state.Spacecraft.TryGetTranslation(geometry.Spacecraft,out var l,out var p)&&l==linear&&p==properties&&
            state.Spacecraft.TryGetRigidBody(geometry.Spacecraft,out var a)&&a==angular&&!engine.HasContactProofBoundaryThrough(end);
    }

    internal FloridaContactResult Evaluate(SimulationTransactionEngine engine,SpacecraftContactGeometry currentGeometry,
        CelestialSystemDefinition system,ReferenceFrameGraph currentGraph,IPhysicalSurfacePointQuery currentTerrain) =>
        Proof.Evaluate(this,new(engine,currentGeometry,system,currentGraph,currentTerrain));

    /// <summary>Private construction only from the enclosing provider's complete checked proof path.</summary>
    internal readonly partial struct Proof
    {
        private readonly FloridaContactProvider? owner;
        internal FloridaBound RootEnclosure { get; }
        internal FloridaBound Function { get; }
        internal FloridaBound Derivative { get; }
        internal int RefinementCount { get; }
        internal bool IsRoot=>owner is not null&&Function.Lower<=0&&Function.Upper>=0;
        private Proof(FloridaContactProvider owner,FloridaBound enclosure,FloridaBound function,FloridaBound derivative,int count)
        {this.owner=owner;RootEnclosure=enclosure;Function=function;Derivative=derivative;RefinementCount=count;}
        internal static FloridaContactResult Evaluate(FloridaContactProvider owner,in FloridaContactUse current)
        {
            if(!owner.IsApplicable(current.Engine,current.Geometry,current.System,current.Graph,current.Terrain))
                return new(FloridaContactStatus.Stale,FloridaContactFailure.ChangedAuthority);
            var domain=owner.Interval;
            if(!owner.motion.ProveDomain(domain,owner.region,out var f,out var derivative))
                return new(FloridaContactStatus.Unresolved,FloridaContactFailure.GradingDomain);
            owner.motion.ProveDomain(Seconds(owner.start),owner.region,out var first,out _);
            owner.motion.ProveDomain(Seconds(owner.end),owner.region,out var last,out _);
            var status=Classify(f,first,last,derivative,out var failure);
            if(status==FloridaContactStatus.Unresolved)return new(status,failure);
            if(status==FloridaContactStatus.CertifiedClear)return new(status,failure,new(owner,domain,f.Lower>0?f:last,derivative,0));
            // Existence/uniqueness is already proved. No approximated event time is published.
            // Narrower bounds are a separate operation with an explicit unresolved outcome.
            return new(status,failure,new(owner,domain,f,derivative,0));
        }

        internal FloridaContactResult Refine(double maximumWidthSeconds,int budget,in FloridaContactUse current)
        {
            if(owner is null||!IsRoot)return new(FloridaContactStatus.Unsupported,FloridaContactFailure.InvalidProof);
            if(!owner.IsApplicable(current.Engine,current.Geometry,current.System,current.Graph,current.Terrain))
                return new(FloridaContactStatus.Stale,FloridaContactFailure.ChangedAuthority);
            if(!double.IsFinite(maximumWidthSeconds)||maximumWidthSeconds<=0||budget<0||budget>MaximumRefinements)
                return new(FloridaContactStatus.Unsupported,FloridaContactFailure.InvalidProof);
            var bracket=RootEnclosure;var count=0;
            while((new FloridaBound(bracket.Upper,bracket.Upper)-bracket.Lower).Upper>maximumWidthSeconds)
            {
                if(count==budget)return new(FloridaContactStatus.Unresolved,FloridaContactFailure.RefinementBudget);
                if(!TryNarrow(owner,bracket,out bracket))return new(FloridaContactStatus.Unresolved,FloridaContactFailure.NumericalResolution);
                count++;
            }
            return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaContactFailure.None,
                new(owner,bracket,Function,Derivative,RefinementCount+count));
        }
        private static bool TryNarrow(FloridaContactProvider owner,FloridaBound bracket,out FloridaBound narrower)
        {
            narrower=bracket;
            var mid=bracket.Lower+(bracket.Upper-bracket.Lower)*.5;
            if(mid<=bracket.Lower||mid>=bracket.Upper||!owner.motion.ProveDomain(FloridaBound.Point(mid),owner.region,out var sample,out _))return false;
            if(sample.Lower>0)narrower=new(mid,bracket.Upper);
            else if(sample.Upper<0)narrower=new(bracket.Lower,mid);
            else return false;
            return true;
        }
        internal FloridaComparison CompareRational(PhysicalEventEpoch epoch,SimulationTransactionEngine engine,SpacecraftContactGeometry geometry,
            CelestialSystemDefinition system,ReferenceFrameGraph graph,IPhysicalSurfacePointQuery terrain)
        {
            if(owner is null||!IsRoot)return new(FloridaContactStatus.Unsupported,FloridaRootOrder.Unresolved);
            if(!owner.IsApplicable(engine,geometry,system,graph,terrain))return new(FloridaContactStatus.Stale,FloridaRootOrder.Unresolved);
            // Avoid rounding a rational to a binary64 point: divide outward, including its exact fraction.
            var t=(FloridaBound.Integer(epoch.FloorTicks)+FloridaBound.Integer(epoch.Numerator)/FloridaBound.Integer(epoch.Denominator))/SimulationInstant.TicksPerSecond;
            if(RootEnclosure.Upper<t.Lower)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Less);
            if(RootEnclosure.Lower>t.Upper)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Greater);
            if(t.Lower>=owner.Interval.Lower&&t.Upper<=owner.Interval.Upper&&owner.motion.ProveDomain(t,owner.region,out var f,out _))
            {if(f.Lower>0)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Greater);
                if(f.Upper<0)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Less);}
            return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Unresolved);
        }
        internal FloridaComparison CompareRoot(in Proof other,in FloridaContactUse mine,in FloridaContactUse theirs)
        {
            if(owner is null||other.owner is null||!IsRoot||!other.IsRoot)return new(FloridaContactStatus.Unsupported,FloridaRootOrder.Unresolved);
            if(!owner.IsApplicable(mine.Engine,mine.Geometry,mine.System,mine.Graph,mine.Terrain)||
                !other.owner.IsApplicable(theirs.Engine,theirs.Geometry,theirs.System,theirs.Graph,theirs.Terrain))
                return new(FloridaContactStatus.Stale,FloridaRootOrder.Unresolved);
            var same=ReferenceEquals(owner,other.owner)||owner.SameRelationAndSearch(other.owner);
            if(same)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Equal);
            var a=RootEnclosure;var b=other.RootEnclosure;
            for(var i=0;i<=MaximumRefinements;i++)
            {
                if(a.Upper<b.Lower)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Less);
                if(a.Lower>b.Upper)return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Greater);
                if(i==MaximumRefinements)break;
                var changedA=TryNarrow(owner,a,out a);var changedB=TryNarrow(other.owner,b,out b);
                if(!changedA&&!changedB)break;
            }
            return new(FloridaContactStatus.CertifiedUniqueApproachingRoot,FloridaRootOrder.Unresolved);
        }
    }
    private bool SameRelationAndSearch(FloridaContactProvider other)=>linear==other.linear&&angular==other.angular&&properties==other.properties&&
        geometry.Identity==other.geometry.Identity&&geometry.Spacecraft==other.geometry.Spacecraft&&stateRevision==other.stateRevision&&
        timelineRevision==other.timelineRevision&&ReferenceEquals(engineIdentity,other.engineIdentity)&&ReferenceEquals(terrain,other.terrain)&&
        ReferenceEquals(graph,other.graph)&&authority==other.authority&&start==other.start&&end==other.end;
}
