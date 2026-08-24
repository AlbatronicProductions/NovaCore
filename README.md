# NovaCore

**A space simulation engine built to handle everything from entire solar systems to planetary surfaces.**

![NovaCore Earth against the Milky Way](docs/images/novacore-11a-earth-milky-way.png)

<p align="center">
  <img src="docs/images/novacore-11a-earth-moon.png" alt="NovaCore Earth and Moon presentation" width="49%">
  <img src="docs/images/novacore-11a-earth-sun.png" alt="NovaCore Earth, Moon, and Sun presentation" width="49%">
</p>
<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="NovaCore Mars presentation" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="NovaCore Solar overview at maximum time warp" width="49%">
</p>

NovaCore is an experimental space simulation and rendering engine built from the ground up for enormous differences in scale.

The goal is simple to describe, even if it is difficult to build:

> **Create one continuous simulation capable of taking you from the scale of a solar system all the way down to the surface of a world.**

NovaCore combines astronomical simulation, planetary rendering, large-scale coordinate systems, terrain, real planetary data, and a custom Vulkan renderer into a single engine.

## What can NovaCore do today?

NovaCore can already:

- Simulate the major bodies of the Solar System and their motion.
- Start from the current real-world time and accelerate simulation time from 0.1× to 7,776,000×.
- Render planets and moons across astronomical distances.
- Approach planetary surfaces while maintaining numerical precision.
- Transition between global planetary rendering and near-surface terrain rendering.
- Stream real NASA/NOAA Earth imagery and elevation data.
- Light planets using the simulated position of the Sun, producing coherent day/night boundaries.
- Render distinct planetary appearances, Saturn's rings, orbital paths, labels, and markers.
- Explore the Solar System using Solar Map and free 3D camera modes.
- Reproduce simulation and rendering results deterministically for testing and validation.

## The goal

NovaCore is being developed toward a simulation where scale does not define the boundaries of the experience:

**Solar System → Planet → Orbit → Atmosphere → Surface**

Rather than treating these as completely separate environments, NovaCore is being designed so they can exist within the same underlying simulation.

NovaCore is still under active development. Advanced atmospheres, close volumetric clouds, physical oceans, weather, spacecraft simulation, colonies, maneuver planning, and navigation systems remain future work.

## A simple design rule

NovaCore is built around one important principle:

> **Simulation owns truth. Rendering owns presentation.**

In plain language, the simulation decides where objects really are, how large they are, how time is progressing, and how the universe is behaving. The renderer decides how that information should look on screen.

Keeping those responsibilities separate allows visual systems to become more sophisticated without changing the underlying simulated universe.

---

