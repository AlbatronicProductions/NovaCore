# NovaCore handoff

## Start here

Before editing, read `README.md`, `ENGINEERING_RULES.md`, `docs/NOVACORE_CURRENT_STATE.md`, `docs/architecture.md`, and `docs/planetary-rendering.md`. Inspect `git status --short`, then inspect the exact implementation and tests in scope. Repository contents and tests win over this handoff if they differ. Preserve all existing unstaged work; do not reset, restore, stage, commit, tag, or push unless explicitly directed.

## Architectural invariants

- Simulation owns celestial truth. Graphics receives immutable evaluated presentation data only.
- Do not change authoritative body centers, physical radii, hierarchy, ephemerides, simulation time, or deterministic identity to solve rendering issues.
- Subtract root-resolved camera and body positions in doubles before GPU float transport.
- NCPE is storage. `CelestialSystemDefinition` plus `CelestialSystemEvaluator` is runtime celestial authority.
- CSPICE, NAIF kernels, source manifests, and builders are offline-only; no runtime dependency is allowed.
- GPU LOD, culling, labels, markers, and representation handoff are presentation-only. Keep CPU reference implementations as correctness oracles where equivalent GPU logic exists.
- Native/managed ABI changes require explicit fixed-width layout validation and focused native/managed tests. Native Vulkan resources have explicit native ownership.

## Renderer architecture

`CelestialSystemDefinition` → `CelestialSystemEvaluator` → immutable `PlanetaryPresentationSnapshot` → managed camera-relative submission → narrow native ABI → Vulkan renderer.

The renderer shares one normalized 16×16 cube-sphere grid. Near-field patches use deterministic quadtree identities and native stitch metadata; GPU selection is the Earth production path, with CPU/GPU validation mode retained. Far bodies use a shared whole-body mesh and deterministic distant/detail handoff. Solar paths, labels, and markers are presentation overlays rooted in the same evaluated snapshot. Solar’s infinite-far projection is scoped to that overview; other camera paths remain finite.

## Current milestone

Milestone 9A-4F-2 and the 10A-9B rendering baseline are complete in the working tree. `SolCompact-DE440Validated-v3` preserves the v1 DE440-grounded ET0 seeds and v2 mean/node/periapsis correction, adding a generic immutable bounded-periodic catalog. The Moon uses seven distinct frequencies containing four radial and four phase series; all other analytical bodies use identity entries. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`, velocity is `76.297 / 32.262 m/s`, and maximum separation error is `8.918 Mm`. Definition hash is `0x493FE8B1E867110F`; residual hash is `0xD5E2E00FF5F1C2C2`. Runtime stays below 25 µs for all ten bodies and allocates zero warmed bytes. DE440/CSPICE remains offline-only. The fixed Sun and omitted EMB are explicit compact-runtime policies.

The true-distance Solar scene retains focus navigation, moving bodies, deterministic orbit paths/labels/markers, label collision rejection, and extreme-range infinite-far projection correction. Orbit paths are now sampled from the current simulation instant through the same compact evaluator; this replaces the visibly stale ET0 path policy without a Moon renderer branch. The overview remains capped at 100 AU and resets at 45 AU.

## Recommended next milestone

**Spacecraft-navigation foundation with an explicit fidelity policy.** Treat Sol v3 as celestial context for launch-to-LEO and as an approximate first translunar-transfer authority, but not as precision lunar-orbit-insertion or close-navigation truth. Before those regimes, state the admissible position/separation budget and add an exceptional offline-derived lunar Chebyshev or `SampledHermite` layer if required. Active spacecraft remain a separate dynamics authority. If presentation remains the priority, 10A-10 stellar treatment is still independent and consumes the existing immutable Solar snapshot.

## Recommended models

Use **Terra Medium** for documentation, focused tests, managed wiring, and simple rendering changes. Use **Sol High** for Vulkan, shaders, GPU compute, synchronization, ABI-sensitive native work, and renderer architecture changes.
