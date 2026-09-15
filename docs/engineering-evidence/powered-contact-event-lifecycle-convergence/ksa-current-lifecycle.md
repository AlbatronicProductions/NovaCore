# Current installed KSA 5438 lifecycle: bounded revalidation

## Identity and scope

Closing inspection found a different current installation from the initially inspected 5402 binary. The replacement mechanism/time is not inferred. The earlier readable source and tokens remain explicitly labeled in ksa-5402-snapshot.md and ksa-source-provenance.md.

ACTUAL CURRENT root: E:\Kitten Space Agency
Build: 2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9

| Current file | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4936792 | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| KSA.deps.json | 73752 | 23FCAA825C40F828349310C8DC480F292F9BE65DE6C4D378B36FBA64F7EE37B3 |
| BepuPhysics.dll | 880640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

BEPU product remains 2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7, assembly 2.5.0.0. That unchanged backend does not prove KSA lifecycle unchanged.

Two read-only reviewers freshly decoded current installed method bodies and resolved call/field operands and decisive helper closures. Raw IL hashes alone were not treated as semantic equivalence across metadata tables. No new decompiled tree, source copy, build, KSA execution or numerical qualification occurred.

## Current chain and ownership

| Boundary / owner | Current type/method/token and resolved evidence | Timing / precision / authority |
|---|---|---|
| Control → command | FlightComputer.ComputeControl 06000966: EngineOn selects zero/infinite requested duration 025F–027A; requested throttle 0281–0286; CommandEngineThrottles 0297. CommandEngineThrottles 06000967 writes command fields 0051/008A/009C | Game request becomes staged engine command; not realized resource-backed output |
| Availability / realization | ResourceManager.ResourceAvailable 06002507 iterates ConsumptionOrder levels; TryGetMoleAndState 00B0, float Mass 00BE, positive comparison 00C3. Current liquid realization still uses availability and command rather than available/demand interval ratio | Prepared/staged, no exact NovaCore debit or event lease |
| Body wrench | VehicleUpdateState.UpdateActiveNozzles 06001C59 uses realized nozzle thrust, direction/current COM. PhysicsStates.ComputeDerivatives 06001B8F: propellant check 02EE; modifier 0320; force/torque 0341/034F; addition 0359; mass rate 0361 | Float nozzle wrench → prepared double disturbances. Derivative body changed elsewhere; only this block is claimed |
| BEPU input population | PhysicsBubble.FullPhysicsStepUntil 06001C02: population 07F9/0846 precedes module job 0912/execution0936 and constrained call095E. VehicleProperties.CopyToBepu 06001B40 writes inverse mass000F/tensor0025 | Staged mass/inertia uploaded before constrained update; module updates now also include FxDeformation. Do not assume every module is unchanged |
| Outer constrained substep | PhysicsBubble.FullPhysicsConstrainedStep 06001C19: derivatives009D; cap1/60 at0020; Simulate0296→ReadBack02D6→RecordTimestep031E; consume0364→UpdateVehicle0388 | Derivatives/propellant once per selected OUTER SUBSTEP, not necessarily once per display frame |
| Force into private physics | PoseIntegratorCallbacks.IntegrateVelocity 060006BD: prepared Disturbances00AE; staged inverse mass00C4/inertia00EA; current orientation; dt factors011D/013B; float-vector projection | Force/torque enters constrained evolution; no continuous exact resource-mass law inside callback |
| Solver / poison | ConstraintSim.Simulate 0600069D: Timestep002F; exception handler _isPoisoned003C | Retained float BEPU step; exception poisons continuation |
| Staged consumption | VehicleProperties.ConsumePropellantFromActiveNozzles 06001B46: modifier002F; core consumption0060; mass recompute007E. VehicleUpdateState.ConsumePropellantFromActiveNozzles06001C5B calls properties consume0038 | Outer-substep demand into staged new resources/properties after inner physics/readbacks; later failure remains possible |
| Ready | PhysicsBubble.PublishResults06001BF8: failed check0004, ready0012 | Successful prepared result eligible for game application |
| Live apply | Vehicle.UpdateFromTaskResultsUnsynchronized06002F31: kinematics0036, properties0062, flight computer039E, parts/modules03AB. VehicleUpdateTask.ApplyResultsToMainThread06003115: ready gates; failure rethrow0085; synchronized apply00CD; render events0115 | Sequential live authority writes, no exception region/rollback in inspected apply method; not atomic NovaCore publication |

