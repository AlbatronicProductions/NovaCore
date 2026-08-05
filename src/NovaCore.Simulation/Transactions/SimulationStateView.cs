using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;

namespace NovaCore.Simulation.Transactions;

/// <summary>Immutable evaluator input. It exposes no mutation path to authoritative state.</summary>
internal readonly record struct SimulationStateView(long MarkerValue, StateRevision Revision, CelestialStateView Celestial, SpacecraftStateView Spacecraft);
