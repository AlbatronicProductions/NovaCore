# Interop

The native boundary is a small C ABI. `nc_run_renderer` receives a blittable `NcFrameSubmission`, result callback, and caller data pointer. The synchronous callback carries UTF-8 diagnostics or current keyboard input. Native code does not retain a managed object reference after the invocation returns.

`NcFrameSubmission` contains one 96-byte `NcCameraData` record, a contiguous `NcRenderObject` array, and derived `NcDrawBatch` records. Managed code assembles the corresponding `RenderFrameSubmission`; native code validates mesh handles and batch bounds, copies camera and object transport data to its mapped Vulkan storage buffer, and records one indexed instanced draw per batch.

No C++ class, exception, container, template, Vulkan handle, or ownership-bearing pointer crosses the ABI. Native code owns all Vulkan resources and destruction. Reference-frame resolution is managed-only and completes before `UniversePosition` values become render transport.

## Render transport layout

`NcEncodedPosition` is 32 bytes: high and low `vec4` values. Each std430-compatible `NcRenderObject` is 80 bytes, aligned to 16 bytes:

- encoded position at offset 0;
- `xyzw` quaternion and padded `xyz` scale at offset 32;
- `NcMeshHandle` at offset 64.

The corresponding C# interop record, C ABI record, C++ record, and GLSL record are layout-tested. C++ also has compile-time assertions.

`GpuCameraData` / `NcCameraData` is 96 bytes and 16-byte aligned: encoded camera position is at offset 0 and column-major view/projection is at offset 32. The storage buffer places this camera record at byte 0; the first 80-byte render object begins at byte 96.

`NativeInputState` / `NcInputState` is 48 bytes. `DeltaSeconds` is at offset 0, movement/reset/look flags occupy offsets 4–32, mouse deltas are at 36 and 40, and signed `MouseWheelDetents` is at 44. Native and managed layout checks cover these fields.

Quaternions use right-handed Hamilton `xyzw` order and rotate local vertices as `q * v * conjugate(q)`. The current native mesh table contains one reusable colored triangle with host-visible static vertex and index buffers.
