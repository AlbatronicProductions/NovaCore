# Stage 5 final closure

**Current Project Control correction, 2026-09-17: engineering PASS; Florida manual acceptance PENDING / NOT YET TESTED CORRECTLY; Stage 5 NOT FULLY QUALIFIED.** The earlier manual PASS applies only to supported-craft visual/physical behavior. See [manual-result.json](manual-result.json), the [route ownership check](manual-route-ownership.md) and the [held promotion review](../../post-stage5-promotion.md). The engineering report below remains historical. Its claim that the simplified site-local viewport was sufficient for Florida manual acceptance is not accepted by Project Control. Physics and timing results remain unchanged.

**PASS — STAGE 5 ENGINEERING QUALIFIED, MANUAL ACCEPTANCE PENDING.**

Stock physical qualification remains unchanged. This closure establishes terminal debt semantics, completes the unchanged Debug storage test, classifies the live servicing tail with exactly three predeclared fresh Release captures, and completes launcher/protected regression checks. No production/permanent-test code, physics, solver quality, stock resources, terrain geometry, frame mathematics or lifecycle was changed. No optimization was performed. UNBANKED. Stage 6 CLOSED.

## Terminal debt: A. LEGITIMATE EXCESS HOST CREDIT AFTER COMPLETE FRONTIER

The exact 1,200-entry schedule sums to 20,000,000 ticks. Host admission retains the complete elapsed input; publication subtracts only each exact interval. The historical live run therefore admitted 20,004,094 ticks, published all 20,000,000 required ticks and retained 4,094 ticks beyond the bounded episode. No required interval remains.

The focused `TerminalCreditProof` admitted exactly 20,004,094 ticks, completed 1,200 publications/history records, observed pending=false / invalidated=false / SupportedContact, and checked four further calls returning Completed with zero publication and unchanged canonical/native state. A further credit call also returned Completed without mutation. World/body identity remained retained. The original log did not separately print pending/consumer; their historical values follow from the successfully completed synchronous source path, while the focused proof directly observes them.

Source: `AssemblyFlight.CreateFloridaSupported` constructs the telescoping schedule; `SimulationTransactionEngine.AdmitAssemblyHostTime` commits credit independently; `PublishAssemblyInOwnedPhase` subtracts the interval, clears Active, commits state/history and acknowledges native Pending=false. `ServiceAssemblyDebt` returns Completed at frontier==Plan.Length before attempting further debt/index/preparation work. The scene stops admitting host credit after Completed. Debt is neither discarded nor unfinished required physical work.

The three fresh terminal debts were 4,831 /1,603 /3,094 ticks. Every capture reached tick 20,000,000, frontier/history 1,200, pending=false, invalidated=false and SupportedContact; an additional service returned Completed/0 without mutation. The complete 1,200-frontier motion hash was identical across all three: `F8D69B7E49FFFD20`.

## Completed storage check

The already-added storage-only permanent test ran unchanged in Debug, after 1,200 completed intervals. It matched retained Release exactly:

- Managed retained delta:4,439,832 B.
- Native pool:475,136 B.
- GPU mesh payload:1,359,096 B.
- Combined owned total:**6,274,064 B**, below 8,388,608 B.

Shared terrain data and global renderer/device resources remain excluded and separately disclosed in the restart report. This is not a total-process working-set claim. The Debug build passed with zero warnings/errors.

## Historical event correlation: UNKNOWN

The original 72.4557 ms service and 74.5905 ms display maxima remain in the unchanged [first live process](../stock-florida-restart/first-live-process.txt). The original reporter independently sorted both arrays and retained no indices/timestamps. Their historical association cannot be recovered and is not inferred from magnitude.

Important boundary: display[n] measures callbackStart[n]−callbackStart[n−1]; service[n] occurs after callbackStart[n]. A service delay therefore contributes to the following display[n+1]. Fresh indexed captures establish that boundary without rewriting the historical run.

## Exactly three final captures

[capture-plan.md](capture-plan.md) and `capture-preflight.json` were recorded before process 1. Same 960×540 Vulkan/RX 6800 XT route, default fixed camera, engine/RCS OFF,20 simulated seconds,4,000-frame auto-start/auto-close capture. Exactly three fresh sequential Release processes completed with exit 0. No invalid/replaced/fourth capture. No other build/test job overlapped.

