# Exact-event Earth-relative observation

**UNBANKED CANDIDATE.** Latest banked accomplishment remains M14.7. M14 — Surface
Interaction / Launch Foundation remains open. No next milestone number is assigned.

`SpacecraftPhysicalEventObservationEvaluator.Evaluate` is a read-only Simulation
query for one complete immutable spacecraft geometry containing exactly one
zero-radius authored feature. It combines M14.7 spacecraft motion, the current
Earth celestial model, and an acquired M14.1 physical surface query at the same
`PhysicalEventEpoch`. Caller-owned work buffers avoid allocation. Call in the
single-writer read phase with a fresh state view and independently current revision.

Only `SolAnalyticalDefinition.Instance` is supported: analytical Earth beneath
fixed identity Sun, current identity time mapping and Earth corrections, and the
existing IAU_EARTH model, including pole evolution. Other definitions, including
look-alike copies, are explicitly refused. No Moon, sampled ephemeris or arbitrary
hierarchy support is implied. Terrain remains the acquired canonical physical
CPU query; render LOD and camera state do not participate.

Canonical epochs delegate to banked M14.3 body/contact evaluation. Fractional
epochs use exact whole-second anchoring and an exact local difference before FP64
conversion. Both Earth position and velocity advance under the existing two-body
model. Earth orientation includes right ascension, declination and prime-meridian
evolution. Its angular velocity retains the numerical symmetric one-second
rotation-stencil meaning, not a certified derivative.

The immutable result records spacecraft/geometry/feature identity, exact epoch,
source revision, system/body/root identity, terrain provenance, positions, qualified
normal, radial gap, and feature/terrain/relative velocities. It contains no live
state view. Failures return a wholly default observation with status/detail.
`ContactGenerationStatus` preserves canonical status semantics on the shared
supported input domain; the separate failure detail identifies new scope refusals.
Strict system identity refusal precedes defensive mapping/model checks. Those
defensive branches are not claims of generalized mapping support.

Spacecraft feature velocity is COM root velocity plus root-space angular velocity
cross root-space feature offset. Terrain witness velocity is Earth origin root
velocity plus Earth root-space angular velocity cross the rotated physical witness.
Relative velocity is feature minus terrain velocity. Positions use metres,
velocities metres/second, angular velocities radians/second. The feature direction
is transformed into Earth body-fixed axes before the physical query.

`RadialSignedGapMetres = (featureRadius - referenceRadius) - physicalHeight`, as
in M14.3. It is not closest-point distance or finite-volume penetration. No
tolerance or snapping is applied. Only the terrain owner's ready result supplies
height, witness and qualified normal.

**Exact PhysicalEventEpoch != certified contact. Observation != root proof.**
Exact time identity accompanies evaluated FP64 state, not exact mathematical
physical state. There is no contact flag, discovery, admissibility, root identity,
response, scheduling, mutation or canonical publication. Future discovery and
certification consume this numerical prerequisite and own their own proof duties.

[Equations, numerical qualification, validation, performance and reproduction](engineering-evidence/exact-event-earth-relative-observation/README.md)
describe the bounded candidate evidence. Manual visual acceptance is not required:
this adds no visible production route.
