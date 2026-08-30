# Planetary data ingestion

NovaCore's current Earth pipeline produces patch-aligned relaxed cube-sphere
payloads for the terrain-v5 global surface and optional local refinement.

## Inputs

- NASA Blue Marble 21,600 × 10,800 true-color albedo.
- NOAA ETOPO 2022 elevation, converted to the checked 8,192 × 4,096 signed R16
  CPU oracle.
- NASA Blue Marble cloud layer.

See `assets/earth/PROVENANCE.md` for source URLs, hashes, licensing statements,
and current payload identities.

## Build the elevation oracle

```powershell
python tools/earth_data/build_elevation_oracle.py --help
```

The oracle is topology-neutral and remains the CPU authority for deterministic
height queries and camera clearance.

## Build the global terrain payload

```powershell
dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v5
```

The asset tool invokes `tools/earth_data/build_cube_surface_pack.py` with the
arguments recorded by `assets/terrain/manifests/earth-surface-v5.json`. The
result contains all six faces through L2. Every record is one transaction of
albedo, elevation, land classification, and clouds with canonical spherical
gutters.

## Build the local payload

```powershell
dotnet run --project tools/NovaCore.AssetTool -- build earth-local-v2
```

`tools/earth_data/build_local_terrain_pack.py` emits current NCCUBE2 BC7/BC4/BC5
level-12 records using the same body-fixed address and source authority.

## Distribution

Generated payloads are not ordinary Git blobs. Tracked manifests define exact
byte size, SHA-256, generator, hierarchy, and source provenance. The asset tool
verifies or builds the artifact and installs it atomically into the ignored
content-addressed cache. Native runtime receives an explicit verified path and
never performs network acquisition.

## Correctness requirements

- Patch identity is `body / terrain-version / face / level / x / y`.
- +Y is north, +X is longitude zero, and east-positive longitude advances
  toward -Z.
- Cube-face edge and corner samples canonicalize to the same spherical point.
- Child records cannot become visible until their complete data dependency is
  resident and acknowledged.
- Current generators must reproduce the tracked payload digest exactly.
