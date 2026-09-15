# Pinned BEPU friction ownership

Inspected current installed DLL identity and exact pinned upstream source on
2026-09-15. KSA and NovaCore use the same Physics/Utilities bytes listed in
[KSA identity](ksa-current-work-ownership.md). Product:
`2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`, assembly 2.5.0.0.
Dependency bytes remain unchanged; the temporary observer is described in [plan](observer-plan.md).

All line anchors below refer to raw source at that commit, not a moving branch.

| Source / anchor | Proven behavior |
|---|---|
| [ContactConvexTypes.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs#L787), 787-835 | Contact4OneBodyFunctions: warm tangent, four normals, twist; each Solve processes four normals, tangent, twist; processor batch ID 3 |
| Same, 92-119 | Four normal, two tangent, one twist accumulated impulses; no work field |
| Same, 170-196 | Friction center averages contacts with nonnegative depth, or all contacts if none qualify |
| Same, 800-823 | Tangent cap mu/4 times summed current normal impulse; twist cap mu/4 times sum of normal impulse times current contact radius |
| [TangentFrictionOneBody.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TangentFrictionOneBody.cs), 20-90 | Two tangent Jacobian rows; point-relative slip includes angular velocity; 2x2 effective mass; circular accumulated-impulse clamp; apply new-minus-old increment |
| [TwistFrictionOneBody.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TwistFrictionOneBody.cs), 19-65 | Normal-axis angular slip, inverse inertia metric, scalar accumulated cap and incremental application |
| [PenetrationLimitOneBody.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/PenetrationLimitOneBody.cs), 10-105 | Normal bias/softness, shared velocity updates; normal impulses determine friction caps |
| [Helpers.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Helpers.cs), 21-35 | Tangent basis from current normal and its Z sign |
| [OneBodyTypeProcessor.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/OneBodyTypeProcessor.cs), 82-130 | Gather/integrate before warm start; gather/solve/scatter each iteration |
| [Solver_Solve.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver_Solve.cs), 1415-1478 | Warm start once, then velocity iterations within the same substep |
| [ISolverContactDataExtractor.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ISolverContactDataExtractor.cs) | Prestep/accumulated-impulse extraction; no physical work or force-time history |

Tangent Jacobian rows have linear part t and angular part r cross t. With
W=diag(inverse mass, inverse world inertia), the effective-mass matrix is
(J_t W J_t^T)^-1. The solver uses current generalized velocity, clamps a
trial accumulated tangent vector to a disk, and applies its difference
from the old vector. The 1e-16f length guard is numerical; friction has no
normal spring-softness work term. Twist uses n dot omega and n^T I^-1 n.
There is no separate rolling row in this concrete contact type.

Geometry is fixed during this single solve, not thereby certified constant
along an underlying continuous physical trajectory. Normal rows and tangent
rows couple through shared angular velocity and the inertia metric. Normal
load also changes tangent/twist caps. The eight iterations are convergence
chronology, not eight physical-time samples. Warm application and corrective
increments do not by themselves provide a physical force history.

Fresh raw-source SHA-256:

- ContactConvexTypes.cs: `B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5`
- TangentFrictionOneBody.cs: `3E102918561194C5871AF95F35F03A3FEE5FB39884CE4E60AC50F7E1CE195573`
- TwistFrictionOneBody.cs: `554C03FBD6FAA9DB6D738FE613AF64BCB44EA88F2F1259C7E8536E5C51962440`

No native work accumulator/output was found in this concrete path. This is
an observability fact, not proof that a justified external energy account
could never be constructed.
