# Candidates and bounded next responsibility

No implementation authorized. These are architecture choices judged against current source and the proposed authority contract, not benchmarked alternatives.

## Three serious candidates

| Criterion | A: monolithic evaluator | B: every request through controller/wrench allocator | C: hybrid engine-direct plus attitude allocator |
|---|---|---|---|
| Physical correctness | Can be correct only if internal five-way separation is retained; easy to conflate throttle, thrust and torque | Clean requested/achieved wrench, but ignition/propellant/core state still needs a separate hardware channel | Engine lifecycle directly addresses hardware; assisted motion separately requests realizable response |
| KSA convergence | Loses visible controller/core/nozzle responsibility boundaries | More generic than inspected KSA; direct engine commands do not require optimization | Closest current responsibility map: direct engine branch plus TVC/RCS allocation |
| Determinism/canonical time | Possible with explicit inputs/state, but opacity makes hidden progression harder to review | Possible with fixed algorithm/order; unconstrained optimization is unacceptable | Fixed engine path initially; bounded canonical controller/allocator added only when qualified |
| Failure atomicity | One proposal can help; a monolith still must expose all successor state | Staged pipeline fits atomic proposal if stages cannot commit independently | Same staged proposal discipline; direct engine path is not a separate commit |
| Extensibility | Grows into unrelated controller/hardware/chemistry complexity | Useful for later multiple competing actuators; premature as first gate | Engine, gimbal and RCS can be qualified separately while sharing output/authority |
| Contact/free-flight reuse | Possible if it does not integrate bodies | Good if dynamics remains downstream | Good; no regime-owned hardware copies or alternate physics authority |
| Performance/storage | O(authored hardware) feasible; giant abstraction not needed | Generic optimizer cost/saturation policy needs evidence; no current need | O(1) first engine, O(B) command batch, bounded O(hardware) aggregation later |
| Authored data | All hardware/control facts risk being lumped together | Requires meaningful capabilities, weights/priorities before useful allocation | First engine has small explicit physical definition; control policy stays separate |
| Scope | Excessive as a framework; acceptable only as a small first evaluator inside C | Rejected as mandatory universal front end | **PREFERRED** |

Kill A as the overall ownership architecture, not the option of implementing the first algebraic engine in one understandable type. Kill B's requirement that ignition/throttle become arbitrary desired wrench before hardware. Retain B's controller/allocator separation where multiple hardware choices actually exist. Choose C with one shared prepared/commit contract.

## One first implementation ticket

**Prepare single-engine actuation from canonical commands.**

One independently translating spacecraft; one fixed-axis authored ideal restartable engine; existing finite command lattice; no controller/gimbal/RCS implementation, no dynamics application, no fuel depletion. This slice establishes the useful production preparation boundary, not a live canonical engine advancing through unapplied intervals.

### In scope

1. A validated, stable-identity authored engine definition: finite positive maximum force/effective exhaust speed; finite mount relative to a declared body origin/COM; validated unit force axis; explicit instantaneous zero-minimum proportional throttle `[0,1]`; explicit restartable ideal model/version. No representative numeric engine constants are borrowed from KSA or meshes.
2. A narrow composed transaction-owner operation that preflights fixed command-transition capture, commits requested state under unchanged M14.22 semantics and records the ordered transition through fixed writes. No observer callback. Preserve neutralization reserve. Explicit capacity backpressure before command mutation; detect bypass/gaps rather than rebind from the final snapshot. This prerequisite is included in the slice, not left to an undefined logger.
3. Explicit prior hardware values and source identity; deterministic ordered ignite/shutdown outcomes; separate hardware availability and available-feed qualification assumption. Requested throttle persists while Off. Off/Enabled latch and inactive/firing/unavailable outcome as specified in the authority contract.
4. Pure bounded preparation from one closed canonical command boundary to the next exact target: sealed proposal with conditional engine successor, body interval-model force/moment, required flow, source/batch/config identity and copied preview. Proposal allocation/storage is prepared cold; repeated evaluation does not consume cursors or mint a canonical actuator revision.
5. Explicit preview lifecycle: observe, discard/reprepare against the same valid source or refuse stale source. One pending prepared proposal storage slot; no fake physical acknowledgement or production preview-to-canonical promotion.

This is independently qualifiable because request ordering, hardware outcomes, analytical force/moment/flow and canonical nonmutation have direct exact/analytic witnesses. Multi-interval evaluator tests may feed privately proposed values into a deterministic fixture; this proves evaluator behavior only. They cannot advertise a production canonical engine history or ready powered-contact path.

### Qualification bar for that future ticket

