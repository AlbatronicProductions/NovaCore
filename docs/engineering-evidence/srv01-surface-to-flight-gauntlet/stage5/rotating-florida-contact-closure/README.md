# Stage-5 causal closure and resource-spatial authority gate

## Disposition

**CONTACT EFFECTIVE-MASS / COMPLIANCE DOMAIN EXCEEDED.**

**Contact behavior: CONSISTENT WITH CURRENT MODEL.**

Project Control stopped the correction path. No contact parameter, inertia, geometry, gravity, resource amount, permanent test or production implementation was changed during this investigation. No further Stage-5 run was attempted after that decision. Stage 4 remains qualified for its preserved bounded point-resource/free-flight outcomes; Stage 5 remains unqualified; Stage 6 remains closed.

The physical-authority decision is [spatial-resource-authority.md](spatial-resource-authority.md): **C — a new product/physical authoring contract is required; ESCALATE TO PROJECT CONTROL — PRODUCT CHOICE REQUIRED.** No numerical storage shape or corrective tensor is selected.

The subsequent Project Control ticket explicitly reopened provisional mission sizing before spatial authoring. Its [current feasibility result](../development-profile-feasibility/README.md) reduces the reference budget but still exposes a vehicle/engine/mission product choice. This does not rewrite the preceding causal or Stage-4 evidence.

## Baseline and retained evidence

Branch `codex/srv01-supported-contact-admission`; HEAD/main/origin/main/authoritative remote main `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`. Historical tags: 67. No staging, commit, tag, merge, push or milestone assignment. See preflight and final verification JSON for current refs, status and seals. Existing dirty/untracked multi-stage work is preserved.

| Evidence | Responsibility |
|---|---|
| [preflight.json](preflight.json) | Original refs, tags, status, 14 Stage-5 seals, preserved 35 Stage-4 / 29 Stage-3 seals, executable-input hashes |
| [failed-witness.json](failed-witness.json) | Exact failed canonical replay, six selected native/contact snapshots, solver-facing description/impulses, accounting |
| [controls.json](controls.json) | Five controlled populations; raw full model and canonical path match exactly |
| [site-oracle.json](site-oracle.json) | Independent analytic matrix/derivative transport at 0 and 20 s; limited held-relative oracle |
| [contact-analysis.json](contact-analysis.json) | Contact rows, static formula, final impulse comparison |
| [mass-oracle.json](mass-oracle.json) | Independent rational aggregation from authored JSON, full tensors, row effective mass, geometry equality and static prediction |
| [ksa-source-history.md](ksa-source-history.md) | Direct current installation and authenticated engineering history; source/intent distinguished |
| [spatial-resource-authority.md](spatial-resource-authority.md) | Final architecture/physical-authority assessment and stop |
| `reproduction/Probe.cs`, `Probe.csproj` | Exact small diagnostic harness; no production correction |
| [reproduce.md](reproduce.md) | Commands, dependencies, failure journal, reproduction boundaries |
| `final-verification.json`, `cleanup.md` | Source/ref preservation and exact retained disposable inventory |

## What reproduced

Every diagnostic contains 1,200 exact 60 Hz integer intervals, total 20,000,000 ticks, using the retained native world. No solver/contact/geometry tuning. The canonical path also verifies 1,200 publications, revisions and history entries, zero remaining debt, unchanged TimelineRevision, fuel, oxidizer and mass, and engine OFF.

| Diagnostic | Peak penetration mm | Final penetration mm | Final speed m/s | Maximum angular speed rad/s | All final support predicates |
|---|---:|---:|---:|---:|---:|
| Original canonical Florida | 19.0437285983 | 14.5606513613 | .137258632233 | .0813518148658 | 0/600 |
| Raw retained solve, full site terms | 19.0437285983 | 14.5606513613 | .137258632233 | .0813518148658 | 0/600 |
| Same site, Omega/Alpha zero | 19.0791019463 | 13.8184092987 | .131175123165 | .0777489204340 | 0/600 |
| Development profile, constant local gravity | 19.0594499969 | 13.8039984934 | .131145898758 | .0777309915688 | 0/600 |
| Stock profile, constant local gravity | .0957363406764 | .0780274231595 | .000000474426525 | .000002192846589 | 600/600 |

The stock maximum speed during the final portion is .00000373910907062 m/s. Canonical maximum absolute height during that portion is 17.5716734202 mm; horizontal drift is 46.0970768677 mm. Canonical final native/canonical values are retained exactly in JSON. The independent all-corner peak differs the former printed value only by approximately 4.4e-16 m, not a physical improvement.

The raw full-model and contact-readback repeats match the canonical native pose/velocity and complete motion exactly. Raw controls do not publish, so their private authority frontier counters are not interpreted as canonical advancement. A zero first-compliant sentinel means no qualifying interval, not successful early settling; the count checks all original geometry, height, speed, angular and drift predicates.

**ROTATING-FRAME HYPOTHESIS: REJECTED FOR CURRENT FAILURE.** Neither frame terms nor Florida geometry are necessary. This does not independently qualify every rotating handoff or establish the sole cause of every residual motion component.

## Actual support representation

The current contact is one `Contact4OneBody` constraint with four valid aft-main-envelope contacts at approximately local X/Z = +/-0.26 m, common upward normal. The original support set is present. The compound-selector scratch index array is not the convex manifold's contact mapping and must not be reported as evidence of discarded contacts.

