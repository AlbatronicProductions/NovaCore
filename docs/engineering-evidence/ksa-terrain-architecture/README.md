# KSA terrain architecture reconstruction — M13 decision record

**Strategic classification: NO CONCLUSION YET.**

**Final classification: ARCHITECTURAL COMPARISON INCOMPLETE — FOLLOW-UP REQUIRED.**

Current installed KSA demonstrates a useful, cheaper precision boundary: resolve
the large coordinate/phase in high precision upstream, then evaluate using a
small anchored representation. It does **not** demonstrate a persistent final
refined mesh replacing hardware tessellation. Nor does it demonstrate moving
NovaCore's exact near-field physical function into a cache while preserving that
function at final samples. The source reconstruction is complete enough to reject
those proposed shortcuts; the compatibility and performance proof needed to choose
a production migration remains incomplete.

This is not a recommendation to preserve NovaCore's present architecture or to
resume local shader tuning. The current 26–27 ms path misses its target badly.
The best next proof is a bounded **local physical-domain representation** study,
described below. No production correction, alternative renderer, or M13.3 milestone
is authorized or assigned by this report.

## Baseline and scope

| Item | Verified state |
|---|---|
| HEAD, local `main`, local `origin/main` | `4accf92fd080c16cc8656080aa69def3fa65db53` |
| Starting branch | `codex/m13-next-performance-target` |
| Investigation branch, created from banked main | `codex/m13-ksa-terrain-convergence` |
| M13.1 annotated tag | `m13.1-ncsm1-tes-hotpath` → `fade1384c1c7df93d954e7223b1cc8f17db17f98` |
| M13.2 annotated tag | `m13.2-ordinary-terrain-shading` → HEAD |
| Starting tracked/staged differences | Empty / empty |
| Starting untracked work | Existing conservative-pre-refinement-visibility and post-m13.2-next-target evidence directories |
| Hardware / reference extent | RX 6800 XT / native 3440×1440, as recorded in measured evidence |
| Production changes / new GPU measurements | None / none |

`origin/main` is the verified local remote-tracking ref; no network fetch or remote
write was performed. Both prior evidence directories are preserved. The previous
proof's historical closeout reports scratch left for manual cleanup; the current
filesystem confirms that scratch and associated Python bytecode are now absent.
Neither was recreated. The prior GPU timeout/freeze remains **UNCLASSIFIED**;
this ticket neither attributes it to production nor clears it.

The deployed Debug and Release native/managed hashes match the prior completed
restoration. All **49 Debug + 49 Release** SPIR-V module hashes match banked
deployment evidence. This is a current byte-identity check, not a new rendering
test. Native Release hash:
`b6b3ad74c35d73df34e0cba41a6703cfd7a7014a181d422b2e7b6184697883a2`.
Managed Release hash:
`92eefa86a87b1988161c082a929ca75263cbdc072abef42f3bab550dc916ad0a`.
Exact hashes for both configurations and every shader are in `verification.json`.
The normal runtime remains
`E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`.

No build was required: no diagnostic instrumentation or production source was
changed, and deployment identity was verified. No staging, commit, push, tag,
cleanup, main modification, quality adjustment or production experiment occurred.

## Source and history authority

Installed KSA contains shipped GLSL/XML and managed assemblies. The C# references
are **decompiled production assembly views**, not an original KSA checkout.
Existing extraction was reused after matching the installed assembly identities;
no new decompiler output or KSA source copy was created. The extraction reports
Production `2026.9.7.5402`, informational revision
`487c3f340de24c6a81037120b6d1129c045c5400`. The version string is recorded verbatim;
it is not substituted for the host date or an official release date.

| Installed assembly | SHA-256 |
|---|---|
| KSA.dll | `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f` |
| Planet.Core.dll | `2823b299051b91252453acb4f1fff5543b1f5473bc6f42c8d51d4958d3159ece` |
| Planet.Render.Core.dll | `6b9b3c3bd709bf198c6ab3ec6daaef7e468f593fb110187869a1d44272b92e3c` |

The [15-stage maps and direct comparison](pipeline-maps.md) record both pipelines
using identical responsibility names, cadence, persistence, precision, inputs,
outputs and synchronization. The [official chronology](history.md) records
specific messages, dates/periods, causes, replacements and installed-source
confirmation. `source-files.json` and `verification.json` record exact inspected
files and hashes. Targeted file inspection is not a claim to have read every
line of the very large Program/native files. Source facts, history reports,
measured NovaCore results and architectural inference are distinguished below.

