# Celestial simulation foundation

Milestone 7A introduces internal managed contracts for authoritative celestial data. It does not propagate trajectories, evaluate reference-frame transforms from body state, schedule celestial events, publish `SimulationSnapshot`, or change rendering.

`CelestialBodyId`, `ReferenceFrameId`, `SimulationEventId`, and `RenderObjectId` remain separate value identities. A `CelestialBodyDefinition` contains only a stable celestial ID, an optional primary-body ID, its inertial frame, and positive finite gravitational parameter (μ). μ is stored directly because analytical two-body propagation consumes μ; mass and a gravitational-constant/unit framework are intentionally deferred.

The canonical future propagation input is `CartesianState`: finite double-precision position and velocity at an exact `SimulationInstant` epoch. `TwoBodyTrajectory` pairs that state with a central-body identity and a narrow model version. It contains no orbital elements, evaluated cache, anomaly, solver state, render data, or reference-frame transform.

Root bodies have no primary and explicitly have no trajectory. Non-root bodies have exactly one trajectory whose central body matches their declared primary. This avoids a fake self-orbit or a hidden invalid central-body ID.

`SimulationState` owns the fixed-size internal `CelestialStateStore`. The store copies fully validated definitions and body states, retains caller declaration order as the canonical traversal order, and constructs a setup-time sorted ID index for allocation-free lookup. Its `CelestialStateView` exposes immutable indexed and ID-based reads only. The transaction engine remains the sole future authority for changing celestial records; 7A adds no celestial mutation API.

All position, velocity, and μ values must use one internally consistent simulation unit system. NovaCore does not introduce a unit framework in this slice.

The next slices will add a pure analytical evaluator, then transaction-based trajectory replacement, then celestial-to-frame evaluation. Graphics and native code remain unaware of celestial-domain types throughout.
