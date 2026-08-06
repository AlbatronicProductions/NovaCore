# Celestial Bodies

`CelestialSystemDefinition` owns one immutable `CelestialBodyCatalog`. The catalog defines what each body is: its stable `CelestialBodyId`, canonical display name, optional immutable aliases, classification, parent body, and physical constants. It does not define a trajectory, transform, sample range, or mutable state.

`CelestialHierarchyNode` contains only a body ID and an ephemeris binding. The binding selects the separate system-owned trajectory payload catalog. This keeps identity and physical constants independent of whether a system is heliocentric, geocentric, binary, fictional, analytical, or dataset-backed.

Physical properties currently include gravitational parameter, mean/equatorial/polar radii, flattening, and optional stable references reserved for sidereal rotation, atmosphere, and visual metadata. Those references have no evaluator, renderer, atmosphere, terrain, or rotation behavior yet.

Stable IDs—not catalog indices—are the persistent body references intended for future saves and replay metadata. The initial Solar-System namespace reserves IDs for the Solar System Barycenter, Sun, Mercury, Venus, Earth-Moon Barycenter, Earth, Moon, Mars, Jupiter, Saturn, Uranus, and Neptune. No Solar System values or ephemeris dataset are supplied by this contract.

`SolAnalytical` now supplies one approximate, fixed-epoch authored use of the Sun through Neptune and Moon IDs. Its constants remain catalog data and its trajectories remain separate ephemeris bindings; the reserved barycenter IDs are intentionally available for a future source-backed dataset.

Catalog construction validates unique IDs and canonical names, valid classifications and physical values, parent references, and a one-to-one correspondence between authored hierarchy nodes and catalog bodies. Lookup, hashing, and evaluation-time property access are allocation-free after construction.
