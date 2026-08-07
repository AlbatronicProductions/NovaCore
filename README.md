# NovaCore

NovaCore is a deterministic space simulation engine featuring a C# managed core, a native Vulkan renderer, and a high-precision camera-relative architecture for seamless planetary and Solar System visualization.

NovaCore is designed around one core principle: simulation owns truth, rendering owns presentation.

Celestial mechanics, reference frames, and time remain authoritative in the simulation. The renderer consumes immutable presentation snapshots, allowing GPU rendering, visualization, and future graphical improvements without compromising deterministic simulation.

## Current capabilities

- ✓ Deterministic celestial simulation with immutable definitions and exact simulation time.
- ✓ Immutable runtime architecture: evaluation publishes derived presentation snapshots; graphics consumes them.
- ✓ NCPE v2 artifact format, byte-only runtime reconstruction, and deterministic hash verification.
- ✓ Offline NAIF/CSPICE adapter foundation; runtime assemblies do not depend on CSPICE, kernels, network access, or the builder.
- ✓ GPU-driven adaptive cube-sphere planetary rendering with CPU-reference and CPU/GPU parity validation.
- ✓ Crack-safe mixed-level planetary LOD through deterministic neighbor balancing and edge-stitch metadata.
- ✓ Distant/detail planetary handoff using shared physical radius and camera-relative center authority.
- ✓ Evaluated `SolAnalytical` Solar presentation for Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune.
- ✓ Deterministic orbit paths, body labels, screen-space markers, focus navigation, moving bodies, and `SimulationClock` integration.
- ✓ Double-precision camera-relative subtraction before GPU float transport, plus Vulkan validation-layer verification.

NovaCore does **not** yet provide terrain, textures, cubemap materials, atmosphere, oceans, clouds, spacecraft gameplay, colonies, or a completed production DE440 Solar runtime artifact.

## Architecture

```text
Offline astronomy (NAIF/CSPICE)
            ↓
          NCPE
            ↓
CelestialSystemDefinition
            ↓
CelestialSystemEvaluator
            ↓
Immutable presentation snapshots
            ↓
Planet renderer
            ↓
Native Vulkan renderer
```

Simulation owns celestial truth. Graphics owns presentation resources and never changes body positions, hierarchy, ephemerides, simulation time, or physical radii.

## Current screenshots

The current Solar System overview uses true evaluated distances and physical body radii. Labels and markers are presentation aids; they do not relocate or inflate bodies.

![NovaCore Solar System overview](docs/images/solar-system-overview.png)

## Current Milestone

10A-9B

✓ True-distance Solar System visualization
✓ GPU adaptive planetary rendering
✓ Interactive camera and body focus
✓ Deterministic orbit visualization

## Roadmap

Current development:

- Stellar rendering
- Planet materials
- Atmosphere
- Terrain rendering

Future:

- Spacecraft
- Physics and gameplay
- Colonies

## Building and testing

Windows development uses .NET, the configured MSVC x64 environment, Ninja/CMake, and Vulkan.

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja --config Debug

dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
```

The focused Solar sample is `samples/NovaCore.Triangle` with `--scene=sol`. See the engineering documents below for scoped build/test commands and architecture constraints.

## Documentation

- [Architecture](docs/architecture.md)
- [Current engineering state](docs/NOVACORE_CURRENT_STATE.md)
- [Planetary rendering](docs/planetary-rendering.md)
- [Celestial simulation](docs/celestial-simulation.md)
- [Ephemeris dataset format](docs/ephemeris-dataset-format.md)
- [Ephemeris runtime loader](docs/ephemeris-runtime-loader.md)
- [NAIF source adapter](docs/naif-source-adapter.md)
- [Build on Windows](docs/build-windows.md)
