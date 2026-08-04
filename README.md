# NovaCore

NovaCore is a precision-first rendering and simulation foundation for large-scale spaceflight games. It is not a complete game engine.

## Philosophy

Authoritative simulation state remains managed C# data in double precision. Rendering is camera-relative, and Graphics never owns simulation state. Immutable frame snapshots provide a deterministic evaluation boundary. The native backend is C++20 and Vulkan; the managed runtime remains CLR/JIT compiled.

Hierarchical celestial reference frames, camera state, and camera control remain managed. Vulkan consumes only resolved GPU transport data.

## Current Architecture

```text
Win32 input
    ↓
NativeInputState
    ↓
DebugCameraInput → bounded CameraCommand span → FreeCameraController
    ↓
CameraState + ReferenceFrameSnapshot / ReferenceFrameResolver
    ↓
CameraRenderSnapshotBuilder
    ↓
GpuCameraData + RenderFrameSubmission
    ↓
NcCameraData → Vulkan storage buffer → vertex shader view/projection
```

`CameraState` is the only authoritative camera state. Native code reports input snapshots and owns rendering transport; it does not control the camera or resolve reference frames. `GpuCameraData` is the only camera record that crosses the render-submission boundary.

## Current Capabilities

- Double-precision managed spatial and frame mathematics.
- Immutable evaluated ECL/ORB/CCE/CCI/CCF frame snapshots.
- GPU high/low camera-relative position transport.
- A frame-aware Free camera with Raw Input look and logarithmic speed adjustment.
- Reusable indexed triangle geometry, stable mesh batching, and indexed instanced Vulkan drawing.
- Resize-safe swapchain lifecycle, validation-enabled Debug runs, and deterministic shutdown.

## Precision Model

`FramePosition` is authoritative contextual position data. It resolves through the managed frame snapshot to root/ECL `UniversePosition` before Graphics. Each root-space component is encoded as high/low FP32 values; the shader reconstructs object-minus-camera translation before applying the FP32 view/projection transform. The camera remains at render-space origin.

See [Precision Model](docs/precision-model.md).

## Reference Frames

The managed hierarchy is:

```text
ECL
├── ORB
└── CCE
    └── CCI
        └── CCF
```

See [Hierarchical Celestial Reference Frames](docs/reference-frames.md).

## Camera Controls

- `W` / `S` — forward along local `-Z` / backward along local `+Z`
- `A` / `D` — strafe left / right
- `Q` / `E` — down / up
- Hold RMB and move the mouse — relative Raw Input look
- Mouse wheel — logarithmically adjust movement speed
- `R` — restore the default pose and movement speed

The implemented controller is Free camera only. Orbit and Follow cameras are planned work.

## Build and Run

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja --config Debug
dotnet build NovaCore.sln -c Debug

dotnet run --project samples/NovaCore.Triangle -c Debug -- --objects=1000
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=frames
dotnet run --project samples/NovaCore.Triangle -c Debug -- --objects=1000 --log=camera
```

See [Build on Windows](docs/build-windows.md).

## Testing

```powershell
dotnet run --project tests/NovaCore.Precision.Tests -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Debug
dotnet run --project tests/NovaCore.Camera.Tests -c Debug
```

Tests cover precision transport, render and ABI layouts, mesh batching, reference-frame mathematics, camera control, input command mapping, and steady-state allocation checks.

## Current Limitations

NovaCore has no deterministic simulation clock, orbital propagation, gravity, celestial-body simulation, spacecraft simulation, Orbit or Follow camera, terrain, materials, ECS, scene graph, gameplay, or asset pipeline.

## Milestone Roadmap

Completed implementation:

- Milestone 1 — Precision foundation
- Milestone 2 — Camera-relative rendering
- Milestone 3 — Reusable meshes and indexed instanced rendering
- Milestone 4 — Hierarchical celestial reference frames
- Milestone 5 — Frame-aware Free camera architecture

Planned work; not present in this repository:

- Milestone 6 — Deterministic simulation clock
- Milestone 7 — Celestial-body state and orbital geometry
- Milestone 8 — Spacecraft dynamics and flight controls

## Repository Layout

- `src/` — managed Core, Graphics, Interop, Platform, and reserved Simulation assemblies.
- `native/` — C++20 Vulkan library and shaders.
- `tests/` — deterministic console test projects.
- `samples/` — managed Vulkan demonstration sample and scenes.
- `docs/` — technical architecture and build documentation.

## Documentation

- [Architecture](docs/architecture.md)
- [Camera](docs/camera.md)
- [Precision Model](docs/precision-model.md)
- [Interop](docs/interop.md)
- [Hierarchical Celestial Reference Frames](docs/reference-frames.md)
- [Build on Windows](docs/build-windows.md)
