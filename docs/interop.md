# Interop

The ABI consists of `nc_run_renderer`, a blittable `NcFrameSubmission` containing one encoded camera position and a pointer/count of `NcRenderObject` submissions, an explicit result code, and one C-style host callback. The callback carries either UTF-8 diagnostics or per-frame keyboard movement intent; managed code updates the authoritative double camera and overwrites only the transient encoded camera data.

No C++ class, exception, container, template, or ownership-bearing object crosses this boundary. Native code never stores a managed object reference; it calls the supplied function pointer only during the synchronous native invocation. The callback proves native-to-managed flow and reports GPU and lifecycle diagnostics. The native side copies the supplied contiguous batch into its own mapped Vulkan storage buffer each frame. Future rendering APIs should extend this batch rather than add per-object calls.
# Milestone 3 render transport

The renderer receives a frame snapshot only. `NcEncodedPosition` is 32 bytes (two `vec4` values). Each std430-compatible `NcRenderObject` is 80 bytes, aligned to 16 bytes: encoded position at offset 0, `xyzw` quaternion plus padded `xyz` scale at offset 32, and `NcMeshHandle` at offset 64. The C# interop record, C ABI record, C++ record, and GLSL record are layout-tested; C++ also has compile-time assertions.

Quaternions are right-handed Hamilton `xyzw` values and rotate local vertices as `q * v * conjugate(q)`. Native code owns reusable vertex/index buffers and batches are derived from the managed frame snapshot, never from simulation state.
