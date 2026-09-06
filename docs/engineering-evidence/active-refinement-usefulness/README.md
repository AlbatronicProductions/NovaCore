# Active refinement usefulness and ordinary fragment specialization

Investigation date: 2026-09-06. This package investigates the next target after
M13.1. It does not bank, implement, or assign a new production milestone.

## Baseline and method

`main`, HEAD, `origin/main`, and annotated tag `m13.1-ncsm1-tes-hotpath` resolve to
`fade1384c1c7df93d954e7223b1cc8f17db17f98`. Production was initially clean, with
only the expected untracked `docs/engineering-evidence/m13-next-target/` package.
Its file hashes are preserved in `baseline.json` and checked again at closeout.
GPU: AMD Radeon RX 6800 XT; Windows driver 32.0.21045.5002, Vulkan driverInfo
26.8.1 AMD proprietary shader compiler. All 49 deployed shader hashes match the
M13.1 baseline. Native client resolution is 3440 x 1440.

The pose mapping for **this ticket** is A factor 1, B active refinement, C grazing,
D Florida. `run.py` fixes the camera, L17, J2000, diagnostic time and route using
the earlier campaign's established setup. Timing uses the last 100 aligned query
samples and nearest-rank median/P95. The initially uninstrumented factor-1 median
was 7.00964 ms; the instrumented host without capture shaders measured 7.00548 ms.
Per-frame preparation/publication is not reopened. Warm-up and steady-state costs
remain separate. Clocks and other applications' GPU activity were not controlled;
paired repeats support the measured benefit, not a guaranteed universal frame time.

All canonical child processes use the SDK Khronos validation layer and process-local
empty implicit-layer discovery. No VUID is filtered. `rasterDiagnostic=0x8000`
identifies the directional-query harness; it is **not** the fragment diagnostic
push constant. Captured native contexts explicitly report `diagnostic=0`.

## Measured workload and geometry usefulness

See `results.md` for the four-pose table, final triangle chain, projected-area
distributions, physical uniqueness, parity, repeated timings and opportunity ranking.
`measurements.json` retains the exact run configurations, frame/camera/publication
identities, compiler statistics and compact numerical results.

The production active pose issues about 2.779 million TES invocations and 4.954
million fragment invocations, versus 1.083 million and 2.592 million at factor 1.
Thus active refinement raises both workloads. The active terrain draw is roughly
26 ms versus 7 ms at factor 1. Query clipping counts are not a complete generated
triangle inventory: AMD's normal tessellation/NGG path can reject work before those
counters. They must not be interpreted as unique visible pixels or all generated
vertices. The final triangle capture measures the full post-TES list separately.

The passthrough geometry shader records three final FP32 clip positions, TES
sample identities, and per-primitive fragment counters. CPU homogeneous clipping
uses -w<=x/y<=w and 0<=z<=w; clipped polygon areas use the 3440 x 1440 viewport.
CPU fan-triangle counts are a reconstruction, not a hardware clipper query.
The front-facing sign is established by actual fragment-producing primitives,
not assumed from a sign convention. Early-fragment atomics count non-helper
invocations surviving early tests; the second counter records the normal path
after material discard. Neither counter measures helper invocations. No separate
pre-depth raster counter was collected, so zero-fragment work is not partitioned
exactly among subpixel sampling and depth rejection.

The identity-color control replaces final terrain color with a lossless three-channel
primitive index and an HDR alpha marker. It is explicitly **OUTPUT-ALTERING**.
Final HDR winners provide unique visible terrain pixels and contributing primitive
IDs. Geometry-stage captures are not timing controls: adding a GS changes driver
lowering and increases TES executions to three per generated triangle. Production
TES counts come from unmodified-stage runs. Where available, sampled body-direction
membership maps final contributing corners back to the TES-only baseline; this is
vertex adjacency to visible primitives, not a claim that every such corner has its
own independently visible pixel or that nonadjacent executions are safely removable.

## TES pressure and physical evaluation

The actual production TES interpolates prepared camera-relative base geometry,
reconstructs body direction in FP64, and interpolates base height/normal. Inside
the 40..50 m falloff it evaluates the analytic near field and gradient, including
facility-support suppression and its gradient. It reconstructs physical normals,
adds near displacement in camera-relative transport and emits material inputs.
Regional/base/modifier preparation is already authoritative upstream; TES does
not regenerate the complete regional base field at each refined sample. M13.1
already specializes away the geographic inverse needed only by owner/seam diagnostics.

