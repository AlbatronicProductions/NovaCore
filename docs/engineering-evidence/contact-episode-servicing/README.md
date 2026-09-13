# Host-paced canonical contact servicing — candidate qualification

Date: 2026-09-13. **UNBANKED. No milestone assigned.**

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Final classification: **HOST-PACED CANONICAL CONTACT SERVICING — FOLLOW-UP REQUIRED**.

Current follow-up: the Unicode status defect is corrected and both repeated manual scenarios are PASS. The bounded whole-frame observer localizes early service costs and later costs outside service but does not explain their full mechanism: **UNRESOLVED**. See [status/tail follow-up](status-tail-follow-up.md) and [exact results](status-tail-results.json). No performance correction or broad requalification followed that classification.

## Previous validation stop (historical; status correction supersedes this issue)

The numerical, allocation, storage, combined-operation timing, build and regression gates passed. The live visual check then exposed a status-presentation failure: the selected native window still reports and displays only `N`, including after a completed tilted episode. The attempted scenario-specific `SetWindowTextW` update did not establish the required visible running/completed status. The console correctly reports completion, but that does not establish an independently clear status in the rendered witness. No further implementation or acceptance runs followed this observation.

This is a **visible status transport / presentation blocker**, not evidence of canonical accounting, solver, or dependency failure. The exact mechanism is not qualified. A source pointer for a bounded follow-up is `NovaCoreNative.cpp`'s `Proc` (`DefWindowProc` without an explicit suffix) alongside its `WNDCLASSW`/`CreateWindowExW` setup and the scene's `UpdateWindowStatus`; this is a lead, not an attributed cause. Do not start a general renderer investigation.

Manual acceptance is **PASS, explicitly reported by Project Control for both centered and tilted cases**: stable settling/support, camera independence, no teleport/explosion, bounded recovery after a brief frame delay and correct final held endpoint. This is user observation, not acceptance inferred from headless tests or static screenshots. The separately observed failed window-status transport remains a bounded follow-up; the user's behavioral acceptance did not explicitly waive that requirement. Both agent-inspected windows were closed after preserving their results. The live-frame tails below remain reported, without an optimization campaign or a claim that every frame meets 6.67 ms.

## Baseline and ownership

- Branch: `codex/contact-episode-servicing`, created from `3e1028339e9e80648ed15c510644d871259c26bd` — `Docs: reconcile landing page with banked M14.19`.
- HEAD, main, origin/main and queried remote main remain that exact commit.
- Parent/tag target: `fc6fbf269d7a8022ce2fe184aea0dfc94716db05`, `m14.19-persistent-bepu-contact-publication`.
- No staging, commit, push, merge, tag operation or milestone assignment. Historical refs are preserved.
- The pre-existing untracked `bepu-local-contact-staging/staging-build-blocker.md` is unrelated and was left untouched.
- SDK 10.0.303; .NET 10.0 runtime installation includes 10.0.12. Qualification is same-build/machine; no cross-platform bit claim.

The new responsibility is one prepared, finite, retained contact episode driven by integer host-duration samples through the existing canonical owner. It uses the qualification-only box/slab and has no free-flight contact entry, real terrain, production collider, force editing, pause/rate editing or departure behavior.

## Source-proven integration gap and correction

Banked M14.19 captures the complete expected canonical clock when binding. Ordinary `AdvanceByHostDuration` after binding changes debt and therefore correctly fails the binding's original authority comparison. The candidate retains that comparison. It adds explicit `AdmitContactHostTime` with an exact next owner sequence, genuine acknowledged receipt, engine/configuration/world/generation/frontier checks and the original complete canonical authority check.

`SimulationClock.PrepareHostAdvance` extracts the existing conversion arithmetic without inventing a new rule; ordinary advancement retains its original mutation guard and behavior. The owner accepts only fixed 1:1, unpaused, zero-remainder clock accounting. Negative/overflow input refuses; zero input is an explicit no-op and does not consume a sequence. Accepted nonzero inputs consume exactly one sequence. Duplicate, stale and out-of-order identities cannot credit debt again.

Preparation/recheck occurs under the existing `ContinuationPublicationPhase`. Fixed accounting commit installs debt/remainder, then fixed acknowledgement installs only the private expected clock and owner sequence. No paired state, revision, history, time cursor, world pose, source epoch or frontier is changed by host credit.

