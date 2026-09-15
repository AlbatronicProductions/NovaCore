# Pinned BEPU mass, wrench and lifecycle assessment

Exact consumed version: 2.5.0-beta.29. Both accepted package manifests identify source commit f73164bb3c9ca733eb3329f1f6b1cea4e216ece7. The read-only investigator inspected these pinned official files in memory and verified retained source where available; hashes are in identity.json. No moving upstream branch was substituted and no source dump was added.

## Body mass/inertia

[Body API](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Bodies.cs#L421), Bodies.cs:421-452; [BodyReference](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/BodyReference.cs#L157), :157-164,226-249:

- SetLocalInertia modifies the existing local inverse mass/tensor, wakes sleepers and clears ephemeral world inertia.
- Positive-mass dynamic-to-dynamic change preserves handle, pose, linear/angular velocity, shape and retained constraints.
- Dynamic/kinematic transition has separate mobility/constraint effects and is outside the first powered scope.
- Direct local mutation is supported for dynamic-to-dynamic changes but does not awaken. It is not a reason to cache unstable backing-array references.
- Neither path automatically clears or rescales contact impulses.
- Unchanged geometry/pose does not acquire a new shape or broadphase extent just from mass. Awakening can relocate active storage; retain handles, reacquire references.

Use a controlled pre-Timestep owner boundary for supported changes. This is a conservative lifecycle inference from the call graph. An API existing does not prove safe arbitrary writes from substep events while solver gathering/dispatch is active.

[TypeProcessor](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/TypeProcessor.cs#L1298), :1298-1342, gathers local inertia and calculates world response. Updating callback acceleration but leaving constraint inverse mass stale is physically inconsistent.

## Engine wrench is evaluated input, not a resource callback

[PoseIntegrator callback contract](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/PoseIntegrator.cs#L70), :70-94,319-341, proves repeated PrepareForIntegration calls and IntegrateVelocity calls for speculative bounding-box prediction whose velocities are discarded. Inactive SIMD-lane results can also be discarded.

| Mechanism | Meaning | Allowed use |
|---|---|---|
| Force/torque | Physical rate of momentum change | Owner prepares immutable current-slice external law |
| Impulse | Integrated momentum change | ApplyImpulse immediately changes velocities, including lever-arm angular effect; not a second rocket burn |
| Direct velocity mutation | Private state write | Only a deliberately qualified numerical boundary; no canonical authority implied |
| Pose-integrator callback | Evaluate prepared input into predicted/solved velocity | Respect orientation, inertia, dt and mask; no fuel/actual/lease/canonical mutation |

The callback sees local-world orientation/angular velocity and local inertia. Body force rotates into local world using current pose; torque uses consistent inverse world inertia. Keep BEPU gyro treatment consistent—do not add the same gyroscopic correction twice. Solver constraints then operate on the propulsion-modified predicted velocities.

BEPU does not own a persistent engine-force accumulator that automatically clears. The adapter must publish/replace its immutable slice inputs at an owner boundary and supply zero engine wrench for coast. Callback count is not resource time. Never preapply an engine impulse AND apply that same wrench through the callback.

Current NovaCore LocalContactIntegrator only stores constant acceleration; it does not implement this adapter.

## Geometry and solver caches

Pinned [DefaultTimestepper](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/DefaultTimestepper.cs#L28) orders sleep, prediction, detection, solve and optimization. Retained NarrowPhaseConstraintUpdate.cs:82-128,165-207 matches normal impulses by feature ID, redistributes unmatched normal impulses, updates compatible constraints and creates/replaces types as needed. ContactManifold.cs:14-59,115-120 stores contact offsets/normals/depth/features separately from solver accumulation.

Mass alone with fixed COM/body frame/shape does not destroy geometric validity. Geometry still refreshes as poses evolve. Do not clear manifolds because cached impulses need a numerical policy. See [warm-start contract](warm-start-contract.md).

## Exact-duration and unconstrained limits

[Solver_Solve](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver_Solve.cs#L1415), :1415-1421, forms float substep dt and reciprocal. [SpringSettings](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/SpringSettings.cs#L50), :50-54, also has duration-sensitive reciprocal terms. Arbitrary resource-owned exact offsets cannot be passed through plain float Timestep without a separate numeric bridge.

[PoseIntegrator](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/PoseIntegrator.cs#L634), :634-645, advances velocity then position. For constant acceleration the position differs from the analytical/M15 outcome by a*h^2/2 for one step before FP32 effects. Keeping BEPU unconstrained is mechanically feasible, not free-flight equivalence.

No mass/wrench API finding qualifies arbitrary asymmetric powered spacecraft, finite-iteration stability, tiny constrained events or a safe M15 consumer switch.
