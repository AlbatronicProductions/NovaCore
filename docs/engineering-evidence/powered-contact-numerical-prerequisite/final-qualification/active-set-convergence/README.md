> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Saturated-to-interior finite-iteration convergence and payoff

Judgment: **PASS — CAUSE / PAYOFF RESOLVED**, limited to this bounded study.
Recommendation: **B. USE D + 12 SWEEPS GLOBALLY** as the next correction class for Project
Control review. Nothing is adopted or changed in the original candidate. Final prerequisite
qualification, powered continuation and production integration remain incomplete.

## What happened

The unchanged retained boundary diagnostic reproduced all nine non-process result files
exactly, including the baseline eight-sweep failure. See
[reproduction comparison](baseline-reproduction-check.json). Its original binary/input
hashes were checked before execution. No build was needed for that first reproduction.

An isolated source-generated diagnostic then parameterized only sweep count, explicit
initialization controls, optional D omission, and per-update observation. Transport algebra,
duration preparation, D arithmetic when enabled, friction model, sweep order and endpoint
arithmetic remained unchanged. Trace-on/off eight-sweep results matched the frozen result
exactly. No original production, permanent test, candidate or earlier evidence file changed.

Two diagnostic setup errors were retained rather than hidden: a vector-times-scalar compile
error was corrected before execution; the first new harness then refused StaleLineage before
any sweep because its loader constructed positive native zeros instead of reading saved
signed zeros. The reader now reads the original native bits. No authority check was relaxed.
[Initial harness stop](curve-results/stop.json), [corrected equivalence](curve-results-v2/harness-equivalence.json).
This was not a failure to reproduce the original candidate: that reproduction had already
passed. All subsequent curve runs start again from the same immutable accepted tuple.

## Cause

The historical tangent impulse, scaled into the new duration, is far from the new coast's
interior solution. Its magnitude is 0.16348929731429387 Ns, while the reference tangent
magnitude is 1.64783976758347e-8 Ns. Initial tangent error is approximately 9.92 million times
the tiny current tangent magnitude. Normal initial error norm is only 1.2311287657071913e-7 Ns;
the common component is 1.2554540740339348e-12 Ns per row. Initial twist mismatch is large
(0.18278659119421747 Nms), but the first scalar update removes almost all of its influence.

The ordered normal solve responds to stale tangent history through K_NT. The following tangent
block update feeds residual back into those normal rows. At sweep8, for example, tangent
updates change normal residuals by [0.00026713998326418004, -0.00026713998322529833,
-0.000046452337887692714, 0.00004645233792657443]. The first normal residual grows from
0.00036149207352386244 before the tangent block to 0.0006286320567880424 afterward.
K_NT * delta_t reconstructs all measured injections within 6.94e-18.

Independent ordered equations reproduce every recorded stage through sweep12 within
2.7755575615628914e-16 impulse units. Signed normal-common, normal-X, normal-Z, checker,
tangent and twist error components are propagated separately, then vectors are summed before
physical norms. At sweep8 the component reconstruction differs from measured impulses by at
most 4.991666802123262e-16. Tangent mismatch alone predicts normal-sum error
0.000284225181247688 Ns, linear error0.0002929228169495697 m/s and angular error
0.0005820025682717032 rad/s, accounting for essentially the full observed failure. The
initial twist component contributes only 4.47e-14 rad/s after eight sweeps. The rounded
reference fixed-point defect is retained explicitly; this is not a claim of bitwise linearity
of floating arithmetic. [Reconstruction and decomposition](causal-reconstruction.json),
[independent implementation](reconstruct.py), [every update](curve-results-v2/first-pass-trace.json).

Thus the cause is **finite-sweep tangent/normal coupled error from a large stale historical
tangent mismatch**. The saturated-to-interior transition produces that mismatch here, but
the exact boundary label is not itself a slow-convergence branch. D corrects a common normal
residual, not this tangent/differential coupled mode.

## Initialization controls

These controls preserve the actual current equations. They are diagnostic counterfactual
initializations, not fabricated claims of previously accepted physical history.

