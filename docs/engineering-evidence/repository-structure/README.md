# Repository structure audit — 2026-09-06

**READY TO BANK REPOSITORY STRUCTURE PACKAGE.** This is an unbanked structural
candidate, not a new Earth milestone. Current placement authority is the single
[repository map](../../repository-structure.md). Repository Debt Retirement remains
open. This snapshot records the evidence and decisions behind that map.

## Baseline

HEAD, local `origin/main` and directly queried remote main all matched
`9409fec36e72f34f26cd4ab5fe47082e7dcafb20`, **NovaCore tests: complete deterministic
graphics validation**. Branch `main`, initially empty `git status --short`.
P2S5H `m12d-p2s5h-earth-route-convergence` still targets
`32ffac50ab5c06518ede24edfb5c531976d4ec99`.

Initial workspace: **7,906 files, 4,331,968,586 logical bytes**. The scan excluded
six existing `earth-data` junctions under old diagnostic deployments and followed
no reparse targets; no scan errors. Directory metadata/alternate streams are not
included. The original compressed-size API did not measure allocation rounding,
so those equal-to-logical results are not presented as allocated bytes.

| Initial location | Files | Logical bytes |
|---|---:|---:|
| root files | 7 | 97,381 |
| `.git` | 144 | 1,632,196,677 |
| `.novacore` | 13 | 1,012,006,819 |
| `assets` | 51 | 989,049,944 |
| `build` | 1,782 | 78,269,291 |
| `docs` | 88 | 78,163,335 |
| `external` | 4,217 | 290,071,355 |
| `native` | 108 | 807,238 |
| `samples` | 246 | 152,162,817 |
| `src` | 449 | 27,470,581 |
| `tests` | 551 | 56,877,887 |
| `tools` | 250 | 14,795,261 |
| `.codex` | 0 | 0 |

Root source/configuration consists of `.gitattributes`, `.gitignore`,
`Directory.Build.props`, `ENGINEERING_RULES.md`, `NovaCore.sln`, `README.md`, plus
the accidental transcript described below. No other unexpected root file appeared.
All 51 initial asset files are tracked. No tracked files occur under `.codex`,
`.novacore`, `build` or `external`; `.codex` was empty and previously not ignored.

## Required structure matrix

Authority types: **S** source/production; **T** test/regression; **G** tooling/generator;
**D** documentation/provenance; **W** workspace/cache; **B** build output;
**A** agent state; **E** external reference; **U** misplaced/unknown.
Confidence is about the measured responsibility, not unknown external API usage.

