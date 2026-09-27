using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Immutable physical support geometry, independent of site gravity.
/// Feet are authored physical patches, not a fixed-count launch class.</summary>
internal sealed class CompiledCraftContact
{
    internal CompiledCraft Craft {get;}
    internal double SupportPlane {get;}
    internal double ContactTolerance=>.002;
    internal ImmutableArray<Double3> Centers {get;}
    private readonly Lazy<double> minimumHalfWidth;
    internal double MinimumNativeHalfWidth=>minimumHalfWidth.Value;
    private readonly ImmutableArray<ImmutableArray<int>> supportByChild;
    internal CompiledCraftContact(CompiledCraft craft)
    {
        Craft=craft;Require(craft.Function&&craft.AdmissionDiagnostics.IsEmpty,"Contact preparation requires compiled function and support.");
        Require(craft.Collision.Length is >0 and <=256&&craft.Collision.All(c=>c.Vertices.Length is >=4 and <=256),"Physical convex preparation capacity.");
        Require(!craft.Support.IsDefaultOrEmpty,"No authored physical support patches.");
        SupportPlane=craft.Support.Min(s=>s.MaterialFrame.Position.X);
        var owners=Enumerable.Range(0,craft.Collision.Length).Select(_=>ImmutableArray.CreateBuilder<int>()).ToArray();
        for(var i=0;i<craft.Support.Length;i++){
            var foot=craft.Support[i];
            Require((foot.MaterialFrame.Rotation.Apply(Double3.UnitX)-Double3.UnitX).LengthSquared<=1e-24&&Math.Abs(foot.MaterialFrame.Position.X-SupportPlane)<=1e-12,"Support patches require one outward upright plane.");
            var corners=new List<Double3>();foreach(var y in new[]{-foot.Foot.HalfWidthY,foot.Foot.HalfWidthY})foreach(var z in new[]{-foot.Foot.HalfWidthZ,foot.Foot.HalfWidthZ})corners.Add(foot.MaterialFrame.Point(new(0,y,z)));
            var carried=false;
            for(var child=0;child<craft.Collision.Length;child++){
                var c=craft.Collision[child];if(c.Part!=foot.Part||!corners.All(p=>c.Vertices.Any(v=>(v-p).LengthSquared<=1e-24)))continue;
                carried=true;
            }
            Require(carried,"Authored support patch is not carried by a physical convex face.");
        }
        // A load path can contain several authored convex regions (for example
        // a leg and its pad). All their real contact rows belong to the same
        // part-owned finite patch, not only the hull carrying its outer corners.
        supportByChild=craft.Collision.Select(c=>Enumerable.Range(0,craft.Support.Length).Where(i=>craft.Support[i].Part==c.Part).ToImmutableArray()).ToImmutableArray();
        for(var child=0;child<craft.Collision.Length;child++)foreach(var point in craft.Collision[child].Vertices){
            Require(point.X>=SupportPlane-1e-12,"Physical geometry extends below authored support.");
            if(point.X<=SupportPlane+ContactTolerance)Require(SupportAt(child,point)>=0,$"Initial contact is outside uniquely authored support patches: {craft.Collision[child].Part}/{craft.Collision[child].Shape} at {point}.");
        }
        Centers=craft.Collision.Select(c=>c.Vertices.Aggregate(Double3.Zero,(sum,v)=>sum+v)/c.Vertices.Length).ToImmutableArray();
        minimumHalfWidth=new(()=>CraftContactMotionBounds.MinimumHalfWidth(this));
    }
    internal int SupportAt(int child,Double3 materialPoint)
    {
        var owner=-1;
        foreach(var i in supportByChild[child]){
            var s=Craft.Support[i];var p=s.MaterialFrame.Rotation.Transpose().Apply(materialPoint-s.MaterialFrame.Position);
            if(Math.Abs(p.X)>ContactTolerance||Math.Abs(p.Y)>s.Foot.HalfWidthY+ContactTolerance||Math.Abs(p.Z)>s.Foot.HalfWidthZ+ContactTolerance)continue;
            if(owner>=0)return -1;owner=i;
        }
        return owner;
    }
}