| Eight-sweep control | Normal error Ns | Linear error m/s | Angular error rad/s | Result |
|---|---:|---:|---:|---|
| Actual preparation | 2.842249782211148e-4 | 2.929225747067425e-4 | 5.820020859851548e-4 | FAIL |
| Half historical tangent/twist, normals preserved | 1.4211240354056187e-4 | 1.4646117320577537e-4 | 2.9100081542489706e-4 | FAIL |
| Zero historical tangent/twist, normals preserved | 1.711402131121531e-10 | 2.2848198313086032e-10 | 4.5533370909158136e-10 | PASS |
| Fully cold historical cache, with existing D | 7.50006723393426e-11 | 3.57492875824996e-11 | 6.905344848264362e-11 | PASS |
| Zero tangent after D, original normals/twist retained | 1.7111778660705568e-10 | 2.2846456744736604e-10 | 4.5529921942728497e-10 | PASS |
| One current tangent-block prediction after D | 6.664193641370275e-10 | 4.950057151049725e-10 | 9.767010649969438e-10 | PASS |
| Current-reference tangent, ORACLE ONLY | 2.030267065578073e-10 | 2.4243541838631674e-10 | 4.826065949278471e-10 | PASS |

All use zero normal/tangent/twist clamps. The half-friction history is strictly interior yet
still fails, disproving boundary equality as a necessary label for the accuracy failure.
The current-block prediction is -K_TT^-1*(J_T*v_free + K_T,notT*lambda_notT), not the reference.
Its bounded control passes; its admission/general-domain behavior is not qualified and it
is not adopted. Zeroing, cold start and the oracle are not proposed runtime corrections.
In the saved control records Preparation.AfterD precedes the explicitly named tangent
override; it must not be mistaken for the overridden starting cache.
[Full control results](curve-results-v2/initialization-controls.json).

## Active-set matrix

A separate manufactured-source matrix uses the same2x1x1 box dimensions and planar four-row
friction law, h=1/64, omega=128, p=1/64 per normal, depth3/65536. These numerical fixture
parameters are distinct from the unchanged actual tiny/coast reproduction. They make the
source equations transparent; they are not solver tuning or replacements for the actual
fixture. Independent acceleration/wrench loads are constructed from the prescribed free
endpoint; they do not claim Earth gravity or single-engine application-point provenance.

Prior interior and sliding-saturated solutions are independently constructed from the pinned
equations, used as analytical warm seeds, then solved and normally installed in the numerical
owner. Actual installed classifications are checked without epsilon. This is genuine
numerical-owner output/installation, not native-world acquisition or arbitrary initial-state
convergence. Compensating current loads account for each actually installed source endpoint.
Current free-state mismatch between history arms is exactly zero within each target column.

| Historical -> current reference | First tested physical PASS | Eight-sweep N / V / W errors | Eight-sweep T/W clamps |
|---|---:|---|---|
| Interior -> interior | 4 | 1.09611e-5 / 4.15821e-6 / 7.85270e-6 | 0/0 |
| Saturated -> interior | 8 | 2.19222e-5 / 8.31641e-6 / 1.57054e-5 | 0/0 |
| Interior -> saturated | 4 | 7.40457e-6 / 9.40809e-7 / 3.71788e-6 | 8/8 |
| Saturated -> saturated | 1 | 0 / 0 / 0 | 8/8 |

Both tangent and twist are independently included in the references, clamps and map residuals.
For saturated targets raw slip is intentionally nonzero; sourceMapResiduals are the guarded
projection/clamp residuals. The ordinary Measure.Residual remains labeled raw stationarity/slip
in these matrix records. Do not apply the inactive-friction affine reconstruction to those
active matrix rows. [Reference construction](matrix-results/references.json),
[actual histories](matrix-results/history-1.json), [all matrix measurements](matrix-results/active-set-matrix.json).

The matrix supports a mismatch-dependent convergence cost, not a universal rule that every
active-set change needs twelve sweeps or every saturated continuation is easy.

## Sweep convergence

