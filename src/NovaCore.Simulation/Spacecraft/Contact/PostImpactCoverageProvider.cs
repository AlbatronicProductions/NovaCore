using NovaCore.Core.Surface;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal readonly struct FloridaPostImpactGap : IPostImpactCoverageGap
{
    private readonly FloridaContactMotion motion;
    private readonly PrivatePostImpactStateValues source;
    private readonly SpacecraftContactFeature feature;
    private readonly FacilitySupportRegion region;
    private readonly FloridaBound initialSlope, continuationSecond;
    private readonly bool hasAnchor;
    private readonly CoverageFailure anchorFailure;
    internal FloridaPostImpactGap(FloridaContactMotion motion, PrivatePostImpactStateValues source,
        SpacecraftContactFeature feature, FacilitySupportRegion region)
    {
        this.motion=motion;this.source=source;this.feature=feature;this.region=region;
        initialSlope=default;continuationSecond=default;hasAnchor=false;anchorFailure=CoverageFailure.None;
        // Checked source admission proves g(alpha)=0 and g'(alpha)>=0. Cache only numerical
        // inclusions of that same trajectory. No nominal alpha or new receipt authority.
        if(motion.PostImpact(source,feature.OffsetFromComMetres,FloridaBound.Point(0),out var initial)!=CoverageFailure.None)return;
        var up=FloridaVector.From(region.Up);
        var slope=FloridaVector.Dot(up,initial.BodyVelocity);
        if(!slope.IsFinite)return;
        if(slope.Upper<0){anchorFailure=CoverageFailure.InvalidSource;return;}
        initialSlope=new(Math.Max(0,slope.Lower),slope.Upper);
        if(motion.PostImpact(source,feature.OffsetFromComMetres,new(0,1),out var whole)!=CoverageFailure.None)return;
        continuationSecond=FloridaVector.Dot(up,whole.BodyAcceleration);
        hasAnchor=continuationSecond.IsFinite;
        // Curvature bounds the globally defined algebraic plane extension. Full-weight terrain
        // equivalence remains mandatory on each actual search leaf below, not on this wide box.
    }
    internal CoverageFailure Trajectory(FloridaBound sigma,out CoverageTrajectory value)=>motion.PostImpact(source,feature.OffsetFromComMetres,sigma,out value);
    public CoverageFailure Enclose(FloridaBound sigma,out CoverageGap gap)
        =>Enclose(sigma,out _,out gap);
    internal CoverageFailure Enclose(FloridaBound sigma,out CoverageTrajectory value,out CoverageGap gap)
    {
        gap=default;
        if(anchorFailure!=CoverageFailure.None){value=default;return anchorFailure;}
        var failure=Trajectory(sigma,out value);
        if(failure!=CoverageFailure.None)return failure;
        if(!FloridaContactMotion.CoverageDomain(value.BodyPosition,region))return CoverageFailure.GradingDomain;
        var up=FloridaVector.From(region.Up);
        gap=new(FloridaVector.Dot(up,value.BodyPosition)-((FloridaBound)region.RadiusMetres+region.PlaneAltitudeMetres),
            FloridaVector.Dot(up,value.BodyVelocity),FloridaVector.Dot(up,value.BodyAcceleration));
        if(hasAnchor)gap=IntersectRootAnchor(gap,value.Elapsed,initialSlope,continuationSecond);
        return gap.IsFinite?CoverageFailure.None:CoverageFailure.NonFinite;
    }

    // Integral Taylor identity for the SAME function with g(alpha)=0:
    // g(alpha+s) in s*(g'(alpha)+s*A/2), g'(alpha+s) in g'(alpha)+s*A.
    // A encloses g'' on the entire continuation. Intersection cannot enlarge a raw bound.
    internal static CoverageGap IntersectRootAnchor(CoverageGap raw,FloridaBound elapsed,FloridaBound slope,FloridaBound second)
    {
        static FloridaBound Intersection(FloridaBound a,FloridaBound b)=>new(Math.Max(a.Lower,b.Lower),Math.Min(a.Upper,b.Upper));
        return new(Intersection(raw.Value,elapsed*(slope+elapsed*second/2)),
            Intersection(raw.First,slope+elapsed*second),raw.Second);
    }
}

