# Physical terrain material receiver convergence — unbanked candidate

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Florida manual acceptance remains **FAIL pending retest**. Earth-route convergence
remains **UNBANKED**. The September 5 15:56:02 recording and matching log identify
the current facility-occlusion candidate; this is not stale-runtime evidence.
The bounded production correction replaces the physical terrain material receiver
and gradient domain. It changes no physical terrain, facility, TES or lighting model.

## Cause and replacement

The production fragment already received a complete post-TES terrain point, but
re-intersected its camera ray with a radial shell derived from coarse geographic
payload elevation for procedural material coordinates. At Florida the shell is
below the composed, supported physical terrain. Moving the camera therefore
moves the material sample for a fixed physical receiver. Derivatives, procedural
frequency selection and material bump normals also describe the wrong surface.
This produces a second apparent presentation layer without another depth owner.

The controlled native 3440x1440 baseline records the actual GPU material coordinate
and actual final position. At close frame 479, the separation is 43.477-59.033 m
(median 49.744 m); represented shell height is approximately -4.537 m. Across the
525-frame sequence, 17,641,183 sampled terrain receivers have separations from
5.207 m to 13,666.813 m, including grazing-shell cases. A fixed close receiver
moves approximately 1.934 m in the old material domain when the camera moves
laterally by 1 m. This is a coordinate-ownership failure, not missing physical
residency, facility separation, tessellation loss or another shadow mask.

The physical path now uses the existing final camera-relative surface position:

- FP64 addition to the split body camera restores body-fixed material identity;
- the same small relative position supplies pre-projection derivatives;
- the existing procedural fields, colors, frequencies, material weights, normal
  response, attenuation and lighting consume that corrected domain unchanged.

`surface_material_coordinates.glsl` is the shared production/test coordinate
operation. No extra stage varying, descriptor, physical evaluation, geographic
special case, distance threshold, blur, LOD bias or quality reduction is added.
The obsolete radial-shell receiver is bypassed for complete physical terrain,
including authoritative NCSM1. Its existing global/bootstrap compatibility use
remains isolated to that representation; two material receivers do not compete
within the authoritative physical path.

## KSA source and responsibility comparison

Read-only inspection used shipped GLSL/XML in `E:\Kitten Space Agency` and the
previously decompiled host source whose three installed assembly hashes were
reverified unchanged. `DefaultAssets.xml` registers the terrain shaders and
`PlanetRenderer.BuildShaderStages` selects them for production. The host supplies
the body-fixed transform from the celestial orientation. Original host repository
comments/line numbers are unavailable; these are installed production artifacts,
not a claim of access to KSA's original repository.

| Responsibility | Classification | NovaCore action |
| --- | --- | --- |
| Material receiver owned by prepared surface rather than an unrelated shell | EQUIVALENT WITH NOVACORE AUTHORITY DIFFERENCES | Adapt: consume the actual post-TES physical receiver and preserve FP64 body identity. |
| Gradients derived from continuous surface coordinates before projection/scale selection | DIRECTLY EQUIVALENT | Adopt: use the same camera-relative receiver for the existing gradient/filtering responsibility. |
| Filtered normal/material response | EQUIVALENT WITH NOVACORE AUTHORITY DIFFERENCES | KSA samples authored normal/material textures with explicit gradients. NovaCore's procedural library keeps its existing response but now receives the correct surface gradient domain; no independent residual cause justifies replacing assets or the material model. |
| Near-field displacement | EQUIVALENT WITH NOVACORE AUTHORITY DIFFERENCES | Preserve canonical H and current TES contract. KSA's texture displacement policy is not a replacement physical authority. No TES change is needed for this measured material-domain failure. |
| Prepared terrain reuse/publication/ownership | EQUIVALENT WITH NOVACORE AUTHORITY DIFFERENCES | Retain complete regional readiness, outgoing-owner retention, atomic publication and P3 scheduling; material coordinates no longer invent a separate representation. |
| Facility caster/terrain receiver/direct-light visibility | DIRECTLY EQUIVALENT at responsibility level | Existing analytical facility occlusion remains unchanged. No second mask or light model is introduced. |

