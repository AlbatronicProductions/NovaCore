# Near-surface geometry cost: diagnosis for implementation review



2026-09-06. This continues the interrupted performance investigation. It is an

evidence package, not an optimized production candidate or a new milestone.

Repository Debt Retirement remains closed.



## Baseline and measurement boundary



HEAD and origin/main: `219ec3c3f4f1db0496e7ef6204d510a1bd337aeb`, branch `main`.

P2S5H tag `m12d-p2s5h-earth-route-convergence` has annotated object

`7c41bb5d0317dc48290b359727031988145abb09` and target

`32ffac50ab5c06518ede24edfb5c531976d4ec99`. These references are unchanged.

The initial full diff contained only the intended temporary performance changes:



```text

 M native/NovaCore.Native/NovaCoreNative.cpp

 M samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs

```



The initial diff check passed. Preliminary logs were summarized in

`preliminary.json` before collecting new evidence. The startup run lacking the

required near-surface projection is excluded; it was not a valid owner workload.



All new runs use RX 6800 XT, driver 32.0.21045.5002, Release, native 3440x1440,

fixed J2000 time, deterministic directional holds, canonical process-local Vulkan

layer isolation with explicit Khronos validation, and the existing sole NCSM1

Earth owner. They run sequentially. No manual camera movement is used.

Other applications are not uninstalled or stopped; these are within-machine

comparisons, not controlled-clock laboratory benchmarks.



Each result uses the last **100 frame-aligned samples** of the 180-frame hold

after the requested publication/LOD is ready. Medians and P95 are nearest-rank

order statistics (ranks 50 and 95 of 100). Warm-up and earlier publication frames

are excluded from this window. Existing periodic regional-demand work can still

affect the compute/total tail; no outliers are removed. Its full-run demand and

incoming-preparation slice statistics are retained separately. They must not be

added to the steady-state median as if they occurred every frame.



`measurements.json` retains exact first/last native camera coordinates, body

orientation, VP matrix, generation, LOD, factor bins, per-run switches, sample

counts, distributions, runtime hashes, shader hashes, and compact publication

provenance. Managed pose log coordinates and the native high/low reconstructed

coordinates are distinct representations; compare native with native.



Pose A uses L0 representative altitude, aimed at the body. B uses L17's

representative altitude (about 136.19158 m), yaw pi/2 and pitch -0.035.

C uses the Florida preset's eye/target and physical candidate, held through the

diagnostic driver. D and E use altitude 10.004 m, yaw pi/2, and pitches -1.0 and

-0.001. `--altitude=10.004` also establishes the correct startup projection;

the driver then sets the selected pose. C's actual preset orientation is in the

pose record, not the unused directional yaw option.



## Fixed-pose results



All times are milliseconds; each row has 100 samples. Total and terrain columns show median / P95.



| Pose | GPU total | Terrain draw | TCS patches | TES invocations | Clip output | Fragment invocations | CPU fence wait | CPU submit |

|---|---|---|---|---|---|---|---|---|

| A | 2.196 / 2.286 | 2.026 / 2.097 | 15376 | 46128 | 12896 | 35913 | 2.1176 | 0.0423 |

| B | 22.248 / 22.546 | 19.399 / 19.656 | 360950 | 1082850 | 142658 | 2591685 | 22.1461 | 0.0501 |

| C | 21.226 / 23.214 | 18.432 / 18.674 | 294748 | 884244 | 119647 | 3984352 | 21.1256 | 0.0711 |

| D | 45.697 / 46.153 | 42.560 / 42.918 | 2060 | 2779259 | 946137 | 4953599 | 45.6378 | 0.0621 |

| E | 32.744 / 34.550 | 29.577 / 29.924 | 378919 | 3470008 | 235575 | 2472103 | 32.7432 | 0.0551 |



CPU fence wait waits for the previous submitted GPU frame and tracks its critical path. It is not an additional GPU cost. Submit stays about 0.04-0.07 ms: no evidence for a submission/batching optimization. Detailed terrain timing is the existing candidate-draw timestamp span; it excludes compute and does not separately time hardware TCS versus TES.



### Selection and screen-space efficiency



| Pose | Post-horizon candidates | Screen rejected | Selected patches | Distinct controls | Base clipped empty | Base area <1 px | Base area <0.25 px | Summed base area px |

