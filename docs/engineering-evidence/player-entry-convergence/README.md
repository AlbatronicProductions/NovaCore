# Player entry and full viewport convergence

**Bounded engineering PASS; Project Control visual acceptance pending. Unbanked.**
Entry: clean canonical `E:\NovaCore`, HEAD
`40314c0f72396ea5f4ff39121f0b6821f1f96146` (M16.1), 2026-09-29.
Project Control retains visual acceptance and banking. No flight time-rate,
staging, autopilot or planetary-fidelity implementation belongs to this front.

## Cheap ownership proof

| Responsibility | Current owner | Bounded adaptation |
|---|---|---|
| Window/display | `DesktopEditorForm`, `ScenarioCatalog` | Full client borderless startup; versioned player settings; safe real resize only. |
| Render/presentation extent | native child HWND -> `Swap` -> framebuffer/viewport | Native host fills client. Overlay visibility must never change its bounds. |
| UI composition | WinForms layered child controls | Floating chrome and modal backdrop over full native target; target stability proven below. |
| Commands/input | native `Proc` / polling, managed preprocess, scene commands | One command catalogue and context predicate, explicit modal ownership, release gating. |
| Pause | `IApplicationPresentation.Paused`, `SuspendLive`, Solar clock | UI pause holds admission independently of existing user pause/rate. Preserve persistent engine command. |
| Loading | managed preparation then native `RunRenderer` | Real stage notifications; no synthetic progress. Preserve the synchronous same-thread HWND/buffer lease. |
| Spawn/settings | `PlayerConfiguration` and retained validated craft bytes | Replaces historical hardcoded Solar overview/unversioned player selection; bounded supported values validated before Start. |
| HUD | player status and Solar presentation | Presentation-only anchors, safe areas, visibility and layout persistence; genuine observations only. |

The 46-pixel navigation and 72-pixel status docks explain the historical
3440x1322 renderer. Changing only the outer Form would preserve that defect.
Native setup requires parent HWND and renderer on the same thread. Existing
swapchain recreation waits for device/frozen ownership completion; this lifecycle
is preserved. A first-frame callback cannot truthfully report earlier initialization.

## Current references

Video: `E:\Videos\2026-09-28 21-45-06.mp4`, 3440x1440, 81.578 seconds,
SHA-256 `8b4b9e2c21683ae231c8ef9f48527ae21368bdccf4b62cfca383459e7f4d6638`.
Local temporary timestamp captures are under `build/player-entry-convergence/reference`.
Inspected configuration 2s; loading 5/10s; File 38s; View 41s; HUD 44s;
flight/exploration pause 48s; construction 68s. Compact dark panels, monospaced
typography, narrow full-width top bar, grouped rows and centered modal hierarchy
are required. These are reference observations, not candidate acceptance.

Installed KSA is **2026.9.22.5482**, product commit
`40faabcc2a54baeb11b7b9f21beade6f7e6b524e`. DLL SHA-256
`ce43d022e7dac9beb2352b6106164bc1f521dda1ae9d0b898b8dba0c59a69723`.
Read-only current metadata/selected IL inspection established:

- ADOPT: `GameSettings.ApplyTo` owns window mode independently of camera mode.
- ADOPT: `Program.RenderGame` composes scene then UI; main viewport differs from
  secondary texture-in-window viewports. ADAPT the platform composition mechanism,
  conditional on full-target/native-underlay proof.
- ADOPT: configuration precedes expensive session preparation; loading reports
  active scoped work and measured device-local usage/budget, including unavailable.
- ADOPT: command dispatch respects modal/text ownership, while pause is a separate
  policy. ADAPT to NovaCore's existing host-admission pause without rate restoration
  that could overwrite a user pause.
- ADOPT: context-sensitive save document and HUD canvas/default ownership.
- Shift+C is one `InputAction.CameraMode`, dispatched to `NextCameraMode`; current
  installed cycle is Orbit -> Free -> IVA -> Orbit. The repeated menu labels do not
  authorize three bindings or require implementing unavailable NovaCore cameras.

