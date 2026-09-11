# Global permanent-gate allocation observation

2026-09-11. **H — MIXED / INSUFFICIENT**: a corresponding managed object is now
proven, but its type and allocating owner are unresolved. Candidate causality:
**NOT ESTABLISHED**. Lead judgment: **ESCALATE TO PROJECT CONTROL**.
**PAIRED PRIVATE PROPAGATION CANDIDATE — FOLLOW-UP REQUIRED**.

## Baseline and freeze

Branch `codex/root-linked-private-propagation`; HEAD/main/origin/main and M14.14
target `f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe`. Before instrumentation, all
315 canonical C#/project hashes and six retained binaries matched the prior return.
The [data record](global-gate-observer.json) preserves the complete prior evidence
package identity. SDK 10.0.303; .NET 10.0.12, Windows x64; cwd `E:\NovaCore`.

Normal Release production DLL remained
`6773F2A46EDE28B31AFA772174D68B427C34A89BB55DD785F50503936E56C384`.
Normal test DLL is
`E1548AFE9A333125C69C9C6DFD503CF21B1F6870442624B665B98784296C578F`;
temporary observed test DLL was
`EBCAE8DCF58E4617DFE7159226FF81630EC9C6FA945F4C99CC4F63A570C19DCE`.
One necessary test-only Release build passed with zero warnings/errors, using
`--no-restore -p:BuildProjectReferences=false`. No production build/edit, tiering
change, group reorder, selector wiring, or acceptance restart.

## Scope and comparison quality

Reused the same qualified native observer DLL as [the prior bounded observation](real-runner-observer.md),
SHA256 `8B86C6B14BFF5E35FCE4873C17A9B3A0620C9BB30BDD60A211CA60216A755E31`.
The native mechanism was not changed/rebuilt. Its source remains at
[the existing shared provenance](../contact-response-transaction/sas-allocation-classification/observer.cpp).

Temporary hooks covered every `OrdinaryAllocationMeasurement` use and five direct
normal-runner zero-required counter windows: physical epochs, contact generation,
isolated-response policy, long-duration servicing and clock orchestration. The two
direct positive-control windows were also covered. No production operation was patched.

Static reconciliation: 42 shared-helper construction sites, minus two uninvoked
performance-only sites and one control site, leave 39 normal zero-required sites.
The post-impact measurement site executes twice with distinct case names: 40 shared
zero-required windows plus five direct windows = **45**. The first full run confirms
all 45 gate identities in sequence. Six existing positive controls also execute.
The full identity/order/counter table is retained in the data record.

Report-only construction/orientation counters and uninvoked selector performance
methods were excluded. None of these defines a zero-required window in the normal
46-group invocation. Timers, setup, warmups, iterations, predicates and exact-zero
thresholds remained unchanged.

Native Begin follows successful existing no-GC entry and precedes the original
counter read. Native End immediately follows the original counter close, before
the original no-GC exit. Gate reporting follows native close. Cleanup closes an
active observer scope on exceptions; original no-GC failure behavior remains hard.
There is no continuous process-wide capture. An initial wrapper preparation scope
is labelled separately and is not interpreted as a permanent-gate observation.

The normal runner's loop is unchanged inside a diagnostic catch: after a failed
group it logs the original exception, invokes the unchanged existing byte-array
control in that same process, then rethrows. This covers failures before a normal
control executes without adding a selector process or changing group order/workload.
No later test group runs after the failure.

## Limits

The observer uses exact owner-thread allocation callbacks, object-size queries,
bounded managed stacks and its existing context/GC/JIT notification fields. It
can perturb runtime timing, compilation and state. Native metadata reporting is
synchronous. Its scope slightly brackets the exact counter interval. These are
diagnostic runs, not unprofiled acceptance or performance results.

Type resolution failed for the decisive ClassID (`module=0`, `token=0`, callback
metadata HRESULT `0x80070057`). Do not infer an array element type from this or
its size. This ClassID differs from the observed `byte[128]` control ClassID.
The available stack has no allocating native/runtime helper or precise IL offset.
No escalation to a new observer, type probe, EventPipe, or JIT/GC archaeology occurred.

## Matrix and stop

| Run / PID | First zero-required failure | Counter | Objects / bytes | Groups pass/fail/unreached | Candidate group already executed? | Result |
|---|---|---:|---|---|---|---|
| 1 / 47756 | None | All 45 zero | No failing window | 46 / 0 / 0 | Yes, at completion | PASS; exit 0 |
| 2 / 25328 | `sas-proof`, group 32 | 12,336 | 1 / 12,336 | 31 / 1 / 14 | No | FAIL; exit -532462766 |
| 3 | NOT RUN | — | — | — | — | Required stop after run 2 |

The run-2 gate reported no-GC entry **PASS**, exit **PASS**. Its observer scope
was valid; two same-process controls had already passed. The post-failure control
also passed. Thus all three stop conditions were met. No retries or builds between runs.

## Positive controls

All six existing controls in run 1 and all three executed controls in run 2
reported **152 counter bytes**, **one 152-byte object**, and successful no-GC
entry/exit. Their stacks identify the permanent `AllocateServicingCounterControl`,
`AllocateOrchestrationCounterControl`, or `OrdinaryAllocationMeasurement.AllocateControl`
body, each `new byte[128]`. Native array-name lookup is unavailable, so type identity
comes from these known source/call sites, not a fabricated native type name.

## Decisive counter/object evidence

Run 2: measured OS thread **43684**; same context storage address throughout.

