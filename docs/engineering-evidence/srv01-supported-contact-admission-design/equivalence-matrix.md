# Responsibility equivalence and departure decision

The mandatory KSA prerequisite passed before NovaCore source/design work. This
comparison concerns canonical assembled-vehicle ownership, not merely two games
having spacecraft. Actual installed method provenance is in the KSA owner map;
live history was directly read as recorded in ksa-live-history.md.

| Required outcome | Current KSA method/boundary | NovaCore decision and positive reason |
|---|---|---|
| Same assembled identity enters physics | IntakeOrphans / AddVehicle use existing VehicleUpdateState and Vehicle | ADOPT; same canonical spacecraft/launch/parts, subordinate native handle |
| Physical shapes derive from authored parts | ColliderModules -> one compound, COM-relative part transforms | ADAPT; explicit versioned SRV part-collision profile is missing today; never infer it from rendered GLB or mass tensor |
| Properties derive from canonical parts/stores | RecomputeMassProperties / CopyToBepu | ADAPT FP64 tensor and exact two-store source; BEPU projections are not a competing mass owner |
| Persistent private solver | Existing ConstraintSim and body reused | ADOPT; one owned pool/world/body/compound for bounded episode, conditional manifold and impulse persistence |
| Source properties during constrained solve | FullPhysicsConstrainedStep then resource/derived-property update | ADAPT exact resource/event/publication delta, source-mass policy; first slice admits zero demand so successor properties equal source |
| Staged then ready then apply | VehicleUpdateData / PublishResults / ApplyResultsToVehicles | ADAPT single exclusive canonical transaction, sealed receipt and same-phase acknowledgement |
| Stable topology property refresh | UpdateShape offsets/refit and CopyToBepu; replacement possible for changed child count | ADOPT ownership boundary; no topology edits in first slice; future changed COM must preserve material geometry and body identity |
| One physical evolution per interval | FullPhysicsStepUntil chooses constrained OR unconstrained | ADAPT explicit consumer identity in existing assembly owner; no parallel free-flight evaluator |
| Support is not ownership | EndFrame contact situation versus member removal and idle-sim retirement | ADOPT; loss of a contact row is not automatic release or spacecraft replacement |
| Failure cannot publish unsafe state | poisoned sim, failed bubble cannot become ready | ADAPT NovaCore precommit refusal vs canonical-committed/private-invalidated distinction |
| Display reads applied game state | game vehicle updated before render work | ADOPT copied canonical material-origin presentation, no live BEPU render authority |
| Resource arithmetic and time | KSA staged resource application is not NovaCore exact integer resource/revision/debt law | ADAPT; preserve explicit NovaCore exact store, tick, event, history and raw staged endpoint requirements |

**INTENTIONALLY DIFFER: none required for equivalent ownership/lifetime boundaries.**
General bubble clustering, pooling, rebasing, rails, clutter and arbitrary topology
editing are outside the bounded outcome, not invented competing mechanisms.
KSA's readback clamps/normalization are not adopted where NovaCore requires checked
staged values and bounded projection. This is the minimum ADAPT delta, not a claim
that KSA implements NovaCore revisions or bit contracts.

## Departure scope under the latest Project Control clarification

Current KSA proves a shared authority encompassing supported and unconstrained
motion. It also proves separate support observation, eligibility selection,
membership transfer and native-sim retirement. `RemoveEligibleVehicles` is a
parent/frame test; support loss does not trigger it. Therefore admission can be
qualified separately **without inventing a new free-flight authority**.

Decision: defer departure eligibility and executable handoff in the first slice.
The architecture reserves one assembly consumer identity; it never routes a
contact result through a second spacecraft. A future transfer must retire the
old evolution permission at an acknowledged canonical frontier before granting
the next consumer, preserve spacecraft/parts/stores/mass/COM/inertia/commands and
revision lineage, and dispose/retain the native world under explicit ownership.
It cannot run both consumers, delete a body merely on missing support, or use
the 600 N thrust/weight fact as a prohibition. Those are handoff invariants,
not an implemented transition or a departure qualification claim.

During the bounded supported-only slice, support-domain failure after a native
step invalidates private continuation and preserves the last committed canonical
endpoint; it neither silently falls back to free flight nor publishes a false
supported result. Completion holds the endpoint; explicit cold disposal releases
resources, not permission for analytic propagation. Future departure remains a
separate Project Control decision supported by this same owner model.
