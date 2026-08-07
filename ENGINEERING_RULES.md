# NovaCore engineering rules

## Authority and presentation

- Simulation owns truth: exact time, celestial definitions, hierarchy, ephemerides, physical constants, and authoritative state live outside Graphics.
- `CelestialSystemDefinition` is immutable runtime celestial authority. NCPE is serialized storage, not a second runtime model.
- Graphics owns presentation only. `PlanetaryPresentationSnapshot` and other renderer inputs are immutable derived data; presentation must never change celestial state.
- Camera-relative subtraction occurs in high precision before FP32 GPU transport. Do not relocate bodies, compress orbital distances, or inflate physical radii to solve a presentation problem.

## Determinism

Determinism is a first-class requirement. Hashes use explicit stable values and order. Do not depend on locale, host time, enumeration order, machine paths, or environment state. Paths and build locations never contribute to dataset identity. Preserve deterministic operation order when floating point participates in identity. CPU reference implementations remain correctness oracles for equivalent GPU presentation paths.

## Offline astronomy

Runtime projects must never depend on CSPICE, NAIF kernels, the NAIF adapter, EphemerisBuilder, network access, host calendars, or source discovery. The intended flow is official source → offline adapter → normalized data → NCPE builder → NCPE artifact → runtime loader → `CelestialSystemDefinition` → evaluator. Offline astronomy tools and source files never become runtime dependencies.

## Ephemerides and frames

Analytical and sampled systems share the generic evaluator. Sampled coverage is finite with no extrapolation; cubic Hermite position/velocity interpolation is versioned runtime policy. Provenance participates in identity. Celestial hierarchy, observation/camera origin, gravity hierarchy, launch frames, and SOI policy are separate concepts.

## Renderer and native boundary

GPU LOD, culling, label selection, and representation handoff are presentation-only. They do not enter simulation identity, persistence, replay, or dataset hashes. Renderer resources have explicit native ownership and deterministic lifecycle; no Vulkan handle or C++ ownership-bearing object crosses the managed boundary. Native/managed ABI changes require explicit fixed-width layout, offsets where applicable, and focused managed/native validation.

## Scope discipline

CSPICE is offline-only through a narrow C ABI. Managed correctness never relies on native finalization. CSPICE uses controlled `RETURN` failures; SHORT/LONG diagnostics are captured before reset. Third-party kernels and native artifacts remain untracked. Prefer small slices and focused tests while developing; run full regression only at checkpoints. Do not alter unrelated deterministic hashes or established contracts without a concrete reason and regression coverage.
