using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Transactions;

var tests = new (string Name, Action Test)[]
{
    ("SimulationInstant", InstantTests),
    ("SimulationDuration", DurationTests),
    ("SimulationRate", RateTests),
    ("Event ordering", EventOrderingTests),
    ("Timeline topology", TimelineTopologyTests),
    ("Simulation clock", ClockTests),
    ("Host-duration conversion", HostDurationTests),
    ("Host-duration debt servicing", HostDurationDebtServiceTests),
    ("Transaction contracts", TransactionTests),
    ("Canonical transaction groups", CanonicalGroupTests),
    ("Clock execution orchestration", ClockExecutionTests),
    ("Allocation", AllocationTests),
};
foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }

static void InstantTests()
{
    Check(SimulationInstant.Zero.Ticks == 0 && new SimulationInstant(-1) < SimulationInstant.Zero && new SimulationInstant(long.MinValue) < new SimulationInstant(long.MaxValue), "zero/negative/extreme instant");
    var instant = SimulationInstant.FromWholeSeconds(2) + new SimulationDuration(1);
    Check(instant.Ticks == 2_000_001 && instant - SimulationInstant.FromWholeSeconds(2) == new SimulationDuration(1), "instant arithmetic");
    Check(SimulationInstant.FromSecondsRounded(1.25).Ticks == 1_250_000 && SimulationInstant.FromWholeSeconds(-1).SecondsSinceEpoch == -1d, "seconds conversion");
    Check(new SimulationInstant(1).Ticks - new SimulationInstant(0).Ticks == 1, "microtick resolution");
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MaxValue) + new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MinValue) - new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MaxValue) - new SimulationInstant(-1));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.NaN));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.PositiveInfinity));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.NegativeInfinity));
    Throws<OverflowException>(() => SimulationInstant.FromSecondsRounded(double.MaxValue));
}

static void DurationTests()
{
    Check(SimulationDuration.Zero.IsZero && new SimulationDuration(-1).IsNegative, "duration signs");
    Check(SimulationDuration.FromWholeSeconds(-2).TotalSeconds == -2d, "duration seconds");
    Check(new SimulationDuration(-5).Abs() == new SimulationDuration(5), "duration absolute value");
    Check(new SimulationDuration(-2) < new SimulationDuration(0) && new SimulationDuration(2) > new SimulationDuration(0), "duration comparison");
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MaxValue) + new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MinValue) - new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MinValue).Abs());
}

static void RateTests()
{
    Check(new SimulationRate(2, 4) == SimulationRate.Half, "rate normalization");
    Check(SimulationRate.Quarter == new SimulationRate(1, 4) && SimulationRate.Half == new SimulationRate(1, 2) && SimulationRate.One == new SimulationRate(1, 1) && SimulationRate.Two == new SimulationRate(2, 1) && SimulationRate.Five == new SimulationRate(5, 1) && SimulationRate.Ten == new SimulationRate(10, 1) && SimulationRate.Hundred == new SimulationRate(100, 1), "rate presets");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationRate(0, 1));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationRate(1, 0));
    Check(new SimulationRate(9_000_000_000_000_000_000, 3).Numerator == 3_000_000_000_000_000_000, "GCD edge case");
    var quarter = SimulationRate.Quarter; long remainder = 0; var repeated = 0L;
    for (var i = 0; i < 4; i++) { Check(quarter.TryScale(1, ref remainder, out var result), "tiny scale"); repeated += result; }
    Check(repeated == 1 && remainder == 0, "remainder after repeated tiny host durations");
    remainder = 0; Check(quarter.TryScale(4, ref remainder, out var combined) && combined == 1 && remainder == 0, "combined duration");
    var half = SimulationRate.Half; remainder = 0; Check(half.TryScale(2, ref remainder, out var halfResult) && halfResult == 1 && remainder == 0, "half rate exact");
    var twice = SimulationRate.Two; remainder = 0; Check(twice.TryScale(3, ref remainder, out var twiceResult) && twiceResult == 6 && remainder == 0, "double rate exact");
    remainder = 0; Check(half.TryScale(1, ref remainder, out var zero) && zero == 0 && remainder == 1, "retained fractional remainder");
    twice.ResetRemainder(ref remainder); Check(remainder == 0, "rate change resets remainder");
    remainder = 0; Check(!twice.TryScale(long.MaxValue, ref remainder, out _), "Int128 scaling overflow reports false");
    Check(!twice.TryScale(-1, ref remainder, out _), "negative host duration rejected");
    remainder = twice.Denominator; Check(!twice.TryScale(1, ref remainder, out _), "invalid remainder rejected");
    remainder = 0; var scripted = 0L; for (var i = 0; i < 10_000; i++) { Check(new SimulationRate(5, 7).TryScale(13, ref remainder, out var result), "scripted scale"); scripted = checked(scripted + result); Check(remainder >= 0 && remainder < 7, "remainder invariant"); } Check(scripted == 92_857 && remainder == 1, "deterministic scripted conversion");
}

