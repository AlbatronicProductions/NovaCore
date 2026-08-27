# Planetary rendering

NovaCore publishes immutable, root-resolved `PlanetaryPresentationSnapshot` data to Graphics. Graphics never owns a body's position, radius, hierarchy, ephemeris, or time.

## Current production surface: terrain-v5

Earth production rendering is one body-fixed terrain-v5 authority. A shallow
L0–L2 relaxed cube-sphere supplies global/orbital coverage from the checked
`earth_surface_v5.nccube` pack. Patch identity is
`body / terrain-version / face / level / x / y`; geometry, elevation, albedo,
classification, and source-cloud payloads share that identity. The current
renderer consumes the surface payloads; no atmosphere/cloud presentation pass
is active. Parents remain opaque
and authoritative until an entire child quartet is resident, then one coherent
promotion epoch transfers ownership.

Terrain-v5 also fixes the geographic frame contract at the source boundary:
+Y is north, longitude zero is +X, and increasing/east longitude advances
toward -Z. Its EPSG:4326 inputs are resampled offline using that convention;
runtime cube addressing therefore preserves the same right-handed geography as
SurfaceAnchor and ENU (`East × North = Up`).

The production pack is distributed through the tracked terrain manifest and
ignored content-addressed runtime cache described in
[`terrain-assets.md`](terrain-assets.md). It is not an ordinary Git blob and is
not copied into managed/native build outputs. This changes distribution only:
native residency still performs bounded patch-record reads and the renderer's
terrain ownership, filtering, pupil, and hierarchy are unchanged.

Near-planet density is supplied by four persistent precomputed production
Eyeball meshes (T0–T3), not per-frame mesh generation. A deterministic snapped
body-fixed pupil and projected-error hysteresis choose the tier. The production
Eye samples terrain-v5 cube payloads directly and exchanges sole terrain
ownership with the shallow globe without an independently visible texture page
or hidden legacy surface beneath it. Its topology hashes are
`0x406A2FB30687F0DA`, `0x6A8D7F46E937CAE9`, `0xEA22A4136AFA7884`, and
`0xA462CD4E25B748FB`.

### Dormant anchored spherical tier contract (11B-7A)

11B-7A does not add a renderer. It defines the body-fixed identity and bounded
CPU reference topology for a future replacement near-surface mesh tier. The
identity composes the existing `SurfaceAnchor` with tier, canonical relaxed-cube
cell, and topology version. Camera position/orientation, focus mode, residency,
frame number, GPU address, and backend never participate in geographic
identity. A reduced rational canonical cube coordinate gives independently
generated same-face, cross-face, parent/child, and three-face-corner vertices
one exact key before the established relaxed-cube projection. Body-fixed anchor
values can use the existing FP32 high/low transport while FP64 remains CPU
authority. A quantized shared-edge demand/factor record is backend-neutral; no
tessellation, compute subdivision, mesh shader, draw, asset request, or native
ABI is active yet. 11B-7B may use this contract to begin GPU physical-height
preparation. Until a later takeover is proved, the terrain-v5 globe and T0–T3
Eyeball above remain the sole live Earth surface.

### Dormant GPU physical-height preparation (11B-7B)

11B-7B adds a separate bounded validation ABI and Vulkan compute path; it does
not participate in frame submission or terrain ownership. The query transports
canonical FP64 body-fixed geography as FP32 high/low anchor components plus a
small local delta, independently reconstructs the relaxed-cube face/address on
the GPU, and samples the checked terrain-v5/global and local-v2 payloads. A
bounded readback compares the result with the canonical CPU physical-terrain
authority, `PlanetaryPhysicalTerrainAuthority.TrySampleHeight`, including
Florida ENU offsets and cube edges/corners. Physical source selection follows
dataset availability rather than transient GPU residency. Host writes,
compute reads/writes, and host readback have explicit Vulkan barriers. The
proof creates no live draw, dispatch, descriptor, or steady-state resource.

### Dormant displaced mesh and physical normals (11B-7C)

11B-7C consumes the 11B-7B height contract in a separate, explicitly invoked
Vulkan proof. A canonical whole-body relaxed-cube topology deduplicates every
face edge and three-face corner before adjacency construction. One persistent
native session owns bounded input, displaced-vertex, index, adjacency, terrain,
normal, descriptor, pipeline, command, fence, and timestamp resources. Its
first compute pass reconstructs body-fixed FP64 positions, adds authoritative
global/local physical height radially, preserves split high/low output, and
performs camera subtraction before the final FP32 presentation value. The
second pass accumulates area-weighted triangle adjacency in FP64 and emits an
outward FP32 physical normal. Explicit host-to-compute, pass-to-pass, and
compute-to-host barriers define visibility; focused validation measures CPU/GPU
height, displacement, winding, normal, edge/corner, camera, source/fallback,
reuse, shutdown, and determinism behavior. The optional local-v2 pack may be
absent; the same persistent session then deterministically reports no local
source and uses the checked global elevation oracle without changing topology.

This is still a dormant preparation proof: it is absent from normal frame
submission and issues no live draw. The live renderer remains the accepted
terrain-v5 L0–L2 globe plus persistent T0–T3 Eyeball. A later 11B-7D must prove
bounded live ownership and handoff before either current terrain owner can be
retired.

### Dormant screen-space subdivision proof (11B-7D)

11B-7D keeps that ownership boundary. A backend-neutral demand function
projects the two canonically ordered body-fixed endpoints of a physical edge,
measures their fixed-point screen length, divides by a configurable 32/16/8/4
pixel target, and clamps the deterministically quantized factor. Both incident
owners therefore evaluate the same inputs; all twelve cube edges measure zero
factor mismatch. A bounded 12.5% factor hysteresis suppresses threshold chatter
without entering geographic identity or the future tier-promotion contract.

The dormant reference topology can now materialize deterministic factors 1,
2, and 4 while retaining the 11B-7C factor-1 hash. The existing persistent
Vulkan compute preparation path measures authoritative displacement and
geometry-derived physical normals for each density. Coarse-normal interpolation
is measured for comparison, but geometry-derived adjacency normals are retained
because they follow the newly sampled physical positions and share canonical
edge/corner vertices. The development RX 6800 XT exposes Vulkan tessellation
and `VK_EXT_mesh_shader`; those are capability results, not active backends.
No tessellation or mesh-shader pipeline enters frame submission in 11B-7D.

`--scene=planetary-subdivision-diagnostic` is an explicit validation-only
spherical density/seam visualization. It reuses the bounded shared-grid draw
and never activates in `earth` or `sol`; physical-height and normal acceptance
remain the compute/readback proof rather than this simple diagnostic material.
The live terrain-v5 globe and T0–T3 Eyeball remain the only normal Earth owners.
11B-7E may use the retained demand contract when it introduces anchored tiers,
snapping, and transactional promotion.

The old equirectangular SVT page model, 5×5 predictor, per-page fallback/fade,
deep-global near-ground refinement, and per-frame radial Eyeball compute path
were retired in 4C. Lawful source rasters and their provenance remain offline
inputs. The checked R16 elevation oracle remains topology-neutral shared
infrastructure for CPU clearance and CPU/GPU parity, not a second renderer.

