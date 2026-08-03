# NovaCore

NovaCore is a lightweight, precision-first rendering and simulation foundation for future games. It is intentionally not a complete game engine.

Milestone 1 proves a narrow managed/native seam: a JIT-hosted .NET sample resolves a double-precision universe position to a camera-relative position, then invokes a C++20 Vulkan runtime which opens a Win32 window and presents a colored triangle.

See [architecture](docs/architecture.md), [Windows build instructions](docs/build-windows.md), [interop](docs/interop.md), and the [precision model](docs/precision-model.md).
