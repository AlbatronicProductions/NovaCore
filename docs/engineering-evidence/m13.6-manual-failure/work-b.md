# Work B: NovaCore presentation and zoom/resource lifecycle

Read-only audit for the M13.6 manual failure. No production edits, builds, GPU runs, Windows-event scans or KSA searches were performed by this worker. The earlier sealed M13 exit package is unchanged.

Project Control's later clarification supersedes the original FPS estimate: there was **no measured FPS reading**. The run was approximately five minutes using **Florida Launch Site / Fullscreen Native / Normal Diagnostics**. The lead's display API inspection reports the primary ASRock PG34QRT2B at3440x1440 /180 Hz on the RX6800XT, and a secondary display at2560x1440 /144 Hz. This worker did not repeat that display inspection; its provenance belongs to the lead's combined evidence.

## Result and boundary

NovaCore already requests **FIFO** presentation and serializes rendering through **one frame fence**. There is no explicit FPS limiter, refresh query or separate unlimited present mode, but that does **not** prove that normal presentation is uncapped or wastes frames. Available modes, actual swapchain image count, active display timing and useful/displayed cadence are not logged by the production source. The first required measurement is effective presentation behavior, not a presupposed 144 FPS cap.

**Source-only freeze classification: INSUFFICIENT EVIDENCE.** The three-file candidate changes one memory-placement preference and its tests. It preserves the presentation, polling, publication and lifetime implementation. That narrows the causal dependency; it does not clear the candidate of a driver/cache-path or timing-mediated interaction.

**Source-only over-rendering classification: INSUFFICIENT EVIDENCE.** Reciprocal GPU query time is capacity, not displayed cadence. Earlier short-run CPU averages also do not establish the display state or discarded-frame count during this manual failure.

## Baseline and candidate

- HEAD and origin/main: `d4baab6940a57a46e478b98e36f5e45d1c4b558f`.
- Branch: `codex/m13-final-exit`; candidate is unbanked.
- Tracked difference: `MappedBufferMemory.h`, `MappedBufferMemoryTests.cpp`, `NovaCoreNative.cpp`; 36 insertions and 6 deletions at audit start. Source hashes and the exact diff are in `work-b.json`.
- Native changes: `CreateTerrainResidency` selects `MappedBufferUse::TerrainRequestKeys`; `CreateHostBuffer` admits that role to the existing preference/fallback allocator. No shader or managed source difference from the bank is present.
- Existing candidate proof records a 393,216-byte key buffer moving from compatible memory type1/flags6 to type3/flags14 on this device. Both are host-visible and host-coherent; the latter adds host caching. These are retained-run identities, not a fresh reading of the failed process.

## Current presentation map

| Responsibility | Current implementation | Evidence / limitation |
|---|---|---|
| Selected mode | `VK_PRESENT_MODE_FIFO_KHR` unconditionally | `NovaCoreNative.cpp:1230-1261`; no runtime mode override or benchmark branch |
| Available modes | Not enumerated | No `vkGetPhysicalDeviceSurfacePresentModesKHR` call in native renderer; FIFO is the required portable mode |
| Requested images | `min(minImageCount + 1, maxImageCount or UINT32_MAX)` | `:1241-1247`; actual count subsequently queried into `a.images` |
| Actual images | Driver-returned count `n` | `:1265-1271`; not included in current swapchain log |
| Rendering frames in flight | At most one graphics submission pending before host update/mutation | One `a.fence`, waited at every `Update`; not a claim of only one presentation-engine queued image |
| Acquire | Infinite timeout, one `imageAvailable` binary semaphore | `:2214-2228`; out-of-date recreates without resetting the fence; suboptimal requests recreation |
| Submit | One command buffer for acquired image; wait at color-attachment-output; signal `renderFinished[image]` and frame fence | `:2238-2247`; compute and earlier pipeline stages can execute before the acquisition wait point |
| Present | Same image, waits `renderFinished[image]` | `:2248-2261`; queue families may coincide or differ; distinct families use concurrent swapchain sharing |
| Command/semaphore ownership | One command buffer and one present semaphore per actual swapchain image; one acquire semaphore | `Commands`, `:1893-1916`; reuse is image-indexed |
| CPU loop | Pump Win32 messages, `Update(dt)`, `Draw`, repeat | `:2503-2518`; no explicit sleep/refresh target in the normal loop |
| Refresh / display | No active-monitor refresh, display-timing, present-ID/wait or VRR policy | Device enables only `VK_KHR_swapchain` (`:935-940`); no display timing extensions |
| Window mode | Win32 overlapped window or environment-selected borderless `WS_POPUP` at `(0,0)` | `Window`, `:823-844`; no exclusive-fullscreen/display-mode change; borderless is not proof of direct scanout |
| Benchmark | `--benchmark-frames` counts callbacks then posts quit | `Program.cs:229`; no pacing bypass. Traversal flags collect diagnostics but still call the same FIFO swapchain path |
| Simulation | Native passes elapsed host-frame duration; managed scene advances its existing deterministic simulation clock | `:2512-2515`, `Program.cs:216-219`; existing `.1s` host-duration clamp remains. No refresh constant belongs in simulation authority |

