> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Equation-level cause and bounded payoff

The [controlled witnesses](controlled-witnesses.json) and [signed ordered-map reconstruction](modal-decomposition.md) establish a common normal initialization excess, chiefly caused by the successor's separating source velocity and reduced depth bias. Current-duration softness/bias also matters. This remains after the correct external-load shift and valid generalized cache transport.

## Exact residual identity

Let source piece index 0 be the producing powered equations and index 1 the first coast equations. Define:
- lambda0: accepted powered output cache (not its reference);
- rho=h1/h0=1.133248; T: accepted restricted basis transport;
- ell: the accepted load-shift vector, nonzero only in the four normals;
- u_s: source generalized velocity, a: gravity plus engine generalized acceleration;
- J: current contact Jacobian; K=J M^-1 J^T;
- D: diagonal matrix with the four normal K_ii values and zero elsewhere;
- alpha=1/[omega*h*(omega*h+2)], A=K+alpha D;
- b: the bias vector, with zero tangent/twist entries;
- rhs=b-J(u_s+h*a); r=A*lambda-rhs.

Then the accepted coast guess is rho*T*lambda0+ell, and:
```text
r1 = rho*r0
   + rho*(A1*T - A0)*lambda0
   + (rho*b0 - b1)
   + (J1*u_s1 - rho*J0*u_s0)
   + h1*(J1*a1 - J0*a0)
   + A1*ell.
```

Operator attribution further expands:
```text
rho*(K1*T - K0)*lambda0
+ rho*(alpha1-alpha0)*D0*lambda0
+ rho*alpha1*(D1*T-D0)*lambda0.
```
All positive contact depths here use the uncapped second bias branch k_p*d, so:
```text
rho*b0-b1 = (rho*k_p0-k_p1)*d0 + k_p1*(d0-d1)
```
where k_p0=54.2804784311515 and k_p1=51.37735096539659. This is a transparent accounting identity; it is not another controlled solve.

The independent seven-component reconstruction agrees with the current residual to 4.17e-17. The four normal-row means are:

| Residual source | Mean normal residual m/s |
| --- | ---: |
| Inherited powered residual | -1.4234148304e-7 |
| New source kinematics | +0.007093161239406 |
| Reduced depth | +0.002847965741028 |
| Duration bias | +0.004201233239655 |
| Duration softness | -0.002876664038704 |
| K mass/geometry response | +0.000029050023726 |
| Soft diagonal mass/geometry | +0.000001186153701 |
| Acceleration difference plus accepted load shift remainder | +0.000017283552953 |
| Total | 0.011313073570282 |

Duration bias/softness net: +0.001324569200952. The final acceleration remainder reflects differing producing/current mass; it is not a second missing load correction. In the fixed-mass load-only comparisons, the accepted load shift cancels the common-normal projection of the changed external RHS; this rank-one rule does not promise cancellation of every individual row in asymmetric geometry. The mean accepted A1*ell term is approximately +0.035414 and is already included once.

Exact independent vectors, in N0,N1,N2,N3,T0,T1,twist row order:
```text
rho*r0:
[-9.434838656138717e-7,1.1578746760771442e-7,3.5711192541964553e-7,-9.878145957904839e-8,4.1389618850849222e-19,-3.5850705121068152e-19,0]
source kinematics:
[.007092829273605452,.007093493205205805,.0070932641838373,.007093058294973957,-2.316207694004218e-11,6.3613245712373654e-12,-3.74645603504964e-16]
depth:
[.0028478371471443095,.0028480943349107404,.002848003122970316,.002847928359084727,0,0,0]
duration bias:
[.0042012332396554036,.0042012332396554036,.0042012338296420065,.0042012326496688015,0,0,0]
duration softness:
[-.0028765924707348056,-.0028766461312103465,-.002876729337351924,-.0028766882155160764,0,0,0]
mass/geometry soft diagonal:
[1.1861325068293292e-6,1.1861380008302945e-6,1.1861705471889294e-6,1.186173748545986e-6,0,0,0]
mass/geometry K:
[2.90500237262233e-5,2.9050023726246887e-5,2.9050023726239023e-5,2.9050023726246887e-5,9.24792734805395e-11,-2.4468009939661807e-11,-4.2374777422829385e-16]
external plus accepted load shift:
[1.728365117412839e-5,1.7283454731169268e-5,1.7283454731176207e-5,1.728365117412839e-5,1.8161204707616744e-10,0,0]
sum:
[.011311883513211898,.011313810052487466,.0113136485600277,.011312952155400735,2.509292449721992e-10,-1.8106686313348963e-11,-7.983933777332578e-16]
```
Floating operation association differs slightly from the C# residual in convergence.json. Source ownership and current-mass arithmetic are unchanged. The controlled mass-only result and geometry-only result separately bound those effects; this algebraic combined term is not permission to call all of it a geometric cause.

## Why eight sweeps miss

The current target common reaction is lower than the historical load-corrected guess. The initial common excess is 0.018173779796074 N s per row. Ordered normal solving breaks the common symmetry into differential components. Tangent response changes normal residual again. Eight sweeps do not fully remove these coupled errors; twelve is the first sampled passing count. The exact map and physical kicks account for the normal/angular failure without blaming basis rotation.

The velocity/depth factorial gives signed contributions 62.69% and 25.17% of the final normal miss with negligible interaction. This is a specific controlled signed decomposition, not a sum of positive error magnitudes or a universal causal percentage for other fixtures.

## D derives from current residual, without oracle input

For q=(1,1,1,1,0,0,0), choose one scalar:
```text
delta = -(q^T*(A*guess-rhs))/(q^T*A*q)
D = guess + delta*q
```
Its sign reduces the overpredicted common support. It preserves normal differences, tangent and twist values. It does not reapply the old engine shift. In the coast case:
- numerator: 0.045252294281127814;
- denominator: 2.4899770322354415;
- delta: -0.018173779796073618;
- common residual after: 2.7755575615628914e-17.

The compiled diagnostic constructs D before calling the reference. The Python arithmetic reconstruction also constructs D before using reference values. The reference is used afterward to validate error propagation and physical bars.

**Measured payoff unavailable:** the one payoff process stopped at moderate-normal's current friction cap guard; its all-at-end report write was not reached. Coast D was earlier in source order, but its numeric values were not flushed. No retry or replacement correction occurred.

**Arithmetic payoff retained:** the ordered equations, independently checked against measured baseline iterates, predict coast errors 5.068848915713742e-9 N s, 6.809610604863548e-9 m/s, 1.3569883385587606e-8 rad/s. Per-stage branch checks stay interior. These are not recovered measurements.

Four prior synthetics also have interior predicted passes. Two rotated-normal synthetics refuse unchanged tangent feasibility after D; they are not evaluated through an invalid unclamped map.

## Responsibility recommendation

One class only: current-piece kinematic/bias/softness-aware common-normal residual initialization in a proven interior domain, following accepted cache meaning preparation. No full seven-row correction, extra friction policy, extra sweeps, history weakening, threshold change or installation.

The first-coast causal result is strong. Complete phase-13 measured payoff evidence and the constrained operator are not qualified. Return REVISE and stop for Project Control rather than repeat the failed campaign or claim predicted data as measured.
