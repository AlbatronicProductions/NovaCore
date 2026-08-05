# NovaCore

NovaCore is a deterministic, high-precision simulation foundation with a native C++20/Vulkan rendering backend. It is intended for large-scale spaceflight games, not a complete game engine. Within its tested .NET x64 contracts, identical authoritative inputs produce identical deterministic results.

## Current Development Status

NovaCore is progressing from deterministic orbital mechanics toward full spacecraft dynamics. It now supports authoritative orbital propagation, spacecraft attitude evaluation, reference-frame extraction, and deterministic visualization. The next milestone focuses on controllable spacecraft dynamics, propulsion, and guidance.

## Overview

Authoritative simulation is managed C#. Native code owns only platform input and Vulkan rendering. Double-precision spatial mathematics, immutable evaluated reference-frame data, and camera-relative GPU transport keep simulation meaning outside the renderer.

## Current Capabilities

- Deterministic simulation clock and transaction-driven authoritative simulation state with immutable evaluated data.
- Deterministic elliptic two-body orbital propagation and scheduled inertial impulses.
- Hierarchical reference-frame graph and authoritative celestial/body-frame extraction.
- Double-precision, camera-relative rendering with a deterministic Vulkan backend.
- Analytical orbit visualization, including a previous-orbit ghost after an impulse.
- Spacecraft attitude and torque-driven rigid-body rotation with quaternion-based body orientation and an authoritative spacecraft body reference frame.
- Sample-local SAS mode selection and exact-time hold-attitude capture; automatic SAS pointing remains a later control-integration step.

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

Hyperbolic and parabolic propagation, patched conics, sphere-of-influence transitions, N-body gravity, automatic SAS pointing, terrain, atmosphere, networking, and save/load remain future work.

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

The default triangle command renders the reusable mesh field. `--scene=frames` retains the frame-marker view. `NovaCore.ReferenceFrameFixture` verifies static graph and transform resolution in the terminal. `--scene=fixture` draws static Star, Planet, Moon, and TestVessel markers; `--scene=fixture-dynamic` publishes complete immutable snapshots for prescribed transform motion. `--scene=celestial` uses the authoritative clock, trajectory evaluator, frame extraction, and existing camera-relative Vulkan path. Its spacecraft marker rotates from simulation time, and its fixture torque changes that spin at an exact authoritative instant. Keys `1`–`7` select sample-local SAS modes (`1` captures hold attitude); `0` disables SAS. These selections do not yet steer the spacecraft automatically. It is not N-body gravity, patched conics, terrain, spacecraft gameplay, lighting, or final planet rendering.

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

Progress:

- Deterministic orbital mechanics: complete.
- Orbit visualization: complete.
- Spacecraft attitude: complete.
- Spacecraft torque control: complete fixture path.

NovaCore remains a focused foundation. It does not yet provide complete orbital gameplay, general physics, an ECS, a scene graph, asset tooling, or a renderer-owned world model.