static void EventOrderingTests()
{
    var early = Header(1, -1, 0, 1); var late = Header(2, 1, 0, 1);
    var highPriority = Header(3, 0, -1, 1); var lowPriority = Header(4, 0, 1, 1);
    var firstSequence = Header(5, 0, 0, 1); var laterSequence = Header(6, 0, 0, 2);
    var firstId = Header(7, 0, 0, 3); var laterId = Header(8, 0, 0, 3);
    Check(SimulationEventHeaderComparer.Compare(early, late) < 0 && SimulationEventHeaderComparer.Compare(highPriority, lowPriority) < 0 && SimulationEventHeaderComparer.Compare(firstSequence, laterSequence) < 0 && SimulationEventHeaderComparer.Compare(firstId, laterId) < 0, "ordering tuple");
    Check(SimulationEventHeaderComparer.Compare(early, early) == 0 && Math.Sign(SimulationEventHeaderComparer.Compare(early, late)) == -Math.Sign(SimulationEventHeaderComparer.Compare(late, early)), "equality and antisymmetry");
    Check(SimulationEventHeaderComparer.Compare(early, highPriority) < 0 && SimulationEventHeaderComparer.Compare(highPriority, lowPriority) < 0 && SimulationEventHeaderComparer.Compare(early, lowPriority) < 0, "transitivity");
    var duplicate = new[] { early, early }; Throws<ArgumentException>(() => SimulationEventHeaderComparer.ValidateStrictlyOrdered(duplicate));
    var unordered = new[] { late, early }; Throws<ArgumentException>(() => SimulationEventHeaderComparer.ValidateStrictlyOrdered(unordered));
    var minimum = Header(9, long.MinValue, int.MinValue, ulong.MaxValue); var maximum = Header(10, long.MaxValue, int.MaxValue, ulong.MaxValue - 1); Check(SimulationEventHeaderComparer.Compare(minimum, maximum) < 0, "extreme timestamp and priority ordering");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(SimulationEventId.Invalid, SimulationInstant.Zero, 0, new SimulationEventSequence(1), SimulationEventKind.Marker));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(new SimulationEventId(1), SimulationInstant.Zero, 0, SimulationEventSequence.Unassigned, SimulationEventKind.Marker));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(new SimulationEventId(1), SimulationInstant.Zero, 0, new SimulationEventSequence(1), (SimulationEventKind)255));

    var canonical = CreateStressHeaders(); Array.Sort(canonical, SimulationEventHeaderComparer.Compare); SimulationEventHeaderComparer.ValidateStrictlyOrdered(canonical); var hash = Hash(canonical);
    for (var pass = 0; pass < 8; pass++) { var shuffled = (SimulationEventHeader[])canonical.Clone(); Shuffle(shuffled, (ulong)(pass + 11)); Array.Sort(shuffled, SimulationEventHeaderComparer.Compare); Check(Hash(shuffled) == hash, "permutation canonical ordering"); }
    Console.WriteLine($"Deterministic event-order stress hash: 0x{hash:X16}");
}

static void AllocationTests()
{
    var instant = SimulationInstant.Zero; var duration = new SimulationDuration(1); var rate = new SimulationRate(5, 7); long remainder = 0; var left = Header(1, 0, 0, 1); var right = Header(2, 1, 0, 2);
    _ = instant + duration; rate.TryScale(1, ref remainder, out _); _ = SimulationEventHeaderComparer.Compare(left, right);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { instant += duration; _ = instant - duration; rate.TryScale(13, ref remainder, out _); _ = SimulationEventHeaderComparer.Compare(left, right); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "steady-state arithmetic, scaling, and comparison allocations");
}

static void TimelineTopologyTests()
{
    var now = new SimulationInstant(100);
    var timeline = new SimulationTimeline(32);
    var first = timeline.Schedule(now, Request(1, 100, 0));
    Check(first.Succeeded && first.ScheduledEvent.Header.Sequence.Value == 1 && timeline.Revision.Value == 1, "first sequence and revision");
    var second = timeline.Schedule(now, Request(2, 101, 0));
    Check(second.Succeeded && second.ScheduledEvent.Header.Sequence.Value == 2, "monotonic sequence");
    var revision = timeline.Revision;
    Check(timeline.Schedule(now, Request(0, 101, 0)).Status == SimulationScheduleStatus.InvalidId, "zero ID rejected");
    Check(timeline.Schedule(now, new SimulationEventRequest(new SimulationEventId(3), now, 0, (SimulationEventKind)255)).Status == SimulationScheduleStatus.InvalidKind, "kind rejected");
    Check(timeline.Schedule(now, Request(3, 99, 0)).Status == SimulationScheduleStatus.PastTime, "past event rejected");
    Check(timeline.Schedule(now, Request(1, 102, 0)).Status == SimulationScheduleStatus.DuplicateId, "duplicate ID rejected");
    Check(timeline.Revision == revision && timeline.ValidateInvariants(), "failed schedule leaves topology unchanged");

    var middle = timeline.Schedule(now, Request(4, 102, 0));
    var leaf = timeline.Schedule(now, Request(5, 103, 0));
    Check(middle.Succeeded && leaf.Succeeded && timeline.ValidateInvariants(), "insert heap entries");
    Check(timeline.TryPeekPending(out var minimumPending) && minimumPending.Header.Id == new SimulationEventId(1), "heap root is canonical minimum");
    Check(timeline.Cancel(new SimulationEventId(1)).Succeeded && timeline.ValidateInvariants(), "cancel root");
    Check(timeline.Cancel(new SimulationEventId(4)).Succeeded && timeline.ValidateInvariants(), "cancel middle");
    Check(timeline.Cancel(new SimulationEventId(5)).Succeeded && timeline.ValidateInvariants(), "cancel leaf");
    Check(timeline.Cancel(new SimulationEventId(5)).Status == SimulationCancelStatus.NotPending && timeline.IsIdReserved(new SimulationEventId(5)), "cancelled ID remains reserved");

    var beforeReplacement = timeline.Revision;
    var replacement = timeline.Replace(now, new SimulationEventId(2), Request(6, 104, -1));
    Check(replacement.Succeeded && replacement.ScheduledEvent.Header.Sequence.Value == 5 && timeline.Revision.Value == beforeReplacement.Value + 1, "atomic replacement");
    Check(timeline.IsIdReserved(new SimulationEventId(2)) && timeline.IsIdReserved(new SimulationEventId(6)) && timeline.ValidateInvariants(), "replacement reserves both IDs");
    revision = timeline.Revision;
    Check(timeline.Replace(now, new SimulationEventId(2), Request(7, 104, 0)).Status == SimulationScheduleStatus.ReplacementTargetNotPending, "missing replacement target");
    Check(timeline.Replace(now, new SimulationEventId(6), Request(6, 104, 0)).Status == SimulationScheduleStatus.DuplicateId, "replacement duplicate new ID");
    Check(timeline.Revision == revision && timeline.ValidateInvariants(), "failed replacement unchanged");

    var overflow = new SimulationTimeline(1, ulong.MaxValue, TimelineRevision.Zero);
    Check(overflow.Schedule(SimulationInstant.Zero, Request(100, 0, 0)).Status == SimulationScheduleStatus.SequenceOverflow && overflow.PendingCount == 0 && overflow.Revision == TimelineRevision.Zero, "sequence overflow controlled");
    var revisionOverflow = new SimulationTimeline(1, 1, new TimelineRevision(ulong.MaxValue));
    Check(revisionOverflow.Schedule(SimulationInstant.Zero, Request(101, 0, 0)).Status == SimulationScheduleStatus.RevisionOverflow, "revision overflow controlled");

    var expected = CanonicalHeaders();
    for (var pass = 0; pass < 8; pass++)
    {
        var permuted = (SimulationEventRequest[])expected.Clone(); ShuffleRequests(permuted, (ulong)(pass + 41));
        var orderedTimeline = new SimulationTimeline(permuted.Length);
        foreach (var request in permuted) Check(orderedTimeline.Schedule(new SimulationInstant(long.MinValue), request).Succeeded, "permuted schedule");
        var actual = new ScheduledSimulationEvent[orderedTimeline.PendingCount]; orderedTimeline.CopyPending(actual);
        Array.Sort(actual, static (left, right) => SimulationEventHeaderComparer.Compare(left.Header, right.Header));
        for (var index = 0; index < actual.Length; index++) Check(actual[index].Header.Time == expected[index].Time && actual[index].Header.Priority == expected[index].Priority && actual[index].Header.Id == expected[index].Id, "canonical heap order");
        Check(orderedTimeline.ValidateInvariants(), "permuted heap invariants");
    }

    var allocatedTimeline = new SimulationTimeline(20_000);
    for (ulong id = 1; id <= 100; id++) Check(allocatedTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id, 0)).Succeeded, "allocation warmup");
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (ulong id = 101; id <= 10_000; id++) { Check(allocatedTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id, 0)).Succeeded, "allocation schedule"); Check(allocatedTimeline.Cancel(new SimulationEventId(id)).Succeeded, "allocation cancel"); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && allocatedTimeline.ValidateInvariants(), "preallocated timeline operations allocate zero bytes");

    var hash = TimelineHash(expected); Check(TimelineHash(CanonicalHeaders()) == hash, "deterministic timeline stress hash");
    Console.WriteLine($"Deterministic timeline stress hash: 0x{hash:X16}");
    var mixedHash = MixedTimelineHash(); Check(MixedTimelineHash() == mixedHash, "fixed-seed mixed timeline operations");
    Console.WriteLine($"Deterministic mixed-timeline stress hash: 0x{mixedHash:X16}");
}

