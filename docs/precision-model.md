# Precision Model

Authoritative spatial values use `Double3` and double-precision quaternions. `FramePosition` is the contextual authoritative position representation. It resolves through a managed immutable `ReferenceFrameSnapshot` to a root/ECL `UniversePosition` before entering Graphics.

`UniversePosition` contains a root-space `Double3` and root `ReferenceFrameId`. The authoritative camera resolves through `CameraRenderSnapshotBuilder` to root/ECL position and orientation. Simulation position authority, focus position authority, camera orientation authority, and render-space authority are separate: none may implicitly rewrite or inherit another.

The rendering flow is:

```text
FramePosition
→ managed reference-frame resolution
→ root/ECL UniversePosition
→ FP64 objectRoot - cameraRoot
→ CameraRelativeRenderPosition
→ optional GPU-only float or high/low encoding
→ batched storage-buffer upload
→ shader camera-relative reconstruction
→ rasterization
```

The camera renders from `(0, 0, 0)` and authoritative world state is never shifted. The mandatory ordering is `cameraRelativeDouble = objectRootDouble - cameraRootDouble` on the CPU. Only that result may be narrowed, split, packed, or uploaded. `(float)objectRoot - (float)cameraRoot`, including subtraction of independently narrowed astronomical high/low lanes in a shader, is forbidden. `CameraRelativeRenderPosition` makes this boundary explicit and validates matching root frames where `UniversePosition` is available.

When a relative value still benefits from a split, each component encodes as `high = (float)relative` and `low = (float)(relative - (double)high)`. The generic object shader reconstructs `high + low`; specialized planetary compute paths likewise receive body-local or camera-relative doubles split only after root subtraction. The legacy camera-position field stays zero solely to retain the fixed GPU/native ABI; authoritative camera root position never crosses that boundary. At a four-trillion-metre root, tests retain represented separations from one kilometre through approximately one millimetre (the FP64 value is `0.0009765625 m`) with zero observed transport error for the large-root cases, while the single-float control collapses the millimetre delta to zero. The limiting quantization is therefore the upstream FP64 root representation, not premature FP32 astronomical subtraction.

All current GPU-visible translation paths were audited in 11B-2A. Distant bodies, detailed/regional planets, eyeball terrain, stellar Sun, rings, markers, labels, environment/atmosphere, Solar lighting, and orbit lines already subtracted in managed doubles before native float transport. The generic triangle/object path was the sole demonstrated violation: it encoded absolute root values separately and subtracted high/low lanes in the vertex shader. It now stores an already camera-relative split without changing the fixed native object ABI.

Milestone 11B-2B keeps surface focus inside the same precision boundary. A `SurfaceAnchorFocus` stores its acquired point as a body-local FP64 vector and reevaluates root position as `bodyRoot + BodyFixedToRoot * anchorLocal` at the current instant. East/north/up conversion is a pair of FP64 dot-product/basis transforms; tests round-trip metre, centimetre, and represented millimetre offsets. Camera clearance and logarithmic zoom operate on FP64 root or anchor-relative distances. Only the final root-relative render values are narrowed, and a surface anchor never rebases or mutates the evaluated body.
