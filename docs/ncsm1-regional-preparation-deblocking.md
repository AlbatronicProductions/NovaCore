# NCSM1 regional preparation de-blocking candidate

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

2026-09-04. **P3 RESOLVED — READY FOR CONTACT-ARCHITECTURE WORK**, subject to
website-side review and manual acceptance. Route convergence remains **unbanked**.
The separate 0.692 m contact residual remains unresolved and unchanged.

## Cause and corrected responsibility

The corrected regional preparation dispatched every L17 physical vertex in the
graphics command buffer immediately before the outgoing draw. Its measured
36.995 ms was GPU timestamp duration, not command recording, allocation, CPU
sampling, or a disguised queue-idle call. `Draw` submitted that buffer with the
normal frame fence. The next `Update` waited on that fence before input and the
managed callback; it therefore waited for full incoming preparation as well as
the outgoing draw. Keeping the outgoing owner valid did not prevent the stall.

The render and main loop are the same native thread. Warm-return recording was
0.307/0.361 ms, submission 0.066/0.057 ms, and the following fence wait
68.422/65.256 ms. Regional requests, disk access, verification, decode and upload
were **zero new work** on those retained-data returns. The dataset was already
complete. The synchronous responsibility was the placement of the complete GPU
dispatch ahead of the frame fence, not the regional worker.

The correction retains the existing queue and frame fence, with no additional
queue, device-idle call, or full-preparation wait:

1. Regional geographic demand visits at most 32,768 vertices per slot per frame,
   including the same normal-support directions. Masks accumulate across slices.
   Each completed slice may start deduplicated asynchronous I/O; readiness still
   requires a complete footprint and every selected record, or the existing
   proof that every potentially contributing catalog record is resident.
2. An incoming job freezes its pupil and generation. After regional readiness,
   at most 65,536 complete physical vertices are prepared per frame. Each vertex
   uses the unchanged canonical base and normal functions. No incomplete result
   is eligible for drawing, culling or publication.
3. Only the last slice can record incoming culling, compaction and indirect draw
   state. Only its completed frame fence can satisfy atomic publication. The
   outgoing generation continues to draw through earlier slices.
4. Pupil-only updates use one staging physical buffer and fixed previous/target
   pupils. Exact unchanged directions copy their already-complete prior physical
   vertex; changed directions are evaluated once. The published buffer remains
   immutable until the final slice's fence, when buffer and pupil swap together.
   Culling for that pupil precedes its first draw. A topology incoming transaction
   supersedes an unpublished pupil refresh; there is never a second incoming
   generation. Subsequent movement is coalesced into the next pupil job.

The bounded render-thread responsibilities remain resource/descriptor ownership,
host-coherent copies of at most eight ready records per frame, recording a slice,
submission, completed-fence inspection, and publication. File read, digest checks
and payload preparation remain on the existing background residency worker. No
CPU physical sampling was moved: the production physical evaluator already ran
on the GPU and now runs in bounded slices. Readiness latency is intentionally
separate from foreground blocking.

## Before and after: equivalent warm Florida L17 returns

Debug native 3440×1440, AMD Radeon RX 6800 XT, the same 800-frame `regional`
scenario, physical generation 4, NCSM1 L17 topology and 712,106 physical vertices,
670 complete regional records, and factor-1 settled Florida terrain. Timing
builds omit only expensive diagnostic geometry readback. Actual production
readback is validated separately. Both returns are measured; these are small
samples, not invented percentiles or scanout measurements.

| Metric | Correct regional path before | Staged correction |
| --- | ---: | ---: |
| Total L17 physical GPU preparation, median / max | 36.995 / 37.196 ms | 44.567 / 45.182 ms |
| Largest warm physical GPU slice | 37.196 ms, one dispatch | 4.521 ms, 11 slices |
| Publication frame, median / max | 71.699 / 73.107 ms | 37.026 / 38.229 ms |
| Publication main/render fence wait, median / max | 66.839 / 68.422 ms | 32.499 / 33.748 ms |
| Warm readiness interval, each return | 69.537 / 66.415 ms | 408.671 / 403.192 ms |
| Preparation-affected frame median / max, first return | full wait on publication frame | 34.498 / 38.229 ms |
| Preparation-affected frame median / max, second return | full wait on publication frame | 33.948 / 35.824 ms |
| Retained frames 760–799, median / max | 30.899 / 33.146 ms, n=40 | 30.851 / 32.997 ms, n=40 |

