# Staged finite-body BEPU contact — qualification result

2026-09-13 qualification: **READY FOR PROJECT CONTROL ACCEPTANCE**, unbanked and
unnumbered at measurement time. Project Control subsequently **PASS / ACCEPTED**
this result as **M14.18 — Staged Finite-Body BEPU Contact**. Measurements below are
unchanged; the [final review](m14.18-banking-review.md) records the banking plan.

## Baseline / branch

At qualification: `codex/bepu-local-contact-staging-v2`; HEAD/main/origin/main were
`b78f5b8c763b8d118b93f48fc4f87e78d7b6b4b2` (NovaCore: restore qualified BEPU manifest identity).
That commit contains only manifest restoration and its two evidence files, no solver draft.
The six incoming candidate hashes were verified before this follow-up.
[Current/prior fingerprints and results](staging-qualification.json) identify the candidate.
SDK 10.0.303, runtime 10.0.12, Windows x64, ALBATRONIC007.

## WHAT HAPPENED / WHY IT MATTERS

Prior corrected speculative reach gave centered penetration 4.134361 mm but tilted
53.39229 mm. Both eventually supported the body; transient quality still failed.
Temporary diagnostics reproduced both peaks and proved that coplanar mesh internal-edge
filtering suppressed an entire speculative child manifold containing needed corners.
One matched static-slab control retained them: tilted peak became 4.164368 mm.

The single production correction changes only this qualification surface representation.
Both fixtures and all resumed gates pass. This qualifies finite-body contact against
the authored convex planar static surface, **not arbitrary triangle-mesh terrain contact**.
BEPU itself is unchanged; this is not a claim of a BEPU bug.

## Explicit penetration bar

**20 mm = 2% of the qualification box minimum dimension (1 m).** This is now an explicit
Project Control fixture requirement, not a universal terrain/spacecraft/landing-gear
tolerance. Tests name the same unchanged .020 m threshold and independently check endpoint
corner geometry as well as pre-solve manifold depth.

## Diagnostic trace / proven causal winner

[Witness](tilted-impact-witness.json) retains exact ticks/float dt, pre/post COM pose,
linear/angular velocity, lowest support height/velocity, activity, actual/max speculative
margin, feature/depth/normal/normal velocity, and the decisive child/parent manifolds.
Disposable source-linked Debug diagnostics were separate from canonical code and cost tests.

The tilted box starts with 1.268140 m clearance, zero velocities and .25 rad Z tilt.
First contact is step 31 while the low edge is still 1.015 mm above the plane. Its impulse
induces about -3.22 rad/s Z rotation; the opposite edge approaches faster than the COM.

Original step 35: 566666 -> 583333 ticks, dt .016667 s. Actual speculative margin is
.13166395 m (cap .52 m). The threatened corner is .041784 m above the plane and moving
down at about 6.93 m/s. Child 0 supplies three face contacts, first feature `32768`.
Child 1 supplies four contacts, including both far corners at depths -.04178971 and
-.041799515 m, but first feature `6` lacks the face flag. Its deepest representative point
lies on the seam: (-.5002397,0,.5002443), normal (-.019227935,.99963033,-.019227935).

The pinned [MeshReduction](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/MeshReduction.cs)
(`ComputeMeshSpaceContact`, `ShouldBlockNormal`, `TryApplyBlockToTriangle`, `ReduceManifolds`)
explains deletion: diagonal distance 3.25269e-6 m is inside .128 m threshold; normal
infringement .027192406 exceeds 1e-6. Neighboring face manifold does not block back;
`ForceDeletionOnBlock` removes the entire speculative child. The parent exactly retains
child 0 features 0,5,2. [NonconvexReduction.Flush](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NonconvexReduction.cs)
forwards that sole remaining child. This is **not** greedy four-point selection preferring
seam points over corners.

Without the removed constraint, the far corner moves +.041784 -> -.053392857 m in one
step (95.177 mm). X roll reaches 2.165 rad/s following asymmetric support. The next
manifold reports .05339229 m at step 36. Once penetrating corners are admitted, friction
and recovery settle the body. Centered contact preserves its support footprint and has
only small compliant overlap. This explains both peak disparity and eventual support.

## Cause candidates tested

Classification concerns the primary correction target. No parameter sweep followed the
decisive witness; no unused experiment was run merely to label another knob.

