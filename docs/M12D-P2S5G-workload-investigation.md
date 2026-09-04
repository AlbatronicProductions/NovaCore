# M12D-P2S5G — Surface Workload Efficiency

## Current banked status

**Banked and production-accepted.** The authoritative production commit is
`7bb03b0e635fa3444a4243bbad82758a1770ea60`, with annotated tag
`m12d-p2s5g-surface-workload-efficiency`. NCSM1 / New Earth Renderer and the
P2S5G tessellation interface compaction are accepted production architecture.
Physical/manual Desktop acceptance passed at native 3440×1440.

The deterministic +89 TES invocation difference is an accepted bounded
invocation-accounting consequence (classification C), not increased refinement
or changed tessellation semantics. The measured factors, performance results,
and implementation-specific limits below remain unchanged.

Older Earth/Solar/Florida routes still retain the anchored/global Earth path.
Their divergence is a pending compatibility/ownership decision, not current
production Earth authority. Earth-route ownership/convergence is the next major
architectural responsibility, not further P2S5G optimization; no route change
or renderer retirement is authorized by this record.

## Historical investigation and pre-banking closeout

Everything below this heading is the preserved investigation and closeout
record from before banking. References to an unbanked candidate, P2S5F as the
banked baseline, pending banking authorization, READY TO BANK, or dirty Git
status describe that historical point in time. They are not current status or
new work instructions. In the A/B evidence, "banked" denotes the P2S5F reference
used at the time. Original measurements, exclusions, decisions, and provenance
are retained verbatim below; the current status above supersedes only their
pre-banking status statements.

Date: 2026-09-04. **Implemented and unbanked. Manual Desktop acceptance: PASS.
Closeout recommends READY TO BANK; explicit banking authorization remains pending.**
The TES delta is classification C, limited to deterministic invocation accounting;
see the final closeout section. P2S5F remains the banked
production milestone. This record describes the working-tree candidate, not a
newly banked baseline.

## Repository truth

The initial worktree was clean. HEAD, main, origin/main and the peeled
`m12d-p2s5f-new-earth-renderer` tag resolved to
`950fabb5dccc21461c647134f023f901fb368459`, following production milestone
`f9ff1bdead850492b5e0a98f1ce518be582238f3`.

Inspected current-state, planetary-rendering, handoff and engineering rules;
production managed selection/pupil/publication; native current/incoming resource
work, timestamps, statistics and raster routing; NCSM1 cull, reset, compaction,
VS/TCS/TES and fragment shaders; existing Desktop diagnostics, launcher mapping
and focused regressions. Production routing and NCSM1 assets were not changed.

## Proven cause and correction

More conservative visibility survivors in certain orientations amplified a
redundant per-control-point shader payload. The original TCS exported 45 user
scalars per control point, including body/frame constants repeated by every
vertex and coordinates that TES replaced before fragment consumption.

The candidate exports **13 user scalars per control point**, preserving physical
normal, lighting direction, view vector, physical direction and height. TES reads
body/frame constants directly from the same existing immutable frame buffers.
Unused VS address calculation and transport are removed. The final fragment
interface still supplies the same sixteen locations with matching types and
interpolation qualifiers. Physical calculations, TCS factor arithmetic, TES
refinement and early-out, culling and indexed-indirect draws are unchanged.

This removes 32 scalar exports per control point, or 96 per triangle. At the
high fixed pose that is 34,651,200 fewer scalar exports per frame. The nominal
138,604,800-byte payload equivalent is a structural work measure, **not** a
measurement of external-memory traffic. Hardware scheduling/storage effects
are included in measured draw time; individual VS/TCS/TES timestamps are not
available. The proven correction is at the inter-stage transport boundary.

There is no new cache, buffer, allocation, dispatch, resource lifetime, CPU
preparation, publication transaction or invalidation rule. Frame/body values
are consumed at TES cadence instead of being copied through every control
point. Their authority and synchronization remain the existing frame buffers
and serialized/fence-complete production lifecycle.

## Equivalent fixed-pose comparison

