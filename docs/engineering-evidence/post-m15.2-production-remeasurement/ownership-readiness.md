# Current source ownership and readiness

Paths below are repository-relative, current banked source at ab14e3e. No historical
report is used to override source. Method/performance evidence is in README and
measurements.json. Line numbers are reading aids, not identity seals.

## Simulation, lifecycle and asset owners

| Responsibility | Current source / cadence / authority |
|---|---|
| Vehicle, capsule, service/tank, engine, four RCS blocks | `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDesign.cs` 10,136–262 and `Data/SRV01-FourHorn.json`: compiled immutable four definitions/seven instances, explicit poses/feeds/structure; stock digest pinned by `AssemblyFlight.cs` 101–107. No visual geometry as physical authority. |
| Main/gimbal and all sixteen physical jets | `AssemblyProfileAdmission.cs` prepares command/wrench/profile; main 600 N, jets 22.5 N each; `AssemblyFlight.cs` 45–60 compiles Boolean main + one named pair + gimbal targets. Six ±roll/pitch/yaw rows; pitch/yaw have coupled +45 N axial force, roll net force zero (`Srv01IntegrationTests.cs` 87–92). |
| Exact fuel/oxidizer | `AssemblyResources.cs` 23 onward: two canonical exact species, 2:3 use, exact exhaustion/segments, no visual consumption. |
| Mass/COM/inertia and motion | `AssemblyDesign.cs` 158 and `AssemblyDynamics.cs` 14–61: dry compiled properties + evolving mass/tensor, coupled material-origin RK4 and exact burn/coast segmentation. No gravity/contact/aero/thermal force in this owner. |
| State/command/clock/publication | `SimulationTransactionEngine.Assemblies.cs` 10,87,118,152,173: owner phase, immutable recorded commands, sequenced credit, proposal checks, fixed state/revision/debt/history writes and acknowledgement. Max four publications/service call; renderer is outside that loop. |
| Retained state | `AssemblyFlight.cs`, `SpacecraftStateStore.Assemblies.cs` 13–40: canonical assembly endpoint, material-origin pose, stores/mass/actuals; preallocated 128 history and 4,096 credit entries. Episode <=2 s; no unbounded continuation claim. |
| Existing supported powered contact | `PoweredContactPreparation.cs` 13–17,56–70 and `SimulationTransactionEngine.PoweredContact.cs` 11,104: independent qualified box/slab source, single engine/resource, BEPU retained world; zero contacts/invalid constraint topology invalidates. Not SRV-01. |
| Scene/bootstrap | `samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs` 36–61: load catalog, compile prerecorded plan, create session, prepare visual bindings before host sampling; `Start` initializes timestamp. |
| Display/presentation | Same file 63–164; copied observation supplies part transforms, gimbal, exhaust axis/realized jet bits and canonical time; terminal hold suppresses effects. `Program.cs` 237–258: bounded service then one submission/native frame. Camera remains independent. |
| Save/restore | `SimulationTransactionEngine.AssemblySave.cs` 15–79: explicit cold JSON state/plan/credit/history serialization; pinned catalog/new managed owner + exact credit and publication replay; current state/digest/debt verified. No native or BEPU object restore in this API. |
| Visual intake | `src/NovaCore.Graphics/ReusablePartVisuals.cs` 25–63,140–199: four size/hash-qualified GLBs, one rigid constant-PBR profile, no textures/skins/animation; arrays pinned once, shared meshes reused by block instances, pins released at disposal. Sockets bind presentation only. |
| Native GPU storage | `native/NovaCore.Native/NovaCoreNative.cpp` 745–774,1293,2613–2615: startup GPU mesh copies, fixed capacity54 submission, renderer-owned lifetime. Two plume accumulation targets/descriptor sets and scene-depth inputs belong to swapchain lifecycle; explicit invalidation/rebuild on resize. No per-frame mesh construction. |
| Exhaust rendering | `shaders/exhaust.frag`, `exhaust_resolve.frag` and native2141–2169: original shared proxy, nozzle-local density, 40-sample emission/extinction integration, advected procedural noise, opaque-depth clipping, weighted transparency then HDR tone-map. No atmosphere/pressure input; weighted overlap approximate. |

Readiness is proven by hash/shape/socket checks before rendering and sealed engine
authority before publication. Launch/definition identity and proposal generation
reject stale/foreign use. Material-origin transforms do not drift with changing
COM. No live BEPU body feeds the SRV-01 renderer. No duplicate asset ownership,
unbounded retained allocation or simulated-interval render multiplication was found
in this bounded path. Generic renderer allocations/resize are separate from the
private simulation-storage bound; do not equate a stable episode with a global leak proof.

## Transitions actually present