Credit and physical publication have separate linearization points. Credit that already committed remains canonical debt if servicing subsequently refuses. A forced accounting-acknowledgement failure returns `CanonicalCommittedPrivateInvalidated`, preserves committed debt and invalidates private continuation. There is no rollback or blind retry.

## Service and retained-world continuity

`ServiceContactDebt` is a distinct drain-existing-debt path, bounded to four complete intervals. It owns the same phase and checks authority, fixed clock policy, finite endpoint, event boundary, exact debt, history capacity and revision arithmetic before stepping. Each admitted interval follows the existing step/export/M14.19 publication/acknowledgement operation and returns a copied committed observation. Remaining debt is retained. Result status, publication count and copied observation distinguish waiting, budget exhaustion, blockers, completion and committed-but-private-invalidated outcomes.

The M14.19 publisher's admission and fixed-write body were extracted into `PublishPersistentContactInOwnedPhase`; its existing public/internal entry still acquires the original phase. This is body reuse, not a second publication engine. M14.17's certified-clearance implementation was not changed or used for BEPU receipts.

The original T0, source end, moving-origin epoch, fixed frame, configuration and world generation are immutable. Host acknowledgement writes no BEPU body, pose, velocity, manifold or warm-start data. Physical authority advances only through successful M14.19 publication. One outstanding endpoint remains mandatory; host admission and service refuse it without discard. A sound staged endpoint after an ordinary precommit publisher refusal can still retry through the existing publisher.

## Deterministic long-sequence witness

Debug and Release each passed centered and tilted 1,200-interval sequences under integer-partitioned 30, 60, 150 and 240 Hz schedules and a 500,000-tick delayed/backlog schedule. Each was compared at **every committed frontier** with the corresponding pre-funded reference using exact position, velocity, orientation and angular-velocity bits.

- T(n) remains T0 + floor(n × 1,000,000 / 60).
- Exactly 400 intervals of 16,666 and 800 of 16,667 ticks; cumulative duration 20,000,000 ticks.
- Exactly 1,200 StateRevision increments and 1,200 ordered persistent-contact history records; TimelineRevision unchanged.
- Every publication subtracts its exact integer interval. Admitted credit = published duration + retained debt throughout; final debt zero in equal-total synthetic schedules.
- No service call publishes more than four intervals. The backlog schedule drains without resubmitting accepted credit.
- Original episode/value identity and world generation remain unchanged; completion refuses additional input without mutation.
- Moving/tilted case: nonzero T0=1,234,567, origin (1e9,-2e9,3e9), velocity (11,-7,3), rotated frame; 150 Hz live partitions match reference bits and original-epoch reconstruction.

Independent eight-corner/slab geometry and relative velocity checks preserve the accepted 0.020 m penetration, 0.002 m final support/height, 0.06 m/s support-speed and 0.001 m drift bounds. Results were identical across the schedule family:

| Fixture | Final support | Peak geometric penetration m | Final height drift m |
|---|---:|---:|---:|
| Centered | 600/600 | 0.005455926960645563 | 5.960464477539063e-8 |
| Tilted | 600/600 | 0.004164378500802757 | 5.960464477539063e-8 |
| Tilted, moving frame | 600/600 | 0.004164354113830093 | 4.0696466752088156e-8 |

## Failure matrix

Permanent `ContactServicingTests.Refusals` and `TerminalFailures` passed in both configurations and full suites. Existing M14.18/M14.19 refusal tests also passed unchanged.

| Responsibility | Witness |
|---|---|
| Negative/zero duration, maximum-debt overflow, invalid negative debt | Exact typed refusal/no-op and canonical snapshot nonmutation |
| Duplicate/out-of-order host sequence | No second credit; sequence advances only on committed credit |
| Wrong thread/reentrancy | Both admission and service refuse, canonical snapshot unchanged |
| Foreign engine/world/configuration, default/fabricated receipt, altered generation/frontier, old receipt | Fail-closed authority/receipt refusal |
| StateRevision, TimelineRevision, external position, mass/inertia, force/torque, model/definition, rate/pause/clock/debt | Both owner operations refuse; numerically identical state mutation with a changed revision also refuses |
| Insufficient debt/exact boundary | Zero step until 16,666 ticks funded, then exact next 16,667 interval |
| Event before/at target | Refusal, no event processing or reordering; original M14.19 event matrix retained |
| History capacity/StateRevision overflow after successful credit | No private step; already committed credit stays canonical |
| Source end | Exactly 1,200 publications, terminal hold, no additional input accepted |
| Outstanding endpoint/precommit publisher clock conflict | No credit/restep/discard; valid receipt remains publishable |
| Duplicate/consumed publication | No repeated revision/debt/history mutation |
| Disposed/invalidated world and unsafe export | Fail closed; prior canonical credit retained; unsafe continuation invalidated |
| Forced physical acknowledgement failure | Physical state/revision/debt/history commit; distinct terminal result; retry refuses |
| Forced accounting acknowledgement failure | Debt commits, physical state/revision/history unchanged; distinct terminal result; retry refuses |

