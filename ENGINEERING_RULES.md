# NovaCore engineering rules

## Runtime authority

`CelestialSystemDefinition` is immutable runtime celestial authority. NCPE is serialized storage, not another runtime model. Reconstruction publishes atomically or not at all; required dataset values are never inferred.

## Determinism

Determinism is a first-class requirement. Hashes use explicit stable values and order. Do not depend on locale, host time, enumeration order, machine paths, or environment state. Paths and build locations never contribute to dataset identity. Preserve deterministic operation order when floating point participates in identity.

## Offline astronomy

Runtime projects must never depend on CSPICE, NAIF kernels, the NAIF adapter, EphemerisBuilder, network access, host calendars, or source discovery. The intended flow is official source → offline adapter → normalized data → NCPE builder → NCPE artifact → runtime loader → `CelestialSystemDefinition` → evaluator.

## Ephemerides and frames

Analytical and sampled systems share the generic evaluator. Sampled coverage is finite with no extrapolation; cubic Hermite position/velocity interpolation is versioned runtime policy. Provenance participates in identity. Celestial hierarchy, observation/camera origin, gravity hierarchy, launch frames, and SOI policy are separate concepts.

## Native and scope discipline

CSPICE is offline-only through a narrow C ABI. Managed correctness never relies on native finalization. CSPICE uses controlled `RETURN` failures; SHORT/LONG diagnostics are captured before reset. Third-party kernels and native artifacts remain untracked. Prefer small slices, focused tests while developing, and full regression only at checkpoints. Do not alter unrelated deterministic hashes without justification.
