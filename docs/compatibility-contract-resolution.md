# Compatibility contract resolution — after banked Package 1

**PARTIAL RESOLUTION — PROJECT CONTROL DECISION REQUIRED.**

The primary result is the [complete 25-row contract matrix](engineering-evidence/compatibility-contract-resolution/contract-matrix.csv).
It includes the requested producer, consumer, production, fixture, public exposure,
replacement, migration, classification, confidence and retirement-condition columns.
No compatibility code, generator, fixture, production asset or runtime behavior was
changed. This package records the read-only contract resolution and proposed
migrations. It does not enact a new support policy.

## Baseline

Verified initially clean `main`. HEAD, local `origin/main` and directly queried
remote main were `742cd8953045343310c809bec4067baf6586f2fa`,
`NovaCore repo: retire proven dead architecture debt`. Package 1 is banked.
The annotated `m12d-p2s5h-earth-route-convergence` tag still resolves to
`32ffac50ab5c06518ede24edfb5c531976d4ec99`. No ref, index or history change.
The overall repository-debt front remains open.

## Resolved format boundaries

| Responsibility | Current authority | Disposition |
|---|---|---|
| Self-describing ephemeris storage/reconstruction | NCPE v2 | KEEP current builder, codec, frozen semantic hash and byte-only reconstruction. Normal Solar uses its authored compact model; no current `.ncpe` files were found in this workspace. |
| Offline NCPE v1 read/write | Public legacy codec; only tests are known current callers | UNKNOWN external contract. Current docs explicitly promise legacy readability. Runtime rejects v1; do not remove the codec by conflating it with the rejection discriminator. |
| Global Earth pack | NCCUBE1 magic, schema 1, terrain generation 5 | KEEP. It is the required global production format. |
| Regional Florida pack | NCCUBE2 magic, schema 3, payload 3, terrain generation 5 | KEEP. R16 residual, per-record ranges, R8 physical control and record digests feed current physical preparation. |
| Non-regional generator output | NCCUBE2 schema 2, payload 2, terrain generation 5 | KEEP current generator/reader workflow until deliberately migrated. The producer still exists, so the ticket’s removal conditions are not satisfied. |
| Historical local lineage | NCCUBE2 schema 2, payload 1, terrain generation 4 | Historical production use has ended; fixture/public readability boundary remains unresolved. Migrate fixtures, resolve supported external data, then retire only the obsolete branch. |

The numbered suffix in `NCCUBE1`/`NCCUBE2`, binary header schema, per-record
payload revision, terrain-data generation and `NovaCore.TerrainAsset/1` manifest
schema are different contracts. There is no separately implemented `NCCUBE3`
magic. A low version number or a permissive parser check is not proof of either
obsolescence or an intentionally supported artifact combination.

Native generic `Pack::Open` recognizes local schemas 2/3 and payload identities
1/2/3. Current production paths additionally require `IsProductionLayout()`:
body 6, terrain 5, current extent/detail frequency and payload 3. Both runtime
startup and GPU physical preparation enforce that boundary. Public generic
verification does not make an old pack authoritative NCSM1 geometry.

The physical inventory found 11 `.nccube` files: four schema-3/payload-3 packs;
one schema-2/payload-2 pack; two schema-2/payload-1 packs including `tiny-local`;
four global schema-1 packs including `tiny-global`. These are format/header
observations, not cache-retention decisions. Junctions were excluded. No NCPE,
NUPKG or SNUPKG files were found in the scanned workspace outside `.git`.
This does not assert absence elsewhere on the computer or on external feeds.

Both current production packs were read and SHA-256 checked against manifests:

- Global: 61,484,224 bytes, `38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae`.
- Florida: 210,307,596 bytes, `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.

## Fixture migrations and invariant ownership

**NCPE v1:** current tests build synthetic data in memory. There is no installed
artifact or v1 CLI producer. Preserve round-trip, digest rejection, duplicate-body
rejection and shared-hash invariants in current-format tests. However, v1 sorts
bodies before serialization; v2 writes body/source declaration order and includes
that order in the frozen runtime-compatible hash. Reversing v1 bodies currently
preserves artifact identity. Do not assert that the same transformation preserves
v2 identity, silently sort v2, or delete the assertion without deciding which
authoring invariant is intended. The v2 fixtures already cover reconstruction,
three-way hash equality, defensive copy and several malformed inputs.

A general v1-to-v2 converter cannot reconstruct missing semantic authority merely
from a version-one payload. Explicit time mapping, complete body identity/physical
catalog, sources/bindings and provenance must be supplied by an authoring source.
The current v2 record definitions also have no named per-body position/velocity
error-bound fields, despite broad wording in `ephemeris-dataset-format.md`;
v1 does. Preserve those measurements/provenance deliberately if migrating actual
v1 data. A current supported artifact and metadata policy must precede conversion.
No migration utility was found, and no converter was invented here.

**Local fixture:** the 697,539-byte tracked `tiny-local` file is schema 2,
payload 1, terrain 4. Its metadata labels the generator with an east-positive
v2 name, but current `--fixture --sector-level 4` generation writes payload 2,
terrain 5. Header inspection and current generator constants already prove the
recorded recipe cannot reproduce the exact old identity. Commit `0a6e384` also
changed longitude mapping from positive-Z east to negative-Z east. Rewriting
header version bytes is not a valid migration. The fixture’s original generator
at `11bccb2` supplied payload 1/terrain 4; history is provenance, not a current
production consumer.

The local fixture protects bounded/sequential parsing, deterministic identity
order, raw/PackBits transcode, native digest corruption rejection, geographic
absence and explicit BC4 error accounting. These invariants can survive a new
bounded fixture. The current generator has no schema-3 synthetic fixture path:
that requires a bounded R16/control fixture recipe, or a deliberate intermediate
payload-2 workflow migration. Preserve R16 quantization/range/control checks,
per-channel digests and sparse absence in the new fixture. Retain historical BC4
measurements as history; do not retain a legacy reader solely to satisfy them.
The BC4 block encoder also creates BC5 normal components, so it cannot be removed
wholesale with the old elevation representation.

**Authoring fixtures:** all 22 direct calls to pre-catalog overloads are in
`tests/NovaCore.Simulation.Tests/Program.cs`: ten six-argument and twelve
ten-argument calls. They cover hierarchy/error/copy isolation, exact time mapping,
metadata/hash sensitivity, typed source/payload failures and reserved-model
rejection. One `CheckSystem` wrapper drives multiple malformed cases.
Convert those inputs to explicit `CelestialBodyCatalogEntry` arrays, named sources
and payloads; preserve input order, expected validation failures and hashes.
Read copied parent identity through the catalog instead of `LegacyDefinition`.
Do not move automatic inference into another permanent compatibility helper.

`CelestialSystemFixtures` already creates explicit catalogs. Its SolMini,
GeocentricDemo, BinaryDemo and SampledDemo are current independent oracles.
`CelestialBodyDefinition` still serves `CelestialStateStore` and mechanical
simulation; removal of its use as legacy authoring input must not remove that
shared mechanical type.

## Public/external contract resolution

The [GitHub repository](https://github.com/AlbatronicProductions/NovaCore) is public.
A live GitHub releases API query returned an empty list. No package-publication
configuration/history was found in inspected project files, `.github`, nuspec or
NuGet configuration paths. Exact NuGet flat-container checks for
`NovaCore.Core`, `NovaCore.Simulation`, `NovaCore.Graphics` and
`NovaCore.EphemerisFormat` returned 404. Those observations do **not** disprove
private feeds, alternate package IDs, copied DLLs, source consumers or downloads
of public tags. Zero observed forks is not a consumer census.

`CameraMath` and `RelativePosition` are public and existed in source at `v0.5.0`.
No current internal/test/sample caller was found. Direct quaternion rotation
replaces the camera-axis wrappers with the same convention; the meaning of a
consumer’s `RelativePosition` must be understood before selecting a frame-aware
replacement. Both remain UNKNOWN external-consumer risk, not permanently kept.

NCPE codec types and managed terrain format/loader APIs are public assembly
surface. In contrast, `CelestialSystemDefinition`, `CelestialHierarchyNode`,
`CelestialBodyDefinition` and `NcpeCelestialSystemLoader` are internal types.
Methods declared `public` inside an internal type do not create an ordinary public
C# API. Simulation grants friend access to its tests, Triangle, Graphics tests,
EphemerisBuilder tests, NAIF adapter tests and the NAIF adapter. Those consumers
were included in searches. No static import/reflection dispatch to the legacy
TryCreate methods was found. Public exposure and intentional supported compatibility
must be recorded separately.

The earlier phrase “separately published v1 codec” in `ephemeris-builder.md` is
only proven as public source/assembly exposure here, not a NuGet or binary release.
Project Control should resolve the support promise before wording implies a
published-package obligation or removes documented legacy readability.

## KSA source and engineering-history comparison

Installed `E:/Kitten Space Agency/KSA.dll`:
`2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
This matches the current decompiled-reference provenance. Relevant current local
types were reread; this version string is not an inferred calendar date.

