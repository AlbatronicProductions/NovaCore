# Startup / first-present correlation — 2026-09-17

**Subsequent Project Control decision:** after this diagnostic conclusion, the 2026-09-17 acceptance-boundary decision separated the shared startup residual and unresolved historical display observations from Stage-2 powered-support qualification. The measurements/classification below remain unchanged; their pre-decision acceptance ambiguity is resolved by that explicit decision. Continue only final Stage-2 consolidation, independent review and manual acceptance as recorded in [final acceptance](../final-acceptance/README.md). Do not continue timing investigation or open Stage 3 while manual acceptance is pending.

## What happened

The four predeclared captures establish a real shared **window-created but not yet scene-presented** startup path. The first powered capture spent **16,670.3851 ms** between window creation and the first successful Present API return; the other powered capture and both unpowered controls took **196.9571–215.2120 ms**. No physical servicing or canonical publication occurred before that return.

However, the long segment precedes the first managed scene callback and therefore precedes the original display counter's start. First legacy display intervals were **33.0536–40.7652 ms**, alongside first native frames of **34.9369–42.6326 ms**. The requested signature of a legacy display interval materially exceeding native work was **not reproduced**. Window lifetime must not be substituted for that counter.

**D. TARGETED EVENT NONREPRODUCIBLE / UNRESOLVED.** This classifies the requested combined startup/display-overhang hypothesis. It does not deny the separately proven unpresented startup lifecycle. Both original giant display witnesses remain **UNRESOLVED**. **STOP FOR PROJECT CONTROL.** Stage 2 remains UNQUALIFIED; Stage 3 CLOSED; no milestone assigned; UNBANKED.

## Current repository truth

