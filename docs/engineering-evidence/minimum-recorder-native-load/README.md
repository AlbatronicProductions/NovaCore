# Minimum recorder native-load overflow — OFFLINE PASS

2026-09-27 · FROZEN · UNBANKED · STOP FOR PROJECT CONTROL.
This report records the original offline correction, which made no GPU launch or
deployment. A later explicit authorization deployed it and consumed one native
retry: [recorder PASS; overall production-performance REVISE](../surface-recontact/native-retry/README.md).
No Surface Recontact physics changes, commit, tag, push or banking. No further
native retry is authorized.

## Cause and evidence limits

The loss originated at producer admission: `Produced - Consumed >= 8192`.
The observer copied at most 15 records, wrote a 4 KiB journal page, flushed the
bank, flushed its head, and only then returned ring credit. Persistence throughput
therefore limited admission to 15 events per durable transaction. The corrected
responsibility is persistence amortization, not ring size or event suppression.

Retained unmatched witnesses span 227.077–240.900 seconds after recorder open,
frames 36,357–38,845: approximately 180 FPS and 3,177.5 accepted events/s. All
273 open operations and 38 pending submissions carry producer fault word **1**
(Overflow). Missing edges then produce reducer Protocol=64. Pending ledger
entries are not evidence of actual GPU hangs. First modular flight begins later,
at frame 40,894. Recorder hooks observe frame/update/draw/Vulkan lifecycle, not
contact points/manifolds; contact physics did not directly multiply traffic.

The two current bank incarnations retain 32,516 recent events, sequences
1,198,461–1,230,976, 649.799–669.580 seconds after open. Ordinary complete frames
have 19 records. Retained lifecycle frames reach 821 records. Sliding-window
peaks are 84 records/1 ms, 432/10 ms, 908/100 ms and 2,815/1 s. These are suffix
observations, not whole-session peak guarantees. Instantaneous burst rates must
not be interpreted as sustained production rates.

Historical persistence: 99,539 commits, 199,176 flushes, 48 rotations, 841,970,915
bytes written. The last 4,096 commits have median/P95/P99/max
1.5853/1.8777/5.0700/12.4429 ms. The recent pages show backlog up to 574 records;
Overflow independently proves historical occupancy reached the 8,192 capacity.
The checkpoint grew to 944,356 bytes with unmatched operations/context history.
This amplification can add persistence cost after loss, but is not proven as the
initial trigger.

**Not recoverable:** exact first-drop timestamp, whole-session peak production,
queue trajectory or persistence-stall/rotation timing during the loss interval.
No particular OS/disk stall or rotation is claimed as the historical trigger.
The source-established credit bottleneck is reproduced independently below.

## Bounded correction

Only `OrdinaryObserver.cs` and `OrdinaryJournal.cs` change production behavior.
The observer consumes all currently available records up to
`15 * floor((8192 / 8) / 15) = 1020`, without waiting to fill the batch. This
one-eighth-ring bound accommodates the observed 908-record/100-ms burst and
821-record lifecycle frame. It introduces no additional producer queue.

The journal writes at most 68 existing-format pages, then flushes bank data once
and commits/flushed the head once. Ring credit is returned only after the commit
succeeds. Across a bank boundary, the durable prefix is committed before rotation
checkpoints it; speculative suffix state never enters that checkpoint. Failure
poisons the journal owner. Page/head/checkpoint format, ring capacity, producer
code, required events, fault authority and uncertainty semantics are unchanged.

There is no renderer persistence wait, filesystem operation, GPU submission,
fence, API wrapper or simulation change. The native producer and managed producer
files are bytewise unchanged. The isolated corrected recorder package is under
`build/recorder-overflow/artifacts/bin/NovaCore.Recorder/release`; the frozen
canonical player package has deliberately not been replaced.

## Offline causal experiment

Identical 19-event frame envelopes at 180 FPS for 2,700 frames, real shared
producer and real journal, with a precise 5.07 ms delay injected before each data
flush. The delay is a controlled throughput falsifier, not a reconstruction of
unrecorded historical latency. Actual commit latency includes real filesystem I/O.

| Result | Former 15-record transactions | Corrected batching |
|---|---:|---:|
| Attempted rate, events/s | 3,419.1 | 3,420.3 |
| Maximum occupancy (includes warmup) | 8,192 | 2,433 |
| Dropped | 6,894 | 0 |
| Producer / recovered fault masks | 1 / 65 | 0 / 0 |
| Open / pending at close | 1,024 / 0 | 0 / 0 |
| Produced = Durable | 46,841 | 53,735 |
| Complete | false | true |
| Median commit latency, ms | 6.1856 | 6.2723 |

