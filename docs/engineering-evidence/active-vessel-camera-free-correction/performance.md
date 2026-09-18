# Performance and precision disposition

Six complete renderer captures: two repeats each of baseline matched, corrected matched and corrected core routes. Each has 4800 frames at 960×540, four dynamic input populations, 180 separately reported transition frames and 1020 warmed samples per route. Fixed physical credit is equal: first warmed interval 3016666→20000000 us, then held. New vessel routes have no equivalent baseline view and are characterized separately. Full distributions, indexed tails, switches and GC are in performance.json.

| Whole-run metric (ms), repeat range | Baseline | Corrected matched |
|---|---:|---:|
| Frame median | 5.544–5.553 | 5.547–5.552 |
| Frame P95 | 5.867–5.911 | 5.884–5.917 |
| Frame P99 | 6.438–6.681 | 6.580–6.655 |
| Maximum including startup | 70.274–80.708 | 69.619–72.055 |
| Mean host callback | 0.227–0.238 | 0.236–0.239 |
| Mean GPU total | 0.820–0.832 | 0.857–0.861 |

No blocking persistent Solar regression is demonstrated. The modest whole-run GPU increase is disclosed; anchored draw averages remain 0.274–0.276 ms versus 0.275–0.276. Two repetitions on one machine are bounded characterization, not a universal guarantee.

New core callback P99: vessel follow 0.2305–0.2518 ms; celestial 0.4113–0.4178; vessel refocus 0.1946–0.2392; switch/refocus 0.1722–0.1845. Corresponding warmed display P99 ranges are 5.9561–6.0394, 6.0194–6.0713, 5.9647–6.1426 and 6.0338–6.0944 ms. New-view whole-run GPU averages 1.475–1.493 ms represent a different workload.

Do not hide tails: new switch/refocus records callback maxima 3.073 ms at frame4168 and 4.9382 ms at4155, with 1/1/0 GC in both populations. These are not scheduled switch frames; a precise cause is unproven. Other warmed new populations have 0/0/0 GC. Matched existing celestial switching also has 1/1/0 GC on both versions. Startup callbacks reach 58.54–60.30 ms, following intervals 71.44–72.45; celestial transition intervals reach 23.03–24.09 ms. These samples remain included in their transition populations.

Exact allocation tests are separate: warmed follow/orbit/zoom/display and F-refocus are 0 B in both Debug/Release; positive control 152 B. Existing discrete celestial switching is 48 B/action. Release dedicated median/P95/P99/max: follow 0.1008/0.1023/0.1037/0.1369 ms; F input/refocus 0.0651/0.0656/0.0679/0.0869; celestial-switch 0.0192/0.0193/0.0208/0.0312. F now measures real input dispatch; the previous free/refocus helper is not an equivalent measurement.

Two attempts at the final new-route repeat ended without complete populations or parent completion records and with empty stderr. Their cause is unproven; raw partial logs are retained under build/active-vessel-camera-free-correction/incomplete, with hashes in validation.json. Neither is a qualified sample. An explicitly awaited repeat completed all 4800 frames and exited 0.

Moving-publication tests report zero measured follow error and zero relative encoding error at roots through 7e12 m; canonical save bytes remain unchanged. The 600-frame surface regression keeps vessel distance ≤15.359995874 m while carrying more than 300 km root translation. This is engineering evidence; mouse feel/manual acceptance remains pending.
