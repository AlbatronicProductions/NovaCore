# Conservative pre-refinement visibility proof — recovery and closeout

**NO SAFE M13.3 TARGET YET**

The final field-derived, outward-rounded bound passes its independent containment, zero-false-reject and exact physical/depth/HDR/image comparisons. The diagnostic implementation saves approximately 1 ms total GPU in the two refined fixed views. Control and incoming-publication percentile non-regression is not established: the first Florida comparison worsened, its reversed repeat changed that result, and incoming-publication P95 rose slightly. The strict decision rule therefore does not permit an implementation-ready classification. No M13.3 is assigned. M13 remains OPEN.

## Repository and recovery checkpoint

- HEAD and origin/main: `4accf92fd080c16cc8656080aa69def3fa65db53`.
- Branch: `codex/m13-next-performance-target`.
- M13.1 `m13.1-ncsm1-tes-hotpath`: `fade1384c1c7df93d954e7223b1cc8f17db17f98`.
- M13.2 `m13.2-ordinary-terrain-shading`: `4accf92fd080c16cc8656080aa69def3fa65db53`.
- Ticket start: no tracked or staged changes; only the pre-existing `docs/engineering-evidence/post-m13.2-next-target/` was untracked. That evidence package was preserved.
- Recovery found the two expected temporary instrumented source files and no staged changes. `recovery-checkpoint.json` records refs, full status, diff hash, artifact identities and incomplete directories.
- Last completed pre-restart checkpoint: `sphere-D-active`, 2026-09-06 21:27:33 local. Its original metadata-sized sphere proposed no rejects. The interrupted next run had no durable complete log. Two preliminary post-restart runs are explicitly unqualified in the recovery ledger.
- Recovery rebuilt native Release with `--clean-first`, rebuilt Triangle, verified all 49 deployed shaders against the banked identities and validated SPIR-V. Each private diagnostic shader was compiled again. The final field-derived bound was then run through its own complete matrix.
- The earlier angular traversal is exploratory evidence only. It is not counted in any final-bound pass below. The original 11.81 m metadata sphere is also a distinct variant.
- The prior `build/post-m13.2-next-target` directory was already absent and was not recreated.

## Host freeze and previous access violation

The system freeze remains an **UNCLASSIFIED host/diagnostic event**. Windows booted at 21:29:53 on September 6. Kernel-Power 41 reports BugcheckCode=0; WER records two LiveKernelEvent 141 watchdog dumps named `WATCHDOG-20260906-2128.dmp` and `WATCHDOG-20260906-2129.dmp`. Code 0x141 records a display-engine timeout, not an attribution to NovaCore or proof of a successful driver reset. No Display 4101 event was found in the inspected interval. Direct watchdog dump and matching ReportQueue access was denied; no ACL or privilege changes were made. See `restart-events.json`, `recovery-checkpoint.json`, and [Microsoft’s 0x141 definition](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x141---video-engine-timeout-detected).

No Resource-Exhaustion-Detector 2004 event was found. Recovery free space was approximately 435 GiB on E: and 84 GiB on C:, with approximately 19.2 GiB RAM free out of 31.1 GiB. These are recovery observations, not historical peak measurements. The interrupted run had no bulk raw capture enabled; its oracle buffer was 96,000,016 bytes. Storage exhaustion is unsupported by the evidence. Transient driver/compiler or memory pressure remains unmeasured. One later AppHangTransient report for NovaCore.Triangle appeared at 22:05:40 without module/dump attribution; retain it as a diagnostic interactivity observation, not a new established production cause. No further 0x141 or access-violation report was found in the post-recovery interval inspected.

The earlier 20:16:45 diagnostic access violation is separately classified from an actual user minidump. DXCore+0x169f2 read an address inside already-unloaded D3D12Core.dll; the main thread was simultaneously unloading the AMD Vulkan driver through Vulkan teardown. The measured failure is an external module-unload race, not a stale mapped terrain readback stack. The private diagnostic process pinned the already-loaded AMD driver module until process exit while preserving normal Vulkan object/device/instance teardown. Repeated captures, oracle runs and exact-comparison runs then completed. This mitigation was removed with the temporary source patch; it is not a production change. See `driver-teardown-evidence.txt`, `driver-teardown-identity.json` and `dump_inspect.cpp`.

