# Final repository cleanliness audit — 2026-09-06

**REPOSITORY DEBT RETIREMENT COMPLETE — READY TO BANK FINAL CLOSURE.**
The focused CUBE-03 follow-up resolves the final support decision. No competing
production owner, new root debris, unowned generated directory or new empty
scaffold was found by the preceding audit. The other 39 closure findings and
known future debt are unchanged; this is a closure record, not another architecture
or compatibility-policy catalog.

## Baseline and banked packages

Initially clean `main`; HEAD, `origin/main` and directly queried remote main:
`f230b3fef4cbe5d0299d1c15569b1d51c61398e9`, RuntimeHost retirement. P2S5H tag
`m12d-p2s5h-earth-route-convergence` remains at
`32ffac50ab5c06518ede24edfb5c531976d4ec99`.

Git ancestor checks verified these actual commits, not conversation assertions:

| Banked responsibility | Commit prefix |
|---|---|
| P2S5G workload efficiency | `7bb03b0` |
| P2S5H convergence and permanent diagnostic evidence rules | `32ffac5` |
| Diagnostic-video repository hygiene | `c78b582` |
| Repository debt Package 1 | `742cd89` |
| Compatibility contract audit | `4cd5963` |
| Tiny-local/internal authoring migration | `d3ecaba` |
| Cache lifecycle | `e9e9939` |
| Graphics Package 1 / Package 2 | `e963e8c` / `9409fec` |
| Structure/ownership | `a0595b3` |
| Public API contract authority | `74364df` |
| RuntimeHost retirement | `f230b3f` |

Since banked Graphics Package 2, the only changes under source/native/sample/test/
tool/asset paths are two asset-role READMEs and removal of RuntimeHost. No renderer,
shader, fixture, generator or runtime-route behavior changed in those packages.

## Closure matrix

**CLEAR** means an intentional current responsibility. **B** means KNOWN FUTURE
DEBT with a clear present owner. **A** means CLOSURE BLOCKER. Local/generated paths
are not bankable authority. Confidence describes the measured scope, not omniscient
external-consumer knowledge. The [repository map](../../repository-structure.md)
retains detailed placement and producer/consumer guidance.

