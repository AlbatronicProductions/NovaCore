# Celestial system time and provenance

`SimulationInstant` is NovaCore's exact authoritative time coordinate. A `CelestialSystemDefinition` owns one immutable `CelestialSystemTimeMapping` from that coordinate into its authored ephemeris domain; bodies do not choose independent epochs, scales, or domains.

The mapping is evaluated directly with checked integer arithmetic and `Int128` intermediates. It has no calendar, UTC, time-zone, host-time, or render-frame dependency. For fractional mappings, its result retains a whole domain tick plus a canonical Euclidean remainder: the denominator is positive and the remainder is always nonnegative and smaller than that denominator.

`CelestialTimeDomainId` is opaque to the simulation core. Meanings such as TT, TDB, or J2000 belong to dataset metadata and offline provider adapters, not to `SimulationClock`.

`CelestialEphemerisMetadata` records opaque source, version, coordinate-frame, and constants-version identities; an inclusive supported domain-tick interval; and content and authored-modification hashes. This metadata is included in the immutable system-definition hash so future save/load and replay paths can detect incompatible authored systems or datasets.

Hierarchy nodes hold only a compact `CelestialEphemerisBinding` (model, source ID, and payload index). A system owns immutable typed catalogs for fixed bodies, circular orbits, analytical Kepler trajectories, and sampled ephemerides, plus its declared source metadata. Validation resolves every binding before publication; there is no embedded polymorphic payload in a node. Byte-only NCPE v2 reconstruction is implemented by `NcpeCelestialSystemLoader`; generic provider discovery and dispatch remain separate future responsibilities.

Sampled ephemerides now use the same time mapping and binding boundary; see [Sampled Ephemerides](sampled-ephemerides.md).

The original time/provenance milestone did not implement ephemeris providers, external dataset ingestion, calendar formatting, sampled evaluation, or a real Solar System dataset. Subsequent work added sampled evaluation, the [byte-only NCPE runtime loader](ephemeris-runtime-loader.md), the [offline NAIF adapter](naif-source-adapter.md), and the [compact Solar model](sol-analytical.md). Those additions preserve the exact-time mapping boundary described here.
