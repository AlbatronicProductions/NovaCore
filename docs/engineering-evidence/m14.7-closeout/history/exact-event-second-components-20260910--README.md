# Second and final exact-event structural discriminator

**EXACT-EVENT-ONLY PRODUCTION CAPABILITY IS NOT NECESSARY FOR 12,336 PHENOMENON**

**Lead judgment: ESCALATE TO PROJECT CONTROL.** The authorized comparison completed. Candidate acceptance remains FOLLOW-UP REQUIRED. No third structural discriminator, correction or acceptance rerun is authorized by this result.

## Baseline

- HEAD/main/origin/main: `00f226096b61a117184464b5619e464d39789574`.
- Canonical branch: `codex/exact-event-spacecraft-motion`.
- M14.6: `m14.6-physical-event-epochs` -> `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- M14.1–M14.6 targets unchanged; exact targets retained in preflight.json.
- SDK 10.0.303. All six fresh children loaded `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
- Full inherited environment SHA-256: `E6BA15A56660573C429F8C664E2291B655AE4B755F62B28837051D17AE7A7B2B`, identical to failed normal candidate acceptance and the preceding banked-baseline matrix.
- Runtime configuration, no-argument invocation, root-relative working-directory semantics and ordinary 1 MiB checked reservation unchanged. No environment overrides, observer, profiler, tracing, extra warmup or special child initialization.

## Why this discriminator has payoff

The retained banked current-main population passed 5/5 normal Release processes, with every ordinary zero-required gate at zero and the existing positive control at 152 bytes. Candidate E's normal acceptance was 2/5 clean. The first component experiment passed 3/3 in the unchanged candidate arm but only 1/3 in the candidate-derived canonical-restoration control, with two SAS 12,336 witnesses. No necessary component had been isolated.

This second and final subtraction therefore preserved canonical candidate refactoring while removing the complementary exact-event-only responsibility. No banked-baseline process was rerun here.

## Reconciled component map and exact boundary

Paths below are relative to the source snapshot for each disposable arm.

| Component | Candidate C | Control X- |
|---|---|---|
| Spacecraft/Translation/SpacecraftTranslationEvaluator.cs | Canonical kernel extraction/generalization and PhysicalEventEpoch overload | Keep canonical wrapper and private Evaluate kernel exactly; remove only PhysicalEventEpoch overload |
| Spacecraft/SpacecraftAttitudeEvaluator.cs | Canonical constant-rate kernel extraction/generalization | Entire file byte-identical to C; kernel remains a canonical consumer |
| Spacecraft/Rotation/SpacecraftRigidBodyRotationEvaluator.cs | Canonical rotation plus exact-event endpoint and fractional-tail dispatch | Remove only exact-event overload; preserve canonical evaluator, validation and shared TryRk4Step implementation exactly |
| Spacecraft/Translation/SpacecraftMotionEvaluator.cs | Canonical coherent result/evaluator plus exact-event overload/result | Remove TryEvaluateAtPhysicalEpoch and SpacecraftPhysicalEventMotion; retain canonical coherent evaluator/result |
| Time/PhysicalEventDuration.cs | Bounded exact difference arithmetic | File absent from X- source/build; no canonical consumer |

The production change is four file responsibilities: three files with exact-only spans removed and one exact-only file omitted. Attitude is deliberately untouched. No blanket rollback to main, shared RK4 removal, algorithm specialization or arbitrary code-size reduction occurred.

`preflight.json` retains each exact removed text span, source identities and boundaries. `prepare.ps1` reproduces the six-path C/X difference: four production paths plus the disposable runner and project file. The retained three production files reconstruct C text exactly when the removed spans are reinserted; all other source bytes match C. Source control diff summary before builds: 6 paths, 3 inserted lines / 142 deleted lines (including test dispatch/project differences).

## Disposable test handling

C retains all 38 candidate groups. X- retains all 37 common groups in the same order, omitting only the final Exact-event spacecraft motion registration.

The focused exact-event test source itself remains byte-identical on disk in X-, but a single `Compile Remove="SpacecraftPhysicalEventMotionTests.cs"` excludes it because its intentionally unavailable APIs cannot compile. The two existing exact-event selector predicates remain present; their unavailable bodies throw NotSupportedException. Neither branch executes during these no-argument runs. All other selectors, common test bodies, warmups, workloads, thresholds and helper bytes are identical.

No canonical test or project was edited. No common test was omitted, re-ordered or weakened. Existing internal physical-event epoch tests remain in both arms; only spacecraft exact-event evaluation is removed.

## Pre-gate equivalence and limitations

