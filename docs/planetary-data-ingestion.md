# Planetary scientific-data ingestion

## Authority boundary

Planetary scientific rasters are offline inputs, not runtime authorities. The
offline tool validates provenance, transforms source pixels into NovaCore's
body-fixed geographic page grid, and emits final GPU-ready channel payloads.
Runtime code reads only checked NovaCore packs. It never opens GeoTIFF files,
downloads data, reprojects pixels, or recompresses textures.

Every source manifest records the publisher, product and date/version, stable
catalog and service identifiers, license or terms, attribution, source and
export CRS, source resolution, dimensions, geographic bounds, bytes, SHA-256,
retrieval date, color/vertical conventions, nodata handling, and every
transformation. A source may be described as public domain only when its own
authority supports that statement.

## Deterministic pipeline

`tools/planetary_data/ingest_region.py` implements the first bounded regional
pipeline:

1. validate the source manifest, exact source sizes/hashes, geographic area,
   raw-byte budget, and pinned preprocessing versions;
2. reproject each source through Rasterio/GDAL into north-up EPSG:4326 using
   one resampling thread;
3. use Lanczos for sRGB albedo, bilinear for metric elevation, and nearest for
   source validity;
4. sample the existing equirectangular SVT identity (longitude wraps,
   latitude maps north-to-south, polar rows clamp), including the existing
   256-texel interior and two-texel gutter;
5. encode albedo as deterministic opaque BC7 mode 6, elevation as R16 UNORM
   over the unchanged -11,000 to +9,000 metre range, and validity as BC4;
6. write one sparse, level/Y/X-ordered `.ncvreg` pack plus a checked manifest,
   deterministic index, hashes, and a non-runtime preview.

The conservative defaults refuse more than 128 MiB of raw input, 64 MiB of
pack output, 256 pages, or 1,000 square kilometres. Overrides require an
explicit tool change or future reviewed command-line contract; the current
proof cannot silently exceed them. The pinned offline dependencies are NumPy
2.3.5, Rasterio 1.4.3, PyProj 3.7.2, and Pillow 11.3.0.

## Regional pack and fallback contract

`NCREGN1` version 1 is optional content layered over, never substituted for,
the production-v3 global pack. Its fixed header stores body, bounds, levels,
page-count ceiling, hash-table capacity, identity, and three channel
descriptors. Each channel owns its own maximum LOD, sparse `(level, x, y)`
record count, ordered records, and fixed-size GPU-ready payloads. The proof
uses the same 48 identities for all three channels, but the format, loader,
residency tables, and fallback search do not require equal counts or maximum
LODs. Regional and global pages use the same page identity and body-fixed UV
convention.

The runtime reads a prebuilt index once at startup, validates exact pack size,
SHA-256, identity, format descriptors, and ordered page records, then inserts
resident pages into three fixed 512-entry open-addressed channel tables. There
is no per-frame directory scan. Each channel loads in ascending level/Y/X
order, so its bounded lookup resolves requested regional page, regional
parent, then the existing global requested/parent/root hierarchy. A BC4
source-validity sample smoothly gates the regional override at its data
boundary. Missing, malformed, or corrupt regional content leaves the global
hierarchy fully operational.

The proof reserves slots 80–127 in the existing 128-layer BC7, R16, and BC4
arrays. It creates no new texture array and does not increase their
34,611,200-byte allocation. At most two pages (six channel payloads, 473,200
bytes) are scheduled per frame through the existing persistent staging buffer.

## Mount St. Helens proof

The bounded region is Mount St. Helens and Spirit Lake, Washington:
`[-122.30, 46.10]` to `[-122.10, 46.30]`, approximately 15.41 by 22.26 km and
343 km². Albedo comes from the USGS/USDA NAIP Washington 2017 mosaic; elevation
comes from the USGS 3DEP bare-earth dynamic service. Both are U.S. Government
public-domain products; attribution is `USGS, USDA, The National Map` for NAIP
and `USGS National Map 3DEP` for elevation. Exact service queries, dates,
source products, terms, CRS, hashes, and acquisition metadata live in
`assets/earth/source/regional/mount_st_helens/source.json`.

The service exports total 30,677,362 bytes and intentionally target about ten
ground metres. The pack contains 48 sparse pages from L5 through L12. At
46.2° latitude, L12 is about 13.23 metres per texel. It is 11,359,360 bytes:
3,244,800 bytes BC7 albedo, 6,489,600 bytes R16 elevation, 1,622,400 bytes BC4
validity, plus header/page records. Pack SHA-256 is
`9f66aa63963ce503fc871eed03d5626b42548dec24c8222221f8c273b6c27b00`.
BC7 albedo measures 41.265 dB PSNR. R16 elevation has 0.153 m maximum and
0.083 m RMS quantization error with no clipped samples.

3DEP heights are retained as NAVD 88 metres; NovaCore does not hide that datum
behind an undocumented offset. This is a bounded presentation proof, not a
navigation-grade vertical-datum unification. Horizontal registration is the
shared body-fixed EPSG:4326 page identity. Unique scientific information ends
near 13 m/texel; future local material detail may begin around 50–100 m camera
altitude and transition through metre/centimetre visual frequencies, but it
must not move coastlines, terrain peaks, source elevation, or page geography.

Linear area extrapolation is deliberately cautionary: 100 times this area is
about 1.136 GB, 10 million km² about 331 GB, all land about 4.93 TB, and the
whole Earth about 16.89 TB. These figures demonstrate why regional content is
optional and bounded; they are not proposed distribution sizes.