| Candidate | Judgment | Evidence |
|---|---|---|
| A Discrete travel | REJECTED as primary cause | 95.177 mm corner descent is observed, but unchanged exact dt arrests it with the slab. |
| B Speculative reach | REJECTED | Required points already exist in child 1 within reach, then are removed. |
| C Angular sweep | REJECTED as correction target; contributing motion | Corner speed ~-6.93 m/s vs COM -3.36; same integration works with retained manifold. |
| D Recovery/spring | REJECTED as primary cause | Constraint absent at crucial step; unchanged material passes. Requested recovery at old peak ~1.95738 m/s, below cap 2. |
| E Iterations | REJECTED | Eight iterations cannot act on deleted contacts; same eight pass with slab. |
| F Substeps | REJECTED | Same one substep passes; pinned internal substeps do not regenerate collision manifolds each substep. |
| G Mesh seam | PASS AS CAUSE | Child/parent trace, exact deletion branch and matched static-surface intervention agree. |
| H Initial state | REJECTED | Positive clearance, zero velocities, unchanged mass/inertia/force/tilt in control. |

Requested recovery derives from pinned spring equations. Actual endpoint corner velocities
and pre-solve point velocities are observed. Individual accumulated impulses and a separate
recovery-only applied contribution are **UNAVAILABLE**; requested bias is not claimed as
actual solved velocity. No extra observer was added after cause resolution.

## HOW the correction works / why it is not a hack

Replace the two triangles with one static `Box(128,2,128)` centered at (0,-1,0).
Top remains local y=0 and extent 128 x 128 m. Thickness is two body heights below the
plane. One convex face retains the approaching corners without triangle-child filtering.
Original brief lines 151/620 allow a physical mesh/static surface; the follow-up explicitly
permits a proven planar-static representation correction. Equivalence is bounded to the
declared central top-face impacts; slab sides/bottom add geometry. General mesh/underground
equivalence and future Earth collision remain unqualified.

Before implementation the control predicted tilted ~4.2 mm and centered ~5.5 mm, no new
iterations/substeps and removal of mesh/tree storage/work. No old/new timing speedup is
claimed; final absolute cost/storage gates pass. No clamp, extra force, velocity reset,
damping hack, export correction, canonical-time change or threshold relaxation.

## Parameters / solver iterations and substeps before-after

| Parameter | Before | After |
|---|---|---|
| Static surface | Two upward coplanar triangles, 128 x 128 m | Slab, same top/extent, 2 m thickness below |
| Dynamic box / mass | 2 x 1 x 1 m / 1000 kg | Unchanged |
| Inertia kg m2 | (166.66666666666666,416.6666666666667,416.6666666666667) | Unchanged |
| Start | COM(0,2,0), v=omega=0; identity or Z .25 rad | Unchanged |
| Source force N | (0,-9810,0) | Unchanged |
| Friction / spring / recovery | .5 / 30 Hz, damping 1 / 2 m/s | Unchanged |
| Speculative cap | .52 m, actual velocity-derived | Unchanged |
| Velocity iterations / substeps | 8 / 1 | 8 / 1 |
| Time | 1,000,000 ticks/s, exact 16666/16667-tick intervals | Unchanged |

## Centered/tilted quality, support, repeatability

Debug and Release report identical results. Each 1200-step/20-second fixture compares
paired `SpacecraftMotion` at every endpoint with an independently initialized retained
world. All equality tests pass on this build/machine; no cross-platform replay claim.

| Fixture | Old peak mm | New manifold peak mm | Independent geometric peak mm | Final COM y m | Last-10s vertical drift m | Final speed m/s | Supported final steps |
|---|---:|---:|---:|---:|---:|---:|---:|
| Centered | 4.1343607 | 5.455954 | 5.455926961 | .49966177344322205 | 5.960464477539063e-8 | 9.169306265126838e-7 | 600/600 |
| Tilted | 53.39229 | 4.164368 | 4.164378501 | .49966174364089966 | 5.960464477539063e-8 | 9.405539856306131e-7 | 600/600 |

Centered increases **1.3215933 mm** because manifold construction/constraint layout changes;
it is not numerically unchanged. Both overlaps are small compliant contacts under 20 mm.
This removes tilted support loss rather than redistributing error through solver tuning.
Tilted peak angular speed remains 3.5320871536617684 rad/s. All states are finite, no
explosive rebound occurs, and settled multipoint support persists.

### Corrected tilted impact trace

Depth is pre-solve; height/velocity are post-integration. Full poses/individual points
are retained in JSON. Float probe and canonicalized FP64 corner tests differ slightly;
neither is rounded into a claim of exact identity.

