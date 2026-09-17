using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Complete derived motion at one instant/revision. Stored linear and angular segment epochs may differ.</summary>
internal readonly record struct SpacecraftMotion(
    SpacecraftId Spacecraft, SimulationInstant Time, StateRevision Revision, ReferenceFrameId RootFrame,
    Double3 PositionRoot, Double3 VelocityRoot, DoubleQuaternion BodyToRoot, Double3 AngularVelocityBody,
    SpacecraftPhysicalProperties Properties, PrincipalMomentsOfInertia Inertia);

internal static class SpacecraftMotionEvaluator
{
    /// <summary>Explicit fixed material-origin observation, never a COM propagation segment.</summary>
    internal static SpacecraftTranslationStatus TryEvaluateAssembly(SimulationStateView state,SpacecraftId subject,
        SimulationInstant time,out AssemblyPublishedMotion motion)
    {
        motion=default;
        if(!state.Spacecraft.TryGetAssembly(subject,out var launch,out var value))return SpacecraftTranslationStatus.SubjectNotFound;
        if(time!=value.Epoch)return SpacecraftTranslationStatus.OutsideQualifiedEndpoint;
        motion=new(subject,launch!.Spacecraft.CarrierFrame,time,state.Revision,value.Motion,value.Mass);
        return SpacecraftTranslationStatus.Success;
    }
    /// <summary>Call within the simulation single-writer phase using a fresh state view. Output is complete or default.</summary>
    internal static SpacecraftTranslationStatus TryEvaluate(SimulationStateView state, SpacecraftId subject,
        SimulationInstant time, out SpacecraftMotion motion)
    {
        motion = default;
        if(state.Spacecraft.TryGetAssembly(subject,out _,out _))return SpacecraftTranslationStatus.AssemblyMotionRequiresTypedView;
        if (state.Spacecraft.TryGetAppliedEndpoint(subject, out var endpoint))
        {
            if (time != endpoint.Epoch) return SpacecraftTranslationStatus.OutsideQualifiedEndpoint;
            motion = new(subject, time, state.Revision, endpoint.RootFrame, endpoint.PositionRoot, endpoint.VelocityRoot,
                endpoint.BodyToRoot, endpoint.AngularVelocityBody, endpoint.Properties, endpoint.Inertia);
            return SpacecraftTranslationStatus.Success;
        }
        if (!state.Spacecraft.TryGetTranslation(subject, out var translation, out var properties) ||
            !state.Spacecraft.TryGetRigidBody(subject, out var rotation)) return SpacecraftTranslationStatus.SubjectNotFound;
        if (time < rotation.Epoch) return SpacecraftTranslationStatus.TimeBeforeEpoch;
        var linear = SpacecraftTranslationEvaluator.TryEvaluate(translation, properties, time);
        if (!linear.Succeeded) return linear.Status;
        var angular = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, time);
        if (!angular.Succeeded) return SpacecraftTranslationStatus.RotationEvaluationFailed;
        motion = new(subject, time, state.Revision, translation.RootFrame, linear.PositionRoot, linear.VelocityRoot,
            angular.OrientationLocalToParent, angular.AngularVelocityBody, properties, rotation.PrincipalInertia);
        return SpacecraftTranslationStatus.Success;
    }
}

internal readonly record struct AssemblyPublishedMotion(SpacecraftId Spacecraft,ReferenceFrameId RootFrame,
    SimulationInstant Time,StateRevision Revision,AssemblyMotion MaterialOriginMotion,AssemblyMass CurrentMass)
{
    internal Double3 CenterOfMassPositionRoot=>MaterialOriginMotion.PositionO+MaterialOriginMotion.BodyToWorld.Rotate(CurrentMass.Com);
    // Rigid material velocity here is not the derivative of the migrating COM coordinate.
    internal Double3 MaterialVelocityAtCurrentComRoot=>MaterialOriginMotion.VelocityO+
        MaterialOriginMotion.BodyToWorld.Rotate(Double3.Cross(MaterialOriginMotion.AngularVelocityBody,CurrentMass.Com));
}
