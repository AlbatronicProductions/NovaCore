# Work C — KSA comparison for the final whole-frame causality gate

## Decision boundary

The current KSA terrain path supports three useful responsibility boundaries:

- Contributing material response belongs to fragment shading; material displacement that changes geometry remains in hardware tessellation.
- Generated geometry and camera visibility indices use persistent device-local working buffers. Host control/upload mappings have a separate purpose.
- Visibility owns a conservative displacement envelope. KSA derives that envelope from authored material displacement and does not perform NovaCore's camera-to-triangle distance classification in its cull shader.

These are architectural precedents, not measured NovaCore savings. This review proves no 1.5 ms winner, makes no regional-blocker assumption, and cannot independently justify either implementation or M13 closure. The lead's matched whole-frame controls decide the payoff. No source examined here supports reducing positive detail, changing canonical physical authority, or removing hardware tessellation to achieve that payoff.

## Reference identity and scope

Fresh local verification identifies KSA.dll as file version **2026.9.7.5402**, product **2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400**, SHA-256 **a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f**. The current NovaCore HEAD was **047ae479b33831eae1c0dfa3f37c657a7f70148f**. The inspected NovaCore production files had no tracked diff.

The accompanying work-c.json records 23 current file fingerprints. Ten overlap the immediately preceding lifecycle reference and all ten match. Other entries are current fingerprints of narrowly inspected installed shaders or NovaCore source. Cached KSA decompilation was reused with its prior binary/source identity chain; it was not freshly decompiled. VersionInfo was read directly from the installed DLL. No KSA build, execution, performance measurement or Discord research was needed.

The previous [lifecycle review](E:/NovaCore/docs/engineering-evidence/m13-regional-replacement-lifecycle/work-c.md) remains the source for detailed streaming, publication and retirement analysis. This document revisits only material interfaces, GPU working-data placement, and the newly identified zero-support visibility calculation.

## Positive material contribution

### Current KSA stage and frequency

KSA's Planet.frag receives world position/normal, texture position/normal, flat celestial position, interpolated detail strength and vertex color. It reads macro color/normal maps, biome IDs and biome control. The material coordinate is the camera-relative world position rotated by Cce2CcfMatrix. A CPU-prepared, per-material/per-scale fractional offset restores stable texture phase. [K1–K3]

Each applicable frame, UpdateUvOffsets computes the camera's body-frame position in double precision, evaluates fractional texture offsets for 16 scale levels and both authored ground/slope material slots, packs those small offsets into floats, and copies its staging range into the GPU offset buffer. Its transfer barrier targets TES, fragment and compute consumers. That is a bounded coordinate-phase interface for periodic textures; it is not evidence that a world-coordinate procedural hash can lose its integer cell address. [K3]

In the fragment shader:

1. Biome IDs/control and prepared mesh slope select up to four material candidates.
2. A shared biplanar structure captures the selected projection axes, blend weights and pre-projection coarse derivatives.
3. Distance selects two texture scales and their offset entries.
4. The first material pass samples displacement for up to four candidates and uses it to adjust material blend weights.
5. The candidates are re-sorted. The second pass samples the strongest **three**, skipping negligible weights, for diffuse, detail normal, and combined AO/roughness/metallic response.
6. Macro color, detail strength and those material values feed lighting and shadow consumers. [K1–K2]

This is positive fragment work, not an already prepared per-vertex material lookup. Both distance-scale samples remain in each enabled projection of SampleBiplanarTextureGrad; projection and negligible-material guards limit the selected work. The reviewed source does not establish a persistent cache of the final shaded material value.

### Tessellation remains a separate geometric responsibility

KSA TCS derives edge factors from projected edge size, camera distance and perspective skew. TES interpolates the prepared triangle, resolves material candidates, samples displacement at explicit texture LOD 2, applies material scale and blend weights, fades displacement near its configured range boundary, and changes gl_Position. Beyond the tessellation range it passes geometry through. [K4]

Therefore future surface relief and silhouette capacity remain legitimate TES responsibilities. Fragment normal/detail work is not interchangeable with that displacement. One source detail also limits literal copying: KSA's TES outputs its interpolated pre-displacement world-position varying even when gl_Position uses the displaced position. This review does not qualify that representation against NovaCore's final physical/material receiver identity, nor claim exact cross-stage position parity for KSA.

### Direct NovaCore comparison

Banked M13.4 already:

- keeps geographic albedo as macro material authority;
- calculates footprint derivatives on a smooth camera-local differential domain;
- keeps body-fixed procedural coordinates in FP64;
- shares biplanar weights between material frequencies;
- skips only zero-contribution noise bands;
- shares the repeated four-corner FP64 hash prefix;
- reuses positive meso/micro noise for material response and bounded shading-normal variation. [N1–N2]

