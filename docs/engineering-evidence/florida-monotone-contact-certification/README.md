# Florida monotone contact certification qualification

**UNBANKED — ready for Project Control acceptance.** Proposed accomplishment:
**Certify monotone point contact on the existing Florida grading plane.**
Lead judgment: **PASS**. No milestone number, banking or operational contact loop.

Baseline: `2657bb16cdee2a42d1e5f9be830d247fb3c607f6` (banked M14.8), initially clean
`main`. Development branch: `codex/florida-monotone-contact-certification`.
HEAD, main, origin/main and all eight annotated M14 tags remain unchanged.
The implementation, proof equations, exact limits and consumer contract are in
[the current-facing contract](../../florida-monotone-contact-certification.md).

## What was proved

The acquired production Florida terrain, complete one-point craft geometry,
unchanged constant-root-force segment, current Earth orbit/orientation and strict
zero-angular specialization support actual clear and unique approaching-root
certificates. Whole-interval footprint/guard/clamp checks are proof premises.
M14.8 supplies numerical observations and authority admission, not root signs or
derivative evidence. No frozen Earth, shadow orbit, replacement plane or sampled
anchor was introduced.

The qualified coverage is **seed-relative ±3,600 seconds**, in a single whole-second
cell. It does not include the normal 2024 launcher start and is not generalized
Earth ephemeris coverage. Proofs are useful within that explicitly bounded domain;
no route was wired to consume them. Enlarging coverage is a separate qualification
question, not extrapolation permitted by this result.

Three independent read-only reviews examined mathematics, grading containment and
authority/ordering while one writer implemented the candidate. Review corrections
were bounded: enclose large epoch conversion before arithmetic; add pole rates
inside outward arithmetic; avoid a second normalization of numerical witness
directions; and check freshness in the internal certificate issuance entry itself.
No legacy physical evaluator, time/order implementation or transaction mutation
path changed. The transaction engine gained only a read-only view of its own
clock/timeline boundary for proof applicability.

## Decisive physical witnesses

Actual acquired authority:

* Earth body 6; terrain source/version 2/5; physical generation 4; query policy 1;
* radius 6,371,008.8 m; facility identity 5597594574148789740;
* global SHA-256 `4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317`;
* regional SHA-256 `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.

All six independent real-Earth bound cases passed: clear/crossing intervals
starting at −3,600, 0 and 3,599 seconds. These are not sampled containment tests.
At the seed, the approaching case's certified derivative is
`[-1.085982153958896, -0.8874696315145535] m/s`; endpoint f bounds are strictly
positive then negative. The clear case's entire f range is
`[9.971718955785034, 10.034916226752104] m`.

The seed crossing refines in 17 bisections to
`[0.50335693359375, 0.5033645629882812] seconds` for a requested witness width of
10 microseconds. This width is not a contact tolerance. Those two rational witness
endpoints have physical radial-gap enclosures approximately `[5.223,6.231] µm`
and `[-2.297,-1.303] µm`; M14.8 observes approximately `5.939 µm` and `-1.508 µm`.
The exact endpoint epochs and sample-specific discrepancy intervals are retained
in [validation.json](validation.json). The root itself was never rounded or
converted to either endpoint.

At 3,599 seconds, strict signs and monotonicity still certify existence/uniqueness,
but the same tight refinement request returns `Unresolved/NumericalResolution`.
The coarse existence proof does not become an approximate-time success. The
coverage position remainder is 0.05123638785047009 m per component; endpoint
radial-gap uncertainty can be about 0.094 m there. Numerical discrepancy bounds
are a-posteriori evidence at named samples, not uniform accuracy guarantees.

## Validation

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Full Simulation | 40/40 groups PASS | 40/40 groups PASS |
| New mathematical/analytical proof group | PASS | PASS |
| Actual Florida proof, refusal, root and cost integration | 1/1 PASS, no skips | 1/1 PASS, no skips |
| ReferenceFrames | 11/11 PASS | 11/11 PASS |
| Precision | PASS | PASS |
| Focused M14.2–M14.8 Simulation gates | PASS | PASS |
| M14.1 production query and stale snapshot | 2/2 PASS, no skips | 2/2 PASS, no skips |
| M14.3 production terrain integration | 1/1 PASS, no skips | 1/1 PASS, no skips |
| M14.8 production terrain integration | 1/1 PASS, no skips | 1/1 PASS, no skips |
| Git whitespace check | PASS | PASS |

The final strengthened tests passed one full suite per configuration. A new
distinct-input check first exposed that the nearby-root fixture's 1e-7 m position
delta rounded away at inertial-root magnitude. The fixture now uses distinct stored
velocities, checks that distinction and still requires unresolved overlap. Both
configurations passed after this test-only correction; production was unchanged.
No unchanged failing gate was retried into a pass. Existing servicing and canonical-group
measurements remained zero, with exact clock/debt/count/completion outcomes.
Debug/Release certificate, refinement and numerical witness records match exactly.
The new Simulation group is appended after the existing groups; their common order
and permanent workloads are unchanged. No unrelated GPU/window campaign was run.

Analytical controls use exact polynomial reasoning for rational, irrational and
oversized-denominator roots, clear intervals, uncertain/exact boundary contact,
tangency and multiple crossings. Independent decimal Machin/Taylor identities
check the numerical enclosure; a separate decimal Euler/analytic-derivative oracle
checks position/derivative at the authoritative Cartesian seed with a nonidentity
craft quaternion. The oracle does not call the production proof helper. Test-local
polynomial isolation is not claimed as a general production polynomial provider.
Actual provider tests separately cover successful refinement and budget/resolution
exhaustion, equality after refinement/re-admission, overlapping roots, root/rational
ordering and deterministic repeated/reordered calls.

The real-Earth tests also exercise full-weight near-edge contact, endpoints inside
with an interior excursion outside (refused), blend/guard refusal, negative time,
nonidentity fixed attitude and nonzero offset, nonzero force, changed force,
nonzero angular state/torque refusal, copied provenance, altered body/facility/model,
changed graph/geometry, default proof and private constructor visibility. Direct
torque invalidation explicitly changes StateRevision while leaving TimelineRevision
unchanged; cancellation separately changes timeline applicability. All operations
refuse stale context, including internal issuance, refinement and comparisons.

## Bounded cost and allocation

Ryzen 9 9900X; SDK 10.0.303; net10.0 with installed .NET 10.0.12 under default host
selection. No profiler, tiering, PGO or GC configuration override. Timings use 32
warmup calls then 101 individual ordinary-runtime samples; sorted indices
50/95/99/100 report median/P95/P99/max. Timer resolution is 100 ns, so tiny refusal
measurements are at that resolution. Allocation windows are separate, using the
banked checked 1 MiB no-GC helper with hard entry/exit failures.

| Release CPU operation | Median µs | P95 µs | P99 µs | Observed max µs | Warmed bytes |
|---|---:|---:|---:|---:|---:|
| Clear certification | 137.5 | 144.4 | 149.7 | 164.6 | 0 |
| Crossing certification | 137.4 | 144.0 | 149.4 | 156.5 | 0 |
| Unresolved initial/boundary case | 137.4 | 145.8 | 149.4 | 161.6 | 0 |
| Successful 17-step refinement | 763.6 | 773.5 | 775.8 | 800.0 | 0 |
| Root/root comparison | 270.0 | 273.9 | 285.0 | 295.6 | 0 |
| Root/rational comparison | 45.4 | 46.9 | 49.7 | 54.6 | 0 |
| Stale refusal | 0.1 | 0.1 | 0.1 | 0.1 | 0 |
| Unsupported admission | 0.3 | 0.3 | 0.4 | 0.4 | 0 |
| Setup/admission, including two M14.8 observations | 701.4 | 709.9 | 729.1 | 741.2 | 1,672/call |

The frozen provider is the setup allocation. Caller-owned observation scratch is
four arrays, 3,520 bytes of element payload for current Sol (array headers excluded).
`Proof` is a 64-byte value retaining the provider. Certification uses three domain
evaluations; refinement has at most 24 steps; root comparison has at most 48 total
narrowing attempts. Domain evaluations use fixed-size local value storage, no heap
workspace. Reusable certification/refinement/comparison/refusal paths measured
exactly zero. The separate real `byte[128]` control detected 152 bytes in both
configurations. Re-admission/replay has the documented setup allocation; no
serialization path exists. Full Debug distributions are in validation.json.

These are bounded CPU measurements, not whole-frame or gameplay scaling claims.
M14.8 queries occur at admission only, not on each proof/refinement/comparison.
No repeated terrain-query work dominates the warmed proof path. No cadence,
thread-pool, runtime scheduling or performance optimization entered this ticket.

## KSA / BRUTAL lifecycle check

The installed `E:\Kitten Space Agency\KSA.dll` is version 2026.9.7.5402,
source revision `487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
The existing matching decompilation under
`build/ksa-residency-reference/assembly-source` was inspected read-only; its
AssemblyInfo identifies that revision. No new KSA source/binary copies were made.

* `KSA/ConstraintSim.cs`, lines 834–880: collision preparation and solver stepping
  are distinct, with a fresh contact pass reset each time and poisoned-state handling.
* `KSA/PhysicsBubble.cs`, lines 664–700: results must be ready before explicit
  application to authoritative vehicles; synchronized application is separate.
