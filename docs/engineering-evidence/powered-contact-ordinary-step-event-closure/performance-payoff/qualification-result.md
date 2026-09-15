# Contract migration, exact-zero recheck and bounded performance/payoff result

2026-09-15. **PASS — PERFORMANCE / PAYOFF UNCERTAIN.**
Allocation and native-equivalence gates PASS. Structural simplification is
supported for the admitted ordinary-step fixture. Repeating preparation-side
tails leave integrated performance/payoff unresolved. No new investigation,
optimization, production implementation, test migration or architecture banking.

## What happened / contract migration

Project Control accepted [projection-contract B](projection-contract/README.md).
The old allocating Rational result and binary64 mismatch STOP are preserved in
[allocation-closure](allocation-closure/README.md). New diagnostic acceptance
removes only the four old/new ax/ay/wx/wy double equality requirements. All six
FP32 bits, exact ledger, banked numerical semantics, inverseMass/H double checks
and corrected-self binary64 determinism remain enforced. No tolerance or new map.

The current banked-duration/ratio/scale correction was reused exactly. Its
FixedInput/import and Map/Ratio bodies match the retained correction. Three
current production source primitives/helper files were linked, not modified.
There is no new retained mapper cache or pool. The accepted KSA exact-event
projection non-equivalence remains applicable; no new mechanism or KSA write.

## Required outcomes

Six consumed FP32 bits, independently recovered from off-com-8.json:
`[1077935923,-1061557234,-1077097267,1067030938,1040187119,1015580809]`.
[FP32/exact-ledger result](allocation-recheck/fp32-equivalence.json): PASS.

One fresh allocation process after 128 warm maps:

| Window | Raw bytes |
|---|---:|
| Immutable read | 0 |
| byte[128] positive control | 152 |
| Warm single 1 / 2 / 3 | 0 / 0 / 0 |
| Warm repeated 128 | 0 |

Every checked entry/exit succeeds. Each later timing process separately measures
A=0, B=0 and C128=0 using equivalent fixtures and the unchanged helper. No timing
inside a no-GC region. [Allocation detail](allocations.md).

The single corrected native witness matches **every original JSON section**
except the binary's build-directory path: exact ledger, native prestate/order,
all four snapshots, endpoint bits, contacts/cache, material and physical bars.
Eight iterations configured, one observed substep and ordinary dt unchanged.
No extra solve or manual cache work. [Native comparison](allocation-recheck/native-equivalence.json).

## Performance boundaries and all process results

Original plan deferred matched lifetime definition until allocation passed;
[the finalized preregistration](allocation-recheck/timing-plan.md) retains its
A/B/C boundaries. Three fresh Release processes, 128 warm and 1,024 measured
samples per arm. A/B use independent identically prepared retained worlds, one
target interval each; cold 120-step preparation/disposal is outside timing.
Timed order alternates. Native source/endpoint/contact/cache equality is checked
outside each timing window. This is not a continuous live depletion episode.

- A: fixed prepared off-COM native-input control + ordinary retained BEPU step.
- B: unchanged corrected event Map + six projections + same native step.
- C: corrected event Map + six projections only.

All values below are **microseconds**. Nearest-rank P95/P99, middle-pair median.

| Process | Boundary | Median | P95 | P99 | Max |
|---|---|---:|---:|---:|---:|
| 1 | A | 2.9 | 4.5 | 6.3 | 74.7 |
| 1 | B | 23.2 | 257.0 | 311.7 | 384.4 |
| 1 | C | 20.4 | 20.8 | 23.1 | 37.2 |
| 2 | A | 2.9 | 5.0 | 5.8 | 43.8 |
| 2 | B | 24.0 | 260.4 | 313.2 | 357.2 |
| 2 | C | 20.3 | 20.6 | 22.7 | 45.3 |
| 3 | A | 2.7 | 5.2 | 6.6 | 11.6 |
| 3 | B | 24.1 | 266.6 | 304.8 | 369.1 |
| 3 | C | 20.6 | 20.7 | 24.2 | 35.3 |

