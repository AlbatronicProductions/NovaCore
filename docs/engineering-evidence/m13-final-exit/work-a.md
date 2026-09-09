# Work A: fresh banked M13.5 whole-frame map

Authoritative bank: `d4baab6940a57a46e478b98e36f5e45d1c4b558f`. This worker performs no builds, GPU runs or production writes.

## Measurement boundary

Only fresh journals based on this bank are included. Bank-native measurements, private timing, the subsequent cached-key prototype and the implemented unbanked default candidate are labeled separately; a bank field is source provenance, not proof every private control or candidate is the production baseline. Candidate filenames override inherited unprefixed internal labels. Quiet fixed GPU and separate profile CPU rows remain distinct runs. No M13.4 or pre-banking candidate measurements are substituted. Percentiles use nearest rank; threshold counts are strict greater-than.

GPU/geometry/completion joins require positive gpuFrame == geometryFrame == host.frame - 1. Child GPU scopes additionally require exact frame and total equality. Inspection precedes ++a.frame in UpdatePlanetary: preparation log frame N and split recordFrame N both identify GPU N, completed during host frame N+1. Publication marker N introduces the new owner in host/draw N+1. CPU submission and completion components are separate.

Do not add candidate/draw children or compute children to their enclosing spans. No raster parity, exact per-stage material time, cache behavior or calibrated publication latency follows from these timings.

## Fresh fixed runs

| Run | Mode | GPU median / P95 / P99 | Draw median | Completion CPU median / P95 / P99 | >8.33 / >11.11 |
|---|---|---|---:|---|---|
| cached-florida | cached-key prototype | 9.12292 / 9.20600 / 9.22828 | 7.70716 | 9.92020 / 10.08710 / 10.31600 | 100 / 0 |
| cached-orbital | cached-key prototype | 1.11960 / 1.25600 / 1.27196 | 0.81352 | 5.54400 / 5.82300 / 5.86960 | 0 / 0 |
| cached-repeat-florida | cached-key prototype | 9.13960 / 9.22784 / 9.45332 | 7.71220 | 10.03250 / 10.30900 / 10.53280 | 100 / 0 |
| cached-repeat-orbital | cached-key prototype | 1.14120 / 1.24700 / 1.26716 | 0.83304 | 5.52910 / 5.83910 / 5.90580 | 0 / 0 |
| candidate-cpu-florida | implemented unbanked default candidate | 8.93788 / 9.11884 / 9.13880 | 7.69604 | 9.76600 / 10.18900 / 10.31290 | 100 / 0 |
| candidate-cpu-inland | implemented unbanked default candidate | 8.03832 / 8.21532 / 8.22704 | 6.76244 | 8.77230 / 9.08470 / 9.12690 | 0 / 0 |
| candidate-fixed-active-refinement | implemented unbanked default candidate | 4.70000 / 4.85920 / 4.87168 | 3.40964 | unavailable | 0 / 0 |
| candidate-fixed-factor1 | implemented unbanked default candidate | 3.52580 / 3.68496 / 3.72064 | 2.30652 | unavailable | 0 / 0 |
| candidate-fixed-florida | implemented unbanked default candidate | 8.95040 / 9.07908 / 9.12464 | 7.69968 | unavailable | 100 / 0 |
| candidate-fixed-grazing | implemented unbanked default candidate | 3.86328 / 3.97364 / 4.07700 | 2.60736 | unavailable | 0 / 0 |
| candidate-fixed-inland | implemented unbanked default candidate | 8.05332 / 8.23944 / 8.30720 | 6.76292 | unavailable | 0 / 0 |
| candidate-fixed-orbital | implemented unbanked default candidate | 0.82540 / 0.90936 / 0.99560 | 0.73492 | unavailable | 0 / 0 |
| cpu-active-refinement | bank semantics / private timing | 4.58896 / 4.60912 / 4.61200 | 3.40756 | 7.06330 / 7.65690 / 8.20580 | 0 / 0 |
| cpu-factor1 | bank semantics / private timing | 3.50464 / 3.57728 / 3.58324 | 2.30576 | 5.98530 / 6.42250 / 6.70110 | 0 / 0 |
| cpu-florida | bank semantics / private timing | 8.87888 / 8.94352 / 8.97668 | 7.70156 | 11.45950 / 11.88200 / 12.03950 | 100 / 0 |
| cpu-grazing | bank semantics / private timing | 3.80072 / 3.81596 / 3.83008 | 2.61124 | 6.24240 / 6.60660 / 6.70570 | 0 / 0 |
| cpu-inland | bank semantics / private timing | 7.94772 / 7.95888 / 7.96644 | 6.76080 | 10.46980 / 10.77560 / 11.04340 | 0 / 0 |
| cpu-orbital | bank semantics / private timing | 0.83224 / 0.87124 / 0.88292 | 0.73660 | 3.40590 / 3.74020 / 3.82800 | 0 / 0 |
| fixed-active-refinement | bank native | 4.58716 / 4.60588 / 4.60992 | 3.40768 | unavailable | 0 / 0 |
| fixed-factor1 | bank native | 3.50228 / 3.51728 / 3.52040 | 2.31040 | unavailable | 0 / 0 |
| fixed-florida | bank native | 8.88412 / 8.92772 / 8.94396 | 7.70692 | unavailable | 100 / 0 |
| fixed-grazing | bank native | 3.79564 / 3.81368 / 3.82044 | 2.60444 | unavailable | 0 / 0 |
| fixed-inland | bank native | 7.94880 / 7.95904 / 7.96092 | 6.76156 | unavailable | 0 / 0 |
| fixed-orbital | bank native | 0.82864 / 0.83716 / 0.83872 | 0.73636 | unavailable | 0 / 0 |
| poll-florida | bank key placement / polling instrument | 8.87696 / 8.92284 / 8.95232 | 7.70408 | 11.44840 / 11.71570 / 11.83370 | 100 / 0 |
| poll-orbital | bank key placement / polling instrument | 0.82976 / 0.84072 / 0.87444 | 0.73688 | 3.28470 / 3.48190 / 3.69190 | 0 / 0 |
| poll-return-florida | bank key placement / polling instrument | 9.10520 / 9.20364 / 9.23564 | 7.70056 | 11.77030 / 12.13190 / 12.27670 | 100 / 0 |
| poll-return-orbital | bank key placement / polling instrument | 1.13668 / 1.26416 / 1.29784 | 0.81972 | 5.54810 / 5.86110 / 6.28330 | 0 / 0 |

