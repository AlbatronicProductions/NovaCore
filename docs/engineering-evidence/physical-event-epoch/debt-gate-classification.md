# Bounded full-Simulation failure classification

2026-09-09. **PHYSICAL EVENT EPOCH CANDIDATE — FOLLOW-UP REQUIRED**.
Primary failed contract: **A. ALLOCATION CONTRACT**.
Causal classification: **INSUFFICIENT EVIDENCE**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**.

## Baseline and preservation

Branch `codex/physical-event-epoch`. HEAD/main/origin/main remain
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`. M14.1–M14.5 annotated-tag commit
targets match the [candidate baseline table](README.md#baseline-and-scope).
The existing candidate working tree was preserved; the index remained empty.
All five source/test SHA-256 fingerprints in the candidate report matched before
analysis and after restoration. No epoch, comparator, M14.5 or other production
correction was made.

## Observation method and declared bounds

The original assertion at `Program.cs:1228` combined allocation, clock, debt,
processed count and pending count. Its text did not identify the failed operand.
Temporary test-only reporting captured all five independently after the allocation
counter closed. Before-window snapshots used scalar getters. The original warmup,
event source, iteration count, loop, per-cycle completion check and composite
thresholds were unchanged. Reporting itself was outside the measured window.
Thread identity was captured beforehand. No GC/JIT/tiering changes or profiler
were introduced.

The selector `--debt-service-only` invoked the existing complete
`HostDurationDebtServiceTests` group; no setup was extracted into a different
workload. The measured section schedules 5,000 marker events at `id * 10` ticks,
with maximum 16 events per advance and preallocated 5,000-entry engine history.
It services one initial 100-tick interval, then measures 499 additional intervals
of 100 ticks each. Every measured service retains its original `Completed` check.

Declared candidate matrix: five fresh isolated Debug and five fresh isolated
Release processes. If needed, at most three full runs per configuration, stopping
when a failing run exposes the operands. All ten isolated runs passed. The first
full Debug run exposed the failure; no further full candidate runs were made.
Full Release reporting was therefore **not run**, not classified as passing.

A small causal control used `git archive HEAD` for Directory.Build.props, Core,
EphemerisFormat, Simulation and Simulation.Tests only. No Git checkout/ref was
changed. The original measured block matched banked M14.5 after newline
normalization; identical reporting and the selector were applied to that copy.
Control bound: one isolated run per configuration and one full Debug run matching
the failing context. All controls passed. No baseline Release full run was needed.

## Captured operands

| Operand | Before measured loop | Failing candidate full Debug | Expected final |
| --- | ---: | ---: | ---: |
| Allocated byte delta | Counter baseline captured | **8,160** | **0** |
| Clock ticks | 100 | 50,000 | 50,000 |
| Pending debt ticks | 0 | 0 | 0 |
| Processed events | 10 | 5,000 | 5,000 |
| Pending events | 4,990 | 0 | 0 |
| Completed measured service calls | 0 | 499 | 499 |

Managed thread ID was 2 in every recorded run. In all passing runs the allocation
delta was zero and every other operand had the same values as the table. Passing
isolation hashes: host-duration orchestration `2DB5C6703CC7E025`; long-duration
advancement `F5E81045A0C3D909`, identical in candidate and baseline configurations.

| Run | Allocation bytes | Result |
| --- | ---: | --- |
| Candidate isolated Debug 1 | 0 | PASS |
| Candidate isolated Debug 2 | 0 | PASS |
| Candidate isolated Debug 3 | 0 | PASS |
| Candidate isolated Debug 4 | 0 | PASS |
| Candidate isolated Debug 5 | 0 | PASS |
| Candidate isolated Release 1 | 0 | PASS |
| Candidate isolated Release 2 | 0 | PASS |
| Candidate isolated Release 3 | 0 | PASS |
| Candidate isolated Release 4 | 0 | PASS |
| Candidate isolated Release 5 | 0 | PASS |
| Candidate full Debug 1 | 8,160 | FAIL: allocation only; 9 groups passed, 1 failed, 27 not reached |
| Baseline isolated Debug 1 | 0 | PASS |
| Baseline isolated Release 1 | 0 | PASS |
| Baseline full Debug 1 | 0 | PASS: complete banked 36-group suite |

The observed pattern is **full-suite-context dependent in this bounded matrix**.
It does not establish a deterministic trigger or a rate of intermittent failures.
Earlier uninstrumented Debug/Release composite failures remain historical evidence;
their failing operands cannot be retroactively inferred from this Debug capture.

## Causality audit and stopping decision

Reachable production path: canonical host-duration clock advancement → execution
orchestrator → canonical transaction groups and pending timeline → event heap →
`SimulationEventHeaderComparer`. The shared comparator extraction is reachable.
It preserves the same integral time, signed priority, sequence and ID comparisons,
without added allocation expressions. Existing event scheduling, clock/debt
ownership and transactions have no candidate diff.

New rational epoch/order types are not called by the debt-service path. They have
no static registration or initializer injected into the timeline. Their serializer
is explicit and unregistered. The new epoch test DOES run earlier in the full
suite and includes a test-only no-GC accounting region. It can affect process
history. The additional comparer call can also affect code generation even when
comparison semantics are unchanged. Neither is evidence of the allocation's
actual owner or mechanism.

VERIFY A's strongest challenge is therefore indirect candidate influence through
test order or runtime/code-generation state. The passing baseline control does
not resolve this. It is insufficient to label the issue pre-existing, harness-only,
or candidate-caused. No object type, stack, allocator mechanism or actual managed
object callback was captured. The 8,160-byte size alone is not attribution and was
not equated with earlier unrelated allocation investigations.

This completes the authorized bounded classification. Positive object/runtime
attribution would be a new causal question requiring another payoff decision.
No GC/JIT profiling, repeated search for a green run, warmup adjustment, tolerance,
runtime configuration change or production correction was attempted.

## Correction, test review and validation

**Correction: NONE.** One production contract demonstrably failed, so the
conditional authority for a permanent assertion-only correction was not used.
Temporary reporting and the selector were removed by restoring the exact original
candidate Program.cs bytes. The reporting recipe below preserves reproduction without retaining the disposable
patch; no instrumentation is compiled into the permanent candidate.

VERIFY B: all five original thresholds remained exact; every loop completion check
remained in place; warmup and workload remained unchanged; no diagnostic formatting
occurred inside the allocation window. Diagnostic perturbation of runtime/codegen
cannot be excluded, so no inference depends on treating instrumentation as invisible.

Rebuilt Simulation.Tests and its dependencies in Debug and Release with zero
warnings/errors, both for instrumentation and after exact restoration. Restoration
used explicit `-t:Rebuild` to prevent older source timestamps leaving diagnostic
binaries deployed. Restored focused epoch tests passed in both configurations:
1,175 exact controls, 2,304 integral comparisons, 50 shuffled orders and zero warmed
managed bytes. Integral comparator semantics and test hashes remain unchanged;
machine-code identity is not claimed.

No permanent source/test correction occurred. The prior full solution builds,
ReferenceFrames, Precision and focused M14.2–M14.5 results remain the accepted
candidate evidence; those independent gates were not needlessly repeated. Full
candidate Simulation acceptance remains **uncleared**, and no banking is authorized.

## Reproduction and fingerprints

The [consolidated reproduction recipe](RETENTION.md#reproduction-without-disposable-patches)
describes the original raw-counter fixture, independent scalar operands and reporting
placement. Historical instrumented hashes below remain provenance, not a promise
that regenerated instrumentation has identical codegen. Use only a disposable copy.
After building the test project, each fresh isolated process was invoked as:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --debt-service-only
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --debt-service-only
```

