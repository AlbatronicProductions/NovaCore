# Final clean Release acceptance: exact-event spacecraft motion

**EXACT-EVENT SPACECRAFT MOTION CANDIDATE — FOLLOW-UP REQUIRED**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

Clean Release acceptance: **2/5 PASS; 3/5 allocation failures.** Required 5/5 not achieved. No production, permanent tests, allocation helpers, runtime settings or registration/order changes were made. No attribution or performance investigation followed these failures.

## Baseline

- HEAD/main/origin/main: `00f226096b61a117184464b5619e464d39789574`.
- Branch: `codex/exact-event-spacecraft-motion`.
- M14.6: `m14.6-physical-event-epochs` -> `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- M14.1–M14.6 tags verified unchanged; exact targets in `preflight.json`.
- SDK: 10.0.303. Each child loaded `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
- Candidate canonical fingerprints: 14/14 unchanged. All 167 normal source-closure hashes remained fixed before/through/after the matrix. Comparison to the earlier archive showed 45 byte-identical files and 122 CRLF-only differences; zero content differences. Nothing was normalized or rewritten.

## Allocation-witness policy

Zero-allocation gates retain exact production contracts. A nonzero counter is a failure witness, not by itself an object/call-stack/accounting attribution. Further root-cause work requires sufficient reproducibility, candidate association and mechanism relevance to justify its cost. Acceptance requires every declared gate to pass exactly; 2/5 does not qualify.

The interpretation was adopted for this ticket. It did not change thresholds, reservation, warmups, production behavior or tests.

## Historical 12,336 evidence

Previously retained witnesses: SAS 12,336; attitude 0/12,336/12,336 in matched candidate runs; one of those candidates passed attitude then failed impulse-evaluation at 12,336. Matched baseline attitude results were 0/0/0. No production managed-object population explaining that number was established. Previous evidence remains unchanged in the sibling `exact-event-sas-ab-20260910` and `exact-event-attitude-ab-20260910` packages.

## Existing attribution evidence

The preceding bounded attribution package, `exact-event-allocation-attribution-20260910`, retains two attributed 38/38 Release passes, all 32 ordinary allocation counters zero, a working one-object byte[128] positive control (152 object/counter bytes), and no 12,336-byte target population. A 108-byte System.String callback with a zero body-catalog counter remains unresolved, does not explain 12,336 and was not investigated again. The observer was not installed in the normal candidate used here.

## Preflight and fixed execution

Built the normal `tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj` and its dependencies in Release once with `-t:Rebuild`: PASS, zero warnings/errors. No solution-wide renderer/native validation was run. This was not the disposable attributed binary.

PE metadata inspection without loading candidate code found the exact-event test type and no disposable observer/probe types. Candidate source had no observer hooks. No CORECLR/COR/COMPlus profiler/runtime overrides or DOTNET startup hooks were present. Only the existing DOTNET_ROOT paths were inherited. The runner supplied no environment overrides, selectors, special preparation or warmup. Each child used the same normal DLL, dependencies, working directory and environment hash. Source and output-binary hashes were verified before each run and afterward. No builds during the matrix.

