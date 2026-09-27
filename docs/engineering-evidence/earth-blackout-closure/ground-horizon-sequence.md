# Ground-visible → horizon sequence — STOP / REVISE / UNBANKED

## Decision

**The ground stage was stopped on repeating materially higher capture-submission fence costs. Horizon was not launched. No sequence PASS is claimed.** The stop follows Project Control's explicit synchronization condition even though the existing automatic progress/device/capture gates remained clean.

The first supervisor alarm retained waits of **6.2444, 6.3938, 6.1748 and 6.4145 ms**, at held-ground frames **6274, 6634, 6814 and 6994**. Unlike the accepted isolated **6.0696 ms closer-stage terrain-transition witness**, these repeated during a stable LOD-14 topology and coincide with frozen-capture transfers. The final independently inspected window confirms the pattern: **6.0994 ms capture-fence median**, versus closer **3.5972 ms**, an increase of **69.56%**. Seven of 13 retained capture waits exceeded 6 ms; no retained non-capture wait did.

GPU transfer/barrier median increased **0.24108 → 2.60084 ms** (10.79×), while frozen file size increased **6,497,556 → 71,716,116 B** (approximately 11×). This identifies substantial recurring diagnostic GPU work at this workload. It does **not** establish a deadlock, ownership failure, driver defect or original blackout cause. No synchronization semantics, capture cadence, rendering quality or timeout was changed to suppress the measured cost.

**Supervision limitation:** approximately **24.20 seconds** elapsed between the alarming supervision snapshot and the stop-record timestamp. This was a supervisor-driven UI close, not an automatic lower-cost stop. Immediate stopping at this millisecond-scale boundary was **not** demonstrated. The first alarm's raw frames aged out of the bounded journal before shutdown; the derived first-alarm measurements remain, and later repetitions retain complete raw frame/submission/capture joins. This limitation is part of the result, not a successful fail-closed qualification at the new boundary.

**STOP for Project Control review.** Ground exposure is not promoted to PASS; horizon remains unattempted under this authorization. September 22 dual-monitor blackout and historical 411,877-triangle discrepancy remain **UNRESOLVED**. Manual Player acceptance remains **ON HOLD**. No commit, tag, push, banking or milestone promotion.

## Scope, candidate and display

Evidence root: `E:\NovaCore\build\earth-blackout-closure\ground-horizon-live-qualification`; actual run: `live-01-ground`. There is **one application launch**, no retry and no horizon launch. Ground inspection produced `ground-boundary.json` with `passed=false` and `horizonPermitted=false`; no horizon launch gate was created. Local/global exposure gates are closed.

The exact accepted `authority-scalars` Debug application/native DLL and Release observer were reused. Preflight and postflight verified **1,005 unique sealed identities**, covering **63 source/report files, 780 build files, 159 earlier qualification artifacts and three terrain authorities**. No production implementation, shader, instrumentation, tessellation, ownership, synchronization or deadline change occurred. Accepted horizon arithmetic, pupil preparation and AuthorityScalars corrections remain intact.

The sequence used the previously defined ground/horizon route bounds of **90 seconds each**, including approach; this is distinct from startup and stall deadlines. The previous closer-stage 20-second exposure cap was not represented as a startup timeout. The ground attempt ended before its 90-second cap; horizon's bound was never exercised. Both stages would use the same accepted display and rendering settings.

The temporary preset was prepared before launch and visually confirmed as **960×540 windowed**, borderless unchecked, with the resolution list closed. The supervisor clicked Start once. The existing minimum window dimensions yielded a **960×582 render extent**. No manual camera input was sent. Original player settings were restored byte-for-byte after exit; other defaults were not changed.

Application PID **27024** started at **2026-09-22 23:52:13.5306398 UTC** (19:52:13.5306398 EDT). The existing ground route focused Earth and applied one ordinary wheel detent per 0.4 seconds until its ≤1,000 m waypoint latched. After that, it issued no orbit/look sequence. Ground terrain was visually observed. Recorded target altitude ultimately reached **900.956665 m** minimum; the route accumulated **7,759** ground-target observations. Changes in Earth state/prepared pupil during the hold are recorded and are not treated as identical historical geometry.

## Termination and observer evidence

