# Measured scalability and headroom

Scope: this machine, current Release candidate, finite representative fixtures. No maximum-document, unlimited-count, first-install startup or long-duration soak claim. Raw compact distributions are in `debug-cpu.json`, `release-cpu.json`, `native-results.json` and `native-gpu-samples.json`.

## Contract

The accepted construction plan, section 27, requires distributions, allocations, GC, retained storage and repeated tails. Warm input/observation must remain exactly allocation-free. Existing exact BigInteger service evolution is not a zero-allocation contract. Cold editor/compiler work has no established numeric acceptance bar and remains report-only.

Standing whole-frame targets remain 8.33 ms terrain foundation, preferred 6.67–6.94 ms headroom, 11.11 ms preferred full game and 16.67 ms sustained Ultra floor. No 6 ms fence threshold is substituted for them. See `../modular-craft-first-playable/accepted-plan.md` and `../earth-blackout-closure/native-ordinary-frame-contract.md`. Preferred-target misses remain visible; they are not spare budget.

## Release CPU

Times in milliseconds. Cold compilation uses 16 samples after two warmups; P99 therefore equals the maximum. Active service uses 256 samples with normal GC. Retained heap is measured separately using forced-GC deltas with three graphs kept alive, repeated three times. These are managed graph deltas, not process working set or peak memory.

| Jets | Cold median / P95 | Service median / P95 / P99 / max | Service allocated bytes, median | Retained bytes per graph, median |
|---:|---:|---:|---:|---:|
| 8 | 8.873 / 13.007 | .0255 / .0351 / .0725 / .5634 | 20,920 | 60,496 |
| 32 | 20.535 / 26.881 | .0662 / .0697 / .0892 / .6638 | 55,904 | 144,408 |
| 64 | 49.859 / 52.136 | .1826 / .2189 / .2340 / 9.2633 | 115,056 | 261,712 |
| 96 | 24.591 / 39.901 | .2497 / .3438 / .6329 / 9.1442 | 166,720 | 378,960 |
| 288 | 68.913 / 107.329 | .4580 / .5575 / .6781 / .9501 | 506,696 | 1,087,216 |

Eight jets measure exact fuel plus wrench because that stock arrangement lacks one attitude axis and correctly refuses physical admission. Other service rows measure physical service evolution. The 96-jet fixture uses the documented smaller finite stores; these rows are not identical mass/topology comparisons. Tiering, runtime scheduling and GC are not disabled, and the nonmonotonic cold medians must not be interpreted as algorithmic speedup. The isolated 64/96 service maxima remain reported; their individual cause is not established. The CPU microbenchmark did not retain temporal ordering, so no consecutive-tail claim is made for it.

Warmed immutable observation plus wrench accumulation is exactly **0 allocated bytes** at 32/64/96/288, independently asserted. Logical selection/share/row payload sizes are retained separately from measured heap. The allocator retains O(J) pair-search scratch and has a proven O(J² log J) worst cold bound; this is not a claim that every admitted document is cheap.

## Native current-package flight

AMD Radeon RX 6800 XT, ordinary Release Vulkan path. RCS routes use 3440×1440 borderless settings and a measured **3440×1322 game viewport** after normal product chrome. Short/long use 1280×720 settings and a 1280×602 viewport. Quality, terrain data and renderer algorithms are unchanged. Release validation-layer state is the normal disabled state. No KSA runtime dependency.

Each row below summarizes three consecutive 256-frame active-RCS windows. Ranges are the minimum/maximum of the three window statistics, not pooled quantiles.

| Jets | Frame median | Frame P95 | Frame P99 | Worst frame | CPU callback median | CPU callback P95 | Longest consecutive frames at/above window P95 |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 64 | 8.302–8.411 | 8.727–8.872 | 9.056–9.756 | 10.418 | .280–.294 | .561–.580 | 1 |
| 96 | 8.099–8.193 | 8.573–8.705 | 8.839–9.423 | 10.030 | .329–.485 | .645–.824 | 1 |

Both routes realize high-index jets in every one of their 768 measured coast frames. Median callback allocations are 98,848 bytes (64) and 140,272 bytes (96), including actual physical intervals and exact resource evolution. They are not attributed to the zero-allocation observer. GC counts and allocation tails are retained per window. Callback P95 runs reach three samples in the first 96-jet window; frame P95 runs remain one.

Periodic asynchronous GPU samples during active coast: 64 jets total median/P95 7.717/7.930 ms; 96 jets 7.445/7.613 ms. Powered GPU totals peak at 8.657/8.727 ms. These queries are labelled by host phase, not aligned to individual callback samples; CPU time must not be added to GPU time or subtracted from frame time. Submitted/completed counters increase monotonically in all four runs.

Powered windows retain median frames up to 9.216 ms and a worst frame of 11.273 ms. Preferred terrain/headroom targets are therefore not universally met; all measured warm phase windows remain within the sustained production floor. Editor observer windows are 0 B with no GC. Complete per-phase distributions are retained.

Cold-inclusive first-4096-flight samples retain frame maxima of 85.925 ms (96), 85.198 ms (64), 80.293 ms (short), and 79.910 ms (long); physical-service maxima are 75.567–78.452 ms. Their P99 frame times are 8.994–9.973 ms. These startup/transition-inclusive spikes are explicitly **not** a universal frame-time pass or erased as spare budget. The separate held active-RCS windows show no repeating sustained breach. This bounded scalability result is acceptable under the existing measured-cold / sustained-frame contract; it does not qualify all future gameplay workloads.
