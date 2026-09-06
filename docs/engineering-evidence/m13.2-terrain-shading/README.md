# NovaCore M13.2: Specialize ordinary terrain shading

Unbanked production candidate. Measured 2026-09-06; manual acceptance pending Project Control review.

## Baseline and responsibility

`main`, HEAD, `origin/main`, and annotated `m13.1-ncsm1-tes-hotpath` resolve to `fade1384c1c7df93d954e7223b1cc8f17db17f98`. Initially no tracked or staged changes. The two incoming evidence packages are listed and fingerprinted in `baseline.json`; neither is changed. Hardware: RX 6800 XT, Windows driver 32.0.21045.5002, native client 3440 x 1440.

Ordinary NCSM1 already knows its published physical owner and that surface diagnostics are disabled. The shared fragment module nevertheless compiled their dynamic branches, bootstrap addressing and physical diagnostic evaluators into ordinary shading. The minimal correction specializes those two immutable facts at pipeline creation.

| Responsibility | Required work / inputs | Candidate behavior |
|---|---|---|
| Ordinary production | Normal, light direction, response, view direction, body direction, height, camera high/low (locations 1,2,4,5,6,7,8,9); real payload lookup/filtering, FP32 presentation weights, material synthesis, receiver precision, stellar lighting and facility light visibility | Preserved |
| Diagnostic only | Packed `lighting.radianceGlowEnabled.w` selector; owner/address and seam/topology visualization; FP64 modifier/biome evaluation; alternate normal, footprint and depth displays | Compiled out only when ordinary=true; full mode retained for every nonzero context diagnostic |
| Bootstrap only | Dynamic owner tag, analytic ray/sphere addressing and receiver, parent/child texture transactions and edge blending | Separate startup pipeline keeps module default=false |
| Unused linkage | Color 0, material 3 and localDetail 10 already eliminated by baseline driver; owner 11, UV 12, address 13, transition 14, topology 15 additionally unnecessary for ordinary shading | Five additional linked exports eliminated; source interface declarations remain intact |

Bindings 2,24-27 and shared include descriptors are unchanged. Shared push constants remain present because lighting still uses them; only the diagnostic read disappears. No material layer, helper needed by ordinary shading, or physical arithmetic is approximated.

## Implementation and selection

`planetary_production.frag` adds one boolean specialization constant, ID 0, default false. Ordinary=true makes anchored=true and diagnostic=0. `NovaCoreNative.cpp::Swap` supplies it to fragment stage 3 for all four existing NCSM1 raster states precisely when `surfaceDiagnostic==0`. There are no additional pipeline permutations or deployed shader modules. The independent startup pipeline supplies no fragment specialization, so it remains full. M13.1 stage-2 TES specialization is untouched.

| Route/state | Fragment specialization | TES geographic specialization |
|---|---|---|
| Ordinary NCSM1 / Florida / orbital | true | false |
| owners | false | true |
| boundaries (seams) | false | true |
| physical-modifier and every other nonzero surface diagnostic | false | existing diagnostic-specific rule |
| Global terrain-v5 startup, before ready NCSM1 publication | shared module default false | no NCSM1 TES |

Diagnostic selection is immutable for a native context (`SurfaceDiagnosticFromEnvironment` at creation; no setter). Enable/disable is tested by recreating contexts. Focus and publication do not mutate that selection. Draw ownership selects an existing matching pipeline, and swapchain recreation reapplies the same context value. Descriptors, pipeline layout, declared 16-location producer/consumer interface, TES source and topology are unchanged. The compiled regression validates both specialized SPIR-V modules and checks exact location/type/width/interpolation for each surviving fragment input against the TES producer. A producer may legally supply unused outputs; none of the consumed inputs lacks a producer.

Production edits are limited to the native pipeline selection, shared fragment shader, compiled shader regression, and directly related rendering documentation. `instrumentation.patch` is a reproduction attachment, not applied production code.

## Actual production compiler evidence

| Stage / metric | Baseline | Candidate |
|---|---:|---:|
| Fragment VGPR / SGPR | 113 / 70 | 85 / 53 |
| Fragment compiler wave size | 64 | 32 |
| Fragment static instructions | 8,111 | 2,955 |
| Fragment static FP64 mnemonics | 2,934 | 763 |
| TES VGPR / SGPR | 190 / 36 | 190 / 36 |
| TES wave size | 32 | 32 |
| TES instructions / FP64 mnemonics | 7,667 / 3,094 | 7,658 / 3,094 |
| TES parameter exports | 13 | 8 |
| Fragment scratch / LDS bytes | 0 / 0 | 0 / 0 |

