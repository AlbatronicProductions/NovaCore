# First physical model: system boundary and equations

Architecture recommendation only. No production force, mass or fuel change is implemented.

## Adopt a declared net effective wrench, not a resolved nozzle

Current M14.23 computes stored binary64 `T = throttle * maximumThrust`, `F_B = T*d_B`, `tau_B = r_B cross F_B`, and `q = T / effectiveExhaustSpeed` at `SimulationTransactionEngine.EnginePreparation.cs:163-166`. These stored values, including the rounded q, are the admitted inputs. Do not recompute q from an unrounded symbolic ratio, or replace T by q times the authored exhaust speed.

NASA's [thrust equation](https://www.grc.nasa.gov/www/k-12/airplane/rktthsum.html) includes exhaust momentum and nozzle pressure contributions. Its [specific impulse convention](https://www.grc.nasa.gov/www/k-12/airplane/specimp.html) defines effective exhaust velocity from thrust divided by mass flow. These support treating M14.23 thrust as the effective engine reaction, not a force awaiting another rocket-reaction term.

**Proposed physical closure:** the ideal engine's supplied BODY force and BODY moment are the complete effective propulsion wrench on the retained vehicle. The model contains no separately resolved in-engine fluid momentum, nozzle-exit spin/swirl, jet damping, pressure force or transport reaction. Those contributions are either represented by that effective wrench or excluded from this declared idealization. The mount is the effective wrench application point; this does not certify an actual rotating nozzle's exit flow.

This is an explicit new physical-model assumption for Project Control acceptance, NOT a physical capability already proved by M14.23/M14.24. It preserves their numbers and authorship. It must be part of the future physical-model identity, separate from the resource mass-law identity.

For a control volume retaining spacecraft plus remaining fuel, let q>0 be outgoing mass rate, V the retained COM velocity, and u the effective outgoing velocity relative to that COM. With pressure folded into effective thrust, linear balance is

    d(m V)/dt = F_other - q (V + u)
    dm/dt = -q
    m dV/dt = F_other - q u = F_other + F_engine.

The V*dm/dt term cancels the transported bulk velocity. Thus no extra `-q*V`, `+q*V`, exhaust force, or separately applied rocket-equation delta-v belongs in the derivative. The ideal rocket equation is the integral of this same acceleration, not an additional impulse.

The adopted angular balance about the fixed body COM is

    d_B(I omega)/dt + omega cross (I omega)
        = tau_other + tau_engine_effective.

In a resolved open system the right-hand side would include the net flux/transport moment as well as surface forces; the effective-wrench closure has already chosen their resultant. It is NOT legitimate to infer their absence from constant stored inertia. For example, at a hypothetical exit r=(1,0,0), omega=(0,0,1), q=2, outgoing co-rotation alone carries `q*r cross (omega cross r)=(0,0,2)` in moment units even if stored I is constant. That is a counterexample to the inference, not another term to append to this model.

The DLR/ESA [variable-mass study, sections 2.2-2.3](https://www.eucass.eu/doi/EUCASS2022-6124.pdf) explicitly distinguishes thrust from transport/Coriolis and angular-flow effects. It supports the limitation: a real tank/nozzle model needs more than changing mass and a thrust lever arm. We do not transplant its geometry-specific equations or constants into NovaCore.

## CentralPointReservoirV1

The remaining resource is an ideal point at independently authored dry COM. Let dry mass be m_d>0, exact remaining mass M(t)>=0, and constant dry body inertia I_d positive diagonal:

    m(t) = m_d + M(t)
    COM_B(t) = (0,0,0)
    I_B(t) = I_d;  dI_B/dt = 0.

During a powered portion, M(t)=M_source-q*t; after depletion M=0. During an unpowered portion without depletion, M is its unchanged source value. Mass used at numerical stages follows the exact resource law; it is not the source mass held constant or the final mass substituted for the interval.

These mass properties are mathematically coherent with the declared effective-wrench system. They do not describe real spatial tanks, fluid travel time or nozzle mass distribution. No M14.21 article masses, COM, inertia or geometry are reauthored. The first physical fixture must author dry structure and point fuel separately.

A future mass-law provider must return m, COM, full body inertia and their necessary derivatives/transport convention. Moving COM also changes force moment arms and position/velocity transformations. Replacing I by I(t) adds `dI/dt * omega` on the left, but does not by itself account for outgoing angular momentum. That future model requires new applicability and qualification, not an automatic v1 extension.

## Coupled state derivative

Use `(x_R, v_R, Q_BtoR, omega_B)` and exact resource/segment data. Q is scalar-last Hamilton, active body-to-inertial-ECL rotation:

    dx_R/dt = v_R
    dv_R/dt = R(Q) F_engine,B / m(t) + F_other,R(t,x,v,Q,omega)/m(t)
    dQ/dt = 0.5 Q * (omega_B,0)
    domega_B/dt = I_d^-1 [tau_engine,B + tau_other,B - omega_B cross (I_d omega_B)].

The gyro sign matches current `SpacecraftRigidBodyRotationEvaluator.cs:93-100`; for X it is `(tau_x+(I_y-I_z)*omega_y*omega_z)/I_x`. Body axes remain +X forward, +Y right, +Z down. A root-space environmental moment is rotated into BODY at the current stage. Engine moment already is in BODY; do not rotate it to root and insert it into a BODY Euler equation.

At each coupled stage, use that stage's orientation for body thrust. Keep the RK stage quaternion algebra and existing endpoint normalization/sign canonicalization; use its normalized rotation for transforming physical vectors. Do not freeze thrust at segment start or normalize again during canonical fixed commit.

Current spacecraft translation has no hidden gravity provider. The first implementation should admit zero environment, and optionally an explicitly authored constant root force used once, with no disguised gravity. A later point-mass gravity provider contributes acceleration g(x,t); its equivalent force is m(t)*g, so gravitational acceleration does not change solely because fuel decreases. Select gravitating sources explicitly and evaluate g from the stage position/time. Never add g to a legacy net force that already includes the same gravity. Tides/gradient torque and real Earth integration are outside the first slice.

## Independent analytic fixture

For constant root thrust direction, no torque/rotation, constant q>0, no other forces, h powered seconds, m1=m0-qh>0:

    delta_v = (T/q) ln(m0/m1)
    delta_x_powered = (T/q^2) [(m0-m1) - m1 ln(m0/m1)]
    x(h) = x0 + v0*h + delta_x_powered.

Follow with the unpowered coast from that private endpoint; do not restart from the original x0/v0. A constant g adds g*h to velocity and g*h^2/2 to position over the complete interval when present. Use `T/stored_q`, not presumed bit equality to authored exhaust speed. For small fractional mass loss, a qualified log1p/series form avoids cancellation; the oracle uses high precision, independently of future production RK code. q=0 is the legitimate no-demand/no-feed unpowered path, not a division limit used to allow fuel-free thrust.

The algebra witness gives, for dry 8 kg, fuel 1/128 kg, q 3 kg/s, T 9000 N and 16,667 ticks: powered 15625/6 ticks then unpowered 84377/6 ticks; delta-v 2.9282579191663767 m/s and displacement 0.04499181863057906 m. Constant-source-mass integration instead gives 2.926829268292683 m/s and 0.04497048780487805 m. Editing endpoint mass cannot repair that trajectory.

This closed form is an independent permanent qualification oracle, not the general rotating/off-COM integrator. Off-COM tests need a separate converged reference of the explicitly declared effective-wrench equations.
