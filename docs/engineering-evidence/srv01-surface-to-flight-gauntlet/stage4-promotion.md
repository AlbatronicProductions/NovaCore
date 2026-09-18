# Stage 4 promotion gate — propulsion product decision

2026-09-17. Stage 3 is [qualified](stage3/allocation-closure/README.md). Its required downstream promotion review was performed. **STOP FOR PROJECT CONTROL: no non-discretionary launch-capable configuration is established.** Stage4 implementation is not started; no rating, resource capacity, geometry or physical policy was changed. Stage5/6 remain closed.

## Current requirement and source truth

The campaign's Stage4 special gate permits thrust/TW calculations and source/history review, but explicitly stops for a materially changed engine rating, replacement/multiple engines, boosters, staging or vehicle redesign unless current product truth uniquely establishes the choice. The finite-development-resource clarification allows generous **finite**, exact, physically coherent provisionally identified stores, independently of the current Blender tank's implied capacity. It does not supply a selected engine rating, target acceleration, mission delta-v, burn duration or tank distribution. It also explicitly retains the genuine-product-decision stop.

Current registered catalog `AssemblyStockCatalog.LoadDefault` contains `novacore.stock.SRV01.G0B` and `novacore.stock.SRV01.FourHorn`; both retain the authored main-engine rating, not an approved launch profile. Source inputs are `src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-G0-B.json` and `SRV01-FourHorn.json`, plus existing exact `AssemblyResources` and `AssemblyFlight` ownership. Current internal state/handoff and admission design supply no different selected launch rating. The same canonical SRV-01 can support a future explicit profile; no duplicate spacecraft authority is justified.

At current local gravity 9.81 m/s²:

| Quantity | Current value |
|---|---:|
| Dry / initial wet mass | 630 / 705 kg |
| Initial species stores | 30 kg fuel + 45 kg oxidizer |
| Mixture fuel:oxidizer | 2:3 |
| Main thrust | 600 N |
| Total full-flow rate | 0.1953125 kg/s |
| Equivalent exhaust speed | 3072 m/s |
| Ideal continuous full-main burn | 384 s |
| Ideal no-loss delta-v | 345.5323650867926 m/s |
| Wet / dry weight | 6916.05 / 6180.3 N |
| Wet / dry T/W | 0.08675472271021753 / 0.09708266589000533 |

Vertical liftoff from rest at current wet mass requires **thrust >6916.05 N**, before choosing useful acceleration margin. More finite propellant raises mass and this threshold. That inequality is a necessary outcome, not a unique engine design. No thrust increase, reduced gravity or special contact release was used to manufacture launch.

## Fresh current KSA evidence

Actual root `E:\Kitten Space Agency`; product `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`. Fresh KSA.dll SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. Dependency identities remain those in the Stage3 report. Read-only installed source inspection, not NovaCore summaries:

| Responsibility | Current installed method evidence |
|---|---|
| Engine configuration | CombustorTemplate.CreateConfig `06002557` takes authored chamber pressure/efficiency; Create `06002558` resolves reaction ID/hash and plumbing. |
| Nozzle/thrust/flow | DeLavalNozzleTemplate.Create `06002571` derives throat/exit areas from authored parameters. ComputeMassFlowRate `06001574`, ComputePerformance `0600156E`, GetTotalThrust `06001579` derive physical flow and momentum/pressure thrust. |
| Species/feed ownership | BindFeedPoints `06001530` resolves named containers/connectors; ResourceAvailable `06002507` requires all reactants in eligible enabled tanks. |
| Capacity/density | AsmbTankTemplate.GetTankPropertiesAsmb `06002A24`, Tank.ConfigureFor `06001646`, Liquid.ComputeVolume/ComputeMass `060028EE/EF` couple authored volume, mixture and densities. This does not give NovaCore's current visual mesh final capacity authority. |
| Dry mass | AsmbVolumetricMassTemplate.GetMassFromVolume `06001227` supports authored material/density/explicit mass. |
| Depletion/shutdown | Mole.Refill `0600149F` starts from finite capacity; ConsumeStored `0600149C` stops at zero. Rocket.UpdateRockets `06001513` and RocketCore.UpdateState `06001524` refresh availability and stop unavailable propulsion. |
| Mass/COM/inertia | VehicleProperties.ConsumePropellantFromActiveNozzles `06001B46` debits then recomputes properties; `06001B43`, `06001AB0`, `06001634/35` combine dry and remaining propellant properties through part transforms. |
| Identity/application | Tank.GetSaveData/ApplySaveData `06001648/49` preserves template/species/amounts; Vehicle.UpdateFromTaskResultsUnsynchronized `06002F31` and PartTree.UpdateFromTaskResults `06001AB3` apply staged state. |

Authenticated actual [KSA live-changelog](https://discord.com/channels/1260011486735241329/1260112103134724146) was searched directly with `in:live-changelog resource` (77 results, newest-first first page inspected). Relevant direct history: revision **5434**, September13, fixes premature dry state, incomplete-reactant drain levels, own-part feeds, per-species shares and shared flight/planner draining; **4925**, July15, fixes resource managers built before saved stores; **4904/4882**, July13/10, establish explicit cross-part feed ownership; **4858**, July7, routes edits through input buffers for main-thread application. **5239**, August10, records stale native resource handles after pool return. These explain ownership/availability constraints, not a NovaCore vehicle rating.

The latest resource-search result is 5434; newer ground-clutter/presentation history inspected for Stage3 does not supply a superseding spacecraft sizing rule. Revision numbers/date are history provenance, not a claim that all later history is in the installed binary. Current installed methods are decisive.

**ADOPT:** explicitly authored physical propulsion parameters; finite species-aware feed/stores; depletion controls realized thrust; remaining resources contribute mass/COM/inertia; rendering separate from physics.

**ADAPT:** NovaCore exact resource/event arithmetic, deterministic command/debt ownership and atomic successor; one explicit provisional profile identity on the same canonical SRV-01. A simple coherent thrust/flow/exhaust model need not reproduce KSA's full thermochemistry.

**INTENTIONALLY DIFFER:** no copying KSA tuning, dimensions/assets or floating-point resource authority. Provisional finite capacity is not inferred from current visual geometry. No generic parts/staging framework is opened.

## Decision and next input

Launch-capable propulsion is the direct downstream blocker. KSA supplies the mechanism, while the desired launch acceleration/TW and finite mission budget remain authored product inputs. Feasible architectural classes include a provisional higher-rated single main engine, multiple propulsion units or a different vehicle architecture; selecting among them is explicitly reserved by the campaign.

Project Control must select or explicitly delegate the launch-capable propulsion profile before implementation. Existing finite 30/45 kg stores may be retained if suitable for that chosen development objective; a separate mission-budget redesign is not inherently mandatory. If a larger mission budget is requested, derive and identify its finite profile explicitly. Stage3's millimetre-scale separation fixture grants no Earth-liftoff or player-visible-launch acceptance.

**STOP FOR PROJECT CONTROL.** Stage4 promotion analysis complete; implementation withheld. No Florida, next milestone, bank or push.
