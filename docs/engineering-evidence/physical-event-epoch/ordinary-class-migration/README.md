# Ordinary zero-allocation class migration

ZERO-ALLOCATION SMALL CLASS MIGRATION — VALIDATED

Lead judgment: PASS. Candidate remains UNBANKED. Stop for Project Control.

## Baseline and scope

Branch `codex/physical-event-epoch`; HEAD/main/origin/main `b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`. M14.1–M14.5 tag objects and targets unchanged (identity.json). All 210 production C# fingerprints match the pre-correction candidate. No production edits, staging, banking, or milestone assignment.

The authoritative [42-window inventory](../zero-allocation-inventory/inventory.md) contains 31 ordinary B windows, 5 timed E windows and 6 checked A windows. Only the 31 B windows were migrated. Existing servicing disposition: **KEEP AS IMPLEMENTED**. Existing checked boundaries, epoch allocation test and report-only paths remain unchanged.

## Shared measurement contract

`tests/NovaCore.Simulation.Tests/OrdinaryAllocationMeasurement.cs` is a small test-only ref struct. Original caller setup and warmup precede construction. Construction requires successful `TryStartNoGCRegion(1 << 20, disallowFullBlockingGC: true)` before opening the per-thread counter. The 1 MiB reservation reuses the qualified servicing policy; preallocated ordinary workloads require exactly zero measured allocation, and the positive control needs only one small object. All windows successfully entered and exited this same bounded reservation. No per-test enlargement, fallback, retry, delegates, suite-wide region, or production runtime configuration change.

`Complete()` closes the counter before guaranteed cleanup and reporting, returns the exact delta, and `RequireZero` demands exactly zero. Caller `using` also guarantees cleanup if work throws. Entry failure throws; EndNoGCRegion failure propagates. No failed entry/exit can produce a PASS report. Reporting and state assertions occur outside the measured interval. No subtraction, tolerance, or known-byte exception.

The prior resolved mechanism is CANDIDATE-TRIGGERED CLR ALLOCATION-CONTEXT ACCOUNTING EFFECT: unchanged cumulative managed/UOH allocation with unused context 8,160 → 0 produced counter +8,160 without objects. This migration isolates ordinary workload measurement; it makes no new attribution claim for other prior failures.

## 31-window reconciliation

File names below are under `tests/NovaCore.Simulation.Tests/`. Original audit IDs are preserved.

| Audit # | Gate | File / method | Migrated | Reason |
|---|---|---|---|---|
| 1 | Speed preset access | Program.cs / SolarUtcAndSpeedPresetTests | YES | B-family; exact original workload |
| 3 | Timeline topology | Program.cs / TimelineTopologyTests | YES | B-family; exact original workload |
| 4 | Clock steady state | Program.cs / ClockTests | YES | B-family; exact original workload |
| 5 | Host conversion | Program.cs / HostDurationTests | YES | B-family; exact original workload |
| 6 | Single debt service | Program.cs / HostDurationDebtServiceTests | YES | B-family; exact original workload |
| 8 | Per-event transaction | Program.cs / TransactionTests | YES | B-family; exact original workload |
| 9 | Canonical group | Program.cs / CanonicalGroupTests | YES | B-family; exact original workload |
| 11 | Celestial store | Program.cs / CelestialContractTests | YES | B-family; exact original workload |
| 12 | System definitions | Program.cs / CelestialSystemDefinitionTests | YES | B-family; exact original workload |
| 13 | Body catalog | Program.cs / CelestialBodyCatalogTests | YES | B-family; exact original workload |
| 14 | SolAnalytical warm evaluation | Program.cs / SolAnalyticalDatasetTests | YES | B-family; exact original workload |
| 15 | System time mapping | Program.cs / CelestialSystemTimeAndProvenanceTests | YES | B-family; exact original workload |
| 16 | Ephemeris catalog | Program.cs / CelestialEphemerisCatalogTests | YES | B-family; exact original workload |
| 17 | Sampled evaluation | Program.cs / CelestialSystemEvaluationTests | YES | B-family; exact original workload |
| 18 | SolMini evaluation | Program.cs / CelestialSystemEvaluationTests | YES | B-family; exact original workload |
| 23 | Propagation adapter/refusal | Program.cs / TwoBodyPropagationTests | YES | B-family; exact original workload |
| 24 | Spacecraft attitude | Program.cs / SpacecraftAttitudeTests | YES | B-family; exact original workload |
| 25 | Spacecraft translation | SpacecraftTranslationTests.cs / Allocations | YES | B-family; exact original workload |
| 27 | Atomic contact response | ContactResponseTests.cs / Allocations | YES | B-family; exact original workload |
| 29 | Attitude integration | Program.cs / SpacecraftAttitudeIntegrationTests | YES | B-family; exact original workload |
| 30 | Rigid rotation | Program.cs / RigidBodyRotationTests | YES | B-family; exact original workload |
| 31 | Torque transaction lookup | Program.cs / RigidBodyTorqueTransactionTests | YES | B-family; exact original workload |
| 32 | Guidance | Program.cs / FlightReferenceAndSasTests | YES | B-family; exact original workload |
| 33 | SAS sign/frame proof | Program.cs / SasSignFrameContinuityProofTests | YES | B-family; exact original workload |
| 34 | Orbit sampling | Program.cs / AnalyticalOrbitSamplingTests | YES | B-family; exact original workload |
| 35 | Celestial frame extraction | Program.cs / CelestialFrameExtractionTests | YES | B-family; exact original workload |
| 36 | Trajectory candidate creation | Program.cs / CelestialTrajectoryReplacementTests | YES | B-family; exact original workload |
| 37 | Trajectory commit | Program.cs / CelestialTrajectoryReplacementTests | YES | B-family; exact original workload |
| 38 | Impulse evaluation | Program.cs / CelestialImpulseEventTests | YES | B-family; exact original workload |
| 39 | Impulse commit | Program.cs / CelestialImpulseEventTests | YES | B-family; exact original workload |
| 40 | Integral arithmetic/order | Program.cs / AllocationTests | YES | B-family; exact original workload |

