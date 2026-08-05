using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Rotation;

/// <summary>Immutable authoritative input for bounded pure rigid-body rotational evaluation.</summary>
internal readonly record struct SpacecraftRigidBodyRotationState(
    SpacecraftId Spacecraft,
    SimulationInstant Epoch,
    DoubleQuaternion OrientationLocalToParent,
    Double3 AngularVelocityBody,
    PrincipalMomentsOfInertia PrincipalInertia,
    Double3 ConstantBodyTorque,
    RigidBodyRotationModel Model)
{
    internal static SpacecraftRigidBodyRotationEvaluationStatus TryCreate(
        SpacecraftId spacecraft, SimulationInstant epoch, DoubleQuaternion orientationLocalToParent,
        Double3 angularVelocityBody, PrincipalMomentsOfInertia principalInertia,
        Double3 constantBodyTorque, RigidBodyRotationModel model,
        out SpacecraftRigidBodyRotationState state)
    {
        state = default;
        if (!spacecraft.IsValid) return SpacecraftRigidBodyRotationEvaluationStatus.InvalidSpacecraftId;
        if (model != RigidBodyRotationModel.ConstantBodyTorqueV1) return SpacecraftRigidBodyRotationEvaluationStatus.UnsupportedModel;
        var orientationStatus = SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(orientationLocalToParent, out var canonical);
        if (orientationStatus != SpacecraftRigidBodyRotationEvaluationStatus.Success) return orientationStatus;
        if (!angularVelocityBody.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteAngularVelocity;
        if (!constantBodyTorque.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteTorque;
        if (!principalInertia.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteInertia;
        if (!principalInertia.IsStrictlyPositive) return SpacecraftRigidBodyRotationEvaluationStatus.NonPositiveInertia;
        state = new(spacecraft, epoch, canonical, angularVelocityBody, principalInertia, constantBodyTorque, model);
        return SpacecraftRigidBodyRotationEvaluationStatus.Success;
    }
}
