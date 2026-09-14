# Segmented powered free flight: final qualification

2026-09-14. **PASS — READY FOR PROJECT CONTROL ACCEPTANCE. UNBANKED.** No milestone assigned.

## WHAT HAPPENED

The existing Release `powered-free-flight` route was observed directly by the user / Project Control, who explicitly replied **PASS** to all requested visible outcomes. The eight-second episode accelerated under actual thrust, exhausted its finite fuel near five seconds, coasted and held the final copied canonical endpoint. Process 58676 exited normally with empty stderr.

After manual acceptance, one independent read-only final verifier passed all 15 specified responsibilities and identified no concrete bounded defect. No source/test edits, builds, automated qualification campaigns, allocation instrumentation or additional performance runs occurred during this closeout.

## WHY IT MATTERS

Thrust changes canonical physical position/velocity through the bounded changing-mass, evolving-orientation evaluator. Physical motion, resource successor and actual actuator state commit under the same transaction owner. The scene presents copied committed observations; it does not script thrust motion or use preview state as hardware authority.

The admitted model remains bounded free flight with central point fuel and documented numerical error limits. It supplies no powered-contact, real-tank, Florida-launch or unrestricted spacecraft-dynamics claim. Exact resource authority is distinct from numerical trajectory approximation.

Project Control expressly accepted nine subsequent zero-byte full Release passes as sufficient to resume the remaining qualification. Manual acceptance and final review are now complete. That decision does not explain or erase the original clock-allocation failure.

## MANUAL ACCEPTANCE

Scenario: `--scene=powered-free-flight --log=vulkan`, existing Release build, launched by [launch.ps1](launch.ps1). User acceptance date: 2026-09-14, America/New_York. Exact user response: **PASS** to the question enumerating all eight outcomes.

| Observation | Result |
|---|---|
| Acceleration | PASS |
| Requested throttle / actual engine distinction | PASS |
| Fuel decrease | PASS |
| Shutdown/exhaustion followed by coast | PASS |
| Camera independence | PASS |
| Final hold | PASS |
| Status readability and truthfulness | PASS |
| No visible motion/resource/actuator authority split | PASS |

No visible discrepancy was reported. The displayed throttle is copied actual endpoint throttle; the route submits a fixed full-throttle command at Start. The final verifier separately checked the source authority.

End witness: frontier **480**, canonical time **8,000,000 ticks**, StateRevision **480**, history count **480**, retained excess debt **3,995 ticks**, actual state **EnabledNoFeed**, displayed fuel **0.000 g**, displayed speed **4.998 m/s**. The final endpoint was held after completion. Displayed quantities are rounded and are not substitutes for raw identity checks.

[Manual record](manual-acceptance.md) retains the concise lifecycle witnesses. [Machine-readable summary](final-qualification.json) retains the four Release deployment hashes and exact manual-log hashes. The launch executable SHA-256 is `6F721ABC94E12ADE8CDE5E9EBBE2CEB112C517E31E7FCCDCE97F54E4EAB81F92`; its Simulation assembly is `3CB3491662FA24E91E304E50349EDD059CA5D2D5E57AD9264D553038455F65E2`.

Existing telemetry is report-only: cold preparation 19.3077 ms; 1,441 whole callback intervals, median 5.5555 ms, P95 5.7121, P99 6.0194, max 14.3217. These include presentation and host scheduling. Manual physical acceptance is not a performance test; no frame optimization or new campaign was undertaken.

## ALLOCATION ANOMALY

One original full Release process reported 12,336 warmed bytes in the pre-existing clock allocation gate. The failure did not reproduce in the immediately authorized follow-up or in eight additional predeclared fresh full Release processes; all nine later processes reported zero bytes and 67/67 groups. No correction was made and the original cause remains unknown.

| Observation | Result |
|---|---|
| Original full Release clock gate | **12,336 bytes**, expected **0**; 8 groups passed, 1 failed, 58 unreached |
| Immediately authorized follow-up | **0 bytes**, **67/67 PASS** |
| Eight additional predeclared fresh processes | **8/8**, each **0 bytes**, **67/67 PASS** |
| Subsequent authorized full Release total | **9/9 zero-byte PASS** |
| Cause | **UNRESOLVED** |
| Correction | **NONE** |

