# NovaCore current state

## Repository state

Inspect `git status --short` before editing. The working tree currently contains unstaged Milestone 10A presentation work; preserve it unless the active task explicitly changes it. Repository code and tests are authoritative if this document ever becomes stale.

## Current milestones: 9A-4F-2 and 10A-9B

NovaCore now has a deterministic, true-distance Solar System presentation built from `SolCompact-DE440Validated-v3`. `--scene=sol` evaluates Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune at the current `SimulationInstant`, root-resolves them through `CelestialSystemEvaluator`, and publishes immutable `PlanetaryPresentationSnapshot` data to Graphics. It is a compact analytical runtime validated offline against DE440, not runtime DE440 playback.

The scene includes deterministic current-time orbit paths, body labels, screen-space reticle markers, number-row focus, orbit/zoom camera controls, and `SimulationClock` rate/pause integration. Labels use deterministic NDC collision rejection: focused body, Sun, Earth, Moon, then stable body order, with `0.004` NDC clearance. A rejected label never suppresses its marker or physical body. Solar uses an infinite-far Vulkan projection and a 100 AU presentation clamp, preserving the established double-precision camera-relative subtraction through the prior finite-depth boundary.

## Completed rendering foundation

- Immutable renderer-facing planetary snapshots and generic body presentation conversion.
- Shared 16×16 cube-sphere patch topology, fixed-width native patch ABI, and native layout validation.
- Renderer-owned Vulkan planetary grid/buffer lifecycle and reusable planetary pipeline.
- Earth presentation from its catalog radius and evaluated root position.
- Deterministic CPU spatial quadtree LOD, horizon culling, cross-face neighbor balancing, and crack-safe edge stitching.
- GPU patch selection with a CPU oracle, bounded output, explicit synchronization, and CPU/GPU parity diagnostics.
- Distant/detail handoff using the same physical radius and camera-relative center authority.
- Shared distant-body batch, Solar paths/labels/markers, and Vulkan validation-layer manual regressions.

## Milestone 9 pipeline summary

Milestone 9 established immutable generic celestial definitions and a pure evaluator for fixed, circular, analytical-Kepler, and sampled trajectories. `SolCompact-DE440Validated-v3` retains the v1 DE440-derived ET0 seeds and v2 three-rate lunar secular correction, then adds a generic seven-frequency bounded radial/phase correction. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`; maximum separation error is `8.918 Mm`, down from v2's `47.273 Mm`. Dense maximum separation is `0.992 Mm` through 180 days and `4.196 Mm` through five years, but grows to `27.214 Mm` over ±25 years. NCPE v2 provides a deterministic, self-describing artifact for its current fixed/sampled payloads; it does not yet serialize analytical-Kepler payloads or corrections. The NAIF/CSPICE adapter remains an offline-only source boundary; runtime code has no CSPICE, kernel, network, calendar, or builder dependency.

## Remaining work

- Stellar presentation, cubemap planet materials, atmosphere, and terrain.
- A declared spacecraft-navigation error budget and exceptional higher-fidelity lunar ephemeris if lunar-orbit insertion, close navigation, or precision long-horizon planning requires more than v3. Compact Chebyshev or `SampledHermite` remains the measured fallback, not the default.
- Frustum/occlusion refinement, richer material systems, and later planetary surface features.
- Spacecraft, physics/gameplay, colonies, asset pipeline, and save/replay integration.
