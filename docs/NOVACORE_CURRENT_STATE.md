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
- `earth-florida-m12` supplies NCCUBE2-v3 BC7/R16/BC5/R8 regional payloads from
  a provenance-tracked USGS 3DEP source in a complete L8-L11 hierarchy.
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
- Physical height is `max(0, signed global elevation + regional residual +
  deterministic physical modifier)`. Visual material detail does not change
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

## M12 regional foundation

- Florida now has about 98 x 111 km of 1/3 arc-second source coverage, packaged
  as 859 L8-L11 regional records. The control ABI includes ocean, coastal,
  beach, wetland, grass, scrub, forest, developed, and launch-site reservation
  classes; these are deterministic NovaCore classifications, not surveyed
  land-cover truth.
- The stored regional BC5 channel is diagnostic. Production normals come from
  the final composed displaced geometry, so mip/tier changes cannot replace
  physical-normal authority.
- Native regional demand is retained by authoritative anchored generation.
  A stationary or rotating footprint does not rescan every regional record
  against every anchored patch; pending uploads can still promote atomically.
- The bounded native 3440x1440 Florida proof selected 55 records once, reached
  55/55 published residency with no eviction or queue failure, and measured
  25.596 ms CPU frame average / 59.878 ms p95 and 17.902 ms GPU average in the
  Debug validation-capable executable. Startup preparation is included.
- Manual native 3440x1440 M12 visual and performance acceptance remains open.

## M12B planet-wide physical synthesis

- A deterministic FP64 body-fixed biome/control authority now selects and
  normalizes the four strongest contributors from ocean/coastal, beach,
  wetland, grass, scrub, desert, rocky, alpine, snow/glacial, and developed/
  reserved classes. Patch identity, camera state, LOD, residency, and time are
  not inputs.
- Five terrain modifier families cover rolling, rocky, dune, coastal/wetland,
  and glacial landforms across explicit macro/meso bands. A bounded 32 m to
  1.4 m near field is evaluated after GPU tessellation; sub-metre material
  response remains presentation-only.
- The same control blend drives the seven production PBR families:
  grass/soil, wet ground, beach sand, rock/cliff, alpine rock, desert sand,
  and snow/ice. CPU and GPU height/gradient/normal oracles share this contract.
- Florida regional R16 residual and reservation data remains an optional truth
  enhancement over the complete planet-wide result. Crossing its boundary
  changes source fidelity, not surface ownership or biome identity.
- M12B adds biome ID/blend, modifier-family, and near-physical diagnostics.
  Planet-wide visual character, anti-repetition, transitions, and native
  performance still require manual 3440x1440 acceptance.
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

Physically replay M12B from orbit through 1 km, 100 m, and 10 m, including a
360-degree rotation, lateral traversal, boundary crossing, retreat, and
re-approach. Include Florida, Appalachians, rocky/alpine terrain, desert/dunes,
snow/glacial terrain, grassland, and a no-regional-data region. The bounded distant-representation study and measured fragment/normal/
selector optimizations remain later work.
