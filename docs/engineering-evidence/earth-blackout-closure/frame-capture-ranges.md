# Changing-frame capture transport — V4 qualification

Project Control accepted ground-visible V3 and the topology-owned GPU-read witness. V3 horizon stopped on repeated changing-frame capture cost. This revision preserves the original rendering, topology/tessellation policy, one-second capture cadence, deadlines, and strict >6 ms / three-of-four fence gate. The current authorization is offline qualification, then horizon, then controlled movement if horizon passes. The original 3440×1440 borderless incident route still requires the final Project Control boundary. If heavyweight packing still fails, the next responsibility is a lighter native-test evidence design, not repeated transfer micro-optimization.

Original dual-monitor blackout and historical 411,877-triangle discrepancy remain **UNRESOLVED**. Manual acceptance remains **ON HOLD**. No banking or milestone promotion.

## Decomposition of the retained horizon trigger

Capture 46 / frame 8,238 / submission 8,301 used prepared generation 18, pupil 233, 712,106 vertices and 1,424,208 triangles. Its actual draw contained 602,404 triangles. All ranges start at source byte zero. Source allocation capacities equaled selected counts; shrinking physical/visibility allocation capacity would remove no further bytes.

| Resource / owner | V3 transfer | Authority needed | V4 changing-frame transport |
| --- | ---: | --- | ---: |
| Prepared physical output, handle 878509790593823; current preparation/pupil owner | 45,574,784 B | Every original 64-byte vertex, including double position/height, normal/validity and reserved bytes. Contents change with preparation even when topology generation is unchanged. | 34,181,152 B: raw first 48 bytes per vertex plus raw 16-byte exceptions for vertices 0–3; GPU proves every remaining reserved word is exactly zero. |
| Visibility output, handle 880708813849377; current cull owner | 5,696,832 B | All 1,424,208 raw uint32 flags, including absence and any invalid value. A visible count or CPU cull reconstruction cannot replace this. | 5,696,832 B unchanged. |
| Compaction output, handle 882907837104931; current compaction owner | 17,090,496 B | Actual GPU-written draw prefix, preserving exact index bits and order. The unwritten capacity tail is not consumed or persisted. | 7,228,848 B, selected by the same GPU indirect count; no prior-frame/CPU count. |
| Counters, handle 887305883616039; reset/cull/compact/TCS owner | 168 B | All 42 raw counters, including diagnostics and tessellation observations. | 168 B unchanged. |
| Indexed indirect command, handle 885106860360485; compaction owner | 20 B | All five raw words; exact count, instance count, offsets and validity. | 20 B unchanged. |
| Packing completion metadata; independent capture-slot owner | — | GPU error/status, vertex/triangle/count, frame/capture identity, actual output/source byte accounting, zero-word count and offending padding witness. | 64 B. |
| **Total changing evidence written** | **68,362,300 B** | | **47,107,084 B** |

The reduction is 21,255,216 bytes (31.09%). A separate 64-byte GPU fill initializes metadata before packing. Unique source authority occupies 58,500,652 bytes at this trigger: physical padding is read to prove its exact value; only the unwritten compacted tail is not read. These are unique payload sizes, **not measured bus traffic**: each shader lane also reads the indirect command, and clear/atomic/cache transactions add work. Logical file physical records remain the original full 64 bytes. No float conversion, normal calculation or CPU physical reconstruction substitutes for GPU bytes.

Immutable index evidence is already owned by topology witness 18: 17,090,496 GPU-read bytes from source handle 874111744082715, incarnation/create serial 259737, copied at frame 4,499 / submission 4,562 and completed there. Subsequent snapshots pin and reuse that exact witness. This revision does not duplicate or alter its provenance, readiness, cache or retirement rules. The inputs in the small frozen header remain the authoritative CPU→GPU submissions; they are not GPU outputs or replacements for physical geometry.

## Exactness and lifecycle

`FrozenPackingNative.inl` creates one diagnostic pipeline, two descriptor sets and two 64-byte mapped metadata buffers at diagnostic startup. These are independent of the production renderer descriptors. Existing 88 MiB capture slots gain storage-buffer usage; their ownership and capacity remain unchanged. A successfully reserved slot alone may have its descriptors updated. No additional queue, submission, fence wait, query WAIT flag, runtime lock or render-thread allocation is added by our recording/completion code. Driver-internal costs remain subject to hardware measurement.

The existing command buffer, after production drawing, resets the slot metadata using a 64-byte fill. One producer barrier makes production shader/host writes and the metadata fill visible to the diagnostic compute dispatch. A fixed 256×256 grid uses bounded stride loops over exact active counts. Raw integer loads/stores preserve all floating-point bit patterns. A second barrier makes diagnostic writes host-readable before the existing frame fence completes. The full packing/fill/barrier interval is inside the capture GPU timer and full-frame timer. Ordinary-frame scheduling is unchanged.

The GPU validates all five indirect words before accessing any compacted prefix. A malformed count invalidates the capture; it is never clamped into a valid one. Every visibility uint32 is copied unchanged. For physical vertices beyond the first four, any nonzero reserved word invalidates the capture and records an offending vertex, component and raw value. The first four reserved records are always copied, preserving the production test-only oracle/catalog witnesses. Other padding is not merely assumed zero.

After the existing frame fence, completion validates GPU metadata against frozen CPU frame/capture/layout identity, copied indirect data, live resource ownership and actual byte formula. It emits a fixed-size metadata completion record. Any mismatch fails closed. Successful raw GPU metadata is retained in the frozen header's otherwise-unused final 64 bytes. The observer checks this completion before accepting byte-count and capture-completion records. GPU-written actual bytes are distinct from the planned capacity recorded before submission.

The background writer exclusively owns the completed slot. It saves the first-four exception bytes, expands packed physical records backwards in place, and restores only GPU-proven zero words. It then hashes and writes the same complete 64-byte physical records, raw visibility, actual compacted prefix and GPU-read topology indices. Expansion never touches live terrain mappings or the render thread. The existing topology pin, flush/publication checks, reader overlap and release rules remain in force.

Version 4 explicitly marks this lossless transport. Snapshot/journal joining normalizes only post-completion byte count, packing-complete flag and the GPU completion footer for this version, then validates those fields separately. Older transports retain their existing validation. Dropping physical fields, reconstructing compaction from visibility, using only a topology key for mutable physical bytes, CPU reads of live terrain mappings, extra readback waits and capture skipping are excluded.

## Qualification evidence

**Offline PASS.** Debug and Release each pass 11,929 production-scope recording checks, 810,969 ownership checks, 25 topology lifetime cases, 116 actual writer/publication cases, 643 exact shared-shader packing checks, 76 topology/wire/proof cases, and 76 first-alarm cases. All 50 outer-call and 23 scalar interruption classifications pass. Recording/completion allocation counts remain zero. The 46-command recovery matrix and both observer-loss cases pass; first-alarm sealing and late capture publication remain qualified. Both independent read-only reviews found no correctness blocker. Existing production SPIR-V remains byte-identical; the new diagnostic shader passes offline SPIR-V validation.

Evidence is retained under `build/earth-blackout-closure/frame-capture-ranges`. Accepted V3 evidence is sealed in `autonomous-live-qualification-v3/preservation.json` and copied by hash into the V4 entry manifest. CPU mocks prove the packing algorithm, layout, ownership and failure rules; actual GPU behavior and cost remain unqualified until the authorized horizon stage.
