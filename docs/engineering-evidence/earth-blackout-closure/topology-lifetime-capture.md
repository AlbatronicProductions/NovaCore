# Topology-lifetime GPU capture transport — offline PASS; ground PASS; horizon REVISE

The topology-owned GPU witness is implemented and qualified. Ground-visible V3 passes. **Horizon does not pass:** the unchanged capture-fence gate stopped the held waypoint at frame 8,238 / submission 8,301 after three of four waits exceeded 6 ms. Those captures reused a completed immutable topology witness. The remaining blocker is changing per-frame evidence transport, not a repeated cold topology copy.

No relaunch followed the alarm. No orbit or original native-resolution/borderless route was launched. Original dual-monitor blackout and historical 411,877-triangle discrepancy remain **UNRESOLVED**. Manual Player acceptance remains **ON HOLD**. No commit, tag, push, banking, milestone promotion, driver/system change, KSA change, or rendering-quality reduction.

## Authority and ownership contract

Production sources are `FrozenTopologyNative.inl`, `FrozenCapture.h`, `FrozenCaptureNative.inl`, and the diagnostic hooks in `NovaCoreNative.cpp` / `CausalNative.inl`. Capture transport version 3 retains the previous file schema and adds the sixteenth section, type 109, containing a 176-byte topology provenance capsule.

- Each immutable source index buffer is identified by its Vulkan handle **and actual buffer-create event serial**. Family, topology hash, vertex/triangle counts, source section type/offset/stride/bytes, and destination handle/incarnation are explicit. Cache reuse gets a new monotonic witness identity. A reused native handle cannot reuse an earlier lifetime's proof.
- Topology activation allocates exact-sized independent host-readable diagnostic storage. The cache has four fixed metadata entries; each payload is bounded by the existing 88 MiB capture capacity. Metadata, recording and completion operations use no new allocation or lock; buffer creation is explicitly a lifecycle allocation. Oversize, unavailable or exhausted diagnostic ownership fails closed; it does not clamp terrain or skip a due capture.
- Startup backfills the initial topology. Runtime acquisition occurs only when no incoming terrain owner exists. At most one new witness can be allocated at a production recording boundary; a permanent fail-closed assertion protects this invariant. Swapchain recreation preserves incoming ownership and does not rerun frozen setup.
- The GPU copies source index bytes once in the existing graphics command buffer, after the full-frame GPU timestamp starts and before terrain preparation. It uses HOST_WRITE→TRANSFER_READ and TRANSFER_WRITE→HOST_READ barriers. No additional queue, submission, wait, fence, query WAIT flag, retry loop, or production publication dependency is introduced.
- `Allocated → Recorded → Submitted → Proven` requires the exact successful command-buffer submission and existing completed-frame fence. Witness completion precedes terrain publication for both physical-preparation modes. Current-frame generation and immutable source lifetime remain separate authorities.
- A due capture must pin a matching Proven witness. The writer reads the independent GPU-read index storage directly while hashing and writing a self-contained snapshot. It releases its pin after all reads, including storage failure. It never reads CPU candidate indices or live terrain mappings. Current/incoming owners, recorded/submitted copies and writer pins cannot be evicted.
- Retirement requires completed, unpinned, noncurrent, nonincoming ownership. Shutdown uses existing GPU idle, drains the writer, reports the original storage error before cleanup, then releases pins and buffers. All observed witness owners retired.
- The observer retains a bounded 256-entry session proof ledger, at most four active witnesses: checksummed Create, Record, Submit Begin, Submit End, GPU Progress, Complete and Retire records. It validates command/fence/sequence and both resource lifetimes. Captures embed the completed capsule and real GPU-read bytes, so their origin remains reconstructable after the rolling journal expires.
- Witness frames retain CPU recording/completion, GPU transfer, full GPU frame and fence measurements. Their separate recurrence cohort uses the unchanged 6 ms / three-of-four fence and 1 ms completion limits; ordinary and capture gates remain active. Raw samples and the first trigger are copied into immutable alarm evidence. Sparse witness work cannot be hidden by intervening cheap ordinary frames.

