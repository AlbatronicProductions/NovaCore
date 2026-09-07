# Post-M13.3: contribution-aware terrain material evaluation

**Lead judgment: ESCALATE TO PROJECT CONTROL.**
**Technical classification: M13.4 CANDIDATE — FOLLOW-UP REQUIRED.**
The shader passes its independent quality/performance review; the stock Release
headless SurfaceAnchor validation gate remains unresolved. See `closeout.md`.

Unbanked production candidate for Project Control. The only production change is
seven inserted/four removed lines in `native/NovaCore.Native/shaders/production_terrain_material.glsl`.
The shader skips three procedural noise evaluations exactly where their final
land-detail contribution is zero. It changes no physical, render or gameplay
contract. **Hardware tessellation, factors, density and future high-fidelity
displacement/silhouette capacity are preserved. M13 remains open.**

## Baseline

HEAD, main, origin/main and peeled M13.3 tag:
`180eaf150ba5db6364e17dd48336690778f058f9`.
The supplied ticket transposed the last characters; Git established the value above.
M13.3 annotated tag `m13.3-prepared-physical-terrain`, object
`bb08cba20947f863391649e68e2e4fe7035520db`.
M13.1: `fade1384c1c7df93d954e7223b1cc8f17db17f98`, `m13.1-ncsm1-tes-hotpath`.
M13.2: `4accf92fd080c16cc8656080aa69def3fa65db53`, `m13.2-ordinary-terrain-shading`.
Development branch: `codex/post-m13.3-next-performance-target`.
Initial main was clean with no staged diff. The initial identity record was written
after creating this evidence directory, so its sole untracked entry is this package.
No existing milestone or main moved; no staging, commit or tag occurred.

Hardware: AMD Radeon RX 6800 XT; native 3440 x 1440; Vulkan SDK 1.4.357.0.
Exact 49-shader, native, managed and asset identities: `baseline-identity.json`.
Final deployment: `deployment.json`. Every performance pair uses equal warm-up,
100 final samples (frames 113–212), fixed J2000 lighting/time and matched camera,
publication, pupil, topology and refinement conditions. Additional profiling and
capture are disabled for final GPU timing. Existing production queries remain.

## WHAT / HOW / BAR / PAYOFF

The active-refinement excess is predominantly fragment material work interacting
with dense primitive rasterization. The measured winner is **procedural material
noise with zero final contribution**, not excessive hardware tessellation by itself.
Exact-zero gating removes that work before evaluation; all nonzero values keep
the same arithmetic, including arbitrarily small contributions.

The fourteen implementation conditions passed before production edits; see
`implementation-boundary.md`. Measured settled active net GPU recovery is
11.16727 ms (MAJOR), with an independent normal-native control of 11.17929 ms.
The standing minimum was 1.5 ms. Output-changing diagnostic savings were not
treated as recoverable quality-preserving gains.

## WORK A: current pipeline decomposition

Fresh active 2 x 2 diagnostic terrain-draw medians, milliseconds:

| Fragment | Accepted factors | Force factor one |
| --- | --- | --- |
| Production | 14.48428 | 5.97604 |
| Cheap diagnostic | 1.53844 | 0.748 |

The full-fragment factor sensitivity is 8.50824 ms; cheap-fragment
factor sensitivity is only 0.79044 ms. Their 7.71780 ms interaction is nonadditive.
The cheap shader consumes every fragment input; VS/TCS machine instructions stay
identical and TES retains all 101 FP64-named instructions, excluding a trivial
dead-input elimination explanation. Fragment/helper utilization is a plausible
mechanism, not a directly measured occupancy attribution.

Timestamp scopes overlap: `candidateDraw` covers VS/TCS/TES/raster/fragment;
`materialsOverlays` includes terrain; `cullCompact` includes demand and preparation.
They must not be summed or mislabeled as independent TES timing. Uploads precede
the total timestamp; paired CPU/preparation accounting below protects that boundary.

## WORK B: fresh baseline refinement and geometry

These initial instrumented medians establish workload; final quieter A/B below
is the performance acceptance comparison. Clip input is measured before clipping;
clip output/fragment counts are reported hardware counters, not estimates of
unique useful geometry. Factor-one and cheap-fragment ablations change outputs.