Median of process medians: A 2.9, B 24.0, C 20.4 us. Maximum across processes:
A 74.7, B 384.4, C 45.3 us. These are not pooled quantiles. B−A median differences
20.3/21.1/21.4 us are descriptive; independent medians and cold-order asymmetry
do not establish an exact causal delta. B preparation medians are 20.7/21.3/21.5 us.

Per-process complete summaries/top-ten rows:
[1](candidate-process-1.json), [2](candidate-process-2.json),
[3](candidate-process-3.json). [A summary](baseline.json),
[C summary](event-preparation.json).

## Tails, cold costs, retained state and solver work

B tails repeat across all processes and are primarily in the preparation
timestamp bracket; no measured generation collection increments coincide.
Underlying cause remains UNATTRIBUTED. C runs later and cannot dismiss B tails.
See [tails](tails.md). No isolated budget exists; no microsecond cutoff is invented.
Current 6.67–6.94 ms frame headroom has whole-engine scope, not a mapper budget.
No FPS, scaling, backlog or integrated publication cost is inferred here.

The 2,304 warm/measured source and endpoint checks per process all pass. There
are 2,304 explicit target Timestep calls and observed target substeps per process;
configured iterations are eight, internal per-iteration observation UNAVAILABLE.
No event-sized step or event-specific solver mechanism. [Work detail](solver-work.md).

FixedInput is a 3,832-byte raw value representation; outputs are 48/24-byte
double/float value records. New mapper retained heap state: NONE. Cold worlds
average 577.4/617.9/597.6 us preparation each; two concurrent native pools reserve
6,029,312 bytes. Cold allocation traffic is separately reported, not retained
storage or a hidden warmed-path cost. [State/cold detail](retained-state.md).

## Candidate A and structural payoff

Comparison: **NOT DIRECTLY COMPARABLE**. Historical D+12 numerical medians
59.8/60.4/59.5 us exclude native world/installation/publication and describe a
different operator. No speedup ratio, rerun or retirement. [Comparison](candidate-a-comparison.md).

This fixture uses one ordinary step and needs no event-sized constrained pieces,
event-duration cache rescale, custom piece-basis transport, load/D initializer or
12-sweep piece policy. Native feature/cache coherence, duration provenance,
friction validity and physical continuation requirements remain. The paired
private-response replacement contract is unresolved. These are bounded
prospective findings, not universal deletion authority. [Complete disposition](mechanism-payoff.md).

## Red team, source and execution integrity

Independent verifier PASS on all 18 required objections and result limits.
[Verification](verification.md) records the reporting-only pre-timing compile
correction and the separate JSON reporting parse correction. No application
measurement or native witness was retried. No full correctness campaign resumed.

Branch `codex/powered-contact-numerical-prerequisite`; HEAD/main/origin/main/remote
main remain `49057fecceb0f725d5f551ec40e2780971b0d81d`. Historical tags, production,
permanent tests, accepted prior evidence, KSA and Blender are untouched.
All new source is diagnostic/reproduction code under allocation-recheck; all
other additions are concise results/reports in this performance-payoff child.
Nothing staged, committed, pushed, tagged or banked. See
[identity](allocation-recheck/identity.json) and [cleanup](allocation-recheck/cleanup.md).

## Judgment / what happens next

**PASS — PERFORMANCE / PAYOFF UNCERTAIN.**

The accepted contract migration, exact-zero gate and native equivalence are
resolved. Bounded structural payoff is demonstrated, but repeating 0.3–0.38 ms
preparation-side tails and unmeasured integrated responsibilities prevent an
unqualified architecture/performance acceptance recommendation.

Return the measurements and this uncertainty to Project Control. No more
profiling, mapper attempts, warmup changes, numerical experiments or performance
runs are undertaken. Architecture acceptance, evidence consolidation and
production-integration design require Project Control's next decision; none is
started automatically. Candidate A and permanent tests remain untouched.

UNBANKED. STOP FOR PROJECT CONTROL.
