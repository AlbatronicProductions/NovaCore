# NovaCore current state

## Repository state

Inspect `git status --short` before editing. Preserve all existing unstaged work unless the active task explicitly changes it. Repository code and tests are authoritative if this document ever becomes stale.

## Current baseline: celestial-v3 and terrain-v5 surface authority

Continuous Earth terrain-v5 is the sole production Earth terrain authority.
The retained global path is a shallow L0–L2 body-fixed relaxed cube-sphere
backed by the checked 61,484,224-byte `earth_surface_v5.nccube` hierarchy. The
heavy immutable bytes resolve from an ignored content-addressed terrain cache
through the tracked `earth-surface-v5` manifest; normal Git and build outputs
retain no production-pack copy. Its
126 patch transactions carry albedo, elevation, classification, and cloud data
under `body / terrain-version / face / level / x / y`. The complete parent
remains opaque while a complete child quartet prepares. Near-planet density is
owned by four persistent production Eyeball tiers with deterministic snapped
body-fixed pupil identity and projected-error hysteresis. T0–T3 hashes are
`0x406A2FB30687F0DA`, `0x6A8D7F46E937CAE9`, `0xEA22A4136AFA7884`, and
`0xA462CD4E25B748FB`.

The optional `earth-local-v2` companion is a sparse level-12 terrain-v5
refinement payload with its own tracked manifest and content-addressed cache
identity. Global roots remain conservative opaque coverage; local publication
can refine that owner but cannot remove it or independently own visibility.
Canonical body-fixed geography is +Y north, +X at longitude zero, and -Z at
+90 degrees east. SurfaceAnchor, mixed-payload material addressing, terrain
selection, and the anchored Florida launch-site proof all use that identity.
Production topology is outward, Vulkan front-face state matches its projected
winding, host writes are explicitly published before compute reads, and runtime
terrain version 5 is the selection authority.

The accepted recovered Desktop validation spans 10 m–100,000 km over 1,025
far→near→far samples. Vulkan checks at 50,000, 30,000, 20,000, 10,000, 3,000,
and 700 km retained the six-root `0x3F` mask and reported zero validation
errors. Camera-drag isolation, mixed-LOD geography, and terrain-authority
regressions pass.

11B-7A adds a dormant host-side contract for future anchored spherical mesh
tiers. Its geographic key is the existing canonical terrain-v5
`SurfaceAnchor` plus tier, containing relaxed-cube cell, and topology version;
camera state, focus state, residency, frame, GPU address, and subdivision
backend are deliberately absent. Exact reduced rational cube-surface vertex
keys canonicalize shared parent/child vertices, all twelve cube edges, and all
eight three-face corners before relaxed projection. Existing split-FP32
transport is explicitly measured for body-fixed anchors, and a quantized
backend-neutral shared-edge subdivision demand is defined without selecting or
activating tessellation, compute subdivision, or mesh shaders. This contract is
not bound to native rendering. The live renderer remains the accepted 11B-6C
terrain-v5 L0–L2 globe plus persistent T0–T3 Eyeball; no topology, ownership,
draw, streaming, material, camera, or visible behavior changes in 11B-7A.

11B-7B adds an equally dormant, explicitly invoked GPU physical-height proof.
A dedicated 96-byte query transports the 11B-7A body-fixed anchor through
split high/low FP32 plus a bounded local delta; a 160-byte result reports the
GPU-reconstructed point, canonical relaxed-cube address, global/local source
identity, and physical height. A temporary bounded Vulkan compute context
loads the checked CPU elevation oracle and terrain-v5/local-v2 data, dispatches
only for the requested validation batch, applies explicit host/compute/readback
barriers, and is destroyed after readback. The CPU authority remains
`PlanetaryPhysicalTerrainAuthority.TrySampleHeight`; GPU payload residency is
never physical truth. This path is absent from normal frame submission and
adds no live draw, dispatch, resource, ownership, camera, material, or visible
change.

