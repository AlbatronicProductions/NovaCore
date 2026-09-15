> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Retained contact-basis compatibility and cache transport

**Judgment: REVISE. Correction class: A. BASIS TRANSPORT VALID, within the explicit bounded domain.**

Full generalized-impulse transport is mathematically valid and independently verified for the original captured coast and admitted synthetic cases. It removes the exact-basis numerical admission obstacle diagnostically. **It does not qualify coast continuation: the first coast8-sweep solve fails the unchanged normal-impulse and angular-velocity gates.** No further cause is attributed and no additional solve campaign was started.

Baseline HEAD/main/origin/main/remote main: `49057fecceb0f725d5f551ec40e2780971b0d81d`; branch `codex/powered-contact-numerical-prerequisite`. M15.0 remains `4607d8c802006d5e1a01c595ab608cf53a4dab6b`. All2,076 starting files and65 historical tag refs are preserved. No production, existing diagnostic, permanent test, package, Blender or tracked-file change. Nothing staged or banked.

## What happened

The pinned same-type contact path transfers normal components by feature but reapplies old tangent/twist scalars in refreshed axes/geometry. The old cache represents four normal contact impulses, one patch tangent pair and one scalar normal-axis twist. A complete transport must preserve their summed linear AND COM-referenced angular impulse. Independent derivation and source trace are in [transport-derivation.md](transport-derivation.md) and [bepu-basis-semantics.md](bepu-basis-semantics.md).

The diagnostic solves the actual new Jacobian plus one declared normal-nullspace equation, using historical producing geometry from the retained capture. It does not approximate equality by angle. It does not normalize axes, clamp transported cache, alter equations, or install anything.

## Original coast witness

Old normal `(0,1,7.002423640933841e-11)`; new `(0,1,-3.1946030265572745e-9)`. Both have tangents `(0,0,-1)` and `(1,0,0)`, but neither stored frame is exactly orthogonal. The four lever-Y values change from−.49958550930023193 to−.49964094161987305m. Full source/target inputs and outputs are retained in [original-coast-witness.json](original-coast-witness.json).

Before duration/load changes:

| Invariant | Result |
|---|---:|
| Linear impulse error | 0 N·s |
| Angular impulse error | 3.229815946489969e-17 N·m·s |
| Current-body linear velocity kick error | 0 m/s |
| Current-body angular velocity kick error | 1.6149079732449846e-17 rad/s |
| Declared absolute vector error bar | 1e-12 |
| Condition screening estimate | 8.764161226668799e-13; admitted |
| Duration/transport commutation component error | 4.163336342344337e-17 |
| Blind-reuse linear impulse error | 1.371788250169109e-9 N·s |
| Blind-reuse angular impulse error | 7.817693146263636e-10 N·m·s |

The new cache is strictly feasible: minimum normal .10504676418943519 N·s; tangent magnitude1.7893023031718454e-6 versus cap.052524688872234875 N·s; twist magnitude1.4789792010570797e-15 versus cap.058724387407671975 N·m·s.

Independent support/lineage checks pass. This is an evidence-only admission verdict, not a CacheHistory Prepared capability. The original exact-normal check still refuses in the unchanged operator.

## First coast solve — unchanged equations,8 sweeps

All arms use the same refreshed geometry, dry mass8kg, fixed inverse inertia .5 diagonal, h=17707/2000000s, gravity−9.81, zero engine force, same source velocity and material. B/C apply the same accepted duration scaling and current-piece load predictor; D is truly zero; E is the current independent reference itself.

| Arm | Normal impulse error N·s | Linear error m/s | Angular error rad/s | Interior equation residual | Result |
|---|---:|---:|---:|---:|---|
| A exact-basis baseline | — | — | — | — | Refused before solve |
| B unchanged components | 2.8747062258260847e-4 | 8.390478730095765e-5 | 1.517503058753226e-4 | 2.8252293810695876e-4 | FAIL |
| C full transport | 2.8747062645639865e-4 | 8.390478684903634e-5 | 1.5175030441508136e-4 | 2.825229372442645e-4 | FAIL |
| D cold | 2.44992273011424e-3 | 7.17138484094381e-4 | 1.2978583166526503e-3 | 2.419036511244007e-3 | FAIL |
| E reference initialized | 0 | 7.544551578752139e-18 | 3.6410418972622145e-17 | 3.686287386450715e-18 | PASS |

The unchanged physical bars are 1e-4 for each error. The independently constructed current reference is admissible, residual 4.887504513621441e-17. Residual is the interior linear-equation max residual, not a general complementarity norm for clamped cases. B/C/E have no clamp activation; cold D has one tangent clamp during its iterations. Transport changes only initialization; it does not change or explain away the target equations. B/C are nearly identical at eight sweeps, so no causal claim that basis mismatch caused the new convergence failure is supported.