FIFO queues presentation requests in order and processes one on each applicable vertical blank. This is materially different from mailbox replacement or immediate presentation. The finite swapchain should provide backpressure under ordinary FIFO operation, but the actual application/driver/compositor/display behavior must be measured. Present-return time is not display completion. [Khronos present-mode specification](https://docs.vulkan.org/refpages/latest/refpages/source/VkPresentModeKHR.html)

The current startup log prints format, colorspace, extent and HDR format, but not selected/available modes, requested/actual images, monitor, refresh or driver version (`:1264`, `:923-925`). It cannot establish over-rendering or reconcile a hypothetical rendered-FPS figure with the active display configuration. The user clarified that no FPS measurement was taken, so the original200-270 estimate is not performance evidence.

## Zoom-out responsibility chain

1. `WM_MOUSEWHEEL` accumulates Win32 wheel delta (`NovaCoreNative.cpp:586-594`). After the prior frame fence, `Update` consumes whole detents and calls the managed callback (`:2340-2399`).
2. Florida/Solar input enters `SolarSystemScene.ApplyPresentationInput` (`:578-674`), computes canonical surface altitude, retained visual aim and surface-anchor handoff, then updates camera distance/pose. Surface-relative mode has its own camera input branch. No Vulkan resource is allocated by the camera policy.
3. The callback rebuilds final camera/presentation constants and calls `UpdateProductionBillboard` (`Program.cs:219-228`). Earth eligibility depends on the focused body's identity/terrain contract, not an altitude cutoff (`Program.cs:278-279`, `SolarSystemScene.cs:385-393`). Zooming outward while Earth stays focused does not inherently retire NCSM1 or select another Earth owner.
4. The selector computes desired density from altitude, FOV and viewport; it uses Schmitt thresholds and adjacent L0-L17 replacements. It does not reverse a transaction already in flight (`PlanetaryProductionSphericalBillboardRuntime.cs:49-113`). A large zoom step can therefore cause a sequence of adjacent transactions, not unlimited concurrent generations.
5. Managed runtime has current plus one preparing/ready/submitted transaction. It may adjust the current pupil as direction changes; a same-level pupil refresh does not upload another immutable topology. Physical preparation for the accepted NCSM1 route is native GPU work (`MovingRuntime.cs:134-248`).
6. Native receives one new generation, reuses or creates its immutable topology and acquires one incoming work set (`NovaCoreNative.cpp:1646-1657`). Regional dependency/preparation state retains the published pupil while data/preparation is incomplete. A topology replacement supersedes an unpublished pupil refresh (`RegionalPhysicalPreparation.inl:22-61`).
7. Incoming preparation is sliced to at most65,536 vertices per dispatch; dependency demand is at most32,768 vertices per slot/dispatch. Only the final complete incoming slice records final cull/compact and arms publication (`RegionalPhysicalPreparation.inl:64-86`). Current rendering/culling continue each display frame.
8. At the next completed frame fence, readiness and generation/geometry counters are checked. Current work is retained as one spare, incoming becomes current, descriptors are rewritten and the acknowledged generation is returned to managed code (`NovaCoreNative.cpp:2309-2338`). Managed pinned topology/submission leases are disposed after that acknowledgment (`Program.cs:234,338-349`).

Zoom therefore changes workload size, density, pupil preparation, possible first-use topology allocation and publication/retirement timing. It does **not directly call** swapchain recreation, key-buffer reallocation or regional-cache destruction. A coincident resize/display event is a separate path that requires evidence.

## Static resource bounds and retained observations

| Resource | Lifetime / bound | What can grow |
|---|---|---|
| Frame fence / acquire semaphore | One each for renderer lifetime | No per-frame accumulation |
| Swapchain images, views, framebuffers, commands, present semaphores | Actual swapchain image count | Recreated on resize/out-of-date, with prior objects destroyed |
| Descriptor pool / set | One pool, `maxSets=1` | Rebuilt on swapchain recreation; no per-frame set allocation |
| Immutable NCSM1 topology | At most18 resources, two buffers each | First visits may increase retained topology count; reuse by hash/family/count; exceeds18 throws |
| Terrain mutable work | Current plus incoming **or** spare; at most two five-buffer work sets | Spare is moved or destroyed before incoming allocation; allocation counters are cumulative events, not live counts |
| Same-level pupil scratch | One extra physical buffer, capacity high-water | Can grow to required current vertex capacity; swap with published physical buffer after fence |
| Regional jobs | Two fixed dependency states; current/incoming preparation states | One physical replacement is active; no unbounded job list |
| Regional payload | At most896 records, 124,895,232 bytes; current catalog859 records allocates119,737,728 bytes once needed | Monotonic renderer-lifetime residency, no zoom-out eviction/reallocation loop |
| Regional I/O | One worker; each record requested at most once; ready capacity8; upload budget8/frame | Pending request deque can reach the finite catalog count; completed ancillary payload vectors are released |
| Global material/elevation residency | 256 payload slots, 512 lookup entries, 8,192 key slots; production upload budget2 | Complete126-record L0-L2 hierarchy bootstrapped before first presentation; no normal production eviction policy |
| Managed preparation / leases | One task/ready/submitted transaction; one incoming pinned lease and one persistent frame lease | Topology set bounded by18 supplied levels; task records reclaimed by GC, not an expanding history list |
| Normal timing records | Native two8,192-element rings; managed timing windows2,048 entries | Fixed storage; shutdown copies are transient |
| Traversal diagnostic vectors | Grow while diagnostic bit2 is enabled | Bounded by the explicitly finite traversal, not the normal flags1 route; inspect actual launch flags before treating these as a long-run cause |

Sources: `NovaCoreNative.cpp:39-49,262,384-490,1604-1643,1753-1760,1893-1916,2035-2074`; `RegionalPhysicalResidency.h:14-16,62-95`; `RegionalPhysicalResidency.inl:80-96`; `MovingRuntime.cs:64-90,206-286,299-324`.

Retained **short controls**, not the failed manual run:

- Original/candidate full traversal:58 publications and18 topology uploads each. Thus40 further publications reused those18 topology identities.
- Original/candidate regional traversal:37 publications and10 topology uploads each; both loaded670 contributing records, requested670 once, allocated119,737,728 bytes and uploaded93,392,640 bytes. Queue high-water634, ready capacity8, upload budget8.
- Warp:1 publication, no regional records loaded,4-byte regional placeholder retained.
- Full-route regional demand differs between old and final runs (old loaded8 records; final0). That reinforces that whole-route timing is not an exact geographic input join.
- Detailed resource-residency lines with live work allocations/bytes were not retained in these journals. No measured long-runtime trend in total GPU allocations, handles, descriptors, process working set or driver heap budget can be invented from the static bounds or cumulative retirement counter.

`deferredRetirements` is a completed-publication counter, not a live list of retained generations. The immutable topology/library and bounded regional residency deliberately remain after zoom-out; retention alone is not a leak.

## Candidate causal dependency and synchronization challenge

The new role requires compatible `HOST_VISIBLE | HOST_COHERENT` memory and prefers `HOST_CACHED`. The unchanged allocator falls back only for allocation/map capacity failures; device-lost and unknown results remain fatal. The key buffer is still allocated once, mapped persistently, zero-initialized, bound at the same descriptor and destroyed with terrain residency. Its size, API consumers, two scans per frame, writes and upload bookkeeping are unchanged (`MappedBufferMemory.h:11-36`, `NovaCoreNative.cpp:1552-1576,1588-1602`).

**Do not overstate coherence:** a fence wait establishes execution completion, not by itself the visibility of every GPU write to the host. Host coherence removes explicit flush/invalidate requirements for the applicable host-domain operations; it does not eliminate memory-dependency requirements. [Khronos synchronization specification](https://docs.vulkan.org/spec/latest/chapters/synchronization.html)

The exact key-field distinction matters here. In the normal production hierarchy, host code seeds words0-7 and publishes words10-11. `QueueProductionRequests` reads those fields; `planetary_select.comp` production code writes words8-9 for frame/transition state, which that poller does not consume. After NCSM1 authority, the alternate `gpuPlanetary` selector is not dispatched (`NovaCoreNative.cpp:1124-1136,1593-1596,1949-1955`; `planetary_select.comp:109-145`). The non-production shader branches can write other key words, but the production I/O poller returns when no production pack exists. No newly introduced GPU-readback visibility defect is demonstrated on the failing Earth path by this audit.

Plausible dependencies that source alone cannot reject:

- The legal cached/coherent memory type exercises a different driver/cache path. Automated output checks do not prove that path cannot fail after long interactive execution.
- The measured CPU scan reduction changes frame cadence and the elapsed-time spacing of otherwise unchanged GPU, I/O and publication work. It can expose a pre-existing workload or host/driver condition without adding code to that subsystem.
- A first visit to a different LOD may allocate a previously unseen topology or larger work/scratch buffer. Different timing can change when that bounded allocation meets driver heap pressure. The tiny key-buffer size alone neither proves nor disproves that interaction.

No source evidence currently establishes any of those as the manual freeze cause. No production correction follows from this audit alone.

## Pre-existing failure-site limits

- `Recreate` pumps messages until nonzero client dimensions without sleeping or reacting to `WM_QUIT` itself, then calls `vkDeviceWaitIdle`. This can busy-wait while minimized. It is not the ordinary wheel-zoom path (`:2035-2048`).
- `vkDeviceWaitIdle` in recreation/destruction and `vkResetFences` / `vkResetCommandBuffer` have unchecked return values. Most subsequent API operations are checked, and failures throw/log, but these sites lack precise result telemetry (`:2048,2229,2265,1941`).
- The frame fence and image acquisition use infinite waits. If a driver never progresses, the current log may have no final completed operation. No independent watchdog or durable per-operation breadcrumb exists in production.
- Existing short-run final-candidate full traversal had one retained early post-authority CPU frame8 at50.4425 ms, dominated by48.4895 ms fence wait while completed GPU7 was.982 ms. That observation was already reported and remains unclassified; it is not evidence of the later system freeze or of a50 ms terrain GPU command.

These are narrow observability/possible failure sites. They are not a finding that the M13.6 diff introduced a synchronization defect or authorization to modernize unrelated APIs.

## Minimum runtime telemetry needed

Before one safe bounded comparison, the lead should capture:

1. Runtime/shader/asset fingerprint and exact route/environment; selected key memory type, flags, heap, allocation bytes and compatible-type mask.
2. At every swapchain creation: selected and enumerated present modes; capability min/max/requested/actual image count; graphics/present queue family; extent/format; generation of the swapchain.
3. Window-to-monitor mapping, active refresh numerator/denominator and resolution, borderless/windowed state and display changes. OS refresh is not proof of VRR operation or actual scanout rate.
4. Monotonic before/after breadcrumbs for fence wait, acquire, record, submit, present and recreation, including VkResult, image index, command/fence/submission serial. Use bounded rings plus periodic durable summaries, not unlimited frame archives.
5. Distinguish submitted/rendered cadence from actual presentation/display completion. Prefer a supported display-timing/present observer if available; plain `vkQueuePresentKHR` return counts cannot measure displayed frames. GPU query reciprocal remains separate.
6. At zoom/LOD/pupil/publication events: actual camera altitude/direction, current/incoming generation and pupil, topology hashes, preparation cursors/readiness, submitted geometry counters and the existing sole-owner checks.
7. Periodic live resource/heap summaries: topology count/bytes; current/incoming/spare/scratch capacities and handles; allocation/free totals with live difference; regional loaded/queued/ready counts; command/descriptor/swapchain counts; process and driver memory budget/usage. Cumulative allocation or publication counters alone do not prove growth.
8. If safely available, utilization, temperature, clocks/power and driver/device failure data from the other worker's channel. Do not infer thermal failure from fans or add a cap to conceal device loss.

A cheap source control must preserve the distinction between normal FIFO, any separately justified paced diagnostic, and a deliberately explicit unlimited benchmark mode. The current benchmark flag does not provide the latter. No mechanism should be chosen until effective pacing and the KSA comparison are available. Do not hard-code144 Hz or alter simulation time.

## Worker judgment

No causal production defect or pacing correction is proven by this source audit. The strongest red-team challenge is the missing link between requested FIFO and actual displayed cadence, together with the absence of a durable last-operation/resource trace at the manual failure. M13.6 remains unbanked and the manual acceptance blocker remains open for the lead's combined evidence review.

The missing mode enumeration and image count should remain **not measured**. The telemetry list identifies evidence that would discriminate a future safe control; it does not require another GPU run now or establish that a short probe could clear this hard-hang event. With no measured over-rendering, a pacing change is not justified by this audit.

Retained output: this report plus `work-b.json` with source hashes, exact candidate diff, resource bounds and compact prior-control provenance. No disposable output was created. No prior evidence package was modified.
