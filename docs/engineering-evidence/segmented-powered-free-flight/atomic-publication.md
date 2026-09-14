# One joint canonical successor

## Ownership and preparation

`SimulationTransactionEngine.PoweredFreeFlight.cs` owns one cold-created bounded storage object, one active joint proposal, one copied canonical observation, immutable episode provenance and a preallocated coherent interval history. The existing clock publication phase serializes owner access. There is no second authority lock or physics clock.

The engine/resource capability chain is genuine M14.22 -> M14.23 -> M14.24. Source checks compare applied-endpoint physical bits (including signed zero), validity, definition, mass/inertia, exact canonical resource, revisions, complete clock state, owner and private frontier. Initial legacy physical sources retain their existing struct-equality semantics. Preparation consumes copied exact segmentation, performs all numerical work, computes checked successors and issues a private sealed generation token. Source engine/resource leases remain active before canonical commit. Fabricated/default/foreign/stale/replayed tokens refuse.

The common command, engine and resource in-owner preparations are extracted from their banked public wrappers; the wrappers retain their original exclusive entry/exit behavior. Body-wrench semantics, exact resource arithmetic and demand/flow meanings remain unchanged. The resource amount/revision now resides in one owner-controlled canonical record; its capability exposes copied reads. There is no second numerical tank authority.

## Admission and fixed commit

Publication verifies the active resource lease, exact prepared segmentation, endpoint validity, event boundary at/before target, positive checked integer interval, sufficient debt, history capacity and an exact prepared physical slot. It prepares clock/debt, successor resource, physical identity and copied observation, then rechecks source/lease/slot without releasing the owner.

Fixed writes install paired endpoint + mass/inertia + StateRevision, exact canonical resource + ResourceRevision, actual actuator + ActuatorRevision, clock/debt and one complete history record. No solver, ODE, quaternion normalization, event execution or callback runs there. Other owner-governed canonical reads cannot observe a partial successor. The private acknowledgement updates only the expected source/resource/clock/revision/frontier to the committed successor.

All source execution leases are retired immediately after canonical success, before the acknowledgement branch, so an injected acknowledgement failure cannot replay an already-spent interval. That terminal branch preserves **every canonical successor** and returns CanonicalCommittedPrivateInvalidated. It cannot masquerade as a precommit refusal. The physical evaluator continuation refuses afterward.

## Host accounting and linearization

Positive integer elapsed samples use the existing clock preparation semantics at fixed rate one. They require exactly the next owner input sequence, no outstanding source/joint proposal and unchanged expected canonical state. Fixed host debt installation and private expected-clock acknowledgement share the owner phase. Zero duration is NoWork and does not consume the next positive-input identity. Negative, overflow and duplicate/stale inputs refuse without accounting change. A forced failure after host debt commit preserves that debt, invalidates continuation and returns the distinct committed-terminal result.

Host credit and physical publication are separate operations. `CanonicalCommitted` includes AcceptedCredit and any published prefix even if later service is blocked. Service admits at most four complete original-lattice intervals per call, uses the just-committed mass/resource state, retains backlog and never acknowledges ordinary external clock mutation. Event, debt and history checks occur before interval work. A valid pending proposal survives an ordinary publication refusal and can be retried when safe.

## Lifetime and history

Explicit joint retirement discards only its private prepared receipt. Cancel/reissue of a resource lease may then recompute from the unchanged canonical source; old joint generations remain dead. An explicitly retired parent engine cursor cannot be rewound, so that cancellation poisons this private continuation rather than silently rebinding.

One immutable `PoweredFlightEpisode` stores numerical policy version, spacecraft/engine value definitions, exact end and initial canonical observation. Each indexed `PoweredFlightRecord` stores exact engine/resource/exhaustion provenance, after endpoint/actual state, before/new revisions and DebtBefore/After. Initial state plus ordered successors reconstructs every transition. Records contain deterministic values, not live capabilities, pointers, BEPU handles or process-global generation order. No separate unbounded fuel or actuator history exists.

## Endpoint-only authority

`SpacecraftAppliedEndpoint` is a distinct canonical payload, with explicit EndpointOnly validity. It is not represented as a legacy constant-force segment. Exact-time copied motion and reference-frame queries can observe it; past/future/fractional queries outside its exact canonical epoch fail. Legacy paired replacement and prepared legacy continuation slots refuse this state, including the cached attitude shadow path. Same-epoch copies preserve raw endpoint bits and validity; they cannot shed the fence through a legacy replacement API.

## Permanent refusal witnesses

`PoweredFreeFlightTests.Authority.cs` checks wrong owner/reentrancy, foreign authority, StateRevision/TimelineRevision, applied-endpoint signed-zero/mass/validity mutation, external rate/pause/debt mutation, bad receipt and duplicate replay, cancellation/reissue and parent retirement, event at/before target, insufficient service funding followed by additional credit, history capacity, StateRevision overflow, preparation refusal followed by preparation/publication, and both post-commit acknowledgement seams. Ordinary precommit snapshots remain unchanged. Post-commit tests assert the actual committed successor and retired leases. Immutable initial provenance and indexed debt/history reconstruction have permanent tests.

Definition/configuration checks, ResourceRevision/ActuatorRevision overflow checks and preservation of a sealed endpoint on ordinary publication refusal were source-reviewed; dedicated new permanent witnesses for those exact cases are not claimed. The existing preparation-refusal retry is not a sealed publication-refusal/retry test. These coverage distinctions remain for Project Control at the validation stop.

These source/lifecycle responsibilities passed independent read-only review and focused Debug/Release tests. Overall acceptance remains stopped at the unrelated-in-location but **unattributed** full Release clock allocation gate; no causality exemption is asserted.
