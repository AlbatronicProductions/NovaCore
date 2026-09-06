# Repository structure and ownership

This is the directory-placement authority. Start with [engineering rules](../ENGINEERING_RULES.md),
[current state](NOVACORE_CURRENT_STATE.md) and [architecture](architecture.md).
Subsystem documents explain contracts; [engineering history](engineering-evidence/README.md)
records how they were established. A historical report's “unbanked”, “blocked” or
“current” wording describes its dated snapshot, not today's production authority.

## Permanent map

| Path | Responsibility and source-control boundary | Put elsewhere |
|---|---|---|
| `src/` | Six managed projects: reusable authority, presentation, serialization, ABI and logging. Track source and explicitly declared embedded runtime data. | Executables' scenario orchestration belongs in samples/tools; generated `bin/obj` stay ignored. |
| `native/NovaCore.Native/` | Vulkan/Win32 ownership, terrain GPU preparation, native readers, renderer submission, shader source and native validation utilities. Track source/CMake. | Compiled DLLs/SPIR-V belong in ignored build/deployment output. |
| `native/NovaCore.CSpiceShim/` | Source for the narrow offline CSPICE adapter ABI. | Downloaded toolkit/kernels belong in `external/naif`; never a game runtime dependency. |
| `assets/` | Declared production resources, generator inputs, manifests/provenance and clearly labeled topology fixtures. Track reviewed content; selected large binaries use existing LFS rules. | Acquired/cache `.nccube` payloads and temporary diagnostics are not source authority. |
| `tests/` | Executable regressions grouped by invariant owner; bounded fixture files under `tests/fixtures`. | Investigation captures are temporary `build/` output; permanent evidence needs explicit promotion. |
| `tools/` | Offline acquisition/generation, asset distribution, reference extraction and the point-and-click launcher. | Tool outputs follow their manifest/build/evidence destination, not a new source tree. |
| `samples/` | Active runtime composition/examples: Triangle is the supported launcher runtime; ReferenceFrameFixture is a console example/probe. | Shared algorithms belong to their managed/native owner. |
| `docs/` | Current subsystem/operational contracts and links to dated reports. `NOVACORE_CURRENT_STATE.md` owns the current checkpoint; architecture owns relationships. | Runtime must not load a report or historical source snapshot. |
| `docs/engineering-evidence/` | Indexed historical reports, hashes, compact evidence and recipes; track only justified bounded packages. | No default archive of raw frames/GPU buffers, SDKs or installation copies. |
| `external/naif/` | Ignored acquired CSPICE toolkit/kernels and offline shim build output; provenance is tracked in the NAIF/source documents. | No normal runtime dependency or undocumented vendored source authority. |
| `.novacore/cache/` | Ignored cache: manifest-verified terrain payloads, acquired sources and offline build staging. AssetTool owns the banked lifecycle. | No source identity derived from cache paths or implicit machine contents. |
| `.codex/` | Ignored repository-local agent scratch; no shared configuration is currently present or required. | Shared rules belong in `ENGINEERING_RULES.md`; explicitly review any future shared configuration before changing this boundary. |
| `build/`, any `bin/obj/` | Ignored rebuildable output, deployments and active diagnostics. Some old directories contain redirects to retained evidence or local reference scratch. | Permanent conclusions belong in indexed docs, not a build warehouse. |
| `.git/` | Git implementation/history storage, managed by Git. | No renderer/cache/diagnostic ownership; no manual pack/LFS maintenance in structural work. |
| Root files | `README.md`: orientation; `ENGINEERING_RULES.md`: delivery/authority rules; `NovaCore.sln`: composition; `Directory.Build.props`: managed defaults; `.gitignore`: local/generated boundary; `.gitattributes`: existing binary/LFS transport. | Logs, one-off binaries, captures and shell transcripts. |

## Managed ownership — KEEP all six projects

Actual project references form an acyclic graph:

