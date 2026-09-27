# Accepted planning input

Captured verbatim from Production Development Work, task `01a0c466-2e38-7f40-8b9c-488520737bf7`, final turn `01a0c555-cae4-71a3-b386-def64f63afe2`. User implementation authorization supersedes inspection-only wording and earlier four-socket proposal. This is planning evidence, not qualification.

The plan is ready for Project Control review. It extends the existing construction candidate and canonical simulation owners, with one reusable path for both vehicles.

**Inspection only:** KSA writes **0**; NovaCore implementation writes **0**. No builds, tests, game sessions, asset creation, or Git mutations were performed. Calculations below are original planning calculations, not flight certification.

**1. Current repository truth**

Current HEAD is `8c189b28ce2a68f97de734d1589acb500c41fd99`. The working tree contains pre-existing modified and untracked construction work; it must be preserved and explicitly reconciled.

The banked capabilities are narrower than the proposed playable slice:

| Current capability | Established boundary |
|---|---|
| M15.3 | Canonical Florida supported-contact foundation. Rotating-Florida powered departure remains unqualified. |
| M15.4 | Active-vessel focus, follow, orbit, zoom, and celestial focus transitions. Free camera remains deferred. |
| M15.5 | Physical engine/gimbal/RCS control in its qualified stock free-flight route. |
| Construction candidate | Versioned definitions, structural/service graphs, exact resource accounting, isolated editing, serialization, and static runtime binding. Unbanked; manual acceptance pending. |
| Generic construction flight | Not implemented or qualified. |

Material source findings:

- The stock physical compiler admits a closed vehicle profile.
- The current contact profile selects stock geometry and uses a fixed support offset.
- The pilot allocator requires **16 jets**; the proposed catalog produces **32**.
- Generic construction mass properties deliberately become unavailable at partial fill.
- Current command reachability checks data connectivity without making delivered electrical power a prerequisite.
- The current editor is a browser schematic with raw identifiers/configuration fields, not the required direct construction interface.

Sources: [current state](/E:/NovaCore/docs/NOVACORE_CURRENT_STATE.md), [construction evidence](/E:/NovaCore/docs/engineering-evidence/vehicle-construction-architecture/README.md), [contact profile](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyContactProfile.cs), [pilot allocation](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyPilotAllocation.cs), [construction runtime](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionRuntime.cs).

Installed KSA remains build `2026.9.10.5438`, with DLL SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. Current directory/content inspection corroborated separately authored connectors, physical metadata, modules, and feed declarations. Earlier interaction reports and the 124-item baseline remain reference evidence; their experiments were not repeated.

**2. Current candidate reconciliation**

| Existing component | Disposition | Required treatment |
|---|---|---|
| `PartDefinitionData`, definition references, rigid poses, tensor helpers | REUSE / EXTEND | Preserve immutable identity and mathematics; add the missing Part Standard contracts. |
| `AssemblyDefinitionCatalog` | EXTEND | Versioned interface, physical, service-routing, placement, and configuration validation. |
| `ConstructionDesignData` | MIGRATE | Become the single player CraftDocument authority through explicit schema migration. |
| `CompiledConstructionDesign` | EXTEND | Retain structural validation and canonical ordering as the compiler’s design-graph stage. |
| `ConstructionEditorSession` | EXTEND | Transactions, grouped operations, undo/redo, dirty state, and typed interaction commands. |
| Existing snapping | EXTEND | Explicit compatibility and permitted indexed clocking. |
| Existing symmetry records | MIGRATE | Add authored group, socket, axis/frame, count, and transaction semantics. |
| Construction fuel/power networks | EXTEND | Explicit internal port routing, electrical command dependency, and bounded physical-runtime execution. |
| `ConstructionRuntimeBinding` | MIGRATE | Its immutable binding responsibilities enter `CompiledCraft`; static service qualification remains diagnostic evidence. |
| Construction application composition | MIGRATE | Join the existing flight application/transaction lifecycle for physical craft. |
| Browser schematic and mandatory raw JSON/IDs | RETIRE after replacement acceptance | Preserve useful diagnostic access and tests; remove its role as the permanent player editor. |
| Visual loader/upload infrastructure | EXTEND | Resolve catalog assets instead of the fixed four-stock-asset list. |
| Stock SRV admission and finite qualification episodes | PRESERVE FOR LEGACY/DIAGNOSTIC USE | Preserve accepted behavior and replay contracts while sharing canonical owners. |
| DLV fixtures and construction evidence | PRESERVE FOR LEGACY/DIAGNOSTIC USE | No conversion into an implied flight-qualified vehicle. |

