namespace NovaCore.Simulation.Celestial;

/// <summary>Raw-value deterministic contract hash for tests and future replay verification.</summary>
internal static class CelestialContractHash
{
    public static ulong Compute(in CelestialStateView view)
    {
        ulong hash = 14695981039346656037UL;
        hash = Mix(hash, (ulong)view.Count);
        for (var index = 0; index < view.Count; index++)
        {
            var definition = view.GetDefinition(index); var state = view.GetState(index);
            hash = Mix(hash, definition.Id.Value); hash = Mix(hash, definition.PrimaryBody?.Value ?? 0UL); hash = Mix(hash, (ulong)definition.InertialFrame.Value); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(definition.GravitationalParameter));
            hash = Mix(hash, state.Id.Value); hash = Mix(hash, state.Trajectory is null ? 0UL : 1UL);
            if (state.Trajectory is { } trajectory)
            {
                hash = Mix(hash, trajectory.CentralBody.Value); hash = Mix(hash, (ulong)trajectory.Epoch.Ticks); hash = Mix(hash, (ulong)trajectory.Model);
                hash = MixVector(hash, trajectory.StateAtEpoch.Position); hash = MixVector(hash, trajectory.StateAtEpoch.Velocity);
            }
        }
        return hash;
    }

    private static ulong MixVector(ulong hash, NovaCore.Core.Double3 value) => Mix(Mix(Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)), (ulong)BitConverter.DoubleToInt64Bits(value.Y)), (ulong)BitConverter.DoubleToInt64Bits(value.Z));
    private static ulong Mix(ulong hash, ulong value) { for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; } return hash; }
}
