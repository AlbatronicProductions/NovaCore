# Powered contact physics and unresolved numerical bridge

These are derivations and requirements, not a simulated/qualified powered-contact result.

## One wrench and one mass law

For CentralPointReservoirV1, independently author dry mass m_d, fixed dry COM and positive dry principal inertia I_d. Remaining exact fuel supplies m(t)=m_d+M(t). Fuel at dry COM contributes zero inertia, so I(t)=I_d; do not scale inertia by total-mass ratio. The existing engineering article cannot silently become an 8 kg sphere or have its 1,000 kg relabelled wet/dry.

M14.23's stored net effective body wrench is F_B and tau_B=r_COM cross F_B; use its stored binary64 flow q as the resource law. No extra rocket-equation impulse, -q*v correction or second nozzle-reaction force is added.

The declared equations are:

dx/dt=v
dv/dt=g(t,x)+[R(Q)F_B+F_other]/m(t)+constraint response
dQ/dt=0.5 Q*(omega_B,0)
I_d domega_B/dt=tau_B+tau_other,B-omega_B cross(I_d omega_B)+constraint moment.

Gravity is acceleration, or exactly one explicitly identified mass-proportional force. Keeping the original M14 force -m0*g while changing mass produces the wrong gravity. A body-frame engine contribution must rotate with current stage orientation; external torque must be transformed consistently with BEPU's local-world angular velocity. The displayed Euler equation describes the full physics, not an instruction to add every term in a callback: the current LocalContactIntegrator uses BEPU ConserveMomentumWithGyroscopicTorque, so the adapter must not apply that gyroscopic correction a second time.

## Supported centered ignition

With upward thrust T, downward g and a planar normal reaction N, sustained nonpenetrating rest requires N(t)=m(t)g-T while N>=0. Decreasing mass and/or increased thrust unloads support. A unilateral contact cannot pull downward once support would require N<0; separating acceleration and subsequent geometric separation follow the solve.

This equality is an independent symmetric-fixture witness. It is NOT a consumer switch, a launch command or an airborne flag. Friction, offset thrust, attitude and multiple supports invalidate its use as a general liftoff decision.

Off-COM thrust changes tau, support distribution and angular motion. Per-contact moments r_i cross(n_i lambda_i) must balance the full body wrench where support is feasible. One support can unload before another. Do not constrain thrust to COM, clamp penetration, increase row count/iterations or retune the banked spring/friction settings to obtain ignition.

## Exact exhaustion

If resource has an interior exhaustion event, the physical history is powered exact duration h_p followed by unpowered exact duration h_c, with h_p+h_c equal to the admitted canonical interval. Mass reaches dry mass continuously; engine wrench becomes zero after the event. Actual endpoint NoFeed does not mean there was no earlier thrust.

At least two logically ordered physical pieces are required. A single averaged-thrust contact solve is not generally equivalent: an upward powered phase can unload/separate before coast, while averaged thrust can leave support active throughout. Constraints and free dynamics are state dependent; the two flow maps do not generally commute.

No canonical intermediate publication or event queue entry is needed solely for the exact resource split. Both pieces use one conditional resource trajectory and private physical continuation; the second reads the first private endpoint and exact dry successor. Canonical fuel/actual/time still advance once at successful whole-interval commit.

## Why one installed mass is not a complete answer

For a powered smooth slice h with m_a>m_b>0 and frozen force direction:

integral_0^h dt/m(t) = h*ln(m_a/m_b)/(m_a-m_b).

A logarithmic-mean effective mass m_eff=(m_a-m_b)/ln(m_a/m_b) reproduces constant-force delta-v. It does not automatically reproduce position, rotating thrust, gyro dynamics, changing contact Jacobians, active-set transitions or impact impulse timing.

A sharper supported counterexample: exact centered support under constant g and T has

integrated N = g*h*(m_a+m_b)/2 - T*h.

A frozen log-mean mass solve instead gives h*(m_eff*g-T). They differ for nonzero mass change. Thus using one effective mass cannot be declared a universal physically correct contact reconciliation merely because it matches an unconstrained velocity oracle. Source/endpoint mass followed by a final patch is weaker still.

Preferred research direction: exact event boundaries plus bounded deterministic mass/wrench/constraint integration within each smooth piece, using consistently evaluated inverse properties for velocity forcing, selection and effective constraint response. It needs a stated error/work envelope and active-set/support witnesses. No fixed substep count or large reservation is selected by guess in this audit. This proof is currently **MISSING**.

## FP32 exact-event obstruction

The existing permanent M15 fixture includes a positive duration 2^-1075 seconds that rounds to zero as binary64 seconds, while 16 N / 8 kg times that duration yields exactly double.Epsilon velocity. PoweredFreeFlightTests.cs:50-54 and PoweredFreeFlightTests.Physics.cs:129-134 retain that witness.

Pinned BEPU Solver_Solve.cs:1417-1421 consumes float substep dt and computes 1/dt; SpringSettings.cs:50-54 also depends on dt. A float cast can erase the segment, and even a nonzero tiny float dt can make reciprocal-dependent constraint terms nonfinite. Ordinary Timestep casts, more substeps, averaging or a tick snap do not preserve the admitted exact-resource event domain.

M15's exponent-scaled unconstrained derivative is not automatically a scaled contact operator. A correct contact bridge must preserve exact ordering and any representable physical effect without consuming resource on an unsupported/discarded solve. Silently refusing every tiny event conflicts with the retained exact-resource continuity contract and is not an architecture PASS.

One bounded numerical bridge proof is needed: ordinary representable pieces plus a derived, error-qualified event-sensitive/scaled contact treatment for the remainder of the admitted exact-duration domain. This ticket writes no prototype and supplies no unproven tiny-impulse shortcut.

## Selector inputs

CompoundContactSelector.cs:39-49,97-103 explicitly uses inverse mass/inertia and predicted v+a*dt+omega cross r. CompoundContactCoverage.cs:15,31 freezes acceleration today. Retain the four-row rule, precision classes, native feature slots and material parameters. Powered integration must prepare consistent predicted linear/angular motion and sampled inverse properties before selection, with no double application of thrust. Adding torque while leaving angular prediction at the old unpowered state requires qualification.

## Required future independent physics witnesses

Centered residual support and integrated normal impulse; off-COM sign/mirror/support unloading; exact interior exhaustion versus averaged-thrust counterexample; decreasing-mass versus endpoint-patch counterexample; tiny positive effect; asymmetric gyro; no-force/static M14 equivalence; M15-domain free equivalence; retained manifold/impulse lifecycle; bounded error/work and no canonical mutation on refusal.

No penetration, support, allocation or performance threshold is relaxed. Historical M14/M15 measurements are not measurements of this new combined consumer.
