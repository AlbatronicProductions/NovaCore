# Spacecraft Attitude

Milestone 8A-1 defines the internal, authoritative attitude contract for a future spacecraft simulation. It does not add a spacecraft store, transaction payload, frame extraction, rendering path, input path, or control system.

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

Future transaction work will make a validated attitude state part of authoritative `SimulationState`; a later immutable `SimulationSnapshot` will publish it to frame extraction and rendering. Graphics will receive only resolved derived transport data and will never evaluate or own spacecraft attitude.
