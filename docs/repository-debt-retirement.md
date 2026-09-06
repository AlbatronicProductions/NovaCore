# Subsequent repository-debt retirement ledger

This ledger records findings deferred from the Earth/Solar/Florida consolidation.
Deferral is not acceptance of permanent debt. KEEP requires a specific current
production, development or reproducibility responsibility. A historical test or
callable old API alone is not sufficient. INVESTIGATE must resolve to a justified
KEEP or a subsequent bounded RETIRE; it is not an indefinite compatibility promise.

| Item / responsibility | Current consumers found | Possible retention reason / evidence still needed | Recommendation |
|---|---|---|---|
| NCPE v1 offline interchange (`EphemerisArtifact.cs`) alongside NCPE v2 | EphemerisBuilder tests and offline artifact tooling; v2 runtime hash/loader are separate | Determine whether a maintained source-conversion workflow or reproducible published artifact still requires v1. Tests alone do not justify it. | INVESTIGATE in an ephemeris-format pass |
| NCCUBE local older header/payload versions and BC4 height codec | `LocalTerrainPack.cpp`, AssetTool codec/fixture generation, local terrain format tests | Production Florida uses format 3 and R16 physical residuals. Determine whether older fixtures protect codec interoperability that is still needed, or can migrate and retire older decoding. | INVESTIGATE; retire unsupported versions after asset/provenance inventory |
| Local pack albedo, normal and control channels after texture-transport retirement | Pack format/parser, source generator, manifests and codec tests; NCSM1 residency consumes physical residuals | Removing unused runtime demand is complete. Changing the multi-channel format or asset identity needs a separate reproducible asset migration. | INVESTIGATE in NCCUBE/source-data pass |
| Legacy celestial authoring APIs | Celestial source definitions and analytical test fixtures | Prove maintained authored-source conversion consumers before narrowing APIs. No change was needed to Earth ownership. | INVESTIGATE |
| Repository-root launch/build helpers, including `ln -c Debug` | Documented developer workflow and launcher regression | `ln -c Debug` is explicitly retained and must continue selecting Debug. Assess duplicate helpers separately against actual developer workflows. | KEEP `ln`; INVESTIGATE duplicates |
| Simulation scaffolding and snapshot/API modernization | Simulation projects, sample controls and deterministic tests | Simulation authority is independent of renderer consolidation. No obsolete responsibility was proven here. | INVESTIGATE in a bounded simulation audit |
| Independent subdivision/spherical topology proof scenes and GPU mesh/query helpers | Explicit development presets; topology, CPU/GPU height/normal and mesh-preparation tests | Current reproducibility and differential GPU correctness oracles. They do not own ordinary Earth rendering. The old dynamic owner no longer supplies shared constants or physical-key definitions. | KEEP for those named development contracts; reassess if consumers retire |
| Rejected `PlanetaryTerrainTessellationStudy` prototype | Only the historical section of the material-synthesis regression | Its old test demonstrates no current production responsibility; numerical study history can remain in documentation. It predates the convergence scaffolding and was not expanded into this ticket. | RETIRE in a subsequent bounded diagnostic cleanup |
| Existing D3D11 KMT external-memory Vulkan VUID `memoryTypeIndex-00645` | Window interop path and live Vulkan validation | Interop still supports the native presentation window. The warning needs its own measured interoperability correction. | INVESTIGATE; not accepted permanently |
| Solution `Release|Any CPU` project mappings | `NovaCore.sln` Release build reports MSB4121 for its unmapped projects | Explicit Release project builds work and are required for actual validation/deployment; the solution command is not evidence that those projects built. | INVESTIGATE / repair in bounded build-configuration work |
| Broad Graphics-suite fixture/order assumptions and distant/detailed orbit-line assertion | Full Graphics test runner | Isolated contracts and integration order need independent fixture ownership. Do not change unrelated renderer behavior to satisfy a stale test. | INVESTIGATE; record concrete failures in consolidation results |
| Remaining near-surface GPU workload and orientation variability | Accepted NCSM1/TES/material rendering | Real production work; preserve quality and measure equivalent steady/moving workloads before the next bounded performance objective. | KEEP architecture; INVESTIGATE measured cost separately |

No unrelated item above was retired by this ticket. Every later retirement must
identify current consumers and reproducibility obligations before deletion.
