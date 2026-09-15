# Powered retained contact architecture investigation

**Judgment: REVISE. Investigation only. No production implementation or milestone assignment.**

Baseline HEAD/main/origin/main: 49057fecceb0f725d5f551ec40e2780971b0d81d. Existing branch: codex/segmented-powered-free-flight. M15.0 remains banked at 4607d8c802006d5e1a01c595ab608cf53a4dab6b, annotated tag m15.0-segmented-powered-free-flight.

## WHAT HAPPENED

Current source supports **one common prepared physical interval and one canonical successor owner**, with distinct private constrained/unconstrained consumers. A third spacecraft physics owner is unnecessary.

The audit also found decisive numerical limits. Current contact freezes mass/acceleration and rejects body torque; M15.0 only qualifies dry-8 kg, spherical inertia and no environment. The pinned BEPU API can update inertia in place, but ordinary float stepping cannot execute all exact resource durations; continuing unconstrained in BEPU is not the accepted M15 RK4 outcome. The independent verifier returned REVISE on production readiness.

No production/test source, binaries, runtime state or historical evidence was modified. No simulation, performance, allocation or renderer campaign was run.

## CURRENT CONTACT LIFECYCLE

Copied canonical source -> checked local FP32 import -> retained dynamic compound/slab -> frozen external acceleration -> child contacts/four-row selection -> BEPU constraints -> sealed export -> canonical paired/clock/debt/history publication -> fixed expected-state acknowledgement. Body/world/frame epochs persist; solver/export failures poison continuation. See [complete current map](current-contact-lifecycle.md).

Geometry validity and cached impulse validity are separate. The existing selector already reads inverse mass/inertia and predicted motion; its cold acceleration input cannot be reused unchanged for powered dynamics.

## CURRENT POWERED LIFECYCLE

Canonical command boundary -> genuine engine proposal -> genuine exact resource segmentation -> scaled changing-mass, evolving-orientation RK -> actual actuator successor -> joint physical/mass/resource/actual/time/history commit -> lease retirement -> private acknowledgement -> copied observation. See [M15 source map](current-powered-lifecycle.md).

Engine sealing already advances the PRIVATE preparation cursor. Recoverable downstream refusal must keep the active parent lease instead of repreparing it. Canonical fuel and actual hardware advance only on canonical success.

## CONVERGENCE MAP

Already common: canonical spacecraft/state/command/resource authority, owner phase, exact time and copied presentation. Must converge: prepared input, expected-source/frontier state, mass/environment law, actual actuator progression and coherent publication/history. Keep separate: contact evidence/geometry/constraints, smooth free integration and M14.17 certified clearance. See [responsibility table and prepared input](convergence-map.md).

## BEPU MASS / WRENCH

Pinned commit f73164bb3c9ca733eb3329f1f6b1cea4e216ece7 supports SetLocalInertia without rebuilding positive-mass dynamic bodies. It preserves handle/pose/velocity/shape, wakes sleepers, invalidates ephemeral world inertia and does not clear cached impulses. Apply under a qualified owner lifecycle.

Pose callbacks execute for speculative/discarded prediction as well as actual integration. They may evaluate immutable prepared external force/torque only. They must never spend fuel, progress actual state or retire proposals. Do not apply thrust once as impulse and again as callback input. See [exact source/API assessment](bepu-mass-wrench-assessment.md).

## WARM STARTS

Select **A — PRESERVE UNCHANGED for mass alone as an iterative guess**, using the solver's CURRENT inverse mass/effective response. No universal mass-ratio scaling exists with fixed dry inertia and varying lever arms. No arbitrary reset threshold is justified.

This is not finite-iteration stability proof. New exact resource-piece durations and mass cadence must be qualified together; cached geometry must not be destroyed as a substitute. See [warm-start contract](warm-start-contract.md).

## POWERED CONTACT PHYSICS

Use one net effective body wrench, explicit gravity acceleration, CentralPointReservoirV1 changing mass/fixed COM/dry inertia, and ordered exact powered/coast pieces. Supported centered reaction is N(t)=m(t)g-T while feasible; off-COM thrust changes contact torque/load distribution. T>mg is only an analytic witness, never a mode switch.

Two concrete counterexamples prevent declaring the adapter solved:

1. M15 preserves a positive 2^-1075-second event with a representable one-double.Epsilon velocity effect. Plain BEPU float dt/reciprocal cannot carry it.
2. Logarithmic-mean mass reproduces a restricted constant-force velocity integral but not the general changing-mass contact problem. Exact supported normal impulse uses the arithmetic mean mass in a centered fixed-support example.

See [derivations, exact event obstruction and required proof](powered-contact-physics.md).

## DEPARTURE

A retained BEPU body can mechanically continue with zero constraints. That is not M15 numerical equivalence or a next-interval clearance certificate. Canonical state need not transfer owners; the private consumer still needs a qualified selection/reactivation boundary.

Preferred future lifetime keeps reusable contact resources while admitted M15-compatible free work advances canonical state. Dormant body pose/caches must be explicitly treated as stale until safe reactivation. No perpetual warm-start continuity is claimed across removal or unobserved separation. See [departure and recontact assessment](departure-regime-contract.md).

