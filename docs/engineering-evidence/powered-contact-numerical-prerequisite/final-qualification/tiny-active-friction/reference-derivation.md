> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Independent active reference and separated response

Use exactly the retained tiny-event source, dry8kg, inverse inertia .5I, gravity represented
as binary64 -9.81, h=2^-1075 seconds and engine force0/16 N. All other branch inputs match.
Every captured binary64 input is decoded by its integer ratio. Prepared FP64 cross products
and radii are decoded at that explicit input boundary. No candidate impulse/endpoint defines
the tiny reference. The old inadmissible interior answer is not used as its solution.

Let J_i=(linear_i,angular_i), K_ij=J_i M^-1 J_j^T, omega the captured spring frequency,
k=omega*h*(omega*h+2), and z=lambda/h. All positive-depth rows use bias=depth/(h+2/omega),
independently checked against both other bias branches. The positive normal equations reduce to

diag(Kii)*zN + k*K_N,*z = depth*omega^2 - omega*(omega*h+2)*J_N*v_source - k*J_N*a.

For a tentative unit tangent direction u and independently determined negative twist sign,
zT=.125*sum(zN)*u and zW=-.125*sum(radius_i*zNi). Substitute into the four normal equations,
solve by pivoted elimination, and recompute the raw tangent direction from the pinned block
map. At most six direction updates were allowed; each branch used three. This is not the
candidate's sequential eight-sweep solver or a higher-sweep substitute. A separately arranged
1100-digit elimination/direction implementation independently agreed on the reported values
and every representation decision.

The main solver uses1100 decimal digits. A separate1200-digit checker reconstructs final
velocity and the original unscaled normal equations, tangent map, twist clamp and boundary
conditions. Predeclared normalized residual/conditional-KKT bar:1e-900. Observed maxima:
baseline2.54105548e-1100; powered3.23142553e-1100. All outputs finite; all normal impulses
positive; depth bias branch verified. Conditional tangent stationarity is checked against
the fixed-cap block, not cap derivatives in a global optimization.

## Tiny active set (both branches, independently solved)

- Normal impulses approximately [4.8466617972,4.8466593675,4.8466612414,4.8466599234]e-323 N s.
- Tangent=[-6.5664881931e-324,-2.3326684351e-323] N s.
- Raw tangent length1.5166770353e-16 exceeds guard1.0000000169e-16 and cap2.4233302912e-323.
  Tangent is saturated, guard inactive, with opposing slip and zero normalized boundary
  residual to the declared precision.
- Raw twist=-6.9191445188e-20 N m s; cap2.7093656315e-323 N m s. Negative saturation is
  admissible and opposes twist slip; it was not inferred from tangent saturation.

## Powered minus baseline (high precision, before field projection)

| Field | Response |
|---|---|
| Linear m/s | (+3.69087718e-646,+4.940656458412465442e-324,-1.03898613e-646) |
| Angular rad/s | (+2.07625114e-646,+1.71476334e-645,+7.37564030e-646) |
| N0/N3 N s | -6.134923823817443466e-645 |
| N1/N2 N s | -6.134923824070785853e-645 |
| Tangent N s | (+8.31188902e-646,+2.95270174e-645) |
| Twist N m s | +3.42952668e-645 |

Only the separately represented Y velocity response rounds nonzero in binary64: one epsilon.
All binary32 responses are zero. Differences of independently rounded absolute endpoint
fields are zero in both binary64 and binary32. Differences of the independently rounded
53-bit scaled cache fields are also zero. The per-piece Scaled increment fields preserve
a Y difference which projects to one epsilon; candidate comparison checks this interface
separately. Do not equate round(P-B) with round(P)-round(B).

## Coast reference

The exact following remainder is16666/1000000-2^-1075 seconds. Both actual installed sources
and both unrounded reference sources are retained separately, including their cache, geometry,
exact producing duration and old load. The prepared current coast geometry matches the
installed geometry numerically. A fresh independent seven-equation interior elimination is
admitted only after checking four positive normals and strict tangent/twist feasibility.

All four coast references pass: tangent magnitude1.64783977e-8 versus cap.163493460 N s;
twist magnitude4.26855449e-19 versus cap.182791246 N m s. Direct final-velocity equation
residual <=9e-1101. Thus the reference transition is saturated tangent/twist to interior,
with all normal rows remaining active. This is the unique interior linear-system solution,
not a globally established unique solution over every active set.

The discarded tiny cache response scaled by h_coast/h_tiny rounds to
[-8,-8,-8,-8,+1,+4,+5]*double.Epsilon. These are initialization/cache differentials,
not the converged coast reaction. Unrounded reference-source coast Y endpoint response
is4.19844217e-325 m/s, rounding to zero. Unrounded converged normal-impulse differences are
about-9.04162448e-324 N s each, rounding to-2*epsilon; those also disappear when each
ordinary-sized absolute impulse is first rounded. Actual installed-source coast references
agree. No downstream candidate preservation is claimed: its first coast admission refused.