Moles/properties remain staged copies until game application; current consumption uses GetNewMoleStates/GetNewProps. Tank/module operations before physics mean resource-related staged mutation is not universally deferred until physics success. Successful constrained stepping precedes its nozzle drain, but later postwork/application can still fail. Body/resource writes are not a single rollback-safe transaction in the inspected current apply closure.

Mass upload is still absent from the nested constrained slice loop. The velocity callback reads staged mass/inertia and frozen disturbances; end-of-substep resource drain recomputes properties for later updates. This is not a proof that such cadence meets NovaCore mass-weighted physical precision.

## Current liquid shortage and changed routing

CURRENT ResourceAvailable uses ConsumptionOrder across routing levels, replacing the old FindReactantsNodeGroup-centric description. It still tests positive available reactants without dt or demand. Current ConsumeNodeGroup060024FF divides remaining demand by CountCarriers06002500 with requirePropellantUse=true and requireStoredMass=true; carriers must be eligible matching molecules with positive mass. That replaces the old nodeGroup.Length denominator.

The core exhaustion conclusion survives this changed helper closure: current Combustor.ConsumePropellant0600153B calls MassChange000B and discards the result at0010. Current Mole.ConsumeStored0600149C still stores zero for insufficient mass and returns the old amount. No actual-consumed feedback reduces the already-completed physical response in this traced liquid path.

Therefore positive required reactants below a substep's nominal demand still do not create a proved exact fuel-limited engine duration/contact piece here. Drain clamps actual stores, and later availability evaluation yields dry/zero realized output. This is a static source consequence, not a new runtime experiment. Requested controller activation is distinct from actual unavailable-engine performance.

Commanded time remains different: ActiveNozzle.ComputeThrustMod06001BA7 retains remaining-command-time/dt, conv.r4 and clamp operations. It scales prepared wrench and requested consumption, not an exact shortage event.

Fresh installed-IL callsite closure found TryPrepareDrain/TryAccumulateDrain used only by SequencePerformanceList.RegisterDrainCore06002614 at00A5 and TryCommitCoreDrain0600261B at0021. Those helpers are not evidence that live constrained contact suddenly performs exact event admission. Current routing helper changes are direct implementation evidence consistent with parts of the retained resource-history discussion; completeness of every historical bug fix is not claimed.

Current SolidMotor.UpdateState060015CE was separately decoded: it cuts off when remaining grain is <= max(previous flow*float(dt), initial grain*0.0001), or pressure fails its threshold; otherwise duration is remaining mass/flow. Resolved flow/float multiplication00C6–00CD, initial grain/threshold00CF–00DA, cutoff00DF–010B, duration division0193. This remains a distinct pre-update residual/shutdown policy, not exact interior fuel-event splitting. Current Rocket.UpdateRockets06001513 checks availability0124/writes015D before engine0389/nozzle04B7; RocketCore.UpdateState06001524 dry branch zeros realized throttle and remaining times012D/013C/014B. No liquid/solid conflation.

Current staged-copy closure: VehicleUpdateState.GetNewMoleStates06001C45 sets staged ValuesUpdated0012 and obtains new-state span0022. GetNewProps06001C46 tests staged HasValue001A; reads ReadOnlyVehicle.Props0027, stores copied NewVehicleProperties0039 and returns staged ref004B. ModuleStateful.StateUpdater.GetNewStates06003F38 reads read-only states0026, creates destination new-buffer span0034/0043, copies0048, returns new-buffer span0050–0064. StateList.UpdateBulkStates06003F1C installs global state000D and later copies updater new states into live state span00EA, after length checks. Copy-before-mutation is proven; global-state-first installation reinforces that failure-atomic application is NOT established.

