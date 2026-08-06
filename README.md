# NovaCore

NovaCore is a deterministic C# spaceflight-simulation foundation with a native C++20/Vulkan rendering backend. It is designed around immutable datasets, reproducible celestial mechanics, and offline-generated ephemerides. It is a foundation, not a complete game engine.

## Current Architecture

NovaCore keeps authority in managed simulation and treats rendering as a downstream consumer of immutable evaluated data.

- **Simulation** owns exact time, transactions, authoritative celestial and spacecraft state, and deterministic evaluation.
- **Precision** provides double-precision spatial mathematics and camera-relative GPU transport.
- **Graphics** consumes resolved immutable render data and owns no simulation state.
- **Camera** is managed, frame-aware, and resolves its authoritative state before graphics transport.
- **Reference Frames** provide immutable topology, evaluated transforms, and deterministic frame resolution.
- **EphemerisFormat** defines the neutral, self-describing NCPE v2 storage contract.
- **EphemerisBuilder** is the offline deterministic artifact builder.
- **NaifEphemerisAdapter** is an offline-only contract boundary for a future pinned NAIF/CSPICE source adapter.

```text
Official Sources
    ↓
NaifEphemerisAdapter
    ↓
EphemerisBuilder
    ↓
NCPE v2
    ↓
Runtime Loader
    ↓
CelestialSystemDefinition
    ↓
Simulation
```

Runtime code never depends on CSPICE, NAIF kernels, network resources, calendars, or the builder. The current NAIF manifest is an explicitly unpinned template; it cannot produce an authoritative dataset until official files are locally verified and pinned.

## Implemented Features

- Immutable `CelestialSystemDefinition` runtime authority.
- Binding-only celestial hierarchy with typed fixed, circular, analytical-Kepler, and sampled ephemeris catalogs.
- Exact `SimulationInstant` to `CelestialTimeArgument` mapping.
- Deterministic analytical trajectories, scheduled inertial impulses, and rigid-body attitude evaluation.
- Flat sampled cubic-Hermite ephemerides with finite coverage and no extrapolation.
- Deterministic raw-value hashing and immutable reference-frame extraction.
- Self-describing NCPE v2 artifacts and byte-only runtime reconstruction.
- Three-way NCPE definition-hash verification: stored, neutral semantic, and reconstructed runtime definition hashes must agree.
- Offline NCPE builder with a synthetic fixture.
- Offline NAIF adapter contracts, stable Sol target map, source manifest template, and deterministic parent-relative SI conversion.
- Double-precision camera-relative Vulkan rendering, reusable meshes, instancing, and analytical orbit visualization.

## Engineering Principles

- Deterministic execution from explicit authoritative inputs.
- Immutable runtime definitions and evaluated snapshots.
- Offline preprocessing for source-dependent data.
- Zero-allocation warmed runtime evaluation paths where measured contracts require it.
- Explicit source, constants, conversion-policy, and artifact provenance.
- Reproducible generated datasets and stable raw-value hashes.
- Runtime independence from astronomy libraries and source files.

## Building and Testing

Windows development uses .NET and the configured native Vulkan build.

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja --config Debug

dotnet build NovaCore.sln -c Debug

dotnet run --project tests/NovaCore.Simulation.Tests -c Debug
dotnet run --project tests/NovaCore.Precision.Tests -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Debug
dotnet run --project tests/NovaCore.Camera.Tests -c Debug
dotnet run --project tests/NovaCore.EphemerisBuilder.Tests -c Debug
dotnet run --project tests/NovaCore.NaifEphemerisAdapter.Tests -c Debug
```

## Documentation

- [Architecture](docs/architecture.md)
- [Celestial Simulation](docs/celestial-simulation.md)
- [Sampled Ephemerides](docs/sampled-ephemerides.md)
- [Ephemeris Dataset Format](docs/ephemeris-dataset-format.md)
- [Ephemeris Builder](docs/ephemeris-builder.md)
- [Ephemeris Runtime Loader](docs/ephemeris-runtime-loader.md)
- [NAIF Source Adapter](docs/naif-source-adapter.md)
- [Reference Frames](docs/reference-frames.md)
- [Precision Model](docs/precision-model.md)
- [Camera](docs/camera.md)
- [Build on Windows](docs/build-windows.md)

## Current Roadmap

Completed:

- 9A-1 — generic celestial-system definitions.
- 9A-2 — deterministic celestial-system evaluation.
- 9A-3 — celestial time, ephemeris binding, and sampled-storage contracts.
- 9A-4B — immutable celestial identity and physical constants.
- 9A-4C — `SolAnalytical` authored analytical dataset.
- 9A-4D — deterministic NCPE artifact format and builder.
- 9A-4E-1 — NCPE v2 runtime reconstruction and hash verification.
- 9A-4E-2 — official-source adapter design.
- 9A-4E-3 — NAIF source manifest and offline adapter contracts.

Next:

- 9A-4E-4 — extract bounded real DE440 states through CSPICE in the offline adapter.

NovaCore does not yet include real ephemeris samples, N-body gravity, patched conics, SOIs, asset tooling, an ECS, a scene graph, a runtime dataset registry, or save/replay loading.
