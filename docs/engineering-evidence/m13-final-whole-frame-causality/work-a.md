# Work A: whole-frame GPU cost map

**One worthwhile placement responsibility merits the next cheap correctness gate; no production qualification is asserted here.** Historical and fresh measurements distinguish current terrain storage/read cost from incoming preparation. Fresh bulk-local Florida repeatedly recovers about 2 ms against bracketing current baselines. Positive material noise has a larger output-changing sensitivity but no proven removable portion. Global tessellation reduction is unsupported by the workload comparison.

The continuation rule for this ticket is measured safe avoidable payoff of at least 1.5 ms for exactly one responsibility. The earlier demand that every regional transient clear 8.33 ms is not reused as the decision rule. Closure remains a valid outcome if the one placement responsibility fails correctness, net benefit or bounded production integration.

## Retained production envelope

Quiet banked-native M13.4 last-100-frame warm windows; CPU is a separate profile run. Median uses nearest rank, including the lower middle observed sample for n=100. All values ms.

| Regime | GPU median / P95 / P99 / peak | Draw median | Current cull/prep median | TCS patches | TES invocations | Fragment median | Maximum factor |
|---|---|---:|---:|---:|---:|---:|---:|
| orbital | 1.18824 / 1.33660 / 1.37276 / 1.38412 | 0.78128 | 0.10476 | 15,376 | 46,128 | 35,913 | 1.0 |
| factor1 | 5.16652 / 5.81876 / 5.82400 / 5.84912 | 2.41980 | 2.68696 | 360,946 | 1,082,838 | 2,595,734 | 1.0 |
| florida | 10.58128 / 11.00164 / 11.03756 / 11.17816 | 7.86904 | 2.65352 | 294,750 | 884,250 | 3,993,782 | 1.0 |
| grazing | 5.59988 / 6.48728 / 6.50068 / 6.51564 | 2.79384 | 2.75424 | 378,919 | 3,470,003 | 2,488,853 | 64.0 |
| active-refinement | 5.96372 / 5.97724 / 5.98632 / 5.98736 | 3.41584 | 2.49272 | 2,060 | 2,779,255 | 4,953,599 | 64.0 |
| active | 4.71456 / 4.86276 / 4.87808 / 4.88892 | 1.86188 | 2.50252 | 8,953 | 26,859 | 4,953,600 | 1.0 |
| inland | 9.73524 / 9.81800 / 9.85840 / 9.89460 | 6.78764 | 2.52872 | 9,252 | 27,756 | 4,953,600 | 1.0 |

Every retained warm window has generation 1 with no incoming/current preparation or demand dispatch. The roughly 2.5–2.75 ms near-surface broad compute interval is therefore current visibility work, not evidence of continuous physical preparation. Separate cull/compact costs were unavailable in those fixed captures and are supplied by the fresh measurements below.

The exact 50 m clearance file named active is a factor-1 boundary control. True active refinement is 10.004 m clearance at pitch −1 rad, where factor 64 is measured. Inland is 40° N, 105° W at canonical H plus 50 m; it is not an inside-ground pose. Florida uses the authored launcher eye and fixed diagnostic orientation. Exact body camera, quaternion/view projection, generation, topology family and environment are retained in work-a.json.

Inland and active each issue about 4.954 million fragment invocations, yet inland draw is 6.788 ms against active 3.416 ms. Inland has only 27,756 TES invocations against 2.779 million active. Florida also costs more despite lower TES workload. This rejects TES invocation count alone as the cross-regime explanation. It suggests a material-dependent sensitivity; it does not prove positive material work removable.

## Fresh split GPU scopes

All listed runs use their complete last 100 aligned warm frames. Total is the actual total GPU query. Candidate/draw spans VS/TCS/TES/raster/fragment and is never labeled fragment-only.

