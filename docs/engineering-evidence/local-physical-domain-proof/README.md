# Local physical-domain representation proof

**NO PROVEN M13.3 CONVERGENCE TARGET YET.**

The tested cheaper representations fail the ticket's numerical preflight. An
anchor plus one FP32 local offset cannot, in general, preserve the exact current
input: a retained counterexample encodes two distinct current near-field results
as the **same local payload**. Keeping FP64 offsets and the original evaluation
order passes the numerical control, but does not move the costly normalization,
nonlinear field, gradients or domain reduction out of final-sample work.

Even the tested FP64 expansion of the norm and split quotient changes rounding.
These are quantified below. They are not evidence of a visible production defect,
nor proof that every compensated/integer local representation is impossible.

**Stop condition:** ticket section 6 requires quantification and Project Control
review when representation reordering loses exact identity without an applicable
accepted error contract. Section 9 permits a GPU dual path only after the error
analysis is sound; section 16 permits performance A/B only after physical/raster
parity. Those gates were not met. No GPU prototype, shader change or performance
run was performed. The later validation gates are explicitly **NOT RUN**, not PASS.

## Baseline

| Item | Verified result |
|---|---|
| HEAD / main / local origin/main | `4accf92fd080c16cc8656080aa69def3fa65db53` |
| M13.1 annotated tag | `m13.1-ncsm1-tes-hotpath` → `fade1384c1c7df93d954e7223b1cc8f17db17f98` |
| M13.2 annotated tag | `m13.2-ordinary-terrain-shading` → HEAD |
| Starting branch | `codex/m13-ksa-terrain-convergence` |
| New branch from banked main | `codex/m13-local-physical-domain-proof` |
| Starting staged/tracked diff | Empty / empty |
| Existing untracked work | Three prior evidence directories, preserved |
| Reference hardware / extent | RX 6800 XT / native 3440×1440; no new GPU run |
| Deployed identity | Both restored runtimes unchanged; 49 Debug + 49 Release shader hashes match prior banked evidence |

`origin/main` is the local remote-tracking ref, not a fresh network fetch. No ref,
production source, shader, manifest, asset, cache policy or launcher behavior was
changed. Exact refs, binaries, source hashes and prior-evidence content fingerprints
are in `verification.json`. The previous broad KSA audit and conservative culling
proof were not repeated. The prior host/GPU timeout remains unclassified.

## Current physical coordinate flow

Symbols: R = 6,371,008.8 m; C = body-fixed camera reconstructed from two FP32
components; V = interpolated FP32 camera-relative view vector; P = C − V;
d = normalized P; X = normalized d × R, the actual near-family input. The second
normalization is present in the current near wrapper. A new coordinate system
must preserve the implemented evaluation point, not replace the prepared planar
triangle with a nominal spherical triangle.

