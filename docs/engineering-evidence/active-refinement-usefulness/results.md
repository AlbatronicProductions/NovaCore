# Numerical results and next-target decision

**READY FOR M13.2 IMPLEMENTATION REVIEW**. No production correction is left and no milestone is assigned.

## Four fixed poses

| Pose | Terrain median / P95 ms | Patches | TES invocations | Clip query input / output | Fragment invocations |
|---|---:|---:|---:|---:|---:|
| A: factor 1 | 7.00548 / 7.10228 | 360,950 | 1,082,850 | 142,655 / 142,655 | 2,591,685 |
| B: active | 25.99472 / 26.13324 | 2,060 | 2,779,255 | 953,914 / 946,137 | 4,953,599 |
| C: grazing | 15.49400 / 15.61068 | 378,919 | 3,470,003 | 245,742 / 235,575 | 2,472,101 |
| D: Florida | 8.87248 / 8.96952 | 294,748 | 884,244 | 119,644 / 119,640 | 3,984,355 |

Every row has 100 aligned samples. Query counts are medians, not a full generated primitive inventory.

## Final geometric chain

| Measurement | A: factor 1 | B: active |
|---|---:|---:|
| Generated post-TES triangles | 360,950 | 3,824,770 |
| Completely clipped inputs | 0 | 2,876,036 |
| Clipped polygons surviving | 360,950 | 948,734 |
| CPU clip fan triangles | 363,067 | 952,311 |
| Front-facing clipped polygons | 224,791 | 948,734 |
| Opposite-facing clipped polygons | 136,159 | 0 |
| Primitives with early-depth-surviving fragments | 130,126 | 937,506 |
| Primitives producing color | 130,126 | 937,506 |
| Primitives producing zero fragments | 230,824 | 2,887,264 |
| Front-facing survivors with no fragments | 94,666 | 11,228 |
| Non-helper early-depth-surviving invocations | 2,591,699 | 4,953,600 |
| Normal-path color output invocations | 2,591,699 | 4,953,600 |
| Final visible primitives | 130,098 | 937,506 |
| Final NCSM1 owner pixels | 2,591,645 | 4,953,600 |
| Distinct directions adjacent to final pixels | 80,987 | 475,774 |
| Baseline TES executions adjacent to final pixels | 483,463 | 693,040 |
| Clipped percentage | 0.000000 | 75.195005 |
| Zero-fragment percentage | 63.949023 | 75.488565 |
| Generated primitives / final owner pixel | 0.139274 | 0.772119 |
| Color output / final owner pixel | 1.000021 | 1.000000 |

A: baseline TES / final owner pixel = **0.417823**; baseline executions adjacent to final visible primitives = **44.6473%**. Distinct sampled directions adjacent to those primitives = **44.5865%**.

B: baseline TES / final owner pixel = **0.561061**; baseline executions adjacent to final visible primitives = **24.9360%**. Distinct sampled directions adjacent to those primitives = **24.8590%**.

Active GS ordinary-color control matches the TES-only baseline in physical records, depth, HDR and image exactly. Both ID controls preserve physical records and depth exactly; they intentionally alter color. Factor-1 face classification has one CPU winding/fragment disagreement; do not call the CPU winding count an exact hardware backface counter. Active has none. Active 7,065 triangles intersect a clipping plane; 2,876,035 reject trivially and one more rejects after polygon clipping. The CPU clip reconstruction and hardware clipper query are distinct.

## Projected area in pixel²

| Population | Count | P10 / P50 / P90 | <0.25 / <0.5 / <1 / <2 percent |
|---|---:|---|---|
| A clip survivors | 360,950 | 0.2469 / 2.3664 / 20.6380 | 10.0734 / 17.6698 / 30.5693 / 45.8886 |
| A final visible primitives | 130,098 | 2.2067 / 11.0400 / 43.6840 | 0.5242 / 1.4743 / 3.9524 / 8.9187 |
| B clip survivors | 948,734 | 3.9376 / 5.0119 / 7.1861 | 0.4278 / 0.8604 / 1.5727 / 3.3844 |
| B final visible primitives | 937,506 | 3.9854 / 5.0282 / 7.1993 | 0.0480 / 0.1893 / 0.6154 / 2.2593 |

Active clip-surviving geometry has median area 5.01 pixel² and only 1.57% below one pixel². The dominant measured noncontribution is outside-view geometry, not excessive visible subpixel refinement. No quality/range reduction follows. A tighter conservative pre-TES bound is an architectural possibility, but no bound, output parity, or saved ms has been proven. Vertex adjacency is not a safe deletion predicate.

## Unique versus duplicated physical work

| Pose | Captured TES executions | Distinct directions/positions | Repeated directions | Excess executions in shared-patch direction groups | Near-field executions |
|---|---:|---:|---:|---:|---:|
| A | 1,082,850 | 181,640 | 901,210 | 901,210 | 0 |
| B | 2,779,226 | 1,913,891 | 865,335 | 92,152 | 2,779,071 |
| C | 3,470,006 | 1,801,715 | 1,668,291 | 1,016,273 | 2,338,423 |
| D | 884,244 | 148,371 | 735,873 | 735,873 | 0 |

