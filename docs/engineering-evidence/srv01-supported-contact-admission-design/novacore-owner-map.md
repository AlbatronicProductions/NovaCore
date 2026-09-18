# Current NovaCore ownership and exact rejection

Source at ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5, read only after the
independent KSA prerequisite passed. Paths below are repository-relative;
method names, not line numbers alone, identify the evidence.

| Authority | Current type/method and path | Writer -> reader; lifetime and revision; publication |
|---|---|---|
| Spacecraft/assembly/part identity | Spacecraft/Assemblies/AssemblyFlight.cs: AssemblyLaunch; AssemblyDesign.cs: CompiledAssemblyDesign. Both under src/NovaCore.Simulation | Cold compile/launch -> store/actuation/display; immutable design digest, launch ID, spacecraft/frame IDs, length-prefixed part/capability/store keys |
| Canonical assembly slot | Spacecraft/SpacecraftStateStore.Assemblies.cs: CreateAssemblies, TryPrepareAssemblySlot, InstallAssembly (13,38,40) | One canonical launch/state array; engine prepares exact source slot and installs complete AssemblyRuntimeState; global StateRevision advanced through SimulationState wrapper |
| Part transforms | AssemblyDesign.cs: CompiledPart, Compile | Immutable canonical instance poses and authored definitions -> mass, wrench and presentation; no physics-derived authoring |
| Mass/COM/inertia | AssemblyDesign.cs: ObserveMass (158); AssemblyFlight.cs: ObserveMass (70) | Exact two-store total + compiled dry properties -> FP64 AssemblyMass/Matrix3; part first moment and origin tensor authoritative; successor stored atomically |
| Fuel and oxidizer | AssemblyResources.cs: AssemblyStores, Calculate; AssemblyRuntimeState | Pure preparation produces exact After stores; transaction engine alone installs; ResourceRevision advances only for nonzero use |
| Commands/main/RCS/gimbal | AssemblyFlight.cs: compiled Plan and AssemblyFlightPreparation.Evaluate; AssemblyProfileAdmission.cs: AssemblyActuation | Immutable request -> held wrench, proposed actual/target gimbal and realized sixteen-jet mask; ActuatorRevision advances per accepted interval; never callback-owned |
| Current free-flight motion | AssemblyDynamics.cs: Evaluate/Derivative | Pure segmented RK4 at material O; reads exact consumption and FP64 properties, cannot publish/spend; no gravity/contact in this consumer |
| Assembly transaction | Transactions/SimulationTransactionEngine.Assemblies.cs: BeginAssemblyFlight, CheckAssemblySource, PrepareAssemblyInOwnedPhase, PublishAssemblyInOwnedPhase (46,36,118,152) | Existing owner thread/phase, single active proposal, seal+generation, expected state/revision/timeline/clock; fixed typed slot/clock/history writes at 162-170 |
| Host accounting | Same file: AdmitAssemblyHostTime (87), ServiceAssemblyFlightDebt (173) | Sequenced credit is canonical before private acknowledgement; clock PrepareHostAdvance owns conversion; service max four; preallocated credit/history arrays |
| Contact source | Contact/Staging/LocalContactSource.cs: Capture, CapturePoweredPreparation, Validate, TryEndpoint | Existing box/article or named powered fixture plus legacy/applied physical source; immutable original epoch/frame/end and captured authority. **No assembly variant today** |
| Native world/body/shapes | Contact/Staging/LocalContactWorld.cs: constructor, TryCreate, Step, TryDispose | World owner thread owns BufferPool, Simulation, body/static/shape handles and generation; bodies retain BEPU private pose/velocities. No canonical write capability |
| Compound construction | LocalContactWorld.Article.cs: CreateArticleShape | Three explicit EngineeringContactArticle boxes; canonical COM-relative children, no builder mass/recentering. Not a ready SRV collider adapter |
| Contact coverage/manifolds/warm starts | LocalContactCallbacks.cs; CompoundContactCoverage.cs; CompoundContactSelector.cs | Callbacks collect child rows, existing bounded four-row selector; BEPU owns pair/solver state. Scratch allocated from actual child count; diagnostic child mask remains article-specific |
| Powered ready/apply | LocalContactWorld.Powered.cs and SimulationTransactionEngine.PoweredContact.cs | Token rechecks -> private step/export -> pending receipt -> canonical powered publication + fixed acknowledgement. Source inertia/one-store fixture assumptions are explicit |
| Existing contact history | Transactions/PersistentContactPublication.cs and SimulationTransactionEngine.PersistentContact.cs; powered history in PoweredFreeFlight.cs | Value provenance only, one per interval, engine owns commit; no native handle in canonical history |
| Exclusive phase | Transactions/ContinuationPublicationPhase.cs | Existing thread/phase guard, not a second lock; ordinary mutation cannot interleave fixed publication, but between calls external mutation remains possible and must be detected |
| Copied display | samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs: Observe, PresentedPart, BuildSubmission (98,102,110) | Reads copied AssemblyFlightObservation; material-O + part pose, actual gimbal/jets; holds endpoint, never reads BEPU |
| Persistence/replay | Transactions/SimulationTransactionEngine.AssemblySave.cs: SaveAssemblyFlight/RestoreAssemblyFlight | Current schema/2 cold save replays **free-flight** plan through engine and compares history/current; cannot represent a contact solver continuation implicitly |

