# Spacecraft command and control authority assessment

Architecture investigation only. 2026-09-13. **PASS** for the bounded architecture and implementation plan below. No player-control implementation, qualification run, milestone assignment or banking occurred.

## Baseline and evidence scope

HEAD, main, origin/main and remote main: `8c291881b323be3e3038d57d2ac180eff826f065` (Docs: refresh NovaCore landing page for M14.21). Banked authored compound article: `b6e8fa568585706ce38c0609aeb14db4a643123e`, tag `m14.21-authored-compound-spacecraft-contact`. Branch remains main. The unrelated untracked `bepu-local-contact-staging/staging-build-blocker.md` is preserved.

Current NovaCore source was inspected first, then the installed-identity-matched retained KSA source, then relevant official live-changelog history. The cached KSA source corresponds to installed production 2026.9.7.5402; official history may be newer. A later changelog entry does not silently update the inspected binary. No new decompilation or proprietary source copy was retained.

Retention budget: **96 KiB**, revised from the initial 64 KiB estimate to retain the required source, lifetime, cadence and convergence tables with independent review and reproduction. Concise Markdown and a small identity manifest only; no raw Discord export, benchmark output, runtime dump or copied KSA implementation. This is static architecture evidence, not measured qualification of the proposal.

## What happened

KSA's actual path is action mapping, buffered UI/input events, vehicle-owned persistent manual state, a copied worker snapshot, flight-computer control, engine/RCS/gimbal commands, physical core/nozzle realization, force and torque, constrained or unconstrained physics, then applied vehicle/module observations. The path supports common spacecraft control ownership across contact and flight. It separates an engine request from the thrust actually produced.

It does not provide a drop-in NovaCore command clock. Input drains by category before the next worker frame. Throttle-up/down uses player delta time. The flight computer evaluates at worker-frame starts as well as additional simulation wakeups; it is not globally fixed at 10 Hz. Input edges can collapse into final state before physics. These findings prevent importing a frame snapshot as canonical command authority.

NovaCore already has exact time/debt, deterministic event ordering, one publication owner, paired FP64 state, force/torque transactions, pure SAS/target mathematics and qualified retained contact. Its celestial sample owns manual/SAS mode and dispatch, including a real 50,000-tick SAS cadence. It lacks a simulation-owned player intent/lifetime contract, hardware actuator realization and an admitted dynamic-force/torque update for retained contact.

The strongest integration finding is concrete: contact captures constant force, refuses nonzero source torque, and rejects unexpected state/timeline/clock changes. Putting player commands into an ordinary timeline or calling the existing torque transaction does not make powered contact valid. The authority checks must remain; a separately qualified owner-authorized dynamics admission is required.

## Current KSA source and official history

The complete [source map](current-source-map.md) records types, owners, readers/writers, callers and cadence, including device/rebinding limits and current NovaCore classifications. [History and convergence](history-convergence.md) keeps official intent separate from current implementation and contains the required fourteen-responsibility matrix.

Decisive historical lessons include startup/shutdown replacing push-to-fire; input/gauge processing moving through buffered events; physical controller/core/nozzle separation; realized propulsion driving presentation; input clearing on focus/control/warp transitions; and an RCS repeated-command bug especially visible under warp. These corroborate responsibility boundaries. They do not prove deterministic replay, pad-assist stability or NovaCore applicability by themselves.

## Preferred architecture

**Simulation-owned vehicle command state with bounded, prospective, exactly ordered intent admission.**

Device/UI action mapper -> bounded intent ingress -> existing simulation owner -> canonical command state -> controller -> physical actuator realization -> exactly one dynamics owner for the vehicle/interval -> canonical physical publication -> copied command/actuator/physical observations.

The [command authority contract](command-authority-contract.md) specifies lifetimes, effective epochs, backlog, event order, ownership loss, pause/warp scope and refusal/commit linearization. A command is an intent, not a force or pose. A requested wrench is not an actuator. An accepted command is not proof of realized thrust or published motion.

