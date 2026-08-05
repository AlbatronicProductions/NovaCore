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

The sample fixture starts with spherical inertia `(120, 120, 120) kg·m²`, body angular velocity `(.08, .12, .35) rad/s`, and constant body torque `(1.5, -.8, .6) N·m`. At exactly 5,000 simulated seconds a closed timeline event evaluates the rigid state, commits a torque-free replacement at that instant, and continues from the resulting orientation and angular velocity. This is fixture-only presentation, not player control.

Extreme-duration and high-warp rotational propagation, structure-preserving integration, torque controls, RCS, inertia-from-geometry, propellant, reaction wheels, and SAS remain deferred.