11B-7C adds a dormant persistent GPU mesh-preparation proof beside that live
renderer. Its deterministic 98-vertex whole-body validation topology uses one
canonical owner for all relaxed-cube face edges and corners, with topology hash
`0x7F3262E7C37D781B`. A persistent bounded Vulkan session prepares physically
displaced split-FP32 body vertices and camera-relative FP32 output, then derives
outward physical normals from indexed triangle adjacency. Terrain source
selection remains the 11B-7B global/local CPU-authority contract; explicit
barriers and readback support parity and lifetime validation. An unavailable
optional local-v2 pack is a tested oracle-only fallback rather than an
initialization failure. The proof remains
outside `NcFrameSubmission`, creates no normal-frame dispatch or draw, and does
not replace the terrain-v5 L0–L2 globe or T0–T3 Eyeball. A later 11B-7D must
prove live integration and coherent ownership before any renderer takeover.

11B-7D adds a dormant backend-neutral projected-edge demand and stability
contract. Canonical body-fixed endpoints produce fixed-point screen lengths,
bounded quantized factors, and a 12.5% factor hysteresis; independently
evaluated cube-edge owners have zero factor mismatch. Deterministic factor
1/2/4 reference meshes contain 98/386/1,538 vertices and 576/2,304/9,216
indices. They pass through the persistent 11B-7C Vulkan compute displacement
and geometry-normal session without changing physical-height authority. The
development RX 6800 XT reports tessellation and `VK_EXT_mesh_shader` support,
but neither is installed as a renderer backend. The explicit
`planetary-subdivision-diagnostic` mode visualizes bounded spherical density
only; normal `earth` and `sol` frames contain no 11B-7D work. Production
terrain-v5 L0–L2 plus T0–T3 Eyeball ownership remains unchanged.

11B-7E assembles those dormant contracts into a bounded, backend-neutral
anchored spherical-billboard hierarchy. A canonical body-fixed footprint owns
deterministic T0/T1/T2 identities and exact relaxed-cube parent/child patch
sets. The retained 11B-7D projected-edge demand can request a finer tier, but
demand, complete resource readiness, and geographic ownership are separate
states. The parent remains the sole complete owner until every required child
topology, height, displaced-vertex, physical-normal, adjacency, bounds, GPU
buffer, and synchronization dependency is ready; promotion and coarsening then
change the single owner atomically. Missing, failed, culled, or unsynchronized
children retain parent coverage. Florida, cross-face edges, and three-face
corners have deterministic coverage proofs. The explicit
`planetary-anchored-billboard-diagnostic` visualizes ownership only; normal
`earth` and `sol` frames still use terrain-v5 L0–L2 plus the legacy radial
T0–T3 Eyeball and contain no 11B-7E preparation or draw.

4C/11B-4 remove the superseded equirectangular SVT descriptors,
request/upload/page cache, independent fallback/fade, sparse regional overlay,
deep-global close-range ownership, and per-frame radial Eyeball compute
pipeline. The
checked 8192×4096 R16 elevation oracle remains topology-neutral shared CPU
infrastructure. Lawful source imagery, source manifests, and provenance remain
offline inputs. Non-Earth bodies continue through the bounded generic renderer
and cannot activate Earth terrain-v5 resources.

NovaCore now has a deterministic, true-distance Solar System presentation built from `SolCompact-DE440Validated-v3`. `--scene=sol` evaluates Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune at the current `SimulationInstant`, root-resolves them through `CelestialSystemEvaluator`, and publishes immutable `PlanetaryPresentationSnapshot` data to Graphics. It is a compact analytical runtime validated offline against DE440, not runtime DE440 playback.

The scene includes deterministic current-time orbit paths, body labels, screen-space reticle markers, number-row focus, orbit/zoom camera controls, and `SimulationClock` rate/pause integration. A fresh default Solar launch samples UTC once, converts it through the pinned NAIF0012 UTC-to-ET constants, and thereafter treats `SimulationInstant` as the only time authority; explicit ET0 construction remains available for deterministic tests. One ordered 15-entry preset catalog spans 0.1× through 7,776,000×. Comma/period step it with native rising-edge input, and the existing lightweight 3×5 overlay shows an exact speed label for two seconds using wall/render delta rather than simulation time. `SolarCameraPresentationMode` adds an explicit true-distance Solar Map home pose while preserving the same free 3D focus-orbit camera. Startup and `R` select the evaluated Sun at 58 AU, orient near the ecliptic normal, preserve the current instant, and fit the major paths. Focus uses one projected-extent rule with physical radius, optional ring extent, and the existing stellar presentation extent rather than a body-specific distance table. Solar uses an infinite-far Vulkan projection and a 100 AU presentation clamp, preserving the established double-precision camera-relative subtraction through the prior finite-depth boundary.

