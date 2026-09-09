# VERIFY B — actual production assembly qualification

Verdict: PASS for the measured bounded recovery. No hidden context/cache or setup displacement was found. This is performance qualification of the query service, not spacecraft-contact capacity certification.

The same benchmark executable loads the original preserved Core/Graphics DLLs or the final production Release DLLs; no cloned query implementation is used. Four fresh processes run A-forward, B-forward, B-reverse, A-reverse, with exclusive CPU timing coordination. DOTNET_TieredCompilation=0 and DOTNET_ReadyToRun=0 deliberately control optimized JIT execution and avoid tier-transition/profile ordering effects. These absolute costs must not be numerically merged with the cost worker's default-tiered/PGO measurements or the original ~4.06 ms figure. They are an independently conservative controlled comparison of the actual production code.

Each pattern has 1,024 timed samples per process after 32 complete pattern warmups. Timing includes all actual Query calls, deterministic index rotation, result-array stores and timestamps. Output arrays and coordinates are preallocated; full results are consumed into SHA-256 outside the measured loop. All ten pattern result hashes and status populations match in all four processes, and preflight/final hashes within each process match. Every timed pattern reports 0 allocated bytes, 0 allocations and 0 collections in all three GC generations. P95/P99 are descriptive sample quantiles, not guaranteed maxima or confidence intervals.

The four-leg pattern uses an 8 m × 8 m square, leg centers at east/north ±4 m. Four points means one/leg; eight means two per leg at ±0.15 m east; sixteen means four per leg at ±0.15 m east/north. Nearby16 uses 16 distinct directions on a 6 m strip. Input rotation changes traversal order deterministically. Boundary16 straddles the physical regional boundary near 28° latitude. Florida, inland and explicit unqualified-pole workloads are separated.

## Actual cost (microseconds, range across two processes per variant)

| Pattern | Baseline median | Revised median | Baseline P95 | Revised P95 | Baseline P99 | Revised P99 |
|---|---:|---:|---:|---:|---:|---:|
| single-regional | 338.7–346.1 | 135.6–137.9 | 394.4–430.5 | 163.6–228.8 | 425.8–465.5 | 194.6–297.1 |
| repeated-identical-16 | 5596.9–5813.1 | 2253.5–2356.4 | 6016.1–6161.3 | 2422.5–2711.4 | 6235.9–6614.3 | 2652.4–2880.9 |
| nearby-16 | 3506.5–3647.5 | 1608.2–1609.0 | 3771.7–4076.6 | 1959.3–2057.8 | 4009.6–4534.2 | 2111.2–2256.5 |
| regional-foot4 | 709.5–720.0 | 345.9–366.1 | 943.2–944.9 | 421.1–506.9 | 1041.4–1067.0 | 471.3–521.2 |
| regional-foot8 | 1431.7–1433.4 | 693.0–694.7 | 1714.5–1759.9 | 826.9–879.4 | 1829.3–1977.0 | 884.6–928.0 |
| regional-foot16 | 2926.4–2958.1 | 1377.4–1394.7 | 3296.0–4037.3 | 1588.0–1592.9 | 3489.9–4189.4 | 1679.4–1683.0 |
| coverage-boundary-foot16 | 2047.4–2174.7 | 1094.8–1123.3 | 2288.3–2940.1 | 1288.8–1350.5 | 2505.4–3253.9 | 1373.9–1487.0 |
| florida-foot16 | 671.3–697.7 | 684.4–687.6 | 816.0–915.8 | 798.0–942.8 | 873.5–1026.3 | 922.7–1047.2 |
| inland-foot16 | 2544.4–2636.0 | 1261.0–1305.2 | 2808.2–3040.4 | 1382.1–1673.8 | 3006.5–3335.6 | 1429.2–1792.5 |
| unqualified-south-pole | 570.5–596.2 | 195.8–203.2 | 630.9–726.1 | 240.5–322.4 | 685.3–788.6 | 271.7–394.5 |

## Attack outcome and amortized cost

Strongest objection was that recovery might exist only for repeated identical points or be moved into setup. It did not survive: a real 16-contact footprint improves from 2.926–2.958 ms to 1.377–1.395 ms, recovering 1.532–1.581 ms, while boundary and independent nearby patterns also improve. A fully qualified first-iteration Florida pad pattern is unchanged within observed variance; no gain is claimed there. Qualification status remains Ready for all corresponding physical footprint points, and NormalUnqualified for the pole case.

No new batch/context lifecycle was introduced. Query authority is still acquired once after data publication. Existing process setup is explicitly material: global read/verify/decode 70.5–75.9 ms; regional resolution 300.8–326.3 ms; regional read/decode/hash/publication 200.4–205.3 ms; first authority acquisition 7.2–8.1 ms; first regional query including JIT 14.4–15.8 ms. Pattern construction includes its own first-use JIT and takes 9.3–10.2 ms in this harness. Preflight/warmup costs per pattern are recorded in the original run JSONs. These are cold-process observations with an already populated OS file cache; they are not a cold-disk guarantee. None is newly moved out of Query by the revision.

Repeated warm TryAcquire costs 35.8–41.8 microseconds and allocates 70,232 bytes/call due to existing manifest/identity work; it must not be mistaken for a zero-allocation operation or called once per physical contact. The zero-allocation contract applies to Query. The new code does not add acquisition to the loop. Input pattern/output-copy setup is harness work and unchanged across variants; source assembly copying is deployment, excluded from timing.

Illustrative amortization, not a chosen simulation cadence: sixteen contacts at 60 reevaluations/s consume approximately 83 ms of CPU core time per second at the revised median; at 120/s approximately 167 ms/s. Four contacts at 120/s consume approximately 42–44 ms/s. This provides useful room for a first craft and additional simulation, but does not justify unlimited craft count, repeated data admission or silently adopting a contact cadence. Several high-rate 16-contact craft would still spend a substantial fraction of one core. Project Control must judge later gameplay composition with measured total simulation work.

## Reproduction / identities

`actual-benchmark/Program.cs` and `ActualBenchmark.csproj` build the native-free harness against the preserved starting assemblies. `actual-runner.py prepare` creates external baseline/candidate hosts and records every assembly hash. `actual-runner.py run` executes the balanced sequence only when the CPU slot is free; `actual-compare.py` requires exact output hashes, statuses and zero allocations/GC. Do not overwrite starting-assemblies or the existing previous parity host-candidate.

`starting-identity.json` proves preserved pre-revision source and DLLs. `final-actual-input.json` identifies the tested final query source and both complete host identities. `actual-runs.json` records exact invocations and durations; all four actual-*.json files retain setup and timing distributions. `actual-result.json` is the compact comparison. The final tested source hash was rechecked after measurements. No production or GPU execution source was modified by the verifier.

Existing cleanup remains PROJECT CONTROL CLEANUP REQUIRED: 683 proven earlier disposable files / 45,650,081 bytes, verified without another deletion attempt. New external benchmark build/host output is additional temporary diagnostic material and must be counted at final consolidation; do not call the old total a final whole-ticket total.
