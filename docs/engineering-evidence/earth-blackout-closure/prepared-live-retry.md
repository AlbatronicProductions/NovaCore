# Prepared-display live retry — recording boundary identified / REVISE

## Result

**Outcome A: the frame-168 CPU command-recording stall recurred. Operation 7 (`HeaderClock`) returned; operation 8 (`AuthorityScalars`) was entered and has no return marker. The run stopped; no relaunch occurred.**

The accepted call instrumentation narrowed the event to frozen-capture header construction in `FrozenFrameAuthority`, `native/NovaCore.Native/FrozenCaptureNative.inl:45–55`. The marked body reads scalar state and fills header fields. It contains no Vulkan API call. This establishes the unresolved diagnostic metadata boundary, not a particular failing scalar read, machine instruction, exception, or driver operation. No thread stack or instruction-pointer capture exists for this attempt.

**Blackout cause UNRESOLVED. Historical 411,877-triangle discrepancy UNRESOLVED. Manual Player acceptance ON HOLD. UNBANKED.** No causal closure, GPU readback qualification PASS, hardware/driver attribution or milestone advance is claimed.

## Authorization, preparation and startup

Fresh Project Control authority allowed exactly one same distant-Earth attempt after preparing the approved display preset before launch. The temporary settings changed only `WindowMode=Windowed` and `Resolution=960x540`. Original bytes were retained. All other player preferences, quality settings, ownership and stop thresholds were unchanged.

Before launch the saved preset was read back and verified. Desktop inspection found no existing NovaCore window or popup. The one observer-owned application launch then showed **960 × 540**, an unchecked borderless box and a closed resolution list. The supervisor clicked **Start NovaCore** directly without opening a display control. The existing minimum application sizing produced the same **960 × 582 render extent** as the earlier rendered attempt. There was no manual zoom, orbit or closer-Earth input; the existing `distant-earth` diagnostic route selected Earth after the initial low-workload solar view.

Evidence root: `E:\NovaCore\build\earth-blackout-closure\prepared-live-retry`; actual run: `live-01-distant`. The qualified Debug application and Release observer were reused from `recording-call-instrumentation`; no rebuild or production edit occurred. Preflight and postflight verified **512 unique sealed identities**: 58 source/report files, 420 build files, 31 qualification artifacts and three terrain authorities. The qualified 50-operation implementation, Debug/Release regressions and existing recovery thresholds remain unchanged.

Application **PID 16692** started at **2026-09-22 22:09:20.2339506 UTC** (18:09:20.2339506 EDT). Observer PID was 20000. Only one attempt was consumed.

| Transition | Measured duration |
|---|---:|
| Observer launch QPC → UI ready | 0.3943197 s |
| Observer launch QPC → Start click | 32.6498598 s |
| Start → native initialization entry | 5.4895491 s |
| Native initialization | 18.6257057 s |
| Native ready → first submission | 17.0667 ms |
| First submission → observed first GPU completion | 183.6729 ms |
| Observer launch QPC → first GPU completion | 56.9658542 s |
| Steady rendering → stop decision | 1.9302069 s |

The first-completion duration is CPU-observed wall time, not GPU execution time. Native initialization was slower than in the earlier rendered attempt but remained inside its unchanged 30-second phase bound. No startup timeout was relaxed. The nominal 20-second rendering allowance ended early on the recording abnormality.

## Exact retained recording boundary

The journal contains **6,820 contiguous, checksum-valid records**: emitted = received = durable, with no gap, overwrite, corruption or producer contention drop. All **16** detailed trace records share frame, command and prepared/resource ownership identity. The full trace start and seven successful enter/return pairs precede operation 8's entry; the trace is incomplete because operation 8 did not return.

| Observation | Identity |
|---|---|
| Last submitted rendered frame | **167**, submission sequence **230**, journal serial **6764** |
| Last successful main-frame fence wait | Serial **6769**, result success; **1.1672 ms** wall time |
| GPU completion observation | Frame **167**, serial **6771** |
| Unsubmitted recording frame | **168**, swapchain generation **2** |
| Last returned call | **7 — HeaderClock**, serial **6819** |
| Entered call without a return | **8 — AuthorityScalars**, ordinal **8**, serial **6820** |
| Command buffer | **1888345303616** |
| Renderer thread | **15916** |
| Frame fence | **380431023210842** |
| Stop decision after Record begin | **1003.3414 ms** |
| Stop decision after operation-8 entry | **1003.0740 ms** |
| Last retained observer heartbeat after operation-8 entry | **3018.4413 ms** |

The observer reason `renderer operation 8 has not returned for 1s` uses **phase 8 = command recording**. Independently, the new call trace uses **operation 8 = AuthorityScalars**. These are separate enumerations that happen to have the same number.

`FinalDraw`, `EndRenderPass` and the scene debug-label end all returned on the CPU before the captured boundary. This does not mean frame 168 executed on the GPU: it never reached submission or command-buffer end. The previous frame's full submission completed successfully.

The unresolved body fills frame/time/swap, generation/topology/pupil, population/work/extent and submission-derived physical-generation, terrain-version and flag fields. The entry marker also retains frame-buffer context: buffer **544258255749615**, mapped address **1888167936000**, size **480 bytes**, memory **545357767377392**. Those arguments are context, not proof that the mapped buffer caused the stall. The separately marked mapped incoming-pupil read, operation **49**, was **not reached**.