| Pose | Terrain ms | Total ms | P95 | P99 | Patches | TES invocations | Clip input median | Clip output | Fragments | Inner factor bins |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| orbital | 0.76540 | 0.92648 | 1.04900 | 1.14771 | 15376 | 46128 | 12894 | 12884 | 35913 | (15376,0,0,0,0,0,0) |
| factor1 | 6.27448 | 11.91288 | 14.41584 | 15.31971 | 360946 | 1082838 | 145261 | 145248 | 2595826 | (360946,0,0,0,0,0,0) |
| florida | 7.97924 | 10.69688 | 11.01636 | 11.02896 | 294750 | 884250 | 121847 | 121817 | 3993415 | (294750,0,0,0,0,0,0) |
| active | 14.48428 | 18.66730 | 20.62929 | 21.03114 | 2060 | 2779241 | 953924 | 946145 | 4953599 | (97,120,157,255,344,372,715) |
| grazing | 7.59080 | 10.35852 | 11.13248 | 11.40960 | 378919 | 3469973 | 252803 | 242635 | 2489113 | (377285,138,165,234,270,240,587) |

Inner bins follow the existing telemetry's (1,2,4,8,16,32,64) groups.
Active has 2,060 patches, approximately 2.779 million TES invocations and
4,953,599 fragments. Grazing combines many factor-one patches with a smaller
high-factor region. Force-one active retains 2,060 patches but falls to 6,180 TES
invocations, 404 clipping-output primitives and 4,953,600 fragments. That comparison
does not establish permission to reduce tessellation: interpolated normal/direction
fields and future material displacement remain meaningful responsibilities.
Small invocation-count variation occurs with unchanged parallel submission; its
precise origin is not established. The
oriented triangle set and prepared bytes are the geometry invariants, not raw
compaction buffer order. No false-reject or conservative-culling claim is made.

## WORK C: KSA responsibility

**ADAPT** contribution-aware material evaluation. Current local KSA version
2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400 skips zero-weight materials
and computes gradients before conditional projection sampling. Its TES retains
authored material displacement, including up to four-material blending at LOD 2
and bounded near-camera fade. NovaCore retains the capacity for that equivalent
future geometric responsibility. KSA ocean hiding is not adopted: NovaCore's
currently visible surface still needs its original shading outputs.
Verified source/DLL hashes and independent reasoning are in `reviews.md`.
No KSA source or assets were copied. No broad historical comparison was repeated.

## TES and fragment audits

Selected production TES: wave32, 48 VGPR, 32 SGPR, 4,608 bytes LDS, zero scratch;
101 FP64-named / 150 FP32-named instructions (static ISA mnemonic count).
The retained ISA has 546 machine-instruction lines excluding `s_code_end` padding.
Its live work includes prepared interpolation, normalization, projection and
varying/address transport. This candidate changes none of it: VS/TCS/TES ISA is
byte-identical. Per-invocation TES cost is not separately timed; factor-one
under cheap shading bounds a combined geometry/raster sensitivity, not pure TES.

All 4,953,599 terrain pixels in the active mask capture have zero land-detail
contribution. Three FP64 noise bands were nevertheless evaluated. Guarding them
preserves all material outputs. Fragment VGPR falls 85 to 78, SGPR remains 53,
scratch remains zero. Static FP64 code remains for contributing land; the gain
comes from the compiled dynamic path, including possible scheduling effects.
Do not attribute each saved millisecond exclusively to raw instruction count.
Exact mask capture and AMD ISA/resources are retained as small records.

## Candidate ranking

| Responsibility | Measured sensitivity / net | Proven avoidable | Risk / KSA | Decision |
| --- | --- | --- | --- | --- |
| Zero-contribution noise | 11.16727 ms net active GPU | Same full attachments | Low; ADAPT weighted evaluation | Winner; MAJOR |
| Reduce factors | 8.50824 ms full shader, only .79044 ms cheap shader | Not proven with equivalent varyings/future displacement | Quality contract changes; KSA retains TES | Reject for this candidate |
| Remaining TES normalization | 101 live FP64 mnemonics | No safe dead operation proven | Physical/varying precision | No qualifying implementation |
| Early visibility | No new safe multi-ms proof | Not established | Prior final conservative-bound limitations remain | Do not reopen |
| Preparation/residency | Startup sliced work; no measured warm active excess | Not this settled deficit | Publication/physical ownership | Preserve |

## Implementation and cheap validation

