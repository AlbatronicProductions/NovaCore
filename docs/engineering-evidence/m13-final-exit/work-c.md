# Work C — post-M13.5 narrow KSA responsibility review

## Decision

M13.5's terrain working-data placement is banked and is excluded from any new payoff. The applicable current KSA source does not establish another output-preserving 1.5 ms GPU opportunity.

A newly isolated **CPU** production-request inspection scope warrants a narrow memory-placement prototype. QueueProductionRequests scans all 8,192 mapped terrain-key slots twice. Its 100-frame warm conventional median is 1.9147 ms in Florida and 1.8862 ms in orbital, with zero requests, 126 completed uploads and zero pending uploads throughout both windows. The key allocation uses compatible uncached type 1, flags 6; compatible type 3 adds HOST_CACHED while retaining HOST_VISIBLE and HOST_COHERENT (decimal flags 14, hexadecimal 0xE). KSA supplies a useful request/completion ownership comparison, but no equivalent cached Vulkan key-buffer selector to transplant. This is isolated CPU cost; output-preserving recovery is still subject to the prototype. It must not be subtracted from GPU time.

This Work C review supports preferring coherent host-cached memory for this one CPU-read key allocation while preserving the existing loops and their entire request/readback contract. M13.5's GPU working-buffer roles remain unchanged. No new host mirror or completed-state shortcut is required. This review does not independently declare a continuation winner or M13 closure; measured prototype recovery, exact behavior and end-to-end pacing remain decisive.

## Current reference

At the initial read, HEAD was **d4baab6940a57a46e478b98e36f5e45d1c4b558f** and git status was empty. The installed KSA DLL remains:

- File version: 2026.9.7.5402.
- Product version: 2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400.
- SHA-256: a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f.

All 16 previously reviewed KSA binary/decompiled-source/shader fingerprints match the immediately preceding [Work C reference](E:/NovaCore/docs/engineering-evidence/m13-final-whole-frame-causality/work-c.md). NovaCore's material shaders, current/incoming NCSM1 cull and regional-residency implementation also match that reference; NovaCoreNative.cpp and RegionalPhysicalPreparation.inl changed for banked M13.5. MappedBufferMemory.h was additionally inspected. The JSON records current identities and the extra narrow TextureStreamRequest source fingerprint.

This reused cached decompilation with the verified unchanged DLL/source chain. No new decompilation, broad terrain research, Discord review, KSA execution, build, GPU run or production edit was needed.

## Required, completed and unresolved boundaries

| Responsibility | Current KSA equivalent | Current NovaCore boundary | Judgment |
|---|---|---|---|
| Positive material response | Fragment stage resolves four candidate materials, samples displacement to reweight them, then samples the strongest three for diffuse/normal/AO-roughness-metallic response. Shared projection/derivative inputs and distance-scale offsets remain live. | Accepted FP64 body-fixed broad/meso/micro fields contribute to albedo, roughness, AO and the shading normal. Zero-contribution guards and corner-prefix sharing are already banked. | ADAPT separation of geographic identity and filtered detail. INTENTIONALLY DIFFER on the exact field/precision contract. KSA textures or fractional offsets are not an exact replacement for NovaCore's hash cell identity. |
| Geometric detail/tessellation | TCS determines edge factors; TES samples authored displacement and changes geometry near the camera. | Prepared physical interpolation and future material-displacement/silhouette capacity remain separate from fragment shading. | ADOPT retaining hardware tessellation for geometric responsibility. Removing factors/detail is not an admissible exact-output saving. |
| Current visibility/index construction | Per-frame cull encloses each prepared triangle with material-displacement tolerance and writes surviving indices directly. | Current camera visibility changes even while physical data is retained. NovaCore's conservative envelope and precise reject semantics remain its authority. | ADAPT camera-owned visibility. No KSA proof for a persistent camera-list cache or replacing NovaCore's bound. A source-level zero-support opportunity does not prove worthwhile payoff. |
| Complete incoming physical readiness | Same-frame prepare/modifier/normal/cull work precedes rendering; texture exposure follows upload completion. | Complete regional physical dependency footprint, valid prepared data and deterministic published current/incoming owner must be preserved. | INTENTIONALLY DIFFER on exact canonical generation-4 publication. KSA does not supply NovaCore's exact-reuse map or a substitute for its regional readiness validator. |
| GPU working-data placement | Persistent device-local generated geometry and visibility buffers, with separate mapped control/staging responsibilities. | M13.5 already prefers compatible device-local memory for terrain working data while preserving host visibility/coherence and capacity fallback. | Completed banked convergence. Do not count this saving again or broaden the memory policy to unrelated CPU buffers. |
| CPU request inspection/completion | CPU-owned map/request state, camera-derived mip demand and polling actual in-flight upload batches. | Two fixed-capacity shared-key scans remain intact; prefer compatible coherent host-cached memory for their CPU-read allocation. | ADAPT role-specific placement. INTENTIONALLY DIFFER on NovaCore's GPU-shared key/readback contract. No KSA HOST_CACHED selector implementation is claimed. Baseline CPU cost is isolated; recovery remains to be measured. |

