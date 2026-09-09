# M14.2 final orchestration failure investigation

This records the earlier unresolved checkpoint. The subsequent
[direct attribution](allocation-attribution.md) resolves the technical blocker;
manual diagnostic disposal remains before banking. Measurements below remain
historical evidence, not the current causal classification.

**M14.2 BANKING FOLLOW-UP REQUIRED.** Lead judgment: **ESCALATE TO PROJECT CONTROL**.
Failure decomposition: **A — ZERO-ALLOCATION CONTRACT**. Causal classification:
**INSUFFICIENT EVIDENCE**. This identifies the failed predicate in the reproduced
failure; it does not identify the allocating mechanism or retroactively recover
the operands of the original Debug failure.

## Baseline and scope

Branch `codex/spacecraft-translation`. HEAD, main, origin/main and peeled
`m14.1-canonical-surface-point-queries` remain
`5b9b02ab1d2ba0e903809510908e4819984d4de2`. All 19 accepted source/test SHA-256
fingerprints in `verification.json` matched before diagnosis. All 17 production
files and `SpacecraftTranslationTests.cs` still match. Only `Program.cs` differs
from those fingerprints, to expose operands and an isolated diagnostic entry.
Nothing was staged, committed, tagged, merged or pushed.

Host: Windows 10.0.26200 x64, SDK 10.0.303, Microsoft.NETCore.App 10.0.11.
No global runtime environment configuration was changed.

## What, how and bar

The old assertion combined allocation, completion and event count. The first
measurement closes `GC.GetAllocatedBytesForCurrentThread` immediately after the
existing advance call, then prints each operand. Formatting occurs outside the
allocation interval. The original composite assertion and its zero-byte threshold
remain intact. The new `--orchestration-only` entry selects the existing
`ClockExecutionTests` group; normal execution still runs all 33 groups.

Permanent contracts remain: warmed preallocated orchestration allocates zero
bytes, completes execution, and deterministically processes exactly 1,000 events.
No allocation tolerance, reduced count, best-effort completion, extra warmup or
production workaround was introduced. A future assertion split may improve
failure reporting, but cannot itself correct the demonstrated allocation failure.

Exact existing setup:

- Group: `Clock execution orchestration` (12th in normal suite order).
- Warmup: one Marker event, ID 90, time 1 tick, priority 0; one preallocated
  timeline/engine; advance from 0 to 2 ticks.
- Measurement: fresh timeline capacity 1,000 and engine history capacity 1,000.
  Schedule Marker IDs 1 through 1,000 at tick 1, priorities 1 through 1,000,
  in that sequence. These are marker events, not spacecraft-force events.
- One `AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(2))` call;
  default maximum event budget 10,000.

| Operand/state | Before | Reproduced failing Release result |
|---|---:|---:|
| Measured allocation | counter captured after construction | **8,160 bytes** |
| Completion | not executed | **Completed** |
| Processed events | 0 | **1,000**, expected **1,000** |
| Pending events | 1,000 | 0 |
| Marker value | 0 | 1,000 |
| State revision | 0 | 1,000 |
| Clock | 0 ticks | 2 ticks |

## Reproduction and bounded comparison

| Case | Debug | Release |
|---|---|---|
| Candidate isolated, fresh processes | 5/5 pass, zero bytes | 5/5 pass, zero bytes |
| Candidate initial full-suite repetitions | 3/3 pass, zero bytes | 2/3 pass; one allocation-only 8,160-byte failure |
| Candidate final consistency run | all 33 groups pass, zero bytes | all 33 groups pass, zero bytes |
| Banked M14.1 isolated, operand diagnostics only | 3/3 pass | 3/3 pass |
| Banked M14.1 full suite, operand diagnostics only | 1/1 pass | 4/4 pass |

All observed target calls completed and processed exactly 1,000 events. The
failure is intermittent and is not Debug-only. The original pre-diagnosis Debug
failure still has unknown operands. A later passing full suite does not erase
the reproduced failing Release run.

The comparison used an ignored disposable source tree extracted with `git archive`
from the banked commit, limited to Directory.Build.props, Core, EphemerisFormat,
Simulation and Simulation.Tests. No checkout, branch or candidate production file
was reverted. The baseline's passing sample is insufficient to classify the issue
as either candidate-caused or preexisting.

## Allocation localization, not a correction

A second disposable copy overlaid the 19 candidate files onto the baseline tree.
Only that private copy bracketed `EvaluateNext` and `ValidateAndCommit` with
thread-allocation counters and accumulated primitive numeric counters, without
formatting inside the interval. The prefix ran the normal first 12 groups.

Correctly reset target-only probes measured 1,000 calls. Representative failures:

- 8,160 bytes in commit, first affected event 77;
- 8,160 bytes in evaluation, first affected event 83;
- a later probe counted exactly one affected event, event 74, accounting for all
  8,160 bytes in evaluation. All three GC collection-count deltas were zero.

Thus attribution to a fixed NovaCore sub-operation did not survive repetition.
No GC collection does not mean no allocation. An early probe accidentally included
preceding groups in its phase totals; those totals were discarded, not used as
target attribution. That probe's outer target counter also observed one 3,296-byte
failure, so 8,160 is not asserted to be the only possible nonzero amount.

Process-local controls on the private candidate copy:

| Control | Target result |
|---|---|
| `DOTNET_TieredCompilation=0` | 5/5 zero-byte passes |
| `DOTNET_TieredPGO=0`, tiering otherwise enabled | 3/5 zero-byte passes; 2/5 failures, each 8,160 bytes |

These are diagnostic controls, **not an accepted test configuration or fix**.
The normal environment was restored after each control. Five additional private
baseline Release prefix probes with the same per-operation counters all passed.
A sampled GCAllocationTick EventListener in the private candidate copy did not
identify an 8,160-byte object. Sampling and instrumentation perturbation prevent
interpreting that absence as proof that no object was allocated.

Pinned .NET 10.0.11 source review found that the
[allocation API](https://github.com/dotnet/runtime/blob/v10.0.11/src/coreclr/vm/comutilnative.cpp)
reads allocation-context totals minus unused space. The reviewed native
[tiering worker](https://github.com/dotnet/runtime/blob/v10.0.11/src/coreclr/vm/tieredcompilation.cpp)
and [thread-creation path](https://github.com/dotnet/runtime/blob/v10.0.11/src/coreclr/vm/threads.cpp)
do not establish a managed allocation or counter adjustment accounting for this
delta. No matching runtime defect was established. Tiering association is evidence;
calling this a runtime defect, test-only problem or proven warmup defect would
overstate it.

## Candidate causal audit and verification

M14.2 adds force dispatch, transaction payload/state representation and force
history. Marker construction and canonical ordering, the orchestration loop,
completion semantics, clock advancement and event budget remain unchanged. Force
dispatch is bypassed for this workload. New force-history constructor allocations
occur before the measured interval. Larger value-type copies alone do not prove
managed heap allocation. No explicit new reference allocation was found on the
measured ordinary-marker path, but source inspection cannot clear the observed
runtime failure.

VERIFY A's strongest objection: M14.1 did not reproduce in the bounded sample, so
candidate-specific runtime interaction remains plausible. The exact allocating
object/call stack or counter mechanism is still missing. This blocks a test-only
classification and banking.

VERIFY B confirmed that the operand diagnostics preserve setup, original one-event
warmup, capacity, exact count and zero-byte bar; output begins after the counter
closes. The isolated entry cannot replace the failing full-suite path. Passing
repetitions and disabled-tiering runs are not substitutes for a causal correction.

## Validation and next boundary

`orchestration-final-gates.json` records the final consistency run:
Debug/Release full solution builds pass with zero warnings/errors; Simulation
passes all 33 groups in each configuration (34 PASS lines include a nested
translation summary); ReferenceFrames passes 11 groups each; Precision passes;
selected M14.1 queries pass 2/2, zero failures/skips each. Query output fingerprint
is unchanged: `3e08f86757588127099cbb100d3829b73d5e5f29b1de8d0f22ec83de28534912`.
Numerical translation oracle, deterministic replay, exact-time transactions and
frame coherence passed. No performance campaign was rerun.

**The complete sequence passed once, but the intermittent blocker remains open.**
No production correction is justified by the current evidence. No change to the
M14.2 physical authority was made. The next bounded responsibility is allocation
object/call-stack or allocation-context attribution across the observed tier
transition, with candidate/baseline comparison; not more blind pass repetitions,
arbitrary warmup, tolerances or translational architecture changes.

Results are consolidated here. Automatic approval review rejected the cleanup
command with "blocked by policy" and no further reason; no deletion occurred.
Pending disposable comparison copies are `.codex/m14-2-orchestration-baseline`
(330 files, 36,298,331 bytes) and `.codex/m14-2-orchestration-trace`
(253 files, 18,895,067 bytes), plus the temporary
`.codex/m14-2-orchestration-final-gates.json`. These copies contain only the bounded
comparison sources/builds described above and are not production dependencies.
Their contents can be regenerated from the banked commit, accepted candidate and
the probe recipe above. No raw profiler/GPU capture is retained. Original accepted
evidence remains unchanged. Stop for Project Control; do not bank.