One permanent sphere cannot scale from solar-system views to a launch site. Far-field rendering therefore uses a shared whole-body proxy with the physical radius applied only in presentation. Earth global terrain then uses six stable relaxed cube faces addressed by `(face, level, x, y)` through shallow L2; the persistent production Eyeball tiers take sole close-range terrain ownership without changing that body-fixed geography. Unsupported bodies remain on the inexpensive generic whole-body/detailed path and cannot activate Earth terrain-v5 resources.

The current Earth production policy selects global patch transactions and Eye tier from projected demand, residency completeness, snapped pupil identity, and bounded hysteresis. The managed topology and payload checks remain validation oracles. This policy is presentation-only and never participates in simulation identity.

Astronomical body positions are never converted directly to FP32 model translations. The body center is first subtracted from the root-resolved camera position in doubles; patches are then expressed around this camera-relative center. Deep patches reconstruct their address and final camera-relative position in shader FP64 before rasterization. Terrain displacement remains renderer-owned presentation data and does not change snapshot or patch identity contracts.

10A-2A-1 adds one shared deterministic 16x16 indexed `[0,1]²` grid and a 48-byte fixed-width native patch record. Face values exactly match `CubeSphereFace`; native validation borrows no caller memory.

10A-2A-2 gives the native Vulkan renderer one renderer-owned vertex/index allocation for that shared grid and a host-visible patch-record buffer. One instanced indexed draw maps each submitted record onto its cube face in the vertex shader, normalizes the cube position to the unit sphere, applies the body-local radius, and finally adds the camera-relative body center. The diagnostic scene submits the six level-zero roots with distinct solid colors. Back-face culling makes the debug body opaque without changing the existing triangle pipeline; depth, terrain, textures, lighting, atmosphere, and adaptive patch selection remain deferred.

10A-3 adds `--scene=earth`. The sample evaluates the existing SolAnalytical system at its authored epoch, publishes the root-resolved Earth through `PlanetaryBodyPresentationProvider` into an immutable `PlanetaryPresentationSnapshot`, and reads the mean radius directly from the authored body catalog. Earth focus and orbit update only the managed camera. Each frame subtracts that camera from the immutable Earth proxy in doubles before the existing six root records are submitted to the unchanged Vulkan grid, pipeline, shaders, and patch ABI. The result is deliberately one smooth solid-color Earth-sized sphere; terrain, textures, atmosphere, clouds, and lighting remain deferred.

10A-4 replaces Earth’s fixed six-root submission with the existing deterministic CPU `PlanetaryRepresentationSelector`. Its presentation-only altitude/radius policy selects an ordered, complete set of cube-sphere quadtree leaves: six roots at the far limit and uniform recursive refinement through the deliberately capped level 5 as the orbit camera approaches. The sample converts only those leaves to the existing 48-byte patch records, and the native renderer expands its renderer-owned patch-record buffer when the active batch grows; the shared grid, Vulkan pipeline, face mapping, camera-relative coordinates, and celestial presentation state are unchanged. Current uniform per-level selection has no unequal neighboring levels, so crack stitching and geomorphing remain deferred.

10A-5 makes that CPU selection spatially adaptive. Each patch compares its body-local projected span (the diameter of a conservative corner bound divided by camera distance to the patch center) with a configurable presentation threshold. Deterministic face-order/child-order traversal refines only failing patches, applies conservative analytical horizon culling below the roots, and then repeatedly refines the first coarse edge neighbor until every retained cross-face or same-face neighbor differs by at most one level. Cross-face transitions are an explicit 24-entry integer table with edge-orientation reversal; no floating-point neighbor discovery participates.

Mixed levels use deterministic edge stitching. `NativePlanetaryPatch` is explicitly extended from 48 to 64 bytes by one fixed-width `uvec4`: a four-bit `-U/+U/-V/+V` stitch mask followed by three required-zero reserved words. On a fine edge adjacent to a level-minus-one patch, the existing planetary vertex shader collapses odd boundary samples of the shared 16x16 grid onto the coarse grid positions. This creates degenerate transition triangles but no T-junction opening, while retaining one shared mesh, one patch batch, and the existing pipeline. The planetary pipeline declares the shared grid's established outward winding as clockwise; analytical horizon culling exposed and corrected the earlier counter-clockwise declaration, which had rendered the retained back hemisphere when all six faces were always submitted. Discrete presentation-only LOD popping remains; skirts, geomorphing, terrain, textures, and frustum/occlusion culling remain deferred.

10A-6 retains that CPU selector as the reference/debug implementation and makes GPU selection the Earth sample's default production mode. The per-frame GPU input is one 48-byte fixed-width record containing camera position in body-local metres, physical radius, the established `0.11` refinement threshold, near-field altitude, MaxLOD, and output capacity. GPU production receives no CPU-generated leaves: one compute dispatch seeds the six roots in face order, evaluates the same conservative patch-center/corner projected-span metric, applies the same analytical horizon test, and emits the retained quadtree leaves. Frustum and occlusion culling remain deferred so validation can require exact active-set identity with the CPU horizon-culling oracle.

Renderer-owned buffers persist for the maximum 6,144 output patches, traversal stack/leaf scratch, all 8,190 level-0-through-5 node occupancy values, the input constants, and a 64-byte control/indirect record. The bounded compute traversal repeatedly refines the first cross-face or same-face coarse neighbor until the level difference is at most one, derives the existing four-bit stitch mask from the final occupancy hierarchy, writes contiguous 64-byte draw records, and writes a single `VkDrawIndexedIndirectCommand`. Capacity exhaustion increments an explicit overflow counter and never raises the indirect instance count above capacity. Normal GPU rendering performs no leaf readback or per-frame resource creation; validation mode reads the persistently mapped output only after the frame fence, canonicalizes `(face, level, x, y, stitchMask)`, and compares membership and a debug-only FNV-1a hash with the CPU oracle. GPU output order and hashes are presentation diagnostics only and never enter celestial, simulation, save, or dataset identity.

Synchronization is explicit and narrow: the previous frame fence is awaited before validation readback or any persistently mapped host write, a host-write-to-compute-read barrier publishes the new per-frame constants, then a compute-write barrier targets indirect-command reads and planetary vertex-shader storage reads before the render pass (plus host reads only in validation mode). The graphics command buffer issues one indexed indirect draw using the unchanged shared 289-vertex/1,536-index grid and planetary pipeline. Swapchain recreation rebuilds the renderer-owned submission resources and descriptors under device-idle synchronization. The sample exposes `--planetary-mode=cpu|gpu|validate` plus a bounded `--gpu-capacity` diagnostic override; telemetry reports roots, candidates, refinements, culls, active leaves, balancing refinements, min/max LOD, overflow, and indirect instances.

