# NCSM1 TES geographic-address removal candidate

**PERFORMANCE OPTIMIZATION CANDIDATE READY FOR MANUAL ACCEPTANCE**

Automated gates passed. Project Control retains the manual-acceptance decision. Nothing staged, committed, pushed or tagged.

## Baseline and boundary

- HEAD and origin/main: `219ec3c3f4f1db0496e7ef6204d510a1bd337aeb`, branch `main`.
- P2S5H (`m12d-p2s5h-earth-route-convergence`) remains at `32ffac50ab5c06518ede24edfb5c531976d4ec99`.
- Initial tree: only `docs/engineering-evidence/near-surface-performance/` untracked; production source clean. All 49 deployed shader hashes matched the preceding investigation.
- AMD Radeon RX 6800 XT, driver `32.0.21045.5002`, native 3440 x 1440.
- Fixed J2000 Earth/NCSM1 generation-4 route; A orbital L0, B representative factor-1 L17 (136.19158 m), C Florida launch viewpoint, D 10.004 m downward active refinement, E 10.004 m grazing horizon. Exact arguments, environment, camera/body quaternion, view-projection, pupil, publication and shader identities are in [measurements.json](measurements.json).

The user authorized this one production optimization. No refinement, topology, culling, physical H, support, residency, material, lighting, route or publication policy was changed. The sample pose instrumentation was restored completely. P2S5H remains the banked production authority.

## Consumer audit and implementation

`production_spherical_billboard.tese` interpolates prepared camera-relative physical vertices, reconstructs the FP64 body direction, evaluates the existing bounded near-field displacement, then formerly called `ProductionDirectionAddressD` unconditionally. The inverse helper performs up to eight relaxed-cube Newton iterations, including repeated spherify/normalization. It produced face, UV, level, cell and local coordinates.

| Responsibility | Actual consumer / decision |
| --- | --- |
| Final position, physical H and normals | Prepared base plus existing generation-4 near-height/gradient; no inverse-address output consumed. Unchanged. |
| Regional physical data | Participates in authoritative prepared physical vertices; demand/residency/publication and all prepared bytes are unchanged. |
| Facility support | Existing grading and near-field modifier; unchanged. Florida sampled support weight and prepared geometry match exactly. |
| Ordinary material receiver | NCSM1 sets anchored layer bit `0x40000000`; fragment resolves material address from final physical `bodyDirection`. No TES address required. |
| `productionUv` (location 12) | Mixed-LOD blend only in the nonanchored fragment branch, supplied by other vertex paths. Ordinary NCSM1 may supply zero. |
| `productionAddress` (location 13) | Owners bit 32 hashes face/level/cell. Boundaries bit 64 classifies face edges. Nonanchored material branch also consumes it, but is outside this TES owner. Retain inverse only for owners/boundaries. |
| `topologyCoordinate` (location 15) | Boundaries bit 64 uses local UV and derivatives. Retain inverse for that diagnostic. |
| Addresses diagnostic bit 256 | Uses fragment `resolvedAddress`, not the TES address. Unchanged. |
| Publication/coverage/generation telemetry | Actual indexed draw and transactional publication own these invariants, not diagnostic address colors. Unchanged. |
| Other diagnostics | Physical normals, depth, material/layer isolation and related outputs do not consume the removed ordinary TES coordinates. |

The TES now declares boolean specialization constant ID 0, default false, around only the inverse/address output calculation. Physical calculations and the 16-location fragment interface remain shared and unchanged. There is one deployed SPIR-V binary, not two physical algorithms or an additional renderer.

Native pipeline creation specializes that binary from `App.surfaceDiagnostic`, fixed once before pipeline creation. Only owners/boundaries enable it. All four existing raster-state pipelines use the same value; resize/recreation preserves it. Submitted lighting rejects the upper 16 bits, so a frame cannot silently introduce diagnostic flags. There is no per-frame toggle, dynamic TES branch, extra pipeline object, or ABI change. Startup logs identify the selected specialization.

The permanent compiled-SPIR-V regression verifies ID 0 defaults false and specializes both ways: dead-code elimination removes the inverse function only from ordinary code and retains it for diagnostics. SDK `spirv-opt` and `spirv-dis` are test tools, not runtime dependencies.

## Physical, depth and presentation parity

