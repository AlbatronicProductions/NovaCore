# Repository debt retirement — post-P2S5H review

**PARTIAL RETIREMENT — FOLLOW-UP REQUIRED.** This is the unbanked safe subset of
the debt front. Proven dead responsibilities were removed. Compatibility/API,
fixture-migration and storage decisions remain explicit follow-ups, not permanent
debt acceptance. P2S5H architecture, physical authority and performance policy
were not reopened.

## Verified baseline

Initially clean `main`; HEAD, local `origin/main` and the remote main ref all
resolved to `c78b5824e7f99570d696dc4607944964bccf006e`. The annotated
`m12d-p2s5h-earth-route-convergence` tag object is
`5d7efb2f035ccf22700b03380b4193fc82220f8a`, targeting the accepted production
commit `32ffac50ab5c06518ede24edfb5c531976d4ec99`. The only later commit is
`NovaCore repo: retire tracked diagnostic video evidence`. No unbanked
Earth-route implementation remained. These refs were read, not changed.

Starting workspace: **6,809 files; 4,566,325,481 logical bytes; 4,578,649,088
allocated bytes**. Starting `build/`: **63,888,216 allocated bytes**. Windows
FileStandardInfo allocation was measured without following six junctions.
Directory metadata/alternate streams are excluded consistently with prior accounting.

## Classification and evidence

