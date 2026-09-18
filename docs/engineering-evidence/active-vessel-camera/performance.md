> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Performance disposition

Engineering characterization is acceptable for this bounded camera change on the tested machine at 960×540. No persistent regression is demonstrated on matched existing routes. This is not a claim of hitch-free rendering or universal performance. Manual acceptance remains pending.

Twelve sequential renderer processes cover two repeats each of baseline matched, candidate matched and candidate new routes, under ordinary live servicing and controlled fixed physical credit. Each process has 4,800 frames and four 1,200-frame dynamic routes. The first 180 frames of each route are reported separately from 1,020 warmed frames. Inputs include orbit, alternating wheel changes and periodic focus changes; this is not a fixed-pose-only comparison.

Controlled comparisons use exactly the same physical frontier: 3,016,666→20,000,000 microseconds in the first warmed route, then a held 20,000,000 microsecond endpoint. Live runs retain normal wall-clock servicing and are labeled separately. All twelve processes exited successfully with empty stderr. Full per-route median/P95/P99/max, GC, indexed tails, transition and switch event populations are in [performance.json](performance.json).

## Equivalent existing routes, fixed-credit repeats

Ranges below describe the two whole-run observations, in milliseconds.

| Metric | Baseline | Candidate |
| --- | ---: | ---: |
| Frame median | 5.554–5.558 | 5.552–5.557 |
| Frame P95 | 5.925–5.963 | 5.945–5.948 |
| Frame P99 | 6.566–6.628 | 6.531–6.573 |
| Maximum including startup | 75.546–77.008 | 70.638–76.253 |
| Mean host callback | 0.284–0.287 | 0.288–0.293 |
| Mean CPU update including fence | 5.223–5.225 | 5.230–5.232 |
| Mean GPU total | 0.626–0.658 | 0.638–0.652 |

Matched routes are existing Florida surface navigation, Earth, Moon, and repeated Earth/Moon switching. The ordinary live comparison has stable medians; candidate whole-run P99 is 6.660–6.719 versus baseline 6.487–6.508 ms. The fixed-credit repeats do not reproduce that difference. No unrelated renderer optimization was attempted.

## New routes, fixed-credit repeats

These semantics do not exist in the baseline; the figures characterize candidate behavior rather than claim an equivalent-view speedup.

| Warm route | Callback median | Callback P95 | Callback P99 | Largest callback | Display P99 | Largest display sample |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Vessel follow/orbit/zoom | 0.124–0.137 | 0.202–0.221 | 0.272–0.282 | 0.439 | 6.079–6.149 | 6.432 |
| Celestial | 0.149–0.152 | 0.264–0.274 | 0.460–0.476 | 0.729 | 6.081–6.105 | 11.478 |
| Free movement/look | 0.083–0.086 | 0.120–0.140 | 0.153–0.163 | 0.223 | 6.754–6.811 | 15.449 |
| Switch/refocus display | 0.098–0.110 | 0.138–0.157 | 0.192–0.216 | 0.328 | 6.060–6.144 | 11.603 |

All warmed new-route GC counts are 0/0/0. Existing matched celestial-switch populations are 1/1/0 on both baseline and candidate. The new route whole-run GPU average is 1.146–1.162 ms; different close-Earth views account for a different workload, so this is not compared as an equivalent-view code regression. GPU values are whole-run averages; no per-route GPU percentiles are claimed.

## Action cost and allocation

Dedicated Release populations measure 2,048 actions after 512 warmups under normal runtime timing, independently of exact-zero windows. Follow/orbit/zoom/display median/P95/P99/max is 0.0443/0.0445/0.0469/0.0649 ms. Free/refocus is 0.0145/0.0208/0.0211/0.0266 ms. Celestial/vessel switching is 0.0193/0.0194/0.0209/0.0381 ms. Free display is 0.0156/0.0156/0.0169/0.0264 ms. All dedicated populations have zero GC collections.

Applicable warmed follow, free and refocus windows are exactly 0 B in Debug and Release; positive control is 152 B. Existing discrete celestial Focus allocates 48 B per action (`PlanetaryRepresentationHandoff`), reported separately. No all-focus-actions-zero claim is made. Cold registration binds one value and scans ten existing bodies once; standalone cold binding latency/allocation is not separately instrumented. Whole assembly cold preparation, including assets and history, is approximately 307–320 ms in the fixed integrated captures.

## Retained tails and limits

Startup remains visible: first callback approximately 58.6–65.5 ms and its following interval 70.4–78.1 ms in controlled captures. Existing native/GPU startup is outside cold assembly preparation.

The first return to vessel produces a repeatable 11.082 ms following interval; later returns are roughly 7.0–7.62 ms. Associated action callbacks remain below 0.329 ms. One new-route transition also records 17.833 ms callback / 18.335 ms following interval at frames 3614/3615; its cause is unproven. These samples are retained, not discarded by warmup reporting. No new mitigation or speculative causal claim is introduced.

Independent measurement review by `/root/ksa_commands` found no blocking camera regression, with these limits retained. Two repetitions and one resolution establish a bounded characterization, not a general latency guarantee.