10A-7 adds a generic, presentation-only handoff between that detailed path and a cheap whole-body renderer. The controller uses camera distance divided by the immutable proxy radius, with configurable detailed and distant boundaries plus hysteresis. Its three explicit states are `DistantOnly`, `Transition`, and `DetailedOnly`; transition weights are deterministic, normalized, and monotonic. Earth currently uses detailed-only at or below 12 radii, distant-only at or above 18 radii, and 0.25 radii of state hysteresis. These thresholds and the controller live entirely in Graphics/sample presentation code and do not enter celestial evaluation, simulation identity, persistence, or dataset hashes.

The native whole-body path owns one persistent 12-by-24 latitude/longitude sphere (266 vertices and 1,584 indices) and one opaque indexed draw. It consumes the same immutable body color, physical radius, and double-derived camera-relative center as the detailed cube-sphere. During transition the whole-body sphere supplies the opaque base while the detailed patch pipeline alpha-blends over it, so the visible detailed contribution follows the reported detailed weight without requiring transparency ordering between two shells. `DistantOnly` records zero detailed patches, dispatches no planetary selection compute work, and issues one distant draw; `DetailedOnly` issues no distant draw. Telemetry reports state, distance/radius metric, both weights, detailed compute intent, detailed patch count, and distant draw count. The presentation-only material binding is the future seam for cubemap albedo and normals plus cheap atmosphere, cloud, and ocean approximations; those additions require no change to celestial authority or representation selection. The distant renderer remains a far-field optimization, not a replacement for the near-field cube-sphere terrain architecture. Terrain, textures, atmosphere, clouds, billboards, point sprites, and impostor atlases remain deferred.

10A-8 introduces the `--scene=sol` overview. It evaluates Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune through the committed SolAnalytical definition and publishes their root-resolved centers and catalog mean radii through one immutable presentation snapshot. The renderer batches the existing shared distant sphere over those presentation records; every GPU center remains the double-precision evaluated center minus the double-precision camera position. At overview scale, screen-space markers provide visibility without changing a proxy's physical radius, focus distance, or any evaluated center-to-center distance.

Presentation focus is an explicit fixed-width input independent of the eight SAS/control commands: `1` through `9` select Sun through Uranus and `0` selects Neptune. Focus reads the target center and physical radius from the immutable snapshot and affects no evaluated state. Only the focused body is eligible for the existing 10A-7 representation handoff and 10A-6 detailed GPU traversal. It remains first in the presentation buffer so the detailed selector consumes the same center, radius, color, and material authority; its distant alpha blends out through transition and reaches zero in `DetailedOnly`. The other nine records remain cheap distant-sphere instances. The native frame contract is 480 bytes after 10B-1B expanded the embedded focused presentation record to the same 144-byte material ABI used by the distant batch; the separate focus input remains 68 bytes without changing the SAS offset.

10A-9 makes the true-distance overview readable without modifying physical body geometry. The shared ten-record distant presentation buffer carries stable presentation label identity and focused-state bits without changing its 48-byte layout or the 336-byte frame ABI. Renderer-owned overlay shaders project those existing camera-relative centers and draw fixed-NDC diamond markers plus a compact 3×5 glyph label. The marker is a screen-space aid, distinct from the physical-radius sphere: it neither changes an authoritative center nor the proxy’s physical radius metadata. The earlier Solar minimum-apparent-radius aid is superseded by markers so a near Earth view cannot inflate the Moon. Labels alternate bounded offsets around their authoritative projected anchors; focused labels and markers are highlighted.

The Solar sample also reuses the established camera-relative line transport for nine root-resolved trajectory paths. At startup it deterministically derives one period from each existing analytical two-body trajectory, samples 129 evaluator states across that period, and emits 128 independent line-list segments per path in stable Mercury-to-Neptune order. Each point is root resolved through the same generic celestial evaluator—including the Moon’s Earth parent—then converted every frame as `double root point - double camera root position` before float upload. A persistent native line buffer and one reusable line-list pipeline render the 2,304 vertices with a stable per-path presentation palette. The sampling is presentation-only; it does not advance or mutate simulation state. Orbit lines, labels, and markers have no UI toggles yet; no orbit-distance compression, labels framework, terrain, textures, atmosphere, clouds, or bloom has been introduced.

10A-9A makes the Solar overview interactive and corrects its presentation without changing those trajectory samples or any planetary contract. The label shader treats its 3×5 masks as top-to-bottom rows and left-to-right high-bit columns, then accounts for NovaCore's positive-height Vulkan viewport when placing glyph cells in NDC. Labels use smaller fixed-screen cells, a stable positive marker offset, neutral non-focus color, and a restrained focus tint. Body markers are four thin screen-space reticle arms centered on the existing projected body center; orbit colors are reduced to 28% of their body palette. These aids never change a sphere radius or evaluated position.

The Solar camera uses one focus-orbit pose in a non-rotating root-inertial frame. Mouse drag updates bounded orbital azimuth/elevation and therefore moves the camera around the selected body at fixed distance; wheel input changes only that distance. The camera offset is never stored in body-fixed coordinates or rotated by axial spin. Time advancement translates the entire rig by the focused body's center displacement while the planet rotates underneath it. Camera controls modify no celestial or immutable snapshot data. Milestone 10B-1C refines the focus and home policies described below.

The apparent static-scene defect was an update-loop omission: the original Solar snapshot was evaluated once at instant zero. The scene now advances the established `SimulationClock` from bounded host duration, indexes the shared ordered 0.1× through 7,776,000× preset catalog (`.` faster, `,` slower), and honors Space pause. Each accepted instant is evaluated through `CelestialSystemEvaluator` and published as a new immutable `PlanetaryPresentationSnapshot`; the focus camera pose is then rebuilt around the newly evaluated focused center. The fixed orbit-line sample geometry remains authoritative presentation data and only its camera-relative transport changes. Once-per-second debug telemetry reports simulation ticks, effective rate/pause, focus, and camera distance. A two-second wall-time overlay reuses the Solar 3×5 label path to report each accepted rate change without coupling its lifetime to simulation warp.

10A-9B adds deterministic screen-space label rejection without changing any body or marker record. Each label computes the exact NDC rectangle implied by the existing glyph stride, cell dimensions, marker offset, and label length. Candidates are considered in the fixed priority order focused body, Sun, Earth, Moon, then the remaining stable body order. A candidate is accepted only when its rectangle plus a `0.004` NDC clearance does not overlap an already accepted higher-priority rectangle. The focused body therefore wins an otherwise exact conflict, Earth wins a distant Earth/Moon conflict when neither is focused, and both labels return as soon as their projected rectangles separate. Repeating an identical camera/snapshot produces the same accepted sequence. Only a presentation flag suppresses a rejected label; the corresponding physical sphere and screen-space marker remain submitted. The clearance margin supplies a deterministic anti-flicker dead band; no persistent visibility history or temporal hysteresis enters replay or simulation state.

