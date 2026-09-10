# Exact-event spacecraft motion: bounded allocation attribution

**Final attribution classification: ATTRIBUTION INSUFFICIENT**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

The two authorized attributed candidate Release processes completed without the historical 12,336-byte counter failure. The observer positively detected a real allocation control, but no target event was available to attribute. These are profiled observations, not replacement contract/performance measurements. The candidate remains unbanked and Release allocation qualification is not cleared.

## Baseline

- HEAD / main / origin/main: `00f226096b61a117184464b5619e464d39789574`.
- Branch: `codex/exact-event-spacecraft-motion`.
- M14.6 annotated tag target: `m14.6-physical-event-epochs` -> `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- M14.1–M14.6 targets match the preceding preflight; exact targets in `result.json`.
- SDK 10.0.303; actual observed child runtime 10.0.12, Windows x64.
- All 14 accepted changed/untracked canonical file hashes match. The candidate source closure is 167 files, including 156 source/project files under src. This does not claim a new 210-file audit.

## Why attribution was justified

The preceding unprofiled matched Release matrix retained three baseline full-suite passes with no target failures. Candidate results were attitude 0 then impulse-evaluation 12,336; attitude 12,336; attitude 12,336. A separate historical SAS witness was also 12,336. This supports process-level attribution rather than another gate matrix; equal magnitude does not establish one shared cause.

Previous evidence remains in sibling directories `exact-event-attitude-ab-20260910` and `exact-event-sas-ab-20260910`. It was not changed or rerun.

## Static candidate-diff / initialization audit

See `static-audit.md` for every changed component, canonical versus rational-only reachability, registration, static fields and limitations. Key findings:

- Extracted canonical translation and attitude kernels are reachable before historical failures.
- The added rational rotation/motion entry points and exact-duration helper were not demonstrated to execute before them.
- New result/state arithmetic uses value types; no explicit reference-object allocator was identified in the successful changed numerical paths.
- The new test delegate is registered before groups, but its body runs last. Its static readonly fixtures are value types; BeforeFieldInit permits initialization timing that was not dynamically observed.
- Per-method load/JIT/static-initialization timing is UNAVAILABLE. Source reachability neither proves allocation nor exonerates candidate code.

## Observer and limits

Reused the retained NovaCore SAS/M14.2 exact allocation callback and stack-snapshot machinery. No new observer mechanism, EventPipe, JIT trace, GC trace or runtime-context decoder was introduced. The native observer records current-owner-thread objects, sizes, order/timestamps and up to 24 managed frames per object into a fixed 256-object buffer. Approximately 13 MiB of native static storage is reserved; no overflow occurred.

Microsoft documents ObjectAllocated as a managed heap object notification and supports a synchronous current-thread stack snapshot from that callback. See [ObjectAllocated](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-objectallocated-method) and [DoStackSnapshot](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo2-dostacksnapshot-method).

Only disposable caller copies changed: Program.cs, ContactResponseTests.cs, SpacecraftTranslationTests.cs and SpacecraftPhysicalEventMotionTests.cs. All 32 ordinary gates received hooks. OrdinaryAllocationMeasurement remained byte-identical. Entry, counter, workload, warmup, assertions and exit were not rewritten. No production source changed.

Phase 1 starts after the unchanged helper opens its counter and ends at a marker immediately before Complete. Phase 2 contains completion/counter-close/exit/reporting and cannot automatically be counted as workload allocation. The post-open/pre-hook margin is not observed. This is not an instruction-exact counter-boundary observer.

An initial separate control detected the object correctly, but deferred metadata lookup was rejected with `CORPROF_E_UNSUPPORTED_CALL_SEQUENCE`. Before any full candidate execution, metadata resolution was moved into the supported native callback context, retaining deferred output. The qualified control resolved names and stacks. This was an observer technical correction, not a failed candidate rerun.

Preparation exposed two initial 60-byte diagnostic bridge string allocations outside contract windows. Bridges were prepared before the suite; no extra production warmup or GC regions were added. Callback enablement, native metadata work, synchronous stacks and added caller instructions can change codegen/timing/process history. The disappearance of the failure is consistent with perturbation, but does not prove its cause.

Only child processes received CORECLR_ENABLE_PROFILING, CORECLR_PROFILER and CORECLR_PROFILER_PATH. No tiering/PGO/GC policy overrides. Inherited DOTNET_ROOT paths retained. Managed disposable Release build passed with zero warnings/errors. Native linking emitted only the two inherited COM export PRIVATE advisories. No build occurred between candidate runs.

## Positive control

Separate qualified process, OS thread **49084**, CLR thread **2223966718576**, managed thread **2**:

- Object: `System.Byte[]`, length 128.
- Phase-1 count: **1**.
- Object size: **152 bytes**.
- Counter delta: **152 bytes**.
- Stack: `AllocationProbe.AllocateControl -> AllocationProbe.PositiveControl -> Program.Main`.
- Size and stack operations: S_OK; no overflow; checked entry/exit PASS.

Existing helper reporting allocated separately in phase 2 and was not subtracted from or added to the 152-byte result. The original permanent positive control also reported 152 in both full suites.

## Attributed candidate runs

| Run | Full groups | Zero-required ordinary windows | Counter deltas | Checked entry/exit | First 12,336 event | OS thread |
|---|---:|---:|---|---|---|---:|
| Candidate 1 | 38/38 PASS | 32 | All 0 | All PASS | Not observed | 51436 |
| Candidate 2 | 38/38 PASS | 32 | All 0 | All PASS | Not observed | 13416 |

Fresh processes, same source/binaries/runtime/settings. Run 2 was executed only because run 1 completed without the historical effect. Maximum two reached; no further candidate execution. No baseline attributed control: there was no captured target mechanism for a baseline to distinguish, so another process had no justified attribution payoff.

### Incidental object observation—do not omit

Both runs recorded one phase-1 **System.String, 108 object bytes**, during `body-catalog`, while its checked counter delta was **0**. Stack: `Program.CelestialBodyCatalogTests -> Program.Main -> unmanaged`; size and stack queries succeeded, same owner thread. CLR thread IDs were 2267965717792 and 1838421062720 respectively. This discrepancy is **unresolved**. No string contents, allocation-context analysis or new baseline probe was added to investigate it. It does not identify the 12,336 event and cannot be silently treated as zero observed objects.

All other ordinary gate phase-1 captures reported zero objects/bytes. Helper reporting objects remain separated in phase 2. The earlier commentary saying every window observed zero objects was corrected after this detailed record audit.

## First 12,336-byte event / arithmetic

No such event occurred in either attributed process. Target object type/count/size, target call stack/thread/timestamp, first-use trigger and candidate owner are **UNAVAILABLE**.

The positive-control identity is exact: **152 observed object bytes = 152 counter bytes**. The incidental catalog observation is **108 observed object bytes versus 0 counter bytes**; no accounting explanation is asserted. There is no observed sum explaining 12,336. No reuse of the older 8,160-byte mechanism, no subtraction, tolerance or known-byte exception.

## Migrating-gate hypothesis

**UNRESOLVED.** No target allocation or lazy-initialization trigger was captured. The historical pattern remains compatible with several explanations, including instrumentation-sensitive runtime behavior. Neither a one-time candidate initializer nor a common SAS/attitude/impulse allocator is proven. Historical SAS cause remains unresolved, not cleared by these runs.

## Candidate causality and ownership

No candidate production allocator, test registration allocator or lazy lifecycle owner was identified for 12,336. No production defect is proven or cleared. The exact-event read-only coherent-motion responsibility remains provisionally accepted independently of this unresolved Release gate.

No cache/preinitialization/warmup/kernel/rotation/helper change is justified by these results. No equivalent KSA ownership research was needed because the purported initialization mechanism was not established.

## VERIFY A — attribution

Strongest objection: profiling itself may change the process state responsible for the historical failure, while caller hooks do not cover instruction-exact counter boundaries. The control proves real-object visibility for its known object, not absence of all possible unseen allocations. The 108/0 catalog discrepancy further limits any blanket accounting claim. A causal attribution **does not survive**; ATTRIBUTION INSUFFICIENT does.

## VERIFY B — ownership

No lazy allocation was proven, so moving initialization earlier would be speculative symptom masking. No ownership correction recommended. Baseline capability to produce the target object is unknown because no target object was captured.

## VERIFY C — architecture

All accepted source fingerprints and helper contents remain unchanged. The two attributed processes retained numerical/deterministic suite outcomes, but this does not replace unprofiled Release qualification. No evidence invalidates the required exact-event architecture; no architectural revision made.

## Recommended next action

Return to Project Control for payoff reassessment of the unresolved Release allocation gate. Do not begin another run, observer escalation, gate matrix, warmup change or correction automatically. The prescribed two-process attribution bound is exhausted.

**RELEASE PERFORMANCE UNQUALIFIED.** No performance benchmark was requested or executed; ordinary full-suite timing output is not a performance qualification result.

## Evidence / restoration / Git

Retain this report, static audit, preflight identities, compact run/window results, decisive positive/incidental witnesses and reproduction source/scripts. Full stdout/build products remain disposable in `.codex/exact-event-attribution`; no bulk traces exist.

All four disposable callers were restored byte-for-byte; the extra hook source was moved out of the test compile directory. All **167** disposable closure files match their pre-probe source hashes; all **332** prior baseline/candidate comparison files also match. The built diagnostic DLL still contains hooks and is explicitly disposable, not deployed production.

Canonical **14/14 fingerprints match**; working status is unchanged, **8 tracked modifications and 6 untracked candidate files**, **0 staged**. `git diff --check` PASS, with only existing LF/CRLF conversion warnings. No commit/tag/merge/push/banking.

Existing deferred scratch: **557 files / 44,067,060 bytes**. New disposable attribution output: **290 files / 21,478,493 bytes**. Combined: **847 files / 65,545,553 bytes**. Disposed: **0 files / 0 bytes**. Cleanup remains deferred; no blocked deletion was retried.

## Reproduction capability

For a separately authorized future reproduction, `prepare.ps1` verifies the prior 167-source candidate closure, canonical hashes and tags; copies only that source closure; reuses the retained native observer boilerplate/headers; and inserts the listed caller hooks. `native-prefix.cpp` plus retained callback boilerplate produces `observer.cpp`; `AllocationProbe.cs` is diagnostic-only. Build the generated native build.cmd and disposable Release test project before running. `run.ps1` sets child-local registration, records identity and prevents same-run retries; `closeout.ps1` restores source and records the result. Use a reviewed fresh scratch/evidence destination rather than overwriting this completed probe. Do not run these instructions without new Project Control authorization.

`preflight.json`, `observer-windows.json`, `witnesses.txt` and `result.json` provide IDs/hashes/timestamps and exact outcomes. The retained source is sufficient to reconstruct instrumentation without keeping raw dumps. Existing source-only comparison content is deferred scratch; its original preparation scripts and banked/candidate fingerprints are retained in the sibling evidence packages.

STOP FOR PROJECT CONTROL.
