# Independent spacecraft translational authority

**M14.2 — Establish independent spacecraft translational authority** is production
accepted but unbanked on `codex/spacecraft-translation`. The transient allocation
gate is resolved as CLR accounting; the test-only measurement boundary and all
technical closeout gates passed. Classified diagnostics await manual disposal
([attribution](engineering-evidence/spacecraft-translation/allocation-attribution.md)). No M14.2 tag was
created. M14.1 remains banked; M14 remains open.
[Validation and decision evidence](engineering-evidence/spacecraft-translation/README.md).

## State and ownership

Simulation owns spacecraft identity, immutable mass in kilograms, COM position in
metres, velocity in metres/second, applied net force in newtons, exact segment epoch
and root-frame identity. All spatial values are FP64. The translation/property
slots are part of the existing fixed `SpacecraftStateStore`; rotation-only craft
do not acquire an invented default mass or trajectory. Setup copies caller data.
Force transactions cannot change mass or spacecraft/frame identity.

`TryCreateTranslating` requires initial rotation and translation to have the same
craft ID and epoch. Its carrier must be the sole existing ECL root and its body
frame a direct child. The body-frame origin is the COM. Existing frame topology,
orientation, angular velocity, inertia, torque, guidance and celestial motion keep
their owners. The analytical sample still legitimately inherits its celestial
carrier; no sample/launcher behavior was changed.

## Force and integration

The supported model is fixed mass with piecewise-constant **net force in root
inertial axes**. Between explicit force-change events:

```text
dt = checked(requestedTicks - epochTicks) / ticksPerSecond
a = netForceRoot / mass
velocity = initialVelocity + a * dt
position = initialPosition + initialVelocity * dt + (a * dt) * (0.5 * dt)
```

This is the analytical solution of the declared physical model, subject to FP64
rounding. It has no numerical timestep, host/render delta, hidden clamps, velocity
resets, substep loop or mutable query cache. Finite inputs whose acceleration or
propagation overflows are rejected. No position-dependent gravity is claimed.
A known constant force provides the first physically meaningful dynamics proof;
celestial gravity/trajectory authority is not repurposed or changed.

The exact tick difference is formed before conversion to floating seconds, so a
microsecond interval at a late absolute epoch does not disappear through subtraction
of two rounded absolute times. Tick subtraction overflow and requests before the
current segment epoch fail explicitly. For long intervals, the exact timestamp
remains authoritative but its FP64 duration conversion has ordinary rounding.

`SpacecraftForceCommand` is intent containing ID, exact time and net root force.
It becomes a closed payload in the existing canonical timeline. At its boundary,
the evaluator advances the old segment through that instant, then proposes the
new force with continuous position and velocity. Applied force persists until an
explicit accepted change, including an explicit zero-force command. Repeating
the same force consumes the event and records history without rebasing the epoch
or incrementing state revision.

This API replaces a **net** force. It is not an additive contributor registry;
future gravity/thrust/contact contributors must be combined deterministically
before one net request. Root-fixed force does not rotate with the craft. A future
body-axis or state-dependent force model needs an explicit force evaluation cadence
and numerical qualification. It can reuse this state/commit owner; a frozen force
segment must not be mislabeled continuous gravity or body-following thrust.

## Transactions, time and publication

The flow is pure candidate evaluation → validation against the actual canonical
pending event and current state → one commit → revision/history publication.
Commit reconstructs the entire expected candidate; callers cannot inject arbitrary
position, velocity, mass or root changes. Wrapper metadata is checked as well.
Clock instant, timeline/state revisions, history capacity and revision overflow
are checked before mutation. Failure preserves the pending event and prior state.
History is preallocated at engine construction, as with existing bounded rigid-body
transactions; capacity exhaustion rejects explicitly, never discards evidence.

Pure evaluations do not mutate epochs. The same initial state and canonical event
schedule replay bit-identically on the validated runtime regardless of observation
frequency. The clock stops at pending force boundaries. Consumers must service
canonical events before publishing current motion; evaluating a segment past a
known pending event is only that segment's prediction, not executed future history.

Rotation and translation retain independent segment epochs, but
`SpacecraftMotionEvaluator` evaluates both at one requested instant and returns
one complete value carrying craft, time, root and state revision. Failure returns
default output. A request before either current segment epoch is rejected; recover
historical states from the appropriate retained event history rather than applying
the newest force or torque backward.

`SimulationStateView` is live-backed. Obtain a fresh view inside the existing
single-writer simulation phase; retaining it across a commit is not an immutable
snapshot or an asynchronous publication contract. The resulting `SpacecraftMotion`
value is self-contained. Serial same-time force/torque events are separately atomic;
they are **not** an atomic combined wrench transaction. A later off-center contact
force may require one jointly validated linear/angular replacement through this
same transaction owner. That response is not implemented here.

## Frames and future surface interaction

Integration happens only in the existing inertial root. Body-centered and rotating
body-fixed states are derived representations. In root coordinates:

```text
surfaceVelocity = bodyOriginVelocity + bodyAngularVelocity × rotatedSurfaceOffset
relativeContactVelocity = craftContactVelocity - surfaceVelocity
```

Use existing `ReferenceFrameMath`/resolvers to include origin motion and rotational
transport exactly once. Do not reset craft velocity or silently add planetary spin
when changing observation frame. Initial body-relative values can be explicitly
converted to the root before admission. Authoritative reparenting is unnecessary
for this model and is not implemented; no second frame graph is introduced.

Root FP64 precision scales with root magnitude. The fixtures qualify the stated
planetary-distance error envelope, not universal submillimetre precision at any
interplanetary coordinate. M14.1 retains its own physical normal/readiness contract.
Future contact can consume complete motion, mass/inertia, canonical query results
and correctly transported surface velocity. The point query is not a collider or
solver, and neither terrain nor authored pad contact is added here.

## Bounds and acceptance

Translation cost is constant per requested instant, including high simulation
rate; warp does not multiply rendering work. Existing rotation and timeline event
budgets still apply to complete motion and advancement. This does not solve
high-warp contact or arbitrary time-varying forces.

Permanent tests use dyadic closed-form checks, an independent decimal oracle,
scale-derived floating-point error bars, exact event/replay checks, moving/rotating
frame conversion, and failure atomicity. Tests also measure warmed allocations and
provide a bounded Release timing mode. No visible moving-craft scene was added;
manual visual acceptance is **NOT REQUIRED** for this candidate.
