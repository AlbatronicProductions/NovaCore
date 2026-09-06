# Florida physical versus rendered terrain agreement investigation

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Current follow-up: the [regional physical-residency migration](ncsm1-regional-physical-residency.md)
corrects live regional demand and parity. It remains **UNBANKED — BLOCKED by
remaining near-field physical/rendered seating disagreement** (0.692 m sampled
foundation clearance). The earlier observations below are historical evidence;
manual acceptance has not subsequently passed.

2026-09-04. **UNBANKED — MANUAL ACCEPTANCE FAIL — WEBSITE REVIEW REQUIRED.**

**Primary classification: D — regional physical-data mismatch, specifically
availability/publication in live NCSM1 preparation.** At Florida the canonical
CPU query includes the installed regional residual, but the live GPU regional
lookup has no published layers and returns zero. This is not a 14 m coarse
triangle interpolation error. No corrective production implementation was made.

## Scope and provenance

Started at HEAD `7c41bb5d0317dc48290b359727031988145abb09` with the existing
unbanked convergence and foundation work intact. P2S5G remains banked at
`7bb03b0e635fa3444a4243bbad82758a1770ea60`, tag
`m12d-p2s5g-surface-workload-efficiency`. No production/test source was changed
by this investigation. All 14 pre-existing modified/untracked production/test
files were verified byte-identical to their starting hashes. No staging,
commit, push, tag, banking, retirement, pad penetration or offset change occurred.

The supplied `E:/Videos/2026-09-04 20-33-11.mp4` is physical acceptance evidence;
it does not establish cause. Numerical evidence below comes from the exact
published GPU representation, not visual estimation or a substituted height mesh.

Instrumentation exists only under ignored `build/florida-agreement/`:

1. A copy of the candidate native host reads host-coherent mapped physical,
   source-index, compacted-index, indirect-draw and pupil buffers after the frame
   fence and completed pipeline-statistics query, before the next host update.
   It rejects mismatched generation/prepared/raster identities.
2. It intersects the exact site-direction ray with the actual compacted GPU
   triangles and records vertex indices, GPU-prepared body positions, heights,
   integer lattice keys and barycentric coordinates. Source index triplets and
   topology hashes are checked against the immutable NCSM1 files.
3. A second isolated diagnostic writes six exact-anchor component values into
   unused physical-vertex padding. The direction uses explicit FP64 GLSL
   constants. It calls the live production functions through the live descriptor
   set. It does not alter positions, normals, culling, tessellation or draw data.
4. VS/TCS/TES and culling source and runtime SPIR-V remain byte-identical to the
   candidate. The controlled run disables live host keyboard/mouse buttons in
   the copied native host; the existing Florida validation driver supplies
   detach/attach, ascent and return. Debug runs use native 3440x1440 borderless,
   generation 4, terrain-v5, NCSM1, and a J2000 start.

The factor-1 values below reconstruct the plane from actual GPU-prepared
vertices. The unchanged TES emits the same plane when the measured near weight
is zero. They are geometric values before final FP32 clip/raster rounding,
not interpolated fragment `terrainHeight` attributes. A separate mirror of the
VS camera-relative FP32 narrowing changes L17 footprint height by at most
3.961 micrometres in these captures. No per-fragment depth readback is claimed.

## Exact anchor and comparison

- Earth body: **6**; site object: **0x4E435F464C5F5044**.
- Latitude **28.6084 degrees**, longitude **-80.6042 degrees**; unchanged heading.
- Exact normalized body-fixed direction:
  **(0.1433224599406355, 0.4788205718227514, 0.8661348234979923)**.
- Earth radius authority: **6371008.8 m**. Live split-radius reconstruction is
  `6371008.799999997 m`; pupil physical-preparation radius is `6371008.8 m`.
- Canonical physical point:
  **(913111.3604436027, 3050579.1201640856, 5518168.941137452) m** body-fixed.

All heights are metres above that reference sphere at the same direction.
Settled frame 60 gives the same anchor geometry as published frame 2.

