# Engineering spacecraft contact article: tilted causal review

**Current disposition:** [formal selector/article qualification](../compound-contact-coverage/formal-qualification/README.md)
is PASS, including the article's new visible route and explicit centered/tilted manual acceptance.
The candidate remains UNBANKED for Project Control. The report below is the preserved historical
causal-review snapshot; its blocked status and deferred gates do not describe the current result.

## Historical causal-review snapshot

**Lead judgment: BLOCKED / ESCALATE TO PROJECT CONTROL.**

**ENGINEERING SPACECRAFT CONTACT ARTICLE — FOLLOW-UP REQUIRED**

UNBANKED. No milestone assigned. The bounded causal review reproduced the tilted failure
exactly and independently confirmed real 87.864 mm right-pod penetration at interval 35.
No article-local construction, transform, setup or measurement defect was proven. The
compound-contact response mechanism is not sufficiently isolated to authorize a correction.

See [the current causal judgment](tilted-causal-review.md),
[the 41-row transient witness](tilted-transient-witness.json),
[verification results](causal-verification.json) and
[guarded reproduction script](reproduce-transient.ps1). All nine candidate source/test
fingerprints were restored exactly after temporary diagnostics. No permanent correction,
solver tuning or downstream qualification followed.

The resumption results below remain historical evidence; the causal report supersedes
their previous unattributed disposition.

## Baseline reconciliation

HEAD/main/origin/main: `5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0`
(`NovaCore fix: restore M14.20 owned-phase contact publication`). Remote main was checked
against that same commit before integration. Branch: `codex/engineering-spacecraft-contact-article`.
M14.20 tag remains at `49712256756e7d5433fc3ecf6aa1d1880a9f36a8`.

All nine retained draft source/test fingerprints matched [the stopped identity](draft-identity.json).
The corrected baseline touched disjoint files, so the candidate branch was fast-forwarded
without stash, patch reapplication or a merge commit. All nine source/test files and both
original evidence files remained byte-identical across that operation. All historical tags
and the unrelated untracked staging blocker remained unchanged. See
[baseline-resumption.json](baseline-resumption.json).

The original report is preserved byte-for-byte in [stopped-work.md](stopped-work.md).
This README intentionally supersedes its current disposition; no earlier failure evidence
was discarded. Blender tooling and its separate worktree were not incorporated or modified.

## Implemented draft responsibility

One immutable, additive, three-box engineering bus/pod definition supplies canonical
mass/inertia, COM-relative collision transforms, precision bounds and deterministic
publication provenance through the existing retained contact lifecycle. The visible article
route is not implemented: the physical gate failed before that authorized work phase.

| Child order | Dimensions (m) | Assembly centre (m) | Mass (kg) | Centre relative to COM (m) |
|---|---|---|---|---|
| 0: bus | 1.5, 1, 2.5 | 0, 0, 0 | 800 | 0, 0, -0.1 |
| 1: left pod | 0.5, 1, 1.5 | -1, 0, 0.5 | 100 | -1, 0, 0.4 |
| 2: right pod | 0.5, 1, 1.5 | 1, 0, 0.5 | 100 | 1, 0, 0.4 |

All child orientations are identity. X spans pods, Y up, Z fore/aft. The pieces share
faces without overlapping volume. Mass is explicitly additive uniform rigid boxes.
Total mass: 1,000 kg. Assembly COM: (0, 0, 0.1) m. Canonical body origin is COM.

The retained analytical values were not redesigned. Executed cheap tests confirmed the
full inertia tensor (kg m²), including parallel-axis contributions:

```text
[ 577.5                 0       0 ]
[     0  848.3333333333333       0 ]
[     0                 0   437.5 ]
```

The full cross terms are computed and checked as zero before using the diagonal principal
moments in the fixed body axes. Smallest feature: 0.5 m. Tolerance: 0.0005 m.
COM-relative corner radius: `1.7705931209625774` m, equal to sqrt(3.135).
The slab retains its explicit 128 x 2 x 128 m geometry, top plane Y=0.
The centered/tilted initial COM is 2 m high, with identity / 0.25 rad about Z orientations.

