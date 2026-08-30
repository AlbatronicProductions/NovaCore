# NovaCore current engineering state

NovaCore is a deterministic C# space-simulation core with a native Vulkan
renderer. Simulation owns celestial identity, time, physical state, and
reference-frame transforms. Graphics consumes immutable root-resolved snapshots
and performs FP64 camera-relative subtraction before GPU transport.

## Celestial runtime

- `SimulationInstant` and `SimulationClock` are the only runtime time authority.
- `SolCompact-DE440Validated-v3` provides the compact Solar presentation model.
- Major-body position and body-fixed orientation are deterministic and
  independent of camera state or render cadence.
- Root-resolved render snapshots contain presentation data, not simulation
  ownership.

## Earth surface

Earth uses one physical-surface authority and two coordinated presentation
scales:

- `earth-surface-v5` supplies complete L0-L2 relaxed cube-sphere coverage.
- `PlanetaryDynamicAnchoredSurface` selects finer body-fixed patches from
  projected error inside a retained camera-centered physical neighborhood.
  Camera orientation affects GPU visibility, not CPU residency authority.
- `earth-local-v2` supplies optional NCCUBE2 BC7/BC4/BC5 local payloads.
- Patch identity is
  `body / terrain-version / physical-surface-generation / face / level / x / y`.
- A published generation owns complete geometry, elevation, material data,
  cache residency, and GPU acknowledgement. The global owner remains complete
  while another generation prepares.
- Visible dynamic demand is bounded to 6,144 patches. A separate 8,192-slot
  cache provides 2,048 slots of measured transactional/reuse headroom. The
  default target is 256 pixels per patch with 700 km acquire and 800 km release
  hysteresis. The retained body-local neighborhood is 32-64 km with a bounded
  128-1,000 m recenter threshold.
- Physical height is evaluated from the terrain-v5 global source plus any
  applicable local NCCUBE2 residual. Visual material detail does not change
  collision or geographic identity.
- CPU work ends at canonical patch selection, residency, compact descriptor
  preparation, and transactional publication. A bounded generation coordinator
  uses at most eight workers; it does not build final-raster vertex density.
- Vulkan reuses a 4x4-quad base patch plus 16 stitch templates, performs
  conservative final visibility and bounded 1/2/4/8/16 screen-space
  tessellation, then evaluates physical displacement and normals on the final
  refined surface.

The global path is the conservative distant representation while the dynamic
hierarchy owns published close-range footprints. Their ownership boundary is
geographic and transactional; overlapping visible owners and unowned holes are
invalid.

## Camera and surface identity

- `SurfaceAnchor` is immutable body-fixed geographic identity.
- Surface-critical height, clearance, direction, and local tangent-frame work
  stays in body-relative FP64 authority.
- Solar-root FP64 remains authoritative for celestial translation.
- GPU presentation receives only camera-relative values.
- Earth camera clearance uses the persistent CPU physical-surface oracle and a
  10 m minimum exterior constraint.
- Surface-relative navigation hands off bidirectionally to body-center orbit
  without changing body orientation or geography.

## Native renderer

The native renderer owns the Vulkan instance/device/swapchain, HDR and depth
targets, current terrain resources, bounded terrain queues, reusable terrain
base topology, indirect commands, tessellation pipelines, and synchronization.
The managed/native frame ABI is 768 bytes; the compact dynamic patch pointer and
counts occupy offsets 576, 584, 588, and 592 and the presentation block begins
at 624. Terrain payload resolution is manifest-driven and never performs
implicit network acquisition.

## Supported launcher scenarios

- Solar System Overview
- Earth Far / Orbital View
- Earth 700 km
- Earth — Fullscreen Native
- Florida Launch Site
- Screen-space Subdivision Diagnostic

The fullscreen-native acceptance preset resolves the active display before
launch and structurally supplies borderless client dimensions plus Vulkan and
performance diagnostics to the child process.

## Current validation baseline

The current automated gate covers canonical cube geography, physical height and
normal parity, retained-neighborhood selection, bounded cache residency,
transactional publication, local-payload dependency, camera/SurfaceAnchor
behavior, rendering ABI, Solar/Earth Vulkan traversal, and Vulkan validation.
The moving dynamic traversal uses 18 poses and 801 frames and reports complete
ownership, zero shared-edge gap, zero holes, zero overlap, and zero stale
patches. Its final 312-frame stationary phase performs slow and rapid 360-degree
turns without selection, publication, cache, preparation, or upload churn.
Vulkan pipeline statistics independently report tessellation-control patches,
refined vertex invocations, and raster-emitted primitives.

The 11B-7H1 manual 3440x1440 Desktop recording is the accepted stable GPU
terrain baseline.

## Current limitations

- Florida currently has no regional NCCUBE2 physical coverage. Its broad,
  low-frequency ground appearance is an accepted global physical-data fidelity
  limit, not a renderer ownership or residency failure.
- KSA-class close-ground detail requires higher-resolution regional physical
  terrain plus associated biome/modifier authority.
- Close-ground material and environmental fidelity are not final.
- Fragment material and physical-normal cost remains a future measured
  optimization target.
- Compute triangle compaction remains measurement-driven future work; current
  measurements do not identify geometry/refinement as the dominant cost.
- Movement-time selector spikes may be reduced later. Pure camera rotation now
  performs no selection or terrain publication.
- Production atmosphere, volumetric clouds, ocean, weather, vegetation, and
  surface gameplay are not implemented.
- The dynamic hierarchy has not yet been measured as a replacement for the
  specialized global distant representation.
- The sample remains a single-window, single-foreground-thread host.

## Next work

The next surface-development priority is a provenance-tracked,
higher-resolution Florida/regional physical terrain payload and its associated
modifier/biome authority. The bounded distant-representation study and measured
fragment/normal/selector optimizations remain later work; do not reopen the
accepted renderer architecture without a demonstrated regression.