| Step/role | Start -> end ticks | dt s | Points | Depth mm | Lowest endpoint height mm | Lowest-corner vy m/s | Omega Z rad/s |
|---|---|---:|---:|---:|---:|---:|---:|
| 30 before contact | 483333 -> 500000 | .016667 | 0 | 0 | 1.014948 | -4.9049993 | 0 |
| 31 first contact | 500000 -> 516666 | .016666 | 2 | 0, speculative | -3.323197 | -.19974852 | -3.219158 |
| 35 far-edge arrival | 566666 -> 583333 | .016667 | 4 | 0, speculative | -3.621042 | -2.7727487 | -1.2528434 |
| 36 deepest endpoint | 583333 -> 600000 | .016667 | 4 | 3.621056 | -4.164279 | -.03259883 | -.00522827 |
| 37 peak manifold/recovery | 600000 -> 616666 | .016666 | 4 | 4.164368 | -2.182663 | .11891326 | .041698784 |
| 38 continuing recovery | 616666 -> 633333 | .016667 | 4 | 2.182702 | -1.095414 | .06523624 | .019305855 |
| 1200 settled | 19983333 -> 20000000 | .016667 | 4 | .338197 | -.338256 | -9.834937e-7 | 8.945131e-9 |

## Production files / permanent tests

Entire unbanked production candidate: `SimulationTransactionEngine.ContactProof.cs`
(one read-only thread query), `LocalContactSource.cs` (source/config/admission),
`LocalContactCallbacks.cs` (material/source-force callbacks), `LocalContactWorld.cs`
(persistent world/frontier/paired export). Full paths/hashes are in the identity JSON.
**Only LocalContactWorld.cs production content changed in this follow-up**, constructing
and disposing the new surface. Other incoming production hashes match exactly.

New permanent `LocalContactStagingTests.cs` contains qualification/negative/free-flight/
allocation/storage/timing paths. Follow-up adds the explicit unchanged 20 mm constant,
independent eight-corner depth assertion and matching description/comments. `Program.cs`
keeps two focused routes and one appended group, unchanged this follow-up. No predecessor
test predicate or accepted dependency artifact changes. No diagnostic dependency.

## Architecture / qualification-only box

**ADAPT** the game-authority to subordinate retained-solver boundary. NovaCore owns FP64
source state, identities/revisions, mass/inertia, force, exact time and admission. BEPU
owns private float bodies/static/constraints/manifolds/warm-start caches. Rendering is
not involved. Existing KSA assessment remains ownership provenance, not proof of this
fixture's quality. The box is qualification-only, not a production spacecraft collider;
future spacecraft shape is not committed. Dependency/trust architecture remains closed.

## Source, identity/lifetime, exact time and event admission

Retain copied linear/angular segments, coherent motion, mass/inertia, engine, revisions
and source/end epochs. Immutable configuration reference protects reused numeric IDs;
no borrowed source view persists. World reference/generation/frontier/thread are validated
on step/read. One world/pool/body/static and cache set persists; none is rebuilt per step.

Endpoint n is `SourceStart + floor(n*1_000_000/60)` using checked Int128 arithmetic.
Only its exact next endpoint is admitted; float dt follows admission. No public fractional
ticks or second time graph. Pending events through admitted end (including equality),
changed canonical source/clock/revisions fail closed. No event is consumed or scheduled.

## Local FP64 -> FP32 frame mapping / precision / force

Subtract root position/velocity in FP64, rotate to fixed local axes, cast only bounded
values. Body omega maps body->root->local. Export reverses mapping using solved orientation
and exact elapsed time for the constant-velocity frame origin. No rebase/rotating frame.

Fixture tolerance 1 mm; exclusive local bound 1024 m reserves eight float spacings inside
that tolerance. Half-diagonal plus reach must fit. Point-speed bound is .5 m/.016667 s,
including angular sweep at half-diagonal. Root FP64 spacing must also resolve tolerance;
invalid/extreme float geometry or mass/inertia is rejected.

Measured rotated frame at 1e11 m root: import position error 2.3010547423046216e-7 m;
velocity error 5.691492973191235e-13 m/s. One-second free-flight reconstruction and velocity
error are both 0 for this fixture, not universally. Separate -1 m/s2 source yields
-1.0000001192092896 m/s at one second (bound 2e-6 m/s). Only captured force/mass is
integrated, with no hidden gravity. Nonzero source torque is explicitly unsupported;
torque-free gyroscopic angular integration remains active.

## Paired export / nonmutation / warm-start / invalidation

One endpoint returns FP64 position/velocity and canonical orientation/body omega plus
source/target/revisions/generation/frontier. Initial read retains original FP64 state.
It is staged data, not publication or complete solver-state serialization.

Both physical fixtures, free flight and failure controls preserve canonical segments,
mass, state/timeline revisions, clock, pending/processed counts and domain histories.
No canonical writable API is invoked. After actual BEPU advancement, failed export
invalidates the world; further step/read refuses. It never retries already-mutated caches.
The same retained world preserves constraints/manifolds across 1200 steps. This proves
continuation, not a quantified warm-start speedup or cross-platform cache serialization.

Negative matrix PASS: foreign engine; changed state/timeline/clock; changed/reused config;
pending event at target; invalid interval; default/foreign/prior-generation/stale receipt;
wrong-thread capture/step/disposal; disposed world; torque; inertia mismatch; invalid mass/
inertia; missing surface config; float geometry extremes; root precision; coordinate/speed
envelope; endpoint overflow; real post-step export failure and refused subsequent step/read.
Expected admission refusals use typed statuses, with no production diagnostic hooks.

