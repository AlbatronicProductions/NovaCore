# Coverage certifiability: read-only source review

This review belongs to the positive-beta source-feasibility assessment. It performs no fixture execution, parameter search, production edit or test edit. Current authority is the source in `FloridaPostImpactMotion.cs`, `PostImpactCoverageProvider.cs` and `PostImpactCoverageSearch.cs`; accepted real Florida results remain prior evidence.

## Exact proof conditions

Write `s = sigma * (T-alpha)` and `g` for the current globally defined grading-plane residual. Source admission supplies exact `g(alpha)=0` and `g'(alpha)>=0`. It does not supply an arbitrary selectable post-impact velocity or root time.

The chronological depth-first search has only these acceptance routes:

1. An initial node `[0,u]` is clear when its local first derivative lower bound is strictly positive OR its local second derivative lower bound is strictly positive. The inherited nonnegative initial derivative makes the latter theorem valid. A tiny positive sampled derivative is insufficient.
2. A later node `[l,u]` is clear when its gap lower bound is positive, or a monotone derivative bound and the appropriate endpoint's strictly positive gap establish the same result.
3. A beta node requires the already-proved clear prefix to end exactly at `l`, a strictly positive left-endpoint gap, a strictly negative right-endpoint gap, and a strictly negative whole-node derivative upper bound. This proves a unique strictly approaching root strictly between those endpoints, including when `u=1`.
4. Every actual node/endpoint inclusion must pass the unchanged grading-domain guards. No later root may bypass an earlier unresolved node.

Consequently a suitable physical trajectory is not sufficient. A source-feasible beta design needs certified local positive initial curvature (or certified positive derivative), positive gap boxes throughout its prior prefix, strict endpoint signs around beta, and useful negative derivative margin around beta.

## What the accepted Coast correction can and cannot establish

The gap owner caches an initial derivative interval `R=[max(0,rawLower),rawUpper]` and a global continuation curvature enclosure `A`. It intersects the raw boxes with `s*(R+s*A/2)` and `R+s*A`. This is an exact integral-Taylor inclusion for the same trajectory.

If `A.Lower>0`, inherited nonnegative slope implies strictly positive gap for every `s>0`; no beta exists in that interval. That is precisely why Coast is a successful clear witness. It cannot be repurposed as a beta witness merely by asserting that root machinery exists.

A genuine recontact after positive departure requires negative curvature somewhere, so a valid global `A` cannot have a strictly positive lower bound. If the cached `R.Lower=0` and `A.Lower<0`, the anchored lower gap is nonpositive away from alpha. The anchor therefore cannot alone clear the intermediate positive arc. The raw gap or endpoint/derivative routes must do that work. This does not make beta structurally impossible: initially positive local curvature followed by negative curvature can satisfy the initial theorem and later approach theorem.

The cached global curvature concerns an algebraic plane extension. Full-weight terrain equivalence remains a mandatory leaf-local check. Requiring the broad `[0,1]` grading box to pass would impose a condition the current implementation does not require.

## Root uncertainty does not disappear under sigma subdivision

Let the captured pose-root interval have width `w_alpha`, neglecting outward-rounding widths for this explanation. At a fixed interior sigma, the implemented `elapsed=sigma*(T-root)` has width `sigma*w_alpha`. Before the explicit source-cell intersection, `time=root+elapsed` has width `(1+sigma)*w_alpha`. The exact linked expression would have width `(1-sigma)*w_alpha`; production does not use that latter expression here. These are interval-expression widths, not independent physical root choices. Clipping can reduce them, and the exact target is special-cased at sigma=1.

Thus shrinking a sigma node does not remove the captured-root uncertainty in translation, orientation or rotation. It also cannot remove fixed polynomial/rounding enclosures. Midpoint sign refinement may eventually refuse even with remaining depth budget. More bisections are not proof that the endpoint-sign floor will be overcome.

For a strictly approaching beta with certified speed magnitude at least `v_min`, a useful design needs signed endpoint gaps to exceed the actual one-sided enclosure excesses. A diagnostic local scale is `epsilon_g/v_min`, but this is not a certified universal root tolerance: `epsilon_g` must come from the current expression at the chosen source and endpoints. Both beta endpoints and all requested refinement midpoints need independent sign margin. A small bracket width does not by itself establish that margin.

The prior Force root about 40 microseconds after alpha is not excluded merely by the dyadic depth limit. Depth 24 gives a sigma width `2^-24`, hence elapsed width at most 59.605 ns before captured-root uncertainty. Force instead has tiny departure/gap margins, a first-derivative box crossing zero and strictly negative initial curvature. The present initial theorem cannot certify departure from those operands.

## Work and ordering requirements

