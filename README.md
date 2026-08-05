# NovaCore

NovaCore is a deterministic, high-precision simulation framework with a native C++20/Vulkan rendering backend. It is a foundation for large-scale spaceflight games, not a complete game engine. Within its implemented deterministic contracts, identical authoritative inputs are intended to produce identical simulation results.

## Project Overview

Authoritative simulation remains managed C# data. The native layer owns only platform input and Vulkan rendering. Double-precision spatial mathematics, immutable evaluated reference-frame snapshots, and camera-relative GPU transport keep simulation ownership separate from rendering concerns.

## Current Capabilities

- **Precision mathematics** — checked microtick time primitives, double-precision spatial values, and high/low FP32 GPU position encoding.
- **Reference-frame system** — immutable structural graphs and evaluated transforms resolved in managed code, including the terminal Star–Planet–Moon–Vessel fixture.
- **Resolved render transport** — immutable root-resolved render snapshots feed camera-relative GPU transport without giving Graphics frame-graph ownership.
- **Graphics abstraction** — reusable mesh handles, indexed instanced drawing, batched render submissions, and Vulkan resource ownership in native code.
- **Camera system** — managed frame-aware Free camera state and control; the renderer receives resolved GPU camera transport only.
- **Deterministic simulation time primitives (6A)** — `SimulationInstant`, `SimulationDuration`, and normalized rational `SimulationRate`.
- **Deterministic event timeline (6B-1)** — canonical pending-event ordering, stable IDs, sequence assignment, cancellation, replacement, and timeline revisions.
- **Deterministic simulation foundation** — exact simulation time, canonical event topology, clock orchestration, and authoritative transaction contracts.
- **Vulkan visual fixture modes** — static and prescribed-dynamic reference-frame fixtures render through the existing managed/native triangle path.

## Current Milestone Status

Milestone 6E-2 — Static Reference-Frame Fixture Visual Integration — is complete.

The visual fixture is a static transform and render-transport demonstration. It does not imply orbital propagation, gravity, gameplay, `SimulationSnapshot`, or physically realistic bodies.

## Design Principles

- **Determinism** — canonical event ordering and exact authoritative timestamps.
- **Replayability** — stable IDs, revisions, and deterministic evaluation boundaries support future replay validation.
- **Numerical stability** — simulation uses doubles; rendering resolves positions camera-relatively as late as possible.
- **Allocation-free steady-state execution** — tested arithmetic, timeline, and clock paths avoid managed allocation after adequate preallocation.
- **Immutable simulation data** — immutable evaluated frame snapshots separate consumers from mutable authoritative topology.
- **Explicit ownership** — simulation is managed; Graphics transports resolved data; native code owns Vulkan and platform details.
- **Long-term maintainability** — small focused abstractions, fixed-width contracts, and deferred features until their ownership model is justified.

## Architecture

```text
Authoritative simulation
    ↓
Reference-frame snapshot and resolution
    ↓
Resolved render snapshot
    ↓
GPU high/low position transport
    ↓
Native Vulkan renderer
```

The event timeline and simulation clock remain managed and independent of Graphics. Vulkan, shaders, mesh batching, and native code do not interpret reference-frame or simulation-time semantics.

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

For the current rendering sample:

```powershell
dotnet run --project samples/NovaCore.Triangle -c Debug -- --objects=1000
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=frames
dotnet run --project samples/NovaCore.ReferenceFrameFixture -c Debug
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture-dynamic
```

`NovaCore.ReferenceFrameFixture` verifies static graph and transform resolution in the terminal. The default triangle command renders the reusable mesh field; `--scene=frames` retains the existing frame-marker view; `--scene=fixture` renders four static Star, Planet, Moon, and TestVessel markers; `--scene=fixture-dynamic` republishes complete immutable snapshots for prescribed Moon and TestVessel transform motion. The dynamic mode is not gravity or orbital physics.

See [Build on Windows](docs/build-windows.md) for prerequisites and environment setup.

## Documentation

- [Architecture](docs/architecture.md)
- [Precision Model](docs/precision-model.md)
- [Reference Frames](docs/reference-frames.md)
- [Camera](docs/camera.md)
- [Interop](docs/interop.md)
- [Simulation Time](docs/simulation-time.md)
- [Build on Windows](docs/build-windows.md)

## Run the Samples

```powershell
dotnet run --project samples/NovaCore.ReferenceFrameFixture -c Debug
dotnet run --project samples/NovaCore.Triangle -c Debug
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=frames
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=fixture-dynamic
```

The fixture modes follow: reference-frame resolution → `ResolvedRenderSnapshot` → camera-relative GPU transport → Vulkan rendering. The dynamic fixture uses prescribed deterministic transform motion and complete immutable snapshot replacement; it is not orbital propagation, gravity, gameplay, `SimulationSnapshot`, or physically realistic bodies.

## Future Roadmap

Planned work, not current functionality:

- Analytical trajectory and orbital-mechanics propagation.
- Celestial-body and spacecraft simulation.
- Generated simulation events and expanded processed-event history.
- Immutable `SimulationSnapshot` publication for renderer synchronization and external consumers.
- Save/load, replay restoration, and networking built on deterministic revisions and event history.

## Current Status

- **Current milestone:** Milestone 6E-2 — Static Reference-Frame Fixture Visual Integration
- **Next milestone:** Deferred pending review

NovaCore remains a focused foundation. It does not yet implement trajectories, orbital mechanics, gravity, spacecraft dynamics, networking, ECS, terrain, assets, materials, or gameplay systems.
