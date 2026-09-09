# M14.2 — Independent spacecraft translational authority

**Accepted candidate judgment: PASS. Final banking judgment: ESCALATE TO PROJECT CONTROL.**

**M14.2 BANKING BLOCKED — PRODUCTION ACCEPTED, UNBANKED**

Accomplishment: **NovaCore M14.2: Establish independent spacecraft translational authority**.
Proposed tag (not created): `m14.2-spacecraft-translational-authority`. Manual visual acceptance is
**NOT REQUIRED**, because no visible scene or route behavior was introduced.

## Baseline and scope

Verified clean `main` at `5b9b02ab1d2ba0e903809510908e4819984d4de2` before editing.
HEAD, main, local `origin/main`, and the peeled annotated tag
`m14.1-canonical-surface-point-queries` all resolved to that commit. Tag object:
`3ccb998031e15cb1898394589c27244d55e06e24`. Created the requested neutral branch
`codex/spacecraft-translation`. M14.1 stays banked. Project Control subsequently accepted and authorized banking this exact production/test candidate as M14.2; M14 remains open.

Existing celestial trajectories remain genuine celestial translation authority.
The existing spacecraft store already owned identity, frame references, attitude,
angular velocity, inertia and torque, with guidance/SAS outside the pure evaluator.
It lacked independent spacecraft COM position, linear velocity, mass and force
advancement. This candidate fills that gap in the same store and transaction owner.

No contact, grounded state, pad collision, liftoff/landing, gravity redesign, Bepu,
propulsion, atmosphere, native/render/launcher change or celestial trajectory mutation.

## Accepted candidate proof and architecture decision

WHAT → spacecraft-owned linear dynamics. HOW → read current state, transaction,
frame and KSA ownership; independent physical/time/scope reviews. BAR → one owner,
exact-time force boundaries, FP64 physical state, no partial commit. PAYOFF → real
motion and mass for future contact/propulsion. WORK → bounded store/evaluator/event
extension. VERIFY → adversarial source review and permanent tests. JUDGE → proceed
after the ten gates below. IMPLEMENT → one candidate, one bounded review correction.
VALIDATE → Debug/Release builds, numerical/replay/frame/atomicity tests and warm
timing. RETURN → unbanked for Project Control.

| Cheap-proof gate | Decision before implementation |
|---|---|
| State owner | Existing `SpacecraftStateStore`, indexed by existing spacecraft ID; no second database. |
| Coordinate representation | FP64 COM position in the existing sole inertial ECL root. |
| Velocity frame | Explicit root-inertial metres/second, not ambiguous world or body-relative velocity. |
| Mass owner | Copied immutable positive finite properties per spacecraft slot; constant mass only. |
| Force boundary | Closed canonical event carries persistent net root-force intent; no external mutable state. |
| Integrator | Analytical constant-force segment, not Euler or unnecessary RK4/subdivision. |
| Mutation/publication | Pure evaluation → current canonical/revision validation → existing engine commit → history. |
| Rotation coherence | Same ID; initial common epoch; later stored segment epochs may differ; evaluate at one instant. |
| Contact compatibility | Motion exposes position/velocity, orientation/angular velocity, mass/inertia; M14.1 and frame math remain consumers. |
| Project Control choice | No unresolved owner/model choice; constant physical force is explicitly allowed by the ticket. |

The simpler frame layout uses the existing root directly as carrier and the body
frame at the COM beneath it. No additional translating carrier or celestial-owned
carrier is overwritten. Parent/body-centered and rotating views remain derived.

## State, force, integration and time contracts

The durable [translation contract](../../spacecraft-translation.md) owns exact
semantics. In brief:

- Immutable mass in kg; segment contains ID, root ID, exact epoch, COM position,
  linear velocity and applied net root force. No redundant acceleration/momentum.
