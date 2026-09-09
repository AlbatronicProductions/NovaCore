# Bounded timeline payload ownership revision

## Pre-implementation decision, 2026-09-09

Baseline/main/origin: `35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7`;
branch `codex/contact-response-transaction`. All original 14 candidate fingerprints
verified unchanged before revision. Prior proofs establish 112-byte banked versus
312-byte inline candidate records and the layout-only +8,104 counter effect.

| Representation | Generic storage | Contact storage | Lookup/lifetime | Judgment |
|---|---|---|---|---|
| A: typed external store keyed by event ID | 112 bytes possible | 200-byte intent plus map metadata | Extra key/index map or bounded linear search | No advantage over existing index and identity |
| B: typed bounded slots, compact index | Derived 112 bytes by placing an int in existing alignment padding | Derived 248-byte slot: intent 200 + header 40 + free-list int/padding 8 | O(1) slot; existing unique header sequence identifies occupancy; deterministic free list | Selected |
| C: separate discriminated queues | Small records per kind | Typed arrays | Requires cross-queue ordering, replacement and cancellation redesign | Too broad |
| D: universal inline payload | Measured 312 bytes, references in every record | Included universally | Simple direct copy but proven universal cost | Not accepted |

No per-event allocation, dictionary, boxing or object payload is introduced by B.
Generic heap/ID lookup and history capacity behavior stay banked. Two 20,000-event
arrays return from 12,480,000 to a derived 4,480,000 element bytes; an independently
configured C-contact arena costs approximately 248*C element bytes. Array/object
headers and existing maps are additional. Runtime sizes must confirm the derivation.
Marker-only timelines default to zero contact capacity and no contact arena object.

The timeline owns the typed arena and immutable intent after admission. The compact
payload has a positive slot index; its existing full event header binds it to one
occupancy. Sequence overflow already rejects rather than wrapping, and IDs remain
reserved. No extra generation identity is invented. Slots are deterministic LIFO
after initial ascending allocation. Lookup validates kind, canonical pending record,
slot bounds and full header before returning intent. Failed/stale/forged transactions
retain event and payload. Cancel, replacement and successful canonical consumption
retire the payload and clear reference fields; no transient payload is held by
cancelled-record history. Coupled committed history keeps its existing value copy.

All normal rejection and retirement checks precede mutation. Publication is the
existing exclusive single-writer schedule/commit boundary; no callbacks or normal
failure paths may occur between payload/event publication or paired commit/retirement.
Capacity is explicit and never grows for contact slots. Typed contact replacement
requires a free destination slot; exhaustion leaves the old event unchanged.

The winner is one bounded timeline-owned typed store, not a new event framework.
Physics evaluation, authority mutation, impulse mapping and coupled history are
preserved. No no-GC migration, profiler or runtime-policy change is authorized.

## Implemented ownership and measured structure

One bounded revision implemented B. `SimulationTimeline.ContactPayloads.cs` owns
the array, deterministic free list, admission, exact canonical resolution and
retirement. `SimulationTimeline.cs` shares preflight/commit logic for generic and
typed admission and checks retirement before cancel/replace/consume mutation.
`SimulationEventPayload.cs` retains only an int slot index placed after the byte
kind in its alignment gap. Generic requests cannot admit a contact handle; callers
use the timeline's typed ScheduleContactImpulse/ReplaceContactImpulse boundary.
The unused contact request factory was retired, returning SimulationEventRequest
to banked source contents.

The pure response evaluator resolves the immutable intent from its timeline;
engine dispatch and canonical re-evaluation supply that owner. Impulse equations,
both authoritative writes, paired preflight and coupled history are unchanged.
ScheduleResult gains the explicit contact-arena exhaustion status. Permanent
ContactResponseTests migrate admission and add lifetime/structure coverage and
bounded storage microbenchmarks. The permanent Program.cs allocation boundaries
are byte-identical to the preceding candidate.

| Runtime structure, Debug/Release | Bytes | Managed references |
|---|---:|---|
| Banked generic event | 112 | No |
| Original inline candidate | 312 | Yes |
| Revised generic event | 112 | No; RuntimeHelpers check passes |
| Immutable contact intent | 200 | Yes, unchanged provenance |
| Configured typed slot | 248 | Yes, only in explicitly allocated arena |

Marker-only timelines have capacity/count zero and share an empty array. Marker
events never acquire, resolve or release a contact slot. Generic terminal paths
only check event kind before returning from the retirement helper. A configured
contact arena is fixed capacity and has no heap object/dictionary allocation per
event. Existing generic heap, reserved-ID set and cancelled-record list still
require their normal independent preallocation; their lifetime/growth policy was
not redesigned. The array element savings derived above are verified, not a claim
that all timeline/map/object overhead is included in those numbers.

