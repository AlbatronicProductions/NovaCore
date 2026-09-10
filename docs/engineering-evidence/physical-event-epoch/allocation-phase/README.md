# Epoch Allocation() phase discriminator

2026-09-09 EDT; six runs began at 2026-09-10 01:04:28 UTC.

**EPOCH ALLOCATION TEST PHASE IS A NECESSARY STABLE TRIGGER**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Normal: **8160 / 8160 / 8160 bytes**. Only `Allocation()` invocation omitted:
**0 / 0 / 0 bytes**. Every non-allocation servicing operand passed. This meets
decision A and ends the ticket before profiling or runtime-accounting attribution.
Candidate acceptance remains uncleared; no production/test correction is proposed
as already justified by this result.

## Baseline

HEAD/main/origin/main remain banked M14.5
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, tag
`m14.5-isolated-analytical-contact-response`. Working branch:
`codex/physical-event-epoch`. All M14.1–M14.5 tag targets are unchanged.
All five accepted production/test fingerprints match the [candidate report](../README.md).
All 21 pre-existing candidate, documentation and evidence files also matched their
starting hashes after the comparison. Only new concise evidence was added here.

SDK 10.0.303; each process loaded .NET 10.0.12 from
`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.

The preceding [whole-group structural control](../structural-omission/README.md)
reported normal 8160/8160/8160 and whole-group omission 0/0/0. This experiment
retains the epoch group and all its other phases.

## Phase audit, before execution

`PhysicalEventEpochTests.Run()` calls four phases in the following order. Replay
and failure controls are nested within those phases, not separate reordered calls.

| Order / phase | Production types exercised | Allocation expectation | Plausible process-state effects, not attributed causes |
| --- | --- | --- | --- |
| 1. Normalization | PhysicalEventEpoch construction, canonical conversion, comparison, equality/hash | Test arrays, Random and BigInteger oracle work allocate; no phase-wide zero-byte contract. Deliberate overflow paths can construct handled exceptions. | Arithmetic/generic/type compilation, test allocation occupancy, failure-path execution, console formatting |
| 1a. Overflow/failure controls within Normalization | Invalid denominator, capacity and checked tick/intermediate failures | Same setup regime; expected failures remain exact | Exception/runtime paths and numerical helpers |
| 2. Ordering | PhysicalEventEpoch, PhysicalEventOrderKey, integral header comparer | Arrays, List, BigInteger, Random and LINQ replay checks allocate; no phase-wide zero-byte contract | Generic sorting/comparison compilation, test allocation and formatter state |
| 2a. Replay/shuffle within Ordering | Eight-key interleaving/ties across 50 seeded shuffles, then 2304 integral comparisons | Ordinary test allocations permitted | Same phase state; exact order and canonical comparator coverage retained |
| 3. Serialization | Epoch binary encode/decode, malformed/unsupported-kind rejection | Stack buffers plus test arrays and hex-string creation; no phase-wide zero-byte contract | Binary primitive/type paths, small setup objects; no global serializer registration |
| 4. Allocation setup/warmup | 128-key Workload, compare/construct/sort Batch modes | Source and scratch arrays allocated before measurement; existing 4096 operations per warmup mode retained | Allocation occupancy, method/generic warm state |
| 4a. Allocation measured section | Same three Batch modes, 1024 iterations each, plus 1024 encode/decode iterations | Exact zero calling-thread bytes required inside existing test-only accounting region | Existing region entry/exit and execution may alter later process state; mechanism unknown |
| 4b. Allocation teardown/assertion/output | Finally-protected region exit; exact-zero assertion; result line | Formatting occurs after the phase counter closes | Existing GC-region lifecycle and output/state effects; no causality assigned |

No additional group-wide setup/teardown is invoked by Run. The static sink exists
already; there is no explicit test-class static constructor. Performance() is a
separate command path and is not used in this full-suite experiment.

## Disposable invocation control

No existing phase-selection hook was available. The disposable copy adds one
Boolean field, sets it once from `--omit-epoch-allocation` at runner startup, and
guards only the final `Allocation()` invocation in Run. The other three phase
bodies and their order are byte-equivalent after removing that guard/field.
This describes the full invocation-only change; the disposable patch was retired.
The [minimal operand reporter recipe](../RETENTION.md#reproduction-without-disposable-patches)
is identical in both arms and remains outside the unchanged servicing allocation window.

One Debug build, zero warnings/errors, produced both launch configurations by
copying the identical output into normal/omitted directories before execution.
Both use the same test and production binaries, all 37 registered groups and
identical runtimeconfig bytes. No build occurred during the matrix.

- Both test DLLs: `ECA45A45628754F47F44B33ED86DB5B79F511D45166AAC0E8192DBF3D7F4BE45`.
- Both Simulation DLLs: `419782FB2903C5A62BD1B9BCDF04B3C5AE30AFE36208F98F23EA8ED734C2937D`.
- Environment SHA-256: `B204C3C0C4F4BF5BD52F35DF5890EEC985DE607F7EF68BC4B17E8E5000BFE336`.
- Reporter block SHA-256: `5722E8A1F82D1B453110D6179D1B813B7DE7D165E62F89B6E7AB42C8A050BBC2`.

Every process received the same cloned environment and working directory
`E:\NovaCore`. Only the intentional phase flag and launch-directory path differed.
No profiler, EventPipe, callbacks, GC/JIT trace, runtime controls, added warmup or
new no-GC region was used. The original epoch Allocation() region naturally executes
in the normal arm and is omitted with the phase in the control. No no-GC boundary
was introduced around the later servicing gate.

## Exact six-run matrix

| Sequence | Epoch Allocation() phase | Allocation bytes | Clock | Debt | Processed | Pending | Completed |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 2 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 3 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 4 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 5 | Normal | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 6 | Omitted | 0 | 50000 | 0 | 5000 | 0 | 499 |

All clock, debt, processed/pending count and completion contracts passed. Initial
measured-window state was clock 100, debt 0, processed 10, pending 4990; measured
thread ID 2 throughout. Every normal run passed nine groups, failed only the target
allocation assertion and did not reach 27 groups. Every omission run completed all
37 registered groups, with only the epoch Allocation() phase deliberately excluded.
Those omission passes are controls, not full permanent-candidate acceptance.

Fresh PIDs: 45940, 39760, 39644, 46592, 37332, 27164. The phase-output line was
present exactly once in normal runs and absent in omission runs; the full epoch
group's PASS line was present in every run. Exactly six processes in declared
N → A− → N → A− → N → A− order. No retries or unrelated failures.
Per-run operands/status/runtime identity: [matrix.json](matrix.json).
Orders and build/environment identity: [preflight.json](preflight.json).

## Causality, verification and next decision

Omitting only this phase removes the stable later counter signal in three matched
pairs while candidate production and all other epoch phases remain present. This
narrows the structural trigger from the whole group to its Allocation() phase.
It does not identify which activity inside that phase matters, establish managed
object allocation, or prove a test/production/CLR defect.

The shared binary avoids separate codegen between arms. The startup Boolean and
argument presence still alter runner state; skipping the phase intentionally alters
allocation occupancy, warm execution and lifecycle history together. Normal runs
reproduced the signal despite the guard. The selected classification is the ticket's
necessary-stable-trigger result for this bounded experiment, not a universal causal
claim about every runtime or machine.

Recommended next action, subject to Project Control: **one disposable discriminator
of the phase's existing GC-region entry/exit lifecycle**, retaining its array setup,
warmup, measured workload, output and exact-zero checks while comparing that existing
pair enabled versus omitted. This would test a narrower state-changing responsibility
without adding no-GC to servicing. Any earlier zero-byte or other contract failure
must remain visible and stop the control. This recommendation is not a production
fix, permanent measurement migration or assertion of runtime-accounting causality.
It was not executed here. No profiler or further phase experiment followed decision A.

## Restoration and evidence lifecycle

Both disposable Program.cs and PhysicalEventEpochTests.cs were restored to the exact
canonical candidate bytes after the matrix. Diagnostic binaries remain explicitly
disposable; they were not deployed as the normal candidate. Canonical files were
never patched or rebuilt. The temporary runner refuses a second matrix invocation.

Retain this report, preflight.json and matrix.json; the invocation recipe above replaces the disposable phase patch. New
disposable `.codex/physical-epoch-allocation-phase`: **307 files / 30,092,394 bytes**.
Previous disposable output remains unchanged: **1,217 files / 111,079,692 bytes**.
Combined: **1,524 files / 141,172,086 bytes**. No cleanup/deletion or retry of blocked
cleanup occurred; consolidated manual cleanup remains deferred for Project Control.

Final git diff --check: PASS, line-ending warnings only. Nothing staged, committed,
tagged, merged, pushed, banked or assigned a milestone number.