```text
NATIVE_ATTRIBUTION owner=43684; delta=12336; objectCount=1; objectBytes=12336; entries=8
NATIVE_EVENT kind=1; thread=43684; qpc=2605327733221; a=140719029858696; b=12336
GLOBAL_GATE gate=sas-proof; counter=12336; observer=CLOSED
ORDINARY_ALLOCATION gate=sas-proof bytes=12336 entry=PASS exit=PASS
```

| Context field | Before | After | Difference |
|---|---:|---:|---:|
| Cumulative allocation bytes | 10,470,328 | 10,482,664 | **12,336** |
| Cumulative UOH bytes | 46,622,840 | 46,622,840 | 0 |
| Unused allocation-context bytes | 272 | 272 | 0 |
| Allocation pointer | 2,714,957,640,928 | 2,714,957,653,288 | 12,360 |
| Allocation limit | 2,714,957,641,200 | 2,714,957,653,560 | 12,360 |

Object bytes = counter delta = cumulative allocation increase = **12,336**.
Unused context did not retire. Pointer/limit displacement is reported independently;
its additional 24 bytes are not silently counted as another managed object.
GC start/finish notification counts stayed at 9/9. Eight event records fit the
8,192-event buffer. Stack snapshot HRESULT was zero (success).

Resolved managed stack:

```text
Program.<<Main>$>g__SasSignFrameContinuityProofTests|0_11
Program.<Main>$
[unresolved native/runtime frame]
```

A JIT notification for that test precedes the allocation callback. That is a
recorded ordering fact, not proof that JIT/tiering owns the allocation.

## Minimal ownership audit and causality

The SAS test method is unchanged from banked M14.14 (normalized SHA256 in the data).
Its measured 100,000-iteration loop calls `SpacecraftSasController.TryEvaluate`
with value-type arguments/results and mixes one scalar hash. Setup arrays,
diagnostic formatting and other test phases remain outside that counter interval.
The controller uses the existing attitude canonicalization and value arithmetic.
No paired-propagation code appears in the captured stack or this direct source path.

This does not prove whether the object belongs to legacy execution, compiler/runtime
preparation, test machinery, or observer effects. Inlining and missing native/type
resolution prevent assigning ownership from a test frame alone. **Owner UNKNOWN**;
candidate production causality **NOT ESTABLISHED**. The propagation group had not
executed before failure, but indirect assembly/registration/state effects remain open.

## Classification and historical relationship

**H — MIXED / INSUFFICIENT** under the mandated owner-based taxonomy. Real managed
allocation is proven for this witness; A–E require an owner not established here.
F is contradicted by the object callback. G is contradicted by the failed gate.

Historical accounting relationship: **DIFFERENT MECHANISM**, specifically for this
captured SAS witness versus the prior retirement-only witness. Here an object and
cumulative allocation increase match the counter; there the object count and
cumulative bytes stayed zero/unchanged while unused context retired. No CLR-bug claim.

Preserve separately: normal attitude 12,336; modified isolated attitude 6,192;
normal speed 6,192; narrow-observer rigid lookup 12,336; and this global-observer
SAS 12,336. No shared mechanism is established for the older failures by equal size.

Red-team finding: object presence and arithmetic survive, but allocation ownership
does not. The observer may perturb state, and the managed test frame is not proof
of a source-level allocating instruction. No broader causality claim survives.

## Payoff and reproduction

The expansion answered the useful presence question at a scoped failing gate.
It did not identify an owner for a correction. Stop for Project Control; no third
run, code/test correction, new measurement boundary or acceptance restart.

The [reproduction recipe](global-gate-observer-reproduce.ps1) retains the exact
hook plan and executed backup/build/run/restore operations. Its assembled script
is syntax-checked, not executed again. Only under a new authorization:

```powershell
$recipe = 'E:\NovaCore\docs\engineering-evidence\private-canonical-propagation\global-gate-observer-reproduce.ps1'
& $recipe -Phase Prepare
& $recipe -Phase Build
& $recipe -Phase Run1
# Run2/Run3 only while the authorized stop condition has not been reached.
& $recipe -Phase Restore
```

Normal child command has no selectors:
`dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll`.
The three child-only CORECLR registration variables point to the same qualified
native DLL used by P/Invoke. No unrelated runtime environment settings are changed.
If that DLL is unavailable, its retained source/build recipe is linked by the prior
observer report; a future run must qualify it again. No SDK/header/source-tree copy
belongs in permanent evidence.

## Restoration and storage

All 33 backed-up source/build-output files were restored and the temporary adapter
removed. Production/permanent tests and normal deployed binaries are unchanged.
Candidate remains UNBANKED; all outstanding acceptance is paused.

The old 50-file / 6,217,201-byte scratch directory was explicitly reviewed again.
Its deletion was rejected before execution by automatic approval review, stating
only “blocked by policy”; no alternative or further retry was used.
**DISPOSABLE SCRATCH RETAINED DUE TOOLING/POLICY RESTRICTION**. This is not an
engineering blocker. The new reviewed directory's deletion was also policy-blocked
before execution: **46 files / 6,610,623 bytes** remain. Disposed in this ticket: zero.
Combined disposable remaining: **96 files / 12,827,824 bytes**. No retry, permission
change or alternative destructive mechanism was used. Only concise
report/data/reproduction evidence is permanent. Reviewed manual cleanup:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\private-propagation-real-runner-observer','E:\NovaCore\.codex\private-propagation-global-gate-observer' -Recurse -Force
```
