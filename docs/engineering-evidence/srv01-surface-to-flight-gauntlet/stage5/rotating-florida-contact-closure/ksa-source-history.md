# Current KSA source and live history: mass, contact and physical authority

Internal engineering reference only. Direct current installation inspection and authenticated Discord history were both available. No KSA source, mesh, texture or configuration was copied into the candidate. This record paraphrases responsibility and method; it grants no rights in KSA assets.

## Installation and inspection identity

Root: `E:\Kitten Space Agency`. `KSA.dll` product version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.

| File relative to installation | SHA-256 |
|---|---|
| KSA.dll | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| Content/Core/CoreFuelTankAGameData.xml | 4C032E3E89F941BF6C3AD38997E286B7A90568292C04B00216D12D518552F242 |
| Content/Core/CoreFuelTankAAssets.xml | B3876903B0C6E793767EE3DA2E0F4B6AAFD204099EBD43C9E881FFEB6DDE3266 |
| Content/Core/CoreFuelTankBGameData.xml | 4DA7EA1E554201AC9E9D3F024DC611791CAC656B792F9894FB959465CC8FEC0F |
| Content/Core/Volatiles.xml | 6602BF06CF6DCCF9FC9D7EC3D0FEBB7F73912F294A0BCB1DBFD6D602FE5002B5 |
| Content/Core/defaultvehicles/Rocket/vehicle.xml | B94F783D3AE21056559CE49542BDFF64BA788178F2442811302BC311F648D2A3 |

Existing ILSpy 11.0.0.9375 was run with `--disable-updatecheck`, decompiling selected types to stdout only. Method tokens below identify these exact binaries, not a generic KSA release. Current bytes outrank historical intent.

## Physical storage to retained body

| Responsibility | Direct method/token | Finding |
|---|---|---|
| Authored tank physical shape | `TankGeometry.ComputeSphericalTank` / `ComputeCylindricalTank`, 06002A20/21 | Separate shell and solid interior sphere or cylinder/endcaps. Physical parameters include radius, wall thickness, length and dome shape. |
| Storage volume and tensor | `AsmbTankTemplate.GetTankPropertiesAsmb`, 06002A24 | Interior geometry determines volume and mass-specific principal moments, rotated into assembly coordinates. |
| Structural mass | `AsmbVolumetricMassTemplate`, 06001227/28 | Authored material, density, or mass is separate from stored species. |
| Species density | `SubstanceTemplate.Create`, 06002925 | Liquid/solid phase identity and storage density come from substance configuration. |
| Mixture capacity | `ReactantMix.GetLiquidMassByVolume`, 06002957; `Tank.ConfigureFor`, 06001646 | Total full mixture mass is V/sum(w_i/rho_i); species capacity volume is w_i*M/rho_i. |
| Saved quantity / fill | `Tank.ApplySaveData`, 06001649; `ComputeSubstanceVolume` / `FilledFraction`, 06001633/31; `Mole.FilledFraction`, 0600149E | Saved species masses remain distinct. Used volume is sum(m_i/rho_i); tank fill is used/storage volume. |
| Resource mass | `Mole.GetStoredMass`, 06001498; `Refill`, 0600149F; `ConsumeStored`, 0600149C | Stored phase mass is density times volume; consumption subtracts mass and exhausts at zero. |
| Tank spatial properties | `Tank.ComputeMassPropertiesPartAsmb`, 06001634 | Current total species mass multiplies fixed storage mass-specific tensor at fixed tank offset. |
| Tank to vehicle | `ComputeMassPropertiesVehicleAsmb`, 06001635; `PartTree.ComputePropellantMassPropertiesAsmb`, 06001AB0 | Rotate/translate full tank contributions, then aggregate all tanks. |
| COM / full inertia | `OffsetMassProperties.op_Addition`, 060011FD; `MassProperties.GetPropertiesAtOrigin`, 060011F6 | Weighted COM plus full parallel-axis tensor, including cross terms. |
| Dry + stored mass | `VehicleProperties.RecomputeMassProperties`, 06001B43 | Combine inert and propellant properties; invert complete tensor, invalidate principal-axis cache. |
| Drain update | `ConsumePropellantFromActiveNozzles`, 06001B46; `FullPhysicsConstrainedStep`, 06001C19 | Source properties held through bounded constrained stepping; consumption and recomputation afterward. Not a claim of recomputation every internal microstep. |
| Retained native update | `PopulateBepuStatesFromKinematic`, 06001C33; `UpdateSimFromVehicle`, 0600068A; `CopyToBepu`, 06001B40; `UpdateShape`, 0600068E | Refresh inverse mass/tensor and COM-relative compound placement on retained body at the next applicable outer cycle. |

`Tank.SetScale` (06001622) scales volume cubically and mass-specific inertia quadratically. Separate tank modules have independent placement; Tank B's C/D examples contain Tank1/Tank2 at different assembly locations. Differential drain can move aggregate COM.

The current approximation is **fixed tank centroid and normalized spatial tensor scaled by remaining resource mass**. Per-species allocated capacity volume does not establish separate spatial placement inside that tank. Fill fraction does not reshape this tensor or move its centroid. No free-surface, liquid-slug orientation, ullage shape or slosh calculation was found in this path. This is distributed inertia, not an exact partially filled fluid model.

## Physical, collision and visual authority are separate

`CoreFuelTankAGameData.xml` declares a collider and a separate tank shape. `CoreFuelTankAAssets.xml` imports a GLB mesh atlas and binds meshes/materials through visual modules. `MeshReference.Load` (0600128C) and `PartModelDynamicModule.CreateComponents` (060019F0) do not infer tank inertia from those meshes.

