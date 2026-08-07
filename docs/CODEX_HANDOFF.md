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

Milestone 10A-9B is complete in the working tree: true-distance SolAnalytical Solar visualization, focus navigation, moving bodies, deterministic orbit paths/labels/markers, label collision rejection, and extreme-range infinite-far projection correction. The Solar overview remains capped at 100 AU and resets at 45 AU. Validate that work with the focused Graphics tests, native Debug x64 build under `vcvars64`, the Earth CPU/GPU parity sample, the triangle sample, and `git diff --check`.

## Recommended next milestone

**10A-10 — stellar presentation foundation.** Add a presentation-only Sun/stellar treatment that consumes the existing immutable Solar snapshot and camera-relative transport. Do not introduce physical lighting authority, alter celestial evaluation, or begin terrain/material work in the same slice.

## Recommended models

Use **Terra Medium** for documentation, focused tests, managed wiring, and simple rendering changes. Use **Sol High** for Vulkan, shaders, GPU compute, synchronization, ABI-sensitive native work, and renderer architecture changes.
