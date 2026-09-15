> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Signed modal reconstruction

Evidence: [convergence.json](convergence.json), including initial/final signed errors, each modal contribution's impulses and generalized wrench, ordered-map columns, and every normal/tangent/twist stage for sweeps 1 through 8. Mode amplitude is pattern dot normal-error / 4; it is not the orthonormal dot/2 convention. Tangent values are kept together when propagated.

Normal/tangent cache modes have units N s; twist cache has units N m s. Normal/tangent equation residuals are m/s and the twist residual is rad/s. The reported maximum over seven residual rows is a diagnostic numeric check over mixed units, not a single physical-unit norm.

| Mode / coefficient convention | Initial error | After eight sweeps |
| --- | --- | --- |
| common q=(1,1,1,1) | 0.018173779796074 | -0.00007186765661409272 |
| X=(-1,1,-1,1) | 2.7381437819884447e-7 | 0.000031084093757342834 |
| Z=(-1,1,1,-1) | 0.000001759886845004166 | 0.00029684261109021864 |
| checker=(1,1,-1,-1) | -0.000001851412758754889 | -0.0007530655637533953 |
| tangent T0 | 0.0000035205449877539667 | 0.0005936850699095325 |
| tangent T1 | -0.0000010953297088936635 | -0.00012433634294660727 |
| twist | -3.3451021601874494e-15 | -1.9846000886781578e-13 |

The checker pattern is initial self-stress with zero generalized wrench for this rectangular geometry. Its large final coefficient alone is not proof of a large physical torque. The ordered iteration also creates X/Z pressure and tangent errors that do change body velocity.

For current interior equations A lambda = rhs, define G as: normal rows 0,1,2,3 in that order; coupled tangent rows 4,5; twist row 6. Then e8=G^8 e0. Source-linked candidate iterations contain no active clamps in this coast trajectory, so the homogeneous map is applicable. Each signed mode was propagated separately, summed in the seven-dimensional cache space, and only then mapped to body kicks and norms.

| Initial mode propagated alone | Signed final normal sum N s | Final linear velocity error vector m/s | Final angular velocity error vector rad/s |
| --- | --- | --- | --- |
| common | -0.00028747569530528544 | (-0.0000155371229997553, -0.00003593446191316068, -0.00007420596840868665) | (-0.00014851859050642527, 9.934121891230631e-14, 0.00003109657697262115) |
| x-pressure | -4.170177200006691e-9 | (2.3883914677726387e-10, -5.212721500008363e-10, -5.342726092255577e-10) | (-1.0693131101174502e-9, -1.5270891506234755e-18, -4.780215688548853e-10) |
| z-pressure | 5.6366642178385605e-8 | (1.4738174250630907e-9, 7.0458302722982006e-9, 1.2835525151501154e-8) | (2.5689498362338823e-8, -9.423290236054021e-18, -2.9497531172783114e-9) |
| checker | -6.038711281771659e-8 | (-4.7482006959712284e-9, -7.548389102214573e-9, -1.7450106784813925e-8) | (-3.4925294008612225e-8, 3.0359033959213274e-17, 9.503225817678597e-9) |
| tangent-block | 1.3259496760978482e-8 | (-1.8843244580188003e-9, 1.6574370951223102e-9, 4.836390279385856e-10) | (9.67973173636636e-10, 1.204798909610278e-17, 3.7713571908452506e-9) |
| twist | 2.9541139090055845e-26 | (7.965147641240384e-27, 3.692642386256981e-27, 1.381237740303538e-26) | (2.764460686158871e-26, -5.092754144443395e-35, -1.5941743315543342e-26) |

Reconstructed seven-component error:
```text
[-0.0011528599252149999,-0.0004970065155198823,0.000946956424472144,0.00041543938980637417,0.0005936850699095647,-0.00012433634294669959,-1.984600088679631e-13]
```
Maximum difference from measured candidate error: **9.23198149871407e-17**, versus 1e-12 proof bar. Independent direct affine equation tracing matches all measured curve samples within **1.3877787807814457e-16**.