The TES difference is four moves and five dead parameter exports; physical FP64 arithmetic is unchanged. VS/TCS merged stage-mask 3 is identical (39 VGPR, 64 SGPR; 32,212 LDS bytes), counted once. TES LDS is 4,608 bytes. Static counts and selected wave size do not measure achieved occupancy or dynamic instruction frequency. `compiler.json` retains complete counts and SHA-256 identities. These were queried from actual pipelines of the production implementation, not inferred from source or the earlier prototype.

| Binary / ISA SHA-256 | Baseline | Candidate |
|---|---|---|
| Fragment SPIR-V | `3c75d16a441d456c7c80279250388cb75b21712e31a17b3e75b6c1902a927241` | `4b79673d14c90df52703a322c116415ec9e833cf2247731ca5ee1ab7b563df06` |
| Fragment driver ISA | `563762f432b481a36a18d981e0544c17031a3cd83b52c82bcf7bde60d38a80c9` | `24808b90a08d718a676c9e0cd3a21094125d1733d92f1777849c6df3da67f320` |
| TES driver ISA | `d6db758b4e309bfc04bc51a3259e9e8fa0f33d3ec7c07c2155a4aa9e42a69c56` | `5458bb37bc54ee428dd4bd53c93fd549d620b4a5ab5ebd17e88e40c28d45cc60` |

## Equivalent 100-sample performance comparisons

All timings are milliseconds, nearest-rank medians from 100 aligned steady-state query samples. Timing runs use production shaders with no capture shader. Temporary host query/ISA instrumentation is identical except for the production specialization selection. Captured parity runs are excluded from performance conclusions. Warm-up/preparation/publication costs are retained separately in measurements and are not attributed to this optimization.

| Pose | Baseline terrain | Candidate terrain | Gain ms | Gain % | Baseline total GPU | Candidate total GPU |
|---|---:|---:|---:|---:|---:|---:|
| Factor 1 | 7.01920 | 6.55116 | 0.46804 | 6.67% | 9.91070 | 10.43715 |
| Active refinement | 25.98032 | 23.83824 | 2.14208 | 8.25% | 29.56071 | 26.68684 |
| Grazing horizon | 15.51556 | 13.95076 | 1.56480 | 10.09% | 18.55319 | 16.97796 |
| Florida | 8.87484 | 8.38616 | 0.48868 | 5.51% | 11.76636 | 11.28760 |
| Orbital | 0.84680 | 0.82812 | 0.01868 | 2.21% | 1.00452 | 0.98392 |

The first factor-1 total-frame result regressed 0.52645 ms while terrain improved. The aligned median remainder outside cull/compact and terrain grew from 0.23984 to 1.15211 ms. Two repeats, including reversed order, did not reproduce it; their remainders matched (0.19589/0.20043 and 0.23718/0.23636 ms). The outlier remains recorded; no particular external process or driver event was measured as its cause. It is not evidence of a repeatable shader regression. GPU clocks and other application activity were not controlled.

| Repeat | Baseline terrain | Candidate terrain | Baseline total | Candidate total |
|---|---:|---:|---:|---:|
| Factor 1 2 | 7.02892 | 6.54920 | 9.87579 | 9.45502 |
| Factor 1 3 | 7.02228 | 6.55216 | 9.92305 | 9.45051 |
| Active refinement 2 | 25.99880 | 23.85552 | 28.82188 | 26.70649 |

Active draw improvement is 2.14208 and 2.14328 ms in the production pairs, reproducing the approved opportunity. No repeatable terrain regression was observed. P95, ranges, timestamps and complete runtime/pose identities are in `measurements.json`.

## Exact physical and attachment parity

For each fixed pose, frame 175 compares the complete prepared GPU buffer and canonical sets of final TES FP64 body positions/directions, FP32 clip positions, barycentrics and near-field terms. Dispatch-order patch IDs are not geographic identity and are excluded from the 112-byte physical key. The complete 128-byte record set and raw capture hashes are retained as provenance; differing compaction IDs and duplicate execution counts are not presented as identical.

| Pose | Baseline TES records captured | Candidate TES records captured | Unique physical directions | Physical set / prepared bytes | Depth/HDR/image differences |
|---|---:|---:|---:|---|---|
| Factor 1 | 1,082,850 | 1,082,850 | 181,640 | exact / 45,574,784 exact | 0 / 0 / 0 |
| Active refinement | 2,779,244 | 2,779,283 | 1,913,891 | exact / 45,574,784 exact | 0 / 0 / 0 |
| Grazing horizon | 3,470,039 | 3,470,035 | 1,801,715 | exact / 45,574,784 exact | 0 / 0 / 0 |
| Florida | 884,244 | 884,244 | 148,371 | exact / 45,574,784 exact | 0 / 0 / 0 |
| Orbital | 46,128 | 46,128 | 7,793 | exact / 884,864 exact | 0 / 0 / 0 |

