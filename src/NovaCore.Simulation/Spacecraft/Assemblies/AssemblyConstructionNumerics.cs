using System.Numerics;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal static class ConstructionNumerics
{
    internal static int RequiredScratchBits(ConstructionFuelNetwork network)
    {
        var d=checked(1+network.InitiallyPositive*network.RateBits);var s=checked((int)network.Scale.GetBitLength());var t=checked((int)network.TimeScale.GetBitLength());
        // kg denominator d*S*10^6*2^1074; time denominator d*T*10^6.
        // Observe can left-shift a numerator by1074 or a denominator by971,
        // then double a remainder for nearest-even rounding.
        var bits=checked(1+Math.Max(Math.Max(network.QuantityBits+1074,d+s+1094+971),Math.Max(63+d+t+1074,d+t+20+971)));
        Require(bits<=ConstructionFuelNetwork.MaximumArithmeticBits,"Physical numerical observation width exceeded.");return bits;
    }
    // Correctly rounded nonnegative rational observation. Inventory never uses
    // this conversion to make availability or debit decisions.
    internal static double Observe(BigInteger numerator,BigInteger denominator)
    {
        Require(numerator>=0&&denominator>0&&numerator.GetBitLength()<=ConstructionFuelNetwork.MaximumArithmeticBits-1076&&
            denominator.GetBitLength()<=ConstructionFuelNetwork.MaximumArithmeticBits-1076,"Invalid exact numerical observation.");
        if(numerator.IsZero)return 0;
        var exponent=checked((int)(numerator.GetBitLength()-denominator.GetBitLength()));
        if(exponent>=0?numerator<(denominator<<exponent):(numerator<<-exponent)<denominator)exponent--;
        Require(exponent<=1023,"Exact numerical observation overflows FP64.");
        if(exponent < -1075)return 0;
        var shift=Math.Max(52-exponent,-971);if(exponent < -1022)shift=1074;
        var n=shift>=0?numerator<<shift:numerator;var d=shift>=0?denominator:denominator<<-shift;
        var q=BigInteger.DivRem(n,d,out var remainder);var twice=remainder<<1;
        if(twice>d||(twice==d&&!q.IsEven))q++;
        var result=Math.ScaleB((double)q,-shift);Require(double.IsFinite(result),"Rounded numerical observation overflows FP64.");return result;
    }
    internal static double Kilograms(ConstructionFuelState state,int store)=>
        Observe(state.Quantities[store],state.Denominator*state.Network.Scale*1_000_000*(BigInteger.One<<1074));
    internal static double Seconds(ConstructionRatio ticks)=>Observe(ticks.Numerator,ticks.Denominator*1_000_000);
    internal static double[] Quantities(ConstructionFuelState state){var result=new double[state.Quantities.Length];for(var i=0;i<result.Length;i++)result[i]=Kilograms(state,i);return result;}
}
