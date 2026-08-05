using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Canonical position and velocity state in one caller-consistent simulation unit system.</summary>
internal readonly record struct CartesianState(Double3 Position, Double3 Velocity)
{
    public bool IsFinite => Position.IsFinite && Velocity.IsFinite;
}
