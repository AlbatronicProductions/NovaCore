using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Encloses the actual pinned discrete compound/mesh queries, including
/// mesh reduction's additional contact-centred triangle queries. No part size,
/// craft depth or whole-tile padding determines the footprint.</summary>
internal static class CraftTerrainFootprint
{
    internal readonly record struct Bounds(double MinX,double MinY,double MaxX,double MaxY,bool AboveGrade);
    private readonly record struct Range(double Low,double High)
    {
        public static implicit operator Range(double x)=>new(x,x);
        private static Range Out(double low,double high)=>new(Math.BitDecrement(low),Math.BitIncrement(high));
        public static Range operator +(Range a,Range b)=>Out(a.Low+b.Low,a.High+b.High);
        public static Range operator -(Range a,Range b)=>Out(a.Low-b.High,a.High-b.Low);
        public static Range operator *(Range a,Range b)
        {var aa=a.Low*b.Low;var ab=a.Low*b.High;var ba=a.High*b.Low;var bb=a.High*b.High;return Out(Math.Min(Math.Min(aa,ab),Math.Min(ba,bb)),Math.Max(Math.Max(aa,ab),Math.Max(ba,bb)));}
        public static Range operator /(Range a,Range b)
        {if(b.Low<=0)throw new InvalidDataException("Terrain footprint denominator is not positive.");return a*Out(1/b.High,1/b.Low);}
    }
    private readonly record struct Vector(Range X,Range Y,Range Z)
    {
        public static implicit operator Vector(Double3 p)=>new(p.X,p.Y,p.Z);
        public static Vector operator +(Vector a,Vector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static Vector operator *(Vector a,Range b)=>new(a.X*b,a.Y*b,a.Z*b);
        internal static Vector Cross(Vector a,Vector b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal Range Dot(Double3 b)=>X*b.X+Y*b.Y+Z*b.Z;
    }
    // Forward bound on the original outward interval expression's endpoints.
    // M bounds exact magnitude; E bounds distance from exact evaluation. Two
    // ULPs cover round-to-nearest plus Range.Out's adjacent representable value.
    private readonly record struct Error(double M,double E)
    {
        static double U(double x)=>Math.BitIncrement(x);
        internal static double Round(double x)=>U(2*(Math.BitIncrement(x)-x));
        public static implicit operator Error(double x)=>new(Math.Abs(x),0);
        public static Error operator +(Error a,Error b)
        {var m=U(a.M+b.M);var e=U(a.E+b.E);return new(m,U(e+Round(U(m+e))));}
        public static Error operator -(Error a,Error b)=>a+b;
        public static Error operator *(Error a,Error b)
        {
            var m=U(a.M*b.M);var e=U(U(U(a.M*b.E)+U(b.M*a.E))+U(a.E*b.E));
            return new(m,U(e+Round(U(U(a.M+a.E)*U(b.M+b.E)))));
        }
        internal Error Reciprocal(double lower)
        {
            var remaining=Math.BitDecrement(lower-E);
            if(!(remaining>0))throw new InvalidDataException("Unresolved projection roundoff denominator.");
            var m=U(1/lower);var e=U(E/Math.BitDecrement(lower*remaining));
            return new(m,U(e+Round(U(1/remaining))));
        }
    }
    private readonly record struct ErrorVector(Error X,Error Y,Error Z)
    {
        public static implicit operator ErrorVector(Double3 p)=>new(p.X,p.Y,p.Z);
        public static ErrorVector operator +(ErrorVector a,ErrorVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static ErrorVector operator *(ErrorVector a,Error b)=>new(a.X*b,a.Y*b,a.Z*b);
        internal static ErrorVector Cross(ErrorVector a,ErrorVector b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal Error Dot(Double3 b)=>X*b.X+Y*b.Y+Z*b.Z;
    }
    internal static Bounds Compute(BepuPhysics.Simulation simulation,BodyHandle body,TypedIndex shape,AssemblyFloridaSite site,Double3 origin,double reach)
        =>Compute(simulation,body,shape,site,origin,reach,false);
    // A cheaper enclosure is used only to prove reuse of an already prepared
    // mesh. A miss always uses the original per-child footprint below.
    internal static Bounds Union(BepuPhysics.Simulation simulation,BodyHandle body,TypedIndex shape,AssemblyFloridaSite site,Double3 origin,double reach)
        =>Compute(simulation,body,shape,site,origin,reach,true);
    private static Bounds Compute(BepuPhysics.Simulation simulation,BodyHandle body,TypedIndex shape,AssemblyFloridaSite site,Double3 origin,double reach,bool union)
    {
        var native=simulation.Bodies[body];ref var compound=ref simulation.Shapes.GetShape<Compound>(shape.Index);
        var orientationA=new QuaternionWide();QuaternionWide.WriteFirst(native.Pose.Orientation,ref orientationA);
        var identity=new QuaternionWide();QuaternionWide.WriteFirst(Quaternion.Identity,ref identity);
        QuaternionWide.Conjugate(identity,out var toLocalB);QuaternionWide.ConcatenateWithoutOverlap(orientationA,toLocalB,out var localA);
        var offsetB=new Vector3Wide();Vector3Wide.WriteFirst(-native.Pose.Position,ref offsetB);QuaternionWide.TransformWithoutOverlap(offsetB,toLocalB,out var localOffsetB);
        var margin=native.Collidable.MaximumSpeculativeMargin;
        var expanded=MathF.Max(margin,MathF.BitIncrement((float)reach));
        if(!float.IsFinite(expanded)||expanded<0)throw new InvalidDataException("Nonfinite native terrain reach.");
        var region=FloridaFacilitySupport.Region;var q=site.LocalToBodyFixed.Normalized();Vector axis=new Double3(q.X,q.Y,q.Z);
        var unionLow=new Double3(double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity);var unionHigh=-unionLow;
        var x0=double.PositiveInfinity;var y0=x0;var x1=double.NegativeInfinity;var y1=x1;var above=true;var radialLower=double.PositiveInfinity;
        static float Minimum(float a,float expansion,float position)=>MathF.BitDecrement(MathF.BitDecrement(a-expansion)+position);
        static float Maximum(float a,float expansion,float position)=>MathF.BitIncrement(MathF.BitIncrement(a+expansion)+position);
        for(var childIndex=0;childIndex<compound.Children.Length;childIndex++)
        {
            ref var child=ref compound.Children[childIndex];
            var childQ=new QuaternionWide();QuaternionWide.WriteFirst(child.LocalOrientation,ref childQ);
            var childP=new Vector3Wide();Vector3Wide.WriteFirst(child.LocalPosition,ref childP);
            QuaternionWide.ConcatenateWithoutOverlap(childQ,localA,out var rotation);
            QuaternionWide.TransformWithoutOverlap(childP,localA,out var offset);Vector3Wide.Subtract(offset,localOffsetB,out var position);
            QuaternionWide.ReadFirst(rotation,out var actualRotation);Vector3Wide.ReadFirst(position,out var actualPosition);
            simulation.Shapes[child.ShapeIndex.Type].ComputeBounds(child.ShapeIndex.Index,actualRotation,out var low,out var high);
            var nativeLow=new Vector3(Minimum(low.X,margin,actualPosition.X),Minimum(low.Y,margin,actualPosition.Y),Minimum(low.Z,margin,actualPosition.Z));
            var nativeHigh=new Vector3(Maximum(high.X,margin,actualPosition.X),Maximum(high.Y,margin,actualPosition.Y),Maximum(high.Z,margin,actualPosition.Z));
            var span=MathF.Max(MathF.BitIncrement(nativeHigh.X-nativeLow.X),MathF.Max(MathF.BitIncrement(nativeHigh.Y-nativeLow.Y),MathF.BitIncrement(nativeHigh.Z-nativeLow.Z)));
            var smoothing=MathF.BitIncrement(span*1e-4f); // pinned MeshReduction neighborhood half-width
            low=new(Minimum(low.X,expanded,actualPosition.X),Minimum(low.Y,expanded,actualPosition.Y),Minimum(low.Z,expanded,actualPosition.Z));
            high=new(Maximum(high.X,expanded,actualPosition.X),Maximum(high.Y,expanded,actualPosition.Y),Maximum(high.Z,expanded,actualPosition.Z));
            if(union)
            {
                // Enclose the original binary64 +/- smoothing operation too.
                Double3 lo=new(Math.BitDecrement((double)low.X-smoothing),Math.BitDecrement((double)low.Y-smoothing),Math.BitDecrement((double)low.Z-smoothing));
                Double3 hi=new(Math.BitIncrement((double)high.X+smoothing),Math.BitIncrement((double)high.Y+smoothing),Math.BitIncrement((double)high.Z+smoothing));
                unionLow=new(Math.Min(unionLow.X,lo.X),Math.Min(unionLow.Y,lo.Y),Math.Min(unionLow.Z,lo.Z));
                unionHigh=new(Math.Max(unionHigh.X,hi.X),Math.Max(unionHigh.Y,hi.Y),Math.Max(unionHigh.Z,hi.Z));
            }
            else for(var corner=0;corner<8;corner++)
            {
                Range Coordinate(float lo,float hi,int bit)=>((Range)((corner&bit)==0?lo:hi))+((corner&bit)==0?-(double)smoothing:smoothing);
                Project(new(Coordinate(low.X,high.X,1),Coordinate(low.Y,high.Y,2),Coordinate(low.Z,high.Z,4)));
            }
        }
        if(union)
        {
            if(!unionLow.IsFinite||!unionHigh.IsFinite)throw new InvalidDataException("Invalid native query union.");
            // Rotation/translation is affine. With positive radial denominator,
            // each gnomonic coordinate has its extrema at a box vertex.
            for(var corner=0;corner<8;corner++)
            {
                Range Coordinate(double lo,double hi,int bit)=>(Range)((corner&bit)==0?lo:hi)+0;
                Project(new(Coordinate(unionLow.X,unionHigh.X,1),Coordinate(unionLow.Y,unionHigh.Y,2),Coordinate(unionLow.Z,unionHigh.Z,4)));
            }
            // Geometric extrema alone need not contain every legacy interval's
            // rounding width. Bound that width from its actual expression tree.
            Error Input(double lo,double hi){var m=Math.Max(Math.Abs(lo),Math.Abs(hi));return new(m,Error.Round(m));}
            ErrorVector local=new(Input(unionLow.X,unionHigh.X),Input(unionLow.Y,unionHigh.Y),Input(unionLow.Z,unionHigh.Z));
            local+=(ErrorVector)origin;ErrorVector a=new Double3(q.X,q.Y,q.Z);
            var point=(ErrorVector)site.OriginBodyFixed+local+ErrorVector.Cross(a,ErrorVector.Cross(a,local)+local*q.W)*2;
            var reciprocal=point.Dot(region.Up).Reciprocal(radialLower);
            var ex=(point.Dot(region.East)*region.RadiusMetres*reciprocal).E;
            var ey=(point.Dot(region.North)*region.RadiusMetres*reciprocal).E;
            if(!double.IsFinite(ex+ey))throw new InvalidDataException("Nonfinite projection roundoff bound.");
            x0=Math.BitDecrement(x0-ex);x1=Math.BitIncrement(x1+ex);y0=Math.BitDecrement(y0-ey);y1=Math.BitIncrement(y1+ey);
            above=Math.BitDecrement(radialLower-point.Dot(region.Up).E)>region.RadiusMetres+region.PlaneAltitudeMetres&&
                x0> -region.InnerEastMetres&&x1<region.InnerEastMetres&&y0> -region.InnerNorthMetres&&y1<region.InnerNorthMetres;
        }
        void Project(Vector local)
        {
            local+=(Vector)origin;
            var point=(Vector)site.OriginBodyFixed+local+Vector.Cross(axis,Vector.Cross(axis,local)+local*q.W)*2;
            var up=point.Dot(region.Up);
            radialLower=Math.Min(radialLower,up.Low);
            if(up.Low<=region.RadiusMetres*.5)throw new InvalidDataException("Native terrain query leaves its radial patch.");
            var x=point.Dot(region.East)*region.RadiusMetres/up;var y=point.Dot(region.North)*region.RadiusMetres/up;
            x0=Math.Min(x0,x.Low);x1=Math.Max(x1,x.High);y0=Math.Min(y0,y.Low);y1=Math.Max(y1,y.High);
            above&=up.Low>region.RadiusMetres+region.PlaneAltitudeMetres&&x.Low>-region.InnerEastMetres&&x.High<region.InnerEastMetres&&y.Low>-region.InnerNorthMetres&&y.High<region.InnerNorthMetres;
        }
        return new(x0,y0,x1,y1,above);
    }
}