| Run | Total median / P95 / P99 / peak | Draw median | Cull median | Compact median | Whole current block median | Tone-map mean |
|---|---|---:|---:|---:|---:|---:|
| base-active | 5.95776 / 5.96820 / 5.97540 / 5.98768 | 3.41180 | 2.27316 | 0.20192 | 2.48936 | 0.04421 |
| base-florida-repeat-isa | 11.19293 / 11.60281 / 11.78606 / 11.79734 | 8.04040 | 2.28120 | 0.33384 | 2.65916 | 0.18996 |
| base-florida-return | 11.38076 / 11.84535 / 12.24737 / 12.52717 | 8.10388 | 2.39380 | 0.33292 | 2.77164 | 0.15082 |
| base-florida | 11.06856 / 11.47504 / 11.50571 / 11.54186 | 8.04804 | 2.28428 | 0.33324 | 2.66228 | 0.09249 |
| base-grazing | 5.49704 / 6.42900 / 6.44044 / 6.44192 | 2.74260 | 2.27340 | 0.37856 | 2.69448 | 0.04427 |
| base-inland-return | 9.72476 / 10.03664 / 12.07361 / 12.22914 | 6.78628 | 2.38824 | 0.20528 | 2.61116 | 0.10283 |
| base-inland | 9.38668 / 9.40228 / 9.40468 / 9.41028 | 6.78580 | 2.32344 | 0.20524 | 2.54012 | 0.04711 |
| local-active | 4.83224 / 4.93328 / 4.98400 / 4.99064 | 3.40804 | 1.11152 | 0.01144 | 1.12984 | 0.14385 |
| local-florida-repeat | 9.13840 / 9.27524 / 9.33576 / 9.39216 | 7.70132 | 1.09772 | 0.01672 | 1.12200 | 0.15876 |
| local-florida | 8.88184 / 8.94156 / 8.97756 / 8.98348 | 7.70720 | 1.09508 | 0.01640 | 1.11860 | 0.04786 |
| local-grazing | 4.03480 / 4.20840 / 4.36688 / 4.37976 | 2.60664 | 1.09964 | 0.02184 | 1.13456 | 0.13538 |
| local-inland | 8.21552 / 8.42536 / 8.64128 / 8.72504 | 6.76348 | 1.11092 | 0.01144 | 1.12932 | 0.18893 |
| noise-florida-isa | 6.83248 / 7.31892 / 7.33580 / 7.34992 | 4.11872 | 2.27528 | 0.33292 | 2.65216 | 0.04867 |
| noise-inland | 6.04340 / 6.36432 / 6.57356 / 6.58648 | 3.21848 | 2.27920 | 0.20520 | 2.49896 | 0.10943 |
| physical-local-florida | 9.63416 / 9.92628 / 10.26094 / 10.35258 | 7.76136 | 1.16424 | 0.33284 | 1.54544 | 0.15950 |
| physical-local-inland | 8.42024 / 8.66884 / 8.79720 / 8.80172 | 6.77512 | 1.17736 | 0.20524 | 1.40000 | 0.14095 |

Fresh Florida/inland placement controls match baseline camera, generation, topology, TCS/TES and shader identities. Active/grazing match camera, TCS, factor bins and shader hashes, but their raw TES counter distributions vary slightly (active medians 2,779,258/2,779,251; grazing 3,469,998/3,470,001). These counter observations are not claimed bit-identical or silently accepted as geometry parity.

Unplaced current cull remains approximately 2.27–2.32 ms across all four initial fixed regimes despite selected patches ranging from 2,060 to 378,919. Compaction varies from about 0.20 to 0.38 ms. This identifies a substantial whole-topology/read-side floor plus smaller survivor-output cost; it does not prove the view-dependent cull can be omitted.

Current block is the sum of adjacent reset/cull/compact and their explicit barrier intervals; broad cull/preparation additionally contains framing. Prep is nested inside broad compute. Candidate, background, detailed, scene and materials/overlays use overlapping spans and must not be summed as independent costs. The JSON stores arithmetic residuals only as consistency checks, not new independently timed stages.

The fresh Florida total-minus-candidate-minus-compute remainder is larger and more variable than in earlier quiet runs. Tone-map means vary from about 0.048 to 0.190 ms across fresh controls. Do not credit that entire remainder or an ISA-enabled run difference to terrain optimization. Use bracketing repeated total GPU measurements and retain all tails.

## Exact scope of the placement controls

| Allocation role | Fixed allocation bytes | Historical C / fresh physical-only | Fresh bulk-local |
|---|---:|---|---|
| Prepared physical work storage | 45,574,784 | Moved | Moved |
| Regional current-pupil staging physical | Not allocated in these settled fixed samples | Moved if present | Moved if present |
| Immutable topology lattice | 11,393,696 | Unchanged | Moved |
| Immutable topology indices | 17,090,496 | Unchanged | Moved |
| Visibility work output | 5,696,832 | Unchanged | Moved |
| Compacted index output | 17,090,496 | Unchanged | Moved |