| Quantity | Height (m) | Authority |
| -- | --: | -- |
| Canonical generation-4 H | 18.886950220789107 | Physical, including regional and near fields |
| Canonical base at exact anchor | 16.59866301698382 | Physical geographic + macro/meso |
| Live GPU base function at exact anchor | 4.778169726528001 | Live NCSM1 descriptors; regional absent |
| Actual NCSM1 source triangle / factor-1 plane | 4.778170082718134 | Published GPU geometry |
| TES output at this sample | 4.778170082718134 | Factor 1, local displacement weight 0 |
| Foundation lower plane at center | 15.13489225835374 | Structure, unchanged |
| Slab root/origin | 21.97046153575948 | Structure, unchanged |

| Difference | Metres |
| -- | --: |
| Factor-1 rendered minus canonical H | -14.108780138070973 |
| TES rendered minus canonical H | -14.108780138070973 |
| Foundation lower plane minus canonical H | -3.752057962435366 |
| Foundation lower plane minus rendered NCSM1 | +10.356722175635607 |

The solid foundation intersects canonical terrain along its sidewalls. Its
lower plane is not asserted to coincide with H everywhere. The center lower
plane is physically buried by 3.752058 m yet sits 10.356722 m above the live
rendered ground. Slab origin, model pivot and foundation dimensions were not
adjusted during this investigation.

## Actual triangle lineage

Representative controlled frame **2**: NCSM1 **L17**, generation **1**, pupil
frame identity **1**, pupil generation **1**, lattice scale **6291456**;
topology hash **0xBCE444AFFB2D713B**. Source triangle **1399470**, compacted
triangle **317392**. Original winding/index order is retained.
Barycentric weights at Florida are
`(0.6567245602403572, 0.12379942211713185, 0.21947601764251087)`.

| Vertex | NCSM1 rational lattice (X,Y,Z)/denominator | GPU base H (m) | CPU full H at same vertex direction (m) |
| -- | -- | --: | --: |
| 699980 | (-114,134,6291456)/6291456 | 4.777901371528102 | 19.029627764513886 |
| 699837 | (-114,132,6291456)/6291456 | 4.777533264078189 | 18.91380508302759 |
| 699838 | (-112,132,6291456)/6291456 | 4.77933390635729 | 18.430882504385036 |

GPU-derived vertex directions, in the same order:

```text
(0.1433223825759389,  0.4788206676237831, 0.8661347833386388)
(0.14332240742834423, 0.478820388542817,  0.8661349335090055)
(0.1433227210547444,  0.4788203885455509, 0.8661348816105674)
```

Actual GPU-prepared body-fixed positions, metres:

```text
(913108.8454084819, 3050572.9748109225, 5518156.464943131)
(913109.0036907356, 3050571.1966060405, 5518157.421361745)
(913111.0020668621, 3050571.197485642,  5518157.09227569)
```

The shader's pupil frame is captured in JSON, including retained lattice
orientation and offsets; this is not a generic sphere tessellation. All tested
GPU vertex heights match the CPU base with regional contribution omitted to
within **2.026e-12 m**. Interpolated CPU full heights on this same triangle are
18.883878758202414 m, only **0.003071 m** below exact H. That counterfactual is
diagnostic attribution, not a replacement surface or proposed policy.

## Footprint comparison

Nearby points are explicitly defined by
`normalize(R * anchorDirection + East * x + North * y)` in the fixed site ENU
frame. Each row queries CPU H and intersects the GPU triangle at that exact
same direction. Offsets identify center, four footprint corners and four edge
midpoints; they do not relocate the site. Values rounded to six decimals here;
full precision, triangles and component attribution are in the JSON logs.

| East (m) | North (m) | Canonical H | NCSM1/TES height | Render minus H | Foundation bottom minus render |
| --: | --: | --: | --: | --: | --: |
| 0 | 0 | 18.886950 | 4.778170 | -14.108780 | 10.356722 |
| -32 | -24 | 21.720462 | 4.744981 | -16.975480 | 10.390037 |
| 32 | -24 | 19.915678 | 4.802851 | -15.112827 | 10.332167 |
| -32 | 24 | 21.285737 | 4.754736 | -16.531001 | 10.380282 |
| 32 | 24 | 19.544928 | 4.810525 | -14.734404 | 10.324493 |
| -32 | 0 | 21.588543 | 4.749851 | -16.838692 | 10.385122 |
| 32 | 0 | 20.286709 | 4.806673 | -15.480036 | 10.328299 |
| 0 | -24 | 16.884125 | 4.773825 | -12.110300 | 10.361112 |
| 0 | 24 | 17.402873 | 4.782538 | -12.620335 | 10.352400 |

