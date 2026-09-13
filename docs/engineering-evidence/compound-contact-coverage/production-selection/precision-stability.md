# Precision-aware established-support stability

**PASS for this bounded correctness/stability revision. Candidate remains UNQUALIFIED / UNBANKED.**

The prior [implementation report](README.md) is historical and remains retained with its
failing witness. This follow-up resolves that witness without replacing the four-contact
architecture. Allocation, performance, broad regressions and remaining article acceptance
are not granted by this result.

## Baseline and scope

HEAD/main/origin/main: `5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0`.
Branch: `codex/engineering-spacecraft-contact-article`.
Windows x64, .NET SDK 10.0.303, runtime 10.0.12.
All 60 historical tag refs and the empty index remain unchanged.

All 372 source/test/dependency inputs matched the preceding candidate before editing.
Exactly one production file and two test files changed in this follow-up; 369 inputs remain
byte-identical. [precision-identity.json](precision-identity.json) contains the exact
before/after hashes, protected inputs and whole-input-tree digests.

## Exact original comparison path

The existing generic permanent witness uses six coplanar contacts with normal +Y,
COM-relative y = -0.5 m, unit inverse mass and identity inverse inertia:

| Stable feature | x | z | Original depth m | Perturbed depth m |
|---|---:|---:|---:|---:|
| 0 | 1 | 0 | 0.0004 | 0.0004 |
| 1 | 0 | 1 | 0.0004 | 0.0004 |
| 2 | -1 | 0 | 0.0004 | 0.0004 |
| 3 | 0 | -1 | 0.0004 | 0.0004 |
| 4 | 1 | 1 | 0.0004 | 0.00041 |
| 5 | -1 | -1 | 0.0004 | 0.0004 |

These literals are stored as FP32 input values. Geometry, motion, gravity and all identities
are unchanged. The existing admitted contact tolerance comes from minimum feature / 1000
in `LocalContactSource.TryCreateCore`: this article's 0.5 m minimum feature gives 0.0005 m.
The adapter passes its FP32 representation to the selector; this revision changes neither
derivation nor representation.

Previously the first pass selected the greatest **raw current depth**, breaking only exact
depth equality by stable tuple. The unperturbed seed was feature 0; adding 10 micrometres
made feature 4 the exact maximum and changed that seed. Resolution already controlled
imminence and relevant-row eligibility, but did not control this initial support ranking.

Common gravitational approach does not reserve a second imminent anchor here: predicted
depth advantage over the seed is not greater than the existing resolution. All six contacts
remain relevant. The only depth-sensitive choice that changes in this witness is the seed.

For this fixture, the existing coverage metric reduces to:

```text
Kij = 1 + xi*xj + zi*zj
Kii = 1 + xi*xi + zi*zi
distance(i,j) = 2 - 2*Kij / sqrt(Kii*Kjj)
next = greatest minimum distance to all already selected rows
exact score ties = existing stable feature tuple
```

| Old input | Seed | First coverage choice | Second | Third | Complete order |
|---|---:|---|---|---|---|
| Original | 0 | 2: score 2, ties 5 and wins by feature | 1: score 1 | 3: score 1 | 0,2,1,3 |
| +10 micrometres at 4 | 4 | 5: score 8/3 | 0: score 0.3670068381445477 | 1: same score | 4,5,0,1 |

The latter two slots tie among the remaining symmetric rows and use stable features.
Thus unchanged coverage logic changes sets from {0,1,2,3} to {0,1,4,5} because raw depth
reseeded it. Every candidate score for each choice is retained in
[precision-results.json](precision-results.json). No broader attribution was needed.

## Precision contract and smallest correction

For one validated candidate set, let D be its common maximum stored depth and rho its
existing admitted positive resolution. Define one contextual top-depth cohort:

```text
C = { i : computed binary64(D - depth[i]) < rho }
support anchor = minimum existing (ChildA, ChildB, RawFeature) tuple in C
```

The maximum is always in C, so C cannot be empty. All input depths are checked finite before
selection. Physical depth remains unchanged input data. Original normals, offsets and feature
identities also remain unchanged. The existing imminent-anchor and normal/moment-arm coverage
selection then run on the original data.

This is a cohort for the top-support decision, not a pairwise approximately-equal comparator
or a complete ordering of all depths. Membership uses the **same D** for every row. It cannot
chain A-close-to-B and B-close-to-C into admission of an A that is a resolved distance from D.
Membership in the common cohort is transitive; admitted stable identity gives a deterministic
total tie order within it. No scan incumbent, sort order or previous-frame state defines D.

Boundary and sign semantics:

- Computed gap less than rho: depth does not rank rows within the cohort.
- Computed gap equal to or greater than rho: excluded from this top cohort.
- No buckets anchored at zero; negative speculative separation and positive penetration use
  the same rule. Larger depth still means nearer/deeper.
- NaN and either infinity fail the existing finite-input admission.
- A row can enter or leave at the explicit gap == rho boundary. No universal invariance under
  arbitrarily small perturbations crossing that boundary is claimed.