## Composite assertions

Allocation is now independent at 15 original composite assertion sites covering audit rows 1, 6, 8, 9, 11, 12, 13, 14, 15, 16, 18, 23, 25, 36–39 (17 rows; 36/37 and 38/39 shared sites). Original non-allocation predicates and thresholds remain. Remaining independent checks keep their existing workload predicates.

Canonical group retains the separate single-event warmup and preallocated 5,000-event measured group. Allocation, completion and processed-count assertions are independent. Current result: 0 bytes, complete=True, processed=5,000. Historical failing operand remains UNKNOWN.

The adapter B window formerly shared an assertion with two timed propagation windows. Only the adapter allocation conjunct was extracted. Timed counters, stopwatch intervals, workloads, exact-zero predicates and original timed assertion message remain. No timed measurement was migrated.

## Static verification and builds

PASS: all 31 original measured bodies identical, no extra migration, all 5 timed intervals preserved, existing checked windows retained, report-only paths unchanged. Diff review preserves setup, warmup, counts, checksums and state expectations. Production fingerprints unchanged. The three changed existing test files and shared helper are current candidate test source; reconciliation.json and identity.json preserve the reviewed window list and accepted hashes. The superseded test-boundary patch was retired at evidence closeout.

Representative Debug project compile and canonical+positive check passed before remaining migration. Final full solution Debug and Release builds passed with zero warnings and errors. No failed build or test, no retry, no profiling. The predeclared plan and process IDs/arguments/results are retained in plan.json and validation.json.

## Focused helper and positive control

Debug and Release focused helper checks: canonical allocation=0, complete=True, processed=5,000; entry/exit PASS. The shared positive control warms its separate no-inline factory, allocates one `byte[128]`, retains it through counter closure, and requires a nonzero delta. Three fresh Debug and three fresh Release controls each measured 152 bytes; entry/exit PASS in all six. The permanent condition is nonzero, not a fixed-size subtraction or permission for production allocation. One shared control also runs at the end of the existing arithmetic allocation group; no new group/order change.

## Full Simulation matrix

Ten fresh processes, predeclared five per configuration, first-failure stop, no retries. All 37 registered groups passed in every run. All 31 migrated measurements report their exact delta and successful entry/exit separately.

| Configuration | Run | Result | First failure | Allocation gates | Allocation | Entry | Exit |
|---|---|---|---|---|---|---|---|
| Debug | 1 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Debug | 2 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Debug | 3 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Debug | 4 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Debug | 5 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Release | 1 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Release | 2 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Release | 3 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Release | 4 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |
| Release | 5 | 37/37 PASS | None | 31/31 | 0 each | PASS | PASS |