## Bounded synthetic results

All admitted tests preserve the stated wrench within1e-12. **All six admitted synthetic8-sweep comparisons FAIL their unchanged physical bars**, including identical-basis initialization; they are transport witnesses, not qualified dynamics. Full errors, reference applicability, clamps and caches remain in [synthetic-results.json](synthetic-results.json).

| Case | Actual decision | Expected decision | Angular transport error N·m·s |
|---|---|---|---:|
| Identical | Ready | Ready | 0 |
| Tiny normal1e-8rad | Ready | Ready | 1.8116236439988926e-17 |
| Moderate normal.08rad | Ready | Ready | 6.165746321498593e-17 |
| Tangent-only.7rad | Ready | Ready | 5.594483210025203e-17 |
| Normal+tangent | Ready | Ready | 5.033127601994671e-17 |
| Reflection | InvalidBasis | InvalidBasis | — |
| Normal flip | NormalHemisphere | NormalHemisphere | — |
| Feature identity changed | IdentityMismatch | IdentityMismatch | — |
| Actual Helpers sign seam | ConditionErrorBudget | Ready | 1.0488982222938547e-16, refused |
| Friction-incompatible.3rad | TangentCap | TangentCap | 8.412626408164514e-17, refused |
| Lever-only.0001m | Ready | Ready | 4.403302626461545e-17 |
| Lever-incompatible3m | ConditionErrorBudget | NonpositiveNormal | 2.4827765763866867e-16, refused |

No expected result or gate was adjusted. The sign seam is not admitted/qualified. Conditioning refuses the3m shift before checking positivity; independent geometry also proves positive normals cannot reproduce the required zero normal z-moment with all x-levers in[2,4]. No attempt to find a different distribution or relax the screen followed.

The separate nonzero-twist difference witness preserves angular impulse within2.4747374745180785e-17 and linear impulse exactly. Projecting twist alone loses.0015982938858868107 N·m·s. Full transport transfers that missing transverse moment into normal distribution.

## Compatibility, correction class and payoff

See [compatibility-contract.md](compatibility-contract.md). Domain is one supported4-row common-normal patch, stable owned identity/feature pairing, explicitly retained producing geometry, valid frames, resolved full-rank mapping and strictly feasible cache. Exact normal identity is not a mathematical requirement for this domain. Neither blanket invalidation nor partial normal-only retention is justified by the evidence.

**A. BASIS TRANSPORT VALID** is restricted to preserving the defined COM generalized kick and admitted numerical/feasibility domain. It does not establish arbitrary manifold transport, fixed-world-origin wrench preservation, sign-seam admission, tiny-duration behavior, changed contact topology, or future supported motion. Existing AcceptedCache needs additional owned producing-geometry metadata before implementation is possible.

Duration scaling and fixed-map transport commute algebraically. Current load preconditioning follows once. Future adoption is not authorized here. The first coast accuracy failure remains unqualified and independently verified. Project Control must decide its next bounded scope/payoff; no corrective mechanism is nominated from correlation.

## Reproduction and execution

Read [plan.md](plan.md), [verification.md](verification.md), and [identity.json](identity.json). From PowerShell7:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\basis-compatibility\reproduce.ps1'
```

The script requires its disposable output directory to be absent, verifies retained input hashes when identity.json exists, builds a source-linked Debug diagnostic, and reads the retained capture. It runs no BEPU world/installation or qualification sequence. Its exit0 means the bounded evidence was produced, NOT that coast accuracy passed. Runtime .NET10.0.12, SDK10.0.303. Three compile-only passes during pre-run ownership-review refinement, each0 warnings/errors; one numerical process, no retry. No build/run was needed after the numerical outputs were observed.

JSON fields originating as float (for example immutableInputs.omega=188.49556) must be decoded to binary32 then widened when reproducing equations. The pinned material omega is exactly188.4955596923828 in binary64. Do not reinterpret its shortest float decimal directly as binary64. Prior observer's documented initial-seed exactPositive limitation remains unchanged; this probe uses only the valid ExactEvent producing/current durations.

Retained: plan, diagnostic source/script, exact two result files, source/derivation/domain notes, independent verification and identity. Prior evidence is untouched. See [cleanup.md](cleanup.md) for scratch disposition.

**UNBANKED. STOP FOR PROJECT CONTROL.** No production basis transport, full prerequisite resumption, live powered contact, departure, M15.1 assignment, or banking.
