# Florida visible terrain / depth / presentation investigation

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Date: 2026-09-05. Read-only investigation. Route convergence remains **UNBANKED**; Florida manual acceptance remains **FAIL**.

## Result and evidence boundary

**BLOCKED at runtime provenance: V7 — the supplied manual session used the pre-facility-support Release runtime.** This is a proven artifact mismatch, not a proven explanation of every visible pixel in the recording. No V1–V6 visual mechanism is established by this investigation. Do not design a renderer, TES, grading, depth, or material correction from this evidence.

The ticket requires facility support to be active. The captured executable's deployed CPU authority and GPU preparation/TES shaders do not contain that implementation. Generation 4, terrain-v5, NCSM1 ownership, and complete regional residency do not imply that the facility-support revision is present.

The earlier facility-support validation built native Debug/Release artifacts and validated the Debug sample, but did not ensure the executable's **Release launch directory** was refreshed. The prior readiness handoff therefore missed a deployment verification gate. The user's recording remains valid evidence of a player-visible failure in the runtime actually launched; it is not evidence that the completed facility-support candidate has failed the same way.

## Inputs and preservation

- Ticket: `C:/Users/Tyler/.codex/attachments/5843e785-1166-4647-b5bc-5179a8bc4a94/pasted-text.txt`.
- Video: `E:/Videos/2026-09-05 07-21-50.mp4`; 3440 × 1440, 60 fps, 96.384 seconds; SHA-256 `e5843e05e474c7493299e6592513884f4ef7825a3a4e8fee28abe434b62f91df`.
- Log: `C:/Users/Tyler/Downloads/ENovaCoresamplesNovaCore.TrianglebinReleasenet10.0NovaCore.Triangle.exe.txt`; SHA-256 `b04b6aaa8f6753a09a9d6278b658389e0c0677921e8d5c20f47c7e9dfd906d41`.
- Evidence directory: `E:/NovaCore/build/florida-visible-investigation/` (ignored). Includes incoming hashes/status/diff, extracted frames, binary manifests, SPIR-V disassembly, isolated binary-query results, and parsed log evidence.

Only this investigation document is added to the visible working tree. No existing production source, shader, configuration, test, document, or launch-directory artifact was edited. The binary probes copied existing assemblies into ignored diagnostic directories; they did not rebuild any production project or invoke its native-copy targets.

## Runtime mismatch proof

The log's managed entry assembly, native DLL, production fragment, TCS and TES hashes match the files currently in `samples/NovaCore.Triangle/bin/Release/net10.0`. Full SHA-256 values are in `complete-artifact-manifest.json`; abbreviated values below are for reading only.

| Artifact | Captured/deployed Release | Existing candidate artifact |
|---|---|---|
| Native DLL | 366,592 bytes; `41ed35c2897b…` | Native Release build: 372,736 bytes; `4016ee44bb95…` |
| TES | 60,292 bytes; `3300441e705d…` | Native Release build and Debug launch copy: 64,580 bytes; `5e3903dbcb49…` |
| Current preparation compute | 101,980 bytes; `980006ff5def…` | 111,492 bytes; `3d61c64246a2…` |
| Incoming preparation compute | 100,884 bytes; `178afeb0442f…` | 108,440 bytes; `c650dc48c73f…` |
| VS | 8,028 bytes; `1baa9043db7a…` | Identical |
| TCS | 12,632 bytes; `0163498278f9…` | Identical |
| Production fragment | 163,256 bytes; `e9bd73570204…` | Identical |

This is not inferred merely from different Debug/Release hashes. SPIR-V disassembly shows the captured TES's `EvaluateNaturalCandidateNearD` directly returns the natural near field. It has no support weighting or support-gradient term. The candidate TES calls `FacilitySupport`, multiplies height by `1-weight`, and transforms its gradient. The captured preparation shader also has no facility-support call; the candidate preparation shader has four static call sites. Both base preparation and TES are old, so this is **not** proof of V2's narrower claim that a correct supported base diverges only in final TES.

An isolated executable linked to copies of the deployed Release assemblies loaded the real global and regional data and queried Florida. The same executable was then run separately with copies of the candidate Debug assemblies:

| Binary query | Deployed Release | Candidate Debug |
|---|---:|---:|
| Core `FloridaFacilitySupport` type exists | false | true |
| Canonical center H / site anchor terrain height | 18.886950220789107 m | 15.134892258793116 m |
| Terrain-relative root offset | 3.083511314970373 m | 6.835569277405739 m |
| Foundation depth | 6.835569277405739 m | 6.835569277405739 m |
| World/body root radius | 6371030.770461536 m | 6371030.770461536 m |

The Release query exactly reproduces the supplied seating log, line 15. The height difference is 3.752057961995991 m at the center; it is **not** a measured visible foundation gap. The geographic anchor, fixed world root, and footing dimensions are unchanged.

`NovaCoreNative.cpp:1353` (`Read`) resolves shaders relative to the loaded native module. The log's separate `[native] Loaded runtime paths` string prints a CWD-based absolute shader path; it is not the loader's resolution rule. The real module-relative TES file matches the log's runtime fingerprint. Building `build/native-ninja-release` alone does not refresh the sample output: `NovaCore.Triangle.csproj` performs that copy in `CopyNativeRuntime`, after the managed sample build.

## 1. Pose A / B / C and reproducibility

Video observations, with original frame-size PNGs preserved:

| Sample | Video time | Observation | Evidence |
|---|---:|---|---|
| A reference | 00:24 | Foundation appears substantially flush with the dark ground silhouette | `video-24s.png` |
| B reference | 00:48 | Strong floating impression: a faint mottled band reaches beneath the bright foundation while dominant green ground appears lower | `video-48s.png` |
| C nearby sample | 00:49 | Nearby moving view changes the faint band's silhouette; floating impression persists | `video-49s.png` |

C is a nearby motion sample, **not** a verified flush-to-floating transition capture. These timestamps reproduce video images, not simulation poses. No exact camera body position, camera quaternion, full view-projection matrix, input replay, or video-to-submission synchronization is recorded for these samples. The early focus trace's `orientation` is the body's presentation orientation, not the camera quaternion. Inventing camera poses from it would be incorrect.

The recorded initial simulation instant is 841879272564746 ticks, rate 1:1, UTC startup `2026-09-05T11:20:03.3821861+00:00`. Those values do not establish the simulation instant at a particular video pixel/frame. No new production run was presented as an exact replay. The required supported-candidate A/B/C capture gate remains unmet.

## 2. Production owner state

The supplied log establishes generation 4, `surfaceMode=2`, terrain-v5, NCSM1 topology family 1, and L17. Line 298 records regional readiness: 670 requested/resident records, 93,392,640 uploaded bytes, complete=true, host visibility barrier before preparation.

Frame 86 is the first recorded NCSM1 owner: global/dynamic/distant=0, candidate=1, material=production-billboard. Submission line 305 reports candidateIndirectDraw=1 and globalDraw=dynamicHierarchyDraw=0. Frame 180 is the last detailed owner/submission trace. Later pipeline statistics extend through frame 4885 with generation 1/L17/NCSM1; these are not timestamp-synchronized to the video samples. There are 180 owner/submission records and 41 refinement records. Facility support is absent throughout this captured runtime; the mandatory owner/revision state for A/B/C cannot be certified.

## 3. Contributor map and submitted sequence

The following is a **source-recording-path audit**, not a replayed GPU event list for A/B. Relevant code: `NovaCoreNative.cpp:2280–2364`, pipeline creation at 1500–1615, mesh creation at 736–789, `Program.cs:256–262`, and the shader files named below. Exact resource handles, per-pixel winning events, and frame-specific indirect contents were not captured.

