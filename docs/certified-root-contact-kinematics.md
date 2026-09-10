# Qualified contact kinematics at a certified root

**UNBANKED — Evaluate qualified contact kinematics at provider-owned certified
roots.** Baseline: banked M14.9 at
`9698a08b84b9b40b0fa03bec8f8ba80d1d7404e7`. M14 remains open; no new milestone
number is assigned. [Validation and reproduction](engineering-evidence/certified-root-contact-kinematics/README.md).

## Responsibility and authority

Given a currently applicable `FloridaContactProvider.Proof` for a unique
approaching root alpha, `QualifyKinematics` produces an immutable provider-issued
`Proof.Kinematics`. Its private construction retains the original proof and
qualified physical-value enclosures. It performs no response admission,
scheduling, state replacement, publication or event-local execution.

`Read(expectedRoot, currentUse, out values)` is the consumption boundary. It
requires the issuing provider/root lineage and rechecks the engine, independent
state and timeline revisions, current clock, frozen translation/force and
rotation/torque, mass/inertia, complete geometry/feature, Sol/model, frame graph,
acquired terrain owner and physical authority. The original provider's pending
event boundary checks remain authoritative. Use is within its existing
single-writer read phase, not concurrently with mutation.

Refinement of the same owner's root is accepted. A different provider is
rejected even when it proves the same physical time. Default, clear and non-root
proofs cannot issue a witness. Failed qualification returns no usable witness.
Returned value records are historical read-only snapshots; constructing one
does not fabricate the private witness. Future consumers must accept and check
the witness, not treat a saved value record as current permission.

Replay reconstructs the provider and evidence against current authority. Identical
inputs reproduce identical physical bounds; replay never transfers an old
witness to a new engine/provider. No certificate serialization is introduced.

## Supported domain

The [M14.9 domain](florida-monotone-contact-certification.md) is unchanged:
current supported Sol/Earth/time-mapping/root-frame identities, acquired canonical
Florida full-weight grading, one complete authored zero-radius point, arbitrary
fixed COM offset, positive mass/valid inertia, constant-root-force translation,
and exactly zero spacecraft angular velocity and body torque. The whole canonical
search interval must occupy one second cell inside seed-relative ±3,600 seconds.

This is not operational-date coverage. Normal Solar startup samples current
host UTC; the 2024 date is an injected regression fixture. No route or startup
time changes. There is no new contact-capable spacecraft in a visible route.

## Same-root physical quantities

Let R(t) map Earth body axes to inertial root axes, p_f(t) be the authored feature
position, and p_E(t) the Earth center. The banked relation uses

```text
q(t) = R(t)^T [p_f(t) - p_E(t)]
f(t) = Up dot q(t) - (referenceRadius + gradingAltitude)
f(alpha) = 0
n_body = Up / |Up|
n_root(alpha) = R(alpha) n_body
u_normal(alpha) = f'(alpha) / |Up|
```

The derivative identity is

```text
R q' = v_feature - v_E - omega_E cross (p_feature - p_E).
```

At the certified root the feature coincides with the rotating grading material
point, so its right-hand side is the feature velocity relative to that material
point. Taking the dot product with n_root proves the stated normal velocity.
This identity does not substitute an off-contact radial query witness for the
material point. No M14.8 finite-stencil normal or velocity is a proof premise.

The complete source-owned orientation is
`Rz(ra) Rx(tilt) Rz(w) Rx(pi/2)`, where `ra = RA + pi/2` and
`tilt = pi/2 - Dec`. Both derivative and forward direction retain the RA,
declination and axial-spin terms. `RootDirection` is the exact inverse sequence
of the existing `BodyFixed` transform, evaluated with outward bounds over the
root enclosure. Midpoints used internally as Taylor/bisection centers never
become alpha or a published physical state.

The fixed lever is the exact-normalized stored spacecraft attitude applied to
the authored body offset. It is not obtained by subtracting large inertial
positions. The witness retains the stored attitude bits, authored offset,
qualified root lever, mass, principal inertia, physical authority, original root
and qualified root enclosure. The stored quaternion denotes its exact
normalization; no second rounded quaternion authority is created. A body-fixed
location package is unnecessary for this normal/material-velocity responsibility.

All fields enclose functions of the **same alpha**. Their components cannot be
chosen independently to invent an FP64 state. `Qualified` means the requested
widths were met. It is neither a claim that interval endpoints are exact physical
values nor a new impulse/sign-admission policy; the original approaching-root
certificate retains its own theorem.

## Qualification and work bounds

`FloridaKinematicsRequest` supplies maximum **full interval widths** for root
seconds, each root-normal component, normal velocity in m/s, and each root-lever
component in metres, plus a refinement budget from 0 through 24. Every width
must be finite and positive. Width comparison itself is outward rounded.
These are numerical witness requests, not contact tolerances.

The provider first validates root/current authority and the request. It evaluates
the fields over the current certified root enclosure. If widths are unmet it
uses the existing source/domain-proved sign bisection, preserving the original
root identity. At most 25 value evaluations and 24 sign/domain evaluations occur
per request. There is no heap workspace on reusable operations. An already
narrowed proof can meet the same request with zero refinement work.

Outcomes are explicit:

| Outcome | Meaning |
|---|---|
| Qualified | All requested widths met; checked witness supplied. |
| Unresolved | Budget exhausted, uncertain midpoint sign, numerical bounds unavailable, or irreducible enclosure floor; no witness. |
| Unsupported | Invalid/default/non-root proof or request; consumption also refuses mixed root/owner. |
| Stale | Current authority no longer matches the issuing context. |

A fixed-lever width below its arithmetic enclosure is refused immediately;
time refinement cannot improve it. The late-coverage Earth remainder can prevent
useful tightening even when a coarse root existence proof remains valid. No
threshold enlargement, nearest-rational selection, zero-gap fabrication or
unbounded retry is used.

## Consumer boundary

M14.5's supplied-canonical represented-zero admission is unchanged. There is no
epsilon, snapping or provider exception. This witness cannot emit M14.4's
`SpacecraftContactImpulseIntent` at alpha: that intent and transaction path use
canonical `SimulationInstant`. Future response policy owns effective inverse
mass, impulse qualification and any FP64 response representation. Application,
private execution, operational-date coverage, rotating features, support/rest,
grounding, landing and departure remain separate responsibilities.

The installed KSA reference supports prepared-state ownership separated from
application. NovaCore adapts that lifecycle to a checked exact-root witness;
Bepu float states, speculative margins, approximate impact endpoints and sleep
flags are not imported as exact evidence. The matching reference identities and
narrow official-history provenance are retained with the validation report.
