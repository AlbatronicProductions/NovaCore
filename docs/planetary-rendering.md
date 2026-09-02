# Planetary rendering

The accepted milestone and current migration decision are summarized in
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). This document defines
the detailed planetary-rendering responsibilities and invariants.

## Authority boundary

Celestial simulation publishes immutable body center, radius, orientation, and
material identity. Rendering cannot modify them. Earth geography is body fixed
in the right-handed convention +Y north, +X at longitude zero, and
east-positive longitude toward -Z.

Surface-critical values are evaluated in body-relative FP64. Root-relative
camera subtraction occurs before float transport. A patch render position is
derived from canonical body-fixed geography and physical height; screen-space
selection, residency, and representation never change geographic identity.

## One physical surface

Every representation of a geographic point must consume one canonical
geographic/physical-height authority. The canonical composition includes signed
global elevation, optional regional residual, deterministic accepted physical
modifier, and the final non-negative clamp. Regional data changes fidelity, not
ownership or identity.

Global/coarse and anchored/refined geometry may evaluate different sample
densities or prepared levels, but they may not independently produce different
underlying heights for shared geography. Physical authority owns:

- FP64 geometry and deterministic displacement;
- physical gradients and normals where physical truth is required;
- collision, camera clearance, and physical queries;
- body-fixed identity independent of camera, LOD, cache, and time.

Material presentation may consume cheaper deterministic derived classification
and filtering. Candidate D demonstrates that fragments need not reconstruct the
entire FP64 physical field. No presentation value may feed back into physical
height or become a second authority.

## Accepted migration representations

`earth-surface-v5` is complete six-face L0-L2 relaxed cube-sphere coverage. Its
roots are renderer-lifetime resident and it remains the conservative distant
and migration fallback.

`PlanetaryDynamicAnchoredSurface` selects canonical finer patches from
projected size and physical error inside a retained camera-centered body-local
neighborhood. Its identity is
`body / terrain-version / physical-surface-generation / face / level / x / y`.
Its API is not restricted to Florida or permanently to near-surface use.

Current limits are 6,144 visible/demanded patches, a 12,288-slot managed cache,
and 16,384 native slots, with
256 target pixels per patch, maximum level 20, acquisition below 700 km, and
release above 800 km. The retained 32-64 km neighborhood uses a bounded
4-32 km recenter threshold. Camera direction affects final GPU visibility,
not body-fixed residency identity.

This two-representation arrangement is migration architecture, not permission
for two physical surfaces and not a predetermined permanent renderer split.

## Transactional ownership

Patch state advances through requested, preparing, resident, ready,
authoritative, cached, retiring, or failed. The previous complete
representation remains authoritative while the incoming generation prepares.
Replacement is atomic only after every required patch has complete geometry,
physical and material data, cache residency, draw payload, synchronization, and
GPU acknowledgement.

Exactly one representation owns each visible surface pixel. An incomplete
incoming generation may not suppress its predecessor. Retirement restores
complete ownership atomically. Skirts, depth bias, overlapping opaque copies,
material masking, and fallback flat geometry are not substitutes for this
contract.

The cache and queues are fixed-capacity. Current, incoming, predicted, and
required-parent entries are protected; deterministic LRU may evict only cold
unprotected entries. One bounded coordinator uses at most eight workers and
publishes an immutable generation to the render thread. Selection and settled
updates perform no file I/O or managed allocation.

## GPU terrain responsibility

The accepted 11B-7H1/M12B foundation keeps CPU work at canonical selection,
residency, bounded preparation, compact descriptors, and publication. CPU does
not build final-raster density.

Native Vulkan owns reusable base topology, stitch/index templates, descriptor
and indirect buffers, screen-space refinement, physical displacement, final
physical normals, visibility, synchronization, HDR, reversed-Z depth, and tone
mapping. Descriptor and pipeline construction is not per-frame work.

The target coherent flow is:

```text
select representation
-> prepare canonical height and modifiers
-> displace
-> generate final normals
-> cull/compact conservatively
-> apply bounded screen refinement
-> draw
```

Preparation results should be published and reused rather than recomputed in
vertex, TES, and fragment stages without measured justification. Conservative
triangle compaction and TES-range changes remain measurement-gated; KSA use is
not sufficient evidence by itself.

## Frequency continuity decision

