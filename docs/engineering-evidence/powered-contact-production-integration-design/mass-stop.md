# Mass responsibility: bounded stop resolved

**A. ADOPT KSA SOURCE-MASS POLICY** for the accepted bounded ordinary-contact scheme. Project Control authorized one bounded investigation after the initial stop. Source/analytical review and already-retained physical witnesses resolve the policy choice; no new solver run, mass sweep or numerical mechanism was needed. Production integration remains unimplemented and must pass its own repeated-cadence gates.

## Three separate responsibilities

| Responsibility | Current proof | Unresolved part |
|---|---|---|
| Exact resource successor | `PropellantSegmentation.Calculate` computes exact consumed/successor units and exact powered/coast duration. Sealed transaction proposals qualify applicability. | None reopened. Exact resource observation is not permission to step or debit. |
| Canonical successor properties | `CentralPointReservoirV1` is an ideal point reservoir at dry COM; total mass is correctly rounded dry+remaining units, inertia remains declared dry inertia. M15.0 jointly publishes these values. | No distributed tank/COM migration or article wet/dry reinterpretation has been qualified. |
| Numerical properties inside ordinary constrained solve | Hold the acknowledged source mass/inertia through the ordinary solve. This is the current KSA constrained policy and the accepted bounded native witness policy. | Broader body/depletion domains and repeated integrated accuracy remain validation responsibilities, not already proven by a one-step fixture. |

## Exact current-source anchors

All paths below are relative to `E:\NovaCore`; line references are for baseline `49057fec...`.

1. `src/NovaCore.Simulation/Spacecraft/Resources/FinitePropellantSegmentation.cs:18–48,75–108`: central point reservoir; source/successor mass observations are rounded views, exact amount/offset fields remain exact.
2. `src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Propellant.cs:37–47,107–146`: physical source mass/inertia consistency and proposal formation; no native mass policy selected.
3. `src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightEvaluator.cs:24–35,45–67,100–111`: narrow dry-8 kg spherical-inertia/no-environment model; changing powered RK stage masses; successor mass for coast; rotating body-frame thrust.
4. `src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs:63–77`: exact dyadic stage mass preparation from dry+successor+remaining consumed units.
5. `tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.Physics.cs:94–103`: independent changing-mass oracle; constant-source-mass mechanism differs by >1e-3 m/s, versus the accepted 1e-6 m/s velocity gate. This is a **free-flight** counterexample to universal substitution, not proof the small diagnostic contact fixture failed.
6. Same test `:105–122`: body-frame demand rotates; frozen root demand fails an independent witness. The contact diagnostic's fixed wrench must likewise not be silently generalized.
7. `src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactSource.cs:113–120,153–156`: banked box inertia is proportional to total mass; article admission requires the authored exact mass/inertia. Neither automatically models wet mass with fixed dry inertia.
8. `LocalContactWorld.cs:66–73,112–120,124–162`: native inverse mass/inertia initialized once; fixed source acceleration; current ordinary step/export never prepares successor mass.
9. `LocalContactWorld.Publication.cs:14–35,61–78,128–134`: publication binding stores readonly physical properties; unrelated mass change is correctly stale authority. Successful acknowledgement updates pose/revision/clock/frontier, not mass properties.
10. `SimulationTransactionEngine.PoweredFreeFlight.cs:198–207,224–268`: qualified free-flight physical/resource/actual-actuator successor is one transaction. This does not create a contact mass-update capability.

## Accepted evidence limits, not a new failure

[Ordinary closure README](../powered-contact-ordinary-step-event-closure/README.md):44–97 explicitly states source-held mass, fixed inertial wrench, one observed interval, largest tested fuel about 0.000130208333 kg and source/dry inverse-mass discrepancy <=1.628e-5. Its ideal rigid-model support/acceleration bounds justify those witnesses only; they explicitly do not bound compliant BEPU contact or future changing-mass continuation.