The preceding report contains the full material/visibility/lifecycle source reconstruction. It is not repeated here and its M13.4-era timings are not used as the fresh M13.5 exit envelope.

## Narrow CPU request/polling comparison

### KSA's production owner

CelestialTextureStreamer.Tick visits retained CPU Entry objects and their Height/Diffuse/Normal StreamedMap objects. AddCandidate reads each map's InFlight request status, computes needed texels from camera/projected size, selects a target mip and passes the candidate to ApplyMap. It does not discover terrain demand by reading a GPU-written per-slot key array. [K1]

TextureStreamRequest contains the target image, source/level ranges, priority, completion callback, status and uploaded-byte accounting. StreamingUploader.Enqueue places that request into a CPU priority queue and signals its worker. Poll iterates the actual in-flight Batch list, checks each completion gate with WaitFor(0), and only after completion invokes request callbacks and disposes the batch. Program calls Poll in the frame lifecycle. This is completion polling over outstanding work, not a scan across unused GPU residency capacity. [K2–K3]

The culling result also stays on its GPU consumer path: PlanetFrustumCulling resets/updates indirect draw arguments and generated indices, then barriers expose them to index/indirect draw. The reviewed path does not read every generated visibility/index entry back to the CPU to decide texture demand. [K4]

This is a bounded negative finding about the applicable celestial streaming/culling owners. It is not a claim that all KSA subsystems avoid readback or that all polling is free. No KSA CPU timings were measured. Its completion and coarse-mip fallback policy does not replace NovaCore's canonical physical publication contract.

### NovaCore's exact difference

QueueProductionRequests holds the production I/O mutex and scans TerrainCacheCapacity twice. Each slot has 12 uint32 words: the full mapped key array is 393,216 bytes. The first scan updates layer last-use information. The second discovers unresolved keys, may publish already-resident root samples, rejects unavailable records, queues requests and changes key states. PrepareProductionUploads calls it before consuming ready uploads; the frame path calls PrepareProductionUploads. [N1]

This mixes CPU request scheduling/bookkeeping with inspection of GPU-shared state. A fully uploaded pack or a warm frame with no uploads is not alone proof that discovery can stop: the current source still defines producers, state transitions, generic paths, pending worker reads and teardown. The lead/Work B ownership audit must prove which key fields remain invariant under the exact complete production transaction and which fields the GPU continues to modify.

The current proposed correction does not skip either scan. Prefer a compatible HOST_VISIBLE | HOST_COHERENT | HOST_CACHED type only for this CPU-read key buffer, preserving the existing compatible host fallback if that preference is unavailable or capacity/map constraints require it. Keep the same buffer contents, key producers, CPU writes, request discovery, upload completion, in-flight worker progress, fences and lifetime. This avoids creating a second host mirror or requiring a new invariant about whether the full production hierarchy can ever change.

