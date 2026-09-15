# NovaCore outcome / mechanism and banked impact

Current authoritative commit: 49057fecceb0f725d5f551ec40e2780971b0d81d. Source anchors relative to E:\NovaCore; no source changed. Exact resource ledger / event chronology and finite-precision physical realization are different responsibilities.

Primary anchors:
- src/NovaCore.Simulation/Spacecraft/Resources/FinitePropellantSegmentation.cs:114–154: exact available/required comparison, debit, successor and powered/coast ratios.
- src/NovaCore.Simulation/Spacecraft/Resources/PropellantInteger.cs:159 onward: interval-local exact duration, not public fractional clock or solver capability.
- src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs:28–29,63–77: complete scaled duration products and stage mass.
- src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightEvaluator.cs:45–111: separately scoped changing-mass free-flight numerical realization.
- src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredFreeFlight.cs:187–269: prepared endpoint/resource/actual engine, checked canonical fixed publication and acknowledgement; :271–309 owner servicing.
- Banked command/actuation/resource seals and existing contact publisher remain as implemented. The exact-piece/D+12 prerequisite is not a banked powered-contact execution capability.

| # | Required outcome | Current mechanism | Can mechanism change? |
|---|---|---|---|
| 1 | Exact resource amount | Integer-unit resource amount and exact debit | Numerical contact realization may; ledger exactness cannot |
| 2 | Exact exhaustion ordering | Interior powered ratio followed by coast, conditional successor | Solver partition may; chronology and debit order cannot |
| 3 | Interior exhaustion does not abort valid simulation | Two prepared physical segments | Yes, to a qualified chronological event map; unsupported future contact proof may refuse honestly, but is not a completed replacement |
| 4 | No public fractional tick | Interval-local exact ratio; canonical SimulationInstant integral | Internal integration may change; public clock cannot |
| 5 | Exact canonical 60 Hz publication | Original T0 + floor(n*1,000,000/60), paired endpoint | Internal contact cadence may change; endpoints and original epoch cannot |
| 6 | Actual engine coherent with resource | Prepared actual successor activity/NoFeed state in same publication | Yes internally; never fabricate firing after exhausted debit |
| 7 | No fuel double spend | Sealed prepared proposal/source identity and consumed leases | Equivalent ownership primitive only |
| 8 | No propulsion double application | One execution/publication, explicit consumed leases | Equivalent ownership primitive only; no apply+postadd |
| 9 | Correct momentum consequence | Separate mass/wrench numerical map, exact-product tiny free-flight witness | Yes if new contact map proves constrained and open-system consequences |
| 10 | Correct supported-contact consequence | Banked unpowered retained contact; prospective powered exact-piece model | New powered model may change; support/penetration/friction/departure outcomes cannot be replaced by cache equality alone |
| 11 | Changing-mass consequence | CentralPointReservoirV1 ledger; mass-stage evaluation in free flight | Contact numerical representation may be derived; source/successor mass shortcut needs a proven error contract |
| 12 | Atomic motion/resource/actuator publication | Prepared fixed canonical writes + owner/lease/history/clock checks | Reuse; do not copy sequential KSA application semantics |
| 13 | Deterministic host/backlog behavior | Owner admission, bounded exact intervals, retained debt, copied endpoints | Numerical internals may; host partition must not determine physical event ordering |
| 14 | Tiny positive event remains positive in authority | Exact ratio; complete scaled products before projection | Never erase event by first converting h to float/double |

## Banked contract impact

| Banked scope | Disposition |
|---|---|
| M14.21 authored compound retained contact | KEEP unchanged. New powered adaptation must prove the zero-engine branch preserves it; no selector/fixture/cadence rewrite |
| M14.22 canonical command authority | KEEP ordered command/source/owner identity; no repeated command execution per solver slice |
| M14.23 prepared single engine actuation | KEEP prepared wrench/engine definition and exactly-once consumption. Rotation of body-frame force remains a numerical responsibility |
| M14.24 exact finite-propellant segmentation | KEEP authority. It computes exact segments without invoking BEPU; therefore exact internal event can survive a different contact numerical partition |
| M15.0 segmented powered free flight | KEEP entire banked model and results, including tiny representable velocity. No migration of its evaluator in this gate |
| Canonical publication / host lifecycle | KEEP atomic resource/actuator/paired endpoint, revision/history/debt and failure linearization. KSA ready/apply is not an equivalent transaction |

No required banked outcome needs to change for the conditional architecture proposed here. Whether an implementation can satisfy all of them remains a separate proof. If it cannot, return to Project Control rather than relaxing outcomes.