Control possession is separate from assist mode and physical regime. Contact does not hand player authority to a second controller. Ignition may be requested while supported, subject to hardware and admission policy; support/liftoff remains a physical result. Neither the current article nor this assessment qualifies propulsion while in contact, assist stability on the slab, contact departure or ascent.

## Architecture candidates

| Responsibility | A: Frame-copied vehicle state | B: Prospective canonical admission (preferred) | C: Timestamped device event mapping |
|---|---|---|---|
| Authority/lifetime | KSA-like vehicle latches and frame drain | Vehicle command record owned by canonical transaction owner; ordered edges and held/latch/target state | Same owner and lifetimes as B, with timestamped event ingress |
| Input cadence | Latest frame snapshot; ramp may use player dt | Polling supplies intent only; canonical boundary assigns authority; canonical dt drives ramps | Device events mapped through admitted host/rate/pause history |
| Consumption | Worker/frame boundary plus internal control updates | Explicit open canonical control boundary, before control and interval physics | Exact mapped epoch, rounded only under declared integrator-boundary rules |
| Propulsion/attitude | Good KSA separation if retained; direct sample torque alone is inadequate | Controller demand -> supported actuator realization -> wrench -> single integrator | Same downstream chain as B |
| Contact/flight | Same intent owner is useful; timing does not fit current contact binding | Same command owner; separately qualified dynamic contact admission and regime handoff | Same as B |
| Warp | Copying KSA frame policies would alter authority and requested effects | Initial active-control scope 1:1; explicit refusal of unsupported rate/paused execution; later qualification | Requires exact rate/pause history and event-clock calibration first |
| Publication | Main-thread applied vehicle/module state | Copied accepted command, realized actuator and committed physical observations with distinct identities | Same as B, plus mapping provenance |
| Determinism | Fails NovaCore's backlog/render invariant without redesign | Identical accepted epoch/sequence/value stream must yield equal endpoints under different host partitions | Potentially stronger capture fidelity; more mapping ambiguity and machinery |
| Cost | Simple, but unbounded growth/category drain and cadence coupling are unsuitable | Fixed prepared storage, bounded command/control/physics budgets; no per-step heap target | Additional timestamp history, conversion and overflow work |
| Convergence | ADAPT responsibility; reject literal cadence/lifetime mechanism | ADAPT KSA graph; INTENTIONALLY DIFFER in time/order/replay mechanics | ADAPT graph; INTENTIONALLY DIFFER in timestamp admission |
| Decision | Reject as production command authority | Select | Defer; not needed for the first bounded responsibility |

B wins because it retains KSA's useful ownership graph while preventing a newly sampled input from changing already-admitted backlog. C may improve future device-event fidelity, but importing host timestamps requires a separately specified pause/rate mapping. A's weakness is a demonstrated contract mismatch, not its origin or implementation age.

## Positive evidence for NovaCore differences

