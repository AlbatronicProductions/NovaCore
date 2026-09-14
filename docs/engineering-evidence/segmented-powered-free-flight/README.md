# Segmented finite-fuel propulsion: implementation and qualification

Current disposition (2026-09-14): **PASS — READY FOR PROJECT CONTROL ACCEPTANCE. UNBANKED.** No milestone assigned. The [final qualification summary](final-qualification.md) supersedes the original stopped disposition; the original failure and unresolved cause remain retained below.

Baseline HEAD/main/origin/main/remote main: `89a432ce3b96a5dcb0751cfdc26bfd6776c883a1` (banked M14.24). Candidate branch: `codex/segmented-powered-free-flight`.

Subsequent bounded causal ticket: the single authorized full Release reproduction reported clock **0 bytes** and **67/67 PASS** with matched candidate binaries. Its Phase 1 non-reproduction stop rule was applied; no baseline/standalone controls or correction followed. Cause remains unresolved. See the [clock attribution follow-up](clock-allocation-followup.md).

The separately authorized [eight-process stability campaign](clock-allocation-stability.md) then completed **8/8 full Release PASS**, each with clock **0 bytes**, unchanged inputs/runtime configuration and no correction. Project Control accepted those nine later passes as sufficient to resume remaining qualification. The user explicitly granted [manual PASS](manual-acceptance.md), and one independent [final verifier](final-verifier.md) passed all 15 specified responsibilities. No source/test correction or new automated campaign occurred during closeout.

## Original stopped qualification record

The following original qualification narrative retains its historical stop and pending status. Current acceptance is recorded above and in the final summary; it does not erase the original allocation witness.

## What happened

The bounded free-flight candidate now evaluates coupled position, velocity, orientation and angular velocity with changing point-reservoir mass; consumes genuine prepared engine/resource segments; and publishes one sealed physical/mass/resource/actual-actuator/time/history successor under the existing transaction owner. A minimal Release development scene presents copied canonical endpoints. There is no contact participation.

Qualification stopped on the **first full Release Simulation failure**, after eight of 67 groups passed. The existing ninth group, `Simulation clock`, failed its checked allocation gate: expected **0 bytes**, actual **12,336 bytes**. Fifty-eight groups were not reached. No retry, attribution, profiler, helper change or threshold change followed. The failed operand is allocation; object/type/call-stack/accounting cause is **UNRESOLVED**. An unchanged gate does not prove a pre-existing failure, and a candidate full-suite failure does not prove a propulsion allocation.

The complete Debug Simulation suite passed 67/67. Focused propulsion physics, authority, exact allocation and host-partition tests passed in Debug and Release. Independent numerical, atomic/lifecycle and presentation/scope reviews passed their inspected responsibilities. These results do not override the red Release suite. Manual visual acceptance remains pending at the stop.

## Why it matters

Prepared thrust now changes physical motion while exact resource authority remains M14.24's responsibility. Resource consumption cannot become externally visible separately from its physical successor. Endpoints are explicitly endpoint-only; the legacy constant-force evaluators cannot extrapolate them. This is an isolated first mathematical model, not powered contact, a real tank model or a production spacecraft article.

## Evidence index

- [Final qualification](final-qualification.md) and [machine-readable result](final-qualification.json): current manual/verifier PASS, anomaly disclosure and unchanged candidate identity.
- [Final independent verifier](final-verifier.md): 15 responsibility checks and strongest failure-path attack.
- [Allocation follow-up](clock-allocation-followup.md) and [stability campaign](clock-allocation-stability.md): nine later zero-byte passes, no correction, unresolved original cause.
- [Physics contract](physics-contract.md): domain, equations, segment semantics, actual state.
- [Numerical integration](numerical-integration.md): predeclared ceilings and independent error review.
- [Atomic publication](atomic-publication.md): ownership, leases, failures and history.
- [Validation](validation.json): measured values, completed gates and explicit unreached work.
- [Verification](verification.md): independent attacks and the exact full-suite stop.
- [Identity](identity.json): candidate fingerprints, unchanged architecture package, refs and protected paths.
- [Reproduction](reproduce.ps1): explicit, bounded modes; no automatic retries.
- [Manual acceptance](manual-acceptance.md) and [launch route](launch.ps1): READY/Space, acceleration, depletion, coast and final hold.

## Performance and storage already measured

Three fresh Release processes, each 128 warm and 1,024 measured complete operations, produced median 0.0423/0.0421/0.0430 ms; P95 0.1840/0.1820/0.1866; P99 0.1905/0.1909/0.1939; maximum 0.2212/0.2213/0.2220. All three worst indices were 171 (zero based), overall interval 300 after the 128 warm intervals. This aligns with the fixture's depletion boundary; no timing-mechanism attribution is claimed. The workload includes powered and post-depletion coast intervals, so its median is not a powered-only cost. The maximum is about 3.33% of the 6.67 ms whole-frame objective; this is not a whole-frame certification.

The coherent 1,200-record history uses 4,838,424 bytes including its array header. Complete cold managed construction allocated 4,870,592 to 4,878,600 bytes, a conservative retained upper bound including setup transients. No unmanaged physics world exists. Cold preparation measured 0.3484 to 0.5151 ms after a separate initialization fixture. Presentation cold preparation and frame sampling are separate.

## Retention and next action

Retain this package and the accepted sibling [powered-dynamics architecture package](../powered-dynamics-architecture/README.md), unchanged at 11 files / 103,541 bytes. Any future banking closure must include both. This new package has a 192 KiB budget: contracts, identities, results and reproduction only. No bulk runtime dumps, temporary build trees or profiler copies were created. Standard ignored bin/obj outputs remain rebuildable; unrelated historical scratch is untouched. No cleanup of unresolved evidence occurred.

Qualification has resumed and completed under Project Control's later instruction. Two temporary manual-run logs (24,066 bytes total) remain after automatic approval review blocked deletion; their witnesses, identities and a narrow manual cleanup command are retained in the final summary. No cleanup retry or workaround was attempted. Do not rerun allocation campaigns, start attribution, assign a milestone, bank, begin powered contact or begin Florida launch. Stop for Project Control.