## Dynamic/control coverage

| Run | Aligned Earth frames | GPU median / P95 / P99 / peak | >8.33 / >11.11 | Publications / replacements | Exit / logged errors |
|---|---:|---|---|---|---|
| cached-full | 1698 | 4.89156 / 7.06960 / 7.98880 / 8.62480 | 9 / 0 | 58 / 57 | 0 / 0 |
| cached-regional | 737 | 8.94876 / 11.87425 / 12.82576 / 13.15552 | 554 / 58 | 37 / 36 | 0 / 0 |
| candidate-dynamic-full | 1698 | 4.75400 / 6.89472 / 7.93868 / 8.46020 | 2 / 0 | 58 / 57 | 0 / 0 |
| candidate-dynamic-regional | 737 | 8.77008 / 11.59208 / 12.49588 / 12.84380 | 541 / 44 | 37 / 36 | 0 / 0 |
| candidate-dynamic-warp | 745 | 3.46220 / 3.66908 / 5.01572 / 5.14892 | 0 / 0 | 1 / 0 | 0 / 0 |
| control-full | 0 | unavailable (sparse normal telemetry) | unavailable | 58 / 57 | 0 / 0 |
| control-regional | 0 | unavailable (sparse normal telemetry) | unavailable | 37 / 36 | 0 / 0 |
| control-warp | 0 | unavailable (sparse normal telemetry) | unavailable | 1 / 0 | 0 / 0 |
| dynamic-active-grid | 809 | 3.71404 / 4.62448 / 6.05980 / 6.08536 | 0 / 0 | 1 / 0 | 0 / 0 |
| dynamic-full | 1698 | 4.68676 / 6.79024 / 7.74608 / 8.13880 | 0 / 0 | 58 / 57 | 0 / 0 |
| dynamic-inland-grid | 809 | 6.77240 / 8.89196 / 9.21376 / 9.31404 | 202 / 0 | 1 / 0 | 0 / 0 |
| dynamic-regional-repeat | 737 | 8.70584 / 11.58168 / 12.32072 / 12.67580 | 534 / 42 | 37 / 36 | 0 / 0 |
| dynamic-regional | 737 | 8.70492 / 11.57328 / 12.33732 / 12.66220 | 533 / 42 | 37 / 36 | 0 / 0 |
| dynamic-warp | 745 | 3.43968 / 3.46144 / 4.94864 / 4.96764 | 0 / 0 | 1 / 0 | 0 / 0 |
| poll-full | 1697 | 4.89380 / 7.11404 / 8.09588 / 8.87760 | 8 / 0 | 58 / 57 | 0 / 0 |
| poll-regional | 737 | 8.84548 / 11.75540 / 12.64832 / 13.04008 | 557 / 48 | 37 / 36 | 0 / 0 |
| poll-return-regional | 737 | 8.90560 / 11.91204 / 12.71283 / 13.35426 | 551 / 58 | 37 / 36 | 0 / 0 |
| quiet-cached-regional | 0 | unavailable (sparse normal telemetry) | unavailable | 37 / 36 | 0 / 0 |
| quiet-poll-regional | 0 | unavailable (sparse normal telemetry) | unavailable | 37 / 36 | 0 / 0 |