At the active pose about 1.914 million body directions/physical positions are
bitwise distinct. Almost every active TES execution has nonzero near-field weight.
Repeated same-patch/barycentric evaluations and shared-patch directions exist,
but uniqueness alone does not establish a reusable cache with an exact sample key,
safe synchronization, lower total cost and unchanged deterministic authority.
Most factor-1 duplicates are shared prepared corners outside the expensive near
field. No new preparation probe was made; Candidate A's prior no-win result remains.

`VK_AMD_shader_info` queried the actual pipeline, rather than a standalone GLSL
complexity estimate. VS and TCS are merged by this driver and return the same
statistics; do not count them as independent stage costs. Ordinary TES uses
190 VGPRs, 36 SGPRs, 4,608 LDS bytes and zero reported scratch, with wave32 code.
Its static listing contains 7,667 instructions and 3,094 FP64 mnemonics. `isa-summary.json`
separates scalar/vector, global memory, LDS and reciprocal/root/transcendental
mnemonics. Static listings include all branches and loops, not dynamic execution
frequencies. The driver reports available VGPR=256, SGPR=104 and physical VGPR=1024,
SGPR=2048; these are **not measured occupancy**. No dynamic occupancy, wave
utilization, helper-quad rate, cache hit rate or stage-isolated TES timestamp is claimed.

## Fragment/material decomposition and safe probe boundary

The measured safe lead is **specializing immutable ordinary NCSM1 fragment state**.
One control sets the diagnostic selector to its known ordinary value zero; a
second also fixes the known NCSM1 owner branch. This lets the driver eliminate
inactive diagnostic and bootstrap paths at compilation. All ordinary materials,
texture/procedural evaluation, lighting and facility visibility remain present.

The first control reduces fragment VGPRs 113→71 (wave64 retained); the combined
control uses 85 VGPRs and changes the compiler's choice to wave32. SGPRs change
70→68→53, with zero scratch/LDS in all three. Static fragment instructions change
8,111→4,120→2,955; FP64 mnemonics 2,934→1,003→763. These are inactive-path/code-pressure
reductions, not proof that all removed instructions executed on ordinary fragments.
The combined probe's TES source/SPIR-V is unchanged, and its physical arithmetic,
FP64 instruction count and register allocation stay the same. Pipeline linking
also removes unused interface exports: TES listing 7,667→7,658 instructions, four
moves and five exports removed. Thus the gain is attributed to ordinary-pipeline
specialization, not exclusively to a separately timed fragment stage.

The proposed implementation boundary is the NCSM1 graphics pipeline, following
the existing M13.1 specialization ownership pattern. The isolated probe replaces
constants in its private fragment module; this is **not** permission to hardcode
those values globally in the shared production fragment shader. Bootstrap/non-Earth
consumers and diagnostic contexts must retain their appropriate unspecialized or
diagnostic permutations. Production implementation is left for Project Control review.

The output-changing no-facility-visibility control changes active draw time by
only about 0.053 ms and factor-1 time slightly worsens. It provides no material
facility-visibility saving at these non-Florida poses and is not a safe candidate.
The prior report's near-evaluation removal (~9.6 ms sensitivity), constant fragment
and all-interface cheap fragment controls remain **OUTPUT-ALTERING** evidence.
They locate substantial near-field and material/procedural/lighting cost but
overlap, change compiler scheduling and sometimes interface liveness. They cannot
be added, treated as isolated stage times, or converted into safely recoverable ms.
No independent material-versus-lighting millisecond split is established here.

## Parity and diagnostic limitations

The combined specialization passes exact sampled physical-record, prepared-buffer,
D32 depth, HDR and final-image comparisons in A/B/C/D at native frame 175, generation
1, pupil 1 and physical generation 4. H/gradient/near weight, clip position and
barycentric record sets match after excluding dispatch-order primitive IDs.
Compaction can reorder those IDs and the implementation can repeat TES executions;
raw atomic record order/count is not a deterministic physical identity. Selected
patch counts/factor bins and owner coverage remain unchanged. No 50 m/tessellation,
support, residency, physical authority or geometry implementation is changed.