A bounded 406,641-byte `interrupted-runtime.zip` preserves the exact changed runtime/shader overlay for the unresolved watchdog investigation. `recovery-instrumentation.patch`, the overlay manifest and the banked dependency hashes preserve the reproduction boundary. User crash dumps and Windows reports were not deleted.

## Mathematical and ownership proof

See [the complete derivation](bound-proof.md), `envelope.py`, `regional-range-proof.json` and `conservative_bound.glsl`. Prepared vertices already contain global elevation, regional residuals, nonnegative geographic recomposition, generation-4 macro/meso terrain and facility support. The only additional TES position displacement is the bounded near field. No global mountain-height estimate or extra physical owner is introduced.

The exact-arithmetic unit-gradient field bound is sqrt(3)/2, proved from the quintic interpolation weights. The largest near amplitude is 11 m. Explicit rounding, blending, support attenuation and range-fade bounds produce **9.536805152893066 m**, rounded upward, plus a per-patch base-height cancellation bound. The existing 11.8125865 m metadata remains a checked covering contract. The final bound uses actual VS clip corners and interval-evaluated support of the displaced hull, with the exact stored quaternion and matrix and explicit final-operation error bounds. A negative upper bound for one clip half-space proves noncontribution. Borderline cases remain. No new horizon rejection is attempted.

The private rejection path computes the original tessellation factors and only evaluates the bound when an outer factor exceeds one. It sets zero levels only for proven rejected patches. Retained factors, prepared geometry, canonical H, 50 m refinement range, regional residency, publication, materials, facility support and lighting remain unchanged. Selected indices and TCS patch count are unchanged; this experiment avoids TES work rather than adding a second selection/ownership subsystem.

## Final-bound oracle

Every final TES invocation checked all six outward bounds; every frame checked the proposed rejecting plane, generation and completed fence. Common-half-space clipping is a conservative sufficient classification; cross-plane ambiguous cases are retained.

| Case | Frames | Proposed patch events | False rejects | Containment violations | TES invocations checked |
|---|---:|---:|---:|---:|---:|
| A-orbital | 182 | 0 | 0 | 0 | 8,303,040 |
| B-factor1 | 213 | 0 | 0 | 0 | 194,913,000 |
| C-Florida | 282 | 0 | 0 | 0 | 159,163,920 |
| D-active | 213 | 55,800 | 0 | 0 | 500,265,338 |
| E-grazing | 213 | 27,540 | 0 | 0 | 624,599,367 |
| traversal | 1,701 | 47,201 | 0 | 0 | 2,102,282,027 |
| Florida-motion | 1,020 | 130,435 | 0 | 0 | 1,626,234,378 |
| return | 379 | 390 | 0 | 0 | 102,982,482 |

The final-bound matrix contains 4,203 frames. The full traversal independently visited L0–L17, ten snaps, three reversals and 58 publications. Florida movement used 0.05 m/frame slow movement, 64 m sinusoidal rapid movement, 10.004–80.004 m altitude transitions and a -0.001 rad grazing segment, with regional/facility support active. The return case exercises zero-visible Earth viewing and return under the same Earth owner; it is not a Sun-focus ownership switch. All reported owner/overlap/stale counts were zero. Existing temporary startup presentation is accounted separately from the authoritative NCSM1 owner.

