using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Actuation;

internal enum PoweredFlightStatus
{
    Invalid, Ready, Prepared, Published, AcceptedCredit, NoWork, AwaitingDebt, BudgetExhausted, Completed,
    InvalidAuthority, WrongOwnerThread, Reentrant, StaleSource, InvalidInput, InvalidSequence,
    OutstandingProposal, InvalidProposal, OutsideModel, NumericalFailure, PendingEvent, HistoryCapacity,
    RevisionOverflow, ArithmeticOverflow, Invalidated, PreparationRefused, CanonicalCommittedPrivateInvalidated,
    CommandBlocked, EngineBlocked, ResourceBlocked, Retired
}
internal enum ActualEngineActivity { Off, EnabledNoFeed, EnabledIdle, ProducingOutput }
internal enum PoweredPhysicalConsumer { FreeFlight, RetainedContact }
internal readonly record struct ActualEngineState(ActualEngineActivity Activity, ProposedEngineLatch Latch, double EndpointThrottle,
    PropellantDuration AppliedPoweredDuration, long Frontier, ulong ActuatorRevision);
internal sealed class PoweredFlightAuthority(PropellantResourceAuthority resource, PoweredPhysicalConsumer consumer = PoweredPhysicalConsumer.FreeFlight)
{
    internal readonly PropellantResourceAuthority Resource = resource;
    internal readonly PoweredPhysicalConsumer Consumer = consumer;
    internal SpacecraftCommandAuthority Commands => Resource.Engine.Commands;
}
internal readonly struct PoweredFlightProposal(long generation, object? seal = null)
{
    private readonly object? _seal = seal;
    internal readonly long Generation = generation;
    internal bool IsIssuedBy(object seal) => ReferenceEquals(_seal, seal);
}
/// <summary>One deterministic interval record. Exact exhaustion authority is retained in Segmentation; no live capabilities.</summary>
internal readonly record struct PoweredFlightRecord(int Version, long Index, PropellantSegmentationPreview Segmentation,
    SpacecraftAppliedEndpoint Endpoint, ActualEngineState Actuator, StateRevision BeforeRevision,
    StateRevision StateRevision, ulong ResourceRevision, TimelineRevision TimelineRevision,
    SimulationDuration DebtBefore = default, SimulationDuration DebtAfter = default,
    PoweredPhysicalConsumer Consumer = PoweredPhysicalConsumer.FreeFlight, int ContactFixture = -1, long ContactFrontier = 0);
internal readonly record struct PoweredFlightEpisode(int NumericalPolicyVersion, SpacecraftDefinition Definition,
    PoweredFlightObservation Initial, SimulationInstant End, IdealEngineDefinitionValues EngineDefinition,
    PoweredPhysicalConsumer Consumer = PoweredPhysicalConsumer.FreeFlight, int ContactFixture = -1);
internal readonly record struct PoweredFlightObservation(SpacecraftAppliedEndpoint Endpoint, PropellantSourceObservation Resource,
    ActualEngineState Actuator, StateRevision StateRevision, TimelineRevision TimelineRevision, ContinuationClockState Clock, int HistoryCount);
internal readonly record struct PoweredFlightResult(PoweredFlightStatus Status, PoweredFlightObservation Observation = default,
    int PublishedCount = 0, PoweredEvaluationStatus Evaluation = default)
{
    internal bool CanonicalCommitted => PublishedCount > 0 || Status is PoweredFlightStatus.AcceptedCredit or
        PoweredFlightStatus.Published or PoweredFlightStatus.CanonicalCommittedPrivateInvalidated;
}
