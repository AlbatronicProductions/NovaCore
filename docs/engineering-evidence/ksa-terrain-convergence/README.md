# KSA terrain responsibility convergence — implementation candidate

2026-09-07. Lead judgment: **PASS**.

Technical classification: **BLOCKED** — automatic scratch deletion was rejected
by execution policy. The architectural candidate passed; required evidence
cleanup has not executed. See the manual cleanup procedure at the end.

This is a surviving, unbanked candidate for Project Control. No commit, staging,
main movement, tag or push occurred. It does **not** finish the 8.33 ms M13 target
or claim hitch-free operation. The remaining timing limits are recorded below.

## Baseline and scope

HEAD, main and origin/main remain `4accf92fd080c16cc8656080aa69def3fa65db53`.
M13.2 is `m13.2-ordinary-terrain-shading` at that commit; its annotated tag object
is `add1c94658c1de869124c75606ce0a084c77ba25`. M13.1 remains
`fade1384c1c7df93d954e7223b1cc8f17db17f98`, tag `m13.1-ncsm1-tes-hotpath`,
annotated object `b534d149b74b0ca854e4fb09267fc84321cc52ff`.

Created `codex/m13-ksa-terrain-convergence-implementation` from banked main.
Initially there were no tracked/staged changes. Five pre-existing untracked
evidence directories were preserved: conservative visibility, exact local
encoding, KSA architecture, local physical-domain proof and post-M13.2 target
investigation. Their old-contract conclusions remain historical evidence, not
vetoes over the newly authorized responsibility migration.

The prior deployment verification passed before instrumentation: 49 shaders
and four checked binaries per Debug/Release configuration. Final deployments
are the new candidate, identified in [deployment.json](deployment.json).

## WHAT — migrated responsibility

Move full terrain relief evaluation from amplified TES samples to shared,
published terrain vertices. The previous render authority combined prepared
base H with a camera-weighted analytic near H in TES. The candidate render
authority is the published, camera-relative, piecewise linear terrain surface.
Gameplay continues to query continuous full physical H independently.

This is one render/preparation boundary migration. It does not replace NCSM1,
tessellation, physical datasets, support grading, facility light visibility,
simulation precision, material shading, residency or publication ownership.

## HOW / WORK A — current KSA reference

