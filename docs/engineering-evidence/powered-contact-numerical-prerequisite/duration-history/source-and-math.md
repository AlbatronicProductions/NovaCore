> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Cache quantity, duration and ownership

This is a causal study and correction recommendation, not adoption. Pinned BEPU 2.5.0-beta.29 source commit: `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`. Binary/manifest and inspected-source hashes are in [identity.json](identity.json).

## Exact source path

Paths below are within [the pinned upstream tree](https://github.com/bepu/bepuphysics2/tree/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics). The inspected execution path, rather than a generic sequential-impulse assumption, establishes the following.

| Responsibility | Source | Finding |
|---|---|---|
| Stored contact values | Constraints/Contact/ContactConvexTypes.cs, Contact4AccumulatedImpulses | Four normals, tangent pair, twist. No hidden force or timestep normalization. |
| Normal warm application | Constraints/Contact/PenetrationLimitOneBody.cs, WarmStart/ApplyImpulse | Cached normal directly produces inverse-mass linear and inverse-inertia angular velocity changes. |
| Tangent warm application | Constraints/Contact/TangentFrictionOneBody.cs, WarmStart | Direct application. Its comment recognizes timestep-related scaling, but the executable warm-start code does not perform it. |
| Twist warm application | Constraints/Contact/TwistFrictionOneBody.cs, WarmStart | Direct angular impulse application. |
| Current duration coefficients | Constraints/SpringSettings.cs; contact Prestep/Solve | Current substep h and inverse h prepare bias, effective-mass scale and softness. No old-h coefficient is reused by the local piece kernel. |
| Optional explicit scaling | Solver.cs, ScaleActiveAccumulatedImpulses/ScaleAccumulatedImpulses; Constraints/TypeProcessor.cs, ScaleAccumulatedImpulses | Caller-requested scaling multiplies all impulse DOFs. These methods are not automatically invoked by the inspected default timestep path. |
| Solve cadence | Solver_Solve.cs, DefaultTimestepper.cs | Current h is split into solver substeps. Warm start precedes iterations. No prior-duration ratio is supplied automatically. Cached result corresponds to the last solved substep, not necessarily the outer interval. |
| Feature transport | CollisionDetection/NarrowPhaseConstraintUpdate.cs, PairCache.cs | Feature matching transfers normal impulses to surviving contacts. Constraint cache holds handle/features, not the duration that produced the impulse. |
| Type storage | Constraints/TypeBatch.cs | Impulse/prestep buffers and indices have no old-duration provenance field. |

NovaCore's current `LocalContactWorld.Step` derives float dt from the exact integer interval and calls `simulation.Timestep(dt)`. Its retained fields do not include previous solved duration; no NovaCore invocation of the explicit BEPU impulse-scaling APIs was found. NovaCore is not bypassing an automatic ratio operation on this path: that automatic operation is absent. This does not establish a defect in ordinary banked stepping or justify changing its policy.

## Units and equation

With generalized inverse mass W and contact Jacobian J, cached lambda is applied as `delta_velocity = W J^T lambda`. Normal and tangent entries have units N s; twist has units N m s. Subsequent accumulated-impulse updates apply differences, so warm application plus updates telescopes to the final accumulated impulse.

Raw old lambda remains dimensionally valid as an initial guess. It is not a forbidden BEPU input or universally incorrect equation. The proven problem here is inadequate convergence in eight sweeps after a large duration change in retained steady support.

For constant reaction force/torque rates f, `lambda(h)=h*f`; old impulse is therefore not invariant. The corresponding predictor is

`rho = h_new / h_cache`

`lambda_guess = rho * lambda_cached`.

All normal/tangent/twist components share the duration factor, with their respective units. This preserves normal differences divided by h, tangent force, and twist torque. It intentionally does not preserve raw impulse differences when h changes. The friction basis/feature mapping must already be valid; arbitrary contact or frame changes are not qualified here.

## Spring dependence and conditional exactness

For the damping-ratio-one fixture, write x=omega*h, k=x*(x+2), c=k/(1+k), and alpha=1/k. In the unclamped normal equations:

`[K + diag(alpha*Kii)] lambda = bias(h) - J*v_free`, with `K=J W J^T`.

`bias_i = min(depth_i/h, depth_i*omega/(omega*h+2), maximumRecovery)`.

The unchanged PieceKernel recomputes these terms from each current h. Its independent pivoted reference computes the equivalent current-h equations separately. Stale spring/effective coefficients are ruled out for this failure. The earlier tiny-dt singularity of another mechanism is not reproduced or claimed solved by this study.

For steady support, `v_source=0`, `K*f=-J*a`, and `depth_i=Kii*f_i/omega^2`. Inactive recovery caps give `bias_i=alpha*Kii*h*f_i`, so lambda=h*f satisfies the changed soft equations. Tangent and twist steady velocity constraints and their impulse caps scale homogeneously.

This is conditional. A stationary, compressed, **unloaded** single row has

`lambda(h)=omega^2*h*depth / [K*(1+omega*h)^2]`.

Halving h in the retained scalar witness changes its exact impulse by factor **1.2976854437948657**, not 0.5. A ratio is a steady-reaction predictor, not an exact universal transformation for impact, unloading, separation or changing friction state.

## Independent analytical physical witness

The runtime fixture manufactures a seven-component steady solution using its Patch construction. A separate explicit box calculation in [reproduce.ps1 — retired record](../../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md) checks those inputs without calling Patch, J or K. For m=8, inverse inertia=.5 I, offsets x=+-1, z=+-.5, y=-.5, normal +Y and tangents -Z,+X:

`Kii = 1/8 + .5*(1+.25) = .75`.

With Wload=78.48 N, P=Wload/16, Q=Wload/32, S=Wload*sqrt(1.25)/16, the reaction is force `(Q,Wload,-P)` and torque `(.5P,S,.5Q)`. Balancing acceleration is exactly

`linear=(-.3065625,-9.81,.613125)`;

`angular=(-1.22625,-2.7419783574091174,-.613125)`.

All recorded inputs match those formulas with zero discrepancy. Both friction demands are strictly within their caps. The ideal normal depth is .0004141503360420091 m; FP32 stores .0004141503304708749 m. Its rounding difference predicts the reference impulse offset through `delta_lambda=kp(h)*delta_depth/[4/m+alpha*Kii]`. Across six durations this matches observed reference discrepancies within 1.995e-15 N s. The predeclared 1e-8 analytical bar covers N s for normal/tangent, N m s for twist, and the respective velocity units.

Full ratio initialization preserves the analytical seven rates exactly. Normal-only scaling loses that initialization and fails the velocity/angular bars at doubled h. Twist decouples in this symmetric fixture and is repaired by one twist update; these results do **not** independently establish twist-specific eight-sweep benefit. Its scaling recommendation follows direct angular impulse units and the nonzero steady torque witness.

## Composition with the accepted common-load predictor

Let u affect normals only and beta(h_new) be the existing current-piece common-load correction, evaluated at current mass/inertia. The composed predictor is

`rho*lambda_old + beta(h_new)*u`.

Scale old-duration history first; then add the new-duration load change. Applying beta before scaling instead produces `rho*lambda_old + rho*beta*u`, incorrectly scaling an adjustment already derived for the new piece. The measured difference is `(rho-1)*beta=.02567040320110876 N s` per normal; the reversed-order control fails all bars. No additional old-mass predictor, tuned coefficient, second transform or full solve was introduced.

## Required future provenance, not an implemented lifecycle

The actual preparation duration is `(double)(1f/60)=.01666666753590107`, not `1d/60`. Its provenance is the 120 explicit preparation calls. The current powered diagnostic has no accepted duration-history storage. BEPU's cache itself cannot supply that missing value.

A future correction must associate the cache-producing **solver substep duration** with its accepted private cache, feature/body/world generation and piece frontier. Current fixed ordinary stepping can reconstruct that duration from its original schedule, but mixed exact powered/coast pieces cannot safely infer it from host time or assume 1/60. Preserve banked exact/scaled duration arithmetic; the ordinary binary64 ratio used here does not qualify tiny or extreme ratios.

Prepare the new metadata before mutation; promote it with successful private endpoint/cache acceptance. Never promote it from collision callbacks, speculative reads, host-credit admission or canonical publication acknowledgement. Pre-step refusals preserve prior cache and duration. A canonical publication refusal AFTER an accepted private solve preserves the NEW pending cache and its NEW solved duration; it must not roll them back to the preceding piece. A post-step/export failure invalidates continuation, so a partly updated cache must not be reused with stale metadata. Reset/disposal/generation changes invalidate the pair. Sleeping/reactivation and new feature domains remain future qualification obligations.

`LocalContactWorld.Step` already has a private owner/poisoning boundary: pre-admission checks, invalidation before solve, accepted export/frontier, then restored continuation permission. No canonical clock/revision/resource/actuator change is required merely to retain private numerical provenance. This is an architectural recommendation, not implemented or tested duration lifecycle behavior.

Static extra arithmetic for this seven-entry witness: one prepared duration ratio and seven multiplications; existing common-load projection unchanged. No allocation, storage or performance acceptance claim is made.
