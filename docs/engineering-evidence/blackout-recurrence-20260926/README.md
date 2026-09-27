# Fresh dual-monitor blackout — forensic disposition

**ESCALATE — evidence ceiling. Blackout cause UNRESOLVED; Player PASS and banking HOLD.**

Project Control reported both monitors going black during ordinary use of the current canonical Release, followed by a required restart. The accessible retained evidence does not distinguish a renderer/GPU stall, presentation/display failure, driver fault, or whole-machine failure. No production correction is justified yet. The earlier bounded native PASS remains valid only for its recorded route; it does not clear this recurrence.

No NovaCore build, application launch, GPU experiment, production/test edit, settings change, cleanup, Git staging/commit/tag/push, or banking occurred during this investigation. Three independent read-only reviewers examined GPU/lifetime, Windows, and route differences. The lead alone wrote these reports and a separate forensic output directory.

## Preservation and candidate identity

Immutable input: `E:\NovaCore\build\blackout-recurrence-20260926-193024`.
Its repaired manifest SHA-256 is `ab37a3a6bff1173f224871f4978caec19402648eea897e1f62376d74407ef04e`.
All **226 listed files / 37,471,453 bytes** matched their lengths and hashes. The only unlisted files are the repaired manifest and its digest. The original manifest's self-hashing failure is not a collection-stage failure. Nothing in the sealed tree was written or normalized.

New working evidence: `E:\NovaCore\build\blackout-recurrence-analysis-20260926-1943`.
It contains entry Git HEAD/index/refs/diffs, a 3,839-file repository inventory, current source/package inventories, derived session analysis, and additional read-only OS exports. System/Application EVTX, raw XML, and formatted records cover 21:30:00–23:46:24.9424289 UTC. Supplemental channel records and access limitations are retained separately. These unresolved inputs are KEEP; no retirement is authorized here.

| Identity | SHA-256 / value |
|---|---|
| Git HEAD, branch | `8c189b28ce2a68f97de734d1589acb500c41fd99`, `main` |
| Git index | `83aa8c801a40ba244619029a424f13401247a6e10ec307691e0254c9770a809e` |
| Frozen source, 926 files | `4c4ef09957e306c567684f59db39625851e6d4fba685037c44fd91ffb8bd1182` |
| Canonical package, 127 files | `15ec023278f85a720f838c35ff638831c576e6becd5d83a63b2a69d15a707a00` |
| `NovaCore.exe` | `4a14ff8b27d9bd4d42867b4179679f02afb9d6647b2aa07f05e74d8b59f581c8` |
| Native DLL | `8e0b9842570a40bcb9921c7d9bff536502e30a113161f3539814bfd079a84e46` |
| Simulation DLL | `d7a0069f6f60832e327619920e0ccf89fe2ed9fb6ddce320d3bdbd211a0e9057` |
| Incident log | `bdbff23fb12fbb4924d74fcadf0bd09b1a78d7c857c63477af2b243d31297d70` |

Canonical executable: `E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe`.
Every current source/package file matches the frozen [scalable-support candidate](../scalable-launch-support/identity.json); no extra package file was found. Incident startup fingerprints directly match the current Triangle DLL, native DLL and four logged shaders. Read-only PE metadata inspection matches incident App MVID `5505b9e8-e773-4c20-a18a-0ff290b1ecee`. The complete package inventory is a post-reboot preservation measurement, not a fictitious incident-time measurement of every file.

## Incident timeline

Times below are **UTC on September 26**; subtract four hours for EDT. Native log batch times are background flush times, not individual operation timestamps.

| UTC | Positive evidence |
|---|---|
| 23:12:11.6466556–23:13:01.7226524 | Application RestartManager records 38570–38571 prove OS activity after the earlier time quoted by Event 6008. |
| 23:16:12.6658652 | Canonical App starts, PID 37752; session `20260926-231612-657-37752-58a27f43f8a94cc184a9de4ff1928561`. |
| 23:16:22.6709446 batch | Solar starts at tick 843736650739490, rate 1:1; runtime identities recorded. |
| 23:18:09.6767596 batch | Construction viewport, resize. |
| 23:18:45.6721324 batch | First 21-part / 4,008 kg launch at tick 871392704305760; explicit `Craft presentation epoch/clock mismatch`, physical sequence 0. Rendering continues. |
| 23:19:04.6698741 batch | Returns to construction, resize. |
| 23:19:40.6825689 batch | Second 21-part / 4,008 kg launch at the same epoch; disposes the first failed flight. There is no retained final state for the second flight. |
| 23:19:41.6758591 batch | Last recorded swapchain recreation; near-pad scene and renewed LOD preparation. |
| 23:20:06.6826222 batch | Generation 138 / pupil 1108 publication reports fence complete, atomic boundary, 712,106 vertices, readiness 76.757 ms. |
| 23:20:09.6761442 batch | Renderer health: completed frame 33301, 3440×1322, mode 1, surface mode 2, fence 5.472 ms. |
| 23:20:12.6762088 batch | Last labelled completed GPU work: frame **33978**, generation **138**, LOD **17**. |
| 23:20:13.6719368 | Background process-memory heartbeat. |
| 23:20:16.6790693 | Last retained empty background flush. No orderly session-end record. |
| 23:21:11.5000000 | Boot start, reported by System record 28698. |
| 23:21:15.3859626 | Kernel-Power 41 record 28731: abnormal restart, zero bugcheck/power-button fields. |
| 23:21:31.6369850 | Event 6008 record 28712 quotes shutdown at **23:02:18 / 19:02:18 EDT**. This conflicts with subsequent OS and App activity; it is **not** established blackout onset. |

