> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Pinned boundary lifecycle semantics

Pinned BEPU2.5.0-beta.29, source commit f73164bb3c9ca733eb3329f1f6b1cea4e216ece7.
The package, DLLs, integrity pins and dependency architecture are unchanged.

For the four-contact convex one-body manifold the tangent disk has radius
L=(mu/4)*sum(lambdaNi), and twist interval radius
B=(mu/4)*sum(lambdaNi*distance(contact_i,frictionCenter)). Here mu=.5, so mu/4=.125.
The normal quantities are current accumulated impulses, not increments or independent
per-contact caps. Valid solved tangent and twist sets are CLOSED: norm(tau)<=L; abs(w)<=B.

Tangent writes x*min(1,L/max(double(1e-16f),norm(x))) back to the accumulated vector by
reference. Twist writes an inclusive Min/Max clamp in[-B,+B]. Neither moves a saturated
answer inward. [Tangent projection](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TangentFrictionOneBody.cs#L55-L63),
[twist projection](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TwistFrictionOneBody.cs#L35-L39).

OneBodyTypeProcessor reads the persistent accumulated-impulse bundle by reference and calls
WarmStart without any strict-interior friction gate. Contact4OneBodyFunctions warm-applies
tangent, four normals, then twist. Its next solve updates normals, recomputes the caps,
and updates/projects tangent and twist. Same-type refresh remaps normal contact impulses
while retaining friction components; that finding does not cover constraint-type replacement.
[Type processor](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/OneBodyTypeProcessor.cs#L82-L127),
[four-contact warm start/solve](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs#L787-L822),
[same-type cache refresh](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactConstraintAccessor.cs#L26-L75).

Thus equality is valid historical solved state. This source finding does not grant current
initialization feasibility under the prerequisite's stronger full-wrench preparation
contract. Boundary history can become interior, boundary or infeasible in new geometry/load.

The exact historical classification uses the existing Scaled/ScaleMath cap and magnitude
arithmetic that produced and checked the saved solve. No epsilon, inward adjustment, clamp
or substitution of an unrelated exact-real norm is introduced. A stored value outside that
consistent closed set is rejected. The old high-precision reference independently bounds
the saved tiny solution's physical error; these are distinct validation responsibilities.