Old execution condition: each noise band's footprint permits evaluation.
New condition: that footprint condition AND `landDetail > 0.0`.
The unchanged land-detail expression moves before the three evaluations.
Footprint derivatives remain before the branch; height-normal derivatives remain
after it. There is no early return, approximate threshold, reduced detail weight,
subgroup requirement, new flag, data upload, bridge or duplicate authority.

Tests KEEP: all existing geometry, physical authority, material, facility,
residency, topology, seam, window and route tests. MIGRATE/SPLIT/RETIRE: none.
No expectation was weakened. No production implementation revision occurred.

Cheap active timing with added collection off: 17.11309 to 5.96024 ms total GPU.
Exact attachment proofs on active, grazing, inland, mixed coastal/helper lanes
and confirmed Florida passed before full validation. No added CPU or memory work.
The go gate therefore passed; only this one responsibility was implemented.

## Full performance: final 100-sample paired runs

All values are milliseconds. Gain is total GPU; percentages are total GPU gain.
Negative budget distance means headroom. Tiny orbital/Florida changes are noise.

| Pose | Terrain A | Terrain B | Terrain gain | Terrain % | GPU A | GPU B | GPU P95 A/B | GPU P99 A/B | GPU gain | GPU % | B minus 8.33 | B minus 11.11 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| orbital | 0.75704 | 0.75784 | -0.00080 | -0.11% | 0.91356 | 0.91200 | 0.91952/1.00501 | 1.03332/1.12946 | 0.00156 | 0.17% | -7.41800 | -10.19800 |
| factor1 | 5.99920 | 2.38832 | 3.61088 | 60.19% | 8.74516 | 5.13420 | 9.05476/5.47208 | 9.08384/5.48396 | 3.61096 | 41.29% | -3.19580 | -5.97580 |
| florida | 7.79888 | 7.78664 | 0.01224 | 0.16% | 10.51148 | 10.49888 | 10.78356/10.74080 | 10.81040/10.80100 | 0.01260 | 0.12% | +2.16888 | -0.61112 |
| active | 14.49268 | 3.40748 | 11.08520 | 76.49% | 17.12243 | 5.95516 | 17.26257/5.97308 | 17.31656/5.97736 | 11.16727 | 65.22% | -2.37484 | -5.15484 |
| grazing | 7.49756 | 2.71036 | 4.78720 | 63.85% | 10.25564 | 5.46592 | 10.69220/5.97704 | 10.76360/5.99236 | 4.78972 | 46.70% | -2.86408 | -5.64408 |

Normal deployed native-DLL control: active 17.13441 to 5.95512 ms,
11.17929 ms saved, with the same fixed-pose managed driver. This rejects temporary
native instrumentation as the source of the gain. Fully contributing inland land
remains approximately 17.42 ms total GPU and Florida remains above 8.33 ms.
This is not a universal ocean/land speedup or a completed M13 performance campaign.
The 11.11 ms future full-featured floor cannot be certified by terrain-only GPU
timing. Orbital P95/P99 rose by about .085/.096 ms at a sub-1 ms median; controls
show no material regression, but these small tail differences are not hidden.

## CPU, preparation, synchronization and memory

Paired diagnostic collection is separate from final GPU timing. CPU update
contains fence wait; columns are overlapping responsibilities, not additive costs.
GPU frame N is waited/inspected during update N+1; record/submit belongs to N.

| Pose | Host median A/B | Update A/B | Fence A/B | Record A/B | Submit A/B | Present A/B | Acquire A/B |
| --- | --- | --- | --- | --- | --- | --- | --- |
| orbital | 3.4495/3.3800 | 3.0368/2.9893 | 0.8479/0.8474 | 0.1409/0.1393 | 0.0373/0.0373 | 0.1693/0.1698 | 0.0392/0.0388 |
| factor1 | 11.3908/7.8362 | 10.9947/7.4465 | 8.8354/5.3678 | 0.1455/0.1410 | 0.0374/0.0367 | 0.1688/0.1673 | 0.0407/0.0401 |
| florida | 13.2929/13.2366 | 12.8418/12.8363 | 10.6461/10.6912 | 0.1571/0.1522 | 0.0397/0.0382 | 0.1788/0.1702 | 0.0420/0.0412 |
| active | 19.7566/8.2947 | 19.2994/7.8970 | 17.1001/5.8715 | 0.1741/0.1406 | 0.0415/0.0364 | 0.1817/0.1679 | 0.0433/0.0400 |
| grazing | 12.9202/8.0415 | 12.5085/7.6389 | 10.3648/5.4597 | 0.1471/0.1402 | 0.0380/0.0364 | 0.1710/0.1655 | 0.0420/0.0401 |

