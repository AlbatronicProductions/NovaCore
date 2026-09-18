# Current KSA owner and lifecycle gate

Direct installed-DLL inspection, 2026-09-17. This record precedes NovaCore
mechanism design. Decompiled methods were read to stdout; no KSA source/assets
were copied into the repository. Method tokens below belong only to the hashed
build in identity.json. Prior reports supplied navigation, not proof.

## Identity and production reachability

Root `E:\Kitten Space Agency`; KSA product
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.
`Content/Versions/v2026.9.X.5438.json` identifies release date 2026-09-15 and
revision range 5402–5438. KSA.dll SHA-256:
`A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`.
Backend: BEPU `2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`.
This is the current **available installed** build, not a claim that live main
stops at 5438. Live history has newer 5448 work; see supersession record.

`Universe.ExecuteNextVehicleSolvers` (06002E04) removes eligible old membership,
prepares real current-system vehicles, prepares/queues the single vehicle task.
`PrepareVehicleWorkers` (06002E05) calls each vehicle's worker preparation and
collects orphan update states. `VehicleUpdateTask.Run` (06003116) enters the
shape-registry update phase, trims/intakes/merges, prepares bubbles, steps them,
splits and marks results ready; finally exits that phase.
`Universe.ApplyVehicleSolvers` (06002DFC) reaches
`VehicleUpdateTask.ApplyResultsToMainThread` (06003115). These are production
callers, not debug UI or a unit-test substitute.

## Responsibility map

| Responsibility | Current owner / method token | Authority, lifetime and boundary | NovaCore equivalence / classification |
|---|---|---|---|
| Vehicle and part identity | Vehicle; PartTree; UpdateAfterPartTreeModification 06002F13 | Existing Vehicle owns parts, resources and physical properties; update state points back to the same vehicle. Admission does not create a replacement canonical craft | Same assembled craft, ADOPT responsibility |
| Inert + stored-resource mass/COM/inertia | PartTree.ComputeInertMassPropertiesAsmb 06001AB4 / ComputePropellantMassPropertiesAsmb 06001AB0/1; VehicleProperties.RecomputeMassProperties 06001B43 | Derived from vehicle parts and staged module/resource states, not inferred from solver body; rebuild after applicable source changes | ADAPT exact two-store authority and FP64 tensor |
| Compound collision geometry | Vehicle.CreateColliderCompound 06002F1B; CreateOrUpdateColliderCompound 06002F1C; BepuHandles.Create/Dispose 0600066E/F | One assembled BigCompound of part collider primitives, assembly transforms relative to COM; child-to-Part map. Shape handles belong to update-state/vehicle lifetime; shared shapes registry has a separate update phase | ADAPT canonical collision description to native owned representation; no visual-mesh authority |
| Bubble admission | VehicleUpdateTask.IntakeOrphans 06003117; PhysicsBubble.AddVehicle 06001BC8 | Worker chooses existing compatible bubble or rents one, converts frame, inserts existing update state; canonical identity survives | ADAPT bounded single prepared episode, no general clustering requirement |
| Native world/body admission | PhysicsBubble.InitializeConstraintSim 06001C26; ConstraintSim.AddVehicle 06000686 | Returns existing sim; otherwise rents sim and creates one body per vehicle; body handle maps back to the same update state | ADOPT retained world and body lifetime |
| Source physical upload | PopulateBepuStatesFromKinematic 06001C33; ConstraintSim.UpdateSimFromVehicle/PushBodyStateToSim 0600068A/88; VehicleProperties.CopyToBepu 06001B40 | Current source props, kinematics and collider/COM adjustments update retained body. Native state is numerical state, not resource authority | ADAPT bounded projection; NovaCore acknowledgement must not unnecessarily reimport solved pose |
| Staged successors | VehicleUpdateData.Prepare 06001BBC; VehicleUpdateState.GetNewProps/GetReadOnlyProps 06001C46/47 | Preparation resets optional successors; first write copies source, subsequent writes reuse buffer. Source/staged selection is explicit | ADAPT ready/apply with exact revisions, genuine prepared authority |
| Constrained solve/resource cadence | PhysicsBubble.FullPhysicsConstrainedStep 06001C19; ConsumePropellantFromActiveNozzles 06001C5B / 06001B46 | Derivatives/source props held through ordinary constrained substeps; solve/readback first, consume resources and update props afterward. Native world does not own stores | ADAPT exact resource/event and atomic successor delta; retain accepted source-mass policy |
| Support observation and next motion consumer | ConstraintSim.UpdateIsAnyConstrained 060006A1; PhysicsBubble.FullPhysicsStepUntil 06001C02; FullPhysicsEndFrame 06001C03 | Constraint participation selects constrained OR unconstrained evolution. Fresh terrain-contact evidence updates Situation. Support flags are not body membership | ADOPT distinction; no inferred immediate removal from contact loss |
| Unconstrained/surface/rail ownership | ApplySingleVehicleMotion 06001BFE; FullPhysicsUnconstrainedStep 06001C1A; TryToPutCoastingOnRails 06001BFC | Same bubble/update state selects exactly one physical path; clearance, atmosphere, actions and activity affect readiness for analytic/surface treatment | Equivalent broader lifecycle, but regime eligibility is separate from body admission |
| Readback and application | ConstraintSim.ReadBackFromSim/UpdateVehicleFromSim 0600068D/8B; PhysicsBubble.PublishResults/ApplyResultsToVehicles 06001BF8/BEA; Vehicle.UpdateFromTaskResultsUnsynchronized 06002F31 | Native successor becomes staged vehicle state; ready required; apply updates same vehicle and part/module successors, followed by synchronized/render work | ADAPT NovaCore exact single canonical transaction and same-phase acknowledgement; do not copy arbitrary KSA apply code |
| Persistence of constraints/warm starts | ConstraintSim owns Simulation, narrowphase, solver; InitializeConstraintSim returns existing | Ordinary steps retain sim/body; contacts and impulses evolve within BEPU. Individual manifold existence is conditional on pair persistence, not an eternal guarantee | ADOPT persistence, verify trajectory rather than promise immutable contact arrays |
| Retirement and transfer | RemoveEligibleVehicles 06001BEF; RunPostWorkAndCluster 06001BE5; ReturnConstraintSim 06001C25; ConstraintSimPool.Return 060006CE | Parent/frame mismatch removes bubble membership. Sim can persist beyond contact loss; after >30 simulated seconds unused it is returned and bodies removed. Merge/split may also migrate membership | ADOPT distinction between support, physical consumer, membership and disposal; bounded NovaCore scope need not copy pools/30-second constant |
| Failure/invalidation | ConstraintSim.Simulate 0600069D; TryResetForPool 06000683; PhysicsBubble.MarkFailed 06001BF5 / PublishResults 06001BF8 | Solver exception poisons sim; poisoned sim cannot reset for reuse. Failed bubble cannot become ready. Pool reset clears solver/body/narrowphase caches | ADAPT terminal private invalidation; preserve NovaCore committed-vs-refused distinction |