- Load → READY: catalog/session/plan/assets/bindings and native resources cold;
  zero host credit before ready/start. Native first visible frame can recreate
  swapchain; measured startup tail is retained.
- SRV-01 idle → main → gimbal → six RCS rows → final hold: exact plan, one
  canonical spacecraft/launch, two resource stores and revision/history progression.
  Runtime ends at frontier/revision128, tick2,000,000, fuel29.9521484375 kg,
  oxidizer44.92822265625 kg, mass704.88037109375 kg, COMx1.0418789203343042 m.
  Host overshoot debt differs by process and is retained.
- No-demand, concurrent main/RCS, endpoint/interior exhaustion, dry continuation:
  existing API/permanent fixture routes qualify these; not visible stock presets.
- Box supported → powered-held → exhaustion-held → live dry → final support:
  actual separate current `powered-contact` route, 1,200 publications. No SRV
  assembly identity, no contact-to-free transition or production terrain.
- Save → restore → resumed frontier65: exact managed API replay proved by the
  unchanged cost fixture; scene-native reload/first restored display frame absent.

## Exact first missing contact owner

`SpacecraftStateStore.cs` 35–42 rejects assembly slots in `TryPrepareAppliedSlot`.
`SpacecraftMotionEvaluator.cs` 19–33 uses endpoint-only typed assembly observation
and returns `AssemblyMotionRequiresTypedView` through the generic path.
`AssemblyDesign.cs` 10–11 excludes contact. The retained-contact publisher accepts
different single-craft authority and prepared geometry; this is not a missing UI
button. A supported SRV-01 must establish coherent collision representation and
contact admission/publication for its actual assembly resources and mass properties.
No mechanism is selected in this audit.

Departure is downstream. Current stock has 630 kg dry +75 kg propellant and600 N
main: maximum initial acceleration0.851064 m/s², dry0.952381 m/s². With9.81 m/s²
reference gravity, thrust/weight0.08675 wet /0.09708 dry. Retained contact transfer
alone would not make this article launch-capable; do not retune it in an audit.
Approach also needs finite-body contact acquisition and compatible environment/frame
binding. Prepared supported contact is not a landing acquisition system.

## Surface, terrain and environment

- `src/NovaCore.Core/Surface/FloridaLaunchSite.cs` 7–27,40–105 already owns physical
  terrain survey, ENU site anchoring and foundation seating. `SolarSystemScene.cs`
  1074–1133 displays the site and positions a camera. This is not spacecraft
  placement/contact-world binding; existing infrastructure should not be reinvented.
- Earth source is terrain-v5/generation4 (`PlanetaryTerrain.cs` 11,19–27;
  `PlanetaryPhysicalSurface.cs` 400–406), global `earth-surface-v5`, regional
  `earth-florida-m12` (`TerrainAssetDistribution.cs` 175–176). Stock prints
  generation3 because it uses no Earth surface. Neither fact contradicts the other.
  This audit did not rerun the Earth terrain camera campaign; historical terrain
  frame costs are not reported as current measurements.
- `PlanetaryPhysicalSurfacePointQuery.cs` 39–112 requires verified global/regional
  authority and qualifies position/normal fail-closed. It is not a generic finite-
  body SRV collider. More displacement/detail is not the missing contact owner.
- `planetary_production.frag` 266–270 separates bathymetry and unimplemented water
  displacement; analytic sea-level ocean presentation exists. No water dynamics claim.
- `SolarPlanetMaterials.cs`11 and `PlanetMaterialPresentation.cs`61–62,138–139
  retain atmosphere/cloud hook metadata. Current audited rendering path does not
  implement atmospheric scattering or a cloud renderer merely because fields exist.
- Physical aerodynamic force and thermal/reentry evolution are separate absent
  SRV dynamics responsibilities; none blocks its current vacuum demonstration.
- Planetary lighting and authored facility sun visibility exist (shader323–326;
  `SolarSystemScene.cs`804–806). No new lighting defect was demonstrated.

## Tooling/frontend

`tools/NovaCore.Launcher/MainForm.cs`88–139 provides presets, body/location/altitude,
window/resolution/diagnostics. `ScenarioCatalog.cs`9–52 includes stock, numerical
contact/powered routes, Earth/Solar and Florida camera routes. It is not a vehicle
editor, runtime input panel or save manager. `AssemblyDesign.cs`175,211–262
qualifies fixed topology, not arbitrary construction. Current two-second plan is
immutable at launch; no runtime force/config edit is supported.

Live case selection/inspection could improve iteration, and must be considered
alongside physics; its actual iteration-time savings are unmeasured. Full editor,
art, terrain polish or atmospheric bundles have no demonstrated immediate payoff
for this current production responsibility. No broader vehicle framework is proposed.
