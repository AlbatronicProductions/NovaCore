# Architecture

`NovaCore.Core` contains only small managed precision types. `NovaCore.Simulation` is reserved for authoritative JIT-compiled simulation. `NovaCore.Graphics` converts resolved positions into native submission data. `NovaCore.Interop` owns the C ABI declarations. `NovaCore.Platform` is a placeholder for the future managed host boundary. `NovaCore.Triangle` is the sole sample.

The C++20 library owns the Win32 window, Vulkan instance, selected GPU, device, surface, swapchain, pipeline, synchronization, and their destruction. The native entry point owns all of those handles for one invocation; no handle is exposed to managed code in Milestone 1.

The application has one foreground thread and no background workers. A frame pumps Win32 messages, acquires an image, records the triangle draw, submits it, and presents. Resize recreates swapchain-dependent resources. Shutdown waits for the device, destroys resources in reverse dependency order, then destroys the instance and window. The five-second sample duration is intentionally nondeterministic with respect to exact frame count; simulation itself is not yet implemented.

Current limitations: one window, one foreground thread, one frame in flight, shader files compiled at native build time, no asset system, and no mod loader.
