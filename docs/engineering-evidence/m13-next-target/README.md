# M13 next-target investigation: preparation cadence versus active refinement

2026-09-06. **PARTIAL PERFORMANCE DIAGNOSIS — FOLLOW-UP REQUIRED. Winner: NO WINNER YET.**

Neither candidate has a measured, behavior-preserving recoverable cost established strongly enough to authorize implementation. No production optimization remains. M13.1 is still the accepted production authority; no subsequent milestone is named or banked.

## Baseline and method

Initial branch `main` was clean. HEAD, `origin/main`, and annotated tag `m13.1-ncsm1-tes-hotpath` resolved to `fade1384c1c7df93d954e7223b1cc8f17db17f98`. GPU: AMD Radeon RX 6800 XT, driver `32.0.21045.5002`. Native client: 3440 x 1440. `baseline.json` records the original native/managed identities and all 49 deployed shader hashes. Baseline factor-1 and orbital measurements were collected before changing production source: 7.01244 and 0.85128 ms terrain medians respectively.

The fixed poses reuse the M13.1 route, camera identities, J2000 time, warm-up, and last 100 frame-aligned samples. Statistics use the existing nearest-rank median/P95 methodology. `measurements.json` retains exact arguments, camera provenance, deployment/variant fingerprints, sample boundaries, counters and numerical summaries. Its runner pose letters retain the earlier campaign mapping: old B/C/D/E/A correspond to this ticket's A/B/C/D/E. Do not compare an old-letter D probe with this ticket's grazing D.

Canonical runs use the SDK Khronos validation layer with process-local empty implicit-layer discovery and fatal validation errors. OBS was running as a separate process during the investigation; its hook was excluded from canonical child processes. GPU clocks and unrelated GPU workload were not controlled. Terrain medians reproduce M13.1 closely; small paired differences and total-frame tails must not be treated as guaranteed performance gains.

## Fixed poses

All rows contain 100 samples. Times are milliseconds; counters are medians.

| Pose | Terrain median / P95 | Total GPU median | Compacted triangles / TCS patches | TES invocations | Clipping input / output | Fragment invocations |
|---|---:|---:|---:|---:|---:|---:|
| A: factor 1 | 7.01056 / 7.10904 | 10.53715 | 360,950 | 1,082,850 | 142,661 / 142,661 | 2,591,685 |
| B: Florida | 8.87212 / 8.95232 | 12.70648 | 294,748 | 884,244 | 119,650 / 119,646 | 3,984,341 |
| C: active refinement | 25.98244 / 26.13472 | 29.62245 | 2,060 | 2,779,257 | 953,916 / 946,137 | 4,953,599 |
| D: grazing | 15.50216 / 15.59088 | 19.17053 | 378,919 | 3,470,011 | 245,743 / 235,573 | 2,472,101 |
| E: orbital | 0.85772 / 1.22556 | 1.31035 | 15,376 | 46,128 | 12,894 / 12,894 | 35,913 |

The active-minus-factor-1 terrain delta is **18.97188 ms**. Compacted indices equal three times the reported compacted triangle count: active 6,180 versus factor-1 1,082,850. These are pre-tessellation indices, not final rasterized triangles. TCS query values count patches, not individual control-point shader invocations.

## Candidate A: what triggers preparation

`PlanetaryProductionBillboardPupil.Resolve` retains the pupil below the snap threshold. A meaningful integer offset or a rebase changes canonical sample directions in the affected lattice region. `RegionalPhysicalResidency.inl` gates physical data readiness. `RegionalPhysicalPreparation.inl` freezes a CURRENT target, dispatches bounded slices into scratch, then publishes only after completion and the fence at an atomic frame boundary. The old published pupil remains the sole CURRENT physical owner while this happens. INCOMING topology-generation preparation has separate state and is not the recurring cost below.

`production_spherical_billboard_prepare.comp` already computes the old and new canonical direction, and **copies the previous physical vertex without reevaluating H when they are equal**. A complete slice is not a full H recomputation. Changed vertices evaluate geographic + macro/meso base height and facility support, with base normals from neighboring physical samples. TES owns only the near contribution. Camera-relative projection/precision work is not an independent physical-H input. Immutable regional asset/generation changes require full generation replacement, rather than patching published data.

### Cadence controls

