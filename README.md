# NovaCore

**A custom precision space-simulation and game-engine framework for seamless travel from planetary surfaces to orbit and beyond.**

![NovaCore Earth against the Milky Way](docs/images/novacore-11a-earth-milky-way.png)

<p align="center">
  <img src="docs/images/novacore-11a-earth-moon.png" alt="NovaCore Earth and Moon presentation" width="49%">
  <img src="docs/images/novacore-11a-earth-sun.png" alt="NovaCore Earth, Moon, and Sun presentation" width="49%">
</p>
<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="NovaCore Mars presentation" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="NovaCore Solar overview at maximum time warp" width="49%">
</p>

## What is NovaCore?

NovaCore is an experimental engine built from the ground up to bring astronomical simulation, planetary terrain, and spacecraft dynamics into one continuous world. It combines a deterministic C# simulation core with a native Vulkan renderer, real planetary data, and high-precision coordinates that reach from the Solar System down to the surface.

The ambition is a physically coherent spaceflight experience: explore a planet, launch, travel through space, and return to land. NovaCore is under active development; the engine foundations are working, while the complete flight experience is still being built.

## What works today?

- **Solar System exploration** — simulated planetary motion, Solar Map and free 3D camera modes, orbit paths, and time warp from 0.1× to 7,776,000×.
- **Planetary rendering** — a native Vulkan renderer with Sun-driven day/night lighting, distinct planets and moons, Saturn's rings, and HDR presentation.
- **Orbit-to-surface Earth traversal** — continuous camera travel from orbit to the near surface, with real NASA/NOAA imagery and elevation data and optional Florida regional detail.
- **Physical terrain foundations** — one canonical Earth surface for physical queries, terrain-aware camera clearance, anchored surface objects, and a Florida launch-site proof scene.
- **Spacecraft motion foundations** — independent translation and rotation, deterministic force and torque transactions, and a bounded attitude-control demonstration.
- **Spacecraft-to-terrain observations** — deterministic measurements of authored spacecraft contact points against physical terrain, including surface normals, radial gaps, and relative velocities.
- **Development tools** — a Windows launcher, repeatable scenarios, and automated simulation and graphics validation.

The latest banked production milestone is **M14.9 — Certify monotone point contact on the existing Florida grading plane**, at [commit `9698a08b`](https://github.com/AlbatronicProductions/NovaCore/commit/9698a08b84b9b40b0fa03bec8f8ba80d1d7404e7), tagged `m14.9-florida-monotone-contact-certification`. M14.1 through M14.9 are banked. A bounded [read-only contact-kinematics candidate](docs/certified-root-contact-kinematics.md) is awaiting Project Control acceptance.

M14.4 supplies atomic linear/angular contact response, and M14.5 supplies an isolated analytical policy for one qualified point at exactly zero represented radial gap. M14.6 adds internal exact event identity/order; public time remains integral. General contact discovery, event-local execution, persistent grounding, and playable launch/landing remain unfinished. M14 — Surface Interaction / Launch Foundation remains open.

## Why it is interesting

- **One world across enormous scales.** Planetary and astronomical views share high-precision coordinates, with camera-relative rendering to preserve local detail.
- **Deterministic simulation.** Exact simulation time and explicit state changes make physical behavior reproducible and testable, including at extreme Solar System time warp.
- **Terrain with physical meaning.** Earth geography and physical height remain independent of the camera and the detail used to draw the surface.
- **A clear separation of responsibilities.** Simulation owns truth; rendering owns presentation. Visual systems can evolve while the underlying physical world stays consistent.

## Where it is going

**Surface → Contact → Launch → Atmosphere → Orbit → Interplanetary travel → Return → Landing**

That is the long-term gameplay vision. Future work includes general contact execution and surface interaction, complete spacecraft flight, atmospheres and clouds, water and weather, richer terrain, and maneuver planning and navigation. The current Solar model supports exploration and simulation development; precision navigation beyond its measured accuracy remains future work.

## Build and explore

NovaCore currently targets a configured **Windows development environment** with .NET, MSVC x64, CMake/Ninja, and Vulkan.

Start with the [Windows build and run guide](docs/build-windows.md) and [terrain asset setup](docs/terrain-assets.md). The repository includes a [Windows development launcher](tools/NovaCore.Launcher) for Solar System, Earth, and Florida scenarios.

## Go deeper

| Read about | Documentation |
|---|---|
| Implemented systems, measured limits, and open work | [Current engineering state](docs/NOVACORE_CURRENT_STATE.md) |
| Engine design and ownership | [Architecture](docs/architecture.md) · [Repository map](docs/repository-structure.md) |
| Earth terrain and rendering | [Planetary rendering](docs/planetary-rendering.md) · [Physical surface queries](docs/surface-point-queries.md) |
| Spacecraft foundations | [Translation](docs/spacecraft-translation.md) · [Contact observations](docs/contact-generation.md) |
| Astronomy and time warp | [Celestial simulation](docs/celestial-simulation.md) |
| Validation and development history | [Engineering evidence](docs/engineering-evidence/README.md) · [M14.6 epoch evidence](docs/engineering-evidence/physical-event-epoch/README.md) |
| Development conventions | [Engineering rules](ENGINEERING_RULES.md) |

Dated engineering reports preserve earlier candidate and blocked states. The current-state document and engineering-evidence index identify later accepted results; historical stage judgments do not override banked production truth.
