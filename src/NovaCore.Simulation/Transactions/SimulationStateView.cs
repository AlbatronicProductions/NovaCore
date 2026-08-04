using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

/// <summary>Immutable evaluator input. It exposes no mutation path to authoritative state.</summary>
internal readonly record struct SimulationStateView(long MarkerValue, StateRevision Revision);
