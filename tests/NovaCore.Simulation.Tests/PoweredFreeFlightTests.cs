using System.Numerics;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class PoweredFreeFlightTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Powered free flight: " + message); }
    private static BigInteger Integer(in PropellantInteger value)
    {
        BigInteger result = 0;
        for (var i = PropellantInteger.LimbCount - 1; i >= 0; i--) result = (result << 64) + value.Limb(i);
        return result;
    }
    private static PropellantInteger Exact(BigInteger value)
    {
        var result = default(PropellantInteger);
        foreach (var b in value.ToByteArray(isUnsigned: true, isBigEndian: true))
            Check(PropellantInteger.TryMultiply(result, 256, out var scaled) &&
                PropellantInteger.TryAdd(scaled, PropellantInteger.FromUInt64(b), out result), "oracle input capacity");
        return result;
    }
    // Independently test rounding-cell membership with exact BigInteger cross-products.
    private static void Ratio(BigInteger n, BigInteger d, uint factor)
    {
        Check(PoweredFlightNumerics.TryRatio(Exact(n), Exact(d), factor, out var scale), "ratio accepted");
        if (n == 0) { Check(scale.IsZero, "zero ratio"); return; }
        Check(scale.Significand >= 1 && scale.Significand < 2, "normalized ratio");
        var m = new BigInteger(scale.Significand * 4503599627370496d);
        var e = scale.Exponent - 52;
        var right = d * factor;
        var left = n;
        if (e >= 0) right <<= e; else left <<= -e;
        var error = BigInteger.Abs(left - m * right);
        Check(error * 2 <= right && (error * 2 != right || m.IsEven), "nearest-even exact ratio rounding cell");
    }
    internal static void Arithmetic()
    {
        Ratio(0, 1, 1); Ratio(1, 3, 1); Ratio(1, 1, 1_000_000);
        foreach (var bit in new[] { 1, 52, 53, 64, 1074, 2098, 2175 })
        {
            Ratio(BigInteger.One << bit, 3, 1_000_000);
            Ratio(3, BigInteger.One << bit, 1_000_000);
            Ratio((BigInteger.One << bit) + 1, (BigInteger.One << bit) - 1, 1);
        }
        var random = new Random(92425);
        for (var i = 0; i < 64; i++)
            Ratio((BigInteger.One << random.Next(2175)) + random.Next(1, 100000),
                (BigInteger.One << random.Next(2175)) + random.Next(1, 100000), 1_000_000);
        PropellantInteger.TryFromKilograms(double.Epsilon, out var fuel);
        PropellantInteger.TryDecodeFlow(2, out var flow);
        Check(PoweredFlightNumerics.TrySeconds(new(fuel, flow), out var tiny) && tiny.Exponent == -1075 &&
            tiny.Significand == 1 && tiny.Value == 0 && tiny.MultiplyDivide(16, 8) == double.Epsilon,
            "positive duration whose double seconds are zero retains one-subnormal impulse");
        PropellantInteger.TryFromKilograms(8, out var dry);
        PropellantInteger.TryFromKilograms(1d / 128, out var used);
        for (uint j = 0; j <= 256; j++)
        {
            Check(PoweredFlightNumerics.TryStageMass(dry, default, used, j, 256, out var mass), "dyadic stage mass");
            Check(mass == 8 + (256 - j) / 32768d, "independent exact dyadic mass");
        }
        Console.WriteLine("POWERED_ARITHMETIC PASS ratio_rounding=PASS stage_mass=PASS tiny_seconds=0 tiny_dv=4.9406564584124654E-324");
    }
}