## CPU critical path and steady work

Profile warm100 medians below are the submitted frame's components. Update contains fence/inspection/callback/upload; these are not independent additive columns. Whole-frame-minus-fence is descriptive non-fence wall work, not proven removable CPU computation.

| Profile pose | CPU total / update | Fence / non-fence | Inspection / callback / validation-upload | Record / submit / acquire / present |
|---|---|---|---|---|
| cached-florida | 9.9185 / 9.3864 | 9.0314 / 0.8495 | 0.1091 / 0.2158 / 0.0342 | 0.2029 / 0.0500 / 0.0476 / 0.1784 |
| cached-orbital | 5.5371 / 5.0507 | 4.7597 / 0.7806 | 0.1116 / 0.1301 / 0.0454 | 0.1955 / 0.0490 / 0.0477 / 0.1727 |
| cached-repeat-florida | 10.0271 / 9.4796 | 9.0756 / 0.9336 | 0.1157 / 0.2260 / 0.0378 | 0.2330 / 0.0543 / 0.0524 / 0.1904 |
| cached-repeat-orbital | 5.5291 / 5.0721 | 4.7406 / 0.7467 | 0.1129 / 0.1299 / 0.0454 | 0.1806 / 0.0464 / 0.0454 / 0.1652 |
| candidate-cpu-florida | 9.7659 / 9.2384 | 8.8237 / 0.9250 | 0.1204 / 0.2310 / 0.0218 | 0.1809 / 0.0444 / 0.0446 / 0.2123 |
| candidate-cpu-inland | 8.7723 / 8.3082 | 7.9613 / 0.8247 | 0.1244 / 0.1683 / 0.0329 | 0.1715 / 0.0448 / 0.0444 / 0.1833 |
| cpu-active-refinement | 7.0627 / 6.6454 | 4.5095 / 2.5439 | 0.1008 / 0.0944 / 1.9356 | 0.1526 / 0.0404 / 0.0410 / 0.1692 |
| cpu-factor1 | 5.9853 / 5.5605 | 3.4303 / 2.5535 | 0.1001 / 0.0985 / 1.9278 | 0.1583 / 0.0411 / 0.0414 / 0.1760 |
| cpu-florida | 11.4595 / 11.0057 | 8.7929 / 2.6632 | 0.1059 / 0.1602 / 1.9332 | 0.1766 / 0.0442 / 0.0435 / 0.1789 |
| cpu-grazing | 6.2424 / 5.8273 | 3.7253 / 2.5200 | 0.0981 / 0.0910 / 1.9189 | 0.1535 / 0.0401 / 0.0406 / 0.1674 |
| cpu-inland | 10.4698 / 10.0274 | 7.8676 / 2.6014 | 0.1014 / 0.0963 / 1.9588 | 0.1648 / 0.0435 / 0.0429 / 0.1735 |
| cpu-orbital | 3.4057 / 2.9667 | 0.7546 / 2.6514 | 0.1026 / 0.1372 / 1.9643 | 0.1608 / 0.0423 / 0.0419 / 0.1716 |
| poll-florida | 11.4484 / 11.0018 | 8.7988 / 2.6479 | 0.1018 / 0.1554 / 1.9362 | 0.1683 / 0.0433 / 0.0424 / 0.1766 |
| poll-orbital | 3.2847 / 2.8755 | 0.7564 / 2.5263 | 0.0981 / 0.0967 / 1.9099 | 0.1491 / 0.0402 / 0.0395 / 0.1658 |
| poll-return-florida | 11.7703 / 11.2553 | 9.0027 / 2.7282 | 0.1099 / 0.2156 / 1.8889 | 0.2071 / 0.0531 / 0.0501 / 0.1901 |
| poll-return-orbital | 5.5341 / 5.0701 | 2.9195 / 2.5859 | 0.1155 / 0.1283 / 1.8832 | 0.1874 / 0.0482 / 0.0473 / 0.1673 |

