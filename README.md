# NovaCore

**A precision space-simulation and game-engine project built for continuous planetary scale.**

![NovaCore Earth against the Milky Way](docs/images/novacore-11a-earth-milky-way.png)

<p align="center">
  <img src="docs/images/novacore-11a-earth-moon.png" alt="NovaCore Earth and Moon presentation" width="49%">
  <img src="docs/images/novacore-11a-earth-sun.png" alt="NovaCore Earth, Moon, and Sun presentation" width="49%">
</p>
<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="NovaCore Mars presentation" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="NovaCore Solar overview at maximum time warp" width="49%">
</p>

NovaCore is an experimental space-simulation and game-engine project built from the ground up for enormous differences in scale. It combines deterministic simulation authority, high-precision reference frames, real planetary data, and a native Vulkan renderer.

The goal is simple to describe, even if it is difficult to build:

> **Create one continuous simulation capable of taking you from a planetary surface through launch, atmosphere, orbit, and interplanetary space.**

NovaCore combines astronomical simulation, planetary rendering, large-scale coordinate systems, terrain, real planetary data, and a custom Vulkan renderer into a single engine.

## What can NovaCore do today?

NovaCore can already:

- Simulate the major bodies of the Solar System and their motion.
- Start from the current real-world time and accelerate simulation time from 0.1× to 7,776,000×.
- Render planets and moons across astronomical distances.
- Approach planetary surfaces while maintaining numerical precision.
- Traverse from orbit to the near surface through one production spherical-billboard terrain runtime.
- Stream real NASA/NOAA Earth imagery and elevation data.
- Retain body-fixed Earth geography through `SurfaceAnchor`, terrain-aware camera, and anchored surface-object foundations.
- Place and reevaluate a deterministic Florida launch-site proof on the physical Earth terrain authority.
- Light planets using the simulated position of the Sun, producing coherent day/night boundaries.
- Render distinct planetary appearances, Saturn's rings, orbital paths, labels, and markers.
- Explore the Solar System using Solar Map and free 3D camera modes.
- Evaluate fixed-attitude and rigid-body spacecraft rotation, torque transactions, and a bounded SAS guidance proof without presenting a finished flight game.
- Reproduce simulation and rendering results deterministically for testing and validation.

## The goal

NovaCore is being developed toward a simulation where scale does not define the boundaries of the experience:

**Surface → Launch → Atmosphere → Orbit → Interplanetary space**

Rather than treating these as completely separate environments, NovaCore is being designed so they can exist within the same underlying simulation.

NovaCore is still under active development. Production atmospheres, close volumetric clouds, physical oceans, weather, complete spacecraft flight gameplay, colonies, maneuver planning, and navigation systems remain future work.

## A simple design rule

NovaCore is built around one important principle:

> **Simulation owns truth. Rendering owns presentation.**

In plain language, the simulation decides where objects really are, how large they are, how time is progressing, and how the universe is behaving. The renderer decides how that information should look on screen.

Keeping those responsibilities separate allows visual systems to become more sophisticated without changing the underlying simulated universe.

---

# Technical overview

Developer orientation: [engineering rules](ENGINEERING_RULES.md) →
[current state](docs/NOVACORE_CURRENT_STATE.md) → [architecture](docs/architecture.md)
→ [repository ownership map](docs/repository-structure.md) → the relevant subsystem
guide. [Engineering history](docs/engineering-evidence/README.md) supplies provenance;
it is not prerequisite onboarding or a competing current specification.

The rest of this README goes deeper into NovaCore's current engineering state, architecture, renderer, planetary systems, validation, and limitations.

NovaCore currently combines a C# simulation core with a native Vulkan renderer, compact DE440-validated Solar propagation, camera-relative high-precision transport, GPU-driven planetary rendering, HDR presentation, procedural planetary materials, Saturn rings, and an interactive Solar map.

## Current technical capabilities

### Celestial simulation

