using NovaCore.Core.ReferenceFrames;

namespace NovaCore.Simulation.Celestial;

internal static class CelestialSystemEvaluationHash
{
    internal static ulong Compute(ReadOnlySpan<ReferenceFrameEvaluation> values)
    {
        ulong hash = 14695981039346656037UL;
        foreach (ref readonly var value in values) { hash = Mix(hash, (ulong)value.Frame.Value); hash = MixVector(hash, value.Value.LocalToParent.Translation); hash = MixVector(hash, value.Value.OriginVelocityInParent); }
        return hash;
    }
    private static ulong MixVector(ulong hash, NovaCore.Core.Double3 value) => Mix(Mix(Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)), (ulong)BitConverter.DoubleToInt64Bits(value.Y)), (ulong)BitConverter.DoubleToInt64Bits(value.Z));
    private static ulong Mix(ulong hash, ulong value) { for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; } return hash; }
}
