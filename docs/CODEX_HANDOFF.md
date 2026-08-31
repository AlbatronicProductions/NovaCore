# NovaCore Codex handoff

## Repository rules

- Work in `E:\NovaCore`.
- Preserve unrelated user changes.
- Do not stage, commit, push, or tag without explicit instruction.
- Generated terrain payloads belong in the manifest-managed cache, not ordinary
  Git blobs.

## Current surface architecture

Earth global coverage is `earth-surface-v5`, a body-fixed L0-L2 relaxed
cube-sphere. `PlanetaryDynamicAnchoredSurface` is the production multiscale
refinement hierarchy. It uses canonical patch identity
`body / terrain-version / physical-surface-generation / face / level / x / y`,
projected-error demand, complete-generation publication, a 6,144-patch active
descriptor limit, and an 8,192-slot cache. Demand covers a 32-64 km retained
body-local neighborhood with a 128-1,000 m recenter threshold; camera direction
affects GPU visibility rather than physical residency. The global owner remains
visible while a demanded generation prepares; a generation becomes visible
only after every required patch and its GPU acknowledgement are complete.

`earth-florida-m12` is the current regional payload: USGS 3DEP-derived R16
physical residuals plus BC7 albedo, BC5 diagnostic normals, and R8 control in a
complete L8-L11 NCCUBE2-v3 hierarchy. Signed global elevation and regional
residual compose before the non-negative physical-height clamp; the same
`PlanetaryPhysicalSurface` evaluator owns camera and geometry height.
Material-only detail cannot change geographic identity, clearance, or contact.

The dynamic hierarchy receives camera position and SurfaceAnchor state only as
physical-availability inputs. Patch identity remains body fixed. The current
defaults are 256 target pixels, maximum level 20, 700 km acquisition, and 800
km release. Pure yaw/pitch rotation reuses the retained generation.

## Precision and camera invariants

- Solar-root FP64 owns celestial translation.
- Body-fixed/body-relative FP64 owns surface direction, height, clearance, and
  SurfaceAnchor identity.
- CPU subtraction produces camera-relative values before GPU narrowing.
- Camera exterior safety uses the CPU physical-surface oracle with a 10 m
  minimum and bounded radial correction.
- Navigation transitions cannot mutate Earth position, radius, orientation, or
  body-fixed geography.

## Current validation facts

Exact physical/address parity covers 567 samples, including local payload
coverage. Maximum final-height difference is `8.384518E-009 m`; maximum normal
difference is `2.980232E-008 rad`; address and footprint mismatches are zero.

The exact 3440x1440 moving Vulkan traversal covers 18 poses and 801 frames. It
reaches 4,194 demanded patches with an 8,192-slot bounded cache and records zero
physical drift, edge gap, ownership hole, ownership overlap, or stale patch.
Its final 312-frame window performs slow and rapid 360-degree turns with one
unchanged generation and demand set, zero selection/misses/evictions/
preparation/uploads, and 10.78 ms average / 11.29 ms p95 CPU cadence.

## Build and validation

```powershell
dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.Launcher.Tests -c Debug
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=earth --dynamic-traversal --log=validation
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --log=validation
git diff --check
git status --short
```

Configure/build native code with the x64 Visual Studio developer environment
and `build/native-ninja`. Runtime terrain assets must resolve through
`NovaCore.AssetTool` before Vulkan proofs.

## Supported launcher intents

Solar overview, Earth 3,000 km, Earth 700 km, Earth — Fullscreen Native,
Florida launch site, and one screen-space subdivision diagnostic are supported.
The fullscreen preset resolves the active desktop and owns its native-window
and validation/performance environment without requiring shell setup.

## M12 physical-data checkpoint

The 11B-7H1 renderer baseline remains banked. M12 adds a provenance-tracked
roughly 98 x 111 km Florida physical region, categorical control, a 275 m
launch-site reservation, millimetre-class residual quantization, retained
streaming, and final-surface normal authority. Manual 3440x1440 M12 acceptance
is still required. Fragment optimization, compute triangle compaction,
movement-time selector tuning, and the bounded distant-representation study
remain measured future work; do not reopen the renderer architecture without a
genuine regression.

## M12B planet-wide synthesis checkpoint

M12B adds one body-fixed FP64 biome/control authority with up to four blended
contributors, five deterministic terrain modifier families, tessellation-stage
near physical relief, and seven shared material families. The result is
complete without regional data; Florida remains a higher-fidelity residual and
reservation overlay. CPU/GPU height and physical-normal parity is retained.
Manual native 3440x1440 acceptance remains required across Florida,
Appalachians, mountain/alpine, desert/dune, snow/glacial, grassland, and a
no-regional-data region. Use **Earth — Fullscreen Native**; no shell setup is
required.