Labels use deterministic NDC collision rejection in focused, direct parent/child, Sun, major-planet, then stable-body order. Accepted bounds include 0.0035 NDC clearance and remain wholly inside the viewport. Markers disappear when the physical rendered body is already readable; close-local views suppress unrelated labels and markers. Orbit opacity is deterministic presentation metadata: the map retains all major paths, Earth local retains Earth and Moon paths, and unrelated local paths fade away. Alpha-blended orbit lines render before opaque bodies so physical disks occlude the segment behind them without depth or orbit edits.

10B-1A adds a persistent FP16 scene-color target, fixed ACES-style exposure/tone mapping, a deterministic procedural deep-space/Milky Way layer, a dedicated photosphere/corona Sun path, and evaluated-Sun illumination shared by distant and detailed spherical paths. The lighting record is presentation-only: its source center comes from the evaluated Sun snapshot, and no physical radius, evaluated center, time behavior, or deterministic celestial identity changes.

10B-1B adds one immutable presentation-material catalog shared by the distant sphere and detailed cube-sphere paths. Repository-authored direction-space procedural materials now distinguish Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune without per-patch data or body-specific pipelines. Saturn uses the first generic ring record and one persistent annulus mesh; ordered far-ring/body/near-ring draws provide correct planet occlusion. Milestone 11A-4 removes the old presentation-rotation approximation: material longitude and Saturn's equatorial ring plane now consume the authoritative body-fixed transform.

The following 10B/11A/11B entries preserve milestone history. Any equirectangular SVT, deep-global close-range, regional-page, or old radial-Eyeball ownership described there is superseded by terrain-v5 above.

10B-2A historically proved the surface-scale continuation of that same Earth path. The previous quadtree address extended safely through level 24, with production capped at level 22 and 8,192 active patches. GPU selection used viewport-scaled projected error, horizon and view-cone culling, deterministic sparse traversal, the established 2:1 balancing/stitching policy, and CPU/GPU exact-set validation. That deep-global near-ground owner is no longer production architecture.

The renderer now owns a swapchain-dependent D32 depth target and uses direct reversed-Z projections: finite for local scenes and infinite-far for Solar scale. The Earth camera can descend to a ten-metre terrain clearance, queries the same deterministic source for its presentation-only floor, and blends into a body-fixed east/north/up tangent frame below 1,000 km. Authoritative Earth center, radius, celestial time, hierarchy, and Sol v3 identity remain unchanged.

10B-2B makes the camera transition explicit: `Orbital` above 1,000 km, smooth body-fixed tangent blending through 1,000–100 km, and `SurfaceLocal` at or below 100 km. The captured immutable body-local surface focus retains local yaw/pitch and the ten-metre terrain/ocean floor; receding releases it and returns to orbit without changing the evaluated Earth snapshot.

The 10B-2B provisional environment record and fullscreen atmosphere/cloud pass are retired in 11B-4. No replacement atmosphere, cloud shell, fog, or ocean presentation is installed. The numeric sea-level floor remains terrain/material data rather than an environment renderer. Evaluated-Sun lighting, day/night terminators, FP16 HDR scene color, and tone mapping remain production infrastructure.

Milestone 11A retires deep levels 13–22 from the production near-field. From 2,000 to 1,000 km a smooth deterministic overlap hands the regional cube-sphere to one renderer-lifetime concentric spherical topology; at and below 1,000 km no CPU terrain leaves, neighbor balancing, stitch masks, cache lookup, or regional terrain dispatch run. The topology has 128 warped radial rings, 256 azimuth segments, 32,769 vertices, 195,840 indices, and deterministic hash `0x4A46E29A7D6E90A7`. Its squared radial parameter concentrates samples at the body-local camera pupil while a compute pass evaluates the version-2 continuous Earth height field, finite-difference normals, camera-relative vertices, and one indexed-indirect command.

