# Work A: regional replacement event decomposition

**NO SINGLE AVOIDABLE RESPONSIBILITY PROVEN from retained evidence.** The private vertex-validation journal explains the 42/64 failures as a mixture of existing current rendering cost, physical-preparation additions, and two first-slice draw-duration outliers. This is an event audit, not a production implementation or reopening of final-block work.

The source is the sealed `m13-residual-regional-frames/validation-vertices.json.gz`; SHA-256 and uncompressed identity are in work-a.json. Strict alignment requires `gpuFrame == geometryFrame == host.frame - 1`. All 64 samples are retained.

## Exact render-only controls

Each event has exactly one earlier render-only frame with identical current generation/topology, full camera/GPU/presentation/lighting/published-pupil bits, the same current physical allocation/memory handles and the same hardware TCS/TES counts. No incoming/current physical preparation or regional demand dispatch occurs on those controls. They are first-post-publication observations, not a statistically sampled settled baseline. Later post-job frames have a different current owner and are not mixed in.

| Incoming generation | Current generation / LOD | Event logical frames | R-1 control | Control total | Control draw | Current block | Event mean / peak | >8.33 |
|---|---|---|---:|---:|---:|---:|---:|---:|
| 17 | 16 / L14 | 334-343 | 333 | 7.38188 | 6.32904 | 0.99264 | 7.952552 / 10.19840 | 2/10 |
| 18 | 17 / L15 | 345-355 | 344 | 8.09944 | 6.96068 | 1.07828 | 8.801160 / 10.79456 | 9/11 |
| 19 | 18 / L16 | 357-367 | 356 | 8.93132 | 7.71216 | 1.15840 | 9.226920 / 10.66576 | 11/11 |
| 35 | 34 / L14 | 714-723 | 713 | 7.41544 | 6.35736 | 0.99756 | 8.091276 / 10.16788 | 3/10 |
| 36 | 35 / L15 | 725-735 | 724 | 8.34104 | 7.20696 | 1.07384 | 8.782916 / 10.98624 | 6/11 |
| 37 | 36 / L16 | 737-747 | 736 | 8.98280 | 7.76372 | 1.15816 | 9.396833 / 10.69556 | 11/11 |

The remaining 42 failures divide into 5/20 frames rendering current L14, 15/22 rendering current L15, and 22/22 rendering current L16. The L16 render-only controls already cost 8.93132/8.98280 ms. The L15 control costs 8.09944/8.34104 ms. Therefore the 42 failures cannot all be attributed to incremental replacement compute.

## Full64-frame decomposition

All times are milliseconds. Draw is the existing candidate timestamp scope. Cull/compact below belong to the current owner. Final is the already-reduced incoming readiness block. Barriers are the sum of the adjacent timed current and incoming-final barrier intervals; preparation post-barriers and initial host/demand barriers remain in the compute residual. Shader-copy time is inside preparation; transfer-engine commands/bytes are zero in these state records. No GPU demand dispatch or current physical-preparation dispatch occurs in any selected frame.

