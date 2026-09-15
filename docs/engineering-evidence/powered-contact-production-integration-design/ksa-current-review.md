# Actual current KSA source, history and equivalence

## Direct access and identity

Inspection date: 2026-09-15. Root: **`E:\Kitten Space Agency`**. Read actual installed member bodies using existing ILSpy `11.0.0.9375` and metadata/IL inspection. Old extracted build 5402 files and NovaCore reports were navigation only. No KSA execution, source dump, write or configuration change.

| File | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4,936,792 | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 880,640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163,328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| KSA.deps.json | 73,752 | 23FCAA825C40F828349310C8DC480F292F9BE65DE6C4D378B36FBA64F7EE37B3 |

KSA version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9` is still the current available installation, freshly verified. BEPU file version `2.5.0.0`, product `2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`. This is installation identity, not a claim about the newest globally released build.

## Current implementation findings

Tokens below identify actual current `KSA.dll` methods; do not reuse tokens if the DLL identity changes.

| Responsibility | Current method/token | Observed mechanism |
|---|---|---|
| Update owner | `VehicleUpdateTask.Run` `06003116` | Begin update, prepare bubbles, ordinary/horizon stepping, split, publish ready results, end phase. |
| Retained world | `PhysicsBubble.InitializeConstraintSim` `06001C26`; `ConstraintSim..ctor` `06000684` | Reuse existing simulation when present; cold creation retains BEPU simulation/pool/callbacks/handle-state table. |
| Body identity | `AddVehicle` `06000686`, `RemoveVehicle` `06000687` | Existing game `VehicleUpdateState` is associated with native handle; explicit removal clears native ownership. |
| Regime selection | `PhysicsBubble.FullPhysicsStepUntil` `06001C02` | Update nozzles/situation, populate native input where needed, run modules, then select constrained/unconstrained from `ConstraintSim.IsAnyConstrained`. Same staged vehicle identity. |
| Engine demand | `VehicleUpdateState.UpdateActiveNozzles` `06001C59`; `ActiveNozzle.ComputeThrustMod` `06001BA7` | Prepare realized body force, moment, flow and command duration; clamp command-duration weighting for the numerical interval. Not an exact fuel-empty ledger. |
| Disturbances | `PhysicsStates.ComputeDerivatives` `06001B8F` | Accumulate prepared thrust/moment and mass rate. |
| Native input/mass upload | `PopulateBepuStatesFromKinematic` `06001C33` → `UpdateSimFromVehicle` `0600068A` → `PushBodyStateToSim` `06000688`; `VehicleProperties.CopyToBepu` `06001B40` | Retain world/handle while uploading staged pose, inverse mass/inertia and required shape/bounds updates. NovaCore's no-pose-reimport acknowledgement is an intentional difference. |
| Constrained schedule | `FullPhysicsConstrainedStep` `06001C19` | Derivatives once per selected outer substep; native slices capped at 1/60; simulate/read back per slice, then consume propellant/recompute staged properties after the slices. **Not necessarily a mass update after every 1/60 slice.** |
| Native force callback | `PoseIntegratorCallbacks.IntegrateVelocity` `060006BD` | Apply disturbances with staged source inverse properties and current native orientation, yielding FP32 velocity inputs. No exact canonical fuel debit. |
| Contact/cache ownership | `NarrowPhaseCallbacks.ConfigureContactManifold` `060006C8/060006C9`; retained `ConstraintSim` | Game supplies filtering/material/contact observations; BEPU owns ordinary constraints/manifolds/warm starts. No normal per-interval cache clear found. |
| Step failure | `ConstraintSim.Simulate` `0600069D` | Native timestep exception poisons simulation and rethrows. |
| Readback | `ReadBackFromSim` `0600068D`, `UpdateVehicleFromSim` `0600068B` | Native body state copied into private staged kinematics/properties/environment. |
| Resource/property successor | `VehicleProperties.ConsumePropellantFromActiveNozzles` `06001B46` → `RecomputeMassProperties` `06001B43` | Staged resource drain then property recomputation; `GetNewMoleStates` `06001C45` / `GetNewProps` `06001C46` provide update buffers. |
| Unconstrained numerical mass | `PhysicsStates.IntegrateVelocityVerlet` `06001B92` | Source derivative, predicted mass/property scaling, endpoint derivative, actual consumption/recompute, final correction. Different from constrained source holding. |
| Support loss | `UpdateIsAnyConstrained` `060006A1`; `FullPhysicsEndFrame` `06001C03` | Ordinary active native body constraints inform constrained selection; fresh terrain contacts update staged situation. Next selected consumer can be unconstrained with same vehicle identity. Kitten and broader situation rules are separate. |
| Unconstrained consumer | `FullPhysicsUnconstrainedStep` `06001C1A`, `RunUnconstrainedVehicleStep` `06001C1B` | Same vehicle state, surface/analytic freefall/numerical maneuvering selected by situation. No proof here of an exact continuous departure instant. |
| Pair horizons | `VehicleUpdateTask.StepBubblesWithHorizons` `0600311A` | Bound segment targets with pair-clearance horizons, run progressing nonfailed bubbles and resolve expired horizons. This is not a fuel-empty contact-piece owner. |
| Ready/apply | `PublishResults` `06001BF8`, `ApplyResultsToMainThread` `06003115`, `Vehicle.UpdateFromTaskResultsUnsynchronized` `06002F31` | Failed work does not become ready; staged results apply in worker/synchronized/render phases. Sequential live writes do not establish NovaCore failure-atomic resource/physical transaction. |
| Retirement | `TryResetForPool` `06000683` | Poisoned simulation refused; explicit reset clears solver/bodies/statics/observations. Not a normal step operation. |
| Shortage limitation | `ResourceManager.ResourceAvailable` `06002507`, `Combustor.ConsumePropellant` `0600153B` | Positive-reactant checks are not exact interval-demand admission; actual consumed result is discarded by the inspected combustor path. Do not adopt as NovaCore exact exhaustion authority. |

## Decisive current IL provenance

Fresh hashes of `GetILBytes()` (not recompiled source):

| Token | IL bytes | SHA-256 |
|---|---:|---|
| 06001C02 | 2487 | 197390C814CB457EE10A1243BF424872C9E9207F1FEF41FA18583BB6B2EFD555 |
| 06001C19 | 1210 | 042293A6AED45AEB5EDBA6189A118B1787352503142BCCD718739E7DE34CAAE8 |
| 06001B92 | 1538 | AC8E722AEC2AB44C94481A067FE2B3D78024AA5BDB855074C612FC076AF46A41 |
| 060006BD | 1729 | F2C76657B851D8B13423979C03513AB95419269A84303240DF2347ADE1CBA08D |
| 06001B40 | 43 | DF68A823F4B416798138601EBE0C655D4006C84A20DD43271B04B855E5461540 |
| 06001B46 | 133 | CD4DE8A508C052A5EB7252CF144ABBCDAE0D911520269E0423374FE705FBD337 |
| 06001B43 | 96 | C8EA8E40DB5D09B19ADFECAD64FBB6A2B96F5EDDBDF79605CEAEF05210561BB0 |
| 060006A1 | 316 | F43A7F50D9BB80315DAD096E30DCB7A787E9E0297D0317D3629C49CAFC6FB20D |
| 0600069D | 106 | 2FB3153A787D195A0A066C4368F62FB40EBDD6FC086DB092E838DA2400A9BAA4 |

## Actual official live-changelog history

Directly inspected authenticated Discord channel **Kitten Space Agency / #live-changelog**, server `1260011486735241329`, channel `1260112103134724146`, via the existing in-app browser. Target message IDs and revision text were visible in the live accessibility state on this date. No message/reaction was sent or changed; no export or bulk capture created. These are GitBot engineering-history entries, distinct from current installed implementation.

| Actual source | Observed historical result | Reconciliation |
|---|---|---|
| [Revision 4549, June 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020) | Derivative preparation and propellant consumption moved outside repeated small collision steps; use active-nozzle flow instead of duplicated disturbance rates. | Current outer-substep preparation/consume pattern remains observable. Do not translate the historical wording into once per display frame or per every native slice. |
| [Revision 5177, August 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058) | Repeated execution of flight-computer jet commands caused overshoot, chatter and excess consumption, particularly under warp. | Supports exactly-once prepared-demand ownership and separate display cadence. Does not supply NovaCore exact resource leases. |
| [Revision 5434, September 13](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602) | Corrected reachability/share/drain errors, unified flight and planner drain path and fixed pooled-graph double release. | Current source must decide actual shortage semantics. Shared drain implementation does not imply exact interval exhaustion or atomic canonical publication. |

Discord display dates/revisions are recorded as observed. They are not inferred from the installed file's version date and are not used to infer that a newer revision is installed.

## Equivalence before design

| Responsibility | Equivalent outcome? | Classification / required NovaCore delta |
|---|---|---|
| Retained ordinary contact and caches | Yes, conditional on admitted physical model | **ADOPT** backend ownership; preserve banked NovaCore configuration/precision, no new event-piece solver/cache owner. |
| One game owner selects physical consumer | Yes | **ADAPT** current KSA owner pattern to existing transaction engine; no third powered-contact physical state. |
| Force preparation before native integration | Yes for prepared numerical input | **ADAPT** fixed-workspace exact-event projection, once per actual admitted interval. The bounded mass choice is resolved; the architecture explicitly defines prepared environment and source-frame applicability for subsequent implementation validation. |
| Source properties then staged successor | Lifecycle equivalent; scope-specific numerical evidence required | Subsequent bounded review selected **A. ADOPT KSA SOURCE-MASS POLICY** for the retained ordinary-contact scheme; **ADAPT** the exact canonical acknowledgement boundary. KSA has different constrained/unconstrained numerical schemes. See mass-stop.md for actual scope/proof. |
| Finite-resource/exhaustion authority | No, literal mechanism insufficient | **INTENTIONALLY DIFFER** using banked exact amount/duration, genuine proposal and canonical sole writer. No callback debit. |
| Ready/apply and failure poisoning | Partially | **ADAPT** private staging/failure lifetime, retain NovaCore fixed canonical commit and distinct postcommit invalidation. |
| Body state application | Partially | **INTENTIONALLY DIFFER** in no native pose reimport at same-phase self-publication, canonical FP64 bits and exact history/revisions/debt. |
| Supported → unconstrained selection | Owner lifecycle is useful precedent | **ADAPT** future same-identity boundary; no exact departure certificate, new recontact system or broad KSA situation framework inferred. |
| Pair horizons | Not the same as exact fuel authority | Leave KSA horizon system out of this bounded responsibility. Do not claim KSA never subdivides physics. |

**KSA source and relevant history available and directly inspected: COMPLETE for this gate.** No contradictory simpler owner forces rejection of the accepted direction. The separate bounded mass review and architecture are recorded alongside this current-source map; no broad numerical or production qualification is implied by KSA equivalence alone.