The five logged fixed allocations total 96,846,304 bytes; only 45,574,784 bytes belong to physical-only placement. The private heap ledger shows type 1/properties 6/heap 0 becoming type 2/properties 7/heap 1 for the chosen roles. HOST_VISIBLE and HOST_COHERENT remain required. The buffer count, sizes and ownership schedule are unchanged by these controls.

Physical-only is materially narrower. Fresh physical-only Florida cull/compact are 1.16424/0.33284 ms, versus bulk-local 1.09508/0.01640 ms. Bulk placement therefore includes an extra measured compaction-path gain; it cannot be attributed solely to prepared physical storage. A production design must name the actual GPU-dominated resident roles and preserve CPU initialization/immutable upload/readback consumers and portable allocation failure behavior.

The initial physical-only fresh gain is about 1.43 ms in Florida and 0.97 ms inland by median; using the noisier Florida return gives a different number. It does not independently establish a robust current >=1.5 ms winner. Bulk-local Florida instead repeats 8.88184/9.13840 ms against baselines 11.06856/11.19293/11.38076 ms. That is the stronger single-responsibility opportunity, subject to output and allocator qualification.

## Retained placement-only signal, before this ticket

Retained A/C have identical shader hashes. C changed only prepared physical placement, not reuse or vertex-validation logic. These chosen complete settled groups have no active incoming/current preparation or demand; full logged input bytes and TCS/TES match.

| Settled group | Frames | A mean / P95 | C mean / P95 | Total mean recovery | Broad current-compute mean recovery | Draw mean recovery |
|---|---:|---|---|---:|---:|---:|
| Florida_start | 119 | 10.900821 / 11.13760 | 9.173050 / 9.29904 | 1.727771 | 1.500751 | 0.227334 |
| first_return_settled | 30 | 10.931975 / 11.20424 | 9.158312 / 9.23916 | 1.773663 | 1.519759 | 0.254071 |
| second_return_settled | 250 | 10.927227 / 11.14344 | 9.162784 / 9.26672 | 1.764443 | 1.510136 | 0.253713 |

These historical 1.73–1.77 ms signals justified a fresh whole-frame test under the new payoff rule. They are not substituted for less favorable fresh physical-only measurements. Global A/C publication differences around logical 248–270 are retained in JSON, and clipping/fragment counts differ even in these fixed-input groups. They do not provide raster parity or a controlled GPU-clock experiment.

## Material sensitivity and tessellation reality

Noise-live replaces the procedural material noise field with a deliberately different cheap field while retaining the function inputs. Florida draw drops from about 8.04 to 4.12 ms; inland from 6.786 to 3.218 ms. This is a large material sensitivity, not a quality-preserving recovery claim. Positive contribution is visible, so preserving inputs and upstream compiler stages still cannot authorize deleting or approximating the field.

Root owns the compiler comparison and any exact-output proof. Work A does not infer a stage cost from static ISA or assume all varying transport survives because the diagnostic function uses its parameters. Noise specialization already banked by M13.4 is not a new target.

True factor-64 active and grazing are substantially cheaper than positive-material Florida/inland. No global factor reduction or retirement of future displacement/silhouette capacity is justified. The existing hardware count/factor bins are adequate for this causality decision unless a new distinct geometric cause emerges.

## Dynamic and neighboring workload context

Retained dynamic streams below preserve complete aligned Earth observations and their separate full-stream/startup records. Exact correlation requires positive gpuFrame == geometryFrame == host.frame−1; stale focus-away IDs are not new terrain observations.

| Route | Aligned Earth frames | GPU median / P95 / P99 / peak | Post30 without prep GPU median / P95 / P99 |
|---|---:|---|---|
| dynamic-full | 1698 | 6.49040 / 8.92700 / 10.20660 / 11.68900 | 5.55692 / 6.48384 / 6.55276 |
| dynamic-active-grid | 809 | 5.54488 / 7.08580 / 7.46756 / 8.49152 | 5.48536 / 6.50888 / 7.13232 |
| dynamic-grid | 809 | 4.41872 / 5.87684 / 6.10844 / 6.20600 | 4.41632 / 5.75856 / 6.12492 |
| dynamic-inland-grid | 809 | 8.64168 / 10.65476 / 11.15256 / 11.72788 | 8.52020 / 10.63680 / 11.15676 |
| dynamic-regional | 737 | 10.84140 / 13.73060 / 14.67372 / 16.65820 | 10.84248 / 11.15748 / 11.24124 |
| dynamic-florida | 397 | 10.86012 / 13.64516 / 14.69960 / 16.63816 | 10.84480 / 11.18268 / 11.48344 |
| dynamic-warp | 745 | 5.49208 / 6.41656 / 7.33200 / 7.95132 | 5.48940 / 6.40384 / 6.43276 |

