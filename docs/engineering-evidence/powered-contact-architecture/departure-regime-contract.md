# Departure: one authority, but a real private-consumer gap

## Physical fact versus numerical dispatch

Departure means support is no longer physically exerted/required and motion is separating from the previously supporting surface. Useful copied evidence includes resolved contact geometry, normal relative velocity, penetration/separation within the declared numerical enclosure, and normal support impulses. Manifold count alone includes speculative contacts; zero impulse can coexist with touching or gravity-driven return. No timer, altitude threshold, camera state or T>mg branch defines canonical departure.

Minimum new representation should be a value observation of support/contact status and its episode/frontier provenance. It must not own spacecraft state or certify a future interval merely by naming it airborne.

At the end of a last constrained interval, publish the one whole successor. The next consumer starts from that EXACT canonical endpoint with the same spacecraft, commands, throttle/latch/actual, resource, mass, revisions, time and debt. There is no transfer between authoritative spacecraft objects. There can still be an explicit PRIVATE consumer/lifetime transition.

## Can the same BEPU episode continue with zero constraints?

Mechanically yes: the pinned timestepper integrates unconstrained bodies, and LocalContactWorld.Step does not require positive contact count. A retained body can leave its slab without a special mode flag.

Numerically that is not M15 equivalence. Pinned PoseIntegrator.cs:634-645 advances velocity then p += v*h; for constant acceleration the displacement differs from the analytical/M15 RK result by a*h^2/2 before FP32 export. M15 additionally evaluates changing mass and orientation at coupled stages, preserves tiny exact events and restricts its physical domain.

Therefore natural loss of BEPU contact is an available mechanical fact; continuing unconstrained in BEPU cannot be relabelled banked M15 free flight. Conversely BeginPoweredFreeFlight rejects a departed applied endpoint and non-origin/asymmetric/gravity sources today. Calling Begin at departure is not the architecture.

## Private lifetime candidates

| Option | Assessment |
|---|---|
| Retain body/world and continue all free flight in BEPU | Cheap continuity/recontact, but FAILS demonstrated M15 numerical equivalence; reject as the accepted universal consumer |
| Retain prepared world/shape resources while M15-compatible evaluator handles qualified free intervals | Preferred direction. One canonical owner; dormant solver data explicitly stale until validated reactivation; no per-frame recreate. Needs coverage and reactivation contract |
| Remove body but keep world/shape resources | Possible lifecycle alternative when dormant representation cannot safely participate; stable canonical identity survives, private handle/warm starts may not. Requires explicit generation/recontact semantics |
| Dispose/recreate whenever contacts vanish | Reject as default: unnecessary cold churn, lost manifolds, no demonstrated need, poor future recontact |
| Same common external integrator with BEPU as constraints-only correction | Attractive convergence candidate, but split accuracy, position integration, exact event handling and no-contact equivalence are not established by pinned APIs. Not an implemented or qualified shortcut |

The preferred direction preserves reusable resources, not falsely reusable physical state. While M15 integrates free motion, the old BEPU pose/velocity/cache is not current authority. Never continue it later without an explicit validated synchronization/activation boundary; never reuse old contact impulses across an unobserved separation as if they still describe the same contact.

## Why departure is deferred from the first powered-support slice

1. No banked finite-body clearance/recontact interval certificate exists for the powered, asymmetric article. An empty current manifold cannot prove the next full interval stays clear.
2. The article's 1,000 kg asymmetric-inertia/gravity model is outside M15's dry-8 spherical/no-environment domain. The banked free result must remain a reference; extending its admission/equations requires qualification.
3. No existing reactivation protocol preserves all common source tokens while reconciling a dormant private contact body with free motion.
4. Exact tiny constrained event and changing-mass integration remain unresolved before either option A or B is implementation-ready.

These are ownership/numerical boundaries, not reluctance to include visible liftoff. Choose **A: powered supported contact only** as the next bounded production objective after the numerical prerequisite is settled. Use fixtures that remain supported; if support is lost, do not clamp/pull it back or pretend qualified M15 continuation. Report/hold the last successfully committed endpoint with explicit domain completion/refusal semantics.

## Future contact-free continuation contract

Owner selects M15-compatible free evaluation only for a qualified contact-free interval. Retain original command/resource episode/time identity; use a new explicit private activation generation if resources are reactivated. A recontact candidate must be found before stepping past its validity boundary; ordinary endpoint observation is insufficient. Conservative interval bounds/event localization and exact-source recheck belong to that later responsibility.

No render sample, host frame duration or transient KSA-style contact-count check may bypass this admission. Future landing need not recreate all geometry, but stale private constraint state must be retired explicitly.

No M14.17 clearance evidence is fabricated; that certified singleton path remains separate and currently does not certify this finite compound powered trajectory.