Canonical snapshots include paired state/properties, clock, revisions, event/pending/history counts and last publication value. Frontier/receipt and end-of-episode checks supplement those snapshots; this is not a byte-level manifold-memory audit. Fixed precommit paths contain no solver writes. No generic failure-injection framework was added.

## Allocation, retained storage and timing

Separate checked ordinary allocation windows preserve the original no-GC helper contract. In Debug and Release:

| Warmed path | Allocation |
|---|---:|
| 1,024 host credit + step/export/publication/acknowledgement/observation operations, after 128 warm operations | 0 bytes |
| 1,024 no-work credit/service calls | 0 bytes |
| 128 four-interval backlog service calls, after 128 warm calls | 0 bytes |
| Deliberate byte[128] positive control | 152 bytes |

No threshold subtraction/tolerance/retry. Region entry/exit passed. Complete-operation results/counts and stable pool size were asserted separately.

Storage uses a conservative upper bound: all managed bytes allocated during fixture setup/warmup, plus BEPU native pool reservation. Managed setup upper bound 952,600 + native pool 458,752 = **1,411,352 bytes**, below 8,388,608. It includes 892,800 bytes of preallocated 1,200-record history payload. This is not process working set. Pool size remained stable.

Three fresh Release processes, in order, each 128 warm and 1,024 measured complete operations under normal runtime behavior:

| Process | Median ms | P95 ms | P99 ms | Max ms | Cold total ms | Result |
|---|---:|---:|---:|---:|---:|---|
| 1 | 0.0189 | 0.0271 | 0.0653 | 0.2692 | 41.2016 | PASS |
| 2 | 0.0188 | 0.0193 | 0.0231 | 0.0520 | 41.2594 | PASS |
| 3 | 0.0207 | 0.0212 | 0.0242 | 0.0493 | 40.5592 | PASS |
| Ceiling | 0.05 | 0.10 | 0.25 | 0.50 | Separate | |

Worst median/P95/P99/max across processes: 0.0207/0.0271/0.0653/0.2692 ms. These are maxima of process statistics, not pooled percentiles. Process 1's largest tail cluster was around measured indices 235–278 (largest 0.2692 at 235); the other two runs had dispersed maxima at 89 and 37. All declared ceilings passed; no extra timing run or attribution was attempted. These timings include the main-thread owner phase and copied result, not display work or cold initialization. There is no second lock or worker synchronization in the candidate path.

## Visible scene and whole-frame observations

`contact-centered` and `contact-tilted` are separate opt-in launcher routes. They use dedicated qualification body/support mesh identities and finite geometry scaled exactly to the solver box/slab. The scene stores copied committed linear/angular values and submits them once per display callback. It holds the last endpoint between publications and on completion/failure; it never reads live BEPU pose for rendering or extrapolates through contact. Camera commands do not enter the engine. There is no display work per solver interval or backlog item.

Resources are prepared before the first live callback establishes the host timestamp. The host accumulator converts Stopwatch counts with integer arithmetic and retains its conversion remainder. No future duration is pre-funded. There is an explicit cold relaunch instead of an in-episode rebuild.

| Release live run, 1280 × 720 | Samples | Median ms | P95 ms | P99 ms | Max ms | Final debt ticks |
|---|---:|---:|---:|---:|---:|---:|
| Centered, inspected | 3,589 | 5.5545 | 5.6862 | 5.8962 | 36.0642 | 339 |
| Tilted, no in-episode UI capture | 3,590 | 5.5550 | 5.6891 | 5.8800 | 40.4358 | 5,279 |

Both reached exact clock 20,000,000, frontier/revision/history 1,200 and retained the final endpoint. Residual live debt is legitimately admitted host time past the finite final boundary, not lost time; it is held, not serviced beyond SourceEnd. Frame measurements are callback-to-callback including present and host scheduling, **not GPU-only execution**. Median/P95/P99 meet the approximately 6.67 ms objective; maxima do not. No claim of uniformly 150 FPS, no tail diagnosis and no renderer optimization follows from these observations.

