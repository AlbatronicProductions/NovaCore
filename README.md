# NovaCore

NovaCore is a precision-first rendering and simulation foundation for future large-scale spaceflight games. It is not a complete game engine.

## Philosophy

Authoritative spatial state is managed C# data using double precision. Rendering is camera-relative: the renderer receives derived positions and never owns simulation state. Managed immutable snapshots provide a consistent frame of reference for resolution and rendering boundaries.

The current renderer is a C++20 Vulkan backend called from a JIT-compiled .NET host through a narrow C ABI. Hierarchical celestial reference-frame mathematics remains managed and uses double precision throughout.

## Current Architecture

```text
Managed sample state
    ↓
Reference Frames
    ↓
ReferenceFrameSnapshot
    ↓
Root/ECL UniversePosition
    ↓
RenderFrameSubmission
    ↓
EncodedPosition GPU transport
    ↓
Vulkan renderer
```

The repository currently contains a single-threaded sample host, managed reference-frame resolution, reusable native triangle geometry, batched render submissions, and an indexed-instanced Vulkan draw path.

## Precision Model

`UniversePosition` is the root/ECL-resolved compatibility representation used at the rendering boundary. Contextual positions use `FramePosition` and resolve through a managed `ReferenceFrameSnapshot` before entering Graphics.

The renderer encodes each root-space double component as high/low FP32 values. The vertex shader reconstructs object-minus-camera translation from those pairs, so normal GPU FP32 rendering operates on small camera-relative values. Graphics transport data is not authoritative simulation state.

See [Precision Model](docs/precision-model.md).

## Reference Frames

The current managed frame system supports a validated immutable hierarchy:

```text
ECL
├── ORB
└── CCE
    └── CCI
        └── CCF
```

ECL is the root star-centered ecliptic inertial frame. ORB, CCE, CCI, and CCF are evaluated frame types with explicit snapshot-time transforms and kinematics. The system resolves positions, directions, orientations, and velocities, including rotating-frame transport.

See [Hierarchical Celestial Reference Frames](docs/reference-frames.md).

## Rendering

The native Vulkan renderer owns the Win32 window, Vulkan resources, reusable vertex/index buffers, mesh lookup, draw recording, and destruction. The managed side assembles immutable-style `RenderFrameSubmission` snapshots containing contiguous `RenderObject` records.

`MeshHandle` is a fixed-width transport identifier. The current native mesh table contains one built-in colored triangle. Objects sharing a mesh are assembled into stable batches and rendered with indexed instancing: one draw call per mesh batch.

## Testing

Automated console tests cover:

- high/low precision encoding and camera-relative reconstruction;
- Graphics transport conversion, batch assembly, capacity checks, and ABI/layout validation;
- reference-frame transform algebra, hierarchy validation, frame conversion, ORB/CCI/CCF construction, rotating-frame velocity transport, and allocation checks.

The reference-frame benchmark resolves 10,000 positions after snapshot construction and verifies steady-state managed allocations are zero. Vulkan ABI structure layout is checked between managed interop and the loaded native library.

## Milestone Roadmap

Completed implementation:

- ✓ Milestone 1 — Precision foundation
- ✓ Milestone 2 — Camera-relative rendering
- ✓ Milestone 3 — Reusable meshes and indexed instanced rendering
- ✓ Milestone 4 — Hierarchical celestial reference frames

Planned work; not present in the current repository:

- □ Milestone 5 — Frame-aware camera architecture
- □ Milestone 6 — Deterministic simulation clock
- □ Milestone 7 — Celestial-body state and orbital geometry
- □ Milestone 8 — Spacecraft dynamics and flight controls

## Repository Layout

- `src/` — managed Core, Graphics, Interop, Platform, and reserved Simulation assemblies.
- `native/` — C++20 Vulkan library and shaders.
- `tests/` — deterministic precision, graphics, and reference-frame tests.
- `samples/` — the managed Vulkan triangle sample and demonstration scenes.
- `docs/` — architecture, precision, interop, reference-frame, and Windows build documentation.

## Documentation

- [Architecture](docs/architecture.md)
- [Precision Model](docs/precision-model.md)
- [Interop](docs/interop.md)
- [Hierarchical Celestial Reference Frames](docs/reference-frames.md)
- [Build on Windows](docs/build-windows.md)

## Current Status

NovaCore currently provides a managed double-precision spatial foundation, immutable evaluated celestial reference-frame snapshots, high/low camera-relative GPU transport, and a minimal Vulkan indexed-instancing sample. It remains a foundation rather than a complete engine: it has no gameplay, orbital propagation, simulation clock, camera architecture, asset pipeline, or general resource system.
