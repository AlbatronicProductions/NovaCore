# Gate 8 causal and numerical contract

Status: IN PROGRESS. This is a derivation and qualification plan, not flight admission.

## Variable mass

The accepted store law removes co-moving material proportionally from a fixed
spatial distribution. Let material origin O have velocity v, body angular rate
w, total mass m, first moment S=mc and origin inertia J. Removed material carries
its local velocity v+w×r and angular momentum. The flux terms cancel m-dot v,
w×S-dot and J-dot w in the open-system balances. Consequently

    I(c) alpha + w × (I(c) w) = moment(O) - c × force
    acceleration(O) = force/m - alpha × c - w × (w × c) + gravity

with gravity transported to the appropriate frame. A naked `-I_dot*w` term would
create artificial spin. Adding `2*w×c_dot` would confuse the moving mathematical
COM with a material point. Finite-volume store inertia participates in both
instantaneous inertia and the removed angular-momentum flux.

BEPU uses the instantaneous COM. A numerical recenter from c0 to c1 preserves O:
`p1=p0+R(c1-c0)`, `v1=v0+R(w×(c1-c0))`. Child offsets and the full inverse tensor
change in the same preparation/publication phase. No impulse, second solver
step or canonical pose import is authorized.

## Exact physical service events

The accepted finite-battery policy requires simultaneous continuous enabled
loads. The legacy static generator/load sequence remains available to its
qualified static consumers; it cannot authorize physical thrust across outage.
The bounded physical profile contains batteries and fixed configured loads,
with no generators or in-flight load reenable. This is qualification scope for
the six definitions, not a new Part Standard policy.

Let R_b be each enabled bus load rate in existing exact energy/tick units and
T=lcm(positive R_b), or one if all rates are zero. Multiply the craft-only fuel
mixture/sharing scale by T. Initial and integer-host-boundary battery charges
are integers. Each outage time E_b/R_b, clipped to the host interval, and every
difference of successive outage times has reduced denominator dividing T.
The power solver returns ordered boundaries. For fuel duration n/d, divide the
prepared integer fuel rates by d and run the existing finite-consumption event
algorithm for integer n. Activity and phase durations are then divided by d.
No fuel is charged for a consumer lacking delivery or data in that phase.

Power endpoint charge is integer `max(0,E_b-R_b*ticks)`. The existing power
solver and ledger apply that balance to the single battery per bus. Per-load delivery is its exact rate times
bus-powered duration. The order of loads cannot change outage or delivery.
Generated phase proposals and snapshots are immutable; only the existing
transaction owner may install them in a later gate.

## Arithmetic proof obligations

The enlarged mixture scale participates in the existing rate, quantity and
depletion-denominator derivations. A rational numerator can require
`63 + bit_length(T)` bits. Its denominator must divide T, be positive and be
validated before products. Depletion adds at most one positive store's event;
integer-rate division cannot increase RateBits. All comparison, product,
subtraction, GCD and activity-time compositions must be bounded from these
operands, including malformed but reachable operands before refusal.

Implemented bounds use `t=bits(T)`, `r=RateBits`, `D=1+positiveStores*r`,
`Q=capacityBits+scaleBits+D`, and `N=63+t` for craft rational time:

| Reachable expression | Conservative magnitude bits |
|---|---:|
| Inventory/rate comparison and depletion products | Q+r |
| rate × (n×d-p), work/elapsed composition including carry | N+D+r+1 |
| Returned duration denominator | D+t |
| Signed hex value constructed before sign/canonical rejection | 4×(floor(Q/4)+2) |
| kg observation denominator | D+scaleBits+1094 |
| Observation rounding | numerator+1074 or denominator+971, plus carry |

Fuel scratch is the maximum of the first, second and parser bounds, with a
carry for quantity composition and separate positive signed headroom. Physical
observation has its own derived bound and admission. Cold LCM, scale, rate and
mixture products are checked before multiplication. Legacy static networks keep
their identity/scale/qualified expressions. Craft snapshot identity now seals
the arithmetic policy and both scales; earlier-scale snapshots refuse.