Final comparison: Release; AMD Radeon RX 6800 XT; Vulkan 1.4; native borderless
3440×1440; production NCSM1; physical generation 4; J2000 startup; standard
materials; production broad+narrow screen rejection. The new optional
`NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME=1` uses the normal pause input before the
first simulation advance. It is allowed only for directional/horizon diagnostics
and rejected for full physical traversal. Thus body orientation, lighting and
camera matrices are fixed for this comparison.

Both poses use L17, topology hash `0xBCE444AFFB2D713B`, publication generation 1,
pupil/prepared/cull/TES/raster identity 1, 712,106 vertices and 1,424,208 source
triangles. The native camera is
`(-1036.8510346254334,-2603399.3763885498,5814963.3804244846)` m, approximately
136.191580 m above the reference radius. Pitch is -0.035 radians. The diagnostic
selects its representative L17 altitude; startup `--altitude=10.004` establishes
a near-surface projection. The logged near-plane coefficient is 0.200080127 m.

| Fingerprint | Low orientation | High orientation |
|---|---:|---:|
| Logged yaw, radians | 0.785398163397448 | 1.5707963267949 |
| Source triangles, before = after | 1,424,208 | 1,424,208 |
| Horizon rejected, before = after | 240,052 | 240,052 |
| Post-horizon survivors, before = after | 1,184,156 | 1,184,156 |
| Screen rejected, before = after | 855,498 | 823,206 |
| Compacted triangles / TCS patches, before = after | 328,658 | 360,950 |
| Indirect indices / TES invocations, before = after | 985,974 | 1,082,850 |
| Indexed indirect draws, before = after | 1 | 1 |
| Maximum outer / inner factor, before = after | 1 / 0.9999 | 1 / 0.9999 |
| TES-envelope-active triangles, before = after | 0 | 0 |
| Median fragment invocations, before / after, repeat 2 | 2,591,662 / 2,591,662 | 2,591,685 / 2,591,685 |

Each run completed 180 measured pose frames. Statistics below use submitted
frames 31–179: 149 samples/run, excluding startup. Timestamp and pipeline query
frame IDs match. Percentiles use sorted nearest ranks. Two before and two after
runs were completed per orientation.

| GPU measure | Low before | Low after | High before | High after |
|---|---:|---:|---:|---:|
| Draw median, repeat 1 | 25.24264 ms | 18.44068 ms | 27.80960 ms | 20.59792 ms |
| Draw p95, repeat 1 | 25.41072 ms | 18.64508 ms | 27.97616 ms | 20.77360 ms |
| Draw median, repeat 2 | 25.34872 ms | 18.45660 ms | 27.83096 ms | 20.56756 ms |
| Draw p95, repeat 2 | 25.50452 ms | 18.64752 ms | 27.96804 ms | 20.82388 ms |
| Cull/compact median, repeat 2 | 2.84856 ms | 2.68944 ms | 2.89340 ms | 2.95744 ms |
| Total GPU median, repeat 2 | 28.74251 ms | 21.64069 ms | 31.38706 ms | 24.07145 ms |

Repeat 2 reduces high-pose draw time by 7.26340 ms (26.1%) and low-pose draw time
by 6.89212 ms (27.2%). Total GPU time also falls; this is not a transfer of the
same cost elsewhere. Some orientation dependence remains because the accepted
visible triangle stream still differs.

One topology upload/publication and no replacements occurred during each hold.
Original triplet compaction, reset command fields and generation identity are
unchanged. No compacted-payload hash was added: atomic append ordering is not
claimed identical. Managed/native/fragment/data runtime hashes were checked
unchanged across each comparison; only the intended VS/TCS/TES binaries differ.

CPU preparation is not the changed responsibility. In the original high hold,
CPU medians were 31.321 ms fence wait, 0.317 ms host callback, 2.050 ms
validation/upload and 0.102 ms command recording. Existing zero managed physical
sample/time fields denote the native-preparation route, not zero GPU work.

## Active 50 m refinement check

Separate Release, paused J2000, fixed 10.004 m horizon runs retained the factor
64 maximum and the accepted 50 m range. These are whole-run means over 238 GPU
samples, not the trimmed directional medians above.