The same report :108–118 identifies one retained world prepared for 120 gravity steps and **one** observed ordinary interval, and explicitly says the numerical `I=(2,2,2)` fixture is not unmodified M14.18 admission. The [consolidated handoff](../powered-contact-ordinary-step-event-closure/PRODUCTION-DIRECTION-HANDOFF.md):55–57 leaves source/successor mass lifecycle unqualified.

The later fixed-workspace projection qualification preserves the six consumed inputs of this admitted diagnostic. It proves neither a new mass policy nor a repeated property update. No exact-event/projection result is reopened.

## Current KSA comparison

Actual installed constrained `FullPhysicsConstrainedStep` (`06001C19`) uses staged source properties through its native slices, then consumes resource/recomputes staged successor properties. Later owner population uploads current properties. Its unconstrained `IntegrateVelocityVerlet` (`06001B92`) instead predicts changing mass for endpoint derivatives before actual consumption. Current KSA itself has regime-specific numerical policies.

This defeats two shortcuts: there is no universal KSA source-held policy to copy, and an actual production precedent alone does not qualify NovaCore's bounded numerical outcome. Retain game-owned property timing as **ADAPT**; precision admission must be explicit.

## Required NovaCore constrained outcome

Use the already-declared powered-contact materiality bars, not M15.0's free-flight oracle: ordinary H=1/60, 8 iterations / 1 substep; exact resource/event ledger; four-contact support and no material false separation; velocity error <=.06 m/s, angular-rate <=.0489897948557 rad/s, displacement <=.001 m, orientation <=.000816496581 rad, normal/tangent impulse <=.48 N*s, twist/support-moment <=.0979795897 N*m*s, source/endpoint geometric penetration <=.001 m; signed tangent and twist discrete-work errors each <=.07848 J.

These protect the ten exact retained body/load/depletion witnesses in `ordinary-step-event-closure/inputs.json`, not a continuous rectangle made from their extrema. Their body is the numerical 2x1x1 box with dry mass8, I2, small central fuel, fixed inertial wrench, and supported retained prestate. Existing unpowered box/article long-sequence and M15.0 gates remain separate, unchanged preservation tests. Expanding fuel/thrust/body/rotation/support/departure scope is not justified by this assessment.

## Cheap mass-only analytical proof

Let h be powered duration, md=8, m0=md+q*h, m(t)=m0-q*min(t,h), H=1/60, I=2, tangent coefficient k and twist coefficient c from each exact retained row. Comparing source-held versus changing mass for the same chronological wrench:

`delta Jn = g*q*h*(H-h/2)`; `abs(delta Jt)=k*delta Jn`; `abs(delta Jtw)=c*delta Jn`.

With `A=Fx+k*Fy`, the tangent rate difference is bounded by `abs(A)*q*h*h/(2*m0*md)`, and yaw-rate difference is `c*delta Jn/I`. Constant mass also makes the integrated averaged wrench equal to the integrated chronological wrench for these endpoint impulse/rate identities. Event-timing effects on displacement/orientation remain in the full native comparison.

Exact-fraction arithmetic over the retained inputs, stdout only, found these maximum mass-only effects/bounds (all at off-COM):

| Quantity | Bound/effect | Existing contact bar |
|---|---:|---:|
| Mass loss kg | 1.302083333333e-4 | Exact ledger, no native mass tolerance |
| Relative change | 1.627604166667e-5 | No standalone bar |
| Normal impulse N*s | 1.596679687500e-5 | .48 |
| Tangent impulse N*s | 1.995849609375e-6 | .48 |
| Twist impulse N*m*s | 2.231427699714e-6 | .0979795897 |
| Normal moment N*m*s | 9.979248046875e-7 | .0979795897 |
| Tangent velocity m/s | 4.747101555422e-7 | .06 |
| Yaw rate rad/s | 1.115713849857e-6 | .0489897948557 |
| Position mass contribution m | 7.911835925703e-9 | .001 |
| Orientation mass contribution rad | 1.859523083095e-8 | .000816496581 |