Descriptive post30/no-preparation correlations with total GPU: inland sweep candidate 0.993, fragment invocations 0.924, TES 0.431; active sweep candidate 0.978, fragments 0.866, TES 0.878. These are not causal regression coefficients: view, primitive density, material coverage and overlapping timestamps all change together. They explain why a single global invocation-count ranking would mislead.

The JSON retains three expensive frames and lower-cost neighbors within 12 native frames of each dynamic route, requiring the same current generation. Those neighbors may have different camera/view/refinement workloads. They are labeled workload comparisons, never exact-control savings.

For regional event boundaries, fresh banked-semantics lifecycle samples below preserve the same current owner immediately before/through final incoming work and the first new owner afterward. Incoming physical work is displayed separately from final readiness. Every source frame identity and current/incoming state is in JSON.

| Logical | Current / incoming generation | Total | Candidate | Current cull | Current compact | Incoming physical | Incoming final block |
|---:|---|---:|---:|---:|---:|---:|---:|
| 333 | 16 / 0 | 9.15992 | 6.57088 | 1.92752 | 0.29200 | 0.00000 | 0.00000 |
| 334 | 16 / 17 | 12.71912 | 6.59972 | 1.92384 | 0.29104 | 3.48128 | 0.00000 |
| 342 | 16 / 17 | 12.95532 | 6.62824 | 1.91964 | 0.29072 | 3.73888 | 0.00000 |
| 343 | 16 / 17 | 14.76476 | 6.55128 | 1.91460 | 0.29136 | 3.26500 | 2.43576 |
| 344 | 17 / 0 | 10.77840 | 7.62248 | 2.07436 | 0.31852 | 0.00000 | 0.00000 |
| 345 | 17 / 18 | 13.63812 | 7.13264 | 2.09800 | 0.31836 | 3.88656 | 0.00000 |
| 354 | 17 / 18 | 14.19940 | 7.40324 | 2.07680 | 0.31960 | 4.20360 | 0.00000 |
| 355 | 17 / 18 | 15.88330 | 7.65444 | 2.08608 | 0.31916 | 2.70172 | 2.63184 |
| 356 | 18 / 0 | 11.65105 | 8.29760 | 2.23328 | 0.35116 | 0.00000 | 0.00000 |
| 357 | 18 / 19 | 14.72217 | 7.86532 | 2.23700 | 0.34672 | 3.48132 | 0.00000 |
| 366 | 18 / 19 | 15.32232 | 7.84340 | 2.25444 | 0.34656 | 4.25664 | 0.00000 |
| 367 | 18 / 19 | 16.95828 | 8.07656 | 2.23956 | 0.34772 | 3.32608 | 2.67384 |
| 368 | 19 / 0 | 11.21352 | 8.05084 | 2.30032 | 0.35288 | 0.00000 | 0.00000 |
| 713 | 34 / 0 | 9.53056 | 6.74884 | 1.95596 | 0.29272 | 0.00000 | 0.00000 |
| 714 | 34 / 35 | 12.73408 | 6.62916 | 1.92768 | 0.29244 | 3.47584 | 0.00000 |
| 722 | 34 / 35 | 13.04576 | 6.49648 | 1.92172 | 0.29168 | 3.73720 | 0.00000 |
| 723 | 34 / 35 | 15.15808 | 6.49160 | 1.91916 | 0.29156 | 3.08744 | 2.43872 |
| 724 | 35 / 0 | 10.60167 | 7.29116 | 2.06784 | 0.32044 | 0.00000 | 0.00000 |
| 725 | 35 / 36 | 14.34084 | 7.51696 | 2.07792 | 0.31932 | 3.74416 | 0.00000 |
| 734 | 35 / 36 | 14.18472 | 7.34688 | 2.08860 | 0.31852 | 4.11872 | 0.00000 |
| 735 | 35 / 36 | 15.62185 | 7.39152 | 2.10172 | 0.31900 | 2.82004 | 2.61928 |
| 736 | 36 / 0 | 10.92856 | 7.87332 | 2.22944 | 0.34548 | 0.00000 | 0.00000 |
| 737 | 36 / 37 | 14.82083 | 7.88852 | 2.27224 | 0.34696 | 3.87452 | 0.00000 |
| 746 | 36 / 37 | 14.89788 | 7.85476 | 2.25976 | 0.34732 | 4.23500 | 0.00000 |
| 747 | 36 / 37 | 16.80360 | 7.87084 | 2.24348 | 0.34728 | 3.32756 | 2.67836 |
| 748 | 37 / 0 | 11.51916 | 8.33908 | 2.29672 | 0.35332 | 0.00000 | 0.00000 |

