# Planetary rendering

The accepted milestone and current work boundary are summarized in
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). This document defines
the production planetary-rendering responsibilities and invariants.

The current banked baseline is **M13.1 — NCSM1 TES hot-path removal**, commit
`fade1384c1c7df93d954e7223b1cc8f17db17f98`, annotated tag
`m13.1-ncsm1-tes-hotpath`. It preserves the accepted **M12D-P2S5H — Earth Route
Convergence** architecture and Florida result. M13.2 ordinary terrain shading
specialization is an unbanked candidate pending Project Control review.

The accepted Earth contracts below apply to every supported Earth route.

The **banked P2S5H production baseline** uses NCSM1 and generation 4 for all six
supported Earth/Solar/Florida launcher routes, including **New Earth Renderer**.
Scene, focus, altitude, camera and Florida initialization remain route-specific.
Florida launcher-based manual acceptance **PASSED** before consolidation.
The superseded dynamic anchored owner, stitch/coverage draw, local texture-demand
transport and its investigation drivers have been retired. Generation-3 numerical
oracles and independent development scenes remain outside production Earth routing.
Terrain-v5 global bootstrap remains only until complete NCSM1 publication;
non-Earth presentation remains independent. M13.1 retains these ownership contracts.

See [production consolidation](production-consolidation.md) for reachability, retained responsibilities and validation.

## Authority boundary

Celestial simulation publishes immutable body center, radius, orientation, and
material identity. Rendering cannot modify them. Earth geography is body fixed
in the right-handed convention +Y north, +X at longitude zero, and
east-positive longitude toward -Z.

Surface-critical values are evaluated in body-relative FP64. Root-relative
camera subtraction occurs before float transport. Screen-space selection,
topology density, pupil movement, residency, and publication may change the
representation, but never canonical geographic identity.

## One physical surface

Every rendered or queried geographic point consumes the same canonical
physical-height authority, `H(bodyDirection)`. The composition includes signed
global elevation, regional residual wherever the authoritative catalog contributes, accepted deterministic physical
modifiers, and the final non-negative clamp. It owns:

- FP64 body-fixed geometry and deterministic displacement;
- physical gradients and normals where physical truth is required;
- collision, camera clearance, and physical queries;
- identity independent of camera, topology level, pupil, cache, generation,
  and time.

Material presentation may consume cheaper deterministic derived classification
and filtering. No presentation value may feed back into height, physical
normals, collision, or ownership.

`earth-surface-v5` and `earth-florida-m12` are distribution and sampling inputs.
The global package supplies macro material/classification data and the regional
package improves physical and presentation fidelity where installed. Neither is
an independent visible terrain owner.

## Production spherical-billboard representation

Earth production uses 18 immutable NCSM1 scale-mesh levels, L0 through L17.
Each level has stable lattice, region, index, adjacency, scale, and hash
identity. Runtime validates the production manifest, retains a bounded set of
scale resources, and does not regenerate, copy, or re-upload immutable topology
when a resident level is revisited.

Projected geometric error selects the active level. Hysteresis prevents
unstable adjacent-level oscillation. A camera-facing pupil resolves an exact
signed cube-lattice origin, retains its generation below movement thresholds,
snaps deterministically when required, and reuses matching physical samples
across same-level movement and adjacent-level transitions.

The production chain is:

```text
canonical body-fixed physical terrain
→ immutable persistent NCSM1 scale resource
→ retained/snapped pupil
→ physical position and normal preparation
→ conservative curved-patch planet occlusion
→ conservative screen/frustum rejection
→ compacted original index triplets
→ KSA-parity per-edge TCS factors and bounded TES refinement
→ indexed indirect raster
→ fence-complete atomic publication
```

Topology density controls the piecewise-linear approximation presented to the
GPU. It does not own or redefine `H(bodyDirection)`.

## Visibility, compaction, and TES

