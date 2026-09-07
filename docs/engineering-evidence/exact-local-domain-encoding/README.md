# Exact local physical-domain encoding feasibility

**NO VIABLE LOCAL REPRESENTATION FOUND.**

The small feasibility matrix found an exact local representation, but no
representation that simultaneously preserves authority and plausibly removes
multi-millisecond final-sample work. The exact compensated residual reconstructs
the canonical point and then retains the canonical FP64 normalization and H
evaluation. The cheaper tested encodings have collisions and physical identity
failures. The CPU/cost stop gate therefore closes this precision-boundary lead for
the current M13 campaign. This is not a mathematical assertion that every possible
coordinate encoding is impossible.

No GPU prototype, production shader edit, renderer migration, performance run or
manual acceptance was performed. M13.3 is not assigned. Return to Project Control
before investigating a different major responsibility.

## Baseline and scope

| Item | Verified state |
|---|---|
| HEAD / main / origin/main | `4accf92fd080c16cc8656080aa69def3fa65db53` |
| Starting branch | `codex/m13-local-physical-domain-proof` |
| Investigation branch, created from main | `codex/m13-exact-local-domain-encoding` |
| M13.1 dereferenced tag | `fade1384c1c7df93d954e7223b1cc8f17db17f98` |
| M13.2 dereferenced tag | `4accf92fd080c16cc8656080aa69def3fa65db53` |
| Tracked and staged changes at start/end | None |
| Starting untracked evidence | conservative-pre-refinement-visibility; ksa-terrain-architecture; local-physical-domain-proof; post-m13.2-next-target |
| Reference GPU / requested future GPU resolution | RX 6800 XT / 3440 x 1440 native |

The live Windows inventory confirms RX 6800 XT, driver `32.0.21045.5002`. It
reports 2560 x 1440 on that adapter and 3440 x 1440 on the Meta/Virtual Desktop
virtual monitors. No display setting was changed, and no native-resolution GPU
measurement is claimed in this CPU-only ticket.

Only this evidence directory was added. Prior investigation packages and their
conclusions are preserved. `verification.json` records tag objects as well as
dereferenced commits, deployment hashes, source hashes and prior evidence hashes.

## KSA exact precision boundary

Current installed assembly identity matches the previously verified decompilation:
KSA `2026.9.7.5402`, production revision
`487c3f340de24c6a81037120b6d1129c045c5400`.
`KSA.dll` SHA-256 is
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
All inspected paths and full current hashes are in
`verification.json:ksaSourceProvenance`. No KSA source or assets are copied into
this package, and its CPU proof has no KSA runtime dependency.

| Responsibility | Actual representation / order | Local source reference |
|---|---|---|
| Direction anchor | CPU normalized binary64 direction; FP32 high and FP32 residual low, not an arbitrary binary64 bit container | `build/ksa-residency-reference/source/KSA.PlanetRenderer.cs:1785-1794` |
| Geographic anchor | CPU double cubemap UV; separate two-float UV components plus face index; four vec4 UBO slots for direction and UV metadata | same source; `Content/Core/Shaders/Planet/TerrainMesh/MeshDataCommon.glsl:62-70` |
| Mesh input / basis | CPU double normalized cube mesh, pivot translation, then FP32 vec4 packing; FP32 rotation-only matrix rotates its small offset into body coordinates | `build/ksa-residency-reference/assembly-source/KSA/CubeMesh.cs:191-192`; `MeshDataCommon.glsl:153-159` |
| Downstream direction | Three vec3 components: anchor high, anchor low, delta; an additional collapsed FP32 direction is for tolerant consumers | `Content/Core/Shaders/Common/PrecisionFuncs.glsl:95-144` |
| Compensation | Error-free two-float sum and FMA product primitives; multi-term dot/residual accumulation remains floating arithmetic | `PrecisionFuncs.glsl:22-62` |
| Normalization | Second-order local inverse-length residual around a unit anchor; low component kept separately; not binary64 canonical normalization | `PrecisionFuncs.glsl:117-144` |
| Noise address | Precise FP32 anchor-frequency product and FMA residual, low/delta contribution, floor/carry; ivec3 cell plus FP32 fractional coordinate | `PrecisionFuncs.glsl:261-277` |
| Cubemap address | Split UV plus local quotient correction, power-of-two scale, floor/carry; same-face usability check | `PrecisionFuncs.glsl:182-256` |
| Stage boundary | Anchor payload is supplied to terrain compute preparation/modifier consumers; finalize publishes camera-relative FP32 positions. TES receives prepared positions/normals/materials, not this complete exact-direction tuple as NovaCore H input | `PrepareModifiers.comp`; `FinalizeModifiers.comp:30-48`; `PlanetTessEvaluation.tese:26-65` |
| Cadence / reuse | Anchor and preparation UBO updated during `GenerateMeshData` for displayed billboarded terrain; retained topology/resources, not a persistent final-geometry cache | `KSA.PlanetRenderer.cs:1770-1795` and prior architecture reconstruction |