## Current performance and sustained-frame limits

These are existing post-M13.2 production-control measurements, not fresh KSA or
NovaCore benchmarks. Original workload fingerprints and series are retained in
[post-M13.2 next-target evidence](../post-m13.2-next-target/README.md).

| Pose | Terrain median / P95 ms | Total GPU median ms | Selected patches | TES invocations | Fragment invocations |
|---|---:|---:|---:|---:|---:|
| Orbital | 0.83140 / 0.84156 | 0.98880 | 15,376 | 46,128 | 35,913 |
| Factor-1 | 6.54028 / 6.73788 | 9.40848 | 360,950 | 1,082,850 | 2,591,685 |
| Florida | 8.38312 / 8.73156 | 11.24696 | 294,748 | 884,244 | 3,984,347 |
| Active refinement | 23.92364 / 24.03032 | 26.75952 | 2,060 | 2,779,256 | 4,953,599 |
| Grazing | 13.93588 / 14.11132 | 16.94323 | 378,919 | 3,470,006 | 2,472,104 |

Active total GPU exceeds 8.33 ms by **18.42952 ms**. Terrain draw alone exceeds
that whole-frame budget; CPU submit tuning cannot close it. Terrain draw time
includes VS/TCS/TES/raster/fragment activity, not a measured isolated TES duration.

The later [final conservative proof](../conservative-pre-refinement-visibility/README.md)
has paired same-study total GPU percentiles. Preserve that separate run context:

| Pose | Normal total median / P95 / P99 ms | Reject total median / P95 / P99 ms |
|---|---:|---:|
| Orbital | 0.97328 / 1.01236 / 1.09436 | 0.97076 / 0.98372 / 1.10532 |
| Factor-1 | 9.38616 / 9.73972 / 9.88756 | 9.40484 / 9.86168 / 10.06444 |
| Florida | 11.18926 / 11.54331 / 11.69120 | 11.55276 / 12.05134 / 12.48384 |
| Active refinement | 26.64628 / 27.34446 / 27.62973 | 25.54533 / 26.28584 / 26.45075 |
| Grazing | 16.63324 / 16.97664 / 17.33018 | 15.56016 / 16.55472 / 17.19721 |

The final bound passed 4,203 oracle frames with zero false rejects/containment
violations and 4,204 parity frames with zero physical/depth/HDR/image mismatches.
Control/transition percentile non-regression did not pass the promotion burden;
the prototype is not production. Its approximately 1 ms refined-view saving is
neither a universal culling ceiling nor an authorized M13.3 target.

CPU record medians in the five-pose study were 0.1453–0.2690 ms, submit
0.0390–0.0619 ms; GPU fence wait 0.9076–26.5082 ms is largely waiting, not another
cost to add to total GPU. Existing preparation evidence records zero stationary
preparation, approximately 0.207 ms/frame ordinary movement and 0.401 ms/frame fast
movement, with a whole preparation/publication taking roughly 4.5–4.8 ms spread
across stages. Those older preparation figures are not newly measured at every
post-M13.2 pose. Sustained factor-1 movement still has an explicit target-age /
superseded-preparation proof gap. The earlier warp traversal's 1,069.178 ms CPU
outlier remains unexplained. No current full-game 90 FPS floor, unrestricted
flight P99, or KSA matched timing is established by this analysis.

## Final physical authority: what actually reaches raster

KSA prepares height from geographic maps and ordered authored modifiers, including
decals, into base vertices. It computes geometric normals from adjacent prepared
triangles. This happens each displayed terrain frame in the installed source.
TES interpolates those FP32 camera-relative positions and normals, then adds
material-texture displacement inside the configured near range. It does not
rerun the geographic height/procedural modifier stack for each final sample.
Source textures and buffers persist; those final TES positions do not.

KSA's CPU height query uses the height source and CPU modifier stack, with float
modifier evaluation deliberately aligned to GPU inputs. Its per-vehicle collision
patch persists and rolls sampled rows/columns as contact demand moves. **The CPU
query does not include the final material displacement sampled in TES.** Thus
there is no source proof of exact collision/final-raster equivalence for all fine
detail. A patch-local collision representation exists, but it is not the rendered
terrain's persistent final geometry. KSA's own history records errors caused by
filtering, texture quality, domain precision and mismatched support geometry.

