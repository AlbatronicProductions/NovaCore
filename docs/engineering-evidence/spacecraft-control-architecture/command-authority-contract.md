# Proposed command authority contract

Proposal for Project Control, not current production behavior. Read with the [source map](current-source-map.md). Initial implementation scope is one vehicle, 1:1 host conversion, bounded prepared storage and command consumption without actuation. Physical engine/RCS control is a later qualification boundary.

## Responsibility and ownership

Input observes devices and resolves actions. It cannot write the clock, canonical pose, force, torque, solver state or command state directly. The simulation transaction owner accepts value intents addressed to a vehicle and an owner-issued control lease/generation. It owns one globally increasing sequence for that bounded owner lifetime (never reset on lease changes), assigned effective epochs and a bounded ordered queue, then installs the canonical command snapshot. Camera selection and camera movement cannot acquire/revoke control implicitly.

Proposed command storage contains only deterministic values: vehicle/lease identity, consumed sequence, command revision, engine request/throttle, held rotation/translation and throttle-adjustment demand, RCS enable, assist mode and typed targets. Separate prepared command-result/history storage records accepted, applied, declined and canceled identities. Its dedicated **CommandRevision** is proposed new state, not an existing NovaCore field. Applying an intent does not increment the physical StateRevision or append a persistent-contact physical history. Do not disguise command admission as a marker/state mutation. Physical publication retains its existing exactly-once StateRevision/history responsibility.

The existing transaction owner and phase govern both domains. A new command record is not a second clock or independent publication lock. Later dynamics admission must explicitly bind the consumed CommandRevision/actuator-state revision to the prepared endpoint and private expectation acknowledgement. A separate command revision must never be used to hide a physical force/configuration change from retained-world checks.

## Exact prospective epoch assignment

The following rule is concrete for the initial constant 1:1 scope. It avoids retrospective input over backlog without inventing host-event timestamp calibration.

1. The host boundary submits elapsed integer duration through the existing authorized accounting path. Accepted credit is committed separately and remains debt even if the following intent cannot be admitted. A refused or duplicate host sample cannot be disguised as accepted credit.
2. Before servicing more physics, the same owner admits the newly observed action batch against one coherent snapshot. Compute the checked funded horizon **H = canonical clock + retained debt**. No wall time, display frame index or float delta becomes an authoritative epoch.
3. The initial control boundary set is the original contact lattice **B(n) = T0 + floor(n x 1,000,000 / 60)**, including B(0). Use checked wide arithmetic and a bounded integer inverse/search. The owner records the last boundary closed to further command admission. Choose the **smallest n** for which B(n) >= H, B(n) is not before current canonical time, and that boundary is not closed. The resulting **E = B(n)** is assigned once and never moved when more host time is credited.
4. A published endpoint opens its boundary for commands; closing happens immediately before evaluating control/actuation for the following physical interval. It is not closed merely because a display callback ended. No concurrent producer can change the boundary after the owner takes its evaluation snapshot.
5. At an open E equal to current time, command-only consumption may occur with zero debt. This requires an explicit owner operation: generic debt servicing returns NoDebt, while retained contact servicing returns AwaitingDebt; neither supplies a command-only consumer. No artificial tick or positive host duration may be created to consume an intent. Control integration/actuator evolution/physics still receive zero elapsed time and do not run.
6. Commands for a future E wait. All intervals before E retain the previously authoritative command state. A held setpoint effective at E drives subsequent admitted intervals until superseded; it never changes the already-solved endpoint at E.
7. Order one accepted stream by (effective epoch, owner sequence). Multiple commands at E are handled in sequence, each with its own outcome. A recorded effective epoch plus sequence/value/lease is the replay input. Do not replay arbitrary UI arrival timing.

Examples with T0=0: at H=10,000 the next eligible E is 16,666; at H=50,000 an open B(3) permits E=50,000. If that boundary is closed, select B(4)=66,666. If H=0 and B(0) is open, an intent can change command state at 0 without stepping physics. With 500,000 ticks of debt, new input cannot control the earlier thirty intervals; it waits for its assigned boundary. This introduces explicit prospective latency under backlog. It does not promise immediate physical cutoff of previously admitted work.

