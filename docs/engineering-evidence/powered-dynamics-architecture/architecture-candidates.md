# Candidate decision and first physical ticket

At most three serious candidates. This recommendation grants no implementation authority.

| Dimension | A: coupled free-flight + joint publication | B: private evaluator only | C: retained BEPU universal dynamics now |
|---|---|---|---|
| Ownership | NovaCore stage evaluator, existing canonical owner | Pure private successor; no applied result | BEPU would own both flight/contact numerical work |
| Segments/mass | Exact two-segment plan; scaled local-time coupled RK4; exact-law stage mass | Same numerical obligations as A | Rational events/continuous mass must enter FP32 solver schedule |
| Rotation/wrench | Share existing quaternion/Euler math; rotate body thrust per stage | Same | Callback orientation/torque integration needs qualification |
| COM/inertia | Explicit central point model, fixed dry inertia | Same | Inverse mass update without scaling dry inertia |
| Contact reuse | Share interval model/authority/publication, later specialized solver adapter | Defers application and contact obligations | Broadens immediately into warm starts, mass cadence and event-sized solver steps |
| Atomicity/lifetime | One joint bundle, sources consumed with commit, same-phase ack | Sources remain conditional; physically invisible | Joint commit still required; solver failure/asymmetry expands risk |
| Revisions/history | One coherent physical/resource/actuator record | No real successor/revision | Same canonical contract plus contact identity |
| Determinism/error | Fixed ordered bounded numerical policy; explicit approximate trajectory | Same unqualified numerical work, less observable payoff | FP32/sleep/solver continuity and tiny dt impose extra qualifications |
| Allocation/cost | Fixed stage/workspace; bounded history; measure complete path | Cheap preview but another layer before real motion | Retained allocation possible, cost/stability not inferred from old tests |
| KSA convergence | ADAPT separate dynamics regimes and game-owned apply; intentional exact authority difference | Only prepared half of lifecycle | Overfits contact; no current-source evidence justifies universal ownership |
| Judgment | **PREFER** | Kill as final accomplishment: insufficient physical payoff when joint owner is feasible | Kill for first slice: unsolved contact responsibilities swamp bounded flight |

Unchanged banked motion facade and analytic-only general integration were killed before this table: the former freezes root force/mass; the latter cannot integrate rotating/off-COM body thrust. The logarithmic solution remains an independent oracle.

## Preferred responsibility chain

    closed canonical commands (M14.22)
      -> genuine prepared BODY engine wrench (M14.23)
      -> genuine exact resource segments/mass law (M14.24)
      -> bounded coupled free-flight physical evaluation
      -> prepared joint canonical successor
      -> fixed resource + actuator + mass + paired motion + time/history commit
      -> private expectation/source-lease acknowledgement
      -> copied committed endpoint.

Physical integration and its joint publication should be ONE bounded first physical accomplishment. A private evaluator is a necessary internal stage, not a separate preview-only milestone. The existing owner doctrine makes atomicity a concrete extension, although readonly resource storage, actual actuator authority and endpoint validity must be implemented and tested. This does not imply those changes are trivial.

## Recommended ticket: Apply segmented finite-fuel propulsion in bounded free flight

One independently translating dry-body fixture, ideal point reservoir, one fixed ideal BODY engine and original 1:1 exact60 Hz schedule. Admit the explicitly declared net-effective engine-wrench physical closure. No contact. Use zero environment for the independent analytic case; any optional constant root environmental force is authored separately and applied once. No gravity-provider/world integration required in this slice.

Implement the scaled exact-ratio numerical adapter, a narrow coupled evaluator sharing banked rotational math, and joint canonical publication as described in the adjacent contracts. Preserve preparation-only APIs while adding application mode. Add the endpoint-only validity fence across direct/paired/event/proof consumers before exposing motion. Make explicit actual actuator state/applied cursor and owner-writable exact resource source; no standalone fuel/mass commit.

### Required cheap-first sequence

1. Freeze input/source identity and derive fixed numeric workspace bounds. Declare the admitted physical envelope, component precision budgets and bounded subdivision policy BEFORE running the qualification matrix. Preserve supported fractional/tiny positive depletion; do not use double-dt representability as physical admission.
2. Prove ratio conversion/weighted-product/stage-mass arithmetic independently, including underflow duration with representable impulse and overflow-prone intermediates with finite result.
3. Prove centered analytic changing-mass translation and private coupled off-COM rotation/force transform. Convergence and error bounds must hold in the declared envelope; zero numerical error is not implied by exact resource timing.
4. Implement and prove the full joint transaction, retry/lease semantics, endpoint validity fence and all precommit nonmutation/postcommit terminal paths. No visibility until these pass.
5. Run long multi-interval resource/actuator/physical conservation and equivalent host-partition sequences, then full regression/measurement and one minimal visible route.

### Physical and authority qualification bar