Authenticated official live-changelog was read on 2026-09-29 through revision 5523.
Focused `in:live-changelog menu` search corroborates:
[4996](https://discord.com/channels/1260011486735241329/1260112103134724146/1529724186421887067)
contextual Esc/save menus;
[5007](https://discord.com/channels/1260011486735241329/1260112103134724146/1530066697766174813)
Esc modal dismissal;
[5047](https://discord.com/channels/1260011486735241329/1260112103134724146/1531399502144077997)
modal Settings;
[5202](https://discord.com/channels/1260011486735241329/1260112103134724146/1535160509769191456)
persisted HUD context assignments;
[5334](https://discord.com/channels/1260011486735241329/1260112103134724146/1539912056747069504)
refusal of game saves in editor context.
Later [5517](https://discord.com/channels/1260011486735241329/1260112103134724146/1554361002294906951)
changes KSA's Speed availability again, superseding 5497; it is later than the
installed build and grants no NovaCore physical-warp qualification.

No KSA source, assets, fonts or runtime dependencies are imported.

## Proof and stop rules

Prove full parent/host/child/swapchain/framebuffer/viewport extents, stable identities
through UI show/hide, moving world visible through alpha composition, menu input
isolation, and actual display resize. Then complete the coherent interface.
Layered-child composition needs a Windows compatibility manifest and cannot itself
be the input lock. A fully transparent top bar cannot own the reveal hotspot.

Independent verifier completed the ownership/input/telemetry attack and recommends
bounded engineering acceptance. Project Control retains final visual acceptance.
No blackout/display-loss/system anomaly was observed. The stop rule remains in
force. **150 FPS remains OPEN.** No commit, tag, push, bank or milestone assignment.

## Final screen flow and capability boundary

`NovaCore.exe` -> fullscreen configuration -> validated START -> separate fullscreen
loading with real tasks and memory -> genuinely ready gameplay. The later Project
Control placement instruction supersedes the earlier request for configuration
memory display. Configuration has no live memory block, timestamp, API name or
implementation details. Internal adapter discovery remains owned before START.

| Reference responsibility | NovaCore result |
|---|---|
| Grouped dark configuration, amber Start | 600-pixel centered panel, monospace labels, dependent game/situation/vehicle/location choices, explicit unsupported graphics rows. |
| Post-Start loading screen (video 5/10s) | Full charcoal background, compact centered stage, memory, active task and three prior real stage entries. No percentage, fabricated KSA subsystem or artificial delay. |
| Narrow top menus | Hover-revealed File/Universe/View/HUD; same catalogue owns menu/keyboard action, state and refusal. Full native extent remains underneath. |
| Contextual pause | Flight/exploration and construction have different document actions; Esc dismisses the owned layer; Resume retains prior user pause/rate. |
| HUD layout | Real game time and flight status, safe-area anchors, named versioned layouts and default persistence. No invented navball/altitude/resource readings. |

Enabled: exploration/construction/flight startup from a saved craft; actual window/resolution
settings; craft and flight save/load in their supported contexts; editor/flight
switching; celestial focus, existing Orbit/Map views; frame interval/target display;
telemetry/time visibility and anchors. Physical flight exposes pause/1x only.
Exploration retains its qualified rate ladder through the existing time owner.
Shift+C dispatches one available-camera cycle. Space is unbound in gameplay and
reserved for future staging; text/UI Space retains normal ownership.

Disabled with reasons: unqualified physical rates, Auto Warp, crew/roster/manifest,
planner/track/resources/staging/profiler/thread panels, Free/additional cameras,
unavailable orbit presentation toggles, autopilot/control/instrument panels and
custom HUD contexts. Existing terrain residency remains automatic. No new texture,
shadow, light, streaming-budget, simulation or gameplay backend was implemented.

## KSA memory ownership trace and bounded adaptation

Read-only selected IL from installed 5482 established this actual path; API leads
were not assumed to be KSA implementation:

`ConfigOnStartPopup.OnDrawUi` (0600056D, reported usage/budget label) and
`Loading.DrawUi` (06001204, VRAM label) -> `Program.GetRenderer` (06001DF3,
static renderer) -> inherited `Core.KSADeviceContextEx.GetDeviceLocalMemoryUsage`
(0600019F) -> renderer Device.PhysicalDevice ->
`Brutal.Vulkan.VkInstanceExtensions.GetMemoryProperties2` (060002F2) ->
`vkGetPhysicalDeviceMemoryProperties2` with
`VkPhysicalDeviceMemoryBudgetPropertiesEXT` in `pNext`.

KSA selects the first memory type whose flags equal DEVICE_LOCAL, then reads that
type's heap index. Those are driver usage/budget estimates, not VMA statistics or
allocator payload sums. Renderer construction (06000195) filters required device
extensions, prefers discrete hardware and selects the first candidate; the query
uses that renderer's physical device. Both visible UI draw methods query directly;
no independent sampling timer was found in this traced path. The provider records
support but does not guard the query with it, and returns true even for zero/no
matching heap. NovaCore deliberately does not reproduce that failure behavior.

Selected reference identity: Planet.Render.Core.dll SHA-256
`9a2e23440aa2c55a7f0ab7ab4fcf03e5e935077fea0b82cbb05c6557c2b7050a`;
Brutal.Vulkan.dll
`328894dc18f20917d677e5cd840bd59ba75433e97a90bd3f7f6b00fff043b974`;
selected provider IL digest
`94bb9e95013a536038b226b32261b7fb16f3230076ba39e89eacb4624acf44aa`.
No proprietary implementation/assets were copied. Earlier authenticated history
through 5523 is reused; later browser access redirected to login, so no fresh
post-login read is claimed or needed for this already established boundary.

ADOPT driver estimates from the rendering adapter. ADAPT one cached owner,
explicit unsupported/failure/stale states, deduplicated DEVICE_LOCAL heap indices
and low-cadence sampling outside paint. INTENTIONALLY DIFFER in placement: only
NovaCore's loading screen displays this telemetry, per Project Control.

The [Vulkan extension](https://docs.vulkan.org/refpages/latest/refpages/source/VK_EXT_memory_budget.html)
and [budget structure](https://docs.vulkan.org/refpages/latest/refpages/source/VkPhysicalDeviceMemoryBudgetPropertiesEXT.html)
define the application budget and usage estimates. UUID/LUID identity follows
[VkPhysicalDeviceIDProperties](https://docs.vulkan.org/refpages/latest/refpages/source/VkPhysicalDeviceIDProperties.html).
Windows [QueryVideoMemoryInfo](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/nf-dxgi1_4-idxgiadapter3-queryvideomemoryinfo)
was considered, but is not needed or implemented. Vulkan is the sole authority;
there is no second backend or sum across Vulkan/DXGI segments.

## NovaCore telemetry lifetime and readiness

The native `PlayerGpuMemory` owner creates one Vulkan 1.1 instance after recorder
admission and UI readiness. A temporary presentation surface applies the existing
`Suitable` policy to the actual viewport HWND and is immediately destroyed. No
logical device, queues, world or GPU allocation is created to populate telemetry.
After START the renderer borrows that same instance and physical device. UUID is
revalidated and exact instance/physical handles are logged and checked. The
renderer creates its one ordinary logical device and enables the supported budget
extension. On shutdown it retires device/resources/surface/debug state; the form
then stops its timer and destroys the retained instance. Ownership checks reject
wrong-thread begin/poll/end and ending while the renderer is active.

A separate discovery instance initially returned zero usage even after allocation
on this RX 6800 XT. That unsuccessful attempt is retained in build evidence and
is not qualification. Sharing the renderer's actual instance corrected observed
usage. This is evidence of the required ownership domain on this driver, not a
claim about all Vulkan implementations. No second rendering device was introduced.

One 992-byte versioned snapshot owns identity, UTC/QPC timestamp, sequence, state,
raw heaps and totals. A 250ms WinForms timer and real loading checkpoints call the
same owner; an approximately one-second deadline limits native queries. `Read`
only copies the cache. Sampling stays on the renderer/UI owner thread to avoid
concurrent debug-callback writes. It adds no GPU wait and changes no quality,
allocation or memory policy. Optional CSV logging is qualification-only.

Sampling and repaint resume at real checkpoints during synchronous preparation;
they are not guaranteed every second inside an indivisible driver/file operation.
The final capture run's maximum sample gap was 2.064s (earlier successful run:
2.847s). At the next UI update, a sample older than 3s shows an explicit stale
state and hides usage/budget. Unsupported/missing-entry/query failure also hides
numbers. A query exception clears previous estimates and bounded retry recovers.
This does not claim hardware driver-fault injection or guaranteed repaint while
the UI thread is blocked.

START retains the exact admitted craft bytes, not a path that may change during
loading. The selected scene must publish its actual physical regional terrain
and matching presentation before readiness. A successful submission/present and
its normal completed fence are required. In the captured run, initial Solar
readiness was submit1/physicalOwner0; selected flight readiness was
submit59/regionalPupil1/physicalOwner1. Only then was the loading overlay removed.
No extra fence/GPU-idle wait was added for reporting or readiness.

## RX 6800 XT receipt and captures

The exact [machine-readable receipt](memory-receipt.json) and
[native samples](qualification/gpu-memory-final-native.csv) bind loading image,
UI log and native sample25 at **2026-09-29T22:21:07.2383376Z**. The capture returned
142.6624ms later. The PNG, not the asynchronously cached accessibility tree, is
the visual evidence. Same query/render instance `000002E5A9902F50` and physical
device `000002E5A4312E20`; vendor1002/device73BF, UUID
`00000000030000000000000000000000`, valid LUID `D6C4010000000000`.

| Heap | Flags | Included | Capacity bytes | Budget bytes | Usage bytes |
|---|---|---|---:|---:|---:|
| 0 | 0, non-device-local | No | 16,703,094,784 | 15,897,839,616 | 128,786,432 |
| 1 | 3, DEVICE_LOCAL + MULTI_INSTANCE | Once | 17,163,091,968 | 12,353,323,008 | 622,632,960 |
| Display aggregate | heap mask2 | heap1 | 17,163,091,968 | 12,353,323,008 | 622,632,960 |

Every DEVICE_LOCAL heap is included once by heap index, never once per memory
type. MULTI_INSTANCE is a flag, not a multiplier. `GiB = bytes / 1,073,741,824`;
fixed-point F1 capacity and F2 usage/budget produce **16.0 GiB capacity,
0.58 GiB NovaCore usage / 11.50 GiB application budget**. Unrounded values remain
in the receipt. The lower budget is retained honestly; it is neither free VRAM nor
card capacity. Usage is the driver estimate for NovaCore, not total-system usage.
An early zero was an actual pre-allocation reading, not a loaded-world prediction.
All 28 Ready samples passed the independent heap-total/identity audit.

- [Configuration: no live telemetry block](qualification/configuration.png)
- [Loading: real task, subordinate entries and live memory](qualification/loading.png)
- [Gameplay after genuine readiness](qualification/gameplay.png)

Captures precede only two final nonvisual corrections: frame statistics now says
GPU timing unavailable, and intentional loading cancellation is handled as a
normal exit. The successful captured branches are unchanged. Final current
source/binary seals are separate from the exact capture provenance.

## Qualification results and performance

| Evidence | Result and practical scope |
|---|---|
| Release build | 0 warnings/errors. |
| Native memory owner mock | 16 checks: heap dedup/aggregation, identity, cache/cadence, Ready -> exception clearing -> recovery, unsupported/missing entry/no local heap, balanced surface/instance cleanup. No GPU/device creation in this test. |
| Managed player tests | 22 checks: settings/HUD persistence/refusals and separate units/rounding, stale/failure presentation. |
| Launcher tests | 19 passed. |
| Captured flight startup from a saved craft | 12 checks: exact validated bytes, full3440x1440, readiness, physical pause/1x, Space unbound, queued ignition discarded across modal boundary, fresh ignition available, persistent engine state retained during pause, live nonzero memory, healthy teardown. |
| Final full-extent transitions | 17,275 checks over4806 loops. Stable parent/native HWND/projection and target through menu/pause/editor; actual1920x1080 resize then3440x1440 restore. Target generation1 during UI changes,2 on1920 resize,3 onrestore. |
| Construction preservation | 588 checks: placement/symmetry, undo/redo, consumables, craft save/load/admission, launch/powered/coast/editor return, focused text/Tab/combo and drag/capture/display lifecycle. Retained prior AD4 result; no simulation edits in telemetry slice. |
| Editor/input regressions | Exit0; native window/resize/minimize/restore/input boundary plus engine9898, attitude8264, integrated3007 checks and camera precision/storage cases. |
| Contact persistence / cycles | 35 / 3692 checks passed: save/reload plus repeated supported liftoff/contact/relaunch paths. |
| Real loading close | Alt+F4 during Universe preparation. First test exposed expected cancellation incorrectly logged as failure; narrow approved-cancellation catch fixed it. Recheck logs requested cancellation, cleanup complete, shared instance destroyed, empty stderr, exit0. |

Loading remains responsive at its checkpoints: real stages repaint, samples advance
during initialization, and a real close request is serviced at the next progress
boundary. This is bounded responsiveness, not a claim of continuous UI pumping
during every synchronous preparation operation. Normal completion and intentional
cancellation both retire the owner. No native driver error or display anomaly was
observed.

Reporting native query cost across28 samples: median **7.05us**, P95 **10.3us**,
P99/max **10.5us**. The four loading updates' poll/copy/format/label work had median
217.45us, maximum454.8us including optional CSV evidence writes, excluding final
console write and later compositor work. These are small bounded measurements,
not a whole-frame isolated telemetry A/B test.

Final 3440x1440 exploration cadence, milliseconds (first30 transition loops excluded):

| State | Median | P95 | P99 | Max | GPU median |
|---|---:|---:|---:|---:|---:|
| UI hidden | 5.5666 | 6.1654 | 6.4057 | 6.7434 | 1.0832 |
| Pause backdrop | 5.5392 | 6.1864 | 6.3694 | 6.6669 | 1.1117 |
| Hidden after resume | 5.5448 | 6.0667 | 6.2529 | 6.6066 | 1.0851 |
| Universe dropdown | 5.5583 | 6.1737 | 6.4917 | 6.6302 | 1.0869 |
| Restored3440x1440 | 5.5523 | 6.0869 | 6.3187 | 6.4738 | 1.0858 |

Full CPU/GPU/cadence distributions, transition windows and repeated120-loop tails
are retained in [final performance](qualification/transitions-final-performance.json).
Medians use the conventional mean of the two central values for even counts;
P95/P99 use nearest rank. 1920x1080 GPU median0.3297ms versus full-resolution1.0832ms helps separate pixel
work from UI cost. Editor changes scene content and is not an overlay cost control.
Windows compositor cost is outside GPU timestamps, desktop activity is uncontrolled,
and physical flight has a different workload. Original3440x1322 figures are not
used to qualify this extent. No resolution/quality reduction was made.

Independent read-only reviewer found no remaining blocking telemetry lifetime,
heap aggregation or receipt issue, verified the exact sample/image match and
recommended bounded engineering PASS. It caught the shared-instance worker/debug
callback risk (resolved by same-thread polling) and the stale HUD VRAM wording
(corrected). Final source/evidence hashes are in `seal.json`.

Remaining manual limits: Project Control visual correspondence/acceptance, real
multi-monitor DPI change, physical held-key release across Alt-Tab/focus regain,
and broader sustained performance are not qualified by injected-message tests.
150 FPS and blackout causality remain OPEN. No bank or next front is authorized.