## ATOMIC PUBLICATION

One whole canonical interval installs paired endpoint-only state, mass/inertia, exact resource, actual actuator progression, StateRevision, clock/debt and one deterministic history record. ResourceRevision changes only on consumption; ActuatorRevision follows actual frontier; TimelineRevision is not invented.

No BEPU motion commit followed by a separate fuel commit. Final canonical success retires every lease; failed private ack leaves all canonical values committed and invalidates private continuation. Backlog N+1 uses N's successor even within the same host callback; render once after bounded service. See [transaction/failure model](atomic-publication.md).

## KSA CONVERGENCE

ADOPT common vehicle update input and game-owned solved-state application. ADAPT ready/apply and retained resource reuse. INTENTIONALLY DIFFER on exact event/resource/mass cadence, canonical revisions/debt/history and acknowledgement.

Current KSA selects constrained versus unconstrained work on common vehicle update state; it also consumes resource after an outer step rather than supplying NovaCore's exact stage-mass contract. Its official history supports ownership/lifecycle intent, not numerical equivalence. See [current source versus official history](ksa-convergence.md).

## ARCHITECTURE CANDIDATES

- **A, preferred direction:** common prepared interval + specialized private consumers + one joint successor owner.
- **B, rejected:** universal BEPU integration replacing M15; accepted free-flight outcome is not preserved.
- **C, rejected:** independent powered-contact owner beside contact/free flight; duplicates resource/actual/frontier/publication responsibility.

Full responsibility/cost comparison: [candidates](architecture-candidates.md).

## PREFERRED ARCHITECTURE

Canonical source -> sealed common engine/resource/mass/environment interval -> exactly one eligible consumer -> one coherent canonical successor -> prepared same-phase private acknowledgement.

This removes duplicated ownership; it does not remove the need to prove a correct constrained numerical operator. The preference is conditional and must not be presented as production-ready PASS.

## FIRST IMPLEMENTATION SLICE

**A — powered supported contact only**, after the specific numerical prerequisite is settled. Departure is deferred because source proves a distinct free-model/coverage/private-reactivation gap.

Recommended prerequisite: one bounded event-aware changing-mass constraint operator proof, including exact tiny events, consistent force/constraint mass cadence, selector predictions, finite-iteration warm-start stability and no-constraint reference comparison. No new generic ownership framework is needed.

The subsequent production objective would qualify canonical ignition/throttle/shutdown, finite fuel, support unloading and full off-COM moment on one explicitly authored fixed-COM body/slab with the existing owner/debt/lattice. No geometry retuning or silent article mass reinterpretation. [Ticket boundary and qualification bar](architecture-candidates.md).

**Do not issue or execute the full powered-contact implementation from this REVISE report.** Project Control must scope the numerical proof first.

## GAME PROGRESSION PAYOFF

The recommended supported slice would prove actual finite-fuel thrust while physically supported, with load redistribution and coherent copied actual/resource/motion state. It would not yet claim lift-off into the M15 consumer. No new gameplay capability was implemented by this investigation.

## FLORIDA DEPENDENCY

After supported-only qualification: compatible article/environment free dynamics, departure/clear-interval/recontact lifecycle, actual Florida pad/initial pose/local frame/collision binding, and player command ingress remain. Gimbal/RCS/aero/final art/VFX are fidelity/control extensions only where promised, not invented prerequisites for a minimal fixed-engine launch. See [classified gap](florida-gap.md).

## RED TEAM

The strongest attacks are exact tiny contact time, wrong constraint mass, stale selector prediction, finite-iteration warm-start residuals, and a false BEPU-to-M15 equivalence at departure. They expose unresolved engineering work; the evidence retains those blockers instead of hiding them behind a common interface. [Independent 28-point review](verification.md).

## SOURCE / EVIDENCE

[Identity](identity.json) records baseline/tag/source hashes, pinned dependency and current KSA/history provenance, and final preservation. [Reproduction](reproduction.md) contains bounded read-only navigation/derivations.

Final raw-SHA preservation: **1,991/1,991 tracked files unchanged; 65/65 tag refs unchanged.** HEAD/main/origin/main and remote main all remain the baseline above. Index and tracked diff are empty. `git diff --check` passes. The only short-status entry is `?? docs/engineering-evidence/powered-contact-architecture/`. The package contains 15 concise evidence files; local evidence links and whitespace checks pass.

Budget: **160 KiB** for this package. Retain concise contracts, source anchors, derivations, identities and review only. No new bulk source/decompile/runtime dumps or temporary build tree. Existing historical scratch is untouched; no cleanup was needed or attempted. Blender untouched. Nothing staged, committed, tagged, merged, pushed or banked.

## JUDGMENT

**REVISE.** Ownership convergence is supported; the required constrained numerical bridge and departure equivalence are not qualified.

## WHAT HAPPENS NEXT

**STOP FOR PROJECT CONTROL.** Return the specific bounded numerical prerequisite and conditional supported-contact implementation recommendation. Do not implement automatically, assign M15.1 or start Florida integration.
