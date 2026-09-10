# M14.7 production acceptance and closeout

Project Control accepted **M14.7 — Evaluate coherent spacecraft motion at exact
physical-event epochs** on 10 September 2026. Milestone tag:
`m14.7-exact-event-spacecraft-motion`. M14 remains OPEN. This closeout performs no
new validation, allocation attribution or performance investigation.

## Accepted responsibility

Banked canonical spacecraft evaluators remain unchanged. Five additive production
files provide a read-only exact-event query. Canonical requests delegate to existing
coherent motion; fractional translation computes an exact bounded local duration
before FP64 analytical evaluation; fractional rotation uses banked rotation to the
canonical floor and the existing physical equations over the terminal fraction.
The result is immutable, transient and derived. StateRevision is provenance, not
mutation permission. Exact epoch identity is not exact FP64 physical state,
represented-zero terrain gap, contact admissibility or root certification. There is
no publication, persistent state, private execution, contact discovery or grounding.

Current authority: [motion contract](../../spacecraft-physical-event-motion.md).
Implementation and qualification records are the
[additive stage](../exact-event-additive-motion/README.md) and
[final timed-family qualification](../timed-measurement-split/README.md).
Their dated UNBANKED/FAIL/READY wording remains stage-specific historical evidence.
This closeout records the subsequent production acceptance.

## Accepted validation, not rerun at banking

- Debug/Release solution builds: PASS, zero warnings/errors.
- Canonical parity: 64 representative cases, bit-identical reused outputs.
- Exact duration: bounded, overflow, signed/negative-floor and fractional cases PASS.
- Translation: 120 cases / 360 axes; maximum position difference
  0.0001220703125 m at about 1e12 m, one ULP at that scale; velocity difference zero.
- Rotation: analytical quaternion difference at most 1.1103267813128674e-16;
  mixed-reference quaternion/rate differences about 2.8e-15. Independent refined
  midpoint reference, refusal/default, deterministic replay and immutability PASS.
- Early normal Release: 3/3 PASS, 38 groups each. Final fresh normal Release:
  5/5 PASS, 38 groups each, no retries/observer/special initialization.
- Existing full Debug, focused exact-event and M14.2–M14.6, ReferenceFrames and
  Precision qualification remains applicable to identical accepted source.
- Five timed windows are SPLIT SAFE: lunar/all-body orientation, circular/elliptic
  propagation and translation performance. Normal timing is separate from checked
  allocation measurement. Exact zero and entry/exit PASS in Debug/Release;
  deliberate byte[128] allocation detected 152 bytes.
- Lunar retains <10,000 ns/evaluation: focused Debug 4,428.5 and Release 2,245.7.
  The other four timing responsibilities remain report-only.

## Bounded performance

These are batch-average ns per evaluation, not individual-call tails. Each fixture
uses 41 samples of 256 evaluations after 4,096 warmups, with separate allocation
measurement. No hard event-local latency budget or full-duration envelope is claimed.

| Fixture | Median | P95 | P99 | Allocation |
|---|---:|---:|---:|---:|
| Analytical canonical wrapper | 639.453 | 650.391 | 48,257.422 | 0 |
| Analytical rational | 907.422 | 927.344 | 1,019.922 | 0 |
| Analytical batch32 | 963.672 | 973.047 | 1,033.984 | 0 |
| RK4 canonical wrapper | 1,371.094 | 1,385.156 | 1,415.625 | 0 |
| RK4 rational | 2,032.812 | 2,044.531 | 2,102.734 | 0 |
| RK4 batch32 | 1,866.016 | 1,893.750 | 1,896.875 | 0 |

The analytical canonical-wrapper P99 is RETAINED / UNFILTERED / UNATTRIBUTED.
It is neither classified as a defect nor claimed universally harmless. No rerun
or optimization occurred at closeout.

## Historical lineage and retirement rationale

The [original candidate report](../exact-event-spacecraft-motion/README.md), its
identity/results and the [history manifest](history-manifest.json) retain the
superseded mechanism's stage-specific evidence. Copies are byte-identical to their
original reports. The manifest gives original paths, lengths and SHA-256 identities.
Relative historical links/scripts refer to each original report directory;
supporting research remains at those E:\NovaCore-Research paths. Those directories
are outside the reviewed scratch cleanup and are not removed by this closeout.