| Measure | Low before | Low after | High before | High after |
|---|---:|---:|---:|---:|
| TCS patches | 346,143 | 346,143 | 379,021 | 379,021 |
| Mean TES invocations | 3,285,761.4 | 3,285,827.8 | 3,464,347.7 | 3,464,419.2 |
| Mean fragment invocations | 2,616,955.7 | 2,616,955.6 | 2,617,515.3 | 2,617,512.0 |
| Surface draw | 39.155 ms | 30.990 ms | 41.144 ms | 32.367 ms |
| Total GPU | 42.667 ms | 34.687 ms | 44.682 ms | 35.992 ms |

Near-field TES invocations increase approximately 0.002%, including with time
paused. They are not asserted bit-identical. The final closeout below isolates
this to deterministic invocation accounting with identical tessellation factors.
It cannot explain the gain by reducing refinement work. Source physical/TES
calculations and output responsibilities remain unchanged. The clipping-output
query also changes with shader variants and is retained in raw logs; it is not
used as proof that visible geometry was reduced. No bit-identical image claim
is made. The subsequent user/manual visual acceptance is recorded below as PASS.

## KSA responsibility reference

Read the currently installed KSA `PlanetTessControl.tesc`,
`TessellationFuncs.glsl`, `PlanetTessEvaluation.tese` and
`Culling/PlanetFrustumCulling.comp` under its Planet shader directory.

- **Adopt responsibility:** keep per-triangle cull/compact ahead of indexed
  indirect raster; keep per-edge factors and bounded detailed TES work. Uniform
  camera/planet state is available at its consuming stage rather than requiring
  every value to be a repeated per-control-point attribute.
- **Adapt responsibility:** minimize NovaCore's tessellation payload while
  retaining its physical normal/direction/height and camera-relative inputs.
  TES reads the already-bound current frame/body buffers; no KSA code is copied.
- **Intentionally differ:** retain NovaCore's FP64 physical/camera-relative
  boundary and anchored body-direction/address calculations. A simple TES
  passthrough is not adopted because it does not establish output preservation.

This is a shader-interface decision, not a host cache or synchronization redesign.
KSA host-side lifetime/publication cadence was not established by the shader
inspection and is not claimed as evidence. NovaCore's existing host lifecycle
was inspected and remains unchanged. There is no new decision cache or retained
visibility data whose validity needs to be inferred from KSA.

## Invariants and automated validation

Canonical H(bodyDirection), body-fixed geometry, physical normals and gradients,
collision/clearance, geographic identity and FP64 transport remain unchanged.
No NCSM1 assets, scale selector, pupil behavior, ownership/publication, conservative
visibility, original index triplets, winding, depth state or fragment material
implementation changed. The factor-1 base and 50 m refinement boundary remain.

- Debug and Release native/sample builds pass, with zero sample warnings/errors.
- Existing production spherical-billboard runtime regression: PASS after change;
  canonical height and position deltas remain zero. Existing rare-rebase
  presentation differences remain explicitly separate from physical truth.
- Existing production topology-library regression: PASS.
- NCSM1 regression using the banked artifact-input library: PASS for all 18
  levels. The default in-memory generator run was interrupted when its artifact
  input option was identified. No artifact-output option was used and no topology
  assets were written.
- New P2S5G regression: PASS. It reflects compiled SPIR-V interfaces, verifies
  matching locations/types/width/interpolation across stages, enforces the
  geometry-only 13-scalar TCS output budget and all sixteen fragment inputs,
  checks current-frame/body provenance and absence of unused VS addressing.
  It also checks the diagnostic reversal target/wait correspondence and guards
  fixed-time mode from full physical traversal. It contains no machine timing
  threshold.
- Launcher suite: all 15 PASS. Broad unrelated Graphics regression was not run.

## Native Desktop trajectory and validation

The corrected full Debug native 3440×1440 trajectory completed with **PASS**:
714 frames, 22.838 seconds, all L0–L17 levels, ten sustained L17 snaps, three
out/in reversal cycles, 58 publications, retreat and re-approach, plus the
existing warp phases. Zero-owner, overlap and stale-generation counters are
all zero. Logged height/normal parity maxima are zero. These existing telemetry
fields do not replace the focused physical parity regressions.

