using System.Collections.Immutable;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // These helpers prepare the same world's convex children and full inertia.
    // Only the transaction-owned world may use them for a published body.
    internal static TypedIndex CreateCraftShape(Shapes shapes,BufferPool pool,CompiledCraftContact profile,AssemblyMass mass,out ImmutableArray<Double3> nativeCenters)
    {
        pool.Take<CompoundChild>(profile.Craft.Collision.Length,out var children);var created=0;
        var centers=ImmutableArray.CreateBuilder<Double3>(children.Length);
        try{
            for(var i=0;i<children.Length;i++){
                var points=profile.Craft.Collision[i].Vertices;var center=profile.Centers[i];var vertices=new Vector3[points.Length];
                for(var j=0;j<points.Length;j++){
                    var exact=AssemblyToNativeBody.Apply(points[j]-center);vertices[j]=ToFloat(exact);
                    if(!Finite(vertices[j])||Math.Sqrt((FromFloat(vertices[j])-exact).LengthSquared)>profile.ContactTolerance/16)throw new InvalidDataException("Convex vertex precision envelope exceeded.");
                }
                var hull=new ConvexHull(vertices.AsSpan(),pool,out var hullCenter);TypedIndex shape;
                try{shape=shapes.Add(hull);}catch{hull.Dispose(pool);throw;}
                children[i]=new(new RigidPose(default,Quaternion.Identity),shape);created++;
                var nativeCenter=AssemblyToNativeBody.Apply(center)+FromFloat(hullCenter);var offset=nativeCenter-AssemblyToNativeBody.Apply(mass.Com);
                centers.Add(nativeCenter);children[i]=new(new RigidPose(ToFloat(offset),Quaternion.Identity),shape);
                if(!Finite(children[i].LocalPosition)||Math.Sqrt((FromFloat(children[i].LocalPosition)-offset).LengthSquared)>profile.ContactTolerance/16)throw new InvalidDataException("Convex center precision envelope exceeded.");
            }
            nativeCenters=centers.MoveToImmutable();return shapes.Add(new Compound(children));
        }catch{
            for(var i=0;i<created;i++)shapes.RemoveAndDispose(children[i].ShapeIndex,pool);
            pool.Return(ref children);throw;
        }
    }
    internal static BodyInertia CraftInertia(AssemblyMass mass)
    {
        var inverse=AssemblyToNativeBody*mass.Inertia.Inverse()*AssemblyToNativeBody.Transpose();
        var inertia=new BodyInertia{InverseMass=(float)(1/mass.Mass)};
        inertia.InverseInertiaTensor.XX=(float)inverse.A;inertia.InverseInertiaTensor.YX=(float)inverse.D;inertia.InverseInertiaTensor.YY=(float)inverse.E;
        inertia.InverseInertiaTensor.ZX=(float)inverse.G;inertia.InverseInertiaTensor.ZY=(float)inverse.H;inertia.InverseInertiaTensor.ZZ=(float)inverse.I;
        var rounded=new Matrix3(inertia.InverseInertiaTensor.XX,inertia.InverseInertiaTensor.YX,inertia.InverseInertiaTensor.ZX,
            inertia.InverseInertiaTensor.YX,inertia.InverseInertiaTensor.YY,inertia.InverseInertiaTensor.ZY,
            inertia.InverseInertiaTensor.ZX,inertia.InverseInertiaTensor.ZY,inertia.InverseInertiaTensor.ZZ);
        if(!rounded.Positive||!PositiveFloat(1/mass.Mass)||!PositiveFloat(inverse.A)||!PositiveFloat(inverse.E)||!PositiveFloat(inverse.I)||!float.IsFinite(inertia.InverseInertiaTensor.YX)||
            !float.IsFinite(inertia.InverseInertiaTensor.ZX)||!float.IsFinite(inertia.InverseInertiaTensor.ZY))throw new InvalidDataException("Physical native inertia precision envelope exceeded.");
        return inertia;
    }
    internal static (Double3 Position,Double3 Velocity) RecenterCraftBody(BepuPhysics.Simulation simulation,BodyReference native,TypedIndex compound,
        ImmutableArray<Double3> centers,AssemblyMass previous,AssemblyMass successor,Double3 positionResidue,Double3 velocityResidue,double tolerance)
    {
        if(centers.IsDefaultOrEmpty||centers.Length>256||!double.IsFinite(tolerance)||tolerance<=0)throw new InvalidDataException("Native recenter preparation dimensions.");
        var fq=native.Pose.Orientation;var q=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W);
        var delta=q.Rotate(AssemblyToNativeBody.Apply(successor.Com-previous.Com));
        var p=FromFloat(native.Pose.Position)+delta+positionResidue;
        var v=FromFloat(native.Velocity.Linear)+Double3.Cross(FromFloat(native.Velocity.Angular),delta)+velocityResidue;
        var fp=ToFloat(p);var fv=ToFloat(v);var dp=p-FromFloat(fp);var dv=v-FromFloat(fv);var inertia=CraftInertia(successor);
        Span<Vector3> offsets=stackalloc Vector3[centers.Length];
        for(var i=0;i<centers.Length;i++){
            var exact=centers[i]-AssemblyToNativeBody.Apply(successor.Com);offsets[i]=ToFloat(exact);
            if(!Finite(offsets[i])||Math.Sqrt((FromFloat(offsets[i])-exact).LengthSquared)>tolerance/8)throw new InvalidDataException("Native child recenter range exceeded.");
        }
        if(!Finite(fp)||!Finite(fv)||Math.Sqrt(dp.LengthSquared)>tolerance/8||Math.Sqrt(dv.LengthSquared)>tolerance/8)throw new InvalidDataException("Native material-origin transport range exceeded.");
        ref var shape=ref simulation.Shapes.GetShape<Compound>(compound.Index);
        if(shape.Children.Length!=offsets.Length)throw new InvalidDataException("Native recenter child identity mismatch.");
        // All refusal checks above precede writes. Caller owns pending-frontier
        // invalidation/publication, exactly as RefreshAssemblyMass does.
        native.Pose.Position=fp;native.Velocity.Linear=fv;native.SetLocalInertia(inertia);
        for(var i=0;i<offsets.Length;i++)shape.Children[i].LocalPosition=offsets[i];native.UpdateBounds();return(dp,dv);
    }
}