|---|---|---|---|---|---|---|---|---|

| A | 15376 | 0 | 15376 | 7793 | 0 | 3703 (24.08%) | 962 | 37830.22 |

| B | 1184156 | 823206 | 360950 | 181640 | 0 | 110340 (30.57%) | 36360 | 2915882.51 |

| C | 1184192 | 889444 | 294748 | 148371 | 4 | 87163 (29.57%) | 25352 | 4432272.52 |

| D | 1183855 | 1181795 | 2060 | 1095 | 1659 | 0 (0.00%) | 0 | 4953600.00 |

| E | 1183855 | 804936 | 378919 | 190708 | 797 | 216629 (57.17%) | 102977 | 2820905.35 |



The distinct-control count is the compacted index set, not measured VS invocation count; control-point references and logical TCS invocations are three times selected patches. The CPU inspection is one frame (150, generation 1) per pose, not 100 independently sampled area measurements.



| Pose | Inner bins <=1,2,4,8,16,32,64 | Post-horizon near-envelope patches | Clip input query | TES / fragment invocation | Clip output / fragment invocation | Base winding + / - |

|---|---|---|---|---|---|---|

| A | (15376,0,0,0,0,0,0) | 0 | 12895 | 1.2844 | 0.3591 | 2440 / 12936 |

| B | (360950,0,0,0,0,0,0) | 0 | 142667 | 0.4178 | 0.0550 | 136159 / 224791 |

| C | (294748,0,0,0,0,0,0) | 0 | 119649 | 0.2219 | 0.0300 | 113781 / 180963 |

| D | (97,120,157,255,344,372,715) | 3874 | 953910 | 0.5611 | 0.1910 | 0 / 401 |

| E | (377285,138,165,234,270,240,587) | 3874 | 245727 | 1.4037 | 0.0953 | 196045 / 182077 |



B has no selected triangle wholly outside the CPU clip volume and no near-envelope candidates. E has 797 empty base projections, 216,629 subpixel base triangles (57.17%), and mixed winding. The tiny/negative/empty groups overlap in ways not retained as a cross-tab; do not sum them into an avoidable percentage. D has 1,659 empty base projections out of 2,060 selected patches, consistent with conservative displacement support, not proof they may be dropped. No suppression probe is justified from these approximate base classifications alone. Final microtriangle area, exact depth-passing unique pixels and overdraw remain unmeasured.



### Diagnostic A/B results



Each row is one 100-sample run; B address bypass was independently repeated. Positive saved time is baseline terrain median minus probe terrain median.



| Probe | Terrain median / P95 | Saved ms | TCS patches | TES invocations | Fragment invocations |

|---|---|---|---|---|---|

| B-counters-off | 19.433 / 19.651 | -0.034 | 360950 | 1082850 | 2591685 |

| B-minimal-tcs | 19.297 / 19.510 | 0.103 | 360950 | 1082850 | 2591685 |

| B-no-address-1 | 7.040 / 7.124 | 12.359 | 360950 | 1082850 | 2591686 |

| B-no-address-repeat | 7.037 / 7.089 | 12.362 | 360950 | 1082850 | 2591685 |

| C-no-address | 8.859 / 8.931 | 9.573 | 294748 | 884244 | 3984348 |

| D-force-one | 6.496 / 6.505 | 36.064 | 2060 | 6180 | 4953600 |

| D-no-address | 26.009 / 26.121 | 16.551 | 2060 | 2779260 | 4953599 |

| D-no-near | 30.736 / 31.160 | 11.824 | 2060 | 2779256 | 4953599 |

| D-no-normal | 41.369 / 41.890 | 1.191 | 2060 | 2779256 | 4953599 |

| D-owners | 34.817 / 35.156 | 7.743 | 2060 | 2779256 | 4953599 |

| E-force-one | 14.344 / 14.525 | 15.232 | 378919 | 1136757 | 2472101 |

| E-no-address | 15.532 / 15.642 | 14.044 | 378919 | 3470003 | 2472098 |

| E-owners | 26.495 / 26.828 | 3.081 | 378919 | 3470005 | 2472099 |



Probe contracts:



- **no-address:** exact source call substitution only. H/position/depth expressions and selected owner are unchanged; actual raster bit parity is still required. Diagnostic address colors change. Factor-1 TCS/TES are exact; amplified invocation counts remain within repeated-run variation.

- **owners:** existing early fragment-color return. H, TES positions and geometric depth remain unchanged; it also bypasses payload-miss discard and material evaluation, so fragment survival is not guaranteed identical generally. Counts at these poses remain effectively unchanged. It is not a production material proposal.

- **force-one:** same selected patches, but tessellator output, physical interpolation, final displacement/depth and microgeometry change. This is only an amplification lower bound, never an acceptable quality reduction.

- **no-near:** skips the near physical field/support contribution at the same tessellation factors. Canonical source H is untouched, but the rendered near-field evaluation, normals and potentially positions/depth are deliberately different. Not an optimization recommendation.

- **no-normal:** replaces reconstructed surface normal with prepared base normal. H, position/depth expressions and selected owner are unchanged; lighting normals change. Not an accepted physical normal correction.

- **minimal-tcs:** substitutes constant edge factor 1 only for B, where all production factors are already 1. Output factor arithmetic and counts match B. This bypass does not remove the whole TCS stage or its fixed patch cost.

- **counters-off:** suppresses detailed atomic bins/classification while keeping normal selection and production maximum-factor atomics. Counter bins cease to report meaningful detail; physical work and queries are unchanged.



### Differential explanation



B's 19.399 ms cannot be attributed to active physical refinement: there are no

near-envelope candidates and all 360,950 patches use factor 1, yet TES executes

1,082,850 times. Address bypass removes 12.362 ms (63.7%). TCS edge-factor bypass

removes only 0.103 ms (0.53%); detailed counters-off is 0.034 ms slower, not an

improvement. Earlier fragment bypass leaves about 16.45 ms, so materials alone do

not explain the cost. The remaining ~7.04 ms after address bypass includes all

other geometry, raster and ordinary fragment work; these intervals are not additive.



D minus B is 23.161 ms terrain. D submits far fewer patches (2,060) but executes

2.779 million TES invocations versus B's 1.083 million, with 1,963 inner-factor

bins above 1 and approximately 4.954 million fragments versus 2.592 million.

At the *same D pose*, factor-1 reduces TES to 6,180 and terrain to 6.496 ms while

fragments stay at approximately 4.954 million: the 36.064 ms within-pose difference

strongly isolates geometric amplification and repeated TES work rather than

increased fragment coverage. The D owners bypass saves 7.743 ms; address bypass

saves 16.551 ms; no-near saves 11.824 ms; normal bypass saves 1.191 ms. These are

marginal probes of interacting workloads, not a pie chart of stage times. Exact

near-field function call counts were not instrumented: zero is established for

A/B/C's inactive envelope, while D/E calls are bounded above by TES invocations.



E minus B is 10.177 ms, despite E having fewer fragment invocations (2.472 million)

and only 4.98% more patches. It has 3.470 million TES invocations, 1,634 patches in

inner-factor bins above 1, extreme projected edge lengths (maximum ~3.26 million

pixels), 116 TCS patches on/behind the camera and 46 within one metre. The exact

screen-space/skew rule matters here. At E's same pose, factor-1 reduces TES to

1.137 million and terrain to 14.344 ms, with essentially unchanged fragment count.

The resulting 15.232 ms amplification delta is larger than the cross-pose delta:

pose changes also affect projection, fragment cost and hardware scheduling.

Address bypass saves 14.044 ms; owners bypass saves 3.081 ms. Increased fragment

load is contradicted by the measurements. There is potential base-geometry waste,

but its safe avoidable fraction and cost have not been proven.



### Opportunity ranking



| Rank | Responsibility | Measured avoidable opportunity | KSA support | Physical / implementation risk |

|---|---|---|---|---|

| 1 — selected | Unconditional diagnostic inverse address in ordinary TES | B 12.36 ms (63.7%); C 9.57 ms (51.9%); D 16.55 ms (38.9%); E 14.04 ms (47.5%) bypass deltas | Expensive evaluation follows its actual consumer; prepared far terrain passes through | Low physical risk by dataflow; moderate interface/diagnostic gating risk; final raster proof required |

