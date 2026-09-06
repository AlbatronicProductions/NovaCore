# Tiny-local fixture and internal authoring migration

**READY TO BANK INTERNAL COMPATIBILITY MIGRATION** — unbanked implementation
candidate for Project Control review. Repository Debt Retirement remains open.

## Baseline and scope

Initially clean `main`; HEAD, local `origin/main` and directly queried remote main
all `4cd59634ee0c6327b5d573dc375f493f7d61570a`. Package 1
`742cd8953045343310c809bec4067baf6586f2fa` and the banked compatibility report/matrix
were present. The audit's fixture and internal-adapter findings were rechecked,
not audited again repository-wide. P2S5H remains
`32ffac50ab5c06518ede24edfb5c531976d4ec99`, tagged
`m12d-p2s5h-earth-route-convergence`; no ref or index operation.

The only runtime-source edits delete the proven pre-catalog adapter island in
`CelestialSystemDefinition` and `CelestialHierarchyNode`. The immutable catalog,
validator, hash, evaluation core, mechanical body definitions, NCPE v1, public
camera/position APIs and all terrain readers are unchanged. No shader, physical
authority, residency, facility, launcher, cache-policy or manifest for production
assets is changed.

## Tiny-local invariant and intentional migration

| Responsibility | Historical fixture | Current candidate |
|---|---|---|
| Format | NCCUBE2 magic, schema 2, payload 1, terrain 4 | NCCUBE2 magic, schema 3, payload 3, terrain 5 |
| Physical channels | BC7/BC4/BC5, package residual range | BC7/R16/BC5/R8, digest-bound per-record range/control |
| Geography | Old positive-Z-east encoding, despite later metadata label | Canonical east-positive toward negative Z |
| Scope | Four positive-Z L4 sectors | Same four ordered sector addresses and 264 x 264 extent; now correctly interpreted as west |
| Binary size | 697,539 bytes | 1,117,669 bytes; 1,200,000-byte permanent budget |
| Height quality | BC4 lossy residual | Production R16 quantization, approximately 1/256 metre steps |

Old SHA-256:
`6fea3a62833e8aa6beffcbd845697baaca2a8db281eab2ed900d53b2657cf053`.
New SHA-256:
`f91d5922c8cd22d1aa200b3800a7bf87aa4fa8ae9f9b18e04008e735289a6f3d`.

Before migration the current generator's `--fixture` branch still emitted
schema 2/payload 2/terrain 5. History at `0a6e384` changed longitude sign and
payload/terrain identity without regenerating the tracked fixture. This candidate
adds bounded synthetic input to the current schema-3 writer, rather than editing
payload headers or adding a second reader interpretation. Non-regional real-input
generation still emits schema 2/payload 2/terrain 5; that public/uncertain workflow
is intentionally untouched.

`build_fixture` supplies synthetic tile fields to the extracted
`write_regional_pack`; `build_florida` supplies its unchanged real-source tile
fields to the same writer. Encoding operations, order, physical ranges, channels,
record digests and Florida metadata are unchanged. A bounded comparison ran the
banked and candidate `build_florida` functions with identical four-sector synthetic
source providers: complete binary and content JSON were identical. This is writer
parity evidence, not a claim of regenerating the full production DEM pack.

Recipe identity: `tiny-local-r16-control-v1` in
`tools/earth_data/build_local_terrain_pack.py`; generator working-file SHA-256:
`934f0ee6b5c0a651830a86463e722fe8ab5e3c24b94cd043e605d749d6033593`
(Windows working-file newline form). See the
[permanent fixture recipe](../tests/fixtures/terrain/README.md). The regression
ran with Python 3.12, NumPy 2.3.5 and import-only Pillow 12.3.0 from the available
local runtime. The repository's real-image recipe still pins Pillow 11.3.0;
synthetic fixture bytes do not call Pillow. No dependency pins were changed.

Two independent generation paths reproduced both the entire binary and every
content-manifest value. Public manifest generation arguments reproduce the binary;
`--content-manifest` additionally reproduces the numerical report.

### Geography and sample authority

