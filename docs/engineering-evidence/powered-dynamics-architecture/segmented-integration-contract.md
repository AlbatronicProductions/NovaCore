# Exact segments and approximate numerical trajectory

Recommendation, not implementation or a claim of certified trajectory error.

## Exact authority first

S=1,000,000 ticks/second; M14.24 resource quantum Qmass=1/(S*2^1074)kg. For source units U, stored flow units V and canonical interval n ticks:

| Classification | Ordered physical history |
|---|---|
| NoDemand / NoFeed | One unpowered interval; all separately admitted environmental dynamics remain |
| FullPowered | Full n ticks powered; resource successor U-nV>0 |
| EndpointExhaustion | Full n ticks powered; zero resource at target; no nonzero unpowered remainder |
| InteriorExhaustion | Powered U/V ticks, then unpowered (nV-U)/V ticks |

The exact durations sum to n. M14.24's fixed 2176-bit numerator/denominator is accepted directly. Do not convert its event to UInt64-denominator `PhysicalEventEpoch`, round it to a canonical tick, sort it by a double, or accumulate float time to choose the target. Original `T(n)=T0+floor(n*S/60)` and integer publication clock remain authority.

Powered integration includes its left-hand force at the terminal quadrature stage. The subsequent segment uses zero engine wrench. The endpoint force value has measure zero and adds no impulse. At endpoint exhaustion the whole preceding interval remains powered, while the copied actual endpoint activity is exhausted/inactive. No exact-zero-length RK segment is invoked.

## Select a scaled local-time RK adapter

Reject a **bare** conversion `double h = numerator/denominator/S` followed by ordinary h*f. It can erase valid physical effects: with fuel 2^-1074 kg, q 2 kg/s, T 6000 N, h=2^-1075s rounds to zero while its impulse and delta-v at 1 kg dry mass round to 1.482e-320. Returning UnsupportedDuration solely for that event would also violate the accepted resource contract.

Use the exact ratio as authority and a bounded exponent-scaled numerical view as an integration tool. Represent a positive numerical duration by a normalized binary significand and a separate integer exponent; do not first materialize an underflowing double seconds value. A checked fixed-width ratio operation supplies correctly rounded significand plus a conservative error bound (guard/sticky or directed enclosure). Evaluate weighted products such as h*F/m, h*tau/I and h*omega with exponent scaling and safe operation order. Do not overflow F/m or underflow h before their representable product is formed. A compensated residual may be retained when needed by the declared error budget.

This adapter is NEW future numerical work, not an API currently available in M14.24. Its fixed workspace must be derived from admitted limb width and products, not assumed to fit the existing 34-limb resource amount. Fixed widened arithmetic is possible; no dynamic BigInteger, per-stage allocations or unbounded division loop belongs in production. The implementation ticket must prove the width/shift bounds and converter rounding with an independent integer oracle before physical acceptance.

Parameterize a segment by lambda in [0,1], and integrate `dy/dlambda = h*f(y,lambda)`. The RK stage weights therefore carry h through the checked scaled-product boundary. Subdivision uses exact dyadic lambda endpoints derived from integer indices, not accumulated doubles. For powered stages, evaluate nonnegative resource mass as

    dry + successor + (1-lambda)*consumed

in exact unit/rational arithmetic before the specified rounded stage observation. This avoids subtracting two almost equal double masses. At exact segment endpoints use the banked correctly rounded exact mass-law observation. Canonical fuel is never updated from RK stages. An unpowered segment after exhaustion uses dry mass; NoDemand with fuel retains wet mass.

The actual arithmetic target is the COMPLETE local weighted right-hand side `(h * delta_lambda) * f`, including RK coefficients. Do not materialize an unscaled f, or even h*f, if a later dyadic subdivision factor makes the final increment representable. This rule includes gyro products/cancellations and quaternion products, not only F/m. Share existing mathematical primitives only where the admitted envelope proves their intermediates safe; otherwise use a qualified scaled algebraic equivalent of that same derivative. Cold physical admission must explicitly bound every unscaled intermediate, stage quaternion length/domain and final output. Duration underflow alone is never an exclusion. Test weighted finite results whose naive intermediate multiplication/division overflows.

