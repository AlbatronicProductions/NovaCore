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
    public (double MinX,double MinY,double MaxX,double MaxY) Bounds{get{var scale=1d/(1<<Level);return(X*scale,Y*scale,(X+1)*scale,(Y+1)*scale);}}
    public int CompareTo(PlanetaryPatch other){var face=Face.CompareTo(other.Face);if(face!=0)return face;var level=Level.CompareTo(other.Level);if(level!=0)return level;var x=X.CompareTo(other.X);return x!=0?x:Y.CompareTo(other.Y);}
}

public readonly record struct PlanetaryLodConfiguration
{
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel):this(nearFieldAltitudeRadii,maximumLevel,.11d) { }
    public PlanetaryLodConfiguration(double nearFieldAltitudeRadii,int maximumLevel,double maximumProjectedPatchSpan)
    { NearFieldAltitudeRadii=nearFieldAltitudeRadii;MaximumLevel=maximumLevel;MaximumProjectedPatchSpan=maximumProjectedPatchSpan; }
    public double NearFieldAltitudeRadii { get; }
    public int MaximumLevel { get; }
    public double MaximumProjectedPatchSpan { get; }
    public bool IsValid=>double.IsFinite(NearFieldAltitudeRadii)&&NearFieldAltitudeRadii>0&&MaximumLevel is >=0 and <=12&&double.IsFinite(MaximumProjectedPatchSpan)&&MaximumProjectedPatchSpan>0;
}

public readonly record struct PlanetaryLodSelection(
    PlanetaryRepresentation Representation,
    int MaximumLevel,
    PlanetaryPatch[] Patches,
    PlanetaryPatchEdge[] StitchMasks,
    int RefinementCount,
    int BalancedRefinementCount,
    int CulledPatchCount);

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
    {
        var representation=Select(body,cameraRootPosition,configuration);if(representation==PlanetaryRepresentation.FarFieldBody)return new(representation,0,[],[],0,0,0);
        var cameraRelative=cameraRootPosition-body.Position.Value;var active=new HashSet<PlanetaryPatch>();var refined=0;var culled=0;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())Traverse(new(face,0,0,0),cameraRelative,body.RadiusMetres,configuration,active,ref refined,ref culled);
        var balanced=Balance(active,configuration.MaximumLevel);var ordered=Order(active,configuration.MaximumLevel);var masks=new PlanetaryPatchEdge[ordered.Length];
        for(var index=0;index<ordered.Length;index++)
        {
            foreach(var edge in Edges)
            {
                var neighbor=FindCoveringNeighbor(ordered[index],edge,active);
                if(neighbor is { } candidate&&candidate.Level+1==ordered[index].Level)masks[index]|=edge;
                else if(neighbor is { } invalid&&invalid.Level+1<ordered[index].Level)throw new InvalidOperationException("Unbalanced planetary patch selection.");
            }
        }
        var maximum=ordered.Length==0?0:ordered.Max(patch=>patch.Level);return new(representation,maximum,ordered,masks,refined,balanced,culled);
    }

    public static PlanetaryPatch? FindCoveringNeighbor(in PlanetaryPatch patch,PlanetaryPatchEdge edge,IReadOnlySet<PlanetaryPatch> active)
    {
        PlanetaryPatch? candidate=CubeSphereAdjacency.NeighborAtSameLevel(patch,edge);
        while(candidate is { } value){if(active.Contains(value))return value;candidate=value.Parent;}
        return null;
    }

    public static PlanetaryPatch[] BalancePatches(IEnumerable<PlanetaryPatch> leaves,int maximumLevel,out int refinementCount)
    {
        ArgumentNullException.ThrowIfNull(leaves);if(maximumLevel is <0 or >12)throw new ArgumentOutOfRangeException(nameof(maximumLevel));var active=leaves.ToHashSet();
        if(active.Any(patch=>patch.Level<0||patch.Level>maximumLevel||patch.X<0||patch.Y<0||patch.X>=(1<<patch.Level)||patch.Y>=(1<<patch.Level)))throw new ArgumentOutOfRangeException(nameof(leaves));
        refinementCount=Balance(active,maximumLevel);return Order(active,maximumLevel);
    }

    private static void Traverse(PlanetaryPatch patch,in Double3 cameraRelative,double radius,in PlanetaryLodConfiguration configuration,HashSet<PlanetaryPatch> active,ref int refined,ref int culled)
    {
        if(patch.Level>0&&IsHorizonCulled(patch,cameraRelative,radius)){culled++;return;}
        if(patch.Level==configuration.MaximumLevel||ProjectedSpan(patch,cameraRelative,radius)<=configuration.MaximumProjectedPatchSpan){active.Add(patch);return;}
        refined++;for(var child=0;child<4;child++)Traverse(patch.Child(child),cameraRelative,radius,configuration,active,ref refined,ref culled);
    }

    private static double ProjectedSpan(in PlanetaryPatch patch,in Double3 cameraRelative,double radius)
    {
        var bounds=patch.Bounds;var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,radius);var halfSpan=0d;
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MinY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MinY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MaxY,radius)));
        halfSpan=Math.Max(halfSpan,Distance(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MaxY,radius)));
        return 2d*halfSpan/Math.Max(Distance(cameraRelative,center),radius*1e-12d);
    }

    private static bool IsHorizonCulled(in PlanetaryPatch patch,in Double3 cameraRelative,double radius)
    {
        var cameraDistance=Math.Sqrt(cameraRelative.LengthSquared);if(cameraDistance<=radius)return false;var view=cameraRelative/cameraDistance;var bounds=patch.Bounds;var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,1d);var angularRadius=0d;
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MinY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MinY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MinX,bounds.MaxY,1d)));
        angularRadius=Math.Max(angularRadius,Angle(center,CubeSphereProjection.Project(patch.Face,bounds.MaxX,bounds.MaxY,1d)));
        var centerAngle=Angle(center,view);var horizon=Math.Acos(Math.Clamp(radius/cameraDistance,-1d,1d));return centerAngle-angularRadius>horizon;
    }

    private static int Balance(HashSet<PlanetaryPatch> active,int maximumLevel)
    {
        var count=0;
        while(true)
        {
            PlanetaryPatch? coarse=null;
            foreach(var patch in Order(active,maximumLevel))
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

    private static PlanetaryPatch[] Order(HashSet<PlanetaryPatch> active,int maximumLevel)
    {
        var result=new List<PlanetaryPatch>(active.Count);foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())Emit(new(face,0,0,0),maximumLevel,active,result);return result.ToArray();
    }

    private static void Emit(PlanetaryPatch patch,int maximumLevel,HashSet<PlanetaryPatch> active,List<PlanetaryPatch> result)
    {
        if(active.Contains(patch)){result.Add(patch);return;}if(patch.Level==maximumLevel)return;for(var child=0;child<4;child++)Emit(patch.Child(child),maximumLevel,active,result);
    }

    private static double Distance(in Double3 a,in Double3 b)=>Math.Sqrt((a-b).LengthSquared);
    private static double Angle(in Double3 a,in Double3 b)=>Math.Acos(Math.Clamp(a.X*b.X+a.Y*b.Y+a.Z*b.Z,-1d,1d));
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
        if(cameraRootPosition.Frame!=body.Position.Frame)throw new ArgumentException("Camera and body roots differ.");return body.Position.Value-cameraRootPosition.Value;
    }
}