The trace is generator `tile_data`/`regional_tile_data` (`atan2(-dz,dx)`) →
terrain-v5 sector identity and named content convention → managed/native record
loaders (address identity is preserved, no geographic relabeling) →
`RelaxedCubeSphereProjection.TryAddress` and `EarthLocalTerrainElevationDataset`
sampling. `BodyFixedGeography` independently defines forward direction
`(cos(lat)cos(lon), sin(lat), -cos(lat)sin(lon))` and inverse `atan2(-z,x)`.
`docs/planetary-rendering.md` already states the same convention.

Permanent Python cardinal tests check both east and west. Permanent Graphics tests
derive each stored texel direction with the production projection and derive its
latitude/longitude through Core, then independently evaluate the synthetic height
function against the actual decoded R16 bytes. For all 278,784 samples:

- maximum height error: **0.001961682224806083 m**;
- RMS height error: **0.0011270416053056442 m**;
- maximum central-difference slope error: **3.994816183666011e-7**;
- maximum physical-normal vector error: **5.433658708365317e-7**;
- maximum mismatch with mirrored longitude: **49.88018319105478 m**.

Tests also check every R8 control texel against geographic class selection,
record ordering, exact file coverage, four-channel lengths, residual ranges,
manifest identity, both storage codecs, malformed PackBits, first-record native
corruption rejection for each channel/range, unsupported header rejection, and
manifest rejection of truncation. BC5 remains a material-normal channel; the
reported physical-normal measurement comes from decoded physical heights.

Historical BC4 measurements remain provenance: maximum/RMS vertical error
6.023490905761719 / 2.143428934302063 m; maximum/RMS slope error
0.0021931789815425873 / 0.00030037846109700917; maximum/RMS normal error
0.31775182485580444 / 0.21945029396953147 degrees. These observations are not
rewritten as incorrect, nor made the current R16 acceptance thresholds. The banked
audit/matrix and Git preserve the old artifact identity without another binary copy.

## Internal compatibility retirement

| Adapter/responsibility | Prior purpose and former consumers | Current consumer/replacement | Decision / confidence |
|---|---|---|---|
| AUTH-01 six-argument pre-catalog `TryCreate` | Inferred catalogs, sources, payloads and central mu; ten direct Simulation test sites, one driving malformed cases | All sites now supply literal catalogs, bindings, sources and payloads | RETIRE; high |
| AUTH-02 ten-argument pre-catalog `TryCreate` | Inferred catalog from legacy nodes; twelve typed-catalog Simulation test sites | Explicit catalog overload; same typed sources/payloads | RETIRE; high |
| AUTH-03 twelve-argument pre-catalog sampled overload | Inferred catalog for samples; zero production/test/tool/sample callers before and after migration | Existing explicit sampled overload and SampledDemo/NCPE loader | RETIRE; high |
| AUTH-04 node constructors, `_legacyDefinition`, `LegacyDefinition`, `ParentId` | Stored duplicate body authority for the above tests and adapter extraction | Node stores only body ID/binding; parentage/copy assertions read the catalog | RETIRE; high |
| `BuildCompatibilityCatalog` | Synthesized names/physical properties from legacy nodes | No callers after the above conversion; explicit inputs own identity | RETIRE; high |
| `CelestialBodyDefinition` | Shared mechanical state input to `CelestialStateStore` and dynamics | Still current mechanical production/test responsibility | KEEP; high |
| Current catalog overloads / fixture oracles | Explicit immutable catalog authority and independent SolMini, GeocentricDemo, BinaryDemo, SampledDemo oracles | Production Solar, NCPE loader, friend tests/tools/samples remain | KEEP; high |

The authoring fixture tests are a separate internal responsibility from the terrain
file; they did not literally consume `tiny-local`. Both were the bounded internal
migrations authorized by this ticket. Searches included production, tests, tools,
samples, friend assemblies, reflection/dynamic and interop references. Enclosing
types are internal and the retired members have no reflection/ABI contract found.
Solution and tool builds provide an additional static consumer check; no claim is
made that repository search resolves the excluded public contracts.

The former synthesized names are now literal authored strings in the tests to
preserve their exact semantic identity. No new inference helper replaces the old
adapter. Existing validation failures remain covered; caller-copy tests now mutate
catalog, binding, source and each typed payload input and verify published identity.
Pre-migration versus candidate hashes, now frozen as assertions:

- hierarchy: `AB869A12B4D3734A`;
- exact time/metadata fixture: `52818656D0A49113`;
- typed ephemeris catalog: `393A2730EA3A1BF5`.

