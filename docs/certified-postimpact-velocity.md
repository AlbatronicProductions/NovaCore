# Certified final post-impact velocity states

**UNBANKED candidate — Certify final paired post-impact velocity states.**
Baseline: banked M14.12, `e5fde08bcae834d8abc107cd26e7601fbfa3e1e0`,
tag `m14.12-certified-preimpact-velocity`. M14 remains open.
[Evidence and reproduction](engineering-evidence/certified-postimpact-velocity/README.md).

**READY FOR PROJECT CONTROL ACCEPTANCE — UNBANKED.** The original 256-byte refusal allocation was
attributed to eight calls to the allocating `Environment.Version` getter. Runtime
version applicability is now prepared once as immutable process metadata; all
per-call floating-point witnesses remain. Corrected Debug/Release builds, full
Simulation, focused predecessor gates, ReferenceFrames, Precision and real Florida
integration pass. Both Florida fixtures have identical complete certificates and
final bits across Debug/Release. Reusable receipt/refusal paths measure zero bytes.
See the
[bounded attribution and correction](engineering-evidence/certified-postimpact-velocity/refusal-allocation.md).

## Ownership

Simulation consumes the checked [response proposal](certified-response-proposals.md)
and [pre-impact tuple](certified-preimpact-velocity.md) for the identical provider-owned
root and kinematics. It qualifies FINAL binary64 COM inertial-root and body-frame
angular velocity values. This is immutable numerical evidence, not physical state,
an impulse command, mutation permission, or a consumed token.

`Proof.QualifyPostImpactVelocity` takes the witness, both receipts and their original
requests, and current source use. Only successful issuance constructs a
`Proof.PostImpactVelocity`. Its `Read` rechecks held and supplied receipts, exact
root/witness/request applicability, and the supported arithmetic contract.
Constructible numerical result data carries no authority. Existing provider checks
retain spacecraft, state/timeline/clock, geometry, terrain, Earth/frame and physical
source ownership. The existing single-writer phase remains required; a checked read
is not a concurrency lock.

The same alpha can be nonrational. No rational event epoch is selected. A source
interval endpoint or the numerical center seed is never declared the actual event
state. Same-owner refinement does not globally revoke older immutable evidence;
different supplied root/witness/request tuples require explicit reissuance.

## Numerical policy version 1

All quantities below describe the same alpha. Let `n` be the exact physical unit
normal, `Q` the exact normalization of the stored attitude, `r` the authored body
lever, and `I` the positive diagonal principal inertia. Current source admission
already proves zero pre-impact spin. The checked law supplies:

```text
a = r cross transpose(Q)*n
k = 1/m + transpose(a)*inverse(I)*a
j = -u/k, with u < 0
J = j*n; L = j*a
vstar = Vminus + J/m
wstar = inverse(I)*L
```

Outward target intervals use the banked reciprocal/multiply division and separate
addition operations. For each of their six component intervals `[lo,hi]`, generate
`down=BitDecrement(lo)`, `up=BitIncrement(hi)` and the separate-rounded seed
`center=RN64(RN64(lo/2)+RN64(hi/2))`. Evaluate the center tuple and all 64 corners.
Exact zero is canonicalized to positive zero; no nonzero component is erased.
Each lower endpoint is strictly below its upper endpoint, so corners are unique;
the center is skipped if encountered again. Maximum evaluation count is 65.
There is no root refinement, candidate expansion or time-dependent stopping rule.

Before selection, bound each component error against its entire target interval,
covering down, up and center. Denote these ceilings by `bv_i`, `bw_i`. Derive:

```text
Brho = sum(sup|n_i|*bv_i) + sum(sup|a_i|*bw_i)
R2 = m*sum(bv_i^2) + sum(I_i*bw_i^2)
BT = m*sqrt(sum(bv_i^2))
BC = sqrt(sum((I_i*bw_i)^2)) + m*|r|*sqrt(sum(bv_i^2))
BE = m*sum(sup|Wminus_i+J_i/m|*bv_i) + sum(sup|L_i|*bw_i) + R2/2
sLower = -upper(u)
dLower = lower(u^2/(2*k))
```

Require `Brho<sLower`, `BE<dLower`, and `R2<2*dLower`, using an outward-safe
comparison threshold. These are fixed numerical-resolvability conditions; budgets
are never adjusted after seeing a candidate. They can conservatively refuse a case
even if an admissible state exists elsewhere in the binary64 lattice.

