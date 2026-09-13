# Authored engineering contact article: blocked draft

Disposition: **BLOCKED / UNBANKED**. Lead judgment: **ESCALATE TO PROJECT CONTROL**.
No milestone assigned. The first compilation stopped at an unchanged banked-source error;
the article draft is **UNQUALIFIED**. Do not rely on it as accepted implementation.

## Baseline and first blocker

Repository: `E:\NovaCore`. Candidate branch: `codex/engineering-spacecraft-contact-article`.
HEAD, main, origin/main and remote main were independently verified at
`f0c8e0f2c16de98e492ef4b9b4a5a24f372e8b18`
(`Tests: restore M14.20 contact development scene wiring`).
The M14.20 tag still peels to `49712256756e7d5433fc3ecf6aa1d1880a9f36a8`.

One attempted command, SDK 10.0.303:

```powershell
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --nologo
```

Result: FAIL, 0 warnings, 1 error, 3.52 s reported build time:

```text
src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.ContactServicing.cs(100,35):
error CS0103: The name 'PublishPersistentContactInOwnedPhase' does not exist in the current context
```

The banked servicing source calls that method; the banked publisher defines
`PublishPersistentContactCore`, which acquires/exits the owner phase itself, but no
`PublishPersistentContactInOwnedPhase`. Substituting the public publisher would require
examining reentrancy/ownership semantics and is not an article geometry correction.
No such substitution or other ownership change was made.

Cheap static confirmation, without another build or comparison tree:

```powershell
git grep -n 'PublishPersistentContactInOwnedPhase' HEAD -- '*.cs'
git hash-object src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.ContactServicing.cs src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PersistentContact.cs
git rev-parse 'HEAD:src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.ContactServicing.cs' 'HEAD:src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PersistentContact.cs'
```

The sole tracked occurrence is the call at line 100. Both working files match their HEAD
Git blobs exactly:

| Unchanged responsibility | Git blob |
|---|---|
| Contact servicing | `36abb02cfb4452c5de778750d7ff70e8bf95c90c` |
| Persistent-contact publisher | `566c91cf68c8bec80680c3ff64c2e1b16fbc6f89` |

This is a current baseline compilation blocker, not evidence about article contact quality,
BEPU binaries or the dependency architecture. The ticket requires stopping on an unexpected
material failure. Project Control must resolve/authorize the baseline correction before
the article gauntlet resumes. No retry or broader diagnostic campaign was run.

## Declared physical article (not dynamically qualified)

`NovaCore.EngineeringBusPods`, version 1. SI units; additive uniform rigid box masses.
Fixed order: bus, left pod, right pod. X spans pods, Y up, Z fore/aft. All authored
orientations are identity. Pieces share faces in X without overlapping volume.

| Child | Dimensions, m | Assembly centre, m | Mass, kg | COM-relative centre, m |
|---|---|---|---|---|
| Bus | 1.5, 1, 2.5 | 0, 0, 0 | 800 | 0, 0, -0.1 |
| Left pod | 0.5, 1, 1.5 | -1, 0, 0.5 | 100 | -1, 0, 0.4 |
| Right pod | 0.5, 1, 1.5 | 1, 0, 0.5 | 100 | 1, 0, 0.4 |

Analytical total mass: 1,000 kg. Assembly COM: (0, 0, 0.1) m. Canonical origin is COM.
The full aggregate inertia tensor is diagonal in the declared body axes:

```text
kg m²: [ 1155/2      0       0 ]
       [    0    2545/3      0 ]
       [    0       0    875/2 ]
```

Cross terms cancel by symmetry after including the full parallel-axis terms. Draft code
computes all six independent tensor elements and requires exact zero cross terms before
using principal moments. No eigensolver is introduced.

Smallest feature: 0.5 m, yielding the existing dimension/1000 tolerance policy at 0.0005 m.
COM-relative maximum corner radius: sqrt(3.135) m. Overall dimensions: 2.5 x 1 x 2.5 m;
those overall dimensions do not replace the smallest feature or COM-relative radius.
Initial COM height: 2 m. Centered orientation: identity. Tilted orientation: 0.25 rad about Z.
Constant acceleration: (0, -9.81, 0) m/s². Slab: unchanged 128 x 2 x 128 m with top Y=0.
Flat support COM height derives as 0.5 m from the authored geometry.

Identity encoding in the draft: little-endian version followed by each ordered child's
dimensions, assembly position, orientation and mass; canonical signed zero; SHA-256 stored
as four value-only UInt64 fields. No live shape handles or generation order enter provenance.
The identity implementation and all physical assertions remain unexecuted.