| Generation / event frame | Logical | Total | >8.33 by | Draw | Prep | Current cull | Current compact | Timed barriers | Final | Delta vs R-1 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 17 / R-0 | 334 | 7.59628 | -0.73372 | 6.38820 | 0.08560 | 0.66068 | 0.29280 | 0.04452 | 0.00000 | 0.21440 |
| 17 / R-1 | 335 | 7.47596 | -0.85404 | 6.31600 | 0.09428 | 0.66200 | 0.29316 | 0.04532 | 0.00000 | 0.09408 |
| 17 / R-2 | 336 | 7.65840 | -0.67160 | 6.36696 | 0.16024 | 0.68428 | 0.29188 | 0.04632 | 0.00000 | 0.27652 |
| 17 / R-3 | 337 | 7.54788 | -0.78212 | 6.39544 | 0.09252 | 0.66180 | 0.29244 | 0.04452 | 0.00000 | 0.16600 |
| 17 / R-4 | 338 | 7.53148 | -0.79852 | 6.38016 | 0.09332 | 0.65988 | 0.29132 | 0.04512 | 0.00000 | 0.14960 |
| 17 / R-5 | 339 | 7.53288 | -0.79712 | 6.38148 | 0.09204 | 0.66076 | 0.29228 | 0.04508 | 0.00000 | 0.15100 |
| 17 / R-6 | 340 | 7.47212 | -0.85788 | 6.32668 | 0.09256 | 0.65472 | 0.29120 | 0.04524 | 0.00000 | 0.09024 |
| 17 / R-7 | 341 | 7.47972 | -0.85028 | 6.33648 | 0.08324 | 0.66084 | 0.29172 | 0.04580 | 0.00000 | 0.09784 |
| 17 / R-8 | 342 | 9.03240 | 0.70240 | 6.38212 | 1.59732 | 0.65528 | 0.29144 | 0.04448 | 0.00000 | 1.65052 |
| 17 / R-9 | 343 | 10.19840 | 1.86840 | 6.38512 | 2.64696 | 0.66044 | 0.29264 | 0.05656 | 0.10484 | 2.81652 |
| 18 / R-0 | 345 | 8.44348 | 0.11348 | 7.16636 | 0.08436 | 0.71420 | 0.32364 | 0.04092 | 0.00000 | 0.34404 |
| 18 / R-1 | 346 | 8.35196 | 0.02196 | 7.13512 | 0.08388 | 0.71152 | 0.31908 | 0.04116 | 0.00000 | 0.25252 |
| 18 / R-2 | 347 | 8.33064 | 0.00064 | 7.11548 | 0.08320 | 0.71000 | 0.31960 | 0.04104 | 0.00000 | 0.23120 |
| 18 / R-3 | 348 | 8.40336 | 0.07336 | 7.18152 | 0.08988 | 0.71104 | 0.31868 | 0.04076 | 0.00000 | 0.30392 |
| 18 / R-4 | 349 | 8.32304 | -0.00696 | 7.10412 | 0.08632 | 0.71084 | 0.31916 | 0.04148 | 0.00000 | 0.22360 |
| 18 / R-5 | 350 | 8.37204 | 0.04204 | 7.15656 | 0.08328 | 0.71072 | 0.31856 | 0.04140 | 0.00000 | 0.27260 |
| 18 / R-6 | 351 | 8.24664 | -0.08336 | 7.02928 | 0.08440 | 0.71212 | 0.31928 | 0.04016 | 0.00000 | 0.14720 |
| 18 / R-7 | 352 | 8.38856 | 0.05856 | 7.16432 | 0.08900 | 0.71320 | 0.31976 | 0.04180 | 0.00000 | 0.28912 |
| 18 / R-8 | 353 | 8.86580 | 0.53580 | 7.12144 | 0.61144 | 0.71028 | 0.31948 | 0.04184 | 0.00000 | 0.76636 |
| 18 / R-9 | 354 | 10.79456 | 2.46456 | 6.99160 | 2.66964 | 0.71056 | 0.31916 | 0.04200 | 0.00000 | 2.69512 |
| 18 / R-10 | 355 | 10.29268 | 1.96268 | 7.10716 | 1.94276 | 0.71204 | 0.31984 | 0.05008 | 0.10784 | 2.19324 |
| 19 / R-0 | 357 | 9.05136 | 0.72136 | 7.74600 | 0.08452 | 0.76704 | 0.34576 | 0.04568 | 0.00000 | 0.12004 |
| 19 / R-1 | 358 | 9.07680 | 0.74680 | 7.70460 | 0.08576 | 0.76760 | 0.34600 | 0.04364 | 0.00000 | 0.14548 |
| 19 / R-2 | 359 | 9.05348 | 0.72348 | 7.74980 | 0.08316 | 0.76740 | 0.34516 | 0.04524 | 0.00000 | 0.12216 |
| 19 / R-3 | 360 | 8.97016 | 0.64016 | 7.66412 | 0.08592 | 0.76716 | 0.34600 | 0.04484 | 0.00000 | 0.03884 |
| 19 / R-4 | 361 | 8.97604 | 0.64604 | 7.67124 | 0.08424 | 0.76800 | 0.34576 | 0.04504 | 0.00000 | 0.04472 |
| 19 / R-5 | 362 | 9.04048 | 0.71048 | 7.73008 | 0.08900 | 0.76760 | 0.34516 | 0.04592 | 0.00000 | 0.10916 |
| 19 / R-6 | 363 | 8.97932 | 0.64932 | 7.67688 | 0.08440 | 0.76656 | 0.34556 | 0.04432 | 0.00000 | 0.04800 |
| 19 / R-7 | 364 | 9.04584 | 0.71584 | 7.74288 | 0.08472 | 0.76664 | 0.34536 | 0.04472 | 0.00000 | 0.11452 |
| 19 / R-8 | 365 | 9.03752 | 0.70752 | 7.73376 | 0.08448 | 0.76712 | 0.34452 | 0.04512 | 0.00000 | 0.10620 |
| 19 / R-9 | 366 | 9.59936 | 1.26936 | 7.66460 | 0.71432 | 0.76752 | 0.34616 | 0.04468 | 0.00000 | 0.66804 |
| 19 / R-10 | 367 | 10.66576 | 2.33576 | 7.74204 | 1.58212 | 0.76708 | 0.34480 | 0.05416 | 0.12152 | 1.73444 |
| 35 / R-0 | 714 | 8.83900 | 0.50900 | 7.50492 | 0.09444 | 0.74280 | 0.29312 | 0.04448 | 0.00000 | 1.42356 |
| 35 / R-1 | 715 | 7.59292 | -0.73708 | 6.44160 | 0.09348 | 0.65964 | 0.29256 | 0.04428 | 0.00000 | 0.17748 |
| 35 / R-2 | 716 | 7.51408 | -0.81592 | 6.36436 | 0.09284 | 0.65980 | 0.29120 | 0.04464 | 0.00000 | 0.09864 |
| 35 / R-3 | 717 | 7.49816 | -0.83184 | 6.35368 | 0.09256 | 0.65340 | 0.29152 | 0.04560 | 0.00000 | 0.08272 |
| 35 / R-4 | 718 | 7.52968 | -0.80032 | 6.38700 | 0.09292 | 0.65456 | 0.29096 | 0.04324 | 0.00000 | 0.11424 |
| 35 / R-5 | 719 | 7.52428 | -0.80572 | 6.38064 | 0.09264 | 0.65456 | 0.29052 | 0.04460 | 0.00000 | 0.10884 |
| 35 / R-6 | 720 | 7.49896 | -0.83104 | 6.34852 | 0.09188 | 0.66068 | 0.29100 | 0.04544 | 0.00000 | 0.08352 |
| 35 / R-7 | 721 | 7.48332 | -0.84668 | 6.33912 | 0.09196 | 0.65468 | 0.29128 | 0.04524 | 0.00000 | 0.06788 |
| 35 / R-8 | 722 | 9.26448 | 0.93448 | 6.38704 | 1.82412 | 0.65460 | 0.29100 | 0.04548 | 0.00000 | 1.84904 |
| 35 / R-9 | 723 | 10.16788 | 1.83788 | 6.36796 | 2.64036 | 0.65476 | 0.29108 | 0.05496 | 0.10568 | 2.75244 |
| 36 / R-0 | 725 | 8.29704 | -0.03296 | 7.08012 | 0.08380 | 0.71072 | 0.31888 | 0.04172 | 0.00000 | -0.04400 |
| 36 / R-1 | 726 | 8.28856 | -0.04144 | 7.07196 | 0.08304 | 0.71128 | 0.31960 | 0.04116 | 0.00000 | -0.05248 |
| 36 / R-2 | 727 | 8.28160 | -0.04840 | 7.06028 | 0.09000 | 0.71072 | 0.31804 | 0.04168 | 0.00000 | -0.05944 |
| 36 / R-3 | 728 | 8.23648 | -0.09352 | 7.02192 | 0.08268 | 0.71068 | 0.31876 | 0.04120 | 0.00000 | -0.10456 |
| 36 / R-4 | 729 | 8.20796 | -0.12204 | 6.99016 | 0.08468 | 0.71052 | 0.31948 | 0.04152 | 0.00000 | -0.13308 |
| 36 / R-5 | 730 | 8.39900 | 0.06900 | 7.18224 | 0.08392 | 0.71124 | 0.31920 | 0.04128 | 0.00000 | 0.05796 |
| 36 / R-6 | 731 | 8.34292 | 0.01292 | 7.12484 | 0.08596 | 0.71064 | 0.31872 | 0.04124 | 0.00000 | 0.00188 |
| 36 / R-7 | 732 | 8.43460 | 0.10460 | 7.21516 | 0.08804 | 0.71164 | 0.31856 | 0.03988 | 0.00000 | 0.09356 |
| 36 / R-8 | 733 | 8.85316 | 0.52316 | 7.10708 | 0.61216 | 0.71176 | 0.32044 | 0.04020 | 0.00000 | 0.51212 |
| 36 / R-9 | 734 | 10.98624 | 2.65624 | 7.19416 | 2.65808 | 0.71132 | 0.32020 | 0.04024 | 0.00000 | 2.64520 |
| 36 / R-10 | 735 | 10.28452 | 1.95452 | 7.09308 | 1.94148 | 0.71120 | 0.31916 | 0.05040 | 0.11572 | 1.94348 |
| 37 / R-0 | 737 | 10.69556 | 2.36556 | 9.18248 | 0.08420 | 0.90292 | 0.34928 | 0.04564 | 0.00000 | 1.71276 |
| 37 / R-1 | 738 | 9.08316 | 0.75316 | 7.77820 | 0.08372 | 0.76748 | 0.34712 | 0.04480 | 0.00000 | 0.10036 |
| 37 / R-2 | 739 | 9.01096 | 0.68096 | 7.70620 | 0.08516 | 0.76680 | 0.34520 | 0.04420 | 0.00000 | 0.02816 |
| 37 / R-3 | 740 | 9.09428 | 0.76428 | 7.72160 | 0.08480 | 0.76860 | 0.34584 | 0.04512 | 0.00000 | 0.11148 |
| 37 / R-4 | 741 | 9.06800 | 0.73800 | 7.68988 | 0.08396 | 0.76828 | 0.34640 | 0.04464 | 0.00000 | 0.08520 |
| 37 / R-5 | 742 | 9.04072 | 0.71072 | 7.73864 | 0.08372 | 0.76704 | 0.34584 | 0.04404 | 0.00000 | 0.05792 |
| 37 / R-6 | 743 | 8.96168 | 0.63168 | 7.65968 | 0.08292 | 0.76620 | 0.34588 | 0.04536 | 0.00000 | -0.02112 |
| 37 / R-7 | 744 | 8.98924 | 0.65924 | 7.68708 | 0.08340 | 0.76684 | 0.34476 | 0.04556 | 0.00000 | 0.00644 |
| 37 / R-8 | 745 | 8.95772 | 0.62772 | 7.65484 | 0.08316 | 0.76700 | 0.34524 | 0.04612 | 0.00000 | -0.02508 |
| 37 / R-9 | 746 | 9.86456 | 1.53456 | 7.67140 | 0.97144 | 0.76916 | 0.34536 | 0.04512 | 0.00000 | 0.88176 |
| 37 / R-10 | 747 | 10.59928 | 2.26928 | 7.67588 | 1.57984 | 0.76748 | 0.34536 | 0.05292 | 0.12364 | 1.61648 |

