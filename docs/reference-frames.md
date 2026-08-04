# Hierarchical celestial reference frames

Milestone 4 keeps all `ReferenceFrame` semantics in managed `NovaCore.Core.ReferenceFrames` and evaluates them from explicit caller-supplied state. `ReferenceFrameDefinition` contains only ID, parent ID, kind, and diagnostic name. Snapshot-time transforms, origin velocities, angular velocities, and inertial state are stored separately in `EvaluatedReferenceFrame`.

The hierarchy is `ECL → ORB` and `ECL → CCE → CCI → CCF`. ECL is the sole inertial root. ORB receives an explicitly supplied ECL origin and velocity; this milestone derives only its orientation as `Rz(LAN) * Rx(inclination) * Rz(argument of periapsis)`. CCF receives an explicit rotation angle and angular rate, never wall-clock time.

All values are doubles and all transforms are rigid local-to-parent transforms: `pParent = translation + rotation(pLocal)`. Velocity transport includes `omega × r`. `ReferenceFrameSnapshot` validates IDs, parents, cycles, root validity, finite values, and normalized quaternions, then caches local-to-root transforms and root kinematics. `FramePosition` resolves to a root/ECL `UniversePosition` before the unchanged `RenderSubmission` and high/low GPU transport path.
