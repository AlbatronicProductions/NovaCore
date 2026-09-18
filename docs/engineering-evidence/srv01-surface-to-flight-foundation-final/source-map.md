# Final source ownership and cleanup

| Responsibility | Current files |
|---|---|
| Canonical assembly resources, command/dynamics, sealed consumer proposals and application | src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyApplicationSession.cs, AssemblyFlight.cs, AssemblyDynamics.cs, AssemblyResources.cs, AssemblyDeparture.cs; Transactions/SimulationTransactionEngine.AssemblyContact.cs and assembly state-store/save guards |
| Explicit physical envelopes / stock-only admission | Assemblies/AssemblyContactProfile.cs; eight ordered envelopes, immutable stock digest |
| Retained subordinate body, power, site transport, publication acknowledgement | Contact/Staging/LocalContactWorld.Assembly.cs, .AssemblyPower.cs, .Site.cs, .Publication.cs, LocalContactSource.cs and LocalContactWorld.cs |
| Historical finite development mechanics | Assemblies/AssemblyDevelopmentPropulsion.cs and Data/SRV01-Development-Propulsion-v1.json; internal explicit free-flight qualification only |
| Authenticated finite slab | Core/Surface/FloridaSlabSupport.cs, Graphics/FloridaLaunchSite.cs, Assemblies/AssemblyFloridaSite.cs; one exact site/slab binding |
| Shared visible scenario | Triangle/StockAssemblyDevelopmentScene.cs; copied canonical state, retained original Solar instance, one native mesh7 box matching physics |
| Entry selection / Solar preservation | Launcher/ScenarioCatalog.cs -> existing LaunchCommandBuilder -> Triangle/SampleOptions.cs, Program.cs, SolarSystemScene.cs; manual and GUI share scenario construction |
| Native presentation | NovaCoreNative.cpp/.h and managed render/interop capacity definitions; one slab, no old cube/arms/caster in this route |
| Permanent tests | AssemblyContact*, AssemblyPoweredContact*, AssemblyDeparture*, AssemblyDevelopmentPropulsion*, AssemblyFlorida*, StockAssemblyPresentationTests; existing full-suite registration and launcher contracts |

## Closeout edits versus entry

Exactly three source paths changed during closeout, sealed before/after in identity.json:

1. Program.cs: route-description manualAcceptance=pending becomes passed.
2. StockAssemblyDevelopmentScene.cs: two introductory Florida descriptions record
   Project Control's support/presentation acceptance and deferred production shadows.
3. AssemblyContactProfile.cs: remove uncalled CreateDevelopment and its private bool
   switch accepting the rejected oversized contact digest. The public/internal stock
   creation path evaluates the same digest guard and performs identical work.

Repository-wide source/test/sample/tool search found no callers for the removed factory.
Stage 4 uses the separate AssemblyLaunch.CreateDevelopmentQualification free-flight path;
its definition, finite stores and tests remain unchanged. The removed factory's exact
predecessor bytes (SHA256 9377192cda38c72fc01c2931dc1eb5e844167459632c0193ff6bd2a14f1edcfa)
already exist in ../srv01-surface-to-flight-gauntlet/stage5/stock-florida-restart/prior-stage5-inputs.zip.
Historical failed oversized-contact reproduction therefore remains possible in isolation.

No permanent test edits, physical tuning, threshold changes, source-mass changes, solver
policy changes or current asset changes occurred. All 702 input identities were checked;
699 are byte-identical to closeout entry. Final builds/regressions use all three edits.
Timing reliance is limited to unchanged hot-path work, not binary-identical fresh timing.

Temporary timing/attribution observers remain evidence-only reproduction inputs. No
runtime project compiles them or loads build/evidence scratch. Legacy native facility
meshes remain required by unrelated existing consumers/tests; removing them globally
would exceed this ticket. Final Florida returns through the single-slab submission,
and disables its obsolete facility shadow caster. No hidden arms/cube collider remains.
