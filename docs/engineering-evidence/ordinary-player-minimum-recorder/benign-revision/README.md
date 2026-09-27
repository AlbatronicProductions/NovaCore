# Benign recorder correction and one live retry

2026-09-26 local / 2026-09-27 UTC. **PASS — bounded benign recorder qualification; independent review PASS.** Frozen, unbanked. No further application launch authorized by this package.

The first healthy Solar-overview run remains unchanged historical evidence. This bounded revision closes its three qualification gaps without reopening recorder architecture or changing ordinary player camera behavior.

## Correction

The old short synthetic Earth-focus keypress could miss the polled input boundary. The opt-in qualification host now calls the existing production Earth-focus controller once, then requires actual Earth body identity and correctly encoded distant camera range every frame. Five seconds of valid warmup precede twenty seconds of valid hold. Failed coverage closes the run; it cannot silently refocus or claim Earth coverage from an input request.

Clean shutdown acquires the existing nonblocking producer lease, reserves both close records, emits a successful sealed final sequence, then closes admission under that same lease. Native and managed emitters recheck the seal. Recovery reports COMPLETE only for a valid durable head/checkpoint with final successful close, Produced=Durable, no corruption/fault/drop/open operation/pending submission or context. Missing, failed, legacy, interrupted or damaged closure remains uncertain. A valid persisted head does not depend on a surviving volatile ACK.

The opt-in measurement times explicit CPU recorder bookkeeping, suppresses nested double counting, and excludes raw Vulkan calls, callbacks and persistence waits. Fixed storage bounds samples; clock failure or overflow faults qualification. Actual recorder-owned allocations and retained capacity are counted; a separate offline global-new probe checks bypass allocations. No Vulkan feature, extension, buffer-usage, submission, wait, fence or resource-lifetime policy changed. Shader/assets and ordinary camera/controller source remain unchanged.

## Offline qualification

- Managed/native Debug and Release: PASS; managed builds zero warnings/errors.
- Recorder durability, fault, closure and camera gate suite: **8,524 checks each**.
- Actual production controller/encoded-submission camera test: **6,010 checks each**, zero and current epochs, no GPU.
- Exact production native wrappers with CPU Vulkan fakes: **1,800 frames / 35,303 produced=durable each**. Every raw fake asserts it is outside a timing scope; exact API forwarding/count, nested timing, excluded wait, capacity, retained storage, zero timed global allocations, resource reuse, failed submission and rotations pass. This fixture's legacy close remains uncertain; sealed COMPLETE is tested through the actual managed host close and independent worker.
- Application diagnostics 113, launcher 19, presentation outcomes 18, regional pupil dependency handoff: PASS.
- Canonical repository-layout package: **134 files / 66 shaders**, PASS. Required terrain cache remains an explicit dependency; no standalone publish claim.

## Single live retry

Canonical `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe --qualify-recorder <report>`; session **c7de847a-1f9b-4ac3-89f6-1fe4a160517a**, PID 27848. Exactly one retry, no relaunch. Normal shutdown, exit 0.

The unchanged Native-display setting selected the **2560×1440 monitor, borderless**, renderer **2560×1322**. This is not a 3440×1440 qualification. No cross-resolution or first-run performance subtraction is used. A live window observation visibly confirmed distant Earth; no display instability was observed.

Camera distance was **73,566,072.914 m** from Earth's center (radius 6,371,008.8 m), about 67,195 km altitude. Actual focus/submitted body were 6, at 1×, unpaused, outside editor/flight. Every held observation satisfied the camera gate. Recorded coverage markers join to successful submission, positive completion and present-return identities. All 3,596 measured frames correlate to completed GPU work.

- **4,476 frames** over 25.0217 s; **3,596 measured held frames over 20.0001204 s**.
- **88,499 produced=durable events**, all checksummed and contiguous.
- **4,539 successful submissions/completions** (63 bootstrap + 4,476 frames), 4,476 present returns.
- **276 resource births**, all retired; no open/pending operations, fault, drop or corruption.
- Generation/publication transitions `(0,0,0) → (0,1,0) → (1,0,1)`; roles and incarnations coherent.
- Three journal rotations; final checkpoint 76,120; all 12,379 retained tail events match the live copy byte-for-byte. Full 88,499-event reduction also equals recovered canonical state (222,230 bytes, SHA-256 `3b619aa9d8d43dfd8c0bfb4afabfca5292265d71c782efa0e86927af97ebcd6c`).
- Recovery: **COMPLETE**. Present return still does not claim physical scanout completion.