| ID | Path / responsibility | Current purpose | Type | Producer | Consumer | Problem | Decision | Action | Confidence |
|---|---|---|---|---|---|---|---|---|---|
| S01 | `assets/` | Declared content/fixtures | S/T/D | Reviewed generators and source acquisition | Runtime, tools, tests | Family names obscure roles | KEEP | One asset-family map | High |
| S02 | `native/` | Native renderer and offline shim source | S/G/T | C++/GLSL maintainers | CMake, Interop, adapter | Different runtime/offline roles | KEEP | Document two native owners | High |
| S03 | `src/` | Reusable managed responsibilities | S | Six managed projects | Samples/tools/tests | No circular project ownership found | KEEP | Publish actual dependency graph | High |
| S04 | `tests/` | Current invariant regressions | T | Invariant owners | CI/developers | Milestone names require interpretation | KEEP | Invariant map; no mass renames | High |
| S05 | `tools/` | Acquisition, generation, distribution, launcher | G/S | Tool maintainers | Assets, offline models, player launch | Launcher is runtime entry, not generator | KEEP | Explicit per-tool roles | High |
| S06 | `samples/` | Runtime composition and examples | S/T | Sample maintainers | Launcher/developers/regressions | Triangle name understates full runtime | KEEP | Document role; avoid path churn | High |
| S07 | `docs/` | Current contracts plus dated reports | D | Subsystem owners | Developers/agents | Flat history can look like onboarding | CLARIFY | Short onboarding chain + history index | High |
| S08 | `external/naif/` | Acquired CSPICE/kernels and shim build | E/B | Offline acquisition/build | NAIF adapter only | Must not imply runtime/vendor authority | KEEP | Link pinned provenance and shim contract | High |
| S09 | `.novacore/cache/` | Verified payloads, sources and staging | W | AssetTool/acquisition/build | Explicit runtime/tool resolution | Machine contents must not be assumed | KEEP | Document banked recovery/acquisition boundary | High |
| S10 | `.codex/` | Empty local agent directory | A | Creator not recorded; local agent convention | No repo consumers/config found | Unignored future transient state | IGNORE | Add bounded root ignore rule | High |
| S11 | `build/`, `bin/obj` | Rebuildable output and local diagnostics | B/W | CMake/MSBuild/probes | Development/deployment | Old directory names remain after evidence retirement | KEEP | Document lifecycle; no cache/deployment purge | High |
| S12 | `.git/` | Git-managed repository state | W | Git | Git | Separate storage debt | KEEP | Read-only accounting; no maintenance | High |
| S13 | root solution/props | Composition and compiler policy | S | Build maintainers | MSBuild | None | KEEP | No change | High |
| S14 | root attributes/ignore | Binary transport/local boundary | S | Repository maintainers | Git | Missing agent-local rule only | KEEP | `.codex` ignore; attributes unchanged | High |
| S15 | root README/rules | Orientation vs engineering authority | D | Project/architecture owners | New sessions/developers | Technical detail precedes orientation path | CLARIFY | Add short authoritative chain | High |
| S16 | root `ln -c Debug` | Accidental warning/diff transcript | U/D | Captured Git output, first in `32c20ae` | Historical docs only | Incorrectly called a build helper | RETIRE | Delete exact tracked transcript; preserve Git provenance | High |
| A01 | `assets/earth/source` | Generator albedo/cloud inputs | C: generator input | NASA source acquisition | Earth pack builders | Not runtime terrain authority by itself | KEEP | Document input role | High |
| A02 | `assets/earth/runtime` oracle | Checked canonical elevation input | A: production | `build_elevation_oracle.py` | CPU/GLSL physical height + builders | Runtime-directory name includes metadata too | KEEP | Document oracle versus sidecars | High |
| A03 | Earth content sidecars | Detailed generated record provenance | C: provenance | Cube/local builders | Manifest references and engineering verification | Could be confused with distribution manifests | KEEP | Explain distinct metadata responsibility | High |
| A04 | `planetary-nested-scale-mesh` | 18-level NCSM1 production library | A: production | Nested-scale generator | Sample → library/adapter → NCSM1 | None | KEEP | Preserve 554,589,170-byte family | High |
| A05 | `planetary-production-topology` | 18-level transactional topology fixture | B: fixture | Production-spherical generator | Topology/runtime/seating tests | Historical “production” name implies authority | FIXTURE | Add role README; preserve 235,631,006-byte inputs | High |
| A06 | `planetary-topology` | Compact manifold/GPU proof fixture | B: fixture | Spherical topology generator | GPU proof tests + supported proof route | Could look like duplicate production topology | FIXTURE | Add role README; preserve 246,346-byte inputs | High |
| A07 | `assets/terrain/manifests` | Install identity/generator/acquisition contract | A: production | Reviewed manifest authors | AssetTool/cache/sample/launcher | Distinct from record sidecars | KEEP | Identify sole distribution owner | High |
| A08 | `tests/fixtures/terrain` | Tiny global/local format fixtures | B: fixture | Retained deterministic scripts | Reader/cache/corruption regressions | Historical local migration already resolved | KEEP | Reference banked migration; no format changes | High |
| N01 | `NovaCoreNative.cpp/.h` | Live App/window/device/submission and C ABI | S | Native renderer | Interop/sample | Multipurpose but one lifecycle owner | KEEP | Map responsibilities; no library split | High |
| N02 | `ProductionCubeSurface.*`, `LocalTerrainPack.*` | Serialized data readers/identity | S | Native data layer | Residency/query/runtime | Low format numbers are not obsolescence | KEEP | Defer public format retirement | High |
| N03 | Regional `.h/.inl` files | Demand, residual readiness/preparation/publication | S/T | Native App + worker/GPU phases | NCSM1 publication | Includes reflect phase ownership, not duplicate modules | KEEP | Map phases and probe boundary | High |
| N04 | HeightQuery/MeshPreparation | Independent physical-query/preparation contexts | S/T | Native compute utilities | Managed query and parity workflows | Generic names/proof association can mislead | KEEP | Trace callers; no removal | High |
| N05 | SphericalBillboardGpuProof | Offscreen lifecycle/indirect proof | T/G | Native proof context | Managed proof route/regressions | Historical name, current invariant consumer | KEEP | Document development responsibility | High |
| N06 | AuthoredFacilityGeometry / FacilityLightOcclusion | Geometry versus derived light visibility | S | Facility owners | Runtime plus native parity tests | Shared helpers referenced by tests | KEEP | Distinguish physical/contact and lighting | High |
| N07 | PresentationGpuTestDevice + native tests | Strict test instance/device ownership | T | Native test harness | Native CPU/GPU regressions | Similar Vulkan calls are independent test ownership | KEEP | No merge with production App | High |
| N08 | `shaders`, CMake, sample deployment | Source → SPIR-V → declared runtime subset | S/B | GLSL/CMake/MSBuild | Native runtime/proofs | Multiple lists represent build vs deploy roles | KEEP | Document configuration/hash owner | High |
| M01 | Core | Precision/frame/camera/surface primitives | S | Core | Graphics/Simulation/examples/tests | None found | KEEP | Document authority | High |
| M02 | EphemerisFormat | NCPE storage contract | S | Format codecs | Simulation/offline builder | v1/v2 public support is separate | KEEP | Link contract resolution | High |
| M03 | Graphics | Derived presentation and physical-query contracts | S/T/G | Graphics | Sample and tests | Some public helpers combine proof and reference preparation | KEEP / INVESTIGATE naming seam | Map callers; no public API extraction | High / medium |
| M04 | Interop | ABI declarations and transport | S | Interop/native contract | Graphics/sample | None found | KEEP | No handle/authority migration | High |
| M05 | Platform | Consumed logging/options | S | Platform | Triangle/Precision tests | Empty public RuntimeHost is not actual window owner | KEEP project | Record placeholder for bounded public-surface decision | High |
| M06 | Simulation | Time, definitions, evaluation, transactions, spacecraft | S | Simulation | Sample/offline tools/tests | No Graphics/NAIF runtime dependency | KEEP | Preserve acyclic direction | High |
| T01 | Precision/Frames/Camera tests | Numeric, coordinate and input invariants | T | Core/camera maintainers | Regression suite | No superseded-only island proven | KEEP | Group by invariant in map | High |
| T02 | Graphics/native tests | Terrain, ABI, ownership, GPU/window invariants | T | Graphics/native maintainers | Canonical validation | P2S* names retain chronology | KEEP | Categories/map instead of renaming | High |
| T03 | Simulation/ephemeris/NAIF tests | Determinism, state and offline reference invariants | T | Simulation/format owners | Regression suite | Offline prerequisite differs from normal runtime | KEEP | Explicit prerequisite boundary | High |
| T04 | Launcher tests | Preset/route/deployment contracts | T | Launcher owner | Regression suite | None | KEEP | Existing 15-test contract unchanged | High |
| G01 | AssetTool | Install/verify/build/cache lifecycle | G | Managed distribution code | Runtime cache and user workflow | Already banked owner | KEEP | No cache redesign | High |
| G02 | earth_data / terrain_assets | Earth conversion and tiny-fixture regeneration | G/T | Python/PowerShell generators | Manifests, payloads, permanent fixtures | Fixture script is intentional, not dead acquisition code | KEEP | Per-output mapping | High |
| G03 | EphemerisBuilder / NAIF adapter / CSpiceShim | Offline storage/extraction | G/E | Offline tools and pinned NAIF inputs | Artifacts/compact models/tests | Not runtime dependencies | KEEP | Document separation | High |
| G04 | Launcher | Supported point-and-click configuration | S/G | Launcher | Triangle runtime | Lives under tools, has real runtime-entry role | KEEP | No launcher/scenario change | High |
| R01 | Triangle | Six production Earth routes + useful generic/proof scenes | S/T | Sample composition | Launcher, window tests, developer runs | “Sample” understates current role | KEEP | Describe; no relocation/build-path churn | High |
| R02 | ReferenceFrameFixture | Console reference-frame example | T/G | Example project | Developer frame demonstration | Not a player launcher route | KEEP | Distinct example purpose | High |
| D01 | Current state / architecture / handoff / rules | Checkpoint / relations / workflow / constraints | D | Named document owners | New sessions | Overlapping narrative, roles not prominent enough | CLARIFY | Links to one structure authority | High |
| D02 | History/evidence families | Engineering conclusions, provenance and reproduction | D | Completed investigations | Forensic reference | Two families lack individual indexes; flat report paths | KEEP / INDEX | Add one umbrella history index; no archive moves | High |
| E01 | Installed KSA / ignored local reference scratch | Read-only architectural reference | E | External installation/local prior analysis | Targeted architecture investigations | Source repo differs from shipped installation | KEEP external | No copying into tracked source/history | High |