The earlier incomplete-preparation control's 43.410 ms publication median is
historical context, not a correctness target or a whole-application baseline.
The new total GPU time is higher because slicing incurs repeated dispatch and
cache costs; the benefit is a bounded foreground contribution. Ordinary frame
fence waiting remains. This is not a claim of zero render-thread blocking.

Before, generations 19 and 37 submitted at frames 297 and 677 and published at
298 and 678. After, their slices run at 357–367 and 737–747, publishing at 368
and 748. Owners 18 and 36 remain unchanged throughout their incoming slices.
The readiness endpoints differ slightly: before is native staging to inspection;
after is the staging frame's start through the publication fence (a conservative
interval including that frame's host work). It is not GPU-only latency.

Cold entry: 22 demand dispatches, demand GPU median 1.527 ms / max 5.972 ms;
dependency readiness 1,364.794 ms; 414.326 ms accumulated record read/decode/verify
wall time on the worker. Physical preparation takes 11 slices / 42.297 ms total
GPU time. First publication is frame 103, 20.980 ms frame time; native incoming
readiness is about 1,556 ms. Before, cold publication was frame 86 with a
56.369 ms frame. Startup before the first NCSM1 owner is distinguished from
replacement of an established owner. Pupil preparation slices in the new run
have median 0.275 ms / max 3.679 ms; they also avoid a whole-buffer frame stall.

### Thread and stage audit

A second pair of 400-frame diagnostic runs added Windows thread CPU accounting
without changing preparation. They reproduced publication frames of 71.978 ms
before / 38.072 ms after. The native thread IDs were 24740 / 32108 respectively;
each run used one thread for update, recording, submission and publication.

| Stage on the warm L17 return | Thread and synchronization | Measured wall time before / after | Foreground responsibility |
| --- | --- | --- | --- |
| Managed request/envelope/metadata callback | Main; existing coordinator handoff | 1.537 / 1.366 ms for the staging-frame callback | Bounded host work; no full CPU physical mesh |
| Geographic dependency determination | Main completeness/identity checks; GPU demand only if needed | Included in validation/update below; no warm demand dispatch | Warm catalog checks; cold demand now sliced |
| Residency request, disk/cache, record decode/verification | Residency worker; mutex and condition variable | No new warm requests, reads or decodes in either run | Worker can sleep while current draws; cold work remains asynchronous |
| CPU regional lookup/physical sampling | Canonical physical evaluator executes on GPU | No CPU per-vertex preparation on this production path | No foreground mesh sampling |
| Validation, resource acquisition, host updates/uploads | Main, after frame fence | 2.300 / 2.066 ms, staging-frame aggregate | Warm work-buffer/topology reuse; no regional payload copy or allocation on these returns |
| Command recording | Main; no preparation wait | 0.383 / 0.385 ms, first preparation frame | Records one full dispatch before; a bounded slice after |
| Physical positions/normals | GPU compute on graphics queue | Primary two-return measurements above | Must complete before publication; current draws between slices |
| Queue submission | Main; `vkQueueSubmit` with frame fence | 0.061 / 0.075 ms, first preparation frame | Does not wait for completion |
| Fence wait on publication frame | Main; `vkWaitForFences` | 66.405 / 33.650 ms | Before includes full preparation; after includes only last slice and ordinary rendering |
| Readiness inspection/publication | Main after fence | 0.194 / 0.162 ms, whole inspection stage | Atomic current/incoming exchange; no disk or mesh generation |
| Present API on first preparation frame | Main; render-finished semaphore | 0.563 / 0.426 ms | API duration, not scanout latency |