Pre-TES planet occlusion encloses each displaced curved patch conservatively;
it does not infer visibility from a planar triangle-facing test. Screen and
frustum rejection likewise remove only work proven unable to contribute. The
surviving triangle stream is compacted into an indexed-indirect draw payload.

The accepted P2S5G tessellation interface compaction bounds the TCS user output
to 13 scalars per control point, down from 45. It carries only physical normal, lighting
direction, view vector, physical direction, and height into TES. Frame/body
constants are read from the same existing immutable buffers in TES, and unused
vertex-stage addresses are no longer calculated or forwarded. This removes
redundant per-patch transport without changing physical evaluation, edge factors,
the fragment interface, culling, resource lifetime, or draw responsibility.
See [the measurement and historical closeout record](M12D-P2S5G-workload-investigation.md)
for validation and its limits. The deterministic +89 TES invocation difference
is an accepted bounded invocation-accounting consequence (classification C),
with bit-identical measured outer/inner factors. It is not increased refinement
or changed tessellation semantics.

Banked M13.1 specializes inverse geographic-address
reconstruction at native context creation. Ordinary NCSM1 rendering uses the
default-false specialization; `owners` and `boundaries` diagnostics select the
same shader with that specialization enabled. The immutable context diagnostic
mode drives all four existing raster-state pipelines. This keeps one physical
algorithm and one deployed TES binary, with no per-frame diagnostic branch.
Ordinary material coordinates still come from the final physical receiver.
See the [parity and performance evidence](engineering-evidence/tes-address-removal/README.md)
for the implementation measurements; M13.1 remains authoritative.

The unbanked M13.2 candidate also specializes ordinary NCSM1 fragment shading.
The shared fragment module defaults to its full bootstrap/diagnostic contract.
Only an NCSM1 context with no surface diagnostic enables `ordinaryNcsm1`, making
the already-known owner and disabled-diagnostic selector compile-time constants.
The separate startup terrain pipeline retains the default; every nonzero surface
diagnostic retains the full NCSM1 fragment path. Owner/seam diagnostics also retain
M13.1's enabled TES geographic-address specialization.

Diagnostic mode is immutable for a native context. Changing it requires context
recreation; focus changes and physical/pupil publications select existing owner
pipelines and update data without changing that contract. All four NCSM1 raster
states receive the same fragment specialization. Declared inter-stage interfaces
and descriptor layouts remain complete and unchanged; the compiler may eliminate
five unused ordinary inputs/exports while retained variants still consume them.
Physical H, 50 m refinement, material synthesis, lighting and facility visibility
are unchanged. See [M13.2 candidate evidence](engineering-evidence/m13.2-terrain-shading/README.md)
for exact parity, pipeline/transition gates and the Project Control review boundary.

TCS/TES provides bounded near-camera raster refinement. The configured physical
range is 50 m, the evaluation shader exits before displacement work outside the
range, and exact per-edge tessellation factors derive the interior factor. That range is not a
terrain or ownership radius: factor-1 base geometry remains present and
depth-owning everywhere outside the refined footprint, including across the
local horizon.

The P2S5C3 stabilization restored the 50 m contract after an accidental
50,000 m value and replaced the old planar pre-TES facing assumption with the
curved conservative bound. It also aligned physical preparation with the
canonical Earth radius. The accepted traversal no longer reproduces the broad
missing-Earth band or device-loss workload associated with those defects.

## Persistent scale resources and transactional GPU ownership

The runtime keeps immutable NCSM1 topology resources resident in a bounded
current/incoming pool and reuses working buffers by capacity. A level revisit
selects the existing resource rather than rebuilding or uploading topology.
The runtime has one current generation and at most one incoming generation.
The current generation remains the sole Earth owner while bounded asynchronous
work prepares the replacement. Publication requires complete:

- topology and pupil identity;
- physical positions and normals;
- conservative cull and compact results;
- compacted indices and valid indirect command;
- GPU visibility of all required data;
- fence completion.