All common group registrations/order and all source from the runner loop through the common test implementations match exactly. Candidate C retains its original 167-file source closure; X- has 166 files, including the unchanged but compile-excluded focused source. All common tests invoke canonical overloads. The removed exact-event APIs have no common pre-gate test call sites.

Static metadata inspection, without loading/invoking the test assemblies, confirmed the exact duration/result/test types and coherent exact-event method absent in X- and present in C. Both assemblies retain the canonical translation Evaluate, attitude EvaluateConstantRate and rotation TryRk4Step methods.

Necessary limitations: removing APIs changes assembly metadata/binary layout; excluding focused test compilation and removing its final registration changes test-assembly metadata and the initial delegate-array shape. This is the specifically authorized unavailable-group control, not identical process initialization. Common executed workload semantics and helper behavior remain identical. Such unavoidable differences constrain positive necessity claims to candidate state within an experiment; they do not identify an allocating instruction. Here the phenomenon survives despite the removal, making the negative necessity result directly useful.

## Preparation and builds

Both isolated Release test-project/dependency builds passed with zero warnings/errors before execution. No builds during the matrix. Preparation guards initially rejected newline-specific text anchors in mixed LF/CRLF candidate files. The disposable preparation script was corrected to recognize the existing boundaries without normalizing or rewriting canonical files. Preparation was then verified completely before either build or any test process; these were not matrix retries.

## Six-run matrix

| Seq | Arm | PID | Common-suite result | Full available suite | First allocation gate | Bytes | Entry | Exit | Other failure |
|---|---|---:|---|---|---|---:|---|---|---|
| 1 | C | 31872 | 37/37 PASS | 38/38 PASS | None | 0 | PASS | PASS | None |
| 2 | X- | 47168 | 37/37 PASS | 37/37 PASS | None | 0 | PASS | PASS | None |
| 3 | C | 47424 | 27 passed; group 28 failed; 9 unreached | 27/38 passed; 10 unreached | attitude-integration | 12,336 | PASS | PASS | None observed; later predicates/groups unreached |
| 4 | X- | 44924 | 37/37 PASS | 37/37 PASS | None | 0 | PASS | PASS | None |
| 5 | C | 28660 | 35 passed; group 36 failed; 1 unreached | 35/38 passed; 2 unreached | impulse-evaluation | 12,336 | PASS | PASS | None observed; later predicates/groups unreached |
| 6 | X- | 18844 | 34 passed; group 35 failed; 2 unreached | 34/37 passed; 2 unreached | trajectory-candidate | 12,336 | PASS | PASS | None observed; later predicates/groups unreached |

Six distinct fresh Release processes in the exact C/X-/C/X-/C/X- order, with no retries. Successful exits were 0; the three assertion-failure exits were -532462766. All comparison source and executable hashes were fixed before, during and after the matrix. No no-GC entry/exit, environment, runtime, binary or source failure occurred.

The three failures were independently reported exact-zero assertions. No object observer was used: these are counter witnesses, not attributed managed allocations.

### Independent predicates and reachability

- Sequence 3: all 100,000 lookup and attitude/reference-frame evaluation success checks completed before the allocation assertion. The later report was not reached; no expected hash assertion was removed.
- Sequence 5: all 100,000 impulse evaluation success checks completed. The subsequent impulse-commit measurement reported zero with entry/exit PASS. Its completion assertion follows the failing allocation assertion and was UNREACHED; its value is UNAVAILABLE from normal output.
- Sequence 6: all 100,000 trajectory-replacement candidate success checks completed. The subsequent trajectory-commit measurement reported zero with entry/exit PASS. Its Committed assertion follows the failing allocation assertion and was UNREACHED; its value is UNAVAILABLE from normal output.
- Successful sequences 1, 2 and 4 completed every available contract and reported 152 bytes for the existing positive control. That control was unreached in the failed processes; no extra control was added.

## 12,336 pattern and classification

C: 2/3 process-level 12,336 observations (attitude integration, impulse evaluation); 1/3 completely clean.

X-: 1/3 process-level 12,336 observations (trajectory candidate); 2/3 completely clean.

**EXACT-EVENT-ONLY PRODUCTION CAPABILITY IS NOT NECESSARY FOR 12,336 PHENOMENON**

Decision B applies because X- produced the target counter phenomenon while the exact-event-only production capability was absent. The different witness gate remains inside the explicitly defined ordinary process-level phenomenon. Matching magnitude does not prove that all witnesses share one runtime/object mechanism.

## Joint interpretation of the first and second discriminators

