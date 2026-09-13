# Tilted article penetration: bounded causal review

**BLOCKED / ESCALATE TO PROJECT CONTROL. UNBANKED.**

## What happened

The unchanged Debug contact gate reproduced the original first failure exactly: interval
35, penetration **0.08786423715152591 m**, against the unchanged **0.020 m** ceiling.
One fresh process executed the existing centered-then-tilted command. Centered again passed
1,200 intervals, final 600/600 supported, peak 0.007420579678659611 m. No retry occurred.

One separate diagnostic process captured the tilted initial state and intervals 1-40.
Its original corner metric at interval 35 remained exactly -0.08786423715152591 m.
It continued only to observe the transient, not to replace the permanent acceptance gate.
Temporary diagnostics have been removed from the compilation tree and the original nine
candidate source/test files restored byte-for-byte. No permanent correction was made.

## Baseline and unchanged experiment

HEAD/main/origin/main: `5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0`.
Branch: `codex/engineering-spacecraft-contact-article`.
M14.20 tag still peels to `49712256756e7d5433fc3ecf6aa1d1880a9f36a8`.
The banked owned-phase publisher and contact-servicing engine remain unchanged.

The original three children, ordering, dimensions, masses, COM and full analytical inertia
are recorded in [README.md](README.md). Actual retained BEPU geometry agrees with those
COM-relative definitions within the existing FP32 transport contract. No second COM shift
or automatic compound recentering was found. Initial COM is (0,2,0) m, orientation 0.25 rad
about Z, initial linear/angular velocity zero, mass 1,000 kg, downward force 9,810 N.
The slab is 128 x 2 x 128 m, centre (0,-1,0), top Y=0. All article corners in the transient
remain within its footprint.

T0 is tick 1,234,567. The original integer lattice, floor(n * 1,000,000 / 60), and FP32
conversion of each 16,666/16,667-tick interval are unchanged. Each diagnostic iteration
credits precisely the next interval and performs one complete service/publication operation.
No display schedule or backlog is involved. Original solver policy remains 8 iterations,
1 substep, friction 0.5, recovery 2, spring 30/1. The discrete speculative margin has
minimum zero and maximum 0.26 m; the maximum is a cap, not a constant per-step margin.

## Exact penetration contract and independent witness

The permanent gate computes the running maximum of max(0, -minimum Y) over the 24 authored
COM-relative box corners transformed by each copied, post-step canonical endpoint.
Coordinates are the fixed local world; positive signed distance is above slab top Y=0.
It is geometric support-plane penetration, not manifold depth, COM height or an AABB proxy.
The centered 7.42 mm result uses precisely this metric. Child identity chooses the physical
box/transform but does not change its sign or threshold. The quantity observes endpoints,
not an unmeasured within-step swept maximum.

The independent witness reads actual retained collision child dimensions/offsets and the
raw private pose. For child half-extents h and COM-relative centre c, with unit quaternion
q=(x,y,z,w), the world-Y row of its rotation is:

```text
a = 2(xy + zw)
b = 1 - 2(x*x + z*z)
cR = 2(yz - xw)
d = pY + a*cX + b*cY + cR*cZ - |a|*hX - |b|*hY - |cR|*hZ - slabTop
```

All child local rotations are identity. This analytic oriented-extents calculation does
not enumerate corners or use the permanent oracle's rotation helper. The raw FP32
quaternion is near unit; using its stored components gives interval-35 right-pod distance
**-0.08786423626376838 m**, only 8.877575341515342e-10 m from the original metric.
Across all 41 rows the largest disagreement is 2.639361484924052e-8 m.
A separate normalized-quaternion calculation also confirms approximately 87.8642373 mm.
These small transport/normalization differences cannot explain the 67.864 mm excess.

Runtime child centres also agree with independently composed transforms to about
2.22e-16 m in the normalized geometry check. Initial minimum signed distance is
**+1.2062888397794544 m** (left pod): there is no initial intersection.

## Decisive transient

Signed support distances below are from actual private collision geometry, in metres.
Callbacks in row n describe the pre-step pose (endpoint n-1); distances describe endpoint n.

