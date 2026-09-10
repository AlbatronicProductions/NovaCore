# Post-M14.6 production responsibility assessment

Research only, 10 September 2026. **Lead judgment: PASS.**

**Recommended next responsibility: pure coherent spacecraft motion evaluation at a supplied exact physical-event epoch.** This is Candidate E, a smaller prerequisite extracted from Candidate A. It supplies evaluated values and their provenance, not proof that a contact root exists. No implementation, branch, milestone assignment, build, test run or banking was performed by this assessment.

## Current banked baseline

Repository: `E:/NovaCore`, branch `main`, clean at inspection. HEAD/main/origin/main all resolve to `00f226096b61a117184464b5619e464d39789574` (documentation reconciliation). The annotated M14.6 tag resolves to production commit `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`. M14 remains OPEN; there is no unbanked candidate.

| Banked tag | Peeled production commit |
|---|---|
| m14.1-canonical-surface-point-queries | 5b9b02ab1d2ba0e903809510908e4819984d4de2 |
| m14.2-spacecraft-translational-authority | caa6d93ccd7c4e5924381e077a62585f99e5a32a |
| m14.3-spacecraft-terrain-contact-observations | 79978cf8b0fd783d43a661df6df53a2c0202af4a |
| m14.4-atomic-contact-response | 28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c |
| m14.5-isolated-analytical-contact-response | b53d48025cfb8142cb32a6fc6fceb6018e33cfcd |
| m14.6-physical-event-epochs | d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6 |

## Bars and source method

Before deep comparison, the common rejection bars were: no fractional public time; no inference of represented zero gap from exact time; no combined discovery/response/publication implementation; no duplicate authority; a useful independently testable result is required. Applied individually:

| Candidate | PASS bar | REJECT as next step if |
|---|---|---|
| A | Define precisely what evaluation certifies and produce coherent values from existing owners | General certification needs an unspecified root, terrain-time provider, or new response policy |
| B | A complete private state tuple can execute and publish correctly | Only a frontier/queue shell exists without physical state and mutation contracts |
| C | A defined trajectory and root result have a meaningful consumer and representation | Roots cannot be represented or consumed without a prior evaluation contract |
| D | An actual provider proves a specific contact relation; receipt cannot be forged or become stale silently | It is metadata around FP64 gap, or bundles discovery, event state and response |
| E | One smaller missing owner supplies usable physical values from banked authority | It merely renames an existing value or quietly becomes general execution |

Current NovaCore source was read first. The installed KSA distribution contains production assemblies, not an authored KSA/BRUTAL Git checkout. Existing production-IL decompilation was reread against matching installed assembly identity. Current BRUTAL/Bepu/Planet.Core metadata was inspected nonexecutingly with PEReader; previously retired framework method-body decompilations were not recreated. This limits framework-body claims, but KSA's actual call sites establish the relevant ownership boundary. Official Discord was searched read-only in the authenticated desktop browser. No private API or account data was extracted.

## Missing execution chain: actual source ownership

| Current owner | What exists | Missing responsibility |
|---|---|---|
| PhysicalEventEpoch / PhysicalEventOrderKey | Exact bounded rational identity, wire encoding and ordering | No duration evaluation, state, scheduling or execution |
| SpacecraftTranslationEvaluator | Pure constant-force FP64 evaluation from a canonical segment epoch | Requested time is SimulationInstant only |
| SpacecraftRigidBodyRotationEvaluator | Pure constant-body-torque bounded RK4; spherical torque-free analytical path | Requested time and RK4 tail are integral ticks only |
| SpacecraftMotionEvaluator | Complete translation/rotation tuple at one identity, revision and integral instant | Cannot request the corresponding tuple at a nonintegral PhysicalEventEpoch |
| ContactBodyMotion | Celestial/body/frame evaluation at canonical time; moving-surface velocity | No event-local celestial/body-time evaluation provider |
| SpacecraftContactGenerator | Immutable terrain authority, authored features, canonical coherent motion, represented radial gap | No root certificate or fractional observation contract |
| IsolatedContactObservation / IsolatedContactResponsePolicy | Generator-issued immutable receipt; identity/revision/query/segment checks; strict represented zero gap | Receipt proves canonical observation provenance, not an exact physical root |
| Contact transaction engine | Preflight, recomputation, paired linear/angular replacement, history, revision and canonical event consumption | Intent, event, clock and replacement epochs remain integral; no private fractional mutation/history |
| SimulationClock | Canonical clock, exact host-derived debt, canonical boundary servicing | No private physical frontier or atomic multi-subevent interval publication |