| Contributor | Geometry / submission | Material / depth responsibility after candidate publication |
|---|---|---|
| NCSM1 current publication | Physical SSBO binding 38; compute-produced compacted uint32 index buffer; one indexed indirect draw; instance count comes from its indirect command | Final VS/TCS/TES raster; `planetary_production.frag`; eligible Earth depth/color owner |
| NCSM1 incoming preparation | Separate pending compute buffers; demand, physical preparation, cull/compact, publication | No separate incoming graphics draw in the recording path |
| Facility grading | Canonical physical modifier incorporated into prepared positions and attenuated TES near field in the candidate | No dedicated site-terrain mesh, overlay, or support-plane draw; absent from captured Release shaders |
| Global/bootstrap terrain | Persistent grid VB/IB, root/global compute and indirect command | Graphics submission requires `!candidate`; retained resources are not a surviving draw |
| Legacy anchored hierarchy | Anchored VB/IB and patch indirect buffer | Draw appears in the `else` of the candidate branch; cannot run in that branch with candidate=true |
| Focused distant Earth | Distant sphere VB/IB | Focused distant presentation requires `!candidate`; unfocused-body draws skip focused instance 0 when handoff is enabled |
| Legacy regional data | Local material/residual/normal/control resources and lookup | Data sampled by existing shaders, not independently rasterized regional geometry |
| NCSM1 regional residuals | Catalog/residual SSBOs, bindings 53/54 | Physical preparation dependencies; no independent regional draw |
| Florida pad | Mesh 3; four authored boxes, 144 indices; expected one instance | `triangle.vert/frag`; vertex colors and direct diffuse illumination |
| Florida foundation | Mesh 4; unit box, 36 indices; expected one instance, fixed authored scale | Same generic pipeline; fragment returns vertex color unlit for mesh != 3 |

Source order: preparation/demand/cull/compact compute and barriers; begin scene subpass; background; generic batches (pad then foundation in sample submission); applicable Solar overlays and unfocused distant bodies; NCSM1 indexed indirect raster; conditional global draw (skipped when candidate); remaining overlays; next subpass; tone map. Render submission sorting/batch metadata and actual instance counts at A/B have not been captured.

NCSM1's selected production pipeline is `productionBillboardOppositeFacePipeline` for NCSM1: patch-list, three control points, back-face culling with CCW front face. It uses the common set-0 descriptor: frame/camera 0, physical input 2, presentation 6, physical vertices 38, and terrain/material resources. The generic facility pipeline reads frame/object data at binding 0 and indexed persistent meshes. There is no GPU-recorded ID evidence in this investigation.

## 4–5. Final depth and color owner beneath the pad

**Not measured at the failing pixels.** The source path predicts NCSM1 for Earth pixels and the generic facility pipeline where visible facility fragments win. That does not establish whether the faint band and dominant green region are the same surface, different triangles, a material transition, or a depth competition in the supplied frame.

The MP4 contains final encoded color, not depth, draw IDs, G-buffer normals, or post-TES vertices. No RenderDoc capture accompanied it; no RenderDoc executable was found on PATH or in its standard installation directory. The scene depth attachment is D32 float/S8, cleared every scene pass and configured with DONT_CARE store; it cannot be recovered retrospectively from the recording.

## 6. Actual TES geometry and near-field factors

No post-TES output or exact failing-pixel primitive was captured. The recorded maximum outer factor grows from 1 to 64; the final record reports inner max 63.993599. These are whole-draw extrema, not factors for the triangle under the facility. They do not prove a pose correlation or a displacement error.

The shared VS subtracts the body camera from prepared FP64 position before FP32 transport. TES interpolates that camera-relative base and adds radial near displacement to interpolated clip position. Its distance weight is `1-smoothstep(40,50,distance)`. Captured TES evaluates the unadapted natural near field; candidate TES applies facility suppression. TCS and VS binaries are identical across the captured and candidate artifacts. No TES factor, range, interface, or implementation was changed here.

## 7. Facility-supported canonical geometry

In the candidate, the support plane's center altitude is 15.134892258793116 m; inner rectangle half-widths are 64/56 m, transition width 128 m. Base grading and near suppression compose into canonical H. The copied candidate binary query confirms its center H and unchanged world root/footing. These are CPU authority results, not actual final TES positions at B. Previous live-buffer contact tests remain historical evidence at their own measured poses; they do not fill this ticket's post-TES/pixel evidence gap.

## 8. Global/bootstrap status after publication

The actual source command-recording branches, beyond the high-level owner count, exclude global graphics submission and focused distant Earth when candidate=true. Legacy anchored graphics submission is mutually exclusive with the candidate branch. Command buffers begin a fresh render pass with cleared scene depth; no separate retained secondary draw path was found.

Thus a surviving global/bootstrap draw is not supported by the inspected code and early post-publication trace. **An exact A/B command-buffer capture has not been obtained**, so bootstrap is not claimed to be closed at the failing-pixel evidentiary standard requested by the ticket. The stale native DLL also differs from the current build; source inspection alone is not binary replay.

