# Engine definition, handoff and proposal contract

## Ownership and capture

The existing exclusive `SimulationClock.PublicationPhase` and transaction engine remain the sole owner. No second lock or clock is introduced. `BindSingleEnginePreparation` is optional and cold: one immutable engine definition and one exact command authority, before any command is consumed or a boundary closed. Late binding refuses rather than reconstructing missing edges. A second binding refuses even if values are identical; stable definition identity/version describe values, while the issued capability establishes ownership.

The composed first-slice lag budget is **at most seven unconsumed engine edges**, one maximal ordinary command burst. This is an explicit supported bound, not an assumption that an eight-slot command queue limits lifetime commits. The seven-element bridge spans drain/refill cycles. `CanReserveEngineTransition` counts captured plus pending relevant edges at admission. `CanCaptureEngineTransition` rechecks capacity before command commit. `CaptureEngineTransition` appends the exact sequence, effective epoch, kind and resulting CommandRevision during the existing fixed command commit. Later command writes cannot erase the record. No event/history allocation occurs. Standalone M14.22 without a bound engine follows the same banked path with no added refusal.

Capacity preflight prevents a full captured prefix from stranding an already-admitted due edge in front of boundary closure. An additional relevant input returns Capacity before acceptance; no accepted edge is discarded. The commit guard also fails before writes. Nonengine inputs, including neutralization, do not use these slots. Edge sequences can have valid gaps from other command types. Repeated Ignite/Shutdown edges still carry their individual sequence and revision.

## Definition and SI mathematics

Versioned ideal model: one fixed mount relative to canonical COM in the spacecraft body frame; fixed nominal body thrust axis; Off/Enabled latch; immutable hardware availability; instantaneous normalized [0,1] throttle, minimum zero; restartable without a consumable start. There is no spool, delay, gimbal or controller allocator.

`IdealEngineDefinition` admits nonzero identity/version, finite mount/axis, positive finite maximum thrust (newtons) and effective exhaust speed (metres/second). Axis normalization first divides by the largest absolute component, then by scaled norm, avoiding square overflow/underflow. A nonzero finite axis at the subnormal or maximum finite limit remains valid. There is no magic clamp. No redundant Isp or gravitational constant is authored.

For available Enabled hardware: `u = requested throttle`, `T = u Fmax`, `Fbody = axis T`, `momentBody = mountFromCom × Fbody`, `requiredFlow = T / exhaustSpeed`. Arithmetic is FP64; all output components and flow must be finite before sealing. Impossible moment/flow overflow refuses while preserving the entire pending range.

Off keeps requested throttle but has zero proposed realized throttle, force, moment and flow. Ignite sets the private proposed latch Enabled even when unavailable; `Unavailable` remains a separate hardware observation with all physical outputs zero and accepted request provenance retained. Zero throttle with Enabled is inactive output, not Shutdown. Preview field `ProposedActivity.Firing` is conditional under `ProposedIfApplied`, never canonical actual firing.

Independent fixtures use 1,200 N and 3,000 m/s, mount (0,2,0) m and +X axis. At quarter throttle: T=300 N, F=(300,0,0) N, moment=(0,0,-600) N m and required flow=0.1 kg/s. Tests vary zero/full/fractional/tiny throttle, COM placement, mirror mount/axis, subnormal/maximum axes, maximum finite valid authored values, unavailable hardware and nonfinite/overflow inputs. These fixture values are not production constants or a spacecraft engine asset.

`AvailableFeedRequiredFlowOnly` says only what feed would be required. No sufficiency, reservation, consumed mass or exact exhaustion is implied.

## Interval and authority

Preparation reuses the issued command authority's original exact schedule: `T(n)=T0+floor(n*1,000,000/60)`. It requires the current canonical boundary closed, exact next target, finite source lifetime, supported 1:1 unpaused clock, contiguous preparation frontier and no event at/before target. Both stored physical segment epochs must be at/before start. It does not propagate a body, evaluate force or qualify a future physical solution.