| 2 — investigate later | Repeated near physical evaluation under amplification | D no-near 11.82 ms lower bound with a different surface; safely avoidable fraction unknown | KSA bounds detailed displacement to its near region; NovaCore already does so | High physical risk; no accepted replacement or performance benefit claim |

| 3 — investigate later | Tiny/back-facing/off-screen conservative geometry | B 30.57%, E 57.17% base triangles <1 px; removable milliseconds unknown | KSA conservatively culls prepared geometry with displacement margin, uses scale meshes | High coverage/precision risk; no safe rejection/coarsening predicate proven |

| Not selected | Ordinary fragment material work | B ~2.9 ms, D 7.74 ms, E 3.08 ms bypass opportunities, not additive | KSA has material pruning, but this is a different responsibility | Surface presentation risk; does not explain the majority of B |

| Not selected | TCS edge math / detailed counters / CPU submission | ~0.10 ms edge-math delta; counters no improvement; submit ~0.04-0.07 ms | Current edge/cull/indirect boundaries already similar | No compelling benefit; retain accepted architecture |



## Causal conclusion



The primary avoidable responsibility is the **unconditional FP64 relaxed-cube

inverse address calculation in NCSM1 TES**. `ProductionDirectionAddressD` in

`native/NovaCore.Native/shaders/production_cube_surface.glsl` performs up to eight

Newton iterations, with three spherify/normalize evaluations per iteration.

`production_spherical_billboard.tese` calls it once per TES invocation, including

factor-1 terrain beyond the 50 m refinement region.



Its outputs reach `productionUv`, `productionAddress`, and `topologyCoordinate`.

For NCSM1, `productionLayer` always has the anchored bit. Ordinary

`planetary_production.frag` resolves material residency/addressing independently

from `bodyDirection`; the input address and UV fallback is for the non-anchored

branch. Owner-color and seam diagnostics still consume the TES address/topology

outputs. This real diagnostic responsibility must be preserved explicitly.



The isolated `no-address` probe replaces only the inverse-address call with

constant address outputs. It leaves prepared vertices, TCS, factors, physical

near-field evaluation, normal calculation, `gl_Position`, material receiver

direction, publication, residency, and ownership selection untouched. Two B runs

measured terrain medians 7.03992 and 7.03684 ms, against 19.39916 ms baseline.

This is strong evidence for one bounded production proposal. It is **not** a

production-safe diagnostic implementation: owner/seam colors would be wrong in

this probe, so it must never be deployed as the correction.



The measured delta includes compiler scheduling/register/occupancy consequences

of removing that work. It is not an independently timed arithmetic stage and

must not be added to other bypass deltas. No ISA-level instruction attribution

or hardware occupancy measurement is claimed.



## Physical and counter interpretation



No production shader was edited or overwritten. Probe shaders are compiled into

temporary isolated runtimes; deployed shader hashes are checked before and after

each run. This investigation did not lower physical accuracy or the 50 m contract.



For `no-address`, source dataflow proves the position/depth expressions and

canonical H inputs are unchanged. Full post-TES D32/HDR bitwise parity has **not**

been captured; that is an explicit implementation acceptance gate. Existing

zero-owner/overlap/stale and physical parity checks passed, but they are not a

replacement for final raster comparison. Tiny TES/clip/fragment counter variation

across repeated identical runs is retained, not interpreted as changed semantics.



TCS queries count patches; this shader has three control invocations per patch.

TES queries count invocations, not unique vertices. Clipping input/output and

fragment queries are implementation-relative statistics, not exact unique visible

