# NovaCore

NovaCore is a deterministic, high-precision simulation framework with a native C++20/Vulkan rendering backend. It is a foundation for large-scale spaceflight games, not a complete game engine. Within its implemented deterministic contracts, identical authoritative inputs are intended to produce identical simulation results.

## Project Overview

Authoritative simulation remains managed C# data. The native layer owns only platform input and Vulkan rendering. Double-precision spatial mathematics, immutable evaluated reference-frame snapshots, and camera-relative GPU transport keep simulation ownership separate from rendering concerns.

## Current Capabilities

- **Precision mathematics** — checked microtick time primitives, double-precision spatial values, and high/low FP32 GPU position encoding.
- **Reference-frame system** — immutable ECL, ORB, CCE, CCI, and CCF frame snapshots resolved in managed code.
- **Graphics abstraction** — reusable mesh handles, indexed instanced drawing, batched render submissions, and Vulkan resource ownership in native code.
- **Camera system** — managed frame-aware Free camera state and control; the renderer receives resolved GPU camera transport only.
- **Deterministic simulation time primitives (6A)** — `SimulationInstant`, `SimulationDuration`, and normalized rational `SimulationRate`.
- **Deterministic event timeline (6B-1)** — canonical pending-event ordering, stable IDs, sequence assignment, cancellation, replacement, and timeline revisions.
- **Exact authoritative simulation clock (6B-2)** — explicit forward advancement, pause state, rate ownership, and deterministic event-boundary detection.

## Current Milestone Status

Milestone 6B-2 — Exact Authoritative Simulation Clock — is complete.

The clock advances through empty spans or stops exactly at the canonical next deterministic event boundary. Events deliberately remain pending at that boundary: execution begins in Milestone 6B-3.

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
```

See [Build on Windows](docs/build-windows.md) for prerequisites and environment setup.

## Documentation

- [Architecture](docs/architecture.md)
- [Precision Model](docs/precision-model.md)
- [Reference Frames](docs/reference-frames.md)
- [Camera](docs/camera.md)
- [Interop](docs/interop.md)
- [Simulation Time](docs/simulation-time.md)
- [Build on Windows](docs/build-windows.md)

## Future Roadmap

Planned work, not current functionality:

- Milestone 6B-3 — deterministic transaction engine and event execution.
- Generated events and processed-event history.
- Deterministic replay foundations.
- Orbital mechanics and analytical propagation.
- Spacecraft simulation.
- Renderer synchronization with evaluated simulation state.
- `SimulationSnapshot` — an immutable capture of complete deterministic simulation state at one `SimulationInstant`, intended to support replay, save/load foundations, rollback, renderer synchronization, debugging, and deterministic verification.

## Current Status

- **Current milestone:** Milestone 6B-2 — Exact Authoritative Simulation Clock
- **Next milestone:** Milestone 6B-3 — Deterministic Transaction Engine

NovaCore remains a focused foundation. It does not yet implement event execution, trajectories, orbital mechanics, gravity, spacecraft dynamics, networking, ECS, terrain, assets, materials, or gameplay systems.