This is a migration of the candidate, not a second permanent editor architecture. [Editor owner](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionEditor.cs:18), [current host](/E:/NovaCore/tools/NovaCore.ConstructionEditor/EditorHost.cs).

**3. Part Standard v1 implementation delta**

Enforce the accepted standard in executable validators:

- Definition ID, revision, schema, and content digest remain distinct.
- Asset identity, revision, hash, metres, basis, material origin, and required named nodes are mandatory.
- Dry mass, COM, full inertia tensor, collision shapes, support contacts, and clearance volumes have explicit independent provenance.
- Interfaces declare kind, family/revision, size class, mating role, rigid frame, permitted clocking, occupancy, and service capabilities.
- Stores declare resource identity, exact capacity, spatial mass law, and allowed configuration.
- Actuators declare application point, direction, thrust, exact mixture/flow, and applicable gimbal or RCS behavior.
- Definitions declare root eligibility, placement rules, supported socket groups, and configuration scope.

Retain the existing right-handed engineering basis: **+X noseward**, roll about X. Rigid transforms only.

A valid mesh cannot repair missing physics, an overlap cannot create a joint, and a structural joint cannot invent services. [Current definitions](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionDefinitions.cs).

**4. The 8× symmetry amendment**

The earlier four-socket proposal is superseded.

Both tank definitions author:

- Eight named radial sockets.
- One named socket group.
- Explicit supported placement sets for **1×, 2×, 4×, and 8×**.
- Exact rigid member frames and a declared group axis.
- Valid anchor-to-placement-set mappings.

A generic group-placement operation enumerates this authored data. It does not infer sockets from circumference, scale geometry, reflect instances, or contain a special eight-copy construction routine.

For 8× placement, all eight ghosts undergo the same validation used for commit. One failure rejects the complete operation.

**5. CraftDocument v1 contract**

CraftDocument is mutable **design authority**, implemented through committed immutable snapshots.

It contains:

- Document identity, schema, revision, and player name.
- Stable instance identities and exact definition references.
- Rigid design poses, root, and control-part selection.
- Named structural connections and explicit service choices.
- Symmetry groups and exact member/socket relationships.
- Typed initial configuration, resource quantities, and battery charge.
- Deterministic ordering.
- Reserved, versioned action metadata without an executable staging system.

It excludes runtime position, velocity, attitude history, contact handles, remaining flight inventory, active actuator state, and runtime authority capabilities.

An incomplete but structurally valid draft can be saved. Flight admission is a separate decision.

**6. Schema and migration plan**

Use distinct schema identities:

- Part Standard: `novacore.part-standard/1`.
- Extended catalog: `novacore.construction-catalog/2`.
- CraftDocument v1: `novacore.craft-document/1`.

Do not reinterpret existing `novacore.vehicle-design/1` bytes.

Migration must:

1. Validate the old document using its original reader.
2. Resolve pinned definitions and dependencies.
3. Apply a named migration with explicit mappings.
4. Preserve instance identities, quantities, relationships, and ordering.
5. Produce a separate new document.
6. Leave the source untouched.

Missing interface classes, physical laws, or symmetry socket-group provenance cannot be guessed. Such documents remain available through legacy inspection or require a specific migration recipe.

Unknown schemas, fields, changed hashes, missing modules, and unsupported migrations fail with useful diagnostics.

**7. Transaction/edit architecture**

Extend `ConstructionEditorSession` as the sole design mutation owner.

Every operation follows:

**capture revision → prepare candidate → validate → preview → commit or cancel**

Required commands cover root placement, attachment, permitted rotation, grouped placement/configuration/removal, branch reconnection, filling, and document replacement.

Rules:

- Preview does not mutate the committed document.
- Commit rejects stale revisions.
- Cancel changes no design state.
- Failure changes neither document nor undo history.
- One user operation produces one undo record.
- Undo restores the entire topology/configuration/group state.
- Redo restores the same identities.
- A new edit after undo clears the abandoned redo branch.
- History has explicit count and memory bounds; eviction is visible and deterministic.

For the player interface, migrate the current tool into one desktop editor with a native Vulkan construction viewport and ordinary catalog/inspector/save controls. Reuse existing Windows desktop patterns and rendering infrastructure. The native host/input changes are bounded editor integration work, not a new rendering engine or general UI framework.

**8. Symmetry architecture**

A persisted group records:

- Stable group ID.
- Base member and ordered membership.
- Definition reference.
- Axis and reference frame.
- Count.
- Host instance and authored socket-group identity.
- Exact target sockets and member transforms.

Each member receives its own instance ID and mutable module state. Shared immutable definitions remain shared.

For this slice:

- Group selection highlights all affected members.
- Group configuration and deletion are atomic.
- Moving a group requires another complete valid authored target set.
- Removing a parent branch removes its whole dependent group.
- Nested symmetry, inheritance, reflection, and arbitrary unlink/reconstruction workflows remain deferred.

Changing the current placement count does not rewrite existing groups. Compiler results must be independent of symmetry-record enumeration order.

**9. Six greybox definitions**

The following are **proposed original NovaCore values**, subject to Gate 4 and physical qualification. They are not taken from KSA.

| Definition | Dry mass | Geometry/function |
|---|---:|---|
| `nc.core.command-2` | 120 kg | 1.2 m outer diameter, 0.6 m axial length; eligible root; finite battery and command authority. |
| `nc.tank.short-2` | 120 kg | 1.2 m diameter, 1.5 m length; two stores; eight radial sockets. |
| `nc.tank.long-2` | 180 kg | 1.2 m diameter, 3.0 m length; same interfaces/group contract; twice the capacity. |
| `nc.mount.single-2to1` | 100 kg | NC-2 forward interface, NC-1 engine interface 0.4 m aft; integral structural support frame and four feet. |
| `nc.engine.main-1` | 100 kg | 30,720 N engine, finite bipropellant consumption, two-axis gimbal. |
| `nc.rcs.block-r1` | 8 kg | Four independently realized jets; one radial mount; explicit fuel, power, and data connections. |

NC-2 and NC-1 use the proposed 1.2 m and 0.4 m interface classes. Stack interfaces explicitly declare supported clocking; radial blocks remain keyed.

Greyboxes must expose interfaces, support feet, engine clearance, and actuator directions clearly. No polished Blender assets are required.

The adapter’s frame, beams, mount ring, legs, and feet are internal components of **one definition**, not additional spacecraft parts.

**10. Exact short craft**

Twelve instances:

- One command core.
- One short tank.
- One adapter.
- One main engine.
- Eight identical attitude blocks.

Topology:

`core → tank → adapter → engine`, with eight tank-to-block branches.

There are **11 structural joints**.

Using the core’s aft interface as document origin:

| Instance | Axial position |
|---|---:|
| Core origin | 0 m |
| Tank aft origin | −1.5 m |
| Adapter forward origin | −1.5 m |
| Engine forward origin | −1.9 m |
| Radial socket plane | −0.75 m |
| Support-foot plane | −3.3 m |

The eight radial mounting positions are authored at radius 0.6 m; block COMs lie at radius 0.75 m.

Initial inventory: **320 kg propellant A, 480 kg propellant B, 90,000 J battery charge**.

**11. Exact long craft**

The same twelve-instance arrangement substitutes only the long-tank definition.

| Instance | Axial position |
|---|---:|
| Core origin | 0 m |
| Tank aft origin | −3.0 m |
| Adapter forward origin | −3.0 m |
| Engine forward origin | −3.4 m |
| Radial socket plane | −1.5 m |
| Support-foot plane | −4.8 m |

Initial inventory: **640 kg propellant A, 960 kg propellant B, 90,000 J battery charge**.

No long-craft simulator, launch branch, control allocator, or support-offset constant is permitted.

**12. Feasibility envelope**

The analytical planning model uses:

- Main thrust: **30,720 N**.
- Effective exhaust velocity: **3,072 m/s**, retaining the existing NovaCore propulsion-law basis.
- Main consumption: **10 kg/s**, with an exact **2:3** mixture.
- Planning gravity: **9.81 m/s²**.
- No aerodynamic forces in these estimates.

| Quantity | Short | Long |
|---|---:|---:|
| Dry mass | 504 kg | 564 kg |
| Propellant capacity | 800 kg | 1,600 kg |
| Wet mass | 1,304 kg | 2,164 kg |
| Initial TWR | 2.401 | 1.447 |
| Initial net vertical acceleration | 13.748 m/s² | 4.386 m/s² |
| Nominal main-only burn | 80 s | 160 s |
| Ideal vacuum Δv, before gravity/control losses | 2,920 m/s | 4,131 m/s |

These Δv values establish resource consistency, not orbital capability.

The authored physical model gives the following COM positions measured forward from the adapter’s forward plane:

| Fill | Short COM | Long COM |
|---|---:|---:|
| Empty | 0.4147 m | 1.0940 m |
| Half | 0.5631 m | 1.3321 m |
| Full | 0.6204 m | 1.3942 m |

Full inertia tensors are diagonal for these symmetric reference builds; units are kg·m²:

| State | Short `(Ixx, Iyy, Izz)` | Long `(Ixx, Iyy, Izz)` |
|---|---|---|
| Empty | `(140.769, 640.966, 641.399)` | `(159.069, 1537.804, 1538.238)` |
| Full | `(250.769, 775.000, 775.433)` | `(379.069, 2070.695, 2071.128)` |

