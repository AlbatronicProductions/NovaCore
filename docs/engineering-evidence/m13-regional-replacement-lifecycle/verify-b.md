# Verify B: matched render-only / replacement-work control

**No single scheduling correction is proven to close the 8.33 ms gate.** The corrected private paired control reproduces all six requested replacement jobs, with every one of their 64 slices retained. Its render-only reference already exceeds 8.33 ms on 26/64 frames; work exceeds it on 39/64. The incoming work accounts for the mean increment without a large measured current-draw or current-cull inflation. This is a diagnostic using the previously rejected mapped/placement/vertex control, not a new production candidate.

## Identity and alignment

- Corrected journal: 1,000 logical reference/work pairs; 2,000 native logical records; 1,679 strict aligned GPU observations; 839 complete GPU pairs. Missing alignment consists of non-Earth/transition periods plus final result latency, not 161 silently skipped tests. Every target64 pair has both GPU results.
- Alignment requires `gpuFrame > 0` and `gpuFrame == geometryFrame == host.frame - 1`. The extended GPU row must repeat that exact frame and GPU total. Phase is read from `Lifecycle transfers.reference`, not guessed from frame parity.
- All target64 pairs have byte-identical camera, GPU, presentation, lighting, published-pupil and incoming-pupil fields; current/incoming generations and topology hashes; dependencies; pre-record cursor; source/destination pupils and LODs; current/incoming/scratch physical and mapped-buffer handles; associated memory handles and map generations. TCS/TES counts match exactly.
- The reference leaves the incoming cursor unchanged and performs no incoming physical preparation or final block. Work advances it by the exact slice count. Expected post-record differences are cursor/count/final-slice/work-recorded bookkeeping, not ownership changes.
- All six complete event ranges and full target input bytes also match old A, the prior vertex control, and the fresh bank-semantics journal. No ordinal normalization is needed to make these target byte comparisons pass; normalized results are additionally retained in JSON.
- Global equivalence does not pass: paired camera/GPU differ on logical 120–149 and 180/280/400/500/580/660; published bytes differ on 128/139/150. Those frames are outside the target64 and are explicitly retained as failures in JSON. TCS/TES differ on 35 of 839 complete pairs globally. No full-route exactness claim is made.
- Clipping-output and fragment fields differ in all 64 target pairs. These fields are retained, but neither pixel parity nor a constant fragment invocation workload is asserted from the input/TCS/TES proof. There are no new depth/HDR/image captures.

## All target64 timings

All values are milliseconds; percentiles use nearest rank, with every tail included.

| Mode | Mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|
| Reference, incoming GPU work omitted | 8.254868 | 9.02848 | 9.79276 | 9.79276 | 26/64 | 0/64 |
| Work, unchanged prior rejected control | 8.676622 | 10.62408 | 10.92796 | 10.92796 | 39/64 | 0/64 |

| Incoming job | Source/destination generation | Source/destination LOD | Source/destination pupil | Logical slices | Vertex count | Reference mean / peak / >8.33 | Work mean / peak / >8.33 |
|---|---|---|---|---|---:|---|---|
| 17 | 16 → 17 | 14 → 15 | 25 → 26 | 334–343 (10) | 647,002 | 7.410436 / 7.52564 / 0 | 7.933892 / 10.12148 / 2 |
| 18 | 17 → 18 | 15 → 16 | 26 → 27 | 345–355 (11) | 696,410 | 8.230695 / 8.37556 / 2 | 8.790593 / 10.92796 / 7 |
| 19 | 18 → 19 | 16 → 17 | 27 → 28 | 357–367 (11) | 712,106 | 8.972564 / 9.12280 / 11 | 9.256040 / 10.67384 / 11 |
| 35 | 34 → 35 | 14 → 15 | 45 → 46 | 714–723 (10) | 647,002 | 7.439200 / 7.51184 / 0 | 7.932416 / 10.15284 / 2 |
| 36 | 35 → 36 | 15 → 16 | 46 → 47 | 725–735 (11) | 696,410 | 8.392236 / 9.79276 / 2 | 8.767160 / 10.88556 / 6 |
| 37 | 36 → 37 | 16 → 17 | 47 → 48 | 737–747 (11) | 712,106 | 8.933160 / 9.02848 / 11 | 9.244455 / 10.62408 / 11 |

Current source L14/L15/L16 produce respectively 273,100/302,089/331,531 TCS patches and 819,300/906,267/994,593 TES invocations in both phases. Both L16 events remain over 8.33 on every reference and work frame.