For later free-flight use, the owner must install an explicit control-boundary schedule and relation to force/event boundaries. Do not assume the sample's 50,000-tick SAS cadence and the contact 60 Hz lattice are interchangeable. Retain a regime's original schedule origin; no display frame or publication resets it. The first candidate uses one existing lattice and does not design general adaptive control scheduling.

## Event order and finite-frontier rules

The existing canonical timeline remains authoritative. Ordinary timeline events at/before the next target block contact stepping/publication; the command path cannot consume, reorder or bypass them. At a command boundary, due canonical events must be resolved by the already-authorized owner policy before command/control evaluation; under today's retained contact contract that means an explicit blocker, not running those events through a new hidden loop.

Do not simply schedule every player command in the current contact timeline: its revision is captured readonly, so even a later inserted event makes that episode stale. The selected architecture uses bounded owner-owned intent ingress, with the same owner checking timeline boundaries at admission and consumption. This does not invent a second event authority. A future full merge with the canonical event stream would require a separately qualified timeline/expected-authority contract.

If E lies beyond SourceEnd, refuse admission with an explicit exhausted-frontier result; no wrapping or resetting T0. At SourceEnd, command state may only produce an explicitly non-actuating terminal observation, not another episode interval. If an admitted command reaches an unresolved event/blocker, report PendingBlocked; do not retime it, silently discard it or claim its actuator effect happened. Shutdown/decrease requests are not exemptions from event, owner, stale-state or arithmetic checks. Visible accepted/pending/refused status is required.

## Bounded ingress, exactly-once and retries

Ingress and command-result capacity are prepared before readiness. No unbounded queue and no per-input allocation in the warm path. Capacity is sized for the later declared bounded scenario, not copied from KSA's initial array length or arbitrarily guessed here. Exhaustion returns an explicit refusal and stops further admission; it does not overwrite oldest edges. Preserve a fixed reserved administrative slot for lease revocation/neutralization, and define refusal if the owner itself is terminal. Refusal never pretends an edge was accepted.

A valid new submission has the current ingress lease and exactly the next expected owner sequence. Duplicate or stale sequence does not reapply; report duplicate/already processed or stale according to the bounded retained-result policy. A skipped/foreign sequence refuses. Retryable inputs retain their identity and original outcome; accepting a later input is not permission to resubmit an earlier edge as new. Exactly-once applies to **accepted identity and result**, not every possible device observation or guaranteed combustion.

No cross-command edge coalescing. Repeated equal held snapshots may be treated as explicitly recorded no-change outcomes, but must not erase an intervening release, mode transition or lease change. Normal toggles are converted to a sequenced operation applied once; explicit SetMode/SetEnabled is preferable for UI buttons and replay.

## Command lifetime table

All rows are sampled by action/UI adapters, stored by the same simulation owner and consumed at assigned E. No row is a renderer-owned control variable.

