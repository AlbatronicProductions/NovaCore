# Recording-call instrumentation — offline qualified; STOP for Project Control

2026-09-22. **REVISE. The diagnostic changes pass offline Debug/Release qualification. No application/GPU exposure was launched.** The real frame-168 stall was not reproduced with ordinary CPU memory and local API substitutes. Its exact interrupted call is still unknown. These markers are ready for Project Control review before any separately authorized hardware attempt.

The accepted [retained classification](frame168-stall-classification.md) is unchanged: frame 167 / submission 230 completed; frame 168 was recording and never submitted. Pending GPU readback, writer reuse and a frame-submission wait do not explain that event. The original blackout cause and historical 411,877-triangle discrepancy remain **UNRESOLVED**; manual Player acceptance remains **ON HOLD**. UNBANKED; no milestone promotion.

## Change and scope

The native renderer now brackets individual operations from its final tone-map draw through the first eligible frozen capture. The trace includes the first capture-scheduled marker and continues through the remaining authority/diagnostic events and `vkEndCommandBuffer`, so a later stop is not misattributed to an earlier returned call.

It covers 50 operations: final draw, render-pass end, scene-label end; capture clocks; header initialization; host scalar preparation; the incoming-pupil mapped read; each bulk-section declaration and input copy; residency assembly; catalog scan; reservation clock and slot reservation; slot-header assignment; capture-label begin/end; query reset and timestamps; transfer barriers; each of six buffer-copy recordings; scheduling, authority and cost markers; and command-buffer end. The mapped-pupil read has its own operation rather than being hidden inside general header assignments.

`RecordingCallTrace.h` owns the fixed operation IDs and one-shot recording state. `FrozenCaptureNative.inl`, the native recording tail and the capture debug-label wrapper supply the markers. The observer's `RecordingCallProgress` reports the pending operation and last normally returned operation in its existing replaceable progress file, final summary and journal inspection. A return marker means the CPU call returned normally; it is not proof that recorded GPU commands executed. Existing Vulkan-result checks remain authoritative for API errors.

**The detail is emitted only for the first eligible capture interval per native App lifetime.** Earlier ineligible frames and later frames do not emit these detailed markers. A normal complete interval produces **103 records / 52,736 bytes** in the existing recorder. The explicit limit is 160 normal records plus one overflow/error record (82,432 bytes maximum). Overflow stops further detailed recording and is rejected by the observer; it does not skip or change the production operation being called.

The 512-byte journal layout remains unchanged; informational phase 28 carries a versioned call protocol. Markers use only fixed arrays, scalar state and the existing non-waiting recorder. No new heap allocation, mutex, wait, render-thread file operation, resource allocation, GPU query, barrier, transfer or shader work is introduced by the trace. The atomic widths used for snapshots are compile-time verified lock-free. Existing recorder contention/loss detection remains fail-closed. No rendering-quality setting, tessellation policy, camera behavior, publication rule, buffer lifetime or resource ownership transition is changed.

The observer does not use call markers to reset startup, Record, submission or completion deadlines. The one-second unreturned-operation bound, 500 ms completed-operation limit and two-second shutdown grace are unchanged. Observer serialization remains outside the rendering process.

## Retained identity and interpretation

Every call marker repeats the interval context; a surviving suffix can therefore retain its identity without requiring an earlier context packet. The observer retains the decoded state independently of the rolling journal and identifies a partial historical suffix as partial.

| Information | Authority retained |
|---|---|
| Execution identity | Existing serial, QPC, frame, submitted/completed frames, swapchain generation and producer thread; command-buffer handle; last submission sequence |
| Call progress | Protocol version, entry/return/state/finish kind, operation ID, call ordinal, last normally returned operation and ordinal |
| Terrain | Active/incoming generations, topology hash/family, prepared/cull/raster pupil identities, vertex/triangle counts, authoritative and preparation-fence-pending flags |
| Resource identity | Six published geometry/cull/draw source-buffer handles, both readback-buffer handles, main frame fence and capture query pool; creation incarnations join to the existing resource ledger |
| Readback ownership | Both slot states and capture identities at entry and immediately after reservation; no header mutation or ownership change by the trace |
| Input-memory operation | Source buffer, mapped address, byte extent and device-memory allocation handle; host-owned inputs use buffer/allocation zero |
| Transfer recording | Source/destination buffer handles, destination byte offset and copy length; query identity/index for timestamp/reset operations |