static void ClockTests()
{
    var timeline = new SimulationTimeline(16);
    var clock = new SimulationClock(new SimulationInstant(-10), timeline);
    Check(clock.CurrentTime.Ticks == -10 && ReferenceEquals(clock.Timeline, timeline) && clock.Rate == SimulationRate.One && clock.Settings.MaximumEventsPerAdvance == 10_000 && !clock.IsPaused && clock.RateRemainder == 0, "clock construction");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationClockSettings(0));

    var revision = timeline.Revision; clock.Pause(); clock.Pause(); Check(clock.IsPaused && clock.CurrentTime.Ticks == -10 && timeline.Revision == revision, "pause idempotent");
    var pausedAdvance = clock.AdvanceTo(new SimulationInstant(10)); Check(pausedAdvance.Reason == SimulationAdvanceStopReason.ReachedTarget && clock.CurrentTime.Ticks == 10, "explicit advance while paused");
    clock.Resume(); clock.Resume(); Check(!clock.IsPaused && timeline.Revision == revision, "resume idempotent");
    Check(clock.TrySetRate(SimulationRate.Two) && clock.Rate == SimulationRate.Two && clock.RateRemainder == 0 && clock.CurrentTime.Ticks == 10 && timeline.Revision == revision, "rate change");
    Check(!clock.TrySetRate(new SimulationRate(4, 2)), "equivalent normalized rate is no-op");

    var noEvents = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(noEvents.AdvanceTo(SimulationInstant.Zero).Reason == SimulationAdvanceStopReason.ReachedTarget, "equal empty target");
    Check(noEvents.AdvanceTo(new SimulationInstant(long.MaxValue)).Reason == SimulationAdvanceStopReason.ReachedTarget && noEvents.CurrentTime.Ticks == long.MaxValue, "huge empty jump");
    Check(noEvents.AdvanceTo(SimulationInstant.Zero).Reason == SimulationAdvanceStopReason.TargetBeforeCurrent && noEvents.CurrentTime.Ticks == long.MaxValue, "target before current");
    Check(noEvents.AdvanceUntilNextEvent().Reason == SimulationAdvanceStopReason.NoPendingEvent, "no pending event");

    var eventTimeline = new SimulationTimeline(8);
    Check(eventTimeline.Schedule(SimulationInstant.Zero, Request(1, 20, 0)).Succeeded, "schedule future boundary");
    Check(eventTimeline.Schedule(SimulationInstant.Zero, Request(2, 20, -1)).Succeeded, "schedule canonical priority boundary");
    var eventClock = new SimulationClock(SimulationInstant.Zero, eventTimeline);
    Check(eventClock.AdvanceTo(new SimulationInstant(10)).Reason == SimulationAdvanceStopReason.ReachedTarget && eventClock.CurrentTime.Ticks == 10, "event after target");
    var boundary = eventClock.AdvanceTo(new SimulationInstant(20));
    Check(boundary.Reason == SimulationAdvanceStopReason.ReachedEventBoundary && boundary.BoundaryEvent!.Value.Id == new SimulationEventId(2) && eventClock.CurrentTime.Ticks == 20, "canonical boundary at target");
    Check(eventTimeline.PendingCount == 2 && eventTimeline.Revision.Value == 2, "boundary remains pending");
    var repeated = eventClock.AdvanceTo(new SimulationInstant(100)); Check(repeated.ReachedBoundary && repeated.BoundaryEvent!.Value.Id == new SimulationEventId(2), "repeated boundary remains stable");
    eventClock.Pause(); var until = eventClock.AdvanceUntilNextEvent(); Check(until.ReachedBoundary && until.BoundaryEvent!.Value.Id == new SimulationEventId(2), "until next works while paused");
    var reentrant = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(reentrant.AdvanceToWhileGuardedForTest(new SimulationInstant(1)).Reason == SimulationAdvanceStopReason.ReentrantAdvance, "nested AdvanceTo rejected");
    Check(reentrant.AdvanceUntilNextEventWhileGuardedForTest().Reason == SimulationAdvanceStopReason.ReentrantAdvance, "nested AdvanceUntilNextEvent rejected");
    Check(reentrant.AdvanceTo(new SimulationInstant(1)).Reason == SimulationAdvanceStopReason.ReachedTarget, "advancement guard restored");

    var allocationTimeline = new SimulationTimeline(2); Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(50, 5, 0)).Succeeded, "allocation boundary setup");
    var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); _ = allocationClock.AdvanceTo(new SimulationInstant(1)); _ = allocationClock.AdvanceUntilNextEvent(); allocationClock.Pause(); allocationClock.Resume(); _ = allocationClock.TrySetRate(SimulationRate.One);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { _ = allocationClock.AdvanceTo(new SimulationInstant(5)); _ = allocationClock.AdvanceUntilNextEvent(); allocationClock.Pause(); allocationClock.Resume(); _ = allocationClock.TrySetRate(SimulationRate.One); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "clock steady-state operations allocate zero bytes");

    var hash = ClockHash(); Check(ClockHash() == hash, "deterministic clock script");
    Console.WriteLine($"Deterministic clock stress hash: 0x{hash:X16}");
}

