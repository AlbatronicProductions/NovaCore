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
    public int CompareTo(PlanetaryPatch other){var face=Face.CompareTo(other.Face);if(face!=0)return face;var level=Level.CompareTo(other.Level);if(level!=0)return level;var x=X.CompareTo(other.X);return x!=0?x:Y.CompareTo(other.Y);}
}

public readonly record struct PlanetaryLodConfiguration
{
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel):this(nearFieldAltitudeRadii,maximumLevel,.11d) { }
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel,double maximumProjectedPatchSpan)
        :this(nearFieldAltitudeRadii,maximumLevel,maximumProjectedPatchSpan,0d,0d,0d) { }
    private PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel,double maximumProjectedPatchSpan,double maximumTerrainHeightMetres,double targetPatchPixels,double viewportHeightPixels)
    { NearFieldAltitudeRadii=nearFieldAltitudeRadii;MaximumLevel=maximumLevel;MaximumProjectedPatchSpan=maximumProjectedPatchSpan;MaximumTerrainHeightMetres=maximumTerrainHeightMetres;TargetPatchPixels=targetPatchPixels;ViewportHeightPixels=viewportHeightPixels; }
    public double NearFieldAltitudeRadii { get; }
    public int MaximumLevel { get; }
    public double MaximumProjectedPatchSpan { get; }
    public double MaximumTerrainHeightMetres { get; }
    public double TargetPatchPixels { get; }
    public double ViewportHeightPixels { get; }
    public bool IsValid=>double.IsFinite(NearFieldAltitudeRadii)&&NearFieldAltitudeRadii>0&&MaximumLevel is >=0 and <=24&&double.IsFinite(MaximumProjectedPatchSpan)&&MaximumProjectedPatchSpan>0&&double.IsFinite(MaximumTerrainHeightMetres)&&MaximumTerrainHeightMetres>=0;

    /// <summary>Converts a patch pixel budget into the selector's conservative angular-span metric.</summary>
    public static PlanetaryLodConfiguration ForViewport(double nearFieldAltitudeRadii,int maximumLevel,double targetPatchPixels,double viewportHeightPixels,double verticalFieldOfViewRadians,double maximumTerrainHeightMetres=0d)
    {
        if(!double.IsFinite(targetPatchPixels)||targetPatchPixels<=0||!double.IsFinite(viewportHeightPixels)||viewportHeightPixels<=0||!double.IsFinite(verticalFieldOfViewRadians)||verticalFieldOfViewRadians<=0||verticalFieldOfViewRadians>=Math.PI)throw new ArgumentOutOfRangeException();
        var span=2d*targetPatchPixels*Math.Tan(verticalFieldOfViewRadians*.5d)/viewportHeightPixels;
        return new(nearFieldAltitudeRadii,maximumLevel,span,maximumTerrainHeightMetres,targetPatchPixels,viewportHeightPixels);
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
    {
        if(!double.IsFinite(surfaceAltitudeMetres)||surfaceAltitudeMetres<0)throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));
        var useFrustum=viewForward.IsFinite&&viewForward.LengthSquared>0&&double.IsFinite(verticalFieldOfViewRadians)&&verticalFieldOfViewRadians>0&&verticalFieldOfViewRadians<Math.PI&&double.IsFinite(aspectRatio)&&aspectRatio>0;var normalizedForward=useFrustum?viewForward.Normalized():Double3.Zero;var halfViewAngle=useFrustum?Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(verticalFieldOfViewRadians*.5d)*aspectRatio,2d)+Math.Pow(Math.Tan(verticalFieldOfViewRadians*.5d),2d))):0d;
        var representation=Select(body,cameraRootPosition,configuration);if(representation==PlanetaryRepresentation.FarFieldBody)return new(representation,0,[],[],0,0,0,0,0,0,0,0,0);
        var previousExact=previousLeaves.IsEmpty?null:new HashSet<PlanetaryPatch>(previousLeaves.Length);var previousRefined=previousLeaves.IsEmpty?null:new HashSet<PlanetaryPatch>();
        if(previousExact is not null&&previousRefined is not null)foreach(ref readonly var leaf in previousLeaves){previousExact.Add(leaf);var parent=leaf.Parent;while(parent is { } value){previousRefined.Add(value);parent=value.Parent;}}
        var cameraRelative=cameraRootPosition-body.Position.Value;var selectionSurfaceRadius=configuration.MaximumTerrainHeightMetres>0?Math.Sqrt(cameraRelative.LengthSquared)-surfaceAltitudeMetres:body.RadiusMetres;var active=new HashSet<PlanetaryPatch>();var refined=0;var frustumCulled=0;var horizonCulled=0;var splits=0;var merges=0;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())Traverse(new(face,0,0,0),cameraRelative,body.RadiusMetres,selectionSurfaceRadius,surfaceAltitudeMetres,configuration,normalizedForward,halfViewAngle,useFrustum,previousExact,previousRefined,active,ref refined,ref frustumCulled,ref horizonCulled,ref splits,ref merges);
        var balanced=Balance(active,configuration.MaximumLevel);var ordered=Order(active);var masks=new PlanetaryPatchEdge[ordered.Length];
        for(var index=0;index<ordered.Length;index++)
        {
            foreach(var edge in Edges)
            {
                var neighbor=FindCoveringNeighbor(ordered[index],edge,active);
                if(neighbor is { } candidate&&candidate.Level+1==ordered[index].Level)masks[index]|=edge;
                else if(neighbor is { } invalid&&invalid.Level+1<ordered[index].Level)throw new InvalidOperationException("Unbalanced planetary patch selection.");
            }
        }
        var maximum=ordered.Length==0?0:ordered.Max(patch=>patch.Level);return new(representation,maximum,ordered,masks,refined,balanced,frustumCulled+horizonCulled,frustumCulled,horizonCulled,splits,merges,0,0);
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

    private static void Traverse(PlanetaryPatch patch,in Double3 cameraRelative,double radius,double selectionSurfaceRadius,double surfaceAltitudeMetres,in PlanetaryLodConfiguration configuration,in Double3 viewForward,double halfViewAngle,bool useFrustum,HashSet<PlanetaryPatch>? previousExact,HashSet<PlanetaryPatch>? previousRefined,HashSet<PlanetaryPatch> active,ref int refined,ref int frustumCulled,ref int horizonCulled,ref int splits,ref int merges)
    {
        if(patch.Level>0&&IsHorizonCulled(patch,cameraRelative,radius,configuration.MaximumTerrainHeightMetres)){horizonCulled++;return;}
        if(patch.Level>0&&useFrustum&&IsViewCulled(patch,cameraRelative,selectionSurfaceRadius,viewForward,halfViewAngle)){frustumCulled++;return;}
        var wasLeaf=previousExact?.Contains(patch)==true;var wasRefined=previousRefined?.Contains(patch)==true;var threshold=configuration.MaximumProjectedPatchSpan*(wasLeaf ? 1.12d : wasRefined ? .88d : 1d);var projectedSpan=ProjectedSpan(patch,cameraRelative,configuration.MaximumTerrainHeightMetres>0?selectionSurfaceRadius:radius);
        if(patch.Level==configuration.MaximumLevel||projectedSpan<=threshold){active.Add(patch);if(wasRefined)merges++;return;}
        refined++;if(wasLeaf)splits++;for(var child=0;child<4;child++)Traverse(patch.Child(child),cameraRelative,radius,selectionSurfaceRadius,surfaceAltitudeMetres,configuration,viewForward,halfViewAngle,useFrustum,previousExact,previousRefined,active,ref refined,ref frustumCulled,ref horizonCulled,ref splits,ref merges);
    }

    private static double ProjectedSpan(in PlanetaryPatch patch,in Double3 cameraRelative,double radius)
    {
        var bounds=patch.Bounds;var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,radius);var halfSpan=0d;
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MinY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MinY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MaxY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MaxY,radius)));
        var distanceToPatch=Math.Max(Distance(cameraRelative,center),radius*1e-12d);return 2d*halfSpan/distanceToPatch;
    }

    private static bool IsHorizonCulled(in PlanetaryPatch patch,in Double3 cameraRelative,double radius,double maximumTerrainHeightMetres)
    {
        var cameraDistance=Math.Sqrt(cameraRelative.LengthSquared);if(cameraDistance<=radius)return false;var view=cameraRelative/cameraDistance;var bounds=patch.Bounds;var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,1d);var angularRadius=0d;
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MinY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MinY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MaxY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MaxY,1d)));
        angularRadius+=Math.Asin(Math.Clamp(maximumTerrainHeightMetres/radius,0d,.5d));var centerAngle=Angle(center,view);var horizon=Math.Acos(Math.Clamp(radius/cameraDistance,-1d,1d));return centerAngle-angularRadius>horizon;
    }

    private static bool IsViewCulled(in PlanetaryPatch patch,in Double3 cameraRelative,double surfaceRadius,in Double3 viewForward,double halfViewAngle)
    {
        var bounds=patch.Bounds;var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,surfaceRadius)-cameraRelative;if(center.LengthSquared<=surfaceRadius*surfaceRadius*1e-24d)return false;var angularRadius=0d;
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MinY,surfaceRadius)-cameraRelative));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MinY,surfaceRadius)-cameraRelative));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MaxY,surfaceRadius)-cameraRelative));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MaxY,surfaceRadius)-cameraRelative));
        return Angle(center,viewForward)-angularRadius>halfViewAngle;
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
