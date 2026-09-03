# NovaCore engineering handoff

This document is the concise workflow handoff. The authoritative current
architecture and milestone state is
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). Durable scope,
acceptance, KSA-reference, and authority rules are in
[ENGINEERING_RULES.md](../ENGINEERING_RULES.md). Do not reproduce their full
terrain narrative here.

## Repository rules

- Work in `E:\NovaCore` and inspect `git status --short` before editing.
- Preserve unrelated and unstaged user work.
- Do not stage, commit, push, or tag without explicit instruction.
- Do not commit a rendering milestone before its required physical Desktop
  acceptance unless the user explicitly changes that gate.
- Generated terrain payloads belong in the manifest-managed cache, not ordinary
  Git history or build output.
- Use bounded tickets. If implementation exposes an authority conflict or needs
  a broader architecture change, stop and report it.

## Current planetary checkpoint

Start from the banked and physically accepted M12D-P2S5C3 baseline:

- main commit `e9b35e4c9f475adffb38f22ac705835d8d5f4181`;
- tag `m12d-p2s5c3-production-billboard-stabilization`;
- commit title `NovaCore M12D-P2S5C3: stabilize production billboard coverage and terrain continuity`.

Earth's production owner is the 18-level spherical-billboard runtime. It uses
projected-error selection and hysteresis, an exact-lattice moving/snapped pupil,
canonical `H(bodyDirection)` physical preparation, conservative curved-patch
planet occlusion, conservative screen visibility, GPU compaction, bounded 50 m
TCS/TES refinement, indexed indirect draw, and fence-confirmed atomic
current/incoming generation publication.

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

## Next authorized work boundary

- Preserve the banked production spherical-billboard owner and C3
  coverage/culling architecture.
- Do not regenerate `.nctop2` assets without explicit authorization.
- Do not restore the radial Eye, adaptive CPU final-raster grids, dynamic
  patch/stitch ownership, or any other retired terrain system.
- Do not increase the 50 m TES range, weaken conservative culling, or reopen C3
  coverage based on visual impression alone; require measured causal evidence.
- Keep terrain presentation/material quality and finer pupil/re-triangulation
  morphing distinct from physical terrain and depth ownership.
- Atmosphere, clouds, environmental rebuilding, and spacecraft/surface gameplay
  remain separate future work.

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
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --focus=earth --p2s5c3-traversal --log=validation
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --log=validation
git diff --check
git status --short
```

Configure and build native code from an x64 Visual Studio developer environment
using `build/native-ninja`. Resolve production assets with
`NovaCore.AssetTool`; runtime performs no implicit network acquisition. For
point-and-click physical acceptance, use **Earth — Fullscreen Native** in the
launcher. A launcher built in Release starts the Release Triangle runtime; a
launcher built in Debug starts Debug.