The same milestone corrects Solar extreme-zoom clipping. The prior finite 100 AU projection was assembled in double precision but transported to the shader as FP32. With the overview camera nearly normal to the ecliptic, all Solar content occupied almost the same forward depth: the last valid wheel step was `87.69226950000007 AU` (FP32 depth `1`), and the first invalid step was `96.46149645000007 AU` (FP32 depth `1.0000001192092896`). Vulkan therefore clipped spheres, orbits, markers, and labels together even though their camera-relative centers remained finite. Solar frames now use the standard Vulkan zero-to-one infinite-far perspective form, whose depth approaches one without crossing it; the former failing distance produces FP32 depth `0.9999999403953552`. The logical presentation camera still clamps at 100 AU. Other scenes retain their established finite projection. True body distances, physical radii, celestial evaluation, and double-precision camera subtraction are unchanged.

10B-1A introduces a renderer-owned HDR scene-color stage without changing celestial or camera authority. Each swapchain extent owns one persistent `VK_FORMAT_R16G16B16A16_SFLOAT` image, view, and memory allocation shared safely by the existing single frame in flight. The first render-pass subpass writes background, bodies, the stellar Sun, and overlays into that attachment. A second input-attachment subpass applies a fixed presentation exposure, an ACES-style fitted curve, and explicit linear-to-sRGB encoding into the existing `VK_FORMAT_R8G8B8A8_UNORM` / `VK_COLOR_SPACE_SRGB_NONLINEAR_KHR` swapchain. There is no automatic eye adaptation. Descriptors, framebuffers, HDR target, and compatible pipelines are rebuilt only during renderer initialization or swapchain recreation; destruction releases submission descriptors before their referenced views.

The deep-space layer is a repository-authored deterministic procedural shader with no external texture or game-asset provenance. A fullscreen triangle reconstructs a view direction from the submitted camera transform, so the sky rotates with the camera but has no translation parallax or CPU-side star list. Deterministic hashed star layers supply varied density, brightness, and warm/cool color; low-frequency three-dimensional noise supplies a broad Milky Way band and dust lane. The layer is enabled only by the immutable stellar-lighting presentation contract, preserving the legacy flat clear for non-Solar regression scenes.

`SolarLightingPresentation` derives its source center from the evaluated Sun proxy and encodes `SunCenter - CameraCenter` in doubles before FP32 transport. Both the distant-sphere and detailed cube-sphere shaders then derive their body-local light vector from `SunCenter - BodyCenter`, use outward normalized sphere directions as normals, and share the same diffuse plus restrained ambient floor. The two representations therefore keep one terminator direction through handoff; the lighting contract is generic and contains no Earth-specific direction or material behavior.

The Sun is identified by presentation metadata but keeps its evaluated center and catalog physical radius. A dedicated persistent high-resolution sphere mesh and stellar shader render the photosphere with a warm HDR color, controlled limb response, and inexpensive procedural variation. A separate analytic screen-space corona draw extends beyond the physical disk and retains a bounded minimum apparent size at Solar-map range; it is a presentation aid and does not change the physical radius. This is deliberately not a general bloom engine or lens flare. The glow renders behind scene overlays, while the opaque photosphere is redrawn after orbit paths so overview aids do not stripe the resolved stellar disk; markers and labels remain on top.

10B-1B adds immutable `PlanetMaterialPresentation` and `PlanetRingPresentation` contracts plus one sorted `PlanetMaterialCatalog`. Material identity, procedural albedo source, tint, roughness/specular/emissive response, body-local presentation rotation, projection metadata, future atmosphere/cloud hooks, and optional ring association are presentation data only. The fixed-width native body record is 144 bytes and carries this material response beside the existing authoritative camera-relative center and physical radius. Focus reordering copies the same material identity into record zero, so the whole-body and detailed shaders consume one logical material through handoff rather than selecting body-specific pipelines.

Both spherical shaders include the same material implementation and sample it from the normalized body-local sphere direction. This direction-space mapping has no camera-space motion, per-patch copy, cube-face seam, or equirectangular pole singularity. Milestone 11A-4 replaces the old explicit presentation rotation with the exact-epoch body-fixed-to-root quaternion carried by the immutable snapshot and native presentation ABI. The same transform orients Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune in both distant and detailed paths.

Rings are a separate generic renderer-owned path. `PlanetRingPresentation` carries parent identity, a normalized ring-plane quaternion, inner and outer radii, radial opacity/banding response, color, and a future source identity. Native transport stores radii as ratios of the unchanged authoritative parent radius. One persistent 256-segment annulus is 512 vertices and 1,536 indices (12,288 buffer bytes total); it is instanced over the existing presentation batch and rebuilt only with renderer-lifetime resources. Two alpha-blended, two-sided pipelines render the camera-far half before the opaque body batch and the camera-near half afterward. Fragment classification is derived from ring-plane position and camera direction, so Saturn occludes the far half and the near half passes in front without a camera-authored mask or a depth attachment. The ring normal uses the same evaluated-Sun lighting authority as planets. This ordered solution preserves the established two-attachment HDR render pass, swapchain lifecycle, and triangle path; it does not yet provide planet-cast ring shadows, ring-cast planet shadows, or physical ring thickness.

10B-1C completes the first cohesive Solar presentation layer without creating a second camera. `SolarCameraPresentationMode` distinguishes the explicit home/map pose from free 3D interaction while retaining the same focus, yaw, pitch, and wheel-zoom state. Startup and `R` select the evaluated Sun, preserve the current `SimulationInstant`, set a deterministic 58 AU distance, and orient the camera 23.439291111 degrees from the established pose so the true ecliptic geometry is readable with Neptune's path inside the viewport. Any drag enters free 3D; focusing preserves the current orientation.

Focus distance is now one generic projected-extent calculation instead of a fixed body-radius multiplier. The physical radius is the visual extent for ordinary bodies, the existing corona extent frames the Sun, and the optional material ring outer radius frames a ringed body. The chosen vertical projected target is 0.15 NDC with a four-radius safety floor. No body center, physical radius, ring ratio, orbit, or representation threshold changes.

Overlay selection is a bounded deterministic pass over the ten current records. Labels are considered in focused-body, direct parent/child, Sun, major-planet, then stable-body-ID order; accepted rectangles include a 0.0035 NDC clearance and must fit wholly within a 0.012 NDC screen margin. Large already-readable bodies and unrelated close-local bodies lose redundant labels or markers, while sub-pixel overview bodies retain markers. Orbit opacity is encoded in the existing presentation metadata: Solar Map retains all major paths and subordinates the lunar path; a local focus retains its own path and direct parent/child paths while unrelated Solar paths fade to zero. The orbit line pass is alpha blended and recorded before opaque bodies, so planets and the photosphere cover the segment behind their physical disk without depth, path edits, or camera-specific masks. Marker and label pipelines use restrained alpha blending and smaller neutral technical glyphs.

Measured validation at 2048×864 produced: Solar Map `9 paths / 6 labels / 10 markers`, inner Solar `9 / 6 / 7`, Earth–Moon `2 / 2 / 2`, and Mars/Jupiter/Saturn local views `1 / 1 / 0`. Short validation-layer measurements averaged 19.180 ms in Solar Map, 18.574 ms inner Solar, 18.405 ms Earth–Moon, 18.563 ms Jupiter, and 18.617 ms Saturn; the longer all-view run averaged 19.400 ms over 49,160 frames, versus the pre-change 18.737 ms baseline. The run included resize recreation, every focus target, map reset, free-camera orbit, the 100 AU clamp, and clean native shutdown with no validation or stderr output.