Frozen physical bars remain exactly1e-4 for normal-sum, linear and angular errors. No tangent,
twist or residual tolerance is substituted for those bars. All actual-baseline counts had
0/0/0 projection clamps. Every count starts from the same prepared history, not from the
previous count's result. [All quantities/counts, including no-D](curve-results-v2/convergence.json).

| D + sweeps | Normal error Ns | Linear error m/s | Angular error rad/s | Result |
|---:|---:|---:|---:|---|
| 1 | .0337901172 | .0132886521 | .0252199432 | FAIL |
| 2 | .00974885783 | .00538681151 | .0105030282 | FAIL |
| 4 | .00702639259 | .00152502109 | .00249548230 | FAIL |
| 8 | .000284224978 | .000292922575 | .000582002086 | FAIL |
| 9 | .0000795388256 | .000177708733 | .000355154944 | FAIL |
| 10 | .0000117070673 | .000103555111 | .000207261209 | FAIL |
| 11 | .0000505774420 | .0000576187058 | .000114636563 | FAIL |
| 12 | .0000651509907 | .0000305789044 | .0000589978851 | PASS |

First tested passing count: **12**. Counts16/24/32 were unnecessary and not run. No universal
mathematical minimum or all-domain twelve-sweep guarantee is claimed. Some individual metrics
are nonmonotonic even as the coupled error contracts.

## D contribution and powered prediction

No-D also first passes at12. At8 it gives N=.00028439627317311533, V=.00029295770083243304,
W=.0005820676813932491; at12 N=.00006505265446477004, V=.0000305929867571265,
W=.00005903391816652841. D slightly improves V/W here but can worsen the absolute normal-sum
metric at some counts. Its effect is small and does not save a tested sweep. Removing its
common delta predicts the measured no-D-minus-D trajectory within1.71e-16.

The powered saved cache/source/geometry/duration match baseline; only previous load differs.
Current coast equations are identical. Its load correction adds a common shift s*q, which D
annihilates in exact arithmetic: D(x+s*q)=D(x). The same tangent mismatch and approximately the
same convergence are predicted, subject to represented cancellation rounding. No powered
Prepare/solve was executed; no powered PASS is claimed. This argument does not predict
no-D powered equality. [Saved-input prediction](powered-prediction.json).

## Local Release payoff

Three fresh processes, each128 warm operations then1,024 measured operations per arm,
interleaved with rotating order. Timed boundary: full numerical admission/transport/exact
duration/load/D-or-no-D/current-feasibility/sweeps/endpoint-proposal export. Trace calls are
removed, reporting and validation are outside timing, and accepted input remains unchanged.
No native world, solver refresh, installation, publication or canonical mutation is included.
This is not integrated performance qualification. No new GC/tiering settings or retries.

All values are microseconds:

| Process | Arm | Median | P95 | P99 | Max |
|---:|---|---:|---:|---:|---:|
| 1 | D+8, accuracy FAIL | 43.8 | 46.9 | 73.7 | 140.8 |
| 1 | D+12 | 59.8 | 66.4 | 101.0 | 122.5 |
| 1 | no-D+12 | 59.5 | 65.1 | 108.0 | 144.1 |
| 2 | D+8, accuracy FAIL | 44.2 | 46.0 | 65.3 | 89.8 |
| 2 | D+12 | 60.4 | 62.9 | 93.2 | 119.2 |
| 2 | no-D+12 | 60.1 | 62.2 | 91.8 | 827.8 |
| 3 | D+8, accuracy FAIL | 43.7 | 46.0 | 71.7 | 882.4 |
| 3 | D+12 | 59.5 | 63.8 | 97.6 | 123.4 |
| 3 | no-D+12 | 59.2 | 63.5 | 101.1 | 150.4 |

Median-of-process-medians: D+8=43.8us, D+12=59.8us, no-D+12=59.5us. Adding four sweeps
costs15.8-16.2us at the median (about36%). Omitting D saves0.3us per process here, insufficient
evidence to discard its separately proven earlier responsibility. Large maxima remain in
the report. Process2 shows a shared tail cluster around samples731-744; process3 also has
several nearby tails around679-689. There is no exact recurring slow sample across processes,
and no runtime/JIT/GC explanation was established. Do not assign one from these timings.

