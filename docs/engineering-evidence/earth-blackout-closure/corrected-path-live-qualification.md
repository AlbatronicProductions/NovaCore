# Corrected-path distant-Earth qualification — limited PASS / UNBANKED

## Judgment and stop

**PASS — the corrected AuthorityScalars path and frozen GPU capture/readback mechanism passed this one supervised distant-Earth qualification.** Frame 168 returned through all 23 scalar stores and all 50 outer operations. Rendering continued to frame 3,602; 20 GPU capture publications were validated; both retained captures pass exact internal membership and authority checks. The observer stopped at the existing 20-second stage limit and the application exited normally.

This is a limited diagnostic-route PASS. **September 22 dual-monitor blackout: UNRESOLVED. Historical 411,877-triangle discrepancy: UNRESOLVED. Manual Player acceptance: ON HOLD.** No closer Earth, ground/horizon, orbit or original incident route was attempted. **STOP for Project Control review and fresh authorization before any increased exposure.** No commit, tag, push, banking or milestone promotion.

## Candidate, display and single attempt

Evidence: `E:\NovaCore\build\earth-blackout-closure\corrected-live-qualification`; run: `live-01-distant`. The accepted corrected candidate is under `authority-scalars`: Debug application/native DLL and Release observer. Preflight and postflight verified **941 unique sealed identities**, covering 61 source/report files, 780 build files, 97 qualification artifacts and three terrain authorities. No implementation or instrumentation change was made for this run.

The approved temporary preset was prepared before launch: **960×540 windowed**, with other preferences unchanged. UI inspection confirmed the selected resolution, unchecked borderless control and closed resolution list. The supervisor clicked Start once without opening a display popup. The existing minimum window sizing yielded a **960×582 render extent**, matching the earlier distant-Earth attempt. Original saved preferences were restored byte-for-byte after exit. No camera input was sent.

The observer launched application PID **17500**, once, at **2026-09-22 22:50:03.0599355 UTC** (18:50:03.0599355 EDT). The existing diagnostic route selected distant Earth after its initial solar-system view. Earth altitude remained **67,195,064 m** throughout recorded target observations. The local authorization is consumed and its live gate is closed.

| Startup / termination interval | Measured duration |
|---|---:|
| Launch → UI ready | 0.422 s |
| Launch → Start click | 30.021 s |
| Loading handoff | 5.416 s |
| Native initialization | 19.139 s |
| Native ready → first submission | 18.364 ms |
| First submission → observed GPU completion | 176.429 ms |
| Steady rendering → duration stop | 20.006 s |
| Stop → native cleanup complete | 262.786 ms |
| Stop → application exit | 297.039 ms |

First-completion and shutdown values are CPU-observed wall time, not GPU execution time. The original UI/startup/render/recovery deadlines remained unchanged. Exit code **0**, no forced termination, observer stop reason **stage duration reached**, producer stop reason **observer requested stop**.

## Frame and capture evidence

- **3,602 rendered frames submitted; 3,602 GPU-completed**, with matching final identity.
- **3,437** Earth-target snapshots; no closer stage or camera movement input.
- **206,546 records emitted, received and durably recorded**, with zero producer overruns, contention drops, torn records or checksum errors.
- The bounded journal retains its last **131,072 records**, serials **75,475–206,546**, covering **12.777 seconds**. Intentional rolling retirement of older records is distinct from producer loss.
- **2,254 complete frame-authority assemblies**, zero partial assemblies, and no retained submission/completion join errors.
- The online frame-168 trace consumer reports **149 valid records**, all 50 outer operations returned through `EndCommandBuffer`, all 23 scalar stores returned through `Flags`, and no pending call. Capture **1 / frame 168** passed the independent background membership check.
- All **20** publication identities were checked in order with zero membership errors or validation backlog. The two alternating files retain captures **19 and 20**.

The raw first trace and first seven capture timing records aged out normally. The online trace summary and all 20 live capture-validation results remain. No retrospective raw operation timing or complete raw first-trace retention is claimed for this successful run. The last 13 captures retain exact timing, submission and completion records.

## Both retained frozen captures