Payload words 0–9 are version, event kind, operation, ordinal, last return, last-return ordinal and four operation arguments. Words 10–41 hold the fixed 32-word context. The context layout is: command, active/incoming generation, topology hash, prepared/cull/raster pupils, vertices, triangles, physical/index/visibility/compacted/counter/indirect buffers, readback buffers 0/1, fence, slot-0 state/identity, slot-1 state/identity, submission sequence, capture query pool, current/incoming fence-pending flags, authoritative flag and topology family; the last four words are reserved zero.

The consumer rejects malformed/duplicate intervals, mismatched entry/return pairs, missing last-return authority, frame/command/generation/resource changes, unannounced slot changes and record-budget overflow. Native and observer operation-name tables are compared during probe extraction. The existing journal commit/checksum check still precedes live decoding.

## Offline reproduction attempt and behavior preservation

Evidence root: `E:\NovaCore\build\earth-blackout-closure\recording-call-instrumentation`.

The probe extracts the **actual production recording tail, capture functions, native structures, debug-label wrapper and pre-draw recorder wrapper**. The same `vkCmdDraw` macro mapping used by production is applied to both tails. It also compiles the sealed pre-instrumentation tail and capture functions for comparison; only two namespace qualifications are added to the baseline fixture to prevent ambiguous C++ lookup. No alternate rendering implementation is substituted for the tested command sequence.

Vulkan entry points are local CPU substitutes which record call order and arguments. The real recorder writes a preallocated Windows shared-memory ring; the real capture header and writer ownership code run against ordinary CPU memory. Executable imports contain neither the Vulkan loader nor NovaCore.Native. No device, renderer or game process is created.

Known retained inputs include frame 168, submitted/completed frame 167, submission 230, the recorded command/fence and published/readback buffer handles, generation/pupil 1, topology hash, 13,826 vertices, 27,648 triangles and buffer populations. Catalog size 859 follows the recorded 119,737,728-byte allocation divided by the production 139,392-byte record size. Five resident records were logged. Their **exact bit positions were not retained**; five synthetic valid positions are used. Exact pupil/projection matrices, physical geometry bytes, mapped-memory properties and driver state were also missing, so ordinary synthetic input bytes are explicitly not an exact historical replay.

**Result: no natural non-return reproduced.** Every mocked production operation returns. This tests the ownership/control sequence and bounded header/catalog work; it cannot execute or qualify real driver recording calls or reads from actual GPU-backed mappings.

Both Debug and Release pass:

- **8,812 recording-probe checks each:** old/new API sequence and argument equality; byte-for-byte capture-authority equality after excluding its observation timestamp; enabled, disabled, ineligible and no-capture branches; same ownership; zero measured C++ allocation calls in the recording path; valid recorder checksums/commits; no producer loss; no repeat detail on a later frame; exceptions do not manufacture a return marker; the hard marker cap preserves all production calls and emits a fail-closed error.
- **181 consumer checks each:** replay every actual producer prefix ending at a call entry. All **50** identify the correct pending operation and previous return. Protocol corruption and ownership mutations are rejected.
- **28 existing capture-ownership checks each:** unsubmitted reservation, matching submission/completion, query-not-ready, writer-owned slot refusal, wraparound and shutdown retain their behavior.

These are CPU qualification results, not completed live frozen captures or GPU-membership evidence.

## Cost measured offline

65 paired samples per configuration compare the original and marked recording tails with the real recorder and CPU API substitutes. Setup, memory allocation, worker startup/shutdown and evidence-file writing are outside the timed interval. Values are CPU milliseconds; P95/P99 use nearest rank. With 65 samples, P99 equals the maximum. The added detail occurs once per App lifetime.

| Configuration / series | Median | P95 | P99 / maximum |
|---|---:|---:|---:|
| Debug original | 0.0170 | 0.0315 | 0.0332 |
| Debug marked | 0.0903 | 0.1329 | 0.2160 |
| Debug paired added cost | 0.0724 | 0.1019 | 0.1995 |
| Release original | 0.0097 | 0.0161 | 0.1020 |
| Release marked | 0.0605 | 0.0661 | 0.0724 |
| Release paired added cost | 0.0500 | 0.0531 | 0.0630 |