## 9. Legacy regional status

The log repeatedly records zero requested/resident/visible legacy local records. The descriptor resources still exist: albedo 28, residual 29, normal 30, lookup 31, control 34. `SampleLocalTerrainMaterial` returns resident=false/weight=0 if its lookup cannot resolve a record; production albedo blending is guarded by resident. BC5 is sampled as payload data but is not applied as an additional production physical normal. This dormant local machinery is distinct from the completed 670-record NCSM1 physical residency system.

No independently rasterized regional mesh or regional overlay draw was found. Descriptor existence is not proof that local payloads influence the final pixel. None of this code was removed.

## 10. Material and overlay decomposition

The captured production fragment binary is identical to the candidate's. It consumes global albedo/elevation/land, derives presentation biome weights, conditionally samples local material, synthesizes detail albedo/roughness/normal/AO, applies planetary lighting, and writes opaque alpha 1. A ray/sphere-derived material coordinate based on represented payload elevation is used for procedural material addressing; it is not another geometry draw or depth write. Whether that addressing contributes to the apparent band is untested.

The incoming geometry normal is mixed with the analytic ocean/land normal and then with material detail. Regional BC5 is not an extra applied slope. There is no separate facility terrain material or facility-shadow-map pass in this draw path. The foundation is unlit while the pad and Earth use different lighting responses; this is a real source-level presentation distinction, **not** a proven cause of the recorded floating effect.

No neutral-material A/B was performed at the exact B pose. No production material change is recommended.

## 11. Depth-state comparison

| State from source | Production NCSM1 | Pad/foundation |
|---|---|---|
| Depth test / write | enabled / enabled | enabled / enabled |
| Compare | GREATER (reversed Z) | GREATER (reversed Z) |
| Polygon depth bias | disabled, zero-initialized | disabled, zero-initialized |
| Color channels | RGBA enabled | RGBA enabled |
| Blending | disabled | disabled |
| Topology | three-control-point patch list | triangle list |
| Pass | scene subpass 0, after facility batches | scene subpass 0, before Earth |

Tone mapping is subpass 1 and consumes scene HDR color. The legacy anchored pipeline's GREATER_OR_EQUAL/stencil state is not the selected NCSM1 depth state. The production near-clip policy clamps 2% of surface altitude to [0.05 m, 1,000,000 m]; exact near clip at A/B is unavailable. No nearly coincident-depth classification is justified without pixel depth and event history.

## 12. Capture / primitive-ID findings

| Required capture | Result |
|---|---|
| Normal final frame | Original video and unmodified extracted A/B/nearby-C images available; camera state absent |
| NCSM1 geometry only plus pad | Not captured at exact B |
| Final depth visualization/readback | Not captured |
| Unique contributor/object/primitive ID | Not captured |
| Final tessellated wireframe | Not captured |
| Neutral unlit Earth with unchanged geometry/depth | Not captured |

Existing fragment diagnostics offer albedo, normals, depth shading, and material/address coloring, but material-address coloring is not contributor/primitive ID and fragment depth shading is not full pixel history. They cannot be retroactively applied to an MP4. No approximate pose was substituted for the requested exact production capture. The runtime prerequisite failed before a valid supported-candidate isolation series could be claimed.

## 13. Causal classification

Primary proven finding: **V7 — stale deployed runtime / missing tested physical-support revision**. Proven by log-matched TES hash, SPIR-V implementation, absence of the managed support type, and binary physical queries reproducing the log's old H.

The narrower question, “what exact surface is the player's eye seeing as ground in B?”, remains **unresolved**. V1 duplicate owner, V2 final-only TES mismatch, V3 material illusion, V4 z-fighting, V5 facility presentation, and V6 legacy regional representation are not proven. In particular, missing support from both preparation and TES must not be mislabeled as supported-base/unsupported-TES divergence.

## 14. KSA trigger

No additional KSA comparison was necessary for the proven artifact mismatch. No non-trivial NovaCore presentation architecture has been causally classified, and no design is proposed. The ticket explicitly defers broad KSA work until that classification; existing KSA-informed support architecture was not reopened.

## 15. Smallest next action

