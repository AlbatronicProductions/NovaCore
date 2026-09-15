# Initially inspected KSA 5402 lifecycle snapshot

Scope: initially inspected build 2026.9.7.5402. The closing identity check found current installation 5438 with different KSA.dll/deps hashes. This file is retained SNAPSHOT evidence, not the current installation claim. See ksa-current-lifecycle.md for the bounded direct 5438 recheck and ksa-source-provenance.md for both identities. Readable anchors here are the 5402 decompilation at E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA. It must not be silently relabeled as 5438 source.

## End-to-end authority and cadence

| Boundary / owner | Current direct anchor | Timing and representation | Authority / failure consequence |
|---|---|---|---|
| Player/control input → staged vehicle | VehicleUpdateState.PrepareFromVehicle:296–305; PhysicsBubble.FullPhysicsPreStep:1546–1561; FullPhysicsStepUntil:1193–1225 | Copies inputs; flight computer updates at selected update boundaries | Game-owned input copied into worker update state |
| Command → engine request | FlightComputer.ComputeControl:396–401; CommandEngineThrottles:420–442 | EngineOn chooses infinite/zero burn command; requested throttle and command time written to staged engine state | Request is distinct from availability and realized output; not a canonical NovaCore command transaction |
| Availability → liquid realization | Rocket.UpdateRockets:93–103; Combustor.ComputePropellantAvailable:60–63; ResourceManager.FindReactantsNodeGroup:723–755; RocketCore.UpdateState:172–217 | Required reactants must have positive float mass; no duration/demand parameter in availability. Core uses availability and commanded/minimum burn duration | Staged realization; no exact admitted debit or exhaustion lease |
| Module updates | VehicleUpdateState.UpdateModules:371–392; PhysicsBubble.UpdateModulesJob.ExecuteOnBubble:36–41 | Gimbal/tank-transfer/rocket/other module updates precede constrained physics | Some resource-related staged writes can precede physics; do not claim all resource writes occur afterward |
| Nozzles → body wrench | VehicleUpdateState.UpdateActiveNozzles:396–423; RocketNozzle.UpdateState:217–227 | Float force from performance/direction; torque from location relative to current COM; flow and cutoff time retained | Prepared inputs, not already-applied physical impulse |
| Command-duration weighting | ActiveNozzle.ComputeThrustMod:19–22; PhysicsStates.ComputeDerivatives:891–903 | Clamp(float((ThrustTime − intraStepTime)/dt),0,1) scales force, torque and nominal flow. Cutoff assignment also passes through float | Averaged commanded-duration weighting; not proof of exact fuel-shortage admission |
| Retained solver preparation | PhysicsBubble.FullPhysicsStepUntil:1369–1380; ConstraintSim.UpdateSimFromVehicle / PushBodyStateToSim:261–267; VehicleProperties.CopyToBepu:64–68 | Body state/inverse mass/inertia uploaded before module jobs and constrained step | Private BEPU inputs; no continuous exact resource-mass law |
| Outer physical update | PhysicsBubble.FullPhysicsConstrainedStep:1953 onward | Compute derivatives once; retain prepared disturbances over nested slices | Staged physical advancement |
| Inner constrained slice | Same method; ConstraintSim.Simulate:851–867 | Each slice min(remaining,1/60); float BEPU timestep; retained solver resources / SolveDescription(8,1) | Timestep exception poisons private simulation and rethrows |
| Force/torque integration | PoseIntegratorCallbacks.IntegrateVelocity:48–93 | Prepared body wrench transformed by current BEPU orientation; staged mass/inertia read; double computations narrowed to float velocity increments | Force applied before solver contact response; terrain impulse also influences angular damping |
| Contact / retrieval | ConstraintSim.ReadBackFromSim:407–415; KinematicStates.FromBepu:25–36 | Simulate → readback on every nested slice; BEPU body values widened to staged double fields | Widening does not recover lost precision; no live vehicle commit yet |
| Resource debit / mass update | VehicleUpdateState.ConsumePropellantFromActiveNozzles:486–494; VehicleProperties:133–150,99–107 | After nested physics/readbacks: nominal flow × outer dt × modifier; recompute staged mass properties | Debit to new buffers. Can precede later failure in postprocessing or application |
| Ready result | VehicleUpdateTask.Run:176–207; PhysicsBubble.PublishResults:916–921 | Prepare → worker jobs → publish ready only if bubble not failed | Readiness is a game-owned boundary |
| Live body/resource application | VehicleUpdateTask.ApplyResultsToMainThread:141–173; Vehicle.UpdateFromTaskResultsUnsynchronized:2399–2507; ModuleStateful.StateList.UpdateBulkStates:383–410 | Sequential kinematics, properties, flight computer and part/module copying; synchronized postwork/render events afterward | Live fields become game state. Inspected apply method has no rollback/exception region; not NovaCore atomic motion/resource publication |
| Next update | Rocket.UpdateRockets:98–101; RocketCore.UpdateState:189–216 | Rechecks now-dry resources, zeros realized throttle/durations and computes conditions | Requested controller activation need not turn false. EngineController.ComputeActivePerformance:68–75 separately returns zero for unavailable propellant |

