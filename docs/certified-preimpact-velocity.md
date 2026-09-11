# Certified pre-impact velocity tuples

**Banked M14.12 — Qualify pre-impact velocity tuples at provider-owned contact roots.**
Commit/tag target: `e5fde08bcae834d8abc107cd26e7601fbfa3e1e0`,
`m14.12-certified-preimpact-velocity`.
M14 remains open. [Evidence and reproduction](engineering-evidence/certified-preimpact-velocity/README.md).

## Responsibility

Simulation qualifies the COM inertial-root velocity and full feature/material-relative
velocity at the SAME provider-owned alpha as the supplied checked
[root kinematics](certified-root-contact-kinematics.md). This fills the missing inputs
for later final paired-velocity realization. It selects no executable impulse or
post-impact state and grants no mutation rights.

`Proof.QualifyPreImpactVelocity(kinematics, request, currentUse)` returns `Qualified`,
`Unresolved`, `Unsupported` or `Stale`. Only successful issuance carries a privately
constructed `Proof.PreImpactVelocity`. Its `Read` checks the exact root, supplied
kinematics qualification, width request and current source applicability again.
Constructible values remain data, never proof authority.

## Physical and numerical meaning

For the frozen authoritative constant-force segment and existing Earth motion:

```text
V_COM(alpha) = V_epoch + (F/m) (alpha - epoch)
W(alpha) = R_Earth(alpha) q'_body(alpha)
omega_body_preimpact = 0
V_surface(alpha) = V_COM(alpha) - W(alpha)
```

`q'_body` is the full derivative of the feature's Earth-relative body-fixed position.
It includes Earth translation and every orientation rate. Rotating that derivative
back to root axes gives feature velocity minus material-point velocity, not merely
craft velocity minus Earth-center velocity. At alpha the material point and feature
coincide. No second Earth angular-velocity correction is applied.

The exact zero spacecraft spin follows from the existing provider's zero-spin,
zero-torque admission. Fixed attitude/lever, mass/inertia and physical provenance
remain in the checked source witness. Surface velocity is a linked derived relation;
no redundant surface-velocity authority is stored.

The witness's qualified root enclosure is consumed even when it is narrower than
the original proof. Alpha is never sampled or converted to a rational event epoch.
The existing Taylor midpoint is an enclosure center with a remainder, not event time.
All vector bounds refer to the same alpha; Cartesian endpoint combinations are not
independently selectable physical states.

The request supplies two finite positive maximum FULL component widths in m/s.
They are qualification requirements, not contact tolerance, bias or permitted
penetration. Nonfinite arithmetic or unmet widths returns Unresolved with no usable
tuple. Invalid requests/witnesses return Unsupported; changed applicability returns
Stale. Arithmetic/model floors are not hidden by retries or nominal values.

## Ownership and bounded work

Qualification performs one checked source read, one existing bounded motion evaluation,
one inverse direction rotation and the COM affine velocity expression. Consumption
performs one checked read. There are no refinement calls, heap workspaces, mutable
root versions or hidden loops beyond the banked fixed arithmetic bounds.

Applicability retains engine/root, complete geometry/feature, state/timeline/clock,
translation/force, rotation/torque, mass/inertia, attitude, Earth/frame/model and
terrain provenance. Equal replayed values do not transfer capabilities. Same-owner
immutable refinement does not globally revoke earlier mathematical evidence; using
a different root/qualification/request tuple requires explicit reissuance.

The matching [M14.11 response proposal](certified-response-proposals.md) can be read
alongside this tuple using the same root and source witness. Numerical equality
alone does not establish that relationship.

## Future consumer and preserved boundaries

Absolute COM velocity is needed to qualify state-addition rounding. Full relative
velocity is needed to bound work-adjusted energy and the actual paired momentum
change. A rounded impulse alone cannot guarantee the final normal residual: the
`1/3` example changes again during binary64 velocity addition, and a small increment
can disappear beside a large stored velocity.

This tuple does not resolve binary64 post-state selection. That later responsibility
must include final addition errors, tangential/coupling defects and prescribed
surface work. It must retain off-center spin rather than erase it for reacquisition.

M14.4 canonical atomic transactions, M14.5 admission, public integral time,
physical-event ordering, M14.9 discovery and M14.11 exact-response equations remain
unchanged. Private execution, linked remaining duration, rotating reacquisition,
support/rest and operational-date extension remain deferred. Consumers depend on
checked provider semantics; the present seed-near implementation is not a new
response-policy or public-time contract.
