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

The renderer shares one normalized 16×16 cube-sphere grid. Near-field patches use deterministic quadtree identities and native stitch metadata; GPU selection is the Earth production path, with CPU/GPU validation mode retained. Far bodies use a shared whole-body mesh and deterministic distant/detail handoff. Both representations consume the same immutable planet-material identity and body-local procedural response. Solar paths, labels, markers, HDR lighting, the stellar Sun, and generic rings are presentation paths rooted in the same evaluated snapshot. Solar’s infinite-far projection is scoped to that overview; other camera paths remain finite.

## Current milestone

Milestone 9A-4F-2 and the 10B-1C Solar presentation foundation are complete in the working tree. `SolCompact-DE440Validated-v3` preserves the v1 DE440-grounded ET0 seeds and v2 mean/node/periapsis correction, adding a generic immutable bounded-periodic catalog. The Moon uses seven distinct frequencies containing four radial and four phase series; all other analytical bodies use identity entries. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`, velocity is `76.297 / 32.262 m/s`, and maximum separation error is `8.918 Mm`. Definition hash is `0x493FE8B1E867110F`; residual hash is `0xD5E2E00FF5F1C2C2`. Runtime stays below 25 µs for all ten bodies and allocates zero warmed bytes. DE440/CSPICE remains offline-only. The fixed Sun and omitted EMB are explicit compact-runtime policies.

The true-distance Solar scene retains focus navigation, moving bodies, deterministic orbit paths/labels/markers, label collision rejection, and extreme-range infinite-far projection correction. Orbit paths are sampled from the current simulation instant through the same compact evaluator; this replaces the visibly stale ET0 path policy without a Moon renderer branch. The overview remains capped at 100 AU. Startup and `R` now select a deterministic 58 AU Solar Map home pose near the ecliptic normal without changing the current instant; mouse drag switches the same focus-orbit camera into free 3D.

The renderer writes the scene to one persistent FP16 target and tone-maps it in a second subpass using fixed exposure. Solar frames add a deterministic procedural infinite-far background, an evaluated-center/physical-radius stellar Sun with analytic corona, and one generic evaluated-Sun lighting record shared by distant and detailed planets. A 144-byte fixed-width presentation record carries the same material identity/response to both planet representations. The nine non-stellar bodies use repository-authored body-local procedural materials; Saturn additionally supplies a generic ring contract rendered with one persistent annulus and ordered far/body/near draws. Extent-aware focus framing uses the physical body, existing stellar extent, or optional ring outer radius. Overlay metadata carries deterministic orbit opacity and marker/label visibility; local views retain only the focused/direct hierarchy, and orbit rendering precedes opaque bodies so physical disks provide occlusion. Resize rebuilds all swapchain-dependent HDR resources after device idle. There is no automatic exposure, multi-pass bloom, image texture asset, atmosphere, or depth attachment yet.

## Recommended next milestone

**Measured atmosphere/cloud and surface-material planning.** Use the completed 10B-1 Solar presentation as the baseline for a deliberately scoped atmosphere pass, then decide whether lawful authored image assets or expanded procedural materials provide the next highest visual return. Keep terrain, physical body rotation, additional moons, city lights, ring shadows, spacecraft, and navigation as separate measured milestones. The spacecraft-navigation fidelity policy remains unchanged: Sol v3 is suitable context and approximate translunar gameplay authority, not precision close-lunar truth.

## Recommended models

Use **Terra Medium** for documentation, focused tests, managed wiring, and simple rendering changes. Use **Sol High** for Vulkan, shaders, GPU compute, synchronization, ABI-sensitive native work, and renderer architecture changes.
