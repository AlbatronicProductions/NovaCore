# Independent verification and investigation closure

## Verdict

**PASS — architecture and bounded first-ticket recommendation.** This approves the proposed responsibility/contract, not implemented resource arithmetic, changing-mass physics, BEPU application or a milestone.

Independent read-only investigators checked exact-time capacity/arithmetic, current publication/resource authority, and narrow KSA source/history. A separate red-team reviewer attacked all 18 requested failure modes against the detailed resource and exhaustion contracts. A final closure review read the completed candidate/first-ticket document and explicit positive dry-mass/inertia admission requirement: **PASS, no remaining blocker**.

The reviewer requested concrete closure of source mass consistency, active engine-proposal lifetime, owner-phase reentrancy, exact integer capacity, zero-case precedence and tiny-duration handling. Those requirements are in the final contracts. No reviewer modified source or ran qualification tests.

## Eighteen required attacks

| # | Attack | Contract that survives |
|---:|---|---|
| 1 | Exhaustion rounded to ticks | Exact U/V internal offset; public integer target and original schedule unchanged |
| 2 | Negative fuel | Nonnegative exact units; validate before subtraction, no clamp |
| 3 | Overdraw | Compare U against Vn; partial consumption is exactly U |
| 4 | Averaged partial thrust | Original body wrench then zero engine wrench, with exact separate durations |
| 5 | Shared-feed double spend | One resource owner and one outstanding first-slice proposal; later aggregate admission |
| 6 | Starvation rewrites commands | Enabled intent survives; NoFeed is distinct realized availability |
| 7 | Resource commits without motion | Future joint prepared canonical transaction; no standalone live burn writer |
| 8 | Motion commits without resource | Same bundle includes actual actuator, resource, mass, physical, clock/debt and history |
| 9 | Nondeterministic timing | Bit-decoded fixed amount and source-bound exact ratio; no host/display-time input |
| 10 | Event replay | Genuine dependent lease, source revisions/frontier and once-only joint publication |
| 11 | FP64 equality error | Exact integer availability; observed false floating equality has positive exact residual |
| 12 | Exact-full-interval edge | U=Vn>0 means fully powered and empty at target; zero cases handled first |
| 13 | Tiny positive segment | Exact positive offset retained even when FP64 seconds is zero; nonzero impulse witness |
| 14 | Stale proposal | Active M14.23 seal plus full source checks; retirement/debt/source changes invalidate dependency |
| 15 | Frame/render dependence | Only accepted canonical interval and resource/engine facts enter segmentation |
| 16 | Contact coupling | Resource output feeds either future free-flight or contact consumer |
| 17 | Overbuilt tanks | One scalar resource/feed and explicit ideal point-reservoir law; no plumbing/slosh |
| 18 | Future tanks/engines blocked | Versioned mass/numeric/feed models and owner aggregation; future bounds must be qualified |

**Strongest numeric objection:** an exact event identity alone does not prove the current FP64-duration integrators can apply a tiny powered segment. The architecture explicitly retains that future integration obligation. Current constant-mass/RK/BEPU paths are not declared qualified and cannot be used as an automatic powered consumer.

**Strongest authority objection:** adding fuel writes after an existing physical commit would allow partial canonical success and leave the retained binding stale. The recommendation instead requires one future complete fixed canonical transaction and an explicit committed/private-invalidated terminal outcome.

**Strongest physical objection:** a scalar resource could silently double-count an existing wet article or preserve inertia without justification. Cold binding requires independently valid dry mass/inertia and exact-law source consistency. The first model is a declared mathematical point at dry COM; changing-mass and exhaust-momentum dynamics still require separate qualification.

## Cheap executed proof

The retained standard-library [arithmetic witness](arithmetic-witness.py) executed successfully with Python 3.11. It checks six fixed counterexamples/edges and the finite binary64/integer capacity bounds:

- positive FP64 debit lost by subtraction;
- positive duration rounded to zero despite representably nonzero impulse;
- rounded false endpoint equality;
- genuine exact endpoint equality;
- ordinary fractional-tick exhaustion;
- positive thrust with underflowed zero flow;
- exact repeated-debit conservation and maximum 2098/2118/2161-bit bounds.

This script is an analytical oracle, not a resource implementation or performance test. No build, Simulation run, profiler, allocation tracing or formal timing/storage campaign was performed. Future zero allocation and fixed storage are design targets, not claimed measured results.

## Source and scope preservation

Compared pre-investigation and post-investigation SHA-256 fingerprints for **all 1,932 tracked files: zero changes**. This includes production, permanent tests, dependency binaries, banked evidence and documentation. All **63 historical tag refs** remained unchanged. HEAD/main/origin/main and a fresh remote-main query remained at the stated baseline; no branch or index operation occurred.

The preexisting untracked staging-build-blocker evidence retained its original bytes. The only new working-tree content is this investigation directory. Blender, BEPU, physical mass, command/actuator state and solver code remain untouched. No new disposable build tree, runtime dump or proprietary source copy was created; no unrelated existing scratch was deleted.

See [identity.json](identity.json) for aggregate/selected fingerprints, protected refs and retained KSA provenance.

## Reproduction and evidence closure

1. At the recorded baseline, use the exact source paths/searches in [current-novacore-map.md](current-novacore-map.md).
2. Re-read the existing M14.23 prepare/preview/discard lifetime and M14.19/20 fixed-property binding; check the selected file hashes.
3. Run `python docs/engineering-evidence/finite-propellant-architecture/arithmetic-witness.py`. It writes only its compact result to stdout.
4. Check KSA installed-binary identity and retained decompilation hashes against the linked banked provenance before relying on its source anchors. No source text is copied here.
5. Compare exact source/ref identity, local Markdown link closure and whitespace before submitting this evidence. Future banking must include these dependencies in its exact stage tree and build that tree; this investigation stages nothing.

The evidence budget is 96 KiB. Nine concise files preserve the result and reproduction path. Exact file sizes/hashes are recorded in identity.json for the other eight files; the identity file is excluded from its own hash set.

**Stop for Project Control. Recommended next work: nonphysical finite-propellant segmentation only, after explicit authorization.**