static void HostDurationTests()
{
    var one = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    var accepted = one.AdvanceByHostDuration(new SimulationDuration(10));
    Check(accepted.Reason == SimulationHostAdvanceStopReason.Accepted && accepted.DerivedSimulationDuration.Ticks == 10 && one.PendingSimulationDebt.Ticks == 10 && one.RateRemainder == 0 && one.CurrentTime == SimulationInstant.Zero, "one-to-one host conversion");

    var half = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Half);
    Check(half.AdvanceByHostDuration(new SimulationDuration(1)).DerivedSimulationDuration.Ticks == 0 && half.RateRemainder == 1, "half fractional remainder");
    Check(half.AdvanceByHostDuration(new SimulationDuration(1)).DerivedSimulationDuration.Ticks == 1 && half.PendingSimulationDebt.Ticks == 1 && half.RateRemainder == 0, "half retained remainder");
    var quarter = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Quarter);
    for (var index = 0; index < 4; index++) _ = quarter.AdvanceByHostDuration(new SimulationDuration(1));
    Check(quarter.PendingSimulationDebt.Ticks == 1 && quarter.RateRemainder == 0, "quarter rate");
    var twice = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Two);
    Check(twice.AdvanceByHostDuration(new SimulationDuration(3)).DerivedSimulationDuration.Ticks == 6 && twice.PendingSimulationDebt.Ticks == 6, "accelerated rate");

    var split = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    var combined = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    for (var index = 0; index < 10_000; index++) _ = split.AdvanceByHostDuration(new SimulationDuration(13));
    _ = combined.AdvanceByHostDuration(new SimulationDuration(130_000));
    Check(split.PendingSimulationDebt == combined.PendingSimulationDebt && split.RateRemainder == combined.RateRemainder, "split and combined exact composition");

    var rateChange = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Half);
    _ = rateChange.AdvanceByHostDuration(new SimulationDuration(1));
    Check(!rateChange.TrySetRate(new SimulationRate(2, 4)) && rateChange.RateRemainder == 1, "equivalent rate complete no-op");
    _ = rateChange.AdvanceByHostDuration(new SimulationDuration(1));
    var debtBeforeChange = rateChange.PendingSimulationDebt;
    Check(rateChange.TrySetRate(SimulationRate.Two) && rateChange.RateRemainder == 0 && rateChange.PendingSimulationDebt == debtBeforeChange, "changed rate resets only remainder");

    rateChange.Pause(); var pausedDebt = rateChange.PendingSimulationDebt; var pausedRemainder = rateChange.RateRemainder;
    Check(rateChange.AdvanceByHostDuration(new SimulationDuration(100)).Reason == SimulationHostAdvanceStopReason.Paused && rateChange.PendingSimulationDebt == pausedDebt && rateChange.RateRemainder == pausedRemainder, "pause preserves debt and remainder");
    rateChange.Resume();
    var zeroDebt = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(zeroDebt.AdvanceByHostDuration(SimulationDuration.Zero).Reason == SimulationHostAdvanceStopReason.NoWork && zeroDebt.PendingSimulationDebt.IsZero, "zero duration with no debt");
    Check(rateChange.AdvanceByHostDuration(SimulationDuration.Zero).Reason == SimulationHostAdvanceStopReason.NoWork && rateChange.PendingSimulationDebt == pausedDebt, "zero duration retains debt in 4A");

    var invalid = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(invalid.AdvanceByHostDuration(new SimulationDuration(-1)).Reason == SimulationHostAdvanceStopReason.InvalidHostDuration && invalid.PendingSimulationDebt.IsZero && invalid.RateRemainder == 0, "negative host duration rejected");
    var scaleOverflow = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Two);
    Check(scaleOverflow.AdvanceByHostDuration(new SimulationDuration(long.MaxValue)).Reason == SimulationHostAdvanceStopReason.ArithmeticOverflow && scaleOverflow.PendingSimulationDebt.IsZero && scaleOverflow.RateRemainder == 0, "scaling overflow no partial state");
    var debtOverflow = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    _ = debtOverflow.AdvanceByHostDuration(new SimulationDuration(1)); Check(debtOverflow.TrySetRate(new SimulationRate(long.MaxValue, 1)), "debt overflow rate setup");
    Check(debtOverflow.AdvanceByHostDuration(new SimulationDuration(1)).Reason == SimulationHostAdvanceStopReason.ArithmeticOverflow && debtOverflow.PendingSimulationDebt.Ticks == 1 && debtOverflow.RateRemainder == 0, "debt overflow no partial state");

    var warm = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7)); _ = warm.AdvanceByHostDuration(new SimulationDuration(1));
    var allocation = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) _ = allocation.AdvanceByHostDuration(new SimulationDuration(13));
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore, "host conversion and debt accumulation allocate zero bytes");

    var hash = HostDurationHash(); Check(HostDurationHash() == hash, "deterministic host conversion replay hash");
    Console.WriteLine($"Deterministic host-duration conversion hash: 0x{hash:X16}");
}

