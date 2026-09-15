> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Private retained preparation draft

This is a standalone numerical prerequisite in `tests/NovaCore.ContactNumerics.IntegratedProbe`.
It is not production, is not registered in the Simulation runner, and has no canonical engine, resource,
command, actuator, clock or publication capability. Full qualification stopped at Phase4.

## Owned state and lifecycle

`OwnedState` contains exact private identity (world/body/body generation/constraint/manifold generation),
accepted cache generation and piece, piece kind, producing geometry, producing duration, seven scaled
impulses, load and accepted velocity. `ContactGeometry` owns four feature IDs/levers/depths, the actual
normal and tangent axes, and material values. Geometry is a copied value, never an alias into callback
scratch. One accepted tuple and one installing tuple are retained; no geometry history grows.

The seed geometry/cache is extracted from the final ordinary solver prestep BEFORE changing mass or
refreshing powered contacts. Every later proposed tuple carries the geometry that produced its impulses.
Geometry, duration, cache, load, endpoint, piece and generation promote together only after native cache
and physical endpoint projection are checked. The retained-world wrapper separately owns/checks the
accepted pose. It does not infer producing levers from the new pose.

Preparation checks thread/non-reentrancy/invalidation, exact identity/generation/frontier, expected velocity,
feature order, native cache projection, finite current geometry/tensor/load, positive support and duration.
The admitted material remains friction.5/recovery2/30Hz critical damping, shared-normal convex four-row
support, strictly positive bounded depths and support velocities. No compound/nonconvex solver claim.

The fixed pipeline is:

1. Generalized linear/angular impulse transport from OWNED old geometry to current geometry, with
   original rank/conditioning, hemisphere, positive support and strict friction checks.
2. Seven-component duration transformation using the old producing and new exact/binary duration.
3. Current-body common-normal load-transition shift; separate previous/current loads at the SAME current
   inverse mass/inertia. Gravity remains active, engine wrench only in the powered piece.
4. Current equations and selected D: delta=−qᵀ(A*lambda−b)/(qᵀAq), q=(1,1,1,1,0,0,0).
5. Strict prepared-cache feasibility. No feasibility clamp, cold fallback or alternate sweep budget.
6. Exactly eight N0/N1/N2/N3/tangent2x2/twist sweeps with the retained constitutive equations and pinned
   friction guard. Solver projection is the unchanged solver behavior, distinct from admission.
7. Copied proposed endpoint/cache; no accepted provenance mutation during speculative work.

D has no reference argument and runs before the independent reference is formed. It changes initialization
only. Historical load preparation precedes D even though a purely common shift is algebraically projected
out by D. General current tensor plumbing does not qualify off-COM or changing general-inertia motion.

`BeginInstall` checks the exact prescribed FP32 projection, saves the complete proposal and poisons private
continuation while native writes are pending. `CompleteInstall` must match that whole begun proposal;
a different speculative duration/geometry/load with the same generation cannot acknowledge it. Native
refresh has its own unsafe state: an unchanged accepted tuple is not falsely described as an untouched
native world. Refusal after unsafe refresh invalidates continuation. No canonical state exists to roll back.

## Representation and qualification limits

Ordinary represented-duration operations passed the integrated regression/actual-world gates. Their
scale-separated algebra can reassociate binary64 arithmetic, so only measured physical agreement is claimed;
the captured coast normal/linear errors match prior D while angular error differs in its last rounded digit.

The draft includes factored exponent-scaled arithmetic avoiding reciprocal overflow, but a single53-bit
scaled mantissa cannot retain every tiny perturbation beside a nonzero baseline. It is NOT a qualified
tiny-response consumer. The accepted two-row primitive's separate baseline/normalized response must
eventually extend through the actual active friction branch, accepted private cache/motion and coast.
That work was not claimed complete: the mandatory tiny reference-admission gate failed first.

The exact rational witness proves the interior solution violates the tangent Coulomb cap. It does not
measure D's tiny accuracy, solve active friction, prove a production regression or justify changing the
pinned1e-16f tangent denominator guard. The guard/twist/full active-branch applicability still belongs to the
remaining proof. No new correction was selected after the stop.
