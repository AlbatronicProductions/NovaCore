# Candidate C: one endpoint from chronological temporal moments

Definitions: hc=H-h, mf=m0-qh, L=ln(m0/mf), f=Fx+k*Fy, I=Iy.
The candidate forms exact input products before projection and uses
700-digit Decimal arithmetic. No binary64 h enters any backend; no backend
exists in this standalone proof. There is no event-sized contact solve,
no intermediate body endpoint/cache, no average force substitution.

v1=v0-k*g*H+(f/q)*L

x1=v0*H-k*g*H^2/2+(f/q)*[(H-m0/q)*L+h]

Jn=g*(m0*h-q*h^2/2+mf*hc)-Fy*h

Jt=-k*Jn; Jtw=-c*Jn

w1=w0+(e*Fx*h-c*Jn)/I

Mt=m0*(H*h-h^2/2)-q*(H*h^2/2-h^3/3)+mf*hc^2/2

theta1=theta0+w0*H+[(e*Fx+c*Fy)*(H*h-h^2/2)-c*g*Mt]/I

Normal position/velocity and roll/pitch remain zero. Support moment
impulses are e*Fy*h about X and d*k*Jn about Z. Final mass=8, fuel=0,
COM and dry inertia unchanged. q is positive in this frozen list; a general
q=0 API is not implemented or silently divided by zero.

## Work

L0=h-(mf/q)*L

Lm=(m0^2-mf^2-2*mf^2*L)/(4*q)

Jnp=g*(m0*h-q*h^2/2)-Fy*h

integral_power(N*v)=v0*Jnp
-k*g*[g*(m0*h^2/2-q*h^3/3)-Fy*h^2/2]
+(f/q)*(g*Lm-Fy*L0)

Add coast g*mf*(vh*hc-k*g*hc^2/2), multiply by -k. Here vh is only a
scalar integration constant used in a work primitive, not a stored
physical event endpoint or backend step. Angular work follows the explicit
quartic primitive of (A-Bt)*(w0+C1*t+C2*t^2), with
A=g*m0-Fy, B=g*q, C1=(e*Fx-c*A)/I, C2=c*B/(2I), then polynomial coast.

These formulas are analytically valid, but direct binary64 evaluation would
cancel tiny moments. This proof's 700 digits leave >50 digits after h^2
cancellation at h=2^-1075. This is NOT a production precision/performance
proposal. A stable bounded log1p/moment implementation would need separate
error and cost qualification; it is not built here.

## Tiny versus zero

The original nine-case execution passed. A separate, declared supplementary
evaluation of the same tiny/zero pair checks exact positive-minus-zero
moments to relative error <=1e-40, rather than accepting tiny loss under
the ordinary absolute bar. Actual maximum relative error is 5.091e-375.
force*h=3.95252516673e-323 N*s; h itself rounds to binary64 zero. Exact
fuel is one 2^-1074 kg resource unit. Candidate/reference body endpoints
round identically between h=0 and h>0. Aggregate reaction moments can still
have nonzero high-precision differences. No inherited 13-cache-field
one-ULP condition is imposed. See tiny-differential.json.

Ledger fields in JSON record the analytical one-transition/one-debit model;
they are not a new execution-lease, rollback or publication implementation.
Those banked authority responsibilities remain unchanged and necessary.
