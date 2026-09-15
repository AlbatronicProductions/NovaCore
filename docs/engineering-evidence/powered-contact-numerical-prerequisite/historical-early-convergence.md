> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Eight-sweep powered-load convergence study

**PASS — CAUSE PROVEN / CORRECTION RECOMMENDED.**

The numerical prerequisite candidate remains **UNQUALIFIED and UNBANKED**. No production correction or candidate-kernel change was made. This study recommends a bounded next correction class for Project Control; it does not resume operator qualification.

## Baseline and exact reproduction

HEAD/main/origin/main/remote main: `49057fecceb0f725d5f551ec40e2780971b0d81d`. Branch: `codex/powered-contact-numerical-prerequisite`. All seven prior diagnostic source files matched their retained hashes before execution. The unchanged candidate was rebuilt in a separate artifacts directory. Its original failure reproduced **byte-for-byte**, including geometry, seven initial impulses, mass, load, source velocities and errors: result SHA256 `C4F6EFA2C2F7F60F0DCDBFFAFF847AFD145FE275951F751AE3F9C2D94E526E22`. Exit 1 is the expected failure witness, not a passed operator.

The isolated study source-links that unchanged candidate. Reflection is used for diagnostic access to its existing private methods; no solver rewrite supplies the convergence curve. A prepared typed delegate removes per-call reflection from the separate cost estimate.

## Convergence curve

Every integer count 1..32 was evaluated from the same captured local system and original warm guess. The requested subset follows; all three error bars remain 0.0001 in their respective units.

| Sweeps | Normal impulse error, N s | Linear error, m/s | Angular error, rad/s | Max equation residual |
|---:|---:|---:|---:|---:|
| 1 | .123496830 | .016985056 | .014213262 | .033083497 |
| 2 | .024049520 | .005833614 | .010008835 | .014047778 |
| 4 | .004466977 | .001296754 | .002342963 | .004421038 |
| 8 | .002512790 | .000637809 | .001111305 | .001864534 |
| 12 | .001442301 | .000422079 | .000763990 | .001250932 |
| 16 | .000861241 | .000257309 | .000467848 | .000761386 |
| 24 | .000306313 | .000092009 | .000167483 | .000272085 |
| 32 | .000108814 | .000032695 | .000059518 | .000096681 |

**No count through 32 passes all three bars. No minimum passing count was established.** The normal-sum error temporarily crosses at sweep 3 while both velocity errors fail, then increases at sweep 4. Angular error and equation residual also increase from sweep 6 to 7. Do not claim globally monotonic convergence. Later errors decay smoothly; this does not prove divergence or justify extrapolating/adopting an untested count.

## Initialization and load decomposition

| Eight-sweep initialization | Normal error | Linear error | Angular error | Result |
|---|---:|---:|---:|---|
| Original preserved cache | .002512790 | .000637809 | .001111305 | FAIL |
| Zero cache | .004162816 | .001064049 | .001858094 | FAIL |
| Exact reference, diagnostic only | 1.11e-16 | 1.31e-17 | 4.30e-17 | PASS |
| Derived additive common-load correction | 3.05317e-6 | 7.74820e-7 | 1.34995e-6 | PASS |

Preserved warm starts help relative to cold. They are not generally harmful and must not be cleared blindly. The exact reference guess is an ideal diagnostic, never a proposed runtime method.

With the same midpoint mass, eight-sweep normal errors at 0/8/16/32 N are respectively 3.05317e-6 / .000630487 / .001257922 / .002512790 N s. Those cases are unclamped and the signed error varies affinely with load. At 64 N the normal error is .004824612 N s and two tangent projections occur: a mild active-set bend, not one unchanged affine regime over the entire load series. The correction was tested only at 32 N.

The missing unchanged-mass/32 N case gives .002509483 N s, .000636817 m/s and .001109667 rad/s, almost the combined result. Existing unchanged and mass-only controls are reused. Signed factorial decomposition puts the combined normal-sum error into approximately -3.05195e-6 mass contribution, -.00250948210 load contribution, and -2.54873e-7 interaction (plus the tiny unchanged residual). Wrench change dominates; mass-only failure is not demonstrated.

## Proven numerical cause

**The legitimate centered load change creates a common normal-reaction mismatch in the preserved cache. The unchanged ordered iteration transfers that mismatch into weakly damped normal-pressure and coupled moment/friction error that eight sweeps do not remove accurately enough.**

The source applies the new wrench once. No extra force, unintended load ramp/discontinuity, stale-state integration bug, equation error or policy-order mismatch was found. Finite-iteration truncation, initialization mismatch and coupling are parts of this causal chain, not mutually exclusive labels. Eight sweeps are not intrinsically incapable: the derived initializer passes without changing them.

For the rectangle's normal rows, the checker pattern (1,1,-1,-1) produces zero net force and torque. It is regularized by normal spring softness, not rigid effective response. The ordered affine map takes a pure common-load error with zero checker component to checker -0.0375602 N s after one sweep and -0.0371330 after eight, compared with actual -0.0371782. Adding the independently retained mass-only error to that pure-load propagation reconstructs every actual eighth-sweep impulse error within **6.01e-13**. This is direct attribution through the measured system's iteration map, not a visual inference from the pattern.

The large checker component is self-stress; it does not directly rotate the body. Observable error is mainly the z-offset normal imbalance coupled to physical Z friction, producing vx-negligible / vy=-.000313945 / vz=-.000555192 m/s error and wx=-.001111305 rad/s error. At sweep 8 the tangent block reduces its own residual from .000129575 to approximately 2.17e-19, while perturbing earlier normal equations; the last normal equation moves from near zero to -.000139255. Normal maximum residual remains .001864534. Twist is negligible. Thus the tangent block works locally while the coupled system remains unconverged.

