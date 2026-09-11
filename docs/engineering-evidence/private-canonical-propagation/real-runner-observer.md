# Bounded real-runner allocation observation

2026-09-11. **H — MIXED / INSUFFICIENT**. Candidate production causality:
**NOT ESTABLISHED**. Lead judgment: **ESCALATE TO PROJECT CONTROL**.
**PAIRED PRIVATE PROPAGATION CANDIDATE — FOLLOW-UP REQUIRED**.

The three authorized normal Release processes are exhausted. Both requested
gates measured zero in all three. Runs 1 and 3 passed; run 2 failed at a different,
unobserved allocation gate. This does not identify or clear the historical failures.
No correction, fourth process, expanded observer scope, or acceptance restart followed.

## Baseline and comparison quality

Branch `codex/root-linked-private-propagation`; HEAD/main/origin/main and
`m14.14-private-root-postimpact-state` target remain
`f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe`. All 315 canonical C#/project
fingerprints matched the failed normal acceptance state before preparation and
after restoration. All banked tags are unchanged. SDK 10.0.303; .NET 10.0.12,
Windows x64; cwd `E:\NovaCore`.

Normal Release Simulation assembly SHA256:
`6773F2A46EDE28B31AFA772174D68B427C34A89BB55DD785F50503936E56C384`.
Normal permanent test assembly:
`E1548AFE9A333125C69C9C6DFD503CF21B1F6870442624B665B98784296C578F`.
Temporary observed test assembly:
`D3A0212E04A8E9DA0511BA1756F875C8A2B2F95C90069F3DCC5C537B47A768D8`.

One test-only Release build was necessary for observation: zero warnings/errors,
`BuildProjectReferences=false`. No production rebuild or edit. Program differed
from the frozen candidate only by the observer-preparation call before existing
argument dispatch. All 46 normal registrations, order, method bodies, workload,
warmup, existing checked 1 MiB no-GC boundaries, assertions and exact-zero
thresholds were preserved. The normal three invocations had **no selector args**.
The existing control selector was used once for observer qualification only.

## Observer mechanism and limits

Reused the existing built [qualified observer](../contact-response-transaction/sas-allocation-classification/README.md)
and its [native source](../contact-response-transaction/sas-allocation-classification/observer.cpp).
The accompanying scratch source matches the retained source after CRLF normalization
and matches its original manifest hash. Native DLL SHA256:
`8B86C6B14BFF5E35FCE4873C17A9B3A0620C9BB30BDD60A211CA60216A755E31`.
It was not rebuilt in this ticket. Exact child-process registration and source/hash
provenance are in [the compact data record](real-runner-observer.json).

The observer enables exact owner-thread object callbacks, bounded 16-frame stacks,
basic GC and suspend notifications, and its existing narrowly recorded JIT events.
It has fixed 8,192-event/32-context buffers. No EventPipe or new tracing mechanism.
Native counter-pattern recognition succeeded. Managed P/Invoke and CLR registration
resolved the same absolute DLL, avoiding the previously demonstrated duplicate-module
state problem. No tiering, PGO or global runtime settings changed.

For `speed-presets`, `attitude-integration`, and the existing positive control:
native Begin runs after successful no-GC entry and before the original counter opens;
native End runs immediately after the counter closes and before no-GC exit.
The native extent is therefore slightly wider than the exact counter interval.
No managed JSON reporter was introduced. Workload exceptions still trigger cleanup.
Unobserved gates keep their counters and hard failure behavior.

Profiler registration, the temporary helper IL, first-use compilation, metadata
callbacks and synchronous native reporting perturb execution. These runs cannot
replace normal acceptance or performance measurements. Native callback byte totals
need not equal per-thread counter bytes. Array metadata naming is unsupported by
this observer; the known array is identified through its allocation stack/source.
Post-window metadata lookup failures are not resolved names. No buffer overflow
appeared in the captured windows. No object observation is available for `rigid-lookup`.

## Positive control

Qualification process PID 40480 used the unchanged permanent `AllocateControl` body,
`new byte[128]`, and its original warmup/helper. Counter: **152**; entry/exit PASS.
The observer captured a **152-byte object** with stack
`AllocateControl → PositiveControl → Program.<Main>$`. Allocation pointer advanced
152 bytes, with cumulative/context-limit fields unchanged.

That qualification also captured three `System.String` callbacks (72/36/64 bytes):
four callbacks / 324 bytes total, while the counter was 152. Their precise accounting
is unresolved; they must not be added to or subtracted from the control result.
Eight existing control executions within successful full runs 1 and 3 each reported
**152 counter bytes, one 152-byte object**, entry/exit PASS. The corresponding array
was therefore visible repeatedly in this harness. Run 2 stopped before these controls.

## Normal-runner matrix

| Run / PID | First allocation failure | Counter bytes at failure | Observer objects at failure | Groups pass/fail/unreached | Entry / exit | Process exit |
|---|---|---:|---|---|---|---:|
| 1 / 61300 | None | — | — | 46 / 0 / 0 | All allocation gates passed | 0 |
| 2 / 57532 | Rigid-body torque transaction / `rigid-lookup` | 12,336 | UNAVAILABLE: outside target scope | 29 / 1 / 16 | PASS / PASS | -532462766 |
| 3 / 58076 | None | — | — | 46 / 0 / 0 | All allocation gates passed | 0 |

