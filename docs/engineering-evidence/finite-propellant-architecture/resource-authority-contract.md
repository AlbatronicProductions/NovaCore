# Canonical resource ownership and future atomic publication

This is a proposed architecture. No resource state, revision, successor writer or physical application is added by this investigation.

## First resource definition and state

One cold-authored, versioned definition binds a stable `ResourceId`, spacecraft identity, definition version, numeric-model version, initial usable mass in kilograms, one engine/feed identity and one mass-distribution-law identity. Use the existing cold validated-definition / owner-issued capability pattern; a copied identity value does not grant authority.

The canonical resource owner is a fixed-capacity spacecraft resource component under `SimulationState` / the existing `SimulationTransactionEngine` owner. First capacity is one resource, one consumer and one outstanding realization. Do not introduce a new global owner, clock, tank graph or publication lock. Engine hardware and feed are distinct: a working Enabled engine may have EmptyFeed; unavailable hardware is a different condition.

Initial usable mass is a finite nonnegative authored binary64 value, lifted exactly once by bit decomposition into the [fixed exact amount representation](exhaustion-event-contract.md). Remaining usable amount is that exact scalar, not a binary64 subtraction history. Kilograms are the physical unit. Definition version denotes immutable authored meaning; `ResourceRevision` denotes later canonical value changes; transient proposal generation is private capability bookkeeping only.

Cold attachment verifies one complete source definition and current physical state. Dry mass must itself be finite and strictly positive, with valid finite positive dry principal inertia under the existing physical validation contract; a positive wet total cannot conceal an invalid dry component. It must not add fuel to an already authored wet mass, reinterpret the banked article's 1,000 kg as dry mass, or mutate mass merely because a resource object was attached. For the new synthetic first-slice fixture, dry-body and resource definitions are explicit and the initial canonical total mass/inertia must match their declared law. The existing article remains unchanged. Binding refuses inconsistent mass/configuration before storing an authority.

The first implementation can initialize this canonical source component as part of cold state construction and prepare successors from it. It provides **no live resource-change or commit API**. Initial construction is not a burn or refueling operation.

## Mass authority decision: C

Choose **staged mass-property successor consumed with future physical dynamics**, not an unrelated scalar decrement and not an immediate endpoint-only mass mutation.

The first scalar law is explicitly named/versioned **CentralPointReservoirV1**:

- dry mass, dry COM/body frame and dry principal inertia are independently authored;
- the ideal usable resource is a mathematical point exactly at dry COM;
- `totalMass(t) = dryMass + usableMass(t)`;
- COM and dry principal axes remain fixed;
- point-resource inertia about COM is exactly zero, so total inertia equals dry inertia.

The positive mathematical evidence is the point-mass parallel-axis tensor `m (|r|² Identity − r rᵀ)`: at `r=0` it is exactly zero. This is a deliberate idealization, not a claim that real fuel occupies no volume. There is no imported tank radius or arbitrary ignored inertia threshold. A finite sphere/tank would have nonzero mass-dependent inertia; that model is not silently treated as this point reservoir.

At source and proposed endpoint, compute the exact dry-plus-usable total then a qualified correctly rounded finite positive FP64 physical-mass observation. Compare with the authoritative FP64 source mass according to that one declared realization; do not independently round usable mass first and then sum it with dry mass. Exact resource amount remains authoritative even if its copied kilograms view rounds to zero or the total mass rounds unchanged. Preserve source identity and exact resource revision checks; numerical equality alone is not authority.

First-slice preparation should retain the mass law, source consistency witness, exact consumption model and proposed endpoint mass properties as conditional data. No canonical mass changes. No physical consumer may claim a correct burn using constant old mass and updating only the endpoint.

During a future powered segment the continuous model is `usableMass(s)=sourceMass−q·s` until its exact zero event, then zero; total mass follows the same model. A future physical integrator must qualify its numerical evaluation, body-wrench rotation and exhaust/mass-flux angular-momentum assumptions. Fixed dry inertia in this ideal law does not by itself qualify an open-system rotational equation or permission to reuse old constant-mass dynamics.

Future spatial tanks can replace this versioned mass-law realization with exact/staged COM and full-inertia evolution. Resource owner, aggregate consumption, interval segmentation and joint-commit ownership remain the same. Feed topology, species, slosh, plumbing, pressure and tank depletion routing are deliberately absent now. The numeric encoding's closure is qualified for the first constant-flow model; more complex feed switching must separately prove arithmetic closure and bounds before admission, rather than silently rounding into v1.

## Proposal dependency and lifecycle

Inputs are one owner-bound canonical resource source plus one **genuine active M14.23 engine proposal**, for the same spacecraft, authored engine, closed exact interval, owner and physical/clock/command source. The copied engine preview alone is insufficient.

1. Enter the existing owner phase; validate owner thread/non-reentrancy and resource/engine identity, source revisions, closed interval, source mass law and single-consumer eligibility.
2. Validate the engine's active seal and all M14.23 source checks using a narrowly factored **in-owned-phase reader**. Do not recursively call its externally guarded Preview API or remove that API's guard.
3. Compute exact availability, event and two-segment description, proposed amount/mass successor, deterministic provenance and copied conditional observation entirely before bookkeeping writes.
4. Final source recheck; fixed private writes seal one resource proposal. Do not consume a canonical amount, increment a canonical revision, or mark the exhaustion event as executed.

