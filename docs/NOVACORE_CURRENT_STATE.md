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

## Banked baseline

The current production-accepted planetary baseline is M12D-P2S5F, the **New
Earth Renderer**. Its milestone commit and annotated tag are banked only after
the cleanup validation in this closeout; automated and physical Desktop
acceptance are complete at native 3440×1440.

The milestone sequence from P2S2 through C3 moved spherical billboarding from
an isolated structural proof into the production Earth path:

- P2S2 proved immutable nested topology and artifact validation;
- P2S3 established isolated GPU runtime and draw responsibility;
- P2S4 bound canonical natural terrain to billboard geometry;
- P2S5B produced the first 18-level production topology library;
- P2S5C1 integrated the production renderer;
- P2S5C2 added live pupil movement, snapping, scale transition, and sample reuse;
- P2S5C3 stabilized physical coverage, culling, TES bounds, publication, and the
  physical Desktop traversal;
- P2S5F replaced the radial migration topology with the denser NCSM1 production
  scale mesh, established its explicit raster winding contract, added valid
  zero-visible publication/re-entry, persistent scale residency, body-specific
  presentation authority, and KSA-parity tessellation responsibility.

Spherical billboarding is therefore current production architecture, not a
future design or proof-only path.

## Production planetary responsibility chain

```text
canonical body-fixed physical terrain H(bodyDirection)
→ immutable persistent NCSM1 scale resource
→ retained and snapped pupil representation
→ physical position and normal preparation
→ conservative curved-patch planet occlusion
→ conservative screen/frustum visibility
→ compacted GPU workload
→ KSA-parity per-edge TCS factors and bounded TES refinement
→ indexed indirect raster
→ fence-complete atomic publication
```

The accepted NCSM1 topology library contains 18 immutable levels, L0 through L17.
Projected geometric error selects a level, and hysteresis prevents unstable
scale toggling. The camera-facing pupil retains an exact signed cube-lattice
identity, snaps on deterministic thresholds, and reuses matching samples when
the pupil moves or changes level.

CPU responsibility is canonical FP64 identity, physical evaluation, selection,
bounded asynchronous preparation, residency, and immutable publication. Native
Vulkan owns persistent per-scale topology resources, reusable working buffers,
conservative visibility work, compaction, bounded tessellation, indexed indirect
draw, synchronization, HDR, reversed-Z depth, and presentation.

## Physical authority and representation

Every representation of a geographic point derives from one body-fixed
physical-height function, `H(bodyDirection)`. The canonical composition includes
the checked global elevation oracle, optional regional residual, accepted
deterministic physical modifiers, and the final non-negative clamp. It owns:

- FP64 body-fixed geometry and deterministic displacement;
- physical gradients and normals where physical truth is required;
- collision, camera clearance, and physical queries;
- geographic identity independent of camera, topology, pupil, level, cache,
  generation, and render cadence.

Topology density controls the piecewise-linear presentation of that surface; it
does not define physical truth. Material shading may consume cheaper
deterministic derived information, but no material, LOD, cache, or fallback path
may become a second height or ownership authority.

`earth-surface-v5` and optional `earth-florida-m12` remain manifest-resolved,
hash-verified distribution and sampling inputs. They improve macro material,
classification, and regional physical fidelity. They are not competing visible
Earth surfaces. The retired adaptive patch, stitch-template, and radial-Eyeball
architectures must not be restored as production ownership paths.

## Pupil and scale behavior

The pupil is a moving presentation of the same body-fixed surface. Its origin,
level, and generation may change while the canonical direction, physical
height, and FP64 body-fixed position of a tracked geographic sample remain
unchanged. Exact lattice identities are reused across retained movement; a new
generation prepares only the data required by the resolved pupil.

Scale selection is driven by projected error rather than camera-distance tables
alone. Hysteresis and a retained current generation prevent oscillation while a
replacement prepares. Level changes and same-level snaps do not change Earth
orientation or `SurfaceAnchor` authority.

The 50 m contract applies only to near-camera TCS/TES refinement. It is not a
terrain-coverage radius. Factor-1 production base geometry remains present and
depth-owning outside the refinement range, including across the local horizon.

## GPU generations and ownership

The production runtime maintains one current generation and at most one
incoming generation. Preparation is asynchronous and bounded. The incoming
generation cannot publish until topology, physical positions, normals,
visibility/compaction state, indirect draw payload, and required GPU work are
complete and fence-confirmed.

Publication occurs atomically at a frame boundary. A completely prepared
generation with zero visible triangles is valid and may publish as the sole
owner; per-frame culling regenerates visible work from that same generation when
Earth re-enters the frustum. The new current generation
then owns Earth and the superseded GPU resources retire only after their use is
safe. Production telemetry and regressions require:

- exactly one Earth owner;
- zero overlap owners and zero unowned frames;
- zero stale-generation draws;
- valid compacted indices and one indexed-indirect billboard draw;
- no partial incoming generation suppressing the current surface.

## C3 and P2S5F stabilization

P2S5C3 corrected four related production facts:

- billboard preparation now uses the canonical Earth radius authority;
- an accidental 50,000 m TES range was restored to the existing 50 m contract;
- pre-TES horizon/backface rejection no longer assumes a planar triangle is an
  adequate bound for a curved displaced patch;
- conservative screen/frustum rejection preserves possible contributors while
  still compacting provably invisible work.

The resulting native 3440×1440 traversal covers all 18 levels, descent to
10.004 m, horizon rotation, repeated L16/L17 transitions, sustained L17 pupil
snaps, retreat, and re-approach. The recovered broad missing-Earth band and its
device-loss workload did not reproduce; ownership remained one-owner with zero
overlap or stale generations.

The Windows launcher also preserves build-configuration identity: a Release
launcher starts the Release Triangle runtime, and a Debug launcher starts the
Debug runtime. Runtime fallback builds retain the launcher's configuration.

P2S5F then established NCSM1 as the accepted New Earth Renderer topology. It
keeps immutable scale resources resident in a bounded current/incoming pool,
selects raster face convention from explicit topology-family identity, preserves
original compacted index triplets, accepts complete zero-visible generations,
keeps Earth presentation authority separate from resource residency, and uses
the audited KSA-equivalent per-edge tessellation-factor responsibility with a
TES range early-out. The remaining orientation-sensitive surface GPU variability
is performance debt, not an ownership or physical-authority exception.

## Known presentation continuity limit

Canonical physical terrain is invariant during camera translation, pupil
replacement, and adjacent-level selection. The current factor-1 triangulated
approximation is not perfectly invariant under a rare full pupil rebase because
the new lattice may triangulate the same smooth physical field differently.

Measured bounds at the accepted C3 baseline are:

- up to approximately 2.595 m change in the coarse factor-1 triangulated
  approximation during a rare/full rebase;
- approximately 2.8 mm difference between adjacent L14 and L15
  representations.

These values describe presentation/LOD morph work. They do not indicate moving
`H(bodyDirection)`, changing FP64 body-fixed points, translucent duplicate
terrain, or lost depth ownership. Future finer pupil/re-triangulation morphing
must preserve that distinction.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through authoritative body orientation. Earth —
Fullscreen Native uses the same production Solar navigation path after preset
initialization. Near-surface ENU free look, terrain-aware clearance, and the
body-center/surface handoff are presentation behavior and may not modify Earth
orientation or physical terrain.

## Accepted versus unfinished

Implemented and stable:

- the 18-level NCSM1 New Earth Renderer topology/runtime;
- canonical body-fixed terrain authority and physical preparation;
- moving/snapped pupil identity, reuse, scale selection, and hysteresis;
- asynchronous current/incoming GPU lifecycle and fence-complete publication;
- conservative curved-patch occlusion and screen/frustum rejection;
- persistent topology residency and reusable current/incoming GPU working resources;
- GPU cull/compact, KSA-parity bounded 50 m TES refinement, and indexed indirect raster;
- zero-visible publication/re-entry and body-specific presentation authority;
- one-owner Earth coverage through the accepted orbit-to-surface trajectory;
- FP64 world/physical authority with camera-relative GPU transport.

Still in development:

- terrain presentation, filtering, and material quality;
- finer pupil/re-triangulation morph quality;
- atmosphere, clouds, water, weather, and environmental rebuilding;
- richer regional/global source fidelity;
- spacecraft flight and surface gameplay;
- remaining non-catastrophic orientation-sensitive surface workload optimization;
- general immutable simulation snapshots and broader host concurrency.

The current renderer is production-owned and physically accepted, but it is not
visually finished.

## Next authorized work boundary

Begin from the accepted P2S5F New Earth Renderer path. Do not regenerate NCSM1
artifacts, restore retired terrain systems, or reopen recovered coverage/culling
decisions without explicit authorization and measured evidence. Keep
`H(bodyDirection)` authoritative and classify any future visual change first as
physical geometry, depth/ownership, or presentation/LOD behavior.

The next renderer work should be a bounded surface-workload efficiency milestone
against the accepted production owner, followed by finer pupil/re-triangulation
continuity and terrain material/detail quality. Atmosphere, clouds, environment,
and gameplay remain separate future milestones.
