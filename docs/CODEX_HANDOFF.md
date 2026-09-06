# NovaCore engineering handoff

This document is the concise workflow handoff. The authoritative current
architecture and milestone state is
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). Durable scope,
acceptance, KSA-reference, and authority rules are in
[ENGINEERING_RULES.md](../ENGINEERING_RULES.md). Do not reproduce their full
terrain narrative here.

Start with the [repository ownership map](repository-structure.md) for placement,
project dependencies, fixtures and generated state. Repository-local `.codex/` is
ignored scratch, not session authority. Investigation archives are indexed under
[engineering history](engineering-evidence/README.md); consult them for provenance,
not as an onboarding requirement.

## Repository rules

- Work in `E:\NovaCore` and inspect `git status --short` before editing.
- Preserve unrelated and unstaged user work.
- Do not stage, commit, push, or tag without explicit instruction.
- Do not commit a rendering milestone before its required physical Desktop
  acceptance unless the user explicitly changes that gate.
- Generated terrain payloads belong in the manifest-managed cache, not ordinary
  Git history or build output.
- Follow the authoritative [diagnostic evidence lifecycle](../ENGINEERING_RULES.md#diagnostic-evidence-lifecycle):
  keep reproducibility, consolidate accepted evidence before banking, retire
  reviewed bulk output, and report storage totals for capture-heavy tickets.
- Use bounded tickets. If implementation exposes an authority conflict or needs
  a broader architecture change, stop and report it.

## Current planetary checkpoint

The banked, production-accepted baseline is **M12D-P2S5H — Earth Route
Convergence**, on **NCSM1 / New Earth Renderer**:

- authoritative production commit `32ffac50ab5c06518ede24edfb5c531976d4ec99`;
- annotated tag `m12d-p2s5h-earth-route-convergence`;
- commit title `NovaCore M12D-P2S5H: converge production Earth routes on NCSM1`;
- accepted Florida result preserved through consolidation and bounded validation;
- repository baseline for debt retirement: `c78b5824e7f99570d696dc4607944964bccf006e`,
  the subsequent diagnostic-video hygiene commit. The P2S5H tag remains untouched.

P2S5G Surface Workload Efficiency remains accepted historical architecture at
`7bb03b0e635fa3444a4243bbad82758a1770ea60`, tag
`m12d-p2s5g-surface-workload-efficiency`, with native 3440×1440 manual PASS.

Earth's accepted production owner is the 18-level NCSM1 New Earth Renderer. It
uses projected-error selection and hysteresis, an exact-lattice moving/snapped
pupil, persistent scale resources, canonical `H(bodyDirection)` physical
preparation, conservative curved-patch planet occlusion, conservative screen
visibility, GPU compaction of original index triplets, KSA-parity per-edge TCS
factors, bounded 50 m TES refinement, indexed indirect draw, valid zero-visible
publication/re-entry, body-specific presentation authority, and fence-confirmed
atomic current/incoming generation publication.

The orbit-to-near-surface native 3440×1440 trajectory is accepted. The recovered
missing-Earth band and device-loss workload did not reproduce, and production
ownership remained exactly one Earth owner with zero overlap or stale
generations.

## Physical and presentation boundary

- FP64 body-fixed height, displacement, physical normals, collision, clearance,
  and physical queries are one canonical physical authority.
- Material shading may consume cheaper deterministic derived classification.
- No material, fallback, LOD, cache, or renderer path may become a second
  physical authority.
- A complete outgoing representation remains authoritative until the incoming
  prepared geometry and draw payload are complete, GPU-ready, and
  fence-confirmed.
- Topology density and pupil triangulation describe presentation, not physical
  truth. `H(bodyDirection)` and the FP64 body-fixed point remain authoritative.
- A rare full pupil rebase may change the coarse factor-1 triangulated
  approximation by up to approximately 2.595 m; the adjacent L14→L15 difference
  is approximately 2.8 mm. Treat these as deferred presentation/morph behavior,
  not moving physical terrain or lost ownership.

## Next architectural decision and work boundary

P2S5G tessellation interface compaction is accepted production architecture:
the TCS user payload is reduced from 45 to 13 scalars while TES physics and
the fragment interface are preserved. The deterministic +89 TES invocation
difference is an accepted bounded invocation-accounting consequence
(classification C), not increased refinement or changed tessellation semantics.
Read [the preserved measurement and closeout record](M12D-P2S5G-workload-investigation.md)
for the evidence and its limits.

The **banked P2S5H production baseline** uses NCSM1 and generation 4 for all six
supported Earth/Solar/Florida launcher routes, including **New Earth Renderer**.
Scene, focus, altitude, camera and Florida initialization remain route-specific.
Florida launcher-based manual acceptance **PASSED** before consolidation.
The superseded dynamic anchored owner, stitch/coverage draw, local texture-demand
transport and its investigation drivers have been retired. Generation-3 numerical
oracles and independent development scenes remain outside production Earth routing.
Terrain-v5 global bootstrap remains only until complete NCSM1 publication;
non-Earth presentation remains independent. P2S5H is banked; subsequent debt
retirement remains separately reviewed and unbanked.

See [production consolidation](production-consolidation.md) for reachability, retained responsibilities and validation.


Complete the bounded repository-debt retirement review before the next performance
objective. Do not reopen P2S5H or move its tag.

- Preserve the accepted NCSM1 production spherical-billboard owner and C3/P2S5F
  coverage/culling architecture.
- Do not regenerate NCSM1 assets without explicit authorization.
- Do not reintroduce radial Eye, adaptive CPU final-raster grids, or dynamic
  patch/stitch ownership into the accepted NCSM1 path.
- Do not increase the 50 m TES range, weaken conservative culling, or reopen C3
  coverage based on visual impression alone; require measured causal evidence.
- Keep remaining surface workload optimization, terrain presentation/material
  quality, and finer pupil/re-triangulation
  morphing distinct from physical terrain and depth ownership.
- Atmosphere, clouds, environmental rebuilding, and spacecraft/surface gameplay
  remain separate future work.

Known non-blocking debt is the remaining orientation-sensitive surface GPU
variability, the existing D3D11 KMT external-memory validation VUID, and the
unrelated opaque distant/detailed handoff orbit-line assertion in the broad
Graphics suite. Do not hide these, but do not reopen accepted ownership,
topology, tessellation, or physical-authority contracts to address them.

## KSA reference workflow

For analogous planetary work, inspect the relevant current installation under
`E:\Kitten Space Agency\` before selecting a responsibility boundary. Record
what problem the technique solves, its pipeline stage and cadence, prepared
data, reuse, synchronization, continuity, and cost bound. Then classify the
NovaCore decision as adopt, adapt, or intentionally differ. Intentional
difference requires measured NovaCore evidence.

KSA is reference evidence only. Do not copy source, shaders, assets, constants,
or proprietary data; do not expose substantial KSA source in documentation or
create a runtime dependency.

## Validation order

1. Run focused parity/build/tests for the bounded change.
2. Run the canonical fixed-pose benchmark only when the workload fingerprint is
   equivalent.
3. Run focused dynamic validation for publication, coverage, and Vulkan state.
4. Run the required 3440x1440 physical trajectory and stop for manual Desktop
   acceptance.
5. Run broad regression only at the requested checkpoint.

A fixed-pose benchmark pass does not imply player-facing acceptance.

## Common commands

```powershell
dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.Launcher.Tests -c Debug
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=m12d-production-spherical-billboard --altitude=10.004 --p2s5c3-traversal --log=validation
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --log=validation
git diff --check
git status --short
```

Configure and build native code from an x64 Visual Studio developer environment
using `build/native-ninja`. Resolve production assets with
`NovaCore.AssetTool`; runtime performs no implicit network acquisition. For
point-and-click physical acceptance, use **New Earth Renderer** in the
launcher. A launcher built in Release starts the Release Triangle runtime; a
launcher built in Debug starts Debug.

Unrelated retirement findings are tracked in [the subsequent debt ledger](repository-debt-retirement.md). Deferral is not permanent acceptance.
