# Ephemeris dataset format

NovaCore Ephemeris Artifact v1 (`NCPE`) is a deterministic, offline-only, little-endian binary interchange format. It is not yet a runtime loader or astronomical source adapter.

The fixed header contains magic, format version, endianness marker, total byte length, and section count. Each section is `(uint32 type, uint32 byteLength, bytes)` in this required order: metadata, bodies, flat samples, hashes. Identifiers are fixed-width unsigned integers; positions and velocities are IEEE-754 double bit patterns. Readers bounds-check all lengths and sections.

Bodies are ordered by body ID; samples are ordered by ascending domain tick within each body. The artifact stores one root, parent/source IDs, interpolation model, coverage, and measured position/velocity error bounds. It rejects invalid hierarchy, duplicate bodies, non-monotonic or non-finite samples, invalid headers, and hash or length mismatches.

Hash layers cover metadata/source identity, conversion policy, catalog, hierarchy, payload, and final serialized artifact. Artifacts contain no paths, host timestamps, CLR type names, locale-sensitive text, or machine state.

This milestone uses only a synthetic fixture. It does not ingest JPL, SPICE, or a real Solar System dataset.