Multiple anchors are intended to represent overlapping geography. KSA handles
precision locally with compensated address reduction and shared geographic
sampling, rather than declaring an independent-anchor, bit-identical binary64
rendered-physics contract. Its comments call the pair exact; the code's two FP32
roundings do not preserve every possible 53-bit input. Exact two-sum/product
primitives do not make every subsequent sum, fraction or normalization exact.
Integer cells are per-query addresses, not a lossless integer terrain-position
payload.

The KSA-pattern probe independently tests that responsibility pattern with
favorable inputs: canonical CPU direction supplied upstream, no FP32 basis
rotation error, split anchor and FP32 delta, local second-order correction, then
binary64 reconstruction for NovaCore's existing H. Its dot accumulation is also
more favorable than a strict instruction-by-instruction FP32 shader emulation.
It is **not** a simulation or measurement of KSA's final terrain. Failure already
occurs at payload injectivity and NovaCore authority; adding the omitted KSA
FP32 noise-fraction path cannot reconstruct distinctions absent from the payload.

## Authority requirements

No new tolerance was introduced. Storage bits need not match the canonical
storage layout; decoded authoritative quantities must preserve their existing
contract. Signed zero is tracked separately to avoid calling a zero numerical
delta a bit-exact result. The exact candidate retains a three-bit zero-sign mask.

| Quantity / relationship | Required in this investigation | Existing source / scope of tolerance |
|---|---|---|
| Source point and canonical direction | Bit-exact decode and preserved normalization ordering; independently anchored answers identical | `ENGINEERING_RULES.md`, precision/determinism; `production_spherical_billboard.tese:50-53`; canonical authority tests' identical parent/child direction |
| H input | Exact after the existing direction normalization and radius scale | `planetary_natural_terrain_surface.glsl:30-34`; no accepted coordinate-representation substitution tolerance |
| Repeated height / Cartesian physical position | Exact physical output for the same input | `PlanetaryCanonicalPhysicalSurfaceAuthorityTests.cs:62-63` requires zero repeated height/position error |
| Base/near split and frequency-boundary height | Existing <=1e-12 m for that split/boundary relationship | same test, lines 62-67; not a general coordinate perturbation allowance |
| Shared parent/child geography | Zero direction and height delta | same test, lines 101-104 |
| Physical normals | Existing <=3e-8 rad for repeated/coarse-fine contract; this proof additionally records exact normals | same test, lines 62-67 |
| Cross-face / independent anchors | Identical point must retain identical physical answer; nearby points are not required to be identical | canonical geographic owner and topology-independent sampling in `PlanetaryTerrain.cs:14-27`; no additional anchor-specific error budget |
| CPU versus GLSL family oracle | Height <=2e-10 m, gradient/weight <=2e-11, orientation <=2e-13 | `PlanetaryNaturalTerrainFamiliesTests.cs:404-405`; cross-backend test, not permission to replace H coordinates |
| Preparation / raster transport | <=1e-3 m authority-to-split transport; coordinate reconstruction **exact after the explicitly prescribed FP32 local delta**; UV <=2e-12; GPU/CPU prepared height <=2e-6 m | `GpuPhysicalHeightPreparationTests.cs:73-76`; these already-defined conversion boundaries do not authorize another quantization before H |
| Regional / support / modifiers | Same loaded signed global + residual, generation-4 composition and support inputs/results | `PlanetaryTerrain.cs:19-27`; `PlanetaryPhysicalSurface.Evaluate(...M12DNaturalTerrainCandidate)`; `FacilitySupportRegion.cs:25-41` |

The CPU test follows the existing point-to-direction and direction-to-H-input
operation sequence using binary64 dot, reciprocal square root and radius scale.
It directly calls the banked CPU generation-4 H evaluator, global oracle,
regional residual oracle and support sampler. It also evaluates the canonical
near family with support attenuation. This is a CPU authority test, **not** an
emulator of GPU `inversesqrt`, interpolation, depth or rasterization. The reported
final Cartesian position is the canonical CPU radial surface `normalize(d)*(R+H)`;
it is not the post-TES coarse-triangle-plus-near-displacement raster position.