The last marker identifies the production-source interval. It does not identify an exact scalar access, nor exclude a failure at the marker's own return boundary without an instruction/stack sample. No correction is made on the strength of an unproven instruction-level explanation.

## Prepared generation, resources and capture ownership

| Retained state | Observation |
|---|---|
| Active / incoming generation | **1 / 0** |
| Prepared / cull / raster pupil identity | **1 / 1 / 1** |
| Topology | L0, family 1, hash **0x118D7D350CB66136** |
| Prepared population | **13,826 vertices / 27,648 triangles** |
| Completed published population | **15,368 retained triangles**, **12,280 horizon rejects**, **46,104 compacted indices** |
| Active / incoming preparation fence pending | **false / false** |
| Readback slot 0 | **Free**, identity **0**, buffer **385928581349727** |
| Readback slot 1 | **Free**, identity **0**, buffer **388127604605281** |
| Capture timing pool | **384829069721950** |
| Frozen reservation / scheduling / completion | **Not reached / none / none** |

`recording-boundary.json` retains every physical/index/visibility/compacted/counter/indirect handle and its relevant lifetime record. The relevant buffers have recorded births and no destruction before the stop. Both slots were directly observed free at trace entry; execution did not reach reservation. There was no pending GPU readback, writer-owned slot, writer reuse wait or submitted frame-168 work.

Publication diagnostics report physical/normal/cull/compact readiness, valid indirect work, completed preparation fence and one atomic publication, with no invalid, stale-generation, zero-owner or overlapping-owner draw. These are retained counters, not a frozen membership proof: no capture completed, so exact visibility/compaction membership qualification remains incomplete.

The final camera snapshot records distant Earth at **67,194,784 m altitude**, body-relative position `(12747796.224764675, 67495551.79023543, 26340297.338878512)` m and forward direction `(-0.1732836365699768, -0.9174820780754089, -0.35804951190948486)`. Three Earth-target snapshots include unsubmitted frame 168; only frames **166–167** completed with Earth active. No frozen projection/geometry payload was retained, and none is reconstructed by approximation.

## Cost, residency and recovery limits

Capture/readback CPU cost, GPU transfer/barrier cost, completion-inspection cost, writer cost, and their median/P95/P99/repeating worst cases are **unavailable: zero completed/scheduled captures**. No zero-cost claim or overhead qualification follows from the interrupted header construction.

The resource ledger's recorded allocation high-water was **450,852,992 bytes**. Each of two readback buffers was **92,274,688 bytes**. Recorded topology residency was **552,992 bytes** and terrain working storage **1,327,420 bytes**, each at its reported high-water, with one topology upload, one work allocation and no recorded reuse. These categories overlap the allocation ledger and must not be summed as independent totals.

| Heap | Maximum sampled usage | Sampled budget | Minimum sampled headroom |
|---|---:|---:|---:|
| 0 | 311,934,976 B | 15,897,839,616 B | 15,585,904,640 B |
| 1 | 252,747,776 B | 16,325,505,024 B | 16,072,757,248 B |

No sampled budget pressure was observed. The values are bounded samples, not a continuous device-wide residency maximum. The retained generic GPU timestamp payloads still carry the known legacy frame identity zero. Their 166-sample aggregate is retained in `performance.json` with explicit limits; it cannot be joined to exact rendered frames or presented as distant-Earth capture cost.

The observer retained its heartbeat through the stop and shutdown grace. The UI acknowledged shutdown, but native cleanup did not finish; the observer terminated its exact child after the unchanged two-second grace. Exit code was −1. No Vulkan/device-error record was emitted. These observations do not establish that a driver/device fault is impossible.

Read-only Windows checks from 22:09:19 UTC through 22:11:54.8858584 UTC found no matching Application/System warning/error/critical events and no newly modified files in accessible WER archive/queue inventories. `C:\Windows\LiveKernelReports` was inaccessible; permissions were not changed. Absence of these records does not establish external causation or disprove the original blackout.

## Preservation and next responsibility

The original display-preference bytes were restored exactly after both processes exited. `preservation.json` checks Git HEAD/index/refs, unrelated source, all qualified build/probe identities, 25 files across both preceding live attempts and eight historical files. No driver/system/KSA changes, production edits, timeout changes, quality reductions, commit, tag, push or banking occurred. Source excerpts and hashes needed to interpret the marked region are retained under `source-at-live`; the pre-report README and accepted manifest are retained separately.

New diagnostic evidence is bounded to **512 MiB**; the measured inventory and permanent report size are recorded in `preservation.json`. Retention is for the unresolved scalar-header recording boundary and its next engineering decision. No earlier evidence was deleted.

**STOP for Project Control review.** This authorization is consumed. The next bounded engineering responsibility is the CPU-side `AuthorityScalars` header construction and its authority/lifetime/marker boundary, using the now-retained production identities. The exact instruction and mechanism remain unproven. No further GPU attempt, diagnostic correction, closer stage or Player acceptance is performed under this authorization.