Each window observes 160 submitted frames, scripted phases 80..239. Florida uses L17, 712,106 prepared vertices, physical generation 4, CURRENT topology generation 1, context epoch 1 and 670 resident regional records. The eye is 136.19158 m above physical terrain at the Florida anchor; this motion control is distinct from the fixed authored Florida camera. The snap boundary is approximately 16.1647 m. Warm-up relocation and incoming startup preparation are excluded. Speed labels mean displacement per scripted frame at a deterministic 60 Hz, not measured wall-clock velocity or an unscripted manual orbit.

| Control | Desired pupil changes | CURRENT triggers / publications | Completed GPU slices | CURRENT GPU sum | Slice median / P95 | Whole-publication median / P95 | Share of frame GPU |
|---|---:|---:|---:|---:|---:|---:|---:|
| Stationary | 0 | 0 / 0 | 0 | 0 | — | — | 0% |
| Sub-threshold oscillation | 0 | 0 / 0 | 0 | 0 | — | — | 0% |
| Exactly one boundary | 1 | 1 / 1 | 11 | 4.80388 | 0.20980 / 1.87316 | 4.80388 / 4.80388 | 0.27958% |
| Slow, 10 m/s | 1 | 1 / 1 | 11 | 4.74460 | 0.20820 / 1.87072 | 4.74460 / 4.74460 | 0.27240% |
| Ordinary scripted pan, 50 m/s | 8 | 8 / 7 | 78 | 33.14432 | 0.20416 / 1.86940 | 4.73748 / 4.78380 | 1.87579% |
| Fast, 250 m/s | 40 | 15 / 14 | 154 | 64.10648 | 0.20564 / 1.77796 | 4.54900 / 4.93032 | 3.88059% |
| Active-refinement altitude transition, fixed direction | 0 | 0 / 0 | 0 | 0 | — | — | 0% |

Ordinary motion averages **0.207152 ms/frame** CURRENT work; fast motion **0.4006655 ms/frame**. Whole-publication timing sums the staged slices, not wall-clock latency. In-flight work is included in GPU sums but excluded from completed-publication percentiles. The normal and fast windows end with one pending target. Fast movement coalesces desired pupils while a frozen target completes; this is not forty separate publications. No incoming preparation slices occur in these measured windows.

For each nonzero Florida trigger, the logged cause is **east lattice offset changed**: one/slow/normal/fast counts 1/1/8/15. Basis, north offset, transition settings and metadata change counts are all zero. Physical generation, context and resident count remain fixed. Stationary and sub-threshold controls establish no accidental every-frame cadence. Regional/support samples change because geographic sample positions move, not because assets or support configuration mutate.

### Physical deltas

One Florida boundary changes 19,805 of 712,106 positions (**2.781187%**); **692,301 vertices / 97.218813%** preserve all 48 meaningful physical bytes at the same index. Height changes at 19,803 vertices and normal bytes at 15,435. Maximum position change is 16.2941024547 m; maximum stored height change is 3.35758376326 m. Seven ordinary-motion publications retain the same changed fraction.

The component probe records the actual regional residual and `CandidateBaseHeightD - CandidateNaturalBaseHeightD` (facility base-support contribution) as doubles in otherwise unused reserved bytes. Regional contribution changes at 19,805 vertices, maximum 9.97845366461 m. Support contribution changes at 16,527 vertices, maximum 9.98272298973 m. The published state has 419,712 nonzero regional and 22,501 nonzero support samples. These deltas are not additive height errors: support grading can compensate natural terrain. The meaningful 48-byte physical FNV fingerprints exactly match the ordinary one-boundary run, old `12758232823253971471`, new `16166837062827835182`.

Cross-index lookup finds 699,650 fully identical physical outputs after that boundary: an additional 7,349 vertices, **1.032009% of the entire buffer**, beyond existing same-index reuse. This is a possible overlap to investigate, not proof of safely reusable canonical inputs. The diagnostic lookup is keyed by exact body-position output; a future geographic-input key must prove H/normal dependencies, precision, lookup cost and publication lifetime. No cross-index production cache was implemented.

