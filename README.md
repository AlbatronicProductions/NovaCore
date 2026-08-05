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
- **Elliptic two-body propagation, orbit visualization, and frame extraction** — pure exact-time universal-variable f/g propagation evaluates authoritative Cartesian trajectories at `SimulationClock.CurrentTime`. The celestial fixture derives one fixed-count elliptic orbit curve from the active trajectory; it is not simulation state or a historical trail. `CelestialReferenceFrameEvaluator` maps the result into immutable local-to-parent frame candidates without mutating state, time, topology, or rendering. Celestial values use SI units: metres, metres per second, seconds, and m³/s² for μ.

## Architecture

```text
SimulationClock.CurrentTime
    ↓
Pure exact-time two-body propagation
    ↓
Immutable evaluated reference-frame transforms
    ↓
ResolvedRenderSnapshot
    ↓
Graphics transport
    ↓
Native Vulkan renderer
```

Graphics never traverses frame graphs, evaluates transforms, owns simulation time, or mutates simulation state.

## Current Limitations

`--scene=celestial` is a compact analytical visual fixture: cyan denotes the current authoritative orbit and, after its scheduled impulse, a dim curve denotes the immediately previous orbit. A small marker identifies the exact impulse location. These curves and marker are derived visualization only—not mutable trails, prediction state, saved history, or renderer-owned orbit models. Triangle markers and their presentation scale are debug geometry only, not physical body rendering.

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
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=celestial
```

The default triangle command renders the reusable mesh field. `--scene=frames` retains the frame-marker view. `NovaCore.ReferenceFrameFixture` verifies static graph and transform resolution in the terminal. `--scene=fixture` draws static Star, Planet, Moon, and TestVessel markers; `--scene=fixture-dynamic` publishes complete immutable snapshots for prescribed transform motion. `--scene=celestial` uses the authoritative clock, trajectory evaluator, frame extraction, and existing camera-relative Vulkan path. It is not N-body gravity, patched conics, terrain, spacecraft gameplay, lighting, or final planet rendering.

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

The completed foundation includes deterministic simulation orchestration, the reference-frame system, immutable graphics transport, dynamic snapshot publication, authoritative celestial contracts, pure elliptic two-body propagation, exact-time celestial impulses, and visible celestial-to-frame extraction.

Next planned integration is deliberately narrow:

- Multi-body and non-inertial celestial frame evaluation.
- Hyperbolic/parabolic propagation and future regime transitions.
- Physical body and spacecraft presentation without changing simulation authority.

NovaCore remains a focused foundation. It does not yet provide complete orbital gameplay, general physics, an ECS, a scene graph, asset tooling, or a renderer-owned world model.
