# Sampled ephemerides

Sampled ephemerides are immutable authored/source data, not authoritative mutable simulation state. A `CelestialSystemDefinition` owns one flat shared sample block. Each sampled payload binds a contiguous non-overlapping range in that block through an index and count; hierarchy nodes retain only a model/source/payload binding.

The evaluator maps `SimulationInstant` into `CelestialTimeArgument`, preserves its rational remainder through deterministic binary-search bracketing, and converts to the cubic-Hermite parameter only after selecting the adjacent samples. Exact sample hits return stored position and velocity bits directly. Coverage is finite and inclusive at declared endpoints; NovaCore never extrapolates, clamps, or wraps a sampled trajectory.

`CubicHermitePositionVelocityV1` interpolates parent-relative position and velocity from position/velocity endpoints. It yields derived frame state for the existing composition and atomic publication path; it does not mutate simulation state.

No JPL/SPICE importer, real astronomical dataset, packed unmanaged storage, or memory mapping exists yet. The logical flat catalog can later sit over packed or memory-mapped storage, and future sampled-Chebyshev support can use the same binding and hierarchy contracts. `SampledDemo` is deliberately authored test data, not real astronomy.

The offline-only self-describing `NCPE` v2 artifact and `NovaCore.EphemerisBuilder` define the reproducible conversion seam for future source adapters. They are separate from runtime simulation: no runtime project parses source data or accesses the file system. See [Ephemeris dataset format](ephemeris-dataset-format.md) and [Ephemeris builder](ephemeris-builder.md).

The runtime byte reader reconstructs existing immutable sampled contracts from NCPE v2 without keeping the artifact buffer. It does not add a dataset registry, file discovery, or source-adapter dependency.