## Cost and additivity check

| Measured interval | Reference mean | Work mean | Work-reference mean | Delta P95 / peak |
|---|---:|---:|---:|---:|
| Total GPU | 8.254868 | 8.676622 | 0.421754 | 2.634000 / 2.757840 |
| Candidate draw interval | 7.109829 | 7.087787 | -0.022042 | 0.145160 / 0.179200 |
| Whole cull/preparation interval | 1.082506 | 1.529693 | 0.447187 | 2.657560 / 2.744400 |
| Current reset/cull/compact block | 1.080359 | 1.077803 | -0.002556 | 0.002360 / 0.019680 |
| Current cull | 0.715576 | 0.713348 | -0.002227 | 0.002200 / 0.005200 |
| Current compact | 0.319704 | 0.319547 | -0.000157 | 0.001560 / 0.019280 |
| Incoming physical + final block | 0.000000 | 0.448556 | 0.448556 | 2.657480 / 2.743960 |

Incoming physical preparation averages 0.438201 ms; its final readiness block adds 0.010356 ms averaged over all 64. The observed whole-compute increment differs from this incoming sum by −0.001369 ms on average. The total GPU increment minus incoming measured work is −0.026803 ms (P95 +0.148560; peak +0.180480). This supports a predominantly additive incoming cost in this ordered control; it does not establish an exact hardware execution partition.

Prep and current/final blocks are nested in whole cull/preparation. Candidate, detailed, scene, background and material/overlay measurements use different stage boundaries and overlapping intervals. They are not summed as independent stages. Any stored arithmetic remainder is labeled as a consistency residual, not an independently timed other GPU stage or recoverable optimization.

No regional demand or current physical-preparation dispatch runs in these 128 target native frames. Material transfer logs report zero upload commands/bytes for every target reference and work frame. Their earlier position outside the historical total timestamp therefore does not conceal target64 GPU payload copies. Host-side upload preparation can still consume CPU.

## Per-slice paired totals

