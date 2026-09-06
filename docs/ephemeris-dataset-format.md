# Ephemeris dataset format

NovaCore Ephemeris Artifact v2 (`NCPE`) is a deterministic little-endian binary interchange format produced offline. The implemented byte-only runtime loader reconstructs existing immutable celestial-system contracts without companion catalogs or inferred values. The format is storage; runtime reconstruction and offline astronomical source adaptation are separate responsibilities.

The fixed header contains magic, format version, endianness marker, total byte length, and section count. Each section is `(uint32 type, uint32 byteLength, bytes)`. Required v2 sections are system/time mapping, sources, bodies and physical properties, hierarchy bindings, sampled payloads, flat samples, and hashes. Identifiers are fixed-width unsigned integers; positions and velocities are IEEE-754 double bit patterns. Readers bounds-check all lengths and sections.

V2 preserves supplied body/source/binding declaration order, which participates in its semantic identity. Sampled payloads describe their sample spans and coverage; samples must satisfy the current validation contract. V2 stores the explicit body/physical catalog and bindings needed for reconstruction, but has no named per-body position/velocity error-bound fields. Body-ID sorting and stored per-body error bounds belong to the v1 codec. Preserve those measurements as explicit provenance when migrating v1 data; do not silently sort v2 or fabricate missing metadata.

Hash layers cover metadata/source identity, conversion policy, catalog, hierarchy, payload, a neutral definition hash matching the runtime-definition semantic contract, and final serialized artifact. Artifacts contain no paths, host timestamps, CLR type names, locale-sensitive text, or machine state.

NCPE v1 remains supported for offline compatibility through its public reader/writer; it is explicitly reported as insufficient for runtime reconstruction. The builder CLI writes v2 only. The [public contract and migration policy](public-api-contracts.md) defines that boundary. `NcpeCelestialSystemLoader` defensively reconstructs immutable runtime arrays, recomputes the stored definition hash, and returns a definition only after complete validation; the caller owns publication.

The current byte-only runtime reader implements that v2 reconstruction policy. It rejects v1, malformed sections, invalid artifact hashes, and semantic/runtime-definition hash disagreement; it has no registry or filesystem requirement.

The original format milestone used only a synthetic fixture and did not ingest JPL, SPICE, or a real Solar System dataset. That fixture remains the builder/format regression input. The repository now also includes a separate [offline NAIF adapter](naif-source-adapter.md) and a [DE440-validated compact Solar model](sol-analytical.md); normal Solar evaluation uses the authored model rather than runtime kernel ingestion or NCPE analytical-Kepler reconstruction.
