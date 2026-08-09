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

`FocusTarget` identifies the physical point followed by a presentation camera. Its active `BodyCenter` form evaluates to the current immutable body's FP64 root position at the current `SimulationInstant`. The structural `SurfaceAnchorFocus` form stores a body ID, body-fixed direction, authoritative elevation, body-local FP64 position, and local tangent-basis placeholder; its root position is `bodyRoot + BodyFixedToRoot * bodyLocalAnchor`. `SceneObject` is a future seam that accepts a caller-supplied current root position. Focus evaluation is O(1), allocation-free, and owns no celestial or vessel state.

Focus position and camera orientation are independent authorities. The Solar orbital camera remains root-inertial by default: advancing the evaluated target translates the camera rig, while `BodyFixedToRoot` never enters camera orientation unless a future explicit camera mode requests that behavior. Click-drag still changes the root-inertial orbital offset and view; wheel input still applies the existing logarithmic focus-distance policy. Phase B may acquire a surface anchor and add a true local east/north/up mode, but 11B-2A does not switch focus kinds automatically.

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
