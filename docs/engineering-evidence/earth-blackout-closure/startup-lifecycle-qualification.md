# Startup lifecycle correction and bounded distant-Earth retry

2026-09-22 · **REVISE / UNBANKED**

The observer/startup handoff correction passed offline qualification and exercised the real managed-to-native-to-GPU path. The single authorized distant-Earth retry then stopped on an unresolved **CPU command-recording boundary at frame 168**, before a frozen capture completed. The capture/readback stage does **not** pass. No further live exposure occurred.

**Blackout cause: UNRESOLVED. Historical 411,877-triangle discrepancy: UNRESOLVED. Manual Player acceptance: ON HOLD.** No commit, tag, push, banking, milestone promotion, KSA change, rendering-quality reduction, or driver/system setting change.

Evidence root: `E:\NovaCore\build\earth-blackout-closure\startup-lifecycle`. The previous failed attempt remains in `readback-qualification/live-01-distant`; it is not relabeled as GPU evidence.

## Observer correction

A 256-byte process-shared lifecycle channel now exists before the application starts. The managed UI, native renderer and observer use write-once milestones with one process identity. This channel is separate from the native render-thread causal ring; it does not introduce multiple producers into that ring.

| Boundary | Authority and deadline |
|---|---|
| UI/loading handoff | UI-ready and Start-click timestamps are separate. Start must occur within 60 seconds of process launch. UI-ready cannot reset that clock. |
| Loading to native handoff | Explicit Start-click to native entry, at most 30 seconds. |
| Native initialization | Native entry to completed initialization, at most 30 seconds. |
| First render submission | Initialized renderer to successful main-frame queue submission, at most 5 seconds. Bootstrap uploads with frame zero do not qualify. |
| First GPU completion | First render submission to observed completion of its existing frame fence, at most 1 second. |
| Steady rendering entry | First completed frame to a second distinct completed frame, at most 1 second. |
| Overall startup | At most 60 seconds after Start, and an absolute 120-second process-to-steady cap. Repeated milestones cannot restart either bound. |
| Shutdown | Explicit shutdown, native cleanup and process-exit milestones; at most 2 seconds for orderly exit. One exact-child termination attempt if necessary. |

This separates supervised UI waiting from active startup; it does not enlarge the renderer/GPU stall allowances. The existing 500 ms completed critical-operation, 1 second unreturned-operation/progress and memory/validation/capture failure rules remain. The authorized 20-second rendering duration begins at the second GPU completion.

The managed Start action rejects a revoked or expired observer lease. Native entry checks the same lease **before window/Vulkan initialization**, and again between initialization phases. Cancellation is a distinct controlled path. An old stop request cannot silently enter GPU initialization after the UI/loading handoff.

Lifecycle recording is bounded, with no render-thread file writes or per-frame allocation. The observer retains the small lifecycle file beside the rolling journal; the native producer stamps only the first submission/completions and lifecycle transitions. Without diagnostic environment variables the channel is inactive.

Implementation: `src/NovaCore.Interop/DiagnosticStartup.cs`, `native/NovaCore.Native/StartupLifecycle.h`, observer `StartupLifecycle.cs`/`Program.cs`, and the managed/native lifecycle hook sites. New permanent tests are in `StartupRegression.cs` and the native CPU recorder mock.

## Offline and recovery qualification

Both Debug and Release passed:

- Native, application, observer and Graphics-test builds; managed builds have zero errors/warnings.
- 27 production-policy/managed-producer checks: slow valid startup beyond the old process-wide 60 seconds, absent native handoff, incomplete initialization, initialization without submission, first-frame GPU stall, absent second completion, exact deadlines, overall bounds, immutable/order-valid milestones, frame-zero exclusion, and orderly shutdown at all eight tested prefixes.
- Six native admission checks: explicit stop, expired heartbeat, shutdown already requested, missing managed handoff, wrong process ownership, and valid admission. All were CPU-only. Rejected cases made zero simulated GPU calls. Frame-zero and duplicate-completion exclusions were checked directly.
- 22 cross-process observer scenarios per configuration: normal/workload/cleanup-linger traffic; simulated fatal result, stall and recorder overflow; first-submission and first-completion deadlines; early shutdown prefixes; late native admission; and frozen normal/large/stale/storage/abrupt/backpressure paths. Every expected pass or fail-closed result matched.
- Frozen evidence mutation/ownership/membership regression suites and intact-journal inspection.
- Existing CPU tessellation, pupil handoff and presentation-result checks. The application CPU route passed **9,918 checks in each configuration**. Accepted correction/regression sources remain byte-identical to entry.
- Killing only the CPU mock's observer caused the held child process to exit with code zero after **1,019.954 ms Debug / 1,009.899 ms Release**. No renderer was launched by this recovery test.

