using System.Numerics;
using BepuPhysics.Collidables;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Contact-consistent FP32 encoding of an exactly coplanar initial
/// supported pose. This never changes the canonical FP64 pose or velocity.</summary>
internal static class CraftSupportImport
{
    internal static Vector3 Encode(Shapes shapes,TypedIndex shape,CompiledCraftContact profile,
        Double3 exact,float slabHalfHeight)
    {
        var result=new Vector3((float)exact.X,(float)exact.Y,(float)exact.Z);
        ref var compound=ref shapes.GetShape<Compound>(shape.Index);
        Span<Vector2> supportBounds=stackalloc Vector2[compound.Children.Length];var count=0;
        for(var i=0;i<compound.Children.Length;i++){
            if(!profile.Craft.Collision[i].Vertices.Any(p=>Math.Abs(p.X-profile.SupportPlane)<=1e-12))continue;
            ref var child=ref compound.Children[i];
            shapes.GetShape<ConvexHull>(child.ShapeIndex.Index).ComputeBounds(Quaternion.Identity,out var min,out _);
            supportBounds[count++]=new(min.Y,child.LocalPosition.Y);
        }
        supportBounds=supportBounds[..count];
        if(count==0||!float.IsFinite(slabHalfHeight)||slabHalfHeight<=0)
            throw new InvalidDataException("Initial support import has no finite contact bounds.");
        // Pinned ConvexCompoundOverlapFinder expands its child query by motion,
        // capped by speculative margin; it has NO minimum margin at rest.
        // The world broadphase, child query and child-relative face each use
        // a different FP32 addition tree. None implies the other two at rest.
        if(!Overlaps(supportBounds,result.Y,slabHalfHeight)){
            var allowance=profile.ContactTolerance/16;
            var dx=result.X-exact.X;var dz=result.Z-exact.Z;
            var remaining=allowance*allowance-dx*dx-dz*dz;
            if(!double.IsFinite(remaining)||remaining<0)throw new InvalidDataException("Contact-consistent initial support import exceeds the precision envelope.");
            var lower=exact.Y-Math.Sqrt(remaining);var lowFloat=(float)lower;
            if(lowFloat<lower)lowFloat=MathF.BitIncrement(lowFloat);
            if(!float.IsFinite(lowFloat)||!Overlaps(supportBounds,lowFloat,slabHalfHeight))
                throw new InvalidDataException("Contact-consistent initial support import exceeds the precision envelope.");
            // Search the ordered IEEE binary32 domain, not adjacent values.
            // At most 32 rank decisions even for a COM very near the plane.
            var low=Rank(lowFloat);var high=Rank(result.Y);
            while(low<high){
                var middle=(uint)(low+((ulong)high-low+1)/2);var y=Value(middle);
                if(Overlaps(supportBounds,y,slabHalfHeight))low=middle;else high=middle-1;
            }
            result.Y=Value(low);
        }
        Check();return result;
        void Check(){
            var error=new Double3(result.X-exact.X,result.Y-exact.Y,result.Z-exact.Z);
            if(!float.IsFinite(result.Y)||Math.Sqrt(error.LengthSquared)>profile.ContactTolerance/16)
                throw new InvalidDataException("Contact-consistent initial support import exceeds the precision envelope.");
        }
    }
    private static bool Overlaps(ReadOnlySpan<Vector2> supportBounds,float bodyY,float half)
    {
        var bodyFromSlab=bodyY+half;var queryMax=half-bodyFromSlab;
        foreach(var bounds in supportBounds){
            var childBottom=bounds.X+bounds.Y;
            if(!float.IsFinite(childBottom)||childBottom+bodyY>0||childBottom>queryMax||
                bounds.X>half-(bounds.Y+bodyFromSlab))return false;
        }
        return true;
    }
    private static uint Rank(float value){var bits=BitConverter.SingleToUInt32Bits(value);return (bits&0x80000000)==0?bits^0x80000000:~bits;}
    private static float Value(uint rank)=>BitConverter.UInt32BitsToSingle((rank&0x80000000)==0?~rank:rank^0x80000000);
}
