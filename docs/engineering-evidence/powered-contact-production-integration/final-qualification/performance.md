# Final performance characterization

Historical .050/.100/.250/.500ms targets and the .0683ms failed median are preserved in ../final-median-closure/REPORT.md. The new explicit Project Control decision treats those as historical first-ticket diagnostic targets. No optimization or solver change follows.

| Population | Process | Median ms | P95 ms | P99 ms | Max ms |
|---|---:|---:|---:|---:|---:|
| UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL | 1 | 0.0690 | 0.0714 | 0.0735 | 0.1009 |
| GENUINE EXHAUSTED-DRY RETAINED CONTACT | 1 | 0.0670 | 0.0701 | 0.1000 | 0.2258 |
| PRODUCTION POWERED-CONTACT INTERVAL | 1 | 0.0355 | 0.1205 | 0.1572 | 0.2199 |
| GENUINE EXHAUSTION -> DRY CONTINUATION | 1 | 0.0199 | 0.0257 | 0.0361 | 0.0845 |
| EVENT-TO-ORDINARY INPUT PREPARATION | 1 | 0.0026 | 0.0027 | 0.0032 | 0.0520 |
| UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL | 2 | 0.0690 | 0.0716 | 0.0730 | 0.0991 |
| GENUINE EXHAUSTED-DRY RETAINED CONTACT | 2 | 0.0678 | 0.0709 | 0.0733 | 0.1151 |
| PRODUCTION POWERED-CONTACT INTERVAL | 2 | 0.0363 | 0.0444 | 0.0536 | 0.0645 |
| GENUINE EXHAUSTION -> DRY CONTINUATION | 2 | 0.0197 | 0.0279 | 0.0310 | 0.0658 |
| EVENT-TO-ORDINARY INPUT PREPARATION | 2 | 0.0025 | 0.0025 | 0.0025 | 0.0073 |
| UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL | 3 | 0.0690 | 0.0748 | 0.0877 | 0.0994 |
| GENUINE EXHAUSTED-DRY RETAINED CONTACT | 3 | 0.0671 | 0.0745 | 0.1166 | 0.1574 |
| PRODUCTION POWERED-CONTACT INTERVAL | 3 | 0.0376 | 0.0469 | 0.0618 | 0.0867 |
| GENUINE EXHAUSTION -> DRY CONTINUATION | 3 | 0.0202 | 0.0287 | 0.0319 | 0.0562 |
| EVENT-TO-ORDINARY INPUT PREPARATION | 3 | 0.0026 | 0.0027 | 0.0027 | 0.0158 |

All populations:128 warm/1024 measured samples, three fresh Release processes; all timed-operation collection counts0. A/B each one retained world. C/E independent prepared episodes, cold preparation/disposal outside each timed sample. E is TWO complete operations, genuine interior exhaustion then dry. D is the accepted prepared mapper, solver calls0. Do not subtract C/E from A/B as added powered cost: lifecycle/cache/frontier populations differ. No FPS claim.

C process1 has elevated P95/P99/max .1205/.1572/.2199ms versus processes2/3. Retained dry process1 has an adjacent tail cluster at447-454, max.2258ms, no observed collection. The cluster is not at the same frontiers in processes2/3. Tail cause remains unassigned; no tracing or optimization performed. Control index0 appears among top samples in all processes, approximately.099-.101ms. Full top-eight indices/values are in performance.json.

Historical preparation P95~.26ms/max.30-.38ms and complete-operation>1ms magnitudes: NOT REPRODUCED in this bounded final sample. This does not prove the old causes disappeared, were identical, or cannot recur.

Ordinary unpowered still-fuelled cost is .0690ms median here; genuine dry .0670-.0678ms; powered first intervals .0355-.0376ms; exact event mapper .0025-.0026ms; genuine exhaustion-plus-dry pair .0197-.0202ms. All absolute values are population-specific, not a net cost estimate.

Owner service remains capped at4 exact intervals/call, debt retained, solver8 iterations/1substep per interval, no event-sized timestep. No waiting lock/worker synchronization added; owner-thread phase admission remains. Rendering happens once after service, no warp multiplier. Current whole-engine objectives: preferred6.67-6.94ms, full-game11.11ms, Ultra16.67ms. These microbenchmarks alone do not establish whole-engine headroom; live frame evidence and known unrelated renderer residuals must be considered separately.
