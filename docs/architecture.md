# Architecture

NovaCore is a small managed/native foundation, not a complete game engine. Its current architecture keeps authoritative spatial mathematics in managed C# and Vulkan resource ownership in native C++20.

`NovaCore.Core` contains double-precision spatial primitives and managed hierarchical reference-frame mathematics. `NovaCore.Graphics` owns GPU transport concepts: `EncodedPosition`, `RenderObject`, `MeshHandle`, `RenderBatch`, and `RenderFrameSubmission`. `NovaCore.Interop` declares the narrow C ABI. `NovaCore.Platform` currently supplies logging and a minimal host boundary. `NovaCore.Simulation` is reserved for future authoritative simulation.

Reference-frame definitions are immutable identity metadata. A `ReferenceFrameSnapshot` holds one validated evaluated frame graph for an explicit caller-supplied instant, caches local-to-root transforms and kinematics, and resolves `FramePosition` values to root/ECL `UniversePosition` values. Graphics receives only those root-resolved positions and has no reference-frame semantics.

The native library owns the Win32 window, Vulkan instance, selected GPU, device, surface, swapchain, storage buffer, reusable triangle vertex/index buffers, pipeline, synchronization, and deterministic destruction. No Vulkan handle or C++ ownership-bearing object crosses into managed code.

The current sample is single-threaded and uses one frame in flight. Each frame pumps Win32 messages, invokes the managed callback, uploads a contiguous camera/object storage-buffer submission, acquires an image, records indexed instanced draws, submits, and presents. Resize recreates swapchain-dependent resources after in-flight work completes. The sample remains open until its window closes, then reports average frame time and shuts down deterministically.

Current limitations include one window, one built-in triangle mesh, one foreground thread, no asset pipeline, no camera architecture, no simulation clock, and no celestial-body or spacecraft simulation.