The fixed topology and its buffers never change with altitude. Targeted shader FP64 combines high/low camera and physical-radius transport and evaluates body-coordinate terrain; final camera-relative positions remain FP32. Milestone 11A-2 adds a body-metre triplanar material hierarchy, smooth slope response, derivative-filtered micro normals/roughness, endpoint-based aerial perspective, coherent near-ocean response, bounded `W/S/A/D` tangent-plane movement, and animated handoff verification. These are material/control changes only: the 32,769/195,840 topology, one compute dispatch, one indirect draw, regional LOD-12 boundary, overlap, and celestial authority are unchanged.

At 3440×1440 on the development RX 6800 XT, 11A-2 Release/no-validation GPU totals remain 4.07–7.06 ms from 100 km through two metres and eye compute remains 0.235–0.241 ms. Integrated averages are 6.10–8.22 ms with no altitude-correlated workload growth. The scoped procedural visual gate passes; the result is a stable, readable asset-light surface proof, not production Earth imagery or KSA parity.

Milestone 11A-3 replaces Earth macro geography with a deterministic checked data pack derived from NASA Blue Marble true color/clouds and NOAA ETOPO 2022 elevation. Milestone 11B-3A advances that data to explicit production-v3 channel sections while retaining 256×256 interiors, two geographic gutter texels per side, and block-aligned 260×260 physical tiles. Albedo is BC7 sRGB through level 4, authoritative elevation remains bit-identical R16 UNORM through level 4, the separated land/ocean mask is BC4 UNORM through level 4, and the source-limited cloud channel is BC4 UNORM through level 2. One shared logical page identity now resolves residency independently for each channel.

The production-v3 identity is `664ff32c3a57043960f246d5d97397214cedc4b976e48e867e9803c414d796b5`; payload hash is `d09a9ddf944242a7d322ae3ce58c1b0b31014feb8d6a330fb9d592e438e9d306`; the checked pack hash is `a16aebd834f01bdd430790de499a095d55f895655ce037fe25b6e13106674dc5`. Pack size is 162,781,056 bytes, down 49.56% from v2, while the separate 8192×4096 CPU elevation oracle remains 67,108,864 bytes. The four 128-slot Vulkan arrays consume 34,611,200 payload bytes, down 42.86%. The worker performs channel-specific reads and the render thread copies compressed blocks directly into BC images; there is no runtime decode or recompression. The fixed 30-frame ancestor promotion, body-fixed addressing, camera/focus/LOD topology, celestial authority, and deterministic celestial hashes are unchanged.

Milestone 11A-4 adds a separate exact-epoch body-fixed authority for Mercury through Neptune. `SimulationInstant.Zero` remains J2000 ET zero; each update evaluates one normalized body-fixed-to-inertial quaternion and angular velocity. Venus and Uranus retain their negative-spin conventions. The final lunar-precision checkpoint uses the official DE440 `MOON_PA_DE440` binary-PCK solution through the `MOON_ME_DE440_ME421` frame chain. A checked 1900–2100, six-hour residual pack contains 292,201 records in 3,506,540 bytes with hash `0x3BCE78D924EA3532`; warmed runtime evaluation is kernel-free and allocation-free. The complete 13-term `IAU_MOON` text-PCK model remains the explicit fallback outside coverage or if pack validation fails. Moon translation and the Sun-rooted/Earth-relative-Moon hierarchy are unchanged.

The immutable planetary snapshot carries body-fixed orientation beside each evaluated center and physical radius. Camera orbit, zoom, focus, and `SurfaceLocal` motion are converted into or out of that frame but never write it. Terrain-v5 patch identity, material directions, lighting inputs, production pupil coordinates, and surface anchors remain body fixed while final geometry is rotated into the root inertial frame. The fixed-width planetary presentation ABI is 176 bytes and, after provisional-environment retirement, the frame submission ABI is 688 bytes. Orientation reference hash at ET0 is `0xD5767D2C2BABE9AA`; warmed evaluation of all nine bodies is allocation-free.