The three positive procedural scales are 96 m, 5.5 m and 410 m. Meso/micro values affect several outputs, so deleting a nominally expensive call can alter albedo, roughness, AO and normals together. KSA's textures are not the same field as NovaCore's FP64 procedural hash. Copying KSA's FP32 local texture representation, substituting a texture, removing a band, or lowering precision would change material representation or its precision contract. None is a proven output-preserving cleanup in this ticket.

**Adopt/adapt:** preserve the separation of macro geographic identity, local periodic detail, geometric displacement and filtered fragment response. Share exact common inputs/evaluations where still redundant and measured. **Intentionally differ:** preserve NovaCore's exact accepted field and coordinate contract; KSA supplies no equivalent exact hash evaluator to transplant. Do not propose four-corner prefix sharing as new work: it is in the current HEAD.

### A valid cheap-fragment control must preserve required inputs

| Dependency | Why the production result consumes it | Causal-control requirement |
|---|---|---|
| Prepared geometric normal and body direction | Projection weights, land/ocean normal blend, material response and lighting | Keep a live, nonconstant downstream dependency; shader input declarations alone do not prove consumption. |
| Camera-relative position and FP64 body material position | Stable metre-scale noise, derivative footprint, view lighting and facility light visibility | Consume the actual coordinate values in the control; replacing all material output by constants can eliminate their upstream production work. |
| Macro surface layers/address, albedo, elevation and land | Geographic sampling, presentation weights and land detail | Keep required sampling/interface work live when measuring the incremental positive material field. |
| Smooth differential domain and derivatives | Frequency attenuation and height-derived shading normal | Preserve the required derivative domain and helper-invocation semantics; a control cannot be called equivalent merely because its color is plausible. |
| Material weights and light/facility inputs | Roughness, AO, specular response, authored occlusion and final radiance | Keep them in observable output or separately instrument their surviving workload. |

A cheap control is an attribution experiment, not an image-parity candidate. Inspect generated SPIR-V/driver ISA and actual upstream invocation/interface behavior under the same compiler configuration. A declared varying, multiply-by-zero checksum, or dead output is not adequate evidence that a dependency survived optimization. Report the baseline-minus-control result as a conditional material-path bound until that survival is demonstrated. A production winner additionally needs unchanged physical/depth/HDR/image behavior under the accepted contract. This worker generated no shader binary or ISA and makes no claim that the lead's controls have passed these checks.

## Persistent GPU working-data placement

KSA CreateMeshGenerationBuffers allocates maximum-capacity vertex, normal, color, height/gradient and culling-index arrays per frame slot. Each requests DeviceLocalBit and survives until renderer disposal. It does not map these generated working arrays for CPU inspection. Uniform, biome/modifier and UV-staging controls are separately mapped. [K3, K5]

NovaCore already retains topology resources and current/incoming/spare working capacity. Its CreateHostBuffer requests HOST_VISIBLE | HOST_COHERENT, maps and zeroes the allocation, and supplies the production prepared-physical, visibility, compacted-index and retained topology resources. It does not require DEVICE_LOCAL. That absence does **not** prove the actual selected type lacks device-local memory: the allocator's selected memory type/heap must be recorded. [N3]

The lead's isolated bulk-local control preserves coherent host mappings while changing only the placement of physical/staged/visibility/compacted/lattice/index working data. That is a compatible way to measure placement without simultaneously redesigning validation/readback. It need not imitate every KSA flag: a DEVICE_LOCAL allocation may also be host-visible/coherent on the measured device.

| Responsibility | Current NovaCore | Current KSA | Compatible conclusion |
|---|---|---|---|
| Allocation lifetime | Retained topology and current/incoming/spare capacities; allocation on acquisition/growth | Maximum-capacity arrays per frame slot; retained mesh library | Persistent capacity is already shared architecture, not by itself new avoidable work. |
| Bulk GPU placement | Host-visible/coherent requirement; chosen heap needs measurement | Generated vertex/normal/color/gradient and cull-index buffers require device local | A placement-only measured control is justified; no saving may be inferred from names or flags alone. |
| Preparation frequency | Complete canonical physical replacement, retained between replacements | GenerateMeshData each applicable rendered frame | Do not copy KSA's preparation frequency or claim KSA proves NovaCore redundant preparation. |
| Visibility frequency | Camera cull and index-list construction for current draw | Per-frame cull directly appends surviving indices | Stable allocation does not authorize a stable camera-visibility cache. |
| Current draw ordering | NovaCore current/incoming publication and fences | Preparation, barriers, cull and draw in the graphics frame command buffer | KSA does not demonstrate an asynchronous terrain-preparation queue or eliminate contention. |
| Publication/retirement | Exact physical authority and complete regional footprint before publication | Texture views exposed after completion polling; prior views retired after safe frames/fences | Completion-before-exposure is compatible. Coarse texture fallback is not NovaCore physical authority. |

