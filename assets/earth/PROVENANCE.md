# NovaCore Earth data provenance

NovaCore's current Earth payloads are deterministic derivatives of public U.S.
government scientific data. Source channels remain logically separate during
offline processing.

## True-color albedo

- NASA Earth Observatory / Visible Earth, *Blue Marble: Next Generation*,
  December 2004, cloud-free true color, 21,600 × 10,800, EPSG:4326.
- Repository source: `source/nasa-blue-marble-2004-12-21600x10800.png`.
- Original URL:
  <https://eoimages.gsfc.nasa.gov/images/imagerecords/74000/74218/world.200412.3x21600x10800.png>
- SHA-256:
  `4ee45a0a18229e5667b3523088567e11ea4d857ceac8d7a2d7b6130d5376c5a6`.
- NASA media guidance applies; NovaCore does not imply NASA endorsement.

## Elevation and land classification

- NOAA National Centers for Environmental Information, *ETOPO 2022 15
  Arc-Second Global Relief Model*, ice-surface variant distributed at 60
  arc-seconds, EPSG:4326.
- DOI: <https://doi.org/10.25921/fd45-gt74>.
- Source filename: `ETOPO_2022_v1_60s_N90W180_surface.tif`.
- Dataset metadata states that it is not subject to U.S. copyright protection
  and is not suitable for navigation.
- NovaCore derives the land/ocean classification from the zero-metre contour.

The source TIFF is not a tracked runtime blob. The current checked CPU oracle is
`runtime/earth_elevation_8192x4096.r16` and is reproduced by
`tools/earth_data/build_elevation_oracle.py`.

## Clouds

- NASA Visible Earth, *Blue Marble: Clouds*, 2,048 × 1,024.
- Repository source: `source/nasa-blue-marble-clouds-2048.jpg`.
- Original URL:
  <https://eoimages.gsfc.nasa.gov/images/imagerecords/57000/57747/cloud_combined_2048.jpg>.
- NASA media guidance applies; NovaCore does not imply NASA endorsement.

## Current payloads

`tools/earth_data/build_cube_surface_pack.py` builds `earth_surface_v5.nccube`
from the current albedo, elevation oracle, and cloud sources. Its tracked asset
identity is `assets/terrain/manifests/earth-surface-v5.json`:

- body ID 6, terrain version 5;
- complete relaxed cube-sphere L0-L2 hierarchy;
- 126 patch-aligned records;
- RGB8 sRGB albedo, R16 elevation, R8 land classification, R8 clouds;
- byte size 61,484,224;
- SHA-256
  `38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae`.

`tools/earth_data/build_local_terrain_pack.py` builds the optional
`earth_local_v2.nccube` from the same lawful albedo and elevation authority. Its
tracked identity is `assets/terrain/manifests/earth-local-v2.json`:

- terrain version 5, payload format version 2;
- sparse level-12 relaxed cube-sphere records;
- BC7 albedo, BC4 height, BC5 normal payloads;
- byte size 7,652,567;
- SHA-256
  `60ada8949bfd782dfaea6c04270186bda52654d7263fbc3dbda5eaa4fd2e578a`.

The geographic convention is right-handed: +Y north, +X at longitude zero,
and east-positive longitude toward -Z. Offline source sampling converts the
EPSG:4326 raster into canonical face-aligned records and cross-face gutters.

Generated payloads are installed through `NovaCore.AssetTool` into the ignored
content-addressed runtime cache. Normal runtime performs no implicit download.
NumPy 2.3.5 and Pillow 11.3.0 are the pinned offline Python dependencies.
