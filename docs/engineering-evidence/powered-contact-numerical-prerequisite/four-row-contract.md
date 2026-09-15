> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Actual retained four-contact finite-iteration gate

Preregistered after cache-access Debug/Release PASS, before kernel execution. Candidate C survived the early primitives. This is its next necessary numerical gate, not another candidate.

## Fixture and genuine warm state

One numerical box 2 x 1 x 1 m, fixed authored dry inertia (2,2,2) kg m^2, source mass 8+1/128 kg, one unchanged slab, gravity 9.81, spring frequency 30 Hz, damping ratio 1, friction .5, recovery cap 2, eight velocity iterations and one substep. Run exactly 120 ordinary BEPU intervals of 1/60 s to obtain a genuine retained support manifold/cache (two seconds, sixty spring periods). This is cold reference preparation, not an added production warmup. Capture the actual geometry, velocity, all seven impulse slots, feature IDs and material before custom solving. No canonical state exists in the executable.

## Candidate and oracle

Continue C with the stable factored normal equation, all four current normal rows, the two-dimensional tangent block and the twist row. Preserve physical units, friction center, caps and update order. Start from the genuine cached impulses unchanged. Use the same midpoint mass for external force and generalized inverse properties. The narrow-phase body's FP32 inverse mass is the documented transport projection of that same midpoint. The fixture is convex: there is no compound reduction; powered M14.21 selector qualification remains a later required gate.

For one ordinary 1/60 s numerical piece compare (1) unchanged unpowered source mass, (2) mass-only 8+1/128 ->8 kg, and (3) that mass change with upward 32 N engine force. Geometry/initial velocities/cache are the same captured state. These are local finite-iteration experiments, not three alternate physical trajectories. No pose or resource publication is performed here.

Independently assemble the seven-row generalized matrix K and the linear system A=K+diag(alpha*Kii for normal rows,0 for friction), where alpha=softness/CFM. Solve A*lambda=rhs using pivoted elimination. Accept this reference only if normal impulses are positive and the tangent/twist limits are strictly inactive at the final solution; check its equation residual <=1e-12 N-scaled velocity. Candidate iterates retain actual projections, including the pinned tangent 1e-16 magnitude guard. Record active/clamped branch counts. The oracle is not another production iteration budget.

## Fixed bars

- Require finite values and actual source/body/feature/cache continuity.
- The sum of normal impulses after eight sweeps must differ from the independent converged local reference by <=1e-4 N s.
- Linear velocity and angular velocity vector errors must each be <=1e-4 m/s and rad/s respectively.
- Record individual impulse errors and first-sweep error as diagnostics, without adding a hidden acceptance criterion.

The tighter impulse bar is necessary to distinguish the proposed mass cadence: erroneously using source mass through the entire maximal-depletion 1/60 s centered piece changes the motionless gravity reaction by 9.81*(1/60)*(1/128)/2 = 0.000638671875 N s. A 0.24 N s support envelope cannot detect this error. The 0.0001 N s budget is below one sixth of that concrete physical discrepancy, corresponds to 0.0000125 m/s on the dry body, and reserves ample separation for a bounded reference/transport error. The velocity/angular bars independently bound the complete physical response; they cannot replace the impulse check. These are numerical prerequisite accuracy bars, not a claim that stock BEPU promises convergence to them.

Velocity-bar derivation: allow eight times the centered reaction's 0.0000125 m/s error for the complete three-component response, including friction and transport, giving 0.0001 m/s; the tighter reaction criterion is still independent. Angular-bar derivation: equal/opposite edge-reaction errors of 0.0001 N s at the two x=+/-1 m edges can hide in a net-force sum but produce 0.0002 N m s moment error, or 0.0001 rad/s with fixed I=2. This separately rejects that load-redistribution discrepancy. Actual captured geometry (rather than ideal support depth) enters both candidate and oracle. The 120-step state must first pass the existing 20 mm penetration and support-speed envelope. These derivations are recorded before any kernel result.

One Debug execution first. Stop on the first failed numerical case, retain exact errors, and do not tune, raise iterations, clear caches or run subsequent long/performance qualification. If all pass, run the same Release gate once, then continue C's outstanding full-world/tiny/selector/trajectory obligations. A failed eight-iteration gate does not establish impossibility of every bounded adapter or justify an architecture reopening automatically.

Before interpreting a physical failure, crosscheck candidate sweeps one/eight against an independently assembled affine block iteration G=-(D+L)^-1 U and q=(D+L)^-1 rhs. This uses pivoted matrix solves rather than the candidate normal/friction update functions. Guard positive normals and inactive friction limits on all eight reference iterates. A conservative arithmetic sanity bound is 2048*2^-52*(1+||lambda_old||1+||lambda_8||1)*(1+||G||infinity)^8; it explicitly allows amplification and ordinary coefficient/factorization rounding in this fixed seven-row problem. It is not a rigorous interval-arithmetic theorem or a physical tolerance. Physical acceptance still uses the independently stated 1e-4 bars. If the arithmetic crosscheck/domain fails, classify an implementation/reference limitation before making any iteration-budget claim.
