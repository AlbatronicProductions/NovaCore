# One physical interval, one canonical successor set

Proposed specialized powered-interval publication under the existing transaction owner. No code is implemented. M14.17 certified clearance and M14.19 unpowered retained-contact admission remain distinct.

## Canonical state versus metadata

| Responsibility | Authoritative future state | Revision / update |
|---|---|---|
| Paired motion | Exact represented x,v,Q,omega at canonical target; explicit endpoint-only model validity | StateRevision increments exactly once per physical publication |
| Mass law | Rounded exact-law total mass; fixed COM and dry inertia in v1; immutable mass-law/dry-body identity | Installed with paired endpoint; no separate mass transaction/revision needed |
| Resource | Exact remaining units behind one stable owner-issued capability; initial amount/definition remain immutable | ResourceRevision increments once iff exact consumption>0, even if rounded mass bits do not change |
| Actual actuator | Latch, availability/feed/activity, actual endpoint output, last applied boundary and consumed command cursor | ActuatorRevision increments once per applied interval, including unchanged throttle when its applied frontier advances |
| Requested commands | M14.22 requested state/revision and captured ordered edges | Existing CommandRevision semantics only; physical commit does not increment it |
| Clock/accounting | Exact target, retained debt minus exact whole integer interval | Preserve rate, remainder, pause and other clock fields |
| Timeline | Existing pending events/order | TimelineRevision unchanged; exhaustion creates no synthetic queue event |
| History | One deterministic joint interval record and next history index | Exactly one append; no second resource/actuator history stream |
| Private continuation | Private evaluated/solver state, pending receipt/lease, acknowledged expected authority/frontier | Not canonical physical authority; generations are replay protection, not deterministic global history IDs |

Interval powered duration, demand, exact exhaustion offset and realized-in-interval output are history/observation facts. They are not instantaneous endpoint throttle. On endpoint/interior exhaustion, actual output at target is zero, but the record retains the preceding powered duration.

**Resolve prior evidence chronologically:** the older actuator architecture proposed Off-on-feed-loss. The later M14.24-banked `finite-propellant-architecture/resource-authority-contract.md:86` explicitly chooses **Enabled + NoFeed**, with no starvation-induced Off transition. Preserve that later model. No fuel restoration is implemented; a future restoration would not replay ignite edges. A different safing/re-ignition rule needs a versioned authored policy. The accepted per-applied-interval ActuatorRevision rule remains in force and has meaningful application-frontier/cursor authority separate from requested commands and physical state.

## Current API gaps

M14.24 RemainingUnits/ResourceRevision are readonly cold fields, and the state store's existing prepared continuation installs paired motion without mass. The physical ticket must introduce owner-writable canonical resource/actuator records behind stable capabilities and a prepared paired-plus-mass destination. No per-interval cold resource rebind, public fuel setter, or post-publication mass edit is acceptable.

An active physical application mode must keep M14.23's private preparation progress distinct from canonical applied state. Its seal has already consumed the private edge/latch cursor, but that is not hardware progression. Same-phase acknowledgement synchronizes the prepared pipeline to the committed actual latch/application frontier without replaying edges or rewinding cursors. Standalone preparation-only M14.23/M14.24 behavior remains unchanged.

## Finite lifecycle and source leases

    READY at acknowledged canonical frontier
      -> admit host credit; commit due commands; close boundary
      -> genuine M14.23 parent
      -> genuine M14.24 dependent resource lease
      -> private ordered physical evaluation
      -> one issued endpoint receipt
      -> fully prepared joint successor/history/ack
      -> final applicability checks under existing owner phase
      -> fixed canonical bundle + consumed execution leases
      -> fixed private acknowledgement
      -> READY at next exact frontier.

| State / failure | Canonical result | Lease and continuation disposition |
|---|---|---|
| Invalid input/source before evaluation | No interval successor changes | Refuse; no forged preview accepted |
| Resource preparation refusal | No interval successor changes | Preserve valid parent; do not recreate it |
| Resource-only cancellation before physical evaluation | No interval successor changes | Parent may remain active; fresh resource generation on reissue; old lease dead |
| Sound endpoint, ordinary precommit refusal | No interval successor changes; prior host/command commits remain | Keep matching parent/resource/endpoint for publication retry; no repeated solver step or debit |
| Canceled resource after endpoint staged | No interval successor changes | Old physical receipt invalid; free-flight pure proposal may be discarded/recomputed from valid source. Retained-contact cancellation needs terminal disposal or preservation of all matching leases, not automatic rewind |
| Parent discard in active physical mode | No interval successor changes | Terminal pending-episode disposition: banked parent discard cannot prepare that same interval again |
| Stale authority or unsafe private evaluation failure | No interval successor changes | Invalidate affected private continuation; no automatic rebind |
| Canonical success and ack success | Entire bundle committed | Parent/resource/physical leases consumed once; advance expected authority |
| Canonical success then private ack failure | Entire bundle remains committed | Distinct CANONICAL_COMMITTED_PRIVATE_INVALIDATED; all leases dead, no rollback/retry/reconstruction |

Equal copied values after a canceled/reissued resource lease do not revive an endpoint tied to the old seal/generation. One pending endpoint may exist; no stepping ahead of an unpublished endpoint. An interval consuming zero fuel still publishes coherent unpowered motion/actual state; 'no fuel without corresponding motion' means the paired state was evaluated for that same physical interval, not that every coordinate must numerically change.

## Admission and final recheck