The driver initially stalled because ScaleOut requested the representative L15
altitude but awaited settled L16. That diagnostic target now requests L16;
production selection/hysteresis is unchanged. The corrected complete run is
`p2s5g-physical-trajectory-final.log`; the earlier interrupted run is not an
acceptance result. The exact low/high orientations were also exercised through
the fixed-pose and near-horizon runs.

Vulkan validation was enabled for the Debug trajectory. The sole observed VUID
was the pre-existing `VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645`. No device
loss or new Vulkan validation failure was observed. Release timing runs have
validation disabled by build configuration; a validation log category alone
would not enable the layer.

Visual spot checks showed opaque ground and a continuous horizon without a
broad missing band or duplicate surface at the observed moments. They do not
constitute exhaustive manual visual acceptance. The selected surface views were
largely smooth ocean/dark terrain, limiting detailed material-quality assessment.
**Subsequent user/manual Desktop acceptance: PASS; explicit banking authorization
remains pending.** The user's acceptance record is included in the final closeout.
The separate Debug zero-visible publication/re-entry diagnostic also **PASS**:
four publications, off-Earth L14, re-entry with the same generation, preserved
cross-scale preparation, and zero missing-owner, overlap and stale counts.
`p2s5g-zero-visible.log` contains the result; its only VUID is the same known KMT
interoperability warning.

## Exact changed responsibilities

- `production_spherical_billboard.vert`: remove repeated frame/body attributes
  and unused address calculation; retain physical and camera-relative outputs.
- `production_spherical_billboard.tesc`: reduce the user payload from 45 to 13
  scalars; preserve factors, diagnostics, barrier and built-in position.
- `production_spherical_billboard.tese`: consume the same frame/body constants
  directly; preserve the complete fragment interface and physical calculations.
- `ProductionBillboardDesktopTraversal.cs`: optional fixed-time diagnostics and
  corrected L16 reversal target; no production renderer/camera-authority change.
- `PlanetaryBillboardSurfaceWorkloadTests.cs` and test registration: compiled
  interface/workload and diagnostic-contract regressions.
- Current state, planetary rendering and handoff documentation: mark this
  candidate unbanked, describe its responsibility, and correct the production
  traversal command. This report records evidence and remaining gates.

No native C++ lifecycle, managed production runtime, topology artifact, fragment
shader or terrain authority implementation is changed.

## Remaining debt and evidence

Surface cost still varies with accepted survivor volume. Active near refinement
remains expensive (about 31–32 ms mean surface draw in these two 10 m cases).
This ticket does not claim a 60/90 FPS gate, perfect bit-level shader-output
parity, resolved material quality, or finer pupil continuity. The known KMT VUID
and unrelated broad Graphics orbit-line debt remain.

Final measurement logs under ignored `E:\NovaCore\build` are
`p2s5g-frozen-{before,after}-{low,high}-{1,2}.log` and
`p2s5g-frozen-{before,after}-active-{low,high}.log`. The summary JSON and
`p2s5g-summarize.ps1` preserve sample selection and timing statistics. Runtime
logs contain binary/data hashes, viewport, camera matrices, generation and
workload counters. Reproduction uses the actual production scene:

```powershell
$env:NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1'
& build/p2s5g-run.ps1 comparison-low '0.785398163397448'
& build/p2s5g-run.ps1 comparison-high '1.5707963267949'
& build/p2s5g-summarize.ps1
```

Earlier `p2s5g-diagnostic-*` shader probes remain local evidence only. Distant
edge early-out, counter-atomic removal and barrier removal showed no material
benefit. Moving full TES corner calculation into VS regressed both poses. Simple
fragment/TES passthrough probes changed output and were attribution-only. None
is retained. Their marginal timing differences are not additive stage costs.

Initial plain Solar and default-700-km-near-plane exploratory runs were excluded.
An empty inherited horizon-frame value also caused one diagnostic startup to be
rejected before rendering; the final comparison was rerun with clean settings.
The production launcher scene and near-surface projection are explicit above.

Final work is unbanked: no staging, commit, push or tag. `git diff --check` passes.
Final `git status --short`:

```text
 M docs/CODEX_HANDOFF.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/planetary-rendering.md
 M native/NovaCore.Native/shaders/production_spherical_billboard.tesc
 M native/NovaCore.Native/shaders/production_spherical_billboard.tese
 M native/NovaCore.Native/shaders/production_spherical_billboard.vert
 M samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs
 M tests/NovaCore.Graphics.Tests/Program.cs
?? docs/M12D-P2S5G-workload-investigation.md
?? tests/NovaCore.Graphics.Tests/PlanetaryBillboardSurfaceWorkloadTests.cs
```

## Final closeout — manual PASS and banking readiness

The user supplied **physical/manual Desktop acceptance PASS at native 3440×1440**
for this unchanged candidate. They observed no new cracks/discontinuities,
missing geometry, Earth disappearance, depth/ownership overlap, pupil/refinement
flicker, obvious LOD-transition regression, or visible terrain-quality reduction.
The optimization was visually transparent. Their captured runtime record showed
`surfaceMode=2`, `terrainVersion=5`, `material=production-billboard`, NCSM1 topology,
and single-owner Earth publication. This is the authoritative manual gate; it
was not inferred from the automated runs and does not need repeating.

### TES delta: C — deterministic invocation accounting, unchanged refinement decisions

The discrepancy is **causal at the compiled TES pipeline/invocation-accounting
boundary**. It is not correctly classified as A (sampling variation) or B (state
mismatch), although changing compacted ordering adds frame-to-frame variation.
There is no demonstrated increase in the unique tessellated geometry or in the
accepted refinement footprint. No production correction is proposed.

The smallest decisive comparison used the existing high near-field pose: paused
J2000, 10.004 m startup altitude, yaw pi/2, 3440×1440, L17, pupil/publication 1,
379,021 compacted original triplets and 1,137,063 indirect indices. The normal
production A/B rerun again completed 240 frames and reproduced approximately
70 additional TES invocations per frame on average (before 3,464,349.3; after
3,464,419.5). Both ownership diagnostics passed. This rules out closing the
original difference merely because its percentage is small.

A separate native diagnostic copy under ignored `build/p2s5g-closeout-diagnostic`
read existing host-mapped buffers only after the existing completed-frame fence.
Snapshots at submitted frames 31 and 32 established:

- Physical vertex bytes, camera, body presentation, pupil data, and indirect
  arguments matched exactly across before/after; same-frame GPU constants also
  matched exactly. The only inter-frame GPU-constant byte difference was the
  existing terrain-frame counter at byte offset 60, outside the TCS factor inputs.
- All 379,021 original triplets matched as a set. Atomic compaction changed their
  sequence, both within and between runs.
- After matching by original triplet, all inner-factor bits matched across A/B
  and between the two frames. There were zero factor mismatches.

To remove ordering as a confounder, that isolated diagnostic copied the captured
baseline index sequence after frame 31's fence and replayed it from frame 32
without recomputing compaction. It retained the same original triplets, draw
arguments, physical inputs and production shader binaries. This replay is a
local diagnostic control, not a retained visibility cache or production change;
its timings are not production performance evidence.

| Replay pipeline | TES invocations, every frame 32–239 | Samples |
|---|---:|---:|
| Banked VS / TCS / TES | 3,464,311 | 208 |
| P2S5G VS / TCS / TES | 3,464,400 | 208 |
| Banked VS / P2S5G TCS / P2S5G TES | 3,464,400 | 208 |
| Banked VS / banked TCS / P2S5G TES | 3,464,400 | 208 |

The identical-order difference is **89 invocations (0.002569%)**. These are exact
per-frame values, not differing sample-window averages. Matched frame-32 physical,
index, camera, GPU-constant, pupil, presentation, indirect and inner-factor buffers
were byte-identical. Fragment invocations were also identical at 2,617,497. The
hybrids localize the count change to the P2S5G TES module; it does not require the
removed VS address calculation or changes to TCS factor calculation.