The last two bounds are H times the rate bounds for the same wrench timing. These explain the small mass effect; they are **not** a compliant-BEPU stability proof, continuous-work enclosure, error subtraction or long-horizon bound.

## Existing native evidence settles the bounded policy

Actual source-held 8/1 native outputs were already compared directly to the changing-mass chronological reference. Across ten retained rows, maximum errors were .0055524194 m/s velocity, .0042548881 rad/s angular rate, .0001703953 m displacement, .000761922433 rad orientation, .0440989561 N*s normal impulse, .0055140537 N*s tangent impulse, .0061630229 N*m*s twist and .0060583663 N*m*s normal moment. Maximum independent penetration was .0004298389 m. All satisfy their own bars; support checks pass.

The closest result is orientation at **93.316%** of its bar. Do not broaden domain from these maxima. The [later work qualification](../powered-contact-ordinary-step-event-closure/friction-work/README.md) uses unchanged source-held native outcomes: off-COM tangent/twist errors .0041788200982/.0006883590519 J and yaw .0016713229434/.0000003207288 J, each <.07848 J. Its accepted meaning remains ordered discrete impulse-work approximation. The old root report's superseded work-unqualified label is not the current conclusion.

No mass-related physical failure was found. No evidence justifies evaluating successor, midpoint, average or continuous constrained mass as an alternative. A new sweep would have no demonstrated payoff.

## Native successor-property update is an existing responsibility

Actual pinned `BepuPhysics.dll` SHA256 `77185E...D1FA7`, `BodyReference.LocalInertia` getter token06000077, returns the local-inertia field by reference. KSA `CopyToBepu`06001B40 writes that field. The pinned [BodyReference](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/BodyReference.cs#L143-L153) and [Bodies](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Bodies.cs#L392-L423) permit direct update if the body remains dynamic. Existing awake qualification avoids sleeping/island migration.

`SetLocalInertia`0600007E→06000028 is the broader API: wake if needed, write local inertia, clear ephemeral world inertia, and perform kinematic-transition handling. Dynamic→dynamic handling returns without changing pose/velocity, reconstructing body, clearing manifold/narrowphase, or resetting/scaling impulses. The [ordinary TypeProcessor](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/TypeProcessor.cs#L1171-L1307) gathers the new local inertia and derives world inertia from the current native orientation before solving. Old impulses are ordinary warm-start guesses, not canonical conserved quantities.

This proves a property-input update mechanism exists; it does not prove future convergence merely because caches remain allocated. No velocity rescale, exhaust impulse or COM/shape change is implied by the setter.

## Selected contract and scope

1. Exact canonical source mass is the rounded view of dry+current exact fuel; inverse mass is prepared from that source, not from pending fuel debit. Hold it for the complete ordinary constrained solve.
2. CentralPointReservoirV1 keeps dry inertia and COM unchanged. Native inverse inertia is prepared from that declared source inertia. No new variable-inertia physical law.
3. Exact resource successor is prepared before solving; canonical mass/resource/actuator/physical successor commit together, once. No callback debit.
4. After commit, fixed acknowledgement advances expected canonical property/resource identity. Before the **next** native ordinary step, prepare/validate and install that acknowledged source's numerical properties on the same awake dynamic body. Do not call a solver or property setter inside canonical fixed commit.
5. Retain native pose/velocity/handles/manifold/warm starts. No source-epoch reset. Stale/failed authority cannot silently rebind. If native mutation fails, invalidate private continuation and preserve prior canonical commits.
6. Use a separately explicit resource-aware admission for the declared dry body plus central point fuel, preserving the old uniform-box/article admission unchanged. This is an integration/admission delta using an existing physical law, not automatic reinterpretation of an authored article.

**A. ADOPT KSA SOURCE-MASS POLICY.** The policy choice is resolved for the bounded scheme. Repeated production cadence, canonical lattice transport, source-frame/body-wrench mapping and physical successor updates must still pass integrated validation; no new physical accuracy claim is made now. Resume architecture design, without production implementation or a numerical campaign.
