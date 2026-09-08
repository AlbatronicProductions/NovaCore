# Work C — KSA replacement and residency ownership

## Result and source identity

KSA supports persistent topology and persistent frame-slot GPU allocations, completion-gated texture exposure, and explicit retirement. Its selected terrain path prepares geometry and reconstructs camera visibility every applicable rendered frame. It does not provide evidence for a NovaCore-style asynchronous current/incoming physical generation or a stable-geometry visibility cache.

The installed local KSA.dll still reports file version 2026.9.7.5402 and product revision 487c3f340de24c6a81037120b6d1129c045c5400. Its freshly computed SHA-256 is a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f. Eleven reviewed binary/source/shader identities match the previous verified package. PlanetMeshLibrary.cs and FreeImagePool.cs are additional fingerprinted files in the same cached decompilation tree, not newly decompiled or prior-manifest-verified files. Full paths, byte sizes and hashes are in work-c.json.

This is a source-only responsibility comparison. No KSA or NovaCore GPU run, build, source modification, fresh decompilation or Discord research was performed. No timing, overlap percentage, bounded publication latency or spike-avoidance guarantee is inferred from KSA source.

## What persists and what repeats

1. PlanetMeshLibrary loads each referenced mesh collection. PlanetMeshCollection.Generate builds its levels with Parallel.ForAsync during loading and waits while servicing loading frames. CubeMesh.Generate constructs positions, triangle indices, adjacency and average-neighbor data. Mesh buffers remain attached to the collection until disposal. Moving the camera selects an existing LOD; it does not regenerate the topology library. [K1–K3]
2. PlanetRenderer creates vertex, normal, color, height/gradient and culling-index arrays indexed by frame slot. Each buffer is sized from the maximum terrain topology and requests device-local memory. Those allocations last until renderer disposal. Uniform/modifier and UV-staging mappings also have a persistent lifetime. Allocation reuse does not imply that their geometry contents are reused. [K4]
3. PlanetRenderer.OnFrame updates selected LOD, billboard/snap transforms, and texture demand. Program then calls GenerateMeshData for the applicable rendered frame without a replacement-only or dirty-geometry guard. It updates six mesh descriptor entries and records prepare, each configured modifier, finalization, normals and camera culling. The resulting frame-slot geometry is regenerated even when the same CubeMesh is selected. [K5–K6]
4. Camera culling updates its descriptors/push constants and resets the indirect index count. One shader invocation tests each triangle and directly appends its three indices on survival. There is no intermediate visibility array and no separate compaction dispatch in this selected KSA terrain path. The culling allocation persists; its list contents are reconstructed. [K7]
5. No source/destination prepared-physical copy map, per-replacement rational lookup, or recurring mapped-index scan appears in this path. This bounded finding must not become a claim about every KSA subsystem. [K5–K7]

## Actual command order and queues

Program.Render's acquired frame supplies commandBuffer2. Program records GenerateMeshData at line 4541 and PlanetRenderer.Render at line 4644 into that same buffer. GenerateMeshData's compute barriers order prepare/modifiers/finalize/normals; it then calls PerformCulling. Culling barriers make its output available for vertex-index and indirect consumers. Terrain Render binds the frame-slot prepared buffers and the culling index output before DrawIndexedIndirect. [K5–K8]

Thus this terrain preparation is in the graphics-capable frame command stream with its consumers. The separate compute command-pool BeginFrame/SubmitFinalize calls and the semaphore waited by TrySubmitFrame are real, but they do not show that GenerateMeshData runs on that separate compute queue. No asynchronous geometry-preparation queue migration or freedom from current-draw contention is proven.

KSA keeps multiple frame-slot allocations, but the reviewed KSA source does not expose NovaCore's separate current/incoming published physical owner, source-copy capability, or 11-slice physical transaction. Same-frame prepared geometry must be ready before its own draw. Old published texture views remain usable during an upload; that narrower resource-readiness responsibility is genuinely comparable.

## Asynchronous data residency and exposure

