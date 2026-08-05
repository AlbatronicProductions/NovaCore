using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial.Transactions;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;

namespace NovaCore.Simulation.Transactions;

/// <summary>Immutable proposed mutation; evaluation never commits it.</summary>
internal readonly record struct SimulationTransaction(
    SimulationEventHeader Event,
    SimulationInstant EvaluationTime,
    TimelineRevision ExpectedTimelineRevision,
    StateRevision ExpectedStateRevision,
    long ProposedMarkerValue,
    bool ChangesAuthoritativeState,
    bool IsInternallyConsistent,
    CelestialTrajectoryReplacementTransaction? CelestialReplacement = null,
    CelestialImpulseEvaluationStatus? CelestialImpulseStatus = null,
    RigidBodyTorqueReplacementTransaction? RigidBodyTorqueReplacement = null);