Early capture attempts exposed diagnostic issues, not production defects: the
TES array begins at byte 32 under std430 because it contains dvec4, and the primitive
counter reset must not overlap the subsequent enable-header write. Invalid sample
analysis and empty primitive records are explicitly excluded from accepted results.
The corrected layout is checked against SPIR-V offsets and finite/unit directions.
An initial GS run also exceeded the original 4-million TES-record cap; the bounded
diagnostic cap is now 16 million. Those failed attempts are recorded in provenance;
their timings or missing geometry are never counted as successful measurements.

## KSA and architectural decision

`ksa-comparison.md` compares current local production source and targeted official
history. KSA independently samples fine displacement per TES invocation and has
conservative pre-refinement culling and shared edge factors; it does not provide
a proven cross-patch physical-output cache. NovaCore already resembles those
corrected boundaries. **ADOPT/ADAPT** immutable feature specialization and measured
compiler-pressure reduction. **INTENTIONALLY DIFFER** on analytic FP64 H, complete
regional readiness, atomic publication and exact physical refinement. KSA's
material truncation and historic range changes are not authorized NovaCore changes.

## Reproduction, restoration and evidence lifecycle

Use a clean reproduction checkout at the baseline. Apply only this package's complete
`instrumentation.patch`, build native Release and Triangle Release with the normal
Windows toolchain, set `VULKAN_SDK`, and use Python with NumPy. `reproduce.py fixed`
runs four ordinary timing controls; `timing` runs three active paired comparisons;
`parity` explicitly opts into four bounded raw pairs; `primitives` opts into the
larger final-triangle diagnostics. Raw captures are analyzed then removed from
isolated temporary runtimes. Do not run the full capture campaign for routine validation.
The common timing parser remains `../near-surface-performance/analyze.py`.

Permanent package budget: 3 MB, for report, numerical/provenance summaries,
reproduction source/patch, compiler summaries and final validation. Driver listings
are reproducible from retained hashes and tooling; bulk readbacks and verbose
per-frame logs are not permanent assets. `storage.json` reports scoped logical
diagnostic bytes, separately identifying failed-attempt accounting estimates.
Routine build outputs and transient runtime copies are excluded; this is not a
whole-workspace storage audit. Prior evidence, assets, caches and Git history are
untouched. `validation.json` and `final-validation.log` record restoration/build,
focused tests, identities, whitespace and final status.

## Specification references

- [AMD shader statistics](https://docs.vulkan.org/refpages/latest/refpages/source/VkShaderStatisticsInfoAMD.html): stage merging and reported resource fields.
- [Shader execution](https://docs.vulkan.org/spec/latest/chapters/shaders.html): helper invocation storage side effects are suppressed.
- [Invariance](https://docs.vulkan.org/spec/latest/appendices/invariance.html): added shader side effects require measured parity, not an assumed invariant.
- [Rasterization](https://docs.vulkan.org/spec/latest/chapters/primsrast.html): fragments, coverage and rasterization are distinct from unique final pixels.

## Closeout validation

Temporary edits to `NovaCoreNative.cpp` and `ProductionBillboardDesktopTraversal.cs`
were saved in the complete reproduction patch, restored to HEAD, and checked for
patch applicability. Native Release, Triangle Release and Graphics Release builds
pass. Four focused headless contracts and all eight GPU tests pass with zero
failures/skips and strict Khronos Vulkan validation. The selected contracts cover
M13.1 geographic specialization/interface, Florida support, canonical physical
geometry, generation-4 integration, and the supported GPU runtime/P3/field surface.
The unrelated complete Graphics suite was not claimed as run.

A restored ordinary Florida 180-frame smoke exits successfully, resolves 670
regional records and reports sole NCSM1 ownership. No manual acceptance was requested.
All 49 production shaders match both native Release output and deployed Triangle
Release. Native build/sample/test DLL copies match one another; relinking changed
the native hash while production source remained identical. The normal Release
launcher still exists and its Florida preset remains unchanged. Exact identities
and compact smoke provenance are in `validation.json`.

`git diff --check` passes; tracked and staged diffs are empty. Both evidence folders
are untracked, and the prior next-target package is byte-exact. M13.1/main/origin/main
are unchanged. No stage, commit, push or tag operation occurred. This task's 242
manifested temporary files were verified by path/type/hash and removed individually;
raw captures were already retired after analysis. No other workspace cleanup occurred.

Final decision: **READY FOR M13.2 IMPLEMENTATION REVIEW**. Review ordinary terrain
shader specialization, with an expected active draw saving around 2.1 ms. The
proposed title and full gates are in `results.md`; no milestone has been assigned
and no production optimization is left behind.
