# Celestial simulation foundation

Milestone 7A introduces internal managed contracts for authoritative celestial data. It does not propagate trajectories, evaluate reference-frame transforms from body state, schedule celestial events, publish `SimulationSnapshot`, or change rendering.

`CelestialBodyId`, `ReferenceFrameId`, `SimulationEventId`, and `RenderObjectId` remain separate value identities. A `CelestialBodyDefinition` contains only a stable celestial ID, an optional primary-body ID, its inertial frame, and positive finite gravitational parameter (μ). μ is stored directly because analytical two-body propagation consumes μ; mass and a gravitational-constant/unit framework are intentionally deferred.

The canonical future propagation input is `CartesianState`: finite double-precision position and velocity at an exact `SimulationInstant` epoch. `TwoBodyTrajectory` pairs that state with a central-body identity and a narrow model version. It contains no orbital elements, evaluated cache, anomaly, solver state, render data, or reference-frame transform.

Root bodies have no primary and explicitly have no trajectory. Non-root bodies have exactly one trajectory whose central body matches their declared primary. This avoids a fake self-orbit or a hidden invalid central-body ID.

`SimulationState` owns the fixed-size internal `CelestialStateStore`. The store copies fully validated definitions and body states, retains caller declaration order as the canonical traversal order, and constructs a setup-time sorted ID index for allocation-free lookup. Its `CelestialStateView` exposes immutable indexed and ID-based reads only. The transaction engine remains the sole future authority for changing celestial records; 7A adds no celestial mutation API.

All position, velocity, and μ values must use one internally consistent simulation unit system. NovaCore does not introduce a unit framework in this slice.

## Pure elliptic propagation

Milestone 7B defines `CartesianTwoBodyV1` as pure elliptic universal-variable f/g evaluation. It receives immutable Cartesian state at its exact epoch, one requested `SimulationInstant`, and the central body's μ. It returns a controlled `TwoBodyPropagationResult`; it does not mutate `SimulationState`, the clock, revisions, event topology, processed history, reference frames, or renderer data.

The celestial domain adapter resolves a subject body and its primary through `CelestialStateView`, verifies that the trajectory central body matches the declared primary, and reads μ only from the primary definition. The mathematical core has no celestial-store access.

Authoritative celestial mechanics use SI: position in metres, velocity in metres per second, elapsed propagation time in seconds, and μ in m³/s². Existing non-celestial presentation fixtures retain their generic units.

The first supported regimes are circular through high-eccentricity elliptic trajectories, including backward-time evaluation. Hyperbolic, parabolic or near-parabolic, radial/zero-angular-momentum, and degenerate-radius inputs return controlled statuses. Near-parabolic classification uses `abs(alpha * r0) <= 1e-10`; tested high-eccentricity elliptical inputs remain well outside this boundary.

Exact timestamp subtraction is checked, then microticks convert once using `deltaTicks / 1,000,000.0`. The evaluator supports a numerical interval of ±2^31 seconds (±2,147,483,648,000,000 microticks), not a restriction on `SimulationInstant`. For elliptic conditioning over many revolutions, the solver reduces its local interval by the analytically derived period; the requested authoritative timestamp and stored epoch remain unchanged.

Stumpff C/S functions use a fixed polynomial branch through `z = 1e-4`, then direct trigonometric forms for positive z. Universal anomaly solving uses a signed alpha-based initial guess, a bounded 64-expansion bracket, safeguarded Newton steps, midpoint fallback, and a 48-iteration cap. Its residual limit is `2^-48 * max(|r0 * chi|, |sqrt(mu) * deltaSeconds|, double.Epsilon)`, matching the universal Kepler equation's length-to-the-three-halves scale. Normal failure uses allocation-free statuses rather than exceptions.

On the tested .NET x64 runtime, warmed direct propagation, domain-adapter evaluation, and controlled failure paths allocate zero managed bytes. Deterministic raw hashes cover circular, elliptic, backward, and validation sequences. No cross-platform bitwise determinism claim is made.

Future slices will add transaction-based trajectory replacement and celestial-to-frame evaluation. Hyperbolic/parabolic propagation, numerical integration, reference-frame integration, snapshots, and rendering remain deferred.

## Authoritative trajectory replacement

Milestone 7C-1 adds the first celestial discontinuity without changing pure coast evaluation. `UniversalVariableTwoBodyPropagator` and `CelestialTrajectoryEvaluator` remain read-only: evaluating a trajectory at an arbitrary instant never changes the stored trajectory, revisions, event topology, clock, or processed history.

The permanent mutation primitive is an internal immutable `CelestialTrajectoryReplacementTransaction`. It binds one canonical `ReplaceTrajectory` event header, exact event/evaluation time, expected timeline and state revisions, subject body, expected current trajectory, and a complete replacement `TwoBodyTrajectory`. Candidate construction reads `SimulationStateView` only. There is no scheduled celestial payload or delta-v intent in this slice.

At commit, `SimulationTransactionEngine` validates the canonical pending event, exact event-time epoch, clock boundary, revisions, subject/primary relationship, expected trajectory bits, finite replacement Cartesian state, supported model, central μ, and immediate elliptic evaluatability. Root bodies, topology changes, no-op replacements, stale candidates, radial or degenerate replacements, hyperbolic/parabolic replacements, and invalid states are controlled rejections.

After history capacity and revision/timeline checks succeed, the engine replaces exactly one engine-owned `CelestialStateStore` trajectory slot in place, increments `StateRevision` once, consumes the canonical event (advancing `TimelineRevision`), advances the clock to the already-canonical event instant, and appends one immutable processed record. The store itself owns no revisions, clock state, or history. Every controlled rejection leaves authoritative celestial values, marker value, revisions, pending topology, clock time, and history unchanged.

Processed celestial transition metadata records the subject, exact event time, prior and replacement epochs, state revisions, and stable raw trajectory hashes. Same-time replacements remain separate transactions in canonical `(time, priority, sequence, event ID)` order; a later candidate must be evaluated against the trajectory committed by earlier successful events.

Impulse payloads, delta-v semantics, burn frames, maneuver planning, fuel, reparenting, sphere-of-influence changes, and celestial-to-reference-frame evaluation remain deferred to later work.
