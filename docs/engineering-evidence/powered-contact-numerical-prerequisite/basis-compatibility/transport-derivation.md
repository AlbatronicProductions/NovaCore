> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Full generalized-impulse transport

All vectors use world-oriented axes. Angular impulse is COM-referenced generalized impulse: preserve the angular velocity kick under the same current inverse inertia. We do not equate this with a wrench about one fixed external origin after the COM moves. For that different contract the target moment would be L_old+(oldCOM-newCOM) cross P. No such shift is hidden here.

Let c be the friction center, r_i the contact levers, n the shared normal, t1/t2 the actual generated tangent axes, lambda_i the four normals, u/v the tangent components and q the twist scalar.

P = n*sum(lambda_i) + t1*u + t2*v

L = sum((r_i cross n)*lambda_i) + c cross(t1*u+t2*v) + n*q.

These are the six rows of the actual impulse-to-generalized-kick Jacobian G. Historical G uses the prestep geometry that PRODUCED the accepted powered cache. Refreshed coast G uses the new levers and normal. The old cache snapshot's normal alone cannot reconstruct old L.

## Derivation independent of the diagnostic dense implementation

Solve the actual3x3 frame matrix [n,t1,t2]*(S,u,v)=P_old. Do not assume exact orthogonality or normalize input bits.

Set R=L_old-c_new cross P_old and a_i=(r_i_new-c_new) cross n_new. Then:

q_new = dot(n_new,R)/dot(n_new,n_new)

sum(lambda_i_new)=S

sum(a_i*lambda_i_new)=R-q_new*n_new.

The moment equation has two independent coordinates in the plane perpendicular to n. With a nondegenerate footprint these give3 independent equations C*lambda=b for4 normals. Choose the minimum Euclidean change from feature-mapped historical normal values:

lambda_new=lambda_old + C^T*(C*C^T)^-1*(b-C*lambda_old).

Equivalently preserve z dot lambda_new=z dot lambda_old for a nonzero vector z spanning null(C). This declared choice is unique; it does not prove every feasible normal distribution has been searched.

## Diagnostic implementation

Probe.cs builds G_new exactly from the unchanged PieceKernel columns. A cofactor null vector supplies the seventh equation. It solves7x7 with partial pivoting. Angular equations divide by patch radius ell and the twist unknown is q/ell, avoiding a mixed-unit least-norm objective. The selected gauge constrains only the normal distribution. It reconstructs both physical impulse vectors afterward.

Exact arithmetic preserves P and L whenever that augmented system is invertible. Actual FP64 output must separately pass the1e-12 absolute vector-error checks, conditioning screen, positivity and current friction caps. The conditioning calculation is a conservative engineering screening estimate, not a formal interval proof. No epsilon-normal admission and no component-wise blindly reused frame.

## Twist and lever changes

When n rotates, projecting old q*n onto n_new alone discards its perpendicular moment. Full transport shifts that part into the four normal impulses. In the nonzero-twist witness q_old=.02; a.08-rad rotation gives q_new≈.01993603357 and a normal redistribution of ±.00039957346037. Angular error is2.4747374745180785e-17 N·m·s, linear error0. Projection alone loses.0015982938858868107 N·m·s.

Changed contact levers also change normal and friction moments. They are included in G_new and may require a different normal distribution and twist even if the normal itself is unchanged. The admitted lever-only witness preserves full wrench to4.403302626461545e-17 angular error. Large leverage changes need not remain feasible or sufficiently conditioned.

## Duration and load

With fixed old/new geometry and gauge, transport T is linear, so T(alpha*lambda)=alpha*T(lambda) for positive duration scale alpha. Feasibility cones are positively homogeneous too. Numerical screening/underflow can still refuse; no claim of branch-equivalence across all magnitudes or previously unqualified tiny durations.

Use historical cache -> transport -> accepted duration transform -> current-piece common-normal load correction -> unchanged8-sweep solve. Scaling first gives the same exact-arithmetic result; the captured ordinary-duration comparison differs by at most4.163336342344337e-17 per component. Load correction uses current mass, current rows, current duration and the previous/current load difference once. It is not part of wrench preservation and must remain last.

Transport is an initialization operation. G_current, K_current, depth, free velocity, spring coefficients, mass, inertia, duration and loads are identical across coast B/C/D/E. The independent coupled reference takes none of these initial caches as input. No repropagation, world mutation or installation occurred.
