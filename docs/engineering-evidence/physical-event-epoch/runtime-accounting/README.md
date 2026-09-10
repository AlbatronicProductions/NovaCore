# Physical-event epoch: narrow runtime-accounting probe

**CANDIDATE-TRIGGERED CLR ALLOCATION-CONTEXT ACCOUNTING EFFECT**

Lead judgment: **PASS** for the authorized attribution question. The physical-event
epoch candidate remains **UNBANKED / FOLLOW-UP REQUIRED**. No production correction,
permanent test correction, no-GC migration, or banking was performed.

## Baseline

Branch: `codex/physical-event-epoch`. HEAD, main, origin/main and peeled M14.5:
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`.
SDK: `10.0.303`. Every child loaded
`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
M14.1–M14.5 tag targets remain unchanged. Starting candidate source/test and existing
evidence hashes are retained in `canonical-before.json`; all still match afterward.
The index remains empty.

## Proven structural chain

Candidate full-suite state → epoch group → Allocation() → its existing GC-region
entry/exit pair → later 8,160-byte servicing counter increase.

The earlier structural matrices established necessity under the tested full-suite
conditions. This probe directly explains the final counter movement. It does not
claim that PhysicalEventEpoch or servicing code allocated an object.

## Observer and comparison quality

Reused the retained M14.2 `allocation-attribution-profiler.cpp` (SHA-256
`E1116EEFC4DD832E3A4C823F3504C62771EF9AEAA0771D50F13B702C0D891DB7`).
One added include, `checkpoints.inc`, supplies bounded checkpoint/window storage and
deferred printing. The original exact ObjectAllocated callbacks, owner-thread
filter, BASIC_GC notifications and fail-closed native counter decoder are retained.
No new observer mechanism, stack profiler, EventPipe session, tiering/PGO override,
or GC-policy override was used. Existing JIT notifications were not analyzed.

Project Control explicitly authorized the same three child-only CORECLR observer
registration variables in both arms. Their values and common environment digest
are in `preflight.json`. No other profiler/runtime controls were present. Parent
environment and machine configuration were not modified.

The decoder accepted the actual 10.0.12 counter instructions. Both arms used
byte-identical compiled files, runtime configuration, observer and all production
dependencies. Only the omitted arm's command-line flag differed. That flag gates
only the epoch phase's existing GC-region entry/exit calls. Both arms retain the
same probe calls, original setup, warmup, workload, exact-zero assertions, full
37-group registration and common group order. No build or retry occurred during
the four-run matrix. A separate positive-control process ran first.

Preflight found 121 archived source files with CRLF/LF-only differences from the
canonical checkout. Normalized contents matched exactly; no production difference
was accepted. This was resolved before starting the matrix. The five accepted
candidate files preserve their canonical starting identities. Both comparison
arms use the same archived representation.

Perturbation limits: exact allocation callbacks can change runtime timing/codegen;
the C/D and G/H snapshot calls also add bounded diagnostic work inside the original
counter window. They introduced zero observed managed objects. Reports are deferred
until after H. These are attribution runs, not replacement performance measurements.
The unprofiled structural signal was reproduced exactly in both observed pairs.
No claim of zero observer CPU overhead is made. Buffers are bounded; no relevant
event/context buffer overflow occurred. The inherited multi-thread event recorder
is not a general-purpose lossless trace; coherent owner-thread endpoints and their
arithmetic are the primary evidence, supported by the suspension snapshots.

## Four-run matrix

| Sequence | Epoch GC region | Servicing bytes | Clock | Debt | Processed | Pending | Completed calls | Epoch bytes | Full suite |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| 1 | Normal | 8,160 | 50,000 | 0 | 5,000 | 0 | 499/499 | 0 | 9 pass; debt allocation assertion fails; 27 not reached |
| 2 | Omitted | 0 | 50,000 | 0 | 5,000 | 0 | 499/499 | 0 | 37/37 pass |
| 3 | Normal | 8,160 | 50,000 | 0 | 5,000 | 0 | 499/499 | 0 | 9 pass; debt allocation assertion fails; 27 not reached |
| 4 | Omitted | 0 | 50,000 | 0 | 5,000 | 0 | 499/499 | 0 | 37/37 pass |

