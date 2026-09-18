# Performance closure

Eight complete native-renderer processes at 960×540: two entry/candidate pairs of 4800 display frames (four routes), plus two matched celestial-only pairs of 1200 frames. Every route retains its first 180 startup/transition frames separately and measures the next 1020. Input alternates bounded drag and wheel actions; fixed diagnostic physical credit is 60 Hz and the support episode holds after its accepted 20-second endpoint. This instrumentation does not replace Project Control's manual acceptance.

Corrected warmed display intervals, milliseconds (range across two repeats):

| Route | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|
| Supported vessel 1x | 5.5532–5.5565 | 5.7219–5.7316 | 5.9341–5.9400 | 6.1750–6.2499 |
| Supported vessel 120x | 5.5547–5.5564 | 5.7212–5.7361 | 5.8925–5.9656 | 6.1637–6.3205 |
| Supported vessel 14400x | 5.5531–5.5550 | 5.7299–5.7349 | 5.9190–5.9317 | 6.3846–7.0652 |
| Matched celestial 1x | 5.5567–5.5568 | 5.9624–6.0056 | 6.7325–6.9768 | 8.1058–8.4350 |

Copied focus preparation plus camera refresh costs, milliseconds:

| Supported rate | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|
| 1x | 0.0246–0.0250 | 0.0308–0.0344 | 0.0388–0.0566 | 0.0560–0.0699 |
| 120x | 0.0245–0.0249 | 0.0301–0.0316 | 0.0348–0.0504 | 0.0563–0.0689 |
| 14400x | 0.0247–0.0250 | 0.0301–0.0331 | 0.0457–0.0485 | 0.0567–0.1145 |

This focused timer excludes input dispatch; the full callback includes it. Corrected callback medians span 0.0848–0.0879 ms for the three vessel routes. Permanent managed measurements separately include input/orbit/zoom, F, submission, four rates through 86400x and normal celestial work (`allocation-and-invariants.json`). Warp does not multiply camera work: permanent production-order tests observe one reconstruction per display callback, including F and pause.

## Equivalent Solar comparison

The four full-route captures are behavior characterization, **not equivalent old/new image workloads**. The corrected vessel frame changes the view, and celestial focus retains incoming angles. A separate matched run resets the camera before Earth focus on both versions, with equal epoch, pose, physical credit and input schedule.

Matched entry celestial display median/P95/P99/max: 5.5546–5.5570 / 5.8716–5.9849 / 6.8935–6.9139 / 8.5221–8.8427 ms. Corrected values are in the table. P95 is modestly higher; P99 ranges overlap. Whole matched-run GPU averages are 0.660–0.835 ms corrected versus 0.740–0.864 entry; anchored draw is 0.176–0.181 versus 0.177–0.180 ms. This bounded comparison identifies no material blocking Solar regression; it is not a universal guarantee or an optimization claim.

## Allocation, GC and tails

Exact-zero Debug/Release gates pass for warmed camera/focus/input/submission and F-refocus. Positive control allocates 152 B. Actual native focus-copy-refresh counters also total **0 B** in every measured route. Existing dynamic Solar publication allocates **2320 B/display**, unchanged in the warmed supported populations; full callbacks are therefore not allocation-free. Existing discrete celestial switching remains 48 B/action. Do not relabel camera-only zero as whole-display zero.

Corrected 1x and 120x warmed vessel populations have 0/0/0 collections. Both 14400x populations have 1/1/0 collections and callback maxima of **3.3141 ms at frame 4429** and **4.0940 ms at frame 4409**. Precise attribution of those tails is unproven; they are retained, not removed from maxima. Focus refresh itself peaks at 0.1145 ms. The report does not claim the repeating whole-callback tail is fixed.

Startup residuals remain: corrected full-route native maxima are 68.647–69.538 ms, with observer following-display intervals 70.212–71.963 ms. Matched celestial startup intervals reach **74.3489–76.3651 ms**. Transition tails repeat around frame 105 at 18.55–22.55 ms. Full distributions, indexed top tails, transition first frames, allocation and GC remain in `performance.json`; native and observer timings have different boundaries and must not be conflated.

The first capture sequence lost its parent tool process during entry repeat 2. Its partial log is excluded and hashed; cause is unproven. The continuously awaited replacement pair completed and exited zero. The first four complete captures predate the optional matched-only diagnostic mode; the timers and full-route schedule are unchanged. Diagnostic observers were never deployed.

Initial attempted whole-display NoGC measurement failed when inherited Solar publication exceeded the existing 1 MiB reservation. That failure is retained. Final isolation uses the already-contracted camera scope while dynamic publication allocation is measured normally. No allocator policy, publication implementation, physics, clock or performance threshold was changed.
