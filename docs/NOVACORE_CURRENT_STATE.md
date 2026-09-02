# NovaCore current engineering state

This is the primary current-state and architecture-handoff document. Durable
engineering rules live in [ENGINEERING_RULES.md](../ENGINEERING_RULES.md), and
the detailed renderer contract lives in
[planetary-rendering.md](planetary-rendering.md). Historical milestone records
remain useful provenance but do not override this file.

NovaCore is a deterministic C# space-simulation core with a native Vulkan
renderer. Simulation owns celestial identity, exact time, physical state, and
reference-frame transforms. Graphics consumes immutable root-resolved snapshots
and performs FP64 camera-relative subtraction before GPU transport.

## Accepted foundation

The previous banked renderer recovery point is M12C Phase 1B at commit
`dccb101af1053b9e44f9e12d5738ca064488efe5`, tagged
`m12c-phase1b-stable`. The accepted M12D foundation extends that renderer with
topology-independent physical-content work and a structural proof of the
selected long-term geometry architecture:

- FP64 body-fixed physical authority and deterministic geography;
- transactional terrain ownership and complete fallback coverage;
- CPU authority limited to canonical identity, residency, bounded preparation,
  and immutable publication rather than final-raster density;
- GPU near-field displacement, bounded screen refinement, final physical
  normals, and rasterization;
- canonical CPU/GPU height, gradient, normal, and address parity;
- immutable `SurfaceAnchor` identity, terrain clearance, free look, and stable
  bidirectional body-center/surface navigation;
- `earth-surface-v5` global distribution plus optional
  `earth-florida-m12` NCCUBE2-v3 regional data;
- bounded dynamic selection, cache residency, background preparation, and
  generation publication.

M12D milestone status is:

- P2A canonical hashed-cell natural terrain field: **PASS**;
- P2B multiscale natural terrain families: **PASS**;
- P2C physical/preparation foundation: **accepted**;
- P2C2 opt-in renderer and material-performance investigation: correctness,
  parity, generation propagation, and value-preserving material-noise work are
  accepted; the candidate remains opt-in and does not replace generation 3;
- P2C3 prepared/MIP material architecture: **paused** until the spherical
  billboard migration has stable physical and workload evidence;
- P2S1 spherical-billboard architecture decision: **PROCEED**;
- P2S2 immutable spherical-billboard topology proof: **PASS**;
- P2S3 runtime/GPU integration: **not begun**.

The shallow global representation remains the conservative migration fallback.
It is not permission for global and refined owners to define different physical
surfaces.

## Hard planetary-surface invariants

Every representation of the same planetary geography must derive geometry from
one canonical geographic/physical-height authority. Coarse, global, anchored,
and refined presentations may sample that authority at different densities, but
they may not independently disagree about the underlying surface.

A previous complete representation remains authoritative while its replacement
prepares. Replacement occurs only after complete prepared geometry, required
physical and material data, cache residency, draw payload, and GPU publication
are ready. There must be exactly one visible pixel owner, no unowned region, and
no presentation path that silently becomes a second physical authority.

Physical authority includes:

- FP64 body-fixed geometry and deterministic displacement;
- physical normals where physical truth is required;
- collision, camera clearance, and physical queries;
- stable geography independent of camera, patch, residency, time, or renderer.

Material presentation may consume cheaper deterministic derived information.
It may not redefine height, collision, ownership, or geographic identity.

## Current terrain representation

`earth-surface-v5` supplies complete L0-L2 relaxed cube-sphere coverage.
`PlanetaryDynamicAnchoredSurface` selects finer canonical body-fixed patches
inside a retained physical neighborhood. Patch identity is
`body / terrain-version / physical-surface-generation / face / level / x / y`.

Visible demand is bounded to 6,144 patches and the managed cache to 12,288 slots
(16,384 native slots). The
current target is 256 pixels per patch with 700 km acquire and 800 km release
hysteresis. The body-local retained neighborhood is 32-64 km with a bounded
4-32 km recenter threshold. Camera orientation affects GPU visibility, not
physical residency authority.

`earth-florida-m12` supplies provenance-tracked USGS-derived R16 residual,
BC7 albedo, BC5 diagnostic normal, and R8 control records over roughly
98 x 111 km in a complete L8-L11 hierarchy. Regional data increases fidelity;
it is an input to the canonical surface rather than a separate visible owner.