## Bounded draft changes

- Immutable article definition, full tensor calculation, COM transforms and content identity.
- Optional article geometry on local-contact configuration; strict mass/inertia admission;
  actual small-feature and COM-radius reduction bounds; explicit article slab thickness.
- Three owned convex children in one small compound. Children arrive already COM-relative;
  BEPU does not recenter them or author canonical inertia.
- Compound/child disposal and a narrow partial-construction cleanup test seam.
- Optional deterministic article identity in episode provenance. Legacy box identity fields
  retain their values and receive the default empty article identity.
- Bounded three-bit child-manifold diagnostic, used with independent 24-corner geometry checks.
- Draft analytical/admission/lifetime tests and centered/tilted continuation tests, with focused
  command registration only. No visible article route or launcher has been changed.

The transaction engine, host-time owner phase, receipt seal/acknowledgement logic, solver
settings and materials are unchanged. Preserving their behavior still requires regressions
after the baseline builds. No qualification claim is made merely from source similarity.

## Qualification disposition

| Gate | Result |
|---|---|
| Baseline/ref/source inspection | PASS; missing banked method confirmed |
| Initial Debug Simulation project compile | FAIL: CS0103 in unchanged baseline |
| Analytical, geometry, identity and lifetime execution | NOT RUN |
| Centered/tilted, final 600/600 supported intervals | NOT RUN |
| Private/published trajectory and host partition comparison | NOT RUN |
| Tick/debt/revision/history and moving-frame execution | NOT RUN |
| Exact allocation and positive control | NOT RUN |
| Retained storage measurement | NOT RUN |
| Three Release performance processes | NOT RUN: 0/3 |
| Cold preparation/first contact and whole display frames | NOT RUN |
| New visible route/manual acceptance | NOT IMPLEMENTED / NOT REQUESTED |
| Full builds, Simulation, M14.17-M14.20, precision/reference/dependency/graphics regressions | NOT RUN |
| `git diff --check` | PASS after stopping |

No acceptance matrix, profiling, retries, renderer campaign or dependency changes were run.

## KSA provenance scope

Current installed KSA DLL SHA-256 was rechecked:
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
Available source metadata matches version
`2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`.
This verifies the available metadata match; it does not claim a fresh decompilation.

Inspected source responsibilities: collider templates, `Vehicle` compound construction
with assembly-minus-COM child positions, `MassProperties`/`OffsetMassProperties` parallel-axis
aggregation, `VehiclePropertiesEx` principal axes, part-model transforms and `BepuHandles`
resource ownership. NovaCore adapts physical authorship to one immutable fixed-axis article;
it does not import KSA's vehicle assembly, eigensolver or mutable COM mechanisms.

Official history inspected during the preceding architecture assessment is design provenance,
separate from current source: revisions
[3059](https://discord.com/channels/1260011486735241329/1260112103134724146/1449651012901011547),
[3132](https://discord.com/channels/1260011486735241329/1260112103134724146/1456807271085506780),
[4585](https://discord.com/channels/1260011486735241329/1260112103134724146/1514099339931291698),
[4589](https://discord.com/channels/1260011486735241329/1260112103134724146/1514133187314450473),
[4873](https://discord.com/channels/1260011486735241329/1260112103134724146/1524632713259122734),
[5191](https://discord.com/channels/1260011486735241329/1260112103134724146/1535062333699924061), and
[5354](https://discord.com/channels/1260011486735241329/1260112103134724146/1541296827129794571).
These describe assembly/COM separation, mass-property fixes, compound COM maintenance,
construction lifecycle, authored colliders and contact diagnostics. They do not qualify
this NovaCore draft. No KSA code, assets, dimensions or runtime dependency were copied.

## Retention and resumption

Keep this report, `draft-identity.json` and the nine unqualified source/test draft files.
The JSON fingerprints the precise stopped draft and unchanged blocker files. No disposable
comparison tree or raw diagnostic dump was created. The attempted build used ordinary ignored
project outputs; no unrelated outputs were deleted. The pre-existing untracked
`bepu-local-contact-staging/staging-build-blocker.md` remains untouched.

After Project Control supplies an authorized corrected baseline, reconcile the draft against
that baseline and restart at compilation followed by `--engineering-article-cheap`.
Only after cheap gates pass should `--engineering-article-contact` run. Remaining allocation,
trajectory, visible-route, performance, regression and manual gates are outstanding implementation
and qualification work; do not treat retained draft tests as passed.

No staging, commit, push, merge, tag, banking or milestone assignment occurred.

**STOP FOR PROJECT CONTROL.**
