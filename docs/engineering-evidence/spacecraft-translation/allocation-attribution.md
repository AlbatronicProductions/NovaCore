# M14.2 transient allocation attribution

Technical judgment: **PASS**. Attribution: **CLR / JIT / TIERED-COMPILATION ACCOUNTING**.
The identified mechanism is background-GC accounting, not a demonstrated JIT
allocation. **M14.2 BANKING FOLLOW-UP REQUIRED** only for manual diagnostic disposal.
Production implementation is unchanged; no staging or banking occurred.

## Baseline and original failure

`codex/spacecraft-translation`; HEAD/main/origin/main/peeled M14.1 remain
`5b9b02ab1d2ba0e903809510908e4819984d4de2`. All 17 accepted production
fingerprints and the translation test fingerprint still match `verification.json`.
Program.cs contained the previously authorized operand diagnostics at entry and
now additionally contains the reviewed measurement-boundary correction.

Original full Release failure: 8,160 counter bytes, Completed, 1,000/1,000 events;
pending 1,000 to zero, marker/revision zero to 1,000, clock reached tick 2. Its exact
allocation context was not recorded. The attribution below directly proves the
same reproduced failure class; identifying the historical 8,160 as discarded
unused context is supported inference, not retrospective inspection of that run.

## Positive attribution

An exact native CLR allocation observer was used only in disposable copies.
`allocation-attribution.json` retains the positive records and runtime/source hashes;
`allocation-attribution-profiler.cpp` retains the diagnostic source. Runtime was
Microsoft.NETCore.App 10.0.11, Windows x64. Ordinary contract runs had no profiler.

The observer enabled exact ObjectAllocated callbacks, suspension/JIT notifications
and **COR_PRF_HIGH_BASIC_GC**. A known byte[128] control produced exactly one
152-byte object and a 152-byte counter delta on the target thread. It also verified
that the observed allocation pointer advanced 152 bytes with unchanged totals.
No sampling was used for object counts.

| Directly observed field | Before / GC-preparation suspension | After resume / end |
|---|---:|---:|
| Measured owner OS thread | 39940 | 39940 |
| Owner context address | 2022639491856 | same |
| Allocation pointer | 2022716134232 | 0 |
| Allocation limit | 2022716137360 | 0 |
| alloc_bytes | 3358200 | unchanged |
| alloc_bytes_uoh | 28252192 | unchanged |
| Unused context | **3128** | **0** |
| Managed objects / object bytes | 0 / 0 | 0 / 0 |

The counter increased **exactly 3,128 bytes**: totals minus unused context changed
from 31,607,264 to 31,610,392. Completion and all 1,000 events passed. GC thread
42432 issued suspension reason 7 (GC_PREP), resumed execution, and finished an
already-running collection. GC starts/finishes were 4/3 at entry and 4/4 at exit.
There was no JIT event in the window. Earlier exact-object observation also
captured a 7,992-byte counter rise with zero objects and the same GC overlap.

There is no allocating managed object type, size or allocation call stack for the
positive accounting event: **object count is zero**. Its cause is the counter's
native accounting state. The source-supported native responsibility chain is
background mark completion → repair_allocation_contexts(FALSE) → void_allocation.
We observed the context change across GC preparation; we did not capture a native
debugger stack at void_allocation itself.

Pinned runtime evidence:

- [Counter arithmetic](https://github.com/dotnet/runtime/blob/v10.0.11/src/coreclr/vm/comutilnative.cpp): allocated totals minus unused context.
- [Context retirement](https://github.com/dotnet/runtime/blob/v10.0.11/src/coreclr/gc/gc.cpp#L8031): void_allocation clears pointers without decrementing totals; background-mark completion invokes this path around line 39463.
- Collection counts increment at initialization, so unchanged counts in earlier
  probes did not exclude an already-running background collection completing.

Tiering-disabled comparisons from the earlier investigation altered timing; they
did not establish JIT allocation. This direct observation supersedes that causal
hypothesis. Candidate setup adds preallocated force-history storage before timing,
which can affect GC timing; no measured orchestration object allocation is thereby
established. The exact same marker path under banked M14.1 passed three bounded
attribution controls, with zero objects and no GC overlap. That sample alone does
not establish a candidate-specific production defect or a preexisting one.

## Observer bounds and reproduction

The initial profiler requested legacy MONITOR_GC, which the CLR explicitly uses to
disable concurrent GC. Its six passing runs were rejected as attribution evidence.
After correcting the observer to BASIC_GC, the first positive result occurred on
run 2 of a maximum 6. Direct context sampling then stopped at the first positive
result, run 3 of a maximum 4. M14.1 control was capped at 3. No endless reruns were
performed. Only the declared post-correction contract repeats followed.

For future reproduction, use a disposable copy of Core, EphemerisFormat, Simulation
and Simulation.Tests plus Directory.Build.props. Preserve the normal first 12 test
groups; stop after Clock execution orchestration. In that copy only, restore the
pre-correction allocation measurement, bracket it with the observer BeginWindow /
EndWindow(delta) exports, and remove the later positive-control assertion. Do not
apply this reversal to production tests. Warm the EndWindow P/Invoke before tests.

Build the retained profiler against pinned v10.0.11 corprof.h (pal/prebuilt/inc),
cor.h and corhdr.h (inc), and corerror.h (pal/prebuilt/inc), using Windows SDK headers,
MSVC x64 /LD /EHsc /std:c++17 /O2 and ole32.lib. Export DllGetClassObject as
ProfilerGetClassObject and DllCanUnloadNow as ProfilerCanUnloadNow via a .def file.
Keep outputs in the disposable directory. Set child-process-only
CORECLR_ENABLE_PROFILING=1, CORECLR_PROFILER={61D15631-E78F-45D1-A00E-20FC6F1CED24},
and CORECLR_PROFILER_PATH to that DLL. Restore environment values afterward.

ConfigureCounter receives the actual GC.GetAllocatedBytesForCurrentThread method
entry, following its FF25 RIP-relative indirection. It accepts only the recorded
x64 native instruction pattern, then resolves the TLS index/offset and the same
four fields read by that counter. The managed caller MUST abort if ConfigureCounter
returns zero, before BeginWindow. This is a runtime-specific diagnostic, not a
production dependency or a portable API. Contexts are read, never modified.

VERIFY found the decoder arithmetic correct. The five-event/four-context positive
record is complete and internally consistent, far below the 8,192/32 record bounds.
The purpose-built observer is not a general concurrent recorder: entry indices are
published before all fields, and shutdown does not drain arbitrary in-flight
callbacks. The retained positive record has no malformed/truncated entries; owner
begin/end snapshots independently establish the exact arithmetic. These limits,
profiling perturbation and missing original-8160 context remain explicit.

## Bounded correction and permanent contract

Only tests/NovaCore.Simulation.Tests/Program.cs changed. Original one-event warmup,
fixture construction, 1,000 events and target tick 2 are unchanged. Successful
TryStartNoGCRegion(1 MiB, disallowFullBlockingGC:true) establishes a deterministic
measurement boundary after setup; failed entry fails the test. The counter encloses
only the existing orchestration call. EndNoGCRegion runs in finally before output;
failed exit also fails the test. Default production GC and tiering remain unchanged.

The 1 MiB reservation bounds the CLR region; it is **not** an allocation allowance.
The production threshold remains exactly zero, with no subtraction, tolerance,
retry, extra warmup or timing criterion. A deliberate byte[128] allocation follows
the measured workload inside the same region, through a non-inlined returning
helper retained with KeepAlive; the counter must detect it (>0, no fixed object-size
assumption). Completion, exact event count and zero allocation assert independently.

The required contract is zero managed allocations by warmed preallocated
orchestration. The observer proved that a positive old counter could violate that
measurement without violating the contract. The corrected measurement prevents
that GC-accounting transition while still detecting actual managed allocation.
Ordinary functional tests execute with normal GC outside this narrow window.

The [supported API contract](https://learn.microsoft.com/en-us/dotnet/api/system.gc.trystartnogcregion?view=net-10.0)
and pinned runtime implementation support this boundary; entry settles existing
background GC and exit rejects an invalidated region. This is not a claim of zero
GC pause cost or a new production GC policy.

## Validation and review

Predeclared plan: 10 full fresh Simulation processes and 5 isolated orchestration
processes per configuration, stop on first failure. This exercises suite ordering
and isolated execution under default tiering, not retries until success.

**Debug: 10/10 full + 5/5 isolated PASS. Release: 10/10 full + 5/5 isolated PASS.**
All 30 measured calls reported zero bytes, Completed and 1,000 events; positive
allocation controls and no-GC entry/exit succeeded. Each full run passed 33 groups.
Debug/Release solution builds passed with zero warnings/errors. ReferenceFrames
passed 11 groups each, Precision passed, selected M14.1 queries passed 2/2 each
without failures/skips. Numerical/replay/frame/transaction gates passed unchanged.
See `allocation-corrected-validation.json`. No performance benchmark was rerun.

Production red-team challenge (profiler suppressing allocation or confusing
threads) was addressed by exact flags, real-allocation control, owner identity and
direct context arithmetic. Test red-team challenge (weakening zero allocation) was
addressed by fail-closed boundaries, unchanged warmup/workload, independent exact
assertions and the positive control. Both reviews passed. Historical attribution
limits above remain; no further allocation investigation is required for this gate.

## Manual disposal before banking

No automatic deletion was retried. Proven disposable copies: baseline 331 files /
36,298,951 bytes; candidate trace 254 / 18,899,175 bytes; attribution scratch 17 /
1,641,965 bytes. All are ordinary directories under E:\NovaCore\.codex, with no
reparse points. None is consumed by production, builds or permanent tests.
Also disposable: the 9,151-byte prior temporary gate summary and ten superseded
build/test transcripts (27,382 bytes), already classified in closeout-classification.json.
Results, hashes and reproduction responsibility are consolidated in retained evidence.

**Disposable remaining: 56,876,624 bytes.** Project Control may run these exact
commands after review; they have not been executed:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\m14-2-orchestration-baseline', 'E:\NovaCore\.codex\m14-2-orchestration-trace', 'E:\NovaCore\.codex\m14-2-allocation-attribution' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore\.codex\m14-2-orchestration-final-gates.json' -Force
$evidence = 'E:\NovaCore\docs\engineering-evidence\spacecraft-translation'
Remove-Item -LiteralPath "$evidence\build-debug.txt", "$evidence\build-release.txt", "$evidence\precision-debug.txt", "$evidence\precision-release.txt", "$evidence\referenceframes-debug.txt", "$evidence\referenceframes-release.txt", "$evidence\simulation-debug.txt", "$evidence\simulation-release.txt", "$evidence\surface-query-debug.txt", "$evidence\surface-query-release.txt" -Force
```

Preserve performance-release.txt, all reports/JSON, and allocation-attribution-profiler.cpp.
After manual disposal, confirm status and diff-check; no further full validation
campaign is needed merely to delete these unused copies. M14.2 remains unbanked.
