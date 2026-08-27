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

Earth production rendering is the terrain-v5 architecture: a shallow L0–L2 relaxed cube-sphere plus four persistent production spherical-billboard/Eyeball mesh tiers. Both consume patch-aligned `.nccube` data with stable `body / terrain-version / face / level / x / y` identity. A snapped body-fixed pupil and projected-error hysteresis change close-range density without per-frame topology generation; complete parent ownership persists until a complete replacement transaction is resident. Far and unsupported bodies remain on the inexpensive generic whole-body/detailed paths. All representations consume immutable planet material, evaluated-Sun, physical-radius, authoritative body-fixed orientation, and camera-relative presentation authority. Local scenes use finite reversed-Z; Solar uses infinite-far reversed-Z.

## Current baseline

The recovered 11B-6C Desktop baseline retains the Continuous Earth retirement
of the former production-v3 equirectangular SVT and old per-frame radial
Eyeball after terrain-v5 replacement ownership was proven.
The sole production Earth terrain path is now the shallow L0–L2 relaxed
cube-sphere `earth_surface_v5.nccube` hierarchy plus four persistent production
Eyeball tiers. Patch/payload identity is
`body / terrain-version / face / level / x / y`; parent-to-child promotion is a
complete quartet transaction. The checked topology-neutral R16 elevation oracle
remains for CPU clearance and parity. Lawful source datasets and provenance are
preserved. `SolCompact-DE440Validated-v3` and all celestial/body-orientation
authority are unchanged; DE440/CSPICE remains offline-only.

The body-orientation evaluator is a separate pure exact-epoch authority. Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, and Neptune evaluate the official `pck00010.tpc` models directly. The Moon uses a 3,506,540-byte embedded residual pack extracted every six hours over 1900–2100 from official `moon_pa_de440_200625.bpc` through `moon_de440_250416.tf` frame `MOON_ME_DE440_ME421`; pack hash is `0x3BCE78D924EA3532`. The complete 13-term `IAU_MOON` model remains an explicit deterministic fallback outside coverage or on invalid pack provenance. Offline `pxform` comparisons cover J2000, current epoch, ±1/30/365.25 days, arbitrary history/future, fractional knots, and maximum-warp destinations. ET0 orientation hash is `0xD5767D2C2BABE9AA`; all-nine warmed evaluation allocates zero bytes.

The true-distance Solar scene retains focus navigation, moving bodies, deterministic orbit paths/labels/markers, label collision rejection, and extreme-range infinite-far projection correction. Fresh default construction queries UTC exactly once and converts it with the pure pinned-NAIF0012 `SolarUtcTime` boundary; deterministic scenes call the explicit `SimulationInstant` constructor. Thereafter only `SimulationClock.CurrentTime` drives translations, orientations, paths, and rendering. One immutable 15-entry speed catalog spans 0.1×, realtime, and the ordered accelerated presets through 7,776,000×. Native rising-edge comma/period input steps exactly once. Speed feedback remains wall-time driven for two seconds with the final 0.75 seconds fading smoothly, now through a dedicated 210-vertex 5×7 sans bitmap-glyph HUD draw that uses derivative antialiasing, adds no descriptor or per-frame allocation, and leaves ordinary 3×5 body labels unchanged. Orbit paths are sampled from the current simulation instant through the same compact evaluator; this replaces the visibly stale ET0 path policy without a Moon renderer branch. The overview remains capped at 100 AU. Startup and `R` select a deterministic 58 AU Solar Map home pose near the ecliptic normal without changing the current instant; mouse drag orbits in a root-inertial body-centered frame, and time advancement translates that rig without inheriting body spin.

The renderer writes the scene to one persistent FP16 target and tone-maps it in a second subpass using fixed exposure. Solar frames add a deterministic procedural infinite-far background, an evaluated-center/physical-radius stellar Sun with analytic corona, and one generic evaluated-Sun lighting record shared by distant and detailed planets. A 176-byte fixed-width presentation record carries the same material identity and body-fixed-to-root quaternion. The nine non-stellar bodies use repository-authored body-local materials; Saturn's generic annulus is relative to the authoritative equatorial frame. Earth production terrain consumes only terrain-v5 cube payloads; non-Earth bodies remain bounded and never submit Earth production work.

