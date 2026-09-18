# Post-M15.2 current-production remeasurement

2026-09-17. **Audit only. No implementation, milestone assignment or banking.**

**Judgment: A. OPEN ONE NEW PRODUCTION FRONT (recommendation only).**
Recommended responsibility: **qualify the existing canonical SRV-01 assembly in
bounded retained supported contact without replacing its identity or resource authority.**
Project Control must authorize scope before design/implementation. This audit does
not open that front, prescribe a collider, or authorize departure/Florida.

## What happened

Verified current main, rebuilt current Release production, ran three fresh
480-frame stock-scene processes, the existing operation/save/restore measurement
campaign, fourteen allocation gates and focused correctness/presentation checks.
Temporary source copies exposed per-frame ownership and phase labels: three stock
runs, one moving-camera run, and one separate qualification-box contact run.
No tracked source, tests, assets, README, internal control docs or history changed.

Main/HEAD/origin/main/live remote main: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`.
M15.2 bank: `c3de150db8563864d23c4951b4630559453d8f56`.
Annotated tag: `m15.2-srv01-reusable-production-spacecraft-integration`;
tag object `eb86bab80bdcae1f2c745ae82f3a016d71c260b4`, target remains that bank.
All 67 tags are preserved. Historical preparation reports remain historical.
See [repo truth](repo-truth.json), [input/build identity](identity.json),
[measurements](measurements.json), [ownership/readiness](ownership-readiness.md)
and [reproduction](reproduce.md).

## Current whole-frame result

Release, AMD RX 6800 XT, 1280×720, FIFO presentation. Unmodified production;
each process includes startup, the actual two-second episode and final hold.
Native frame = Update + Draw/present elapsed wall time. The message pump is
outside that bracket; managed callback-interval distributions are separately
retained. CPU includes waits and must not be added to GPU time.

| Fresh process | Median ms | P95 ms | P99 ms | Maximum ms | GPU mean ms | Managed cold preparation ms |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 5.548 | 5.867 | 7.310 | 36.498 | 0.112 | 164.8209 |
| 2 | 5.552 | 5.855 | 7.566 | 38.927 | 0.110 | 138.0106 |
| 3 | 5.547 | 5.854 | 7.522 | 38.025 | 0.109 | 145.6852 |

CPU averages across those processes: inclusive update 5.171–5.181 ms, fence
4.987–4.997 ms, inspection 0.031–0.033 ms, callback 0.149–0.154 ms, upload
0.002 ms, recording 0.054–0.055 ms, submit 0.027–0.029 ms, present 0.245–0.249 ms.
Update includes the four child buckets. GPU timestamp `toneMap` also contains
exhaust/resolve; it is not an isolated tone-map measurement.

Current source-of-truth targets retain 6.67–6.94 ms preferred headroom, 11.11 ms
preferred full-game and 16.67 ms sustained Ultra/max floor; terrain foundation
has its separate 8.33 ms target. Stock median leaves **1.118–1.123 ms to 6.67**
and **1.388–1.393 ms to 6.94** in measured wall time. This paced result is not
a measured spare CPU/GPU budget. P99 exceeds 6.67 by 0.640–0.896 ms. Startup
maxima miss every one of these ceilings. No all-frame target PASS is claimed.
The current bounded scenario does not prove a future fully featured game budget.

## Scenario performance and coverage

The stock plan is 128 × 15,625 ticks (64 Hz), not the contact world's 60 Hz
schedule. It contains main, gimbal, then six RCS rotational command rows.
Pitch/yaw rows also produce translation; no independent translation-axis UI exists.

Diagnostic frame figures below are from stock process 1 (540 frames, first 60
READY). All three populations/tails remain in measurements.json. Labels describe
the copied destination endpoint: bounded servicing may cross a phase boundary.
No instrumentation-overhead subtraction or equivalence to isolated workloads.

| Case | Current executable coverage | Median / P95 / P99 / max, ms |
|---|---|---|
| A READY | Existing stock scene before Space, 60 diagnostic frames incl startup | 5.5571 / 6.0372 / 37.5475 / 37.5475 |
| B unpowered free flight | Existing no-demand production API/test population; **no displayed preset** | Operation only: 0.0119 / 0.0130 / 0.0241 / 0.0785 |
| C main | Stock main phase, 48 frames | 5.5535 / 6.1998 / 10.0203 / 10.0203 |
| C gimbal | Stock gimballed phase, 45 frames | 5.5312 / 5.8946 / 6.0111 / 6.0111 |
| D RCS | Stock six-pair phase, 268 frames | 5.5502 / 5.8452 / 5.9557 / 5.9940 |
| E main + RCS | Existing API/permanent test; **no displayed stock overlap** | Operation only: 0.0160 / 0.0169 / 0.0184 / 0.0202 |
| F supported powered SRV-01 | **Unavailable**; separate box does not substitute | Not measured for SRV-01 |
| G dry SRV-01 | Existing no-feed API/permanent test; stock does not exhaust | Operation only: 0.0156 / 0.0166 / 0.0183 / 0.0406 |
| H save/restore | Existing synchronous API, no scene/UI restore transition | API figures below; displayed transition unavailable |
| I moving camera | Same stock plan, diagnostic orbit, 540 frames | 5.5476 / 5.9153 / 7.5381 / 37.6442 |
| Final stock hold | Held endpoint, **not continued unpowered flight**, 119 frames | 5.5676 / 5.8230 / 5.9708 / 8.0636 |

The separate `powered-contact` **2×1×1 box** route was also measured, without
calling it SRV-01: 4,000 frames, 5.5468 / 5.9107 / 6.1801 / 37.2501 ms.
First powered inspection transition, frame 60: **20.1421 ms** total,
**14.6200 ms** managed service. First exact-exhaustion inspection is separately
captured. Held powered/exhausted bins mostly wait for inspection; their medians
are not powered-solver operation costs. Dry continuation (3,583 frames) is
5.5474 / 5.9048 / 6.1533 / 11.5623 ms; service
0.0076 / 0.0875 / 0.1735 / 0.6108 ms, including no-work frames and cold paths.
It completes 1,200 history/publication steps at tick 20,000,000, retaining 304
debt ticks. This does not requalify historical isolated hot-operation ceilings.

## Tail/hitch audit

| Witness | Repetition | Attribution and current disposition |
|---|---|---|
| Stock diagnostic frame 0: 37.5475 / 38.1402 / 38.9236 ms | Every fresh process, cold | Startup resize/recreation-associated; 4.22–4.52 ms callback, 8.12–8.57 ms present, approximately 24–26 ms outside existing child buckets. Acquire/recreation lie outside them. **PARTIAL**; no invented internal split or JIT cause. First GPU result unavailable. |
| READY frame 7/8: 16.4211–16.6098 ms | One early frame in each stock process | Fence 15.9320–15.9802 ms; no managed GC. Wait ownership **PROVEN**, initiating scheduling/driver/display cause **UNATTRIBUTED**. |
| First stock physical publication, frame 63: 9.9825–10.3046 ms | Once per episode/process | Service 4.3879–4.4685 ms plus ordinary wait. First-use location **PROVEN**, internal cold/JIT split unmeasured. |
| First completion report, frame 422: 7.5988–8.0636 ms | Once per stock episode | Synchronous report/sort path lies in service boundary; **PARTIAL**. Not repeating steady RCS work. |
| Box first powered transition: 20.1421 ms | One measured transition | 14.62 ms service, 52,984 callback counter bytes, no GC. Cold path, not warmed allocation gate; **PARTIAL**. |
| Box dry frame 2090: 11.5623 ms | Five dry frames >11.11 ms; no dry frame >16.67 | 10.7632 ms fence vs 0.0640 ms service, GPU 0.03636 ms. Wait ownership **PROVEN**, root cause **UNATTRIBUTED**. |
| Save #115 17.5171; restore #29 18.6303 ms | Repeated API stress population, cold request boundary | Synchronous serialization/replay ownership **PROVEN**; these maxima have zero GC collections within their own timing windows. Initiating tail cause **UNATTRIBUTED**. |

Exact frame/sample IDs, top 12 whole-frame tails, phase tails, GC counts and
transition rows are retained. Three 540-frame stock runs each have six frames
>6.67 ms, two >11.11 ms and one >16.67 ms. The camera run is similar.
Cold costs are not hidden in warmed averages. These re-observed limitations are
bounded and do not establish a new M15.2 regression; the current user observation
reported no visible defect. No independent proof that a tail is imperceptible.

## Operation ownership, allocations and persistence

Existing campaign: 128 warm + 1,024 timed operations, normal runtime, one fresh
Release process, sixteen explicitly named populations. No new thresholds.

| Responsibility | Owner/cadence | Median / P95 / P99 / max ms |
|---|---|---|
| Main complete operation | Canonical assembly owner, one admitted interval | 0.0156 / 0.0168 / 0.0190 / 0.0334 |
| RCS pair complete operation | Same owner, one admitted interval | 0.0158 / 0.0167 / 0.0181 / 0.0363 |
| Interior depletion | Exact resources + two motion segments + one publication | 0.0221 / 0.0229 / 0.0251 / 0.0492 |
| Concurrent preparation | Exact resources, tensor/wrench and RK4 motion | 0.0130 / 0.0136 / 0.0160 / 0.0253 |
| Concurrent fixed publication | Final authority, state/history/debt, acknowledgement/copy | 0.0014 / 0.0016 / 0.0017 / 0.0044 |
| Four-interval backlog | Same retained owner, one copied observation, no render loop | 0.0583 / 0.0689 / 0.1060 / 0.1262 |
| Save at frontier 64 | Explicit request; serialize state/history/credits | 0.7007 / 1.5432 / 3.9249 / 17.5171 |
| Restore at frontier 64 | Explicit request; new managed owner + exact replay | 1.3219 / 2.9767 / 4.2330 / 18.6303 |
| First resumed interval 65 | Ordinary canonical operation | 0.0106 / 0.0197 / 0.0280 / 0.0850 |

Sub-buckets are distinct populations; do not add/subtract their percentiles.
Command ingestion, resource arithmetic and individual derivative costs are not
individually instrumented here. Source establishes owner/cadence, not invented costs.
All fourteen independent checked allocation windows: **0 B**, entry/exit PASS;
positive byte[128] control **152 B**. Focused presentation also passes warmed zero.
No render work inside simulated-interval loops was found. Physics/publication
scale with serviced intervals; copied transforms/plumes scale with display frames.

Save samples allocate 954,027,072 aggregate counter bytes; restore samples
1,422,913,240 across 1,024 requests. Collections save 36/4/4 and restore 65/8/8
(Gen0/1/2). This is intentionally cold synchronous work, not a hot zero failure.
Restore verifies exact state/resource/history/debt, then ordinary frontier 65.
**ACCEPTED LIMITATION, re-observed.** No GPU/BEPU resources are recreated by this
free-flight session API. First rendered restored frame/settling are unavailable
because the scene has no restore consumer. Do not extrapolate an API maximum
into a player frame or claim in-game reload acceptance.

Conservative runtime managed retention bound **344,544 B**; forced-GC estimates
317,400 B initially / 317,592 B after 128 intervals. History 199,704 B, credit
trace 65,560 B, compiled plan 40,984 B are **included**, not extra. Serialized
frontier-128 save 18,982 B. These are bounded measurements, not an unbounded-leak
proof. Shared meshes **1,359,096 B**, prepared object submission **4,416 B**, two
plume GPU targets **9,896,672 B** at 1280×720. Native startup also reports generic
terrain residency **19,398,656 B** even on this route; it is shared renderer
infrastructure, not private SRV physics storage. Figures do not sum to total GPU
memory; HDR/depth/swapchain and other native resources are separate.

## Presentation and current visual observation

Physical thrust and visual exhaust consume the same realized endpoint, with
opposite exhaust axis, qualified nozzle sockets and actual gimbal. One shared
original proxy drives depth-clipped emission/extinction integration and weighted
overlap; overlapping light transport remains approximate. Time comes from copied
canonical state. Terminal observations suppress plumes; no renderer-clock physics.
Four hash-qualified GLBs load once; no textures/skins/animation runtime, no Blender
runtime dependency. Meshes/pins live for the scene; native buffers for renderer;
plume attachments rebuild on resize, not per nozzle or physics interval.

The user observed the current autoplay window and reported: **“The window opened
and ran on its own and closed. It PASSED.”** This is a bounded no-defect visual
observation, not evidence that the user pressed Space, exercised save/restore,
or personally moved the camera in this audit. The automated presentation gate
and separate camera-orbit capture cover camera independence/attachment. A separate
hidden READY-only helper launch was stopped and excluded from all measurements.
No reproducible physical/visual mismatch was found. No subjective polish defect
or broader player acceptance is inferred.

## Residual inventory and top three

1. **Canonical assembled spacecraft retained-contact admission — HIGH potential,
   strong source evidence.** Current SRV-01 never calls BEPU, has no gravity or
   contact, and generic applied-state installation explicitly rejects assembly
   slots. Current powered contact is a separate qualification-box/single-resource
   owner. This is the first missing owner for a supported SRV-01, before departure.
2. **Bounded live command/scenario inspection — MEDIUM potential, incomplete
   payoff evidence.** Stock commands are selected before launch; the frontend
   cannot select combined/no-demand/depletion cases without API/test code. This
   limits iteration/player interaction, but no workflow-cost campaign or user-error
   rate proves it outranks the physical authority gap.
3. **Ready/first-use frame responsiveness — MEDIUM current visibility,
   PARTIAL cause.** Cold startup 36–39 ms and first-operation tails recur. No warm
   throughput blocker, visible failure, or measured avoidable-cost mechanism was
   established. A bounded ownership attribution could precede any correction;
   no general renderer optimization justified.
4. **Explicit restore readiness — MEDIUM future workflow value.** API cost is
   measured, but no live restore path exists; preservation/cold staging is needed
   before promising interactive persistence. Do not optimize by removing replay
   authority checks without a separate design gate.
5. **Contact acquisition for approaching assemblies; departure from retained
   contact — downstream HIGH eventual unlock, not the immediate owner.** Existing
   prepared contact does not establish acquisition or contact-to-free transfer.
6. **Environment/site binding and lift-capable article — separate launch needs.**
   Existing 600 N main on 705 kg wet / 630 kg dry cannot lift against 9.81 m/s²;
   even dry thrust/weight is about 0.0971. No thrust retuning is authorized.
7. **Physical aerodynamics, thermal/reentry, visual atmosphere, clouds, water
   dynamics, construction/editor authority — LOW/UNPROVEN immediate payoff.**
   Distinct future outcomes, not current regressions or a bundled milestone.
8. **Higher terrain fidelity/generic renderer resource reduction — LOW/UNPROVEN
   immediate payoff.** Current terrain is v5/generation 4 on Earth, not the
   ticket's v4 shorthand. More detail is not a demonstrated spacecraft blocker.

| Top candidate | Required outcome / unlock | Why now and cheap first proof | Preserve / stop / future scope |
|---|---|---|---|
| A recommended: supported canonical SRV-01 | Same authored assembly can participate in bounded supported contact with coherent motion, two species, mass/COM/inertia and actuator publication | Two accepted production responsibilities remain disconnected at explicit type/authority guards; prove one admitted finite supported episode with unchanged identity/resource ownership before broad design | Preserve exact resources, M15.1/M15.2 and solver quality. Stop if this requires a general vehicle framework, new mass policy, broader geometry architecture or terrain/environment integration beyond the bounded supported fixture. Qualifying the assembly collision representation belongs to the proposed outcome. Later scope is one bounded admission/publication outcome, not departure/Florida. |
| B live command/scenario inspection | Exercise existing production actuation cases without rebuilding schedules | Existing launcher exposes one prerecorded stock plan; first quantify missing selections and actual iteration burden | Preserve canonical commands and prepared applicability. Stop if this expands into construction/editor or arbitrary mutations. |
| C first-use readiness | First visible ready/action frames satisfy an explicitly chosen responsiveness outcome | Repeating cold frame IDs are now located; first separate acquire/recreate/wait from managed first use and decide product significance | Preserve rendering/physics/quality. Stop if no material visible/avoidable cost; no optimization bundle. |

These are descriptive payoff classes, not fake scores. A is recommended for
architecture/player leverage, **not** because current production failed or because
“reusable” implies landing. It connects a concrete current assembly to an existing
contact responsibility; B has less established payoff and C is a bounded accepted
limitation rather than a demonstrated new blocker. Project Control may still hold
if a supported-assembly outcome is not its current product priority.

## KSA screen and closeout

Future design of A must begin with the actual current KSA assembled-body/contact
owner, ready/apply lifecycle and retained live-changelog history. B likely has
equivalent command/UI ownership; C needs an equivalence check for the identified
startup responsibility before adopting any method. This ticket performs a **future
gate screen only**, not a new completed direct KSA comparison or mechanism design.
No KSA installation/history was accessed or copied for this audit.

Current Release app, Simulation-test and Graphics-test builds: zero warnings/errors.
Focused SRV-01: 16,260 checks, 24 independent trajectories; presentation and exact
allocation PASS. No broad full Debug/Release qualification rerun is claimed.
Independent source and measurement review: see [verification](verification.md).
Disposable copies/logs/builds and exact commands: [cleanup](cleanup.md).
No branch, tag, commit, push, optimization, production fix or new front created.

**MILESTONE NUMBER: NOT ASSIGNED. STOP FOR PROJECT CONTROL.**