No production correction class is justified yet. The bounded validation prerequisite is to assemble and fingerprint a complete candidate Release runtime, including managed authority, native DLL, preparation shaders and TES; verify supported center H; then repeat manual acceptance. A native build result alone is insufficient. This investigation did not refresh or replace the user's launch directory.

If the symptom persists with that verified runtime, capture exact camera/simulation state and GPU events at A/B/C, then perform all six isolated views and pixel history/post-TES readback before selecting V1–V6 or recommending an implementation. Stop here for website-side architectural review. Do not bank convergence or change the pad, support grading, TES, materials, depth, or legacy renderer.

## 16–18. Read-only audit, diff check, and final status

Final verification is appended below. The original unbanked candidate is preserved; this report is the only repository-visible addition.

- HEAD unchanged: `7c41bb5d0317dc48290b359727031988145abb09`.
- SHA-256 comparison: all **502 incoming repository files** unchanged, none missing.
- All **42 inspected runtime/build artifacts** unchanged; no launch-directory replacement.
- `git diff --check`: **PASS, exit 0** (existing LF/CRLF notices only).
- Index empty. No staging, commit, push, tag, or banking.
- Final status: **26 modified / 25 untracked**; the sole addition relative to incoming status is this investigation report.

```text
 M README.md
 M docs/CODEX_HANDOFF.md
 M docs/M12D-P2S5G-workload-investigation.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/architecture.md
 M docs/planetary-rendering.md
 M native/NovaCore.Native/CMakeLists.txt
 M native/NovaCore.Native/NovaCoreNative.cpp
 M native/NovaCore.Native/shaders/planetary_height_query.comp
 M native/NovaCore.Native/shaders/planetary_natural_terrain_surface.glsl
 M native/NovaCore.Native/shaders/planetary_physical_authority.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_incoming_prepare.comp
 M native/NovaCore.Native/shaders/production_spherical_billboard_physical.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_prepare.comp
 M samples/NovaCore.Triangle/NovaCore.Triangle.csproj
 M samples/NovaCore.Triangle/Program.cs
 M src/NovaCore.Graphics/FloridaLaunchSite.cs
 M src/NovaCore.Graphics/PlanetaryNaturalTerrainPreparation.cs
 M src/NovaCore.Graphics/PlanetaryPhysicalSurface.cs
 M src/NovaCore.Graphics/RenderObject.cs
 M tests/NovaCore.Graphics.Tests/GpuPhysicalHeightPreparationTests.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/PlanetaryProductionSphericalBillboardRuntimeTests.cs
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tests/NovaCore.Launcher.Tests/Program.cs
 M tools/NovaCore.Launcher/ScenarioCatalog.cs
?? docs/earth-route-convergence.md
?? docs/florida-facility-support.md
?? docs/florida-generation4-seating.md
?? docs/florida-physical-rendered-agreement.md
?? docs/florida-visible-terrain-investigation.md
?? docs/ncsm1-regional-physical-residency.md
?? docs/ncsm1-regional-preparation-deblocking.md
?? docs/ncsm1-regional-residency-ksa-reference.md
?? native/NovaCore.Native/RegionalPhysicalPreparation.inl
?? native/NovaCore.Native/RegionalPhysicalProbe.inl
?? native/NovaCore.Native/RegionalPhysicalResidency.h
?? native/NovaCore.Native/RegionalPhysicalResidency.inl
?? native/NovaCore.Native/RegionalPhysicalResidencyTests.cpp
?? native/NovaCore.Native/shaders/facility_support.glsl
?? native/NovaCore.Native/shaders/regional_physical.glsl
?? native/NovaCore.Native/shaders/regional_physical_demand.comp
?? native/NovaCore.Native/shaders/regional_physical_incoming_demand.comp
?? samples/NovaCore.Triangle/EarthRoutePolicy.cs
?? samples/NovaCore.Triangle/EarthRouteValidation.cs
?? samples/NovaCore.Triangle/SampleOptions.cs
?? src/NovaCore.Core/Surface/FacilitySupportRegion.cs
?? tests/NovaCore.Graphics.Tests/EarthRouteConvergenceTests.cs
?? tests/NovaCore.Graphics.Tests/FacilitySupportTests.cs
?? tests/NovaCore.Graphics.Tests/FloridaFoundationSeatingTests.cs
?? tests/NovaCore.Graphics.Tests/RegionalPhysicalResidencyTests.cs
```