| Command | Canonical form and lifetime | Clearing/replacement; absent sample or stall | Contact, warp and publication |
|---|---|---|---|
| Ignite | Ordered edge updates requested engine-running state for an explicit engine/group identity | Latch survives no new frame; shutdown/availability policy may alter realization; repeated identity never reignites | Allowed intent while supported only after hardware/control admission qualifies; copied request is separate from actual ignition/thrust |
| Shutdown | Ordered edge requests shutdown, with a typed hardware outcome | Not a universal immediate zero-thrust promise: accepted engine model may have minimum pulse, shutdown limits or nonshutdownable solid motor | Cannot bypass event/backlog/terminal frontiers; pending/refused remains visible; no invented emergency authority |
| Throttle position | Finite bounded absolute latched setpoint, not thrust | Persists until next accepted replacement; unavailable engines may realize zero | Same ownership on contact/flight; display shows commanded and realized separately |
| Throttle up/down | Held adjustment-rate demand; integrates into throttle using canonical elapsed duration and declared model limits | Opposing inputs cancel; release/revocation removes adjustment; no sample preserves current held demand; zero simulation duration gives no ramp | Initial 1:1 only; canonical-dt timing intentionally differs from KSA player-dt ramp; no copied 0.7 gain |
| Pitch/yaw/roll | Held dimensionless manual intent in declared control axes; manual mode requests actuator allocation, assisted mode requests declared rate/target changes | Persists across steps; explicit release/replacement/lease revocation; no one-frame expiration | Physical torque must be realized by accepted hardware; contact cannot be resolved by overwriting orientation |
| Forward/back, lateral, vertical translation | Held translation intent in explicit body/control frame | Opposite directions cancel; release/replacement ends it; no implicit camera frame | RCS enable/availability and plume/force model determine realized force; no magical acceleration |
| RCS enabled | Latched enable state; applies to translation and rotation authority | Changes only by accepted command/declared owner lifecycle | Disabled means no new RCS demand; any already-realized minimum pulse obeys physical model, not retroactive deletion |
| Assist/control mode | Ordered change to latched mode | Deterministic manual override, explicit Off/Manual and target reset rules; no hidden scene mutation | Possession and contact state are separate; unsupported constrained assist refuses visibly |
| Hold current attitude | Target command captures copied canonical orientation at E in a declared reference frame | Replaced/canceled explicitly; never capture render pose or KSA rate-hold by mistake | Target may persist across contact loss; validity/actuator authority still rechecked |
| Desired attitude/angular rate | Typed target with frame identity, epoch, finite values and mode | Persists until replacement; invalid frame/model fails closed | Rate hold and orientation hold are distinct; solver remains pose authority |
| Desired translation rate | Future typed target, not part of first manual/RCS scope | Must specify reference frame/target lifetime/controller before admission | Do not invent a rate controller simply because KSA offers direct/pulse translation |
| Focus/device loss or control transfer | Sequenced owner revocation/neutralization plus ingress lease generation change | Immediate ingress rejects new stale/unadmitted events; canonical held effects change at assigned E; unapplied old-lease commands after the revocation ordering key are canceled with outcomes | Pending neutralization is observable; no claim of instantaneous physical neutralization through old backlog |

Short ignite/shutdown or press/release pairs at one boundary can result in zero physical burn. Each edge is still accounted for once. Actuator realization, not event count, determines actual impulse.

## Control state machine

Use orthogonal bounded concepts rather than GroundController versus FlightController:

- **Possession:** no lease -> owned lease -> revoked/suspended/terminal. Grant and handoff are owner-authorized, sequenced and target a stable vehicle identity. One owner at a time.
- **Control mode:** Manual, RateAssist, AttitudeTarget, with explicit availability. Do not invent a full autopilot framework for the first candidate.
- **Physical execution regime:** retained contact, qualified free flight, transition pending/blocked. Exactly one dynamics owner consumes the realized wrench for each vehicle/interval.

Contact loss changes the last axis only after a qualified transition. It does not clear throttle or transfer player ownership automatically. Grounded ignition is conceptually valid because contact can oppose propulsion; it is not currently implemented. Constrained assist must be qualified for available torque, saturation and controller-state behavior; a free-flight controller may chatter or push against constraints. Manual/Off remains an explicit mode, not a pose reset.

Lease cancellation is prospective. Its ordering key is R=(effective epoch, owner sequence). Cancel only unapplied old-lease commands whose (epoch,sequence) key is after R. Earlier-sequence commands at the same epoch retain their committed outcomes; no history is rewritten. Previously admitted old-lease commands before R are validated against their historical lease applicability, not rejected simply because ingress now uses a newer lease. Immediately rejecting stale ingress does not cancel the prior held snapshot in already-funded [current,E). Do not promise both nonretroactive backlog and instantaneous physical neutralization. If a future safety intervention must interrupt previously admitted work, it needs a separately named and qualified owner operation and replay record. This assessment does not authorize it.

## Six cadence boundaries

