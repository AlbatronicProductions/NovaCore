using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.PartStandardExact;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold exact reference for v2 constituent mass facts; no runtime mass-state owner.</summary>
internal static class PartStandardAggregateConsistency
{
    private static readonly BigInteger Unit=BigInteger.One<<QuantumExponent;
    private static readonly BigInteger UnitSquared=Unit*Unit;
    private static readonly BigInteger MaximumFinite=Encode(double.MaxValue);
    // numerator/denominator is in units of 2^-1074. Exact midpoint tests, including
    // ties-to-even, enforce one final rounding of the constituent physical facts.
    private static bool Rounded(double authored,BigInteger numerator,BigInteger denominator)
    {
        if(!double.IsFinite(authored)||denominator<=0||BigInteger.Abs(numerator)>MaximumFinite*denominator)return false;
        var value=Encode(authored);
        var below=authored==-double.MaxValue?-(BigInteger.One<<(1024+QuantumExponent)):Encode(Math.BitDecrement(authored));
        var above=authored==double.MaxValue?BigInteger.One<<(1024+QuantumExponent):Encode(Math.BitIncrement(authored));
        var twice=numerator*2;var low=(below+value)*denominator;var high=(value+above)*denominator;
        var even=(BitConverter.DoubleToUInt64Bits(authored)&1)==0;
        return (twice>low||even&&twice==low)&&(twice<high||even&&twice==high);
    }
    private static BigInteger[] Vector(Double3 v)=>[Encode(v.X),Encode(v.Y),Encode(v.Z)];
    private static BigInteger Parallel(BigInteger[] v,int row,int column)=>row==column?
        v[(row+1)%3]*v[(row+1)%3]+v[(row+2)%3]*v[(row+2)%3]:-v[row]*v[column];
    private static double[] Tensor(Matrix3 m)=>[m.A,m.B,m.C,m.D,m.E,m.F,m.G,m.H,m.I];
    internal static void Validate(PartDefinitionData definition,ImmutableArray<MassRegionData> regions)
    {
        // Existing collection admission caps N at 4096. |scaled FP64| < 2^2098,
        // hence M < 2^2110, |S| < 2^4208, |J| < 2^6308 and |JM-P(S)| < 2^8419.
        // Midpoint comparisons need one extra carry bit; no floating cancellation occurs.
        var mass=BigInteger.Zero;BigInteger[] first=[0,0,0];var origin=new BigInteger[9];
        foreach(var region in regions)
        {
            var m=Encode(region.MassKg);var r=Vector(region.Com);var own=Tensor(region.InertiaAtCom);mass+=m;
            for(var i=0;i<3;i++)first[i]+=m*r[i];
            for(var row=0;row<3;row++)for(var col=0;col<3;col++)
                origin[row*3+col]+=Encode(own[row*3+col])*UnitSquared+m*Parallel(r,row,col);
        }
        var at=$"definition[{definition.Id}]";
        AssemblyConstructionFacts.Require(mass>0&&Rounded(definition.DryMassKg,mass,BigInteger.One),$"DRY_AGGREGATE_MASS {at}.dryMassKg");
        var com=definition.LocalCom;
        var components=new[]{com.X,com.Y,com.Z};
        for(var i=0;i<3;i++)AssemblyConstructionFacts.Require(Rounded(components[i],first[i],mass),$"DRY_AGGREGATE_COM {at}.localCom[{i}]");
        var tensor=Tensor(definition.LocalInertia);var denominator=UnitSquared*mass;
        for(var row=0;row<3;row++)for(var col=0;col<3;col++)
        {
            var numerator=origin[row*3+col]*mass-Parallel(first,row,col);
            AssemblyConstructionFacts.Require(Rounded(tensor[row*3+col],numerator,denominator),$"DRY_AGGREGATE_INERTIA {at}.localInertia[{row},{col}]");
        }
    }
}
