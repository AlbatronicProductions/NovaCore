# Frame 168 recording stall — retained-evidence classification

2026-09-22. **REVISE. No application or GPU launch in this revision.**

The distant retry stopped on the CPU side of command recording, after completion of frame 167 and before submission of frame 168. Pending GPU readback completion and background-writer reuse blockage are excluded by the retained submission history and production ownership transitions. The precise stalled CPU call is **not observed**. Ordinary Vulkan command recording and the first diagnostic capture's input reads/command recording remain distinguishable hypotheses, not established causes.

The existing timeout and fail-closed behavior remain unchanged. The blackout cause and historical 411,877-triangle discrepancy remain **UNRESOLVED**. Manual Player acceptance remains **ON HOLD**. UNBANKED; no milestone advancement.

## Evidence authority and limits

Original run: `E:\NovaCore\build\earth-blackout-closure\startup-lifecycle\live-01-distant`, process 28408. The journal, original analysis, summary, lifecycle, resource ledger and application logs were read without modification. New derived evidence and CPU probe outputs are under `build/earth-blackout-closure/frame168-classification`.

`classify_recording_boundary.py` independently checks all 6,808 committed records: contiguous serials, valid checksums, emitted = received = durable, zero overruns and zero contention drops. This is the entire producer history, not an overwritten suffix. It joins the last submitted fence to the last successful wait. `recording-boundary.json` retains the events, thread identity, resource births and classification. `entry.json` and `preservation.json` seal prior evidence and current source/build preservation.

Statements marked **OBSERVED** come from retained events or logs. **DERIVED** statements follow the sealed production code and observed history, assuming its normal ownership invariants; no assertion is made about hypothetical unobserved memory corruption. **NOT OBSERVED** means the missing authority is not reconstructed by approximation.

## Submission, completion and stop sequence

QPC frequency is 10,000,000 ticks/second. The producer thread on these events is 21068. Swapchain generation is 2.

| Boundary | Serial / QPC | Established fact |
|---|---|---|
| Initial resource uploads | Submission sequences 1–63, frame 0 | Bootstrap work, not 63 rendered frames |
| Rendered frames 1–167 | Submission sequences 64–230 | Every rendered frame has a successful submission; completed cursor reaches 167 |
| Frame 167 submit success | 6768 / 186387364505 | Submission 230, frame fence `380431023210842` |
| Frame 167 fence wait | Begin 6772 / 186387367854; end 6773 / 186387384563 | Same fence; `VK_SUCCESS`; 1.6709 ms wait |
| Completed cursor publication | 6775 / 186387384631 | Explicit completed frame 167; the preceding wait-end record still carries cursor 166 because the assignment follows the wait |
| Frame 168 image acquisition | End 6790 / 186387408756 | Success, swap image 1 |
| Frame 168 recording begins | 6791 / 186387408817 | Submitted = completed = 167 |
| Frame 168 dispatch/draw markers | 6792–6808 | Four dispatches and thirteen draws recorded on the CPU; no evidence of GPU execution of these commands |
| Last producer event | 6808 / 186387412098 | Pre-call marker for the final tone-map `vkCmdDraw(c,3,1,0,0)` |
| Observer stop decision | 186397507542 | Recording had not returned for 1,009.8725 ms; existing one-second bound triggered |
| UI shutdown acknowledgement | 186397550666 | 4.3124 ms after stop; native cleanup/app-exit milestones remain absent |
| Last observer heartbeat | Journal header / 186417599356 | 3,018.7258 ms after last producer event; 2,009.1814 ms after stop |

There is no frame-168 RecordEnd, submission begin/end, or present. The observer's maximum loop interval was 47.1193 ms and maximum flush was 2.565 ms. It continued heartbeating and draining; missing observer heartbeat is not the initiating failure. The exact child was terminated after the existing two-second shutdown grace, exit code -1. Native cleanup never began, so a cleanup-time writer join is not this stall.

Submission 230 names graphics queue `2603403393088`, command buffer `2603721397440` and the fence above. The frame-168 final draw marker names command buffer `2603718174112`; it has no corresponding submission. These handles are joined within this process lifetime, not treated as persistent identities across runs.