| Fixed pose, final oracle frame | Patches | Proposed / true | False retains | Clipped / inside / mixed / uncertain | TES in proposed patches |
|---|---:|---:|---:|---|---:|
| A-orbital | 15,376 | 0 / 0 | 0 | 0 / 15376 / 0 / 0 | 0 |
| B-factor1 | 360,950 | 0 / 0 | 0 | 0 / 356748 / 4202 / 0 | 0 |
| C-Florida | 294,748 | 0 / 0 | 4 | 4 / 291230 / 3514 / 0 | 0 |
| D-active | 2,060 | 310 / 310 | 1,346 | 1656 / 278 / 123 / 3 | 398,240 |
| E-grazing | 378,919 | 153 / 153 | 644 | 797 / 373630 / 4492 / 0 | 518,107 |

The 96.09% earlier result was the proportion of already-clipped generated triangles belonging to wholly clipped parent patches. It was not a recoverable GPU-time percentage. This certified bound proposes only 310 active-view patches and 398,240 TES invocations, about 14.3% of that view’s total TES work. The more aggressive angular prototype is not promoted into this conclusion.

## Exact same-submission parity

Two independent layers of validation are retained. First, ordinary production TES/fragment modules were used for same-frame depth/HDR/image A/B. Second, a bounded GPU lookup compared the actual post-TES physical records for every retained candidate execution, keyed by patch and exact barycentric bits in the same submitted draw. Both renders shared the identical prepared buffers, compacted indices, camera, lighting, owner and publication. The physical comparison also checked attachment parity.

| Physical + attachment case | Frames | Retained TES records compared | Physical mismatches / missing | Depth / HDR / image unequal bytes |
|---|---:|---:|---:|---:|
| orbital | 182 | 8,303,040 | 0 / 0 | 0 / 0 / 0 |
| factor1 | 213 | 194,913,000 | 0 / 0 | 0 / 0 / 0 |
| Florida | 282 | 159,163,920 | 0 / 0 | 0 / 0 / 0 |
| active | 213 | 428,613,912 | 0 / 0 | 0 / 0 / 0 |
| grazing | 213 | 531,351,170 | 0 / 0 | 0 / 0 / 0 |
| full-traversal | 1,702 | 1,911,785,397 | 0 / 0 | 0 / 0 / 0 |
| Florida-motion | 1,020 | 1,372,178,236 | 0 / 0 | 0 / 0 / 0 |
| return | 379 | 101,670,067 | 0 / 0 | 0 / 0 / 0 |

Each parity matrix contains 4,204 frames; the one-frame difference from the oracle traversal is recorded rather than silently aligned across separate executions. The actual A/B comparisons are within the same submission, so they do not depend on matching publication timing between separate runs. No baseline-record conflict, table exhaustion or sample overflow occurred. Peak baseline records were 3,470,327 against a 4,194,304 capacity. All frames with an emitted hardware TES query matched the record counter exactly (see `physical-count-identity.json`); that diagnostic query is not emitted on every full-traversal frame.

Identical-output controls passed. Injected attachment and physical-record bit changes failed precisely as expected, validating both comparators. These expected negative controls are documented in `comparator-selftest.json` and `physical-selftest.json`. The abandoned direct CPU attachment comparison was stopped after 45 zero-mismatch frames because mapped reads were too slow; the GPU comparator replaced it. It is not an acceptance run.

Separate active captures also produced identical complete physical sets with the oracle enabled, and exact retained sets with rejection enabled. The prepared 45,574,784-byte buffer was bit-identical. Compaction order differed between processes, while the selected triangle set matched. No raw per-frame geometry or image archive is retained.

## Qualified performance — RX 6800 XT, native 3440×1440

Each fixed-pose row uses 100 warmed, frame-aligned samples. Median/P95/P99 use nearest-rank order statistics. Oracle, dual-render, physical-comparison and raw-capture instrumentation are OFF for these timings; the same existing measurement telemetry is enabled on both sides. Private process driver pinning affects teardown, not the production shader path. All ordinary TES and fragment SPIR-V remain unchanged.

