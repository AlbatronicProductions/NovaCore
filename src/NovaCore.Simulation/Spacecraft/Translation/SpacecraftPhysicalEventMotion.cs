using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>
/// Transient immutable FP64 evaluation at an exact time identity. Revision is source provenance, not mutation
/// permission. No retained live view, contact/root proof, execution or publication authority.
/// </summary>
internal readonly record struct SpacecraftPhysicalEventMotion(
    SpacecraftId Spacecraft, PhysicalEventEpoch Epoch, StateRevision Revision, ReferenceFrameId RootFrame,
    Double3 PositionRoot, Double3 VelocityRoot, DoubleQuaternion BodyToRoot, Double3 AngularVelocityBody,
    SpacecraftPhysicalProperties Properties, PrincipalMomentsOfInertia Inertia);