- **DE440-validated compact Solar runtime** — `SolCompact-DE440Validated-v3` evaluates the current Solar model without loading CSPICE or DE440 kernels at runtime.
- **Current-real-world UTC startup** sampled once, then governed exclusively by exact `SimulationInstant` and `SimulationClock` authority.
- **Deterministic arbitrary-time evaluation** with 15 ordered simulation-speed presets from 0.1× through 7,776,000×.
- **Compact lunar corrections** — generic secular plus bounded periodic corrections improve the Moon while retaining a lightweight analytical runtime.
- **Authoritative planetary orientation** — direct-epoch axial orientation and rotation remain independent of the camera and rendering cadence.
- **High-precision lunar orientation** — a compact, kernel-free DE440 lunar frame pack covers 1900–2100, with explicit deterministic `IAU_MOON` fallback outside validated coverage.
- **Zero-allocation warmed celestial evaluation**, measured below 25 µs for all ten currently presented Solar bodies on the development machine.
- **NCPE v2** artifact reconstruction and deterministic definition/hash verification.

### Rendering and presentation

- **Native Vulkan renderer** with double-precision camera-relative subtraction before GPU float transport.
- **New Earth Renderer** with 18 immutable NCSM1 scale-mesh levels spanning orbital to near-surface representation.
- **One canonical physical surface** — body-fixed `H(bodyDirection)` defines Earth height independently of camera, topology density, pupil placement, and GPU residency.
- **Moving snapped pupil** — a camera-facing spherical representation retains exact lattice identity, reuses samples across movement, and changes scale through projected-error selection with hysteresis.
- **Persistent scale resources and transactional GPU generations** — immutable topology is reused when a level is revisited; one current and at most one incoming generation prepare asynchronously, and fence-confirmed atomic publication preserves exactly one Earth presentation owner, including valid zero-visible views.
- **Conservative GPU visibility** — curved-patch planet occlusion and screen/frustum rejection feed compacted indexed-indirect work without removing required base coverage.
- **Prepared physical terrain and bounded tessellation** — full relief is prepared before TES, which interpolates the published surface. KSA-parity per-edge factors retain the 50-metre near-camera refinement contract and future displacement capacity; base terrain remains depth-owning outside that range.
- **FP64 physical/world authority** with camera-relative GPU presentation for stable rendering across planetary and astronomical scales.
- **Correct body-fixed handoff conventions** across detailed, transitional, and distant paths, including a dedicated outward-winding convention for the shared distant sphere.
- **FP16 HDR scene color** with fixed exposure and ACES-style tone mapping.
- **Procedural deep-space background** and dedicated stellar-Sun presentation with controlled HDR corona/glow.
- **Evaluated-Sun lighting** for coherent planetary day/night terminators.
- **Generic procedural planet materials** with distinct presentations for Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune.
- **Generic ring presentation**, currently demonstrated by Saturn.
- **NASA/NOAA Earth data pipeline** using canonical east-positive, right-handed `.nccube` records, a checked topology-neutral elevation oracle, and optional USGS-derived Florida refinement. These packages improve sampling and material fidelity; they do not create a second physical surface.
- **Surface-relative foundations** with terrain-aware camera clearance, immutable body-fixed `SurfaceAnchor` identity, anchored surface objects, and a deterministic Florida launch-site proof.
- **Solar Map and Free 3D camera modes** with deterministic home framing and body focus.
- **Authoritative orbit visualization** sourced from the same `CelestialSystemEvaluator` used for body motion.
- **Deterministic label/marker hierarchy** with collision suppression, distance-aware presentation, and focused-body clutter reduction.

### Validation

NovaCore treats correctness tooling as part of the engine rather than as presentation behavior:

- CPU-reference / GPU planetary parity validation.
- Vulkan validation-layer verification.
- Deterministic celestial and topology hashes.
- Camera/reference-frame precision tests.
- Native, managed, Solar-scene, Earth-LOD, resize, and triangle regression coverage.

