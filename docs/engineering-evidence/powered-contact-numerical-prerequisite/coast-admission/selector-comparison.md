> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Selection, normal basis and source ownership

The diagnostic project constructs BEPU `Box(2,1,1)` and `Box(16,2,16)` directly using [WorldProbe.Callbacks](../../../../tests/NovaCore.ContactNumerics.WorldProbe/Program.cs). It does not invoke NovaCore's M14.21 compound-contact selector, `LocalContactCallbacks`, or a production spacecraft article. No compound selector was changed or qualified by this attribution.

## Current path

1. Pinned BEPU box-pair collision generation/reduction produces a convex manifold.
2. Diagnostic callback copies features and manifold normal/offset/depth into Capture, in manifold order.
3. BEPU's contact accessor transfers each feature and corresponding contact geometry into the same indexed constraint description/cache. Normal impulse history is transferred by feature identity.
4. Diagnostic Extractor reads actual solver prestep normal/offset/depth/material and seven accumulated impulses. It overwrites geometry, but feature IDs remain from the callback.
5. `OwnedQualification.Rows` widens that current geometry to the private FP64 admission input. `CacheHistory.Supported` compares every normal against the retained seed normal.

Source and coast both have four contacts ordered `[-4,-21,-5,-20]`, the same constraint handle and unchanged XZ coverage. In all four rows, separately captured callback and extracted solver normal bits, offset bits and depth values match exactly. This plus pinned indexed-transfer source supports their association. It is not independent extraction of solver feature IDs.

## Basis change

Source normal `(0,1,7.002423640933841e-11)` becomes `(0,1,-3.1946030265572745e-9)` after the accepted endpoint and collision refresh. Only the tiny Z component changes; this is not a180-degree normal flip. The robust angle `atan2(length(n_old×n_new),dot(n_old,n_new))` is **3.264627262966613e-9 rad**, or **1.8704936385133248e-7 degrees**. `acos(dot)` loses this distinction because the computed dot rounds to1.

Each recorded normal Z equals twice its respective stored quaternion X. Source shows normal construction through selected axes and the current body orientation; it does not promise an invariant exact previous normal. The specific winning SAT axis was not separately traced and is not needed to identify the exact failed equality. No selector order/coverage change or stale geometry is demonstrated.

The [piece kernel](../../../../tests/NovaCore.ContactNumerics.PreconditionedProbe/PieceKernel.cs#L47) builds tangent axes from the current normal; twist axis also uses it. CacheHistory retains the original normal but has no general basis-transfer proof. Therefore a tiny observed difference alone does not establish safe cache reuse under a relaxed predicate. The unit-normal bound is not an angular compatibility policy.

## Pinned BEPU source anchors

Version2.5.0-beta.29, source commit `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`:

- [ContactManifold.cs, lines17-36](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactManifold.cs#L17-L36): world-oriented offset from A, signed depth, normal B→A.
- [NarrowPhaseConstraintUpdate.cs, lines298-317](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NarrowPhaseConstraintUpdate.cs#L298-L317): dynamic A/static B convention.
- [ContactConstraintAccessor.cs, lines218-235](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactConstraintAccessor.cs#L218-L235): indexed feature/geometry copying.
- [BoxPairTester.cs, lines335-401](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/CollisionTasks/BoxPairTester.cs#L335-L401): face/edge axis selection, B→A orientation and transform through body A; lines517-525 perform ordinary convex reduction.

These were inspected by the independent read-only source investigator at the pinned revision. No dependency source or binary was changed; no broad KSA research occurred.
