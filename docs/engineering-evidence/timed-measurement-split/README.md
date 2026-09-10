# Timed zero-allocation family — split measurement qualification

2026-09-10. **Lead judgment: PASS.**

**TIMED ZERO-ALLOCATION FAMILY — VALIDATED SPLIT MEASUREMENT**

**ADDITIVE EXACT-EVENT SPACECRAFT MOTION CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE**

The candidate remains **UNBANKED**. No milestone number is assigned.

## Baseline and reason

Main/origin/main/HEAD: `00f226096b61a117184464b5619e464d39789574`.
M14.6 tag `m14.6-physical-event-epochs` remains at
`d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`; all M14.1–M14.6 targets are unchanged.
Work is confined to `E:\NovaCore-ExactEvent-Additive`, branch
`codex/exact-event-spacecraft-motion-additive`. The old candidate at `E:\NovaCore`,
branch `codex/exact-event-spacecraft-motion`, remains untouched.

The [previous additive run](../exact-event-additive-motion/README.md) passed early
Release 3/3 but stopped at final run 1: lunar orientation reported 12,336 counter
bytes and 1,952.7 ns/evaluation. That counter witness made the previously deferred
timed family an active acceptance responsibility. It does **not** establish a
production allocator, CLR mechanism, test defect or equivalence to the historical
8,160 case. No mechanism investigation, observer, profiler, EventPipe or runtime
tuning occurred in this ticket. The previous failure record is preserved.

All **332 production fingerprints** match before and after: 327 banked files plus
five additive files. Only `Program.cs` and `SpacecraftTranslationTests.cs` changed
among permanent Simulation test files. The accepted helper, exact-event test and
all other tests remain byte-identical. Both full solution builds passed with zero
warnings and errors.

## Five-window reconciliation

The original [42-window inventory](../physical-event-epoch/zero-allocation-inventory/inventory.md)
rows **19, 20, 21, 22, 41** match current source. These are five source responsibilities;
translation instantiates its one window for one and 256 spacecraft. The ninth-body
orientation probes and construction counters are report-only and are not migrated.

| Gate / current file and method | Timing contract | Allocation contract | Previous combined mechanism | Split classification |
| --- | --- | --- | --- | --- |
| Lunar; `Program.cs`, `CelestialBodyOrientationTests` → `LunarOrientationTiming` | 100,000 calls; mean **<10,000 ns/evaluation** | Same warmed calls; **0 bytes** | Raw counter spans stopwatch start/stop and production loop | SPLIT SAFE |
| All-body; `Program.cs`, `CelestialBodyOrientationTests` | 90,000 calls; total ms and ns per nine-body pass, **report-only** | Same warmed calls; **0 bytes** | Raw counter spans prepared stopwatch and production loop | SPLIT SAFE |
| Circular; `Program.cs`, `TwoBodyPropagationTests` | 100,000 calls; total ms, **report-only** | Same warmed calls; **0 bytes** | Timer starts before raw counter; elapsed read before counter close | SPLIT SAFE |
| Elliptic; same file/method | 100,000 calls; total ms, **report-only** | Same warmed calls; **0 bytes** | Timer restarts before raw counter; elapsed read before counter close | SPLIT SAFE |
| Translation; `SpacecraftTranslationTests.cs`, `Performance` | 101 batch-average samples per count; sorted indices 50/95/99, **report-only** | Same 101 ×1,000 ×count calls; **0 bytes** | Raw counter spans sample timers, loop and sample recording | SPLIT SAFE |

Only lunar has a hard latency threshold. No new threshold was invented for the
other four. Their timing/reporting contracts are retained, not described as
passing an absent latency limit.

## Setup, warmup and workload preservation

**Lunar / all-body:** readonly model and lunar pack, preceding orientation
correctness tests, one zero-epoch call per body and nine ×100 report-only probes.
The probes are exact-text preserved in `OrientationMeasurementWarmup`. Lunar calls
Moon at `pass * 1_000_000` ticks with a per-call success check and XOR quaternion-W
checksum. All-body visits the same nine bodies in the same order for 10,000 passes
at `pass` ticks; non-short-circuit `&=` guarantees all 90,000 evaluations. Neither
evaluator changes its model/pack or caches mutable state. Both independent passes
reuse these already-warmed immutable inputs without added warmup loops.

