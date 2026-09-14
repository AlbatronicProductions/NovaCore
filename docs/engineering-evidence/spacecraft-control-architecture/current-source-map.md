# Current-source responsibility map

Inspected 2026-09-13. References are locations in the inspected source, not copied implementations. NovaCore root: `E:/NovaCore`. KSA reference root: `E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA`. The installed `E:/Kitten Space Agency/KSA.dll` SHA-256 is `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`, version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`.

The current installed hash and cached AssemblyInfo/VehicleUpdateState/VehicleUpdateData/PhysicsBubble/ConstraintSim hashes match provenance retained at `docs/engineering-evidence/certified-root-contact-kinematics/identity.json:77–89`. Other inspected-file hashes freshly identify the available cached files. This is retained decompilation provenance with current installed-identity verification, not a newly regenerated decompilation. No claim that local build5402 contains later official changes.

## KSA end-to-end graph

Program.PrepareFrame -> GLFW/Program input routing -> Input action mapping -> Vehicle.OnKey / gauge input -> InputEvents buffers -> next main preparation drain -> Vehicle.ProcessInput / SetEnum -> vehicle latches -> Vehicle.PrepareWorker -> copied worker input/FlightComputer -> ComputeControl -> controller states -> Rocket/Gimbal modules -> core/nozzle physical realization -> active nozzle force/moment -> constrained BEPU or unconstrained integration -> worker result -> main vehicle/module/FlightComputer observations.

| Path / member / lines under KSA root | Owner, side and caller -> callee | Reads -> writes; persistence and cadence |
|---|---|---|
| `Program.cs:792–793,1716–1728,1945–1950`, callback registration / OnKey | Main input; GLFW -> UI/editor/camera routing -> selected vehicle | Active window/UI/control selection -> routed action; not a solver callback |
| `Program.cs:2101–2146`, PrepareFrame | Main lifecycle; prior worker apply -> InputEvents -> worker dispatch -> PollEvents | Completed simulation -> live vehicle; buffered input drained at 2113; next jobs dispatched 2141–2144; polling 2146 normally affects a later drain |
| `Input.cs:97–195,274–289,324–326`; `InputAction.cs:3–54` | Input mapping; key/modifiers -> semantic actions | Current rebindable settings -> action; exact versus containing modifiers differ; actual defaults outrank stale help text |
| `KeyBindingValue.cs:9–45`; `KeyAssignmentPopup.cs:64–73`; `GameSettings.cs:2755–2804,3153–3188` | Settings/UI | One primary key plus modifiers; rebind on release; defaults overridden from TOML, changed bindings saved; not canonical vehicle state |
| `Controller.cs:96–115`; `OrbitController.cs:504–508`; `FlyController.cs:699–711` | Camera/UI gamepad polling | Gamepad 0, per-axis deadzone/rescale -> camera motion using player dt; no traced spacecraft-flight gamepad handoff |
| `Vehicle.cs:3194–3235,3238–3497`, helpers / OnKey | Main vehicle input admission | Press/release and action-specific lockout -> queued semantic events; key repeat does not add controls |
| `ControlsLockout.cs:39–145`; `Vehicle.cs:588` | Input policy | Control-module availability, warp and auto-burn -> action admissibility; no spacecraft contact predicate; not universally revalidated by later ProcessInput |
| `InputEvents.cs:717–730`, VehicleInputData | Buffered main input -> Vehicle.ProcessInput | Vehicle reference/action/key edge/modifiers; no exact time, global sequence or lease stored |
| `InputEvents.cs:892–942,1061–1085`, TypedBuffer / ApplyInputEvents | Main drain | Reused array grows by doubling; insertion order within category, category order across types; speed before vehicle before flight-computer input |
| `Vehicle.cs:238–240,3500–3568`, ProcessInput | Persistent main vehicle state | Events -> EngineOn latch, throttle-adjustment flags, held rotation/translation flags; edges may collapse into final state before worker snapshot |
| `GaugeButtonFlightComputer.cs:130–142`; `Vehicle.cs:6130–6142,6179–6194,6324–6342` | UI -> buffered enum/action -> vehicle/FlightComputer | Mode/target/engine requests; UI engine disable checks are not identical to keyboard warp guards |
| `Vehicle.cs:3582–3591,3612–3628`; `FlightComputer.cs:1604–1607,1619–1622,1855–1857,1873–1875` | Vehicle mode handling | Stabilization enables rate hold, not automatic capture-hold; target/direct-pulse/RCS mode persists |
| `Vehicle.cs:2360–2386`, PrepareWorker; `Universe.cs:1835–1847` | Main vehicle -> worker prepare | Persistent flags -> throttle ramp using player dt then copied manual snapshot; Auto burn clears manual engine latch; one preparation per dispatched frame |
| `VehicleUpdateState.cs:296–305`; `VehicleUpdateData.cs:84–103` | Worker state ownership | Copies kinematics/manual input/control selection and FlightComputer; snapshot persists across internal substeps |
| `PhysicsBubble.cs:473–488,1062–1087,1546–1560` | Worker controller scheduling | Resets FC wakeup at each frame; initial control evaluation before motion branch or in pre-step |
| `PhysicsBubble.cs:1175–1214`; `GameSettings.cs:1015` | Worker adaptive scheduling | Additional within-frame control wakeups obey requested/minimum intervals (default additional FC rate 10); not a globally fixed controller frequency |
| `FlightComputer.cs:341–417`, ComputeControl | Worker control/assist | Navigation/manual/modes/availability/mass -> engine duration/throttle, per-axis TVC/RCS choice, jets and gimbal demands; clears output on evaluation |
| `FlightComputer.cs:420–442`, CommandEngineThrottles | Worker engine command allocation | Shared manual/automatic demand -> active propellant-available controllers' throttle/timestamp/duration; zero otherwise; controller may own multiple cores |
| `FlightComputer.cs:445–504,519–589,1210–1238` | Worker RCS/authority selection | Available TVC/RCS, enable, flags and errors -> per-axis selection and jet pulses; opposing flags cancel; direct versus pulse differs |
| `FlightComputer.cs:592–752,904–942,1104–1147` | Worker TVC/attitude control | Manual axes or typed target/rate error -> gimbal demands; manual may override selected axes; physical torques remain downstream |
| `FlightComputer.cs:767–830,873–885,955–974` | Worker auto-burn / authority | Realized thrust/mass/g-load and settling estimates -> automatic burn throttle; not a universal manual throttle cap |
| `VehicleUpdateState.cs:371–392`; `PhysicsBubble.cs:1399–1418` | Per physics substep | Runs gimbals then rockets after control scheduling, before physical integration |
| `Gimbal.cs:91–112`; `GimbalAxis.cs:11–13` | Physical actuator module | Normalized command/configured angle limit -> realized gimbal angle; no slew-rate model found in traced path |
| `Rocket.cs:74–186`, UpdateRockets | Propulsion module | Controller demands, propellant availability and prior state -> core/nozzle state and firing summary; cached/idle paths exist |
| `RocketCore.cs:172–216`, UpdateState | Generic/liquid core realization | New command timestamp/availability/prior timer -> throttle and required minimum burn/pulse duration; no generic physical spool model found |
| `SolidMotor.cs:195–225`, UpdateState | Solid physical model | Prior burning/propellant/pressure -> continued burn independent of later zero request; shutdown is not universal |
| `RocketNozzle.cs:217–250`, UpdateState | Nozzle physical model | Core gases/pressure/ambient/gimbal -> realized thrust, mass flow, direction, duration |
| `VehicleUpdateState.cs:396–420`, UpdateActiveNozzles | Worker force preparation | Realized nozzle thrust/direction and COM offset -> force, moment, mass flow, active duration |
| `PhysicsStates.cs:891–903`; `ActiveNozzle.cs:19–23` | Dynamics derivatives | Nozzle force/moment/active fractional interval -> disturbances and mass loss |
| `PhysicsBubble.cs:1953–2035`; `PoseIntegratorCallbacks.cs:48–72,164–168` | Constrained dynamics owner | Disturbances -> BEPU velocity integration plus constraints/contact; internal calls at most 1/60 s |
| `PhysicsBubble.cs:2148–2169`; `PhysicsStates.cs:993–1065` | Unconstrained dynamics owner | Same propulsion/disturbances -> FP64 Velocity Verlet state and changing mass properties |
| `PhysicsBubble.cs:2179–2185`; `FlightComputer.cs:327–338` | Simulation feedback | Integrated measurements -> control mass/COM/inertia, pressure, acceleration and burn delta-v |
| `VehicleUpdateTask.cs:141–201`; `PhysicsBubble.cs:664–698`; `Vehicle.cs:2399–2495` | Worker staging -> main result apply | Physical state/environment/measurements/FlightComputer/module results applied after work; manual input stays vehicle-owned |
| `RocketNozzleState.cs:52–65,92–104`; `Vehicle.cs:5525–5534,5920–5946` | Simulation-derived presentation | Realized nozzle fractions/duty cycle and performance -> exhaust/audio/UI; averages are frame observations, not command history |

### Devices and lifetimes: current source, not NovaCore recommendations

Default keyboard uses W/S pitch, A/D yaw, Q/E roll; H/N, J/L, K/I translation; Z/X ignition/shutdown; arrow Up/Down throttle; T stabilization, F direct/pulse, R RCS. No default bindings/constants were copied into NovaCore. `Vehicle.cs:3184` help text differs on throttle keys; mapping source wins. Mouse controls camera/selection/maneuver editing and gauge buttons. Gamepad support exists in camera/UI; a spacecraft-flight gamepad route was not established by the complete cached-source callers found for GetGamepadState/TryGetGamepadAxes. That bounded absence is not a claim about other installed assemblies or future KSA versions.

`ManualControlInputs.cs:5–9` defaults engine off and throttle 1. Vehicle save/load preserves engine latch/throttle (`Vehicle.cs:1030–1036,1125–1130`); load clears held thruster flags. FlightComputer copy/save preserves modes, target/rates and deadbands (`FlightComputer.cs:145–180,229–251`; `FlightComputerData.cs:7–62`). This is persistence, not a per-command replay log.

`Vehicle.ClearHeldPlayerInput:5864–5870` clears held flags but preserves ignition/throttle position/assist. It is invoked on control switch, focus loss and inactive/UI-captured/>30x worker preparation. Already buffered events retain a vehicle reference; cancellation of every old queued event is not established. Keyboard allows shutdown/throttle-down past the warp admission guard, but worker preparation clears held adjustment flags above 30x, so history's throttle-down permission is not proof of continued held ramp there. GUI engine actions have different guards.

KSA manual assisted angular-rate and pulse demand scale inversely with sqrt(max(1,warp)) (`FlightComputerManualThrustModeEx.cs:9–23`; `FlightComputer.cs:1241–1263`). Minimum physical pulse still applies. Automatic control can continue through worker evaluations; not all control is disabled at high warp.

### Contact and physics boundary

`PhysicsBubble.cs:1062–1143,1221–1244` evaluates spacecraft control before branch selection; commanded/active actuators take vehicles off rails. `ConstraintSim.cs:373–392,407–420` considers activity before returning a sleeping constrained body to rails. Normal spacecraft input/FlightComputer has no landed prerequisite. Engines may act while constrained through the same nozzle force model. Only kitten locomotion masks thrusters outside MMU (`PhysicsBubble.cs:1586–1593`); it is not the spacecraft rule.

No contact-loss command reset was found in this path. Common ownership does not prove pad-assist stability or correct liftoff in NovaCore. KSA mixes FP64 navigation/kinematics/disturbances and float actuator/nozzle/mass/BEPU data; do not describe it as universally FP64. Core/nozzle/controller separation is supported, generic spool or gimbal slew is not. No allocation or performance qualification of KSA was performed.

## NovaCore current-state classification

Locations below are repository-relative. Source and permanent tests were inspected only; earlier reported measurements remain attributed to their retained reports.

| Piece / location | Classification | Current owner/reads/writes/cadence and future disposition |
|---|---|---|
| `native/NovaCore.Native/NovaCoreNative.cpp:2386–2417`; `src/NovaCore.Interop/NativeRuntime.cs:333` | MIGRATE control-facing transport; KEEP presentation input | Native display callback polls movement/key edges/mouse and emits NativeInputState. No propulsion/engine/RCS command schema. Future action producer must not become canonical authority |
| `samples/NovaCore.Triangle/DebugCameraInput.cs`; `Program.cs:222–229` | UNRELATED to physical command authority | Input -> camera movement/look; retain camera behavior. Shared keys do not make camera vectors accepted torque axes |
| `samples/NovaCore.Triangle/ContactDevelopmentScene.cs:87`; contact fixture input in Program | KEEP | Integer host sampling -> authorized contact credit/service -> copied committed pose; Space starts prepared episode, not engine ignition |
| `samples/NovaCore.Triangle/CelestialAnalyticalScene.cs:33–36,58–68` | MIGRATE | Scene owns SAS mode, target, last torque and 50,000-tick 20 Hz cadence; not a simulation-owned command store |
| Same `:179–203,206–260,285–305` | MIGRATE production consumption; KEEP qualified prototype behavior until replacement | Host credit then SAS debt service then mode/manual torque. Nonzero manual input disables SAS before servicing; cap 2048 and >10x suspension are sample policy. Do not label its existing exact SAS cadence FPS-based |
| Same `:311–406,408–415` | MIGRATE | Pure SAS result -> quantization/direct torque transaction; manual requested torque from movement flags; scene mutates mode and rate/pause. No engine/RCS realization |
| `src/NovaCore.Simulation/Spacecraft/Guidance/SpacecraftSasController.cs:18–35,48–65` and target/reference math | REUSE | Pure canonical orientation/rate target evaluation -> bounded PD requested torque. Owns neither persistent intent nor actuator hardware; fixture target carrier must not be assumed to describe every independently translating vehicle |
| `Spacecraft/Rotation/SpacecraftTorqueCommand.cs:6–10`; `.../Transactions/RigidBodyTorqueTransactionEvaluator.cs:22–31` | REUSE mathematics; MIGRATE input ownership | Immutable body-torque request/time; evaluates prior rotation at boundary and prepares replacement. A torque API is not a physical thruster |
| `Transactions/SimulationTransactionEngine.cs:249–261,332–339` | REUSE within exact admitted scope | Direct torque commits at current time with revision/history checks. No canonical pending-event ordering check in this direct path; new owner must supply one |
| `Timeline/SimulationEventPayload.cs:32–33`; `SimulationEventEvaluator.cs:44–51`; torque evaluator `:10–18` | KEEP specialized contract | Existing torque event carries spacecraft ID, advances rotation and installs zero torque; not a general queued player torque payload |
| `Spacecraft/Translation/SpacecraftTranslationState.cs:35`; `SpacecraftForceTransaction.cs:17–35`; engine `:344–370` | REUSE | Exact-time event-backed root-force replacement preserves boundary position/velocity and reevaluates on commit; scoped to its free-flight segment model |
| `Clock/SimulationClock.cs:36–113` | KEEP / REUSE | Exact time/rate/remainder/debt, pure PrepareHostAdvance and fixed install, checked horizon. Host duration credits work rather than directly defining pose |
| `Clock/SimulationClock.cs:116–157`; `Transactions/SimulationExecutionOrchestrator.cs:59–111` | KEEP explicit-time semantics; add future gameplay policy | Pause stops new host funding but explicit advancement and generic debt servicing may continue; do not infer global pause freeze |
| `Timeline/SimulationEventHeaderComparer.cs:4–21`; `SimulationTimeline.cs:43–80` | KEEP / REUSE | Exact time/priority/sequence/event-ID order; scheduling/consuming changes timeline revision. Future ingress must respect event authority |
| `Transactions/ContinuationPublicationPhase.cs:9–37`; `SimulationTransactionEngine.cs:68–110` | KEEP / REUSE | Owner-thread/exclusive publication and bounded canonical event execution; neither is already a player-control ownership model |
| `Spacecraft/SpacecraftStateView.cs:8–32`; `Translation/SpacecraftMotionEvaluator.cs:9–36` | KEEP / REUSE | Revision-checked borrowed views, coherent copied paired FP64 motion. Controller/presentation may not keep stale views |
| `Spacecraft/Contact/Staging/LocalContactSource.cs:133,157–158,182–189`; `LocalContactWorld.cs:112,124–141`; `LocalContactCallbacks.cs:49–65` | KEEP; dynamic actuation missing | Captures constant force, refuses nonzero torque, keeps original exact lattice, integrates captured local acceleration. No admitted changing actuator wrench |
| `Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs:14–36,61–78,118–135` | KEEP | Immutable episode/configuration/timeline; mutable expected paired state/clock/revision acknowledged only by allowed owner operations. Ordinary force/torque/revision changes correctly stale it |
| `Transactions/SimulationTransactionEngine.PersistentContact.cs:56–116` | KEEP | Prepare/recheck paired endpoint -> fixed canonical writes -> private acknowledgement. One pending stage and terminal committed/private-invalidated result |
| `Transactions/SimulationTransactionEngine.ContactServicing.cs:18–65,73–108` | KEEP / REUSE | Owner-authorized sequenced host credit and separate existing-debt service; four-interval cap; unpaused 1:1 only; immutable source/frame/world |
| `Spacecraft/Contact/Staging/EngineeringContactArticle.cs:21–39,52–85` | KEEP / REUSE | Accepted immutable physical dimensions, COM/inertia and identity; does not define engine/RCS hardware |
| M14.17 certified continuation publisher and evidence | KEEP SEPARATE | Clearance evidence and admission remain distinct from BEPU retained contact; no command receipt may fabricate that clearance |
| Renderer, Earth/celestial presentation, Blender tooling | UNRELATED | Copied canonical observations may drive visuals; art/camera transforms do not own force, torque or command time |
| Scene-owned authoritative manual/SAS latch/dispatch after replacement qualifies | RETIRE as production mechanism, later only | No deletion now. Preserve tests, reusable math and development scenes until an accepted replacement covers their responsibilities |

NovaCore prefixes omitted in shorter table rows are `src/NovaCore.Simulation/`. Missing production responsibilities were also checked semantically across Simulation/Interop/sample source for throttle, propulsion, actuator, gamepad and joystick. No complete engine/actuator/player-command model was found. Broad names such as SimulationTransactionEngine are not engine-propulsion evidence.

## Decisive integration gap

An ordinary future command timeline insertion changes TimelineRevision, even if its time is after the next contact endpoint. Direct torque/force replacement changes captured state/revision. Both conflict with current retained authority. Consuming a timeline event also changes revision; resolving the event does not automatically permit the same contact binding to continue. Current contact must stop/refuse until a separately qualified lifecycle or dynamics-admission contract applies.

Therefore neither an input buffer alone nor an unrestricted reuse of existing force/torque commits qualifies powered contact. The proposed command-only slice preserves physical state/revisions and then requires explicit later control-to-actuator-to-retained-world admission. This conclusion comes from reachable production checks, not architectural preference.