Constrained ordering, directly corroborated in installed IL:

    ComputeDerivatives(outer dt)
    loop: terrain preparation → Simulate(slice) → origin propagation → ReadBack → RecordTimestep
    ConsumePropellantFromActiveNozzles(outer dt)
    UpdateVehicleFromSim / measurements / activity
    ready publication
    game-owned application

No mass/inertia upload appears in that inner constrained loop. End-of-outer-update consumption changes staged mass properties; a later applicable population uploads them. The callback reads current staged properties; this is not evidence of a continuously evolving mass model.

## Remaining liquid fuel < one update's demand

For the inspected active liquid-combustor path, positive required reactants admit realization even when their total is below interval demand. RocketCore.UpdateState has no available-mass/demand calculation; its moleStates parameter does not compute a fuel-limited duration or scale.

Consumption requests nominal mass later. Combustor.ConsumePropellant discards MassChange's success and actual amount. Mole.ConsumeStored clamps an insufficient store to zero and returns the previous mass. ResourceManager distributes consumption across eligible routes/reactants, so clamping is bounded by actual available stores. No consumed-amount feedback adjusts the already-completed physical response in this path.

Therefore the source establishes: positive availability → prepared nominal force → ordinary physical update → bounded staged drain → later dry realization. It does not establish exact interior exhaustion, corrected fractional impulse for shortage, a resource-driven shorter BEPU dt, or a separate contact piece. This is a source-derived implication; no new runtime shortage experiment was performed.

A finite commanded cutoff is different: its ratio modifier weights force and consumption over the outer update. It is not an exact resource-event timestamp and must not be cited as one.

## Solid motors and planning are different responsibilities

SolidMotor.UpdateState:195–226 stops firing when remaining burnable grain is <= max(previous massFlow × float dt, initial grain × 0.0001), or pressure fails its threshold. Continuing motors derive remaining thrust time from mass/rate. This is an existing pre-update cutoff/residual policy, not the liquid post-drain mechanism or exact NovaCore exhaustion. Consumption is distributed through grain segments at :655–673.

TryPrepareDrain/TryAccumulateDrain call sites in SequencePerformanceList:417–438,622–682 operate on simulated sequence-performance masses, durations and delta-v. They do not prove the constrained live path uses exact fuel admission. The September 13 later history discusses unifying paths; that code cannot be attributed to this September 7 installation.

## Staging and failure are not atomic equivalence

VehicleUpdateState.GetNewMoleStates/GetNewProps:176–192 and ModuleStateful.StateUpdater.GetNewStates:674–695 copy state on first update. Staged drain after successful inner stepping may still precede a later bubble failure. Failed bubbles do not publish ready normally. During live apply, however, fields are written sequentially; the inspected path does not roll back already-written fields on an application failure. Broader recovery outside this path is UNPROVEN.

## Supported engine changes and private history

FullPhysicsStepUntil:1225–1264 uses commanded/active actuators in taking vehicles off rails. Ordinary supported propulsion then follows prepared force + retained constrained stepping. No exact resource-event split or subnormal warm-start responsibility is present in the traced path. This bounded finding is not a universal proof about every KSA event path. PhysicsContext.MinDeltaTime is not a universal lower-bound theorem; remainder handling exists.

Warm starts/manifolds remain solver-owned, but BodyTerrainAccumulatedImpulse is also consumed by PoseIntegratorCallbacks:74–92 for angular damping. Private does not mean irrelevant.

UNAVAILABLE / UNPROVEN: KSA private tests; exact subnormal event/contact requirements; a general same-time powered/coast canonical transaction; rollback-safe body/resource application; correctness of installed shortage behavior against NovaCore precision outcomes. None is filled in from history or memory.