CelestialTextureStreamer.Tick derives demand from celestial distance/projected size, field of view, viewport height, texture-quality settings and optional full-resident export demand. It owns mip selection and a memory budget, prioritizing height. This is per-celestial texture-mip demand, not NovaCore's exact geographic residual-record footprint. Near the body it requests the finest allowed mip. [K9]

StreamingUploader lazily creates a below-normal background worker. It dequeues prioritized requests, builds/stages upload regions, and submits batches. Defaults are 32 MB per batch target and 96 MB in-flight admission target. CelestialTextureStreamer permits two concurrent streams and at most two mip levels per promotion step. These are byte/work-count policies, not GPU-millisecond frame budgets. A whole request can exceed the remaining batch target; another batch can exceed the in-flight threshold after admission. They are not strict hard memory bounds. [K9–K10]

The uploader uses a dedicated transfer family only if available, different from graphics, and the batch does not touch an already graphics-owned image. Otherwise it submits on graphics. Dedicated-transfer uploads record release/acquire work and a semaphore into a graphics staging pool. Poll checks the completion gate with WaitFor(0), marks requests complete and invokes completion callbacks only afterward. When a graphics acquire is required, that acquire is part of the completion gate. No upload latency deadline is established. [K10]

OnPromotionCompleted creates a view exposing completed mip levels, retires the previous view/handle, updates the resident level and increments ContentVersion. GetHeightView returns the published view or existing coarse asset. Changing allocation tier or failure may reset to coarse; an incomplete finer upload is not simply exposed as ready. RetiredResourceQueue delays reclaim until sufficient frames have advanced or all relevant fences report complete. The image can then return to a shape-keyed FreeImagePool. [K9–K12]

Important limit: the reviewed FreeImagePool has no explicit idle-byte/count cap. The streamer's active image budget therefore cannot be cited as proof that all retained idle allocations are bounded by that same value. This is a comparison limit, not an expansion into KSA cleanup or NovaCore cache work.

## Repeated-work frequency table

“Banked” means current NovaCore M13.4. Private map/index experiments are labeled separately. “Required frequency” describes the invariant boundary to prove, not an instruction to implement it. This worker measured no event/frame timings; all cost cells await the lead's Work A/B measurements. KSA source cannot supply missing NovaCore payoff numbers.