| Stage | Representation, precision and origin | Range / granularity / cadence | Cost responsibility |
|---|---|---|---|
| Body/world authority | Immutable double celestial/body/camera state | Planetary/interplanetary; body/view; display snapshot from simulation | High precision remains at world authority; not a simulation-step render loop |
| Patch identity | NCSM1 integer common-denominator lattice, topology hash and pupil frame | L0–L17 library; per vertex/level; static topology, movement/publication frame | Already persistent, not rebuilt per TES |
| Patch preparation | FP64 body direction/physical position; global/regional base + macro/meso and facility support | Approximately R in position, terrain in metres; per base vertex on changed physical/pupil inputs | Residency readiness, reusable physical results and bounded preparation |
| VS | FP64 `bp − C`, then FP32 relative body position; existing quaternion/projection | Near visible positions tens of metres; distant vertices can be planet scale; per base vertex/draw | High-precision subtraction precedes FP32 transport; prepared normal and height transported |
| TCS inputs/outputs | FP32 corners, normals, view vectors, height; unchanged compact interface | Three control points, shared-edge factors, per patch/draw | No per-patch physical anchor currently transported; 13 user scalars per control point |
| TES interpolation | V is FP32 barycentric combination of camera-relative corners | Each final sample; near branch when `length(V) < 50 m` | Already local; changing interpolation order is a separate source of rounding |
| Body reconstruction | C high + low converted to FP64; P = C − double(V) | Components up to Earth radius; per final sample in source | Large coordinate reconstruction, square/dot and inverse root repeated; compiler may scalarize uniform work, so source frequency is not isolated cost |
| Direction / H input | FP64 normalized P, then normalize again and multiply by R | d components ≤1; X approximately R; per participating final sample | Returns from small V to large domain; second normalization must be included in parity |
| H near evaluation | FP64 control fields, biome blend, orientation, warped near fields and analytic derivatives; signed integer cell hashes | Control scale 240 km, near cells 120–520 m; per participating sample | Controls/warp vary within patch; not all hoistable. Global/regional base is not reloaded here |
| Support contribution | Canonical direction to immutable Florida site frame; near height/gradient attenuation | Compact 64×56 m inner half-extents plus 128 m feather; per participating sample | Must not depend on chosen render anchor; base grading was already prepared |
| Final physical result | Interpolated prepared base height plus weighted near height; analytic normal; radial near offset along d | Metre/submetre offsets; per final sample; near fade 40–50 m | Banked max/normal/gradient operation order and field identity are part of the current output baseline |
| Camera-relative transport | Same FP32 interpolated base plus FP32 radial displacement; rotate offset only | Small local offset; per sample | Near local displacement already avoids rebuilding full Earth position for projection |
| Raster position | Interpolated base clip + VP × rotated displacement with w=0 | FP32 homogeneous clip coordinates; per sample/draw | Rounding affects clipping/depth/coverage; small physical delta does not imply identical D32/HDR/image |

Source lookup: current `production_spherical_billboard.vert/.tesc/.tese`,
`planetary_natural_terrain_surface.glsl`, `planetary_natural_terrain_families.glsl`
and `planetary_natural_terrain_field.glsl`. Their hashes are retained. No geographic
inverse removed by M13.1 is proposed for removal a second time.

## Required invariants

| Invariant | Outcome that must remain true; mechanism is not protected merely because it exists |
|---|---|
| Body direction | Same accepted point on the actually prepared/interpolated surface, with exact current identity unless a specific replacement numerical contract applies |
| Canonical H input | Same signed cells, fractions, family/seed/warp/blend and resulting near value/gradient; no anchor-dependent answer |
| Regional terrain | Same authoritative dataset/sector content and complete physical dependencies; no fallback or material-residency substitution |
| Facility support | Same support plane, weight/gradient and feather, with exact current authored geometry/contact |
| Parent/child and shared edges | Same common physical sample from either owner; no different reconstructed point because anchors differ |
| Cross-face / corners | Same body-fixed geographic meaning across face aliases; face index may not change H |
| Final physical position | Same prepared base, near contribution, fade and normal; no nominal-sphere respherization or quality reduction |
| Generation determinism | Same physical/source identity and complete publication independent of arrival time, cache state or frame rate |
| Camera-relative output | Sufficient world-to-local precision and required exact raster comparison; small error is not an automatic exemption |

## Bounded KSA reference

The [retained architecture report](../ksa-terrain-architecture/README.md) remains
the provenance. Only `Common/PrecisionFuncs.glsl` was reread for this proof.
KSA uses a CPU-double billboard pivot/direction and cubemap anchor, splits anchor
components, and sends a small pivot-relative delta from its prepared mesh basis.
The tangent reference changes on movement/snap; transforms and packed data are
updated per displayed terrain frame. Height/modifier evaluation occurs after
localization in compute. Final TES consumes FP32 prepared camera-relative geometry
and material displacement, not NovaCore's analytic near family.

KSA retains split anchors through terrain sampling and uses compensated products,
phase reduction and a local renormalization approximation. It does not merely
collapse the whole Earth position to FP32. Its second-order local normalization
is sufficient for its stated representation, but is not an exact implementation
of NovaCore's two rounded FP64 normalizations. Its source comments distinguish
collapsed orientation consumers from precision-sensitive terrain sampling.