Paired baseline/candidate runs captured the actual completed frame 175 under strict Vulkan validation. Temporary instrumentation copied D32 depth (from D32_SFLOAT_S8_UINT), scene RGBA16F, final swapchain four-channel 8-bit image, prepared physical vertices and bounded post-TES samples. Same camera, quaternion, view-projection, pupil 1, publication generation 1 and physical generation 4; no cross-pose comparison. Capture store/transfer/readback changes were identical on both sides and removed before final deployment.

TES samples are keyed by control-vertex IDs and exact barycentric coordinates, independent of atomic compact order. A stable first-vertex modulo-128 subset includes interior refined points; duplicate invocations at the same key are compared as one physical location. Recorded fields include final camera-relative position, body direction, height, clip position, normal, near weight, facility-support weight, base height, near height and local displacement.

| Pose | Distinct TES locations | Near-field locations | Facility-support locations | Max / RMS position delta (m) | Max elevation delta (m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| B | 7,251 | 0 | 0 | 0 / 0 | 0 |
| C | 6,630 | 0 | 350 | 0 / 0 | 0 |
| D | 16,092 | 16,092 | 0 | 0 / 0 | 0 |
| E | 20,406 | 12,774 | 0 | 0 / 0 | 0 |

All 50,379 compared locations and their recorded physical fields are bit-identical, with identical key sets. Each pose also compares 712,106 prepared vertices (45,574,784 bytes) byte-for-byte, including the Florida regional/support terrain. The separate regional component is not reconstructed from image data: parity is established on the full authoritative prepared result, unchanged regional shader/source, residency-ready provenance and the physical authority regressions.

| Comparison | Per pose | Four-pose total | Exact matches | Mismatches | Max / RMS delta |
| --- | ---: | ---: | ---: | ---: | ---: |
| D32 depth values | 4,953,600 | 19,814,400 | 19,814,400 | 0 | 0 / 0 |
| HDR channel values | 19,814,400 | 79,257,600 | 79,257,600 | 0 | 0 / 0 |
| Final 8-bit channel values | 19,814,400 | 79,257,600 | 79,257,600 | 0 | 0 / 0 |

All mismatch bounding boxes are empty and every 8x8 spatial mismatch bin is zero. These exact frames expose no terrain movement, seam/owner boundary, horizon, material-coordinate, Florida contact or regional appearance change. This is fixed-pose numerical acceptance, not a claim about a new manual traversal.

## Owner/seam observability

At the orbital diagnostic pose, baseline versus candidate owner and boundary images, depth and 366 TES sample locations match exactly. The depth readback contains 35,913 nonclear values, establishing actual raster content. Negative controls deliberately force the candidate specialization false while the fragment requests the diagnostic:

| Diagnostic | Final channel mismatches | HDR channel mismatches | Depth mismatches | Physical change |
| --- | ---: | ---: | ---: | --- |
| Owners negative control | 71,419 | 71,756 | 0 | None |
| Boundaries negative control | 107,212 | 107,675 | 0 | None |

The visualization can therefore still observe wrong address outputs; it does not merely compile. These controls perturb diagnostic coordinates, not physical ownership. Actual one-owner/coverage/stale-generation/publication invariants remain covered by the production runtime/physical regressions and Florida/regional smoke results below.

## Performance

Sequential baseline/candidate runs, last 100 settled terrain-draw samples each, native resolution and exact workload fingerprint. These use the production TES without capture SSBO code. Temporary CPU log/query support is identical in the native host; no other GPU workload was started by this task concurrently. Capture runs have substantial extra register/readback cost and are deliberately excluded from these timings.

| Pose | Baseline terrain ms | Candidate terrain ms | Saved ms | Saved % | Baseline total GPU ms | Candidate total GPU ms | Terrain p95 B / C ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 2.05536 | 0.89936 | 1.15600 | 56.24 | 2.46416 | 1.33016 | 2.25840 / 1.08484 |
| B | 19.40024 | 7.02820 | 12.37204 | 63.77 | 22.75111 | 10.08956 | 19.68656 / 7.09936 |
| C | 18.46044 | 8.87656 | 9.58388 | 51.92 | 21.91729 | 12.10697 | 18.67972 / 8.97408 |
| D | 42.54912 | 25.97472 | 16.57440 | 38.95 | 45.79554 | 29.11564 | 42.92608 / 26.11332 |
| E | 29.54540 | 15.49636 | 14.04904 | 47.55 | 32.92296 | 18.87296 | 29.86520 / 15.60320 |

| Pose | TCS patches B = C | TES invocations B / C | Fragment invocations B / C | CPU submit B / C ms | Fence wait B / C ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| A | 15,376 | 46,128 / 46,128 | 35,913 / 35,913 | 0.0440 / 0.0447 | 2.7993 / 2.8940 |
| B | 360,950 | 1,082,850 / 1,082,850 | 2,591,685 / 2,591,684 | 0.0655 / 0.0502 | 22.7034 / 10.2324 |
| C | 294,748 | 884,244 / 884,244 | 3,984,320 / 3,984,331 | 0.0604 / 0.0595 | 21.8944 / 12.0898 |
| D | 2,060 | 2,779,256 / 2,779,257 | 4,953,599 / 4,953,599 | 0.0752 / 0.0651 | 45.7110 / 29.0888 |
| E | 378,919 | 3,470,006 / 3,469,997 | 2,472,099 / 2,472,103 | 0.0654 / 0.0569 | 32.8802 / 18.8188 |

| Pose | Clipping input B / C | Clipping output B / C |
| --- | ---: | ---: |
| A | 12,893 / 12,897 | 12,893 / 12,897 |
| B | 142,663 / 142,656 | 142,663 / 142,656 |
| C | 119,648 / 119,645 | 119,644 / 119,641 |
| D | 953,912 / 953,917 | 946,137 / 946,137 |
| E | 245,736 / 245,743 | 235,569 / 235,572 |

Selected/compacted patches, all factor bins and maximum outer/inner factors agree. A/B/C are factor 1 (inner 0.9999); D/E retain outer 64, inner 63.993599. D bins: `(97,120,157,255,344,372,715)`; E bins: `(377285,138,165,234,270,240,587)`. Topology, publication hash and all culling counts agree.

D/E TES median differences are +1 and -9 respectively, inside overlapping unchanged-pose invocation ranges (D baseline 2,779,183–2,779,319, candidate 2,779,185–2,779,315; E baseline 3,469,943–3,470,076, candidate 3,469,947–3,470,072). Driver clip counters also vary frame to frame; their small median differences are not a generated-topology change. Fragment median differences are 0/-1/+11/0/+4 for A–E, inside the same-run ranges. Exact sampled physical positions, complete depth and image equality independently establish unchanged raster coverage. No factor or quality reduction was used.

Primitive clipping input/output medians, complete ranges/p95s, CPU record/submit/present, preparation slices and frame-aligned waits are retained in measurements.json. Clip counters are not unique selected triangle counts and fragment invocations are not unique pixels or a direct overdraw ratio. Fence wait N+1 waits for GPU work N; it is not an additional independent cost. Orbital presentation pacing can dominate CPU wait despite lower GPU time. Periodic regional work and synchronization tails remain separate from steady terrain draw.

The measured near-surface gains reproduce the accepted address-bypass diagnosis. The ordinary compiled shader contains no inverse function after specialization: the gain comes from less work per TES invocation.

## KSA relationship

**ADAPT responsibility.** The previously collected local KSA production evidence separates prepared terrain data from expensive raster displacement; its TES returns prepared data beyond the configured 50 m range and has no equivalent unconditional eight-iteration relaxed-cube inverse for diagnostic-only outputs. The relevant preparation/culling history and exact source pointers remain in the [preceding diagnosis](../near-surface-performance/README.md#ksa-responsibility-comparison). A focused read reconfirmed the far return in local `Content/Core/Shaders/Planet/PlanetTessEvaluation.tese` lines 77–89 (SHA-256 `80c3168561c468d3b255d635b31c1e0163ed8331c21e1340b8d490525bc93b27`). No new broad research or proprietary source copying was performed.

NovaCore keeps canonical generation-4 H, FP64 reconstruction, analytical gradients, facility support, conservative culling and the existing 50 m contract. This applies KSA's responsibility boundary; it does not claim KSA has this exact specialization implementation or replace NovaCore authority with KSA texture sampling.

## Validation and deployment

[validation.json](validation.json) retains the exact commands, pass records, runtime paths, all shader hashes, six launcher plans and smoke provenance.

| Gate | Debug | Release |
| --- | --- | --- |
| Native build | PASS | PASS |
| Managed solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Graphics headless | 79 pass / 0 fail / 0 skip | 79 pass / 0 fail / 0 skip |
| Graphics GPU | 8 pass / 0 fail / 0 skip | 8 pass / 0 fail / 0 skip |
| Regional physical window | 1 pass / 0 fail / 0 skip | 1 pass / 0 fail / 0 skip |
| Native GPU | 2 pass / 0 fail / 0 skip | 2 pass / 0 fail / 0 skip |
| Compiled specialization/interface | PASS within headless | PASS within headless |

This focused Graphics surface executes 88 cases per configuration. The three unrelated generic-window/lifecycle/input cases were outside this shader ticket. Physical authority, prepared height, natural terrain, Florida support/seating, topology/seams, ownership, zero-visible/re-entry and publication contracts passed. No physical assertion was weakened.

- Launcher regression: **15/15 PASS**.
- Six deployed `CreatePlan` probes: **PASS**, including Solar Overview, Earth Far/Orbital, Earth 700 km, Earth Fullscreen Native, Florida and New Earth Renderer. These are actual deployed-launcher configuration probes, not six independent visual acceptance traversals.
- Florida configured launcher route: **500-frame smoke PASS**, complete regional readiness, stable seating at frame 440, one owner, zero invalid/stale/overlap/missing-owner draws, zero VUIDs.
- Final ordinary Florida route after the rejected cleanup attempt: **120-frame smoke PASS**, ready generation-4 publication, zero VUIDs. No capture/investigation flags or physical probe were enabled.
- Both production terrain packs verified against their manifests again after the cleanup attempt; deployed elevation oracle SHA-256 `4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317` matches source in both configurations. Production assets, generators and permanent fixtures remain unchanged.
- All **98 deployed production shader files** (49 per configuration) match their corresponding native builds. Only `production_spherical_billboard.tese.spv` differs from baseline. Graphics proof tests load their matching build's shader directory and their correctly deployed native DLL; native-only fixture shaders are not expected in the sample runtime.
- Final TES SHA-256, Debug and Release: `3b64eca169c95f00b1d375d380de8b38d18a2ae00dce0cc640f7621fc1df3ed5`.
- Final native Debug DLL: `2bfa9b8e9d200f303c4614a47da4418a6e1817455f192ed92fb3be5d36e01c3c`.
- Final native Release DLL: `7c4b9e8a78b3b18e5a9c143e176f51eab345de1d61862089e2d432613e06e8dd`.
- Release Triangle managed DLL remains baseline-identical: `2a9b5d865e7d7873d860c2c8cedb695d64633212c30d9247501d94677d7defb4`.
- After removing every temporary host/pose change, a further 100-sample B run on that exact final native/managed/TES identity measured **7.07164 ms terrain median, 7.12892 ms p95**. Same camera/publication/workload fingerprint. This corroborates the instrumented-host production pair without retaining instrumentation in the candidate.
- Strict Khronos Vulkan validation stayed enabled in controlled process-local layer environments. No VUID filtering or global loader/registry change was used.
- `git diff --check`: **PASS**.

The normal Release launcher remains `E:/NovaCore/tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`. The **Florida Launch Site** preset resolves to `E:/NovaCore/samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe` with exactly `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. Launch/deployment behavior was not changed.
 The first broader Debug headless run was deliberately interrupted during unnecessary offline NCSM1 topology regeneration; it is not counted as a completed PASS. The bounded rerun uses the existing `NOVACORE_P2S5F_ARTIFACT_INPUT` production library mode with all numerical, topology, serialization, corruption and selection assertions retained. No production test assertion or asset was changed to accommodate the run.

## Reproduction and evidence lifecycle

Budget: 1 MiB for this compact candidate package. Retain this report, baseline/hash provenance, paired numerical results, final validation/deployment identities and reusable capture/validation tools. The permanent compiled shader regression lives in the existing Graphics interface test. No raw frames or reference images are needed permanently for zero-difference comparisons.

For reproduction, use an isolated checkout containing the candidate and the original baseline commit. Apply the preceding `near-surface-performance/instrumentation.patch`, then run `python docs/engineering-evidence/tes-address-removal/capture_host.py` from its root. Build native Release with the Visual Studio developer environment and build Triangle Release. Use Python with NumPy, Vulkan SDK glslc and:

```powershell
python docs/engineering-evidence/tes-address-removal/validate.py performance ABCDE
python docs/engineering-evidence/tes-address-removal/validate.py parity BCDE
python docs/engineering-evidence/tes-address-removal/validate.py parity A --diagnostic owners
python docs/engineering-evidence/tes-address-removal/validate.py parity A --diagnostic owners --negative
python docs/engineering-evidence/tes-address-removal/validate.py parity A --diagnostic boundaries
python docs/engineering-evidence/tes-address-removal/validate.py parity A --diagnostic boundaries --negative
```

The runner pins the original baseline commit for shader retrieval, creates an isolated runtime, hard-links only the read-only oracle, records compact measurements and retires its single-frame raw captures before the next run. It never changes deployed shaders. Owner/seam negative controls exist only in that isolated runtime. Baseline native mode logs describe the requested specialization; old baseline SPIR-V has no specialization ID and still executes the inverse. Actual per-run shader hashes are the authority for that distinction.

Restore all temporary host/pose edits before final builds. `final_verify.py build`, `tests`, `identity`, `routes`, and `post-smoke` reproduce the final production gates using existing canonical Vulkan layer isolation and the retained six-route launcher recipe. A missing raw capture or skipped test is not a pass.

## Storage and remaining temporary output

Measured logical-file accounting (not allocated workspace size or cumulative write I/O):

- Diagnostic output created: **1,658,916,382 bytes**, consisting of 1,655,960,192 bytes of single-frame raw readbacks, 2,321,625 bytes of this ticket's persistent temporary output, and 634,565 bytes of retained evidence.
- Permanent evidence retained: **634,565 bytes**, below the 1 MiB package budget. The preceding 672,442-byte evidence package is preserved separately and is not counted as new output.
- Disposable raw output removed: **1,655,960,192 bytes** (1.656 GB). No raw GPU frame archive remains.
- Reviewed persistent cleanup removed: **0 bytes**.
- Disposable persistent output remaining: **6,955,549 bytes** across **112 files**. All hashes were rechecked after rejection; none changed or disappeared.

| Remaining group | Files | Bytes | Git state / disposition |
| --- | ---: | ---: | --- |
| `build/near-surface-performance/` | 32 | 4,625,070 | Ignored; eight old logs and 24 summarized run JSON files |
| `docs/engineering-evidence/near-surface-performance/__pycache__/analyze.cpython-311.pyc` | 1 | 8,854 | Untracked, not ignored; generated bytecode, exclude from banking |
| `build/tes-address-validation/` | 79 | 2,321,625 | Ignored; summarized results/logs, offline SPIR-V inspection, recovery source copy, rebuildable route-probe output |

Exact paths, sizes, hashes and classifications are in [prior-temporary-inventory.json](prior-temporary-inventory.json) and [current-temporary-inventory.json](current-temporary-inventory.json). None is required by production, builds, permanent tests or asset generation. The retained scripts/reports supply their continuing reproduction value. No ignore-policy change is warranted; the retained runners disable bytecode generation where imported modules are used, and `build/` is already ignored.

**Automatic approval review rejected deletion of the reviewed 112 files with “blocked by policy”; no more specific reason was supplied. The process did not start. No deletion was retried through another mechanism.** The fresh raw-capture temporary runtimes had already completed their ordinary cleanup before this separate rejection. Final runtime/shader/asset verification and the 120-frame Florida smoke passed afterward. These small inventoried leftovers do not invalidate the production optimization and must not be banked as evidence.

Per-run isolated executable/shader copies, compiler overwrites, ordinary test/build intermediates outside this ticket's directory, and stdout summarized only in memory were not cumulatively metered. They are excluded from the measured created/removed totals, rather than assigned invented byte counts. Production deployment/cache and unrelated workspace storage were untouched. See [storage.json](storage.json) and [manifest.json](manifest.json).

## Final Git status and review boundary

```text
 M docs/planetary-rendering.md
 M native/NovaCore.Native/NovaCoreNative.cpp
 M native/NovaCore.Native/shaders/production_spherical_billboard.tese
 M tests/NovaCore.Graphics.Tests/PlanetaryBillboardSurfaceWorkloadTests.cs
?? docs/engineering-evidence/near-surface-performance/
?? docs/engineering-evidence/tes-address-removal/
```

Production changes are limited to nine native pipeline-selection lines and the TES specialization/address block. The permanent compiled regression and rendering documentation explain/protect that boundary. The preceding performance evidence was preserved. No temporary native or managed pose instrumentation remains. HEAD, origin/main, branch main and P2S5H remain unchanged; the index is untouched.

**PERFORMANCE OPTIMIZATION CANDIDATE READY FOR MANUAL ACCEPTANCE**

Stop for Project Control review. No manual acceptance, banking or further optimization was performed.