- FP32 values are promoted for the binary64 difference. This is a specified computed comparison,
  not a claim of exact real subtraction over every possible FP32 exponent range.
- Geometrically degenerate ties use existing stable identity. Label-free symmetry or arbitrary
  relabeling invariance is not claimed; mirrored geometry with preserved identities is tested.

Only the exact-depth seed comparison changed: accumulate the maximum, then scan once to choose
the stable cohort member. The prior gravity/angular-noise correction, imminent logic, kinetic
coverage metric, four-row budget and padding remain. No new allocation, LINQ, sorting,
collection, temporal hysteresis, child quota or article identity appears in production.
The added work is one scan over existing bounded input storage. Formal cost/allocation
qualification remains deferred.

## Permanent selector tests

Final focused Debug build: **PASS, zero warnings/errors**. Final selector suite: **PASS**.

| Gate | Result |
|---|---|
| Existing 10 micrometre witness | Both sets {0,1,2,3}; original assertion preserved |
| Below-resolution perturbation sweep | 11/11, from -490 to +490 micrometres |
| Clearly resolved depth difference | 2*rho changes anchor and selected topology |
| Exact boundary | FP32 predecessor/equal/successor of rho select anchor 0/4/4 |
| Epsilon-chain counterexample | 0, 0.75*rho, 1.5*rho; distant first row excluded from top cohort |
| Input order | All 720 permutations preserve complete ordered selection |
| Mirror | Mirrored geometry with stable features preserves mirrored selection |
| Deep separating contact | 0.1 m contact remains protected despite separating velocity |
| Sign behavior | Negative, positive and zero-straddling depth cohorts pass |
| Original data | Literal FP32 bits of every input depth/offset/normal and identity unchanged |
| Nonfinite | NaN, positive infinity and negative infinity refused |

Existing imminent angular approach, multiple imminent normals, deep support, moment-arm
coverage, common-gravity/angular-noise, relative motion, duplicate and invalid-data tests pass.
No physical gate was weakened or changed to an expected failure.

## Resting-support stability

The same tilted 1,200-interval rest witness now reports:

| Measurement | Result |
|---|---:|
| Compared final-rest transitions | 599 |
| Selected-set changes | 0 |
| Exact transition intervals | Empty |
| Raw-contact availability changes | 0 |
| Native-manifold feature-set changes | 0 |
| Maximum raw depth spread in final rest | 0.00016781393787823617 m |
| Unexplained sub-resolution transitions | 0 |

The old result was 132/599. In the new measured rest window, the entire raw depth span remains
below unchanged 0.0005 m resolution, so raw-depth ordering cannot reseed the common cohort.
There are no remaining transitions to assign to another cause. This is the measured scenario
result, not a promise that contact sets never change under resolved motion or availability.

Two rest processes were used: the first reported zero transitions; the second retained the
same policy and added the requested raw/native/depth-span observation. No physics or policy
adjustment and no retry after a failure occurred.

## Required physical correctness and deterministic repeat

After selector tests passed, one fresh Debug process ran the original tilted sequence.
A second ran centered, mirrored, moving frame, then two tilted repeat sequences.
All original physical thresholds and setup remain unchanged.

| Sequence | Previous peak m | Current peak m | Peak interval | Length | Final support |
|---|---:|---:|---:|---:|---|
| Tilted +0.25 | 0.010605117363801653 | 0.006945546380241474 | 35 | 1200 | 600/600 |
| Centered | 0.007878320269941397 | 0.005819714882528193 | 34 | 1200 | 600/600 |
| Mirrored -0.25 | 0.010220270317622182 | 0.006688154529344681 | 35 | 1200 | 600/600 |
| Moving-frame tilted | 0.0106052272260021 | 0.006945746950922316 | 35 | 1200 | 600/600 |
| Tilted repeat A | — | 0.006945546380241474 | 35 | 1200 | 600/600 |
| Tilted repeat B | — | 0.006945546380241474 | 35 | 1200 | 600/600 |

All peaks are below the unchanged 0.020 m gate. All six sequences show zero final-rest
selected-set changes. Independent transformed-corner depth, support height, COM footprint,
drift/speed, angular response and distinct supporting-child checks pass.

Every sequence preserves:

- 400 intervals of 16,666 ticks and 800 of 16,667 ticks: exactly 20,000,000 ticks;
- original T0, moving-frame origin and retained world generation;
- +1,200 StateRevision, unchanged TimelineRevision and 1,200 publication histories;
- exact host credit/debt conservation and zero remaining debt.