Retain the [original validation](validation.json), [single follow-up](clock-allocation-followup.md) and [eight-process stability report](clock-allocation-stability.md). No threshold, helper, test order, warmup or runtime setting was changed. Focused powered allocation windows remained exact zero in the accepted Debug/Release evidence; the deliberate `byte[128]` control measured **152 bytes**. This is evidence about the observed bounded runs, not a claim that the unresolved anomaly cannot recur.

## RETAINED PERFORMANCE AND STORAGE

These accepted results were not rerun. Each fresh Release process used 128 warm complete operations and 1,024 measured operations.

| Process | Median ms | P95 ms | P99 ms | Max ms |
|---|---|---|---|---|
| 1 | 0.0423 | 0.1840 | 0.1905 | 0.2212 |
| 2 | 0.0421 | 0.1820 | 0.1909 | 0.2213 |
| 3 | 0.0430 | 0.1866 | 0.1939 | 0.2220 |

The sample includes powered and post-exhaustion coast intervals; its median is not a powered-only measurement. Each worst index was 171, corresponding to overall interval 300 after warmup, near the nominal exhaustion boundary; no timing-mechanism attribution is claimed.

Preallocated 1,200-record history: **4,838,424 bytes**. Complete cold managed construction: **4,870,592–4,878,600 bytes**, a conservative retained upper bound including setup transients, below 8 MiB. No unmanaged physics world is involved. Accepted cold preparation after initialization: **0.3484–0.5151 ms**. Presentation costs remain separately reported.

## FINAL VERIFIER

**PASS.** [Final verifier record](final-verifier.md) contains all 15 responsibility results and exact source pointers.

The strongest attack was canonical success followed by failed private acknowledgement and attempted source-lease replay. Execution leases retire before the failure seam; the tested terminal path preserves the committed canonical physical/resource/actuator/time/history successors and refuses continuation. The attack failed to expose a defect.

Existing source-reviewed cases without dedicated new assertions are disclosed in the verifier record. No speculative expansion was performed. Numerical and deterministic claims remain bounded to the documented fixture/error contract and same-machine/build history comparison.

## SOURCE / EVIDENCE

HEAD, main, origin/main and remote main remain **`89a432ce3b96a5dcb0751cfdc26bfd6776c883a1`**. Branch: **`codex/segmented-powered-free-flight`**.

All **50** recorded candidate/architecture/test-binary/manual-deployment checks matched. All **1,974** tracked/nonignored input files outside this implementation-evidence directory retain the accepted digest:

`092C10C32BC0D219F0D6A1F080FF6D89EBDA53294608AE009D3286F995C59144`

Digest definition: ordinal path order; UTF-8 `path TAB uppercase raw SHA256 LF` records. No source, test, launch-script or build-output changes were made during closeout. Blender was not accessed or modified.

All **64** historical tag refs remain unchanged. Protected identities:

| Milestone | Tag | Tag object | Peeled commit |
|---|---|---|---|
| M14.21 | `m14.21-authored-compound-spacecraft-contact` | `d8c13e46a9272e5ef6eb89acbe23c29709e3c55b` | `b6e8fa568585706ce38c0609aeb14db4a643123e` |
| M14.22 | `m14.22-canonical-spacecraft-command-authority` | `e943a128ef425ff7427b917f2bb04056e58282f5` | `899f11f727205340bdd550dd65a853e253de0ed3` |
| M14.23 | `m14.23-prepared-single-engine-actuation` | `cd22cc96d88d2b6cf3ca48740ad41baecbbe7c9e` | `6c9830237bc29bbd3c9cf5f4644b6409c32eaa9b` |
| M14.24 | `m14.24-exact-finite-propellant-segmentation` | `50aa4e984e6646a9334e4e87356a43ffdf138e73` | `89a432ce3b96a5dcb0751cfdc26bfd6776c883a1` |