**Last positively completed GPU work:** all commands in frame 167's graphics submission 230, confirmed by its matching successful fence wait. This includes incoming terrain preparation/culling/compaction and the then-current fallback scene rendering. The final draw marker at serial 6808 is CPU intent, not a completed GPU operation. Present success is a returned presentation call, not proof of physical display scanout completion.

**DERIVED fence state at frame 168:** production `Draw` resets the frame fence successfully before calling `Record`. RecordBegin therefore proves that reset returned successfully. The fence is reset/unsignaled for an **unsubmitted** frame. There is no frame-168 queue operation whose GPU completion is pending on that fence. An unsignaled fence alone would misclassify this boundary.

## Capture/readback ownership

The only retained FrozenCapture event is initialization: two 88 MiB slots, 1,000 ms cadence. There are no capture-scheduled, capture-completed, durable-publication, or FrameAuthority records. No frozen file exists in the original run.

The initial slots are Free. The worker consumes only Ready slots. The producer transitions are:

`Free → Reserved → InFlight → Ready → Writing → Free`

Reservation precedes recording the six copy commands. InFlight requires a successful frame submission. Ready requires the matching completed frame fence and available queries. The worker alone changes Ready to Writing and returns the slot to Free after saving.

| Question | Classification | Evidence and implication |
|---|---|---|
| Which slot could own the first capture? | **DERIVED:** slot 0 Free; slot 1 either Free or Reserved for identity 1, frame 168 | First reservation selects `(0 + 1) % 2`. Capture entry/reservation were not separately recorded, so the exact Free/Reserved boundary is NOT OBSERVED. |
| Could a previous capture still be in flight? | **No, DERIVED** | Before frame 168 there was no eligible active terrain draw and no scheduled capture. An intact history contains zero capture scheduling events. |
| Was GPU readback completion pending? | **No, DERIVED** | No capture-bearing submission occurred. Reserved is ignored by `CompleteFrozenCapture`; only InFlight can query completion. The prospective copies in frame 168, if recorded, were never submitted. |
| Was the writer holding a slot and blocking reuse? | **No, DERIVED** | No prior capture could reach Ready or Writing. This was the first possible reservation. Furthermore, Reserve uses one compare/exchange and returns a failure immediately on a busy slot; it does not wait for the writer. |
| Could a writer shutdown join explain the live stop? | **No at this boundary** | The renderer did not leave Record or enter native cleanup. |
| Did first capture admission finish? | **NOT OBSERVED** | The first scheduling marker is after authority input reads, reservation and transfer command recording. Its absence does not locate the interrupted operation within that interval. |

The production member `anchoredPipelineStatisticsFrameSubmitted` is set from draw eligibility **during CPU recording**. Its name does not prove a queue submission. Frame 168 is the first eligible frame after terrain publication. Treating that flag as actual submission would invent an InFlight capture and a GPU wait that the queue trace excludes.

Readback buffer births are serials 224 and 226, handles `385928581349727` and `388127604605281`, each 92,274,688 bytes. Both survive through the final event. They are allocated storage; allocation does not establish that a transfer has executed.

## Terrain publication and resource state

**OBSERVED:** frame 167 completes preparation for generation 1, topology family 1 / level 0, topology hash `0x118D7D350CB66136`, pupil identity 1. Population: 13,826 vertices; 27,648 triangles; 15,368 visible; 12,280 horizon-rejected; 46,104 compacted indices; zero screen-rejected, invalid or overflow counts in the completed publication counters. The host then publishes generation 1. Before frame 168: active generation 1, incoming generation 0, authoritative true, current/incoming preparation-fence-pending false.

Frame 168's indirect-draw marker references the published indirect buffer. Its four dispatch markers are CPU-recorded groups 217, 1, 432 and 432. These commands were not submitted, so the frame-167 publication population must not be represented as frame-168 GPU visibility or raster statistics.