CPU-active time was instrumented with `GetThreadTimes` at every native stage.
On this machine it advances in 15.625 ms accounting ticks: individual callbacks,
recording and even whole publication updates often report zero ticks, and a
0.383 ms recording can receive one 15.625 ms tick. Those readings cannot resolve
per-stage active time or justify subtracting active from wall time to invent an
exact wait duration. The measured fence-call wall duration is the foreground
blocking metric; active-time and non-fence wait subdivisions remain unresolved.
Cold record read, verification and payload extraction share the existing worker
timer, so their individual CPU/I/O subdivisions are likewise not separately
claimed. Raw CPU samples, including these limitations, are retained in
`before-cpu.log`, `after-cpu.log` and `cpu-timings.json` under the evidence directory.

## Synchronization, reuse and bounds

GPU demand/physical results are inspected after the normal frame fence. Host
catalog, payload and control writes cannot race an outstanding submission.
Host-to-compute barriers expose uploaded residuals; compute barriers order
physical writes before culling and later draw reads. Frame snapshots cannot be
overwritten mid-job. Incoming physical range completion is an explicit part of
the existing readiness check in addition to the fence, topology, regional,
normal, culling, compaction, indirect and generation checks.

The hot path has no `vkQueueWaitIdle` or `vkDeviceWaitIdle`. Existing calls remain
in initialization, swapchain recreation, compatibility capacity growth and
shutdown, outside this warm-return chain. No full host buffer copy is introduced
for pupil staging. There is no speculative global-only physical preparation,
second normal evaluation for a completed vertex, repeated warm regional upload,
or authoritative cache of incomplete vertices. Current culling still runs each
frame because camera visibility changes; incoming culling runs once at completion.

The existing immutable body/terrain/record digest/range/physical-generation
identity, bounded I/O queue, eight-result ready capacity, eight-record upload
budget and renderer-lifetime residency are preserved. Florida retains 670
records, 93,392,640 residual bytes, in the 119,737,728-byte bounded payload.
No records are evicted or reloaded during pupil, LOD or body-focus returns.
The extra pupil buffer is bounded by the resident topology vertex capacity
(45,574,784 bytes at L17); it is reused/swapped, not allocated each pupil update.
It is an unpublished same-generation refresh, not another incoming topology.
Control is 512 bytes; demand storage is 576 bytes. The P2S5G vertex/tessellation
interface and managed/native ABI are unchanged; two compute-only bindings carry
staging control and output.

## Validation and scope

The production live regression passed cold Florida, retained operation, lateral
pupil motion, L8–L17 transitions, leave/re-entry, Mars/Saturn focus and Earth
return: **43 captures, 891 checked vertices, 40 pupil identities**. Exact regional
contribution is still 15.001533002687239 m on both CPU and GPU; composed H differs
by the existing 0.663416 mm global-oracle UV precision difference. Prepared base
vertices pass the unchanged 3 mm tolerance. All 670 required records are resident
before affected publication; anchored-patch demand is zero. A separate outside
Florida Earth entry requests five records; fresh non-Earth focus requests zero.

The regression now verifies contiguous bounded slices, immutable outgoing owner,
full completion before every incoming publication, and sufficient frames with
an incomplete incoming job and a retained owner. Structural checks reject a full
regional dispatch, early completion, synchronous preparation waits, premature
pupil swaps, or partial-demand readiness. Re-entry parity checks the actual
completed L17 publication after frame 740 rather than imposing the old timing.
There were zero missing-owner, overlap or stale-generation failures and no device
loss. The pre-existing external Win32 import
`VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645` remains; no new Vulkan VUID was
observed. This is not a completely validation-clean run.

Changes for this ticket are limited to `NovaCoreNative.cpp`,
`RegionalPhysicalResidency.inl`, new `RegionalPhysicalPreparation.inl`, both
production physical prepare shaders, both regional demand shaders,
`RegionalPhysicalResidencyTests.cs`, the P2S5C snapshot structural assertion in
`PlanetaryProductionSphericalBillboardRuntimeTests.cs`, and these active
investigation documents.
Canonical physical formulas, residual composition, topology, routes, pad and
foundation, 50 m TES behavior and P2S5G interfaces were not changed. The earlier
convergence candidate is preserved. No commit, push, tag or banking is authorized.