NovaCore already prepares geographic/global/regional and macro/meso base shape
outside final TES, with regional readiness and facility support. Its final path
interpolates that base and evaluates the remaining analytic near contribution
and gradient, including support attenuation, when the 40–50 m fade participates.
The ordinary TES no longer pays the inverse geographic address removed by M13.1.
Calling all of this “full H recomputed in TES” would misdiagnose the current code.

The important difference is **the meaning of near detail**: NovaCore's analytic
near height/gradient is physical authority; KSA's corresponding TES stage applies
material visual displacement atop a separately queried physical base. Merely
moving near detail into normal maps or substituting KSA textures changes the
surface. It is not an equivalent implementation of the required outcome.

**H-in-TES conclusion: INSUFFICIENT EVIDENCE.** The required outcome is the
correct, reproducible near physical surface, including support and contact; the
shader stage is not itself a required outcome. A prepared representation could
replace it if it proves that outcome. Current KSA does not provide such a proven
equivalent representation for NovaCore's continuous field. Retaining production
unchanged during this investigation is not a permanent retention decision.

**Final refinement ownership conclusion: ADAPT.** KSA still uses the same broad
hardware tessellation owner, shared-edge endpoint factors and fractional-odd
triangles. NovaCore already uses the equivalent perspective compensation and
50 m near range. There is no KSA evidence to justify replacing this owner with
compute-generated persistent refined patches now. Adapt its inputs/physical-domain
representation if proven cheaper and equivalent; do not reduce amplification
quality to make that comparison pass. This is an architectural direction, not a
measured or authorized TCS/TES change.

## Precision boundary and numerical analysis

The required precision is an outcome at physical queries, world placement,
support/contact, edges, depth and transitions. It does not require FP64 in every
shader expression. KSA resolves exact CPU world/body transforms and the billboard
anchor before FP32 packing; carries high/low anchors, local deltas and compensated
noise/UV domain reduction; then uses FP32 prepared geometry and final TES work.
Some modifier paths still accept a collapsed direction; this is not proof every
installed KSA operation has micrometre precision. Its precision helpers explicitly
rely on correct `precise`/FMA behavior. That must be validated on target compilers.

For IEEE representations, spacing at magnitude x is
`2^(floor(log2(abs(x))) - fraction_bits)`. Reproduction is in `verify.py`.

| Representation / magnitude | Adjacent-value spacing in metres | What this proves |
|---|---:|---|
| FP32 at Earth radius 6,371,008.8 m | 0.5 | Full absolute FP32 Earth positions cannot preserve millimetre contact |
| FP32 at 10 km local offset | 0.0009765625 | About 0.977 mm spacing before accumulated arithmetic error |
| FP32 at 1 km local offset | 0.00006103515625 | About 61 micrometres spacing |
| FP32 at 50 m local offset | 0.000003814697265625 | About 3.815 micrometres spacing |
| FP64 at Earth radius | 0.0000000009313225746154785 | About 0.931 nm spacing; not a whole-pipeline accuracy guarantee |

Half an ULP bounds one correctly rounded conversion, not the total evaluator.
Near magnitude-one direction components, FP32 spacing of order 2^-23 times Earth
radius is order 0.76 m; multiplying a normalized direction back to Earth scale
reintroduces lost information. A small coordinate alone solves neither the
nonlinear normalization nor the discrete noise-cell identity problem.

| Expensive current FP64 responsibility | Error prevented | Can responsibility move upstream? | Missing proof |
|---|---|---|---|
| World/body subtraction and pupil anchor | Catastrophic cancellation and unstable facility/world contact | Keep high precision at authority; publish local basis/anchor instead of repeating full magnitudes | Transform composition, rebases, poles, seams and error accumulation |
| Reconstruct/normalize final body direction in TES | Correct evaluation point over curved Earth and under moving camera | Potentially derive compensated local direction from prepared anchor/corners | Must match the actual prepared planar triangle and existing final evaluation point, not substitute nominal-sphere respherization |
| Near noise cell reduction/hash coordinates | Different cells/gradients at large signed coordinates and boundaries | Integer anchor + small signed cell carry/fraction is plausible; KSA proves analogous compensated domain reduction | NovaCore uses signed two-word cell identity, different field math, warp, scales and gradients. Boundary crossing and overflow proofs needed |
| Per-sample controls, family blend and domain warp | Correct varying field and derivative | Not all patch-constant. Some invariant basis/phase can be prepared; nonlinear sample work remains unless a new representation is proven | Earlier census found no additional expensive constant term. Interpolation of corner near values is not exact for the nonlinear field |
| Near analytic gradient and support composition | Normal/contact coherence, support feathering | Could use local arithmetic with bounded error; cannot replace by an interpolated base normal without proof | Hessian/gradient and support-transition error bounds, CPU/GPU agreement |
| Final small radial displacement/projection | Preserve actual depth/contact after camera-relative placement | Local displacement is already partly used. KSA suggests cheaper local projection inputs | Equal physical points can still yield different rounded clip/depth values; same-frame raster proof required |

