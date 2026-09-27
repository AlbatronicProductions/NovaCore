using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold FIT preparation shared by player editing and later craft compilation.
/// The document still owns topology; geometry can refuse a joint but cannot create one.
/// No runtime state, mesh data, service solver or launch authority is held here.</summary>
internal sealed class PartCompatibilityEvaluator
{
    // Same ten-micrometre transport/contact band as the admitted explicit mating
    // frames. Positive overlap beyond that band refuses FIT. This is not density,
    // inertia, fuel or runtime contact tolerance.
    internal const double ContactToleranceMetres=1e-5;
    private sealed record Hull(ImmutableArray<Double3> Points,ImmutableArray<Double3> Normals,ImmutableArray<Double3> Edges,ImmutableArray<IntervalPoint> Bounds=default);
    // Outward enclosures make separation a proof even when authored offsets or
    // a transform consume the FP64 contact resolution. Uncertain overlap refuses.
    private readonly record struct Interval(double Low,double High)
    {
        internal static Interval Exact(double x)=>new(x,x);
        public static Interval operator +(Interval a,Interval b)=>new(Math.BitDecrement(a.Low+b.Low),Math.BitIncrement(a.High+b.High));
        public static Interval operator -(Interval a,Interval b)=>new(Math.BitDecrement(a.Low-b.High),Math.BitIncrement(a.High-b.Low));
        public static Interval operator *(Interval a,double b)=>b>=0?new(Math.BitDecrement(a.Low*b),Math.BitIncrement(a.High*b)):new(Math.BitDecrement(a.High*b),Math.BitIncrement(a.Low*b));
    }
    private readonly record struct IntervalPoint(Interval X,Interval Y,Interval Z)
    {
        internal static IntervalPoint Exact(Double3 p)=>new(Interval.Exact(p.X),Interval.Exact(p.Y),Interval.Exact(p.Z));
        public static IntervalPoint operator -(IntervalPoint a,IntervalPoint b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        internal Interval Dot(Double3 n)=>X*n.X+Y*n.Y+Z*n.Z;
    }
    private sealed class PlacedHull
    {
        internal AssemblyPose Pose {get;}
        internal Hull Rotated {get;}
        internal PlacedHull(Hull local,AssemblyPose pose){Pose=pose;Rotated=Place(local,new(Double3.Zero,pose.Rotation),IntervalPoint.Exact(Double3.Zero));}
    }
    private sealed record Definition(ImmutableArray<(string Id,Hull Hull)> Collision,ImmutableArray<(string Id,Hull Hull)> Clearance);
    private readonly AssemblyDefinitionCatalog catalog;
    private readonly ImmutableDictionary<DefinitionReference,Definition> prepared;
    internal PartCompatibilityEvaluator(AssemblyDefinitionCatalog catalog)
    {
        Require(catalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema,"FIT requires Part Standard definitions.");this.catalog=catalog;
        prepared=catalog.Data.Definitions.ToImmutableDictionary(catalog.Reference,d=>new Definition(
            d.Standard!.Collision.Select(v=>(v.Id,Prepare(v.Vertices))).ToImmutableArray(),
            d.Standard.Clearance.Select(v=>(v.Id,Prepare(v.Vertices))).ToImmutableArray()));
    }
    internal static AssemblyPose Snap(CompiledPart parent,string target,PartDefinitionData child,string mount,int clock)
    {
        var a=parent.Definition.Standard!.Mechanical.SingleOrDefault(m=>m.Interface==target);
        var b=child.Standard!.Mechanical.SingleOrDefault(m=>m.Interface==mount);
        Require(a is not null&&b is not null&&PartStandard.CanMate(a,b,clock),"FIT_INTERFACE: incompatible class, role or clock.");
        var frame=parent.Instance.Pose.Then(parent.Definition.Attachments.Single(x=>x.Id==target).Frame);
        var local=child.Attachments.Single(x=>x.Id==mount).Frame;
        var rotation=frame.Rotation*PartStandard.Roll(clock)*Matrix3.Mate*local.Rotation.Transpose();
        return new(frame.Position-rotation.Apply(local.Position),rotation);
    }
    internal void RequireFit(CompiledConstructionDesign design)
    {
        Require(ReferenceEquals(design.Catalog,catalog),"FIT catalog preparation mismatch.");
        var shapes=design.Parts.Select(p=>{
            Require(prepared.TryGetValue(p.Instance.Definition,out var d),"FIT definition preparation missing.");
            return (Part:p.Instance.Id,Collision:d.Collision.Select(h=>(h.Id,Hull:new PlacedHull(h.Hull,p.Instance.Pose))).ToArray(),
                Clearance:d.Clearance.Select(h=>(h.Id,Hull:new PlacedHull(h.Hull,p.Instance.Pose))).ToArray());}).ToArray();
        for(var i=0;i<shapes.Length;i++)for(var j=i+1;j<shapes.Length;j++)
        {
            var a=shapes[i];var b=shapes[j];
            foreach(var x in a.Collision)foreach(var y in b.Collision)
                if(Overlap(x.Hull,y.Hull))throw new InvalidDataException($"FIT_COLLISION: {a.Part}/{x.Id} intersects {b.Part}/{y.Id}.");
            foreach(var x in a.Clearance)foreach(var y in b.Collision)
                if(Overlap(x.Hull,y.Hull))throw new InvalidDataException($"FIT_CLEARANCE: {a.Part}/{x.Id} blocked by {b.Part}/{y.Id}.");
            foreach(var x in b.Clearance)foreach(var y in a.Collision)
                if(Overlap(x.Hull,y.Hull))throw new InvalidDataException($"FIT_CLEARANCE: {b.Part}/{x.Id} blocked by {a.Part}/{y.Id}.");
        }
    }
    private static Double3? Direction(Double3 v)
    {
        Require(v.IsFinite,"FIT_GEOMETRY_RANGE: direction is not finite.");
        var scale=Math.Max(Math.Abs(v.X),Math.Max(Math.Abs(v.Y),Math.Abs(v.Z)));if(scale==0)return null;
        var n=(v/scale).Normalized();if(n.X<0||n.X==0&&(n.Y<0||n.Y==0&&n.Z<0))n*=-1;
        return n;
    }
    private static Hull Prepare(ImmutableArray<Double3> points)
    {
        var edges=new HashSet<Double3>();var normals=new HashSet<Double3>();
        for(var a=0;a<points.Length;a++)for(var b=a+1;b<points.Length;b++)
        {
            var edge=Direction(points[b]-points[a]);if(edge is not {} e)continue;edges.Add(e);
            for(var c=b+1;c<points.Length;c++)
            {
                var other=Direction(points[c]-points[a]);if(other is not {} o)continue;
                if(Direction(Double3.Cross(e,o)) is not {} n)continue;
                var low=double.PositiveInfinity;var high=double.NegativeInfinity;
                foreach(var p in points){var dot=Double3.Dot(p-points[a],n);Require(double.IsFinite(dot),"FIT_GEOMETRY_RANGE: plane is not finite.");low=Math.Min(low,dot);high=Math.Max(high,dot);}
                if(low>=-ContactToleranceMetres||high<=ContactToleranceMetres)normals.Add(n);
            }
        }
        Require(normals.Count>=3&&edges.Count>=3,"FIT_GEOMETRY_RANGE: insufficient prepared axes.");
        return new(points,normals.ToImmutableArray(),edges.ToImmutableArray());
    }
    private static Hull Place(Hull local,AssemblyPose pose,IntervalPoint offset)
    {
        var points=local.Points.Select(p=>pose.Point(p)).ToImmutableArray();Require(points.All(p=>p.IsFinite),"FIT_GEOMETRY_RANGE: transformed vertex is not finite.");
        var r=pose.Rotation;
        var bounds=local.Points.Select(p=>{
            var v=IntervalPoint.Exact(p);
            return new IntervalPoint(v.Dot(new(r.A,r.B,r.C))+offset.X,v.Dot(new(r.D,r.E,r.F))+offset.Y,v.Dot(new(r.G,r.H,r.I))+offset.Z);
        }).ToImmutableArray();
        return new(points,local.Normals.Select(p=>pose.Rotation.Apply(p)).ToImmutableArray(),local.Edges.Select(p=>pose.Rotation.Apply(p)).ToImmutableArray(),bounds);
    }
    private static bool Separated(Hull a,Hull b,Double3 n,IntervalPoint offset)
    {
        // Translate to a common origin before projecting. Large world offsets do
        // not consume precision in a design-space compatibility query.
        var origin=a.Points[0];var amin=double.PositiveInfinity;var amax=double.NegativeInfinity;
        var bmin=double.PositiveInfinity;var bmax=double.NegativeInfinity;
        foreach(var p in a.Bounds){var q=(p-IntervalPoint.Exact(origin)).Dot(n);Require(double.IsFinite(q.Low)&&double.IsFinite(q.High),"FIT_GEOMETRY_RANGE: projection.");amin=Math.Min(amin,q.Low);amax=Math.Max(amax,q.High);}
        var projectedOffset=offset.Dot(n);
        foreach(var p in b.Bounds){var q=(p-IntervalPoint.Exact(origin)).Dot(n)+projectedOffset;Require(double.IsFinite(q.Low)&&double.IsFinite(q.High),"FIT_GEOMETRY_RANGE: projection.");bmin=Math.Min(bmin,q.Low);bmax=Math.Max(bmax,q.High);}
        // Use the translation needed to separate in either direction. Intersection
        // width is wrong for containment: a thin object deep inside a solid is not
        // near a contacting face merely because the contained object is thin.
        // Projection depth is scaled by |n|. Neither an admitted approximately
        // rigid matrix nor floating-point normalization promises an exact unit
        // axis. Round the metric band downward; separation still needs proof.
        var squaredLower=IntervalPoint.Exact(n).Dot(n).Low;
        var lengthLower=Math.Max(0,Math.BitDecrement(Math.Sqrt(Math.Max(0,squaredLower))));
        var band=Math.Max(0,Math.BitDecrement(ContactToleranceMetres*lengthLower));
        return Math.BitIncrement(amax-bmin)<=band||Math.BitIncrement(bmax-amin)<=band;
    }
    private static bool Overlap(PlacedHull first,PlacedHull second)
    {
        // Rebase the part origins BEFORE adding local vertices. Subtracting two
        // already rounded absolute vertices would erase metre-scale solids at
        // astronomical offsets and could turn penetration into apparent contact.
        var a=first.Rotated;var b=second.Rotated;
        var offset=IntervalPoint.Exact(second.Pose.Position)-IntervalPoint.Exact(first.Pose.Position);
        if(Separated(a,b,Double3.UnitX,offset)||Separated(a,b,Double3.UnitY,offset)||Separated(a,b,Double3.UnitZ,offset))return false;
        foreach(var n in a.Normals)if(Separated(a,b,n,offset))return false;
        foreach(var n in b.Normals)if(Separated(a,b,n,offset))return false;
        foreach(var x in a.Edges)foreach(var y in b.Edges)if(Direction(Double3.Cross(x,y)) is {} n&&Separated(a,b,n,offset))return false;
        return true;
    }
}