## Real ambiguity and bounded corrections

| Owner A | Owner B | Ambiguity | Target owner | Migration / risk |
|---|---|---|---|---|
| Root transcript named like a command | Actual `dotnet build NovaCore.sln -c Debug` workflow | Earlier reports incorrectly implied the transcript executes Debug | Solution/MSBuild + build-windows instructions | Delete one transcript; no executable/source input changes |
| Historical `planetary-production-topology` name | NCSM1 production library | Name alone suggests two production asset authorities | NCSM1 production; former family explicitly regression | Two small asset READMEs; no artifact/path/hash movement |
| Flat investigation reports | Current onboarding docs | Historical candidate status can be mistaken for current instructions | Current-state/architecture/map + explicit history index | Links/indexes, no historical measurements rewritten |
| Unignored empty `.codex` | Shared engineering rules | Future local agent state could enter source control as authority | Ignored scratch; shared rules in ENGINEERING_RULES | One ignore pattern; no local contents deleted |

No duplicate current renderer, data authority or managed-project ownership was
proven. CMake's source compilation list and the sample's deployed shader subset
have different consumers; this ticket does not merge them. Generator content
sidecars and install manifests also have distinct consumers and identities.

## Exact root-debris proof

`ln -c Debug`: 53,560 bytes; SHA-256
`588b6518e9eccfaa4fbcf4350bf307d80709d41ca05321147e55a1393bc259d4`.
Tracked first in `32c20ae2cb36c30ea50ea1d49c2b95ed0ac888dc`, Milestone 2.
Its contents are Git line-ending warnings and a source diff, ending with the old
RunTriangle→RunRenderer declaration change. It has no script entry point, project
input, generator input or source/test/runtime consumer. Existing references are
historical documentation; substring hits inside `NovaCore.sln -c Debug` are the
real build command, not file consumers. The exact original shell redirection that
created it was not recovered; the captured-output responsibility is proven from
content/history. This ticket explicitly authorizes removal of proven accidental
root debris, superseding the prior temporary retention boundary. Git history is
preserved; the transcript is not duplicated into another directory.