The KSA developer report of roughly ±20 micrometres after anchor correction is
reported evidence, not a NovaCore tolerance or benchmark. A height interpolant's
error generally depends on curvature of the function and squared sample spacing;
no certified Hessian/error bound for a reusable NovaCore near-field interpolant
was established here. The existing ~9.54 m conservative displacement envelope is
a visibility bound, not permission for metre-scale geometric approximation.

Therefore a blanket FP64→FP32 edit is rejected. Equally, FP64's current location
is not established as necessary. Anchor/cell/fraction preparation is the narrow
unresolved boundary with meaningful KSA precedent.

## Contract and required-outcome audit

Classifications describe the target principle; **ADAPT** is not proof an
implementation is ready. Positive evidence is required for intentional differences.

| Responsibility | Required outcome | NovaCore current mechanism | KSA current mechanism | What breaks with literal adoption; real requirement or coupling? | Classification / evidence |
|---|---|---|---|---|---|
| Canonical H | Reproducible terrain queries and physical surface | Versioned canonical field and CPU oracle | CPU/GPU height and ordered modifier counterpart | Different authored terrain changes physical height. Real outcome; one exact function's shader placement is coupling | ADAPT shared physical authority; Celestial + canonical GLSL |
| H-in-TES | Correct fine physical height/gradient | Analytic near final sample | Prepared base + visual texture displacement | Nonzero near field is replaced/omitted; real shape mismatch, not just a test expectation | ADAPT representation only after parity; no stage exemption |
| Physical sample frequency | Same accepted surface under movement | Base on changes, near every final sample | Base each frame, texture per final sample | Recomputing base every frame wastes existing reuse; merely caching samples at changed positions can give stale terrain | ADAPT invalidation to actual dependencies; current preparation measurements |
| TES final refinement | Sufficient detail, seamless shared edges | Near 50 m endpoint/skew rule | Equivalent endpoint/skew rule; near material displacement | No demonstrated break at amplifier boundary; semantics of input differ | ADAPT, not replace by an unobserved KSA architecture |
| FP64 GPU placement | Precision sufficient from contact to orbit | FP64 physical frame and final direction/field | CPU double anchor, split/local GPU arithmetic | Full FP32 Earth loses 0.5 m spacing; anchored representation avoids that argument but needs full proof | ADAPT; numerical table + KSA precision history |
| Physical vs cosmetic displacement | Do not change required physical terrain to improve FPS | Near field participates in canonical physical result | Material displacement absent from CPU height query | Literal replacement changes known nonzero physical height/gradient; real mismatch | INTENTIONALLY DIFFER for substituting cosmetic detail for physical shape, not for retaining H in TES |
| Current/incoming count | Bounded resource use, coherent owner, forward progress | At most one incoming physical generation | Frame-slot geometry plus independent texture transactions | Old/new maps may mix physical meaning; exact two-generation mechanism is replaceable | ADAPT completion and dependency ownership; generation count not fundamental |
| Atomic publication granularity | No incomplete physical data becomes current | Whole-generation readiness plus pupil identity | Completed map view independently publishes | Resident coarse map can differ from canonical full physical source. Actual prior Florida missing-residual failure demonstrates this matters | ADAPT transaction granularity after dependency-closed partition proof |
| Patch persistence | Reuse stable values without stale results | Persist prepared base; transient final TES | Reuse topology/buffers; regenerate render base; rolling collision patch | No product need to discard NovaCore reuse; no evidence a KSA final patch cache exists | ADAPT only demonstrated reuse ownership |
| Physical representation | Same supported heights, normals, contact, edges | Procedural analytic near over prepared base | Heightmap/modifier base + material fine representation | Replacing function changes outcomes; a new representation preserving them remains possible | ADAPT, require explicit error/parity contract |
| Visibility | Reject only noncontributing terrain | Prepared displaced physical bounds → cull/compact | Prepared sphere + displacement tolerance → cull/compact | Different bound constants can reject valid NovaCore surface; ownership itself matches | ADAPT actual field/rounding envelope; latest oracle proof |
| Residency ownership | Required physical data available and integrity verified | NCSM1 geographic masks, separate physical cache | Projected whole-map mip demand, async image pool | Budget-driven physical degradation can change terrain/support | ADAPT async lifecycle, retain outcome-complete dependencies; no arbitrary fallback |
| Deterministic selection | Reproducible simulation/physical queries | Stable physical identities and ordered publication | Presentation snap history can vary; physics separate | Different render vertex history is not itself simulation nondeterminism; coupling if no visible/physical consequence | ADAPT; keep stable physical identity, test presentation continuity independently |
| Facility support/contact | Accepted pad, footprint, transitions and lighting | Physical support + accepted facility presentation | Decal flattening, mesh-aware placement, terrain material integration | Different grading/shape affects known physical geometry; arbitrary mound/offset unacceptable | ADAPT current shared-support responsibility; do not change accepted facility work here |
| Time warp | Warp does not multiply graphical work per display frame | Frame callback graphics; simulation separate | Render command terrain preparation separate from physics tasks | No break for equivalent frame ownership; physics may do additional necessary work | ADOPT separation, already substantially aligned |
| Visual materials | Future detail and shading without redefining physics | Specialized ordinary shading now | CPU phase offsets, biplanar material stack, separate fine appearance | Copying artistic textures/quality settings not equivalent or authorized | ADAPT independent material responsibility, not a replacement physical authority |

