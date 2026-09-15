> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Independent references and numerical limits

## Exact duration and spring representation

The banked PropellantInteger/PropellantDuration and PoweredFlightNumerics code is source-linked unchanged. Fuel 2^-1074 kg divided by 2 kg/s gives positive exact duration 2^-1075 s, although its binary64 seconds projection is zero. Multiplying the factored duration by 16/8 before projection yields exactly one binary64 epsilon of free velocity. This is not a retained-contact result.

For the current spring, let w be the **actual stored** angular frequency, h the numerical piece duration, a=w*h, k=a*(a+2). Stable coefficients are c=k/(1+k), s=1/(1+k), and position-error gain w/(a+2). Stock reciprocal evaluation fails for the tested positive FP32 h=2^-140. Similarity scaling preserves w*h and therefore does not remove that dimensionless problem by itself. Algebraic rearrangement is a necessary numerical correction, not an increased spring or minimum timestep.

At zero depth, one active normal row with inverse effective mass K=1/8 has lambda=-c*v_free/K and v=s*v_free. This supplies the scalar independent reference. Factoring the old warm kick before arithmetic preserves the small response without changing its mathematical role as an initial iterate. Merely adding the free residual after an ordinary contact solve is wrong: the residual must receive the contact operator's response.

## Actual seven-row system

The finite-body patch has generalized velocity u=(v,w_body), normal rows Ji=(n,ri cross n), tangent rows at the accepted friction center, and twist row (0,n). The fixed dry inertia is spherical I=(2,2,2); this is an authored qualification model, not a reinterpretation of the article or its uniform-box inertia.

Kij=Ji*M^-1*Jj^T. For each normal, alpha=1/[w*h*(w*h+2)]. In a strictly active, unclamped branch the converged impulse satisfies:

`[K + diag(alpha*K00, ..., alpha*K33, 0, 0, 0)] lambda = bias - J*u_free`.

Normal bias is `min(depth/h, depth/(h+2/w), recoveryCap)`. Signed speculative depth remains included. The candidate evaluates c*bias in a form avoiding inverse h; after analytically cancelling its own warm kick, a normal update is:

`lambda_i = max(0, [c*bias_i - c*(Ji*u_free + sum(j!=i,Kij*lambda_j))]/Kii)`.

The actual tangent/twist laws are not softened. Normal rows 0..3 update first, then the coupled tangent block, then twist. The four-contact tangent cap is `(friction/4)*sum(lambda_normal)`; twist uses the same factor times each normal impulse's distance from the friction center. The physical-unit 1e-16f tangent denominator guard is retained. The friction center averages contacts with nonnegative depth, or all contacts if all are speculative.

The oracle independently assembles A and uses pivoted elimination. It checks equation residual and friction/normal applicability. A separate block splitting A=L+U (L includes the tangent 2x2 block) constructs `G=-L^-1*U` and `q=L^-1*rhs`. Repeated affine multiplication gives the first/eighth iterates. This numerical construction does not call the candidate update functions. It distinguishes arithmetic implementation error from finite iteration truncation; its arithmetic check is a conservative sanity bound, not formal interval certification.

Independence is limited to the solution algorithms: both references use the same captured K/Jacobian assembly and velocity reconstruction as the candidate. Those shared equations were separately inspected against pinned source by the numerical and source reviewers. They are not an independently coded geometry oracle. The original preregistration's stronger assembly wording is corrected in primitive-limitations.md without rewriting the pre-run document.

All failed-case warm iterates are unclamped; the converged normal impulses are positive and friction is interior. The affine candidate error is 9.020562075079397e-17; the converged-reference equation residual is 5.519675414929199e-17. The physical reaction error is 0.002512790148672339 N s, much larger than either. The cold control has a tangent clamp and is only compared to the admissible converged solution, not claimed to follow the warm affine sequence.

## Equilibrium, mass cadence and interpretation

For an ideal flat patch at x=+/-1,z=+/-1/2, Kii=1/m+5/8. Net downward force W=m*g-T gives stationary impulses W*h/4 and compliant equilibrium depth Kii*W/(4*w^2). The actual test uses captured depths (~0.41449 mm); it does not move the body to a desired analytical equilibrium or confuse FP32 geometric quantization with convergence error.

For continuous linear mass, stationary centered reaction is `g*h*(ma+mb)/2 - T*h`. For moving support the exact momentum balance also requires `mb*vb - ma*va + q*(yb-ya)`, q=(ma-mb)/h. Endpoint momentum alone omits a term. The current local experiment tests a **declared midpoint approximation** against its converged constraint equation, not this complete continuous-mass trajectory reference.

For the maximal qualification depletion 1/128 kg over 1/60 s, using source mass instead of mean mass changes stationary gravity reaction by 0.000638671875 N s. The preregistered 0.0001 N s error budget is below one sixth of that discrepancy. Independent velocity/moment bars and their derivations are in four-row-contract.md. No bar changed after execution.

No-contact ordinary BEPU velocity-then-position differs from exact constant-acceleration position by a*h^2/2 (2 m/s^2 over 1/60 s gives 0.0002777777778 m). Therefore a future full C implementation needs a separately qualified free/contact pose quadrature; stock drift cannot be labelled M15-equivalent. This was a source/mathematical observation, not an executed full free-flight comparison.

## Source ownership

Pinned BEPU source commit: f73164bb3c9ca733eb3329f1f6b1cea4e216ece7.

- [Normal law](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/PenetrationLimitOneBody.cs).
- [Four-contact ordering and friction center](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs).
- [Tangent block](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TangentFrictionOneBody.cs), [twist](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TwistFrictionOneBody.cs).
- [Prestep fields](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexCommon.cs), [public extraction](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ISolverContactDataExtractor.cs).

BEPU owns real collision/manifold/cache preparation. The new test-only kernel owns the bounded numerical experiment. Existing canonical/resource/actuator owners are not used or replaced. No new KSA research or proprietary source copying was performed.