| Logical | Job | Cursor / count | Reference | Work | Incoming physical | Incoming final | Draw delta | Current block delta | Total delta minus incoming |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 334 | 17 | 0 / 65536 | 7.39008 | 7.49144 | 0.08372 | 0.00000 | 0.01816 | -0.00156 | 0.01764 |
| 335 | 17 | 65536 / 65536 | 7.52564 | 7.48352 | 0.09132 | 0.00000 | -0.01728 | -0.00244 | -0.13344 |
| 336 | 17 | 131072 / 65536 | 7.48136 | 7.53088 | 0.08920 | 0.00000 | -0.03940 | -0.00052 | -0.03968 |
| 337 | 17 | 196608 / 65536 | 7.38000 | 7.46164 | 0.08464 | 0.00000 | -0.00376 | -0.00036 | -0.00300 |
| 338 | 17 | 262144 / 65536 | 7.40368 | 7.56784 | 0.09056 | 0.00000 | 0.07192 | 0.00120 | 0.07360 |
| 339 | 17 | 327680 / 65536 | 7.35692 | 7.50600 | 0.08964 | 0.00000 | 0.05872 | -0.00028 | 0.05944 |
| 340 | 17 | 393216 / 65536 | 7.43464 | 7.51100 | 0.08764 | 0.00000 | -0.01264 | 0.00036 | -0.01128 |
| 341 | 17 | 458752 / 65536 | 7.38328 | 7.46544 | 0.08448 | 0.00000 | -0.00200 | -0.00140 | -0.00232 |
| 342 | 17 | 524288 / 65536 | 7.38512 | 9.19968 | 1.81128 | 0.00000 | 0.00076 | 0.00108 | 0.00328 |
| 343 | 17 | 589824 / 57178 | 7.36364 | 10.12148 | 2.63912 | 0.10484 | 0.01300 | -0.00108 | 0.01388 |
| 345 | 18 | 0 / 65536 | 8.36192 | 8.25128 | 0.08996 | 0.00000 | -0.21572 | 0.00076 | -0.20060 |
| 346 | 18 | 65536 / 65536 | 8.13044 | 8.39412 | 0.08320 | 0.00000 | 0.17920 | -0.00044 | 0.18048 |
| 347 | 18 | 131072 / 65536 | 8.23304 | 8.39508 | 0.08336 | 0.00000 | 0.07904 | -0.00040 | 0.07868 |
| 348 | 18 | 196608 / 65536 | 8.25468 | 8.34864 | 0.08440 | 0.00000 | 0.00728 | 0.00140 | 0.00956 |
| 349 | 18 | 262144 / 65536 | 8.23956 | 8.31628 | 0.08300 | 0.00000 | -0.01092 | 0.00236 | -0.00628 |
| 350 | 18 | 327680 / 65536 | 8.14312 | 8.38028 | 0.08512 | 0.00000 | 0.15312 | -0.00156 | 0.15204 |
| 351 | 18 | 393216 / 65536 | 8.27084 | 8.21068 | 0.09108 | 0.00000 | -0.15160 | -0.00064 | -0.15124 |
| 352 | 18 | 458752 / 65536 | 8.37556 | 8.23312 | 0.08352 | 0.00000 | -0.22660 | -0.00088 | -0.22596 |
| 353 | 18 | 524288 / 65536 | 8.14880 | 8.87288 | 0.61632 | 0.00000 | 0.10800 | -0.00156 | 0.10776 |
| 354 | 18 | 589824 / 65536 | 8.20644 | 10.92796 | 2.65748 | 0.00000 | 0.06384 | -0.00136 | 0.06404 |
| 355 | 18 | 655360 / 41050 | 8.17324 | 10.36620 | 1.93796 | 0.10644 | 0.14516 | 0.00044 | 0.14856 |
| 357 | 19 | 0 / 65536 | 8.90464 | 8.99792 | 0.08396 | 0.00000 | 0.00768 | 0.00128 | 0.00932 |
| 358 | 19 | 65536 / 65536 | 8.89864 | 8.99872 | 0.08408 | 0.00000 | -0.02552 | -0.00152 | 0.01600 |
| 359 | 19 | 131072 / 65536 | 9.02092 | 9.16584 | 0.08384 | 0.00000 | 0.09756 | -0.00308 | 0.06108 |
| 360 | 19 | 196608 / 65536 | 8.97360 | 9.04556 | 0.08336 | 0.00000 | -0.00996 | -0.00184 | -0.01140 |
| 361 | 19 | 262144 / 65536 | 8.97164 | 9.00832 | 0.08452 | 0.00000 | -0.04996 | 0.00188 | -0.04784 |
| 362 | 19 | 327680 / 65536 | 8.94620 | 9.03216 | 0.09020 | 0.00000 | 0.01076 | -0.01552 | -0.00424 |
| 363 | 19 | 393216 / 65536 | 9.12280 | 9.03640 | 0.09040 | 0.00000 | -0.17832 | 0.00160 | -0.17680 |
| 364 | 19 | 458752 / 65536 | 8.99040 | 9.14640 | 0.08444 | 0.00000 | 0.07412 | -0.00332 | 0.07156 |
| 365 | 19 | 524288 / 65536 | 8.92304 | 9.05272 | 0.08540 | 0.00000 | 0.03856 | 0.00516 | 0.04428 |
| 366 | 19 | 589824 / 65536 | 9.06940 | 9.65856 | 0.71916 | 0.00000 | -0.12868 | -0.00176 | -0.13000 |
| 367 | 19 | 655360 / 56746 | 8.87692 | 10.67384 | 1.58864 | 0.11528 | 0.08856 | 0.00172 | 0.09300 |
| 714 | 35 | 0 / 65536 | 7.47824 | 7.53892 | 0.08496 | 0.00000 | 0.02332 | 0.00040 | -0.02428 |
| 715 | 35 | 65536 / 65536 | 7.43600 | 7.51140 | 0.08500 | 0.00000 | -0.00940 | -0.00068 | -0.00960 |
| 716 | 35 | 131072 / 65536 | 7.42136 | 7.55812 | 0.08940 | 0.00000 | 0.04944 | -0.00216 | 0.04736 |
| 717 | 35 | 196608 / 65536 | 7.43952 | 7.53080 | 0.08360 | 0.00000 | 0.00856 | -0.00136 | 0.00768 |
| 718 | 35 | 262144 / 65536 | 7.41532 | 7.56804 | 0.09112 | 0.00000 | 0.05960 | 0.00140 | 0.06160 |
| 719 | 35 | 327680 / 65536 | 7.51184 | 7.50252 | 0.08996 | 0.00000 | -0.11864 | 0.01968 | -0.09928 |
| 720 | 35 | 393216 / 65536 | 7.44140 | 7.49492 | 0.08772 | 0.00000 | -0.03504 | 0.00056 | -0.03420 |
| 721 | 35 | 458752 / 65536 | 7.37220 | 7.46528 | 0.08996 | 0.00000 | 0.00308 | -0.00080 | 0.00312 |
| 722 | 35 | 524288 / 65536 | 7.47752 | 9.00132 | 1.58960 | 0.00000 | -0.06492 | -0.00148 | -0.06580 |
| 723 | 35 | 589824 / 57178 | 7.39860 | 10.15284 | 2.63832 | 0.10436 | 0.01196 | -0.00188 | 0.01156 |
| 725 | 36 | 0 / 65536 | 9.79276 | 8.31652 | 0.08468 | 0.00000 | -1.42684 | -0.12584 | -1.56092 |
| 726 | 36 | 65536 / 65536 | 8.15920 | 8.29628 | 0.08628 | 0.00000 | 0.04792 | 0.00244 | 0.05080 |
| 727 | 36 | 131072 / 65536 | 8.35308 | 8.35452 | 0.08340 | 0.00000 | -0.08296 | 0.00060 | -0.08196 |
| 728 | 36 | 196608 / 65536 | 8.25468 | 8.38880 | 0.08672 | 0.00000 | 0.04836 | -0.00112 | 0.04740 |
| 729 | 36 | 262144 / 65536 | 8.30780 | 8.34052 | 0.08448 | 0.00000 | -0.05232 | -0.00008 | -0.05176 |
| 730 | 36 | 327680 / 65536 | 8.27156 | 8.29688 | 0.08352 | 0.00000 | -0.05832 | -0.00096 | -0.05820 |
| 731 | 36 | 393216 / 65536 | 8.29636 | 8.17580 | 0.09108 | 0.00000 | -0.21192 | 0.00060 | -0.21164 |
| 732 | 36 | 458752 / 65536 | 8.17936 | 8.27592 | 0.08424 | 0.00000 | 0.02040 | -0.00860 | 0.01232 |
| 733 | 36 | 524288 / 65536 | 8.20228 | 8.78680 | 0.61360 | 0.00000 | -0.02972 | 0.00008 | -0.02908 |
| 734 | 36 | 589824 / 65536 | 8.25156 | 10.88556 | 2.66156 | 0.00000 | -0.02592 | -0.00220 | -0.02756 |
| 735 | 36 | 655360 / 41050 | 8.24596 | 10.32116 | 1.94480 | 0.11644 | 0.01196 | 0.00020 | 0.01396 |
| 737 | 37 | 0 / 65536 | 8.94708 | 9.04576 | 0.08436 | 0.00000 | 0.05996 | -0.00492 | 0.01432 |
| 738 | 37 | 65536 / 65536 | 8.85324 | 9.09812 | 0.08440 | 0.00000 | 0.15852 | 0.00136 | 0.16048 |
| 739 | 37 | 131072 / 65536 | 8.88132 | 9.03552 | 0.08440 | 0.00000 | 0.07248 | -0.00312 | 0.06980 |
| 740 | 37 | 196608 / 65536 | 9.02848 | 9.00396 | 0.08372 | 0.00000 | -0.10476 | -0.00356 | -0.10824 |
| 741 | 37 | 262144 / 65536 | 8.89528 | 8.97260 | 0.08500 | 0.00000 | -0.00380 | -0.00424 | -0.00768 |
| 742 | 37 | 327680 / 65536 | 8.91556 | 9.08092 | 0.09008 | 0.00000 | 0.07676 | -0.00196 | 0.07528 |
| 743 | 37 | 393216 / 65536 | 8.93188 | 8.97420 | 0.09100 | 0.00000 | -0.04756 | -0.00116 | -0.04868 |
| 744 | 37 | 458752 / 65536 | 9.00512 | 9.08732 | 0.08688 | 0.00000 | -0.00052 | -0.00432 | -0.00468 |
| 745 | 37 | 524288 / 65536 | 8.87396 | 9.05600 | 0.08416 | 0.00000 | 0.09704 | 0.00056 | 0.09788 |
| 746 | 37 | 589824 / 65536 | 8.93156 | 9.71052 | 0.72104 | 0.00000 | 0.05520 | 0.00204 | 0.05792 |
| 747 | 37 | 655360 / 56746 | 9.00128 | 10.62408 | 1.59080 | 0.11540 | -0.08568 | -0.00076 | -0.08340 |