Current/incoming compute blocks use adjacent non-overlapping timestamps. Preparation is nested inside the whole-compute scope. Draw/background/material/scene timestamps must not be added as independent stages. `work-a.json` retains the whole-compute and arithmetic residual values, slice ranges, source/destination identities, hardware counts and per-frame matched deltas.

## Frequency and payoff limits

| Responsibility | Observed frequency | Total over 64 | Mean/frame | Peak/frame |
|---|---|---:|---:|---:|
| currentVisibilityCull | 64/64 display frames, including render-only controls | 45.935920 | 0.717749 | 0.902920 |
| currentCompaction | 64/64 display frames, including render-only controls | 20.468920 | 0.319827 | 0.349280 |
| currentVisibilityEntireBlock | 64/64 display frames | 69.239680 | 1.081870 | 1.298680 |
| incomingPhysicalPreparation | one bounded slice on64/64 work frames | 28.416240 | 0.444004 | 2.669640 |
| incomingFinalReadiness | 6/64 final slices | 0.679240 | 0.010613 | 0.123640 |
| timedComputeBarriers | current block64; incoming final6 | 2.850960 | 0.044546 | 0.056560 |

Current visibility and compaction also run in the render-only controls. Incoming existence is not what triggers those rebuilds. Frozen camera/current-physical inputs make the result invariant in these recorded events, but a production reuse path needs a complete visibility invalidation identity. Its zero-cost limit below is insufficient by itself. Regional demand has zero selected dispatches; its unconditional compute-to-host barrier remains untimed separately.