Representation-independent physical-frequency metadata is retained. A
representation must reject or smoothly attenuate frequencies it cannot sample.
The retained contract includes wavelength-versus-spacing representability, C2
attenuation, deterministic body-fixed evaluation, CPU/GLSL parity, zero shared
boundary height and position delta, and bounded normal continuity.

M12C-4B's runtime bridge is rejected and removed. Edge/guard masks, raster-ABI frequency
transport, repeated per-edge contextual physical evaluation, and
`PhysicalFrequencyBoundaryMask` must not be used to reconcile unrelated base
height authorities. The surviving frequency context is a representation-
independent representability/C2 contract, not ownership transport.

## Procedural physical detail

The generation-3 fixed-axis carrier family is not the long-term near-field
primitive. Phase-warped projected sine carriers retain planet-wide periodic
identity and can still form ribs and crossing grids.

Future NovaCore-native detail should use precision-preserving body-fixed local
domains, deterministic hashed cells, decorrelated local frames, continuous
neighbor blending, multidirectional and multiscale structure,
derivative-aware fields, erosion/domain warping, and modifier-specific
composition. Algorithms, constants, art direction, and data remain original to
NovaCore.

## Material and normal continuity

Material address derives from represented body-fixed direction. Canonical
gutters cross cube-face boundaries with the geometry mapping. Parent/child edges
coordinate morph and neighbor constraints. Production physical normals derive
from the accepted final displaced surface with stable body-space sampling; the
regional BC5 field remains diagnostic.

Candidate D is retained as an accepted presentation optimization. Its
workload-equivalent fixed-pose result reduced GPU time from 37.492 ms to
20.845 ms. It does not own geometry, displacement, physical normals, collision,
or material/biome authority.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through the authoritative body orientation. Near the
surface, ENU navigation and free look use the anchor; outward navigation hands
off to ordinary body-center orbit without changing the physical camera path.

The accepted M12C-4A/4A2 behavior is retained. Earth Fullscreen Native uses the
same production Solar navigation path after preset initialization. Terrain
architecture must not compensate for camera behavior.

## KSA reference responsibility

For analogous planetary work, inspect the current installed KSA subsystem and
record the problem, pipeline stage, prepared data, CPU/GPU boundary, cadence,
reuse, synchronization, continuity rule, and cost bound. Classify the
NovaCore-native choice as adopt responsibility, adapt responsibility, or
intentionally differ with measured evidence.

KSA is architectural evidence only. NovaCore does not copy KSA source, shaders,
assets, constants, or proprietary data and has no runtime dependency on its
installation.

## Diagnostics and acceptance

Focused diagnostics cover canonical geography, winding, height and normal
parity, owner identity, depth/stencil, publication generation, cache state,
refinement workload, and CPU/GPU timing. Diagnostic modes must not alter
production authority when disabled.

Two acceptance classes are mandatory:

- **Microbenchmark:** deterministic fixed pose, workload fingerprint, CPU/GPU
  timing, and physical/parity invariants.
- **Physical player:** orbital approach, representation activation, low
  altitude, grazing horizon, lateral movement, 360-degree rotation, retreat,
  re-approach, and relevant 1x/moderate/high permitted warp at 3440x1440.

A microbenchmark pass is not player-facing acceptance. Manual physical Desktop
testing remains an authoritative rendering milestone gate.

## Phase 1 automated candidate

Global/fallback and anchored/refined geometry now consume the same required
body-fixed elevation oracle, regional residual, and canonical modifier
authority. Terrain-v5 remains the complete global owner's macro material and
classification payload, but it no longer displaces global production geometry
or reconstructs a separate global physical normal in the fragment stage.
Normals are prepared from each density's canonical displaced geometry.

The complete global owner remains visible while an anchored generation is
prepared, resident, indexed, covered, GPU-ready, and acknowledged; the existing
anchored-first/stencil-zero global fill still provides one raster owner per
pixel. Automated authority and canonical-workload validation use the intentional
generation-3 operation-order hash. Manual physical replay accepted the primary
Phase 1B moving-reference hitch correction. The rejected runtime guard/edge
bridge and temporary material profiling bypasses are absent from the closeout
candidate. Stable 60 FPS remains open at approximately 19.94-20.00 ms canonical
production GPU p95.

Later work replaces fixed-axis procedural synthesis, prepares/reuses immutable
simulation and orbit presentation, and measures triangle compaction and
tessellation-range changes. The current shallow global source and regional data
coverage remain fidelity limits, not reasons to create another physical
authority.
