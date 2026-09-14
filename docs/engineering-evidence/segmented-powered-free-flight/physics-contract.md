# Bounded physical propulsion contract

The fixture is independently authored: dry mass 8 kg; dry spherical principal inertia (2,2,2) kg m^2; fixed COM; CentralPointReservoirV1 fuel initially at most 1/128 kg with zero inertia contribution. It has no gravity, environment force, contact body, slab or BEPU world. Maximum body force is 9,000 N, body torque 2 N m, interval 16,667 microsecond ticks, source position components 1,000 m, speed 100 m/s and angular-speed envelope 1.1 rad/s. Admission and finite checks enforce this numerical domain. It does not admit arbitrary M14.24 resource inputs into dynamics.

## Equations and conventions

`x' = v`, `v' = Rotate(Q, F_body) / m(t)`, with `m(t) = 8 + remaining exact-law fuel` projected at each dyadic RK stage. `I omega' + omega cross (I omega) = tau_body`; the admitted spherical inertia makes the gyroscopic term zero. The scalar-last Hamilton convention is `Q' = 0.5 Q * (omega,0)`, using the current NovaCore body-rate convention. Orientation rotates thrust at every derivative stage. Engine force and torque are zero in the unpowered segment.

The banked body wrench is the **net effective propulsion reaction**. No extra exhaust-momentum impulse is applied. Fixed-thrust changing mass therefore reproduces the rocket logarithm through force/mass integration itself. The analytic rocket solution is only a test oracle.

## Exact segments and numerical time

Genuine engine/resource leases determine full-unpowered, full-powered, endpoint exhaustion or powered-then-unpowered interior exhaustion. Exact units and exhaustion ratios remain M14.24 authority. Numerical endpoint mass is the exact-law successor mass, and each stage mass derives from that same law rather than holding source mass and patching the endpoint.

`PoweredBinaryScale` retains a normalized binary significand and independent exponent. Exact positivity is decided before FP64 conversion. Weighted derivative products are scaled before projection; positive subnormal-duration events can therefore retain representable impulse. The ratio helper uses fixed-size exact integer arithmetic, guard/sticky bits and ties-to-even rounding. Resource stage interpolation uses exact dyadic weights. Cumulative raw RK sums project from the segment source, avoiding repeated per-step subnormal rounding loss.

Trajectory is numerical under the predeclared error contract. Exact event/resource authority does not imply exact trajectory or universal correctly rounded quaternion arithmetic.

## Independent witnesses

The straight-line case uses 9,000 N, flow 3 kg/s and 1/128 kg fuel. A separately evaluated logarithmic closed form, including the coast after interior exhaustion, gives position error 2.220446049250313e-16 m and velocity error 0. A source-mass-only integration differs by more than 1e-3 m/s.

The rotating case uses 8 N at body Y=0.25 m, initial omega Z=0.6 rad/s, torque Z=-2 N m. Independent scalar angle `0.6t - 0.5t^2` plus 16,384-panel Simpson integration of root thrust/mass gives x error 1.0842021724855044e-19 m, vy error 2.439454888092385e-19 m/s and quaternion angular-distance witness 6.938893903907228e-18 rad. A frozen root thrust misses 8.247412062658337e-5 m/s in Y. Permanent tests independently enforce all four component ceilings, including omega.

Three complete publication tests produce exactly one `double.Epsilon` of delta-v: (1) epsilon fuel, 16 N, 8 m/s exhaust with exact positive duration 2^-1075 s which converts to double zero; (2) fuel 1/128, thrust 2^-64, exhaust 2^-1064, flow 2^1000; (3) full ordinary duration, thrust 512 epsilon, exhaust 8, whose individual small increments would otherwise round away. Exact consumption is positive in all three.

## Actual actuator and revisions

Requested command, prepared latch/wrench and actual applied state remain distinct. Actual state is Off, EnabledNoFeed, EnabledIdle (enabled zero demand with feed), or ProducingOutput. Endpoint throttle is zero without endpoint output; exact applied powered duration remains in the interval record even when the endpoint is NoFeed.

StateRevision and ActuatorRevision each advance exactly once for every successfully applied frontier, including no-output intervals. ResourceRevision advances only for positive exact consumption. CommandRevision advances only under its banked command contract. TimelineRevision is unchanged by physical publication.

## Determinism and visible model

Straight and rotating 1,200-interval streams match their prefunded reference under 30/60/150/240 Hz and a 100 ms/zero-credit delayed schedule: 400 short + 800 long intervals, 20,000,000 ticks, 1,200 physical/actual revisions and history records, final debt zero. Physical/resource/actuator/revision/exhaustion history matches bitwise on this machine/build. Host-partition-specific DebtBefore/After legitimately differs and is checked independently for exact conservation. Delayed runs each hit the four-interval budget 200 times without lost work.

The development scene uses an explicitly authored uniform cube of side sqrt(1.5) m, matching dry I=2 for mass 8. One body-X 8 N engine with exhaust 5,120 m/s consumes the point fuel in approximately five seconds, then coasts to eight seconds. It renders copied committed endpoints and holds them between publications. The reference grid is presentation-only. Manual visible acceptance is pending; automated geometry/pose tests do not grant it.
