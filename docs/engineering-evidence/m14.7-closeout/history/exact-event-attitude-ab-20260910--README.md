# Release attitude-integration allocation discriminator

10 September 2026. **INTERMITTENT CANDIDATE-ASSOCIATED ATTITUDE SIGNAL**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**. Candidate remains UNBANKED.

The predeclared six-process matrix completed without retries:
baseline **0 / 0 / 0**; candidate **0 / 12,336 / 12,336** attitude-counter bytes.
All target entry/exit and non-allocation assertions passed. This is candidate-only
association in this bounded sample, not stable separation or mechanism attribution.
No production, permanent test, helper, canonical documentation or candidate evidence
file was modified. No profiling, GC/JIT tracing, attribution or implementation.

## Baseline and prior state

HEAD/main/origin/main remain `00f226096b61a117184464b5619e464d39789574`.
Branch: `codex/exact-event-spacecraft-motion`.
M14.6 tag `m14.6-physical-event-epochs` remains at
`d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
M14.1-M14.6 tags unchanged. SDK 10.0.303; every child loaded
`C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/coreclr.dll`.

Prior candidate Debug full Simulation passed 38/38. The earlier canonical Release
acceptance had a SAS 12,336-byte witness. The later SAS discriminator stopped on
its candidate arm at attitude-integration, also 12,336 bytes. Those witnesses
remain distinct and unresolved. This new experiment targets attitude integration
only; it neither resumes the SAS matrix nor completes candidate acceptance.

## Pre-attitude reachability audit

| Candidate difference | Classification relative to target | Interpretation |
|---|---|---|
| Translation evaluator's extracted analytical kernel | DIRECTLY EXECUTED BEFORE ATTITUDE GATE | Existing translation and contact groups call canonical evaluation. No analytical formula change is intended. |
| Attitude evaluator's extracted constant-rate kernel | DIRECTLY EXECUTED BEFORE ATTITUDE GATE | Existing spacecraft attitude and other consumers call it; the target's own loop also calls canonical attitude evaluation. |
| Rigid rotation evaluator file | LOADED / JIT-REACHABLE BEFORE ATTITUDE GATE | Existing canonical body is unchanged and exercised by earlier translation/contact work. The added rational entry is NOT REACHED by common groups. |
| Coherent-motion evaluator file | LOADED / JIT-REACHABLE BEFORE ATTITUDE GATE | Existing canonical method is exercised before target. Added TryEvaluateAtPhysicalEpoch and new result construction are NOT REACHED by common groups. |
| PhysicalEventDuration | NOT REACHED | Used only by added rational paths; its presence changes assembly metadata, not proof of execution. |
| Three command-line selectors | DIRECTLY EXECUTED BEFORE ATTITUDE GATE | Contains checks run at entry; no arguments select their bodies. |
| Appended exact-event group delegate | REGISTERED BUT BODY NOT EXECUTED | The whole tuple/delegate array is built before group one. Its extra entry exists before target, even though its body is last. |
| Exact-event test static fields | LOADED / JIT-REACHABLE BEFORE ATTITUDE GATE | Compiled type is BeforeFieldInit; early field initialization is permitted, actual timing unobserved. Source initializers construct value-type IDs/properties/states. No explicit static constructor in source. |
| Exact-event test Run/oracles/new generic uses | REGISTERED BUT BODY NOT EXECUTED / NOT REACHED | BigInteger oracle, generic reference-containment check and performance code are inside unexecuted bodies. No new production generic framework; existing tuple/Action registration machinery remains. |
| Current-state/handoff/contract/evidence changes | DOCUMENTATION ONLY | Excluded from the four-project comparison closure; not runtime inputs. |

Reachability is source/registration context, not a statement about JIT allocation,
managed objects or the origin of the reported bytes. No runtime method/type-init
trace was collected.

## Permanent attitude-integration contract

Group **Spacecraft attitude integration**, method
`SpacecraftAttitudeIntegrationTests`, candidate Program.cs:189-210. Baseline and
candidate method bodies are identical after CRLF/LF normalization; checked helper
is identical. Fingerprints are in `preflight.json`.

Setup creates craft 44, carrier/body frames 2/3, identity initial attitude at tick
zero, constant angular velocity (0,0,pi), an immutable declaration/store, a
three-node frame graph, and a preallocated three-result frame array. Canonical
replacement setup commits a 0.25-radian X orientation at tick zero and exercises
existing transaction rejection contracts.

Immediate warmup is **one view.TryGetAttitude lookup**, unchanged. Earlier setup
evaluations/transactions also remain in their original order. The helper reserves
**1 MiB** with `disallowFullBlockingGC: true`; entry false throws. It opens
GetAllocatedBytesForCurrentThread only after successful entry. The measured window
initializes the hash and executes **100,000 iterations** (indices 0..99,999):
lookup, canonical attitude evaluation at the index's SimulationInstant, canonical
frame extraction into the existing result array, and mixing quaternion-W bits.
Each iteration asserts lookup and both evaluation statuses.

Complete closes the per-thread counter before finally calling EndNoGCRegion.
Using cleanup covers exceptions. Entry/exit failure is fatal, with no retry or
fallback. RequireZero independently requires **exactly 0 bytes**. Hash reporting
occurs only afterward; there is no extra exact-hash or loop-count assertion to
invent. No work, warmup, ordering, threshold or boundary changed.

Shared positive control is `OrdinaryAllocationMeasurement.PositiveControl`, using
byte[128], later in the full-suite Allocation group. No local target control or
additional invocation was introduced.

### Independent non-allocation predicates — all six runs

Reaching the completed target-counter report proves every preceding and every
per-iteration Check returned successfully. Individual unprinted numbers remain
UNAVAILABLE; no reporter was inserted inside or outside the test method.

| Predicate | Result in runs 1-6 |
|---|---|
| Initial attitude creation success | PASS |
| Store creation succeeds, store non-null, status Success | PASS |
| Count 1, definition found, body frame matches, stored attitude equals initial | PASS |
| Duplicate craft rejected with DuplicateSpacecraftId | PASS |
| Frame extraction status Success | PASS |
| Frame translation and origin velocity exactly zero | PASS |
| Parent angular Z differs from pi by <1e-12; forward Y differs from 1 by <1e-12 | PASS; raw values unreported |
| Replacement state creation and pure transaction creation succeed | PASS |
| Commit succeeds, processed attitude count 1, revision 1, current state equals replacement | PASS |
| Stale replacement rejected with StateRevisionMismatch | PASS |
| No-op replacement returns ReplacementNoOp | PASS |
| Mismatched time returns TimeMismatch | PASS |
| All 100,000 lookups succeed | PASS |
| All 100,000 attitude and frame evaluations succeed | PASS |

In target-zero runs 1,2,3,5 the report hash is `0xE41DB63F425CA55C` in each.
Runs 4 and 6 fail allocation before hash reporting, so their final hash is
UNAVAILABLE, not assumed equal.

## Comparison controls

The verified prior isolated trees at
`E:/NovaCore/.codex/exact-event-sas-ab/B` and `/C` were reused to avoid another
duplicate build tree. Their original 165/167 source files were fingerprint-checked
(332 total), including the embedded lunar fixture. Baseline is the exact stated
Git archive dependency closure; candidate overlays the seven retained source/test
files. Text comparison normalizes line endings only; binary inputs match bytes.

`prepare.ps1` reverified refs, tags, all 14 existing canonical changed/untracked
files and common source, then built both Release arms before the matrix; both
builds passed with zero warnings/errors. The baseline has 37 groups and candidate
38; the same first 27 groups precede target. New group is last. No files in either
comparison arm were instrumented or patched.

Every child uses the same absolute dotnet executable, identical relative DLL
argument, its arm root as working directory, and matching inherited environment
hash. No command-line selector, profiler, tiering/PGO/GC override or acceptance
retry. Runtimeconfig hashes match. Source and build hashes were checked before
each child. One external process-module inventory read identifies loaded coreclr;
no managed/runtime observer was attached. This can perturb scheduling, but both
arms use the identical parent mechanism; no absence-of-perturbation claim is made.

The matrix continues target allocation-only failures. A later allocation-only
failure does not alter the already completed target observation and is recorded
incidentally. Earlier failure, target non-allocation failure, no-GC entry/exit
failure, changed environment or source contamination would stop it. No such hard
stop occurred. No build or retry occurred during the matrix.

## Six-run matrix

| Seq | State | Groups before target | Reached | Attitude bytes | Entry | Exit | Other attitude contracts | Exact-event body before target | Process result |
|---|---|---:|---|---:|---|---|---|---|---|
| 1 | B | 27 | Yes | 0 | PASS | PASS | PASS | No; absent | 37/37 PASS; exit 0 |
| 2 | C | 27 | Yes | 0 | PASS | PASS | PASS | No | Later impulse-evaluation failure; 35 pass, 1 fail, 2 unreached |
| 3 | B | 27 | Yes | 0 | PASS | PASS | PASS | No; absent | 37/37 PASS; exit 0 |
| 4 | C | 27 | Yes | 12336 | PASS | PASS | PASS | No | Attitude allocation failure; 27 pass, 1 fail, 10 unreached |
| 5 | B | 27 | Yes | 0 | PASS | PASS | PASS | No; absent | 37/37 PASS; exit 0 |
| 6 | C | 27 | Yes | 12336 | PASS | PASS | PASS | No | Attitude allocation failure; 27 pass, 1 fail, 10 unreached |

Failed processes exited -532462766. Target failures report Program.cs:209.
Run 2's separate incidental witness, preserved without investigation:

```text
ORDINARY_ALLOCATION gate=impulse-evaluation bytes=12336 entry=PASS exit=PASS
impulse-evaluation: expected zero managed allocation, actual=12336
CelestialImpulseEventTests, candidate Program.cs:996
```

That failure occurs after target and thus is outside the specified earlier-group
stop rule. All six target observations are available. No follow-up on impulse
allocation, SAS attribution or mechanism equivalence was performed.

## Classification and causal limits

Baseline: **0 / 0 / 0**.
Candidate: **0 / 12,336 / 12,336**.

**INTERMITTENT CANDIDATE-ASSOCIATED ATTITUDE SIGNAL**.

This bounded sample meets Decision D. It does not prove stable separation, an
allocating method/object, a helper defect, a CLR mechanism, or that the baseline
can never fail. Candidate association is intermittent; the next step requires
Project Control payoff reassessment. The target window was unchanged, and all
non-allocation predicates passed even on its two nonzero observations.

SAS status: **UNRESOLVED — DEFERRED UNTIL EARLIER GATE IS CLEARED**.
The unmodified suites can naturally reach SAS, but no SAS matrix was resumed or
classified. Equal attitude/SAS/impulse magnitudes do not establish a shared cause.
The old raw-counter 8,160 explanation is not applied.

## VERIFY A / VERIFY B / next action

**VERIFY A:** current refs, source identity, same method/helper, common group order,
same runtime/environment and six fresh processes are verified. No contamination,
rebuild, retry or in-process instrumentation. The incidental later failure does
not invalidate the completed attitude window; it does prevent calling the
candidate full suites successful.

**VERIFY B:** the strongest alternative is an intermittent process-state/runtime
effect whose baseline incidence was missed by three samples. Nothing in this
experiment disproves it. Retain the exact intermittent association classification;
do not upgrade it to production causality or stable candidate-only allocation.

Return to Project Control for payoff reassessment of this intermittent signal.
No implementation, automatic profiling, extra runs or renewed acceptance plan is
started. The exact-event architecture remains unchanged and unbanked.

## Evidence, reproduction and disposable output

Retained here: `prepare.ps1`, `matrix.ps1`, `preflight.json`, `controls.json`,
`matrix.json`, `result.json` and this report. Complete source and canonical hashes
remain in the referenced prior sibling evidence package; its preflight file hash
is recorded here. `controls.json` fingerprints loaded build inputs, while
`matrix.json` preserves all six outputs/identities. No bulk profiler artifact.

The scripts document the exact commands and stop rules. Matrix execution refuses
an existing result file: this is a reproduction record, not permission to retry.
Any future run needs explicit authorization and a new reviewed evidence location.

The reused comparison root now contains **557 files / 44,067,060 bytes**, including
the previous 543 files / 43,994,366 bytes. This ticket added only **14 raw build/run
logs / 72,694 bytes**; it did not add another source/build tree. No deletion was
attempted and the prior policy-blocked deletion was not retried.

Consolidated manual cleanup path after Project Control review:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\exact-event-sas-ab' -Recurse -Force
```

Canonical Git status is unchanged: eight modified tracked paths, six untracked
candidate files, nothing staged. All seven accepted production/test fingerprints
and all 14 canonical file fingerprints remain unchanged. No refs moved.
`git diff --check` PASS; line-ending warnings only.

**STOP FOR PROJECT CONTROL.**