Each image compares 4,953,600 D32 depth values, 19,814,400 HDR half values and 19,814,400 final image byte values. Every compared byte matches; depth mismatch count and maximum absolute delta are zero. Startup frame 8 independently matches all attachments while globalOwner=1 and candidateOwner=0; there are no TES records then, so startup is an attachment/ownership proof, not a fictitious physical-record measurement.

Florida retains generation-4 H=15.134892258793116 m, slab root=21.970461536198854 m, foundation depth=6.835569277405739 m and 0.25 m embed. Its regional package SHA is `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`. All 670 required records are ready before publication, with 93,392,640 bytes uploaded. Facility geometry/support/light visibility source and shaders are unchanged; exact Florida prepared geometry and full-frame depth/HDR/image include their visible interaction.

## Workload parity

| Pose | Selected triangles = TCS patches (both) | TES baseline median | TES candidate median | Inner factor histogram (both, seven bins) |
|---|---:|---:|---:|---|
| Factor 1 | 360,950 | 1,082,850 | 1,082,850 | `(360950,0,0,0,0,0,0)` |
| Active refinement | 2,060 | 2,779,257 | 2,779,262 | `(97,120,157,255,344,372,715)` |
| Grazing horizon | 378,919 | 3,470,007 | 3,470,000 | `(377285,138,165,234,270,240,587)` |
| Florida | 294,748 | 884,244 | 884,244 | `(294748,0,0,0,0,0,0)` |
| Orbital | 15,376 | 46,128 | 46,128 | `(15376,0,0,0,0,0,0)` |

Every one of the 100 aligned factor histograms matches per pose. Maximum outer factors remain 1 or 64; inner factors remain 0.9999 or 63.993599. Compute/TCS/TES SPIR-V, topology inputs, prepared buffers and canonical final physical samples are unchanged. Small active/grazing TES execution-count variations overlap within both runs (ranges retained), including baseline-to-baseline variation. Counts include repeated evaluations and are not unique topology. Atomic compaction IDs likewise vary. AMD clipping/fragment query counts show small raster-accounting differences despite bit-exact attachments; they are not treated as a count of the complete generated topology. No patch or factor reduction explains the speedup. Generation/pupil/prepared/cull/TES/raster identity and sole-owner publication checks pass in fixed-pose runs.

## Nonordinary output proof

Owners, boundaries and physical-modifier each match baseline depth/HDR/image exactly using the full fragment specialization. Owners and boundaries retain the TES geographic specialization. The private negative control forces ordinary=true despite the diagnostic request: owner HDR differs by 214,177 bytes and final image by 107,447; seam HDR differs by 211,425 bytes and final image by 103,867. Physical sets and depth remain exact in both controls. These deliberately output-altering controls establish observable diagnostic behavior, not a production approximation.

Bootstrap frame 8 output is byte-exact; later frames publish the ordinary NCSM1 owner. Context recreation covers ordinary → owners → seams → ordinary → physical-modifier, with explicit creation logs. Other retained modes select the same complete nonordinary module; they are not individually recaptured.

## KSA relationship

**ADAPT.** Reuse the already collected [production/source and official history comparison](../active-refinement-usefulness/ksa-comparison.md). The compatible responsibility is compiling immutable ordinary work without unused features and measuring actual register pressure. NovaCore retains its FP64 H, geographic precision, complete material fidelity, one-owner publication and 50 m refinement. No KSA source is copied and no new broad audit is conducted.

## Reproduction and evidence lifecycle

Use a disposable checkout of this candidate with the unchanged two incoming evidence packages available. `reproduce.py prepare` applies the retained temporary patch, builds a baseline native snapshot with the specialization selection removed, compiles the banked fragment, then rebuilds/deploys the instrumented candidate. `reproduce.py timing`, `repeats` and `parity` reproduce the named batches. `consolidate.py` checks and summarizes results. `reproduce.py restore` removes instrumentation; `validate.py build`, `tests`, `routes`, `transitions` validate normal production deployment. NumPy is needed only for capture analysis. Standard prerequisites are .NET 10, VS C++/CMake/Ninja, Vulkan SDK and installed verified production assets.

Runners create isolated private deployments below `build/m13.2-terrain-shading`, hard-link immutable Earth data, and remove each private deployment/raw capture after hashing and analysis. Normal production shaders and assets are never substituted in place for capture. Canonical child processes use SDK Khronos validation and process-local empty implicit-layer discovery. No VUID or error is filtered. Existing suite rules for the specifically known unused vertex-output warning remain unchanged.

Retain compact reports, measurements, hashes, this reproduction tooling/patch and final validation identity. Additional permanent evidence budget: 3 MB. Do not retain raw frame/TES archives or compiler listings already recoverable by hash and reproduction. The earlier prototype listings remain in the preserved incoming evidence package.

