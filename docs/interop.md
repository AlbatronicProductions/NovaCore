# Interop

The ABI consists of `nc_run_triangle`, a blittable `NcRelativePosition` of three `double` values, an explicit result code, and an optional C-style UTF-8 diagnostic callback. The managed declaration uses `LibraryImport` and a Cdecl callback delegate.

No C++ class, exception, container, template, or ownership-bearing object crosses this boundary. Native code never stores a managed object reference; it calls the supplied function pointer only during the synchronous native invocation. The callback proves native-to-managed flow and reports GPU and lifecycle diagnostics. Future rendering APIs should prefer batched submission structures over many per-object calls.
