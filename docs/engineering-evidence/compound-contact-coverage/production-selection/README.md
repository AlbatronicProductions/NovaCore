# Four-contact compound selection: bounded implementation result

**Current follow-up:** [formal selector/article qualification](../formal-qualification/README.md)
passes the authorized allocation, storage, physics, schedules, performance, regression and
manual gates. Candidate remains UNBANKED for Project Control. The accepted
[precision-aware stability](precision-stability.md) result and the REVISE snapshot below
remain historical; their identities and reproduction scripts refer to those earlier states.

**REVISE. UNQUALIFIED / UNBANKED.** The selector fixes the demonstrated tilted penetration,
but independent review exposed unresolved established-support anchor instability. The one
permitted bounded correction was already used for a separate imminence/noise defect.
No second correction, performance campaign, banking or remaining article qualification followed.

The [accepted causal predecessor](../README.md) remains unchanged. This result does not
reclassify the original default-reduction defect or accept the engineering article.

## Baseline and identity

HEAD/main/origin/main and remote main: `5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0`.
Branch: `codex/engineering-spacecraft-contact-article`. Historical tags unchanged; index empty.
.NET SDK 10.0.303, Windows x64, .NET runtime 10.0.12.

[identity.json](identity.json) records seven changed/new source/test inputs, protected inputs,
and before/after SHA-256 tree digests. Of the 369 preflight inputs, 365 are unchanged and four
changed; three new inputs were added. The prior tree digest equals the accepted predecessor's
`A484B49182D28E2C1B5ACA50D5A7B0AC4BE6A4E58F8BC92A4312B34FB2D285B2`.
Current digest: `260C0BE8580496D7F3C2B10D75A8D1A5F9829C12380F7C8376F2AA0AD8BDECCA`.

The article physical definition and actual compound-construction source are byte-identical.
The nine-file pre-existing article draft was preserved except the explicitly authorized
callback/world integration and test witness extensions. No mass/COM/inertia analytical work
was redone. M14.19 `SimulationTransactionEngine.PersistentContact.cs` and M14.20
`SimulationTransactionEngine.ContactServicing.cs` are byte-identical to preflight. BEPU DLLs,
packages, manifest, license and build wiring are unchanged.

## Ownership decision

**A: existing supported callback surface; narrow NovaCore-owned component.**

1. BEPU child collision detection supplies convex contacts to the child callback before reduction.
2. NovaCore copies raw contact values into cold-allocated per-world scratch, bounded by actual
   immutable compound child count times four. Pruned children need not produce a callback.
3. Child-A offsets are translated by BEPU's exact rotated child-pose operation; native feature
   mixing is reproduced. Stable `(childA, childB, rawFeature)` identity is retained separately.
4. BEPU runs its default reducer. At the final parent callback, NovaCore selects from copied raw
   values, retains native slots for surviving features, and fills holes in deterministic tuple order.
5. The callback's final manifold goes directly to constraint creation/update. There is no second
   reduction, dependency patch, solver setting change or contact-count increase.

Sources at pinned BEPU commit `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`:
[CollisionBatcher](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/CollisionBatcher.cs),
[NonconvexReduction](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NonconvexReduction.cs),
[BodyReference](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/BodyReference.cs).
The accepted default reducer balances depth and spatial/normal separation; it omits normal
approach in the demonstrated speculative transition. This is not a claim of an upstream bug.

The runtime adapter admits one dynamic compound/static convex pair, worker zero, discrete
stepping. Foreign pair/worker, duplicate callbacks, invalid contact transport, ambiguous native
feature IDs and capacity violations latch failure. After `Timestep`, failure rejects export and
keeps continuation invalidated. Merely suppressing a constraint and publishing freefall is not
allowed. Existing convex fallback, material and native manifold count are preserved.
Larger compounds, multiple pairs, dynamic/dynamic worlds and CCD are not qualified here.

## Selector contract and alternatives

