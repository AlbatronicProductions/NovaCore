# Closer-Earth qualification — limited PASS / UNBANKED

## Judgment and stop

**PASS — this single authorized closer-Earth stage completed with coherent GPU progress, frozen captures and bounded residency.** All **3,593 rendered submissions completed**. Twenty GPU capture publications passed live validation; both retained frozen captures pass exact membership, authority and submission/completion checks. The stage stopped at its unchanged 20-second limit and exited normally. No implementation, instrumentation, shader, quality, tessellation, synchronization or deadline change was made.

Capture-submission fence median increased **3.4772 → 3.5972 ms (+0.1200 ms, +3.45%)** versus the accepted distant stage. One **6.0696 ms** non-capture wait coincided with the LOD-2-to-LOD-3 preparation/publication transition. It did not recur at the held waypoint. The evidence supports this bounded stage; the small capture sample does not establish stable timing tails or authorize a longer/higher workload.

**STOP for Project Control review before ground-visible, horizon, orbit or original incident exposure.** This one authorization is consumed. September 22 dual-monitor blackout: **UNRESOLVED**. Historical 411,877-triangle discrepancy: **UNRESOLVED**. Manual Player acceptance: **ON HOLD**. No commit, tag, push, banking or milestone promotion.

## Candidate, display and exposure

Evidence root: `E:\NovaCore\build\earth-blackout-closure\closer-live-qualification`; run: `live-01-closer`. The unchanged accepted `authority-scalars` candidate used its Debug application/native renderer and Release observer. Preflight and postflight checked **971 unique sealed identities**: 62 source/report files, 780 build files, 126 prior qualification artifacts and three terrain authorities. Source-at-live copies preserve the pre-report state. The ordinary player executable was not replaced.

Application PID **10512** started once at **2026-09-22 23:32:46.1945994 UTC** (19:32:46.1945994 EDT). The supervisor verified **960×540 windowed**, borderless unchecked, resolution popup closed, then clicked Start once. Existing minimum window sizing produced a **960×582 render extent**, as in the accepted distant run. Original saved preferences were restored byte-for-byte after exit.

The existing `closer-earth` route used ordinary focus and wheel input: one zoom detent every 0.4 seconds until the approach latched at altitude ≤1,000,000 m. No orbit/look/manual camera input or later stage was sent. The current **20-second steady-render cap** was retained; the older 60-second exploratory duration was not reinstated. After the initial view, Earth approach reached **968,386.25 m** minimum. Frames **1530–3593** provide **2,064** target observations, at 968,386.25–968,415.3125 m. This is the previously defined closer waypoint, not the near-ground/horizon route.

| Startup / termination interval | Duration |
|---|---:|
| Launch → UI ready | 0.411 s |
| Launch → Start click | 30.664 s |
| Loading handoff | 5.540 s |
| Native initialization | 0.613 s |
| Native ready → first submission | 16.217 ms |
| First submission → observed GPU completion | 237.597 ms |
| Steady rendering → duration stop | 20.011 s |
| Stop → native cleanup complete | 254.192 ms |
| Stop → application exit | 292.331 ms |

These are CPU-observed lifecycle intervals. The same UI/startup/render/recovery deadlines and fail-closed policy remained active. Stop reason: **stage duration reached**; producer reason: **observer requested stop**; exit **0**; no forced termination or process left running.

## Capture and identity validation

The live observer received and durably recorded all **206,128** emitted records, with zero producer overruns, contention drops, torn records, corruption or native errors. The bounded journal retains **131,072** records, serials **75057–206128**, spanning **12.763 seconds**. Its **2,253** complete authority assemblies have no partial assembly, retained submission/completion join error or unresolved phase.

The online frame-168 marker consumer again completed all **149 records**, **50 outer calls** and **23 scalar calls**, through `EndCommandBuffer`, with no pending call. Capture 1/frame 168 passed membership validation. All **20** capture identities were validated in sequence with no reported error or ownership failure. The two alternating files retain identities 19 and 20. The raw first trace and first seven capture timing records aged out of the bounded journal; their online success summaries remain. No retrospective raw first-call timing is claimed.

