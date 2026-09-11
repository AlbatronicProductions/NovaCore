# Isolated-response and reduced beta-feasibility assessment

This is a read-only analytical subtask of the positive-beta source-feasibility assessment. No production or permanent test was changed, no fourth fixture was proposed, and no production root or coverage search was executed.

## Predeclared calculation budget

Before calculations, declare exactly sixteen combinations: `eta=h/x` in `{0.05,0.1,0.2,0.3}` crossed with terminal rotation angle `Theta=omega*(T-alpha)` in `{0.2,0.4,0.6,0.8}`. The retained script computes one reduced scalar root per eta using a fixed 64 bisections and evaluates all sixteen combinations. There is no random search, parameter optimizer, or parameter replacement after results.

This reduced stationary-plane, zero-force calculation is **not** an independent oracle of the current moving-Earth / represented M14.13 physical model. Its purpose is to identify response-law tradeoffs and necessary numerical margins, not to issue an admitted fixture or certificate.

## Current response authority

`CertifiedContactResponseMath.Enclose` computes a zero-restitution impulse from a qualified strict-approach speed at the same provider-owned alpha. With normal `n`, lever `r`, body-normal cross product `a=r cross Q^-1 n`, mass `m` and principal inertia `I`, it uses

`k=1/m+sum(a_i^2/I_i)`, `J=-v_n/k`, `DeltaV=J*n/m`, `DeltaL_body=J*a`.

The stored quaternion has exact-normalization meaning; interval endpoints are not selectable response commands. Finite positive mass/inertia, strict negative approach, positive inverse mass/impulse, and every original request width must hold. Larger impulse or lever is not exempt from width checks.

`CertifiedPostImpactVelocityMath.Prepare/Certify` then selects final represented linear/angular bits from at most 65 candidates. It requires the component-error-derived residual budget to remain below the incoming speed; energy-defect and mass-weighted-error budgets remain below certified dissipation bounds. The final candidate must have certified normal residual `rho.Lower>=0` and nonincreasing energy. Thus a physical exact-law zero normal velocity becomes a qualified represented tuple with nonnegative residual; a large arbitrary outward speed cannot be substituted. `PrivatePostImpactState.Prepare` installs those exact final bits into private source evidence without reapplying the impulse. Existing requests, authority identity and epoch relations remain requirements.

The pre-impact specialization requires zero body spin and zero torque (`FloridaContactMotion.TryCreate`). The subsequent M14.15 trajectory has constant root force and torque-free body rotation. It uses the same moving Earth and body-fixed plane relation, not a stationary plane. Its remainder is at most one second. Rotation uses degree 11 with an order-12 energy-derived remainder; request width failure remains possible even when the analytical trajectory exists.

## Spherical reduced response map

Choose an orthonormal local tangent `t`, outward normal `n`, and the actual lever at alpha `r=x*t-h*n`, with `x,h>0`. Assume zero pre-spin, incoming relative normal speed `-u`, zero tangential relative COM speed, spherical inertia `I`, and a stationary plane only for this calculation.

`J=m*u*I/(I+m*x^2)`

`omega=m*u*x/(I+m*x^2)`

`Vplus_normal=-m*u*x^2/(I+m*x^2)=-x*omega`

Tangential COM velocity is unchanged. Angular impulse points along `r cross n`; rotation raises the feature initially. The exact zero-restitution feature derivative is zero at alpha, despite negative COM normal speed. For `theta=omega*s` and `eta=h/x`,

`g(s)/x=sin(theta)-theta+eta*(1-cos(theta))`

`g'(s)/(x*omega)=cos(theta)-1+eta*sin(theta)`

`g''(0)=h*omega^2>0`.

The first positive maximum occurs at `theta_peak=2*atan(eta)`. The later positive root has strict negative derivative. Normal offset changes curvature and root separation, not the initial effective inverse mass. Inertia and mass set omega through the response rather than independently of it. In particular, `omega<u/x`; a larger lever and a fixed low incoming speed cannot produce an arbitrarily large angle within the one-second remainder. For fixed `lambda=I/(m*x^2)`, `omega=u/[x*(1+lambda)]`.

## Sixteen-combination result

`b` denotes the subsequent root angle. `peak` is maximum gap divided by x; `approach` is `-g'(beta)/(x*omega)`. All are reduced-model diagnostic scalar quantities, not certified enclosures.

