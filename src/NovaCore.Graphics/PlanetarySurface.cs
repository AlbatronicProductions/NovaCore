using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Stable cube-face order used by all procedural surface work.</summary>
public enum CubeSphereFace:byte { PositiveX,NegativeX,PositiveY,NegativeY,PositiveZ,NegativeZ }
public enum PlanetaryRepresentation:byte { FarFieldBody,NearFieldSurface }

[Flags]
public enum PlanetaryPatchEdge:uint { None=0,NegativeU=1,PositiveU=2,NegativeV=4,PositiveV=8 }

/// <summary>Explicit face-edge transition; Reversed applies to the coordinate running along the edge.</summary>
public readonly record struct CubeSphereEdgeTransition(CubeSphereFace NeighborFace,PlanetaryPatchEdge NeighborEdge,bool Reversed);

/// <summary>Lightweight deterministic quadtree identity; x/y address a face grid at Level.</summary>
public readonly record struct PlanetaryPatch(CubeSphereFace Face,int Level,int X,int Y):IComparable<PlanetaryPatch>
{
    public PlanetaryPatch? Parent=>Level==0?null:new(Face,Level-1,X>>1,Y>>1);
    public PlanetaryPatch Child(int index)=>index is >=0 and <4?new(Face,Level+1,(X<<1)+(index&1),(Y<<1)+(index>>1)):throw new ArgumentOutOfRangeException(nameof(index));
    public (double MinX,double MinY,double MaxX,double MaxY) Bounds{get{var scale=Math.ScaleB(1d,-Level);return(X*scale,Y*scale,(X+1)*scale,(Y+1)*scale);}}
    /// <summary>Maps a local grid vertex through the patch's exact dyadic integer lattice.</summary>
    public (double U,double V) GridCoordinate(int gridX,int gridY,int gridResolution=PlanetaryTerrainDefinition.GridResolution)
    {
        if(Level is <0 or >24||gridResolution<=0||gridX is <0||gridY is <0||gridX>gridResolution||gridY>gridResolution)throw new ArgumentOutOfRangeException();
        var denominator=(long)gridResolution<<Level;
        return(((long)X*gridResolution+gridX)/(double)denominator,((long)Y*gridResolution+gridY)/(double)denominator);
    }
    /// <summary>Maps a parent grid vertex to the identical vertex in one selected child quadrant.</summary>
    public static bool TryMapGridVertexToChild(int childIndex,int parentGridX,int parentGridY,out int childGridX,out int childGridY,int gridResolution=PlanetaryTerrainDefinition.GridResolution)
    {
        childGridX=childGridY=0;
        if(childIndex is <0 or >3||gridResolution<=0||(gridResolution&1)!=0||parentGridX is <0||parentGridY is <0||parentGridX>gridResolution||parentGridY>gridResolution)throw new ArgumentOutOfRangeException();
        var half=gridResolution/2;var quadrantX=childIndex&1;var quadrantY=childIndex>>1;
        if(parentGridX<quadrantX*half||parentGridX>(quadrantX+1)*half||parentGridY<quadrantY*half||parentGridY>(quadrantY+1)*half)return false;
        childGridX=(parentGridX-quadrantX*half)*2;childGridY=(parentGridY-quadrantY*half)*2;return true;
    }
    public int CompareTo(PlanetaryPatch other){var face=Face.CompareTo(other.Face);if(face!=0)return face;var level=Level.CompareTo(other.Level);if(level!=0)return level;var x=X.CompareTo(other.X);return x!=0?x:Y.CompareTo(other.Y);}
}

