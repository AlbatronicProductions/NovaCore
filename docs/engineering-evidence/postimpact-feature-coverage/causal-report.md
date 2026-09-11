# Real Florida coverage causal result — unbanked

Baseline: `dc274639f670d122764bb0e8f3d0c95ae201c7a4`, branch `codex/postimpact-feature-coverage`.
This report supersedes the previous validation stop. Exactly one production correction was made in the new candidate's FloridaPostImpactGap. No banked production file changed.

**Lead judgment: REVISE.**
**POST-IMPACT TERRAIN-CONTACT COVERAGE CANDIDATE — FOLLOW-UP REQUIRED.**

Coast now certifies useful real Florida coverage. Force remains conservatively unresolved; no second correction was attempted. Positive opaque provider-owned next-root read/refinement/ordering remains unqualified by a real fixture. Analytical search-root tests and the prior source-extracted comparison witness are not equivalent to that runtime proof.

## Coast: decisive interval and width

The original search certified sigma [0, 0.03125], then exhausted depth24 at
[0.03125, 0.031250059604644775], visits26/evaluations66. Linked time was
[0.5188767910003661, 0.5188846884157102] seconds; elapsed from alpha was
[0.015519857406616197, 0.015520125427428883].

Gap: [-0.021017260849475864, 0.021032934077084068] m.
First derivative: [0.000899778658905878, 0.0009059974579053721] m/s.
Second derivative: [0.05758433271033881, 0.05758436584979832] m/s².
The left endpoint also straddled zero: [-0.020938443019986156, 0.02095411811023951].
Positive derivative therefore could not prove a positive left endpoint, and cannot certify an approaching root.

Current-leaf projection/grading/clamp guards all passed. East was
[-0.02759511070325971, 0.02168057451490313], North
[-0.01082877814769745, 0.01210821606218815], Up
[6371023.913874999, 6371023.955925192]. Exact operands, source identity,
quaternion/spin, derivatives, absolute/relative coordinates and plane guards are
retained in coast-intervals.json.

| Diagnostic | Gap width, m | Interpretation |
|---|---:|---|
| A: original certified expression | 0.04205019492655993 | Unresolved |
| B: same natural interval expression at 60/90 decimal digits | 0.04204921061716646 | Extra precision removes only about 0.00234% of width |
| C: central alpha, diagnostic only | 0.00015858747065067 | About 99.62% narrower, still straddles zero; not an admissible authoritative time |
| D: independent Earth oracle | Scalar physical evaluation available; rigorous replacement interval UNAVAILABLE | No narrower Earth certificate was substituted |
| E: independent rotation oracle | Scalar Euler/quaternion evaluation available; rigorous replacement interval UNAVAILABLE | No sampled attitude was substituted |
| F: exact root-correlated scalar identity | [1.2350890432978456e-6, 8.621361072623561e-6] | Positive decisive-leaf inclusion using checked source residual |
| G: same plane/trajectory physical oracle | +7.034483493450475e-6 at sigma 0.03125 | Physical trajectory is clear; unchanged grading plane |

Direct high-precision interval sin/cos (a DIFFERENT expression, not precision-only)
reduces width to 0.003682100803012656 m and still straddles zero. It does not replace B.
Naive independent absolute orbital boxes produce about 0.493119 m width;
production ALREADY differences Earth/spacecraft positions and velocities before
widening. It is incorrect to blame the existing implementation for that naive expression.

Pre-body relative widths were approximately 0.001084264, 0.003033417,
0.000004004 m. Body X/Z widths grew to 0.0430924/0.04141635 m. Earth
Taylor position remainder was 2.211321677052856e-17 m, spin remainder
7.852577252495512e-32, attitude remainder 6.288312974309357e-30.
Dominant loss is root/time correlation through local motion, Earth orientation
and plane projection, including natural interval trigonometric dependency.
It is not principally IEEE rounding or ODE remainder.

## Independent physical oracle

physical-oracle.py interprets exported binary64 values via exact integer ratios.
It independently solves the same two-body Earth motion, orientation, source
contact root and torque-free Euler/quaternion flow, retaining the M14.13 final
velocity bits, force, feature, inertia and plane. It never supplies its numerical
alpha to production. The predeclared 60-digit/degree40 and 90-digit/degree64
runs agree in signs and approximately 40 digits for the tiny Force root.

Coast: initial normal speed +8.022462701941074e-7 m/s, acceleration
+0.05866652397858787 m/s², target gap +0.00580738503481044 m.
Classification: COAST_PHYSICALLY_CLEAR. Sampling alone is not a certificate;
the production interval theorem below independently proves coverage.

Force: initial normal speed +2.695731202092664e-6 m/s, acceleration
-0.13473251139190677 m/s². Next contact is approximately
40.015680734355 microseconds after alpha (sigma 7.695550045915688e-5).
Gap at half that elapsed time is +2.696800294e-11 m; at 1.5 times it,
-8.090474777e-11 m. Classification: FORCE_STRICTLY_DEPARTS, followed by
very early reacquisition. This is not immediate penetration. Scalar results
are diagnostic physical evidence, not a provider-owned next-root receipt.

