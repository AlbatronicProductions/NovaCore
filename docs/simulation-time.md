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

### FUTURE ROADMAP — SimulationSnapshot

A later milestone will introduce an immutable `SimulationSnapshot` tied to one explicit `SimulationInstant`. It is expected to carry the matching `TimelineRevision`, `StateRevision`, authoritative state or a stable state reference, the matching `ReferenceFrameSnapshot`, deterministic identity/version information, and optionally a canonical state/event hash. That future boundary will support replay validation, save/load foundations, debugging, prediction invalidation, rollback, and future synchronization while preserving the flow: authoritative simulation → evaluated reference frames → resolved render snapshot → Graphics transport. None of those snapshot, rollback, networking, save/load, replay-restoration, or cross-platform bitwise-determinism capabilities exists today.

Simulation clocks, event execution, analytical propagation, snapshots, time warp controls, reference-frame integration, and sample integration remain pending Milestone 6 work.

## Intentional 6A limitations

The value types are allocation-free for normal arithmetic, comparison, and rate scaling. Diagnostic formatting (`ToString`) and exceptional validation paths may allocate, and are not part of the steady-state contract. `FromSecondsRounded` is explicitly derived-input convenience only; it rounds to the nearest microtick using ties-to-even and must not be used for authoritative serialized timestamps. A carried rate remainder belongs to one normalized rate denominator and must be reset when that rate changes. No clock, event queue, payload, or timeline exists yet.