Milestone 11A-4D closes the remaining render-path convention split at the distant/detail handoff. Distant, regional, and eyeball geometry now begin in body-fixed space and apply the same immutable body-fixed-to-root quaternion exactly once; material, lighting, terrain, SVT page identity, and surface anchors remain body fixed. The focused orbital camera retains a root-inertial body-center offset: drag changes orbital azimuth/elevation at fixed distance, body translation carries the rig, and axial rotation never rotates the offset or camera orientation. Detailed lighting subtracts the root-camera-relative body center from the root-camera-relative evaluated Sun center before entering body space, matching the distant path. Paused all-body handoff proofs and multi-body 1× through 7,776,000× tests preserve celestial authority while body-fixed longitude evolves beneath the camera. Solar speed feedback uses a dedicated compact 5×7 sans bitmap-glyph HUD pipeline with derivative antialiasing, while ordinary Solar body labels retain the established lightweight overlay path.

Milestone 11B-2A formalizes four independent authorities: FP64 root/inertial simulation position, evaluated focus position, root-inertial camera orientation, and derived camera-relative render transport. `FocusTarget` activates body-center focus without changing click-drag or logarithmic zoom, and defines surface-anchor and future scene-object seams without automatic acquisition. `CameraRelativeRenderPosition` now requires CPU FP64 subtraction before float conversion or high/low packing. An audit confirmed the specialized Solar/planetary/environment paths already obeyed that rule and corrected the generic object shader path, which previously subtracted independently narrowed root high/low lanes. Large-root precision tests cover kilometre through represented millimetre separation; high-warp tests cover fixed body-center and body-fixed mock targets through 7,776,000× without camera co-rotation.

Milestone 11B-2B activates the body-fixed focus seam without requiring a vessel. The Solar camera acquires `SurfaceAnchorFocus` only when its actual view ray intersects the focused body's physical/elevation surface; a miss retains the previous valid focus or remains at `BodyCenter`. Earth refinement uses the existing loaded CPU elevation oracle, and other bodies use physical radius. Acquisition begins at 2,000 km, reaches full anchor focus at 1,000 km, and releases only after receding through 3,000 km. Anchor-relative logarithmic zoom, a ten-metre terrain floor, pole-safe right-handed ENU transforms, and tangent translation remain allocation-free presentation behavior. Body translation/orientation reevaluates the fixed local anchor through maximum warp while click-drag and camera orientation remain root-inertial.

Milestone 11B-2C fixed the demonstrated Earth dropout during SurfaceAnchor approach. The cause was the Solar camera retaining a 1,000 km near plane until full SurfaceLocal state, not loss of terrain residency. The reversed-Z near plane remains continuous with altitude. Its camera, view-ray footprint, and no-empty-owner invariants survive; its old SVT demand and deep-global leaf implementation do not.

Milestone 11B-3A adds queried Vulkan format policy and deterministic incompatibility handling. On the development RX 6800 XT, BC1/BC3/BC4/BC5/BC7/R16 all expose sampled-image plus transfer-destination support. The preferred path selects BC7+BC4+R16; a missing, malformed, version-2, or unsupported pack selects an explicitly logged uncompressed procedural-root fallback and never silently changes quality. Measured BC7 albedo is 45.301 dB globally and 32.868 dB on coastline blocks versus BC1 at 37.778 and 27.192 dB, so BC7 is the selected premium format. BC4 mask/cloud measurements are 51.755 and 39.936 dB. The repository-authored offline encoder is versioned, requires pinned NumPy 2.3.5 (BSD-3-Clause), and reproduces byte-identical pack and manifest hashes.

An earlier bounded regional-ingestion experiment proved lawful USGS/USDA NAIP and USGS 3DEP acquisition. Its independent equirectangular pack, index, lookup, descriptors, and residency are retired because they conflict with coherent spherical-patch ownership. Only the lawful source files and exact manifest remain as clearly historical provenance for possible future cube-patch-aligned ingestion. The active runtime mounts no regional packs.

## Completed rendering foundation

