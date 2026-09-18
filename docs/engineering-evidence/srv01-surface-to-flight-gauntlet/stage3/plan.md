# Stage 3 promotion and bounded implementation plan

2026-09-17. Stage 2 final red team and Project Control manual acceptance passed. **PASS — PROMOTE into bounded Stage-3 implementation**, not a Stage-3 qualification claim. UNBANKED; no milestone.

## Required outcome and boundary

The same canonical SRV-01 finishes a contact-owned interval, proves clearance for the next short interval, atomically selects the existing assembly free-flight evaluator, and publishes the first free-flight successor with the same stores, mass/COM/inertia, clock, history and material-origin identity. An empty manifold alone grants no release. No replacement assembly, fresh admission, second debit, event-sized contact solve, thrust retuning or recontact system.

Current source proves this is missing: `SimulationTransactionEngine.Assemblies.cs` rejects loss of support, `AssemblyLaunch.Consumer` describes cold admission, and `AssemblyDynamics` is propulsion-only. Contact gravity is -9.81 Y; dropping it during handoff is forbidden. Free-flight increments remain <=15,625 ticks. The old 128-command zero-gravity route and replay remain unchanged.

## Current KSA gate (fresh direct source and authenticated history)

Actual root: `E:\Kitten Space Agency`, product `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.

| Binary | SHA-256 |
|---|---|
| KSA.dll | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

Fresh installed-method review used ILSpy stdout, without retaining proprietary source:

- `FullPhysicsEndFrame` 06001C03 records support; it does not choose an integrator or remove a body.
- `UpdateIsAnyConstrained` 060006A1 is bubble-wide. `FullPhysicsStepUntil` 06001C02 selects one consumer; `DetectCollisions` 0600069C updates predictive bounds and contacts.
- `CertifyTerrainClearance` 06001C05 / `ComputeThrustAccelBound` 06001BDE bound an eligibility horizon. KSA's terrain/radius padding constants are not NovaCore requirements.
- `FullPhysicsUnconstrainedStep` 06001C1A / `RunUnconstrainedVehicleStep` 06001C1B use the same staged vehicle. `IntegrateVelocityVerlet` 06001B92 / `ComputeDerivatives` 06001B8F include gravity/environment and consume propulsion once. The constrained alternative 06001C19 consumes once after its native steps.
- `RemoveEligibleVehicles` 06001BEF concerns frame/parent membership, not support loss. Native resources persist through unconstrained selection; `RunPostWorkAndCluster` 06001BE5 / `ReturnConstraintSim` 06001C25 retire them separately. No 30-second policy is copied.
- Detection/step failures poison private state; `PublishResults` 06001BF8 cannot publish a failed bubble.

Authenticated actual KSA Discord `#live-changelog` was read directly in the existing browser, separately from source:

- [4646](https://discord.com/channels/1260011486735241329/1260112103134724146/1516883816554168393): terrain contact from collision detection, independent situation bits. Adjacent 4659 records collision checks before timestep choice and avoiding low-precision overwrite of authoritative positions.
- [5173/5174/5177 vicinity](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902): stale sleeping-body bounds, false landed state while engines fire, and repeated jet execution/resource use. These reinforce separate support/consumer/command authority.
- [5434–5449 visible latest history](https://discord.com/channels/1260011486735241329/1260112103134724146/1550158043986010133): resource, presentation and ground-clutter changes. Clutter promotion/inertia/terrain/dispatcher fixes do not replace the inspected spacecraft ownership method. Live history is newer than the installed build; it is not asserted to be installed. Supersession check is bounded, not an exhaustive future-history claim.

ADOPT: one physical consumer, same staged assembly, interval-valid eligibility, support distinct from native lifetime. ADAPT: fixed slab clearance, existing qualified RK4 rather than a second integrator, exact ticks/resources/revisions/atomic history. No competing general vehicle/environment framework.

## Narrow design

- A distinct immutable qualification continuation profile separates original frame transport from prescribed initial upward vehicle-relative velocity. This is a departure-boundary fixture, **not stock-engine liftoff** or a settled-to-launch claim. The existing article/600 N engine/stores/collider are unchanged.
- Contact retains its original episode/frame epochs and ordinary 60 Hz first interval. After a clear final contact endpoint, subsequent free-flight increments use the existing <=15,625 tick domain. No reset or reinterpretation of an already-evolved interval.
- Prepare a conservative slab-clearance envelope for the entire next interval using actual geometry, current relative velocity, force/gravity bounds and bounded angular motion. Recheck each interval. Refuse before expiry/recontact; never infer clearance solely from absent rows.
- Add explicit consumer provenance to prepared/history/observation state without changing old serialization. Transfer occurs with the contact successor's existing atomic publication. Native continuation permission is revoked in the same phase; storage/body identity remains until explicit cold disposal.
- Publication retries consume the same prepared successor; ordinary refusal changes nothing. Postcommit acknowledgement failure preserves the commit and terminates continuation.
- Add explicit root gravity to the existing pure free-flight evaluator with an exact old zero-gravity branch. Resource segmentation, changing mass and material-origin terms remain unchanged.

## Declared validation

Cheap focused gates first: real contact-owned separation, first free-flight successor versus independent gravity/force calculation, full-interval geometric clearance and near-return refusal, one debit/revision/history per interval, native no-step after transfer, stale/default/foreign/duplicate receipts, pending-event/capacity/debt/authority refusal, retry and postcommit failure, moving-frame and host-partition determinism. Preserve the Stage-1/2 supported-only fail-closed profile and old M15.2 no-gravity replay.

Then Debug/Release focused and Simulation/regression/build gates appropriate to changed files; exact warmed allocation with positive control; storage accounting; bounded Release timing without reopening Stage-2 residual investigations. No claim of player-visible departure acceptance from numeric tests.

Independent read-only reviewers confirm these are bounded Stage-3 adaptations already authorized by the campaign, not a new product choice. The new force/cadence/profile logic must be qualified before relying on it. Stage 4 still requires its separate propulsion/product gate.