The banked M13.6 production baseline preserves the accepted native 3440×1440 Florida result and orbit-to-near-surface coverage on the NCSM1 New Earth Renderer. Focused regressions cover all 18 NCSM1 levels, persistent level reuse, moving-pupil identity, atomic and zero-visible publication, one-owner coverage, body authority, body-fixed height and normal parity, topology-family winding, KSA-parity tessellation responsibility, conservative horizon coverage beyond the TES range, and Vulkan validation. Camera motion cannot mutate Earth orientation, body-fixed geography, or canonical physical height.

The CPU reference/parity path is a development and regression oracle; the intended production planetary path remains GPU-driven.

## How NovaCore is structured

```text
Offline astronomical truth
        │
        ▼
JPL/NAIF DE440 + CSPICE
        │  validation only
        ▼
Compact versioned Solar definitions

Runtime simulation
SimulationClock
        ↓
CelestialSystemEvaluator
        ↓
Parent-relative / reference-frame resolution
        ↓
Immutable presentation snapshots
        ↓
Native Vulkan renderer
        ↓
HDR / GPU planets / materials / rings / overlays
```

The runtime never needs to stream DE440 kernels or call CSPICE. High-authority astronomy is used offline to measure and validate compact deterministic models. The lunar orientation path is likewise extracted offline from official NAIF/JPL DE440 kernels into a checked 3.5 MB residual pack; normal evaluation remains allocation-free and kernel-independent.

For exceptional future precision requirements, a bounded sampled or Chebyshev ephemeris layer can be introduced where measurement proves it necessary rather than becoming the default representation for every body.

## Run the Solar System

From the repository root on a configured Windows development environment:

```powershell
dotnet run --project tools/NovaCore.AssetTool -- status earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -- status earth-florida-m12
# On a fresh clone, explicitly populate the verified runtime cache:
dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v5
pwsh tools/earth_data/acquire_florida_m12.ps1
dotnet run --project tools/NovaCore.AssetTool -- build earth-florida-m12
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol
```

Heavy generated terrain payloads are not ordinary Git blobs and are not copied
into each build output. The tracked manifests identify the required global and
optional local immutable bytes; the explicit asset tool verifies, fetches when
a remote is configured, or deterministically regenerates them into an ignored
content-addressed cache.
Normal runtime never downloads terrain implicitly. See
[Production terrain assets](docs/terrain-assets.md).

After it has already been built:

```powershell
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug --no-build -- --scene=sol
```

### Launch NovaCore

For a point-and-click development workflow, build the lightweight Windows launcher in the configuration you intend to run:

```powershell
dotnet build tools/NovaCore.Launcher/NovaCore.Launcher.csproj -c Release
```

Then double-click `tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`.
The launcher provides Solar overview, Earth orbital, Earth 700 km, Earth —
Fullscreen Native, Florida launch-site, and one screen-space subdivision
  diagnostic workflow, plus the accepted **New Earth Renderer**. Window mode, native or explicit client resolution, and
normal/performance/Vulkan diagnostics are selected independently. The launcher
shows the resolved client dimensions and starts the matching Triangle runtime:
a Release launcher uses Release and a Debug launcher uses Debug. No shell
environment setup is required.

All six supported Earth/Solar/Florida routes use NCSM1 and generation-4 physical
terrain, including **New Earth Renderer**. Scene, focus, altitude, camera and
Florida initialization remain route-specific. Terrain-v5 global bootstrap is
temporary presentation until complete NCSM1 publication; non-Earth rendering
remains independent. See [production consolidation](docs/production-consolidation.md)
for route-ownership provenance.

Current Solar-scene controls include mouse drag for free orbiting, mouse wheel zoom, number-key body focus, `.` / `,` simulation-rate changes, Space pause/resume, and `R` to return to the deterministic Solar Map home view.

## Current planetary baseline

The latest banked production milestone is **M13.6 — Prefer CPU-cached memory
for terrain residency keys**, commit `90fef759243dd67918cd556e19027159e5a5eada`,
tag `m13.6-cpu-cached-terrain-residency-keys`.
**M13 — NCSM1 Terrain Performance is CLOSED. There is no M13.7.**

**NCSM1 / New Earth Renderer** remains the production Earth renderer. M13 moved
expensive terrain work to appropriate shader, preparation, material and memory
boundaries while preserving canonical physical terrain, accepted Florida
presentation and future tessellation capacity.