The supervisor sent **one Alt+F4** to the exact diagnostic window after detecting the repeated waits. Native shutdown completed normally, then the observer finalized its evidence. Its summary says **`child exited`**, exit **0**, no forced termination; this was not the normal stage-duration stop. `firstStopDecisionQpc=0` and producer stop reason 0 correctly distinguish this application close from an observer-requested stop. `supervisor-stop.json` records the external reason.

| Lifecycle interval | Observed duration |
|---|---:|
| Launch → UI ready | 0.419 s |
| Launch → Start click | 24.192 s |
| Loading handoff | 5.408 s |
| Native initialization | 0.608 s |
| Native ready → first submission | 21.084 ms |
| First submission → observed completion | 231.489 ms |
| Steady rendering → application shutdown signal | 64.144 s |
| Shutdown signal → native cleanup | 248.440 ms |
| Shutdown signal → application exit | 285.071 ms |

The original UI 60 s, loading/init 30 s, first-submit 5 s, completion/steady-progress 1 s, post-click 60 s, overall startup 120 s and shutdown 2 s deadlines remained unchanged. The observer's completed-operation stop threshold is 500 ms; it does not automatically classify repeated 6 ms waits as abnormal. That distinction explains why automatic gates could remain clean while the supervisor's cost criterion stopped the sequence. No threshold was raised or weakened.

There were **11,534 submitted / 11,534 GPU-completed frames**. All **669,543** emitted records were received and durably recorded, with zero producer overruns, contention drops, torn records, corruption or native errors. The final rolling journal has **131,072** records, serials **538472–669543**, covering **12.7168 s**, with **2,244 complete authority assemblies**, zero partial assemblies and no unresolved phase. All 2,244 retained general GPU timing payloads match their completed-frame identity.

The first eligible marked capture was frame **167** in this run. Its online summary completed all **149 marker records, 50 outer calls and 23 scalar calls** without recurrence of the closed AuthorityScalars defect. It was not necessary for the eligible frame number to be exactly 168. The first raw marker trail aged out; the online successful classification remains.

## Exact frozen evidence at stop

All **64** published capture identities passed the live background validator, with no skipped identity, capture ownership failure or validation error. The rolling files retain captures **63 and 64**; both pass the official postflight inspector.

| Authority / population | Capture 63 | Capture 64 |
|---|---:|---:|
| Rendered / completed frame | 11314 / 11314 | 11494 / 11494 |
| Submitted / completed submission sequence | 11377 / 11377 | 11557 / 11557 |
| Prepared generation / incoming generation | 15 / 0 | 15 / 0 |
| Prepared / cull / raster pupil | 76 / 76 / 76 | 77 / 77 / 77 |
| Swapchain generation | 2 | 2 |
| Vertices / source triangles | 597,594 / 1,195,184 | 597,594 / 1,195,184 |
| Visible triangles / TCS patches | 15,747 / 15,747 | 15,679 / 15,679 |
| Horizon rejects | 236,352 | 236,349 |
| Screen-cone rejects | 943,085 | 943,156 |
| Compacted indices / TES invocations | 47,241 / 47,241 | 47,037 / 47,037 |
| Invalid triangles / compaction overflow | 0 / 0 | 0 / 0 |
| Draws / dispatches / dispatch groups | 13 / 3 / 37,351 | 13 / 3 / 37,351 |
| Frozen file bytes | 71,716,116 | 71,716,116 |

Both have present success, topology family 1/hash **`0x8999DD8A034A9AB1`**, physical generation 4, terrain version 5 and extent 960×582. All captured physical position/height/normal/validity values are finite. Source-index hashes match, while physical-geometry and authority hashes differ with their **distinct prepared pupils**. Each capture is validated against its own exact inputs, not forced to match the other capture's population.

Validation checks committed-file/section hashes and ranges, every source/compacted index, binary visibility, exact visible/compacted triangle multiset, indirect arguments, rejection accounting, TCS population and finite tessellation domain. Each capture's frozen authority joins the same frame's journaled authority, submission/completion sequence and valid GPU resource incarnations. Detailed fields, handles, hashes and timing joins are in `qualification-audit.json`, `ground-boundary.json` and `live-01-ground/frozen-analysis.json`.