## Evidence/archive review

Initial family sizes: Earth-route convergence **73,531,244 bytes** (29 files),
repository-debt retirement **105,213** (4), compatibility resolution **31,638** (3),
cache lifecycle **4,866** (1), Graphics validation **253,753** (2).
ZIP central-directory member lists were inspected without extraction. Larger
members are representative clips/images, logs, JSON summaries and source/recipe
snapshots; no `.d32`, `.hdr`, `.exr`, production `.nccube`/`.r16`, DLL or executable
member was found in the checked ZIPs. This extension check alone is not proof of
every member's semantics; the largest-member names/sizes and existing retained
package manifests corroborate a bounded evidence role, not reintroduced multi-GB
GPU archives. Existing clips/JSON snapshots remain historical evidence under the
already accepted retention package. No re-compression or evidence purge here.

Some retained recipes contain historical KMT allowances and retired arguments.
The new umbrella index explicitly directs current regression users to banked
Graphics commands, preventing historical recipes from becoming present authority.

## KSA responsibility comparison

Read-only inspection of the current installation found runtime/engine DLLs and
`Content/Core` families for shaders, textures, meshes, parts and ground clutter,
plus other shipped content. No installation content was copied or changed.

| KSA responsibility | NovaCore equivalent | Decision / lesson |
|---|---|---|
| Engine/runtime binaries distinct from content | Source projects/native versus assets; build/deployed binaries ignored | ADAPT: preserve responsibility separation, not installation folder names |
| Shader/content categories under Content | Native GLSL source, generated SPIR-V, declared runtime deployment; reviewed asset families | ADAPT: source/build/deploy are separate lifecycle stages |
| Shipped installation consumed by players | Source repo also contains generators, tests, manifests and engineering provenance | INTENTIONALLY DIFFER: measured project/test/generator consumers justify source-repository structure |
| Local installed game as reference | External KSA tree and prior ignored local scratch | ADOPT read-only reference boundary; no production dependency |

