# Bounded compound-contact coverage investigation

Current candidate follow-up: [formal selector/article qualification](formal-qualification/README.md)
passes the authorized engineering and manual gates, UNBANKED for Project Control.
The causal investigation below remains a historical result; its frozen-article disposition
and diagnostic identities apply to that snapshot, not the qualified candidate.

## Historical causal investigation

**PASS — CAUSAL POLICY DEFECT PROVEN.**

This judgment is limited to the demonstrated contact transition and NovaCore's unchanged
20 mm requirement. The engineering article remains frozen, blocked and unbanked. No
production correction or article acceptance is implied.

## What happened

With all physical inputs held fixed, changing one retained contact at interval 35 reduced
the tilted article's peak through interval 40 from **87.864237 mm to 14.454722 mm**.
The solver still received exactly four contacts. The default selection resumed immediately
at interval 36. A same-count alternative bus-contact selection reduced the peak to
42.171830 mm, still above the 20 mm gate.

All three fresh Debug processes used the same diagnostic binary. Their complete physical
states and contact/solver captures matched through interval 34. Their interval-35 initial
state, raw contacts and unmodified reduced set also matched. The only intervention was
contact slot 3. Geometry, mass/inertia, initial state, gravity, timestep, iterations,
material, damping, publication and host-credit schedule were unchanged.

## Identity and scope

HEAD/main/origin/main: `5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0`.
Branch: `codex/engineering-spacecraft-contact-article`.
The nine frozen article source/test identities are retained in the
[article identity manifest](../engineering-spacecraft-contact-article/current-draft-identity.json).
All 369 tracked/untracked source, test and dependency inputs captured at preflight remained
byte-identical. Their sorted path/hash digest is
`A484B49182D28E2C1B5ACA50D5A7B0AC4BE6A4E58F8BC92A4312B34FB2D285B2`.
The digest recipe and experimental binary identities are in [comparison.json](comparison.json).

The experiment uses the accepted `2.5.0-beta.29` BEPU binaries unchanged. Every build retains
the repository's dependency integrity checks. BepuPhysics SHA-256 remains
`77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`;
BepuUtilities remains `E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68`.
Source inspection uses their manifest-pinned commit
`f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`, rather than an unrelated current upstream branch.
SDK 10.0.303 and the host runtime inventory are retained with the comparison.

## Current contact ownership

| Stage | Owner and actual behavior |
|---|---|
| Child dispatch | BEPU `ConvexCompoundCollisionTask` dispatches box/slab child pairs; `ConvexCompoundContinuations` supplies original child IDs and parent offsets |
| Child manifolds | BEPU box collision detection creates convex manifolds; NovaCore's child callback permits all and only records diagnostics |
| Aggregation | BEPU `NonconvexReduction.Flush` converts populated child contacts to parent-A coordinates across one entire collidable pair |
| Selection | BEPU `ChooseMostDistinct` greedily selects at most four contacts using depth, spatial separation and normal distinctiveness |
| Final callback | NovaCore's ordinary generic callback passes the reduced manifold unchanged and supplies the existing material |
| Solver representation | BEPU `NarrowPhase.UpdateConstraintsForPair` directly copies the returned parent into one contact constraint; no second reduction follows |
| History/warm start | Native feature IDs match previous penetration impulses; unmatched old impulses are redistributed among unmatched new contacts; tangent impulses remain by slot |
| Solve | Four nonconvex dynamic/static contacts use one-body constraint type 10; each iteration processes penetration and tangent friction rows |

The four-contact capacity is per parent collidable pair, not per child. Child identities are
mixed into retained feature IDs using `rawFeature ^ ((childA << 8) ^ (childB << 16))`.
They do not reserve per-child coverage. Selection does not inspect velocity, timestep,
mass/inertia, prior impulses or warm-start history. Input ordering affects tied scores;
strict comparisons and native fast removal define a deterministic result for these inputs.