**Circular / elliptic:** same readonly Cartesian inputs, epoch, gravitational
parameter and requested times (quarter circular period, moderate elliptic state at
4,321 seconds). Existing oracle/refusal/hash setup and explicit circular/adapter
warmups remain. Each pass performs 100,000 evaluations with the original success
check and maximum-iteration reduction. Inputs are not advanced or mutated.

**Translation:** same immutable state array for counts 1 and 256, original positions,
mass, state order and epochs `1000000 + repeat`. Each responsibility independently
uses the existing **500 ms warmup policy**, then 101 ×1,000 ×count evaluations.
The original warmup contributes to checksum and has a runtime-dependent call count;
each checksum independently remains >0. Whole checksums are intentionally not
equated across two different time-bounded warmups. This method calls the pure
translation evaluator, not the class's mutating transaction fixture.

No production state mutates between repetitions, so no fresh mutable fixture or
reset is needed. Setup allocation stays outside both measured workloads. Timer
operations and sample-storage writes are measurement machinery, excluded from the
separate production allocation pass.

## Measurement architecture and representative

Normal timing retains original start/stop/elapsed boundaries, initialization,
production calls, counts and predicates. Both orientation timers finish before
either new allocation region; both propagation timers do likewise. All-body retains
stopwatch Start/Stop/Reset preparation. For propagation, removal of the opening raw
counter read removes one instrumentation operation that previously lay inside the
timer. The production workload is unchanged; this small instrumentation difference
is explicit. An added Stop occurs only after the last elapsed value is captured.

Separate allocation passes use the unchanged `OrdinaryAllocationMeasurement`:
checked temporary no-GC entry, then counter; original logical work; counter close
before exit; guaranteed cleanup; hard entry/exit failure; exact zero without
subtraction, tolerance, skipped checks or retries. Its existing **1 MiB reservation**
is sufficient for all qualified cases, including the largest repeated translation
case. It is a reservation for test isolation, not an allowed allocation budget.

Success, checksum, timing and allocation predicates fail independently. Orientation
also checks timing/allocation checksum equality; propagation checks equal iteration
reductions. No no-GC region covers normal timing or the whole suite. Existing
ordinary and other checked windows are unchanged.

Lunar was implemented and qualified **before the other four were migrated**:

| Configuration | Normal ns/evaluation | Threshold | Allocation | Entry / exit | Checksum / positive control |
| --- | ---: | --- | ---: | --- | --- |
| Debug | 4,306.7 | <10,000 PASS | 0 | PASS / PASS | Matching `003FCCC32716069E`; byte[128] →152 bytes |
| Release | 2,402.5 | <10,000 PASS | 0 | PASS / PASS | Same checksum; byte[128] →152 bytes |

The helper's existing non-inlined deliberate byte[128] control is reused; no five
redundant controls were added. Representative and complete-family focused runs each
detected 152 counter bytes per configuration. These runs do not use an object observer.

## Complete-family focused results

| Gate | Debug normal timing | Release normal timing | Isolated allocation |
| --- | --- | --- | --- |
| Lunar | 4,428.5 ns/evaluation | 2,245.7 ns/evaluation | 0 /0; entry/exit PASS; checksum parity PASS |
| All-body | 175.587 ms /90,000 calls | 43.314 ms /90,000 calls | 0 /0; success and checksum parity PASS |
| Circular | 37.397 ms /100,000 calls | 26.135 ms /100,000 calls | 0 /0; max iterations 1; parity PASS |
| Elliptic | 58.520 ms /100,000 calls | 39.031 ms /100,000 calls | 0 /0; aggregate max iterations 5; parity PASS |
| Translation, count 1 | Median/P95/P99 172.20 /189.60 /202.10 ns per batch | 7.90 /7.90 /8.00 ns per batch | 0 /0; both checksums >0 |
| Translation, count 256 | 44,109.00 /45,742.00 /46,207.20 ns per batch | 1,933.90 /1,949.10 /1,956.90 ns per batch | 0 /0; both checksums >0 |

Translation is opt-in and is included explicitly in the focused family command;
it was not silently added to the normal full suite. The default 38-group order and
runner are exact-text unchanged. Timings are measurements, not attribution of the
previous failure or guarantees of identical future latency.

## Fresh Candidate E Release acceptance

Exactly five fresh normal Release full-Simulation processes, no arguments, no
build between runs, no profiler/runtime overrides, no retries. Earlier early 3/3
and the previous failed final run were **not counted**.

