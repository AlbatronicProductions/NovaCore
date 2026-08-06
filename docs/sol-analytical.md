# SolAnalytical

`SolAnalytical` is NovaCore’s first complete authored Solar-System dataset. It is an immutable, approximate, fixed-epoch analytical model intended for sandbox-scale simulation, not a high-precision ephemeris.

It uses a Sun-rooted hierarchy:

```text
Sun
├── Mercury
├── Venus
├── Earth
│   └── Moon
├── Mars
├── Jupiter
├── Saturn
├── Uranus
└── Neptune
```

The Solar System Barycenter and Earth-Moon Barycenter retain stable reserved IDs but are not nodes in this first gameplay hierarchy. The Moon is explicitly propagated relative to Earth. An Earth-centered view is therefore a derived observation transform, not a separate geocentric physics system.

## Time and coordinates

The opaque analytical time domain has a J2000-equivalent epoch at `SimulationInstant.Zero`, a 1:1 exact seconds mapping, and a finite engine evaluation interval of ±500 Julian years (`365.25 × 86400` seconds per year). Its coordinate metadata identifies J2000/ICRF-equivalent inertial axes. NovaCore does not interpret this as UTC, use calendars, or consult a host clock.

## Sources and conversion

Planetary fixed epoch elements use the JPL Solar System Dynamics “Approximate Positions of the Planets” J2000 table (Table 1), in AU/degrees. NovaCore intentionally omits the published secular rates and supplementary outer-planet terms in this slice. Each element set is converted once at immutable dataset construction to the existing parent-relative Cartesian `TwoBodyTrajectory`; runtime evaluation uses the existing universal-variable propagator.

Earth uses the table’s Earth-Moon-barycenter element set as a documented approximation because this initial hierarchy does not include the barycenter. The Moon uses a fixed J2000-equivalent Earth-relative element set. This dataset is consequently approximately modeled at its epoch, and position accuracy degrades away from it. The ±500-year interval is a supported deterministic engine range, not an astronomy-error guarantee and not JPL/Horizons precision.

The primary validation proof is implementation fidelity: tests independently convert each immutable authored element record to Cartesian position/velocity, compare it with the generated trajectory seed and exact-epoch runtime output, and verify two-body invariants. Direct bounded propagation and generic-system evaluation are compared over an overlapping interval; broad evaluation uses deterministic bounded composition. Familiar AU-scale distances and velocity ranges are retained only as human-readable smoke checks.

Physical gravitational parameters and radii are authoring constants from the NASA/JPL planetary physical-parameter and fact-sheet family; the catalog records the constants source/version metadata. They are immutable body properties, not a gravity implementation.

Custom, edited, or future source-backed systems use the same catalog and ephemeris-binding contracts. Any body identity, constants, hierarchy, trajectory, provenance, coverage, or authored-modification change produces a different deterministic definition hash.

Sources:

- JPL Solar System Dynamics, [Approximate Positions of the Planets](https://ssd.jpl.nasa.gov/planets/approx_pos.html)
- NASA/JPL Solar System Dynamics, planetary physical-parameter data