10B-2A extends the existing hierarchy rather than creating a second terrain tree. `PlanetaryPatch(face, level, x, y)` remains the stable address through level 24; Earth deliberately caps production refinement at level 22. The selector now uses viewport-scaled projected geometric error, analytical horizon rejection with a bounded terrain margin, view-cone rejection, deterministic sparse traversal, and the existing 2:1 neighbor balancing/stitch masks. At 3440×1440 the current 128-pixel patch target corresponds to eight pixels per shared-grid cell. The production GPU path remains bounded at 8,192 active leaves; the CPU path uses identical ordering and stays the exact-set/hash oracle in validation mode.

Surface data is generated only on cache misses. One renderer-owned 8,192-slot deterministic cache is keyed by `(source identity, source version, face, level, x, y)` and stores 289 double-precision elevations per resident patch. An open-addressed lookup maps stable patch keys to slots; generational replacement supplies bounded eviction without per-frame allocation or geometry upload. The current Earth source is repository-authored deterministic procedural elevation, version 1, bounded to 7,600 metres. A compute pass evaluates its continuous direction-space function into the cache, and the shared vertex shader samples those elevations to displace the unchanged cube-sphere grid. Terrain normals use neighboring cached elevation samples and sphere directions; deep address reconstruction uses shader FP64 so level-22 cell offsets do not collapse under FP32 quantization. This is an architectural terrain proof, not production geography or authored elevation truth.

The swapchain-dependent render target now includes one persistent `VK_FORMAT_D32_SFLOAT` depth image. Opaque scene pipelines use reversed-Z (`clear = 0`, `GREATER`) with a managed finite reversed projection for local scenes and an infinite-far reversed projection for Solar scale. Depth is written by opaque bodies, terrain patches, rings, and the triangle regression; sky, glow, tone mapping, orbit lines, and screen overlays retain their intended depth-independent ordering. This corrected an observed FP32 cancellation defect in the earlier shader-side `w - z` conversion at whole-Earth range. Depth image, view, framebuffers, compatible pipelines, and descriptors follow the established device-idle swapchain recreation/destruction order.

Near-surface camera motion remains presentation-only. The Earth scene clamps requested clearance to two metres above the procedural surface, queries the same deterministic terrain function on the CPU for collision/floor placement, and blends the existing orbital orientation into a body-fixed east/north/up tangent frame between 1,000 km and 100 km. Camera and terrain positions are still subtracted in double precision before high/low GPU transport; neither evaluated Earth center nor physical mean radius changes. Discrete LOD refinement remains stable and stitched during the tested orbit-to-ground descent, but geomorphing is still deferred.

## Historical 10B-2B provisional environment (retired in 11B-4)

10B-2B added one immutable, body-associated `PlanetaryEnvironmentPresentation` contract for atmosphere, cloud, and ocean parameters. Earth’s version-1 repository-authored definition used a 100 km atmosphere, 8 km Rayleigh and 1.2 km Mie scale heights, Mie anisotropy 0.76, a bounded 2–11 km cloud shell, and a presentation sea level. The fixed-width native environment record was 128 bytes. It carried no celestial position, time, or hierarchy authority.

That renderer-owned fullscreen HDR layer reconstructed camera rays, intersected physical planet/atmosphere spheres, and performed bounded density samples plus a procedural cloud shell. It was a compact presentation approximation, not a production multiple-scattering model. Its managed record, frame ABI field, descriptors, pipeline, shader, and draw are removed in 11B-4. No replacement environment effect is installed; evaluated-Sun lighting, HDR scene color, and tone mapping remain.

Cloud density is deterministic body-direction FBM at fixed physical altitudes, so camera translation reveals shell parallax and never camera-attaches the pattern. The same logical density masks distant and detailed body materials, and evaluated-Sun direction controls cloud brightness. A small source-direction offset supplies a bounded matching shadow mask on land and water. Clouds are static presentation data: there is no weather simulation, dynamic storm evolution, wind, or high-frequency close-cloud volume.

Ocean classification uses the exact raw cached terrain elevation and immutable sea level in both distant and detailed paths. Detailed vertices below sea level clamp to one spherical water surface while retaining raw height for material selection; outward radial normals, bounded procedural waves, roughness/specular response, and view Fresnel distinguish water from land. There is no extra ocean mesh or per-frame upload, and coastline identity remains stable across camera motion and LOD. The source is procedural and not real Earth geography; wave dynamics, foam, refraction, and physical ocean simulation remain deferred.

Near-surface control is explicit `Orbital`, `Transition`, and `SurfaceLocal` presentation state. Above 1,000 km the existing planet orbit remains active; from 1,000 km to 100 km a smoothstep blends it into a captured body-fixed east/north/up anchor; at or below 100 km mouse look changes bounded local yaw/pitch without moving that anchor. Altitude-dependent wheel factors reduce descent step size near the ten-metre shared terrain/ocean floor. The reusable `PlanetarySurfaceFocus` stores body identity, body-local anchor, tangent basis, and local camera offset, providing a future vehicle-focus seam without granting Graphics simulation authority. Receding above the transition releases the anchor and returns to orbital control.

At 3440×1440 on the development RX 6800 XT, validation-enabled end-to-end runs (including descent and cache warm-up) measured 5.653 ms for whole-Earth distant rendering and progressed through 22.581, 23.913, 27.085, 31.612, 35.230, 38.433, and 40.192 ms at successively closer low-orbit/~100 km/~10 km/~1 km/~100 m/~10 m/~2 m targets. The final LOD-22 endpoint retained 7,321 active and 7,905 resident leaves without overflow. Atmosphere, cloud, ocean, and terrain costs are not separately timestamped. The environment adds one persistent 128-byte host-visible storage buffer and one graphics pipeline; it reuses existing HDR/depth targets and creates no per-frame image, mesh, or descriptor. The result improves scale continuity but does not match the supplied KSA references: final geography, dense close cloud structure, vegetation, surface objects, foam, cloud self-shadowing, and production color grading remain future work.

## Historical 11A fixed-workload near field (retired in 4C)

The 10B-2A/2B deep quadtree remains a useful measured regional renderer, but its production surface responsibility is retired. At 3440×1440 the V1 Debug/validation path grew from 2,223 active leaves and 22.581 ms at 1,000 km to 7,321 leaves and 40.192 ms at the two-metre floor. Selection, 2:1 balancing, stitch metadata, cache lookup/generation, and up to 8,192 indirect instances all scaled with descent. The renderer now caps regional refinement at level 12 and uses it only above/through the new handoff.

The superseded `PlanetaryEyeballTopology.Shared` used one 32,769-vertex radial cap generated every frame. Its geometry, compute pipeline, constants, and topology hash are retained here only as milestone history; no source, shader, descriptor, or runtime owner remains after 4C.