static void HostDurationDebtServiceTests()
{
    var emptyClock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    var emptyEngine = new SimulationTransactionEngine(emptyClock, new SimulationState());
    Check(emptyEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.NoDebt, "no debt service is controlled");
    _ = emptyClock.AdvanceByHostDuration(new SimulationDuration(20));
    var coast = emptyEngine.ServicePendingHostDurationDebt();
    Check(coast.Reason == SimulationDebtServiceStopReason.Completed && coast.TargetTime.Ticks == 20 && coast.ReachedTime.Ticks == 20 && coast.DebtAfter.IsZero && coast.ProcessedEventCount == 0 && coast.ExecutedGroupCount == 0 && coast.LastGroupStopReason is null, "debt coast diagnostics");

    var timeline = new SimulationTimeline(4);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 5, 1)).Succeeded, "debt schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 10, 0)).Succeeded, "debt schedule B");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 4);
    _ = clock.AdvanceByHostDuration(new SimulationDuration(15));
    var executed = engine.ServicePendingHostDurationDebt();
    Check(executed.Reason == SimulationDebtServiceStopReason.Completed && executed.TargetTime.Ticks == 15 && clock.CurrentTime.Ticks == 15 && clock.PendingSimulationDebt.IsZero && executed.ProcessedEventCount == 2 && executed.ExecutedGroupCount == 2 && executed.LastGroupStopReason == SimulationCanonicalGroupStopReason.Completed && engine.ProcessedCount == 2, "debt group diagnostics");

    var capTimeline = new SimulationTimeline(4);
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(10, 5, 0)).Succeeded, "budget schedule A");
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(11, 5, 1)).Succeeded, "budget schedule B");
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(12, 10, 0)).Succeeded, "budget schedule C");
    var capClock = new SimulationClock(SimulationInstant.Zero, capTimeline, settings: new SimulationClockSettings(2));
    var capEngine = new SimulationTransactionEngine(capClock, new SimulationState(), 4);
    _ = capClock.AdvanceByHostDuration(new SimulationDuration(20));
    var capped = capEngine.ServicePendingHostDurationDebt();
    Check(capped.Reason == SimulationDebtServiceStopReason.EventLimitReached && capped.TargetTime.Ticks == 20 && capped.ProcessedEventCount == 2 && capped.ExecutedGroupCount == 1 && capped.LastGroupStopReason == SimulationCanonicalGroupStopReason.Completed && capClock.CurrentTime.Ticks == 10 && capClock.PendingSimulationDebt.Ticks == 10 && capTimeline.PendingCount == 1, "one call-wide budget retains accurate debt");
    Check(capEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed && capClock.CurrentTime.Ticks == 20 && capClock.PendingSimulationDebt.IsZero && capEngine.ProcessedCount == 3, "next service resumes retained debt");

    var rejectionTimeline = new SimulationTimeline(2);
    Check(rejectionTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(20, 5, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "debt rejection schedule");
    var rejectionClock = new SimulationClock(SimulationInstant.Zero, rejectionTimeline);
    var rejectionEngine = new SimulationTransactionEngine(rejectionClock, new SimulationState(), 2);
    _ = rejectionClock.AdvanceByHostDuration(new SimulationDuration(20));
    var rejected = rejectionEngine.ServicePendingHostDurationDebt();
    Check(rejected.Reason == SimulationDebtServiceStopReason.ValidationRejected && rejected.TargetTime.Ticks == 20 && rejected.ProcessedEventCount == 0 && rejected.ExecutedGroupCount == 1 && rejected.LastGroupStopReason == SimulationCanonicalGroupStopReason.ValidationRejected && rejectionClock.CurrentTime.Ticks == 5 && rejectionClock.PendingSimulationDebt.Ticks == 15 && rejectionEngine.ProcessedCount == 0 && rejectionTimeline.PendingCount == 1, "validation rejection retains untraversed debt and authority");

    var laterTimeline = new SimulationTimeline(1); Check(laterTimeline.Schedule(SimulationInstant.Zero, Request(30, 25, 0)).Succeeded, "later boundary schedule");
    var laterClock = new SimulationClock(SimulationInstant.Zero, laterTimeline); var laterEngine = new SimulationTransactionEngine(laterClock, new SimulationState(), 1);
    _ = laterClock.AdvanceByHostDuration(new SimulationDuration(20));
    Check(laterEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed && laterClock.CurrentTime.Ticks == 20 && laterTimeline.PendingCount == 1, "debt never executes beyond requested duration");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(90, 1, 0)).Succeeded, "debt allocation warmup schedule");
    var warmClock = new SimulationClock(SimulationInstant.Zero, warmTimeline); _ = warmClock.AdvanceByHostDuration(new SimulationDuration(2)); _ = new SimulationTransactionEngine(warmClock, new SimulationState(), 1).ServicePendingHostDurationDebt();
    var allocationTimeline = new SimulationTimeline(1_000);
    for (ulong id = 1; id <= 1_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 1, (int)id)).Succeeded, "debt allocation schedule");
    var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); var allocationEngine = new SimulationTransactionEngine(allocationClock, new SimulationState(), 1_000); _ = allocationClock.AdvanceByHostDuration(new SimulationDuration(2));
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); var allocation = allocationEngine.ServicePendingHostDurationDebt();
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocation.Reason == SimulationDebtServiceStopReason.Completed && allocation.ProcessedEventCount == 1_000, "preallocated debt servicing allocates zero bytes");

    const int stressEventCount = 5_000;
    var stressTimeline = new SimulationTimeline(stressEventCount);
    for (ulong id = 1; id <= stressEventCount; id++) Check(stressTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id * 10, 0)).Succeeded, "long-run schedule");
    var stressClock = new SimulationClock(SimulationInstant.Zero, stressTimeline, settings: new SimulationClockSettings(16));
    var stressEngine = new SimulationTransactionEngine(stressClock, new SimulationState(), stressEventCount);
    _ = stressClock.AdvanceByHostDuration(new SimulationDuration(100)); _ = stressEngine.ServicePendingHostDurationDebt();
    var stressBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var cycle = 1; cycle < 500; cycle++) { _ = stressClock.AdvanceByHostDuration(new SimulationDuration(100)); Check(stressEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed, "long-run debt service"); }
    Check(GC.GetAllocatedBytesForCurrentThread() == stressBefore && stressClock.CurrentTime.Ticks == 50_000 && stressClock.PendingSimulationDebt.IsZero && stressEngine.ProcessedCount == stressEventCount && stressTimeline.PendingCount == 0, "repeated long-duration servicing is allocation-free");

    var hash = HostDurationDebtServiceHash(); Check(HostDurationDebtServiceHash() == hash, "deterministic host-duration orchestration hash");
    Console.WriteLine($"Deterministic host-duration orchestration hash: 0x{hash:X16}");
    var longRunHash = HostDurationLongRunHash(); Check(HostDurationLongRunHash() == longRunHash, "deterministic long-duration replay hash");
    Console.WriteLine($"Deterministic long-duration host advancement hash: 0x{longRunHash:X16}");
}