```text
Core              (no project dependencies)
EphemerisFormat   (no project dependencies)
Interop           (no project dependencies)
Platform          (no project dependencies)
Graphics          -> Core, Interop
Simulation        -> Core, EphemerisFormat
```

- **Core:** FP64 math, reference frames, camera primitives and body-fixed surface
  anchor/support contracts. No Vulkan resource ownership or astronomical file I/O.
- **Simulation:** time/clock/timeline, transactions, immutable celestial definitions,
  evaluation and spacecraft state. No renderer or NAIF runtime dependency.
- **EphemerisFormat:** serialized NCPE storage contract. Runtime reconstruction and
  celestial authority remain in Simulation; offline builders/adapters stay tools.
- **Interop:** fixed-layout managed/native transport and P/Invoke declarations.
  It does not own native handles or physical/celestial state.
- **Graphics:** derived render data, terrain physical-reference/query contracts,
  topology generation/loading, pupil/publication coordination and presentation.
  CPU/GPU parity protects the single generation-4 height contract; it does not make
  a second simulation timeline or celestial authority.
- **Platform:** consumed log categories/options. The empty `RuntimeHost` scaffold
  is retired under the [API contract decision](public-api-contracts.md#runtimehost-retired);
  actual startup/window ownership remains in the sample/native boundary.

`PlanetarySphericalBillboardNaturalTerrainProof` has both proof orchestration and
`PrepareProductionIncremental`, used by the moving-runtime preparation seam.
Do not classify a public helper as obsolete from “Proof” in its name. The native
GPU production path and independent managed/query reference path must remain
distinguishable. The [API contract policy](public-api-contracts.md#proofpreparation-ownership)
defines the retained developer surface and the gate for any public rename/extraction.

## Asset families — one production topology owner

| Family | Classification | Producer → manifest → consumer |
|---|---|---|
| `assets/earth/` | KEEP: production elevation oracle + generator input/provenance | NASA/NOAA imagery/elevation, `build_elevation_oracle.py`, cube/local builders → provenance and content sidecars → CPU/GPU physical height, terrain-package generation. Content sidecars describe record content; they are not a second install manifest. |
| `assets/planetary-nested-scale-mesh/` | KEEP: current production | `PlanetaryNestedScaleMeshTopologyGenerator` → `nested-scale-mesh-manifest.json` → `PlanetaryNestedScaleMeshTopologyLibrary`, sample startup/NCSM1 adapter and integrity regressions. |
| `assets/planetary-production-topology/` | FIXTURE: retained transactional terrain regressions | Production-spherical topology generator → `production-manifest.json` → topology/runtime/seating regressions. Historical directory name is not current Earth authority; see its [role](../assets/planetary-production-topology/README.md). |
| `assets/planetary-topology/` | FIXTURE / development proof | Spherical-billboard generator → `manifest.json` → GPU/natural-terrain proofs and supported development route; see its [role](../assets/planetary-topology/README.md). |
| `assets/terrain/` | KEEP: distribution authority | Reviewed `NovaCore.TerrainAsset/1` manifests → `TerrainAssetCache`, AssetTool, sample/launcher asset resolution → hash-addressed cache payloads. |
| `tests/fixtures/terrain/` | FIXTURE: small permanent format/cache regressions | Tiny-global generator and tiny-local generator/test → digest-checked fixture files → reader, distribution and corruption tests. Never substitute them for Earth production data. |

Normal Earth/Solar/Florida startup explicitly resolves verified assets. Missing
required payloads yield acquisition/build guidance; runtime does not silently
generate or download them. The USGS source under `.novacore/cache/sources` is
acquired and verified by the retained script before regeneration. Cache eviction
and recovery policy remain in [terrain-assets](terrain-assets.md) and
[cache lifecycle](cache-lifecycle-policy.md); no redesign here.

## Native ownership without artificial library boundaries

- `NovaCoreNative.cpp` and `NovaCoreNative.h`: the live App owner, platform/device,
  window/swapchain, submission, shared resources and public C ABI. Size alone is
  not a reason to split this lifecycle across libraries.
- `ProductionCubeSurface.*`, `LocalTerrainPack.*`: global/regional serialized
  readers and identity, not demand policy or network acquisition.
- `RegionalPhysicalResidency.h` plus `RegionalPhysicalResidency.inl` and
  `RegionalPhysicalPreparation.inl`: bounded residual demand, readiness, asynchronous
  preparation and publication at the native App boundary. `RegionalPhysicalProbe.inl`
  is opt-in regression evidence, not an alternate physical owner.
- `PlanetaryHeightQuery.*`, `PlanetaryMeshPreparation.*`: independent GPU physical
  query/preparation contexts and parity; `PlanetarySphericalBillboardGpuProof.*`:
  isolated proof lifecycle. Their explicit callers and tests justify retention.
- `AuthoredFacilityGeometry.h` and `FacilityLightOcclusion.h`: authored geometry
  versus derived lighting/visibility. They do not replace canonical terrain height.
- `PresentationGpuTestDevice.h` and three native test sources: strict native test
  infrastructure, not production window/device ownership.
- `shaders/` owns GLSL sources; CMake produces SPIR-V; sample project deployment
  copies a declared runtime subset. Test-only shaders need not be deployed to players.
  Configuration hashes and canonical environment checks belong to Graphics tests.

## Test and tool placement

| Test owner | Current invariant |
|---|---|
| Precision / ReferenceFrames / Camera | Numerical transport, frame resolution, camera/input and geometry contracts. |
| Simulation | Time, determinism, transactions, celestial authority and spacecraft rotation/guidance. |
| Graphics | ABI, physical height, topology, assets/cache, publication/ownership, facility contact/lighting, GPU parity and native window lifecycle. Milestone names retain traceability; categories identify execution requirements. |
| EphemerisBuilder / NAIF adapter | Serialization/reconstruction and offline extraction/reference validation; neither makes NAIF a runtime dependency. |
| Launcher | Supported preset arguments, configuration, deployment and route intent. |
| Native regional / visibility / material tests | Residual readiness/cache identity, lighting CPU/GPU agreement and coordinate precision. |

`NovaCore.AssetTool` owns install/verify/build/cache operations.
`earth_data` owns acquisition and deterministic Earth conversion plus tiny-local
regeneration tests; `terrain_assets/build_test_fixture.py` owns tiny-global.
`NovaCore.EphemerisBuilder` serializes NCPE; `NovaCore.NaifEphemerisAdapter` owns
offline CSPICE extraction and compact lunar-orientation generation.
`NovaCore.Launcher` owns player-facing point-and-click configuration, not rendering.
Do not collapse unrelated tools to reduce project count.

## Contracts and reference boundaries

Public NCPE v1 offline readability, generic NCCUBE reader variants and retained
public authoring/camera/position APIs have explicit boundaries in
[compatibility contract resolution](compatibility-contract-resolution.md) and
[tiny-local/internal-authoring migration](tiny-local-internal-authoring-migration.md).
Their dated audit is evidence; it is not authorization to retire a public contract.
Current global terrain uses NCCUBE1; that name is not proof of obsolescence.

`E:\Kitten Space Agency` is an external read-only installed reference. Its content,
binaries and shaders are not NovaCore source authority. Existing ignored decompilation
scratch under `build/ksa-residency-reference` is local reference material, never a
vendored dependency or bankable source tree. Do not copy installation/source/history
into tracked NovaCore. A source repository deliberately separates generators,
tests and provenance more explicitly than a shipped game installation.

The [dated structure audit](engineering-evidence/repository-structure/README.md)
contains the required decision matrix, root-debris evidence, storage and deferred
items. It is a snapshot; update this map when an authorized ownership change lands.
