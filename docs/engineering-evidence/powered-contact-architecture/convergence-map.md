# Responsibility convergence and the prepared interval

## Classification

| Responsibility | Classification | Existing owner / required convergence |
|---|---|---|
| Canonical source spacecraft identity/configuration | ALREADY COMMON | SimulationTransactionEngine / SimulationState / SpacecraftStateStore |
| Canonical body pose, linear velocity, orientation, angular velocity | ALREADY COMMON | One FP64 canonical spacecraft; two private numerical consumers do not create two authoritative states |
| Expected-authority capture / frontier binding | MUST CONVERGE | LocalContactWorld.Publication binding and PoweredFlightStorage duplicate expected state/clock/revisions; one interval owner must advance them only by acknowledged self-publication |
| Mass | MUST CONVERGE | M14 frozen source scalar versus M15 exact-law stage mass and endpoint mass; common versioned mass-law input |
| COM | SHOULD REMAIN SEPARATE | Dry geometry authorship versus resource mass-distribution law; current central point preserves COM; no automatic reinterpretation of article mass |
| Inertia | MUST CONVERGE | Common physical mass properties; BEPU receives bounded inverse properties while free solver uses canonical doubles; current sphere/asymmetric domain mismatch must be qualified |
| Gravity/environment | MUST CONVERGE | Explicit acceleration/force convention; frozen M14 mg source must not become g=m0*g/m(t); M15 currently zero environment |
| External engine body force | MUST CONVERGE | Genuine M14.23 stored wrench from common interval, transformed once by appropriate stage orientation |
| External engine body torque | MUST CONVERGE | Full r cross F and general gyro law; no COM-only shortcut; M14 callback currently lacks torque |
| Exact physical interval/event identity | MUST CONVERGE | Original integer T(n) plus M14.24 exact interior offsets; no float consumer owns time |
| Resource segmentation | ALREADY COMMON | M14.24 is reusable proposed input; contact has no second ledger |
| Resource consumption | MUST CONVERGE | Reuse single canonical resource writer at joint commit, never in BEPU callbacks |
| Actual actuator state | MUST CONVERGE | Current actual storage is inside powered physical owner; convergence must make it common interval authority, not copy a second actual state into contact |
| Command state | ALREADY COMMON | M14.22 regime-independent request/edge owner; do not reset at contact loss |
| Canonical publication/history | MUST CONVERGE | Existing owner/slot/clock primitives shared; extend one coherent successor contract, not call two publishers |
| Private solver constraints/manifolds/impulses | CONTACT-SPECIFIC | Retained BEPU owns private numerical continuation; not serialization or canonical contact authority |
| Collision geometry/contact selection | CONTACT-SPECIFIC | Banked M14.21 selector and authored geometry; dynamic input adapter needs consistency |
| Smooth scaled RK integration | FREE-FLIGHT-SPECIFIC | M15 evaluator remains reference in its admitted domain; cannot replace it with BEPU Euler and claim equivalence |
| Private continuation acknowledgement | MUST CONVERGE | One prepared same-phase acknowledgement of physical/resource/actual/clock tokens, optionally including retained-world frontier |
| Physical evidence/admission | SHOULD REMAIN SEPARATE | M14.17 certified clearance, BEPU contact receipt and free endpoint proof retain distinct capabilities |
| Copied presentation | ALREADY COMMON | Graphics receives canonical observation, never live body or proposed actuator/resource |
| Consumer selection / future recontact coverage | MUST CONVERGE | Owner chooses eligible private consumer using physics/coverage evidence; current contact flags alone insufficient |

## Preferred immutable prepared physical interval

One owner-issued sealed capability with copied value payload:

- exact source epoch and target; original T0, interval index, stable episode/model identity;
- source paired endpoint, FP64 mass/COM/principal inertia and physical/configuration revisions;
- genuine active engine proposal and exact resource proposal, held privately by owner;
- copied M14.23 full body wrench and stored mass flow; ordered M14.24 durations/resource successor;
- versioned central-point mass law and required stage-mass evaluation;
- explicit environmental acceleration/force/torque law with frame/time provenance;
- requested-command revision/frontier and conditional actual-actuator progression;
- admitted numerical contract / consumer capability, preallocated publication destinations;
- expected timeline/clock/debt and private-world generation/frontier when constrained.

The copied data is not authorization; the seal and parent leases are. A BEPU callback may only read the numerical schedule, not the capability that spends fuel. Values must not retain a mutable canonical state view.

## Common outcome, distinct numerical consumers

Source -> prepared interval -> exactly one eligible private consumer -> checked staged successor -> common canonical commit -> prepared private acknowledgement -> copied endpoint.

Keep consumer-specific work in small adapters: BEPU collision/constraint solving versus qualified unconstrained RK. Common preparation, actual/resource calculation, validation, history and fixed-write logic should not be cloned into a new PoweredContactStorage that competes with PoweredFlightStorage.

This is convergence of responsibility, not a generic plugin transaction engine. Reuse the existing exclusive owner phase, checked slot preparation, clock installation and exact resource/command readers. Banked public/internal entry contracts retain compatibility and focused regressions during any future refactor.

## Evidence limits

The common boundary is strongly supported by current source. It does NOT by itself prove a physically correct contact integrator. Three gaps remain visible rather than hidden behind an interface:

1. Continuous changing mass and exact fractional/tiny resource events must become valid constraint numerical work.
2. The authored asymmetric article/gravity domain is not the current M15 free-flight domain.
3. Switching private consumers needs qualified no-contact interval coverage and recontact/lifetime semantics; an empty contact list is not that proof.

Thus the boundary wins architecturally, but production authorization is **REVISE** until the numerical/consumer proof is bounded and explicit.