BEPU normals point B to A. For offsets from each COM:

```
u = n dot ((vA + aA*dt + omegaA cross rA) - (vB + aB*dt + omegaB cross rB))
p = currentDepth - u*dt
```

The timestep is the admitted 16,666/16,667-tick interval, not a display interval. Narrow phase
precedes known force integration: linear prediction uses the existing semi-implicit acceleration
once (gravity contributes about 2.725 mm over this step). Current angular velocity is a first-order
ranking estimate; future gyroscopic/finite-rotation geometry is not certified. Stored contact
depth/position/normal remain untouched.

Candidate A, implemented:

1. Protect maximum current depth, or nearest contact when all are speculative.
2. Protect the most materially imminent contact if it has a predicted-depth advantage over the
   first anchor exceeding the world's existing contact resolution. Significant closing displacement
   and predicted penetration are also required. Resolution is 0.5 mm for this admitted article.
3. Fill remaining slots by greedy max-min normalized kinetic-response row distance among current
   or predicted contacts within the existing resolution. Pad from valid native contacts when fewer
   relevant rows exist; never replace protected anchors with padding.

For normal Jacobians `Ji`, `Kij = Ji M^-1 Jj^T` and
`Dij = 2 - 2*Kij/sqrt(Kii*Kjj)`. Current local inverse inertia is rotated using BEPU's public
`PoseIntegration.RotateInverseInertia`; ephemeral cached world inertia is not read. This combines
normals and COM moment arms without adding differently dimensioned quantities or tuned weights.
Stable feature tuple is the final exact tie-break. Selection is bounded O(N) with four slots;
adapter identity/transport checks are bounded O(N²) in the cold-admitted raw capacity (12 here).

Candidate B, considered and rejected before runtime implementation: preserve native four and
repair one omitted imminent contact by minimum-loss exchange. It is smaller and reproduces the
old decisive exchange offline, but only one repair cannot address multiple independent omitted
approach directions. No algorithm tournament or physical B arm was run.
Pure future-depth top-four ranking and linear-rank-only selection were eliminated by reasoning:
they can lose current deep support or necessary unilateral torque coverage.

The selector has no article, pod, interval-35, slot-3 or 20-mm rule. No per-child quota exists.
Unit normals and valid positive mass/inertia remain upstream admitted-input requirements.
Same-build/machine determinism is the scope; degenerate geometric symmetry uses feature identity
ties, not a claim of universal label-free symmetry or cross-platform bit identity.

## Bounded validation actually executed

Final focused Debug compilation: PASS, zero warnings/errors. Earlier mechanical compilation
errors (Contact namespace qualification, generic OffsetB API and loop stack allocation) were
corrected and are not physical failures. The bounded synthetic suite initially passed before
integration. Independent review then found common gravity plus tiny angular noise could reserve
a different imminent anchor. The one permitted correction required a resolved predicted-depth
advantage over protected support; its explicit gravity/angular-noise regression passed.

| Debug sequence | Peak penetration | Peak interval | Length | Final support |
|---|---:|---:|---:|---:|
| Original tilted +0.25 rad | 10.605117363801653 mm | 35 | 1,200 | 600/600 |
| Original centered | 7.878320269941397 mm | 34 | 1,200 | 600/600 |
| Mirrored tilt -0.25 rad | 10.220270317622182 mm | 35 | 1,200 | 600/600 |
| Original tilt, existing moving frame | 10.6052272260021 mm | 35 | 1,200 | 600/600 |
| Original tilt repeat A | 10.605117363801653 mm | 35 | 1,200 | 600/600 |
| Original tilt repeat B | 10.605117363801653 mm | 35 | 1,200 | 600/600 |

