using System.Numerics;
using System.Collections.Immutable;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold sign predicates on exact authored binary64 physical metadata.</summary>
internal static class PartStandardExact
{
    internal const int QuantumExponent=1074;
    internal static BigInteger Encode(double value)
    {
        if(!double.IsFinite(value))throw new InvalidDataException("Nonfinite physical input.");
        var bits=BitConverter.DoubleToUInt64Bits(value);var exponent=(int)((bits>>52)&2047);
        var mantissa=new BigInteger(bits&0xfffffffffffffUL);
        if(exponent!=0)mantissa=(mantissa+(BigInteger.One<<52))<<(exponent-1);
        return (bits>>63)==0?mantissa:-mantissa;
    }
    private static bool Positive(BigInteger a,BigInteger b,BigInteger c,BigInteger e,BigInteger f,BigInteger i)=>
        a>0&&a*e-b*b>0&&a*(e*i-f*f)-b*(b*i-c*f)+c*(b*f-c*e)>0;
    internal static bool PhysicalInertia(Matrix3 m)
    {
        if(!m.Finite||!m.Symmetric)return false;
        var a=Encode(m.A);var b=Encode(m.B);var c=Encode(m.C);var e=Encode(m.E);var f=Encode(m.F);var i=Encode(m.I);
        // Sylvester's criterion for I and twice the central second-moment tensor.
        // The existing nondegenerate 3D inertia policy is preserved, without rounded signs.
        return Positive(a,b,c,e,f,i)&&Positive(e+i-a,-2*b,-2*c,a+i-e,-2*f,a+e-i);
    }
    internal static bool AxialOverlap(double a,double lengthA,double b,double lengthB)=>
        BigInteger.Abs(Encode(a)-Encode(b))*2<Encode(lengthA)+Encode(lengthB);
    internal static bool FullRank(ImmutableArray<Double3> points)
    {
        var origin=points[0];var ox=Encode(origin.X);var oy=Encode(origin.Y);var oz=Encode(origin.Z);
        var vectors=points.Skip(1).Select(p=>new[]{Encode(p.X)-ox,Encode(p.Y)-oy,Encode(p.Z)-oz}).ToArray();
        // Unique vertices guarantee a nonzero first edge. The first independent
        // pair determines a plane; a single point outside it establishes rank 3.
        var a=vectors[0];
        for(var j=1;j<vectors.Length;j++){
            var b=vectors[j];var nx=a[1]*b[2]-a[2]*b[1];var ny=a[2]*b[0]-a[0]*b[2];var nz=a[0]*b[1]-a[1]*b[0];
            if(nx==0&&ny==0&&nz==0)continue;
            return vectors.Any(c=>nx*c[0]+ny*c[1]+nz*c[2]!=0);
        }
        return false;
    }
}
