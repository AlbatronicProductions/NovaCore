# NAIF source adapter

`NovaCore.NaifEphemerisAdapter` is an offline-only contract boundary. It will eventually use a pinned CSPICE implementation to query geometric J2000 barycentric states relative to the Solar System barycenter, with correction mode `NONE`. It converts km and km/s to SI once, then derives parent-relative states by deterministic child-minus-parent subtraction.

It is not referenced by runtime simulation, graphics, samples, or native code. It does not download, discover, or process kernels in this milestone.

The planned first source bundle is NAIF `de440.bsp`, `gm_de440.tpc`, `pck00010.tpc`, and `naif0012.tls`, plus a pinned CSPICE binary, target map, and conversion policy. Local paths are invocation-only and never part of source identity.

The current local development bundle is pinned to official DE440 inputs and CSPICE N0067. Local paths remain invocation-only and never contribute to dataset identity.

## Current implementation status

Implemented and proven locally: the official CSPICE N0067 static library links with MSVC 19.51; a narrow native C ABI shim is explicitly loaded by the offline adapter; `CspiceSession` owns kernel readiness, clearing, and disposal; CSPICE uses `RETURN` error action with `NULL` output; and a deliberately invalid query captures both SHORT and LONG diagnostics, resets CSPICE, then allows a Sun/SSB ET=0 query in the same session. Runtime projects remain independent of the adapter, shim, CSPICE, and local kernels.

Deferred hardening: broaden the lifecycle test matrix, add more exhaustive controlled-failure coverage, expand lifecycle documentation, and rerun the full repository regression suite after final lifecycle changes. Source files and generated native outputs remain under ignored `external/naif/` paths.
