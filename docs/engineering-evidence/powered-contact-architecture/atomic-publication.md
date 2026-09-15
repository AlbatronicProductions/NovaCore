# One coherent canonical successor and private acknowledgement

Preferred convergence model only; no implementation in this investigation.

## Existing common primitives

SimulationTransactionEngine uses _clock.PublicationPhase for owner-thread/reentrancy/exclusivity. M14.19 fixed publication uses SimulationState.TryPrepareContinuationSlot / InstallCertifiedContinuation and clock.InstallCertifiedContinuation. M15 uses TryPrepareAppliedSlot / InstallAppliedEndpoint, the same clock primitive and owner phase. The applied-endpoint slot supports mass/inertia together with paired endpoint-only state.

Reuse those genuinely shared owner/slot/revision/clock responsibilities. Do not call the M14 publisher then debit fuel with M15. M14.17 clearance evidence/admission remains separate. New consumer evidence is a specialization of common input/successor ownership, not acceptance of a BEPU receipt by the old free or certified-clearance APIs.

## Proposed flow

1. Owner admits host credit once with monotone sequence, preserving successful credit if later work blocks.
2. At each exact funded frontier, commit due requested commands and close boundary; retain those independently committed command outcomes on later physical failure.
3. Prepare genuine engine and exact resource leases, common source/mass/environment/schedule and capacity.
4. Perform every cheap applicability/numerical/event/debt/history/revision check before private solver mutation.
5. One selected physical consumer produces one sealed staged result. No resource/actual write in prediction, collision, force callback or solve.
6. Prepare one endpoint-only physical slot, exact resource successor, mass/inertia, actual activity/frontier, revision successors, exact clock/debt, deterministic history and resolved private acknowledgement target.
7. Final source/capability/world-generation/frontier/event/revision/clock recheck while retaining the same phase.
8. Fixed canonical writes; retire ALL source/joint execution leases; fixed private expected-authority acknowledgement; copied committed observation.

There is no second lock, solver call, normalization, dictionary search, history construction, callback or resource arithmetic in step 8.

## Successor set

| Authority | Commit rule |
|---|---|
| Paired physical state | Exact checked staged FP64 endpoint bits and exact integer target epoch; endpoint-only validity |
| Mass/COM/inertia | Same versioned mass-law successor as exact resource; central point keeps COM/dry I fixed |
| Resource | Exact remaining units; ResourceRevision increases only for nonzero consumption |
| Actual engine | Same segmentation's activity/latch/endpoint throttle/applied powered duration/frontier; ActuatorRevision once |
| Physical revision | StateRevision once per whole canonical interval, even if two internal resource pieces |
| Requested command | Existing separate M14.22 revision/sequence; never reset or republished because support disappears |
| Clock/debt | Target from original T(n); subtract whole exact integer interval once; keep rate/remainder/pause contract |
| Timeline | Unchanged by physical publication; authoritative event at/before target rejects |
| History | ONE common interval record; exact source/successor/resource segments/actual/revisions/debt and deterministic contact-consumer provenance |
| Private continuation | Expected physical/mass/resource/actual/clock tokens, acknowledged frontier and consumer state advanced only after canonical success |

History must contain stable values, not BEPU handles, object references, process-global generation order, callbacks or live capabilities. A world-generation capability can protect private applicability without becoming nondeterministic canonical history.

## Failure linearization

| Failure point | Required result |
|---|---|
| Input/lease/numeric/event/debt/capacity refusal before private step | No physical/resource/actual/time/history mutation; previously accepted host credit or commands remain committed |
| Solver/export failure after private mutation | Canonical bundle unchanged; unsafe private continuation invalidated; no hidden re-step/reconstruct |
| Sound sealed endpoint but ordinary precommit refusal | Preserve pending endpoint and active leases where still valid; retry publication only, not solver or engine preparation |
| Canonical commit then private acknowledgement failure | Distinct CANONICAL_COMMITTED_PRIVATE_INVALIDATED; retain mass/fuel/actual/motion/revisions/time/history; leases spent; never rollback or blind retry |
| Duplicate/foreign/stale receipt | Refuse before writes; no second spend/revision/history |
| Backlog after earlier successful intervals | Report actual published count and current successor; do not claim the whole host call rolled back |

M14.23 preparation has already advanced its private cursor at seal creation (EnginePreparation.cs:187-192). A recoverable contact refusal must retain the same active engine lease. Cancellation does not rewind edges; a numerical admission should precede private mutation, and irrecoverable cancellation must be explicit rather than automatic reprepare.

## Backlog / copied presentation

Reuse max four canonical intervals per service call. Within one host callback, interval N+1 reads N's just-committed mass/resource/actual/source and acknowledged private frontier. Reusing a batch-start mass or engine proposal across four steps violates the source contract.

Rendering runs once per display frame after bounded service and holds the last committed observation between publications. A failed or completed episode keeps that observation. No prediction/callback/preview data appears as actual engine or spent fuel.
