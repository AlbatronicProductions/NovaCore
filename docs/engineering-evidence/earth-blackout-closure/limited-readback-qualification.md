# Limited live readback qualification — startup stop / REVISE

**2026-09-22. STOP. The authorized attempt ended before its first rendered frame. Actual frozen GPU readback and its performance remain unqualified. Blackout cause UNRESOLVED; historical 411,877-triangle discrepancy unresolved; Manual Player acceptance ON HOLD; UNBANKED.**

## Attempt and observed boundary

Project Control authorized low-workload GPU capture qualification only. One diagnostic Debug application was started under the observer with a 20-second distant-Earth stage gate. The camera stage only focuses Earth; it performs no approach, zoom or orbit. No closer or incident-matching stage was started.

The startup UI was changed from the saved native borderless setting to windowed 960 × 540. The application imposes a 700-pixel minimum client height; native setup logged a **960 × 582 renderer extent**. This was a small-window diagnostic attempt, not the original native-resolution route. The exact saved display preference was restored afterward.

The supervised UI interaction and subsequent managed scene loading consumed the observer's shared 60-second pre-render startup allowance. The observer decided `renderer never started within 60s` **141.8325 ms before the first native causal record**. Its stop request was already set when the native recorder opened. Native initialization completed, observed that request and cleaned up without entering the frame loop.

This locates this qualification stop in the observer/UI startup handoff. It does **not** identify a terrain, device or blackout cause. The native session's startup text and zero-frame average are not rendering qualification.

| Authority | Observed value |
|---|---|
| Application PID / start | 12420 / 2026-09-22 19:25:33.7717408 UTC |
| Observer first stop QPC | 166174733436 |
| First native record QPC | 166176151761 |
| Native cleanup completion QPC | 166187515099 |
| QPC frequency | 10,000,000 Hz |
| Native progress from first record through cleanup | 1.1363338 s |
| Stop decision to completed cleanup | 1.2781663 s |
| Rendered frames submitted / completed | **0 / 0** |
| Scheduled / completed / durable frozen captures | **0 / 0 / 0** |
| Target distant-Earth frames | **0** |
| Successful initialization queue submissions | 63; these are startup/upload work at frame identity 0 |
| Journal | 709 records; all received/durable; no corruption, gaps, overrun or contention loss |
| Termination | Application exit 0, no forced kill; observer exit 3, qualification failed |

Vulkan device initialization and GPU uploads did execute on the AMD Radeon RX 6800 XT with validation enabled. There were no recorded Vulkan errors or returned device loss. The existing unused vertex-output warning appeared during pipeline creation. A bounded System-event query covering the attempt found no Display, WHEA, Kernel-Power or WER-SystemErrorReporting event. These observations do not establish an external cause or qualify recovery from a device hang.

The automatic stop was honored. **No relaunch was performed after this result.**

## Required performance evidence

The table deliberately distinguishes missing samples from zero cost. No frame, readback transfer or frozen-state membership result exists for this attempt.

| Measurement | Samples | Median | P95 | P99 / maximum | Meaning |
|---|---:|---:|---:|---:|---|
| Absolute GPU capture-frame cost | 0 | unavailable | unavailable | unavailable | No rendered frame submitted |
| GPU readback transfer + barrier interval | 0 | unavailable | unavailable | unavailable | No readback scheduled |
| CPU capture recording / completion / writer cost | 0 | unavailable | unavailable | unavailable | No capture produced |
| CPU wall time in initialization queue submission | 63 | 0.0240 ms | 0.0496 ms | 0.1777 ms | Host API time; not GPU work duration |
| CPU wall time in initialization queue-idle wait | 63 | 0.1487 ms | 0.4533 ms | 0.7094 ms | Startup synchronization; not per-frame readback synchronization |

Percentiles use nearest rank. The ten worst retained occurrences are in `live-01-performance.json`; the three largest initialization queue-idle waits are 0.7094, 0.5409 and 0.4959 ms. They all have frame identity 0. Their distribution is not evidence of repeating readback cost. Device-idle waits had two samples (0.0049 and 0.0248 ms). Total native setup was 1,049.6917 ms, including a 597.4583 ms swap/setup scope; completed cleanup was 80.7349 ms. These setup scopes are distinct from active-renderer deadlines.