| Pose | Terrain median control → reject (ms) | Total median control → reject | Total P95 control → reject | Total P99 control → reject |
|---|---:|---:|---:|---:|
| A-orbital | 0.809 → 0.807 | 0.973 → 0.971 | 1.012 → 0.984 | 1.094 → 1.105 |
| B-factor1 | 6.289 → 6.343 | 9.386 → 9.405 | 9.740 → 9.862 | 9.888 → 10.064 |
| C-Florida | 8.031 → 8.084 | 11.189 → 11.553 | 11.543 → 12.051 | 11.691 → 12.484 |
| D-active | 22.826 → 21.910 | 26.646 → 25.545 | 27.344 → 26.286 | 27.630 → 26.451 |
| E-grazing | 13.360 → 12.098 | 16.633 → 15.560 | 16.977 → 16.555 | 17.330 → 17.197 |

| Pose | Selected / TCS patches (both) | TES median control → reject | Generated primitives control → reject | Clipping output median control → reject |
|---|---:|---:|---:|---:|
| A-orbital | 15,376 | 46,128 → 46,128 | 12,893 → 12,895 | 12,893 → 12,895 |
| B-factor1 | 360,950 | 1,082,850 → 1,082,850 | 142,653 → 142,662 | 142,653 → 142,662 |
| C-Florida | 294,748 | 884,244 → 884,244 | 119,651 → 119,643 | 119,647 → 119,639 |
| D-active | 2,060 | 2,779,253 → 2,381,186 | 953,918 → 952,563 | 946,137 → 946,137 |
| E-grazing | 378,919 | 3,469,998 → 2,951,955 | 245,729 → 243,710 | 235,557 → 235,570 |

TES invocation counts can differ slightly between executions; exact record keys and hardware-counter joins distinguish replay accounting from physical output. Clipping queries alone are not the parity authority. The actual same-submission byte/record comparisons above all returned zero mismatches. AMD reported merged VS/TCS VGPR usage rising from 39 to 135, with no scratch spill; production TES remained 190 VGPR and the ordinary fragment shader 85. These are resource observations, not an independently isolated timing attribution.

| Pose | CPU submit median / P95 control → reject (ms) | Fence wait median / P95 control → reject (ms) |
|---|---:|---:|
| A-orbital | 0.039 / 0.046 → 0.039 / 0.052 | 0.893 / 0.959 → 0.886 / 0.942 |
| B-factor1 | 0.070 / 0.097 → 0.059 / 0.087 | 9.290 / 9.661 → 9.313 / 9.818 |
| C-Florida | 0.053 / 0.077 → 0.072 / 0.110 | 11.162 / 11.551 → 11.564 / 12.018 |
| D-active | 0.081 / 0.116 → 0.082 / 0.119 | 26.540 / 27.250 → 25.408 / 26.230 |
| E-grazing | 0.055 / 0.095 → 0.077 / 0.116 | 16.574 / 16.885 → 15.491 / 16.557 |

CPU record, present, update and full distributions are retained in `results.json` and `frame-traces.json.gz`. Fence wait is aligned to the preceding submitted GPU frame, not treated as independent CPU compute work.

### Transition and repeat evidence

| Uninstrumented full traversal subset | Control total median / P95 / P99 / max (ms) | Reject total median / P95 / P99 / max (ms) |
|---|---:|---:|
| incoming (n=1044/1044) | 9.403 / 13.783 / 15.308 / 17.837 | 9.371 / 14.045 / 15.192 / 16.492 |
| notIncoming (n=655/656) | 16.091 / 19.885 / 21.358 / 22.393 | 15.170 / 18.711 / 19.618 / 21.348 |

Incoming preparation/publication P95 increased from 13.783 to 14.045 ms while P99 fell from 15.308 to 15.192 ms. Non-incoming frames improved. These mixed route-level distributions are kept separate; neither is substituted for fixed-pose steady-state cost.

The initial Florida total P99 increased from 11.691 to 12.484 ms. A reversed repeat measured control/reject P99 of 13.811/13.546 ms and P95 of 13.632/12.798 ms. The repeat changes the sign and shows substantial total-frame variance. It does not establish a candidate-specific regression cause, and it also does not establish the required non-regression gate. Factor-1 P99 increased from 9.888 to 10.064 ms in the paired sweep. The rejection recommendation therefore remains withheld despite the refined-view benefit.

