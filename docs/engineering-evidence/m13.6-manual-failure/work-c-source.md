# Work C — current KSA presentation and transition responsibilities

This is a source/IL comparison for the M13.6 manual-failure investigation. It does not classify the host freeze, establish a displayed frame rate, or propose a production correction. The user withdrew the inferred 200–270 FPS observation; fan noise and an approximately five-minute run are not frame-rate or thermal measurements. The lead's completed official Discord chronology is retained separately in `ksa-history.md` and supplies the historical reasons and their limits.

## Current installation and inspection provenance

The live `E:/Kitten Space Agency/KSA.dll` is still **2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400**, SHA-256 `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`. Relevant retained KSA decompilation fingerprints and its production assembly version remain consistent with the previous package; none were rewritten. `GameSettings.cs` and `App.cs` were additionally fingerprinted, and their decisive present-mode/main-loop methods were verified directly against the live DLL's IL.

The missing dependency owners were inspected directly in the live installed assemblies with .NET `PEReader`/`MetadataReader`, without loading or executing KSA types, downloading a decompiler, compiling a project, or running either application:

- `Planet.Render.Core.dll`, product `1.0.0+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256 `6b9b3c3bd709bf198c6ab3ec6daaef7e468f593fb110187869a1d44272b92e3c`.
- `Planet.Core.dll`, same product revision, SHA-256 `2823b299051b91252453acb4f1fff5543b1f5473bc6f42c8d51d4958d3159ece`.

The companion JSON records file hashes, exact method tokens/RVAs/IL hashes, bounded call-reference search results and the read-only IL inspection helper. Method tokens refer to these exact assembly hashes; they are not stable identifiers across arbitrary KSA versions. No proprietary source or full IL dump was added to this package.

## Presentation policy and buffering

`GameSettings` defaults to `Vsync=true`. `GetPresentMode` requests **FIFO** with VSync and **Immediate** without it (`GameSettings.cs:1264`, `:2947`; live token `0x06000b39`). `Core.Renderer.CreateSwapchain` enumerates available modes, uses the requested mode when present, and falls back to FIFO otherwise. No mailbox selection appears in this policy. This establishes the selection algorithm, not the actual mode of a running KSA process.

`Core.Renderer` initializes `MaxFramesInFlight=2` (constructor `0x06000111`). Rebuild requests `MaxFramesInFlight+1`, hence three swapchain images before surface clamping. `CreateSwapchain` clamps that request between the surface minimum and maximum; when maximum is zero, this implementation uses minimum+1 as its upper clamp. The actual returned image count is queried and logged. Neither requested three nor two frame slots proves an actual image count on this machine.

Two frame synchronization slots own acquire semaphores, command buffers and fences. `TryAcquireNextFrame` rotates the slot, waits its fence with an all-ones/infinite timeout, resets it, and acquires the next image with an infinite timeout. Separately, `ImageReadyForPresentSemaphores` is allocated per **actual swapchain image**. Submission signals the entry for the acquired image; present waits that entry. `PresentFrame` submits one image through the present queue. There is no CPU present-wait/present-ID or display-timing chain in this inspected submission method.

The lead's Work B independently verified NovaCore already uses FIFO, one frame fence/acquire semaphore and per-image `renderFinished` semaphores. Its image semaphore ownership therefore aligns with KSA. KSA is not evidence that NovaCore needs a FIFO conversion or a semaphore-ownership correction. Actual acquire, render, present and display cadence remain separate measurements.

## Frame limiter: configured UI is not executed ownership

KSA's settings show a configurable FPS limit when VSync is off, disabled as `DISPLAY` when VSync is on. The slider range is 1–240 and values at least 240 display `Unlimited` (`GameSettings.cs:1940–1965`). `ApplyTo` and `Program.RebuildRenderer` write `Core.Time.FrameLimit` when VSync is off.

However, the live dependency's `Core.Time.Update` is the location containing the explicit limiter: for a positive limit, it calculates the remaining interval and calls `Thread.Sleep` with integer milliseconds. Its class initializer sets the field to 200. The inspected production main loop, `KSA.App.Run`, directly calls `OnFrame`; it does not call `Time.Update` or sleep.

The bounded reachability check found:

- KSA.dll: 16,706 method bodies inspected; only the two `FrameLimit` writes, no reference/call to `Time.Update`.
- Planet.Render.Core.dll: 919 bodies; no `Core.Time` reference.
- Planet.Core.dll: 107 bodies; no internal call to `Time.Update`.
- All 35 installed `KSA`/`Planet.*`/`Brutal.*` managed product assemblies: no additional `Core.Time` member reference.

Thus **an effective explicit KSA gameplay FPS limiter is not established by this current path**. The UI label must not be promoted into a measured or source-proven cap, and the dormant helper must not be copied as a proven production pacing design. This is bounded static evidence, not an exhaustive claim against reflection, future modules, external driver policy, or uninspected application paths. It also does not establish that VSync-on gameplay runs uncapped: FIFO and finite image availability remain its demonstrated presentation boundary.

No explicit benchmark-only present-mode override or VRR-specific pacing logic was found in the inspected settings, main loop and renderer methods. VSync-off requests Immediate, with FIFO fallback if unavailable; the settings expose an unlimited choice, but this is not proof of a separate benchmark mode. No KSA runtime or monitor configuration was sampled.

## Refresh and fullscreen ownership

KSA enumerates monitor video modes, builds supported refresh options and obtains the primary monitor's current refresh (`GameSettings.cs:2674–2695`, `:2811–2848`). Fullscreen with an unset refresh takes the monitor's reported rate and passes it to GLFW fullscreen setup. This is display-mode configuration; it is not a displayed-cadence measurement or a hard-coded 144 Hz cap. The inspected path uses the primary monitor, so it must not be cited as proof of perfect active-monitor or VRR handling.

The current renderer also has a Windows fullscreen-exclusive owner. Acquire checks focus/exclusive-mode state and can rebuild; `ModeLost` recognizes the fullscreen-exclusive-loss result specifically. This is a distinct result from Vulkan device loss. No source evidence here establishes a cause for NovaCore's fullscreen black-screen event.

## Rebuild, failure and long-running lifetime boundaries

`Core.Renderer.Rebuild` waits device idle, waits for a nonzero/available window, releases exclusive state, creates a replacement swapchain with the old swapchain supplied, recreates image semaphores/resources/sync objects and destroys superseded handles. The application then refreshes dependent targets centrally, acknowledges the rebuild, and waits idle (`Program.cs:4903–5009`). Fixed-size frame-slot arrays and explicit replacement/destruction are visible; no long-duration live allocation counts were collected.

Acquire/present handle out-of-date, suboptimal and recognized fullscreen-exclusive-loss cases through rebuild. Other exceptions propagate; there is no device-lost recovery success path in these inspected methods. This supports keeping real Vulkan failure observable, not reclassifying a driver/device failure as an ordinary resize. `Renderer.Dispose` itself does not begin with an idle wait: the normal application caller, `Program.Dispose`, first waits device idle, disposes the streaming uploader and dependent resources, then calls renderer disposal (`Program.cs:1362–1482`). This establishes the inspected normal shutdown ordering, not successful recovery after a device/driver failure or every exceptional exit.

## Narrow zoom/terrain transition boundary

The current frame-phase split corroborates the lead's April 9, 2026 revision-4034 history finding. `PrepareFrame` waits/applies prior solver jobs, applies queued input and schedules the next jobs (`Program.cs:2092–2159`). `OnFrame` then prepares/draws ImGui before its controller/celestial work and separate `OnPreRender`, `Render`, `PostRender` calls (`:2162–2279`). This verifies surviving ownership boundaries; it does not reproduce the historical camera-stutter diagnosis or show a GPU-hang cause. The June 7, 2025 minimum-target/time-compression history is a simulation-workload responsibility, not evidence of a maximum render-FPS limiter. The May 7, 2025 uniform fix supports checking each buffer's actual slot/consumer lifetime, not assuming host caching or high CPU cadence is intrinsically invalid. Exact historical records and links remain in `ksa-history.md`; no expanded physics audit was performed.

`PlanetRenderer.OnFrame` derives mesh LOD from altitude and ticks texture demand using the camera (`PlanetRenderer.cs:2143–2155`). `GenerateMeshData` selects the resident topology and per-frame descriptor/buffer slot, then records prepare/modifier/finalize/normal compute work with barriers (`:1770–1939`). Program submits the compute pool and makes graphics submission wait its completion semaphore (`Program.cs:2365`, `:2263–2267`). This is an explicit work-completion relationship, not evidence that KSA has NovaCore's exact current/incoming pupil publication contract.

The celestial streamer derives requested height/diffuse/normal mip detail from angular size, FOV and viewport height; resolves budget; limits simultaneous promotion and promotion steps; and does not reset a map's allocation while its upload is in flight (`CelestialTextureStreamer.cs:445–505`, `:648–664`, `:747–825`). The uploader distinguishes transfer/graphics queue ownership and establishes transfer-to-graphics acquire synchronization. Completion polling requires the batch gate, invokes publication callbacks, then releases batch resources (`StreamingUploader.cs:114–157`, `:240–311`).

Publication creates a view only over completed resident mips and advances content version. Demotion returns to the retained coarse representation, retiring the former view/handle/image before reuse (`CelestialTextureStreamer.cs:828–883`). `RetiredResourceQueue` allows reclamation after its frame-age rule **or** after all frame fences report complete (`RetiredResourceQueue.cs:116–132`); these are alternatives, not a requirement that both pass. Do not import that age rule or coarse-height fallback into NovaCore's stronger exact physical-data/publication contract without qualification.

## Responsibility comparison

Historical chronology is kept in the lead's `ksa-history.md`; this table compares the surviving current responsibilities without duplicating that record.

| Responsibility | NovaCore current, verified by lead/Work B | KSA current source/IL | Classification and concrete boundary |
| --- | --- | --- | --- |
| VSync presentation | FIFO already selected | FIFO default; Immediate option with supported-mode/FIFO fallback | **ADOPT — already aligned** for normal FIFO ownership. No pacing change follows from source alone. |
| Present semaphore lifetime | Per acquired image | Per acquired image, separate from frame-slot fences | **ADOPT — already aligned**. Preserve it; do not invent a missing correction. |
| Frames in flight | One fence also gates mapped readback and publication work | Two frame slots; requested three images before clamping | **INTENTIONALLY DIFFER**: NovaCore's current shared/readback/publication lifetime is qualified against its fence. Adding another live slot requires separate ownership proof, not copying a count. |
| User cap / unlimited policy | No explicit limiter identified by Work B; benchmark currently shares FIFO | Settings expose cap/unlimited, but effective sleep helper is not reachable through inspected normal path | **ADAPT** policy only if a measured NovaCore need emerges. Do not adopt dormant limiter code or claim that KSA proves cap efficacy. |
| Refresh / VRR | Actual active display must be measured | Monitor refresh config; no explicit VRR policy proven here | **ADAPT** capability-aware configuration if needed. No 144 FPS constant, no unproven VRR guarantee. |
| Swapchain replacement and device errors | Existing centralized recreate/failure reporting | Idle/rebuild dependency updates; narrow recoverable results; other failures propagate | **ADOPT** the responsibility boundary; no NovaCore defect or required change established. |
| Terrain replacement / retirement | Exact physical authority and complete regional dependencies before publication | Completed mip publication, coarse fallback, frame/fence retirement | **INTENTIONALLY DIFFER** on authoritative completeness/fallback; **ADAPT** explicit completion/retirement ownership. Do not weaken H or readiness. |
| Simulation versus pacing | Deterministic simulation contract must remain independent | App clamps player dt separately; unused general Time helper couples update callbacks and sleep | **INTENTIONALLY DIFFER**: retain NovaCore's deterministic authority; any future pacing belongs outside it. |

**Work C judgment:** no source-based freeze classification, no source-based proof of unnecessary rendering, and no justified production correction. The strongest useful reference is explicit, bounded presentation/resource ownership. The strongest caveat is that KSA's visible limiter settings do not prove an executed limiter.

## Storage and scope

Only this report and its compact JSON were created. No GPU captures, application runs, builds, source changes, downloads, deletion, Git mutation or old-evidence rewrite occurred in this subtask. Full IL remained transient in tool memory; the permanent package retains hashes, method identities, the inspection recipe and concise findings. Disposable files created by this subtask: zero. No memory-derived timing or architectural result is used.
