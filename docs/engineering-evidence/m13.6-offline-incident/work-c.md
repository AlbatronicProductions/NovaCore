# Work C — installed instrumentation and bounded retest design

Offline interface/source review, 2026-09-08. **No retest was run or authorized by this worker.** No renderer, GPU workload, trace session, sensor collection, build, download, Windows-event query, browser research or production edit was performed. Help, provider catalogs and performance-counter definitions were queried; these are not incident measurements. Only this report and its JSON companion were written.

## Result and limits

The installed AMD package includes a usable **PresentMon command-line interface**. Windows supplies ETW and GPU performance-counter interfaces. A supported, usable AMD temperature/clock/power/fan logging stream is **not yet established**. Existing NovaCore startup and publication logs are useful but do not preserve durable, continuous last-operation breadcrumbs across a system hang. A future retest therefore needs a bounded private diagnostic recorder and a separately supervised collection preflight; it must not be presented as ready merely because the binaries exist.

The incident remains the lead's offline classification responsibility. This report does not attribute or exonerate M13.6, predict a freeze, or infer FPS/temperature from fan noise. A short successful run would not establish five-minute stability or satisfy the failed manual acceptance gate.

## Current narrow KSA reference

The three current installed assemblies were rehashed and exactly match the prior source/IL review:

| Current local file | Bytes | SHA-256 |
| --- | ---: | --- |
| `E:/Kitten Space Agency/KSA.dll` | 4,798,552 | `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f` |
| `E:/Kitten Space Agency/Planet.Render.Core.dll` | 274,008 | `6b9b3c3bd709bf198c6ab3ec6daaef7e468f593fb110187869a1d44272b92e3c` |
| `E:/Kitten Space Agency/Planet.Core.dll` | 34,904 | `2823b299051b91252453acb4f1fff5543b1f5473bc6f42c8d51d4958d3159ece` |