- HEAD, main, origin/main and authoritative remote main: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`.
- Branch: `codex/srv01-supported-contact-admission`.
- Candidate source/test identities: **24/24 unchanged**. Native source restored to SHA-256 `4C468B0302603B59397A1D82C6E6E08577A2853F166FFEE7D2552107363C99B6`.
- All three temporarily edited files were restored byte-for-byte before captures. Their original/temporary hashes and built binary identities are in [identity.json](identity.json).
- All **67 historical tag refs unchanged**; index empty; `git diff --check` and cached check PASS. Existing candidate changes and unrelated review files remain preserved.
- This ticket adds only this evidence child. No retained production, test, asset, startup, timing or physics correction; no commit, tag or push.

## Previous population and Project Control observation

The [completed population](../display-gap-population/README.md) remains evidence: nine fresh processes, 36,000 frames, 35,991 display intervals, **200.4709508 seconds** of sampled exposure. No giant recurrence. Powered maxima were 60.6113 ms display / 60.2434 ms native; unpowered 58.8710 / 58.5364; free flight 41.0411 / 44.4730. The maximum after the first eight callbacks was **8.0598 ms**. No broad population was repeated.

Its actual dimensions were 960×540 despite a 1280×720 plan. Logging, observer overhead and active-physics exposure differed from historical captures. This targeted check also does not claim historical-harness equivalence.

Project Control independently observed a white client area before the scene appeared. That supplies the human visual observation, not a timestamp or accepted cause. This diagnostic measures lifecycle boundaries; it does not independently claim pixel color, human perception, compositor presentation or physical scanout.

## Fixed plan and method

[plan.md](plan.md) predates execution. Exact order: **A1, B1, B2, A2**. A is Stage-2 `srv01-powered-support`; B is Stage-1 `srv01-supported-contact`. Exactly two fresh processes per route, each 180 frames. All four VALID; none replaced, excluded or retried. No additional runs.

Actual dimensions **960×540** in every capture; windowed Vulkan FIFO, RX 6800 XT, Release .NET SDK 10.0.303, default logging, no intentional input. Runtime cache/background state was uncontrolled. The instrumented native/managed build passed with zero managed warnings/errors. Timestamp overhead is disclosed and not subtracted. Existing legacy sample and native frame expressions were preserved. Post-render replay and report output are outside the measured frame population; native overall wall-time averages are not used.

QPC and Stopwatch both report 10,000,000 ticks/second. [captures.json](captures.json) retains exact integer timestamps, process identities, first eight native-frame event traces, first twelve callback/accounting rows, complete-capture summary statistics, hashes and canonical comparisons. [reproduce.md](reproduce.md) and the bounded builder/capture/analyzer retain the experiment without retaining bulk binaries/logs by default.

## Startup timelines

Milliseconds relative to T0, the **earliest managed entry**, not exact OS process creation. External launch timestamps/PIDs are retained separately. T2 follows backend commands/bootstrap. T5/T6 are nested inside T3/T4; numbering is not sequential execution order.

| Boundary | Powered A1 | Control B1 | Control B2 | Powered A2 |
|---|---:|---:|---:|---:|
| T0 managed entry | 0 | 0 | 0 | 0 |
| T1 window created | 245.0557 | 215.8932 | 252.7890 | 214.8605 |
| ShowWindow returned | 251.2389 | 221.8277 | 264.2633 | 220.9540 |
| T2 backend ready | 16902.1405 | 399.0302 | 454.0592 | 396.8911 |
| T3 first native frame begins | 16902.4953 | 399.3448 | 454.6937 | 397.8775 |
| T4 first native frame ends | 16940.6267 | 434.2820 | 492.0286 | 440.5111 |
| T5 first Present begins | 16907.6072 | 404.5368 | 459.7028 | 402.9646 |
| T6 first successful Present returns | 16915.4408 | 412.8503 | 468.0010 | 413.6140 |
| T7 subsequent callback, scene ready | 16940.6905 | 434.3885 | 492.1126 | 440.6293 |
| T8 first advancing service entry bound | 16942.7217 | 436.2636 | 494.0097 | 442.5298 |
| T9 copied publication observation upper bound | 16998.9058 | 489.4013 | 546.0714 | 496.6748 |
| Original legacy counter start | 16904.5995 | 401.3825 | 456.7561 | 399.9617 |
| First legacy sample | 16940.7635 | 434.4361 | 492.1681 | 440.7269 |

All first Present calls returned strict `VK_SUCCESS` (0); image acquisition also returned 0. **T6 is API return, not display completion.** T7 is the first subsequent callback after successful Present and graphics-submit fence completion, with 38 scene objects. It establishes a successful submission lifecycle, not human-visible scanout.

T8 is entry to the first service invocation that advances frontier, not an instruction inside BEPU. T9 is the copied committed observation after that invocation; first physical solve/commit lie inside this bound. Some calls publish two endpoints, so T9 is not an invented exact timestamp for the first commit. B1/B2 have analogous unpowered service/publication boundaries. Native diagnostic code 4 means window-initialization entry; it is not exact RunRenderer entry despite the broader wording in the predeclared plan.

## Unpresented startup, display counter and cold service

| Duration, ms | A1 | B1 | B2 | A2 |
|---|---:|---:|---:|---:|
| Created → backend ready | 16657.0848 | 183.1370 | 201.2702 | 182.0306 |
| Created → first native frame | 16657.4396 | 183.4516 | 201.9047 | 183.0170 |
| Created → successful Present return | 16670.3851 | 196.9571 | 215.2120 | 198.7535 |
| Shown → successful Present return | 16664.2019 | 191.0226 | 203.7377 | 192.6600 |
| First Present call | 7.8336 | 8.3135 | 8.2982 | 10.6494 |
| First legacy display interval | 36.1640 | 33.0536 | 35.4120 | 40.7652 |
| First native frame | 38.1312 | 34.9369 | 37.3345 | 42.6326 |
| Present return → first advancing service entry | 27.2809 | 23.4133 | 26.0087 | 28.9158 |
| First advancing service execution | 56.1829 | 53.1363 | 52.0602 | 54.1439 |
| Entire capture legacy maximum | 59.5807 | 56.1269 | 55.5400 | 57.3560 |
| Entire capture native maximum | 59.2452 | 55.7981 | 55.0156 | 57.0537 |

A1's exceptional **16,531.2585 ms** bracket lies between visual-mesh readiness at 364.5328 ms and initial-swap readiness at 16895.7913 ms. Source places `CreateProductionCubeSurface`, `CreateLocalTerrain`, `Validate` and `Swap` inside that bracket. The evidence does **not** identify which operation, driver/runtime action or host interruption explains it. Do not label it shader compilation, swapchain cost or asset loading without further proof. No further attribution or correction was performed.

The narrow proven owner is **shared native render-backend/presentation initialization before first Update**. Both controls exhibit the same window-before-backend-ready lifecycle without powered physics. Only A1 exhibits the exceptional 16.5-second magnitude; its recurrence or route dependence is not established.

The normal 52–56 ms first service occurs **after** the first successful Present and is distinct from this pre-frame delay. It is not the historical giant-gap mechanism by numerical resemblance.

## Source ownership and powered-physics exclusion

Current `native/NovaCore.Native/NovaCoreNative.cpp:882` creates and shows the window. `RunRenderer` at line 2609 then runs Instance, debug setup, Surface, Device, mesh creation, production cube surface, local terrain, validation, Swap, submission, Commands and bootstrap before entering the Update/Draw loop. Outer messages are processed before the existing native frame timer.

`samples/NovaCore.Triangle/Program.cs:239` returns from diagnostic callbacks without advancing simulation. The first scene update calls `StockAssemblyDevelopmentScene.Start` (line 72), which starts the legacy timestamp; `AdvanceLive` returns immediately on that first call. Only later calls sample elapsed time, admit host credit and service contact debt.

Thus the measured pre-callback bracket is outside **both** original timers. The later first legacy interval may include callback suffix, Draw/Present, message processing and next callback prefix; these remain distinct scopes. In these captures it does not materially exceed native work.

Across all four runs there were **zero service entries and zero publications before first successful Present return**. The retained solve, thrust application, resource debit, mass/COM/inertia successor update, canonical publication and acknowledgement are reached through the service path, which starts later. Cold preparation can construct world/property state before the window; that is not powered evolution or elapsed-time debit.

## Historical attribution and three-way decision

| Historical witness | Retained native maximum | Attribution |
|---|---:|---|
| 595.898 ms display | 59.259 ms | **UNRESOLVED** |
| 1228.1696 ms display | 64.332 ms | **UNRESOLVED** |

The historical logs lack these lifecycle timestamps. More decisively, this newly measured long **pre-callback** segment cannot be included in the original legacy counter by its arithmetic. Do not promote it to a historical cause, or even claim the combined historical signature is demonstrated by this check. Other unobserved lifecycle delays are not ruled out.

| Independent question | Answer |
|---|---|
| Does current evidence attribute the measured pre-frame startup gap to Stage-2 powered physics? | **NO**. Historical display causation remains unresolved. |
| Is there a shared NovaCore startup/presentation responsibility? | **PROVEN** for the common lifecycle; exceptional severity is not proven common. |
| Does that newly isolated shared residual independently block Stage 2 under the current contract? | **AMBIGUOUS**. No explicit startup budget or acceptance exception was established. |

Current [closure plan](../closure/plan.md) and prior population disposition retain integrated display quality; no automatic exception waives the original unresolved display blocker. This ticket permits no invented waiver. Physics exclusion does not dismiss a player-visible startup stall. The original blocker remains unresolved regardless of how Project Control assigns the newly isolated startup responsibility.

## Simulation and recovery

Initial frontier and debt were zero in all captures. Backend startup preceded host sampling; the 16.5-second A1 delay was **not** credited as simulation debt. First advancing callback was callback 1 in every run:

| Capture | Admitted ticks | Publications in call | First resulting target | Debt after call | Service ms |
|---|---:|---:|---:|---:|---:|
| A1 | 36164 | 2 | 33333 | 2831 | 56.1829 |
| B1 | 33053 | 1 | 16666 | 16387 | 53.1363 |
| B2 | 35412 | 2 | 33333 | 2079 | 52.0602 |
| A2 | 40765 | 2 | 33333 | 7432 | 54.1439 |

| Capture | Final publications / revision / history | Total admitted ticks | Published ticks | Retained debt | Max publications / callback |
|---|---:|---:|---:|---:|---:|
| A1 | 64 | 1070121 | 1066666 | 3455 | 3 |
| B1 | 63 | 1063525 | 1050000 | 13525 | 4 |
| B2 | 64 | 1067208 | 1066666 | 542 | 3 |
| A2 | 64 | 1073547 | 1066666 | 6881 | 3 |

Every run had 180 callbacks, 180 native frames and 179 legacy samples. Each copied endpoint matched a post-render same-route single-interval replay: motion bits, resources, full mass/COM/inertia, revisions and history. This verifies partition consistency, not an independent physics algorithm. World/body/generation/shape continuity passed; no invalidation or pending endpoint; TimelineRevision stayed zero. All admitted credit minus published ticks equals retained debt. At most four publications per callback and exactly one native frame per callback: backlog never multiplied rendering.

Final powered stores were fuel **29.91666671875 kg**, oxidizer **44.875000078125 kg**, total mass **704.791666796875 kg**; unpowered stores stayed 30/45 kg and 705 kg. This bounded startup capture intentionally ends after about one simulated second: `completed=false`, `failed=false` is expected, not a failed 20-second qualification. No new exhaustion, departure, support-qualification or allocation claim.

## KSA, red team and judgment

KSA review required: **NO**. **NO NEW KSA RESPONSIBILITY OPENED.** Evidence locates the measured delay before physical servicing; no physics/publication mechanism design occurred.

The independent reviewer recalculated raw-log boundaries, checked all four identities/counts/accounting/replay results, source restoration and all 24 requested attacks. [red-team.md](red-team.md) records the result and limitations. No manual acceptance was requested or self-declared.

**Judgment: STOP FOR PROJECT CONTROL.**

Stage 2: **UNQUALIFIED**. Stage 3: **CLOSED**. Milestone: **NOT ASSIGNED**. Banking: **UNBANKED**. No next front, correction, extra capture or qualification campaign begins automatically.

## Evidence and disposable output

Retain this report, the fixed plan, captures/timeline values, preflight/final identity, independent red team, small probe sources/build/capture/analyzer and reproduction instructions. Reference prior [display-gap-closure](../display-gap-closure/) and [display-gap-population](../display-gap-population/) instead of rewriting them. Build binaries, source backups and bulk raw logs are disposable after this retention/review; counts, exact reviewed targets, cleanup result and manual/non-destructive commands are in [cleanup.md](cleanup.md).