A small rounded h view is permissible in ordinary representable cases when its error is included; it is an optimization of the same contract, not a fallback that changes ordering or loses a positive segment. A segment may have zero *represented final state change* only after the qualified numerical path/error accounting establishes that result, not because h rounded to zero. Even then its exact fuel, activity duration and event commit remain real.

## Bounded coupled integration

Choose a narrow sibling coupled evaluator, sharing current Euler/quaternion primitives. State is x,v,Q,omega; mass is a supplied exact-law stage function, not a second numerically integrated fuel state. Four coupled derivative stages evaluate orientation, mass and body thrust together. Accepted substeps use existing quaternion normalization/hemisphere policy. The original constant-root-force and read-only physical-event evaluators keep their semantics.

The existing 10,000-tick RK step is only a starting comparison. It is NOT a proven step for low dry mass, rapid depletion or high angular rate. Before implementation qualification, declare a physical envelope (mass/dry ratio, force/torque, inertia, rate, coordinate/velocity ranges and numerical output validity), component error budgets and a maximum deterministic dyadic subdivision depth/work count. Select/refine against that policy, with fixed ordering and an explicit bounded failure if an otherwise inadmissible physical model exceeds it. Normal fractional or underflowing event time is never itself a reason for refusal.

Implementation may use bounded deterministic step doubling against component-scaled errors plus cold analytical derivative bounds. Step doubling is an error estimator, NOT a rigorous universal certificate; validate it with independent high-precision fixtures and derivative bounds over the admitted envelope. No host timing, render frame, unbounded adaptive loop or retry-until-pass may select the trajectory. On failure, leave the entire canonical bundle unchanged and report numerical-model admission/integration failure. Do not consume fuel first.

Acceptance is conjunctive: the source and EVERY stage satisfy the admitted domain; conservative truncation, weighted-rounding and normalization bounds propagated to the endpoint fit EACH declared component budget; and any enabled step-doubling consistency check passes. An estimator pass alone cannot accept an endpoint. The implementation must supply those independently justified bounds and the finite subdivision/work limit before physical qualification. If no admitted subdivision proves the budget, return bounded integration refusal without consuming any canonical resource. Reference convergence is an independent validation of this policy, not a substitute for the acceptance predicate.

## Error contract, not an exact-trajectory claim

For a normal scaled duration with p significand bits, relative duration rounding can be bounded by 2^-p; unlike an ordinary double h, the bound need not collapse at the binary64 subnormal exponent. Propagate the rounding enclosure through weighted derivatives; count stage mass/force projection, arithmetic, quaternion normalization and final state rounding separately.

On each smooth segment with positive dry mass and bounded derivatives, classical RK4 has local truncation O(h^5) and global O(h^4). In a declared component-scaled norm and a domain with Lipschitz bound L, a conservative propagation form is

    endpoint_error <= exp(L*H) * sum(local_truncation_bound
                                        + weighted_arithmetic_bound
                                        + normalization_bound).

This is an error-budget structure, not measured NovaCore constants. Exact event splitting prevents a discontinuity from being crossed by a powered RK step; it does not eliminate truncation error. The implementation must derive/qualify actual bounds for its declared fixture envelope before accepting it. No unqualified universal claim for every finite binary64 physical input is made.

The rounded powered and unpowered duration views need not sum bit-exactly to the rounded full duration (the witness demonstrates this). Never repair this by moving the exact exhaustion event or debiting rounded time. Integrate each exact logical segment with bounded numerical error, then publish the original exact integer target and exact whole-interval debt subtraction.

## First-slice tests

Require independent rational conversion/mass-stage tests, tiny positive duration with representable impulse, tiny unpowered remainder, endpoint/interior ordering, full/no-feed/no-demand, and cancellation-sensitive analytic position/velocity checks. Add rotating off-COM sign/mirrored-mount and changing-orientation reference tests. Verify deterministic convergence/work bounds, finite output/refusal and no old-mass endpoint shortcut. Equal physical sources and exact command/interval histories under different host partitions must produce the same endpoint bits under the existing same-build/machine contract.

Report errors in physical units, convergence and the qualified range. Do not claim cross-platform bit identity or an exact trajectory. Zero warmed allocation uses the qualified checked helper with positive control; timing remains a separate normal-runtime measurement.