The banked article qualification compares all 1,200 physical endpoints for centered and tilted fixtures under 30/60/150/240 Hz and delayed integer host partitions. It reports equal endpoint bits, 400 x 16,666 + 800 x 16,667 = 20,000,000 ticks, exact +1,200 state revisions/history records and retained debt conservation. See [formal qualification](../compound-contact-coverage/formal-qualification/README.md#host-schedules-and-physical-equivalence), source tests `EngineeringContactArticleQualificationTests` and `ContactServicingTests`.

Those are existing measured witnesses for simulation/render partition independence, exact temporal accounting and retained-world continuity. The proposed command extension must preserve them for the same accepted command stream. They do not prove the unimplemented extension or identical human input capture at different polling rates. No new performance number is claimed.

## Bounded implementation sequence for one production writer

1. **First candidate: canonical command admission and lifetime only.** One already-prepared physical article, 1:1 time, one owner/lease, bounded prepared ingress and command-history storage. Implement explicit edge/held/latch/target records, prospective epoch assignment, copied observations, deterministic non-actuating consumer, overflow/refusal and lease cancellation. Do not add propulsion, contact-force changes, Florida or gameplay controls. Qualify identical accepted streams under different host/backlog partitions, duplicate/old/foreign input refusal, same-epoch order, no early consumption and exact zero warmed allocation with the existing positive control. Prove canonical physical state and existing retained world remain unchanged by this non-actuating candidate. Return to Project Control.
2. **Physical realization candidate, separately authorized.** Define one supported engine/actuator model against accepted physical metadata. Prove command vs availability vs realized thrust/torque, conservation, limits, ignition/shutdown semantics, and staged controller/actuator/propellant state. Reuse appropriate FP64 dynamics math; do not call a requested torque a thruster model. No arbitrary KSA gains/constants or chemical framework import.
3. **Retained-contact actuation admission, separately authorized.** Prepare/recheck the exact interval's force/torque and private acknowledgement under the existing owner. Preserve body/manifolds/T0/frame and stale mutation rejection. Bind staged command/actuator state to the same pending endpoint; no re-step or repeated pulse on refusal. Compare powered private-only and publishing paths before expanding applicability. Keep current unpowered qualification intact.
4. **Manual/assist convergence.** Move production mode/target lifetime and deterministic control cadence from the scene into the owner; reuse qualified pure math only where it matches the physical article and actuator limits. Qualify constraint-aware authority, manual override and unsupported target/mode refusal. Add presentation/input bindings only to this proven path.
5. **Florida prerequisites and regime transition, separate responsibility.** Authoritative terrain/pad placement, first-contact/entry/departure contracts, dynamic forces, environmental models and safe one-owner handoff must qualify before a player-driven launch. Do not disguise the existing 20-second fixture as launch gameplay.

These are responsibility boundaries, not authorization to execute five projects. Only the first bounded candidate is recommended for the next Project Control instruction.

## Florida launch dependencies and Blender boundary

| Player operation | Required contract before integration |
|---|---|
| Place article on Florida pad | Accepted physical definition plus authoritative terrain/pad contact and placement; current slab witness is insufficient |
| Ignite while supported | Command admission + engine availability/lifecycle + dynamic retained-world force admission |
| Throttle-up | Canonical throttle lifetime/ramp + realized propulsion/limits; no frame-dt increments |
| Liftoff | Contact/departure criteria and one dynamics-owner transition; command lease/targets survive |
| Pitch/yaw/roll | Actual RCS/TVC/other declared hardware, qualified torque response and manual arbitration |
| Controllable ascent | Command path above, environment/propulsion validity, free-flight integration and copied feedback |

Future authored metadata may supply engine/RCS locations, thrust axes, control axes, gimbal limits and hardware identities after validation against COM/inertia/units. Blender meshes, camera axes and visual transforms are never runtime force or command authority. No asset pipeline is designed here.

## Red team and judgment

[Verifier and reproduction](verification-reproduction.md) records independent attacks and resolution. Important corrections incorporated: rate-hold is not automatically attitude-hold; KSA is not globally 10 Hz; UI and keyboard warp gates are not identical; a press/release pair need not cause physical thrust; same input values do not confer authority; contact state cannot be updated by bypassing captured force/revision checks; failed publication cannot silently advance actuator timers/propellant/controller state.

**Judgment: PASS.** The source graph and bounded architecture are sufficiently resolved to propose a production ticket. Dynamic contact actuation, powered assist, pause/rate transitions and Florida departure remain explicit qualification dependencies, not uncertainty to implement through.

What happens next: Project Control reviews the first implementation scope. No automatic implementation. No milestone assignment. No banking.

## Package

- [Current source map](current-source-map.md)
- [Command lifetime and cadence](command-authority-contract.md)
- [Official history and convergence matrix](history-convergence.md)
- [Independent verification and reproduction](verification-reproduction.md)
- [Identity and integrity](identity.json)

No existing source, tests, dependencies or historical tags were changed. No builds, runtime tests, benchmarks or visual acceptance were run for this static investigation.