| Responsibility | Current NovaCore frequency | Required frequency | KSA frequency | Cost/event | Cost/frame | Candidate migration |
|---|---|---|---|---|---|---|
| Topology validation | Managed immutable topology load/generation; banked native per-triangle checks during current and incoming cull. | Immutable structural/index qualification once per retained topology; changed physical values require their own readiness. | CubeMesh topology, adjacency and neighbor data generated during library loading and retained. No equivalent repeated standalone index validator found in selected render path. | Not measured here | Not measured here | ADAPT immutable topology authority; do not remove native checks without moving their proof. |
| Source map construction | Rejected mapped prototype builds synchronous rational lookup per replacement; banked incoming preparation has no such copy map. | A topology-pair address relation can belong to those immutable resources; physical reuse authorization still belongs to each transaction. | No equivalent current-to-incoming exact physical copy map in inspected path; selected persistent CubeMesh supplies indices directly. | Not measured here | Not measured here | ADAPT persistent resource ownership only; KSA does not prove the NovaCore map algorithm. |
| Mapped-index scan | Private residual vertex-validator prototype scans once per distinct resident topology, gated from incoming acquisition (10 scans in the preceding control); this is not banked behavior. | Actual immutable native index resource once on creation, if that replaces repeated structural validation. | Generated static mesh/index/adjacency resources persist; no recurring CPU mapped-index scan in GenerateMeshData or PerformCulling. | Not measured here | Not measured here | ADAPT once-per-resource qualification, subject to Work B measured lifecycle. |
| Visibility list construction | Banked current-owner camera cull every frame; incoming cull once on its final preparation frame. | When camera or geometry inputs require a current draw list; stable topology alone does not make visibility invariant. | Applicable rendered frame: frustum cull directly emits current visible indices. | Not measured here | Not measured here | ADAPT camera-owned visibility. KSA does not justify caching current visibility solely during replacement. |
| Compaction | Separate compact dispatch after current cull every frame; once after final incoming cull. | When producing a changed visibility-derived index list, without duplicating its ownership. | Fused with culling: each survivor reserves three indices and writes them directly in PlanetFrustumCulling.comp. | Not measured here | Not measured here | ADAPT only if a measured single visibility-construction responsibility clears the gate; no performance claim from source alone. |
| Full physical preparation | Incoming or staged-pupil job, 65536-vertex slices; published physical geometry retained between jobs. | Changed authoritative physical inputs/topology/pupil, with complete required data and deterministic publication. | GenerateMeshData dispatches prepare, modifiers, finalization and normals every applicable rendered frame. | Not measured here | Not measured here | INTENTIONALLY DIFFER: retain NovaCore complete generation-4 physical authority; KSA is not evidence for a lower-frequency physical reuse cache. |
| Reusable physical copy | Private same-index/mapped incoming copy per slice; not an accepted banked incoming path. | Only exactly authorized unchanged physical values in a qualified source/destination lifetime. | No equivalent exact 64-byte prepared-vertex source copy in the inspected GenerateMeshData path. | Not measured here | Not measured here | INTENTIONALLY DIFFER where exact reuse is required; retain NovaCore identity/fallback proof. |
| Buffer allocation | Bounded retained topology resources and current/incoming/spare work capacities; growth/acquisition allocates, not every frame. | At retained resource creation/capacity change, never merely because an equivalent replacement recurs. | Per-frame-slot maximum-capacity geometry and culling buffers created by PlanetRenderer constructor and released at Dispose; static CubeMesh library retained. | Not measured here | Not measured here | ADAPT persistent capacity ownership; first identify actual NovaCore avoidable allocation frequency. |
| Buffer mapping | Host-visible allocations stay mapped over their resource lifetime; private CPU scans read those mappings. | Resource lifetime for persistent host mappings; device-local-only resources do not need CPU mapping. | Uniform/modifier/staging control buffers mapped at construction and unmapped at Dispose; generated vertex/normal/color/height/index output requests device-local memory without a CPU map. | Not measured here | Not measured here | ADAPT separated host control and GPU working-data ownership without weakening native inspection readiness. |
| Descriptor/resource binding | Descriptor contents change at resource/publication changes; compute/draw binds occur in command recording. | Descriptor update when its resource/view/range changes; binds for consumers. | Persistent descriptor sets, but mesh descriptors updated in each GenerateMeshData and cull descriptors in each PerformCulling; frame-slot sets bind for current commands. | Not measured here | Not measured here | ADOPT correct resource lifetime; KSA does not prove eliminating all per-frame descriptor updates. |
| Publication readiness | Complete regional footprint, finished slices, final fence and atomic current/incoming swap. | Each authoritative replacement, with current owner retained through the last consuming fence. | Geometry consumed after same-command-buffer preparation/barriers. Texture views published from zero-time completion polling, with old views retired after safe frames/fences. | Not measured here | Not measured here | ADAPT completion-before-exposure and delayed retirement; INTENTIONALLY DIFFER for NovaCore exact physical-generation contract. |


## Adopt, adapt, intentionally differ

