# NovaCore

NovaCore is a deterministic space simulation engine featuring a C# managed core, a native Vulkan renderer, and a high-precision camera-relative architecture for seamless planetary and Solar System visualization.

NovaCore is designed around one core principle: **simulation owns truth; rendering owns presentation.**

Celestial mechanics, reference frames, and time remain authoritative in the simulation. The renderer consumes immutable presentation snapshots, allowing GPU rendering, visualization, and future graphical improvements without compromising deterministic simulation.

## Current capabilities

- ✓ Deterministic celestial simulation with immutable definitions and exact simulation time.
- ✓ Immutable runtime architecture: evaluation publishes derived presentation snapshots; graphics consumes them.
- ✓ NCPE v2 artifact format, byte-only runtime reconstruction, and deterministic hash verification.
- ✓ DE440-validated compact analytical Solar runtime (`SolCompact-DE440Validated-v3`) with deterministic lunar secular and bounded periodic corrections; DE440/CSPICE remains an offline validation oracle rather than a runtime dependency.
- ✓ CSPICE/kernel-free, zero-allocation runtime celestial evaluation, measured below 25 µs for all ten Solar bodies on the current development machine.
- ✓ Deterministic arbitrary-time evaluation and 50,000× time-warp validation.
- ✓ GPU-driven adaptive cube-sphere planetary rendering with CPU-reference and CPU/GPU parity validation.
- ✓ Crack-safe mixed-level planetary LOD through deterministic neighbor balancing and edge-stitch metadata.
- ✓ Distant/detail planetary handoff using shared physical radius and camera-relative center authority.
- ✓ Evaluated `SolAnalytical` presentation for Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune.
- ✓ Orbit paths and body motion use the same `CelestialSystemEvaluator` authority, with deterministic labels, markers, focus navigation, and `SimulationClock` integration.
- ✓ Double-precision camera-relative subtraction before GPU float transport, plus Vulkan validation-layer verification.

`SolCompact-DE440Validated-v3` is currently sufficient for Solar presentation, time warp, and approximate translunar gameplay. It is not DE440 playback or a precision lunar-navigation ephemeris, and it is not intended as the precision truth for lunar orbit insertion, close lunar navigation, or precision long-horizon targeting.

NovaCore does **not** yet provide terrain, textures, cubemap materials, atmosphere, oceans, clouds, spacecraft gameplay, colonies, or a higher-fidelity lunar ephemeris for measured precision-navigation requirements.

## Architecture

```text
Offline
JPL/NAIF DE440
            ↓
CSPICE validation oracle
            ↓
Compact, versioned Solar definitions

Runtime
SimulationClock
            ↓
CelestialSystemEvaluator
            ↓
Parent-relative / reference-frame resolution
            ↓
Immutable presentation snapshots
            ↓
Vulkan renderer

Exceptional future fidelity path
Measured requirement → SampledHermite / Chebyshev
```

High-authority astronomy validates compact, deterministic runtime models offline; the runtime never loads CSPICE or DE440 kernels. Simulation owns celestial truth. Graphics owns presentation resources and never changes body positions, hierarchy, ephemerides, simulation time, or physical radii. SampledHermite and Chebyshev remain exceptional future fidelity layers, used only where measured requirements justify them rather than as the normal Solar runtime.

## Current screenshots

The current Solar System overview uses true evaluated distances and physical body radii. Labels and markers are presentation aids; they do not relocate or inflate bodies.

![NovaCore Solar System overview](docs/images/solar-system-overview.png)

## Current Milestone

9A-4F-2 / 10A-9B

✓ True-distance Solar System visualization
✓ DE440-validated compact analytical Solar runtime
✓ GPU adaptive planetary rendering
✓ Interactive camera and body focus
✓ Deterministic orbit visualization

## Roadmap

Near-term visual work:

- Stellar/Sun presentation
- Planet materials
- Atmosphere
- Terrain rendering

Future spacecraft and navigation foundation:

- Local/floating reference-frame transitions where required
- SOI / patched-conic transition policy
- Spacecraft force/torque dynamics
- Higher-fidelity lunar ephemeris if measured gameplay accuracy requires it

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
