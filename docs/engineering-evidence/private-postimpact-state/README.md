# Private post-impact initial state candidate

Baseline: banked M14.13 `704c77f1aab899ccf3452ee3fc88471d1e311aca`.
Development branch: `codex/private-root-postimpact-state`. UNBANKED.
Accomplishment: **Construct private post-impact spacecraft state at provider-owned contact roots**.

## Declared proof and storage budget

Permanent evidence budget: 64 KiB, covering this report, concise machine-readable
validation/identity results and decisive numeric witnesses. Source and permanent
tests reproduce the evidence. No disposable comparison tree, profiler or bulk
runtime capture is planned. Build output stays in existing ignored build locations.

Before execution, the sequence is fixed: full Debug and Release solution builds;
focused analytical and production-Florida new tests in each configuration;
complete Simulation in each; focused affected M14.2–M14.13 gates, ReferenceFrames,
Precision and Florida query/stale-source integration in each; final diff check.
Stop on the first material failure, with no automatic attribution or retries.

The Florida pose request is a 0.5 m **full component enclosure width**, not a
contact tolerance or a selected pose. The existing source request bounds root
width by 1e-5 s, with these fixtures' component speeds below 4e4 m/s; the remaining
width accommodates outward arithmetic at Earth inertial coordinates. Qualification
must refuse rather than enlarge this request. Analytical controls independently
bound exact rational quadratic extrema; their declared requests appear in the
permanent tests. A bracket for sqrt(1/2) is a genuinely irrational analytical
control, separate from actual Florida provider-root identity.

Performance plan: 32 warmups, 101 default-runtime one-call timing samples (median,
P95, P99, maximum), followed by 8 calls in the qualified checked no-GC allocation
helper. Source reconstruction is separately reported allocating setup. Construction,
checked reads and stale/invalid/numerical refusals require exact zero. Positive
control must detect a real byte[128]. No timing occurs inside allocation isolation.

## Contract and reproduction

