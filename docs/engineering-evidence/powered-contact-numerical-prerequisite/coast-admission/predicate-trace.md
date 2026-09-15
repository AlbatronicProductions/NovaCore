> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Exact UnsupportedSupport decision tree

The only executable `UnsupportedSupport` return in current production/diagnostic C# is [CacheHistory.cs:147](../../../../tests/NovaCore.ContactNumerics.OwnedCacheProbe/CacheHistory.cs#L147), following `Supported == false`. Enum declaration and two existing refusal assertions are the other search hits. CommonNormal and duration arithmetic have different statuses and execute later.

## Outer admission

In source order, `Prepare` checks owner thread, busy/invalidation, world/body/constraint/generation and ordered features, monotone piece/kind, valid duration at most 16667/1000000 s, exact accepted endpoint velocity, and all seven native cache projections. Refusal here yields WrongThread, Busy, Invalidated, IdentityMismatch, StalePiece, InvalidDuration or CacheTransportChanged, not UnsupportedSupport. Reaching Supported in the actual trace establishes these branches passed; this is control-flow evidence, not additional instrumented telemetry.

Actual coast: private identity world1/body0/body-generation1/constraint0/manifold-generation1; features `[-4,-21,-5,-20]`; accepted piece1 Powered, requested piece2 Coast; h_old=1/128 s; h_new=17707/2000000 s; accepted endpoint/cache match. The generation values are explicitly maintained fixture-owner identities, not independently queried BEPU generation counters.

## Actual coast evaluation order

P labels below are passing conditions. In raw JSON, `rejectionExpression` is the original rejecting expression; PASS means it was false. The final two source predicates use Accept wrappers and are recorded as their passing expressions. All boolean values and original short-circuit order are preserved.

| Check | Condition required | Actual input / units | Result |
|---|---|---|---|
| P1 | rows.Length == 4 | 4 contacts | PASS |
| P2 | depths.Length == 4 | 4 depths | PASS |
| P3 | body.Valid | inverse mass .125 kg^-1; inverse inertia diag(.5,.5,.5), offdiag0 (kg.m²)^-1 | PASS |
| P4 | previous load finite | gravity(0,-9.81,0) m/s²; force(0,32,0) N; torque0 N.m | PASS |
| P5 | current load finite | same gravity; force0; torque0 | PASS |
| P6 | omega > 0 | 188.4955596923828 rad/s spring frequency | PASS |
| P7 | omega finite | same | PASS |
| P8 | source linear velocity finite | (-5.732312402528805e-8, .007093304768204689, -2.087629411562375e-7) m/s | PASS |
| P9 | source angular velocity finite | (-4.1787228610701277e-7, -8.034676604968475e-18, 1.1474136840661231e-7) rad/s | PASS |
| P10 | previous translational drive finite | (0,-5.8100000000000005,0) m/s² using current dry inverse mass | PASS |
| P11 | current translational drive finite | (0,-9.81,0) m/s² | PASS |
| P12 | previous angular drive finite | 0 rad/s² | PASS |
| P13 | current angular drive finite | 0 rad/s² | PASS |
| P14, row0 | row.Identity == features[0] | -4 == -4 | PASS |
| **P15, row0** | **row.Normal == accepted.Normal** | **(0,1,-3.1946030265572745e-9) != (0,1,7.002423640933841e-11)** | **FAIL** |

This is component numeric equality of `D3`, with no tolerance. It is not a quaternion comparison, unit-length test, depth test, or bitwise comparison that distinguishes signed zeros. Only Z differs here. Dimensionless delta is `-3.264627262966613e-9`; both X values are zero and both Y values are one.

The old Z binary32 bits are `2E99FC24`; current Z bits are `B15B880D`. Both are widened exactly to binary64 for admission. The current value appears identically in the fresh callback manifold and extracted solver prestep.

## All later branches: NOT EVALUATED by actual coast admission

For each row, source next checks finite normal, finite lever, `abs(n.n-1)<=8*2^-23`, finite depth, `depth>0`, `depth<=.020 m`, previous/current generalized normal drive `<0`, `abs(n.v+(r×n).w)<=.0005/.016667 m/s`, then positive cached normal mantissa. It accumulates normal/twist caps only after that row passes. After four rows: tangent magnitude `<.125*sum(normal impulses)`, then absolute twist `<sum(.125*normalImpulse*contactRadius)`. These tests retain their original strict/non-strict inequalities.

`body.Valid` means finite positive inverse mass, finite tensor entries, and positive leading principal minors of the inverse-inertia tensor. Here the minors are .5, .25 and .125. `Load.Finite` checks all gravity/force/torque components. No body-validity or load-finiteness subcondition failed.

Captured values permit separate offline inspection of the unreached branches, without relabelling them executed: every current row is finite; normal squared length rounds to1; depth .00035905587719753385.. .0003590608830563724 m; previous/current drives -5.8100000000000005/-9.81 m/s²; speed .007092981090693229.. .007093628445716149 m/s < .029999400011999758. Every normal cache is positive. Tangent magnitude1.7906284269367387e-6 N.s < .052524688872234875; twist1.592006543710134e-17 N.m.s < .05872438740767197. These facts do not qualify changing the failed basis condition.

The unit-length bound `8*2^-23` is NOT an existing angular-equivalence tolerance and must not be repurposed as one without qualification.

## Controls and nonmutation

The original powered-source admission executes all 63 Supported conditions successfully. At the accepted successor, another powered admission keeps geometry, current dry mass, current coast duration, cache/provenance and endpoint identical; only piece classification/force is powered32 N instead of coast0 N. It fails at exactly the same P15. Both admissions leave the accepted tuple unchanged and uninvalidated while being inspected.

The separate history-free diagnostic classifier removes historical-normal equality, previous-load compressive inequality, normal-cache positivity and tangent/twist cache feasibility. It retains current geometry/load/speed checks and finite derived old-load values. It returns true at both successor comparisons. It never prepares a solver capability, modifies the real predicate, or runs a solve. This separates physical support from history/basis applicability; it is not a single-variable corrected-operator experiment.

After the actual refusal, the original failure handler invalidates continuation and preserves accepted piece1/cache/endpoint/h=1/128. Duration scaling, load correction, coast reference, solve and installation are all unreached.
