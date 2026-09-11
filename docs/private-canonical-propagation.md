# Paired private spacecraft propagation

Banked M14.15: **Certify paired private spacecraft propagation to a canonical
target**. M14.15 is the banked baseline. This is conditional private
continuation, not authoritative advancement or contact coverage.

**BANKED M14.15.** The final normal Release
bar passed 5/5 fresh processes, 46/46 groups each, with exact-zero gates intact.
Outstanding ReferenceFrames, Precision, focused predecessor and Florida/query/stale
integration gates passed in both configurations; qualified analytical/Florida and
bit-replay evidence remains valid. Historical candidate-associated allocation
witnesses remain retained with production causality not established. They were not
reproduced in the final bar; no production or measurement correction was made.
See [final acceptance](engineering-evidence/private-canonical-propagation/final-acceptance.md).

## Ownership and target

`Proof.PreparePrivatePropagation` consumes M14.14's checked private initial state.
The expected realization and pose request are opaque keys required by that existing
read contract, not independently reconstructed physical inputs. One immutable
`PrivateCanonicalState` receipt retains the source and complete endpoint. Its read
validates the held and expected initializations again without rerunning propagation.
Constructible snapshot data grants no authority.

The only target is the captured integral `SourceEnd`. The strict crossing theorem
establishes `SourceStart < alpha < SourceEnd` inside one whole-second cell. Alpha
is never selected, rounded or converted to `PhysicalEventEpoch`. The duration is
the linked expression `D=T-alpha`; intervals enclose it. Rotation intersects its
duration enclosure with the theorem's `[0,1]` domain. Translation may use the wider
outward enclosure of the same expression. Neither selects an independent time.

Engine/frame/terrain/geometry, properties, states, revisions, clock and pending
boundary applicability remain owned by the checked source. Success and refusal
perform no writes, event consumption, history update or debt service. The receipt
is not a consumed token or concurrency lock.

## Physical calculation

Source binary64 values denote their exact real values. M14.13's final represented
velocities are the exact initial derivatives, not impulses. Initial attitude
denotes exact normalization of stored quaternion bits. Current torque is zero;
principal inertia is constant and positive.

With `tau=T-t0`, `a=F/m`, `Vpre=V0+a*tau`, `Xpre=X0+V0*tau+a*tau²/2`,
translation evaluates `X=Xpre+(Vplus-Vpre)*D+a*D²`, `V=Vplus+a*D` outward.
This equals the root-position-plus-remainder continuation and preserves orbital
velocity cancellation. Exact zero force plus unchanged velocity bypasses numerical
zero coefficients, making the endpoint independent of the root bracket.

Rotation advances `omega_i'=(Ij-Ik)*omega_j*omega_k/Ii` and `q'=q*(omega,0)/2`.
Asymmetric zero-torque body spin evolves. Version 1 uses degree eleven with a
uniform twelfth-order Taylor-Lagrange remainder. There are no substeps, adaptive
retries, refinements or order increases. Canonical RK4 and M14.7 remain unchanged.

Conserved `H=sum(Ii*omega_i²)` bounds all intermediate spin by
`Omega >= max sqrt(H/Ii)`. Let `C >= max abs((Ij-Ik)/Ii)`. Uniform derivative
component bounds divided by factorial are `b_n=Omega^(n+1)*C^n` for spin and
`a_n=Omega^n/n! * product(j=0..n-1)(3/2+j*C)` for quaternion. Unit quaternion norm
provides `a_0=1` at every intermediate point. Outward positive convolution computes
these bounds; remainder radii are `b_12*Dmax^12` and `a_12*Dmax^12`. Tests use the
independent exact-rational closed forms. Conservation/convergence samples alone
are not the integration certificate.

## Endpoint realization and arithmetic

Endpoint selection uses an explicitly rounded, overflow-safe half-sum center. It
selects output bits, never alpha. Quaternion realization applies the existing
normalization/sign policy, then encloses exact normalization of the FINAL bits.
The smaller upper bound of `norm(Q-qhat)` and `norm(Q+qhat)` certifies sign-invariant
unit-quaternion chordal error. Future publication must install certified bits
without another unaccounted realization or normalization.

Requests bound absolute position, velocity and spin component errors, and
dimensionless attitude chordal error. These are numerical errors, not terrain
contact tolerances. Root enclosure, polynomial intervals, truncation radii, final
enclosures and representation errors are retained. Root uncertainty and outward
arithmetic coexist within polynomial intervals; they are not disjoint measured
error terms. Component enclosures do not claim every enclosed tuple is realizable.

Version 1 uses the qualified .NET 10.0.12 Windows x64 arithmetic guard, rounding and
gradual-underflow witnesses. No global FP mode changes. Outward primitives and
endpoint half-sums have explicit rounding boundaries. Normalization/sign semantics
remain the existing policy. Debug/Release replay includes complete endpoint bits
and certificates.

## Coverage and refusal

Every ready result states **EVENT COVERAGE UNKNOWN**. Free continuation can cross
another contact before T. The pre-impact root theorem does not certify rotating
post-impact coverage. No publisher, renderer, public snapshot or clock consumes
the staged result. Subsequent-root discovery, coverage and atomic publication
remain separate. No one-event-per-interval completion flag exists.

Invalid/foreign inputs, changed keys/requests, stale applicability, unsupported
targets, unproved ordering, nonfinite arithmetic and unmet numerical bounds refuse
without partial output. Fixed-order insufficiency is an explicit work-limit
refusal; it never changes the numerical method.

KSA's prepared worker state, paired staged result and later application are adopted;
exact-root certification is NovaCore's adaptation. No persistent support, friction,
manifold, warm start, accumulated impulse or general constraint solver is introduced.
Those retain the Bepu convergence gate.

See [validation and reproduction](engineering-evidence/private-canonical-propagation/README.md).
