using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Rotation;

/// <summary>Immutable player intent. It requests body torque only; it never contains pose state.</summary>
internal readonly record struct SpacecraftTorqueCommand(SpacecraftId Spacecraft, Double3 RequestedBodyTorque, SimulationInstant Time)
{
    internal bool IsValid => Spacecraft.IsValid && RequestedBodyTorque.IsFinite;
}
