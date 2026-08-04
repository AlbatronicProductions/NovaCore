# NovaCore

NovaCore is a lightweight, precision-first rendering and simulation foundation for future games. It is intentionally not a complete game engine.

Milestone 2 proves GPU-resolved camera-relative precision. A JIT-hosted .NET sample keeps universe positions authoritative as doubles, encodes object and camera positions only at the rendering boundary, and uses a native C++20 Vulkan storage buffer to reconstruct the relative position in the vertex shader.

See [architecture](docs/architecture.md), [Windows build instructions](docs/build-windows.md), [interop](docs/interop.md), and the [precision model](docs/precision-model.md).
