# Simulation Time (Milestone 6A)

Milestone 6A establishes the managed temporal value types used by later simulation work. `SimulationInstant` is a signed 64-bit microtick timestamp relative to a project-defined epoch at tick zero. One simulated second is exactly 1,000,000 ticks, giving a range of approximately plus or minus 292,471 years. Arithmetic is checked; negative instants and durations are valid.

`SimulationRate` is a normalized positive rational. Scaling a nonnegative host duration uses `Int128` for `hostTicks * numerator + remainder`, then divides by the denominator. The caller owns the carried remainder; it is expressed in numerator units modulo the current denominator and must be reset when the rate changes.

Scheduled event headers have no payload in 6A. Their canonical ascending ordering is `(Time, Priority, Sequence, EventId)`. Sequence assignment is reserved for the future authoritative timeline transaction.

Simulation clocks, queues, timelines, analytical propagation, snapshots, time warp controls, reference-frame integration, and sample integration remain pending Milestone 6 work.

## Intentional 6A limitations

The value types are allocation-free for normal arithmetic, comparison, and rate scaling. Diagnostic formatting (`ToString`) and exceptional validation paths may allocate, and are not part of the steady-state contract. `FromSecondsRounded` is explicitly derived-input convenience only; it rounds to the nearest microtick using ties-to-even and must not be used for authoritative serialized timestamps. A carried rate remainder belongs to one normalized rate denominator and must be reset when that rate changes. No clock, event queue, payload, or timeline exists yet.