## Encodings and range analysis

Let P be the canonical point, A the anchor, D a component's maximum residual
magnitude and q a fixed-point quantum. No tangent-basis transform is needed by
the Cartesian candidates; removing this additional rounding opportunity favors
them. A hypothetical rotated basis would need another independent parity proof.

| Encoding | Range / resolution / overflow | Storage, radial and angular domain, anchor responsibility |
|---|---|---|
| Global FP64 control | 53 significant bits, floating exponent; Earth's dominant component ULP is about 9.31e-10 m, while near-zero components have much finer ULPs | 24 B point; no local bound or anchor update; keeps canonical physical operation order |
| Single FP32 control | 24 significant bits; error <= half a local FP32 ULP, not a fixed Earth-scale error | 12 B residual + 24 B exact anchor; full floating range but insufficient precision; only rechecked as rejection control |
| A: compensated two-FP32 local | Encode error-free P-A first, then hi and lo; compensate anchor reconstruction. Around residual exponent e the low-word worst-case spacing is about 2^(e-47), with rounding error about 2^(e-48). Underflow and cancellation near an axis remain constraints | 24 B residual + 24 B anchor; approximately 48 significant residual bits, not all 53 bits of P; cannot guarantee arbitrary independent anchors even at near range |
| B: exact anchor + two-FP64 residual | Error-free subtraction stores rounded delta plus subtraction error. Compensated reconstruction recovers P, then performs the unchanged canonical sequence. Finite Earth/700-km envelope has no arithmetic overflow | 48 B residual + zero-sign mask + 24 B anchor. Full canonical radial/angular/patch range of tested finite points; re-encoding changes payload but not decoded identity |
| C: signed fixed64, q=2^-40 m | Quantum 9.094947e-13 m; signed range [-8,388,608, 8,388,608) m per component. Runtime guard rejects overflow; no sampled overflow. Not fine enough near axes, despite being finer than dominant Earth-coordinate ULP | 24 B residual + 24 B anchor + scale identity. Same q for radius/tangent components. A whole diameter exceeds the range; reducing q shrinks it further. Requires anchor/scale changes across ranges |
| KSA pattern | Two FP32 unit-anchor parts + FP32 delta, approximate local normalization. Valid small-local-domain intent; it is not an all-L0-to-orbit exact numerical format | 12 B delta, 24 B direction anchor; probe also retains 8 B radial magnitude for fair point reconstruction. Actual KSA geographic UBO adds UV/face slots. Anchor/rotation/UV refresh together |

Near/factor-1/Florida/active/grazing inputs span the 40-50 m TES attenuation
boundary, fine patches, facility interior/feather/outside and regional sectors.
The CPU camera constructions include 10, 40, 49.999, 50, 1000 and 700000 m
camera offsets; these are analytical pose envelopes, not the five saved GPU
camera poses. Broad L0-L17, cube-face edges/corners and parent/child contexts are
explicitly covered. Typical face angular span decreases on the order of 2^-L;
the level number is not permission to quantize geography.

For an all-surface point and anchor, a safe component-difference envelope is
2(R+h), about 12.8 million metres for ordinary terrain. A camera-relative
representation extended to a 700-km camera must allow the additional camera
range; a surface anchor avoids that radial cost but still has arbitrary small
transverse components. Higher orbital limits would require an explicit bound.
The fixed64 range is derived, not inferred from this finite sample set.

An exact binary64 component near 2^-120 has ULP about 2^-172. Supporting a
64-m residual at that quantum would require about 180 signed bits, not 64.
Even the restricted current split-camera/FP32-view input lattice can reach
extremely small transverse components: normal FP32 alone extends to 2^-126.
There is no current minimum-axis-magnitude contract that makes a fixed64 grid
universally exact. These broad bounds are separate from the concrete near-camera
counterexample retained in the results.

An exponent plus integer significand can retain identity, but converting it back
to binary64 simply repackages the existing float representation. Adaptively
choosing a patch quantum requires proof at binade, sign, face and anchor changes;
it does not eliminate the FP64 normalization/H operations. No further speculative
formats were implemented after the bounded matrix and cost gate.

## Matrix, witnesses and deterministic results

