# NovaCore current state

## Repository state

Inspect `git status --short` before editing. Preserve all existing unstaged work unless the active task explicitly changes it. Repository code and tests are authoritative if this document ever becomes stale.

## Current milestones: 9A-4F-2 and 10B-2A

NovaCore now has a deterministic, true-distance Solar System presentation built from `SolCompact-DE440Validated-v3`. `--scene=sol` evaluates Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune at the current `SimulationInstant`, root-resolves them through `CelestialSystemEvaluator`, and publishes immutable `PlanetaryPresentationSnapshot` data to Graphics. It is a compact analytical runtime validated offline against DE440, not runtime DE440 playback.

The scene includes deterministic current-time orbit paths, body labels, screen-space reticle markers, number-row focus, orbit/zoom camera controls, and `SimulationClock` rate/pause integration. `SolarCameraPresentationMode` adds an explicit true-distance Solar Map home pose while preserving the same free 3D focus-orbit camera. Startup and `R` select the evaluated Sun at 58 AU, orient near the ecliptic normal, preserve the current instant, and fit the major paths. Focus uses one projected-extent rule with physical radius, optional ring extent, and the existing stellar presentation extent rather than a body-specific distance table. Solar uses an infinite-far Vulkan projection and a 100 AU presentation clamp, preserving the established double-precision camera-relative subtraction through the prior finite-depth boundary.

Labels use deterministic NDC collision rejection in focused, direct parent/child, Sun, major-planet, then stable-body order. Accepted bounds include 0.0035 NDC clearance and remain wholly inside the viewport. Markers disappear when the physical rendered body is already readable; close-local views suppress unrelated labels and markers. Orbit opacity is deterministic presentation metadata: the map retains all major paths, Earth local retains Earth and Moon paths, and unrelated local paths fade away. Alpha-blended orbit lines render before opaque bodies so physical disks occlude the segment behind them without depth or orbit edits.

10B-1A adds a persistent FP16 scene-color target, fixed ACES-style exposure/tone mapping, a deterministic procedural deep-space/Milky Way layer, a dedicated photosphere/corona Sun path, and evaluated-Sun illumination shared by distant and detailed spherical paths. The lighting record is presentation-only: its source center comes from the evaluated Sun snapshot, and no physical radius, evaluated center, time behavior, or deterministic celestial identity changes.

10B-1B adds one immutable presentation-material catalog shared by the distant sphere and detailed cube-sphere paths. Repository-authored direction-space procedural materials now distinguish Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune without textures, per-patch data, or body-specific pipelines. Saturn uses the first generic ring record and one persistent annulus mesh; ordered far-ring/body/near-ring draws provide correct planet occlusion without adding a depth attachment. Material rotation remains an explicit presentation approximation until physical body orientation is modeled.

10B-2A proves the surface-scale continuation of that same Earth path. The existing quadtree address extends safely through level 24, with production capped at level 22 and 8,192 active patches. GPU selection uses viewport-scaled projected error, horizon and view-cone culling, deterministic sparse traversal, the established 2:1 balancing/stitching policy, and CPU/GPU exact-set validation. A persistent 8,192-slot renderer cache stores 289 double elevations per resident patch and generates only deterministic cache misses from the versioned repository-authored Earth procedural source. Shader FP64 reconstructs deep patch coordinates and preserves the established double camera-relative transport.

The renderer now owns a swapchain-dependent D32 depth target and uses direct reversed-Z projections: finite for local scenes and infinite-far for Solar scale. The Earth camera can descend to a two-metre terrain clearance, queries the same deterministic source for its presentation-only floor, and blends into a body-fixed east/north/up tangent frame below 1,000 km. Authoritative Earth center, radius, celestial time, hierarchy, and Sol v3 identity remain unchanged.

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
- Deep projected-error Earth LOD through level 22, deterministic GPU terrain residency, continuous procedural elevation, and cached terrain normals.
- Persistent reversed-Z D32 depth plus a two-metre terrain-aware camera floor and body-fixed local tangent orientation.

## Milestone 9 pipeline summary

Milestone 9 established immutable generic celestial definitions and a pure evaluator for fixed, circular, analytical-Kepler, and sampled trajectories. `SolCompact-DE440Validated-v3` retains the v1 DE440-derived ET0 seeds and v2 three-rate lunar secular correction, then adds a generic seven-frequency bounded radial/phase correction. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`; maximum separation error is `8.918 Mm`, down from v2's `47.273 Mm`. Dense maximum separation is `0.992 Mm` through 180 days and `4.196 Mm` through five years, but grows to `27.214 Mm` over ±25 years. NCPE v2 provides a deterministic, self-describing artifact for its current fixed/sampled payloads; it does not yet serialize analytical-Kepler payloads or corrections. The NAIF/CSPICE adapter remains an offline-only source boundary; runtime code has no CSPICE, kernel, network, calendar, or builder dependency.

## Remaining work

- Authored image textures, atmosphere, clouds, production terrain/geography, oceans, and physically modeled body rotation.
- A declared spacecraft-navigation error budget and exceptional higher-fidelity lunar ephemeris if lunar-orbit insertion, close navigation, or precision long-horizon planning requires more than v3. Compact Chebyshev or `SampledHermite` remains the measured fallback, not the default.
- Richer material layers, ring shadowing/thickness, geomorphing, occlusion refinement, and authored planetary surface features.
- Automatic exposure, convolution bloom, lens flare, antialiased font rendering, and richer overlay interaction remain optional measured presentation work; 10B-1A uses fixed exposure and an analytic stellar corona, while 10B-1C retains the lightweight 3x5 overlay renderer.
- Spacecraft, physics/gameplay, colonies, asset pipeline, and save/replay integration.