The same persistence-delay floor remains; amortization resolves the deficit.
An earlier harness using Windows Sleep rounded the requested delay to roughly
15.6 ms. That run also overflowed but is not the matched experiment above. An
initial report-serialization error likewise is not product evidence. Both are
retained as superseded harness outputs, not silently promoted.

## Qualification

- Managed recorder and test projects: Debug + Release, zero warnings/errors.
- Existing durability/fault/clean-close suite: 8,524 checks per configuration.
- Retention suite: 198 checks per configuration.
- Batch boundaries and crash cuts: 342 checks each, including every one of 68
  data pages, partial pages, exact bank limits, cross-bank checkpoint/head cuts,
  failed-owner refusal and strongest verified prefix after corruption.
- Debug + Release: 3,600 frames at 180 FPS, plus ten 802-resource-event bursts
  (821 including the ordinary frame), with a 12.4429 ms injected flush floor.
  Both recover **78,855 produced/durable, zero drops/faults/open/pending,
  COMPLETE**, and zero timed producer allocation. Repeated rotations included.
- Exact native wrappers with stub Vulkan calls: 1,800 frames each in Debug and
  Release, 35,303 accepted/durable events, zero producer allocation/faults; raw
  call counts unchanged. These are native-free tests, not hardware qualification.
- Independent verifier forced overload (~1.9M offered events/s, 100 ms injected
  delay): capacity stops at 8,192, 67,808 explicit drops, zero producer allocation,
  incomplete recovery despite Produced=Durable. No blocking or silent loss.
- Corrected reader applied read-only to the original pinned session returns
  **1,230,976 durable; 3,383 drops; faults 65; 273 open / 38 pending; incomplete**.

Independent source review found no demonstrated correction defect. Its requested
combined rotation/cut adversaries were added and passed. No native acceptance or
Surface Recontact campaign PASS is inferred from these offline results.

## Cost and memory

Matched no-delay, loss-free 180 FPS tests measure producer cost per 19-event frame:

| Microseconds | Before | After |
|---|---:|---:|
| Median | 5.4 | 5.7 |
| P95 | 8.9 | 9.4 |
| P99 | 19.1 | 24.8 |
| Maximum | 60.6 | 50.9 |

No intrinsic producer code changed. The measured median/P99 differences are
0.3/5.7 microseconds per frame; maximum fell. No material producer regression is
demonstrated. Both timed producers allocate exactly zero. Flush count falls
7,734 → 2,522; observer allocations 65,769,072 → 55,971,072 bytes for equal accepted
traffic. Burst stress includes 802 additional events in its maximum; its
816.1-microsecond Release maximum is not comparable to an ordinary 19-event frame.

The mapping remains 2,101,248 bytes. Copied-event references are bounded at 1,020;
the largest batch contains 278,528 journal bytes, with individual pages written
sequentially. Existing bounded reducer/checkpoint capacity is unchanged. Release
burst probe measured process managed retention delta 48,664 bytes and peak working
set 64,253,952 bytes (includes harness); producer allocation is measured separately.
No claim of exclusive recorder memory is inferred from whole-process retention.

## Preservation / next decision

All 3,956 entry files remain. Only the two recorder production files and test
entrypoint changed among existing files; new tests/tools/evidence are bounded to
this responsibility. All 134 canonical player package files and all ten pinned
forensic files remain bytewise identical. Physics, native rendering, settings,
HEAD and index are preserved. Refs had already differed from the earlier surface
seal when this ticket began; this ticket preserves its own entry refs exactly.

See [qualification and identities](qualification.json) and
[reproduction](reproduce.md). Permanent evidence budget: 128 KiB. Generated
native-free sessions/builds remain temporary evidence pending disposition; the
original pinned session is explicitly protected and is not disposable.

Storage at seal: approximately **1.024 GB** of new temporary native-free builds
and synthetic journals; approximately **36 KB** of permanent reports/identities
(below the 128 KiB budget). Regeneratable bulk is disposable after disposition;
retain the isolated corrected helper until Project Control decides its use.
No original forensic data was deleted, rewritten or copied into synthetic output.

**OFFLINE PASS · FROZEN · UNBANKED. STOP FOR PROJECT CONTROL.**
