# Independent verification and qualification stop

Three read-only reviewers inspected separate responsibilities. The implementation lead was the only writer. No proprietary KSA code/assets/constants were introduced; the accepted architecture provenance remains in the unchanged sibling package. No broad KSA research was reopened.

## Red-team matrix

| Attack | Evidence / result |
|---|---|
| 1. Exhaust reaction counted twice | One net force/mass derivative, no added rocket impulse; analytic oracle PASS. |
| 2-3. Constant burn mass or endpoint-only mass patch | Exact dyadic stage mass; source-mass alternative fails independent oracle. |
| 4. Frozen root thrust | Each RK stage rotates body force; independent scalar-angle/Simpson witness PASS. |
| 5-6. Interior/tiny event erased | Exact segment branch plus scaled duration and cumulative weighted sums; three one-epsilon complete publications PASS. |
| 7. Segments reversed | Powered integration precedes coast; analytic interior exhaustion and exact successor tests PASS. |
| 8. Wrong stage mass | BigInteger independent exact stage-weight/rounding controls PASS. |
| 9. Quaternion/frame sign | Banked Hamilton body-rate convention; torque/rotating reference PASS. |
| 10-11. Motion/resource partial commit | Prepared shared owner transaction installs one coherent successor; nonmutation and postcommit seam tests PASS. |
| 12-13. Actual state/revisions desynchronized | Applied frontier increments actual/state once; resource only positive consumption; terminal and no-output cases PASS. |
| 14. Lost endpoint validity | Distinct endpoint-only payload, legacy replacement/query fences including attitude shadow and signed-zero tests PASS. |
| 15-16. Replay or early lease retirement | Genuine seal/generation, precommit retention, resource cancel/reissue, parent retirement, consumed replay tests PASS. |
| 17. Postcommit rollback | Forced physical and host acknowledgement failures preserve committed authority and invalidate continuation; PASS. |
| 18. Backlog uses old mass/resource | Every interval consumes current acknowledged successor; all five host partitions match reference. |
| 19. Allocation | Seven focused measured windows exactly zero in Debug and Release; byte[128] positive control 152. Full Release later failed the existing clock gate at 12,336. Overall allocation acceptance incomplete. |
| 20. Contact accidentally participates | No contact/BEPU object in physical evaluator or development fixture; protected contact production paths unchanged. |
| 21. Visible bypass | Scene builds pose only from copied committed endpoint; presentation tests PASS; manual acceptance pending. |
| 22. Shared oracle defect | Closed-form logarithmic translation and scalar-angle/Simpson reference are independent of production RK4. |
| 23. Postselected error bar | Numerical contract recorded before trajectory results. Reviewer tightened admission arithmetic, not thresholds. |
| 24. Hidden tail | All three fresh process percentiles/maxima/worst indices retained; mixed powered/coast timing disclosed; no attribution of runtime tail cause. |

Final source review judgments: numerical PASS for admitted domain; authority/lifecycle/history PASS; presentation/scope PASS. These are scoped reviews, not overall candidate acceptance.

## Corrections during implementation

The numerical review found the inward-rounded angular admission bound; directed magnitude checks and a conservative angular-envelope inflation replaced it. Lifecycle review found result reporting that omitted accepted credit/published prefixes, a joint-proposal cancellation gap, and incomplete first-record reconstruction. The candidate now reports canonical commitment for those cases, supports explicit private retirement, invalidates irreversibly retired parent continuation, and retains immutable initial provenance plus per-record debt facts. Focused suites passed, with dedicated angular-rounding, cancel/reissue and initial-history cases. Nonfinite cold-torque rejection and AcceptedCredit.CanonicalCommitted were source-reviewed; dedicated assertions for those exact two cases are not present in the current tests. No tests were added after the full-suite stop.

A launcher regression separately caught a new label's single CP1252 dash byte inside a UTF-8 source file. The compiler then misread pre-existing Unicode labels. Only that new byte was replaced with an ASCII hyphen; old label bytes stayed intact. Release launcher rebuild and all 17 tests passed. This was a mechanical presentation-label correction, not a physics or numerical-policy change.

## First material acceptance failure

Invocation: `dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll`, one fresh ordinary process after the focused tests and three timing processes. SDK 10.0.303, runtime 10.0.12. No profiler, GC trace, tiering override or EventPipe was attached.

Eight registered groups passed: Segmented powered free flight; SimulationInstant; SimulationDuration; SimulationRate; Solar UTC and speed presets; Event ordering; Internal physical-event epochs; Timeline topology.

The ninth registered group, Simulation clock, then threw:

```text
Unhandled exception. System.InvalidOperationException: clock: expected zero managed allocation, actual=12336
FULL_SIMULATION_EXIT=-532462766
```

Expected allocation=0, observed delta=12,336. `OrdinaryAllocationMeasurement.Complete()` returned before `RequireZero` threw, so checked region entry and exit succeeded. The window performs 100,000 repetitions of AdvanceTo(5), AdvanceUntilNextEvent, Pause, Resume and same-rate TrySetRate after the existing warmup. That body matches HEAD after LF normalization, SHA-256 `5F2C022831447C3BCC4D594586B04724BE83F659E39E1C37C512F0BFF33B9082`. The helper and clock production files match preimplementation raw hashes. The runner gained a new first group; full process history therefore differs.

This is an allocation-counter failure witness, not an identified managed object allocation or a proven accounting artifact. Object type, count, stack, runtime mechanism, repeatability and candidate causality are UNRESOLVED. No inference is made from the familiar byte magnitude or historical failures. The previous complete Debug run passed 67/67, including this gate. Fifty-eight Release groups were unreached.

The lead stopped immediately: no retry, A/B, profiling, warmup/tiering/threshold change or follow-on broad validation. Source remained frozen; only concise evidence and identity checks were completed afterward.

## Remaining acceptance

Full Release Simulation must be resolved under a separately authorized bounded decision. Remaining ReferenceFrames/Precision/dependency focused runs, final Debug launcher/final solution rebuild after the label-only correction, and manual visible acceptance are not claimed. No additional source correction or qualification campaign is authorized by this report. Lead judgment **REVISE**; candidate **UNBANKED, FOLLOW-UP REQUIRED**.