Source anchors: [epoch](E:/NovaCore/src/NovaCore.Simulation/Time/PhysicalEventEpoch.cs), [motion](E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftMotionEvaluator.cs), [linear evaluation](E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftTranslationEvaluator.cs), [rotation](E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Rotation/SpacecraftRigidBodyRotationEvaluator.cs), [body motion](E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Contact/ContactBodyMotion.cs), [contact policy](E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Contact/IsolatedContactResponsePolicy.cs), [paired commit](E:/NovaCore/src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.ContactImpulse.cs), [clock](E:/NovaCore/src/NovaCore.Simulation/Clock/SimulationClock.cs).

`SimulationStateView` is a read-only interface, not a deep snapshot: its spacecraft view references the live fixed store. Capturing it does not freeze its backing data. Any new retained evaluation input must copy the relevant immutable segment values within the single-writer phase and bind them to the state owner/revision. A revision value alone is not globally unique across different state stores.

## Candidate A — Certified event-local physical evaluation/state

**WHAT:** produce a qualified physical evaluation at E from all contributing authorities.

**HOW:** existing spacecraft and celestial/terrain owners evaluate; a composition boundary binds identity, epoch, input versions, values and the exact proposition proved. No second mutable Simulation store.

**BAR result:** too broad as the immediate implementation. A single general certificate would have to distinguish numerical evaluation correctness, common-time frame coherence, terrain witness authority and root truth. Those are not one interchangeable proof. Rotation and body motion are presently integral-time consumers. General contact qualification additionally needs a mathematically defined root relation.

**PAYOFF: HIGH eventually, MEDIUM immediately.** Unlocks physical policy once those predicates are real. Cheap tests can validate one provider; certifying every contributor together is premature. Narrow the first provider rather than invent a general certificate framework. Runner-up as an architectural direction, not a complete next implementation.

## Candidate B — Private event-local execution frontier

**WHAT:** own staged physical progress and eventual canonical publication across an interval.

**HOW:** a future private frontier must bind the original canonical revision, complete evolving linear/angular segments, physical epoch/order cursor, pending work, journal records and a final canonical publication tuple. Failure or stale authority must discard the unpublished candidate consistently. Rendering continues to consume canonical snapshots.

**BAR result:** REJECT as next implementation. M14.4 currently recomputes and commits only when intent time, event time and public clock agree. Adding a fractional cursor alone would be unusable; making it useful now would also require event-local evaluation, mutation, history and continuation contracts.

**PAYOFF: LOW now, HIGH later.** Can its design precede certified event state? Its outer invariants can be specified now; a responsible working implementation cannot precede the state and mutation contracts it must execute. A general contact-root certificate is not required for every possible event, but some correct provider-specific event-state contract is required.

## Candidate C — Physical root/event discovery

**WHAT:** determine an event condition along a valid trajectory.

**HOW:** a contact provider would need a defined continuous condition involving the authored point, spacecraft translation/rotation, moving planetary frame and canonical height authority. It must establish bracketing, uniqueness or multiplicity, refinement termination and failure semantics. A discontinuous or piecewise terrain predicate cannot inherit generic smooth-root assumptions.

**BAR result:** REJECT as next production implementation. The current consumers accept integral state, and M14.6 cannot encode arbitrary irrational roots. Returning a convenient rational approximation as exact would violate identity. Root evaluation also cannot presume the FP64 radial-gap function is continuous or reaches represented zero.

**PAYOFF: LOW now, HIGH later.** Discovery can be useful before response for conservative prediction, waking or rejecting a proposed interval; KSA provides such examples. That is a different bounded contract. Under this ticket's event-execution objective, a discovered timestamp alone leaves the consuming-state gap unresolved. Do not build discovery merely to add more unconsumable identities.

