# Isolated analytical contact response

**Banked M14.5**, commit `b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, tag
`m14.5-isolated-analytical-contact-response`. M14 remains open.
[Validation/provenance](engineering-evidence/isolated-contact-response/README.md).

## Responsibility and supported geometry

Simulation's `Spacecraft.Contact.IsolatedContactObservation` is an ephemeral,
generator-issued receipt and pure response producer. `TryObserve` uses the M14.3
generator; `Evaluate` requalifies its result and returns either an explicit refusal
or the existing M14.4 `SpacecraftContactImpulseIntent`. It never schedules or mutates.

The COMPLETE immutable authored geometry must contain exactly one zero-radius
feature. Selecting one observation from a multi-feature definition is forbidden.
This is a natural-terrain-only composition: it cannot certify isolation from other
bodies, pad colliders, external contact systems or unmodeled hull geometry. Such
compositions must not invoke this specialization as a competing-contact solver.

**Geometric admission is deliberately strict: represented canonical radial gap
must equal zero.** Positive values return `NotContacting`; negative values return
`RecoveryRequired`. Even tiny nonzero gaps are refused. No production-wide terrain
slope/error bound exists in M14.3; its analytical fixture tolerances cannot justify
a nonzero contact skin. Exact represented zero is not proof of exact-real
coincidence and is not promised to occur routinely on arbitrary Earth trajectories.
This limitation is intentional, not a claim of a general usable touchdown detector.
Radial gap never becomes normal penetration depth or an input to recovery impulse.

## Authority, identity and lifetime

M14.1 supplies canonical terrain/normal readiness; M14.2 and existing rotation
supply one coherent physical motion; M14.3 supplies the observation. The receipt
privately binds successful generation to the query instance/authority, geometry,
current celestial system/body sample, exact time, root, revision and complete
stored translation/rotation states. Caller scratch can be overwritten safely.
Default receipts are invalid. A copied `IsReady` observation cannot create a receipt.

Composition supplies independently CURRENT revision, time, geometry, system,
query and authority in the single-writer phase. Retained old inputs must not be
presented as current. Requalification checks the receipt's provenance and exact
stored states, including mass; this also rejects old views backed by changed data.
Changing any required authority invalidates admission. A replacement query object
is conservatively rejected even if its digest is identical.

The production CPU query is immutable after acquisition: data publishes once and
never mutates/unloads. Successful query provenance plus current instance/identity
therefore suffices without a new terrain query or readiness poll. No cached mutable
residency or render generation participates. `GenerateWithMotion` returns the same
motion already evaluated by generation; policy does not repeat body/craft evaluation.

Consume the result through the canonical M14.4 scheduling/transaction path in the
same single-writer phase. Do not retain a policy result as a prediction across
authority changes. M14.4 independently re-evaluates mutation authority; admission
does not replace its defenses. A first commit invalidates same-revision duplicate
intents; rejected payloads follow existing retry/cancel ownership. This producer
has no deduplication cache, persistent support, or permission to relabel revisions.

## Admission and numerical interpretation

Explicit results cover unavailable observations, unsupported geometry, stale
state, identity/time/authority mismatch, invalid physical state/normal, positive
gap, recovery-required gap, separating/indeterminate motion and nonfinite response.
Every unsuccessful result contains a default intent. No zero-impulse event is made.

Ready query provenance establishes physical-normal qualification. An additional
finite, outward, unit check uses Core's existing squared-unit-length ceiling
`SurfaceAnchor.DirectionUnitLengthSquaredTolerance` (1e-12). Unit length alone
would not establish a physical normal. The policy does not substitute radial Up
or normalize an unqualified vector into eligibility.

Velocity-sign uncertainty is against CANONICAL M14.3 velocities and normal, not
unknown true-gradient error or a certified celestial angular-velocity derivative.
Let unit roundoff be u=2^-53, gamma5=5u/(1-5u), and component i have:

```text
e_i = u/(1-u) * (abs(v_feature_i) + abs(v_surface_i))
b   = sum(abs(n_i)*e_i) + gamma5*sum(abs(n_i*v_relative_i))
```

Each positive bound operation rounds upward; explicit minimum-subnormal terms
cover subtraction/product/addition underflow. Overflow refuses response. This
bounds component subtraction plus three dot products/two additions; a dot-only
bound would miss cancellation between large planetary transport velocities.
Normal speed less than -b approaches, greater than b separates, and within [-b,b]
is indeterminate/no response. No fixed velocity epsilon, retry or tolerance subtraction.

## Physics and transaction

All quantities share the exact instant and FP64 inertial root. Q maps body to root;
r is Q times the authored COM-relative feature offset (never large-position
subtraction); I is positive principal body inertia and m positive physical mass.

```text
v_rel = (v_COM + omega_craft_root x r) - v_terrain_witness
u_n   = dot(n, v_rel)
a     = inverse(Q) * (r x n)
k     = 1/m + a.x^2/I.x + a.y^2/I.y + a.z^2/I.z
j     = -u_n/k
J     = j*n
```

Finite positive k/j and finite nonzero J are required without clamping. Restitution
is zero and friction absent. Canonical M14.4 applies delta_v=J/m and
delta_omega_body=inverse(I)*inverse(Q)*(r x J), keeping event pose unchanged.
It alone publishes paired state, revision and history. Continuous force/torque
state is not replaced. Prescribed rotating/moving support can transfer inertial
energy to the craft; global craft-energy decrease is not a universal invariant.

The unit-normal formula admits the existing tiny normal-length error. Numerical
post-normal comparisons include its residual bound
`abs(dot(n,n)-1)*abs(u_n)/(m*k)` separately from arithmetic rounding. Independent
matrix-compliance and momentum tests retain an oracle distinct from policy code.
The older M14.4 oracle remains unchanged.

## Missing responsibilities and manual acceptance

A clean endpoint observation does not prove no impact was crossed beforehand.
Autonomous touchdown still requires bracketing, sweep/CCD or qualified conservative
advancement. This policy accepts a supplied instant and implements none of them.
No positional correction, penetration recovery, friction, configurable restitution,
multi-contact/manifold solver, warm start, sleeping, grounded state, finite shape,
pad collision, launch or landing is present. No existing visible route invokes it;
manual visual acceptance was not required for this banked simulation-only responsibility.

The closed-form specialization remains a reference for future solver qualification.
It must not grow into an arbitrary sequential multi-contact solver. Solver state
remains subordinate to NovaCore authoritative publication if adopted later.