Full-suite runs omit the selector. No special runtime environment was set.
Baseline control uses the corresponding paths in its disposable archive copy.
The candidate group ordering is recorded in Program.cs; baseline omits only the
new epoch group and candidate additions. Both keep original debt-group setup.

Instrumented Program.cs actual-byte SHA-256:

- Candidate: `8EAA82809D02745F1470140F405D3BAFB625F4128D9AB1E08906EE7C69338E13`.
- Baseline control: `7F1A8572265FD826F377718C3539F78E0EB1F62CC8E0613FD93C32157092336A`.
- Restored candidate: `872B9E2D2AB58CBAF7F573F0D916ACEB2DCD0BB72649D8B105D65DD59EC1C11C`.

## Evidence lifecycle

The new disposable directory `.codex/physical-epoch-gate` contained 361 files /
38,126,642 logical bytes: a bounded source archive, comparison builds, temporary
source backup and short run logs. The compact operand matrix, source fingerprints
and reporting recipe preserve the useful evidence. No bulk trace was created.
Existing unrelated scratch directories are outside this ticket.

Automatic approval review rejected the guarded deletion command as **blocked by
policy**, without a more specific reason. It was not retried or bypassed. Read-only
verification confirms the resolved target is exactly
`E:\NovaCore\.codex\physical-epoch-gate`, inside the workspace, with no reparse
points. **Disposed: 0 bytes; disposable remaining: 361 files / 38,126,642 bytes**.
It contains no canonical production source or active runtime dependency. Project
Control may remove only that comparison directory manually:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\physical-epoch-gate' -Recurse -Force
```

Final `git diff --check`: PASS (line-ending warnings only). Nothing staged.
