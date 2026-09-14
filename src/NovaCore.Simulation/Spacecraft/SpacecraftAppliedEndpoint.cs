using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft;

internal enum AppliedEndpointValidity { Invalid, EndpointOnly }
/// <summary>Canonical instantaneous values. Intentionally has no legacy force law or conversion to a propagation segment.</summary>
internal readonly record struct SpacecraftAppliedEndpoint(SpacecraftId Spacecraft, ReferenceFrameId RootFrame,
    SimulationInstant Epoch, Double3 PositionRoot, Double3 VelocityRoot, DoubleQuaternion BodyToRoot,
    Double3 AngularVelocityBody, SpacecraftPhysicalProperties Properties, PrincipalMomentsOfInertia Inertia,
    AppliedEndpointValidity Validity)
{
    internal bool SameBits(in SpacecraftAppliedEndpoint other) => Spacecraft == other.Spacecraft && RootFrame == other.RootFrame &&
        Epoch == other.Epoch && Validity == other.Validity && Bits(PositionRoot, other.PositionRoot) &&
        Bits(VelocityRoot, other.VelocityRoot) && Bits(AngularVelocityBody, other.AngularVelocityBody) &&
        Bits(BodyToRoot.X, other.BodyToRoot.X) && Bits(BodyToRoot.Y, other.BodyToRoot.Y) &&
        Bits(BodyToRoot.Z, other.BodyToRoot.Z) && Bits(BodyToRoot.W, other.BodyToRoot.W) &&
        Bits(Properties.MassKilograms, other.Properties.MassKilograms) &&
        Bits(Inertia.X, other.Inertia.X) && Bits(Inertia.Y, other.Inertia.Y) && Bits(Inertia.Z, other.Inertia.Z);
    private static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a, Double3 b) => Bits(a.X, b.X) && Bits(a.Y, b.Y) && Bits(a.Z, b.Z);
}

/// <summary>Discriminated copied source for command/engine/resource authority. Never manufactures a legacy segment from an endpoint.</summary>
internal readonly record struct SpacecraftPhysicalSource(bool IsEndpoint, SpacecraftAppliedEndpoint Endpoint,
    SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular, SpacecraftPhysicalProperties Properties)
{
    internal ReferenceFrameId RootFrame => IsEndpoint ? Endpoint.RootFrame : Linear.RootFrame;
    internal PrincipalMomentsOfInertia Inertia => IsEndpoint ? Endpoint.Inertia : Angular.PrincipalInertia;
    internal bool Same(in SpacecraftPhysicalSource other) => IsEndpoint == other.IsEndpoint &&
        (IsEndpoint ? Endpoint.SameBits(other.Endpoint) : Linear == other.Linear && Angular == other.Angular && Properties == other.Properties);
    internal static bool TryCapture(in SpacecraftStateView view, SpacecraftId subject, out SpacecraftPhysicalSource source)
    {
        source = default;
        if (view.TryGetAppliedEndpoint(subject, out var endpoint))
        { source = new(true, endpoint, default, default, endpoint.Properties); return true; }
        if (!view.TryGetTranslation(subject, out var linear, out var mass) || !view.TryGetRigidBody(subject, out var angular)) return false;
        source = new(false, default, linear, angular, mass); return true;
    }
}