## Visibility, persistence and publication verdict

KSA culls **after base preparation and before tessellation**. NovaCore already
does that. Neither current architecture proves exact rejection of all final
subtriangles outside the raster view. The 96.09% result means 96.09% of clipped
generated primitives came from wholly clipped parent patches; it is not 96.09%
of GPU time or of all geometry. In that census they were 72.25% of all generated
primitives. The source does not establish KSA structurally eliminates this waste.
Better footprint planning or final physical bounds may still matter, but a new
unmeasured visibility architecture cannot be called KSA-proven.

KSA amortizes topology construction, buffer allocation, material-phase preparation
and streaming resources. NovaCore already amortizes topology and physical base
samples; KSA does not additionally cache its final render vertices across frames.
KSA collision cache reuse is useful for future simulation consumers, not evidence
to rebuild the renderer around it.

NovaCore's one current plus one incoming generation is a mechanism protecting
complete coherent publication. The required outcome can permit smaller
dependency-closed transactions, but shared edges, normals, bounds, physical
versions and facility contact must move together. KSA's per-map completed upload
is a sound asynchronous resource lifetime boundary, not a proof that independent
mip publication preserves NovaCore physical output. Its coarse-while-streaming
behavior cannot simply replace missing-regional-data blocking.

## Performance opportunity model

Gain classes below are hypotheses or established probe results, never an additive
budget. Overlapping changes cannot be summed. Current KSA matched timing, dynamic
occupancy, isolated current TES time and end-to-end local-domain cost are UNKNOWN.