No KSA history lookup would resolve the local placement findings; no general KSA
comparison or folder imitation was undertaken.

## Remaining structural debt

- `PlanetarySphericalBillboardNaturalTerrainProof` mixes proof orchestration with
  a reusable preparation seam referenced by the moving coordinator. Names can
  mislead; an extraction/rename could affect public callers. Future bounded API
  ownership review should distinguish native production and reference/test paths.
  No implementation collision is inferred from the file name.
- Platform's empty public `RuntimeHost` has no current internal caller; logging
  justifies the Platform project. Retire or give the placeholder a concrete role
  only through the deferred public-surface contract decision.
- `NovaCoreNative.cpp` and Triangle composition are large; all live phases have
  identified owners. Split only after a concrete maintenance/ownership defect is
  measured, not for line-count aesthetics.
- Historical evidence could later receive another bounded value/retention review;
  no new bulk leak or runtime dependency was found here. Existing path references
  and snapshots make mass relocation unjustified.
- Public NCPE/NCCUBE/API retirement, generic resource cleanup, cache/Git/LFS
  maintenance and performance remain separate debt; deferral is not permanent
  acceptance. Existing contract matrices retain their unresolved external-use
  evidence boundaries.

## Validation / storage closeout

Only documentation/indexes, the local-state ignore rule and the proven accidental
root transcript change. No source, test, asset binary, shader, namespace, project,
runtime path or manifest identity moves. Path/link, project-reference and asset
manifest checks are proportional validation; Debug/Release builds additionally
check that retiring the tracked transcript removes no build input. No performance
profiling, broad GPU replay or manual Florida acceptance is required.

Validation completed:

| Check | Result |
|---|---|
| `dotnet build NovaCore.sln -c Debug --no-restore` | PASS; zero warnings/errors |
| `dotnet build NovaCore.sln -c Release --no-restore` | PASS; zero warnings/errors |
| Relative Markdown references in changed/new documents | 94 resolve; checked anchors resolve |
| Managed project references | 20 projects, 30 references; all paths resolve, no cycles |
| Three topology manifests | 39 referenced artifacts exist with declared sizes |
| Terrain distribution manifests | Six generator/provenance/content-sidecar paths resolve |
| Generated/local ignore boundary | `.codex`, cache, build and sample deployment probes ignored; no probe files created |
| `git diff --check` | PASS |
| Git authority | HEAD/origin/main and P2S5H targets unchanged; nothing staged |

Artifact checks above verify manifest paths/sizes, not a newly regenerated topology
or content-hash acceptance run. No artifact, source or shader changed. Additional
GPU/window/launcher tests would not exercise these documentation/debris changes;
their banked Package 2 results remain historical validation, not newly run results.

Audit scans run in memory without dumping file inventories/captures. The retained
report/map/indexes are the evidence; no temporary audit directory is created.
The post-build scan, immediately before adding this closeout text, measured
7,910 files / 4,331,955,315 logical bytes / 4,345,714,600 allocated bytes using
FileStandardInfo for allocation, with zero scan errors and the same six junctions
excluded. `build/` remained 78,269,291 logical bytes (80,810,928 allocated).
Final figures including this report are returned to Project Control separately.
The exact removed transcript accounts for 53,560 logical bytes; documentation
additions and small ignored build-metadata updates account for the remaining delta.
Diagnostic captures/temporary audit files created: **0 bytes**. Disposable audit
output remaining after acceptance: **0 bytes**. No staging, commits, pushes, tags
or P2S5H movement.