| Cadence | Current KSA | Required NovaCore boundary |
|---|---|---|
| Display | Main prepare/render loop | Display copied observations once per display frame; camera only affects presentation |
| Input poll | GLFW polled after worker dispatch; UI event buffers drain next preparation | Polling may be per frame; reports intent and lease, never physical dt or authority |
| Host service | Main applies completed work and dispatches next worker frame | Credits integer debt then bounded service; frame delay retains backlog |
| Canonical simulation | Floating simulation/player times and worker snapshots; no inspected integer command log | Exact epoch/sequence command consumption, independent of callback count |
| Physics | Adaptive worker substeps; constrained BEPU steps <=1/60 s | Existing exact admitted lattice for retained contact; no dt enlargement to catch up |
| Publication | Worker results applied back to vehicle/parts/flight computer | Exact paired endpoint commit plus private acknowledgement; command-only results remain distinguishable |

Controller evaluations are tied to declared canonical boundaries, not render callbacks. One admitted held setpoint can drive multiple intervals; each interval re-evaluates control/actuators as declared, while an ignition edge is not re-executed per step. Output pulse/timer identities prevent repeated impulse application. The same accepted command manifest must reproduce under different service partitions. Different polling can observe different human input; this design does not claim otherwise.

## Warp and pause

Initial active gameplay command/control qualification supports **1:1 only**. It cannot inherit arbitrary general-clock rates, the old SAS sample's 10x cap, KSA's 30x gate or inverse-square-root manual-effect scaling. Unsupported active rate/pause changes are explicitly refused through the owner; unrelated external changes retain stale-authority failure. Never silently drop a held command or edge because rendering sped up.

Future higher-rate control requires qualification of canonical controller/actuator cadence, physical pulse duration, force evolution and per-call budgets. A valid policy may explicitly change to a qualified coasting mode at a canonical boundary before warp; this ticket does not qualify such a mode. Commands to decrease throttle or shut down should remain observable as accepted/pending/refused according to capability and frontier rules, not promise an impossible immediate effect. No automatic reset or warp-dependent physical scaling is selected here.

Current NovaCore Pause blocks new host funding, **not every explicit time advance**; its general debt orchestrator can drain existing debt while paused. Contact servicing separately refuses paused clocks. A later gameplay pause contract must explicitly suspend automatic gameplay servicing and controller integration while preserving legitimate explicit administrative time commands. Command-only bookkeeping at a fixed epoch must never create positive dt. Resume/handoff uses a fresh ingress lease and must not resurrect old held input or stale ignition. Active pause/resume is outside the first candidate.

## Realization and publication linearization

Three distinct facts must be observable: input accepted for future E; canonical command installed at E; physical endpoint committed after an admitted interval. A later event/history/debt blocker does not undo previously accepted credit or committed command state.

Future control/actuation must prepare controller state, engine/pulse timers, availability, propellant/mass changes and realized wrench for the exact interval, together with the private step. One pending endpoint owns those staged results. Ordinary publication refusal preserves a valid pending stage without re-solving, reapplying ignition or consuming propellant twice. Terminal pre-step failure changes no physical state. Canonical commit followed by failed private acknowledgement leaves canonical state committed and private continuation invalidated, as today; the returned observation must say so.

This extends responsibility beyond a single force field. No direct mutation of captured acceleration, global revision suppression, arbitrary source rebinding or per-frame BEPU reconstruction qualifies. Mass/force/torque changes require an explicit prepared admission and matching authority update. M14.17 clearance evidence remains separate.

Copied observations should expose vehicle/lease, assigned/pending/applied command sequence and epoch, CommandRevision, engine request/throttle, RCS/assist/target, availability/decline reason, realized actuator activity and wrench over its stated interval, and canonical pose/velocity/clock/state/timeline/publication identities. Never label a UI requested throttle as realized thrust. No live BEPU handle or mutable canonical reference is a rendering contract.

## Qualification obligations, not measurements

Bounded queue/history/control/actuator work; exact zero warmed allocation with a deliberate control; separate normal-runtime timing; same command manifest under 30/60/150/240 Hz and delayed service; every endpoint and command result compared; events at/before boundary; stale/duplicate/foreign lease; overflow/capacity; command-only zero-debt path; E tie/rounding; no retiming under later credit; revocation latency/cancellation; refusal after prior committed credit; physical/controller/actuator pending stage and terminal acknowledgement failures. No speculative benchmark was run.
