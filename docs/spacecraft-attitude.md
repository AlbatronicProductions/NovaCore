# Spacecraft Attitude

Milestone 8A-2 integrates the internal attitude contract into the authoritative fixed spacecraft store. `SimulationState` owns the store and exposes only `SpacecraftStateView` to evaluators. Declaration order and IDs are stable; warmed lookup and attitude evaluation allocate no managed memory.

`SpacecraftAttitudeState` identifies a nonzero `SpacecraftId`, an exact `SimulationInstant` epoch, a local-to-parent `DoubleQuaternion`, a body-frame angular velocity in radians per second, and `ConstantBodyAngularVelocityV1`. The state is a value contract; `SpacecraftAttitudeEvaluator` is pure and returns a controlled status instead of using expected failures as exceptions.

NovaCore uses XYZW Hamilton active quaternions. `q.Rotate(localDirection)` produces the parent-frame direction, and composition `parent * local` applies the right-hand operand first. Spacecraft body axes are `+X` forward, `+Y` right, and `+Z` down; up is `-Z`.

For constant body angular velocity `ω_body`, evaluation at an arbitrary exact requested instant uses:

```text
Δt = requestedTime - epoch
q(t) = q(epoch) * Exp(0.5 * ω_body * Δt)
```

The evaluator converts the exact tick delta to derived double seconds only for the closed-form mathematical calculation. It does not integrate over render frames, mutate input state, consult wall time, or advance `SimulationClock`. The same valid input and requested instant therefore produce the same canonical attitude on the supported x64 runtime.

Orientation inputs are rejected when non-finite or when squared norm is at most `1e-24` (norm at most `1e-12`). Valid quaternions are normalized and canonicalized: prefer `W > 0`; when `W` is exactly zero, prefer the first nonzero component in `X`, `Y`, `Z` to be positive. Signed zero is cleared before that exact tie-break. This is intentionally not an epsilon sign test. Angular velocity must be finite. Evaluation is bounded to the same signed microtick span as the pure two-body evaluator (plus or minus `2^31` seconds); tick subtraction overflow and every invalid result are reported by an explicit status.

The only supported model is constant body angular velocity. Torque, inertia, RCS, propulsion, SAS, control input, celestial/frame extraction, persistence, and graphics integration remain deferred.

Direct immutable replacement candidates are built by a pure evaluator and committed only by `SimulationTransactionEngine`. A successful candidate atomically replaces one stored attitude, increments state revision once, and appends compact transition metadata. It does not advance the clock or consume an event in this slice.

The existing immutable reference-frame graph contains each spacecraft body frame beneath its declared carrier frame. At an exact clock instant, celestial extraction produces carrier position and velocity; spacecraft extraction produces zero body translation and origin velocity, evaluated local-to-parent orientation, and parent-space angular velocity `q.Rotate(ω_body)`. Graphics receives only the resolved derived transform later in the existing snapshot path.

RCS, propulsion, SAS, control input, persistence, and spacecraft graphics remain deferred.

Milestone 8B-4A adds pure guidance math. Flight-reference directions derive from exact Cartesian trajectory position and velocity, never camera-relative values. A deterministic target basis maps body `+X` to the selected direction, body `+Y` right, and body `+Z` down; degenerate up vectors use a fixed `+Z`, `+Y`, `+X` fallback order. The sample additionally supports local numeric SAS mode selection and exact-time hold-target capture, but it does not yet execute automatic SAS torque. Control cadence, authoritative SAS integration, and high-warp behavior remain deferred.

The Milestone 8B rigid-body contract evaluates prescribed constant body-space torque with diagonal principal inertia. When present in the store, it is the authoritative source used by body-frame extraction and exact-time replacement transactions; see [Rotational Dynamics](rotational-dynamics.md).