The fresh mixed validation/upload scope is nearly constant across cheap and expensive fixed poses. The subsequent poll-* nested timer identifies QueueProductionRequests at about1.9 ms per frame even with zero new requests/pending work and126 pinned records/uploads. The cached-key prototype changes only backing memory for that CPU-read key role and is separately labeled above. Verify B owns its net CPU/pacing and run-order challenge; broad-scope subtraction alone does not prove recovery, and no total GPU saving is inferred.

## Recurring dynamic cost and publication boundary

The explicit publication marker records the just-completed GPU identity N before UpdatePlanetary increments it. Host/draw N+1 uses the new owner. The first validated publication provides an exact initial-authority boundary; it is not asserted that every pre-boundary CPU cost is intrinsically startup-only on later replacements. All later similar costs remain in the table and retained outlier rows.

| Profile route | First authoritative host | CPU after first publication median / P95 / P99 / peak | >16.67 / >40 | Current cull / compact median |
|---|---:|---|---|---|
| cached-full | 3 | 5.87390 / 8.18100 / 9.82440 / 22.51010 | 9 / 0 | 0.65660 / 0.01456 |
| cached-regional | 103 | 9.66700 / 12.69910 / 13.91210 / 23.18010 | 1 / 0 | 0.73448 / 0.01840 |
| candidate-dynamic-full | 3 | 5.74980 / 7.99350 / 11.03990 / 50.44250 | 8 / 1 | 0.65324 / 0.01460 |
| candidate-dynamic-regional | 103 | 9.43260 / 12.31440 / 13.42870 / 21.07470 | 1 / 0 | 0.73164 / 0.01832 |
| candidate-dynamic-warp | 34 | 4.22810 / 5.04740 / 6.58200 / 11.33190 | 0 / 0 | 0.73072 / 0.01816 |
| dynamic-active-grid | 34 | 6.31120 / 7.41140 / 8.62600 / 12.87800 | 0 / 0 | 1.09976 / 0.01120 |
| dynamic-full | 3 | 7.43580 / 9.62580 / 11.70280 / 24.40200 | 9 / 0 | 0.65444 / 0.01444 |
| dynamic-inland-grid | 34 | 9.34700 / 11.64990 / 12.09260 / 26.54160 | 1 / 0 | 1.10580 / 0.01108 |
| dynamic-regional-repeat | 103 | 11.24180 / 14.12350 / 15.20740 / 22.79220 | 2 / 0 | 0.73160 / 0.01812 |
| dynamic-regional | 103 | 11.24150 / 14.17660 / 15.22960 / 23.15870 | 2 / 0 | 0.73168 / 0.01812 |
| dynamic-warp | 34 | 5.98240 / 6.43690 / 7.90280 / 12.41170 | 0 / 0 | 0.72920 / 0.01820 |
| poll-full | 3 | 7.76040 / 10.10750 / 12.14510 / 24.67080 | 9 / 0 | 0.65632 / 0.01492 |
| poll-regional | 103 | 11.51220 / 14.33030 / 15.51240 / 24.45980 | 3 / 0 | 0.73316 / 0.01844 |
| poll-return-regional | 103 | 11.40710 / 14.54830 / 15.68650 / 24.53690 | 3 / 0 | 0.73408 / 0.01848 |

