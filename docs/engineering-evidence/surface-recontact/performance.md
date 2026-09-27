# Contact performance — measured limits

CPU-only Release measurements time the real 64 Hz service call with normal GC.
Observations/assertions are outside the timed interval. Each of six short/long
gentle/skid/roll fixtures includes 256 transient intervals and three consecutive
256-interval rest windows. These are service costs, not total frame or isolated
BEPU costs. Preparation is reported separately. Timing gates are report-only.

| Fixture | First contact ms | Largest rest-window P95 / P99 ms | Rest allocation B/interval | Retained managed delta B |
|---|---:|---:|---:|---:|
| Short gentle | 13.0180 | 2.2235 / 5.1290 | 59,248 | 1,784 |
| Short skid | 1.8366 | 1.9397 / 5.0680 | 59,248 | 1,376 |
| Short roll | 2.1926 | 2.0132 / 2.2133 | 59,248 | 224 |
| Long gentle | 1.8693 | 1.9425 / 2.0872 | 59,272 | 8,792 |
| Long skid | 1.8435 | 1.9895 / 2.0759 | 59,272 | 1,352 |
| Long roll | 1.7843 | 1.9413 / 2.0127 | 59,272 | 200 |

Rest medians span 1.8726–1.9293 ms. Short gentle transient P95/P99/max:
14.4892/16.3034/21.4328 ms. Preparation: 43.6–260.7 ms. Short/long grounded
RCS median 2.1104/2.1362 ms, maximum 4.3299/2.3197 ms (four samples each).
Grounded ignition median 1.9342/1.9028 ms, maximum 3.2405/2.0415 ms (4/8 samples).
Those small command samples are not a tail-distribution qualification.

Last-slice observations reach 32 contacts / 8 constraints upright and 8 / 4
rolling. Native pool maxima are 720,896–999,424 bytes and return to zero on world
disposal. Managed deltas include process/test observations, not exclusive solver
retention. Approximately 59 KB allocated per rest tick remains an explicit cost.

Moving-terrain seam diagnostics have service medians 4.1–9.5 ms. New tile/mesh
preparation repeats 2–6 times per route with 25–223 ms spikes. These instrumented
values are not a clean production frame benchmark, but the preparation tail is
real remaining evidence and is not described as a one-time warmup. No blanket
performance PASS is asserted against the 60 FPS floor / preferred 90 FPS target.

## Native integrated observations

| Loaded route | Frame samples | Frame median / P95 / P99 / max ms |
|---|---:|---:|
| Short descent | 1,296 | 6.3659 / 9.3979 / 66.4757 / 78.2542 |
| Short grounded reload | 1,024 | 6.3714 / 9.2507 / 9.6969 / 10.5240 |
| Long descent | 2,793 | 6.6170 / 9.3507 / 9.8328 / 11.2502 |
| Terrain | 2,069 | 13.7375 / 14.4062 / 14.8421 / 93.8310 |

Three held-terrain 256-frame windows had median 13.6632–13.7031 ms,
P95 14.3447–14.3866 ms, P99 15.3219–15.8494 ms and maxima 15.7505–15.9474 ms.
Flight callback medians were approximately 2.8 ms; GPU time approximately
10.3–10.5 ms. Early short-contact transitions include much larger CPU tails.
Whole-session GPU average 2.805 ms is dominated by overview/menu and must not
stand in for terrain cost. These observations do not repair the recorder's
incomplete native acceptance evidence.

Recorder persistence process: 669,491.3 ms wall / 18,171.9 ms CPU (~2.7% of one
core), 841,970,915 cumulative bytes written, 48 rotations, 199,176 flushes;
last 4,096 commits P50/P95/P99/max 1.5853/1.8777/5.0700/12.4429 ms.
Allocated 2,205,683,464 bytes cumulatively, retained managed 898,696 bytes, peak
working set 60,420,096 bytes. These are observer/persistence measurements, not
isolated renderer producer overhead. Overflow makes recorder qualification FAIL.

Raw source: `build/surface-recontact/qualification/observer-surface-performance-Release.log`
and `build/surface-recontact/native/summary.json`. Concise measurements are
retained in `qualification.json`.
