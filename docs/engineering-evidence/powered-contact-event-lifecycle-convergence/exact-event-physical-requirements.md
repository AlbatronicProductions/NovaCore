# Exact event, constrained chronology, tiny limit and changing mass

These are derivations, not a newly qualified solver. The one data-only arithmetic run is retained in analytic-witnesses.py and analytical-results.json. It imports no NovaCore, BEPU or KSA code. No old qualification was resumed.

## Precision belongs to a responsibility

| Quantity | Required representation / reason |
|---|---|
| Resource debit and successor | Exact integer units: ownership/conservation and exhaustion may distinguish quantities below physical projection |
| Engine transition and event h | Exact chronological positive ratio: powered then exhausted/coast, even when float(h)==0 |
| Resource mass authority | Exact successor ledger; total physical mass is an explicitly derived rounded view, not an independently mutable tank |
| Engine J and L | Integrals over exact chronological domains; evaluate complete products before rounding. Rotating thrust/torque and changing mass need more than constant F*h |
| Contact reaction | Correct constrained response for a declared geometry, compliance, mass and friction model; not generally an exact scalar or a copy of free impulse |
| Endpoint position, velocity, orientation | Correct rounded physical result under declared model/error/rounding contract, including temporal moments and work; all endpoint bits remain authoritative after publication |
| Private caches | Valid owned continuation with coherent geometry/duration/history and qualified downstream outcomes. Exact symbolic event h does not logically require one-ULP equality of a chosen finite-sweep cache decomposition |

## Noncommutativity rejects shortcuts

For unilateral normal projection P(v)=max(v,0), P(-J/m)=0 but P(0)-J/m=-J/m. An after-solve engine impulse can violate support.

For the existing zero-bias soft normal row, k=omega*h*(omega*h+2), c=k/(1+k), s=1-c=(1+omega*h)^-2. The constrained perturbation is s*deltaV_free, not deltaV_free. Postaddition errs by c*deltaV_free. Also s(h)*s(H-h) != s(H): for omega=1,H=1,h=1/4, the factors give 256/1225 versus 1/4. The present discrete piece model is not a semigroup. Replacing its partition while demanding identical intermediate values would be a different requirement from deriving an appropriate physical model.

Temporal averaging also fails physical outcomes:
- Normal departure: supported mass m under gravity g, upward thrust 4mg for H/4, then coast. At H, v=0 and y=3gH^2/8. Spreading equal impulse as mg over H gives y=v=0. The body never intersects the floor during the pulse/coast witness.
- Friction: fixed normal load N, k=mu*N; tangential force 2k for h, then zero for 2h. It accelerates for h, decelerates for h and sticks for h. At 3h, v=0, x=k*h^2/m. Engine work and friction dissipation are both k^2*h^2/m. Equal impulse spread as 2k/3 stays below static friction and gives no motion/work. Equal final velocity alone misses the defect.

These counterexamples reject generic net-impulse substitution, ordinary force averaging and an unqualified post-solve residual correction.

## A legitimate conditional construction

For constant positive mass matrix M, fixed full-row-rank linear constraint J and a proved unchanged active/sticking regime:
K=J*M^-1*J^T
P_M=I-M^-1*J^T*K^-1*J
a_c(t)=P_M*M^-1*w(t)
lambda(t)=-K^-1*J*M^-1*w(t).

This is the already-constrained response. Integrate it chronologically:
deltaV=integral_0^H a_c(t)dt
deltaX-H*v0=integral_0^H (H-t)*a_c(t)dt.

Thus both total impulse and temporal response moments matter. Angular state requires corresponding rotational kinematics; these translational formulas are not a quaternion integrator.

For a declared fixed linear compliant model z'=A*z+B*w(t):
z(H)=exp(AH)z0+integral_0^H exp(A(H-t))*B*w(t)dt.
A pulse F ending at exact h contributes exp(A(H-h))*h*phi1(Ah)*B*F. Its chronology is retained without a backend call at dt=h. This establishes existence for the stated model; it does NOT prove equality to BEPU's timestep-dependent spring/finite-sweep map.

