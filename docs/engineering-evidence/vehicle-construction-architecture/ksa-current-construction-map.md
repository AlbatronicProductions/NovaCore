# Current installed KSA construction map

Inspected read-only: E:/Kitten Space Agency/KSA.dll, 2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9, 4936792 bytes, SHA256 A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8. KSA writes: 0. No copied source/assets retained. Types were inspected in process; retained evidence is architectural summary and method provenance only.

| Owner | Lifetime/identity/mutability | Editor/runtime/save responsibility |
|---|---|---|
| PartTemplate / ModLibrary / PartGameDataReference | Shared catalog template, mutable during asset/game-data loading, named ID | Modules, connectors and subpart references; same template instantiates repeated parts |
| Part / ModuleList | Instance retains template; fresh modules/subparts and Universe running ID | Physical module and store owner; save PartInstance refers to InstanceOf |
| SubPartTemplate / PartParent | Real Part instances with shared tree and fresh IDs, modules/transforms | Can own mass, collider, tank, gimbal; not an independent rigid body by implication. Hidden from main catalog, editable through owner SubParts panel |
| PartInstance / PartTreeData | Save-local IDs assigned by traversal; runtime global IDs not serialized | Shared stock/player DTO; includes dynamic module state as well as topology |
| Connector / Connection | Named template frames, occupancy/compatibility and endpoint relationship | Explicit persistent connection; snapping and raycast surface mounting both create topology |
| PartTree | Rooted parent/children structural tree, derived aggregates and separate fuel links | Aggregate rigid owner; split/merge transfer modules, partition links; detachable metadata only in NovaCore scope |
| VehicleEditor / VehicleEditingSpace | Mutable editing trees, ghost/selection/gizmos and temporary archetypes | Existing vehicle editing directly retains its PartTree; exit updates vehicle or constructs from root. NovaCore must isolate drafts instead |
| VehicleTemplate / DefaultVehicleSaves | Catalog stock data read through VehicleSaveData.RootPartInstance | Same PartTree.Deserialize machinery as player saves; no special stock physical engine |
| SymmetryLayerInstance / PartSymmetryInstance | Saved base/member local IDs and ordering | Radial/inherited clones through shared reconstruction; connector siblings |
| Sequenced modules / Part.Stage | Saved stage, sequence group/order and module sequence | Design authoring metadata; NovaCore must not execute staging here |
| Vehicle.Dispose / editor cleanup | Disposes modules, temporary archetypes/thumbnails and membership | NovaCore preserves stronger generation/capability retirement |

Variants are separate template IDs, game-data overlays, module choices, scaling and subpart composition. No independent variant inheritance architecture was established. Saved subparts restored by index and connector references by index are not proof of a stable named revision contract.

## Services and actual update path

Tank owns Moles (SubstancePhase, volume, mutable mass); ReactantMix defines requirements. Combustor binds authored FeedTanks and FeedConnectors. ResourceManager seeds only declared same-part containers and leaves the consumer via declared connectors. Plumbing capabilities, enabled stores, resource identity and role constrain eligibility. Bulk-fluid stage/branch restrictions differ from service fluid. FuelLink is a separately directed/enabled crossfeed edge; geometric route does not own flow. Partition keeps links whose endpoints both transfer and drops severed ones.

Default engine priority is FurtherestToNearestSameStage; thrusters NearestToFurtherest. Equal-level eligible positive stores share rate. Current live path is VehicleProperties.ConsumePropellantFromActiveNozzles -> core ConsumePropellant -> ResourceManager.MassChange, sequential reactants/cores. The planner SequencePerformanceList.RunDrainSimulation separately assembles atomic mixtures, sums concurrent rates and advances to earliest depletion. Current source does NOT prove one shared atomic planner/runtime event solver, despite the broader wording of revision 5434 history. NovaCore exact atomic accounting must not regress to the live clamp/ignored-shortfall behavior.

ElectricalCircuits builds components using Electricity capability, collecting batteries. PowerManager integrates producer/load Watts over dt to Joules, deposits production into batteries and withdraws load energy with a rotating battery cursor. No battery means no generator-to-load supply. Shortfall spends partial energy and returns failure; cursor resets. Generation precedes ordinary loads; state updater reads newly written state. Module order allocates competing loads, not an authored priority system. Ordinary Generator is driven by an engine controller; solar is a separate source. No voltage/resistance/current model established.

UpdateModules: gimbal, tank transfers, parachute, rocket, FX, generator, solar, consumer, tracker, animation, mole. Tank.UpdateTransfers is a separate tree-wide manual transfer feature and not proof of circuit-respecting pumps.

Connector capabilities are Electricity, BulkFluid, ServiceFluid, SolidMotorCase and DecouplerJoint: no command/data capability. IsControllable is override or presence of control modules, not battery/data reachability. NovaCore's explicit data graph is a required extension.

Current capability defaults allow electricity/service fluid unless disabled; bulk fluid opts in. Two endpoint capabilities intersect; nullable defaults are permissive. NovaCore's explicit independent capabilities intentionally avoid those omitted-field defaults.

Current resource-manager recreation is eager; flow topology/electrical caches are dirty/lazy. Installed split/merge/transfer already accept recomputeDerivedData. Later 5465 extends batching/lazy rebuilding. Do not merge installed and newer history.

Runtime save stores tank quantities, battery charge, consumer Active and flow rule. Caches/cursors reconstruct. NovaCore must retain deterministic relevant state, reject unresolved links and prove replay rather than copy silent skips.

Reproduce method inspection with ../srv01-production-integration/inspect-current-ksa.ps1 against the verified DLL. Representative IL hashes are in ksa-methods.json.