The fast run disables CPU readback hashing; its physical-delta distribution is **not measured**. Two earlier fast attempts with intrusive CPU readback timed out, including one before a diagnostic camera reset was corrected. Those attempts are excluded, not counted as production failures or passes. Their precise timeout cause was not established. The completed unmodified-compute fast run supplies cadence/timing only. One component-run metadata field was shadowed by a runner variable; its per-run common shader-hash dictionary is unavailable. The issue is corrected in the retained runner, and the source recipe, physical fingerprints and global before/after deployment hashes remain available.

### Outside-Florida readiness confound

At the factor-1 geographic control outside the resident regional footprint, a single boundary followed by a stop publishes once (4.44844 ms over 11 slices). Sustained snap-rate movement instead repeatedly completes approximately 22-frame regional demand jobs while CURRENT preparation/publication remains zero in the 160-frame windows. Source order in `RegionalPhysicalResidency.inl` explains this: when the frozen demand becomes ready, a newer live pupil can replace it before preparation consumes that completed target. The last prepared pupil remains authoritative.

This is **observed preparation deferral**, not an optimization or evidence that moving terrain needs no refresh. A bounded follow-up should measure completed-ready target handoff and geometric age under sustained motion before altering this ownership boundary. The observation does not prove indefinite starvation or quantify a recoverable performance cost. The earlier L1 `cadence-*` motion controls have approximately 582 km snap spacing and are retained as secondary controls only; they are not ordinary near-surface pan evidence. The altitude-only `cadence-transition` control is L17 and correctly isolates near refinement without CURRENT preparation.

### Rejected copy-predicate probe

An isolated compute variant checked unchanged lattice basis/identity and zero transition weight before reconstructing both FP64 directions. This targets existing same-index copy eligibility, not the closed TES geographic-address path. Two normal-motion A/B pairs measured total CURRENT GPU work over 160 frames:

| Pair | Baseline | Probe | Baseline minus probe |
|---|---:|---:|---:|
| 1 | 32.85440 | 33.47916 | -0.62476 |
| 2 | 32.94704 | 32.46640 | +0.48064 |

There is no consistent gain: combined probe cost is 0.14412 ms higher across 320 frames. A capture pair at native frame 250 has identical CURRENT generation 1, pupil 3, physical generation 4 and all **45,574,784 prepared bytes**, SHA-256 `e01108c9aacb35bdc5e76f6a9803e9e318823ff5d348cf4c970ba205c9542f33`.

However depth differs in **4 bytes**, HDR in **6**, and final image in **3**. Strict raster parity therefore did not pass. The size/location/numerical cause of those pixel differences was not resolved; they must not be called equivalent or attributed to harmless nondeterminism. No baseline-baseline raster control was collected. `copy-parity.json` retains full hashes and identities. The probe is rejected and absent from production. The capture-only preparation totals (4.56100 / 4.79616 ms) are not substituted for the paired motion timing.

## Candidate B: active refinement decomposition

Active refinement has 2.567 times the TES invocations and 1.911 times the reported fragment invocations of factor 1, despite far fewer selected base patches. Active maximum outer factor is 64; inner 63.993599. Counts by maximum-factor bins `<=1, <=2, <=4, <=8, <=16, <=32, >32` are `97,120,157,255,344,372,715`. Factor 1 has all 360,950 patches in the first bin. Grazing has `377285,138,165,234,270,240,587`.

CPU submit median is 0.0896 ms active versus 0.0703 ms factor 1. GPU cull/compact is 2.50248 versus 2.69148 ms. Fence wait is 29.245 versus 10.3152 ms: waiting for the measured GPU work is not a second additive 19 ms cost. Stationary CURRENT physical preparation is zero in both measured fixed windows. TCS execution time is not separately timestamped within the draw.

The following isolated, explicitly behavior-changing ablations locate cost; **none is a production proposal**. Every row uses the same fixed camera and 100-sample methodology.

| Diagnostic | Factor-1 terrain ms | Active terrain ms | Interpretation |
|---|---:|---:|---|
| Production shaders | 7.01056 | 25.98244 | Baseline |
| Zero near-field height/gradient | 6.96476 | 16.38044 | 9.602 ms active sensitivity to physical near evaluation and downstream effects; violates H |
| Remove normal reconstruction | 6.73280 | 23.93800 | 2.04444 ms sensitivity; changes shading and compiler use of gradients |
| Constant fragment output | 1.72304 | 7.18656 | Also permits dead-code elimination of unused TES outputs; not pure material timing |
| Cheap fragment consuming every interface input | 2.38504 | 14.24624 | Keeps interface consumers; 11.73620 ms active material/lighting-path sensitivity, with changed shading/compiler scheduling |
| Force factor 1 in TCS | 7.00716 | 6.43020 | Removes physical refinement, changes TES to 6,180 active invocations; violates 50 m contract |