- Immutable renderer-facing planetary snapshots and generic body presentation conversion.
- Shared 16×16 cube-sphere patch topology, fixed-width native patch ABI, and native layout validation.
- Renderer-owned Vulkan planetary grid/buffer lifecycle and reusable planetary pipeline.
- Earth presentation from its catalog radius and evaluated root position.
- Deterministic CPU spatial quadtree LOD, horizon culling, cross-face neighbor balancing, and crack-safe edge stitching.
- GPU patch selection with a CPU oracle, bounded output, explicit synchronization, and CPU/GPU parity diagnostics.
- Distant/detail handoff using the same physical radius and camera-relative center authority.
- Shared distant-body batch, Solar paths/labels/markers, and Vulkan validation-layer manual regressions.
- Swapchain-safe HDR scene color and tone mapping, procedural infinite-far space, dedicated stellar Sun presentation, and common Solar terminators.
- Shared immutable planet-material identity and response across distant/detail handoff, with deterministic body-local procedural projection.
- Generic renderer-owned ring presentation and persistent annulus geometry, initially configured for Saturn.
- One deterministic Solar Map/free-3D camera, extent-aware focus framing, hierarchical path fading, bounded label priority, and marker-to-body transition.
- Shallow L0–L2 relaxed cube-sphere ownership with deterministic transactional patch residency and checked terrain-v5 elevation/material payloads.
- Persistent reversed-Z D32 depth plus a ten-metre terrain-aware camera floor and body-fixed local tangent orientation.
- Deliberate absence of a provisional atmosphere/cloud owner; future environment work must integrate with terrain-v5 rather than constrain it.
- Explicit orbital/transition/SurfaceLocal control with deterministic round-trip and a reusable body-local focus contract.
- Four persistent spherical-billboard near-field tiers with snapped body-fixed pupil identity, bounded hysteresis, GPU displacement, and deterministic topology/ABI validation.
- Explicit `FocusTarget` identity and FP64-before-narrowing camera-relative transport across generic and planetary presentation paths.
- Actual-view-ray surface-anchor acquisition, pole-safe local ENU transforms, anchor-relative zoom, and hysteretic body-center/surface-focus handoff.
- Patch-aligned terrain-v5 payload residency with complete parent/child transaction ownership and a retained topology-neutral R16 elevation oracle.

## Milestone 9 pipeline summary

Milestone 9 established immutable generic celestial definitions and a pure evaluator for fixed, circular, analytical-Kepler, and sampled trajectories. `SolCompact-DE440Validated-v3` retains the v1 DE440-derived ET0 seeds and v2 three-rate lunar secular correction, then adds a generic seven-frequency bounded radial/phase correction. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`; maximum separation error is `8.918 Mm`, down from v2's `47.273 Mm`. Dense maximum separation is `0.992 Mm` through 180 days and `4.196 Mm` through five years, but grows to `27.214 Mm` over ±25 years. NCPE v2 provides a deterministic, self-describing artifact for its current fixed/sampled payloads; it does not yet serialize analytical-Kepler payloads or corrections. The NAIF/CSPICE adapter remains an offline-only source boundary; runtime code has no CSPICE, kernel, network, calendar, or builder dependency.

## Remaining work

- Deeper, higher-frequency lawful patch-aligned terrain-v5 payloads, followed by close-ground material/detail refinement.
- Production atmosphere/cloud reconstruction, then water/coastline systems, all designed against the retained cube-surface and persistent production-Eyeball architecture.
- GPU-driven local environmental detail and launch/landing surface gameplay; none is implemented yet.
- A declared spacecraft-navigation error budget and exceptional higher-fidelity lunar ephemeris if lunar-orbit insertion, close navigation, or precision long-horizon planning requires more than v3. Compact Chebyshev or `SampledHermite` remains the measured fallback, not the default.
- Richer material layers, ring shadowing/thickness, geomorphing, occlusion refinement, and authored planetary surface features.
- Automatic exposure, convolution bloom, lens flare, a general font/atlas system, and richer overlay interaction remain optional measured presentation work; 10B-1A uses fixed exposure and an analytic stellar corona, while 11A-4D gives the speed feedback its own antialiased analytic-glyph path without replacing the lightweight body-label overlay renderer.
- Spacecraft, physics/gameplay, colonies, asset pipeline, and save/replay integration.