### Distance to the M13 contract

| Pose | Diagnostic total median / P99 (ms) | Median / P99 above 8.33 ms | Median / P99 above 11.11 ms |
|---|---:|---:|---:|
| A-orbital | 0.971 / 1.105 | -7.359 / -7.225 | -10.139 / -10.005 |
| B-factor1 | 9.405 / 10.064 | 1.075 / 1.734 | -1.705 / -1.046 |
| C-Florida | 11.553 / 12.484 | 3.223 / 4.154 | 0.443 / 1.374 |
| D-active | 25.545 / 26.451 | 17.215 / 18.121 | 14.435 / 15.341 |
| E-grazing | 15.560 / 17.197 | 7.230 / 8.867 | 4.450 / 6.087 |

Negative deltas are available headroom. Active-view P99 remains 26.451 ms: 18.121 ms above the preferred terrain-only budget and 15.341 ms above the future fully-featured floor. M13 remains OPEN. The blocking gate for this particular candidate is frame-time non-regression, while the larger budget gap remains a separate campaign responsibility. No quality or refinement-range reduction was made.

## Time-warp control

| Rate | Attributed GPU queries | Total GPU median / P95 / P99 (ms) | CPU submit median / P95 (ms) |
|---|---:|---:|---:|
| 1:1 | 239 | 17.250 / 18.778 / 19.566 | 0.084 / 0.133 |
| 600:1 | 239 | 16.744 / 17.722 / 18.361 | 0.089 / 0.134 |
| 7776000:1 | 239 | 16.294 / 16.930 / 17.303 | 0.072 / 0.124 |

The existing anchored control held 240 frames at each rate; 239 queries per hold were attributed by logged phase boundaries. The traversal passed with one pupil/frame identity/publication and one topology upload, without ownership failures or rate-scaled publication/draw multiplication. One Earth indirect draw was observed at each hold. A 1,069.178 ms CPU update outlier in the first 1:1 hold is retained in `warp-summary.json`; its precise inspection/host cause was not isolated and it is not averaged away. This control does not prove the overall M13 frame-time target.

## KSA architectural reference

Current local KSA production source prepares/modifies/finalizes terrain, then performs GPU frustum compaction before indirect tessellation. Its patch sphere includes maximum authored material displacement. KSA owns those displacement inputs at the prepared-terrain/culling boundary; its independent horizon/body tests are separate responsibilities. Source identities and the verified current assembly are in `ksa-reference.json`; no KSA source was copied.

- **ADOPT:** bound the final displacement at the owner preparing and selecting renderable terrain; keep culling and publication responsibilities explicit.
- **ADAPT:** use NovaCore’s actual prepared VS hull and generation-4 field envelope with outward numerical bounds; evaluate in the TCS before tessellator amplification.
- **INTENTIONALLY DIFFER:** retain canonical H, regional readiness, exact stored transforms and existing horizon semantics. KSA material-texture displacement is not substituted for NovaCore physical authority.

