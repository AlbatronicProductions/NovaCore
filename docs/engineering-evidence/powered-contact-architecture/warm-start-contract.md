# Selected warm-start policy and explicit qualification limit

**Selected policy: A — PRESERVE UNCHANGED for a mass-only dynamic-to-dynamic update, as the iterative initial guess.**

This selects the meaning of cached impulse. It is not a claim that finite-iteration powered-contact accuracy/stability has already passed.

## Source and mathematical justification

Pinned [PenetrationLimit.cs](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/PenetrationLimit.cs#L15), :15-25,46-74,113-130:

v = v_free + M_new^-1 J^T lambda
K_new = J M_new^-1 J^T + softness.

WarmStart applies the retained lambda through CURRENT inertia; Solve calculates current effective mass and projected corrective accumulation. Old lambda is a starting iterate, not momentum that must be conserved from last step. The previous physical endpoint already contains the previous step's physical result. Reusing lambda as a new-step guess does not authorize an additional canonical impulse or resource expenditure.

Contact normal lambda>=0 remains an admissible guess independent of positive inverse mass. Friction limits and active-set changes are resolved by the current constraint solve; prior friction/support can be a poor guess even though using it is mathematically legitimate. Eight finite iterations need independent residual/support/energy quality witnesses. No convergence guarantee follows from source alone.

## Why not an arbitrary mass rescale/reset?

Central fuel changes translational inverse mass, not dry rotational inertia. K ratios differ by moment arm, normal, friction and active set. There is no universal scalar m1/m0 or m0/m1 correction for all cached rows.

Zero lambda is also admissible but discards useful banked warm starts; the source supplies no proof that resetting every fuel change is more accurate/stable. A percent-change threshold is unqualified. Thus keep geometric feature matching and the current warm-start interpretation as the starting policy, with a hard finite-iteration qualification gate.

## Duration changes are a separate responsibility

For roughly constant physical contact force, lambda≈force*h derives the guess scaling h_new/h_old. [Solver.cs:1484-1527](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver.cs#L1484) supplies scaling APIs. This is not exact when ignition/exhaustion or contact geometry changes the force.

The ordinary banked 16,666/16,667-tick contact path already retains its caches. New exact resource subsegments can have radically different durations; do not infer their cache policy from that small tick alternation. No unconditional duration-ratio scaling or tiny-dt reset is authorized here. The event-sensitive numerical bridge must qualify whether preserving, scaling or selectively zeroing the affected impulse slots meets its declared error/stability contract.

## If future evidence requires invalidation

The desired scope would be only the affected retained body's accumulated impulse fields at a safe prepared solve boundary. The inspected Solver.cs APIs scale constraint sets; they do NOT establish a supported per-body/per-constraint clearing API. Exact targeted access and its lifecycle remain to be qualified before selective invalidation could be selected. Keep shapes, handles, manifold geometry, feature IDs and collision detection intact. Bound enumeration/scratch at cold preparation; avoid global solver reset.

This is a contingency description, not selected production code or a new reset heuristic. World/manifold destruction is not required by mass change.

## Unresolved gate

**Finite-iteration stability across the derived mass cadence and exact ignition/exhaustion transitions is UNQUALIFIED.** The selected A semantics must survive centered residual support, off-COM unloading, normal/friction impulse and energy/residual checks at unchanged solver settings. If it fails, return the bounded policy result to Project Control; do not tune iterations or hide the failure.
