# Narrow current KSA dynamics comparison

Current installed reference: `E:/Kitten Space Agency/KSA.dll`, version `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA256 `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`. This is the verified local installation, not a claim about latest announcements.

Read-only decompiled source root: `E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/`. All 37 prior actuator-map source hashes still match. No KSA source/assets/constants were copied into NovaCore or made a runtime dependency.

| CURRENT SOURCE anchor | Observed responsibility / cadence | Decision |
|---|---|---|
| `NozzlePerformance.cs:31-45,58-66` | Momentum/pressure thrust and effective exhaust performance | ADOPT effective-thrust convention; ADAPT bounded authored model |
| `VehicleUpdateState.cs:398-420`; `Disturbances.cs:23-39`; `PhysicsStates.cs:891-903` | Prepare body force, COM-relative moment, mass flow; accumulate force/torque/negative mass rate | ADOPT body wrench responsibility; ADAPT immutable source-bound interval input |
| `PhysicsStates.cs:993-1065` | Free-flight velocity-Verlet family; start/end force evaluation, orientation transforms, temporary mass-property scaling, withdrawal/recompute and angular momentum/COM corrections | ADAPT evolving physical state; do not copy the integrator or a different mass law |
| `VehicleProperties.cs:99-112,133-149`; `PartTree.cs:895-909` | Spatial resources contribute mass/COM/inertia | ADAPT coupling; INTENTIONALLY DIFFER for explicitly ideal point reservoir |
| `PhysicsBubble.cs:1953-2035` | Disturbances once per outer constrained interval; retained solver chunks <=1/60s; readback then resource withdrawal after outer solve | ADAPT retained dynamics ownership; this is not NovaCore exact exhaustion or continuous contact-mass proof |
| `PoseIntegratorCallbacks.cs:48-72` | Body wrench transformed with body orientation into velocity increments | ADAPT callback ingress and copied inputs; qualify NovaCore cadence separately |
| `VehicleProperties.cs:64-68`; `ConstraintSim.cs:261-278,311-327`; `PhysicsBubble.cs:2657-2668` | Push local inverse mass/inertia/kinematics and wake retained body | ADAPT explicit in-place admission |
| `ConstraintSim.cs:851-864` | BEPU step; poison private simulation on exception | ADAPT terminal private invalidation |
| `VehicleUpdateState.cs:176-203`; `Vehicle.cs:2488-2495` | Worker-prepared resources/properties; game-owned application | ADAPT prepare/apply ownership; does not prove NovaCore atomic successor semantics |

## OFFICIAL HISTORY / DESIGN INTENT

Reused the banked [official history classification](../spacecraft-actuator-architecture/ksa-history-convergence.md) and [resource comparison](../finite-propellant-architecture/ksa-resource-convergence.md). No broad controls research or fresh Discord campaign was performed.

- A3/rev2905: controller/core/nozzle/feed separation supports responsibility decomposition.
- A4/rev3391: output-based behavior supports realized-versus-requested distinctions.
- A8/rev5177: once-only lifetime/replay responsibility supports explicit source consumption.

These records explain intent; current code supplies the cadence findings above. Neither establishes KSA-equivalent NovaCore revisions, exact rational depletion, all-or-none resource/motion commit or mass-driven warm-start policy.

## Intentional NovaCore differences and evidence

- Exact admitted-bit fuel accounting and two ordered segments: banked [M14.24 arithmetic/nonmutation witnesses](../finite-propellant-segmentation/README.md), including sub-double-duration positive impulse counterexample in the accepted architecture.
- One integral endpoint, exact debt/revision/history and canonical-committed/private-invalidated result: banked persistent-contact and host-paced servicing source/tests, mapped in [current source](current-novacore-dynamics-map.md). Sharing that doctrine does not qualify powered contact.
- First point-reservoir inertia differs explicitly from KSA spatial resources. It is a narrow mathematical fixture, not a real-tank approximation claimed to be accurate.

No KSA defect is alleged from these differences. Only the inspected responsibilities are compared. A correct NovaCore free-flight/contact architecture may share preparation/publication data while using different numerical regime adapters.