No placement proposal should bundle cull fusion, map reuse, a new queue, scheduling changes or a cache redesign. The required causal result is actual selected memory identity, unchanged bytes/lifetimes/readiness, and net whole-frame benefit including any host access cost. Prior regional placement measurements do not substitute for this fresh whole-frame gate. The KSA streaming pool has no reviewed explicit idle-byte cap; neither fixed GPU milliseconds nor universally bounded retained memory follows from this precedent.

## Newly emerging zero-support cull calculation

The current NCSM1 cull reads the prepared displacement envelope, unconditionally evaluates triangleDistanceSquared against the camera, and selects screenTesSupport as either that envelope or zero. Its diagnostic counters also classify triangles using tesActive. Current and incoming shaders contain this responsibility. [N4]

KSA's corresponding production owner computes one CullingTolerance from the maximum authored ground/slope material DisplacementScale. Its cull shader encloses the prepared triangle by centroid plus maximum vertex radius and that tolerance, then tests frustum, planetary occlusion and ocean occlusion before writing indices. It does not compute the point-to-triangle camera distance or a 50 m TES-active classification in this cull. The support accounts for potential later TES displacement rather than making the current screen-space distance test its owner. [K6–K7]

That comparison supports the narrower responsibility question: does a zero-valued geometric support envelope leave any production consumer of the expensive distance classification? It does **not** authorize replacing NovaCore's cull with KSA's sphere or its support constant. NovaCore has different conservative plane/horizon and exact physical contracts.

A bounded exact-output candidate, if the lead proves the zero-envelope condition, can avoid the distance evaluation only where its production screen-support result is already exactly zero, while retaining it when required by the diagnostic classification or a nonzero future TES envelope. Do not simply force tesActive false: that would falsify the diagnostic counters. Do not globally disable future displacement support. Preserve all validation, geometry, reject-list and publication behavior; inspect optimized code to prove the unused computation actually disappears.

No avoided-time number is available from this source review. This remains a candidate for the lead's isolated cheap gate, not a 1.5 ms winner established by analogy. If the distance computation has no material net payoff, it is insufficient for this ticket even if mathematically unnecessary.

## Requested actual-executable ISA cross-check

The lead supplied two completed runs, base-florida-repeat-isa.json and noise-florida-isa.json, with driver ISA captures under their matching -compiler directories. Work C inspected those existing files without a new build or GPU run. Only planetary_production.frag.spv differs in the journals' deployed shader hashes; their managed binary and arguments match. Their private native DLL hashes differ, so the lead still owns that host provenance check.

**The captured upstream executable is unchanged.** Stages 1 and 2 expose the same AMD merged hull executable in each run, and both files are byte-identical across runs. Stage 4 is also byte-identical. Its position and param0–param7 export instructions are unchanged. Do not sum stages 1 and 2 as independent 806-instruction programs: the captured executable is shared.

| Vulkan stage flag / captured executable | Static instructions, base → control | VGPR, base → control | SGPR | LDS bytes | Scratch bytes |
|---|---:|---:|---:|---:|---:|
| 1 / merged hull | 806 → 806 | 39 → 39 | 64 | 32,212 | 0 |
| 2 / same merged hull | 806 → 806 | 39 → 39 | 64 | 32,212 | 0 |
| 4 / tessellation evaluation | 546 → 546 | 48 → 48 | 32 | 4,608 | 0 |
| 16 / fragment | 2,895 → 2,047 | 78 → 63 | 53 | 0 | 0 |

The fragment executable retains the same 22 interpolated/flat attribute components and exactly the same 32 attribute references: attr0.xyz, attr1.xyz, attr2.xyz, attr3.xyz, attr4.xyz, attr5.x, attr6.xyz and attr7.xyz. These are the driver's packed surviving interface, not an assertion that every originally declared GLSL location is used. Baseline and control have three static image instructions each. The static FP64-opcode count changes 763 → 187 in the fragment executable; upstream FP64 counts remain 67 in the shared hull executable and 101 in TES.

Normalized opcode SHA-256 values:

| Executable | Baseline | Noise control |
|---|---|---|
| Stage 1 / 2 | 922fd309e6a1c1b0cf71331ac2c1d9212079ed591a622dc52477537c07d139e7 | identical |
| Stage 4 | 5bf857b285e9fd00d9dca5c88049e034ed480c5ab7955a5250f05d0b806172c7 | identical |
| Stage 16 | 69589311f892d3dbbc4422dce6ee84e2243d635de077799394bf71933126c0f9 | 83d829289934c1ce1a1c77aca34dae073dbd72bec981039de655ca002b9aa301 |

Normalization removes NUL, discards labels and hex-encoding comments, excludes s_code_end padding, extracts the opcode from each remaining assembly instruction, then hashes the LF-joined list with a final LF. The JSON also retains raw ISA hashes, normalized full-instruction hashes, exact attribute counts, export instructions, metadata and both journal identities.

This evidence rules out the specific upstream dead-code/interface contraction concern for the captured oppositeFace=1 pipeline. It does not prove every variant, equal dynamic execution, a numerical material cost, or an output-preserving optimization. Fragment register pressure changes are part of this diagnostic control and may affect scheduling. The lead must still match camera/workload/host provenance and interpret the duration difference conditionally. No exact material milliseconds are asserted here.


## Source anchors

Paths below identify installed shader source or cached decompilation whose current SHA-256 values appear in work-c.json.

- K1: E:/Kitten Space Agency/Content/Core/Shaders/Planet/Planet.frag:25–46, 107–175, 178–273 — fragment interface, material selection and response.
- K2: E:/Kitten Space Agency/Content/Core/Shaders/Common/Planet.glsl:179–223, 277–373, 431–541, 817–844 — offsets, top-four/top-three limits, tiling, shared biplanar derivatives, texture sampling and material reweighting.
- K3: E:/NovaCore/build/ksa-residency-reference/source/KSA.PlanetRenderer.cs:688–728, 2050–2113; E:/NovaCore/build/ksa-residency-reference/source/KSA.Program.cs:4542 — mapped control/staging ownership and per-frame double-precision fractional offsets.
- K4: E:/Kitten Space Agency/Content/Core/Shaders/Planet/PlanetTessControl.tesc:32–68; PlanetTessEvaluation.tese:44–164; TessellationFuncs.glsl:48–115 — factors, material displacement, range and varyings.
- K5: E:/NovaCore/build/ksa-residency-reference/source/KSA.PlanetRenderer.cs:1499–1600, 1770–1943, 2023–2041; E:/NovaCore/build/ksa-residency-reference/source/KSA.Program.cs:4464–4552, 4644 — retained generated buffers and same-frame preparation/draw ordering.
- K6: E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/PlanetFrustumCulling.cs:117–237 — indirect reset, output visibility, material-envelope calculation.
- K7: E:/Kitten Space Agency/Content/Core/Shaders/Planet/Culling/PlanetFrustumCulling.comp:57–99; E:/Kitten Space Agency/Content/Core/Shaders/Culling/Culling.glsl:4–38 — triangle enclosing sphere and cull consumers.
- N1: E:/NovaCore/native/NovaCore.Native/shaders/production_terrain_material.glsl:24–88, 206–279 — exact hash prefix, derivative/projection inputs, positive/zero noise contributions.
- N2: E:/NovaCore/native/NovaCore.Native/shaders/planetary_production.frag:1–36, 248–328 — final material receiver, physical normal, geographic material and facility-light consumers.
- N3: E:/NovaCore/native/NovaCore.Native/NovaCoreNative.cpp:1551–1555, 1589–1641; E:/NovaCore/native/NovaCore.Native/RegionalPhysicalPreparation.inl:51 — host-mapped allocation policy and retained working resources.
- N4: E:/NovaCore/native/NovaCore.Native/shaders/production_nested_scale_mesh_cull.comp:41–57, 124–149; production_nested_scale_mesh_incoming_cull.comp:118–143 — distance, support and diagnostic consumers.

## Reproduction, limits and retained output

Recompute SHA-256 directly from each absolute path in work-c.json using Python with -B, or Get-FileHash. Read JSON/Markdown explicitly as UTF-8. There is no need to copy source, extract raw evidence, build KSA or recapture frames. The 23 source fingerprints and this bounded comparison are the only new Work C artifacts.

This review performed no builds, GPU runs, renderer edits, source copies, staging or banking. Its follow-up ISA analysis used existing captures supplied by the lead. It claims no measured KSA performance, no equivalence of KSA's complete physical publication contract, and no measured payoff or production acceptance for the lead's private controls. No unresolved KSA source gap justified Discord research. Source-level plausibility remains subordinate to the final matched whole-frame causality and safety gates.