This patch renderer is transitional migration architecture. It remains the
production and recovery renderer until the spherical-billboard path passes its
GPU, physical-continuity, performance, and Desktop acceptance gates. Do not
optimize the patch renderer merely to extend its lifetime, and do not retire it
before its accepted replacement is complete.

## Selected spherical-billboard architecture

The selected long-term planetary geometry representation is:

```text
pre-generated nested spherical-billboard topology
-> camera-facing tangent-snapped pupil
-> canonical H(bodyDirection)
-> GPU physical preparation
-> GPU conservative triangle culling/compaction
-> bounded near-camera tessellation
-> body-fixed material presentation
```

P2S2 supplies three deterministic structural-proof artifacts:

| Level | Vertices | Triangles | Topology hash |
| --- | ---: | ---: | --- |
| Orbital | 162 | 320 | `0x46714C6571D73AE6` |
| Intermediate approach | 642 | 1,280 | `0xAAC90882FE7F656E` |
| Surface pupil | 2,562 | 5,120 | `0xBBBACB117D88439D` |

The approximately 245,868-byte proof library is immutable at runtime,
deterministic, closed, watertight, outward wound, adjacency-authored, exactly
nested coarse-to-fine, and stitch-free. These three meshes prove structure
only. Their projected near-surface spacing is not production density and must
not be documented or tuned as the final density policy.

P2S3 is the next authorized phase: an isolated spherical-billboard GPU
runtime/draw proof. It has not begun. The existing dynamic patch quadtree,
mixed-LOD balancing, stitch templates, translated patch cache, per-patch
indirect submission, parent/child promotion, and global/anchored ownership stay
intact through migration and are scheduled for retirement only after P2S6
acceptance.

## Candidate D and focused benchmark status

Candidate D is accepted as a presentation-only optimization. It
lets material shading consume cheaper deterministic derived classification
instead of reconstructing the complete FP64 physical field per fragment. In the
canonical 3440x1440, 10.004 m, workload-equivalent benchmark it reduced total
GPU time from 37.492 ms to 20.845 ms. That result does not authorize Candidate D
to own physical geometry or replace physical parity.

The deterministic fixed-pose benchmark and its workload fingerprint are useful
diagnostic infrastructure. A benchmark comparison is invalid when owner count,
TCS patches, refined vertices, raster primitives, material/terrain generation,
or relevant descriptor/resource generation differs beyond its declared
tolerance.

The P2C2 material investigation also retained a bit-identical optimization of
the existing `TerrainNoise2` corner-hash prefix. It does not alter band
activation, classification, albedo, visual normals, physical authority, or
material identity. Temporary material bypasses and profiling branches are not
part of the accepted foundation. The old triangle-dependent material footprint
may remain only inside the transitional renderer until P2C3 establishes the
topology-independent prepared/MIP replacement.

## M12C-4B decision

Retain these 4B concepts as representation-independent contracts:

- declarative physical-frequency metadata;
- wavelength-versus-sampling representability;
- smooth C2 attenuation mathematics;
- deterministic body-fixed frequency rules;
- CPU/GLSL parity;
- zero shared-boundary height and position delta;
- bounded physical-normal continuity;
- rejection of physical frequencies that a representation cannot sample.

The 4B runtime bridge was rejected and has been removed:

- runtime edge/guard machinery used to reconcile unrelated height authorities;
- frequency data transported through vertex/TCS/TES ABI solely for that bridge;
- repeated per-edge contextual physical evaluation;
- `PhysicalFrequencyBoundaryMask` as a production ownership mechanism;
- tests that lock the rejected mechanism rather than the invariant.

The removed bridge restored mathematical modifier continuity but did not make the
underlying global and anchored geographic-height sources identical. Its
canonical geometry workload returned to the Candidate D fingerprint while GPU
and material cost still rose substantially. It is not an accepted production
direction.

## Procedural-terrain decision

M12 generation 3's globally coherent fixed-axis carrier family is rejected as
the long-term near-field primitive. Phase-warped expressions of the form
`sin(dot(bodyPoint, fixedAxis) / wavelength + warpedPhase)` retain global
periodicity and can produce visible ribs and crossing grids.

Future NovaCore-native physical detail must use deterministic body-fixed,
locally decorrelated structure: hashed integer-cell domains, local transformed
frames, continuous neighbor blending, multidirectional domains,
derivative-aware fields, erosion/domain warping, and biome/modifier-specific
composition. These are responsibility requirements, not permission to copy KSA
algorithms, constants, shaders, assets, or data.

## KSA reference boundary

