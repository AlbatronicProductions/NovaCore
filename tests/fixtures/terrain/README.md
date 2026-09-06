# Bounded terrain regression fixtures

`tiny-global` remains the generic NCCUBE1 parser/asset-distribution fixture.
`tiny-local` now uses NCCUBE2 magic, schema 3, payload 3 and terrain generation 5:
four L4 sectors, 264 x 264 stored texels each, BC7/R16/BC5/R8 channels. Its binary
budget is 1,200,000 bytes. These are synthetic regression inputs, not production
asset replacements or a depiction of Florida.

The authoritative geographic frame is +Y north, +X at longitude zero and
east-positive longitude toward -Z. Thus the fixture's positive-Z face is west.
The generator uses `atan2(-z,x)`; `BodyFixedGeography` and the production relaxed
cube-sphere address/query path use the same convention. The content manifest
names this convention explicitly. There is no dual interpretation switch.

From the repository root, use Python with the pinned dependencies in
`tools/earth_data/requirements.txt` (NumPy 2.3.5; Pillow 11.3.0):

```powershell
python tools/earth_data/build_local_terrain_pack.py --fixture --sector-level 4 --output tests/fixtures/terrain/tiny-local.nccube --content-manifest tests/fixtures/terrain/tiny-local.content.json
python -B tools/earth_data/test_local_terrain_fixture.py
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- '--test=Local terrain format'
```

The fixture path uses synthetic NumPy inputs and the same R16/control encoder,
record digest and writer as production regional generation. It does not load
Pillow images, source DEMs, network inputs or production asset caches. Use a
temporary output path to investigate changes; update the tracked asset manifest's
size/hash only for an intentional reviewed fixture change. The permanent Python
test uses that manifest's generation command and regenerates twice in independent
temporary directories, requiring byte-identical payloads and numerical metadata.
Temporary copies are automatically removed.

Permanent checks protect ordered/sequential parsing, all four channel layouts,
raw and PackBits decoding, current identity/range/control, manifest verification,
first-record native digest corruption rejection, sparse absence, every decoded
R16 texel against an independent C# geographic oracle, physical slope/normal error,
and a decisive wrong-longitude negative comparison. They do not install this
fixture into the process-global production oracle.

Current SHA-256:
`f91d5922c8cd22d1aa200b3800a7bf87aa4fa8ae9f9b18e04008e735289a6f3d`
(1,117,669 bytes). The physical-error methodology and generator parity proof are
in the [fixture migration report](../../../docs/tiny-local-internal-authoring-migration.md).

## Active schema-2 authoring fixture

`local-payload2.nccube` is one schema-2/payload-2/terrain-5 record, 174624 bytes,
with a 192 KiB binary budget. SHA-256:
`19366ff801f81204ca1be18efa466785126a5bd1601fb2b28cc89be1fe749524`.
`LocalTerrainPayload2Tests` consumes it in its own harness child process.

The current real-input generator uses an 8x4 RGB(100,120,140) image, an 8192x4096
little-endian R16 raster filled with 55000 and sector level 3. This is a bounded
synthetic authoring test, not a production asset. Its permanent Python test writes
actual input files, runs the CLI, verifies all 46 record identities and package
hash, and requires the first record to equal this frozen reader fixture. The
approximately 75.13 MB of temporary input/output is removed automatically.

To extract that first record, retain bytes 0 through
`384 + sum(uint32[3] at offset 288)` (exclusive), and set the uint32 record count
at offset 32 to 1. Identity, payload, offset and digest stay unchanged.

```powershell
python -B tools/earth_data/test_local_terrain_payload2.py
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --category=headless '--test=Local terrain'
dotnet run --project tests/NovaCore.Graphics.Tests -c Release -- --category=headless '--test=Local terrain'
```

Use NumPy 2.3.5 and the offline dependencies above. Tests protect the supported
[terrain contract](../../../docs/terrain-assets.md#nccube-support-contract):
BC7/BC4/BC5 raw/PackBits decoding, identity-bound channel digests, current-address
residual queries, manifest lineage/hash/structure, supported payload versions,
and corruption/truncation rejection. The production schema-3 fixture and manifests
remain unchanged.