Retention requires an explicit current production, supported compatibility,
regression, reproducibility, development or historical/provenance responsibility.
Old code and old tests cannot justify each other. The authoritative diagnostic
policy remains [ENGINEERING_RULES.md](../ENGINEERING_RULES.md#diagnostic-evidence-lifecycle).

A: required production; B: intentional compatibility; C: permanent regression;
D: reproducibility/generator/source; E: active development; F: historical/provenance;
G: proven dead; H: duplicate responsibility; I: stale API/format; J: continuing
diagnostic; K: resolved debris; L: cache/storage; M: Git/LFS; N: insufficient proof.
A–F/J are justified KEEP; G/K RETIRE; H converges on the surviving authority;
I/L/M/N require the explicit decision below. N is temporary.

The [bounded evidence package](engineering-evidence/repository-debt-retirement/README.md)
contains file coverage, artifact/header/hash inventories, exact removal receipts,
Git reachability accounting and validation logs. Reference-count searches are
leads, not deletion proof: same-file calls, inferred types, reflection and exports
must be considered.

## Retirement packages implemented

| Package | Former responsibility and absence proof | Surviving authority / permanent protection | Changes |
|---|---|---|---|
| G: rejected T3 tessellation study | `PlanetaryTerrainTessellationStudy` was explicitly rejected; only its historical material-test block called it. No scene, shader, ABI or generator used it. | Accepted bounded late GPU tessellation; current material checks and pipeline assertion remain. Historical ceiling: 261,632 × 8² = 16,744,448 triangles, up to 64× amplification without physical-height benefit. | Remove class from `PlanetaryTerrainMaterialSynthesis.cs`, its self-test block and historical log fields; rename retained material regression. |
| G/H: managed local-demand/LRU model | Planner, input/result, slot state/token/statistics and cache types were used only by their own test block. No runtime/ABI or independent reference oracle consumed that policy. | Native `RegionalPhysicalResidency.h/.inl`, regional demand shaders and staged preparation own NCSM1 physical dependencies/readiness. Regional tests and live probe remain. | Remove trailing model island from `PlanetaryLocalTerrain.cs` and its obsolete test block. Preserve parser, CPU oracle, codec, corruption and geographic-absence checks. |
| G/H: two private BC4 decoders | Anonymous-namespace `DecodeBc4` in `PlanetaryHeightQuery.cpp` and `PlanetaryMeshPreparation.cpp` had no calls, address uses or exports. | Existing R16 upload and GPU physical height/displaced-mesh parity. Active managed BC4 compatibility is separate. | Remove only these functions; no shared parser, ABI, shader or renderer loop changed. |
| G: synthetic factory | Internal `SyntheticEphemerisDemo.Create()` had no caller; CLI/self-test use `CreateV2()`. | Current NCPE v2 builder and shared sample generation. | Remove unused v1 factory only, not public v1 codec. |
| G: simulation placeholder | Empty `SimulationWorld` class had no repository consumer, fields, serialization or behavior. | Typed simulation/time/celestial/spacecraft stores and deterministic regressions. | Remove `src/NovaCore.Simulation/SimulationWorld.cs`. |
| H: Release solution mapping | Release solution existed without any project mappings: baseline exited 0 with 20 MSB4121 warnings and no project builds. | Existing per-project Release configurations and deployment targets. Debug unchanged. | Add 40 ActiveCfg/Build mappings for all 20 projects; actual Debug/Release builds pass. Native configuration remains explicit. |
| K/L: four redundant cache copies | Exact hash equality, ordinary-file/reparse/Git-ignore checks and no current code/manifest path consumers. Content-addressed originals retained. | Every distinct cache object and both production manifest-selected packs remain. | Remove four aliases/staging copies; 340,934,656 allocated bytes recovered. No new eviction policy. |
| F: documentation normalization | Current docs still called P2S5G current and convergence unbanked; evidence index described the removed video as a working-tree artifact. | Exact P2S5H authority, historical P2S5G measurements and original acceptance chronology. | Normalize README/state/handoff/rendering/architecture, native/Release build prerequisites, future NCPE authoring guidance and historical package status. |

The build-configuration package also corrects the Graphics test project, which
previously copied the Debug native DLL even for Release. It now selects the native
DLL by configuration, matching the sample convention. Relevant native Graphics
checks were rerun against the actual Release DLL. Production deployment is unchanged.

Packages are independent. Removing the old managed model does not implement
another Earth-route migration or modify current native residency.

## Responsibility-island map

Each row traces current entry/consumer → implementation/dependencies →
tests/assets/tooling. The original deferred ledger remains historical below;
this table supersedes its current dispositions. The [structured debt inventory](engineering-evidence/repository-debt-retirement/debt-inventory.csv) supplies stable IDs, classifications, consumers, retention reasons, surviving authorities, actions and confidence for every row.

| Island | Current responsibility and consumers | Classification / decision |
|---|---|---|
| Root/build/deployment | Solution/project references govern 20 projects; CMake explicitly builds Vulkan and CSPICE. Triangle deploys configuration-specific DLL and an explicit 49-shader production set. | A/E. Release mapping repaired. Keep explicit deployment rather than copying diagnostic shaders indiscriminately. |
| Precision/frames/camera | FP64 subtraction, exact time, typed positions, transform graphs/resolvers and camera projection serve runtime and frame fixtures; precision/reference-frame/camera suites pass. | A/C. Independent mathematical reference implementations are deliberate oracles. |
| Unreferenced public helpers | `CameraMath` and `RelativePosition` have no explicit other-file references in this checkout. Their public assembly surface has no established external-consumer inventory. | I/N. Confirm supported external API contract, then retire if unclaimed. Low-reference scan is insufficient by itself. |
| Celestial authority | Solar and NCPE v2 loader create immutable explicit catalogs; analytical/sampled evaluation, rotations and definition hashes serve Solar and deterministic tests. | A/C/D. |
| Legacy celestial authoring | Internal legacy node constructors and compatibility `TryCreate` overloads synthesize catalogs for focused Simulation tests; production already supplies catalogs. | I: MIGRATE THEN RETIRE. Move fixtures to explicit catalogs while proving hierarchy/error/ephemeris/hash invariants. Old adapter is not a supported production authority. |
| Spacecraft/time | Player torque/SAS controls, warp and celestial examples consume integration, attitude and guidance. Empty `SimulationWorld` did nothing. | A/C for real contracts; G placeholder removed. |
| NCPE v2 | Builder `--synthetic/--self-test`, byte-only runtime loader and sampled-system regressions; no installed/tracked `.ncpe` artifacts found. Solar uses authored compact DE440 model. | D/C; runtime loader capability A. Fixtures generated in tests. |
| NCPE v1 | Separate public offline codec; only v1 tests call it. No supported current asset/producer found. Shared `EphemerisHash`/sample records are required by v2. | I/N: public docs advertise legacy readability and external consumers are unknown. Decide compatibility, then remove codec/v1 types and migrate applicable corruption/determinism tests to v2. Tests alone are not a KEEP reason. |
| Offline NAIF | Pinned kernels/CSPICE, explicit shim, lunar pack generator and compact-Solar validation/fitting provide reproducibility. Runtime uses coefficients/NCLo pack. | D/C. Missing rebuildable shim rebuilt; adapter tests pass. Offline oracle is not obsolete because runtime must not call it. |
| NCCUBE1 global | Current terrain-v5 manifest: schema 1, 126 records, 61,484,224 bytes; native/managed readers, generator and tiny-global fixture. | A/C/D. “1” is the current global family, not stale local compatibility. |
| NCCUBE2 schema 3 | Florida terrain 5, L8–L11, 859 records, 210,307,596 bytes; generator → manifest/cache → parser → NCSM1 dependencies/publication and R16/control oracle. | A/C/D. Accepted format/assets/physical authority untouched. |
| NCCUBE2 schema 2 / payload 1–2 / BC4 residual | Tiny-local schema-2 terrain-4 fixture; three old ignored packages at baseline; generator non-regional branch emits schema 2; backward readability is documented. No production manifest selects it. | I/N: decide supported non-regional workflow/external readers; migrate fixture to schema 3 and preserve corruption/transcode checks, then retire old parser branches. BC4 primitive used to encode BC5 normals must remain. |
| Local albedo/normal/control channels | Regional runtime needs R16 residual; generator, digests, manifest identities and managed physical-control classification still involve the multi-channel layout. | A/D until a bounded schema migration separates unused presentation payload from shared physical/source/digest responsibilities. No asset rewrite here. |
| Topology families | NCSM1 is sole production Earth owner. NCTOP/NCTOP2 serve explicit topology/GPU proof scenes, generators and differential tests. | A production; C/D/J proofs. Retain their named reproducibility purpose, not alternate Earth ownership. |
| Earth/Solar/Florida | Six presets → EarthRoutePolicy/SampleOptions → Solar/New Earth → NCSM1. Global bootstrap and non-Earth presentation remain separate. Support/visibility/material-receiver paths are accepted. | A/C. Generation-3 comparisons remain independent C physical oracles, never production ownership. |
| Old local planner/cache/T3 | Only their own tests exercised superseded policies; current regional authority and GPU tessellation supersede them. | G/H, removed. |
| Interop/GPU helpers | Managed ABI callers reach height-query, mesh preparation, GPU proofs and runtime. Layout checks and CPU/GPU parity are independent verification. | A/C/J. Remove unused private decoders, retain ABI and independent contexts. |
| KMT Vulkan warning | Window shared-memory transport remains live. Prior memoryTypeIndex-00645 warning needs measured window-specific investigation. Offscreen parity here reports zero validation errors. | A/N. Not a window-interop fix or proof that prior window warning is resolved. |
| Launcher/settings | Six supported production presets and explicit grid/frame/subdivision/proof tools. Legacy settings loader upgrades pre-window/resolution/diagnostics persisted JSON. User settings exist outside repository inventory. | A/E, B/N settings upgrade. Define supported settings versions before silently dropping user preferences. |
| Diagnostics | EarthRouteValidation, regional physical probes, P2S5C traversal, Solar warp, query/GPU proof modes and telemetry have permanent test or reproduction consumers. Archived mutation scripts are provenance only. | C/J/E. Keep useful capture ability separately from temporary output. |
| Remaining diagnostic interfaces | Fixed timestamp accounting still has an unused anchored label; `--verbose-input` is a documented alias. | I/N. Identify telemetry/CLI consumers before removing interfaces. No performance work. |
| Assets/source | NASA imagery/elevation, manifest-selected packs, NCSM1 levels, NCLo pack, source provenance and deterministic generators have named runtime/reproduction consumers. | A/D/C; hashes preserved. Small repeated deployment elevation files serve independent executable directories. |
| Acquisition/cache | USGS TIFF and metadata are regeneration sources; content-addressed packs are derived; downloads/generation are staging. AssetTool verify/install/fetch/build and acquisition scripts remain. | D/A/L. Cached source data is not automatically disposable. |
| Root `ln -c Debug` file | It is a 53,560-byte captured warning/diff log, not an executable helper; no code/project reads it. Prior explicit retention instruction and historical docs called it supported. Actual Debug build command is `dotnet build NovaCore.sln -c Debug`. | K/N: resolve the explicit prior retention boundary, then retire the misleading log. Retention here is temporary, not a claim of launcher functionality. |
| Historical docs/evidence | Original measurements, rejected candidates, runtime identities, provenance and reproduction source remain useful. Removed video remains in milestone history. | F. Original package hashes are historical snapshots, not a current all-files-present guarantee. |
| Current docs | README, handoff/state and architecture/rendering must identify P2S5H and current commands/debt front. | A/E/F; normalized without rewriting measurements. |
| Broad Graphics runner | Existing fixture/order assumptions and distant/detailed orbit assertion require isolated fixture ownership analysis. Targeted tests pass in fresh processes. | C/N. No full Graphics-suite PASS claimed; no renderer changes made to satisfy old assumptions. |
| Public compatibility inventory | Public APIs/formats can have external consumers not visible in the checkout. | N. Establish supported producer/consumer inventory, then retire unclaimed interfaces in bounded packages. |
| Historical LFS rules | Old extension patterns describe LFS-backed historical assets and avoid accidental ordinary-Git reintroduction during recovery. | F/M. No current runtime consumer inferred; attributes/history unchanged. |

## Cache findings and implemented cleanup

Initial `.novacore`: **1,352,966,144 allocated bytes**. Exact duplicate copies
removed, with full paths/hashes in the removal receipt:

| Removed path under .novacore | Retained content-addressed SHA-256 | Allocated bytes |
|---|---|---:|
| baseline-cache/sha256/5e/5e92…nccube | 5e92a0676bf8cd64f4c00b5e8d79f4b8186cd9a8a57b395138edbab760f1cb76 | 61,485,056 |
| cache/earth_florida_m12.nccube | c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe | 210,309,120 |
| cache/terrain/v1/.generation/earth_local_v2.nccube | 60ada8949bfd782dfaea6c04270186bda52654d7263fbc3dbda5eaa4fd2e578a | 7,655,424 |
| cache/terrain/v1/.generation/earth_surface_v5.nccube | 288f56e129717ceb83ad133490d424fd1d49836601d0a768bb5dab525cf4bf08 | 61,485,056 |

No recursive cache deletion, junction traversal or deletion of unique content.
Current manifest-selected packs verify after cleanup. Cache keys use SHA-256 and
a versioned root; publication validates staged bytes and atomically replaces only
the matching address. Normal generator staging cleans up in `finally`.
`clean-incomplete` removes staging older than 24 hours. Completed unreferenced
generations have no retention/mark-sweep policy and can accumulate indefinitely.

Recommend a separate dry-run-first eviction command based on current manifests,
explicit reproduction pins and active leases. Pin source-acquisition inputs
separately. Remaining distinct old objects need that policy; no automatic eviction
or distribution redesign was implemented here.

## Git/LFS findings — no reclamation performed

| Measurement | Bytes / objects |
|---|---:|
| .git allocated | 1,630,698,032 |
| All Git objects | 5,051; logical 1,232,990,945; stored representations 539,043,693 |
| Reachable from refs | 3,456; logical 306,245,077; stored representations 212,746,864 |
| Unreachable from refs | 1,595; logical 926,745,868; stored representations 326,296,829 |
| Unreachable including reflogs | 1,591; stored representations 326,295,930 |
| Entirely unreachable pack | 325,747,840; 1,412 objects, no ref/reflog reachability |
| Nine temporary Git object files | 103,067,648 allocated |
| Local LFS | 987,908,353 logical; 987,987,968 allocated |
| Local LFS without reachable-history pointer | 0 |

Stored representation sums exclude pack headers/indexes and are not exact
filesystem recovery predictions. Mixed-pack delta dependencies, reflog retention,
grace periods and concurrent Git writers matter. Normal local maintenance could
reclaim unreachable representations/abandoned temporary files after those checks.
**0 bytes were approved for immediate Git/LFS deletion or reclaimed here.**
Do not manually delete packs.

Reachable history includes the **76,632,635-byte accepted Florida video**, an old
**61,484,224-byte terrain-v4 pack**, and diagnostic ZIP/PNG blobs. Reclaiming
reachable artifacts would require a separate published-history decision.
No history rewrite, tag movement, force-push, reachable-object deletion or LFS
pruning occurred.

## KSA source and engineering history

Installed `E:/Kitten Space Agency/KSA.dll` reports
`2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`; SHA-256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
Hash/version match the local decompiled reference provenance. Relevant production
types were reread this pass. That version string identifies the installed build;
it is not an inferred calendar date or proof every live-changelog revision is installed.

| Evidence | Old responsibility → replacement/current source | Decision |
|---|---|---|
| Official version-history July 11, 2026 deployment entry; current PartTree.SetRootPose | Four root/child movement copies consolidated; current method owns pose propagation and explicit derived-cache reset. | ADAPT: retire duplicate/dead owners after proving the surviving authority. Do not merge independent mathematical oracles merely for fewer lines. |
| Official version-history July 11 entry announcing save/vehicle incompatibility and removal of a dead part field; GameSave, SaveMetaData, PartTree.Serialize | Migration announced explicitly; current serialization owns complete tree data and saves carry version metadata. | ADAPT: explicit compatibility/migration decisions. KSA history does not prove NovaCore NCPE/NCCUBE external consumers absent. |
| [Live-changelog revision 5394, September 1, 2026](https://discord.com/channels/1260011486735241329/1260112103134724146/1544532836135206964) | Shared mutable pose caused multiple objects to render the last update; immutable rig data remains shared and mutable pose becomes instance-owned. | ADAPT: distinguish shared input from duplicate/misowned state. Shared pack/geometry/oracle dependencies remain deliberately retained. |
| Local CelestialTextureStreamer, TextureMipStreamSource and retired-resource queue | Explicit source, demand, budgeted residency and delayed GPU resource retirement. | ADOPT existing compatible boundary: keep banked native residency/publication, remove superseded test-only managed demand/LRU. No new renderer/cache design copied. |

Official live-changelog and version-history were consulted read-only. Dev-updates
provided no needed additional proof for these bounded removals. No equivalent KSA
NCPE/NCCUBE or Git reclamation contract was established. GPU residency policy
does not authorize disk-cache/Git deletion. No proprietary source/assets were
copied into NovaCore or the evidence package.

## Validation

- Native Debug and Release: PASS.
- Managed solution Debug/Release: PASS, all 20 projects, zero warnings/errors.
- Precision, ReferenceFrames, Camera, Simulation, EphemerisBuilder and Launcher: PASS.
- NAIF adapter: initially missing explicit shim; rebuilt retained prerequisite, then PASS; no source correction.
- Targeted Graphics: material synthesis, local format/compression, Florida regional physical surface, Earth route convergence, GPU physical height, displaced mesh/normals and single canonical authority: PASS in separate processes.
- GPU height/displaced mesh: zero Vulkan errors. Candidate height maximum error 9.313225746154785e-10 m; shared mesh position/normal gaps zero. Parity results, not a performance campaign.
- Launcher: 15 permanent tests and six deployed CreatePlan route probes PASS.
- Production global/Florida assets: verification PASS after cache cleanup.
- Shader source unchanged; deployed shader/DLL matching is recorded in the evidence summary.
- No full Graphics-suite PASS claim, expensive Florida recapture or new manual acceptance claim.

Normal launcher:
`E:/NovaCore/tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`.
**Florida Launch Site** remains
`--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`.

Final storage, preservation/whitespace verification and complete Git status are
in the [bounded evidence index](engineering-evidence/repository-debt-retirement/README.md).
The safe subset is ready for Project Control review; the named follow-ups remain
before declaring the debt front clean. No staging, commit, push, tag or banking.

## Historical deferred ledger from the Earth-route consolidation

The following original ledger records the earlier investigation boundary.
It is preserved as chronology and is superseded by the current decisions above.


This ledger records findings deferred from the Earth/Solar/Florida consolidation.
Deferral is not acceptance of permanent debt. KEEP requires a specific current
production, development or reproducibility responsibility. A historical test or
callable old API alone is not sufficient. INVESTIGATE must resolve to a justified
KEEP or a subsequent bounded RETIRE; it is not an indefinite compatibility promise.

| Item / responsibility | Current consumers found | Possible retention reason / evidence still needed | Recommendation |
|---|---|---|---|
| NCPE v1 offline interchange (`EphemerisArtifact.cs`) alongside NCPE v2 | EphemerisBuilder tests and offline artifact tooling; v2 runtime hash/loader are separate | Determine whether a maintained source-conversion workflow or reproducible published artifact still requires v1. Tests alone do not justify it. | INVESTIGATE in an ephemeris-format pass |
| NCCUBE local older header/payload versions and BC4 height codec | `LocalTerrainPack.cpp`, AssetTool codec/fixture generation, local terrain format tests | Production Florida uses format 3 and R16 physical residuals. Determine whether older fixtures protect codec interoperability that is still needed, or can migrate and retire older decoding. | INVESTIGATE; retire unsupported versions after asset/provenance inventory |
| Local pack albedo, normal and control channels after texture-transport retirement | Pack format/parser, source generator, manifests and codec tests; NCSM1 residency consumes physical residuals | Removing unused runtime demand is complete. Changing the multi-channel format or asset identity needs a separate reproducible asset migration. | INVESTIGATE in NCCUBE/source-data pass |
| Legacy celestial authoring APIs | Celestial source definitions and analytical test fixtures | Prove maintained authored-source conversion consumers before narrowing APIs. No change was needed to Earth ownership. | INVESTIGATE |
| Repository-root launch/build helpers, including `ln -c Debug` | Documented developer workflow and launcher regression | `ln -c Debug` is explicitly retained and must continue selecting Debug. Assess duplicate helpers separately against actual developer workflows. | KEEP `ln`; INVESTIGATE duplicates |
| Simulation scaffolding and snapshot/API modernization | Simulation projects, sample controls and deterministic tests | Simulation authority is independent of renderer consolidation. No obsolete responsibility was proven here. | INVESTIGATE in a bounded simulation audit |
| Independent subdivision/spherical topology proof scenes and GPU mesh/query helpers | Explicit development presets; topology, CPU/GPU height/normal and mesh-preparation tests | Current reproducibility and differential GPU correctness oracles. They do not own ordinary Earth rendering. The old dynamic owner no longer supplies shared constants or physical-key definitions. | KEEP for those named development contracts; reassess if consumers retire |
| Rejected `PlanetaryTerrainTessellationStudy` prototype | Only the historical section of the material-synthesis regression | Its old test demonstrates no current production responsibility; numerical study history can remain in documentation. It predates the convergence scaffolding and was not expanded into this ticket. | RETIRE in a subsequent bounded diagnostic cleanup |
| Existing D3D11 KMT external-memory Vulkan VUID `memoryTypeIndex-00645` | Window interop path and live Vulkan validation | Interop still supports the native presentation window. The warning needs its own measured interoperability correction. | INVESTIGATE; not accepted permanently |
| Solution `Release|Any CPU` project mappings | `NovaCore.sln` Release build reports MSB4121 for its unmapped projects | Explicit Release project builds work and are required for actual validation/deployment; the solution command is not evidence that those projects built. | INVESTIGATE / repair in bounded build-configuration work |
| Broad Graphics-suite fixture/order assumptions and distant/detailed orbit-line assertion | Full Graphics test runner | Isolated contracts and integration order need independent fixture ownership. Do not change unrelated renderer behavior to satisfy a stale test. | INVESTIGATE; record concrete failures in consolidation results |
| Remaining near-surface GPU workload and orientation variability | Accepted NCSM1/TES/material rendering | Real production work; preserve quality and measure equivalent steady/moving workloads before the next bounded performance objective. | KEEP architecture; INVESTIGATE measured cost separately |

No unrelated item above was retired by this ticket. Every later retirement must
identify current consumers and reproducibility obligations before deletion.