All non-allocation servicing conditions remain exact. The epoch Allocation() phase
completed and measured zero in all four runs. Normal failures remain observable;
these results do not assert a green full banking suite.

## Normal and omitted accounting timelines

See [complete eight-checkpoint and exact pointer/context/event records](timelines.json)
for all four runs. The redundant rendered timeline was retired at closeout.

In both normal runs:

- A–F counter: 5,531,320; unused context: 3,024; context address/fields unchanged.
- Entry changes reported collection counts from 0/0/0 to 1/1/1. There is **no
  immediate context clearing at GC-region entry or exit** in these samples.
- At G, collection counts are 3/3/3 and actual observer GC callbacks are 2 started,
  1 finished. An earlier collection is still outstanding.
- During servicing, a GC_PREP suspension (reason 7) brackets clearing the measured
  owner's allocation pointer and limit. A collection finishes. At H, callbacks
  are 2/2 and collection counts remain 3/3/3.

In both omitted runs:

- A–F counter: 5,531,424; unused context: 2,920; counts remain 0/0/0.
- At G and H, counts are 2/2/2 and observer callbacks are already balanced 2/2.
- The allocation pointer, limit and cumulative fields remain unchanged; unused
  context stays 7,792. No suspension/completion occurs in the observed window.

The 104-byte A–F difference between arms exists before the omitted operation
(different argument/setup state). It is not subtracted or attributed to production.
The critical within-window accounting equality below holds independently in each
process. Approximate process-total counters include other threads and are not used
to establish owner-thread allocation.

Background-GC flag: **UNAVAILABLE**. Native GC implementation stack: **UNAVAILABLE**.
Region status is caller-observed successful entry/exit, not an independently read
CLR flag. We do not claim collection-count increments alone prove a completed GC.

## Positive managed-allocation control

Separate process: a warmed, non-inlined `new byte[128]` produced **one object,
152 object bytes, 152 counter bytes**, on owner OS thread 45884 / managed thread 2.
The allocation pointer advanced exactly 152; cumulative fields and limit stayed
unchanged. This result and the explicit control source preserve the decisive
positive witness; raw stdout/class IDs were retired at closeout. No
deliberate allocation was added to epoch or servicing workloads.

## Actual managed objects and arithmetic

Both epoch and servicing windows in all four runs: **zero managed objects and zero
object bytes on the measured owner thread**. There is no allocating object type or
managed allocation call stack for the observed 8,160-byte effect.

The verified native instruction sequence evaluates:

`counter = alloc_bytes + alloc_bytes_uoh - (alloc_limit - alloc_ptr)`.

| Field | Normal G, both runs | Normal H, both runs |
|---|---:|---:|
| alloc_bytes | 3,173,488 | 3,173,488 |
| alloc_bytes_uoh | 23,832,640 | 23,832,640 |
| unused = limit - ptr | 8,160 | 0 |
| counter | 26,997,968 | 27,006,128 |
| owner object count / bytes | 0 / 0 | 0 / 0 |

`3,173,488 + 23,832,640 - 8,160 = 26,997,968`

`3,173,488 + 23,832,640 - 0 = 27,006,128`

`27,006,128 - 26,997,968 = 8,160`.

Thus the exact reported increase equals the discarded unused allocation context.
The owner context storage address stays identical; its pointer and limit are
zeroed. The callback thread reading that owner context is distinct from the owner:
run 1 owner 20664 / suspension thread 45420; run 3 owner 46700 / suspension thread
29812. The same owner is measured before/after in each run.

Omitted controls both evaluate
`3,181,248 + 23,832,640 - 7,792 = 27,006,096` at both G and H: delta zero.
Every A–H sample was also checked against the decoded formula, not only G/H.

This identifies a CLR allocation-context accounting effect. It is **not a claim
of a CLR bug**, a new managed allocation, or a captured stack at the internal
context-retirement method. No additional runtime-source reconstruction is needed
to establish the measured accounting identity.

## Candidate production causality

The candidate's earlier test GC-region execution is the demonstrated stable trigger
in this suite. The immediate entry/exit samples do not clear the context; the
later GC completion clears it during servicing. The matched control lacks that
later state transition. Exact policy decisions between F and G were not traced.