| Boundary / action | Current measured NovaCore cost | Work eliminated / moved / persisted / unchanged | Potential gain class | Status, confidence and missing proof |
|---|---|---|---|---|
| Prepared local precision/domain representation | Active terrain 23.924 ms; 2.779 M TES; 190 VGPR and 3,094 static FP64 instructions | Move large-domain phase/anchor reduction upstream; retain varying field/gradient; may reduce precision cost/register lifetimes | **3–8 ms hypothesis only**; could be <1 ms or negative | INFERRED opportunity, low quantitative confidence. No measured fraction attributable to replaceable FP64; compiler, exact-field and net timing proof required |
| Move all near physical evaluation into a prepared representation | Same combined draw; near branch almost all active samples | Would amortize near value/gradient, but adds representation generation/storage/lookup and invalidation | **UNKNOWN**, cannot select 8–15 or >15 ms from evidence | No equivalent current KSA final-physical cache, no certified representation. Not a production recommendation |
| Persist final tessellated geometry | Same draw; duplicate exact direction census exists | Avoid some repeat evaluation, add cache identity/topology/bandwidth/synchronization; camera fade remains varying | **UNKNOWN** | No current KSA reference for this responsibility. Census is not cache performance proof |
| Conservative pre-refinement rejection | Active total ~26.65 ms in paired study | Removes proven invisible amplification; leaves bound/TCS and other work | **1–3 ms** refined-view probe (~1 ms); controls can regress | MEASURED, high confidence for recorded matrix, not a promotable stable-frame result |
| Adopt KSA per-frame prepared mesh cadence | Stationary prep zero; movement averages ~0.207/0.401 ms in prior study | Loses some existing preparation reuse; does not remove final near work | **<1 ms** ordinary-prep budget, potentially negative | MEASURED current cost context, no gain demonstrated; reject as default migration |
| Change current/incoming publication granularity | ~4.5–4.8 ms total work per publication spread across stages; movement age issue | Could change latency/spikes, not directly remove steady 23.9 ms draw | **UNKNOWN** for tails; no proven steady-state gain | Need dependency partition and age/P95/P99 proof; KSA per-map transaction is not an equivalent physical transaction |
| Material phase upstream / fragment responsibility | Ordinary FS 85 VGPR; draw includes fragment cost | Future high-fidelity materials can use local addressing; ordinary inverse and specialization already banked | **<1 ms unproven incremental phase hypothesis**; future material cost UNKNOWN | Current banked savings cannot be recounted; no new material change justified |
| Impact-driven physics patch demand | No comparable current NovaCore GPU terrain timing | Avoid future unnecessary collision work, leaves current render cost unchanged | **<1 ms current GPU opportunity (no demonstrated work to remove)** | Source/history architecture for future physics only; not an M13 GPU target |

Older pre-M13.1 ablations changing near work or address work overlap and used a
different shader. They are causal clues, not a current 8–15 ms saving estimate.
The post-M13.2 exact scheduling probe improved only 0.00472/0.01256 ms in reversed
pairs, below observed variation. That closes that local probe, not the precision
representation question.

## Next bounded proof, not production implementation

No **next production responsibility** is sufficiently proven yet. The first
useful research boundary is the authoritative **local physical-domain input to
the near evaluator**. It challenges current global FP64 reconstruction without
assuming a new height texture, coarser field, persistent final mesh or reduced
tessellation. KSA precedent is `PrecisionFuncs`/`MeshDataCommon` and CPU UV phase;
NovaCore adaptation must preserve its own signed cell hashing, nonlinear warp,
family mixing, derivatives, support and coordinate conventions.

| Migration/proof element | Bounded design for review |
|---|---|
| Old authority | Current final-sample FP64 body direction/domain reduction and near evaluator; banked source remains oracle |
| Candidate authority | Prepared high-precision anchor/basis and explicit integer cell/fraction representation, with sample-local evaluation. Start only with domain reduction; do not move nonlinear field values into corner interpolation |
| Temporary bridge | Private diagnostic descriptor/pipeline variant with banked and candidate records from the same frozen frame/physical identity. No ordinary launcher flag or second production owner |
| A/B selection | Diagnostic-only explicit variant with shader/source hashes, identical topology/factors/camera/assets/generation, alternating run order. Separate capture from timing pipelines |
| Physical proof | Exact cell/hash identity first, then height, gradient, support contribution, final physical position/normal against CPU and banked GPU. Cover cell boundaries, signed carries, warped cells, poles, seams, high relief, all support transitions and 40/50 m boundaries |
| Precision proof | Outward error analysis for anchor packing, normalization, reduction, FMA, near gradient, rebase and projection; test worst cancellation and driver compilation. Do not infer tolerance from KSA's reported ±20 micrometres |
| Determinism proof | Same physical identity across repeats, run order, camera histories and warp; no timing/cache-arrival-dependent heights. Presentation sample selection may differ only under an explicitly reviewed equivalent-outcome contract |
| Depth/image proof | Same-frame physical records, D32, HDR and final image. Begin with exact gates; if a mathematically equivalent reordering changes bytes, report the error and request architectural adjudication in the next ticket rather than silently relax acceptance |
| Frame-time proof | Native 3440×1440 RX 6800 XT; orbital/factor-1/Florida/active/graze, reversed repeated A/B, warmed controls; total GPU median/P95/P99 and worst sustained states; CPU prepare/record/submit/fence, upload, memory and publication costs included |
| Dynamic proof | Continuous approach, low/grazing traverse, orbit descent/ascent, rotations/retreat/reapproach, pupil snap/rebase, level changes, regional boundary and facility feather; ordinary/fast motion and warp. No reuse of an earlier different candidate's traversal as proof |
| Rollback | Remove private variant and rebuild only if instrumentation affected diagnostic deployment; verify banked production source and shaders. Preserve compact measurements, dispose private scratch |
| Old-authority retirement | Only after required physical/precision/determinism/depth/image/dynamic/performance and manual player acceptance gates pass, replace the one owner and remove temporary bridge. Do not retain dual implementations by default |
| Expected class | 3–8 ms is a low-confidence hypothesis for investigation, not measured gain or a promise to close 18+ ms. If insignificant, stop this lead; do not compensate by reducing required quality |

