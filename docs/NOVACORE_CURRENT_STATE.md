# NovaCore current state

## Repository state

Inspect `git status --short` before editing. The working tree currently contains unstaged Milestone 10A presentation work; preserve it unless the active task explicitly changes it. Repository code and tests are authoritative if this document ever becomes stale.

## Current milestone: 10A-9B

NovaCore now has a deterministic, true-distance Solar System presentation built from the existing `SolAnalytical` definition. `--scene=sol` evaluates Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune at the current `SimulationInstant`, root-resolves them through `CelestialSystemEvaluator`, and publishes immutable `PlanetaryPresentationSnapshot` data to Graphics. It is an evaluated analytical presentation, not a production DE440 runtime artifact.

The scene includes deterministic orbit paths, body labels, screen-space reticle markers, number-row focus, orbit/zoom camera controls, and `SimulationClock` rate/pause integration. Labels use deterministic NDC collision rejection: focused body, Sun, Earth, Moon, then stable body order, with `0.004` NDC clearance. A rejected label never suppresses its marker or physical body. Solar uses an infinite-far Vulkan projection and a 100 AU presentation clamp, preserving the established double-precision camera-relative subtraction through the prior finite-depth boundary.

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

Milestone 9 established immutable generic celestial definitions and a pure evaluator for fixed, circular, analytical-Kepler, and sampled trajectories. NCPE v2 provides a deterministic, self-describing offline artifact with byte-only runtime reconstruction and stored/neutral/reconstructed hash agreement. The NAIF/CSPICE adapter remains an offline-only source boundary; runtime code has no CSPICE, kernel, network, or builder dependency.

## Remaining work

- Stellar presentation, cubemap planet materials, atmosphere, and terrain.
- Production sampled Solar artifacts from verified offline source data; no production DE440 runtime artifact is claimed.
- Frustum/occlusion refinement, richer material systems, and later planetary surface features.
- Spacecraft, physics/gameplay, colonies, asset pipeline, and save/replay integration.
