using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static class CraftContactMotionBounds
{
    internal static double MinimumHalfWidth(CompiledCraftContact profile)
    {
        var pool=new BufferPool();var shapes=new Shapes(pool,16);TypedIndex compound=default;
        try
        {
            // Reuse the exact production convex preparation. An inball of
            // radius r proves width >=2r in every orientation, unlike a world
            // AABB or the launch feet's width. No part identity enters this law.
            compound=LocalContactWorld.CreateCraftShape(shapes,pool,profile,profile.Craft.InitialMass,out var centers);
            ref var children=ref shapes.GetShape<Compound>(compound.Index).Children;
            var minimum=double.PositiveInfinity;
            for(var i=0;i<children.Length;i++)
            {
                ref var hull=ref shapes.GetShape<ConvexHull>(children[i].ShapeIndex.Index);
                for(var f=0;f<hull.FaceToVertexIndicesStart.Length;f++)
                {
                    hull.GetVertexIndicesForFace(f,out var indices);var face=new Double3[indices.Length];
                    for(var j=0;j<face.Length;j++)
                    {
                        hull.GetPoint(indices[j],out var p);face[j]=new(p.X,p.Y,p.Z);
                        var native=face[j]+centers[i];var authored=new Double3(native.Y,-native.X,native.Z);
                        var error=profile.Craft.Collision[i].Vertices.Min(v=>Math.Sqrt((v-authored).LengthSquared));
                        if(!double.IsFinite(error)||error>profile.ContactTolerance/8)throw new InvalidDataException("Native retained hull vertex exceeds its authored geometry allowance.");
                    }
                    var normal=Double3.Zero;var largest=0d;
                    for(var j=1;j<face.Length-1;j++)
                    {
                        var n=Double3.Cross(face[j]-face[0],face[j+1]-face[0]);
                        if(n.LengthSquared>largest){normal=n;largest=n.LengthSquared;}
                    }
                    if(!double.IsFinite(largest)||largest<=0)throw new InvalidDataException("Native hull face has no finite inball certificate.");
                    normal=normal.Normalized();var distance=Math.Abs(Double3.Dot(normal,face[0]));
                    var size=face.Max(p=>Math.Sqrt(p.LengthSquared));
                    // Arithmetic enclosure for normalized cross/dot, with
                    // explicit conditioning by the chosen triangle area.
                    var arithmetic=256*Math.ScaleB(1d,-52)*Math.Max(1,size)*(1+size*size/Math.Sqrt(largest));
                    var radius=Math.BitDecrement(distance-arithmetic-profile.ContactTolerance);
                    if(!double.IsFinite(radius)||radius<=0)throw new InvalidDataException("Native hull too thin for the contact precision domain.");
                    minimum=Math.Min(minimum,radius);
                }
            }
            return minimum;
        }
        finally
        {
            if(compound.Exists)shapes.RecursivelyRemoveAndDispose(compound,pool);
            shapes.Dispose();pool.Clear();
        }
    }
}
