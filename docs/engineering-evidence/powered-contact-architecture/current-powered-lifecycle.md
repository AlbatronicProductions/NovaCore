# Current M15.0 powered interval lifecycle

Banked M15.0 at 4607d8c802006d5e1a01c595ab608cf53a4dab6b; inspected current source at public main 49057fecceb0f725d5f551ec40e2780971b0d81d.

| Stage | Current source | Authority / lifetime |
|---|---|---|
| Canonical requested commands | SimulationTransactionEngine.SpacecraftCommands.cs:150-230 | Regime-independent requested throttle/axes/mode/ignite/shutdown; own CommandRevision; boundary closed before physical interval |
| Genuine engine preparation | SimulationTransactionEngine.EnginePreparation.cs:126-193 | Sealed proposal binds source/clock/command/index. Stored force is throttle*Tmax*bodyAxis; moment r_COM cross F; stored flow T/exhaustSpeed |
| Preparation cursor | Same file:187-192,228-238 | Successful sealing ALREADY advances private latch/cursor/LastBoundary and drains captured edges. Explicit retirement does not rewind. This is not canonical fuel/actual advancement |
| Genuine resource preparation | SimulationTransactionEngine.Propellant.cs:105-146,157-169 | Validates parent active seal; exact classification and successor; private resource seal only. Must retry same active parent after a recoverable refusal |
| Exact segments | FinitePropellantSegmentation.cs:75-109; PropellantInteger.cs:160 | One segment or ordered powered + unpowered; exact numerator/denominator; observations do not forge execution rights |
| Physical source / model | SimulationTransactionEngine.PoweredFreeFlight.cs:58-98,177-200 | Accepts initial legacy epoch then uses copied applied endpoints internally; initial binding is not a generic endpoint adoption API |
| Coupled dynamics | PoweredFlightEvaluator.cs:24-82,100-110; PoweredFlightNumerics.cs | Changing stage mass from exact units; body force rotates at each RK stage; spherical body moment/2; no gravity/contact; scaled duration preserves tiny products |
| Prepared actual state | SimulationTransactionEngine.PoweredFreeFlight.cs:196-213 | Actual activity/latch/throttle/applied powered duration/frontier/ActuatorRevision derived from same segmentation and successor resource |
| Joint canonical publication | Same file:224-260 | Exact source/lease recheck; endpoint/mass/resource/actual/state revision/clock/debt/history fixed commit; every execution lease retired after canonical success |
| Private acknowledgement | Same file:261-267 | Advances expected physical/resource/clock/revision/frontier. Failure retains ALL canonical writes, invalidates continuation |
| Host service | Same file:139-165,272-314 | Separate host credit and physical publication; at most four intervals; each next interval reads just-committed successor |
| Endpoint/presentation | SpacecraftAppliedEndpoint.cs; SpacecraftStateStore.cs:29-47; PoweredFlightDevelopmentScene.cs:85-105 | Endpoint-only validity; downstream extrapolation fences; copied pose/resource/actual observation, rendering once per display frame |

Source files live under src/NovaCore.Simulation/Transactions, src/NovaCore.Simulation/Spacecraft/{Actuation,Resources}, and samples/NovaCore.Triangle respectively.

## Qualified model is deliberately narrower than the resource authority

PoweredFlightEvaluator.cs:28-35 requires dry mass 8 kg, dry I=(2,2,2), initial fuel <=1/128 kg, interval <=16,667 ticks, force <=9,000 N, moment <=2 Nm, plus bounded coordinates/speed/angular rate. Its derivative contains no environmental acceleration and no general asymmetric-inertia gyro term because spherical inertia cancels that term.

BeginPoweredFreeFlight additionally rejects pre-existing applied endpoints, nonzero constant root force/body torque and any non-origin initial epoch (owner file:65-84). Continuing an already-started M15 episode is supported; importing a departed M14 article by calling Begin is not.

CentralPointReservoirV1 itself admits positive diagonal dry inertia and explicit dry mass. It leaves COM/dry inertia fixed while exact fuel changes total mass. A general resource definition is not proof that the banked physical evaluator accepts that definition.

## Revisions and history

StateRevision advances once per successfully published physical interval. ActuatorRevision advances once with actual frontier progression. ResourceRevision advances only if exact consumed units are nonzero. CommandRevision belongs to separately committed requested-state transitions. TimelineRevision remains the observed authoritative timeline value; publication does not invent an event.

One PoweredFlightRecord carries segmentation, endpoint, actual successor, before/after physical/resource revision provenance, timeline and debt before/after. Private seal/world handles do not become canonical history. Exact resource fields coexist with approximate bounded physical state; the record does not claim an exact trajectory.

## Proposal/failure nuance that convergence must keep

Fuel and actual hardware do not advance on preview, speculative dynamics or a refused canonical publication. However, M14.23 private preparation cursor consumption already occurs on sealing. The common prepared interval must retain that active engine lease, resource lease and private pending result for safe retry; it must not call engine preparation twice at the same frontier.

Before a private solver mutation, perform every cheap numerical-domain/event/debt/capacity check possible. If the solver has advanced and export fails, invalidate private continuation with canonical physical/resource state untouched. If a complete staged endpoint is sound but publication is temporarily refused, retain it and the active leases; retry publication without solving/spending twice.

A failed private acknowledgement after canonical commit retires all leases and leaves endpoint/resource/actual/clock/debt/history authoritative. Do not describe either physical or host-credit service as an all-or-nothing batch when an earlier operation in the same call has already committed.
