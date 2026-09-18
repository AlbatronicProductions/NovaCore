# Stage 3 — support-loss ownership transfer draft

Historical stop record. The subsequent authorized [allocation closure and qualification](allocation-closure/README.md) records the current result without changing this original failure.

2026-09-17. **REVISE — UNQUALIFIED. STOP FOR PROJECT CONTROL.**

Stage 2 is fully qualified following its independent red team and Project Control's manual report. The authorized campaign evaluated Stage-3 promotion. Fresh current KSA source and authenticated history review, plus independent read-only architecture review, passed the bounded design gate in [plan.md](plan.md). Implementation began without changing the vehicle, engine, solver or banked public refs.

## What happened

The draft adds a narrow prescribed-separation profile, explicit current-consumer provenance under the same assembly owner, full-next-interval slab eligibility, and the existing assembly free-flight evaluator with the contact environment's constant gravity. A contact endpoint is committed once, then native continuation permission is revoked in the same owner phase. Its native world/body remains allocated and does not evolve further. Exact stores, mass/COM/inertia, material-origin state, debt and history stay with the same canonical assembly. The original zero-gravity free-flight admission and old save boundary remain separate.

The qualification fixture starts touching the slab with a prescribed upward relative velocity of 0.25 m/s (0.2 m/s for moving-frame coverage), retaining the stock 600 N engine. **It is not a demonstration of stock-powered liftoff from rest.** Original frame transport is explicitly separate from initial relative vehicle motion. No scene/asset/launcher or production thrust change was made.

The first contact interval is 16,666 ticks. Subsequent free-flight increments remain in the banked <=15,625 tick domain. Eligibility checks all authored corners over the next interval with conservative force, full-inertia angular and frame-relative motion bounds. Old speculative rows describe the completed native interval; their count alone neither grants nor denies next-interval geometric clearance. This fixed-slab adaptation is not M14.17 clearance evidence or a generic rotating-vehicle CCD claim.

## Executed gates and first stop

1. Initial Debug compilation: PASS, zero warnings/errors.
2. Initial cheap Debug proof: PASS.
3. Expanded-test compilation initially found two test-only API spelling errors (`SetRate`/`SetPaused`); corrected to existing `TrySetRate`/`Pause`. No runtime or physical result was retried to green.
4. Expanded Debug compilation: PASS, zero warnings/errors.
5. One expanded Debug validation process: cheap, failure matrix and schedules PASS; **first allocation gate FAIL**. Process terminated at that failure.

Exact decisive output:

```text
DEPARTURE_PREPARE status=Prepared
DEPARTURE_FIRST_FLIGHT tick=32291 heightO=0.002157609515294083 vy=-0.03929292264178322 oracleError=3.251288127614771E-13 nativeFrontier=1
DEPARTURE_CLEARANCE_HOLD frontier=3 tick=47916 debt=15625
DEPARTURE_FAILURES PASS authority/consumer/receipt/refusal/retry/terminal/capacity/save boundaries
DEPARTURE_SCHEDULES PASS 6 equal-time host partitions / same frontier bits / moving frame
ORDINARY_ALLOCATION gate=departure-contact-handoff bytes=624 entry=PASS exit=PASS
```

The unchanged requirement is **0 managed bytes**. The measurement uses the qualified checked no-GC helper. Sixteen prior prepared worlds exercised contact handoff and first free flight; the measured operation then used another already-constructed world and included its first contact-owned solve/export/publication/handoff and copied observation. Cold world construction itself is outside the counter. This exact lifetime boundary is disclosed; no attribution to runtime accounting, cold caches, BEPU or another owner has been established.

**No allocation retry, profiler, tracing, attribution campaign, threshold change or corrective optimization followed.** First-free-flight allocation and the shared positive control were not reached. Release, storage, performance, full regressions and any visible departure/manual gate did not run. Stage 4 remains closed.

## What the successful focused checks establish

- Same owner and retained native identity; native frontier stays 1 after handoff.
- First free-flight successor at tick 32,291; independent axial changing-mass/gravity velocity error `3.251288127614771E-13 m/s`.
- Ordinary refusal preserves the pending contact result; publication retry does not restep.
- Old native and contact consumer permissions are refused after handoff.
- Default/fabricated/old/duplicate flight receipts, wrong thread, reentrancy, pending credit, history capacity and stale canonical authority refuse without extra writes.
- Forced physical and host-credit acknowledgement failures preserve the actual committed successor/accounting and terminate continuation.
- Six equal-time partitions preserve frontier bits and conservation. Moving-frame/lower-speed first-successor comparison passes.
- Before return toward the slab, clearance expires at frontier 3/tick 47,916 with 15,625 ticks debt retained and no fourth physical step.

These are focused successes, not complete qualification. The independent position/corner-error oracle and angular-edge clearance checks requested by static review remain **pending**. The numerical reserve alone is not a proof of RK4 error for general motion. No claim is made that Stage-1/2/M15.2 regressions have passed against this new draft.

## Independent red team

Final read-only review: **REVISE** on the exact-zero failure. No additional blocking ownership/mass/frame/commit defect was found in the static review. Final mutable authority checks, sealed proposals, pure-flight abort, contact retry and terminal commit distinction remain explicit. Pending numerical-clearance witnesses and all stopped gates still prevent qualification.

## Exact source preservation

Stage-2 qualification applies to its retained seals, not the new Stage-3 tree. Five sealed Stage-2 source files overlap the draft. Their exact pre-Stage-3 bytes were reconstructed **in memory**, matched **5/5 retained SHA-256 seals**, and retained in [qualified-stage2-overlap.zip](qualified-stage2-overlap.zip); no working source was reverted. [source-preservation.json](source-preservation.json) records old/new identities and archive hash. The other 19 Stage-2 seals remain in place. The only additional preexisting source changed is banked `AssemblyDynamics.cs`; the two new Stage-3 files are identified separately.

This small preservation archive is required to reproduce the accepted unbanked Stage-2 source independently of the failed draft. It is **KEEP**, not disposable build output. To reproduce Stage 2, use a separate source copy, restore these five files from the archive, restore `AssemblyDynamics.cs` from the recorded public baseline, and omit the two Stage-3-only files. Verify all 24 Stage-2 seals before using its existing reproduction instructions. Do not restore over the retained draft automatically.

## Reproduction and disposition

No reproduction is requested during this stop. A future authorized reproduction from the current sealed draft is:

```powershell
dotnet build tests/NovaCore.Simulation.Tests -c Debug --artifacts-path E:\NovaCore\build\srv01-stage3\artifacts -p:ContinuousIntegrationBuild=true --nologo -v:q
dotnet E:\NovaCore\build\srv01-stage3\artifacts\bin\NovaCore.Simulation.Tests\debug\NovaCore.Simulation.Tests.dll --assembly-departure-validation
```

The current permanent focused route contains the tests; it has not been added to the ordinary full-suite registration because qualification stopped. [identity.json](identity.json) seals the draft and Git state; [validation.json](validation.json) preserves exact output and execution disposition. [cleanup.md](cleanup.md) inventories only rebuildable output and prints the manual cleanup and non-destructive verification commands.

No commit, staging, tag, push, milestone, banking, startup optimization, propulsion redesign or Florida work. **Stage 2 PASS; Stage 3 REVISE. UNBANKED. STOP FOR PROJECT CONTROL.**