| Zero-cost single-responsibility subtraction | Mean | P95 | P99/peak | Remaining >8.33 |
|---|---:|---:|---:|---:|
| currentCull | 8.012320 | 9.83180 | 10.27492 | 13/64 |
| currentCompact | 8.410242 | 10.32096 | 10.66604 | 33/64 |
| wholeCurrentVisibilityBlock | 7.648199 | 9.44232 | 9.91384 | 11/64 |
| allIncomingPhysicalPreparation | 8.286065 | 9.00948 | 10.61136 | 27/64 |
| remainingIncomingFinalBlock | 8.719456 | 10.54424 | 10.98624 | 42/64 |
| allSeparatelyTimedComputeBarriers | 8.685522 | 10.61160 | 10.94600 | 39/64 |

These are optimistic arithmetic bounds, not measured candidates or proven avoidable work. They do not model a scheduling change that could also change draw duration. No single directly timed stage reaches 8.33 ms across the event. Do not silently combine unrelated stage removals to manufacture a winning projection.

## Contentious timing remains unclassified

Logical 714 and 737 are first slices with preparation only 0.09444/0.08420 ms. Their draw scope rises to 7.50492/9.18248 ms, versus 6.35736/7.76372 ms in the exact-current controls. Current cull also grows. These frames follow approximately 291.55/347.14 ms synchronous diagnostic map construction. That establishes a correlation, not a GPU clock, cache, queue-contention or memory-bandwidth diagnosis. No corresponding repeat or driver counters are retained.