## New current owner route: pair-clearance horizons

The initial 5402 unconditional runner description is superseded.

VehicleUpdateTask.Run06003116 checks _horizonPairs.Count; it chooses ordinary parallel stepping or StepBubblesWithHorizons0600311A, then SplitBubbles and PublishResults. Horizon segments invoke:
BubbleStepSegmentJob.ExecuteOnBubble0600312D → PhysicsBubble.StepFrameTo06001BE2 (target-StepTime) → full-physics branch StepFullPhysicsTo06001C00 → FullPhysicsStepUntil06001C02 → SAME constrained derivative/slice/drain closure. FullPhysicsBeginFrame is guarded once by _fullPhysicsBegan. Ordinary RunStep06001BF4 also uses BeginStepFrame→StepFrameTo(frame end)→EndStepFrame.

Direct horizon provenance: MergeBubbles06003118 sets Horizon from ClearUntil05D6/05DB; scheduler selects frame/pair-horizon minimum. ResolveExpiredHorizonPairs0600311B calls ShouldMergeBubbles0600311C at the pair horizon; cluster states/rails orbits reach BubbleMergePredicate.ShouldMergeBy06001C68, which evaluates linear/analytic pair-envelope overlap.

This is pair-clearance segmentation in the inspected owner scheduler. No explicit fuel-demand/exhaustion/debit call was found there. It does not exclude indirect propulsion effects on motion bounds or every KSA event path. Do not say “KSA never splits physics” or “once per display frame.” The finding is no separately represented exact fuel-empty contact piece in this inspected lifecycle.

## Current IL hashes

Hashes below identify GetILBytes(); resolved operands above establish semantics.

