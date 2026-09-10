# Epoch Allocation() GC-region structural discriminator

2026-09-09 EDT; six runs began at 2026-09-10 01:23:04 UTC.

**EPOCH ALLOCATION GC-REGION EXECUTION IS A NECESSARY STABLE TRIGGER**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Normal GC-region pair: **8160 / 8160 / 8160 servicing bytes**.
Only that pair omitted: **0 / 0 / 0 servicing bytes**.
The epoch Allocation() phase itself completed and measured **zero bytes in every
run**. All non-allocation servicing conditions passed. This meets decision A;
no runtime-accounting probe, profiler or permanent correction followed.

## Baseline

HEAD/main/origin/main remain banked M14.5
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, tag
`m14.5-isolated-analytical-contact-response`. Branch:
`codex/physical-event-epoch`. M14.1–M14.5 tags remain unchanged.
All five accepted production/test fingerprints match the [candidate report](../README.md).
All 25 pre-existing candidate/documentation/evidence files matched their starting
hashes after this experiment. Only this new evidence package was added.

SDK **10.0.303**. Every process loaded .NET **10.0.12** at
`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
The prior [phase discriminator](../allocation-phase/README.md) established normal
8160/8160/8160 versus Allocation() omitted 0/0/0. Here the entire phase executes
in both arms; only its existing GC-region entry/exit calls are conditional.

## Allocation() audit in exact execution order

The window below is the epoch phase's own allocation measurement, not the later
long-duration servicing measurement.

| Substep | Touches production code? | May allocate? | May affect GC/allocation-context state? | Window position |
| --- | --- | --- | --- | --- |
| Workload() creates 128 keys; scratch array setup | Yes: exact epoch/key construction | Two test arrays explicitly allocate | Setup changes allocation occupancy | Before |
| Existing warmup: three Batch modes, 4096 iterations each | Yes: comparison, construction and sorting | Outside zero-byte contract; runtime preparation is not independently measured | May warm generic/JIT paths and alter runtime state | Before |
| TryStartNoGCRegion(1 << 20, disallowFullBlockingGC: true), require success | Runtime API, not NovaCore production | Runtime mechanism not attributed by this experiment | Explicit GC-region state transition | Before counter open |
| GetAllocatedBytesForCurrentThread baseline | No production call | No allocation deliberately added | Reads runtime accounting; side effects not investigated | Opens window |
| Three Batch modes, 1024 iterations each | Yes | Required to measure exactly zero bytes | Work executes in the selected runtime state | Inside |
| Stack buffer and 1024 encode/decode iterations | Yes: epoch serialization | Stack buffer; managed allocation forbidden by phase contract | Serialization/runtime paths execute; no mechanism attribution | Inside |
| GetAllocatedBytesForCurrentThread minus baseline | No production call | No allocation deliberately added | Reads runtime accounting | Closes window |
| Finally-protected EndNoGCRegion() | Runtime API | Runtime mechanism not attributed | Explicit exit transition; normal arm only | After |
| Require allocated == 0 | Test assertion | Failure exception only if contract fails; message formatting occurs after counter | Potential test/runtime state changes outside window | After |
| Console result line | Test reporting | Formatting may allocate | May affect later process occupancy/state | After |

There is no separate disposal phase beyond region exit. Setup, warmup, all measured
operations, exact-zero assertion and result formatting were preserved. Their
possible state effects were listed before the matrix; none was inferred as the
actual allocation mechanism from static possibility alone.

## Disposable control design

The disposable test copy adds one Boolean set at runner startup from
`--omit-epoch-gc-region`. It guards only the existing entry call/entry-success check
and the matching exit in finally. No new GC API or region was added. In the normal
arm the original entry success requirement is enforced; the omission arm does not
claim to have entered a region. All other phase assertions remain identical.

The guard is outside the measured phase window. The phase and the full epoch group
still run in both arms; no test is reordered. The later servicing reporter and
assertion remain unchanged, including zero allocation and exact counts/time.
Removing the temporary field/guards reproduced the original epoch test source.
Removing the startup assignment reproduced the qualified reporting runner.

A single Debug build passed with zero warnings/errors and supplied both disposable
launch directories with identical files. Both configurations were ready before
the matrix; there was no compilation between runs.

- Both test DLLs: `17DC890607F615754407A454A61D23DDE2BF2CA4CACEF6065CB242EFBCD34467`.
- Both Simulation DLLs: `A590F930027C0312488A7FD1C5D760122584612A06FE4C4DF203C81F55046FEA`.
- Identical runtimeconfig SHA-256:
  `256DDA2706E1151BB95987B6B5BE610BF68D64E058285B19E7E958222FC90414`.
- Identical cloned environment SHA-256:
  `B204C3C0C4F4BF5BD52F35DF5890EEC985DE607F7EF68BC4B17E8E5000BFE336`.

Same dotnet executable, `E:\NovaCore` working directory and 37-group order.
The intentional flag and launch-directory path distinguish the arms. No profiler,
EventPipe, callbacks, trace, accounting probe, tiering/runtime configuration change
or new measurement boundary was used. Loaded CLR identity was checked externally
from process modules, as in the preceding controls, without debugger attachment.

## Exact six-run matrix

| Sequence | Epoch GC region | Servicing allocation | Clock | Debt | Processed | Pending | Completed |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 2 | Entry/exit omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 3 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 4 | Entry/exit omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 5 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 6 | Entry/exit omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |

Fresh PIDs: 4128, 26608, 46832, 41860, 37188, 24288.
All six epoch Allocation() invocations completed and reported zero bytes, with
their exact-zero assertion passing. All six full epoch groups passed. Initial
servicing-window state was clock 100, debt 0, processed 10, pending 4990; measured
thread ID 2 throughout. Every non-allocation servicing operand passed exactly.

Each normal run passed nine groups, failed only the servicing allocation assertion
and did not reach the remaining 27. Each omitted-pair run completed all 37 groups.
These diagnostic omission passes do not clear the unchanged normal candidate.
Exactly six fresh processes in N → G− → N → G− → N → G− order. No retry, build,
runtime mismatch or unrelated failure interrupted the matrix.

Full operands, epoch-phase completion/zero-byte results, exit codes and timestamps:
[matrix.json](matrix.json). Runtime/environment/order/binary identities:
[preflight.json](preflight.json).

## Interpretation, verification and next action

The later signal follows execution of the existing GC-region pair when the phase's
setup, warmup, workload, counter reads, assertions and output remain present. This
narrows the structural trigger one level deeper than phase omission. It does not
show which of entry/exit matters, whether managed objects were allocated later,
or which allocation-context bookkeeping mechanism produced 8160.

The shared binary controls a separate-arm compilation difference. Startup argument
occupancy and guard control flow remain acknowledged confounds, while the intended
GC-state transition itself is necessarily different. Normal runs reproduced the
previous signal under this guarded runner. The ticket's necessary-stable-trigger
classification holds for these three pairs; a universal causal or production-defect
claim would exceed the evidence. Counter-zero observations are not allocation
callback/object-count evidence.

**Stop for Project Control.** Recommended next action is one narrowly scoped
runtime-accounting probe, if authorized, to compare per-thread allocation-context
state around the existing epoch region transition and the later servicing window.
It should seek a quantitative relationship between context state, counter movement
and actual managed allocation, rather than infer mechanism from the familiar byte
count. No such probe or permanent region removal/measurement migration was performed.

## Restoration, storage and Git

The startup/entry/exit guards described above and the consolidated
[reporting recipe](../RETENTION.md#reproduction-without-disposable-patches)
preserve the control without its disposable patch.
Both disposable Program.cs and PhysicalEventEpochTests.cs were restored to their
exact canonical bytes after execution. Diagnostic binaries remain isolated output,
not a normal runtime deployment. Canonical production/tests were never edited.

Retain this report, preflight.json and matrix.json; the invocation recipe replaces the disposable patch.
New disposable `.codex/physical-epoch-gc-region`: **307 files / 30,092,806 bytes**.
Previous disposable total remains **1,524 files / 141,172,086 bytes**.
Combined disposable total: **1,831 files / 171,264,892 bytes**.
No deletion or retry of blocked cleanup occurred. Consolidated manual cleanup
remains deferred until Project Control accepts the technical result.

Final git diff --check: PASS, line-ending warnings only. Nothing staged, committed,
tagged, merged, pushed, banked or assigned a milestone number.