- `a = F/m`, `v = v0 + a*dt`, `p = p0 + v0*dt + (a*dt)*(0.5*dt)`; exact tick
  difference precedes FP64 conversion. Pure evaluation is constant work and never
  rebases state. Invalid/nonfinite/overflow cases fail without clamps.
- Net force persists until an explicit canonical replacement. At T, old force
  advances through T, then new force becomes active with continuous p/v.
- Same-force events are processed without changing state or epoch; observation or
  repeated intent frequency therefore does not change rounding order.
- Commit reconstructs the proposal from actual pending payload/current state.
  Forged replacement position/velocity/root/ID, stripped payload, inconsistent
  wrapper, wrong time, stale state/timeline and exhausted history reject.
- History capacity and revision/consume checks precede mutation. History records
  exact before/after segments and revisions; no per-frame logs are needed.
- `SpacecraftMotion` is complete at one time and revision. Live-backed state views
  must be freshly obtained in the existing single-writer phase. No asynchronous
  snapshot guarantee is claimed. Requests before either current segment epoch
  fail; newest torque/force cannot reconstruct earlier history backward.

No gravity contributor was needed to prove dynamics. Constant force on known mass
has an exact oracle. State-dependent gravity, body-following thrust, deterministic
combination of multiple contributors and atomic combined wrench response require
later force/response contracts, without replacing the spacecraft owner.

## Reference frames and rotating-body compatibility

Translating craft admission checks one ECL root, matching root/carrier, direct body
parent and valid initial craft/epoch. Frame extraction writes only the existing
craft body slot. Orientation is body-to-root; body origin is COM; root p/v is
included once. Existing non-translating spacecraft frame behavior is retained.

For a body-fixed point, existing transport computes origin velocity plus
`omega × rotatedOffset`; the inverse subtracts both origin and rotational motion.
The tests use a moving/rotating body at a 1.5e11 m root position and demonstrate
round-trip p/v and correct subtraction of ground velocity. No spin is silently
added or removed from stored craft momentum. Observation-frame conversion is not
authoritative reparenting, which this candidate does not implement.

M14.1 continues to own point, normal, identity and readiness. Its point query is
not a shape manifold or contact solver. Existing finite-difference planetary spin
and physical-normal qualification remain their documented contracts, not newly
claimed exact analytic derivatives or universal normal coverage.

## KSA / BRUTAL comparison and concise history