public readonly record struct PlanetaryLodConfiguration
{
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel):this(nearFieldAltitudeRadii,maximumLevel,.11d) { }
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel,double maximumProjectedPatchSpan)
        :this(nearFieldAltitudeRadii,maximumLevel,maximumProjectedPatchSpan,0d,0d,0d,false) { }
    private PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel,double maximumProjectedPatchSpan,double maximumTerrainHeightMetres,double targetPatchPixels,double viewportHeightPixels,bool relaxedCubeProjection)
    { NearFieldAltitudeRadii=nearFieldAltitudeRadii;MaximumLevel=maximumLevel;MaximumProjectedPatchSpan=maximumProjectedPatchSpan;MaximumTerrainHeightMetres=maximumTerrainHeightMetres;TargetPatchPixels=targetPatchPixels;ViewportHeightPixels=viewportHeightPixels;RelaxedCubeProjection=relaxedCubeProjection; }
    public double NearFieldAltitudeRadii { get; }
    public int MaximumLevel { get; }
    public double MaximumProjectedPatchSpan { get; }
    public double MaximumTerrainHeightMetres { get; }
    public double TargetPatchPixels { get; }
    public double ViewportHeightPixels { get; }
    public bool RelaxedCubeProjection { get; }
    public bool IsValid=>double.IsFinite(NearFieldAltitudeRadii)&&NearFieldAltitudeRadii>0&&MaximumLevel is >=0 and <=24&&double.IsFinite(MaximumProjectedPatchSpan)&&MaximumProjectedPatchSpan>0&&double.IsFinite(MaximumTerrainHeightMetres)&&MaximumTerrainHeightMetres>=0;

    /// <summary>Converts a patch pixel budget into the selector's conservative angular-span metric.</summary>
    public static PlanetaryLodConfiguration ForViewport(double nearFieldAltitudeRadii,int maximumLevel,double targetPatchPixels,double viewportHeightPixels,double verticalFieldOfViewRadians,double maximumTerrainHeightMetres=0d,bool relaxedCubeProjection=false)
    {
        if(!double.IsFinite(targetPatchPixels)||targetPatchPixels<=0||!double.IsFinite(viewportHeightPixels)||viewportHeightPixels<=0||!double.IsFinite(verticalFieldOfViewRadians)||verticalFieldOfViewRadians<=0||verticalFieldOfViewRadians>=Math.PI)throw new ArgumentOutOfRangeException();
        var span=2d*targetPatchPixels*Math.Tan(verticalFieldOfViewRadians*.5d)/viewportHeightPixels;
        return new(nearFieldAltitudeRadii,maximumLevel,span,maximumTerrainHeightMetres,targetPatchPixels,viewportHeightPixels,relaxedCubeProjection);
    }
}

public readonly record struct PlanetaryLodSelection(
    PlanetaryRepresentation Representation,
    int MaximumLevel,
    PlanetaryPatch[] Patches,
    PlanetaryPatchEdge[] StitchMasks,
    int RefinementCount,
    int BalancedRefinementCount,
    int CulledPatchCount,
    int FrustumCulledPatchCount,
    int HorizonCulledPatchCount,
    int SplitPatchCount,
    int MergedPatchCount,
    int ParentFallbackCount,
    int PendingChildCount);

/// <summary>
/// Conservative body-fixed sphere for one curved patch over the complete
/// physical-height envelope.  Selection and native submission share this
/// bound so a patch cannot be culled with weaker geometry than is consumed by
/// the renderer.
/// </summary>
public readonly record struct PlanetaryPatchConservativeBounds(Double3 Center,double Radius)
{
    public bool IsValid=>Center.IsFinite&&double.IsFinite(Radius)&&Radius>=0d;
    public double DistanceTo(in Double3 point)=>Math.Sqrt((point-Center).LengthSquared);
    public bool IntersectsSphere(in Double3 center,double radius)
    {
        if(!double.IsFinite(radius)||radius<0d)throw new ArgumentOutOfRangeException(nameof(radius));
        var sum=Radius+radius;return(center-Center).LengthSquared<=sum*sum;
    }
}

/// <summary>
/// Conservative swept bound for the complete radial displacement interval.
/// Keeping the inner and outer curved bounds separate avoids turning a tall,
/// tiny patch into a many-kilometre center sphere during near-eye selection.
/// </summary>
public readonly record struct PlanetaryPatchDisplacedBounds(
    PlanetaryPatchConservativeBounds Inner,PlanetaryPatchConservativeBounds Outer)
{
    public bool IsValid=>Inner.IsValid&&Outer.IsValid;
    public bool IntersectsSphere(in Double3 center,double radius)
    {
        if(!double.IsFinite(radius)||radius<0d)throw new ArgumentOutOfRangeException(nameof(radius));
        var axis=Outer.Center-Inner.Center;var lengthSquared=axis.LengthSquared;
        var amount=lengthSquared<=1e-24d?0d:Math.Clamp(Double3.Dot(center-Inner.Center,axis)/lengthSquared,0d,1d);
        var closest=Inner.Center+axis*amount;var envelopeRadius=Math.Max(Inner.Radius,Outer.Radius)+radius;
        return(center-closest).LengthSquared<=envelopeRadius*envelopeRadius;
    }
}