The four-entry bound follows from one current owner plus at most two writer-slot pins, leaving one incoming slot. Both live runs needed only two concurrent entries. Horizon witness payload high-water was **33,804,288 bytes**; this is payload, not total Vulkan allocation/residency.

## Offline qualification

Debug and Release builds pass. Each configuration passes:

| Qualification | Result |
| --- | ---: |
| Production-scope recording checks | 11,929 |
| Capture ownership/prefix checks | 810,969 |
| Exact extracted topology lifecycle checks | 25 |
| Topology observer/proof/cost checks | 58 |
| Writer publication/pinning checks, including transport 3 | 82 |
| First-alarm delayed handling/repeated alarms | 53 |
| Outer/scalar interrupted-call classifications | 50 / 23 |

The native lifecycle tests execute the extracted production functions with local Vulkan substitutes and no Vulkan loader. They cover source-handle reincarnation, unchanged topology across generation changes, pending/failed copies, no due-capture skip, current/incoming/writer pins, full cache, malformed acquisition followed by cleanup, exact-copy arguments, existing-fence readiness, query failure, shutdown and balanced allocations. Recording/completion allocation counts are zero; 32-bit pin atomics are statically lock-free. Publication tests deliberately hold the old file open through writer completion and storage rejection, including topology-pin release.

The observer rejects missing/wrong command submission, source/destination lifetime changes, forged or corrupted proof records, and forged provenance fields. Sparse expensive witness frames retain the exact four-sample alarm window even when ordinary frames intervene. A reusable-buffer regression proves later records cannot overwrite the latched trigger.

Final recovery qualification: **46 commands**, plus **two observer-loss checks**, pass. Both legacy and final matrices are preserved; the reviewed trigger-alias correction required a final rerun. Output remains within the explicit 6 GiB offline evidence budget. All **130 shader binaries** are byte-identical to the accepted baseline. Accepted tessellation and moving-pupil regressions remain intact. Cadence remains 1,000 ms; startup/stall/shutdown deadlines are unchanged.

The two independent read-only reviews found and verified the corrections for validation-before-retirement, first-error reporting before cleanup, full submit/progress proof, immutable first-trigger bytes, and the single-activation invariant. Root remained the sole production writer.

Offline seals: `build/earth-blackout-closure/topology-lifetime-capture/preservation.json` and `current-task-manifest.json`. Historical sources/builds/evidence were hash-verified. HEAD, index, branches, tags and remotes are unchanged. Differences confined to `refs/codex/turn-diffs/` are recorded explicitly in `ref-reconciliation.json`; the report does not claim every internal ref remained unchanged.

## Ground-visible V3 — PASS

One 90-second stage, approved 960×540 windowed preset, direct supervised Start handoff with no display popup. **15,634 submitted / 15,634 completed**, **11,854 target frames**, **90 validated publications**. Both retained captures independently pass exact visible/compacted membership (16,865 and 16,863 triangles), frame/submission/completion and source-incarnation joins.

All 15 topology witnesses validate. The retained captures reuse index copy **frame 3,783 / submission 3,846**. No recurrence alarm, Vulkan fault, incomplete capture, ownership fault or sampled budget pressure occurred. Allocation high-water **903,343,608 bytes**; **250 allocations / 250 releases**; zero final tracked bytes. Normal shutdown and exact settings restoration pass.

An isolated 6.0794 ms capture wait at frame 4,484 survives in supervision metadata, not retained raw records. An isolated 8.5032 ms ordinary wait at frame 13,535 has 94 checksummed raw records retained separately. Neither met the unchanged recurrence gate. These witnesses are not erased or described as tail stability.

All timings below are milliseconds. P95/P99 use nearest rank; capture tails remain provisional with 13 retained samples. Topology samples span the approach and have varied workloads.

