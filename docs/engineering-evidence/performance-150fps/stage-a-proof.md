# Stage A: current terrain-query coverage

Owner: `CraftTerrainColliders.Ensure` previously computed every child's eight
outward-rounded footprint corners before discovering that retained terrain
already covered the query. The same projection also repeated above the graded
slab, where the existing terrain-only rule requires no terrain mesh.

The correction builds a union of CURRENT native child AABBs, including each
current child transform/COM offset, speculative margin, predictive sweep and
mesh-reduction smoothing. It does not cache a pose. Site, terrain source and
frame origin are immutable for a collider owner; world recreation creates a new
owner. The committed tile rectangle changes only after successful preparation.

For a positive radial denominator, the linear-fractional gnomonic projection has
its extrema at corners of the union box. Eight projections therefore enclose
geometric queries from all children. That geometric fact alone is insufficient:
the original interval operations can overhang the geometric result through
rounding and dependency. An adversarial case exposed approximately 1.16e-10 m
of such overhang during development.

The implementation consequently propagates a separate forward-error bound over
the original expression tree. Each value carries an exact-magnitude upper bound
M and an absolute endpoint-error upper bound E. Addition/subtraction adds errors;
multiplication bounds error by Ma*Eb + Mb*Ea + Ea*Eb. Every operation adds outward
rounding headroom of two ULPs at its bounded magnitude, covering nearest rounding
and the original interval's next-representable expansion. Intermediate bound
arithmetic also rounds outward. Reciprocal error uses E/[L*(L-E)] plus rounding,
where L is the positive global geometric lower bound. Invalid, nonfinite or
nonpositive proofs fall back to the original computation.

The final padded x/y enclosure must fit the complete committed tile rectangle.
Closed high-edge tile semantics are preserved. Alternatively, its strict graded
rectangle and padded radial lower bound must prove the ORIGINAL AboveGrade
predicate. This shortcut changes only terrain preparation; slab collision and
contact solving retain their owners. Any inconclusive proof runs the original
full per-child preparation before physical advancement.

Permanent `--terrain-reuse` tests exercise actual short/long compounds, arbitrary
orientations, current child/COM offsets, distant coordinates, subnormal and
binade-adjacent inputs, signed tile boundaries, one-ULP boundary crossings,
graded-slab and grade-edge cases, invalid-domain refusal, nonvacuous retained
coverage, and source immutability. The union must contain original interval
endpoints with no tolerance. Debug and Release each report 12,322 checks.

The fixed 537-row replay uses the previous accepted command/debt schedule.
Every before/after physical observation and every admitted/retired/debt value
matches exactly. Both generations use 3,631 native slices, one world creation,
two disposals, four tiles and one terrain mesh. Among 216 contact rows without
world creation, terrain preparation median/P95/P99/max changed from
12.8276/15.2812/15.4985/15.8945 ms to 0.4429/0.4779/0.7445/0.7499 ms.
Service changed from 14.0304/17.3699/23.2303/34.1168 ms to
1.3264/2.1388/9.1804/17.8079 ms. The full replay retains one 291.1032 ms
world-import transition. These are CPU replay measurements, not native cadence
qualification or permission to discard remaining tails.

Reproduce with `--terrain-reuse` and the existing `--surface-performance` route;
the campaign driver includes both. Input identity and row comparison are in
`build/performance-150fps/offline-comparison.json`. Native admission is separate.