| eta | b | peak | approach | Theta=.2 | Theta=.4 | Theta=.6 | Theta=.8 |
|---|---:|---:|---:|---|---|---|---|
| .05 | .149887662439 | .00008320855611 | .003745788018 | beta/T=.74944 | .37472 | .24981 | .18736 |
| .10 | .299105170970 | .0006626950177 | .01493293076 | no beta before T | .74776 | .49851 | .37388 |
| .20 | .592962101695 | .005208880300 | .05894697990 | no beta before T | no beta before T | .98827, near target | .74120 |
| .30 | .876890751907 | .01708641104 | .1298292924 | no beta before T | no beta before T | no beta before T | no beta before T |

Nine combinations have a subsequent root before the selected terminal angle; eight have materially greater temporal separation than the eta=.2/Theta=.6 case. This does not prove numerical robustness in any admitted Florida source. The `.95` label in the script is display-only and changes no production threshold.

Small eta causes peak gap to scale approximately `(2/3)*x*eta^3`, beta angle approximately `3*eta`, and beta approach approximately `(3/2)*x*omega*eta^2`. Merely establishing physical root existence can therefore leave very small useful gap and derivative margins. Increasing eta improves those margins but pushes beta later.

Tangential feature displacement is `Delta_t/x=cos(theta)-1+eta*sin(theta)`. It is the point's swept path, not lever length alone, that must remain inside the grading footprint. A kilometre lever is not automatically a domain violation. Conversely, zero initial point offset does not establish later containment. The current retained Florida input uses inner extents 64 m east and 56 m north; exact body-fixed projection inequalities and uncertainty must be checked.

## Conditions needed to upgrade this screen

Let `E_g`, `E_v`, and `E_a` bound, respectively, gap, derivative and curvature differences/uncertainties from the complete moving-Earth, actual M14.13 represented-tuple, torque-free model over the intended interval. These are quantities to prove, not tolerances to add to physics.

- Positive departure by the current search theorem can use `g'(alpha)>=0` plus strictly positive certified local curvature. The requirement is `h*omega^2` exceeding the complete opposing curvature and enclosure uncertainty over a nontrivial initial interval. It need not rely on the tiny represented residual being strictly positive.
- Clear-prefix intervals need certified lower gaps greater than zero. A necessary scale screen is `x*peak > E_g`; substantial ratio margin is required before calling it robust.
- Beta must have a bracket whose left gap exceeds its error bound, whose right gap is below minus its error bound, and whose whole derivative remains strictly negative. A first-order numerical scale is `E_g/|g'(beta)|`, to be checked against alpha and target separation and actual bounded interval widths. No numerical tolerance is introduced by this estimate.
- Root-linked time width matters: increasing time/attitude uncertainty expands a lever by roughly its scale times angular/time uncertainty. Neither a small local scalar residual nor arbitrary root refinement proves the accepted provider can meet all M14.10-15 request widths.
- The degree-11 spherical rotation bound reduces analytically to an order-12 attitude majorant `(1.5*Theta)^12/12!` (with actual outward arithmetic and represented spin still required). Lever amplification and the original request must be included. This is not a license to select new request thresholds.
- Grading guards require full same-model point containment in the supported plane region. Current coverage uses local leaf guards. A broad whole-remainder box failing a guard is not by itself physical domain exit.

The coverage search's 255 visits, depth 24, cumulative refinement 24 and total root depth 48 do not establish sufficient useful resolution by themselves. Real derivative/function floors determine whether bisection progresses. No production search was used to turn the above necessary inequalities into a work-count prediction.

## Scope conclusion

The reduced response law supports separated departure/reacquisition regions. It disproves the claim that zero restitution and zero initial spin make any later root physically impossible. It does **not** prove that the current banked M14.9-15 chain robustly admits one such source, nor that the candidate certifies beta under its current floors. No classification A fixture can be supplied from this subtask. The lead must combine actual source/refinement blockers and numerical floors; missing same-model bounds must remain explicit.

Reproduce the bounded calculation with `python docs/engineering-evidence/postimpact-feature-coverage/response-feasibility-study.py`. It uses only the Python standard library. The JSON output records every one of the sixteen combinations. No third-party dependency, runtime diagnostics or build is needed.
