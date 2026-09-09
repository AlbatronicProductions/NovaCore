# M13.6 manual acceptance failure: Project Control disposition

**Failure classification: INSUFFICIENT EVIDENCE**

**Over-rendering classification: INSUFFICIENT EVIDENCE**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

**M13.6 CANDIDATE — FOLLOW-UP REQUIRED**

There is a confirmed Windows GPU/display-engine timeout near the reported restart, but its initiating process, operation and cause are not resolved. M13.6 has neither been proven causal nor cleared. No production correction, frame cap, rollback, new GPU run, build or deployment was performed. The prior candidate and its automated evidence remain intact. Manual acceptance is failed and banking remains on hold.

This report supersedes the previous `m13-final-exit/README.md` automated-ready status **for manual acceptance only**. The earlier report remains an accurate record of completed automated work; it was not rewritten.

## Baseline and preservation

| Identity | Verified value |
|---|---|
| HEAD | `d4baab6940a57a46e478b98e36f5e45d1c4b558f` |
| main | `d4baab6940a57a46e478b98e36f5e45d1c4b558f` |
| origin/main, local remote-tracking ref | `d4baab6940a57a46e478b98e36f5e45d1c4b558f` |
| Branch | `codex/m13-final-exit` |
| Annotated tag | `m13.5-local-gpu-terrain-working-data` |
| Tag object / target | `75712e4bc990e11c3072a37299c55187cc30e0a1` / banked commit above |
| GPU | AMD Radeon RX 6800 XT; current driver `32.0.21045.5002` |
| Primary desktop now | ASRock PG34QRT2B, `\\.\DISPLAY7`, 3440×1440 at 180 Hz, origin 0,0, RX 6800 XT |
| Secondary desktop now | `\\.\DISPLAY6`, 2560×1440 at 144 Hz, origin −2560,0, RX 6800 XT |
| Failed window/display | User selected Fullscreen Native. Historical output assignment, actual refresh/VRR and driver overrides were not recorded. Current desktop state does not prove the incident display state. |

Starting staged diff was empty. Starting tracked diff was exactly the three existing candidate files, 36 insertions / 6 deletions. The source SHA-256 values match the previous closeout. `identity.json` records the exact diff hash, source hashes, all 108 prior evidence files, current deployment and display enumeration. Final read-only preservation checks and complete status are in `closeout.json`.

The normal Release runtime is `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`. Current hashes:

| Artifact | SHA-256 |
|---|---|
| Release native DLL | `932267253ea6e0221ba4ef5093515874116b26ede8c565ff8b8316e02e21eabf` |
| Release managed DLL | `1a2843c69458704feabcbd11760f43620c8cc39f0b0740980b9bfa96b47f705e` |
| Release executable | `59a1e9f40073ad39fefcbc3ad4e3df69b740bd1971dd297a96d01ea63131b1d8` |
| Debug native DLL | `6e10a80772efaa51da256983355b1ab3bdec49e792891bfc5280c12a596bdc6e` |

Both configurations' native, managed, executable and all 49 deployed shader hashes match the prior final candidate manifest. The three protected elevation/cache assets also match their recorded sizes and hashes. No stale deployment is demonstrated. These are current on-disk identities; the missing incident log prevents an exact loaded-module fingerprint at the moment of failure.

## Manual event and corrected observation

Project Control clarified: **Florida Launch Site / Fullscreen Native / Normal Diagnostics**, approximately **five minutes**, very loud fans followed by the PC freezing. The ticket describes zooming outward, the NovaCore display turning black, and a restart. Exact starting time, final altitude/LOD/pupil/generation, last completed frame and last successful GPU operation are unknown.

**There was no FPS reading.** The original 200–270+ FPS estimate is withdrawn as measurement evidence. Fan loudness is not measured fan percentage, temperature, power, GPU utilization or proof of over-rendering. No thermal diagnosis follows from it.