| Current method | Token | IL SHA-256 |
|---|---|---|
| ConstraintSim constructor | 06000684 | FBED1FA7C813078C59F1A80BDF32633351A4F658BC88C7D85AFAF1036153D0EC |
| FullPhysicsConstrainedStep | 06001C19 | 042293A6AED45AEB5EDBA6189A118B1787352503142BCCD718739E7DE34CAAE8 |
| ConstraintSim.Simulate | 0600069D | 2FB3153A787D195A0A066C4368F62FB40EBDD6FC086DB092E838DA2400A9BAA4 |
| PoseIntegratorCallbacks.IntegrateVelocity | 060006BD | F2C76657B851D8B13423979C03513AB95419269A84303240DF2347ADE1CBA08D |
| VehicleProperties.CopyToBepu | 06001B40 | DF68A823F4B416798138601EBE0C655D4006C84A20DD43271B04B855E5461540 |
| FullPhysicsStepUntil | 06001C02 | 197390C814CB457EE10A1243BF424872C9E9207F1FEF41FA18583BB6B2EFD555 |
| Vehicle.UpdateFromTaskResultsUnsynchronized | 06002F31 | F9D8F3C4128AE6659C873F4CDAE90274D95411826CC443D3BBB0E472D7AF6C48 |
| VehicleUpdateTask.Run | 06003116 | 278C810E97FA6BF33039890AE2596DC21206D9D73B1A4E429B01432969FC7721 |
| StepBubblesWithHorizons | 0600311A | 0E2C568B31343F995ACFF33138FE40E1DF07351976DEF29406DDDD3BBC9FD830 |
| StepFrameTo | 06001BE2 | CAD0462D93B866E6D682AF18C2EB5E59863C3EF30200D27D1928C90976E67460 |
| StepFullPhysicsTo | 06001C00 | 43C3793FADA82D415A39B2868921EA8FF13A6AD0A3914762312894834EED150C |
| ResourceAvailable | 06002507 | 97FA59BAD19DB9BE8FB02C7EFABC9ADD18241C9FC3F58E4C91E3242C7FC76BA7 |
| Combustor.ComputePropellantAvailable | 0600153A | DC3B943F344A0BE8C5BF2F354603DA18CC8D644A6A988F52A5DBCBF6E1034D9F |
| Combustor.ConsumePropellant | 0600153B | A826E9D5BC486ED5370CC993C00C22E05DEC9B551185A3D6C2B13D954E35C659 |
| ConsumeNodeGroup | 060024FF | 0CDAFD165A7AB1B66256C1C41D1D6A573292F4176380A98708B7C8A14FFD086F |
| CountCarriers | 06002500 | 756758A4857AF725F03538352F760DC6AB92D43DEFDCD5776FA32832A439CE5E |
| Mole.ConsumeStored | 0600149C | 547FD508CE0345E8306B31D6DA7816499A633E3A83A07ED02D9EB96B2434D214 |
| ActiveNozzle.ComputeThrustMod | 06001BA7 | 9E0810301B6540FB0C4A5218E0974340CB2D2397837EE61D5B655D8A5A781A98 |
| RocketCore.UpdateState | 06001524 | 00D9728406BE86D835BCAE885624930A0395D25E617EBE997CE7C7A3F62A1242 |
| Rocket.UpdateRockets | 06001513 | 9B0D74377DBDC6EC73E5E9D5A79D4F4E9DC3676BBE1A8506FBEB8F6543B93C88 |
| SolidMotor.UpdateState | 060015CE | CD5555FBCE2550878AABBCD21279A1F03A7532EB908066A6B602F5F5D1656353 |
| VehicleProperties.ConsumePropellantFromActiveNozzles | 06001B46 | CD4DE8A508C052A5EB7252CF144ABBCDAE0D911520269E0423374FE705FBD337 |
| VehicleUpdateState.UpdateActiveNozzles | 06001C59 | CA6A4DD9B11D4F3AA4FCA37894EB4D872F11F3BCF166C7EC2E0138F3A7567417 |
| VehicleUpdateState.ConsumePropellantFromActiveNozzles | 06001C5B | 76D0F7DD9D870A175464AB3BE1098035921F0BB5638261F9D6A7916EF5C97755 |
| VehicleUpdateState.GetNewMoleStates | 06001C45 | 67EBA5F8E26135DB2C479375D72B3BFF47EF9B9B34F61F6C11B696445D2D93D5 |
| VehicleUpdateState.GetNewProps | 06001C46 | 387021B916E7571BF3CA997B374ACBFAC9B453FEB781E24D14D68956679D3ED8 |
| StateUpdater.GetNewStates | 06003F38 | CE4DC5FE272A94636EC580EBD95D50B0B7DFB9F8FCAEDAA467398C45C539E3A3 |
| StateList.UpdateBulkStates | 06003F1C | 99F93169B3A7E28A2827BB3F128F6E901B2E9B9B1FF3ACD83EED2CC29B23046A |

## Gate limits

Current constructor was independently rechecked: ConstraintSim06000684 loads8 at0047,1 at0048,64 at0049, calls SolveDescription constructor004B, Simulation.Create00A2, and retains Simulation at00A7. Current8 velocity iterations/1 substep are therefore direct installed evidence, not inferred from unchanged BEPU. No selection of those values for NovaCore follows.

Current 5438 private tests, exact subnormal contact contracts, exhaustive all-path absence and rollback-safe game publication remain UNAVAILABLE / UNPROVEN. Retained backend impulses remain real continuation and terrain aggregate can influence angular damping; unchanged installed backend identity and the rechecked callback preserve that accepted fact.

The bounded current-source update changes routing/owner detail but does not establish exact resource authority, a chronological event-integrated constraint map or NovaCore atomic publication in KSA. The analytical case for conditional C remains distinct from copying literal current B.