| ID | Responsibility/path | Why it exists | Current authority/consumer | Retained contract | Issue? | Closure classification | Action | Confidence |
|---|---|---|---|---|---|---|---|---|
| R01 | `src/` | Six reusable managed responsibilities | Maintainers → samples/tools/tests | Acyclic Core/Format/Interop/Platform; Graphics→Core/Interop; Simulation→Core/Format | None found | CLEAR | Keep source boundaries | High |
| R02 | `native/` | Win32/Vulkan and offline C ABI shim source | CMake → Interop/runtime; CSPICE shim → offline adapter | Runtime versus offline ownership | None found | CLEAR | Keep source and shader owners | High |
| R03 | `tests/` | Current precision, state, format, terrain and presentation invariants | Invariant owners → managed/native harnesses | Bounded fixtures and explicit environment categories | No ceremonial skipped/obsolete-only island proven | CLEAR | Preserve tests and milestone traceability | High within structural review |
| R04 | `assets/earth/` | Elevation oracle and source imagery/provenance | Earth generators → physical query/package production | Production input, not a competing renderer | None | CLEAR | Keep tracked inputs/provenance | High |
| R05 | `assets/planetary-nested-scale-mesh/` | Current production topology | Nested-scale generator/manifest → NCSM1 library/sample | 18-level accepted production owner | None | CLEAR | Keep artifacts; no regeneration | High |
| R06 | `assets/planetary-production-topology/` | Transactional topology/seating regressions | Generator/manifest → topology/runtime/Florida tests | Explicit FIXTURE role | Historical name understood | CLEAR | Keep fixture paths | High |
| R07 | `assets/planetary-topology/` | Small manifold/GPU proof library | Generator/manifest → tests and supported development proof route | FIXTURE / development proof | No competing ordinary Earth owner | CLEAR | Keep current proof inputs | High |
| R08 | `assets/terrain/` | Distribution identity and recipes | Reviewed manifests → AssetTool/cache/runtime resolution | Verified global v5 and Florida v5 | None | CLEAR | Keep manifests distinct from content sidecars | High |
| R09 | `tools/` | Offline generators/adapters, asset lifecycle and launcher | Tool maintainers → artifacts/cache/player entry | Named current workflows | No dead utility island proven | CLEAR | Keep; no tool consolidation | High |
| R10 | `samples/` | Triangle runtime composition and frame example | Launcher/developer → sample/native entry | Six ordinary routes plus explicit development examples | Naming is understood | CLEAR | No route or executable movement | High |
| R11 | `docs/` and evidence | Current contracts versus engineering history | Named owners → developer orientation/forensics | Short onboarding chain; indexed dated evidence | Two stale current statements | CLEAR after correction | Correct host and resolved-Graphics wording | High |
| R12 | `external/` | Acquired NAIF toolkit/kernels/offline build support | Acquisition → adapter/shim | Ignored, provenance documented; no game runtime dependency | None structural | CLEAR | Keep separate from KSA installation | High |
| R13 | `build/`, `bin/obj/` | Rebuildable output and active diagnostics | Build/harness → development/deployment | Ignored; bounded evidence promotion | No renewed bulk-capture accumulation | CLEAR | Keep lifecycle; no purge | High |
| R14 | `.novacore/` | Verified acquired/cache data and source staging | AssetTool/acquisition → explicit consumers | Banked conservative recovery/eviction policy | Old final objects have separate recovery debt | CLEAR / B (D02) | No cache redesign/deletion | High |
| R15 | `.codex/` | Empty agent-local state | Local agent convention; no shared consumer | Ignored; rules remain tracked elsewhere | Empty, no required config | CLEAR | Keep ignore boundary | High |
| R16 | `.git/` | Repository history and local recovery state | Git/LFS → source/provenance retrieval | Reachable refs protected; normal grace periods | Prior maintenance found no safe reclamation | CLEAR / B (D01) | Record, do not reopen maintenance | High |
| R17 | Six root files | Orientation, rules, solution, compiler defaults, ignore/LFS transport | Authors/build/Git → developers/builds | Tracked current configuration | No unexplained root file | CLEAR | No removal or reorganization | High |
| A01 | Simulation | Exact time, celestial/mechanical state and transactions | Simulation definitions/evaluators/transaction engine | Graphics consumes derived results | No competing owner found | CLEAR | Preserve authority | High |
| A02 | Precision/reference frames | FP64 frame resolution and relative transport | Core resolver → Graphics CameraRelativeRenderPosition → ABI | Subtract before narrowing | No competing owner | CLEAR | Preserve precision | High |
| A03 | Ephemeris | Neutral storage and offline production | EphemerisFormat/build tools → internal Simulation loader | Supported offline NCPE v1; v2 reconstruction; explicit migration limits | None in decided scope | CLEAR | Keep banked public contracts | High |
| A04 | Platform/window/device/presentation | Composition and native handle lifetime | Triangle + NovaCoreNative App; Platform logging | Generic startup needs no Earth oracle | Shared allocations remain understood future work | CLEAR / B (D04) | RuntimeHost remains retired | High |
| A05 | Earth physical terrain | Canonical generation-4 H and reference parity | PhysicalSurface/query, verified global/regional inputs | Same body-direction physical authority | No alternate physical owner | CLEAR | No terrain changes | High |
| A06 | NCSM1 and regional residency | Sole ordinary Earth owner; demand/readiness/preparation | Moving coordinator + native regional worker/GPU/publication phases | One current/incoming owner, complete publication; bootstrap is temporary | No competing route owner found | CLEAR | Preserve accepted P2S5H | High |
| A07 | Facility support/light visibility | Ground support and authored light occlusion | Canonical support + AuthoredFacilityGeometry/FacilityLightOcclusion | Geometry and light visibility cooperate, do not duplicate H | None | CLEAR | Preserve accepted Florida result | High |
| A08 | Material coordinates | Consistent surface coordinates and derived shading | Production shaders + SurfaceMaterialCoordinatesTests | Single physical/geographic convention | Presentation quality is future work | CLEAR / B (D05) | No material/quality edits | High |
| A09 | Asset distribution/cache | Acquisition, verification, publication and bounded maintenance | TerrainAssetCache/AssetTool; native residency consumes verified paths | Manifest identity, recovery proof, no implicit runtime generation | Recovery debt is explicit | CLEAR / B (D02) | Preserve current lifecycle | High |
| A10 | Launcher and validation | Point-and-click route intent; strict deterministic tests | ScenarioCatalog/SampleOptions; managed/native validation harnesses | Six Earth routes; scoped canonical layers and observable ambient failures | External overlay issues remain | CLEAR / B (D03) | No new runtime testing needed here | High |
| C01 | NCCUBE1 schema 1 / global v5 | Current global data | Cube generator/manifest → native/managed reader; tiny-global tests | KEEP current supported global contract | Low version number is intentional | CLEAR | Keep | High |
| C02 | NCCUBE2 schema 3 / payload 3 | Current Florida and tiny-local physical format | Current regional/fixture writer → residency/oracle/regressions | R16/control, ranges and record digests | None | CLEAR | Keep | High |
| C03 | NCCUBE2 schema 2 / payload 2 | Current non-regional real-input generator/reader workflow | `build_local_terrain_pack.py` legacy real-input branch → public generic readers | Banked CUBE-04 KEEP — ACTIVE CONTRACT | Future migration may be desirable | CLEAR / B | Preserve producer/consumer together until separately migrated | High |
| C04 | Retired CUBE-03 read contract | No current producer, production asset or supported consumer requires it | Managed record validation / native reader reject payload 1; payload 2 and 3 remain active | RETIRE — HISTORICAL COMPATIBILITY ONLY | No concrete external support obligation established | CLEAR — RETIRED | Remove acceptance; preserve shared active codecs and current-format regressions | High known-consumer evidence; private copies cannot be excluded |
| C05 | CameraMath / RelativePosition | Axis vocabulary and contextualized-by-caller displacement value | Public Core developer surface | SUPPORTED in public-api-contracts | No direct local callers, but meaningful decided contract | CLEAR | Do not reopen support | High |
| C06 | Public proof/preparation APIs | Development/reference operations and cache/report surface | Proof branch, coordinator reference branch and tests | SUPPORTED; native production branch remains distinct | Public naming polish only | CLEAR / B (D06) | No speculative rename | High |
| C07 | Launcher settings and input alias | Preserve documented user configuration/input workflow | Settings loader and LogOptions → launcher/tests | Existing upgrades/defaults and documented verbose-input alias | A future settings-version sunset needs its own decision | CLEAR / B | Keep current behavior; no new compatibility retirement | High |
| D01 | Git cruft and reachable historical blobs/LFS | Recovery grace and published provenance | Git-supported lifecycle | Prior NO SAFE RECLAMATION AVAILABLE; history rewrite unauthorized | Recent cruft, old video and historical LFS recovery limits | B — KNOWN FUTURE DEBT | Fresh audit after grace period or when historical restoration is needed | High prior evidence; not remeasured object reachability |
| D02 | Old cache finals and source/staging copies | Potential recovery/provenance inputs | Banked cache owner | Retain until exact regeneration/recovery and quiescence proven | Reclamation evidence incomplete | B — KNOWN FUTURE DEBT | Before separately authorized cache eviction, not production work | High |
| D03 | OBS KMT / stale Epic registration | External interoperability boundary | OBS/driver/installation; canonical harness excludes ambient contamination | Errors remain observable/fatal; canonical validation strict | Ambient compatibility not certified | B — KNOWN FUTURE DEBT | Upstream/installation investigation when ambient capture is required | High banked diagnosis |
| D04 | Generic/planetary shared allocations | Current native graphics resource ownership | Native App | Grid/frames oracle dependency already fixed | Further resource separation not justified by closure | B — KNOWN FUTURE DEBT | Only after a measured generic-resource requirement | High |
| D05 | GPU variability, morphing, materials, atmosphere/environment/gameplay | Known future production quality/features | Named future renderer/content/simulation fronts | Accepted ownership remains intact | Work unfinished, purpose understood | B — KNOWN FUTURE DEBT | Separately authorized production tickets; no benchmark here | High |
| D06 | Large native/sample files and historical public names | Coherent lifecycle and compatibility | Current owners and public contract | Naming/size is not obsolete responsibility | Possible maintenance improvement | B — KNOWN FUTURE DEBT | Only a measured maintenance defect justifies restructuring | High |