The observer was an isolated MSBuild compile overlay in disposable output. It leaves candidate source, `Advance`, time conversion, solver, publication and render calls unchanged. Sensors and buffers initialize before host sampling; row serialization occurs only after episode completion. It is a bounded diagnostic, not a permanent profiler. Allocation results from this observer do not replace exact-zero permanent qualification.

| Process | Display samples | Display median / P95 / P99 / max ms | Service median / P95 / P99 / max ms |
|---|---:|---|---|
| 1 | 3,587 |5.5558 /5.7316 /5.9746 /59.7444 |0.0034 /0.0732 /0.1646 /58.3315 |
| 2 | 3,587 |5.5554 /5.7316 /5.9225 /60.4608 |0.0038 /0.0802 /0.1829 /58.8670 |
| 3 | 3,588 |5.5568 /5.7087 /5.8647 /55.1968 |0.0036 /0.0775 /0.1315 /53.7912 |

The service population is one bounded call per display callback and includes debt-only/no-work callbacks; it is not the earlier per-publication complete-operation population. All absolute distributions, selected tail rows, timestamps and raw-log hashes are in `capture-results.json`.

## Tail classification: A. BOUNDED COLD / FIRST-USE SERVICING

| Maximum-service witness | Process 1 | Process 2 | Process 3 |
|---|---:|---:|---:|
| Callback index (zero-based) |0 |0 |0 |
| Publication frontier before→after |0→2 |0→2 |0→1 |
| Service ms |58.3315 |58.8670 |53.7912 |
| Solver-facing constraints before→after |0→1 |0→1 |0→1 |
| Native pool before→after B |344,064→475,136 |344,064→475,136 |344,064→458,752 |
| Per-thread counter delta B |680 |680 |624 |
| GC 0/1/2 delta |0/0/0 |0/0/0 |0/0/0 |
| Bracketed thread CPU ms |46.875 |62.5 |46.875 |
| Largest display callback index |1 |1 |1 |

Each process had exactly one service sample above 1 ms, at the first physical operation. World/body/site already existed before READY; identity stayed unchanged. The first constraint is admitted on the first actual timestep. Process 3's remaining 16,384 B pool growth and 56 counter bytes occur in callback 1, whose service took 0.8516 ms. No further pool growth is shown by callback-boundary snapshots. These facts support one-time first-use work, not recurring world/body/site reconstruction.

All three largest display intervals are the interval immediately following the first service call. Their timestamps bracket that service. Thus the fresh maxima have **different callback indices but a proven containing next-display interval**. The three historical magnitudes are never substituted for the original values.

At>=128 published intervals, each process has 3,216 callback samples:

| Process | Service median / P95 / P99 / max ms | Display median / P95 / P99 / max ms |
|---|---|---|
| 1 |0.0034 /0.0685 /0.0876 /0.1775 |5.5558 /5.7262 /5.9624 /11.5206 |
| 2 |0.0036 /0.0728 /0.0882 /0.1577 |5.5556 /5.7282 /5.9070 /6.2862 |
| 3 |0.0036 /0.0687 /0.0901 /0.1243 |5.5577 /5.7075 /5.8486 /6.2557 |

No comparable warmed servicing tail recurred. The 128-frontier cohort alone is not the cold proof; exact first-publication/constraint/pool evidence establishes that classification. The 1 ms evidence threshold is not an invented acceptance budget.

**Residual disclosure:** process 1 has four warmed display intervals 11.1948–11.5206 ms at indices 1503/1563/2137/3179 (4/3,216 warmed callbacks). Their preceding service times are 0.0024 /0.0042 /0.0018 /0.0574 ms. These delays are predominantly outside contact servicing; their precise host/render/wait owner is **UNPROVEN**. Processes 2/3 have no warmed display interval above 6.67 ms. No claim is made that all display outliers disappeared. Median/P99 remain within the approximately 6.67 ms integrated frame objective; the 50–59 ms servicing event is confined to first use. Independent review accepts timing closure under the current integrated contract, with these tails disclosed; no retired standalone microbudget is invoked.

