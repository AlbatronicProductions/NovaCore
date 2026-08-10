# NovaCore Earth presentation data provenance

NovaCore's Earth runtime dataset is a deterministic derivative of public U.S.
government scientific data. The original rasters remain logically separate in
the preprocessor and the runtime pack preserves separate albedo, land-mask,
elevation, and cloud channels.

## True-color albedo

- NASA Earth Observatory / Visible Earth, *Blue Marble: Next Generation*,
  December 2004, cloud-free true color, 5400 × 2700, EPSG:4326.
- Source file: `world.200412.3x5400x2700.jpg`.
- Source: <https://eoimages.gsfc.nasa.gov/images/imagerecords/74000/74218/world.200412.3x5400x2700.jpg>
- NASA makes the collection free and open to use. NASA media guidance applies;
  NovaCore does not imply NASA endorsement.

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

Run `tools/earth_data/build_earth_dataset.py` with the three original inputs to
produce the registered production-v2 intermediate, then run
`tools/earth_data/upgrade_earth_pack_v3.py` to produce the checked GPU-native
pack. The upgrade tool requires NumPy 2.3.5 as pinned in
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
