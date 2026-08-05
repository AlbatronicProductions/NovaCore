using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft;

/// <summary>Authoritative epoch attitude: XYZW active local-to-parent orientation and body-space radians/second angular velocity.</summary>
internal readonly record struct SpacecraftAttitudeState(
    SpacecraftId Spacecraft,
    SimulationInstant Epoch,
    DoubleQuaternion OrientationLocalToParent,
    Double3 AngularVelocityBody,
    SpacecraftAttitudeModel Model)
{
    internal static SpacecraftAttitudeEvaluationStatus TryCreate(
        SpacecraftId spacecraft, SimulationInstant epoch, DoubleQuaternion orientationLocalToParent,
        Double3 angularVelocityBody, SpacecraftAttitudeModel model, out SpacecraftAttitudeState state)
    {
        var status = SpacecraftAttitudeEvaluator.TryCanonicalize(orientationLocalToParent, out var canonical);
        if (status != SpacecraftAttitudeEvaluationStatus.Success) { state = default; return status; }
        if (!spacecraft.IsValid) { state = default; return SpacecraftAttitudeEvaluationStatus.InvalidSpacecraftId; }
        if (model != SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1) { state = default; return SpacecraftAttitudeEvaluationStatus.UnsupportedModel; }
        if (!angularVelocityBody.IsFinite) { state = default; return SpacecraftAttitudeEvaluationStatus.NonFiniteAngularVelocity; }
        state = new(spacecraft, epoch, canonical, angularVelocityBody, model);
        return SpacecraftAttitudeEvaluationStatus.Success;
    }
}
