# Repository-debt retirement evidence

**PARTIAL RETIREMENT — FOLLOW-UP REQUIRED.** Read the
[responsibility-island report](../../repository-debt-retirement.md) for the
retired packages, supported consumers, KSA comparison and exact follow-ups.

Budget: **1 MB** for this package. It contains reports/metadata/reproduction
recipes and validation logs, no raw GPU/frame captures or videos. The audit ZIP
was checked member-by-member against original SHA-256; its identity is in
[summary.json](summary.json). The original engineering evidence package is unchanged
except for its documentation status note. Its old hashes remain historical provenance.

| Measured storage | Allocated bytes |
|---|---:|
| Workspace before | 4,578,649,088 |
| Workspace after | 4,324,752,672 |
| build before | 63,888,216 |
| build after | 62,724,952 |
| Four proven redundant cache copies removed | 340,934,656 |
| Permanent evidence retained | 110,592 |

Diagnostic evidence created: **361,114 logical bytes** of text,
inventories, source and logs. Permanent evidence is this bounded package;
**361,114 bytes** of external working copies are disposable after
Project Control review. No bulk diagnostic corpus was generated. Build outputs
are separate, ordinary rebuildable output: actual solution builds restored managed
bin/obj trees and the missing CSPICE shim. See summary.json for net workspace
change; source retirement is not a claim that compiling all projects reduces storage.

All production assets, generators, permanent fixtures, shader sources and accepted
renderer-owner files retained their baseline hashes. Debug/Release deployment each
has 49 matching shaders; sample and Graphics-test DLLs match the native build for
that configuration. GPU physical-height/mesh parity reports zero Vulkan errors.
All 20 projects built in both configurations with zero warnings/errors. No new
manual Florida acceptance or full Graphics-suite PASS is claimed.

Git history/LFS objects and milestone refs were untouched. There are no staged
changes. **git diff --check PASS.** Source/asset cache survivors and six junction
identities were checked against the initial inventory. No performance work began.

## Reproduction

Run explicit native Debug/Release builds before solution builds, as documented in
[build-windows.md](../../build-windows.md). The ZIP preserves filtered regression
logs and the read-only audit/size scripts; adapt their output directory before
running. Launcher probe source defaults to route resolution without a capture.
Archived patch/initial failure logs are provenance, not instructions to replay
mutations against the current checkout. NAIF's initial missing-shim failure is
retained alongside its successful post-build rerun.

## Final Git status

```text
 M NovaCore.sln
 M README.md
 M docs/CODEX_HANDOFF.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/architecture.md
 M docs/build-windows.md
 M docs/diagnostic-output-policy.md
 M docs/engineering-evidence/earth-route-convergence/README.md
 M docs/ephemeris-builder.md
 M docs/planetary-rendering.md
 M docs/production-consolidation.md
 M docs/repository-debt-retirement.md
 M native/NovaCore.Native/PlanetaryHeightQuery.cpp
 M native/NovaCore.Native/PlanetaryMeshPreparation.cpp
 M src/NovaCore.Graphics/PlanetaryLocalTerrain.cs
 M src/NovaCore.Graphics/PlanetaryProductionSurface.cs
 M src/NovaCore.Graphics/PlanetaryTerrainMaterialSynthesis.cs
 D src/NovaCore.Simulation/SimulationWorld.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tools/NovaCore.EphemerisBuilder/Program.cs
?? docs/engineering-evidence/repository-debt-retirement/
```

Nothing staged, committed, pushed, tagged or banked. Stop for Project Control review.