Retirement clears intent/provenance references and returns the slot. Old cancelled
records retain only their compact header/index, which cannot resolve after removal
or reuse. The sequence is never wrapped/reissued; complete-header equality also
checks time, priority, ID and kind. The single-writer invariant excludes concurrent
mutation; fatal OOM/internal corruption is not an added rollback guarantee.

## Matched original workload, before broader validation

Reconstructed the same banked test driver in a disposable tree, overlaid only the
revised production files and preserved driver SHA256
`59A8EC6369D1F3141D68C5A97B61C4E6FE7EB250EE416FE287B79A463025E42D`.
Predeclared three fresh Debug processes and three fresh Release processes. No
additional runs, no-GC guard, profiler, warmup/count change or runtime override.
Same .NET 10.0.11/X64 and complete environment hash as prior matrices.

| Configuration | Banked prior | Inline prior | Revised runs 1 / 2 / 3 |
|---|---|---|---|
| Debug | 0 / 0 / 0 | 8104 / 8104 / 8104 | 0 / 0 / 0 |
| Release | 128 / 128 / 128 | 8232 / 8232 / 8232 | 128 / 128 / 128 |

Every revised run: 112-byte event, invariants true, 100 pending, 9900/9900
cancellations, GC counts (1,1,1) unchanged. The candidate-associated excess is
absent. The shared instrumented Release 128 bytes remain unattributed and still
fail that disposable zero-byte assertion; they are not subtracted or excused.
This does not identify the earlier allocating object/accounting mechanism.

`results.json` retains each PID, exact DLL path, assembly hash, environment identity
and observation. The subsequent source adjustment only preserved banked error-code
precedence in replacement preflight (not the measured Schedule/Cancel path).
No matched rerun was necessary for that unexecuted path. Final full-suite and focused
validation used the final production contents.

## Functional validation and new payoff stop

Both full solution builds passed with zero warnings/errors. One full Simulation
run per configuration was executed, without retries:

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS | PASS |
| Full Simulation | 35/35 groups PASS | 29 pass, 1 fail, 5 not reached |
| Permanent original timeline gate | 0 bytes, 9900 cancellations, invariants PASS | Same |
| Existing qualified orchestration gate | 0 bytes, Completed, 1000/1000 events | Same |
| Focused contact response | PASS; 256 complete transactions, 0 bytes | Same |
| Focused M14.2 translation | PASS | PASS |
| Focused M14.3 generation | PASS | PASS |
| ReferenceFrames | 11 groups PASS | 11 groups PASS |
| Precision | PASS | PASS |
| M14.1 physical query/stale snapshot | 2 pass, 0 fail/skip | Same |
| M14.3 production-terrain contact | 1 pass, 0 fail/skip | Same |
| git diff --check | PASS | Shared tree |

The new Release failure is `Program.cs:282`, assertion
**"warm SAS proof evaluation allocation"**, in the SAS sign/frame continuity group.
Its measured-byte value is not reported by that unchanged assertion. The test
contains one warm evaluation and 100,000 measured SAS evaluations/hash updates.
No attribution was attempted, no diagnostic counter hook added, no retry, no
threshold change and no no-GC migration. Debug passes that gate. The remaining
five groups (analytical sampling, frame extraction, trajectory replacement,
celestial impulses, final allocation group) were not reached in Release.

**DIAGNOSTIC PAYOFF RECHECK:** the new SAS failure is outside this bounded payload
ownership responsibility. Candidate causality and runtime accounting remain
unclassified. Stop attribution and return it to Project Control. Independent
explicitly requested regressions above were completed; none replaces the missing
full Release pass. No broad SAS/CLR/performance investigation was opened.

Retained numerical results match the original candidate: max delta-v error
1.6653345369377348e-16, delta-omega 2.220446049250313e-15, linear momentum
1.3322676295501878e-15, root angular momentum 7.105427357601002e-15, all within
the original scale-derived bars. Exact pose continuity and complete-history replay
hash `0x88CC7A6780F19270` match Debug/Release and 1/30/60/144/1000 cadences.

## Bounded Release performance

One performance invocation: storage paths have four warmup batches then 21 timed
1000-operation batches and 21 separate allocation batches. Complete transactions
retain the original 4-warmup, 21x256 timing/allocation protocol. Preparation is
outside measurement; no runtime overrides or pass-seeking reruns.

