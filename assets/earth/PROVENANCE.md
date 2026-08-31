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

## Florida M12 regional physical elevation

- U.S. Geological Survey 3D Elevation Program, historical 1/3 arc-second DEM,
  product `USGS 1/3 Arc Second n29w081 20221103`.
- Original URL:
  <https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/13/TIFF/historical/n29w081/USGS_13_n29w081_20221103.tif>
- Source filename: `USGS_13_n29w081_20221103.tif`; 10,812 x 10,812
  Float32 samples; 1/3 arc-second horizontal spacing; elevation in metres.
- CRS: EPSG:4269 (NAD83 geographic). The package builder samples longitude and
  latitude into NovaCore's canonical east-positive relaxed cube-sphere frame;
  no projected-coordinate approximation is used.
- Geographic bounds: 27.9994444437 to 29.0005555559 degrees north and
  -81.0005555561 to -79.9994444439 degrees east longitude (about 98 x 111 km
  at the launch-site latitude).
- SHA-256:
  `532ab3a4ade336d9a7d266e6745a12f043db928ba8bf28a4576886de421a74cd`.
- USGS-authored data are public-domain U.S. Government data. They are not a
  navigation product and use does not imply USGS endorsement.

`tools/earth_data/acquire_florida_m12.ps1` acquires and verifies the ignored
source TIFF. `tools/earth_data/build_local_terrain_pack.py` then builds
`earth_florida_m12.nccube`; its tracked identity is
`assets/terrain/manifests/earth-florida-m12.json`:

- terrain version 5, regional payload format version 3;
- one contiguous relaxed cube-sphere L8-L11 hierarchy, 859 records;
- BC7 geographic albedo, R16_UNORM physical residual, BC5 diagnostic regional
  normal, and R8 categorical control;
- global-to-regional residual feather over the outer 4 km source boundary;
- byte size 210,307,596;
- SHA-256
  `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`;
- maximum measured R16 quantization error 0.001961 m, RMS 0.000672 m.

The control map is independently generated from the lawful USGS elevation,
coastal/geographic rules, and NovaCore deterministic classification. It is not
KSA data and is not claimed to be a surveyed land-cover product.

The geographic convention is right-handed: +Y north, +X at longitude zero,
and east-positive longitude toward -Z. Offline source sampling converts the
EPSG:4326 raster into canonical face-aligned records and cross-face gutters.

Generated payloads are installed through `NovaCore.AssetTool` into the ignored
content-addressed runtime cache. Normal runtime performs no implicit download.
NumPy 2.3.5, Pillow 11.3.0, Rasterio, and PyProj are the offline Python
dependencies for the regional build. Generated packages and the source TIFF
remain in ignored content-addressed/source caches, not ordinary Git history.