Sol production definition remains `493FE8B1E867110F`; SampledDemo evaluation remains
`79D206F8D15C2775`. All 44 printed Simulation hexadecimal hashes match the baseline.

## KSA comparison

**ADAPT:** migrate producer/consumer together and carry complete semantics through
one explicit authority. Current local `PartTree.Serialize` and its
`VehicleSaveData` consumer were reread. Installed KSA version remains
`2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`; DLL SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f` matches the
banked source provenance. The previously verified official history is retained in
the [banked audit](compatibility-contract-resolution.md#ksa-source-and-engineering-history-comparison),
including complete serialization and converter/serializer convergence. It was not
searched again for an unrelated format analogy. No equivalent KSA longitude or
NCCUBE fixture contract is claimed and no proprietary code/content is copied.

## Validation and runtime boundary

| Check | Result |
|---|---|
| Solution Debug / Release builds | PASS; both zero warnings/errors |
| Simulation full suite, Debug / Release | PASS; baseline hashes preserved and new fixed assertions pass |
| Three permanent Python generator tests | PASS; two exact regenerations, cardinal convention, bounded-input rejection |
| Local terrain parser/codec/physical oracle, Debug / Release | PASS |
| Graphics Debug geographic handedness, asset distribution, celestial publication, production relaxed hierarchy | PASS |
| EphemerisBuilder full Release tests | PASS; NCPE v1 retained; v2 reconstruction/hash/loader tested |
| NAIF adapter full Release tests | PASS; offline/current authority integration |
| Launcher Release tests | PASS; all 15, including Florida and existing production mappings |
| Current global/Florida asset verification | PASS; 61,484,224 / 210,307,596 bytes; manifest hashes unchanged |
| Release deployed shaders | PASS; all 49 match `build/native-ninja-release/shaders` |
| Release Simulation deployment | PASS; sample copy equals current built assembly |
| Bounded Solar/Earth runtime | PASS; 60 frames at 700 km, expected assets/oracle loaded, one visible owner; NCSM1 after bootstrap; empty stderr and no validation-error messages observed |
| Whitespace / scope / index / P2S5H checks | PASS |

Smoke command (a bounded authoring/runtime check, not another Florida campaign):

```powershell
samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe --scene=sol --focus=earth --altitude=700000 --physical-surface=m12d-natural-candidate --benchmark-frames=60 --log=startup,validation,vulkan
```

The initial global root presentation was legitimate bootstrap, followed by NCSM1;
no alternate production owner was introduced. This smoke is not new manual
Florida acceptance and makes no performance claim. Current Release Simulation DLL
SHA-256: `d9c1314e882cd09caeb3580c2f3cd67deb01672513b762c34bab55f8ee1762cd`.

### Deferred observation

`nc_validate_terrain_asset` opens the local directory and reads/verifies only its
first payload. A final-record truncation therefore passed that limited probe in
an exploratory test. The unchanged manifest size boundary rejects it before
runtime publication; `Pack::Read` rejects incomplete payload reads. The permanent
test asserts the actual manifest responsibility, rather than claiming the native
probe is a full-file integrity verifier. **INVESTIGATE** widening or documenting
that probe in a later bounded validation ticket; current consumers are asset
diagnostics/Graphics regression. The public schema-2 reader is not altered here.

## Storage and review boundary

No bulk captures, DEM generation or asset-cache copies. Four temporary binary
comparisons created 4,470,676 bytes total (two reproductions and two old/new writer
outputs), plus small JSON reports, all removed automatically. Permanent replacement
fixture plus its two JSON manifests totals 1,120,044 bytes. The binary grows by
420,130 bytes in exchange for covering the actual production physical layout.
Permanent report/recipe/test source is bounded below 32 KiB. Sixteen exact
operational script/log/bytecode files totaling 214,095 bytes were verified untracked
and removed after their conclusions were recorded. Temporary JSON comparison
metadata was also removed automatically. Diagnostic output created was approximately
4.7 MB, including those temporary binaries, metadata and operational files;
none remains disposable after this closeout. Normal build/deployment outputs remain
for current development.

No unresolved public compatibility decision was made. No staging, committing,
pushing, tagging, history maintenance, cache cleanup or unrelated retirement.
Stop for Project Control review of this candidate.