Observed cold live preparation: centered setup/world/binding+presentation 10.7581/29.5731/2.0316 ms; tilted 10.4005/29.3538/2.0501 ms, all before host sampling. Native renderer creation is also before that sampling boundary but was not separately attributed here. Presentation adds a bounded 800,000-byte frame-observation payload and two unit-box GPU meshes with 2,016 logical vertex/index bytes (2 × (24 × 36 + 36 × 4)); driver reservation and total managed presentation retention were not separately measured. These costs are separate from the contact-world storage bound.

The rendered bodies/slab were visible in agent post-episode screenshots. The complete behavioral manual witness comes from Project Control's explicit PASS, recorded in [manual acceptance](manual-acceptance.md). The independently observed title/status failure remains the declared stop.

The user also produced manual-run logs after the agent's two runs. The tilted manual log records completion at frontier 1,200, exact clock 20,000,000, debt 2,657, and frame median/P95/P99/max 5.5557/6.2106/6.4575/35.7652 ms (3,591 samples). The centered manual log contains readiness but no terminal line; the independent agent-centered log above supplies its terminal numerical witness, and the user explicitly supplies centered manual PASS. These user runs are distinct from the three declared contact-operation timing processes; no agent performance retry was performed.

## Validation record

- Debug and Release full solution builds: PASS, zero warnings/errors. Native Debug/Release builds also PASS.
- New cheap then full focused servicing: PASS Debug/Release.
- Full Simulation: **51/51 PASS Debug and 51/51 PASS Release**. No retry or skipped group.
- M14.18 and M14.19 focused tests: PASS Debug/Release, including original nonpublishing mode and receipt/refusal/terminal contracts.
- M14.17 Graphics cases `Certified Florida continuation publication`, `Certified continuation acceptance gaps`, `Certified continuation allocation matrix`: PASS Debug/Release.
- ReferenceFrames, Precision, verified BEPU dependency inspection: PASS Debug/Release; dependency DLLs and manifest untouched.
- New copied-presentation/lifecycle case: PASS Debug/Release. Launcher: 16/16 PASS in each configuration, including existing/default routes.
- Presentation-only title update and early-preparation disposal changes were followed by both full solution builds and affected presentation cases. Simulation source/binaries remained identical to the numerical/performance qualification; no gratuitous repeat of those campaigns.
- One launcher invocation initially used the wrong output TFM directory (`net10.0` instead of `net10.0-windows`); it did not execute tests. Correct paths passed. This is distinct from the material visible-status failure.
- `git diff --check`: PASS. All changes unstaged.

## KSA provenance and boundaries

Current local KSA binary: version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256 `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`. Inspected `PhysicsBubble.Prepare`, `ApplyFullPhysics`, `InitializeConstraintSim`, ConstraintSim and game-owned apply lifecycle in the retained local decompilation. The official-history witnesses retained in [M14.18 banking review](../bepu-local-contact-staging/m14.18-banking-review.md) cover persistent arenas, shared shape storage and collision-aware timestep selection.

**ADAPT:** game-owned service lifecycle, retained solver resources, ready/apply ownership and copied game-state presentation. **INTENTIONALLY DIFFER:** exact integer lattice, canonical debt, revisions/history, strict source authority and same-phase acknowledgement. No KSA revision/debt/history behavior is invented, and no wider situation/vehicle/pooling/rebasing system is copied.

## Reproduction, artifacts and next boundary

See [launch/manual procedure](manual-acceptance.md), [qualification commands](reproduce.md) and [candidate identity](identity.json). Permanent tests contain the workloads, independent witnesses and deliberate controls; there is no disposable comparison tree. `validation.json` retains the concise machine-readable results. Raw live logs remain only in ignored `build/contact-episode-servicing-live` while the visual blocker is active: eight files, 78,579 bytes, including four user-manual log files. They are disposable after the bounded follow-up is resolved. No pre-existing scratch was deleted.

Recommended next responsibility is limited to the visible contact-status transport and its verification, with the user's manual acceptance retained and the reported live-frame tails available to Project Control. No new contact architecture, allocation forensics, renderer optimization or milestone work is authorized by this report.

**UNBANKED. STOP FOR PROJECT CONTROL.**
