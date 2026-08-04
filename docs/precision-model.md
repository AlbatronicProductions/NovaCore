# Precision Model

Authoritative spatial values use `Double3` and double-precision quaternions. `FramePosition` is the contextual authoritative position representation. It resolves through a managed immutable `ReferenceFrameSnapshot` to a root/ECL `UniversePosition` before entering Graphics.

`UniversePosition` contains a root-space `Double3` and root `ReferenceFrameId`. `RenderOrigin` identifies the camera root position at the rendering boundary. `ReferenceFrame.Resolve` verifies the shared root frame and produces a camera-relative `RelativePosition`.

The rendering flow is:

```text
FramePosition
→ managed reference-frame resolution
→ root/ECL UniversePosition
→ GPU-only EncodedPosition
→ batched storage-buffer upload
→ shader reconstruction
→ rasterization
```

The camera renders from `(0, 0, 0)` and authoritative world state is never shifted. `EncodedPosition` is GPU transport data only; simulation and gameplay code must not depend on it.

Each component encodes as `high = (float)value` and `low = (float)(value - (double)high)`. The shader resolves `(object.high - camera.high) + (object.low - camera.low)` before normal FP32 vertex placement. At four trillion units, the 0.25-unit sample separation survives while a single world-space float produces zero. This is not GPU FP64: residual quantization grows with absolute magnitude. The included `10^15` test observes a 0.25-unit reconstruction error.
