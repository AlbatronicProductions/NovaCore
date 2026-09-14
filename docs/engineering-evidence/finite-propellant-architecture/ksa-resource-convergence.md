# Narrow KSA resource convergence

## Current installed-source evidence

Inspected the retained decompilation associated with installed `E:\Kitten Space Agency\KSA.dll`, product version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256 `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`. The binary identity and all 37 prior actuator-related source fingerprints still match [banked provenance](../spacecraft-actuator-architecture/identity.json). This verifies the local installed build, not the latest announced KSA build.

Source root: `E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA\`. Inspection was read-only and narrow; no new decompilation, asset acquisition or source copying. File names/anchors below identify responsibilities; proprietary implementation text is not retained here.

| Source anchors | Observed responsibility | NovaCore implication |
|---|---|---|
| `MoleState.cs:3–10`; `VehicleUpdateData.cs:52–103`; `VehicleUpdateState.cs:176–192` | Typed stored mass and writable worker resource state separated from source state | ADAPT staged game-owned resource authority, not float representation |
| `Combustor.cs:13–47`; `ResourceManager.cs:319–391` | Engine feed routing resolves authored connected/staged tank groups | ADAPT stable resource/feed binding; defer tank graph/mixture architecture |
| `Tank.cs:75–87`; `ResourceManager.cs:331–334,485–505`; `VehicleProperties.cs:133–149` | Multiple managers can reference shared staged storage; live withdrawal is sequential | One resource owner is justified; this is not proof of joint full-interval demand admission |
| `ResourceManager.cs:640–755`; `Combustor.cs:60–62`; `Rocket.cs:93–103` | Availability tests positive reactant presence, not enough mass for the complete next interval | NovaCore must explicitly compare exact demand against available resource |
| `ResourceManager.cs:394–505`; `Mole.cs:89–102` | Withdrawal reports completion/actual amount and bounds subtraction by availability | ADAPT no negative withdrawal; not proof of force/resource consistency |
| `Combustor.cs:65–68`; `VehicleProperties.cs:133–149`; `PhysicsBubble.cs:2008–2025` | Live path discards actual withdrawal result; constrained solve/readback precedes withdrawal | Does not establish NovaCore exact within-interval exhaustion or joint canonical publication |
| `PhysicsStates.cs:1036–1062`; `VehicleProperties.cs:99–106`; `PartTree.cs:895–909`; `Tank.cs:69–73` | Free dynamics includes mass-rate effects; spatial propellant contributes recomputed mass/COM/inertia | ADAPT resource/mass coupling; do not treat fuel as an unrelated counter |
| `EngineController.cs:44,68–86`; `Rocket.cs:129–155`; `RocketCore.cs:172–216` | Activation differs from feed availability; no feed zeros realized core output/timers without switching controller active state off | ADAPT requested Enabled versus realized NoFeed separation |
| `RocketCore.cs:184–207` | New positive command may restart available core | Not proof of automatic resume after refuel without a new command; NovaCore's ideal policy is explicit |
| `ActiveNozzle.cs:19–23` | Limiting remaining commanded burn duration affects thrust fraction | Command-duration limiting is not exact fuel depletion evidence; do not transplant averaging |
| `SequencePerformanceList.cs:545–618,664–676`; `Combustor.cs:70–99` | Forecast/Delta-V machinery aggregates drains and computes earliest mass/rate exhaustion | ADAPT aggregate-demand/segment responsibility; this is forecast code, not verified live depletion |
| `Vehicle.cs:2488–2495`; `PartTree.cs:949–951`; `ModuleStateUpdaters.cs:19–24` | Game owner applies prepared worker state | ADAPT owner application; not a proof of NovaCore revisions/fixed all-or-none commit |

Two additional narrow source fingerprints: `Tank.cs` `6E58D9E26861B21A3D4348351BAF0AF7642144DBFF35E7147236C58348CDBE28`; `MoleState.cs` `25A9F48F1D499CE4CE49502B3841F9F7A4E31269DF4A710B3FD5FFF4AA204D08`. Existing 37-file provenance is linked rather than duplicated as bulk source.

## Official history / design intent — separate evidence

Reused the previously verified, now banked [official history summary](../spacecraft-actuator-architecture/ksa-history-convergence.md). No new broad Discord campaign was performed.

- A1: ignition/shutdown distinct from throttle.
- A3, rev2905: controllers, cores, nozzles, feed/gas production and thrust separated.
- A8, rev5177: repeated command execution associated with excess propellant use; supports explicit once-only lifetime/provenance.

These records motivate responsibility separation. They do not establish exact live exhaustion or atomic resource/actuator/physical successor publication. The current installed-source findings above, retained official intent, and NovaCore's proposed architecture remain separately labelled.

## Convergence judgment

**ADOPT** separation of request, hardware realization, feed and stored resource.

**ADAPT** staged resource ownership, stable feed identity, aggregate resource admission, mass-property coupling and game-owned application under NovaCore's existing transaction owner. Keep Enabled intent separate from NoFeed realization.

**INTENTIONALLY DIFFER** in exact accepted-bit resource accounting, source-bound rational depletion time, two unaveraged body-wrench segments, integral public endpoints, no replay and joint fixed canonical publication. The numeric witnesses and NovaCore's established revision/nonmutation contracts are positive reasons for that difference. The inspected KSA live code does not prove the stronger contract; this is not a demonstrated KSA physics defect.

Do not copy tank dimensions, reaction constants, vehicle data, float arithmetic, containers or source text. One ideal scalar reservoir and a declared mass law are enough for the next nonphysical slice; pressure, species, multi-stage plumbing and vehicle-framework work remain excluded.