See [current candidate contract](../../private-postimpact-state.md). New focused
commands after `dotnet build NovaCore.sln -c Debug` and `-c Release`:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --private-postimpact-state-only
dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll '--case=Private Florida post-impact initial state'
```

Repeat with `Release`. Florida uses the unchanged canonical runtime Earth assets
and physical grading authority. [validation.json](validation.json) records all
54 executed commands and outcomes, bounded results and complete decisive witnesses.
[identity.json](identity.json) records source fingerprints and unchanged tag objects/targets.

## Ownership review and source provenance

Static review found no new canonical writer, time conversion, root-refinement owner,
response-selection call or heap-owning cache. A receipt contains frozen value data
and upstream checked receipts, which reference the same existing provider graph.
That graph retains the engine, geometry, physical terrain and frame graph; holding
the private receipt can extend their managed lifetime. Checked applicability, not
garbage collection or a new generation counter, determines readiness. No concurrency
or publication guarantee is claimed by a successful read.

Current local KSA source was inspected read-only at
`build/ksa-residency-reference/assembly-source/KSA`: `VehicleUpdateData` constructor
and `Prepare` initialize complete private kinematic/origin state; `Vehicle` applies
task results through a distinct game-owned path. The installed
`E:/Kitten Space Agency/KSA.dll` SHA256 is
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`, matching the
existing [source provenance](../certified-root-contact-kinematics/identity.json),
version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`.
The `VehicleUpdateData`, `VehicleUpdateState` and `PhysicsBubble` source hashes were
rechecked against that provenance. No broad historical investigation was necessary
for this already-approved boundary. Adopt captured preparation/private staging;
adapt ownership to NovaCore's checked provider lifetime; intentionally preserve
exact root semantics and withhold sequential/canonical publication. No general
contact-solver conclusion follows.

## Final result

**PASS — PRIVATE ROOT-TIME POST-IMPACT STATE CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE.**
UNBANKED. All 54 declared commands pass, zero failures/skips/unreached gates.
Both full solution builds have zero warnings/errors. Full Simulation is 45/45 in
each configuration; all twelve affected milestone-focused gates pass in each;
ReferenceFrames is 11/11 each, Precision passes each, and all nine existing selected
production-terrain/query/stale-source gates pass in each. New analytical and Florida
tests pass Debug/Release. `git diff --check` passes with line-ending conversion
warnings only. No existing production file changed (232/232 byte-identical).

Both Florida controls pass exact-rational whole-bracket COM containment and independent
exact-normalized-attitude lever containment. Maximum position full widths are
0.22621917724609378 m (X), 0.04133605957031251 m (Y), and at most
0.016662597656250003 m (Z), below the predeclared 0.5 m request. These are enclosure
widths, not errors of a selected pose. Debug/Release **entire witness records are
identical**, including source values, root/pose bounds and all final velocity bits.
Independent provider replay and repeated reads match. Nonzero spin and inertia
(2,3,4) remain intact. Default/foreign/refined-mismatched receipts, changed requests,
stale engine/frame/terrain/geometry/state/timeline/time, force/torque and invalid
mass/inertia refuse without mutation. Snapshot checks include 1,000 ticks of pending
debt, both state components, properties, clocks/revisions and history counts.

The existing unsupported-arithmetic guard is source-reviewed on the qualified
.NET 10.0.12 Windows x64 host; no different-runtime or arithmetic-mode injection was
performed. Renderer nonmutation is established by the absence of a renderer or
publication path; no unrelated GPU/window campaign was run.

## Bounded cost

Times below are microseconds per call. Allocations are **total bytes over eight
calls**, measured separately. The source-chain setup is explicitly allocating;
all five reusable paths are exact zero. The positive byte[128] control detects
152 bytes in each configuration. All no-GC entry/exit checks pass.

| Configuration | Path | Median us | P95 us | P99 us | Max us | Bytes / 8 calls |
|---|---|---:|---:|---:|---:|---:|
| Debug | source reconstruction | 4350.2 | 4445.0 | 4495.8 | 6737.3 | 13,376 |
| Debug | private state construction | 5.5 | 5.5 | 5.6 | 5.8 | 0 |
| Debug | checked read | 6.6 | 10.4 | 11.4 | 12.0 | 0 |
| Debug | invalid pose request refusal | 4.4 | 6.1 | 6.9 | 7.2 | 0 |
| Debug | numerical pose refusal | 5.2 | 7.7 | 8.7 | 9.2 | 0 |
| Debug | stale source refusal | 1.6 | 2.5 | 2.7 | 2.7 | 0 |
| Release | source reconstruction | 3439.4 | 3472.3 | 4563.4 | 4865.3 | 13,376 |
| Release | private state construction | 7.0 | 7.5 | 7.6 | 7.9 | 0 |
| Release | checked read | 12.5 | 12.9 | 13.0 | 13.1 | 0 |
| Release | invalid pose request refusal | 6.8 | 7.0 | 7.1 | 33.1 | 0 |
| Release | numerical pose refusal | 7.1 | 7.3 | 7.4 | 7.5 | 0 |
| Release | stale source refusal | 2.4 | 2.5 | 2.6 | 2.6 | 0 |

Receipt size: 4,656 bytes; checked data snapshot: 504 bytes; construction result:
4,664 bytes; pose request: 8 bytes. These are managed value-layout sizes, not a
new heap graph. No new provider objects, root refinements or response selections
occur in construction/read. Source reconstruction allocates 1,672 bytes per call.
Value copies and upstream chain checks are part of these timings. Release being
slower than Debug for some very short paths is reported unchanged; these bounded
default-tiering samples do not establish long-term steady-state ranking or justify
an optimization investigation. No whole-frame or general physics cost is inferred.

## Adversarial review

- Numerical review: **PASS**. Exact rational endpoints/interior extrema independently
  test the entire quadratic range. Tight requests refuse; no midpoint or represented
  pose is selected. The irrational control is mathematical, not a claim that the
  actual Florida root's irrationality was independently established.
- Authority/lifetime review: **PASS**. Frozen values plus complete checked issuing
  identity retain one alpha, source epochs and final bits. Same-owner refinement
  does not revoke old evidence; reissued tuples cannot borrow readiness. Refusals
  expose no partial state. No new canonical writer or linked remainder exists.
- Cost/scope review: **PASS**. The nested receipt is large but bounded and retains
  only the existing provider graph. It does not create a second authority or enter
  persistent-contact/solver responsibilities.

Storage: three concise evidence files retained within the declared 64 KiB budget.
No disposable comparison tree, temporary source or raw on-disk diagnostic logs
were created; disposable evidence remaining is **0 bytes**. Ordinary ignored build
outputs remain in their established build directories.