| Revision / observed publication | Old contract and reason | Replacement / current source | NovaCore decision |
|---|---|---|---|
| [4885, July 10, 2026](https://discord.com/channels/1260011486735241329/1260112103134724146/1525038373109235767) | Saved-game/vehicle break explicitly announced alongside richer reactions, propellant changes and a dead tank-field removal. | Replacement example vehicles were supplied; current `Reaction`/`ReactionReference` and `PartTemplate` reflect the newer model. | **ADAPT:** make the supported break and replacement artifacts explicit. KSA’s break does not prove NovaCore external consumers absent. |
| [Version 2026.7.8.4980, posted July 22](https://discord.com/channels/1260011486735241329/1406840455093682258/1529404426106699921) | Partial tree serialization and implicit derived-data rebuilding caused state loss/disagreement. | `PartTree.Serialize()` stores root, sequence environments and fuel links; `VehicleSaveData` uses that contract; save metadata records current version. | **ADAPT:** migrate complete semantics into one explicit authority, then retire old input adapters. A version field alone is not a compatibility guarantee. |
| Same version-history post | Texture quality settings could change physical height sampling. | Current `TextureCategory.TerrainHeight` and `GameSettings.GetMaxTextureSize` exempt physical-height inputs from the terrain cap. | **ADAPT existing accepted boundary:** a format migration must preserve physical authority and CPU/GPU agreement. No quality or rendering change follows from this ticket. |
| [Version 2026.7.10.5056, posted July 28](https://discord.com/channels/1260011486735241329/1406840455093682258/1531539522112716962) | Separate authoring conversion tooling drifted from game asset structure. | History records moving GlbToXmlUtility alongside shared serializers and aligning emitted paths. Current installed serialization authority was inspected; no claim of reconstructing that tool’s entire history. | **ADAPT:** fixture producers and consumers must migrate together. This directly informs the measured `tiny-local` recipe mismatch. |

Official live-changelog and version-history were searched read-only after sign-in.
The table uses exact message dates and version/revision identities, rather than
reusing the earlier audit’s abbreviated July chronology. No equivalent KSA NCPE
codec contract, NCCUBE schema contract or permanent test-fixture migration policy
was proven. Those decisions therefore remain NovaCore-specific requirements;
KSA does not authorize speculative compatibility deletion. No proprietary source,
assets or substantial excerpts were copied into this package.

## Retirement candidate and exact review decisions

| Candidate | Proof / replacement | Work required before removal |
|---|---|---|
| AUTH-03 sampled pre-catalog overload | No current call site; internal; explicit catalog + samples overload is already used. | Small standalone removal and affected compilation/Simulation/loader checks. No fixture migration required. Proposed here; no removal performed. |
| AUTH-01/02/04 authoring adapter island | Only the identified focused tests consume implicit authoring. Current explicit catalog is authoritative. | Convert all inputs and accessor assertions, compare deterministic identities/errors/copy semantics, then retire adapters and legacy node storage together. |
| NCPE v1 codec-only island | No CLI or production asset consumer; public legacy-read promise remains. | Project Control declares supported artifacts/distribution channels and sunset; migrate generic invariants, explicitly decide ordering and missing metadata; retain shared hash/sample and rejection gate. |
| Local payload-1 / eventual schema-2 compatibility | Payload 1 is historical lineage; schema 2/payload 2 still has an active producer. | Resolve supported non-regional workflow and external artifacts, implement current bounded fixture, migrate producer/manifest/tests, then narrow only retired branches. |
| CameraMath / RelativePosition | Public, locally unreferenced; direct current mechanisms exist. | Resolve external source/DLL support and consumer coordinate meaning, then deprecate/migrate or explicitly declare unsupported. |

No fixture migration or compatibility retirement was implemented. This is the
review package requested by the read-only-first gate; classifications are not
deletion authorization. Uncertainty is confined to named decisions, not blanket
permanent retention. The internal authoring migration is independently bounded
and does not depend on the external format decision.

## Evidence and validation boundary

The [evidence index](engineering-evidence/compatibility-contract-resolution/README.md)
records current headers, manifests/hashes, public-publication observations and
source-call inventory. All original tracked files are checked against their
initial SHA-256 values. Only this new documentation/evidence package is added.

No production changes means no new Debug/Release build, runtime, Vulkan or manual
Florida PASS is claimed or required for this report. Read-only asset SHA/header
checks and report consistency/whitespace checks are current. A later implemented
retirement requires the ticket’s Debug/Release, affected format/loader, asset,
fixture, launcher and deterministic/precision checks. Cache policy, Graphics/window
validation, Git/LFS cleanup, performance and accepted P2S5H behavior were not touched.

## Source anchors for reproduction

- NCPE: `src/NovaCore.EphemerisFormat/EphemerisArtifact.cs:9`, `NcpeV2.cs:15`, `NcpeV2RuntimeHash.cs:4`; `src/NovaCore.Simulation/Celestial/NcpeCelestialSystemLoader.cs:12`; `tools/NovaCore.EphemerisBuilder/Program.cs:3`; `tests/NovaCore.EphemerisBuilder.Tests/Program.cs:5`.
- Terrain: `src/NovaCore.Graphics/PlanetaryLocalTerrain.cs:63`, `TerrainAssetDistribution.cs:75`; `native/NovaCore.Native/LocalTerrainPack.cpp:29`, `LocalTerrainPack.h:44`, `RegionalPhysicalResidency.h:34`; `tools/earth_data/build_local_terrain_pack.py:458`, `:490`, `:533`; `tests/NovaCore.Graphics.Tests/Program.cs:294`; current and fixture manifests listed in the matrix.
- Authoring: `src/NovaCore.Simulation/Celestial/CelestialSystemDefinition.cs:43`, `CelestialHierarchyNode.cs:4`, `CelestialSystemFixtures.cs:34`, `Properties/AssemblyInfo.cs`; Simulation tests at `:478`, `:612`, `:632`, `:668`.
- Publication: current project files and `Directory.Build.props`; Git source introduction history and tags; direct GitHub releases and exact-ID NuGet endpoint results in `evidence.json`. Empty results are scope-limited observations.

Nothing staged, committed, pushed, tagged or banked. Stop for Project Control review.
