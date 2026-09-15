> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Common-normal preconditioning: derivation and declared gates

Declared before implementing/running the correction. The historical convergence evidence and original candidate source remain unchanged. The correction is a separate numerical candidate, not a banked BEPU or canonical policy change.

## One bounded mode

For N=1..4 normal rows, J_i=(n_i,r_i cross n_i), W=diag(inverse mass,inverse inertia), K_ij=J_i W J_j^T. The normal block of the pinned compliant equation is A=K+alpha*diag(K_ii), alpha=1/[omega*h*(omega*h+2)]. The current critical-damping contract is retained.

Define the common **impulse** mode u=(1,...,1). Changing prepared generalized external acceleration by delta_a changes free velocity by h*delta_a, so the normal right-hand-side change is -J*h*delta_a. Project the correction onto this ONE declared mode by virtual work: u^T(A*u*beta + J*h*delta_a)=0. Therefore

`beta = -h * sum_i(J_i delta_a) / (sum_ij(K_ij) + alpha*sum_i(K_ii))`.

This is a rank-one Galerkin prediction, not the complete normal or seven-row endpoint solution. Its residual orthogonal to u, including differential torque demand, is left to the unchanged eight sweeps. Unequal lever arms, effective diagonals and normals are all used. Equal impulse SHIFT does not assert equal final reactions or equal row response. Pure torque on balanced supports has zero common projection; on unbalanced supports sum(r cross n) can project it nontrivially. The existing solver must still meet every accuracy bar after that projection. No wider torque-solving claim is made.

An explicit shared offset preserves the input differential normal cache representation exactly; materializing ordinary doubles incurs normal rounding. Tangent/twist fields are never inputs to or written by the preconditioner. Cached differences are not cleared or multiplied. Negative resulting guesses are refused, not clipped. A repeated unchanged nonzero load must produce exactly zero adjustment.

Dimensions: J_i delta_a is m/s^2, K has 1/kg (angular terms include metres twice and 1/(kg m^2)), h*J_i delta_a is m/s. Beta has kg m/s = N s.

For the symmetric rectangle with N=4, sum K=16/m and sum diag=4*Kd; centered delta_a=T/m gives beta=-h*T/(4+m*alpha*Kd), exactly the accepted specialization. No count, rectangle constant or load magnitude occurs in the general implementation.

## Prepared mass/load/time and applicability

Previous and new immutable load facts each contain gravity acceleration, external force and external torque. Both are evaluated using the SAME current prepared inverse mass/inertia that the local solver uses: delta_a=(g_new-g_old)+W_linear*(F_new-F_old), delta_angular=W_angular*(tau_new-tau_old). Thus unchanged gravity cancels as an acceleration; a changed gravity input does not. Midpoint mass remains the solver's declared approximation. A mass-only transition with unchanged force facts is left to the solver's changed K; it is not secretly preconditioned using old mass. A nonzero unchanged force also has zero transition at a common stage mass. This corrector does not claim to fix independent mass-, geometry- or duration-history cache mismatch.

Use the banked mantissa/exponent duration representation. With k=(omega*h)*(omega*h+2), compute beta=-h*k*sum(J delta_a)/(k*sum K+sum diag) using exponent-scaled products. Do not form ordinary float dt or alpha's potentially overflowing reciprocal. The returned signed scaled beta remains nonzero even below binary64 range. A tiny retained-world result requires the consumer to retain that channel through its actual cache/state transport; a standalone scaled beta is not that proof.

Domain: 1..4 finite nonzero unit normal Jacobians, positive finite inverse mass, symmetric positive-definite inverse inertia, fixed current geometry, positive exact prepared h<=16,667/1,000,000 s (the existing integer lattice maximum), positive finite omega, critical damping. The scaled exponent bound -4096 exceeds the banked exact-ratio arithmetic's 2,240-bit workspace range; it is not a minimum physical dt or replacement timestep. FP32-generated normal length has the declared eight-ulp input check; input geometry is never normalized. Refuse invalid/nonfinite inputs, response degeneracy, overflow, unsupported count/duration, or negative predicted normal initialization without mutation. Split-sign refusal happens before projection so subnormal rounding cannot hide a negative guess. No global mutation, oracle or fallback. No assumed parallel normals. Final friction-active eligibility is tested by the solver/reference separately.

## Predeclared execution order / stop

1. Synthetic tests once Debug then Release: symmetric accepted beta within 1e-12 N s; independent projected-system residual within 1e-12 of its normalized scale; N=1,2 unequal,3,4 asymmetric, nonparallel normals, off-axis force, tangential force, symmetric/asymmetric torque, identity, mass-only, permutation, malformed/negative-guess refusal, and scaled h=2^-1075 (no physical operator qualification from that primitive).
2. Source-link the unchanged captured solver for centered and load cases; retain eight sweeps, order, original captured geometry/mass/material. Loads fixed 0/8/16/32/64 N. Three physical error bars remain 1e-4. Compare corrected/warm/cold and preserve all cache parts. One Debug execution; stop first material failure. Release confirmation only after Debug passes.
3. If local gates pass, one retained world: same box/slab/source mass,120 original cold preparation steps. Exact interior split of a 16,666-tick interval into 8,333 ticks powered32 N (all 1/128 kg consumed), then8,333 ticks coast at dry8 kg. This is a dyadic mass midpoint, exact integer subpiece schedule. Same actual captured four-row geometry/cache, updated pose/cache and collision refresh between pieces. Local seven-row independent reference and unchanged1e-4 bars at EACH piece before claiming continuation. Compare cache/handle/features; no force averaging. A duration-history or continuation failure is a material blocker, not authorization for another correction.
4. Only if all earlier gates pass: full tiny-channel transport, selector same-input checks, fixed off-COM force32 N at x=.25 m (torque8 N m),1,200 exact intervals/final600 support using existing .020 m penetration/.0005 m drift/support-speed bounds; local impulse/linear/angular1e-4 bars; independent reference admission before candidate interpretation. Strongest sequence repeat twice, zero-constraint M15 shared no-environment domain with existing1e-6 position/velocity and1e-9 rotation/rate bars. Stop on any material failure, do not widen the common mode.
5. Only after correctness: zero warmed allocation with checked helper/positive control, storage, then direct Release ordinary-runtime cost. No cost/allocator work on a correctness-failed candidate.

No live contact, departure, canonical/resource writes, M15.1, banking, iteration tuning or historical evidence rewrite.
