# NAIF source adapter

`NovaCore.NaifEphemerisAdapter` is an offline-only contract boundary. It will eventually use a pinned CSPICE implementation to query geometric J2000 barycentric states relative to the Solar System barycenter, with correction mode `NONE`. It converts km and km/s to SI once, then derives parent-relative states by deterministic child-minus-parent subtraction.

It is not referenced by runtime simulation, graphics, samples, or native code. It does not download, discover, or process kernels in this milestone.

The planned first source bundle is NAIF `de440.bsp`, `gm_de440.tpc`, `pck00010.tpc`, and `naif0012.tls`, plus a pinned CSPICE binary, target map, and conversion policy. Local paths are invocation-only and never part of source identity.

The repository currently contains an `UnpinnedTemplate` manifest, not official file pins. Production verification rejects a template manifest. A manifest becomes `FullyPinned` only when every required source has a verified nonzero SHA-256, positive byte length, release identity, and semantic role. Source files are supplied out of band; real CSPICE extraction begins only after that bundle is locally verified.
