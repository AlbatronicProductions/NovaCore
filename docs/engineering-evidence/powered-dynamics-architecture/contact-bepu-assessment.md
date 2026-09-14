# Retained contact: API feasibility and qualification boundary

Keep M14.18-M14.21 source authority unchanged. A future powered-contact adapter must be a specialized lifecycle under NovaCore ownership, not a relaxation of the existing frozen-mass/force checks.

Exact dependency source inspected: BEPU 2.5.0-beta.29, repository commit `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`, identified by the accepted manifest. No dependency bytes, version, pins, trust configuration or provenance rules changed.

## What the exact source proves

| Source | Finding | Implication |
|---|---|---|
| [Bodies.cs421-452](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Bodies.cs#L421), [BodyReference.cs226-249](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/BodyReference.cs#L226) | SetLocalInertia wakes sleepers, replaces local inertia, clears ephemeral world inertia; handles dynamic/kinematic transition. Direct dynamic-to-dynamic mutation is permitted but does not wake. No impulse rescale/clear is performed here. | In-place positive-mass update is mechanically supported; use a controlled phase and remain dynamic. API support is not stability proof. |
| [PoseIntegrator.cs70-94,319-341](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/PoseIntegrator.cs#L70) | Velocity callback receives orientation/inertia/dt/mask. It also runs for speculative bounding-box velocities which are discarded. Preparation can run repeatedly for different durations. | Callback evaluates immutable prepared wrench/mass data; NEVER debit fuel, advance hardware/cursor or publish in it. Callback count is not elapsed physical authority. |
| Retained `build/compound-coverage-investigation/DefaultTimestepper.cs:28-42` | Sleep, prediction, detection, solve, optimization | Exclusive validated admission before Timestep is a conservative safe boundary for a full private step. |
| [Solver_Solve.cs1415-1477](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver_Solve.cs#L1415) | Substep start precedes incremental contact update/warm start/solve; multithreaded event is on worker zero | Substep event existence is not permission for arbitrary awakening/reindexing. A finer mass schedule needs qualified coordination. |
| [Constraints/TypeProcessor.cs1298-1383](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/TypeProcessor.cs#L1298) | Local inertia is gathered and world inertia recomputed for solve | Changing callback acceleration alone while constraint inverse mass stays old is inconsistent. |
| [Constraints/Contact/PenetrationLimit.cs67-74,113-130](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/PenetrationLimit.cs#L67) | Warm start applies impulses using current inertia; effective mass is calculated for solve | Cached impulses are iterative guesses, not conserved canonical momentum. |
| [Solver.cs accumulated-impulse APIs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver.cs) | Explicit scaling APIs exist, including timestep-related guidance | No source proof of universally correct mass-ratio scaling; do not invent one. |
| Retained `NarrowPhaseConstraintUpdate.cs:82-128,147-207`; `ContactManifold.cs:14-36,115-119` | Old impulses match feature IDs; unmatched impulse may redistribute; new constraints initialize from zero. Geometry stores offsets, normals, depth/features. | Mass does not determine feature matching. Normal collision refresh remains required. |

**Inference, with scope:** unchanged shape, pose and body COM frame mean mass alone does not alter geometric contact validity. It does alter the constraint response/effective mass. No inference is made for moving COM, shape changes, material changes, dynamic/kinematic transitions or arbitrary timestep changes.

## Eventual powered-contact lifecycle

1. NovaCore validates the genuine command/actuator/resource proposals, exact target and source authority. Reserve the one pending endpoint and all future publication destinations.
2. Prepare a bounded private mass/wrench schedule; account for powered and unpowered portions explicitly. The episode's original T0, frame epoch, shape/body identity and generation remain stable.
3. Under exclusive private-step ownership, install qualified local inverse mass at an admitted boundary and stage immutable external-wrench inputs. CentralPointReservoir changes inverse mass only; dry inertia/COM stay fixed. Do not uniformly scale the inertia with total mass.
4. BEPU applies engine force/torque using the callback stage orientation and actual step duration alongside gravity and contact impulses. No second free-flight endpoint is superimposed after the contact solve. All force owners contribute once.
5. Export a staged FP64 endpoint. Prepare joint mass/resource/actuator/motion/time/history publication and the matching new expected-authority acknowledgement.
6. Commit canonically once, then acknowledge the retained body's already-existing state. Never reimport rounded canonical values each interval, recreate the world or clear manifolds just for convenience.

This is a lifecycle design, not an executable schedule. In particular, holding one endpoint mass for a complete canonical interval is NOT continuous changing-mass integration. The choice of substep mass, time-varying external velocity integration, contact effective-mass sampling and scaled tiny-duration execution must be qualified together. BEPU's FP32 dt/mass range is an additional numerical admission problem; do not round an interior event away or claim the free-flight scaled adapter automatically solves constrained contact.

## What remains unqualified

- Accuracy and solver stability for continuous mass variation, supported ignition, exhaustion and abrupt force changes.
- Whether retained impulses, targeted reset or a derived rescale best satisfies the admitted mass/dt envelope. No default policy change is recommended without that test.
- Sleeping/awakening behavior; mass/inertia updates at solver substep boundaries without invalidating gathered data or dispatcher ownership.
- Arbitrarily short resource-owned rational segment handling in the FP32 constrained solver.
- Contact departure, reacquisition and authority transfer back to free flight.

The first physical implementation should therefore use the free-flight adapter plus joint canonical publication. Share immutable actuation/resource/mass-law and publication responsibilities with eventual contact; do not share one unconstrained integrator as the solver for both regimes. Powered contact later has its own sealed receipt, source expectation update and terminal-failure contract. M14.17 clearance is never synthesized, and the banked unpowered M14.19/M14.20 path remains valid unchanged.