Input hashes and precise results/limitations are in physical-oracle-summary.json.

## Force: departure operands

Original alpha derivative interval: [-2.7458084451287284e-7, 5.638120856327201e-6].
First depth24 interval derivative: [-2.987344296032091e-7, 5.658098571809101e-6].
Its curvature: [-0.13473251572085723, -0.13473250991849076].
The checked source has a tiny strictly positive residual lower bound
3.4948618737827324e-12 m/s, upper 5.36353530052859e-6 m/s. The old
gap evaluator loses strict positivity, and negative curvature prevents its
initial nonnegative-speed/nonnegative-curvature departure theorem.

At alpha, projected components are:
- COM velocity [25074.88684677756, 25074.887037363835];
- feature rotation [1.0617502010119397, 1.061750206439118];
- Earth translation [-25075.948784184347, -25075.948594917892];
- Earth orientation [-1.5652834548518515e-6, 1.48222653706398e-6];
- COM acceleration [-0.20000000051521183, -0.1999999992959252];
- angular acceleration [-1.6651472757069077e-10, -9.996464506861756e-11];
- centripetal acceleration [0.03873941183443915, 0.03873941292515689];
- Earth translation acceleration [0.0004375802861038835, 0.00043758032230174743];
- Earth orientation acceleration [0.026090495647509395, 0.02609049628223319].

These separately enclosed diagnostic velocity components must NOT be treated as
the production expression: production preserves some cancellation before widening.
The full operands are retained in force-intervals.json.

## Physical versus certificate

| Fixture | Physical evidence | Original certificate | Corrected certificate | Causal responsibility |
|---|---|---|---|---|
| Coast | Clear; positive curvature and target gap | RootUnresolved after sigma .03125 | EventFreeThroughTarget, visits63, depth5, evaluations63 | Avoidable root/time correlation loss |
| Force | Strict tiny departure, then very early reacquisition | DepartureUnproved, visits25/depth24 | Same conservative refusal | Thin physical margin plus insufficiently sharp certified initial departure information |

No evidence justifies treating Force as physically nondeparting. Its unresolved
answer is safe, but does not prove absence of a next root. Resolving that proof
would require separately authorized work; no limit increase or easier fixture.

## One bounded correction and payoff

HIGH payoff for Coast: retain exact g(alpha)=0 and inherited g'(alpha)>=0 in
the same fixed-plane gap responsibility. Cache an interval slope at sigma0 and
the existing trajectory's global plane acceleration interval A. Intersect:

g(alpha+s) in s*(g'(alpha)+s*A/2)
g'(alpha+s) in g'(alpha)+s*A

with the raw gap/derivative intervals. Preserve the raw local second derivative.
The global Coast A is [0.01025540443882647, 0.0713826059570931].
Production conservatively uses max(0, raw initial slope lower), not a guessed
alpha or a new public M14.13 capability. The decisive Coast lower bound remains
positive even with initial slope lower zero.

The integral Taylor identity encloses all intermediate times. A concerns the
globally defined algebraic plane extension; actual terrain equivalence is still
checked on every current search leaf. The global wide grading box need not pass.
Empty/nonfinite intersections refuse; a contradictory negative initial slope
refuses before optional global-bound fallback. Failed optional wide trajectory
bounds retain the old conservative proof. Search/refinement limits, source
identity, receipt authority, body domain, initialization, motion and target
remain unchanged. No solver, Bepu, impulse, publication or event execution change.

Production change: only FloridaPostImpactGap in the candidate
PostImpactCoverageProvider.cs. Tests add exact-rational quadratic containment,
positive/negative departure, intersection and contradiction controls, plus
bounded cost qualification and selectors. Temporary reflection/source-export
instrumentation was removed before final production/test validation.

## Validation and costs

causal-validation.json contains all results and identical repeated witnesses.
- Full Debug and Release solution builds: zero warnings/errors.
- Three fresh focused processes/configuration: all pass, identical proof output.
- Full Simulation: 47/47 groups Debug and Release.
- ReferenceFrames: 11/11 Debug and Release; Precision: both pass.
- All 14 focused predecessor selections/configuration pass.
- Real Florida coverage and surface-query/stale-snapshot cases pass in both.
- Source, singleton, nonzero spin, terrain identity, refusal and canonical
  nonmutation checks pass. The unchanged Force refusal is explicit, not a skip.

Costs use 32 warmups, 101 ordinary-runtime timing samples and a separate checked
allocation window of eight calls. No profiling or runtime tuning. All nine
reusable paths report zero bytes in Debug/Release. Setup owner admission reports
122240 bytes/8 = 15280 bytes/owner. Each of four cost processes detects the
byte[128] positive control as 152 bytes.

| Release path | Median us | P95 us | P99 us | Maximum us |
|---|---:|---:|---:|---:|
| Owner admission | 229.9 | 258.5 | 273.6 | 283.5 |
| Reusable interval | 141.6 | 159 | 166.9 | 167.2 |
| Real Coast certification | 1182.1 | 6826.5 | 7904.7 | 8662 |
| Checked read | 2.9 | 4.5 | 5.2 | 5.3 |
| Foreign refusal | .2 | .3 | .3 | .3 |
| Invalid interval refusal | 2.9 | 2.9 | 3 | 4.3 |
| Analytical next-root search | 1.9 | 2 | 2.1 | 2.1 |
| Depth exhaustion | 1.9 | 2 | 2 | 2 |
| Domain refusal | 1.3 | 1.3 | 1.3 | 1.3 |

Debug Coast median/P95/P99/max: 8893.4/9091.9/9481.2/9575.6 us.
Release variability is recorded, not attributed to JIT/tiering without evidence.
These are CPU provider costs, not total-frame GPU times or high-warp acceptance.
Each real search adds two setup trajectory evaluations; search itself uses63
Coast or25 Force evaluations. Coverage performs no terrain point queries.
Search stack600B, receipt56B, root40B, values168B. The visit255 cap is a
declared bound, not a benchmark of a 255-visit exhausted run.

## Review and remaining responsibility

Independent numerical and source review PASS: root identity unchanged, same
trajectory, global curvature scope sound, local grading mandatory, intersection
only, contradictory/nonfinite evidence refused, no new authority or work limit.
Strongest objection was applying a global plane bound while the global terrain
box failed grading; it survived because only the plane extension supplies
curvature and every actual search leaf retains its original terrain check.

No Bepu gate is met: this is a certificate dependency problem, not evidence
requiring replacement of the solver/contact architecture.

Follow-up remains bounded to real next-root receipt/refinement/ordering
qualification and the Force departure proof limitation. One production
correction is complete; this ticket does not implement a second. The measured
cost tail must also remain visible to Project Control.

## Reproduction and evidence lifecycle

Current correctness:
```powershell
dotnet build NovaCore.sln -c Debug --nologo -v:q
dotnet build NovaCore.sln -c Release --nologo -v:q
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --postimpact-coverage-only
dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll '--case=Post-impact Florida terrain-contact coverage'
```
Use Release path equivalently. Remove selector for full Simulation.
Cost selectors: Simulation --postimpact-coverage-cost; Graphics
--case="Post-impact Florida coverage costs". Query/stale case names and
predecessor selector names are in causal-validation.json.

Independent numerical reproduction requires Python with mpmath1.3.0:
```powershell
python docs/engineering-evidence/postimpact-feature-coverage/physical-oracle.py docs/engineering-evidence/postimpact-feature-coverage <disposable-output.json>
python docs/engineering-evidence/postimpact-feature-coverage/natural-interval-oracle.py docs/engineering-evidence/postimpact-feature-coverage <disposable-width-output.json>
```
The first regenerates precision-paired samples; the second replays the retained
decisive source interval at both precisions. Neither issues production evidence.

To reproduce the ORIGINAL certified search, use a disposable candidate copy.
Replace only FloridaPostImpactGap with pre-correction-gap.cs.txt; do not replace
the rest of its file. Copy coverage-causal-diagnostic.cs.txt to a .cs file inside
the disposable Graphics tests. Immediately after the fixture's owner.Evaluate,
read final.Realization with (root,k.Witness,response.Proposal,Response,pre.Tuple,Preimpact,c.Use)
into final values, then call CoverageCausalDiagnostic.Capture(force,c,preProvider!,
source,continuation,owner,staged.State,finalValues). The helper writes to the
explicit scratch directory named at its top. It reruns the same search through
a reporting wrapper and checks equality, exports exact source bits, and runs
central-alpha/anchored diagnostics only. Rebuild that disposable Debug variant.
Never apply this instrumentation or pre-correction class to canonical source.

Retain exact input hashes, decisive source intervals, numerical scripts/summary,
the compact pre-correction class needed for original reproduction, validation
matrix and final identity. Omit bulk trajectories, repeated stdout, build
trees, copied Python dependencies and redundant backups. No banking.

## Scratch disposition

The new reviewed scratch tree coverage-causal contains244 files/7418130 bytes.
One guarded deletion attempt was rejected before execution as blocked by policy;
no retry or bypass. The prior coverage-comparison-control remains28 files/441530
bytes after its previously reported block. Total reviewed disposable remaining:
272 files/7859660 bytes. No scratch files were disposed this turn. Temporary
compiled test instrumentation was removed before rebuilding the final candidate.
The unrelated closed allocation investigation tree was neither modified nor
included in these counts. Standard solution bin/obj outputs are not evidence.

Project Control can manually remove only these reviewed directories:
```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\coverage-comparison-control','E:\NovaCore\.codex\coverage-causal' -Recurse -Force
```