pixel or generated-triangle counts. See the [Khronos counter definitions](https://docs.vulkan.org/refpages/latest/refpages/source/VkQueryPipelineStatisticFlagBits.html).

The clipping input query on this driver is already much smaller than submitted

factor-1 patches; it is not a safe substitute for a tessellator generated-primitive

query. Exact amplified generated-triangle totals and post-depth unique pixel

counts were not obtained. Ratios below explicitly use fragment invocations as a

proxy; no measured overdraw ratio is claimed.



The optional CPU geometry inspection runs once after a completed fence and reads

existing mapped prepared vertices/compacted indices. It mirrors the VS's FP64

camera subtraction, float rotation/projection, clips against the six homogeneous

Vulkan planes, and measures projected polygon area. It is an approximate

**prepared-base** classification, with possible CPU/GPU rounding differences.

It includes both winding signs and overlap. Summed area is not unique coverage.

For D/E it is not final displaced microtriangle area. It changes no GPU buffers.

The main timing matrix is from runs without this CPU inspection.



## KSA responsibility comparison



The installed production shader source and matching decompiled assemblies were

read locally. Assembly SHA-256 identities:



- KSA.dll: `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`

- Planet.Render.Core.dll: `6B9B3C3BD709BF198C6AB3EC6DAAEF7E468F593FB110187869A1D44272B92E3C`

- Planet.Core.dll: `2823B299051B91252453ACB4F1FFF5543B1F5473BC6F42C8D51D4958D3159ECE`



Local references are under `E:/Kitten Space Agency/Content/Core/Shaders/Planet/`

and `E:/NovaCore/build/ksa-residency-reference/`. No KSA implementation is copied

into this package.



| Responsibility | Current KSA evidence | NovaCore decision |

|---|---|---|

| Coarse topology/LOD | `KSA.PlanetMeshCollection.cs:GetLodLevel` chooses a scale mesh by altitude; `KSA.PlanetRenderer.cs` selects it before preparation | ADOPT boundary already present in NCSM1; no proof that KSA eliminates every tiny triangle |

| Preparation | `PlanetRenderer.GenerateMeshData` prepares displaced vertex/normal buffers; current `KSA/Program.cs:4541` calls it per frame | ADAPT preparation/raster separation; retain NovaCore deterministic regional readiness/publication. Do not claim current KSA only recomputes on LOD changes |

| Visibility | `Culling/PlanetFrustumCulling.comp` tests displaced triangle bounding spheres against frustum, planet and opaque ocean, with tessellation displacement tolerance; compacts indices for indirect draw | ADAPT conservative visibility already used; do not import ocean masking or aggressive removal without NovaCore authority proof |

| Refinement | `TessellationFuncs.glsl` uses projected edge size, perspective-skew compensation, factor 1..64, distance fade. `PlanetTessEvaluation.tese` passes the prepared surface beyond the configured 50 m region | ADOPT responsibility separation, not a shorter distance or coarser physical surface |

| Expensive TES work | KSA's far branch returns prepared position/normal/texture coordinates before detailed displacement. No equivalent unconditional eight-iteration inverse relaxed-cube address is present | ADAPT: ordinary NovaCore TES should not compute coordinates only a diagnostic needs |

| Material/precision authority | KSA uses texture-driven displacement/material blending; NovaCore uses canonical generation-4 analytical H plus support | INTENTIONALLY DIFFER; retain H, FP64, normals, support and exact geographic semantics |

| Submission | Compacted indexed indirect terrain draw; GPU compute followed by raster barriers | Already equivalent; no measured CPU draw-call bottleneck justifying rebatching |



KSA still sends factor-1 terrain through TES and uses conservative bounds. It is

not proven to have no low-value geometry, and no matched KSA GPU benchmark was

performed. The supported convergence is consumer-driven expensive work, not a

claim that KSA has this exact inverse-address optimization or that NovaCore should

replace its physical field with KSA textures. KSA's depth prepass method is for

ground clutter; it is not evidence of a terrain depth prepass solution here.



## Targeted official engineering history



The same campaign's official `live-changelog` search for tessellation produced

22 results. Relevant geometry history, paraphrased from the original messages:



| Date/revision | Earlier responsibility / observed issue | Correction and current relevance |

|---|---|---|

| [2025-06-16](https://discord.com/channels/1260011486735241329/1260112103134724146/1384241537331761153) | Height/normal work coupled to rendering | Introduced height/normal preparation and renderable buffers, enabling separation from drawing; the post explicitly said it still ran per frame then (0.3-0.5 ms), not that caching was already implemented |

| [2025-08-20](https://discord.com/channels/1260011486735241329/1260112103134724146/1407745827572682962) | Terrain outside view still expensive, particularly with tessellation; initial culling too aggressive near displaced terrain | GPU terrain frustum culling, corrected with maximum world-displacement tolerance |

| [2025-09-07 / 2323](https://discord.com/channels/1260011486735241329/1260112103134724146/1414342991958511627) | Duplicated terrain/ocean tessellation heuristics | Shared edge-factor logic; current equivalent includes screen-space and perspective-skew handling |

| [2026-08-12 / 5276](https://discord.com/channels/1260011486735241329/1260112103134724146/1537087719317053502) | 220 m refinement tuned before denser base meshes/textures | Reduced to 50 m and retuned fade; added 0.25 km base meshes for Moon/Mars (Earth already had one). NovaCore already uses 50 m: this does not authorize further reduction |

| [2026-08-13 / 5290](https://discord.com/channels/1260011486735241329/1260112103134724146/1537526170218790983) | Texture/refinement transition morphing | Extended first texture scale beyond tessellation range; preserves presentation coherence |



NovaCore does not currently resemble KSA's pre-preparation or pre-culling design:

those boundaries are already adopted. Its unconditional diagnostic address work

does resemble the broader superseded coupling of expensive evaluation to every

raster invocation, but this is an architectural analogy, not proof KSA once had

the same algorithm. No targeted history evidence establishes a ready-made KSA

tiny-triangle retirement rule compatible with NovaCore's physical contract.



## Proposed single production ticket



**NCSM1 TES address-demand isolation.** Remove inverse relaxed-cube address work

from ordinary NCSM1 TES while retaining exact owner/seam diagnostic addressing.

Keep the physical and fragment material-coordinate paths unchanged.



The target is one coherent owner for each coordinate responsibility: physical

receiver direction remains TES authority; ordinary payload/material addressing

remains the fragment resolver; topology/owner visualization requests its address

explicitly. Audit all consumers before selecting a uniform diagnostic gate or a

bounded shader variant. The current TES push-constant stage range does not expose

the fragment diagnostic flag, so merely reading it in TES without adjusting the

interface would be invalid. Do not enlarge per-control-point transport or alter

the P2S5G interface-compaction contract as a shortcut.



Expected opportunity on this machine is the measured 9.6-16.6 ms terrain reduction

across B-E (12.36 ms / 63.7% in B). These are bypass results, not a guaranteed

production speedup. Preserve exact diagnostics and validate the implemented branch

or variant before claiming the same benefit.



Acceptance gates for that future ticket:



1. H, prepared vertices, final post-TES positions/depth, support/contact, normal

   field, material coordinates, sole-owner coverage, residency and publication

   identity unchanged. Paired fixed-pose D32 comparison should be bit-identical;

   any mismatch requires causal explanation before accepting equivalent output.

   Compare HDR/visible output too, including cube seams and Florida near contact.

2. Preserve 50 m refinement, factor distributions, source/selected counts,

   generation/LOD, clipping/fragment behavior. Retain measured repeat-run TES

   accounting variation; do not accept a semantic factor reduction disguised as

   counter tolerance. Zero owner holes/overlap/stale publications.

3. Owner and seam diagnostics must match their original geography and behavior.

   Ordinary NCSM1 shaders must demonstrably skip the inverse-address work.

4. Repeat all five 100-sample poses with paired baseline/candidate ordering at

   native resolution. Proposed minimum review gate: B median and P95 terrain

   draw at least 30% lower, C/D/E each at least 15% lower, no >3% A regression

   beyond repeated baseline variability. Report total GPU and CPU wait separately.

5. Strict canonical Vulkan validation, matching Debug/Release dependencies,

   physical and P2S5G workload regressions, launcher route checks and bounded

   Florida smoke. Review final raster evidence before any banking decision.



No production correction is implemented in this investigation.

The proposal removes a call from an inappropriate consumer. It does not retire
the shared inverse helper or alter its valid physical-preparation/residency uses.

## Instrumentation and restored-runtime validation

The optional clipping query plus one CPU inspection changed terrain medians by
+0.0062 / +0.0126 / +0.0354 / -0.0354 / -0.0692 ms in A/B/C/D/E, respectively
(absolute change below 0.31%). The primary CPU timing matrix excludes these runs.
Detailed counters-off in B changed 19.39916 to 19.43296 ms (+0.17%), with identical
TCS/TES counts. Neither probe materially explains the bottleneck.

After restoring both production files to HEAD and rebuilding Release, B measured
19.40388 ms terrain / 22.24836 ms total versus 19.39916 / 22.24792 ms with added
frame logging. The terrain difference is +0.00472 ms (+0.024%), with matching
pose, selection and TCS/TES counts. This tests added host instrumentation, not the
already-banked timestamps and maximum-factor atomics. CPU per-frame fields are
explicitly null in the restored-host evidence.

Completed validation:

- Instrumented and restored native Release builds: PASS.
- Restored Triangle and Graphics test Release builds: PASS, zero warnings/errors.
- P2S5G surface interface regression: 1 pass, 0 fail, 0 skip.
- P2S5C production spherical billboard GPU runtime: 1 pass, 0 fail, 0 skip.
- Launcher regressions: 15 pass.
- Ordinary deployed Florida 120-frame smoke: exit 0, zero VUIDs; 670 regional
  dependencies ready; physical/normal/cull/compact/TES/indirect readiness true;
  zero missing/overlap/stale owner counts. This is not manual acceptance or
  another fixed-pose performance sample.
- All 49 deployed shaders match the pre-probe hashes. All 24 new runs retain
  100 frame-aligned samples and matched native pose/publication/selection.
- `git apply --check instrumentation.patch`: PASS against restored HEAD; the
  patch was not reapplied. `git diff --check`: PASS.

The initial `--test=workload` filter matched no test; it was corrected to the
two explicit names above. It is not counted as a pass or skip. No complete
Debug/Release Graphics-suite rerun is claimed for diagnostic-only work.

Normal launcher:
`E:/NovaCore/tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`.
The existing Release runtime selected by `CreatePlan` is
`E:/NovaCore/samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe`.
No alternate deployment or fallback build is required. Build and deployed native
DLL SHA-256 both equal
`0D0AA1844067DF9C4566773FD3D50E84CDC29089A3E3CABE745F3419867E4B11`;
restored Triangle DLL SHA-256 is
`2A9B5D865E7D7873D860C2C8CEDB695D64633212C30D9247501D94677D7DEFB4`.
Older executable byte identity is not claimed; current source and deployment
identity are verified. Shader, runtime and asset source remain unchanged.

## Reproduction, retention and file classification

Package budget: **1 MiB**. Continuing purpose: Project Control's immediate
implementation decision and paired validation. It is not a production dependency.
`manifest.json` gives retained-file hashes; `storage.json` records exact temporary
paths, sizes, hashes and cleanup disposition. No production source, permanent
test, manifest, asset/cache or unrelated evidence was removed.

| Files | Ticket classification | Retention reason |
|---|---|---|
| `README.md` | PERMANENT PERFORMANCE INSTRUMENTATION CANDIDATE - report | Causal decision, limits, proposed ticket and gates |
| `measurements.json`, `preliminary.json` | PERMANENT PERFORMANCE INSTRUMENTATION CANDIDATE - evidence | 24 new and seven valid preliminary summaries, poses, counts, provenance |
| `analyze.py`, `run.py` | PERMANENT PERFORMANCE INSTRUMENTATION CANDIDATE | Frame-aligned summaries and isolated reproducible probes |
| `instrumentation.patch` | TEMPORARY DIAGNOSTIC - inactive reproduction source | Exact opt-in host/pose/geometry instrumentation, removed from production |
| `validation.json` | PERMANENT PERFORMANCE INSTRUMENTATION CANDIDATE - evidence | Restored runtime, regressions and Florida smoke |
| `storage.json`, `manifest.json` | PERMANENT PERFORMANCE INSTRUMENTATION CANDIDATE - provenance | Retention budget, identity and reviewed cleanup inventory |
| `__pycache__/analyze.cpython-311.pyc` | TEMPORARY DIAGNOSTIC - disposable generated output | No retention responsibility; deletion was blocked. Untracked and not ignored; excluded from the permanent package and recorded in pending cleanup |

These are review classifications, not permission to bank or permanently install
telemetry. The inactive patch has immediate reproduction value. Permanent tests
stay in their existing source locations; no runtime depends on this package.

Reproduce in a separate checkout at the recorded HEAD. Copy this small package
to the same relative directory there, deliberately adapt local toolchain paths,
and apply the patch only in that reproduction checkout. Configure its normal
native Release CMake build and resolve declared production assets without copying
the asset cache. In a Visual Studio developer shell:

```powershell
git apply --check docs/engineering-evidence/near-surface-performance/instrumentation.patch
git apply docs/engineering-evidence/near-surface-performance/instrumentation.patch
cmake --build build/native-ninja-release --target NovaCore.Native
dotnet build samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Release
python docs/engineering-evidence/near-surface-performance/run.py B normal B-baseline
python docs/engineering-evidence/near-surface-performance/run.py B no-address B-address-probe
python docs/engineering-evidence/near-surface-performance/run.py E normal E-geometry --geometry
```

Use A-E for the matrix and the named probes above. Do not benchmark other GPU
work concurrently. The runner derives the checkout from its own path, creates
and removes an isolated runtime per run, and leaves only compact result JSON in
that checkout's ignored campaign directory. Never deploy a probe shader.
Missing assets/raw data must not count as a pass. Log hashes in measurements are
normalized UTF-8 stdout hashes; cleanup inventory hashes are exact disk bytes.

## Cleanup status and review disposition

Both diagnostic production files were restored after their purpose was complete
and the reproduction patch was preserved. Production source is clean. No staging,
commit, push, tag change or optimization was performed.

**Automatic approval review rejected the reviewed 33-file deletion with
"blocked by policy"; it supplied no more specific reason. No file was deleted
by that attempt, and deletion was not retried through another mechanism.**

The pending inventory contains eight inherited logs, 24 intermediate run JSON
files, and one Python bytecode file: **4,633,924 bytes**. Campaign files are Git
ignored; bytecode is local generated output. No runtime/build/test/asset consumer
references this campaign directory. Retained evidence and target hashes were
verified before the rejected operation. These files still exist and are recorded
as pending retirement, not completed cleanup.

Final `git status --short`:

```text
?? docs/engineering-evidence/near-surface-performance/
```

Expanded status (`--untracked-files=all`) contains exactly:

```text
?? docs/engineering-evidence/near-surface-performance/README.md
?? docs/engineering-evidence/near-surface-performance/__pycache__/analyze.cpython-311.pyc
?? docs/engineering-evidence/near-surface-performance/analyze.py
?? docs/engineering-evidence/near-surface-performance/instrumentation.patch
?? docs/engineering-evidence/near-surface-performance/manifest.json
?? docs/engineering-evidence/near-surface-performance/measurements.json
?? docs/engineering-evidence/near-surface-performance/preliminary.json
?? docs/engineering-evidence/near-surface-performance/run.py
?? docs/engineering-evidence/near-surface-performance/storage.json
?? docs/engineering-evidence/near-surface-performance/validation.json
```

The runner now disables Python bytecode generation before importing the analyzer;
no repository-wide ignore change is necessary for this retained tool. The already
created bytecode remains pending cleanup and must not be banked with the package.

**READY FOR PERFORMANCE IMPLEMENTATION REVIEW**

This means the single optimization proposal has sufficient causal support for
review. It does not claim a production candidate, completed temporary deletion,
final raster parity of a future implementation, or banking approval.


## Storage accounting

Logical bytes, scoped to this investigation (not a whole-workspace allocation audit):

- Diagnostic output created on disk: **1,244,767** bytes (new retained package,
  24 intermediate JSON results, one bytecode file; excludes inherited logs).
- Permanent evidence retained: **672,442** bytes, within the 1 MiB budget.
- Disposable output removed in this continuation: **0 bytes**.
- Disposable output remaining: **4,633,924 bytes**, policy-blocked reviewed inventory.
- Starting inherited raw logs: **4,061,599 bytes**, summarized before new collection.
- New stdout generated and summarized in memory: **13,019,276 normalized UTF-8 bytes**
  (24 diagnostic runs plus the final smoke); no bulk GPU readback/frame archives.

Per-run temporary copies of runtime/shaders were removed by the isolated runner;
its Earth oracle was a read-only hard link. Their cumulative allocation, compiler
intermediates and overwritten build outputs were not metered. They are excluded
from the persistent-output totals rather than assigned invented byte counts.
The zero removed/remaining figures above concern the reviewed persistent cleanup
inventory. This scope distinction is also recorded in `storage.json`.
