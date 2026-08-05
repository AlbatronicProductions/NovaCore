# NovaCore

NovaCore is a deterministic, high-precision simulation foundation with a native C++20/Vulkan rendering backend. It is intended for large-scale spaceflight games, not a complete game engine. Within its tested .NET x64 contracts, identical authoritative inputs produce identical deterministic results.

## Overview

Authoritative simulation is managed C#. Native code owns only platform input and Vulkan rendering. Double-precision spatial mathematics, immutable evaluated reference-frame data, and camera-relative GPU transport keep simulation meaning outside the renderer.

## Current Capabilities

- **Simulation foundation** — exact deterministic time primitives, canonical scheduled-event ordering, transaction-controlled authoritative mutation, same-time group execution, host-duration conversion and advancement, and immutable processed history with revision semantics.
- **Reference frames** — immutable frame topology, allocation-free path queries, deterministic transform composition, and root-resolved position, orientation, direction, and rotating-frame velocity. Static and prescribed-dynamic fixtures exercise this path.
- **Graphics foundation** — a flat immutable `ResolvedRenderSnapshot` is the only render-data boundary. It feeds camera-relative high/low FP32 transport, reusable meshes, indexed instancing, warmed allocation-free frame assembly, and the native Vulkan renderer. There is no `RenderWorld`, renderer-owned scene graph, or renderer-owned spatial hierarchy.
- **Camera system** — managed, frame-aware Free-camera state and control. Graphics receives resolved `GpuCameraData` only.
- **Celestial contracts** — authoritative celestial identities, central-body definitions with gravitational parameter μ, canonical Cartesian position and velocity at an exact epoch, and immutable two-body trajectory records.
- **Elliptic two-body propagation** — pure exact-time universal-variable f/g propagation for circular through tested high-eccentricity elliptic trajectories, including backward-time evaluation. Celestial values use SI units: metres, metres per second, seconds, and m³/s² for μ. Warmed propagation is allocation-free on the tested runtime and rejects unsupported or invalid inputs through controlled results.

## Architecture

```text
Authoritative celestial state
    ↓
Pure exact-time two-body propagation
    ↓
Future evaluated reference-frame transforms
    ↓
ResolvedRenderSnapshot
    ↓
Graphics transport
    ↓
Native Vulkan renderer
```

The propagation-to-reference-frame connection is planned, not implemented. Graphics never traverses frame graphs, evaluates transforms, owns simulation time, or mutates simulation state.

## Current Limitations

The current propagation contracts are not yet connected to transaction-based trajectory replacement, reference-frame evaluation, or visible orbital rendering. The dynamic visual fixture uses prescribed deterministic transform motion, not real orbital propagation.

Hyperbolic and parabolic propagation, patched conics, sphere-of-influence transitions, N-body gravity, maneuvers, spacecraft gameplay, terrain, atmosphere, networking, and save/load remain future work.

## Building and Testing

Windows development uses .NET and the configured native Vulkan build.

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja --config Debug

dotnet build NovaCore.sln -c Debug

dotnet run --project tests/NovaCore.Simulation.Tests -c Debug
dotnet run --project tests/NovaCore.Precision.Tests -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Debug
dotnet run --project tests/NovaCore.Camera.Tests -c Debug
```

`NovaCore.Simulation.Tests` is the deterministic terminal verification suite for simulation-time, event, transaction, celestial-contract, and two-body propagation behavior. It is not a visual orbital scene.

## Run the Samples

```powershell
dotnet run --project samples/NovaCore.Triangle -c Debug -- --objects=1000
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=frames
dotnet run --project samples/NovaCore.ReferenceFrameFixture -c Debug
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture-dynamic
```

The default triangle command renders the reusable mesh field. `--scene=frames` retains the frame-marker view. `NovaCore.ReferenceFrameFixture` verifies static graph and transform resolution in the terminal. `--scene=fixture` draws static Star, Planet, Moon, and TestVessel markers; `--scene=fixture-dynamic` publishes complete immutable snapshots for prescribed transform motion. Neither fixture mode is gravity or orbital physics.

## Documentation

- [Architecture](docs/architecture.md)
- [Celestial Simulation](docs/celestial-simulation.md)
- [Simulation Time](docs/simulation-time.md)
- [Reference Frames](docs/reference-frames.md)
- [Precision Model](docs/precision-model.md)
- [Camera](docs/camera.md)
- [Interop](docs/interop.md)
- [Build on Windows](docs/build-windows.md)

## Status and Roadmap

The completed foundation includes deterministic simulation orchestration, the reference-frame system, immutable graphics transport, dynamic snapshot publication, authoritative celestial contracts, and pure elliptic two-body propagation.

Next planned integration is deliberately narrow:

- Transaction-based authoritative trajectory replacement.
- Propagated celestial state feeding evaluated reference frames.
- Visible analytical orbital propagation through the existing Vulkan pipeline.

NovaCore remains a focused foundation. It does not yet provide complete orbital gameplay, general physics, an ECS, a scene graph, asset tooling, or a renderer-owned world model.