The common mode alone supplies initial generalized linear impulse (0,0.072695119184296,-2.3223204776209378e-10) N s and angular impulse (1.1603263835868738e-10,0,0) N m s. Its eight-sweep signed normal result is -0.00028747569530528544 N s. Other signed modes contribute approximately +5.06885e-9, not independent positive error norms.

## Stage coupling

| Sweep | Stage | Normal max residual | Tangent residual norm | Twist absolute residual |
| --- | --- | --- | --- | --- |
| 1 | after-normals-before-tangent | 0.007386376341346825 | 0.0020983375638598555 | 7.983933777332578e-16 |
| 1 | after-tangent | 0.005040599687939328 | 1.3376175218227403e-24 | 6.038330774660727e-12 |
| 1 | after-twist | 0.005040599687939328 | 9.636771005317258e-21 | 0 |
| 2 | after-normals-before-tangent | 0.001576490078312949 | 0.0007635536634104205 | 0 |
| 2 | after-tangent | 0.001927265928946636 | 5.505999666241038e-20 | 1.8909931429774184e-12 |
| 2 | after-twist | 0.001927265928946636 | 5.461263414750274e-20 | 0 |
| 3 | after-normals-before-tangent | 0.0005787227240375802 | 0.0007614150453705662 | 0 |
| 3 | after-tangent | 0.0012262701427451527 | 7.435278642593824e-21 | 2.0478550862855516e-12 |
| 3 | after-twist | 0.0012262701427451527 | 4.7644868108809886e-21 | 0 |
| 4 | after-normals-before-tangent | 0.0007636470549641481 | 0.0004263633124169356 | 0 |
| 4 | after-tangent | 0.000646144313885061 | 5.086034780425986e-20 | 1.2105707718934636e-12 |
| 4 | after-twist | 0.000646144313885061 | 5.2792605746157537e-20 | 0 |
| 5 | after-normals-before-tangent | 0.0006565761255003676 | 0.00019537809527673348 | 0 |
| 5 | after-tangent | 0.00044131645933928284 | 1.2135024277951122e-20 | 5.975878657853591e-13 |
| 5 | after-twist | 0.00044131645933928284 | 1.3088866817730419e-20 | 0 |
| 6 | after-normals-before-tangent | 0.0005064376657766444 | 0.00008295933158030037 | 0 |
| 6 | after-tangent | 0.0004293328937868326 | 2.713296642640801e-20 | 2.6293125423741973e-13 |
| 6 | after-twist | 0.0004293328937868326 | 2.7155234264516243e-20 | 0 |
| 7 | after-normals-before-tangent | 0.00037443482729503677 | 0.000047213332097510716 | 0 |
| 7 | after-tangent | 0.0003608632182250463 | 2.7105089994893314e-20 | 9.986944848551623e-14 |
| 7 | after-twist | 0.0003608632182250463 | 2.71053000783678e-20 | 0 |
| 8 | after-normals-before-tangent | 0.0002706484582144797 | 0.00004089999805310253 | 0 |
| 8 | after-tangent | 0.00028252293724428175 | 1.154256229606562e-22 | 2.608065834985693e-14 |
| 8 | after-twist | 0.00028252293724428175 | 1.5705438221742277e-22 | 0 |

At sweep 8 the earlier normal rows already have residual after the normal block (max 0.0002706484582144797). Later normal updates disturb earlier rows. Tangent solving then changes normal residual by K_NT times delta tangent and raises the max to 0.00028252293724428175. Thus tangent feedback contributes; it does not explain every normal residual by itself.

Across the eight saved stages, independent verification matches observed tangent feedback to its exact K_NT delta-tangent prediction within 8.67e-19. Twist's direct normal coupling is zero here; the tiny T1/twist coupling (-7.980772321453865e-10) produces negligible remaining physical error. Twist is not the coast causal winner.

Normal-error acceptance uses abs(sum solved normals - sum reference normals). Summing signed per-row errors can differ in the final rounding bits; that does not change the exact replay of the original scalar operands.
