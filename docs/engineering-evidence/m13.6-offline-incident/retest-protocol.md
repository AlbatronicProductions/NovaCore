# One instrumented manual observation protocol — design only

**Not executed. Not ready to launch until preparation is implemented and checked. Not M13.6 acceptance clearance.** This is the single bounded fallback protocol requested by Project Control if offline evidence cannot resolve causality. Existing readable dump evidence, if later obtained, takes priority over another runtime attempt.

## Question, scope and payoff

Join the first abnormal operation, if any, to runtime/memory identity, camera/LOD/publication, live resources and actual presentation. Collect enough durable context to discriminate key-memory, workload-transition, synchronization, swapchain, device/driver and system interactions. Do not change production memory policy, frame pacing, FIFO, shaders, physical authority, terrain quality, simulation or launcher scenario.

Use one short manually controlled Florida run with an independent observer and a hard duration limit. Do not run bank/candidate back-to-back stress loops or repeat after an abnormal stop. A clean short run provides telemetry qualification only; it cannot exonerate the approximately five-minute failed session or satisfy manual acceptance by itself.

## Mandatory preparation before launch

1. Project Control reviews the remaining dump-access gap and explicitly authorizes this instrumented observation. No runtime launch occurs merely because this document exists.
2. Produce an isolated diagnostic build/launcher deployment with only bounded telemetry. Root remains the single writer. Preserve the normal deployed candidate and record exact diff, native/managed/shader hashes, memory policy and diagnostic flags. Do not add per-frame GPU readback, fences, waits or a cap; do not install a global Vulkan layer or disable validation. Instrumentation must have a bounded fixed ring and durable last-operation channel.
3. Record per-operation **begin and end** breadcrumbs with QPC, process/session ID, frame/submission serial and thread. At entry/exit of frame fence wait, acquire, record, submit, present, device-idle/recreate and teardown, record VkResult, image/swapchain/fence identity and current/incoming generation. Use a nonblocking producer ring and an independent observer that persists a fixed overwrite journal. Record separately the last emitted, observer-received and durably flushed serials; a kernel hang can prevent the final marker reaching disk. Never wait for observer acknowledgment or disk I/O on the render/submission thread. Test crash-safe partial records, ring loss, observer lag and logging backpressure with synthetic input, not a GPU hang. Report the last proven durable boundary and any gap instead of claiming that it is necessarily the last GPU operation.
4. Start an independent observer **before** NovaCore. It must not wait on the render callback or the same fence. Validate its deadline, cancellation, process targeting, log quota and graceful-stop/termination behavior using a disposable mock child. No sustained GPU test is needed for this prerequisite. The observer can request app quit and terminate the exact child if the OS still schedules it; it cannot guarantee recovery from a kernel/display-engine hang.
5. Configure the installed AMD-bundled PresentMon for this exact process and a unique session. Retain dropped-frame rows. Keep default display/GPU/input tracking enabled; use QPC timestamps and bounded duration/output. Confirm the actual emitted schema and field availability before interpreting it. Native render serials and PresentMon timing must be joined by QPC/process/swapchain context with any ambiguity reported. A `vkQueuePresentKHR` return is not display completion.
6. Verify sensor availability. If a supported AMD logging interface is accessible, validate its device identity, units, sample interval and timestamps. Otherwise mark each missing sensor **unavailable**, not zero. Available generic Windows thermal/power counter names do not prove GPU junction temperature or board power. No DLL guessing or undocumented ADL/ADLX invocation is required by this plan.
7. Establish the frame-zero manifest, baseline live-resource counters and requested/actual swapchain/display identities below. A missing mandatory breadcrumb/observer/identity stream is a no-go. Preflight records observer backlog/loss, durable-write lag and measured producer overhead under a mock workload. The initial runtime phase records instrumentation perturbation without treating this diagnostic build as an identical timing control. Optional unavailable sensors/displayed-cadence fields are explicitly listed; their absence constrains conclusions and cannot be replaced by fan noise.

No prototype build, instrumentation or observer was implemented by the offline ticket. Future preflight must prove it exists and passes its bounded checks; the current production console logging is insufficient for this protocol.

## Required evidence and ownership

| Field | Collection owner and rate | Meaning / limit |
|---|---|---|
| Frame/session identity | Native breadcrumb ring every frame; durable operation markers | Monotonic QPC plus process/frame/submission/swapchain serial, not a mutable wall-clock label |
| GPU frame time | Existing completed timestamp queries, associated with originating submission | Nonblocking availability; missing/unfinished query stays missing. Do not stall to obtain it. |
| CPU frame time | QPC frame begin/end and existing phase timing | Include fence/acquire/submit/present wait scopes; do not sum overlapping CPU/GPU intervals. |
| Render/submit cadence | Observer counts joined native completed/attempted frames and submissions over fixed intervals | Separate attempted, completed and submitted counts. |
| Acquire/present/display cadence | Native call begin/end/result plus PresentMon observer | Record dropped/undisplayed frames and available displayed timestamps; distinguish composition/independent-flip state. Missing display completion is not inferred from API returns. |
| Present mode / images | At every swapchain creation/recreation | Enumerated modes, selected mode, requested/actual image count, queue families, extent/format, handle identity |
| Display identity / refresh | Window-to-output mapping and active display configuration at start and every change | Adapter/output identity, rational refresh, physical pixels, window style/focus, active monitors. Record VRR/driver override only if actually observable. Never assume current 180/144 Hz is historical truth. |
| Temperature, power, clocks, utilization | Validated AMD sensor stream at 1 Hz where supported; Windows GPU counters for available utilization/memory at 1 Hz | Clearly label sensor/device/domain/units and unavailable fields. No thermal diagnosis from fans. |
| Camera / LOD / pupil | At transition plus periodic 1 Hz state snapshot | Canonical altitude/direction, current/desired level, pupil/domain identity and input phase |
| Incoming/publication | At request, preparation slice, readiness and completed publication | Current/incoming generation, topology identity, preparation cursors, regional demand/completion and sole-owner checks |
| M13.6 key allocation | At create/map/bind/cleanup only, plus live count | Usage/type/heap/flags/memoryTypeBits, logical and allocated bytes, return codes, host mapping/buffer/memory diagnostic IDs, map-before-bind order and fallback attempts. Do not obtain a new buffer device address just for telemetry. |
| Resource growth | Initial baseline, every allocation/free/replace and 1 Hz summary | Live totals/deltas: mapped buffers, descriptor/command/fence/semaphore/swapchain resources, topology count/bytes, current/incoming/spare/scratch, regional loaded/queued/ready/payload bytes. Distinguish cumulative events from live counts. |
| Memory pressure | Process working set/private bytes and available GPU adapter/process memory counters at 1 Hz | Heap-budget field only if existing supported query is enabled; record absent support. |
| Vulkan/device failure | Every checked result and strict validation callback | Preserve VkResult, VUID/message ID, severity, first operation and serial. Flush to observer immediately. No filter that changes errors into success. |