## Focused CUBE-03 closure

**RETIRE — HISTORICAL COMPATIBILITY ONLY.** The known artifacts are
NovaCore-controlled source-derived content; no current producer, runtime route,
fixture invariant or concrete external support obligation requires payload 1.
Public source/manifest exposure was confirmed, but exposure and possible private
copies do not by themselves establish permanent compatibility. Project Control
explicitly directed forward consolidation once controlled regeneration was proven.

The retained source inputs are unchanged from the original authoring revision.
Current geographic regeneration produced 63 payload-2 records twice with SHA-256
`e365628bbfefef55280c05b70bdc50dd0a40fcf643ef89e006faf15558577e83`
(7,496,116 bytes), covering all seven intended sites; maximum direction
reconstruction error was `1.1102230246251565e-16`. This is deliberate regeneration
under current geographic/address semantics, not a lossless binary conversion.
The older cached payload-2 hash differs because current authoring also performs
the corrected relaxed-cube inverse. Neither output was installed into production.
The known tiny-local invariant already uses the current schema-3 fixture.

Managed record validation and the native reader no longer admit payload 1.
Current schema-2/payload-2 generation and BC4/BC5 primitives remain intact. A
current payload-2 fixture plus generation/reader and invalid-version tests protect
that live responsibility. No payload-1 fixture, reader test, migration utility,
compatibility guide or dedicated historical report is retained by this follow-up.
Git history supplies retired implementation provenance. Existing dated physical
investigation reports retain their continuing regression/measurement value.