CPU accounting is coarse Windows thread accounting, not an exact stack/descheduling breakdown; it can exceed a shorter wall interval at that granularity. Constraint counts are solver-facing admission evidence, not a measurement of internal manifold-cache cost. Pool endpoint equality cannot rule out transient internal activity. Rendering and title update are outside the service stopwatch. The residual wall-minus-CPU values do not prove a host wait. No profiler or speculative attribution was added.

The observer dumps rows after completion, so native whole-4,000-frame maxima can include diagnostic serialization. They remain in the raw aggregates but are not represented as uninstrumented whole-frame performance. The separately preserved active-episode populations above end before that dump.

## Correct lifecycle boundary; no optimization

Explicit world/body/shape/site creation is already cold preparation before READY and before host sampling. Actual first-contact constraint creation occurs in the first funded ordinary timestep; it is not repeated each display frame. Native storage is then retained. No unnecessary repeated lifecycle work or measured payoff for moving preparation was demonstrated. The capture does not separately assign the first-use cost among JIT, contact buffers and individual solver methods. No production correction is needed for this ticket and no theoretical optimality claim is made.

**NO NEW KSA GATE REQUIRED.** No body-fixed ownership, retained-body lifecycle, contact lifetime or publication semantics changed.

## Final regressions and manual route

- Existing Launcher tests:18/18 Debug and 18/18 Release, correct five-parent output layout.
- Full Simulation:80/80 declared groups per configuration;95 PASS-prefixed lines include nested witnesses, not extra top-level groups. This covers preserved Stage 1–4 mechanisms and M15.2 contracts.
- ReferenceFrames, Precision, verified BEPU and focused stock Florida presentation: PASS Debug/Release.
- Presentation gates retain 0 B warmed allocation and 152 B deliberate control.
- Prior stock physical/oracle/determinism/zero-allocation and full-solution qualification is relied on through unchanged source seals; it was not reopened. Debug storage closure and diagnostic Release build both have 0 warnings/errors.

The existing Launcher's “Florida launch mapping” test covers Solar scenery. No new GUI dropdown was added. The dedicated SRV-01 direct scenario is separately verified by argument parsing, focused presentation tests and these three actual processes.

[manual-acceptance.md](manual-acceptance.md) gives the original uninstrumented, hash-checked Release executable, exact command and controls. It presents the authenticated terrain-v5 graded patch through its admitted bounded planar representation, not a substitute floor. Actual full Florida scenery is not drawn in this site-local development viewport. Project Control must still observe stock identity, placement, OFF actuators, absence of sinking/creep/instability, independent camera and stable final hold. **MANUAL ACCEPTANCE: PENDING.** No fourth process/manual window was launched.

**ROTATING-SITE RUNTIME DEPARTURE: NOT YET QUALIFIED.** Stage 3's nonrotating handoff remains restricted to its accepted domain. No powered Florida departure, stack, ascent, SAS, autopilot, navball or guidance work opened.

## Independent review, identity and cleanup

The read-only verifier checked all 20 ticket attacks and returned **PASS — STAGE 5 ENGINEERING QUALIFIED, MANUAL ACCEPTANCE PENDING**. It independently verified debt semantics, raw timing rows/hashes,3/3 process journals, Debug/Release storage,80/80 Simulation,18/18 launcher, protected regressions and manual binary seals. It required the historical UNKNOWN correlation, four outside-service display tails, observer-reporting limit and departure/manual boundaries to remain explicit.

Current HEAD/main/origin/main/remote main remain `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5` on `codex/srv01-supported-contact-admission`. All 67 historical tags remain unchanged; nothing staged, committed, tagged or pushed. The seven candidate seals and 475 other protected source/test/sample files match. No permanent source/test changed during this closure. New files are concise evidence/reproduction/manual instructions; two historical packet pages only gain explicit supersession pointers, preserving their original stop record.

See `closure-verification.json`, [reproduction.md](reproduction.md) and [cleanup.md](cleanup.md). The new isolated output root is disposable after this consolidation; native dependencies and original manual executable remain KEEP. No cleanup was attempted or policy workaround used.

**STOP FOR PROJECT CONTROL. UNBANKED. Stage 6 CLOSED.**
