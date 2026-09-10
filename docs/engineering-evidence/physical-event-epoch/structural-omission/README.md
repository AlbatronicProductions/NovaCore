# Epoch-group execution structural discriminator

2026-09-09 EDT; execution began 2026-09-10 00:57:56 UTC.

**EPOCH TEST-GROUP EXECUTION IS A NECESSARY STABLE TRIGGER**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Normal candidate: **8,160 / 8,160 / 8,160 bytes**.
Same candidate with only epoch-group invocation omitted: **0 / 0 / 0 bytes**.
This meets the ticket's structural decision A. Stop before allocation profiling.
It does not identify the allocation mechanism or establish a defective test or
production method. The candidate remains unbanked and acceptance is not cleared.

## Baseline and previous evidence

HEAD/main/origin/main remain banked M14.5
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, tag
`m14.5-isolated-analytical-contact-response`. Branch:
`codex/physical-event-epoch`. M14.1–M14.5 tag targets remain unchanged.
All five accepted source/test fingerprints match the [candidate report](../README.md).
All 17 pre-existing candidate/documentation/evidence files were checked against
their starting hashes after the experiment and remained unchanged.

The preceding [matched A/B](../matched-full-suite-ab/README.md) established baseline
0/0/0 versus candidate 8160/8160/8160. This experiment did not run or revert to
baseline production; both arms contain the complete candidate production code.

## Static reachability and order audit

Baseline order before the target is SimulationInstant → SimulationDuration →
SimulationRate → Solar UTC/speed presets → Event ordering → Timeline topology →
Simulation clock → Host-duration conversion → Host-duration debt servicing.
The candidate inserts Internal physical-event epochs at position 6, shifting
debt servicing to position 10. All common groups preserve relative order.
The complete baseline/candidate lists are in the preceding A/B preflight manifest;
this experiment's exact normal/omitted lists are in [preflight.json](preflight.json).

The epoch group calls `Normalization`, `Ordering`, `Serialization`, then
`Allocation`. It executes test-only BigInteger controls, array construction,
shuffling/sorting, binary round trips, output formatting and an existing bounded
no-GC allocation check with finally-protected exit. These operations can change
process state before a later gate; possibility is not mechanism attribution.

| Candidate component | Directly used by servicing workload? | Executed earlier in suite? | Can affect process/JIT/GC/type state? | Source evidence |
| --- | --- | --- | --- | --- |
| SimulationEventHeaderComparer | Yes, via pending-event heap; equal-time helper only after equal canonical time | Yes, common Event ordering and timeline work | Method/codegen path can differ after extraction; no added allocation expression | Timeline/SimulationEventHeap.cs Compare; SimulationEventHeaderComparer.Compare/CompareEqualTime |
| PhysicalEventEpoch | No direct debt-service reference | Yes, group 6 | Method compilation and callers' test/runtime work; no explicit static constructor | Time/PhysicalEventEpoch.cs; epoch tests Normalization/Ordering/Serialization/Allocation |
| PhysicalEventOrderKey | No direct debt-service reference | Yes, group 6 | Generic sorting/comparison paths can be initialized/exercised; no explicit static constructor | Timeline/PhysicalEventOrderKey.cs; epoch tests Ordering/Workload/Batch |
| PhysicalEventEpochTests | Not called by servicing | Yes, group 6 | Setup allocations, oracle work, existing accounting region, formatting and compilation | Tests Run calls the four phases listed above |
| Group registration/omission branch | Outside servicing | Yes, runner startup and group dispatch | Delegate setup and control flow; flag/argument presence is an explicit confound | Program.cs test array and foreach loop |

The new production types have no explicit static constructor, registration or
global serializer hook. The epoch test class has a zero-initialized static integer
sink, with no explicit type constructor. Compiler/runtime initialization of
delegates and generic machinery is not claimed absent. Live servicing remains
canonical clock → execution orchestrator → canonical transaction groups/pending
timeline → event heap → integral header comparer. No rational event is scheduled.

## Structural control and preflight

One disposable source tree was populated with the accepted candidate. The existing
minimal operand reporter was reused unchanged. There was no existing full-suite
group filter, so a minimal disposable runner patch introduced one startup Boolean
from `--omit-epoch-group` and a conditional `continue` only for the named epoch
group. Its delegate remains registered in the test array in both arms. There is
no omission logging inside the measured window and no change to the debt workload,
warmup, exact thresholds, per-call completion checks or epoch test source.

Both arm configurations use **the same compiled Debug runner and production
binaries**, copied into normal/omitted launch directories before execution.
A single successful Debug build supplies both; zero warnings/errors. This avoids
separately compiling the arms. No builds occurred during the matrix.

