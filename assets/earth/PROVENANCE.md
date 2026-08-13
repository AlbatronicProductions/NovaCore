# NovaCore Earth presentation data provenance

NovaCore's Earth runtime dataset is a deterministic derivative of public U.S.
government scientific data. The original rasters remain logically separate in
the preprocessor and the runtime pack preserves separate albedo, land-mask,
elevation, and cloud channels.

## True-color albedo

- NASA Earth Observatory / Visible Earth, *Blue Marble: Next Generation*,
  December 2004, cloud-free true color, 21600 × 10800, EPSG:4326.
- Source file: `world.200412.3x21600x10800.png`, retrieved 2026-08-12.
- Source: <https://eoimages.gsfc.nasa.gov/images/imagerecords/74000/74218/world.200412.3x21600x10800.png>
- SHA-256: `4ee45a0a18229e5667b3523088567e11ea4d857ceac8d7a2d7b6130d5376c5a6`.
- NASA makes the collection free and open to use. NASA media guidance applies;
  NovaCore does not imply NASA endorsement.

The runtime albedo is deterministically Lanczos-resampled without geographic
reprojection to registered 2:1 equirectangular levels through 16384 × 8192.
Longitude wraps, polar rows clamp, each 256-pixel interior receives a two-texel
geographic gutter, and the result is encoded as opaque BC7 mode-6 sRGB. The
superseded 5400 × 2700 JPEG remains checked only as prior-source provenance; it
is not used by the current runtime pack.

## Elevation and land/ocean classification

- NOAA National Centers for Environmental Information, *ETOPO 2022 15
  Arc-Second Global Relief Model*, ice-surface variant distributed at 60
  arc-seconds, EPSG:4326.
- DOI: <https://doi.org/10.25921/fd45-gt74>
- Source file: `ETOPO_2022_v1_60s_N90W180_surface.tif`.
- The dataset metadata states that it is not subject to U.S. copyright
  protection. It is not suitable for navigation. NovaCore derives its binary
  land/ocean mask from the zero-metre elevation contour.

## Clouds

- NASA Visible Earth, *Blue Marble: Clouds*, global 2048 × 1024 cloud layer.
- Source file: `cloud_combined_2048.jpg`.
- Source: <https://eoimages.gsfc.nasa.gov/images/imagerecords/57000/57747/cloud_combined_2048.jpg>
- NASA media guidance applies; NovaCore does not imply NASA endorsement.

## Reproduction

Run `tools/earth_data/upgrade_earth_pack_v3.py` with the checked v3 pack as its
preserved-channel base and the authoritative 21600 × 10800 albedo source to
replace only the global albedo section. The elevation, land-mask, and cloud
sections are copied byte-for-byte and their section hashes are recorded in the
manifest. The legacy v2-to-v3 mode remains available for complete historical
reproduction. The upgrade tool requires NumPy 2.3.5 as pinned in
`tools/earth_data/requirements.txt`; NumPy is BSD-3-Clause licensed and is an
offline build dependency only. The repository-authored converter implements
its own deterministic BC1 quality candidate, BC4, and opaque BC7 mode-6
encoders. No third-party texture encoder or decoder is shipped or loaded at
runtime.

The checked v3 manifest records source and output SHA-256 values, deterministic
identity, tile geometry, channel semantic, GPU format, color space, maximum
useful level, section offset, measured compression quality, and output size.
Source paths never participate in identity. Production format v3 retains
256 × 256 interiors with a two-texel geographic gutter on each side; longitude
wraps and polar rows clamp deterministically. The 260 × 260 physical extent is
already divisible by the BC 4 × 4 block size, so no geographic padding or page
identity changes are required. R16 elevation bytes are copied verbatim from the
validated intermediate.

## Mount St. Helens bounded regional proof

The optional regional proof uses bounded exports from the official USGS NAIP
and 3DEP ImageServer services. Its complete product names, acquisition and
service dates, public-domain terms, attribution, export queries, CRS, vertical
datum, dimensions, byte counts, and SHA-256 values are checked in
`source/regional/mount_st_helens/source.json`. The source GeoTIFFs total
30,677,362 bytes. `tools/planetary_data/ingest_region.py` produces the optional
11,359,360-byte `mount_st_helens_v1.ncvreg` pack; it does not alter the global
Earth pack or the CPU elevation oracle. See `docs/planetary-data-ingestion.md`
for the regional format, fallback, budgets, and measured proof limits.