Command for each child:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' 'E:\NovaCore\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\NovaCore.Simulation.Tests.dll'
```

Working directory: `E:\NovaCore`. The parent captured ordinary stdout/stderr and read the loaded runtime module path; it did not attach a debugger/profiler or collect GC/JIT/allocation traces.

## Five-run Release acceptance matrix

| Run | PID | Result | First failure | Allocation gate | Bytes | Entry | Exit | Other contracts / exact-event group |
|---|---:|---|---|---|---:|---|---|---|
| 1 | 43824 | FAIL; 31/38 groups passed | SAS sign/frame continuity proof | sas-proof | 12,336 | PASS | PASS | Completed assertions passed; subsequent assertions/groups unreached. Exact-event unreached. |
| 2 | 49528 | FAIL; 31/38 groups passed | SAS sign/frame continuity proof | sas-proof | 12,336 | PASS | PASS | Completed assertions passed; subsequent assertions/groups unreached. Exact-event unreached. |
| 3 | 15244 | PASS; 38/38 | None | All 32 ordinary zero-required windows | 0 | PASS | PASS | All contracts PASS; exact-event PASS. |
| 4 | 47688 | PASS; 38/38 | None | All 32 ordinary zero-required windows | 0 | PASS | PASS | All contracts PASS; exact-event PASS. |
| 5 | 6140 | FAIL; 27/38 groups passed | Spacecraft attitude integration | attitude-integration | 12,336 | PASS | PASS | Completed assertions passed; subsequent assertions/groups unreached. Exact-event unreached. |

Allocation failures terminated their own processes normally through the existing unhandled assertion; the independent next process was a predeclared matrix row, not a retry. All five were executed. No additional processes, selective reruns or substitutions were added. No non-allocation failure or directly attributed workload object population was reported that would trigger an early hard stop.

Exact failure output for runs 1 and 2:

```text
System.InvalidOperationException: sas-proof: expected zero managed allocation, actual=12336
OrdinaryAllocationMeasurement.RequireZero ... OrdinaryAllocationMeasurement.cs:44
Program...SasSignFrameContinuityProofTests ... Program.cs:295
```

Run 5:

```text
System.InvalidOperationException: attitude-integration: expected zero managed allocation, actual=12336
Program...SpacecraftAttitudeIntegrationTests ... Program.cs:209
```

These are assertion-failure stacks, **not allocating-object stacks**. Nonprinted numerical operands remain UNAVAILABLE; later assertions are not inferred to pass. Full per-run ordinary gate records and exact error text are in `matrix.json` and bounded stdout/stderr witnesses.

## Acceptance classification

**MIGRATING PROCESS-LEVEL ALLOCATION WITNESS.**

The matrix also contains a **REPRODUCIBLE GATE-SPECIFIC ALLOCATION SIGNAL** at SAS: two runs failed the same gate with the same 12,336 delta and successful entry/exit. A third failed attitude, so the complete pattern spans multiple gates. Both facts are reported rather than selecting one and hiding the other.

Allocation qualification: **FAIL**. This is an intermittent candidate-associated allocation witness, not a newly proven production managed-allocation defect, CLR accounting effect or test bug. Equal magnitude across gates does not prove one mechanism.

## Focused acceptance

**Not run:** the required clean 5/5 prerequisite failed. No post-matrix exact-event, translation, rigid rotation, M14.2–M14.6, ReferenceFrames or Precision focused commands were executed. Full-suite runs 3 and 4 did execute and pass their registered groups, including exact-event motion; these do not replace the conditionally required focused acceptance sequence.

## Physical / numerical evidence

Historical Debug 38/38 remains retained. Accepted evidence includes 64 canonical parity cases, three refusal/default-output comparisons, 1,000 exact-duration controls, 120 rational translation oracle cases, independent rotation comparisons, determinism, boundaries/refusals and immutability.

The two clean Release full suites again reported 64 canonical parity cases; maximum translation position difference 0.0001220703125 m at the large-coordinate fixtures; velocity difference 0; mixed rotational quaternion difference 2.784314975363703E-15. No numerical failure was observed in reached assertions. The incomplete runs do not validate their unreached tests.

Exact requested epoch identity remains distinct from approximate FP64 physical state. No result is relabeled contact certification. No independent new architecture qualification was performed after the matrix failed.

## Authority / immutability

Accepted source remains unchanged: immutable, transient, derived, read-only coherent motion from existing authority. StateRevision is provenance, not mutation permission. No mutation/publication/discovery/contact proof/event-local execution was added. No new authority defect was observed in reached tests; the full candidate remains unaccepted because allocation qualification failed.

## Performance

**RELEASE PERFORMANCE UNQUALIFIED.**

Canonical median/P95/P99: NOT RUN.
Rational median/P95/P99: NOT RUN.
Representative batch median/P95/P99: NOT RUN.
Dedicated warmed performance-allocation qualification: NOT RUN.

These gates were conditional on clean 5/5 plus focused acceptance. Ordinary timing output emitted by the full suite is retained only as normal test output, not used as substitute candidate performance qualification.

## VERIFY A — clean execution

Five distinct PIDs; no retries; normal uninstrumented source/binary; fixed environment and runtime; original warmup/order/thresholds; no observer registrations or preparation. All three failures remain in the matrix. Comparison quality passes; candidate acceptance does not. Checked entry/exit succeeded at each failing allocation gate.

## VERIFY B — witness interpretation

Do not claim 12,336 was fixed, harmless or definitely CLR. Fresh normal acceptance reproduced it three times. The current result supports a repeated SAS signal and a broader migrating pattern, but provides no object-level causality. The positive-control evidence from the prior attributed runs cannot identify these new normal-run failures.

## VERIFY C — physical / architecture

No production/test change or numerical/authority failure was observed. Runs 3 and 4 reached the unchanged exact-event tests; other runs did not. No additional architecture investigation was authorized after acceptance failed. Allocation uncertainty neither clears nor automatically rejects the exact-event architectural responsibility.

## Historical witness disposition

**UNRESOLVED HISTORICAL INTERMITTENT WITNESS — REPRODUCED IN FINAL CLEAN RELEASE ACCEPTANCE.**

No production managed-allocation defect was established. Residual uncertainty is not accepted by this matrix; the hard 5/5 bar failed. Preserve old and new witnesses without rewriting history.

## Engineering-rule update

**Not performed.** The durable rule edit was authorized only upon technical PASS. This ticket adopted the interpretation while leaving the existing engineering rules unchanged.

## Evidence and Git

This external acceptance package contains the report, fixed preflight source/binary/environment identities, five-row results, bounded normal stdout/stderr witnesses and the reproduction runner. No new forensic comparison tree or tracing output was created. Earlier scratch remains untouched; no cleanup or blocked deletion retry occurred.

All 14 canonical candidate fingerprints and 167 current source-closure fingerprints remain unchanged. Output binaries stayed fixed throughout all five runs. `git diff --check` PASS, with existing LF/CRLF warnings only. Working status remains the original 8 modified tracked files and 6 untracked candidate files, nothing staged. No commit/tag/merge/push/bank, no milestone assigned.

## Next decision

Return the exact observed pattern to Project Control for payoff reassessment. The repeated SAS signal meets the ticket's bar for a targeted attribution **decision**; attribution is not automatically authorized or executed. The additional attitude failure must remain part of that decision.

Proposed accomplishment title remains:

**Evaluate coherent spacecraft motion at exact physical-event epochs**

STOP FOR PROJECT CONTROL.
