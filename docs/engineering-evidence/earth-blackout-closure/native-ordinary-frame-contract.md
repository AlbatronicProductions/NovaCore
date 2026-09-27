# Native ordinary-frame gate classification — 2026-09-25

## Provenance and judgment

**Classification 2: ordinary native-resolution workload inside the accepted sustained production floor, with preferred-target overruns explicitly retained. No synchronization/progress pathology demonstrated.** This classifies the first native run's triggering window; it does not establish long-duration performance or the historical blackout cause.

The 6 ms value was introduced in [capture-cost-first-alarm.md](capture-cost-first-alarm.md) as a **diagnostic cost threshold**, applied to separate capture and noncapture fence cohorts. It followed 960×540 capture-overhead observations: held-ground capture median 6.0994 ms versus closer 3.5972 ms, while the isolated 6.0696 ms ordinary terrain-transition wait was accepted. That rule was never a whole-frame FPS contract or a GPU timeout. The native fallback inherited it unchanged, including its ordinary-fence arm.

The current accepted [production envelope](../../NOVACORE_CURRENT_STATE.md) and [construction campaign plan, section 27](../modular-craft-first-playable/accepted-plan.md) retain these distinct targets:

- 8.33 ms terrain-foundation target;
- preferred 6.67–6.94 ms headroom;
- 11.11 ms preferred fully featured game;
- **16.67 ms sustained Ultra/max floor**.

The banked reference already retains fixed-Florida GPU median 8.95040 ms and regional GPU peak 12.84380 ms. It expressly does not claim universal 8.33 ms compliance. A 6 ms fence-only gate cannot represent that accepted native workload. It implies more than 166 FPS before accounting for the remaining CPU work, instead of testing the accepted sustained floor. Existing preferred-target misses are not spare budget.

## Exact triggering frames

Evidence: `build/earth-blackout-closure/native-adaptive-evidence/ordinary-gate-20260925/trigger-analysis.json`. The reproducible parser joins raw QPC markers and GPU frame identities from the checksum-verified original journal.

CPU cycle N waits for GPU frame N−1. “CPU wall” spans that wait through the next submission/present return; “cadence” spans successive present returns, including intervening host scheduling. The FPS reciprocal is observed host presentation cadence, not a compositor/display scanout measurement. CPU wall includes waits and **must not be added to GPU time**.

| GPU frame waited for | Fence ms | Following CPU cycle wall ms | Present cadence ms / FPS | GPU total ms | Wall outside fence ms |
|---|---:|---:|---:|---:|---:|
| 2919 | 2.5789 | 7.9652 | 7.9683 / 125.50 | 2.61140 | 5.3863 |
| 2920 | 9.0224 | 11.2011 | 11.2046 / 89.25 | 9.06929 | 2.1787 |
| 2921 | 8.6424 | 10.4980 | 10.5025 / 95.22 | 8.69463 | 1.8556 |
| 2922 | 8.3835 | 10.4377 | 10.4417 / 95.77 | 8.44692 | 2.0542 |

All four satisfy the 60 FPS floor. One slightly misses the preferred 90 FPS target; three exceed the terrain/headroom targets. No all-target PASS is claimed.

| Retained population | Metric | Samples | Median ms | P95 ms | P99 ms | Maximum ms |
|---|---|---:|---:|---:|---:|---:|
| Trigger | Fence | 4 | 8.51295 | 9.0224 | 9.0224 | 9.0224 |
| Trigger | CPU wall | 4 | 10.46785 | 11.2011 | 11.2011 | 11.2011 |
| Trigger | Present cadence | 4 | 10.47210 | 11.2046 | 11.2046 | 11.2046 |
| Trigger | GPU | 4 | 8.570775 | 9.06929 | 9.06929 | 9.06929 |
| Nearby LOD 11 | Present cadence | 51 | 5.3276 | 10.4417 | 11.2046 | 11.2046 |
| Retained complete frames | CPU wall | 2,138 | 5.2139 | 8.0926 | 10.4043 | 16.7304 |
| Retained complete frames | Present cadence | 2,138 | 5.21935 | 8.0962 | 10.4080 | 16.7340 |
| Retained GPU frames | GPU | 2,138 | 2.819495 | 3.42686 | 4.36138 | 9.06929 |

