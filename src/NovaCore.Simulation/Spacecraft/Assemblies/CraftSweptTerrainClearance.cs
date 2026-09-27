using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Positive separation from full H and the finite slab over a swept
/// convex-hull box. Cache entries are immutable source certificates, never
/// solver handles. Cache history cannot change a certificate or a decision.</summary>
internal sealed class CraftSweptTerrainClearance(AssemblyFloridaSite site)
{
    private const double TileSize=4;
    // Storage only. A miss evaluates the identical certificate before eviction.
    private const int Capacity=64;
    private readonly Dictionary<(int X,int Y),double> upper=new();
    private readonly Queue<(int X,int Y)> order=new();
    internal int Prepared {get;private set;}
    internal int Cached=>upper.Count;
    internal void ClearCache(){upper.Clear();order.Clear();}
    internal readonly record struct Range(double Low,double High)
    {
        internal bool Finite=>double.IsFinite(Low)&&double.IsFinite(High)&&Low<=High;
        public static implicit operator Range(double x)=>new(x,x);
        internal static Range Out(double lo,double hi)=>new(Math.BitDecrement(lo),Math.BitIncrement(hi));
        public static Range operator +(Range a,Range b)=>Out(a.Low+b.Low,a.High+b.High);
        public static Range operator -(Range a,Range b)=>Out(a.Low-b.High,a.High-b.Low);
        public static Range operator *(Range a,Range b)
        {var aa=a.Low*b.Low;var ab=a.Low*b.High;var ba=a.High*b.Low;var bb=a.High*b.High;return Out(Math.Min(Math.Min(aa,ab),Math.Min(ba,bb)),Math.Max(Math.Max(aa,ab),Math.Max(ba,bb)));}
        public static Range operator /(Range a,Range b)=>b.Low>0?a*Out(1/b.High,1/b.Low):throw new InvalidDataException("Nonpositive swept projection denominator.");
    }
    internal readonly record struct Vector(Range X,Range Y,Range Z)
    {
        public static implicit operator Vector(Double3 p)=>new(p.X,p.Y,p.Z);
        public static Vector operator +(Vector a,Vector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static Vector operator *(Vector a,Range b)=>new(a.X*b,a.Y*b,a.Z*b);
        internal static Vector Cross(Vector a,Vector b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal Range Dot(Double3 b)=>X*b.X+Y*b.Y+Z*b.Z;
        internal Vector Rotate(DoubleQuaternion orientation)
        {var q=orientation.Normalized();Vector axis=new Double3(q.X,q.Y,q.Z);return this+Cross(axis,Cross(axis,this)+this*q.W)*2;}
    }
    internal static Vector SweptBox(CompiledCraft craft,AssemblyMotion motion,Double3 velocity,double h,double expansion)
    {
        var x0=double.PositiveInfinity;var y0=x0;var z0=x0;var x1=double.NegativeInfinity;var y1=x1;var z1=x1;
        foreach(var hull in craft.Collision)foreach(var vertex in hull.Vertices)
        {
            var v=(Vector)motion.PositionO+((Vector)vertex).Rotate(motion.BodyToWorld);
            x0=Math.Min(x0,v.X.Low);y0=Math.Min(y0,v.Y.Low);z0=Math.Min(z0,v.Z.Low);
            x1=Math.Max(x1,v.X.High);y1=Math.Max(y1,v.Y.High);z1=Math.Max(z1,v.Z.High);
        }
        Range Sweep(double a,double b,double v)=>new Range(a,b)+new Range(0,h)*v+new Range(-expansion,expansion);
        return new(Sweep(x0,x1,velocity.X),Sweep(y0,y1,velocity.Y),Sweep(z0,z1,velocity.Z));
    }
    internal bool Clears(Vector box,out double clearance)
    {
        clearance=double.NegativeInfinity;
        if(!site.Applicable||!box.X.Finite||!box.Y.Finite||!box.Z.Finite)return false;
        // Slab is a full finite solid in this admitted site frame, not an
        // infinite plane or a radius enlarged by distant horizontal corners.
        if(site.Slab is {} slab)
        {
            var halfX=Math.BitIncrement(slab.Dimensions.X*.5);var halfZ=Math.BitIncrement(slab.Dimensions.Z*.5);
            if(!(box.X.High< -halfX||box.X.Low>halfX||box.Z.High< -halfZ||box.Z.Low>halfZ||
                box.Y.Low>Math.BitIncrement(site.SupportPlane)||box.Y.High<Math.BitDecrement(site.SupportPlane-slab.Dimensions.Y)))return false;
        }
        var region=FloridaFacilitySupport.Region;
        var point=(Vector)site.OriginBodyFixed+box.Rotate(site.LocalToBodyFixed);
        var up=point.Dot(region.Up);
        if(up.Low<=region.RadiusMetres*.5)return false;
        var x=point.Dot(region.East)*region.RadiusMetres/up;var y=point.Dot(region.North)*region.RadiusMetres/up;
        if(!x.Finite||!y.Finite||Math.Max(Math.Max(Math.Abs(x.Low),Math.Abs(x.High)),Math.Max(Math.Abs(y.Low),Math.Abs(y.High)))>PhysicalCollisionFrame.MaximumCoordinate)return false;
        var x0=(int)Math.Floor(x.Low/TileSize);var x1=(int)Math.Floor(x.High/TileSize);
        var y0=(int)Math.Floor(y.Low/TileSize);var y1=(int)Math.Floor(y.High/TileSize);
        // Same bounded local coverage domain as the admitted contact mesh.
        // Beyond it the caller retains its global radial proof/import path.
        if((long)(x1-x0+1)*(y1-y0+1)>1024)return false;
        var maximum=double.NegativeInfinity;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        for(var i=x0;i<=x1;i++)for(var j=y0;j<=y1;j++)
        {
            var key=(i,j);
            if(!upper.TryGetValue(key,out var value))
            {
                if(!site.CollisionSource.TryPreparePatch(frame,new(i*TileSize,j*TileSize),new((i+1)*TileSize,(j+1)*TileSize),out var patch)||
                    patch is null||!patch.TryFloridaHeightRange(out _,out value)||!double.IsFinite(value))return false;
                Prepared++;
                if(upper.Count==Capacity)upper.Remove(order.Dequeue());
                upper.Add(key,value);order.Enqueue(key);
            }
            maximum=Math.Max(maximum,value);
        }
        clearance=((up-region.RadiusMetres)-maximum).Low;
        return double.IsFinite(clearance)&&clearance>0;
    }
}