| Frozen authority / population | Capture 19 | Capture 20 |
|---|---:|---:|
| Rendered / GPU-completed frame | 3408 / 3408 | 3588 / 3588 |
| Submitted / completed submission sequence | 3471 / 3471 | 3651 / 3651 |
| Active / incoming prepared generation | 4 / 0 | 4 / 0 |
| Prepared / cull / raster pupil | 4 / 4 / 4 | 4 / 4 / 4 |
| Swapchain generation | 2 | 2 |
| Physical vertices / source triangles | 54,106 / 108,208 | 54,106 / 108,208 |
| Visible triangles / TCS patches | 4,633 / 4,633 | 4,633 / 4,633 |
| Horizon / screen-cone rejects | 28,924 / 74,651 | 28,924 / 74,651 |
| Compacted indices / TES invocations | 13,899 / 13,899 | 13,899 / 13,899 |
| Invalid triangles / compaction overflow | 0 / 0 | 0 / 0 |
| Draws / dispatches / dispatch groups | 13 / 3 / 3,383 | 13 / 3 / 3,383 |
| Frozen file bytes | 6,497,556 | 6,497,556 |

Both captures have present success, topology family 1/hash `0xDCF463780EF4E7C1`, physical generation 4, terrain version 5 and extent 960×582. Physical-geometry and source-index hashes match between captures; all captured physical position/height/normal/validity values are finite. Each capture retains its exact camera/projection and prepared-pupil bytes, geometry, membership and authoritative reconstruction inputs.

The official validator checks file/section hashes and ranges, every source/compacted index, binary visibility, exact visible-versus-compacted triangle multiset, indirect arguments, reject accounting, TCS population and finite tessellation domain. It joins the frozen authority hash to that frame's journal authority and exact submitted/completed identities, and verifies each of the six GPU source-buffer incarnations has one valid owner at capture time. Both joins are clean. Detailed fields, source handles and hashes are in `qualification-audit.json` and `live-01-closer/frozen-analysis.json`.

This is internal live GPU membership consistency, not an independent re-proof of every shader culling decision. It does not reconstruct the old session's missing authority or resolve its 411,877-triangle discrepancy.

## Terrain workload and resource ownership

Full-session publication records show four prepared generations with the accepted topology family:

| Generation / LOD | Physical vertices | Source triangles | Visible at publication | Topology resident bytes | Working bytes |
|---|---:|---:|---:|---:|---:|
| 1 / 0 | 13,826 | 27,648 | 15,368 | 552,992 | 1,327,420 |
| 2 / 1 | 18,706 | 37,408 | 11,470 | 1,301,184 | 3,123,320 |
| 3 / 2 | 29,626 | 59,248 | 6,064 | 2,486,176 | 4,640,120 |
| 4 / 3 | 54,106 | 108,208 | 7,602 | 4,650,368 | 8,038,520 |

All publications report physical/normal/cull/compaction/draw readiness, valid indirect arguments and completed fence, with atomic frame-boundary publication, no missing/overlapping owner and no stale-generation draw. The final viewpoint narrows generation 4 to approximately 4,633 visible triangles; this is a view-dependent population, not geometry loss.

The active production topology owns terrain here. The separate legacy planetary patch-list count remains **0** and is not a measure of this route's terrain workload. Retained tessellation counters report maximum outer factor **1.0**, inner factor approximately **0.9999**, and zero raster-domain diagnostic. The last frozen captures record 4,633 TCS patches and 13,899 TES invocations. Existing refinement limits were unchanged; this stage did not exercise high-factor near-surface refinement. A factor ceiling is not used as workload qualification.

Tracked Vulkan allocation high-water was **581,399,192 B**, versus distant **570,590,716 B**, an increase of **10,808,476 B**. All **156 allocations were freed**, leaving **0 tracked bytes**. All resource ledger incarnations have a recorded death. The last resource birth was frame **1459**, before the held waypoint; there were no later creates or unexplained growing queues.