- ADOPT the distinction between persistent allocations/topology and camera-dependent visibility contents. Persistent geometry alone is not evidence that a camera list can persist unchanged.
- ADAPT immutable-resource construction/validation and long-lived working capacity where NovaCore measurements prove reconstruction at excessive frequency. NovaCore already retains topology and work buffers; the mere presence of KSA persistent buffers proves no remaining saving.
- ADAPT completion-before-exposure, source-view lifetime and deferred reclaim. Use NovaCore's actual generation, basis, dependency and allocation identity, not a borrowed frame-age heuristic.
- ADAPT KSA's single cull-and-index-output responsibility only if one measured winner has sufficient payoff. The source comparison supplies an architectural precedent, not permission to reopen the rejected final-block optimization or combine independent changes.
- INTENTIONALLY DIFFER on canonical physical authority. KSA can display a valid coarse published texture while a finer mip streams. NovaCore's generation-4 H and regional residual footprint require complete authoritative data before physical publication; a coarse substitute would weaken that contract.
- INTENTIONALLY DIFFER on exact prepared-physical reuse where NovaCore uses it: the reviewed KSA path rebuilds frame geometry, so it supplies no proven exact-copy map/lifetime implementation to transplant.
- Do not claim KSA supports a separate asynchronous terrain-preparation queue, fixed terrain GPU slice budget, persistent camera list, or guaranteed spike-free publication. Those equivalences were not found.

KSA does not establish that NovaCore performs full physical generation or camera visibility at a higher frequency: KSA itself performs both each applicable rendered frame. It does establish a useful persistent topology/allocation boundary, and it fuses visibility selection with index output. The remaining winner still requires NovaCore event-level causal cost and an 8.33-ms payoff with margin.

## Source anchors

All line numbers refer to the current cached decompilation/shader files whose identities are recorded in work-c.json.

- K1: build/ksa-residency-reference/assembly-source/KSA/PlanetMeshLibrary.cs:36-108, load-time referenced-collection generation.
- K2: build/ksa-residency-reference/source/KSA.PlanetMeshCollection.cs:96-147, parallel level generation, retained maxima, LOD selection and disposal.
- K3: build/ksa-residency-reference/assembly-source/KSA/CubeMesh.cs:153-330 and 334-426, topology/adjacency/neighbor generation and GPU upload.
- K4: build/ksa-residency-reference/source/KSA.PlanetRenderer.cs:688-728, 1117-1132, 1499-1600, persistent control mappings and device-local frame-slot resources.
- K5: build/ksa-residency-reference/source/KSA.PlanetRenderer.cs:1770-1943, full selected mesh generation, barriers and culling; 2143-2202, frame demand/LOD/snap updates.
- K6: build/ksa-residency-reference/source/KSA.Program.cs:4464-4552 and 4644, acquired-frame preparation and subsequent terrain draw.
- K7: build/ksa-residency-reference/assembly-source/KSA/PlanetFrustumCulling.cs:117-202 and E:/Kitten Space Agency/Content/Core/Shaders/Planet/Culling/PlanetFrustumCulling.comp:57-99, per-frame descriptor update/reset and direct surviving-index append.
- K8: build/ksa-residency-reference/source/KSA.Program.cs:2260-2267 and 2350-2365; PlanetRenderer.cs:2023-2041, queue-semantic boundary and indirect consumer.
- K9: build/ksa-residency-reference/source/KSA.Rendering.CelestialTextureStreamer.cs:102-118, 319-334, 415-443, 480-503, 649-665, 746-789 and 796-884, demand, budgets, promotion and publication.
- K10: build/ksa-residency-reference/source/KSA.Rendering.StreamingUploader.cs:33-35, 101-146, 160-312 and 344-364, worker, batching, transfer-family handling and completion gate.
- K11: build/ksa-residency-reference/source/KSA.Rendering.RetiredResourceQueue.cs:62-127, delayed disposal/reclaim.
- K12: build/ksa-residency-reference/assembly-source/KSA.Rendering/FreeImagePool.cs:9-68, keyed reuse without explicit idle capacity limit.

## Reproduction and output boundary

Run Python with -B when reading or recomputing this JSON's SHA-256 entries; this avoids creating local bytecode. Fingerprints can be rechecked directly from the stored absolute paths, without copying KSA files, building anything or generating GPU evidence. The installed version was independently read through the DLL's Windows VersionInfo and matches cached AssemblyInfo.cs.

Only work-c.md and work-c.json were written for this subtask. No bulk evidence, production changes, source copies or deferred diagnostic scratch were created.
