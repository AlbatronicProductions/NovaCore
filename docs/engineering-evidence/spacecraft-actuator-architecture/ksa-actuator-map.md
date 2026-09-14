# Current KSA actuator responsibilities

CURRENT SOURCE EVIDENCE. Installed `E:\Kitten Space Agency\KSA.dll` is `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256 `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`. It matches the retained decompilation provenance. All 20 non-null KSA source hashes in the previous control assessment matched again. Two independent read-only investigators inspected the narrower actuator paths afresh.

References below resolve under `E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA\`. The cached source was not regenerated. This is evidence about the installed build, not the latest announced revision. Only responsibility summaries and hashes are retained; no proprietary implementation is copied.

## Engine, core and nozzle

| Source / location | Observed responsibility |
|---|---|
| `FlightComputer.cs:327-443`, ComputeControl/CommandEngineThrottles | Measure environment/mass/kinematics, derive available authority, issue engine throttle/burn-duration commands, then TVC/RCS. Engine-on, throttle and active/available controller are distinct |
| `EngineController.cs:36-46,68-86`; `EngineControllerState.cs:5-43` | Ordered cores, activation, minimum throttle and timestamped command state; activation does not itself calculate thrust |
| `Rocket.cs:74-186`, UpdateRockets | Refresh feed availability; thruster and engine commands update cores; core conditions plus gimbal update nozzles; nozzle flow aggregates back to core |
| `RocketCore.cs:172-216`; `RocketCoreState.cs:7-49` | Command timestamp freshness, remaining burn/minimum-pulse timers, bounded actual throttle, available/unavailable and activation/deactivation observations |
| `Combustor.cs:50-67`; `CombustorConfig.cs:15-36` | Authored reaction/pressure behavior from realized throttle; resource manager supplies feed availability and consumption |
| `RocketNozzle.cs:217-250`; `NozzlePerformance.cs:31-67` | Gas conditions and ambient pressure produce momentum/pressure thrust; realized gimbal transforms application point/direction separately from FX transforms |
| `VehicleUpdateState.cs:371-423` | Update gimbals before rockets; emit active-nozzle force, moment about measured COM, flow and remaining burn duration |
| `PhysicsStates.cs:891-903`, ComputeDerivatives | Downstream accumulation of admitted nozzle force/torque and signed flow, scaled by burn-duration fraction |

There is no universal physical Off/Starting/Running/ShuttingDown/Failed enum established by this path. Common liquid-core behavior assigns accepted throttle directly, preserves a minimum pulse and stops on expired duration or unavailable feed. This is not a spool ODE. A subsequent newer positive command may restart available hardware; a finite ignition-resource/failure-recovery system was not established.

`SolidMotor.cs:166-225` is a counterexample to universal shutdown: it remains burning after a zero request while its grain/pressure conditions permit. Do not infer that NovaCore's first restartable engine represents solids. Conversely, names like StartupTime/ShutdownTime in `RocketActiveTimeRecord` feed `RocketNozzleFxState.LightAnimation` and `Vehicle.cs:5795-5835`; they are not evidence of physical throttle ramping.

Unavailable engines receive zero hardware command. Automatic burn may record ignition denial (`FlightComputer.cs:401-409`). This is requested/realized separation, not proof every failure is latched or every refusal has NovaCore's atomic semantics.

## Controller, TVC and gimbal

| Source / location | Observed responsibility |
|---|---|
| `FlightComputer.cs:904-953,1104-1208` | Typed target/frame conversion and control-axis attitude/rate errors; direct input can replace assisted axes |
| `FlightComputer.cs:955-1078` | Actual nozzle thrust, mount lever arms, gimbal ranges and inertia determine current TVC authority; maximum capability is separate. Some variables named torque authority contain angular-acceleration authority after inertia scaling |
| `FlightComputer.cs:445-505` | Choose TVC/RCS/none per rotational axis, considering available hardware |
| `FlightComputer.cs:592-753,1652-1760` | Bounded normalized TVC feedback/feedforward and geometry-based gimbal requests; small scalar/2x2 Riccati gain work. Not a joint generic wrench optimizer |
| `GimbalReference.cs:8-57`; `GimbalController.cs:20-64`; `GimbalControllerState.cs:7-29` | Authored pivot/two angular ranges and controller requests are separate from actual hardware angles; actual nozzle output feeds controller authority |
| `Gimbal.cs:48-115`; `GimbalAxis.cs:11-14`; `GimbalState.cs:7-27` | Convert normalized request to bounded angles, directly assign realized angles/orientation. No rate limit or dt integration in this inspected path |

No PID integral accumulator or anti-windup was found in the inspected attitude path. RCS hysteresis/pulse state does persist; it is not interchangeable with an integral accumulator. The decompiled optional circle expression in `Gimbal.GetCommand:97-104` is suspiciously asymmetric. It was not validated as a cone model and is not recommended for copying; it does not undermine the separately observed command/hardware ownership.

## RCS and allocation

`ThrusterController.cs:73-316` derives force and moment contributions from nozzle geometry, COM and feed availability; it creates manual or geometry-derived directional maps, authority fractions and minimum impulses. `Rocket.cs:190-245`, `ThrusterAuthority.cs:26-64` and `ThrusterControllerGlobalState.cs:27-78` cache authority using physical/frame tolerances. Those thresholds and cache invalidation rules are KSA policy, not NovaCore constants or exact identity contracts.

`FlightComputer.cs:519-590` handles enabled state, opposing direct flags and direct/pulse modes. Assisted rotation may replace direct rotation while translation remains active. `1241-1264` forms manual impulses; `1284-1414` forms assisted angular impulses with inertia, current authority and hysteresis. Persistent state appears at `54-64,145-180,209-211,247-249`.

`SelectJetsToFire:1210-1239` walks available controllers, combines rotational and translational pulse-duration contributions using their maximum, and can supply a direct continuous demand. `ThrusterControllerState.cs:49-57` converts this to full-throttle pulse commands; the common core/nozzle path realizes them. This is a bounded heuristic allocation policy, not evidence that arbitrary six-dimensional demand is exactly or optimally realized. Availability, saturation, achieved output and residual must remain distinguishable.

## Propellant and physical application

`VehicleUpdateData.cs:52-103` prepares typed worker module/substance state. `VehicleUpdateState.cs:176-192` provides writable worker property/resource state. `VehicleProperties.cs:133-149` drains active nozzles over their admitted duration; `99-106` recomputes mass/COM/inertia from inert and spatial propellant properties. `PhysicsStates.IntegrateVelocityVerlet:993-1062` uses mass-rate effects and recomputation in free dynamics. `PhysicsBubble.cs:2008-2025` drains propellant after constrained simulation/readback.

**Exact live fuel-exhaustion handling is not established.** `ActiveNozzle.ComputeThrustMod:19-23` limits remaining commanded burn duration; this is not proof of exact shared-feed exhaustion. `Combustor.ConsumePropellant:65-68` discards the resource manager's returned result/actual quantity; `ResourceManager.cs:394-405,423-482` can report incomplete withdrawal and `Mole.ConsumeStored:89-102` caps at available mass. This is a limit on our inference, not a demonstrated KSA physics defect. `TryPrepareDrain`/`TryAccumulateDrain` callers at `SequencePerformanceList.cs:433,668` are forecast machinery, not the live exhaustion proof.

Consequently, use KSA's resource/hardware separation as architecture evidence. Do not claim it already satisfies NovaCore's required exact exhaustion and all-or-none actuator/resource/physical publication.

## Cadence, application and observations

`PhysicsBubble.cs:1174-1217,1547-1563` schedules control in simulation time with configured frequency and wakeups. It is not simply one controller step per display frame. `VehicleUpdateState.cs:371-392` advances module state in the simulation-step context.

`Vehicle.cs:2399-2409,2488-2495` applies physical/property measurements, copies flight-computer state and applies module results through `PartTree.cs:949-951` and `ModuleStateUpdaters.cs:19-24`. That is game-owned staged application. It does not prove NovaCore-style revisions, stale-source rechecks or atomic refusal/terminal handling.

`RocketCoreState.cs:58-78` exposes actual throttle/flow/conditions; `RocketNozzleState.cs:11-31,52-72,92-106` exposes realized performance and accumulated duty/activity. Separate FX consumption occurs at `81-89` and `Vehicle.cs:5795-5835`. Requested throttle is not a substitute for emitted thrust or pulse activity. Live updater/core references are implementation capabilities, unsuitable for NovaCore's copied canonical observations.

The same hardware output feeds constrained or free dynamics after realization. This supports regime-independent hardware and a single downstream consumer, not a claim that unrestricted free-flight attitude control is stable while supported.

Expected responsibility complexity is bounded engine/core/nozzle/thruster iteration plus fixed small control work. No performance or managed-allocation benchmark was run; KSA pooled buffers/dynamic capacity do not prove NovaCore's zero-allocation requirement.