/// <summary>Integer-only adjacency for the normalized cube projection, including oriented cross-face edges.</summary>
public static class CubeSphereAdjacency
{
    public static CubeSphereEdgeTransition GetTransition(CubeSphereFace face,PlanetaryPatchEdge edge)=> (face,edge) switch
    {
        (CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveU,false),
        (CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeU,false),
        (CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveU,true),
        (CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveU,false),
        (CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveU,false),
        (CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeU,false),
        (CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeU,false),
        (CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeU,true),
        (CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveV,true),
        (CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveV,false),
        (CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveV,false),
        (CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveV,true),
        (CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeV,false),
        (CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeV,true),
        (CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeV,true),
        (CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeV,false),
        (CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveU,false),
        (CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeU,false),
        (CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveV,false),
        (CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeV,false),
        (CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeU)=>new(CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveU,false),
        (CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveU)=>new(CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeU,false),
        (CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeV)=>new(CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeV,true),
        (CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveV)=>new(CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveV,true),
        _=>throw new ArgumentOutOfRangeException(nameof(edge))
    };

    public static PlanetaryPatch NeighborAtSameLevel(in PlanetaryPatch patch,PlanetaryPatchEdge edge)
    {
        var size=1<<patch.Level;
        if(patch.Level<0||patch.X<0||patch.Y<0||patch.X>=size||patch.Y>=size)throw new ArgumentOutOfRangeException(nameof(patch));
        if(edge==PlanetaryPatchEdge.NegativeU&&patch.X>0)return patch with{X=patch.X-1};
        if(edge==PlanetaryPatchEdge.PositiveU&&patch.X+1<size)return patch with{X=patch.X+1};
        if(edge==PlanetaryPatchEdge.NegativeV&&patch.Y>0)return patch with{Y=patch.Y-1};
        if(edge==PlanetaryPatchEdge.PositiveV&&patch.Y+1<size)return patch with{Y=patch.Y+1};
        var transition=GetTransition(patch.Face,edge);var along=edge is PlanetaryPatchEdge.NegativeU or PlanetaryPatchEdge.PositiveU?patch.Y:patch.X;
        if(transition.Reversed)along=size-1-along;
        return transition.NeighborEdge switch
        {
            PlanetaryPatchEdge.NegativeU=>new(transition.NeighborFace,patch.Level,0,along),
            PlanetaryPatchEdge.PositiveU=>new(transition.NeighborFace,patch.Level,size-1,along),
            PlanetaryPatchEdge.NegativeV=>new(transition.NeighborFace,patch.Level,along,0),
            PlanetaryPatchEdge.PositiveV=>new(transition.NeighborFace,patch.Level,along,size-1),
            _=>throw new InvalidOperationException()
        };
    }
}

/// <summary>Presentation-only deterministic adaptive quadtree policy.</summary>
public static class PlanetaryRepresentationSelector
{
    private static readonly PlanetaryPatchEdge[] Edges=[PlanetaryPatchEdge.NegativeU,PlanetaryPatchEdge.PositiveU,PlanetaryPatchEdge.NegativeV,PlanetaryPatchEdge.PositiveV];

