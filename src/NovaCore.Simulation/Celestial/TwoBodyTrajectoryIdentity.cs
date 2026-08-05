namespace NovaCore.Simulation.Celestial;

/// <summary>Raw deterministic comparison and hashing for authoritative trajectory values.</summary>
internal static class TwoBodyTrajectoryIdentity
{
    internal static bool EqualsRaw(in TwoBodyTrajectory left, in TwoBodyTrajectory right) =>
        left.CentralBody == right.CentralBody &&
        left.Epoch == right.Epoch &&
        left.Model == right.Model &&
        EqualsRaw(left.StateAtEpoch, right.StateAtEpoch);

    internal static bool EqualsRaw(in CartesianState left, in CartesianState right) =>
        BitConverter.DoubleToInt64Bits(left.Position.X) == BitConverter.DoubleToInt64Bits(right.Position.X) &&
        BitConverter.DoubleToInt64Bits(left.Position.Y) == BitConverter.DoubleToInt64Bits(right.Position.Y) &&
        BitConverter.DoubleToInt64Bits(left.Position.Z) == BitConverter.DoubleToInt64Bits(right.Position.Z) &&
        BitConverter.DoubleToInt64Bits(left.Velocity.X) == BitConverter.DoubleToInt64Bits(right.Velocity.X) &&
        BitConverter.DoubleToInt64Bits(left.Velocity.Y) == BitConverter.DoubleToInt64Bits(right.Velocity.Y) &&
        BitConverter.DoubleToInt64Bits(left.Velocity.Z) == BitConverter.DoubleToInt64Bits(right.Velocity.Z);

    internal static ulong ComputeHash(in TwoBodyTrajectory value)
    {
        ulong hash = 14695981039346656037UL;
        hash = Mix(hash, value.CentralBody.Value);
        hash = Mix(hash, (ulong)value.Epoch.Ticks);
        hash = Mix(hash, (ulong)value.Model);
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Position.X));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Position.Y));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Position.Z));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Velocity.X));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Velocity.Y));
        return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.StateAtEpoch.Velocity.Z));
    }

    private static ulong Mix(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; }
        return hash;
    }
}