The actual blackout onset and last manual action were not timestamped in retained telemetry. Log cessation cannot timestamp physical display loss: GPU/CPU activity could continue while displays are black. No incident-window clock adjustment explains the 6008 discrepancy. Do not rewrite either source's timestamps.

## Last proven sequence and first unproven operation

**CPU:** the render thread successfully inspected GPU query results for frame 33978. Separately, the background logger wrote batches through 23:20:16.6790693. These are different threads/responsibilities; the latter does not prove render-loop continuation.

**GPU:** frame 33978 completed. `NovaCoreNative.cpp:2611` successfully waits the existing frame fence, then invokes query inspection at 2637–2638; pipeline statistics print only after `vkGetQueryPoolResults == VK_SUCCESS` at 2352–2354. Final counters: 1,424,208 input triangles; 387,518 TCS patches; 3,581,481 TES invocations; 89,399 clipping primitives; 258,654 fragment invocations; outer factor 64; diagnostic 0. This proves this sampled work completed, not that later work did.

**Present/display:** the last explicit present-adjacent line is serial 29281, logged after `vkQueuePresentKHR` returned, before result classification. The serialized Update→Draw→Update control flow supports a later present-return inference associated with frame 33978. Neither line nor inference proves physical scanout or that both monitors displayed that frame. No retained final VkResult, present ID/acknowledgment, or OS display trace exists.

**First unproven continuation:** the remainder of Update after the final query inspection and subsequent frame/message/present operations. There is no identified last unreturned API. **Do not label frame 33979 the stalled submission**: its admission, start, submission and completion are all unrecorded. Hidden/minimized-window suppression can also leave logger activity without rendering, falsifying the claim that empty batches alone prove GPU stall.

The final unlabelled GPU timing sample is **3.308 ms**; its counter is independent of the labelled query counter, so it is not assigned exactly to frame 33978. Across 278 retained, incomplete GPU samples: median 1.077 ms, P95 11.9143 ms, P99 13.48539 ms, max 15.350 ms (linear percentiles). These are not full-frame cadence/FPS statistics and cannot establish an incident-time performance PASS or exclude a later TDR. Final CPU frame time and CPU/GPU split are unavailable.

## Windows / AMD / hardware

Accessible evidence contains no incident-time Display 4101, AMD, WHEA-Logger, bugcheck, or watchdog fault identifying an owner. The retained System CSV's 200 entries are all post-reboot. Event 41 has BugcheckCode 0, PowerButtonTimestamp 0, LongPowerButtonPressDetected false, WHEABootErrorCount 0; it proves abnormal restart without identifying cause. [Microsoft's Event 41 guidance](https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/event-id-41-restart) explicitly documents the ambiguity of zero error fields.

Boot PnP 219 names **Meta Virtual Monitor** (`ROOT\DISPLAY\0000`) and FakerInput, not a Radeon failure. The separate Virtual Desktop Monitor reports code 22 (disabled), consistent with [Microsoft's device-code definition](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity). WHEA Operational records 340–347 repeat the exact EventData of previous-boot records 332–339; they provide no fresh fault signature. This does not establish hardware innocence.

Post-reboot DXDiag: RX 6800 XT, driver 32.0.21045.5002 / WDDM 3.2; DisplayPort 2560×1440@144 Hz SDR and 3440×1440@180 Hz HDR. This is environment identity, not incident-time health or proof of unchanged composition/overlay state.

`C:\Windows\LiveKernelReports` enumeration is **ACCESS DENIED**, contents unknown. `Minidump` is empty; `MEMORY.DMP` absent; accessible WER records concern older git/dotnet/test failures. DxgKrnl Admin/Operational metadata queries were denied; no matching records returned, which is not a reliable negative. Other relevant diagnostic channels were disabled or returned no matching events. No logging configuration or ACL was changed.

## Causal judgment and next proof

The [causal matrix and resource audit](causal-matrix.md) leave the major causal owners unresolved. The [route differential](differential.md) identifies genuine coverage differences and counterexamples. The independent [review/verification record](verification.md) rejects unsupported shortcuts.

**Next cheapest discriminating proof:** obtain an elevated **read-only** inventory of the currently inaccessible `LiveKernelReports`, then preserve any incident-relevant existing dump into a new sibling directory. This requires an operator with access; this session did not elevate or alter permissions. A matching watchdog/device dump could narrow responsibility without another NovaCore run. A negative inventory would close this access gap, not clear the driver/hardware.

If no usable dump exists, exact replay of the terminal frame is impossible from these sampled logs. Before proposing exposure, audit the existing observer's ordinary-route coverage offline: CPU callback, message/window suppression, acquire, record, submit, completion, present, generation/handle lifetime, and heartbeat. Account for observer-enabled resource-usage/extension differences; do not assume instrumentation is behavior-neutral. A bounded recoverable GPU stage would require a reviewed discriminant and separate authorization. Representative workload and ordinary manual reproduction remain later steps. No scaffold expansion or live proof was implemented here.

The first-launch clock refusal is preserved as a separate observed integration witness; which clock predicate fired remains unproven. It is not a blackout correction or authority to reopen flight warp. No accepted RCS/support/pad/construction/package/terrain work is discarded.

**STOP FOR PROJECT CONTROL.** No causal fix claimed; no Player PASS, bank, or milestone promotion.