The five largest marked samples were Debug 0.1191/0.1329/0.1389/0.1602/0.2160 ms and Release 0.0627/0.0661/0.0666/0.0715/0.0724 ms. The raw pairs and summary are retained, including an original Release outlier of 0.1020 ms. These figures do **not** establish live GPU readback, transfer, synchronization or mapped-memory cost, nor the absence of a hardware-sensitive timing effect.

## Builds, stop behavior and recovery

Final native, observer and diagnostic application builds pass in Debug and Release. Each application output contains its matching newly built native DLL; no application was launched. Previously retained diagnostic builds and the ordinary manual-test executable remain unchanged.

The final recovery matrix runs **40 commands with their expected outcomes**, including 26 cross-process CPU-only scenarios. It covers normal operation, workload traffic, injected recording stall, fatal/device-error reporting, fence stall, ring flood, first-frame stall, early native/steady shutdown, and normal/storage-failure/abrupt/backpressure frozen captures. The 15 frozen-format/mutation checks pass in each configuration.

The new injected recording stall is correctly retained as `InputCamera` pending after `EndRenderPass` returned. The observer triggers the same **“renderer operation 8 has not returned for 1s”** stop, writes that call state into durable progress, and terminates only its exact mock child after the unchanged grace period. No checksum corruption or producer loss appears in this case. This is deliberately simulated missing return, not reproduction of the actual mapped-memory or Vulkan failure.

Separate observer-loss tests pass in both configurations: the CPU mock exits after approximately 1,012 ms Debug / 1,020 ms Release when its observer is stopped. No retry, reboot loop or system-setting change occurs.

The unchanged startup policy passes 27 checks per configuration, native admission passes six, and presentation-result handling passes 18. Permanent pupil-handoff tests and tessellation regressions pass; the latter retain 100,000 ordinary inputs and 2,400 singular-neighborhood inputs with zero change to ordinary results. No accepted numerical or preparation correction was removed.

## Reproduction and preservation

The permanent probe entry points are `tools/NovaCore.Causal.Observer/offline/build_frozen_ownership_probe.py`, `build_frozen_ownership_probe.cmd` and `build_recording_call_probe.cmd`. From the repository root, extract with a fresh build output and `--baseline-source build/earth-blackout-closure/recording-call-instrumentation/source-at-entry`, then build/run each configuration. `RecordingCallProbe` takes one fresh output directory. Run the observer with `--test-recording-calls <output>/recorded-commands.bin` to qualify all interruption prefixes. `extraction.json`, `recording-operations.json`, executable import inventories, logs and `probe/recording-compiled-sequence-{debug,release}` retain the final source/measurement linkage. The scoped build/recovery command scripts are retained alongside this active investigation.

`preservation.json` checks the entry source allowlist, old runtime/probe artifacts, original distant retry, historical horizon evidence, accepted regressions, unchanged shader binaries, current native/application pairing and Git identity. `current-task-manifest.json` records the new source and diagnostic-build hashes. `qualification-blocked.json` remains closed to live exposure. No KSA, save, driver, display, system setting, commit, tag, push or banking changes.

The new permanent source/report package has a 128 KiB budget. The active generated package has a 4 GiB budget, principally rebuilt diagnostic binaries and bounded CPU-mock journals; exact measured sizes are recorded in `preservation.json`. It is retained for this unresolved qualification and includes minimal sealed pre-instrumentation source needed to reproduce the equivalence test. Rebuildable output is disposable after acceptance and authorized consolidation; nothing is deleted by this revision.

## Project Control stop

The instrumented command sequence is offline-qualified with the limits above. The exact historical call remains **NOT OBSERVED**. A real mapped-memory read or Vulkan recording call cannot be reproduced by pretending a CPU substitute exercised the driver.

Stop here for review. Any next GPU attempt requires separate Project Control authorization and must remain the lowest-risk distant first-capture stage with these markers and unchanged stop rules. No closer Earth, horizon, orbit or incident-matching route is authorized or run. General GPU-timing payload identity and actual frozen readback cost/membership remain outstanding as already documented.
