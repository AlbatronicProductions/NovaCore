> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Contract judgment: D. SPLIT CONTRACT

This is a recommendation for Project Control, not a changed test. The selected D+12 candidate remains failed on all thirteen frozen one-ULP rows.

## Cause and arithmetic

The only differences in the actual saved following-coast input records are `accepted.Kind` and `accepted.Load.Force.Y` (0 versus16 N). Both poses, orientations, native body velocities, scaled cache, producing/current geometry, exact duration limbs, mass/inertia and current gravity-only load match. Both current equation objects (K, source, free velocity, J, h, body, geometry and load) match exactly.

S0 historical cache, S1 basis transport and S2 duration transformation are identical. S3 changes the powered normal initialization by **+0.060999065573407007523** per normal because the remembered source load differs. This is an intentional initializer difference, not a physical output error or a new tiny impulse. S4 current equations remain identical.

Let u=(1,1,1,1,0,0,0), A be the current equation operator, and q select the sum of its four normal rows. In exact arithmetic the common correction satisfies:

`D(x+s*u) = x+s*u - u*q(A*(x+s*u)-b)/(q*A*u) = D(x)`.

The represented load addition and sequential residual sum/division do not perform that cancellation exactly. Independent reconstruction reproduces the captured D arithmetic:

| | Baseline | Powered |
|---|---:|---:|
| summed normal residual | -1.8198544288999807e-5 | 0.13330980145571125 |
| denominator | 2.1857384001981397 | 2.1857384001981397 |
| D delta | 8.326039514769969e-6 | -0.06099073953389233 |

At S5 all four normal values differ by exactly **-1/9007199254740992 = -1.1102230246251565e-16** (two reference normal ULPs). No feasibility clamp produces it. The first ordered sweep propagates it into tangent/twist; final ordered export produces the six increment differences. `d-arithmetic.json` records exact fractions, row sums and binary representations. The powered S3 addition already differs from the requested shift by 3/144115188075855872; finite D summation/division and final addition complete the residual. No single unexplained runtime/random-state mechanism is required.

No-D leaves the large previous-load initializer difference and gives a much larger physical discrepancy. D is principally cancelling it; this does not justify removing D, nor prove D alone is an inappropriate architecture.

## What the bounded controls prove

Independent identical-input owners reproduce every stage and final value exactly. Zero tiny force through Powered versus Coast kind, with diagnostic prepare/install/coast lifecycle, produces identical numerical outputs. Enum/provenance/control-path differences alone do not create the discrepancy.

| Diagnostic policy | Paired rows passing /13 | Native branch response | Baseline max equation residual |
|---|---:|---|---:|
| D+12, unchanged selected candidate | 0 | identical | 1.0894215130867486e-4 |
| D+16 | 5 | identical | 3.64532221281833e-5 |
| D+24 | 3 | identical | 1.8926427987682947e-5 |
| D+32 | 13 | identical | 6.8476675359412325e-6 |
| no-D+12, diagnostic only | 0 | different | 1.0892945639677366e-4 |

Paired errors oscillate at rounding scale before coalescing at32. Absolute error/residual decay is a different measurement. At32 the cache, increments and current equations are identical, so further identical deterministic sweeps preserve equality. The smallest achievable paired difference in this equation pair is therefore **zero**. This is not an unavoidable nonzero representation floor and not a claim that32 achieves absolute convergence. No additional sweeps or direct solve were needed to answer that floor question.

One later identical frozen-geometry constrained piece receives the saved D12 states after diagnostic owner promotion, preserving the differing scaled cache and ordinary native endpoint projection. Both return Ready, zero normal/tangent/twist clamps, and identical cache, increments, FP64 endpoints and native endpoint bits. The discrepancy **disappears in this witness**. No native world was advanced or canonical state published. No conclusion about all refreshed manifolds, trajectories or arbitrary duration ratios follows.

## Reference precision

The actual comparison uses the **installed-source** references. Those two high-precision outputs are identical in every field. Separately, unrounded physical source references have nonzero tiny differences, but independent binary53 rounding of each absolute value gives the same value in each branch, field by field. Thus `round(P)-round(B)=0`; `round(P-B)` need not be zero. The source history and exact tiny event must not be erased by conflating these operations.

`analysis.json` records every common binary53 projection, the nonzero unrounded response, reference ULP, exact stage delta, all sweep results and the downstream result. The independent verifier used integer quotient/remainder ties-to-even rounding. This audits the retained 1100-digit oracle outputs, not an infinite-precision mathematical theorem.

## Required outcomes and proposed split

Seven cache components are genuine prospective retained inputs. Protect ownership/lineage, feasibility, deterministic identical-input replay, current physical solution and subsequent-step admission/response. Six increments are proposal/export intermediates; protect the physical endpoint they construct and any explicitly required preprojection tiny response. Their numerical values are not a second independently advanced body state.

The evidence supports **D. SPLIT CONTRACT**, with B (required outcome/wrong uniform metric) for treating all thirteen comparisons alike. It does not establish that exact paired one-ULP identity after a fixed number of iterations is necessary for either prospective cached state or temporary export arithmetic. Equally, it does not authorize discarding cache precision or ignoring all private differences.

Proposed future contract, subject to explicit approval:

1. Keep exact event/resource/time provenance and genuine retention/nonmutation/seal checks.
2. Keep exact deterministic replay for identical represented inputs, including the seven retained fields.
3. Keep the existing independently projected native endpoint response check and existing absolute physical budgets; do not substitute zero-looking FP32 data for the tiny preprojection physical witness where that witness is required.
4. For differing histories, protect current and downstream physical response, admission and active set. Assess cache error through the current equation/transport-to-physical map, not ULP at a near-cancellation cache component. Any bound must come from fixed equations, reference precision and the previously declared physical budgets, including duration/basis transformation conditioning. For an admitted interior linear piece, the response operator `M^-1 J^T A^-1` maps residual error to velocity error; this is the appropriate derivation location, not the measured D12 discrepancy. No numerical replacement cache tolerance is proposed here.
5. Retain the current thirteen-row report as diagnostic evidence until Project Control approves a permanent scope change. Do not silently convert its FAIL to PASS.

KSA reinforces the ordinary solver-private/game-state boundary, while its float timestep does not settle NovaCore's subnormal event domain. This supports bounded adaptation, not wholesale copying of its eight iterations, angular damping, float-time policy or cache representation.

## One recommended next action

Project Control should review and authorize (or reject) this **ownership-based contract split** before any further candidate correction or qualification. Preserve D+12 and all frozen failures now. Do not automatically adopt32 sweeps, remove D, align arithmetic paths, add tolerance or change cache lifetime. A larger sweep count is a diagnostic witness, not the selected production policy.