The 21 allocations after the initial prepared state are exactly seven buffers for each of LODs 1, 2 and 3, born at frames **1096, 1313 and 1459**: topology lattice `vertices×16`, source indices `triangles×12`, physical vertices `vertices×64`, visibility `triangles×4`, compacted indices `triangles×12`, 20-byte indirect arguments and 168-byte counters. Sizes, identities and deaths are checked in `stage-comparison.json`. Four topology uploads and four working allocations occurred, with zero reuse hits on this monotonic approach. Older working storage was retired/replaced while immutable topology levels remained resident; the final state retained one spare work slot. This run does not qualify reversal/reuse under a returning camera route.

Topology residency peaked at **4,650,368 B** and terrain working storage at **8,038,520 B**. The regional payload remained **119,737,728 B**, scratch capacity 0, and the two preallocated readback buffers remained **92,274,688 B each**. These categories overlap the allocation ledger and are not independent additive totals.

| Heap | Retained samples | Maximum sampled usage | Budget | Minimum sampled headroom |
|---|---:|---:|---:|---:|
| 0 | 13 | 431,685,632 B | 15,897,839,616 B | 15,466,153,984 B |
| 1 | 13 | 284,205,056 B | 16,325,505,024 B | 16,041,299,968 B |

No sampled memory-budget pressure occurred. Budget observations are sampled, not continuous device-wide high-water measurements.

## Capture cost and synchronization

All costs below are **milliseconds**, nearest-rank percentiles. Thirteen later capture timings remain (IDs 8–20); capture 8/frame 1428 occurred during approach. The table isolates the **12 captures at the held closer waypoint** (IDs 9–20). P95 and P99 select the maximum at this sample count and remain provisional.

| Measurement at held waypoint | Samples | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|---:|
| CPU capture-path recording wall time | 12 | 0.2354 | 0.3024 | 0.3024 | 0.3024 |
| CPU full command recording, capture frames | 12 | 0.4336 | 0.5265 | 0.5265 | 0.5265 |
| GPU transfer and surrounding barriers | 12 | 0.24108 | 0.24268 | 0.24268 | 0.24268 |
| GPU capture-frame timestamp interval | 12 | 0.60744 | 0.64088 | 0.64088 | 0.64088 |
| GPU frames without capture | 2,051 | 0.39548 | 0.39820 | 0.40104 | 0.40568 |
| Submission-fence API wait, captures | 12 | 3.5972 | 3.8347 | 3.8347 | 3.8347 |
| Submission-fence API wait, other frames | 2,051 | 3.4635 | 3.6758 | 3.7371 | 3.8022 |

CPU capture-path recording includes authority construction and scheduling. GPU capture-frame timing spans the production frame timestamps through the final readback: from the timestamp immediately after production uploads to the final bottom-of-pipe timestamp. Uploads preceding the first timestamp are outside that interval. CPU full command-recording timing includes the enclosing recording call. GPU and CPU wall costs are separate and must not be added as a single critical-path estimate.

Across all 13 retained captures, CPU recording median/max is **0.2409/0.3024**, full CPU recording **0.4607/0.5490**, completion inspection **0.0102/0.0198**, GPU capture interval **0.60744/0.64088**, and GPU transfer/barriers **0.24108/0.24268**. Thirteen capture submissions join exact scheduled/submitted/completed identities. All **2,253** retained general GPU timing payloads identify their completed frame. Final frame totals include orderly completion even where no further timing sample was emitted during shutdown.

Distant versus held closer medians are CPU capture **0.2376 → 0.2354**, GPU transfer/barriers **0.06604 → 0.24108**, GPU capture interval **0.28280 → 0.60744**, and capture fence **3.4772 → 3.5972**. The GPU cost increases with the larger captured geometry and rendering workload. Same-run capture/non-capture comparisons are descriptive; no diagnostics-disabled control was authorized, so total instrumentation overhead is not isolated.

