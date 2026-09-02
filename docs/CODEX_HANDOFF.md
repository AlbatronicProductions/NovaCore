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

The M12C closeout candidate extends the banked M12B/11B-7H1 GPU terrain baseline.
Candidate D and the canonical fixed-pose benchmark are retained.
M12C-4B's declarative frequency/representability invariants survive, but its
runtime guard/edge bridge between unrelated height authorities is rejected.
The fixed-axis generation-3 carrier family is also rejected as the long-term
near-field primitive.

The Phase 1 automated candidate now makes global/fallback and anchored/refined
geometry consume one canonical oracle-plus-regional physical-height authority;
terrain-v5 remains presentation data only. Complete transactional coverage and
stencil global fill are unchanged. Manual physical replay accepted the primary
Phase 1B moving-reference-frame hitch correction. The rejected runtime
guard/edge bridge, material-cost profiling bypasses, and obsolete CPU/global-
selector parity oracle are removed. Canonical production GPU p95 remains about
19.94-20.00 ms at 3440x1440, so the stable 60 FPS target remains open. See the
primary current-state document for the exact validation and hash decisions.

## Physical and presentation boundary

- FP64 body-fixed height, displacement, physical normals, collision, clearance,
  and physical queries are one canonical physical authority.
- Material shading may consume cheaper deterministic derived classification.
- No material, fallback, LOD, cache, or renderer path may become a second
  physical authority.
- A complete outgoing representation remains authoritative until the incoming
  prepared geometry and draw payload are complete and GPU-ready.

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
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=earth --dynamic-traversal --log=validation
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --log=validation
git diff --check
git status --short
```

Configure and build native code from an x64 Visual Studio developer environment
using `build/native-ninja`. Resolve production assets with
`NovaCore.AssetTool`; runtime performs no implicit network acquisition. For
point-and-click physical acceptance, use **Earth — Fullscreen Native** in the
launcher.
