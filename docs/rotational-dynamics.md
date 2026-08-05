# Rotational Dynamics

Milestone 8B-1 adds a pure, internal rigid-body rotation contract. It does not change authoritative spacecraft storage, transactions, reference-frame extraction, input, rendering, or the sample.

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

Extreme-duration and high-warp rotational propagation, structure-preserving integration, torque controls, RCS, inertia-from-geometry, propellant, reaction wheels, SAS, authoritative replacement, frame extraction, and rendering integration remain deferred. Future improvements may replace the numerical method without changing the state contract.