`results.json` is the numerical authority. It contains 3,180 constructed input
observations, 8,884 anchor evaluations per encoding, all six encodings, exact
counts, maximum/RMS errors, per-level/subset counts, collision payloads and input,
height, final-position and anchor witnesses. Each run evaluates 53,304 encoded
samples. Counts are observations in the deterministic probe stream, not counts
of all possible encodings or unique equivalence classes.

See [measurements.md](measurements.md) for the generated matrix and numerical
maxima/RMS. Both the global control and B have **zero collisions**, zero
independent-anchor mismatches, and **8,884/8,884 exact** point, direction, H input,
height, final position, normal, near contribution, support, global and regional
results. This matches the error-free subtraction/reconstruction argument within
the finite domain; it is not just an inference from visually small deltas.

Topology witnesses use the production relaxed-cube projection on every face and
L0-L17, shared parameter endpoints, corners, and two surrounding plus radial
anchors. An additional 144-input/432-anchor set explicitly pairs 36 matching
edge/corner aliases on different faces, then approaches the same canonical point
with actual face-interior anchors at L17 scale. Neighboring representable X
inputs distinguish injectivity from merely
encoding the same point twice. Regional coverage includes an actual decoded
package sector boundary and the Florida-containing sectors at L8-L11; L9-L11
boundary probes have nonzero residuals. All 420 support probes have regional
residuals; 294 also have nonzero support weights. Repeated canonical-direction
points are compared across anchor contexts; no GPU traversal is claimed.

Important limits and witnesses:

- A's broad-patch collision includes two adjacent X binary64 values at a cube
  corner with different near and full H, encoded to identical two-float words.
  Its first retained height-changing collision is in `CollisionWitness`.
- Restricting attention to near range does not establish A: the valid split
  camera with a small transverse high/low coordinate and 16-m FP32 view has 254
  colliding observations, 256 H-input mismatches and 256 final Cartesian
  mismatches. Its two colliding canonical heights happen to match; their H input
  and final Cartesian bits do not. `ReachableNearCollisionWitness` retains both.
- A passes the sampled Florida support probes exactly. That local success is
  preserved as evidence; it does not negate other authority failures.
- The maximum approximately 3.357-m full-H difference is **at an exact pole**:
  a tiny transverse perturbation changes the longitude used by the current
  global elevation oracle. `MaximumHeightWitness` isolates global elevation,
  regional zero and support zero. It is not a measured near-field renderer gap
  or a regional-residency defect. No production correction is inferred here.
- The KSA-pattern Florida subset has nonzero regional/support participation and
  418/420 H-input mismatches, 361/420 full-H mismatches, maximum H delta
  6.271777728272809e-7 m. This is NovaCore H under the tested analogous encoding,
  not evidence of a KSA bug.

Three fresh process runs produce identical encoded/reconstructed stream hashes,
counts and complete structured result hashes. The canonical JSON SHA-256 is in
`verification.json:repeatCanonicalJsonSha256`; each candidate also has its own
stream digest. No timing, allocation order or unrecorded random seed enters it.

## CPU stop gate and downstream cost model

| Candidate | Classification |
|---|---|
| Global FP64 control | AUTHORITY-PRESERVING BUT NO PLAUSIBLE GPU ADVANTAGE |
| Single FP32 control | REJECTED — INFORMATION LOSS |
| A: compensated two-FP32 local | REJECTED — INFORMATION LOSS |
| B: exact two-FP64 local | AUTHORITY-PRESERVING BUT NO PLAUSIBLE GPU ADVANTAGE |
| C: fixed64 | REJECTED — INFORMATION LOSS (also bounded range) |
| KSA pattern | REJECTED — INFORMATION LOSS (also anchor/normalization disagreement) |

For the CPU-passing B, the source-level cost is concrete:

| Responsibility | Current final-sample boundary | Exact local B |
|---|---|---|
| Point reconstruction | Camera high/low sum then FP32 view promoted and subtracted: three double adds and three double subtracts, excluding conversions | Error-free decode about eight double adds/subtracts per component, 24 total; encoding adds about six per component, 18 total; zero-sign handling additional |
| Point payload | Current interpolated view is 3 FP32 scalars; camera high/low is shared | If transported, 6 FP64 residual scalars plus mask per point, 24 B exact anchor per context. Residual alone 48 B versus 12 B view. A GPU layout may need mask/alignment padding |
| Per-triangle coordinate bandwidth | 36 B for three view vectors, excluding other attributes | 144 B residuals plus masks if naively carried at corners; this is a representation accounting comparison, **not** a validated interpolation replacement |
| Buffers/descriptors | Existing prepared/TCS/TES layout | Could extend an existing buffer; no intrinsically required new descriptor, but layout, stride and interface responsibility grows |
| Normalization / H input | Full binary64 dot and inverse-length scaling, followed by current normalized-direction radius multiplication | All retained in the passing CPU path |
| Dot/cross, domain reduction, warp, interpolation, gradients and support | Existing final-sample H arithmetic | All retained; zero demonstrated expensive responsibility removed |
| Conversion / registers | Current promotions and compact interface | B uses doubles, avoids no H conversions; 12 32-bit words for residual versus 3 for view, plus reconstruction temporaries. Exact compiled peak VGPR/SGPR change unknown |
| Anchor/publication | Current production owner | Re-encode when point or anchor changes; cannot interpolate independently encoded corners and assume current per-TES-sample rounding identity |

Using B solely as a temporary **inside** TES avoids transport changes but first
requires the current P, then adds encoding/reconstruction around it. Transporting
B upstream needs an additional interpolation proof and added preparation/upload
work. Neither option removes the nonlinear canonical H evaluation.

The prior compiler evidence is context only (banked TES: 190 VGPR, 36 SGPR,
wave32, 3,094 static FP64 instructions); no new ISA measurement was made. Static
counts are not milliseconds. Preserving the exact H operation sequence with
multiword FP32 would require compensated dot/cross, quotient/fraction rounding,
normalization and subsequent field arithmetic, not just one high/low sum. No
supported lower operation count or multi-millisecond net advantage was identified.
We do not assume software multiword arithmetic beats the GPU's native FP64.

Thus no encoding is **AUTHORITY-PRESERVING AND GPU-CANDIDATE**. The mandated stop
gate is met. GPU physical/raster/workload/ISA A/B, five-pose timing, L0-L17 GPU
traversal, percentiles and net-performance classification are **NOT RUN**. They
are neither passes nor unresolved attempted tests.

## Decisions and validation

- **KSA: INTENTIONALLY DIFFER** at this rendered physical-address boundary. Its
  local precision pattern is useful for its consumers, but the tested transfer
  cannot retain NovaCore's exact canonical identity. This conclusion does not
  reject KSA's broader preparation, residency or tessellation architecture.
- **H-in-TES: RETAIN H-IN-TES** for the current banked architecture and this closed
  lead. This does not prove every future responsibility migration impossible.
- **Final refinement: RETAIN.** No new evidence supports altering it.
- **Strategic recommendation: NO VIABLE LOCAL REPRESENTATION FOUND.** Do not
  continue inventing local formats in this campaign; return to Project Control.
- Performance significance: **not measured**. No ms gain, percentile or frame-rate
  improvement is claimed.

Diagnostic Release build: PASS, zero warnings/errors. Three deterministic CPU
runs: PASS. All 49 Debug and 49 Release deployed shader hashes and the four
recorded binaries per configuration match the prior verified deployment. Global
and regional production assets are loaded and hashed, never regenerated.
Production tracked source and staged diff remain empty. `git diff --check`: PASS.
The proof project references current Release assemblies without rebuilding any
production project. The bounded compiler error encountered while writing the
diagnostic was corrected before the recorded successful repeat runs.

Reproduce with `python -B docs/engineering-evidence/exact-local-domain-encoding/run.py`.
It builds only this diagnostic, runs three CPU processes, refreshes bounded
results/provenance, and checks deployment identity. It creates no GPU captures
and performs no deletion. `verify.py` validates the retained package without
rebuilding or generating scratch.

## Evidence lifecycle

Permanent package budget: 256 KiB. Retain the report, measurements, exact
encoding/test source, reproduction and verification scripts, compact results,
runtime/KSA provenance, and bounded cleanup inventory. No screenshots, raw GPU
attachments, KSA source, production binaries or terrain assets are copied here.
`closeout.json` records final byte accounting, cleanup outcome and full status.

The previous proof's **33 files / 1,436,946 bytes still exist**, verified against
their prior manifest. They are accounted separately and were not touched or
subjected to a retry of the previous rejected deletion. New scratch is confined
to `build/exact-local-domain-encoding`; no production runtime depends on it.
All 35 new scratch files, 1,489,393 logical bytes, were individually checked for
path containment, hashes, ignored status and absence of reparse-point parents,
then deleted. The empty directory structure may remain. Read-only verification
after deletion confirms the retained package, prior evidence, production
deployment and asset hashes. No diagnostic was regenerated after cleanup.
