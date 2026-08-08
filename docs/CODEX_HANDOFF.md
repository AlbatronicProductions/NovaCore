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

The renderer shares one normalized 16×16 cube-sphere grid. Near-field patches use deterministic quadtree identities and native stitch metadata; GPU selection is the Earth production path, with CPU/GPU validation mode retained. The same address now reaches production level 22, and a bounded renderer-owned cache supplies versioned deterministic elevation to the unchanged grid. Far bodies use a shared whole-body mesh and deterministic distant/detail handoff. Both representations consume the same immutable planet-material identity and body-local procedural response. Solar paths, labels, markers, HDR lighting, the stellar Sun, and generic rings are presentation paths rooted in the same evaluated snapshot. Local scenes use finite reversed-Z; Solar uses infinite-far reversed-Z. Both preserve the double camera-relative authority boundary.

## Current milestone

Milestone 9A-4F-2 and the 10B-2B near-surface environment foundation are complete in the working tree. `SolCompact-DE440Validated-v3` preserves the v1 DE440-grounded ET0 seeds and v2 mean/node/periapsis correction, adding a generic immutable bounded-periodic catalog. The Moon uses seven distinct frequencies containing four radial and four phase series; all other analytical bodies use identity entries. Fixed-report Moon maximum/RMS position is `34.077 / 10.243 Mm`, velocity is `76.297 / 32.262 m/s`, and maximum separation error is `8.918 Mm`. Definition hash is `0x493FE8B1E867110F`; residual hash is `0xD5E2E00FF5F1C2C2`. Runtime stays below 25 µs for all ten bodies and allocates zero warmed bytes. DE440/CSPICE remains offline-only. The fixed Sun and omitted EMB are explicit compact-runtime policies.

The true-distance Solar scene retains focus navigation, moving bodies, deterministic orbit paths/labels/markers, label collision rejection, and extreme-range infinite-far projection correction. Orbit paths are sampled from the current simulation instant through the same compact evaluator; this replaces the visibly stale ET0 path policy without a Moon renderer branch. The overview remains capped at 100 AU. Startup and `R` now select a deterministic 58 AU Solar Map home pose near the ecliptic normal without changing the current instant; mouse drag switches the same focus-orbit camera into free 3D.

The renderer writes the scene to one persistent FP16 target and tone-maps it in a second subpass using fixed exposure. Solar frames add a deterministic procedural infinite-far background, an evaluated-center/physical-radius stellar Sun with analytic corona, and one generic evaluated-Sun lighting record shared by distant and detailed planets. A 144-byte fixed-width presentation record carries the same material identity/response to both planet representations. The nine non-stellar bodies use repository-authored body-local procedural materials; Saturn additionally supplies a generic ring contract rendered with one persistent annulus and ordered far/body/near draws. Earth near field uses viewport-scaled sparse GPU selection through level 22, an 8,192-slot deterministic elevation cache, continuous repository-authored procedural terrain, shader-FP64 deep addressing, and reversed-Z D32 depth.

10B-2B adds one 128-byte body-associated environment record and extends the frame ABI to 640 bytes. Earth uses a bounded 100 km atmosphere, 2–11 km procedural cloud shell, and 180 m presentation sea level. The persistent fullscreen environment pipeline draws after the infinite background and before opaque geometry into the existing HDR target; it adds no depth attachment or per-frame image. Distant and detailed materials share body-direction cloud density, evaluated-Sun brightness, bounded cloud shadows, and raw-terrain sea-level classification. Below sea level the existing grid clamps to a spherical water surface with radial normals, bounded wave perturbation, roughness/specular response, and Fresnel color.

The Earth camera now exposes `Orbital`, `Transition`, and `SurfaceLocal` modes. It smoothsteps into a captured east/north/up body-local anchor from 1,000 km to 100 km, applies bounded local yaw/pitch below that, refines wheel steps near the surface, and preserves the established two-metre floor. `PlanetarySurfaceFocus` is presentation-only and reusable for a future vehicle focus; it does not own or mutate vehicle or celestial transforms. Fixed exposure, procedural geography, static clouds, and the compact atmosphere/ocean approximations remain known visual limits.

## Recommended next milestone

**Milestone 10B-2C: production-oriented surface/environment quality.** Build on the measured 10B-2B camera, atmosphere/cloud/ocean ABI, 10B-2A deep-LOD/residency, and reversed-depth contracts without changing celestial truth or patch identity. Prioritize measured close-range cloud detail, surface material quality, lawful production data strategy, and color/exposure refinement. Keep physical body rotation, vegetation, surface objects, additional moons, city lights, ring shadows, spacecraft, and navigation separate. The spacecraft-navigation fidelity policy remains unchanged: Sol v3 is suitable context and approximate translunar gameplay authority, not precision close-lunar truth.

## Recommended models

Use **Terra Medium** for documentation, focused tests, managed wiring, and simple rendering changes. Use **Sol High** for Vulkan, shaders, GPU compute, synchronization, ABI-sensitive native work, and renderer architecture changes.