Publication is atomic at a frame boundary. A fully prepared zero-visible
generation with consistent counters and a valid no-op indirect command may
publish as the sole owner; later per-frame culling creates normal visible work
from that generation when Earth re-enters view. Superseded resources retire only
after their use is safe. An incomplete incoming generation cannot suppress the
current surface. Production diagnostics require one owner, zero overlap, zero
unowned frames, and zero stale-generation draws.

Earth presentation authority is decided from current body focus and production
eligibility, not from whether Earth resources remain resident. This prevents
other bodies from inheriting Earth geometry or material state. The
current/incoming relationship is a lifecycle for one production
spherical-billboard renderer. It is not the retired global-versus-anchored patch
ownership split.

## Physical preparation and normals

CPU code owns FP64 canonical selection and physical evaluation, bounded
preparation scheduling, residency identity, and immutable publication. Native
Vulkan owns reusable generation resources, conservative culling and compaction,
bounded refinement, indirect rasterization, reversed-Z depth, and presentation.

Physical positions derive from canonical direction, canonical Earth radius,
and `H(bodyDirection)`. Physical normals derive from the same displaced surface.
Material lookup derives from the same body-fixed direction. The preparation and
shader paths retain CPU/GPU parity tests for height, gradient, normal, address,
and generation identity.

## Presentation continuity

Camera translation, retained pupil movement, pupil replacement, and level
selection cannot change canonical height or the FP64 position of a tracked
body-fixed sample. The current factor-1 triangulated approximation can still
change slightly when a new lattice triangulates that physical field.

The accepted measured bounds are approximately 2.595 m for a rare/full pupil
rebase and approximately 2.8 mm across an adjacent L14→L15 representation
change. This is deferred representation/morph quality. It is not moving
physical terrain, duplicate geometry, translucency, or missing depth ownership.
Future morph work must preserve canonical identity and the one-owner contract.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through authoritative body orientation. Near-surface
ENU navigation and free look use the anchor; outward navigation hands off to
ordinary body-center orbit without changing physical terrain. Earth —
Fullscreen Native shares the Solar navigation path after preset initialization
and now selects NCSM1 in the banked P2S5H baseline.

## Diagnostics and acceptance

Focused diagnostics cover canonical geography, winding, radius, height and
normal parity, pupil identity/reuse, owner state, publication generation,
curved-patch visibility, compacted indices, indirect draw validity, TES range,
and CPU/GPU timing. Diagnostic modes must not alter production authority when
disabled.

P2S5F physical acceptance exercised native 3440×1440 orbit-to-near-surface
descent, all 18 representative levels, low-altitude horizon rotation, sustained
L17 pupil snaps, repeated L16/L17 reversals, retreat, and re-approach. Manual
Desktop acceptance remains required for future rendering milestones; a static
benchmark alone is not player-facing acceptance.

## Current development boundary

P2S5H Earth-route convergence is production accepted and banked. All ordinary
routes select NCSM1. Repository-debt retirement precedes the next performance
campaign; this pass does not change the accepted renderer.

The NCSM1 production topology/runtime, moving pupil, canonical physical
authority, persistent GPU scale lifecycle, culling/coverage, KSA-parity bounded
TES, zero-visible re-entry, body authority, and atomic ownership are stable.
Terrain presentation and material quality, finer pupil/re-triangulation morph
quality, atmosphere/cloud/environment rebuilding, richer data coverage,
spacecraft/surface gameplay, and later specialized-renderer retirement or
promotion remain in development.

Do not reintroduce radial Eye, adaptive CPU final-raster grids, dynamic
patch/stitch ownership, or a second physical surface into the accepted NCSM1
path.
Do not regenerate NCSM1 assets, expand the TES range, or weaken conservative culling without
explicit authorization and measured evidence.

Unrelated retirement findings are tracked in [the subsequent debt ledger](repository-debt-retirement.md). Deferral is not permanent acceptance.