| Path | Median ns/op | P95 | P99 | Allocation median / max per batch |
|---|---:|---:|---:|---|
| Marker schedule + cancel | 326.4 | 362.2 | 7183.3 | 0 / 0 |
| Contact schedule + cancel | 1251.4 | 1287.1 | 1294.0 | 0 / 0 |
| Validated contact lookup + intent equality check | 321.5 | 324.0 | 326.2 | 0 / 0 |
| Complete EvaluateNext + ValidateAndCommit | 4603.1 | 4705.5 | 4719.1 | 0 / 0 |

Percentiles are batch-average costs, not individual-event tails. The marker P99
outlier is retained; it was not investigated or removed. The previously retained
inline-candidate complete transaction median/P95/P99 was 8021.5/8131.2/8144.9 ns.
This is a descriptive cross-run comparison, not a synchronized causal speedup
experiment. No earlier matched marker/lookup microtiming exists; no fabricated
before values are supplied. Lookup timing includes its correctness comparison,
not just an isolated array access. This is not future solver/contact cost.

## Red-team review

- **VERIFY A — lifetime:** strongest concern is reusing a slot while an old pending
  snapshot or rejected proposal survives. Tests cancel/reuse the same slot, reject
  the old header/proposal, reject forged indices and direct generic admission, prove
  full/exhausted replacement leaves ownership unchanged, consume/clear on commit,
  retain rejected proposals, test final sequence/overflow and 9900 reuses. Existing
  history/revision failures now also verify active payload count and arena invariants.
  History remains an independent immutable value after slot reuse. No storage blocker
  found under the explicit exclusive single-writer contract.
- **VERIFY B — determinism:** strongest concern is free-list order changing event
  execution. Only existing time/priority/sequence/ID ordering reaches the heap;
  slots are never enumerated to choose an event. Initial allocation and reuse are
  deterministic. Complete numerical/replay history hash is unchanged across both
  configurations and fresh focused/full processes. No determinism blocker found.
- **VERIFY C — scope:** strongest concern is creating a generic event framework or
  shifting cost to per-event objects. One typed partial/array, an int in existing
  padding, existing header as generation and existing heap/index are sufficient.
  No new dictionary, boxing, object events, physics cache, solver or dependencies.
  Paired state/history/physics code is unchanged except passing the timeline resolver.
  The separate full Release SAS failure remains an acceptance blocker, not a reason
  to expand this implementation.

## Fingerprints, evidence and handoff

The original 14 fingerprints remain historical in the parent package. This folder's
`candidate-fingerprints.json` records all 16 relevant final paths, including the
restored banked request source and two new responsibility paths. `source-drift.json`
compares the original candidate exactly: seven production paths and the contact
test changed; six original paths (including paired state/store/transaction envelope
and permanent Program.cs) are unchanged. New source paths are ScheduleResult's
capacity status and the typed timeline partial. No rendering/terrain/solver drift.

Retention budget: **64 KiB** for this bounded report, compact results/provenance,
fingerprints, validation summary and reproduction runner. Bulk console logs,
comparison binaries and source copies remain disposable. Reconstruction: expand the
banked archive paths from the preceding comparison, overlay final Simulation source,
recreate its hash-verified banked reporting driver, build Debug/Release, and use
`run-matched.ps1`. Normal validation commands are in the parent evidence recipe;
the new SAS failure must receive a payoff decision before any further attribution.

Disposable logical-byte inventory, no deletion attempted:

| Exact directory | Files | Bytes |
|---|---:|---:|
| `E:\NovaCore\.codex\contact-allocation-discriminator` | 697 | 71,133,883 |
| `E:\NovaCore\.codex\contact-payload-layout-control` | 352 | 37,160,094 |
| `E:\NovaCore\.codex\contact-payload-ownership` | 384 | 34,460,249 |
| Total | 1433 | 142,754,226 |

New disposable output: 384 files / 34,460,249 bytes, including pre-revision copies,
matched build/test output and ordinary text validation logs. All three exact roots
are ignored comparison output with no tracked files/reparse points. No source
assets or active runtime deployment is in them. Nothing disposed; previous blocked
automatic deletion was not retried. Project Control may manually clean after review:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\contact-allocation-discriminator', 'E:\NovaCore\.codex\contact-payload-layout-control', 'E:\NovaCore\.codex\contact-payload-ownership' -Recurse -Force
```

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

**CONTACT RESPONSE TRANSACTION CANDIDATE — FOLLOW-UP REQUIRED**

Proposed accomplishment: **Atomically commit linear and angular contact response**.
No milestone number. No staging, commit, merge, tag, push or banking.

**STOP FOR PROJECT CONTROL.**