Identity is `NovaCore.EngineeringBusPods` v1, using fixed little-endian authored values
and SHA-256 value provenance. The exact four UInt64 digest fields are in
[resumed-validation.json](resumed-validation.json). Changed dimensions, masses and offsets
were verified to produce different identities. Actual BEPU child dimensions/orientations
and COM-relative offsets passed comparison against the authored derivation within the
existing transport bound. No BEPU recentering is performed. Presentation will consume the
same derivation, but the actual visible product has not yet been built or accepted.

## Cheap compilation and tests

The first resumed build compiled the corrected Simulation library, then exposed six
previously uncompiled article-test API references. These were corrected only in the new
test file: explicit `ReferenceFrameNode` construction, revision through the state view,
and the stored rotation's `OrientationLocalToParent` property.

A static draft-admission correction also requires normal positive component masses,
refusing subnormal mass underflow before aggregation. This does not change the declared
article or any existing box admission. These are the two intentional draft files changed
after the byte-preserving baseline update; their final fingerprints are retained.

The next Debug Simulation project build passed with **0 warnings / 0 errors**.
The original CS0103 did not recur. The cheap article gate then passed:

- Declared dimensions, masses, deterministic ordering and weighted COM.
- Full analytical tensor, principal convention and parallel-axis contribution.
- Actual COM-relative compound children and transport bounds.
- Stable identity and changed-definition discrimination.
- Invalid/unsupported dimensions, mass, orientation, precision and inertia admission.
- Source/article/configuration mismatch refusal and canonical nonmutation.
- Partial-construction cleanup: all three child slots reclaimed, pool cleared.
- Compound/child/world disposal, repeated-disposal and disposed-receipt refusal.

## First contact qualification run

One fresh Debug process ran the declared centered-then-tilted focused gate. No retries.

| Observation | Centered | Tilted |
|---|---:|---:|
| Completed intervals | 1,200 | Stopped at 35 |
| Final supported intervals | 600 / 600 | Not reached |
| Observed peak penetration | 0.007420579678659611 m | **0.08786423715152591 m** |
| Unchanged peak ceiling | 0.020 m | 0.020 m |
| Result | PASS | **FAIL** |

The tilted failure is the independent 24-corner oracle's peak-penetration gate at interval
35, after 583,333 canonical ticks. It exceeds the ceiling by about 67.864 mm. The failure
is not an allocation or baseline compilation failure. At the end of that resumption ticket
its cause was unattributed and no further investigation was attempted. The separately
authorized causal review above now confirms the physical overlap and records the remaining
mechanism uncertainty.

The centered sequence additionally proved:

- All final 600 intervals meet the geometry/support-height, drift and rest-velocity bounds.
- At least two distinct child manifolds in each final supported interval; all three child
  bits observed, mask 7. This supplements the independent geometry checks.
- Peak final-support drift: `6.122245311970352E-08` m.
- 400 intervals of 16,666 ticks and 800 of 16,667 ticks; exact 20,000,000 ticks.
- Exactly 1,200 StateRevision increments and history entries; unchanged TimelineRevision.
- Exact owner credit/debt conservation at every interval, unchanged world generation,
  original T0/frame and article provenance, and copied committed endpoint agreement.

Tilted publication/accounting assertions preceding the penetration check passed through
interval 35. This is not a completed tilted trajectory or support qualification.

## Preserved boundaries and outstanding qualification

The corrected owned-phase publisher and its banked tests match HEAD. The existing
host-servicing engine, publication/acknowledgement logic, original box tests, BEPU binaries,
solver settings and physical materials were not edited. M14.17 remains separate.

