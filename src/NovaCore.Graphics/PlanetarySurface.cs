using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Stable cube-face order used by all procedural surface work.</summary>
public enum CubeSphereFace:byte { PositiveX,NegativeX,PositiveY,NegativeY,PositiveZ,NegativeZ }
public enum PlanetaryRepresentation:byte { FarFieldBody,NearFieldSurface }

/// <summary>Lightweight deterministic quadtree identity; x/y address a face grid at Level.</summary>
public readonly record struct PlanetaryPatch(CubeSphereFace Face,int Level,int X,int Y):IComparable<PlanetaryPatch>
{
    public PlanetaryPatch? Parent=>Level==0?null:new(Face,Level-1,X>>1,Y>>1);
    public PlanetaryPatch Child(int index)=>index is >=0 and <4?new(Face,Level+1,(X<<1)+(index&1),(Y<<1)+(index>>1)):throw new ArgumentOutOfRangeException(nameof(index));
    public (double MinX,double MinY,double MaxX,double MaxY) Bounds{get{var scale=1d/(1<<Level);return(X*scale,Y*scale,(X+1)*scale,(Y+1)*scale);}}
    public int CompareTo(PlanetaryPatch other){var face=Face.CompareTo(other.Face);if(face!=0)return face;var level=Level.CompareTo(other.Level);if(level!=0)return level;var x=X.CompareTo(other.X);return x!=0?x:Y.CompareTo(other.Y);}
}

public readonly record struct PlanetaryLodConfiguration(double NearFieldAltitudeRadii,int MaximumLevel)
{ public bool IsValid=>double.IsFinite(NearFieldAltitudeRadii)&&NearFieldAltitudeRadii>0&&MaximumLevel is >=0 and <=12; }
public readonly record struct PlanetaryLodSelection(PlanetaryRepresentation Representation,int MaximumLevel,PlanetaryPatch[] Patches);

/// <summary>Presentation-only deterministic policy: near field begins at altitude/radius <= configured threshold.</summary>
public static class PlanetaryRepresentationSelector
{
    public static PlanetaryRepresentation Select(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration)
    {
        if(!configuration.IsValid)throw new ArgumentOutOfRangeException(nameof(configuration));var altitude=Math.Sqrt((cameraRootPosition-body.Position.Value).LengthSquared)-body.RadiusMetres;return altitude/body.RadiusMetres<=configuration.NearFieldAltitudeRadii?PlanetaryRepresentation.NearFieldSurface:PlanetaryRepresentation.FarFieldBody;
    }
    public static PlanetaryLodSelection SelectPatches(in PlanetRenderProxy body,in Double3 cameraRootPosition,in PlanetaryLodConfiguration configuration)
    {
        var representation=Select(body,cameraRootPosition,configuration);if(representation==PlanetaryRepresentation.FarFieldBody)return new(representation,0,[]);
        var ratio=Math.Max(1d,Math.Sqrt((cameraRootPosition-body.Position.Value).LengthSquared)/body.RadiusMetres);var level=Math.Min(configuration.MaximumLevel,Math.Max(0,(int)Math.Floor(Math.Log2(configuration.NearFieldAltitudeRadii/ratio)+1d)));var patches=new List<PlanetaryPatch>();foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())Add(face,0,0,0,level,patches);return new(representation,level,patches.ToArray());
    }
    static void Add(CubeSphereFace face,int level,int x,int y,int target,List<PlanetaryPatch> patches){if(level==target){patches.Add(new(face,level,x,y));return;}for(var child=0;child<4;child++)Add(face,level+1,(x<<1)+(child&1),(y<<1)+(child>>1),target,patches);}
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