    public static PlanetaryRepresentation Select(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration)
    {
        if(!configuration.IsValid)throw new ArgumentOutOfRangeException(nameof(configuration));var altitude=Math.Sqrt((cameraRootPosition-body.Position.Value).LengthSquared)-body.RadiusMetres;return altitude/body.RadiusMetres<=configuration.NearFieldAltitudeRadii?PlanetaryRepresentation.NearFieldSurface:PlanetaryRepresentation.FarFieldBody;
    }

    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration)
        =>SelectPatches(body,cameraRootPosition,configuration,Double3.Zero,0d,0d);

    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double verticalFieldOfViewRadians,double aspectRatio)
        =>SelectPatches(body,cameraRootPosition,configuration,viewForward,verticalFieldOfViewRadians,aspectRatio,Math.Max(0d,Math.Sqrt((cameraRootPosition-body.Position.Value).LengthSquared)-body.RadiusMetres));

    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double verticalFieldOfViewRadians,double aspectRatio,double surfaceAltitudeMetres)
        =>SelectPatches(body,cameraRootPosition,configuration,viewForward,verticalFieldOfViewRadians,aspectRatio,surfaceAltitudeMetres,ReadOnlySpan<PlanetaryPatch>.Empty);

    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double verticalFieldOfViewRadians,double aspectRatio,double surfaceAltitudeMetres,ReadOnlySpan<PlanetaryPatch> previousLeaves)
        =>SelectPatches(body,cameraRootPosition,configuration,viewForward,verticalFieldOfViewRadians,
            aspectRatio,surfaceAltitudeMetres,.05d,previousLeaves);

    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double verticalFieldOfViewRadians,double aspectRatio,double surfaceAltitudeMetres,double nearClipMetres,ReadOnlySpan<PlanetaryPatch> previousLeaves)
        =>SelectPatchesCore(body,cameraRootPosition,configuration,viewForward,verticalFieldOfViewRadians,
            aspectRatio,surfaceAltitudeMetres,nearClipMetres,double.PositiveInfinity,previousLeaves);

    /// <summary>
    /// Selects one orientation-independent, body-fixed neighborhood around the
    /// viewer.  The global hierarchy remains the coarse owner outside this
    /// bounded region; GPU culling decides which retained primitives are visible.
    /// </summary>
    public static PlanetaryLodSelection SelectRetainedNeighborhood(in PlanetRenderProxy body,
        in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,
        double surfaceAltitudeMetres,double nearClipMetres,double neighborhoodRadiusMetres,
        ReadOnlySpan<PlanetaryPatch> previousLeaves)
    {
        if(!double.IsFinite(neighborhoodRadiusMetres)||neighborhoodRadiusMetres<=nearClipMetres)
            throw new ArgumentOutOfRangeException(nameof(neighborhoodRadiusMetres));
        return SelectPatchesCore(body,cameraRootPosition,configuration,Double3.Zero,0d,0d,
            surfaceAltitudeMetres,nearClipMetres,neighborhoodRadiusMetres,previousLeaves);
    }

    private static PlanetaryLodSelection SelectPatchesCore(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double verticalFieldOfViewRadians,double aspectRatio,double surfaceAltitudeMetres,double nearClipMetres,double retainedNeighborhoodRadiusMetres,ReadOnlySpan<PlanetaryPatch> previousLeaves)
    {
        if(!double.IsFinite(surfaceAltitudeMetres)||surfaceAltitudeMetres<0||!double.IsFinite(nearClipMetres)||nearClipMetres<=0)throw new ArgumentOutOfRangeException();
        var useFrustum=viewForward.IsFinite&&viewForward.LengthSquared>0&&double.IsFinite(verticalFieldOfViewRadians)&&verticalFieldOfViewRadians>0&&verticalFieldOfViewRadians<Math.PI&&double.IsFinite(aspectRatio)&&aspectRatio>0;var normalizedForward=useFrustum?viewForward.Normalized():Double3.Zero;var halfViewAngle=useFrustum?Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(verticalFieldOfViewRadians*.5d)*aspectRatio,2d)+Math.Pow(Math.Tan(verticalFieldOfViewRadians*.5d),2d))):0d;
        var representation=Select(body,cameraRootPosition,configuration);if(representation==PlanetaryRepresentation.FarFieldBody)return new(representation,0,[],[],0,0,0,0,0,0,0,0,0);
        var previousExact=previousLeaves.IsEmpty?null:new HashSet<PlanetaryPatch>(previousLeaves.Length);var previousRefined=previousLeaves.IsEmpty?null:new HashSet<PlanetaryPatch>();
        if(previousExact is not null&&previousRefined is not null)foreach(ref readonly var leaf in previousLeaves){previousExact.Add(leaf);var parent=leaf.Parent;while(parent is { } value){previousRefined.Add(value);parent=value.Parent;}}
        var cameraRelative=cameraRootPosition-body.Position.Value;var selectionSurfaceRadius=configuration.MaximumTerrainHeightMetres>0?Math.Sqrt(cameraRelative.LengthSquared)-surfaceAltitudeMetres:body.RadiusMetres;var active=new HashSet<PlanetaryPatch>();var refined=0;var frustumCulled=0;var horizonCulled=0;var splits=0;var merges=0;
        if(!RelaxedCubeSphereProjection.TryAddress(cameraRelative,out var nadirFace,out var nadirU,out var nadirV))throw new InvalidOperationException("Planetary nadir address is unavailable.");
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())Traverse(new(face,0,0,0),cameraRelative,body.RadiusMetres,selectionSurfaceRadius,surfaceAltitudeMetres,nearClipMetres,retainedNeighborhoodRadiusMetres,configuration,normalizedForward,halfViewAngle,useFrustum,nadirFace,nadirU,nadirV,previousExact,previousRefined,active,ref refined,ref frustumCulled,ref horizonCulled,ref splits,ref merges);
        var balanced=Balance(active,configuration.MaximumLevel);var ordered=Order(active);var masks=ComputeStitchMasks(ordered,active);
        var maximum=ordered.Length==0?0:ordered.Max(patch=>patch.Level);return new(representation,maximum,ordered,masks,refined,balanced,frustumCulled+horizonCulled,frustumCulled,horizonCulled,splits,merges,0,0);
    }

    /// <summary>
    /// Computes the production stitch template for an already balanced canonical
    /// leaf set.  The fine patch owns stitching in its own local edge frame;
    /// cube-face rotation is used only to locate the covering coarse neighbor.
    /// </summary>
    public static PlanetaryPatchEdge[] ComputeStitchMasks(ReadOnlySpan<PlanetaryPatch> ordered,IReadOnlySet<PlanetaryPatch> active)
    {
        ArgumentNullException.ThrowIfNull(active);var masks=new PlanetaryPatchEdge[ordered.Length];
        for(var index=0;index<ordered.Length;index++)
        {
            foreach(var edge in Edges)
            {
                var neighbor=FindCoveringNeighbor(ordered[index],edge,active);
                if(neighbor is { } candidate&&candidate.Level+1==ordered[index].Level)masks[index]|=edge;
                else if(neighbor is { } invalid&&invalid.Level+1<ordered[index].Level)throw new InvalidOperationException("Unbalanced planetary patch selection.");
            }
        }
        return masks;
    }

    public static PlanetaryPatch? FindCoveringNeighbor(in PlanetaryPatch patch,PlanetaryPatchEdge edge,IReadOnlySet<PlanetaryPatch> active)
    {
        PlanetaryPatch? candidate=CubeSphereAdjacency.NeighborAtSameLevel(patch,edge);
        while(candidate is { } value){if(active.Contains(value))return value;candidate=value.Parent;}
        return null;
    }

    public static PlanetaryPatch[] BalancePatches(IEnumerable<PlanetaryPatch> leaves,int maximumLevel,out int refinementCount)
    {
        ArgumentNullException.ThrowIfNull(leaves);if(maximumLevel is <0 or >24)throw new ArgumentOutOfRangeException(nameof(maximumLevel));var active=leaves.ToHashSet();
        if(active.Any(patch=>patch.Level<0||patch.Level>maximumLevel||patch.X<0||patch.Y<0||patch.X>=(1<<patch.Level)||patch.Y>=(1<<patch.Level)))throw new ArgumentOutOfRangeException(nameof(leaves));
        refinementCount=Balance(active,maximumLevel);return Order(active);
    }

    private static void Traverse(PlanetaryPatch patch,in Double3 cameraRelative,double radius,double selectionSurfaceRadius,double surfaceAltitudeMetres,double nearClipMetres,double retainedNeighborhoodRadiusMetres,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double halfViewAngle,bool useFrustum,CubeSphereFace nadirFace,double nadirU,double nadirV,HashSet<PlanetaryPatch>? previousExact,HashSet<PlanetaryPatch>? previousRefined,HashSet<PlanetaryPatch> active,ref int refined,ref int frustumCulled,ref int horizonCulled,ref int splits,ref int merges)
    {
        var containsNadir=ContainsAddress(patch,nadirFace,nadirU,nadirV);
        var conservative=DisplacedBounds(patch,radius,
            configuration.MaximumTerrainHeightMetres,configuration.RelaxedCubeProjection);
        // A curved patch whose complete physical envelope reaches the eye/near
        // sphere is never an off-screen owner.  It must refine before culling;
        // otherwise one coarse triangle can straddle the camera plane and be
        // magnified into a screen-filling planar sheet.
        // The canonical column containing the camera radial is the only patch
        // that can surround the eye while being entirely outside the forward
        // frustum.  Refine that column deterministically; do not interpret the
        // planet-wide height envelope as a request to refine a many-kilometre
        // disk around the camera.
        var eyeRelevant=containsNadir||conservative.IntersectsSphere(cameraRelative,nearClipMetres);
        if(patch.Level>0&&!containsNadir&&double.IsFinite(retainedNeighborhoodRadiusMetres)&&
            !conservative.IntersectsSphere(cameraRelative,retainedNeighborhoodRadiusMetres))
        {
            frustumCulled++;
            return;
        }
        if(patch.Level>0&&!containsNadir&&!eyeRelevant&&IsHorizonCulled(conservative,cameraRelative,radius,
            configuration.MaximumTerrainHeightMetres)){horizonCulled++;return;}
        if(patch.Level>0&&useFrustum&&!eyeRelevant&&IsViewCulled(conservative,cameraRelative,viewForward,halfViewAngle,nearClipMetres))
        {
            frustumCulled++;
            return;
        }
        var wasLeaf=previousExact?.Contains(patch)==true;var wasRefined=previousRefined?.Contains(patch)==true;var threshold=configuration.MaximumProjectedPatchSpan*(wasLeaf ? 1.12d : wasRefined ? .88d : 1d);var projectedSpan=ProjectedSpan(patch,cameraRelative,configuration.MaximumTerrainHeightMetres>0?selectionSurfaceRadius:radius,configuration.RelaxedCubeProjection);
        if(patch.Level==configuration.MaximumLevel||(!eyeRelevant&&projectedSpan<=threshold)){active.Add(patch);if(wasRefined)merges++;return;}
        refined++;if(wasLeaf)splits++;for(var child=0;child<4;child++)Traverse(patch.Child(child),cameraRelative,radius,selectionSurfaceRadius,surfaceAltitudeMetres,nearClipMetres,retainedNeighborhoodRadiusMetres,configuration,viewForward,halfViewAngle,useFrustum,nadirFace,nadirU,nadirV,previousExact,previousRefined,active,ref refined,ref frustumCulled,ref horizonCulled,ref splits,ref merges);
    }

    public static PlanetaryPatchConservativeBounds ConservativeBounds(in PlanetaryPatch patch,
        double bodyRadius,double maximumTerrainHeightMetres,bool relaxed=true)
    {
        if(!double.IsFinite(bodyRadius)||bodyRadius<=0d||!double.IsFinite(maximumTerrainHeightMetres)||maximumTerrainHeightMetres<0d)throw new ArgumentOutOfRangeException();
        var b=patch.Bounds;var face=patch.Face;var middleRadius=bodyRadius+maximumTerrainHeightMetres*.5d;
        var outerRadius=bodyRadius+maximumTerrainHeightMetres;
        var centerDirection=Project(face,(b.MinX+b.MaxX)*.5d,(b.MinY+b.MaxY)*.5d,1d,relaxed);
        var center=centerDirection*middleRadius;var radius=maximumTerrainHeightMetres*.5d;
        // Corners plus edge midpoints capture the chart's directional extrema.
        // The O(cell^2) curvature guard bounds the unsampled smooth arc between
        // those points and deliberately becomes conservative at coarse levels.
        Expand(b.MinX,b.MinY);Expand(b.MaxX,b.MinY);Expand(b.MinX,b.MaxY);Expand(b.MaxX,b.MaxY);
        var middleU=(b.MinX+b.MaxX)*.5d;var middleV=(b.MinY+b.MaxY)*.5d;
        Expand(middleU,b.MinY);Expand(middleU,b.MaxY);Expand(b.MinX,middleV);Expand(b.MaxX,middleV);
        var cellSpan=Math.ScaleB(1d,-patch.Level);
        radius+=outerRadius*8d*cellSpan*cellSpan;
        var result=new PlanetaryPatchConservativeBounds(center,radius);
        if(!result.IsValid)throw new InvalidOperationException("Invalid conservative planetary patch bound.");
        return result;

        void Expand(double u,double v)
        {
            var point=Project(face,u,v,outerRadius,relaxed);
            radius=Math.Max(radius,Math.Sqrt((point-center).LengthSquared));
        }
    }

    public static PlanetaryPatchDisplacedBounds DisplacedBounds(in PlanetaryPatch patch,
        double bodyRadius,double maximumTerrainHeightMetres,bool relaxed=true)
    {
        if(!double.IsFinite(bodyRadius)||bodyRadius<=0d||!double.IsFinite(maximumTerrainHeightMetres)||maximumTerrainHeightMetres<0d)
            throw new ArgumentOutOfRangeException();
        var result=new PlanetaryPatchDisplacedBounds(
            ConservativeBounds(patch,bodyRadius,0d,relaxed),
            ConservativeBounds(patch,bodyRadius+maximumTerrainHeightMetres,0d,relaxed));
        if(!result.IsValid)throw new InvalidOperationException("Invalid displaced planetary patch bound.");
        return result;
    }

    private static bool ContainsAddress(in PlanetaryPatch patch,CubeSphereFace face,double u,double v)
    {
        if(patch.Face!=face)return false;
        var cells=1<<patch.Level;
        var x=Math.Min((int)Math.Floor(u*cells),cells-1);
        var y=Math.Min((int)Math.Floor(v*cells),cells-1);
        return patch.X==x&&patch.Y==y;
    }

    private static double ProjectedSpan(in PlanetaryPatch patch,in Double3 cameraRelative,double radius,bool relaxed)
    {
        var bounds=patch.Bounds;var center=Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,radius,relaxed);var halfSpan=0d;
        halfSpan=Math.Max(halfSpan,Distance(center,Project(patch.Face,bounds.MinX,bounds.MinY,radius,relaxed)));
        halfSpan=Math.Max(halfSpan,Distance(center,Project(patch.Face,bounds.MaxX,bounds.MinY,radius,relaxed)));
        halfSpan=Math.Max(halfSpan,Distance(center,Project(patch.Face,bounds.MinX,bounds.MaxY,radius,relaxed)));
        halfSpan=Math.Max(halfSpan,Distance(center,Project(patch.Face,bounds.MaxX,bounds.MaxY,radius,relaxed)));
        var distanceToPatch=Math.Max(Distance(cameraRelative,center),radius*1e-12d);return 2d*halfSpan/distanceToPatch;
    }

    private static bool IsHorizonCulled(in PlanetaryPatchDisplacedBounds displaced,in Double3 cameraRelative,double radius,double maximumTerrainHeightMetres)
    {
        var bounds=displaced.Outer;
        var cameraDistance=Math.Sqrt(cameraRelative.LengthSquared);if(cameraDistance<=radius)return false;
        var centerDistance=Math.Sqrt(bounds.Center.LengthSquared);if(centerDistance<=bounds.Radius)return false;
        var angularRadius=Math.Asin(Math.Clamp(bounds.Radius/centerDistance,0d,1d));
        var centerAngle=Angle(bounds.Center,cameraRelative);var horizon=Math.Acos(Math.Clamp(radius/cameraDistance,-1d,1d));
        return centerAngle-angularRadius>horizon;
    }

    private static bool IsViewCulled(in PlanetaryPatchDisplacedBounds bounds,in Double3 cameraRelative,in Double3 viewForward,double halfViewAngle,double nearClipMetres)
    {
        var a=bounds.Inner.Center-cameraRelative;var b=bounds.Outer.Center-cameraRelative;var axis=b-a;
        var axisLengthSquared=axis.LengthSquared;var closestAmount=axisLengthSquared<=1e-24d?0d:
            Math.Clamp(-Double3.Dot(a,axis)/axisLengthSquared,0d,1d);
        var minimumDistance=Math.Sqrt((a+axis*closestAmount).LengthSquared);
        var clipAwareRadius=Math.Max(bounds.Inner.Radius,bounds.Outer.Radius)+nearClipMetres;
        if(minimumDistance<=clipAwareRadius)return false;
        var q=a.LengthSquared;var r=Double3.Dot(a,axis);var s=axisLengthSquared;
        var c=Double3.Dot(a,viewForward);var d=Double3.Dot(axis,viewForward);
        var denominator=d*r-c*s;var alignmentAmount=Math.Abs(denominator)<=1e-24d?0d:
            Math.Clamp((c*r-d*q)/denominator,0d,1d);
        var minimumCenterAngle=Math.Min(Angle(a,viewForward),Math.Min(Angle(b,viewForward),
            Angle(a+axis*alignmentAmount,viewForward)));
        var angularRadius=Math.Asin(Math.Clamp(clipAwareRadius/minimumDistance,0d,1d));
        return minimumCenterAngle-angularRadius>halfViewAngle;
    }

    private static int Balance(HashSet<PlanetaryPatch> active,int maximumLevel)
    {
        var count=0;
        while(true)
        {
            PlanetaryPatch? coarse=null;
            foreach(var patch in Order(active))
            {
                foreach(var edge in Edges)
                {
                    var neighbor=FindCoveringNeighbor(patch,edge,active);
                    if(neighbor is { } candidate&&patch.Level-candidate.Level>1){coarse=candidate;break;}
                }
                if(coarse is not null)break;
            }
            if(coarse is not { } parent)return count;
            active.Remove(parent);for(var child=0;child<4;child++)active.Add(parent.Child(child));count++;
        }
    }

    private static PlanetaryPatch[] Order(HashSet<PlanetaryPatch> active)
    {
        var result=active.ToArray();Array.Sort(result,TraversalComparer.Instance);return result;
    }

    private sealed class TraversalComparer:IComparer<PlanetaryPatch>
    {
        internal static readonly TraversalComparer Instance=new();
        public int Compare(PlanetaryPatch left,PlanetaryPatch right)
        {
            var face=left.Face.CompareTo(right.Face);if(face!=0)return face;
            var depth=Math.Max(left.Level,right.Level);
            for(var step=0;step<depth;step++)
            {
                var leftChild=step<left.Level?ChildAt(left,step):0;var rightChild=step<right.Level?ChildAt(right,step):0;
                if(leftChild!=rightChild)return leftChild.CompareTo(rightChild);
            }
            return left.Level.CompareTo(right.Level);
        }
        private static int ChildAt(in PlanetaryPatch patch,int step){var bit=patch.Level-1-step;return((patch.Y>>bit)&1)<<1|((patch.X>>bit)&1);}
    }

    private static double Distance(in Double3 a,in Double3 b)=>Math.Sqrt((a-b).LengthSquared);
    private static Double3 Project(CubeSphereFace face,double u,double v,double radius,bool relaxed)=>relaxed?RelaxedCubeSphereProjection.Project(face,u,v,radius):CubeSphereProjection.Project(face,u,v,radius);
    private static double Angle(in Double3 a,in Double3 b){var denominator=Math.Sqrt(a.LengthSquared*b.LengthSquared);if(denominator<=0)return 0d;return Math.Acos(Math.Clamp((a.X*b.X+a.Y*b.Y+a.Z*b.Z)/denominator,-1d,1d));}
}

/// <summary>Normalized-cube projection: map each face coordinate to a unit direction then multiply by physical radius.</summary>
public static class CubeSphereProjection
{
    public static Double3 Project(CubeSphereFace face,double u,double v,double radius)
    {
        if(!double.IsFinite(u)||!double.IsFinite(v)||!double.IsFinite(radius)||radius<=0)throw new ArgumentOutOfRangeException();var a=2d*u-1d;var b=2d*v-1d;var cube=face switch{CubeSphereFace.PositiveX=>new Double3(1,b,-a),CubeSphereFace.NegativeX=>new Double3(-1,b,a),CubeSphereFace.PositiveY=>new Double3(a,1,-b),CubeSphereFace.NegativeY=>new Double3(a,-1,b),CubeSphereFace.PositiveZ=>new Double3(a,b,1),_=>new Double3(-a,b,-1)};return cube.Normalized()*radius;
    }
    public static Double3 CameraRelativeCenter(in PlanetRenderProxy body,in UniversePosition cameraRootPosition)
    {
        return CameraRelativeRenderPosition.Create(body.Position,cameraRootPosition).Value;
    }
}