## Candidate D — Event-local contact certificate

**WHAT:** provider-issued evidence of one contact relation with derived quantities for response.

**HOW:** eventually use a typed receipt binding the root proposition, unique event/root identity, exact supported epoch representation, craft/geometry/terrain/body authorities, trajectory segments, branch/side semantics and physical witness quantities. The issuer must prove that relation; a caller cannot set a boolean or relabel an ordinary observation.

**BAR result:** REJECT as next implementation. It is smaller than a general Simulation state, but not smaller than the missing evaluation provider. The current receipt already handles canonical provenance. A genuinely new certificate must additionally prove root truth and align terrain/body state at E. Implementing that now combines several missing responsibilities. A certificate-only DTO would prove nothing.

**PAYOFF: MEDIUM now, HIGH when a root provider exists.** Keep this contact-specific type later; do not make contact metadata the universal representation for burns, staging or exhaustion. M14.5 remains strict and unchanged; a future certified-root response path requires explicit qualification, not an unnoticed bypass of its zero-gap gate.

## Candidate E — Pure coherent spacecraft motion at a supplied exact epoch

**WHAT:** evaluate one existing spacecraft's linear and angular segments coherently at a caller-supplied supported PhysicalEventEpoch, without changing any authoritative state.

**HOW:** Simulation owns the provider. It captures the current spacecraft's immutable force/rotation segments, properties, owner identity and revision within the existing single-writer boundary. Exact epoch identity is retained separately from derived FP64 numerical duration. Translation and rotation use their current physical models; rotation preserves the established full-step partition and adds only a properly evaluated fractional terminal duration. Integral requests retain existing behavior. Return a complete immutable evaluated result or an explicit failure; never partial linear success with stale rotation.

The initial consumer is a pure query, not a private clock. Evaluation from one canonical authority snapshot at one supplied E is sufficient. The result is a **transient evaluated state**, not authoritative state, a publication candidate or proof of contact. Its provenance supports stale-owner/revision checks; its numerical contract states finite bounded approximation, not exact real-valued motion. It cannot be passed into M14.5 by replacing Time with FloorTicks.

**BAR result: PASS, winner.** There is a concrete absent API and an existing production owner. No root search, terrain query, public clock advance, generic event arena or fractional state commit is needed. The change can be independently proved using analytical translation, controlled rotation and integral parity. It turns the banked epoch type into an actual physical evaluation input.

**PAYOFF: HIGH relative to scope.** Immediate outcome: obtain p, v, orientation and angular velocity at E, bound to one spacecraft and source revision, while canonical state is unchanged. Next unlocks: root evaluators, event-local physical policy inputs and later private execution. It does not complete those systems. Avoid a duration-only milestone: exact relative-duration arithmetic is necessary internal machinery of this evaluator, not another standalone abstraction with no physical consumer.

## KSA comparison by responsibility

KSA source root below: `E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/` (production-IL decompilation lines, not authored source lines).