| Scope | Samples | Median | P95 | P99 | Maximum |
| --- | ---: | ---: | ---: | ---: | ---: |
| Capture CPU recording | 13 | 0.2326 | 0.2808 | 0.2808 | 0.2808 |
| Capture CPU completion | 13 | 0.0158 | 0.0752 | 0.0752 | 0.0752 |
| Capture GPU transfer + barriers | 13 | 2.0549 | 2.1632 | 2.1632 | 2.1632 |
| Full GPU capture frame | 13 | 5.1218 | 5.6753 | 5.6753 | 5.6753 |
| Capture submission-fence wait | 13 | 5.3876 | 5.9200 | 5.9200 | 5.9200 |
| Ordinary submission-fence wait | 2231 | 3.3184 | 3.9035 | 4.0893 | 8.5032 |
| Topology CPU recording | 15 | 0.0237 | 0.0354 | 0.0354 | 0.0354 |
| Topology CPU completion | 15 | 0.0119 | 0.0210 | 0.0210 | 0.0210 |
| Topology GPU transfer + barriers | 15 | 0.2242 | 0.5254 | 0.5254 | 0.5254 |
| Topology frame fence | 15 | 1.8098 | 3.2602 | 3.2602 | 3.2602 |
| Full GPU topology-copy frame | 14 | 1.8593 | 3.4686 | 3.4686 | 3.4686 |

## Horizon V3 — automatic STOP / REVISE

Horizon reached its held waypoint, unlike the earlier V2 cold-transfer stop. The run completed **8,240 submitted / 8,240 completed frames**, including **2,523 target frames**, and **46 validated publications**. Generation 18 has 712,106 vertices and 1,424,208 triangles. The final retained visible population is 602,404 triangles. Minimum observed altitude was 70.5477 m; this is the prescribed diagnostic horizon route, not orbit or the original incident route.

The unchanged gate fired on this exact window:

| Capture | Frame | Submission | Pupil | Fence wait (ms) |
| --- | ---: | ---: | ---: | ---: |
| 43 | 7,699 | 7,762 | 202 | 6.4699 |
| 44 | 7,878 | 7,941 | 213 | 6.0059 |
| 45 | 8,058 | 8,121 | 222 | 5.2636 |
| 46 | 8,238 | 8,301 | 233 | 6.1848 |

All four use generation 18 and reuse witness 18: the original 17,090,496-byte GPU index copy completed at **frame 4,499 / submission 4,562**. Its source birth serial is 259,737; destination birth serial 259,742; record/completion serials 259,772 / 259,814. Thus **no cold index copy occurs in the qualifying window**. All 18 topology-copy frames remain under the fence recurrence limit, maximum **3.8853 ms**; index transfer/barrier maximum **0.61964 ms**.

| Scope | Samples | Median | P95 | P99 | Maximum |
| --- | ---: | ---: | ---: | ---: | ---: |
| Capture CPU recording | 13 | 0.2287 | 0.2914 | 0.2914 | 0.2914 |
| Capture CPU completion | 13 | 0.0162 | 0.0265 | 0.0265 | 0.0265 |
| Capture GPU transfer + barriers | 13 | 2.4458 | 2.4521 | 2.4521 | 2.4521 |
| Full GPU capture frame | 13 | 5.8951 | 6.2034 | 6.2034 | 6.2034 |
| Capture submission-fence wait | 13 | 5.9664 | 6.4699 | 6.4699 | 6.4699 |
| Ordinary submission-fence wait | 2208 | 3.4411 | 3.8858 | 4.3426 | 4.6617 |
| Topology CPU recording | 18 | 0.0255 | 0.0426 | 0.0426 | 0.0426 |
| Topology CPU completion | 18 | 0.0128 | 0.0218 | 0.0218 | 0.0218 |
| Topology GPU transfer + barriers | 18 | 0.2661 | 0.6196 | 0.6196 | 0.6196 |
| Topology frame fence | 18 | 2.3054 | 3.8853 | 3.8853 | 3.8853 |
| Full GPU topology-copy frame | 17 | 2.4103 | 4.0931 | 4.0931 | 4.0931 |

Topology full-frame timing is unavailable for the initial copy; it is reported as unmeasured, not zero. Creation/allocation CPU cost is not separately isolated by these copy-recording/completion scopes. Existing frame progress/operation gates stayed active throughout allocation and rendering.