* The same file, lines 1473–1489: game-owned conservative terrain-clearance horizon
  uses current state, acceleration bounds and a bounding sphere before expensive
  work. Its float solver/timestep and speculative margins are not exact roots.

The equivalent preparation, evidence lifetime and application responsibilities
are in KSA game code. Its reviewed call sites consume BRUTAL numerics/logging and
solver utilities; no BRUTAL-owned exact-root contract is asserted by this review.
This is a narrow lifecycle comparison, not a full engine or solver audit.

Authenticated official `live-changelog` entries were verified read-only:
[revision 4659, June 18](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885)
moved collision checks before final timestep selection to avoid excessive solver
work at high warp and protected higher-quality analytical vehicle positions.
[Revision 4866, July 8](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037)
added a prepass to prevent spurious speculative contacts and severe high-warp
hitching. The current installed source establishes the surviving separation.

**ADOPT/ADAPT:** preparation before expensive work; producer-owned fresh evidence;
explicit authoritative result application. **INTENTIONALLY DIFFER:** NovaCore's
exact-time contract needs a relation-owned root and honest unresolved comparison.
Float solver times, speculative margins, sleep thresholds and velocity zeroing
cannot establish that contract. No Bepu or vehicle-framework dependency was added.

## Red-team disposition

| Review | Strongest challenge | Result |
|---|---|---|
| A — root exactness | Finite rational epoch cannot represent every root; midpoint or descriptor tie could fabricate equality. | PASS: root denotes frozen relation; private construction; bounded explicit unresolved refinement/comparison; M14.6 untouched. |
| B — whole domain | Both endpoints can be inside while the middle exits; stored U is not exactly unit. | PASS: whole interval projected footprint and nonunit cone proof; excursion fixture refused; numerical branch uses original returned direction. |
| C — monotonicity | Earth pole/orbit terms or rounding could invalidate sampled approach. | PASS: current-source ODE/Euler derivative and outward bounds; no stencil sign premise; independent decimal oracle. |
| D — multiple/tangent | Positive endpoints may hide a dip or tangency; initial zero is not new contact. | PASS: whole positive range or strict monotonic theorem required; tangent/multiple/initial cases unresolved. |
| E — authority/replay | Direct state mutation without timeline revision could leave a usable proof. | PASS: independent revision checks and direct torque test; changed engine/force/graph/geometry/terrain refused; no persisted trusted flags. |
| F — cost/qualification | Unbounded refinement or repeated M14.8 sampling could conceal cost. | PASS: fixed 24/48 limits, bounded setup/scratch, zero reusable allocations, measured explicit refusal. Coverage and refinement limitations remain explicit. |

## Reproduction and retention

From `E:\NovaCore`, with the current canonical physical assets already deployed:

```powershell
dotnet build NovaCore.sln -c Debug --nologo
dotnet build NovaCore.sln -c Release --nologo
foreach ($configuration in 'Debug','Release') {
    $simulation = "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
    dotnet $simulation
    foreach ($gate in '--florida-proof-only','--translation-only','--contact-only',
        '--contact-response-only','--isolated-contact-only','--physical-event-epoch-only',
        '--exact-event-motion-only','--earth-event-only') { dotnet $simulation $gate }
    dotnet "tests/NovaCore.ReferenceFrames.Tests/bin/$configuration/net10.0/NovaCore.ReferenceFrames.Tests.dll"
    dotnet "tests/NovaCore.Precision.Tests/bin/$configuration/net10.0/NovaCore.Precision.Tests.dll"
    foreach ($test in 'Florida monotone contact certification',
        'Exact-event Earth-relative production terrain','Contact generation production terrain',
        'Physical surface-point') {
        dotnet "tests/NovaCore.Graphics.Tests/bin/$configuration/net10.0/NovaCore.Graphics.Tests.dll" --category=headless "--test=$test"
    }
}
git diff --check
```

Each process must succeed; stop and report a failure rather than retrying it into
a pass. Permanent fixture source contains exact setup, independent controls,
warmup, work limits and JSON measurement output. No asset generation, new renderer
run or profiler is part of reproduction.

Retained package budget: 48 KiB. Three files: this result/reproduction report,
[identity.json](identity.json) (13 source/test/project fingerprints and baseline
tags), and [validation.json](validation.json) (32 gates and decisive measurements).
No bulk stdout, dumps, temporary observer, comparison tree or duplicated assets.
New diagnostic scratch created/disposed/remaining: **0 files / 0 bytes**. Normal
rebuildable bin/obj outputs remain deployed; unrelated pre-existing scratch was
not inspected for cleanup or removed. Banking remains a Project Control decision.