KSA removes full-magnitude physical reconstruction from its final TES partly
because it prepared physical height upstream and applies different fine-detail
semantics afterward. This proof cannot inherit that semantic difference.

## Anchor candidates and proposed domain

| Granularity | Local range / updates | Continuity, publication and cost tradeoff |
|---|---|---|
| Body center | Order R | No useful magnitude reduction; stable identity but leaves the expensive domain unchanged |
| Cube face | Order R across a face | Requires seam-equivalent reconstruction; no near precision benefit without another local anchor |
| Base patch | Approximately patch span, small for dense patches | More anchor/basis records or varyings; adjacent/parent patches can round the same sample differently. Mixed large patches need a separate bound |
| Whole generation/pupil | Small near the pivot, large toward its outer mesh | Best existing publication owner, but pupil age/travel can grow the local range. A 50 m near branch alone does not bound distance from an old generation anchor |
| Refinement group | Small group around the active footprint | Requires shared anchor identity, group boundaries and invalidation; no existing proven grouping or free cache |
| Display-frame camera/grid anchor | Every near point is within 50 m of camera plus anchor-grid extent | O(1) high-precision frame metadata; not a physical-generation change. Removes within-frame multiple-anchor disagreement but not re-anchor equivalence or expensive nonlinear field work |

For numerical preflight the minimal candidate is a **high-precision local patch /
generation anchor A in body-fixed Cartesian axes**, retaining an exact FP64 A and
encoding δ = P − A locally. Avoiding a basis rotation is the cheapest initial
boundary. A tangent-basis variant was also evaluated to test whether reduced
radial range solves the identity problem. It did not. A shared camera/grid anchor
could simplify later ownership but cannot recover information absent from a
single FP32 local payload.

No production granularity is selected as a winner. The tested failure is at the
representation boundary before scheduling/ABI choice. Near tests use offsets
under 50 m from camera; total stress coverage reaches 10 km from anchor. Outside-
near tests intentionally evaluate the mathematical function and are **not claims
that production runs near H there**.

## Numerical analysis and measured preflight

`CoordinateProof.cs` is a CPU arithmetic experiment using the current deployed
`PlanetaryNaturalTerrainFamilies.EvaluateComposed(...).Near` and the current
`FloridaFacilitySupport.Region`. It is **not a GPU shader emulator**. It uses the
same declared coordinate operation sequence with CPU binary64 dot/sqrt; GPU
inverse-root accuracy, contraction and generated TES barycentrics are not assumed
identical. The numerical samples are constructed probes, not a capture of selected
production primitives or the five actual camera poses.

There are **26,208 constructed points**, including six axes, face seams/corners,
24 remote directions, Florida center and twelve support-transition anchors;
13 distance bands and 32 azimuths. **12,096** have camera-relative distance ≤50 m.
Each point is checked through four representations plus a two-anchor shared-point
comparison. Two complete runs produced identical structured results. The lossless
control passes all **26,208** points, including outside-near stress.

| Near preflight variant | Exact H inputs / 12,096 | Exact near heights | Max input-point error m | Max near-height error m | RMS near-height error m |
|---|---:|---:|---:|---:|---:|
| A + one FP32 Cartesian delta | 128 | 1,556 | 2.661268720e-6 | 8.607263546e-8 | 6.399553112e-9 |
| A + FP32 tangent-basis coordinates | 708 | 2,301 | 2.459477829e-6 | 8.606877966e-8 | 5.605380324e-9 |
| FP64 local round trip, original norm/H order | 12,096 | 12,096 | 0 | 0 | 0 |
| FP64 norm expanded around A | 10,673 | 10,858 | 2.374415991e-9 | 3.239852830e-11 | 1.998409913e-12 |
| Same point, two FP32 local anchors 64 m apart | 266 | 1,091 | 5.496562577e-6 | 1.699072689e-7 | 1.453377166e-8 |