Primary source references:
[compound dispatch](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/CollisionTasks/ConvexCompoundCollisionTask.cs),
[aggregation and selection](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NonconvexReduction.cs),
[manifold capacity](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactManifold.cs#L167-L205),
[constraint update](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NarrowPhaseConstraintUpdate.cs),
[contact accessor](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactConstraintAccessor.cs),
[nonconvex solver](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactNonconvexCommon.cs).

## Why the right pod loses at interval 35

All twelve raw contacts are speculative: their depths describe small gaps at the pre-step
pose, before the large post-step overlap. The reducer chooses the deepest speculative
contact as its seed. It then maximizes minimum distinctiveness from selected contacts,
multiplying normal/spatial distinctiveness by `max(0.01, 1 + depth * depthScale)`.

Independent source replay using captured parent-frame FP32 contact values gives:

```text
maximumDistanceSquared ~= 10.2498750687 m^2
maximumDistance        ~=  3.2015426159 m
depthScale = 400 / maximumDistance ~= 124.93977356 /m
minimum depth weight starts at separation ~= 7.92381779 mm
```

All four right-pod gaps are 7.964-12.157 mm, so all receive the minimum **0.01 weight**.
The left and selected bus contacts receive approximately 0.205-0.899.

| Round | Selected child / raw feature | Winning score | Best remaining right score |
|---|---|---:|---:|
| Seed | Left / -4 | depth -0.000805921 m | depth at most -0.007964319 m |
| 2 | Left / -5 | 0.131494358 | 0.008292649 |
| 3 | Bus / -4 | 0.106817871 | 0.006097548 |
| 4 | Bus / -20 | 0.045028884 | 0.006097548 |

These are arithmetic-replay scores, not instrumented internal score fields. They reproduce
the exact parent order `-260, -261, -4, -20`. Every right candidate retains positive
distinctiveness: it loses ranking at capacity four rather than being eliminated as fully
redundant. The replay's small arithmetic-rounding uncertainty is immaterial to these margins.

The contacts are approaching very differently. A right outer point has pre-step normal
velocity about **-7.8751 m/s**, while the selected left seed is slightly separating.
That impending closure is absent from the reduction score. Selecting only the smaller
current gaps therefore loses coverage important during the upcoming fixed interval.

## Was equivalent physical coverage retained?

No equivalent set of unilateral normal constraints was retained. With normal approximately
(0,1,0), a contact's normal-velocity row over [vY, omegaX, omegaZ] is [1, -rZ, rX].
The retained set reaches only rX=+0.752111 m; right outer points reach approximately
+1.2515/+1.2519 m. Their normal rows cannot be a nonnegative normalized combination of
retained rows. The associated support lever arm is missing. A right-inner rear point
nearly duplicates another raw bus point, but that bus point is also discarded.

This is a normal-coverage argument, not a claim that friction cannot supply torque.
The experiment below establishes the causal effect within the actual coupled solver.

## Controlled counterfactual and same-count control

The predeclared sequence was **normal -> right -> bus**, one fresh process each, 40 exact
intervals plus the initial state. The matrix had no builds, retries or adaptive policy
changes. Only interval 35 was eligible for intervention. Every later step used the original
default selection again.

In both changed arms, slots 0-2 remain exact; slot 3 replaces bus raw feature -20. The
chosen omitted candidate maximizes `rawDepth - dot(v + omega cross offset, normal) * dt`
within the specified diagnostic child. This predictor only chooses the point. It does not
replace actual depth, change velocity or add bias. The callback installs original valid
raw normal/depth, exact native parent offset and native feature identity.

| Arm | Changed slot / original raw candidate | Solver feature set | Peak (m) | Peak step | 20 mm comparison |
|---|---|---|---:|---:|---|
| Normal | None | -260, -261, -4, -20 | 0.08786423715152591 | 35 | Fails |
| Right coverage | Right / -20 | -260, -261, -4, -532 | **0.014454722411638476** | 35 | Below ceiling in diagnostic window |
| Bus control | Bus / -21 | -260, -261, -4, -21 | 0.042171829502266056 | 35 | Fails |

All arms present one nonconvex constraint, type 10, with **four contacts**. This compares
selection at fixed solver strength, not a contact-count increase. The bus arm supplies
useful different spatial coverage, so it is not an inert/redundant negative control.
Its partial improvement supports selection influence; it does not establish a per-child
quota or the unique necessity of one particular right point among all possible sets.

The right replacement has parent offset approximately
(1.251866, -0.5058369, -0.34890294) m and actual depth **-0.009753902 m**. Its selection
prediction is +0.121500425 m, which is **not installed**. Native transport cross-checks
against unmodified parent contacts report exactly zero offset difference, with exact
normal/depth equality. The bus replacement keeps its original -0.010367690 m depth.

## Direct solver and impulse witness

Temporary cold-created `DefaultTimestepper.CollisionsDetected` and `ConstraintsSolved`
observers use BEPU's public `TryExtractSolverContactData`/`ISolverContactDataExtractor`.
The former reads the actual updated constraint before solving. It proves contact count,
type, order, offsets, normals and depths exactly match the returned parent manifold.
The latter copies resulting impulses. No live reference is retained or modified.
The implementation follows the pinned
[official solver enumeration example](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/Demos/Demos/SolverContactEnumerationDemo.cs).

At interval 35, **all four presolve normal impulses and all eight tangent components are
zero in every arm**. These are actual nonconvex extractor values. Thus different warm-start
remapping does not explain the decisive-step improvement. Native history remains enabled;
later history differences are consequences of the intervention.

| Arm | Post-solve normal impulses, slots 0-3 (N s) | Post-step angular velocity xyz (rad/s) |
|---|---|---|
| Normal | 0, 878.5843, 0, 1731.5895 | 1.6046604, -0.07926568, -2.172651 |
| Right | 460.1955, 642.8384, 28.065052, **2193.5308** | 0.24856824, -0.001099075, -0.44686627 |
| Bus | 0, 0, 1296.2758, 2005.8849 | -0.69330454, 0.00070255646, -1.6154604 |

The replacement changes a solver row and receives substantial support impulse. The changed
normal lever arm and resulting coupled response suppress the angular penetration transient.
Right coverage reduces the observed peak by **83.5488%**; the bus selection reduces it by
52.0034%. No iteration, force, material or timestep change is needed for this diagnostic result.

## Right-pod history and support limits

Signed right-pod support distances (m), positive above the slab:

| Endpoint | Normal | Right coverage | Bus control |
|---|---:|---:|---:|
| 34 | +0.007964268 | +0.007964268 | +0.007964268 |
| 35 | -0.087864236 | -0.014454723 | -0.040680538 |
| 36 | -0.057934268 | -0.008672837 | -0.019222882 |
| 37 | -0.024031546 | -0.003415082 | -0.000420824 |
| 38 | +0.006902334 | -0.001314224 | +0.003347825 |
| 39 | +0.029586353 | -0.000588131 | +0.004663668 |
| 40 | +0.047418734 | -0.000376439 | +0.002880597 |

At endpoint 40 the right arm's deepest article point is -0.455833 mm and linear speed
0.0065742 m/s. This shows early recovery toward the existing support regime. It does not
prove final support, drift stability, 600/600 supported intervals or a 1,200-step pass.
The original publication/accounting checks remain exact across the diagnostic's 40 steps.

## Cause, ownership and payoff

**Cause:** default depth-weighted compound reduction discards valid rapidly closing
right-side normal coverage. The actual solver lacks that contact row. Retaining it, at the
same contact count and identical pre-solve state, removes the excessive transient in the
bounded window. This completes the requested causal chain.

**Ownership D:** upstream/default BEPU behavior is used as designed but is insufficient for
this NovaCore requirement. NovaCore's current callbacks do not introduce a reducer. No API
misuse, child-construction defect or banked M14.19/M14.20 defect was found. This is not a
claim that BEPU contains an implementation bug or that reduction is the sole contributor
to all compound penetration.

**Production fix candidate, recommendation only:** a bounded reusable contact-selection
boundary before constraint creation that retains imminent normal/spatial coverage within
the existing four-contact budget. It can use the current callback interface and preallocated
child-contact storage, preserving original depths/normals and deterministic feature/tie
semantics. Its general admission, selection and lifecycle contract needs Project Control
review and independent qualification. The diagnostic's interval number, child choice and
slot number are not a production algorithm; neither a per-child quota nor this predictor
is qualified for general use.

Potential payoff extends to multi-child spacecraft, future landing assemblies and other
dynamic convex compounds undergoing angular contact transitions. That is a responsibility
class inference from the generic reduction path, not validation of those future fixtures.
No general vehicle framework, KSA research, geometry, Blender work or dependency change
was started. Existing KSA current-source/history provenance remains separate and unchanged.

## Red-team judgment and stopping boundary

Two independent read-only reviews checked selection arithmetic and solver/experiment
semantics. Both conclude **PASS — CAUSAL POLICY DEFECT PROVEN** for this transition.

The strongest alternative, warm-start reassignment, is excluded at the intervention by
zero normal/tangent impulses in all arms. Earlier trajectory, raw input and selected-set
equality exclude changed preparation. Direct solver extraction excludes changing only
reporting. Fixed count/type excludes additional solver strength. The non-inert bus control
and finite 40-step horizon limit generality; they do not invalidate the demonstrated cause.

No second experiment, production correction or article qualification follows. The original
article remains blocked until an accepted permanent responsibility is separately designed,
implemented and qualified. Its 20 mm threshold remains unchanged.

## Reproduction, evidence and Git

This directory has a 128 KiB evidence budget. Retain this report, `comparison.json`,
`reproduce.ps1` and the three source/target templates under `reproduction/`. The JSON retains
all 0-40 trajectories, decisive 34-36 raw/reduced/solver records, exact original-output
hashes, binary identities and comparison checks. No profiler or broad campaign was used.

The guarded reproducer creates a new bounded scratch directory, clones only three files
for instrumentation and injects the diagnostic sources at build time. SDK
`CustomAfterMicrosoftCommonTargets` and separate `--artifacts-path` outputs keep the canonical
source/test files and ordinary build outputs untouched. It runs exactly normal/right/bus,
without rebuilding between arms. It neither restores nor deletes canonical files because
it never edits them. The packaged script's syntax is checked; its equivalent constituent
build/run operations produced this evidence, and no extra packaged rerun was performed.

One preparatory diagnostic build exposed temporary item-substitution and type-alias errors;
these were corrected only in scratch before any physics process started. The next build
passed with zero warnings/errors. Exactly three physics processes were executed, all
completed their 40 intervals, and none was retried.

Production paths changed by this investigation: **none**. Permanent test paths changed:
**none**. All nine article draft fingerprints and all 369 preflight input hashes match.
The banked `SimulationTransactionEngine.PersistentContact.cs` and
`SimulationTransactionEngine.ContactServicing.cs` remain untouched. Historical tags and the
separate staging-build-blocker file remain unchanged. Nothing was staged, committed,
pushed, merged, tagged or banked. Final verification and disposable disposition are recorded
in `closeout.json`.

The seven pre-existing files under `build/engineering-article-tilt-probe` were left in place;
no deletion or workaround was attempted. New scratch was confined to
`build/compound-coverage-investigation`. Its reviewed 139 files / 21,973,615 bytes are
disposable after verification of the concise reproduction package. One attempt to delete
only this newly created directory was rejected before execution by automatic approval
review as "blocked by policy". No retry or workaround followed. Both scratch directories
remain; together they contain 146 files / 22,372,200 bytes. This does not change the causal
judgment or constitute article acceptance.

**PASS — CAUSAL POLICY DEFECT PROVEN. STOP FOR PROJECT CONTROL. UNBANKED.**