Validate owner thread/non-reentrancy, engine/spacecraft/configuration/model identities, genuine active parent/resource/physical receipt and generation, exact same source/target/frontier, expected paired values/mass/inertia, StateRevision/ResourceRevision/ActuatorRevision/CommandRevision/TimelineRevision, closed command prefix, full clock/debt, and private continuation validity.

Refuse any authoritative pending event at OR before target without consuming/reordering it. Source-end and original 60 Hz schedule remain exact. Require sufficient nonnegative debt and checked target/debt/revision/history-index arithmetic. Prepare all finite/normalization/mass-law checks, result observations, destination indices, history slot and acknowledgement data before the commit block. A numerical integration failure cannot become a resource debit.

Perform the final source/receipt/event/debt/capacity recheck under `ContinuationPublicationPhase`; no other canonical mutation may interleave. Existing concurrency doctrine provides owner serialization, not hardware-wide multiword atomic writes visible to arbitrary unsynchronized memory readers. Consumers obtain copied observations through the authorized phase; no live partial canonical references are exposed.

## Fixed commit and acknowledgement

Reuse the existing fixed-write doctrine and appropriate destination/clock/history primitives, with one prepared combined payload:

    paired endpoint and temporal validity
    canonical mass/COM/inertia representation
    exact resource successor and ResourceRevision
    actual actuator successor and ActuatorRevision
    StateRevision, target clock/debt
    one prebuilt joint history record/frontier
    parent/resource/physical execution leases marked consumed
    private fixed expectation/latch/frontier acknowledgement.

All indices/references are prepared. No solver, force evaluation, resource calculation, normalization, allocator, event search, lookup, callback, history construction, lock acquisition or normal refusal occurs inside fixed writes. Ordinary managed scalar/value assignments to prevalidated storage constitute the commit; this is not a promise of recovery from process termination/hardware failure halfway through memory writes.

Lease consumption is commit-associated fixed bookkeeping before/independent of private acknowledgement success. The terminal ack-failure result is prebuilt and reports every committed successor. Narrow internal test seams must force both final-precommit refusal and postcanonical-commit acknowledgement failure; not a generic fault-injection framework.

## Endpoint-only validity is mandatory

Current translation/exact-event evaluators can extrapolate ordinary constant-force segments after their epoch. Publishing new x/v/m while leaving that interpretation intact would permit unsupported powered continuation. A scene-only hold is insufficient.

Introduce explicit applied-endpoint model/validity in canonical physical records and their copied sources. Exact endpoint observation remains readable for commands, frame evaluation and next preparation. Legacy scalar, paired, canonical and rational-time evaluators must return a typed unsupported-regime/outside-qualified-interval result for other times. Guard direct constant-force proof/transaction consumers, including M14.17/contact source admission, against accepting such records implicitly. Do not set force to zero as a fake valid propagation model or freeze a BODY force into root.

Old constant-force records retain all previous semantics. Future historical dense output requires its own qualified representation; the joint record alone does not certify intermediate motion. Powered owner publishes once per exact interval; visible presentation holds the copied last committed endpoint.

The fence must also survive mutation paths. Existing force, torque, attitude and paired-state replacement transactions must either preserve endpoint-only validity and satisfy the powered owner's applicability, or refuse while this regime owns the craft. A generic replacement must not silently drop the marker and manufacture a legacy extrapolatable segment. Any transfer to another valid motion model is an explicit separately qualified regime transition. Add direct copied-source and replacement-path tests; a top-level evaluator or scene flag alone does not protect this boundary.

This includes SAME-EPOCH replacements: successfully reading the current endpoint does not authorize constructing a new unrestricted model there. For example, current `RigidBodyTorqueTransactionEvaluator.cs:21-42` creates a fresh torque state after current-time evaluation. The future model/validity must participate in copied source identity, equality, deterministic hashes and proposal seals; constructors/transactions cannot remove it implicitly. Only a separately qualified explicit regime-transition receipt may change propagation validity. While the first powered fixture owns the craft, unrelated runtime force/torque/configuration edits remain refused.

## History without duplication

Preallocate one bounded versioned record per interval containing exact start/target; before/after paired endpoint and mass-property bits; exact resource before/after and flow; command/engine/resource/model identity and applied edge provenance; actual actuator before/after; all relevant source/new revisions; clock/debt facts; segment classification and exact exhaustion provenance; numerical-policy/endpoint identity.

Reconstruct the exhaustion ratio from exact U,V,n and model/source facts instead of storing several nested M14.24 previews and repeated definitions. Shared immutable definition tables use stable IDs, never live object capabilities. No process-global generation order, pointer, callback or solver handle enters deterministic history. History capacity failure rejects before evaluation where possible and always before canonical writes.

## Backlog and host accounting

Each interval independently reads the last committed successor, prepares fresh engine/resource data, evaluates up to two segments and commits. After depletion, interval N+1 uses exact zero fuel and the committed Enabled+NoFeed state. Four complete canonical intervals maximum per service call remains a bounded scenario policy; no larger dt, skipped work or average throttle. Remaining admitted debt stays canonical.

M14.23 seals complete clock/debt. Admit host credit before parent preparation. While a retryable endpoint remains pending, do not credit new time and weaken the clock check. Defer new submission or retain checked unsubmitted elapsed input/sequence in bounded owner storage; it is not yet canonical debt and must be reported distinctly. After resolving the pending interval, accept that input once. Previously accepted host credit/commands remain committed even when physical publication refuses.

Keep M14.22's funded-horizon admission and exact source-boundary transition ordering. Later backlog intervals see updated resource/actuator state but do not retroactively see newly entered commands effective beyond the funded horizon. Rendering happens once after bounded simulation service, never per segment/RK stage/backlog interval.
