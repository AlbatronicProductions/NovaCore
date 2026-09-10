# Candidate production-component causal discriminator

**Final classification: COMPONENT DISCRIMINATOR INCONCLUSIVE**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

The unchanged candidate passed 3/3 disposable normal Release processes. The canonical-kernel-restored control failed SAS at 12,336 bytes twice, then passed once. Restoring the two banked canonical method bodies did not eliminate the witness. No necessary candidate component was isolated; no production correction follows from this comparison.

## Baseline

- Canonical branch: `codex/exact-event-spacecraft-motion`.
- HEAD/main/origin/main: `00f226096b61a117184464b5619e464d39789574`.
- M14.6: `m14.6-physical-event-epochs` -> `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- All M14.1–M14.6 tag targets unchanged; exact values in `preflight.json`.
- SDK 10.0.303. All six processes loaded the same .NET 10.0.12 coreclr.dll.
- All 14 accepted canonical file fingerprints and 167 canonical source-closure fingerprints remained unchanged. The only production edits were the two reviewed method substitutions in the disposable K control.

## Why further work was justified

Final normal candidate acceptance was 2/5 clean: SAS 12,336 in runs 1 and 2, clean runs 3 and 4, attitude-integration 12,336 in run 5. Earlier candidate witnesses also included impulse-evaluation after clean attitude. Earlier matched banked processes were clean. This justified a production-component subtraction, without treating the gate as the owner.

The prior observer's two attributed candidates were clean and did not expose a 12,336-byte population. Its observed perturbation/non-reproduction makes repeating it a poor first discriminator. No observer, tracing or special initialization was used here.

## Candidate production diff map / causal grouping

The complete method-level map is in `component-map.md`.

Actual production scope: **four modified tracked files plus one new file**. Tracked diff: 127 additions / 17 deletions; the new PhysicalEventDuration file is additional.

| Component | Methods / responsibility | Before historical witnesses? | First-control handling |
|---|---|---|---|
| T canonical | Integral translation entry -> extracted constant-force Evaluate kernel | Yes; translation/contact groups 24–27 precede attitude integration 28 and SAS 32 | Restore banked integral entry body; keep candidate shared kernel and fractional entry |
| R canonical attitude | Integral attitude entry -> EvaluateConstantRate quaternion kernel | Yes; attitude group 23 and later canonical rotation/integration | Restore banked attitude entry body; keep candidate shared kernel |
| R exact endpoint | New PhysicalEventEpoch rigid-body overload and fractional RK4 tail | Not shown before failures; new exact-event group is 38 | Retain unchanged; canonical rigid-body implementation was already unchanged |
| M exact motion/result | Exact coherent-motion overload, immutable SpacecraftPhysicalEventMotion record struct | Added API/test body not shown before failures; existing canonical motion body unchanged | Retain unchanged |
| D exact difference | PhysicalEventDuration exact bounded arithmetic and value representation | Fractional paths/new last test group; no demonstrated pre-failure invocation | Retain unchanged |

Shared kernels are class C in the requested inventory; changed integral entries A; added physical-epoch paths B; new representation D. No other production component was discovered. Metadata/code layout may affect runtime state without invoking added rational APIs, but precise load/JIT/initialization times were not measured.

## Structural control design

- **C:** unchanged candidate source, full 38-group suite.
- **K:** copy of C with exactly two complete canonical SimulationInstant method bodies replaced by the bodies from banked main: SpacecraftTranslationEvaluator.TryEvaluate and SpacecraftAttitudeEvaluator.TryEvaluate.

Those banked bodies are also unchanged between M14.6 and current main. Each restoration is source-exact; the surrounding text of each file is verified unchanged. All exact-event-only code, shared helpers, types/APIs, project configuration and new test registration remain present.

No dummy result, alternate arithmetic, omitted test or invented fallback was needed. All permanent tests and OrdinaryAllocationMeasurement are byte-identical between C, K and the canonical checkout. The exact-event group remains last. The restored bodies naturally serve canonical delegation from exact-event APIs, while fractional calls retain the candidate helpers. This is a diagnostic control, not a proposed permanent duplication of the kernels.

Both Release builds passed with zero warnings/errors before execution. Read-only PE inspection verified identical named type/method/parameter-count shape across both assemblies, including the new exact-event test and production types. This is not a claim of identical machine code; the two canonical methods deliberately differ.

## First matrix

| Seq | Arm | PID | Result | First allocation gate | Bytes | Entry | Exit | Exact-event group |
|---|---|---:|---|---|---:|---|---|---|
| 1 | C — unchanged | 46356 | 38/38 PASS | None | 0 | PASS | PASS | PASS |
| 2 | K — canonical restored | 44472 | FAIL; 31/38 passed | sas-proof | 12,336 | PASS | PASS | UNREACHED |
| 3 | C — unchanged | 43340 | 38/38 PASS | None | 0 | PASS | PASS | PASS |
| 4 | K — canonical restored | 47740 | FAIL; 31/38 passed | sas-proof | 12,336 | PASS | PASS | UNREACHED |
| 5 | C — unchanged | 43968 | 38/38 PASS | None | 0 | PASS | PASS | PASS |
| 6 | K — canonical restored | 35520 | 38/38 PASS | None | 0 | PASS | PASS | PASS |

All six were fresh normal processes, C -> K alternating. Same inherited environment hash, runtime and runtimeconfig. No selectors, retries, pre-JIT, pre-touch, extra warmup, observer/profiler registration or builds during execution. Per-arm binary hashes remained fixed.

Each failed control reported the existing `sas-proof: expected zero managed allocation, actual=12336` assertion, with successful checked no-GC entry and exit. Completed assertions passed; assertions after the allocation failure and later groups were unreached. Unprinted operands remain unavailable. These exception stacks locate the assertion, not an allocating instruction.

No non-allocation failure was observed. The successful K run reached all 38 groups, including the unchanged canonical parity, rational translation/rotation and exact-event tests. This provides a bounded behavioral check of the control's preserved feature and workloads; it does not clear candidate acceptance.

## Optional second discriminator

**Not executed. Six of the maximum twelve normal processes used.**

The necessary-component branch was not reached: the restored control itself reproduced the witness, while the matched unchanged candidate did not. Translation-versus-attitude subtraction would not refine a demonstrated necessary combined component. The remaining exact-only APIs/result/duration dependencies cannot simply be removed without breaking unchanged tests or inventing substitute implementation. No clearly justified second clean subtraction was identified. The budget was not spent automatically.

## Necessary component result

**None established.**

The 12,336 witness can occur in this control with the banked canonical entry mechanisms restored. Therefore retaining the candidate's canonical extraction calls is not required for that control's observed counter failure. This does not identify which remaining component causes it or establish that its allocating mechanism is identical to any historical witness.

This is neither the requested clean-control/failing-candidate separation nor disappearance in both arms. The overall responsibility attribution remains inconclusive. It would be incorrect to label the canonical extraction component necessary or to nominate another component from presence alone.

## Static lifecycle analysis

No necessary component was proven, so no further allocator/lifecycle investigation was opened. The diff inventory found no new static cache, delegate, closure or table in the changed successful numerical kernels. Added arithmetic/result construction uses value types; existing exception/status behavior and method/JIT shape remain possible influences, not attributed causes.

The two method restorations alter call and compilation structure intentionally. Native addresses, optimization decisions and runtime first-use behavior were not traced. A source-level absence of explicit reference-object allocation is not proof of zero managed allocation, and source size does not exonerate a component.

## Narrow observer

**Not executed.** Necessary component isolation was not achieved, so the conditional observer authorization was not reached. No M14.2/M14.6 observer, EventPipe or GC/JIT tracing used.

## 12,336 process-level interpretation

SAS, attitude and impulse remain witness locations, not separate investigations. Here the witness appeared twice in K at SAS. Historical C association remains retained, but this matrix's C arm was clean. No allocating object population or runtime-accounting mechanism was identified. Magnitude equality alone is not a causal proof.

The earlier failed 2/5 final acceptance is not replaced by this 3/3 diagnostic candidate arm. The canonical candidate remains unbanked and follow-up required.

## Candidate architecture implication

The required read-only coherent-motion result at exact PhysicalEventEpoch remains intact. The control retains exact duration differencing, canonical parity, constant-force translation, analytical/RK4 rotation with fractional endpoint, immutable result and provenance. No mutation, publication or contact certification was introduced or removed.

Restoring these canonical bodies is not a supported production remedy on this evidence: K itself failed. Do not degrade or remove exact-event support to obtain a passing allocation measurement.

## KSA lifecycle comparison

**Not performed.** No lifecycle/first-use owner was isolated; a KSA comparison would not resolve the observed structural result within this ticket.

## VERIFY A — structural validity

Strongest challenge: method restoration changes generated code and layout, potentially affecting process timing beyond the immediate workload. That limits causal specificity, but is the intended structural variable rather than an undisclosed test/environment change.

Counterchecks: only two declared production method bodies differ; outside-method content is unchanged; all tests/helper/order/warmup/thresholds match; new candidate APIs/types remain; binary named-member shape matches; both configurations build; K's final full pass exercises the exact-event body. No evidence invalidates the comparison as a structural control. It still does not identify an allocator.

## VERIFY B — signal interpretation

Strongest challenge: C is clean here, so this matrix does not reproduce the candidate side of the intended failing-C/clean-K separation. K's two failures demonstrate survival under restoration, not necessity of an untested remaining component. Intermittent process behavior, layout effects and remaining candidate/common state remain plausible. **COMPONENT DISCRIMINATOR INCONCLUSIVE** survives; stronger component attribution does not.

## VERIFY C — candidate outcome

No correction implemented. Any future correction must retain exact epoch and duration semantics, physical models, canonical parity, immutable read-only result and authority boundaries. Neither reverting the feature nor accepting this 3/3 as a substitute for required clean acceptance is justified.

## Recommended bounded correction

**None justified by this experiment.** Return the counterexample to canonical-restoration necessity and the remaining component map to Project Control. A new comparison or attribution scope requires a renewed payoff decision; do not automatically profile, specialize kernels, add warmup or remove APIs.

## Performance status

**RELEASE PERFORMANCE UNQUALIFIED.** No candidate performance qualification or benchmark was run. Timing printed by normal full tests is not substituted for qualification.

## Evidence and reproducibility

Retain this report, `component-map.md`, `prepare.ps1`, `matrix.ps1`, `preflight.json`, `first-controls.json`, `first-matrix.json` and `result.json`. They preserve the exact two banked method bodies/hashes, source identity, API-presence checks, complete normal-process outcomes and reproduction procedure. Bulk builds/raw normal logs remain disposable, not permanent evidence.

Reproduction after separate authorization: source closure and accepted fingerprints come from the retained clean-acceptance preflight. `prepare.ps1` creates reviewed C/K copies, restores only those two banked methods, and verifies all other content. Build each Release test project before the matrix; then the runner executes the six declared processes with no overrides. Use a newly reviewed destination rather than overwriting completed evidence. Current scripts refuse overwriting their prepared tree/matrix.

## Disposable output

| Reviewed path | Files | Logical bytes |
|---|---:|---:|
| E:\NovaCore\.codex\exact-event-sas-ab | 557 | 44,067,060 |
| E:\NovaCore\.codex\exact-event-attribution | 290 | 21,478,493 |
| E:\NovaCore\.codex\exact-event-components | 552 | 39,650,561 |
| Total | 1,399 | 105,196,114 |

All three absolute paths are within E:\NovaCore\.codex and contain no discovered reparse points. They are dedicated comparison/observer/build trees; no canonical source, accepted evidence package, Git storage or production asset path is included. No cleanup was executed or blocked deletion retried.

One consolidated **manual** command for Project Control after reviewing/accepting the retained result:

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\.codex\exact-event-sas-ab', `
  'E:\NovaCore\.codex\exact-event-attribution', `
  'E:\NovaCore\.codex\exact-event-components' `
  -Recurse -Force
```

The external NovaCore-Research reports and scripts are retained separately and are not cleanup targets.

## Git

Canonical branch/status unchanged: original 8 tracked modifications and 6 untracked candidate files, zero staged. Canonical production/tests/helper unchanged. All 14 accepted canonical fingerprints, all 167 source-closure fingerprints and declared C/K source/binary hashes verified after execution. M14.1–M14.6 targets unchanged. `git diff --check` PASS with existing LF/CRLF conversion warnings only. No stage, commit, merge, tag, push, bank or milestone assignment.

STOP FOR PROJECT CONTROL.