The last row compares the two encodings with each other, not to the banked path.
Exact-match counts include zero/support-suppressed heights; input and gradient
comparisons are separate so those zeros cannot manufacture a PASS.

| Variant | Max reconstructed-point error / RMS m | Max angular error rad | Max near radial-displacement vector delta m | Max FP32 displacement-vector delta m |
|---|---:|---:|---:|---:|
| Cartesian FP32 delta | 2.669133741e-6 / 8.230463290e-7 | 4.176339526e-13 | 8.607263558e-8 | 2.665600750e-7 |
| Tangent FP32 coordinates | 2.463425991e-6 / 6.564102038e-7 | 3.860243112e-13 | 8.606877964e-8 | 2.402740046e-7 |
| FP64 lossless control | 0 / 0 | 0 | 0 | 0 |
| FP64 expanded norm | 0 / 0 | 1.570092459e-16 | 3.239819304e-11 | 0 in these probes |
| Shared point / two anchors | 5.529910648e-6 / 1.793474678e-6 | 8.627271240e-13 | 1.699072689e-7 | 2.682209015e-7 |

The displacement-vector measure holds the prepared base fixed and uses unit near
weight to isolate the physical contribution. It is not a full final TES record,
normal, projection or raster result. The real fade cannot exceed one, but exact
combined output also depends on base/max/normal/projection rounding. Reported
point error is the reconstructed evaluation point, not a change to stored
prepared vertices. Radial reconstruction error is bounded by that vector error;
the near-height difference is the measured scalar radial contribution error.

### Information-loss counterexample

At one retained remote anchor, two legal FP32 view vectors differ only in X:
`-1.659104585647583` versus `-1.6591044664382935` metres. With the same split camera,
the current reconstructed body/H inputs differ. Both encode to the same FP32
local offset:

`(4.42707633972168, 11.141589164733887, 10.595564842224121)` metres.

Their current CPU near heights are respectively `0.1226087860617272` and
`0.12260878599794128` metres. Exact IEEE bit patterns for camera, anchor, original
points, view vectors and H inputs are in `informationLossWitness` in
`numerical-results.json`.

This is an injectivity failure: **a decoder given only the same A and same local
payload cannot return both distinct authoritative answers**. Later compensated
arithmetic cannot recover bits already discarded. Keeping the original V or an
additional residual can distinguish them, but then the representation and cost
must be assessed again. This is not a theorem against arbitrary multiword local
representations, and the two view values are legal input probes, not claimed
observed barycentric values from a production frame.

### Pure reordering counterexample

The norm variant preserves P, but replaces its squared norm by the algebraic
expansion around A. It changes rounded directions and H inputs even in FP64.
Separately, **96,000 scalar domain reductions** compare `P/s` with the sum of
anchor quotient and local-delta quotient over all eight relevant cell scales.
Only **72,107** match exactly. No integer-cell difference was observed in this
bounded sample; fractions still differ, with maximum quotient delta
`7.275957614183426e-12`. This does not prove zero cell mismatches at adversarial
boundaries. The retained witnesses show exact input/output bits.

### Analytic bounds and their limits

For a correctly rounded FP32 component with |δ|≤64 m, a conservative absolute
conversion bound is 2^-18 m; the three-component conversion norm is at most
√3·2^-18 ≈ **6.608e-6 m**, plus binary64 subtraction/addition error. This deliberately
overbounds the spacing just below 64 m. Near Earth, one FP64 absolute-coordinate
ULP is at most 2^-30 m. Budgeting three such component roundings adds less than
5e-9 m in vector norm. For local basis transport, dot products and basis
reconstruction add rounding; reducing magnitude does not make them exact.

If ε bounds point perturbation and both points have norm at least r, direction
perturbation has a conservative bound 2ε/(r−ε), before normalization arithmetic.
With r near R, the Cartesian bound is order **2.1e-12 radians**. The normalized
H point can differ by approximately 2Rε/(r−ε), plus normalization/multiplication
rounding. These are analytic transport bounds, not empirical maxima or a GPU
end-to-end error certificate. The measured maxima above are smaller.