Recorded allocation high-water was **448,917,284 bytes** across 110 allocations. All 110 were freed; final recorded allocation bytes were zero. The diagnostic readback reservation contributes two 88 MiB buffers to setup. This allocation ledger is not driver-reported heap usage. The memory-budget extension was available, but **no per-heap usage/budget sample was emitted before the frame loop was skipped**; pressure/headroom qualification is therefore unavailable. There is no meaningful prepared-terrain residency high-water from this attempt.

Observer loop maximum was 31.6792 ms and flush maximum 1.6987 ms. These are observer wall-time measurements, not the incremental cost of the renderer diagnostics. No A/B or GPU overhead conclusion is drawn.

## Diagnostic additions qualified before launch

The accepted frozen capture format and production corrections were preserved. Narrow additions made the requested measurements and live stop decisions observable:

- A dedicated two-timestamp query brackets the diagnostic GPU transfer/barrier interval. Results are read after the existing completed frame fence. The total frame timestamp still includes diagnostic work. No new fence wait or quality limit was introduced.
- Fixed causal events record CPU wall time spent building/recording capture inputs and reading completed queries. The background writer publishes high-resolution wall time and operating-system thread CPU time for hashing/writing; thread CPU accounting can quantize short samples to zero.
- One bounded background observer task validates each published capture's hash, identity, synthetic/live provenance and exact visibility/compaction membership. The heartbeat does not wait for hashing/sorting. Validation lag beyond the retained two-slot window or three seconds stops the attempt. Final journal/submission/lifetime joins remain mandatory.
- The observer enforces the gate's maximum exposure duration and stops if a supported heap lacks 176 MiB of free reported budget, equal to one complete capture-storage reservation. This is a qualification stop threshold, not a rendering clamp.
- `measure_readback.py` computes retained CPU/GPU distributions separately, preserves the worst occurrences and reports absent samples as null. It cannot launch a renderer.

Before this attempt: native and application/Graphics-test Debug and Release builds passed; observer Release build passed; full-size CPU captures passed in Debug and Release; final normal, workload, cleanup, stale, storage, occupied-slot, abrupt-exit, fatal, stall and overflow cases produced their expected results. Fifteen format/background-validation checks passed, including corrupt live-capture rejection. The accepted pupil-handoff, tessellation arithmetic and presentation-outcome regressions passed in Debug and Release. These are offline checks; they do not fill the missing GPU results above.

## Evidence and next decision

Evidence root: `E:\NovaCore\build\earth-blackout-closure\readback-qualification`.

- `live-01-distant`: untouched run identity, copied authorization/seals, journal, progress, failed summary, analysis, lifetime ledger and copied bounded application session.
- `live-01-performance.json`: measured initialization costs and explicit zero-sample capture distributions.
- `clock-before-live.json`, `live-01-exit.json`: clock correlation and exact observer exit.
- `final-recovery-tests.json`, `format-final/regression-results.json`, `cpu-tests.json`, build/regression logs: pre-exposure qualification.
- `launcher-settings-restoration.json`, `system-events-after-attempt.json`, `preservation.json`: restoration and preservation evidence.

The copied authorization remains historical evidence of this attempt. The active gate is closed again. Source/build identities in the run's sealed manifest were verified before launch and after exit. The ordinary executable, unrelated repository work, accepted corrections/regressions, historical live witness, Git HEAD/index/refs and KSA remain preserved. No driver/system settings, commit, tag, push, banking or milestone promotion occurred.

The next bounded issue is the **pre-render qualification handoff**: UI waiting must be distinguished from managed/native startup, and an expired pre-render authorization should not permit subsequent GPU initialization. That should be resolved and CPU-qualified before another live attempt. Simply extending the active GPU-progress deadlines would not address that ownership boundary.

No further exposure is performed in this pass. Project Control receives an **incomplete live qualification**, not a capture/readback PASS and not blackout closure. Actual GPU-produced snapshots, fresh population reconciliation, capture overhead, budget observation and active-renderer termination remain to be qualified under a reviewed startup boundary.