Active has 1,997,482 distinct patch/barycentric records and 79,600 directions shared across patches. Approximately 31.1% of executions repeat a direction; about 3.3% are excess executions within direction groups spanning multiple patches. That group count includes same-patch replay as well as cross-patch duplication; it is not an exact pure-edge-only overhead. Repeated patch/barycentric evaluations account for much of the overall duplication. Costs are not proportional to these counts and no safely recoverable milliseconds are assigned. GLSL tessellation provides independent final sample evaluations; a cross-patch cache is a new responsibility, not a free elimination.

## Output-preserving cost probes

| Probe / pose | Baseline terrain ms | Probe ms | Saving ms | Classification |
|---|---:|---:|---:|---|
| fragment-diagnostics, A | 7.00548 | 6.79504 | 0.21044 | OUTPUT-PRESERVING intent; A parity not captured, not independently qualified |
| fragment-diagnostics, B | 25.99472 | 24.65348 | 1.34124 | OUTPUT-PRESERVING raster at B; physical-set comparison not retained |
| fragment-ncsm1, A | 7.00548 | 6.54524 | 0.46024 | OUTPUT-PRESERVING; four-pose exact combined parity |
| fragment-ncsm1, B | 25.99472 | 23.85760 | 2.13712 | OUTPUT-PRESERVING; four-pose exact combined parity |
| no-facility, A | 7.00548 | 7.01180 | -0.00632 | OUTPUT-ALTERING; no safe gain |
| no-facility, B | 25.99472 | 25.94180 | 0.05292 | OUTPUT-ALTERING; no safe gain |
| Combined B repeat 1 | 25.98796 | 23.84880 | 2.13916 | OUTPUT-PRESERVING |
| Combined B repeat 2 | 25.97964 | 23.89064 | 2.08900 | OUTPUT-PRESERVING |
| Combined B repeat 3 | 25.99160 | 23.91232 | 2.07928 | OUTPUT-PRESERVING |
| Combined C | 15.49400 | 13.93112 | 1.56288 | OUTPUT-PRESERVING |
| Combined D | 8.87248 | 8.37456 | 0.49792 | OUTPUT-PRESERVING |

Three active repeats recover **2.07928–2.13916 ms**, median paired saving **2.08900 ms**. These are ordinary draws with no TES/GS/fragment capture shader, not capture timings.

## Exact four-pose preservation

| Pose | Prepared bytes | Physical record set | D32 / HDR / image changed bytes | Identity |
|---|---|---|---|---|
| A | EXACT | EXACT | 0 / 0 / 0 | frame 175, generation 1, pupil 1, physical generation 4 |
| B | EXACT | EXACT | 0 / 0 / 0 | frame 175, generation 1, pupil 1, physical generation 4 |
| C | EXACT | EXACT | 0 / 0 / 0 | frame 175, generation 1, pupil 1, physical generation 4 |
| D | EXACT | EXACT | 0 / 0 / 0 | frame 175, generation 1, pupil 1, physical generation 4 |

## Ranked opportunities

| Rank / responsibility | Measured contribution | Proven avoidable / expected ms | Parity confidence | Risk / KSA support |
|---|---|---|---|---|
| 1. Ordinary NCSM1 fragment specialization | ~26 ms active draw; inactive paths constrain compiled code | 2.079–2.139 ms active paired gain; ~0.46 ms factor 1 | High for four fixed poses; exact physical/depth/image | Low physical risk; bounded pipeline-selection/diagnostic-permutation implementation risk. KSA compiler/register and immutable-feature boundary supports ADAPT |
| 2. Earlier conservative refinement rejection | 75.2% generated triangles fully clipped; 75.1% baseline TES executions not adjacent to final pixels | Not established | No proposed culling rule validated | Higher coverage/crack/precision risk; KSA conservative culling already adopted |
| 3. Shared/replayed final physical samples | ~31.1% repeated directions; ~3.3% excess executions within shared-patch groups | Not established | No cache/output-reuse probe validated | High synchronization/key/authority risk; current KSA independently evaluates final samples |
| 4. Near-field/material ablations | Prior controls show large, overlapping sensitivity | No safe saving established | OUTPUT-ALTERING | Violates physical or appearance contracts; not a milestone target |

## M13.2 implementation-review recommendation

**Exact responsibility:** let the ordinary NCSM1 pipeline compile its known owner and disabled-diagnostic state as constants, preserving diagnostic and other-owner pipeline behavior. Expected active fixed-pose benefit is about 2.1 ms, roughly 8% of terrain draw time, subject to final production implementation validation.

**Proposed title only:** NovaCore M13.2: Specialize ordinary terrain shading.

Preservation gates: unchanged H/gradients, prepared bytes, 50 m field, selected topology/factors, TES clip/body positions, regional/support data, one-owner coverage, exact D32/HDR/image and facility visibility. Keep generic/startup and diagnostic permutations reachable. Do not global-hardcode the isolated probe into the shared fragment module.

Implementation validation: repeat these four parity poses and three active timing pairs against the banked baseline; inspect compiled ordinary and diagnostic permutations; run focused material/geographic-specialization/physical/support/regional/P3 Graphics contracts, strict Vulkan, shader/dependency identity, owner/seam diagnostics and supported launcher routes. Check dynamic pupil/LOD handoff and Florida presentation before banking under the normal milestone acceptance process. This investigation itself requests no manual acceptance.

No production optimization remains. Stop for Project Control review. **READY FOR M13.2 IMPLEMENTATION REVIEW**.