| Authority / population | Capture 19 | Capture 20 |
|---|---:|---:|
| Rendered / completed frame | 3,408 / 3,408 | 3,588 / 3,588 |
| Submitted / completed submission sequence | 3,471 / 3,471 | 3,651 / 3,651 |
| Active / incoming prepared generation | 1 / 0 | 1 / 0 |
| Prepared / cull / raster pupil | 1 / 1 / 1 | 1 / 1 / 1 |
| Swapchain generation | 2 | 2 |
| Vertices / source triangles | 13,826 / 27,648 | 13,826 / 27,648 |
| Visible triangles / TCS invocations | 15,369 / 15,369 | 15,368 / 15,368 |
| Horizon rejects | 12,279 | 12,280 |
| Screen-cone rejects / invalid triangles / overflow | 0 / 0 / 0 | 0 / 0 / 0 |
| Compacted indirect indices / TES invocations | 46,107 / 46,107 | 46,104 / 46,104 |
| Frozen file bytes | 1,663,508 | 1,663,508 |

Both are GPU-produced, non-synthetic captures with present success, topology family 1/hash `0x118D7D350CB66136`, physical generation 4, terrain version 5 and extent 960×582. Physical geometry and source-index hashes are identical between the two captures. Every captured physical position/height/normal/validity component is finite.

The validator checks committed-file SHA-256, section layout/ranges, every source and compacted vertex index, binary visibility flags, exact triangle-multiset membership, indirect draw arguments, input/reject accounting, TCS population and the accepted finite tessellation domain. Each visible triangle multiset exactly equals that capture's compacted draw multiset; the one-triangle population difference between frames is accounted for by their own horizon-rejection counters. Each capture retains its own live input bytes and hashes. No approximation is used to force the populations equal.

The frozen authority hash matches the same frame's journaled authority, with its exact submission/completion sequence. The six source GPU buffer incarnations each have exactly one valid owner at capture time. Prepared pupil bytes match the stated identity. `qualification-audit.json` and `frozen-analysis.json` preserve complete hashes, fields and joins.

These checks establish internal capture and GPU-membership consistency. They do not independently re-prove every shader culling decision, qualify a near-horizon workload, or reconstruct the missing historical session authority.

## Absolute capture and readback cost

All values below are **milliseconds**. Percentiles use nearest rank. The 13 retained capture samples are identities **8–20**, frames **1429–3588**. At this sample count P95 and P99 both select the maximum; these are measured values, not stable tail estimates.

| Measurement | Samples | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|---:|
| CPU capture-path recording wall time | 13 | 0.2376 | 0.3271 | 0.3271 | 0.3271 |
| CPU authority-only path, frames without capture | 2,241 | 0.2083 | 0.2491 | 0.2828 | 0.3524 |
| CPU full command recording, capture frames | 13 | 0.4518 | 0.5757 | 0.5757 | 0.5757 |
| CPU completion inspection wall time | 13 | 0.0109 | 0.0185 | 0.0185 | 0.0185 |
| GPU transfer and surrounding barriers | 13 | 0.06604 | 0.06664 | 0.06664 | 0.06664 |
| GPU full frame including capture | 13 | 0.28280 | 0.29344 | 0.29344 | 0.29344 |
| GPU frames without capture | 2,241 | 0.21572 | 0.22576 | 0.22768 | 0.22992 |
| Background hash/write wall time | 14 | 3.320 | 3.973 | 3.973 | 3.973 |
| Background capture-validation wall time | 20 | 3.7651 | 14.8508 | 49.1087 | 49.1087 |

Capture-path recording includes authority construction and scheduling; completion inspection covers the existing post-fence query/metadata work. These CPU values are wall time. GPU transfer/barrier cost comes from timestamps surrounding the real copies and barriers. Full-frame GPU timing ends after diagnostic copies, so their work is included. All **2,254** retained general GPU timing payloads identify the same completed frame as their envelope; capture timings join exact capture identities and submission sequences. The earlier startup identity-zero limitation does not affect these retained distant-Earth samples.

The recurring capture-frame GPU increase is visible: 0.28280 ms median versus 0.21572 ms for other frames, alongside the directly measured 0.06604 ms transfer/barrier median. This is descriptive same-run evidence, **not** a diagnostics-disabled A/B measurement. Total instrumentation overhead relative to an uninstrumented application is not isolated by this authorization.