internal readonly record struct CoverageValues(CoverageSearchResult Proof, ContactFeatureIdentity Feature,
    PhysicalSurfaceAuthorityIdentity Terrain, SimulationInstant Target, uint Version);

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        /// <summary>
        /// Separate coverage owner for this EXACT checked singleton trajectory. No execution/publication.
        /// The only factory checks opaque banked receipts; numerical snapshots cannot mint this owner.
        /// </summary>
        internal sealed class PostImpactCoverageProvider
        {
            private readonly Proof root;
            private readonly PrivatePostImpactState initial;
            private readonly PostImpactVelocity realization;
            private readonly PrivatePostImpactPoseRequest pose;
            private readonly PrivateCanonicalState staged;
            private readonly PrivateCanonicalStateValues captured;
            private readonly FloridaPostImpactGap gap;
            private readonly ContactFeatureIdentity feature;

            private PostImpactCoverageProvider(Proof root,PrivatePostImpactState initial,PostImpactVelocity realization,
                PrivatePostImpactPoseRequest pose,PrivateCanonicalState staged,PrivateCanonicalStateValues captured)
            {
                this.root=root;this.initial=initial;this.realization=realization;this.pose=pose;this.staged=staged;this.captured=captured;
                var source=root.owner!;
                feature=new(source.geometry.Spacecraft,source.geometry.Identity,source.geometry.GetFeature(0).Id);
                gap=new(source.motion,captured.Initial,source.geometry.GetFeature(0),source.region);
            }
            internal static PrivatePropagationStatus TryCreate(in PrivateCanonicalState staged,in Proof root,
                in PrivatePostImpactState initial,in PostImpactVelocity realization,in PrivatePostImpactPoseRequest pose,
                SimulationInstant target,in PrivatePropagationRequest request,in FloridaContactUse current,
                out PostImpactCoverageProvider? provider)
            {
                provider=null;
                var status=staged.Read(root,initial,realization,pose,target,request,current,out var values);
                if(status!=PrivatePropagationStatus.Ready)return status;
                if(values.EventCoverage!=PrivatePropagationCoverage.Unknown || values.ArithmeticVersion!=PrivatePropagationMath.Version ||
                    !root.IsRoot || root.owner is null || root.owner.geometry.Count!=1)return PrivatePropagationStatus.Unsupported;
                provider=new(root,initial,realization,pose,staged,values);
                return PrivatePropagationStatus.Ready;
            }
            private PrivatePropagationStatus Check(in PrivateCanonicalState expected,in FloridaContactUse current)
            {
                var s=staged.Read(root,initial,realization,pose,captured.Target,captured.Request,current,out var held);
                if(s!=PrivatePropagationStatus.Ready)return s;
                s=expected.Read(root,initial,realization,pose,captured.Target,captured.Request,current,out var supplied);
                if(s!=PrivatePropagationStatus.Ready)return s;
                // Read already checks the complete initial tuple without Proof boxing. Compare endpoint
                // and request/version separately: no valid coverage A may be paired with staged B.
                if(held.Endpoint!=captured.Endpoint || supplied.Endpoint!=captured.Endpoint ||
                    supplied.RemainderSeconds!=captured.RemainderSeconds || supplied.EventCoverage!=PrivatePropagationCoverage.Unknown ||
                    supplied.ArithmeticVersion!=captured.ArithmeticVersion)return PrivatePropagationStatus.Unsupported;
                return CertifiedPostImpactVelocityMath.ArithmeticSupported()?PrivatePropagationStatus.Ready:PrivatePropagationStatus.Unresolved;
            }
            private static CoverageStatus Map(PrivatePropagationStatus s)=>s==PrivatePropagationStatus.Stale?CoverageStatus.Stale:
                s==PrivatePropagationStatus.Unresolved?CoverageStatus.Unresolved:CoverageStatus.Unsupported;

            internal CoverageStatus Evaluate(in PrivateCanonicalState expected,in FloridaContactUse current,
                out CoverageEvidence evidence,out CoverageSearchResult diagnostic)
                =>CoverageEvidence.Prepare(this,expected,current,out evidence,out diagnostic);

            internal PrivatePropagationStatus Enclose(in PrivateCanonicalState expected,in FloridaContactUse current,FloridaBound sigma,
                out CoverageTrajectory trajectory,out CoverageGap residual,out CoverageFailure failure)
            {
                trajectory=default;residual=default;failure=CoverageFailure.None;
                var status=Check(expected,current);
                if(status!=PrivatePropagationStatus.Ready)return status;
                failure=gap.Enclose(sigma,out trajectory,out residual);
                return failure==CoverageFailure.None?PrivatePropagationStatus.Ready:PrivatePropagationStatus.Unresolved;
                // This numerical read grants no coverage receipt; its success concerns only inclusion.
            }

            internal readonly struct CoverageEvidence
            {
                private readonly PostImpactCoverageProvider? owner;
                private readonly CoverageSearchResult proof;
                private CoverageEvidence(PostImpactCoverageProvider owner,CoverageSearchResult proof){this.owner=owner;this.proof=proof;}
                internal static CoverageStatus Prepare(PostImpactCoverageProvider owner,in PrivateCanonicalState expected,
                    in FloridaContactUse current,out CoverageEvidence evidence,out CoverageSearchResult diagnostic)
                {
                    evidence=default;
                    var checkedStatus=owner.Check(expected,current);
                    if(checkedStatus!=PrivatePropagationStatus.Ready)
                    {
                        var status=Map(checkedStatus);
                        diagnostic=new(status,status==CoverageStatus.Stale?CoverageFailure.ChangedAuthority:
                            status==CoverageStatus.Unresolved?CoverageFailure.UnsupportedArithmetic:CoverageFailure.InvalidSource,0,0,0);
                        return status;
                    }
                    diagnostic=PostImpactCoverageSearch.Evaluate(owner.gap);
                    if(diagnostic.Status == CoverageStatus.EventFreeThroughTarget)
                        evidence=new(owner,diagnostic);
                    return diagnostic.Status;
                }
                internal CoverageStatus Read(in PrivateCanonicalState expected,in FloridaContactUse current,
                    out CoverageValues values)
                {
                    values=default;
                    if(owner is null)return CoverageStatus.Unsupported;
                    var s=owner.Check(expected,current);if(s!=PrivatePropagationStatus.Ready)return Map(s);
                    values=new(proof,owner.feature,owner.captured.Initial.TerrainAuthority,owner.captured.Target,PostImpactCoverageSearch.Version);
                    return proof.Status;
                }
            }

        }
    }
}
