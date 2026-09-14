# Current Desktop source map

Read-only inspection at `cad23d1ba9476a75d9897b8c20924cc3f1497db9`. Paths below are relative to `src/NovaCore.Simulation/`, unless specified. Source anchors identify the inspected responsibility, not a promise that future line numbers stay fixed.

| Piece / source | Current responsibility and limit | Handling |
|---|---|---|
| `Spacecraft/Translation/SpacecraftTranslationState.cs:7–18` | One positive finite `MassKilograms`; COM position/body origin; no dry/usable decomposition | KEEP; new resource authority is missing |
| `Spacecraft/SpacecraftStateStore.cs:23,74–96,125–134` | Mass array created cold; paired replacement writes translation/rotation only | REUSE fixed-slot preparation pattern; future augmented commit, not a mass write now |
| `Spacecraft/Contact/Staging/EngineeringContactArticle.cs:37–44,66–100` | Sum of authored child masses and analytical COM/full inertia; current article mass is not identified as dry fuel-free mass | KEEP exact authored article; no reinterpretation |
| `Spacecraft/Actuation/SingleEngineActuation.cs:18–39,62–90` | Immutable ideal engine values, issued proposal, conditional body-wrench/required-flow preview | KEEP M14.23; REUSE genuine source capability |
| `Transactions/SimulationTransactionEngine.EnginePreparation.cs:131–190` | Closed exact interval, all checks before private latch/cursor sealing; `flow=thrust/exhaust`, finite output checks | KEEP; resource realization checks positive-demand/zero-flow compatibility independently |
| Same, `:196–233` | Preview checks seal/source; discard does not rewind consumed interval | REUSE through future identical owned-phase validation; never regenerate discarded interval |
| `Transactions/SimulationTransactionEngine.SpacecraftCommands.cs` | Deterministic committed command stream and bounded optional edge bridge | KEEP unchanged; no fuel policy in command owner |
| `Time/SimulationInstant.cs:3–7`, `SimulationDuration.cs:3–7` | Signed Int64 canonical microticks; 1,000,000 ticks/second | KEEP public clock integral |
| `Time/PhysicalEventEpoch.cs:11–14,42–67` | Exact rational identity: Int64 floor, reduced UInt64 fraction; rejects oversized denominator before reduction | KEEP version/encoding; ADAPT internal-event separation, not universal fuel capacity |
| Same, `:79–115` | UInt128 comparison and stable 32-byte encoding; specialized certificate kinds need separate typed contracts | REUSE principles; no silent widening of historical format |
| `Time/PhysicalEventDuration.cs:17–38` | Exact bounded local difference then binary64 seconds observation | KEEP current callers; not authoritative for arbitrary tiny exhaustion |
| `Timeline/PhysicalEventOrderKey.cs:5–7,29–35` | Internal ordering/value identity, not automatic public scheduling/execution | REUSE principle; no synthetic fuel timeline event |
| `Spacecraft/Contact/PrivatePostImpactState.cs:31–40,85–149`; `PrivateCanonicalPropagation.cs:228–275` | Specialized provider-owned root identities, sealed conditional successors, numerical evaluation distinct from authority | ADAPT ownership pattern; contact certificates remain UNRELATED to fuel proof |
| `Spacecraft/Translation/SpacecraftTranslationEvaluator.cs:6–29` | Analytic constant root force / constant mass | KEEP; UNRELATED as a changing-mass body-thrust solver |
| `Spacecraft/Rotation/SpacecraftRigidBodyRotationEvaluator.cs:32–49,91–119` | Bounded integer RK4 steps with fixed inertia/constant body torque | REUSE bounded-work/equation discipline only; no powered variable-mass qualification |
| `Spacecraft/Rotation/SpacecraftPhysicalEventRotationEvaluator.cs:23–45,68–92` | Read-only fractional event evaluation, fixed-inertia model | KEEP; not persistent depletion continuation |
| `Transactions/ContinuationPublicationPhase.cs:3–35` | Existing owner-thread/exclusive publication phase | REUSE; no second lock |
| `Transactions/SimulationTransactionEngine.Continuation.cs:89–140` | M14.17 genuine EventFreeThroughTarget admission and fixed paired publication | KEEP certified-clearance responsibility separate |
| `Transactions/SimulationTransactionEngine.PersistentContact.cs:61–126` | Receipt/event/debt/revision/history/ack preparation, fixed canonical commit and terminal acknowledgement failure | REUSE atomic pattern; KEEP current nonpowered contract |
| `Transactions/PersistentContactPublication.cs:30–38` | One record with unchanged properties, paired before/after and exact revision/clock provenance | KEEP banked record; future joint powered record carries resource/mass provenance |
| `Transactions/SimulationTransactionEngine.ContactServicing.cs:18–60,74–116` | Separately committed host credit and bounded four-interval servicing | REUSE linearization; credit before sealing engine/resource proposals |
| `Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs:18,61–78,129–133` | Readonly bound properties; rejects changed mass; acknowledgement advances paired authority/clock/frontier only | KEEP; cannot acknowledge mass edits today |
| `Spacecraft/Contact/Staging/LocalContactSource.cs:118–119,151–190` | Exact article mass/inertia/source admission; original 60 Hz T0 lattice | KEEP all stale/source/interval checks |
| `Spacecraft/Contact/Staging/LocalContactWorld.cs:66–73,107–117,138–158` | BEPU initial inverse mass/inertia/acceleration; export uses source properties | KEEP; not dynamic wrench/changing-mass admission |
| `tests/NovaCore.Simulation.Tests/CertifiedResponseOracle.cs:9–24`, `SpacecraftPhysicalEventMotionTests.cs:113–127` | Test-only exact BigInteger rational decoding | REUSE oracle technique only; no production BigInteger |
| `tests/.../PhysicalEventEpochTests.cs:43–58,103–118`, `SpacecraftPhysicalEventMotionTests.cs:60–81` | Existing exact event ordering/capacity and read-only sub-tick witnesses | KEEP; not full finite-flow exhaustion coverage |

