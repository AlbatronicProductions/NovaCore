using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable node-to-catalog reference. Nodes carry no model payload.</summary>
internal readonly record struct CelestialEphemerisBinding(CelestialTrajectoryModel Model, CelestialEphemerisSourceId SourceId, int PayloadIndex)
{
    internal bool IsDefault => !SourceId.IsValid;
}

/// <summary>One declared immutable source and the one model catalog it supplies.</summary>
internal readonly record struct CelestialEphemerisSource(CelestialEphemerisSourceId Id, CelestialTrajectoryModel Model, CelestialEphemerisMetadata Metadata);

/// <summary>Parent-relative static state. Orientation is local-to-parent.</summary>
internal readonly record struct FixedBodyEphemerisPayload(Double3 Position, Double3 Velocity, DoubleQuaternion Orientation, Double3 AngularVelocity)
{
    internal bool IsFinite => Position.IsFinite && Velocity.IsFinite && Orientation.IsFinite && AngularVelocity.IsFinite;
    internal bool IsCanonical => IsFinite && Orientation.LengthSquared > 0d && Math.Abs(Orientation.LengthSquared - 1d) <= 1e-12d &&
        (Orientation.W > 0d || (Orientation.W == 0d && (Orientation.X > 0d || (Orientation.X == 0d && (Orientation.Y > 0d || (Orientation.Y == 0d && Orientation.Z >= 0d))))));
    internal static FixedBodyEphemerisPayload Identity => new(Double3.Zero, Double3.Zero, DoubleQuaternion.Identity, Double3.Zero);
}

/// <summary>Immutable circular-orbit parameters in the owning system's ephemeris domain.</summary>
internal readonly record struct CircularOrbitEphemerisPayload(long EpochDomainTicks, double Radius, double InitialPhaseRadians, DoubleQuaternion PlaneOrientation, double CentralGravitationalParameter)
{
    internal bool IsValid => double.IsFinite(Radius) && Radius > 0d && double.IsFinite(InitialPhaseRadians) && PlaneOrientation.IsFinite &&
        PlaneOrientation.LengthSquared > 0d && Math.Abs(PlaneOrientation.LengthSquared - 1d) <= 1e-12d && double.IsFinite(CentralGravitationalParameter) && CentralGravitationalParameter > 0d;

    // Temporary 9A-3B adapter: fixtures use the identity time mapping. 9A-3C replaces this with mapped-domain dispatch.
    internal TwoBodyTrajectory ToLegacyTrajectory(CelestialBodyId centralBody)
    {
        var localPosition = new Double3(Radius * Math.Cos(InitialPhaseRadians), Radius * Math.Sin(InitialPhaseRadians), 0d);
        var speed = Math.Sqrt(CentralGravitationalParameter / Radius);
        var localVelocity = new Double3(-speed * Math.Sin(InitialPhaseRadians), speed * Math.Cos(InitialPhaseRadians), 0d);
        return new TwoBodyTrajectory(centralBody, new SimulationInstant(EpochDomainTicks), new CartesianState(PlaneOrientation.Rotate(localPosition), PlaneOrientation.Rotate(localVelocity)), TwoBodyPropagationModel.CartesianTwoBodyV1);
    }
}