## Rejection trace

1. The ordinary command/resource producer requires legacy translation:
   SimulationTransactionEngine.SpacecraftCommands.cs:52-53 returns
   SubjectUnavailable for an assembly. FinitePropellant binding also assumes
   one central reservoir and fixed dry inertia (Engine.Propellant.cs:37-47,70-73).
2. SpacecraftAppliedEndpoint.cs:34-40 captures only a legacy pair or an applied
   endpoint. It cannot capture AssemblyRuntimeState. SpacecraftStateStore.cs:30,
   119-123,161 excludes instantaneous assembly authority from legacy getters.
3. BeginPoweredContact -> CapturePoweredPreparation (LocalContactSource.cs:195)
   requires the named numerical fixture and inertia (2,2,2), then the same
   legacy/applied physical capture. It is not an assembly producer.
4. PublishPoweredFlightInOwnedPhase, Engine.PoweredFreeFlight.cs:295/317,
   finally calls TryPrepareAppliedSlot. SpacecraftStateStore.cs:39 explicitly
   rejects `IsAssembly(index)` before InstallAppliedEndpoint.

The last guard is a second-line defense, not the only obstacle. It protects
**one authoritative physical representation per canonical spacecraft**. The
typed assembly contains material O, both stores, COM/tensor, gimbal, realization,
resource/actuator revisions and frontier; the old endpoint cannot replace it.
M15.2 introduced assembly storage and this exclusion together. AssemblyProductionTests
222/227 protect typed material-O observation and no duplicate legacy authority.

Preserve the guard. Add a typed assembly contact consumer and reuse
TryPrepareAssemblySlot/InstallAssembly plus the existing owner-phase publication
responsibility. No conversion to a box identity, legacy force segment or second
canonical endpoint store. Exact source, event, clock, debt, capacity and receipt
checks are outcome protections. Fixture inertia, single reservoir and capture-union
restrictions are model boundaries, not evidence that contact needs another craft.

## Box versus SRV-01

| Responsibility | Existing supported box | Current SRV-01 | Classification |
|---|---|---|---|
| Canonical identity | Legacy/applied single craft | Assembly slot + seven parts, two stores, sixteen jets | ADAPTATION REQUIRED |
| Collision | 2x1x1 box; separate three-box article also exists | No canonical collision declarations | ADAPTATION REQUIRED: explicitly author physical part profile |
| Visuals | Qualification geometry | Four reusable GLBs, seven instances, 37 mesh instances | EQUIVALENT copied pose; geometry binding adaptation |
| Mass/COM/inertia | Central reservoir; fixed (2,2,2) fixture inertia | 630 kg dry, 705 kg initial wet; material O distinct from COM, full derived tensor | ADAPTATION REQUIRED; never borrow fixture mass law |
| Resources | One exact reservoir | Exact fuel+oxidizer with 2:3 law | ADAPTATION REQUIRED, no duplicate stores |
| Forces/actuation | Qualified single-engine contact fixtures | Main/gimbal + sixteen jets | Unpowered equivalent; powered combinations OUTSIDE first slice |
| Frames | Native body COM, fixed local frame | Canonical material O + COM offset | ADAPTATION REQUIRED explicit paired conversion |
| Solver | Retained BEPU, 8 iterations/1 substep | No contact consumer yet | EQUIVALENT ownership; qualify new shape source |
| Publication/history | Powered endpoint/resource successor | Typed assembly successor/record | ADAPTATION REQUIRED, preserve common atomic responsibility |
| Schedule | 60 Hz integer lattice | Stock 128x15625 ticks, 64 Hz/2 seconds | ADAPTATION REQUIRED for contact only; free flight unchanged |
| Property refresh | Source mass, post-step successor | Canonical stores derive tensor/COM | EQUIVALENT owner ordering; powered COM refresh OUTSIDE first slice |
| Replay | Not a generic solver snapshot | Free-flight exact replay schema/2 | Contact save/restore OUTSIDE first slice, explicit refusal |

No new production behavior has been implemented or qualified by this map.