- Full powered, full unpowered, endpoint and interior exhaustion; exact U conservation and stage mass law; no negative fuel, no thrust past depletion, and no dropped tiny positive segment.
- Analytic x/v oracle with stable logarithmic small-loss evaluation; independent converged rotating/off-COM reference, mirrored torque signs, nonzero body rotation with visibly changing thrust direction; no constant-source-mass shortcut.
- At least one depletion crossing in a multi-interval sequence and subsequent Enabled+NoFeed intervals; command shutdown and later new ignition are distinct from feed state. No refuel, safing or spool behavior invented.
- Exact integer targets/debt; StateRevision and ActuatorRevision once per applied interval; ResourceRevision iff exact resource change; no physical CommandRevision/TimelineRevision increment. One history record per interval and reconstruction of exact exhaustion provenance.
- Default/foreign/fabricated/stale parent/resource/physical receipt; wrong owner/reentrant; canceled and reissued resource; discarded parent; mismatched mass/configuration/model; stale resource/actuator/command/physical/timeline/clock; outstanding endpoint; event before/at target; insufficient debt; all arithmetic/revision/capacity failures. Precommit proves no partial successor; prior admitted host/command changes remain real.
- Force final precommit refusal with sound retryable endpoint and postcommit private-ack failure. All canonical successors committed in terminal case, leases dead, private continuation invalid; no rollback or blind retry.
- Endpoint-only observation at target passes; later ordinary integral/fractional/raw-source/proof evaluation refuses. Same-time canonical command capture and next M14.23 preparation still work. Existing legacy evaluation behavior unchanged.
- Force/torque/attitude/paired replacement paths cannot strip endpoint-only validity while the powered regime owns the craft. An explicitly admitted future regime transition is required to regain a propagatable legacy model.
- Equivalent integer host partitions at30/60/150/240 Hz plus bounded backlog give the same committed physical/resource/actual-actuator endpoints under same-build/machine policy. At most four complete intervals per service call, no lost credit/work and no retimed command input.

### Measurement and regressions

Use the existing checked zero-allocation helper and deliberate positive control for the complete warmed preparation/evaluation/publication/ack path and refusals; exact 0 bytes. Keep timing separate under ordinary runtime. Report storage composition for fixed resource/actuator/stage/history/owner state; retain the existing <=8 MiB combined bounded-scenario target without counting unrelated process working set. A1200-record compact joint history must have a cold declared capacity and measured byte cost.

Retain the existing combined-path performance goals as a proposed first-ticket gate: three fresh Release processes, 128 warm then 1024 measured complete operations; median<=0.05 ms, P95<=0.10 ms, P99<=0.25 ms, max<=0.50 ms. Confirm the complete operation includes M14.23/M14.24 and the new physical work; do not add historical percentiles. Stop on first material failure; no allocation forensics or threshold relaxation automatically.

Inspect rather than assume feasibility: banked resource preparation alone reported median 0.0325 ms/P99 0.0712 ms. It used a different report-only workload and excludes new physics. Therefore available headroom is not established by subtraction. Base RK4 work is four derivative stages per substep, up to two exact segments per interval; actual bounded subdivision count determines cost. At four serviced intervals the work remains bounded; no wall-clock performance is claimed here. Measure cold preparation separately and optimize only demonstrated costs.

Require Debug/Release builds and full Simulation, focused M14.17-M14.24, ReferenceFrames/Precision, verified BEPU identity and affected presentation checks. Old physical contact/article fixtures and thresholds remain unchanged. No implementation tests were run in this investigation.

### Visible/manual payoff

After physics/atomic tests pass, add a labelled dedicated powered free-flight fixture route using existing minimal presentation. Display copied COM position/orientation, actual endpoint activity/throttle, exact-resource-derived fuel and exhaustion/shutdown status. Hold the last committed endpoint between publications; do not extrapolate old force or read private state as rendering authority. Begin elapsed sampling after cold preparation; finite completion/failure holds the last authoritative result. Camera movement cannot affect physics. Manual acceptance must confirm acceleration, changing body thrust direction where applicable, exhaustion/shutdown and final hold. This is a fixture, not a live article engine, player bindings or a final asset.

## Deferred dependency chain

Banked command -> prepared engine -> exact resource segments -> **coupled free-flight + atomic joint publication** -> powered retained-contact mass/wrench/solver-continuity qualification -> supported ignition -> contact departure and regime handoff/reacquisition -> Florida pad/world integration -> real player input/ignition/liftoff -> gimbal/attitude-control realization -> controllable ascent.

The first bold responsibility is recommended as one accomplishment. Powered contact may need more than one bounded ticket after its numerical/lifecycle assessment; do not promise it from this free-flight pass. No real tank/COM shift, gimbal/RCS, gravity system, live contact entry, Florida, renderer overhaul, Blender or milestone assignment belongs here.