The slight transverse difference comes from the explicitly authored adapter frame. General rotated builds still require full tensors, including off-diagonal terms.

Storage fits geometrically:

- Short stores each occupy **0.32 m³**; long stores each occupy **0.64 m³**.
- Proposed resource-density parameters are 1,000 and 1,500 kg/m³.
- Disjoint concentric storage regions fit within a 0.5 m internal radius.
- Their occupied axial spans are approximately 0.815 m and 1.630 m respectively.
- A declared proportional spatial-depletion model supplies mass properties at every quantity. Slosh is excluded.

Control feasibility:

- Each jet supplies **15 N**.
- Opposed axial pairs supply **22.5 N·m** pitch/yaw torque.
- Eight tangential jets supply **90 N·m** roll torque with cancelling net force.
- Wet pitch/yaw acceleration is approximately **0.0290 rad/s² short** and **0.0109 rad/s² long**.
- Wet roll acceleration is approximately **0.359 rad/s² short** and **0.237 rad/s² long**.
- A 0.01 rad main-gimbal deflection supplies approximately **313 N·m short** and **551 N·m long** at full load.
- Proposed gimbal slew is 0.04 rad/s.
- Twelve simultaneous RCS jets consume **0.058594 kg/s**; a conservative 20-second control allowance consumes only **1.172 kg**.

A simple 20-second vertical burn followed by 10 seconds of coast gives:

| Analytical estimate | Short | Long |
|---|---:|---:|
| Altitude after burn | 3,011 m | 969 m |
| Vertical speed at cutoff | 315 m/s | 102 m/s |
| Altitude after 10 s coast | 5,673 m | 1,496 m |

These are reference calculations using constant gravity and neutral attitude. They show adequate manual operating time and lift margin. Gates 8–11 must replace estimates with independent numerical and integrated evidence.

**13. Launch-support owner**

Choose **adapter-owned support feet**. No new spacecraft definition or facility stand is required.

Adapter-local support points are:

`(-1.8, ±0.65, ±0.65) m`

Each has an authored contact patch, frame, structural ownership, and load limit. The tank loads the adapter frame; the frame loads its integral legs and feet.

The engine’s nominal exit is at adapter-local `X = −1.3 m`, providing **0.5 m ground clearance**. Its complete admitted gimbal envelope remains inside the central clearance region.

Planning support checks:

| Quantity | Short | Long |
|---|---:|---:|
| Wet COM height above feet | 2.420 m | 3.194 m |
| Neutral load per foot | 3.198 kN | 5.307 kN |
| Conservative COM margin at 2° tilt | 0.505 m | 0.478 m |

Use a proposed simulation admission limit of **15 kN per foot**. This is a greybox support contract, not a real structural certification.

Reuse the authenticated Florida slab geometry and authority. Extend the existing contact owner to consume compiled support/collision facts instead of identifying the stock nozzle as support.

For radial fit, use authored convex tank envelopes with faces aligned to the eight sockets. A square tank bounding box would incorrectly obstruct diagonal placements. Qualification of these convex shapes is explicit Gate 8 work.

Sources: [Florida slab](/E:/NovaCore/src/NovaCore.Core/Surface/FloridaSlabSupport.cs), [contact integration](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Assembly.cs), [mass refresh](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.AssemblyPower.cs).

**14. Electrical/control-power decision**

Choose **Option A: finite command-core battery and explicit control-power dependency**.

Proposed loads:

- Core avionics: 30 W.
- Engine controller: 10 W.
- Eight attitude-block controllers: 2 W each.
- Total enabled load: **56 W**.

A full 90 kJ battery supports approximately **1,607 seconds**, or 26.8 minutes, at that load. A 30-second acceptance flight uses 1,680 J.

Power availability requires both an electrical route and actual energy delivery. Command availability additionally requires data reachability.

At power loss:

- Main propulsion fails closed.
- New attitude actuation stops.
- Pending actuator intent cannot continue consuming propellant without power.
- Physical motion and simulation time continue.
- The player receives a clear power-loss state.
- Camera operation and game-interface inspection remain available.

Power depletion must split the physical interval at the exact event. The existing static “consume fuel, then evaluate power” sequence cannot serve as the physical dependency model.

**15. FIT / FUNCTION / ADMISSION architecture**

Expose three separate evaluations:

| State | Meaning |
|---|---|
| **FIT** | Structural attachment, compatibility, occupancy, allowed transforms, collision, and clearance are valid. Required placement-service routes exist. |
| **FUNCTION** | The configured craft has usable mixtures, powered command/data paths, and the required actuator authority. |
| **ADMITTED** | This exact compiled craft, initial state, launch site, physical model, and runtime bounds are qualified together. |