## What an impossible zero-cost stage would leave

These arithmetic bounds subtract one entire measured stage from each work frame while holding all other timings fixed. They are not proposed changes or performance promises.

| Entire interval set to zero | Remaining mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|
| allIncomingMeasured | 8.228066 | 9.00044 | 9.08200 | 9.08200 | 22/64 | 0/64 |
| entireCurrentBlock | 7.598819 | 9.46568 | 9.85488 | 9.85488 | 10/64 | 0/64 |
| currentCull | 7.963274 | 9.85664 | 10.21772 | 10.21772 | 15/64 | 0/64 |
| currentCompact | 8.357075 | 10.27856 | 10.60940 | 10.60940 | 32/64 | 0/64 |

Even removing all measured incoming GPU cost leaves 22/64 work-frame arithmetic totals above 8.33. The actual references, which skip the incoming work and its barriers, leave 26/64 above 8.33. Removing the entire current reset/cull/compact block from work leaves 10/64 above 8.33. Thus none of these single direct removals establishes full-regime closure. Secondary scheduling interactions remain unmeasured, but this experiment finds no large positive current-render inflation to support them as the winning cause.

## CPU, publication and ordered-control limits

The reference always precedes work. This warms current rendering before work and changes queue/cache/clock history. It deliberately doubles native render ticks per logical route step and is not a production publication-latency test.