Keep regional physical residency and facility/physical publication unchanged. The production texture request table is not permission to change regional H preparation. The banked TerrainGpuWorkingSet policy is a separate role and must remain unchanged.

KSA supports assigning request/completion state to its actual CPU owner while keeping GPU-generated data with GPU consumers. **INTENTIONALLY DIFFER:** NovaCore retains its GPU-shared key/readback contract, whereas the inspected KSA streamer uses CPU-owned request objects. **ADAPT:** choose resource placement for the actual producer/consumer role. No exact KSA coherent host-cached selection implementation was found in these applicable owners, and none is claimed. Vulkan HOST_CACHED does not mean maintaining a duplicate application-level key cache. Whether this local preference recovers 1.5 ms without moving cost remains an empirical NovaCore question.

## CPU and GPU payoff must remain separate

The original approximately 1.92–1.96 ms enclosing CPU scope has now been narrowed to QueueProductionRequests in poll-florida.json and poll-orbital.json. Florida frames 182–281 and orbital frames 82–181 contain 100 observations each. Conventional middle-pair medians are 1.9147 and 1.8862 ms; the Florida nearest-rank median is 1.9139 ms. Both windows report requests=0, uploads=126, pending=0 and records=126. This is measured queue CPU cost, not yet a measured saving and not GPU image-transfer time. Compare the host-cached prototype with identical work and end-to-end display-frame behavior before declaring it avoidable.

If a bounded correction removes at least 1.5 ms of recurring CPU work and improves ordinary pacing without moving the cost or changing output, it can be an economically meaningful responsibility. GPU medians/P95/P99 must still be reported unchanged where that is what the evidence shows. A CPU improvement cannot turn an 8.88 ms GPU frame into a sub-8.33 ms GPU frame arithmetically.

Positive material sensitivity remains output-changing work; KSA fidelity does not make it removable. Current visibility and regional preparation contain required and potentially avoidable portions, but the KSA reference alone supplies neither an exact replacement nor new measured payoff. Do not combine several unrelated sub-millisecond items to manufacture a continuation winner.

## Source anchors and reproduction

Current hashes and absolute paths are in work-c.json.

- K1: E:/NovaCore/build/ksa-residency-reference/source/KSA.Rendering.CelestialTextureStreamer.cs:415–503 — CPU map entries, request status and camera-derived demand.
- K2: E:/NovaCore/build/ksa-residency-reference/source/KSA.Rendering.TextureStreamRequest.cs:7–26 — explicit CPU request object.
- K3: E:/NovaCore/build/ksa-residency-reference/source/KSA.Rendering.StreamingUploader.cs:100–146; E:/NovaCore/build/ksa-residency-reference/source/KSA.Program.cs:2239 — enqueue, in-flight completion polling and its frame caller.
- K4: E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/PlanetFrustumCulling.cs:117–182 — GPU cull output and index/indirect consumers.
- N1: E:/NovaCore/native/NovaCore.Native/NovaCoreNative.cpp:42, 1124–1135, 1590, 2405 — capacity, two scans, upload caller and mapped key allocation.
- N2: E:/NovaCore/native/NovaCore.Native/MappedBufferMemory.h:9–35 — already banked coherent mapping/locality preference/fallback.
- N3: E:/NovaCore/native/NovaCore.Native/shaders/production_terrain_material.glsl:25–46, 220–258; production_nested_scale_mesh_cull.comp:124–149 — retained exact material contribution and current cull/diagnostic responsibility.

Recheck hashes directly from the stored paths with Python -B or Get-FileHash. Read retained JSON/Markdown as UTF-8. No source copying, compiler recapture, raw evidence generation or cache cleanup is necessary for this comparison. Only work-c.md and work-c.json were written by this worker.