- Search limits: 255 visited nodes, depth 24; a whole-node failure still consumes a visit. Endpoint evaluations add work independently of visits.
- A full balanced depth-7 traversal has exactly 255 nodes. Uniform depth 8 through the full interval would exceed the budget. Adaptive early clearance can need far fewer nodes. Coast's 63-node, depth-5 witness is one concrete example, not a bound for another trajectory.
- For a beta around normalized position `sigma_beta`, approximately `ceil(sigma_beta*2^k)` depth-k prefix leaves may need clearance in a uniform-resolution model. A conservative prefix-tree estimate is about twice that leaf count plus the path depth. This is diagnostic planning only; actual interval pass/fail must establish the work bound.
- Beta refinement is sign bisection, at most 24 cumulative steps, and total root depth at most 48. A midpoint containing zero refuses immediately. There is no interval Newton step, root-width acceptance tolerance or retry past a refusal.
- Same-owner first-root selectors compare Equal by immutable identity. Distinct owners require disjoint absolute-time enclosures or successful further refinement. Because absolute-time boxes inherit source-root width, a fixture must have an ordering separation larger than the resulting boxes; sigma refinement alone need not resolve an overlapping pair.

## Upstream gates that also constrain a proposed region

M14.9 accepts zero initial spin and zero torque, real current Sol/Earth and a singleton fixed authored feature. Its source function uses a midpoint Taylor enclosure with a conservative vector acceleration majorant. It requires whole-cell grading and strict monotonic approach before issuing alpha; later midpoint refinement refuses if a sign cannot be proved. An extra standalone refinement request is not equivalent to this initial admission contract.

M14.10's kinematic requests include fixed rotated-lever component width. That lever is independent of time in the pre-impact source; if its numerical width already exceeds the request, narrowing alpha cannot repair it. Any global upstream-impossibility claim must distinguish this condition from mere refinement request size.

M14.14 retains the provider-owned alpha interval and checks a COM-position enclosure request. M14.15 requires the target to be the captured source end, a remainder within one second, zero torque, and its existing degree-11 rotation / order-12 remainder and endpoint-error requirements. Coverage consumes those checked final velocity bits; a freely chosen diagnostic post-impact spin does not prove upstream feasibility.

## Feasibility judgment from this review alone

No structural impossibility of positive beta follows from coverage source. Initially positive local curvature and later negative curvature is a viable physical/proof shape. Conversely, no robust current-source parameter region is established by these necessary inequalities alone. A full-contract recommendation requires the lead's upstream and same-model interval evidence. Do not label the whole family B or C just because one proposal fails; failure of a single fixed lever representation or oversized request need not exclude all current-domain inputs.

Absent that positive proof, this review supports `E — MIXED / INSUFFICIENT` for feasibility and `NARROW CONTRACT` as the least-assumptive banking disposition if Project Control elects to bank demonstrated coverage now. That is a policy recommendation, not authorization to change the frozen candidate.

## Exact clean narrowing if selected later

Do not leave `NextEventCertified` as an accepted provider-owned production result with a caveat. A bounded narrowing should:

1. Restrict `CoverageEvidence.Prepare` to mint evidence only for `EventFreeThroughTarget`; all other paths issue no receipt.
2. Remove the beta-only `NextRoot` checked capability and `CoverageRootValues`, including its factory, read, refinement, comparison and beta-specific counters/limits where unused.
3. Remove `NextEventCertified` from the banked production outcome surface. Keep the shared chronological clear/refusal machinery, but map any encountered potential/strict crossing to conservative Unresolved without a beta capability. A root diagnostic must not be reusable as a checked production receipt.
4. Remove now-unused root bracket/depth fields and beta-only production/test cost claims. Preserve source/root correlation, local domain checks, clear-prefix chronology, work bounds and nonmutation.
5. Keep exact analytical crossing, grazing, multi-root and boundary controls insofar as they protect the narrowed no-false-clear/refusal contract. Beta-only read/refinement/ordering experiments may be deferred outside production; no need to preserve duplicate obsolete implementation by default.
6. Update current documentation/tests to the actual clear/refusal contract. Require unchanged Coast EventFree and Force conservative refusal, authority/stale checks, no mutation and existing zero-allocation contracts before any later acceptance.

If Project Control instead insists that coverage must bank both outcomes together, retain the candidate unbanked until one current-source positive-beta proof (or a separately scoped upstream proof change) traverses the full chain. General collision machinery is not implicated: BEPU GATE NOT REACHED.

## Adversarial result

The strongest challenge to a narrow recommendation is that spherical-inertia restitution-zero geometry can yield positive initial curvature followed by recontact; the current source could contain a robust untested region. That challenge survives. It prevents claiming physical impossibility and prevents assigning a unique upstream-versus-coverage cause without the lead's bounded study. It does not justify accepting an unqualified checked beta capability.

No source, permanent test, fixture or proof limit was changed by this review.
