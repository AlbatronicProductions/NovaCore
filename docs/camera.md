# Free Camera

Milestone 5 implements a managed, frame-aware Free camera. `CameraState` is authoritative: it stores `FramePosition`, a normalized double-precision XYZW Hamilton orientation quaternion, projection parameters, and camera mode. Native code supplies only `NativeInputState`; it does not mutate camera state, create camera commands, or resolve reference frames.

The input and render path is:

```text
Win32 input → NativeInputState → DebugCameraInput → bounded CameraCommand span
→ FreeCameraController → CameraState → ReferenceFrameSnapshot / ReferenceFrameResolver
→ CameraRenderSnapshotBuilder → GpuCameraData → RenderFrameSubmission
```

`CameraRenderSnapshotBuilder` resolves camera position and orientation to root/ECL coordinates and produces the view/projection matrix. Milestone 11B-2A makes the subsequent translation boundary explicit: `CameraRelativeRenderPosition` subtracts the FP64 root camera from an FP64 root object before any float conversion, high/low split, native packing, or Vulkan upload. The legacy encoded-position field remains zero in `GpuCameraData` to preserve ABI layout without transporting an absolute root translation. Authoritative world state is never rebased or mutated.

## Focus and orientation authority

`FocusTarget` identifies the physical point followed by a presentation camera. Its `BodyCenter` form evaluates to the current immutable body's FP64 root position at the current `SimulationInstant`. Its active `SurfaceAnchorFocus` form stores a body ID, body-fixed direction, authoritative elevation, body-local FP64 position, and a deterministic local east/north/up basis; its root position is `bodyRoot + BodyFixedToRoot * bodyLocalAnchor`. `SceneObject` accepts a caller-supplied current root position as an explicit alternative target. Focus evaluation is O(1), allocation-free, and owns no celestial or vessel state.

Milestone 11B-2B acquires a surface focus from the focused body's physical surface itself. When the body-center altitude enters the acquisition band, the current camera view ray is inverse-rotated into body-fixed space and intersected with the physical-radius/elevation surface. Earth refinement samples the loaded authoritative CPU elevation oracle; other current bodies use their physical-radius sphere. A miss never fabricates an anchor or substitutes the sub-camera point: the previous valid target is retained, or `BodyCenter` remains active when no anchor exists. The acquired body-local point is immutable until explicit surface-local translation or release.

Acquisition begins at 2,000 km altitude, reaches full surface-anchor focus at 1,000 km, and retains the anchor until receding through 3,000 km. Smoothstep position blending and the separate release threshold prevent target snapping and hysteresis chatter. Wheel input remains logarithmic but scales the distance to the physical anchor after acquisition; a bounded terrain query enforces ten metres of clearance, the smallest conservative floor for the current sampled elevation footprint and 5 cm minimum near plane. `W/S/A/D` in the full surface state moves the anchor along its east/north tangent axes without changing celestial state. Solar reversed-Z varies its near plane continuously as `clamp(0.02 × surfaceAltitude, 0.05 m, 1,000 km)` rather than switching at the focus-mode boundary.

Focus position and camera orientation remain independent authorities. The Solar camera is root-inertial in body-center, transition, and surface-anchor states: advancing the evaluated body translates the rig with the evaluated target, while `BodyFixedToRoot` updates the anchor position but never enters camera orientation. Click-drag changes only the root-inertial camera offset and view around the current focus. Body spin therefore moves fixed geography and an acquired anchor through root space without co-rotating the camera.

## Controls

- `W` moves forward along camera-local `-Z`; `S` moves backward along local `+Z`.
- `A` and `D` strafe left and right; `Q` and `E` move down and up.
- Diagonal motion is normalized before applying speed.
- Hold RMB and move the mouse for relative Raw Input look.
- Mouse wheel changes fly speed without translating the camera.
- `R` restores default position, orientation, projection, mode, and speed.

Movement is scaled by the current frame delta and clamps that delta to 0.1 seconds. Pitch is limited by the controller configuration. The right-handed identity camera looks along `-Z`.

The default speed is 0.1 units per second. Wheel detents apply `speed = clamp(speed * 2^(detents * 0.25), 0.00625, 64)`. Speed changes and reset speed are reported only with `--log=camera`.

Orbit and Follow camera modes in the generic controller enum remain future work. The Solar sample has its own presentation-only focus-orbit behavior and does not alter the Free-camera input contract.
