# Official history and convergence

Official history is design intent/reporting, not independent validation or current-code proof. The lead freshly reread three exact official Discord messages (2905, 3391, 5177) on 2026-09-13 through the authenticated read-only browser. Other narrow entries below reuse the previously verified [banked history record](../spacecraft-control-architecture/history-convergence.md), which preserves dates and exact message links. No broad input research was repeated, no messages/reactions were sent, and no bulk channel export was retained.

## Relevant official history

| ID | Official entry | Actuator relevance / evidence limit |
|---|---|---|
| A1 | [2025-08-18 startup/shutdown](https://discord.com/channels/1260011486735241329/1260112103134724146/1407178045856682117) | Separate ignition/shutdown input from throttle. Retained historical witness; not a physical start-delay model |
| A2 | [rev2330, 2025-09-08](https://discord.com/channels/1260011486735241329/1260112103134724146/1414716016516272189) | Corrected presentation using manual throttle during automatic burns. Supports realized feedback rather than request-based exhaust |
| A3 | [rev2905, 2025-11-24](https://discord.com/channels/1260011486735241329/1260112103134724146/1442679965236854785) | Freshly read: separate controllers, cores and nozzles; feed/gas production distinct from thrust, with ambient-pressure effects. Current corresponding code is mapped separately; historic known issues are not presumed current |
| A4 | [rev3391, 2026-02-03](https://discord.com/channels/1260011486735241329/1260112103134724146/1468433433167532104) | Freshly read: TVC feedforward and gains from output rather than input throttle, intended to reduce overshoot. No gain or quantitative improvement adopted without qualification |
| A5 | [rev3416, 2026-02-05](https://discord.com/channels/1260011486735241329/1260112103134724146/1468874467320266784) | Duty cycle separated low continuous output from pulses for time-compression exhaust presentation |
| A6 | [rev4579, 2026-06-09](https://discord.com/channels/1260011486735241329/1260112103134724146/1513776664390144104) | Prevented RCS pulse/rate scaling growth below 1x. NovaCore does not inherit warp-dependent physical effectiveness |
| A7 | [rev4947, 2026-07-16](https://discord.com/channels/1260011486735241329/1260112103134724146/1527479506494029825), [rev5144, 2026-08-03](https://discord.com/channels/1260011486735241329/1260112103134724146/1534019342486868101) | Enable policy and disabled-jet/controller/translation behavior. Both assisted rotation and translation must respect enable state |
| A8 | [rev5177, 2026-08-05](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058) | Freshly read: repeated execution of jet commands caused overshoot, chatter and excess propellant use, especially at warp. Strong reason to bind pulse lifetime and prevent replay; effect not remeasured here |
| A9 | [rev5200, 2026-08-07](https://discord.com/channels/1260011486735241329/1260112103134724146/1535150852585820221), [rev5319, 2026-08-18](https://discord.com/channels/1260011486735241329/1260112103134724146/1539465134064402453) | Reported automatic-burn constraints and TVC/gain corrections. Corroborates distinct policy; not optimality or pad-stability proof |

## Convergence matrix

ADOPT accepts an equivalent responsibility/mathematics. ADAPT preserves that responsibility with NovaCore authority and bounded models. INTENTIONALLY DIFFER names a specific mechanism and positive existing outcome to preserve. Current-source references expand in [KSA map](ksa-actuator-map.md); NovaCore references expand in [current map](current-novacore-map.md).

| Responsibility | KSA current source | History / intent | NovaCore current state | Decision | Rationale |
|---|---|---|---|---|---|
| Engine command intake | FlightComputer396-443; EngineControllerState | A1,A3 | M14.22 ordered requested commits and prospective E | ADAPT | Preserve request/hardware separation; bounded lossless transition handoff, no snapshot edge replay |
| Ignition/shutdown | RocketCore172-216; SolidMotor195-225 | A1,A3 | Requests only | ADAPT | Explicit authored restartable ideal engine first; no universal solid shutdown or invented failure model |
| Throttle realization | RocketCore direct clamp/timers | A4 | Requested throttle; no hardware | ADAPT | Instantaneous zero-min proportional first model; no FX-derived spool |
| Thrust calculation | CombustorConfig; NozzlePerformance31-67 | A3 | Constant-force dynamics, no engine | ADAPT | Small authored effective-exhaust model first; preserve output units/assumptions, no chemistry import |
| Core/nozzle ownership | Rocket74-186; RocketNozzle217-250 | A3 | Missing hardware split | ADAPT | Separate flow/magnitude from mount/direction even if first model is one small evaluator |
| Propellant flow | VehicleProperties133-149; ResourceManager | A3,A8 | Immutable scalar mass; no resource owner | ADAPT ownership; INTENTIONALLY DIFFER exhaustion/commit | Existing exact internal-event and nonmutation witnesses demand shared-feed conservation and atomic state, not inferred live KSA exhaustion |
| Gimbal | GimbalController, GimbalAxis11-14 | A4,A9 | Missing | ADAPT | Authored axis/pivot/range, requested vs actual angle; rate dynamics only if specified and qualified |
| Desired torque | FlightComputer errors/feedback/actual authority | A4,A9 | Pure SAS PD requested torque | ADAPT | Controller output is desired response, not unlimited applied torque |
| Actuator allocation | Per-axis TVC/RCS selection; SelectJetsToFire | A4,A7,A8 | Missing | ADAPT | Separate bounded allocation with achieved/residual output; no generic optimizer or copied heuristic constants |
| RCS | ThrusterController geometry, pulse core commands | A6,A7,A8 | Request enable/translation only | ADAPT | Stable indexed hardware, explicit pulse lifetime and blend policy; unavailable axes remain residual |
| Realized observations | RocketCoreState/NozzleState; applied Vehicle state | A2,A5 | Copied requested and physical observations | ADAPT | Copied typed prepared/committed hardware/output observations, no live core/BEPU access |
| Force/torque production | Active nozzles; F and r cross F | A3 | FP64 vectors/frames/rigid-body math | ADOPT mathematics; ADAPT boundary | Body wrench at named COM/frame; one dynamics consumer and explicit time-domain model |
| Actuator state lifetime | Worker module/core/controller state and timers | A8 | CommandRevision and physical revision are separate | ADAPT | Sibling ActuatorRevision/cursor, immutable source and prepared successors; unchanged command may still yield evolution |
| Physical commit/publication | Game-owned worker application; module updaters | No history entry proves atomicity | Fixed paired publication, debt/history and terminal private invalidation | INTENTIONALLY DIFFER mechanics | Measured exact endpoint/refusal/terminal outcomes require one owner transaction for hardware/controller/resources/physics, followed by private acknowledgement |
| Contact/free-flight reuse | Same hardware outputs, downstream dynamics branch | A3; no pad stability proof | Retained contact captures immutable force/mass and refuses changes | ADAPT | Shared hardware model; later explicit changing-wrench/mass admission, no weakened source checks or BEPU receipt through M14.17 |

Further intentional timing difference: exact original integer lattice and canonical elapsed intervals, initially 1:1, instead of copying frame-initial updates, floating wakeups or warp-dependent pulse scaling. Positive NovaCore evidence is the banked equal-frontier host-partition result and exact debt/revision/history/refusal witnesses linked in the current map. No claim that this proves performance superiority or cross-platform bit identity.
