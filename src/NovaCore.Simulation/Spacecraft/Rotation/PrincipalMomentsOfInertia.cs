using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Rotation;

/// <summary>Immutable diagonal principal inertia in body axes, in kg·m².</summary>
internal readonly record struct PrincipalMomentsOfInertia(double X, double Y, double Z)
{
    internal bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
    internal bool IsStrictlyPositive => X > 0d && Y > 0d && Z > 0d;
    internal Double3 Multiply(in Double3 angularVelocity) => new(X * angularVelocity.X, Y * angularVelocity.Y, Z * angularVelocity.Z);
}