Finally, a local-only TCS diagnostic wrote its three outer factors and inner
factor into unused, disjoint capacity at the tail of the existing compacted
buffer. The same instrumented banked VS/TCS was paired with each TES module.
All **379,021 four-factor tuples / 1,516,084 float bit patterns matched exactly**,
with zero mismatches. The factor-buffer SHA-256 on both sides was
`bce88c79ec305aaa66131355e175317a483671735e5c12072140c8e650dfa266`.
The instrumented runs retained the same exact 3,464,311 / 3,464,400 invocation
counts for all 208 replay frames, so instrumentation did not remove the effect.
Triangle mode, fractional-odd spacing, winding and absence of point mode are
unchanged between TES modules.

This establishes the causal distinction: a different compiled TES interface
changes invocation multiplicity/accounting even when the factors and primitive
generation inputs are identical. Vulkan requires at least one TES invocation
per unique generated vertex, rather than exactly one, and identical factors
with identical tessellation modes preserve the generated primitive set. See
[TES execution](https://docs.vulkan.org/spec/latest/chapters/shaders.html#shaders-tessellation-evaluation-execution)
and [tessellation invariance](https://docs.vulkan.org/spec/latest/appendices/invariance.html#invariance-tessellation).
The observations therefore support invocation accounting/re-evaluation, not
additional refinement decisions. The specific proprietary driver cache/scheduling
mechanism was not observed; no particular cache size, wave occupancy or replay
algorithm is claimed. Nor is a bit-identical final-image claim inferred from
query counts. The pre-existing clipping-output query differences remain raw
implementation statistics, not evidence of additional geometric coverage.

### Final validation, retained candidate and recommendation

**No tracked production rendering, diagnostic-driver or test source changed
during closeout.** Only the report and its three existing state/handoff references
were updated. The native readback/replay copy, factor-capture shader and mixed
pipelines were confined to ignored build evidence. Temporary runtime DLL/shader
substitutions were restored to the accepted candidate and their hashes verified.
No NCSM1 assets were regenerated; no subsequent objective was started.

- Focused compiled P2S5G interface/provenance regression: PASS.
- Normal production near-field A/B reruns: PASS, 240 frames each.
- Identical-input replay and TES-module isolation: exact constant counts over
  208 frames per variant; all-factor comparison: zero mismatches.
- Restored, unchanged Debug production candidate horizon check: PASS, 240 frames,
  zero missing-owner/overlap/stale counts, zero reported height/normal parity
  maxima. Vulkan validation was enabled; its only VUID was the known
  `VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645`. No device loss or new
  validation error was observed.
- Existing manual acceptance is retained as PASS; the full physical trajectory
  was not repeated. `git diff --check` passes. The intended ten-path status
  listing above is unchanged; no files are staged, committed, pushed or tagged.

**Recommendation: READY TO BANK, with classification C explicitly disclosed.**
This is an engineering readiness recommendation, not an A/B non-causal closure:
89 additional counted evaluations of unchanged tessellation are a bounded,
measured tradeoff within the already demonstrated net GPU improvement. There
is no evidenced change to the accepted physical/tessellation responsibility
that requires correction or architectural redesign. Forcing matching query
counts would add unjustified work or complexity. The user's explicit banking
authorization remains required; this report does not perform banking.

The original production performance conclusion stands: repeated fixed-pose draw
medians improved 27.2% in the low pose and 26.1% in the high pose, with lower total
GPU time, plus near-field draw improvements from 39.155/41.144 ms to
30.990/32.367 ms. Replay/capture timings are excluded. Remaining debt is accepted
orientation-dependent survivor volume, expensive active near refinement,
implementation-dependent invocation counts, the known KMT interoperability VUID,
and the unrelated broad Graphics orbit-line assertion. No 60/90 FPS claim or
additional presentation/continuity work is included.

Closeout evidence: `build/p2s5g-closeout-{before,after}-high.log`,
`build/p2s5g-closeout-{capture,replay,factors}-{before,after}-high.log`,
`build/p2s5g-closeout-hybrid-{bankVS,bankVSTCS}-high.log`,
`build/p2s5g-closeout-validation-high.log`, and
`build/p2s5g-closeout-interface-tests.log`. Binary captures, isolated sources,
comparison scripts and `comparison.json`, `replay-comparison.json`,
`all-factor-comparison.json` are in `build/p2s5g-closeout-diagnostic`.
