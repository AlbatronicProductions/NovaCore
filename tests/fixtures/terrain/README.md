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
(1,117,669 bytes). The historical 697,539-byte schema-2/payload-1/terrain-4 fixture
and its BC4 measurements are documented in the
[migration report](../../../docs/tiny-local-internal-authoring-migration.md).
Public schema-2 reader support is unchanged.
