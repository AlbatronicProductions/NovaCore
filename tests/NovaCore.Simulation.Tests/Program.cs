using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Clock;

var tests = new (string Name, Action Test)[]
{
    ("SimulationInstant", InstantTests),
    ("SimulationDuration", DurationTests),
    ("SimulationRate", RateTests),
    ("Event ordering", EventOrderingTests),
    ("Timeline topology", TimelineTopologyTests),
    ("Simulation clock", ClockTests),
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

static SimulationEventHeader Header(ulong id, long time, int priority, ulong sequence) => new(new SimulationEventId(id), new SimulationInstant(time), priority, new SimulationEventSequence(sequence), SimulationEventKind.Marker);
static SimulationEventRequest Request(ulong id, long time, int priority) => new(new SimulationEventId(id), new SimulationInstant(time), priority, SimulationEventKind.Marker);
static SimulationEventRequest[] CanonicalHeaders(){var random=new FixedRandom(0xD6E8FEB86659FD93);var values=new SimulationEventRequest[1024];for(var index=0;index<values.Length;index++){values[index]=Request((ulong)index+1,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%2001)-1000);}Array.Sort(values,static(left,right)=>{var time=left.Time.CompareTo(right.Time);if(time!=0)return time;var priority=left.Priority.CompareTo(right.Priority);return priority!=0?priority:left.Id.CompareTo(right.Id);});return values;}
static void ShuffleRequests(SimulationEventRequest[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong TimelineHash(ReadOnlySpan<SimulationEventRequest> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong ClockHash(){var timeline=new SimulationTimeline(4);Check(timeline.Schedule(SimulationInstant.Zero,Request(10,30,0)).Succeeded,"clock hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(11,10,0)).Succeeded,"clock hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var first=clock.AdvanceTo(new SimulationInstant(100));var second=clock.AdvanceUntilNextEvent();ulong hash=14695981039346656037;hash=Mix(hash,(ulong)first.ReachedTime.Ticks);hash=Mix(hash,first.BoundaryEvent!.Value.Id.Value);hash=Mix(hash,(ulong)second.ReachedTime.Ticks);return Mix(hash,second.BoundaryEvent!.Value.Id.Value);}
static ulong MixedTimelineHash(){var timeline=new SimulationTimeline(4096);var random=new FixedRandom(0xA24BAED4963EE407);var active=new SimulationEventId[4096];var activeCount=0;ulong nextId=1;for(var operation=0;operation<4096;operation++){if(activeCount==0||(random.Next()%3)!=0){var request=Request(nextId++,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%101)-50);Check(timeline.Schedule(new SimulationInstant(long.MinValue),request).Succeeded,"mixed schedule");active[activeCount++]=request.Id;}else{var index=(int)(random.Next()%(ulong)activeCount);Check(timeline.Cancel(active[index]).Succeeded,"mixed cancel");active[index]=active[--activeCount];}Check(timeline.ValidateInvariants(),"mixed invariant");}var pending=new ScheduledSimulationEvent[timeline.PendingCount];timeline.CopyPending(pending);Array.Sort(pending,static(left,right)=>SimulationEventHeaderComparer.Compare(left.Header,right.Header));ulong hash=14695981039346656037;foreach(var value in pending){hash=Mix(hash,(ulong)value.Header.Time.Ticks);hash=Mix(hash,(uint)value.Header.Priority);hash=Mix(hash,value.Header.Sequence.Value);hash=Mix(hash,value.Header.Id.Value);}return Mix(hash,timeline.Revision.Value);}
static SimulationEventHeader[] CreateStressHeaders(){var random=new FixedRandom(0x6A09E667F3BCC909);var result=new SimulationEventHeader[2048];for(var index=0;index<result.Length;index++){var time=(long)(random.Next()>>1)-long.MaxValue/2;var priority=(int)(random.Next()%2001)-1000;result[index]=Header((ulong)index+1,time,priority,(ulong)index+1);}return result;}
static void Shuffle(SimulationEventHeader[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong Hash(ReadOnlySpan<SimulationEventHeader> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Sequence.Value);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong Mix(ulong hash,ulong value){for(var index=0;index<8;index++){hash^=(byte)value;hash*=1099511628211;value>>=8;}return hash;}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
static void Throws<T>(Action action) where T:Exception {try{action();throw new Exception($"Expected {typeof(T).Name}.");}catch(T){}}
struct FixedRandom(ulong state){private ulong _state=state;public ulong Next(){_state^=_state>>12;_state^=_state<<25;_state^=_state>>27;return _state*2685821657736338717UL;}}