All long sequences kept 400 intervals of 16,666 ticks, 800 of 16,667 ticks, exactly 20,000,000
ticks, +1,200 StateRevision, unchanged TimelineRevision, 1,200 history entries, zero debt,
one world/generation and original episode/moving-frame provenance. All independent corner,
support-height, footprint, drift/speed and final child-coverage checks remained enabled.
Repeat A/B matched every complete `SpacecraftMotion` value at all 1,200 frontiers. Mirrored
physics is not claimed bit-identical to reflected original physics; both satisfy the same bounds.
Centered peak differs from prior 7.420579679 mm by about 0.458 mm, within the unchanged gate;
changed selected rows change the solver trajectory, with stable support still measured.

### Critical interval 35

Selection order (solver slots separately preserve surviving native indices):

| Role | Child / raw feature | Native feature | Current depth m | Predicted depth m |
|---|---|---:|---:|---:|
| Current anchor | left / -4 | -260 | -0.0008059209 | 0.0013848795717197378 |
| Imminent anchor | right / -20 | -532 | -0.009753902 | 0.12422554714615375 |
| Constraint coverage | bus / -5 | -5 | -0.0049989386 | 0.023381969730236606 |
| Constraint coverage | right / -5 | -517 | -0.010367731 | 0.09708636004883249 |

These labels are report interpretation only. The imminent row has COM-relative x lever arm
`1.251866 m`; the policy uses normal motion and kinetic-response geometry. Four original raw
contacts are submitted, unmodified. The original pre-step-35 contact geometry is reproduced.
Afterward angular velocity is approximately (-0.092491, 0.0003855, -0.301669) rad/s in body
coordinates; endpoint penetration falls to 6.184646 mm at36 and 0.462587 mm at40.
Exact rows and trajectory witnesses are in [results.json](results.json).

Synthetic passes include deep separating support plus imminent angular outer contact, duplicates,
far speculative separation, two independent imminent normals, mirror/30 permutations, relative
two-body rigid-motion cancellation, acceleration counted once, invalid input/scratch refusal,
and 100 static velocity-noise cases plus actual-gravity angular-noise cases.

## Red team: blocking established-support instability

The physical runs showed 130–132 feature-set changes among 599 final-rest transitions.
One bounded same-trajectory observation, with no policy or physics change, classified the
tilted 132 changes: zero disappearing raw features, zero equivalent-geometry replacements,
132 removed distinct rows still in native selection and 132 other removed distinct rows.
Classification compares outgoing/incoming rows in the same current raw set using the existing
contact resolution and exact matching normal; aggregate counts are observations, not a new gate.

Decisive witness:

| Interval | Previous selected features | Selected features | Native features |
|---|---|---|---|
| 607 | -533,-4,-261,-20 | -277,-20,-533,-276 | -533,-4,-261,-516 |
| 608 | -277,-20,-533,-276 | -533,-4,-261,-20 | -533,-4,-261,-516 |

All twelve contacts remain available. At607, -277 and -533 tie at depth `0.0005115991 m`,
so exact tuple tie-breaking seeds -277. At608, -533 exceeds -277 by only `0.00008964538 m`,
below the existing 0.0005 m resolution, and becomes the seed. The maximum depth span at607
is about0.166018 mm; contact locations exchanged are about0.5–1 m apart. Native selection
stays identical. There is no second imminent anchor responsible for the change.

**Cause:** exact current-depth anchor ranking discontinuously reseeds coverage on unresolved
established-depth differences. This is neither harmless relabeling nor loss of raw availability.
Low drift and penetration PASS do not satisfy the separate no-unnecessary-resting-churn contract.

A permanent generic regression now deliberately exposes this remaining defect. Six unchanged
coplanar contacts, identity inertia, rest motion and gravity select `{0,1,2,3}`. Changing one
established depth from0.0004 to0.00041 m (10 micrometres, below resolution) changes selection
to `{0,1,4,5}`. `--compound-selector-only` therefore FAILS the independent resting-stability
assertion. The gate is retained, not weakened or treated as expected PASS.

Independent read-only mathematical and integration reviewers confirmed the above. Ownership,
normal sign, relative/angular velocity, mass metric, deep anchor protection, bounded storage
and failure invalidation survived review. Resting established-anchor stability did not.