Evidence: `build/regional-preparation-deblocking/{initial-*,runtime.log,frames.json,
timings.json,live-test-final.log}`; live parity evidence in
`build/regional-live-tests/287bebea183648228b19905c88138185/`. Historical before
timings remain in `build/structure-contact-review/`. Runtime traces and native
timing builds are ignored diagnostic artifacts, not production modes.

Automated results establish the P3 scheduling improvement. Manual terrain
stability, input feel and the coherence of delayed pupil publication still
require website-side review and physical acceptance. Do not bank convergence or
begin contact implementation or renderer retirement as part of this ticket.

## Final automated checks and working tree

Debug and Release native builds pass. The Debug sample and Graphics test build
pass. The live regional regression, native regional identity/readiness/cache
unit, Earth-route convergence, Florida foundation seating, anchored Florida,
P2S5C production runtime, compiled P2S5G interface, body eligibility/transition
ownership and canonical physical-authority checks all pass. The initial P2S5C
source assertion expected a mutable per-frame incoming pupil; it was updated to
require the fixed preparation snapshot and the new structural scheduling gates,
then the complete P2S5C test passed.

`git diff --check`: **PASS, exit 0**. HEAD remains
`7c41bb5d0317dc48290b359727031988145abb09`; the index is unchanged/empty.
The starting snapshot contains 436 source/document/configuration files. Ten
existing files changed for this ticket and two files were added; the other 426
are byte-for-byte preserved, with none missing. The combined status below
includes earlier authorized, unbanked work: 21 modified and 20 untracked files.
No staging, commit, push, tag or banking occurred.

Final `git status --short`:

```text
 M README.md
 M docs/CODEX_HANDOFF.md
 M docs/M12D-P2S5G-workload-investigation.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/architecture.md
 M docs/planetary-rendering.md
 M native/NovaCore.Native/CMakeLists.txt
 M native/NovaCore.Native/NovaCoreNative.cpp
 M native/NovaCore.Native/shaders/planetary_physical_authority.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_incoming_prepare.comp
 M native/NovaCore.Native/shaders/production_spherical_billboard_prepare.comp
 M samples/NovaCore.Triangle/NovaCore.Triangle.csproj
 M samples/NovaCore.Triangle/Program.cs
 M src/NovaCore.Graphics/FloridaLaunchSite.cs
 M src/NovaCore.Graphics/PlanetaryPhysicalSurface.cs
 M src/NovaCore.Graphics/RenderObject.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/PlanetaryProductionSphericalBillboardRuntimeTests.cs
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tests/NovaCore.Launcher.Tests/Program.cs
 M tools/NovaCore.Launcher/ScenarioCatalog.cs
?? docs/earth-route-convergence.md
?? docs/florida-generation4-seating.md
?? docs/florida-physical-rendered-agreement.md
?? docs/ncsm1-regional-physical-residency.md
?? docs/ncsm1-regional-preparation-deblocking.md
?? docs/ncsm1-regional-residency-ksa-reference.md
?? native/NovaCore.Native/RegionalPhysicalPreparation.inl
?? native/NovaCore.Native/RegionalPhysicalProbe.inl
?? native/NovaCore.Native/RegionalPhysicalResidency.h
?? native/NovaCore.Native/RegionalPhysicalResidency.inl
?? native/NovaCore.Native/RegionalPhysicalResidencyTests.cpp
?? native/NovaCore.Native/shaders/regional_physical.glsl
?? native/NovaCore.Native/shaders/regional_physical_demand.comp
?? native/NovaCore.Native/shaders/regional_physical_incoming_demand.comp
?? samples/NovaCore.Triangle/EarthRoutePolicy.cs
?? samples/NovaCore.Triangle/EarthRouteValidation.cs
?? samples/NovaCore.Triangle/SampleOptions.cs
?? tests/NovaCore.Graphics.Tests/EarthRouteConvergenceTests.cs
?? tests/NovaCore.Graphics.Tests/FloridaFoundationSeatingTests.cs
?? tests/NovaCore.Graphics.Tests/RegionalPhysicalResidencyTests.cs
```