## One run and one automatic deadline

Normal point-and-click entry remains **Florida Launch Site / Fullscreen Native / Normal Diagnostics**, with the instrumented candidate identity visibly/logically confirmed before start. It resolves to `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. Diagnostic observation is attached to that route, not a fixed-camera performance scenario.

The independent observer enforces **180 seconds total process lifetime**, with at most 60 seconds allowed to reach the first healthy authoritative terrain frame. Once healthy, the operator follows this single sequence within the remaining budget:

1. First 45 seconds: near-surface Florida movement with active refinement; verify sole NCSM1 ownership and joined telemetry.
2. Next 45 seconds: gradual zoom out toward orbit, marking each input phase. Stop on the first anomaly; do not accelerate through a stall to reach a scripted endpoint.
3. Next 30 seconds: return toward Florida and hold a normal near-surface view if time remains.
4. Automatic end at the global deadline even if a phase is incomplete. Do not extend to five minutes to chase a failure. Record actual completed phases and elapsed time.

This deliberately shorter observation is not a clean comparison of long-run stability. Its payoff is qualified instrumentation and, if encountered, an early correlated anomaly. If no anomaly appears, return that limited result to Project Control; do not schedule another run automatically.

## Stop conditions and authority

- **Immediate stop request:** any `VK_ERROR_DEVICE_LOST`, other fatal Vulkan result, strict validation error, physical/publication/sole-owner failure or invalid live-resource invariant. Persist the triggering record before requesting stop where execution allows it.
- **Severe completed-frame stall:** after healthy terrain begins, one CPU frame or observed render/present operation taking at least 500 ms, or three consecutive CPU frames at least 250 ms. These are conservative operational stop thresholds, not a TDR setting or a causal classifier. Record individual times and phase.
- **Lost progress:** independent observer sees no renderer heartbeat or no completed render/present progress for 1 second after healthy rendering. Request graceful quit once. If the exact child remains alive without progress for another 2 seconds, attempt termination once; do not enter repeated restart/kill loops. A responsive but intentionally minimized/suspended window is still reason to end this observation, not a reported GPU defect.
- **Thermal/power:** stop at a verified manufacturer/firmware limit, a reported thermal/power fault, or sustained sensor abnormality against a predeclared supported limit. Preflight records the exact source, sensor and threshold; this plan does not invent RX 6800 XT thermal/power limits. If sensors are unavailable, automated thermal protection is unproven and must be reported as such.
- **Visible freeze/black screen or operator concern:** stop interaction immediately. Use the observer's stop path if the OS responds. Do not instruct repeated forced resets or re-entry into the failing view. If the system becomes unresponsive, the observer cannot promise control; preserve the existing logs and Windows evidence after recovery.
- **Quota/deadline/observer failure:** end the observation. A dead observer, unusable disk destination, lost mandatory stream, ring overrun or more than 1 second of durable-journal lag must never silently turn this into another uninstrumented run. The stop threshold limits usable evidence loss; it does not guarantee final-marker persistence if the kernel stops scheduling the observer.

No stop criterion changes rendering cadence, simulation dt, geometry quality or Windows TDR/driver settings. No proposed cap is part of this protocol.

## Bounded evidence and follow-up judgment

Predeclare temporary output budget **64 MiB**: two preallocated 4 MiB breadcrumb slots, at most 32 MiB PresentMon CSV and at most 24 MiB combined scalar/sensor/system summaries. A quota boundary causes orderly stop instead of unlimited output. No raw frames, D32/HDR dumps, screenshot/video archive or full ETL capture is part of this protocol. The installed WPR profiles are optional future tools, not a second tracing plan enabled here.

Consolidate to at most **4 MiB** permanent report/provenance/last-operation and representative scalar evidence after the run, without purging unresolved unique fault evidence merely to hit the target. Preserve unexpected original Windows dumps in place. Any larger irreplaceable incident output requires an explicit retention reason and Project Control disposition; do not move bulk bytes into Git by default.

Return actual startup/phase completion, stop reason, runtime identity, memory policy, live counts, first/last anomalous serial, exact available metrics and unknowns. A driver-module association alone is not an application-cause finding. A successful 180-second observation does not clear the earlier five-minute freeze. M13.6 remains unbanked until its physical acceptance and causal-clearance bar is met.
