# Simulation Time (Milestone 6A and 6B-1)

Milestone 6A establishes the managed temporal value types used by later simulation work. `SimulationInstant` is a signed 64-bit microtick timestamp relative to a project-defined epoch at tick zero. One simulated second is exactly 1,000,000 ticks, giving a range of approximately plus or minus 292,471 years. Arithmetic is checked; negative instants and durations are valid.

`SimulationRate` is a normalized positive rational. Scaling a nonnegative host duration uses `Int128` for `hostTicks * numerator + remainder`, then divides by the denominator. The caller owns the carried remainder; it is expressed in numerator units modulo the current denominator and must be reset when the rate changes.

Scheduled event headers have no payload. Their canonical ascending ordering is `(Time, Priority, Sequence, EventId)`.

## Pending-event timeline (Milestone 6B-1)

`SimulationTimeline` owns only pending-event topology: the indexed canonical min-heap, timeline-assigned sequences, permanently reserved caller-supplied event IDs, cancelled-header diagnostics, and `TimelineRevision`. It does not own current time, host time, event execution, or simulation state. Scheduling and replacement therefore receive the authoritative current `SimulationInstant` explicitly.

The first successful schedule receives sequence 1. Sequences are monotonic, never reused, and are consumed only by a successful schedule or replacement. Event ID zero is invalid; a nonzero ID remains reserved after cancellation or replacement. A request earlier than caller-supplied current time is rejected; a request at exactly that time is accepted.

Successful scheduling, cancellation, and replacement each increment `TimelineRevision` once. Rejected operations leave the heap, ID reservation, sequence allocator, cancellation history, and revision unchanged. Replacement is one topology operation: it requires a pending old ID, requires a new unused ID, assigns a new sequence, and increments the revision once. The heap maintains an EventId-to-index lookup while never deriving ordering from dictionary or hash-set enumeration.

Preallocation may allocate during setup. After adequate capacity has been supplied, the tested scheduling, cancellation, lookup, and heap-maintenance paths perform no managed allocation. Capacity growth, diagnostics, exceptions, and future payload storage are outside that claim.

### Explicit 6B-1 exclusions

There is no `SimulationClock`, event execution, processed-event history, payload framework, state transition evaluator, analytical trajectory, or snapshot implementation in 6B-1.

## Exact authoritative clock (Milestone 6B-2)

`SimulationClock` owns the managed authoritative `CurrentTime`, pause state, active normalized `SimulationRate`, carried rate remainder, advancement guard, configured `MaximumEventsPerAdvance`, and one injected `SimulationTimeline`. Its default event cap is 10,000. The clock does not own simulation state, reference frames, rendering, or native time.

`AdvanceTo(target)` is an explicit exact-time command and is allowed while paused. It never moves time backward. With no pending event, or when the canonical next event is after the target, it reaches the target. When the next event is at or before the target, it stops exactly at that event boundary and returns the canonical header. `AdvanceUntilNextEvent()` similarly moves to the next boundary, or returns `NoPendingEvent` without moving time.

In 6B-2, reaching a boundary does not execute, dequeue, cancel, consume, or otherwise mutate the event. Repeated advancement at or beyond the boundary intentionally returns the same pending event until 6B-3 introduces transactional event execution. Clock advancement does not change `TimelineRevision` or `StateRevision`.

Pause and resume are idempotent and do not move time, alter the rate, or catch up host time. `TrySetRate` changes only a non-equivalent normalized rate and resets its carried remainder; 6B-2 has no host-duration scaling, so that remainder remains zero in normal operation. 6B-4 will add rational host-duration advancement and its exact carried-debt semantics.

The clock rejects nested advancement with a controlled result and restores its guard with `finally`. It has no event-loop processing yet; the maximum-event setting is retained solely for the approved later execution API.

### Explicit 6B-2 exclusions

There is no event transition evaluator, state mutation, generated event, payload, processed-event history, `StateRevision` update, host-duration advancement, host-time debt, analytical trajectory, `SimulationSnapshot`, reference-frame integration, or sample integration. Milestone 6B-3 will add transactional event execution; Milestone 6B-4 will add host-duration rational advancement.

## Authoritative transaction contracts (Milestone 6B-3A)

Milestone 6B-3A establishes the managed authoritative mutation boundary. `SimulationClock` still owns time and `SimulationTimeline` still owns pending topology. `SimulationTransactionEngine` is the only component permitted to commit the minimal internal `SimulationState`, consume a canonical pending event, and append immutable processed history.

The flow is deliberately separated: scheduled simulation command → pure evaluation → immutable `SimulationTransaction` → validation → commit. Evaluators receive only `SimulationStateView`; they cannot mutate authoritative state. The current marker transaction is infrastructure proof only and does not represent gameplay, propulsion, trajectory propagation, or a payload framework.

Validation occurs before mutation and verifies that the event remains canonical, clock time matches the evaluation timestamp, expected timeline and state revisions still match, and the transaction is internally consistent. Normal validation failure returns a controlled result and leaves current time, pending topology, both revisions, processed history, and authoritative state unchanged.

After validation, history capacity is reserved before the irreversible sequence. A successful commit changes state, consumes the canonical event, advances `TimelineRevision` through that existing consumption operation, advances `StateRevision` exactly once for the marker-state change, advances clock time only after successful state/timeline mutation, and appends `ProcessedSimulationEvent`. Processed history is immutable append-only metadata containing the event header, execution time, and revision values before and after commit.

This is not event sourcing, replay implementation, or snapshot implementation. It is the stable mutation contract that later transaction kinds will use.

### FUTURE ROADMAP — SimulationSnapshot

A later milestone will introduce an immutable `SimulationSnapshot` tied to one explicit `SimulationInstant`. It is expected to carry the matching `TimelineRevision`, `StateRevision`, immutable authoritative state or stable state references, pending-event topology, processed-history version, the matching `ReferenceFrameSnapshot`, deterministic identity/version information, and optionally a canonical state/event hash. It will become the sole immutable simulation-facing snapshot for rendering and external consumers, preserving the flow: authoritative simulation → evaluated reference frames → resolved render snapshot → Graphics transport. None of snapshot publication, rollback, networking, save/load, replay restoration, or cross-platform bitwise determinism exists today.

Host-duration advancement, generated events, additional transaction kinds, analytical propagation, snapshots, time warp controls, reference-frame integration, and sample integration remain pending Milestone 6 work.

## Intentional 6A limitations

The value types are allocation-free for normal arithmetic, comparison, and rate scaling. Diagnostic formatting (`ToString`) and exceptional validation paths may allocate, and are not part of the steady-state contract. `FromSecondsRounded` is explicitly derived-input convenience only; it rounds to the nearest microtick using ties-to-even and must not be used for authoritative serialized timestamps. A carried rate remainder belongs to one normalized rate denominator and must be reset when that rate changes.
