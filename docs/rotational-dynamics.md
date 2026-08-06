# Rotational Dynamics

Milestone 8B-2 integrates the pure rigid-body rotation contract into authoritative spacecraft storage, exact-time transaction replacement, and body-frame extraction. A spacecraft with a rigid-body state uses it as its sole authoritative rotation source; the older attitude value remains compatibility data only.

`SpacecraftRigidBodyRotationState` contains an exact epoch, canonical local-to-parent quaternion, body-space angular velocity, diagonal principal inertia, constant body-space torque, and `ConstantBodyTorqueV1`.

For principal moments `(Ix, Iy, Iz)`, body angular velocity `(wx, wy, wz)`, and body torque `(tx, ty, tz)`, the evaluator uses the complete Euler equations:

```text
dwx/dt = (tx + (Iy - Iz) wy wz) / Ix
dwy/dt = (ty + (Iz - Ix) wz wx) / Iy
dwz/dt = (tz + (Ix - Iy) wx wy) / Iz
dq/dt  = 0.5 * q * (wbody, 0)
```

Quaternion conventions remain XYZW Hamilton, active local-to-parent, with body `+X` forward, `+Y` right, and `+Z` down.

Evaluation uses deterministic fixed-step RK4: full substeps are exactly 10,000 simulation microticks (10 ms), followed by one exact integer-tick remainder substep. Evaluation is bounded to 1,000,000 substeps and normalizes/canonicalizes the quaternion after every completed substep. It is deterministic and allocation-free after warmup, but it is not claimed to conserve energy exactly or to be bitwise reversible under forward/backward evaluation.

The player fixture starts stationary with spherical inertia `(120, 120, 120) kg·m²`. Each requested control axis uses `4 N·m`; this produces an acceleration of approximately `.0333 rad/s²` about that axis, making a short 1× hold visible without automatic scripted rotation.

In `--scene=celestial`, W/S request positive/negative body-X pitch torque, A/D request positive/negative body-Y yaw torque, and Q/E request positive/negative body-Z roll torque. A key edge or changed combination commits one exact-time replacement; unchanged held input creates no transaction or history entry. Releasing commits zero torque once. Numeric keys select sample-local SAS modes: `1` captures the current authoritative orientation as a hold target, `2`–`7` select the flight-reference modes, and `0` clears the selection. Manual nonzero torque disengages the selected mode. Active SAS evaluates every 50,000 simulation ticks (20 Hz), clamps through the existing controller, then quantizes each requested torque component to `.01 N·m` using midpoint-away-from-zero rounding before suppression and authoritative commit. Unchanged quantized requests create no transition. SAS is suspended above 10× and commands zero torque once; it resumes at the first future boundary when the rate returns to 10× or lower. This is a one-spacecraft fixture control path only. RCS, inertia-from-geometry, propellant, reaction wheels, final SAS tuning, and full high-warp control behavior remain deferred.

The guidance module now provides a pure PD SAS torque calculation, `Kp ⊙ attitudeErrorBody - Kd ⊙ angularVelocityBody`, with canonical shortest-path quaternion error, deadbands, and per-axis clamping. It owns no scheduling, input, transaction, or state mutation.

The celestial SAS fixture uses the same spherical inertia `(120, 120, 120) kg·m²` with fixture-specific SAS authority `(8, 8, 8) N·m`, proportional gain `(7.5, 7.5, 7.5)`, derivative gain `(63, 63, 63)`, `.002` attitude/rate deadbands, `.01` settled thresholds, `.01 N·m` torque quantization, and a 20 Hz cadence. This deliberately mildly overdamped presentation configuration prioritizes stable convergence over aggressive snapping: 90-degree Prograde, Normal, and Radial Out acquisitions are validated within 35 simulated seconds, while the 180-degree Retrograde fixture is validated within 55 seconds. These values describe only the demonstration spacecraft; future spacecraft may define different inertia and actuator authority.
