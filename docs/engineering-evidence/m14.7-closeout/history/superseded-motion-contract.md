# Spacecraft motion at exact physical-event epochs

**UNBANKED CANDIDATE — FOLLOW-UP REQUIRED.** Branch
`codex/exact-event-spacecraft-motion`; no milestone number assigned.
Qualification stopped at an existing Release Simulation allocation gate.
See [evidence](engineering-evidence/exact-event-spacecraft-motion/README.md).

## Responsibility and lifetime

`SpacecraftMotionEvaluator.TryEvaluateAtPhysicalEpoch` derives one complete
`SpacecraftPhysicalEventMotion` from existing canonical spacecraft authorities.
Call with a fresh `SimulationStateView` in the simulation single-writer phase,
as required by the canonical evaluator. A view is not an independently frozen
concurrent snapshot. The evaluator captures the immutable linear/angular segments
before numerical evaluation and retains no view or store reference in its result.

The immutable value contains spacecraft ID, exact requested epoch, source revision,
inertial-root frame, FP64 position/velocity, body-to-root quaternion, body angular
velocity, physical properties and principal inertia. The source revision is
provenance, not a self-authorizing receipt. A future mutation consumer must
requalify or transact under the appropriate authority/revision contract; this
candidate defines no such future protocol. Retained values do not update when
canonical state changes.

Evaluation does not advance the clock, process pending events, publish state,
append history or modify either physical segment. Values extrapolate those
captured segments under their current forces/torques; intervening future events
are not executed by this API.

## Exact time, evaluated values

**Exact PhysicalEventEpoch != exact FP64 physical state.**

Canonical embedded epochs delegate to the existing coherent evaluator, including
failure status and complete/default output. The distinct method name preserves
source compatibility for canonical callers using target-typed `new(...)`.

For rational epochs, internal `PhysicalEventDuration` first subtracts the stored
canonical segment epoch using `Int128`, then represents the signed magnitude as
whole ticks plus a proper fraction. Capacity is the existing signed Int64 tick
span. A fractional value beyond positive `long.MaxValue` ticks is refused;
negative `long.MinValue` magnitude fits the unsigned internal magnitude. No
absolute epoch is converted to double. There is no production BigInteger,
saturation, canonical snapping or new public duration framework.

Only the local duration is converted to FP64 seconds at the numerical boundary.
Translation preserves the analytical constant-force model: `v = v0 + a*t` and
`p = p0 + v0*t + (a*t)*(0.5*t)`. Rotation retains its existing inertia/body-torque
model, normalized quaternion convention, 10,000-tick RK4 step and bounded work.
Exact duration magnitude determines the number of full steps; a noncanonical
endpoint adds one fractional terminal step. Spherical torque-free rotation uses
the existing analytical constant-angular-velocity kernel. The existing duration
and maximum-step bounds remain in force. FP64 rounding at this final numerical
boundary does not change the retained epoch identity.

The two stored segment epochs may differ following canonical transactions. Both
components evaluate at the requested epoch; requests before either segment are
refused. Canonical `SimulationInstant`, event ordering and serialization remain
unchanged.

## Failure behavior

The coherent API retains `SpacecraftTranslationStatus`: missing subject, invalid
mass/state, time before segment, duration overflow, nonfinite translation, or
rotation evaluation failure. Lower rotation evaluation retains its specific
validation/duration/step-limit status. Failed coherent evaluation returns default,
never a partial linear/angular result. Default `PhysicalEventEpoch` is valid
canonical zero, not an invalid sentinel.

## Not contact proof

This result is not proof of impact, proof of represented `radialSignedGap == 0`,
a root/contact certificate, an M14.3 observation or M14.5 admissibility proof.
Exact event identity and evaluated motion do not guarantee represented zero gap
in terrain/contact arithmetic. No event discovery, CCD, root solving, response,
fractional scheduling, private frontier, remaining-interval execution, grounding
or landing is implemented.

## Validation boundary

Debug focused and full-suite evidence covers independent exact-rational duration
and translation oracles, canonical bit parity, analytical principal-axis rotation,
independent midpoint convergence, failure boundaries, replay and authority
immutability. Warmed new evaluation measured zero bytes using the accepted
checked ordinary no-GC helper. Release exact-event correctness/performance and
the remaining standalone regression gates are not yet qualified because the
full Release suite hit the ticket's explicit stop condition.
