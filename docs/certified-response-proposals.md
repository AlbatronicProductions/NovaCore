# Certified isolated-contact response proposals

**UNBANKED — Qualify certified isolated-contact response proposals at provider-owned roots.**
Baseline: M14.10, `fce80a520240293a0373857986c607caf187eacd`.
Development branch: `codex/certified-response-proposal`. M14 remains open.
No milestone number is assigned. [Evidence and reproduction](engineering-evidence/certified-response-proposal/README.md).

## Responsibility

Simulation consumes a privately issued [M14.10 kinematics witness](certified-root-contact-kinematics.md)
and qualifies an immutable enclosure of the exact isolated response at its SAME
provider-owned physical root alpha. Alpha need not be a rational event epoch.
The new provider does not discover contact, select an executable FP64 impulse,
schedule an event, construct replacement state, or mutate any authority.

`Proof.QualifyResponse(kinematics, request, currentUse)` returns `Qualified`,
`Unresolved`, `Unsupported` or `Stale`. Only successful qualification carries a
privately constructed `Proof.ResponseProposal`. Every `Read` checks its bound
root/qualification/request tuple and current source applicability again.
Constructible `CertifiedResponseValues` are data, never proof authority.

## Supported physical domain

The specialization is one point in the COMPLETE authored geometry, prescribed
Earth natural-terrain authority, frictionless contact and restitution zero.
There is no penetration recovery, support/rest or competing collision-system
composition. One root or one selected point is not a universal isolation proof.
The existing provider's zero-spin/zero-torque and seed-near coverage constraints
remain unchanged. The response code consumes qualified semantics, not the seed
polynomial or source coverage internals.

## Physical and numerical law

With authored body lever `r_B`, exact normalized stored attitude `Q_c`, root
normal `n_R`, relative normal speed `u_n`, mass `m` and principal inertia `I`:

```text
a = r_B × (Q_c^T n_R)
k = 1/m + a_x²/I_x + a_y²/I_y + a_z²/I_z
j* = -u_n/k
J_R* = j* n_R
ΔL_B* = j* a
```

The inverse rotation uses the stored quaternion's exact norm squared; it does
not select another rounded attitude. The authored lever remains body-local,
avoiding subtraction of large inertial positions. Nonnegative interval squaring
preserves the physical `k >= 1/m > 0` law.

Admission requires checked current source, finite positive mass/inertia,
`U_H < 0`, and a finite computed `K_L > 0`. Outward interval operations enclose
`k`, `j*`, root-frame linear impulse and body-frame COM angular impulse. The
request gives maximum FULL widths for these four quantities (vector widths
apply to every component). They are numerical qualification requirements, not
contact tolerances, penetration permission or impulse bias.

Each quantity belongs to the same alpha and exact response law. Independent
interval combinations are not selectable physical states. Dependency loss may
widen the enclosure; it never authorizes picking midpoints or endpoints.
Uncertain approach, denominator/arithmetic floors, nonfinite arithmetic or unmet
widths refuse qualification. Uncertainty is not reclassified as `NoResponse`.
The arithmetic routine cannot issue a receipt from arbitrary input data.

One response evaluation consumes an already qualified witness. It performs no
refinement. Callers may explicitly requalify through M14.10 and reissue; there is
no hidden retry, recursive search or reset refinement budget.

## Exact guarantees and application boundary

The linked exact law satisfies `u_n + k j* = 0`, has no tangential impulse and
preserves paired linear/angular impulse meaning. With prescribed material-point
surface velocity `v_surface`:

```text
ΔT_craft - J_R* · v_surface = -u_n²/(2k) < 0
```

This is work-adjusted dissipation. Earth can perform positive inertial-frame
work, so spacecraft kinetic energy need not decrease. Earth receives no dynamic
reaction update; the specialization does not claim full spacecraft-plus-Earth
simulated momentum conservation.

These guarantees describe the EXACT linked response, not every Cartesian
combination of bounds or a rounded authoritative state update. For `k=3` and
`j*=1/3`, nearest binary64 leaves an exact negative normal residual `-2^-54`;
the next larger value leaves `+2^-53`. Generic rounded normal components can
also introduce tangential error. A zero-containing residual enclosure does not
prove applied nonpenetration. Numerical realization/application therefore
remains a separate responsibility; no command, velocity clamp or slop is added.

## Identity, lifetime and replay

The proposal retains the supplied root, actual M14.10 witness, response request
and immutable values. Policy/numerical versions are both 1. The source owns
engine/root identity, state/timeline revisions, complete geometry/feature,
translation/force and rotation/torque segments, mass/inertia, normalized attitude,
Earth/frame/model and terrain/facility provenance. Checked use preserves all of
those applicability checks, including the public-clock/search-start guard.

Exact input comparison avoids boxed `ValueType.Equals`. M14.10 does not retain
its original requested widths as a separate identity: the retained qualified
fields are its qualification tuple. The NEW response width request is bound
explicitly. Replacing a root tuple, qualified values or response request requires
reissuance. Identical copies remain usable while current. Computing an immutable
same-owner refinement does not globally revoke earlier mathematical evidence.

Default, fabricated data snapshots, mixed-owner and stale use return no usable
proposal. Private construction is an API authority boundary, not protection
against arbitrary reflection/unsafe process-memory modification. Permanent tests
fault-inject mass/inertia only into disposable fixtures to exercise the source's
frozen-property guards, restoring them in `finally`; no mutation API was added.

Replay reconstructs authority, revalidates and recomputes deterministically.
Equal outputs do not transfer capabilities across engine/provider instances.
No serialization or nonce registry is introduced.

## Banked boundaries and future consumer

M14.5's represented-zero canonical admission and M14.4's canonical paired
transaction remain unchanged. The proposal is not `SpacecraftContactImpulseIntent`;
it cannot be scheduled at non-rational alpha through that canonical path.

A later numerical realization/private execution owner can consume its root,
source and exact-law enclosures. That owner must supply qualified pre-impact
state, define rounding/application guarantees, respect source preflight and
perform atomic publication. Nothing here advances the public clock to alpha.

Nonzero body angular impulse is preserved. Applying an off-center response can
leave M14.9's zero-spin discovery domain; rotating reacquisition, remaining-time
execution, support/rest and force-driven release are still deferred.

## Validation and bounded cost

Independent rational fixtures cover central/off-center contact, asymmetric
inertia, exact normalization, rational/irrational analytical roots, correlated
inputs, conditioning/refusals, moving-surface energy and realization limitations.
Actual Florida integration uses a separate ODE/decimal root bracket and exact
rational interval response oracle over that ENTIRE bracket.

The qualification path performs one checked source read and one fixed arithmetic
pass; consumption performs one checked read. There are no refinement calls or
heap workspaces. The receipt is 640 bytes in the tested x64 build, values 128 bytes,
request 32 bytes. Timing runs under normal runtime behavior, separately from the
banked checked allocation measurement. See retained results for actual distributions,
allocation, source identity and final validation. No whole-frame claim is made.