- Runner DLL SHA-256, both arms:
  `27ACA6F312872C6047E5A961FE8BFFF003B85320EE2B4F3BBDE3E3EA542F068E`.
- Production Simulation DLL SHA-256, both arms:
  `A1075BD371D04EC5F8A36704B5A9AAA9955F2EAB5020B851DCF2AB40EB25597A`.
- Reporter block SHA-256:
  `5722E8A1F82D1B453110D6179D1B813B7DE7D165E62F89B6E7AB42C8A050BBC2`.
- SDK 10.0.303; all six processes loaded .NET 10.0.12 from
  `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
- Same working directory `E:\NovaCore`; same cloned environment, digest
  `B204C3C0C4F4BF5BD52F35DF5890EEC985DE607F7EF68BC4B17E8E5000BFE336`.
- Runtimeconfig bytes identical; no profiler/runtime-tuning environment variables.

The only intentional execution difference was the omission flag and its effect
on invoking the epoch group. Target servicing had no new no-GC boundary. Existing
epoch test source was untouched: its own accounting region executes when that
group runs and is naturally absent when the whole group is omitted.

## Exact six-run result

| Sequence | Epoch group | Allocation | Clock | Debt | Processed | Pending | Completed |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 2 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 3 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 4 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 5 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 6 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |

Every non-allocation operand passed exactly. Before the measured loop, all runs
had clock 100, debt 0, processed 10 and pending 4990. Measured thread ID was 2.
Each normal run: nine groups passed, target allocation assertion failed, 27 groups
not reached. Each omitted run: all 36 common groups passed, no other failure.
The omitted epoch group is an intentional diagnostic control, not a permanent
skip or an accepted full-candidate suite pass.

Fresh process IDs, in order: 3596, 26620, 40488, 46672, 34016, 40324.
All operands, exit codes, group counts, timestamps, environment digest and loaded
CLR identity are retained in [matrix.json](matrix.json). Exactly six processes;
no retry, no baseline run, no unrelated failure or environment mismatch.

## Verification, limits and next action

The strongest challenge is that omission changes process history by design, and
the startup flag, argument occupancy and skipped branch can themselves influence
runtime state. A shared binary removes a separate-build/codegen difference between
arms, but cannot make omission observationally neutral. Normal execution reproduced
the prior stable signal despite the shared runner patch. With the same production
assembly, omitting the epoch invocation removed it in all three controls.

The classification is therefore the ticket's **necessary stable trigger** result
within this bounded structural experiment. It is not a universal necessity theorem,
proof that the epoch tests are wrong, or proof of a production managed allocation.
The test group may legitimately establish JIT, GC, allocation-context or other
state that the later measurement encounters. None of those mechanisms was observed.

No allocation observer, runtime-accounting probe, production component isolation
or measurement migration was executed. The explicit decision-A stop rule applies.

Recommended next action, subject to Project Control approval: **one disposable
phase-level process-state discriminator for the existing epoch `Allocation()`
phase**, preserving Normalization/Ordering/Serialization and all production code,
and comparing the later unchanged debt counter with that phase executed versus
omitted. This tests a narrower trigger; it must not introduce no-GC around debt
servicing, weaken assertions or imply a mechanism from the 8,160-byte magnitude.
No such phase experiment was performed in this ticket.

## Restoration, evidence and Git

The two runner-only edits described above and the consolidated
[operand reporting recipe](../RETENTION.md#reproduction-without-disposable-patches)
preserve reproduction without the disposable runner patch.
Both reporting and omission edits were removed from the new disposable source
Program.cs after the runs; its bytes now match the canonical candidate exactly.
Instrumented arm binaries remain classified diagnostic output, not active runtime
deployment. No canonical source/test file was edited or rebuilt in this ticket.

Retain this report, preflight.json and matrix.json; the described control replaces the disposable omission patch.
Disposable comparison directory `.codex/physical-epoch-structural`:
**306 files / 30,074,202 logical bytes** after source restoration.
Existing directories remain unchanged: `.codex/physical-epoch-gate`
361 files / 38,126,642 bytes; `.codex/physical-epoch-matched-ab`
550 files / 42,878,848 bytes. Combined disposable total:
**1,217 files / 111,079,692 bytes**. No deletion or retry of blocked cleanup occurred.
Consolidated manual cleanup remains deferred until Project Control accepts the
technical result.

Final `git diff --check`: PASS, line-ending warnings only. Nothing staged,
committed, tagged, merged, pushed, banked or assigned a milestone number.