Percentiles use nearest rank (median uses the middle-pair mean); four-sample P95/P99 simply equal the maximum and are not stable tail estimates. One earlier retained interval, CPU cycle 2851 at LOD 10, reached 16.734 ms cadence: an isolated overrun, with GPU 3.13145 ms and 13.5707 ms wall outside its fence. It is retained, not hidden or called a GPU stall. Nine retained intervals exceeded the preferred 11.11 ms target; only that one exceeded the sustained floor, without recurrence.

## Progress, workload and residency

Completion IDs 785–2923 are continuous in the retained window. Its largest completion gap is 16.8282 ms; all 2,924 submitted frames completed at clean shutdown. No lost/torn/corrupt producer record or returned GPU error occurred.

The longer waits track completed GPU work closely: GPU 9.06929/8.69463/8.44692 ms versus fence 9.0224/8.6424/8.3835 ms. Anchored-draw intervals are 7.41682/7.32256/7.28372 ms, versus regional compute approximately 0.467 ms. These GPU subranges overlap and are not additive. Managed callback wall time in the three cycles is 1.2841/1.0494/1.2223 ms, recording approximately 0.35 ms, and present-return work approximately 0.19–0.25 ms. This is consistent with returned rendering work, not an unreturned synchronization call.

A zoom step changed camera altitude from approximately 17,443.6 m to 13,954.9 m. Current physical LOD remained **11**, generation/pupil **12**, with **898,736 triangles / 449,370 vertices**; selected maximum level was 12. Prepared/cull/raster identities agreed, incoming generation was zero, and neither current nor incoming fence ownership was pending. Draw/dispatch counts remained 13/3.

There were no allocation/resource events in the four triggering frames. The 12 resident topologies used 93,111,424 bytes; working storage was 81,536,120 bytes; regional payload was 119,737,728 bytes; scratch capacity and regional preparation cursors were zero. The latest budget sample showed heap usages 452,083,712 and 697,081,856 bytes against budgets 15,897,839,616 and 16,325,505,024 bytes. All 226 tracked allocations were subsequently freed.

The three slower frames repeated over only about 28 ms after the camera step. Topology was held, but this is not a long held-camera trial: the stop censored subsequent behavior. Neither sustained growth nor recovery to the preceding level is established. The accepted sustained floor was nevertheless not breached by these frames.

## Bounded correction and offline proof

Only the explicitly opted-in native matching route replaces the obsolete ordinary-fence stop. `ProductionFrameWatch` observes complete successive present-return intervals. Its budget is derived as **1000 / 60 ms**, with an integer QPC cross-product at the exact 1/60-second boundary. The existing three-of-four recurrence rule is retained as the bounded sustained-cost detector. This is a contract-derived diagnostic stop, not a renderer limiter or a claim that the production contract defines a particular four-frame statistical window.

Current/preceding frames carrying admitted capture or topology-witness work remain owned by the unchanged heavy-capture cost/fallback mechanism; their intervals are explicitly counted as excluded from the ordinary cohort. Swapchain discontinuities are also counted and do not fabricate a full frame. Ordinary full-frame gating remains active after diagnostic work/fallback. Existing 500 ms completed-operation and one-second progress/heartbeat/stall bounds, native failures, memory/corruption guards, capture fence >6 ms and completion >1 ms rules remain unchanged. No quality, density, shader, native renderer, managed application or physical data changed.

Debug and Release: observer build PASS, **27 production-frame checks**, **27 adaptive checks**, **76 first-alarm checks**, and four process recovery mocks each PASS. Permanent cases cover below/exactly/above 1/60 second, recurrence and isolated tails, diagnostic overlap, ordinary faults after diagnostic work, swapchain gaps, nonadvancing clock, and preservation of the earlier limited diagnostic policy. Replay of the original raw journal yields **2,095 ordinary intervals, 44 diagnostic exclusions, one leading discontinuity, no production-frame alarm** in both builds. All raw records pass independent checksum/continuity validation during replay.

Project Control's conditional continuation is satisfied. The [final native closure](native-route-closure.md) records the successful continuation, including the subsequent harness cleanup check (33 production/closure checks per configuration after six additions). The first native attempt and its original gate remain preserved. No banking, milestone promotion or manual Player PASS follows from this correction.
