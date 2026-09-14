# Clock allocation attribution: Phase 1 stop

**REVISE. Failure not reproduced. Cause unresolved. Candidate remains UNBANKED.**

## What happened

Project Control authorized one causal investigation beginning with exactly one fresh full Release reproduction. Phase 1 explicitly requires stopping if the existing 12,336-byte clock failure does not reproduce.

The recorded 32 candidate source/test/sample/launcher files, 11 accepted architecture files and three tested binaries all matched before execution: **46/46**. No build, instrumentation, warmup, test-order, threshold or runtime-setting change was made. The helper and all five clock production source files also match their preimplementation hashes. The Release DLL beside the test executable matches the recorded production DLL.

On 2026-09-14 at 13:52:05.2652668 UTC, one fresh `dotnet` process (PID 53432) ran the unchanged full Release route from `E:\NovaCore`. Loaded CoreCLR was the existing .NET 10.0.12, SDK 10.0.303. The process completed at 13:52:11.8423841 UTC, exit code 0, empty stderr.

| Observation | Original qualification | One authorized reproduction |
|---|---:|---:|
| Clock allocated-byte delta | 12,336 | **0** |
| Required delta | 0 | 0 |
| Checked no-GC entry / exit | PASS / PASS | PASS / PASS |
| Full suite | 8 passed / 1 failed / 58 unreached | **67 passed / 0 failed** |
| Retry | 0 | 0 |

The exact reproduction witness was:

```text
ORDINARY_ALLOCATION gate=clock bytes=0 entry=PASS exit=PASS
PASS Simulation clock
```

Predecessor order was unchanged: Segmented powered free flight; SimulationInstant; SimulationDuration; SimulationRate; Solar UTC and speed presets; Event ordering; Internal physical-event epochs; Timeline topology. The immediately preceding registered group was Timeline topology. No preceding group was omitted.

The full route's existing deliberate allocation controls continued reporting `System.Byte[]`, length 128, **152 bytes**. This does not replace or explain the original clock failure.

## Why it matters / what non-repeatability means

The retained outcomes now differ under the verified same source/binary identities and same full-suite order. An unconditional, stable 12,336-byte failure is therefore not reproduced by these checked inputs. This is evidence of an intermittent/process-dependent observation, **not an explanation of its mechanism**. The runtime/process condition that differed remains unknown.

The original nonzero counter remains a valid failure witness. This one pass does not establish a fix, show baseline failure, exonerate candidate production, prove lazy initialization, or establish a harness/CLR accounting defect. Object type, count, allocation stack, minimal trigger and correction layer are all **UNRESOLVED**. The reason focused propulsion allocation previously passed while the original full clock window failed also remains unknown.

Phase 7's causal classes are not assigned: none has been proven, and the mandated Phase 1 stop occurs before that decision. No familiar byte magnitude or historical allocation mechanism is reused as an explanation.

## Stop and validation disposition

- Baseline isolated/full controls: **NOT RUN**.
- Standalone candidate clock control: **NOT RUN**.
- Predecessor minimization or attribution tooling: **NOT RUN**.
- Production/test/measurement correction: **NONE**.
- Clock gate: **0 bytes in this single reproduction**.
- Full Release: **67/67 in this single reproduction**, with prior failure preserved.
- Full Debug: **not rerun**; the previous ticket's 67/67 result remains historical evidence.
- Focused preservation and timing: **not rerun**, because no candidate correction occurred.
- Manual acceptance: **pending; not performed**.

The single full-route process naturally completed after its clock gate passed. No second process, ordering control, baseline tree or temporary diagnostic build was created afterward. Remaining work requires Project Control's next bounded decision on non-repeatability; it is not authorization to continue until green.

## Identity, evidence and reproduction

HEAD/main/origin/main/remote main remain `89a432ce3b96a5dcb0751cfdc26bfd6776c883a1`. Branch: `codex/segmented-powered-free-flight`. All 64 historical tag references, including M14.21-M14.24, remain unchanged. The accepted architecture package is unchanged. No Blender workspace was touched. Nothing is staged or banked.

Retain [structured run evidence](clock-allocation-followup.json), [candidate fingerprints](identity.json), and the [original failure/qualification record](validation.json). This follow-up adds only concise evidence; full raw stdout is not a permanent artifact. No bulk dumps, disposable comparison trees, profiler copies or cleanup operations were created.

Exact CLI route (recorded for reproduction; no further run authorized by this stop):

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
```

Preserve the recorded bytes and ordinary environment; do not build, change order, add warmup or instrumentation merely to obtain another pass. The external launcher used ordinary inherited environment and redirected stdout/stderr, with one read-only inventory of the loaded CoreCLR module for identity; it did not attach runtime diagnostics.

**STOP FOR PROJECT CONTROL. No manual acceptance, correction, banking or milestone assignment.**