If this first boundary fails to deliver equivalent outcomes or meaningful net
gain, the next architectural question is a certified physical representation
for fine detail, not an unproved KSA cache transplant. That would require separate
design evidence. No full renderer replacement is recommended here.

## Future player/simulation compatibility

| Required use | What the local-domain boundary must preserve | Present evidence / remaining gate |
|---|---|---|
| Standing, contact, landing | Same physical surface and gradients, bounded numerical error at rest/impact | Canonical oracle remains; proposed representation unproved |
| Launch pads and surface facilities | Same grade, support attenuation, authored mesh contact and final visual/depth coherence | Accepted production is untouched; must traverse footprint/feather at grazing views |
| Low-altitude flight and terrain-relative spacecraft motion | Stable fine field under translation/rotation and snap, no sample swimming | Local coordinate spacing is favorable; nonlinear/temporal proof missing |
| High-speed atmospheric flight | Bounded work, timely physical demand/publication, no stale terrain authority | KSA impact-demand history applies to future physics; NovaCore generation-age issue remains explicit |
| Launch, surface-to-orbit and orbit-to-surface descent | Seamless local/global precision and LOD/50 m transitions | Requires continuous dynamic matrix, not stationary image comparisons |
| Time warp | Render preparation once per display frame; simulation remains independently correct | Existing ownership supports separation; candidate must not add per-microstep GPU work |
| Deterministic/reproducible simulation | Stable physical function, seed/source IDs and exact authority independent of frame arrival | Preserve CPU oracle and identity; render vertex sequence is not automatically simulation truth |
| Later high-fidelity materials, water, atmosphere, clouds, vegetation, clutter, shadows, spacecraft and effects | Visual material complexity separated from physical queries; sufficient frame-time headroom | KSA's material/physical distinction is useful only without removing required physical detail. Current budget has no headroom; no future fully featured performance claim |

## Remaining proof and decision

The comparison does not establish either required strategic proposition:

- **CONTINUE LOCAL OPTIMIZATION** would require equivalent core responsibility
  boundaries. Physical fine-detail semantics and precision placement still differ
  materially; “both use TES” is insufficient.
- **ARCHITECTURAL CONVERGENCE REQUIRED** would require a superior compatible
  replacement boundary. KSA provides a credible local precision precedent, but
  neither NovaCore field equivalence nor meaningful net gain has been proven.

Thus **NO CONCLUSION YET** is deliberate, not a claim that the current path is
optimal. Missing proof is bounded to local-domain physical/precision equivalence
and net performance. It is not missing access to KSA source/history. No M13.3
title is proposed: a number must follow the proven responsibility, not a promising
idea. Project Control can authorize the bounded proof above before selecting a
production implementation ticket.

## Validation and storage

`python -B docs/engineering-evidence/ksa-terrain-architecture/verify.py` verifies
refs, empty staged/tracked diffs, both restored runtime identities, all banked
shader hashes, source provenance and absence of manually retired scratch. It
does not modify production or generate bytecode/captures. `git diff --check` is
required at closeout; because new evidence is untracked, whitespace/JSON checks
also explicitly inspect this package. Exact closeout counts are in `closeout.json`.

Permanent package budget: **250,000 logical bytes**. Retain only this report,
maps, concise chronology, source/hash manifests and read-only verification recipe.
No KSA source, bulk Discord export, image/video download, raw GPU data, new binary,
shader build, cache or disposable scratch is retained. Diagnostic/research output
created equals the final retained package payload for lifecycle accounting;
intermediate rewrites are not represented as an audited lifetime I/O byte count.
Disposable output created/removed/remaining: **0 / 0 / 0 bytes**.

Final Git status is recorded in `closeout.json`; the only added status entry from
this ticket is this evidence directory. Existing untracked evidence remains.
Stop for Project Control review.