| Run | Groups | Result | Allocation failure | Timing failure | Other failure | Lunar ns/evaluation |
| --- | --- | --- | --- | --- | --- | ---: |
| 1 | 38/38 | PASS | None | None | None | 2,100.2 |
| 2 | 38/38 | PASS | None | None | None | 1,888.7 |
| 3 | 38/38 | PASS | None | None | None | 1,995.3 |
| 4 | 38/38 | PASS | None | None | None | 1,923.1 |
| 5 | 38/38 | PASS | None | None | None | 2,020.5 |

All zero-required gates passed exactly, all checked entry/exit operations passed,
and every correctness contract was reached and passed. The opt-in translation
timed allocation contract was separately qualified in both configurations above.
Existing positive controls correctly remain nonzero. Source and test fingerprints
identify the tested candidate in [identity.json](identity.json).

## Candidate E bounded performance

Only after 5/5: one unchanged performance-selector process. Predeclared 41 samples
×256 evaluations per case, after 4,096 warmups; separate checked allocation pass
of 8,192 calls per case. Values below are **ns per evaluation derived from batch
averages**, not individual-call tail measurements. Batch32 varies craft identity
and query epoch while retaining one production store.

| Model | Case | Median ns | P95 ns | P99 ns | Warmed bytes |
| --- | --- | ---: | ---: | ---: | ---: |
| Spherical analytical | Canonical exact-event wrapper | 639.453 | 650.391 | **48,257.422** | 0 |
| Spherical analytical | Rational | 907.422 | 927.344 | 1,019.922 | 0 |
| Spherical analytical | Batch32 | 963.672 | 973.047 | 1,033.984 | 0 |
| RK4 | Canonical exact-event wrapper | 1,371.094 | 1,385.156 | 1,415.625 | 0 |
| RK4 | Rational | 2,032.812 | 2,044.531 | 2,102.734 | 0 |
| RK4 | Batch32 | 1,866.016 | 1,893.750 | 1,896.875 | 0 |

The analytical canonical P99 is the highest of 41 samples, an observed outlier
retained without filtering or attribution. No rerun or optimization followed.
The recipe does not record individual sample timestamps or explain this outlier.
Typical costs are bounded and suitable for the supplied local-event fixtures,
with no new persistent preparation, cache or allocation. This is not a total
future contact/solver budget or a benchmark over the maximum supported duration.
Existing canonical callers retain identical production mechanisms and do not route
through the sidecar. The allocation/performance bar is met without weakening tests.

## Verifiers and durable rules

- **VERIFY A — timing:** independent read-only review passed. Normal loop bodies,
  counts, order and timing thresholds are preserved; no checked region overlaps
  a timer. Propagation's removed counter-read overhead is disclosed.
- **VERIFY B — allocation:** repeated work uses identical immutable inputs;
  translation repeats its existing time-bounded warmup independently. Exact zero,
  hard entry/exit failure and the positive control remain effective. No timer
  infrastructure is charged to the separate production allocation contract.
- **VERIFY C — family:** all five are SPLIT SAFE. No excluded or non-equivalent
  gate. The reporting-only latency cases and translation's two instances remain
  explicitly distinct; this is not permission to migrate unrelated mutable tests.
- **VERIFY D — Candidate E:** 332 production hashes unchanged, including every
  additive source file; accepted exact-event test unchanged; old worktree status,
  14 candidate fingerprints and 167-file source closure unchanged. No test-only
  result is credited as a production correction or causal explanation for 12,336.

Following technical PASS, `ENGINEERING_RULES.md` now records the authorized
allocation-witness/payoff rule and the qualified separation of timing from
allocation isolation. Current-state, handoff and candidate documentation report
ready for Project Control acceptance, **not banked**. Prior failure evidence is
unchanged and linked through the new closeout.

## Evidence and handoff

Permanent budget: under 100 KiB for this bounded package. Retain this reconciliation,
source identity references, [structured validation](validation.json), review result,
and [reproduction/cleanup recipe](reproduce.md). Existing old evidence remains intact.
Raw stdout, temporary before-edit test copies and build logs are disposable only
after Project Control accepts this result; no output has been deleted. Exact counts
and reviewed paths are in [storage.json](storage.json).

No staging, commit, merge, tag, push, bank or new responsibility. Proposed Candidate E
accomplishment title: **Evaluate coherent spacecraft motion at exact physical-event
epochs**. No milestone number. **STOP FOR PROJECT CONTROL.**