## Configuration and limits

Current GameSettings.Simulation defines worker count, target frame rate, flight
computer cadence and optional ground-clutter collisions. Actuator commands,
active actuators, animation, ocean/locomotion and `_forceOffRails` affect analytic
eligibility; none establishes a replacement canonical vehicle. `_forceOffRails`
is an override, not the ordinary ownership mechanism. No KSA runtime was launched
or setting changed. No claim is made about the user's currently saved settings.

Retained does not mean never reconstructed: explicit removal, parent/frame change,
merge/split, disposal and pool reset are boundaries. Arbitrary topology edits and
shape-handle replacement are outside the proposed fixed-assembly question; do not
infer that every such edit preserves manifolds. ShapesVersion is not a proven
vehicle-world reset hook (current callers concern cloth/clutter caches).

## Departure clarification — source result before NovaCore design

The full KSA owner encompasses constrained and unconstrained motion. However,
**support loss, motion-consumer selection and world release are separate decisions**.
FullPhysicsStepUntil chooses one solver path for the staged interval; EndFrame
updates terrain contact; RemoveEligibleVehicles tests parent/frame, not loss of
support; the native sim has a separate unused retention period. Coasting rail
admission additionally checks atmosphere, terrain anchor and future impact/expiry.

Therefore direct evidence does not force an indivisible admission-to-departure
implementation. A bounded retained supported episode can share that ownership
architecture without implementing the eligibility/transfer policy. This conclusion
survived the independent gate review before deciding NovaCore scope. The 600 N
engine's Earth thrust/weight is not an architectural argument against departure.

KSA has no inspected equivalent of NovaCore's exact store revisions, canonical
debt/history transaction, sealed receipt or committed/private-invalidated status.
Those explicit NovaCore outcomes require ADAPTATION, not fabricated KSA claims.

## Direct-inspection qualifications

- AddVehicle initially supplies linear velocity; subsequent PushBodyStateToSim /
  KinematicStates.CopyToBepu (06001B34) installs the complete pose and angular
  velocity as well. Body construction alone is not completed state admission.
- Current COM/collider changes update child positions and refit the existing
  compound (ConstraintSim.UpdateShape 0600068E). A changed child count can recreate
  its shape (Vehicle.CreateOrUpdateColliderCompound 06002F1C). Stable topology is
  essential to the ordinary retained-body claim.
- Worker resource successors are applied through PartTree.UpdateFromTaskResults
  (06001AB3). The BEPU callbacks are not a second resource store.
- Independent direct-call scan found vehicle Bodies.Add only in AddVehicle;
  the other matching body creation path is ChuteBepuClothState.Create (06000468),
  a separate cloth solver. AddVehicle callers are PhysicsBubble.AddVehicle and
  InitializeConstraintSim; RemoveVehicle callers are PhysicsBubble.RemoveVehicle
  and ReturnConstraintSim. This is a bounded managed-call check, not a claim about
  arbitrary reflective/plugin code.
- KSA's readback can clamp velocities and apply can normalize orientation. Those
  behaviors are not prescribed for NovaCore; its qualified staged endpoint bits
  and fail-closed bounded projection remain explicit required outcomes.