| Pose | Mode | Demand slices / sum ms | Incoming slices / sum ms | Current slices | Logged warm slices |
| --- | --- | --- | --- | --- | --- |
| orbital | baseline | 2 / 0.84096 | 1 / 0.96404 | 0 | 0 |
| orbital | candidate | 2 / 0.83636 | 1 / 0.96460 | 0 | 0 |
| factor1 | baseline | 44 / 65.07672 | 11 / 41.06988 | 0 | 0 |
| factor1 | candidate | 44 / 65.37696 | 11 / 40.97312 | 0 | 0 |
| florida | baseline | 22 / 31.66644 | 11 / 40.92572 | 0 | 0 |
| florida | candidate | 22 / 31.67048 | 11 / 40.91908 | 0 | 0 |
| active | baseline | 44 / 65.07748 | 11 / 41.17576 | 0 | 0 |
| active | candidate | 44 / 65.07176 | 11 / 40.99852 | 0 | 0 |
| grazing | baseline | 44 / 65.23744 | 11 / 41.08148 | 0 | 0 |
| grazing | candidate | 44 / 65.42864 | 11 / 40.94056 | 0 | 0 |

The active host median falls 19.7566 to 8.2947 ms, principally fence
wait 17.1001 to 5.8715 ms. No cost migrates into CPU, preparation or memory.
Demand is logged above .01 ms; absence alone does not prove zero sub-threshold
work. Recorded cumulative dispatch/publication counters remain unchanged. Current
preparation is already ready; incoming preparation is sliced before the warm
window. Divide incoming total by 11 for about 3.73 ms/slice in near regimes;
this startup sum must not be added to each settled frame.

Florida A/B preserves 670 requests/loads, 163,158,696 read bytes, 93,392,640 upload
bytes and 119,737,728 allocated bytes. Active has zero regional loads/uploads and
four allocated bytes for its empty demand representation. The regional catalog
contains 670 possible records; that is not active residency. Budget 124,895,232
bytes, bounded ready/upload queues and publication identities remain unchanged.

## Physical / render / gameplay outcome

M13.3 remains: render interpolates prepared full relief; gameplay queries
continuous H. No obsolete per-TES physical authority returns. Full attachment
comparisons validate D32, HDR and final presentation on active, grazing, land,
synthetic mixed masks and repeated Florida. Prepared vertices and the oriented
triangle multiset match exactly. No tolerance or quality expectation changes.

The first active comparison incorrectly required raw compaction ordering; its
false result remains recorded alongside the corrected geometry invariant.
The first Florida A/B differed at one D32/HDR pixel, but not final image. Two
subsequent banked A/A captures and the repeated A/B are fully identical to that
candidate capture, proving baseline inter-run variation. The precise old raster
cause remains UNCLASSIFIED; compaction is not assumed to explain that pixel.
Invalid/NaN domain equivalence is not claimed; valid captured values are finite.

## Dynamic validation and frame pacing

### normal-full-transitions

Exit 0.

```text
P2S5C3 Desktop traversal PASS: frames=1701; elapsedMs=13495.4072; levels=L0,L1,L2,L3,L4,L5,L6,L7,L8,L9,L10,L11,L12,L13,L14,L15,L16,L17; missing=; snaps=10; reversals=3; publications=58; pupilErrorMax=3.885633147E-006rad; heightParityMax=0.000000000E+000m; normalParityMax=0.000000000E+000rad; zeroOwner=0; overlap=0; stale=0; residentMax=191558056; peakResident=191558056; callback=0.004068/0.001400/0.006700/0.022400/1.661400ms(1702); selector=0.001370/0.000900/0.002400/0.004600/0.097000ms(1702); pupilSnap=0.000886/0.000100/0.001000/0.001900/0.607700ms(1702); scheduling=0.026274/0.011500/0.050700/0.827800/0.827800ms(58); physical=0.008967/0.004500/0.010700/0.219600/0.219600ms(58); gpuPrepare=0.000000/0.000000/0.000000/0.000000/0.000000ms(0); publication=0.003103/0.001500/0.006400/0.087700/0.087700ms(58)
[native] P2S5C3 GPU timing: total avg=4.786 ms; p50=4.826; p95=7.076; p99=8.710; max=9.256; samples=1699
[native] P2S5C3 frame timing: all avg=7.656 ms; p50=7.433; p95=9.829; p99=12.098; max=194.199; samples=1701
[native] P2S5C3 frame timing: scaleTransition avg=8.113 ms; p50=8.145; p95=10.121; p99=13.269; max=194.199; samples=1299
[native] P2S5C3 frame timing: snap avg=7.351 ms; p50=7.437; p95=7.652; p99=7.652; max=7.652; samples=10
[native] P2S5C3 frame timing: publication avg=6.663 ms; p50=6.629; p95=8.231; p99=17.727; max=17.727; samples=58
```