The supported [terrain contract](../../terrain-assets.md#nccube-support-contract)
now lists current tuples and authoring modes. No undefined compatibility path was
introduced; existing lower-level parser capability is not expanded into a support
promise. C04 alone changes in the 40-row matrix. Known future-debt rows retain their
prior classification, and no direct consequence introduced a new closure blocker.
KSA cannot determine NovaCore's external support obligation; no new KSA audit was
materially useful for this bounded retirement.

## Validation, scope and storage

Current authorities and their dated evidence were reread. The initial tracked
inventory has 562 files, no empty tracked file and no empty class/interface
candidate in the source scan. These are scoped checks, not proof that every
possible syntactic scaffold is absent. Local/generated roots contain no tracked
files; `.codex` is empty. The six root files are README, engineering rules,
solution, managed defaults, ignore rules and LFS attributes.

All 20 managed projects / 30 project references resolve and are acyclic; the six
source-project directions match the map. Initial relative-link checks covered
58 tracked Markdown files / 207 links, with no missing target. No ceremonial skip
branch was found: Graphics harness separates selected/excluded categories and
reports failures; ordinary LINQ `Skip` occurrences are not skipped regressions.
Existing suite/fixture/source-contract checks protect current invariants and useful
independent references. No fresh all-suite, Vulkan, Florida or performance result
is claimed. That initial documentation-only audit required no builds; the CUBE-03 follow-up
below validates its bounded reader/test changes separately.

Final documentation validation includes this report: 59 Markdown files / 213
relative references resolve, changed-document anchors resolve, all 40 closure
matrix rows have the required columns, and `git diff --check` passes. Nothing
is staged and the banked HEAD/origin/P2S5H identities remain unchanged.

Two obvious current-document defects are corrected: architecture's obsolete
Platform host claim and handoff's already-fixed Graphics assertion. Historical
reports remain unchanged. The existing debt evidence index links this compact
closure report. Those original documentation corrections are preserved. The follow-up narrows
only payload-1 admission; production assets, shaders, cache and root files stay intact.

Read-only storage scan at baseline (logical / allocated bytes):

| Scope | Logical | Allocated |
|---|---:|---:|
| Workspace, 7,946 files | 4,332,094,357 | 4,345,903,984 |
| `build/` | 78,269,291 | 80,810,928 |
| `.novacore/cache/` | 1,012,006,819 | 1,012,031,488 |
| `.git/` | 1,632,313,137 | 1,632,550,928 |
| Permanent engineering evidence | 73,952,891 | 74,023,176 |

Allocation uses FileStandardInfo; directory metadata/alternate streams are excluded.
Six existing earth-data junction targets were not followed; no scan errors.
Build size matches the structure audit; no multi-gigabyte raw-capture accumulation
has returned. Retained evidence is existing compact reports/visuals/provenance,
plus this report (budget 24 KiB). Temporary audit files/captures created: **0 bytes**;
disposable audit files remaining: **0 bytes**. Scripts ran in memory. Final totals
including this report are returned to Project Control.

The prior Git/LFS result remains NO SAFE RECLAMATION AVAILABLE: recent unreachable
objects were inside retention, reachable history/LFS was protected, and history
rewriting was not authorized. This audit does not repeat pruning or object
reachability work. Old LFS payload availability and old cache recovery proof are
bounded future storage responsibilities, not new production ownership conflicts.
KSA comparison was unnecessary because the remaining issue is NovaCore's own
public support decision; its external read-only boundary remains unchanged.

## CUBE-03 final validation

| Corrected-candidate check | Result |
|---|---|
| Native Debug / Release builds | PASS |
| Solution Debug / Release builds | PASS; zero warnings/errors |
| Affected headless Graphics tests | Debug 5 pass / Release 5 pass; zero failures/skips; unrelated cases excluded |
| Current schema-2 producer and geographic site contract | 2 Python tests PASS; full real-input CLI produces 46-record bounded test pack; frozen first record matches reader fixture |
| Current schema-3 fixture generation | 3 Python tests PASS; exact deterministic fixture regeneration |
| Payload/identity, codec, oracle and malformed input | PASS; unsupported version with recomputed valid digest rejected by native and managed boundaries; corruption/truncation/manifest mismatch rejected |
| Known payload-1 object | Native Debug / Release reject; cache untouched |
| Global production verification | 61,484,224 bytes; SHA-256 `38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae` unchanged |
| Florida production verification | 210,307,596 bytes; SHA-256 `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe` unchanged |
| Existing production schema-3 oracle | 278,784 samples; max error 0.001961682224806083 m; prior values unchanged |
| Scope / closure | Only C04 changes among the 40 matrix rows; original architecture/handoff corrections preserved |
| `git diff --check` | PASS |

The five Graphics tests are `Local terrain format and GPU compression`,
`Local terrain payload-2 authoring contract`, `Terrain asset distribution boundary`,
`M12 Florida regional physical surface`, and `Terrain-v5 seams, mixed-LOD authority,
and Florida classification`. Each runs in its own process. Both configurations
proved their actual loaded native DLL matched the corresponding native build:
Debug `c86bb482d8c029246a4e049415377e22bd7fc1342814952c22c872307c074739`;
Release `1742ccf7a28b8a30f7794fcc8d8501d91d779f58bdf1f5bac5db1a2dfc9952a0`.

The current generator, source inputs, production manifests/assets, shaders and
existing fixtures have no diff. No manual Florida, GPU or performance campaign is
needed because no production terrain behavior changed. This does not claim a new
player-facing acceptance result.

CUBE-03 storage: measured temporary generator inputs/packages totaled approximately
244.85 MB of cumulative logical writes, plus small JSON and automatically removed
bounded corruption-test copies. No raw graphics capture was created. Permanent
addition is one current-format fixture (174,624 bytes), current-format test source
(12,417 bytes), and these bounded current-contract/closure documentation updates.
All temporary generation/corruption paths and provisional legacy evidence were
removed; disposable after acceptance: **0 bytes**. Build outputs remain normal
current development dependencies. No cache or Git/LFS cleanup was performed.

**Recommendation:** close Repository Debt Retirement after Project Control review.
No staging, commits, pushes, tags or P2S5H movement.
