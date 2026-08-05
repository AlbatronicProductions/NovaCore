using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Transactions;

/// <summary>Pure spacecraft-attitude candidate construction; it never writes state, clock, or timeline.</summary>
internal static class SpacecraftAttitudeTransactionEvaluator
{
    internal static SpacecraftAttitudeTransactionCreationResult TryCreateReplacement(SimulationStateView state, SimulationInstant evaluationTime, SpacecraftId subject, SpacecraftAttitudeState replacement)
    {
        if (!state.Spacecraft.TryGetAttitude(subject, out var current)) return new(SpacecraftAttitudeTransactionStatus.SubjectNotFound, null);
        var status = Validate(state, evaluationTime, subject, current, replacement, false);
        return status == SpacecraftAttitudeTransactionStatus.Success ? new(status, new(evaluationTime, state.Revision, subject, current, replacement)) : new(status, null);
    }

    internal static SpacecraftAttitudeTransactionStatus Validate(SimulationStateView state, SimulationInstant evaluationTime, SpacecraftId subject, in SpacecraftAttitudeState expected, in SpacecraftAttitudeState replacement, bool requireExpected)
    {
        if (!state.Spacecraft.TryGetAttitude(subject, out var current)) return SpacecraftAttitudeTransactionStatus.SubjectNotFound;
        if (requireExpected && current != expected) return SpacecraftAttitudeTransactionStatus.AttitudeBasisMismatch;
        if (replacement.Spacecraft != subject || replacement.Epoch != evaluationTime) return SpacecraftAttitudeTransactionStatus.TimeMismatch;
        if (SpacecraftAttitudeState.TryCreate(replacement.Spacecraft, replacement.Epoch, replacement.OrientationLocalToParent, replacement.AngularVelocityBody, replacement.Model, out var canonical) != SpacecraftAttitudeEvaluationStatus.Success || canonical != replacement) return SpacecraftAttitudeTransactionStatus.ReplacementInvalid;
        if (current == replacement) return SpacecraftAttitudeTransactionStatus.ReplacementNoOp;
        return SpacecraftAttitudeTransactionStatus.Success;
    }

    internal static ulong ComputeHash(in SpacecraftAttitudeState value)
    {
        ulong hash = 14695981039346656037UL;
        hash = Mix(hash, value.Spacecraft.Value); hash = Mix(hash, (ulong)value.Epoch.Ticks);
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.W));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.Z)); return Mix(hash, (byte)value.Model);
    }
    private static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
}
