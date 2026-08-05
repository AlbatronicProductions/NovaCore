using NovaCore.Core;
using NovaCore.Simulation.Celestial;

namespace NovaCore.Simulation.Timeline;

/// <summary>Closed internal event payload value. It intentionally has no extensible object or byte-payload path.</summary>
internal readonly record struct SimulationEventPayload
{
    private SimulationEventPayload(SimulationEventPayloadKind kind, CelestialBodyId subject, Double3 deltaVelocity)
    {
        Kind = kind; Subject = subject; DeltaVelocity = deltaVelocity;
    }

    internal SimulationEventPayloadKind Kind { get; }
    internal CelestialBodyId Subject { get; }
    internal Double3 DeltaVelocity { get; }
    internal static SimulationEventPayload None => default;

    internal static bool TryCreateCelestialImpulse(CelestialBodyId subject, Double3 deltaVelocity, out SimulationEventPayload payload)
    {
        if (!subject.IsValid || !deltaVelocity.IsFinite || deltaVelocity.LengthSquared == 0d) { payload = default; return false; }
        payload = new(SimulationEventPayloadKind.CelestialImpulse, subject, deltaVelocity);
        return true;
    }

    internal bool IsCompatibleWith(SimulationEventKind eventKind) => eventKind switch
    {
        SimulationEventKind.Marker or SimulationEventKind.NoOpMarker or SimulationEventKind.ReplaceTrajectory => Kind == SimulationEventPayloadKind.None,
        SimulationEventKind.CelestialImpulse => Kind == SimulationEventPayloadKind.CelestialImpulse && Subject.IsValid && DeltaVelocity.IsFinite && DeltaVelocity.LengthSquared != 0d,
        _ => false,
    };
}

internal enum SimulationEventPayloadKind : byte { None = 0, CelestialImpulse = 1 }