For FP64 squared-norm evaluation, the conventional bound
γ_n = n·2^-53/(1−n·2^-53) multiplies the sum of absolute terms. Direct dot and
expanded dot have different rounded sums. With |δ|≪R, a conservative arithmetic
budget γ_20·(R+|δ|)^2 is about **0.091 m²** before accounting for reconstruction
error. An equivalent length sensitivity is about **7.2 nm**. Re-normalization
and multiplication introduce additional rounding. A small nonzero bound is not
an exactness proof; the CPU counterexamples establish actual nonidentity.

KSA's local inverse-length approximation has a nonzero Taylor remainder. For a
unit anchor, squared-length perturbation m, truncating after second order has
remainder bounded by `(5/16)·|m|³/(1−|m|)^(7/2)` when |m|<1. Taking |δ|≤64/R gives
|m|≤2|δ|+|δ|² and a position-scale remainder of order **1.7e-8 m**, even before FP32
rounding and imperfect anchor normalization. This is a mathematical explanation
of why a high-quality local approximation is not automatically the exact banked
double operation sequence; it is not a measured KSA error.

The current CPU composed gradient bound is **0.7331112063**. It can bound smooth
field-value sensitivity for the unmodified family portion; facility attenuation
adds `|height|·|Δweight|`. It does not certify the changed analytic gradient,
gradient-driven normal or discrete classification at every boundary. No new
gradient/Hessian or driver arithmetic contract was accepted here.

For clip values z,w and perturbations δz,δw with |δw|<|w|, a bound on projected
depth change is `(abs(δz)*abs(w)+abs(z)*abs(δw)) / (abs(w)*(abs(w)-abs(δw)))`.
Near-plane/grazing behavior and D32 quantization prevent a universal zero-pixel
guarantee from a micrometre position bound. Any nonzero change can straddle a
rounding/coverage threshold. Zero FP32 displacement differences in the norm
variant's finite probes do not prove zero normal/depth/HDR/image differences.

### Existing numerical contracts do not automatically authorize this change

`PlanetaryNaturalTerrainFamiliesTests` permits CPU/GLSL value error ≤2e-10 m and
gradient error ≤2e-11 for its paired reference comparison. Some transport tests
permit submillimetre transport error; support contact has separate tolerances.
Those are not a general budget for replacing final-sample coordinates or allowing
new D32/HDR/image mismatches. The FP32 candidates exceed the family value/gradient
comparison tolerances in this preflight. The FP64 reassociation is smaller, but
its additional error would need to be combined with the existing CPU/GPU error,
and it does not prove the independent exact input/shared-sample/raster gates.
Repeated canonical coordinates and parent/child identity tests explicitly require
zero direction difference; same-geographic boundary height uses a 1e-12 bound.

No applicable existing contract was found that declares these changed final
inputs/raster outputs acceptable. Therefore this report does not borrow an
unrelated tolerance or silently accept approximation.

## Edge, cross-face, regional and facility result

The two-anchor counterexample directly disproves universal exact shared-point
identity for independently encoded one-FP32-delta patches. The same mathematical
point changes near input/height under a 64 m anchor shift. Tests include seam and
corner directions, but **no generated parent/child edge, actual cube-face alias
topology or L0–L17 GPU traversal was certified**. Visible cracks were not observed
or claimed; micrometre numerical disagreement is not a video finding.

The lossless FP64 control preserved identity at all constructed points. A single
shared anchor removes within-frame competing-anchor encoding, but changing that
anchor at publication/movement still requires the same identity proof. These
mechanisms do not yet establish an efficient exact decoder.

At Florida center the support suppresses near displacement, so equal zero heights
are insufficient proof. The additional **2,304 active near support-transition
probes** expose changes: Cartesian FP32 transport changes support weight by up to
2.773356789e-8 and near height by 8.607263546e-8 m. Two anchors increase the maxima
to 5.432395545e-8 and 1.699072689e-7 m respectively. FP64 reassociation changes
support weight by up to 6.991296431e-12. Thus an origin-dependent modifier answer
cannot be dismissed solely because the pad center remains flat.