`qualification-results.json` retains 68 successful command expectations; the observer-loss checks are separate. `preflight.json` admits only one distant-Earth attempt, 20 seconds, and a versioned lifecycle contract. Its 253 sealed files/authorities were checked before launch and again after termination.

Qualification bookkeeping corrections are retained, not hidden: an initially selected aggregate application regression stopped at its stale-deployment check before any window launch; source inspection showed that aggregate includes GPU window tests, so it was replaced with the explicit CPU application route. The first observer-loss harness did not retain a process handle for reading the exit code; the corrected harness retained the exact handle and passed. Neither initial harness result is counted as a qualification pass.

## Actual supervised attempt

One diagnostic application process, PID **28408**, began at **19:59:08.518 UTC**. The UI selected the same 960 × 540 windowed preset as the preceding attempt; the application's existing minimum client sizing produced a **960 × 582 render extent**. No camera zoom/orbit or closer-Earth stage was performed. Saved display preferences were restored exactly afterward.

| Observed transition | Elapsed time |
|---|---:|
| Process launch → UI ready | 0.415485 s |
| Process launch → Start click | 59.370561 s |
| Start click → native entry | 5.339075 s |
| Native entry → initialization complete | 0.622967 s |
| Initialization complete → first render submission | 14.553 ms |
| First submission → observed first GPU completion | 222.067 ms |
| First completion → second distinct completion | 2.502 ms |
| Process launch → first GPU completion | 65.569223 s |

The 222 ms value is wall time until the CPU observes fence completion, including scheduling/resize work; it is not the first frame's GPU execution time. The real run therefore crossed the old process-wide 60-second cutoff while all new startup phase bounds remained valid. Startup/loading was not mislabeled as a GPU stall.

The causal journal records successful submission of **frames 1–167**, with successful GPU completion through **167**. The first rendered submission is sequence **64** (journal serial **523**); the last is sequence **230** (serial **6768**). The last completion is serial **6775**. Lifecycle first-submitted/first-completed identities are **1/1**, and steady identity is **2**; their QPC stamps join to the queue/fence records.

After the initial low-workload solar view, distant Earth was selected at **67,195,064 m altitude**. Frames 166 and 167 were submitted and completed with Earth active. Three Earth-target snapshots were observed, including the unsubmitted frame 168 snapshot; **three snapshots are not three completed Earth frames**.

The prepared level-0 generation **1**, pupil **1**, topology hash **0x118D7D350CB66136** had **13,826 vertices / 27,648 triangles**. Publication after frame 167 reported **15,368 retained triangles**, **12,280 horizon rejects**, and **46,104 compacted indices**. These are actual completed GPU counter observations, but no frozen membership dataset was retained; they do not qualify exact visibility membership or replace the missing historical authority.

## Stop boundary and evidence limits

Frame **168** acquired swapchain generation **2**, then entered command recording at serial **6791**. The last retained event is serial **6808**, the wrapper's pre-call marker for the final three-vertex draw. There is no matching Record end, frame-168 Submit, FrameAuthority record, scheduled-capture record, or completed frozen file.

The observer stopped after Record had remained open for **1.009873 seconds**. Shutdown was acknowledged by the managed lifecycle channel, but native cleanup and application-exit milestones did not arrive. The observer terminated its exact child after the existing two-second grace period. Exit code was **-1**, `terminated=true`; no relaunch followed.

**Observed boundary:** CPU command recording after the final draw marker and before successful frame submission/frozen publication. **Not established:** which call or operation within that interval failed to return. The untraced tail includes the draw call itself, render-pass/label closure, frozen authority construction and transfer-command recording, and command-buffer completion. The last marker alone cannot identify one of these as the cause. Frame 168 was not GPU-submitted, and all previously submitted frame identities had completed. This is not proof of a GPU execution stall or the original blackout's cause.

All **6,808** emitted records were received and durable; no corruption, gaps, ring overwrite or contention drops. No recorded Vulkan error, device loss, invalid-triangle count or compaction overflow occurred before the stop. Observer maximum loop interval was **47.119 ms**, maximum journal flush **2.565 ms**. Forced termination prevented native resource cleanup evidence; the last live allocation balance is not evidence of a persistent system leak.