The pupil is the body-fixed intersection of the current camera view ray with the visible surface; only a ray miss falls back to `normalize(camera - evaluatedBodyCenter)`. The cap includes the view-cone half angle in addition to 120% of the physical horizon and the existing margin. A deterministic east/north tangent basis orients each ring. Camera movement changes only the 128-byte per-frame eye constants; height and material functions receive the resulting body-fixed direction, never a ring, vertex, camera, or screen identity. Consequently the presentation topology can move underneath stationary body-fixed terrain without changing terrain truth. The managed CPU oracle and shader share the version-2 level-independent seven-octave elevation definition; regional compatibility calls retain a level argument but it no longer changes height.

## Historical 11B-2C spatial coverage and SVT demand (retired in 4C)

The disappearance captured in `Our Earth Popping Invisble.mp4` was a projection discontinuity, not an ephemeris, culling, or SVT failure. Solar retained a 1,000 km near plane throughout the 2,000–1,000 km SurfaceAnchor blend, then switched directly to 5 cm. The aimed surface moved behind that near plane for about 1.2 seconds before full SurfaceLocal mode restored it. The new continuous near-plane policy remains smaller than the current surface distance, and Distant/Regional/Eyeball state now has an explicit no-empty-owner invariant.

This milestone established bounded coverage, visibility telemetry, and the view-ray surface footprint. Its equirectangular 5×5 page predictor, independent ancestor fallback, 128-slot page pool, gutters, and per-page promotion were superseded by terrain-v5 patch transactions. The coverage and body-fixed authority invariants survive in the production selector and cache.

The checked Earth pack contains `L0..L4`, 256×256 logical tiles (260×260 with gutters), and logical global widths 512, 1024, 2048, 4096, and 8192. At Earth mean radius these are approximately 78.18, 39.09, 19.55, 9.77, and 4.89 km per equatorial texel. Elevation is also 8192×4096. Metre-scale viewing therefore magnifies kilometre-scale source samples; it cannot reveal truthful local geography. The future body-fixed hierarchy is checked global macro imagery → independently streamed regional pages → local surface material detail → metric procedural/micro-normal detail. No dataset or compression format changes in 11B-2C.

One compute dispatch uses 129 workgroups of 256 threads. Targeted shader FP64 reconstructs high/low camera coordinates and physical radius, resolves spherical directions, evaluates continuous elevation, clamps water to the immutable sea level, and finite-differences the same field for outward normals. It writes one persistent 48-byte-per-vertex storage/vertex buffer and one `VkDrawIndexedIndirectCommand`. A narrow compute-write barrier publishes vertex and indirect reads. The graphics pipeline then issues exactly one indexed-indirect draw. At or below 1,000 km there are zero CPU leaves, zero regional compute dispatches, zero near-field cache requests, zero stitch masks, and one terrain submission/draw; counts are identical through two metres.

The deterministic handoff smoothsteps from regional-only at 2,000 km to eyeball-only at 1,000 km. Both paths share physical center/radius, body-fixed height source, material identity, sea level, evaluated-Sun lighting, atmosphere, HDR target, and reversed-Z depth. The regional path remains available above the overlap; deep levels 13–22 are no longer selected in production. `SurfaceLocal` retains its immutable body-local anchor, bounded yaw/pitch, and CPU floor query against the same terrain definition. The eyeball shader receives the camera body vector directly, so no celestial value or Earth center is altered.

Persistent V2 eye storage is 1,572,912 bytes for generated vertices, 783,360 bytes for indices, 128 bytes for constants, and 20 bytes for indirect arguments: 2,356,420 bytes total. There is no per-frame eye allocation and no altitude-driven residency. The older 19,398,656-byte regional cache remains bounded for regional overlap only and survives swapchain recreation independently from the swapchain-dependent HDR/depth targets.

The renderer now owns Vulkan timestamp queries for total GPU, eyeball compute/draw, regional compute, background, environment, scene, tone map, and combined material/overlay phases. Host timing separately records fence/update/callback/upload, command recording, submit, present, and frame average/p95/p99/max. Representative mapped eye vertices and the one indirect command are checked after the frame fence for finite values, bounded height, outward unit normals, pupil identity, and fixed arguments; this readback is validation instrumentation, not production terrain generation.

Milestone 11A-2 completes the scoped procedural visual/stability pass without changing that workload. The shared material helper now evaluates body-position triplanar noise with derivative-aware broad-band filtering and explicit close-band distance fades. Earth land combines approximately 120 km regional, 9 km landscape, 64 m local, 12 m aggregate, 4 m surface, and 2.4 m grain bands. A 3 m micro-normal response fades from full strength below 350 m to zero near 2.5 km and modulates roughness without changing collision or elevation. Slope is derived only from `1 - dot(surfaceNormal, localUp)` and smoothly blends sediment, exposed substrate, and cliff response across the moderate and steep ranges; geometry and sea classification still use the same terrain height authority. Metre-scale cells reconstruct wrapped body coordinates from the existing high/low body-camera split plus camera-relative fragment position, avoiding both large-magnitude FP32 block artifacts and per-fragment FP64 cost.

Ocean uses the same sea-level decision as displaced geometry, with body-fixed two-axis wave detail, distance/derivative filtering, radial base normals, controlled Fresnel, and a low-roughness specular response. Aerial perspective now derives from atmosphere presence and integrated path endpoints, reducing distant-terrain contrast without inventing a camera fog plane. The implementation remains repository-authored and texture-free; the triplanar API is deliberately expressed in body metres so a lawful texture-backed source can later replace individual frequency bands without changing eye topology or terrain authority.

`SurfaceLocal` now supports `W/S` tangent-forward/back and `A/D` tangent-right/left translation. Speed is a bounded presentation policy (`12–2,000 m/s`) driven by altitude. Each update advances the immutable body-local direction, re-queries the shared terrain/ocean floor, preserves clearance, and reconstructs the focus frame; Earth’s evaluated center, radius, and snapshot never move. A two-second 2 m lateral proof changed the representative body-fixed eye hash from `0x3E464AC4825A9981` to `0x0A20D234EEA8BD2E` with continuous CPU/GPU parity and no visible topology, material, or horizon discontinuity.

The animated handoff was exercised in both directions. Inward samples at 1,875,000 m, 1,171,875 m, and 732,421.875 m retained alignment while transitioning regional → overlap → eye-only; outward samples at 988,769.562 m and 1,334,838.875 m returned through the overlap without a body or geometry jump. The representative eye hashes change because the body-fixed camera pupil moves, while topology count, indirect arguments, celestial state, and terrain authority remain fixed.

Release/no-validation 3440×1440 stationary measurements after 11A-2 are 6.095, 7.770, 8.222, 8.206, 8.168, and 8.223 ms at 100 km, 10 km, 1 km, 100 m, 10 m, and 2 m respectively. The corresponding measured GPU totals are 4.071, 6.410, 6.916, 6.974, 7.064, and 7.035 ms; eye compute remains 0.235–0.241 ms. The fixed GPU workload remains near seven milliseconds and is not altitude-correlated. Relative to the 11A baseline, close full-screen GPU cost rises by approximately 0.20–0.32 ms while the 100 km run rises 0.33 ms; no geometry, dispatch, or residency growth is introduced.