If resource preparation refuses, no resource proposal/cursor changes and the engine proposal remains active. Retry that same engine proposal when the precondition can be corrected. M14.23 already consumed its **private preparation** edge cursor when it sealed; discarding it and trying to prepare the interval again is invalid.

Resource proposal preview and eventual physical applicability recheck both live capabilities. Retiring/staling the engine invalidates its dependent resource proposal. Discarding a resource preview does not discard the engine or consume fuel. A new resource-preparation lease after explicit cancellation must invalidate the old lease; copied deterministic event values remain identical for identical source facts. Private lease generation must never enter canonical event/history identity or create per-proposal heap allocation.

Duplicate active preparation refuses without spending or replacing anything. Foreign/default/fabricated/retired/stale tokens refuse. Neither a preview copy nor a copied event time can be submitted as a spending right. A canonical commit consumes the one resource proposal once; source resource/physical revisions and frontier prevent replay even for a zero-consumption interval.

Current M14.23 checks the complete clock, including retained debt. Therefore admit host duration first, then close/prepare the engine and resource interval. Later host-credit mutation stales the source. Resource preparation must not acknowledge unrelated clock changes to keep a proposal usable.

## Future all-or-none physical commit

One dynamics consumer evaluates the powered and unpowered segments to the original canonical target. It must keep the matching engine/resource proposals bound to its private endpoint. Free-flight and retained-contact consumers use the same resource result; neither owns independent fuel subtraction.

Prepare before entering fixed writes:

- exact resource source/successor and required resource revision;
- actual actuator successor and its separately justified revision/frontier;
- qualified paired physical endpoint and source/target mass properties from the same mass evolution;
- exact canonical target and full integer interval debt subtraction;
- exactly one next physical StateRevision;
- one preallocated joint history record;
- fixed acknowledgement targets for the private dynamics owner and proposal retirement.

Recheck all engine/resource/physical/configuration/clock/timeline/event/source/frontier/capacity/arithmetic facts under the existing exclusive publication phase. Then perform only fixed writes. No solver call, resource search, allocation, normalization, callback, arithmetic with an expected refusal or independent resource commit belongs after the first canonical write.

Do not call the old M14.19 physical commit and then append a fallible resource/mass write. Its fixed-write primitives and destination preparation can be reused where equivalent, but a future powered transaction must own the complete bundle. M14.17's EventFreeThroughTarget evidence stays separate. Existing BEPU source mass/force/inertia checks and same-phase acknowledgement remain unchanged until their new powered responsibility is explicitly qualified.

Precommit refusal leaves all proposed canonical successors uninstalled. Earlier accepted command commits and host credit remain committed. If dynamics already stepped, retain the matching sound pending endpoint and proposals for publication retry where safe; never re-step or re-spend merely to retry. Unsafe continuation is invalidated under its qualified dynamics contract.

If canonical combined commit succeeds but private acknowledgement fails: retain the committed fuel, actual actuator state, mass, physical endpoint, clock/debt/revisions/history; invalidate private continuation; return the distinct canonical-committed/private-invalidated outcome. No rollback of fuel alone, automatic reconstruction or ordinary refusal label.

## Revisions, history, shared feed and starvation

Recommend **dedicated ResourceRevision** because resource availability is an independently sourced authority shared by consumers and eventually changed by consumption or other explicitly authorized resource operations. It is neither a request revision nor actuator preparation progress. Increment only when canonical resource state actually changes and only inside the joint physical transaction for burns. Zero consumption need not increment it. Physical StateRevision still increments once per successful interval and protects zero-flow interval replay. Canonical actuator revision remains a future actual-realization responsibility. No preview advances any of them.

One joint physical-publication history record per committed interval should contain stable resource/engine/mass-model identity, source/target epochs, exact amount before/after, resource revision before/after, actual actuator/source-command provenance, segment kind/exact exhaustion ratio where present, and paired physical provenance. Zero-consumption records state that explicitly; no second duplicate fuel history is needed. Interior exhaustion is internal record provenance, not a synthetic pending timeline event. Initial empty feed does not emit exhaustion again each interval. Endpoint-empty differs from interior exhaustion.

The **resource owner alone** decides total accepted consumption. First slice rejects a second engine/feed participant instead of pretending to support shared withdrawal. Future multi-engine demands must be aggregated under one declared feed policy before computing a common successor/exhaustion schedule. Multiple separate engines may not each reserve the full remaining source. Future collection/consumer bounds and exact arithmetic capacity are declared before model admission; they are not implied unlimited by v1.

Starvation is **Enabled + NoFeed**, not a command rewrite or destroyed engine. Off/unavailable/zero-throttle remain distinguishable. The ideal model has no finite restart consumable or starvation-induced Off transition. No refueling is implemented; a future authorized feed restoration with Enabled intent would permit output on the next eligible interval under this memoryless model, unless a separately authored safing/re-ignition policy is introduced.