Global/regional base, topology, prepared elevation, physical catalog, generation
and asset identities were not modified. Actual regional payload/physical-prepared
parity was **not tested**; the CPU proof intentionally avoids loading datasets
because the earlier coordinate gate already fails. Unchanged source is not
reported as a new GPU readiness or asset-verification PASS.

## Compiler, performance and net cost

There is no candidate TES or candidate SPIR-V. Current baseline compiler evidence
is retained in [post-M13.2 next-target](../post-m13.2-next-target/README.md):
190 VGPR, 36 SGPR, wave32, 3,094 static FP64 instructions, 7,658 total static
instructions and zero scratch for the recorded TES. These are prior driver
statistics, not new measurements. Static FP32/conversion/export counts were not
recollected, and no candidate count exists. Source and all deployed shader hashes
are verified in this package.

| Required fixed pose | Prior normal total GPU median ms, context only | Candidate terrain / total / P95 / P99 / gain |
|---|---:|---|
| Orbital | 0.98880 | NOT RUN — numerical gate |
| Factor-1 | 9.40848 | NOT RUN — numerical gate |
| Florida | 11.24696 | NOT RUN — numerical gate |
| Active refinement | 26.75952 | NOT RUN — numerical gate |
| Grazing horizon | 16.94323 | NOT RUN — numerical gate |

No isolated TES duration, occupancy, VALU/SALU, FP64-pressure or memory counter
was measured. No new AMD capture was attempted after the stop gate. Previously
available shader-info statistics do not establish those dynamic metrics.

Potentially moved work would include anchor/basis preparation and large-domain
normalization/reduction; the varying controls, warp and analytic gradients remain.
One FP32 delta adds at least 12 bytes of local coordinate payload where not already
available, plus anchor/basis metadata; a second residual adds another 12 bytes.
Whether this travels per vertex/patch or is computed transiently remains a design
choice. The exact FP64 round-trip control reconstructs the old point and executes
the old expensive evaluator, so it proves representability, not useful work
removal. No CPU/GPU preparation, bandwidth, upload, publication or net frame
saving is measured or claimed.

**Performance significance: NOT ASSIGNED.** No timing A/B is authorized by the
failed numerical gate; selecting NEGLIGIBLE/SMALL/MEANINGFUL/HIGH-LEVERAGE/MAJOR
would invent a measured gain. This is not a negative performance result.

## Dynamic transitions and warp

Slow/fast motion, live anchor changes, refinement/publication transitions,
surface-to-orbit traversal, P95/P99 and repeating spikes are **NOT RUN** for a
candidate. The two-anchor CPU comparison is a numerical counterexample, not an
animated transition proof. Existing current/incoming preparation remains intact.

A future local representation should derive the displayed anchor from the latest
accepted snapshot; intermediate simulation states must not each trigger graphics
preparation/draw. A frame/grid anchor could update without changing physical
generation identity. A generation anchor needs an explicit travel/range bound and
timely publication. No warp test was performed after the numerical stop; the
previous warp evidence is not reused as candidate acceptance.

## Validation ledger

| Gate | Result |
|---|---|
| Baseline refs / annotated tags / tracked and staged diffs | PASS |
| Restored runtime and 98 deployed shader identities | PASS |
| Numerical diagnostic Release build | PASS, zero warnings/errors after correcting an initial helper API-name error |
| Repeatability and lossless control | PASS: two identical result sets; exact FP64 control across all 26,208 points |
| One-FP32-delta exact representation | FAIL; information-loss counterexample retained |
| Tangent FP32 / FP64 norm-reordering exact input | FAIL in numerical preflight |
| Independent-anchor exact shared sample | FAIL in numerical preflight |
| Production Release native / Triangle builds | NOT RUN; no runtime instrumentation or production edits, stopped at section 6 |
| Focused Graphics / strict Vulkan / actual five-pose parity | NOT RUN; prerequisite failed |
| L0–L17 / Florida regional GPU check / asset verification | NOT RUN; prerequisite failed |
| Depth / HDR / image / selected patches / TCS / TES / factors | NOT RUN; no candidate draw or comparison counts |
| Performance A/B / CPU-GPU net cost / dynamic percentiles | NOT RUN; sections 9/16 gate not met |
| Prior evidence and production non-mutation | PASS, content hashes and empty tracked diff |
| Git whitespace and evidence JSON/source syntax | PASS at closeout |

