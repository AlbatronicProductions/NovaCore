using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal const int MaximumContactIntervalsPerService = 4;

    internal ContactHostCreditResult AdmitContactHostTime(LocalContactWorld world, LocalContactConfiguration configuration,
        LocalContactWorld.Receipt receipt, ContactHostInput input) => AdmitContactHostTimeCore(world, configuration, receipt, input, false);

    internal ContactHostCreditResult AdmitContactHostTimeWithFailedAcknowledgementForTest(LocalContactWorld world,
        LocalContactConfiguration configuration, LocalContactWorld.Receipt receipt, ContactHostInput input) =>
        AdmitContactHostTimeCore(world, configuration, receipt, input, true);

    private ContactHostCreditResult AdmitContactHostTimeCore(LocalContactWorld world, LocalContactConfiguration configuration,
        LocalContactWorld.Receipt receipt, ContactHostInput input, bool failAcknowledgementForTest)
    {
        var phase = _clock.PublicationPhase;
        if (!phase.IsOwnerThread) return new(ContactHostCreditStatus.WrongOwnerThread);
        if (_isExecutingGroup || !phase.TryEnter(this)) return new(ContactHostCreditStatus.Reentrant);
        try
        {
            if (world is null || configuration is null) return new(ContactHostCreditStatus.Refused, LocalContactStatus.InvalidSource);
            var status = world.PrepareService(this, configuration, receipt, out _, out var completed, out var sequence);
            if (status != LocalContactStatus.Success) return new(ContactHostCreditStatus.Refused, status);
            var before = CaptureContinuationClock();
            if (!ServiceClockSupported(before)) return new(ContactHostCreditStatus.UnsupportedClock);
            if (completed) return new(ContactHostCreditStatus.Completed, Clock: before);
            if (sequence == long.MaxValue || input.Sequence != sequence + 1)
                return new(ContactHostCreditStatus.InvalidSequence);
            if (before.Debt.Ticks < 0) return new(ContactHostCreditStatus.ArithmeticOverflow);
            var prepared = _clock.PrepareHostAdvance(input.Duration);
            if (prepared.Reason != SimulationHostAdvanceStopReason.Accepted)
                return new(prepared.Reason switch
                {
                    SimulationHostAdvanceStopReason.NoWork => ContactHostCreditStatus.NoWork,
                    SimulationHostAdvanceStopReason.InvalidHostDuration => ContactHostCreditStatus.InvalidDuration,
                    _ => ContactHostCreditStatus.ArithmeticOverflow,
                }, Clock: before);
            var after = before with { Debt = prepared.DebtAfter, RateRemainder = prepared.RateRemainderAfter };
            if (!LocalContactWorld.HostCreditAcknowledgement.TryPrepare(world, this, after, input.Sequence, out var acknowledgement))
                return new(ContactHostCreditStatus.Refused);
            status = world.PrepareService(this, configuration, receipt, out _, out var recheckedComplete, out var recheckedSequence);
            if (status != LocalContactStatus.Success || recheckedComplete || recheckedSequence != sequence || CaptureContinuationClock() != before)
                return new(ContactHostCreditStatus.Refused, status);
            var success = new ContactHostCreditResult(ContactHostCreditStatus.Accepted, Clock: after);
            var terminal = new ContactHostCreditResult(ContactHostCreditStatus.CanonicalCommittedPrivateInvalidated, Clock: after);
            // Fixed canonical accounting commit, then fixed same-phase private expectation update.
            _clock.InstallHostAdvance(prepared);
            try
            {
                if (failAcknowledgementForTest) { acknowledgement.Invalidate(); return terminal; }
                acknowledgement.Commit();
            }
            catch (Exception) { acknowledgement.Invalidate(); return terminal; }
            return success;
        }
        finally { phase.Exit(); }
    }

    private static bool ServiceClockSupported(in ContinuationClockState clock) =>
        clock.Rate == SimulationRate.One && clock.RateRemainder == 0 && !clock.Paused;

    internal ContactServiceResult ServiceContactDebt(LocalContactWorld world, LocalContactConfiguration configuration,
        LocalContactWorld.Receipt receipt) => ServiceContactDebtCore(world, configuration, receipt, false);

    internal ContactServiceResult ServiceContactDebtWithFailedAcknowledgementForTest(LocalContactWorld world,
        LocalContactConfiguration configuration, LocalContactWorld.Receipt receipt) => ServiceContactDebtCore(world, configuration, receipt, true);

    private ContactServiceResult ServiceContactDebtCore(LocalContactWorld world, LocalContactConfiguration configuration,
        LocalContactWorld.Receipt receipt, bool failAcknowledgementForTest)
    {
        var phase = _clock.PublicationPhase;
        if (!phase.IsOwnerThread) return new(ContactServiceStatus.WrongOwnerThread, 0, receipt);
        if (_isExecutingGroup || !phase.TryEnter(this)) return new(ContactServiceStatus.Reentrant, 0, receipt);
        var count = 0; PersistentContactObservation observation = default;
        try
        {
            if (world is null || configuration is null) return Result(ContactServiceStatus.AuthorityRefused, LocalContactStatus.InvalidSource);
            while (true)
            {
                var status = world.PrepareService(this, configuration, receipt, out var target, out var completed, out _);
                if (status != LocalContactStatus.Success) return Result(ContactServiceStatus.AuthorityRefused, status);
                var clock = CaptureContinuationClock();
                if (!ServiceClockSupported(clock)) return Result(ContactServiceStatus.UnsupportedClock);
                if (completed) return Result(ContactServiceStatus.Completed);
                var delta = (Int128)target.Ticks - clock.Time.Ticks;
                if (clock.Debt.Ticks < 0 || delta <= 0 || delta > long.MaxValue) return Result(ContactServiceStatus.ArithmeticOverflow);
                if (HasContactProofBoundaryThrough(target)) return Result(ContactServiceStatus.AuthorityRefused, LocalContactStatus.PendingEvent);
                if (clock.Debt.Ticks < delta) return Result(ContactServiceStatus.AwaitingDebt);
                if (_persistentContactCount >= _persistentContactHistory.Length) return Result(ContactServiceStatus.HistoryCapacity);
                if (_state.CreateView().Revision.Value == ulong.MaxValue) return Result(ContactServiceStatus.StateRevisionOverflow);
                if (count == MaximumContactIntervalsPerService) return Result(ContactServiceStatus.BudgetExhausted);
                status = world.Step(this, configuration, receipt, target, out var next);
                if (status != LocalContactStatus.Success) return Result(ContactServiceStatus.StepFailed, status);
                receipt = next;
                var publication = PublishPersistentContactInOwnedPhase(world, configuration, new(receipt, clock), failAcknowledgementForTest);
                if (publication.CanonicalCommitted) { count++; observation = publication.Observation; }
                if (publication.Status == PersistentContactPublicationStatus.CanonicalCommittedPrivateInvalidated)
                    return new(ContactServiceStatus.CanonicalCommittedPrivateInvalidated, count, receipt, observation, PublicationStatus: publication.Status);
                if (publication.Status != PersistentContactPublicationStatus.Published)
                    return new(ContactServiceStatus.PublicationRefused, count, receipt, observation, PublicationStatus: publication.Status);
            }
        }
        finally { phase.Exit(); }

        ContactServiceResult Result(ContactServiceStatus status, LocalContactStatus authority = LocalContactStatus.Success) =>
            new(status, count, receipt, observation, authority);
    }
}
