# Ephemeris dataset format

NovaCore Ephemeris Artifact v2 (`NCPE`) is a deterministic, offline-only, little-endian binary interchange format. It is self-describing enough for a future runtime reader to reconstruct existing immutable celestial-system contracts without companion catalogs or inferred values. It is not yet a runtime loader or astronomical source adapter.

The fixed header contains magic, format version, endianness marker, total byte length, and section count. Each section is `(uint32 type, uint32 byteLength, bytes)`. Required v2 sections are system/time mapping, sources, bodies and physical properties, hierarchy bindings, sampled payloads, flat samples, and hashes. Identifiers are fixed-width unsigned integers; positions and velocities are IEEE-754 double bit patterns. Readers bounds-check all lengths and sections.

Bodies are ordered by body ID; samples are ordered by ascending domain tick within each body. The artifact stores one root, parent/source IDs, interpolation model, coverage, and measured position/velocity error bounds. It rejects invalid hierarchy, duplicate bodies, non-monotonic or non-finite samples, invalid headers, and hash or length mismatches.

Hash layers cover metadata/source identity, conversion policy, catalog, hierarchy, payload, a neutral definition hash matching the runtime-definition semantic contract, and final serialized artifact. Artifacts contain no paths, host timestamps, CLR type names, locale-sensitive text, or machine state.

NCPE v1 remains readable only as a legacy format; it is explicitly reported as insufficient for runtime reconstruction. The builder writes v2 only. The future runtime reader will defensively reconstruct immutable runtime arrays, recompute the stored definition hash, and publish only after complete validation.

The current byte-only runtime reader implements that v2 reconstruction policy. It rejects v1, malformed sections, invalid artifact hashes, and semantic/runtime-definition hash disagreement; it has no registry or filesystem requirement.

This milestone uses only a synthetic fixture. It does not ingest JPL, SPICE, or a real Solar System dataset.