Contiguous incoming-preparation and final-readiness episodes, full GPU/CPU children, complete publication records and worst/same-generation-neighbor rows are retained in JSON. Regional repeats are reported in full; prior-ticket64 frame sets are not imported. The one-milestone continuation bar requires one safe, measurable >=1.5 ms net responsibility, not a collection of smaller costs.

## Implemented candidate boundary

The candidate-* rows are the default implementation of the CPU-read terrain-request key memory preference. They are additional measurements and do not replace the fresh bank or same-binary prototype/return controls. Verify B contains the independent net-benefit judgment, exact-output comparisons and run-order challenge. There is no GPU8.33/120 FPS closure claim: the final Florida fixed population and recurring regional work remain over target.

The final dynamic profiles use the normally rebuilt candidate managed application; their managed hash differs from the bank and matches candidate-deployment.json. Their shaders and route arguments match the reference. Full, regional and warp publication identity/cadence match the original bank profiles, but dynamic TCS/TES/fragment observations differ. Timing joins do not establish exact camera/physical/raster parity.

Retained post-authority outlier: candidate-dynamic-full host8 generation1, CPU50.4425 ms, fence48.4895 ms, completed GPU7 total0.98200 ms. Its exact wait cause is unclassified. It is not silently excluded as startup-only, and it prevents a blanket assertion that every post-authority frame stays below40 ms.

## Interpretation and remaining proof

Startup/gameplay separation is accepted only from an explicit supplied boundary or the fixed100 warm window. All CPU outliers and complete aligned dynamic frames remain retained. Post30 is a sensitivity population and is not permission to discard early gameplay stalls. Incoming contiguous preparation episodes are not automatically distinct replacement jobs.

Publication log counts are exact log counts; replacements exclude the first publication. A publication frame associated through the preceding split-timer context is used only after host-generation confirmation. Sparse normal telemetry cannot establish full GPU distributions. Same-generation expensive/cheap neighbors are not assumed to have the same camera or pupil.

- candidate-dynamic-full: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- candidate-dynamic-regional: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- candidate-dynamic-warp: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- candidate-fixed-active-refinement: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-active-refinement: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- candidate-fixed-factor1: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-factor1: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- candidate-fixed-florida: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-florida: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- candidate-fixed-grazing: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-grazing: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- candidate-fixed-inland: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-inland: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- candidate-fixed-orbital: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- candidate-fixed-orbital: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- control-full: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- control-full: No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.
- control-full: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- control-full: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- control-regional: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- control-regional: No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.
- control-regional: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- control-regional: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- control-warp: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- control-warp: No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.
- control-warp: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- control-warp: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- dynamic-active-grid: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- dynamic-full: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- dynamic-inland-grid: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- dynamic-regional-repeat: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- dynamic-regional: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- dynamic-warp: No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.
- fixed-active-refinement: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-active-refinement: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- fixed-factor1: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-factor1: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- fixed-florida: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-florida: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- fixed-grazing: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-grazing: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- fixed-inland: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-inland: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- fixed-orbital: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- fixed-orbital: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- quiet-cached-regional: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- quiet-cached-regional: No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.
- quiet-cached-regional: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.
- quiet-poll-regional: No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.
- quiet-poll-regional: No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.
- quiet-poll-regional: Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.

The one justified additional responsibility is the narrowly timed CPU-read key-memory preference discussed above; its implemented-candidate performance judgment is in Verify B. No further exploratory broadening is recommended. Prior noise replacement, reuse, validator or M13.5 GPU working-data controls are not a new production baseline or a second optimization.

## Reproduction

Run `python -B docs/engineering-evidence/m13-final-exit/work-a.py` after journals are complete. It reads only fresh-bank journals and writes only work-a.json/work-a.md. An explicitly proven gameplay boundary may be supplied with `--normal-start label=FRAME`; it is recorded in the output, not silently inferred. Plain and lossless gzip journals are interchangeable.