Restoring banked canonical method bodies did not eliminate the observed phenomenon. Removing exact-event-only capability while preserving candidate canonical refactoring did not eliminate it either. The two authorized subtractions have not identified a necessary individual production responsibility. They do not establish that all candidate effects are absent or that banked baseline must fail: the retained banked 5/5 population remains unchanged.

The component-isolation strategy has reached its authorized payoff limit. No third permutation, file-by-file experiment, allocator attribution or fix follows automatically.

## Static lifecycle review and KSA

Not performed: no necessary exact-event-only responsibility was isolated. No equivalent lifecycle question warrants KSA research in this result.

## VERIFY A — control semantics

Strongest challenge: test registration/exclusion and production binary removal affect initial metadata/allocation and JIT layout even though common source behavior is identical. These are bounded, necessary consequences of the authorized coherent capability removal, explicitly retained in the difference map. Canonical wrappers/kernels and all shared RK4 code survived byte-for-byte; exact APIs/types did not survive compilation. All common tests remain. Control remains meaningful for the prescribed responsibility-level discriminator; no algorithmic confound or common-test substitution was found.

## VERIFY B — necessity language

The X- counter witness defeats necessity of exact-event-only capability for the defined observed 12,336 process-level phenomenon. It does not identify its owner, object count, allocator, runtime mechanism or equal causality across gates. No such claim is made. The normal observation must not be relabeled as profiler-derived attribution.

## VERIFY C — required outcome and acceptance

Exact PhysicalEventEpoch -> coherent read-only spacecraft motion remains required. No result authorizes abandoning exact duration, fractional translation/rotation, immutability, canonical behavior or simulation authority. Canonical Candidate E remains intact and UNBANKED / FOLLOW-UP REQUIRED. Prior normal acceptance remains 2/5; these diagnostic C runs are not a replacement acceptance population.

## Recommended Project Control action

Reassess mechanism/payoff with the banked 5/5 reference and both failed component-isolation attempts. Decide whether any further bounded qualification has sufficient value before authorizing it. No production revision is justified by this experiment alone. No implementation, new diagnostic or acceptance resumption is started.

## Performance

**RELEASE PERFORMANCE UNQUALIFIED.** No separate benchmark or performance qualification was run. Existing timing checks embedded in unchanged full suites remain part of normal execution; their observations are not promoted to a performance result here.

## Evidence and reproduction

Retain README.md, preflight.json, controls.json, matrix.json, result.json and the three preparation/matrix/verification scripts. They preserve exact candidate/control source hashes and removed spans, invocation differences, loaded-runtime identity, full environment identity, binary identities, every decisive normal gate/group line, error witnesses and final non-mutation checks. No bulk traces, dumps or observer machinery were created.

Reproduction requires separately authorized fresh destinations. `prepare.ps1` verifies the accepted candidate fingerprints before copying source-only closures; its resume option is restricted to failed unbuilt preparation. Review the retained exact spans and invocation changes, build both arms once with `dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --nologo`, then run the six-process matrix only once. `matrix.ps1` refuses any already-started matrix, fixes binary/source/environment identities, and executes the absolute test DLL from each arm root without selectors. Do not rerun this completed experiment automatically.

## Disposable output

No cleanup performed. Five reviewed dedicated scratch roots contain **2,229 files / 168,968,975 logical bytes**. The new second-discriminator root adds **551 files / 39,525,752 bytes**. All roots resolve within E:\NovaCore\.codex with no discovered reparse points. Exact per-root counts are retained in result.json.

After Project Control accepts the retained result, consolidated manual cleanup:

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\.codex\exact-event-sas-ab', `
  'E:\NovaCore\.codex\exact-event-attribution', `
  'E:\NovaCore\.codex\exact-event-components', `
  'E:\NovaCore\.codex\exact-event-banked-baseline', `
  'E:\NovaCore\.codex\exact-event-second-components' `
  -Recurse -Force
```

Canonical source, Git, assets and external retained research packages are excluded. No blocked deletion was retried.

## Git and identity

Canonical Candidate E untouched: 14 accepted canonical fingerprints and 167 source-closure fingerprints unchanged. All 333 disposable C/X source fingerprints and all built artifact fingerprints unchanged through the matrix. Branch, HEAD/main/origin/main and M14.1–M14.6 targets unchanged. Original 8 modified tracked files and 6 untracked candidate files remain; nothing staged. `git diff --check` PASS with existing LF/CRLF warnings only.

No stage, commit, merge, tag, push, bank or milestone assignment.

**Lead judgment: ESCALATE TO PROJECT CONTROL**

STOP FOR PROJECT CONTROL.