| Responsibility | Current mechanism / lifecycle | NovaCore relationship |
|---|---|---|
| Game time | UniverseTime: Int128 nanoseconds; double constructors round. Universe.GetJobSimStep derives a duration from player time, speed and achieved-speed fraction | **INTENTIONALLY DIFFER**: exact rational order and replay independent of render cadence are positive NovaCore requirements; importing this rounding/cadence would lose representable distinctions |
| Outer step and substeps | SimStep has previous/next UniverseTime and double DeltaTime. Stack-local PhysicsContext records CurrentTime/IntraStepTime. PhysicsBubble chooses substeps; constrained work subdivides into at most 1/60-second calls here | **ADAPT** separate physical progress from published state; do not import fixed limits or call it an exact root frontier |
| Vehicle authority and transient state | VehicleUpdateState references the published vehicle and owns UpdateData/new states. ConstraintSim pushes pose/velocity/inertia into Bepu and reads results back into new states | **ADOPT** game-owned physical state; mechanism-owned temporary solver representation. New NovaCore evaluator is read-only and precedes such mutation |
| Detection and TOI/CCD boundary | PhysicsBubble checks unconstrained pair time and DetectCollisions before selecting constrained work. ConstraintSim calls PredictBoundingBoxes/CollisionDetection; Bepu supplies detection/manifolds. Installed Bepu exposes Discrete/Passive/Continuous modes | **ADAPT** demand/prediction belongs above the solver. No exact rational TOI receipt was found in these KSA paths. Bepu API availability does not prove KSA selects continuous sweep mode; no such claim is made |
| Witness/material/policy | NarrowPhaseCallbacks receives and filters solver manifolds, selects pairs/material behavior; terrain/pad shape data and vehicle associations come from KSA | **ADAPT** typed provider evidence tied to current physical authority. **INTENTIONALLY DIFFER** from speculative-margin/recovery admission for NovaCore's currently qualified isolated strict-zero policy; adopting it now changes the physical problem |
| Lifetime | ConstraintSim and its buffers/handles can persist across steps; BeginContactPass refreshes per-pass evidence. GlobalShapes is shared and mutation is forbidden while stepping. Vehicle/topology change invalidates adapters | **ADOPT** distinguish reusable mechanism storage from transient validity; an event evaluation receipt is not permanently valid because its storage persists |
| Solver input/response | KSA PoseIntegratorCallbacks supplies game derivatives; Bepu integrates/solves; ConstraintSim reads back new kinematics and processes contact loads | **ADAPT** numerical machinery remains beneath game authority. No reason to put event truth in Vulkan or introduce Bepu for this evaluator |
| Publication | PhysicsBubble stages work and marks results ready; VehicleUpdateTask applies synchronized vehicle state and then render events; Universe advances last-step metadata | **ADAPT** explicit stage/ready/apply boundaries. This is not evidence that KSA implements NovaCore's atomic transaction rollback or exact event journal |
| Framework ownership | Current Brutal.Concurrency JobScheduler exposes Queue/Wait/ExecuteJobs/AbortJobs/metrics. KSA JobSystems invokes it; KSA.App and Program own the application loop | **ADOPT** reusable scheduling below application physics; there is no demonstrated BRUTAL-owned contact certificate prerequisite |
| Rendering/warp | KSA.App samples wall time; Program applies previous solver results and dispatches next work per frame. Physics work may contain multiple substeps | **ADAPT** render once per display frame. **INTENTIONALLY DIFFER** by retaining NovaCore's exact host-duration debt rather than achieved-frame-speed as physical time authority |

Primary source anchors: UniverseTime.cs; SimStep.cs; PhysicsContext.cs; PhysicsBubble.cs:865,916,1329,1953; ConstraintSim.cs:105,193,210,311,373,407,510,834,851; VehicleUpdateState.cs; VehicleUpdateTask.cs:141; NarrowPhaseCallbacks.cs; PoseIntegratorCallbacks.cs; Universe.cs:1641,2308; App.cs:28; Program.cs:2092.

## KSA source/history correlation

Official live-changelog inspected in authenticated Discord on 10 September. These are bounded architectural lessons, not a replacement for current source:

| History | Old limitation -> change | What current installed code supports / lesson |
|---|---|---|
| [3813, 12 March](https://discord.com/channels/1260011486735241329/1260112103134724146/1481812386389430325) | Sphere-based contact -> box/triangle collision using Bepu internals as an incremental bridge | Current game-owned ConstraintSim/TerrainPatch adapters support evolving collision mechanism without transferring game authority. Do not copy the old shape method |
| [4136, 16 April](https://discord.com/channels/1260011486735241329/1260112103134724146/1494540216482926693) and [4524, 2 June](https://discord.com/channels/1260011486735241329/1260112103134724146/1511508702937743502) | Vehicle updates became persistent local clusters; shared shapes avoided migrating geometry between bubbles | Persistent PhysicsBubble/ConstraintSim and GlobalShapes remain. Storage lifetime and per-step validity are separate contracts |
| [4646, 17 June](https://discord.com/channels/1260011486735241329/1260112103134724146/1516883816554168393) | Rough terrain-contact state -> Bepu detection; terrain/ocean situation separated | Current callbacks consume physical contacts. Event truth must come from the responsible physical provider, not presentation or a label |
| [4659, 18 June](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885) and [4866, 8 July](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037) | Excessive warp solver work and speculative contacts -> detection/prepass before expensive timestep choice; preserve analytical positions | Current PhysicsBubble prepass and conditional constrained path corroborate the ownership. Evaluation must not silently overwrite a better authoritative state; do not multiply expensive work by all warped ticks |
| [5354, 24 August](https://discord.com/channels/1260011486735241329/1260112103134724146/1541296827129794571) | Impulse interpretation used the wrong substep denominator -> measured solver contract and tests | Current PartContactLoad reads accumulated impulses and uses game-side interpretation; later code is not identical to the historical API wording. Bind values to the interval/model they actually describe |
| [5382, 1 September](https://discord.com/channels/1260011486735241329/1260112103134724146/1544200205769900113) | Pad poses recomputed per vehicle/microstep -> once per pass | BeginStaticObjectPass/ResolveLaunchPads cache is present. Evaluate invariant inputs at their owner and invalidate at the correct pass boundary |
| [5421, 9 September](https://discord.com/channels/1260011486735241329/1260112103134724146/1547117063749902347) | Announced mid-frame bubble merge/split | Newer than installed build 5402. It is evidence of evolving requirements, not proof that this installed source includes the change. Another reason not to copy an entire execution manager now |

No verified history establishes an exact rational KSA contact-root certificate or tells NovaCore to equate exact time with exact gap. No such equivalence is inferred. Installed source takes precedence over older mechanisms and newer not-yet-installed announcements.

## Exact-time versus exact-state analysis

Keep three independent propositions:

1. E has exact identity/order in M14.6's bounded rational domain.
2. The physical model was evaluated at a documented numerical approximation to the exact relative duration, with coherent inputs and bounded numerical behavior.
3. A particular physical event condition holds at E.

The winner implements proposition 2 using proposition 1. It does not assert proposition 3. FP64 evaluation can map distinct exact epochs to identical values; numerical equality cannot merge event identities. Root truth must not be inferred from a rounded duration, position or radial gap.

One cheap exact analytical witness exists without contact: E = 15625/32768 microticks equals 2^-21 seconds. From p=0 with v=8 and zero force, the x displacement is 2^-18 metres. This is an evaluation oracle, not a claim about Earth contact. More general fractions require numerical error analysis rather than exact FP64 equality.

The evaluator must subtract the integral source epoch before any floating conversion. It must not convert two large absolute timestamps to double and subtract them. Exact range checks and full-step counts precede numerical duration conversion. Bounded arithmetic failure must be explicit. Preserve the exact fractional remainder through partition decisions; a rounded terminal duration is a numerical approximation, never a new event identity.

Unsupported irrational roots remain unsupported. A rational bracket/sample can be evaluated but is not the root. Future root certification requires a typed representation and comparison contract; no new wire kind belongs in this candidate.

## Certificate alternatives

| Representation | Judgment |
|---|---|
| Exact state values only | REJECT as general event truth. FP64 physical values are not exact mathematical witnesses |
| Root/evaluation certificate only | REJECT now. A certificate with no issuer/proposition/consumer is metadata |
| Values + generic certificate | REJECT as a universal model. It conflates numerical provenance and contact truth |
| Provider-owned typed certificate with derived values | Preferred later for a specific certified physical event, once its proposition and issuer exist |
| Evaluated values + typed evaluation provenance | **Use now.** Proves source identity/revision/model/common epoch and success of the bounded evaluation; explicitly does not prove a root |

Current-state eligibility must be rechecked against the originating state authority and a fresh revision/segments. An old result can remain a historical numerical observation, but is ineligible as current physical input after replacement. Same-epoch ordered mutations invalidate prior results through the revision; querying an epoch alone does not consume order keys or authorize mutation.

For this candidate, do not serialize object references or an opaque receipt as proof. Replay recomputes from the existing authoritative input records, model identity and M14.6 epoch bytes. No new persistent state format is needed. Future consumption/journaling must carry publication tick and physical epoch separately and revalidate provenance.

## Authority, lifetime and publication

The winner has one ownership classification: **transient evaluated state**. Simulation owns its issuer; existing stores retain authority. Creation occurs during a single-writer capture of the relevant immutable values. The result contains no mutation path and does not read changing live stores after capture. It may be copied for inspection; current eligibility ends on source-owner/revision/segment replacement. No terrain/body authority is certified because none is evaluated in this responsibility.

One epoch and one craft are sufficient. Do not add an ordered multi-event loop now. M14.6 ordering becomes operational when a later execution owner applies subevents and invalidates earlier evaluations. A pure query can demonstrate same-epoch ordering independence for identical source state; it must not pretend to execute those events.

Minimum future publication: stage a coherent linear/angular tuple and associated physical journal against a canonical base revision; execute ordered private mutations and remainder; preflight capacities/revisions; atomically publish at a canonical time with defined debt accounting. This remains deferred. Numerical RK4 sampling is not necessarily partition-invariant: rebasing at E and continuing must not be assumed bit-identical to unsplit evaluation. The future frontier must qualify that integration/continuation responsibility explicitly.

## Performance and payoff

No benchmark was run. Estimates are from source structure:

| Candidate | Immediate payoff | Work/cost exposure | Main risk |
|---|---|---|---|
| E | HIGH for small scope | One linear O(1) evaluation plus rotation; fixed-size inputs/result; zero heap allocation target; zero terrain queries | Rotation is not universally O(1) |
| A | MEDIUM | Multiple authority/frame/terrain evaluations | Unbounded certification scope or duplicate evaluation ownership |
| D | MEDIUM | At least coherent craft/body state plus physical witness/proof | Hidden root search or recovery policy |
| C | LOW toward execution now | Repeated trajectory and terrain evaluations per root refinement | Multiplicity, discontinuities, unresolved roots, warp multiplication |
| B | LOW without providers | Staged state/journal capacity, ordered execution, invalidation/publication | Broad transaction and continuation contract |

Existing rigid rotation uses 10,000-tick (0.01 s) full RK4 steps, a one-million-step cap, and a constant-time spherical torque-free special case. General cost is O(ceil(segment age / 0.01 s)), subject to existing limits. Repeated root queries could repeat that work. Do not cite the old ~12 ns translation-only result as the cost of a coherent event evaluator. A fixed-size result is plausibly a few hundred bytes with provenance; its exact layout/copy cost must be measured in implementation, not invented here.

Make evaluation explicit per requested sample/event, not automatic per microtick. If a future owner requests k evaluations per event and r events per simulated second at warp W, work scales roughly with W*r*k and model evaluation cost; it must budget that separately. Rendering remains per display frame and has no dependency on these samples. No performance optimization, cache or new rebase policy belongs in this candidate.

## Ranked candidates

1. **WINNER: E — pure coherent spacecraft motion evaluation at a supplied exact physical-event epoch.** Small, useful physical consumer with an independently provable contract.
2. **RUNNER-UP: A — provider-composed certified event-local evaluation.** Correct larger direction; begin with E rather than general certification.
3. **D — typed event-local contact certificate.** Useful later after event/body evaluation and a provable root relation.
4. **B — private execution frontier.** Needed later, not a useful empty shell now.
5. **C — physical root discovery.** Useful later or under a separately authorized prediction-only contract; not yet a complete bridge to executable contact.

Ranks 3–5 are readiness ranks for this next step, not a mandated future implementation sequence. B and C may have independent bounded designs after their consuming contracts exist. Rejected candidates are rejected as the next implementation, not permanently retired.

## RED TEAM A — Architecture

Strongest objection: E cannot by itself generate a contact-ready state; body/terrain evaluation and root proof are still missing. **Survived with explicit scope:** it produces the missing spacecraft contribution and demonstrates a physical consumer of exact epochs; it does not claim contact readiness. Existing M14.2 pure canonical evaluation is direct evidence that evaluation need not own a private execution frontier. A general certified state proposal was narrowed once to E; no universal certificate or execution shell is retained.

Additional attack: a stored SimulationStateView is not a frozen snapshot. **Required implementation bar:** capture immutable segment values and bind owner/revision; never retain the live view as evidence that nothing changed. This is part of the evaluation boundary, not a new physical authority.

## RED TEAM B — Exactness

Irrational roots: unsupported, no conversion. Fractions on either side of a canonical boundary: exact comparison/range checks before FP64 arithmetic. Negative absolute epochs: allowed when the segment-relative request is valid; requests before either contributing segment fail consistently with coherent-motion semantics. Very large/small fractions: explicit capacity/duration limits and numerical error proof. Same E after a mutation: old result stale. Terrain/body rotation: result must not claim body-fixed contact coherence. Angular velocity and frame transforms: preserve body-space angular velocity and inertial-root translation; no camera/body-fixed relabeling. **Survived.**

Strongest unresolved later issue: a certified contact relation at a non-rational root and the subsequent remainder integration are not solved by E. They remain explicit deferred responsibilities, not acceptance assumptions for this evaluator.

## RED TEAM C — Future generality

The result describes spacecraft motion, not a terrain event. It can supply values for future cutoff, timed burns, staging, exhaustion, impacts or docking without inventing their discovery/policy/state now. It does not claim to supply future propellant or atmosphere state. A multi-contact solver may consume a different local numerical representation subordinate to the same authority. No event type is forced to carry a contact receipt. **Survived.**

These reviews were performed by the synthesis lead; no independent-agent review or runtime proof is claimed in this research ticket.

## Decision and proposed accomplishment

**Lead judgment: PASS.** Confidence: HIGH on dependency ordering and authority separation; MEDIUM on implementation effort until the fractional rotation tail and numerical bars are qualified.

**Recommended next responsibility / proposed accomplishment title:**

**Evaluate coherent spacecraft motion at exact physical-event epochs**

It precedes the alternatives because none of them supplies the currently missing physical evaluation of spacecraft at E, whereas this result can be proved without any of them. It is the first provider contribution to A, not a claim that all certified event state now exists.

Explicitly deferred: celestial/body event-time evaluation, terrain root/witness certification, irrational root representation, discovery/CCD, fractional mutation, private frontier/debt, multi-event execution, continuation/rebase policy, atomic canonical interval publication, grounding/landing/pads/solver/propulsion and all renderer changes.

The bounded [Active Development prompt](active-development-prompt.md) is prepared for Project Control only. It has not been started.

## Evidence provenance and reproduction

- NovaCore source and tests are available at the recorded current main; no new validation claims are made. Inspect the source anchors above and PhysicalEventEpochTests, SpacecraftTranslationTests, ContactGenerationTests, ContactResponseTests and IsolatedContactResponseTests.
- Installed KSA: Production `2026.9.7.5402`, revision `487c3f340de24c6a81037120b6d1129c045c5400`; SHA-256 `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`. Retained decompilation AssemblyInfo matches that version. No authored source checkout was claimed.
- Current Brutal.Concurrency.dll: `FEDE16A5AC46D50E2E2E8D19687FF8835FDB75BA6630DB20C11E3969C7DF5B65`; Planet.Core.dll: `2823B299051B91252453ACB4F1FFF5543B1F5473BC6F42C8D51D4958D3159ECE`; BepuPhysics.dll: `77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`.
- To repeat framework structural checks, open those PE files with System.Reflection.Metadata/PEReader and enumerate the named types/methods; no target assembly execution is required. This establishes signatures and references, not an independent method-body proof. Current KSA call sites supply the stronger ownership evidence.
- Official Discord searches: `in:live-changelog substep` and `in:live-changelog Bepu`, plus the visible current changelog. Direct decisive message links are retained above; no raw message archive was created.
- [Source fingerprints](source-fingerprints.json) pin the fifteen principal source files inspected. All matched at final verification.
- No source, tests, assets, Git refs or existing evidence were modified. New output consists only of this bounded external assessment, its proposed prompt and the source fingerprint manifest. No bulk diagnostics or comparison trees were created. Final `git status --short` and staged diff are empty; `git diff --check` passes. HEAD/main/origin/main and the M14.6 tag target remain as recorded above.

STOP FOR PROJECT CONTROL.