11B-4 retires the provisional 10B-2B environment record, descriptor, pipeline, shader, and draw because no production terrain owner requires them. No replacement atmosphere, cloud shell, fog, or ocean effect is installed. The numeric terrain sea-level floor remains available to surface/clearance policy. Evaluated-Sun lighting, day/night terminators, HDR scene color, and tone mapping are preserved; the compact frame submission is now 688 bytes.

The Earth camera still exposes `Orbital`, `Transition`, and `SurfaceLocal` modes and preserves the ten-metre CPU/GPU terrain floor. The production Eyeball owns four renderer-lifetime tiers with hashes `0x406A2FB30687F0DA`, `0x6A8D7F46E937CAE9`, `0xEA22A4136AFA7884`, and `0xA462CD4E25B748FB`. Tier topology is precomputed once; runtime changes only snapped body-fixed pupil identity, projected-error tier selection, and bounded terrain-v5 payload residency. There is no per-frame mesh-generation dispatch.

11B-7A adds only a dormant host/reference contract for a future anchored
spherical near-surface tier. `PlanetaryAnchoredMeshTierId` derives from the
existing canonical terrain-v5 `SurfaceAnchor`, relaxed-cube cell, tier, and
topology version. Reduced rational cube-surface vertex/edge identities prove
exact parent/child, cross-face, and corner correspondence; existing high/low
FP32 encoding is transport support, never authority. There is no native ABI,
pipeline, draw, streaming, or live ownership change. The next bounded step is
11B-7B GPU physical-height preparation; do not describe the Eyeball as retired
or the external reference renderer as implemented.

11B-7B adds a separate dormant managed/native query ABI and bounded Vulkan
compute/readback proof for physical height. It reconstructs split high/low
body-fixed anchors plus local delta, validates canonical relaxed-cube
addressing, and compares terrain-v5/local-v2 results with
`PlanetaryPhysicalTerrainAuthority.TrySampleHeight`. The proof is explicitly
invoked by focused validation only; it is not in `NcFrameSubmission`, creates
no normal-frame work, and does not change the live terrain-v5/Eyeball renderer.
Keep CPU physical terrain canonical and keep source/fallback decisions based
on authoritative dataset availability rather than GPU residency. The next
bounded milestone is implemented by 11B-7C, not an implied Eyeball retirement.

11B-7C adds an explicitly invoked persistent Vulkan mesh-preparation session.
The dormant whole-body proof topology canonicalizes all cube-face edges and
corners before indexed adjacency; its deterministic hash is
`0x7F3262E7C37D781B`. One compute pass produces authoritative-height displaced
body-fixed split positions plus camera-relative FP32 output, and a synchronized
second pass produces area-weighted outward physical normals. The test path
proves source/fallback parity, Florida and local-v2 displacement, winding,
normal continuity, repeated-dispatch reuse, determinism, and explicit shutdown.
The optional local-v2 pack may be unavailable; that path is explicitly tested
as an oracle-only physical-height fallback. It is not referenced by normal
frame submission and does not draw. Preserve the
terrain-v5 L0–L2 globe and persistent T0–T3 Eyeball as sole live owners until a
separate 11B-7D integration and handoff proof succeeds.

11B-7D retains the dormant boundary and formalizes projected subdivision
demand. Always derive demand from the canonical shared physical edge endpoints,
current camera/projection, target pixel length, and bounded factor; never put
camera or backend state into geographic identity. The reference proof supports
deterministic factor 1/2/4 topologies and runs them through the persistent
11B-7C Vulkan displacement/geometry-normal compute session. A 12.5% local
factor hysteresis is the only stability policy. Tessellation and mesh shaders
are supported on the development RX 6800 XT but remain capability-only; no
pipeline or normal-frame work was added. The explicit
`--scene=planetary-subdivision-diagnostic` path is a simple spherical
density/seam visualization, not physical terrain ownership. Preserve the live
terrain-v5 L0–L2 globe and T0–T3 Eyeball until 11B-7E proves anchored-tier
snapping and transactional promotion.

