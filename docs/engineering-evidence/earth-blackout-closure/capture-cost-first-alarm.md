# Capture cost ownership and first-alarm preservation — offline qualification

**PASS, bounded offline responsibilities A and B.** Subsequent Project Control authorization permits autonomous ground-visible, horizon and controlled-movement qualification after this offline gate is clean. The exact original native-resolution/borderless incident route remains a separate hard stop.

No application/GPU route was launched. The revised transport's hardware cost is **NOT QUALIFIED**. Further exposure depends on the final clean preflight under that authorization. The September 22 dual-monitor blackout and historical 411,877-triangle discrepancy remain **UNRESOLVED**; manual Player acceptance remains **ON HOLD**. No commit, tag, push, banking or milestone change.

Evidence: `build/earth-blackout-closure/capture-cost-first-alarm/`. The compact [witness](capture-cost-first-alarm-witness.json) identifies the current sources, measured historical facts, offline checks and preservation result. Existing ground/closer/distant reports remain historical and unchanged.

## A. What scaled, and why

The old capture records six `vkCmdCopyBuffer` calls, one region each. With V prepared vertices and T input triangles, its GPU copy volume is **64V + 28T + 188 bytes**. The physical, source-index and visibility copies cover the entire prepared topology, including culled triangles. The compacted copy also covers **12T bytes**, although only 12 × visible triangles are initialized/used. The writer subsequently trims that prefix, after the GPU has already copied the unused tail. Immutable topology indices are redundantly transferred every capture.

| Retained workload | Closer | Ground |
|---|---:|---:|
| Prepared vertices / triangles | 54,106 / 108,208 | 597,594 / 1,195,184 |
| Visible triangles | 4,633 | 15,679–15,747 |
| Old GPU bytes per capture | 6,492,796 | 71,711,356 |
| Capture-fence median | 3.5972 ms | 6.0994 ms |
| GPU transfer/barrier median | 0.24108 ms | 2.60084 ms |

The prepared population/copy volume increased **11.04×**; measured transfer/barrier time increased **10.79×**. Effective copied bytes divided by the measured interval are approximately **26.93 and 27.57 GB/s**. This locates the scaling in the topology-sized diagnostic transfer, rather than the much smaller visible population. These are effective rates for a scope containing copies **and barriers**, not a measurement of a particular hardware bus. The old instrumentation cannot separately attribute each copy or barrier's individual milliseconds.

The ground capture-fence increase is 2.5022 ms; transfer/barrier increase is 2.35976 ms. CPU scheduling, presentation overlap and rendering also affect the fence interval, so their difference is not assigned to an invented component cause. The separate accepted 6.0696 ms noncapture terrain-transition witness remains unchanged.

Ground frame 11,494, submission 11,557, generation 15, prepared/cull/raster pupil 77 has these exact source sizes and transfer ranges:

| Source | Buffer / old copied bytes | Destination offset in slot |
|---|---:|---:|
| Prepared physical vertices | 38,246,016 | 0 |
| Immutable triangle indices | 14,342,208 | 38,246,144 |
| Visibility flags | 4,780,736 | 52,588,544 |
| Compacted indices | 14,342,208 | 57,369,344 |
| Counters | 168 | 71,711,744 |
| Indirect draw | 20 | 71,712,000 |

Actual compacted output in that frame is **188,148 bytes**. The old copy transferred **14,154,060 unused tail bytes**. Frame 11,314 has the same topology sizes and 188,964 meaningful compacted bytes. The accounting artifact retains handles and birth/death serials, both snapshots' hashes, buffer capacities, exact offsets and timing samples. No missing historical camera/pupil state was synthesized.

The source range scales with **current prepared counts**, not dirty bytes and not spare allocation capacity. In these retained frames current counts equal their active capacities. Both destination allocations remain fixed at **92,274,688 bytes each**. Header/alignment holes explain why the ground file length, 71,716,116 bytes, is not the copied byte count. The compacted section was already shortened by the writer; file offsets still reserve its capacity range.

### Queue, synchronization and lifetime

Capture commands follow the final draw in the **existing graphics command buffer**, before command-buffer completion. They enter the same queue submission and existing frame fence. There is no additional queue submission, capture fence, queue-idle wait or readback-query WAIT flag. The renderer already gates its next update on that fence; the extra copies extend that serialized interval. This is not a separate writer queue preventing render-slot reuse.