- Verify current refs and M14.22 regressions; preserve all existing command admission, prospective E, no-op, capacity, neutralization and typed target contracts.
- Show throttle requested while Off produces no output; ignite realizes retained throttle; shutdown returns zero; newer restart works; zero throttle leaves Enabled/inactive; unavailable ignition is an explicit hardware denial, not a lost requested edge.
- Same-E ignite/shutdown and shutdown/ignite preserve order and zero elapsed time; repeated identical edge kinds remain distinct sequences; no duplicate pulse/ignite execution on repeated preview or host partitions.
- Prove bounded capture over repeated queue drain/refill; full capture refuses before requested mutation, reserve remains available, no post-command failure can lose a transition; explicit bypass/gap/foreign/default/duplicate/stale/reentrant/wrong-owner cases fail closed.
- Require successful Close and exact closed-prefix witness; a late command cannot change a prepared prior interval. Preserve original T0 and exact 16,666/16,667 lattice; no floating accumulated time.
- Independently check `F=u*Fmax*d`, `tau=(p-COM) cross F`, required flow `|F|/v_effective`, signs/units/frame transforms and finite bounds. Include offset-mount torque, zero/maximum demand, malformed definitions, unsupported nonzero-minimum/rate models and extreme arithmetic refusal.
- Verify proposed-only labels and source seal; mutation/clock/config/revision changes invalidate old proposals; default/copied values alone grant no physical application capability.
- Every preview and refusal leaves canonical physical/actuator/resource state, clock/debt, StateRevision, TimelineRevision and physical history unchanged. Only separately successful M14.22 command commits change requested authority; their effects are not rolled back on actuator refusal. No actual fuel is present/spent in this slice.
- Compare equivalent canonical command histories under varied host/display partitions; same-build/machine proposal values and edge outcomes match. Do not claim device sampling or cross-platform identity.
- Exact zero warmed managed allocation with the qualified checked helper and deliberate positive control; fixed declared storage/capture counts, finite arithmetic/work bounds. Timing measured only under the future ticket's approved qualification, not inferred here.
- Focused relevant Simulation/ReferenceFrames/Precision and command regressions, full Debug/Release build, source preservation and diff check. Any required alteration of M14.22 outcomes or current contact admission is an escalation, not a quiet extension.

### Exclusions

No canonical ActuatorRevision commit/evolution yet; its source/successor meaning is modeled, not applied. No finite propellant store, mass changes, exhaustion execution, chemistry, solid motor, spool, gimbal, RCS, SAS controller, generic optimizer, player bindings, renderer/Blender work, thrust integration, contact/free-flight force admission, departure, pad placement or milestone. No production loop may privately advance proposals and present them as committed spacecraft behavior.

## Follow-on boundary and Florida dependency ordering

These are dependencies, not authorization to open concurrent implementation fronts.

1. **Banked M14.22 requested command authority** supplies exact prospective input. First slice above prepares hardware demand from it.
2. **Finite-fuel realization and exact exhaustion/resource accounting** must qualify before a finite-fuel engine can physically power the spacecraft. A deliberately ideal external-feed test fixture can help qualify demand, but does not satisfy launch fuel requirements. Joint-feed limits, during-interval mass and eventual COM/inertia laws must be explicit.
3. **Changing force/torque/mass admission with atomic actuator + dynamics publication** must preserve retained-world state and NovaCore authority. Contact and free-flight need qualified consumers of the same model, not old constant-root-force substitutions. No current M14.21 acceptance is widened by this document.
4. **Bounded supported powered behavior and constrained control policy**, followed by **contact/free-flight departure and ownership handoff**, must qualify before claiming liftoff. Gimbal/attitude allocation must be available before controllable ascent; RCS is required only if the authored vehicle/control envelope needs it. A free-flight PD law is not automatically support-safe.
5. **Florida pad placement/physical-frame and geometry integration** must qualify before a Florida launch demonstration. Article/slab qualification is not that integration. This can be assessed in parallel with later dependencies, but no live Florida powered path is ready without both.
6. **Actual player bindings and feedback** must feed M14.22 and show requested versus committed realized state before player ignition acceptance. Scripted numerical qualification may precede device bindings; bindings alone cannot qualify physical propulsion.
7. **Player ignition -> supported thrust -> departure/liftoff -> controllable ascent** is the end-to-end acceptance sequence after the above requirements. Atmosphere/nozzle applicability for Florida must match the admitted physical model; a vacuum/effective-exhaust fixture is not automatically an Earth engine certification.

Blender/final representation is not a prerequisite to mathematical actuator qualification: explicit engineering geometry and validated mount metadata suffice. A final visual spacecraft front can follow or proceed independently; it must be reconciled with physical metadata before final visual acceptance. It never replaces authored force/mass/COM authority. Tank slosh, broad vehicle systems and richer assets do not belong in the first slice.