The installed KSA implementation is an important reference for analogous
planetary responsibility boundaries. Relevant future work must inspect it
directly and record what problem a technique solves, where and how often it
runs, what it prepares, how it is reused, and how it bounds cost and continuity.
NovaCore then adopts the responsibility, adapts it to NovaCore's architecture,
or intentionally differs with measured justification.

The useful reference flow is:

```text
selected terrain representation
-> canonical height preparation
-> modifier preparation
-> displacement
-> final normals
-> conservative culling/compaction
-> rendering
```

NovaCore remains independently implemented and has no runtime KSA dependency.

## Performance and scheduling facts

- Candidate D's fixed-pose improvement is workload-equivalent and real.
- At the accepted 3440x1440 canonical production pose, GPU p95 is approximately
  19.94-20.00 ms. Stable 60 FPS is not achieved.
- Manual physical replay accepted the Phase 1B moving-reference-frame hitch
  correction. The remaining measured bottleneck is steady anchored
  terrain/material rendering.
- The rejected 4B contextual runtime evaluation reintroduced significant GPU
  cost without increasing the canonical geometry workload and is absent from
  the closeout candidate.
- Time warp has not been shown to execute terrain once per simulation substep.
- The current render callback does synchronously perform celestial evaluation,
  orbit-presentation construction, camera presentation, and terrain update.
- Existing logs separate costly movement/publication from settled rendering;
  they do not provide equivalent 1x/moderate/high-warp category measurements.

The intended scheduling direction is immutable prepared simulation snapshots,
cached/reused orbit presentation until relevant inputs change, and rendering of
the latest completed snapshot. Terrain visibility, tessellation demand,
material evaluation, and drawing remain render-frame responsibilities. Do not
claim warp-specific causality until focused equivalent measurements prove it.

## Acceptance policy

Microbenchmark acceptance proves a deterministic fixed pose, equivalent
workload fingerprint, CPU/GPU timing, and physical/parity invariants. It is an
optimization proof only.

Physical player acceptance covers orbital approach, representation activation,
low altitude, grazing horizon, lateral movement, a 360-degree turn, retreat,
re-approach, and 1x/moderate/high permitted warp where relevant. It must check
camera behavior, complete ownership, physical continuity, dynamic publication,
frame pacing, and Vulkan validation at the target 3440x1440 configuration.

A microbenchmark pass never implies physical acceptance. Manual physical
Desktop testing is an authoritative rendering-milestone gate.

## Banked M12D foundation candidate

The automated Phase 1 candidate routes complete global geometry and refined
anchored geometry through the same required 8192x4096 oracle plus NCCUBE2
residual and canonical modifier evaluator. Terrain-v5 elevation remains a
presentation/classification payload; it is no longer global physical geometry
or a second fragment normal authority. Each geometry density prepares normals
from its canonical displaced vertices, and the existing acknowledged complete
generation plus stencil-zero global fill remains the publication boundary.

Focused height, position, normal, representability/C2, parent/child,
publication, clearance, shader, Vulkan, and canonical-workload checks are the
closeout gate. The accepted generation-3 physical operation order intentionally
produces Florida reference hash `0x9C929A91ECE4A902`; tests retain that value as
an operation-order regression, not as a visual blessing. Temporary material
cost-attribution bypasses and the obsolete CPU/global-selector parity oracle are
removed. The latter compared the retired adaptive CPU global hierarchy with the
current shallow global fallback plus anchored owner and no longer represented a
production invariant.

The natural-terrain field, multiscale families, GPU preparation contracts, and
topology artifacts are now the accepted foundation for the spherical-billboard
migration. The opt-in P2C renderer exists to preserve and verify generation,
parity, bounds, and publication behavior during migration; it is not a second
permanent production terrain renderer.

After this foundation is banked, the next bounded work is P2S3 only: load the
immutable P2S2 topology through an isolated runtime/GPU draw proof without
changing production ownership. P2C3 remains paused until the billboard path is
physically stable.

Do not restore the retired Eye or CPU final-raster grids, add another terrain
representation, or hide continuity failures with skirts, depth bias, material
masking, or fallback geometry.

## Current limitations

Production atmosphere, volumetric clouds, physical oceans, weather,
vegetation, and surface gameplay are not implemented. Global physical source
fidelity remains shallow outside regional data. The dynamic hierarchy has not
yet been replaced by the selected spherical-billboard representation. P2S2
proof meshes are far below production near-surface density, P2S3 has not begun,
and stable 60 FPS remains open. The sample remains a single-window,
single-foreground-thread host.