The component reconstruction agrees with all 32 already-measured candidate iterates within **1.67e-16**. No row was reordered, friction disabled or new physical trajectory run for this attribution.

## Independent centered analytical witness

An independently derived common/differential normal-mode solution and two 2x2 moment/friction systems use the captured four depths and initial velocities, rather than the candidate's shared seven-row geometry assembly. Flattening only the ~7e-11 normal tilt has a predeclared 1e-8 comparison allowance.

Analytical normal sum is **.8206860380686618 N s**, differing from the seven-row reference by 2.22e-16; the generalized velocity difference is 4.19e-13. The frozen-midpoint momentum balance is:

`J = (m_mid*g - T)*h + m_mid*(v_end - v_source)`.

Its terms are .7753053385416668 + .04538069952699507 N s, with balance error 9.71e-17. The body starts with a compressed compliant support patch and unloads upward after ignition. Comparing it directly with motionless rigid support would wrongly condemn the reference. This is a local midpoint-model crosscheck, not continuous changing-mass trajectory qualification.

## Correction recommendation

Recommend **CLASS C — load-transition-specific common-normal-mode preconditioning, retaining eight sweeps** for a separately reviewed bounded implementation.

For this centered 0→T load change, add to each normal initial impulse:

`delta_lambda = -h*T / (4 + m*alpha*Kd)`

where `alpha = 1/[omega*h*(omega*h+2)]` and `Kd=1/m+5/8` for this authored rectangular/spherical-inertia patch. This is the analytical change in the soft common support mode. It is not arbitrary rescaling, rigid-support subtraction, force smoothing or a seven-row endpoint solve. It reads no converged-reference result. Normal difference modes and all tangent/twist guesses are preserved; the mass-only remainder remains. Positivity is checked rather than forcing guesses into the admitted domain.

At 32 N it adds -.12199946117364349 N s per normal guess. Eight-sweep errors return almost exactly to the mass-only values and pass every unchanged bar. This controlled change isolates and removes the load-excited error without altering equations, geometry, mass, load, dt, order or iteration count.

Only three correction classes were considered:

- Blind reset loses: it worsens all three errors and adds a tangent clamp.
- Increased count has no proven passing candidate inside the authorized 1..32 bound; do not invent one.
- The derived load-mode predictor has direct causal and local accuracy support.

**Scope limit:** this is not yet a general warm-start policy. Arbitrary torque, nonuniform/changing contact geometry, active-set changes, exact tiny pieces and full retained-world powered/coast continuation remain unqualified. Production integration requires Project Control's separate review; no correction is implemented here.

## Direct cost estimate

One fresh Release process, 256 warm paired calls, 2,048 paired samples per arm, eight local calls per timed sample; alternating arm order. The derived arm includes initial-guess preparation. Both use eight sweeps. Original diagnostic Clone allocations and the additional guess copy remain included. No reflection, GC isolation or runtime change is inside the timed calls.

| Batch-normalized local cost | Original eight | Derived guess + eight |
|---|---:|---:|
| Median, ms | .0029375 | .0030500 |
| P95, ms | .0048250 | .0049500 |
| P99, ms | .0068750 | .0059875 |
| Maximum eight-call average, ms | .3793625 | .0154000 |

Median delta: **.0001125 ms (0.1125 microseconds), +3.83%**. No claim is made about the cause of the original tail or that the predictor improves worst-case latency. These are eight-call averages, not individual-operation maximum bounds.

The proposed median is 0.0457% of 6.67 ms. Subtraction leaves 6.66695 ms arithmetically, **not measured available frame headroom**. Collision refresh, export, publication, other simulation and rendering are excluded. This is the authorized local payoff estimate, not performance qualification.

## Evidence and stop

[Plan — retired record](../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md), [results](historical-early-convergence.json), [verification](historical-early-convergence-verification.md), [identity — retired record](../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md), [reproduce — retired record](../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md). Original prerequisite reports and sources remain unchanged; the original full operator remains unqualified. No long support, tiny/world, off-COM, allocation, M15 free comparison, full qualification or manual acceptance was run.

Final preservation check: all 2,026 pre-existing file fingerprints (1,991 tracked and 35 untracked) and all 65 tag refs match. HEAD/main/origin/main/remote main remain at the baseline above. Nothing is staged or tracked-modified; `git diff --check`, new-file whitespace, reproduction-script parsing and evidence links pass. The new diagnostic project is not registered in the permanent solution/test runner. Protected M14.21–M14.24 and M15.0 tag object/commit identities are recorded in identity.json.

Retained additions are the standalone two-file ConvergenceProbe and this six-file evidence package. Rebuildable scratch at `E:\NovaCore\build\powered-contact-convergence` remains: **93 files / 5,496,682 bytes**. Automatic approval review rejected its one guarded cleanup attempt as **blocked by policy** before execution; no retry or workaround was attempted. No files were disposed by this ticket. After review, the exact manual cleanup is:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-convergence' -Recurse -Force
```

The prior `build/powered-contact-numerical-prerequisite` scratch directory is currently absent; this ticket did not delete it and makes no claim about why it is absent. Its useful result/provenance remains in the unchanged prior evidence and the byte-identical reproduction witness. The new scratch is not required for reproduction: source, fixed inputs/results, hashes and rebuild/run instructions are retained.

UNBANKED. No M15.1 assignment. **STOP FOR PROJECT CONTROL.**
