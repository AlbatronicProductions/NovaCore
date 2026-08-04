# Free Camera

Milestone 5 implements a managed, frame-aware Free camera. `CameraState` is authoritative: it stores `FramePosition`, a normalized double-precision XYZW Hamilton orientation quaternion, projection parameters, and camera mode. Native code supplies only `NativeInputState`; it does not mutate camera state, create camera commands, or resolve reference frames.

The input and render path is:

```text
Win32 input → NativeInputState → DebugCameraInput → bounded CameraCommand span
→ FreeCameraController → CameraState → ReferenceFrameSnapshot / ReferenceFrameResolver
→ CameraRenderSnapshotBuilder → GpuCameraData → RenderFrameSubmission
```

`CameraRenderSnapshotBuilder` is the Graphics boundary. It resolves camera position and orientation to root/ECL coordinates, encodes only the root position for GPU transport, and produces the view/projection matrix. The renderer remains camera-relative and has no reference-frame semantics.

## Controls

- `W` moves forward along camera-local `-Z`; `S` moves backward along local `+Z`.
- `A` and `D` strafe left and right; `Q` and `E` move down and up.
- Diagonal motion is normalized before applying speed.
- Hold RMB and move the mouse for relative Raw Input look.
- Mouse wheel changes fly speed without translating the camera.
- `R` restores default position, orientation, projection, mode, and speed.

Movement is scaled by the current frame delta and clamps that delta to 0.1 seconds. Pitch is limited by the controller configuration. The right-handed identity camera looks along `-Z`.

The default speed is 0.1 units per second. Wheel detents apply `speed = clamp(speed * 2^(detents * 0.25), 0.00625, 64)`. Speed changes and reset speed are reported only with `--log=camera`.

Orbit and Follow camera modes are enumerated for future use but are not implemented. They will remain separate controllers and must not alter the Free-camera input contract.