Repeated high cases remain bounded: GPU capture-frame costs **0.29344 / 0.29160 / 0.28864 ms** at frames **2329 / 3049 / 1789**; transfer/barrier costs **0.06664 / 0.06652 / 0.06652 ms** at **2689 / 1429 / 3588**. CPU capture recording reaches **0.3271 ms** at 3408, then **0.2708 / 0.2668 ms** at 3588/1789. All occurrences and ten worst cases are retained in `performance.json`.

Background validation took **49.1087 ms** on the first capture and **14.8508 ms** on the second, then 3.2371–5.2223 ms for the remaining 18. It ran off the heartbeat/render paths. Writer thread CPU accounting has coarse **15.625 ms** quantization: median 0, P95/P99/max 15.625 ms across 14 retained observations. Those zeros do not mean zero CPU work; wall measurements are more informative here.

The first marked capture's native CPU/GPU timing is no longer in the rolling window. No value is fabricated from later samples or offline mocks.

## Synchronization, residency and termination

| CPU-observed wait | Samples | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|---:|
| Frame-fence API wait for capture submissions | 13 | 3.4772 ms | 3.7312 ms | 3.7312 ms | 3.7312 ms |
| Frame-fence API wait for other submissions | 2,240 | 3.4120 ms | 3.6540 ms | 3.7184 ms | 3.8019 ms |

The two retained device-idle calls took **0.0086 and 2.4323 ms**. No unresolved phase remained. CPU submission max was **0.1629 ms**, present max **0.3939 ms**. Observer-loop maximum was **32.0635 ms** and journal-flush maximum **5.0198 ms**; no heartbeat loss, capture-owner failure, publication delay or stop threshold was triggered. No extra fence or ownership policy was introduced.

Tracked Vulkan allocation high-water was **570,590,716 bytes**. All **135 allocations** were freed, leaving **0 tracked bytes** at exit. The last resource birth was frame **166**; no later allocation growth occurred. The increase beyond the earlier stalled run's approximately 450.85 MB is identified in the ledger: a **119,737,728-byte regional payload buffer** became resident, not an unbounded capture queue. Two preallocated readback buffers remain **92,274,688 bytes each** while active.

Topology residency peaked at **552,992 bytes**; terrain working storage at **1,327,420 bytes**. There was one topology upload and one working allocation, with no work reuse required. These categories overlap the allocation ledger and must not be added as independent totals.

| Heap | Budget samples | Maximum sampled usage | Sampled budget | Minimum sampled headroom |
|---|---:|---:|---:|---:|
| 0 | 13 | 431,677,440 B | 15,897,839,616 B | 15,466,162,176 B |
| 1 | 13 | 272,670,720 B | 16,325,505,024 B | 16,052,834,304 B |

No sampled budget pressure occurred. Heap samples are not continuous device-wide maxima; tracked Vulkan allocations include more than device-local residency. No Vulkan/device-error or diagnostic-loss record occurred. Read-only Windows Application/System checks found no warning/error/critical events during the bounded live interval. This absence does not establish or exclude an external cause for the original blackout.

## Preservation and remaining limits

The approved diagnostics and recovery implementation were reused unchanged. Source/build/terrain identity, prior evidence, original player settings, Git HEAD/index/refs and the ordinary application executable are verified in `preservation.json`. The one allowed attempt is consumed. Production implementation writes: **0**. No driver/system/KSA changes.

A postflight read-only inspector was initially given a file path instead of its required directory. That invocation failed after the live application had exited; the corrected invocation passed. This did not launch the application, alter raw evidence, or change the successful live termination classification.

Diagnostic output created is approximately **0.074 GB**, bounded to **512 MiB**. Permanent report evidence is approximately **0.014 MB**, within a **64 KiB** budget. Temporary journals, the two frozen captures, source copies and derived audits are retained for Project Control's review of this active investigation. Approximately **0.074 GB** is eligible for consolidation/disposal only after appropriate acceptance and authorized evidence retirement; no prior evidence was deleted.

Remaining limits: the historical discrepancy lacks its old authoritative state; the original blackout has not been reproduced or causally explained; first-capture native timing and the raw first trace aged out; tail stability and diagnostics-disabled overhead were not established; closer/horizon/orbit/native-resolution incident exposure remains unauthorized. **Manual acceptance remains ON HOLD. STOP here.**