## Final uninstrumented validation

| Gate | Debug | Release |
|---|---|---|
| Native build | PASS | PASS |
| Managed solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Focused headless contracts | 6 pass, 0 fail, 0 skip | 6 pass, 0 fail, 0 skip |
| Managed GPU, no window | 8 pass, 0 fail, 0 skip | 8 pass, 0 fail, 0 skip |
| Window lifecycle + live regional contract | 2 pass, 0 fail, 0 skip | 2 pass, 0 fail, 0 skip |
| Native GPU visibility / material coordinates | 2 pass, 0 fail, 0 skip | 2 pass, 0 fail, 0 skip |

The focused set covers the compiled shader/workload contract, facility support, canonical physical surface, generation-4 renderer, Earth routes and material noise. Other tests are exclusions, not skips or claimed passes. GPU tests use the existing retained NCSM1 artifact-input mode with all assertions intact.

The full traversal passes 1,701 frames across L0-L17, 10 pupil snaps, three scale reversals, anchored/unanchored warp, orbital retreat and surface reapproach. It publishes 58 generations with zero-owner=0, overlap=0, stale=0; maximum measured height and normal parity errors are zero. The creation log confirms ordinary specialization. Each configuration's regional window test checks staged readiness before publication, multiple pupils, L16/L17, Florida departure/reentry, 700 km orbit, Mars/Saturn focus and non-Earth isolation. All required 670 records remain authoritative. Window lifecycle creates both styles twice and checks presentation, resize, minimize, restore and teardown.

All 15 Release launcher regressions pass. Six route probes resolve the normal current Release executable: Solar System Overview, Earth Far / Orbital View, Earth 700 km, Earth — Fullscreen Native, Florida Launch Site and New Earth Renderer. The Florida plan is exactly `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. Its 500-frame smoke passes ready publication, sole ownership and stable seating at frame 440, without capture/probe flags.

Both required assets verify Valid: `earth-surface-v5` SHA-256 `38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae`; `earth-florida-m12` SHA-256 `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`. Production elevation, manifests, generators and cache are preserved.

`final-validation.json` retains 30 successful command records and actual loaded-test native identities. `final-identity.json` records both runtime deployments, 49 runtime shaders per configuration, 64 build shaders consumed directly by configuration-specific tests, managed/runtime/launcher hashes and elevation identity. Debug/Release shader binaries agree; 48 runtime binaries remain M13.1-exact and the changed fragment is correctly deployed. The normal Release Launcher uses the final uninstrumented candidate. Measurement hosts have separate fingerprints in `measurements.json`.

Strict Vulkan validation passes without VUIDs or validation errors. Normal builds retain the pre-existing `WARNING-Shader-OutputNotConsumed` for generic vertex location 11, matched narrowly by the unchanged permanent suite. Temporary TES capture instrumentation additionally exports unused location 16; this produces a capture-only unused-output warning and duplicate-message-limit notice. These recorded warnings do not represent missing inputs and are absent from final production deployment. No new production warning filter or error suppression is introduced.

`git diff --check` passes. Nothing staged, committed, pushed or tagged; M13.1 references remain unchanged. Project Control has not waived manual acceptance. This candidate is ready for review and subsequent normal Launcher acceptance; no new manual pass is claimed.

## Cleanup limitation and return classification

All production, parity, performance and dynamic-transition gates are complete.
Automatic approval review rejected removal of the 260 verified ignored files in
`E:\NovaCore\build\m13.2-terrain-shading`; the stated reason was only "blocked by
policy". No alternate deletion mechanism was attempted. The files remain in place
and are identified by `disposable-manifest.json`: 53,416,461 logical bytes of
reproducible diagnostic logs/ISA, private baseline snapshots and the disposable
route-probe build. They are separate from the normal Launcher/runtime deployment
and required assets. User-performed cleanup of this exact directory is the sole
remaining task. The report does not claim successful cleanup or new manual
visual acceptance.

**PARTIAL M13.2 IMPLEMENTATION — FOLLOW-UP REQUIRED**

## Storage accounting

`storage.json` records the logical file-payload ledger. The 20 attachment/TES capture runs created 4,475,821,056 bytes of raw data, all removed by the isolated runners after analysis. Another 53,416,461 bytes in 260 ticket scratch files remain because cleanup was rejected. Permanent evidence stays below the declared 3 MB budget. Diagnostic output remaining is therefore nonzero; there is no claim of a fully cleaned workspace. Ordinary native/.NET rebuild outputs and transient private deployment copies are excluded from this diagnostic-payload ledger. Existing regional regression tests also create and self-remove bounded probe files; their byte volume was not separately metered. `created` is the metered ledger, not an estimate of cumulative disk writes or total workspace allocation.