Admission must prove normal activity and friction regime throughout, not only at endpoints. Reaction signs, friction feasibility, geometry/lever validity, mass positivity and model error need exact inequalities or justified outward bounds. Rotating contact, sliding transitions, unilateral departure/recontact and changing Jacobians require further treatment. Unsupported cases refuse before a claimed physical advance. This is not yet a production solution for all interior exhaustion.

The useful architectural boundary is chronological constrained-event consequence prepared inside the physical operator, before committing its endpoint and private continuation. It is neither arbitrary force distribution nor a correction tacked on after BEPU. The actual retained-BEPU integration/installation bridge remains UNQUALIFIED.

## Tiny positive limit

For bounded force/torque and mass bounded away from zero:
||J_engine||<=Fmax*h; ||L_engine||<=taumax*h.
The exact engine contribution tends to zero as h→0+, but event ordering and exact debit do not cease to exist. In a fixed well-posed linear regime, variation of constants bounds response by a finite kernel times h; position uses the additional H-t moment. There is no physical requirement that the backend receive h as its timestep.

This does not license rounding h first. For h=2^-1075, F=16 N, m=8 kg, free deltaV=2^-1074 is representable although double(h)=0. Preserve products before projection. The same rule applies to the fully constrained response, which can differ from that free value. The retained arithmetic witness proves the free product identity, not a new subnormal contact qualification.

For nonlinear/active boundaries no uniform fixed-mode margin is assumed: arbitrarily small h can decide departure or friction activity. Use proved regime/error bounds or return unsupported. No epsilon-duration cutoff, tolerance, subtraction or “too small to matter” authority rule is proposed. No universal claim that private cache differences are harmless follows.

## CentralPointReservoirV1 mass consequence

Let m(t)=m0-q*t during 0<=t<=h, mf=m0-q*h>=dryMass>0, then mf afterward; COM and dry inertia fixed. For constant inertial force F during the burn:
deltaV=(F/q)*log(m0/mf)
deltaX-H*v0=(F/q)*[(H-m0/q)*log(m0/mf)+h].
Limits at q=0 must be evaluated continuously.

Neither J/m0 nor J/mf is generally correct. For constant bounded force, source- or successor-mass velocity approximation error <=Fmax*q*h^2/(2*mmin^2); midpoint mass error <=Fmax*q^2*h^3/(12*mmin^3). These bounds describe omitted physics, not a newly accepted tolerance.

For F=32,q=1,h=1/128,mf=8 the data-only witness gives exact-model deltaV approximately 0.03123475113777468467, source approximation 0.03121951219512195, successor 0.03125, midpoint 0.03123474865788189. Even midpoint is not exact.

During stationary support with nonnegative contact reaction throughout:
J_normal=g*[m0*h-q*h^2/2+mf*(H-h)]-F*h.
Using mf for the whole interval misses g*q*h^2/2. Applying a mass change too early across longer force/reaction evolution can also move later dynamics; do not extrapolate a local h^2 bound to arbitrary H.

Contact effective mass depends on 1/m even with fixed dry inertia:
K_nn=1/m+(r cross n)^T*I^-1*(r cross n).
A contact model may integrate the varying inverse-mass response without taking a separate h-sized backend step; it cannot silently substitute one universal averaged mass for all normal, tangent and angular responses.

Momentum accounting is open-system: d(mv)/dt=F_engine+m*g+R_contact-q*v under the stated ejected-mass convention. Comparing body delta(mv) only to engine J would be wrong. Work also depends on velocity along the event; the friction counterexample demonstrates why endpoint momentum is not enough.

For variable M with fixed independent J, use instantaneous M(t), projected response and chronological integrals; this is a conditional analytic extension. Coupled rotating-body force, gyroscopic terms, changing geometry and active-mode transitions are not solved by the scalar log formula.

## Result

Exact event/resource authority does not logically require separate contact solving at h=2^-1075. A mathematically legitimate integrated constrained lifecycle exists for a declared, admitted fixed regime. A general ordinary-BEPU implementation preserving all NovaCore outcomes is not proven here. That precise gap defines the proposed next ticket.