The prior rejected reference/work scheduling control remains only supporting evidence: incoming omitted 8.25487 ms mean versus enabled 8.67662, with 26/39 of64 above 8.33. Reference-first order, map stalls and all64 clipping/fragment differences prevent treating 0.422 ms as an exact production cost. It supplies no reason to rerun mapped-copy or final-validator combinations in this whole-frame ticket.

## CPU/GPU limiter boundary

The retained CPU frame is mostly waiting for GPU plus about 2.5–2.8 ms of other measured host work, not an independent 8–13 ms CPU computation. The no-fence subtraction is descriptive and is not promised recoverable time; serialization and shared upload work still affect wall-clock pacing.

| Regime | CPU submission median / P95 / P99 | Fence median | Host frame minus fence median / P95 | Validation/upload median |
|---|---|---:|---|---:|
| florida | 13.38170 / 14.10410 / 14.27990 | 10.68180 | 2.63880 / 3.41830 | 1.95460 |
| inland | 11.77620 / 12.00120 / 12.11770 | 9.26200 | 2.51310 / 2.74700 | 1.94100 |
| active-refinement | 8.38350 / 8.55440 / 8.76390 | 5.89210 | 2.48960 / 2.66550 | 1.92980 |
| grazing | 8.04400 / 8.87250 / 9.08910 | 5.45180 | 2.47940 / 2.74970 | 1.91990 |

Current fresh submissions/completions, record/submit/acquire/present/unattributed scopes are retained separately for each control in JSON. No GPU saving automatically certifies 120 Hz wall-clock application pacing. Existing shared host orchestration is not assumed disposable; no second CPU optimization is bundled into the GPU placement responsibility.

## Minimum remaining controls and gate

1. Complete the one bulk placement correctness/allocator gate with exact prepared/geometry/output invariants, independent shader identity, portable memory choice, no hidden transfer/readback authority and unchanged publication. Root remains the only production writer.
2. Use fresh bracketing fixed baselines/control across Florida, inland, active and grazing for net total GPU/CPU tails. Noise-live remains only the material sensitivity comparison; do not ship it.
3. If that single placement responsibility passes meaningful payoff and correctness, run one bounded regional traversal including the complete64 replacement slices and neighboring settled owners, then the ticket-required validation. Do not assume regional closure or add copy/validator changes.
4. If placement fails those gates and no exact-output material responsibility emerges, recommend M13 closure with residual fixed/material and transition limits. Do not search indefinitely or require speculative scheduling just to make every transient 8.33 ms.

No new orbital/factor1/full-grid/warp performance recapture was required for this read-only causality map. Historical coverage is retained; root may run appropriate correctness regressions only after a qualifying candidate exists.

## Provenance and reproduction

Work A performed no builds, GPU runs or production edits. Only work-a.md/json were written. JSON stores source SHA-256 identities, last100 frame ranges, full fixed camera/workload identities, stage distributions, heap records, dynamic correlations and representative rows. Existing retained journals and this ticket’s whole.py are the reproduction authority; no bulk frame captures are required.

Statistics are computed with Python -B from lossless columnar rows. The accepted sequence join requires both GPU frame identity and value to match the next historical GPU row before applying positive/previous-frame Earth alignment. Fixed windows take the final100 directional rows and require submittedFrame == timingFrame. Current split timers join by Residual GPU.recordFrame; never take the latest dictionary entry from an aliased non-Earth frame.