static void TransactionTests()
{
    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 10, 0)).Succeeded, "transaction schedule");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 10, -1)).Succeeded, "canonical transaction schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    Check(clock.AdvanceTo(new SimulationInstant(10)).ReachedBoundary, "reach transaction boundary");
    var state = new SimulationState();
    var engine = new SimulationTransactionEngine(clock, state, 8);
    var transaction = engine.EvaluateNext();
    Check(engine.State.MarkerValue == 0 && transaction.ProposedMarkerValue == 1 && transaction.Event.Id == new SimulationEventId(2), "evaluation is immutable");
    var beforeTimeline = timeline.Revision; var result = engine.ExecuteCanonicalPendingEvent();
    Check(result.Committed && result.ProcessedEvent!.Value.Event.Id == new SimulationEventId(2), "one canonical event committed");
    Check(engine.State.MarkerValue == 1 && engine.State.Revision.Value == 1 && timeline.PendingCount == 1 && timeline.Revision.Value == beforeTimeline.Value + 1 && clock.CurrentTime.Ticks == 10, "atomic state and timeline commit");
    Check(engine.ProcessedCount == 1 && engine.TryGetProcessed(0, out var processed) && processed.ExecutionTime.Ticks == 10 && processed.TimelineRevisionBefore == beforeTimeline && processed.TimelineRevisionAfter == timeline.Revision && processed.StateRevisionBefore == StateRevision.Zero && processed.StateRevisionAfter == engine.State.Revision, "append-only processed history");

    Check(timeline.Schedule(clock.CurrentTime, Request(3, 10, 0)).Succeeded, "failure schedule");
    var invalid = engine.EvaluateNext() with { ExpectedTimelineRevision = TimelineRevision.Zero };
    var stateBefore = engine.State; beforeTimeline = timeline.Revision; var historyBefore = engine.ProcessedCount;
    var failed = engine.ValidateAndCommit(invalid);
    Check(!failed.Committed && failed.Validation.Status == SimulationTransactionValidationStatus.TimelineRevisionMismatch, "controlled validation failure");
    Check(engine.State == stateBefore && timeline.Revision == beforeTimeline && timeline.PendingCount == 2 && engine.ProcessedCount == historyBefore && clock.CurrentTime.Ticks == 10, "failed validation leaves all authority unchanged");

    var allocationTimeline = new SimulationTimeline(5_000); var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); var allocationEngine = new SimulationTransactionEngine(allocationClock, new SimulationState(), 5_000);
    for (ulong id = 1; id <= 5_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, 0)).Succeeded, "allocation transaction schedule");
    _ = allocationEngine.EvaluateNext();
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 5_000; index++) Check(allocationEngine.ExecuteCanonicalPendingEvent().Committed, "allocation transaction commit");
    Check(GC.GetAllocatedBytesForCurrentThread() == before && allocationEngine.ProcessedCount == 5_000, "preallocated transaction execution allocates zero bytes");

    var hash = TransactionHash(); Check(TransactionHash() == hash, "deterministic transaction replay hash");
    Console.WriteLine($"Deterministic transaction replay hash: 0x{hash:X16}");
}

static void CanonicalGroupTests()
{
    var emptyEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, new SimulationTimeline()), new SimulationState());
    Check(emptyEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.NoPendingEvent, "empty canonical group");

    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 0, 1)).Succeeded, "group schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 0, -1)).Succeeded, "group schedule B");
    Check(timeline.Schedule(SimulationInstant.Zero, RequestWithKind(3, 0, 0, SimulationEventKind.NoOpMarker)).Succeeded, "group no-op schedule");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(4, 1, 0)).Succeeded, "later group schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 8);
    var group = engine.ExecuteCanonicalGroup();
    Check(group.Reason == SimulationCanonicalGroupStopReason.Completed && group.IsComplete && group.GroupTime == SimulationInstant.Zero && group.ProcessedEventCount == 3 && group.PendingEvent!.Value.Id == new SimulationEventId(4), "canonical group completion");
    Check(engine.State.MarkerValue == 2 && engine.State.Revision.Value == 2 && timeline.PendingCount == 1 && timeline.Revision.Value == 7 && clock.CurrentTime == SimulationInstant.Zero, "per-event revisions and later pending event");
    Check(engine.TryGetProcessed(0, out var first) && engine.TryGetProcessed(1, out var second) && engine.TryGetProcessed(2, out var third) && first.Event.Id == new SimulationEventId(2) && second.Event.Id == new SimulationEventId(3) && third.Event.Id == new SimulationEventId(1), "canonical history ordering");

    var capTimeline = new SimulationTimeline(4);
    for (ulong id = 10; id <= 12; id++) Check(capTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, (int)id)).Succeeded, "cap schedule");
    var capEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, capTimeline, settings: new SimulationClockSettings(2)), new SimulationState(), 4);
    var capped = capEngine.ExecuteCanonicalGroup();
    Check(capped.Reason == SimulationCanonicalGroupStopReason.EventLimitReached && capped.ProcessedEventCount == 2 && capTimeline.PendingCount == 1 && capped.PendingEvent!.Value.Id == new SimulationEventId(12), "event cap preserves pending order");
    Check(capEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.Completed && capEngine.ProcessedCount == 3, "group resumes at same boundary");

    var failureTimeline = new SimulationTimeline(4);
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(20, 0, -1)).Succeeded, "failure marker schedule");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(21, 0, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "failure invalid schedule");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(22, 0, 1)).Succeeded, "failure later schedule");
    var failureEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, failureTimeline), new SimulationState(), 4);
    var failed = failureEngine.ExecuteCanonicalGroup();
    Check(failed.Reason == SimulationCanonicalGroupStopReason.ValidationRejected && failed.ProcessedEventCount == 1 && failed.PendingEvent!.Value.Id == new SimulationEventId(21), "partial group rejection");
    Check(failureEngine.State.MarkerValue == 1 && failureEngine.ProcessedCount == 1 && failureTimeline.PendingCount == 2, "earlier commit retained and later events pending");
    Check(failureEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.ValidationRejected, "rejected group remains resumable without loss");
    Check(failureEngine.ExecuteCanonicalGroupWhileGuardedForTest().Reason == SimulationCanonicalGroupStopReason.ReentrantExecution, "reentrant group rejected");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(99, 0, 0)).Succeeded, "group allocation warmup schedule");
    _ = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, warmTimeline), new SimulationState(), 1).ExecuteCanonicalGroup();
    var allocationTimeline = new SimulationTimeline(5_000);
    for (ulong id = 1; id <= 5_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, (int)id)).Succeeded, "group allocation schedule");
    var allocationEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, allocationTimeline), new SimulationState(), 5_000);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var allocationResult = allocationEngine.ExecuteCanonicalGroup();
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocationResult.IsComplete && allocationEngine.ProcessedCount == 5_000, "preallocated canonical group execution allocates zero bytes");

    var hash = CanonicalGroupHash(); Check(CanonicalGroupHash() == hash, "canonical group permutation replay hash");
    Console.WriteLine($"Deterministic canonical-group replay hash: 0x{hash:X16}");
}