There are two capture memory barriers: shader/host writes to transfer reads before copies, and transfer writes to host reads afterward. The corrected latter barrier also makes compute writes available for the host's compacted-prefix read. It does not change terrain scheduling or rendering dependencies. GPU timestamps bracket this transfer/barrier scope, so its measured cost remains visible.

The source work buffers require `HOST_VISIBLE | HOST_COHERENT` in `MappedBufferMemory.h`. Host coherence does not replace a dependency or completion fence: the shader-to-host barrier and the **existing completed-frame fence** precede the new host read, before publication or resource reuse. This follows the [Vulkan synchronization rules](https://docs.vulkan.org/spec/latest/chapters/synchronization.html) and [host-read example](https://docs.vulkan.org/guide/latest/synchronization_examples.html).

### Correction and evidence contract

- Transfer the actual GPU-produced compacted **draw prefix** into the owned readback slot at `CompleteFrozenCapture`, after the existing fence and before publication/reuse. Validate indirect bounds, source handles, generation, triangle count/capacity and destination layout first. No tail copy, CPU-generated replacement geometry, new GPU wait or new allocation/lock is used.
- Keep the prior GPU-read immutable indices in each readback slot. Reuse only when generation, buffer handle, topology hash/family, vertex/triangle counts and section layout all match. Changed owners invalidate the witness. Each slot needs its own first GPU index copy. A pupil change never skips physical or visibility readback.
- Preserve independently owned, self-contained snapshots: full prepared physical vertices, exact source indices, actual GPU flags, actual compacted membership, counters, indirect draw and frozen inputs. Record transport version, GPU bytes, host prefix bytes and the original index-readback frame. Validate those fields and the existing hash, pupil, membership and submission/completion joins. Legacy snapshots remain readable.
- Preserve the 2-slot ownership sequence, cadence and fail-closed busy behavior. The background writer continues to read only independent slot storage. It never borrows a live renderer buffer.

The new GPU volume is **64V + 16T + 188** for a slot needing indices (five copies), then **64V + 4T + 188** for a matching slot (four copies). Ground: **57,369,148**, then **43,026,940 GPU bytes**, plus the exact **188,148–188,964 host bytes** for the retained examples. The warmed GPU reduction is approximately **40%**. All 18 accepted topology sizes fit the pre-existing capture bound; the finest 712,106-vertex / 1,424,208-triangle topology needs 85,452,796 old bytes and 51,271,804 warmed GPU bytes. A fully visible compacted prefix there is 17,090,496 bytes; it is not clamped.

Physical readback remains full-sized because streaming/preparation can change it; a topology hash alone is insufficient authority to reuse it. Visibility remains GPU-produced for every captured frame. The writer still hashes/writes independent full index evidence. The two 88 MiB allocations and production terrain allocations are unchanged. Historical 875.8 MB allocation high-water and complete release are not new measurements.

**Limit:** mapped host reads may cost more than ordinary RAM copies. Debug/Release CPU fixtures prove bytes, ownership and zero allocations, not this GPU's memory-read latency. Future authorization must measure completion-side CPU cost as well as GPU transfer cost, full frame interval, synchronization and residency. No new live performance gain is claimed.

## B. First qualified alarm owns its evidence

The former 24.20-second interval included human supervision; the lower repeated-cost boundary was not an automatic gate. Increasing the rolling window would not fix that ownership problem.

The observer now qualifies repeated fence cost at **more than 6 ms in 3 of the last 4 samples**, separately for capture and noncapture waits. This explicit diagnostic cost threshold is not a renderer timeout, rendering clamp or moving baseline. Single transition spikes do not qualify. Existing 500 ms completed-operation, 1-second stall/heartbeat, startup and 2-second stop/termination deadlines remain unchanged.

Independent review found that the triggering journal write itself could flush before stop signalling. `WriteTrigger` now signals first; the regression invokes the actual production method and proves even a storage exception occurs after the signal. Both independent reviews found no remaining source-level blocker.

On the first qualified alarm, the observer:

1. Signals both existing stop channels and posts the normal close request **before flushing or serializing evidence**.
2. Changes the existing 64 MiB journal from rolling to immutable at the triggering serial. Subsequent records cannot overwrite it. A separate bounded 8 MiB append-only termination tail retains completion and cleanup; overflow is reported rather than wrapping.
3. Writes immutable first-alarm metadata containing the exact raw triggering record/hash, frame/submission/completion state, pending recording-call context, scheduled capture/generation/pupil/payload, cost-window begin/end records and resource ledger at the boundary.
4. Pins already published snapshot bytes by open file handles, so atomic filename replacement cannot change them. A separate worker preserves the exact triggering publication if it is still pending, using the existing 3-second evidence bound. At most three snapshots are pinned. Missing/unpublished evidence is explicitly reported incomplete, never invented or accepted as completed GPU work.
5. Joins pinned snapshots to the sealed journal plus termination tail and resource lifetimes. Repeated alarms cannot replace the first reason, record or window. Human handling can be delayed without delaying the automatic stop.

The producer ring and render-thread recording remain unchanged. The new disk work belongs to the observer/its worker. The first-alarm package and tail are separate ownership records, not an enlarged rolling window. Pin-worker finishing occurs after exposure has stopped; it does not extend a renderer/recovery deadline.

## Offline qualification

- Native diagnostic and observer **Debug / Release builds pass**. Renderer shaders are byte-identical to the accepted candidate; ordinary application executable, display settings, terrain/tessellation source and Git identity are preserved.
- Extracted production recording and completion code executes against CPU-owned buffers and local Vulkan substitutes, preserving the real renderer `Width`/`Height` scope. Both configurations pass **11,929 recording checks**, **149 one-shot markers**, all **50 outer-call / 23 scalar interruption classifications**, and **zero recording/completion allocations**. No Vulkan loader is linked to the probes.
- Ownership tests cover cold/warm index transfer plans, every cache-key invalidation, moved pupil, stale generation/handles/capacity, no pending wait, busy/wrapped slots, query-not-ready, zero/partial/full compacted prefixes, tail canaries and malformed/overflowing indirect layouts.
- First-alarm regression passes delayed handling beyond the journal window, repeated alarms, finite termination storage, isolated accepted waits, sustained cost, exact submission/generation identities, atomic snapshot replacement and pending publication. The sealed bytes and first reason remain unchanged.
- **44 final recovery commands pass** across both configurations, including startup phases, native admission, accepted tessellation/pupil/presentation regressions, stalls, device-error mock, producer overflow, orderly shutdown, writer/storage/backpressure failures, repeated capture cost, frozen corruption/membership/provenance checks and journal decoding. Two additional observer-loss mocks exit under the unchanged heartbeat policy.
- End-to-end CPU cost-alarm mocks signal stop **as recorded in the compact witness (Debug / Release)** after the producer's triggering record. These are two host samples, not latency percentiles or a hardware qualification. Both retain valid pinned captures, coherent completion, producer stop reason 1, zero journal loss and zero tail overflow.
- Read-only historical replay leaves the closer window clear. In the retained ground window it triggers at frame **10,594**, submission **10,657**, capture **59**, generation **15**, pupil **70**. This does **not** recover the first alarm whose raw evidence was lost.

Reproduce with the permanent `offline/build_frozen_ownership_probe.py --current-unmarked-baseline`, the two probe build commands, observer `--test-recording-calls`, `--test-first-alarm`, `--test-frozen`, and `mock-frozen-repeated-cost`. `offline/capture_transfer_accounting.py` derives the byte/timing report read-only. The task evidence directory contains exact invocations, build logs, extraction hashes, final recovery matrix and preservation audit. CPU mocks explicitly reject arbitrary application paths.

## Preservation, storage and next decision

Only diagnostic transport, its observer and associated tests/reporting changed. No production shaders, rendering quality, accepted tessellation behavior, terrain ownership, display defaults, deadlines, driver settings or KSA data changed. No new GPU submission/fence or writer dependency was introduced. The exact diagnostic copy plan and host visibility barrier changed intentionally as described above.

The initial rebuildable-output budget was 4 GiB. Pin-validation and the independent-review stop-order correction required fresh recovery matrices, retaining three sets of preallocated CPU journals; an explicit **8 GiB** revised bound covers this review, without deleting prior evidence. Approximately **7 GiB** of output is temporary/rebuildable, dominated by those CPU journals. Permanent report/witness remain below the declared 96 KiB bound; useful source/tests remain in their normal locations. `preservation.json` records exact final bytes. These outputs await Project Control acceptance and separately authorized consolidation, not banking.

**Current authorization:** proceed through clean ground-visible, horizon and controlled-movement gates; stop on the first genuine abnormality. Stop once for Project Control review before the exact original native-resolution incident route. This offline result does not itself qualify any new hardware stage. Remaining unknowns are actual revised CPU/GPU cost and timing tails, dynamic/horizon residency and synchronization behavior, the missing historical visibility authority, and the original blackout's causal boundary.