Empty stores may still FIT. Missing electrical energy may prevent FUNCTION without making the geometry invalid.

A craft may FUNCTION in principle but fail site support or numerical admission.

Each refusal identifies affected hardware and a player-understandable remedy. Raw IDs and implementation flags belong in optional diagnostics.

**16. Save/reload architecture**

Provide ordinary named design files and a player-facing save/load browser.

Requirements:

- Stable IDs, ordering, configuration, and complete symmetry metadata survive roundtrip.
- Save serializes the committed document only.
- Pending preview must be explicitly accepted or cancelled.
- Successful save establishes the clean document digest.
- Undoing back to that digest clears dirty state.
- New/load/close protects unsaved changes with Save, Discard, or Cancel.
- Write a temporary sibling file, flush, validate, then atomically replace.
- A failed save preserves the previous file.
- Load completely parses, resolves, migrates if requested, and validates before replacing the active document.
- Reload never refills stores, repairs missing parts silently, or imports runtime leftovers.

Stock examples and player designs use the same document reader and compiler.

**17. Craft Compiler**

Create one public compilation entry over the existing candidate stages:

1. Freeze a document revision.
2. Resolve exact definition and asset dependencies.
3. Validate structure, interfaces, clocking, occupancy, and symmetry.
4. Resolve explicit internal and inter-part service routes.
5. Compile stores, mixtures, electrical loads, and command requirements.
6. Compile mass laws, collision shapes, support contacts, and clearance.
7. Compile actuator identities, geometry, demand allocation, and limits.
8. Produce immutable `CompiledCraft` plus structured diagnostics.

The compiler must not create a live vessel or spend resources.

Editor checks reuse these validation responsibilities through lightweight prepared data. Full compilation, asset hashing, graph construction, and arithmetic-width analysis must not run on every pointer movement or render frame.

**18. CompiledCraft contract**

`CompiledCraft` contains immutable, dependency-sealed facts:

- Source document identity/revision/digest.
- Definition and asset dependency manifest.
- Stable instance-to-runtime-index mappings.
- Structural and service topology.
- Resource/electrical schemas and exact initial quantities.
- Physical mass-law coefficients and bounds.
- Collision and support profiles.
- Actuator geometry and prepared allocation.
- Render bindings and focus bounds.
- Supported admission-profile requirements.

It contains no mutable stores, velocities, contacts, control latches, or editor selection.

`RuntimeCraftState` owns those mutable values under the existing simulation transaction authority. Launch constructs a fresh runtime identity/generation without modifying the source document.

**19. Resource/service topology**

Extend the candidate’s current part-level connectivity to **explicit ports and internal routes**.

For this catalog:

- Core → tank: power and data.
- Tank stores → adapter → engine: separately declared A and B routes.
- Tank stores → each attitude block: separately declared A and B routes.
- Tank and adapter internal wiring explicitly carry power/data to their downstream interfaces.
- Each consumer identifies its actual required ports and resources.

Mechanical attachment supplies none of these implicitly.

Preserve exact, atomic mixture accounting and deterministic shared-store depletion. All 32 jets compete for the same declared stores through the same solver rules.

Manual fuel lines, resource-group authoring, staging-derived feed rules, and arbitrary transfers are deferred.

[Current fuel graph](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionFuel.cs), [power/data graph](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionPower.cs).

**20. Physical-property compilation**

Compile authored analytic mass regions independently from meshes and collision shapes.

For each store, define a qualified quantity-to-mass-distribution law. Evaluate:

- Total mass.
- First moment and COM.
- Full inertia about the fixed material origin and instantaneous COM.
- Bounds across every admitted fill combination.

For the proposed tanks, store centroids remain fixed and their tensors vary proportionally with stored mass. Aggregate properties are recomputed from prepared coefficients, not by rebuilding geometry.

Gate 8 must derive and test the matching variable-mass dynamics, including mass-flux assumptions and COM/reference-point transport. Simply replacing the stock point-store inertia with a finite-volume tensor is insufficient.

BEPU mass refresh must preserve the material-origin motion and canonical publication sequence. No extra velocity impulse, second physics step, or renderer-derived mass update is allowed.

**21. Generic Florida handoff**

Extend the existing launch/contact route with compiled data.

The launch transaction must prepare:

- Authenticated Florida terrain/slab authority.
- Supported pose derived from the authored feet.
- Earth rotation, site velocity, and reference frames.
- Exact authored resources and battery charge.
- Neutral controls and engine-off realization.
- Compound collision, mass, and contact preparation.
- Render bindings and active-vessel focus observation.
- One coherent runtime identity.

