# Pure authored point-feature contact generation

**Unbanked candidate** on `codex/contact-generation`, based on banked M14.2
`caa6d93ccd7c4e5924381e077a62585f99e5a32a`. M14 remains open. No milestone
number is assigned. [Validation and provenance](engineering-evidence/contact-generation/README.md).

## Authority and scope

Simulation's `Spacecraft.Contact` produces a read-only physical observation for
each explicitly authored zero-radius spacecraft point at one exact
`SimulationInstant`. M14.2 owns COM translation/mass, the existing spacecraft
rotation authority owns orientation/angular velocity, and M14.1's
`IPhysicalSurfacePointQuery` owns the natural-terrain witness, physical normal,
height and readiness. Rendering, camera, NCSM1, tessellation and GPU residency
do not participate. Simulation references the Core query contract, not Graphics.
Application composition acquires the production CPU query adapter beside the
existing physical-height oracle; acquisition and dataset verification are outside
the observation loop.

This generates observations; it does not push or stop the spacecraft. It has no
force/torque commit, impulse, pose clamp, friction, restitution, grounded/sleeping
flag, solver, warm-start cache or pad collider. Canonical facility grading already
present in H remains terrain; finite authored pad tops/sides/edges are not queried.

## Authored physical identity

`SpacecraftContactGeometry.TryCreate` admits a nonempty definition with nonzero
spacecraft ID, definition ID and version. Each feature has a nonzero unique ID,
finite FP64 COM-relative offset in spacecraft body axes/metres, and explicit role:
landing tip, support hardpoint or lower-body physical contact point.

Admission copies the caller's data, sorts by feature ID, canonicalizes signed zero
and computes SHA-256 over fixed-width little-endian ID/offset/role records. The
immutable identity is definition ID + version + content digest; observation feature
identity additionally contains spacecraft ID and feature ID. Reusing a version for
changed geometry cannot make it compare equal. This is deterministic content
identity, not authentication of an untrusted author.

Admission allocates; evaluation exposes no mutable geometry array. Authored
geometry can persist for the craft definition's lifetime. An edited definition is
a new immutable object. Feature identity is separate from observation time,
revision and physical-authority identity.

The permanent lander fixture authors fore/aft/lateral lower support hardpoints
in +X-forward, +Y-right, +Z-down axes. These locations retain physical meaning for
future support instrumentation and response even when finite collision shapes
arrive. They are not an approximation that claims to cover a hull, landing leg or
swept volume.

## Composition and time admission

Call in the existing single-writer simulation phase after servicing canonical
events. Supply a current `SimulationStateView` and the independently obtained
current authoritative `StateRevision`. Views contain live-backed spacecraft
storage; comparing the copied view revision with the current revision prevents a
retained old view from labeling new motion with old provenance. This is admission
validation, not concurrent access synchronization. Do not mutate authority while
generation runs or pass a stale revision as though it were current.

`ContactBodyMotion.TryEvaluate` freshly evaluates the current immutable
`CelestialSystemDefinition` at the observation instant, in caller-owned,
nonoverlapping evaluation/staging buffers. It composes center velocity through
the full parent chain with existing reference-frame equations, then uses the
existing canonical body orientation/angular velocity. Reuse this immutable sample
for the same system/body/root/instant; never relabel old transforms with a new time.
The generator requires the expected current system object, exact body time,
support-body/terrain identity and spacecraft root to match.

Application composition must bind the celestial dataset's inertial axes to the
existing unique ECL root. A matching root ID does not prove arbitrary foreign
datasets use those axes. No second frame graph or automatic frame reinterpretation
is introduced. `SpacecraftMotionEvaluator` evaluates complete translation and
rotation for the requested spacecraft/time/revision once per feature set.

The body angular velocity remains the existing orientation authority's numerical
derivative, using quaternion samples at t−0.5 s and t+0.5 s. It is not advertised
as an analytically exact derivative. Generator error bars compare against that
authority; they do not certify its physical model. An angular-velocity uncertainty
δω contributes at most |surface lever arm|·|δω| to witness speed uncertainty.

## Geometry and kinematics

All vectors below are FP64. Root values are inertial-root metres/metres per
second; body offsets are metres. Angular velocities used in cross products are
expressed in the same frame as their lever arms.

```text
r_feature_root = Q_craft * featureOffsetFromCom
q_root         = craftComRoot + r_feature_root
v_feature      = craftVelocityRoot + omega_craft_root × r_feature_root

q_body         = inverse(Q_body) * (q_root − bodyCenterRoot)
rho            = |q_body|
d              = q_body / rho

surface        = canonicalQuery.Query(bodyId, d)
s_body         = surface.BodyFixedPositionMetres
radialSignedGap = rho − referenceRadius − surface.HeightMetres

r_surface_root = Q_body * s_body
s_root         = bodyCenterRoot + r_surface_root
normal_root    = Q_body * surface.PhysicalNormal
v_surface      = bodyVelocityRoot + omega_body_root × r_surface_root
v_relative     = v_feature − v_surface
```

Positive radial gap means outside the radial terrain solid; negative means inside;
zero means the point meets the radial witness. **It is not closest-point distance,
Euclidean penetration depth or finite-shape separation.** In particular, do not
subtract a sphere/capsule radius from it. No radial-Up, previous-normal or render
normal fallback is permitted when the canonical physical normal is unqualified.

## Publication and failure

Each immutable observation carries feature/definition/craft identity, full terrain
authority (including support body and facility-support composition), current state
revision, exact time, root ID, both root positions, body-fixed witness direction,
qualified root normal, radial gap and feature/surface/relative velocities.

The caller owns the bounded output span. Successful publication is exactly the
ordered `[0, Written)` set. Capacity is checked before any physical query. Any
feature failure clears the entire attempted output prefix, returns `Written=0`
and identifies the failing feature and physical query status where applicable.
Do not read unused capacity as observations. Old copied immutable observations
remain historical samples; they do not gain authority for a new time/revision.

Explicit failures cover invalid geometry, insufficient capacity, stale state,
unsupported body, body/root/time/system mismatch, failed body evaluation,
unavailable coherent spacecraft motion, nonfinite geometry, missing/stale physical
authority and all canonical query failures. A late unqualified normal or absent
data cannot silently omit a feature or become a clear/no-contact result.

## Performance and future boundary

Resolve immutable geometry/query acquisition outside the loop. Evaluate body
motion once per body/instant and craft motion once per feature set. Generate writes
caller-owned storage and retains no response state. Real-data qualification also
charges a fresh body evaluation to every batch, conservatively measuring the full
observation path. Exact-zero warmed allocation is measured separately from timings
using the banked test-only GC accounting boundary; production GC/tiering is unchanged.

One four-feature craft at 120 observations/s is the qualification workload, not a
new production scheduling policy. Measured costs and early-process tails are in
the evidence package. No arbitrary multi-craft capacity or response-cost claim is
made. Future response must own coupled force/torque transactions, solver policy,
persistence/invalidation, grounding, pad collision and contact under time warp.
Endpoint observations do not prove that no impact occurred between two states:
event bracketing, sweep geometry or a qualified step bound remains necessary.