| Interval | Bus | Left pod | Right pod | Contact chronology |
|---|---:|---:|---:|---|
| 29 | +0.144617573 | +0.020915593 | +0.515723511 | No child/parent manifold yet |
| 30 | +0.102327177 | -0.002634603 | +0.420790262 | First left-pod manifold; first geometric overlap |
| 31 | +0.077356656 | -0.001863062 | +0.320175792 | Multi-child nonconvex parent begins |
| 34 | +0.000993244 | +0.000805935 | +0.007964268 | Parent contains left and right features |
| 35 | -0.071540473 | -0.022569183 | **-0.087864236** | All three child manifolds; reduced parent retains bus/left only |
| 36 | -0.046469695 | -0.012075978 | -0.057934268 | Right-pod parent depth +0.087864235 reflects prior endpoint |
| 37 | -0.018451270 | -0.001710441 | -0.024031546 | Overlap recovering, still beyond ceiling |
| 38 | +0.005958809 | +0.005644301 | +0.006902334 | All three above plane at endpoint |
| 40 | +0.010654866 | -0.000008963 | +0.047418734 | End of bounded diagnostic, not support qualification |

At interval 35 all three children supply four candidate contacts each. The reduced
four-contact nonconvex parent contains left-pod features -260/-261 and bus features
-4/-20; no right-pod feature survives reduction. Their depths are negative pre-step gaps
of roughly 0.806, 3.209, 0.993 and 6.362 mm. The right pod supplies candidate gaps of roughly
7.964-12.157 mm. Contact normals are approximately world up. Actual margin is 0.14979374 m.
The right pod's post-step centre Y is 0.43870032072179127 m.

Immediately before that step, vertical COM velocity is -3.9135212898254395 m/s and angular
Z velocity -3.162885904312134 rad/s. Afterward they are -1.4668505191802979 m/s and
-2.1726510524749756 rad/s. Angular X velocity becomes 1.6046603918075562 rad/s. The
finite-step coupled response permits the observed geometry to cross the plane deeply.
Full poses/velocities, child centres, normals, offsets, depths and feature IDs are in
[the compact witness](tilted-transient-witness.json).