### normal-warp

Exit 0.

```text
P2S5E anchored warp diagnostic PASS: frames=779; altitude=10.004m; pupil=1; frameIdentity=1; publications=1; topologyUploads=1; zeroOwner=0; overlap=0; stale=0; callback=0.004452/0.001400/0.006300/0.015400/1.594700ms(780); selector=0.001659/0.001000/0.003900/0.008300/0.095800ms(780); pupilSnap=0.000644/0.000100/0.000900/0.001600/0.331600ms(780); scheduling=0.769600/0.769600/0.769600/0.769600/0.769600ms(1); physical=0.229800/0.229800/0.229800/0.229800/0.229800ms(1); gpuPrepare=0.000000/0.000000/0.000000/0.000000/0.000000ms(0); publication=0.076400/0.076400/0.076400/0.076400/0.076400ms(1)
[native] P2S5C3 GPU timing: total avg=3.444 ms; p50=3.341; p95=3.612; p99=4.879; max=7.155; samples=777
[native] P2S5C3 frame timing: all avg=6.543 ms; p50=6.005; p95=7.623; p99=9.273; max=210.892; samples=779
[native] P2S5C3 frame timing: publication avg=7.623 ms; p50=7.623; p95=7.623; p99=7.623; max=7.623; samples=1
```

The additional normal-native 90-yaw by 9-pitch sweep at 10.004 m
crosses active refinement, grazing and sky views. It retains 809 aligned queried
frames per side (810 pose steps; initial query latency excluded). No bulk capture.
This is a mixed dynamic distribution, not another settled-pose benchmark.

| Metric | Baseline median/P95/P99 | Candidate median/P95/P99 |
| --- | --- | --- |
| gpuDetailedDrawMs | 7.32152 / 14.38852 / 14.52372 | 2.85304 / 3.57516 / 4.35436 |
| gpuTotalMs | 10.10768 / 17.15025 / 18.34591 | 5.64156 / 6.88716 / 7.47668 |

The full normal traversal covers L0–L17, surface/orbit return, ten
pupil snaps and three reversals. The Graphics live Florida regression additionally
covers current/incoming regional preparation, L16/L17, focus-away/return, support
and drain. Warp holds 1x, 600x and 7,776,000x with no additional publication or
topology upload. Its particular camera reports zero visible terrain primitives;
it proves cadence/ownership, not high-refinement warp shading throughput.

The oldest approximately 181 ms M13.3 host event remains UNCLASSIFIED.
Measured initial five-pose frame-one spikes around 183–204 ms are dominated by
swapchain recreation; active frame 1 is 203.9621 ms, of which 187.0621 is recreate.
AMD ISA extraction stalls in diagnostic record work are excluded from final timing.
Other measured cold startup fence stalls remain unclassified: active candidate
frame 9 62.8733/59.8414 ms total/fence; grazing frame 9 76.8988/73.7231;
factor-one frame 8 48.9566/45.6168. They precede publication/query identity.
Normal dynamic maxima 194.199 and 210.892 ms lack full per-frame correlation;
do not retrospectively assign them to recreation or the candidate. No hitch-free
claim is made. Aggregate transition distributions and publication spikes above
are reported separately from settled savings.

## Builds and regression results

Native Debug/Release builds passed; managed Debug/Release solution builds passed
with zero warnings/errors. Deployment checks compare each configuration's native
DLL and all 49 shaders to its own build, not another configuration. Normal Release
fragment SHA-256 is `8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`,
equal to the measured candidate. Source hash:
`521435c525b1538f730fda69fb89b62ec10846c7cd870f966a2ee9076a93b598`.

