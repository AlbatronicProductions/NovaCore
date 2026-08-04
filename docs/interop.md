# Interop

The ABI consists of `nc_run_renderer`, a blittable `NcFrameSubmission` containing one encoded camera position and a pointer/count of `NcRenderObject` submissions, an explicit result code, and one C-style host callback. The callback carries either UTF-8 diagnostics or per-frame keyboard movement intent; managed code updates the authoritative double camera and overwrites only the transient encoded camera data.

No C++ class, exception, container, template, or ownership-bearing object crosses this boundary. Native code never stores a managed object reference; it calls the supplied function pointer only during the synchronous native invocation. The callback proves native-to-managed flow and reports GPU and lifecycle diagnostics. The native side copies the supplied contiguous batch into its own mapped Vulkan storage buffer each frame. Future rendering APIs should extend this batch rather than add per-object calls.