11A-4D makes the distant convention match those regional and eyeball contracts: the whole-body mesh is body-local, its geometry is rotated by the authoritative quaternion exactly once, and its normal/material/light directions remain body fixed. A live handoff audit corrected the detailed shader's mixed-frame Sun subtraction and established a non-rotating focused-camera frame. Focused tests drive every non-stellar Solar body through detailed-only, transition, and distant-only states while paused; camera position changes around the center at fixed distance while body position/quaternion remain exact. Earth, Mars, Jupiter, Saturn, and Moon additionally pass 1×, 30×, 120×, 600×, 14,400×, and 7,776,000× translation-follow/rotation-independence proofs.

11B-2A makes the camera/reference-frame boundary explicit. `FocusTarget` separates evaluated target position from root-inertial camera orientation. `CameraRelativeRenderPosition` requires `objectRootDouble - cameraRootDouble` before any narrowing or high/low split. The distant, production globe/Eyeball, stellar, ring, orbit, marker, label, and lighting paths follow that ordering. The generic object/triangle path was corrected from shader-side subtraction of independently encoded absolute roots to CPU-relative encoding without changing celestial authority.

11B-2B activates deterministic `SurfaceAnchorFocus` in the Solar camera without requiring a vessel. At 2,000 km the actual camera view ray may acquire a body-fixed physical/elevation-surface point; a miss preserves the prior valid focus, and full anchor focus is reached at 1,000 km. Receding retains the anchor through a separate 3,000 km release threshold. Earth refinement queries the existing loaded CPU elevation oracle, other bodies retain physical-radius intersection, and the anchor reevaluates through current body translation/orientation at all warp presets. Local east/north/up transforms are pole-safe and right-handed. Wheel zoom is logarithmic relative to the physical anchor, a bounded terrain query enforces ten metres of clearance, and camera orientation remains root-inertial throughout. `SceneObject` remains an explicit alternative target, not an acquisition prerequisite.

The former 11B-3A/3B packs are retained only as milestone history and source
provenance. Their runtime files, independent channel/page residency, regional
overlay, builders, descriptors, and shaders are removed. Terrain-v5 global data
is regenerated by `tools/earth_data/build_cube_surface_pack.py`, optional
local-v2 data by `build_local_terrain_pack.py`, and the CPU elevation oracle by
`build_elevation_oracle.py`.

Pre-11B-5 establishes the production terrain distribution boundary. Heavy
generated `.nccube` bytes are not ordinary Git blobs or build-output copies.
`assets/terrain/manifests/earth-surface-v5.json` resolves the canonical
61,484,224-byte payload by SHA-256 into `.novacore/cache/terrain/v1`;
`assets/terrain/manifests/earth-local-v2.json` independently resolves the
optional sparse local refinement payload;
`tools/NovaCore.AssetTool` provides explicit status, verify, fetch, build,
install, and incomplete-cleanup operations. Solar/Earth verifies once and
passes the explicit path to native bounded record residency. Normal CI uses the
tracked 5,032-byte fixture. Read `docs/terrain-assets.md` before 11B-5 payload
work.

## Recommended next milestone

After the dormant 11B-7A identity, 11B-7B GPU physical-height, 11B-7C
displaced-vertex/physical-normal, and 11B-7D projected-subdivision proofs,
11B-7E now provides a dormant T0/T1/T2 anchored spherical-billboard hierarchy
with explicit demand/preparation/readiness/ownership states and atomic
parent/child promotion and retirement. Its Florida, cube-edge, cube-corner,
failure-fallback, memory, and stable-allocation proofs do not enter normal frame
submission. The launcher preset maps only to
`--scene=planetary-anchored-billboard-diagnostic`; it is not renderer authority.
11B-7F should prove a continuous physically useful orbital-to-Florida vertical
slice with bounded visible participation before any production-owner change.
Do not restore independent
equirectangular pages, sparse regional runtime overlays, or the retired radial
compute owner. Production atmosphere/cloud rendering is intentionally absent
and must be reconstructed later against the retained terrain-v5 contract.

## Recommended models

Use **Terra Medium** for documentation, focused tests, managed wiring, and simple rendering changes. Use **Sol High** for Vulkan, shaders, GPU compute, synchronization, ABI-sensitive native work, and renderer architecture changes.