The accepted [powered-dynamics architecture evidence](../powered-dynamics-architecture/README.md) remains **11 files / 103,541 bytes**, unchanged. Both architecture and implementation packages must be included in any later exact staged-tree banking closure. Banking is not authorized here.

The original reports are retained as historical evidence. Current closeout updates only the implementation README/manual record and adds the final summary, machine-readable result and independent verifier record. [Identity](identity.json) retains the full 32-file candidate source/sample/test inventory: 21 existing modified files and 11 new files.

Git index: **empty**. `git diff --check`: **PASS**, exit 0. Git emitted existing LF-to-CRLF conversion notices; no source normalization was performed.

Current `git status --short`:

```text
 M samples/NovaCore.Triangle/Program.cs
 M samples/NovaCore.Triangle/SampleOptions.cs
 M src/NovaCore.Simulation/Spacecraft/ReferenceFrames/SpacecraftReferenceFrameEvaluator.cs
 M src/NovaCore.Simulation/Spacecraft/Resources/FinitePropellantSegmentation.cs
 M src/NovaCore.Simulation/Spacecraft/SpacecraftStateStore.cs
 M src/NovaCore.Simulation/Spacecraft/SpacecraftStateView.cs
 M src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftMotionEvaluator.cs
 M src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftPhysicalEventMotionEvaluator.cs
 M src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftTranslationState.cs
 M src/NovaCore.Simulation/Transactions/SimulationState.cs
 M src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.EnginePreparation.cs
 M src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Propellant.cs
 M src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.SpacecraftCommands.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tests/NovaCore.Launcher.Tests/Program.cs
 M tests/NovaCore.Simulation.Tests/FinitePropellantTests.cs
 M tests/NovaCore.Simulation.Tests/Program.cs
 M tools/NovaCore.Launcher/LaunchCommandBuilder.cs
 M tools/NovaCore.Launcher/LaunchConfiguration.cs
 M tools/NovaCore.Launcher/ScenarioCatalog.cs
?? docs/engineering-evidence/powered-dynamics-architecture/
?? docs/engineering-evidence/segmented-powered-free-flight/
?? samples/NovaCore.Triangle/PoweredFlightDevelopmentScene.cs
?? src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightEvaluator.cs
?? src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs
?? src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFreeFlight.cs
?? src/NovaCore.Simulation/Spacecraft/SpacecraftAppliedEndpoint.cs
?? src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredFreeFlight.cs
?? tests/NovaCore.Graphics.Tests/PoweredFlightPresentationTests.cs
?? tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.Authority.cs
?? tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.Physics.cs
?? tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.Qualification.cs
?? tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.cs
```

## REPRODUCTION AND DISPOSABLE EVIDENCE

Retain [reproduce.ps1](reproduce.ps1), [launch.ps1](launch.ps1), the original failure/results, follow-up/stability results, identities, manual acceptance and verifier. Do not automatically rerun campaigns; use the retained explicit modes only under a future authorized scope.

Two temporary manual logs remain: **2 files / 24,066 bytes**. Their decisive witnesses and SHA-256 hashes are preserved in the manual/JSON records. No runtime path depends on those logs; an explicit future launch recreates them. All unrelated historical scratch and ordinary rebuildable bin/obj outputs remain untouched.

Removed: **0 files / 0 bytes**. Automatic approval review rejected the reviewed two-file deletion before execution as **blocked by policy**. No retry or workaround was attempted. This is evidence cleanup, not a technical-qualification blocker.

Exact manual cleanup scope, if Project Control performs it:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\segmented-powered-free-flight-live\live.stdout.txt','E:\NovaCore\build\segmented-powered-free-flight-live\live.stderr.txt' -Force
```

No wildcard, recursive deletion, candidate source or retained evidence is included.

## JUDGMENT

**PASS.**

## WHAT HAPPENS NEXT

**READY FOR PROJECT CONTROL ACCEPTANCE. UNBANKED.**

**STOP FOR PROJECT CONTROL.** Do not assign a milestone, commit, tag, bank, begin powered contact or begin Florida launch.