For actual final selected bits define `ev=vhat-vstar`, `ew=what-wstar`:

```text
rho = dot(n,ev) + dot(a,ew)
Peff = m*(vhat-Vminus); Leff = I*what
dP = m*ev; dL = I*ew
tangential = (Id-n*transpose(n))*dP
coupling = dL - r cross transpose(Q)*dP
Ehat = Wminus dot Peff + |Peff|^2/(2*m) + transpose(Leff)*inverse(I)*Leff/2
Estar = -u^2/(2*k)
Ehat-Estar = m*dot(Wminus+J/m,ev) + dot(L,ew)
            + m*|ev|^2/2 + transpose(ew)*I*ew/2
```

Exact-law cancellation precedes interval evaluation. This preserves the shared-root
relation and avoids subtracting astronomical inertial velocities/energies. Material
work uses **effective represented momentum**, not nominal impulse. No fictitious
free-couple work term is introduced. Tangential contraction uses the certified
physical unit normal, not a claim that every Cartesian normal-box point is unit.

The candidate certificate intersects direct finite interval evaluations with
independently proved component/triangle bounds. Empty/nonfinite intersections refuse.
It never clips a negative residual to zero or clips positive energy to zero merely
to meet an acceptance goal. Admit only `rho.lower>=0`, all declared defect ceilings,
and `Ehat.upper<=0`. Report effective linear/angular momentum defects separately.

Among admitted states choose lowest certified `rho.upper`, then lowest certified
mass-weighted state-error upper bound, then raw unsigned canonical IEEE component
bits in linear XYZ/angular XYZ order. This is deterministic finite-set selection,
not global lattice optimality. Numerical departure includes source uncertainty;
there is no uniformly tiny restitution claim for arbitrarily wide source intervals.

## Arithmetic and bounded cost

The initial arithmetic contract is Windows x64, .NET 10.0.12, binary64 nearest/even,
separate operations and gradual underflow. The half-sum and arithmetic witnesses
use non-inlined single-operation call/return boundaries. Existing interval primitives
observe their rounded result through adjacent-bit expansion before the next node.
There is no selected-path FMA. A runtime/JIT upgrade needs arithmetic qualification,
not a silent extension of this version's claim.

Per-call witnesses check even/odd rounding ties, normal-to-subnormal division,
subnormal addition and an unfused multiply/add. Integer bit comparisons prevent
DAZ from turning the expected subnormal into zero during the comparison itself.
The immutable runtime-version predicate is prepared once at this arithmetic
owner's initialization; it is not mutable state or cached floating-point capability.
No global/thread FP, tiering, PGO or GC modes are changed. Unsupported arithmetic,
required nonfinite endpoints/bounds and insufficient resolution return `Unresolved`.
Invalid or stale source receipt use is distinguished as `Unsupported` or `Stale`.

Qualification reads the checked sources once into a prepared numerical block, then
streams the fixed candidate set and retains the best result. It invokes no Earth
motion evaluation or root refinement. Preparation, qualification, checked reads and
refusals have separate timing/allocation measurements. Source evidence reconstruction
allocates and has no zero-allocation contract; reusable qualification, consumption
and refusal paths measure zero. No whole-frame cost is implied.

## Future execution and preserved contracts

Future private execution must install both final values exactly as qualified, with
its own preflight/consumption/publication lifecycle. It must not apply the impulse
again, recompute velocity addition, normalize or clamp these values. The receipt
does not remain automatically applicable after canonical time advances beyond the
provider's captured source start.

M14.4 canonical transactions and M14.5 represented-zero admission remain unchanged.
No canonical intent, mutation, event-local state, remaining duration or publication
is implemented. Nonzero off-center angular response is retained. Reacquisition,
support/rest, operational-date extension and the M14 completion controller remain
later responsibilities. Whole-engine performance characterization remains high-payoff
work deferred until M14 exit.

## General contact solver boundary

This certified isolated-contact realization is a bounded specialization. It does
not define NovaCore's future general contact/constraint-solver architecture, and
the fixed 65-state recipe is not claimed for persistent manifolds, friction,
multi-contact constraints, warm starting, stacking/support or landing gear.
Before persistent support/rest or equivalent general constraint-solving work,
the KSA / Bepu General Contact Solver Convergence Assessment is mandatory.
The presumptive direction is **adapt Bepu behind NovaCore authority**, unless
positive NovaCore-specific evidence establishes a better architecture. No Bepu
dependency or general solver is introduced by this candidate.