| Stage | Retained result |
|---|---|
| Post-M14.6 architecture assessment | Candidate E: smallest missing read-only motion provider; exact state/contact certification is a separate responsibility. Current KSA/BRUTAL and official history establish ownership, not a copied numerical mechanism. |
| Original generalized mechanism | Shared canonical translation/attitude kernels and exact-epoch overloads; full Release stopped at SAS, 12,336 bytes. |
| SAS A/B | Stopped at an earlier candidate attitude witness; SAS causality unanswered. |
| Attitude A/B | Baseline 0/0/0; candidate 0/12336/12336. Incidental impulse witness, no common mechanism established. |
| Narrow observer attribution | Two instrumented candidate runs did not reproduce the target. Attribution insufficient. |
| Old normal acceptance | 2/5 clean; three allocation failures. Acceptance not cleared. |
| First structural control | Candidate 3/3 clean; restored control 1/3 clean. No necessary component isolated. |
| Banked baseline control | 5/5 clean at reconciled main 00f226096b61a117184464b5619e464d39789574. |
| Second/final control | Exact-event-only capability not necessary for the phenomenon; candidate and control witnesses persisted. No third discriminator. |
| Additive replacement | 327 existing production files unchanged; five sidecars, numerical/authority gates and early 3/3 passed. Initial final lunar witness retained. |
| Timed-family qualification | Accepted split measurements; fresh final 5/5 and bounded performance pass. |

Disposition of all historical 12,336-byte witnesses:
**UNATTRIBUTED HISTORICAL ALLOCATION-COUNTER WITNESS.** No common mechanism, CLR bug,
production object allocation or Candidate E allocator was proven. Banking accepts
the replacement's passed contracts; it does not retrospectively attribute or
declare the earlier witnesses fixed.

Before retiring the old dirty tree, its complete tracked source diff was reviewed:
four canonical evaluator files added generalized/shared-kernel behavior; one new
duration file and an appended exact-event test group completed that mechanism.
Its contract, numerical proof, source hashes, failure witnesses and bounded
investigations are retained. The old source implementation has no continuing
production responsibility and is not copied into the accepted source or retained
as a second implementation. Old tests' useful contracts survive in the qualified
additive test. Three unique old evidence files and the old contract were copied
and verified before any source retirement.

## Static closeout gates

Before staging: 332/332 accepted production hashes matched (327 pre-existing plus
five new); 10/10 Simulation test/project hashes matched the final timed-family
identity. Old 14-file candidate identity and 167-file source closure matched before
retirement; M14.1–M14.6 tag targets matched. No diagnostic hooks, observer/profiler
dependency or temporary controls remain in candidate source. No source/test edit
or validation rerun is part of this ticket. Exact-zero thresholds and the accepted
timed split remain intact. Staging admits only production, permanent tests,
current docs/rules and the concise evidence packages.

## Reproduction and retention

Use a clean checkout of `m14.7-exact-event-spacecraft-motion`, SDK 10.0.303 and
.NET 10.0.12 with ordinary runtime configuration. Follow the retained
[timed-family recipe](../timed-measurement-split/reproduce.md), substituting the
checkout location for the historical additive worktree path. Build native
prerequisites as documented in the earlier additive recipe if absent. The
historical branch/path names describe the experiment, not a requirement to retain
development branches. Production/test source is supplied by the milestone;
temporary reporters/build trees are not required.

KEEP: three old candidate reports, the original architecture assessment and final
bounded investigation reports/results, additive numerical/identity evidence, the
decisive lunar witness, timed-family results and recipes, closeout provenance.
DISPOSE: only the seven reviewed scratch directories in
[reviewed-cleanup.json](reviewed-cleanup.json), containing temporary comparison
sources, builds, instrumentation and logs whose results are retained above.
Their reviewed total was 2,282 files / 169,454,138 logical bytes. Project Control
completed manual cleanup on ALBATRONIC007; independent pre-commit verification
confirmed all seven paths absent. Reviewed diagnostic scratch remaining: zero
files / zero bytes. No absent-path deletion was retried. The earlier failed manual
command used malformed paths; its reported result was not accepted as proof of
cleanup until the corrected command and independent filesystem check agreed.

The retirement sequence is: preserve evidence, commit accepted additive state,
annotated tag, fast-forward main and normal push, verify remote refs, return
E:\NovaCore to clean main, then remove the separate additive worktree and both
superseded development branches. No older milestone is moved; no history is rewritten.
The pre-existing stale M12B worktree registration is unrelated to Candidate E and
is not a live development worktree or part of this cleanup authorization.

No next responsibility is assigned. Stop for Project Control.
