# Canonical spacecraft command contract

This candidate implements command authority and a non-actuating copied consumer. It does not wire a device, scene, engine, RCS jet, force, torque or contact actuator. The accepted [architecture investigation](../spacecraft-control-architecture/command-authority-contract.md) remains the provenance; no new KSA investigation or dependency work was performed.

## Owner and finite boundary

`SimulationTransactionEngine` owns private `CommandStorage` under its existing `ContinuationPublicationPhase`. The returned `SpacecraftCommandAuthority` is an immutable capability and finite schedule description. Reference identity binds it to its issuing engine; a fabricated or foreign capability refuses. Mutable arrays, revision and sequence fields never escape through the capability. Preparation is cold, single-vehicle and nonrenewable. A second preparation refuses instead of resetting duplicate or lease history.

Prepare before the episode's first interval, at its original canonical origin T0. The explicit first-slice schedule is B(n) = T0 + floor(n * 1,000,000 / 60), through the caller's finite interval count. This command schedule has no contact-regime branch and does not own an alternative clock.

The funded horizon comes from `SimulationClock.TryGetPendingSimulationDebtTarget`: H = current canonical time + retained debt, with checked wide arithmetic. Select the earliest B(n) >= H whose index exceeds the last explicitly closed boundary. An integer inverse uses ceil((H-T0)*60/1,000,000), then the closed-boundary minimum. The accepted epoch and sequence are immutable. Normal credit increases H; physical debt consumption preserves it. An unrelated regression below the last admitted H refuses.

At an open current boundary, `CommitNextSpacecraftCommand` consumes at most one eligible request. Drain its bounded queue before closing the boundary. Zero debt is valid: this operation changes no clock, debt, physical StateRevision, TimelineRevision or physical history. No zero-length physical step occurs. NoCommand polling leaves the boundary open.

`CloseSpacecraftCommandBoundary` explicitly freezes the snapshot immediately before the next interval's consumer evaluation. Success is BoundaryReady with copied requested demand. An already closed boundary returns BoundaryClosed with no ready demand. Closing is an owner decision, not a display callback or no-work poll. A later input at zero debt after closing receives the next boundary. End-of-source demand is only a non-actuating observation; no new physical interval is authorized.

The caller coordinates consumption at each exact endpoint with the existing single-step/private-publication APIs. The banked four-interval contact service is unchanged. It is unsafe to wrap that batch blindly with only before/after command consumption: crossing an accepted E returns MissedBoundary, never a retrospective commit. This slice does not add a live scene or modify the banked batch service.

Timeline events at/before E block admission and consumption. Closing also checks the current event boundary. The command path never schedules, consumes or rewrites a timeline event. Existing physical admission still governs subsequent physical intervals.

## Bounded ingress and ordering

Seven ordinary slots admit one complete supported burst: ignite, shutdown, six-axis snapshot, absolute throttle position, RCS enable, mode and target. One extra reserved slot admits control-loss neutralization even at ordinary saturation. This is a minimum complete-burst admission guarantee, not an assumption about device polling frequency. A second burst can explicitly saturate; accepted commands are never overwritten. Each operation scans/moves at most eight value entries.

The one lease has engine-scoped identity 1. Input must carry that lease and exactly the next UInt64 sequence. Duplicates/stale identities refuse; gaps refuse; exhaustion never wraps. The producer retains accepted identity rather than turning key repeat into fresh ignite requests. Ordering is (assigned E, sequence), with no command-class precedence. Ignite then shutdown at the same E remains two ordered requests.

Revocation seals ingress immediately and queues a separately ordered prospective neutralization. Earlier accepted commands, including equal-E entries, remain applicable. In this nonrenewable lifetime, monotone H and closed-boundary state make revocation the final ordering key; no legal later old-lease command exists to cancel. This is not a general lease transfer/cancellation/reacquisition implementation. Administrative refusal remains explicit; producer-side focus loss can cease submission independently.

No unbounded result/history storage exists. Every operation returns its copied outcome; the caller may retain it. Old duplicate identities return DuplicateOrStaleSequence rather than reconstructing an unbounded original receipt. Tests retain external histories only.

## Lifetimes and values

| Input | Canonical requested lifetime |
|---|---|
| Ignite/shutdown | Each fresh accepted edge records its kind and sequence exactly once. This is not engine-running evidence. Repeated observation preserves the same historical identity; it does not emit a new edge. |
| Pitch/yaw/roll and translation | One six-axis body-frame snapshot, components in [-1,1]. Held until replacement, zero release or prospective neutralization. X/Y/Z rotation denote pitch/yaw/roll. |
| Throttle | Absolute normalized requested position in [0,1], latched. This first slice intentionally has no held throttle-rate input or integrator: no rate model/gain is needed for the non-actuating contract. A future rate adapter must be separately specified; no display-dt evolution occurs here. |
| RCS and mode | Latched requested enable; Manual, RateAssist or AttitudeAssist. No physical SAS/actuator behavior or capability success is implied. Mode and typed target are orthogonal requested facts. |
| Target | None, desired body-to-root attitude, angular rate expressed in the named root frame, or canonical attitude capture. Foreign frames and nonfinite/degenerate values refuse. Other-frame conversion is not implicit. |
| Hold/capture | Read coherent canonical motion at effective E during preparation, never render/admission pose. |
| Control loss | Prospectively neutralize held axes, assist mode and target. Latched throttle/RCS and historical engine requests are not silently rewritten. No instantaneous physical cutoff through backlog is promised. |

Attitude inputs are validated on admission, retained unchanged as immutable requests, and normalized with the existing evaluator during prepared consumption at E. An exact copied committed target remains unchanged. Crucially, preparation sees the deterministic predecessor command state, not whichever state happened to exist at admission. The regression with (1,2,3,4), its normalized value, and interleaved admissions protects this distinction without a tolerance.

CommandRevision is UInt64, separate from physical/timeline revisions. Each actual state transition increments it once. No-op held/latched/target/release requests consume their accepted sequence without increasing it. A fresh edge changes the request identity even if its kind repeats. A no-op retains original state bits, including signed zero. Revision overflow refuses before queue removal or state writes; no-op consumption remains possible without an increment.

## Atomicity and observation

Owner/thread, non-reentrancy, capability, active 1:1 unpaused clock, source, horizon, sequence, capacity, event and payload checks precede admission writes. Consumption additionally requires exact current E, prepares capture/target/state/revision, and performs a final applicability check before fixed bounded state/queue/bookkeeping writes. There are no callbacks or solver/force operations inside this phase. A narrow prepared-refusal seam and revision-overflow seam exercise atomic refusal; neither is a configurable runtime failure framework.

The copied observation separates canonical command facts from live ingress diagnostics. State, CommandRevision, transition E and consumed identity are deterministic command history. PendingCount, LastAcceptedSequence, NextPendingEpoch and IngressRevoked can legitimately differ when future inputs are admitted earlier; they are not physical or committed-command history. The replay fixture preloads the same manifest, so its entire result sequence also matches.

`ConsumeWithoutActuation` is a pure copied projection into `RequestedControlDemand`. It has no hardware-success, thrust, wrench, propellant or solver state. Values contain no references. Unsupported pause/warp is an explicit operation refusal; observing the last copied requested state remains allowed. The same owner works with the free-motion test subject and retained article. No contact transition, command transfer or implicit camera ownership is introduced.