The discrepancy varies with regional terrain across the footprint. It is not a
constant body transform or radius offset. L17 chord sag is below 0.562 micrometres
at these samples; it cannot explain the separation.

## TES, LOD and pupil

All footprint triangles in the controlled near-surface start are outside the
50 m TES footprint: their edge midpoints are at least **163.219 m** from the
camera; the anchor triangle's minimum is **197.275 m**. GPU counters report
maximum outer factor **1**, inner **0.999900**; the factor clamp therefore gives
outer factors **(1,1,1)** for the anchor. TES near weight is **0** at all nine
points and at their triangle corners. No refined surface is active beneath the
pad in this pose. The 50 m contract was not changed.

At the anchor the canonical near field is **+2.288287203805286 m**. TES currently
adds only this near field when in range; it does not reevaluate the missing
regional geographic contribution. Thus even an in-range near evaluation would
not repair the regional mismatch. This is source-level attribution; no active
refined-at-anchor raster result is claimed for the out-of-range captures.

| Controlled frame/state | Level | Publication generation | Pupil frame / pupil generation | Exact-anchor rendered H (m) |
| -- | --: | --: | -- | --: |
| 2, first published | 17 | 1 | 1 / 1 | 4.778170082718134 |
| 60, retained/settled | 17 | 1 | 1 / 1 | 4.778170082718134 |
| 150, retained | 17 | 1 | 1 / 1 | 4.778170082718134 |
| 163, adjacent level | 16 | 2 | 5 / 3 | 4.77817178145051 |
| 175, farther ascent | 12 | 6 | 13 / 5 | 4.778169656172395 |
| 300, returned | 17 | 19 | 26 / 8 | 4.778169927187264 |
| 440, later pupil state | 17 | 19 | 30 / 12 | 4.778169927187264 |

Canonical H is invariant. The adjacent-level anchor change is **+1.699
micrometres**; returned L17 is **-0.156 micrometres** relative to the initial
sample. Same-level later pupil identities also preserve anchor height in these
captures. Maximum height variation over any one of the nine footprint directions
across the captured states is **0.532 mm**. This is not an exhaustive pupil-error
bound, but it excludes a metre-scale representation transition as this failure's
primary cause. Normal post-publication ownership remains exactly one Earth owner.

## Regional-data proof and precise responsible stage