| Run | Exit | Summary |
| --- | --- | --- |
| Debug-headless | 0 | Graphics Debug: selected=79; pass=79; fail=0; skip=0; excluded=12 |
| Debug-gpu | 0 | Graphics Debug: selected=8; pass=8; fail=0; skip=0; excluded=83 |
| Debug-window | 0 | Graphics Debug: selected=4; pass=4; fail=0; skip=0; excluded=87 |
| Debug-native-gpu | 0 | Native GPU Debug: pass=2; fail=0; skip=0 |
| Debug-NovaCoreRegionalPhysicalTests | 1 |  |
| Debug-regional-cpu | 0 |  |
| Release-headless | 1 | Graphics Release: selected=79; pass=78; fail=1; skip=0; excluded=12 |
| Release-SurfaceAnchor-focused | 0 | Graphics Release: selected=1; pass=1; fail=0; skip=0; excluded=90 |
| Release-gpu | 0 | Graphics Release: selected=8; pass=8; fail=0; skip=0; excluded=83 |
| Release-window | 0 | Graphics Release: selected=4; pass=4; fail=0; skip=0; excluded=87 |
| Release-native-gpu | 0 | Native GPU Release: pass=2; fail=0; skip=0 |
| Release-regional-cpu | 0 |  |
| launcher-Release | 0 |  |
| asset-earth-surface-v5 | 0 | Status: Valid |
| asset-earth-florida-m12 | 0 | Status: Valid |
| route-probe-build | 0 |  |
| six-routes-and-Florida-smoke | 0 | Launcher route probe PASS: six supported presets, current Release executable; exact Florida arguments; Florida bounded smoke PASS: 500 frames, ready publication, stable seating, no new Vulkan/ownership/device regression; no bulk capture |
| normal-full-transitions | 0 |  |
| normal-warp | 0 |  |
| Release-headless-confirmation | 1 | Graphics Release: selected=79; pass=78; fail=1; skip=0; excluded=12 |
| Release-headless-private-predicates | 0 | Graphics Release: selected=79; pass=79; fail=0; skip=0; excluded=12 |

Strict canonical Vulkan validation remains enabled; no VUID filter or
production layer behavior changes. The initial native CPU invocation omitted its
required pack argument; corrected invocation passes. This was a runner error, not
a residency failure. A first motion-summary attempt failed on mixed unavailable
metric values; parsing now records unavailable samples explicitly. Neither changed
production or test expectations. All raw failed-run logs are hashed and important
failure details are retained in compact records.

The Release headless SurfaceAnchor compound assertion is an unresolved gate.
Original and confirmation full runs each report 78/79, while unchanged
focused canonical retry, twenty private predicate probes and the private full
suite pass. Added logging changes the executed test binary and does not resolve
the stock failure. The original
exception hides which condition failed; do not call it JIT/allocation without
predicate evidence. This test does not execute the changed fragment shader.
`anchor-predicate-probe.json`, `headless-failure.txt` and validation records
preserve the distinction. No unrelated production/test correction was attempted.

## VERIFY A / VERIFY B / revision

Independent quality verifier: PASS on the actual material change and exact
attachment/derivative/geometry contract. Its strongest attack was coastal helper
derivatives and downstream roughness; the code preserves both. Independent
performance verifier: PASS on the settled claim, including all 100 samples,
49-shader identity, normal-native control and paired CPU/residency accounting.
No production defect was found to revise. Their full boundaries and limitations
are in `reviews.md`. The later headless gate is not silently covered by those
earlier candidate-specific PASS judgments.

## Retirement / storage / review boundary

Old unnecessary execution retires in the sole material helper; contributing-land
code remains required current production. No dual path, compatibility bridge,
fixture or TES responsibility retires. Project Control acceptance is the banking
condition; no main or milestone promotion was performed.

Reproduction: `reproduction.md`. All full numerical rows are retained losslessly
in columns; ISA, runtime/input hashes, failures and reproduction source are bounded.
No raw attachment, geometry buffer, compiled private host or bulk archive is a
permanent fixture. Permanent budget is 10 MiB; scratch raw set budget is 512 MiB.
Cleanup scope, actual bytes and final status are recorded in `closeout.json`.
Test-internal transient byte churn was not metered and is not invented.

Final lead judgment, technical classification and complete Git status are in
`closeout.md`. No M13.4 title or tag is assigned before all acceptance gates pass.