The production chain is canonical `H(bodyDirection)` → persistent NCSM1 scale
and snapped pupil → prepared full physical relief and normals → conservative
visibility and compaction → bounded hardware tessellation and interpolation →
indexed indirect raster. Complete, fence-confirmed generations publish atomically;
the current generation remains the sole Earth owner while its replacement prepares.

Topology density controls the rendered approximation, not physical truth.
Camera movement and pupil replacement cannot change canonical body-fixed height.
Gameplay and camera clearance retain full physical queries. Ordinary shading
avoids unused diagnostic work and zero-contribution material noise; terrain
working memory prefers GPU locality and residency keys prefer CPU caching where
compatible.

M13 closure does not mean universal 120 FPS terrain performance or finished
visuals. Regional replacement remains the principal measured performance
limitation. Terrain materials, finer representation continuity, environment and
spacecraft/surface gameplay remain future responsibilities. See
[current engineering state](docs/NOVACORE_CURRENT_STATE.md) for the measured
performance envelope, accepted limitations and closure rationale.

A rare full pupil rebase can change the triangulated presentation while canonical
physical terrain stays fixed. Historical measured continuity bounds and the
physical/presentation distinction are documented in the
[renderer contract](docs/planetary-rendering.md#presentation-continuity).

## Accuracy and current scope

`SolCompact-DE440Validated-v3` is intended for Solar presentation, deterministic time warp, and approximate translunar gameplay. It is **not** JPL DE440 playback and should not be described as a precision lunar-navigation ephemeris.

The new high-precision Moon orientation improves pole, prime-meridian, and physical-libration presentation; it does not improve lunar translation. Precision translunar targeting, lunar orbit insertion, close lunar navigation, or other requirements that exceed the compact translational model's measured accuracy will receive a dedicated higher-fidelity ephemeris layer when those requirements exist.

NovaCore now has a production spherical-billboard terrain architecture, but global source fidelity remains shallow outside installed regional data. Production atmosphere, clouds, water/coastlines, physical oceans, weather, complete spacecraft flight gameplay, colonies, maneuver planning, and SOI/patched-conic navigation are not yet implemented.

## Roadmap

**Future planetary and gameplay responsibilities**

Surface interaction, launch and landing; planetary environment; richer terrain
materials and detail; water/coastlines; and finer presentation continuity remain
future work. Next production-front authorization returns to Project Control;
this list does not assign implementation order or a milestone.

**Future flight and navigation**

Extended local/reference-frame transitions → SOI/patched-conic policy → complete spacecraft force/torque and flight integration → maneuver planning and navigation → higher-fidelity ephemerides wherever measured accuracy requires them.

The goal is to add those systems without weakening the existing authority boundary between simulation and presentation.

## Build and test

Windows development currently uses .NET, MSVC x64, Ninja/CMake, and Vulkan.

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja --config Debug

dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
```

See the engineering documentation for the complete scoped build, validation, and architecture requirements.

## Documentation

- [Engineering rules](ENGINEERING_RULES.md)
- [Architecture](docs/architecture.md)
- [Current engineering state](docs/NOVACORE_CURRENT_STATE.md)
- [Engineering handoff](docs/CODEX_HANDOFF.md)
- [Planetary rendering](docs/planetary-rendering.md)
- [Production terrain assets](docs/terrain-assets.md)
- [Celestial simulation](docs/celestial-simulation.md)
- [Ephemeris dataset format](docs/ephemeris-dataset-format.md)
- [Ephemeris runtime loader](docs/ephemeris-runtime-loader.md)
- [NAIF source adapter](docs/naif-source-adapter.md)
- [Build on Windows](docs/build-windows.md)

---

**NovaCore is a work in progress.** The repository documents implemented systems and measured limitations explicitly; roadmap items are not presented as completed features.

Unrelated retirement findings are tracked in [the subsequent debt ledger](docs/repository-debt-retirement.md). Deferral is not permanent acceptance.
