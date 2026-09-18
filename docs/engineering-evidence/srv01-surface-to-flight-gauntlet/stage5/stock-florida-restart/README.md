# Stock SRV-01 Florida restart — REVISE

Historical checkpoint. The subsequent [final closure](../final-closure/README.md) supersedes this stop with engineering qualification and manual acceptance pending. The original result and limits below remain retained unchanged.

Stage 5 is **not fully qualified**. The stock engine-OFF physical population passed, but the first live process exposed an unexplained material servicing/display tail. The parent campaign explicitly stops when performance materially regresses without bounded cause. No second live run, optimization, profiler, manual acceptance or Stage-6 work followed. UNBANKED. STOP FOR PROJECT CONTROL.

## Product decision and scope

SRV-01 remains the canonical seven-part development spacecraft, not a required Earth SSTO. A separate future launch-stack architecture may carry it. The old 24,798.239377037527 m/s / 2,018,900 kg profile is **SUPERSEDED DEVELOPMENT SIZING EVIDENCE**; its engineering mechanisms remain preserved. The later approximately 11,488.705 m/s / 26,520 kg calculation is **FEASIBILITY EVIDENCE**, not adopted configuration. No tank, engine, geometry, force, contact, iteration or timestep tuning occurred here.

The existing Florida factory now admits the stock contact profile only. Stock wet mass is 705 kg: 630 kg dry + 30 kg fuel + 45 kg oxidizer. It rejects the oversized development identity. The ordinary box children, mass distribution and native solver policy are unchanged. The centered stock configuration uses actual terrain-v5 full-weight graded ground, 48 m east of the Florida anchor, outside the existing platform. Engine and RCS stay OFF for all 1,200 intervals.

## Baseline and preservation