The scoped 11A-2 procedural visual gate passes: close terrain has readable multi-scale structure, slope/roughness differentiation, filtered micro-lighting, atmospheric depth, stable ocean response, lateral body-fixed motion, and a stable animated handoff. It still does not match the supplied KSA references or constitute production Earth data. Authored geography/material imagery, coastline-focused capture coverage, true volumetric close clouds, physical ocean/weather, vegetation, rocks, structures, spacecraft, and navigation remain deferred. The next rendering milestone should introduce a lawful authored planetary surface-data/virtual-texture seam and its streaming budget while retaining this fixed near-field topology.

## Historical 11A-3 Earth data and bounded virtual texturing (retired runtime format)

Earth macro presentation is now data-driven. The checked runtime pack combines NASA Blue Marble December 2004 unshaded true-color imagery, NASA Blue Marble cloud coverage, and NOAA ETOPO 2022 60-arc-second surface elevation. `assets/earth/PROVENANCE.md` records the source URLs, source hashes, public-use terms, resolutions, and preprocessing. The deterministic offline builder normalizes every layer to north-up EPSG:4326, constructs five levels (`0` through `4`) containing 682 registered tiles, writes a 256×256 interior with a two-texel geographic gutter on every side (260×260 physical texels), and emits a manifest and SHA-256 identities. Longitude gutters wrap and polar gutters clamp; generation validates the interior and all four gutters for every layer and tile. Albedo is sRGB RGBA8 with an authoritative land/ocean alpha mask, elevation is R16 UNORM mapped over -11,000 to +9,000 metres, and cloud coverage is R8 UNORM. The v2 runtime pack is 322,722,528 bytes; the separate 8192×4096 signed-height CPU oracle is 67,108,864 bytes.

The renderer owns a fixed 128-slot physical pool, one page table for all 682 logical tiles, one two-record upload staging area, and one background disk worker. The three persistent array images consume 60,569,600 payload bytes in aggregate; each tile record is 473,200 bytes, staging is 946,400 bytes, and the fixed page table remains 10,912 bytes. Requests are deterministic ancestor-first. The worker performs bounded raw-record reads into reusable slots; the render thread promotes at most two ready records per frame, applies deterministic oldest-use eviction outside the pinned roots, updates page metadata, and records the copy/layout barriers. A miss resolves to the nearest resident ancestor, never an invalid page. `readyFrame` drives the unchanged 30-frame parent-to-child blend. The pool, page table, sampler, and staging resources are renderer-lifetime and survive swapchain recreation.

Body-fixed spherical direction determines stable equirectangular page identity and local tile coordinates. Page identity is chosen before texture sampling; wrap, clamping, gutters, derivative filtering, and up-to-8× anisotropy keep the shared distant, regional, and fixed-eyeball paths aligned. The existing high/low camera and FP64 eyeball reconstruction remain the geometric precision authority. Real signed elevation feeds the immutable CPU height oracle and the GPU regional/eyeball displacement seam; sea-floor values remain signed data while visible ocean geometry clamps independently to presentation sea level. Procedural metre-scale response supplements the imagery but cannot change geography, coastlines, or elevation authority.

The independent Earth cloud channel drives the existing bounded shell and the land/ocean alpha channel drives the dedicated ocean response. The atmosphere remains a compact single-pass scattering approximation rather than a production LUT solution. 11A-3 removes the previous camera-inside opaque-alpha floor and caps surface aerial blending so atmosphere no longer becomes a uniform veil over nearby terrain. Debug mode `NOVACORE_EARTH_DEBUG=1..11` exposes mip, physical page, residency, fallback depth, requested/resident levels, temporal blend, imagery, elevation, ocean mask, cloud coverage, and atmosphere contribution.

The normal checked-data path reports 22–28 disk loads/uploads, no evictions or queue drops, 12–16 initial fallback frames, and 24–30 occupied slots in stationary 100 km through 2 m proofs. If the pack is absent or invalid, the renderer explicitly logs `bounded procedural root fallback active`, generates only the two global roots, performs no disk requests, and remains Vulkan-valid without black tiles. This is a labeled development fallback, not a silent return to procedural macro geography.

## Historical 11B-3A GPU-native Earth texture channels (retired runtime format)

The checked production-v3 pack preserves the v2 geographic tile contract: 256×256 logical interiors, two gutter texels per side, and 260×260 physical tiles. Because 260 is divisible by four, BC storage requires no new padding and does not alter page identity, wrap, polar clamp, or gutter filtering. The pack header is 256 bytes and contains four fixed descriptors with semantic, format, color space, maximum level, tile count, bytes per tile, and section offset. Old `NCVTEAR1`/version-2 data is never interpreted as version 3.

Albedo uses `VK_FORMAT_BC7_SRGB_BLOCK` through L4 (67,600 bytes/tile); elevation remains `VK_FORMAT_R16_UNORM` through L4 (135,200 bytes/tile); land/ocean mask uses `VK_FORMAT_BC4_UNORM_BLOCK` through L4 (33,800 bytes/tile); and clouds use BC4 only through their source-supported L2 (33,800 bytes/tile, 42 tiles). Albedo sampling performs hardware sRGB-to-linear conversion before lighting. Elevation, masks, and clouds are linear. No runtime normal map is introduced: existing terrain normals remain derived from authoritative elevation. The reserved future normal contract is BC5 UNORM storing signed X/Y in the established body/tangent frame, reconstructing non-negative Z and renormalizing without an un-audited Y inversion; future normal composition must operate on decoded vectors rather than encoded colors.

The four channels share logical IDs but own separate slots, request flags, ready frames, worker records, LRU state, ancestor resolution, and 30-frame promotion. Terrain demand therefore resolves each channel to its own useful maximum instead of requiring identical residency; clouds stop at L2 while albedo, elevation, and mask may reach L4. The asynchronous worker reads the selected section bytes directly into reusable BC/R16 buffers. Vulkan copies those bytes into four persistent array images and samples them natively. No render-thread decode, runtime recompression, or per-frame image creation occurs. The fixed pool contains 128 slots per channel and 34,611,200 payload bytes; maximum steady upload is four 135,200-byte staging lanes (540,800 actual bytes when all four are full), while the 1,081,600-byte allocation also holds the eight one-time pinned-root uploads.

Vulkan format properties are queried for optimal-tiling sampled-image and transfer-destination features. The development RX 6800 XT reports support for BC1, BC3, BC4, BC5, BC7, and R16. Preferred production requires BC7+BC4+R16. Missing/malformed data, version mismatch, or missing required format support selects a deterministic, explicitly logged uncompressed procedural-root path; it never decodes compressed tiles on the CPU or silently treats v2 as v3.