The section 30 runtime minimum is not represented as complete validation: the
earlier explicit stop prevents advancing the unproven candidate into those gates.
There is nothing to restore in production; the only compiled binary was this
isolated CPU analysis tool. Its build outputs are disposable after consolidation.

## Decisions for Project Control

**KSA contract classification: ADAPT.** Retain the high-precision anchor/local-domain
principle, but its physical representation must preserve NovaCore's distinguishable
inputs. More residual information or exact integer-domain arithmetic is required
before claiming a transferable cheaper evaluator. This is conditional research
direction, not a proven implementation.

**H-in-TES conclusion: INSUFFICIENT EVIDENCE.** The tested reduced representations
fail; the exact control removes no expensive H responsibility. This materially
narrows the prior uncertainty, but does not prove the current stage is permanently
required or that a multiword local evaluator cannot work.

**Final refinement conclusion: RETAIN.** This proof supplies no reason to change
the current hardware tessellation owner, factors or 50 m outcome. That limited
conclusion does not freeze its future inputs or reopen culling.

**Strategic recommendation: NO PROVEN M13.3 CONVERGENCE TARGET YET.** No M13.3 title
is proposed. The immediate Project Control decision is whether to pursue an exact
multiword/integer-domain proof that preserves existing normalization/field rounding,
or explicitly specify a replacement numerical contract before evaluating a
reordered representation. The latter must independently state physical, gradient,
edge, depth and image requirements; it is not authorization to reduce quality.

If later proven, old per-sample global reconstruction/domain reduction would
retire behind the one accepted physical evaluator, with a temporary diagnostic
A/B bridge only until exact/accepted physical, raster, dynamic and net-performance
gates pass. No indefinite dual renderer is planned. No production retirement is
authorized by these numerical results.

## Reproduction and evidence lifecycle

Run `python -B docs/engineering-evidence/local-physical-domain-proof/run.py` from
the repository. It verifies the banked refs and deployed identities, builds only
`CoordinateProof.csproj` into `build/local-physical-domain-proof`, executes the CPU
preflight twice, checks repeatability/positive and negative controls, and writes
bounded results. It neither alters production nor launches Vulkan. Current Core
and Graphics assembly hashes identify the canonical CPU evaluator used.

Permanent package budget: **200,000 logical bytes**. Retain coordinate/error
analysis, CPU source/project, reproduction script, compact numerical witnesses,
hashes and closeout accounting. No per-sample raw archive, GPU buffers, screenshots,
videos, KSA source copies or compiled binaries belong in the retained package.
`closeout.json` records exact created/retained/removed/remaining logical bytes and
complete final status. Intermediate file rewrites are not claimed as an audited
lifetime disk-I/O total. This ticket stops at Project Control review.

### Cleanup limitation

Automatic approval review rejected deletion of the reviewed, hash-verified proof
build output before execution with only **“blocked by policy”** as its reason.
No retry or alternative deletion mechanism was used. **33 generated files,
1,436,946 logical bytes** remain under `build/local-physical-domain-proof`.
They are only this CPU tool's bin/obj products, including copied managed
dependencies; production deployment is elsewhere and unchanged. None were deleted.
The exact manifest is retained in `scratch-manifest.json`; bounded user cleanup
is described in [manual-cleanup.md](manual-cleanup.md). This storage remainder is
separate from the failed numerical gate and does not change the scientific result.