All six synchronous private mapping passes occur before their first reference frame. Their durations are 303.3316, 329.2298, 352.0285, 319.3845, 314.5319, 332.0569 ms (total 1,950.5632 ms). All 36 maps total 9,292.3269 ms. They are diagnostic CPU work, not a qualified asynchronous production implementation.

At logical 725, after the map stall, the reference has 9.79276 ms total and 8.52352 ms candidate draw; work has 8.31652 ms total. The −1.42684 ms draw delta is preserved in all statistics and must not be credited to incoming preparation. Reference-first bias and CPU idle history remain plausible confounds; achieved GPU clocks/cache state were not sampled.

| CPU frame boundary | Mean total | P95 | P99/peak | Mean fence wait | Mean validation/upload |
|---|---:|---:|---:|---:|---:|
| Reference submission | 45.046491 | 365.2512 | 402.6434 | 8.354216 | 35.849444 |
| Work submission / reference completion | 11.014606 | 11.9240 | 14.4745 | 8.123564 | 2.077088 |
| Work completion | 11.397666 | 13.3287 | 13.6808 | 8.557523 | 2.017731 |

The JSON retains every selected submission/completion component, including acquire, record, submit, present and unattributed. CPU scopes are nested; candidate creation contains acquisition/descriptor/map work. Their frame labels around callbacks are not silently treated as additive production stages.

All 37 publication reports remain successful and retain their source/final-fence/dependency flags. The six target publication timings are:

| Incoming generation | Reported elapsed ms | Current retained | Complete dependencies | Final fence |
|---|---:|---|---|---|
| 17 | 208.8471 | true | 1 | true |
| 18 | 244.6306 | true | 1 | true |
| 19 | 260.6766 | true | 1 | true |
| 35 | 206.3516 | true | 1 | true |
| 36 | 248.0973 | true | 1 | true |
| 37 | 256.2430 | true | 1 | true |

These durations include the diagnostic reference schedule and are not eligible for production CPU/frame-pacing/publication acceptance.

## Fresh bank-semantics run versus retained A

All 1,000 logical input records match A byte-for-byte, including both pupil structures without ordinal normalization. All 839 strict-aligned TCS/TES counts match. This establishes matching logged input/geometry workload; clipping/fragments differ and no raster parity is inferred.

| Run / population | Mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|
| oldA target64 | 13.876428 | 14.92424 | 16.68492 | 16.68492 | 64/64 | 64/64 |
| oldA all839 | 10.496176 | 13.53300 | 14.64660 | 16.68492 | 728/839 | 206/839 |
| bank target64 | 14.270402 | 15.62185 | 16.95828 | 16.95828 | 64/64 | 64/64 |
| bank all839 | 10.939641 | 13.97100 | 15.15808 | 16.95828 | 744/839 | 593/839 |
| oldVertices target64 | 8.730069 | 10.66576 | 10.98624 | 10.98624 | 42/64 | 0/64 |
| oldVertices all839 | 7.893414 | 9.28412 | 10.63988 | 10.98624 | 531/839 | 0/839 |

The fresh bank run is slower despite identical logged input and tessellation counts. Its all839 >11.11 count is 593 versus 206 in old A. This is material cross-run timing variability; this audit does not assign it to a new rendering cause or count the paired-control comparison as an isolated production optimization.

