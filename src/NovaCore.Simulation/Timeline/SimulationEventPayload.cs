using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;

namespace NovaCore.Simulation.Timeline;

/// <summary>Closed internal event payload value. It intentionally has no extensible object or byte-payload path.</summary>
internal readonly record struct SimulationEventPayload
{
    private SimulationEventPayload(SimulationEventPayloadKind kind, CelestialBodyId subject, SpacecraftId spacecraftSubject, Double3 deltaVelocity)
    {
        Kind = kind; Subject = subject; SpacecraftSubject = spacecraftSubject; DeltaVelocity = deltaVelocity;
    }

    internal SimulationEventPayloadKind Kind { get; }
    internal CelestialBodyId Subject { get; }
    internal SpacecraftId SpacecraftSubject { get; }
    internal Double3 DeltaVelocity { get; }
    internal static SimulationEventPayload None => default;

    internal static bool TryCreateCelestialImpulse(CelestialBodyId subject, Double3 deltaVelocity, out SimulationEventPayload payload)
    {
        if (!subject.IsValid || !deltaVelocity.IsFinite || deltaVelocity.LengthSquared == 0d) { payload = default; return false; }
        payload = new(SimulationEventPayloadKind.CelestialImpulse, subject, SpacecraftId.Invalid, deltaVelocity);
        return true;
    }
    internal static bool TryCreateRigidBodyTorque(SpacecraftId subject, out SimulationEventPayload payload)
    { if (!subject.IsValid) { payload = default; return false; } payload = new(SimulationEventPayloadKind.RigidBodyTorque, CelestialBodyId.Invalid, subject, Double3.Zero); return true; }

    internal bool IsCompatibleWith(SimulationEventKind eventKind) => eventKind switch
    {
        SimulationEventKind.Marker or SimulationEventKind.NoOpMarker or SimulationEventKind.ReplaceTrajectory => Kind == SimulationEventPayloadKind.None,
        SimulationEventKind.CelestialImpulse => Kind == SimulationEventPayloadKind.CelestialImpulse && Subject.IsValid && DeltaVelocity.IsFinite && DeltaVelocity.LengthSquared != 0d,
        SimulationEventKind.RigidBodyTorque => Kind == SimulationEventPayloadKind.RigidBodyTorque && SpacecraftSubject.IsValid,
        _ => false,
    };
}

internal enum SimulationEventPayloadKind : byte { None = 0, CelestialImpulse = 1, RigidBodyTorque = 2 }