The examined physical tank configuration even documents a geometric radius different from its active tank radius. Therefore current KSA establishes **explicit authored physical/gameplay storage authority**, not guaranteed visual/physical fidelity or shared procedural generation. Its oversizing is not authority to choose NovaCore volume, density, capacity or inertia.

## Contact response

Current ordinary vehicle callback `ConfigureContactManifold<T>` (060006C8) uses 30 Hz, damping ratio 1, recovery cap 1.5 m/s and default friction .95 without a vehicle-mass branch. `ConstraintSim` constructor (06000684) selects 8 iterations / 1 substep. Cloth and character friction paths are distinct responsibilities. These values are evidence, not proposed NovaCore replacements.

Shipped BEPU `SpringSettingsWide.ComputeSpringiness` (0600052C) and `PenetrationLimitOneBody.Solve` (06000759) use inverse effective mass
`1/m + (r cross n)^T I^-1 (r cross n)`.
Target-frequency stiffness already scales with effective mass. There is no missing scalar-mass multiplier to add. Current source does not prove any measured heavy KSA craft meets NovaCore's 0.122 mm requirement.

## Authenticated live engineering history

Directly read the Kitten Space Agency `#live-changelog` channel in the user's authenticated Codex browser:
`https://discord.com/channels/1260011486735241329/1260112103134724146`.
This was not a NovaCore summary or unauthenticated web description. Narrow searches included `in:live-changelog inertia` (14 results), `tank volume` (4), and `tank` (112; latest page inspected, not an exhaustive history claim). `tank inertia` returned no results. Relevant messages:

| Official historical record | Meaning and limit |
|---|---|
| [3059, 2025-12-14](https://discord.com/channels/1260011486735241329/1260112103134724146/1449651012901011547) | Moved mass to parts/full tensors and distinguished assembly/body COM. Initial propellant distribution followed static mass; actual liquid placement was future work. Current tank-interior source supersedes that initial approximation. |
| [3129, 2025-12-28](https://discord.com/channels/1260011486735241329/1260112103134724146/1455018855142391870) | Tank/mass hierarchy revision and placeholder rotational-inertia changes. Not a usable NovaCore tensor. |
| [3132, 2026-01-02](https://discord.com/channels/1260011486735241329/1260112103134724146/1456807271085506780) | Principal-axis and parallel-axis correction; refill/COM consistency. |
| [3519, 2026-02-12](https://discord.com/channels/1260011486735241329/1260112103134724146/1471719744813137990) | Virtual spherical capacity increased to match base tanks; illustrates explicit game-data capacity decisions. |
| [4935, 2026-07-15](https://discord.com/channels/1260011486735241329/1260112103134724146/1526814433912029287) | Separated tank game-data file and grouped volume controls. |
| [5064, 2026-07-28](https://discord.com/channels/1260011486735241329/1260112103134724146/1531771812491558943) | Adjusted oversizing among tank families; recorded geometric sizes. Confirms declared gameplay volume can differ from geometry. |
| [5168, 2026-08-04](https://discord.com/channels/1260011486735241329/1260112103134724146/1534364647690604786) | Revolved-shape mass-property fixes. Supports deriving physical moments rather than tuning tensors. |
| [5435, 2026-09-13](https://discord.com/channels/1260011486735241329/1260112103134724146/1548902968160550994) | Scale/save-load mass and volume consistency fixed and tested. History does not substitute for this inspected DLL identity. |

[4874](https://discord.com/channels/1260011486735241329/1260112103134724146/1524639263658999972) changed rolling resistance to use inertia and avoid holding against gravity; that is not evidence to introduce damping here. No equivalent quantitative heavy-craft support witness was established from source/history.

## Prior rotating-frame gate retained

Direct source traced CCF bubble origin/frame conversion (06001B2A/67/6D), native/canonical velocity and angular transport (06001B5E/61, 06001B74/78), pure gravitation (06001B4E/4F), derivative construction (06001B8F), terrain anchors (06000651/662/694), retained constrained stepping (06001C19/02), ready/apply ownership (06001BF8/1BEA/2F31), failure (06001BF5), and release/removal (06001BEF/1C25).

KSA's current contact branch radializes the centrifugal contribution and omits Coriolis while applying a separate terrain angular-damping policy. Those are not justified NovaCore corrections. Source does not support inventing Euler terms or damping as a cure for this mass-domain failure. Direct history [4867](https://discord.com/channels/1260011486735241329/1260112103134724146/1524584112093007943) fixed CCI/CCF angular conversion; [4848](https://discord.com/channels/1260011486735241329/1260112103134724146/1523958088384643174) separated freefall from frame choice; [5429](https://discord.com/channels/1260011486735241329/1260112103134724146/1547728308756676691) fixed fictitious forces in a CCF bubble above the physics radius. None defeats the local nonrotating control.

## Convergence classification

**ADOPT:** explicitly authored storage distribution; full tensor/parallel-axis aggregation; distinct physical and visual authority; retained native body and complete property refresh.

**ADAPT:** NovaCore-owned physical profile/data, exact per-store quantities, exact events, source-mass solve, deterministic contribution order, material-origin continuity and atomic canonical successor. A fixed normalized distribution may be a bounded authored approximation, but requires explicit approval and cannot be described as a settled liquid slug.

**INTENTIONALLY DIFFER:** preserve NovaCore support outcome, solver settings, exact resource/revision/history/ownership contracts. Do not copy KSA virtual-volume balance, friction/damping, automatic refill, assets or broader vehicle/editor systems. No claim that current KSA measured heavy contact already passes this task.

Reproduction: read the listed installed XML files; invoke the existing ILSpy DLL with `--disable-updatecheck -t KSA.Tank 'E:\Kitten Space Agency\KSA.dll'` (and the other named types); inspect linked history while authenticated. Do not export proprietary source into the repository.