At interval 1, offsets relative to development COM are approximately `(+-0.26,-1.7003636,+-0.26)` and the normal is `(0,1.0000002,0)`. Normal rows, two tangent-friction impulses and twist friction are warm-started through retained BEPU state. Contact features persist/reorder; no claim of perpetual pointer or row-index identity is made.

Actual material: frequency 30 Hz, damping ratio 1, maximum recovery 2 m/s, friction .5; 8 velocity iterations / 1 substep. Final normal impulses are 87151.82, 77677.19, 101014.055 and 64679.85 N s, sum 330522.915 N s. Required instantaneous gravitational impulse is about 329560.405705 N s. Unequal impulses and moving endpoint are not a static symmetric equilibrium witness.

## Independent mass and static oracle

`mass-oracle.py` reads the original authored JSON with exact rational decimal values. It rotates each dry part's full tensor, applies parallel-axis offsets, sums first moments, and adds the currently declared point stores. It checks its results against captured runtime input only afterward. Thus the static prediction neither takes the observed penetration as input nor relies on the production mass helper as oracle.

| Quantity | Stock | Development |
|---|---:|---:|
| Translational mass kg | 705 | 2,018,900 |
| Resource mass kg | 75 | 2,018,270 |
| Material COM X m | 1.04170212766 | .000363762444896 |
| Principal I_X kg m2 | 190.47255 | 190.47255 |
| Principal I_Y=I_Z kg m2 | 731.600649113476 | 1496.35954452714 |
| Radius of gyration X m | .519782483148 | .00971312088983 |
| Radius of gyration Y/Z m | 1.01869103127 | .0272245415435 |
| Per-row inverse effective mass kg^-1 | .00160323998974515 | .0000908479356846112 |
| Per-row effective mass kg | 623.736936701 | 11007.4047634 |
| Rotational/translational inverse-mass contribution | .130284192770 | 182.412897354 |
| Static compression prediction mm | .0780179301047 | 12.6600855763 |

Mass ratio is 2863.68794326241; inertia ratios are `(1,2.04532287709198,2.04532287709198)`. Full off-diagonal terms are zero for the authored symmetric assembly; they were not discarded by the calculation. Local upright axes swap the transverse/material-axis positions as recorded in JSON. Individual child dimensions and transforms match stock/development; definition digests appropriately differ where propulsion/capacity changes.

For each contact `Kii=1/m+(r cross n)^T I^-1(r cross n)`. With four symmetric corners and transverse inertia I, `Kii=1/m+2*a^2/I`. BEPU computes p=omega/(h*omega+2*zeta), q=1/[h*omega*(h*omega+2*zeta)], CFM=1/(1+q), softness=q*CFM. At rest lambda=m*g*h/4 and the row update is zero, giving:

`penetration = m*g*Kii/(4*omega^2)`.

This is the uncapped steady solution, with positive normal impulses. Predicted recovery bias is below the 2 m/s cap. Equivalent row stiffness is `omega^2/Kii`: it already scales with **effective** mass. Adding an extra vehicle-mass scale would change the model, not repair a missing scale.

Stock prediction differs from final observation by about 9.49 nm. Development prediction differs by about 1.14 mm from nonrotating final depth and 1.90 mm from Florida final depth; the development run is still moving. This establishes the centimeter compliance scale, **not dynamic agreement within 0.122 mm or complete attribution of residual motion**.

The ideal four-row normal matrix has eigenvalues `{4/m,4*a^2/I,4*a^2/I,0}`. The zero mode is redundant checkerboard normal impulse. With the modeled softness, `K+q*Kii*Identity` is nonsingular. The source review found no proof that BEPU is malfunctioning or that increasing iterations would cure this equilibrium compliance. The recorded heave/pitch ratio is not a full condition number.

## Bar and Stage-4 domain

The stock admission contract pins 30/45 kg stores, 705 kg mass. Its qualification plan defines settled height <=2*(smallest feature/1000); the .061 m main flange produces the **.122 mm** bar. Stage 2 extends the same stock vehicle to 701.09375 kg after consumption. Stage 4 explicitly excludes high-mass contact, real tank/pressure-vessel suitability and broad angular-flight qualification.

Historical bar classification: **B — BAR WAS ONLY QUALIFIED FOR THE STOCK CONTACT DOMAIN.** The current Stage-5 ticket nevertheless retains that required outcome. No bar is relaxed, and no replacement is invented.

Current development source is mathematically consistent with its accepted common-point resource model. It cannot establish physically distributed propellant inertia. The latest Project Control decision assigns that missing authoring responsibility before a fair high-mass Stage-5 acceptance.

## Red team and stop

Independent read-only reviews checked current KSA mass/contact methods and NovaCore authority/domain. A separate algebra review verified the equilibrium formula, warned against a conditioning/instability claim, and identified that the initial oracle consumed captured mass input; the independent authored-data oracle closes that evidence gap.

Rejected shortcuts: visual/collider-derived liquid geometry, scaling dry inertia, deriving inertia from the support target, copying KSA constants/virtual capacity, treating positive-definite tensors as spatial proof, and reusing point-removal dynamics as automatic distributed-drain qualification. Current physical choices remain unapproved.

No correction was attempted. Consequently no post-correction local/Florida run, affected Stage-4 requalification, remaining Stage-5 performance/allocation/handoff campaign or manual Florida acceptance occurred. Prior valid stages stay preserved; no manual acceptance is self-declared. **UNBANKED. STOP FOR PROJECT CONTROL.**