Publish only after complete preparation succeeds.

The craft document digest and compiled digest must remain traceable into the launched vessel. Substitution of SRV-01, DLV, or a hidden stock craft is prohibited.

The current site’s stock support offset and 20-second coverage bound require explicit extension. Continuous coverage preparation must preserve Earth/site authority without periodically respawning the vessel. [Current site owner](/E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyFloridaSite.cs).

**22. M15.5 controls binding**

Preserve M15.5’s input and admission semantics while extending its qualified inputs:

- **Z:** admit main-engine-on intent.
- **X:** admit main-engine-off intent, retaining OFF precedence.
- **WASD/QE:** admit pitch, yaw, and roll demand.
- Opposing requests cancel.
- Held input does not generate repeated admission records.
- Focus loss releases attitude demand.
- Text entry and editor interaction cannot pilot a vessel.

Replace the allocator’s fixed 16-jet mask assumption with a bounded actuator-index representation that handles the compiled 32 jets.

Prepare allocation from actual force points, directions, COM range, and inertia. Qualify signs, unwanted translation, simultaneous-axis requests, gimbal slew, resource availability, and power gating.

The existing control entry currently excludes supported contact. Extend that canonical admission path; do not introduce an alternate player control authority. [Current admission](/E:/NovaCore/src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.AssemblyControl.cs), [input adapter](/E:/NovaCore/samples/NovaCore.Triangle/PlayerFlightControlInput.cs).

**23. M15.4 camera binding**

Reuse active-vessel observation and focus/follow ownership.

At launch, bind the new vessel’s identity, canonical pose, and compiled bounds. Follow must survive support unloading and flight without changing the selected control subject.

The editor needs orbit, pan, zoom, and focus over design observations. These remain presentation state.

Preserve M15.4 celestial focus and **F** refocus behavior. Do not turn camera movement into attitude control or introduce the deferred free-flight camera as a dependency. [Active-vessel integration](/E:/NovaCore/samples/NovaCore.Triangle/SolarSystemScene.cs:601).

**24. Controlled ascent**

Qualify this sequence:

1. Supported cold vessel advances normally.
2. Z reaches canonical command admission.
3. Power and exact propellant permit realized thrust.
4. Contact reactions unload.
5. Clearance evidence permits the existing ownership transfer.
6. Free-flight dynamics continue from the published endpoint.
7. Attitude commands produce the expected compiled actuator response.

The current departure proof is restricted to a very narrow, nearly nonrotating stock case. Extend qualification for the new compiled geometry and controlled departure; do not bypass clearance or contact ownership.

The 20-second burn is an acceptance witness, not a gameplay timer. New live operation must continue beyond the current finite test episodes.

**25. Cutoff/coast**

X must stop actual main thrust and its resource debit at the admitted boundary.

After cutoff:

- Position and velocity remain continuous.
- Gravity and rotation continue.
- RCS remains available while its resources, power, and data path permit.
- Residual angular velocity persists unless counter-commanded.
- Engine visuals follow realized state.
- Time does not freeze at a fixture endpoint.

Extend the existing live command source with bounded rolling preparation and checkpointed history. Finite prerecorded episodes remain diagnostic inputs. History capacity must not become a visible automatic flight termination.

**26. Implementation owner map**

Names marked NEW are proposed types, not claims that they already exist.

| Responsibility | Intended owner | Status |
|---|---|---|
| PartDefinition | `PartDefinitionData` / construction capability records | EXTENDED |
| Definition registry/catalog | `AssemblyDefinitionCatalog` | EXTENDED |
| Interface definition | Existing attachment records plus typed interface contract | EXTENDED |
| Compatibility | Shared `PartCompatibilityEvaluator` used by editor/compiler | NEW |
| PartInstance | `PartInstanceData` | EXTENDED |
| CraftDocument | Migrated `ConstructionDesignData` | MIGRATED |
| Serialization/migration | `AssemblyJson` plus explicit document-version readers | EXTENDED |
| Transactions | `ConstructionEditorSession` | EXTENDED |
| Undo/redo | Session-owned bounded `CraftEditHistory` | NEW |
| Symmetry | Migrated symmetry records and generic group transactions | MIGRATED |
| Preview/selection | Desktop editor interaction state | NEW |
| Save library/atomic replacement | `CraftDocumentStore` | NEW |
| Craft Compiler | Orchestrator over existing construction validation/network stages | EXTENDED |
| CompiledCraft | Consolidated immutable graph/runtime-binding facts | MIGRATED |
| Service routing | Construction fuel/power/data graph compilers | EXTENDED |
| Mutable resource/power state | Existing simulation state and transaction ownership | EXTENDED |
| Physical-property law | Prepared mass-region/store-law evaluator | EXTENDED |
| Collision/support facts | Data-driven successor to stock contact-profile inputs | EXTENDED |
| Admission | Shared compiler/profile/site validation | EXTENDED |
| Florida preparation | `AssemblyFloridaSite`, slab authority, existing application composition | EXTENDED |
| Contact execution | `LocalContactWorld` / BEPU integration | EXTENDED |
| Canonical publication | `SimulationTransactionEngine` | EXISTING |
| Flight controls | Existing control authority, input adapter, allocator | EXTENDED |
| Camera | M15.4 active-vessel observation/follow | EXTENDED |
| Visual binding | `PartVisualLoader` / `ReusablePartVisuals` | EXTENDED |
| Player editor frontend | Current tool migrated to desktop/native viewport | MIGRATED |
| Mandatory browser schematic/raw forms | Removed after replacement acceptance | RETIRED |

