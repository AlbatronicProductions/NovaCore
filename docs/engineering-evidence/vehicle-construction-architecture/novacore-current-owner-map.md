# Current NovaCore owners

Paths below are relative to the repository. The existing assembly concepts must evolve; a new name beside an unrelated implementation would not establish convergence.

| Responsibility | Current owner | Evolution boundary |
|---|---|---|
| Definition, version, instance, transforms, attachments | src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDesign.cs: PartDefinitionData, DefinitionReference, PartInstanceData, AttachmentData | Reuse/evolve these contracts; preserve legacy canonical bytes |
| Saved design, stock catalog | DesignData, CatalogDesignData, AssemblyStockCatalog in AssemblyFlight.cs; embedded SRV assets | One generic design/compiler path; closed SRV physical profile remains an admitted projection |
| Physical compile | CompiledAssemblyDesign / AssemblyProfileAdmission | Current four-definition/seven-instance, fixed-role/feed law is an explicit SRV qualification boundary; do not relax it for DLV |
| Resource identity/capacity | StoreData, PropulsionData | General resource definitions and explicit store/consumer bindings; no mesh authority |
| Exact quantity and depletion | Spacecraft/Resources/PropellantInteger.cs, FinitePropellantSegmentation.cs; AssemblyResources.cs | Existing fixed 2:3 helper is not arbitrary multi-store network arithmetic; preserve exact accounting and transaction ownership |
| Main/RCS/gimbal | AssemblyActuation, AssemblyPilotAllocation, AssemblyDevelopmentPropulsion | Per-instance physical actuators; no DLV dynamics admission |
| Mass, COM, inertia | Matrix3, AssemblyMass, CompiledAssemblyDesign, AssemblyLaunch | Reuse tensor/frame arithmetic; bind accepted constituent definitions. Current SRV resources are a qualified point-store law, not a general tank geometry law |
| Canonical state/publication | SpacecraftStateStore.Assemblies.cs; SimulationState; SimulationTransactionEngine assembly partials | All dynamic mutations remain in this authority, never EditorSession or an independent runtime Step loop |
| Save/restore | SimulationTransactionEngine.AssemblySave.cs; AssemblyApplicationSession | Immutable design separate from dynamic snapshots; stale capability retirement retained |
| Control identity | AssemblyControlAuthority / AssemblyControlIdentity | Vessel plus generation/capability; camera focus does not grant control |
| Camera/presentation | Core scene-object focus and samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs; ActiveVesselCameraTests | Derived read-only focus observation; accepted M15.4/M15.5 stays unchanged |
| Asset loading | Graphics/ReusablePartVisuals.cs, current hash-qualified stock asset paths | Asset revision/hash, units/basis/origin and named nodes verified at load, never per frame |
| Launcher/stock registration | tools/NovaCore.Launcher/ScenarioCatalog.cs and Triangle stock scene | Preserve pre-existing edits; generic construction route must not encode DLV runtime behavior |

No current general EditorSession, configurable service graph or electrical network was found. Current FeedEdgeData is an explicit bounded feed relation, not a general resource-flow solver. Functional subpart ownership needs an extension; do not turn every mesh node into a part.

Compatibility seam: share construction identities and immutable facts; keep the banked SRV admission, transaction dynamics and serialization unchanged. A generic static construction consumer may bind new definition facts in the existing simulation owner without admitting launch physics. This seam carries explicit SRV migration debt, not authority for two permanent vehicle engines.