The last six held capture GPU intervals are **0.62120, 0.62588, 0.61884, 0.63040, 0.64088, 0.63516 ms**, modestly above the earlier six; transfer/barrier cost remains approximately 0.241 ms. This drift is retained for review rather than treated as stable tails. Capture-fence first/second-half medians are **3.5736/3.6365 ms**. Across all held frames, fence P95 is **3.6771/3.6778 ms** and P99 **3.7380/3.7371 ms** in the two halves. No held wait exceeds 4 ms; that is a reporting comparison, not a new runtime threshold. The worst capture waits are **3.8347, 3.7194 and 3.7178 ms**, frames **3048, 2508 and 3228**. No repeating stall or escalating fence tail was observed in this short window.

## The transition spike, with ownership preserved

The only retained fence wait above 4 ms was **frame 1462 / submission 1525: 6.0696 ms**, during approach at altitude **1,210,481.125 m**. This was not a capture frame. Its full GPU timestamp interval was **3.65956 ms**, with **2.78360 ms** in the pre-render compute interval and **0.23588 ms** in the terrain-draw interval. Neighboring full GPU intervals at frames **1460, 1461, 1463, 1464** were **1.46140, 1.22716, 1.73080, 1.37008 ms**. Those are the only five retained intervals above 1 ms.

Production ownership explains which workload was present: frame 1462 recorded generation **3 / LOD 2 / 59,248 source triangles**, with generation **4 incoming**. After its fence completed, serial **82172** retains generation 3 and 1,970 visible triangles; after `InspectProductionBillboardPublication`, serial **82178** retains newly published generation 4 and 7,602 visible triangles. Frame 1463 then records generation 4. The second completed snapshot is a post-publication resource state, not evidence that the completed frame 1462 drew generation 4. The frozen contract preserves the earlier authority before this publication.

This positively associates the spike cluster with the prepared-generation transition and concentrates measured GPU time in pre-render compute. It does not isolate a particular shader or driver scheduling cause. Stage timestamp intervals overlap and must not be summed. No GPU progress was lost, no call remained entered, no stop deadline was reached, and the spike did not repeat in the 2,064 held-waypoint observations. The highest held non-capture GPU interval was **0.40568 ms**. The preserved transition witness is available for Project Control review; no synchronization change was made to suppress it.

## Observer, recovery and preservation

CPU submit max was **0.1435 ms**, present max **0.5537 ms**, and device-idle max **2.8607 ms**. Observer loop max was **31.5541 ms** and journal flush max **5.2549 ms**. There was no Vulkan/device error, incomplete capture, ownership failure, membership mismatch, diagnostic loss or unresolved phase. Read-only Windows System/Application checks found no critical/error/warning event in **23:32:45–23:33:48 UTC**. This is not a diagnosis of the original blackout.

Background hash/write wall time (14 retained observations) was median **8.337 ms**, P95/P99/max **9.661 ms**. Background validation (all 20) was median **8.672 ms**, P95 **14.1764 ms**, P99/max **46.525 ms** at the first capture. Writer CPU accounting is quantized to 15.625 ms: median 0 and P95/P99/max 15.625 ms do not mean zero CPU work. These workers remained off the render and heartbeat paths.

No diagnostic/recovery source changed, so the previously accepted Debug/Release and recovery qualification applies to the exact sealed candidate; no replacement build or additional GPU run was made. `preservation.json` verifies source/build/terrain authority, prior evidence, Git HEAD/index/refs, ordinary executable and restored preferences. Production implementation writes: **0**. Driver/system/KSA changes: **0**. The local/global exposure gates are closed.

New evidence is bounded to **512 MiB**, with this permanent report bounded to **64 KiB**; exact output sizes and inventory hashes are retained in the preservation audit. Journals, two frozen files, source-at-live copies and derived analyses remain active investigation evidence pending authorized consolidation. No previous evidence was deleted.

Remaining limits: historical authority is missing; original blackout causation is unresolved; first-capture native timings and its raw trace aged out; capture timing tails and longer exposure remain unqualified; no diagnostics-disabled overhead baseline or returning-camera reuse test exists; high-factor near-ground/horizon/orbit/native-resolution incident exposure was not attempted. **STOP for Project Control. Manual acceptance remains ON HOLD.**