Prior3.4us/3.9us evidence used a different boundary and cannot replace these current results.
The ~60us local median is a material future integrated-cost consideration, not a claim that
an older50us complete-path ceiling passes. No integrated gate was run or relaxed.
[Process1](payoff-1/payoff.json), [process2](payoff-2/payoff.json), [process3](payoff-3/payoff.json).

## Correction-class comparison and recommendation

| Class | Correctness evidence | Cost / complexity | Disposition |
|---|---|---|---|
| S: D+12 globally | First tested actual PASS; no classifier; original equations | +about16us local median; no new retained state or specialized predictor | **Recommend for next scoped correction/requalification** |
| T: current tangent initialization | One independently derived block prediction makes actual8 pass | Extra block/preparation ordering and feasibility/general-domain proofs; no claimed measured T cost | Not selected from one favorable control |
| C: conditional sweeps on exact transition | Current reference active set is unavailable before solve; half-interior history already fails8 | Needs a new proven classifier or heuristic; exact historical status alone is insufficient | Reject proposed exact-transition rule |

The retained prerequisite already carries generalized basis transport, producing lever and
duration provenance, duration transformation, common-normal load correction, D and two-stage
friction feasibility. Four additional ordinary sweeps introduce fewer new mechanisms than
another specialized initialization rule. This is a class recommendation, not proof that
12 passes every remaining fixture. Eight is not a correct budget for this actual witness;
this study does not establish a globally cheapest solver over all possible predictors.

No correction has been implemented. Any accepted D+12 change still requires Project Control
authorization and the unfinished bounded qualification. No hidden conditional thresholds,
near-boundary epsilon, tangent reset, cold fallback or oracle is proposed for production.

## Verification, identity and reproduction

See [independent verification](verification.md), [identity](identity.json), and
[reproduction routes — retired record](../../../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md). Source-generated diagnostic arithmetic is checked,
prior evidence remains intact, and result/error reconstruction is independent. The corrected
curve harness performs26 numerical calls: two equivalence calls,16 count calls, one12-sweep
trace, six controls and one8-sweep equation export. The last is not preparation-only. Matrix
performs20 calls including actual prior solve/install preparation. Timing performs3,456
calls per process plus three untimed comparison calls. None installs the actual coast.

Reproduction modes are explicit and require a fresh output directory: `Original` first,
then `Curves`, `Matrix`, and separately `Payoff`. The script checks retained input/source
hashes and SDK/runtime before execution. The original reproduction's expected failure exit
is not a PASS by itself: compare its nine non-process results. Build the payoff driver before
the first process and invoke the existing DLL directly for orders2/3, without intervening
builds. `reconstruct.py` consumes only saved curve results. Copy it to a fresh directory
with the curve output named `curve-results-v2` to avoid overwriting retained evidence.

## Repository and evidence boundary

HEAD, main, origin/main and remote main remain
`49057fecceb0f725d5f551ec40e2780971b0d81d` on
`codex/powered-contact-numerical-prerequisite`. All2,224 pre-existing nonignored files,
including274 production paths /267 production C# files, retain their hashes. All65 tag refs
are unchanged. The index and tracked diff remain empty; both diff checks pass. The existing
ten untracked roots remain. This study adds files only inside this new evidence directory.

Retain source, bounded result traces, arithmetic reconstruction, references, identities,
the three summarized payoff records and reproduction routes. The new `bin`/`obj` products
are disposable; their exact cleanup disposition is recorded in `identity.json`. Earlier
evidence and previously blocked cleanup paths are untouched. No bulk trace/profiler capture
or additional comparison tree was created.

UNBANKED. Historical refs, M14.21-M14.24, M15.0 and Blender remain untouched. No production or
permanent test changes, stage, commit, tag, push or remaining qualification campaign.

## What happens next

**STOP FOR PROJECT CONTROL.** Review Class S / D+12 before authorizing any correction. Do
not automatically resume qualification, implement live powered contact/departure, assign
M15.1 or bank.