This is exact internal live membership consistency. It is not an independent proof of all shader decisions and does not synthesize missing historical state to resolve the 411,877-triangle discrepancy.

## Cost and repeating waits

All values are **milliseconds**, nearest-rank percentiles. The final window retains **13 capture timing samples**, identities **52–64**, during held ground exposure. P95/P99 both select the maximum and remain provisional. Earlier timing records, including the first alarm, are outside this final raw window.

| Measurement | Samples | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|---:|
| CPU capture-path recording wall time | 13 | 0.2279 | 0.2724 | 0.2724 | 0.2724 |
| CPU full command recording, capture frames | 13 | 0.4205 | 0.5241 | 0.5241 | 0.5241 |
| CPU completion inspection wall time | 13 | 0.0149 | 0.0236 | 0.0236 | 0.0236 |
| GPU transfer and surrounding barriers | 13 | 2.60084 | 2.60620 | 2.60620 | 2.60620 |
| GPU capture-frame timestamp interval | 13 | 5.03952 | 5.57920 | 5.57920 | 5.57920 |
| GPU frames without capture | 2,231 | 2.40828 | 2.70672 | 3.46276 | 3.65320 |
| Submission-fence wait, captures | 13 | 6.0994 | 6.3794 | 6.3794 | 6.3794 |
| Submission-fence wait, other frames | 2,230 | 3.4501 | 4.0907 | 4.3753 | 5.1024 |
| Background hash/write wall time | 14 | 75.333 | 83.154 | 83.154 | 83.154 |
| Background validation wall time | 64 | 70.9161 | 92.9640 | 95.2425 | 95.2425 |

CPU capture-path time includes authority construction/scheduling; completion inspection follows the existing fence. Full GPU capture-frame timing starts after production uploads and ends after diagnostic copies. Uploads before the first timestamp are not included. GPU transfer/barrier time is measured directly around the copy region. CPU wall and GPU intervals overlap and must not be summed as a critical-path estimate. Writer CPU observations are quantized: median **31.25 ms**, P95/P99/max **62.5 ms**, across 14 retained observations. Background workers remained separate from render and heartbeat paths.

The largest retained capture waits are **6.3794 / 6.3524 / 6.3513 ms** at frames **10414 / 10234 / 9334**. Each joins an actual capture and a completed submission. All final-window captures recorded generation 15 / LOD 14, with no incoming generation. Thus these are repeated capture costs at a held topology, not repeated topology-publication stalls. The previous closer transition witness remains preserved unchanged.

The first/second halves of the retained capture waits have nearest-rank medians **5.4891 / 6.1995 ms** (6/7 samples), and maxima **6.3524 / 6.3794 ms**. These small groups do not prove a continuing upward trend. They do establish recurring higher synchronization cost. The GPU transfer interval is consistently approximately 2.60 ms. Compared with held closer, CPU capture recording remains similar (**0.2354 → 0.2279 ms**), whereas GPU capture interval increases **0.60744 → 5.03952 ms** and GPU transfer/barriers **0.24108 → 2.60084 ms**. Non-capture median fence remains **3.4635 → 3.4501 ms**, although its tail also rises with the greater scene workload.

This supports a capture-associated GPU-work explanation for the repeated wait, not pending writer ownership or a non-returning command call. The larger copied geometry is consistent with the measured transfer scaling. Exact attribution of every additional CPU-observed wait fraction to copy, scheduling or driver work is not established, and there is no diagnostics-disabled A/B run. No blackout causal conclusion follows from these measurements.

## LOD, refinement and bounded residency

Full-session replacement records contain **15 prepared topology generations, LOD 0 through LOD 14**, increasing from 13,826 vertices / 27,648 triangles to **597,594 vertices / 1,195,184 triangles**. `ground-boundary.json` preserves all 15 populations, resource-birth frames and exact allocation sizes. Publication logs show ready physical/normal/cull/compaction/indirect state and atomic ownership, with no missing, overlapping or stale-generation draw.

The legacy planetary patch-list count is **0**; the production topology owns terrain, so that zero is not a terrain-work count. The final journal's maximum visible/TCS population is **16,470** triangles. Retained maximum outer tessellation is **1.0**, inner approximately **0.9999**, raster-domain diagnostic **0**. The accepted tessellation policy is unchanged; high-factor near-surface refinement and horizon workload have not been qualified by this attempt.