There were **zero scheduled/completed frozen captures**, zero live membership validations, and no capture/readback overhead samples. These missing costs are **unavailable, not zero**. The existing capture mock qualification does not fill this live gap.

## Measured costs and residency — restricted to completed observations

Milliseconds; nearest-rank quantiles. These are absolute observed costs, not an observer-on/off overhead comparison and not a qualified capture frame cost.

| Measurement | Samples | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|---:|
| GPU timestamp total, aggregate only | 166 | 0.13120 | 0.13260 | 1.02840 | 1.69905 |
| CPU command recording, returned frames only | 167 | 0.17430 | 0.26070 | 0.40930 | 0.44800 |
| CPU frame-fence API wait | 168 | 4.01400 | 4.28650 | 4.39180 | 5.50710 |
| CPU image acquire | 168 | 0.04720 | 0.09210 | 0.14130 | 0.43430 |
| CPU queue submit, including 63 bootstrap uploads | 230 | 0.04460 | 0.08540 | 0.10260 | 0.14410 |
| CPU present | 167 | 0.16280 | 0.24000 | 0.40420 | 27.14980 |
| GPU capture/transfer; CPU capture/inspection/hash/write | 0 | unavailable | unavailable | unavailable | unavailable |

The non-returned frame-168 recording is excluded from the returned-duration distribution and remains the material abnormality, not a small-cost outlier. Recurring returned recording maxima were frames **1: 0.4480 ms, 167: 0.4093 ms, 166: 0.3670 ms**; fence maxima were **5: 5.5071 ms, 86: 4.3918 ms, 76: 4.3100 ms**. Present frame 1 took 27.1498 ms; the next maxima were frame 45 at 0.4042 ms and frame 2 at 0.3917 ms. Top-ten records remain in `live-01-performance.json`.

**Timing identity limitation:** the native general GPU-timing records carry their legacy anchored-terrain frame field as **zero** for these 166 samples. Their aggregate values are retained, but their per-frame payload identity is not qualified. The separate queue/fence progress identities above are intact. Do not interpret the measurement file's repeated `frame: 0` worst-case labels as one actual rendered frame or claim per-frame readback-cost matching.

Application-recorded allocation high water: **570,590,716 bytes** (135 creates, 17 frees at last record). Two frozen readback buffers reserve **184,549,376 bytes** of that total. Resource ledger balance at termination remains 570,590,716 bytes because cleanup was not observed.

`VK_EXT_memory_budget` produced two samples for each heap. Maximum observed heap-0 usage was **311,934,976 / 15,897,839,616 bytes budget**; heap-1 usage **252,747,776 / 16,325,505,024**. Neither sample approached the observer's headroom stop condition. These sparse samples do not certify instantaneous peak residency.

## Preservation, remaining work and Project Control stop

The application session was copied before further work. Read-only Application/System event queries from 19:59:03.518 through 20:02:46.843 UTC succeeded and found no relevant application-error, hang, .NET, display-reset, WHEA, power or bugcheck events. WER archive/queue had no newly modified top-level items. `C:\Windows\LiveKernelReports` denied access; access controls were not changed. Absence of these events does not establish an external cause or clear the incident.

`preservation.json` verifies scoped source changes, accepted tessellation/pupil regressions, the eight historical live files, the preceding failed attempt, Git state, ordinary executable and restored settings. `source-at-live` retains the revised source and the pre-report README against the live authority seal. The report/README publication occurs after the observer's successful post-run authority verification. `current-task-manifest.json` identifies the final unbanked files/builds; `qualification-blocked.json` and the consumed live gate prohibit further exposure.

Remaining unknowns and necessary next evidence:

1. Identify the exact CPU owner/call in frame 168's recording tail using bounded offline qualification and more precise diagnostic boundaries if required. No timeout relaxation or arbitrary quality change.
2. Complete actual GPU-produced frozen transport, exact membership/generation joins, and readback overhead qualification. This attempt provides none of those capture results.
3. Reconcile general GPU-timing payload identity with its owning completed submission before making per-frame cost claims.
4. Demonstrate orderly shutdown/recovery when the newly observed recording boundary is exercised. CPU mocks passed; this live path required termination.
5. The original blackout cause, historical 411,877-triangle discrepancy, close-Earth GPU route, and native-resolution borderless incident pattern remain unresolved/unqualified. The old missing pupil/geometry/membership authority cannot be reconstructed by assumption.

**STOP for Project Control. No closer Earth, ground/horizon, orbit or original incident exposure is authorized by this result.**