No production allocation by PhysicalEventEpoch, PhysicalEventOrderKey,
SimulationEventHeaderComparer, or the servicing call tree is established. The
observed owner-thread workload creates no objects in these runs. This does not
claim that all code paths or all threads in NovaCore are allocation-free.

## VERIFY A: accounting red team

Strongest objection: the observer changes GC/JIT timing and could miss allocations
or merely correlate with a GC. The positive allocation is detected on the correct
thread; both normal runs reproduce exactly the unobserved structural result; both
omitted runs preserve context; the unchanged cumulative fields and disappearing
unused bytes exactly explain the delta. The owner endpoints agree with the
GC_PREP suspension/resumption snapshots. This is direct accounting evidence, not
an inference from the familiar byte magnitude. Classification **survived**.

Limits retained: no native retirement stack; no independently read background-GC
flag; approximate totals are not object evidence; no general observer-losslessness
or profiler-free performance claim. These do not invalidate the observed equality.

## VERIFY B: future measurement implication (recommendation only)

The permanent servicing contract is zero managed heap allocation by warmed
servicing work, alongside exact clock/debt/event/completion results. Its current
thread counter also includes the demonstrated unused-context retirement effect.
Recommend Project Control authorize a separate bounded evaluation of the already
qualified checked no-GC measurement architecture for this gate. Keep the epoch
test's valid GC boundary. Do not subtract 8,160 or relax exact zero.

Strongest objection: entering a no-GC region could merely move the problem or hide
real allocation. A region does not make managed allocations disappear: the revised
measurement must retain a deliberate allocation control inside its own boundary,
hard-fail unsuccessful entry/exit, and preserve all workload/warmup and exact
non-allocation assertions. The present positive control verifies this observer;
it does **not** validate an unimplemented future measurement wrapper. Recommendation
survived with that explicit future gate. **No migration was implemented.**

## Reproduction and retention

Use a disposable candidate copy of Core, EphemerisFormat, Simulation and
Simulation.Tests with their build props, preserving the candidate hashes and
37-group order. Reconstruct the pre-migration raw-counter fixture and A-H
hook placements using the [consolidated recipe](../RETENTION.md#reproduction-without-disposable-patches);
add the retained `AccountingProbe.cs` only to the disposable test project.
Production inputs must match the canonical candidate. The insertion patch is retired.

Copy the retained M14.2 profiler source, add `#include "checkpoints.inc"` immediately
before its Profiler class, and use this package's include. Reuse the pinned observer
headers described in the M14.2 report; exact input hashes are in
`observer-inputs.json`. Build MSVC x64 `/LD /EHsc /std:c++17 /O2`, `ole32.lib`, exporting
DllGetClassObject=ProfilerGetClassObject and DllCanUnloadNow=ProfilerCanUnloadNow.
Build disposable Simulation.Tests Debug once; copy byte-identical output to the
normal and omitted directories before any comparison.

Set only the authorized child registration values recorded in preflight; use the
same observer DLL path in both arms. First run `--observer-positive-control` and
require one object/152 bytes. Then run fresh processes exactly N, G-, N, G-;
G- adds only `--omit-epoch-gc-region`. Use E:\NovaCore working directory, no builds,
retries, tiering changes or other runtime flags between runs. Fail closed if the
decoder, runtime identity, epoch zero gate, non-allocation contracts or unrelated
suite results fail. Reporting is after H, before the original servicing assertion.

Retained: this report (including positive/restoration results), exact A–H timeline
records, matrix/preflight/hashes and the two minimal final observer adapters.
Disposable insertion patches, raw positive stdout and duplicate restoration captures are retired. Bulk build output,
raw full-suite stdout/stderr and the disposable source tree remain scratch pending
one consolidated Project Control cleanup. No deletion was attempted.

## Git and stop

Canonical production/permanent-test fingerprints and all starting evidence hashes
are unchanged. Disposable Program.cs and epoch test were restored byte-for-byte;
the added helper was moved outside their project. Diagnostic binaries remain only
in scratch. Index empty; same branch and refs. Only this concise evidence package
was added. No production edits, permanent test changes, staging, commit, tag,
merge, push, banking, contact discovery or event-local execution.

**STOP FOR PROJECT CONTROL.**