Allocation high-water **1,024,421,272 bytes**; **277 allocations / 277 releases**, zero final tracked bytes. No sampled budget pressure, GPU progress loss, Vulkan/device error, command-recording non-return, evidence corruption or publication failure was observed. Application exit was 0, native result 0, without forced termination.

## Exact first-alarm closure

Trigger serial **480,628**, frame **8,238**, submission **8,301** is retained byte-for-byte; SHA-256 `286595c9f806e9f20858f970aeb5081ac7774b28b46f3d9a8a240abc56cd72fe`. Primary plus shutdown tail is contiguous **349,557–481,074**, **131,518 records**, including **446 tail records**. No gap, corruption, overrun, producer contention drop or tail overflow.

- Trigger record → automatic stop signal: **6.6052 ms**.
- Stop signal → completed native cleanup: **236.3665 ms**.
- Pinned captures **44, 45 and 46** were published and independently verified. Exact memberships: **593,872 / 600,619 / 602,404** triangles. Capture 46 completion and submission 8,301 are retained in the tail.
- The full topology origin proof survives outside the journal; an independent reviewer verified all checksums, command/submission/progress ordering and resource lifetimes.
- Saved settings were restored byte-for-byte after processes exited. There was no relaunch after the alarm.

This closes the earlier evidence-aging/publication gap for this recurrence. It does not close the original blackout.

## Remaining cost owner and defensible next experiment

Transport 3 removes the 12T immutable-index copy from each frame capture. It retains five GPU copies and two barriers in the existing graphics submission:

| Trigger capture data | GPU bytes copied | Source size |
| --- | ---: | ---: |
| Prepared physical vertices | 45,574,784 | 45,574,784 |
| Visibility flags | 5,696,832 | 5,696,832 |
| Compacted-index capacity | 17,090,496 | 17,090,496 |
| Counters | 168 | 168 |
| Indirect draw | 20 | 20 |
| **Total** | **68,362,300** | |

The readback slot remains 92,274,688 bytes (88 MiB). Each saved file includes the separate GPU-read topology indices. Only the completed compacted prefix is persisted: trigger prefix **7,228,848 bytes**, leaving **9,861,648 copied capacity bytes** outside the saved compacted section. Copy cost scales with selected geometry population, not changed-byte count; source capacities equal selected counts here. The existing frame fence therefore includes these transfers/barriers alongside renderer work.

**Observed:** physical, visibility and compacted section hashes differ across pupils 213/222/233 while topology indices and provenance remain identical. Reusing physical bytes solely because generation/topology is unchanged would violate the evidence contract. **Not established:** the cost split between each individual mutable copy and barrier, or a cheaper exact GPU transport for the compacted prefix. No CPU mapped-memory substitution was reintroduced.

The next defensible step is offline analysis/qualification of the remaining mutable prepared-geometry and visibility/compaction evidence transport, preserving actual GPU provenance and exact membership. Prove any lifecycle or bounded GPU representation change before new exposure; do not relax the gate or reduce terrain. That is a separate responsibility from the completed topology-lifetime revision. No further production change or live attempt is made here.

## Evidence and unresolved scope

- Offline: `build/earth-blackout-closure/topology-lifetime-capture/`.
- Live sealed candidate, startup/display/settings checks, measurements: `build/earth-blackout-closure/autonomous-live-qualification-v3/`.
- Decisions: `ground-boundary.json`, `horizon-boundary.json`, `horizon-classification.json`.
- Exact stop evidence: `live-02-horizon/first-alarm/`, `termination-tail.bin`, `topology-witnesses.json`, `resource-lifetimes.json`.
- Independent reviews verify retained payloads/proofs; total 90/46 capture validation counts are the observer's live retained totals.

**Remaining unknowns:** how to bound mutable capture overhead at held horizon without weakening proof; exact per-copy/barrier cost attribution; controlled movement/orbit qualification; original native-resolution/borderless route; original blackout cause; historical 411,877-triangle population discrepancy whose old pupil/prepared authority was not retained. No fresh exact evidence resolves that historical session retrospectively.