Classification: **ADAPT**. Exact local KSA source, not old engineering history,
supplies the implementation boundary. Installed assembly version
`2026.9.7.5402`, source revision `487c3f340de24c6a81037120b6d1129c045c5400`,
assembly SHA-256 `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
[Source provenance](ksa-provenance.json) records current paths and hashes.

| Responsibility | Current KSA implementation | NovaCore convergence |
|---|---|---|
| Shared relief | `Celestial.cs:791–883` and terrain preparation apply heightmap, biome and ordered modifiers. | Resolve existing generation-4 full H, including regional residual and support, at prepared vertices. |
| Render payload | `PrepareModifiers.comp`, `FinalizeModifiers.comp`, `MeshDataCommon.glsl`: resolve world/body authority upstream, publish local FP32 vertices. | Reuse precise preparation and existing camera-relative FP32 control payload. No new coordinate format. |
| Normals | `GenerateMeshNormals.comp`: adjacent triangle normals. | Initially retain full analytic H normals at shared vertices. This bounded adaptation preserves the current normal/material contract without another adjacency pass; coarse/near land appearance was explicitly checked. |
| TES | `PlanetTessEvaluation.tese`: interpolate prepared positions/normals, then material height textures, fixed mip2, four TOP_K materials, prepared-normal displacement faded over 37.5–48.75 m. | Interpolate prepared full-relief terrain. No extra geometric material detail this slice: NovaCore has no equivalent authored material displacement maps. Fragment detail remains. |
| Final coordinate | KSA `gl_Position` includes displacement; its `outWorldPosition` is pre-displacement. | Actual candidate gl_Position is the interpolated prepared clip surface; CPU analysis also checks actual captured TES positions. |
| Physical interaction | Full CPU terrain query plus independently managed `TerrainPatch` collider patches, about2m quads,4–64quads/side, predicted motion/impact demand. | Continuous full H remains gameplay authority. This ticket does not invent a spacecraft collider implementation. |
| Facility support | Ordered authored CCSFS decal (order9999) participates in shared CPU/GPU modifiers; TES has no special facility suppression. | Existing Florida grading and support participate in full prepared H; no geometry lowering or concealment. |
| Sampling density | `KSA.Program.cs:2640–2650`, `KSA.PlanetRenderer.cs:2154`, `PlanetMeshCollection.cs:114–124` use distance above physical terrain. | Consume the scene's already computed physical ground clearance in two selector inputs. |
| Updates | Render preparation is display-frame driven; it is not proportional to simulated ticks. | Retain bounded current/incoming preparation and atomic publication, transform for each displayed frame. Changed geography/data/pupil identity must be ready before replacement. |

KSA decompilation paths begin `build/ksa-residency-reference/`; authored shaders
are under `E:/Kitten Space Agency/Content/Core/Shaders/`. No KSA source was copied
into production. Its material amplitudes and collider spacing are **not** a
proof of NovaCore's total contact error.

## WORK B — replacement map and costs

| Old owner / stage | Measured or structural cost | New owner | Migration / retirement |
|---|---|---|---|
| Near H and gradient in TES | Amplified millions of times; baseline linked TES7,604 instructions,3,094 named FP64 ops. | Full-H shared vertex preparation, interpolated render surface. | Migrated; banked TES branch becomes removable after acceptance. |
| Base-only current/incoming preparation | Paid per changed prepared vertex/publication. | Full H and full normal preparation. | Migrated; increased preparation measured below. |
| Physical query | Existing continuous generation-4 full H, regional and support. | Gameplay query remains independent. | KEEP; no extra query added by selector correction. |
| FP64 normalization / direction | Global direction previously supported final near H and fragment addressing. | Resolve H upstream; retain direction/material interface where still consumed. | Most TES FP64 removed;101 named FP64 ops remain. |
| VS/TCS preparation and factors | Existing compact13-scalar control interface and projected factors. | Same interface and factor algorithm. | KEEP; linked VS/TCS ISA identical. |
| Regional/support contribution | Demand, verified residency, prepared geometry, same physical definitions. | Same sole NCSM1 owner. | KEEP outcomes; full relief now participates before refinement. |
| Final raster | Base clip plus TES near displacement. | Interpolated published clip surface, zero added geometry displacement. | Migrated. |

An exposed prerequisite required two bounded managed changes. At Colorado
H≈1,587m and clearance10.004m, Program passed approximately1,597m above the
reference sphere to render sampling. The old near evaluator could add fine
relief after coarse preparation. During descent, the18px Schmitt threshold
retained L14 (13.95px), although cold selection reached L17. Scene GPU constants
already computed the correct physical clearance. Startup and moving selection
now consume `gpu.SurfaceAltitudeMetres`; camera placement and physical H are
unchanged. Matched controls isolate the shader benefit after this correction.

## WORK C / IMPLEMENTATION

Lead owned design, branch, edits, builds and execution. KSA reconstruction and
the NovaCore replacement map were independent read-only work; two adversarial
reviewers attacked physical outcomes and performance respectively. No worker
edited production or ran competing GPU measurements.

Changed production files:

- `production_spherical_billboard_physical.glsl`: bounded compile-time selector.
- `production_spherical_billboard_prepare.comp` and `_incoming_prepare.comp`:
  full H/full normal instead of base-only preparation in the candidate.
- `production_spherical_billboard.tese`: candidate interpolates prepared surface,
  without near H/gradient evaluation or additional displacement.
- `samples/NovaCore.Triangle/Program.cs`: two physical-clearance inputs.
- `PlanetarySphericalBillboardNaturalTerrainProof.cs`: GPU reference-preparation
  helper now describes full H/full normal. Runtime physical query implementation
  was not changed.

The development default is `NOVACORE_PREPARED_RENDER_TERRAIN=1`. Compile the three
affected shaders with `=0` for banked shader behavior; baseline comparison also
retains the old Program input in its private host. The corrected-clearance
control uses the same host as candidate with shader0. Selectors operate in fresh
processes; there is no hot switch that could reuse mixed prepared buffers.

## NEW RENDER / PHYSICAL CONTRACT

[contract.md](contract.md) retains the initial cheap gate and its limits.

- **Render authority:** piecewise interpolation of full-relief prepared vertices
  and normals in the existing local render transport. Tessellation refines
  raster sampling; it does not re-query physical H per generated sample.
- **Physical authority:** precise, deterministic, camera-independent continuous
  generation-4 terrain queries. Simulation/world reference frames are unchanged.
- **Shared:** geography, elevation source, regional records, modifier order,
  facility grading and support, body/dataset identity.
- **Render-only:** local interpolation, raster sampling, lighting/material detail.
  There is no added render-only geometric displacement in this candidate.
- **Physical-only:** continuous query evaluation at arbitrary gameplay points;
  future collision consumers must explicitly honor measured representation error.
- **Coherence:** Florida actual rendered core triangles must remain within1cm of
  the support plane, with prepared/query agreement under3mm in that contact
  scope. Ordinary near terrain is measured against full H; this slice achieved
  sub-centimetre sampled agreement within50m, not a universal mathematical bound.
- **Transitions:** current owner persists until incoming regional data and
  physical preparation are complete; one publication replaces the old identity.
  Shared vertices use one direction/H result, retaining watertight interpolation
  and existing edge/factor agreement. No independent terrain owner was added.

High-precision body/geographic resolution is retained upstream. Camera-relative
transport error in the near Colorado triangle sample was at most5.44µm. Far
triangle chord error and GPU dataset sampling are reported separately; a global
1cm claim would be false. Deterministic query behavior remains covered by the
unchanged physical tests. No current spacecraft landing solver exists that would
justify claiming an end-to-end spacecraft landing simulation was tested.

## BAR and progressive gates

Gate1 source/ISA responsibility map justified plausible3–8+ms active gain.
Gate2 implemented only the bounded shader/preparation bridge. Gate3 established
startup, active/Florida rendering, full-H coherence and major performance signal.
Only then did Gate4 run five100-sample poses, additional nonzero-land controls,
moving contact, all18levels, routes, normal runtime and strict validation.
Gate5 reviewers found no remaining blocker after targeted supplemental evidence.

No final required gate is being represented by old angular/local encoding
experiments. Exact old H/depth/image parity is deliberately not the new bar.
The8.33ms goal remains unmet in four poses; an isolated host frame stall remains
unclassified. These limits do not negate the bounded migration's demonstrated
payoff, but prevent a claim of completed performance or hitch-free operation.

## VERIFY A — gameplay / physical red team

Strongest attacks were interpolation penetration, insufficient transition
contact evidence, and aliasing from analytic normals on coarse land.

Independent dense probes against actual GPU prepared arrays and production H:

| Test | Samples | Signed render minus query error |
|---|---:|---:|
| Colorado triangles within50m |307,517|−5.236 to+1.784mm|
| Florida feather within50m |303,470|−7.086 to+5.659mm|
| Worst feather triangle, finer sweep |8,385|minimum−7.172mm|

The first15-point probe underestimated the feather maximum (6.577mm). Denser
adversarial sampling still passed the sampled1cm design outcome. This is not a
planetwide bound. The moving and visual objections were closed below. Final
review: **PASS for bounded architectural implementation review**.

## PHYSICAL / GAMEPLAY VALIDATION

Captured final TES positions: Colorado26,652samples had maximum4.68mm error
(3.89mm among clip-visible samples). Every selected triangle's initial quarter
grid reached5.236mm. Florida feather TES14,899samples reached1.803mm; dense
prepared interpolation is the stricter reported7.172mm result.

Normal deployed candidate moving Florida check retained existing limits:

- 43actual GPU capture frames,1,569vertices, L8–L17 and40pupils overall;
- 81core contact samples specifically across **nine L16/L17 frames**, including
  pupil changes and returns, maximum support gap **3.572µm**;
- maximum prepared full-H mismatch **1.003mm**, squared normal error
  **1.403×10⁻¹⁶**; all contact samples factor1;
- far transport at700km reached25.135mm, not a near contact failure;
- 670regional records requested/loaded,93,392,640bytes uploaded, with readiness
  barriers and atomic publication verified; Mars/Saturn remain ineligible.

[Moving contact result](moving-florida-contact.json) retains the test's legacy
`maxBaseError` label: its migrated oracle is full H. The small43-frame replay
collection remains in `moving-contact-frames/`. Full D32/HDR archives are
consolidated and proven disposable, but still present because deletion was blocked.
Global dynamic driver's zero height/normal parity fields are placeholders; its
PASS is used for ownership/traversal, **not physical contact proof**.

## RENDER VALIDATION

[Representative images](representative-comparison.jpg) and
[pixel summaries](visual-comparison.json) compare the same frame175 pose:

| View | Final RGB change | HDR RMS | D32 maximum absolute difference |
|---|---|---:|---:|
| Colorado coarse L10 |mean0.354/255,max5,p99=2|0.001274|4.184×10⁻¹¹|
| Colorado near L17 |2,230/4,953,600pixels,max1/255|1.763×10⁻⁶|1.676×10⁻⁷|
| Authored Florida L17 |mean0.0743/255,p99=1,max190|0.0001863|0.0006306|

These are raw attachment differences, not metres. Florida's large isolated
maximum is not ignored; no exact image parity is claimed. Images and actual
contact measurements show no discernible facility separation. Coarse land has
slightly more fine grain, without observed broad shading discontinuity, cracks
or missing surface. Near land has no discernible regression. Snapshot coverage
is not exhaustive temporal image parity at every LOD.

Matched dynamic land runs complete L0–L17,10snaps,3reversals and58publications;
no zero/overlap/stale owner or Vulkan error. Normal deployed traversal and Florida
smoke independently confirm this candidate works outside the private host.
High-speed/focus-away returns are covered by existing drivers; no new player
camera or warp controls were introduced. The stable grazing benchmark retains
factor64 and the same projected tessellation responsibility.

## PAYOFF / PERFORMANCE

Pre-implementation expectation: **HIGH-LEVERAGE to MAJOR**, from removing
per-TES physical relief evaluation. Measured active payoff: **MAJOR**.

RX6800XT, driver32.0.21045.5002,3440×1440 client borderless, canonical strict
Khronos validation.100 aligned non-capture samples per pose; P99 is the99th
ordered value, not an invented extrapolation. Data and complete samples are in
[performance-summary.json](performance-summary.json) and each `gate4-*` record.
Constant sample fields are factored into `frameConstants`, without dropping
samples. Capture shader runs are excluded from timing claims.

All times ms; paired tail columns are baseline→candidate **total GPU**:

| Pose | Terrain baseline | Terrain candidate | Gain | Gain% | Total baseline | Total candidate | P95 | P99 | Candidate−8.33 |
|---|---:|---:|---:|---:|---:|---:|---|---|---:|
| Orbital |0.86872|0.83636|0.03236|3.73|1.26044|1.14236|1.41156→1.22420|1.43128→1.24344|−7.18764|
| Factor1 |6.28488|6.04852|0.23636|3.76|9.23236|9.08768|9.51540→9.68848|9.78292→9.74548|+0.75768|
| Florida |8.02504|8.06448|−0.03944|−0.49|11.13628|11.29351|11.48336→11.92392|11.71593→12.03557|+2.96351|
| Active |22.81020|14.48768|8.32252|36.49|26.40286|17.35252|27.06091→17.62653|27.27917→17.73311|+9.02252|
| Grazing |13.35304|7.52564|5.82740|43.64|16.45209|10.91547|16.93884→12.04125|17.21894→12.34868|+2.58547|

Active total improvement9.05034ms; grazing5.53662ms. Florida regresses0.15723ms
total and its tails worsen. Factor1P95 worsens. The table is not universal gain.
The future11.11ms fully featured floor and preferred6–7ms headroom are not
established by this terrain slice.

## CPU / PREPARATION / VERIFY B

The performance red team challenged shifted cost and different land sampling.
Matched corrected-clearance land control uses identical native/managed hashes
and pose, changing only shader0→1:

- terrain22.98552→14.72456ms; total25.91416→17.74152ms (**8.17264ms net**);
- totalP9526.09750→18.15731, P9926.17553→18.38206ms;
- candidate patches increase2,046→2,056 and TES2,777,175→2,782,383;
  fragment count4,953,600 unchanged. Reduced geometry cannot explain the gain.
- fixed startup incoming GPU preparation37.15268→39.73632ms,11slices.

Matched1,712-frame dynamic land runs:

| Quantity | Control | Candidate |
|---|---:|---:|
| Total GPU mean/P95/P99 ms |10.266/17.513/18.523|8.845/12.575/13.404|
| Frame mean/P95/P99 ms |13.881/20.683/21.909|12.238/15.397/17.453|
| Incoming preparation,357slices ms |1,155.42368|1,220.13080|
| Geographic GPU demand ms |1,926.83980|1,925.59240|
| Regional uploaded bytes |50,181,120|51,156,864|
| Regional allocated bytes |119,737,728|119,737,728|
| CPU record/submit/callback mean ms |0.285/0.070/0.286|0.229/0.058/0.267|
| Update minus fence-wait mean ms |2.739|2.653|

Added preparation64.70712ms over the traversal ≈0.0378ms/frame does not consume
the GPU benefit. Both runs have **zero current-preparation records/slices**;
they measure incoming work, not current-preparation cost in every possible
workload. CPU query code is unchanged and the selector reuses existing physical
clearance; no new per-frame full-H query is hidden. GPU total timestamp0 includes
demand/preparation; earlier CPU uploads are accounted separately.

Active standard workload:2,060patches,2,779,256TES and4,953,599fragment median,
factor64 unchanged. Grazing fragment count slightly increases. Gameplay terrain
resolution and source data are not reduced. Render detail is intentionally
prepared/interpolated rather than analytic H at every final sample.

Private dynamic maxima412→419ms include a one-time CPU geometry inventory:
frame166 scans149,101triangles, frame167 inspection402.102→409.650ms. That
diagnostic was removed from the normal deployment; it is not production cost.

Normal candidate1,701-frame traversal: GPU mean4.921/P957.356/P998.894/max9.790ms;
frame mean7.867/P9510.213/P9912.353/max181.135ms; inspection max0.223ms;
publication-frame max25.181ms and snap max7.613ms. A fresh normal shader0 control
also has a large frame maximum230.641ms, P9510.491/P9912.928ms, with58publications.
These are supplemental ordinary traversal controls, not replacements for matched
nonzero-land timings.

**Residual181.135ms is UNCLASSIFIED.** The whole-frame timer includes image
acquisition and work outside the named scopes. At least147.926ms lies outside
the maxima of update, record, submit and present. It belongs to the scale-change
flag bucket, but is not evidence of181ms GPU/preparation/publication. Do not blame
the driver, OS or acquisition without a correlated call/frame trace. No repeated
migration-induced hitch was established; do not claim hitch-free execution.
If Project Control needs causal closure, the bounded next proof is frame identity
plus acquisition/recreation/unattributed CPU timing, not a renderer redesign.

Final Verify B: **NO PERFORMANCE / ARCHITECTURE BLOCKER ESTABLISHED**.

Warp:240display frames each at1×,600× and7,776,000×. Pupil/frame identity stayed1,
publication delta0 and topology upload count1 at every hold. Preparation does
not grow with simulated ticks. This verifies display ownership, not a guarantee
of identical frame timing under every simulation workload.

## COMPILER / GPU

[Compiler evidence](compiler-summary.json), generated by the AMD driver:

| TES property | Banked shader | Candidate |
|---|---:|---:|
| Scalar/vector instruction count |7,604|563|
| Named FP64 operations |3,094|101|
| Named FP32 operations |400|150|
| VGPR / SGPR |190/36|48/32|
| LDS / scratch bytes |4,608/0|4,608/0|
| Wave width |32|32|

Counts classify named ISA operations, not weighted cycles. Linked VS/TCS remains
byte-identical, VGPR39/SGPR64. Fragment machine words are identical (2,949ops,
763named FP64); text hashes differ from relocated labels. Native A/B `.text`
sections are identical, despite PE timestamp/read-only metadata changing DLL
hashes. Each run records shader/native/managed identities. The candidate still
has FP64 direction/material addressing and unchanged fragment FP64; this is not
an FP64-free renderer or persistent final refined-geometry cache.

## TEST MIGRATION AND FINAL VALIDATION

KEEP: physical authority/determinism, foundation dimensions/support, regional
readiness, ownership, factor limits, interface layouts, geographic seams,
precision/camera, materials, launcher and strict Vulkan tests.

MIGRATE: production current/incoming source assertions and GPU preparation
reference use full H/full normal; Florida prepared oracle uses full H. Existing
1cm/3mm contact thresholds are unchanged. The new `TerrainRenderAuthorityTests`
reads the candidate branch explicitly; obsolete #else matches cannot satisfy
candidate assertions. Startup and moving physical-clearance use is checked.

SPLIT: the former final-sample render/query identity responsibility becomes
unchanged continuous physical-query tests plus actual prepared/TES coherence,
moving contact and presentation evidence.

RETIRE at bridge removal: banked base-preparation/TES-near source expectations
and the branch-extraction test helper. Do not remove full-H/near functions still
needed by preparation or physical queries merely because TES stops calling them.

Also corrected a pre-existing M13.2 source assertion: `ordinaryNcsm1` already
participates in anchored shading, but one residency assertion expected only the
old expression. It still rejects alternate anchored/stencil-fill draw owners.

| Validation | Debug | Release |
|---|---|---|
| Native and managed solution builds |PASS,0warnings/errors|PASS,0warnings/errors|
| Headless |79/79 effective,0skips|79/79 effective,0skips|
| GPU, no window |8/8,0skips|8/8,0skips|
| Canonical window lifecycle |1/1; four lifecycle runs|1/1; four lifecycle runs|
| Native regional CPU |PASS|PASS|
| Native GPU |2/2,0skips|2/2,0skips|
| Normal candidate shader identity |49/49|49/49|

Headless accounting is explicit: each broad run finished78pass/1fail because of
the stale M13.2 source assertion; the corrected test then passed independently
in both configurations. The other78passed unchanged. No failure or skip is
hidden inside an aggregate rerun claim. First Release native-GPU attempt lacked
an EXCLUDE_FROM_ALL executable; explicit test-target build resolved it, then2/2
passed. No validation severity or VUID was filtered. The first unbounded
headless topology regeneration was deliberately stopped; its replacement used
the existing library-input mode with unchanged assertions.

All15launcher regressions pass. Six deployed `CreatePlan` probes resolve the
normal current Release runtime: Solar overview, Earth far, Earth700km, Earth
fullscreen native, Florida, New Earth Renderer. These are six actual launcher
plan probes, not six separate GPU launches. Florida normal500-frame smoke
passes readiness, seating on return and one-owner checks, with no raw capture.
Full normal L0–L17 traversal passes separately. Global and regional production
asset hashes verify; no assets, manifests, cache policy or generators changed.
The two unrelated generic/input window cases were not rerun for this terrain
slice; the real moving regional window probe and production lifecycle were run.

Normal acceptance entry: `E:/NovaCore/tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`,
select **Florida Launch Site**. It resolves
`--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`
to `E:/NovaCore/samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe`.
This ticket does not substitute automated evidence for future Project Control
manual acceptance. Full hashes and configuration-specific dependencies are in
[deployment.json](deployment.json), [validation-results.json](validation-results.json).

## Invalid attempts and evidence limits

- Supplemental first Colorado descent stalled at L14 waiting for an L17-sized
  diagnostic snap. Lead stopped only that diagnostic process; exit4294967295 is
  not a GPU crash or production hang. Physical-clearance ownership explained it.
- The subsequent `land-level14` capture actually used L17. The level setting was
  a driver completion expectation, not a forced selector level. It failed its
  completion marker despite process exit0. It is **not L14 evidence**.
- Cold L17 capture did not prove the old descent selected L17; that earlier
  shorthand was corrected by the hysteresis trace and new full traversal.
- An initial diagnostic build missed a qualified geography namespace, then
  rebuilt successfully. A launcher test initially used the wrong TFM directory,
  then passed from its actual net10.0-windows deployment. Neither attempt counts
  as a completed validation pass.
- Ordinary-terrain coherence is sampled, not a world-wide analytic bound. Coarse
  far triangles can differ substantially from continuous H and must not be
  evaluated against Florida's near3mm test. No future spacecraft physics result
  is fabricated from these render checks.

## RETIREMENT PLAN

Old authority: base-only prepared terrain plus final TES near-H/gradient.
New authority: shared full-relief preparation plus local rendered interpolation,
with separately owned continuous gameplay queries.

The macro0 branch and private banked host are temporary review bridges. After
Project Control accepts this candidate, required manual outcome checks pass and
banking is separately authorized, remove the macro and old shader branches in
that bounded acceptance/retirement change. Retire bridge-specific assertions and
retain outcome tests and this compact reproduction record. Git/M13.2 remains the
historical baseline; no perpetual user-facing compatibility switch is intended.
Do not bank with an unexplained permanent dual render authority.

## M13.3 eligibility

The architectural and measured performance requirements survived the gauntlet.
The accomplishment is full relief at shared prepared vertices replacing
amplified physical evaluation in TES, with physical-clearance sampling and
measured contact coherence. No M13.3 title is formally proposed while the
required storage cleanup remains blocked. No banking action is authorized here.

## Reproduction and evidence lifecycle

Use the existing Windows build prerequisites. Run from repository root with
Python `-B` to avoid bytecode debris. `validate.py build` builds normal native and
managed Debug/Release. Native test executables are EXCLUDE_FROM_ALL: explicitly
build `NovaCoreFacilityVisibilityTests`, `NovaCoreSurfaceMaterialCoordinatesTests`
and `NovaCoreRegionalPhysicalTests` before `validate.py tests`. The latter runs
normal lifecycle/native GPU/asset checks. `validate.py routes` and `transitions`
run normal deployed route/smoke and full traversal under process-local strict
validation; these do not alter production registry/launcher behavior.

For complete headless, set `NOVACORE_P2S5F_ARTIFACT_INPUT` to the absolute
`assets/planetary-nested-scale-mesh` directory and use Graphics `--category=headless`.
Use `--category=gpu` for each configuration; no `--ambient` or direct `--case`
invocation should bypass the canonical wrapper.

Private A/B: `prepare.py --baseline`, then `prepare.py`, with no GPU process
running while building. These apply the retained host diagnostic patch and
restore source bytes in finally. They rebuild the native output temporarily;
always finish with `validate.py build` before normal launcher use. `run.py`
compiles shader0/1 into isolated runtime directories, each with a fresh process
and controlled layer environment. Examples:

```text
python -B docs/engineering-evidence/ksa-terrain-convergence/run.py active baseline baseline-active
python -B docs/engineering-evidence/ksa-terrain-convergence/run.py active candidate candidate-active
python -B docs/engineering-evidence/ksa-terrain-convergence/run.py land baseline-corrected-clearance land-control
python -B docs/engineering-evidence/ksa-terrain-convergence/run.py land candidate land-candidate --dynamic
python -B docs/engineering-evidence/ksa-terrain-convergence/run.py land candidate warp-candidate --warp
```

The five standard poses are `orbital`, `factor1`, `florida`, `active`, `grazing`.
Capture adds `--capture`; daylight control adds `--daylight`. GPU TES readback is
capped65,536records and samples primitiveID%127; no giant primitive geometry
buffer is enabled. `CoherenceProof.cs` independently samples actual GPU output
with current full H. Build diagnostic projects with output/intermediate paths
inside the owned scratch root. `LiveProof.cs` invokes existing permanent moving
contact tests. `visuals.py` reduces readbacks using NumPy/Pillow. `consolidate.py`
retains measurements, hashes, source recipes and small representative evidence;
it performs no deletion.

The retention set is reports/contract, exact KSA and runtime provenance, compiler
counts/hashes, all100timing samples, bounded GPU contact frame fixtures, image
comparison and reproduction tools. Resolved full readbacks, private runtime
copies, ISA dumps, logs and transient projects are disposable. No prior evidence,
production cache or deployment is removed. See [storage.json](storage.json) for
created/retained/disposed/remaining accounting and [final verification](final-verification.json)
for the complete unstaged Git status and prior-evidence checks.

## Cleanup execution blocker

Automatic approval review rejected the exact manifest-verified scratch deletion
with **"blocked by policy"**, without a more specific reason. No deletion was
performed and no alternate deletion mechanism was attempted. This is an
execution-policy blocker, not a physical/rendering failure or missing user
authorization. Project Control can review the candidate now, but storage
closeout remains incomplete.

Proven disposable:2,939files,3,938,992,766logical file-view bytes. Allocated space
recoverable is1,662,054,600bytes. The larger logical total includes34hard links
to the67,108,864-byte production oracle; its original asset remains outside the
cleanup scope. It must not be counted as34new physical copies or reclaimed data.

The user may run the guarded script manually from PowerShell:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\ksa-terrain-convergence\cleanup.ps1' -Preview
& 'E:\NovaCore\docs\engineering-evidence\ksa-terrain-convergence\cleanup.ps1'
```

It verifies exact paths, absence of reparse points, complete file count, lengths,
all SHA-256identities and the retained production oracle before deleting only
the two named scratch targets. If anything changed, it stops. The normal
Debug/Release deployment, production assets/cache and prior evidence are outside
its scope. A final normal smoke after manual cleanup remains outstanding.