Official history directly relevant to the decision: [June 16, 2025 prepared-height/normal separation](https://discord.com/channels/1260011486735241329/1260112103134724146/1384241537331761153), and [August 20, 2025 near-camera culling corrected for world displacement](https://discord.com/channels/1260011486735241329/1260112103134724146/1407745827572682962). Those records support the responsibility boundary, not a claim that KSA proves NovaCore’s numerical implementation.

## Validation and restored deployment

The full temporary diff was saved as `instrumentation.patch`, then exactly the two initially clean tracked source files were restored from HEAD. Native Release, Triangle Release and Graphics.Tests Release rebuilt successfully. Validation returned: eight GPU tests; six focused headless contracts (P2S5G, Florida support, canonical H, generation-4 integration, Earth route convergence, material-noise value preservation); one visible-window lifecycle test; 15 launcher regressions; both production asset verifications; normal full traversal; bounded Florida smoke; factor-1 control. Every selected test passed, with zero skips. Unselected tests are identified as excluded, not represented as a complete repository suite.

All private validation used process-local layer discovery with Khronos validation. No VUID was suppressed. Expected comparator negative controls and the stopped CPU-comparison prototype are explicitly separate from regression PASS. Detailed results and validation messages are in `final-validation.json`.

Both configurations retain all 49 banked shader hashes, and every module passed SPIR-V validation. Debug remained untouched. Release’s rebuilt native hash differs from its initial binary, while deployed/native-build hashes match and the tracked source diff is empty. It is freshly rebuilt banked source, not a byte-identity claim for the linker output. `final-identity.json` supplies exact hashes.

- Normal Release runtime: `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`.
- Normal launcher: `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.
- Required production assets, shaders, generators, fixtures and caches were preserved.

## Review recommendation and bounded follow-up

**NO SAFE M13.3 TARGET YET.** The final bound and exact-output proof are complete for this matrix. The current TCS implementation has not established the required control/transition percentile non-regression. Project Control should review the measured approximately 1 ms benefit, bound conservatism and TCS resource cost before authorizing further production work. A future bounded follow-up would need to establish stable total-frame non-regression and retain these exact same-submission gates. It must not inherit the angular prototype’s more aggressive rejection counts. No M13.3 title is assigned and no production culling remains enabled.

## Reproduction and evidence lifecycle

Against the recorded HEAD and a clean tracked tree, `instrumentation.patch` reproduces the private native/managed harness. Rebuild Release before running `run.py`; modes `normal`, `oracle`, `reject`, `dual` and `physical` reproduce the successive gates. The selftest/control modes validate comparison machinery. The runtime runner builds private shader copies, uses the reference client extent and strict process-local Vulkan layers, streams logs durably, records shader/runtime identities and disposes private raw files. Do not apply this diagnostic patch to a production deployment. Restore it and run `validate.py` afterward.

`results.json` is the readable measurement index. `frame-traces.json.gz` retains final oracle frame counters, exact comparisons and qualified timing series. `measurements.json.gz` retains original run configuration/identity/summary records. Exploratory angular and metadata-sphere checkpoints are separately labelled historical experiments. `bound-proof.md`, the GLSL/Python diagnostic sources, source patches, runtime identities and compact crash evidence preserve the ability to reproduce.

See `storage.json` and `scratch-retirement-manifest.json` for exact accounting and disposal identities. Automatic approval review rejected removal of this scratch directory and generated bytecode with "blocked by policy" before the command executed. No deletion was retried. The disposable files remain; see `manual-cleanup.md`. The bounded interrupted-run overlay is intentionally retained for the unresolved watchdog cause. The historical evidence package, user dump/video files, assets, caches and Git history were not cleaned.

### Storage accounting

Completed raw captures already disposed: 2,130,343,736 logical bytes.
Scratch still present: 600 files, 407,583,455 logical bytes; 134,217,728 of these are shared production-asset hard links whose source files remain outside scratch.
Total disposable remainder including the generated bytecode: 601 files, 407,592,309 logical bytes, 274,434,256 bytes of uniquely linked file allocation eligible for manual retirement.
Permanent evidence retained: 2,051,700 bytes against the 3,000,000-byte budget, including the 406,641-byte interrupted-run overlay.
Cumulative bytes created are a documented lower bound, not a fabricated lifetime disk-write total; see storage.json. The automatic deletion command was rejected before it executed, so no scratch bytes are reported as removed.

## Final Git state

`git diff --check`: PASS. Staged and tracked diffs are empty. HEAD, origin/main and both milestone tags remain unchanged. No staging, commit, push or tag operation was performed.

```text
?? docs/engineering-evidence/conservative-pre-refinement-visibility/
?? docs/engineering-evidence/near-surface-performance/__pycache__/
?? docs/engineering-evidence/post-m13.2-next-target/
```

Final storage totals and source/ref verification are recorded in `closeout.json`. This package stops at Project Control review.
