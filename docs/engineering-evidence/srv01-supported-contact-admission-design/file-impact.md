# Proposed file/type impact, no implementation performed

All paths repository-relative. Names of new files/types are design labels, not
existing source. One writer should implement after Project Control accepts scope.

| Class | Existing or proposed path/type | Bounded responsibility |
|---|---|---|
| REUSE | src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDesign.cs; Data/SRV01-FourHorn.json; AssemblyStockCatalog | Existing canonical design, part transforms, tensor and catalog identity; keep stock physical values/digests unchanged |
| ADD | Spacecraft/Assemblies/AssemblyContactProfile.cs and one bounded authored profile data file | Explicit physical child geometry + part/design provenance, immutable cold admission. No GLB-as-collider or replacement mass |
| ADAPT | Spacecraft/Assemblies/AssemblyFlight.cs | Explicit contact consumer/schedule admission and typed contact provenance/observation while preserving free-flight defaults/records; do not silently widen free-flight constructors |
| ADAPT | Spacecraft/Assemblies/AssemblyApplicationSession.cs | Cold consumer composition, same state/clock/engine; retain default free-flight route and save behavior |
| ADAPT/ADD PARTIAL | Transactions/SimulationTransactionEngine.Assemblies.cs; SimulationTransactionEngine.AssemblyContact.cs | Reuse source/owner/history/credit/slot commit responsibility, contact pending/ack flow and mutually exclusive consumer guards; no new transaction engine |
| REUSE | Spacecraft/SpacecraftStateStore.Assemblies.cs; SimulationState assembly install wrapper | Same canonical slot and exact expected-state check; no new physical store; access factoring only if directly required |
| ADAPT | Transactions/SimulationTransactionEngine.AssemblySave.cs | Refuse unsupported contact consumer before free-flight serialization/replay; no contact persistence feature or schema/2 reinterpretation |
| ADAPT/ADD PARTIAL | Spacecraft/Contact/Staging/LocalContactWorld.cs; LocalContactWorld.Assembly.cs (new) | Typed source/binding, COM transport, same private step/export and owner cleanup; no second numerical kernel |
| ADAPT | Contact/Staging/LocalContactSource.cs | Keep old source contracts; factor immutable frame/interval/precision configuration only where identical; typed assembly source remains distinct from legacy capture |
| REUSE/BOUNDED ADAPT | LocalContactWorld.Article.cs; LocalContactCallbacks.cs; CompoundContactCoverage.cs | Factor explicit compound-child creation/ownership for authored part poses; scratch bounded by admitted count, full child identity diagnostics; keep existing article construction and callbacks outcome |
| KEEP POLICY | CompoundContactSelector.cs | Existing four-row selection, stable features, prediction and precision policy; no solver tuning/coverage research |
| REUSE | AssemblyResources.cs, AssemblyDynamics.cs, AssemblyProfileAdmission.cs | Canonical mass/actuation math and unmodified free-flight behavior. Contact must not call free-flight evolution or broaden its resource interval guard |
| REQUIRED MINIMAL DISPLAY AFTER CHEAP PROOF | samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs or a narrow sibling scene; Program.cs; tools/NovaCore.Launcher/ScenarioCatalog.cs / LaunchCommandBuilder.cs | Reuse copied part presentation in a separately labelled supported route plus slab. Existing free-flight launch unchanged; no plume/UI redesign |
| ADD TEST | tests/NovaCore.Simulation.Tests/AssemblyContactAdmissionTests.cs (proposed), Program.cs registration | Identity/shape/COM/owner/receipt/lifetime/failure/trajectory/accounting contracts |
| EXTEND REGRESSION | AssemblyProductionTests.cs, Srv01IntegrationTests.cs | Wrong-consumer refusal, banked free-flight/save semantics preserved; don't rewrite golden endpoints |
| ADD/EXTEND PRESENTATION TEST | tests/NovaCore.Graphics.Tests/StockAssemblyPresentationTests.cs and launcher tests | Copied supported pose, correct slab/actual state, no double evolution/render multiplication |
| REUSE TESTS | LocalContactStagingTests, PersistentContactPublicationTests, ContactServicingTests, PoweredContactTests.*, EngineeringContactArticleTests.*, CompoundContactSelectorTests | Existing fixtures/thresholds untouched and rerun appropriately |

The new types carry a missing immutable geometry description or a typed consumer
binding/record. They do not own a second canonical resource store or assembly.
History distinguishes contact evidence from free-flight numerical provenance;
free-flight save hashes must stay stable (do not serialize new default fields
into its existing format accidentally).

## Must not change in the future slice

BEPU binaries/version/manifests/provenance; canonical tick/resource arithmetic;
M14.17 clearance proof; M15.1 ordinary solver/resource/exhaustion contracts;
M15.2 stock mass/COM/inertia/thrust/parts/feeds/visual asset data, free-flight
evaluator and golden endpoints; selector policy; renderer/native/exhaust shaders;
legal files/public README; Florida/terrain; historical evidence/tags. No KSA
installation writes or copied protected KSA assets/source. If a supposedly
necessary change crosses those boundaries, stop and return the evidence.