The repository-authored offline converter uses pinned NumPy 2.3.5 (BSD-3-Clause) for deterministic array operations and implements its own version-1 BC1 quality candidate, BC4, and opaque BC7 mode-6 encoders. It is not a runtime dependency. Repeated generation produces pack SHA-256 `a16aebd834f01bdd430790de499a095d55f895655ce037fe25b6e13106674dc5` and manifest SHA-256 `868769b2499bab96b32c3f5c5ea6b444db5c747294dd0e1e497057bf4e85e19b`. Measured BC7 global/coastline PSNR is 45.301/32.868 dB versus BC1 37.778/27.192 dB, which justifies BC7. BC4 mask/cloud PSNR is 51.755/39.936 dB. The elevation section is copied byte-for-byte and retains SHA-256 `e16390be4dc29f4e6d9e1f6c05da4defbdc137c4fbde04f7be1c91fd9167d1a0`.

The v3 pack is 162,781,056 bytes versus 322,722,528 bytes for v2, a 49.56% reduction. The unchanged 67,108,864-byte CPU elevation oracle makes the complete checked Earth runtime payload 229,889,920 bytes versus 389,831,392 bytes, a 41.03% reduction. Compression does not create geographic detail: L4 remains 8192×4096, approximately 4.89 km per equatorial texel. The next data milestone must add lawful regional imagery and elevation targeting roughly 10–30 m/texel where practical rather than spending the savings on indiscriminate global residency.

## Historical 11B-3B optional bounded regional pages (retired runtime format)

The first optional regional pack layered 48 sparse Mount St. Helens pages from
L5 through L12 over the unchanged global-v3 Earth hierarchy. Regional pages
retained the exact equirectangular body-fixed identity, 256×256 logical interior,
two-texel gutter, and 260×260 physical extent. Albedo is BC7 sRGB, elevation is
R16 UNORM over -11,000 to +9,000 metres, and source validity is BC4. At the
proof latitude L12 represents about 13.23 metres per texel.

The retired renderer read `earth_regions.index` once, validated exact size/SHA-256,
header identity, channel descriptors, and ordered page records, then used
three fixed 512-entry per-channel GPU hash tables. Pages streamed independently
in level/Y/X order into reserved slots 80–127 of the existing BC7/R16/BC4
arrays. Regional lookup searches from
the requested level through L5; a miss continued through the existing global
requested/parent/root path. BC4 validity blended the optional data boundary,
so no absent regional pixel could create a material or geometry hole. The pack
added one 24,624-byte storage buffer but no texture array, descriptor scan,
runtime decode, GeoTIFF parser, or per-frame filesystem work.

The proof pack was 11,359,360 bytes and uploaded its 48 pages once as 144 channel
payloads (11,356,800 bytes). At most two pages/six payloads/473,200 bytes are
scheduled per frame through the existing 1,081,600-byte staging buffer. The
active 1 km validation occupied 78 of 128 albedo/elevation/mask layers and 18
cloud layers with no eviction or queue drop; the three regional channels used
48 existing slots each. Detailed source and transformation authority is in
`planetary-data-ingestion.md`.

None of these packs, indices, descriptors, lookup tables, or residency rules is mounted or supported by the current runtime. The source manifest remains only as historical lawful-data provenance.

## Milestone 11A-4 body-fixed orientation and camera separation

Physical orientation is evaluated once per body and update from the authoritative `SimulationInstant`; it is never accumulated from render time. The simulation layer publishes a normalized body-fixed-to-inertial quaternion for each major body, while the translational celestial hierarchy remains inertial and unchanged. `PlanetaryPresentationSnapshot` copies that quaternion into each immutable proxy. The 176-byte native presentation record transports it to distant, generic detailed, production globe/Eyeball, and ring paths.

Camera controls operate in presentation space only. Focus-orbit and `SurfaceLocal` camera positions are retained in body coordinates and transformed through the evaluated body quaternion when constructing the root pose. Production globe/Eyeball inputs receive root-to-body camera and view vectors; generated body-local geometry is rotated back into the root camera-relative frame exactly once. Terrain-v5 patch identity, material directions, pupil coordinates, and surface anchors remain body fixed. Moving the camera changes visibility and tier demand but cannot change physical geography.

Saturn's ring orientation is relative to the Saturn body-fixed frame, so its plane follows the authoritative equatorial pole while prime-meridian spin leaves the plane unchanged. Earth production data now uses 256×256 cube-patch interiors, four-texel canonical spherical gutters, bounded patch payload residency, and transactional quartet promotion.

Milestone 11A-4D removes the distance-dependent convention split in the whole-body shader and the remaining frame leaks found at the handoff. The distant mesh previously formed root-space positions from an unrotated local sphere even though it inverse-rotated normal, material, view, and light directions into body space; regional and eyeball paths instead rotated body-local geometry into root space. The distant path now rotates its body-local position by the exact same immutable quaternion exactly once. The detailed vertex path derives `SunCenter - BodyCenter` from two root-camera-relative values before transforming that vector into body space; it no longer subtracts a body-relative patch center from a root-relative Sun center. The corrected focused camera is body-centered but non-rotating: paused drag changes the inertial orbital offset at fixed distance, while time advancement follows only body translation. Deterministic tests run every non-stellar body through detailed-only, transition, and distant-only states and exercise Earth, Mars, Jupiter, Saturn, and Moon through the full speed catalog while requiring invariant celestial authority and a rotating body-fixed longitude beneath the camera.

Milestone 11B-2B extends that same Solar camera from body-center to surface focus without changing a renderer representation threshold. Below the 2,000 km acquisition altitude, the current camera view ray is transformed into body-fixed space and intersected with the physical surface; Earth performs a bounded refinement against the existing authoritative CPU elevation oracle. A missed ray leaves focus unchanged. The acquired point is stored once in body-fixed metres, carries a pole-safe right-handed east/north/up basis, and is reevaluated through the current body translation/quaternion rather than camera motion. Smoothstep focus blending reaches the anchor at 1,000 km, retains it until 3,000 km on exit, and never places body rotation in camera orientation. Zoom remains logarithmic relative to the physical anchor and enforces the current ten-metre terrain clearance. A scene object may provide a more explicit focus target later, but no object is required for the no-object surface workflow.

Speed feedback is intentionally separate from the ordinary Solar label renderer. Its renderer-lifetime pipeline draws one restrained translucent panel and analytic monoline glyphs in a single 210-vertex draw, with derivative-based edge smoothing and no font texture, descriptor, vertex buffer, or managed per-frame allocation. It consumes the existing packed speed-preset and wall-time opacity values, so exact preset semantics and the two-second/final-0.75-second timing contract are unchanged. Body labels continue through the compact 3×5 path.

Debug/validation integrated averages on the development RX 6800 XT were 5.999, 5.855, 5.933, 6.011, 6.493, and 6.565 ms at 100 km, 10 km, 1 km, 100 m, 10 m, and 2 m. Eye compute stayed at 0.100–0.106 ms and total measured GPU time at 0.315–0.368 ms in the current validation window. The fixed geometry workload and one indirect draw remain unchanged. The architecture and streaming gates pass, but the professional visual gate remains open: the 4K-class macro sources and compact shell/scattering model are not sufficient for commercial close-surface fidelity, and automated 3440×1440 capture was blocked by external Vulkan-window discovery.