## Superseded first paired experiment

The first `reference-paired` experiment called the managed coordinator twice per logical frame. Its six jobs began 327/337/348/707/717/728, not 334/345/357/714/725/737. Incoming allocation identity was absent in the first reference of each selected job. That run remains diagnostic history and is not used to claim reproduction of the original 42 failures. The corrected coordinator advances exactly once per logical route step and restores the requested complete events.

## Gate decision and missing proof

The evidence supports stopping the single-winner gauntlet without production implementation. It does not prove a harmful incoming/current scheduling interaction that supplies enough recoverable cost for 8.33 across all 64 frames. Current L16 render-only work itself exceeds the gate. Combining unrelated placement, reuse, shading, culling or CPU changes is outside this one-responsibility proof.

No further GPU run is justified by a newly identified single cause here. A later architectural decision would need a concrete responsibility and a bounded control that can close the whole regime while preserving publication, exact physical authority, raster output and end-to-end CPU latency. No full validation suite was run by this audit.

## Reproduction

Read-only Python with `-B`; load the named `.json` when present or losslessly decompress `.json.gz`. The source identities (including uncompressed SHA-256), exact event list, per-frame input/state/handle bytes, timing rows, all mismatch lists and nearest-rank statistics are in `verify-b.json`. Do not use a logical-key dictionary that overwrites one phase; select the native reference/work pair and require full target identity before computing differences.

The compact verifier below recomputes input joins, stage metrics and complete per-slice records without writing or running GPU work. Set the working directory to the repository root.