No separate resource ledger, physics state store, contact world authority, or flight-control authority is introduced.

**27. Performance plan**

Measure every requested lifecycle separately:

| Boundary | Measurement focus |
|---|---|
| Catalog/assets | Cold parse, validation, hash, preparation, retained memory |
| Picking/preview | Warm single and 8× placement, invalid candidates, rapid pointer motion |
| Transactions | Commit, group configuration, delete/reconnect, undo/redo |
| Persistence | Serialize, atomic save, load, migration, failed load |
| Compiler | Graphs, services, physical preparation, allocation, dependency reuse |
| Launch | Site authentication, body preparation, visual upload, publication |
| Runtime | Input, admission, exact depletion, power, mass update, contact/flight, publication |
| Presentation | Editor interaction, active-vessel follow, rendering, frame pacing |

Report median, P95, P99, maximum, allocations, GC, retained storage, and repeated/consecutive tails. Use equivalent fresh fixtures and normal-runtime timing; measure zero-allocation contracts separately with the existing checked method.

Preserve existing exact-zero warmed contracts. The generic candidate currently allocates during service evolution; migrating that work requires a bounded prepared workspace and qualified arithmetic storage, not relabelling the old path allocation-free.

Use the documented standing frame targets: 8.33 ms terrain foundation, preferred 6.67–6.94 ms headroom, 11.11 ms preferred full-game target, and 16.67 ms sustained Ultra/max floor. Existing documented overruns remain visible; they are not spare budget.

Cold editor operations without an established numeric bar remain measured/report-only until Project Control sets one. No invented threshold becomes a claimed existing contract.

**28. Regression plan**

Preserve existing predicates and fixtures for:

- M15.3 support, powered contact, departure, failure, and publication.
- M15.4 active-vessel/celestial camera behavior.
- M15.5 command admission, pilot allocation, input, replay, and allocation contracts.
- SRV-01 identities, assets, physical results, and saves.
- DLV static construction/service evidence.
- Exact resource conservation and depletion boundaries.
- ReferenceFrames and Precision.
- BEPU version and dependency policy.
- Launcher behavior.
- Graphics, native input, asset lifetime, and FP64 camera-relative conversion.
- Construction graph/editor/fuel/power/runtime tests.
- Existing save/replay schemas.

New tests must exercise independent outcomes: malformed documents, conservation, physical oracles, stale authority, failed publication, and actual player routes—not merely restate implementation branches.

**29. Red-team plan**

Mandatory attacks include:

- Change definition bytes while retaining ID/revision.
- Feed an old schema to the new reader.
- Reorder symmetry records and compare compile results.
- Block exactly one of eight sockets; verify zero new instances.
- Interrupt commit or load after partial preparation.
- Undo/redo placement, configuration, deletion, and reconnection.
- Mutate one member’s runtime state and check all peers remain independent.
- Omit an internal service route while retaining the structural connection.
- Supply only one reactant.
- Retain data connectivity but remove electrical delivery.
- Exhaust battery during a powered interval.
- Change mesh bounds without changing authored physics.
- Replace the supported craft with a stock launch surrogate.
- Use renderer or nozzle geometry as support authority.
- Apply controls during text entry or celestial focus.
- Hold X/Z together, retry admissions, or submit stale generations.
- Cut off during contact transfer and during depletion.
- Exhaust history capacity during continuous manual operation.
- Require a different simulator for the long vehicle.
- Smuggle in staging, arbitrary surface mounting, scaling, or polished art.
- Present engineering automation as Project Control’s Player PASS.

Every attack has an explicit expected refusal or invariant-preserving result.

**30. Manual Player PASS route**

Project Control personally performs:

1. Open the editor and understand the catalog without internal identifiers.
2. Place the core, short tank, adapter, and engine.
3. Select the attitude block and 8× mode.
4. Inspect all eight previews.
5. Cause a blocked-member placement and see complete refusal.
6. Commit the valid group.
7. Undo all eight, then redo the same group.
8. Understand FIT, FUNCTION, and ADMITTED.
9. Fill the declared stores/battery through typed controls.
10. Save, clear/new, and reload.
11. Verify all twelve instances, eight-member symmetry, and initial configuration.
12. Compile and launch the exact authored craft at Florida.
13. Verify supported engine-off spawn and resource values.
14. Ignite, ascend, command attitude, cut off, and observe continuing coast.
15. Repeat with the long tank through the same workflow and architecture.

Engineering instrumentation supplies evidence; only Project Control supplies Player PASS.

**31. Gate-by-gate implementation gauntlet**

All gates below are future work requiring implementation authorization.

| Gate | Deliverable and promotion evidence |
|---|---|
| **0 — Scope/repository baseline** | Capture current source/worktree identities; accept this plan, owner map, scope, and performance predicates; preserve unrelated candidate work. |
| **1 — Part Standard/schema** | Executable validators reject ambiguous interfaces, missing physics, identity misuse, unsupported configuration, and inferred services. |
| **2 — CraftDocument/serialization** | Deterministic roundtrips, strict readers, explicit migrations, stable identities, and failed-load preservation. |
| **3 — Transactions/undo** | Atomic operations, stale-preview rejection, bounded history, complete undo/redo, dirty-state correctness. |
| **4 — Six greyboxes/feasibility** | Exactly six definitions; authored assets/physics/services/support; independent reproduction of both numerical envelopes and clearance checks. |
| **5 — Direct editor/symmetry** | Mesh picking, root/stack/socket placement, permitted clocking, typed configuration, 1/2/4/8 previews and atomic grouped operations. |
| **6 — Short/long persistence** | Both complete twelve-instance craft survive save/new/load with exact group and configuration identity. |
| **7 — Compiler/CompiledCraft** | One deterministic compiler, dependency sealing, explicit diagnostics, immutable facts, no runtime mutation. |
| **8 — Physical/resource/control reconciliation** | Partial-fill mass law, exact power/propellant coupling, 32-jet allocation, convex collision/support, continuous-time bounds, and numerical oracles. |
| **9 — Generic Florida admission** | Both authored craft spawn on their own feet, cold and coherent; no stock substitution; canonical ownership and rotating-site correctness. |
| **10 — Short flight** | Real ignition, unloading, departure, controlled ascent, cutoff, and continuous coast; engineering evidence retained. |
| **11 — Long flight** | Same route and owners, with only definition/configuration differences; repeat physical and player-route evidence. |
| **12 — Integrated qualification** | Regression, performance, red-team, failure recovery, and integration evidence; then Project Control’s manual route. |

After future authorization, a clean gate may promote according to the supplied campaign rules. Allow only one bounded revision at a gate. Product-policy questions or a second revision return to Project Control.

**32. Stop/escalation conditions**

Stop if:

- A seventh spacecraft definition becomes necessary.
- Either vehicle fails lift, support, clearance, or control feasibility.
- Electrical behavior or support ownership cannot remain as specified.
- Part Standard policy requires revision.
- Migration requires a second permanent editor architecture.
- Generic support requires reopening M15.3 ownership policy.
- Correct generic control cannot use the M15.5 authority.
- Numerical/resource correctness requires bypassing canonical publication.
- Staging, arbitrary surface attachment, or another excluded capability becomes mandatory.
- A gate needs more than one bounded revision.

Executable staging, decoupling, fairings, parachutes, docking, antennas, fuel-line authoring, nested symmetry, scaling, multi-vessel flight, flight-sequence editing, and polished art remain outside this campaign.

**33. Recommended Active Development model/effort**

Use **GPT-5.6 Sol at Extra High (`xhigh`)** as the responsible Active Development architecture owner. This follows the repository’s explicit assignment of high-risk architecture and authority work to Sol. Bounded, fully specified implementation tickets may use Terra after their invariants are frozen. [Repository rule](/E:/NovaCore/ENGINEERING_RULES.md:31).

Extra High is appropriate for the cross-owner reasoning and qualification required here; current official guidance associates higher effort with difficult, multistep work. [OpenAI model guidance](https://learn.chatgpt.com/docs/models).

This recommendation does not start development or change any task settings.

**34. Final judgment**

**PASS — SIX-DEFINITION FIRST PLAYABLE MODULAR-CRAFT IMPLEMENTATION PLAN READY**

This judges the completeness of the plan. Implementation, engineering qualification, integration acceptance, and Project Control’s Player PASS remain outstanding.

**STOP FOR PROJECT CONTROL.**