## Focused candidate validation

After the full matrix: physical-event epoch, M14.2 translation, M14.3 contact generation, M14.4 atomic response, M14.5 isolated response, ReferenceFrames and Precision all PASS in Debug and Release. No renderer/GPU campaign. All candidate production semantics remain unchanged.

## Timed family

Five windows (audit 19, 20, 21, 22, 41): **DEFERRED — TIMING CONTRACT REQUIRES SEPARATE REVIEW**. These cover lunar/all-body orientation, circular/elliptic propagation and opt-in translation performance. Introducing isolation can change timing semantics; no review or migration begun here.

## VERIFY A — class migration

Strongest objection: GC isolation could mask real allocation or mechanical assertion splitting could drop a workload predicate. Survived: actual byte-array allocation advances the counter in every control, zero remains exact, original bodies were compared, and all original predicates remain. No excluded timed/report-only/checked window migrated. This is evidence for measurement correctness, not proof against every conceivable future regression.

## VERIFY B — semantics and reservation

Strongest objection: suppressing collection could change a workload that intentionally tests GC interaction or exceed the common reservation. Survived: audited ordinary windows protect preallocated warmed workload allocation, not GC scheduling; none requires collection to satisfy its production contract. Setup/warmup remain outside, workloads unchanged, every entry/exit succeeds with the qualified 1 MiB reservation. No change to production GC policy or epoch arithmetic/order/public time behavior.

## Evidence and manual cleanup

Retain the earlier structural/accounting reports and 42-window inventory, this checklist, concise process/gate records, identity proof and reproduction instructions. No new bulk comparison build tree. Disposed during this ticket: 0 bytes.

Reviewed scratch: 2,245 files / 194,327,893 bytes across eight exact directories listed in the manual command below. This includes prior 2,155 files / 193,681,965 bytes and new 90 files / 645,928 bytes. They contain no tracked files or reparse points. No references were found in inspected production/test source/project files. Current runtime assets, permanent fixtures and normal build/deployment directories are excluded. Temporary scripts/copies are disposable; current tests plus the retained plan, identities and evidence preserve reproduction.

Cleanup is **not executed**. After Project Control accepts the technical result, the reviewed manual command is:

```powershell
Remove-Item -LiteralPath `
'E:\NovaCore\.codex\physical-epoch-gate', `
'E:\NovaCore\.codex\physical-epoch-matched-ab', `
'E:\NovaCore\.codex\physical-epoch-structural', `
'E:\NovaCore\.codex\physical-epoch-allocation-phase', `
'E:\NovaCore\.codex\physical-epoch-gc-region', `
'E:\NovaCore\.codex\physical-epoch-accounting', `
'E:\NovaCore\.codex\physical-epoch-measurement', `
'E:\NovaCore\.codex\physical-epoch-small-class' `
-Recurse -Force
```

Disposable remaining until manual cleanup: 194,327,893 bytes. Banking remains outside this ticket.

## Reproduction

From E:\NovaCore, build NovaCore.sln once per Debug/Release with `dotnet build NovaCore.sln -c <configuration> --nologo`. Run `dotnet tests/NovaCore.Simulation.Tests/bin/<configuration>/net10.0/NovaCore.Simulation.Tests.dll --ordinary-allocation-helper` once per configuration, then `--ordinary-allocation-control` in three fresh processes per configuration. Require 0 for canonical, complete=True, processed=5,000; require nonzero for each control; all entry/exit PASS.

Run that DLL without a selector in exactly five fresh Debug then five fresh Release processes; stop first failure, no retries. Require 37 registered group passes and exactly one zero/entry-PASS/exit-PASS record for every gate in reconciliation.json per full run. After that, run selectors `--physical-event-epoch-only`, `--translation-only`, `--contact-only`, `--contact-response-only`, `--isolated-contact-only` and the ReferenceFrames/Precision test DLLs, both configurations. The precise executed commands/PIDs and gate records are in validation.json. No profiler/runtime overrides or builds inside the full-suite matrix.

## Git boundary

Only this ticket's three existing test files, one new helper and concise evidence were changed. Earlier candidate changes remain intact. Nothing staged; branch unchanged. Final Git verification is recorded separately. Stop for Project Control.
