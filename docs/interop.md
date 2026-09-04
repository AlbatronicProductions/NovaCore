# Interop

The native boundary is a small C ABI. `nc_run_renderer` receives a blittable `NcFrameSubmission`, result callback, and caller data pointer. The synchronous callback carries UTF-8 diagnostics or current keyboard input. Native code does not retain a managed object reference after the invocation returns.

The generic object portion of `NcFrameSubmission` contains one 96-byte `NcCameraData` record, a contiguous `NcRenderObject` array, and derived `NcDrawBatch` records. Managed code assembles the corresponding `RenderFrameSubmission`; native code validates mesh handles and batch bounds, copies camera and object transport data to its mapped Vulkan storage buffer, and records one indexed instanced draw per batch.

No C++ class, exception, container, template, Vulkan handle, or ownership-bearing pointer crosses the ABI. Native code owns all Vulkan resources and destruction. Reference-frame resolution is managed-only and completes before `UniversePosition` values become render transport.

## Render transport layout

`NcEncodedPosition` is 32 bytes: high and low `vec4` values. Each std430-compatible `NcRenderObject` is 80 bytes, aligned to 16 bytes:

- encoded position at offset 0;
- `xyzw` quaternion and padded `xyz` scale at offset 32;
- `NcMeshHandle` at offset 64.

The corresponding C# interop record, C ABI record, C++ record, and GLSL record are layout-tested. C++ also has compile-time assertions.

`GpuCameraData` / `NcCameraData` is 96 bytes and 16-byte aligned: encoded camera position is at offset 0 and column-major view/projection is at offset 32. The storage buffer places this camera record at byte 0; the first 80-byte render object begins at byte 96.

`NativeInputState` / `NcInputState` is 84 bytes. `DeltaSeconds` is at offset 0, movement/reset/look flags occupy offsets 4–32, mouse deltas are at 36 and 40, and signed `MouseWheelDetents` is at 44. Pause/rate/SAS/modifier fields occupy offsets 48–68, presentation focus is at 72, and viewport width/height are at 76 and 80. Native and managed layout checks cover these fields.

Quaternions use right-handed Hamilton `xyzw` order and rotate local vertices as `q * v * conjugate(q)`. The original generic mesh path retains its reusable colored triangle and static vertex/index buffers. The native renderer also owns distant-body and stellar spheres, planetary/ring resources, and the production NCSM1 spherical-billboard generation resources and pipelines.

`NcFrameSubmission` also carries planetary, lighting, overlay, and generation-specific transport; the generic object layout above is not the complete frame ABI. The accepted P2S5G Earth path submits NCSM1 topology/pupil and physical-preparation data to native-owned current/incoming resources, conservative culling/compaction, bounded VS/TCS/TES refinement, and indexed-indirect raster. Fence-confirmed publication controls generation ownership. Older anchored/global paths and separate validation/proof entry points remain compatibility or regression consumers; they do not replace the accepted New Earth authority. See [planetary-rendering.md](planetary-rendering.md) for the current contract.