static void ClockExecutionTests()
{
    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 10, -1)).Succeeded, "orchestration schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, RequestWithKind(2, 10, 0, SimulationEventKind.NoOpMarker)).Succeeded, "orchestration schedule no-op");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(3, 30, 0)).Succeeded, "orchestration later schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 8);
    var first = engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(20));
    Check(first.Reason == SimulationExecutionStopReason.Completed && first.InitialAdvanceReason == SimulationAdvanceStopReason.ReachedEventBoundary && first.ContinuationAdvanceReason == SimulationAdvanceStopReason.ReachedTarget && first.Group!.Value.ProcessedEventCount == 2 && first.ReachedTime.Ticks == 20, "coast execute and resume");
    Check(engine.State.MarkerValue == 1 && engine.State.Revision.Value == 1 && engine.ProcessedCount == 2 && timeline.PendingCount == 1 && timeline.TryPeekPending(out var later) && later.Header.Id == new SimulationEventId(3), "later timestamp remains pending");
    var second = engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(100));
    Check(second.Reason == SimulationExecutionStopReason.Completed && second.Group!.Value.ProcessedEventCount == 1 && clock.CurrentTime.Ticks == 100 && timeline.PendingCount == 0, "subsequent boundary executes once");
    Check(engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(101)).Reason == SimulationExecutionStopReason.ReachedTarget, "empty coast diagnostics");

    var failureTimeline = new SimulationTimeline(4);
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(10, 5, -1)).Succeeded, "orchestration failure marker");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(11, 5, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "orchestration failure invalid");
    var failureClock = new SimulationClock(SimulationInstant.Zero, failureTimeline);
    var failureEngine = new SimulationTransactionEngine(failureClock, new SimulationState(), 4);
    var failure = failureEngine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(50));
    Check(failure.Reason == SimulationExecutionStopReason.ValidationRejected && failure.Group!.Value.ProcessedEventCount == 1 && failure.ReachedTime.Ticks == 5, "validation rejection diagnostics");
    Check(failureEngine.State.MarkerValue == 1 && failureEngine.ProcessedCount == 1 && failureTimeline.PendingCount == 1 && failureTimeline.TryPeekPending(out var rejected) && rejected.Header.Id == new SimulationEventId(11), "rejection preserves failing authority");
    Check(failureEngine.AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(new SimulationInstant(50)).Reason == SimulationExecutionStopReason.ReentrantExecution, "orchestration reentrancy rejected");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(90, 1, 0)).Succeeded, "orchestration allocation warmup schedule");
    _ = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, warmTimeline), new SimulationState(), 1).AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(2));
    var allocationTimeline = new SimulationTimeline(1_000);
    for (ulong id = 1; id <= 1_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 1, (int)id)).Succeeded, "orchestration allocation schedule");
    var allocationEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, allocationTimeline), new SimulationState(), 1_000);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var allocation = allocationEngine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(2));
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocation.Reason == SimulationExecutionStopReason.Completed && allocationEngine.ProcessedCount == 1_000, "preallocated orchestration allocates zero bytes");

    var hash = ClockExecutionHash(); Check(ClockExecutionHash() == hash, "deterministic orchestration hash");
    Console.WriteLine($"Deterministic clock-orchestration hash: 0x{hash:X16}");
}

