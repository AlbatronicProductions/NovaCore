using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal enum ContactHostCreditStatus : byte
{
    Refused, Accepted, NoWork, InvalidDuration, ArithmeticOverflow, InvalidSequence,
    UnsupportedClock, WrongOwnerThread, Reentrant, Completed, CanonicalCommittedPrivateInvalidated,
}

/// <summary>One owner input; its sequence is local to the bound episode and consumed only on commit.</summary>
internal readonly record struct ContactHostInput(long Sequence, SimulationDuration Duration);
internal readonly record struct ContactHostCreditResult(ContactHostCreditStatus Status,
    LocalContactStatus AuthorityStatus = LocalContactStatus.Success, ContinuationClockState Clock = default)
{
    internal bool CanonicalCommitted => Status is ContactHostCreditStatus.Accepted or ContactHostCreditStatus.CanonicalCommittedPrivateInvalidated;
}

internal enum ContactServiceStatus : byte
{
    AwaitingDebt, BudgetExhausted, Completed, AuthorityRefused, HistoryCapacity,
    StateRevisionOverflow, ArithmeticOverflow, UnsupportedClock, WrongOwnerThread, Reentrant,
    StepFailed, PublicationRefused, CanonicalCommittedPrivateInvalidated,
}

/// <summary>Owner-only continuation capability plus a copied canonical observation. No solver references in history.</summary>
internal readonly record struct ContactServiceResult(ContactServiceStatus Status, int Published,
    LocalContactWorld.Receipt Receipt, PersistentContactObservation Observation = default,
    LocalContactStatus AuthorityStatus = LocalContactStatus.Success,
    PersistentContactPublicationStatus PublicationStatus = default);