```python
import pathlib,json,gzip,collections,math,statistics,hashlib
P=pathlib.Path('docs/engineering-evidence');HERE=P/'m13-regional-replacement-lifecycle'
TARGET=[*range(334,344),*range(345,356),*range(357,368),*range(714,724),*range(725,736),*range(737,748)]
GENS=[17,18,19,35,36,37]
def load(p):
 actual=p if p.exists() else p.with_suffix('.json.gz');raw=actual.read_bytes();plain=gzip.decompress(raw) if actual.suffix=='.gz' else raw
 return json.loads(plain),{'path':str(actual),'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'uncompressedBytes':len(plain),'uncompressedSha256':hashlib.sha256(plain).hexdigest()}
def fields(s):return dict(x.strip().split('=',1) for x in s.split(': ',1)[1].split(';') if '=' in x)
def up(v):return [{**v['constants'],**{k:x[i] for k,x in v['columns'].items()}} for i in range(v['count'])] if isinstance(v,dict) else v
def phy(x):
 b=bytes.fromhex(x);return (b[:128]+b[136:144]+b[152:156]).hex()
def stats(v):
 v=sorted(v)
 if not v:return None
 return dict(count=len(v),mean=statistics.mean(v),p95=v[math.ceil(.95*len(v))-1],p99=v[math.ceil(.99*len(v))-1],peak=max(v),minimum=min(v),above8_33=sum(x>8.33 for x in v),above11_11=sum(x>11.11 for x in v))
def parse(name,path):
 d,identity=load(path);j={};logical=0;logicalLists=collections.defaultdict(list);transfers={};state={};scopes={};maps=[];pubs=[];cpu=[];slices={};native=0
 for index,line in enumerate(d['lines']):
  if 'Composition route frame:' in line:logical=int(fields(line)['logicalFrame'])
  elif 'Composition frame:' in line:
   f=fields(line);native=int(f['frame'])
   if logical:j[native]={**f,'logical':logical};logicalLists[logical].append(native)
  elif 'Lifecycle transfers:' in line:f=fields(line);transfers[int(f['frame'])]=f
  elif 'Residual state:' in line:f=fields(line);state[int(f['recordFrame'])]=f
  elif 'Residual GPU:' in line:f=fields(line);scopes[(int(f['recordFrame']),int(f['incoming']))]=f
  elif 'Composition map:' in line:maps.append({**fields(line),'lineIndex':index,'lastCompositionNativeFrame':native,'logicalAtLine':logical})
  elif 'Prep publication:' in line:pubs.append(fields(line))
  elif 'Lifecycle CPU:' in line:cpu.append({**fields(line),'lineIndex':index})
  elif 'NCSM1 physical slice:' in line:
   f=fields(line)
   if f['incoming']=='1':slices[native]=f
 host=up(d['hostRows']);gpus=up(d['gpuRows']);render={};hostByFrame={x['frame']:x for x in host};alignment=[]
 for row in host:
  n=row['gpuFrame']
  if n>0 and n==row['geometryFrame']==row['frame']-1 and n in j:
   gpu=gpus[row['frame']-3]
   assert gpu['frame']==n and gpu['total']==row['gpuTotal'],(name,n,gpu)
   render[n]={'host':row,'gpu':gpu};alignment.append(n)
 prep={k:{int(x['frame']):x['ms'] for x in v} for k,v in d['preparation'].items()}
 return dict(name=name,d=d,identity=identity,j=j,logical=logicalLists,transfers=transfers,state=state,scopes=scopes,maps=maps,pubs=pubs,cpu=cpu,slices=slices,render=render,host=hostByFrame,prep=prep)
MODES={
 'oldA':parse('oldA',P/'m13-regional-preparation-convergence/A.json'),
 'oldVertices':parse('oldVertices',P/'m13-residual-regional-frames/validation-vertices.json'),
 'bank':parse('bank',HERE/'bank-event.json'),
 'pairedUncorrected':parse('pairedUncorrected',HERE/'reference-paired.json'),
 'pairedCorrected':parse('pairedCorrected',HERE/'reference-paired-coordinator.json')}
def phase(m,n):return int(m['transfers'][n]['reference'])
def frameobs(m,n):
 j=m['j'][n];r=m['render'][n];g=r['gpu'];h=r['host'];c=m['scopes'].get((n,0),{});i=m['scopes'].get((n,1),{})
 result=dict(nativeFrame=n,logicalFrame=j['logical'],identity=j,phase=phase(m,n) if n in m['transfers'] else 0,gpu=g,hardware={k:h[k] for k in ['gpuGeneration','tcsPatches','tesInvocations','clippingOutput','fragments']},
 current={k:float(v) for k,v in c.items() if k.endswith('Ms')},incomingFinal={k:float(v) for k,v in i.items() if k.endswith('Ms')},
 prep={k:rows.get(n,0) for k,rows in m['prep'].items()},
 state=m['state'].get(n),slice=m['slices'].get(n),transfers=m['transfers'].get(n),submissionCpu=m['host'][n],completionCpu=h)
 result['incomingMeasuredMs']=result['prep']['incomingPhysicalPreparation']+result['incomingFinal'].get('blockMs',0)
 result['historicalComputeResidualMs']=g['cullAndPreparation']-result['current'].get('blockMs',0)-result['incomingMeasuredMs']-result['prep']['currentPhysicalPreparation']-result['prep']['demand']
 result['totalMinusCandidateAndComputeMs']=g['total']-g['candidate']-g['cullAndPreparation']
 return result
def comparison(a,b,normalize=False):
 different={}
 for k in ['generation','incomingGeneration','currentTopology','incomingTopology','cameraBits','gpuBits','presentationBits','lightingBits','publishedBits','incomingBits','cursor','active','completeDependencies']:
  x=a[k];y=b[k]
  if normalize and k in ['publishedBits','incomingBits']:x=phy(x);y=phy(y)
  if x!=y:different[k]=[x,y]
 return different
def cross(a,b,phaseB=0):
 rows=[];render=[];eq=collections.defaultdict(list);full=collections.defaultdict(list);hardware=collections.defaultdict(list)
 for logical in range(1,1001):
  na=a['logical'][logical][0]
  bs=[n for n in b['logical'][logical] if phase(b,n)==phaseB] if len(b['logical'][logical])==2 else b['logical'][logical]
  nb=bs[0]
  for k in comparison(a['j'][na],b['j'][nb],True):eq[k].append(logical)
  for k in comparison(a['j'][na],b['j'][nb],False):full[k].append(logical)
  if na in a['render'] and nb in b['render']:
   render.append(logical)
   for k in ['tcsPatches','tesInvocations','clippingOutput','fragments']:
    if a['render'][na]['host'][k]!=b['render'][nb]['host'][k]:hardware[k].append(logical)
 return dict(logicalFrames=1000,strictGpuPairs=len(render),differentPhysicalIdentityFrames=dict(eq),differentFullByteFrames=dict(full),hardwareDifferentFrames=dict(hardware),target64PhysicalExact=not any(set(x)&set(TARGET) for x in eq.values()),target64TcsTesExact=not any(set(hardware[k])&set(TARGET) for k in ['tcsPatches','tesInvocations']))
def paired(m):
 pairRows=[];fieldsDiff=collections.defaultdict(list);statesDiff=collections.defaultdict(list);hardwareDiff=collections.defaultdict(list);notAligned=[];jobs={}
 for logical,natives in m['logical'].items():
  assert len(natives)==2
  nr,nw=sorted(natives,key=lambda n:-phase(m,n))
  assert [phase(m,nr),phase(m,nw)]==[1,0]
  for k in comparison(m['j'][nr],m['j'][nw]):fieldsDiff[k].append(logical)
  for k in m['state'][nr]:
   if m['state'][nr][k]!=m['state'][nw][k]:statesDiff[k].append(logical)
  if nr not in m['render'] or nw not in m['render']:notAligned.append(logical);continue
  r=frameobs(m,nr);w=frameobs(m,nw)
  for k in ['tcsPatches','tesInvocations','clippingOutput','fragments']:
   if r['hardware'][k]!=w['hardware'][k]:hardwareDiff[k].append(logical)
  delta={k:w['gpu'][k]-r['gpu'][k] for k in w['gpu']}
  delta.pop('frame')
  delta['incomingMeasuredMs']=w['incomingMeasuredMs']-r['incomingMeasuredMs']
  delta['totalMinusIncomingMs']=delta['total']-delta['incomingMeasuredMs']
  delta['currentBlockMs']=w['current'].get('blockMs',0)-r['current'].get('blockMs',0)
  delta['currentCullMs']=w['current'].get('cullMs',0)-r['current'].get('cullMs',0)
  delta['currentCompactMs']=w['current'].get('compactMs',0)-r['current'].get('compactMs',0)
  delta['computeMinusIncomingMs']=delta['cullAndPreparation']-delta['incomingMeasuredMs']
  pairRows.append(dict(logical=logical,reference=r,work=w,delta=delta))
 selected=[x for x in pairRows if int(x['work']['identity']['incomingGeneration']) in GENS and x['work']['slice'] is not None]
 for g in GENS:
  jobs[g]=[x for x in selected if int(x['work']['identity']['incomingGeneration'])==g]
 return dict(rows=pairRows,selected=selected,jobs=jobs,fieldsDiff=dict(fieldsDiff),statesDiff=dict(statesDiff),hardwareDiff=dict(hardwareDiff),notAligned=notAligned)
PAIR={k:paired(MODES[k]) for k in ['pairedUncorrected','pairedCorrected']}
def pairmetrics(rows):
 out={}
 for phaseName in ['reference','work']:
  obs=[x[phaseName] for x in rows]
  out[phaseName]=dict(gpu={k:stats([x['gpu'][k] for x in obs]) for k in ['total','candidate','cullAndPreparation','toneMap']},
    current={k:stats([x['current'].get(k,0) for x in obs]) for k in ['blockMs','resetMs','resetBarrierMs','cullMs','cullBarrierMs','compactMs','compactBarrierMs']},
    incomingPrep=stats([x['prep']['incomingPhysicalPreparation'] for x in obs]),
    incomingFinal=stats([x['incomingFinal'].get('blockMs',0) for x in obs]),
    incomingMeasured=stats([x['incomingMeasuredMs'] for x in obs]),
    computeResidual=stats([x['historicalComputeResidualMs'] for x in obs]),
    totalMinusCandidateAndCompute=stats([x['totalMinusCandidateAndComputeMs'] for x in obs]),
    submissionCpu={k:stats([x['submissionCpu'][k] for x in obs]) for k in ['total','update','fenceWait','inspection','hostCallback','validationUpload','acquire','record','submit','present','unattributed']},
    completionCpu={k:stats([x['completionCpu'][k] for x in obs]) for k in ['total','update','fenceWait','inspection','hostCallback','validationUpload','acquire','record','submit','present','unattributed']},
    above8_33=[x['logicalFrame'] for x in obs if x['gpu']['total']>8.33],above11_11=[x['logicalFrame'] for x in obs if x['gpu']['total']>11.11])
 out['delta']={k:stats([x['delta'][k] for x in rows]) for k in ['total','candidate','cullAndPreparation','currentBlockMs','currentCullMs','currentCompactMs','incomingMeasuredMs','totalMinusIncomingMs','computeMinusIncomingMs']}
 return out

pc=PAIR["pairedCorrected"]
assert [x["logical"] for x in pc["selected"]]==TARGET
assert all(not comparison(x["reference"]["identity"],x["work"]["identity"]) for x in pc["selected"])
print(json.dumps(pairmetrics(pc["selected"]),indent=2))
print(json.dumps(cross(MODES["oldA"],MODES["bank"]),indent=2))
```