KSA is version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`. Existing `../m13.6-manual-failure/work-c-source.md/json` retain the precise method tokens/IL hashes, dependency inventory and decompilation provenance. Those records were read, not rewritten. Matching the installed assemblies permits reuse of that narrow static evidence; it is not a new runtime observation.

- **Presentation is already aligned:** KSA defaults to FIFO, with a separately requested Immediate option and supported-mode/FIFO fallback. NovaCore already selects FIFO. Both have present semaphores owned by the acquired swapchain image. Neither fact proves the failed process's actual displayed cadence.
- **Frame ownership intentionally differs:** KSA has two frame slots; NovaCore's one fence also qualifies shared mapped-state and physical publication work. KSA's slot count does not authorize increasing NovaCore concurrency. Actual image count is separate from requested count and frames in flight.
- **Recoverable swapchain conditions are narrow:** inspected KSA acquire/present paths rebuild for out-of-date, suboptimal and recognized fullscreen-exclusive loss. Other failures propagate; there is no demonstrated successful device-lost recovery path. `Program.Dispose` waits idle before streaming/dependent disposal and renderer disposal. This is normal-path ownership, not proof that shutdown succeeds after a driver failure.
- **Terrain completion and retirement are explicit:** completed mip views are published after uploader completion; former resources retire through a frame-age rule **or** completion of all frame fences. That coarse-fallback/age policy must not replace NovaCore's exact physical authority, regional completeness and current/incoming publication contract.

The already reviewed official history supports checking the ownership boundaries, not adopting an unmeasured pacing fix: [Windows FIFO decision, 2025-04-24](https://discord.com/channels/1260011486735241329/1260112103134724146/1364835073962016849), [compute recording and frame-fence ownership, 2025-06-09](https://discord.com/channels/1260011486735241329/1260112103134724146/1381810057213116548), [frames-in-flight/image-count separation, 2026-03-18](https://discord.com/channels/1260011486735241329/1260112103134724146/1484009179244007424). The [Bandicam swapchain-destruction interception investigation, 2025-08-19](https://discord.com/channels/1260011486735241329/1260112103134724146/1407214631092752455) is historical external-interception context, not evidence that an overlay caused this incident. The prior `ksa-history.md` retains the reviewed record context. No new history search was needed.

## Installed diagnostic interfaces

| Interface | Verified locally without collection | What remains unproved |
| --- | --- | --- |
| `C:/Program Files/AMD/CNext/CNext/PresentMon-x64.exe` | 396,560 bytes; SHA `8b035dc90c7d664ec3dcf4a8713566e29a30622d58a7b650acb9b5d91bdd0227`; help identifies `HEAD 6af680f45c64574f03ae05e2a02684e35d978065` | Live ETW session permission, delivered events, actual CSV header and field availability, complete hard-hang tail |
| PresentMon targeting/output | `--process_id`, `--process_name`, `--output_file`, `--qpc_time`, distinct `--session_name` | Correct PID/start-time correlation must be established for the launched runtime, not launcher PID |
| PresentMon bounds | `--timed`, `--terminate_after_timed`, `--terminate_on_proc_exit`; default tracking includes display/GPU/input unless disabled | No byte-limit option found in this help; an outside supervisor must enforce output limits |
| Windows Performance Recorder | `C:/Windows/System32/wpr.exe`; profile catalog includes GPU, DesktopComposition, Power, Thermal | Profile names do not prove GPU sensor coverage, capture permission, useful event delivery or bounded disk behavior |
| ETW provider catalog | `Microsoft-Windows-DxgKrnl`, GUID `{802EC45A-1E99-4B83-9920-87C98277BA9D}`; GPU scheduler, memory, resource, present, driver and diagnostic keywords available | Provider registration is not a historical fault observation or a proven minimal decoder recipe |
| `logman create trace -?` | Supports `bincirc`, QPC clock, flush timer, buffer count/size, max file MB, run duration and session-scoped operation | No trace started; a selected provider/keyword recipe still needs qualification before relying on its decoded contents |
| GPU counter definitions | GPU Engine utilization/running time; GPU Adapter Memory and GPU Process Memory usage/commit sets | No values or instances sampled. Matching the renderer PID and adapter LUID, and avoiding double-counting engines, remain necessary |
| Generic Thermal Zone / Power Meter definitions | Temperature/throttle and power fields exist in Windows' counter catalog | They were not identified as RX 6800 XT edge/junction temperature or board power sensors |
| Installed AMD sensor interfaces | `RadeonSoftware.exe` and `atiadlxx.dll` / `amdadlx64.dll` are present | No supported CLI/export invocation or sensor stream was established. Loading an ADL/ADLX library or reading unrelated thermal zones is not a substitute |

`RadeonSoftware.exe` has file version `10,01,02,2068`, product string beginning `25.10.45.05`; this is installed-file metadata, not the incident's loaded driver identity. ADL is `7.25.10.1590`; ADLX is `1.4.0.121`. No standalone HWiNFO, GPU-Z, CapFrameX, Afterburner, uProf or amd-smi was found in the bounded PATH, AMD/Windows Kits, matching Program Files, Downloads, repository build/tools searches. This is not an exhaustive disk absence claim. No new tool was installed or downloaded, and AMD software was not launched.

## Existing NovaCore evidence and missing breadcrumbs

| Current source responsibility | Available evidence | Limit for a hard hang |
| --- | --- | --- |
| `Program.cs:182-187`, native startup | Runtime/native/shader/terrain identities, path/CWD | Normal console output is not a crash-durable journal; failed-run allocation identity still needs explicit capture |
| `Logging.cs`; `Program.cs:214` | Startup, Vulkan, renderer, validation, camera categories; callback writes console output | Category selection alone does not create a flushed on-disk sink |
| `NovaCoreNative.cpp:503-509`, validation callback near `:610` | Native diagnostic callback, failing VkResult, strict validation identity/object/module detail | A call that never returns cannot emit its failure result; no validation message is not evidence of valid execution |
| Swapchain creation near `:1264` | Format, color space, extent and HDR | Actual selected present mode, image count, window/display mapping and refresh should be captured explicitly |
| Camera log in `Program.cs:315` | Approximately once-per-second camera state when enabled | Not a same-frame snapshot of publication, input and GPU submission |
| NCSM1 transitions near native `:2336-2338` | Generation/level/topology, count/bytes, allocation/reuse/readiness and owner checks | Transition logs are not continuous live-object trend or before/after wait/submit/present breadcrumbs |
| Native initial submission trace near `:2255` | Bounded startup submission detail | Not a persistent heartbeat |
| Native frame/shutdown summaries near `:2529-2543` | Completed timing and pacing aggregates | Shutdown may not run; aggregates do not identify the last operation |

These line references refer to the current candidate audited in `work-b.md/json`. The exact M13.6 allocation, field ownership, map-before-bind difference, persistent lifetime and independent regional-resource bounds are recorded there; this worker does not duplicate or reinterpret them. The normal launcher does not redirect stdout into a durable evidence file. A private diagnostic recorder must preserve normal launcher route semantics and clearly identify its instrumented runtime.

## Inputs to the one proposed protocol, held for Project Control

The lead's `retest-protocol.md` is the single protocol authority. The following interface and measurement inputs support it; they do not define a second alternative run. It is not an executable authorization, performance acceptance or an automatic sequence of baseline/candidate retries. The lead must first complete the available offline incident classification and choose the one source/deployment variant to test. Do not run a changed candidate simply because this protocol exists.

### Preconditions

1. Record exact runtime PID, process creation time, executable/native/shader/asset hashes, source/diff identity and chosen variant. Record loaded driver/adapter identity and the actual HWND/display mapping; do not reuse postboot desktop refresh as failed-window proof.
2. Use the normal **Florida Launch Site / Fullscreen Native / Normal Diagnostics** workflow. Do not change quality, tessellation, scene semantics, FPS policy, TDR settings, driver settings or global layer registration. Inventory the effective layer/module environment; do not silently replace the ambient compatibility condition with canonical test isolation.
3. Qualify the installed PresentMon target/session permission and actual CSV schema. Preserve dropped presents. Distinguish present calls, render completion and actual display events; leave missing fields unavailable. An empty file or missing display fields cannot become a display-rate result.
4. Supply a bounded **private** native breadcrumb ring and an independent CPU supervisor. Record before/after wait, acquire, record, submit, present and recreation, including serial/result and identities below. Do not redirect or patch production behavior as an incidental tooling shortcut. The lead owns any diagnostic implementation and review.
5. Verify a named GPU sensor/export stream if temperature, clocks, board power, fan RPM or thermal-throttle state are required as stop telemetry. None is currently qualified by this review. If those are a required safety gate, the run remains held until available; generic thermal-zone values cannot fill the gap. No universal safe GPU temperature or clock threshold is invented here.

### Measured fields and correlation

| Stream | Required measurements | Correlation/interpretation rule |
| --- | --- | --- |
| Identity once, changes immediately | Run GUID; source/variant; runtime/native/shader/asset hashes; process start/PID; loaded modules/layers/driver; adapter LUID; key allocation logical/required bytes, selected memory type/flags/heap, buffer/memory handles and map result | Handles/pointers are run-local identities, not comparable GPU physical addresses. Failure to retrieve an actual GPU address is explicit |
| Startup and swapchain transitions | HWND/display path, refresh rational, window mode/extent, supported and selected present mode, actual image count, frame-slot count, queue families; acquire/present results | Requested mode/image count or monitor desktop maximum does not prove actual scanout behavior |
| Private per-operation breadcrumbs | QPC; frame/submission serial; before/after operation; VkResult; acquired image index; fence state; current/incoming generation, pupil and topology; regional readiness; owner status | Record the last entered call as well as the last completed call. CPU and delayed GPU timestamps must share the correct submission identity |
| Resource transitions and 1 Hz trend | Current/incoming/spare work bytes; resident topology/scratch bytes; regional payload/queue/ready bytes; process private/working-set commit; GPU memory usage | Count live objects/bytes separately from cumulative allocations, uploads and publication count |
| PresentMon | QPC-tagged present, display/drop, CPU/GPU timing and input fields actually delivered by this build | Join PID/start time plus swapchain/QPC/serial where available; do not substitute reciprocal GPU duration for displayed FPS |
| Windows counters, 1 Hz | Per-PID/per-adapter memory; per-engine running/utilization; available system commit | Do not sum overlapping engines into an invented total GPU utilization |
| Qualified AMD sensor stream, if available | Sensor name, unit, adapter, sampling time; temperature/clock/power/fan/throttle values delivered | Unavailable remains unavailable; fan noise is not telemetry. A qualified warning/limit can be an abort condition, not retroactive cause proof |
| Independent supervisor | Own QPC heartbeat, renderer progress heartbeat, last flush, file sizes, stop requests/results | Its continued heartbeat distinguishes renderer stall from supervisor stall only while the OS still schedules it |

Record QPC frequency and a UTC/QPC anchor in metadata. Preserve first/last sample times, lost-event/sample counters when supplied, and clock uncertainties. The lead protocol bounds two preallocated 4 MiB breadcrumb slots. Copy/flush bounded batches through the independent observer; preserve last-entered and last-completed operations, immediate best-effort error/transition records, and journal-lag/overrun counters. Avoid synchronous per-frame disk flushing. Instrumentation overhead disqualifies this run as a clean performance comparison.

### Duration, collection and stop

Use exactly the lead's one-run protocol: **180 seconds total process lifetime**, including at most 60 seconds to reach healthy authoritative terrain. Within the remaining budget, the action phases are up to 45 seconds near Florida, 45 seconds gradual zoom out, then 30 seconds return/hold. Record actual phase/QPC/camera state; these phases are not proof of identical camera replay.

A PresentMon configuration should use the verified runtime PID, a unique owned session, QPC timestamps, a new current-ticket CSV path, `--timed 180`, `--terminate_after_timed`, `--terminate_on_proc_exit`, and optionally `--no_console_stats`. The exact start offset must be recorded: its duration begins with collection, not necessarily process startup. The independent observer enforces the earlier absolute process deadline. No command or collector was run here. Default display/GPU/input tracking remains enabled, and `--exclude_dropped` is omitted. Do not stop or terminate an unrelated ETW session.

The lead's stop policy uses immediate fatal-Vulkan/validation/physical-owner stops; one completed operation at least 500 ms or three consecutive CPU frames at least 250 ms; **one second** without rendering heartbeat/completion; required-stream loss, observer/journal failure, quota/deadline, qualified sensor warning, or user concern. These are conservative abort criteria, not TDR settings or a causal classification. After a single graceful stop request, the observer may attempt termination once after a further two seconds only for the exact approved PID/start-time/path and only while the OS responds. The observer must first be checked against a mock child, without a GPU workload.

Do not add simultaneous broad WPR/GPUView collection or full ETL capture. The installed ETW interfaces are catalogued as available tools, not a second collection plan. Do not change quality, FPS/present policy, TDR timeouts or global driver/layer state. There is no automatic retry, reboot, GPU reset or repeated termination loop.

**This cannot guarantee avoidance of a hard hang.** A kernel/driver/system stall can prevent the observer, termination and disk flush from running. Terminating a process does not guarantee immediate cancellation of GPU work. Missing tail data must remain missing; a stopped recording is not proof that the GPU was safely stopped. Project Control must decide whether the offline findings and remaining telemetry gaps permit this bounded run. A short pass does not certify the approximately five-minute incident condition.

### Storage and completion

The lead's temporary ceiling is **64 MiB total**: two preallocated 4 MiB breadcrumb slots, at most 32 MiB PresentMon CSV and 24 MiB combined scalar/sensor/system summaries. Its observer enforces the quota; duration alone is not a file-size guarantee. Ring overruns, lost events and sample drops must be counted; output or journal-lag failures trigger stop. There are no screenshots, raw frames, HDR/D32 attachments, full ETL archives or geometry readbacks in this protocol. Do not reuse prior evidence or scratch paths.

After a reviewed run, retain the summary, input/runtime identities, relevant small event/breadcrumb slices, actual sensor availability and reproduction recipe within the **current 4 MiB evidence-package budget**. Do not delete unresolved incident evidence merely to meet a budget; stop and report the exact overage if a specific irreplaceable item is necessary. This worker created no temporary capture output and has nothing to purge.

## Read-only reproduction of this inventory

File identity can be repeated with `Get-Item` and `Get-FileHash -Algorithm SHA256` on the exact paths in JSON. Interface checks used PresentMon `-help`, `wpr -profiles`, `logman query providers Microsoft-Windows-DxgKrnl`, `logman create trace -?`, `typeperf -?`, and `Get-Counter -ListSet` for the named sets. These commands do not start recording; help-only exit codes are not failed captures. No prescription here permits querying the incident event history, protected dump, launching KSA or NovaCore, or starting the proposed protocol without the lead's review.

**Conclusion:** the narrow KSA boundary is already relevant and unchanged; no KSA-derived production correction follows. Instrumentation interfaces exist, but effective sensor coverage, live trace permissions/schema and crash-durable private breadcrumbs remain preflight gaps. This is retest design input only, not permission to reproduce the incident.
