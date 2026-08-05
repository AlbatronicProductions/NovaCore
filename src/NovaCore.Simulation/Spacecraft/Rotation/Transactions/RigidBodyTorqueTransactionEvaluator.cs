using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Time;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Rotation.Transactions;

/// <summary>Pure exact-time rigid-body replacement construction. It never mutates state or time.</summary>
internal static class RigidBodyTorqueTransactionEvaluator
{
    internal static RigidBodyTorqueTransactionCreationResult TryCreateReplacement(SimulationStateView state, SimulationInstant end, SpacecraftId subject)
    {
        if (!state.Spacecraft.TryGetRigidBody(subject, out var current)) return new(RigidBodyTorqueTransactionStatus.SubjectNotFound, null);
        var evaluated = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(current, end);
        if (!evaluated.Succeeded) return new(RigidBodyTorqueTransactionStatus.EvaluationFailed, null);
        if (SpacecraftRigidBodyRotationState.TryCreate(subject, end, evaluated.OrientationLocalToParent, evaluated.AngularVelocityBody, current.PrincipalInertia, Double3.Zero, current.Model, out var replacement) != SpacecraftRigidBodyRotationEvaluationStatus.Success)
            return new(RigidBodyTorqueTransactionStatus.ReplacementInvalid, null);
        var status = Validate(state, end, subject, current, replacement, false);
        return status == RigidBodyTorqueTransactionStatus.Success ? new(status, new(end, state.Revision, subject, current, replacement)) : new(status, null);
    }

    /// <summary>Pure control candidate: advances the prior authoritative state to command time, then replaces only requested torque.</summary>
    internal static RigidBodyTorqueTransactionCreationResult TryCreateControlReplacement(SimulationStateView state, in SpacecraftTorqueCommand command)
    {
        if (!command.IsValid) return new(RigidBodyTorqueTransactionStatus.ReplacementInvalid, null);
        if (!state.Spacecraft.TryGetRigidBody(command.Spacecraft, out var current)) return new(RigidBodyTorqueTransactionStatus.SubjectNotFound, null);
        var evaluated = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(current, command.Time);
        if (!evaluated.Succeeded) return new(RigidBodyTorqueTransactionStatus.EvaluationFailed, null);
        if (SpacecraftRigidBodyRotationState.TryCreate(command.Spacecraft, command.Time, evaluated.OrientationLocalToParent, evaluated.AngularVelocityBody, current.PrincipalInertia, command.RequestedBodyTorque, current.Model, out var replacement) != SpacecraftRigidBodyRotationEvaluationStatus.Success)
            return new(RigidBodyTorqueTransactionStatus.ReplacementInvalid, null);
        var status = Validate(state, command.Time, command.Spacecraft, current, replacement, false);
        return status == RigidBodyTorqueTransactionStatus.Success ? new(status, new(command.Time, state.Revision, command.Spacecraft, current, replacement)) : new(status, null);
    }

    internal static RigidBodyTorqueTransactionStatus Validate(SimulationStateView state, SimulationInstant end, SpacecraftId subject, in SpacecraftRigidBodyRotationState expected, in SpacecraftRigidBodyRotationState replacement, bool requireExpected)
    {
        if (!state.Spacecraft.TryGetRigidBody(subject, out var current)) return RigidBodyTorqueTransactionStatus.SubjectNotFound;
        if (requireExpected && current != expected) return RigidBodyTorqueTransactionStatus.RotationBasisMismatch;
        if (replacement.Spacecraft != subject || replacement.Epoch != end) return RigidBodyTorqueTransactionStatus.TimeMismatch;
        if (SpacecraftRigidBodyRotationEvaluator.TryEvaluate(replacement, replacement.Epoch).Status != SpacecraftRigidBodyRotationEvaluationStatus.Success) return RigidBodyTorqueTransactionStatus.ReplacementInvalid;
        if (current == replacement) return RigidBodyTorqueTransactionStatus.ReplacementNoOp;
        return RigidBodyTorqueTransactionStatus.Success;
    }

    internal static ulong ComputeHash(in SpacecraftRigidBodyRotationState value)
    {
        ulong hash = 14695981039346656037UL;
        hash = Mix(hash, value.Spacecraft.Value); hash = Mix(hash, (ulong)value.Epoch.Ticks);
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.OrientationLocalToParent.W));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AngularVelocityBody.Z));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.ConstantBodyTorque.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.ConstantBodyTorque.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.ConstantBodyTorque.Z));
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.PrincipalInertia.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.PrincipalInertia.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.PrincipalInertia.Z)); return Mix(hash, (byte)value.Model);
    }
    private static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
}
