# Independent architecture red team

**Independent judgment: REVISE.** Read-only final verifier examined current NovaCore source, pinned BEPU mechanics, current KSA/source identity and the draft architecture. No edits, builds, tests, solver execution or new qualification campaign were performed by the reviewer. Lead retained this summary.

"Requirement supported" below means the architecture preserves the required ownership rule. It does not claim the unimplemented powered-contact path passed a runtime test.

| # | Attack | Disposition | Finding |
|---|---|---|---|
| 1 | Incorrect mass update | REVISE | API supports installation; within-segment sampling/coupling unqualified. |
| 2 | Stale inverse properties | Requirement supported | Forcing, effective constraint mass and selector must use consistent current properties. |
| 3 | Thrust timing | REVISE | Immutable input prevents callback-count authority; constrained exact timing still open. |
| 4 | Thrust applied twice | Requirement supported | One net wrench; no duplicate impulse/rocket term or gyro correction. |
| 5 | Exhaustion collapsed | REVISE | Float dt/reciprocal cannot carry exact tiny domain; ordered pieces alone insufficient. |
| 6 | Wrong solve mass | REVISE | Endpoint/source patch rejected; log mean not general contact/support solution. |
| 7 | Mass-change warm starts | REVISE qualification | Old lambda through current inverse properties is a valid initial guess, not eight-iteration stability proof. |
| 8 | Arbitrary reset | Requirement supported | No percentage threshold or mass-ratio policy; duration policy belongs to bridge. |
| 9 | Manifold destruction | Source supported | Fixed geometry/COM mass update needs no body/geometry/cache reconstruction. |
| 10 | Speculative fuel spending | Requirement supported | Only joint canonical success spends exact resource; prediction callback is read-only. |
| 11 | Off-COM torque omitted | Requirement supported | Full wrench required; current external torque/selector-prediction gaps explicitly retained. |
| 12 | Thrust/weight switch | Requirement supported | Analytical support equation is a witness, not a consumer switch. |
| 13 | Presentation departure | Requirement supported | Geometry/support/separation evidence; no camera/altitude/timer authority. |
| 14 | Command reset | Requirement supported | Same command owner/revision/intent across consumer choice. |
| 15 | Resource copy/reset | Requirement supported | One canonical resource ledger. |
| 16 | Actuator reset | Requirement supported | One common actual-state/frontier owner. |
| 17 | Double publication | Requirement supported | One coherent whole-interval commit; no motion-then-fuel sequence. |
| 18 | Ack rollback | Requirement supported | All committed successors remain; leases spent; private continuation invalidates. |
| 19 | Stale backlog | Requirement supported | Four-interval cap; N+1 reads N's committed/acknowledged successor. |
| 20 | M15 free divergence | REVISE | BEPU integration differs; M15 domain/begin reject departed article/gravity/generic endpoint. |
| 21 | M14 idle divergence | Preservation required | Exact idle physical path retained; new overhead/equivalence not measured. |
| 22 | Recontact prevented | REVISE | Retained resources preferable; clearance/acquisition/reactivation unqualified. |
| 23 | Render cadence physics | Requirement supported | Integer interval and host debt authority, copied presentation once/frame. |
| 24 | Florida contamination | Requirement supported | Generic authored body/slab; world integration excluded. |
| 25 | Third subsystem | Candidate rejected | Independent powered-contact owner loses to common prepared interval. |
| 26 | Common interval rejected | Convergence supported | Existing engine/resource/source/successor primitives support common boundary. |
| 27 | Unsupported KSA analogy | Narrowly supported | Current shared constrained/unconstrained source, not exact numerical equivalence. |
| 28 | Performance/payoff | REVISE readiness | Clear supported-ignition payoff; new operator work/error/finite-iteration cost unknown. |

## Strongest attack and result

The mass/exact-event/constraint intersection remains open. Current LocalContactWorld.Step casts duration to float. Pinned Solver_Solve.cs:1415-1421 takes its reciprocal. The retained M15 exact positive 2^-1075-second event has a representable physical effect. More ordinary substeps, a tick snap, averaged thrust or resource-only exactness cannot satisfy that physical boundary.

A single log-mean mass also does not universally solve the problem: it matches constant-force free delta-v but not the exact centered changing-mass support impulse. Matching callback acceleration while constraint inverse mass or selector prediction stays old is inconsistent.

**The attack survives. Production readiness cannot be PASS.**

Natural departure is a further explicit consumer/domain boundary: empty contacts do not prove a contact-free next interval; unconstrained BEPU is not M15; the current M15 sphere/no-gravity admission excludes the article; dormant contact state is stale until safe reactivation. Keeping canonical ownership common does not establish those numerical facts.

## Accepted evidence corrections

- Corrected LocalContactSource mass/inertia admission anchors to 118-120 and capture to 143-162.
- Explicitly separated the full Euler equation from BEPU callback work so gyroscopic response is not counted twice.
- Corrected selective-reset wording: inspected Solver scaling APIs act on constraint sets. A supported targeted per-constraint/body clear API was not established.
- Current installed official jet-history entry is revision 5176, not the prior report's 5177 attribution. Banked historical files were left untouched.

## Bounded next proof

One event-aware changing-mass constrained interval operator: declared domain, exact event treatment including tiny positive effects, consistent force/inverse-property/selector sampling, finite-iteration warm-start behavior at unchanged settings, independent support/torque/exhaustion witnesses, no-constraint M15 reference comparison and bounded error/work.

After that proof, supported-only slice A is justified. Departure remains separately gated on compatible free model, interval coverage and recontact/reactivation. No production implementation is authorized by this report.

## Preservation

Reviewer independently confirmed HEAD/main/origin/main 49057fecceb0f725d5f551ec40e2780971b0d81d, M15 tag target 4607d8c802006d5e1a01c595ab608cf53a4dab6b, no tracked diff and only the new architecture evidence directory untracked. Lead's final whole tracked-tree/hash/ref check is in identity.json.

No banked outcome was weakened to resolve a review attack. No powered-contact success, allocation/storage/performance result or manual acceptance was invented.