Source sealing includes the issued engine/command/definition capability, spacecraft/body/root frame values, definition identity/version, exact start/end/index, physical StateRevision, TimelineRevision, CommandRevision, committed command sequence, preparation cursor range, ordered edge witness, latest requested edge/sequence, source paired segment/configuration/mass values and full clock/debt accounting. Private object identity is the unforgeable owner seal; copied provenance is deterministic value data, not an authority token.

The snapshot's last-edge sequence only checks bridge completeness. The entire pending ordered record list actually drives the latch. A deterministic FNV-style value fingerprint records sequence, epoch, kind and revision order; it is evidence only, not authorization. Counts and before/after cursors remain explicit.

`COMMAND COMMITTED CURSOR` and `PreparationConsumedSequence` are separate. On final successful applicability recheck, fixed private writes store the preview/source, proposed latch, consumed cursor, boundary and one active slot, then clear consumed records. This is preparation progress only. It writes none of physical state, clock, debt, revisions, history, canonical hardware, fuel or BEPU. The current ticket explicitly authorizes this evaluation-cursor consumption; it is not a future physical acknowledgement.

Future command admission alone does not stale a frozen closed source prefix. A changed committed source, physical revision/paired state/configuration, timeline or clock/debt makes preview stale. All reads/operations validate owner thread and reject reentrancy.

## Lifetime and failures

Exactly one live proposal is allowed. Foreign/default/fabricated/private-seal mismatches, wrong or older boundary, retirement and stale source refuse. The opaque capability includes no globally nondeterministic publication identifier; equal source facts produce equal copied preview/provenance, while capabilities remain intentionally distinct across issuers.

Explicit `DiscardSingleEngineProposal` releases an issued active slot, including a stale one. It neither applies output nor rolls back the evaluation cursor/latch nor resurrects edges. The same sealed interval cannot be prepared again. The next contiguous interval can prepare an empty edge range using private preparation continuity. Skipping an unprepared interval refuses. No canonical engine trajectory is created by this lifecycle.

Before sealing, invalid input/source, capacity, arithmetic, pending event, interval, owner, open-boundary and forced final refusal paths preserve relevant canonical and private state. A later valid preparation sees the same pending transitions. The narrow internal test seam returns at the final prepared boundary before bookkeeping; it is not a general failure injection framework.

## Rotation and application boundary

The proposal stores BODY-frame force/moment and mount/axis identity. It contains no `ForceRoot` or single world force claimed valid through rotation. A 90-degree rotating-body oracle shows that the future root wrench must change even though the prepared body demand remains constant. Future dynamics must perform its own qualified body-to-root transformation through motion, physical applicability and resource/commit checks. Copied preview values are not permission to apply stale output.

## Permanent coverage map

| Required areas | Permanent proof |
|---|---|
| 1–7 definition, values, identity/version | `SingleEngineActuationTests.Definitions` |
| 8–16 edge order, lost-snapshot witness, independent cursors, saturation | `Capture`; command prepared-refusal no phantom capture |
| 17–25 Off/Enabled/unavailable and analytical wrench/flow | `Mathematics` |
| 26–28 canonical/command/clock/debt nonmutation | `Fixture.Prepare`, `Authority`, article snapshots |
| 29 BEPU/contact nonmutation | `EngineeringContactArticleTests.EngineProposalNonActuation` |
| 30–34 issuer/source/interval/duplicate/refusal | `Authority`, arithmetic refusal in `Mathematics` |
| 35–37 deterministic proposal/pacing/admission timing | `Determinism`; unchanged M14.22 30/60/150/240/delayed stream regression |
| 38 rotating body protection | Independent rotation oracle and body-only output contract |
| 39–41 copied isolation/requested/proposed/conditional | Prior copied preview remains unchanged after next preparation; no reference-containing preview fields |
| Six zero-allocation paths and positive control | `SingleEngineActuationTests.Allocation` |
| M14.21 unpowered centered/tilted/moving-bit equality | Three independent 1,200-interval comparisons per configuration |

The implementation changes three production paths only: two new actuation/preparation files and three narrow command-owner hook lines. No public time, command ordering/revision/lifetime, contact article, solver, publisher, renderer or dependency implementation is replaced.