Tracked Vulkan allocation high-water: **875,844,984 B** (875.845 MB), compared with closer **581,399,192 B**. All **235 allocations were freed**, ending at **0 tracked bytes**. The complete resource ledger has a death for every incarnation. The last create was frame **3957**; no subsequent resource growth occurred through frame 11534, including the repeated waits.

Each LOD replacement allocates exactly seven expected buffers: lattice `vertices×16`, source indices `triangles×12`, physical vertices `vertices×64`, visibility `triangles×4`, compacted indices `triangles×12`, 20-byte indirect arguments and 168-byte counters. The only other post-startup allocations are the **119,737,728 B regional payload** at frame 1109, **28,759,680 B prepared-pupil scratch** at frame 3197, and **38,246,016 B scratch** at frame 3957. The scratch owner grows to the production vertex capacity and exchanges prepared physical buffers under the existing pupil publication path. These sizes are checked against the recorded topology and source owner; there is no unexplained queue accumulation.

| Residency category | Maximum / final active observation |
|---|---:|
| Immutable topology residency | 158,893,600 B across 15 levels |
| Terrain working storage | 109,995,128 B |
| Topology uploads / working allocations / work reuse | 15 / 15 / 0 |
| Regional payload | 119,737,728 B |
| Regional scratch capacity / bytes | 597,594 vertices / 38,246,016 B |
| Fixed readback storage | 2 × 92,274,688 B |

These categories overlap the allocation ledger and are not independent additive totals. This monotonic approach does not qualify reversing-camera reuse behavior.

| Heap | Retained samples | Maximum sampled usage | Budget | Minimum sampled headroom |
|---|---:|---:|---:|---:|
| 0 | 13 | 431,681,536 B | 15,897,839,616 B | 15,466,158,080 B |
| 1 | 13 | 582,262,784 B | 16,325,505,024 B | 15,743,242,240 B |

No sampled budget pressure occurred. Heap observations are sampled and are not continuous device-wide maxima.

## Preservation, limits and next review boundary

Retained CPU submit max was **0.1268 ms**, present max **0.5643 ms**, device-idle max **0.0590 ms**; no operation remained unresolved. Observer-loop max was **62.4553 ms**, journal-flush max **50.3943 ms**. There was no GPU-progress loss, command non-return, Vulkan/device error, incomplete capture, identity mismatch or recurrence of AuthorityScalars. Read-only Windows System/Application checks found no warning/error/critical event during **23:52:12–23:53:50 UTC**. These facts do not diagnose the original incident.

The observer retains a rolling causal window, not unlimited history. The first alert sample is preserved in `live-01-ground-supervision.jsonl` with its frame numbers and measured costs, but its raw records were no longer present at final stop. The final raw journal proves later repetitions with exact identity. The supervisory file timestamps show approximately 24.20 seconds from alert snapshot to stop record; a future claim of immediate fail-closed response at this repeated-cost boundary requires an explicit mechanism and retained first-failure window, not an assumption that the existing 500 ms gate handles it. No such mechanism was implemented in this turn.

The next bounded responsibility is **offline Project Control review of the measured capture-work budget and the lower-cost stop/evidence boundary**, distinguishing expected volume scaling from unacceptable interference before authorizing horizon. Any future instrumentation or stop-policy correction requires appropriate qualification; this report does not authorize an implementation, timing relaxation, synchronization change, capture reduction or further live attempt.

`preservation.json` verifies the entry source/build identities, 65 retained earlier live files, eight historical evidence authorities, Git HEAD/index/refs, ordinary player executable and restored settings. The accepted 6.0696 ms closer-transition witness is unchanged. Production implementation writes: **0**. Driver/system/KSA changes: **0**. Source changes are this report and the evidence index only. New evidence is bounded to **512 MiB** and the permanent report to **64 KiB**, with exact sizes retained in the preservation audit. No earlier evidence was deleted.

**Ground sequence: STOP / REVISE. Horizon: NOT RUN. Original blackout: UNRESOLVED. Historical discrepancy: UNRESOLVED. Manual acceptance: ON HOLD. No banking.**