## Measured cost and limits

| Metric | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|
| Producer bookkeeping per held frame, µs | 17.7 | 20.2 | 27.1 | 136.2 |
| Amortized bookkeeping per event, µs | 0.932 | 1.063 | 1.426 | 7.168 |
| Producer thread cycles per frame | 49,191 | 56,655 | 66,759 | 100,575 |
| Held start-to-start cadence, ms | 5.5567 | 5.7403 | 5.8806 | 38.5999 |
| Existing GPU samples, ms (37 samples) | 0.683 | 0.830 | 2.950 | 2.950 |

Producer QPC includes measurement overhead and scheduling; tiny unscoped dispatch/setup instructions are excluded. Per-event values are amortized frame totals, not individually timed call quantiles. GPU whole-run mean was **0.674 ms**; GPU samples are existing instrumentation, not every-frame GPU quantiles.

Producer P99 is 0.244% of the preferred 11.11 ms frame budget. Worst complete one-second producer mean was **18.628 µs**; worst such frame-cadence mean **5.682 ms**. Both satisfy the predeclared bounded recorder/production gates. No meaningful recurring regression appears in this benign workload; no recorder-disabled live A/B claim is made.

One held frame, loop 2977, took **38.595 ms**: Update 38.1972 ms, including a normal 5.112 ms fence wait; measured recorder work only **24.2 µs**, Draw 0.3939 ms. Adjacent frames were 5.4274/0.9320 ms. It is an isolated CPU/update-side excursion, not evidence of recurring recorder cost or GPU progress loss. Its underlying scheduling/host cause was not established. Startup maximum 154.7173 ms cadence is separately retained, outside timed hold.

Timed recorder-owned allocations: **0 calls / 0 bytes**. Producer retained capacity **4,479,176 bytes**: recorder object 2,377,928 plus ring/header 2,101,248. This includes the fixed qualification sample buffer and is not OS working-set measurement.

Persistence owner over 28.284 s: **1,187.5 ms CPU (4.20% of one core)**, 169,203,800 cumulative managed allocated bytes, **577,608 retained managed bytes**, 58,978,304 peak working-set bytes. **59,409,955 bytes written (2.003 MiB/s)**, 14,284 flushes; fixed binary storage 83,902,464 bytes. Its sampled commit P99/max were 5.284/9.8612 ms, entirely outside renderer-thread ownership. Maximum sampled Produced−Durable backlog was 984, below 8,192 capacity, and drained to zero.

## Frozen identity and preservation

- Source (955 files): `e73864d78502d00125de2ede19b2230cec4bb3a0694fcdd052883e5c88acb830`.
- Canonical package (134 files): `715efb23078db399c241ba346a1c7862247a80e7d212f50d0b02f46473627fd7`.
- HEAD unchanged: `8c189b28ce2a68f97de734d1589acb500c41fd99`.
- Source/package/settings identities unchanged across exposure; HEAD, refs and index unchanged. All unrelated campaign-entry files preserved. No KSA writes, commit, tag, push, bank or milestone assignment.

Detailed identities and bounded preservation are adjacent JSON files. Bulk run evidence remains in `build/ordinary-recorder-benign-revision/live-retry`; compact measured summary and reproduction scripts are retained here. Prior qualification/capture evidence is preserved.

Two independent read-only reviews passed: native route/cost/identity review and durability/head/checkpoint/full-prefix correlation review. Neither reviewer edited code or launched the application. Findings and limits are recorded in [independent-review.md](independent-review.md); [reproduce.ps1](reproduce.ps1) runs CPU-only qualification.

This is benign recorder qualification only. Close Earth, ordinary exploration, blackout reproduction/cause, Surface Recontact and Player acceptance remain unqualified/on hold. **STOP FOR PROJECT CONTROL.**