Pinned BEPU source at `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7` confirms that
[compound traversal composes child poses](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Collidables/Compound.cs#L198-L219)
and [nonconvex reduction converts child offsets and selects parent contacts](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NonconvexReduction.cs#L246-L313).
The callbacks do not reject any article child. This is not evidence of a BEPU bug.

## Causal judgment before any correction

**Proven failure class: REAL TRANSIENT PENETRATION.** Correct geometry undergoes excessive
compound-contact overlap near the transition from left-pod impact to wider support.

**Exact corrective responsibility: UNRESOLVED.** The manifold chronology identifies a
coverage concern, but does not prove that retaining different contacts alone restores the
20 mm gate. Some retained contact-point post-solve velocities also remain closing;
coupled response, compliance and finite convergence remain plausible contributors.
No counterfactual reduction or solver-policy test was performed.

| Hypothesis | Disposition / evidence |
|---|---|
| Measurement overstates overlap | Rejected: independent support function agrees at sub-micrometre scale |
| Wrong runtime child/COM transform | Rejected in observed window: actual children and composed centres agree |
| Unintended initial intersection | Rejected: initial minimum clearance exceeds 1.2 m |
| Missing child narrowphase/callback rejection | Rejected at failing step: all three child manifolds are captured |
| Reconstruction/extra step/export corruption | No evidence: unchanged world/handles, exact pre/post continuity and copied position/velocity bits |
| Compound manifold/response quality under current policy | Supported failure layer; sole mechanism and safe correction not proven |

Raw private position and velocity equal canonical copied values bit-for-bit for all rows.
Every next pre-step pose/velocity equals the preceding raw endpoint. Body handle 0, static
handle 0 and generation 1 remain constant. The same fixture retains the world; no reimport
or recreation occurs in the observed path. Clock, frontier, StateRevision and history
advance exactly once per interval; debt is zero and TimelineRevision is unchanged.
This rules out the observed double-step, export and accounting explanations. It does not
constitute a separate private-only/published trajectory A/B or prove every solver cache
unchanged internally. No banked M14.19/M14.20 defect has been demonstrated.

**Correction: NONE.** There is no proven article-local measurement, setup, construction or
lifetime defect to fix. A change to solver/contact policy, timestep or physical article
definition would need separate Project Control authorization and causal evidence. None
was attempted. The 20 mm ceiling and all physical inputs remain intact.

## Verification and validation

Two independent read-only reviews challenged geometry/measurement and compound/callback
semantics. Both confirmed physical overlap and correct transforms. Both rejected treating
the reduction chronology as proof of a sole cause. The strongest remaining objection is
that the coupled solve, rather than contact selection alone, explains the excessive motion.
That objection survives; an implementation correction is not justified by this probe.

| Executed work this ticket | Result |
|---|---|
| Unchanged focused contact command, one fresh Debug process | Centered PASS; tilted first fails at 35 with identical peak |
| Centered | 1,200 intervals; 600/600 supported; 7.420579679 mm peak; child mask 7 |
| Centered accounting | 400 x 16,666 + 800 x 16,667 = 20,000,000 ticks; 1,200 revisions/history; no lost debt |
| Tilted original gate | Stops at 35; final 600 support not reached; publication/accounting checks preceding failure pass |
| Temporary tilted diagnostic, one fresh Debug process | 40 intervals + initial row; peak 35/right pod; same original metric |
| Static/data verification | 41 rows pass continuity, identity, accounting and geometry checks |
| Restored ordinary Debug Simulation project build | PASS, zero warnings/errors; 2.75 s reported |
| Post-correction tilted/centered qualification | Not run: no correction authorized or made |

No repeated-to-pass run, allocation/performance campaign, host-partition campaign, moving-
frame campaign, broad regression, visible route or manual acceptance followed.

## Evidence, restoration and reproduction

All nine candidate source/test hashes match [current-draft-identity.json](current-draft-identity.json).
No permanent source/test path changed during this ticket. The existing seven production
draft paths and two test draft paths remain unbanked; their exact names/hashes are in that
identity file. The banked `SimulationTransactionEngine.PersistentContact.cs` and
`SimulationTransactionEngine.ContactServicing.cs` remain identical to HEAD, as do the
original qualification-box tests. Historical tags and the unrelated staging-build-blocker
file remain untouched. Nothing is staged; `git diff --check` passes.

This ticket adds this report, `tilted-transient-witness.json`, `causal-verification.json`
and `reproduce-transient.ps1`; updates the README; and corrects only the earlier evidence
JSON's four UInt64 identity fields to exact decimal strings from the retained original
output. That metadata repair fixes serialization rounding, not article identity or physics.

The original failing command is:

```powershell
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --engineering-article-contact
```

The guarded reproducer contains the exact temporary observer/source changes and one 0-40
diagnostic. Its PowerShell syntax was checked; it was not rerun as a packaged script after
the manual equivalent completed. Run it only with explicit authorization for another
reproduction. It checks HEAD and all nine source fingerprints, refuses existing probe
paths/output, restores original bytes in `finally`, verifies hashes and rebuilds ordinary
binaries. It is a diagnostic artifact, not a permanent qualification test.

The retained compact witness is sufficient to reconstruct the independent support
distances and contact chronology without rerunning physics. Raw stdout SHA-256 is
`999C95470FDED85F3907495B0BA3973F617373BC2BA7A1D4103A8E38A040465B`.

Disposable directory: `E:\NovaCore\build\engineering-article-tilt-probe`.
Seven files, **398,585 bytes**, remain:

| File | Bytes |
|---|---:|
| EngineeringContactArticleTests.cs | 18,219 |
| LocalContactCallbacks.cs | 3,360 |
| Program.cs | 250,920 |
| restore.json | 794 |
| RuntimeProbe.cs.diagnostic | 2,529 |
| TestsProbe.cs.diagnostic | 4,133 |
| transient.txt | 118,630 |

Automatic approval review rejected the combined restoration/deletion command before
execution as "blocked by policy". Restoration was subsequently completed through exact
byte writes and reversible moves of the two diagnostic sources to this scratch directory;
no deletion was retried. No diagnostic source remains in the production/test compilation
tree. Cleanup is deferred to Project Control; the remaining files are disposable after
accepting this concise evidence. Separate Blender work is untouched.

## What happens next

Project Control decides whether a separately bounded compound-contact policy investigation
has sufficient payoff. The current evidence neither authorizes solver tuning nor identifies
one permitted local correction. Preserve the article and its strict physical gate.

**BLOCKED / ESCALATE TO PROJECT CONTROL. UNBANKED. STOP FOR PROJECT CONTROL.**
