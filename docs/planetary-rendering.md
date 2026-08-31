# Planetary rendering

## Authority boundary

Celestial simulation publishes immutable body center, radius, orientation, and
material identity. Rendering cannot modify them. Earth surface geography is
body fixed and uses the right-handed convention +Y north, +X at longitude zero,
and east-positive longitude toward -Z.

Surface-critical values are evaluated in body-relative FP64. Root-relative
camera subtraction occurs before float transport. A patch's render position is
derived from canonical body-fixed direction and physical height; screen-space
selection never changes geographic identity.

## Physical surface

`PlanetaryPhysicalSurface` is the shared CPU/GPU contract for canonical relaxed
cube-sphere address, signed terrain-v5 global elevation, NCCUBE2-v3 regional
residual, deterministic modifier, final physical height and gradient, physical lighting normal, and
material classification inputs.

The exact Earth formula is `max(0, signedGlobal + regionalResidual +
physicalModifier)`. The clamp occurs after residual recomposition. M12B's
physical modifier is selected by a deterministic planet-wide body-fixed biome
blend with at most four normalized contributors. It combines rolling, rocky,
dune, coastal/wetland, and glacial families; a bounded near field is evaluated
at tessellated vertices. Florida M12
uses a 4 km source-edge feather plus the common missing-neighbor record feather;
neither depends on camera state or GPU residency identity.

The surface generation is part of patch identity. Visual normal/material
detail is frequency-filtered presentation and cannot affect collision height.

## Global coverage

`earth-surface-v5` is a complete six-face L0-L2 `.nccube` hierarchy. Its roots
are renderer-lifetime resident. Parent patches remain authoritative while
complete child quartets prepare. Global albedo, elevation, land classification,
and clouds share patch-aligned records and canonical cross-face gutters.

The global path is currently the complete distant/orbital representation and
the coverage fallback during dynamic generation preparation.

## Dynamic multiscale hierarchy

`PlanetaryDynamicAnchoredSurface` selects canonical body-fixed patches from
projected patch size and terrain error inside a retained, camera-centered
body-local neighborhood. Camera direction is not part of physical residency;
conservative GPU visibility handles the current view. Its API is not restricted
to a site or near-surface topology.

Production defaults are 256 target screen pixels, maximum level 20, acquisition
below 700 km, release above 800 km, a 6,144-patch authoritative/visible limit,
and an 8,192-slot cache. The additional 2,048 slots are bounded transactional
and reuse headroom; they are not additional visible demand capacity. A
horizon-informed 32-64 km physical neighborhood is retained with a bounded
128-1,000 m recenter threshold, so stationary camera rotation cannot discover
or retire nearby physical terrain.

The selector balances neighbors and retains deterministic ordering. Cache keys
include body, terrain version, physical-surface generation, face, level, x, and
y. Camera motion changes demand only.

## Cache and publication

Patch state advances through requested, preparing, resident, ready,
authoritative, cached, retiring, or failed. A visible generation is atomic:
every selected patch must have geometry, physical-surface data, material data,
and synchronization complete. Native code validates slot generation and GPU
publication before accepting ownership.

If preparation is incomplete or fails, the global owner remains complete. A
published dynamic patch suppresses the exact corresponding global pixels, so
there is one visible owner without depth overlap. Retirement restores global
ownership atomically.

The cache and upload queues are fixed-capacity. Current authoritative, incoming,
predicted, and required-parent patches are protected; deterministic LRU eviction
selects cold unprotected entries. One bounded generation coordinator distributes
descriptor and payload preparation across at most eight workers, then hands one
immutable generation to the render thread. The previous complete generation
remains authoritative until the incoming generation is fully resident. Selection
and steady-state updates perform no file I/O or managed allocation. Heavy payload
bytes resolve through tracked manifests into the content-addressed runtime cache.

## Material and normal continuity

Shader sampling resolves material address from the represented body-fixed
direction. Canonical gutters cross cube-face boundaries using the same relaxed
cube mapping as geometry. Parent/child edges coordinate morph age and neighbor
constraints. Physical normals use the final composed displaced surface in a
canonical body-space tangent frame with fixed physical sampling semantics,
independent of screen-space mesh density. The regional BC5 field is diagnostic
and is not reapplied over the generated production normal.

Required continuity includes exact shared geography, bounded physical height,
outward winding, coherent material address, normal continuity, balanced mixed
LOD edges, and zero ownership holes or overlap.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through the body's authoritative orientation. Near the
surface, ENU navigation and free look use that anchor; outward navigation hands
off to ordinary body-center orbit without changing the physical camera path.

The final Earth camera origin is checked against the CPU physical-surface oracle
after input and pose reconstruction. Invalid ordinary input is corrected
radially to the terrain-safe exterior rather than terminating the runtime.

## GPU terrain refinement and native submission

CPU selection publishes compact canonical patch descriptors rather than a
final-raster vertex pool. Native Vulkan reuses one 4x4-quad base topology and 16
mixed-LOD stitch index templates. Tessellation control conservatively rejects
only wholly invisible coarse triangles and quantizes projected edge demand to
bounded factors 1/2/4/8/16 with a 16-pixel target. Tessellation evaluation
reconstructs canonical relaxed-cube geography, samples physical height after
refinement, displaces the final vertex, and derives its physical normal from the
final displaced surface at a fixed body-space sampling radius.

The 768-byte frame submission carries dynamic patch descriptors, active and
GPU-ready generations, publication flags, and one camera-relative spherical-
billboard frame. Native Vulkan owns the reusable base geometry, bounded
descriptor and indirect buffers, tessellation and fragment pipelines,
synchronization, HDR target, reversed-Z depth, and tone mapping. Descriptor and
pipeline creation are not per-frame work.

## Diagnostics and validation

Current validation covers canonical face-edge/corner identity and outward
winding, parent/child geographic correspondence, CPU/GPU physical height and
normal parity, projected-error selection and hysteresis, cache bounds,
transactional publication, regional-payload dependency, camera continuity, and
Solar/Earth Vulkan traversal with `VK_LAYER_KHRONOS_validation`.

M12 adds opt-in `NOVACORE_SURFACE_DIAGNOSTIC` values `global-height`,
`regional-height`, `residual`, `final-height`, `physical-modifier`,
`regional-control`, `material-id`, `regional-mip`, `regional-residency`, and
`regional-boundary`. M12B adds `biome-id`, `biome-blend`, `modifier-family`,
and `near-physical`. They add no production work when disabled.

The subdivision diagnostic is the single launcher-exposed developer terrain
view. Normal Earth and Solar scenes use production ownership.

## Current limits and next study

The global payload remains shallow outside regional coverage. Florida M12 is
the first approximately 10 m-class source region; its deterministic control
classes are a foundation rather than a surveyed global biome product.
Additional regional tiles and independent land-cover data remain future data
work. Fragment/material/normal optimization, compute triangle compaction, and
movement-time selector tuning remain measured future work.

The current global path remains a specialized distant representation until
measurement shows whether the dynamic hierarchy can efficiently extend through
orbital scales. That later study must compare CPU selection, GPU cost, draw
count, residency, first-frame coverage, projected error, visual quality, and
handoff complexity without reopening the accepted 11B-7H1 near-field baseline.