Current installed KSA assembly identity was rechecked: version `2026.9.7.5402`,
product revision `487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
Relevant methods were refreshed from cached decompilation of the installed
production assembly at `E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA`.
This is local installed-production implementation evidence, not an authored source
checkout; no proprietary implementation or content was copied into NovaCore.

| Current responsibility | KSA evidence | Decision |
|---|---|---|
| Game-owned vehicle kinematics | `KinematicStates.cs:9`, `Vehicle.cs:909`, `PhysicsStates.cs:10` | ADOPT one game/simulation state owner; renderer consumes it. |
| Mass and inverse mass | `VehicleProperties.cs:99` | ADAPT to immutable positive mass; fuel/variable mass are not required now. |
| Force/torque and integration | `PhysicsStates.cs:993` and `:1042` | ADOPT intent/evaluation separation; ADAPT to explicit net root force. |
| Updated environment during Verlet | `PhysicsStates.cs:1042` | INTENTIONALLY DIFFER: the narrower constant-force model has a closed-form solution, qualified against an analytical oracle. No state-dependent gravity equivalence claimed. |
| Physics-ready result before rendering | `VehicleUpdateTask.cs:141`, `:176` | ADOPT derived presentation after complete physical state; ADAPT to NovaCore exact-time transactions. |
| Solver import/readback | `ConstraintSim.cs:210`, `:373` | Keep Bepu working representation distinct from game authority; no solver adopted. |
| Shared framework | BRUTAL numerics/concurrency/Vulkan; Planet.Core time/transform utilities; Planet.Render.Core frame scheduling | Reuse engine mechanisms; vehicle force/physics policy belongs to Simulation, not renderer or generic framework. |

Official history was verified read-only in the authenticated desktop browser during
the immediately preceding accepted architecture gauntlet and reused narrowly here;
current methods were rechecked rather than repeating broad research:

1. [Revision 4659, 18 June 2026](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885):
   bubble/terrain transitions and solver readback could disturb resting high-precision
   state. Preserve the correct vehicle authority rather than unconditionally replacing
   it with local solver state. Current `ConstraintSim.cs:373` corroborates that boundary.
2. [Revision 4979, 22 July 2026](https://discord.com/channels/1260011486735241329/1260112103134724146/1529376156976021616):
   stale position/environment during Verlet force evaluation injected orbital energy
   at high warp. Refresh environment at the proper derivative evaluation. Current
   `PhysicsStates.cs:1042` retains that responsibility. NovaCore's constant-force model
   intentionally avoids pretending a frozen environmental force is continuous gravity.
3. [Revision 5174, 5 August 2026](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902):
   active engines could be incorrectly treated as landed/resting; current
   `PhysicsBubble.cs:1077` accounts for active actuators. This constrains future resting
   policy, but supplies no reason to implement that policy in this translation ticket.

KSA's float-local solver and contact-specific force approximations are not imported.
NovaCore retains FP64 and exact-time physical authority. Historical evidence explains
corrections; it is not proof of an original KSA milestone sequence.

## Implementation files

New production contracts are under `src/NovaCore.Simulation/Spacecraft/Translation/`:

- `SpacecraftTranslationState.cs`: state, immutable mass, force intent, statuses/results.
- `SpacecraftTranslationEvaluator.cs`: pure analytical linear evaluation.
- `SpacecraftMotionEvaluator.cs`: complete same-time angular/linear derived state.
- `SpacecraftForceTransaction.cs`: pure canonical force proposal and history record.

Existing files extended: spacecraft store/view/status and frame evaluator; timeline
kind/header/payload/request admission; simulation event dispatch, transaction payload,
state commit and transaction engine. The permanent test file is
`tests/NovaCore.Simulation.Tests/SpacecraftTranslationTests.cs`, registered by the
existing test executable. Current-state, handoff and architecture docs point to the
new contract. No native, Graphics, shader, asset, launcher or sample file changed.

## Numerical validation

Numerical bars were selected before implementation from the constant-force oracle.
For finite normal intermediates, with `u=2^-53` and `gamma(n)=n*u/(1-n*u)`, the
conservative component position bar is
`gamma(16)*(|p0|+|v0*dt|+0.5*|a|*dt²)` and velocity bar is
`gamma(8)*(|v0|+|a*dt|)`. The allowance includes oracle conversion rounding; it is
scale-derived, not an unexplained global epsilon. Dyadic vector fixtures require
exact equality. No cross-platform/cross-JIT universal bitwise claim is made.

| Measurement, both Debug and Release | Result |
|---|---|
| Decimal X-component position oracle, local and 1.5e11 m origins | Maximum absolute error `2.2737367544323206e-13 m` |
| Decimal X-component velocity oracle | Maximum absolute error `0 m/s` |
| Full-vector dyadic constant/zero force and piecewise-force checkpoints | Exact expected values |
| Bounded direct segment evaluation through 86,400 s | All declared scale-derived bars pass; no timestep accumulation |
| Repeated same-state/time evaluation | Zero bit difference |
| Identical force schedule, 1/30/60/144/1000 observation partitions | Identical final state and complete motion |
| Exact host-duration conversion, 137 partitions at rates 1/2/8 | Identical segment state and final 8 s clock |
| Large configured warp with explicit target evaluation | Same result; not a high-warp contact test |
| Late epoch 1e16 ticks plus 1 tick | Same result as the equivalent one-microsecond duration at zero epoch |
| Moving/rotating frame round-trip position | `9.313225746154785e-10 m` |
| Moving/rotating frame round-trip velocity | `1.831802048925619e-13 m/s` |
| Derived frame bars at tested magnitude | `2.44140625e-4 m`, `1.7919430458545685e-8 m/s` |

The decimal maximum is for the retained X-component oracle matrix, not a universal
3-D trajectory error bound. Full-vector dyadic checkpoints and frame tests separately
cover all axes. The 24-hour interval is bounded evidence, not indefinite stability
or arbitrary-force accuracy. Pure segment evaluation has no successive integration
drift; FP64 rounding remains. Extreme complete-motion duration still obeys existing
rotation limits and timeline event budgets.

## Performance

Release, warmed on the current host, 101 samples. Each sample averages 1,000 complete
batches; values are batch-average latency percentiles, not individual-call tail
latency or end-to-end game frame timing.

| Craft count | Median batch | P95 batch | P99 batch | Median per craft | Allocated bytes |
|---|---:|---:|---:|---:|---:|
| 1 | 12.00 ns | 12.10 ns | 12.10 ns | 12.00 ns | 0 |
| 256 | 3,071.70 ns | 3,082.20 ns | 3,110.10 ns | 12.00 ns | 0 |

The batch is evaluation of 256 immutable states, not a fleet/contact/frame-scheduling
benchmark. Separate 100,000-iteration pure-linear plus coherent-motion allocation
validation reports zero bytes. No cache/pool/optimization mechanism was introduced.
Future forces, contacts and angular integration have their own cost; these timings
are not their budget.

An initial 20,000-call warm-up produced tiered-JIT transition contamination (single
171.8 ns versus batch 7.67 ns/craft and unstable tails). It was rejected as warmed
evidence. The permanent harness now allows 500 ms of bounded active warm-up per case
before samples. The retained performance file contains that corrected run; no
production optimization was made to obtain it.

## Validation results and reproduction

| Surface | Debug | Release |
|---|---|---|
| Full solution managed build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Complete Simulation suite | 33 registered groups PASS | 33 registered groups PASS |
| ReferenceFrames suite | 11 groups PASS | 11 groups PASS |
| Precision executable | PASS | PASS |
| Selected M14.1 Graphics headless query/stale-snapshot tests | 2 pass, 0 fail, 0 skip | 2 pass, 0 fail, 0 skip |

Simulation output has 34 `PASS`-prefixed lines because the new test group also prints
its internal summary. The registered suite count is 33, not 34. The selected Graphics
run excludes 91 unrelated tests explicitly. No full Vulkan/window gauntlet or physical
Florida run was performed or claimed; production rendering, deployment and route
semantics are unchanged. Existing spacecraft/torque/SAS/frame-extraction regression
groups passed. Shader/native builds are not required for this managed-only change.

From repository root:

```powershell
dotnet build NovaCore.sln -c Debug --no-restore
dotnet build NovaCore.sln -c Release --no-restore
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --translation-performance
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Precision.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --category=headless '--test=Physical surface-point'
```

Repeat the final three commands with Debug for parity. `--translation-only` runs
only the focused new numerical/contract tests. Accepted measurements are retained here; final rerun summaries are in
`closeout-validation.json`. Final validation failed before evidence retirement; existing transcripts remain pending closeout.
Reproduction requires normal repository SDK/dependencies/assets,
not this evidence folder. `git diff --check` passes; only normal line-ending
conversion notices appeared in Git diagnostics.

## VERIFY A — physical authority

Strongest attack found that new complete motion could evaluate a later torque
segment backward while translation still permitted the requested time. Corrected
with a maximum-of-current-epochs gate in complete motion and translated frame
extraction. Regression uses translation epoch0, torque change4s, rejects2s/default
output and accepts4s. Root-carrier ownership, mass immutability, force continuity
and existing body transport survived review. **PASS; no remaining blocker.**

## VERIFY B — time / transaction

Canonical force is reconstructed from current pending payload and state; rejection
precedes all mutation. Tests cover capacity, stale state after torque, stale
timeline, early commit, same-time force/torque/force ordering, unchanged-force
segments, real host-duration partitions and complete current-time publication.
Outer metadata consistency was hardened. Strongest remaining precondition is the
existing single-writer, fresh live-backed view contract; asynchronous use is not
supported. Same-time events remain separately atomic. **PASS; no normal-path blocker.**

## VERIFY C — KSA / scope

Strongest attack found a stripped typed force proposal could enter generic marker
handling and consume the event. Corrected by rejecting force-kind wrappers without
their typed proposal before dispatch. Regression verifies unchanged state, pending
event/revisions/history and successful subsequent canonical commit. KSA ownership
comparison supports the boundary, not adoption of its full integrator/solver/game
framework. **PASS; no remaining blocker.**

The bounded review revision corrected both reported defects and completed relevant
guards/tests. Compiler/test-harness corrections were ordinary implementation work;
no second architectural candidate or broader mechanism was attempted.

## Future contact / propulsion and manual acceptance

Future contact can consume complete motion, immutable mass/inertia, M14.1 physical
query/readiness and transported ground velocity. A real contact impulse or coupled
wrench must be modeled and committed through this authority, not by bypassing it.
No impulse-injection, contact solver or grounded state is implemented now.

Future propulsion can supply a physically modeled force, combined with other
contributors. Ordinary eventual liftoff should arise from force/support balance,
not a special launch velocity or teleport. State-dependent forces and coupled
linear/angular response need their own numerical/atomicity bar; this report does
not claim the net-force API already implements them.

Manual acceptance: **NOT REQUIRED** for this numerical, no-visible-change
accomplishment. Project Control accepted the production candidate and authorized M14.2 banking.

## Evidence lifecycle and Git

Final closeout stopped before staging, evidence retirement or banking. The Debug
solution build passed, then the existing clock-orchestration test failed at
`Program.cs:1331`: `preallocated orchestration allocates zero bytes`. This assertion
combines allocated-byte equality, Completed status and 1,000 processed events; the
failure output does not identify which conjunct failed. No measured allocation
amount or causal diagnosis was captured. Accepted production/test fingerprints all
still match. No production/test code was changed during closeout.

`closeout-validation.json` records this failed final run separately from the prior
accepted results above. The remaining final validation gates were not reached.
`closeout-classification.json` classifies all 36 incoming paths and records the
proposed retirement of ten build/test transcripts after their results are safely
consolidated. No files were deleted; those transcripts remain pending closeout.
No raw captures, videos or GPU readbacks were created. Budget remains 128 KiB.

No staging, commit, tag, push, main promotion or branch retirement occurred.
M14.1 remains the latest banked accomplishment. The requested next bounded action
is to classify the failing clock assertion and clear the final gate before banking.
Do not weaken the assertion or change accepted spacecraft authority speculatively.

The subsequent [failure investigation](orchestration-failure.md) decomposed the
failure without changing production: a full Release run reported 8,160 allocated
bytes, Completed, and exactly 1,000 processed events. The failure is intermittent;
passing repetitions do not clear it. Follow-up Debug/Release builds, 33 Simulation
groups, 11 ReferenceFrames groups, Precision and two selected M14.1 regressions
passed in both configurations. Exact allocation causality remains unidentified.
M14.2 remains unbanked; no corrective warmup, tolerance or production edit was made.

The final [allocation attribution](allocation-attribution.md) subsequently proved
CLR background-GC allocation-context accounting, with zero managed object
allocations in a positively reproduced failing window. The test-only measurement
boundary preserves original warmup and exact-zero semantics. Ten full-suite and
five isolated processes per configuration passed, as did all remaining technical
gates. Production remains unchanged. Manual disposal of the classified diagnostic
copies and superseded transcripts is the sole remaining closeout action; no
deletion retry or banking was performed.

M14 remains open. Next action returns to Project Control.