The configured Florida scenario remains `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. The normal launcher is `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`. Those paths identify the preserved candidate; this report does **not** release it for another five-minute manual stress run.

## Windows / Vulkan evidence and attribution

Work A (`work-a.md/json`) preserves bounded Windows event records and query limits:

- Fresh WER `LiveKernelEvent 141`, Application record 15585, **September 8 00:13:10.080839 EDT** (04:13:10.080839 UTC), report `5880d346-5ecd-4c93-a8eb-576e32823930`, references `WATCHDOG-20260908-0013.dmp`.
- Current boot began around 00:14:01.5 EDT. Kernel-Power 41 at 00:14:05 confirms an unclean restart, with `BugcheckCode=0`. It does not diagnose a power-supply cause.
- The same report was resubmitted after boot at 00:14:24 and 00:17:21. These are not three independently proven GPU hangs. Older September 2/6 reports were also resubmitted; their workload/cause is unknown.
- No matching successful Display 4101 recovery, WHEA, Windows bugcheck or NovaCore Application Error/Hang event was found in the inspected incident window. Missing records do not exclude unwritten failures.
- No fresh NovaCore incident log was found. The existing Downloads runtime log is dated September 6 and cannot identify this failure. This does not imply the user's runtime was stale.
- No incident `VK_ERROR_DEVICE_LOST`, VUID, final submitted command, fence/acquire/present result, resource identity, temperature/power or memory-pressure trace is available. Thus Vulkan validation at the instant of failure is **unknown**, not PASS.

Microsoft defines [0x141](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x141---video-engine-timeout-detected) as a display-engine timeout live dump. This positively identifies the observed failure manifestation. It does not, by itself, distinguish invalid application commands, a driver implementation defect, hardware instability or another initiating cause. The requested causal classification therefore remains **INSUFFICIENT EVIDENCE**, rather than asserting the driver was responsible.

The fresh dump and its report are access-denied to this session:

```text
C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp
C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930\Report.wer
```

No ACL change, privilege bypass, registry change or dump copying was attempted. Their size/hash/contents are not verified. Readable access or a user-exported copy is the next bounded evidence step. The driver address in WER is not yet a resolved module. A resolved driver stack would still need causal interpretation; it would not automatically establish that the driver, rather than submitted work, caused the timeout.

## M13.5 versus M13.6

The candidate adds `TerrainRequestKeys` memory intent, preferring a compatible HOST_VISIBLE/HOST_COHERENT/HOST_CACHED type for the same 393,216-byte, renderer-lifetime key buffer. Compatible fallback and fatal-error policy remain. The prior measured device selection changed type 1 / flags 6 to type 3 / flags 14. All buffer layout, writes, two host scans per frame, producer/poller, publication, fences and destruction/reuse remain unchanged.

This is a real memory-placement change and a measured reduction in CPU scan delay. It can exercise a different driver/cache path or change timing around existing work. Source similarity and old automated passes cannot exonerate it.

The field-level audit found no newly demonstrated GPU-to-host visibility violation on this production route: the active host poller reads host-owned words 0–7 and 10–11, while the bootstrap production selector writes words 8–9; authoritative NCSM1 bypasses that selector. Coherent memory alone is not a substitute for required memory dependencies. The conclusion rests on exact consumers/phases, not a blanket assumption about coherence.

No new M13.5/M13.6 runtime comparison was run. A short passing control would not clear a five-minute system timeout with no final-operation context. Another deliberately sustained hang reproduction is not justified before analyzing the already-existing dump.

## Zoom and resource lifecycle

Work B traces wheel input through managed camera/altitude selection, adjacent L0–L17 replacements, one incoming preparation, regional demand, complete physical preparation and fence-gated publication. Zoom can change topology/work sizes and cause first-use bounded allocations. Earth eligibility does not disappear solely because altitude increases, and zoom itself does not recreate the swapchain or reallocate the key buffer.

Static ownership is bounded: one frame fence/acquire semaphore; per-image command/framebuffer/present semaphore; one descriptor set; at most 18 topology identities; current plus incoming-or-spare terrain work; one pupil scratch buffer; fixed catalog/queue limits. Normal Diagnostics does not enable the growing finite-traversal vectors. Retained short traversals exercise reuse/publication but do not establish five-minute live allocation stability or driver heap behavior.

Pre-existing infinite fence/acquire waits, incomplete result telemetry at several reset/idle sites, and the minimized-window recreation loop remain possible diagnostic sites, not proven causes. No cleanup or speculative correction was made. There is no measured final zoom frame with which to assign synchronization, resource lifetime, workload transition or swapchain causality.

## Presentation architecture and independent pacing judgment

| Responsibility | NovaCore current | Measurement boundary |
|---|---|---|
| Requested/selected source policy | FIFO, unconditional | No benchmark or normal-route override |
| Available present modes | Not enumerated | Not measured for the failed surface |
| Images | Requests surface minimum + 1, clamped by a nonzero maximum; then uses returned actual count | Actual incident count not logged |
| Frames in flight | One graphics fence gates host update/mutation | Not the number of images queued in the presentation engine |
| Acquire / present | Infinite acquire with one acquire semaphore; present waits acquired-image-indexed render-finished semaphore | No last-operation or displayed-completion trace |
| Refresh / cap / VRR | No explicit FPS limiter, refresh query or VRR policy | Absence of a sleep is not proof of unbounded presentation |
| Benchmark | Frame-count termination and diagnostic traversal use the same FIFO policy | There is no existing explicit unlimited-present mode |

[FIFO presentation](https://docs.vulkan.org/refpages/latest/refpages/source/VkPresentModeKHR.html) queues requests for vertical-blank presentation; finite swapchain availability normally supplies backpressure. Effective driver/compositor/monitor behavior, useful displayed cadence, discarded work and VRR were not measured here. GPU-duration reciprocal, short historical timing runs and fan noise cannot establish them. No normal-versus-paced-versus-unlimited comparison was justified because its prerequisite—demonstrated over-rendering—was not met.

**Separate pacing responsibility:** bounded observation of render/submit/present/display cadence and active monitor identity under the existing FIFO route, if Project Control authorizes continued runtime investigation after dump review. This is a diagnostic responsibility, not a frame-cap implementation candidate. A later proven policy change must retain independent deterministic simulation, avoid hard-coded refresh values, and explicitly preserve a benchmark/unlimited option rather than pretending the current frame-count flag supplies one.

## KSA current source plus engineering history

`work-c-source.md/json` records the current installed KSA DLL version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256 `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`, and direct IL identities for the dependency-owned renderer and time loop. No KSA application was launched or modified. `ksa-history.md` joins the authenticated read-only official live-changelog, release-history and developer-update evidence.

KSA defaults to FIFO VSync, can request Immediate with FIFO fallback, uses two frame slots and separately indexed swapchain-image presentation semaphores, and centralizes rebuild and completed-resource publication. Historical mailbox-to-FIFO, multi-image and frame/image-ownership corrections explain those boundaries. NovaCore already shares FIFO and per-image semaphore ownership: **ADOPT, already aligned**. Preserve one-frame NovaCore mutation/publication ownership rather than copying two slots: **INTENTIONALLY DIFFER**, justified by its current resource contract. Adapt capability-aware presentation diagnostics if needed: **ADAPT**.

KSA exposes an FPS-limit setting, but current IL places the sleep limiter in a helper with no call/reference found across the inspected installed product assemblies. A slider and field writes are not proof of an executed gameplay limiter. Later official monitor-selection revision 5406 must not be attributed to installed revision 5402. Historical high-FPS camera stutter was addressed through frame/concurrency ownership, not an assumed whole-system thermal failure. No equivalent historical cause of this NovaCore timeout was established.

KSA's coarse fallback, retirement age rule and player-dt behavior are not substitutes for NovaCore's exact regional physical completeness, atomic publication and deterministic simulation contracts. No KSA code was copied and no such invariant was weakened.

## Candidate proposals, verification and judgment

**Stability candidate: none.** No single causal responsibility meets the correction bar.

**Pacing candidate: none.** No measured unnecessary rendering meets the pacing bar. Utilization, GPU/CPU frame cadence, displayed cadence, input latency, thermal/power response and paced/unlimited comparison remain unmeasured for the incident; no values are invented.

VERIFY A (`verify-a.md`) attacked cached memory visibility, fallback, changed cadence, zoom allocation, publication, driver failure and long-runtime bounds. No missed proven fix was identified. Its strongest blocker is absent incident operation/resource context plus the inaccessible fresh dump. Static boundedness and earlier watchdogs do not clear M13.6.

VERIFY B (`verify-b.md`) attacked FIFO assumptions, rendered versus displayed metrics, actual image count, multiple monitors/VRR, KSA limiter reachability, newer history versus installed source, simulation coupling and benchmark contamination. The proposed no-cap disposition survives; a claim that pacing is proven adequate would not.

No build/regression rerun or GPU recapture is claimed for this investigation. Existing automated candidate passes remain historical evidence, not substitute manual acceptance. Validation for this report checks source/diff preservation, 108-file prior-package preservation, deployment/shader/asset identity, structured evidence parsing, bounded storage and `git diff --check`.

## Next bounded responsibility and manual-retake gate

1. Make the existing fresh watchdog dump and Report.wer readable for bounded offline analysis; resolve timeout engine/process/module context without another stress run.
2. Join any recovered context to the actual submitted operation, zoom/publication and key-memory identity. If that evidence does not discriminate the cause, return the exact missing proof to Project Control.
3. Only if further runtime evidence is then justified, prepare durable bounded last-operation breadcrumbs and periodic live-resource/cadence summaries before one safely bounded control. The telemetry requirements are in Work B. Do not begin repeated five-minute hangs, change TDR settings, remove validation, or use an FPS cap to conceal device failure.
4. Release for repeat manual acceptance only when the ticket's candidate-causality bar is met. The eventual launcher-based route should cover near-surface movement, sustained runtime, zoom to orbit and return, Florida and active refinement. **That retest is not cleared by this report.**

The candidate remains unbanked; main, tags and history remain unchanged. No M13.7 or visual/performance front was started.

## Storage and final Git state

Permanent evidence is confined to this package, within the announced 4 MiB budget. It contains reports, compact Windows/source/identity records and read-only reproduction helpers. No bulk capture, GPU raw output, copied crash dump or scratch archive was created. Created bytes equal retained bytes; disposed bytes and disposable remaining are zero. Exact logical/allocated bytes, file count, hashes and complete status are in `closeout.json`.

`git diff --check`: PASS; only existing LF-to-CRLF warnings. Final expected status (verified in closeout):

```text
 M native/NovaCore.Native/MappedBufferMemory.h
 M native/NovaCore.Native/MappedBufferMemoryTests.cpp
 M native/NovaCore.Native/NovaCoreNative.cpp
?? docs/engineering-evidence/m13-final-exit/
?? docs/engineering-evidence/m13.6-manual-failure/
```

The first four entries predate this ticket. Only the last evidence package was added. Nothing was staged, committed, pushed or tagged.