## Deferred validation and disposition

Exact warmed allocation, positive allocation control, direct selection cost, retained storage,
combined-path performance, Release compilation/physics and M14.17/M14.18/M14.19/M14.20 focused
regressions were **not run** after the correctness stop. No corresponding qualification claim
is made. The cost harness exists but was not executed. Managed collector/arrays must be included
in any later storage result; existing `PoolBytes` alone would omit them.

No broad KSA research was reopened. Earlier current-source versus official-history evidence is
retained separately; no KSA selector mechanism was adopted or claimed as equivalent here.

Next Project Control decision: whether to authorize a narrow precision-aware established-support
anchor contract that preserves resolved deeper-contact priority. Stateful incumbency versus
deterministic resolution-equivalent tie handling is not decided or implemented here.

## Source and evidence

New production: `CompoundContactSelector.cs`, `CompoundContactCoverage.cs` under
`src/NovaCore.Simulation/Spacecraft/Contact/Staging/`.
Changed production for this ticket: `LocalContactCallbacks.cs`, `LocalContactWorld.cs` in that folder.
New test: `tests/NovaCore.Simulation.Tests/CompoundContactSelectorTests.cs`.
Changed tests: `EngineeringContactArticleTests.cs` (same defaults/physical assertions, added mirror,
repeat and copied witness routes) and `Program.cs` (focused routes and permanent selector-group
registration). The unresolved stability test also prevents a future full-suite acceptance pass.
Pre-existing article changes in other files remain untouched; see identity manifest.

Retained here: this report, exact concise measurements, source identities, reproduction script.
No bulk build tree was created. Five new scratch preflight/report files (94,381 bytes) remain
under `build/compound-coverage-selection`. Their useful results and provenance were consolidated
here. Automatic approval review rejected their deletion before execution as "blocked by policy";
no retry or workaround was attempted. They are disposable, not a technical acceptance blocker:

- `preflight.json`
- `regressions-debug.txt`
- `rest-witness-debug.txt`
- `synthetic-final-debug.txt`
- `tilted-debug.txt`

The two previously blocked scratch directories remain untouched:
`build/engineering-article-tilt-probe` (7 files/398,585 bytes) and
`build/compound-coverage-investigation` (139 files/21,973,615 bytes).

Use [reproduce.ps1](reproduce.ps1) for one explicitly selected focused route. Default is the cheap
failing selector stability test. It validates candidate identities first; no automatic broad campaign.

**STOP FOR PROJECT CONTROL. No banking, article acceptance or second selector correction.**

## Final Git closeout

HEAD/main/origin/main/remote main remain the baseline above. No staged files. Tags unchanged.
`git diff --check` passed; added source/test/evidence files were also checked for trailing
whitespace. Seven source/test inputs belong to this ticket; all other existing draft changes
below predate it. The unrelated historical `staging-build-blocker.md` remains untouched.

```text
 M src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs
 M src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactSource.cs
 M src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs
 M src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs
 M src/NovaCore.Simulation/Transactions/PersistentContactPublication.cs
 M tests/NovaCore.Simulation.Tests/Program.cs
?? docs/engineering-evidence/bepu-local-contact-staging/staging-build-blocker.md
?? docs/engineering-evidence/compound-contact-coverage/
?? docs/engineering-evidence/engineering-spacecraft-contact-article/
?? src/NovaCore.Simulation/Spacecraft/Contact/Staging/CompoundContactCoverage.cs
?? src/NovaCore.Simulation/Spacecraft/Contact/Staging/CompoundContactSelector.cs
?? src/NovaCore.Simulation/Spacecraft/Contact/Staging/EngineeringContactArticle.cs
?? src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Article.cs
?? tests/NovaCore.Simulation.Tests/CompoundContactSelectorTests.cs
?? tests/NovaCore.Simulation.Tests/EngineeringContactArticleTests.cs
```