The accepted short/long fuel magnitudes require 8,672 / 8,673 bits respectively.
An incomplete core/tank draft instead exposes the signed-hex parser maximum;
its permanent refusal witness protects this separate reachable expression.
Exact power cut sorting uses bounded pair products, not an accumulating product
of every bus denominator. The power snapshot JSON bound includes all explicit
bus cursors (a 32,001-bus adversarial draft roundtrips).

## Continuous numerical domain

The physical evaluator reuses `AssemblyDynamics`, observing exact immutable
service phases. Quantities interpolate monotonically between correctly rounded
endpoints; exact ledger values remain authoritative. The dry central tensor is
a positive-semidefinite lower contribution to every total central tensor.
An outward-padded dry Gershgorin bound supplies positive lambdaMin; the certified
origin-inertia trace supplies Imax. Actual RCS resultants and the authored main
gimbal's forward angular range bound COM torque over the complete COM box.

For `u=|omega0|`, trial steps use `U=u+1` and
`h*(torqueMax/lambdaMin + Imax/lambdaMin*U²) <= 1/2`, so the continuous angular
bootstrap cannot cross U. `h*U <= 1/32` prevents high-rate orientation aliasing.
Each step also compares one RK4 step to two half steps with component-scaled
128-unit-roundoff tolerance. The more accurate two-step result is retained.
This is a qualified numerical acceptance/refinement test, not a claim of a
formal global error theorem for arbitrary flight trajectories. Maximum trial
size is 1/1024 second, host phases <=1/64 second, and 16,384 trial attempts bound
preparation. Unresolved precision/workload refuses before canonical publication.
This is unrelated to the old finite playable-episode endpoint.

Permanent exact-spin witnesses accept 10 and 100 rad/s accurately and refuse
1,000/5,000 rad/s when the bounded preparation cannot resolve them. An independent
60-digit Decimal oracle recomputes finite-volume inertia about instantaneous COM
and integrates nonprincipal spin with unequal fills at 128/256 substeps. It is
retained in `tools/vehicle-construction/check-modular-dynamics.py`. Gimbal target
arrival inside a step, every legal engine clock, and unpowered hold are covered.

## Authored convex/contact preparation

The same `LocalContactWorld` prepares all 69 authored convex regions through
BEPU. Native hull construction/recentering never supplies physical mass.
The full authored tensor supplies native inertia. Float transport is checked
against the existing 2 mm contact tolerance and accumulated reference-change
residue. Native recentering prepares all values before writes and preserves the
material origin and its velocity; it is not a second physics step.

Four authored feet supply a rectangular support profile. Physical hull vertices
must stay above the foot plane; any initial contact must lie within a foot patch.
The engine hard-body hull already includes its authored gimbal sweep. Exhaust
keep-outs may extend through the plane, as expressly authored in Gate 4; they
are craft-hardware clearance constraints, not plume/ground thermal certification.

Static load bounds allow every contact point to move within its finite pad.
For a low-side foot, one axis gives
`reactionFraction <= (oppositeOuterEdge-COMprojection)/(oppositeOuterEdge-ownInnerEdge)`;
the high side is symmetric. The minimum of both axis bounds applies. This covers
arbitrary nonnegative four-foot reactions rather than assuming equal springs.
The entire COM box, two-degree gravity projection and 9.81 m/s² reference gravity
are checked against each authored load limit. Dynamic landing/impact certification
is outside this launch campaign. Actual BEPU stationary support and repeated
unequal-fill body/child recentering are tested separately.

Gate 9 still owns live Florida publication, actual site gravity/transport,
contact evolution coupled to resources, and canonical handoff. These pure
preparation results cannot self-declare ADMITTED or Player PASS.