| Published resource | Buffer handle | Birth serial | Bytes |
|---|---:|---:|---:|
| Topology lattice | 560750930166270 | 6681 | 221,216 |
| Topology indices | 562949953421824 | 6683 | 331,776 |
| Physical vertices | 565148976677378 | 6685 | 884,864 |
| Visibility | 567347999932932 | 6687 | 110,592 |
| Compacted indices | 569547023188486 | 6689 | 331,776 |
| Indirect draw | 571746046444040 | 6691 | 20 |
| Counters | 573945069699594 | 6693 | 168 |
| Regional payload | 576144092955148 | 6740 | 119,737,728 |

No listed resource is destroyed or replaced through the final retained record. There is one resident topology (552,992 bytes), one work allocation, zero work-slot reuses, 1,327,420 work bytes and no spare work slot. Five regional records were uploaded (696,960 bytes) within the allocated payload. Publication has already cleared incoming preparation; it is not waiting for frame 168's fence.

The run remains distant Earth: altitude 67,195,064 m, viewport 960 × 582, windowed. Recorded application allocation high-water is 570,590,716 bytes. Retained memory-budget samples do not show pressure; there is no new Vulkan/device error. These facts do not exclude an unrecorded driver/internal stall. Allocations still live at forced termination are not evidence of a persistent leak by themselves.

**NOT OBSERVED:** exact frozen pupil matrices, geometry bytes, per-triangle visibility membership and frame-168 capture header. No completed frozen capture exists. They cannot be supplied by rerunning the CPU selector or by the synthetic probe below. The historical 411,877-triangle discrepancy remains separately unresolved for the previously documented missing authority.

## CPU-only ownership reproduction

New permanent tooling is in `tools/NovaCore.Causal.Observer/offline/`. `build_frozen_ownership_probe.py` extracts the production `FrozenFrameAuthority`, `RecordFrozenCapture`, `CompleteFrozenCapture` functions and their native layout structures verbatim. The fixture includes the real `Header` and `Writer` implementation. `extraction.json` seals the source and generated-function hashes.

The fixture substitutes ordinary CPU memory, a recorder sink, debug-label stubs and local Vulkan entry points. It uses retained population/generation metadata, **not missing live geometry or pupil matrices**. Both executable import inventories show no Vulkan loader or NovaCore native runtime linkage. No renderer/device/application is launched.

**Debug: 28 checks PASS. Release: 28 checks PASS.** They establish:

1. An ineligible frame leaves both slots Free and performs no capture copies.
2. The first eligible record produces a 15-section, 1,659,412-byte payload description, six copy calls and two barriers; identity 1 belongs to slot 1 in Reserved state.
3. Completion inspection before submission leaves that reservation alone and makes no query call. Stopping with it unsubmitted publishes nothing and returns.
4. Injected Reserved, InFlight and Writing targets fail reservation immediately without overwriting ownership. Wraparound onto a writer-owned identity also fails closed, preserving both identities.
5. Wrong-frame submission cannot advance ownership. A matching simulated submission/completion advances through the real writer to a durable **explicitly synthetic** file and Free state, preserving frame/submission/completion identity.
6. An unavailable simulated query fails closed without a Vulkan WAIT query or an ownership wait loop.

Recorded single-call first-record timings were 0.0156 ms Debug / 0.0090 ms Release; stopping the unsubmitted reservation took 0.4262 / 0.5176 ms. Busy-slot refusal took 0.0015–0.0101 ms. These are structural probe observations on ordinary RAM and API substitutes, **not actual GPU readback cost or overhead qualification**. There is no live capture median/P95/P99, transfer cost or completed readback sample to report.

The probe does not reproduce the live stall. It excludes a deterministic wait in the tested production ownership transitions. It cannot qualify real mapped-memory reads, Vulkan command recording, validation/driver behavior, operating-system scheduling or hardware execution.

The previously reported general GPU-timing payload identity issue also remains: 166 samples name the legacy anchored-terrain frame field as zero. Queue/fence identities are intact, but those timing payloads are not a qualified per-frame cost join. This revision preserves that limitation; it does not silently treat the samples as frame-168 timings.