Relevant local source locations: `Planet/Planet.vert`, `Planet/PlanetTessEvaluation.tese`,
`Planet/Planet.frag`, and `Common/Planet.glsl` under `Content/Core/Shaders`.
The shader registry and `PlanetRenderer` establish their production use.

KSA passes the prepared surface position through its tessellation stage while its
visual displacement separately changes clip position. NovaCore intentionally
uses its existing final physical post-TES receiver because physical refinement is
part of its established authority. The aligned responsibility is surface-derived
body-fixed material coordinates and gradients, not a literal copy of KSA's varying
semantics. No KSA source, assets, shader constants or runtime dependency were copied. Exact authored KSA launch-site static-mesh caster participation remains unverified; the lighting classification is only a responsibility comparison.

## Validation and limits

The independent Vulkan coordinate test follows fixed receivers through three
body orientations, three radii and four camera distances (1-10,000 m): 540 checked
components, maximum physical reconstruction error 0.183627 mm within derived
transport-rounding bounds. The test calls the actual production GLSL helper.
The prior radial-shell test, which compared two hypothetical owners on one ray,
is retired. Existing facility visibility tests remain intact through a shared
Vulkan test transport helper and still pass all 102 cases. Material-noise value
preservation and material synthesis regressions pass without changing the library.

The completed 525-frame native before/after capture preserves all 103,219,200
recorded D32 values bit-for-bit, with matching camera/draw identities in every
frame. All 17,641,183 sampled terrain receivers now have zero material-to-final
relative-position separation. There are no sampled owner or shadow-visibility
changes across 45 L17 pupils, and all 670 regional records remain loaded. At the
close frame, median footprint changes from 0.14451 m on the wrong shell to
0.04933 m on the actual terrain. No filtering constants were adjusted.

Exact-depth/HDR provenance resolution can choose alternate coplanar triangles;
resolved position/normal records are not claimed bit-identical across all samples.
The largest resolved position difference is 0.2594 mm with zero D32 difference.
Geometry-stage binaries and physical authority remain the separate invariance
gates. The continuous movie covers L17 pupil changes; broader LOD coverage belongs
to the focused live regression.

Complete before/after raster, publication, timing, Vulkan, launcher freshness and
working-tree evidence is retained in `build/ksa-terrain-presentation/REPORT.md`.
Those measurements do not substitute for the required manual camera/orbit/time
acceptance. The geometry and shading models retain their existing finite precision
and sampling limits; no universal no-aliasing claim is made.

## Completed candidate validation

Four retained-frame runs (400 frames each, ABBA) keep the same 904,053 indices,
712,106 vertices, generation 1/pupil 28 and zero incoming preparation. Mean of run
medians: terrain GPU 50.32837 -> 49.53169 ms (-0.79668 ms, -1.58%); total GPU
53.79433 -> 52.97600 ms (-0.81833 ms). CPU post-callback submission medians are
1.96025 -> 1.98040 ms (+0.02015 ms); no production CPU submission logic changes.
The terrain timer includes unchanged TES work; there is no isolated absolute
fragment-only timer. Instrumented capture timings are excluded.

The fresh live Debug candidate passes regional parity across 43 captures,
1,590 checked vertices, L8-L17 and 40 pupils. P3 validates 315 incoming slices,
maximum 65,536 vertices, 37 publications and 268 incomplete slices retaining the
outgoing owner. Enabled Vulkan validation reports only the pre-existing external
memory import VUID 00645 (two occurrences in the main run); no new VUID.

Facility, foundation, route, material, P2S5G interface, production-renderer and
all 15 launcher regressions pass. The foundation source guard was updated to
follow the already-established shared authored geometry declaration; its physical,
camera, time and pupil tests are preserved. Release deployment matches all 53
freshly built shaders; only the production material fragment differs from the
incoming candidate. A 240-frame normal Release launch using the verified launcher
plan confirms the deployed native/managed/fragment fingerprints and active NCSM1
facility path. No launcher change was required.

Recommendation: **READY FOR FLORIDA MANUAL ACCEPTANCE**. Use the normal NovaCore
Launcher, select **Florida Launch Site**, then native desktop resolution,
borderless fullscreen and Normal diagnostics. Florida remains FAIL until this
manual test passes. No staging, commit, push, tag or banking is authorized here.
