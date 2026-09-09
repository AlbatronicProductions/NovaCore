# Planetary rendering

The accepted milestone and current work boundary are summarized in
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). This document defines
the production planetary-rendering responsibilities and invariants.

The current banked production baseline is **M13.6 — Prefer CPU-cached memory
for terrain residency keys**, commit `90fef759243dd67918cd556e19027159e5a5eada`,
tag `m13.6-cpu-cached-terrain-residency-keys`. **M13 is CLOSED; there is no M13.7.**

**NCSM1 / New Earth Renderer** and generation-4 physical terrain serve all six
supported Earth/Solar/Florida routes. Scene, focus, altitude, camera and Florida
initialization remain route-specific. Accepted Florida presentation and
surface/orbit continuity are preserved. Terrain-v5 global bootstrap is temporary
presentation until complete NCSM1 publication; non-Earth rendering is independent.
See [production consolidation](production-consolidation.md) for route provenance.

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
→ full physical relief and normal preparation before TES
→ conservative curved-patch planet occlusion
→ conservative screen/frustum rejection
→ compacted original index triplets
→ KSA-parity per-edge TCS factors and ordinary TES interpolation
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

The compact VS/TCS interface carries 13 scalars per control point: prepared
normal, light direction, camera-relative view, physical direction and height. TES reads frame/body
constants from immutable buffers and reconstructs material geography from the
interpolated prepared receiver. Ordinary NCSM1 rendering specializes inverse
geographic-address reconstruction away; `owners` and `boundaries` diagnostics
retain it. Diagnostic mode is immutable for a native context, and all four
NCSM1 raster-state pipelines use the same applicable specialization.

Full canonical physical relief is prepared at shared vertices before
tessellation. Ordinary TES interpolates the published camera-relative surface,
height and normal; it does not evaluate an additional near-field H displacement.
Gameplay and clearance continue to query full canonical H independently. No
authored TES height maps are currently supplied. Hardware tessellation remains:
KSA-parity per-edge factors retain the bounded 50 m near-camera refinement
contract and future geometric/material-displacement capacity. Factor-1 base
terrain remains depth-owning beyond that range, including the local horizon.
Future relief must preserve canonical physical authority and contact coherence.

## Ordinary material shading

The shared fragment module defaults to its full bootstrap/diagnostic contract.
Only an NCSM1 context with no surface diagnostic enables `ordinaryNcsm1`, making
the known owner and disabled-diagnostic selector compile-time constants. The
startup terrain pipeline and nonzero diagnostics retain their required paths;
owner/seam diagnostics also retain TES geographic-address reconstruction.
Changing diagnostic mode requires context recreation. Focus changes and physical
publications select existing pipelines without changing that contract.

Material evaluation skips procedural noise only when its computed contribution
is zero. Positive-contribution albedo, roughness, AO and shading-normal response
remain. This does not lower tessellation factors, remove accepted material detail
or change physical terrain. Material coordinates derive from the final prepared
receiver; presentation data never feeds back into canonical H or publication.

Implementation evidence: [TES specialization](engineering-evidence/tes-address-removal/README.md),
[ordinary shading](engineering-evidence/m13.2-terrain-shading/README.md),
[prepared terrain](engineering-evidence/ksa-terrain-convergence/README.md), and
[zero-contribution noise](engineering-evidence/post-m13.3-next-target/README.md).

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

## Mapped terrain memory placement

Native allocation chooses memory by consumer role, after checking the buffer's
compatible memory-type mask and requiring HOST_VISIBLE|HOST_COHERENT:

| Role | Preference | Resources |
|---|---|---|
| `TerrainGpuWorkingSet` | DEVICE_LOCAL | Prepared physical storage, regional physical staging, immutable lattice/index topology, visibility output and compacted indices |
| `TerrainRequestKeys` | HOST_CACHED | The renderer-lifetime terrain residency-key buffer only |
| `Host` | Existing first compatible coherent-host selection | Other host/control/staging allocations |

These are preferences, not new device requirements. If a preferred compatible
type is unavailable, the original compatible coherent-host type remains the
fallback. The preferred allocator maps tentative memory before publishing it as
a live bound resource. Allocation/map capacity failures release tentative state
and may retry a different compatible fallback; the same type is not retried.
Device loss, binding failures and unrelated errors remain observable and fatal.
No noncoherent-memory path or new flush/invalidate contract is introduced.

Placement does not change buffer contents, polling/writes, mappings' successful
lifetimes, descriptor ownership, barriers, fences, reuse or retirement. CPU caching
is not a substitute for synchronization. See [GPU working-data evidence](engineering-evidence/m13-final-whole-frame-causality/README.md)
and [CPU residency-key evidence](engineering-evidence/m13-final-exit/README.md).

## Physical preparation and normals

Managed code owns canonical FP64 identity and physical queries, selection,
bounded preparation scheduling, regional demand/residency identity and immutable
submission. Native Vulkan owns full-relief render preparation, reusable generation
resources, conservative visibility/compaction, bounded tessellation, indirect
rasterization, synchronization and fence-confirmed publication, reversed-Z depth,
and presentation.

Physical positions derive from canonical direction, canonical Earth radius,
and `H(bodyDirection)`. Physical normals derive from the same displaced surface.
Material lookup derives from the same body-fixed direction. The preparation and
shader paths retain CPU/GPU parity tests for height, gradient, normal, address,
and generation identity. Regional residual data must be resident and ready where
the authoritative catalog contributes before a complete incoming physical surface
can publish. Facility support/grading participates in that same physical field;
facility lighting/visibility remains presentation. Neither residency nor material
fallback may create another height authority.

## Presentation continuity

Camera translation, retained pupil movement, pupil replacement, and level
selection cannot change canonical height or the FP64 position of a tracked
body-fixed sample. The current factor-1 triangulated approximation can still
change slightly when a new lattice triangulates that physical field.

Historical C3 measurements bound the rare/full pupil rebase at approximately
2.595 m and the adjacent L14→L15 representation change at approximately 2.8 mm.
They are not a new universal M13.6 error bound. Full-relief preparation and TES
interpolation preserve the physical-versus-rendered approximation distinction;
exact equality to a fresh H query at every interpolated point is not claimed.
This is deferred representation/morph quality. It is not moving
physical terrain, duplicate geometry, translucency, or missing depth ownership.
Future morph work must preserve canonical identity and the one-owner contract.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through authoritative body orientation. Near-surface
ENU navigation and free look use the anchor; outward navigation hands off to
ordinary body-center orbit without changing physical terrain. Earth —
Fullscreen Native shares the Solar navigation path after preset initialization
and selects the same production NCSM1 owner.

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

M13 is closed at banked M13.6. The [current-state performance envelope](NOVACORE_CURRENT_STATE.md#final-accepted-performance-envelope)
retains the regional replacement limitation: universal 8.33 ms terrain performance
was not achieved. No remaining single bounded, quality-preserving >=1.5 ms
recovery qualified for continuation.

There is no M13.7. Next production-front authorization returns to Project Control.
Richer materials, representation continuity, environment and spacecraft/surface
gameplay remain future responsibilities, not work opened by this renderer contract.

Do not reintroduce radial Eye, adaptive CPU final-raster grids, dynamic
patch/stitch ownership, or a second physical surface into the accepted NCSM1
path.
Do not regenerate NCSM1 assets, expand the TES range, or weaken conservative culling without
explicit authorization and measured evidence.

Unrelated retirement findings are tracked in [the subsequent debt ledger](repository-debt-retirement.md). Deferral is not permanent acceptance.