No retries or builds between these runs. Target details:

| Run | Speed counter | Speed callbacks | Attitude counter | Attitude callbacks | Target entry / exit |
|---|---:|---|---:|---|---|
| 1 | 0 | 3 strings / 172 bytes | 0 | 0 objects / 0 bytes | PASS / PASS |
| 2 | 0 | 3 strings / 172 bytes | 0 | 0 objects / 0 bytes | PASS / PASS |
| 3 | 0 | 3 strings / 172 bytes | 0 | 0 objects / 0 bytes | PASS / PASS |

Each speed window captured `System.String` sizes 72, 36 and 64, with stack
`Program.<<Main>$>g__SolarUtcAndSpeedPresetTests|0_0 → Program.<Main>$`.
Counter and allocation-context pointer/limit/cumulative fields stayed unchanged.
These callback-only observations do not identify the prior 6,192-byte counter effect.
No narrower string ownership/accounting investigation was authorized or performed.

Attitude windows had no object callbacks and unchanged context fields. Measured
OS-thread identities were coherent (51832, 38380, 35504 respectively; see raw values
in the data record if rechecking). The candidate-specific propagation group is last:
it ran in successful runs 1/3 and had not executed before run 2's failure at group 30.
That does not exonerate candidate assembly/registration effects indirectly.

Run 2's decisive failure was:

```text
ORDINARY_ALLOCATION gate=rigid-lookup bytes=12336 entry=PASS exit=PASS
System.InvalidOperationException: rigid-lookup: expected zero managed allocation, actual=12336
OrdinaryAllocationMeasurement.RequireZero: line 52 (temporary instrumentation)
Program.RigidBodyTorqueTransactionTests: line 446
Program.<Main>$: line 106
```

The three relevant permanent methods (`SolarUtcAndSpeedPresetTests`,
`SpacecraftAttitudeIntegrationTests`, `RigidBodyTorqueTransactionTests`) are textually
unchanged from banked M14.14 after line-ending normalization. No source reachability
claim substitutes for missing object evidence at the failed gate.

## Classification, witness separation and payoff

**H — MIXED / INSUFFICIENT** is required: the named witnesses did not reproduce,
but the full observer matrix was not clean. `G` would incorrectly hide the new
permanent-gate failure. `F` is not supported: zero objects were not observed at
the failed gate; the observer was inactive there. No candidate, legacy, harness,
framework or CLR accounting owner of a nonzero witness was established.

Keep distinct: original normal attitude 12,336; modified isolated candidate attitude
6,192; normal speed 6,192; and this observer-run rigid lookup 12,336. Byte magnitude
does not establish a shared mechanism. Relationship to prior allocation-context
retirement evidence: **INSUFFICIENT**. No CLR-bug claim, subtraction, tolerance,
prewarming or extra no-GC boundary is warranted.

Red-team objection: the observer can change code generation/process state and captures
objects outside strict counter accounting. It therefore cannot clear the old failures.
This objection survives and is incorporated in the insufficient classification.
The new out-of-scope failure also forbids calling this a clean three-run observation.

PAYOFF recheck: no proven owner exists for a bounded correction, and this authorized
observer budget is exhausted. Return the new witness and scope limitation to Project
Control. A further named-window observation or a new normal reproducibility bar needs
a separate decision; neither is started or prescribed as already sufficient for acceptance.

## Reproduction and restoration

The [reproduction script](real-runner-observer-reproduce.ps1) retains the executed
preparation/run/restoration operations. Its assembled PowerShell syntax was checked;
it was not rerun after the matrix. Use only under a new authorization. Commands:

```powershell
$recipe = 'E:\NovaCore\docs\engineering-evidence\private-canonical-propagation\real-runner-observer-reproduce.ps1'
& $recipe -Phase Prepare
& $recipe -Phase Build
& $recipe -Phase Control
# Require the known object to be visible before any negative inference.
& $recipe -Phase Run1
& $recipe -Phase Run2
& $recipe -Phase Run3
& $recipe -Phase Restore
```

Stop according to the authorized plan, not merely the next line of this recipe;
always restore through cleanup. If the existing DLL is absent, rebuild the retained
native source using its [existing reproduction instructions](../contact-response-transaction/sas-allocation-classification/reproduce.md)
and pinned headers/compiler; update the single registration path and prove the
positive control again. Do not retain copied SDKs or production trees.

Thirty exact source/build-output preimages were restored, including Release bin/obj
files. The temporary adapter was removed. All 315 source/project hashes and all six
previously retained candidate binary hashes match; original normal Release executable
is restored. No acceptance test was run after restoration. Nothing staged or banked.

This report/data/reproduction script retain the bounded decision and ability to
repeat it. Created disposable scratch: **50 files / 6,217,201 bytes**. Disposed: **0**.
Remaining: **50 files / 6,217,201 bytes**, including verified restoration backups,
temporary scripts and process output. Automatic approval review rejected deletion
before execution, stating only “blocked by policy.” No retry was attempted. The
original source/normal binaries are already restored; these backups are disposable.
Other pre-existing investigation scratch was not touched. Reviewed manual cleanup:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\private-propagation-real-runner-observer' -Recurse -Force
```