Installed regional pack SHA-256 is unchanged:
`c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.
CPU initialization loads it before creating the site. The nonzero CPU residual
proves that the exact anchor is inside its physical footprint.

| Exact-anchor component | Canonical CPU (m) | Live NCSM1 GPU functions (m) |
| -- | --: | --: |
| Signed global oracle | -3.18103971223142 | -3.1817031283731136 |
| Florida regional residual | 15.001533002687239 | **0** |
| Geographic height after combined sea-level clamp | 11.820493290455819 | **0** |
| Geographic + macro/meso base | 16.59866301698382 | 4.778169726528001 |
| Near field, before distance weighting | 2.288287203805286 | 2.2882872038052877 |
| Full unweighted physical function | 18.886950220789107 | 7.066456930333289 |

The live GPU's full unweighted function is not the raster height: its near field
is inactive in this view. Missing clamped geography (**11.820493 m**) plus the
out-of-range near field (**2.288287 m**), with sub-micrometre base interpolation,
accounts for the **14.108780 m** gap. The signed global lookup differs by
0.663416 mm due to its existing FP32 angular addressing; both negative values
clamp to zero when regional data is absent. That small difference is not the
primary failure and was not corrected here.

The responsible chain is:

1. `NovaCoreNative.cpp:1292` — `QueueLocalRequests` calls only
   `QueueAnchoredLocalRequests`.
2. `NovaCoreNative.cpp:1279` — that function returns without requests when
   `anchoredSurfacePatchCount` is zero. Ordinary NCSM1 Florida has no old anchored
   patches. Captures confirm **requests=0**, **published regional layers=0**,
   while the local pack/lookup is enabled.
3. `local_terrain.glsl:88` — `LocalTerrainElevationResidual` returns zero when
   `ResolveLocalTerrain` cannot find a published residency entry.
4. `planetary_physical_authority.glsl:42` — `CanonicalGeographicHeight` uses
   this residency-dependent residual. `production_spherical_billboard_physical.glsl:4`
   and `production_spherical_billboard_prepare.comp:53` consequently prepare
   the wrong regional base into the **actual physical vertex buffer**.
5. VS consumes that buffer; TES interpolates it and only adds bounded near
   displacement. Neither stage recovers the absent regional base.

This establishes **D**, not C. No policy to seat structures on a changing rendered
triangle was adopted. Nor does the finding justify deeper footing penetration.

## Why the previous parity check passed

`PlanetaryHeightQuery.cpp:127` opens the regional pack and eagerly decodes every
record into dedicated query buffers. `planetary_height_query.comp` samples those
buffers with a CPU-provided FP64 oracle UV. It does not use the live renderer's
binding-31 residency table or streamed residual texture. The verifier is a valid
test of its own physical query path, but was insufficient evidence for live
NCSM1 regional participation in the preceding seating report.

A fresh isolated-verifier query again returns regional **15.001533003031648 m**
and full H **18.886950221117083 m**; CPU H at its reconstructed direction is
18.886950221186638 m. Its point transport difference is **3.278e-9 m** and is
explicitly distinct from the exact-constant live GPU probe. The earlier positive
parity result is preserved as historical evidence, with its scope corrected.

## Recommendation and stop condition

The smallest next responsibility is a bounded **NCSM1 regional physical-data
availability/publication correction**: make geometry preparation consume the
authoritative regional residual before publishing affected vertices, independently
of old anchored-patch/material demand. Physical data changes must invalidate or
reprepare affected physical vertices. The exact implementation needs review;
simply queuing material pages without a physical readiness/invalidation contract
would leave an unresolved authority problem.

Keep the site and foundation unchanged. After that correction, repeat this same
live-descriptor/triangle probe, then measure the remaining near-field and
representation difference under the unchanged 50 m contract before manual
acceptance. Do not infer a visual pass from the counterfactual values above.

No corrective implementation was performed. **Convergence remains unbanked and
Florida manual acceptance remains FAIL. Stop for website-side review.**

## Evidence and validation

Subsequent architectural prerequisite: the
[KSA regional-residency responsibility audit](ncsm1-regional-residency-ksa-reference.md)
inspects current installed production code before corrective implementation.
It preserves the historical measurements and classification above. KSA provides
useful independent data ownership and upload-publication principles, but no
equivalent streamed regional residual hierarchy with NovaCore's completeness gate.

Local numerical artifacts under `build/florida-agreement/`:

- `captures-normal/`: first actual GPU readback run with unchanged shaders.
  Live user input was not suppressed; these captures are supplementary.
- `captures-components/`: controlled 460-frame trajectory; all nine footprint
  samples found in every retained capture; triangle and pupil provenance.
- `captures-exact-components/frame-2.json` and `frame-60.json`: exact FP64 anchor
  component GPU readback, zero residual/requests/published layers in both frames.
- `comparison-captures-components.json`, `analysis-components.log`: full-precision
  footprint/vertex physical comparison, transport mirror and verifier contrast.
- `runtime-components.log`, `runtime-exact-components.log`: generation 4,
  terrain-v5, `surfaceMode=2`, NCSM1, current owner/LOD/publication evidence.
- `verification.log`: preserved production files; unchanged VS/TCS/TES/culling;
  actual GPU vertex attribution; exact regional omission; NCSM1 artifact hash,
  source-index and lattice lineage; one-owner checks all PASS.
- `initial-status.txt`, `initial-hashes.json`, `initial.patch`: starting candidate
  preservation evidence. Diagnostic builds and runs exited successfully.

The known Debug KMT external-memory VUID may appear; no new VUID/device loss was
observed. Diagnostic capture overhead makes these runs unsuitable for performance
acceptance. `git diff --check` passed. Final status is reported with delivery;
only documentation was added/updated outside ignored diagnostic output.
