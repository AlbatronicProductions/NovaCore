# Celestial system time and provenance

`SimulationInstant` is NovaCore's exact authoritative time coordinate. A `CelestialSystemDefinition` owns one immutable `CelestialSystemTimeMapping` from that coordinate into its authored ephemeris domain; bodies do not choose independent epochs, scales, or domains.

The mapping is evaluated directly with checked integer arithmetic and `Int128` intermediates. It has no calendar, UTC, time-zone, host-time, or render-frame dependency. For fractional mappings, its result retains a whole domain tick plus a canonical Euclidean remainder: the denominator is positive and the remainder is always nonnegative and smaller than that denominator.

`CelestialTimeDomainId` is opaque to the simulation core. Meanings such as TT, TDB, or J2000 belong to dataset metadata and future provider adapters, not to `SimulationClock`.

`CelestialEphemerisMetadata` records opaque source, version, coordinate-frame, and constants-version identities; an inclusive supported domain-tick interval; and content and authored-modification hashes. This metadata is included in the immutable system-definition hash so future save/load and replay paths can detect incompatible authored systems or datasets.

Hierarchy nodes hold only a compact `CelestialEphemerisBinding` (model, source ID, and payload index). A system owns immutable typed catalogs for fixed bodies, circular orbits, analytical Kepler trajectories, and sampled ephemerides, plus its declared source metadata. Validation resolves every binding before publication; there is no embedded polymorphic payload in a node. Source loading and future provider-model dispatch remain deferred.

Sampled ephemerides now use the same time mapping and binding boundary; see [Sampled Ephemerides](sampled-ephemerides.md).

No ephemeris provider, external dataset ingestion, calendar formatting, sampled evaluation, or real Solar System dataset is implemented in this milestone.