# Technical overview

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
- **GPU-driven planets** with a shallow relaxed cube-sphere for orbital/global presentation and a persistent, body-fixed spherical-billboard surface for the near field.
- **Distant/detail handoff** sharing authoritative physical center, radius, material identity, and Solar-light direction.
- **Correct body-fixed handoff conventions** across detailed, transitional, and distant paths, including a dedicated outward-winding convention for the shared distant sphere.
- **FP16 HDR scene color** with fixed exposure and ACES-style tone mapping.
- **Procedural deep-space background** and dedicated stellar-Sun presentation with controlled HDR corona/glow.
- **Evaluated-Sun lighting** for coherent planetary day/night terminators.
- **Generic procedural planet materials** with distinct presentations for Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune.
- **Generic ring presentation**, currently demonstrated by Saturn.
- **NASA/NOAA terrain-v4 Earth surface authority** using patch-aligned relaxed cube-sphere `.nccube` records, transactional parent/child residency, persistent production Eyeball tiers, and a checked topology-neutral CPU elevation oracle.
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
dotnet run --project tools/NovaCore.AssetTool -- status earth-surface-v4
# On a fresh clone, explicitly populate the verified runtime cache:
dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v4
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol
```

Heavy generated terrain payloads are not ordinary Git blobs and are not copied
into each build output. The tracked manifest identifies the required immutable
bytes; the explicit asset tool verifies, fetches when a remote is configured,
or deterministically regenerates them into an ignored content-addressed cache.
Normal runtime never downloads terrain implicitly. See
[Production terrain assets](docs/terrain-assets.md).

After it has already been built:

```powershell
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug --no-build -- --scene=sol
```

Current Solar-scene controls include mouse drag for free orbiting, mouse wheel zoom, number-key body focus, `.` / `,` simulation-rate changes, Space pause/resume, and `R` to return to the deterministic Solar Map home view.

## Current checkpoint — NovaCore 11B-4

**NovaCore 11B-4 — Production Spherical Billboard Planetary Surface** replaces the former Earth terrain experiments with one production-owned, body-fixed surface architecture:

- a terrain-v4 cube-face `.nccube` pack generated offline from lawful NASA/NOAA sources;
- a shallow relaxed cube-sphere for stable orbital and global presentation;
- a persistent KSA-style spherical-billboard / Eyeball renderer for close-range terrain density;
- four precomputed mesh tiers (T0–T3), selected without rebuilding the surface every frame;
- a snapped viewer-facing pupil with deterministic hysteresis, so the represented geography remains body fixed as the camera moves;
- bounded transactional residency, where complete patch dependencies become ready before ownership changes;
- real-Earth elevation displacement and FP64 camera-relative reconstruction before GPU narrowing;
- explicit production isolation, keeping unsupported planets on the inexpensive generic path.

The old equirectangular virtual-texture ownership model and per-frame radial Eyeball terrain path are retired. Terrain-v4 is deliberately shallow today: its current data proves stable orbital-to-ground ownership and transport, while higher-frequency terrain payloads and close-ground fidelity remain future work. All rendering remains downstream of the authoritative celestial simulation.

## Accuracy and current scope

`SolCompact-DE440Validated-v3` is intended for Solar presentation, deterministic time warp, and approximate translunar gameplay. It is **not** JPL DE440 playback and should not be described as a precision lunar-navigation ephemeris.

The new high-precision Moon orientation improves pole, prime-meridian, and physical-libration presentation; it does not improve lunar translation. Precision translunar targeting, lunar orbit insertion, close lunar navigation, or other requirements that exceed the compact translational model's measured accuracy will receive a dedicated higher-fidelity ephemeris layer when those requirements exist.

NovaCore now has a production-owned terrain architecture, but its terrain-v4 payload is still shallow. The former provisional atmosphere/cloud renderer has been retired; production atmosphere, clouds, water/coastlines, physical oceans, weather, spacecraft simulation, colonies, maneuver planning, and SOI/patched-conic navigation are not yet implemented.

## Roadmap

**Next visual frontier**

Deeper, higher-frequency terrain payloads → close-ground material/detail refinement → production atmosphere/cloud reconstruction → water and coastline systems → GPU-driven local environmental detail → launch and landing surface gameplay.

**Future flight and navigation**

Local/floating reference-frame transitions → SOI/patched-conic policy → spacecraft force/torque dynamics → maneuver planning and navigation → higher-fidelity ephemerides wherever measured accuracy requires them.

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

- [Architecture](docs/architecture.md)
- [Current engineering state](docs/NOVACORE_CURRENT_STATE.md)
- [Planetary rendering](docs/planetary-rendering.md)
- [Production terrain assets](docs/terrain-assets.md)
- [Celestial simulation](docs/celestial-simulation.md)
- [Ephemeris dataset format](docs/ephemeris-dataset-format.md)
- [Ephemeris runtime loader](docs/ephemeris-runtime-loader.md)
- [NAIF source adapter](docs/naif-source-adapter.md)
- [Build on Windows](docs/build-windows.md)

---

**NovaCore is a work in progress.** The repository documents implemented systems and measured limitations explicitly; roadmap items are not presented as completed features.