The material transient change has a concrete selection witness at interval 35. The old seed
was left raw feature -4 (native -260), depth -0.0008059209 m. Bus raw -4 (native -4), at
-0.0009932541 m, is about 0.1873332 mm behind that maximum, inside the same 0.5 mm cohort.
The existing stable child/feature tuple now selects the bus row. The distinct imminent right
row -20 (native -532) remains protected, as do coverage rows bus -5 and right -5.
New selection order is [-4,-532,-5,-517]. Right-row approach prediction remains
0.12422554714615375 m with COM-relative x lever arm 1.251866 m. Original geometry,
gravity, dt and solver configuration are unchanged. Selecting another cohort support row
changes impulse/torque distribution; trajectory identity against the old policy is not expected.
At interval 40 penetration is 0.0004151843020882007 m. The exact stored contact witness is
retained in the results file. The same rule applies in the other fixtures; no per-fixture
policy was introduced.

Tilted repeats match all **1,200 complete SpacecraftMotion values**, **1,200 ordered selector
count/identity snapshots**, and **1,200 full publication-history records** under the existing
same-build/machine value contract. An identity snapshot count of zero denotes native convex
fallback before the specialized selector is active; it is not a missing comparison.
This does not claim cross-platform bit identity or exact reflection of mirrored solver motion.

## Independent red team

A read-only verifier inspected the actual final code and retained results without editing
or running physics. Verdict: **PASS for authorized correctness/stability only**.

The strongest attack was epsilon chaining or incidental seed order masquerading as equivalence.
The common maximum, chain test and 720 complete permutations defeat that failure mode.
Boundary tests make the remaining real discontinuity explicit. Meaningful-depth and deep
separation tests show that stabilization does not hide resolved penetration. Literal bit checks
protect actual solver geometry. Existing mirror/normal/approach/coverage tests remain passing.
No article, side, interval or history-dependent special case appears in the correction.

The former physical churn is zero with unchanged raw/native identities and sub-resolution
depth spread. All physical sequences and deterministic identities pass. Independent review
accepted the interval-35 explanation and found no further bounded correctness defect.

## Source, evidence and reproduction

Changes in this follow-up:

- Production: `src/NovaCore.Simulation/Spacecraft/Contact/Staging/CompoundContactSelector.cs`.
- Tests: `tests/NovaCore.Simulation.Tests/CompoundContactSelectorTests.cs`;
  `tests/NovaCore.Simulation.Tests/EngineeringContactArticleTests.cs`.
- Evidence: this report, [precision-results.json](precision-results.json),
  [precision-identity.json](precision-identity.json), [precision-reproduce.ps1](precision-reproduce.ps1),
  and a current-follow-up link above the historical README.

The prior candidate input-tree digest was
`260C0BE8580496D7F3C2B10D75A8D1A5F9829C12380F7C8376F2AA0AD8BDECCA`.
The current digest is
`74A56DACD06742C5940FF5699E055EAE7AAA028722B4A91E7162F80EAD05BEB5`.
Exact individual hashes and protected identities are in the identity file.

BEPU binaries/packages/license/manifest/pins/build wiring, article physical definition,
compound shape construction and M14.19/M14.20 transaction-owner source are byte-identical
to follow-up preflight. The two transaction-owner files also remain unchanged from HEAD.
The other pre-existing article/callback/world draft changes remain present and untouched.

PowerShell 7, from a matching Windows x64 snapshot with SDK 10.0.303/runtime 10.0.12:

```powershell
& .\docs\engineering-evidence\compound-contact-coverage\production-selection\precision-reproduce.ps1 -VerifyOnly
& .\docs\engineering-evidence\compound-contact-coverage\production-selection\precision-reproduce.ps1 -Case selector
& .\docs\engineering-evidence\compound-contact-coverage\production-selection\precision-reproduce.ps1 -Case rest
& .\docs\engineering-evidence\compound-contact-coverage\production-selection\precision-reproduce.ps1 -Case tilted
& .\docs\engineering-evidence\compound-contact-coverage\production-selection\precision-reproduce.ps1 -Case regressions
```

Each explicitly selected route checks the candidate snapshot first, builds Debug, starts its
test executable in a fresh process and stops on failure. The cheap selector route is the default.
The older reproduction script intentionally guards the earlier snapshot and is historical.
These are reproduction instructions, not authorization for another campaign in this ticket.

No new scratch build tree or raw trace files were created. Existing scratch remains untouched:
5 files/94,381 bytes under `build/compound-coverage-selection`;
7 files/398,585 bytes under `build/engineering-article-tilt-probe`;
139 files/21,973,615 bytes under `build/compound-coverage-investigation`.
No deletion or retry was attempted. Existing reporter cold/first-contact timings are incidental
Debug observations, not performance qualification.

## Closeout

No staged changes, commits, merges, pushes, tag changes or banking.
`git diff --check` passes; follow-up source/test/evidence files additionally pass trailing
whitespace checks. Concise evidence is retained; no expensive follow-on gate was run.

Final `git status --short` (pre-existing candidate changes remain; only the three inputs
listed above changed in this follow-up):

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

**Judgment: PASS. STOP FOR PROJECT CONTROL.**
Formal allocation/positive-control campaigns, Release performance, broad milestone regressions,
remaining article qualification, manual presentation, Blender and banking remain deferred.
No milestone number is assigned.