These differences overlap and must not be summed. The cheap-fragment control leaves a larger active/factor-1 gap (11.86120 ms) than the constant-fragment control and demonstrates why the latter overstates a material-only removal. Compared with the production delta, it removes 7.11068 ms of the active-minus-factor-1 difference. It does not establish that this work can be eliminated while preserving appearance. No instruction-level occupancy, texture-cache, helper-quad or register-spill attribution was measured. Required versus redundant physical/material work remains unresolved.

### Screen-space usefulness and measurement limits

At frame 150, projection of the **prepared base triangles** finds 1,659 of 2,060 active patches completely clipped by the projection test; 401 intersect the view and together cover 4,953,600 projected pixel area. None of these visible base triangles is below one pixel. All 401 have the same negative winding under this diagnostic's coordinate convention; that sign alone does not mean back-facing. Factor 1 has 110,340 base triangles below one pixel (30.57% of selected base patches), and grazing has 216,629 (57.17%). These are not final post-TES sub-pixel rates.

Active query clipping input/output differ by 7,779 (~0.8155%). Active TES per **reported fragment invocation** is ~0.5611 and clipping output per invocation ~0.1910; factor-1 values are ~0.4178 and ~0.0550. These are not per-unique-visible-pixel metrics. The [Vulkan query specification](https://docs.vulkan.org/spec/latest/chapters/queries.html) makes pipeline-statistic counting distinct from a deterministic generated-primitives query. Clipping counters and fragment invocation counts cannot recover the exact final generated primitive list, unique covered pixels or helper lanes.

Exact final-TES generated primitive count, sub-pixel rate, off-screen/back-facing/horizon attribution, primitives producing no fragments and refinement outside the required physical footprint were **not established**. Selected-base clipping is a useful next measurement lead, not proof those patches may be discarded: displaced geometry and conservative 50 m coverage bounds must be considered. The diagnostic has not proven a safe tighter culling rule or a redundant final physical evaluation.

## KSA responsibility comparison

See [ksa-comparison.md](ksa-comparison.md) for current source responsibility boundaries, local SHA-256 identities and eight directly linked official developer-history findings for both candidates. Current KSA prepares complete selected mesh vertex data per rendered frame; persistent buffers and stable tangent sampling are reused, but no equivalent geographic-key physical-output cache was found. The June 2025 history described less frequent preparation as a possibility, not a shipped guarantee. NovaCore already has the stable overlap mapping described in KSA revision 5156 and additionally stops stationary refresh/copies unchanged physical vertices.

For B, KSA has conservative GPU culling before refinement, shared skew-aware edge factors and a near-range return. NovaCore already follows those responsibilities. KSA's material/register work is a useful direction for profiling, not permission to change NovaCore's analytic FP64 physical field or 50 m range. **ADAPT** existing preparation/raster and stable-sampling boundaries; **ADOPT/ADAPT** conservative visibility and shared-edge ownership; **INTENTIONALLY DIFFER** on authoritative regional readiness, FP64 H and atomic physical publication. Neither candidate is proven to be an obsolete KSA architecture with an already-proven replacement yielding a NovaCore-safe gain.

## Opportunity comparison and next responsibility

| Candidate | Total measured cost | Required cost | Proven avoidable / expected recoverable ms | KSA support | Physical / architectural risk | Validation burden | Confidence |
|---|---|---|---|---|---|---|---|
| A: CURRENT preparation/publication | 0.207 ms/frame ordinary, 0.401 fast; ~4.5–4.8 ms per complete publication | Changed physical inputs and ready atomic publication required; already copies 97.2% in ordinary control | None established / unknown; copy-predicate probe did not win | Stable sample overlap; no shipped equivalent cross-index cache proven | High if readiness, precision or reuse identity is weakened | Exact physical/depth, dependency keys, normal neighborhood, owner/publication, motion/rebase and net lookup cost | High on measured cadence; low on recoverable cost |
| B: active refinement | 25.982 ms terrain; +18.972 over factor 1 | Accepted 50 m physical refinement and appearance required; stage costs overlap | None established / unknown; ablations alter outputs | Conservative visibility, material/register lessons; those responsibilities partly already adopted | High for culling/physical changes; moderate for shader scheduling once identical results are proven | Same H/depth/coverage/edge factors/materials, final primitive usefulness, shader occupancy and repeated timing | High on total/delta; low on safely recoverable fraction |

**Winner: NO WINNER YET.** No production implementation or M13.2 title is justified. The next bounded diagnostic recommendation is to measure final active-pose primitive usefulness and material/gradient execution pressure while preserving the production draw, then test one demonstrated redundant operation with exact geometry/depth and appearance gates. Expected recovered milliseconds remain unknown. Separately record the completed-ready regional handoff observation within Candidate A; it is not a third performance winner. Do not implement both candidates or weaken physical readiness to force a faster number.

Before any production recommendation: prove the precise repeated work can be removed, preserve canonical H/normal/support, 50 m coverage, tessellation continuity, one-owner and atomic publication, then obtain repeatable same-pose GPU improvement and exact depth/physical/visual parity. Strict Vulkan and focused regression gates remain mandatory. The current evidence supports a further causal measurement, not a milestone accomplishment statement.

## Validation and restoration

All three temporary production-source edits were saved in `instrumentation.patch`, restored to HEAD, and the patch was checked for reproducible applicability. Native Release, Triangle Release and Graphics Release builds pass. Focused Release checks pass: four headless contracts (surface interface/geographic specialization, Florida support, canonical physical authority, generation-4 integration), plus all eight GPU tests. The pupil runtime test also passed separately before the GPU category rerun. There are zero failures and skips in those selections; the unrelated full suite was not claimed as run. Strict Vulkan remained enabled for successful diagnostic runs and final GPU checks. No manual Florida acceptance was requested or repeated.

`final-validation.log` preserves the bounded final test results and loaded Release native identity. `validation.json` records final source/ref/index checks and runtime/shader fingerprints. All 49 deployed production shaders match the baseline; native Release build and deployed sample/test copies match each other. Native DLL hash can change after relinking without a source change. Production source diff and staged diff are empty; only this evidence package is untracked. `git diff --check` passes. M13.1, `main`, `origin/main` and the tag remain unchanged. No stage, commit, push or tag operation occurred.

## Reproduction and evidence lifecycle

Use a clean reproduction checkout at the baseline revision. Apply **only** `instrumentation.patch` (it contains the complete combined host changes), build native Release and Triangle Release with the repository's normal Windows toolchain, and set `VULKAN_SDK` to the installed SDK. Python standard library suffices. From repository root, run `python docs/engineering-evidence/m13-next-target/reproduce.py fixed`, or explicitly select `cadence`, `decomposition`, `copy-timing`, or `copy-parity`. The last batch returns parity results and automatically removes its approximately 250 MB raw pair; it is not required for ordinary validation. Restore the three instrumented source paths and rebuild afterward.

Individual controls use `run.py`; `--help` lists flags. To reproduce the outside-region deferral: `run.py B normal near-normal --motion normal --level 17 --delta`, with no Florida flag. Motion names without `--speed` use snap-boundaries/frame, not metre/second labels. `analyze_cadence.py` aligns native submitted frames, excludes failed attempts and separates slices from complete publications. `consolidate.py` retains numerical/provenance results and writes a hash inventory without deleting anything. The common parser is the already-retained `../near-surface-performance/analyze.py`.

Retained evidence consists of this report, KSA comparison/identities, baseline, numerical summaries, rejected-probe parity hashes, final validation, reproduction source/patch and disposable-file provenance. Raw capture bytes and verbose successful-run logs are disposable after these summaries are verified. `storage.json` reports exact measured diagnostic-file bytes created, retained and removed, with scope limitations. It excludes routine build products and transient isolated executable copies; it does not claim whole-workspace storage recovery. No old campaign output, asset, cache or Git history is cleaned by this ticket.

Final classification: **PARTIAL PERFORMANCE DIAGNOSIS — FOLLOW-UP REQUIRED**. Stop for Project Control review.