No existing piece is proposed for RETIRE in this ticket. No broad MIGRATE recommendation is justified. Future narrowly authorized owned-phase factoring must preserve M14.23 checks byte-for-byte in meaning; future powered publication must prepare its complete new successor bundle instead of extending a completed old transaction afterward.

## Missing authority and integration gap

Scoped API/semantic search of `src`, `tests` and relevant documentation found no canonical fuel store, resource revision, tank consumption, finite-feed segment proposal or canonical actuator commit. Current required flow is a proposed demand only. There is no existing general fixed-limb exact resource arithmetic to reuse.

Mass, inertia, paired physical state and exact-event identity are substantial existing infrastructure. None permits physical publication with stale mass or lets a resource preview call itself a committed burn. Three boundaries are particularly important:

1. The resource owner cannot call the current externally guarded engine Preview method from inside its already-owned phase; future implementation must factor the same checks into an internal owned-phase reader, retaining the public guard.
2. A later ordinary host-credit change stales the current engine proposal. Admit elapsed time before closing/sealing; do not acknowledge arbitrary clock changes via resource preparation.
3. Retained-contact mass/force changes require separately qualified solver-state evolution and matching expected-authority acknowledgement. A scalar resource patch is insufficient.

## Mobile/Web lesson

The brief reports a historical non-integral-depletion abort. Retained Desktop evidence separately records Mobile's rational-event precedent and explicitly rejects a canonical-floor fallback: [physical-event epochs](../../physical-event-epochs.md#reference-relationship) and [historical source provenance](../physical-event-epoch/README.md). This investigation does not rerun or copy Mobile and does not use it as a Desktop implementation proof. The independent arithmetic witnesses establish why legitimate physical depletion can require an internal non-integral event.

## Reproduce the source search

From the repository root, read the sources above and run:

```powershell
git status --short
git rev-parse HEAD main origin/main 'm14.23-prepared-single-engine-actuation^{}'
rg -n -i 'propellant|fuel|tank|ResourceRevision|dry.?mass|mass flow' src tests -g '*.cs'
rg -n 'PhysicalEventEpoch|PhysicalEventDuration|BigInteger|UInt128' src/NovaCore.Simulation tests/NovaCore.Simulation.Tests -g '*.cs'
rg -n 'MassKilograms|Properties|Inertia|InstallCertifiedContinuation|PublicationPhase' src/NovaCore.Simulation -g '*.cs'
rg -n 'PrepareSingleEngineActuation|PreviewSingleEngineActuation|DiscardSingleEngineProposal|ConsumedThrough' src/NovaCore.Simulation -g '*.cs'
```

Distinguish test oracles, read-only evaluation, identity and actual canonical mutation when interpreting hits. No build or formal performance/allocation campaign is needed for this static map.