Reproduction from the repository root (all outputs isolated under `build`):

```powershell
python tools/NovaCore.Causal.Observer/classify_recording_boundary.py build/earth-blackout-closure/startup-lifecycle/live-01-distant build/earth-blackout-closure/frame168-classification
python tools/NovaCore.Causal.Observer/offline/build_frozen_ownership_probe.py build/earth-blackout-closure/frame168-classification/probe
& tools/NovaCore.Causal.Observer/offline/build_frozen_ownership_probe.cmd build/earth-blackout-closure/frame168-classification/probe Debug
& build/earth-blackout-closure/frame168-classification/probe/ownership-Debug.exe build/earth-blackout-closure/frame168-classification/probe/debug-synthetic-new
& tools/NovaCore.Causal.Observer/offline/build_frozen_ownership_probe.cmd build/earth-blackout-closure/frame168-classification/probe Release
& build/earth-blackout-closure/frame168-classification/probe/ownership-Release.exe build/earth-blackout-closure/frame168-classification/probe/release-synthetic-new
```

## Bounded next experiment — proposed, not launched

The earliest unresolved interval is the final draw call through the first capture-scheduled marker. The sealed source orders it as:

`final vkCmdDraw → vkCmdEndRenderPass → end debug label → capture entry/authority input reads → Reserve → capture debug label/query reset/timestamp → transfer barrier → six copy recordings → host barrier/timestamp → scheduled marker`

Neither the later authority fragmentation nor final `vkEndCommandBuffer` is positively reached. This narrows the preceding report's broader recording-tail list. No positive evidence yet selects the ordinary rendering tail or the diagnostic capture prefix as the responsible owner.

The smallest useful next change is **diagnostic instrumentation**, followed by offline qualification and Project Control review:

1. Add fixed-size entry/exit breadcrumbs around each operation above, including each authority input copy, the bounded catalog scan and each transfer recording. Emit capture-entry and reservation-result records before relying on the existing end-of-record marker. Do not read extra GPU memory solely to construct a diagnostic marker.
2. Include frame/command-buffer/thread identity; active/prepared/pupil generations; all slot states and capture identities; main-fence reset versus submitted state; last submitted/completed sequence. Retain resource incarnation, mapping offset/length and memory-property identity for the authority input being read. This distinguishes an input-memory read from a command-recording call without bulk geometry logging.
3. Exercise the exact marker/state protocol offline with a stalled substitute at each boundary, busy writer, unsubmitted reservation and shutdown. Confirm the observer reports the last entered/unreturned operation, retains ownership, and triggers the **same** stop deadline. Re-run diagnostic/recovery qualification in Debug and Release. Recording remains bounded/preallocated; no render-thread disk I/O or timeout extension.
4. Stop for Project Control review. Only separately authorized hardware execution could test the real mapped-memory/driver calls. Its scope would remain one lowest-risk distant-Earth first-capture attempt, with the existing immediate-stop criteria; no approach, horizon, orbit or native-resolution incident route.

The discriminating result is the first entered operation without a matching return and its ownership snapshot. A draw/render-pass call would locate ordinary renderer recording; an authority-input read or capture command would locate the diagnostic prefix. Neither alone assigns the original blackout's cause to application, driver or hardware. A complete first capture would still require measured transport cost and validation before exposure could increase.

## Preservation and remaining unknowns

This revision adds analysis/probe tooling and this report, and updates the evidence index. Production renderer, shaders, observer, timeout policy, diagnostic application binaries and prior permanent regressions are unchanged. No quality reduction or arbitrary clamp. No KSA, unrelated save, driver or system-setting changes. Final hash checks preserve the original distant retry and the eight sealed historical horizon evidence files, HEAD, index and branch/tag/remote refs. `git diff --check` passes.

Remaining unknowns: the precise interrupted CPU call and its internal cause; real first-capture transport/membership/cost; whether the diagnostic input mapping or a Vulkan recording call contributes; the original blackout cause; and the historical visibility discrepancy whose exact authority was never retained. This is a bounded classification and next experiment, **not blackout closure or authorization to relaunch**.