The current reference implementation records current reset/cull/compact, incoming preparation/readiness, then drawing in one command buffer on the graphics queue. Incoming preparation uses a global compute-to-compute/vertex-shader/host barrier. A common frame fence covers incoming and current work. Thus physical ownership independence does not imply execution independence. The cost of changing those dependencies remains unmeasured.

Source references: `NovaCoreNative.cpp:1906-1934` (record ordering), `RegionalPhysicalPreparation.inl:82-91` (preparation barrier/readiness), `NovaCoreNative.cpp:2226` and `2321` (submission/fence), `RegionalPhysicalResidency.inl:129` (conditional demand and unconditional barrier).

## Smallest causal control

Root should add an exact-current render-only reference tick before the same incoming work tick, freezing camera/logical-route/pupil/generation and skipping only incoming dispatch in the reference. It must not advance the incoming cursor. Retain both total GPU and individual current/draw/incoming scopes, plus allocation identities and hardware TCS/TES. Compare the observed incremental total with the incoming block rather than infer contention from nominal overlap.

Record the preceding CPU map/index work and control ordering. A reference-first tick can warm clocks/caches and bias the work tick; if a promising interaction appears, bound it with repeats or balanced ordering. The diagnostic pair does not qualify publication latency. An incoming-only hidden-terrain control is unnecessary unless the paired measurements reveal a material reproducible interaction.

## Missing proof and judgment

The existing controls are single observations; clocks/cache/queue overlap are not measured. Shader-copy time and several barrier intervals are not split. Current physical byte hashes are not retained, although its owner, allocation and inputs remain stable and no current preparation occurs. The CPU lifecycle and real resource qualification belong to Work B. KSA equivalence belongs to Work C.

Retained measurements reject a direct single-stage removal as the closure answer. They do not reject a larger interaction at the scheduling boundary. One bounded paired-control measurement can materially classify that interaction. No production correction or full-suite gate is justified yet.

Only `work-a.md` and `work-a.json` were written by Work A. No production edits, builds or GPU runs.