- HEAD/main/origin/main/authoritative remote main: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`.
- Branch: `codex/srv01-supported-contact-admission`; no stage, commit, tag or push.
- All 67 historical tag refs remain identical locally and remotely.
- Seven current source/test/sample seals match; 475 other preflight files are unchanged.
- Previous Stage-5 inputs: 14/14 exact bytes in `prior-stage5-inputs.zip`.
- Three sample predecessor inputs were initially missing from that archive. Independent review caught this. `preserve-sample-predecessors.py` mechanically reversed only this restart's additions in memory, required each original preflight SHA-256, and retained the verified bytes in `prior-sample-inputs.zip`. Current candidate source was never restored or rewritten. The reviewer independently verified all three entries.
- Previous Stage-4 35/35 and Stage-3 29/29 checkpoint seals resolve to current files or exact preserved archives. See `final-verification.json` for each location. Historical reports were not rewritten.

## Stock identity and cheap physical proof

Stock digest: `ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf`.
Site digest: `4d94ff53de3222a0ca81815e37e96647a6b8cecc14e2f3a82f589da0c0c53290`.
COM: `(1.0417021276595744, 0, 0)` m; diagonal inertia: `(190.47255, 731.6006491134758, 731.6006491134758)` kg m², off-diagonals zero. Direct native readback verifies stock inverse mass and the expected basis permutation of the tensor.

The actual physical query identifies body 6, terrain source 2/version 5, physical generation 4, global/regional hashes and grade/facility policy. The exact witness is retained in `results.json`. Initial native linear/angular velocities are zero relative to the local slab while canonical inertial velocity includes Earth rotation (about 407.88 m/s). Full gravity, origin acceleration, centrifugal, Coriolis and Euler ownership remains unchanged. No contact-only force shortcut was introduced.

## Physical and publication results

Debug and Release independently produced the same results:

| Measurement | Actual | Unchanged ceiling |
|---|---:|---:|
| Peak penetration | 0.00009773029136006528 m | 0.020 m |
| Final-600 maximum absolute support-height error | 0.00007900738777877336 m | 0.000122 m |
| Final-600 maximum horizontal drift | 0.0000001620506337545895 m | existing permanent bound |
| Final-600 maximum speed | 0.000005580744646958632 m/s | 0.00365993 m/s |
| Final-600 maximum angular speed | 0.00000227449224037654 rad/s | existing permanent bound |
| Supported final intervals | 600/600 | 600/600 |

The independent product-of-rotation-matrices oracle and analytic derivative consume the actual typed canonical inertial observation at every frontier. Maximum errors: position `8.251539034149307E-09 m`, velocity `6.355287432313019E-13 m/s`, orientation-matrix component `2.1094237467877974E-15`, body angular rate `2.1779126191287486E-19 rad/s`.

All 1,200 frontiers obey original T0 + floor(n×1,000,000/60): 400 intervals of 16,666 ticks and 800 of 16,667; final 20,000,000 ticks; exactly 1,200 StateRevision increments and history records; TimelineRevision unchanged. Exact stock resources/mass remain unchanged. Same native generation/body/shape persists with no pending result or invalidation at completion.

30/60/150/240 Hz host partitions and forty 0.5-second delayed inputs receive exactly equal total credit. Every physical frontier bit and publication record matches the reference. The delayed population reports forty budget-exhausted returns, drains at most four intervals per call, loses no debt and ends at zero debt. Completed calls refuse further credit/evolution without mutation.

## Allocation and retained storage

Checked entry/exit gates pass in Debug/Release: complete admission + step/export/publication/acknowledgement + copied inertial observation = **0 B**; backlog = **0 B**; no-work = **0 B**; application/presentation path = **0 B**. Deliberate `byte[128]` control = **152 B**. No tolerance, subtraction or retry.

Contact lifecycle conservative construction/warm managed allocation bound: 3,207,984 B; native pool 475,136 B, unchanged at interval 1,200; combined 3,683,120 B. Included history payload 1,891,200 B and host-credit payload 76,816 B are not added again. Shared terrain residency is outside this bounded owner measurement.

Whole owned scene measurement: managed retained heap delta 4,439,832 B + native pool 475,136 B + GPU mesh payload 1,359,096 B = **6,274,064 B**, below 8,388,608 B. Scene management includes 640,000 B display/service sample arrays. Shared terrain datasets, device/swapchain and global renderer targets are excluded; the existing two renderer exhaust targets separately report 6,071,008 B at 960×540. This is not a claim that total process/GPU storage is below 8 MiB.

## Performance: headless completed, live STOP

Three fresh Release processes, each 128 warm + 1,024 measured complete operations, include host credit, private step, staged export, canonical publication, acknowledgement, copied observation and typed inertial mapping:

| Process | Median ms | P95 ms | P99 ms | Maximum ms | GC 0/1/2 |
|---|---:|---:|---:|---:|---|
| 1 | 0.0771 | 0.0814 | 0.1006 | 0.1926 | 0/0/0 |
| 2 | 0.0736 | 0.0778 | 0.1052 | 0.2049 | 0/0/0 |
| 3 | 0.0664 | 0.0693 | 0.0858 | 0.1575 | 0/0/0 |

Stage 4/campaign integrated headroom and tail review governs this restart; historical 50 µs first-ticket medians are not silently reinstated as new Stage-5 micro-budgets, nor are these medians represented as passing 50 µs. An earlier preliminary `.0569/.0577/.0625/.1227 ms` sample preceded owned-site/storage closure and is separately recorded, not one of the three final populations. Top-eight absolute tails per process remain in `results.json`; no common indexed tail is established as a cause.

**First and only live population:** 4,000 display frames, 960×540, AMD Radeon RX 6800 XT, Release native validation disabled, existing aggregate timing enabled; no temporary instrumentation.

| Live region | Samples | Median ms | P95 ms | P99 ms | Maximum ms |
|---|---:|---:|---:|---:|---:|
| Display callback interval, including host scheduling/present | 3,585 | 5.5568 | 5.707 | 5.8366 | **74.5905** |
| Host admission + bounded servicing + copied observation | 3,585 | 0.004 | 0.0867 | 0.1944 | **72.4557** |
| Native frame pacing | 4,000 | 5.547 | 5.825 | 5.961 | **74.266** |

GPU average 0.083 ms; native frame average 5.578 ms; fence wait average 5.005 ms / P95 5.195 / max 7.503. Native CPU aggregate update 5.198 ms, host callback 0.157 ms, record 0.056 ms, submit 0.026 ms, present 0.224 ms. These aggregates do not localize the worst callback.

**Cause UNCLASSIFIED.** The service stopwatch surrounds `Advance`; it measures elapsed time and cannot separate actual CPU work from scheduling, JIT, GC, synchronization or another cause. The independently sorted aggregate maxima do not prove the longest display and service samples occurred together. No call is made that the cold white-window residual caused this tail. No new attribution campaign or rerun was attempted.

The live episode completed physically at frontier/revision 1,200 and tick 20,000,000 with unchanged stores/mass. It retained **4,094 ticks of admitted excess debt**. Synthetic equal-total populations had zero debt; the live run did not. `failure=Invalid` in the aggregate line is the existing default status field with `failed=False`, not a reported refusal.

Cold owner preparation after dependency/code warmup: 128.7613 / 127.6638 / 126.5352 ms. First live managed scene preparation: 668.5228 ms, excluding native GPU startup. The known pre-first-present white-window residual remains separate and unresolved. No claim is made that cold setup or its scheduling has been optimized.

## Validation completed and stopped

- Full Debug and Release solutions: PASS, zero warnings/errors.
- Full Simulation: **80/80 registered groups**, Debug and Release. There are 95 `PASS` log lines because some groups print nested witnesses; those are not 95 top-level groups.
- Stage-3 departure proof/refusal/partition/allocation regression: PASS, both configurations.
- ReferenceFrames, Precision, verified BEPU dependency checks: PASS, both configurations.
- Focused assembly presentation, powered support, certified continuation and powered-contact presentation: PASS, both configurations.
- Stock Florida physical/oracle/partition/allocation/presentation tests: PASS, both configurations.
- A subsequently added presentation-storage-only test method compiled and passed in Release. Debug compilation of this final test-only addition was not repeated after the live stop. All production bytes used by the successful full solution builds and live process match the current seals.
- Focused Launcher regression, further live populations and Project Control manual Florida acceptance: **NOT EXECUTED after stop**.

Initial construction journal: the first new test build hit a double-versus-string comparison CS0019; it was corrected before test execution. The next attempt referenced absent Stage-4 native output and hit MSB3030; using the actual retained Stage-3 native outputs resolved the mechanical path. These failures remain recorded; no failed acceptance population was retried to green.

## KSA, handoff and presentation boundaries

[ksa-and-scope.md](ksa-and-scope.md) records direct installed methods and directly reread authenticated Discord history. Full coordinate transport, anchored terrain and ready/apply are ADAPT. NovaCore intentionally retains complete rotating equations and actual native stepping rather than importing KSA's contact-specific Coriolis/centrifugal simplification, terrain angular resistance or on-rails sleep shortcut.

Stage-3's existing nonrotating handoff remains intact. Florida rejects competing free-flight/departure service while active and after completion. The complete inertial endpoint is available, but **executable rotating-site departure is not qualified**: the current departure clearance/evaluator has nonrotating/constant-local-gravity assumptions. No fabricated combination is admitted.

The new visible route uses the existing SRV-01 meshes and copied canonical site-local endpoints, holding between publications. It renders the exact 16×16 m graded collision patch, not a complete Florida facility or future pad/stack. Current source retains correct slab dimensions. The focused presentation test independently checks center/count/camera nonmutation but does not separately assert scale/mesh despite a broader assertion label; do not treat that assertion as independent dimensions proof. Manual inspection remains required and blocked at this performance stop.

## Independent red team and disposition

The read-only reviewer found no additional runtime correctness defect, confirmed all source seals and recovered predecessor bytes, and agreed the live tail requires stopping. It explicitly rejected claims of paired maximum samples, zero live terminal debt, rotating departure qualification or completed manual acceptance.

New/changed source is exactly the seven paths in `candidate-seals.json`. No authored geometry, native solver, terrain data, BEPU dependency, README/legal document or historical evidence was changed. Current evidence scripts, archived predecessor inputs and this bounded report are retained. See [cleanup.md](cleanup.md) for all eight exact roots and the reviewed five-root release command; Stage3/native, old Stage5 diagnostic binaries and current stopped output remain needed. No deletion was attempted.

**REVISE. Stage 5 incomplete. Stage 6 CLOSED. UNBANKED. STOP FOR PROJECT CONTROL.**