## Builds / predecessor regressions

Full Debug solution **PASS, zero warnings/errors**. Full Release **PASS, zero warnings/errors**.
Staging focused suite, ReferenceFrames, Precision and BEPU dependency inspection each PASS
Debug/Release. Builds verify archive/manifest/license/DLL integrity; runtime inspection
checks exact pinned assembly binding. No dependency pin or version changed.

Fifteen predecessor focused routes each PASS Debug/Release: exact-event motion, translation,
contact generation, atomic response, isolated response, Earth observation, Florida proof,
root kinematics, certified response, pre-impact velocity, post-impact velocity, private
post-impact state, private propagation, post-impact coverage, continuation ownership.
Full route names/results are in JSON. No unrelated renderer/GPU campaign or full historical
Simulation performance campaign was run.

## Zero allocation / positive control / retained storage

Debug and Release: **0 managed bytes** across 1024 full Step+Read/export calls after 128
warm steps; every call succeeds. Existing checked 1 MiB temporary no-GC helper hard-fails
entry/exit failure. Independent byte[128] control detects **152 bytes** in both. No threshold
tolerance, subtraction or retry. Timing uses ordinary runtime without a no-GC region.

Both configurations: retained BufferPool **458752 bytes** + conservative managed
construction/warmup allocation upper bound **59344 bytes** = **518096 bytes**, below
**8388608 bytes**. Includes body/shape/static/solver plus adapter/source/fixture. Managed
component is an upper bound, not an exact retained-heap census. Native pool stays stable
during measured steps. This is not process working set.

## Performance / cold lifecycle costs

Three fresh normal-runtime Release processes, 128 warm + 1024 measured full step/export
each, no retries. Measured phase is warmed supported continuation, not initial impact.

| Run/PID | Median ms | P95 ms | P99 ms | Max ms | Cold creation ms | Disposal ms |
|---|---:|---:|---:|---:|---:|---:|
| 1/34780 | .0146 | .0150 | .0292 | .0502 | 29.7303 | 2.2174 |
| 2/23456 | .0133 | .0138 | .0245 | .0696 | 30.5348 | 2.5079 |
| 3/2812 | .0147 | .0163 | .0279 | .0705 | 30.4891 | 2.2978 |
| Aggregate 3072 samples | .0145 | .0151 | .0291 | .0705 | — | — |
| Required ceiling | .05 | .10 | .25 | .50 | Separate lifecycle | Separate lifecycle |

Creation includes first-use/JIT/world/body/static together; component costs not separated.
No cold cost is hidden in warmed percentiles. Worst sample indices are 6,748,269; retained
top-five lists show no same-index repeating worst pattern. No tail-cause claim is made.
The 150 FPS/~6.67 ms display budget is not allocated wholesale to this private solver.
Results do not predict many craft, complex colliders, Earth tiles or complete-frame cost.

## VERIFY / runtime-render impact

Read-only exact-BEPU reviewer confirms child deletion and permitted static-fixture scope.
Strongest alternative (iterations/reach) fails against already-generated then deleted
points plus unchanged-setting control. **PASS**.
Read-only authority reviewer confirms source/time/force/frontier/export/invalidation
unchanged. Strongest objection is overclaiming arbitrary mesh qualification; scope is
explicitly limited to central top-face slab impacts. **PASS**.

No renderer changes, display-frame multiplication, live canonical publication, live Earth
integration, production spacecraft collider, live warp scheduler or multicore dispatch.

## Evidence / reproduction / disposal

Retain this report, [identity/results](staging-qualification.json),
[decisive witness](tilted-impact-witness.json), [reproduction](tilted-impact-reproduction.md)
and earlier compact manifest-restoration provenance. Original failure and hashes are preserved.
Raw 150-step traces, temporary linked source/builds and benchmark scratch are disposable
after extraction. Project Control manually removed
`E:\NovaCore\build\bepu-tilted-impact` after qualification; the final banking review
independently verified that it is absent. The prior inventory was **48 files /
17630836 bytes**. Automatic approval review blocked the earlier automated attempt;
no deletion was retried during this review. Earlier deferred trust/dependency
scratch remains outside this cleanup scope.

## Qualification-time Git / judgment (historical)

No staging, commits, tags, merges, pushes or banking. Baseline refs unchanged;
only named candidate files/evidence differ. `git diff --check` PASS.

Lead judgment: **PASS**.

**STAGED FINITE-BODY BEPU CONTACT — READY FOR PROJECT CONTROL ACCEPTANCE**.

UNBANKED. STOP FOR PROJECT CONTROL.