The candidate changes only the article definition, bounded local geometry/admission,
compound lifecycle, optional value provenance, child-contact diagnostics and focused tests.
There is no new owner lock, clock authority, free-flight entry, terrain, geometry editor,
Blender dependency, renderer change or live article integration.

No behavioral regression PASS is inferred merely from unchanged source. After the tilted
failure the following were **NOT RUN / NOT COMPLETED**:

- Private-only versus published trajectory and 30/60/150/240 Hz/backlog comparisons.
- Article moving-frame sequence and final tilted 600/600 support.
- Exact allocation, deliberate positive control and no-work/backlog allocation gates.
- Retained world/article/owner/history storage measurement against 8 MiB.
- Three fresh Release performance processes: **0 / 3 executed**.
- Full Debug/Release solution builds and Simulation suites; focused M14.17/M14.18/M14.19/
  M14.20, ReferenceFrames, Precision, dependency and affected presentation regressions.
- Visible article route, whole display-frame measurement and manual acceptance.

No threshold was weakened, no solver setting changed, and no repeat-to-pass run occurred.

## Cold observations (Debug, report-only)

The centered qualification process reported article creation 3.7318 ms, world preparation
30.3266 ms, first complete service operation 28.9757 ms, and first observed child-manifold
operation at interval 33 taking 25.5881 ms. These are cold Debug observations, not warmed
Release performance qualification. The callback witness may include speculative contact;
first physical-contact cost has not been separately qualified. No live-frame or presentation
resource costs were measured.

## KSA source and history

The unchanged article architecture retains the inspected current-source and official-history
comparison documented in [stopped-work.md](stopped-work.md#ksa-provenance-scope): physical
authorship, COM-relative compound construction and retained resources are adapted to one
immutable NovaCore article. Current source and historical design intent remain distinct.
KSA version/source metadata and DLL identity from that inspection are retained there.
No new KSA inference or reinspection was needed to handle this baseline resumption.
No KSA code, asset, dimensions or runtime dependency is included.

## Files, evidence and reproduction

Nine source/test draft paths are listed with exact SHA-256 values in
[current-draft-identity.json](current-draft-identity.json). Seven production draft files and
two test files remain unbanked. Only `EngineeringContactArticle.cs` and
`EngineeringContactArticleTests.cs` changed during this resumption after baseline integration.

Retained evidence budget: 128 KiB for this folder. The increased bound retains the original
stopped/resumption provenance plus one compact 41-endpoint causal witness and its guarded
reproducer. The full diagnostic stdout and restoration copies are disposable, outside this
folder. No profiler or comparison tree was created. See the causal report for the exact
remaining scratch inventory and restoration result. Unrelated scratch and Blender work
remain outside this task.

| Retained file | Purpose |
|---|---|
| README.md | Current disposition and evidence index |
| stopped-work.md | Original compiler-blocked draft report, preserved byte-for-byte |
| draft-identity.json | Original stopped draft identity |
| baseline-resumption.json | Exact preservation across baseline reconciliation |
| current-draft-identity.json | Nine authoritative current draft source/test fingerprints |
| resumed-validation.json | Earlier cheap/centered/tilted qualification results |
| tilted-causal-review.md | Bounded reproduction, geometry, chronology, rejection and stop judgment |
| tilted-transient-witness.json | Compact 0-40 endpoint/contact evidence and raw-capture hash |
| causal-verification.json | Independent geometry, continuity and accounting checks |
| reproduce-transient.ps1 | Explicit, guarded temporary instrumentation and restoration instructions |

Reproduction, after Project Control authorizes the next bounded step:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --nologo
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --engineering-article-cheap
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --engineering-article-contact
```

The last command is the existing centered/tilted witness; no automatic retry is authorized
by this report. Project Control must decide the bounded tilted-contact follow-up scope.

Nothing staged, committed, pushed, merged into main, tagged or banked. Historical tags
remain unchanged. `git diff --check` passed after stopping.

**STOP FOR PROJECT CONTROL.**