static SimulationEventHeader Header(ulong id, long time, int priority, ulong sequence) => new(new SimulationEventId(id), new SimulationInstant(time), priority, new SimulationEventSequence(sequence), SimulationEventKind.Marker);
static SimulationEventRequest Request(ulong id, long time, int priority) => new(new SimulationEventId(id), new SimulationInstant(time), priority, SimulationEventKind.Marker);
static SimulationEventRequest RequestWithKind(ulong id, long time, int priority, SimulationEventKind kind) => new(new SimulationEventId(id), new SimulationInstant(time), priority, kind);
static SimulationEventRequest[] CanonicalHeaders(){var random=new FixedRandom(0xD6E8FEB86659FD93);var values=new SimulationEventRequest[1024];for(var index=0;index<values.Length;index++){values[index]=Request((ulong)index+1,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%2001)-1000);}Array.Sort(values,static(left,right)=>{var time=left.Time.CompareTo(right.Time);if(time!=0)return time;var priority=left.Priority.CompareTo(right.Priority);return priority!=0?priority:left.Id.CompareTo(right.Id);});return values;}
static void ShuffleRequests(SimulationEventRequest[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong TimelineHash(ReadOnlySpan<SimulationEventRequest> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong ClockHash(){var timeline=new SimulationTimeline(4);Check(timeline.Schedule(SimulationInstant.Zero,Request(10,30,0)).Succeeded,"clock hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(11,10,0)).Succeeded,"clock hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var first=clock.AdvanceTo(new SimulationInstant(100));var second=clock.AdvanceUntilNextEvent();ulong hash=14695981039346656037;hash=Mix(hash,(ulong)first.ReachedTime.Ticks);hash=Mix(hash,first.BoundaryEvent!.Value.Id.Value);hash=Mix(hash,(ulong)second.ReachedTime.Ticks);return Mix(hash,second.BoundaryEvent!.Value.Id.Value);}
static ulong TransactionHash(){var timeline=new SimulationTimeline(32);for(ulong id=1;id<=32;id++)Check(timeline.Schedule(SimulationInstant.Zero,Request(id,0,(int)(id%3))).Succeeded,"hash transaction schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var engine=new SimulationTransactionEngine(clock,new SimulationState(),32);ulong hash=14695981039346656037;while(timeline.PendingCount!=0){var result=engine.ExecuteCanonicalPendingEvent();Check(result.Committed,"hash transaction commit");var entry=result.ProcessedEvent!.Value;hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);hash=Mix(hash,entry.TimelineRevisionAfter.Value);}return hash;}
static ulong CanonicalGroupHash(){var requests=new[]{Request(31,0,2),Request(32,0,-1),RequestWithKind(33,0,0,SimulationEventKind.NoOpMarker),Request(34,0,1)};ulong expected=0;for(var pass=0;pass<8;pass++){var shuffled=(SimulationEventRequest[])requests.Clone();ShuffleRequests(shuffled,(ulong)(pass+101));var timeline=new SimulationTimeline(4);foreach(var request in shuffled)Check(timeline.Schedule(SimulationInstant.Zero,request).Succeeded,"group permutation schedule");var engine=new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero,timeline),new SimulationState(),4);Check(engine.ExecuteCanonicalGroup().IsComplete,"group permutation execute");ulong hash=14695981039346656037;for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"group permutation history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);hash=Mix(hash,entry.TimelineRevisionAfter.Value);}if(pass==0)expected=hash;else Check(hash==expected,"group permutation canonical order");}return expected;}
static ulong ClockExecutionHash(){var timeline=new SimulationTimeline(3);Check(timeline.Schedule(SimulationInstant.Zero,Request(41,10,-1)).Succeeded,"orchestration hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(42,10,0,SimulationEventKind.NoOpMarker)).Succeeded,"orchestration hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(43,20,0)).Succeeded,"orchestration hash schedule");var engine=new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero,timeline),new SimulationState(),3);var first=engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(15));var second=engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(30));ulong hash=14695981039346656037;hash=Mix(hash,(ulong)first.Reason);hash=Mix(hash,(ulong)first.ReachedTime.Ticks);hash=Mix(hash,(ulong)second.Reason);hash=Mix(hash,(ulong)second.ReachedTime.Ticks);for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"orchestration hash history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);}return hash;}
static ulong HostDurationHash(){var clock=new SimulationClock(SimulationInstant.Zero,new SimulationTimeline(),new SimulationRate(5,7));ulong hash=14695981039346656037;for(var index=0;index<1024;index++){var result=clock.AdvanceByHostDuration(new SimulationDuration(13));hash=Mix(hash,(ulong)result.DerivedSimulationDuration.Ticks);hash=Mix(hash,(ulong)result.DebtAfter.Ticks);hash=Mix(hash,(ulong)result.RateRemainderAfter);}return hash;}
static ulong HostDurationDebtServiceHash(){var timeline=new SimulationTimeline(4);Check(timeline.Schedule(SimulationInstant.Zero,Request(70,3,1)).Succeeded,"debt hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(71,7,-1)).Succeeded,"debt hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(72,7,0,SimulationEventKind.NoOpMarker)).Succeeded,"debt hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var engine=new SimulationTransactionEngine(clock,new SimulationState(),4);_=clock.AdvanceByHostDuration(new SimulationDuration(11));var result=engine.ServicePendingHostDurationDebt();ulong hash=14695981039346656037;hash=Mix(hash,(ulong)result.Reason);hash=Mix(hash,(ulong)result.ReachedTime.Ticks);hash=Mix(hash,(ulong)result.ProcessedEventCount);hash=Mix(hash,(ulong)result.DebtAfter.Ticks);for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"debt hash history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);}return hash;}
static ulong HostDurationLongRunHash(){const int count=128;var timeline=new SimulationTimeline(count);for(ulong id=1;id<=count;id++)Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(id,(long)id*5,(int)(id%3),id%5==0?SimulationEventKind.NoOpMarker:SimulationEventKind.Marker)).Succeeded,"long hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline,new SimulationRate(5,7),new SimulationClockSettings(16));var engine=new SimulationTransactionEngine(clock,new SimulationState(),count);ulong hash=14695981039346656037;for(var cycle=0;cycle<count;cycle++){var converted=clock.AdvanceByHostDuration(new SimulationDuration(7));var serviced=engine.ServicePendingHostDurationDebt();Check(converted.Reason==SimulationHostAdvanceStopReason.Accepted&&serviced.Reason==SimulationDebtServiceStopReason.Completed,"long hash service");hash=Mix(hash,(ulong)converted.DerivedSimulationDuration.Ticks);hash=Mix(hash,(ulong)serviced.ReachedTime.Ticks);hash=Mix(hash,(ulong)serviced.ProcessedEventCount);hash=Mix(hash,(ulong)serviced.ExecutedGroupCount);hash=Mix(hash,(ulong)serviced.LastGroupStopReason!.Value);}Check(clock.CurrentTime.Ticks==count*5&&engine.ProcessedCount==count&&timeline.PendingCount==0,"long hash completion");return Mix(hash,engine.State.Revision.Value);}
static ulong MixedTimelineHash(){var timeline=new SimulationTimeline(4096);var random=new FixedRandom(0xA24BAED4963EE407);var active=new SimulationEventId[4096];var activeCount=0;ulong nextId=1;for(var operation=0;operation<4096;operation++){if(activeCount==0||(random.Next()%3)!=0){var request=Request(nextId++,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%101)-50);Check(timeline.Schedule(new SimulationInstant(long.MinValue),request).Succeeded,"mixed schedule");active[activeCount++]=request.Id;}else{var index=(int)(random.Next()%(ulong)activeCount);Check(timeline.Cancel(active[index]).Succeeded,"mixed cancel");active[index]=active[--activeCount];}Check(timeline.ValidateInvariants(),"mixed invariant");}var pending=new ScheduledSimulationEvent[timeline.PendingCount];timeline.CopyPending(pending);Array.Sort(pending,static(left,right)=>SimulationEventHeaderComparer.Compare(left.Header,right.Header));ulong hash=14695981039346656037;foreach(var value in pending){hash=Mix(hash,(ulong)value.Header.Time.Ticks);hash=Mix(hash,(uint)value.Header.Priority);hash=Mix(hash,value.Header.Sequence.Value);hash=Mix(hash,value.Header.Id.Value);}return Mix(hash,timeline.Revision.Value);}
static SimulationEventHeader[] CreateStressHeaders(){var random=new FixedRandom(0x6A09E667F3BCC909);var result=new SimulationEventHeader[2048];for(var index=0;index<result.Length;index++){var time=(long)(random.Next()>>1)-long.MaxValue/2;var priority=(int)(random.Next()%2001)-1000;result[index]=Header((ulong)index+1,time,priority,(ulong)index+1);}return result;}
static void Shuffle(SimulationEventHeader[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong Hash(ReadOnlySpan<SimulationEventHeader> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Sequence.Value);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong Mix(ulong hash,ulong value){for(var index=0;index<8;index++){hash^=(byte)value;hash*=1099511628211;value>>=8;}return hash;}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
static void Throws<T>(Action action) where T:Exception {try{action();throw new Exception($"Expected {typeof(T).Name}.");}catch(T){}}
struct FixedRandom(ulong state){private ulong _state=state;public ulong Next(){_state^=_state>>12;_state^=_state<<25;_state^=_state>>27;return _state*2685821657736338717UL;}}
